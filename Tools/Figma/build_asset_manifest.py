#!/usr/bin/env python3
"""Coast Run → Figma 이관용 에셋 목록 만들기.
Assets/ 의 이미지를 카테고리로 나눠 Tools/Figma/frequency_assets.json 에 적고,
Figma 에 넣을 축소본(긴 변 ≤ max px)을 Tools/Figma/_import/<카테고리>/ 에 만든다(원본은 그대로).
"""
import os, re, json, sys
from pathlib import Path
from PIL import Image
sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(r"C:\dev\game"); A = ROOT / "Assets"; RES = A / "Resources" / "CoastRun"
OUT = ROOT / "Tools" / "Figma"; IMP = OUT / "_import"
EXT = {".png", ".jpg", ".jpeg", ".webp"}

def files(d, pat=None, recursive=False, exclude=None):
    d = Path(d)
    if not d.is_dir(): return []
    it = d.rglob("*") if recursive else d.iterdir()
    out = []
    for p in it:
        if p.suffix.lower() not in EXT or not p.is_file(): continue
        if pat and not re.match(pat, p.name): continue
        if exclude and any(x in str(p) for x in exclude): continue
        out.append(p)
    return sorted(out)

cats = [
  ("ui",        "UI · 아이콘",        768,  files(RES, r"(UI_|Icon_|Watch_)") + files(A/"_CoastRun"/"Art"/"UI") + files(A/"Art"/"UI") + files(A/"_CoastRun"/"Resources"/"CoastRun", r"(Icon_|WK_|Watch_)"), "HUD·버튼·아이콘·미니게임 UI"),
  ("character", "캐릭터",             768,  files(RES, r"(Raise_|GirlSkater_|Rival_|MG_Char_)") + files(A/"Art"/"Character") + files(ROOT/"Tools"/"blender"/"npc_prev"), "주인공 표정·러너·라이벌·NPC 6종"),
  ("card",      "카드 · 스케줄",       640,  files(RES/"Card") + files(RES, r"Sched_"), "주간 스케줄 카드·이벤트 카드"),
  ("env",       "배경 · 환경",         1024, files(RES, r"(Sky_|BG_|Far_|Cloud_|Side_|Sea_)") + files(RES/"Scene") + files(A/"_CoastRun"/"Art"/"Scene"), "하늘·먼 배경·장면 배경(계절 변형 포함)"),
  ("props",     "장애물 · 소품 · FX",  512,  files(RES, r"(Obs_|Prop_|Fx_|BikeWheel|MG_Ddakji|MG_)"), "러닝 장애물·보스·이펙트·딱지"),
  ("texture",   "텍스처",             768,  files(RES, r"Tex_") + files(RES/"Rig"/"Textures"), "건물 파사드·지붕·돌담·리그 텍스처"),
  ("cutscene",  "컷씬 · 챕터",         640,  files(RES, r"Cut_") + files(RES/"컷씬이미지", recursive=True, exclude=["_레거시", "_폴백"]), "챕터별 컷씬(현행본; 레거시·폴백 제외)"),
  ("brand",     "브랜드 · 앨범 · 컨셉", 1024, files(A/"_CoastRun"/"Art"/"Brand") + files(RES/"Album") + files(RES/"FanArt") + files(A/"Art"/"Concept") + files(A/"_Guide"/"Reference"), "로고·앨범 아트·팬아트·컨셉·레퍼런스"),
]
seen = set(); man = {"project": "Frequency", "source": str(A), "categories": []}
total = 0
for key, title, mx, fs, note in cats:
    items = []; used = set()
    d = IMP / key; d.mkdir(parents=True, exist_ok=True)
    for p in fs:
        rp = str(p.resolve()).lower()
        if rp in seen: continue
        seen.add(rp)
        try:
            im = Image.open(p); w, h = im.size
        except Exception as e:
            print("skip", p, e); continue
        # 알파 없는 그림(컷씬·배경·텍스처 대부분)은 JPEG 로 — Figma 파일 크기를 1/5 로
        im = im.convert("RGBA") if im.mode not in ("RGB", "RGBA") else im
        has_alpha = im.mode == "RGBA" and im.getchannel("A").getextrema()[0] < 250
        ext = ".png" if has_alpha else ".jpg"
        dst = d / (p.stem + ext)
        if dst in used: dst = d / (p.stem + "__" + str(len(used)) + ext)
        used.add(dst)
        k = min(mx / w, mx / h, 1.0)
        im2 = im.resize((max(1, int(w * k)), max(1, int(h * k))), Image.LANCZOS) if k < 1.0 else im
        if has_alpha: im2.save(dst, optimize=True)
        else: im2.convert("RGB").save(dst, quality=86, optimize=True)
        items.append({"name": p.stem, "src": str(p.relative_to(ROOT)), "import": str(dst.relative_to(ROOT)), "w": w, "h": h,
                      "group": str(p.parent.relative_to(ROOT)).replace("\\", "/")})
    man["categories"].append({"key": key, "title": title, "note": note, "items": items}); total += len(items)
    print(f"{key:10s} {title:14s} {len(items):4d}")
(OUT / "frequency_assets.json").write_text(json.dumps(man, ensure_ascii=False, indent=1), encoding="utf-8")
print("total", total, "→", OUT / "frequency_assets.json")
