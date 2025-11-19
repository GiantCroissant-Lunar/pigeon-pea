#!/usr/bin/env python3
"""Test iTerm2 inline images protocol instead of Kitty graphics."""
import base64
import sys

# Create a simple 32x32 red square
width = 32
height = 32
rgba = bytearray(width * height * 4)
for i in range(0, len(rgba), 4):
    rgba[i] = 255  # R
    rgba[i + 1] = 0  # G
    rgba[i + 2] = 0  # B
    rgba[i + 3] = 255  # A

# Encode to base64
b64 = base64.b64encode(rgba).decode("ascii")

# iTerm2 protocol: ESC ] 1337 ; File=inline=1:[base64] BEL
# This is what wezterm imgcat might actually use!
cmd = f"\x1b]1337;File=inline=1;width=32px;height=32px:{b64}\x07"

print("Sending iTerm2 inline image protocol...", file=sys.stderr)
print(f"Command length: {len(cmd)}", file=sys.stderr)

sys.stdout.write(cmd)
sys.stdout.flush()

print("\n\nIf you see a red square above, iTerm2 protocol works!", file=sys.stderr)
print("(This might be what wezterm imgcat actually uses)", file=sys.stderr)
print("Press Enter to exit...", file=sys.stderr)
input()
