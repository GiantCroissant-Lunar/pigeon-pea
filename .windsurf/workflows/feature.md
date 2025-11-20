# Feature Development Workflow

Complete workflow for developing a new feature from planning to pull request.

## 1. Planning Phase

### Analyze Requirements

- Review the feature requirements with the user if unclear
- Understand the expected behavior and scope
- Identify which parts of the codebase are affected

### Review Existing Code

- Read relevant files in the affected areas
- Understand the current architecture and patterns
- Identify where the feature should be integrated

### Create Task Breakdown

- Break down the feature into specific, implementable tasks
- Identify dependencies between tasks
- Estimate complexity and potential challenges

### Identify Risks

- Consider edge cases
- Check for breaking changes
- Identify integration points that might cause issues

## 2. Implementation Phase

### Create Feature Branch

```bash
git checkout -b feature/<feature-name>
```

### Implement Core Functionality

- Write the core code following existing patterns
- Ensure code quality and readability
- Add appropriate error handling
- Follow the project's coding standards

### Write Unit Tests

- Create tests for new functionality
- Aim for meaningful coverage
- Test edge cases and error conditions
- Ensure tests are clear and maintainable

### Update Documentation

- Update relevant documentation in `docs/`
- Add code comments for complex logic
- Update README if public API changes

## 3. Quality Checks

### Format Code

```bash
task dotnet:format
```

### Run Pre-commit Hooks

```bash
pre-commit run --all-files
```

Fix any issues found by the hooks.

### Build and Test

```bash
task game:build-console
task dotnet:test
```

Fix any build errors or test failures autonomously.

### Run the Application

```bash
task game:run-console
```

Verify the feature works as expected.

### Self-Review

- Review all changes made
- Check for code quality issues
- Ensure no debugging code left behind
- Verify security implications

## 4. Integration Phase

### Merge Latest Changes

```bash
git fetch origin main
git merge origin/main
```

Resolve any conflicts if they arise.

### Run Tests Again

```bash
task dotnet:test
```

Ensure integration didn't break anything.

### Create Pull Request

```bash
gh pr create --title "feat: <feature-name>" --body "$(cat <<'EOF'
## Summary
- Brief description of the feature
- What problem it solves

## Changes
- List of key changes made

## Testing
- How to test the feature
- Test scenarios covered

## Checklist
- [x] Tests added/updated
- [x] Documentation updated
- [x] Pre-commit hooks pass
- [x] Application runs successfully

🤖 Generated with Windsurf
EOF
)"
```

## 5. Final Report

Provide summary:

- Feature implemented: [description]
- Files changed: [count]
- Tests added: [count]
- PR created: [URL]
- Status: Ready for review
