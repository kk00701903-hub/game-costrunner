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
                case "MV": return MV;
                default: return null;
            }
        }
        public static Def Cutscene(int index) => Get("CS" + Mathf.Clamp(index, 1, 8));

        // ── 오프닝 (열두 살 → 약속 → 열아홉의 아침) · M5 돌아온 제주 1:37 ──
        // 73차(사용자): 오프닝도 컷씬과 같은 수채 풀블리드 스틸 9장(Cut_V_OP_1~9, Kling)으로 — 옛 3D 카툰풍 영상(open_*.mp4)·스틸(Cut_Open_*)은 더 쓰지 않는다(폴백만).
        private static readonly Def Opening = new Def
        {
            id = "OPEN", title = "너와 나의 주파수", bgm = "BGM_M5", sat = 1f, holdToSeconds = 97f, gameTitleCard = true,
            cardMain = "너와 나의 주파수", cardSub = "우리의 송전탑  ·  COAST RUN",
            cuts = new[]
            {
                new Cut(null, "Cut_V_OP_1", "열두 살 봄, 송전탑 아래서 처음 만났다.", 10f, false, null, "Cut_Open_1", 0),   // 73차: 영상 대신 수채 스틸(옛 클립 open_1)
                new Cut(null, "Cut_V_OP_2", "서울서 온 창백한 전학생, 도윤. 그 애가 먼저 손을 내밀었다.", 10f, false, null, "Cut_Open_4", 1),   // 73차: 영상 대신 수채 스틸(옛 클립 open_7)
                new Cut(null, "Cut_V_OP_3", "유리구슬 한 알에 온 세상을 걸던 나이.", 10f, false, null, "Cut_Open_5", 2),   // 73차: 영상 대신 수채 스틸(옛 클립 open_8)
                new Cut(null, "Cut_V_OP_4", "딱지를 다 잃어도 도윤이 웃으면 괜찮았다.", 10f, false, null, "Cut_Open_6", 1),   // 73차: 영상 대신 수채 스틸(옛 클립 open_9)
                new Cut(null, "Cut_V_OP_5", "그 애는 늘 내 앞에 섰다. 한 번도 이유를 말하지 않고.", 10f, false, null, "Cut_Open_2", 0),   // 73차: 영상 대신 수채 스틸(옛 클립 open_2)
                new Cut(null, "Cut_V_OP_6", "말하지 못한 게 하나 있었다. 그때도, 지금도.", 10f, false, null, "Cut_Open_7", 2),   // 73차: 영상 대신 수채 스틸(옛 클립 open_10)
                new Cut(null, "Cut_V_OP_7", "떠나던 날 그 애가 말했다. 스무 살 네 생일, 송전탑 밑에서.", 10f, false, null, "Cut_Open_3", 3),   // 73차: 영상 대신 수채 스틸(옛 클립 open_3)
                new Cut(null, "Cut_V_OP_8", "열아홉의 어느 아침, 눈을 떴다. 아무것도 기억나지 않았다.", 10f, false, null, "Cut_END_A3", 0),   // 73차: 영상 대신 수채 스틸(옛 클립 open_4)
                new Cut(null, "Cut_V_OP_9", "달력엔 한 줄뿐. 스무 살 생일, 송전탑 아래. 1년 남았다.", 10f, false, null, "Cut_CS1_2", 3),   // 73차: 영상 대신 수채 스틸(옛 클립 open_6)
            }
        };

        // 75차(사용자): 컷씬 8편을 10컷 × 8초(≈1:20)로 늘리고 자막을 1.5배 풀어썼다(두 문장, 상황 설명 포함). 회상 컷은 세피아 + 「회상」 태그 + 레터박스.
        //   신규 그림 24장 Cut_V_N<편>_<n>(Kling). 첫 컷은 clip 이 있으면 Kling image2video 영상(StreamingAssets/Opening/cs<편>.mp4).
        // ── 컷씬 1 「이름」 · BGM_M1 ──
        private static readonly Def CS1 = new Def
        {
            id = "CS1", title = "이름", bgm = "BGM_M1", sat = 0.85f, cardMain = "이름", cardSub = "CHAPTER 1",
            cuts = new[]
            {
                new Cut("cs1", "Cut_V_CH01_Open", "눈을 떴다. 낯선 방, 낯선 아침. 문가에 주황 우비를 뒤집어쓴 꼬마가 나를 빤히 보고 있었다.", 8f, false, null, "Cut_CH01_Open", 0),
                new Cut(null, "Cut_V_N1_1", "내 이름이 뭐였더라. 어디서 왔는지, 왜 여기 누워 있었는지, 아무것도 떠오르지 않았다.", 8f, false, null, null, 1),
                new Cut(null, "Cut_CS1_2", "벽의 달력엔 딱 한 줄. 1년 뒤 내 생일에 동그라미. 나머지 칸은 전부 하얗게 비어 있었다.", 8f, false, null, "Cut_END_A3", 2),
                new Cut(null, "Cut_V_CH03_Open", "창밖 마을 사람들은 나를 스치고도 돌아보지 않았다. 거울은 아무리 닦아도 뿌옇기만 했다.", 8f, false, null, "Cut_CH03_Open", 1),
                new Cut(null, "Cut_V_CH17_Open", "이 꼬마만 나를 봤다. 이름이 없다고 하니, 바다처럼 파랗다며 「바다」라고 지어 줬다.", 8f, false, null, "Cut_CH17_Open", 0),
                new Cut(null, "Cut_V_N1_2", "정류장에서 버스를 기다렸지만 오지 않았다. 꼬마가 멀리 송전탑을 가리켰다. 저기까지 가 보자고.", 8f, false, null, null, 3),
                new Cut(null, "Cut_V_CH06_Open", "탑까지는 걸어서 한참. 꼬마는 한 번도 뒤돌아보지 않고 앞장서 걸었다. 나는 그 주황색만 따라갔다.", 8f, false, null, "Cut_CH06_Open", 1),
                new Cut(null, "Cut_V_CH19_Close", "집 문 앞에 스케이트보드 하나가 놓여 있었다. 누가 갖다 놓았는지는 묻지 않았다. 발이 먼저 올라섰다.", 8f, false, null, "Cut_CH19_Close", 2),
                new Cut(null, "Cut_V_N1_3", "보드에 올라서자 몸이 길을 기억했다. 바람, 돌담, 바다 냄새. 처음인데 처음이 아니었다.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH14_Close", "노을 속 탑 아래 우유 두 병이 놓여 있었다. 하나는 늘 그대로였다. 누가, 누구를 위해 두고 가는 걸까.", 8f, false, null, "Cut_CH14_Close", 3),
            }
        };

        // ── 컷씬 2 「하트」 · BGM_M6 ──
        private static readonly Def CS2 = new Def
        {
            id = "CS2", title = "하트", bgm = "BGM_M6", sat = 0.78f, cardMain = "하트", cardSub = "CHAPTER 2 ~ 4",
            cuts = new[]
            {
                new Cut("cs2", "Cut_V_CH02_Open", "빨랫줄엔 해녀복 두 벌. 우편함엔 뜯지 않은 편지가 스무 통. 이 집엔 나 말고 누가 살았던 걸까.", 8f, false, null, "Cut_CH02_Open", 1),
                new Cut(null, "Cut_V_N2_1", "편지 겉봉의 이름은 읽을 수가 없었다. 뜯으려 하면 손이 떨렸다. 무서운 게 무엇인지도 모른 채.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH09_Close", "바람이 편지 한 통을 채 갔다. 쫓아가다 처음으로 탑의 쇠기둥에 손이 닿았다. 차갑고, 웅웅 울렸다.", 8f, false, null, "Cut_CH09_Close", 2),
                new Cut(null, "Cut_CS2_3", "숨비소리. 물 위로 올라온 해녀가 길게 내쉬는 숨. 엄마의 첫 조각이 그 소리로 돌아왔다.", 8f, true, "— 회상 —", "Cut_CH02_Close", 1),
                new Cut(null, "Cut_V_N2_2", "엄마는 해녀였다. 나는 바닷가에서 엄마 숨소리를 세며 기다리는 아이였다. 그것만은 확실했다.", 8f, true, "— 회상 · 열세 해 전 —", null, 0),
                new Cut(null, "Cut_V_CH16_Close", "도윤. 그 애 이름만 먼저 돌아왔다. 길에서 불러 봤지만 그 뒷모습은 한 번 멈칫하고 그냥 갔다.", 8f, false, null, "Cut_CH16_Close", 3),
                new Cut(null, "Cut_V_CH04_Open", "아빠는 어부였다. 그릴 줄 아는 건 하트 하나뿐이라 부표마다 빨간 하트를 그렸다. 내 부표엔 두 개.", 8f, true, "— 회상 · 열세 해 전 —", "Cut_CH04_Open", 1),
                new Cut(null, "Cut_V_N2_3", "생일 전날 밤, 태풍 예보. 한 마리만 더 잡고 올게. 아빠는 주황 우비를 입고 나갔다. 나는 소매를 놓았다.", 8f, true, "— 회상 · 태풍 전날 밤 —", null, 2),
                new Cut(null, "Cut_V_CH13_Open", "그날 밤 비는 담을 넘도록 내렸다. 나는 대문 앞에서 밤새 기다렸다. 우비는 돌아오지 않았다.", 8f, true, "— 회상 · 태풍 전날 밤 —", "Cut_CH13_Open", 0),
                new Cut(null, "Cut_V_CH13_Close", "다음 날은 잔인하게 맑았다. 아빠 배는 탑 아래 바다에서 뒤집힌 채 발견됐다. 그래서 탑이 무섭고, 그리웠다.", 8f, false, null, "Cut_CH13_Close", 3),
            }
        };

        // ── 컷씬 3 「우리 기지」 · BGM_M2 ──
        private static readonly Def CS3 = new Def
        {
            id = "CS3", title = "우리 기지", bgm = "BGM_M2", sat = 0.7f, cardMain = "우리 기지", cardSub = "CHAPTER 5 ~ 7",
            cuts = new[]
            {
                new Cut("cs3", "Cut_V_CH05_Open", "부엌 창가의 낡은 도시락. 계란말이가 전부 하트 모양이었다. 뚜껑 안쪽엔 열두 살 글씨 — 반은 네 거.", 8f, false, null, "Cut_CH05_Open", 0),
                new Cut(null, "Cut_V_N3_1", "열두 살의 나는 도시락이 없는 아이였다. 그 애가 자기 도시락을 반으로 가르며 말했다. 반은 네 거.", 8f, true, "— 회상 · 여덟 해 전 —", null, 1),
                new Cut(null, "Cut_V_CH18_Mid", "밤의 탑 아래 그가 혼자 앉아 있었다. 다가가 어깨를 잡으려던 손이 그냥 지나갔다. 그는 몰랐다.", 8f, false, null, "Cut_CH18_Mid", 2),
                new Cut(null, "Cut_V_CH17_Open", "첫눈 오는 날, 꼬마와 창가에서 성에에 얼굴을 그렸다. 둘을 그리다 손이 멈췄다. 나머지 하나가 누군지 몰라서.", 8f, false, null, "Cut_CH17_Open", 1),
                new Cut(null, "Cut_V_CH07_Open", "눈 위 발자국은 한 줄뿐이었다. 꼬마 것만. 나란히 걸었는데 내 발자국은 어디에도 없었다.", 8f, false, null, "Cut_CH07_Open", 0),
                new Cut(null, "Cut_V_CH05_Close", "탑이 열두 살의 봄을 돌려줬다. 유채밭에서 그 애가 말했다. 내가 하는 말은 반은 진짜고 반은 거짓말이야.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH05_Close", 3),
                new Cut(null, "Cut_V_N3_2", "탑 아래 움푹한 자리가 우리 기지였다. 담요 한 장, 고장 난 라디오, 유리구슬. 세상에서 제일 안전한 곳.", 8f, true, "— 회상 · 여덟 해 전 —", null, 1),
                new Cut(null, "Cut_V_Open_3", "정류장에서 탑까지 4.2킬로. 노을이 지기 전에 닿으면 내가 이기는 놀이였다. 매번 그 애가 져 줬다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_Open_3", 2),
                new Cut(null, "Cut_V_N3_3", "지금의 기지엔 라디오만 남아 있었다. 다이얼을 돌리자 잡음 사이로 91.9. 우리 주파수였다.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH14_Open", "그 애 말대로 하기로 했다. 지워지면 또 그리면 돼. 내일도 탑까지 달린다. 기억이 그 길 위에 있으니까.", 8f, false, null, "Cut_CH14_Open", 3),
            }
        };

        // ── 컷씬 4 「열두 개의 초」 · BGM_M3 ──
        private static readonly Def CS4 = new Def
        {
            id = "CS4", title = "열두 개의 초", bgm = "BGM_M3", sat = 0.6f, cardMain = "열두 개의 초", cardSub = "CHAPTER 8 ~ 10",
            cuts = new[]
            {
                new Cut("cs4", "Cut_V_CH08_Open", "절벽 아래, 아빠 부표가 있던 바위. 누군가 새로 하트를 칠해 놓았다. 페인트 냄새가 아직 났다.", 8f, false, null, "Cut_CH08_Open", 2),
                new Cut(null, "Cut_V_N4_1", "손끝에 빨간 페인트가 묻어났다. 아빠는 없는데 누가 아빠의 하트를 그리는 걸까. 꼬마는 대답하지 않았다.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH09_Open", "3킬로 지점에서 처음으로 주저앉았다. 다리가 아니라 가슴이 무거웠다. 돌아와 보니 부표의 하트가 지워져 있었다.", 8f, false, null, "Cut_CH09_Open", 1),
                new Cut(null, "Cut_CS4_6", "돌 위에 초 하나가 켜져 있었다. 내가 다가가자 초는 저절로 꺼졌다. 바람은 없었다.", 8f, false, null, "Cut_CH13_Close", 3),
                new Cut(null, "Cut_V_CH10_Open", "열두 살 생일. 그 애는 초 열두 개를 꽂은 케이크를 들고 탑 아래로 나를 불렀다. 여기가 어떤 자리인지 모르고.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH10_Open", 1),
                new Cut(null, "Cut_V_N4_2", "나는 케이크를 엎었다. 여기서 아빠가 죽었어. 그 애는 그날 처음으로 내 앞에서 울었다.", 8f, true, "— 회상 · 여덟 해 전 —", null, 0),
                new Cut(null, "Cut_V_CH17_Close", "그 뒤 열흘, 그 애는 오지 않았다. 담장 위에 두고 간 도시락은 그대로 얼어 있었다. 나도 열지 않았다.", 8f, true, "— 회상 · 열흘 뒤 —", "Cut_CH17_Close", 2),
                new Cut(null, "Cut_V_N4_3", "열흘째 되던 날, 그 애가 탑으로 뛰어왔다. 미안하다는 말 대신 숨을 헐떡이며 내 옆에 섰다. 그걸로 됐다.", 8f, true, "— 회상 · 열흘 뒤 —", null, 1),
                new Cut(null, "Cut_V_CH12_Close", "그 집 아빠가 죽은 자리라고들 했다. 마을 사람들은 그렇게 불렀다. 그 애만 「우리 기지」라고 불렀다.", 8f, false, null, "Cut_CH12_Close", 0),
                new Cut(null, "Cut_V_CH11_Open", "비가 시작됐다. 탑 아래엔 오늘도 아무도 없었다. 그래도 간다. 초를 켜 놓는 사람을 만나야 하니까.", 8f, false, null, "Cut_CH11_Open", 3),
            }
        };

        // ── 컷씬 5 「그 밤」 · BGM_M6 ──
        private static readonly Def CS5 = new Def
        {
            id = "CS5", title = "그 밤", bgm = "BGM_M6", sat = 0.5f, cardMain = "그 밤", cardSub = "CHAPTER 11 ~ 13",
            cuts = new[]
            {
                new Cut("cs5", "Cut_V_CH12_Open", "길가에 버려진 리어카 한 대. 바퀴 하나가 없었다. 왜 거기 있는지 그땐 몰랐다.", 8f, false, null, "Cut_CH12_Open", 1),
                new Cut(null, "Cut_CS5_2", "빗속, 우산도 없이 탑 아래 선 뒷모습. 뛰어가면 사라지고, 멈추면 다시 있었다.", 8f, false, null, "Cut_CH11_Open", 0),
                new Cut(null, "Cut_CS5_3", "트럭이 나를 그대로 통과해 지나갔다. 그날부터 꼬마의 우비 소매가 찢어져 있었다. 나를 밀어낸 건 꼬마였다.", 8f, false, null, "Cut_CH17_Open", 2),
                new Cut(null, "Cut_V_N5_1", "다쳤냐고 물었다. 꼬마는 소매를 감추며 웃기만 했다. 나 때문이라는 걸, 그때는 몰랐다.", 8f, false, null, null, 1),
                new Cut(null, "Cut_CS5_4", "밤사이 누군가 리어카 바퀴를 새로 끼워 놓았다. 키 큰 사람이었다고, 꼬마가 말했다.", 8f, false, null, "Cut_CH12_Open", 3),
                new Cut(null, "Cut_V_CH13_Open", "폭우의 밤. 마당에 엄마가 엎어져 있었다. 심장이었다. 그 애가 먼저 보고 소리를 질렀다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH13_Open", 0),
                new Cut(null, "Cut_V_N5_2", "전화는 끊겨 있었고 차는 없었다. 열두 살 둘이 엄마를 리어카에 실었다. 그 애가 자기 자리를 엄마에게 줬다.", 8f, true, "— 회상 · 여덟 해 전 —", null, 1),
                new Cut(null, "Cut_CS5_6", "병원까지 2킬로. 빗길에서 리어카를 밀었다. 그 애 몸이 먼저 무너졌지만 손은 놓지 않았다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH11_Open", 2),
                new Cut(null, "Cut_V_N5_3", "엄마는 살았다. 그 애는 병원 복도에서 쓰러졌다. 검은 차가 왔고, 서울로 실려 갔다. 인사도 못 했다.", 8f, true, "— 회상 · 여덟 해 전 —", null, 0),
                new Cut(null, "Cut_V_CH13_Close", "무서웠어? 응. 나도. 그 밤 우리가 나눈 말은 그게 전부였다. 그 세 마디로 8년을 살았다.", 8f, false, null, "Cut_CH13_Close", 3),
            }
        };

        // ── 컷씬 6 「스무 살」 · BGM_M4 ──
        private static readonly Def CS6 = new Def
        {
            id = "CS6", title = "스무 살", bgm = "BGM_M4", sat = 0.42f, cardMain = "스무 살", cardSub = "CHAPTER 14 ~ 15",
            cuts = new[]
            {
                new Cut("cs6", "Cut_V_CH20_Mid", "정류장에서 탑까지 4.2킬로. 매일 달리던 길인데 오늘은 거기까지 못 갈 것 같다. 발이 자꾸 멈춘다.", 8f, false, null, "Cut_CH20_Mid", 0),
                new Cut(null, "Cut_V_Open_7", "떠나던 날, 우리에게 주어진 시간은 5분이었다. 검은 차가 시동을 건 채 기다리고 있었다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_Open_7", 1),
                new Cut(null, "Cut_V_CH16_Open", "스무 살 네 생일에 송전탑 밑에서 기다릴게. 그 애가 말했다. 8년이나 남았는데. 나는 대답을 못 했다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH16_Open", 2),
                new Cut(null, "Cut_V_CH14_Close", "그 애는 우유 두 병을 내밀며 말했다. 하나는 오늘, 하나는 내일 마셔. 난 기다리는 게 특기야.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH14_Close", 0),
                new Cut(null, "Cut_V_CH19_Close", "차가 멀어질 때 나는 소리쳤다. 안 늦을게! 그 애 입 모양은 「알아」였다.", 8f, true, "— 회상 · 여덟 해 전 —", "Cut_CH19_Close", 3),
                new Cut(null, "Cut_V_N6_1", "그날 밤 달력에 처음으로 글씨를 썼다. 스무 살 생일, 송전탑 아래. 그 한 줄로 7년을 버텼다.", 8f, true, "— 회상 · 여덟 해 전 —", null, 1),
                new Cut(null, "Cut_V_N6_2", "열아홉이 되던 해 엄마는 폐가 나빠 도시 병원으로 갔다. 나는 엄마 해녀복을 입었다. 바다는 내가 지켜야 했다.", 8f, false, null, null, 2),
                new Cut(null, "Cut_V_N6_3", "열아홉 생일 아침. 아빠 부표를 안고 바다로 들어갔다. 마지막 잠수를 하려고. 하늘이 이상하게 고요했다.", 8f, false, null, null, 0),
                new Cut(null, "Cut_CS6_7", "파도는 조용히 왔다. 그리고 기억은 거기서 끊긴다. 그다음 눈을 뜬 곳이 그 방이었다.", 8f, false, null, "Cut_CH08_Open", 1),
                new Cut(null, "Cut_V_CH20_Open", "달력의 그 한 줄이 나를 깨웠다. 스무 살 생일까지 1년. 약속 하나가 나를 여기 붙들어 두고 있다.", 8f, false, null, "Cut_CH20_Open", 3),
            }
        };

        // ── 컷씬 7 「둘 중 하나」 · BGM_M1 ──
        private static readonly Def CS7 = new Def
        {
            id = "CS7", title = "둘 중 하나", bgm = "BGM_M1", sat = 0.32f, cardMain = "둘 중 하나", cardSub = "CHAPTER 16 ~ 18",
            cuts = new[]
            {
                new Cut("cs7", "Cut_V_END_A1", "어느 날부터 기억이 더는 돌아오지 않았다. 대신 탑 아래 낯익은 남자가 서 있었다. 도윤이었다. 스무 살의.", 8f, false, null, "Cut_END_A1", 0),
                new Cut(null, "Cut_V_CH17_Open", "꼬마가 말했다. 저 사람 안다고. 어떻게 아느냐고는 묻지 않았다. 꼬마는 모르는 게 없었으니까.", 8f, false, null, "Cut_CH17_Open", 2),
                new Cut(null, "Cut_CS7_3", "그는 새벽마다 약을 한 움큼 삼켰다. 손목의 병원 팔찌는 끊긴 적이 없었다. 여전히 아픈 사람이었다.", 8f, false, null, "Cut_CH20_Open", 1),
                new Cut(null, "Cut_V_N7_1", "탑 아래 우유 두 병을 두고 가는 사람은 그였다. 하나는 오늘, 하나는 내일. 8년째 같은 자리.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH07_Close", "길에서 넘어졌다. 아무도 보지 못했다. 꼬마만 옆에 앉아 무릎을 털어 줬다.", 8f, false, null, "Cut_CH07_Close", 3),
                new Cut(null, "Cut_V_CH15_Open", "정류장 한가운데서 두 팔을 흔들었다. 그는 나를 그대로 통과해 걸어갔다. 안 보는 게 아니라, 볼 수 없는 거였다.", 8f, false, null, "Cut_CH15_Open", 0),
                new Cut(null, "Cut_V_PRO_A", "우편함에 또 한 통이 들어왔다. 8년 동안 스무 통. 나는 1년째 한 통도 뜯지 못했다.", 8f, false, null, "Cut_PRO_A", 1),
                new Cut(null, "Cut_V_N7_2", "그가 답장 없는 편지를 또 썼다. 우체국 앞에서 오래 서 있다가, 결국 우편함에 넣었다.", 8f, false, null, null, 2),
                new Cut(null, "Cut_V_N7_3", "그 옆에 앉았다. 같은 노을을 봤다. 그는 우유 한 병을 마시고 한 병은 그대로 두었다. 내 몫이었다.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH18_Open", "나란히 창을 봤다. 유리엔 한 사람만 비쳤다. 둘 중 하나는 여기 없다. 그게 누군지, 아직 묻지 못했다.", 8f, false, null, "Cut_CH18_Open", 3),
            }
        };

        // ── 컷씬 8 「주파수」 · BGM_M5 ──
        private static readonly Def CS8 = new Def
        {
            id = "CS8", title = "주파수", bgm = "BGM_M5", sat = 0.25f, cardMain = "주파수", cardSub = "CHAPTER 19 ~ 20  ·  마지막 4.2킬로",
            cuts = new[]
            {
                new Cut("cs8", "Cut_V_CH19_Open", "생일 전날은 아빠 기일. 그와 나란히 4.2킬로를 걸었다. 그는 몰랐다. 나도 이제야 알 것 같았다.", 8f, false, null, "Cut_CH19_Open", 0),
                new Cut(null, "Cut_CS8_2", "탑 아래 꽃이 두 다발 놓여 있었다. 아빠 몫 하나. 나머지 하나는 누구 거지. 국화는 내 이름을 알고 있었다.", 8f, false, null, "Cut_CH13_Close", 2),
                new Cut(null, "Cut_CS8_3", "그의 팔찌가 떨어졌고 내 손에 잡혔다. 1년 만에 처음으로 손에 잡힌 물건. 이름이 적혀 있었다. 하늘.", 8f, false, null, "Cut_CH15_Close", 1),
                new Cut(null, "Cut_V_N8_1", "환자 이름: 하늘. 실종 1년. 그는 내 팔찌를 차고 있었다. 나를 찾겠다고 병원에서 뛰쳐나온 사람의 팔찌.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH20_Open", "그러니까 나는 이미 죽은 거구나. 아니, 아직 못 찾은 거구나. 달력의 약속 옆에 내 이름을 썼다. 하늘.", 8f, false, null, "Cut_CH20_Open", 3),
                new Cut(null, "Cut_V_N8_2", "스무 번째 생일. 집엔 내 것이 하나도 없었다. 그릇도, 신발도. 탑 아래 우유 한 병만 내 것이었다.", 8f, false, null, null, 1),
                new Cut(null, "Cut_V_END_B1", "우유병 하나가 서리 속에서 빛났다. 그는 오늘도 두 병을 놓고 갔다. 8년째, 단 하루도 빠짐없이.", 8f, false, null, "Cut_END_B1", 2),
                new Cut(null, "Cut_V_N8_3", "같은 태풍이 왔다. 같은 정류장. 꼬마는 처음으로 곁에 없었다. 새 부표의 페인트가 손에 묻었다. 아빠였구나.", 8f, false, null, null, 0),
                new Cut(null, "Cut_V_CH20_Mid", "정류장에서 탑까지 4.2킬로. 이번엔 이기려고 달리는 게 아니다. 닿으려고 달린다.", 8f, false, null, "Cut_CH20_Mid", 1),
                new Cut(null, "Cut_V_CH01_Close", "마지막 4.2킬로. 꼬마가 없었다. 처음으로 혼자 일어나야 했다. 노을 전에 닿으면, 그가 나를 볼지도 모른다.", 8f, false, null, "Cut_CH01_Close", 3),
            }
        };

        // ── 75차(사용자): 뮤직비디오 「Our frequency」 — M1 OST 30초. 엔딩 뒤와 레코드 맨 끝에서 재생. 주인공 남녀 + 「스튜디오 우히&히시」 + 스트리밍 안내 카드 ──
        private static readonly Def MV = new Def
        {
            id = "MV", title = "Our frequency", bgm = "BGM_M1", sat = 1f, holdToSeconds = 30f, gameTitleCard = true,
            cardMain = "Our frequency", cardSub = "스튜디오 우히&히시\nYouTube Music · Spotify · iTunes · TIDAL\n「Our frequency」 검색",
            cuts = new[]
            {
                new Cut("mv1", "Cut_V_MV_1", "Our frequency  —  스튜디오 우히&히시", 4.5f, false, null, "Cut_V_OP_1", 0),
                new Cut(null, "Cut_V_MV_2", "너와 나의 주파수, 91.9", 4.5f, false, null, "Cut_V_OP_2", 1),
                new Cut(null, "Cut_V_MV_3", "하나는 오늘, 하나는 내일", 4.5f, false, null, "Cut_V_CH14_Close", 2),
                new Cut(null, "Cut_V_MV_4", "노을 전에 닿으면 내가 이기는 놀이", 4.5f, false, null, "Cut_V_Open_7", 0),
                new Cut("mv5", "Cut_V_MV_5", "우리의 송전탑", 4.5f, false, null, "Cut_V_Open_3", 3),
                new Cut(null, "Cut_V_MV_6", "YouTube Music · Spotify · iTunes · TIDAL  「Our frequency」", 4.5f, false, null, "Cut_V_OP_1", 1),
            }
        };
    }
}
