using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    public enum ScheduleCategory { Job = 0, SelfDev = 1, Rest = 2, Story = 3 }

    /// 스케줄 1종의 정의. 등가교환: 알바 = 돈·체력↑ / 매력·순발력↓, 자기계발 = 스탯↑ / 돈·스트레스↑,
    /// 휴식 = 스트레스↓만. 어떤 조합도 3스탯이 동시에 오르지 않는다.
    [System.Serializable]
    public class ScheduleDef
    {
        public string id;
        public string displayName;
        public string place;
        public ScheduleCategory category;
        public StatKind primaryStat;
        public int difficulty;        // 0~100
        public int dStamina, dAgility, dCharm, dStress, dMoney;
        public int heartsOnGreat;     // 대성공 시 말랑이 하트
        public bool hasOnlySeason;
        public SeasonKind onlySeason;
        public bool hasBonusSeason;
        public SeasonKind bonusSeason;
        public float seasonBonus = 1.5f;
        public string icon;           // Icon_<name> (없으면 글리프)
        public string glyph = "●";

        public bool AvailableIn(SeasonKind season) => !hasOnlySeason || onlySeason == season;
    }

    /// 코드 테이블. ScriptableObject로 뺄 필요가 생기면 이 리스트를 그대로 옮긴다.
    public static class ScheduleTable
    {
        public const string StoryId = "story";

        private static List<ScheduleDef> _all;
        private static Dictionary<string, ScheduleDef> _byId;

        public static IReadOnlyList<ScheduleDef> All
        {
            get { Ensure(); return _all; }
        }

        public static ScheduleDef Get(string id)
        {
            Ensure();
            return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var d) ? d : null;
        }

        public static List<ScheduleDef> ByCategory(ScheduleCategory cat, SeasonKind season)
        {
            Ensure();
            var list = new List<ScheduleDef>();
            foreach (var d in _all)
                if (d.category == cat && d.AvailableIn(season))
                    list.Add(d);
            return list;
        }

        private static void Ensure()
        {
            if (_all != null) return;
            _all = new List<ScheduleDef>
            {
                // ── 알바 ──
                Job("job_orange", "감귤 농장", "서귀포 귤밭", StatKind.Stamina, 30, st: 2, ag: -1, ch: -1, stress: 12, money: 40,
                    bonus: SeasonKind.Autumn, glyph: "귤"),
                Job("job_haenyeo", "해녀 삼촌 돕기", "성산 바다", StatKind.Stamina, 50, st: 3, ag: 0, ch: -2, stress: 16, money: 55,
                    only: SeasonKind.Summer, glyph: "해녀"),
                Job("job_cafe", "해변 카페", "월정리", StatKind.Charm, 35, st: -1, ag: 0, ch: 2, stress: 10, money: 35, glyph: "카페"),
                Job("job_delivery", "스쿠터 배달", "구좌읍", StatKind.Agility, 45, st: 0, ag: 2, ch: -1, stress: 14, money: 50, glyph: "배달"),
                // ── 자기계발 ──
                Dev("dev_oreum", "오름 산책", "다랑쉬오름", StatKind.Stamina, 20, st: 1, ag: 2, ch: 1, stress: 4, money: -5, glyph: "오름"),
                Dev("dev_skate", "스케이트 연습", "해안도로", StatKind.Agility, 40, st: 1, ag: 3, ch: 0, stress: 9, money: 0, glyph: "보드"),
                Dev("dev_dance", "댄스 연습", "청소년센터", StatKind.Charm, 40, st: -1, ag: 1, ch: 3, stress: 10, money: -10, glyph: "댄스"),
                Dev("dev_radio", "라디오 편지", "내 방", StatKind.Charm, 25, st: 0, ag: 0, ch: 2, stress: 3, money: 0, hearts: 2, glyph: "편지"),
                // ── 휴식 ──
                Rest("rest_home", "집에서 뒹굴기", "우리 집", stress: -25, st: 0, glyph: "집"),
                Rest("rest_sea", "바다 수영", "함덕 해변", stress: -18, st: 1, bonus: SeasonKind.Summer, glyph: "수영"),
                // ── 스토리 ──
                new ScheduleDef
                {
                    id = StoryId, displayName = "스토리 돌입", place = "송전탑 가는 길",
                    category = ScheduleCategory.Story, primaryStat = StatKind.None, glyph = "★",
                },
            };
            _byId = new Dictionary<string, ScheduleDef>();
            foreach (var d in _all) _byId[d.id] = d;
        }

        private static ScheduleDef Job(string id, string name, string place, StatKind primary, int diff,
            int st, int ag, int ch, int stress, int money, SeasonKind? only = null, SeasonKind? bonus = null, string glyph = "●")
        {
            var d = new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.Job, primaryStat = primary,
                difficulty = diff, dStamina = st, dAgility = ag, dCharm = ch, dStress = stress, dMoney = money, glyph = glyph,
            };
            if (only.HasValue) { d.hasOnlySeason = true; d.onlySeason = only.Value; }
            if (bonus.HasValue) { d.hasBonusSeason = true; d.bonusSeason = bonus.Value; }
            return d;
        }

        private static ScheduleDef Dev(string id, string name, string place, StatKind primary, int diff,
            int st, int ag, int ch, int stress, int money, int hearts = 1, string glyph = "●")
        {
            return new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.SelfDev, primaryStat = primary,
                difficulty = diff, dStamina = st, dAgility = ag, dCharm = ch, dStress = stress, dMoney = money,
                heartsOnGreat = hearts, glyph = glyph,
            };
        }

        private static ScheduleDef Rest(string id, string name, string place, int stress, int st, SeasonKind? bonus = null, string glyph = "●")
        {
            var d = new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.Rest, primaryStat = StatKind.None,
                dStress = stress, dStamina = st, glyph = glyph,
            };
            if (bonus.HasValue) { d.hasBonusSeason = true; d.bonusSeason = bonus.Value; d.seasonBonus = 1.33f; }
            return d;
        }

        public static string CategoryName(ScheduleCategory c)
        {
            switch (c)
            {
                case ScheduleCategory.Job: return "알바";
                case ScheduleCategory.SelfDev: return "자기계발";
                case ScheduleCategory.Rest: return "휴식";
                default: return "스토리";
            }
        }
    }
}
