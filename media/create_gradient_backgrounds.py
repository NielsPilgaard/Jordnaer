from PIL import Image, ImageDraw
import os

# Instagram story dimensions
WIDTH = 1024
HEIGHT = 1920

# Jordnaer colors
JORDNAER_COLORS = {
    'yellow': '#dbab45',      # GLÆDE - Yellow-orange
    'green': '#878e64',       # RO - Green
    'blue': '#41556b',        # MØDE - Dark Blue
    'red': '#673417',         # MØDE Red - Dark Red
    'beige': '#cfc1a6',       # OMSORG - Beige
    'pale_blue': '#a9c0cf',   # LEG - Pale Blue
}

def hex_to_rgb(hex_color):
    """Convert hex color to RGB tuple."""
    hex_color = hex_color.lstrip('#')
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))

def adjust_brightness(rgb, factor):
    """Adjust RGB color brightness by a factor."""
    return tuple(max(0, min(255, int(c * factor))) for c in rgb)

def create_gradient_background(base_color, output_path, gradient_intensity=0.15):
    """
    Create a subtle gradient background.

    Args:
        base_color: Hex color string
        output_path: Path to save the image
        gradient_intensity: How much darker/lighter the gradient should be (0.1 = 10% difference)
    """
    img = Image.new('RGB', (WIDTH, HEIGHT))
    draw = ImageDraw.Draw(img)

    base_rgb = hex_to_rgb(base_color)

    # Create gradient from slightly lighter at top to slightly darker at bottom
    for y in range(HEIGHT):
        # Calculate gradient factor (1.0 + intensity at top, 1.0 - intensity at bottom)
        progress = y / HEIGHT
        factor = 1.0 + gradient_intensity - (2 * gradient_intensity * progress)

        color = adjust_brightness(base_rgb, factor)
        draw.line([(0, y), (WIDTH, y)], fill=color)

    img.save(output_path, 'PNG', optimize=True)
    print(f"Created: {output_path}")

# Create output directory if it doesn't exist
output_dir = os.path.join(os.path.dirname(__file__), 'instagram_backgrounds')
os.makedirs(output_dir, exist_ok=True)

# Generate gradient backgrounds for each color
for name, color in JORDNAER_COLORS.items():
    output_path = os.path.join(output_dir, f'{name}_gradient_square.png')
    create_gradient_background(color, output_path)

print(f"\nAll 6 gradient backgrounds created in: {output_dir}")
