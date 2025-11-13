#!/usr/bin/env python3
"""
Documentation validation script for RFC-012.

Validates front-matter, checks for duplicates, and generates the documentation registry.
"""
import argparse
import hashlib
import json
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import yaml

# Optional dependencies with graceful degradation
try:
    from simhash import Simhash

    SIMHASH_AVAILABLE = True
except ImportError:
    SIMHASH_AVAILABLE = False

try:
    from rapidfuzz import fuzz

    RAPIDFUZZ_AVAILABLE = True
except ImportError:
    RAPIDFUZZ_AVAILABLE = False

# Valid values for front-matter fields
VALID_DOC_TYPES = {
    "spec",
    "rfc",
    "adr",
    "plan",
    "finding",
    "guide",
    "glossary",
    "reference",
}
VALID_STATUSES = {"draft", "active", "superseded", "rejected", "archived"}

# Doc ID format: PREFIX-YYYY-NNNNN
DOC_ID_PATTERN = re.compile(r"^[A-Z]+-\d{4}-\d{5}$")

# ISO date format: YYYY-MM-DD
DATE_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}$")

# Required fields for documents outside of inbox
REQUIRED_FIELDS = {
    "doc_id",
    "title",
    "doc_type",
    "status",
    "canonical",
    "created",
    "tags",
    "summary",
}

# Minimal required fields for inbox documents
INBOX_REQUIRED_FIELDS = {"title", "doc_type", "status", "created"}


class ValidationError:
    """Represents a validation error or warning."""

    def __init__(self, path: Path, message: str, is_warning: bool = False):
        self.path = path
        self.message = message
        self.is_warning = is_warning

    def __str__(self):
        level = "WARNING" if self.is_warning else "ERROR"
        return f"[{level}] {self.path}: {self.message}"


class DocumentInfo:
    """Container for document information."""

    def __init__(
        self,
        path: Path,
        front_matter: Dict[str, Any],
        content: str,
        content_hash: str,
        simhash: Optional[int] = None,
    ):
        self.path = path
        self.front_matter = front_matter
        self.content = content
        self.content_hash = content_hash
        self.simhash = simhash


def extract_front_matter(file_path: Path) -> Tuple[Optional[Dict[str, Any]], str]:
    """
    Extract YAML front-matter from a markdown file.

    Returns:
        Tuple of (front_matter dict or None, full content string)
    """
    try:
        with open(file_path, encoding="utf-8") as f:
            content = f.read()

        if not content.startswith("---"):
            return None, content

        parts = content.split("---", 2)
        if len(parts) < 3:
            return None, content

        front_matter = yaml.safe_load(parts[1])
        return front_matter, content

    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return None, ""


def validate_front_matter_fields(
    doc_path: Path, front_matter: Dict[str, Any], is_inbox: bool
) -> List[ValidationError]:
    """Validate that all required fields are present and valid."""
    errors = []

    required = INBOX_REQUIRED_FIELDS if is_inbox else REQUIRED_FIELDS
    missing_fields = required - set(front_matter.keys())

    if missing_fields:
        errors.append(
            ValidationError(
                doc_path,
                f"Missing required fields: {', '.join(sorted(missing_fields))}",
            )
        )

    # Validate doc_type
    doc_type = front_matter.get("doc_type")
    if doc_type and doc_type not in VALID_DOC_TYPES:
        errors.append(
            ValidationError(
                doc_path,
                f"Invalid doc_type '{doc_type}'. "
                f"Must be one of: {', '.join(sorted(VALID_DOC_TYPES))}",
            )
        )

    # Validate status
    status = front_matter.get("status")
    if status and status not in VALID_STATUSES:
        errors.append(
            ValidationError(
                doc_path,
                f"Invalid status '{status}'. "
                f"Must be one of: {', '.join(sorted(VALID_STATUSES))}",
            )
        )

    # Validate doc_id format (if present and not in inbox)
    if not is_inbox:
        doc_id = front_matter.get("doc_id")
        if doc_id and not DOC_ID_PATTERN.match(str(doc_id)):
            errors.append(
                ValidationError(
                    doc_path,
                    f"Invalid doc_id format '{doc_id}'. "
                    "Expected format: PREFIX-YYYY-NNNNN (e.g., RFC-2025-00012)",
                )
            )

    # Validate date formats
    for date_field in ["created", "updated"]:
        date_value = front_matter.get(date_field)
        if date_value:
            date_str = str(date_value)
            if not DATE_PATTERN.match(date_str):
                errors.append(
                    ValidationError(
                        doc_path,
                        f"Invalid {date_field} format '{date_str}'. "
                        "Expected ISO format: YYYY-MM-DD",
                    )
                )
            else:
                # Verify it's a valid date
                try:
                    datetime.strptime(date_str, "%Y-%m-%d")
                except ValueError:
                    errors.append(
                        ValidationError(
                            doc_path,
                            f"Invalid {date_field} date '{date_str}'. "
                            "Date is not valid.",
                        )
                    )

    # Validate canonical is boolean
    canonical = front_matter.get("canonical")
    if canonical is not None and not isinstance(canonical, bool):
        errors.append(
            ValidationError(
                doc_path, f"Field 'canonical' must be boolean, got: {type(canonical)}"
            )
        )

    # Validate tags is a list
    tags = front_matter.get("tags")
    if tags is not None and not isinstance(tags, list):
        errors.append(
            ValidationError(doc_path, f"Field 'tags' must be a list, got: {type(tags)}")
        )

    return errors


def check_canonical_uniqueness(
    documents: List[DocumentInfo],
) -> List[ValidationError]:
    """
    Check that only one canonical document exists per concept.

    Uses normalized titles to identify concepts.
    """
    errors = []
    canonical_docs: Dict[str, List[Path]] = {}

    for doc in documents:
        if not doc.front_matter.get("canonical"):
            continue

        title = doc.front_matter.get("title", "")
        # Normalize title: lowercase, remove special chars, collapse spaces
        normalized = re.sub(r"[^a-z0-9\s]", "", title.lower())
        normalized = re.sub(r"\s+", " ", normalized).strip()

        if normalized not in canonical_docs:
            canonical_docs[normalized] = []
        canonical_docs[normalized].append(doc.path)

    # Report duplicates
    for concept, paths in canonical_docs.items():
        if len(paths) > 1:
            errors.append(
                ValidationError(
                    paths[0],
                    f"Multiple canonical documents for concept '{concept}': "
                    + ", ".join(str(p) for p in paths),
                )
            )

    return errors


def detect_near_duplicates(
    documents: List[DocumentInfo],
    hamming_threshold: int = 8,
    title_similarity: int = 80,
) -> List[ValidationError]:
    """
    Detect near-duplicate documents using SimHash and fuzzy title matching.

    Args:
        documents: List of documents to check
        hamming_threshold: Maximum Hamming distance for SimHash (default: 8)
        title_similarity: Minimum title similarity percentage (default: 80)
    """
    warnings = []

    if not SIMHASH_AVAILABLE and not RAPIDFUZZ_AVAILABLE:
        warnings.append(
            ValidationError(
                Path("(system)"),
                "Optional dependencies missing (simhash, rapidfuzz). "
                "Skipping duplicate detection.",
                is_warning=True,
            )
        )
        return warnings

    # Separate inbox docs from corpus
    inbox_docs = [d for d in documents if "/_inbox/" in str(d.path)]
    corpus_docs = [d for d in documents if "/_inbox/" not in str(d.path)]

    # Check for near-duplicates between inbox and corpus
    for inbox_doc in inbox_docs:
        inbox_title = inbox_doc.front_matter.get("title", "")

        for corpus_doc in corpus_docs:
            corpus_title = corpus_doc.front_matter.get("title", "")

            # Check title similarity
            title_sim = 0
            if RAPIDFUZZ_AVAILABLE and inbox_title and corpus_title:
                title_sim = fuzz.ratio(inbox_title.lower(), corpus_title.lower())

            # Check content similarity
            content_sim = 0
            hamming_dist = 999
            if (
                SIMHASH_AVAILABLE
                and inbox_doc.simhash is not None
                and corpus_doc.simhash is not None
            ):
                hamming_dist = bin(inbox_doc.simhash ^ corpus_doc.simhash).count("1")
                # Convert Hamming distance to similarity percentage
                content_sim = max(0, 100 - (hamming_dist * 100 // 64))

            # Report if either similarity is high
            if title_sim >= title_similarity or hamming_dist <= hamming_threshold:
                warnings.append(
                    ValidationError(
                        inbox_doc.path,
                        f"Near-duplicate detected:\n"
                        f"  Inbox:  {inbox_doc.path}\n"
                        f"  Corpus: {corpus_doc.path}\n"
                        f"  Title similarity: {title_sim:.0f}%, "
                        f"Content similarity: ~{content_sim:.0f}%",
                        is_warning=True,
                    )
                )
                break  # Only report first match per inbox doc

    return warnings


def collect_documents(
    docs_dir: Path, exclude_patterns: Optional[List[str]] = None
) -> List[DocumentInfo]:
    """
    Collect all markdown documents with front-matter.

    Args:
        docs_dir: Root documentation directory
        exclude_patterns: List of path patterns to exclude
    """
    if exclude_patterns is None:
        exclude_patterns = ["index/", "archive/"]

    documents = []

    for md_file in docs_dir.rglob("*.md"):
        # Skip excluded patterns
        rel_path = md_file.relative_to(docs_dir)
        if any(pattern in str(rel_path) for pattern in exclude_patterns):
            continue

        front_matter, content = extract_front_matter(md_file)
        if front_matter is None:
            continue

        # Calculate SHA256 hash of content
        content_hash = hashlib.sha256(content.encode("utf-8")).hexdigest()

        # Calculate SimHash if available
        simhash_value = None
        if SIMHASH_AVAILABLE:
            # Extract text content (remove front-matter and markdown syntax)
            text = content.split("---", 2)[-1] if "---" in content else content
            text = re.sub(r"[#*`\[\]()]", " ", text)
            text = re.sub(r"\s+", " ", text).strip()
            if text:
                simhash_value = Simhash(text).value

        documents.append(
            DocumentInfo(md_file, front_matter, content, content_hash, simhash_value)
        )

    return documents


def generate_registry(documents: List[DocumentInfo], output_path: Path) -> None:
    """Generate the documentation registry JSON file."""
    from datetime import timezone

    registry = {
        "generated_at": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "total_docs": len(documents),
        "by_type": {},
        "by_status": {},
        "docs": [],
    }

    # Count by type and status
    for doc in documents:
        doc_type = doc.front_matter.get("doc_type")
        status = doc.front_matter.get("status")

        if doc_type:
            registry["by_type"][doc_type] = registry["by_type"].get(doc_type, 0) + 1
        if status:
            registry["by_status"][status] = registry["by_status"].get(status, 0) + 1

    # Add document entries
    for doc in documents:
        doc_entry = {
            "path": str(doc.path),
            "sha256": doc.content_hash,
        }

        # Add all front-matter fields
        for key, value in doc.front_matter.items():
            doc_entry[key] = value

        # Add simhash if available
        if doc.simhash is not None:
            doc_entry["simhash"] = str(doc.simhash)

        registry["docs"].append(doc_entry)

    # Write registry
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(registry, f, indent=2, ensure_ascii=False)
        f.write("\n")  # Add final newline

    print(f"Registry generated: {output_path}")
    print(f"  Total documents: {registry['total_docs']}")
    print(f"  By type: {registry['by_type']}")
    print(f"  By status: {registry['by_status']}")


def main():
    """Main entry point for documentation validation."""
    parser = argparse.ArgumentParser(
        description="Validate documentation front-matter and generate registry"
    )
    parser.add_argument(
        "--pre-commit",
        action="store_true",
        help="Pre-commit mode: validation only, no registry regeneration",
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        help="Documentation directory (default: docs/)",
    )
    parser.add_argument(
        "--registry",
        type=Path,
        help="Registry output path (default: docs/index/registry.json)",
    )
    args = parser.parse_args()

    # Resolve paths relative to repo root
    repo_root = Path(__file__).parent.parent
    docs_dir = args.docs_dir or repo_root / "docs"
    registry_path = args.registry or repo_root / "docs" / "index" / "registry.json"

    if not docs_dir.exists():
        print(f"Error: Documentation directory not found: {docs_dir}")
        return 1

    print(f"Validating documentation in {docs_dir}...")
    if not SIMHASH_AVAILABLE:
        print("Note: simhash not installed. Install with: pip install simhash>=2.1.2")
    if not RAPIDFUZZ_AVAILABLE:
        print(
            "Note: rapidfuzz not installed. Install with: pip install rapidfuzz>=3.5.2"
        )

    # Collect all documents
    documents = collect_documents(docs_dir)
    print(f"Found {len(documents)} documents with front-matter")

    # Validate each document
    all_errors = []
    all_warnings = []

    for doc in documents:
        is_inbox = "/_inbox/" in str(doc.path)
        errors = validate_front_matter_fields(doc.path, doc.front_matter, is_inbox)
        all_errors.extend([e for e in errors if not e.is_warning])
        all_warnings.extend([e for e in errors if e.is_warning])

    # Check canonical uniqueness
    canonical_errors = check_canonical_uniqueness(documents)
    all_errors.extend([e for e in canonical_errors if not e.is_warning])
    all_warnings.extend([e for e in canonical_errors if e.is_warning])

    # Detect near-duplicates
    duplicate_warnings = detect_near_duplicates(documents)
    all_warnings.extend(duplicate_warnings)

    # Print errors and warnings
    if all_errors:
        print("\n=== ERRORS ===")
        for error in all_errors:
            print(f"[ERROR] {error.path}: {error.message}")

    if all_warnings:
        print("\n=== WARNINGS ===")
        for warning in all_warnings:
            print(f"[WARNING] {warning.path}: {warning.message}")

    # Generate registry (unless in pre-commit mode)
    if not args.pre_commit:
        if all_errors:
            print("\nSkipping registry generation due to validation errors")
        else:
            print("\nGenerating registry...")
            generate_registry(documents, registry_path)

    # Print summary
    print("\n=== SUMMARY ===")
    print(f"Documents validated: {len(documents)}")
    print(f"Errors: {len(all_errors)}")
    print(f"Warnings: {len(all_warnings)}")

    if all_errors:
        print("\nValidation FAILED - please fix errors above")
        return 1
    else:
        print("\nValidation PASSED")
        return 0


if __name__ == "__main__":
    sys.exit(main())
