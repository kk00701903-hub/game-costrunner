"""계절 오프닝 영상 — 컷씬 BG(수채화)를 Kling image2video로 5초 움직여 Resources/CoastRun/Video/VID_CHxx_Open.mp4 로.
    (봄 CH01·여름 CH06 은 Firefly 웹에서 만들었고, 가을 CH11·겨울 CH16 을 여기서.)
    python Tools/KlingGen/make_season.py            # 없는 것만
    python Tools/KlingGen/make_season.py --only 11
ChapterVN 은 Resources/CoastRun/Video/VID_<씬>.mp4 가 있으면 씬 앞에 한 번 재생한다(탭으로 건너뜀)."""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT  # noqa: E402
from kling_client import KlingClient, _extract_task_id, _extract_status  # noqa: E402
from make_opening import submit, wait_video  # noqa: E402

BG = ROOT / "Assets" / "Resources" / "CoastRun"
DEST = ROOT / "Assets" / "Resources" / "CoastRun" / "Video"

SHOTS = [
    (11, "BG_OrangeFarm.png", "VID_CH11_Open.mp4",
     "soft watercolor anime animation, jeju tangerine farm in autumn, tangerine trees heavy with orange fruit, "
     "leaves and branches sway gently in a warm breeze, a few dry leaves drift slowly across the frame, warm amber late-afternoon light, "
     "slow gentle camera drift to the right, painterly, calm melancholic mood, no text"),
    (16, "BG_TowerSnow.png", "VID_CH16_Open.mp4",
     "soft watercolor anime animation, a tall steel power transmission tower on a snowy hill above the winter sea, "
     "snowflakes falling slowly and steadily, low grey-blue sky, distant waves moving gently, cold quiet atmosphere, "
     "very slow camera push-in toward the tower, painterly, no text"),
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", type=int, nargs="*")
    ap.add_argument("--model", default="kling-v1-6")
    ap.add_argument("--mode", default="std", choices=["std", "pro"])
    ap.add_argument("--duration", default="5", choices=["5", "10"])
    args = ap.parse_args()

    client = KlingClient()
    DEST.mkdir(parents=True, exist_ok=True)
    shots = [s for s in SHOTS if not args.only or s[0] in args.only]
    tasks = []
    for n, still, name, prompt in shots:
        img = BG / still
        if not img.is_file():
            print(f"[{n}] missing still {img}"); continue
        out = DEST / name
        if out.is_file() and out.stat().st_size > 1000:
            print(f"[{n}] exists, skip {out.name}"); continue
        tid = submit(client, img, prompt, args.model, args.mode, args.duration)
        print(f"[{n}] submitted task {tid}", flush=True)
        tasks.append((n, tid, out))
    for n, tid, out in tasks:
        url = wait_video(client, tid)
        client.download(url, out)
        print(f"[{n}] saved {out} ({out.stat().st_size // 1024} KB)", flush=True)
    print("done")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
