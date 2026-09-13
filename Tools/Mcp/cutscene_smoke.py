#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Cutscene playtest via CoastRemote `vn` command."""
import json, socket, time, sys
from pathlib import Path

PORT = 47001
SHOT = Path(r"C:\dev\game\Tools\_shots")


def send(cmd, timeout=45.0):
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


def wait_bridge(sec=120):
    t0 = time.time()
    while time.time() - t0 < sec:
        try:
            st = send("status", 8)
            if st and not st.get("compiling") and not st.get("updating"):
                # wait until vn command exists (domain reload)
                return st
        except Exception:
            pass
        time.sleep(2)
    return {}


def shot(name, wait=0.8):
    time.sleep(wait)
    send(f"shot {name}")
    p = SHOT / f"{name}.png"
    for _ in range(25):
        if p.exists() and p.stat().st_size > 1500:
            return True
        time.sleep(0.2)
    return False


def advance(n=20, dt=0.5):
    for i in range(n):
        send("key Space")
        time.sleep(dt)
        if i % 6 == 5:
            shot(f"cut_step_{i}", 0.15)


def main():
    print("wait compile/reload…")
    send("refresh")
    time.sleep(2)
    st = wait_bridge()
    print("status", st)

    # probe vn command
    send("stop"); time.sleep(1.5)
    send("menu Coast Run/Play From Boot (recommended) %&b"); time.sleep(0.8)
    send("play")
    for _ in range(25):
        st = send("status")
        if st.get("playing"):
            break
        time.sleep(0.8)
    time.sleep(2)
    send("tap 0.5 0.85"); time.sleep(1.2)
    send("tap 0.5 0.85"); time.sleep(2)
    shot("cut_title", 1.0)

    # Try vn — may fail until domain reload picks up CoastRemote change
    r = send("vn CH02_Open")
    print("vn CH02_Open", r)
    if r.get("ok") == "error" or "unknown" in str(r).lower() or r.get("error"):
        print("vn command missing — wait more")
        time.sleep(8)
        st = wait_bridge()
        r = send("vn CH02_Open")
        print("retry", r)

    time.sleep(1.5)
    shot("cut_ch02_0", 1.0)
    advance(25, 0.45)
    shot("cut_ch02_mid", 0.5)
    send("vn skip"); time.sleep(0.8)
    send("key S"); time.sleep(1.0)
    shot("cut_ch02_end", 0.8)

    # Long-ish music scene sample
    r = send("vn PRO")
    print("vn PRO", r)
    time.sleep(1.5)
    shot("cut_pro_0", 1.0)
    advance(35, 0.4)
    shot("cut_pro_mid", 0.5)
    send("key S"); time.sleep(1.2)
    shot("cut_pro_skip", 0.8)

    # Ending with many stop/start
    r = send("vn END_B")
    print("vn END_B", r)
    time.sleep(1.5)
    shot("cut_endb_0", 1.0)
    advance(30, 0.4)
    shot("cut_endb_mid", 0.5)
    send("key S"); time.sleep(1.2)
    shot("cut_endb_skip", 0.8)

    st = send("status")
    print("final", json.dumps(st, ensure_ascii=False)[:900])
    print("log", json.dumps(send("log 50"), ensure_ascii=False)[:1500])
    send("stop")
    return 0


if __name__ == "__main__":
    sys.exit(main())
