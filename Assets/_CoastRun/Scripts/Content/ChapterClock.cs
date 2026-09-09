using UnityEngine;

namespace CoastRun
{
    /// 38차: 챕터별 시각 고정표 — 런은 여기서 정한 시각으로 시작한다(랜덤 아님). 스토리 런은 그 시각에서 노을까지 저물고,
    /// K-POP 런(같은 스테이지 번호)도 같은 시각을 쓴다. 0 = 한낮, 0.3 = 오후, 0.55 = 노을 직전, 0.7 = 저녁, 0.9 = 밤(블루아워).
    public static class ChapterClock
    {
        public enum Time { Day, Afternoon, Dusk, Evening, Night }
        private static readonly Time[] Table =
        {
            // 봄 1~5
            Time.Day, Time.Day, Time.Afternoon, Time.Dusk, Time.Day,
            // 여름 6~10
            Time.Day, Time.Afternoon, Time.Evening, Time.Night, Time.Afternoon,
            // 가을 11~15
            Time.Day, Time.Dusk, Time.Night, Time.Day, Time.Dusk,
            // 겨울 16~20
            Time.Afternoon, Time.Night, Time.Day, Time.Dusk, Time.Night,
        };

        public static Time Of(int chapter) => Table[Mathf.Clamp(chapter, 1, Table.Length) - 1];

        public static float StartT(int chapter)
        {
            switch (Of(chapter))
            {
                case Time.Afternoon: return 0.30f;
                case Time.Dusk: return 0.55f;
                case Time.Evening: return 0.70f;
                case Time.Night: return 0.90f;
                default: return 0.05f;
            }
        }

        public static string Label(int chapter)
        {
            switch (Of(chapter))
            {
                case Time.Afternoon: return Loc.T("오후", "Afternoon");
                case Time.Dusk: return Loc.T("노을", "Dusk");
                case Time.Evening: return Loc.T("저녁", "Evening");
                case Time.Night: return Loc.T("밤", "Night");
                default: return Loc.T("낮", "Day");
            }
        }
    }
}
