# -*- coding: utf-8 -*-
import re
text = open(r"C:\dev\game\Assets\_CoastRun\Config\StageTable.asset", encoding="utf-8").read()
stages = []
for b in re.split(r"- chapterIndex:", text)[1:]:
    mi = re.search(r"stageIndex: (\d+)", b)
    md = re.search(r"targetDistance: ([\d.]+)", b)
    mn = re.search(r'stageName: "([^"]+)"', b)
    if mi and md:
        name = mn.group(1) if mn else ""
        try:
            name = bytes(name, "utf-8").decode("unicode_escape")
        except Exception:
            pass
        stages.append((int(mi.group(1)), float(md.group(1)), name))
total = sum(d for _, d, _ in stages)
print(f"stages={len(stages)} total={total:.0f}m")
cum = 0.0
for i, d, n in stages:
    cum += d
    mark = " <-- CH6" if i == 6 else ""
    print(f"S{i:02d}: {d:7.0f}m  cum={cum:7.0f}m  rem={total-cum:7.0f}m  {n}{mark}")
before = sum(d for i, d, _ in stages if i < 6)
ch6 = next(d for i, d, _ in stages if i == 6)
print(f"\nCH6 length={ch6:.0f}m")
print(f"journey before CH6={before:.0f}m")
print(f"at CH6 start progress={before/total:.1%}")
print(f"at CH6 end progress={(before+ch6)/total:.1%}")
