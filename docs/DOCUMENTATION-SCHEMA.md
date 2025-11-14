---
doc_id: "REFERENCE-2025-00001"
title: "Documentation Front-Matter Schema"
doc_type: "reference"
status: "active"
canonical: true
created: "2025-11-13"
tags: ["documentation", "schema", "front-matter", "reference", "validation"]
summary: "Reference documentation for YAML front-matter schema required for all documentation files (except inbox drafts)"
supersedes: []
related: ["RFC-2025-00012"]
---

# Documentation Front-Matter Schema

All documentation (except `_inbox/` drafts) must include YAML front-matter.

## Required Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `doc_id` | string | Unique ID (format: `PREFIX-YYYY-NNNNN`) | `RFC-2025-00012` |
| `title` | string | Document title | `"Documentation Organization"` |
| `doc_type` | enum | Type of document | `rfc` (see valid values below) |
| `status` | enum | Lifecycle status | `active` (see valid values below) |
| `canonical` | boolean | Is this authoritative version? | `true` |
| `created` | date | Creation date (ISO format) | `2025-11-13` |
| `tags` | array | Topic tags | `["infrastructure", "docs"]` |
| `summary` | string | One-sentence summary | `"Structured doc management"` |

## Optional Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `updated` | date | Last update date | `2025-11-14` |
| `author` | string | Primary author | `"Development Team"` |
| `supersedes` | array | List of doc_ids this replaces | `["RFC-2025-00001"]` |
| `related` | array | List of related doc_ids | `["RFC-2025-00004"]` |

## Valid Values

### doc_type

- `spec`: Technical specification
- `rfc`: Request for Comments (design proposal)
- `adr`: Architecture Decision Record
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

| Prefix | Document Type | Example |
|--------|--------------|---------|
| `RFC-` | Request for Comments | `RFC-2025-00012` |
| `ADR-` | Architecture Decision Record | `ADR-2025-00001` |
| `GUIDE-` | How-to Guide | `GUIDE-2025-00001` |
| `PLAN-` | Planning Document | `PLAN-2025-00001` |
| `FIND-` | Finding/Analysis | `FIND-2025-00001` |
| `SPEC-` | Specification | `SPEC-2025-00001` |
| `GLOSSARY-` | Glossary/Terminology | `GLOSSARY-2025-00001` |
| `REFERENCE-` | Reference Documentation | `REFERENCE-2025-00001` |

## Example

```yaml
---
doc_id: "RFC-2025-00012"
title: "Documentation Organization Management"
doc_type: "rfc"
status: "draft"
canonical: true
created: "2025-11-13"
updated: "2025-11-14"
author: "Development Team"
tags: ["infrastructure", "documentation", "agent-tools"]
summary: "Structured documentation management system with validation, registry, and inbox workflow"
supersedes: []
related: ["RFC-2025-00004"]
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
- Doc ID format (`PREFIX-YYYY-NNNNN`)
- Date format (ISO 8601: `YYYY-MM-DD`)
- Near-duplicate detection

## Pre-commit Hook

The pre-commit hook automatically validates documentation on commit:

```bash
git commit -m "docs: add new RFC"
# → Runs: python scripts/validate-docs.py --pre-commit
# → Fails commit if validation errors found
```
