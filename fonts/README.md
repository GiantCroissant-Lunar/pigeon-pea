# Fonts — Development Pipeline

This folder contains the assets and scripts to build the project’s custom CJK monospace font with PUA map glyphs.

## Layout

```text
fonts/
  src/
    svg/
      lines/
        angle_000.svg .. angle_350.svg
  scripts/
    generate_angle_svgs.py
    patch_sarasa.py
  dist/
    (generated TTFs)
```

## Prerequisites

- FontForge with Python bindings (use `fontforge -script`)
- Base fonts: Sarasa Gothic Fixed CL (Regular and Bold) — with or without Nerd patches

## Steps

1. Generate SVGs

```bash
task fonts:generate-svgs
```

2. Build fonts (set env vars to base TTFs)

```bash
export BASE_FONT_REGULAR="/path/to/SarasaFixedCL-Regular.ttf"
export BASE_FONT_BOLD="/path/to/SarasaFixedCL-Bold.ttf"
task fonts:build
```

Outputs: `fonts/dist/LunarSarasaMono-Regular.ttf`, `fonts/dist/LunarSarasaMono-Bold.ttf`.

3. Create dev bundle skeleton

```bash
task bundle:rio:dev
```

See also: docs/rfcs/RFC-020-Font-Pipeline-and-PUA-Layout.md and docs/fonts/PUA-LAYOUT.md.
