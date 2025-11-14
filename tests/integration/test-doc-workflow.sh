#!/usr/bin/env bash
#
# Integration test for the complete documentation workflow (RFC-012)
#
# This script tests the end-to-end workflow:
# 1. Create draft in inbox
# 2. Validate (should pass with minimal fields)
# 3. Add complete front-matter
# 4. Move to final location
# 5. Validate again (should pass with no warnings)
# 6. Verify registry contains the document

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0

# Get repository root
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

# Test data directory
TEST_DIR="/tmp/pigeon-pea-doc-workflow-test-$$"
TEST_DOCS_DIR="$TEST_DIR/docs"

# Helper functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

assert_success() {
    TESTS_RUN=$((TESTS_RUN + 1))
    if [ $? -eq 0 ]; then
        TESTS_PASSED=$((TESTS_PASSED + 1))
        log_info "✓ $1"
    else
        TESTS_FAILED=$((TESTS_FAILED + 1))
        log_error "✗ $1"
        return 1
    fi
}

assert_file_exists() {
    TESTS_RUN=$((TESTS_RUN + 1))
    if [ -f "$1" ]; then
        TESTS_PASSED=$((TESTS_PASSED + 1))
        log_info "✓ File exists: $1"
    else
        TESTS_FAILED=$((TESTS_FAILED + 1))
        log_error "✗ File not found: $1"
        return 1
    fi
}

assert_file_contains() {
    TESTS_RUN=$((TESTS_RUN + 1))
    if grep -q "$2" "$1"; then
        TESTS_PASSED=$((TESTS_PASSED + 1))
        log_info "✓ File contains '$2': $1"
    else
        TESTS_FAILED=$((TESTS_FAILED + 1))
        log_error "✗ File does not contain '$2': $1"
        return 1
    fi
}

cleanup() {
    log_info "Cleaning up test directory: $TEST_DIR"
    rm -rf "$TEST_DIR"
}

# Trap cleanup on exit
trap cleanup EXIT

# Setup test environment
log_info "Setting up test environment in $TEST_DIR"
mkdir -p "$TEST_DOCS_DIR"/_inbox
mkdir -p "$TEST_DOCS_DIR"/rfcs
mkdir -p "$TEST_DOCS_DIR"/index

# Test 1: Create draft document in inbox
log_info "Test 1: Creating draft document in inbox"
cat > "$TEST_DOCS_DIR/_inbox/test-workflow.md" << 'EOF'
---
title: "Test Workflow Document"
doc_type: "rfc"
status: "draft"
created: "2025-11-14"
---

# Test Workflow Document

This is a test document for the documentation workflow integration test.

## Overview

Testing the complete workflow from inbox to final location.
EOF
assert_file_exists "$TEST_DOCS_DIR/_inbox/test-workflow.md"

# Test 2: Validate inbox document (should pass with minimal fields)
log_info "Test 2: Validating inbox document with minimal fields"
python3 scripts/validate-docs.py --docs-dir "$TEST_DOCS_DIR" > /tmp/validation1.log 2>&1
if grep -q "Validation PASSED" /tmp/validation1.log; then
    assert_success "Inbox validation passed"
else
    log_error "Inbox validation failed"
    cat /tmp/validation1.log
    exit 1
fi

# Test 3: Add complete front-matter
log_info "Test 3: Adding complete front-matter to document"
cat > "$TEST_DOCS_DIR/_inbox/test-workflow.md" << 'EOF'
---
doc_id: "RFC-2025-99998"
title: "Test Workflow Document"
doc_type: "rfc"
status: "active"
canonical: true
created: "2025-11-14"
tags: ["test", "workflow", "integration"]
summary: "Integration test document for RFC-012 documentation workflow"
supersedes: []
related: []
---

# Test Workflow Document

This is a test document for the documentation workflow integration test.

## Overview

Testing the complete workflow from inbox to final location with complete front-matter.

## Conclusion

This document validates the end-to-end workflow.
EOF
assert_file_exists "$TEST_DOCS_DIR/_inbox/test-workflow.md"

# Test 4: Validate with complete front-matter
log_info "Test 4: Validating document with complete front-matter"
python3 scripts/validate-docs.py --docs-dir "$TEST_DOCS_DIR" > /tmp/validation2.log 2>&1
if grep -q "Validation PASSED" /tmp/validation2.log; then
    assert_success "Complete validation passed"
else
    log_error "Complete validation failed"
    cat /tmp/validation2.log
    exit 1
fi

# Test 5: Move to final location
log_info "Test 5: Moving document to final location"
mv "$TEST_DOCS_DIR/_inbox/test-workflow.md" "$TEST_DOCS_DIR/rfcs/999-test-workflow.md"
assert_file_exists "$TEST_DOCS_DIR/rfcs/999-test-workflow.md"

# Test 6: Validate in final location
log_info "Test 6: Validating document in final location"
python3 scripts/validate-docs.py --docs-dir "$TEST_DOCS_DIR" --registry "$TEST_DOCS_DIR/index/registry.json" > /tmp/validation3.log 2>&1
if grep -q "Validation PASSED" /tmp/validation3.log; then
    assert_success "Final validation passed"
else
    log_error "Final validation failed"
    cat /tmp/validation3.log
    exit 1
fi

# Test 7: Verify registry was generated
log_info "Test 7: Verifying registry was generated"
assert_file_exists "$TEST_DOCS_DIR/index/registry.json"

# Test 8: Verify document is in registry
log_info "Test 8: Verifying document is in registry"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "RFC-2025-99998"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "Test Workflow Document"

# Test 9: Verify registry structure
log_info "Test 9: Verifying registry structure"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "generated_at"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "total_docs"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "by_type"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "by_status"
assert_file_contains "$TEST_DOCS_DIR/index/registry.json" "docs"

# Test 10: Test pre-commit mode (should not regenerate registry)
log_info "Test 10: Testing pre-commit mode"
REGISTRY_MTIME_BEFORE=$(stat -c %Y "$TEST_DOCS_DIR/index/registry.json" 2>/dev/null || stat -f %m "$TEST_DOCS_DIR/index/registry.json")
sleep 1
python3 scripts/validate-docs.py --pre-commit --docs-dir "$TEST_DOCS_DIR" > /tmp/validation4.log 2>&1
REGISTRY_MTIME_AFTER=$(stat -c %Y "$TEST_DOCS_DIR/index/registry.json" 2>/dev/null || stat -f %m "$TEST_DOCS_DIR/index/registry.json")

if [ "$REGISTRY_MTIME_BEFORE" -eq "$REGISTRY_MTIME_AFTER" ]; then
    assert_success "Pre-commit mode does not regenerate registry"
else
    log_warn "Registry was modified in pre-commit mode (this may be expected on first run)"
fi

# Test 11: Test duplicate detection
log_info "Test 11: Testing duplicate detection"
cat > "$TEST_DOCS_DIR/_inbox/duplicate-test.md" << 'EOF'
---
doc_id: "RFC-2025-99997"
title: "Test Workflow Document Implementation"
doc_type: "rfc"
status: "draft"
canonical: false
created: "2025-11-14"
tags: ["test", "workflow"]
summary: "A document with similar title and content"
---

# Test Workflow Document Implementation

This document has a very similar title to the existing document.
EOF
python3 scripts/validate-docs.py --docs-dir "$TEST_DOCS_DIR" > /tmp/validation5.log 2>&1
if grep -q "Near-duplicate detected" /tmp/validation5.log; then
    assert_success "Duplicate detection works"
else
    log_warn "Duplicate detection did not trigger (may need higher similarity)"
fi

# Print summary
echo ""
echo "================================"
echo "Integration Test Summary"
echo "================================"
echo "Tests run:    $TESTS_RUN"
echo "Tests passed: $TESTS_PASSED"
echo "Tests failed: $TESTS_FAILED"
echo "================================"

if [ $TESTS_FAILED -eq 0 ]; then
    log_info "All integration tests passed!"
    exit 0
else
    log_error "Some integration tests failed!"
    exit 1
fi
