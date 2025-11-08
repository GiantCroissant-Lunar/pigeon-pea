# pigeon-pea

A multi-language project with pre-commit hooks for code quality and security.

## Pre-commit Hooks

This project uses [pre-commit](https://pre-commit.com/) to maintain code quality and prevent secrets from being committed. The hooks automatically check and format code in multiple languages.

### Supported Languages and Checks

- **.NET (C#)**: Code formatting using `dotnet format`
- **Python**: Code formatting (black, isort) and linting (flake8)
- **JavaScript/TypeScript/Node.js**: Code formatting (prettier)
- **YAML**: Validation and linting (yamllint)
- **JSON**: Validation and formatting (prettier)
- **Security**: Secret detection (gitleaks, detect-secrets)
- **General**: Trailing whitespace, EOF fixes, large file detection, merge conflict detection

### Installation

#### Prerequisites

Install the required tools based on the languages you're using:

```bash
# Python (required for pre-commit framework)
pip install pre-commit

# .NET SDK (for C# formatting)
# Download from: https://dotnet.microsoft.com/download

# Node.js (for JavaScript/TypeScript formatting)
# Download from: https://nodejs.org/
```

#### Setup Pre-commit Hooks

1. Install pre-commit in your local repository:

```bash
pre-commit install
```

2. (Optional) Run against all files to verify setup:

```bash
pre-commit run --all-files
```

### Usage

Once installed, the pre-commit hooks will automatically run on every commit. If any check fails, the commit will be blocked and you'll need to fix the issues.

#### Manual Execution

Run hooks on all files:

```bash
pre-commit run --all-files
```

Run a specific hook:

```bash
pre-commit run <hook-id> --all-files
```

Skip hooks (not recommended):

```bash
git commit --no-verify
```

### Configuration Files

- `.pre-commit-config.yaml`: Main pre-commit configuration
- `.secrets.baseline`: Baseline file for detect-secrets
- `.prettierrc.json`: Prettier configuration for JavaScript/TypeScript/JSON/YAML
- `.editorconfig`: Editor configuration for consistent formatting across different editors

### Updating Hooks

Update all hooks to the latest version:

```bash
pre-commit autoupdate
```

### Troubleshooting

**Hook installation fails:**

- Ensure you have Python 3.6+ installed
- Try: `pip install --upgrade pre-commit`

**.NET format not working:**

- Ensure .NET SDK is installed and accessible in PATH
- Verify with: `dotnet --version`

**Gitleaks fails:**

- If it's a false positive, add the file/line to `.gitleaksignore`
- Or update the `.secrets.baseline` for detect-secrets

**Prettier/ESLint issues:**

- Ensure Node.js is installed
- Check `.prettierrc.json` for configuration

For more information, visit: https://pre-commit.com/
