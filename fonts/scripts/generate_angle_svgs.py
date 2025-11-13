#!/usr/bin/env python3
"""
Generate 36 angle SVGs (0..350 step 10) for font import.

Each SVG is a line passing through the canvas center at the target angle,
with square linecaps to visually connect across adjacent cells in terminals.

Canvas: 1000 x 1000 units
Stroke width (regular): 80 units (adjust as needed)

Outputs: fonts/src/svg/lines/angle_XXX.svg
"""
import math
from pathlib import Path

OUT_DIR = Path(__file__).resolve().parents[1] / "src" / "svg" / "lines"
CANVAS = 1000
HALF = CANVAS / 2
STROKE = 80  # Regular weight; Bold can be scaled later


def line_intersections_through_center(angle_degrees: float, size: float):
    """Compute the intersection points of a line through the center at the given angle
    with the square canvas boundary. Returns two points (x1,y1,x2,y2).
    """
    theta = math.radians(angle_degrees)
    # Direction vector
    dx = math.cos(theta)
    dy = math.sin(theta)

    # Parametric line: (x, y) = (cx, cy) + t*(dx, dy)
    cx = cy = size / 2

    # Intersect with square [0,size] x [0,size]
    ts = []
    eps = 1e-9

    # x = 0
    if abs(dx) > eps:
        t = (0 - cx) / dx
        y = cy + t * dy
        if 0 - 1 <= y <= size + 1:
            ts.append(t)
    # x = size
    if abs(dx) > eps:
        t = (size - cx) / dx
        y = cy + t * dy
        if 0 - 1 <= y <= size + 1:
            ts.append(t)
    # y = 0
    if abs(dy) > eps:
        t = (0 - cy) / dy
        x = cx + t * dx
        if 0 - 1 <= x <= size + 1:
            ts.append(t)
    # y = size
    if abs(dy) > eps:
        t = (size - cy) / dy
        x = cx + t * dx
        if 0 - 1 <= x <= size + 1:
            ts.append(t)

    if len(ts) < 2:
        # Fallback: extend long and clip via SVG viewport
        t1, t2 = -size, size
    else:
        ts.sort()
        t1, t2 = ts[0], ts[-1]

    x1 = cx + t1 * dx
    y1 = cy + t1 * dy
    x2 = cx + t2 * dx
    y2 = cy + t2 * dy
    return x1, y1, x2, y2


def svg_for_angle(angle: int) -> str:
    x1, y1, x2, y2 = line_intersections_through_center(angle, CANVAS)
    svg_lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<svg xmlns="http://www.w3.org/2000/svg"',
        f'     width="{CANVAS}" height="{CANVAS}"',
        f'     viewBox="0 0 {CANVAS} {CANVAS}">',
        f'  <line x1="{x1:.3f}" y1="{y1:.3f}"',
        f'        x2="{x2:.3f}" y2="{y2:.3f}"',
        f'        stroke="black" stroke-width="{STROKE}"',
        '        stroke-linecap="square"/>',
        "</svg>",
    ]
    svg = "\n".join(svg_lines) + "\n"
    return svg


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for angle in range(0, 360, 10):
        name = f"angle_{angle:03d}.svg"
        path = OUT_DIR / name
        path.write_text(svg_for_angle(angle), encoding="utf-8")
    print(f"Generated SVGs in {OUT_DIR}")


if __name__ == "__main__":
    main()
