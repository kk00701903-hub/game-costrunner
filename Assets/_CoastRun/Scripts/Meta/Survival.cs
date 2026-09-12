using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 55차(사용자): 육성 **생존 생태계**(다마고치) — 돈으로 쌀·반찬을 사야 하고, 옷은 3개월(12주)마다 새로 사야 한다.
    ///   텃밭(마이룸 화분)에서 채소를 길러 반찬으로 먹을 수 있다. 쌀이 없거나, 계속 잠을 안 자거나, 굶어서 컨디션이
    ///   바닥나면 결국 쓰러진다(죽음 → GameOverUI: 처음부터 / 병원).
    ///   한 주(행동 3번)가 끝날 때 WeekTick 이 소비·악화를 계산하고 WeekPassUI 가 그 결과를 보여 준다.
    public static class Survival
    {
        public const int ClothesWeeks = 12;            // 옷 한 벌 = 3개월
        public const int RicePrice = 60, SidePrice = 40, ClothesPrice = 300;
        public const int DangerWeeksToDie = 2;         // 컨디션 0 이 2주 이어지면 사망

        public class WeekReport
        {
            public bool ateRice, ateSide, slept, clothesWorn;
            public int hungerBefore, hungerAfter, condBefore, condAfter, riceLeft, sideLeft, clothesLeft, harvested;
            public bool died;
            public readonly List<string> lines = new List<string>();
        }

        /// 한 주 끝 — 식량 소비·허기·수면·옷·컨디션 갱신. 죽으면 report.died.
        public static WeekReport WeekTick(SaveData s)
        {
            var r = new WeekReport();
            if (s == null) return r;
            r.hungerBefore = s.hunger; r.condBefore = s.condition;
            // 텃밭: 주가 바뀌면 비가 와서 심어 둔 것이 한 단계 자란다
            r.harvested = HomeData.WeeklyGrow(s);
            // 쌀
            if (s.rice > 0) { s.rice--; r.ateRice = true; s.hunger += 35; s.starveWeeks = 0; }
            else { s.hunger -= 40; s.starveWeeks++; }
            // 반찬
            if (s.sideDish > 0) { s.sideDish--; r.ateSide = true; s.hunger += 10; s.condition += 5; }
            else s.condition -= 5;
            s.hunger = Mathf.Clamp(s.hunger, 0, 100);
            // 잠(밥/휴식 행동을 이번 주에 한 번이라도)
            r.slept = s.restedThisWeek;
            if (s.restedThisWeek) s.sleepDebt = 0; else s.sleepDebt++;
            s.restedThisWeek = false;
            if (s.sleepDebt >= 2) { s.condition -= 15; s.stats.stress = Mathf.Min(PlayerStats.StatMax, s.stats.stress + 10); }
            // 옷
            s.clothesWeeks = Mathf.Max(0, s.clothesWeeks - 1);
            r.clothesWorn = s.clothesWeeks <= 0;
            if (r.clothesWorn) { s.condition -= 8; s.stats.charm = Mathf.Max(0, s.stats.charm - 1); }
            // 허기 → 컨디션
            if (s.hunger >= 60) s.condition += 8;
            else if (s.hunger >= 30) s.condition -= 8;
            else s.condition -= 22;
            s.condition = Mathf.Clamp(s.condition, 0, 100);
            if (s.condition < 30) { s.stats.stamina = Mathf.Max(0, s.stats.stamina - 3); }
            // 죽음 판정
            if (s.condition <= 0) s.dangerWeeks++; else s.dangerWeeks = 0;
            r.died = s.dangerWeeks >= DangerWeeksToDie || s.starveWeeks >= 4;
            if (r.died) s.deaths++;
            r.hungerAfter = s.hunger; r.condAfter = s.condition; r.riceLeft = s.rice; r.sideLeft = s.sideDish; r.clothesLeft = s.clothesWeeks;
            // 요약 문장
            r.lines.Add(r.ateRice ? Loc.T($"쌀 1주분 먹음 · 남은 쌀 {s.rice}주분", $"Ate rice · {s.rice} wk left") : Loc.T("쌀이 없어 굶었다…", "No rice — went hungry…"));
            r.lines.Add(r.ateSide ? Loc.T($"반찬 먹음 · 남은 반찬 {s.sideDish}", $"Side dish · {s.sideDish} left") : Loc.T("반찬 없이 맨밥", "No side dish"));
            if (r.harvested > 0) r.lines.Add(Loc.T($"텃밭에서 수확 · 반찬 +{r.harvested}", $"Harvested · side +{r.harvested}"));
            r.lines.Add(r.slept ? Loc.T("잘 잤다", "Slept well") : Loc.T($"잠을 못 잤다 ({s.sleepDebt}주째)", $"No sleep ({s.sleepDebt} wk)"));
            r.lines.Add(r.clothesWorn ? Loc.T("옷이 낡아서 못 입겠다 — 새 옷을 사자", "Clothes worn out — buy new") : Loc.T($"옷 {s.clothesWeeks}주 남음", $"Clothes {s.clothesWeeks} wk left"));
            r.lines.Add(Loc.T($"배부름 {r.hungerBefore} → {s.hunger}  ·  컨디션 {r.condBefore} → {s.condition}", $"Fullness {r.hungerBefore} → {s.hunger}  ·  Condition {r.condBefore} → {s.condition}"));
            if (!r.died && s.condition <= 0) r.lines.Add(Loc.T("!! 컨디션 0 — 한 주 더 이러면 쓰러진다", "!! Condition 0 — one more week and she collapses"));
            return r;
        }

        /// 밥/휴식 행동 — 이번 주 잠을 잔 것으로, 쌀이 있으면 조금 더 배부르게.
        public static void OnRestAction(SaveData s)
        {
            if (s == null) return;
            s.restedThisWeek = true;
            if (s.rice > 0) s.hunger = Mathf.Min(100, s.hunger + 8);
        }

        public static bool BuyRice(SaveData s, int weeks = 1) { if (s == null || s.stats.money < RicePrice * weeks) return false; s.stats.money -= RicePrice * weeks; s.rice += weeks; return true; }
        public static bool BuySide(SaveData s, int n = 1) { if (s == null || s.stats.money < SidePrice * n) return false; s.stats.money -= SidePrice * n; s.sideDish += n; return true; }
        public static bool BuyClothes(SaveData s) { if (s == null || s.stats.money < ClothesPrice) return false; s.stats.money -= ClothesPrice; s.clothesWeeks = ClothesWeeks; return true; }

        /// 병원에서 깨어남(죽음 뒤 「이어서」): 돈 절반, 컨디션·배부름 회복, 쌀 1주분. 주차는 그대로.
        public static void Revive(SaveData s)
        {
            if (s == null) return;
            s.stats.money /= 2;
            s.condition = 50; s.hunger = 50; s.dangerWeeks = 0; s.starveWeeks = 0; s.sleepDebt = 0;
            if (s.rice <= 0) s.rice = 1;
            if (s.clothesWeeks <= 0) s.clothesWeeks = 2;
            s.stats.stress = Mathf.Max(0, s.stats.stress - 30);
        }

        /// 상태 한 줄(육성 화면 생활 알약).
        public static string Summary(SaveData s)
        {
            if (s == null) return "";
            return Loc.T($"쌀 {s.rice}주 · 반찬 {s.sideDish} · 옷 {s.clothesWeeks}주 · 배부름 {s.hunger} · 컨디션 {s.condition}",
                         $"Rice {s.rice}w · Side {s.sideDish} · Clothes {s.clothesWeeks}w · Full {s.hunger} · Cond {s.condition}");
        }
        /// 위험 경고(없으면 null).
        public static string Warning(SaveData s)
        {
            if (s == null) return null;
            if (s.condition <= 0) return Loc.T("컨디션 0! 다음 주에 쓰러진다 — 쌀·잠·옷", "Condition 0! Collapses next week");
            if (s.rice <= 0) return Loc.T("쌀이 없어 — 장보기", "No rice — go shopping");
            if (s.hunger < 30) return Loc.T("배고파… 밥을 먹자", "Hungry… eat");
            if (s.clothesWeeks <= 0) return Loc.T("옷이 낡았어 — 새 옷", "Clothes worn out");
            if (s.sleepDebt >= 1) return Loc.T("이번 주엔 꼭 자자(밥/휴식)", "Rest this week");
            if (s.condition < 30) return Loc.T("컨디션이 나빠", "Poor condition");
            return null;
        }
    }
}
