#!/usr/bin/env python3
"""
『우리의 송전탑』 BGM 배치 생성기 — ACE-Step 1.5 REST API 클라이언트.

    python generate.py                       # tracks.json 전부 (P0 → P1 → P2 순)
    python generate.py --only BGM_Menu       # 한 곡만
    python generate.py --group ch --stems    # 챕터 주행곡 + Demucs로 a/b/c 스템 분리
    python generate.py --priority P0         # 없으면 플레이 불가한 것만
    python generate.py --takes 4             # 후보 4개 뽑고 첫 번째를 채택 (takes/ 폴더에 전부 보관)

필요: ACE-Step API 서버가 떠 있을 것 (start_api_server.bat / _rocm.bat → http://127.0.0.1:8001).
결과: ../../Assets/Resources/CoastRun/BGM/<name>.wav  → Unity가 다음 Play에 자동 로드.
스템: --stems 는 `pip install demucs` 필요 (ACE-Step venv 안에서). htdemucs_6s 모델 사용.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "CoastRun", "BGM"))
TAKES_DIR = os.path.join(HERE, "takes")


# ── HTTP helpers ─────────────────────────────────────────────────────────────

def post(api, path, payload):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(api + path, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read().decode("utf-8"))


def get_bytes(api, path):
    with urllib.request.urlopen(api + path, timeout=600) as r:
        return r.read()


def health(api):
    try:
        with urllib.request.urlopen(api + "/health", timeout=5) as r:
            return r.status == 200
    except Exception:
        return False


# ── Generation ───────────────────────────────────────────────────────────────

def submit(api, track, common_tail, takes, thinking):
    payload = {
        "prompt": f"{track['prompt']}, {common_tail}",
        "lyrics": "[inst]",
        "bpm": track["bpm"],
        "key_scale": track["key"],
        "time_signature": "4",
        "audio_duration": track["duration"],
        "audio_format": "wav",
        "inference_steps": 8,
        "use_random_seed": False,
        "seed": track["seed"],
        "batch_size": takes,
        "thinking": track.get("thinking", thinking),
        "use_cot_caption": True,
        "use_cot_language": False,
        "vocal_language": "en",
    }
    res = post(api, "/release_task", payload)
    if res.get("code") != 200:
        raise RuntimeError(f"release_task failed: {res}")
    return res["data"]["task_id"]


def wait(api, task_id, poll=3.0):
    dots = 0
    while True:
        res = post(api, "/query_result", {"task_id_list": [task_id]})
        items = res.get("data") or []
        if items:
            st = items[0].get("status", 0)
            if st == 1:
                raw = items[0].get("result", "[]")
                return json.loads(raw) if isinstance(raw, str) else raw
            if st == 2:
                raise RuntimeError(f"task {task_id} failed: {items[0]}")
        dots = (dots + 1) % 4
        sys.stdout.write("\r    generating" + "." * dots + "   ")
        sys.stdout.flush()
        time.sleep(poll)


def download_takes(api, results, name):
    os.makedirs(TAKES_DIR, exist_ok=True)
    paths = []
    for i, item in enumerate(results):
        url = item.get("file")
        if not url:
            continue
        data = get_bytes(api, url)
        p = os.path.join(TAKES_DIR, f"{name}_take{i + 1}.wav")
        with open(p, "wb") as f:
            f.write(data)
        paths.append(p)
        print(f"\r    take {i + 1}: {os.path.basename(p)}  ({len(data) // 1024} KB)  seed={item.get('seed_value', '?')}")
    return paths


# ── Post-processing (numpy + soundfile ship with the ACE-Step env) ──────────

def loop_crossfade(path, seconds=2.0):
    """Makes a seamless loop: the tail is cross-faded onto the head and trimmed off."""
    try:
        import numpy as np
        import soundfile as sf
    except ImportError:
        print("    (loop fix skipped: numpy/soundfile missing)")
        return
    x, sr = sf.read(path, always_2d=True)
    n = int(sr * seconds)
    if len(x) < n * 4:
        return
    head, tail, body = x[:n], x[-n:], x[n:-n]
    t = np.linspace(0.0, 1.0, n)[:, None]
    fade_in, fade_out = np.sqrt(t), np.sqrt(1.0 - t)          # equal-power
    mixed = head * fade_in + tail * fade_out
    out = np.concatenate([mixed, body], axis=0)
    sf.write(path, out, sr)


def trim_tail_fade(path, window=1.0, ratio=0.6, search=12.0):
    """The model likes to write an ending even when asked for a loop. Cut back to the
    last window whose loudness is still `ratio` of the body's, so the cross-fade joins
    full-energy audio to full-energy audio instead of a fade-out to the intro."""
    try:
        import numpy as np
        import soundfile as sf
    except ImportError:
        return
    x, sr = sf.read(path, always_2d=True)
    w = int(sr * window)
    if len(x) < w * 8:
        return
    rms = np.array([np.sqrt((x[i:i + w] ** 2).mean()) for i in range(0, len(x) - w, w)])
    body = np.median(rms[len(rms) // 4: 3 * len(rms) // 4])
    n_search = int(search / window)
    cut = len(rms)
    for k in range(len(rms) - 1, max(len(rms) - n_search, 0) - 1, -1):
        if rms[k] >= body * ratio:
            cut = k + 1
            break
    end = min(len(x), cut * w)
    if end < len(x) - w // 2:
        sf.write(path, x[:end], sr)
        print(f"    tail fade trimmed: {len(x) / sr:.1f}s → {end / sr:.1f}s")


def trim_silence(path, threshold_db=-50.0):
    try:
        import numpy as np
        import soundfile as sf
    except ImportError:
        return
    x, sr = sf.read(path, always_2d=True)
    amp = np.abs(x).max(axis=1)
    thr = 10 ** (threshold_db / 20)
    idx = np.where(amp > thr)[0]
    if len(idx) == 0:
        return
    x = x[idx[0]: idx[-1] + 1]
    sf.write(path, x, sr)


def split_stems(full_wav, name, groups, out_dir, python):
    """Demucs 6-source split, then sum sources into the a/b/c(/d) buses tracks.json asks for."""
    tmp = os.path.join(HERE, "demucs_out")
    # --segment caps the chunk Demucs holds in RAM: on a 16 GB machine that also hosts
    # the ACE-Step server, the default (whole track) swapped and hung at ~50 %.
    cmd = [python, "-m", "demucs", "-n", "htdemucs_6s", "-d", "cpu", "--segment", "7", "-j", "1",   # htdemucs_6s max is 7.8
           "-o", tmp, "--filename", "{track}/{stem}.{ext}", full_wav]
    print("    demucs:", " ".join(os.path.basename(c) if os.sep in c else c for c in cmd))
    subprocess.run(cmd, check=True)
    base = os.path.splitext(os.path.basename(full_wav))[0]
    src_dir = os.path.join(tmp, "htdemucs_6s", base)

    import numpy as np
    import soundfile as sf
    cache = {}

    def load(stem):
        if stem not in cache:
            p = os.path.join(src_dir, stem + ".wav")
            cache[stem] = sf.read(p, always_2d=True) if os.path.exists(p) else None
        return cache[stem]

    length, sr = None, None
    for bus, sources in groups.items():
        acc = None
        for s in sources:
            d = load(s)
            if d is None:
                continue
            x, sr = d
            acc = x if acc is None else acc[: len(x)] + x[: len(acc)]
        if acc is None:
            print(f"    !! bus {bus}: no sources found, skipping")
            continue
        length = len(acc) if length is None else min(length, len(acc))
        peak = np.abs(acc).max()
        if peak > 0.98:
            acc = acc * (0.98 / peak)
        out = os.path.join(out_dir, f"{name}_{bus}.wav")
        sf.write(out, acc, sr)
        print(f"    stem {bus} = {'+'.join(sources)} → {os.path.basename(out)}")

    # Unity starts every stem on the same DSP tick — lengths must match to the sample.
    if length is not None:
        for bus in groups:
            p = os.path.join(out_dir, f"{name}_{bus}.wav")
            if os.path.exists(p):
                x, sr = sf.read(p, always_2d=True)
                if len(x) != length:
                    sf.write(p, x[:length], sr)


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--api", default="http://127.0.0.1:8001")
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--only", nargs="*", help="track names")
    ap.add_argument("--group", choices=["menu", "ch", "cine", "memory", "end"])
    ap.add_argument("--priority", choices=["P0", "P1", "P2"], help="this priority and above")
    ap.add_argument("--takes", type=int, default=2, help="candidates per track (1-8)")
    ap.add_argument("--pick", type=int, default=1, help="which take becomes the final file")
    ap.add_argument("--thinking", action="store_true", help="use the 5Hz LM planner (better, slower; needs ≥6GB VRAM)")
    ap.add_argument("--stems", action="store_true", help="split chapter tracks into a/b/c with Demucs")
    ap.add_argument("--no-loopfix", action="store_true")
    ap.add_argument("--python", default=sys.executable, help="python with demucs installed")
    ap.add_argument("--redo", action="store_true", help="regenerate even if the output already exists")
    ap.add_argument("--resplit", action="store_true",
                    help="rebuild stems from the saved take with the current tracks.json bus mapping (no render)")
    args = ap.parse_args()

    with open(os.path.join(HERE, "tracks.json"), encoding="utf-8") as f:
        spec = json.load(f)

    order = {"P0": 0, "P1": 1, "P2": 2}
    tracks = spec["tracks"]
    if args.only:
        tracks = [t for t in tracks if t["name"] in set(args.only)]
    if args.group:
        tracks = [t for t in tracks if t["group"] == args.group]
    if args.priority:
        tracks = [t for t in tracks if order[t["priority"]] <= order[args.priority]]
    tracks.sort(key=lambda t: order[t["priority"]])

    if not tracks:
        print("nothing matched"); return
    if not health(args.api):
        print(f"ACE-Step API not reachable at {args.api}\n"
              f"  → start_api_server.bat (NVIDIA) / start_api_server_rocm.bat (AMD) 먼저 실행")
        return

    os.makedirs(args.out, exist_ok=True)
    print(f"{len(tracks)} track(s) → {args.out}\n")
    t0 = time.time()
    for i, tr in enumerate(tracks, 1):
        name = tr["name"]
        print(f"[{i}/{len(tracks)}] {name}  {tr['bpm']}bpm {tr['key']} {tr['duration']}s  ({tr['priority']})")
        final = os.path.join(args.out, f"{name}.wav")
        want_stems = args.stems and tr.get("stems")

        # Resumable: a crash mid-batch (Demucs swap-hang, reboot) must not cost the
        # 10-minute CPU renders that already finished.
        take1 = os.path.join(TAKES_DIR, f"{name}_take{args.pick}.wav")
        if args.resplit:
            if not want_stems:
                continue
            if not os.path.exists(final) and os.path.exists(take1):
                shutil.copyfile(take1, final)
                trim_silence(final)
                if tr.get("loop") and not args.no_loopfix:
                    trim_tail_fade(final)
                    loop_crossfade(final)
            if not os.path.exists(final):
                print("    !! no full mix and no take to rebuild from\n"); continue
        elif not args.redo:
            stems_done = want_stems and all(
                os.path.exists(os.path.join(args.out, f"{name}_{b}.wav")) for b in tr["stems"])
            if stems_done or (not want_stems and os.path.exists(final)):
                print("    already done, skipping (use --redo to regenerate)\n"); continue

        if want_stems and os.path.exists(final):
            print("    full mix already rendered; splitting stems only")
        else:
            takes = max(1, min(8, tr.get("takes", args.takes)))
            try:
                task = submit(args.api, tr, spec["common_tail"], takes, args.thinking)
                results = wait(args.api, task)
                paths = download_takes(args.api, results, name)
            except (RuntimeError, urllib.error.URLError) as e:
                print(f"    !! {e}")
                continue
            if not paths:
                print("    !! no audio returned"); continue

            chosen = paths[min(args.pick, len(paths)) - 1]
            shutil.copyfile(chosen, final)
            trim_silence(final)
            if tr.get("loop") and not args.no_loopfix:
                trim_tail_fade(final)
                loop_crossfade(final)

        if args.stems and tr.get("stems"):
            try:
                split_stems(final, name, tr["stems"], args.out, args.python)
                os.remove(final)   # the game plays the buses, not the full mix
            except (subprocess.CalledProcessError, ImportError, FileNotFoundError) as e:
                print(f"    !! stems failed ({e}); full mix kept as {name}.wav — "
                      f"install with: pip install demucs")
        print(f"    ✔ {os.path.relpath(final if not (args.stems and tr.get('stems')) else args.out, HERE)}\n")

    print(f"done in {(time.time() - t0) / 60:.1f} min. Unity: Assets → Refresh, then Play.")


if __name__ == "__main__":
    main()
