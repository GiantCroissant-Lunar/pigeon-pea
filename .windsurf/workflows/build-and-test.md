# Build And Test

Autonomously build, test, and fix the application

## Steps

### 1. Build

**Build console application**

Build the console app and capture output

```bash
task game:build-console
```

**Analyze build errors (if build_failed)**

Read error messages carefully
Extract file paths and line numbers
Open relevant files
Identify and fix issues
Rebuild until successful

### 2. Test

**Run test suite**

Execute all tests

```bash
task dotnet:test
```

**Fix test failures (if tests_failed)**

For each failing test:

- Analyze failure message
- Identify root cause
- Fix implementation or test
- Rerun tests until all pass

### 3. Verify

**Run application**

Launch app to verify runtime behavior

```bash
task game:run-console
```

**Fix runtime issues (if runtime_errors)**

Capture stack traces and errors
Identify problematic code
Fix issues
Rebuild and rerun
Verify successful execution

### 4. Report

**Summary**

Provide a summary:

```
Build: {{ build_status }}
Tests: {{ test_results }}
Runtime: {{ runtime_status }}
Issues Fixed: {{ issues_fixed }}
Status: {{ final_status }}
```

---

_Generated from `.agent/workflows/build-and-test.yaml`_

To modify this workflow, edit the canonical YAML file and regenerate.
