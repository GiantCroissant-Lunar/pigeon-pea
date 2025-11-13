# Documentation Management Rules

These rules guide autonomous agents in creating, validating, and organizing documentation following RFC-012 guidelines.

## When to Create Documentation

Create documentation when:

1. **Proposing significant changes**: Use RFC for design proposals
2. **Making architecture decisions**: Use ADR to record decisions and rationale
3. **Planning features or projects**: Use Plan for roadmaps and timelines
4. **Writing tutorials**: Use Guide for step-by-step instructions
5. **Documenting research**: Use Finding for analysis and investigations
6. **Defining interfaces**: Use Spec for technical specifications

## Checking for Existing Documentation

**ALWAYS check before creating new documentation to prevent duplicates:**

### Method 1: Query the Registry (Preferred)

```bash
# Read the machine-readable index
cat docs/index/registry.json | jq '.docs[] | select(.doc_type == "rfc")'
cat docs/index/registry.json | jq '.docs[] | select(.tags[] | contains("plugin"))'
cat docs/index/registry.json | jq '.docs[] | select(.status == "active")'
```

### Method 2: Search Existing Documentation

```bash
# Search by title/content
find docs -name "*.md" -type f | xargs grep -l "plugin system"

# List all RFCs
ls docs/rfcs/

# List all guides
ls docs/guides/
```

### Method 3: Run Validation Script

```bash
# Script warns about near-duplicates
python scripts/validate-docs.py
```

### When Similar Docs Exist

- **If exact match exists**: Update existing doc instead of creating new
- **If related but different**: Link to related doc in front-matter `related` field
- **If superseding old doc**: Create new doc with `supersedes` field, mark old as `status: superseded`

## Inbox Workflow

The inbox workflow provides low-friction drafting without strict validation.

### Step 1: Create Draft in Inbox

Use the `/task` command to create a structured draft:

```bash
/task rfc "Plugin System Architecture"
/task guide "Setting Up Development Environment"
/task adr "Use PostgreSQL for Primary Database"
```

This creates a file in `docs/_inbox/` with minimal front-matter:

```yaml
---
title: 'Your Title'
doc_type: 'rfc'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---
```

### Step 2: Write Content

- Add content to the draft
- Use the template sections appropriate for the doc type
- Don't worry about perfect structure initially

### Step 3: Run Validation

Before finalizing, check for issues:

```bash
python scripts/validate-docs.py
```

The validation script will:

- ✅ Allow minimal front-matter in inbox
- ⚠️ Warn if similar documents exist
- ⚠️ Suggest related documents
- ❌ Report any critical issues

### Step 4: Complete Front-Matter

Before moving out of inbox, add all required fields:

```yaml
---
doc_id: 'RFC-2025-00012' # Format: PREFIX-YYYY-NNNNN
title: 'Plugin System Architecture'
doc_type: 'rfc'
status: 'draft'
canonical: true # Is this the authoritative version?
created: '2025-11-13'
updated: '2025-11-14' # Optional
tags: ['plugin', 'architecture', 'extensibility']
summary: 'Design proposal for extensible plugin system with hot-reloading support'
supersedes: [] # Optional: docs this replaces
related: ['RFC-2025-00006'] # Optional: related docs
---
```

#### Doc ID Format

Generate doc_id following this pattern: `PREFIX-YYYY-NNNNN`

| Prefix   | Document Type                | Example            |
| -------- | ---------------------------- | ------------------ |
| `RFC-`   | Request for Comments         | `RFC-2025-00012`   |
| `ADR-`   | Architecture Decision Record | `ADR-2025-00001`   |
| `GUIDE-` | How-to Guide                 | `GUIDE-2025-00001` |
| `PLAN-`  | Planning Document            | `PLAN-2025-00001`  |
| `FIND-`  | Finding/Analysis             | `FIND-2025-00001`  |
| `SPEC-`  | Specification                | `SPEC-2025-00001`  |

**Finding the next number**: Check existing docs in the target directory:

```bash
# For RFCs
ls docs/rfcs/ | grep -oP 'RFC-\d+-\d+' | sort -V | tail -1
# Returns: RFC-2025-00012 → next would be RFC-2025-00013

# For Guides
ls docs/guides/ | grep -oP 'GUIDE-\d+-\d+' | sort -V | tail -1
```

### Step 5: Move to Final Location

Move completed docs to their final location based on type:

| Doc Type | Final Location              | Command Example                                                 |
| -------- | --------------------------- | --------------------------------------------------------------- |
| RFC      | `docs/rfcs/`                | `mv docs/_inbox/draft.md docs/rfcs/013-plugin-system.md`        |
| Guide    | `docs/guides/`              | `mv docs/_inbox/draft.md docs/guides/setup-dev.md`              |
| ADR      | `docs/architecture/`        | `mv docs/_inbox/draft.md docs/architecture/adr-001-database.md` |
| Plan     | `docs/planning/`            | `mv docs/_inbox/draft.md docs/planning/q1-2025-roadmap.md`      |
| Finding  | `docs/planning/` or `docs/` | `mv docs/_inbox/draft.md docs/planning/performance-analysis.md` |
| Spec     | `docs/`                     | `mv docs/_inbox/draft.md docs/terminal-format-spec.md`          |

### Step 6: Commit with Validation

Commit the finalized document:

```bash
git add docs/rfcs/013-plugin-system.md
git commit -m "docs: add RFC-013 for plugin system architecture"
```

The pre-commit hook will automatically:

- ✅ Validate front-matter completeness
- ✅ Check canonical uniqueness
- ✅ Detect near-duplicates
- ❌ Fail commit if errors found (warnings allowed)

If validation fails, fix the issues and commit again.

## Validation Requirements

### Required Fields (Outside Inbox)

All documentation outside `docs/_inbox/` must have:

| Field       | Type    | Description          | Example                      |
| ----------- | ------- | -------------------- | ---------------------------- |
| `doc_id`    | string  | Unique ID            | `RFC-2025-00012`             |
| `title`     | string  | Document title       | `"Plugin System"`            |
| `doc_type`  | enum    | Type of document     | `rfc`                        |
| `status`    | enum    | Lifecycle status     | `active`                     |
| `canonical` | boolean | Is authoritative?    | `true`                       |
| `created`   | date    | Creation date (ISO)  | `2025-11-13`                 |
| `tags`      | array   | Topic tags           | `["plugin", "architecture"]` |
| `summary`   | string  | One-sentence summary | `"Design for plugin system"` |

### Valid Values

**doc_type**: `spec`, `rfc`, `adr`, `plan`, `finding`, `guide`, `glossary`, `reference`

**status**:

- `draft` - Work in progress, not yet reviewed
- `active` - Reviewed and currently relevant
- `superseded` - Replaced by a newer document
- `rejected` - Proposal was rejected/declined
- `archived` - No longer relevant but kept for history

### Validation Commands

```bash
# Full validation (generates registry)
python scripts/validate-docs.py

# Pre-commit mode (validation only)
python scripts/validate-docs.py --pre-commit

# Check specific file
python scripts/validate-docs.py docs/rfcs/013-plugin-system.md
```

## Canonical Document System

### Purpose

The canonical system ensures only ONE authoritative source of truth per concept.

### Rules

1. **Only one canonical per concept**: Set `canonical: true` for authoritative version
2. **Other variants must be non-canonical**: Set `canonical: false` for alternatives
3. **Validation enforces uniqueness**: Script checks normalized titles for conflicts
4. **Link related docs**: Use `related` field to connect canonical to variants

### Example: Multiple Docs on Same Topic

```yaml
# docs/rfcs/012-documentation-organization-management.md
---
doc_id: 'RFC-2025-00012'
title: 'Documentation Organization Management'
canonical: true # ← This is the authoritative version
status: 'active'
related: ['PLAN-2025-00005']
---
# docs/planning/documentation-system-plan.md
---
doc_id: 'PLAN-2025-00005'
title: 'Documentation System Implementation Plan'
canonical: false # ← This is a supporting document
status: 'active'
related: ['RFC-2025-00012']
---
```

### When to Set canonical: true

- Primary design document (RFC)
- Official architecture decision (ADR)
- Authoritative specification (Spec)
- Main guide for a topic (Guide)

### When to Set canonical: false

- Supporting plans or timelines
- Alternative approaches (rejected RFCs)
- Supplementary guides
- Analysis documents (Findings)

## Document Lifecycle Management

### Lifecycle States

```text
draft → active → superseded/archived
  ↓       ↓          ↓
inbox  final loc  archive/
```

### State Transitions

#### Draft → Active

When document is reviewed and approved:

```yaml
# Update front-matter
status: 'active'
updated: '2025-11-15'
```

#### Active → Superseded

When creating a replacement document:

**New document**:

```yaml
---
doc_id: 'RFC-2025-00020'
title: 'Enhanced Plugin System v2'
status: 'active'
canonical: true
supersedes: ['RFC-2025-00012']
---
```

**Old document**:

```yaml
---
doc_id: "RFC-2025-00012"
title: "Plugin System Architecture (Superseded)"
status: "superseded"
canonical: false
updated: "2025-11-20"
---

**Note**: This RFC has been superseded by [RFC-020: Enhanced Plugin System v2](../rfcs/020-enhanced-plugin-system-v2.md).
```

Then move to archive:

```bash
git mv docs/rfcs/012-plugin-system.md docs/archive/012-plugin-system.md
```

#### Active → Rejected

When a proposal is not accepted:

```yaml
---
status: "rejected"
updated: "2025-11-15"
---

## Rejection Notice

This RFC was rejected on 2025-11-15 for the following reasons:

1. Reason 1
2. Reason 2

See [ADR-XXX](link) for the alternative approach chosen.
```

#### Active → Archived

When document is no longer relevant:

```yaml
---
status: 'archived'
updated: '2025-11-15'
---
## Archive Notice

This document has been archived as of 2025-11-15. The information is kept for historical reference but is no longer maintained.
```

Move to archive:

```bash
git mv docs/guides/old-setup.md docs/archive/old-setup.md
```

## Updating Documentation

### Minor Updates

For small changes that don't affect the document's core content:

1. Edit in place
2. Update `updated` field in front-matter
3. Commit changes

```bash
# Edit file
vim docs/rfcs/012-documentation-organization-management.md

# Update front-matter
# updated: "2025-11-15"

git add docs/rfcs/012-documentation-organization-management.md
git commit -m "docs: update RFC-012 with additional examples"
```

### Major Revisions

For significant changes that warrant a new version:

1. Create new document with new `doc_id`
2. Link to old document with `supersedes` field
3. Mark old document as `status: superseded`
4. Move old document to `docs/archive/`

See "Active → Superseded" lifecycle transition above.

## Pre-commit Hook Behavior

The pre-commit hook runs `python scripts/validate-docs.py --pre-commit` automatically.

### What It Checks

- ✅ Front-matter validation (all required fields present)
- ✅ Field value validation (valid doc_type, status, etc.)
- ✅ Doc ID format (`PREFIX-YYYY-NNNNN`)
- ✅ Date format (ISO 8601: `YYYY-MM-DD`)
- ✅ Canonical uniqueness (only one canonical per concept)
- ⚠️ Near-duplicate detection (warnings only)

### Files Excluded

- `docs/_inbox/*` - Inbox files have relaxed validation
- `docs/index/*` - Generated files are not validated

### If Validation Fails

1. **Read error message**: Tells you what's wrong
2. **Fix the issues**: Edit front-matter or content
3. **Re-stage files**: `git add <fixed-files>`
4. **Commit again**: Pre-commit runs validation again

### Common Validation Errors

#### Missing Required Fields

```text
[ERROR] docs/rfcs/test.md: Missing required fields: doc_id, tags, summary
```

**Fix**: Add missing fields to front-matter

#### Invalid doc_type

```text
[ERROR] docs/guides/test.md: Invalid doc_type 'tutorial'. Must be one of: spec, rfc, adr, plan, finding, guide, glossary, reference
```

**Fix**: Change `doc_type: tutorial` to `doc_type: guide`

#### Multiple Canonical Documents

```text
[ERROR] Multiple canonical docs for concept 'plugin system':
  docs/rfcs/012-plugin-system.md
  docs/rfcs/020-plugin-system-v2.md
```

**Fix**: Set `canonical: false` on one of them (usually the older one)

#### Invalid Doc ID Format

```text
[ERROR] docs/rfcs/test.md: Invalid doc_id format 'RFC-012'. Expected format: PREFIX-YYYY-NNNNN
```

**Fix**: Change to proper format: `RFC-2025-00012`

## Best Practices

### DO

✅ **Check for existing docs first** - Prevent duplicates
✅ **Start in inbox** - Low-friction drafting
✅ **Use `/task` command** - Consistent templates
✅ **Run validation before finalizing** - Catch issues early
✅ **Complete all front-matter** - Ensure discoverability
✅ **Link related documents** - Use `related` field
✅ **Set canonical correctly** - Mark authoritative versions
✅ **Update `updated` field** - Track modifications
✅ **Use proper doc IDs** - Follow `PREFIX-YYYY-NNNNN` format
✅ **Write clear summaries** - One-sentence descriptions

### DON'T

❌ **Create duplicates** - Check registry first
❌ **Skip validation** - Always run before moving out of inbox
❌ **Leave incomplete front-matter** - Complete all required fields
❌ **Multiple canonicals** - Only one authoritative version per concept
❌ **Forget to link related docs** - Use `related` field
❌ **Commit to final location directly** - Start in inbox
❌ **Skip pre-commit hooks** - Let validation catch errors
❌ **Use wrong doc_type** - Choose appropriate type
❌ **Forget to supersede old docs** - Update old doc status
❌ **Leave TODO comments in final docs** - Clean up before moving

## Example Workflows

### Workflow 1: Creating a New RFC

```bash
# 1. Check if similar RFC exists
cat docs/index/registry.json | jq '.docs[] | select(.doc_type == "rfc" and (.title | contains("plugin")))'

# 2. Create draft
/task rfc "Plugin Hot-Reloading System"

# 3. Edit draft (writes content)
vim docs/_inbox/plugin-hot-reloading-system.md

# 4. Validate
python scripts/validate-docs.py

# 5. Complete front-matter
vim docs/_inbox/plugin-hot-reloading-system.md
# Add: doc_id, tags, summary, canonical, etc.

# 6. Move to final location
mv docs/_inbox/plugin-hot-reloading-system.md docs/rfcs/013-plugin-hot-reloading.md

# 7. Commit
git add docs/rfcs/013-plugin-hot-reloading.md
git commit -m "docs: add RFC-013 for plugin hot-reloading system"
```

### Workflow 2: Updating an Existing Guide

```bash
# 1. Find the guide
ls docs/guides/ | grep setup

# 2. Edit in place
vim docs/guides/setup-development-environment.md

# 3. Update front-matter
# updated: "2025-11-15"

# 4. Validate
python scripts/validate-docs.py

# 5. Commit
git add docs/guides/setup-development-environment.md
git commit -m "docs: update development setup guide with new prerequisites"
```

### Workflow 3: Superseding an Old RFC

```bash
# 1. Create new RFC draft
/task rfc "Enhanced Plugin System v2"

# 2. Write content
vim docs/_inbox/enhanced-plugin-system-v2.md

# 3. Complete front-matter with supersedes field
# supersedes: ["RFC-2025-00012"]

# 4. Move to final location
mv docs/_inbox/enhanced-plugin-system-v2.md docs/rfcs/020-enhanced-plugin-system-v2.md

# 5. Update old RFC
vim docs/rfcs/012-plugin-system.md
# status: "superseded"
# canonical: false

# 6. Move old RFC to archive
git mv docs/rfcs/012-plugin-system.md docs/archive/012-plugin-system.md

# 7. Commit both
git add docs/rfcs/020-enhanced-plugin-system-v2.md docs/archive/012-plugin-system.md
git commit -m "docs: add RFC-020, supersede RFC-012"
```

## Quick Reference

### Commands

```bash
# Create draft
/task <doc-type> <title>

# Validate documentation
python scripts/validate-docs.py

# Search registry
cat docs/index/registry.json | jq '.docs[] | select(.tags[] | contains("search-term"))'

# Find next doc ID number
ls docs/rfcs/ | grep -oP 'RFC-\d+-\d+' | sort -V | tail -1
```

### File Locations

| Doc Type | Draft Location | Final Location              |
| -------- | -------------- | --------------------------- |
| RFC      | `docs/_inbox/` | `docs/rfcs/`                |
| Guide    | `docs/_inbox/` | `docs/guides/`              |
| ADR      | `docs/_inbox/` | `docs/architecture/`        |
| Plan     | `docs/_inbox/` | `docs/planning/`            |
| Finding  | `docs/_inbox/` | `docs/planning/` or `docs/` |
| Spec     | `docs/_inbox/` | `docs/`                     |

### Status Values

- `draft` → Work in progress
- `active` → Current and reviewed
- `superseded` → Replaced by newer doc
- `rejected` → Proposal declined
- `archived` → No longer relevant

## See Also

- [RFC-012: Documentation Organization Management](../../docs/rfcs/012-documentation-organization-management.md) - Full system design
- [DOCUMENTATION-SCHEMA.md](../../docs/DOCUMENTATION-SCHEMA.md) - Front-matter specification
- [docs/\_inbox/README.md](../../docs/_inbox/README.md) - Inbox workflow guide
- [/task command](../../.claude/commands/task.md) - Task creation command reference
