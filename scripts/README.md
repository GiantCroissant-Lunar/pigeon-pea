# Scripts

This directory contains automation scripts for the pigeon-pea project.

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
