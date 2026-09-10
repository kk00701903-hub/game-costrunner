"""40차 — 펫 상점/폴백 스프라이트: 평면 수채화 → 통통한 3D 치비(마젠타 키).
   python Tools/KlingGen/make_pet_sprites.py
"""
from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT  # noqa: E402
from kling_client import KlingClient  # noqa: E402

RES = ROOT / "Assets" / "Resources" / "CoastRun"
OUT = ROOT / "Tools" / "KlingGen" / "out" / "pets"
NEG = (
    "flat 2d sticker, paper cutout, thin silhouette, text, watermark, logo, "
    "realistic photo, horror, gore, low detail, blurry, stretched"
)

JOBS = [
    (
        "Obs_Pet_Sparrow",
        "1:1",
        "cute chibi 3D sparrow pet mascot, chubby round body, thick fluffy wings mid-flap, "
        "big glossy anime eyes with highlight, short orange beak, warm brown and cream feathers, "
        "soft toon shading with clear volume and rim light, stylized game character, "
        "centered, solid pure magenta #FF00FF background, no ground, no text",
    ),
    (
        "Obs_Pet_WildGoose",
        "1:1",
        "cute chibi 3D wild goose pet mascot, plump grey body, long neck, thick wings, "
        "big glossy eyes, orange beak and feet, soft toon shading with strong volume, "
        "stylized mobile game character, centered, solid pure magenta #FF00FF background, no text",
    ),
    (
        "Obs_Pet_BlackPig",
        "1:1",
        "cute chibi 3D Jeju black pig pet mascot, chubby round black body, pink snout and ears, "
        "tiny legs, big glossy eyes, soft toon shading with clear volume and rim light, "
        "stylized mobile game character, centered, solid pure magenta #FF00FF background, no text",
    ),
    (
        "Obs_Pet_BikerThug",
        "1:1",
        "cute chibi 3D biker thug on red scooter pet mascot, small tough guy helmet and shades, "
        "chunky scooter with thick wheels, soft toon shading with clear volume, "
        "stylized mobile game character, centered, solid pure magenta #FF00FF background, no text",
    ),
]


def main() -> int:
    client = KlingClient()
    OUT.mkdir(parents=True, exist_ok=True)
    RES.mkdir(parents=True, exist_ok=True)
    for name, ratio, prompt in JOBS:
        dest = RES / f"{name}.png"
        raw = OUT / f"{name}_raw.png"
        print(f"[{name}] submit…", flush=True)
        tid = None
        for attempt in range(6):
            try:
                tid = client.generate_image(
                    prompt=prompt,
                    negative_prompt=NEG,
                    model="kling-v1-5",
                    aspect_ratio=ratio,
                    n=1,
                )
                break
            except Exception as e:
                print(f"[{name}] submit fail ({e}); retry 20s", flush=True)
                time.sleep(20)
        if not tid:
            print(f"[{name}] gave up")
            continue
        print(f"[{name}] task {tid}", flush=True)
        try:
            urls = client.wait(tid, timeout=480, interval=6)
            if not urls:
                print(f"[{name}] no urls")
                continue
            client.download(urls[0], raw)
            # Copy into Resources (Unity will reimport). Keep previous as backup once.
            bak = RES / f"{name}_prev.png"
            if dest.is_file() and not bak.is_file():
                bak.write_bytes(dest.read_bytes())
            dest.write_bytes(raw.read_bytes())
            print(f"[{name}] saved {dest} ({dest.stat().st_size // 1024} KB)", flush=True)
        except Exception as e:
            print(f"[{name}] FAILED {e}", flush=True)
    print("done")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
