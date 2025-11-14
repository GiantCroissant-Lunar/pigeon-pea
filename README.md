# pigeon-pea

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Pre-Commit](https://img.shields.io/badge/pre--commit-enabled-brightgreen?logo=pre-commit)](https://github.com/pre-commit/pre-commit)
[![Pre-commit CI](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/pre-commit.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/pre-commit.yml)
[![Benchmarks](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/benchmarks.yml)
[![Console Visual Tests](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/console-visual-tests.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/console-visual-tests.yml)

A multi-language project with pre-commit hooks for code quality and security.

## Pre-commit Hooks

This project uses [pre-commit](https://pre-commit.com/) to maintain code quality and prevent secrets from being committed. The hooks automatically check and format code in multiple languages.

### Supported Languages and Checks

- **.NET (C#)**: Code formatting using `dotnet format`
- **Python**: Code formatting (black, isort) and linting (flake8)
- **JavaScript/TypeScript/Node.js**: Code formatting (prettier)
- **YAML**: Validation and linting (yamllint)
- **JSON**: Validation and formatting (prettier)
- **Documentation**: Front-matter validation, duplicate detection (docs/\*.md)
- **Agent Infrastructure**: Validation of agent manifests and skills
- **Security**: Secret detection (gitleaks, detect-secrets)
- **General**: Trailing whitespace, EOF fixes, large file detection, merge conflict detection

### Installation

#### Prerequisites

Install the required tools based on the languages you're using:

```bash
# Python (required for pre-commit framework)
pip install pre-commit

# Development dependencies (required for agent validation scripts)
pip install -r requirements-dev.txt

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

**Agent validation fails:**

- Ensure development dependencies are installed: `pip install -r requirements-dev.txt`
- Check `.agent/schemas/` for schema definitions
- See `scripts/README.md` for validation details

For more information, visit: https://pre-commit.com/

## Agent Infrastructure

This project uses an agent infrastructure for autonomous coding agents (GitHub Copilot, Claude Code, etc.) to perform development tasks.

### Architecture

- **Orchestrator**: Routes tasks to specialized sub-agents based on intent
- **Sub-Agents**: Specialized agents for build, testing, code review
- **Skills**: Atomic capabilities with progressive disclosure (entry ~200 lines + references)
- **Schemas**: JSON Schema validation for all manifests
- **Policies**: Guardrails and coding standards

### Key Features

- **Progressive Disclosure**: Skills have compact entry files (~128-164 lines) that route to detailed references (200-320 lines each)
- **Cold-Start Optimization**: Entry + first reference ≤ 500 lines for optimal LLM token usage
- **Cross-Validation**: Agents reference real skills, orchestrator references real sub-agents
- **Automated Registry**: `AGENTS.md` auto-generated from manifests

### Usage

```bash
# Validate all agent manifests
task agents:validate

# Validate all skills
task skills:validate

# Run all validations and regenerate registry
task check
```

For detailed documentation, see:

- [AGENTS.md](AGENTS.md) - Complete agent infrastructure overview
- [CLAUDE.md](CLAUDE.md) - Claude-specific agent configuration
- [`.agent/`](.agent/) - Agent manifests, skills, schemas, and policies
- [`scripts/README.md`](scripts/README.md) - Validation script documentation
