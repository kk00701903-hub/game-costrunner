"""VN cutscene art: BG_*_raw.png / Cut_*_raw.png (Firefly 1536²) → 4:3 center crop → 1024x768 → Assets/Resources/CoastRun/<name>.png"""
import os, glob, shutil
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Resources", "CoastRun"))
W, H = 1024, 768
BIAS = {"Cut_CH06_Close": 0.4, "Cut_END_A1": 0.45}
n = 0
for p in sorted(glob.glob(os.path.join(HERE, "BG_*_raw.png")) + glob.glob(os.path.join(HERE, "Cut_*_raw.png"))):
    name = os.path.basename(p)[:-len("_raw.png")]
    if name.startswith("Cut_Open_"):
        continue  # portrait stills handled by process_title.py
    im = Image.open(p).convert("RGB"); w, h = im.size
    ch = int(w * H / W); y0 = int((h - ch) * BIAS.get(name, 0.5))
    im = im.crop((0, y0, w, y0 + ch)).resize((W, H), Image.LANCZOS)
    im.save(os.path.join(OUT, name + ".png"), optimize=True); n += 1
# reuse schedule art for three backgrounds until dedicated ones exist
for src, dst in [("Sched_dev_radio", "BG_HanulRoom"), ("Sched_job_orange", "BG_OrangeFarm"), ("Sched_dev_oreum", "BG_Oreum")]:
    d = os.path.join(OUT, dst + ".png")
    if not os.path.exists(d):
        shutil.copy(os.path.join(OUT, src + ".png"), d); n += 1
print("wrote", n)
