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
    /// 스트레스·컨디션이 그 위에 얹힌다. 번아웃 문턱(PlayerStats.StressLimit)을 넘으면 실패율이 급증한다.
    /// 휴식은 판정 없이 항상 성공.
    public static class ScheduleJudge
    {
        public const float BaseChance = 0.72f;     // 스탯 = 난이도일 때
        public const float StatSlope = 0.004f;     // 스탯-난이도 1점당 ±0.4%
        /// 74차: 스트레스 0~100 척도 기준 — 문턱 아래에서도 스트레스 100당 -22%(비례).
        public const float MildStressCoef = 0.22f;
        /// 문턱을 넘은 뒤 추가 감소(문턱~100 을 꽉 채우면 -45%). 번아웃은 「그냥 실패하는 상태」가 된다.
        public const float BurnoutCoef = 0.45f;
        /// 컨디션 보정 — 50이 기준, 0이면 -12%, 100이면 +12%(밥·잠 관리가 성과로 이어지게).
        public const float ConditionCoef = 0.12f;
        public const float GreatBase = 0.06f;
        public const float GreatCharmCoef = 0.0012f;
        public const float FailStressMult = 1.5f;
        public const float GreatGainMult = 1.5f;
        public const float MinChance = 0.05f;
        public const float MaxChance = 0.97f;

        /// v3 생활 리듬 — 교체 가능한 정적 컨텍스트(GameManager가 Save.rhythm을 넣어 준다).
        public static LifeRhythm Rhythm = LifeRhythm.Normal;
        public static bool SnackOn;
        /// 74차: 컨디션(0~100)도 판정에 들어간다. GameManager 가 Save.condition 을 넣어 준다.
        public static int Condition = 50;
        /// 105차: 숙련 ★ 배율(1.0~1.4)·자동 모드(0.85)·「좋은 한 달」(1.2) — 성장·수입에 곱한다. GameManager/TamaRaisingUI 가 넣는다.
        public static float GainMul = 1f;
        public static float RhythmStaminaMul => Rhythm == LifeRhythm.Hard ? 1.3f : Rhythm == LifeRhythm.Easy ? 0.8f : 1f;
        public static float RhythmStressMul => (Rhythm == LifeRhythm.Hard ? 1.3f : Rhythm == LifeRhythm.Easy ? 0.7f : 1f) * (SnackOn ? 0.8f : 1f);
        public static float RhythmChanceAdd => Rhythm == LifeRhythm.Hard ? -0.03f : Rhythm == LifeRhythm.Easy ? 0.03f : 0f;

        public static float SuccessChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story || d.deterministic)
                return 1f;

            int stat = s.Get(d.primaryStat);
            float p = BaseChance + (stat - d.difficulty) * StatSlope;

            // 스트레스: 문턱(체력이 높으면 조금 더 버틴다) 아래는 비례 감소, 넘으면 급락.
            int limit = s.StressLimit;
            p -= MildStressCoef * (s.stress / (float)PlayerStats.StressMax);
            if (s.stress >= limit)
                p -= BurnoutCoef * ((s.stress - limit) / (float)Mathf.Max(1, PlayerStats.StressMax - limit));

            // 컨디션: 잘 먹고 잘 잔 주는 더 잘 된다.
            p += ConditionCoef * ((Mathf.Clamp(Condition, 0, 100) - 50) / 50f);

            p += RhythmChanceAdd;
            return Mathf.Clamp(p, MinChance, MaxChance);
        }

        public static float GreatChance(ScheduleDef d, PlayerStats s)
        {
            if (d == null || d.category == ScheduleCategory.Rest || d.category == ScheduleCategory.Story || d.deterministic)
                return 0f;
            float g = GreatBase + s.charm * GreatCharmCoef;
            // 74차: 지침(55↑)부터 이미 대성공이 잘 안 나오고, 번아웃이면 거의 없다.
            if (s.Burnout) g *= 0.20f;
            else if (s.Stage == StressStage.Worn) g *= 0.55f;
            else if (s.Stage == StressStage.Calm) g *= 1.25f;   // 푹 쉬고 하면 잘 터진다
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
            gain *= Mathf.Max(0.1f, GainMul);   // 105차: 숙련·자동·좋은 한 달
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
            else if (d.dStress < 0)
                // 74차: 스트레스를 「푸는」 놀이(산책·수영·라디오) — 실패하면 덜 풀린다.
                //   실패 배율(×1.5)이나 간식 배율(×0.8)을 그대로 곱하면 실패가 더 시원해지는 역전이 생긴다.
                after.stress += Mathf.RoundToInt(d.dStress * (o == Outcome.Fail ? 0.5f : 1f) * seasonMul);
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
            // 평판 50 이상: 알바 스트레스 -4(단골 대우) — 74차: 알바 스트레스가 1.5배가 되었으니 혜택도 같이.
            if (d.category == ScheduleCategory.Job && before.trust >= 50) after.stress -= 4;

            after.Clamp();

            var log = new List<string>();
            switch (o)
            {
                case Outcome.GreatSuccess:
                    log.Add(Loc.T("★★★ 대성공 ★★★", "★★★ GREAT SUCCESS ★★★"));
                    log.Add(Loc.IsKo ? $"★ {d.Name}  (성공률 {pSuccess:P0})" : $"★ {d.Name}  ({pSuccess:P0})");
                    break;
                case Outcome.Success:
                    log.Add(d.deterministic ? (Loc.IsKo ? $"{d.Name} 수업 완료" : $"{d.Name} — lesson done") : (Loc.IsKo ? $"{d.Name} 성공  (성공률 {pSuccess:P0})" : $"{d.Name} — success  ({pSuccess:P0})"));
                    break;
                default:
                    log.Add(Loc.T("✕ 실패", "✕ FAIL"));
                    log.Add(Loc.IsKo ? $"{d.Name}…  (성공률 {pSuccess:P0})" : $"{d.Name}…  ({pSuccess:P0})");
                    break;
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
            if (after.Burnout) log.Add(Loc.T($"⚠ 번아웃 — 스트레스 {after.stress}/{after.StressLimit}. 이대로면 앓아눕는다.", $"⚠ BURNOUT — stress {after.stress}/{after.StressLimit}. Rest now."));
            else if (after.Stage == StressStage.Worn) log.Add(Loc.T($"스트레스 {after.stress} — 지쳐 간다(성공률·대성공 ↓).", $"Stress {after.stress} — wearing down (success ↓)."));

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

        /// 주말 정산. 74차: 무조건 -5 하던 스트레스 자연 회복은 없앴다 —
        ///   이제 스트레스의 주간 증감은 생활(잠·식사·옷)에 달렸고 `Survival.WeekTick` 이 한곳에서 계산한다.
        ///   여기 남은 것은 간식비뿐(간식은 ScheduleJudge.RhythmStressMul 로 행동 스트레스를 깎아 준다).
        public static void WeeklyDecay(PlayerStats s)
        {
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
            {
                // 74차: 경고에도 대가를 붙인다 — 컨디션이 깎여 다음 주 성공률까지 떨어진다.
                save.condition = Mathf.Max(0, save.condition - 10);
                s.stamina = Mathf.Max(0, s.stamina - 2);
                return Loc.T($"지쳤다(스트레스 {s.stress}). 이대로 한 주 더 가면 앓아눕는다. (컨디션 -10)",
                             $"Exhausted (stress {s.stress}). One more week and you'll fall ill. (Condition -10)");
            }
            if (save.burnoutWeeks == 2)
            {
                // 앓아눕기: 다음 주 전부 강제 휴식, 약값, 스트레스 회복(다 지워 주지는 않는다)
                save.sickWeeks++;
                s.money = Mathf.Max(0, s.money - 60);
                s.stress = Mathf.Max(0, s.stress - 35);
                s.stamina = Mathf.Max(0, s.stamina - 5);
                save.condition = Mathf.Max(0, save.condition - 15);
                for (int i = 0; i < save.queuedSchedule.Length; i++) save.queuedSchedule[i] = "rest_home";
                s.Clamp();
                return Loc.T("열이 났다. 이번 주는 꼼짝 못 하고 누워 있었다. (약값 -60G, 체력 -5, 스트레스 -35)",
                             "Fever. Bedridden all week. (medicine -60G, stamina -5, stress -35)");
            }
            // 3주+: 잠수 — 평판 -10, 말썽 +5, 이번 챕터 노을을 놓친다(자동 C급).
            //   74차: 스트레스를 0으로 지워 주면 「일부러 번아웃」이 이득이 된다 → -50 만.
            s.stress = Mathf.Max(0, s.stress - 50);
            s.trust -= 10;
            s.trouble += 5;
            save.burnoutWeeks = 0;
            save.forfeitPending = true;
            s.Clamp();
            return Loc.T("한동안 아무도 만나지 않았다. 노을을 놓쳤다. (평판 -10, 이번 챕터 C급)", "You disappeared for a while and missed the sunset. (Trust -10, chapter graded C)");
        }
    }
}
