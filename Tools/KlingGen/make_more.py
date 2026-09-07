"""8차 — 챕터 사이 영상 3 + 엔딩 영상 3 (컷씬 CG를 첫 프레임으로 Kling image2video 5초).
    python Tools/KlingGen/make_more.py [--only CH04_Open END_A ...]"""
from __future__ import annotations
import argparse, sys, json, time
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT  # noqa: E402
from kling_client import KlingClient  # noqa: E402
from make_opening import submit, wait_video  # noqa: E402

RES = ROOT / "Assets" / "Resources" / "CoastRun"
DEST = RES / "Video"
STYLE = "soft watercolor anime animation, painterly, subtle natural movement, gentle camera drift, no text, "
SHOTS = [
    ("CH04_Open", "Cut_CH03_Close.png", STYLE + "an old radio on a desk at night, the girl's hand slowly turning the dial, warm lamp light flickering softly, curtains breathing in a breeze"),
    ("CH09_Open", "Cut_CH08_Close.png", STYLE + "grey typhoon sky, rain streaks falling, trees and grass bending in strong wind, the figures stay still, tense quiet mood"),
    ("CH14_Open", "Cut_CH13_Close.png", STYLE + "autumn evening, dry leaves drifting slowly across the frame, hair and clothes moving gently in the wind, warm fading light"),
    ("END_A", "Cut_END_A1.png", STYLE + "sunset over the sea behind a steel transmission tower, the two figures stand close, scarves and hair moving softly, warm light slowly brightening, hopeful"),
    ("END_B", "Cut_END_B1.png", STYLE + "winter dusk, snow falling steadily, the lone figure very still, a distant light on the tower blinking slowly, quiet and lonely"),
    ("END_TRUE", "Cut_END_A3.png", STYLE + "golden spring morning, canola flowers swaying, soft sunlight rays, two figures walking together, gentle camera push-in, peaceful"),
]

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", nargs="*")
    ap.add_argument("--model", default="kling-v1-6")
    ap.add_argument("--mode", default="std", choices=["std", "pro"])
    args = ap.parse_args()
    client = KlingClient()
    DEST.mkdir(parents=True, exist_ok=True)
    reg_path = ROOT / "Tools" / "KlingGen" / "out" / "more_tasks.json"
    reg = json.loads(reg_path.read_text(encoding="utf-8")) if reg_path.is_file() else {}
    tasks = []
    for sid, still, prompt in SHOTS:
        if args.only and sid not in args.only: continue
        img = RES / still
        out = DEST / f"VID_{sid}.mp4"
        if not img.is_file(): print(f"[{sid}] missing {img}"); continue
        if out.is_file() and out.stat().st_size > 1000: print(f"[{sid}] exists"); continue
        if sid in reg:
            print(f"[{sid}] reuse task {reg[sid]}", flush=True); tasks.append((sid, reg[sid], out)); continue
        tid = None
        for attempt in range(6):   # 429 레이트리밋 — 20초 간격으로 재시도
            try:
                tid = submit(client, img, prompt, args.model, args.mode, "5"); break
            except Exception as e:
                print(f"[{sid}] submit failed ({e}); retry in 20s", flush=True); time.sleep(20)
        if not tid: print(f"[{sid}] gave up"); continue
        reg[sid] = tid; reg_path.write_text(json.dumps(reg, indent=1), encoding="utf-8")
        print(f"[{sid}] submitted {tid}", flush=True)
        tasks.append((sid, tid, out))
    for sid, tid, out in tasks:
        try:
            url = wait_video(client, tid)
            client.download(url, out)
            print(f"[{sid}] saved {out.name} ({out.stat().st_size // 1024} KB)", flush=True)
        except Exception as e:
            print(f"[{sid}] FAILED {e}", flush=True)
    print("done")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
