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

        /// v3 생활 리듬 — 교체 가능한 정적 컨텍스트(GameManager가 Save.rhythm을 넣어 준다).
        public static LifeRhythm Rhythm = LifeRhythm.Normal;
        public static bool SnackOn;
        public static float RhythmStaminaMul => Rhythm == LifeRhythm.Hard ? 1.3f : Rhythm == LifeRhythm.Easy ? 0.8f : 1f;
        public static float RhythmStressMul => (Rhythm == LifeRhythm.Hard ? 1.3f : Rhythm == LifeRhythm.Easy ? 0.7f : 1f) * (SnackOn ? 0.8f : 1f);
        public static float RhythmChanceAdd => Rhythm == LifeRhythm.Hard ? -0.03f : Rhythm == LifeRhythm.Easy ? 0.03f : 0f;

        public static float SuccessChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story || d.deterministic)
                return 1f;

            int stat = s.Get(d.primaryStat);
            float p = BaseChance + (stat - d.difficulty) * StatSlope;

            float stamina = Mathf.Max(1f, s.stamina);
            if (s.stress <= s.stamina)
                p -= MildStressCoef * (s.stress / stamina);
            else
                p -= MildStressCoef + BurnoutCoef * ((s.stress - stamina) / stamina);

            p += RhythmChanceAdd;
            return Mathf.Clamp(p, MinChance, MaxChance);
        }

        public static float GreatChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story || d.deterministic)
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
            if (d.category == ScheduleCategory.Rest || d.deterministic)
                o = Outcome.Success;

            float gain = o == Outcome.GreatSuccess ? GreatGainMult : o == Outcome.Success ? 1f : 0f;
            float seasonMul = d.hasBonusSeason && d.bonusSeason == season ? d.seasonBonus : 1f;

            // 체력 성장은 리듬 배율(빡세게 ×1.3 / 무리 안 함 ×0.8), 감소는 그대로.
            float stGain = d.dStamina > 0 ? gain * RhythmStaminaMul : gain;
            after.stamina += Mathf.RoundToInt(d.dStamina * stGain);
            after.agility += Mathf.RoundToInt(d.dAgility * gain);
            after.charm += Mathf.RoundToInt(d.dCharm * gain);
            after.sense += Mathf.RoundToInt(d.dSense * gain);
            after.trust += Mathf.RoundToInt(d.dTrust * gain);
            after.trouble += d.dTrouble;                       // 말썽은 성패 무관(밤에 간 것 자체)
            // 교육비는 성패와 무관하게 낸다; 알바 수입은 성공해야.
            after.money += d.deterministic ? d.dMoney : Mathf.RoundToInt(d.dMoney * gain * seasonMul);

            if (d.category == ScheduleCategory.Rest)
                after.stress += Mathf.RoundToInt(d.dStress * seasonMul);     // 음수, 항상 적용
            else
                after.stress += Mathf.RoundToInt(d.dStress * (o == Outcome.Fail ? FailStressMult : 1f) * RhythmStressMul);

            int hearts = o == Outcome.GreatSuccess ? d.heartsOnGreat : 0;
            after.hearts += hearts;
            if (o == Outcome.Fail && d.category == ScheduleCategory.Job)
            {
                after.charm -= 1;   // 실수로 혼남
                after.trust -= 1;   // 마을에 소문
                if (d.dTrouble > 0) after.trouble += 2;
            }
            // 평판 50 이상: 알바 스트레스 -2(단골 대우)
            if (d.category == ScheduleCategory.Job && before.trust >= 50) after.stress -= 2;

            after.Clamp();

            var log = new List<string>();
            switch (o)
            {
                case Outcome.GreatSuccess: log.Add(Loc.IsKo ? $"★ {d.Name} 대성공!  (성공률 {pSuccess:P0})" : $"★ {d.Name} — great success!  ({pSuccess:P0})"); break;
                case Outcome.Success: log.Add(d.deterministic ? (Loc.IsKo ? $"{d.Name} 수업 완료" : $"{d.Name} — lesson done") : (Loc.IsKo ? $"{d.Name} 성공  (성공률 {pSuccess:P0})" : $"{d.Name} — success  ({pSuccess:P0})")); break;
                default: log.Add(Loc.IsKo ? $"{d.Name} 실패…  (성공률 {pSuccess:P0})" : $"{d.Name} — failed…  ({pSuccess:P0})"); break;
            }
            if (seasonMul > 1f) log.Add(Loc.IsKo ? $"  {Timeline.SeasonName(season)} 보너스 ×{seasonMul:0.##}" : $"  {Timeline.SeasonName(season)} bonus ×{seasonMul:0.##}");
            Delta(log, Loc.T("체력", "Stamina"), before.stamina, after.stamina);
            Delta(log, Loc.T("순발력", "Agility"), before.agility, after.agility);
            Delta(log, Loc.T("매력", "Charm"), before.charm, after.charm);
            Delta(log, Loc.T("감성", "Sense"), before.sense, after.sense);
            Delta(log, Loc.T("평판", "Trust"), before.trust, after.trust);
            Delta(log, Loc.T("스트레스", "Stress"), before.stress, after.stress);
            Delta(log, Loc.T("돈", "Money"), before.money, after.money);
            Delta(log, Loc.T("말랑이 하트", "Hearts"), before.hearts, after.hearts);
            if (after.trouble > before.trouble && after.trouble >= 30 && before.trouble < 30) log.Add(Loc.T("  …요즘 밤에 자꾸 나간다고 누가 그러더라.", "  …someone said you've been out late a lot."));
            if (after.Burnout) log.Add(Loc.T("⚠ 스트레스가 체력을 넘었어. 휴식이 필요해.", "⚠ Stress is over stamina. Rest is needed."));

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

        /// 주말 자연 회복: 스트레스 -5, 번아웃이면 회복 없음. 간식비는 주 15G.
        public static void WeeklyDecay(PlayerStats s)
        {
            if (!s.Burnout) s.stress = Mathf.Max(0, s.stress - 5);
            if (SnackOn) s.money = Mathf.Max(0, s.money - 15);
        }

        /// v3 번아웃 단계. 주말에 호출: 연속 번아웃 주 수를 세고, 2주면 앓아눕기, 3주면 잠수.
        /// 반환: 이번 주말에 일어난 일(로그 문장), 없으면 null.
        public static string BurnoutStage(SaveData save)
        {
            var s = save.stats;
            if (!s.Burnout) { save.burnoutWeeks = 0; return null; }
            save.burnoutWeeks++;
            if (save.burnoutWeeks == 1)
                return Loc.T("지쳤다. 이대로 한 주 더 가면 앓아눕는다.", "Exhausted. One more week like this and you'll fall ill.");
            if (save.burnoutWeeks == 2)
            {
                // 앓아눕기: 다음 주 전부 강제 휴식, 약값, 스트레스 크게 회복
                save.sickWeeks++;
                s.money = Mathf.Max(0, s.money - 30);
                s.stress = Mathf.Max(0, s.stress - 40);
                for (int i = 0; i < save.queuedSchedule.Length; i++) save.queuedSchedule[i] = "rest_home";
                return Loc.T("열이 났다. 이번 주는 꼼짝 못 하고 누워 있었다. (약값 -30G, 스트레스 -40)", "Fever. Bedridden the whole week. (medicine -30G, stress -40)");
            }
            // 3주+: 잠수 — 스트레스 0, 평판 -10, 말썽 +5. (챕터 자동 C급은 다음 단계)
            s.stress = 0;
            s.trust -= 10;
            s.trouble += 5;
            save.burnoutWeeks = 0;
            s.Clamp();
            return Loc.T("한동안 아무도 만나지 않았다. 마을에 소문이 돌았다. (평판 -10)", "You disappeared for a while. The village talked. (Trust -10)");
        }
    }
}
