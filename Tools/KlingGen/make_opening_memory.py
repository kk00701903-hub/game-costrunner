"""12차 — 오프닝 1:37 연장: 어린 시절 제주 추억 4컷(돌담길 / 구슬치기 / 딱지치기 / 첫사랑).
    python Tools/KlingGen/make_opening_memory.py [--only 7 8 9 10] [--skip-stills]
1) Resources/CoastRun/Cut_Open_4..7.png 이 없으면 Kling 이미지 생성(9:16)으로 먼저 뽑는다.
   Firefly 로 직접 그렸다면 Tools/FireflyArt/Open_4..7_raw.png 에 두고 process_title.py 를 돌리면 이 단계는 건너뛴다.
2) 그 스틸을 첫 프레임으로 image2video 10초 → Assets/StreamingAssets/Opening/open_7..10.mp4
OpeningCinematic.Shots 순서: open_1, 7, 8, 9, 2, 10, 3, 4, 6 (open_5 는 이제 쓰지 않는다)."""
from __future__ import annotations
import argparse, sys, json, time
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT  # noqa: E402
from kling_client import KlingClient  # noqa: E402
from make_opening import submit, wait_video  # noqa: E402

RES = ROOT / "Assets" / "Resources" / "CoastRun"
DEST = ROOT / "Assets" / "StreamingAssets" / "Opening"
OUT = ROOT / "Tools" / "KlingGen" / "out"

# 기존 Cut_Open_1~3 과 같은 결: 수채화 그림책, 열두 살 즈음의 두 아이(짧은 갈색 머리 소녀 / 마른 검은 머리 소년), 제주.
STILL_STYLE = ("storybook watercolor illustration, soft painterly edges, warm nostalgic summer light, Jeju island Korea, "
               "a girl about twelve with short brown hair and a thin boy with short black hair, same characters as before, "
               "no text, no letters, vertical composition, ")
STILL_NEG = "text, letters, watermark, logo, photorealistic, 3d render, extra fingers, distorted face, adults"
VID_STYLE = "soft watercolor anime animation, painterly, gentle natural movement, slow cinematic camera drift, no text, no subtitles, "

SHOTS = [
    (7, "Cut_Open_4.png",
     STILL_STYLE + "two children running barefoot down a narrow black basalt stone-wall lane (돌담길) toward the turquoise sea, "
                   "tangerine trees over the wall, white summer clouds, laughing, seen from behind at a low angle",
     VID_STYLE + "two children run down a black stone-wall lane toward the sea, hair and shirts fluttering, tangerine leaves swaying, "
                 "clouds drifting, camera follows gently from behind"),
    (8, "Cut_Open_5.png",
     STILL_STYLE + "close low-angle view of two children crouching on a sunlit dirt yard playing Korean marbles (구슬치기), "
                   "the boy flicking a glass marble with his thumb, colorful glass marbles catching sunlight, a small circle drawn in the dirt, "
                   "the girl leaning in to watch, an old Jeju house with a tiled roof behind",
     VID_STYLE + "the boy flicks a glass marble that rolls slowly across the dirt, marbles glinting, the girl leans closer holding her breath, "
                 "dust motes in warm sunlight, very slow push-in"),
    (9, "Cut_Open_6.png",
     STILL_STYLE + "a boy slamming a folded paper ddakji (딱지) down onto the ground in a sunny courtyard, paper tiles scattered, "
                   "the girl clapping and laughing beside him, a pile of colorful folded paper tiles, hydrangeas by the stone wall, late afternoon",
     VID_STYLE + "the boy throws the paper tile down, the tiles on the ground flip and flutter, the girl laughs and claps, "
                 "hydrangeas sway in the breeze, gentle handheld feel"),
    (10, "Cut_Open_7.png",
     STILL_STYLE + "dusk at a small seaside bus stop, the girl and the boy sitting a little apart on the bench, the boy holding out a single tangerine to her "
                   "without looking at her, her hand hesitating, the sea and a distant steel transmission tower behind them turning violet and orange, "
                   "faint first-love shyness, bittersweet",
     VID_STYLE + "dusk light slowly deepening, sea waves moving far away, the tower light blinking, her hair moves in the wind, "
                 "the tangerine in his outstretched hand, nobody moves much, quiet and wistful"),
]


def make_still(client: KlingClient, dest: Path, prompt: str, model: str) -> None:
    tid = client.generate_image(prompt, negative_prompt=STILL_NEG, model=model, aspect_ratio="9:16", n=1)
    print(f"    still task {tid}", flush=True)
    urls = client.wait(tid)
    raw = OUT / (dest.stem + "_raw.png")
    client.download(urls[0], raw)
    try:
        from PIL import Image
        im = Image.open(raw).convert("RGB").resize((720, 1280), Image.LANCZOS)
        im.save(dest, optimize=True)
    except ImportError:
        dest.write_bytes(raw.read_bytes())
    print(f"    still saved {dest.name}", flush=True)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", type=int, nargs="*")
    ap.add_argument("--model", default="kling-v1-6")
    ap.add_argument("--image-model", default="kling-v1-5")
    ap.add_argument("--mode", default="std", choices=["std", "pro"])
    ap.add_argument("--duration", default="10", choices=["5", "10"])
    ap.add_argument("--skip-stills", action="store_true", help="스틸이 없어도 만들지 않는다(그 컷은 건너뜀)")
    args = ap.parse_args()
    client = KlingClient()
    DEST.mkdir(parents=True, exist_ok=True); OUT.mkdir(parents=True, exist_ok=True)
    reg_path = OUT / "opening_memory_tasks.json"
    reg = json.loads(reg_path.read_text(encoding="utf-8")) if reg_path.is_file() else {}
    tasks = []
    for n, still, still_prompt, vid_prompt in SHOTS:
        if args.only and n not in args.only: continue
        img = RES / still
        out = DEST / f"open_{n}.mp4"
        if not img.is_file():
            if args.skip_stills: print(f"[{n}] missing {img.name}, skipped"); continue
            print(f"[{n}] making still {img.name}", flush=True)
            try: make_still(client, img, still_prompt, args.image_model)
            except Exception as e: print(f"[{n}] still FAILED {e}"); continue
        key = str(n)
        if key in reg:
            print(f"[{n}] reuse task {reg[key]}", flush=True); tasks.append((n, reg[key], out)); continue
        tid = None
        for attempt in range(6):   # 429 레이트리밋 — 20초 간격으로 재시도
            try:
                tid = submit(client, img, vid_prompt, args.model, args.mode, args.duration); break
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
    print("done — Unity 에서 Play 하면 OpeningCinematic 이 open_7~10 을 집어 쓴다")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
