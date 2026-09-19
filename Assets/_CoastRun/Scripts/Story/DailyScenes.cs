using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 105차(재미요소 P1-2, 스타듀 하트 이벤트): 알바·놀기 카드에서 같은 사람을 여러 번 만나면(Affinity) 하트가 쌓이고,
    ///   문턱(3·7·12)마다 **두 줄짜리 일상 장면** 카드가 뜬다. 문체는 CUTSCENE_STYLE_GUIDE v2 — 겪은 일만, 감정 이름 없이, 정체 흘리기 없이.
    ///   글은 여기 한 곳(사용자 검수·수정용). 그림은 Resources/CoastRun/UI_Daily_<npc>_<level>(없으면 사람 색 띠).
    public static class DailyScenes
    {
        // [npc: 꼬마 0 · 아줌마 1 · 할머니 2 · 주파수 3][level 1..3]
        private static readonly string[][] Ko =
        {
            new[] {
                "꼬마가 오름 길에서 먼저 손을 흔들었다.\n내 보드 뒤에 매달려 같이 내려왔다.",
                "등대 계단 열두 개를 꼬마가 세며 올랐다.\n다 오르자 내 손에 조개껍데기를 쥐여 줬다.",
                "바닷가 바위에 나란히 앉았다.\n꼬마는 말이 없었고, 나도 그랬다. 해가 다 질 때까지.",
            },
            new[] {
                "시장 좌판을 치우는데 아줌마가 생선 한 마리를 신문지에 쌌다.\n손짓으로 「가져가」.",
                "배달을 마치고 돌아오니 대문 앞에 반찬통이 놓여 있었다.\n뚜껑에 종이 한 장. 「데워 먹어.」",
                "아줌마가 내 젖은 머리를 수건으로 닦았다.\n닦는 동안 아줌마는 창밖만 봤다.",
            },
            new[] {
                "귤을 따는데 할머니가 내 바구니에 귤을 두 개 더 넣었다.\n「먹으멍 하라.」",
                "마을회관 부엌. 할머니가 국 간을 보라고 국자를 내밀었다.\n짰다. 할머니는 물을 더 부었다.",
                "당산나무 아래 할머니가 자리를 반 내줬다.\n둘이 앉아 버스가 세 대 지나가는 걸 봤다.",
            },
            new[] {
                "다이얼을 89.2에 맞췄다. 지직거리다가 노래 한 소절이 들렸다.\n다 듣기 전에 끊겼다.",
                "햄 교실에서 처음으로 신호를 보냈다.\n답은 없었다. 안테나를 창가로 옮겼다.",
                "새벽 라디오에 사연 하나가 읽혔다.\n「보낸 사람: 제주 해안도로」 — 거기서 방송이 끝났다.",
            },
        };
        private static readonly string[][] En =
        {
            new[] { "The kid waved first on the oreum path.\nHe rode down hanging onto the back of my board.", "The kid counted the twelve lighthouse steps as he climbed.\nAt the top he put a shell in my hand.", "We sat side by side on the rocks.\nThe kid said nothing, and neither did I, until the sun was gone." },
            new[] { "Clearing the market stall, the lady wrapped a fish in newspaper.\nA hand sign: take it.", "Back from deliveries, a side-dish box sat at the gate.\nA note on the lid: heat it up.", "The lady dried my wet hair with a towel.\nWhile she did, she only looked out the window." },
            new[] { "Picking tangerines, grandma dropped two more into my basket.\n\"Eat while you work.\"", "Village hall kitchen. Grandma held out the ladle to taste the soup.\nSalty. She added water.", "Under the dangsan tree grandma gave me half her seat.\nWe watched three buses go by." },
            new[] { "I set the dial to 89.2. Static, then one line of a song.\nIt cut off before the end.", "First signal sent from the ham class.\nNo answer. I moved the antenna to the window.", "A letter was read on the dawn radio.\n\"From: the Jeju coast road\" — the broadcast ended there." },
        };

        public static string Text(int npc, int level)
        {
            npc = Mathf.Clamp(npc, 0, 3); level = Mathf.Clamp(level, 1, 3);
            return Loc.T(Ko[npc][level - 1], En[npc][level - 1]);
        }
        public static Color NpcColor(int npc) => npc == 0 ? new Color(1f, 0.60f, 0.25f) : npc == 1 ? new Color(0.35f, 0.65f, 0.90f) : npc == 2 ? new Color(0.60f, 0.75f, 0.45f) : new Color(0.60f, 0.50f, 0.90f);
    }

    public static class DailySceneUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void Show(int npc, int level, Action onDone)
        {
            Close();
            var crt = EventCardKit.Card("DailySceneCanvas", 464, new Vector2(600f, 720f), out _canvas, 20f);
            var tex = ArtAssets.LoadTexture($"UI_Daily_{npc}_{level}");
            var im = CoastHudLayout.MakeImage(crt, "Art", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -24f - 300f), new Vector2(-24f, -24f), tex != null ? Color.white : DailyScenes.NpcColor(npc));
            im.raycastTarget = false;
            if (tex != null) { im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; EventCardKit.Animate(im); }   // 109차: 그림이 천천히 움직인다
            string hearts = ""; for (int i = 0; i < level; i++) hearts += "♥";
            EventCardKit.JellyTitle(crt, Affinity.Name(npc), DailyScenes.NpcColor(npc), new Color(0.20f, 0.12f, 0.30f), 340f, 76f, 34);
            EventCardKit.IconRow(crt, "Icon_Star", new Color(1f, 0.75f, 0.80f), Loc.T($"하트 {hearts}", $"Hearts {hearts}"), 424f, 48f, 20);
            var box = EventCardKit.InfoBox(crt, 480f, 130f);
            var body = CoastHudLayout.MakeText(box, "Body", DailyScenes.Text(npc, level), 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            body.color = EventCardKit.Ink; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = CoastHudLayout.Scaled(19);
            EventCardKit.IconButton(crt, "Ok", "Icon_Arrow", Loc.T("돌아가기", "Back"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(300f, 70f), () => { Close(); onDone?.Invoke(); }, 26);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
        }
        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
