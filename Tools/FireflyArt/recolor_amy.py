"""Recolour the Mixamo 'Amy' diffuse atlas into the game's protagonist look:
short brown bob, yellow t-shirt, denim shorts, white sneakers. Shading is kept
(value channel), only hue/saturation move per region. The untouched atlas is kept
here rather than beside the texture: everything under Resources/ is force-included
in the player build, and a 9 MB backup nobody samples would ship with it.
Run: python Tools/FireflyArt/recolor_amy.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Resources", "CoastRun", "Rig", "Textures", "Ch46_1001_Diffuse.png")
BAK = os.path.join(os.path.dirname(__file__), "Amy_Diffuse_orig.png")

if not os.path.exists(BAK):
    Image.open(TEX).save(BAK)
src = Image.open(BAK).convert("RGB")
hsv = np.asarray(src.convert("HSV")).astype(np.float32)
h, s, v = hsv[..., 0] * (360.0 / 255.0), hsv[..., 1] / 255.0, hsv[..., 2] / 255.0
out = hsv.copy()


def paint(mask, hue, sat, vmul=1.0, vadd=0.0, feather=None):
    m = mask.astype(np.float32)
    out[..., 0] = np.where(m > 0, hue * (255.0 / 360.0), out[..., 0])
    out[..., 1] = np.where(m > 0, sat * 255.0, out[..., 1])
    out[..., 2] = np.where(m > 0, np.clip((v * vmul + vadd) * 255.0, 0, 255), out[..., 2])


# Hair: cyan/teal → warm chestnut brown
hair = (h > 150) & (h < 205) & (s > 0.22)
paint(hair, 24, 0.62, vmul=0.62, vadd=0.02)

# Top: pale lilac (low sat, high value, hue 270–330) → sunny yellow
top = (h > 265) & (h < 335) & (s < 0.48) & (v > 0.55)
paint(top, 48, 0.80, vmul=1.0)

# Shorts: saturated magenta → soft pastel denim (cuter than workwear blue)
shorts = (h > 290) & (h < 340) & (s >= 0.48)
paint(shorts, 212, 0.42, vmul=0.92, vadd=0.04)

# Skin: the original is a dusty rose; lift it to a clean peach like the key art.
skin = ((h > 340) | (h < 25)) & (s > 0.12) & (s < 0.5) & (v > 0.6)
paint(skin, 16, 0.22, vmul=1.0, vadd=0.05)

# Boots: saturated orange/tan → white sneaker (keep stitch shading)
boots = (h > 18) & (h < 45) & (s > 0.50) & (v > 0.35)
paint(boots, 40, 0.06, vmul=1.0, vadd=0.18)

res = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "HSV").convert("RGB")
res.save(TEX)
print("recoloured", TEX, "hair", hair.mean().round(3), "top", top.mean().round(3),
      "shorts", shorts.mean().round(3), "boots", boots.mean().round(3))
res.resize((1024, 1024)).save(os.path.join(ROOT, "Tools", "FireflyArt", "_amy_preview.png"))
