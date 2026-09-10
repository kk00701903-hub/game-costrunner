using UnityEngine;

namespace CoastRun
{
    /// 챕터별 런 낮/밤 고정표.
    ///  1·20장 = 낮.
    ///  2~19장 = 낮 약 70% / 밤 약 30% (18장 중 밤 5장 ≈ 28%).
    /// 스토리·K-POP 런 모두 같은 스테이지 번호로 조회. 0 = 한낮, 0.9 = 밤(블루아워).
    public static class ChapterClock
    {
        public enum Time { Day, Night }

        /// 인덱스 = 챕터(1..20). true = 밤.
        /// 밤: 6(이호테우 밤 축제), 9(꺼진 초), 11(빗속의 등), 13(그 밤), 17.
        private static readonly bool[] NightByChapter =
        {
            false,
            false, // 1  낮
            false, // 2
            false, // 3
            false, // 4
            false, // 5
            true,  // 6  밤
            false, // 7
            false, // 8
            true,  // 9  밤
            false, // 10
            true,  // 11 밤
            false, // 12
            true,  // 13 밤
            false, // 14
            false, // 15
            false, // 16
            true,  // 17 밤
            false, // 18
            false, // 19
            false, // 20 낮
        };

        public static Time Of(int chapter)
        {
            int c = Mathf.Clamp(chapter, 1, 20);
            return NightByChapter[c] ? Time.Night : Time.Day;
        }

        public static bool IsNight(int chapter) => Of(chapter) == Time.Night;

        public static float StartT(int chapter) => IsNight(chapter) ? 0.90f : 0.05f;

        public static string Label(int chapter) =>
            IsNight(chapter) ? Loc.T("밤", "Night") : Loc.T("낮", "Day");
    }
}
