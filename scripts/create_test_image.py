#!/usr/bin/env python3
"""Create a simple test image for imgcat comparison."""
import os

from PIL import Image

# Create 32x32 red square
img = Image.new("RGBA", (32, 32), (255, 0, 0, 255))

# Save to tests/kitty/assets
out_dir = os.path.join("tests", "kitty", "assets")
os.makedirs(out_dir, exist_ok=True)
out_path = os.path.join(out_dir, "test_red.png")
img.save(out_path)
print(f"Created {out_path} - a 32x32 red square")
