#!/usr/bin/env python3
"""
FontForge script to patch Sarasa (or Sarasa + Nerd) with PUA angle glyphs.

Usage (example):
  fontforge -script fonts/scripts/patch_sarasa.py \
    --base path/to/SarasaFixedCL-Regular.ttf \
    --output fonts/dist/LunarSarasaMono-Regular.ttf \
    --svg-dir fonts/src/svg/lines

Run again with Bold base to generate Bold output.
"""
import os
import sys

try:
    import fontforge  # type: ignore
except Exception as e:
    print("ERROR: This script must be run via FontForge: fontforge -script ...")
    print(e)
    sys.exit(2)


def parse_args(argv):
    base = None
    output = None
    svg_dir = "fonts/src/svg/lines"
    start_cp = 0xE000
    angles = list(range(0, 360, 10))
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--base":
            i += 1; base = argv[i]
        elif a == "--output":
            i += 1; output = argv[i]
        elif a == "--svg-dir":
            i += 1; svg_dir = argv[i]
        elif a == "--start-cp":
            i += 1; start_cp = int(argv[i], 0)
        elif a == "--angles":
            i += 1; angles = [int(x) for x in argv[i].split(',')]
        i += 1
    if not base or not output:
        print("Usage: --base <ttf> --output <ttf> [--svg-dir <dir>] [--start-cp 0xE000]")
        sys.exit(1)
    return base, output, svg_dir, start_cp, angles


def main():
    base, output, svg_dir, start_cp, angles = parse_args(sys.argv[1:])
    if not os.path.exists(base):
        print("Base font not found:", base)
        sys.exit(1)
    if not os.path.isdir(svg_dir):
        print("SVG dir not found:", svg_dir)
        sys.exit(1)

    font = fontforge.open(base)
    default_width = font["A"].width if "A" in font else None
    if default_width is None:
        # Fallback: use space width
        default_width = font["space"].width

    # Import angle glyphs
    for idx, angle in enumerate(angles):
        cp = start_cp + idx
        name = f"line_angle_{angle:03d}"
        svg_path = os.path.join(svg_dir, f"angle_{angle:03d}.svg")
        if not os.path.exists(svg_path):
            print(f"WARN: Missing SVG {svg_path}; skipping CP U+{cp:04X}")
            continue
        print(f"Importing {angle:03d}° -> U+{cp:04X}")
        g = font.createChar(cp, name)
        g.clear()
        g.importOutlines(svg_path)
        g.width = default_width
        # Set side bearings to 0 for tile continuity
        try:
            g.left_side_bearing = 0
            g.right_side_bearing = 0
        except Exception:
            pass

    # Update names for output family
    # If the base is already Nerd-patched, we retain glyph sets.
    font.fontname = font.fontname.replace("Sarasa", "LunarSarasaMono") if font.fontname else "LunarSarasaMono"
    font.fullname = font.fullname.replace("Sarasa", "Lunar Sarasa Mono") if font.fullname else "Lunar Sarasa Mono"
    font.familyname = "Lunar Sarasa Mono"

    # Generate TTF
    out_dir = os.path.dirname(output)
    if out_dir and not os.path.exists(out_dir):
        os.makedirs(out_dir, exist_ok=True)
    font.generate(output)
    font.close()
    print("Generated:", output)


if __name__ == "__main__":
    main()

