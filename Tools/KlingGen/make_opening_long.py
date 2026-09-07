"""11차 — 오프닝 영상 연장: 6컷 × 10초 (Kling image2video, 기존 3컷 스틸 + 컷씬 CG 3장).
    python Tools/KlingGen/make_opening_long.py [--only 4 5 6]
결과: Assets/StreamingAssets/Opening/open_N.mp4 (N=1..6). 기존 open_1~3(5초)은 open_1~3 을 10초로 다시 뽑아 덮어쓴다."""
from __future__ import annotations
import argparse, sys, json, time
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT  # noqa: E402
from kling_client import KlingClient  # noqa: E402
from make_opening import submit, wait_video  # noqa: E402

RES = ROOT / "Assets" / "Resources" / "CoastRun"
DEST = ROOT / "Assets" / "StreamingAssets" / "Opening"
STYLE = "soft watercolor anime animation, painterly, gentle natural movement, slow cinematic camera drift, no text, no subtitles, "
SHOTS = [
    (1, "Cut_Open_1.png", STYLE + "spring, two children under a steel transmission tower, canola flowers swaying, sunlight rays, camera slowly pushes in"),
    (2, "Cut_Open_2.png", STYLE + "a boy standing in front of a girl protectively, wind moving hair and clothes, leaves drifting, quiet afternoon light"),
    (3, "Cut_Open_3.png", STYLE + "six years later, a young man returning, bus stop by the sea, sea waves moving in the distance, warm golden hour light slowly brightening"),
    (4, "Cut_CH01_Close.png", STYLE + "sunset behind the transmission tower, orange sky glowing, silhouettes of two figures, wind in the grass, slow camera pull back"),
    (5, "Cut_CH06_Close.png", STYLE + "summer night festival, lanterns glowing and swaying, fireworks blooming slowly in the sky, the two figures looking up"),
    (6, "Cut_END_A3.png", STYLE + "golden spring morning, canola flowers swaying, soft sunlight rays, two figures walking together toward the tower, peaceful, slow push-in"),
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", type=int, nargs="*")
    ap.add_argument("--model", default="kling-v1-6")
    ap.add_argument("--mode", default="std", choices=["std", "pro"])
    ap.add_argument("--duration", default="10", choices=["5", "10"])
    args = ap.parse_args()
    client = KlingClient()
    DEST.mkdir(parents=True, exist_ok=True)
    reg_path = ROOT / "Tools" / "KlingGen" / "out" / "opening_long_tasks.json"
    reg = json.loads(reg_path.read_text(encoding="utf-8")) if reg_path.is_file() else {}
    tasks = []
    for n, still, prompt in SHOTS:
        if args.only and n not in args.only: continue
        img = RES / still
        out = DEST / f"open_{n}.mp4"
        key = str(n)
        if not img.is_file(): print(f"[{n}] missing {img}"); continue
        if key in reg:
            print(f"[{n}] reuse task {reg[key]}", flush=True); tasks.append((n, reg[key], out)); continue
        tid = None
        for attempt in range(6):
            try:
                tid = submit(client, img, prompt, args.model, args.mode, args.duration); break
            except Exception as e:
                print(f"[{n}] submit failed ({e}); retry in 20s", flush=True); time.sleep(20)
        if not tid: print(f"[{n}] gave up"); continue
        reg[key] = tid; reg_path.write_text(json.dumps(reg, indent=1), encoding="utf-8")
        print(f"[{n}] submitted {tid}", flush=True)
        tasks.append((n, tid, out))
    for n, tid, out in tasks:
        try:
            url = wait_video(client, tid)
            tmp = out.with_suffix(".new.mp4")
            client.download(url, tmp)
            if out.is_file(): out.unlink()
            tmp.rename(out)
            print(f"[{n}] saved {out.name} ({out.stat().st_size // 1024} KB)", flush=True)
        except Exception as e:
            print(f"[{n}] FAILED {e}", flush=True)
    print("done")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
