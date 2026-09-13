using UnityEngine;

namespace CoastRun
{
    /// 52차(사용자): 스토리 모드 구조.
    ///   · 컷씬은 챕터마다 **하나**(오프닝+엔딩 대본을 합쳐 웹소설처럼 읽는 StoryReaderUI) — 육성하며 주가 흘러 챕터 마지막 주에 열린다.
    ///   · 격주로 주말 **미니게임**이 올라오고, 이겨야 그 주가 넘어간다(ChapterMissionUI).
    ///   · 러닝은 이벤트: 52주에 **8번**(RunChapters)만, 한 판 3분 이내(MaxRunMeters). 나머지 챕터는 컷씬을 읽으면 그대로 지나간다.
    ///   · K-POP 러닝 11챕터부터는 롱컷씬 7개(LongCutIds)를 본 수만큼 열린다(KpopChapterSelect.IsUnlocked).
    public static class StoryProgress
    {
        /// 러닝이 있는 챕터(8회): 1 첫 달리기 · 4 하트 · 7 우리 기지 · 10 열두 개의 초 · 13 그 밤 · 15 스무 살 · 18 둘 중 하나 · 20 주파수(엔딩)
        public static readonly int[] RunChapters = { 1, 4, 7, 10, 13, 15, 18, 20 };
        public static bool IsRunChapter(int chapter) { foreach (var c in RunChapters) if (c == chapter) return true; return false; }

        /// 스토리 러닝 한 판 상한(m) — 평균 11 m/s 로 3분 이내. StageManager 가 targetDistance 에 씌운다.
        public const float MaxRunMeters = 1500f;

        /// 롱컷씬 7개: 프롤로그 · CH4 · CH7 · CH10 · CH13 · CH15 · 엔딩
        public static readonly string[] LongCutIds = { "PRO", "CH04_Open", "CH07_Open", "CH10_Open", "CH13_Open", "CH15_Open", "END" };
        public static bool IsLongCut(int chapter) => chapter == 4 || chapter == 7 || chapter == 10 || chapter == 13 || chapter == 15;

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

        // ── 61차(사용자): 컷씬은 **8개** — 러닝 챕터(1·4·7·10·13·15·18·20)마다 하나, 그 사이 챕터 이야기를 한 편으로 묶어 리더로 읽는다.
        //    「오프닝/클로징」 구분 없이 「컷씬 N」. 시네마도 이 8개만.
        public static int CutsceneCount => RunChapters.Length;
        /// chapter 가 컷씬이 열리는 챕터면 1..8, 아니면 0.
        public static int CutsceneIndex(int chapter) { for (int i = 0; i < RunChapters.Length; i++) if (RunChapters[i] == chapter) return i + 1; return 0; }
        public static int CutsceneChapter(int index) => RunChapters[Mathf.Clamp(index, 1, RunChapters.Length) - 1];
        public static int CutsceneFirstChapter(int index) => index <= 1 ? 1 : RunChapters[index - 2] + 1;
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
            PlayerPrefs.Save();
        }
        public static string CutsceneTitle(int index) => ChapterScript.Title(CutsceneChapter(index));
    }
}
