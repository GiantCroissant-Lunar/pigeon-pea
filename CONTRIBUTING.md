# Contributing to pigeon-pea

Thank you for your interest in contributing to pigeon-pea! We welcome contributions from the community.

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## How to Contribute

### Reporting Bugs

If you find a bug, please [create a bug report](https://github.com/GiantCroissant-Lunar/pigeon-pea/issues/new?template=bug_report.yml) with:

- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Your environment (OS, language versions, etc.)

### Suggesting Enhancements

We welcome suggestions for enhancements! Please [create a feature request](https://github.com/GiantCroissant-Lunar/pigeon-pea/issues/new?template=feature_request.yml) with:

- A clear, descriptive title
- Detailed description of the proposed enhancement
- Use cases and benefits
- Any potential drawbacks or considerations

### Pull Requests

1. **Fork the repository** and create your branch from the main branch
2. **Use proper branch naming**: `feature/your-feature-name` or `fix/your-bug-fix`
3. **Make your changes**:
   - Write clear, readable code
   - Follow the existing code style
   - Add tests if applicable
   - Update documentation as needed
4. **Run pre-commit hooks**: All commits must pass pre-commit checks
   ```bash
   pre-commit run --all-files
   ```
5. **Write a good commit message**: Follow [Conventional Commits](https://www.conventionalcommits.org/)
   - `feat:` for new features
   - `fix:` for bug fixes
   - `docs:` for documentation changes
   - `chore:` for maintenance tasks
   - `refactor:` for code refactoring
   - `test:` for test changes
6. **Push to your fork** and submit a pull request
7. **Ensure CI passes**: All GitHub Actions workflows must pass

### Development Setup

#### Prerequisites

Install the required tools based on the languages you're working with:

```bash
# Python (required for pre-commit framework)
pip install pre-commit

# Development dependencies
pip install -r requirements-dev.txt

# .NET SDK (for C# development)
# Download from: https://dotnet.microsoft.com/download

# Node.js (for JavaScript/TypeScript development)
# Download from: https://nodejs.org/
```

#### Install Pre-commit Hooks

```bash
# Install pre-commit hooks
pre-commit install

# Verify setup
pre-commit run --all-files
```

#### Running Tests

```bash
# Use the Taskfile
task check

# Or run tests manually
# .NET tests
dotnet test

# Python tests (if configured)
pytest

# Agent validation
task agents:validate
task skills:validate
```

## Development Guidelines

### Code Quality

- **Pre-commit hooks are mandatory**: Never use `--no-verify` to skip hooks
- **Follow language-specific conventions**:
  - **.NET**: Use `dotnet format` for formatting
  - **Python**: Use `black`, `isort`, and `flake8`
  - **JavaScript/TypeScript**: Use `prettier`
- **Write tests** for new features and bug fixes
- **Keep commits atomic**: One logical change per commit
- **Update documentation** when changing functionality

### Code Review Process

- All pull requests require review before merging
- Address review feedback promptly
- Keep pull requests focused and reasonably sized
- Be respectful and constructive in discussions

### Security

- **Never commit secrets** or credentials
- All commits are checked by `gitleaks` and `detect-secrets`
- Report security vulnerabilities privately (see [SECURITY.md](SECURITY.md))

## Agent Infrastructure

If you're working with the agent infrastructure:

- Read [AGENTS.md](AGENTS.md) for architecture overview
- Read [CLAUDE.md](CLAUDE.md) for Claude-specific guidelines
- Follow schemas defined in `.agent/schemas/`
- Validate manifests: `task agents:validate`
- Validate skills: `task skills:validate`

## Questions?

If you have questions, feel free to:

- Open an issue for discussion
- Check existing documentation in the repository
- Review [AGENTS.md](AGENTS.md) and [CLAUDE.md](CLAUDE.md) for agent-related questions

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

Thank you for contributing to pigeon-pea!
