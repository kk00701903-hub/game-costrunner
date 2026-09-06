using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    public enum ScheduleCategory { Job = 0, SelfDev = 1, Rest = 2, Story = 3, Lesson = 4 }

    /// 스케줄 1종의 정의. 등가교환: 알바 = 돈·체력↑ / 매력·순발력↓, 자기계발 = 스탯↑ / 돈·스트레스↑,
    /// 휴식 = 스트레스↓만. 어떤 조합도 3스탯이 동시에 오르지 않는다.
    [System.Serializable]
    public class ScheduleDef
    {
        public string id;
        public string displayName;
        /// 언어팩 적용 이름(en 사전에 있으면 영어).
        public string Name => Loc.Data("sched." + id, displayName);
        public string place;
        public ScheduleCategory category;
        public StatKind primaryStat;
        public int difficulty;        // 0~100
        public int dStamina, dAgility, dCharm, dStress, dMoney;
        public int dSense, dTrust, dTrouble;   // v3
        public int condTrust;                  // 평판 이상이어야 카드가 열림
        public int condTroubleMax = 100;       // 말썽이 이 값 이상이면 잠김
        public int condStamina, condAgility;   // 스탯 조건
        public bool deterministic;             // 교육: 판정 없이 확정
        public int heartsOnGreat;     // 대성공 시 말랑이 하트
        public bool ngPlusOnly;        // 2회차부터
        public bool hasOnlySeason;
        public SeasonKind onlySeason;
        public bool hasBonusSeason;
        public SeasonKind bonusSeason;
        public float seasonBonus = 1.5f;
        public string icon;           // Icon_<name> (없으면 글리프)
        public string glyph = "●";

        public bool AvailableIn(SeasonKind season) => !hasOnlySeason || onlySeason == season;

        /// 잠긴 이유(없으면 null). 카드에 한 줄로 보여 준다.
        public string LockReason(PlayerStats s)
        {
            if (s == null) return null;
            if (s.trust < condTrust) return Loc.T($"평판 {condTrust} 필요", $"Trust {condTrust} needed");
            if (s.trouble >= condTroubleMax) return Loc.T("얘기가 돌았다…", "Word got around…");
            if (s.stamina < condStamina) return Loc.T($"체력 {condStamina} 필요", $"Stamina {condStamina} needed");
            if (s.agility < condAgility) return Loc.T($"순발력 {condAgility} 필요", $"Agility {condAgility} needed");
            if (category == ScheduleCategory.Lesson && s.money < -dMoney) return Loc.T("돈이 모자라", "Not enough money");
            return null;
        }
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

        /// 현재 회차(GameManager가 세팅). NG+ 전용 스케줄 노출용.
        public static int Playthrough = 1;

        public static List<ScheduleDef> ByCategory(ScheduleCategory cat, SeasonKind season)
        {
            Ensure();
            var list = new List<ScheduleDef>();
            foreach (var d in _all)
                if (d.category == cat && d.AvailableIn(season) && (!d.ngPlusOnly || Playthrough >= 2))
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
                // v3 추가 알바 (제주 특화)
                Job("job_salon", "미용실 보조", "세화", StatKind.Charm, 40, st: 0, ag: 1, ch: 3, stress: 12, money: 38, glyph: "미용", trust: 1, condTrust: 15),
                Job("job_market", "오일장 짐 나르기", "세화 오일장", StatKind.Stamina, 25, st: 2, ag: 1, ch: 0, stress: 10, money: 30, glyph: "장", trust: 1),
                Job("job_sashimi", "횟집 밤 서빙", "함덕", StatKind.Charm, 45, st: -1, ag: 1, ch: 2, stress: 18, money: 70, glyph: "밤", trust: -1, trouble: 4, condTroubleMax: 60),
                Job("job_night_delivery", "심야 배달", "제주시", StatKind.Agility, 55, st: -1, ag: 3, ch: 0, stress: 20, money: 80, glyph: "심야", trust: -1, trouble: 5, condAgility: 50),
                Job("job_hall", "마을회관 봉사", "마을회관", StatKind.Charm, 20, st: 0, ag: 0, ch: 1, stress: 6, money: 10, glyph: "봉사", sense: 1, trust: 3, trouble: -2),
                Job("job_dangsan", "본향당 준비", "본향당", StatKind.Sense, 30, st: 1, ag: 0, ch: 0, stress: 8, money: 15, glyph: "당", sense: 2, trust: 4, trouble: -3),
                Job("job_tower_watch", "송전탑 관리소 야간 순찰", "송전탑", StatKind.Stamina, 60, st: 2, ag: 1, ch: -2, stress: 22, money: 65, glyph: "순찰", sense: 2, condStamina: 60),
                Job("job_lighthouse", "목마등대 청소", "이호테우", StatKind.Stamina, 40, st: 2, ag: 0, ch: 0, stress: 14, money: 45, glyph: "등대", sense: 2, trust: 1),
                // ── NG+ 전용 (2회차부터) ──
                Ng(Job("job_tower_fix", "송전탑 정비 보조", "송전탑 관리소", StatKind.Agility, 55, st: 2, ag: 2, ch: 0, stress: 16, money: 70, glyph: "정비", sense: 2, trust: 2, condStamina: 40)),
                Ng(Job("job_dj_assist", "라디오 국 보조", "제주 방송국", StatKind.Sense, 45, st: 0, ag: 0, ch: 2, stress: 10, money: 50, glyph: "DJ", sense: 3, trust: 1, condTrust: 20)),
                // ── 교육 (확정·유료) ──
                Les("les_skate", "스케이트 트릭 교습", "해안도로", 60, st: 1, ag: 3, stress: 8, glyph: "트릭"),
                Les("les_gym", "체육관", "구좌 체육관", 50, st: 3, ag: 1, stress: 9, glyph: "체육"),
                Les("les_ham", "아마추어 무선 교실", "청소년센터", 70, sense: 3, ch: 1, stress: 5, glyph: "무선"),
                Les("les_photo", "사진·그림 교실", "문화의 집", 55, sense: 2, ch: 2, stress: 5, glyph: "사진"),
                Les("les_speech", "제주어 교실", "도서관", 45, ch: 3, trust: 1, stress: 6, glyph: "말"),
                Les("les_dance", "댄스 학원", "청소년센터", 55, ch: 2, ag: 1, st: 1, stress: 8, glyph: "학원"),
                Les("les_cook", "제주 요리 교실", "마을회관", 40, sense: 1, ch: 1, st: 1, stress: 4, glyph: "요리"),
                Les("les_swim", "해녀학교", "성산", 65, st: 2, ag: 2, sense: 1, stress: 10, glyph: "해녀", only: SeasonKind.Summer),
                // ── 자기계발 ──
                Dev("dev_oreum", "오름 산책", "다랑쉬오름", StatKind.Stamina, 20, st: 1, ag: 2, ch: 1, stress: 4, money: -5, glyph: "오름"),
                Dev("dev_skate", "스케이트 연습", "해안도로", StatKind.Agility, 40, st: 1, ag: 3, ch: 0, stress: 9, money: 0, glyph: "보드"),
                Dev("dev_dance", "댄스 연습", "청소년센터", StatKind.Charm, 40, st: -1, ag: 1, ch: 3, stress: 10, money: -10, glyph: "댄스"),
                Dev("dev_radio", "라디오 편지", "내 방", StatKind.Charm, 25, st: 0, ag: 0, ch: 2, stress: 3, money: 0, hearts: 2, glyph: "편지", sense: 2),
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

        private static ScheduleDef Ng(ScheduleDef d) { d.ngPlusOnly = true; return d; }

        private static ScheduleDef Job(string id, string name, string place, StatKind primary, int diff,
            int st, int ag, int ch, int stress, int money, SeasonKind? only = null, SeasonKind? bonus = null, string glyph = "●",
            int sense = 0, int trust = 0, int trouble = 0, int condTrust = 0, int condTroubleMax = 100, int condStamina = 0, int condAgility = 0)
        {
            var d = new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.Job, primaryStat = primary,
                difficulty = diff, dStamina = st, dAgility = ag, dCharm = ch, dStress = stress, dMoney = money, glyph = glyph,
                dSense = sense, dTrust = trust, dTrouble = trouble, condTrust = condTrust, condTroubleMax = condTroubleMax,
                condStamina = condStamina, condAgility = condAgility,
            };
            if (only.HasValue) { d.hasOnlySeason = true; d.onlySeason = only.Value; }
            if (bonus.HasValue) { d.hasBonusSeason = true; d.bonusSeason = bonus.Value; }
            return d;
        }

        private static ScheduleDef Dev(string id, string name, string place, StatKind primary, int diff,
            int st, int ag, int ch, int stress, int money, int hearts = 1, string glyph = "●", int sense = 0)
        {
            return new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.SelfDev, primaryStat = primary,
                difficulty = diff, dStamina = st, dAgility = ag, dCharm = ch, dStress = stress, dMoney = money,
                heartsOnGreat = hearts, glyph = glyph, dSense = sense,
            };
        }

        /// 교육: 돈을 내고 확정 상승(판정 없음). cost는 양수로 받아 dMoney 음수로.
        private static ScheduleDef Les(string id, string name, string place, int cost,
            int st = 0, int ag = 0, int ch = 0, int sense = 0, int trust = 0, int stress = 0, string glyph = "●", SeasonKind? only = null)
        {
            var d = new ScheduleDef
            {
                id = id, displayName = name, place = place, category = ScheduleCategory.Lesson, primaryStat = StatKind.None,
                difficulty = 0, dStamina = st, dAgility = ag, dCharm = ch, dSense = sense, dTrust = trust, dStress = stress,
                dMoney = -cost, glyph = glyph, deterministic = true, heartsOnGreat = 0,
            };
            if (only.HasValue) { d.hasOnlySeason = true; d.onlySeason = only.Value; }
            return d;
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
                case ScheduleCategory.Job: return Loc.T("알바", "Jobs");
                case ScheduleCategory.SelfDev: return Loc.T("연습", "Practice");
                case ScheduleCategory.Lesson: return Loc.T("교육", "Lessons");
                case ScheduleCategory.Rest: return Loc.T("휴식", "Rest");
                default: return Loc.T("스토리", "Story");
            }
        }
    }
}
