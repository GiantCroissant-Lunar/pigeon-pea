# Pull Request Creation Workflow

Create a well-formatted pull request with proper testing and documentation.

## 1. Pre-PR Checks

### Ensure on Feature Branch

```bash
git branch --show-current
```

Verify you're not on `main` branch.

### Sync with Main

```bash
git fetch origin main
git merge origin/main
```

Resolve any conflicts if they arise.

## 2. Quality Verification

### Format Code

```bash
task dotnet:format
```

### Run Pre-commit Hooks

```bash
pre-commit run --all-files
```

Fix any issues.

### Build and Test

```bash
task game:build-console
task dotnet:test
```

Ensure everything passes.

### Run Application

```bash
task game:run-console
```

Verify the application works.

## 3. Review Changes

### Check All Changes

```bash
git diff main...HEAD
```

Review all changes that will be in the PR.

### List Changed Files

```bash
git diff main...HEAD --name-only
```

### Review Commit History

```bash
git log main..HEAD --oneline
```

Ensure commits are clean and meaningful.

## 4. Push Branch

```bash
git push -u origin $(git branch --show-current)
```

## 5. Analyze Changes for PR Description

### Identify Change Type

Determine if this is:

- New feature
- Bug fix
- Refactoring
- Documentation
- Performance improvement
- Other

### Summarize Changes

- What was changed?
- Why was it changed?
- What problem does it solve?
- What are the key implementation details?

## 6. Create Pull Request

```bash
gh pr create --title "<type>: <brief description>" --body "$(cat <<'EOF'
## Summary

<Concise summary of what this PR does>

## Motivation

<Why this change is needed>

## Changes Made

- <List key changes>
- <Include file paths for major changes>
- <Note any breaking changes>

## Testing

### Manual Testing
- <Steps taken to test the changes>
- <Test scenarios covered>

### Automated Testing
- <New tests added>
- <Modified tests>
- All existing tests pass: ✅

## Screenshots/Demos (if applicable)

<Screenshots or demo of the changes>

## Checklist

- [x] Code builds successfully
- [x] All tests pass
- [x] Code formatted with `task dotnet:format`
- [x] Pre-commit hooks pass
- [x] Documentation updated (if needed)
- [x] Application runs without errors

## Related Issues

Closes #<issue-number> (if applicable)

## Breaking Changes

<List any breaking changes, or "None">

## Additional Notes

<Any additional context or notes for reviewers>

---

🤖 Generated with Windsurf

Co-Authored-By: Windsurf <noreply@codeium.com>
EOF
)"
```

## 7. Verify PR Created

```bash
gh pr view
```

Check the PR details.

## 8. Report Results

Provide summary:

- Branch: [branch-name]
- PR Title: [title]
- PR URL: [url]
- Commits included: [count]
- Files changed: [count]
- Status: Ready for review
