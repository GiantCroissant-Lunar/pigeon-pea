# GitHub Label Strategy

This document defines the scalable label strategy for the pigeon-pea project.

## Problem

**Before**: Using RFC-specific labels like `rfc-004`, `phase-1`, `phase-2`, etc.
- ❌ Doesn't scale - hundreds of RFCs = hundreds of labels
- ❌ Labels become cluttered and unusable
- ❌ Hard to search/filter effectively

**After**: Use generic labels + milestones/projects for tracking
- ✅ Scales to any number of RFCs
- ✅ Clean, manageable label set
- ✅ Use milestones for RFC-specific tracking

---

## Label Categories

### 1. Type Labels (What kind of work)

| Label | Description | Color | When to Use |
|-------|-------------|-------|-------------|
| `enhancement` | New feature or request | ![#a2eeef](https://via.placeholder.com/15/a2eeef/000000?text=+) `#a2eeef` | New functionality, improvements |
| `bug` | Something isn't working | ![#d73a4a](https://via.placeholder.com/15/d73a4a/000000?text=+) `#d73a4a` | Fixes for broken behavior |
| `documentation` | Improvements or additions to documentation | ![#0075ca](https://via.placeholder.com/15/0075ca/000000?text=+) `#0075ca` | README, docs, comments |
| `question` | Further information is requested | ![#d876e3](https://via.placeholder.com/15/d876e3/000000?text=+) `#d876e3` | Clarifications needed |
| `duplicate` | This issue or pull request already exists | ![#cfd3d7](https://via.placeholder.com/15/cfd3d7/000000?text=+) `#cfd3d7` | Duplicate work |
| `invalid` | This doesn't seem right | ![#e4e669](https://via.placeholder.com/15/e4e669/000000?text=+) `#e4e669` | Won't be addressed |
| `wontfix` | This will not be worked on | ![#ffffff](https://via.placeholder.com/15/ffffff/000000?text=+) `#ffffff` | Intentionally not fixing |

### 2. Status Labels (Work status)

| Label | Description | Color | When to Use |
|-------|-------------|-------|-------------|
| `good first issue` | Good for newcomers | ![#7057ff](https://via.placeholder.com/15/7057ff/000000?text=+) `#7057ff` | Easy onboarding tasks |
| `help wanted` | Extra attention is needed | ![#008672](https://via.placeholder.com/15/008672/000000?text=+) `#008672` | Community help needed |
| `in-progress` | Currently being worked on | ![#fbca04](https://via.placeholder.com/15/fbca04/000000?text=+) `#fbca04` | Active work |
| `blocked` | Waiting on dependencies | ![#d93f0b](https://via.placeholder.com/15/d93f0b/000000?text=+) `#d93f0b` | Cannot proceed yet |

### 3. Domain Labels (What area of the codebase)

| Label | Description | Color | When to Use |
|-------|-------------|-------|-------------|
| `agents` | Agent definitions and configuration | ![#c5def5](https://via.placeholder.com/15/c5def5/000000?text=+) `#c5def5` | Sub-agent YAML files |
| `skills` | Agent skills and capabilities | ![#c5def5](https://via.placeholder.com/15/c5def5/000000?text=+) `#c5def5` | SKILL.md files |
| `schemas` | JSON schemas and validation | ![#c5def5](https://via.placeholder.com/15/c5def5/000000?text=+) `#c5def5` | *.schema.json files |
| `policies` | Agent policies and guardrails | ![#c5def5](https://via.placeholder.com/15/c5def5/000000?text=+) `#c5def5` | Policy YAML files |
| `automation` | Automation scripts and tooling | ![#0e8a16](https://via.placeholder.com/15/0e8a16/000000?text=+) `#0e8a16` | Python scripts, validation |
| `ci` | CI/CD pipeline improvements | ![#0e8a16](https://via.placeholder.com/15/0e8a16/000000?text=+) `#0e8a16` | GitHub Actions, pre-commit |
| `dx` | Developer experience improvements | ![#1d76db](https://via.placeholder.com/15/1d76db/000000?text=+) `#1d76db` | Taskfile, tooling |
| `infrastructure` | Infrastructure and tooling improvements | ![#0e8a16](https://via.placeholder.com/15/0e8a16/000000?text=+) `#0e8a16` | Directory structure, setup |

### 4. Special Labels

| Label | Description | Color | When to Use |
|-------|-------------|-------|-------------|
| `rfc` | Request for Comments implementation | ![#d4c5f9](https://via.placeholder.com/15/d4c5f9/000000?text=+) `#d4c5f9` | Implementing an RFC |
| `implementation` | Implementation work | ![#0e8a16](https://via.placeholder.com/15/0e8a16/000000?text=+) `#0e8a16` | Actual coding work |
| `optional` | Optional enhancements | ![#e99695](https://via.placeholder.com/15/e99695/000000?text=+) `#e99695` | Nice-to-have features |

---

## Using Milestones for RFC Tracking

Instead of `rfc-004`, `rfc-005` labels, use **milestones**:

### Creating a Milestone

**Via GitHub UI**: https://github.com/GiantCroissant-Lunar/pigeon-pea/milestones/new

**Example Milestone**:
- **Title**: `RFC-004: Agent Infrastructure`
- **Description**: Implementation of RFC-004: Agent Infrastructure Enhancement - sub-agents, skills, schemas, validation
- **Due Date**: 2025-12-31 (optional)

### Benefits of Milestones

✅ **Scalable**: Unlimited milestones, no label clutter
✅ **Progress tracking**: Shows % completion
✅ **Due dates**: Can set target completion dates
✅ **Filtering**: Easy to view all issues for one RFC
✅ **Burndown**: See remaining work at a glance

---

## Label Assignment Examples

### Example 1: Create orchestrator agent

**Before** (non-scalable):
```
Labels: enhancement, agents, rfc-004, phase-1
```

**After** (scalable):
```
Labels: enhancement, agents, rfc, implementation
Milestone: RFC-004: Agent Infrastructure
```

### Example 2: Create validation script

**Before**:
```
Labels: enhancement, automation, rfc-004, phase-3
```

**After**:
```
Labels: enhancement, automation, rfc, implementation
Milestone: RFC-004: Agent Infrastructure
```

### Example 3: Optional provider hints

**Before**:
```
Labels: enhancement, optional, rfc-004, phase-4
```

**After**:
```
Labels: enhancement, optional, rfc
Milestone: RFC-004: Agent Infrastructure
```

---

## Searching and Filtering

### Find all RFC-004 issues
```
milestone:"RFC-004: Agent Infrastructure"
```

### Find all agent-related issues
```
label:agents
```

### Find all RFC implementation work
```
label:rfc is:open
```

### Find all in-progress RFC-004 issues
```
milestone:"RFC-004: Agent Infrastructure" label:in-progress
```

### Find all automation tasks
```
label:automation is:open
```

---

## Label Lifecycle

### When Creating an Issue

1. **Add type label**: enhancement, bug, documentation, etc.
2. **Add domain label(s)**: agents, skills, schemas, etc.
3. **Add rfc label** if implementing an RFC
4. **Add implementation label** if it's actual coding work
5. **Assign to milestone** for RFC tracking
6. **(Optional)** Add optional if it's not required

### When Work Starts

Add `in-progress` label when someone starts working on it.

### When Blocked

Add `blocked` label and comment explaining the blocker.

### When Complete

No need to remove labels - closing the issue is sufficient.

---

## Migration from Old Labels

### Script to Update Existing Issues

```bash
#!/bin/bash
# Update labels for RFC-004 issues

# Remove non-scalable labels
for issue in {94..112}; do
  gh issue edit $issue --remove-label "rfc-004"
  gh issue edit $issue --remove-label "phase-1"
  gh issue edit $issue --remove-label "phase-2"
  gh issue edit $issue --remove-label "phase-3"
  gh issue edit $issue --remove-label "phase-4"

  # Add generic labels
  gh issue edit $issue --add-label "rfc"
  gh issue edit $issue --add-label "implementation"
done
```

### Then Create Milestone

1. Go to: https://github.com/GiantCroissant-Lunar/pigeon-pea/milestones/new
2. Title: `RFC-004: Agent Infrastructure`
3. Description: Implementation of RFC-004
4. Due Date: (optional)
5. Create milestone
6. Bulk-assign issues #94-#112 to the milestone

---

## Best Practices

### ✅ DO

- Use generic, reusable labels
- Use milestones for tracking specific initiatives (RFCs, sprints, releases)
- Keep label count manageable (< 30 labels total)
- Use consistent color coding (similar purpose = similar color)
- Document label meanings

### ❌ DON'T

- Create initiative-specific labels (rfc-001, rfc-002, sprint-1, etc.)
- Use phase-based labels (phase-1, phase-2, etc.) - use milestones instead
- Over-label (max 5-6 labels per issue)
- Create labels that will only be used once
- Use similar labels with slightly different names

---

## Current Label Inventory

### Type Labels (8)
- enhancement, bug, documentation, question, duplicate, invalid, wontfix, rfc

### Status Labels (4)
- good first issue, help wanted, in-progress, blocked

### Domain Labels (8)
- agents, skills, schemas, policies, automation, ci, dx, infrastructure

### Special Labels (2)
- implementation, optional

**Total: 22 labels** (manageable and scalable)

---

## Future Additions

When adding new labels, ask:
1. **Is it reusable?** Will it apply to multiple issues over time?
2. **Is it generic?** Or is it project/RFC-specific?
3. **Is it necessary?** Could a milestone or project board work instead?
4. **Does it fit a category?** Type, Status, Domain, or Special?

If answers are yes, yes, yes, yes → add the label.
If any answer is no → consider alternatives (milestones, projects, comments).

---

## Related Documents

- [RFC_004_EXECUTION_ORDER.md](RFC_004_EXECUTION_ORDER.md) - Issue execution order
- [AGENTS.md](../AGENTS.md) - Agent infrastructure overview
- [RFC-004](rfcs/004-agent-infrastructure-enhancement.md) - Full RFC specification
