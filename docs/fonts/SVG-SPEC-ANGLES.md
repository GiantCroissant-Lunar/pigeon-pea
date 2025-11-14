---
doc_id: "SPEC-2025-00001"
title: "SVG Specification — 36 Angle Glyphs"
doc_type: "spec"
status: "active"
canonical: true
created: "2025-11-13"
tags: ["fonts", "svg", "specification", "pua", "glyphs"]
summary: "SVG specification for 36 angle glyphs to be imported into font PUA (Private Use Area) for terminal rendering"
supersedes: []
related: ["RFC-2025-00020", "SPEC-2025-00002"]
---

# SVG Specification — 36 Angle Glyphs

Purpose: provide consistent SVG sources for FontForge import into PUA.

## Canvas and Units

- Use the same em size as the base font (typically 1000 or 2048 upm). If unknown, draw in arbitrary units and let FontForge scale on import; ensure aspect ratio preserved.
- Keep all angles centered horizontally with zero side bearings (we will set L/R bearings to 0 in the font editor/script).

## Stroke Design

- Regular face: single stroke of nominal thickness T that visually connects at cell edges.
- Bold face: stroke thickness ~1.25–1.4× T.
- Line caps: square (to ensure seamless joining at cell boundaries).
- Stroke should reach the glyph box edges to connect with adjacent cells for 0°, 90°, 180°, 270°; for diagonal angles, extend to corners accordingly.

## Filenames

```
fonts/src/svg/lines/angle_000.svg
fonts/src/svg/lines/angle_010.svg
...
fonts/src/svg/lines/angle_350.svg
```

## Visual Alignment Rules

- All angle strokes should pass through the cell center.
- Endpoints should meet the glyph bounding box at consistent geometric intersections for each angle so neighboring cells connect cleanly.
- Avoid overshoot that causes visible bleed beyond the cell in terminals; tiny overshoot is acceptable if it improves continuity.

## QA Checklist

- Render a 5×5 grid of the same angle; confirm continuous lines with no gaps.
- Render alternating angles to validate junction appearance.
- Check at common terminal sizes (11–14 pt) on Windows and macOS.
