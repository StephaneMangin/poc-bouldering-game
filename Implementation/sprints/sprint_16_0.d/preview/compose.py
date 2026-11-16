#!/usr/bin/env python3
"""Compose review sheets from preview renders.

Usage: compose.py VIEWPOINTS_JSON RENDERS_DIR PHOTOS_DIR OUTPUT_DIR VERSION
  - <view>_photo_match.png : reference photo next to the render of the same viewpoint
  - passes_<view>.png      : whitebox, greybox and art renders of one viewpoint side by side
Run with the repository .venv python (Pillow).
"""

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SHEET_HEIGHT = 900
MARGIN = 24
CAPTION = 56
BACKGROUND = (24, 26, 28)
INK = (240, 240, 236)


def font(size):
    for candidate in ("DejaVuSans-Bold.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def scaled(image, height):
    return image.resize((round(image.width * height / image.height), height), Image.LANCZOS)


def sheet(panels, title):
    """panels: list of (caption, image); all scaled to the same height and laid out in a row."""
    images = [(caption, scaled(image.convert("RGB"), SHEET_HEIGHT)) for caption, image in panels]
    width = sum(image.width for _, image in images) + MARGIN * (len(images) + 1)
    canvas = Image.new("RGB", (width, SHEET_HEIGHT + CAPTION * 2 + MARGIN), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    draw.text((MARGIN, 12), title, fill=INK, font=font(30))
    x = MARGIN
    for caption, image in images:
        canvas.paste(image, (x, CAPTION))
        draw.text((x, CAPTION + SHEET_HEIGHT + 10), caption, fill=INK, font=font(24))
        x += image.width + MARGIN
    return canvas


def main():
    viewpoints, renders, photos, output, version = sys.argv[1:6]
    renders, photos, output = Path(renders), Path(photos), Path(output)
    output.mkdir(parents=True, exist_ok=True)
    views = json.loads(Path(viewpoints).read_text(encoding="utf-8"))["views"]
    for view in views:
        name = view["name"]
        art = renders / f"{name}_art.png"
        if "photo" in view and art.exists():
            photo = Image.open(photos / view["photo"])
            result = sheet([(f"Photo {view['photo']}", photo), (f"Render {version} (art pass)", Image.open(art))],
                           f"{name}: reference photo vs generated gym ({version})")
            result.save(output / f"{name}_photo_match.png")
        passes = [(p, renders / f"{name}_{p}.png") for p in ("whitebox", "greybox", "art")]
        if all(path.exists() for _, path in passes):
            result = sheet([(p.capitalize(), Image.open(path)) for p, path in passes],
                           f"{name}: level design passes ({version})")
            result.save(output / f"passes_{name}.png")
    print(f"sheets written to {output}")


main()
