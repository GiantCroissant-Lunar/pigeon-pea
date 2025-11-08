# Agent Infrastructure

This document describes the agent infrastructure for the pigeon-pea project.

## Overview

The `.agent` directory contains all agent-related configurations, rules, commands, workflows, and adapters. This structured approach ensures consistent agent behavior and maintainable automation.

## Directory Structure

```
.agent/
├── rules/          # Agent behavior rules and constraints
├── commands/       # Custom executable commands
├── workflows/      # Multi-step process definitions
└── adapters/       # External system integrations
```

## Components

### Rules

Rules define how agents should behave, what constraints they must follow, and what policies govern their actions.

**Location:** [`.agent/rules/`](.agent/rules/)

**Examples:**
- [`.agent/rules/git-commit-rules.md`](.agent/rules/git-commit-rules.md) - **Git and commit requirements (CRITICAL: commits must pass pre-commit hooks)**
- [`.agent/rules/code-quality.md`](.agent/rules/code-quality.md) - Code quality and standards enforcement

**Documentation:** [`.agent/rules/README.md`](.agent/rules/README.md)

### Commands

Commands are executable operations that agents can invoke to perform specific tasks.

**Location:** [`.agent/commands/`](.agent/commands/)

**Examples:**
- [`.agent/commands/run-tests.yaml`](.agent/commands/run-tests.yaml) - Test execution across languages

**Documentation:** [`.agent/commands/README.md`](.agent/commands/README.md)

### Workflows

Workflows orchestrate multiple steps and decisions into cohesive processes.

**Location:** [`.agent/workflows/`](.agent/workflows/)

**Examples:**
- [`.agent/workflows/feature-development.yaml`](.agent/workflows/feature-development.yaml) - Feature development lifecycle

**Documentation:** [`.agent/workflows/README.md`](.agent/workflows/README.md)

### Adapters

Adapters provide interfaces between agents and external systems, APIs, or services.

**Location:** [`.agent/adapters/`](.agent/adapters/)

**Examples:**
- [`.agent/adapters/github-adapter.yaml`](.agent/adapters/github-adapter.yaml) - GitHub API integration

**Documentation:** [`.agent/adapters/README.md`](.agent/adapters/README.md)

## Adding New Components

### Adding a Rule

1. Create a new file in [`.agent/rules/`](.agent/rules/)
2. Use `.md` for documentation-style rules or `.yaml` for structured definitions
3. Reference the rule in this document

### Adding a Command

1. Create a new file in [`.agent/commands/`](.agent/commands/)
2. Use appropriate format (`.yaml`, `.sh`, `.ps1`, `.py`)
3. Ensure command is documented with clear description and parameters
4. Add reference in this document

### Adding a Workflow

1. Create a workflow definition in [`.agent/workflows/`](.agent/workflows/)
2. Define clear stages and steps
3. Include conditions and error handling
4. Document in this file

### Adding an Adapter

1. Create adapter configuration in [`.agent/adapters/`](.agent/adapters/)
2. Define capabilities and API endpoints
3. Include authentication and retry policies
4. Reference in this document

## Integration

### With Pre-commit Hooks

Agent commands can leverage the existing pre-commit infrastructure defined in [`.pre-commit-config.yaml`](.pre-commit-config.yaml).

### With CI/CD

Workflows can be integrated with GitHub Actions or other CI/CD systems to automate development processes.

### With Development Tools

Adapters enable agents to interact with:
- GitHub (issues, PRs, commits)
- Build systems (.NET, npm, Python)
- Testing frameworks
- Code quality tools

## Best Practices

1. **Keep it organized:** Use the appropriate subdirectory for each component type
2. **Document everything:** Include clear descriptions and examples
3. **Use version control:** Track changes to agent configurations
4. **Test before committing:** Validate configurations work as expected
5. **Reference by relative path:** Always use relative paths when referencing files
6. **Follow naming conventions:** Use descriptive, kebab-case names

## Related Documentation

- [CLAUDE.md](CLAUDE.md) - Claude-specific agent configuration
- [README.md](README.md) - Project overview and setup
- [`.pre-commit-config.yaml`](.pre-commit-config.yaml) - Pre-commit hooks configuration
