using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    public enum Outcome { Fail = 0, Success = 1, GreatSuccess = 2 }

    public struct PhaseResult
    {
        public ScheduleDef def;
        public Outcome outcome;
        public float successChance;
        public float greatChance;
        public PlayerStats before;
        public PlayerStats after;
        public string[] logLines;
        public int heartsGained;
    }

    /// 프린세스 메이커식 판정. 성공률은 주 스탯 vs 난이도로 시작하고,
    /// 스트레스가 체력을 넘는 순간(번아웃) 실패율이 급증한다. 휴식은 판정 없이 항상 성공.
    public static class ScheduleJudge
    {
        public const float BaseChance = 0.72f;     // 스탯 = 난이도일 때
        public const float StatSlope = 0.004f;     // 스탯-난이도 1점당 ±0.4%
        public const float MildStressCoef = 0.12f; // 스트레스 ≤ 체력: 최대 -12%
        public const float BurnoutCoef = 0.55f;    // 스트레스 > 체력: 초과비율 × 55% 추가 감소
        public const float GreatBase = 0.06f;
        public const float GreatCharmCoef = 0.0012f;
        public const float FailStressMult = 1.5f;
        public const float GreatGainMult = 1.5f;
        public const float MinChance = 0.05f;
        public const float MaxChance = 0.97f;

        public static float SuccessChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story)
                return 1f;

            int stat = s.Get(d.primaryStat);
            float p = BaseChance + (stat - d.difficulty) * StatSlope;

            float stamina = Mathf.Max(1f, s.stamina);
            if (s.stress <= s.stamina)
                p -= MildStressCoef * (s.stress / stamina);
            else
                p -= MildStressCoef + BurnoutCoef * ((s.stress - stamina) / stamina);

            return Mathf.Clamp(p, MinChance, MaxChance);
        }

        public static float GreatChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story)
                return 0f;
            float g = GreatBase + s.charm * GreatCharmCoef;
            if (s.Burnout) g *= 0.25f;
            return Mathf.Clamp(g, 0f, 0.30f);
        }

        public static PhaseResult Resolve(ScheduleDef d, PlayerStats stats, SeasonKind season, double roll)
        {
            var before = stats.Clone();
            var after = stats.Clone();
            float pSuccess = SuccessChance(d, stats);
            float pGreat = GreatChance(d, stats);

            Outcome o = roll < pGreat ? Outcome.GreatSuccess
                      : roll < pSuccess ? Outcome.Success
                      : Outcome.Fail;
            if (d.category == ScheduleCategory.Rest)
                o = Outcome.Success;

            float gain = o == Outcome.GreatSuccess ? GreatGainMult : o == Outcome.Success ? 1f : 0f;
            float seasonMul = d.hasBonusSeason && d.bonusSeason == season ? d.seasonBonus : 1f;

            after.stamina += Mathf.RoundToInt(d.dStamina * gain);
            after.agility += Mathf.RoundToInt(d.dAgility * gain);
            after.charm += Mathf.RoundToInt(d.dCharm * gain);
            after.money += Mathf.RoundToInt(d.dMoney * gain * seasonMul);

            if (d.category == ScheduleCategory.Rest)
                after.stress += Mathf.RoundToInt(d.dStress * seasonMul);     // 음수, 항상 적용
            else
                after.stress += Mathf.RoundToInt(d.dStress * (o == Outcome.Fail ? FailStressMult : 1f));

            int hearts = o == Outcome.GreatSuccess ? d.heartsOnGreat : 0;
            after.hearts += hearts;
            if (o == Outcome.Fail && d.category == ScheduleCategory.Job)
                after.charm -= 1;   // 실수로 혼남

            after.Clamp();

            var log = new List<string>();
            switch (o)
            {
                case Outcome.GreatSuccess: log.Add($"★ {d.displayName} 대성공!  (성공률 {pSuccess:P0})"); break;
                case Outcome.Success: log.Add($"{d.displayName} 성공  (성공률 {pSuccess:P0})"); break;
                default: log.Add($"{d.displayName} 실패…  (성공률 {pSuccess:P0})"); break;
            }
            if (seasonMul > 1f) log.Add($"  {Timeline.SeasonName(season)} 보너스 ×{seasonMul:0.##}");
            Delta(log, "체력", before.stamina, after.stamina);
            Delta(log, "순발력", before.agility, after.agility);
            Delta(log, "매력", before.charm, after.charm);
            Delta(log, "스트레스", before.stress, after.stress);
            Delta(log, "돈", before.money, after.money);
            Delta(log, "말랑이 하트", before.hearts, after.hearts);
            if (after.Burnout) log.Add("⚠ 스트레스가 체력을 넘었어. 휴식이 필요해.");

            return new PhaseResult
            {
                def = d, outcome = o, successChance = pSuccess, greatChance = pGreat,
                before = before, after = after, logLines = log.ToArray(), heartsGained = hearts,
            };
        }

        private static void Delta(List<string> log, string name, int a, int b)
        {
            if (a == b) return;
            int d = b - a;
            log.Add($"  {name} {a} → {b}  ({(d >= 0 ? "+" : "")}{d})");
        }

        /// 주말 자연 회복: 스트레스 -5, 번아웃이면 회복 없음.
        public static void WeeklyDecay(PlayerStats s)
        {
            if (!s.Burnout) s.stress = Mathf.Max(0, s.stress - 5);
        }
    }
}
