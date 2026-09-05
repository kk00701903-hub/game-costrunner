"""Schedule illustrations (Princess-Maker style 정산 로그 그림):
Firefly 1536x1536 → 4:3 center crop → 1024x768 → Assets/Resources/CoastRun/Sched_<id>.png
Optional per-id vertical bias (0 = top, 0.5 = center, 1 = bottom)."""
import os, glob
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "CoastRun")

BIAS = {
    "dev_radio": 0.6,
    "story": 0.35,
    "rest_home": 0.55,
}

W, H = 1024, 768

def process(path):
    name = os.path.basename(path)[len("Sched_"):-len("_raw.png")]
    im = Image.open(path).convert("RGB")
    w, h = im.size
    ch = int(w * H / W)
    bias = BIAS.get(name, 0.5)
    y0 = int((h - ch) * bias)
    im = im.crop((0, y0, w, y0 + ch)).resize((W, H), Image.LANCZOS)
    out = os.path.join(OUT, f"Sched_{name}.png")
    im.save(out, optimize=True)
    print("wrote", out, im.size)
    return im

if __name__ == "__main__":
    tiles = []
    for p in sorted(glob.glob(os.path.join(HERE, "Sched_*_raw.png"))):
        tiles.append(process(p))
    # contact sheet for capture check
    cols = 4
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 256, rows * 192), (40, 40, 40))
    for i, t in enumerate(tiles):
        sheet.paste(t.resize((256, 192)), ((i % cols) * 256, (i // cols) * 192))
    sheet.save(os.path.join(HERE, "_sched_sheet.png"))
    print("sheet", sheet.size)
