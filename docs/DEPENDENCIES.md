---
canonical: true
created: '2025-11-20'
doc_id: GUIDE-00001
doc_type: guide
related: []
status: active
summary: 'Generated: 2025-11-20 06:56:13'
supersedes: []
tags:
  - documentation
  - guide
title: Documentation Dependencies
---

# Documentation Dependencies

_Generated: 2025-11-20 06:56:13_

## Implementation Status Flow

_Current distribution of RFCs by implementation status_

```mermaid
flowchart LR
    not-started["Not Started<br/>(18 RFCs)"]:::not-started

    not-started --> in-progress
    in-progress --> completed
    in-progress --> blocked
    blocked --> in-progress

    classDef not-started fill:#E0E0E0,stroke:#808080
    classDef in-progress fill:#87CEEB,stroke:#4682B4,stroke-width:2px
    classDef blocked fill:#FFB6C1,stroke:#DC143C,stroke-width:2px
    classDef completed fill:#90EE90,stroke:#006400,stroke-width:2px
```
