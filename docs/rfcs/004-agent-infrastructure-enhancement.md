---
doc_id: "RFC-2025-00004"
title: "Agent Infrastructure Enhancement"
doc_type: "rfc"
status: "draft"
canonical: true
created: "2025-11-10"
tags: ["agents", "infrastructure", "automation", "skills", "sub-agents"]
summary: "Enhance the .agent infrastructure with sub-agents, declarative skills, validation schemas, and coding-focused policies to enable autonomous GitHub Coding Agents to perform complex development tasks"
supersedes: []
related: ["RFC-2025-00012"]
---

# RFC-004: Agent Infrastructure Enhancement

## Status

**Status**: Draft
**Created**: 2025-11-10
**Author**: Development Team

## Summary

Enhance the `.agent` infrastructure with sub-agents, declarative skills, validation schemas, and coding-focused policies to enable autonomous GitHub Coding Agents to perform complex development tasks (building, testing, code quality) with minimal human intervention.

## Motivation

The current `.agent` structure provides basic rules, commands, workflows, and adapters, but lacks:

1. **Composable Architecture**: No clear separation between capabilities (skills) and actors (sub-agents)
2. **Discoverability**: GitHub Coding Agents cannot easily discover what capabilities are available
3. **Validation**: No schemas to ensure agent manifests are correct
4. **Progressive Disclosure**: Large monolithic documentation instead of focused, ~200-line skill entries
5. **Specialized Actors**: No sub-agents for specific domains (build, test, code review)

### Goals

1. **Sub-Agent Architecture**: Orchestrator delegates to specialized sub-agents (build, test, code review)
2. **Declarative Skills**: Atomic capabilities with front-matter schemas (~200-line entries + 200-300-line references)
3. **Schema Validation**: JSON Schemas for skills, sub-agents, orchestrator
4. **Coding Focus**: Build (.NET/Nuke), test execution, code formatting, static analysis
5. **GitHub Agent Ready**: Structured for consumption by GitHub Coding Agents and Claude Code

## Design

### Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                   Orchestrator Agent                 │
│          Routes user requests to sub-agents          │
└─────────────────────┬────────────────────────────────┘
                      │
                      │ Delegation based on intent
                      │
        ┌─────────────┼─────────────┐
        │             │             │
┌───────▼──────┐ ┌────▼─────┐ ┌────▼────────┐
│ Build Agent  │ │Code Agent│ │ Test Agent  │
│              │ │          │ │             │
│ - dotnet     │ │- format  │ │- dotnet test│
│   build      │ │- analyze │ │- coverage   │
│ - nuke       │ │- review  │ │- benchmarks │
│   (future)   │ │          │ │             │
└──────┬───────┘ └────┬─────┘ └─────┬───────┘
       │              │              │
       │  Invokes     │              │
       │  Skills      │              │
       ▼              ▼              ▼
┌──────────────────────────────────────────┐
│              Skills Layer                │
│         (Atomic Capabilities)            │
│                                          │
│  dotnet-build/        code-format/       │
│    SKILL.md             SKILL.md         │
│    references/          references/      │
│      build-solution.md    dotnet-fmt.md │
│      restore-deps.md      prettier.md   │
│                                          │
│  dotnet-test/         code-analyze/      │
│    SKILL.md             SKILL.md         │
│    references/          references/      │
│      run-tests.md         static.md     │
│      coverage.md          security.md   │
└──────────────────────────────────────────┘
```

### Directory Structure

```
.agent/
  agents/
    orchestrator.yaml       # Top-level router
    dotnet-build.yaml       # Build sub-agent
    code-review.yaml        # Code quality sub-agent
    testing.yaml            # Testing sub-agent

  skills/
    dotnet-build/
      SKILL.md              # ~200 lines: entry/router
      references/
        build-solution.md   # 200-300 lines: detailed procedure
        restore-deps.md
        run-benchmarks.md
      scripts/
        build-all.sh

    dotnet-test/
      SKILL.md
      references/
        run-unit-tests.md
        generate-coverage.md
        run-benchmarks.md
      scripts/
        test-with-coverage.sh

    code-format/
      SKILL.md
      references/
        dotnet-format.md
        prettier-format.md
        fix-all.md
      scripts/
        format-all.sh

    code-analyze/
      SKILL.md
      references/
        static-analysis.md
        security-scan.md
        dependency-check.md
      scripts/
        analyze.sh

  schemas/
    skill.schema.json       # Front-matter validation
    subagent.schema.json    # Sub-agent YAML validation
    orchestrator.schema.json

  policies/
    defaults.yaml           # Rate limits, safety, repo boundaries
    coding-standards.yaml   # Coding-specific guardrails

  providers/                # (Optional) Provider-specific hints
    claude.yaml
    copilot.yaml

  rules/                    # (Existing) Keep as-is
  commands/                 # (Existing) Keep as-is
  workflows/                # (Existing) Keep as-is
  adapters/                 # (Existing) Keep as-is
```

## Core Components

### 1. Orchestrator Agent

**File**: `.agent/agents/orchestrator.yaml`

Routes user requests to appropriate sub-agents based on task intent.

```yaml
name: Orchestrator
description: Top-level router that delegates to sub-agents based on task intent
version: 0.1.0

subagents:
  - DotNetBuildAgent
  - CodeReviewAgent
  - TestingAgent

routing:
  rules:
    - if: "task contains 'build' or 'compile' or 'restore' or 'nuke'"
      to: DotNetBuildAgent

    - if: "task contains 'test' or 'coverage' or 'benchmark'"
      to: TestingAgent

    - if: "task contains 'review' or 'format' or 'lint' or 'analyze' or 'quality'"
      to: CodeReviewAgent

    - if: "task contains 'refactor' or 'code' or 'implement' or 'fix'"
      to: CodeReviewAgent # Default for coding tasks

fallback: CodeReviewAgent

policies:
  - enforce: .agent/policies/defaults.yaml
  - enforce: .agent/policies/coding-standards.yaml
```

### 2. Sub-Agents

#### DotNetBuildAgent

**File**: `.agent/agents/dotnet-build.yaml`

```yaml
name: DotNetBuildAgent
description: Handles .NET build, restore, compile, and packaging tasks
version: 0.1.0

skills:
  - dotnet-build
  # - nuke-build  # Future: add when Nuke is integrated

goals:
  - Produce deterministic, reproducible builds
  - Ensure all dependencies restored correctly
  - Generate build artifacts (binaries, packages)
  - Report build warnings and errors clearly

constraints:
  - Work only within ./dotnet directory
  - Never commit build artifacts (bin/, obj/)
  - Report build warnings and errors with context
  - Ensure builds are idempotent

success_criteria:
  - 'Build succeeds with zero errors'
  - 'Artifacts generated in expected output path'
  - 'Build logs attached with full context'
  - 'Dependencies restored successfully'
```

#### CodeReviewAgent

**File**: `.agent/agents/code-review.yaml`

```yaml
name: CodeReviewAgent
description: Reviews code quality, formatting, and enforces coding standards
version: 0.1.0

skills:
  - code-format
  - code-analyze

goals:
  - Ensure code follows formatting standards (dotnet-format, prettier)
  - Run static analysis and security scans
  - Suggest improvements for code quality
  - Identify potential bugs and security issues

constraints:
  - Only analyze code, never execute untrusted code
  - Respect existing .editorconfig and formatting rules
  - Flag security issues with HIGH priority
  - Provide actionable feedback with file/line references

success_criteria:
  - 'All formatting checks pass'
  - 'No critical security issues found'
  - 'Analysis report generated with actionable items'
  - 'Code quality metrics within acceptable range'
```

#### TestingAgent

**File**: `.agent/agents/testing.yaml`

```yaml
name: TestingAgent
description: Executes tests, generates coverage, runs benchmarks
version: 0.1.0

skills:
  - dotnet-test

goals:
  - Run all relevant tests (unit, integration)
  - Generate coverage reports when requested
  - Execute benchmarks when requested
  - Identify failing tests and report clearly

constraints:
  - Use test frameworks already configured (xUnit, NUnit)
  - Never skip tests without explicit permission
  - Report test results in readable format
  - Fail fast on critical test failures

success_criteria:
  - 'All tests pass or failures clearly reported'
  - 'Coverage data generated (if requested)'
  - 'Benchmark results available (if requested)'
  - 'Test execution time within acceptable range'
```

### 3. Skills (Progressive Disclosure Pattern)

Following the Reddit refactor pattern: entry file ~200 lines, references 200-300 lines each.

#### Example: dotnet-build/SKILL.md

**File**: `.agent/skills/dotnet-build/SKILL.md`

````markdown
---
name: dotnet-build
version: 0.2.0
kind: cli
description: Build .NET solution/projects using dotnet CLI. Use when task involves compiling, restoring dependencies, or building artifacts.
inputs:
  target: [solution, project, all]
  configuration: [Debug, Release]
  project_path: string # Optional, defaults to ./dotnet/PigeonPea.sln
contracts:
  success: 'Build completes with zero errors; artifacts in bin/'
  failure: 'Non-zero exit code or compilation errors'
---

# .NET Build Skill (Entry Map)

> **Goal:** Guide agent to the exact build procedure needed.

## Quick Start (Pick One)

- **Build entire solution** → `references/build-solution.md`
- **Restore dependencies only** → `references/restore-deps.md`
- **Run benchmarks after build** → `references/run-benchmarks.md`

## When to Use

- Compiling .NET code (.csproj, .sln)
- Restoring NuGet packages
- Building specific configurations (Debug/Release)
- Preparing for testing or packaging

**Do NOT use for:**

- Running tests (use `dotnet-test` skill)
- Code formatting (use `code-format` skill)
- Static analysis (use `code-analyze` skill)

## Inputs & Outputs (Concise)

- `target`: solution | project | all
- `configuration`: Debug | Release (default: Debug)
- `project_path`: path to .sln or .csproj (default: ./dotnet/PigeonPea.sln)
- **Output**: `artifact_path` (bin/ directory), `build_log`

## Guardrails

- Operate within `./dotnet` directory
- Never commit `bin/`, `obj/` folders
- Surface warnings and errors clearly
- Idempotent: safe to re-run

## Navigation (Pick Relevant Reference)

1. **Build entire solution** → `references/build-solution.md`
2. **Restore dependencies** → `references/restore-deps.md`
3. **Build + run benchmarks** → `references/run-benchmarks.md`

## Common Patterns

```bash
# Build solution (Debug)
dotnet build ./dotnet/PigeonPea.sln

# Build solution (Release)
dotnet build ./dotnet/PigeonPea.sln -c Release

# Restore + Build
dotnet restore ./dotnet && dotnet build ./dotnet/PigeonPea.sln
```

## Troubleshooting

- **Build fails with missing deps**: Read `references/restore-deps.md`
- **Compilation errors**: Capture first 20 error lines, surface to user
- **Benchmark failures**: Check `references/run-benchmarks.md`

<!-- Keep this under ~200 lines. Move detailed procedures to references/. -->
````

#### Example Reference: references/build-solution.md

**File**: `.agent/skills/dotnet-build/references/build-solution.md`

````markdown
# Build .NET Solution - Detailed Procedure

## Overview

Builds the entire PigeonPea.sln solution, including all projects and test projects.

## Prerequisites

- .NET SDK installed (check: `dotnet --version`)
- Solution file: `./dotnet/PigeonPea.sln`
- All project files (.csproj) present

## Standard Build Flow

### 1. Restore Dependencies

```bash
cd ./dotnet
dotnet restore PigeonPea.sln
```

**Expected output:**

```
Restore completed in X.XXs for ...
```

### 2. Build Solution (Debug)

```bash
dotnet build PigeonPea.sln --configuration Debug
```

**Expected output:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 3. Build Solution (Release)

```bash
dotnet build PigeonPea.sln --configuration Release --no-restore
```

**Flags:**

- `--no-restore`: Skip restore if already done
- `-c Release`: Use Release configuration
- `--verbosity normal`: Adjust log level (quiet, minimal, normal, detailed, diagnostic)

## Output Locations

- **Binaries**: `./dotnet/{ProjectName}/bin/{Configuration}/net9.0/`
- **Example**: `./dotnet/console-app/bin/Debug/net9.0/`

## Common Errors

### Error: NU1301 (Unable to load service index)

**Cause:** NuGet package source unreachable.

**Fix:**

```bash
dotnet nuget list source
dotnet restore --source https://api.nuget.org/v3/index.json
```

### Error: CS0246 (Type or namespace not found)

**Cause:** Missing package reference or project reference.

**Fix:** Check `.csproj` for `<PackageReference>` or `<ProjectReference>`.

### Error: MSB4018 (Unexpected error)

**Cause:** Corrupted obj/ or bin/ folders.

**Fix:**

```bash
dotnet clean
rm -rf ./dotnet/**/bin ./dotnet/**/obj
dotnet restore && dotnet build
```

## Performance Tips

- Use `--no-restore` if restore already done
- Parallel builds: `dotnet build -m` (multi-core)
- Skip analyzing: `dotnet build /p:RunAnalyzers=false`

## Integration with Pre-commit Hooks

Before committing code that modifies .csproj or source files:

```bash
dotnet build PigeonPea.sln
pre-commit run --all-files
```

## Related

- Restore only: `restore-deps.md`
- Build + benchmarks: `run-benchmarks.md`
- Testing after build: See `dotnet-test` skill
````

### 4. Schemas

#### skill.schema.json

**File**: `.agent/schemas/skill.schema.json`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Skill Front-Matter",
  "description": "Schema for SKILL.md front-matter (YAML header)",
  "type": "object",
  "required": ["name", "version", "kind", "description", "contracts"],
  "properties": {
    "name": {
      "type": "string",
      "pattern": "^[a-z][a-z0-9-]*$",
      "description": "Skill name (kebab-case)"
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$",
      "description": "Semantic version"
    },
    "kind": {
      "enum": ["cli", "mcp", "http", "script", "composite"],
      "description": "Skill execution type"
    },
    "description": {
      "type": "string",
      "minLength": 20,
      "maxLength": 300,
      "description": "Clear description including WHEN to use this skill"
    },
    "inputs": {
      "type": "object",
      "additionalProperties": {
        "oneOf": [{ "type": "array", "items": { "type": "string" } }, { "type": "string" }]
      },
      "description": "Input parameters and their possible values"
    },
    "contracts": {
      "type": "object",
      "properties": {
        "success": { "type": "string", "minLength": 10 },
        "failure": { "type": "string", "minLength": 10 }
      },
      "required": ["success", "failure"],
      "description": "Success and failure criteria"
    }
  }
}
```

#### subagent.schema.json

**File**: `.agent/schemas/subagent.schema.json`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Sub-Agent Definition",
  "description": "Schema for sub-agent YAML manifests",
  "type": "object",
  "required": ["name", "description", "version", "skills", "goals"],
  "properties": {
    "name": {
      "type": "string",
      "description": "Sub-agent name (PascalCase)"
    },
    "description": {
      "type": "string",
      "minLength": 20,
      "description": "Clear description of what this sub-agent does"
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$",
      "description": "Semantic version"
    },
    "skills": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1,
      "description": "List of skills this sub-agent can invoke"
    },
    "goals": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1,
      "description": "What this sub-agent aims to achieve"
    },
    "constraints": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Limitations and boundaries"
    },
    "success_criteria": {
      "type": "array",
      "items": { "type": "string" },
      "description": "How to measure success"
    }
  }
}
```

### 5. Policies

#### defaults.yaml

**File**: `.agent/policies/defaults.yaml`

```yaml
name: DefaultPolicies
description: General guardrails for all agents
version: 0.1.0

rate_limits:
  max_tool_calls_per_session: 1000
  max_file_edits_per_session: 100
  max_file_reads_per_session: 500

safety:
  never_commit:
    - 'bin/'
    - 'obj/'
    - '*.exe'
    - '*.dll'
    - '*.user'
    - '*.suo'
    - 'node_modules/'
    - '.vs/'

  never_delete:
    - '.git/'
    - '.agent/'
    - '*.sln'
    - '*.csproj'
    - 'README.md'
    - 'LICENSE'

  never_expose:
    - secrets
    - credentials
    - API keys
    - connection strings
    - authentication tokens

repository_boundaries:
  allowed_directories:
    - './dotnet'
    - './tests'
    - './docs'
    - './.agent'
    - './.github'

  forbidden_directories:
    - './bin'
    - './obj'
    - './packages'
    - './.vs'
    - './node_modules'
```

#### coding-standards.yaml

**File**: `.agent/policies/coding-standards.yaml`

```yaml
name: CodingStandards
description: Coding-specific policies and standards for .NET development
version: 0.1.0

dotnet:
  style:
    - 'Follow .editorconfig rules strictly'
    - 'Use dotnet-format before every commit'
    - 'PascalCase for public members'
    - 'camelCase for private fields (_camelCase for backing fields)'
    - 'Use explicit access modifiers'

  testing:
    - 'All public APIs must have unit tests'
    - 'Test projects follow naming: {ProjectName}.Tests'
    - 'Use xUnit/NUnit patterns consistently'
    - 'Aim for >70% code coverage on new code'

  documentation:
    - 'XML docs for all public APIs'
    - 'Inline comments for complex logic (why, not what)'
    - 'Keep README.md updated with new features'
    - 'Update ARCHITECTURE.md when adding new components'

formatting:
  tools:
    - 'dotnet-format (C#)'
    - 'prettier (JSON, YAML, Markdown)'

  enforcement:
    - 'Run pre-commit hooks before every commit'
    - 'CI must validate formatting on every PR'
    - 'Zero tolerance for formatting violations in main'

code_quality:
  metrics:
    - 'Cyclomatic complexity < 15 per method'
    - 'Method length < 50 lines (prefer < 20)'
    - 'Class length < 300 lines (prefer < 200)'

  analysis:
    - 'Enable all Roslyn analyzers'
    - 'Treat warnings as errors in Release builds'
    - 'Fix all critical/high severity issues before merge'
```

## Implementation Plan

### Phase 1: Core Structure (Week 1)

**Goal**: Create directory structure, orchestrator, and 1 sub-agent + 1 skill

1. Create directory structure (agents/, skills/, schemas/, policies/)
2. Add orchestrator.yaml
3. Add dotnet-build.yaml sub-agent
4. Add dotnet-build skill (entry + 1 reference)
5. Add schemas (skill.schema.json, subagent.schema.json)
6. Add policies (defaults.yaml, coding-standards.yaml)

**Deliverable**: Minimal working structure with 1 agent, 1 skill

### Phase 2: Essential Skills & Sub-Agents (Week 2)

**Goal**: Add remaining sub-agents and core skills

1. Add code-review.yaml sub-agent
2. Add testing.yaml sub-agent
3. Add dotnet-test skill (entry + 2 references)
4. Add code-format skill (entry + 2 references)
5. Add code-analyze skill (entry + 2 references)

**Deliverable**: All 3 sub-agents + 4 core skills

### Phase 3: Validation & Automation (Week 3)

**Goal**: Ensure schemas enforce quality, auto-generate registry

1. Create validation scripts (Python or C#)
   - `scripts/validate_skills.py`
   - `scripts/validate_agents.py`
   - `scripts/generate_registry.py`
2. Extend pre-commit hooks to run validation
3. Create Taskfile.yml for convenience commands
4. Auto-generate AGENTS.md registry from manifests

**Deliverable**: Automated validation + registry generation

### Phase 4: Optional Enhancements (Week 4)

**Goal**: Provider hints, Nuke integration (if desired)

1. Add provider-specific hints (.agent/providers/)
2. (Optional) Add Nuke build skill if Nuke is integrated
3. (Optional) Add more granular skills as needed
4. Documentation and examples

**Deliverable**: Polished, production-ready agent infrastructure

## Testing Strategy

### Schema Validation Tests

```python
# scripts/validate_skills.py
import yaml
import json
from jsonschema import validate

def validate_skill_frontmatter(skill_path, schema_path):
    with open(skill_path) as f:
        content = f.read()

    # Extract YAML front-matter
    if not content.startswith('---'):
        raise ValueError(f"Missing front-matter in {skill_path}")

    parts = content.split('---', 2)
    front_matter = yaml.safe_load(parts[1])

    # Validate against schema
    with open(schema_path) as f:
        schema = json.load(f)

    validate(instance=front_matter, schema=schema)
    print(f"✓ {skill_path} validated successfully")
```

### Cold-Start Budget Test

```python
def test_cold_start_budget(skill_path):
    """Ensure entry + 1 reference <= ~500 lines total"""
    with open(skill_path) as f:
        entry_lines = len(f.readlines())

    assert entry_lines <= 220, f"Entry too large: {entry_lines} lines"

    # Check first reference
    ref_path = skill_path.parent / "references" / "*.md"
    refs = list(ref_path.parent.glob("*.md"))
    if refs:
        with open(refs[0]) as f:
            ref_lines = len(f.readlines())
        assert ref_lines <= 320, f"Reference too large: {ref_lines} lines"

        total = entry_lines + ref_lines
        assert total <= 550, f"Cold-start budget exceeded: {total} lines"
```

### Integration Tests

```bash
# Test orchestrator routing
task test:orchestrator

# Test sub-agent invocation
task test:subagent:build

# Test skill execution
task test:skill:dotnet-build
```

## Size Guidelines (Reddit Refactor Pattern)

| File Type         | Max Lines | Purpose                  |
| ----------------- | --------- | ------------------------ |
| SKILL.md entry    | ~200      | Router/map to references |
| Reference file    | 200-300   | Detailed procedure       |
| Sub-agent YAML    | ~50       | Concise definition       |
| Orchestrator YAML | ~80       | Routing rules            |
| Schema            | ~100      | Validation contract      |

**Cold-start budget:** Entry + 1 reference ≤ ~500 lines total

## Success Criteria

After implementation, agents should be able to:

1. **Build the solution**: "Build the solution in Release mode"
   → Orchestrator routes to DotNetBuildAgent
   → Uses dotnet-build skill
   → Loads entry + build-solution.md reference
   → Executes build, returns artifacts

2. **Format and analyze code**: "Format all code and check for issues"
   → Routes to CodeReviewAgent
   → Uses code-format + code-analyze skills
   → Returns formatting changes + analysis report

3. **Run tests with coverage**: "Run all tests and generate coverage"
   → Routes to TestingAgent
   → Uses dotnet-test skill
   → Returns test results + coverage data

4. **Validate on commit**:
   → Pre-commit runs validation scripts
   → Checks all SKILL.md front-matter against schemas
   → Checks all agent YAMLs
   → Fails if violations found

## Migration Path

### Before (Manual Commands)

```yaml
# .agent/commands/run-tests.yaml
steps:
  - name: Run .NET tests
    command: dotnet test
    working_directory: ./dotnet
```

### After (Sub-Agent + Skill)

```yaml
# .agent/agents/testing.yaml
name: TestingAgent
skills:
  - dotnet-test
# .agent/skills/dotnet-test/SKILL.md
# Agent reads skill, loads references, executes
```

## Performance Considerations

1. **Lazy Loading**: Only load skill references when activated
2. **Caching**: Cache parsed YAML/JSON schemas
3. **Incremental Validation**: Only validate changed files
4. **Parallel Execution**: Run independent validations in parallel

## Backward Compatibility

Existing `.agent` components are preserved:

- `.agent/rules/` - Keep as-is
- `.agent/commands/` - Keep as-is (can be wrapped by skills later)
- `.agent/workflows/` - Keep as-is
- `.agent/adapters/` - Keep as-is

New additions are additive, not breaking.

## Alternatives Considered

### Alternative 1: Flat Skill Structure

Put all skills in `.agent/skills/*.md` instead of subdirectories.

**Pros**: Simpler file structure
**Cons**: No progressive disclosure, harder to manage references

**Decision**: Rejected - subdirectories enable progressive disclosure

### Alternative 2: Single Monolithic Agent

One large agent file instead of sub-agents.

**Pros**: Simpler
**Cons**: No specialization, harder to maintain, worse for delegation

**Decision**: Rejected - sub-agents provide better separation of concerns

### Alternative 3: Skills Outside .agent/

Put skills in root-level `.skills/` directory.

**Pros**: More discoverable, easier to share across repos
**Cons**: Less cohesive, agents and skills separated

**Decision**: Deferred - start nested, can export later if needed

## Open Questions

1. **Nuke Integration**: Should we add Nuke build now or later?
   - **Proposal**: Add as optional skill after basic dotnet-build works

2. **Provider-Specific Hints**: Do we need .agent/providers/?
   - **Proposal**: Add if we find Claude/Copilot need different guidance

3. **MCP Integration**: Should skills wrap MCP servers?
   - **Proposal**: Future enhancement, not in v0.1

## References

- [Reddit: Refactoring Agent Skills](https://www.reddit.com/r/ClaudeAI/comments/1opxgq4/i_was_wrong_about_agent_skills_and_how_i_refactor/)
- [Claude Code: Sub-Agents](https://docs.claude.com/en/docs/claude-code/sub-agents)
- [GitHub Coding Agent Documentation](https://github.com/features/copilot)
- [lablab-bean Agent Structure](https://github.com/GiantCroissant-Lunar/lablab-bean)
- [Nuke Build](https://nuke.build/)
