using UnityEngine;

namespace CoastRun
{
    /// 68차: 시네마틱 대본 — 오프닝 + 컷씬 8편(+ 77차: 엔딩 3편·MV)을 **같은 형식**(컷 = 클립/스틸 + 자막 + 켄번즈 + 음악 한 곡 + 마무리 카드)으로.
    ///   103차: **대본 v7.1**(Docs/CUTSCENE_SCRIPTS_v7.md, 279컷 · 한 컷 64자 이하 · 6~7초, 회상 롱컷) · 95차: 대본 v6(188컷) · 85차: 대본 v4 — OPEN 14컷(10.6s) · CS1~8(13~16컷, 8.5s) · **보조 컷씬 EV1~10(4컷 × 7s, 스토리 모드 CH2·4·6·8·9·11·13·14·15·19)** ·
    ///   엔딩 A 「엇갈린 정류장」 / B 「우유 두 병」 / TRUE 「맞닿은 주파수 89.2」(단서 여섯 개 clueMask 로 분기 — ClueSystem).
    ///   그림은 `Cut_T_<컷id>`(v4, 192장: Kling 웹 116 신규 + v3 `Cut_S_*` 복사 76) — 없으면 fallback(옛 v3 스틸). 자막이 긴 컷(>108자)은 10~12초.
    ///   **이 파일은 Tools/Story/gen_cinematic_v7.py 가 v7 md 에서 생성한다** — 자막을 고치려면 md 를 고치고 다시 생성할 것(MV 절만 손으로).
    public static class CinematicTable
    {
        public class Cut
        {
            public string clip, still, fallback, caption, tag;
            public float dur; public Vector2 from, to; public bool sepia;
            /// 85차: 컷별 채도 키프레임(-1 = Def.sat 그대로) — END_TRUE 2컷부터 1.0 복귀.
            public float sat = -1f;
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
                case "OPEN_F1": return Frag(1); case "OPEN_F2": return Frag(2); case "OPEN_F3": return Frag(3);   // 196차: 기억 조각
                case "CS1": return CS1; case "CS2": return CS2; case "CS3": return CS3; case "CS4": return CS4;
                case "CS5": return CS5; case "CS6": return CS6; case "CS7": return CS7; case "CS8": return CS8;
                case "EV1": return EV1; case "EV2": return EV2; case "EV3": return EV3; case "EV4": return EV4; case "EV5": return EV5;
                case "EV6": return EV6; case "EV7": return EV7; case "EV8": return EV8; case "EV9": return EV9; case "EV10": return EV10;
                case "END_A": return EndA; case "END_B": return EndB; case "END_TRUE": return EndTrue;
                case "MV": return MV;
                default: return null;
            }
        }
        public static Def Cutscene(int index) => Get("CS" + Mathf.Clamp(index, 1, 8));
        /// 85차: 보조 컷씬 EV1~10(스토리 모드 CH2·4·6·8·9·11·13·14·15·19)
        public static Def Event(int index) => Get("EV" + Mathf.Clamp(index, 1, EventCount));
        public const int EventCount = 10;
        /// 77차: 엔딩 시네마 id — ClueSystem.EndingId(clueMask) → "END_A" | "END_B" | "END_TRUE"
        public static readonly string[] EndingIds = { "END_A", "END_B", "END_TRUE" };

        // ── 오프닝 「그 약속」 · BGM_M5 · 5컷 ──
        /// 196차(사용자: OPEN 「처음엔 숨기고 기억 조각으로」): 오프닝 다섯 컷을 세 조각(1–2 · 3–4 · 5)으로 — 자막·그림 그대로 잘라 쓴다.
        private static readonly Def[] _frags = new Def[4];
        private static Def Frag(int n)
        {
            if (n < 1 || n > 3) return null;
            if (_frags[n] != null) return _frags[n];
            int a = n == 1 ? 0 : n == 2 ? 2 : 4, len = n == 3 ? 1 : 2;
            var cuts = new Cut[len]; System.Array.Copy(Opening.cuts, a, cuts, 0, len);
            _frags[n] = new Def { id = "OPEN_F" + n, title = Opening.title, bgm = Opening.bgm, sat = Opening.sat, cardMain = Opening.title, cardSub = n + " / 3", cuts = cuts };
            return _frags[n];
        }

        private static readonly Def Opening = new Def
        {
            id = "OPEN", title = "그 약속", bgm = "BGM_M5", sat = 1.00f, holdToSeconds = 12f, gameTitleCard = true, cardMain = "너와 나의 주파수", cardSub = "우리의 송전탑  ·  COAST RUN",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_OPB_01", "검은 차가 출발했다. 나는 뒷유리를 손바닥으로 두드렸다. 창이 내려갔다.", 6.5f, false, null, "Cut_T_N6_02", 0),
                new Cut(null, "Cut_T_V7_E2_08", "「스무 살 네 생일에 송전탑 아래서 보자! 꼭!」 차가 흙먼지 속으로 멀어졌다.", 6.5f, false, null, "Cut_T_V7_OP_07", 1),
                new Cut(null, "Cut_T_V7_OPB_03", "그날 밤. 달력의 내 생일에 동그라미를 치고 썼다. 「송전탑 아래.」 연필이 종이를 팼다.", 6.5f, false, null, "Cut_T_N1_02", 2),
                new Cut(null, "Cut_T_V7_OPB_04", "달력이 일곱 번 바뀌었다. 동그라미는 해마다 같은 날에 있었다.", 6.5f, false, null, "Cut_T_V7_OPB_03", 3),
                new Cut(null, "Cut_T_V7_N1_01", "일곱 번째 봄. 담요 끝이 바람에 들썩였다. 머리 위에서 쇠가 웅웅 울었다.", 6.5f, false, null, "Cut_T_N1_02", 1),
            }
        };

        // ── 컷씬 1 「모르는 얼굴」 · BGM_M1 · 22컷 ──
        private static readonly Def CS1 = new Def
        {
            id = "CS1", title = "모르는 얼굴", bgm = "BGM_M1", sat = 0.85f, cardMain = "모르는 얼굴",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N1_01", "눈을 떴다. 낡은 담요 위였다. 바람이 담요 끝을 자꾸 들췄다.", 6.5f, false, null, "Cut_T_N1_02", 0),
                new Cut(null, "Cut_T_V7_N1_02", "머리 위로 녹슨 쇠기둥이 하늘까지 올라가 있었고, 바람에 쇠가 웅웅 울었다. 하늘은 잿빛이었다.", 6.5f, false, null, "Cut_T_N2_04", 1),
                new Cut(null, "Cut_T_V7_N1_03", "파도가 방파제를 넘어 하얗게 부서졌다. 멀리 마을 스피커의 태풍 경보 방송이 바람에 끊어질 듯 이어졌다.", 6.5f, false, null, "Cut_T_N2_13", 2),
                new Cut(null, "Cut_T_V7_N1_01", "이름을 떠올리려고 했다. 안 나왔다. 엄마 얼굴도, 집도, 어제도. 아무것도.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N1_05", "기둥을 짚고 일어났다. 다리가 후들거렸다. 바위 웅덩이에 얼굴이 비쳤다.", 6.5f, false, null, "Cut_T_N1_02", 1),
                new Cut(null, "Cut_T_V7_N1_05", "웅덩이 속 여자애를 한참 봤다. 이름이 안 나왔다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N1_07", "담요 위에 주저앉아 울었다. 소리를 내서 울었다. 왜 우는지도 몰랐다. 무서웠다. 그냥 무서웠다.", 6.5f, false, null, "Cut_T_N1_02", 3),
                new Cut(null, "Cut_T_V7_N1_08", "돌 세 개 옆에 주황 우비를 눌러쓴 꼬마가 앉아 있었다. 여섯 살쯤. 내가 우는 걸 다 보고 있었다.", 6.5f, false, null, "Cut_T_N1_10", 2),
                new Cut(null, "Cut_T_V7_N1_08", "「누나, 이름 몰라?」 고개를 끄덕였다. 꼬마는 놀라지 않았다. 「그럼 바다누나. 눈이 바다처럼 파래.」", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N1_13", "꼬마 손을 잡고 마을로 내려갔다. 바람에 등이 떠밀렸다.", 6.5f, false, null, "Cut_S_N7_09", 1),
                new Cut(null, "Cut_T_V7_N1_11", "골목마다 사람들이 창문에 신문지를 붙이고 배를 뭍으로 끌어올리고 있었다.", 6.5f, false, null, "Cut_T_N1_06", 2),
                new Cut(null, "Cut_T_E1_01", "할머니한테 인사했다. 할머니는 대답이 없었다. 라디오에서 태풍 경보만 반복됐다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N1_06", "가게 앞, 정류장, 방파제. 다들 바빴다. 누구도 나를 아는 얼굴이 아니었다.", 6.5f, false, null, "Cut_S_N1_04", 1),
                new Cut(null, "Cut_T_N1_06", "아무도 나를 찾지 않았다. 그게 제일 무서웠다.", 6.5f, false, null, "Cut_S_N1_04", 0),
                new Cut(null, "Cut_T_N1_05", "「나는 누나 알아.」 꼬마가 말했다. 「어떻게 알아?」 「그냥 알아.」", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N1_05", "꼬마는 같은 말을 두 번 했다. 그래도 그 말 하나에 다리가 조금 풀렸다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_N1_07", "운행이 끊긴 정류장. 꼬마가 우리가 내려온 언덕의 녹슨 탑을 가리켰다.", 6.5f, false, null, "Cut_S_N1_07", 0),
                new Cut(null, "Cut_T_N1_07", "「여기서 저기까지 노을 전에 가면 누나가 이기는 거야.」 무슨 놀이냐고 물었다. 「그냥 놀이.」", 6.5f, false, null, "Cut_S_N1_07", 1),
                new Cut(null, "Cut_T_N1_08", "꼬마가 정류장 뒤에서 낡은 스케이트보드를 끌고 나왔다.", 6.5f, false, null, "Cut_S_N1_10", 2),
                new Cut(null, "Cut_T_N1_08", "발을 올리니 몸이 먼저 균형을 잡았다. 머리는 모르는데 발이 알았다.", 6.5f, false, null, "Cut_S_N1_10", 3),
                new Cut(null, "Cut_T_V7_N1_21", "해가 지기 전에 탑 아래 담요 자리에 닿았다. 꼬마가 박수를 쳤다. 「이겼다!」 나는 웃었다. 오늘 처음 웃었다.", 6.5f, false, null, "Cut_T_N4_11", 1),
                new Cut(null, "Cut_T_V7_N1_22", "그날 밤 바람은 더 세졌다. 꼬마가 우비 자락을 벌려 내 어깨까지 덮었다. 둘이 그 밑에서 잤다.", 6.5f, false, null, "Cut_T_E5_01", 0),
            }
        };

        // ── 보조 컷씬 EV1 「보드」 · BGM_M1 · CH2 · 8컷 ──
        private static readonly Def EV1 = new Def
        {
            id = "EV1", title = "보드", bgm = "BGM_M1", sat = 0.85f, cardMain = "보드", cardSub = "이야기 · 제 2화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E1_01", "다음 날도 정류장에서 탑까지 달렸다. 꼬마를 보드 앞에 태웠다.", 6.0f, false, null, "Cut_T_E4_02", 0),
                new Cut(null, "Cut_T_V7_E1_01", "우비 후드가 바람에 부풀었다. 「더 빨리!」 「무서우면 말해.」 「더 빨리!」", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_E1_03", "돌담 사이 굽은 길에서 넘어졌다. 둘 다 무릎이 까졌다.", 6.0f, false, null, "Cut_T_E4_02", 2),
                new Cut(null, "Cut_T_V7_E1_03", "꼬마가 내 무릎에 침을 발랐다. 「이러면 안 아파.」 안 아프진 않았다. 그래도 웃었다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_E1_05", "탑 아래 우유 두 병이 놓여 있었다.", 6.0f, false, null, "Cut_T_N1_14", 1),
                new Cut(null, "Cut_T_V7_E1_06", "꼬마가 주머니에서 분홍빛 조개껍데기를 꺼냈다. 빨간 크레파스로 그 위에 하트를 그렸다. 삐뚤었다.", 6.0f, false, null, "Cut_T_E4_03", 0),
                new Cut(null, "Cut_T_V7_E1_06", "꼬마가 그 조개껍데기를 내 손에 쥐여 줬다. 「잃어버리지 마.」", 6.0f, false, null, "Cut_T_E4_03", 3),
                new Cut(null, "Cut_T_N1_13", "돌아오는 길, 꼬마가 내 손을 잡았다. 작고 차가운 손. 「내일도 가자.」 「응. 내일도.」", 6.0f, false, null, "Cut_S_N7_09", 2),
            }
        };

        // ── 컷씬 2 「사진 속 얼굴」 · BGM_M6 · 16컷 ──
        private static readonly Def CS2 = new Def
        {
            id = "CS2", title = "사진 속 얼굴", bgm = "BGM_M6", sat = 0.78f, cardMain = "사진 속 얼굴",
            cuts = new[]
            {
                new Cut(null, "Cut_T_E1_01", "가게에서 우유를 집었다. 주인 할머니는 라디오만 듣고 있었다. 「저기요.」", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_E1_01", "두 번 불렀다. 할머니는 고개를 안 들었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N1_06", "밖에서 아저씨와 부딪혔다. 아저씨는 내 옆을 그냥 지나갔다. 「죄송해요」도 없었다. 나는 길가로 비켜섰다.", 6.5f, false, null, "Cut_S_N1_04", 2),
                new Cut(null, "Cut_T_E1_02", "꼬마한테 물었다. 「왜 아무도 나를 안 봐?」 꼬마는 대답 대신 발끝으로 돌멩이를 찼다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_E1_02", "「누나, 배고파.」 「대답해.」 「배고파.」 꼬마는 같은 말을 두 번 했다. 나는 더 못 물었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N2_06", "초소 앞. 아줌마가 전단지를 돌리고 있었다. 지나가는 사람마다 한 장씩.", 6.5f, false, null, "Cut_T_E6_04", 0),
                new Cut(null, "Cut_T_V7_N2_06", "받는 사람은 적었다. 아줌마는 말 대신 고개를 숙였다. 소리가 하나도 없었다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N2_08", "아줌마가 내 앞에도 섰다. 잠깐 나를 보더니 전단지를 내밀었다. 나는 받았다.", 6.5f, false, null, "Cut_T_E6_04", 2),
                new Cut(null, "Cut_T_V7_N2_08", "어디서 본 얼굴이었다. 슬퍼 보였다. 아줌마는 고개를 숙이고 다음 사람에게 갔다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N2_10", "전단지를 봤다. 여자애 사진. 「고하늘, 19세, 실종.」 나는 사진을 한참 봤다. 웃지 않는 얼굴이었다.", 6.5f, false, null, "Cut_T_N7_05", 1),
                new Cut(null, "Cut_T_E5_03", "꼬마가 전단지를 접어 자기 우비 주머니에 넣었다. 「내가 갖고 있을게.」 후드를 더 눌러썼다.", 6.5f, false, null, "Cut_S_N5_04", 2),
                new Cut(null, "Cut_T_V7_N2_12", "정류장에 키 큰 남자가 있었다. 흰 셔츠. 우유 두 병. 남자가 나를 보고 고개를 살짝 숙였다. 「안녕하세요.」", 6.5f, false, null, "Cut_T_E3_03", 3),
                new Cut(null, "Cut_T_V7_N2_12", "나도 「안녕하세요」 했다. 남자는 버스가 안 오는 정류장에 그냥 서 있었다. 뭘 기다리는지 몰랐다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N2_12", "남자가 입을 막고 오래 기침했다. 입술이 파랬다. 기침이 끝나자 다시 정류장에 섰다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N7_10", "밤에 가게 유리창에 얼굴을 비춰 봤다. 낮에 본 전단지 얼굴이 떠올랐다.", 6.5f, false, null, "Cut_S_N7_12", 3),
                new Cut(null, "Cut_T_N7_10", "꼬마가 옆에 와서 유리를 보더니 손바닥으로 김을 닦았다. 「누나, 자자.」 「응.」 탑 아래 담요로 돌아갔다.", 6.5f, false, null, "Cut_S_N7_12", 2),
            }
        };

        // ── 보조 컷씬 EV3 「물장구」 · BGM_M6 · CH4 · 8컷 ──
        private static readonly Def EV3 = new Def
        {
            id = "EV3", title = "물장구", bgm = "BGM_M6", sat = 0.70f, cardMain = "물장구", cardSub = "이야기 · 제 4화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E3_01", "여름. 나는 방파제에 앉아 바다만 봤다. 며칠째 그랬다.", 6.0f, false, null, "Cut_T_N2_13", 0),
                new Cut(null, "Cut_T_V7_E3_02", "꼬마가 내 앞에서 얕은 물로 첨벙 들어갔다. 우비를 입은 채로. 「누나, 봐 봐.」", 6.0f, false, null, "Cut_T_E4_04", 1),
                new Cut(null, "Cut_T_V7_E3_02", "물을 튀기고, 넘어지고, 다시 튀겼다. 물에 뜬 주황색이 웃었다. 나도 신을 벗고 들어갔다.", 6.0f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_E3_02", "「우비 안 벗어?」 「엄마가 벗지 말랬어.」 「엄마는 어디 있어?」 「뭍에.」 꼬마가 물을 끼얹었다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_E4_04", "모래에 둘이 누워 숨을 골랐다. 꼬마가 왼쪽 눈을 접으며 웃었다. 나도 웃었다.", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_E3_06", "해가 지도록 놀았다. 배가 고팠다. 젖은 채로 방파제에 앉아 있는데 해녀복을 입은 아줌마가 지나가다 멈췄다.", 6.0f, false, null, "Cut_T_E6_04", 0),
                new Cut(null, "Cut_T_V7_E3_06", "초소 앞에서 본 아줌마였다. 아줌마는 나를 한참 보더니 내 손을 잡았다. 그리고 언덕 쪽을 가리켰다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_E3_06", "꼬마가 내 다른 손을 잡았다. 셋이 언덕을 올라갔다.", 6.0f, false, null, null, 2),
            }
        };

        // ── 컷씬 3 「밥」 · BGM_M5 · 14컷 ──
        private static readonly Def CS3 = new Def
        {
            id = "CS3", title = "밥", bgm = "BGM_M5", sat = 0.70f, cardMain = "밥",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N3_01", "언덕 위 외딴집. 마당에 해녀복이 널려 있었다. 아줌마가 마루에 나를 앉히고 수건을 건넸다.", 6.5f, false, null, "Cut_T_N2_01", 0),
                new Cut(null, "Cut_T_V7_N3_02", "아줌마가 종이에 글씨를 썼다. 「밥 먹었어?」 나는 고개를 저었다.", 6.5f, false, null, "Cut_T_V6_N8_06", 1),
                new Cut(null, "Cut_T_V7_N3_02", "「말 못 해요?」 입 모양을 크게 해서 물었다. 아줌마가 고개를 끄덕였다. 「듣는 것도?」 또 끄덕였다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N3_02", "아줌마가 종이에 썼다. 「이름이 뭐야?」", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N3_02", "나는 펜을 들었다. 쓸 게 없었다. 「모르겠어요.」 아줌마가 내 글씨를 한참 봤다. 그리고 더 묻지 않았다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N3_06", "부엌에서 미역국 냄새가 났다. 아줌마가 밥상을 차렸다. 밥, 미역국, 계란말이. 계란말이가 하트 모양이었다.", 6.5f, false, null, "Cut_T_N3_01", 0),
                new Cut(null, "Cut_T_V7_N3_06", "나는 밥을 두 그릇 먹었다. 따뜻한 밥을 먹어 본 게 오랜만이었다. 언제 먹었는지는 기억이 안 났다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N3_02", "아줌마가 종이에 썼다. 「딸이 집을 나갔어. 찾는 중이야.」", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N3_02", "「언제요?」 「봄에.」 「어디로요?」 아줌마는 펜을 놓았다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N3_02", "「그래서 매일 초소에 가는 거예요?」 끄덕. 「딸이 몇 살이에요?」 아줌마가 손가락을 폈다. 열아홉.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N3_11", "아줌마가 내 머리를 한 번 쓰다듬었다. 그리고 썼다. 「우리 딸 같아서.」 눈물이 났다. 밥을 먹다가 울었다.", 6.5f, false, null, "Cut_T_V6_N6_10", 2),
                new Cut(null, "Cut_T_V7_N3_11", "아줌마가 미역국을 더 퍼 줬다. 「많이 먹어.」 그 글씨가 삐뚤었다. 나는 다 먹었다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N3_13", "꼬마는 마루에 올라오지 않았다. 마당에 서서 창으로 나를 보고 있었다.", 6.5f, false, null, "Cut_T_N1_01", 1),
                new Cut(null, "Cut_T_V7_N3_13", "들어오라고 손짓했다. 꼬마는 고개를 저었다. 웃는 것처럼 보였다. 그런데 슬퍼 보였다.", 6.5f, false, null, null, 0),
            }
        };

        // ── 보조 컷씬 EV4 「태왁」 · BGM_M1 · CH6 · 9컷 ──
        private static readonly Def EV4 = new Def
        {
            id = "EV4", title = "태왁", bgm = "BGM_M1", sat = 0.60f, cardMain = "태왁", cardSub = "이야기 · 제 6화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E4_01", "새벽. 아줌마가 해녀복을 입고 바다로 갔다. 나는 태왁을 들고 따라갔다. 아줌마가 손짓했다. 「여기 앉아 있어.」", 6.0f, false, null, "Cut_T_E2_01", 0),
                new Cut(null, "Cut_T_V7_E4_02", "바위 위에서 아줌마가 물에 들어갔다 나오는 걸 셌다. 열일곱 번. 물 위로 올라올 때마다 긴 숨소리가 났다.", 6.0f, false, null, "Cut_T_E2_01", 1),
                new Cut(null, "Cut_T_V7_E4_02", "소리 없는 사람의 숨비소리가 바다보다 컸다. 그런데 태왁 망이 비어 있었다. 아줌마는 소라를 따지 않았다.", 6.0f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_E4_04", "아줌마는 바위 틈을 하나씩 들여다봤다. 폐그물 더미를 들추고, 다시 들어가고, 다시 들췄다.", 6.0f, false, null, "Cut_T_OP_13", 3),
                new Cut(null, "Cut_T_V7_E4_04", "물에서 나올 때마다 손에 아무것도 없었다. 나는 무엇을 찾는지 묻지 못했다.", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_E4_06", "마지막에 아줌마가 물에서 나와 내 무릎에 소라를 하나 올려놨다. 「너 거.」", 6.0f, false, null, "Cut_T_E2_02", 0),
                new Cut(null, "Cut_T_V7_E4_06", "손짓이었다. 손이 차가웠다. 나는 아줌마 손을 두 손으로 감쌌다. 아줌마가 웃었다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_E4_08", "저녁에 아줌마가 내 머리를 땋아 줬다. 마루 끝에 꼬마가 앉아 그걸 보고 있었다.", 6.0f, false, null, "Cut_T_V6_N6_10", 2),
                new Cut(null, "Cut_T_V7_E4_08", "웃는 것처럼 보였다. 그런데 슬퍼 보였다.", 6.0f, false, null, null, 0),
            }
        };

        // ── 컷씬 4 「언덕 위 집」 · BGM_M3 · 17컷 ──
        private static readonly Def CS4 = new Def
        {
            id = "CS4", title = "언덕 위 집", bgm = "BGM_M3", sat = 0.60f, cardMain = "언덕 위 집",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N4_01", "아줌마 집에 매일 갔다. 아줌마는 매일 밥을 했다. 나는 설거지를 했다. 손이 시리도록 찬물이었다. 좋았다.", 6.5f, false, null, "Cut_T_N3_01", 0),
                new Cut(null, "Cut_T_N2_01", "아줌마가 빨래를 걷었다. 크기가 다른 해녀복 두 벌.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N2_01", "작은 쪽은 아무도 안 입었다. 아줌마는 그걸 매일 걷고 매일 다시 널었다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N4_04", "저녁에 아줌마가 사진을 꺼내 보여 줬다. 열아홉 살 여자애.", 6.5f, false, null, "Cut_T_E6_04", 3),
                new Cut(null, "Cut_T_V7_N4_04", "나는 사진을 봤다. 「예쁘네요.」 아줌마가 웃었다. 그리고 사진을 다시 품에 넣었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N4_06", "아줌마가 내 손을 잡고 마당의 작은 텃밭으로 갔다. 손짓으로 알려 줬다.", 6.5f, false, null, "Cut_T_N2_01", 0),
                new Cut(null, "Cut_T_V7_N4_06", "이건 파, 이건 상추. 나는 따라 했다. 손에 흙이 묻었다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N4_08", "비 오는 날 마루에서 둘이 나란히 앉아 있었다.", 6.5f, false, null, "Cut_T_E7_03", 2),
                new Cut(null, "Cut_T_V7_N4_08", "아줌마가 내 어깨에 담요를 덮어 줬다. 아무 말도 없었다. 빗소리만으로 충분했다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N4_10", "부엌 벽 달력에 동그라미가 하나 있었다. 내년 봄 어느 날. 손가락으로 가리키며 물었다.", 6.5f, false, null, "Cut_T_V7_N4_01", 1),
                new Cut(null, "Cut_T_V7_N4_10", "아줌마가 종이에 썼다. 「딸 생일.」 그리고 「미역국 끓여야지」 하고 썼다. 나는 그 동그라미를 오래 봤다.", 6.5f, false, null, "Cut_T_V7_N4_01", 2),
                new Cut(null, "Cut_T_N6_08", "그날 밤 처음으로 담요가 아닌 데서 잤다. 그 집 작은 방. 벽에 낡은 주황 우비가 걸려 있었다. 어른 것이었다.", 6.5f, false, null, "Cut_S_N6_08", 3),
                new Cut(null, "Cut_T_V7_N3_13", "창밖에서 꼬마가 우비를 올려다보고 있었다. 나와 눈이 마주쳤다. 꼬마가 얼른 고개를 돌렸다. 어깨가 조금 흔들렸다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N4_14", "아침에 언덕길에서 그 남자와 마주쳤다. 남자가 언덕 위 집을 올려다봤다.", 6.5f, false, null, "Cut_T_E6_01", 0),
                new Cut(null, "Cut_T_V7_N4_14", "오래 봤다. 그리고 「안녕하세요」 하고 지나갔다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N4_14", "나는 남자의 뒷모습을 봤다. 아줌마도 마루에서 그 뒷모습을 봤다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N4_14", "사진을 꺼내려다 말았다. 남자는 그냥 갔다. 나도 방으로 들어갔다.", 6.5f, false, null, null, 0),
            }
        };

        // ── 보조 컷씬 EV5 「우유 두 병」 · BGM_M3 · CH8 · 10컷 ──
        private static readonly Def EV5 = new Def
        {
            id = "EV5", title = "우유 두 병", bgm = "BGM_M3", sat = 0.50f, cardMain = "우유 두 병", cardSub = "이야기 · 제 8화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_E3_03", "가게 앞. 그 남자가 우유 두 병을 샀다. 딸깍, 딸깍. 주인 할머니가 물었다. 「오늘도 두 개라?」 「예.」", 6.0f, false, null, "Cut_S_EB_08", 0),
                new Cut(null, "Cut_T_E3_03", "「요양 왔으민 고기도 먹어사주. 우유만 먹엉 어떵 살아.」 남자는 웃기만 했다.", 6.0f, false, null, "Cut_S_EB_08", 1),
                new Cut(null, "Cut_T_E3_03", "남자가 나를 보고 또 「안녕하세요」 했다. 나도 「안녕하세요」 했다. 매일 같은 자리, 같은 시간.", 6.0f, false, null, "Cut_S_EB_08", 2),
                new Cut(null, "Cut_T_E3_03", "남자와 눈이 마주쳤다. 나는 괜히 신발 끈을 다시 묶었다.", 6.0f, false, null, "Cut_S_EB_08", 3),
                new Cut(null, "Cut_T_V7_E5_04", "언덕길에서 아줌마가 내려오고 있었다. 나는 다가가려다 걸음을 멈췄다.", 6.0f, false, null, "Cut_T_N5_13", 1),
                new Cut(null, "Cut_T_V7_E5_04", "아줌마는 내 쪽은 보지도 않고, 손에 든 사진만 뚫어지게 보며 스쳐 지나갔다.", 6.0f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_E5_04", "매일 나에게 따뜻한 밥을 차려 주던 얼굴인데, 사진을 보는 그 표정은 텅 비어 있었다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_E5_04", "그 지독한 슬픔 때문에 마치 처음 보는 사람 같았다.", 6.0f, false, null, null, 2),
                new Cut(null, "Cut_T_N5_13", "남자는 우유 두 병을 들고 탑 쪽으로 갔다. 아줌마는 초소 쪽으로 갔다.", 6.0f, false, null, null, 0),
                new Cut(null, "Cut_T_N5_13", "두 사람의 등이 서로 반대편으로 멀어졌다. 나는 그 사이에 서 있었다. 꼬마가 내 소매를 꽉 잡고 있었다.", 6.0f, false, null, null, 1),
            }
        };

        // ── 보조 컷씬 EV6 「첫눈」 · BGM_M3 · CH9 · 6컷 ──
        private static readonly Def EV6 = new Def
        {
            id = "EV6", title = "첫눈", bgm = "BGM_M3", sat = 0.50f, cardMain = "첫눈", cardSub = "이야기 · 제 9화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E6_01", "첫눈. 꼬마와 창가 성에에 얼굴을 그렸다. 꼬마 하나, 나 하나. 꼬마가 하나 더 그렸다. 「누구야?」 「몰라.」", 6.0f, false, null, "Cut_T_E7_01", 0),
                new Cut(null, "Cut_T_E7_02", "눈사람을 만들었다. 꼬마가 내 목도리를 눈사람에게 감아 줬다. 「추우니까.」 「나는?」", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_E7_02", "「누나는 내 우비 반 줄게.」 우비 한쪽을 내 어깨에 걸쳤다. 둘 다 반만 젖었다.", 6.0f, false, null, null, 2),
                new Cut(null, "Cut_T_E2_03", "돌아오는 길에 꼬마가 자꾸 뒤를 돌아봤다. 「뭐 봐?」 「아무것도.」 두 번 물어도 아무것도.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_E7_04", "꼬마가 내 무릎을 베고 잤다. 후드 아래로 왼쪽 눈가가 접혀 있었다.", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_E7_04", "웃으면서 자는 애였다. 나는 후드 위로 머리를 한 번 만졌다. 손바닥이 따뜻했다.", 6.0f, false, null, null, 0),
            }
        };

        // ── 컷씬 5 「파란 머리띠」 · BGM_M6 · 25컷 ──
        private static readonly Def CS5 = new Def
        {
            id = "CS5", title = "파란 머리띠", bgm = "BGM_M6", sat = 0.50f, cardMain = "파란 머리띠",
            cuts = new[]
            {
                new Cut(null, "Cut_T_OP_07", "봄. 아줌마 집 장롱 깊은 데서 노란 통이 나왔다. 뚜껑에 붉은 하트. 아줌마는 집에 없었다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N5_02", "뚜껑을 열었다. 파란 보석 머리띠. 한 번도 안 쓴 새것이었다.", 6.5f, false, null, "Cut_T_N8_16", 1),
                new Cut(null, "Cut_T_N1_02", "머리띠를 집는 순간, 머리가 깨질 듯 아팠다. 기억이 한꺼번에 밀고 들어왔다. 그런데 이상했다.", 6.5f, false, null, "Cut_S_N1_02", 2),
                new Cut(null, "Cut_T_N2_05", "엄마 얼굴만 안 보였다. 손과 해녀복과 숨소리만 돌아왔다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_OP_01", "처음 가 본 시내 백화점. 유리 안에 파란 보석 머리띠가 있었다. 나는 오래 봤다. 갖고 싶다는 말은 안 했다.", 6.5f, true, "— 회상 · 열한 살 —", null, 1),
                new Cut(null, "Cut_T_OP_01", "그 말이 사치라는 건 열한 살도 알았다. 유리에 손자국만 남기고 돌아섰는데, 아빠가 그 손자국을 보고 있었다.", 6.5f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_V7_N5_07", "그을린 얼굴, 낡은 점퍼. 아빠가 웃었다. 왼쪽 눈이 접혔다. 「하늘이 생일 선물이 정해졌주.」", 6.5f, true, "— 회상 —", "Cut_T_N2_06", 3),
                new Cut(null, "Cut_T_V7_N5_08", "아빠는 내 물건마다 빨간 페인트로 하트를 그려 줬다. 가방에도, 신발에도. 삐뚤었다.", 6.5f, true, "— 회상 —", "Cut_T_N2_07", 2),
                new Cut(null, "Cut_T_V7_N5_08", "「잃어버리지 말라.」 아빠 주황 우비 소매에도 같은 하트가 있었다.", 6.5f, true, "— 회상 —", "Cut_T_N2_07", 0),
                new Cut(null, "Cut_T_OP_02", "해녀 엄마, 배 한 척뿐인 아빠. 머리띠 값은 아빠 한 달 벌이였다.", 6.5f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_OP_03", "아빠는 이웃에게 돈을 빌려 머리띠를 사 왔고, 빌린 돈을 갚으려면 풍랑주의보가 내린 밤에도 바다에 나가야 했다.", 6.5f, true, "— 회상 —", null, 2),
                new Cut(null, "Cut_T_OP_04", "아빠가 장화를 신었다. 엄마가 아빠 우비를 낚아챘다. 소리는 없었다. 엄마 손이 빠르게 허공을 갈랐다.", 6.5f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_OP_04", "가지 마. 엄마 얼굴이 어땠는지는 떠오르지 않는다. 그 손만 떠오른다. 아빠는 그 손을 안 봤다.", 6.5f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_OP_05", "나는 대문턱에서 아빠 소매를 두 손으로 잡았다. 「아빠, 생일 안 해도 돼.」 아빠가 내 머리를 한 번 만졌다.", 6.5f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_OP_05", "「한 마리만 더 잡으민 뒈어, 하늘아.」 하트가 그려진 소매가 손에서 빠져나갔다.", 6.5f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_OP_05", "젖은 소매의 감촉이 아직도 손에 있다.", 6.5f, true, "— 회상 —", null, 2),
                new Cut(null, "Cut_T_V6_N2_11", "거짓말처럼 맑은 아침이었다. 사람들이 바다로 뛰었다.", 6.5f, true, "— 회상 · 다음 날 —", null, 0),
                new Cut(null, "Cut_T_V7_N5_18", "엄마가 나를 방에 넣고 문을 닫았다. 문틈으로 주황색이 한 번 지나갔다.", 6.5f, true, "— 회상 —", "Cut_T_N6_08", 1),
                new Cut(null, "Cut_T_N2_10", "송전탑 아래 바다에 아빠 배가 뒤집혀 있었고, 삐뚤어진 하트가 그려진 부표 하나만 떠 있었다.", 6.5f, true, "— 회상 —", "Cut_S_N2_09", 2),
                new Cut(null, "Cut_T_V7_N5_20", "아빠는 돌아오지 않았다. 나는 울지 않았다. 울면 진짜가 될 것 같았다.", 6.5f, true, "— 회상 —", "Cut_T_OP_08", 3),
                new Cut(null, "Cut_T_OP_07", "엄마는 그날 노란 통을 열어 보지도 않고 장롱 깊은 데 넣었다. 그날부터 우리 집엔 생일이 없어졌다.", 6.5f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N3_02", "아빠가 죽은 뒤, 우리 집은 완전히 소리가 사라졌다. 학교에서는 매일 같은 일이 있었다. 「생선 냄새 나.」", 6.5f, true, "— 회상 —", "Cut_S_N3_02", 0),
                new Cut(null, "Cut_T_N3_03", "「벙어리 딸.」 아이들이 도시락을 개수대에 붓고, 가방을 도랑에 던지고, 입만 뻐끔거리며 엄마 흉내를 냈다.", 6.5f, true, "— 회상 —", "Cut_S_N3_03", 3),
                new Cut(null, "Cut_T_V7_N5_24", "나는 매일 못 들은 척했다. 엄마처럼. 집에 와서도 엄마한테 아무 말도 안 했다.", 6.5f, true, "— 회상 —", "Cut_T_N3_02", 2),
                new Cut(null, "Cut_T_V7_N5_24", "말해도 못 들으니까. 말할 사람이 없었다. 그게 제일 외로웠다.", 6.5f, true, "— 회상 —", null, 0),
            }
        };

        // ── 보조 컷씬 EV7 「흰 셔츠」 · BGM_M6 · CH11 · 7컷 ──
        private static readonly Def EV7 = new Def
        {
            id = "EV7", title = "흰 셔츠", bgm = "BGM_M6", sat = 0.42f, cardMain = "흰 셔츠", cardSub = "이야기 · 제 11화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_OP_09", "전학생이 왔다. 서울에서. 몸이 아파서 요양하러 제주에 왔다고 했다.", 6.0f, true, "— 회상 · 열두 살 봄 —", "Cut_S_OP_4", 0),
                new Cut(null, "Cut_T_OP_09", "흰 셔츠가 눈부셨고, 얼굴은 셔츠만큼 하얬다. 아이들이 그 애를 둘러쌌고 나는 늘 그렇듯 구석에 있었다.", 6.0f, true, "— 회상 —", "Cut_S_OP_4", 1),
                new Cut(null, "Cut_T_N3_04", "가방이 또 도랑에 던져진 날, 그 애가 도랑에 들어갔다. 흰 셔츠가 진흙투성이가 되도록 가방을 건져 왔다.", 6.0f, true, "— 회상 —", "Cut_S_N3_10", 2),
                new Cut(null, "Cut_T_N3_06", "아이들이 그 애도 밀었다. 그 애는 넘어졌다. 다시 일어나서 내 앞에 섰다. 손이 떨리는 걸 나만 봤다.", 6.0f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_N3_06", "「싸움도 못하면서 왜 앞을 막아?」 「네가 뒤에 있으니까.」", 6.0f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N3_05", "그 애는 운동장 구석에 앉아 도시락을 열고 한가운데 선을 그었다. 「반은 네 거.」 계란말이가 따뜻했다.", 6.0f, true, "— 회상 —", "Cut_S_N3_08", 0),
                new Cut(null, "Cut_T_N3_05", "나는 그날 처음으로 학교에서 밥을 먹었다. 그 애 이름은 도윤이었다. 도윤이 얼굴은 기억난다. 그 애만은.", 6.0f, true, "— 회상 —", "Cut_S_N3_08", 3),
            }
        };

        // ── 컷씬 6 「우리 기지」 · BGM_M4 · 19컷 ──
        private static readonly Def CS6 = new Def
        {
            id = "CS6", title = "우리 기지", bgm = "BGM_M4", sat = 0.42f, cardMain = "우리 기지",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N6_01", "그해 봄, 도윤이와 나는 매일 바다에서 놀았다. 도윤이는 서울 애라 수영을 못 했다.", 6.5f, true, "— 회상 —", "Cut_T_N4_08", 0),
                new Cut(null, "Cut_T_V7_N6_01", "나는 얕은 물에서 수영을 가르쳤다. 도윤이는 물을 먹고, 기침을 하고, 다시 들어왔다.", 6.5f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N4_08", "해가 지면 방파제에 나란히 앉아 젖은 옷을 말렸다. 그전까지 나는 웃는 법을 잊고 있었다.", 6.5f, true, "— 회상 —", null, 2),
                new Cut(null, "Cut_T_N4_08", "그 봄에 소리 내서 웃었다. 「너 웃을 줄 아네.」 「시끄러워.」", 6.5f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_V7_N6_05", "도윤이가 바닷가 바위 틈의 버려진 해녀 불턱을 기지로 꾸몄다.", 6.5f, true, "— 회상 —", "Cut_T_N3_07", 1),
                new Cut(null, "Cut_T_V7_N6_05", "돌담 안에 담요를 깔고, 고장 난 라디오를 가져다 놓고, 우유를 두 병씩 숨겨 뒀다.", 6.5f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_N3_09", "「여긴 우리 기지야.」 라디오를 같이 뜯다가 89.2에서 희미하게 노래가 잡혔다.", 6.5f, true, "— 회상 —", "Cut_S_N3_07", 3),
                new Cut(null, "Cut_T_N3_09", "도윤이가 다이얼 옆에 하트를 그렸다. 「이 주파수는 우리 거야.」", 6.5f, true, "— 회상 —", "Cut_S_N3_07", 2),
                new Cut(null, "Cut_T_N3_08", "기지까지는 방파제 길을 보드로 달렸다. 도윤이는 뒤에서 헐떡이며 따라왔다.", 6.5f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_N4_02", "기지에 들어가니 촛불이 켜져 있었다. 초 열두 개. 도윤이가 케이크 뒤에서 튀어나왔다.", 6.5f, true, "— 회상 · 열두 살 생일 —", "Cut_S_N4_05", 1),
                new Cut(null, "Cut_T_N4_02", "「깜짝 선물!」 나는 웃었다. 촛불을 끄고, 케이크를 반씩 먹었다.", 6.5f, true, "— 회상 —", "Cut_S_N4_05", 2),
                new Cut(null, "Cut_T_V7_N6_12", "해가 기울자 도윤이가 내 손을 끌었다. 「나머지는 송전탑 가서 먹자. 거기서 노을이 제일 예뻐.」", 6.5f, true, "— 회상 —", "Cut_T_N4_04", 3),
                new Cut(null, "Cut_T_V7_N6_12", "손을 뿌리쳤다. 「거기 가자는 말 하지 마.」 「왜? 딱 한 번만.」 「싫다고!」", 6.5f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_V7_N6_14", "케이크 접시가 바위에 떨어졌다. 「모르면서 왜 자꾸 끼어들어!」 도윤이는 한참 나를 봤다.", 6.5f, true, "— 회상 —", "Cut_T_N4_04", 0),
                new Cut(null, "Cut_T_V7_N6_14", "「……미안.」 그리고 갔다. 나는 엎어진 케이크 앞에 혼자 남았다. 촛농이 바위 위에서 식어 갔다.", 6.5f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_N4_05", "열흘 동안 도윤이는 오지 않았다. 대신 담장 위에 매일 도시락이 놓였다. 첫날은 따뜻했고 열흘째엔 식어 있었다.", 6.5f, true, "— 회상 —", "Cut_S_N4_07", 2),
                new Cut(null, "Cut_T_N4_06", "마지막 도시락 뚜껑 안에 글씨가 있었다. 「친구 아니어도 반은 네 거.」 그날 밤 도윤이 집 앞을 지나갔다.", 6.5f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_V7_N6_18", "창가에 도윤이가 앉아 송전탑 쪽을 보고 있었다. 오래. 방 벽에 그림이 붙어 있었다.", 6.5f, true, "— 회상 —", "Cut_T_N6_05", 1),
                new Cut(null, "Cut_T_V7_N6_18", "송전탑, 노을, 그 아래 케이크를 든 둘. 나는 담 밑에서 그걸 보고 올라가지 못했다.", 6.5f, true, "— 회상 —", null, 2),
            }
        };

        // ── 보조 컷씬 EV8 「태풍」 · BGM_M3 · CH13 · 10컷 ──
        private static readonly Def EV8 = new Def
        {
            id = "EV8", title = "태풍", bgm = "BGM_M3", sat = 0.42f, cardMain = "태풍", cardSub = "이야기 · 제 13화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E8_01", "그 뒤로 도윤이와 나는 말을 하지 않았다. 학교 복도에서 도윤이가 먼저 다가왔다. 「하늘아, 그날……」", 6.0f, true, "— 회상 · 여름 끝 —", "Cut_T_N4_07", 0),
                new Cut(null, "Cut_T_V7_E8_01", "「됐어. 다시는 말 걸지 마.」 도윤이는 입을 다물었다. 다음 날부터 담장 위에 도시락이 놓이지 않았다.", 6.0f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_V7_E8_03", "혼자 기지에 가 봤다. 담요는 비에 젖어 있었고, 라디오는 꺼져 있었다. 나는 켜지 않고 돌아왔다.", 6.0f, true, "— 회상 —", "Cut_T_N3_11", 2),
                new Cut(null, "Cut_T_V7_E8_04", "비가 앞이 안 보이게 쏟아졌다. 양철 지붕이 쉬지 않고 울렸다. 전화가 끊기고 전기도 나갔다.", 6.0f, true, "— 회상 · 여름 태풍 —", "Cut_T_E5_01", 3),
                new Cut(null, "Cut_T_V7_E8_05", "촛불을 켰는데 마당에서 소리가 났다. 엄마가 엎어져 있었다. 가슴을 움켜쥐고.", 6.0f, true, "— 회상 —", "Cut_T_N5_03", 1),
                new Cut(null, "Cut_T_V7_E8_05", "해녀들이 걸리는 심장병이었다. 엄마 얼굴은 이번에도 안 보인다. 젖은 해녀복과 가슴을 쥔 손만 보인다.", 6.0f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_V7_E8_04", "우리 집은 언덕 위 외딴집이었다. 제일 가까운 집까지 30분, 병원까지 2킬로. 빗속에 길이 안 보였다.", 6.0f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_V7_E8_08", "「엄마! 엄마!」 나는 울부짖었다. 엄마는 못 듣는다. 알면서도 불렀다. 부르는 것밖에 할 게 없었다.", 6.0f, true, "— 회상 —", "Cut_T_N5_03", 2),
                new Cut(null, "Cut_T_V7_E8_08", "마당 끝까지 뛰어가 소리를 질렀다. 「누가 좀 도와주세요!」 빗소리가 내 목소리를 삼켰다. 아무도 없었다.", 6.0f, true, "— 회상 —", null, 0),
                new Cut(null, "Cut_T_V7_E8_10", "엄마 손이 점점 차가워졌다. 나는 엄마를 안고 울었다. 열두 살이었다. 할 수 있는 게 아무것도 없었다.", 6.0f, true, "— 회상 —", "Cut_T_N5_03", 1),
            }
        };

        // ── 보조 컷씬 EV9 「리어카」 · BGM_M3 · CH14 · 9컷 ──
        private static readonly Def EV9 = new Def
        {
            id = "EV9", title = "리어카", bgm = "BGM_M3", sat = 0.32f, cardMain = "리어카", cardSub = "이야기 · 제 14화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E9_01", "빗속에서 손전등 불빛이 올라왔다. 도윤이였다. 우비도 없이 흠뻑 젖어서. 「너 걱정돼서.」", 6.0f, true, "— 회상 —", "Cut_T_N5_04", 0),
                new Cut(null, "Cut_T_N5_04", "「어떻게 왔어?」 「뛰었어.」 도윤이가 엄마를 보고 얼굴이 굳었다. 그래도 말했다.", 6.0f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N5_04", "「살 수 있어.」 그렇게 말해야 내가 움직이니까. 그 애도 몰랐으면서.", 6.0f, true, "— 회상 —", null, 2),
                new Cut(null, "Cut_T_N5_05", "길가에 낡은 리어카가 있었다. 바퀴가 진흙에 빠져 있었다. 둘이서 끌어냈다. 손톱이 갈라졌다.", 6.0f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_N5_05", "엄마를 눕히고 도윤이 겉옷을 덮었다. 병원까지 2킬로. 도윤이가 앞에서 끌고 내가 뒤에서 밀었다.", 6.0f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N5_06", "빗길에 몇 번이나 미끄러졌다. 도윤이 무릎이 꺾였다. 다시 일어났다. 「먼저 가.」", 6.0f, true, "— 회상 —", "Cut_S_N5_08", 0),
                new Cut(null, "Cut_T_N5_07", "「싫어. 셋이 같이 가.」 도윤이가 울면서 말했다. 나도 울면서 밀었다. 병원 불빛이 보였다.", 6.0f, true, "— 회상 —", null, 3),
                new Cut(null, "Cut_T_N5_09", "엄마는 살았다. 의사가 그렇게 말했다. 나는 복도 바닥에 주저앉았다.", 6.0f, true, "— 회상 —", "Cut_S_N5_10", 2),
                new Cut(null, "Cut_T_N5_09", "도윤이가 옆에 앉았다. 「거봐. 살 수 있다고 했잖아.」 그 애 손이 아직 떨리고 있었다.", 6.0f, true, "— 회상 —", "Cut_S_N5_10", 0),
            }
        };

        // ── 보조 컷씬 EV2 「스무 살 생일」 · BGM_M1 · CH15 · 10컷 ──
        private static readonly Def EV2 = new Def
        {
            id = "EV2", title = "스무 살 생일", bgm = "BGM_M1", sat = 0.78f, cardMain = "스무 살 생일", cardSub = "이야기 · 제 15화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_N5_09", "응급실 문이 닫히자 도윤이가 복도에 쓰러졌다. 늘 입술이 파랗게 질려 있던 이유를 나는 그때 처음 알았다.", 6.0f, true, "— 회상 —", "Cut_S_N5_10", 0),
                new Cut(null, "Cut_T_N6_01", "다음 날 도윤이네 집 앞에 검은 차가 서 있었다. 짐이 실리고 있었다.", 6.0f, true, "— 회상 —", "Cut_S_N6_02", 1),
                new Cut(null, "Cut_T_N6_01", "「서울 병원으로 가야 한대.」 도윤이 엄마가 울면서 말했다. 「오늘.」", 6.0f, true, "— 회상 —", "Cut_S_N6_02", 2),
                new Cut(null, "Cut_T_V7_E2_04", "나는 병원에서 엄마 곁에 있다가 그 말을 들었다. 달렸다. 병원에서 도윤이네 집까지 쉬지 않고 달렸다.", 6.0f, true, "— 회상 —", "Cut_T_N4_07", 3),
                new Cut(null, "Cut_T_V7_E2_04", "숨이 찼다. 그래도 달렸다. 차가 이미 출발하고 있었다. 도윤이가 뒷유리에 붙어서 나를 봤다.", 6.0f, true, "— 회상 —", null, 1),
                new Cut(null, "Cut_T_N6_02", "창을 내렸다. 「하늘아! 스무 살 네 생일에 송전탑 아래서 보자! 꼭!」", 6.0f, true, "— 회상 —", "Cut_S_N6_03", 0),
                new Cut(null, "Cut_T_N6_03", "「우유 두 병 들고 기다릴게! 하나는 오늘, 하나는 내일!」", 6.0f, true, "— 회상 —", "Cut_S_N6_04", 3),
                new Cut(null, "Cut_T_V7_E2_08", "「알았어! 안 늦을게!」 나는 차를 따라 달리다 넘어졌다. 무릎에서 피가 났다. 안 아팠다.", 6.0f, true, "— 회상 —", "Cut_T_V7_OP_07", 2),
                new Cut(null, "Cut_T_N6_04", "그날 밤 달력에 처음으로 글씨를 썼다. 「스무 살 생일, 송전탑 아래, 도윤이.」", 6.0f, true, "— 회상 —", "Cut_S_N6_06", 0),
                new Cut(null, "Cut_T_N6_04", "연필을 꾹 눌러서 종이가 팼다. 나는 그 약속 하나로 여덟 해를 살았다.", 6.0f, true, "— 회상 —", "Cut_S_N6_06", 1),
            }
        };

        // ── 컷씬 7 「전부 알아버렸다」 · BGM_M1 · 24컷 ──
        private static readonly Def CS7 = new Def
        {
            id = "CS7", title = "전부 알아버렸다", bgm = "BGM_M1", sat = 0.32f, cardMain = "전부 알아버렸다",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N7_01", "머리띠를 든 채로 마루에 앉아 있었다. 밖이 어두워져 있었다. 나는 하늘이었다. 고하늘. 이 집 딸.", 6.5f, false, null, "Cut_T_N7_06", 0),
                new Cut(null, "Cut_T_V7_N7_02", "마당에 아줌마가 들어왔다. 회상에서 비어 있던 엄마 얼굴에 그 얼굴이 들어갔다.", 6.5f, false, null, "Cut_T_N2_05", 1),
                new Cut(null, "Cut_T_V7_N7_03", "많이 야윈 얼굴. 엄마였다. 사진을 품에 넣고 초소에서 돌아오는 엄마.", 6.5f, false, null, "Cut_T_E6_04", 2),
                new Cut(null, "Cut_T_V7_N7_03", "1년 동안 매일 나를 찾으러 다닌 엄마. 딸 같다고 밥을 해 준 엄마. 나를 앞에 두고.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N7_05", "엄마가 나를 보고 손을 흔들었다. 모르는 애한테 하는 손짓.", 6.5f, false, null, "Cut_T_E6_04", 1),
                new Cut(null, "Cut_T_V7_N7_05", "나는 엄마 얼굴을 봤다. 이제 알았다. 아는데, 엄마는 나를 몰랐다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_N7_07", "가게 앞으로 달렸다. 멀리 방파제 끝에 흰 셔츠가 보였다. 우유 두 병. 도윤이었다.", 6.5f, false, null, "Cut_T_N3_13", 3),
                new Cut(null, "Cut_T_V7_N7_07", "매일 「안녕하세요」 하던 사람.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N7_07", "「도윤아!」 불렀다. 바람이 소리를 삼켰다. 도윤이는 돌아보지 않았다. 흰 셔츠가 점점 작아졌다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N8_11", "꼬마가 뒤에 서 있었다. 주황 우비. 소매 끝에 빨간 하트가 삐뚤게 그려져 있었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_E4_03", "주머니 속 조개껍데기를 꺼냈다. 같은 하트였다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_N8_12", "꼬마가 후드를 벗었다. 웃었다. 왼쪽 눈이 접혔다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N8_12", "「……아빠?」 「한 마리만 더 잡으민 뒈어, 하늘아.」", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N8_13", "나는 주저앉았다. 「늦게 와서 미안.」", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N6_07", "마지막 기억이 돌아왔다. 열아홉 생일. 엄마는 가슴이 아파 육지 병원에 가 있었다.", 6.5f, false, null, "Cut_S_N6_07", 3),
                new Cut(null, "Cut_T_V7_N7_16", "나는 혼자 엄마 해녀복을 입고 물에 들어갔다. 물이 차가웠다. 폐그물이 발목을 감았다.", 6.5f, false, null, "Cut_T_N6_11", 2),
                new Cut(null, "Cut_T_OP_13", "수면이 점점 멀어졌다. 아무도 없었다. 그 뒤는 없다.", 6.5f, false, null, "Cut_S_N6_05", 0),
                new Cut(null, "Cut_T_N8_09", "죽은 사람은 산 사람 얼굴부터 잊어버린다. 나는 살아 돌아온 게 아니었다.", 6.5f, false, null, "Cut_S_N8_06", 1),
                new Cut(null, "Cut_T_N7_13", "손을 봤다. 손끝이 흐려지고 있었다. 꼬마가 내 손을 잡았다.", 6.5f, false, null, "Cut_S_N8_05", 2),
                new Cut(null, "Cut_T_N7_11", "「기억을 다 찾으면 우리 딸이 떠나야 하니까. 그래서 아빠가 모른 척했어.」", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N7_11", "「그럼 왜 그 집에 가는 걸 안 말렸어.」", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N8_13", "「우리 딸이 엄마를 못 알아봐도, 엄마 밥은 먹었으면 해서.」 꼬마가 울었다. 아빠가 울었다.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N7_10", "나는 유리창을 봤다. 사진 속 그 얼굴이 거기 있었다. 열아홉의 나.", 6.5f, false, null, "Cut_S_N7_12", 3),
                new Cut(null, "Cut_T_N7_05", "이제야 나로 보였다. 다른 사람들 눈엔 끝까지 다른 사람이었을 얼굴.", 6.5f, false, null, null, 2),
            }
        };

        // ── 보조 컷씬 EV10 「안을 수 없는」 · BGM_M3 · CH19 · 11컷 ──
        private static readonly Def EV10 = new Def
        {
            id = "EV10", title = "안을 수 없는", bgm = "BGM_M3", sat = 0.25f, cardMain = "안을 수 없는", cardSub = "이야기 · 제 19화",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_E10_01", "언덕 위 집으로 달렸다. 엄마가 부엌에서 미역국을 끓이고 있었다.", 6.0f, false, null, "Cut_T_V6_N8_06", 0),
                new Cut(null, "Cut_T_V7_N4_10", "벽에 걸린 달력의 동그라미가 내일이었다. 딸 생일. 내 생일.", 6.0f, false, null, "Cut_T_OP_14", 1),
                new Cut(null, "Cut_T_V7_E10_03", "「엄마.」 엄마는 못 듣는다. 나는 엄마 앞에 서서 입 모양으로 말했다.", 6.0f, false, null, "Cut_T_N8_06", 2),
                new Cut(null, "Cut_T_V7_E10_03", "「엄마, 나야. 하늘이.」 엄마는 국자만 저었다. 내 쪽으로 고개를 들지 않았다.", 6.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_E10_05", "엄마를 안았다. 두 팔이 엄마 어깨를 통과했다.", 6.0f, false, null, "Cut_T_N8_06", 1),
                new Cut(null, "Cut_T_V7_E10_05", "품 안에 찬 공기뿐이었다. 다시 안았다. 또 통과했다. 엄마는 춥다는 듯 옷깃을 여몄다.", 6.0f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_E10_07", "엄마가 국을 두 그릇 펐다. 빈 마루를 둘러보더니, 작은 방 문을 열어 봤다.", 6.0f, false, null, "Cut_T_V6_N8_06", 3),
                new Cut(null, "Cut_T_V7_E10_07", "종이에 「밥 먹자」라고 써서 마루에 올려놓았다.", 6.0f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_E10_09", "그러고는 대문 밖으로 나가 언덕 아래를 한참이나 서성였다. 나는 엄마 바로 뒤에 서 있었다.", 6.0f, false, null, "Cut_T_N8_07", 0),
                new Cut(null, "Cut_T_V7_E10_07", "엄마 옆에 앉아 미역국이 끓는 걸 봤다. 먹을 수 없다는 걸 알면서.", 6.0f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_E10_09", "그동안 이 엄마가 해 준 밥을 먹었다. 그걸로 됐다고, 그렇게 생각하려고 했다. 안 됐다. 안고 싶었다.", 6.0f, false, null, null, 2),
            }
        };

        // ── 컷씬 8 「송전탑 아래」 · BGM_M5 · 26컷 ──
        private static readonly Def CS8 = new Def
        {
            id = "CS8", title = "송전탑 아래", bgm = "BGM_M5", sat = 0.25f, cardMain = "송전탑 아래",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_N8_01", "스무 살 생일. 노을이 탑 아래로 내려앉았다. 도윤이가 우유 두 병을 들고 서 있었다. 여덟 해 만의 약속 자리에.", 6.5f, false, null, "Cut_T_N8_01", 0),
                new Cut(null, "Cut_T_V7_N8_01", "나는 늦지 않았다. 도윤이 앞에 섰다. 「나 왔어.」 도윤이는 내 어깨 너머 언덕길만 봤다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N8_03", "「5분 지나면 화낼 거야.」 도윤이가 애써 웃으며 혼잣말을 했다.", 6.5f, false, null, "Cut_T_V7_N8_01", 2),
                new Cut(null, "Cut_T_V7_N8_03", "「10분 지나면 더 오래 기다릴 거야. 그러니까 빨리 와.」 나는 바로 앞에 있었다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N8_05", "그때 엄마가 언덕을 올라왔다. 품에 흰 국화를 안고 있었다. 걸음이 휘청거렸다.", 6.5f, false, null, "Cut_T_N8_07", 1),
                new Cut(null, "Cut_T_V7_N8_06", "엄마는 초소에서 받은 팩스 종이를 도윤이에게 내밀었다. 『오늘 새벽. 탑 남쪽 폐그물 아래. 시신 인양.』", 6.5f, false, null, "Cut_T_N8_03", 0),
                new Cut(null, "Cut_T_N8_05", "종이를 읽은 도윤이가 우유 두 병을 끌어안은 채 바닥으로 무너졌다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N8_05", "「하나는 오늘, 하나는 내일이라며……. 내일 건 누가 마셔.」", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_N8_09", "엄마는 소리 내어 울지 못했다. 무너진 도윤이의 등을 껴안고 짐승처럼 헐떡이며 눈물만 쏟았다.", 6.5f, false, null, "Cut_T_N8_02", 0),
                new Cut(null, "Cut_T_N8_09", "그 종이를 본 순간, 내 발끝이 투명해지기 시작했다. 바다 밑에 묶여 있던 내 몸이 드디어 뭍으로 올라온 것이다.", 6.5f, false, null, "Cut_S_N8_06", 1),
                new Cut(null, "Cut_T_N8_09", "이승에 남겨뒀던 마지막 미련이 풀리자, 머물 수 있는 시간도 끝이 났다.", 6.5f, false, null, "Cut_S_N8_06", 2),
                new Cut(null, "Cut_T_N8_12", "꼬마가 내 옆에 섰다. 주황 우비를 벗어 바닥에 내려놓았다. 「아빠.」 「응.」", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N8_12", "「나 이제 가나 봐.」 「응. 바닷물 찼지. 이제 춥지 않은 데로 가, 우리 딸.」", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N8_12", "「아빠, 무서워.」 「괜찮아. 아빠가 여기서 보고 있을게. 1년 동안 하늘이 혼자 안 둬서 다행이다.」", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N8_14", "멀리서 「민재야!」 부르는 소리가 들렸다. 뭍 말씨였다. 유채밭 사이로 한 여자가 달려오고 있었다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_N8_16", "여자의 손에 다 해진 전단지. 『민재를 찾습니다.』 전화번호 앞자리가 뭍 것이었다. 날짜는 작년 봄.", 6.5f, false, null, "Cut_T_N8_14", 2),
                new Cut(null, "Cut_T_N8_11", "아빠가 나를 보고 웃었다. 왼쪽 눈이 접히는, 내가 가장 사랑했던 얼굴.", 6.5f, false, null, null, 0),
                new Cut(null, "Cut_T_N8_14", "눈을 깜빡이는 순간, 그 웃음이 지워졌다. 나를 보는 꼬마의 눈에 더 이상 아빠는 없었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_N8_14", "그 시선은 나를 완전히 잃어버린 채, 유채밭 쪽만 멍하니 향해 있었다.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_N8_15", "「엄마한테 가.」 내 말에 꼬마는 뒤도 돌아보지 않고 유채밭으로 뛰어갔다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_N8_15", "아빠를 두 번 보냈다. 이번엔 진짜 마지막이었다.", 6.5f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_N8_22", "나는 무릎을 꿇고 도윤이와 엄마를 안았다. 품이 텅 비어 있었다. 내 몸은 이미 절반 이상 빛으로 부서지고 있었다.", 6.5f, false, null, "Cut_T_N8_13", 0),
                new Cut(null, "Cut_T_V7_N8_22", "「울지 마. 나 여기 왔어. 약속 지켰어.」 손이 도윤이의 뺨을, 엄마의 젖은 어깨를 통과했다.", 6.5f, false, null, null, 3),
                new Cut(null, "Cut_T_E10_03", "도윤이 발치에 낡은 라디오가 놓여 있었다. 다이얼 옆에 삐뚤어진 하트.", 6.5f, false, null, null, 2),
                new Cut(null, "Cut_T_N3_11", "사라지기 직전, 나는 남은 힘을 다해 라디오 다이얼을 돌렸다. 89.2. 찌르르, 잡음이 일었다.", 6.5f, false, null, "Cut_S_N3_11", 0),
                new Cut(null, "Cut_T_V7_N8_26", "도윤이의 눈에서 눈물이 흘러내렸다.", 6.5f, false, null, "Cut_T_N8_05", 1),
            }
        };

        // ── 엔딩 A 「국화와 우유」 · BGM_M3 · 6컷 ──
        private static readonly Def EndA = new Def
        {
            id = "END_A", title = "국화와 우유", bgm = "BGM_M3", sat = 0.35f, cardMain = "국화와 우유", cardSub = "엔딩 A",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_EA_01", "며칠 뒤. 바다가 보이는 언덕에 작은 무덤이 생겼다. 비석에 이름 하나. 고하늘.", 7.0f, false, null, "Cut_T_ET_10", 0),
                new Cut(null, "Cut_T_V7_EA_02", "도윤이가 무덤 앞에 흰 국화를 내려놓았다. 그 옆에 우유 두 병을 나란히 세웠다.", 7.0f, false, null, "Cut_T_EA_05", 1),
                new Cut(null, "Cut_T_V7_EA_03", "도윤이는 한참 비석을 봤다. 바람이 국화를 흔들었다.", 7.0f, false, null, "Cut_T_V7_EA_02", 2),
                new Cut(null, "Cut_T_V7_EA_02", "도윤이는 한참 서 있다가 돌아섰다.", 7.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_EA_05", "무덤 앞에 국화와 우유 두 병이 남았다.", 7.0f, false, null, "Cut_T_EA_05", 1),
                new Cut(null, "Cut_T_EA_09", "그리고……", 7.0f, false, null, null, 0),
            }
        };

        // ── 엔딩 B 「미역국」 · BGM_M6 · 8컷 ──
        private static readonly Def EndB = new Def
        {
            id = "END_B", title = "미역국", bgm = "BGM_M6", sat = 0.50f, cardMain = "미역국", cardSub = "엔딩 B",
            cuts = new[]
            {
                new Cut(null, "Cut_T_EB_09", "이승을 떠난 영혼의 흔적은 세상에서 지워졌다.", 7.0f, false, null, null, 0),
                new Cut(null, "Cut_T_N1_06", "마을 사람들은 지난 1년 동안 언덕을 오르내리던 '낯선 여자애'를 모두 잊었다.", 7.0f, false, null, "Cut_S_N1_04", 1),
                new Cut(null, "Cut_T_E1_01", "가게 할머니도, 초소 아저씨도. 「우리 마을에 그런 애가 있었나.」", 7.0f, false, null, null, 2),
                new Cut(null, "Cut_T_V7_EB_04", "엄마만 기억했다. 마루에는 「밥 먹자」라고 쓴 종이가 그대로 있었다.", 7.0f, false, null, "Cut_T_V6_N8_06", 3),
                new Cut(null, "Cut_T_V7_E10_07", "엄마가 미역국을 끓였다. 빈 마루에 상을 올려놓았다.", 7.0f, false, null, null, 1),
                new Cut(null, "Cut_T_V7_EB_06", "엄마는 마루 끝에 앉아 먼 바다를 봤다. 해가 질 때까지.", 7.0f, false, null, "Cut_T_N8_07", 0),
                new Cut(null, "Cut_T_V7_EB_06", "등 뒤에서 누가 불렀다. 「엄마.」", 7.0f, false, null, null, 3),
                new Cut(null, "Cut_T_V7_EB_08", "엄마가 천천히 뒤를 돌아봤다. 그리고 환한 얼굴로 손짓했다.", 7.0f, false, null, "Cut_T_V6_N8_06", 2),
            }
        };

        // ── 진엔딩 「도윤아」 · BGM_M1 · 10컷 ──
        private static readonly Def EndTrue = new Def
        {
            id = "END_TRUE", title = "도윤아", bgm = "BGM_M1", sat = 0.25f, cardMain = "도윤아", cardSub = "진엔딩",
            cuts = new[]
            {
                new Cut(null, "Cut_T_V7_EA_01", "며칠 뒤. 바다가 보이는 언덕에 작은 무덤이 생겼다. 비석에 이름 하나. 고하늘.", 7.0f, false, null, null, 0),
                new Cut(null, "Cut_T_V7_EA_02", "도윤이가 무덤 앞에 흰 국화를 내려놓았다. 그 옆에 우유 두 병을 나란히 세웠다.", 7.0f, false, null, null, 1) { sat = 1.00f },
                new Cut(null, "Cut_T_V7_EA_03", "도윤이는 한참 비석을 봤다. 바람이 국화를 흔들었다.", 7.0f, false, null, null, 2) { sat = 1.00f },
                new Cut(null, "Cut_T_V7_EA_02", "도윤이는 한참 서 있다가 돌아섰다.", 7.0f, false, null, null, 3) { sat = 1.00f },
                new Cut(null, "Cut_T_V7_EA_05", "무덤 앞에 국화와 우유 두 병이 남았다.", 7.0f, false, null, null, 1) { sat = 1.00f },
                new Cut(null, "Cut_T_EA_09", "그리고……", 7.0f, false, null, null, 0) { sat = 1.00f },
                new Cut(null, "Cut_T_EA_01", "읍내 정류장. 공항 가는 버스가 섰다. 도윤이가 가방을 메고 계단에 발을 올렸다.", 7.0f, false, null, null, 3) { sat = 1.00f },
                new Cut(null, "Cut_T_ET_02", "그 순간, 뒤에서 누가 불렀다. 「도윤아?」", 7.0f, false, null, null, 2) { sat = 1.00f },
                new Cut(null, "Cut_T_ET_07", "도윤이가 돌아봤다. 세상이 하얗게 번졌다. 모든 것이 밝게 빛났다.", 7.0f, false, null, null, 0) { sat = 1.00f },
                new Cut(null, "Cut_T_V7_ET_10", "", 7.0f, false, null, "Cut_T_ET_10", 1) { sat = 1.00f },
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
                new Cut(null, "Cut_V_MV_2", "너와 나의 주파수, 89.2", 4.5f, false, null, "Cut_V_OP_2", 1),
                new Cut(null, "Cut_V_MV_3", "하나는 오늘, 하나는 내일", 4.5f, false, null, "Cut_V_CH14_Close", 2),
                new Cut(null, "Cut_V_MV_4", "노을 전에 닿으면 내가 이기는 놀이", 4.5f, false, null, "Cut_V_Open_7", 0),
                new Cut("mv5", "Cut_V_MV_5", "우리의 송전탑", 4.5f, false, null, "Cut_V_Open_3", 3),
                new Cut(null, "Cut_V_MV_6", "YouTube Music · Spotify · iTunes · TIDAL  「Our frequency」", 4.5f, false, null, "Cut_V_OP_1", 1),
            }
        };
    }
}
