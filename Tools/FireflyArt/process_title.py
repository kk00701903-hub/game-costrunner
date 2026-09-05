"""Title gate + opening stills: Firefly 1080x1920 → Resources/CoastRun/UI_Title_Gate.png, Cut_Open_1..3.png (720x1280)."""
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Resources", "CoastRun"))
JOBS = {"Title_Gate_raw.png": ("UI_Title_Gate.png", (1080, 1920)),
        "Open_1_raw.png": ("Cut_Open_1.png", (720, 1280)),
        "Open_2_raw.png": ("Cut_Open_2.png", (720, 1280)),
        "Open_3_raw.png": ("Cut_Open_3.png", (720, 1280))}
for src, (dst, size) in JOBS.items():
    p = os.path.join(HERE, src)
    if not os.path.exists(p): print("missing", src); continue
    im = Image.open(p).convert("RGB").resize(size, Image.LANCZOS)
    im.save(os.path.join(OUT, dst), optimize=True); print("wrote", dst, im.size)
