# Claude Agent Configuration

This document describes Claude-specific agent configuration for the pigeon-pea project.

## Overview

This file provides guidance for Claude AI agents working with this codebase, including specific rules, workflows, and best practices.

## Agent Rules

Claude agents should follow the rules defined in the [`.agent/rules/`](.agent/rules/) directory.

### Git and Commit Rules (CRITICAL)

**ALL COMMITS MUST PASS PRE-COMMIT HOOKS** - See [`.agent/rules/git-commit-rules.md`](.agent/rules/git-commit-rules.md) for complete requirements.

Key requirements:

- Run `pre-commit run --all-files` before every commit
- Never use `--no-verify` to skip hooks
- Fix all formatting, security, and syntax issues
- Follow conventional commit message format

### Code Quality Rules

See [`.agent/rules/code-quality.md`](.agent/rules/code-quality.md) for detailed code quality standards including:

- Formatting requirements
- Documentation standards
- Testing expectations
- Security guidelines

## Available Commands

Claude can execute commands defined in [`.agent/commands/`](.agent/commands/).

### Run Tests

Use the `run-tests` command to run tests across all supported languages. This command is defined in [`.agent/commands/run-tests.yaml`](.agent/commands/run-tests.yaml) and will automatically:

- Detect which tests to run based on the files present in the project
- Install necessary dependencies before running tests
- Execute tests for .NET, Python, and JavaScript as applicable

## Workflows

Follow the workflows defined in [`.agent/workflows/`](.agent/workflows/) for structured development processes.

### Feature Development

When developing new features, follow the workflow in [`.agent/workflows/feature-development.yaml`](.agent/workflows/feature-development.yaml):

1. **Planning**
   - Analyze requirements
   - Review existing code
   - Create task breakdown
   - Identify potential risks

2. **Implementation**
   - Create feature branch (format: `claude/<description>-<session-id>`)
   - Implement core functionality
   - Write unit tests
   - Update documentation

3. **Quality Checks**
   - Run pre-commit hooks: `pre-commit run --all-files`
   - Execute test suite
   - Perform self-review
   - Check security implications

4. **Integration**
   - Merge latest changes from main
   - Resolve conflicts if any
   - Run integration tests
   - Create pull request

5. **Completion**
   - Address review feedback
   - Ensure CI/CD passes
   - Merge to main branch (when approved)
   - Tag release if applicable

## External System Integration

Claude can use adapters defined in [`.agent/adapters/`](.agent/adapters/) to interact with external systems.

### GitHub Integration

The GitHub adapter ([`.agent/adapters/github-adapter.yaml`](.agent/adapters/github-adapter.yaml)) provides capabilities for:

- Creating and managing issues
- Creating and reviewing pull requests
- Checking commit status
- Interacting with the GitHub API

## Project-Specific Guidelines

### Multi-Language Support

This project supports multiple languages. When working with code:

- **.NET (C#):** Located in `./dotnet` directory
  - Use `dotnet format` for formatting
  - Run tests with `dotnet test`

- **Python:**
  - Use `black` and `isort` for formatting
  - Use `flake8` for linting
  - Run tests with `pytest` (if configured)

- **JavaScript/TypeScript:**
  - Use `prettier` for formatting
  - Configuration in [`.prettierrc.json`](.prettierrc.json)

### Pre-commit Hooks

**CRITICAL:** Always ensure pre-commit hooks pass before committing. See [`.agent/rules/git-commit-rules.md`](.agent/rules/git-commit-rules.md) for detailed requirements.

Configuration is in [`.pre-commit-config.yaml`](.pre-commit-config.yaml).

Run hooks manually:

```bash
pre-commit run --all-files
```

### Security

- Never commit secrets or credentials
- All commits are checked by gitleaks and detect-secrets
- False positives can be added to [`.gitleaksignore`](.gitleaksignore) or [`.secrets.baseline`](.secrets.baseline)

### Git Workflow

- **Branch naming:** Use format `claude/<description>-<session-id>`
- **Commits:** Write clear, descriptive commit messages
- **Pushing:** Always use `git push -u origin <branch-name>`
- **Pull requests:** Include summary and test plan

### Error Handling

When encountering errors:

1. Read error messages carefully
2. Check relevant configuration files
3. Verify tool installations and versions
4. Consult documentation in `.agent/` directories
5. Ask for clarification if needed

## Task Management

When working on complex tasks:

1. Break down into smaller, manageable steps
2. Follow the appropriate workflow from [`.agent/workflows/`](.agent/workflows/)
3. Track progress systematically
4. Document decisions and changes
5. Ensure all quality checks pass

## Best Practices for Claude

1. **Read before writing:** Always read files before editing them
2. **Follow existing patterns:** Maintain consistency with existing code
3. **Use relative paths:** Reference files using relative paths as shown in this document
4. **Respect the structure:** Keep agent configurations in [`.agent/`](.agent/) directory
5. **Test thoroughly:** Run pre-commit hooks and tests before committing
6. **Document changes:** Update relevant documentation when making changes
7. **Ask when uncertain:** Request clarification rather than making assumptions

## Resources

- [AGENTS.md](AGENTS.md) - Complete agent infrastructure documentation
- [README.md](README.md) - Project setup and general information
- [`.agent/rules/`](.agent/rules/) - All agent rules
- [`.agent/commands/`](.agent/commands/) - Available commands
- [`.agent/workflows/`](.agent/workflows/) - Process workflows
- [`.agent/adapters/`](.agent/adapters/) - External system adapters
