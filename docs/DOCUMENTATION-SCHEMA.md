---
canonical: true
created: '2025-11-13'
doc_id: REFERENCE-00001
doc_type: reference
related:
  - RFC-00012
status: active
summary: Reference documentation for YAML front-matter schema required for all documentation
  files (except inbox drafts)
supersedes: []
tags:
  - documentation
  - schema
  - front-matter
  - reference
  - validation
title: Documentation Front-Matter Schema
---

# Documentation Front-Matter Schema

All documentation (except `_inbox/` drafts) must include YAML front-matter.

## Required Fields

| Field       | Type    | Description                        | Example                           |
| ----------- | ------- | ---------------------------------- | --------------------------------- |
| `doc_id`    | string  | Unique ID (format: `PREFIX-NNNNN`) | `RFC-00012`                       |
| `title`     | string  | Document title                     | `"Documentation Organization"`    |
| `doc_type`  | enum    | Type of document                   | `rfc` (see valid values below)    |
| `status`    | enum    | Lifecycle status                   | `active` (see valid values below) |
| `canonical` | boolean | Is this authoritative version?     | `true`                            |
| `created`   | date    | Creation date (ISO format)         | `2025-11-13`                      |
| `tags`      | array   | Topic tags                         | `["infrastructure", "docs"]`      |
| `summary`   | string  | One-sentence summary               | `"Structured doc management"`     |

## Optional Fields

| Field            | Type   | Description                         | Example                           |
| ---------------- | ------ | ----------------------------------- | --------------------------------- |
| `updated`        | date   | Last update date                    | `2025-11-14`                      |
| `author`         | string | Primary author                      | `"Development Team"`              |
| `supersedes`     | array  | List of doc_ids this replaces       | `["RFC-00001"]`                   |
| `related`        | array  | List of related doc_ids             | `["RFC-00004"]`                   |
| `implements`     | string | PRD this RFC implements (RFCs only) | `"PRD-00001"`                     |
| `implementation` | object | Implementation tracking (RFCs/PRDs) | See Implementation Tracking below |
| `dependencies`   | object | Dependencies (RFCs only)            | See Dependencies below            |

## Implementation Tracking (RFC Documents)

For RFC documents, the optional `implementation` field tracks implementation status:

```yaml
implementation:
  status: 'not-started' # not-started | in-progress | completed | blocked | deferred
  completion: 0 # 0-100 percentage
  tasks: [] # List of task IDs from task-master
  issues: [] # List of GitHub issue numbers
  started: '2025-11-15' # Optional: when implementation started
  completed: null # Optional: when implementation completed
```

### Implementation Status Values

- `not-started`: RFC approved but implementation not yet begun
- `in-progress`: Active implementation work
- `completed`: Implementation finished and verified
- `blocked`: Implementation blocked by dependencies or issues
- `deferred`: Implementation postponed to future milestone

## Dependencies (RFC Documents)

For RFC documents, the optional `dependencies` field tracks relationships:

```yaml
dependencies:
  rfcs: [] # Other RFCs this depends on
  external: [] # External dependencies (libraries, tools, etc.)
  blocks: [] # RFCs that are blocked by this one
```

## Valid Values

### doc_type

- `spec`: Technical specification
- `rfc`: Request for Comments (design proposal)
- `adr`: Architecture Decision Record
- `prd`: Product Requirements Document
- `plan`: Planning document or roadmap
- `finding`: Research finding or analysis
- `guide`: How-to guide or tutorial
- `glossary`: Terminology definitions
- `reference`: Reference documentation

### status

- `draft`: Work in progress, not yet reviewed
- `active`: Reviewed and currently relevant
- `superseded`: Replaced by a newer document
- `rejected`: Proposal was rejected/declined
- `archived`: No longer relevant but kept for history

### Doc ID Prefixes

| Prefix       | Document Type                | Example           | Notes                      |
| ------------ | ---------------------------- | ----------------- | -------------------------- |
| `RFC-`       | Request for Comments         | `RFC-00012`       | Sequential numbering       |
| `ADR-`       | Architecture Decision Record | `ADR-00001`       | Sequential numbering       |
| `PRD-`       | Product Requirements Doc     | `PRD-00001`       | Sequential numbering       |
| `GUIDE-`     | How-to Guide                 | `GUIDE-00001`     | Sequential numbering       |
| `PLAN-`      | Planning Document            | `PLAN-00001`      | Sequential numbering       |
| `FIND-`      | Finding/Analysis             | `FIND-00001`      | Sequential numbering       |
| `SPEC-`      | Specification                | `SPEC-00001`      | Sequential numbering       |
| `GLOSSARY-`  | Glossary/Terminology         | `GLOSSARY-00001`  | Sequential numbering       |
| `REFERENCE-` | Reference Documentation      | `REFERENCE-00001` | Legacy format with year OK |

**Note**: Year removed from doc_id format per RFC-00026. Year is tracked in `created` field instead.

## Example

```yaml
---
doc_id: 'RFC-00012'
title: 'Documentation Organization Management'
doc_type: 'rfc'
status: 'draft'
canonical: true
created: '2025-11-13'
updated: '2025-11-14'
author: 'Development Team'
tags: ['infrastructure', 'documentation', 'agent-tools']
summary: 'Structured documentation management system with validation, registry, and inbox workflow'
supersedes: []
related: ['RFC-00004']
---
```

## Validation

Run validation script:

```bash
python scripts/validate-docs.py
```

Checks:

- All required fields present
- Valid doc_type and status values
- Canonical uniqueness (only one canonical doc per concept)
- Doc ID format (`PREFIX-NNNNN` per RFC-00026)
- Date format (ISO 8601: `YYYY-MM-DD`)
- Near-duplicate detection

## Pre-commit Hook

The pre-commit hook automatically validates documentation on commit:

```bash
git commit -m "docs: add new RFC"
# → Runs: python scripts/validate-docs.py --pre-commit
# → Fails commit if validation errors found
```
