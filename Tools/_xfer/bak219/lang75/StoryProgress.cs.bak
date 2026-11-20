using UnityEngine;

namespace CoastRun
{
    /// 52차(사용자): 스토리 모드 구조.
    ///   · 컷씬은 챕터마다 **하나**(오프닝+엔딩 대본을 합쳐 웹소설처럼 읽는 StoryReaderUI) — 육성하며 주가 흘러 챕터 마지막 주에 열린다.
    ///   · 격주로 주말 **미니게임**이 올라오고, 이겨야 그 주가 넘어간다(ChapterMissionUI).
    ///   · 러닝은 이벤트: 52주에 **8번**(RunChapters)만, 한 판 3분 이내(MaxRunMeters). 나머지 챕터는 컷씬을 읽으면 그대로 지나간다.
    ///   · K-POP 러닝 = 스토리용 **돈·아이템 파밍**(챕터 전부 해금, 스토리 진행과 분리).
    ///   85차(대본 v4): **컷씬 챕터와 러닝 챕터를 분리** — 컷씬 8편은 CutsceneChapters(1·3·5·7·10·12·17·20), 러닝(대회)은 그대로 RunChapters.
    ///   그 사이 챕터에는 **보조 컷씬 EV1~10**(EventChapters: 2·4·6·8·9·11·13·14·15·19, 4컷 × 7초). CH16·18은 컷씬 없음.
    public static class StoryProgress
    {
        /// 러닝이 있는 챕터(8회): 1 첫 달리기 · 4 · 7 · 10 · 13 · 15 · 18 · 20(엔딩)
        public static readonly int[] RunChapters = { 1, 4, 7, 10, 13, 15, 18, 20 };
        public static bool IsRunChapter(int chapter) { foreach (var c in RunChapters) if (c == chapter) return true; return false; }

        /// 85차: 컷씬(시네마틱 CS1~8)이 열리는 챕터 — v4 배치표(Docs/CUTSCENE_SCRIPTS_v4.md 「챕터 배치」).
        public static readonly int[] CutsceneChapters = { 1, 3, 5, 7, 10, 12, 17, 20 };   // 107차(사용자): 대본 머리 순서와 같게 — CS5 CH10(EV6 첫눈은 CH9 로 그 앞)
        /// 107차(사용자): 보조 컷씬 EV n 이 열리는 챕터 = 대본 순서(EV1 CH2 · EV3 CH4 · EV4 CH6 · EV5 CH8 · EV6 CH9 · EV7 CH11 · EV8 CH13 · EV9 CH14 · **EV2 CH15**(회상 묶음 끝) · EV10 CH19). 배열 인덱스 = EV 번호−1.
        public static readonly int[] EventChapters = { 2, 15, 4, 6, 8, 9, 11, 13, 14, 19 };

        /// 스토리 러닝 한 판 상한(m) — 평균 11 m/s 로 3분 이내. StageManager 가 targetDistance 에 씌운다.
        public const float MaxRunMeters = 1500f;

        /// 롱컷 씬 id(본 기록용). 스토리 진행은 레벨/K-POP 과 무관.
        public static readonly string[] LongCutIds = { "PRO", "CH03_Open", "CH05_Open", "CH07_Open", "CH10_Open", "CH12_Open", "CH17_Open", "END" };
        public static bool IsLongCut(int chapter) => chapter == 3 || chapter == 5 || chapter == 7 || chapter == 10 || chapter == 12 || chapter == 17;

        public static bool SceneSeen(string id)
        {
            if (id == "END") { var p = GameManager.I != null ? GameManager.I.Profile : null; return p != null && p.endingMask != 0; }
            return PlayerPrefs.GetInt("CoastRun_VN_" + id, 0) == 1;
        }
        public static int LongCutsCleared { get { int n = 0; foreach (var id in LongCutIds) if (SceneSeen(id)) n++; return n; } }

        /// 격주 주말 미니게임 — 짝수 주(2, 4, 6 …)에 5종을 돌아가며. 주 수가 늘어난(게이트 연장) 경우도 같은 규칙.
        public static bool WeeklyMinigame(int week, out ChapterMission.Kind kind)
        {
            kind = ChapterMission.Kind.Marbles;
            if (week < 2 || week % 2 != 0) return false;
            kind = (ChapterMission.Kind)((week / 2 - 1) % 5);
            return true;
        }

        /// 챕터 컷씬(리더) 씬 id 들 — 오프닝 + 엔딩(있으면).
        public static string[] ChapterSceneIds(int chapter)
        {
            var o = ChapterScript.OpenId(chapter); var c = ChapterScript.CloseId(chapter);
            bool ho = ChapterScript.Has(o), hc = ChapterScript.Has(c);
            if (ho && hc) return new[] { o, c };
            if (ho) return new[] { o };
            if (hc) return new[] { c };
            return new string[0];
        }
        public static bool ChapterRead(int chapter) => SceneSeen(ChapterScript.OpenId(chapter));

        // ── 61차(사용자): 컷씬은 **8개**. 85차: 챕터는 CutsceneChapters(러닝 챕터와 다름). 그 사이 챕터 이야기를 한 편으로 묶어 리더로 읽는다.
        public static int CutsceneCount => CutsceneChapters.Length;
        /// chapter 가 컷씬이 열리는 챕터면 1..8, 아니면 0.
        public static int CutsceneIndex(int chapter) { for (int i = 0; i < CutsceneChapters.Length; i++) if (CutsceneChapters[i] == chapter) return i + 1; return 0; }
        public static int CutsceneChapter(int index) => CutsceneChapters[Mathf.Clamp(index, 1, CutsceneChapters.Length) - 1];
        public static int CutsceneFirstChapter(int index) => index <= 1 ? 1 : CutsceneChapters[index - 2] + 1;
        /// 컷씬 N 에 묶인 챕터 이야기 씬 id 전부(앞 챕터부터).
        public static string[] CutsceneSceneIds(int index)
        {
            var list = new System.Collections.Generic.List<string>();
            for (int c = CutsceneFirstChapter(index); c <= CutsceneChapter(index); c++) list.AddRange(ChapterSceneIds(c));
            return list.ToArray();
        }
        public static bool CutsceneRead(int index) => ChapterRead(CutsceneChapter(index));
        /// 68차: 시네마틱으로 본 컷씬도 리더와 같은 흔적(CoastRun_VN_<id>)을 남긴다.
        public static void MarkCutsceneSeen(int index)
        {
            foreach (var id in CutsceneSceneIds(index)) { PlayerPrefs.SetInt("CoastRun_VN_" + id, 1); RecordTable.OnSceneWatched(id); }
            PlayerPrefs.SetInt("CoastRun_VN_" + ChapterScript.OpenId(CutsceneChapter(index)), 1);
            PlayerPrefs.Save();
        }
        public static string CutsceneTitle(int index) { var d = CinematicTable.Cutscene(index); return d != null ? d.title : ChapterScript.Title(CutsceneChapter(index)); }

        // ── 85차: 보조 컷씬 EV1~10 ──
        public static int EventCount => EventChapters.Length;
        /// chapter 에 보조 컷씬이 있으면 1..10, 아니면 0.
        public static int EventIndex(int chapter) { for (int i = 0; i < EventChapters.Length; i++) if (EventChapters[i] == chapter) return i + 1; return 0; }
        public static int EventChapter(int index) => EventChapters[Mathf.Clamp(index, 1, EventChapters.Length) - 1];
        public static bool EventSeen(int index) => SceneSeen(ChapterScript.OpenId(EventChapter(index)));
        public static void MarkEventSeen(int index)
        {
            foreach (var id in ChapterSceneIds(EventChapter(index))) { PlayerPrefs.SetInt("CoastRun_VN_" + id, 1); RecordTable.OnSceneWatched(id); }
            PlayerPrefs.SetInt("CoastRun_VN_" + ChapterScript.OpenId(EventChapter(index)), 1);
            PlayerPrefs.Save();
        }
        public static string EventTitle(int index) { var d = CinematicTable.Event(index); return d != null ? d.title : ChapterScript.Title(EventChapter(index)); }
    }
}
