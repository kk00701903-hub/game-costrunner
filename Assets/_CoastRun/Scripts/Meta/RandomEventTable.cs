using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 육성 화면 복귀 시 30% 확률로 뜨는 돌발 이벤트. 스탯 조건에 따라 본문/보상이 갈린다.
    [System.Serializable]
    public class RandomEventDef
    {
        public string id;
        public string title;
        public string body;          // 조건 충족(또는 조건 없음)
        public string altBody;       // 조건 미충족
        public float weight = 1f;
        public bool hasSeason;
        public SeasonKind season;
        public StatKind condStat = StatKind.None;
        public int condMin;          // condStat >= condMin
        public bool condMoneyBelow;  // money < condMin (돈 조건용)
        public int dStamina, dMoney, dHearts, dStress;
        public int altMoney, altHearts, altStress;

        public bool Eval(PlayerStats s)
        {
            if (condMoneyBelow) return s.money < condMin;
            if (condStat == StatKind.None) return true;
            return s.Get(condStat) >= condMin;
        }
    }

    public struct RandomEventResult
    {
        public RandomEventDef def;
        public bool conditionMet;
        public int dStamina, dMoney, dHearts, dStress;
        public string Body => conditionMet || string.IsNullOrEmpty(def.altBody) ? def.body : def.altBody;
    }

    public static class RandomEventTable
    {
        public const float Chance = 0.30f;

        private static List<RandomEventDef> _all;

        public static IReadOnlyList<RandomEventDef> All
        {
            get { Ensure(); return _all; }
        }

        private static void Ensure()
        {
            if (_all != null) return;
            _all = new List<RandomEventDef>
            {
                new RandomEventDef { id = "ev_orange", title = "이웃의 귤 선물", weight = 1.2f, hasSeason = true, season = SeasonKind.Autumn,
                    body = "옆집 삼춘이 귤 한 봉지를 건넸다. \"먹으멍 힘내라.\"", dStamina = 2, dStress = -5 },
                new RandomEventDef { id = "ev_radio", title = "주파수 질문", weight = 1.0f, condStat = StatKind.Charm, condMin = 40,
                    body = "그 애가 \"너 듣는 라디오, 주파수 몇이야?\" 하고 물었다. 웃으면서 알려줬다.",
                    altBody = "그 애가 라디오 주파수를 물었는데 얼버무리고 말았다.", dHearts = 2, altHearts = 0, altStress = 2 },
                new RandomEventDef { id = "ev_rain", title = "오름에서 소나기", weight = 1.0f,
                    body = "갑자기 소나기. 흠뻑 젖어서 내려왔다.", dStamina = -2, dStress = 4 },
                new RandomEventDef { id = "ev_money", title = "길에서 주운 돈", weight = 0.8f,
                    body = "해안도로 벤치 밑에서 지폐를 주웠다. 주인을 못 찾아 일단 챙겼다.", dMoney = 30 },
                new RandomEventDef { id = "ev_scooter", title = "스쿠터 펑크", weight = 0.8f, condMoneyBelow = true, condMin = 40,
                    body = "스쿠터 타이어가 터졌다. 수리비가 없어서 끌고 왔다…", altBody = "스쿠터 타이어가 터졌다. 수리비를 냈다.",
                    dStress = 10, altMoney = -40 },
                new RandomEventDef { id = "ev_tower", title = "송전탑 아래서", weight = 0.9f, condStat = StatKind.Stamina, condMin = 45,
                    body = "송전탑까지 뛰어 올라갔더니 그 애가 먼저 와 있었다. 같이 바다를 봤다.",
                    altBody = "송전탑까지 올라가려다 숨이 차서 중간에 돌아왔다.", dHearts = 2, dStress = -3, altStress = 3 },
                new RandomEventDef { id = "ev_hospital", title = "병원 가는 날", weight = 0.9f,
                    body = "그 애가 병원 가는 날. 버스 정류장까지 같이 걸었다. 말이 별로 없었다.", dHearts = 1, dStress = 2 },
                new RandomEventDef { id = "ev_snow", title = "첫눈", weight = 1.0f, hasSeason = true, season = SeasonKind.Winter,
                    body = "첫눈. 그 애가 서울 눈이랑 다르다고 했다. 뭐가 다른지는 말 안 했다.", dHearts = 1, dStress = -4 },
                new RandomEventDef { id = "ev_sea", title = "여름 바다", weight = 1.0f, hasSeason = true, season = SeasonKind.Summer,
                    body = "함덕에서 발만 담갔다. 시원해서 피로가 풀렸다.", dStress = -8, dStamina = 1 },
                new RandomEventDef { id = "ev_yuchae", title = "유채꽃밭", weight = 1.0f, hasSeason = true, season = SeasonKind.Spring,
                    body = "유채꽃밭에서 관광객이 사진을 부탁했다. 찍어주고 귤 하나 받았다.", dStress = -3, dMoney = 5 },
            };
        }

        public static RandomEventDef Pick(SeasonKind season, double roll)
        {
            Ensure();
            float total = 0f;
            foreach (var e in _all)
                if (!e.hasSeason || e.season == season) total += e.weight;
            float r = (float)roll * total;
            foreach (var e in _all)
            {
                if (e.hasSeason && e.season != season) continue;
                r -= e.weight;
                if (r <= 0f) return e;
            }
            return _all[0];
        }

        public static RandomEventResult Apply(RandomEventDef ev, PlayerStats stats)
        {
            bool ok = ev.Eval(stats);
            var res = new RandomEventResult { def = ev, conditionMet = ok };
            res.dStamina = ev.dStamina;
            res.dMoney = ok ? ev.dMoney : ev.altMoney;
            res.dHearts = ok ? ev.dHearts : ev.altHearts;
            res.dStress = ok ? ev.dStress : ev.altStress;
            stats.stamina += res.dStamina;
            stats.money += res.dMoney;
            stats.hearts += res.dHearts;
            stats.stress += res.dStress;
            stats.Clamp();
            return res;
        }
    }
}
