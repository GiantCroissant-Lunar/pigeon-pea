#!/usr/bin/env python3
"""Test Kitty graphics with PNG format (f=100) instead of RGBA (f=32)."""
import base64
import io
import sys

from PIL import Image

# Create a PNG file in memory
img = Image.new("RGBA", (32, 32), (255, 0, 0, 255))
buf = io.BytesIO()
img.save(buf, format="PNG")
png_data = buf.getvalue()

# Encode to base64
b64 = base64.b64encode(png_data).decode("ascii")

# Kitty protocol with PNG format (f=100) like the minimal example
# ESC _G a=T,f=100; [base64] ESC \
cmd = f"\x1b_Ga=T,f=100;{b64}\x1b\\"

print("Testing Kitty graphics with PNG format (f=100)...", file=sys.stderr)
print(f"PNG size: {len(png_data)} bytes", file=sys.stderr)
print(f"Base64 length: {len(b64)}", file=sys.stderr)
print(f"Command length: {len(cmd)}", file=sys.stderr)
print(f"First 40 chars: {repr(cmd[:40])}", file=sys.stderr)

sys.stdout.write(cmd)
sys.stdout.flush()

print("\n\nIf you see a red square above, Kitty with PNG works!", file=sys.stderr)
print("(This uses f=100 for PNG instead of f=32 for RGBA)", file=sys.stderr)
print("Press Enter to exit...", file=sys.stderr)
input()
