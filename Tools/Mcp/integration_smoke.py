#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Coast Run 통합 스모크 — CoastRemote(127.0.0.1:47001)."""
import json, socket, sys, time
from pathlib import Path

PORT = 47001
SHOT = Path(r"C:\dev\game\Tools\_shots")
SHOT.mkdir(parents=True, exist_ok=True)
REPORT = []

MENU_RUN = "Coast Run/\u25b6 PLAY \uc8fc\ud589\ub9cc (\ud504\ub864\ub85c\uadf8 \uac74\ub108\ub6b4) %#c"
MENU_RAISE = "Coast Run/\u25b6 PLAY \uc721\uc131 (05_Raising) _F5"
MENU_BOOT = "Coast Run/Play From Boot (recommended) %&b"


def send(cmd, timeout=30.0):
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
    last = {}
    while time.time() - t0 < sec:
        try:
            last = send("status", 8)
            if last and not last.get("compiling") and not last.get("updating"):
                return last
        except Exception as e:
            last = {"error": str(e)}
        time.sleep(2)
    return last


def wait_playing(want_scene_substr=None, sec=25):
    t0 = time.time()
    last = {}
    while time.time() - t0 < sec:
        try:
            last = send("status", 8)
            if last.get("playing"):
                sc = last.get("scene") or ""
                if want_scene_substr is None or want_scene_substr in sc:
                    return last
        except Exception as e:
            last = {"error": str(e)}
        time.sleep(0.8)
    return last


def shot(name, wait=1.2):
    time.sleep(wait)
    r = send(f"shot {name}", 20)
    path = SHOT / f"{name}.png"
    for _ in range(40):
        if path.exists() and path.stat().st_size > 2000:
            # reject pure black-ish tiny? keep size check only
            break
        time.sleep(0.25)
    ok = path.exists() and path.stat().st_size > 2000
    REPORT.append(("SHOT", name, "OK" if ok else "FAIL", str(r)[:160]))
    print(f"[SHOT {'OK' if ok else 'FAIL'}] {name} size={path.stat().st_size if path.exists() else 0}")
    return ok


def check(label, cond, detail=""):
    REPORT.append(("CHECK", label, "PASS" if cond else "FAIL", detail[:400]))
    print(f"[{'PASS' if cond else 'FAIL'}] {label} | {detail[:200]}")
    return cond


def step(name, cmd, sleep=0.4):
    try:
        r = send(cmd)
        ok = isinstance(r, dict) and r.get("error") is None and r.get("ok") != "error"
        # menu returns ok/error as value of "ok" key sometimes as "error"
        if isinstance(r, dict) and r.get("ok") == "error":
            ok = False
        REPORT.append(("CMD", name, "OK" if ok else "FAIL", json.dumps(r, ensure_ascii=False)[:220]))
        print(f"[CMD {'OK' if ok else 'FAIL'}] {name}: {r}")
        time.sleep(sleep)
        return r
    except Exception as e:
        REPORT.append(("CMD", name, "FAIL", str(e)))
        print(f"[CMD FAIL] {name}: {e}")
        return None


def enter_play(via_menu=None, via_scene=None, expect_scene=None):
    step("stop", "stop", 2.0)
    if via_menu:
        step("menu", f"menu {via_menu}", 1.0)
    elif via_scene:
        step("scene", f"scene {via_scene}", 1.0)
    step("play", "play", 1.0)
    st = wait_playing(expect_scene, sec=30)
    return st


def main():
    print("=== Coast Run integration smoke ===")
    st = wait_ready()
    check("bridge", bool(st), json.dumps(st, ensure_ascii=False)[:300])
    check("compile_clean", not (st.get("compileErrors") or "").strip(), st.get("compileErrors", ""))

    step("refresh", "refresh", 2)
    st = wait_ready(120)
    check("compile_after_refresh", not (st.get("compileErrors") or "").strip(), st.get("compileErrors", ""))

    # --- A. Title / Boot ---
    st = enter_play(via_menu=MENU_BOOT, expect_scene=None)
    # Boot may load Title shortly
    for _ in range(15):
        if st.get("playing"):
            break
        st = send("status")
        time.sleep(1)
    # if still not, scene+play
    if not st.get("playing"):
        st = enter_play(via_scene="00_Boot")
        time.sleep(4)
        st = send("status")
    check("title_or_boot_playing", bool(st.get("playing")), json.dumps(st, ensure_ascii=False)[:250])
    shot("it2_title", 3.0)

    # --- B. Run ---
    st = enter_play(via_menu=MENU_RUN, expect_scene="02_Run")
    if not st.get("playing") or "02_Run" not in (st.get("scene") or ""):
        # ASCII fallback: open scene then play (prologue skip may be off)
        st = enter_play(via_scene="02_Run", expect_scene="02_Run")
    check("run_playing", bool(st.get("playing")) and "02_Run" in (st.get("scene") or ""), json.dumps(st, ensure_ascii=False)[:250])
    shot("it2_run_01", 3.0)

    if st.get("playing") and "02_Run" in (st.get("scene") or ""):
        step("lane_L", "lane -1", 0.7)
        step("lane_R", "lane 1", 0.7)
        step("jump1", "jump", 0.35)
        step("jump2", "jump", 0.9)
        shot("it2_run_jump", 0.5)
        step("crouch", "crouch", 0.5)
        step("hit", "hit", 1.0)
        shot("it2_run_hit", 0.5)
        step("warp", "warp", 4.0)
        shot("it2_near_finish", 1.5)
        # let finish trigger
        time.sleep(5)
        shot("it2_after_warp", 1.0)
        # force clear if still running
        st2 = send("status")
        if st2.get("playing"):
            step("clear", "clear", 3.0)
        shot("it2_stage_clear", 2.0)

    # --- C. Raising ---
    st = enter_play(via_menu=MENU_RAISE, expect_scene="05_Raising")
    if not st.get("playing"):
        st = enter_play(via_scene="05_Raising", expect_scene="05_Raising")
    check("raising_playing", bool(st.get("playing")) and "05_Raising" in (st.get("scene") or ""), json.dumps(st, ensure_ascii=False)[:250])
    shot("it2_raising", 2.5)
    # tap center character / meal button approx
    step("tap_meal", "tap 0.22 0.72", 1.2)
    shot("it2_raising_act", 1.0)

    # runtime errors
    st = send("status")
    slog = st.get("log") or ""
    err_lines = [ln for ln in slog.split("\n") if "[Error]" in ln or "[Exception]" in ln]
    # ignore known soft warnings
    err_lines = [ln for ln in err_lines if "Title_Bus" not in ln]
    check("no_runtime_exceptions", len(err_lines) == 0, "\n".join(err_lines)[:800])
    if "Title_Bus" in slog and "prepared=False" in slog:
        REPORT.append(("WARN", "Title_Bus_not_prepared", "WARN", "splash video prepared=False"))
        print("[WARN] Title_Bus prepared=False")

    step("stop_final", "stop", 1)

    out = SHOT / "it2_report.txt"
    fails = 0
    lines = []
    for kind, name, result, detail in REPORT:
        lines.append(f"{result:4} | {kind:5} | {name} | {detail}")
        if result == "FAIL":
            fails += 1
    out.write_text("\n".join(lines), encoding="utf-8")
    print("\n=== REPORT ===\n" + "\n".join(lines))
    print(f"\nFAILS={fails} report={out}")
    return 0 if fails == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
