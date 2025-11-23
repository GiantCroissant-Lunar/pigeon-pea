---
created: '2025-11-21'
doc_id: ''
doc_type: plan
status: draft
summary: Follow-up housekeeping tasks after reorganizing automation scripts and documentation
  tooling.
tags:
  - housekeeping
  - automation
  - scripts
title: Script & Infrastructure Housekeeping – Follow-up Plan
---

# Script & Infrastructure Housekeeping – Follow-up Plan

## Background

We have reorganized the automation scripts to better align with RFC-012 (Documentation Organization Management) and the broader agent infrastructure:

- Agent infrastructure helpers live under `.agent/scripts/`
- Documentation management helpers live under `scripts/docs_mgmt/`
- Asset-generation helpers live under `scripts/assets/`
- Developer tooling helpers live under `scripts/devtools/`
- Long-lived JSON reports live under `scripts/reports/`
- Font-related tooling lives under `fonts/`

This document tracks the next round of housekeeping work building on that reorganization.

## Proposed follow-up tasks

### 1. Align RFC-012 and docs with RFC-00026 doc_id format

- Review RFC-012 and other documentation that still references the legacy `PREFIX-YYYY-NNNNN` doc_id format.
- Decide whether to fully adopt the `PREFIX-NNNNN` format from RFC-00026 across the corpus.
- If we adopt it, plan a migration using:
  - `scripts/docs_mgmt/migrate-rfc-numbering.py`
  - `scripts/docs_mgmt/sync-rfc-index.py`
- Update examples and tables in RFCs and guides to match the final format.

### 2. Harden docs_mgmt scripts across platforms

- Audit `scripts/docs_mgmt/*` for any remaining path or newline assumptions that behave differently on Windows vs POSIX shells.
- Add small smoke tests or Taskfile targets that exercise the most important scripts end-to-end:
  - `validate-docs.py`
  - `generate-index.py`
  - `generate-quality-report.py`
  - `visualize-deps.py`
- Consider adding a GitHub Actions job that runs the doc-management scripts on CI.

### 3. Run the full documentation lifecycle once

- Use the new layout to run a full documentation maintenance pass:
  - Generate or refresh front-matter with `generate-frontmatter.py`.
  - Migrate misplaced docs with `migrate-docs.py`.
  - Clean up inbox content with `cleanup-inbox.py`.
  - Refresh indexes and dashboards with:
    - `generate-index.py`
    - `generate-quality-report.py`
    - `generate-dashboard.py`
    - `visualize-deps.py`
- Capture any gaps or rough edges in this doc for future RFCs.

### 4. Integrate .NET documentation helpers

- Decide how and when to run:
  - `scripts/docs_mgmt/add_dotnet_docs.py`
  - `scripts/docs_mgmt/update_dotnet_registry.py`
- Consider adding Taskfile targets and/or CI steps for refreshing .NET-focused documentation.

### 5. Agent & workflow integration

- Ensure `.agent/rules/*` and `.agent/workflows/*` reference the new script layout where appropriate.
- Consider adding a small section to RFC-012 (or a follow-up RFC) that treats `scripts/README.md` and the new layout as the canonical interface for automation scripts.

## Open questions

- Do we want a dedicated "Scripts & Automation" RFC to complement RFC-012, or is an appendix in RFC-012 sufficient?
- Which of the docs_mgmt scripts should be considered "safe" for agents to call directly vs. human-initiated only?
- How often should we schedule a full documentation maintenance run (e.g., weekly CI job vs. ad-hoc)?
