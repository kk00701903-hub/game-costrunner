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
                case 1: Choice(save, Loc.T("지금 잡을 수 있는 건", "What still fits in my hand"),
                    Loc.T("유리구슬만 잡히고 우유는 미끄러졌다. 한 번 더 손을 뻗어 본다.", "Only the marble stays. I reach again."),
                    "Icon_Star", Loc.T("주머니 속 구슬을 꽉 쥔다", "Grip the marble in my pocket"),
                    "Icon_Coin", Loc.T("탑 아래 미지근한 우유를 든다", "Lift the lukewarm milk by the tower"),
                    "Icon_Refresh", Loc.T("차가운 팩을 억지로 집는다", "Force the cold carton"),
                    () => Apply(save, 0, 0, 1, -4, Loc.T("구슬이 손바닥에 남았다. 이건 내 거였던 것.", "The marble stays. It was mine once.")),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("미지근한 병은 잡혔다. 누군가 매일 두고 가는 것.", "The warm bottle held. Someone leaves it every day.")),
                    () => Apply(save, 0, 0, 0, 3, Loc.T("또 미끄러졌다. 아직 내 것이 아닌 모양이다.", "It slipped again. Not mine yet.")),
                    onDone); break;
                case 2: Choice2(save, Loc.T("처음 듣는 이름", "A name I almost know"),
                    Loc.T("해녀들이 「하늘이」라고 했다. 파란 건 나였다. 바다누나.", "The divers said 'Haneul'. Blue was me. Ocean unnie."),
                    "Icon_Bulb", Loc.T("「하늘」이라고 작게 말해 본다", "Whisper \"Haneul\" aloud"),
                    "Icon_Refresh", Loc.T("입 안에만 굴려 본다", "Keep it only in my mouth"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("발음하니 가슴이 먼저 알아챈 것 같았다.", "Saying it, my chest knew first.")),
                    () => Apply(save, 0, 0, 0, -1, Loc.T("이름은 아직 무거웠다. 오늘은 여기까지.", "The name was still heavy. Enough for today.")),
                    onDone); break;
                case 3: Choice2(save, Loc.T("저녁 우유 두 병", "Two bottles at dusk"),
                    Loc.T("가게 앞 도윤. 꼬마가 소매를 잡았다. 「따라가지 마.」", "Doyun at the shop. The kid tugged my sleeve. \"Don't follow.\""),
                    "Icon_Arrow", Loc.T("그래도 조금 따라가 본다", "Follow him a little anyway"),
                    "Icon_Home", Loc.T("꼬마 말대로 자리를 지킨다", "Stay put, like the kid said"),
                    () => Apply(save, -1, 0, 1, 2, Loc.T("등은 보였지만 얼굴은 안 보였다. 심장이 달렸다.", "I saw his back, not his face. My heart ran.")),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("꼬마가 안심한 듯 손을 놓았다. 「잘했어.」", "The kid let go, relieved. \"Good.\"")),
                    onDone); break;
                case 4: Choice2(save, Loc.T("달력의 X", "An X on the calendar"),
                    Loc.T("연필은 잘 잡힌다. 빈 칸이 줄어들수록 약속이 커진다.", "The pencil holds. Fewer blank days — a bigger promise."),
                    "Icon_Book", Loc.T("오늘도 단정히 X를 긋는다", "Draw today's X carefully"),
                    "Icon_Speed", Loc.T("급하게 줄만 그어 둔다", "Slash it in a hurry"),
                    () => Apply(save, 0, 0, 0, -3, Loc.T("단정한 X. 손끝이 덜 저렸다.", "A neat X. Fingers steadier.")),
                    () => Apply(save, 0, 0, 0, 1, Loc.T("삐뚤어진 줄. 그래도 하루는 지나갔다.", "A crooked mark. Still — a day passed.")),
                    onDone); break;
                case 5: Choice2(save, Loc.T("찢어진 우비 소매", "A torn raincoat sleeve"),
                    Loc.T("바늘이 안 잡힌다. 꼬마가 실을 이로 끊었다.", "The needle won't stay. The kid bit the thread."),
                    "Icon_Star", Loc.T("고맙다고 하고 소매를 감싼다", "Thank him and wrap the sleeve"),
                    "Icon_Bang", Loc.T("한 번 더 바늘을 쥐어 본다", "Try the needle one more time"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("「괜찮아.」 꼬마가 웃었다. 미안함은 남았다.", "\"It's fine.\" He smiled. Guilt stayed.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("손이 떨려 또 놓쳤다. 그래도 포기하진 않았다.", "Trembling — missed again. But I didn't quit.")),
                    onDone); break;
                case 6: Choice2(save, Loc.T("물때의 뒷모습", "A silhouette at low tide"),
                    Loc.T("갯바위에서 폐그물을 넘기는 키 큰 남자. 해경이 「아직요?」 물었다.", "A tall man lifting nets. Coast guard: \"Still looking?\""),
                    "Icon_Arrow", Loc.T("도윤을 따라 갯바위로 간다", "Follow Doyun onto the rocks"),
                    "Icon_Home", Loc.T("멀리서만 보고 돌아온다", "Watch from afar and leave"),
                    () => Apply(save, -1, 0, 1, 3, Loc.T("가까이 가자 사라지듯 멀어졌다. 물 냄새가 짰다.", "Closer — he slipped away. Salt in the air.")),
                    () => Apply(save, 0, 0, 0, -1, Loc.T("오늘은 쫓지 않았다. 찾는 건 사람이라고 했다.", "I didn't chase. He said he was looking for a person.")),
                    onDone); break;
                case 7: Choice2(save, Loc.T("성에 창의 두 얼굴", "Two faces in the frost"),
                    Loc.T("성에에 얼굴이 둘. 꼬마는 눈길에서 내 뒤만 봤다.", "Two faces on the glass. On the snow path the kid watched my back."),
                    "Icon_Camera", Loc.T("두 얼굴을 더 또렷이 그린다", "Trace both faces clearer"),
                    "Icon_Refresh", Loc.T("손등으로 성에를 지운다", "Wipe the frost with my hand"),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("한쪽은 나, 한쪽은… 아직 이름 없는 눈매.", "One is me. The other — eyes without a name yet.")),
                    () => Apply(save, 0, 0, 0, 1, Loc.T("창이 맑아졌다. 달력의 32일이 더 선명해 보였다.", "Clear glass. The calendar's day 32 looked sharper.")),
                    onDone); break;
                case 8: Choice2(save, Loc.T("이름 없는 국화", "Chrysanthemums without a name"),
                    Loc.T("두 다발. 꼬마가 「저 사람은 아직 몰라」라고 했다.", "Two bunches. The kid: \"That person doesn't know yet.\""),
                    "Icon_Heart", Loc.T("한 다발을 ‘나’에게 둔다", "Set one bunch for \"me\""),
                    "Icon_Tower", Loc.T("둘 다 ‘모르는 사람’ 앞에 둔다", "Leave both for the unknown"),
                    () => Apply(save, 0, 0, 1, -2, Loc.T("살아 있는 쪽에 꽃을 두니 숨이 고르게 쉬어졌다.", "Flowers for the living — my breath evened out.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("이름 없는 꽃. 내가 나인 줄 알면서도 모른 척했다.", "Nameless flowers. I knew — and pretended not to.")),
                    onDone); break;
                case 10: Choice2(save, Loc.T("전날 밤의 우비", "The raincoat on the eve"),
                    Loc.T("라디오가 「내일은 들어도 돼」라고 했다. 우비가 침대 옆에 있다.", "The radio: \"Tomorrow you may listen.\" The coat sits by the bed."),
                    "Icon_Home", Loc.T("우비를 그대로 옆에 둔다", "Leave the coat right there"),
                    "Icon_Card", Loc.T("멀리 걸어 장롱에 넣는다", "Hang it far away in the closet"),
                    () => Apply(save, 0, 0, 1, -3, Loc.T("주황이 가까이 있어 잠이 덜 무서웠다.", "Orange nearby — sleep less scary.")),
                    () => Apply(save, 0, 0, 0, 2, Loc.T("문을 닫았다. 내일의 첫차가 더 크게 들렸다.", "Door shut. Tomorrow's first bus sounded louder.")),
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
            // 시안 「헬섬 이벤트」: 크림 카드 + 분홍 태그 + A/B 본문 블록 + 나란히 버튼
            float h = three ? 680f : 620f;
            var card = EventCardKit.Card("StoryEventBeat", 345, new Vector2(560f, h), out _canvas, 16f);
            EventCardKit.HellsumTag(card, Loc.T("✨ 헬섬 이벤트", "✦ Story event"), 18f);
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
