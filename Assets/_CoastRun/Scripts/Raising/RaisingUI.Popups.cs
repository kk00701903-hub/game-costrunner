using System;
using System.Collections;
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
            var kinds = PetShop.ForSale;
            _shopModal = Modal("ShopPopup", 620f, 150f + kinds.Length * 148f + 70f, out var panel);
            // 38차 시안: 크림 알약 제목 "펫 상점 🐾 · 보유 n" (양옆 주황 점)
            var tp = CoastUiArt.CutePill(panel, "TitlePill", Hex("#FFF3D6"), 20, 3); tp.raycastTarget = false;
            Place(tp.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(360f, 84f), new Vector2(0.5f, 1f));
            foreach (int sg in new[] { -1, 1 })
            {
                var dot = CoastUiArt.Panel(panel, "Dot", Hex("#FF9E3D"), 10); dot.raycastTarget = false;
                Place(dot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(sg * 215f, -52f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            }
            var t = Label(tp.transform, "Title", Loc.T("펫 상점", "Pet Shop"), 26, Navy); t.fontStyle = FontStyle.Bold;
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 36f), new Vector2(0.5f, 1f));
            var money = Label(tp.transform, "Money", Loc.T($"보유 {Save.stats.money:N0}", $"Coins {Save.stats.money:N0}"), 15, Ink);
            Place(money.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 24f), new Vector2(0.5f, 0f));

            Color[] rowCols = { Hex("#CFE7FF"), Hex("#FFD5E6"), Hex("#FFF0B8"), Hex("#D6F5D8"), Hex("#E6D9FF"), Hex("#FFE2C9") };
            for (int i = 0; i < kinds.Length; i++)
            {
                var k = kinds[i];
                bool owned = PetShop.Owns(Save, k);
                bool equipped = Save.equippedPet == k;
                var card = CoastUiArt.CutePill(panel, "Pet_" + k, rowCols[i % rowCols.Length], 20, 4);
                Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f - i * 148f), new Vector2(576f, 136f), new Vector2(0.5f, 1f));
                // SSR 배지(좌상단)
                var badge = CoastUiArt.CutePill(card.transform, "Badge", Hex("#FFB300"), 8, 2); badge.raycastTarget = false;
                Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 6f), new Vector2(50f, 22f), new Vector2(0f, 1f));
                var bl = Label(badge.transform, "T", "SSR", 11, Color.white); bl.fontStyle = FontStyle.Bold;
                // 펫 그림(흰 둥근 틀)
                var petTex = ArtAssets.LoadTexture("Obs_Pet_" + k);
                float textLeft = 136f;
                var frame = CoastUiArt.Panel(card.transform, "Frame", Color.white, 16); frame.raycastTarget = false;
                Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(108f, 108f), new Vector2(0f, 0.5f));
                if (petTex != null)
                {
                    var pi = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    pi.transform.SetParent(frame.transform, false);
                    pi.sprite = CoastUiArt.AsSprite(ChromaKeyed(petTex)); pi.preserveAspect = true; pi.raycastTarget = false;
                    Stretch(pi.rectTransform, 6f, 6f, -6f, -6f);
                    if (!owned) pi.color = new Color(0.8f, 0.8f, 0.83f, 1f);
                }
                var name = Label(card.transform, "Name", PetCompanion.Names[(int)k], 22, Navy);
                name.fontStyle = FontStyle.Bold; name.alignment = TextAnchor.MiddleLeft;
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));
                name.rectTransform.offsetMin = new Vector2(textLeft, -46f); name.rectTransform.offsetMax = new Vector2(-150f, -12f);
                var blurb = Label(card.transform, "Blurb", PetCompanion.Blurbs[(int)k], 14, Ink);
                blurb.alignment = TextAnchor.UpperLeft; blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(blurb.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                blurb.rectTransform.offsetMin = new Vector2(textLeft, 12f); blurb.rectTransform.offsetMax = new Vector2(-150f, -50f);
                // 가격 알약(우상단: 금화 + 숫자) / 보유·장착 표시
                var pp = CoastUiArt.CutePill(card.transform, "PricePill", Hex("#FFF6D6"), 12, 2); pp.raycastTarget = false;
                Place(pp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(132f, 32f), new Vector2(1f, 1f));
                var coinSp = CoastUiArt.Icon("Coin");
                var ci = CoastHudLayout.MakeImage(pp.transform, "Coin", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -11f), new Vector2(30f, 11f), Color.white);
                if (coinSp != null) { ci.sprite = coinSp; ci.preserveAspect = true; } else { ci.sprite = CoastUiArt.RoundedRect(11); ci.type = Image.Type.Sliced; ci.color = Hex("#FFC107"); }
                ci.raycastTarget = false;
                var price = Label(pp.transform, "Price", owned ? (equipped ? Loc.T("장착 중", "Equipped") : Loc.T("보유", "Owned")) : $"{PetShop.Price[k]:N0}", 15, Hex("#7A4A00")); price.alignment = TextAnchor.MiddleRight;
                price.fontStyle = FontStyle.Bold; price.rectTransform.offsetMin = new Vector2(34f, 0f); price.rectTransform.offsetMax = new Vector2(-10f, 0f);
                string label = !owned ? Loc.T("구매", "Buy") : equipped ? Loc.T("해제", "Unequip") : Loc.T("장착", "Equip");
                Color col = !owned ? (PetShop.CanAfford(Save, k) ? Hex("#4EA8FF") : new Color(0.65f, 0.65f, 0.7f)) : equipped ? new Color(0.6f, 0.62f, 0.7f) : Hex("#4EA8FF");
                BigButton(card.transform, "Act", label, col, new Vector2(1f, 0f), new Vector2(-14f, 12f), new Vector2(128f, 46f), () => ShopAct(k));
            }

            Action closeShop = () =>
            {
                _modalPrimary = null;
                Destroy(_shopModal);
                _shopModal = null;
                Refresh();
            };
            BigButton(panel, "Close", Loc.T("닫기", "Close"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(220f, 50f), () => closeShop());
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

        private static void AddCellButton(Image img, Action onClick)
        {
            img.raycastTarget = true;
            var btn = img.gameObject.GetComponent<Button>() ?? img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        // 8차: 3막 탭 — 1막 봄(1~5) / 2막 여름·가을(6~15) / 3막 겨울(16~20). 막마다 세로 풀스크린 Firefly 키아트(UI_Act1~3) 위에 챕터 카드.
        static readonly int[] ActStart = { 1, 6, 16 };
        static readonly int[] ActEnd = { 5, 15, 20 };
        int _actTab = -1;

        static string ActName(int act) => act == 0 ? Loc.T("1막 · 돌아온 사람", "Act 1 · The One Who Came Back")
            : act == 1 ? Loc.T("2막 · 같은 주파수", "Act 2 · The Same Frequency") : Loc.T("3막 · 마지막 노을", "Act 3 · The Last Sunset");
        static string ActSeason(int act) => act == 0 ? Loc.T("봄", "Spring") : act == 1 ? Loc.T("여름 · 가을", "Summer · Autumn") : Loc.T("겨울", "Winter");
        static string ActSynopsis(int act) => act == 0
            ? Loc.T("19살 봄. 유채꽃 길을 보드로 달려 송전탑까지.\n정류장에서 돌아온 도윤과 다시 마주친다.", "Spring, 19. Ride the rapeseed road to the tower.\nAt the bus stop, Doyun is back.")
            : act == 1 ? Loc.T("여름 축제와 가을 귤밭. 같은 주파수를 찾는 두 사람.\n노을 전에 닿아야 들리는 목소리가 있다.", "Summer festival, autumn orchards. Two people on one frequency.\nSome voices only reach you before sunset.")
            : Loc.T("눈 내리는 겨울. 마지막 노을을 향해 달린다.\n모든 챕터 S급이면, 송전탑이 답을 준다.", "Snow. Run toward the last sunset.\nRank S everywhere, and the tower answers.");

        static Texture2D ChapterThumb(int c)
        {
            string n = c.ToString("00");
            return ArtAssets.LoadTexture(   // 34차: 옛 컷씬 그림(Cut_*) 참조 제거
                c <= 5 ? "BG_TowerSunset" : c <= 10 ? "BG_Beach" : c <= 15 ? "BG_OrangeFarm" : "BG_TowerSnow");
        }

        /// 10차: 클리어한 챕터 선택지 — 재도전(그 주로 되돌아가 바로 달리기) / 오프닝 다시 보기 / 닫기.
        /// 37차: 비밀코드로 열린 미래 챕터 — 바로 점프하거나 오프닝 컷씬만 본다.
        void DevChapterModal(int chapter)
        {
            var modal = Modal("DevChapter", 560f, 360f, out var panel);
            var t = Label(panel, "Title", $"CH {chapter} 「{ChapterScript.Title(chapter)}」", 24, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            var b = Label(panel, "Body", Loc.T($"비밀코드로 열린 챕터야.\n점프하면 {Timeline.WeekStart(chapter)}주차부터 이 챕터를 키워. 지금 스탯은 그대로.", $"Opened by the secret code.\nJump to week {Timeline.WeekStart(chapter)} with your current stats."), 16, Ink);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(b.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            b.rectTransform.offsetMin = new Vector2(24f, 0f); b.rectTransform.offsetMax = new Vector2(-24f, 0f);
            float y = 150f;
            BigButton(panel, "Jump", Loc.T("▶ 이 챕터로 점프", "▶ Jump to this chapter"), Coral, new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(440f, 56f), () =>
            {
                _modalPrimary = null; Destroy(modal);
                _gm.DevJumpTo(chapter);
            });
            y -= 66f;
            // 38차: 오프닝 다시 보기는 타이틀 더보기(오프닝/레코드)로 — 육성 안 중복 제거
            BigButton(panel, "Close", Loc.T("닫기", "Close"), Hex("#B9B3AC"), new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(440f, 50f), () => { _modalPrimary = null; Destroy(modal); OpenTimeline(); });
        }

        void ChapterActionModal(int chapter, ChapterRecord rec, bool canRetry)
        {
            var modal = Modal("ChapterAction", 560f, 380f, out var panel);
            var t = Label(panel, "Title", $"CH {chapter} 「{ChapterScript.Title(chapter)}」", 24, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            string info = rec != null && rec.cleared
                ? Loc.T($"클리어 · {ChapterGrading.GradeLabel(rec.grade)}급 · 하트 {rec.heartsEarned}/{rec.heartsTarget}", $"Cleared · Rank {ChapterGrading.GradeLabel(rec.grade)} · hearts {rec.heartsEarned}/{rec.heartsTarget}")
                : Loc.T("재도전 중", "Retrying");
            var b = Label(panel, "Body", info + "\n" + (canRetry
                ? Loc.T($"다시 달리면 {rec.weekStart}주차로 돌아가 이 챕터를 다시 키워. 더 좋은 결과만 기록에 남아.", $"Running again rewinds to week {rec.weekStart}; only better results are kept.")
                : Loc.T("S급은 재도전할 게 없어.", "Rank S — nothing left to retry.")), 16, Ink);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(b.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.86f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            b.rectTransform.offsetMin = new Vector2(24f, 0f); b.rectTransform.offsetMax = new Vector2(-24f, 0f);
            float y = 150f;
            if (canRetry)
            {
                BigButton(panel, "Run", Loc.T("▶ 다시 달리기 (재도전)", "▶ Run again (retry)"), Coral, new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(440f, 56f), () =>
                {
                    _modalPrimary = null; Destroy(modal);
                    _gm.RetryRunPending = true;
                    _gm.BeginRetry(chapter);   // 육성 씬을 다시 여는데, Bind 에서 RetryRunPending 을 보고 곧장 런으로 간다
                });
                y -= 66f;
            }
            // 38차: 「오프닝 다시 보기」 제거 — 타이틀 더보기 › 오프닝과 중복

            BigButton(panel, "Close", Loc.T("닫기", "Close"), new Color(0.55f, 0.50f, 0.48f), new Vector2(0.5f, 0f), new Vector2(0f, Mathf.Max(16f, y)), new Vector2(440f, 52f), () => { _modalPrimary = null; Destroy(modal); OpenTimeline(); });
            _modalPrimary = () => { Destroy(modal); OpenTimeline(); };
        }

        /// 재도전 육성 화면이 열린 직후: 씬 전환이 끝나길 기다렸다가(플로우 busy 면 GoTo 가 무시된다) 런으로.
        IEnumerator RetryThenRun()
        {
            _busy = true;
            float t = 0f;
            while (_gm != null && _gm.FlowBusy && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            _busy = false;
            if (_gm != null && _gm.IsRetry) { _gm.RetryRunPending = false; _gm.StartStoryRun(); }
        }

        public void OpenTimeline()
        {
            if (Save == null) return;
            if (_actTab < 0) _actTab = Save.chapter <= 5 ? 0 : Save.chapter <= 15 ? 1 : 2;
            OpenTimeline(_actTab);
        }

        void OpenTimeline(int act)
        {
            if (Save == null) return;
            if (_timelineModal != null) Destroy(_timelineModal);
            _actTab = Mathf.Clamp(act, 0, 2);

            // 풀스크린 루트
            var rootGo = new GameObject("TimelinePopup", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(_overlay, false);
            var rootImg = rootGo.GetComponent<Image>(); rootImg.color = new Color(0.08f, 0.06f, 0.05f, 1f); rootImg.raycastTarget = true;
            var rrt = rootGo.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad); rrt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            _timelineModal = rootGo;
            var panel = rrt;

            // 막 키아트(cover)
            var artTex = ArtAssets.LoadTexture("UI_Act" + (_actTab + 1));
            if (artTex != null)
            {
                var maskGo = new GameObject("ArtMask", typeof(RectTransform), typeof(Image), typeof(Mask));
                maskGo.transform.SetParent(panel, false);
                var mrt = maskGo.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
                maskGo.GetComponent<Image>().raycastTarget = false; maskGo.GetComponent<Mask>().showMaskGraphic = false;
                var pic = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
                pic.transform.SetParent(maskGo.transform, false);
                pic.sprite = CoastUiArt.AsSprite(artTex); pic.raycastTarget = false;
                var prt = pic.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
                var fit = pic.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 810f / 1440f;
                // 카드가 놓이는 아래쪽을 살짝 어둡게
                var shade = CoastHudLayout.MakeImage(maskGo.transform, "Shade", new Vector2(0f, 0f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.08f, 0.28f));
                shade.raycastTarget = false;
            }

            // 상단: 막 탭 3개
            string[] tabShort = { Loc.T("1막", "Act 1"), Loc.T("2막", "Act 2"), Loc.T("3막", "Act 3") };
            for (int a = 0; a < 3; a++)
            {
                bool on = a == _actTab;
                bool unlocked = Save.chapter >= ActStart[a] || (Save.chapters[ActStart[a] - 1]?.cleared ?? false) || _gm.DevUnlockAll;   // 37차: 비밀코드면 전부
                var pill = CoastUiArt.CutePill(panel, "Tab" + a, on ? Coral : unlocked ? Hex("#F3E7CF") : Hex("#B9B3AC"), 14, 3);
                Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200f + a * 200f, -96f), new Vector2(180f, 46f), new Vector2(0.5f, 0.5f));   // 18차: 노치·상태바 아래로
                var tl = Label(pill.transform, "T", tabShort[a] + (unlocked ? "" : Loc.T(" (잠김)", " (locked)")), 18, on ? Color.white : Navy);
                CoastUiArt.OutlineText(tl, new Color(0f, 0f, 0f, on ? 0.35f : 0.12f), 1f);
                int act2 = a;
                if (unlocked) AddCellButton(pill, () => OpenTimeline(act2));
            }
            // 9차: 키아트는 위 45%가 그림, 아래 55%가 UI 자리(일부러 비워 그린 영역). 제목·시놉시스는 그림 바로 아래, 카드는 그 아래 남는 높이의 가운데.
            const float ArtBottom = 0.555f;
            // 18차: 제목·시놉시스 뒤에 어두운 띠 — 밝은 키아트 위에서 흰 글씨가 안 읽히던 문제
            var band = CoastHudLayout.MakeImage(panel, "TextBand", new Vector2(0f, ArtBottom), new Vector2(1f, ArtBottom), new Vector2(0f, -150f), new Vector2(0f, 10f), new Color(0.08f, 0.06f, 0.10f, 0.55f));
            band.raycastTarget = false;
            // 38차 시안: 제목은 분홍 유리 알약 위에 (♫)
            var titlePill = CoastUiArt.Panel(panel, "TitlePill", new Color(1f, 0.55f, 0.75f, 0.55f), 22); titlePill.raycastTarget = false;
            Place(titlePill.rectTransform, new Vector2(0.5f, ArtBottom), new Vector2(0.5f, ArtBottom), new Vector2(0f, -2f), new Vector2(430f, 46f), new Vector2(0.5f, 1f));
            var t = Label(panel, "Title", ActName(_actTab) + " ♫", 30, Color.white);
            CoastUiArt.OutlineText(t, new Color(0.1f, 0.08f, 0.12f, 0.85f), 1.6f);
            Place(t.rectTransform, new Vector2(0f, ArtBottom), new Vector2(1f, ArtBottom), new Vector2(0f, -4f), new Vector2(0f, 38f), new Vector2(0.5f, 1f));
            int sCount = ChapterGrading.CountS(Save);
            string sub = _gm.IsRetry ? Loc.T($"재도전 중 · CH {Save.chapter}", $"Retrying · CH {Save.chapter}")
                : $"{ActSeason(_actTab)}  ·  " + Loc.T($"S급 {sCount} / {Timeline.Chapters}", $"Rank S {sCount} / {Timeline.Chapters}");
            var sl = Label(panel, "Sub", sub, 18, new Color(1f, 0.96f, 0.88f));
            CoastUiArt.OutlineText(sl, new Color(0.1f, 0.08f, 0.12f, 0.8f), 1.2f);
            Place(sl.rectTransform, new Vector2(0f, ArtBottom), new Vector2(1f, ArtBottom), new Vector2(0f, -44f), new Vector2(0f, 22f), new Vector2(0.5f, 1f));
            var syn = Label(panel, "Synopsis", ActSynopsis(_actTab), 19, new Color(1f, 0.97f, 0.92f, 0.98f));
            syn.horizontalOverflow = HorizontalWrapMode.Wrap; syn.alignment = TextAnchor.UpperCenter;
            CoastUiArt.OutlineText(syn, new Color(0.1f, 0.08f, 0.12f, 0.7f), 1f);
            Place(syn.rectTransform, new Vector2(0f, ArtBottom), new Vector2(1f, ArtBottom), new Vector2(0f, -74f), new Vector2(-60f, 66f), new Vector2(0.5f, 1f));

            // 14차-8: 챕터 카드 — 가로 스크롤 큰 카드(챕터 번호 + 그림만). 5열 작은 카드는 폰에서 안 읽혔다.
            int first = ActStart[_actTab], last = ActEnd[_actTab];
            int count = last - first + 1;
            // 18차-3: 카드 3장이 어떤 비율에서도 다 보이게 — 실제 폭(폴드 22:9면 폭이 620대까지 줄어든다)으로 계산
            float panelW = Mathf.Clamp(panel.rect.width, 560f, 800f);
            float gap = 12f, cellW = Mathf.Floor((panelW - 40f - 2f * gap) / 3f), cellH = Mathf.Round(cellW * 1.62f);   // 38차: 시안(별·진행 바·캐릭터 수집 줄)만큼 더 길게
            float canvasH = Mathf.Max(1000f, panel.rect.height);   // 18차-3: 실제 캔버스 높이(디자인 단위)
            float zoneBottom = 96f, zoneTop = ArtBottom * canvasH - 160f;
            float zoneH = Mathf.Max(cellH + 20f, zoneTop - zoneBottom);
            var scrollGo = new GameObject("ChapterScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(panel, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            Place(srt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, zoneBottom + (zoneH - cellH) * 0.5f - 10f), new Vector2(0f, cellH + 20f), new Vector2(0.5f, 0f));
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.inertia = true;
            var content = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(srt, false);
            content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(0f, 1f); content.pivot = new Vector2(0f, 0.5f);
            float contentW = 20f + count * (cellW + gap) - gap + 20f;
            content.sizeDelta = new Vector2(contentW, 0f);
            sr.content = content; sr.viewport = srt;
            for (int c = first; c <= last; c++)
            {
                var rec = Save.chapters[c - 1];
                int idx = c - first;
                bool current = c == Save.chapter;
                bool cleared = rec != null && rec.cleared;
                bool locked = c > Save.chapter && !_gm.DevUnlockAll;   // 37차: 비밀코드면 미래 챕터도 열림
                bool devOpen = c > Save.chapter && _gm.DevUnlockAll;
                // 38차 시안: 완료=금색 틀, 지금=핑크(NEW), 잠김=회색
                Color fill = cleared ? Hex("#F6C34A") : current ? Hex("#FF7BA9") : locked ? Hex("#9A9AA6") : Hex("#EFE6D6");
                var cell = CoastUiArt.CutePill(content, "CH" + c, fill, 18, current ? 6 : 4);
                Place(cell.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f + idx * (cellW + gap), 0f), new Vector2(cellW, cellH), new Vector2(0f, 0.5f));

                // 그림: 카드 위쪽 대부분
                const float bandH = 150f;   // 38차: 아래 띠(번호·별·진행 바·캐릭터 수집)
                var thGo = new GameObject("Thumb", typeof(RectTransform), typeof(Image), typeof(Mask));
                thGo.transform.SetParent(cell.transform, false);
                var thr = thGo.GetComponent<RectTransform>();
                thr.anchorMin = new Vector2(0f, 0f); thr.anchorMax = new Vector2(1f, 1f);
                thr.offsetMin = new Vector2(8f, bandH); thr.offsetMax = new Vector2(-8f, -8f);
                var thMask = thGo.GetComponent<Image>(); thMask.sprite = CoastUiArt.RoundedRect(14); thMask.type = Image.Type.Sliced; thMask.color = Color.white; thMask.raycastTarget = false;
                thGo.GetComponent<Mask>().showMaskGraphic = false;
                var tt = locked ? null : ChapterThumb(c);
                if (tt != null)
                {
                    var ti = new GameObject("Img", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
                    ti.transform.SetParent(thGo.transform, false);
                    ti.sprite = CoastUiArt.AsSprite(tt); ti.raycastTarget = false;
                    var tir = ti.rectTransform; tir.anchorMin = Vector2.zero; tir.anchorMax = Vector2.one; tir.offsetMin = Vector2.zero; tir.offsetMax = Vector2.zero;
                    var tf = ti.GetComponent<AspectRatioFitter>(); tf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; tf.aspectRatio = 810f / 1440f;
                    if (!cleared && !current) ti.color = new Color(0.8f, 0.8f, 0.82f, 1f);
                }
                else
                {
                    var lockBg = CoastHudLayout.MakeImage(thGo.transform, "LockBg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.55f, 0.52f, 0.5f, 1f));
                    lockBg.raycastTarget = false;
                    var shackle = CoastUiArt.Panel(thGo.transform, "Shackle", new Color(1f, 1f, 1f, 0.85f), 14);
                    shackle.raycastTarget = false;
                    Place(shackle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(44f, 48f), new Vector2(0.5f, 0.5f));
                    var shackleHole = CoastUiArt.Panel(shackle.transform, "Hole", new Color(0.55f, 0.52f, 0.5f, 1f), 9);
                    shackleHole.raycastTarget = false;
                    Place(shackleHole.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(20f, 28f), new Vector2(0.5f, 0.5f));
                    var body = CoastUiArt.Panel(thGo.transform, "LockBody", new Color(1f, 1f, 1f, 0.9f), 8);
                    body.raycastTarget = false;
                    Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(60f, 48f), new Vector2(0.5f, 0.5f));
                    var lk = Label(thGo.transform, "Lock", Loc.T("잠김", "Locked"), 20, new Color(1f, 1f, 1f, 0.9f));
                    Place(lk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(160f, 30f), new Vector2(0.5f, 0.5f));
                }

                // ── 38차 시안 카드 ──
                // 좌상단 태그: 완료✓ / NEW / 잠김
                {
                    string tag = cleared ? Loc.T("완료 ✓", "Done ✓") : current ? "NEW" : locked ? Loc.T("잠김", "Locked") : "";
                    if (tag.Length > 0)
                    {
                        var tp = CoastUiArt.CutePill(cell.transform, "Tag", cleared ? Hex("#FFE68A") : current ? Hex("#FF4F7B") : Hex("#6E6E7A"), 10, 2);
                        Place(tp.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(64f, 26f), new Vector2(0f, 1f));
                        tp.raycastTarget = false;
                        var tpl = Label(tp.transform, "T", tag, 12, cleared ? Hex("#7A4A00") : Color.white); tpl.fontStyle = FontStyle.Bold;
                    }
                }
                // 번호 + 별 5개
                var num = Label(cell.transform, "Num", $"CH {c}", 28, current || locked ? Color.white : Navy);
                num.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(num, new Color(0f, 0f, 0f, current ? 0.35f : 0.12f), 1.2f);
                Place(num.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, bandH - 42f), new Vector2(0f, 36f), new Vector2(0.5f, 0f));
                int starN = cleared ? (rec.grade == ChapterGrade.S ? 5 : rec.grade == ChapterGrade.A ? 4 : rec.grade == ChapterGrade.B ? 3 : 2) : 0;
                var stars = Label(cell.transform, "Stars", new string('★', starN) + new string('☆', 5 - starN), 14, cleared ? Hex("#FFB300") : new Color(1f, 1f, 1f, 0.7f));
                Place(stars.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, bandH - 62f), new Vector2(0f, 20f), new Vector2(0.5f, 0f));
                // 진행 바(하트) + 보상
                {
                    int got = rec != null ? rec.heartsEarned : 0, tgt = rec != null && rec.heartsTarget > 0 ? rec.heartsTarget : ChapterGrading.HeartTarget(c);
                    var pl = Label(cell.transform, "PL", Loc.T("진행", "Progress"), 11, current || locked ? new Color(1f, 1f, 1f, 0.9f) : Navy); pl.alignment = TextAnchor.MiddleLeft;
                    Place(pl.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, bandH - 82f), new Vector2(40f, 16f), new Vector2(0f, 0f));
                    var track = CoastUiArt.Panel(cell.transform, "Track", new Color(0f, 0f, 0f, 0.18f), 6); track.raycastTarget = false;
                    Place(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, bandH - 80f), new Vector2(-96f, 12f), new Vector2(0.5f, 0f));
                    track.rectTransform.offsetMin = new Vector2(50f, bandH - 80f); track.rectTransform.offsetMax = new Vector2(-46f, bandH - 68f);
                    var fillBar = CoastUiArt.Panel(track.transform, "Fill", cleared ? Hex("#FF8A3D") : Hex("#FFD54A"), 6); fillBar.raycastTarget = false;
                    fillBar.rectTransform.anchorMin = new Vector2(0f, 0f); fillBar.rectTransform.anchorMax = new Vector2(tgt > 0 ? Mathf.Clamp01((float)got / tgt) : 0f, 1f);
                    fillBar.rectTransform.offsetMin = fillBar.rectTransform.offsetMax = Vector2.zero;
                    var pv = Label(cell.transform, "PV", locked ? $"--/{tgt}" : $"{got}/{tgt}", 11, current || locked ? new Color(1f, 1f, 1f, 0.9f) : Navy); pv.alignment = TextAnchor.MiddleRight;
                    Place(pv.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, bandH - 82f), new Vector2(44f, 16f), new Vector2(1f, 0f));
                    var rw = Label(cell.transform, "RW", cleared ? "+50" : locked ? "+??" : "+0", 11, Hex("#FFE68A")); rw.alignment = TextAnchor.MiddleRight;
                    Place(rw.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, bandH - 98f), new Vector2(44f, 16f), new Vector2(1f, 0f));
                }
                // 캐릭터 수집 줄: 얼굴 2개 + n/2
                {
                    var strip = CoastUiArt.Panel(cell.transform, "Strip", new Color(1f, 1f, 1f, 0.55f), 10); strip.raycastTarget = false;
                    Place(strip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(-16f, 40f), new Vector2(0.5f, 0f));
                    string[] faces = { "UI_Face_Girl", "UI_Face_Boy" };
                    int have = cleared ? 2 : current ? 1 : 0;
                    for (int f = 0; f < 2; f++)
                    {
                        var ft = ArtAssets.LoadTexture(faces[f]);
                        var fi = CoastHudLayout.MakeImage(strip.transform, "F" + f, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f + f * 34f, -15f), new Vector2(38f + f * 34f, 15f), f < have ? Color.white : new Color(0.3f, 0.3f, 0.35f, 0.8f));
                        if (ft != null) { fi.sprite = CoastUiArt.AsSprite(ft); fi.preserveAspect = true; } else { fi.sprite = CoastUiArt.RoundedRect(15); fi.type = Image.Type.Sliced; }
                        fi.raycastTarget = false;
                    }
                    var cs = Label(strip.transform, "CS", Loc.T($"캐릭터 수집 {have}/2", $"Characters {have}/2"), 10, Navy); cs.alignment = TextAnchor.MiddleRight;
                    Place(cs.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(96f, 20f), new Vector2(1f, 0.5f));
                }

                int chapter = c;
                bool canRetry = _gm.CanRetry(c) && !_gm.IsRetry;
                if (current && !cleared && !_gm.IsRetry)
                {
                    AddCellButton(cell, () =>
                    {
                        Destroy(_timelineModal); _timelineModal = null;
                        OnStoryPressed();
                    });
                }
                else if (devOpen)
                {
                    AddCellButton(cell, () =>
                    {
                        Destroy(_timelineModal); _timelineModal = null;
                        DevChapterModal(chapter);
                    });
                }
                else if (cleared || _gm.IsRetry && current)
                {
                    AddCellButton(cell, () =>
                    {
                        Destroy(_timelineModal); _timelineModal = null;
                        ChapterActionModal(chapter, rec, canRetry);
                    });
                    if (canRetry)
                    {
                        var retry = CoastUiArt.CutePill(cell.transform, "Retry", Coral, 10, 2);
                        Place(retry.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -12f), new Vector2(84f, 32f), new Vector2(1f, 1f));
                        var rl = Label(retry.transform, "T", Loc.T("재도전", "Retry"), 16, Color.white);
                        AddCellButton(retry, () =>
                        {
                            Destroy(_timelineModal); _timelineModal = null;
                            Confirm(Loc.T($"CH {chapter}로 돌아갈까?", $"Go back to CH {chapter}?"), Loc.T($"{rec.weekStart}주차 상태로 다시 도전해. 더 좋은 결과만 기록에 덮어써.", $"Retry from week {rec.weekStart}. Only better results are kept."),
                                () => _gm.BeginRetry(chapter));
                        });
                    }
                }
            }
            // 현재 챕터가 보이도록 스크롤
            if (Save.chapter >= first && Save.chapter <= last)
            {
                float viewW = 720f - CoastUiCanvas.HudPad * 2f;
                float target = 20f + (Save.chapter - first) * (cellW + gap) - (viewW - cellW) * 0.5f;
                float maxScroll = Mathf.Max(0f, contentW - viewW);
                sr.horizontalNormalizedPosition = maxScroll > 0f ? Mathf.Clamp01(target / maxScroll) : 0f;
            }

            Action closeTimeline = () =>
            {
                _modalPrimary = null;
                Destroy(_timelineModal);
                _timelineModal = null;
            };
            if (_gm.IsRetry)
            {
                BigButton(panel, "Cancel", Loc.T("재도전 취소", "Cancel retry"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(-120f, 22f), new Vector2(220f, 54f), () =>
                {
                    Destroy(_timelineModal); _timelineModal = null;
                    _gm.CancelRetry();
                });
                BigButton(panel, "Close", Loc.T("닫기", "Close"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(150f, 22f), new Vector2(200f, 54f), () => closeTimeline());
            }
            else if (ChapterGrading.AllS(Save) && Save.reachedEnding != EndingKind.None)
            {
                BigButton(panel, "Ending", Loc.T("송전탑에 다시 가보기", "Return to the tower"), Coral, new Vector2(0.5f, 0f), new Vector2(-120f, 22f), new Vector2(260f, 54f), () =>
                {
                    Destroy(_timelineModal); _timelineModal = null;
                    _gm.ResolveEnding();
                });
                BigButton(panel, "Close", Loc.T("닫기", "Close"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(150f, 22f), new Vector2(200f, 54f), () => closeTimeline());
            }
            else
            {
                // 9차: 현재 챕터가 이 막에 있으면 큰 CTA("CH n 시작") — 카드만 눌러야 하는 걸 몰라 헤매지 않게.
                bool inAct = Save.chapter >= first && Save.chapter <= last && Save.chapter >= 1 && Save.chapter <= Timeline.Chapters;
                var curRec = inAct ? Save.chapters[Save.chapter - 1] : null;
                bool showStart = inAct && (curRec == null || !curRec.cleared);
                if (showStart)
                {
                    BigButton(panel, "Start", Loc.T($"▶ CH {Save.chapter} 스토리", $"▶ Chapter {Save.chapter}"), Hex("#FF6FA0"), new Vector2(0.5f, 0f), new Vector2(-104f, 22f), new Vector2(276f, 54f), () =>
                    {
                        Destroy(_timelineModal); _timelineModal = null;
                        OnStoryPressed();
                    });
                    BigButton(panel, "Close", Loc.T("✕ 닫기", "✕ Close"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(150f, 22f), new Vector2(200f, 54f), () => closeTimeline());
                }
                else
                    BigButton(panel, "Close", Loc.T("닫기", "Close"), new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(240f, 54f), () => closeTimeline());
            }
            _modalPrimary = closeTimeline;
        }
    }
}
