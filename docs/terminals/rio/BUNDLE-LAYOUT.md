---
doc_id: 'REFERENCE-2025-00003'
title: 'Rio Bundle Layout (Template)'
doc_type: 'reference'
status: 'active'
canonical: true
created: '2025-11-13'
tags: ['terminal', 'rio', 'bundle', 'distribution', 'template']
summary: 'Template folder structure for assembling a developer or player bundle with Rio Terminal (binaries not included)'
supersedes: []
related: ['RFC-2025-00021', 'GUIDE-2025-00001', 'GUIDE-2025-00002']
---

# Rio Bundle Layout (Template)

This folder structure is a template for assembling a developer or player bundle. Do not check in Rio binaries.

```
bundle/
  rio/
    (rio.exe and related files)        # user-provided
  fonts/
    LunarSarasaMono-Regular.ttf
    LunarSarasaMono-Bold.ttf
  config/
    rio.toml
  app/
    (your game binaries / scripts)
  README.md
```

`config/rio.toml` should reference the font family:

```
[fonts]
normal = { family = "Lunar Sarasa Mono" }
bold   = { family = "Lunar Sarasa Mono" }
```

Add LICENSE files for fonts you distribute.
