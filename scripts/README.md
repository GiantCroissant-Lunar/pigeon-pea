# Scripts

This directory contains automation scripts for the pigeon-pea project.

## Prerequisites

Install development dependencies before running scripts:

```bash
pip install -r requirements/dev.txt
```

Required packages:

- `pyyaml>=6.0` - YAML parsing for agent/skill manifests
- `jsonschema>=4.0` - Schema validation for agent/skill schemas

**Note**: Pre-commit hooks automatically install these dependencies when needed.

## Scripts

### validate_skills.py

Validates skill manifests against schemas and size limits.

**Usage**:

```bash
python3 scripts/validate_skills.py
```

**What it validates**:

- YAML front-matter against `skill.schema.json`
- Entry file size ≤ 220 lines
- Reference file sizes ≤ 320 lines each
- Cold-start budget (entry + first reference) ≤ 550 lines

**Exit codes**: `0` = success, `1` = validation errors

### validate_agents.py

Validates agent manifests with two-phase cross-validation.

**Usage**:

```bash
python3 scripts/validate_agents.py
```

**What it validates**:

- Agent YAML against schemas (orchestrator.schema.json, subagent.schema.json)
- Sub-agents reference existing skills (checks `.agent/skills/{skill}/SKILL.md`)
- Orchestrator references existing sub-agents
- Routing rules target declared sub-agents

**Exit codes**: `0` = success, `1` = validation errors

## generate_registry.py

Auto-generates the AGENTS.md registry from agent and skill manifests.

### Purpose

This script reads all agent YAML files and skill SKILL.md files, extracts their metadata, and generates markdown tables in AGENTS.md. It preserves any manual introduction content in AGENTS.md.

### Usage

```bash
python3 scripts/generate_registry.py
```

The script automatically:

1. Reads all agent YAML files from `.agent/agents/`
2. Parses YAML front-matter from all SKILL.md files in `.agent/skills/`
3. Generates markdown tables for agents and skills
4. Updates AGENTS.md while preserving the manual introduction

### Output

```
✓ Successfully updated /path/to/AGENTS.md
  - 4 agents
  - 2 skills
```

### Requirements

- Python 3.7+
- PyYAML (pre-installed in the development environment)

### Exit Codes

- `0`: Success
- `1`: Error (missing directories, invalid YAML, etc.)

### Generated Registry Format

The script generates two tables in AGENTS.md:

#### Agents Table

| Name | Description | Version | Skills |
| ---- | ----------- | ------- | ------ |
| ...  | ...         | ...     | ...    |

#### Skills Table

| Name | Kind | Description | Version |
| ---- | ---- | ----------- | ------- |
| ...  | ...  | ...         | ...     |

### Idempotency

The script is idempotent and can be run multiple times safely. It will:

- Replace the existing registry section in AGENTS.md
- Preserve all content before the "## Agent Infrastructure Registry" heading
- Maintain consistent formatting

### Error Handling

The script validates:

- `.agent/agents/` directory exists
- `.agent/skills/` directory exists
- YAML files are valid
- SKILL.md files have proper front-matter

Warnings are printed to stderr for files that cannot be parsed, but the script continues processing other files.

### Integration

This script can be integrated into:

- Pre-commit hooks (to auto-update registry on commit)
- CI/CD pipelines (to validate registry is up-to-date)
- Task automation tools (see `Taskfile.yml`)

### Related Files

- `.agent/agents/` - Agent YAML manifests
- `.agent/skills/` - Skill SKILL.md files
- `AGENTS.md` - Target file for registry output
- `.agent/schemas/skill.schema.json` - Skill front-matter schema
- `.agent/schemas/subagent.schema.json` - Agent manifest schema

## Documentation Management Scripts (Phase 1)

### validate-docs.py

Validates documentation front-matter and generates the registry (RFC-012).

**Usage**:
```bash
# Full validation and registry generation
python scripts/validate-docs.py

# Pre-commit mode (validation only)
python scripts/validate-docs.py --pre-commit
```

### generate-frontmatter.py

Automatically generates YAML front-matter for markdown files that lack it.

**Usage**:
```bash
# Preview (dry-run)
python scripts/generate-frontmatter.py

# Apply front-matter
python scripts/generate-frontmatter.py --apply

# Save report
python scripts/generate-frontmatter.py --output scripts/frontmatter-report.json
```

### migrate-docs.py

Relocates files to correct directories based on doc_type and updates cross-references.

**Usage**:
```bash
# Preview migrations
python scripts/migrate-docs.py --report scripts/frontmatter-report.json

# Apply migrations
python scripts/migrate-docs.py --report scripts/frontmatter-report.json --apply
```

### cleanup-inbox.py

Enforces inbox retention policy and auto-archives conversation dumps.

**Usage**:
```bash
# Preview cleanup
python scripts/cleanup-inbox.py

# Apply cleanup (30-day retention)
python scripts/cleanup-inbox.py --apply

# Custom retention period
python scripts/cleanup-inbox.py --retention-days 14 --apply
```

**See also**: [Documentation Organization Enhancement Plan](../docs/rfcs/012-documentation-organization-management.md)

## RFC-Task Integration Scripts (Phase 2)

### sync-tasks.py

Synchronizes RFC implementation status with task-master tasks.

**Usage**:
```bash
# Preview sync (requires .taskmaster/tasks.json)
python scripts/sync-tasks.py

# Apply updates to RFCs
python scripts/sync-tasks.py --apply

# Generate RFC-task mapping
python scripts/sync-tasks.py --generate-map
```

**Features**:
- Extracts RFC references from tasks
- Calculates implementation status and completion
- Updates RFC front-matter automatically
- Generates `docs/index/rfc-task-map.json`

### generate-dashboard.py

Generates comprehensive RFC implementation dashboard.

**Usage**:
```bash
# Generate dashboard
python scripts/generate-dashboard.py

# Custom output location
python scripts/generate-dashboard.py --output docs/STATUS.md
```

**Features**:
- Status summaries and metrics
- Active/blocked/completed RFCs
- Dependency graphs (Mermaid)
- Implementation timeline (Gantt)
- Progress bars and completion tracking

**Output**: `docs/DASHBOARD.md`

## Quality Assurance Scripts (Phase 3)

### generate-quality-report.py

Generates comprehensive quality reports with scoring metrics.

**Usage**:
```bash
# Generate full quality report
python scripts/generate-quality-report.py

# Filter by minimum score
python scripts/generate-quality-report.py --min-score 60
```

**Quality Metrics** (0-100 each):
- **Completeness**: Required and optional fields filled
- **Freshness**: Recently updated (age-based scoring)
- **Linkage**: Cross-references to other docs
- **Clarity**: Summary quality, structure, content

**Output**: `docs/index/quality-report.md`

**Report Sections**:
- Summary statistics and grade distribution
- Top quality documents
- Documents needing improvement
- Orphaned documents (low linkage)
- Stale documents (not updated 90+ days)

## Navigation and Discovery Scripts (Phase 4)

### generate-index.py

Generates comprehensive documentation index with hierarchical navigation.

**Usage**:
```bash
# Generate main index
python scripts/generate-index.py

# Generate with directory READMEs
python scripts/generate-index.py --generate-dir-readmes
```

**Features**:
- Hierarchical table of contents
- Grouping by document type and status
- Recently updated section
- Most referenced documents
- Topic-based indexes (by tags)

**Output**: `docs/INDEX.md`

### visualize-deps.py

Creates Mermaid dependency visualizations.

**Usage**:
```bash
# Generate dependency diagrams
python scripts/visualize-deps.py
```

**Features**:
- RFC dependency graphs
- Implementation status flowcharts
- Color-coded status indicators
- Document relationship maps

**Output**: `docs/DEPENDENCIES.md`
