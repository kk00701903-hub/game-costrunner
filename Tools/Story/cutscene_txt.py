#!/usr/bin/env python3
"""
컷씬 대본 ↔ 편집용 텍스트 파일 (씬마다 한 파일, 번호 01~41).

    python cutscene_txt.py export <폴더>     # ChapterScript.Data.cs + .En.cs → <폴더>/NN_<씬ID>.txt
    python cutscene_txt.py import <폴더>     # <폴더>/*.txt → ChapterScript.Data.cs + ChapterScript.En.cs 재생성

파일 형식(한 줄 = 한 컷):
    [n] BG | 배경ID | 왼쪽스탠딩(이름:표정) | 오른쪽스탠딩 | 변형
    [n] CG | 일러스트ID | 설명
    [n] SAY | 화자 | 한국어 대사
        EN: 영어 대사
    [n] NARR | 지문            (EN: 줄 동일)
    [n] LETTER | 편지 한 줄     (EN: 줄 동일)
줄을 지우거나 끼워 넣어도 된다 — import 때 [n]은 무시하고 순서대로 다시 번호를 매긴다.
EN: 줄이 없으면 영어 모드에서 한국어가 그대로 나온다. '#'로 시작하는 줄은 주석.
파일 머리의 '제목:' 줄(Open 파일에만)을 고치면 챕터 제목(한/영)도 바뀐다.
"""
import io, os, re, sys, glob

HERE = os.path.dirname(os.path.abspath(__file__))
STORY = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_CoastRun", "Scripts", "Story"))
DATA = os.path.join(STORY, "ChapterScript.Data.cs")
EN = os.path.join(STORY, "ChapterScript.En.cs")
LOC = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_CoastRun", "Scripts", "Core", "Loc.cs"))

SPEAKER_EN = {"하늘": "Haneul", "도윤": "Doyun", "루아": "Rua", "만수": "Mansu", "할머니": "Grandma", "라디오": "Radio", "DJ": "DJ"}


def cs_unescape(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def cs_escape(s):
    return s.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n")


def read_data():
    """returns titles{int:str}, scenes[(id,[ (kind,a,b,c,d) ])] in file order"""
    src = io.open(DATA, encoding="utf-8").read()
    titles = {int(m.group(1)): cs_unescape(m.group(2)) for m in re.finditer(r'\{\s*(\d+),\s*"((?:[^"\\]|\\.)*)"\s*\}', src.split("Scenes")[0])}
    scenes, sid, cur = [], None, None
    for ln in src.splitlines():
        m = re.match(r'\s*\{\s*"([A-Za-z0-9_]+)",\s*new\s*\[\]', ln)
        if m:
            sid = m.group(1); cur = []; scenes.append((sid, cur)); continue
        m = re.match(r'\s*new VnLine\("(\w+)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\)', ln)
        if m and cur is not None:
            cur.append(tuple(cs_unescape(g) for g in m.groups()))
    return titles, scenes


def read_en():
    if not os.path.exists(EN):
        return {}
    src = io.open(EN, encoding="utf-8").read()
    return {m.group(1): cs_unescape(m.group(2)) for m in re.finditer(r'\{\s*"([A-Za-z0-9_]+:\d+)",\s*"((?:[^"\\]|\\.)*)"\s*\}', src)}


def read_loc_titles():
    src = io.open(LOC, encoding="utf-8").read()
    return {int(m.group(1)): cs_unescape(m.group(2)) for m in re.finditer(r'\{\s*"ch\.(\d+)",\s*"((?:[^"\\]|\\.)*)"\s*\}', src)}


def scene_label(sid, titles, loc_titles):
    if sid == "PRO":
        return "프롤로그"
    m = re.match(r"CH(\d+)_(Open|Close)", sid)
    if m:
        ch = int(m.group(1))
        return f"{ch}챕터 「{titles.get(ch, '')}」 {'시작' if m.group(2) == 'Open' else '마무리'}"
    return {"END_A": "엔딩 A (만난다)", "END_B": "엔딩 B (못 만난다)"}.get(sid, sid)


def export(folder):
    titles, scenes = read_data()
    en = read_en()
    loc_titles = read_loc_titles()
    os.makedirs(folder, exist_ok=True)
    for old in glob.glob(os.path.join(folder, "[0-9][0-9]_*.txt")):
        os.remove(old)
    for n, (sid, lines) in enumerate(scenes, 1):
        out = [f"# {n:02d}  {sid}  —  {scene_label(sid, titles, loc_titles)}",
               "# 형식: [번호] 종류 | 화자/그림 | 한국어   ← 바로 아래 'EN:' 줄이 영어. 줄 삭제·추가 가능, 번호는 저장 때 자동 정리.",
               "# BG = 배경ID | 왼쪽 스탠딩 | 오른쪽 스탠딩 | 텍스트위치 / CG = 일러스트ID | 설명 | 텍스트위치 / SAY = 화자 | 대사 / NARR = 지문 / LETTER = 편지",
               "# 텍스트위치: 비우면 대사창이 화면 위(그림의 인물이 아래쪽에 있어서), '아래' 라고 쓰면 화면 아래"]
        m = re.match(r"CH(\d+)_Open", sid)
        if m:
            ch = int(m.group(1))
            out.append(f"제목: {titles.get(ch, '')} | EN: {loc_titles.get(ch, '')}")
        out.append("")
        for i, (kind, a, b, c, d) in enumerate(lines):
            if kind == "BG":
                out.append(f"[{i + 1}] BG | {a} | {b} | {c} | {d}".rstrip(" |"))
            elif kind == "CG":
                out.append(f"[{i + 1}] CG | {a} | {b}" + (f" | {c}" if c else ""))
            elif kind == "SAY":
                out.append(f"[{i + 1}] SAY | {a} | {b}")
                out.append(f"    EN: {en.get(f'{sid}:{i}', '')}")
            else:
                out.append(f"[{i + 1}] {kind} | {b}")
                out.append(f"    EN: {en.get(f'{sid}:{i}', '')}")
        io.open(os.path.join(folder, f"{n:02d}_{sid}.txt"), "w", encoding="utf-8-sig", newline="\r\n").write("\n".join(out) + "\n")
    print(f"{len(scenes)} scenes → {folder}")


def parse_txt(path):
    sid = re.match(r"\d+_([A-Za-z0-9_]+)\.txt$", os.path.basename(path)).group(1)
    lines, en, title = [], {}, None
    for raw in io.open(path, encoding="utf-8-sig"):
        ln = raw.rstrip("\r\n")
        s = ln.strip()
        if not s or s.startswith("#"):
            continue
        if s.startswith("제목:"):
            parts = s[3:].split("| EN:", 1)
            title = (parts[0].strip(), parts[1].strip() if len(parts) > 1 else None)
            continue
        if s.startswith("EN:"):
            if lines:
                en[len(lines) - 1] = s[3:].strip()
            continue
        m = re.match(r"\[\d+\]\s*(\w+)\s*\|\s*(.*)$", s)
        if not m:
            raise SystemExit(f"{os.path.basename(path)}: 형식을 못 읽음 → {ln}")
        kind, rest = m.group(1).upper(), m.group(2)
        cells = [c.strip() for c in rest.split("|")]
        if kind == "BG":
            cells += [""] * (4 - len(cells))
            lines.append(("BG", cells[0], cells[1], cells[2], cells[3]))
        elif kind == "CG":
            lines.append(("CG", cells[0], cells[1] if len(cells) > 1 else "", cells[2] if len(cells) > 2 else "", ""))
        elif kind == "SAY":
            if len(cells) < 2:
                raise SystemExit(f"{os.path.basename(path)}: SAY는 '화자 | 대사' → {ln}")
            lines.append(("SAY", cells[0], "|".join(cells[1:]).strip(), "", ""))
        elif kind in ("NARR", "LETTER"):
            lines.append((kind, "", rest.strip(), "", ""))
        else:
            raise SystemExit(f"{os.path.basename(path)}: 모르는 종류 {kind} → {ln}")
    return sid, lines, en, title


def import_(folder):
    files = sorted(glob.glob(os.path.join(folder, "[0-9][0-9]_*.txt")))
    if not files:
        raise SystemExit("txt 파일이 없음: " + folder)
    titles, _ = read_data()
    loc_titles = read_loc_titles()
    scenes, en_all = [], {}
    for f in files:
        sid, lines, en, title = parse_txt(f)
        scenes.append((sid, lines))
        for i, t in en.items():
            if t:
                en_all[f"{sid}:{i}"] = t
        m = re.match(r"CH(\d+)_Open", sid)
        if m and title:
            ch = int(m.group(1))
            if title[0]:
                titles[ch] = title[0]
            if title[1]:
                loc_titles[ch] = title[1]
    # Data.cs
    out = ["// <auto-generated> Tools/Story/cutscene_txt.py import — 편집용 txt에서 생성. 직접 편집하지 말 것.",
           "using System.Collections.Generic;", "", "namespace CoastRun", "{", "    public static partial class ChapterScript", "    {",
           "        public static readonly Dictionary<int, string> Titles = new Dictionary<int, string>", "        {"]
    for ch in sorted(titles):
        out.append(f'            {{ {ch}, "{cs_escape(titles[ch])}" }},')
    out += ["        };", "", "        public static readonly Dictionary<string, VnLine[]> Scenes = new Dictionary<string, VnLine[]>", "        {"]
    for sid, lines in scenes:
        out.append(f'            {{ "{sid}", new[]')
        out.append("            {")
        for kind, a, b, c, d in lines:
            out.append(f'                new VnLine("{kind}", "{cs_escape(a)}", "{cs_escape(b)}", "{cs_escape(c)}", "{cs_escape(d)}"),')
        out.append("            } },")
    out += ["        };", "    }", "}", ""]
    io.open(DATA, "w", encoding="utf-8", newline="\n").write("\n".join(out))
    # En.cs
    out = ["using System.Collections.Generic;", "", "namespace CoastRun", "{",
           "    /// 컷씬 대사 영어판 — key \"<sceneId>:<line index>\" (Tools/Story/cutscene_txt.py import 로 생성). 영어 모드(Loc.IsKo == false)에서 ChapterVN이 사용.",
           "    public static partial class ChapterScript", "    {", "        public static string SpeakerEn(string ko)", "        {", "            switch (ko)", "            {"]
    for k, v in SPEAKER_EN.items():
        out.append(f'                case "{k}": return "{v}";')
    out += ["                default: return ko;", "            }", "        }", "",
            "        /// 영어 텍스트. 없으면 null(한국어 유지).", "        public static string TextEn(string sceneId, int index)", "        {",
            "            return En.TryGetValue(sceneId + \":\" + index, out var s) ? s : null;", "        }", "",
            "        private static readonly Dictionary<string, string> En = new Dictionary<string, string>", "        {"]
    cur = None
    for sid, lines in scenes:
        for i in range(len(lines)):
            k = f"{sid}:{i}"
            if k in en_all:
                if sid != cur:
                    cur = sid; out.append(f"            // {sid}")
                out.append(f'            {{ "{k}", "{cs_escape(en_all[k])}" }},')
    out += ["        };", "    }", "}", ""]
    io.open(EN, "w", encoding="utf-8", newline="\n").write("\n".join(out))
    # Loc.cs chapter titles (EN)
    src = io.open(LOC, encoding="utf-8").read()
    for ch, t in loc_titles.items():
        src = re.sub(r'\{\s*"ch\.%d",\s*"(?:[^"\\]|\\.)*"\s*\}' % ch, '{ "ch.%d", "%s" }' % (ch, cs_escape(t)), src)
    io.open(LOC, "w", encoding="utf-8", newline="\n").write(src)
    n = sum(len(l) for _, l in scenes)
    print(f"{len(scenes)} scenes, {n} lines, {len(en_all)} EN → {os.path.relpath(DATA, HERE)}, {os.path.relpath(EN, HERE)}")


if __name__ == "__main__":
    if len(sys.argv) < 3 or sys.argv[1] not in ("export", "import"):
        print(__doc__); sys.exit(1)
    (export if sys.argv[1] == "export" else import_)(sys.argv[2])
