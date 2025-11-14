---
doc_id: 'GUIDE-2025-00002'
title: 'Rio Terminal Configuration — Using the Custom Font'
doc_type: 'guide'
status: 'active'
canonical: true
created: '2025-11-13'
tags: ['terminal', 'rio', 'configuration', 'fonts', 'setup']
summary: "Guide for configuring Rio Terminal to use the project's custom font with PUA glyphs"
supersedes: []
related: ['RFC-2025-00021', 'GUIDE-2025-00001', 'GUIDE-2025-00003']
---

# Rio Terminal Configuration — Using the Custom Font

This guide shows how to make Rio use the project’s custom font with PUA glyphs.

## Option A — Side-by-side config (development)

Place the TTFs near your Rio binary and use a `rio.toml` like:

```
[fonts]
normal = { family = "Lunar Sarasa Mono" }
bold   = { family = "Lunar Sarasa Mono" }
italic = { family = "Lunar Sarasa Mono" }
```

Notes:

- If Rio supports supplying a config path flag, point it to this file.
- If Rio requires installation into the OS font directory, install the TTFs and keep the same family name.
- Disable or minimize fallback fonts to avoid PUA conflicts.

## Option B — System-wide install

Install the TTFs in your OS and select the family in Rio settings, using the same names.

## Troubleshooting

- If PUA glyphs show as tofu boxes, the custom font is not active; verify family name and that no fallback is overriding.
- On Windows, confirm no WPF/DirectWrite font cache issues; try clearing font cache if necessary during development.
