# Agent Configuration

This document describes agent configuration for the pigeon-pea project, supporting multiple AI platforms.

## Overview

This file provides guidance for AI agents working with this codebase, including specific rules, workflows, and best practices.

## Multi-Platform Support

This project supports multiple AI agent platforms:

- **Claude Code** - See [`.agent/providers/claude.yaml`](.agent/providers/claude.yaml) for Claude-specific configuration
- **GitHub Copilot** - See [`.agent/providers/copilot.yaml`](.agent/providers/copilot.yaml) for Copilot-specific hints
- **Windsurf** - See [`.windsurf/rules.md`](.windsurf/rules.md) and [`.agent/providers/windsurf.yaml`](.agent/providers/windsurf.yaml) for Windsurf configuration

All agents should follow the core rules in [`.agent/rules/`](.agent/rules/), with platform-specific optimizations available in [`.agent/providers/`](.agent/providers/).

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

### .NET Architecture Rules (CRITICAL)

**ALL agents working on .NET projects MUST follow the tiered architecture rules.**

See [`.agent/rules/dotnet-architecture.md`](.agent/rules/dotnet-architecture.md) for complete architecture requirements.

Key requirements:

- **Four-tier service architecture**: Contracts (Tier 1) → Proxies (Tier 2) → Implementations (Tier 3) → Providers (Tier 4)
- **NO wrapper projects** - Use external libraries directly in plugins
- **Double-plugin architecture** for content domains (Map, Dungeon):
  - Domain plugins: Know WHAT to render/generate
  - Platform plugins: Know HOW to render (ANSI, Braille, SkiaSharp)
- **Plugin isolation** - Plugins never depend on other plugins
- **Contract stability** - Tier 1 contracts use only primitives and DTOs
- **Dependency rules**:
  - ✅ Tier 3 → Tier 1 (allowed)
  - ✅ Tier 3 → Shared libraries (allowed)
  - ❌ Tier 3 → Other Tier 3 (forbidden)
  - ❌ Plugin → Plugin (forbidden)

**Before writing .NET code**: Read [.NET Tiered Architecture and Layer Implementation Guide](docs/guides/dotnet-tiered-architecture-guide.md) for comprehensive details.

### Documentation Management Rules

**IMPORTANT:** Follow RFC-012 documentation management system for all documentation tasks.

See [`docs/rfcs/012-documentation-organization-management.md`](docs/rfcs/012-documentation-organization-management.md) for complete documentation workflow.

Key requirements:

- Check [`docs/index/registry.json`](docs/index/registry.json) for existing docs before creating new ones
- Start drafts in [`docs/_inbox/`](docs/_inbox/) with minimal front-matter
- Run `python scripts/validate-docs.py` to validate and check for duplicates
- Complete front-matter before moving to final location (docs/rfcs/, docs/guides/, etc.)
- All documentation must have YAML front-matter (see [`docs/DOCUMENTATION-SCHEMA.md`](docs/DOCUMENTATION-SCHEMA.md))
- Pre-commit hook automatically validates documentation on commit

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

## Architecture Overview

### Tiered Service Architecture

Pigeon Pea follows a **four-tier service architecture** with **plugin-based composition**:

1. **Tier 1 - Contracts**: Service interfaces and DTOs (stable, minimal dependencies)
2. **Tier 2 - Proxies**: Source-generated routing to implementations
3. **Tier 3 - Real Services**: Plugin implementations of services
4. **Tier 4 - Providers**: Optional internal strategies/backends

**CRITICAL**: See [.NET Tiered Architecture Guide](docs/guides/dotnet-tiered-architecture-guide.md) for complete details.

### Project Organization

- **app-essential**: Non-gameplay infrastructure (Input, Audio, Config, Resource)
  - Contracts: `PigeonPea.Contracts.<Domain>`
  - Plugins: `PigeonPea.Plugins.<Domain>.<Implementation>`

- **game-essential**: Gameplay infrastructure (Inventory, GAS, Perception, AI)
  - Contracts: `PigeonPea.Game.Contracts.<Domain>`
  - Shared: `PigeonPea.Shared.<Domain>` (algorithms and models)
  - Plugins: `PigeonPea.Plugins.<Domain>.<Implementation>`

- **projects**: Domain-specific implementations (Map, Dungeon)
  - Contracts: `PigeonPea.<Domain>.Contracts`
  - Plugins: `PigeonPea.Plugin.<Domain>.<Feature>`

### Domain-Driven Organization

**Content domains** (Map, Dungeon) follow **Double-Plugin Architecture**:

- **Domain Plugins**: Know WHAT to render/generate (domain logic)
  - Example: `PigeonPea.Plugin.Dungeon.Rendering` calls `IRenderer.DrawTile()`
  - Example: `PigeonPea.Plugin.Dungeon.ModernEdgar` uses modern-edgar-dotnet directly

- **Platform Plugins**: Know HOW to render (platform-specific)
  - Example: `PigeonPea.Plugin.Rendering.Terminal.ANSI` implements `IRenderer`
  - Example: `PigeonPea.Plugin.Rendering.Terminal.Braille` implements `IRenderer`

**Key Insight**: Domain and platform plugins don't know about each other! Add new domain → works with all platforms. Add new platform → works with all domains.

### Shared Infrastructure

- `PigeonPea.Shared.ECS`: Arch components (Position, Renderable, Health, etc.)
- `PigeonPea.Shared.Rendering`: Unified rendering contracts (`IRenderer`, `Tile`, `Color`)
- `PigeonPea.Shared.<Domain>`: Domain-specific algorithms and models
- External libraries: Used **directly** in plugins (NO wrappers)

### ECS Worlds

- Each domain owns its own `World` (Map: cities/markers, Dungeon: player/monsters/items)
- Shared components live in `Shared.ECS/Components`
- Game integration layers (`PigeonPea.Game.<Domain>`) tie domains to ECS

### Deprecated/Archived Code

- `dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/` - **DEPRECATED** (use plugins instead)
- `dotnet/game-essential/core/src/PigeonPea.Dungeon.Rendering/` - **DEPRECATED** (use plugins instead)
- `dotnet/game-essential/core/src/PigeonPea.Dungeon.Control/` - **DEPRECATED** (use plugins instead)
- `dotnet/archive/SharedApp.Rendering.archived-*` - Retired shared renderer

**IMPORTANT**: Do NOT create new wrapper projects. Wrapper projects violate the tiered architecture and create ALC type identity issues.

### Architecture Documentation

For comprehensive architecture information:

- **Quick Reference**: [.agent/rules/dotnet-architecture.md](.agent/rules/dotnet-architecture.md)
- **Complete Guide**: [docs/guides/dotnet-tiered-architecture-guide.md](docs/guides/dotnet-tiered-architecture-guide.md)
- **Design Rationale**: [docs/rfcs/013-plugin-architecture-refinement-tiered.md](docs/rfcs/013-plugin-architecture-refinement-tiered.md)
- **Service Tiers**: [docs/dotnet/architecture/service-tiers.md](docs/dotnet/architecture/service-tiers.md)
- **Overview**: [docs/dotnet/architecture/overview.md](docs/dotnet/architecture/overview.md)

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

## Troubleshooting

### Documentation Validation Errors

Common documentation validation errors and their solutions:

#### Missing Required Fields

```
[ERROR] docs/rfcs/test.md: Missing required fields: doc_id, tags, summary
```

**Solution:** Add all required fields to the front-matter:

- `doc_id` (format: PREFIX-YYYY-NNNNN, e.g., RFC-2025-00042)
- `title`
- `doc_type` (spec, rfc, adr, plan, finding, guide, glossary, reference)
- `status` (draft, active, superseded, rejected, archived)
- `canonical` (true/false)
- `created` (ISO date: YYYY-MM-DD)
- `tags` (array)
- `summary` (string)

#### Invalid doc_type

```
[ERROR] docs/guides/test.md: Invalid doc_type 'tutorial'. Must be one of: spec, rfc, adr, plan, finding, guide, glossary, reference
```

**Solution:** Use one of the valid doc_type values. For tutorials, use `guide`.

#### Invalid doc_id Format

```
[ERROR] docs/rfcs/test.md: Invalid doc_id format 'RFC-001'. Expected format: PREFIX-YYYY-NNNNN
```

**Solution:** Use the correct format: `RFC-2025-00001` (not `RFC-001`)

#### Multiple Canonical Documents

```
[ERROR] Multiple canonical documents for concept 'plugin architecture':
  docs/rfcs/006-plugin-system-architecture.md
  docs/rfcs/042-plugin-architecture.md
```

**Solution:** Set `canonical: false` on one of the documents. Only one document per concept can be canonical.

#### Near-Duplicate Warning

```
[WARNING] Near-duplicate detected:
  Inbox:  docs/_inbox/new-doc.md
  Corpus: docs/rfcs/existing-doc.md
  Title similarity: 85%, Content similarity: ~90%
```

**Solution:** Review the existing document. Consider:

- Updating the existing document instead of creating a new one
- Making your document more distinct if it covers different aspects
- Adding a reference to the existing document in `related` field

### Pre-commit Hook Failures

If pre-commit hooks fail on documentation:

1. **Run validation manually:**

   ```bash
   python scripts/validate-docs.py
   ```

2. **Fix reported errors**

3. **Run pre-commit again:**
   ```bash
   pre-commit run --all-files
   ```

### Missing Dependencies

If you see warnings about missing dependencies (simhash, rapidfuzz):

```bash
pip install -r scripts/requirements.txt
```

These are optional but provide better duplicate detection.

## Resources

- [AGENTS.md](AGENTS.md) - Complete agent infrastructure documentation
- [README.md](README.md) - Project setup and general information
- [`.agent/rules/`](.agent/rules/) - All agent rules
- [`.agent/commands/`](.agent/commands/) - Available commands
- [`.agent/workflows/`](.agent/workflows/) - Process workflows
- [`.agent/adapters/`](.agent/adapters/) - External system adapters
- [`docs/rfcs/012-documentation-organization-management.md`](docs/rfcs/012-documentation-organization-management.md) - Documentation management system (RFC-012)
- [`docs/DOCUMENTATION-SCHEMA.md`](docs/DOCUMENTATION-SCHEMA.md) - Front-matter schema reference
