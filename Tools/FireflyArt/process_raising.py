"""Raising-screen art: Firefly standing girl (magenta key) → RGBA cutout Raise_Girl_<Mood>.png,
room background → UI_Raising_Room.png (cropped to the room panel aspect 700:520)."""
import os, glob
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "CoastRun")

def cutout(path, out, size=1024):
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(np.int16)
    key = np.median(np.concatenate([a[:8, :8].reshape(-1, 3), a[-8:, -8:].reshape(-1, 3)]), axis=0)
    d = np.abs(a - key).sum(axis=2)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pinkish = (r > 170) & (g < 90) & (b > 90) & (r - g > 120)
    m = ~((d < 90) | pinkish)
    ys, xs = np.where(m)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    fig = im.crop((x0, y0, x1 + 1, y1 + 1))
    mask = Image.fromarray((m[y0:y1 + 1, x0:x1 + 1] * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
    fa = np.asarray(fig).astype(np.float32)
    rr, gg, bb = fa[..., 0], fa[..., 1], fa[..., 2]
    pink = ((rr - gg) > 70) & ((bb - gg) > 55)
    if pink.any():
        blurred = np.asarray(fig.filter(ImageFilter.MedianFilter(7))).astype(np.float32)
        fa[pink] = blurred[pink]
        fig = Image.fromarray(np.clip(fa, 0, 255).astype(np.uint8))
    rgba = fig.convert("RGBA")
    rgba.putalpha(mask)
    w, h = rgba.size
    pad = int(max(w, h) * 0.02)
    canvas = Image.new("RGBA", (w + 2 * pad, h + pad), (0, 0, 0, 0))
    canvas.paste(rgba, (pad, 0), rgba)
    scale = size / max(canvas.size)
    canvas = canvas.resize((max(8, int(canvas.size[0] * scale)), max(8, int(canvas.size[1] * scale))), Image.LANCZOS)
    canvas.save(out, optimize=True)
    print("wrote", out, canvas.size)

for p in sorted(glob.glob(os.path.join(HERE, "Raise_Girl_*_key.png"))):
    mood = os.path.basename(p)[len("Raise_Girl_"):-len("_key.png")]
    cutout(p, os.path.join(OUT, f"Raise_Girl_{mood}.png"))

room = os.path.join(HERE, "UI_Raising_Room_raw.png")
if os.path.exists(room):
    im = Image.open(room).convert("RGB")
    W, H = im.size
    target = 700 / 520
    ch = int(W / target)
    top = int((H - ch) * 0.55)   # 바닥 쪽을 조금 더 남긴다
    crop = im.crop((0, top, W, top + ch)).resize((1024, int(1024 / target)), Image.LANCZOS)
    out = os.path.join(OUT, "UI_Raising_Room.png")
    crop.save(out, optimize=True)
    print("wrote", out, crop.size)
