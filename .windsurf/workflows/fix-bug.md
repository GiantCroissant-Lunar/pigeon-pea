# Fix Bug

Systematic bug investigation and fixing workflow

## Steps

### 1. Reproduce

**Build and run**

```bash
task game:build-and-run-console
```

**Observe issue**

Reproduce the bug
Capture error messages, stack traces, or unexpected behavior
Note exact steps to reproduce

### 2. Investigate

**Analyze errors**

Read stack traces carefully
Identify failing component
Extract file paths and line numbers

**Read code**

Open files mentioned in errors
Read surrounding context
Understand code flow leading to bug

**Identify root cause**

Trace issue back to source
Distinguish symptoms from root cause
Check for related issues

### 3. Fix

**Implement fix**

Make minimal, targeted changes
Fix root cause, not symptoms
Ensure fix doesn't introduce new issues

**Add regression test**

Create test that would catch this bug
Verify test fails before fix
Verify test passes after fix

### 4. Verify

**Rebuild**

```bash
task game:build-console
```

**Run tests**

```bash
task dotnet:test
```

**Run application**

Verify bug is fixed

```bash
task game:run-console
```

### 5. Finalize

**Quality checks**

```bash
task dotnet:format
pre-commit run --all-files

```

**Commit fix**

```bash
git add .
git commit -m "fix: {{ bug_description }}"

```

**Report**

Provide a summary:

```
Bug: {{ bug_description }}
Root cause: {{ root_cause }}
Fix: {{ fix_description }}
Files changed: {{ files_changed }}
Test added: {{ regression_test }}
```

---

_Generated from `.agent/workflows/fix-bug.yaml`_

To modify this workflow, edit the canonical YAML file and regenerate.
