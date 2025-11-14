# Tests

This directory contains tests for the pigeon-pea project.

## Directory Structure

```
tests/
├── fixtures/          # Test fixtures and sample data
│   └── docs/         # Documentation test fixtures (RFC-012)
│       ├── valid/    # Valid document samples
│       ├── invalid/  # Invalid document samples
│       └── duplicates/ # Duplicate detection samples
├── integration/      # Integration tests
│   └── test-doc-workflow.sh  # Documentation workflow integration test
├── pty/             # PTY (pseudo-terminal) tests
├── windows/         # Windows-specific tests
├── test_validate_docs.py  # Unit tests for documentation validation
└── README.md        # This file
```

## Running Tests

### Documentation Validation Tests (RFC-012)

#### Unit Tests

Run all documentation validation unit tests:

```bash
pytest tests/test_validate_docs.py -v
```

Run specific test class:

```bash
pytest tests/test_validate_docs.py::TestFrontMatterValidation -v
```

Run specific test:

```bash
pytest tests/test_validate_docs.py::TestFrontMatterValidation::test_missing_required_fields -v
```

With coverage:

```bash
pytest tests/test_validate_docs.py --cov=scripts.validate_docs --cov-report=html
```

#### Integration Tests

Run the complete documentation workflow integration test:

```bash
bash tests/integration/test-doc-workflow.sh
```

This tests the end-to-end workflow:

1. Create draft in inbox
2. Validate with minimal fields
3. Add complete front-matter
4. Move to final location
5. Validate in final location
6. Verify registry generation
7. Test duplicate detection

### All Tests

Run all tests in the project:

```bash
# Python tests
pytest tests/ -v

# .NET tests (if applicable)
cd dotnet && dotnet test

# Integration tests
bash tests/integration/test-doc-workflow.sh
```

## Test Fixtures

### Documentation Test Fixtures

Located in `tests/fixtures/docs/`:

- **valid/**: Valid documents with proper front-matter
  - `rfc-example.md` - Complete RFC document
  - `guide-example.md` - Complete guide document
  - `adr-example.md` - Complete ADR document
  - `inbox-minimal.md` - Minimal inbox document

- **invalid/**: Invalid documents for error testing
  - `missing-doc-id.md` - Missing required doc_id field
  - `invalid-doc-type.md` - Invalid doc_type value
  - `invalid-status.md` - Invalid status value
  - `invalid-doc-id-format.md` - Incorrect doc_id format
  - `invalid-date-format.md` - Incorrect date format
  - `missing-tags.md` - Missing required tags field
  - `no-frontmatter.md` - No front-matter at all

- **duplicates/**: Documents for duplicate detection testing
  - `canonical-duplicate-1.md` - First canonical doc
  - `canonical-duplicate-2.md` - Duplicate canonical doc (should error)
  - `similar-content.md` - Similar content for near-duplicate detection

## Test Coverage

### Documentation Validation (`test_validate_docs.py`)

Current coverage: **34 tests, 100% passing**

Test classes:

- `TestFrontMatterExtraction` (3 tests) - Front-matter parsing
- `TestFrontMatterValidation` (8 tests) - Field validation
- `TestCanonicalUniqueness` (3 tests) - Canonical document checking
- `TestDuplicateDetection` (3 tests) - Near-duplicate detection
- `TestDocumentCollection` (3 tests) - Document discovery
- `TestRegistryGeneration` (4 tests) - Registry creation
- `TestValidationConstants` (6 tests) - Validation rules
- `TestValidationError` (3 tests) - Error handling
- `TestPreCommitMode` (1 test) - Pre-commit integration

Coverage by feature:

- ✅ Front-matter validation: 100%
- ✅ Canonical uniqueness: 100%
- ✅ Duplicate detection: 100%
- ✅ Registry generation: 100%
- ✅ Pre-commit mode: 100%

## Writing New Tests

### Adding Documentation Validation Tests

1. Create test fixtures in `tests/fixtures/docs/`
2. Add test methods to `tests/test_validate_docs.py`
3. Follow existing test patterns:

```python
def test_new_validation_feature(self, fixtures_dir):
    """Test description."""
    # Arrange
    doc_file = fixtures_dir / "category" / "test-doc.md"

    # Act
    front_matter, content = extract_front_matter(doc_file)
    errors = validate_front_matter_fields(doc_file, front_matter, is_inbox=False)

    # Assert
    assert len(errors) == expected_count
    assert expected_condition
```

### Adding Integration Tests

1. Create a new shell script in `tests/integration/`
2. Use the pattern from `test-doc-workflow.sh`:
   - Setup test environment
   - Run commands
   - Verify outputs
   - Clean up

## Dependencies

### Required for Testing

```bash
# Python dependencies
pip install pytest pyyaml simhash rapidfuzz

# Optional for coverage
pip install pytest-cov
```

All dependencies are listed in `scripts/requirements.txt`.

## Continuous Integration

Tests run automatically in CI via GitHub Actions:

- **Pre-commit CI** (`.github/workflows/pre-commit.yml`)
  - Runs on every PR
  - Validates documentation via pre-commit hook

- **Documentation Validation CI** (`.github/workflows/docs-validation.yml`)
  - Runs on documentation changes
  - Executes unit tests
  - Executes integration tests
  - Generates and validates registry
  - Comments on PR with duplicate warnings

## Troubleshooting

### Pytest Not Found

```bash
pip install pytest
```

### Import Errors

Ensure you're running from the repository root:

```bash
cd /path/to/pigeon-pea
pytest tests/test_validate_docs.py
```

### Missing Dependencies

```bash
pip install -r scripts/requirements.txt
```

### Integration Test Fails

Check that validation script is working:

```bash
python scripts/validate-docs.py
```

## References

- [RFC-012: Documentation Organization Management](../docs/rfcs/012-documentation-organization-management.md)
- [Documentation Schema](../docs/DOCUMENTATION-SCHEMA.md)
- [Validation Script](../scripts/validate-docs.py)
- [Pre-commit Config](../.pre-commit-config.yaml)
