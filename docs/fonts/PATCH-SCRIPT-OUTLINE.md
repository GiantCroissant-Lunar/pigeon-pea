# FontForge Patch Script — Outline

Outline of a FontForge Python script to import SVG angle glyphs into PUA of the base Sarasa font. This is documentation-first; actual script can live under `fonts/scripts/` later.

```python
import fontforge
import sys
import os

BASE_FONT_PATH = "SarasaMonoSC-Regular.ttf"   # replace with chosen Fixed CL variant
OUTPUT_FONT_PATH = "LunarSarasaMono-Regular.ttf"

ANGLE_GLYPHS = [
    (0,   0xE000, "line_angle_000", "fonts/src/svg/lines/angle_000.svg"),
    (10,  0xE001, "line_angle_010", "fonts/src/svg/lines/angle_010.svg"),
    # ... up to 350
]

def main():
    if not os.path.exists(BASE_FONT_PATH):
        print("Base font not found:", BASE_FONT_PATH)
        sys.exit(1)

    font = fontforge.open(BASE_FONT_PATH)
    default_width = font["A"].width

    for angle, codepoint, name, svg_path in ANGLE_GLYPHS:
        if not os.path.exists(svg_path):
            print("WARN: missing", svg_path)
            continue
        g = font.createChar(codepoint, name)
        g.clear()
        g.importOutlines(svg_path)
        g.width = default_width
        g.left_side_bearing = 0
        g.right_side_bearing = 0

    font.fontname = "LunarSarasaMono-Regular"
    font.fullname = "Lunar Sarasa Mono Regular"
    font.familyname = "Lunar Sarasa Mono"
    font.generate(OUTPUT_FONT_PATH)
    font.close()

if __name__ == "__main__":
    main()
```
