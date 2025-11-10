# GitHub Issues for Plugin System Implementation

This directory contains detailed issue descriptions for implementing the plugin system and project restructure as defined in RFC-005 and RFC-006.

## Issues Overview

| Issue | Title | Labels | Effort | Dependencies |
|-------|-------|--------|--------|--------------|
| #1 | [RFC-005] Phase 1: Migrate project structure | `restructure`, `rfc-005`, `phase-1`, `breaking-change` | 1-2 days | None |
| #2 | [RFC-005] Phase 2: Create contract projects | `contracts`, `rfc-005`, `phase-2`, `infrastructure` | 2-3 days | Issue #1 |
| #3 | [RFC-006] Phase 1: Implement core plugin system | `plugin-system`, `rfc-006`, `phase-1`, `infrastructure` | 3-5 days | Issue #2 |
| #4 | [RFC-006] Phase 2: Integrate game events | `plugin-system`, `rfc-006`, `phase-2`, `game-logic` | 2-3 days | Issue #3 |
| #5 | [RFC-006] Phase 3: Create rendering plugin PoC | `plugin-system`, `rfc-006`, `phase-3`, `rendering` | 3-4 days | Issue #4 |

**Total Estimated Effort:** 11-17 days (2.2-3.4 weeks) sequential, 9-14 days (1.8-2.8 weeks) with parallelization

## Dependency Graph

```
Issue #1: Migrate Structure
    ↓
Issue #2: Create Contracts
    ↓
Issue #3: Plugin System
    ↓
Issue #4: Game Events
    ↓
Issue #5: Rendering Plugin
```

## Creating GitHub Issues

### Option 1: Manual Creation (Recommended)

1. Go to https://github.com/GiantCroissant-Lunar/pigeon-pea/issues/new
2. Copy the title from the issue file (first heading, without the `#` prefix)
3. Copy the entire content below the title as the issue body
4. Add the labels specified in the **Labels:** line
5. Create the issue

Repeat for all 5 issues.

### Option 2: GitHub CLI

If you have GitHub CLI (`gh`) installed and authenticated:

```bash
# From the repository root
cd docs/rfcs/issues

# Create Issue #1
gh issue create \
  --title "[RFC-005] Phase 1: Migrate project structure to new organization" \
  --label "restructure,rfc-005,phase-1,breaking-change" \
  --body-file issue-001-migrate-structure.md

# Create Issue #2
gh issue create \
  --title "[RFC-005] Phase 2: Create contract projects for plugin system" \
  --label "contracts,rfc-005,phase-2,infrastructure" \
  --body-file issue-002-create-contracts.md

# Create Issue #3
gh issue create \
  --title "[RFC-006] Phase 1: Implement core plugin infrastructure" \
  --label "plugin-system,rfc-006,phase-1,infrastructure" \
  --body-file issue-003-plugin-system.md

# Create Issue #4
gh issue create \
  --title "[RFC-006] Phase 2: Integrate game events with plugin system" \
  --label "plugin-system,rfc-006,phase-2,game-logic" \
  --body-file issue-004-game-events.md

# Create Issue #5
gh issue create \
  --title "[RFC-006] Phase 3: Create rendering plugin proof of concept" \
  --label "plugin-system,rfc-006,phase-3,rendering" \
  --body-file issue-005-rendering-plugin.md
```

### Option 3: Script for Bulk Creation

Create a script `create-issues.sh`:

```bash
#!/bin/bash

# Array of issues: "title|labels|file"
issues=(
  "[RFC-005] Phase 1: Migrate project structure to new organization|restructure,rfc-005,phase-1,breaking-change|issue-001-migrate-structure.md"
  "[RFC-005] Phase 2: Create contract projects for plugin system|contracts,rfc-005,phase-2,infrastructure|issue-002-create-contracts.md"
  "[RFC-006] Phase 1: Implement core plugin infrastructure|plugin-system,rfc-006,phase-1,infrastructure|issue-003-plugin-system.md"
  "[RFC-006] Phase 2: Integrate game events with plugin system|plugin-system,rfc-006,phase-2,game-logic|issue-004-game-events.md"
  "[RFC-006] Phase 3: Create rendering plugin proof of concept|plugin-system,rfc-006,phase-3,rendering|issue-005-rendering-plugin.md"
)

for issue in "${issues[@]}"; do
  IFS='|' read -r title labels file <<< "$issue"
  echo "Creating issue: $title"
  gh issue create --title "$title" --label "$labels" --body-file "$file"
done

echo "All issues created!"
```

Then run:
```bash
chmod +x create-issues.sh
./create-issues.sh
```

## Issue Files

- [`issue-001-migrate-structure.md`](issue-001-migrate-structure.md) - Migrate project structure
- [`issue-002-create-contracts.md`](issue-002-create-contracts.md) - Create contract projects
- [`issue-003-plugin-system.md`](issue-003-plugin-system.md) - Implement plugin system
- [`issue-004-game-events.md`](issue-004-game-events.md) - Integrate game events
- [`issue-005-rendering-plugin.md`](issue-005-rendering-plugin.md) - Create rendering plugin

## Assignment Strategy

### Sequential Execution
- Assign one agent to handle all issues in order
- Simplest coordination
- Timeline: 11-17 days

### Parallel Execution
- Agent A: Issue #1
- Agent B: Issue #2 (starts after #1)
- Agent C: Issues #3, #4, #5 (starts after #2)
- Timeline: 9-14 days

### Phased Execution
- Phase 1: Issues #1-#2 (one agent)
- Phase 2: Issues #3-#5 (same or different agent)
- Balanced approach

## Tracking Progress

After creating issues:
1. Create a project board or milestone
2. Link issues to the milestone
3. Track progress via GitHub project management
4. Update issue status as work progresses

## Related Documentation

- [RFC-005: Project Structure Reorganization](../rfc-005-project-structure-reorganization.md)
- [RFC-006: Plugin System Architecture](../rfc-006-plugin-system-architecture.md)
- [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md)
- [PLUGIN_SYSTEM_ANALYSIS.md](../../PLUGIN_SYSTEM_ANALYSIS.md)

## Questions?

If you have questions about any issue:
1. Check the related RFC for detailed context
2. Review the PLUGIN_SYSTEM_ANALYSIS.md for architecture details
3. Ask in the issue comments after creation
