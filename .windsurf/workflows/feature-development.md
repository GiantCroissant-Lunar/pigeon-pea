# Feature Development

Standard workflow for developing a new feature

## Steps

### 1. Planning

**Analyze requirements**

Review and understand the feature requirements

**Review existing code**

Examine relevant existing code and architecture

**Create task breakdown**

Break down the feature into implementable tasks

**Identify potential risks**

Consider edge cases, dependencies, and potential issues

### 2. Implementation

**Create feature branch**

```bash
git checkout -b claude/<description>-<session-id>
```

**Implement core functionality**

Write the core code for the feature

**Write unit tests**

Create tests for the new functionality

**Update documentation**

Update relevant documentation files

### 3. Quality Checks

**Run pre-commit hooks**

```bash
pre-commit run --all-files
```

**Execute test suite**

```bash
run-tests
```

**Perform code review**

Perform a self-review of the changes or request a peer review

**Check security implications**

Review changes for security vulnerabilities

### 4. Integration

**Merge latest changes from main**

```bash
git fetch origin main && git merge origin/main
```

**Resolve conflicts if any**

Resolve any merge conflicts that arise

**Run integration tests**

```bash
run-tests
```

**Create pull request**

```bash
gh pr create --title "<title>" --body "<body>"
```

### 5. Completion

**Address review feedback**

Make changes based on code review feedback

**Ensure CI/CD passes**

Verify all CI/CD checks are passing

**Merge to main branch**

Merge the pull request (requires approval)

**Tag release if applicable**

```bash
git tag -a v<version> -m "<message>"
```

---

*Generated from `.agent/workflows/feature-development.yaml`*

To modify this workflow, edit the canonical YAML file and regenerate.