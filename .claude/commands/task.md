# /task - Create Documentation Task

Creates a structured documentation task in `docs/_inbox/` with proper front-matter template.

## Usage

```bash
/task <doc-type> <title>
```

## Supported Doc Types

- `rfc` - Request for Comments (design proposal)
- `guide` - How-to guide or tutorial
- `adr` - Architecture Decision Record
- `plan` - Planning document or roadmap
- `finding` - Research finding or analysis
- `spec` - Technical specification

## Examples

```bash
/task rfc "Plugin System Architecture"
/task guide "Setting Up Development Environment"
/task adr "Use PostgreSQL for Primary Database"
/task plan "Q1 2025 Feature Roadmap"
/task finding "Performance Analysis of Rendering Pipeline"
/task spec "Terminal Distribution Format"
```

## What It Does

1. Creates file in `docs/_inbox/<slug>.md`
2. Generates minimal front-matter template
3. Adds standard sections based on doc_type
4. File is ready for editing

## Front-Matter Template

All documents created in the inbox start with minimal front-matter:

```yaml
---
title: '<your-title>'
doc_type: '<rfc|guide|adr|plan|finding|spec>'
status: 'draft'
created: '<YYYY-MM-DD>'
tags: []
summary: ''
# TODO: Add doc_id before moving out of inbox (format: PREFIX-YYYY-NNNNN)
# TODO: Set canonical: true/false
# TODO: Complete tags and summary
---
```

## Document Templates by Type

### RFC (Request for Comments)

```markdown
---
title: 'RFC Title'
doc_type: 'rfc'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---

# RFC-XXX: RFC Title

## Status

**Status**: Draft
**Created**: YYYY-MM-DD
**Author**: [Your Name/Team]

## Summary

Brief overview of the proposal in 1-2 sentences.

## Motivation

Why is this needed? What problem does it solve?

### Problems This Solves

1. Problem 1
2. Problem 2

### Goals

1. Goal 1
2. Goal 2

## Design

### Architecture Overview

[Describe the high-level design]

### Components

[Detail the key components]

## Implementation Plan

### Phase 1: [Name]

[Steps and deliverables]

### Phase 2: [Name]

[Steps and deliverables]

## Alternatives Considered

### Alternative 1: [Name]

**Approach**: [Description]
**Pros**: [List]
**Cons**: [List]
**Decision**: [Accepted/Rejected - Reason]

## Success Criteria

1. Criterion 1
2. Criterion 2

## References

- [Reference 1](url)
- [Reference 2](url)
```

### Guide (How-to)

```markdown
---
title: 'Guide Title'
doc_type: 'guide'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---

# Guide: Guide Title

## Overview

Brief description of what this guide covers.

## Prerequisites

- Prerequisite 1
- Prerequisite 2

## Steps

### Step 1: [Name]

[Detailed instructions]

```bash
# Example commands
```
```

### Step 2: [Name]

[Detailed instructions]

## Troubleshooting

### Issue 1

**Problem**: [Description]
**Solution**: [Fix]

## See Also

- [Related Guide 1](path)
- [Related Guide 2](path)

```

### ADR (Architecture Decision Record)

```markdown
---
title: "ADR Title"
doc_type: "adr"
status: "draft"
created: "YYYY-MM-DD"
tags: []
summary: ""
---

# ADR-XXX: ADR Title

## Status

**Status**: Draft
**Date**: YYYY-MM-DD
**Deciders**: [List people involved]

## Context

What is the issue we're trying to solve? What is the context?

## Decision

What is the change that we're proposing/doing?

## Consequences

### Positive

- Consequence 1
- Consequence 2

### Negative

- Consequence 1
- Consequence 2

### Neutral

- Consequence 1

## Alternatives Considered

### Option 1

[Description]
[Pros and Cons]

### Option 2

[Description]
[Pros and Cons]

## References

- [Reference 1](url)
```

### Plan (Planning Document)

```markdown
---
title: 'Plan Title'
doc_type: 'plan'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---

# Plan: Plan Title

## Overview

High-level summary of the plan.

## Objectives

1. Objective 1
2. Objective 2

## Timeline

| Phase   | Duration | Deliverables |
| ------- | -------- | ------------ |
| Phase 1 | [Dates]  | [List]       |
| Phase 2 | [Dates]  | [List]       |

## Resources

- Resource 1
- Resource 2

## Risks

| Risk   | Impact | Mitigation |
| ------ | ------ | ---------- |
| Risk 1 | High   | [Strategy] |

## Success Metrics

1. Metric 1
2. Metric 2
```

### Finding (Research/Analysis)

```markdown
---
title: 'Finding Title'
doc_type: 'finding'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---

# Finding: Finding Title

## Summary

Brief summary of the finding.

## Context

What prompted this investigation?

## Methodology

How was the research/analysis conducted?

## Findings

### Finding 1

[Details]

### Finding 2

[Details]

## Implications

What do these findings mean for the project?

## Recommendations

1. Recommendation 1
2. Recommendation 2

## References

- [Reference 1](url)
```

### Spec (Technical Specification)

```markdown
---
title: 'Spec Title'
doc_type: 'spec'
status: 'draft'
created: 'YYYY-MM-DD'
tags: []
summary: ''
---

# Specification: Spec Title

## Overview

Brief description of what this specification covers.

## Requirements

### Functional Requirements

1. Requirement 1
2. Requirement 2

### Non-Functional Requirements

1. Requirement 1
2. Requirement 2

## Technical Details

### Data Structures

[Descriptions]

### APIs

[Endpoint definitions]

### Protocols

[Protocol specifications]

## Examples

### Example 1

[Code/Usage example]

## Validation

How to validate implementations against this spec.

## References

- [Reference 1](url)
```

## Workflow After Creating Task

1. **Edit the draft**: Add content to the generated file in `docs/_inbox/`
2. **Validate**: Run `python scripts/validate-docs.py` to check for duplicates
3. **Complete front-matter**: Add all required fields:
   - `doc_id`: Format `PREFIX-YYYY-NNNNN` (e.g., `RFC-2025-00012`)
   - `tags`: Relevant topic tags
   - `summary`: One-sentence description
   - `canonical`: Set to `true` if this is the authoritative version
4. **Move to final location**:
   - RFCs → `docs/rfcs/`
   - Guides → `docs/guides/`
   - ADRs → `docs/architecture/`
   - Plans → `docs/planning/`
   - Findings → `docs/planning/` or `docs/`
   - Specs → `docs/`
5. **Commit**: Pre-commit hook will validate automatically

## Doc ID Prefixes

| Prefix   | Document Type                | Example            |
| -------- | ---------------------------- | ------------------ |
| `RFC-`   | Request for Comments         | `RFC-2025-00012`   |
| `ADR-`   | Architecture Decision Record | `ADR-2025-00001`   |
| `GUIDE-` | How-to Guide                 | `GUIDE-2025-00001` |
| `PLAN-`  | Planning Document            | `PLAN-2025-00001`  |
| `FIND-`  | Finding/Analysis             | `FIND-2025-00001`  |
| `SPEC-`  | Specification                | `SPEC-2025-00001`  |

## Notes

- Files in `docs/_inbox/` are excluded from strict validation
- Validation warns if similar documents already exist
- Complete all front-matter fields before moving out of inbox
- See [RFC-012](../../docs/rfcs/012-documentation-organization-management.md) for full documentation system details
