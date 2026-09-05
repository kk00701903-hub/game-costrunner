using UnityEngine;

namespace CoastRun
{
    /// 52주 = 20챕터. (3,3,2,3,2) × 4 → 계절마다 정확히 5챕터·13주.
    /// 1~13 봄 / 14~26 여름 / 27~39 가을 / 40~52 겨울.
    public static class Timeline
    {
        public const int Weeks = 52;
        public const int Chapters = 20;
        public const int PhasesPerWeek = 3;

        private static readonly int[] Length =
        {
            3, 3, 2, 3, 2,
            3, 3, 2, 3, 2,
            3, 3, 2, 3, 2,
            3, 3, 2, 3, 2,
        };

        public static SeasonKind SeasonOf(int week) =>
            (SeasonKind)Mathf.Clamp((Mathf.Clamp(week, 1, Weeks) - 1) / 13, 0, 3);

        public static string SeasonName(SeasonKind s)
        {
            switch (s)
            {
                case SeasonKind.Spring: return "봄";
                case SeasonKind.Summer: return "여름";
                case SeasonKind.Autumn: return "가을";
                default: return "겨울";
            }
        }

        public static int WeekStart(int chapter)
        {
            chapter = Mathf.Clamp(chapter, 1, Chapters);
            int s = 1;
            for (int i = 1; i < chapter; i++)
                s += Length[i - 1];
            return s;
        }

        public static int WeekEnd(int chapter) => WeekStart(chapter) + Length[Mathf.Clamp(chapter, 1, Chapters) - 1] - 1;

        public static int WeeksIn(int chapter) => Length[Mathf.Clamp(chapter, 1, Chapters) - 1];

        public static int ChapterOf(int week)
        {
            week = Mathf.Clamp(week, 1, Weeks);
            for (int c = 1; c <= Chapters; c++)
                if (week >= WeekStart(c) && week <= WeekEnd(c))
                    return c;
            return Chapters;
        }

        /// 기존 5막(arc) 구조: 4챕터 = 1막. 컷씬·BGM 스템·볼륨 프로필은 막 단위 자산.
        public static int ArcOf(int chapter) => Mathf.Clamp((Mathf.Clamp(chapter, 1, Chapters) - 1) / 4 + 1, 1, 5);
        public static bool IsArcEnd(int chapter) => chapter % 4 == 0;
    }
}
