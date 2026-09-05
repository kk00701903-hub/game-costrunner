"""Standing sprites: Firefly magenta-key 1080x1920 → RGBA cutout Resources/CoastRun/Stand_<Name>_<Mood>.png (height 1024)."""
import os, glob, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from process_raising import cutout, OUT
HERE = os.path.dirname(os.path.abspath(__file__))
for p in sorted(glob.glob(os.path.join(HERE, "Stand_*_key.png"))):
    name = os.path.basename(p)[:-len("_key.png")]
    cutout(p, os.path.join(OUT, name + ".png"), size=1024)
