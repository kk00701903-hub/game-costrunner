using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 육성 화면 팝업: 돌발 이벤트 / 상점(펫) / 타임라인(챕터 갱신).
    public partial class RaisingUI
    {
        // ── 돌발 이벤트 ─────────────────────────────────────────────────

        public void ShowEvent(RandomEventResult ev)
        {
            _busy = true;
            var modal = Modal("EventPopup", 560f, 400f, out var panel);
            var tag = Label(panel, "Tag", "돌발 이벤트", 15, new Color(0.55f, 0.5f, 0.7f));
            Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(0f, 24f), new Vector2(0.5f, 1f));
            var t = Label(panel, "Title", ev.def.title, 26, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            var b = Label(panel, "Body", ev.Body, 18, Ink);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            b.alignment = TextAnchor.UpperLeft;
            Place(b.rectTransform, new Vector2(0f, 0.32f), new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            b.rectTransform.offsetMin = new Vector2(28f, 0f); b.rectTransform.offsetMax = new Vector2(-28f, 0f);

            var parts = new System.Collections.Generic.List<string>();
            if (ev.dHearts != 0) parts.Add($"말랑이 하트 {Signed(ev.dHearts)}");
            if (ev.dMoney != 0) parts.Add($"돈 {Signed(ev.dMoney)}");
            if (ev.dStamina != 0) parts.Add($"체력 {Signed(ev.dStamina)}");
            if (ev.dStress != 0) parts.Add($"스트레스 {Signed(ev.dStress)}");
            var d = Label(panel, "Delta", parts.Count > 0 ? string.Join("   ", parts) : "변화 없음", 18,
                ev.dHearts > 0 ? Coral : Navy);
            Place(d.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.32f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            Action close = () =>
            {
                _modalPrimary = null;
                Destroy(modal);
                _busy = false;
                Refresh();
            };
            BigButton(panel, "Ok", "확인", Coral, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(240f, 56f), () => close());
            _modalPrimary = close;
            RefreshStats();
        }

        // ── 상점 ────────────────────────────────────────────────────────

        private GameObject _shopModal;

        public void OpenShop()
        {
            if (_busy || Save == null) return;
            if (_shopModal != null) Destroy(_shopModal);
            _shopModal = Modal("ShopPopup", 620f, 640f, out var panel);
            var t = Label(panel, "Title", "펫 상점", 28, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -16f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            var money = Label(panel, "Money", $"보유 {Save.stats.money:N0}", 18, Ink);
            Place(money.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(0f, 28f), new Vector2(0.5f, 1f));

            var kinds = PetShop.ForSale;
            for (int i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i];
                bool owned = PetShop.Owns(Save, k);
                bool equipped = Save.equippedPet == k;
                var card = CoastUiArt.CutePill(panel, "Pet_" + k, owned ? Mint : new Color(0.90f, 0.86f, 0.80f), 18, 4);
                Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f - i * 150f), new Vector2(570f, 140f), new Vector2(0.5f, 1f));

                var name = Label(card.transform, "Name", PetCompanion.Names[(int)k], 22, Navy);
                name.alignment = TextAnchor.MiddleLeft;
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));
                name.rectTransform.offsetMin = new Vector2(20f, -44f); name.rectTransform.offsetMax = new Vector2(-180f, -10f);

                var blurb = Label(card.transform, "Blurb", PetCompanion.Blurbs[(int)k], 15, Ink);
                blurb.alignment = TextAnchor.UpperLeft;
                blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(blurb.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                blurb.rectTransform.offsetMin = new Vector2(20f, 12f); blurb.rectTransform.offsetMax = new Vector2(-180f, -48f);

                var price = Label(card.transform, "Price", owned ? (equipped ? "장착 중" : "보유") : $"{PetShop.Price[k]:N0}", 18, Navy);
                Place(price.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -14f), new Vector2(150f, 28f), new Vector2(1f, 1f));

                string label = !owned ? "구매" : equipped ? "해제" : "장착";
                Color col = !owned ? (PetShop.CanAfford(Save, k) ? Coral : new Color(0.65f, 0.65f, 0.7f)) : equipped ? new Color(0.6f, 0.62f, 0.7f) : Sky;
                BigButton(card.transform, "Act", label, col, new Vector2(1f, 0f), new Vector2(-16f, 14f), new Vector2(150f, 52f), () => ShopAct(k));
            }

            Action closeShop = () =>
            {
                _modalPrimary = null;
                Destroy(_shopModal);
                _shopModal = null;
                Refresh();
            };
            BigButton(panel, "Close", "닫기", new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(220f, 54f), () => closeShop());
            _modalPrimary = closeShop;
        }

        /// 구매 → 장착 → 해제 순환. 에디터 키(1~3)와 버튼이 공유.
        public void ShopAct(PetKind k)
        {
            if (Save == null) return;
            if (!PetShop.Owns(Save, k))
            {
                if (PetShop.TryBuy(Save, k)) { Toast($"{PetCompanion.Names[(int)k]}를 데려왔어!"); _gm.Persist(); }
                else Toast("돈이 모자라.");
            }
            else if (Save.equippedPet == k) { Save.equippedPet = PetKind.None; _gm.Persist(); }
            else { PetShop.Equip(Save, k); _gm.Persist(); }
            OpenShop();
        }

        // ── 타임라인 ────────────────────────────────────────────────────

        private GameObject _timelineModal;

        public void OpenTimeline()
        {
            if (Save == null) return;
            if (_timelineModal != null) Destroy(_timelineModal);
            _timelineModal = Modal("TimelinePopup", 660f, 760f, out var panel);
            var t = Label(panel, "Title", "타임라인 · 52주", 28, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -16f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));

            int sCount = ChapterGrading.CountS(Save);
            string sub = _gm.IsRetry ? $"재도전 중 · CH {Save.chapter}" : $"S급 {sCount} / {Timeline.Chapters}  ·  S급이 아닌 챕터를 눌러 다시 도전";
            var s = Label(panel, "Sub", sub, 16, Ink);
            Place(s.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(0f, 26f), new Vector2(0.5f, 1f));

            // 계절 띠 4줄 × 5칸
            const float cellW = 118f, cellH = 118f, gap = 8f;
            float startX = -(cellW * 5 + gap * 4) * 0.5f + cellW * 0.5f;
            for (int c = 1; c <= Timeline.Chapters; c++)
            {
                var rec = Save.chapters[c - 1];
                int row = (c - 1) / 5, col = (c - 1) % 5;
                bool current = c == Save.chapter;
                Color fill = rec != null && rec.cleared ? ChapterGrading.GradeColor(rec.grade) : new Color(0.86f, 0.87f, 0.90f);
                var cell = CoastUiArt.CutePill(panel, "CH" + c, fill, 16, current ? 5 : 3);
                Place(cell.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(startX + col * (cellW + gap), -100f - row * (cellH + gap + 22f)), new Vector2(cellW, cellH), new Vector2(0.5f, 1f));
                if (current)
                    cell.color = Coral;

                var num = Label(cell.transform, "Num", $"CH {c}", 15, Navy);
                Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 22f), new Vector2(0.5f, 1f));
                var grade = Label(cell.transform, "Grade", rec != null && rec.cleared ? ChapterGrading.GradeLabel(rec.grade) : "-", 34, Navy);
                Place(grade.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(80f, 44f), new Vector2(0.5f, 0.5f));
                var hearts = Label(cell.transform, "Hearts", rec != null && rec.cleared ? $"♥{rec.heartsEarned}/{rec.heartsTarget}" : $"{rec?.weekStart}~{rec?.weekEnd}주", 12, Ink);
                Place(hearts.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(0f, 20f), new Vector2(0.5f, 0f));

                if (_gm.CanRetry(c) && !_gm.IsRetry)
                {
                    var btn = cell.gameObject.AddComponent<Button>();
                    cell.raycastTarget = true;
                    btn.transition = Selectable.Transition.None;
                    int chapter = c;
                    btn.onClick.AddListener(() =>
                    {
                        Destroy(_timelineModal);
                        _timelineModal = null;
                        Confirm($"CH {chapter}로 돌아갈까?", $"{rec.weekStart}주차 상태로 다시 도전해. 더 좋은 결과만 기록에 덮어써.",
                            () => _gm.BeginRetry(chapter));
                    });
                }
            }
            // 계절 라벨
            string[] seasons = { "봄", "여름", "가을", "겨울" };
            for (int r = 0; r < 4; r++)
            {
                var sl = Label(panel, "Season" + r, seasons[r], 14, new Color(0.5f, 0.5f, 0.6f));
                sl.alignment = TextAnchor.MiddleLeft;
                Place(sl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f - r * (cellH + gap + 22f) + 18f), new Vector2(0f, 18f), new Vector2(0.5f, 1f));
                sl.rectTransform.offsetMin = new Vector2(24f, sl.rectTransform.offsetMin.y);
            }

            if (_gm.IsRetry)
            {
                BigButton(panel, "Cancel", "재도전 취소", new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(-120f, 16f), new Vector2(220f, 54f), () =>
                {
                    Destroy(_timelineModal); _timelineModal = null;
                    _gm.CancelRetry();
                });
            }
            else if (ChapterGrading.AllS(Save) && Save.reachedEnding != EndingKind.None)
            {
                BigButton(panel, "Ending", "송전탑에 다시 가보기", Coral, new Vector2(0.5f, 0f), new Vector2(-120f, 16f), new Vector2(260f, 54f), () =>
                {
                    Destroy(_timelineModal); _timelineModal = null;
                    _gm.ResolveEnding();
                });
            }

            Action closeTimeline = () =>
            {
                _modalPrimary = null;
                Destroy(_timelineModal);
                _timelineModal = null;
            };
            BigButton(panel, "Close", "닫기", new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(150f, 16f), new Vector2(200f, 54f), () => closeTimeline());
            _modalPrimary = closeTimeline;
        }
    }
}
