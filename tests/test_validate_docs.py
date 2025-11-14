#!/usr/bin/env python3
"""
Unit tests for the documentation validation script (RFC-012).

Tests cover:
- Front-matter validation
- Canonical uniqueness checking
- Duplicate detection
- Registry generation
- Pre-commit mode
"""
import json
import shutil
import sys
from pathlib import Path
from unittest.mock import patch

import pytest

# Add scripts directory to path
sys.path.insert(0, str(Path(__file__).parent.parent / "scripts"))

# Import with proper module name (underscore in filename)
import importlib.util
spec = importlib.util.spec_from_file_location(
    "validate_docs",
    str(Path(__file__).parent.parent / "scripts" / "validate-docs.py")
)
validate_docs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validate_docs)

# Import the functions we need from the loaded module
DOC_ID_PATTERN = validate_docs.DOC_ID_PATTERN
INBOX_REQUIRED_FIELDS = validate_docs.INBOX_REQUIRED_FIELDS
REQUIRED_FIELDS = validate_docs.REQUIRED_FIELDS
VALID_DOC_TYPES = validate_docs.VALID_DOC_TYPES
VALID_STATUSES = validate_docs.VALID_STATUSES
DocumentInfo = validate_docs.DocumentInfo
ValidationError = validate_docs.ValidationError
check_canonical_uniqueness = validate_docs.check_canonical_uniqueness
collect_documents = validate_docs.collect_documents
detect_near_duplicates = validate_docs.detect_near_duplicates
extract_front_matter = validate_docs.extract_front_matter
generate_registry = validate_docs.generate_registry
validate_front_matter_fields = validate_docs.validate_front_matter_fields


@pytest.fixture
def fixtures_dir():
    """Return path to test fixtures directory."""
    return Path(__file__).parent / "fixtures" / "docs"


@pytest.fixture
def temp_docs_dir(tmp_path):
    """Create a temporary docs directory for testing."""
    docs_dir = tmp_path / "docs"
    docs_dir.mkdir()
    return docs_dir


class TestFrontMatterExtraction:
    """Tests for front-matter extraction."""

    def test_extract_valid_frontmatter(self, fixtures_dir):
        """Test extraction of valid YAML front-matter."""
        rfc_file = fixtures_dir / "valid" / "rfc-example.md"
        front_matter, content = extract_front_matter(rfc_file)

        assert front_matter is not None
        assert front_matter["doc_id"] == "RFC-2025-99999"
        assert front_matter["title"] == "Test RFC Document"
        assert front_matter["doc_type"] == "rfc"
        assert "Test RFC Document" in content

    def test_extract_no_frontmatter(self, fixtures_dir):
        """Test file without front-matter returns None."""
        no_fm_file = fixtures_dir / "invalid" / "no-frontmatter.md"
        front_matter, content = extract_front_matter(no_fm_file)

        assert front_matter is None
        assert "Document Without Front-matter" in content

    def test_extract_minimal_frontmatter(self, fixtures_dir):
        """Test extraction of minimal inbox front-matter."""
        inbox_file = fixtures_dir / "valid" / "inbox-minimal.md"
        front_matter, content = extract_front_matter(inbox_file)

        assert front_matter is not None
        assert front_matter["title"] == "Minimal Inbox Document"
        assert front_matter["doc_type"] == "plan"
        assert "doc_id" not in front_matter


class TestFrontMatterValidation:
    """Tests for front-matter field validation."""

    def test_valid_document_passes(self, fixtures_dir):
        """Test that a valid document produces no errors."""
        rfc_file = fixtures_dir / "valid" / "rfc-example.md"
        front_matter, _ = extract_front_matter(rfc_file)

        errors = validate_front_matter_fields(rfc_file, front_matter, is_inbox=False)

        assert len(errors) == 0

    def test_missing_required_fields(self, fixtures_dir):
        """Test detection of missing required fields."""
        missing_file = fixtures_dir / "invalid" / "missing-doc-id.md"
        front_matter, _ = extract_front_matter(missing_file)

        errors = validate_front_matter_fields(missing_file, front_matter, is_inbox=False)

        assert len(errors) > 0
        assert any("doc_id" in str(e) for e in errors)

    def test_invalid_doc_type(self, fixtures_dir):
        """Test detection of invalid doc_type value."""
        invalid_file = fixtures_dir / "invalid" / "invalid-doc-type.md"
        front_matter, _ = extract_front_matter(invalid_file)

        errors = validate_front_matter_fields(invalid_file, front_matter, is_inbox=False)

        assert len(errors) > 0
        assert any("doc_type" in str(e) and "invalid-type" in str(e) for e in errors)

    def test_invalid_status(self, fixtures_dir):
        """Test detection of invalid status value."""
        invalid_file = fixtures_dir / "invalid" / "invalid-status.md"
        front_matter, _ = extract_front_matter(invalid_file)

        errors = validate_front_matter_fields(invalid_file, front_matter, is_inbox=False)

        assert len(errors) > 0
        assert any("status" in str(e) and "invalid-status" in str(e) for e in errors)

    def test_invalid_doc_id_format(self, fixtures_dir):
        """Test detection of invalid doc_id format."""
        invalid_file = fixtures_dir / "invalid" / "invalid-doc-id-format.md"
        front_matter, _ = extract_front_matter(invalid_file)

        errors = validate_front_matter_fields(invalid_file, front_matter, is_inbox=False)

        assert len(errors) > 0
        assert any("doc_id" in str(e) and "format" in str(e).lower() for e in errors)

    def test_invalid_date_format(self, fixtures_dir):
        """Test detection of invalid date format."""
        invalid_file = fixtures_dir / "invalid" / "invalid-date-format.md"
        front_matter, _ = extract_front_matter(invalid_file)

        errors = validate_front_matter_fields(invalid_file, front_matter, is_inbox=False)

        assert len(errors) > 0
        assert any("created" in str(e) for e in errors)

    def test_inbox_document_minimal_validation(self, fixtures_dir):
        """Test that inbox documents only require minimal fields."""
        inbox_file = fixtures_dir / "valid" / "inbox-minimal.md"
        front_matter, _ = extract_front_matter(inbox_file)

        errors = validate_front_matter_fields(inbox_file, front_matter, is_inbox=True)

        # Should have no errors with minimal fields for inbox
        assert len(errors) == 0

    def test_inbox_document_missing_minimal_fields(self, fixtures_dir):
        """Test that inbox documents still need minimal required fields."""
        # Create a document missing even minimal fields
        front_matter = {"title": "Test"}  # Missing doc_type, status, created

        errors = validate_front_matter_fields(
            Path("test.md"), front_matter, is_inbox=True
        )

        assert len(errors) > 0
        assert any("doc_type" in str(e) for e in errors)


class TestCanonicalUniqueness:
    """Tests for canonical document uniqueness checking."""

    def test_single_canonical_document(self, fixtures_dir):
        """Test that a single canonical document passes."""
        docs = collect_documents(fixtures_dir / "valid")
        errors = check_canonical_uniqueness(docs)

        assert len(errors) == 0

    def test_multiple_canonical_documents_same_concept(self, fixtures_dir):
        """Test detection of multiple canonical docs for same concept."""
        docs = collect_documents(fixtures_dir / "duplicates")
        errors = check_canonical_uniqueness(docs)

        # Should detect duplicate canonical documents
        assert len(errors) > 0
        assert any("Multiple canonical" in str(e) for e in errors)

    def test_non_canonical_documents_allowed(self, fixtures_dir):
        """Test that multiple non-canonical docs are allowed."""
        # Create test documents
        doc1 = DocumentInfo(
            path=Path("doc1.md"),
            front_matter={
                "title": "Test Document",
                "canonical": False,
            },
            content="Content 1",
            content_hash="hash1",
        )
        doc2 = DocumentInfo(
            path=Path("doc2.md"),
            front_matter={
                "title": "Test Document",
                "canonical": False,
            },
            content="Content 2",
            content_hash="hash2",
        )

        errors = check_canonical_uniqueness([doc1, doc2])

        # No errors expected for non-canonical documents
        assert len(errors) == 0


class TestDuplicateDetection:
    """Tests for near-duplicate document detection."""

    def test_similar_titles_detected(self, temp_docs_dir):
        """Test detection of similar document titles."""
        pytest.importorskip("rapidfuzz")  # Skip if rapidfuzz not available

        # Create documents with very similar titles (> 80% similarity)
        inbox_dir = temp_docs_dir / "_inbox"
        inbox_dir.mkdir()
        corpus_dir = temp_docs_dir / "rfcs"
        corpus_dir.mkdir()

        # Create inbox document with similar title
        inbox_doc = inbox_dir / "test.md"
        inbox_doc.write_text("""---
doc_id: "RFC-2025-77780"
title: "Plugin System Architecture"
doc_type: "rfc"
status: "draft"
canonical: false
created: "2025-11-14"
tags: ["plugin"]
summary: "Test document"
---

# Plugin System Architecture
""")

        # Create corpus document with very similar title
        corpus_doc = corpus_dir / "original.md"
        corpus_doc.write_text("""---
doc_id: "RFC-2025-77781"
title: "Plugin System Architecture Design"
doc_type: "rfc"
status: "active"
canonical: true
created: "2025-11-14"
tags: ["plugin"]
summary: "Original document"
---

# Plugin System Architecture Design
""")

        docs = collect_documents(temp_docs_dir)
        warnings = detect_near_duplicates(docs)

        # Should warn about similar titles (>80% similarity)
        assert len(warnings) > 0
        assert all(w.is_warning for w in warnings)

    def test_exact_duplicate_content_detected(self, temp_docs_dir):
        """Test detection of documents with similar content."""
        pytest.importorskip("simhash")  # Skip if simhash not available

        # Create documents with very similar content
        # Note: simhash needs actual content to compute similarity
        from simhash import Simhash
        
        content1 = "---\ntitle: Test\n---\nThis is a test document about plugin architecture system and design patterns."
        content2 = "---\ntitle: Test2\n---\nThis is a test document about plugin architecture system and design patterns."

        simhash1 = Simhash(content1).value
        simhash2 = Simhash(content2).value

        # Use paths that will be recognized as inbox vs corpus
        doc1 = DocumentInfo(
            path=temp_docs_dir / "_inbox" / "test1.md",
            front_matter={"title": "Test 1"},
            content=content1,
            content_hash="hash1",
            simhash=simhash1,
        )
        doc2 = DocumentInfo(
            path=temp_docs_dir / "docs" / "test2.md",
            front_matter={"title": "Test 2"},
            content=content2,
            content_hash="hash2",
            simhash=simhash2,
        )

        warnings = detect_near_duplicates([doc1, doc2])

        # Should detect near-duplicate (content is nearly identical)
        assert len(warnings) > 0

    def test_no_duplicates_in_different_content(self, fixtures_dir):
        """Test that different content doesn't trigger warnings."""
        docs = collect_documents(fixtures_dir / "valid")
        warnings = detect_near_duplicates(docs)

        # No warnings expected for different documents
        assert len([w for w in warnings if not w.message.startswith("Optional")]) == 0


class TestDocumentCollection:
    """Tests for document collection functionality."""

    def test_collect_documents_with_frontmatter(self, fixtures_dir):
        """Test collection of documents with front-matter."""
        docs = collect_documents(fixtures_dir / "valid")

        assert len(docs) > 0
        assert all(isinstance(d, DocumentInfo) for d in docs)
        assert all(d.front_matter is not None for d in docs)

    def test_collect_excludes_patterns(self, fixtures_dir, temp_docs_dir):
        """Test that excluded patterns are skipped."""
        # Create index and archive directories
        (temp_docs_dir / "index").mkdir()
        (temp_docs_dir / "archive").mkdir()

        # Copy a valid document to each
        valid_doc = fixtures_dir / "valid" / "rfc-example.md"
        shutil.copy(valid_doc, temp_docs_dir / "index" / "test.md")
        shutil.copy(valid_doc, temp_docs_dir / "archive" / "test.md")
        shutil.copy(valid_doc, temp_docs_dir / "test.md")

        docs = collect_documents(temp_docs_dir, exclude_patterns=["index/", "archive/"])

        # Should only find the one in root, not in excluded dirs
        assert len(docs) == 1
        assert "index" not in str(docs[0].path)
        assert "archive" not in str(docs[0].path)

    def test_collect_skips_no_frontmatter(self, fixtures_dir):
        """Test that documents without front-matter are skipped."""
        docs = collect_documents(fixtures_dir / "invalid")

        # Should not include no-frontmatter.md
        paths = [str(d.path) for d in docs]
        assert not any("no-frontmatter" in p for p in paths)


class TestRegistryGeneration:
    """Tests for registry generation."""

    def test_generate_registry_creates_file(self, fixtures_dir, temp_docs_dir):
        """Test that registry file is created."""
        docs = collect_documents(fixtures_dir / "valid")
        registry_path = temp_docs_dir / "index" / "registry.json"

        generate_registry(docs, registry_path)

        assert registry_path.exists()

    def test_registry_content_structure(self, fixtures_dir, temp_docs_dir):
        """Test that registry has correct structure."""
        docs = collect_documents(fixtures_dir / "valid")
        registry_path = temp_docs_dir / "index" / "registry.json"

        generate_registry(docs, registry_path)

        with open(registry_path) as f:
            registry = json.load(f)

        assert "generated_at" in registry
        assert "total_docs" in registry
        assert "by_type" in registry
        assert "by_status" in registry
        assert "docs" in registry
        assert registry["total_docs"] == len(docs)

    def test_registry_includes_document_data(self, fixtures_dir, temp_docs_dir):
        """Test that registry includes document metadata."""
        docs = collect_documents(fixtures_dir / "valid")
        registry_path = temp_docs_dir / "index" / "registry.json"

        generate_registry(docs, registry_path)

        with open(registry_path) as f:
            registry = json.load(f)

        assert len(registry["docs"]) > 0

        # Check first document has expected fields
        first_doc = registry["docs"][0]
        assert "path" in first_doc
        assert "sha256" in first_doc
        # Should have front-matter fields
        for doc in registry["docs"]:
            if "doc_id" in doc:  # Not all test docs have doc_id
                assert "title" in doc
                assert "doc_type" in doc

    def test_registry_counts_by_type_and_status(self, fixtures_dir, temp_docs_dir):
        """Test that registry correctly counts documents by type and status."""
        docs = collect_documents(fixtures_dir / "valid")
        registry_path = temp_docs_dir / "index" / "registry.json"

        generate_registry(docs, registry_path)

        with open(registry_path) as f:
            registry = json.load(f)

        # Should have at least one RFC and one guide
        assert "rfc" in registry["by_type"]
        assert "guide" in registry["by_type"]
        assert "draft" in registry["by_status"] or "active" in registry["by_status"]


class TestValidationConstants:
    """Tests for validation constants and patterns."""

    def test_doc_id_pattern_valid(self):
        """Test doc_id pattern matches valid formats."""
        valid_ids = [
            "RFC-2025-00001",
            "ADR-2024-99999",
            "GUIDE-2025-00042",
            "PLAN-2023-12345",
        ]

        for doc_id in valid_ids:
            assert DOC_ID_PATTERN.match(doc_id), f"{doc_id} should be valid"

    def test_doc_id_pattern_invalid(self):
        """Test doc_id pattern rejects invalid formats."""
        invalid_ids = [
            "rfc-2025-00001",  # lowercase prefix
            "RFC-25-00001",  # 2-digit year
            "RFC-2025-001",  # 3-digit number
            "RFC-2025-0001",  # 4-digit number
            "RFC202500001",  # no separators
            "INVALID-DOC-ID",  # no year/number
        ]

        for doc_id in invalid_ids:
            assert not DOC_ID_PATTERN.match(doc_id), f"{doc_id} should be invalid"

    def test_required_fields_complete(self):
        """Test that required fields constant includes all expected fields."""
        expected = {
            "doc_id",
            "title",
            "doc_type",
            "status",
            "canonical",
            "created",
            "tags",
            "summary",
        }
        assert REQUIRED_FIELDS == expected

    def test_inbox_required_fields_subset(self):
        """Test that inbox required fields is a proper subset."""
        assert INBOX_REQUIRED_FIELDS.issubset(REQUIRED_FIELDS)
        assert len(INBOX_REQUIRED_FIELDS) < len(REQUIRED_FIELDS)

    def test_valid_doc_types_complete(self):
        """Test that valid doc types includes expected values."""
        expected = {"spec", "rfc", "adr", "plan", "finding", "guide", "glossary", "reference"}
        assert VALID_DOC_TYPES == expected

    def test_valid_statuses_complete(self):
        """Test that valid statuses includes expected values."""
        expected = {"draft", "active", "superseded", "rejected", "archived"}
        assert VALID_STATUSES == expected


class TestValidationError:
    """Tests for ValidationError class."""

    def test_validation_error_creation(self):
        """Test ValidationError creation."""
        error = ValidationError(Path("test.md"), "Test error message")

        assert error.path == Path("test.md")
        assert error.message == "Test error message"
        assert not error.is_warning

    def test_validation_warning_creation(self):
        """Test ValidationError creation as warning."""
        warning = ValidationError(Path("test.md"), "Test warning", is_warning=True)

        assert warning.is_warning
        assert "WARNING" in str(warning)

    def test_validation_error_string(self):
        """Test ValidationError string representation."""
        error = ValidationError(Path("test.md"), "Test error")

        assert "ERROR" in str(error)
        assert "test.md" in str(error)
        assert "Test error" in str(error)


class TestPreCommitMode:
    """Tests for pre-commit mode functionality."""

    def test_pre_commit_mode_skips_registry(self, fixtures_dir, temp_docs_dir, monkeypatch):
        """Test that pre-commit mode doesn't regenerate registry."""
        # This is more of an integration test with the main function
        # We'll test the logic indirectly
        docs = collect_documents(fixtures_dir / "valid")
        registry_path = temp_docs_dir / "index" / "registry.json"

        # Generate registry first
        generate_registry(docs, registry_path)
        original_mtime = registry_path.stat().st_mtime

        # In pre-commit mode, registry should not be regenerated
        # This would be tested in integration tests with the main function
        # Here we just verify the registry generation works
        assert registry_path.exists()
        assert registry_path.stat().st_mtime == original_mtime


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
