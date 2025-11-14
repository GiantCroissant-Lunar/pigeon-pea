---
doc_id: 'GUIDE-2025-00004'
title: 'Documentation Inbox'
doc_type: 'guide'
status: 'active'
canonical: true
created: '2025-11-13'
tags: ['documentation', 'inbox', 'workflow', 'drafts']
summary: 'Guide for using the documentation inbox for draft documents that are not yet ready for publication'
supersedes: []
related: ['RFC-2025-00012', 'REFERENCE-2025-00001']
---

# Documentation Inbox

This directory is for **draft documentation** that is not yet ready for publication.

## Purpose

- **Low-friction drafting**: Start writing without worrying about perfect structure
- **Work in progress**: Iterate on docs before moving to final location
- **Duplicate checking**: Validation warns if similar docs exist

## Rules

1. **Minimal front-matter required**:

   ```yaml
   ---
   title: 'Your Title'
   doc_type: 'rfc|guide|adr|plan|finding|spec'
   status: 'draft'
   created: 'YYYY-MM-DD'
   ---
   ```

2. **No strict validation**: Inbox docs excluded from required field checks

3. **Temporary home**: Move to final location when complete

4. **Check for duplicates**: Run `python scripts/validate-docs.py` before finalizing

## Workflow

1. Create draft: `/task <doc-type> <title>`
2. Write content
3. Validate: `python scripts/validate-docs.py`
4. Complete front-matter (doc_id, tags, summary, etc.)
5. Move to final location: `docs/rfcs/`, `docs/guides/`, etc.
6. Commit (pre-commit validates automatically)

## Final Locations

| Doc Type  | Location                                        |
| --------- | ----------------------------------------------- |
| RFC       | `docs/rfcs/`                                    |
| Guide     | `docs/guides/`                                  |
| ADR       | `docs/architecture/`                            |
| Plan      | `docs/planning/`                                |
| Finding   | `docs/planning/` (internal) or `docs/` (public) |
| Spec      | `docs/`                                         |
| Glossary  | `docs/`                                         |
| Reference | `docs/`                                         |

## See Also

- [DOCUMENTATION-SCHEMA.md](../DOCUMENTATION-SCHEMA.md) - Front-matter specification
- [RFC-012](../rfcs/012-documentation-organization-management.md) - This system's design
