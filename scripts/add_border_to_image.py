#!/usr/bin/env python3
"""
Add borders to images using Jordnaer brand colors.
Automatically adjusts border width/height to make images more square.

Usage:
    python add_border_to_image.py input_image.jpg
    python add_border_to_image.py input_image.jpg --color yellow
    python add_border_to_image.py input_image.jpg --border-size 30
"""

from PIL import Image, ImageDraw
import argparse
import os
from pathlib import Path

# Jordnaer brand colors from JordnaerPalette.cs
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


def calculate_border_for_square(width, height, min_border=20):
    """
    Calculate border sizes to make the image as square as possible.

    Args:
        width: Original image width
        height: Original image height
        min_border: Minimum border size on all sides

    Returns:
        tuple: (top, right, bottom, left) border sizes
    """
    # Calculate the difference
    diff = abs(width - height)

    if width > height:
        # Image is wider - add more border to top and bottom
        extra_vertical = diff // 2
        top = min_border + extra_vertical
        bottom = min_border + extra_vertical
        left = min_border
        right = min_border

        # Handle odd differences
        if diff % 2 != 0:
            bottom += 1
    elif height > width:
        # Image is taller - add more border to left and right
        extra_horizontal = diff // 2
        top = min_border
        bottom = min_border
        left = min_border + extra_horizontal
        right = min_border + extra_horizontal

        # Handle odd differences
        if diff % 2 != 0:
            right += 1
    else:
        # Already square - use uniform border
        top = bottom = left = right = min_border

    return (top, right, bottom, left)


def add_border(image_path, output_path, color_name='yellow', border_size=None, make_square=True):
    """
    Add a colored border to an image.

    Args:
        image_path: Path to input image
        output_path: Path to save output image
        color_name: Name of color from JORDNAER_COLORS
        border_size: Fixed border size (overrides make_square if provided)
        make_square: If True, adjust border to make image square
    """
    # Open the image
    img = Image.open(image_path)
    original_width, original_height = img.size

    # Convert to RGBA if the image has transparency
    has_alpha = img.mode in ('RGBA', 'LA') or (img.mode == 'P' and 'transparency' in img.info)
    if has_alpha and img.mode != 'RGBA':
        img = img.convert('RGBA')

    # Get the border color
    if color_name not in JORDNAER_COLORS:
        raise ValueError(f"Color '{color_name}' not found. Available: {list(JORDNAER_COLORS.keys())}")

    border_color = hex_to_rgb(JORDNAER_COLORS[color_name])

    # Calculate border sizes
    if border_size is not None:
        # Use fixed border size
        border = (border_size, border_size, border_size, border_size)
    elif make_square:
        # Calculate border to make image square
        border = calculate_border_for_square(original_width, original_height, min_border=20)
    else:
        # Default uniform border
        border = (20, 20, 20, 20)

    top, right, bottom, left = border

    # Create new image with border
    new_width = original_width + left + right
    new_height = original_height + top + bottom

    # Create bordered image with transparency support if needed
    if has_alpha:
        # For transparent images, start with fully transparent canvas
        bordered_img = Image.new('RGBA', (new_width, new_height), (0, 0, 0, 0))

        # Draw colored border only in the margin areas
        draw = ImageDraw.Draw(bordered_img)
        border_rgba = border_color + (255,)

        # Draw top border
        if top > 0:
            draw.rectangle([(0, 0), (new_width, top)], fill=border_rgba)
        # Draw bottom border
        if bottom > 0:
            draw.rectangle([(0, new_height - bottom), (new_width, new_height)], fill=border_rgba)
        # Draw left border
        if left > 0:
            draw.rectangle([(0, top), (left, new_height - bottom)], fill=border_rgba)
        # Draw right border
        if right > 0:
            draw.rectangle([(new_width - right, top), (new_width, new_height - bottom)], fill=border_rgba)

        # Paste the original image on top, preserving its transparency
        bordered_img.paste(img, (left, top), img)
    else:
        # For opaque images, use RGB mode
        bordered_img = Image.new('RGB', (new_width, new_height), border_color)
        bordered_img.paste(img, (left, top))

    # Save the result with best quality
    # For PNG, no quality parameter needed (lossless)
    # For JPEG, use quality=100 for best quality
    save_kwargs = {}
    if output_path.lower().endswith(('.jpg', '.jpeg')):
        # JPEG doesn't support transparency, so convert RGBA to RGB
        if bordered_img.mode == 'RGBA':
            # Create white background and composite
            rgb_img = Image.new('RGB', bordered_img.size, (255, 255, 255))
            rgb_img.paste(bordered_img, mask=bordered_img.split()[3])
            bordered_img = rgb_img
        save_kwargs['quality'] = 100
        save_kwargs['subsampling'] = 0  # Disable chroma subsampling for best quality

    bordered_img.save(output_path, **save_kwargs)

    print(f"✓ Created: {output_path}")
    print(f"  Original size: {original_width}x{original_height}")
    print(f"  New size: {new_width}x{new_height}")
    print(f"  Border: top={top}, right={right}, bottom={bottom}, left={left}")
    print(f"  Color: {color_name} ({JORDNAER_COLORS[color_name]})")


def create_all_variants(image_path, output_dir=None, border_size=None, make_square=True, output_format='png'):
    """
    Create variants of the image with all available colors.

    Args:
        image_path: Path to input image
        output_dir: Directory to save variants (default: same as input + '_bordered')
        border_size: Fixed border size (optional)
        make_square: If True, adjust border to make image square
        output_format: Output format ('png' or 'jpg')
    """
    input_path = Path(image_path)

    # Create output directory
    if output_dir is None:
        output_dir = input_path.parent / f"{input_path.stem}_bordered"
    else:
        output_dir = Path(output_dir)

    output_dir.mkdir(exist_ok=True)

    print(f"Creating variants for: {input_path.name}")
    print(f"Output directory: {output_dir}\n")

    # Determine output extension
    output_ext = f".{output_format}"

    # Create a variant for each color
    for color_name in JORDNAER_COLORS.keys():
        output_filename = f"{input_path.stem}_{color_name}{output_ext}"
        output_path = output_dir / output_filename

        add_border(
            image_path=str(input_path),
            output_path=str(output_path),
            color_name=color_name,
            border_size=border_size,
            make_square=make_square
        )
        print()

    print(f"✓ Created {len(JORDNAER_COLORS)} variants in: {output_dir}")


def main():
    parser = argparse.ArgumentParser(
        description='Add Jordnaer brand color borders to images',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=f"""
Available colors:
  {', '.join(JORDNAER_COLORS.keys())}

Examples:
  # Create variants with all colors (auto-squared, PNG by default)
  python add_border_to_image.py image.jpg

  # Create all variants as JPEG
  python add_border_to_image.py image.jpg --format jpg

  # Create single variant with specific color
  python add_border_to_image.py image.jpg --color yellow --output bordered.png

  # Use fixed border size (not squared)
  python add_border_to_image.py image.jpg --border-size 40 --no-square

  # Create all variants with custom border size
  python add_border_to_image.py image.jpg --all --border-size 30
        """
    )

    parser.add_argument('image', help='Input image path')
    parser.add_argument('--color', '-c', choices=list(JORDNAER_COLORS.keys()),
                        help='Border color (if not using --all)')
    parser.add_argument('--output', '-o', help='Output path for single image')
    parser.add_argument('--all', '-a', action='store_true',
                        help='Create variants with all colors')
    parser.add_argument('--border-size', '-b', type=int,
                        help='Fixed border size in pixels (overrides auto-square)')
    parser.add_argument('--no-square', action='store_true',
                        help='Disable automatic squaring of image')
    parser.add_argument('--output-dir', '-d',
                        help='Output directory for variants (used with --all)')
    parser.add_argument('--format', '-f', choices=['png', 'jpg', 'jpeg'],
                        default='png',
                        help='Output format for images (default: png)')

    args = parser.parse_args()

    # Validate input file exists
    if not os.path.exists(args.image):
        print(f"Error: Input file not found: {args.image}")
        return 1

    make_square = not args.no_square
    output_format = 'jpg' if args.format == 'jpeg' else args.format

    # Create all variants or single image
    if args.all or args.color is None:
        create_all_variants(
            image_path=args.image,
            output_dir=args.output_dir,
            border_size=args.border_size,
            make_square=make_square,
            output_format=output_format
        )
    else:
        # Single color variant
        input_path = Path(args.image)

        if args.output:
            output_path = args.output
        else:
            output_path = input_path.parent / f"{input_path.stem}_{args.color}{input_path.suffix}"

        add_border(
            image_path=args.image,
            output_path=output_path,
            color_name=args.color,
            border_size=args.border_size,
            make_square=make_square
        )

    return 0


if __name__ == '__main__':
    exit(main())
