# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Public repository files (CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md, CHANGELOG.md)
- GitHub issue templates
- GitHub pull request template
- Badges to README.md (License, Pre-commit, CI workflows)

## [0.1.0] - 2025-01-01

### Added

- Initial project structure with multi-language support (.NET, Python, JavaScript)
- Pre-commit hooks for code quality and security
  - .NET code formatting with `dotnet format`
  - Python formatting with `black` and `isort`
  - Python linting with `flake8`
  - JavaScript/TypeScript formatting with `prettier`
  - YAML validation with `yamllint`
  - JSON validation and formatting
  - Secret detection with `gitleaks` and `detect-secrets`
  - General file checks (trailing whitespace, EOF, large files, merge conflicts)
- Agent infrastructure for autonomous coding agents
  - Orchestrator for routing tasks to specialized sub-agents
  - Specialized sub-agents (build, testing, code review)
  - Skills system with progressive disclosure
  - JSON Schema validation for all manifests
  - Policy guardrails and coding standards
- GitHub Actions workflows
  - Pre-commit CI workflow
  - Benchmarks workflow
  - Console visual tests workflow
- Documentation
  - README.md with setup and usage instructions
  - AGENTS.md with agent infrastructure overview
  - CLAUDE.md with Claude-specific agent configuration
  - Agent rules, commands, workflows, and adapters in `.agent/` directory
  - Script documentation in `scripts/README.md`
- Configuration files
  - `.pre-commit-config.yaml` for pre-commit hooks
  - `.prettierrc.json` for Prettier formatting
  - `.editorconfig` for consistent editor settings
  - `.gitignore` for version control
  - `.gitleaksignore` and `.secrets.baseline` for secret detection
  - `Taskfile.yml` for task automation
  - `requirements/dev.txt` for Python development dependencies
- Setup scripts
  - `scripts/setup-pre-commit.sh` for Unix/Linux/macOS
  - `scripts/setup-pre-commit.ps1` for Windows PowerShell
- MIT License

[Unreleased]: https://github.com/GiantCroissant-Lunar/pigeon-pea/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/GiantCroissant-Lunar/pigeon-pea/releases/tag/v0.1.0
