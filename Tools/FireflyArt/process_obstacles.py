"""Firefly obstacle sprites (RGB on Firefly's drifting magenta) → clean cutouts on
pure magenta, cropped tight with a 2 % margin, feet at the bottom edge, saved as
Resources/CoastRun/Obs_<Key>.png for PaintedProp. Run after exporting *_key.png."""
import os, glob
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "CoastRun")


def figure_mask(a):
    key = np.median(np.concatenate([a[:8, :8].reshape(-1, 3), a[-8:, -8:].reshape(-1, 3)]), axis=0)
    d_key = np.abs(a - key).sum(axis=2)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pinkish = (r > 170) & (g < 90) & (b > 90) & (r - g > 120)
    return ~((d_key < 90) | pinkish)


for path in sorted(glob.glob(os.path.join(HERE, "Obs_*_key.png"))):
    key = os.path.basename(path)[len("Obs_"):-len("_key.png")]
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(np.int16)
    m = figure_mask(a)
    # drop the soft drop-shadow Firefly sometimes paints: dark, low-saturation pixels
    # touching the key colour are shadow, not prop
    ys, xs = np.where(m)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    fig = im.crop((x0, y0, x1 + 1, y1 + 1))
    fm = Image.fromarray((m[y0:y1 + 1, x0:x1 + 1] * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(5))
    fa = np.asarray(fig).astype(np.float32)
    r, g, b = fa[..., 0], fa[..., 1], fa[..., 2]
    pink = ((r - g) > 70) & ((b - g) > 55)
    if pink.any():
        blurred = np.asarray(fig.filter(ImageFilter.MedianFilter(7))).astype(np.float32)
        fa[pink] = blurred[pink]
        fig = Image.fromarray(np.clip(fa, 0, 255).astype(np.uint8))
    w, h = fig.size
    pad = int(max(w, h) * 0.02)
    canvas = Image.new("RGB", (w + 2 * pad, h + pad), (255, 0, 255))
    canvas.paste(fig, (pad, 0), fm)
    # power-of-two friendly height keeps the importer happy and the quad crisp
    # longest side 1024 (wide decals like the puddle would otherwise blow past 2048)
    scale = 1024 / max(canvas.size)
    canvas = canvas.resize((max(8, int(canvas.size[0] * scale)), max(8, int(canvas.size[1] * scale))), Image.LANCZOS)
    out = os.path.join(OUT, f"Obs_{key}.png")
    canvas.save(out, optimize=True)
    print("wrote", out, canvas.size)
