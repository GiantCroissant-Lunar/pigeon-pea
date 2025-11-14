---
doc_id: 'RFC-2025-00021'
title: 'Terminal Distribution — Rio Integration and Bundle Layout'
doc_type: 'rfc'
status: 'draft'
canonical: true
created: '2025-11-13'
tags: ['terminal', 'distribution', 'rio', 'bundle', 'deployment']
summary: 'Ship a consistent terminal experience using Rio Terminal with the custom game font, covering development-time setup and guidelines for player bundles without redistributing proprietary binaries'
supersedes: []
related: ['RFC-2025-00020', 'RFC-2025-00022']
---

# RFC-021: Terminal Distribution — Rio Integration and Bundle Layout

Status: Draft
Date: 2025-11-13
Author: Codex

## Summary

Define how to ship a consistent terminal experience using Rio Terminal with the custom game font. Covers development-time side-by-side setup and guidelines for player bundles without redistributing proprietary binaries.

## Goals

- Provide a predictable way to run the game with our font in Rio.
- Document side-by-side development setup (fonts next to Rio executable + config reference).
- Describe a redistributable bundle layout template that players can assemble.

## Non-Goals

- We will not include Rio binaries in the repo; we provide instructions and config templates.
- We will not cover macOS notarization or Windows code signing here.

## Bundle Layout (template)

```
bundle/
  rio/                      # User-provided Rio binary folder (not in repo)
    (rio.exe, etc.)
  fonts/
    LunarSarasaMono-Regular.ttf
    LunarSarasaMono-Bold.ttf
  config/
    rio.toml                # Rio config pointing to font family
  app/
    (game binaries/scripts)
  README.md
```

## Rio Configuration

Example `config/rio.toml`:

```
[fonts]
normal = { family = "Lunar Sarasa Mono" }
bold   = { family = "Lunar Sarasa Mono" }
italic = { family = "Lunar Sarasa Mono" }
```

Notes:

- If Rio supports direct file paths, prefer explicit file references; otherwise install TTF locally or cache via OS font facilities.
- Ensure no fallback fonts override PUA (disable/avoid fallbacks to keep deterministic output).

## Development-Time Setup

- Place TTFs next to Rio (e.g., `D:\\lunar-snake\\tools\\rio\\fonts\\...`).
- Point a local `rio.toml` to `Lunar Sarasa Mono` family.
- Launch Rio with `--config` if supported; otherwise place `rio.toml` in default config directory.

## Fallback Strategy

- If the font is unavailable, renderer should detect missing PUA glyphs and switch to Braille/box-drawing mode automatically (out of scope here; covered by renderer logic).

## Licensing

- Sarasa Gothic (and Source Han Sans components) licenses must be included in distributions that contain the TTFs.
- Do not redistribute Rio binaries unless license permits; link users to official downloads.

## Acceptance Criteria

- A sample `bundle/` tree exists (sans binaries) and a working `rio.toml` template is provided.
- Rio renders PUA glyphs when the font is installed or referenced by family.
