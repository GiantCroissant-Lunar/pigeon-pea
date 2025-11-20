# Commit

Autonomous commit workflow with full verification

## Steps

### 1. Prepare

**Check status**

```bash
git status
```

**Format code**

```bash
task dotnet:format
```

**Run pre-commit hooks**

Fix any issues automatically

```bash
pre-commit run --all-files
```

### 2. Verify

**Build**

Build and fix any errors

```bash
task game:build-console
```

If errors occur, fix them autonomously and retry.

**Test**

Run tests and fix any failures

```bash
task dotnet:test
```

If errors occur, fix them autonomously and retry.

**Run**

Verify application works

```bash
task game:run-console
```

### 3. Commit

**Review changes**

```bash
git diff
```

**Create commit message**

Analyze changes and create message using conventional commits:

- feat: new features
- fix: bug fixes
- refactor: code refactoring
- docs: documentation
- test: tests
- chore: maintenance

Format:
<type>: <brief description>

<detailed explanation>

🤖 Generated with {{ platform }}
Co-Authored-By: {{ platform }} <{{ email }}>

**Stage and commit**

```bash
git add .
git commit -m "{{ commit_message }}"

```

**Verify commit**

```bash
git log -1
git show HEAD

```

### 4. Report

**Summary**

Provide a summary:

```
Committed: {{ commit_description }}
Files changed: {{ files_count }}
Hash: {{ commit_hash }}
Ready to push: yes
```

---

_Generated from `.agent/workflows/commit.yaml`_

To modify this workflow, edit the canonical YAML file and regenerate.
