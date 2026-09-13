#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""시네마틱 스모크 — CoastRemote Dev/Cine 메뉴 + 캡처."""
import json, socket, time, sys
from pathlib import Path

PORT = 47001
SHOT = Path(r"C:\dev\game\Tools\_shots")
SHOT.mkdir(parents=True, exist_ok=True)


def send(cmd, timeout=60.0):
    s = socket.create_connection(("127.0.0.1", PORT), timeout=timeout)
    try:
        s.sendall((cmd + "\n").encode("utf-8"))
        buf = b""
        s.settimeout(timeout)
        while not buf.endswith(b"\n"):
            chunk = s.recv(65536)
            if not chunk:
                break
            buf += chunk
        txt = buf.decode("utf-8", "replace").strip()
        try:
            return json.loads(txt) if txt else {}
        except Exception:
            return {"raw": txt}
    finally:
        s.close()


def wait_ready(sec=90):
    t0 = time.time()
    while time.time() - t0 < sec:
        try:
            st = send("status", 8)
            if st and not st.get("compiling") and not st.get("updating"):
                return st
        except Exception:
            pass
        time.sleep(1.5)
    return {}


def ensure_play():
    st = send("status")
    if st.get("playing"):
        if st.get("paused"):
            send("pause")
        return st
    send("stop")
    time.sleep(1)
    send("menu Coast Run/Play From Boot (recommended) %&b")
    time.sleep(0.5)
    send("play")
    for _ in range(40):
        st = send("status")
        if st.get("playing"):
            break
        time.sleep(0.5)
    time.sleep(2.5)
    # dismiss title / splash taps
    send("tap 0.5 0.85"); time.sleep(1.0)
    send("tap 0.5 0.85"); time.sleep(1.5)
    return send("status")


def shot(name, wait=0.6):
    time.sleep(wait)
    r = send(f"shot {name}")
    p = SHOT / f"{name}.png"
    for _ in range(30):
        if p.exists() and p.stat().st_size > 1500:
            return True, r
        time.sleep(0.15)
    return False, r


def advance_cuts(n=8, dt=0.35):
    for _ in range(n):
        send("key Space")
        time.sleep(dt)


def play_cine(menu_suffix, tag, advances=10):
    print(f"=== {tag} ===")
    r = send(f"menu Coast Run/Dev/Cine - {menu_suffix}")
    print("menu", r)
    time.sleep(1.2)
    ok0, _ = shot(f"cine_{tag}_0", 0.8)
    advance_cuts(advances, 0.4)
    ok1, _ = shot(f"cine_{tag}_mid", 0.4)
    # long skip / finish
    send("key S"); time.sleep(0.8)
    send("key Space"); time.sleep(0.6)
    send("key S"); time.sleep(1.0)
    ok2, _ = shot(f"cine_{tag}_end", 0.5)
    # dismiss title card if still up
    send("tap 0.5 0.5"); time.sleep(0.6)
    send("key Space"); time.sleep(0.5)
    print(f"shots {tag}: start={ok0} mid={ok1} end={ok2}")
    return ok0 and ok1


def main():
    print("status", wait_ready())
    send("clearlog")
    st = ensure_play()
    print("play", {k: st.get(k) for k in ("playing", "paused", "scene", "compileErrors")})
    if not st.get("playing"):
        print("FAIL: not playing")
        return 1

    send("gameview")
    shot("cine_boot", 0.5)

    results = []
    # OPEN + a few cuts + ending sample (full CS1~8 would be long; cover representative set)
    for suffix, tag, n in [
        ("OPEN", "open", 12),
        ("CS1", "cs1", 10),
        ("CS4", "cs4", 10),
        ("CS8", "cs8", 10),
        ("END_A", "enda", 10),
        ("Select", "select", 0),
    ]:
        if suffix == "Select":
            print("=== select ===")
            r = send("menu Coast Run/Dev/Cine - Select")
            print("menu", r)
            time.sleep(1.0)
            ok, _ = shot("cine_select", 0.8)
            results.append(("select", ok))
            send("key Escape"); time.sleep(0.4)
            send("tap 0.92 0.08"); time.sleep(0.5)  # try X
            continue
        ok = play_cine(suffix, tag, n)
        results.append((tag, ok))
        time.sleep(0.8)

    st = send("status")
    log = send("log 80")
    print("final", json.dumps({k: st.get(k) for k in ("playing", "paused", "scene", "compileErrors")}, ensure_ascii=False))
    print("log", json.dumps(log, ensure_ascii=False)[:2500])
    print("RESULTS", results)
    errs = (st.get("compileErrors") or "")
    fails = [t for t, ok in results if not ok]
    if errs:
        print("COMPILE ERRORS:", errs)
        return 2
    if fails:
        print("SHOT FAILS:", fails)
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
