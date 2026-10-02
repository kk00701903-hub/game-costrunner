using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 85차(대본 v4 「분기 조건」): 플레이 중 모이는 **단서 여섯 개**(SaveData.clueMask) → 엔딩 A/B/TRUE.
    ///   이름(CS7 카드) · 편지 스무 통(EV9) · 하트 흔적(CS2 부표 페인트) · 보석 머리띠(CS8 방수 통) · 돌 세 개(CS4 뒤 다시 쌓기) · 89.2 라디오(CS3 복원).
    ///   컷씬은 항상 재생되므로 단서는 **컷씬 직후 육성 화면의 「단서」 카드**에서 확정한다. 편지·돌은 카드에서 한 번 더 손을 대야(두 번째 버튼) 얻는다 —
    ///   그냥 넘기면 빠진다(놓친 단서는 시네마 다시보기로는 안 채워진다: 스토리 진행 중 카드에서만).
    ///   END_A 「엇갈린 정류장」: 이름 없음 또는 라디오 없음 · END_B 「우유 두 병」: 이름+라디오는 있으나 나머지 중 하나라도 없음 · END_TRUE: 여섯 개 전부.
    public static class ClueSystem
    {
        [Flags]
        public enum Clue { None = 0, Name = 1, Letters = 2, Heart = 4, Headband = 8, Stones = 16, Radio = 32, All = 63 }

        public static bool Has(SaveData s, Clue c) => s != null && (s.clueMask & (int)c) != 0;
        public static int Count(SaveData s) { if (s == null) return 0; int n = 0; for (int i = 0; i < 6; i++) if ((s.clueMask & (1 << i)) != 0) n++; return n; }

        /// 씬 id("CS2", "EV9" …) 가 끝났을 때 주는 단서. 없으면 None.
        public static Clue ClueFor(string sceneId)
        {
            // 220차(연결 점검): 단서를 그 물건이 나오는 장면 뒤로 옮김 — 하트 EV1(조개껍데기) · 돌 세 개 CS2(담요 자리로 돌아감) · 머리띠 CS5 · 라디오 CS6 · 이름 CS7 · 편지 EV2(약속 뒤).
            //   (예전: CS2 하트 · CS3 라디오 · CS4 돌 · CS8 머리띠 · EV9 편지 — 장면에 없는 물건이 나오거나 마지막 장면 뒤에 카드가 떴다)
            switch (sceneId)
            {
                case "EV1": return Clue.Heart;
                case "CS2": return Clue.Stones;
                case "CS5": return Clue.Headband;
                case "CS6": return Clue.Radio;
                case "CS7": return Clue.Name;
                case "EV2": return Clue.Letters;
                default: return Clue.None;
            }
        }

        /// 엔딩 시네마 id — v4 분기 조건.
        public static string EndingId(int mask)
        {
            bool name = (mask & (int)Clue.Name) != 0, radio = (mask & (int)Clue.Radio) != 0;
            if (!name || !radio) return "END_A";
            return (mask & (int)Clue.All) == (int)Clue.All ? "END_TRUE" : "END_B";
        }

        public static string Name(Clue c)
        {
            switch (c)
            {
                case Clue.Name: return Loc.T("이름", "The name");
                case Clue.Letters: return Loc.T("편지 스무 통", "Twenty letters");
                case Clue.Heart: return Loc.T("하트 흔적", "The heart mark");
                case Clue.Headband: return Loc.T("보석 머리띠", "The gem headband");
                case Clue.Stones: return Loc.T("돌 세 개", "Three stones");
                case Clue.Radio: return Loc.T("89.2 라디오", "Radio 89.2");
                default: return "";
            }
        }
        /// 카드 아이콘(Resources 그림 이름 — 없으면 색 동그라미만).
        public static string Icon(Clue c)
        {
            switch (c)
            {
                case Clue.Name: return "Icon_Camera"; case Clue.Letters: return "Icon_Book"; case Clue.Heart: return "Icon_Star";
                case Clue.Headband: return "Icon_Coin"; case Clue.Stones: return "Icon_Tower"; case Clue.Radio: return "Icon_Bulb"; default: return "Icon_Star";
            }
        }
        /// 카드 본문 — 왜 이 단서인지 한 줄.
        private static string Body(Clue c)
        {
            switch (c)
            {
                case Clue.Heart: return Loc.T("조개껍데기 위 삐뚤어진 빨간 하트. 「잃어버리지 마.」", "A crooked red heart on the shell. “Don't lose it.”");
                case Clue.Radio: return Loc.T("다이얼 옆에 하트가 그려진 라디오. 바늘이 89.2에 멈춰 있다.", "A radio with a heart drawn by the dial. The needle rests at 89.2.");
                case Clue.Stones: return Loc.T("탑 아래 돌 세 개가 무너져 있다. 다시 쌓을까, 그냥 둘까.", "The three stones under the tower have fallen. Stack them again — or leave them.");
                case Clue.Name: return Loc.T("코팅된 수색 카드 — 「고하늘 실종 1년」. 내 이름이 거기 있었다.", "A laminated search card — 'Go Haneul, missing one year'. My name was there.");
                case Clue.Headband: return Loc.T("노란 통 속 파란 보석 머리띠. 한 번도 안 쓴 새것.", "The blue gem headband in the yellow tin. Never worn.");
                case Clue.Letters: return Loc.T("편지 스무 통. 전부 읽으면 밤이 새겠지만, 한 통도 남기고 싶지 않다.", "Twenty letters. Reading them all will take the night, but I don't want to leave one unread.");
                default: return "";
            }
        }

        // ── 105차(재미요소 P2-1): 단서마다 **육성에서 해야 할 일** — 컷씬은 그대로 재생되고, 조건이 모자라면 「아직 뭔가 모자라」 카드 뒤 보류(cluePendingMask).
        //    보류된 단서는 행동으로 조건을 채우는 순간 카드가 다시 떠서 얻는다(놓쳐도 만회 가능, 기한 없음). 이름(CS7)은 조건 없음 — 엔딩 A/B 기본 분기 유지.
        public static readonly string[] RadioCards = { "dev_radio", "les_ham", "job_dj_assist" };
        public static readonly string[] TowerCards = { "job_tower_fix", "job_tower_watch" };
        public static readonly string[] SeaCards = { "job_haenyeo", "rest_sea", "les_swim" };
        public static readonly string[] MailCards = { "job_delivery", "job_night_delivery", "job_market" };
        private static int Uses(SaveData s, string[] ids) { int n = 0; foreach (var id in ids) n += RaisingFun.MasteryUses(s, id); return n; }
        /// 단서 조건 통과 여부.
        public static bool ConditionMet(SaveData s, Clue c)
        {
            if (s == null) return false;
            switch (c)
            {
                case Clue.Radio: return Uses(s, RadioCards) >= 3;
                case Clue.Stones: return Uses(s, TowerCards) >= 2 || s.stats.stamina >= 60;
                case Clue.Heart: return Uses(s, SeaCards) >= 2;
                case Clue.Letters: return Uses(s, MailCards) >= 2;
                case Clue.Headband: return s.stats.sense >= 40;
                default: return true;
            }
        }
        /// 조건 설명 + 진행(「라디오 교육 2/3」).
        public static string ConditionText(SaveData s, Clue c)
        {
            if (s == null) return "";
            switch (c)
            {
                case Clue.Radio: return Loc.T($"라디오·햄·DJ 보조 {Mathf.Min(3, Uses(s, RadioCards))}/3회", $"Radio/ham/DJ cards {Mathf.Min(3, Uses(s, RadioCards))}/3");
                case Clue.Stones: return Loc.T($"송전탑 알바 {Mathf.Min(2, Uses(s, TowerCards))}/2회 또는 체력 {s.stats.stamina}/60", $"Tower jobs {Mathf.Min(2, Uses(s, TowerCards))}/2 or stamina {s.stats.stamina}/60");
                case Clue.Heart: return Loc.T($"해녀·바다 {Mathf.Min(2, Uses(s, SeaCards))}/2회", $"Sea cards {Mathf.Min(2, Uses(s, SeaCards))}/2");
                case Clue.Letters: return Loc.T($"배달·시장 {Mathf.Min(2, Uses(s, MailCards))}/2회", $"Delivery/market {Mathf.Min(2, Uses(s, MailCards))}/2");
                case Clue.Headband: return Loc.T($"감성 {s.stats.sense}/40", $"Sense {s.stats.sense}/40");
                default: return "";
            }
        }
        /// 보류 단서에 도움이 되는 카드 id 들(카드 2장 뽑을 때 우선).
        public static System.Collections.Generic.List<string> HelpfulCards(SaveData s)
        {
            var list = new System.Collections.Generic.List<string>();
            if (s == null || s.cluePendingMask == 0) return list;
            if ((s.cluePendingMask & (int)Clue.Radio) != 0) list.AddRange(RadioCards);
            if ((s.cluePendingMask & (int)Clue.Stones) != 0) list.AddRange(TowerCards);
            if ((s.cluePendingMask & (int)Clue.Heart) != 0) list.AddRange(SeaCards);
            if ((s.cluePendingMask & (int)Clue.Letters) != 0) list.AddRange(MailCards);
            return list;
        }
        public static bool IsPending(SaveData s, Clue c) => s != null && (s.cluePendingMask & (int)c) != 0;
        /// 보류 단서 요약(상태창·달력): 「보류: 라디오(라디오 교육 1/3)」
        public static string PendingSummary(SaveData s)
        {
            if (s == null || s.cluePendingMask == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (Clue c in new[] { Clue.Radio, Clue.Stones, Clue.Heart, Clue.Letters, Clue.Headband })
                if (IsPending(s, c)) { if (sb.Length > 0) sb.Append(" · "); sb.Append(Name(c)).Append(" — ").Append(ConditionText(s, c)); }
            return Loc.T("아직 못 얻은 단서: ", "Clues to earn: ") + sb;
        }
        /// 행동 뒤 호출 — 보류 단서 중 조건이 채워진 것이 있으면 그 카드를 띄운다(한 번에 하나). 없으면 즉시 onDone.
        public static void CheckPending(SaveData save, Action onDone)
        {
            if (save == null || save.cluePendingMask == 0) { onDone?.Invoke(); return; }
            foreach (Clue c in new[] { Clue.Radio, Clue.Stones, Clue.Heart, Clue.Letters, Clue.Headband })
            {
                if (!IsPending(save, c) || Has(save, c)) continue;
                if (!ConditionMet(save, c)) continue;
                save.cluePendingMask &= ~(int)c; GameManager.I?.Persist();
                if (c == Clue.Letters) { ShowLetterReading(save, onDone); return; }
                Show(save, c, c == Clue.Stones, onDone);
                return;
            }
            onDone?.Invoke();
        }
        /// 조건이 모자랄 때 카드 — 「아직 뭔가 모자라」 + 조건 한 줄. 단서는 보류로.
        private static void ShowNotYet(SaveData save, Clue c, Action onDone)
        {
            Close();
            save.cluePendingMask |= (int)c; GameManager.I?.Persist();
            var card = EventCardKit.Card("ClueCard", 340, new Vector2(560f, 520f), out _canvas, 20f);
            EventCardKit.Kid(card, Loc.T("누나, 아직인가 봐", "Not yet, I guess"), true);   // 109차: 꼬마 동행
            EventCardKit.JellyTitle(card, Loc.T("단서?", "CLUE?"), new Color(0.80f, 0.82f, 0.90f), new Color(0.25f, 0.22f, 0.35f), 26f, 70f, 46);
            EventCardKit.Divider(card, 104f);
            EventCardKit.IconRow(card, Icon(c), new Color(0.75f, 0.75f, 0.80f), Name(c), 132f, 56f, 26, Loc.T("보류", "later"), new Color(0.60f, 0.55f, 0.70f));
            var box = EventCardKit.InfoBox(card, 200f, 140f);
            var body = CoastHudLayout.MakeText(box, "Body", Loc.T("아직 뭔가 모자라. 손이 닿지 않는다.\n", "Not yet. Something's missing.\n") + ConditionText(save, c), 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            body.color = EventCardKit.Ink; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = CoastHudLayout.Scaled(19);
            CoastToast.Show(Loc.T($"단서 보류 — {ConditionText(save, c)} 을 채우면 얻는다", $"Clue on hold — {ConditionText(save, c)}"));
            EventCardKit.IconButton(card, "Ok", "Icon_Refresh", Loc.T("나중에 다시", "Come back later"), new Color(0.55f, 0.58f, 0.68f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 70f), () => { Close(); onDone?.Invoke(); });
        }

        /// 단서 카드 — sceneId 컷씬이 끝난 뒤 육성 화면에서. 단서가 없거나 이미 가진 것이면 바로 onDone.
        ///   편지·돌은 행동을 골라야 얻는다. 편지는 발췌 2~3장을 넘긴 뒤 「전부 읽기 / 덮기」.
        ///   105차: 육성 조건(ConditionMet)이 모자라면 「아직 뭔가 모자라」 카드 → 보류(cluePendingMask).
        public static void ShowAfterScene(SaveData save, string sceneId, Action onDone)
        {
            var c = ClueFor(sceneId);
            if (save == null || c == Clue.None || Has(save, c)) { onDone?.Invoke(); return; }
            if (!ConditionMet(save, c)) { ShowNotYet(save, c, onDone); return; }
            if (c == Clue.Letters) { ShowLetterReading(save, onDone); return; }
            bool choice = c == Clue.Stones;
            Show(save, c, choice, onDone);
        }

        /// EV9: 편지 발췌를 넘긴 뒤 「전부 읽기 / 덮기」 선택.
        private static void ShowLetterReading(SaveData save, Action onDone)
        {
            string[] pages =
            {
                Loc.T("「…스무 살 생일, 송전탑 아래. 네가 오든 안 오든 나는 갈게.」\n글씨 끝이 물에 번져 있다.",
                    "\"…20th birthday, under the tower. Whether you come or not, I'll go.\"\nThe ink runs at the edge."),
                Loc.T("「우유 두 병. 하나는 오늘, 하나는 내일.」\n같은 문장이 해마다 조금씩 달라진다.",
                    "\"Two bottles of milk. One for today, one for tomorrow.\"\nThe same line, shifted a little each year."),
                Loc.T("받는 사람 칸 — 「하늘」. 스무 통 모두 같은 이름.\n마지막 통만 봉하지 않은 채다.",
                    "The name on every envelope — \"Haneul\".\nOnly the last one is still unsealed."),
            };
            ShowLetterPage(save, pages, 0, onDone);
        }

        private static void ShowLetterPage(SaveData save, string[] pages, int index, Action onDone)
        {
            Close();
            bool last = index >= pages.Length - 1;
            var card = EventCardKit.Card("LetterRead", 340, new Vector2(560f, 620f), out _canvas, 20f);
            EventCardKit.JellyTitle(card, Loc.T($"편지 · {index + 1}/{pages.Length}", $"Letter · {index + 1}/{pages.Length}"),
                new Color(1f, 0.88f, 0.45f), new Color(0.40f, 0.22f, 0.05f), 26f, 70f, 40);
            EventCardKit.Divider(card, 104f);
            EventCardKit.IconRow(card, "Icon_Book", new Color(0.98f, 0.80f, 0.35f), Loc.T("스무 통 상자", "Box of twenty"), 128f, 48f, 22, $"{index + 1}/{pages.Length}", new Color(0.30f, 0.55f, 0.95f));
            var box = EventCardKit.InfoBox(card, 190f, 160f);
            var body = CoastHudLayout.MakeText(box, "Body", pages[index], 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 10f), new Vector2(-16f, -10f));
            body.color = EventCardKit.Ink; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            if (!last)
            {
                EventCardKit.IconButton(card, "Next", "Icon_Arrow", Loc.T("다음 통을 연다", "Open the next one"), new Color(0.95f, 0.55f, 0.35f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(340f, 70f), () => ShowLetterPage(save, pages, index + 1, onDone));
            }
            else
            {
                Action finish = () => { Close(); onDone?.Invoke(); };
                EventCardKit.IconButton(card, "Take", "Icon_Star", Loc.T("스무 통 전부 읽는다", "Read all twenty"), new Color(0.95f, 0.55f, 0.35f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(340f, 70f), () =>
                    {
                        save.clueMask |= (int)Clue.Letters; GameManager.I?.Persist();
                        CoastToast.Show(Loc.T($"단서 획득 — {Name(Clue.Letters)} ({Count(save)}/6)", $"Clue — {Name(Clue.Letters)} ({Count(save)}/6)"));
                        finish();
                    });
                EventCardKit.IconButton(card, "Skip", "Icon_Refresh", Loc.T("몇 통만 읽고 덮는다", "Read a few, close the box"), new Color(0.55f, 0.58f, 0.68f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(340f, 64f), () =>
                    {
                        CoastToast.Show(Loc.T("단서를 지나쳤다 — 편지는 덜 읽힌 채다", "You passed the clue — letters left unread"));
                        finish();
                    }, 18);
            }
        }

        private static Canvas _canvas;

        private static void Show(SaveData save, Clue c, bool choice, Action onDone)
        {
            Close();
            CoastPrefs.VibrateEvent();   // 109차: 단서 획득 — 특정 이벤트 진동
            var card = EventCardKit.Card("ClueCard", 340, new Vector2(560f, choice ? 620f : 500f), out _canvas, 20f);   // 235차: 버튼이 설명 상자 아래 줄을 덮던 것 — 카드 높이 520/440 → 620/500
            if (c != Clue.Name) EventCardKit.Kid(card, choice ? Loc.T("누나, 어떡할래?", "What'll you do?") : Loc.T("누나, 이거 봐!", "Look at this!"), true);   // 109차: 꼬마 동행 · 220차: 꼬마의 정체가 드러난 CS7 뒤(이름)엔 꼬마 말풍선 없음
            EventCardKit.JellyTitle(card, Loc.T("단서", "CLUE"), new Color(1f, 0.85f, 0.30f), new Color(0.35f, 0.16f, 0.02f), 26f, 70f, 46);
            EventCardKit.Divider(card, 104f);
            var prof = GameManager.I != null ? GameManager.I.Profile : null;
            bool seenBefore = prof != null && (prof.clueSeenMask & (int)c) != 0;   // 105차: 2회차 「본 적 있음」
            EventCardKit.IconRow(card, Icon(c), new Color(0.98f, 0.80f, 0.35f), Name(c), 132f, 56f, 26, seenBefore ? Loc.T("본 적 있음", "seen before") : $"{Count(save) + (choice ? 0 : 1)}/6", seenBefore ? new Color(0.60f, 0.55f, 0.70f) : new Color(0.30f, 0.55f, 0.95f));
            var box = EventCardKit.InfoBox(card, 200f, choice ? 150f : 110f);
            string cond = ConditionText(save, c);
            var body = CoastHudLayout.MakeText(box, "Body", Body(c) + (string.IsNullOrEmpty(cond) ? "" : "\n✓ " + cond), 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
            body.color = EventCardKit.Ink; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 12; body.resizeTextMaxSize = CoastHudLayout.Scaled(19);
            Action finish = () => { Close(); onDone?.Invoke(); };
            if (!choice)
            {
                save.clueMask |= (int)c; GameManager.I?.Persist();
                CoastToast.Show(Loc.T($"단서 획득 — {Name(c)} ({Count(save)}/6)", $"Clue — {Name(c)} ({Count(save)}/6)"));
                EventCardKit.IconButton(card, "Ok", "Icon_Star", Loc.T("기억해 둔다", "I'll remember"), new Color(0.30f, 0.62f, 0.95f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 70f), finish);
            }
            else
            {
                string take = c == Clue.Stones ? Loc.T("돌을 다시 쌓는다", "Stack the stones again") : Loc.T("스무 통 전부 읽는다", "Read all twenty");
                string skip = c == Clue.Stones ? Loc.T("그냥 둔다", "Leave them") : Loc.T("몇 통만 읽고 덮는다", "Read a few, close the box");
                EventCardKit.IconButton(card, "Take", "Icon_Star", take, new Color(0.95f, 0.55f, 0.35f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(340f, 70f), () =>
                {
                    save.clueMask |= (int)c; GameManager.I?.Persist();
                    CoastToast.Show(Loc.T($"단서 획득 — {Name(c)} ({Count(save)}/6)", $"Clue — {Name(c)} ({Count(save)}/6)"));
                    finish();
                });
                EventCardKit.IconButton(card, "Skip", "Icon_Refresh", skip, new Color(0.55f, 0.58f, 0.68f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(340f, 64f), () =>
                {
                    CoastToast.Show(Loc.T("단서를 지나쳤다", "You passed the clue by"));
                    finish();
                }, 20);
            }
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }
        public static bool IsOpen => _canvas != null;

        /// 상태창·시네마 목록용 한 줄: 「단서 3/6 · 이름 · 라디오 · 하트」
        public static string Summary(SaveData s)
        {
            if (s == null) return "";
            var sb = new System.Text.StringBuilder(); sb.Append(Loc.T($"단서 {Count(s)}/6", $"Clues {Count(s)}/6"));
            foreach (Clue c in new[] { Clue.Name, Clue.Radio, Clue.Heart, Clue.Headband, Clue.Stones, Clue.Letters }) if (Has(s, c)) sb.Append(" · ").Append(Name(c));
            return sb.ToString();
        }
    }
}
