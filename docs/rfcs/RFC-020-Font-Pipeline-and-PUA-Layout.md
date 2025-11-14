---
doc_id: "RFC-2025-00020"
title: "Custom CJK Monospace Game Font — Pipeline and PUA Layout"
doc_type: "rfc"
status: "draft"
canonical: true
created: "2025-11-13"
tags: ["fonts", "cjk", "pua", "terminal", "pipeline"]
summary: "Font pipeline to produce a unified, terminal-friendly CJK monospace font that embeds custom map glyphs (angled lines, junctions, terrain) in the Unicode Private Use Area (PUA)"
supersedes: []
related: ["RFC-2025-00021", "RFC-2025-00022"]
---

# RFC-020: Custom CJK Monospace Game Font — Pipeline and PUA Layout

Status: Draft
Date: 2025-11-13
Author: Codex

## Summary

Define a font pipeline to produce a unified, terminal-friendly CJK monospace font that embeds our custom map glyphs (angled lines, junctions, terrain) in the Unicode Private Use Area (PUA). The base font is Sarasa Gothic Fixed CL (Regular/Bold). We will ship a pair of faces: Regular and Bold.

## Motivation

- Terminal emulators typically load a single font; we need one file that contains ASCII, CJK, Braille/box-drawing, Nerd symbols, and our PUA glyphs.
- Angle-based map rendering in text mode requires clean, deterministic glyphs that connect visually cell-to-cell.
- Development and distribution should be reproducible via scripts and CI.

## Goals

- Choose base font and variants that render well in consoles (Rio, WezTerm, etc.).
- Reserve a stable PUA scheme for 36 angle glyphs (+ variants) and future tiles.
- Specify glyph metrics so outlines connect across cells without gaps.
- Provide import scripts to patch fonts with SVG sources.

## Non-Goals

- We will not distort CJK shapes to “square-ify” the font. Readability stays intact.
- We will not ship terminal binaries; bundling directions will be documented separately (RFC-021).

## Base Font Selection

- Family: Sarasa Gothic — Fixed CL (no ligatures; pan-CJK “CL” orthography for mixed C/J/K).
- Weights: Regular, Bold.
- Rationale: proven CJK monospace for coding/terminals; balanced metrics; widely adopted.

## Angle Resolution

- Initial bucket: 10 degrees per glyph -> 36 glyphs (0..350).
- Future: optional 5-degree set if terminals demonstrate enough pixel density advantage.

## PUA Allocation

BMP PUA only for now (6,400 slots in U+E000-U+F8FF).

```text
U+E000-U+E0FF  (256)  Angle glyphs & line variants
  E000-E023 (36)  Angles 0,10, ..., 350  (basic)
  E030-E053 (36)  Angles 0..350          (thick)
  E060-E083 (36)  Angles 0..350          (dashed)

U+E100-U+E1FF  (256)  Junctions / connectors (future)
  E100-E13F  L-corners
  E140-E17F  T-junctions
  E180-E1BF  Crossroads
  E1C0-E1FF  Specials

U+E200-U+E2FF  (256)  Terrain tiles (future)
U+E300-U+E3FF  (256)  POIs/icons (future)
```

## Glyph Metrics and Style

- Monospace width: match ASCII width of the base font.
- Bearings: set left/right side bearings to zero (tile fills width).
- Vertical: align to baseline; stroke extends to top/bottom to ensure continuity.
- Style: single centered stroke that visually touches cell borders to connect with neighbors.
- Bold: relies on the Bold face; additionally provide explicit “thick” variants in PUA for style control.

## File/Folder Layout (sources and outputs)

```text
fonts/
  src/
    svg/
      lines/
        angle_000.svg
        angle_010.svg
        ...
        angle_350.svg
      (future: junctions/, terrain/, pois/)
  scripts/
    patch_sarasa.py       # FontForge script (imports SVG into PUA)
  dist/
    LunarSarasaMono-Regular.ttf
    LunarSarasaMono-Bold.ttf
```

## Build Pipeline (local dev)

Prereqs: FontForge (with Python), ttfautohint, Node.js (for Sarasa build if rebuilding base), Nerd Fonts patch pipeline (if starting from plain Sarasa).

1. Obtain base font:
   - Option A: Start from Nerd-patched Sarasa Gothic; then import our PUA glyphs.
   - Option B: Build Sarasa from source -> patch Nerd symbols -> import PUA glyphs.
2. Import SVGs via FontForge script for both Regular and Bold faces.
3. Generate TTFs into `fonts/dist/` and verify metrics/connectivity in terminal.

## Angle -> Glyph Mapping (example)

```text
bucket = round(angle / 10) mod 36
codepoint = 0xE000 + bucket
```

## Acceptance Criteria

- Regular/Bold TTFs exist with PUA glyphs E000-E023 populated and visibly connecting across cells in Rio/WezTerm at common sizes.
- PUA layout documented and stable.
- Scripted generation is repeatable.

## Risks

- Terminal rendering differences (hinting, DPI) may affect stroke thickness/continuity.
- Font fallback must be disabled/irrelevant in target terminals; otherwise tofu boxes.

## Future Work

- Expand junctions/terrain/POIs.
- Optional 5-degree set; smoothing heuristics in renderer.
- CI/CD artifacts per platform and versioned releases (RFC-022).
