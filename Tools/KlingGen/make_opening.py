"""15초 시네마틱 오프닝 — Firefly 스틸 3장을 Kling image2video로 5초씩 움직여 StreamingAssets/Opening/open_N.mp4 로.
    python Tools/KlingGen/make_opening.py            # 3컷 전부
    python Tools/KlingGen/make_opening.py --only 2   # 한 컷만
    python Tools/KlingGen/make_opening.py --model kling-v1-6 --mode std
Unity OpeningCinematic은 mp4가 있으면 VideoPlayer, 없으면 켄번즈 스틸로 재생한다."""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from config import ROOT, OUT_DIR  # noqa: E402
from kling_client import KlingClient, _extract_task_id, _extract_status  # noqa: E402

STILLS = ROOT / "Tools" / "FireflyArt"
DEST = ROOT / "Assets" / "StreamingAssets" / "Opening"

SHOTS = [
    (1, "Open_1_raw.png",
     "storybook watercolor animation, two korean children under a red and white power transmission tower on a spring hill, "
     "the boy gently hands a rusty tin can to the shy girl holding a skateboard, canola flowers sway in a soft breeze, "
     "warm morning light, slow gentle camera push-in, subtle hair and cloth movement, no text"),
    (2, "Open_2_raw.png",
     "storybook watercolor animation, a brave thin boy steps forward and spreads his arm to shield a scared girl from three older bullies "
     "in a narrow stone-wall alley, the bullies hesitate, dramatic afternoon light, slow camera drift, subtle movement, no text"),
    (3, "Open_3_raw.png",
     "storybook watercolor animation, view from behind, two children run hand in hand up a grassy hill toward a tall power transmission tower "
     "at golden sunset, clouds drift slowly, long shadows, grass sways, slow rising camera, hopeful, no text"),
]


def submit(client: KlingClient, image: Path, prompt: str, model: str, mode: str, duration: str) -> str:
    body = {
        "model_name": model,
        "image": client._encode_image(str(image)),
        "prompt": prompt,
        "negative_prompt": "text, letters, watermark, logo, distorted face, extra limbs, flicker, photorealistic",
        "cfg_scale": 0.5,
        "mode": mode,
        "duration": duration,
    }
    data = client._request("POST", "/v1/videos/image2video", json_body=body)
    tid = _extract_task_id(data)
    if not tid:
        raise RuntimeError(f"no task id: {data}")
    return tid


def wait_video(client: KlingClient, tid: str, timeout: int = 1200) -> str:
    deadline = time.time() + timeout
    while time.time() < deadline:
        data = client._request("GET", f"/v1/videos/image2video/{tid}")
        st = _extract_status(data)
        if st in ("succeed", "success", "completed"):
            d = data.get("data") if isinstance(data.get("data"), dict) else data
            vids = ((d.get("task_result") or {}).get("videos")) or []
            if vids and isinstance(vids[0], dict) and vids[0].get("url"):
                return str(vids[0]["url"])
            raise RuntimeError(f"no video url: {data}")
        if st in ("failed", "error"):
            raise RuntimeError(f"task failed: {data}")
        print(f"    {st or 'pending'} ...", flush=True)
        time.sleep(10)
    raise TimeoutError(tid)


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
    for n, still, prompt in shots:
        img = STILLS / still
        if not img.is_file():
            print(f"[{n}] missing still {img}"); continue
        out = DEST / f"open_{n}.mp4"
        if out.is_file():
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
