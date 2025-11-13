from PIL import Image, ImageDraw, ImageFont
import os

# Create a new image
width, height = 600, 400
img = Image.new('RGB', (width, height), color=(40, 40, 50))
draw = ImageDraw.Draw(img)

# Draw warrior (left side - green)
# Head
draw.ellipse([120, 180, 180, 240], outline='green', width=3)
# Body
draw.line([150, 240, 150, 320], fill='green', width=3)
# Arms
draw.line([150, 260, 120, 290], fill='green', width=3)
draw.line([150, 260, 200, 280], fill='green', width=3)
# Legs
draw.line([150, 320, 135, 370], fill='green', width=3)
draw.line([150, 320, 165, 370], fill='green', width=3)
# Sword
draw.line([200, 280, 280, 260], fill='white', width=3)
draw.line([280, 260, 300, 265], fill='yellow', width=2)

# Draw monster (right side - red)
# Head
draw.ellipse([400, 150, 480, 250], outline='red', width=3)
# Eyes (scary)
draw.ellipse([420, 180, 435, 195], fill='red')
draw.ellipse([445, 180, 460, 195], fill='red')
# Body
draw.line([440, 250, 440, 330], fill='red', width=3)
# Arms
draw.line([440, 270, 410, 300], fill='red', width=3)
draw.line([440, 270, 470, 300], fill='red', width=3)
# Legs
draw.line([440, 330, 420, 370], fill='red', width=3)
draw.line([440, 330, 460, 370], fill='red', width=3)

# Add text
try:
    font_large = ImageFont.truetype("arial.ttf", 20)
    font_small = ImageFont.truetype("arial.ttf", 14)
except:
    font_large = ImageFont.load_default()
    font_small = ImageFont.load_default()

draw.text((220, 20), "?? BATTLE! ??", fill='yellow', font=font_large)
draw.text((110, 375), "Warrior", fill='cyan', font=font_small)
draw.text((400, 375), "Monster", fill='orange', font=font_small)

# Add some action lines
draw.line([280, 260, 380, 280], fill='white', width=2)
draw.line([275, 265, 375, 285], fill='yellow', width=1)

# Save the image to docs/assets/images
out_dir = os.path.join('docs', 'assets', 'images')
os.makedirs(out_dir, exist_ok=True)
out_path = os.path.join(out_dir, 'some.png')
img.save(out_path)
print(f"Battle scene created: {out_path}")
