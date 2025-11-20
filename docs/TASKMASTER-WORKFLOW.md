# Task-Master Workflow Integration

**Purpose**: Seamlessly integrate RFC-centric development with task-master task-based execution.

## Overview

This document defines workflows for converting RFCs to tasks, tracking implementation status, and syncing between the RFC system and task-master.

## Workflow 1: RFC to Tasks

### When to Use

- RFC has been approved and is ready for implementation
- Need to break down RFC into actionable tasks
- Want to track implementation progress

### Process

1. **Read the RFC**
   ```bash
   # View RFC
   cat docs/rfcs/042-game-services-architecture.md
   ```

2. **Create Task Template**
   ```markdown
   # Implement RFC-00042: Game Services Architecture
   
   ## Context
   RFC: docs/rfcs/042-game-services-architecture.md
   Status: Approved, ready for implementation
   
   ## Objective
   Implement the six core game services as defined in RFC-00042
   
   ## Tasks
   - [ ] Create service contracts
   - [ ] Implement GameplayLoop service
   - [ ] Implement WorldManagement service
   - [ ] Implement Stats service
   - [ ] Implement Inventory service
   - [ ] Implement Quest service
   - [ ] Implement Dialogue service
   - [ ] Write integration tests
   - [ ] Update documentation
   
   ## Success Criteria
   - All services implemented per RFC spec
   - Tests passing
   - Documentation updated
   
   ## Related
   - RFC-00042
   - RFC-00013 (Plugin Architecture dependency)
   ```

3. **Update RFC Implementation Status**
   ```yaml
   # In RFC front-matter
   implementation:
     status: 'in-progress'
     completion: 0
     tasks: ['task-456']  # Add task ID
     started: '2025-11-20'
   ```

4. **Sync Status**
   ```bash
   python scripts/sync-tasks.py --apply
   python scripts/generate-dashboard.py
   ```

## Workflow 2: Task to RFC

### When to Use

- Implementing a feature that needs design documentation
- Task reveals need for architectural decision
- Want to document design before implementation

### Process

1. **Identify Need for RFC**
   - Task is blocked by design decision
   - Implementation approach needs team review
   - Architectural impact is significant

2. **Create RFC Draft**
   ```bash
   # Find next RFC number
   ls docs/rfcs/*.md | grep -E '^[0-9]' | sort -n | tail -1
   
   # Create draft (assuming next is 043)
   cat > docs/_inbox/043-feature-name.md << 'EOF'
   ---
   doc_id: 'RFC-00043'
   title: 'Feature Name Architecture'
   doc_type: 'rfc'
   status: 'draft'
   canonical: true
   created: '2025-11-20'
   tags: ['architecture', 'feature']
   summary: 'Architecture design for feature name'
   implements: 'PRD-00001'  # If implementing a PRD
   ---
   
   # RFC: Feature Name Architecture
   
   ## Summary
   ...
   EOF
   ```

3. **Link Task to RFC**
   ```markdown
   # In task description
   Design documented in RFC-00043 (draft)
   
   ## Blocked By
   - RFC-00043 needs approval
   ```

4. **After RFC Approval**
   ```yaml
   # Update RFC
   implementation:
     status: 'in-progress'
     tasks: ['task-789']
   ```

## Workflow 3: Sync RFC Status

### Automated Sync

Run periodically to keep RFC status in sync with task progress:

```bash
# Sync all RFCs with tasks
python scripts/sync-tasks.py --apply

# Sync specific RFC
python scripts/sync-tasks.py --rfc-id RFC-00042 --apply

# Generate updated dashboard
python scripts/generate-dashboard.py
```

### Manual Status Update

When task status changes significantly:

```yaml
# In RFC front-matter
implementation:
  status: 'in-progress'     # Update status
  completion: 75            # Update percentage
  tasks: ['task-456']
  issues: [123]             # Add GitHub issues if any
```

## Task Templates

### Template 1: Implement RFC

```markdown
# Implement RFC-{NUMBER}: {TITLE}

## Context
- **RFC**: docs/rfcs/{NUMBER}-{slug}.md
- **Status**: {draft|approved|active}
- **Priority**: {high|medium|low}

## Objective
Implement the design specified in RFC-{NUMBER}

## Prerequisites
- [ ] RFC-{NUMBER} approved
- [ ] Dependencies implemented: {list dependent RFCs}
- [ ] Design reviewed by team

## Implementation Tasks
{Break down RFC into specific tasks}

## Testing
- [ ] Unit tests written
- [ ] Integration tests written
- [ ] Manual testing completed

## Documentation
- [ ] Code documented
- [ ] README updated
- [ ] RFC implementation status updated

## Success Criteria
- All acceptance criteria from RFC met
- Tests passing
- Code reviewed and merged

## Related
- RFC-{NUMBER}
- {Other related RFCs}
- {GitHub issues}
```

### Template 2: Create RFC

```markdown
# Create RFC: {TOPIC}

## Context
Need architectural design for {topic}

## Objective
Document design decisions and architecture for {topic}

## Tasks
- [ ] Research existing solutions
- [ ] Draft RFC in docs/_inbox/
- [ ] Add front-matter with proper doc_id
- [ ] Write Summary section
- [ ] Write Motivation section
- [ ] Write Proposed Solution section
- [ ] Add diagrams/examples
- [ ] Run validation: `python scripts/validate-docs.py`
- [ ] Check quality: `python scripts/generate-quality-report.py`
- [ ] Request review from team
- [ ] Address feedback
- [ ] Move to docs/rfcs/ when approved
- [ ] Update related RFCs

## Success Criteria
- RFC approved by team
- Quality score ≥ 60
- All validation passing
- Related docs updated

## Related
- {Related RFCs}
- {PRD if implementing one}
```

### Template 3: Update Documentation

```markdown
# Update Documentation for {FEATURE}

## Context
{Feature} has been implemented, need to update documentation

## Tasks
- [ ] Update relevant RFCs with implementation status
- [ ] Create/update guides if needed
- [ ] Update README if needed
- [ ] Add code examples
- [ ] Update cross-references
- [ ] Run validation
- [ ] Regenerate indexes

## Documentation Updates
- [ ] RFC-{NUMBER}: Update implementation.status to 'completed'
- [ ] GUIDE-{NUMBER}: Add new section for {feature}
- [ ] README: Add {feature} to features list

## Success Criteria
- All docs updated and validated
- Indexes regenerated
- Quality scores maintained

## Related
- {List RFCs/guides to update}
```

## Status Mapping

### Task Status → RFC Implementation Status

| Task Status | RFC Status | Completion % |
|-------------|------------|--------------|
| Not Started | not-started | 0 |
| In Progress | in-progress | 1-99 |
| Blocked | blocked | Current % |
| Completed | completed | 100 |
| Cancelled | deferred | Current % |

### Automatic Sync Rules

When `sync-tasks.py` runs:

1. **Find RFC references** in task descriptions
2. **Calculate completion** based on subtasks
3. **Update RFC status**:
   - All tasks completed → `completed`
   - Any task blocked → `blocked`
   - Any task in progress → `in-progress`
   - All tasks not started → `not-started`
4. **Update completion %** as average of all related tasks
5. **Add task IDs** to `implementation.tasks` array

## Dashboard Integration

The implementation dashboard (`docs/DASHBOARD.md`) shows:

- **Active RFCs**: Currently being implemented
- **Blocked RFCs**: Implementation blocked
- **Recently Completed**: Recently finished implementations
- **Not Started**: Approved but not yet started

Regenerate after status changes:

```bash
python scripts/generate-dashboard.py
```

## GitHub Actions Automation

### Automated Sync (Recommended)

Create `.github/workflows/sync-rfc-status.yml`:

```yaml
name: Sync RFC Status

on:
  schedule:
    - cron: '0 */6 * * *'  # Every 6 hours
  workflow_dispatch:  # Manual trigger

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'
      
      - name: Install dependencies
        run: pip install pyyaml
      
      - name: Sync RFC status
        run: python scripts/sync-tasks.py --apply
      
      - name: Regenerate dashboard
        run: python scripts/generate-dashboard.py
      
      - name: Commit changes
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add docs/
          git diff --quiet && git diff --staged --quiet || \
            git commit -m "docs: auto-sync RFC implementation status"
          git push
```

### Weekly Quality Report

Create `.github/workflows/quality-report.yml`:

```yaml
name: Weekly Quality Report

on:
  schedule:
    - cron: '0 9 * * 1'  # Monday 9 AM
  workflow_dispatch:

jobs:
  quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Python
        uses: actions/setup-python@v4
        with:
          python-version: '3.11'
      
      - name: Install dependencies
        run: pip install pyyaml
      
      - name: Generate quality report
        run: python scripts/generate-quality-report.py
      
      - name: Commit report
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add docs/index/quality-report.md
          git commit -m "docs: weekly quality report"
          git push
```

## Best Practices

### 1. Reference RFCs in Tasks

Always include RFC doc_id in task descriptions:

```markdown
Implement RFC-00042 (Game Services Architecture)
```

This enables automatic status syncing.

### 2. Update RFC Status Manually

Don't rely solely on automation. Update RFC status when:
- Starting implementation
- Hitting blockers
- Completing implementation
- Deferring to future milestone

### 3. Use Task Metadata

Add RFC references to task metadata:

```json
{
  "metadata": {
    "rfc": "RFC-00042",
    "type": "implementation"
  }
}
```

### 4. Keep Dashboard Current

Regenerate dashboard after significant changes:

```bash
python scripts/generate-dashboard.py
git add docs/DASHBOARD.md
git commit -m "docs: update implementation dashboard"
```

### 5. Link PRDs to RFCs

For product-driven development:

```yaml
# In PRD
implementation:
  rfcs: ['RFC-00042', 'RFC-00043']
  status: 'in-progress'

# In RFC
implements: 'PRD-00001'
```

## Troubleshooting

### RFC Status Not Updating

1. Check task description includes RFC doc_id
2. Verify tasks.json format is correct
3. Run sync manually: `python scripts/sync-tasks.py --apply`
4. Check sync log for errors

### Task Not Found

1. Ensure `.taskmaster/tasks.json` exists
2. Check task ID format
3. Verify task references RFC correctly

### Dashboard Out of Sync

1. Regenerate: `python scripts/generate-dashboard.py`
2. Check RFC front-matter is valid YAML
3. Run validation: `python scripts/validate-docs.py`

## See Also

- [Task-Master Documentation](https://github.com/eyaltoledano/claude-task-master)
- [RFC-00026: RFC Numbering Convention](../docs/_inbox/rfc-numbering-convention.md)
- [RFC-00012: Documentation Organization](../docs/rfcs/012-documentation-organization-management.md)
- [MCP Server Spec](../docs/MCP-SERVER-SPEC.md)
