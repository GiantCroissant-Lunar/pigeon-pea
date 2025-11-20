---
canonical: true
created: '2025-11-20'
doc_id: RFC-2025-00007
doc_type: rfc
related: []
status: active
summary: 'Product Requirement Documents (PRDs) and RFCs serve different purposes:'
supersedes: []
tags:
- architecture
- documentation
- ecs
- plugins
- rfc
title: PRD-RFC Relationship Schema
---

# PRD-RFC Relationship Schema

## Overview
Product Requirement Documents (PRDs) and RFCs serve different purposes:

- **PRD**: What to build and why (product perspective)
- **RFC**: How to build it (technical perspective)

**Recommended Approach**: PRDs reference RFCs (one-to-many relationship)


## PRD Front-Matter Extension
```yaml
---
doc_id: 'PRD-00001'
title: 'Fantasy Calendar System'
doc_type: 'prd'
status: 'active'
canonical: true
created: '2025-11-20'
tags: ['product', 'calendar', 'time-system']
summary: 'Product requirements for fantasy calendar system'
implementation:
  rfcs: ['RFC-2025-00015']  # RFCs that implement this PRD
  status: 'in-progress'
---
```

## RFC Front-Matter Extension
```yaml
---
doc_id: 'RFC-00015'
title: 'Fantasy Calendar to Real-World Time Transformation'
doc_type: 'rfc'
# ... other fields ...
implements: 'PRD-2025-00001'  # PRD this RFC implements
dependencies:
  rfcs: ['RFC-2025-00013']  # Other RFCs this depends on
  prds: []  # Additional PRDs (if implementing multiple)
---
```

## Relationship Types

### 1. PRD → RFC (Implementation)
- **Direction**: PRD references RFCs
- **Cardinality**: One PRD → Many RFCs
- **Field**: `implementation.rfcs` in PRD
- **Use Case**: Track which RFCs implement a product requirement

### 2. RFC → PRD (Implements)
- **Direction**: RFC references PRD
- **Cardinality**: One RFC → One PRD (typically)
- **Field**: `implements` in RFC
- **Use Case**: Show product context for technical design

### 3. RFC → RFC (Dependencies)
- **Direction**: RFC references other RFCs
- **Cardinality**: Many-to-many
- **Field**: `dependencies.rfcs` in RFC
- **Use Case**: Technical dependencies between designs


## Visualization
```mermaid
graph TD
    PRD1[PRD: Fantasy Calendar] --> RFC15[RFC-015: Time Transform]
    PRD1 --> RFC16[RFC-016: Calendar UI]
    RFC15 --> RFC13[RFC-013: Plugin Architecture]
    RFC16 --> RFC15
    
    style PRD1 fill:#E1BEE7,stroke:#7B1FA2
    style RFC15 fill:#BBDEFB,stroke:#1976D2
    style RFC16 fill:#BBDEFB,stroke:#1976D2
    style RFC13 fill:#BBDEFB,stroke:#1976D2
```

## Benefits
1. **Traceability**: Track from product requirement to technical implementation
2. **Impact Analysis**: See which RFCs are affected by PRD changes
3. **Completeness**: Verify all PRD requirements have RFC coverage
4. **Context**: Understand product rationale behind technical decisions
