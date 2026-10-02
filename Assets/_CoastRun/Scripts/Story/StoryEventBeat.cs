using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 스토리 보조 컷씬(EV1~10) 직후 짧은 선택 — 패시브 시네마에 플레이어 손맛을 붙인다.
    ///   단서(ClueSystem)와 별개. 선택에 따라 스트레스·체력·챕터 하트가 살짝 움직인다.
    public static class StoryEventBeat
    {
        private static Canvas _canvas;

        public static bool IsOpen => _canvas != null;

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }

        /// EV 번호(1~10). 해당 beat가 없으면 즉시 onDone.
        public static void ShowAfterEvent(SaveData save, int ev, Action onDone)
        {
            if (save == null || ev < 1 || ev > 10) { onDone?.Invoke(); return; }
            // 197차(사용자: 「순서 맞게」): 선택 카드를 지금 이야기 순서(v7 EV)에 맞춰 붙인다 — 카드 문장은 그대로, 붙는 자리만.
            //   EV1 보드→「지금 잡을 수 있는 건」 · EV2 스무 살 생일→「달력의 X」 · EV3 물장구→「처음 듣는 이름」 · EV4 태왁→「물때의 뒷모습」
            //   EV5 우유 두 병→「저녁 우유 두 병」 · EV6 첫눈→「성에 창의 두 얼굴」 · EV10 안을 수 없는→「전날 밤의 우비」 · EV7~9(회상)은 카드 없음
            int[] beatOf = { 0, 1, 4, 2, 6, 3, 7, 0, 0, 0, 10 };
            ev = beatOf[ev];
            if (ev == 0) { onDone?.Invoke(); return; }
            switch (ev)
            {
                case 1: Choice(save, Loc.T("까진 무릎", "Scraped knees"),
                    Loc.T("돌담 굽은 길에서 둘 다 넘어졌다.", "We both fell on the bend by the stone wall."),
                    "Icon_Star", Loc.T("꼬마 무릎부터 털어 준다", "Brush off the kid's knees first"),
                    "Icon_Coin", Loc.T("보드를 먼저 일으켜 세운다", "Pick up the board first"),
                    "Icon_Refresh", Loc.T("그 자리에 앉아 숨을 고른다", "Sit there and catch my breath"),
                    () => Apply(save, 0, 0, 1, -4, Loc.T("꼬마가 「안 아파」 하고 두 번 말했다.", "The kid said “Doesn't hurt” twice.")),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("바퀴 하나가 헛돌았다. 그래도 굴러갔다.", "One wheel spun loose. It still rolled.")),
                    () => Apply(save, 0, 0, 0, 3, Loc.T("탑 꼭대기가 노을에 빨갛게 보였다.", "The top of the tower looked red in the sunset.")),
                    onDone); break;
                case 2: Choice2(save, Loc.T("언덕길", "The hill road"),
                    Loc.T("아줌마가 내 손을 잡고 언덕을 올랐다. 다른 손은 꼬마가 잡았다.", "The lady held my hand up the hill. The kid held the other."),
                    "Icon_Bulb", Loc.T("아줌마 손을 꼭 잡는다", "Hold the lady's hand tight"),
                    "Icon_Refresh", Loc.T("꼬마 쪽을 돌아본다", "Look back at the kid"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("손바닥이 거칠었다. 바닷물 냄새가 났다.", "Her palm was rough. It smelled of seawater.")),
                    () => Apply(save, 0, 0, 0, -1, Loc.T("꼬마는 후드를 눌러쓰고 발끝만 보며 걸었다.", "The kid pulled his hood down and watched his feet.")),
                    onDone); break;
                case 3: Choice2(save, Loc.T("저녁 우유 두 병", "Two bottles at dusk"),
                    Loc.T("흰 셔츠는 탑 쪽으로, 아줌마는 초소 쪽으로. 꼬마가 내 소매를 잡고 있다.", "The white shirt went toward the tower, the lady toward the post. The kid holds my sleeve."),
                    "Icon_Arrow", Loc.T("흰 셔츠 쪽으로 몇 걸음 가 본다", "Take a few steps after the white shirt"),
                    "Icon_Home", Loc.T("꼬마 옆에 그대로 선다", "Stay beside the kid"),
                    () => Apply(save, -1, 0, 1, 2, Loc.T("등만 보였다. 우유병 두 개가 부딪히는 소리가 났다.", "Only his back. The two bottles clinked.")),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("꼬마가 그제야 소매를 놓았다. 「잘했어.」", "Only then did the kid let go. “Good.”")),
                    onDone); break;
                case 4: Choice2(save, Loc.T("달력", "The calendar"),
                    Loc.T("날마다 X를 그었다. 동그라미 칸만 비워 두었다.", "Every day I drew an X. Only the circled day stayed blank."),
                    "Icon_Book", Loc.T("오늘 칸에 X를 긋는다", "Draw today's X"),
                    "Icon_Speed", Loc.T("남은 칸을 세어 본다", "Count the days left"),
                    () => Apply(save, 0, 0, 0, -3, Loc.T("연필 끝이 뭉툭해졌다.", "The pencil tip went blunt.")),
                    () => Apply(save, 0, 0, 0, 1, Loc.T("세다가 잊어버려서 처음부터 다시 셌다.", "I lost count and started over.")),
                    onDone); break;
                case 5: Choice2(save, Loc.T("찢어진 우비 소매", "A torn raincoat sleeve"),
                    Loc.T("바늘이 안 잡힌다. 꼬마가 실을 이로 끊었다.", "The needle won't stay. The kid bit the thread."),
                    "Icon_Star", Loc.T("고맙다고 하고 소매를 감싼다", "Thank him and wrap the sleeve"),
                    "Icon_Bang", Loc.T("한 번 더 바늘을 쥐어 본다", "Try the needle one more time"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("「괜찮아.」 꼬마가 웃었다. 미안함은 남았다.", "\"It's fine.\" He smiled. Guilt stayed.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("손이 떨려 또 놓쳤다. 그래도 포기하진 않았다.", "Trembling — missed again. But I didn't quit.")),
                    onDone); break;
                case 6: Choice2(save, Loc.T("빈 태왁 망", "The empty net bag"),
                    Loc.T("아줌마는 소라 대신 바위 틈과 폐그물만 들췄다.", "Instead of conches, the lady searched rock cracks and old nets."),
                    "Icon_Arrow", Loc.T("나도 바위 틈을 들여다본다", "Look into the rock cracks too"),
                    "Icon_Home", Loc.T("받은 소라를 귀에 대 본다", "Hold the conch to my ear"),
                    () => Apply(save, -1, 0, 1, 3, Loc.T("물이 빠진 틈에 작은 게 한 마리가 숨어 있었다.", "A small crab hid in a drained crack.")),
                    () => Apply(save, 0, 0, 0, -1, Loc.T("바닷소리가 났다. 아줌마가 그걸 보고 웃었다.", "It sounded like the sea. The lady laughed when she saw.")),
                    onDone); break;
                case 7: Choice2(save, Loc.T("성에 창의 세 얼굴", "Three faces in the frost"),
                    Loc.T("꼬마 하나, 나 하나. 꼬마가 하나 더 그렸다.", "One for the kid, one for me. The kid drew one more."),
                    "Icon_Camera", Loc.T("세 번째 얼굴에 웃는 입을 그린다", "Draw a smile on the third face"),
                    "Icon_Refresh", Loc.T("손등으로 성에를 지운다", "Wipe the frost with my hand"),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("꼬마가 옆에서 한쪽 눈을 접어 보였다.", "Beside me, the kid folded one eye shut.")),
                    () => Apply(save, 0, 0, 0, 1, Loc.T("창이 맑아졌다. 밖에 눈사람이 서 있었다.", "The glass cleared. The snowman stood outside.")),
                    onDone); break;
                case 8: Choice2(save, Loc.T("이름 없는 국화", "Chrysanthemums without a name"),
                    Loc.T("두 다발. 꼬마가 「저 사람은 아직 몰라」라고 했다.", "Two bunches. The kid: \"That person doesn't know yet.\""),
                    "Icon_Heart", Loc.T("한 다발을 ‘나’에게 둔다", "Set one bunch for \"me\""),
                    "Icon_Tower", Loc.T("둘 다 ‘모르는 사람’ 앞에 둔다", "Leave both for the unknown"),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("살아 있는 쪽에 꽃을 두니 숨이 고르게 쉬어졌다.", "Flowers for the living — my breath evened out.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("이름 없는 꽃. 내가 나인 줄 알면서도 모른 척했다.", "Nameless flowers. I knew — and pretended not to.")),
                    onDone); break;
                case 10: Choice2(save, Loc.T("밥 먹자", "“Let's eat”"),
                    Loc.T("마루에 종이 한 장, 「밥 먹자」. 국 두 그릇에서 김이 올랐다.", "A note on the porch: “Let's eat.” Steam rose from two bowls of soup."),
                    "Icon_Home", Loc.T("엄마 옆에 앉는다", "Sit beside Mom"),
                    "Icon_Card", Loc.T("종이 글씨를 손가락으로 따라 쓴다", "Trace the note with my finger"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("엄마가 숟가락을 두 개 놓았다.", "Mom set out two spoons.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("손가락이 종이를 지나갔다. 글씨는 그대로였다.", "My finger went through the paper. The words stayed.")),
                    onDone); break;
                default:
                    onDone?.Invoke();
                    break;
            }
        }

        private static void Apply(SaveData save, int dSta, int dMoney, int dHearts, int dStress, string toast)
        {
            if (save?.stats != null)
            {
                save.stats.stamina += dSta;
                save.stats.money = Mathf.Max(0, save.stats.money + dMoney);
                save.stats.hearts = Mathf.Max(0, save.stats.hearts + dHearts);
                save.stats.stress += dStress;
                save.stats.Clamp();
            }
            if (dHearts != 0) save.chapterHearts = Mathf.Max(0, save.chapterHearts + dHearts);
            GameManager.I?.Persist();
            if (!string.IsNullOrEmpty(toast)) CoastToast.Show(toast);
        }

        private static void Choice2(SaveData save, string title, string body, string iconA, string a, string iconB, string b,
            Action onA, Action onB, Action onDone)
        {
            Choice(save, title, body, iconA, a, iconB, b, null, null, onA, onB, null, onDone);
        }

        private static void Choice(SaveData save, string title, string body,
            string iconA, string a, string iconB, string b, string iconC, string c,
            Action onA, Action onB, Action onC, Action onDone)
        {
            Close();
            bool three = !string.IsNullOrEmpty(c) && onC != null;
            // 시안(크림 카드) + 분홍 태그 + A/B 본문 블록 + 나란히 버튼
            float h = three ? 680f : 620f;
            var card = EventCardKit.Card("StoryEventBeat", 345, new Vector2(560f, h), out _canvas, 16f);
            EventCardKit.HellsumTag(card, Loc.T("✨ 이야기", "✦ Story event"), 18f);   // 220차: 시안 이름(헬섬 이벤트)이 그대로 보이던 것
            var titleT = CoastHudLayout.MakeText(card, "Title", title, 26, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(24f, -72f), new Vector2(-24f, -32f));
            titleT.color = EventCardKit.BrownInk; titleT.fontStyle = FontStyle.Bold;
            titleT.resizeTextForBestFit = true; titleT.resizeTextMinSize = 14; titleT.resizeTextMaxSize = CoastHudLayout.Scaled(26);
            var ask = CoastHudLayout.MakeText(card, "Ask", Loc.T("? 어떻게 할까?", "? What should I do?"), 17, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(24f, -112f), new Vector2(-24f, -84f));
            ask.color = new Color(0.55f, 0.40f, 0.36f);

            // A = 상황+선택지 A, B = 선택지 B (돌발 이벤트 body/altBody 와 같은 역할)
            string blockA = string.IsNullOrEmpty(body) ? a : body;
            string blockB = b;
            EventCardKit.ChoiceBlock(card, "BlkA", true, iconA ?? "Icon_Him", blockA, 130f, three ? 100f : 120f);
            EventCardKit.ChoiceBlock(card, "BlkB", false, iconB ?? "Icon_Eye", blockB, 268f, three ? 100f : 120f);

            Action finish = () => { Close(); onDone?.Invoke(); };
            if (three)
            {
                EventCardKit.SoftChoiceButton(card, "A", iconA, a, true, new Vector2(-132f, 118f), new Vector2(236f, 58f), () => { onA?.Invoke(); finish(); });
                EventCardKit.SoftChoiceButton(card, "B", iconB, b, false, new Vector2(132f, 118f), new Vector2(236f, 58f), () => { onB?.Invoke(); finish(); });
                EventCardKit.IconButton(card, "C", iconC, c, new Color(0.55f, 0.58f, 0.68f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 56f), () => { onC?.Invoke(); finish(); }, 17);
            }
            else
            {
                EventCardKit.SoftChoiceButton(card, "A", iconA, a, true, new Vector2(-132f, 28f), new Vector2(236f, 64f), () => { onA?.Invoke(); finish(); });
                EventCardKit.SoftChoiceButton(card, "B", iconB, b, false, new Vector2(132f, 28f), new Vector2(236f, 64f), () => { onB?.Invoke(); finish(); });
            }
        }
    }
}
