#!/usr/bin/env python3
"""Test iTerm2 protocol matching exactly what wezterm imgcat sends."""
import base64
import io
import sys

from PIL import Image

# Create a PNG file in memory (like imgcat does)
img = Image.new("RGBA", (32, 32), (255, 0, 0, 255))
buf = io.BytesIO()
img.save(buf, format="PNG")
png_data = buf.getvalue()

# Encode to base64
b64 = base64.b64encode(png_data).decode("ascii")

# iTerm2 protocol exactly like imgcat: ESC ] 1337 ; File=size=N;inline=1:[base64] BEL
cmd = f"\x1b]1337;File=size={len(png_data)};inline=1:{b64}\x07"

print("Sending iTerm2 inline image (PNG format)...", file=sys.stderr)
print(f"PNG size: {len(png_data)} bytes", file=sys.stderr)
print(f"Base64 length: {len(b64)}", file=sys.stderr)
print(f"Total command length: {len(cmd)}", file=sys.stderr)
print(f"First 50 chars: {repr(cmd[:50])}", file=sys.stderr)

sys.stdout.write(cmd)
sys.stdout.flush()

print("\n\nIf you see a red square above, iTerm2 protocol works!", file=sys.stderr)
print("Press Enter to exit...", file=sys.stderr)
input()
