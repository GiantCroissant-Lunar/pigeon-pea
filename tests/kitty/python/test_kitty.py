#!/usr/bin/env python3
"""Ultra-minimal Kitty graphics test to verify terminal support."""
import sys
import base64

# Create a simple 32x32 red square
width = 32
height = 32
rgba = bytearray(width * height * 4)
for i in range(0, len(rgba), 4):
    rgba[i] = 255      # R
    rgba[i+1] = 0      # G
    rgba[i+2] = 0      # B
    rgba[i+3] = 255    # A

# Encode to base64
b64 = base64.b64encode(rgba).decode('ascii')

# Send Kitty graphics command: transmit and display (a=T)
# f=32 means RGBA, s=width, v=height
sys.stdout.write(f'\x1b_Ga=T,f=32,s={width},v={height};{b64}\x1b\\')
sys.stdout.flush()

print("\n\nIf you see a red square above, Kitty graphics works!")
print("Press Enter to exit...")
input()

