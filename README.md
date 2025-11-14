# pigeon-pea

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Pre-Commit](https://img.shields.io/badge/pre--commit-enabled-brightgreen?logo=pre-commit)](https://github.com/pre-commit/pre-commit)
[![Pre-commit CI](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/pre-commit.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/pre-commit.yml)
[![Benchmarks](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/benchmarks.yml)
[![Console Visual Tests](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/console-visual-tests.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pigeon-pea/actions/workflows/console-visual-tests.yml)

A multi-language project with pre-commit hooks for code quality and security.

## Architecture

PigeonPea uses a **domain-driven architecture** with two primary domains:

- **Map Domain** (`dotnet/Map/`): World map generation, navigation, rendering
- **Dungeon Domain** (`dotnet/Dungeon/`): Dungeon exploration, FOV, pathfinding, rendering

Each domain follows a **Core / Control / Rendering** pattern:

- **Core** – Domain models, generators, adapters (pure .NET logic)
- **Control** – Navigation, ECS world managers, ReactiveUI ViewModels
- **Rendering** – Visualization pipelines (Skia/Braille/Sixel/etc.) built atop `Shared.Rendering`

Shared infrastructure lives under `dotnet/Shared/`:

- `PigeonPea.Shared.ECS` – Arch components (Position, Renderable, Health, etc.)
- `PigeonPea.Shared.Rendering` – Rendering contracts (`IRenderer`, tiles, primitives, converters)

Archived/legacy code remains for reference only:

- `_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/` (deprecated – see README-DEPRECATED)
- `dotnet/archive/SharedApp.Rendering.archived-*/` (old shared renderer)

**Start here for details:** [docs/architecture/domain-organization.md](docs/architecture/domain-organization.md).

### Technology Stack

- **.NET 9.0** – Core runtime for all projects
- **Arch ECS** – Entity Component System powering map/dungeon worlds
- **FantasyMapGenerator Core** – Procedural world map generation (wrapped in Map.Core)
- **GoRogue** – Dungeon layout, FOV, and pathfinding primitives (wrapped in Dungeon.Core/Control)
- **Mapsui** – Map navigation UI integration in Map.Control
- **SkiaSharp** – Rendering backend for rasterization
- **Terminal.Gui** – Console application UI
- **Avalonia** – Desktop application UI

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
pip install -r requirements/dev.txt

# Or use Taskfile
task python:install-dev

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

- Ensure development dependencies are installed: `pip install -r requirements/dev.txt`
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

## Documentation Management (RFC-012)

This project uses a structured documentation management system with validation, registry generation, and quality assurance.

### Documentation Workflow

1. **Check for Existing Docs**

   ```bash
   # Search the registry for existing documentation
   python scripts/validate-docs.py
   cat docs/index/registry.json | jq '.docs[] | select(.title | contains("your topic"))'
   ```

2. **Create Draft in Inbox**

   ```bash
   # Create a draft with minimal front-matter
   cat > docs/_inbox/my-feature.md << EOF
   ---
   title: "My Feature Documentation"
   doc_type: "guide"
   status: "draft"
   created: "$(date +%Y-%m-%d)"
   ---

   # My Feature Documentation

   Content here...
   EOF
   ```

3. **Validate and Check for Duplicates**

   ```bash
   python scripts/validate-docs.py
   ```

4. **Complete Front-Matter**
   - Add `doc_id` (e.g., `GUIDE-2025-00042`)
   - Add `tags` array
   - Add `summary` string
   - Set `canonical` (true/false)
   - Add optional fields: `author`, `updated`, `supersedes`, `related`

5. **Move to Final Location**

   ```bash
   # Move to appropriate directory:
   # - docs/rfcs/ for RFCs
   # - docs/guides/ for guides
   # - docs/architecture/ for ADRs
   # - docs/planning/ for plans
   mv docs/_inbox/my-feature.md docs/guides/setup-guide.md
   ```

6. **Commit (Pre-commit validates automatically)**
   ```bash
   git add docs/guides/setup-guide.md
   git commit -m "docs: add feature setup guide"
   ```

### Documentation Validation

The validation script (`scripts/validate-docs.py`) checks:

- **Front-matter validation**: All required fields present and valid
- **Canonical uniqueness**: Only one canonical doc per concept
- **Duplicate detection**: Warns about similar titles/content using SimHash and fuzzy matching
- **Registry generation**: Creates `docs/index/registry.json` for agent consumption

#### Running Validation Manually

```bash
# Full validation with registry generation
python scripts/validate-docs.py

# Pre-commit mode (validation only, no registry regeneration)
python scripts/validate-docs.py --pre-commit

# Custom directories
python scripts/validate-docs.py --docs-dir ./custom-docs --registry ./custom-registry.json
```

### Documentation Schema

All documentation (except `docs/_inbox/` drafts) must include YAML front-matter with these required fields:

```yaml
---
doc_id: 'PREFIX-YYYY-NNNNN' # e.g., RFC-2025-00042
title: 'Document Title'
doc_type: 'rfc' # spec, rfc, adr, plan, finding, guide, glossary, reference
status: 'active' # draft, active, superseded, rejected, archived
canonical: true # Is this the authoritative version?
created: '2025-11-14' # ISO date (YYYY-MM-DD)
tags: ['tag1', 'tag2'] # List of tags
summary: 'Brief description' # One-sentence summary
---
```

See [`docs/DOCUMENTATION-SCHEMA.md`](docs/DOCUMENTATION-SCHEMA.md) for complete schema reference.

### Testing Documentation Validation

```bash
# Run unit tests
pytest tests/test_validate_docs.py -v

# Run integration tests
bash tests/integration/test-doc-workflow.sh
```

For more details, see [RFC-012: Documentation Organization Management](docs/rfcs/012-documentation-organization-management.md).
