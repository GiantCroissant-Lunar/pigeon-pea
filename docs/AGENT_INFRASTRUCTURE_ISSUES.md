# GitHub Issues: Agent Infrastructure Enhancement

This document breaks down RFC-004 into actionable GitHub issues suitable for automated coding agents.

## Issue Template Format

Each issue follows this structure:

- **Title**: Clear, action-oriented
- **Labels**: Type and scope
- **RFC Reference**: Which RFC this implements
- **Dependencies**: Other issues that must be completed first
- **Description**: What needs to be done
- **Acceptance Criteria**: Definition of done
- **Files to Create/Modify**: Specific file paths
- **Code Examples**: Snippets showing expected implementation

---

# RFC-004: Agent Infrastructure Enhancement Issues

## Phase 1: Core Structure (Week 1)

### Issue #44: Create Agent Directory Structure

**Title**: Create `.agent` subdirectories for agents, skills, schemas, policies

**Labels**: `enhancement`, `infrastructure`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: None

**Description**:
Create the directory structure for the enhanced agent infrastructure with subdirectories for agents, skills, schemas, and policies.

**Acceptance Criteria**:

- [ ] `.agent/agents/` directory created
- [ ] `.agent/skills/` directory created
- [ ] `.agent/schemas/` directory created
- [ ] `.agent/policies/` directory created
- [ ] `.agent/providers/` directory created
- [ ] All directories committed to git
- [ ] README files added to each directory explaining purpose

**Files to Create**:

- `.agent/agents/README.md`
- `.agent/skills/README.md`
- `.agent/schemas/README.md`
- `.agent/policies/README.md`
- `.agent/providers/README.md`

**Code Example** (agents/README.md):

```markdown
# Agent Definitions

This directory contains sub-agent definitions that specialize in specific development tasks.

## Sub-Agents

- **orchestrator.yaml** - Top-level router that delegates to specialized sub-agents
- **dotnet-build.yaml** - Handles .NET build and compilation tasks
- **code-review.yaml** - Handles code formatting and quality analysis
- **testing.yaml** - Handles test execution and coverage generation

## Format

Each agent is defined in YAML with:

- `name`: Agent name (PascalCase)
- `description`: Clear description of responsibilities
- `version`: Semantic version
- `skills`: List of skills this agent can invoke
- `goals`: What this agent aims to achieve
- `constraints`: Limitations and boundaries
- `success_criteria`: How to measure success
```

---

### Issue #45: Create Orchestrator Agent Definition

**Title**: Create orchestrator agent for routing to sub-agents

**Labels**: `enhancement`, `agents`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create the orchestrator agent that routes user requests to appropriate sub-agents based on task intent.

**Acceptance Criteria**:

- [ ] `orchestrator.yaml` created with all routing rules
- [ ] Routes to DotNetBuildAgent for build tasks
- [ ] Routes to TestingAgent for test tasks
- [ ] Routes to CodeReviewAgent for code quality tasks
- [ ] Fallback to CodeReviewAgent for general coding
- [ ] Policies enforced (defaults.yaml, coding-standards.yaml)
- [ ] Valid YAML structure
- [ ] Documented with inline comments

**Files to Create**:

- `.agent/agents/orchestrator.yaml`

**Code Example**:

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

---

### Issue #46: Create DotNetBuildAgent Definition

**Title**: Create DotNetBuildAgent sub-agent for build tasks

**Labels**: `enhancement`, `agents`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create the DotNetBuildAgent sub-agent that handles .NET build, restore, and compilation tasks.

**Acceptance Criteria**:

- [ ] `dotnet-build.yaml` created
- [ ] Skills list includes dotnet-build
- [ ] Goals clearly define build objectives
- [ ] Constraints prevent committing artifacts
- [ ] Success criteria measurable
- [ ] Version 0.1.0
- [ ] Valid YAML structure

**Files to Create**:

- `.agent/agents/dotnet-build.yaml`

**Code Example**:

```yaml
name: DotNetBuildAgent
description: Handles .NET build, restore, compile, and packaging tasks
version: 0.1.0

skills:
  - dotnet-build

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

---

### Issue #47: Create dotnet-build Skill with Progressive Disclosure

**Title**: Create dotnet-build skill (entry + references)

**Labels**: `enhancement`, `skills`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create the dotnet-build skill with entry file (~200 lines) and reference files (200-300 lines each) following the progressive disclosure pattern.

**Acceptance Criteria**:

- [ ] `SKILL.md` entry file created (~200 lines max)
- [ ] YAML front-matter with all required fields
- [ ] `references/build-solution.md` created (200-300 lines)
- [ ] `references/restore-deps.md` created (200-300 lines)
- [ ] Entry file acts as router/map to references
- [ ] Cold-start budget: entry + 1 reference ≤ 500 lines
- [ ] Clear "When to Use" section
- [ ] Troubleshooting guide included

**Files to Create**:

- `.agent/skills/dotnet-build/SKILL.md`
- `.agent/skills/dotnet-build/references/build-solution.md`
- `.agent/skills/dotnet-build/references/restore-deps.md`
- `.agent/skills/dotnet-build/scripts/build-all.sh`

**Code Example** (SKILL.md):

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

## When to Use

- Compiling .NET code (.csproj, .sln)
- Restoring NuGet packages
- Building specific configurations (Debug/Release)

**Do NOT use for:**

- Running tests (use `dotnet-test` skill)
- Code formatting (use `code-format` skill)

## Inputs & Outputs

- `target`: solution | project | all
- `configuration`: Debug | Release
- **Output**: `artifact_path`, `build_log`

## Guardrails

- Operate within `./dotnet` directory
- Never commit `bin/`, `obj/`
- Idempotent: safe to re-run

## Navigation

1. **Build entire solution** → `references/build-solution.md`
2. **Restore dependencies** → `references/restore-deps.md`

## Common Patterns

```bash
# Build solution (Debug)
dotnet build ./dotnet/PigeonPea.sln

# Build solution (Release)
dotnet build ./dotnet/PigeonPea.sln -c Release
```

## Troubleshooting

- **Build fails**: Read `references/build-solution.md`
- **Missing deps**: Read `references/restore-deps.md`
````

---

### Issue #48: Create JSON Schemas for Validation

**Title**: Create JSON schemas for skills and sub-agents

**Labels**: `enhancement`, `schemas`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create JSON Schema files to validate skill front-matter and sub-agent YAML manifests.

**Acceptance Criteria**:

- [ ] `skill.schema.json` created
- [ ] Validates front-matter: name, version, kind, description, contracts
- [ ] `subagent.schema.json` created
- [ ] Validates: name, description, version, skills, goals
- [ ] `orchestrator.schema.json` created
- [ ] All schemas follow JSON Schema Draft 2020-12
- [ ] Schemas tested with sample data
- [ ] Documentation comments in schemas

**Files to Create**:

- `.agent/schemas/skill.schema.json`
- `.agent/schemas/subagent.schema.json`
- `.agent/schemas/orchestrator.schema.json`

**Code Example** (skill.schema.json):

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
      "description": "Clear description including WHEN to use"
    },
    "inputs": {
      "type": "object",
      "additionalProperties": {
        "oneOf": [{ "type": "array", "items": { "type": "string" } }, { "type": "string" }]
      }
    },
    "contracts": {
      "type": "object",
      "properties": {
        "success": { "type": "string", "minLength": 10 },
        "failure": { "type": "string", "minLength": 10 }
      },
      "required": ["success", "failure"]
    }
  }
}
```

---

### Issue #49: Create Policy Files

**Title**: Create defaults.yaml and coding-standards.yaml policy files

**Labels**: `enhancement`, `policies`, `rfc-004`, `phase-1`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create policy files that define guardrails for all agents, including rate limits, safety rules, and coding standards.

**Acceptance Criteria**:

- [ ] `defaults.yaml` created with rate limits and safety rules
- [ ] Never commit rules for bin/, obj/, etc.
- [ ] Never delete rules for .git/, .agent/, etc.
- [ ] Repository boundaries defined
- [ ] `coding-standards.yaml` created with .NET standards
- [ ] dotnet-format enforcement
- [ ] Testing requirements
- [ ] Documentation requirements
- [ ] Both files valid YAML

**Files to Create**:

- `.agent/policies/defaults.yaml`
- `.agent/policies/coding-standards.yaml`

**Code Example** (defaults.yaml):

```yaml
name: DefaultPolicies
description: General guardrails for all agents
version: 0.1.0

rate_limits:
  max_tool_calls_per_session: 1000
  max_file_edits_per_session: 100

safety:
  never_commit:
    - 'bin/'
    - 'obj/'
    - '*.exe'
    - '*.dll'
    - '*.user'
    - '*.suo'

  never_delete:
    - '.git/'
    - '.agent/'
    - '*.sln'
    - '*.csproj'

  never_expose:
    - secrets
    - credentials
    - API keys

repository_boundaries:
  allowed_directories:
    - './dotnet'
    - './tests'
    - './docs'
    - './.agent'

  forbidden_directories:
    - './bin'
    - './obj'
```

---

## Phase 2: Essential Skills & Sub-Agents (Week 2)

### Issue #50: Create CodeReviewAgent Definition

**Title**: Create CodeReviewAgent sub-agent for code quality

**Labels**: `enhancement`, `agents`, `rfc-004`, `phase-2`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create the CodeReviewAgent sub-agent that handles code formatting and static analysis.

**Acceptance Criteria**:

- [ ] `code-review.yaml` created
- [ ] Skills list includes code-format, code-analyze
- [ ] Goals include formatting and analysis
- [ ] Constraints prevent code execution
- [ ] Success criteria include zero critical issues
- [ ] Version 0.1.0

**Files to Create**:

- `.agent/agents/code-review.yaml`

**Code Example**:

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

constraints:
  - Only analyze code, never execute untrusted code
  - Respect existing .editorconfig and formatting rules
  - Flag security issues with HIGH priority

success_criteria:
  - 'All formatting checks pass'
  - 'No critical security issues found'
  - 'Analysis report generated'
```

---

### Issue #51: Create TestingAgent Definition

**Title**: Create TestingAgent sub-agent for test execution

**Labels**: `enhancement`, `agents`, `rfc-004`, `phase-2`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create the TestingAgent sub-agent that handles test execution, coverage, and benchmarks.

**Acceptance Criteria**:

- [ ] `testing.yaml` created
- [ ] Skills list includes dotnet-test
- [ ] Goals include test execution and coverage
- [ ] Constraints prevent skipping tests
- [ ] Success criteria include test pass/fail reporting
- [ ] Version 0.1.0

**Files to Create**:

- `.agent/agents/testing.yaml`

**Code Example**:

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

success_criteria:
  - 'All tests pass or failures clearly reported'
  - 'Coverage data generated (if requested)'
  - 'Benchmark results available (if requested)'
```

---

### Issue #52: Create dotnet-test Skill

**Title**: Create dotnet-test skill for running .NET tests

**Labels**: `enhancement`, `skills`, `rfc-004`, `phase-2`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44, Issue #47 (for pattern reference)

**Description**:
Create the dotnet-test skill with entry file and references for running unit tests, generating coverage, and executing benchmarks.

**Acceptance Criteria**:

- [ ] `SKILL.md` entry file created (~200 lines)
- [ ] YAML front-matter complete
- [ ] `references/run-unit-tests.md` created
- [ ] `references/generate-coverage.md` created
- [ ] `references/run-benchmarks.md` created
- [ ] Cold-start budget maintained
- [ ] Test execution patterns documented

**Files to Create**:

- `.agent/skills/dotnet-test/SKILL.md`
- `.agent/skills/dotnet-test/references/run-unit-tests.md`
- `.agent/skills/dotnet-test/references/generate-coverage.md`
- `.agent/skills/dotnet-test/references/run-benchmarks.md`
- `.agent/skills/dotnet-test/scripts/test-with-coverage.sh`

**Code Example** (SKILL.md):

````markdown
---
name: dotnet-test
version: 0.2.0
kind: cli
description: Run .NET tests (unit, integration), generate coverage, execute benchmarks. Use when task involves testing or quality verification.
inputs:
  test_type: [unit, integration, all, benchmark]
  coverage: [true, false]
  project_path: string # Optional
contracts:
  success: 'All tests pass; coverage/benchmark data generated if requested'
  failure: 'Test failures or execution errors'
---

# .NET Test Skill (Entry Map)

## Quick Start

- **Run all unit tests** → `references/run-unit-tests.md`
- **Generate coverage report** → `references/generate-coverage.md`
- **Run benchmarks** → `references/run-benchmarks.md`

## When to Use

- Running xUnit/NUnit tests
- Generating code coverage
- Executing BenchmarkDotNet benchmarks
- Verifying code quality

## Navigation

1. `references/run-unit-tests.md`
2. `references/generate-coverage.md`
3. `references/run-benchmarks.md`

## Common Patterns

```bash
# Run all tests
dotnet test ./dotnet/PigeonPea.sln

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```
````

---

### Issue #53: Create code-format Skill

**Title**: Create code-format skill for formatting code

**Labels**: `enhancement`, `skills`, `rfc-004`, `phase-2`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44, Issue #47

**Description**:
Create the code-format skill for running dotnet-format, prettier, and other formatting tools.

**Acceptance Criteria**:

- [ ] `SKILL.md` entry file created
- [ ] `references/dotnet-format.md` created
- [ ] `references/prettier-format.md` created
- [ ] `references/fix-all.md` created (runs all formatters)
- [ ] Format verification patterns included
- [ ] Integration with pre-commit documented

**Files to Create**:

- `.agent/skills/code-format/SKILL.md`
- `.agent/skills/code-format/references/dotnet-format.md`
- `.agent/skills/code-format/references/prettier-format.md`
- `.agent/skills/code-format/references/fix-all.md`
- `.agent/skills/code-format/scripts/format-all.sh`

**Code Example** (references/dotnet-format.md):

````markdown
# .NET Code Formatting - Detailed Procedure

## Overview

Formats C# code using dotnet-format according to .editorconfig rules.

## Prerequisites

- .NET SDK installed
- .editorconfig present in ./dotnet

## Standard Format Flow

### 1. Check Formatting

```bash
cd ./dotnet
dotnet format --verify-no-changes
```

### 2. Fix Formatting

```bash
dotnet format
```

### 3. Fix Specific Project

```bash
dotnet format ./console-app/PigeonPea.Console.csproj
```

## Integration with Pre-commit

Formatting runs automatically on commit via pre-commit hooks.

## Common Issues

- **Multiple .editorconfig files**: Ensure hierarchy is correct
- **Conflicting rules**: Check .editorconfig for conflicts
````

---

### Issue #54: Create code-analyze Skill

**Title**: Create code-analyze skill for static analysis

**Labels**: `enhancement`, `skills`, `rfc-004`, `phase-2`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44, Issue #47

**Description**:
Create the code-analyze skill for running static analysis tools, security scans, and dependency checks.

**Acceptance Criteria**:

- [ ] `SKILL.md` entry file created
- [ ] `references/static-analysis.md` created (Roslyn analyzers)
- [ ] `references/security-scan.md` created (gitleaks, etc.)
- [ ] `references/dependency-check.md` created (dotnet list package)
- [ ] Analysis result interpretation guide included

**Files to Create**:

- `.agent/skills/code-analyze/SKILL.md`
- `.agent/skills/code-analyze/references/static-analysis.md`
- `.agent/skills/code-analyze/references/security-scan.md`
- `.agent/skills/code-analyze/references/dependency-check.md`
- `.agent/skills/code-analyze/scripts/analyze.sh`

**Code Example** (references/static-analysis.md):

````markdown
# Static Analysis - Detailed Procedure

## Overview

Runs Roslyn analyzers and code quality checks on .NET code.

## Prerequisites

- .NET SDK with analyzers enabled in .csproj

## Analysis Flow

### 1. Run Build with Analyzers

```bash
dotnet build /p:RunAnalyzers=true
```

### 2. Treat Warnings as Errors (Release)

```bash
dotnet build -c Release /p:TreatWarningsAsErrors=true
```

### 3. Generate Analysis Report

```bash
dotnet build /p:RunAnalyzers=true > analysis.log
```

## Common Analyzers

- **Microsoft.CodeAnalysis.NetAnalyzers**: Built-in analyzers
- **StyleCop.Analyzers**: Code style enforcement
- **SonarAnalyzer.CSharp**: Advanced code quality

## Severity Levels

- **Error**: Must fix before merge
- **Warning**: Should fix
- **Info**: Optional improvement
````

---

## Phase 3: Validation & Automation (Week 3)

### Issue #55: Create Skill Validation Script

**Title**: Create Python script to validate skill manifests

**Labels**: `enhancement`, `automation`, `rfc-004`, `phase-3`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #48 (schemas must exist)

**Description**:
Create a Python script that validates all SKILL.md front-matter against skill.schema.json and checks file size limits.

**Acceptance Criteria**:

- [ ] `scripts/validate_skills.py` created
- [ ] Parses YAML front-matter from SKILL.md files
- [ ] Validates against skill.schema.json using jsonschema
- [ ] Checks entry file ≤ 220 lines
- [ ] Checks reference files ≤ 320 lines
- [ ] Checks cold-start budget ≤ 550 lines
- [ ] Returns exit code 0 on success, 1 on failure
- [ ] Clear error messages for violations

**Files to Create**:

- `scripts/validate_skills.py`

**Code Example**:

```python
#!/usr/bin/env python3
"""Validate skill manifests against schema and size limits."""
import json
import sys
from pathlib import Path
import yaml
from jsonschema import validate, ValidationError

def extract_frontmatter(skill_md_path):
    """Extract YAML front-matter from SKILL.md"""
    with open(skill_md_path) as f:
        content = f.read()

    if not content.startswith('---'):
        raise ValueError(f"Missing front-matter in {skill_md_path}")

    parts = content.split('---', 2)
    if len(parts) < 3:
        raise ValueError(f"Invalid front-matter format in {skill_md_path}")

    return yaml.safe_load(parts[1])

def validate_skill(skill_path, schema_path):
    """Validate a single skill against schema and size limits."""
    skill_md = skill_path / "SKILL.md"

    # Extract and validate front-matter
    front_matter = extract_frontmatter(skill_md)

    with open(schema_path) as f:
        schema = json.load(f)

    try:
        validate(instance=front_matter, schema=schema)
        print(f"✓ {skill_md}: Schema valid")
    except ValidationError as e:
        print(f"✗ {skill_md}: {e.message}")
        return False

    # Check size limits
    with open(skill_md) as f:
        entry_lines = len(f.readlines())

    if entry_lines > 220:
        print(f"✗ {skill_md}: Entry too large ({entry_lines} lines, max 220)")
        return False

    print(f"✓ {skill_md}: Size OK ({entry_lines} lines)")

    # Check first reference
    ref_dir = skill_path / "references"
    if ref_dir.exists():
        refs = list(ref_dir.glob("*.md"))
        if refs:
            with open(refs[0]) as f:
                ref_lines = len(f.readlines())

            if ref_lines > 320:
                print(f"✗ {refs[0]}: Reference too large ({ref_lines} lines, max 320)")
                return False

            total = entry_lines + ref_lines
            if total > 550:
                print(f"✗ Cold-start budget exceeded: {total} lines (max 550)")
                return False

            print(f"✓ Cold-start budget OK: {total} lines")

    return True

def main():
    skills_dir = Path(".agent/skills")
    schema_path = Path(".agent/schemas/skill.schema.json")

    if not skills_dir.exists():
        print("Error: .agent/skills directory not found")
        return 1

    if not schema_path.exists():
        print("Error: skill schema not found")
        return 1

    all_valid = True
    for skill_dir in skills_dir.iterdir():
        if skill_dir.is_dir() and (skill_dir / "SKILL.md").exists():
            print(f"\nValidating {skill_dir.name}...")
            if not validate_skill(skill_dir, schema_path):
                all_valid = False

    return 0 if all_valid else 1

if __name__ == "__main__":
    sys.exit(main())
```

---

### Issue #56: Create Agent Validation Script

**Title**: Create Python script to validate agent manifests

**Labels**: `enhancement`, `automation`, `rfc-004`, `phase-3`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #48

**Description**:
Create a Python script that validates all agent YAML files against schemas.

**Acceptance Criteria**:

- [ ] `scripts/validate_agents.py` created
- [ ] Validates orchestrator.yaml against orchestrator.schema.json
- [ ] Validates sub-agent YAMLs against subagent.schema.json
- [ ] Returns exit code 0 on success, 1 on failure
- [ ] Clear error messages

**Files to Create**:

- `scripts/validate_agents.py`

**Code Example**:

```python
#!/usr/bin/env python3
"""Validate agent manifests against schemas."""
import json
import sys
from pathlib import Path
import yaml
from jsonschema import validate, ValidationError

def validate_agent(agent_path, schema_path, agent_type):
    """Validate a single agent YAML against schema."""
    with open(agent_path) as f:
        agent = yaml.safe_load(f)

    with open(schema_path) as f:
        schema = json.load(f)

    try:
        validate(instance=agent, schema=schema)
        print(f"✓ {agent_path.name}: Valid {agent_type}")
        return True
    except ValidationError as e:
        print(f"✗ {agent_path.name}: {e.message}")
        return False

def main():
    agents_dir = Path(".agent/agents")
    schemas_dir = Path(".agent/schemas")

    if not agents_dir.exists():
        print("Error: .agent/agents directory not found")
        return 1

    all_valid = True

    # Validate orchestrator
    orchestrator = agents_dir / "orchestrator.yaml"
    if orchestrator.exists():
        if not validate_agent(
            orchestrator,
            schemas_dir / "orchestrator.schema.json",
            "orchestrator"
        ):
            all_valid = False

    # Validate sub-agents
    for agent_file in agents_dir.glob("*.yaml"):
        if agent_file.name != "orchestrator.yaml":
            if not validate_agent(
                agent_file,
                schemas_dir / "subagent.schema.json",
                "sub-agent"
            ):
                all_valid = False

    return 0 if all_valid else 1

if __name__ == "__main__":
    sys.exit(main())
```

---

### Issue #57: Create Registry Generation Script

**Title**: Create script to auto-generate AGENTS.md registry

**Labels**: `enhancement`, `automation`, `rfc-004`, `phase-3`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #45, Issue #46, Issue #47

**Description**:
Create a script that reads agent and skill manifests and generates a registry table in AGENTS.md.

**Acceptance Criteria**:

- [ ] `scripts/generate_registry.py` created
- [ ] Reads all agent YAMLs
- [ ] Reads all skill front-matter
- [ ] Generates markdown tables
- [ ] Updates AGENTS.md (preserves manual intro)
- [ ] Tables include: name, description, version, skills

**Files to Create**:

- `scripts/generate_registry.py`

**Code Example**:

```python
#!/usr/bin/env python3
"""Generate AGENTS.md registry from manifests."""
from pathlib import Path
import yaml

def generate_registry():
    agents_dir = Path(".agent/agents")
    skills_dir = Path(".agent/skills")

    # Read agents
    agents = []
    for agent_file in sorted(agents_dir.glob("*.yaml")):
        with open(agent_file) as f:
            agents.append(yaml.safe_load(f))

    # Read skills
    skills = []
    for skill_dir in sorted(skills_dir.iterdir()):
        if (skill_dir / "SKILL.md").exists():
            with open(skill_dir / "SKILL.md") as f:
                content = f.read()
                if content.startswith('---'):
                    fm = yaml.safe_load(content.split('---', 2)[1])
                    skills.append(fm)

    # Generate markdown
    md = "# Agent Infrastructure Registry\n\n"
    md += "## Agents\n\n"
    md += "| Name | Description | Version | Skills |\n"
    md += "|------|-------------|---------|--------|\n"
    for agent in agents:
        skills_list = ", ".join(agent.get('skills', []))
        md += f"| {agent['name']} | {agent['description']} | {agent['version']} | {skills_list} |\n"

    md += "\n## Skills\n\n"
    md += "| Name | Kind | Description | Version |\n"
    md += "|------|------|-------------|--------|\n"
    for skill in skills:
        md += f"| {skill['name']} | {skill['kind']} | {skill['description']} | {skill['version']} |\n"

    # Write to AGENTS.md (append to existing intro)
    agents_md = Path("AGENTS.md")
    if agents_md.exists():
        with open(agents_md) as f:
            existing = f.read()
        # Find where to insert (after first ## heading)
        if "## Agent Infrastructure Registry" in existing:
            parts = existing.split("## Agent Infrastructure Registry", 1)
            final = parts[0].rstrip() + "\n\n" + md
        else:
            final = existing.rstrip() + "\n\n" + md
    else:
        final = md

    with open(agents_md, 'w') as f:
        f.write(final)

    print("✓ AGENTS.md updated with registry")

if __name__ == "__main__":
    generate_registry()
```

---

### Issue #58: Extend Pre-commit Hooks for Agent Validation

**Title**: Add agent/skill validation to pre-commit hooks

**Labels**: `enhancement`, `ci`, `rfc-004`, `phase-3`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #55, Issue #56

**Description**:
Extend the existing pre-commit configuration to run skill and agent validation before commits.

**Acceptance Criteria**:

- [ ] New pre-commit hook added for skill validation
- [ ] New pre-commit hook added for agent validation
- [ ] Hooks run validation scripts
- [ ] Commit fails if validation fails
- [ ] Python dependencies (pyyaml, jsonschema) added to requirements

**Files to Modify**:

- `.pre-commit-config.yaml`

**Files to Create**:

- `requirements-dev.txt` (if not exists)

**Code Example**:

```yaml
# Add to .pre-commit-config.yaml
repos:
  # ... existing hooks ...

  - repo: local
    hooks:
      - id: validate-skills
        name: Validate agent skills
        entry: python scripts/validate_skills.py
        language: python
        files: '^\.agent/skills/.*\.md$'
        pass_filenames: false
        additional_dependencies:
          - pyyaml
          - jsonschema

      - id: validate-agents
        name: Validate agent manifests
        entry: python scripts/validate_agents.py
        language: python
        files: '^\.agent/agents/.*\.yaml$'
        pass_filenames: false
        additional_dependencies:
          - pyyaml
          - jsonschema

      - id: generate-registry
        name: Generate agent registry
        entry: python scripts/generate_registry.py
        language: python
        files: '^\.agent/(agents|skills)/.*\.(yaml|md)$'
        pass_filenames: false
        additional_dependencies:
          - pyyaml
```

---

### Issue #59: Create Taskfile for Convenience Commands

**Title**: Create Taskfile.yml for common agent operations

**Labels**: `enhancement`, `dx`, `rfc-004`, `phase-3`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #55, Issue #56, Issue #57

**Description**:
Create a Taskfile.yml (using Task/go-task) with convenience commands for validating and managing agents/skills.

**Acceptance Criteria**:

- [ ] `Taskfile.yml` created at repo root
- [ ] `task agents:validate` runs agent validation
- [ ] `task skills:validate` runs skill validation
- [ ] `task registry:generate` generates AGENTS.md
- [ ] `task check` runs all validations
- [ ] Tasks documented with descriptions

**Files to Create**:

- `Taskfile.yml`

**Code Example**:

```yaml
# Taskfile.yml
version: '3'

tasks:
  skills:validate:
    desc: Validate all agent skills
    cmds:
      - python scripts/validate_skills.py

  agents:validate:
    desc: Validate all agent manifests
    cmds:
      - python scripts/validate_agents.py

  registry:generate:
    desc: Generate AGENTS.md registry from manifests
    cmds:
      - python scripts/generate_registry.py

  check:
    desc: Run all validations
    deps:
      - skills:validate
      - agents:validate
      - registry:generate

  dotnet:build:
    desc: Build .NET solution
    dir: ./dotnet
    cmds:
      - dotnet build PigeonPea.sln

  dotnet:test:
    desc: Run .NET tests
    dir: ./dotnet
    cmds:
      - dotnet test PigeonPea.sln

  dotnet:format:
    desc: Format .NET code
    dir: ./dotnet
    cmds:
      - dotnet format

  default:
    desc: Show available tasks
    cmds:
      - task --list
```

---

## Phase 4: Optional Enhancements (Week 4)

### Issue #60: Add Provider-Specific Hints (Optional)

**Title**: Create provider-specific hint files for Claude/Copilot

**Labels**: `enhancement`, `optional`, `rfc-004`, `phase-4`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44

**Description**:
Create optional provider-specific hint files to guide Claude and GitHub Copilot agents with platform-specific optimizations.

**Acceptance Criteria**:

- [ ] `claude.yaml` created with Claude-specific hints
- [ ] `copilot.yaml` created with Copilot-specific hints
- [ ] Hints include context window sizes, tool preferences
- [ ] Documentation explains when these are used

**Files to Create**:

- `.agent/providers/claude.yaml`
- `.agent/providers/copilot.yaml`
- `.agent/providers/README.md`

**Code Example** (claude.yaml):

```yaml
name: ClaudeProviderHints
description: Claude-specific optimizations and preferences
version: 0.1.0

context:
  max_tokens: 200000
  prefer_concise_responses: true

tools:
  prefer:
    - Read # Prefer Read over Bash cat
    - Edit # Prefer Edit over sed/awk
    - Glob # Prefer Glob over find

skills:
  loading_strategy: progressive # Load entry first, then references
  max_initial_load: 500 # lines

sub_agents:
  delegation_preference: specialized # Prefer specialized sub-agents
```

---

### Issue #61: Create Nuke Build Skill (Optional)

**Title**: Create nuke-build skill for Nuke build orchestration

**Labels**: `enhancement`, `optional`, `skills`, `rfc-004`, `phase-4`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #44, Issue #47

**Description**:
Create the nuke-build skill for using Nuke build system (only if Nuke is integrated into the project).

**Acceptance Criteria**:

- [ ] Only create if Nuke is used in the project
- [ ] `SKILL.md` entry file created
- [ ] References for Nuke targets, bootstrapping
- [ ] Integration with dotnet-build documented

**Files to Create** (if applicable):

- `.agent/skills/nuke-build/SKILL.md`
- `.agent/skills/nuke-build/references/setup-nuke.md`
- `.agent/skills/nuke-build/references/build-targets.md`

---

### Issue #62: Update AGENTS.md Documentation

**Title**: Update AGENTS.md with agent infrastructure overview

**Labels**: `documentation`, `rfc-004`, `phase-4`

**RFC Reference**: [RFC-004: Agent Infrastructure Enhancement](rfcs/004-agent-infrastructure-enhancement.md)

**Dependencies**: Issue #57 (registry generation)

**Description**:
Update AGENTS.md to include an overview of the agent infrastructure, how to use it, and the auto-generated registry.

**Acceptance Criteria**:

- [ ] Introduction section explains agent architecture
- [ ] Sub-agents section lists all sub-agents
- [ ] Skills section lists all skills
- [ ] Registry auto-generated and inserted
- [ ] Examples of how agents use skills
- [ ] Markdown formatting consistent

**Files to Modify**:

- `AGENTS.md`

**Code Example**:

```markdown
# Agent Infrastructure

This document describes the agent infrastructure for the pigeon-pea project.

## Overview

The `.agent` directory contains a hierarchical agent system:

- **Orchestrator**: Routes requests to specialized sub-agents
- **Sub-Agents**: Specialized agents for build, test, code review
- **Skills**: Atomic capabilities invoked by sub-agents
- **Schemas**: Validation for agent/skill manifests
- **Policies**: Guardrails and standards

## Architecture
```

User Request → Orchestrator → Sub-Agent → Skill → Action

```

## How It Works

1. User makes a request (e.g., "Build the solution")
2. Orchestrator analyzes intent and routes to DotNetBuildAgent
3. DotNetBuildAgent invokes dotnet-build skill
4. Skill loads entry + relevant reference (progressive disclosure)
5. Skill executes build commands
6. Results returned to user

## Agent Infrastructure Registry

<!-- Auto-generated by scripts/generate_registry.py -->

### Agents

| Name | Description | Version | Skills |
|...

### Skills

| Name | Kind | Description | Version |
|...
```

---

## Summary

**Total: 19 issues** for RFC-004

- **Phase 1 (Core Structure)**: 6 issues
- **Phase 2 (Skills & Sub-Agents)**: 5 issues
- **Phase 3 (Validation & Automation)**: 5 issues
- **Phase 4 (Optional)**: 3 issues

Each issue is:

- **Moderate in scope** (1-3 days of work)
- **Clear acceptance criteria** for definition of done
- **Code examples** for implementation guidance
- **Specific file paths** for creation/modification
- **Dependencies** clearly listed
- **Suitable for GitHub Coding Agents** to implement autonomously
