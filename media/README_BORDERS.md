# Image Border Tool

Add Jordnaer brand color borders to images for social media posts.

## Installation

Install the required dependency (Pillow for image processing):

```bash
pip install Pillow
```

## Quick Start

```bash
# Create variants with all Jordnaer colors (auto-squared, PNG by default)
python add_border_to_image.py my_image.jpg

# This creates a folder `my_image_bordered/` with 6 PNG variants:
# - my_image_yellow.png
# - my_image_green.png
# - my_image_blue.png
# - my_image_red.png
# - my_image_beige.png
# - my_image_pale_blue.png
```

## Usage Examples

### Create All Color Variants (Recommended)

```bash
# Auto-squared with all colors (PNG by default)
python add_border_to_image.py photo.jpg

# Create as JPEG instead
python add_border_to_image.py photo.jpg --format jpg

# Custom border size with all colors
python add_border_to_image.py photo.jpg --border-size 40

# Save to specific directory
python add_border_to_image.py photo.jpg --output-dir ./social_posts
```

### Create Single Variant

```bash
# Yellow border (auto-squared)
python add_border_to_image.py photo.jpg --color yellow

# Green border with custom output name
python add_border_to_image.py photo.jpg --color green --output my_post.jpg

# Fixed 50px border, not squared
python add_border_to_image.py photo.jpg --color blue --border-size 50 --no-square
```

## Available Colors

All colors from [JordnaerPalette.cs](../src/web/Jordnaer/Features/Theme/JordnaerPalette.cs):

- `yellow` - #dbab45 (GLÆDE - Joy)
- `green` - #878e64 (RO - Calm)
- `blue` - #41556b (MØDE - Meeting)
- `red` - #673417 (MØDE Red)
- `beige` - #cfc1a6 (OMSORG - Care)
- `pale_blue` - #a9c0cf (LEG - Play)

## How Auto-Squaring Works

The script automatically calculates border sizes to make images as square as possible:

- **Landscape images** (wider): Adds extra border to top and bottom
- **Portrait images** (taller): Adds extra border to left and right
- **Square images**: Uses uniform border on all sides

This is perfect for social media posts that prefer square formats (Instagram, Facebook, etc.).

### Example

```
Original: 1200x800 (landscape)
Result:   1240x1240 (square)
- Top/Bottom: 220px border
- Left/Right: 20px border
```

## Command Line Options

```
positional arguments:
  image                 Input image path

options:
  -h, --help           Show help message
  --color COLOR, -c    Border color: yellow, green, blue, red, beige, pale_blue
  --output PATH, -o    Output path for single image
  --all, -a            Create variants with all colors (default if no color specified)
  --border-size SIZE, -b  Fixed border size in pixels (disables auto-square)
  --no-square          Disable automatic squaring
  --output-dir DIR, -d Directory for variants (with --all)
  --format FMT, -f     Output format: png (default), jpg, jpeg
```

## Tips

1. **For social media**: Use the default auto-square mode - most platforms prefer square images
2. **Pick the right color**: Choose colors that complement your image content
3. **Batch processing**: Process multiple images with a simple loop:

```bash
for img in *.jpg; do
    python add_border_to_image.py "$img"
done
```

4. **Instagram posts**: Default settings work great (auto-squared, 20px minimum border)
5. **Story/Reel formats**: Use `--border-size 40 --no-square` for portrait images

## Output Quality

Images are saved with maximum quality:
- **JPEG**: Quality 100 with no chroma subsampling (best possible quality)
- **PNG**: Lossless compression (no quality loss)

Perfect for social media posts where you want to maintain the best visual appearance.
