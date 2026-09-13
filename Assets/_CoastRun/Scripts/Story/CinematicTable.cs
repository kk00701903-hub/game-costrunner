using UnityEngine;

namespace CoastRun
{
    /// 68차: 시네마틱 대본 — 오프닝 + 컷씬 8편을 **같은 형식**(컷 = 클립/스틸 + 자막 한 줄, 켄번즈, 음악 한 곡, 마무리 카드)으로.
    ///   디렉터 규칙(69차 검수 반영): 자막은 하늘의 1인칭 한 문장, 32자 안팎 · 이름(도윤/바다)은 첫 등장 컷에서 바로 · 숫자엔 단위(4.2킬로) · 카드는 제목 + CHAPTER 범위(오프닝은 「그때」 과거형, 컷씬은 「지금」 현재형) · 컷 7~9개 · 편마다 끝 컷은 다음 러닝의 이유 ·
    ///   채도 곡선(오프닝 1.0 → 컷씬 1 0.85 … 8 0.25, 회상 컷은 세피아) · 색이 남는 건 하트·꼬마 우비·부표뿐.
    ///   72차: 54차 수채 원화(테두리·종이결이 있어 켄번즈 때 흰 가장자리가 비쳐 깜빡여 보였다)는 전부 Kling 신규(Cut_V_<옛이름>, 테두리 없는 풀블리드)로 교체. 옛 그림은 fallback 으로만. 클립은 StreamingAssets/Opening/<clip>.mp4(오프닝만).
    public static class CinematicTable
    {
        public class Cut
        {
            public string clip, still, fallback, caption, tag;
            public float dur; public Vector2 from, to; public bool sepia;
            public Cut(string clip, string still, string caption, float dur = 9f, bool sepia = false, string tag = null, string fallback = null, int kb = 0)
            {
                this.clip = clip; this.still = still; this.caption = caption; this.dur = dur; this.sepia = sepia; this.tag = tag; this.fallback = fallback;
                // 켄번즈 4패턴: 0 = 천천히 밀고 들어감, 1 = 왼→오, 2 = 오→왼, 3 = 빠져나옴
                switch (kb % 4)
                {
                    case 0: from = new Vector2(1.06f, 0f); to = new Vector2(1.16f, 0f); break;
                    case 1: from = new Vector2(1.14f, 0.03f); to = new Vector2(1.10f, -0.03f); break;
                    case 2: from = new Vector2(1.14f, -0.03f); to = new Vector2(1.10f, 0.03f); break;
                    default: from = new Vector2(1.18f, 0f); to = new Vector2(1.06f, 0f); break;
                }
            }
        }

        public class Def
        {
            public string id, title, bgm, cardMain, cardSub; public float sat; public Cut[] cuts;
            /// 오프닝처럼 카드 뒤 음악 끝까지 붙들지(초). 0 = 2.2초.
            public float holdToSeconds;
            public bool gameTitleCard;
        }

        public static Def Get(string id)
        {
            switch (id)
            {
                case "OPEN": return Opening;
                case "CS1": return CS1; case "CS2": return CS2; case "CS3": return CS3; case "CS4": return CS4;
                case "CS5": return CS5; case "CS6": return CS6; case "CS7": return CS7; case "CS8": return CS8;
                default: return null;
            }
        }
        public static Def Cutscene(int index) => Get("CS" + Mathf.Clamp(index, 1, 8));

        // ── 오프닝 (열두 살 → 약속 → 열아홉의 아침) · M5 돌아온 제주 1:37 ──
        private static readonly Def Opening = new Def
        {
            id = "OPEN", title = "너와 나의 주파수", bgm = "BGM_M5", sat = 1f, holdToSeconds = 97f, gameTitleCard = true,
            cardMain = "너와 나의 주파수", cardSub = "우리의 송전탑  ·  COAST RUN",
            cuts = new[]
            {
                new Cut("open_1",  "Cut_Open_1", "열두 살 봄, 송전탑 아래서 처음 만났다.", 10f, false, null, null, 0),
                new Cut("open_7",  "Cut_Open_4", "서울서 온 창백한 전학생, 도윤. 그 애가 먼저 손을 내밀었다.", 10f, false, null, null, 1),
                new Cut("open_8",  "Cut_Open_5", "유리구슬 한 알에 온 세상을 걸던 나이.", 10f, false, null, null, 2),
                new Cut("open_9",  "Cut_Open_6", "딱지를 다 잃어도 도윤이 웃으면 괜찮았다.", 10f, false, null, null, 1),
                new Cut("open_2",  "Cut_Open_2", "그 애는 늘 내 앞에 섰다. 한 번도 이유를 말하지 않고.", 10f, false, null, null, 0),
                new Cut("open_10", "Cut_V_Open_7", "말하지 못한 게 하나 있었다. 그때도, 지금도.", 10f, false, null, "Cut_Open_7", 2),
                new Cut("open_3",  "Cut_Open_3", "떠나던 날 그 애가 말했다. 스무 살 네 생일, 송전탑 밑에서.", 10f, false, null, null, 3),
                new Cut("open_4",  "Cut_END_A3", "열아홉의 어느 아침, 눈을 떴다. 아무것도 기억나지 않았다.", 10f, false, null, null, 0),
                new Cut("open_6",  "Cut_CS1_2", "달력엔 한 줄뿐. 스무 살 생일, 송전탑 아래. 1년 남았다.", 10f, false, null, null, 3),
            }
        };

        // ── 컷씬 1 「이름」 (CH1) · M1 ──
        private static readonly Def CS1 = new Def
        {
            id = "CS1", title = "이름", bgm = "BGM_M1", sat = 0.85f, cardMain = "이름", cardSub = "CHAPTER 1",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH01_Open", "눈을 떴다. 문가에 주황 우비를 입은 꼬마가 서 있었다.", 9f, false, null, "Cut_CH01_Open", 0),
                new Cut(null, "Cut_CS1_2", "달력엔 한 줄. 1년 뒤, 내 생일. 나머지 칸은 전부 하얗다.", 9f, false, null, "Cut_END_A3", 1),
                new Cut(null, "Cut_V_CH03_Open", "창밖의 마을. 누구도 나를 보지 못했다. 거울조차 흐렸다.", 9f, false, null, "Cut_CH03_Open", 2),
                new Cut(null, "Cut_V_CH17_Open", "이 꼬마만 나를 봤다. 이름이 없대서 「바다」라고 지어 줬다.", 9f, false, null, "Cut_CH17_Open", 1),
                new Cut(null, "Cut_V_CH06_Open", "버스는 안 왔다. 꼬마가 탑까지 걸어가자고 했다.", 9f, false, null, "Cut_CH06_Open", 0),
                new Cut(null, "Cut_V_CH19_Close", "문 앞에 보드 하나. 누가 갖다 놓았는지는 묻지 않았다.", 9f, false, null, "Cut_CH19_Close", 3),
                new Cut(null, "Cut_V_CH14_Close", "노을 속 탑 아래 우유 두 병. 하나는 늘 그대로였다. 왜일까.", 9f, false, null, "Cut_CH14_Close", 0),
            }
        };

        // ── 컷씬 2 「하트」 (CH2~4) · M6 ──
        private static readonly Def CS2 = new Def
        {
            id = "CS2", title = "하트", bgm = "BGM_M6", sat = 0.78f, cardMain = "하트", cardSub = "CHAPTER 2 ~ 4",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH02_Open", "빨랫줄에 해녀복 두 벌. 우편함엔 안 뜯은 편지 스무 통.", 9f, false, null, "Cut_CH02_Open", 1),
                new Cut(null, "Cut_V_CH09_Close", "바람이 편지를 채 갔다. 쫓다가, 처음으로 탑에 손이 닿았다.", 9f, false, null, "Cut_CH09_Close", 0),
                new Cut(null, "Cut_CS2_3", "숨비소리. 물 위로 내쉬는 엄마의 숨. 첫 기억이 돌아왔다.", 9f, true, "— 기억 —", "Cut_CH02_Close", 2),
                new Cut(null, "Cut_V_CH16_Close", "도윤. 그 애 이름만 돌아왔다. 불러도 걸음이 멈췄다 그냥 갔다.", 9f, false, null, "Cut_CH16_Close", 1),
                new Cut(null, "Cut_V_CH04_Open", "아빠가 그릴 줄 아는 건 하트 하나뿐이었다.", 9f, true, "— 열세 해 전 —", "Cut_CH04_Open", 0),
                new Cut(null, "Cut_V_CH13_Open", "한 마리만 더. 내 생일 전날 밤, 아빠는 주황 우비를 입고 나갔다.", 9f, true, "— 태풍 전날 밤 —", "Cut_CH13_Open", 2),
                new Cut(null, "Cut_V_CH13_Close", "다음 날은 잔인하게 맑았다. 아빠는 돌아오지 않았다.", 9f, false, null, "Cut_CH13_Close", 3),
            }
        };

        // ── 컷씬 3 「우리 기지」 (CH5~7) · M2 ──
        private static readonly Def CS3 = new Def
        {
            id = "CS3", title = "우리 기지", bgm = "BGM_M2", sat = 0.70f, cardMain = "우리 기지", cardSub = "CHAPTER 5 ~ 7",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH05_Open", "계란말이가 전부 하트였다. 뚜껑 안쪽엔 열두 살 글씨, 반은 네 거.", 9f, false, null, "Cut_CH05_Open", 0),
                new Cut(null, "Cut_V_CH18_Mid", "탑 아래 그가 앉아 있었다. 잡으려던 손이 그냥 지나갔다.", 9f, false, null, "Cut_CH18_Mid", 1),
                new Cut(null, "Cut_V_CH17_Open", "첫눈. 꼬마와 창가에서 성에에 얼굴 둘을 그리다 멈췄다.", 9f, false, null, "Cut_CH17_Open", 2),
                new Cut(null, "Cut_V_CH07_Open", "눈 위 발자국은 한 줄뿐. 꼬마 것만. 내 건 없었다.", 9f, false, null, "Cut_CH07_Open", 0),
                new Cut(null, "Cut_V_CH05_Close", "열두 살 봄이 돌아왔다. 그 애가 말했다. 반은 진짜야, 반은 거짓말.", 9f, true, "— 여덟 해 전 —", "Cut_CH05_Close", 1),
                new Cut(null, "Cut_V_Open_3", "정류장에서 탑까지 4.2킬로. 노을 전에 닿으면 내가 이기는 놀이.", 9f, true, "— 여덟 해 전 —", "Cut_Open_3", 3),
                new Cut(null, "Cut_V_CH14_Open", "그 애 말대로 하기로 했다. 지워지면 또 그린다. 내일도 탑까지.", 9f, false, null, "Cut_CH14_Open", 0),
            }
        };

        // ── 컷씬 4 「열두 개의 초」 (CH8~10) · M3 ──
        private static readonly Def CS4 = new Def
        {
            id = "CS4", title = "열두 개의 초", bgm = "BGM_M3", sat = 0.60f, cardMain = "열두 개의 초", cardSub = "CHAPTER 8 ~ 10",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH08_Open", "절벽 아래, 아빠 부표가 있던 바위. 새 페인트 냄새가 났다.", 9f, false, null, "Cut_CH08_Open", 2),
                new Cut(null, "Cut_V_CH09_Open", "3킬로에서 처음 주저앉았다. 돌아오니 부표의 하트가 지워져 있었다.", 9f, false, null, "Cut_CH09_Open", 0),
                new Cut(null, "Cut_CS4_6", "돌 위의 초가, 내가 다가가자 꺼졌다.", 9f, false, null, "Cut_CH13_Close", 3),
                new Cut(null, "Cut_V_CH10_Open", "열두 살 생일. 초 열두 개. 그 애는 몰랐다, 여기가 어떤 자리인지.", 9f, true, "— 여덟 해 전 —", "Cut_CH10_Open", 1),
                new Cut(null, "Cut_V_CH17_Close", "그 뒤 열흘, 그 애는 안 왔다. 도시락은 그대로 얼어 있었다.", 9f, true, "— 여덟 해 전 —", "Cut_CH17_Close", 2),
                new Cut(null, "Cut_V_CH12_Close", "그 집 아빠 죽은 자리라고들 했다. 열흘째, 그 애가 탑으로 뛰어왔다.", 9f, true, "— 여덟 해 전 —", "Cut_CH12_Close", 0),
                new Cut(null, "Cut_V_CH11_Open", "비가 시작됐다. 탑 아래엔 오늘도 아무도 없었다. 그래도 간다.", 9f, false, null, "Cut_CH11_Open", 3),
            }
        };

        // ── 컷씬 5 「그 밤」 (CH11~13) · M6 ──
        private static readonly Def CS5 = new Def
        {
            id = "CS5", title = "그 밤", bgm = "BGM_M6", sat = 0.50f, cardMain = "그 밤", cardSub = "CHAPTER 11 ~ 13",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH12_Open", "길가에 버려진 리어카 한 대. 왜 거기 있는지 그땐 몰랐다.", 9f, false, null, "Cut_CH12_Open", 1),
                new Cut(null, "Cut_CS5_2", "빗속, 우산도 없이 탑 아래 선 뒷모습. 뛰어가면 없었다.", 9f, false, null, "Cut_CH11_Open", 0),
                new Cut(null, "Cut_CS5_3", "트럭이 나를 그대로 통과했다. 그날부터 꼬마의 소매가 찢어져 있었다.", 9f, false, null, "Cut_CH17_Open", 2),
                new Cut(null, "Cut_CS5_4", "밤사이 누가 리어카 바퀴를 새로 끼웠다. 키 큰 사람이라고 했다.", 9f, false, null, "Cut_CH12_Open", 3),
                new Cut(null, "Cut_V_CH13_Open", "폭우의 밤. 마당에 엄마가 엎어져 있었다. 그 애가 먼저 봤다.", 9f, true, "— 여덟 해 전 —", "Cut_CH13_Open", 0),
                new Cut(null, "Cut_CS5_6", "병원까지 2킬로. 열두 살 둘이 리어카를 밀었다. 엄마를 태우고.", 9f, true, "— 여덟 해 전 —", "Cut_CH11_Open", 1),
                new Cut(null, "Cut_V_CH13_Close", "무서웠어? 응. 나도. 그 밤 우리가 나눈 말은 그게 전부였다.", 9f, false, null, "Cut_CH13_Close", 3),
            }
        };

        // ── 컷씬 6 「스무 살」 (CH14~15) · M4 ──
        private static readonly Def CS6 = new Def
        {
            id = "CS6", title = "스무 살", bgm = "BGM_M4", sat = 0.42f, cardMain = "스무 살", cardSub = "CHAPTER 14 ~ 15",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH20_Mid", "정류장에서 탑까지 4.2킬로. 오늘은 거기까지 못 갈 것 같다.", 9f, false, null, "Cut_CH20_Mid", 0),
                new Cut(null, "Cut_V_Open_7", "떠나던 날, 우리에게 주어진 시간은 5분이었다.", 9f, true, "— 여덟 해 전 —", "Cut_Open_7", 1),
                new Cut(null, "Cut_V_CH16_Open", "스무 살 네 생일에 송전탑 밑에서 기다릴게. 8년이나 남았는데.", 9f, true, "— 여덟 해 전 —", "Cut_CH16_Open", 2),
                new Cut(null, "Cut_V_CH14_Close", "우유 두 병을 주며 말했다. 하나는 내일 마셔. 난 기다리는 게 특기야.", 9f, true, "— 여덟 해 전 —", "Cut_CH14_Close", 0),
                new Cut(null, "Cut_V_CH19_Close", "안 늦는다고 소리쳤다. 그 애 입 모양은 「알아」였다.", 9f, true, "— 여덟 해 전 —", "Cut_CH19_Close", 3),
                new Cut(null, "Cut_V_CH20_Open", "그날 밤 달력에 처음 썼다. 스무 살 생일, 송전탑 아래. 그 한 줄로 7년.", 9f, false, null, "Cut_CH20_Open", 1),
                new Cut(null, "Cut_CS6_7", "열아홉 생일 아침, 아빠 부표를 안고 바다로 갔다. 기억은 거기까지.", 9f, false, null, "Cut_CH08_Open", 2),
            }
        };

        // ── 컷씬 7 「둘 중 하나」 (CH16~18) · M1 ──
        private static readonly Def CS7 = new Def
        {
            id = "CS7", title = "둘 중 하나", bgm = "BGM_M1", sat = 0.32f, cardMain = "둘 중 하나", cardSub = "CHAPTER 16 ~ 18",
            cuts = new[]
            {
                new Cut(null, "Cut_V_END_A1", "어느 날부터 아무 기억도 안 돌아왔다. 대신 탑 아래, 그가 있었다.", 9f, false, null, "Cut_END_A1", 0),
                new Cut(null, "Cut_V_CH17_Open", "꼬마가 말했다. 저 사람 안다고. 어떻게 아는지는 안 물었다.", 9f, false, null, "Cut_CH17_Open", 2),
                new Cut(null, "Cut_CS7_3", "그는 새벽마다 약 한 움큼. 손목의 병원 팔찌는 끊긴 적이 없었다.", 9f, false, null, "Cut_CH20_Open", 1),
                new Cut(null, "Cut_V_CH07_Close", "넘어졌다. 아무도 안 봤다. 꼬마만 옆에 앉아 줬다.", 9f, false, null, "Cut_CH07_Close", 3),
                new Cut(null, "Cut_V_CH15_Open", "정류장 한가운데서 팔을 흔들어도, 그는 나를 통과해 걸었다.", 9f, false, null, "Cut_CH15_Open", 0),
                new Cut(null, "Cut_V_PRO_A", "우편함에 또 한 통이 들어왔다. 1년째, 나는 한 통도 못 뜯었다.", 9f, false, null, "Cut_PRO_A", 1),
                new Cut(null, "Cut_V_CH18_Open", "나란히 창을 봤다. 유리엔 한 사람만 비쳤다. 둘 중 하나는 여기 없다.", 9f, false, null, "Cut_CH18_Open", 3),
            }
        };

        // ── 컷씬 8 「주파수」 (CH19~20 → 마지막 러닝 → 엔딩) · M5 ──
        private static readonly Def CS8 = new Def
        {
            id = "CS8", title = "주파수", bgm = "BGM_M5", sat = 0.25f, cardMain = "주파수", cardSub = "CHAPTER 19 ~ 20  ·  마지막 4.2킬로",
            cuts = new[]
            {
                new Cut(null, "Cut_V_CH19_Open", "생일 전날은 아빠 기일. 그와 나란히 4.2킬로를 걸었다. 그는 몰랐다.", 9f, false, null, "Cut_CH19_Open", 0),
                new Cut(null, "Cut_CS8_2", "탑 아래 꽃이 두 다발. 아빠 몫 하나. 나머지 하나는 누구 거지.", 9f, false, null, "Cut_CH13_Close", 2),
                new Cut(null, "Cut_CS8_3", "그의 팔찌가 떨어졌고, 내 손에 잡혔다. 1년 만에 처음 잡힌 물건.", 9f, false, null, "Cut_CH15_Close", 1),
                new Cut(null, "Cut_V_CH20_Open", "그러니까 나는 이미 죽은 거구나. 약속 옆에 내 이름을 썼다. 하늘.", 9f, false, null, "Cut_CH20_Open", 0),
                new Cut(null, "Cut_V_END_B1", "스무 번째 생일. 집엔 내 것이 하나도 없었다. 탑 아래 우유 한 병뿐.", 9f, false, null, "Cut_END_B1", 3),
                new Cut(null, "Cut_V_CH20_Mid", "같은 태풍, 같은 정류장. 새 부표의 페인트가 손에 묻었다.", 9f, false, null, "Cut_CH20_Mid", 1),
                new Cut(null, "Cut_V_CH01_Close", "마지막 4.2킬로. 꼬마가 없었다. 처음으로, 혼자 일어나야 했다.", 9f, false, null, "Cut_CH01_Close", 0),
            }
        };
    }
}
