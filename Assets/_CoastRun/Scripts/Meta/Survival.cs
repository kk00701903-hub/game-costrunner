using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 55차(사용자): 육성 **생존 생태계**(다마고치) — 재료를 사서 방에서 조리 → 「밥」으로 먹고, 옷·잠·컨디션을 관리한다.
    ///   한 주(행동 3번)가 끝날 때 WeekTick 이 소비·악화를 계산하고 WeekPassUI 가 그 결과를 보여 준다.
    public static class Survival
    {
        public const int ClothesWeeks = 12;
        public const int RicePrice = 60, SidePrice = 40, ClothesPrice = 300; // 레거시 상수(새 단가는 LifeItems)
        public const int DangerWeeksToDie = 2;
        /// 74차: 한 주를 살면 그냥 쌓이는 피로. 다 잘 챙긴 주에만 겨우 -1 이 되고(공짜 회복을 없앤다),
        ///   굶고 못 자고 옷까지 낡으면 +40 까지 오른다.
        public const int WeeklyStressBase = 8;

        public class WeekReport
        {
            public bool ateRice, ateSide, slept, clothesWorn;
            public int hungerBefore, hungerAfter, condBefore, condAfter, riceLeft, sideLeft, clothesLeft, harvested;
            public int stressBefore, stressAfter, stressLimit;   // 74차: 주간 스트레스 변화(결산 줄)
            public bool died;
            public bool goodMonth;   // 105차: 4주 연속 잘 살았다 — 다음 주 성장 +20%
            public string autoBuy;   // 105차: 정기 장보기로 산 것(없으면 null)
            public readonly List<string> lines = new List<string>();
            public readonly List<string> harvestNames = new List<string>();
        }

        public static WeekReport WeekTick(SaveData s)
        {
            var r = new WeekReport();
            if (s == null) return r;
            LifeItems.Ensure(s);
            r.hungerBefore = s.hunger; r.condBefore = s.condition; r.stressBefore = s.stats.stress;
            r.harvested = HomeData.WeeklyGrow(s);
            CoastRun.Village.VillageFarm.WeekTick(s, r);   // 150차: 마을 텃밭 9칸 성장·잡초·난로 장작
            HomeData.EnsurePots(s);
            for (int i = 0; i < s.pots.Length; i++)
            {
                var p = s.pots[i]; var sd = HomeData.Seed(p.seed);
                if (sd == null) continue;
                if (p.growth >= sd.weeks) r.harvestNames.Add(sd.Name);
            }

            // 식사: 「밥」으로 요리를 먹었으면 유지, 아니면 굶주림(재료만 있어도 안 먹힌 상태)
            r.ateRice = s.ateThisWeek;
            r.ateSide = s.ateThisWeek;
            if (s.ateThisWeek)
            {
                s.starveWeeks = 0;
                s.hunger = Mathf.Max(0, s.hunger - 18);
            }
            else
            {
                s.hunger -= 40;
                s.starveWeeks++;
                s.condition -= 5;
            }
            s.ateThisWeek = false;
            s.hunger = Mathf.Clamp(s.hunger, 0, 100);

            r.slept = s.restedThisWeek;
            if (s.restedThisWeek) s.sleepDebt = 0; else s.sleepDebt++;
            s.restedThisWeek = false;

            s.clothesWeeks = Mathf.Max(0, s.clothesWeeks - 1);
            r.clothesWorn = s.clothesWeeks <= 0;
            if (r.clothesWorn) { s.condition -= 8; s.stats.charm = Mathf.Max(0, s.stats.charm - 1); }

            // ── 74차(사용자: 스트레스가 너무 적게 쌓인다) 주간 스트레스 압력 ──
            //   다마고치처럼 「한 주 살았다는 것만으로 쌓이고, 잘 챙기면 내려간다」. 프메처럼 방치는 벌을 받는다.
            //   전엔 여기서 잠 부족 +10 만 있고 ScheduleJudge.WeeklyDecay 가 무조건 -5 를 줬다 → 사실상 안 쌓임.
            int ds = WeeklyStressBase;                                 // 기본 생활 피로
            if (r.slept) ds -= 5; else ds += 5;                        // 이번 주에 잤나(밥/휴식 칸)
            if (s.sleepDebt >= 2) ds += 10;                            // 이틀 이상 밀린 잠
            if (s.hunger >= 60) ds -= 2; else if (s.hunger < 30) ds += 8;
            if (r.clothesWorn) ds += 4;                                // 낡은 옷 = 밖에 나가기 싫다
            if (s.condition >= 70) ds -= 2; else if (s.condition < 30) ds += 5;
            if (s.stats.Burnout && ds < 0) ds = 0;                     // 번아웃이면 그냥 쉬는 것만으로는 안 풀린다
            s.stats.stress = Mathf.Clamp(s.stats.stress + ds, 0, PlayerStats.StressMax);
            if (s.sleepDebt >= 2) s.condition -= 15;

            // 스트레스가 몸을 갉아먹는다 — 지침부터 컨디션, 번아웃부터 체력까지.
            var stage = s.stats.Stage;
            if (stage >= StressStage.Worn) s.condition -= 6;
            if (stage >= StressStage.Burnout) { s.condition -= 8; s.stats.stamina = Mathf.Max(0, s.stats.stamina - 4); }
            if (stage >= StressStage.Crisis) s.stats.stamina = Mathf.Max(0, s.stats.stamina - 4);

            if (s.hunger >= 60) s.condition += 8;
            else if (s.hunger >= 30) s.condition -= 8;
            else s.condition -= 22;
            s.condition = Mathf.Clamp(s.condition, 0, 100);
            if (s.condition < 30) s.stats.stamina = Mathf.Max(0, s.stats.stamina - 3);

            if (s.condition <= 0) s.dangerWeeks++; else s.dangerWeeks = 0;
            // 74차: 위기(문턱+15↑) 스트레스로도 쓰러진다 — 2주 연속이면 병원행(컨디션 0 과 같은 취급).
            if (stage >= StressStage.Crisis) s.stressCrisisWeeks++; else s.stressCrisisWeeks = 0;
            r.died = s.dangerWeeks >= DangerWeeksToDie || s.starveWeeks >= 4 || s.stressCrisisWeeks >= DangerWeeksToDie;
            if (r.died) s.deaths++;

            // ── 105차(재미요소 P0-4): 생활 관리에 「상」 — 잘 먹고·잘 자고·옷 멀쩡하고·컨디션 50↑ 인 주가 4번 이어지면 「좋은 한 달」(다음 주 성장 +20%) ──
            s.goodMonthBonus = false;
            bool goodWeek = r.ateRice && r.slept && !r.clothesWorn && s.condition >= 50 && !r.died;
            s.goodWeeks = goodWeek ? s.goodWeeks + 1 : 0;
            if (s.goodWeeks >= 4) { s.goodWeeks = 0; s.goodMonthBonus = true; r.goodMonth = true; }
            // 105차: 정기 장보기 — 먹을 요리가 없으면 결산 때 흰밥 한 그릇을 자동으로 산다(돈 있을 때만)
            if (s.autoGrocery && !r.died && !LifeItems.HasEdible(s))
            {
                string buy = LifeItems.CanBuy(s, "dish_rice", 1) ? "dish_rice" : LifeItems.CanBuy(s, "dish_egg", 1) ? "dish_egg" : null;
                if (buy != null && LifeItems.Buy(s, buy, 1)) { var bd = LifeItems.Get(buy); r.autoBuy = bd.HasValue ? Loc.T(bd.Value.nameKo, bd.Value.nameEn) : buy; }
            }
            LifeItems.SyncLegacy(s);
            r.hungerAfter = s.hunger; r.condAfter = s.condition;
            r.stressAfter = s.stats.stress; r.stressLimit = s.stats.StressLimit;
            r.riceLeft = s.rice; r.sideLeft = s.sideDish; r.clothesLeft = s.clothesWeeks;

            int dishes = LifeItems.CountCat(s, LifeItemCat.BasicDish) + LifeItems.CountCat(s, LifeItemCat.PremiumDish);
            int ings = LifeItems.CountCat(s, LifeItemCat.Ingredient);
            r.lines.Add(r.ateRice
                ? Loc.T($"이번 주 식사함 · 남은 요리 {dishes} · 재료 {ings}", $"Ate this week · dishes {dishes} · ingredients {ings}")
                : Loc.T($"식사를 안 했다… 요리 {dishes} · 재료 {ings} (방에서 조리 후 「밥」)", $"No meal… dishes {dishes} · ingredients {ings} (cook then Feed)"));
            if (!r.ateRice && ings > 0 && dishes <= 0)
                r.lines.Add(Loc.T("재료만 있어 — 마이룸에서 조리해야 먹어", "Ingredients only — cook in My Room first"));
            if (r.harvested > 0)
            {
                string crops = r.harvestNames.Count > 0 ? string.Join(", ", r.harvestNames) : $"{r.harvested}";
                r.lines.Add(Loc.T($"텃밭에 다 자랐다: {crops} — 마이룸에서 수확", $"Garden ready: {crops} — harvest in My Room"));
            }
            if (r.goodMonth) r.lines.Add(Loc.T("좋은 한 달 — 다음 주 성장 +20%", "A good month — next week growth +20%"));
            if (r.autoBuy != null) r.lines.Add(Loc.T($"정기 장보기: {r.autoBuy}", $"Auto grocery: {r.autoBuy}"));
            r.lines.Add(r.slept ? Loc.T("잘 잤다", "Slept well") : Loc.T($"잠을 못 잤다 ({s.sleepDebt}주째)", $"No sleep ({s.sleepDebt} wk)"));
            r.lines.Add(r.clothesWorn ? Loc.T("옷이 낡아서 못 입겠다 — 새 옷을 사자", "Clothes worn out — buy new") : Loc.T($"옷 {s.clothesWeeks}주 남음", $"Clothes {s.clothesWeeks} wk left"));
            r.lines.Add(Loc.T($"배부름 {r.hungerBefore} → {s.hunger}  ·  컨디션 {r.condBefore} → {s.condition}", $"Fullness {r.hungerBefore} → {s.hunger}  ·  Condition {r.condBefore} → {s.condition}"));
            r.lines.Add(Loc.T($"스트레스 {r.stressBefore} → {r.stressAfter} / 한계 {r.stressLimit} ({StageName(s.stats.Stage)})",
                              $"Stress {r.stressBefore} → {r.stressAfter} / limit {r.stressLimit} ({StageName(s.stats.Stage)})"));
            if (!r.died && s.condition <= 0) r.lines.Add(Loc.T("!! 컨디션 0 — 한 주 더 이러면 쓰러진다", "!! Condition 0 — one more week and she collapses"));
            if (!r.died && s.stressCrisisWeeks >= 1) r.lines.Add(Loc.T("!! 스트레스 한계 — 한 주 더 이러면 쓰러진다", "!! Stress critical — one more week and she collapses"));
            return r;
        }

        /// 74차: 스트레스 구간 이름 — 결산·상태창·말풍선이 같은 말을 쓰게 한곳에서.
        public static string StageName(StressStage st)
        {
            switch (st)
            {
                case StressStage.Crisis: return Loc.T("위기", "critical");
                case StressStage.Burnout: return Loc.T("번아웃", "burnout");
                case StressStage.Worn: return Loc.T("지침", "worn out");
                case StressStage.Tired: return Loc.T("피곤", "tired");
                default: return Loc.T("평온", "calm");
            }
        }

        /// 밥/휴식 — 잠 표시. 배부름은 Eat()에서 오른다.
        public static void OnRestAction(SaveData s)
        {
            if (s == null) return;
            s.restedThisWeek = true;
        }

        public static bool BuyRice(SaveData s, int weeks = 1)
        {
            LifeItems.Ensure(s);
            return LifeItems.Buy(s, "ing_rice", weeks);
        }
        public static bool BuySide(SaveData s, int n = 1)
        {
            LifeItems.Ensure(s);
            return LifeItems.Buy(s, "ing_veg", n);
        }
        public static bool BuyClothes(SaveData s) => LifeItems.Buy(s, "clothes_set", 1);

        public static void Revive(SaveData s)
        {
            if (s == null) return;
            LifeItems.Ensure(s);
            s.stats.money /= 2;
            s.condition = 50; s.hunger = 50; s.dangerWeeks = 0; s.starveWeeks = 0; s.sleepDebt = 0;
            s.stressCrisisWeeks = 0; s.burnoutWeeks = 0;
            if (!LifeItems.HasEdible(s)) LifeItems.Add(s, "dish_rice", 1);
            if (s.clothesWeeks <= 0) s.clothesWeeks = 2;
            // 74차: 병원에서 나오면 스트레스는 「피곤」 아래로 — 남겨 두면 나온 주에 또 쓰러진다.
            s.stats.stress = Mathf.Min(s.stats.stress, PlayerStats.StressTired - 5);
            LifeItems.SyncLegacy(s);
        }

        public static string Summary(SaveData s)
        {
            if (s == null) return "";
            LifeItems.Ensure(s);
            int dishes = LifeItems.CountCat(s, LifeItemCat.BasicDish) + LifeItems.CountCat(s, LifeItemCat.PremiumDish);
            int ings = LifeItems.CountCat(s, LifeItemCat.Ingredient);
            return Loc.T($"요리 {dishes} · 재료 {ings} · 옷 {s.clothesWeeks}주 · 배부름 {s.hunger} · 컨디션 {s.condition}",
                         $"Meals {dishes} · Ing {ings} · Clothes {s.clothesWeeks}w · Full {s.hunger} · Cond {s.condition}");
        }

        public static string Warning(SaveData s)
        {
            if (s == null) return null;
            LifeItems.Ensure(s);
            if (s.condition <= 0) return Loc.T("컨디션 0! 다음 주에 쓰러진다 — 식사·잠·약", "Condition 0! Collapses next week");
            // 74차: 스트레스도 생활 띠에 뜬다 — 위기·번아웃은 굶는 것과 같은 급의 경고.
            var stg = s.stats.Stage;
            if (stg >= StressStage.Crisis) return Loc.T($"스트레스 {s.stats.stress}! 다음 주에 쓰러진다 — 놀기·쓰다듬기·바다 수영", $"Stress {s.stats.stress}! Collapses next week — play & rest");
            if (stg == StressStage.Burnout) return Loc.T($"번아웃 {s.stats.stress}/{s.stats.StressLimit} — 이번 주는 놀기로 풀자", $"Burnout {s.stats.stress}/{s.stats.StressLimit} — spend this week playing");
            if (!LifeItems.HasEdible(s) && LifeItems.CountCat(s, LifeItemCat.Ingredient) > 0)
                return Loc.T("재료만 있어 — 마이룸에서 조리", "Ingredients only — cook in My Room");
            if (!LifeItems.HasEdible(s)) return Loc.T("먹을 요리가 없어 — 장보기·조리", "No meals — shop & cook");
            if (s.hunger < 30) return Loc.T("배고파… 「밥」으로 식사하자", "Hungry… use Feed to eat");
            if (s.clothesWeeks <= 0) return Loc.T("옷이 낡았어 — 새 옷", "Clothes worn out");
            if (s.sleepDebt >= 1) return Loc.T("이번 주엔 꼭 자자(밥/휴식)", "Rest this week");
            if (s.condition < 30) return Loc.T("컨디션이 나빠 — 약·보양식", "Poor condition — medicine / stew");
            if (stg == StressStage.Worn) return Loc.T($"스트레스 {s.stats.stress} — 지쳐 간다, 놀기로 한 칸", $"Stress {s.stats.stress} — worn out, spend a slot playing");
            return null;
        }
    }
}
