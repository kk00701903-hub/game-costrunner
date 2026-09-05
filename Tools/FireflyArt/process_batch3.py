"""Batch 3 post-processing: Jeju facades C-H, stucco/basalt tiles, Hallasan far layer,
character pose sprites. Raw Firefly exports in Tools/FireflyArt → Assets/Resources/CoastRun.
Run: python Tools/FireflyArt/process_batch3.py
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "CoastRun")
os.makedirs(OUT, exist_ok=True)


def load(name):
    return Image.open(os.path.join(HERE, name)).convert("RGB")


def save(im, name):
    p = os.path.join(OUT, name)
    im.save(p, optimize=True)
    print("wrote", name, im.size, im.mode)


# ── 1. Facades: crop away sky / pavement bands, resample to 1024² ─────────────
def facade_crop(im, top, bottom):
    w, h = im.size
    return im.crop((0, int(h * top), w, int(h * bottom))).resize((1024, 1024), Image.LANCZOS)


# (top, bottom) fractions of the raw image that hold the building only.
FACADES = {
    "C": (0.00, 0.95),   # tangerine shop: pavement strip at the bottom
    "D": (0.00, 0.90),   # basalt cafe (night-lit → daylight below)
    "E": (0.245, 0.80),   # seafood restaurant: sky above, pavement below
    "F": (0.03, 0.97),   # yellow guesthouse
    "G": (0.33, 0.745),   # convenience store: sky + road
    "H": (0.00, 0.90),   # stone house: pavement strip at the bottom
}
for k, (t, b) in FACADES.items():
    raw = load(f"Facade_{k}.png")
    if k == "D":
        # Firefly painted the cafe at dusk; lift it to a noon exposure so it does
        # not read as a black hole between the pastel shops.
        a = np.asarray(raw).astype(np.float32) / 255.0
        a = np.power(a, 0.55) * 1.05
        raw = Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8))
        raw = ImageEnhance.Color(raw).enhance(0.9)
    save(facade_crop(raw, t, b), f"Tex_Facade_{k}.png")


# ── 2. Seamless tiles: roll-blend so the wrap edge disappears ───────────────
def seamless(im, size=1024, feather=0.18):
    im = im.resize((size, size), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32)
    rolled = np.roll(np.roll(a, size // 2, axis=0), size // 2, axis=1)
    # weight: 0 at the centre cross of the rolled image (= original edges), 1 elsewhere
    y = np.abs(np.arange(size) - size / 2) / (size / 2)
    x = np.abs(np.arange(size) - size / 2) / (size / 2)
    wy = np.clip(y / feather, 0, 1)
    wx = np.clip(x / feather, 0, 1)
    w = np.minimum(wy[:, None], wx[None, :])[:, :, None]
    # Near the centre cross the rolled image carries the original's outer edges;
    # fading the original in there hides the wrap seam.
    blend = rolled * w + a * (1 - w)
    return Image.fromarray(np.clip(blend, 0, 255).astype(np.uint8))


# Stucco: brighten slightly toward cream, then seamless.
st = load("Stucco_raw.png")
st = ImageEnhance.Brightness(st).enhance(1.04)
st = ImageEnhance.Color(st).enhance(0.85)
save(seamless(st, 1024), "Tex_Wall_Stucco.png")

# Basalt: the raw came out night-lit; lift to a sunlit dark grey and desaturate the blue.
bs = load("Stonewall_raw.png")
a = np.asarray(bs).astype(np.float32) / 255.0
lum = a.mean(axis=2, keepdims=True)
# push toward neutral grey, gamma-lift, keep some cool tint
neutral = np.repeat(lum, 3, axis=2)
a = neutral * 0.8 + a * 0.2
a = np.power(a, 0.62) * 0.92 + 0.06
a[..., 2] *= 1.03
bs = Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8))
save(seamless(bs, 1024), "Tex_Stonewall_Jeju.png")


# ── 3. Far layer: Hallasan + coastline, sky removed, sea kept (with fade) ────
far = load("Far_Jeju_raw.png")      # 2048×640, horizon line ≈ y 0.63
W, H = far.size
arr = np.asarray(far).astype(np.float32)
# Sky detection: bright cyan above the mountain. Build alpha from a per-pixel
# "how sky-like" test, then keep everything below the horizon opaque.
r, g, b = arr[..., 0], arr[..., 1], arr[..., 2]
yy = np.arange(H)[:, None] / H
# Sky and mountain share the same cyan hue; what separates them is value —
# sky/cloud pixels sit at V ≥ 0.96 while the mountain body stays ≤ 0.92.
# Per column, the first "dark enough" pixel from the top is the silhouette
# edge; everything below it belongs to the layer (haze gaps included).
v = arr.max(axis=2) / 255.0
# Sky brightens toward the horizon, so the running maximum of V down each
# column keeps rising through sky; the mountain is the first place where V
# falls clearly (> 0.06) below that running max and stays there.
from scipy.ndimage import uniform_filter1d
vs = uniform_filter1d(v, size=5, axis=0)
runmax = np.maximum.accumulate(vs, axis=0)
mtn = (runmax - vs) > 0.035
run = np.zeros_like(mtn, dtype=np.int32)
for y in range(H - 2, -1, -1):
    run[y] = np.where(mtn[y], run[y + 1] + 1, 0)
solid = run >= 24
first = np.where(solid.any(axis=0), solid.argmax(axis=0), int(H * 0.62))
first = np.clip(first, int(H * 0.10), H)
# median filter kills any remaining single-column spikes, then a light blur
from scipy.ndimage import median_filter
first = median_filter(first, size=41, mode="nearest")
k = 9
first = np.convolve(np.pad(first, k // 2, mode="edge"), np.ones(k) / k, mode="valid")
hard = (np.arange(H)[:, None] >= first[None, :]).astype(np.float32)
# Above the detected edge, let the hazy lower slopes fade in by their own
# darkness relative to the sky so the cut is not a straight line.
# Feather the edge over ~14 px so the hazy slopes dissolve into the sky quad
# (which carries nearly the same colour) instead of ending on a line.
dist = np.arange(H)[:, None] - first[None, :]
alpha = np.clip((dist + 14) / 28.0, 0, 1)
# Below the shoreline (y > 0.60 H) everything is land/sea: ramp to opaque, then
# fade toward the bottom so the quad blends into the real 3D sea.
alpha = np.maximum(alpha, np.clip((yy - 0.52) / 0.08, 0, 1))
fade = np.clip((0.99 - yy) / 0.20, 0, 1)
alpha = alpha * fade
# soften the mountain/sky edge
alpha_img = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.5))
rgba = np.dstack([arr, np.asarray(alpha_img).astype(np.float32)])
# light haze so the layer sits behind the 3D town, matching the sky quad tone
haze = np.array([190, 222, 240], dtype=np.float32)
hz = np.clip((0.66 - yy) / 0.66, 0, 1)[..., None] * 0.18   # more haze higher up
rgba[..., :3] = rgba[..., :3] * (1 - hz) + haze * hz
far_out = Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA")
# crop the fully transparent sky rows above the summit
al = np.asarray(far_out)[..., 3]
rows = np.where(al.max(axis=1) > 8)[0]
top = max(0, rows.min() - 8)
far_out = far_out.crop((0, top, W, H))
save(far_out, "Far_Town_NOON.png")


# ── 4. Character poses: normalise framing to match GirlSkater_Back (1024×1536) ─
MAG = np.array([255, 0, 255], dtype=np.int16)


def figure_mask(im):
    # Firefly's "magenta" drifts per image (#EA0499, #E6011E...); sample the
    # corner colour as the key and treat anything near it (and near pure magenta)
    # as background. Pink-ish hues with a big R/B vs G gap are all key.
    a = np.asarray(im).astype(np.int16)
    key = np.median(np.concatenate([a[:8, :8].reshape(-1, 3), a[-8:, -8:].reshape(-1, 3)]), axis=0)
    d_key = np.abs(a - key).sum(axis=2)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pinkish = (r > 170) & (g < 90) & (b > 90) & (r - g > 120)
    return ~((d_key < 90) | pinkish)


def compose_pose(name, out_name, target_h_frac=0.89, feet_from_bottom=40, canvas=(1024, 1536), fixed_scale=None):
    im = load(name)
    m = figure_mask(im)
    # remove flecks
    ys, xs = np.where(m)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    fig = im.crop((x0, y0, x1 + 1, y1 + 1))
    fm = Image.fromarray((m[y0:y1 + 1, x0:x1 + 1] * 255).astype(np.uint8))
    cw, ch = canvas
    scale = (ch * target_h_frac) / fig.size[1]
    if fig.size[0] * scale > cw * 0.96:
        scale = cw * 0.96 / fig.size[0]
    if fixed_scale is not None:
        scale = fixed_scale
    nw, nh = int(fig.size[0] * scale), int(fig.size[1] * scale)
    fig = fig.resize((nw, nh), Image.LANCZOS)
    fm = fm.resize((nw, nh), Image.LANCZOS)
    # clean key: replace anything outside the figure with pure magenta, and
    # de-fringe by pulling the mask in by ~1px so no pink halo survives.
    fm = fm.filter(ImageFilter.MinFilter(5))
    # Un-blend the key from the remaining edge texels: where a pixel still leans
    # pink (r,b well above g) pull it toward its non-pink neighbour colour.
    fa = np.asarray(fig).astype(np.float32)
    r, g, b = fa[..., 0], fa[..., 1], fa[..., 2]
    pink = ((r - g) > 70) & ((b - g) > 55)
    if pink.any():
        blurred = np.asarray(fig.filter(ImageFilter.MedianFilter(7))).astype(np.float32)
        fa[pink] = blurred[pink]
        fig = Image.fromarray(np.clip(fa, 0, 255).astype(np.uint8))
    bg = Image.new("RGB", canvas, (255, 0, 255))
    px = (cw - nw) // 2
    py = ch - feet_from_bottom - nh
    bg.paste(fig, (px, py), fm)
    save(bg, out_name)
    return scale


compose_pose("Girl_Run_key.png", "GirlSkater_Back.png")
compose_pose("Girl_Jump_key.png", "GirlSkater_Jump.png", feet_from_bottom=150)   # airborne: lift
compose_pose("Girl_Crouch_key.png", "GirlSkater_Crouch.png", target_h_frac=0.70)
compose_pose("Girl_Lean_key.png", "GirlSkater_Lean.png")
print("done")

# ── 5. Riding cycle: glide (feet on board) ↔ push (rear foot kicking) ────────
# Both frames share the run framing so the board sits at the same height.
if os.path.exists(os.path.join(HERE, "Girl_Glide_key.png")):
    glide_scale = compose_pose("Girl_Glide_key.png", "GirlSkater_Back.png")
    if os.path.exists(os.path.join(HERE, "Girl_Push_key.png")):
        # Same pixel scale as the glide frame so the girl does not grow when the
        # frames alternate; the crouched push pose is simply shorter.
        compose_pose("Girl_Push_key.png", "GirlSkater_Push.png", fixed_scale=glide_scale)
