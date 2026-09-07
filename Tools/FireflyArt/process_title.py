"""Title gate + opening stills: Firefly 1080x1920 → Resources/CoastRun/UI_Title_Gate.png, Cut_Open_1..3.png (720x1280)."""
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Resources", "CoastRun"))
JOBS = {"Title_Gate_raw.png": ("UI_Title_Gate.png", (1080, 1920)),
        "Open_1_raw.png": ("Cut_Open_1.png", (720, 1280)),
        "Open_2_raw.png": ("Cut_Open_2.png", (720, 1280)),
        "Open_3_raw.png": ("Cut_Open_3.png", (720, 1280)),
        # 12차 오프닝 추억 4컷 (돌담길 / 구슬치기 / 딱지치기 / 첫사랑) — Firefly 로 그렸을 때
        "Open_4_raw.png": ("Cut_Open_4.png", (720, 1280)),
        "Open_5_raw.png": ("Cut_Open_5.png", (720, 1280)),
        "Open_6_raw.png": ("Cut_Open_6.png", (720, 1280)),
        "Open_7_raw.png": ("Cut_Open_7.png", (720, 1280))}
for src, (dst, size) in JOBS.items():
    p = os.path.join(HERE, src)
    if not os.path.exists(p): print("missing", src); continue
    im = Image.open(p).convert("RGB").resize(size, Image.LANCZOS)
    im.save(os.path.join(OUT, dst), optimize=True); print("wrote", dst, im.size)
