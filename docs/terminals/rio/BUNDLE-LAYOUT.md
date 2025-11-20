---
canonical: true
created: '2025-11-13'
doc_id: REFERENCE-00003
doc_type: reference
related:
  - RFC-00021
  - GUIDE-00001
  - GUIDE-00002
status: active
summary: Template folder structure for assembling a developer or player bundle with
  Rio Terminal (binaries not included)
supersedes: []
tags:
  - terminal
  - rio
  - bundle
  - distribution
  - template
title: Rio Bundle Layout (Template)
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
