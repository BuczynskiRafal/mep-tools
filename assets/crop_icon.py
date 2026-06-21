"""Build the Revit ribbon icons directly from the hand-drawn assets/screen.png.

No redrawing: we only crop the drawing to its content, pad it to a square,
turn the white paper into transparency (alpha from line darkness) and downscale.
Lines are mildly thickened so they survive the 32px / 16px sizes.

Run:  python assets/crop_icon.py
"""
import os
from PIL import Image, ImageFilter, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
RES_DIR = os.path.join(ROOT, "src", "FastConnect.Revit", "Resources")
SRC = os.path.join(HERE, "screen.png")

WHITE_CUTOFF = 205      # luminance above this is treated as blank paper
MARGIN_FRAC = 0.06      # padding around the drawing, as a fraction of its size


def load_darkness():
    """Return (rgb, darkness) where darkness is 0 (paper) .. 255 (ink)."""
    rgb = Image.open(SRC).convert("RGB")
    gray = rgb.convert("L")
    darkness = gray.point(lambda v: 0 if v >= WHITE_CUTOFF else 255 - v)
    return rgb, darkness


def content_bbox(darkness):
    return darkness.point(lambda v: 255 if v > 0 else 0).getbbox()


def build(thicken):
    rgb, darkness = load_darkness()
    box = content_bbox(darkness)
    rgb = rgb.crop(box)
    darkness = darkness.crop(box)

    if thicken > 1:
        darkness = darkness.filter(ImageFilter.MaxFilter(thicken))

    # stretch contrast so faint pencil edges still read as ink
    alpha = darkness.point(lambda v: min(255, int(v * 1.6)))

    # paint anti-aliased edges with the ink colour so they don't fade to white
    ink = (122, 40, 40)
    coloured = Image.new("RGB", rgb.size, ink)
    icon = coloured.convert("RGBA")
    icon.putalpha(alpha)

    # pad to a centred square with transparent margin
    w, h = icon.size
    side = int(max(w, h) * (1 + 2 * MARGIN_FRAC))
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(icon, ((side - w) // 2, (side - h) // 2))
    return square


def main():
    os.makedirs(RES_DIR, exist_ok=True)

    big32 = build(thicken=13)
    big16 = build(thicken=21)
    icon32 = big32.resize((32, 32), Image.LANCZOS)
    icon16 = big16.resize((16, 16), Image.LANCZOS)
    icon32.save(os.path.join(RES_DIR, "icon32.png"))
    icon16.save(os.path.join(RES_DIR, "icon16.png"))

    # preview on a ribbon-grey card: smooth + real 32px/16px pixels
    bg = (237, 237, 237, 255)
    card = Image.new("RGBA", (560, 300), bg)
    card.alpha_composite(big32.resize((256, 256), Image.LANCZOS), (12, 22))
    card.alpha_composite(icon32.resize((192, 192), Image.NEAREST), (300, 22))
    card.alpha_composite(icon16.resize((96, 96), Image.NEAREST), (300, 224))
    card.convert("RGB").save(os.path.join(HERE, "icon_preview.png"))

    print("crop bbox:", content_bbox(load_darkness()[1]))
    print("wrote 32/16 icons + assets/icon_preview.png")


if __name__ == "__main__":
    main()
