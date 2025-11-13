#!/usr/bin/env python3
"""Debug Kitty graphics test with extra logging."""
import sys
import base64

width = 32
height = 32
rgba = bytearray(width * height * 4)
for i in range(0, len(rgba), 4):
    rgba[i] = 255
    rgba[i+1] = 0
    rgba[i+2] = 0
    rgba[i+3] = 255

b64 = base64.b64encode(rgba).decode('ascii')
cmd = f'\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\'

print(f"Sending Kitty RGBA image: {len(rgba)} bytes -> {len(b64)} base64", file=sys.stderr)
print(f"First 50: {repr(cmd[:50])}", file=sys.stderr)

sys.stdout.write(cmd)
sys.stdout.flush()

print("\n\nIf you see a red square above, Kitty graphics works!", file=sys.stderr)
print("Press Enter to exit...", file=sys.stderr)
input()

