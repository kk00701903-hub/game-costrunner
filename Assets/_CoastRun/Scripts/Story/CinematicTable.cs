using UnityEngine;

namespace CoastRun
{
    /// 68차: 시네마틱 대본 — 오프닝 + 컷씬 8편(+ 77차: 엔딩 3편·MV)을 **같은 형식**(컷 = 클립/스틸 + 자막 + 켄번즈 + 음악 한 곡 + 마무리 카드)으로.
    ///   77차(사용자): 전편 재작성 — 오프닝 11컷(≈1:57)·컷씬 12컷(≈1:42), 어린 시절 괴롭힘·도윤이 준 평화·해녀 출근 전 아빠 우비가 떨어지는 떡밥,
    ///   컷씬1은 우비를 꼬마만 입고 주인공은 하늘색 후드, 카드에서 CHAPTER 표기 삭제, 엔딩 A/B/진엔딩 시네마.
    ///   그림은 Cut_S_<컷id>(컷씬 7 화풍, Kling + 앵커 이미지 ref77/) — 아직 없으면 fallback(옛 스틸)로 재생된다. 캐릭터 바이블·프롬프트: Docs/CUTSCENE_SCRIPTS_v3.md.
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
            /// 77차: 컷 길이 합(초) — 시네마 목록 표시용
            public float Length { get { float t = 0f; foreach (var c in cuts) t += c.dur; return t; } }
        }

        public static Def Get(string id)
        {
            switch (id)
            {
                case "OPEN": return Opening;
                case "CS1": return CS1; case "CS2": return CS2; case "CS3": return CS3; case "CS4": return CS4;
                case "CS5": return CS5; case "CS6": return CS6; case "CS7": return CS7; case "CS8": return CS8;
                case "END_A": return EndA; case "END_B": return EndB; case "END_TRUE": return EndTrue;
                case "MV": return MV;
                default: return null;
            }
        }
        public static Def Cutscene(int index) => Get("CS" + Mathf.Clamp(index, 1, 8));
        /// 77차: 엔딩 시네마 id — GameManager.EndingKind/진엔딩 → "END_A" | "END_B" | "END_TRUE"
        public static readonly string[] EndingIds = { "END_A", "END_B", "END_TRUE" };

        // ── 오프닝 (열두 살 → 괴롭힘 → 도윤 → 약속 → 열아홉의 아침) · M5 돌아온 제주(1:37, 루프) ──
        private static readonly Def Opening = new Def
        {
            id = "OPEN", title = "너와 나의 주파수", bgm = "BGM_M5", sat = 1.0f, holdToSeconds = 117f, gameTitleCard = true, cardMain = "너와 나의 주파수", cardSub = "우리의 송전탑  ·  COAST RUN",
            cuts = new[]
            {
                new Cut(null, "Cut_S_OP_1", "열두 살 봄. 제주의 작은 마을, 언덕 위엔 송전탑이 서 있었다. 나는 그 아래서 혼자 노는 아이였다.", 10.6f, false, null, "Cut_V_OP_1", 0),
                new Cut(null, "Cut_S_OP_2", "학교에선 늘 혼자였다. 해녀 엄마의 생선 냄새가 난다고 아이들은 내 옆에 앉지 않았다.", 10.6f, true, "— 회상 · 열두 살 —", "Cut_V_N3_1", 1),
                new Cut(null, "Cut_S_OP_3", "장날엔 좌판 옆에 앉아 생선을 팔았다. 같은 반 아이들이 지나가며 웃었다. 나는 고개를 들지 않았다.", 10.6f, true, "— 회상 · 열두 살 —", "Cut_V_N2_2", 2),
                new Cut(null, "Cut_S_OP_4", "서울서 온 창백한 전학생, 도윤. 항상 검은 양복 아저씨가 뒤에 서 있는 이상한 애. 그 애가 먼저 손을 내밀었다.", 10.6f, false, null, "Cut_V_OP_2", 1),
                new Cut(null, "Cut_S_OP_5", "아이들이 내 가방을 도랑에 던진 날, 그 애가 도랑에 들어가 가방을 건졌다. 흰 셔츠가 흙투성이가 됐는데도 웃었다.", 10.6f, false, null, "Cut_V_OP_5", 0),
                new Cut(null, "Cut_S_OP_6", "그날부터 아무도 나를 건드리지 않았다. 그 애 옆에 있으면 세상이 조용했다. 처음으로 학교가 무섭지 않았다.", 10.6f, false, null, "Cut_V_N3_1", 2),
                new Cut(null, "Cut_S_OP_7", "유리구슬 한 알에 온 세상을 걸던 나이. 탑 아래 움푹한 자리가 우리 기지였다.", 10.6f, false, null, "Cut_V_OP_3", 1),
                new Cut(null, "Cut_S_OP_8", "그 애는 늘 내 앞에 섰다. 한 번도 이유를 말하지 않고. 나는 그 등만 보고 걸었다.", 10.6f, false, null, "Cut_V_OP_5", 0),
                new Cut(null, "Cut_S_OP_9", "떠나던 날 그 애가 말했다. 스무 살 네 생일, 송전탑 밑에서 기다릴게. 검은 차가 시동을 건 채 서 있었다.", 10.6f, false, null, "Cut_V_OP_7", 3),
                new Cut(null, "Cut_S_OP_10", "열아홉의 어느 아침, 눈을 떴다. 낯선 방. 내 이름도, 어제도 떠오르지 않았다.", 10.6f, false, null, "Cut_V_OP_8", 0),
                new Cut(null, "Cut_S_OP_11", "벽의 달력엔 한 줄뿐. 스무 살 생일, 송전탑 아래. 1년 남았다. 그 약속 하나가 나를 여기 붙들어 두고 있다.", 10.6f, false, null, "Cut_V_OP_9", 3),
            }
        };

        // ── 컷씬 1 「이름」 · BGM_M1 ──
        private static readonly Def CS1 = new Def
        {
            id = "CS1", title = "이름", bgm = "BGM_M1", sat = 0.85f, cardMain = "이름",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N1_01", "눈을 떴다. 낯선 방, 낯선 아침. 문가에 주황 우비를 뒤집어쓴 꼬마가 서 있었다. 후드 아래로 동그란 뺨이 보였다.", 8.5f, false, null, "Cut_V_CH01_Open", 0),
                new Cut(null, "Cut_S_N1_02", "내 이름이 뭐였더라. 어디서 왔는지, 왜 여기 누워 있었는지, 아무것도 떠오르지 않았다.", 8.5f, false, null, "Cut_V_N1_1", 1),
                new Cut(null, "Cut_S_N1_03", "벽의 달력엔 딱 한 줄. 1년 뒤 내 생일에 동그라미. 나머지 칸은 전부 하얗게 비어 있었다.", 8.5f, false, null, "Cut_CS1_2", 2),
                new Cut(null, "Cut_S_N1_04", "창밖 마을 사람들은 나를 스치고도 돌아보지 않았다. 거울은 아무리 닦아도 뿌옇기만 했다.", 8.5f, false, null, "Cut_V_CH03_Open", 1),
                new Cut(null, "Cut_S_N1_05", "이 꼬마만 나를 봤다. 이름이 없다고 하니, 바다처럼 파랗다며 「바다」라고 지어 줬다.", 8.5f, false, null, "Cut_V_CH17_Open", 0),
                new Cut(null, "Cut_S_N1_06", "정류장에서 버스를 기다렸지만 오지 않았다. 꼬마가 멀리 송전탑을 가리켰다. 저기까지 가 보자고.", 8.5f, false, null, "Cut_V_N1_2", 3),
                new Cut(null, "Cut_S_N1_07", "탑까지는 걸어서 한참. 꼬마는 한 번도 뒤돌아보지 않고 앞장서 걸었다. 나는 그 주황색만 따라갔다.", 8.5f, false, null, "Cut_V_CH06_Open", 1),
                new Cut(null, "Cut_S_N1_08", "탑 아래 돌 세 개가 쌓여 있었다. 꼬마가 말했다. 「이건 건드리면 안 돼.」 누가 쌓았는지는 말해 주지 않았다.", 8.5f, false, null, "Cut_V_CH12_Close", 2),
                new Cut(null, "Cut_S_N1_09", "집 문 앞에 스케이트보드 하나가 놓여 있었다. 누가 갖다 놓았는지는 묻지 않았다. 발이 먼저 올라섰다.", 8.5f, false, null, "Cut_V_CH19_Close", 1),
                new Cut(null, "Cut_S_N1_10", "보드에 올라서자 몸이 길을 기억했다. 바람, 돌담, 바다 냄새. 처음인데 처음이 아니었다.", 8.5f, false, null, "Cut_V_N1_3", 0),
                new Cut(null, "Cut_S_N1_11", "노을 속 탑 아래 우유 두 병이 놓여 있었다. 하나는 늘 그대로였다. 누가, 누구를 위해 두고 가는 걸까.", 8.5f, false, null, "Cut_V_CH14_Close", 3),
                new Cut(null, "Cut_S_N1_12", "돌아오는 길, 꼬마가 내 손을 잡았다. 작고 차가운 손. 「내일도 가자.」 나는 고개를 끄덕였다.", 8.5f, false, null, "Cut_V_CH06_Open", 0),
            }
        };

        // ── 컷씬 2 「하트」 · BGM_M6 ──
        private static readonly Def CS2 = new Def
        {
            id = "CS2", title = "하트", bgm = "BGM_M6", sat = 0.78f, cardMain = "하트",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N2_01", "빨랫줄엔 해녀복 두 벌. 우편함엔 뜯지 않은 편지가 스무 통. 이 집엔 나 말고 누가 살았던 걸까.", 8.5f, false, null, "Cut_V_CH02_Open", 1),
                new Cut(null, "Cut_S_N2_02", "편지 겉봉의 이름은 읽을 수가 없었다. 뜯으려 하면 손이 떨렸다. 무서운 게 무엇인지도 모른 채.", 8.5f, false, null, "Cut_V_N2_1", 0),
                new Cut(null, "Cut_S_N2_03", "바람이 편지 한 통을 채 갔다. 쫓아가다 처음으로 탑의 쇠기둥에 손이 닿았다. 차갑고, 웅웅 울렸다.", 8.5f, false, null, "Cut_V_CH09_Close", 2),
                new Cut(null, "Cut_S_N2_04", "숨비소리. 물 위로 올라온 해녀가 길게 내쉬는 숨. 엄마의 첫 조각이 그 소리로 돌아왔다.", 8.5f, true, "— 회상 —", "Cut_CS2_3", 1),
                new Cut(null, "Cut_S_N2_05", "엄마는 해녀였다. 나는 바닷가 바위에서 엄마 숨소리를 세며 기다리는 아이였다. 그것만은 확실했다.", 8.5f, true, "— 회상 · 열세 해 전 —", "Cut_V_N2_2", 0),
                new Cut(null, "Cut_S_N2_06", "도윤. 그 애 이름만 먼저 돌아왔다. 길에서 불러 봤지만 그 뒷모습은 한 번 멈칫하고 그냥 갔다.", 8.5f, false, null, "Cut_V_CH16_Close", 3),
                new Cut(null, "Cut_S_N2_07", "아빠는 어부였다. 작은 고기잡이 배 한 척이 전부였다. 그릴 줄 아는 건 하트 하나뿐이라 부표마다 빨간 하트를 그렸다.", 8.5f, true, "— 회상 · 열세 해 전 —", "Cut_V_CH04_Open", 1),
                new Cut(null, "Cut_S_N2_08", "내 부표엔 하트가 두 개였다. 하나는 나, 하나는 아빠. 아빠는 그걸 뱃머리에 매달았다.", 8.5f, true, "— 회상 · 열세 해 전 —", "Cut_V_CH08_Open", 2),
                new Cut(null, "Cut_S_N2_09", "생일 전날 밤, 태풍 예보. 한 마리만 더 잡고 올게. 아빠는 주황 우비를 입고 나갔다. 나는 소매를 놓았다.", 8.5f, true, "— 회상 · 태풍 전날 밤 —", "Cut_V_N2_3", 2),
                new Cut(null, "Cut_S_N2_10", "그날 밤 비는 담을 넘도록 내렸다. 나는 대문 앞에서 밤새 기다렸다. 우비는 돌아오지 않았다.", 8.5f, true, "— 회상 · 태풍 전날 밤 —", "Cut_V_CH13_Open", 0),
                new Cut(null, "Cut_S_N2_11", "다음 날은 잔인하게 맑았다. 아빠 배는 탑 아래 바다에서 뒤집힌 채 발견됐다. 하트 부표만 떠 있었다.", 8.5f, false, null, "Cut_V_CH13_Close", 3),
                new Cut(null, "Cut_S_N2_12", "그래서 탑이 무섭고, 그리웠다. 오늘도 탑 아래엔 돌 세 개가 그대로 있었다. 누군가 매일 다녀가는 것처럼.", 8.5f, false, null, "Cut_V_CH12_Close", 3),
            }
        };

        // ── 컷씬 3 「우리 기지」 · BGM_M2 ──
        private static readonly Def CS3 = new Def
        {
            id = "CS3", title = "우리 기지", bgm = "BGM_M2", sat = 0.7f, cardMain = "우리 기지",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N3_01", "부엌 창가의 낡은 도시락. 계란말이가 전부 하트 모양이었다. 뚜껑 안쪽엔 열두 살 글씨 — 반은 네 거.", 8.5f, false, null, "Cut_V_CH05_Open", 0),
                new Cut(null, "Cut_S_N3_02", "열두 살의 나는 도시락이 없는 아이였다. 점심시간이면 운동장 구석에서 물만 마셨다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N3_1", 1),
                new Cut(null, "Cut_S_N3_03", "그 애가 자기 도시락을 반으로 가르며 말했다. 반은 네 거. 나는 처음으로 남 앞에서 밥을 먹었다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N3_1", 2),
                new Cut(null, "Cut_S_N3_04", "밤의 탑 아래 그가 혼자 앉아 있었다. 다가가 어깨를 잡으려던 손이 그냥 지나갔다. 그는 몰랐다.", 8.5f, false, null, "Cut_V_CH18_Mid", 1),
                new Cut(null, "Cut_S_N3_05", "첫눈 오는 날, 꼬마와 창가에서 성에에 얼굴을 그렸다. 둘을 그리다 손이 멈췄다. 나머지 하나가 누군지 몰라서.", 8.5f, false, null, "Cut_V_CH17_Open", 0),
                new Cut(null, "Cut_S_N3_06", "눈 위 발자국은 한 줄뿐이었다. 꼬마 것만. 나란히 걸었는데 내 발자국은 어디에도 없었다.", 8.5f, false, null, "Cut_V_CH07_Open", 3),
                new Cut(null, "Cut_S_N3_07", "탑이 열두 살의 봄을 돌려줬다. 유채밭에서 그 애가 말했다. 내가 하는 말은 반은 진짜고 반은 거짓말이야.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH05_Close", 1),
                new Cut(null, "Cut_S_N3_08", "탑 아래 움푹한 자리가 우리 기지였다. 담요 한 장, 고장 난 라디오, 유리구슬. 세상에서 제일 안전한 곳.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N3_2", 2),
                new Cut(null, "Cut_S_N3_09", "정류장에서 탑까지 4.2킬로. 노을이 지기 전에 닿으면 내가 이기는 놀이였다. 매번 그 애가 져 줬다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_Open_3", 0),
                new Cut(null, "Cut_S_N3_10", "괴롭히던 아이들이 다시 왔을 때, 그 애는 내 앞에 서서 말했다. 얘 건드리면 우리 집 아저씨가 온다. 아이들은 물러났다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_OP_5", 1),
                new Cut(null, "Cut_S_N3_11", "지금의 기지엔 라디오만 남아 있었다. 다이얼을 돌리자 잡음 사이로 91.9. 우리 주파수였다.", 8.5f, false, null, "Cut_V_N3_3", 2),
                new Cut(null, "Cut_S_N3_12", "그 애 말대로 하기로 했다. 지워지면 또 그리면 돼. 내일도 탑까지 달린다. 기억이 그 길 위에 있으니까.", 8.5f, false, null, "Cut_V_CH14_Open", 3),
            }
        };

        // ── 컷씬 4 「열두 개의 초」 · BGM_M3 ──
        private static readonly Def CS4 = new Def
        {
            id = "CS4", title = "열두 개의 초", bgm = "BGM_M3", sat = 0.6f, cardMain = "열두 개의 초",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N4_01", "절벽 아래, 아빠 부표가 있던 바위. 누군가 새로 하트를 칠해 놓았다. 페인트 냄새가 아직 났다.", 8.5f, false, null, "Cut_V_CH08_Open", 2),
                new Cut(null, "Cut_S_N4_02", "손끝에 빨간 페인트가 묻어났다. 아빠는 없는데 누가 아빠의 하트를 그리는 걸까. 꼬마는 대답하지 않았다.", 8.5f, false, null, "Cut_V_N4_1", 0),
                new Cut(null, "Cut_S_N4_03", "3킬로 지점에서 처음으로 주저앉았다. 다리가 아니라 가슴이 무거웠다. 돌아와 보니 부표의 하트가 지워져 있었다.", 8.5f, false, null, "Cut_V_CH09_Open", 1),
                new Cut(null, "Cut_S_N4_04", "돌 위에 초 하나가 켜져 있었다. 내가 다가가자 초는 저절로 꺼졌다. 바람은 없었다.", 8.5f, false, null, "Cut_CS4_6", 3),
                new Cut(null, "Cut_S_N4_05", "열두 살 생일. 그 애는 초 열두 개를 꽂은 케이크를 들고 탑 아래로 나를 불렀다. 여기가 어떤 자리인지 모르고.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH10_Open", 1),
                new Cut(null, "Cut_S_N4_06", "나는 케이크를 엎었다. 여기서 아빠가 죽었어. 그 애는 그날 처음으로 내 앞에서 울었다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N4_2", 0),
                new Cut(null, "Cut_S_N4_07", "그 뒤 열흘, 그 애는 오지 않았다. 담장 위에 두고 간 도시락은 그대로 얼어 있었다. 나도 열지 않았다.", 8.5f, true, "— 회상 · 열흘 뒤 —", "Cut_V_CH17_Close", 2),
                new Cut(null, "Cut_S_N4_08", "열흘째 되던 날, 그 애가 탑으로 뛰어왔다. 미안하다는 말 대신 숨을 헐떡이며 내 옆에 섰다. 그걸로 됐다.", 8.5f, true, "— 회상 · 열흘 뒤 —", "Cut_V_N4_3", 1),
                new Cut(null, "Cut_S_N4_09", "그 집 아빠가 죽은 자리라고들 했다. 마을 사람들은 그렇게 불렀다. 그 애만 「우리 기지」라고 불렀다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH12_Close", 0),
                new Cut(null, "Cut_S_N4_10", "기지에 돌 세 개를 쌓은 건 그 애였다. 하나는 너, 하나는 나, 하나는 아저씨. 무너지면 다시 쌓으면 돼.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N3_2", 2),
                new Cut(null, "Cut_S_N4_11", "비가 시작됐다. 탑 아래엔 오늘도 아무도 없었다. 그래도 간다. 초를 켜 놓는 사람을 만나야 하니까.", 8.5f, false, null, "Cut_V_CH11_Open", 3),
                new Cut(null, "Cut_S_N4_12", "꼬마가 뒤에서 뛰어와 우비 자락으로 내 머리를 덮어 줬다. 우비는 너무 커서 둘이 들어가고도 남았다.", 8.5f, false, null, "Cut_V_N5_1", 0),
            }
        };

        // ── 컷씬 5 「그 밤」 · BGM_M6 ──
        private static readonly Def CS5 = new Def
        {
            id = "CS5", title = "그 밤", bgm = "BGM_M6", sat = 0.5f, cardMain = "그 밤",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N5_01", "길가에 버려진 리어카 한 대. 바퀴 하나가 없었다. 왜 거기 있는지 그땐 몰랐다.", 8.5f, false, null, "Cut_V_CH12_Open", 1),
                new Cut(null, "Cut_S_N5_02", "빗속, 우산도 없이 탑 아래 선 뒷모습. 뛰어가면 사라지고, 멈추면 다시 있었다.", 8.5f, false, null, "Cut_CS5_2", 0),
                new Cut(null, "Cut_S_N5_03", "트럭이 나를 그대로 통과해 지나갔다. 그날부터 꼬마의 우비 소매가 찢어져 있었다. 나를 밀어낸 건 꼬마였다.", 8.5f, false, null, "Cut_CS5_3", 2),
                new Cut(null, "Cut_S_N5_04", "다쳤냐고 물었다. 꼬마는 소매를 감추며 웃기만 했다. 나 때문이라는 걸, 그때는 몰랐다.", 8.5f, false, null, "Cut_V_N5_1", 1),
                new Cut(null, "Cut_S_N5_05", "밤사이 누군가 리어카 바퀴를 새로 끼워 놓았다. 키 큰 사람이었다고, 꼬마가 말했다.", 8.5f, false, null, "Cut_CS5_4", 3),
                new Cut(null, "Cut_S_N5_06", "폭우의 밤. 마당에 엄마가 엎어져 있었다. 심장이었다. 그 애가 먼저 보고 소리를 질렀다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH13_Open", 0),
                new Cut(null, "Cut_S_N5_07", "전화는 끊겨 있었고 차는 없었다. 열두 살 둘이 엄마를 리어카에 실었다. 그 애가 자기 자리를 엄마에게 줬다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N5_2", 1),
                new Cut(null, "Cut_S_N5_08", "병원까지 2킬로. 빗길에서 리어카를 밀었다. 그 애 몸이 먼저 무너졌지만 손은 놓지 않았다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_CS5_6", 2),
                new Cut(null, "Cut_S_N5_09", "엄마는 살았다. 그 애는 병원 복도에서 쓰러졌다. 검은 차가 왔고, 서울로 실려 갔다. 인사도 못 했다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N5_3", 0),
                new Cut(null, "Cut_S_N5_10", "무서웠어? 응. 나도. 그 밤 우리가 나눈 말은 그게 전부였다. 그 세 마디로 8년을 살았다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH13_Close", 3),
                new Cut(null, "Cut_S_N5_11", "리어카 손잡이에 오래된 흠집이 있었다. 작은 손톱자국. 여덟 해 전 그 밤의 것이었다. 내 손이 꼭 맞았다.", 8.5f, false, null, "Cut_V_CH12_Open", 1),
                new Cut(null, "Cut_S_N5_12", "꼬마가 리어카 위에 올라앉았다. 「밀어 줘.」 나는 웃으며 밀었다. 바퀴가 처음으로 잘 굴렀다.", 8.5f, false, null, "Cut_V_N5_2", 0),
            }
        };

        // ── 컷씬 6 「스무 살」 · BGM_M4 ──
        private static readonly Def CS6 = new Def
        {
            id = "CS6", title = "스무 살", bgm = "BGM_M4", sat = 0.42f, cardMain = "스무 살",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N6_01", "정류장에서 탑까지 4.2킬로. 매일 달리던 길인데 오늘은 거기까지 못 갈 것 같다. 발이 자꾸 멈춘다.", 8.5f, false, null, "Cut_V_CH20_Mid", 0),
                new Cut(null, "Cut_S_N6_02", "떠나던 날, 우리에게 주어진 시간은 5분이었다. 검은 차가 시동을 건 채 기다리고 있었다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_Open_7", 1),
                new Cut(null, "Cut_S_N6_03", "스무 살 네 생일에 송전탑 밑에서 기다릴게. 그 애가 말했다. 8년이나 남았는데. 나는 대답을 못 했다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH16_Open", 2),
                new Cut(null, "Cut_S_N6_04", "그 애는 우유 두 병을 내밀며 말했다. 하나는 오늘, 하나는 내일 마셔. 난 기다리는 게 특기야.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH14_Close", 0),
                new Cut(null, "Cut_S_N6_05", "차가 멀어질 때 나는 소리쳤다. 안 늦을게! 그 애 입 모양은 「알아」였다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_CH19_Close", 3),
                new Cut(null, "Cut_S_N6_06", "그날 밤 달력에 처음으로 글씨를 썼다. 스무 살 생일, 송전탑 아래. 그 한 줄로 7년을 버텼다.", 8.5f, true, "— 회상 · 여덟 해 전 —", "Cut_V_N6_1", 1),
                new Cut(null, "Cut_S_N6_07", "열아홉이 되던 해 엄마는 폐가 나빠 도시 병원으로 갔다. 나는 엄마 해녀복을 입었다. 바다는 내가 지켜야 했다.", 8.5f, false, null, "Cut_V_N6_2", 2),
                new Cut(null, "Cut_S_N6_08", "출근하는 아침마다 벽의 주황 우비가 떨어져 있었다. 올려놓았다. 다음 날 또 떨어져 있었다. 못은 멀쩡했다.", 8.5f, false, null, "Cut_V_N1_1", 1),
                new Cut(null, "Cut_S_N6_09", "세 번째 아침에도 우비는 바닥에 있었다. 나는 우비를 한 번 안았다가 못에 걸고 나갔다. 그게 마지막이었다.", 8.5f, false, null, "Cut_V_N1_1", 0),
                new Cut(null, "Cut_S_N6_10", "열아홉 생일 아침. 아빠 부표를 안고 바다로 들어갔다. 마지막 잠수를 하려고. 하늘이 이상하게 고요했다.", 8.5f, false, null, "Cut_V_N6_3", 2),
                new Cut(null, "Cut_S_N6_11", "파도는 조용히 왔다. 그리고 기억은 거기서 끊긴다. 그다음 눈을 뜬 곳이 그 방이었다.", 8.5f, false, null, "Cut_CS6_7", 1),
                new Cut(null, "Cut_S_N6_12", "달력의 그 한 줄이 나를 깨웠다. 스무 살 생일까지 1년. 약속 하나가 나를 여기 붙들어 두고 있다.", 8.5f, false, null, "Cut_V_CH20_Open", 3),
            }
        };

        // ── 컷씬 7 「둘 중 하나」 · BGM_M1 ──
        private static readonly Def CS7 = new Def
        {
            id = "CS7", title = "둘 중 하나", bgm = "BGM_M1", sat = 0.32f, cardMain = "둘 중 하나",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N7_01", "어느 날부터 기억이 더는 돌아오지 않았다. 대신 탑 아래 낯익은 남자가 서 있었다. 도윤이었다. 스무 살의.", 8.5f, false, null, "Cut_V_END_A1", 0),
                new Cut(null, "Cut_S_N7_02", "꼬마가 말했다. 저 사람 안다고. 어떻게 아느냐고는 묻지 않았다. 꼬마는 모르는 게 없었으니까.", 8.5f, false, null, "Cut_V_CH17_Open", 2),
                new Cut(null, "Cut_S_N7_03", "그는 새벽마다 약을 한 움큼 삼켰다. 손목의 병원 팔찌는 끊긴 적이 없었다. 여전히 아픈 사람이었다.", 8.5f, false, null, "Cut_CS7_3", 1),
                new Cut(null, "Cut_S_N7_04", "탑 아래 우유 두 병을 두고 가는 사람은 그였다. 하나는 오늘, 하나는 내일. 8년째 같은 자리.", 8.5f, false, null, "Cut_V_N7_1", 0),
                new Cut(null, "Cut_S_N7_05", "길에서 넘어졌다. 아무도 보지 못했다. 꼬마만 옆에 앉아 무릎을 털어 줬다.", 8.5f, false, null, "Cut_V_CH07_Close", 3),
                new Cut(null, "Cut_S_N7_06", "정류장 한가운데서 두 팔을 흔들었다. 그는 나를 그대로 통과해 걸어갔다. 안 보는 게 아니라, 볼 수 없는 거였다.", 8.5f, false, null, "Cut_V_CH15_Open", 0),
                new Cut(null, "Cut_S_N7_07", "우편함에 또 한 통이 들어왔다. 8년 동안 스무 통. 나는 1년째 한 통도 뜯지 못했다.", 8.5f, false, null, "Cut_V_PRO_A", 1),
                new Cut(null, "Cut_S_N7_08", "그가 답장 없는 편지를 또 썼다. 우체국 앞에서 오래 서 있다가, 결국 우편함에 넣었다.", 8.5f, false, null, "Cut_V_N7_2", 2),
                new Cut(null, "Cut_S_N7_09", "그 옆에 앉았다. 같은 노을을 봤다. 그는 우유 한 병을 마시고 한 병은 그대로 두었다. 내 몫이었다.", 8.5f, false, null, "Cut_V_N7_3", 0),
                new Cut(null, "Cut_S_N7_10", "그의 방 창가에 국화 한 다발이 있었다. 리본엔 이름이 없었다. 누구를 위한 꽃인지 물을 수가 없었다.", 8.5f, false, null, "Cut_V_CH18_Open", 2),
                new Cut(null, "Cut_S_N7_11", "꼬마는 그를 보면 늘 한 걸음 물러났다. 「저 사람은 아직 몰라.」 무엇을 모른다는 건지 묻지 못했다.", 8.5f, false, null, "Cut_V_CH17_Open", 1),
                new Cut(null, "Cut_S_N7_12", "나란히 창을 봤다. 유리엔 한 사람만 비쳤다. 둘 중 하나는 여기 없다. 그게 누군지, 아직 묻지 못했다.", 8.5f, false, null, "Cut_V_CH18_Open", 3),
            }
        };

        // ── 컷씬 8 「주파수」 · BGM_M5 ──
        private static readonly Def CS8 = new Def
        {
            id = "CS8", title = "주파수", bgm = "BGM_M5", sat = 0.25f, cardMain = "주파수",
            cuts = new[]
            {
                new Cut(null, "Cut_S_N8_01", "생일 전날은 아빠 기일. 그와 나란히 4.2킬로를 걸었다. 그는 몰랐다. 나도 이제야 알 것 같았다.", 8.5f, false, null, "Cut_V_CH19_Open", 0),
                new Cut(null, "Cut_S_N8_02", "탑 아래 꽃이 두 다발 놓여 있었다. 아빠 몫 하나. 나머지 하나는 누구 거지. 국화는 내 이름을 알고 있었다.", 8.5f, false, null, "Cut_CS8_2", 2),
                new Cut(null, "Cut_S_N8_03", "그의 팔찌가 떨어졌고 내 손에 잡혔다. 1년 만에 처음으로 손에 잡힌 물건. 이름이 적혀 있었다. 하늘.", 8.5f, false, null, "Cut_CS8_3", 1),
                new Cut(null, "Cut_S_N8_04", "환자 이름: 하늘. 실종 1년. 그는 내 팔찌를 차고 있었다. 나를 찾겠다고 병원에서 뛰쳐나온 사람의 팔찌.", 8.5f, false, null, "Cut_V_N8_1", 0),
                new Cut(null, "Cut_S_N8_05", "그러니까 나는 이미 죽은 거구나. 아니, 아직 못 찾은 거구나. 달력의 약속 옆에 내 이름을 썼다. 하늘.", 8.5f, false, null, "Cut_V_CH20_Open", 3),
                new Cut(null, "Cut_S_N8_06", "스무 번째 생일. 집엔 내 것이 하나도 없었다. 그릇도, 신발도. 탑 아래 우유 한 병만 내 것이었다.", 8.5f, false, null, "Cut_V_N8_2", 1),
                new Cut(null, "Cut_S_N8_07", "우유병 하나가 서리 속에서 빛났다. 그는 오늘도 두 병을 놓고 갔다. 8년째, 단 하루도 빠짐없이.", 8.5f, false, null, "Cut_V_END_B1", 2),
                new Cut(null, "Cut_S_N8_08", "같은 태풍이 왔다. 같은 정류장. 꼬마는 처음으로 곁에 없었다. 새 부표의 페인트가 손에 묻었다. 아빠였구나.", 8.5f, false, null, "Cut_V_N8_3", 0),
                new Cut(null, "Cut_S_N8_09", "탑 아래 돌 세 개 옆에 주황 우비가 개켜져 있었다. 소매가 찢어진 우비. 꼬마는 어디에도 없었다.", 8.5f, false, null, "Cut_V_CH12_Close", 1),
                new Cut(null, "Cut_S_N8_10", "정류장에서 탑까지 4.2킬로. 이번엔 이기려고 달리는 게 아니다. 닿으려고 달린다.", 8.5f, false, null, "Cut_V_CH20_Mid", 0),
                new Cut(null, "Cut_S_N8_11", "마지막 4.2킬로. 꼬마가 없었다. 처음으로 혼자 일어나야 했다. 노을 전에 닿으면, 그가 나를 볼지도 모른다.", 8.5f, false, null, "Cut_V_CH01_Close", 2),
                new Cut(null, "Cut_S_N8_12", "탑 아래 그가 서 있었다. 국화 두 다발을 들고. 나는 숨을 고르고 그의 이름을 불렀다. 도윤아.", 8.5f, false, null, "Cut_V_END_A1", 3),
            }
        };

        // ── 엔딩 「스무 살 생일 · 만난다」 · BGM_M3 ──
        private static readonly Def EndA = new Def
        {
            id = "END_A", title = "스무 살 생일 · 만난다", bgm = "BGM_M3", sat = 0.6f, cardMain = "스무 살 생일", cardSub = "엔딩 A · 만난다",
            cuts = new[]
            {
                new Cut(null, "Cut_S_EA_01", "노을 반. 탑 아래에 엄마가 무릎을 꿇고 국화를 놓았다. 어제 그가 놓은 두 다발 옆에.", 8.5f, false, null, "Cut_V_CH19_Open", 0),
                new Cut(null, "Cut_S_EA_02", "엄마가 일어서다 그를 봤다. 8년 전 빗속에서 자기를 리어카에 싣고 간 아이. 「많이 컸수다.」", 8.5f, false, null, "Cut_V_END_A1", 1),
                new Cut(null, "Cut_S_EA_03", "엄마. 나 여기 있어. 두 사람 사이에서 손을 들었다. 둘 다 보지 않았다. 엄마가 언덕을 내려갔다.", 8.5f, false, null, "Cut_V_CH15_Open", 2),
                new Cut(null, "Cut_S_EA_04", "도윤아. 그가 돌아봤다. 얼굴이 환해졌다. 내 얼굴부터 색이 돌았다. 그의 얼굴, 억새, 노을, 바다.", 8.5f, false, null, "Cut_V_MV_6", 0),
                new Cut(null, "Cut_S_EA_05", "무사 이추룩 늦언. 너 그거 어디서 배웠어. 8년. 나는 스무 챕터 만에 처음으로 소리 내어 웃었다.", 8.5f, false, null, "Cut_V_MV_3", 1),
                new Cut(null, "Cut_S_EA_06", "꼬마가 돌 세 개 위에 하나를 더 얹었다. 네 개. 왜 더 쌓아. 또 나가야 되니까. 언제 와. 내년에.", 8.5f, false, null, "Cut_V_CH12_Close", 2),
                new Cut(null, "Cut_S_EA_07", "봄. 유채가 진짜 노란 해안도로. 그가 걷고, 옆에서 보드 바퀴 소리가 났다. 그가 옆을 보며 웃었다.", 8.5f, false, null, "Cut_V_MV_1", 1),
                new Cut(null, "Cut_S_EA_08", "정류장. 운행 재개. 버스 문이 열리자 그가 반걸음 물러나 손을 내밀었다. 먼저 타. 우유 두 개. 딸깍, 딸깍.", 8.5f, false, null, "Cut_V_N1_2", 3),
            }
        };

        // ── 엔딩 「우유 두 병 · 못 만난다」 · BGM_M6 ──
        private static readonly Def EndB = new Def
        {
            id = "END_B", title = "우유 두 병 · 못 만난다", bgm = "BGM_M6", sat = 0.35f, cardMain = "우유 두 병", cardSub = "엔딩 B · 못 만난다",
            cuts = new[]
            {
                new Cut(null, "Cut_S_EB_01", "노을 반. 탑 아래에 엄마가 국화를 놓았다. 조금 떨어진 곳에 그가 앉아 있었다. 한 손을 가슴에 얹고.", 8.5f, false, null, "Cut_V_CH19_Open", 0),
                new Cut(null, "Cut_S_EB_02", "도윤아. 그가 천천히 일어나 돌아봤다. 눈이 반 뼘 어긋나 있었다. 거기 있어? 있는 것 같은데.", 8.5f, false, null, "Cut_V_CH15_Open", 1),
                new Cut(null, "Cut_S_EB_03", "허공을 더듬는 손. 나는 그 손을 잡았다. 두 손 사이에서 하트 하나가 켜졌다. 화면에서 유일한 색.", 8.5f, false, null, "Cut_V_MV_3", 2),
                new Cut(null, "Cut_S_EB_04", "나 보여? 아니. 근데 잡혀. 나는 손목의 팔찌를 빼서 그의 손목에 끼웠다. 그가 손목을 만졌다.", 8.5f, false, null, "Cut_CS8_3", 0),
                new Cut(null, "Cut_S_EB_05", "국화 뒤에 세워진 액자 하나. 검은 리본. 내 얼굴이었다. 마지막 조각 — 물속, 부표를 쥔 손, 멈추는 기포.", 8.5f, false, null, "Cut_CS6_7", 1),
                new Cut(null, "Cut_S_EB_06", "바다야! 억새에서 주황 우비가 올라왔다. 너 나 몇 번 일으켰어. 몰라. 나도 몰라. 많이.", 8.5f, false, null, "Cut_V_CH17_Open", 2),
                new Cut(null, "Cut_S_EB_07", "돌 세 개를 손으로 쓸어 무너뜨렸다. 돌아왔으니까. 주머니에서 하트 돌 하나. 아빠 맞지. 한 마리만 더 잡으민 되주.", 8.5f, false, null, "Cut_V_N4_1", 0),
                new Cut(null, "Cut_S_EB_08", "다음 날 아침. 가게. 오늘도 두 개라? 예. 두 개마씸. 문이 닫히고 종이 한 번 더 울렸다. 아무도 안 나갔는데.", 8.5f, false, null, "Cut_V_N7_2", 3),
            }
        };

        // ── 엔딩 「주파수 · 진엔딩」 · BGM_M1 ──
        private static readonly Def EndTrue = new Def
        {
            id = "END_TRUE", title = "주파수 · 진엔딩", bgm = "BGM_M1", sat = 0.8f, cardMain = "주파수", cardSub = "진엔딩",
            cuts = new[]
            {
                new Cut(null, "Cut_S_ET_01", "이듬해 봄. 새벽 두 시. 91.9. 오늘은 사연이 아니라 목소리로 왔습니다. 제주에서, 열아홉 살…", 8.5f, false, null, "Cut_V_N3_3", 0),
                new Cut(null, "Cut_S_ET_02", "잡음. 그리고 아는 목소리. 하늘아. 규칙이 하나 더 있었어. 네가 두 번 다 끝까지 오면, 한 번 더 말할 수 있대.", 8.5f, false, null, "Cut_V_N3_3", 1),
                new Cut(null, "Cut_S_ET_03", "늦게 와도 된다고 했잖아. 취소. 늦게 오지 마. 내년 봄에도 그 자리에 있을 거야. 노을 질 때.", 8.5f, false, null, "Cut_V_N7_2", 2),
                new Cut(null, "Cut_S_ET_04", "창밖 송전탑에 불이 하나 켜져 있었다. 응. 안 늦어.", 8.5f, false, null, "Cut_V_CH18_Open", 0),
                new Cut(null, "Cut_S_ET_05", "그리고 유채가 하루 일찍 피었다.", 8.5f, false, null, "Cut_V_OP_1", 3),
                new Cut(null, "Cut_S_ET_06", "노을 질 때, 탑 아래. 두 사람이 마주 보고 서 있었다. 이번엔 둘 다 보였다.", 8.5f, false, null, "Cut_V_MV_6", 0),
                new Cut(null, "Cut_S_ET_07", "우리의 주파수. 91.9.", 8.5f, false, null, "Cut_V_MV_4", 1),
                new Cut(null, "Cut_S_ET_08", "— 너와 나의 주파수 —", 8.5f, false, null, "Cut_V_MV_5", 3),
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
