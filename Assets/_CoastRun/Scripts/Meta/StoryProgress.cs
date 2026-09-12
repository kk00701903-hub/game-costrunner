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
    }
}
