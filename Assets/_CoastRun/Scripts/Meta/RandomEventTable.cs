using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 육성 화면 복귀 시 30% 확률로 뜨는 돌발 이벤트. 플레이어가 A/B 중 고른다.
    [System.Serializable]
    public class RandomEventDef
    {
        public string id;
        public string title;
        public string body;          // 선택 A 본문
        public string altBody;       // 선택 B 본문
        public string choiceA = "그래";   // 버튼 라벨 A
        public string choiceB = "아니";   // 버튼 라벨 B
        public float weight = 1f;
        public bool hasSeason;
        public SeasonKind season;
        /// 74차: 스트레스가 이 값 이상일 때만 뜨는 사건(프메의 반항·가출 계열). 0 이면 항상 후보.
        public int condStressMin;
        // 105차(재미요소 P1-1): condStat/condMin 은 이제 **선택 A 의 스탯 체크** — 모자라면 A 버튼이 잠기고 「N 더」가 뜬다(Long Live the Queen 식). condMoneyBelow 는 잠금에 안 씀.
        public StatKind condStat = StatKind.None;
        public int condMin;
        public bool condMoneyBelow;
        public int dStamina, dMoney, dHearts, dStress;
        public int altStamina, altMoney, altHearts, altStress;

        public bool Eval(PlayerStats s)
        {
            if (condMoneyBelow) return s.money < condMin * EconomyScale.Living;
            if (condStat == StatKind.None) return true;
            return s.Get(condStat) >= condMin;
        }
        /// 105차: 선택 A 에 스탯 체크가 걸려 있는가.
        public bool HasStatCheck => !condMoneyBelow && condStat != StatKind.None && condMin > 0;
        /// 105차: 체크 통과 여부 / 모자란 만큼.
        public bool CheckPasses(PlayerStats s) => !HasStatCheck || (s != null && s.Get(condStat) >= condMin);
        public int CheckShort(PlayerStats s) => HasStatCheck && s != null ? Mathf.Max(0, condMin - s.Get(condStat)) : 0;

        public string ChoiceALabel => Loc.T(choiceA, choiceA);
        public string ChoiceBLabel => Loc.T(choiceB, choiceB);
    }

    public struct RandomEventResult
    {
        public RandomEventDef def;
        public bool conditionMet;   // true = chose A
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
            // 본편 v4 톤: 하늘 / 도윤 / 꼬마. 164차: 돌발 이벤트 본문은 17장 반전 전에도 뜨므로 「도윤」 이름 대신 「그 애」(대본 규칙: 이름은 CS7 뒤에). 선택 A = 적극 / B = 소극·다른 길.
            _all = new List<RandomEventDef>
            {
                new RandomEventDef { id = "ev_orange", title = "이웃의 귤 선물", weight = 1.2f, hasSeason = true, season = SeasonKind.Autumn,
                    body = "옆집 삼춘이 귤 한 봉지를 건넸다. \"먹으멍 힘내라.\" 달콤해서 기운이 났다.",
                    altBody = "예의를 차리며 사양했다. 삼춘은 아쉽다는 듯 웃었다.",
                    choiceA = "감사히 받는다", choiceB = "사양한다",
                    dStamina = 2, dStress = -5, altStress = -1 },
                new RandomEventDef { id = "ev_radio", title = "주파수 질문", weight = 1.0f, condStat = StatKind.Charm, condMin = 40,
                    body = "꼬마가 고장 난 라디오를 가리켰다. \"몇이야?\" 89.2라고 했다. 눈이 동그래졌다.",
                    altBody = "라디오 주파수를 물었는데 얼버무렸다. 꼬마가 입을 다물었다.",
                    choiceA = "89.2라고 말한다", choiceB = "얼버무린다",
                    dHearts = 2, altHearts = 0, altStress = 2 },
                new RandomEventDef { id = "ev_rain", title = "오름에서 소나기", weight = 1.0f, condStat = StatKind.Stamina, condMin = 35,
                    body = "갑자기 소나기. 뛰어서 비를 맞으며 내려왔다 — 상쾌하지만 지친다.",
                    altBody = "바위에서 잠깐 비를 피했다. 옷은 덜 젖었지만 시간이 좀 갔다.",
                    choiceA = "뛰어 내려간다", choiceB = "비를 피한다",
                    dStamina = -2, dStress = 4, altStamina = -1, altStress = 1 },
                new RandomEventDef { id = "ev_money", title = "길에서 주운 돈", weight = 0.8f,
                    body = "해안도로 벤치 밑에서 지폐를 주웠다. 주인을 못 찾아 일단 챙겼다.",
                    altBody = "주인을 찾아 파출소에 맡겼다. 마음은 편해졌다.",
                    choiceA = "일단 챙긴다", choiceB = "맡긴다",
                    dMoney = 30, altStress = -3, altHearts = 1 },
                new RandomEventDef { id = "ev_scooter", title = "스쿠터 펑크", weight = 0.8f, condMoneyBelow = true, condMin = 40,
                    body = "스쿠터 타이어가 터졌다. 수리비를 아끼려 끌고 왔다… 어깨가 아프다.",
                    altBody = "스쿠터 타이어가 터졌다. 수리비를 냈다.",
                    choiceA = "끌고 간다", choiceB = "수리한다",
                    dStress = 10, altMoney = -40 },
                new RandomEventDef { id = "ev_tower", title = "송전탑 아래서", weight = 0.9f, condStat = StatKind.Stamina, condMin = 45,
                    body = "송전탑까지 뛰어 올라갔더니 그 애가 멀리 서 있었다. 우유 병 두 개. 같이 바다를 봤다.",
                    altBody = "숨이 차서 중간에 앉았다. 멀리 송전탑만 바라봤다.",
                    choiceA = "끝까지 오른다", choiceB = "중간에 쉰다",
                    dHearts = 2, dStress = -3, altStress = 3 },
                new RandomEventDef { id = "ev_hospital", title = "약 먹는 뒷모습", weight = 0.9f,
                    body = "정류장에서 약을 삼키는 그 애를 봤다. 말없이 옆에 섰다. 바람이 찼다.",
                    altBody = "멀리서 배웅만 했다. 혼자 병을 여는 등이 작아 보였다.",
                    choiceA = "옆에 선다", choiceB = "멀리서 본다",
                    dHearts = 1, dStress = 2, altHearts = 0, altStress = 1 },
                new RandomEventDef { id = "ev_snow", title = "첫눈", weight = 1.0f, hasSeason = true, season = SeasonKind.Winter, condStat = StatKind.Sense, condMin = 25,
                    body = "첫눈. 꼬마가 우비 모자를 젖히고 웃었다. 성에 창에 얼굴이 둘 그려져 있었다.",
                    altBody = "창문으로만 첫눈을 봤다. 나가기엔 너무 추웠다.",
                    choiceA = "밖에 나간다", choiceB = "창문으로 본다",
                    dHearts = 1, dStress = -4, altStress = -1 },
                new RandomEventDef { id = "ev_sea", title = "여름 바다", weight = 1.0f, hasSeason = true, season = SeasonKind.Summer, condStat = StatKind.Stamina, condMin = 40,
                    body = "갯바위에 발만 담갔다. 물때가 빠지며 짠내가 났다. 피로가 풀렸다.",
                    altBody = "바다를 보기만 하고 돌아왔다. 발은 안 적셨다.",
                    choiceA = "발을 담근다", choiceB = "구경만",
                    dStress = -8, dStamina = 1, altStress = -2 },
                new RandomEventDef { id = "ev_yuchae", title = "유채꽃밭", weight = 1.0f, hasSeason = true, season = SeasonKind.Spring, condStat = StatKind.Charm, condMin = 30,
                    body = "유채꽃밭에서 관광객이 사진을 부탁했다. 찍어주고 귤 하나 받았다.",
                    altBody = "바쁘다고 손을 흔들며 지나쳤다.",
                    choiceA = "사진을 찍어준다", choiceB = "그냥 지나간다",
                    dStress = -3, dMoney = 5, altStress = 0 },
                new RandomEventDef { id = "ev_milk", title = "가게 앞 우유", weight = 1.1f,
                    body = "주인 할머니가 \"오늘도 두 개라?\" 물었다. 그 애가 짧게 대답하고 나갔다. 따라가진 않았다.",
                    altBody = "그 애가 우유를 집는 걸 보고 발걸음을 돌렸다. 가슴이 뛰었다.",
                    choiceA = "자리를 지킨다", choiceB = "발길을 돌린다",
                    dHearts = 1, dStress = -2, altStress = 2 },
                new RandomEventDef { id = "ev_name", title = "바다누나", weight = 1.0f, condStat = StatKind.Sense, condMin = 35,
                    body = "꼬마가 또 「바다누나」라고 불렀다. 촌스럽다고 했지만 가슴이 저렸다.",
                    altBody = "이름을 묻자 입을 다물었다. 「말하면 다른 사람이 돼.」",
                    choiceA = "그대로 받아 준다", choiceB = "진짜 이름을 묻는다",
                    dHearts = 1, dStress = -2, altStress = 1 },
                // ── 109차(사용자: 「스토리 모드의 선택지 더 늘려줘」): 사건 8개 추가 — 꼬마·아줌마·마을 일상. 그림은 UI_Ev_<id>(아직 없으면 글 배치).
                new RandomEventDef { id = "ev_kite", title = "돌담 위의 연", weight = 1.0f, hasSeason = true, season = SeasonKind.Spring, condStat = StatKind.Agility, condMin = 30,
                    body = "꼬마의 연이 돌담 꼭대기에 걸렸다. 돌담을 타고 올라가 연을 빼 줬다. 꼬마가 손뼉을 쳤다.",
                    altBody = "긴 막대기로 연을 건드렸다. 줄이 조금 찢어졌지만 내려왔다.",
                    choiceA = "돌담에 올라간다", choiceB = "막대기로 건드린다",
                    dHearts = 2, dStress = -3, altStress = -1 },
                new RandomEventDef { id = "ev_busradio", title = "정류장의 라디오", weight = 0.9f, condStat = StatKind.Sense, condMin = 30,
                    body = "정류장 할머니의 라디오가 지직거렸다. 다이얼을 돌려 89.2에 맞춰 드렸다. 할머니가 귤을 하나 줬다.",
                    altBody = "지직거리는 소리를 같이 들었다. 할머니는 그게 더 편하다고 했다.",
                    choiceA = "다이얼을 맞춘다", choiceB = "그냥 같이 듣는다",
                    dHearts = 1, dMoney = 10, dStress = -2, altStress = -2 },
                new RandomEventDef { id = "ev_eggroll", title = "밥상 위 계란말이", weight = 1.1f,
                    body = "아줌마가 계란말이를 다섯 개 부쳤다. 다 먹었다. 아줌마가 빈 접시를 보고 웃었다.",
                    altBody = "반을 남겨 꼬마 손에 쥐여 줬다. 꼬마가 우비 주머니에 넣었다.",
                    choiceA = "다 먹는다", choiceB = "반은 꼬마에게",
                    dStamina = 2, dStress = -3, altHearts = 2, altStress = -1 },
                new RandomEventDef { id = "ev_typhoon", title = "태풍 전날", weight = 1.0f, hasSeason = true, season = SeasonKind.Summer, condStat = StatKind.Stamina, condMin = 40,
                    body = "마을 사람들이 창에 신문지를 붙이고 배를 끌어올렸다. 하루 종일 도왔다. 삼춘이 일당을 쥐여 줬다.",
                    altBody = "집에서 창문을 닫고 바람 소리를 들었다. 꼬마가 옆에 앉아 있었다.",
                    choiceA = "마을을 돕는다", choiceB = "집에 있는다",
                    dMoney = 20, dStress = 3, dStamina = -1, altStress = -2, altHearts = 1 },
                new RandomEventDef { id = "ev_mailbox", title = "주소 없는 편지", weight = 0.9f, hasSeason = true, season = SeasonKind.Autumn, condStat = StatKind.Sense, condMin = 40,
                    body = "우체통 밑에 주소 없는 편지가 떨어져 있었다. 받는 사람 칸에 「하늘」. 우체통에 도로 넣었다.",
                    altBody = "편지를 주머니에 넣고 왔다. 밤에 봉투를 만지작거리기만 했다.",
                    choiceA = "우체통에 넣는다", choiceB = "가지고 온다",
                    dHearts = 1, dStress = -1, altStress = 2 },
                new RandomEventDef { id = "ev_puppy", title = "길 잃은 강아지", weight = 0.9f,
                    body = "젖은 강아지가 따라왔다. 마을회관까지 안고 갔다. 주인이 와서 고맙다고 했다.",
                    altBody = "모른 척 지나쳤다. 뒤에서 낑낑거리는 소리가 오래 들렸다.",
                    choiceA = "회관에 데려간다", choiceB = "지나친다",
                    dStamina = -1, dHearts = 1, dStress = -2, altStress = 2 },
                new RandomEventDef { id = "ev_stones", title = "무너진 돌 세 개", weight = 1.0f,
                    body = "탑 아래 돌 세 개가 무너져 있었다. 하나씩 다시 쌓았다. 꼬마가 맨 위 돌을 올렸다.",
                    altBody = "그대로 두고 내려왔다. 밤새 그 돌이 생각났다.",
                    choiceA = "다시 쌓는다", choiceB = "그대로 둔다",
                    dHearts = 1, dStress = -2, altStress = 1 },
                new RandomEventDef { id = "ev_studio", title = "오래된 사진관", weight = 0.8f, hasSeason = true, season = SeasonKind.Winter,
                    body = "읍내 사진관 앞. 꼬마가 유리창에 코를 박고 있었다. 둘이 사진 한 장을 찍었다. 꼬마는 끝까지 후드를 안 벗었다.",
                    altBody = "유리창 안 사진들만 구경하고 돌아왔다.",
                    choiceA = "사진을 찍는다", choiceB = "구경만 한다",
                    dMoney = -15, dHearts = 1, dStress = -3, altStress = 0 },
                // ── 74차: 스트레스가 쌓였을 때만 뜨는 사건 — 다마고치처럼 상태가 이야기로 터져 나온다.
                new RandomEventDef { id = "ev_cry", title = "새벽 세 시", weight = 2.0f, condStressMin = PlayerStats.StressWorn,
                    body = "이유도 없이 눈물이 났다. 베개에 얼굴을 묻고 울다 잠들었다. 아침엔 눈이 부었다.",
                    altBody = "참고 라디오를 켰다. 낯선 사연 하나가 밤을 데워 줬다.",
                    choiceA = "울다 잠든다", choiceB = "라디오를 켠다",
                    dStress = -12, dStamina = -1, altStress = -6, altHearts = 1 },
                new RandomEventDef { id = "ev_snap", title = "말이 먼저 나갔다", weight = 1.8f, condStressMin = 62, condStat = StatKind.Charm, condMin = 35,
                    body = "삼춘의 농담에 날카롭게 받아쳤다. 돌아서서 귤 한 봉지를 사 들고 사과하러 갔다.",
                    altBody = "모른 척 지나갔다. 며칠 동안 그 얼굴이 떠올랐다.",
                    choiceA = "사과하러 간다", choiceB = "모른 척한다",
                    dStress = -8, dMoney = -20, altStress = 8 },
                new RandomEventDef { id = "ev_runaway", title = "아무 버스나", weight = 2.2f, condStressMin = 72,
                    body = "번호도 안 보고 버스를 탔다. 종점 바다까지 갔다가 막차로 돌아왔다. 아무도 몰랐다.",
                    altBody = "정류장에 앉아 있다가 그냥 집으로 걸었다. 발이 무거웠다.",
                    choiceA = "종점까지 간다", choiceB = "돌아선다",
                    dStress = -25, dMoney = -30, dStamina = -2, altStress = -5 },
            };
        }

        /// stress: 스트레스 조건이 붙은 사건을 걸러 내기 위한 현재 값(기본 999 = 제한 없음).
        public static RandomEventDef Pick(SeasonKind season, double roll, int stress = 999)
        {
            Ensure();
            System.Func<RandomEventDef, bool> ok = e =>
                (!e.hasSeason || e.season == season) && stress >= e.condStressMin;
            float total = 0f;
            foreach (var e in _all)
                if (ok(e)) total += e.weight;
            if (total <= 0f) return _all[0];
            float r = (float)roll * total;
            foreach (var e in _all)
            {
                if (!ok(e)) continue;
                r -= e.weight;
                if (r <= 0f) return e;
            }
            return _all[0];
        }

        /// choice 0 = A (body + d*), choice 1 = B (altBody + alt*).
        public static RandomEventResult ApplyChoice(RandomEventDef ev, PlayerStats stats, int choice)
        {
            bool a = choice <= 0;
            var res = new RandomEventResult { def = ev, conditionMet = a };
            if (a)
            {
                res.dStamina = ev.dStamina;
                res.dMoney = ev.dMoney * EconomyScale.Living;   // 186차
                res.dHearts = ev.dHearts;
                res.dStress = ev.dStress;
            }
            else
            {
                res.dStamina = ev.altStamina;
                res.dMoney = ev.altMoney * EconomyScale.Living;
                res.dHearts = ev.altHearts;
                res.dStress = ev.altStress;
            }
            stats.stamina += res.dStamina;
            stats.money += res.dMoney;
            stats.hearts += res.dHearts;
            stats.stress += res.dStress;
            stats.Clamp();
            return res;
        }

        /// Legacy: auto-pick branch by Eval (unused by Tama choice UI).
        public static RandomEventResult Apply(RandomEventDef ev, PlayerStats stats) =>
            ApplyChoice(ev, stats, ev.Eval(stats) ? 0 : 1);
    }
}
