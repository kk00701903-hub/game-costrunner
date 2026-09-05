using UnityEngine;

namespace CoastRun
{
    /// 챕터 S급 판정과 기록 덮어쓰기.
    /// heartsTarget = 런닝 하트 30 + (주 수 × 3페이즈 × 대성공 기대 하트 1) + 이벤트 상한 2.
    /// 3주 챕터 41 → S컷 37: 런닝만 완벽해도 S는 못 받고 육성 대성공을 곁들여야 한다.
    public static class ChapterGrading
    {
        public const float S_Ratio = 0.90f;
        public const float A_Ratio = 0.70f;
        public const float B_Ratio = 0.50f;

        public static int HeartTarget(int chapter) =>
            RunTuning.HeartsPerStage + Timeline.WeeksIn(chapter) * Timeline.PhasesPerWeek + 2;

        public static void InitRecords(SaveData save)
        {
            if (save.chapters == null || save.chapters.Length != Timeline.Chapters)
                save.chapters = new ChapterRecord[Timeline.Chapters];
            for (int c = 1; c <= Timeline.Chapters; c++)
            {
                save.chapters[c - 1] = new ChapterRecord
                {
                    chapter = c,
                    weekStart = Timeline.WeekStart(c),
                    weekEnd = Timeline.WeekEnd(c),
                    heartsTarget = HeartTarget(c),
                    grade = ChapterGrade.None,
                };
            }
            save.chapters[0].snapshotAtStart = save.stats.Clone();
        }

        public static ChapterGrade GradeOf(float ratio) =>
            ratio >= S_Ratio ? ChapterGrade.S
            : ratio >= A_Ratio ? ChapterGrade.A
            : ratio >= B_Ratio ? ChapterGrade.B
            : ChapterGrade.C;

        public static string GradeLabel(ChapterGrade g) => g == ChapterGrade.None ? "-" : g.ToString();

        public static Color GradeColor(ChapterGrade g)
        {
            switch (g)
            {
                case ChapterGrade.S: return new Color(1f, 0.80f, 0.25f);
                case ChapterGrade.A: return new Color(0.80f, 0.86f, 0.92f);
                case ChapterGrade.B: return new Color(0.80f, 0.55f, 0.35f);
                case ChapterGrade.C: return new Color(0.55f, 0.58f, 0.62f);
                default: return new Color(0.35f, 0.38f, 0.42f);
            }
        }

        /// 챕터 종료 시 호출. 재도전이면 더 좋은 결과일 때만 덮어쓴다. 반환: 이번 시도의 등급.
        public static ChapterGrade Settle(SaveData save, out bool improved)
        {
            var rec = save.CurrentChapter;
            improved = false;
            if (rec == null) return ChapterGrade.None;

            int earned = save.chapterHearts;
            if (rec.heartsTarget <= 0) rec.heartsTarget = HeartTarget(rec.chapter);
            var attempt = GradeOf(Mathf.Clamp01((float)earned / rec.heartsTarget));

            if (!rec.cleared || earned > rec.heartsEarned)
            {
                rec.heartsEarned = earned;
                rec.grade = attempt;
                improved = true;
            }
            rec.cleared = true;
            return attempt;
        }

        public static bool AllS(SaveData save)
        {
            if (save?.chapters == null) return false;
            foreach (var r in save.chapters)
                if (r == null || r.grade != ChapterGrade.S)
                    return false;
            return true;
        }

        public static int CountS(SaveData save)
        {
            int n = 0;
            if (save?.chapters == null) return 0;
            foreach (var r in save.chapters)
                if (r != null && r.grade == ChapterGrade.S) n++;
            return n;
        }
    }
}
