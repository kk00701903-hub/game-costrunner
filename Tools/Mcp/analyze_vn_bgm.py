# -*- coding: utf-8 -*-
import re, pathlib
root = pathlib.Path(r"C:\dev\game\Tools\Story\script")
rows = []
files = list(root.glob("*.txt"))
for p in sorted(files):
    text = p.read_text(encoding="utf-8", errors="replace")
    bgms = []
    for ln in text.splitlines():
        m = re.search(r"\]\s*BGM\s*\|(.*)$", ln.strip())
        if m:
            parts = [x.strip() for x in m.group(1).split("|")]
            bgms.append(parts[0] if parts else "")
    if not bgms:
        continue
    switches = 0
    last = None
    events = []
    for k in bgms:
        base = k.upper().rstrip("SRW")
        tok = "STOP" if (base in ("정지", "무음", "∅", "0", "STOP", "") or k in ("정지", "무음")) else base
        events.append(tok)
        if tok != last:
            switches += 1
            last = tok
    dialog = len(re.findall(r"\]\s*(SAY|NARR|LETTER|CG|BG)\b", text))
    est = max(dialog * 2.2, 8)
    rows.append((p.stem, len(bgms), switches, round(est), events, bgms))

print(f"scenes_with_bgm={len(rows)} / total={len(files)}")
print("--- densest (track switches per minute, est.) ---")
scored = []
for name, n, sw, est, ev, raw in rows:
    spm = sw / (est / 60.0)
    scored.append((spm, name, n, sw, est, ev, raw))
for spm, name, n, sw, est, ev, raw in sorted(scored, reverse=True):
    print(f"{name}: cues={n} trackSw={sw} est~{est}s ({spm:.1f}/min) events={ev}")
print("--- raw cue lists ---")
for name, n, sw, est, ev, raw in rows:
    print(f"{name}: {raw}")
