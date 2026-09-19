using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 「밥」 직 먹을 요리 고르기 — 112차 시안(나무 액자·점선·새싹/꽃·취소/먹기 알약) 표준.
    public static class MealPickUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void Open(GameManager gm, Action<string> onPicked, Action onCancel = null)
        {
            Close();
            if (gm?.Save == null) { onCancel?.Invoke(); return; }
            LifeItems.Ensure(gm.Save);
            var list = LifeItems.ListEdible(gm.Save);
            if (list.Count == 0) { onCancel?.Invoke(); return; }

            int sel = 0;
            float listH = list.Count <= 1 ? 0f : Mathf.Min(360f, list.Count * 84f + 8f);
            float h = list.Count <= 1 ? 420f : 280f + listH;
            var crt = StoryPopupKit.Frame("MealPick", 466, new Vector2(620f, h), out _canvas, 40f);

            TitlePlate(crt);
            SignDeco(crt, h, 40f);
            StoryPopupKit.TitleMeal(crt, Loc.T("오늘 뭐 먹을까?", "What shall we eat?"), -8f);

            Text nameT = null; Text fxT = null; Text badgeT = null; Text eatT = null;
            RectTransform badgeRt = null;
            Image[] rowImgs = null;

            void ShowDish(int idx)
            {
                sel = idx;
                var item = list[sel];
                string nm = LifeItems.Name(item.def);
                string fx = LifeItems.EffectText(item.def);
                if (string.IsNullOrEmpty(fx)) fx = Loc.T("든든한 한 끼", "A good meal");
                if (nameT != null) nameT.text = "✨ " + nm;
                if (fxT != null) fxT.text = "🌱 " + fx;
                if (badgeT != null) badgeT.text = "x" + item.n;
                // 이름 길이에 맞춰 배지를 이름 오른쪽에
                if (badgeRt != null && nameT != null)
                {
                    float approx = Mathf.Clamp(nm.Length * 18f + 40f, 80f, 220f);
                    badgeRt.anchoredPosition = new Vector2(approx * 0.5f + 28f, badgeRt.anchoredPosition.y);
                }
                // 135차(사용자 「선택이 안 된다」): 고른 줄이 구분이 안 됐다 — 분홍 테두리 + 체크 + 진한 크림으로 확실히
                if (rowImgs != null)
                    for (int i = 0; i < rowImgs.Length; i++)
                        if (rowImgs[i] != null)
                        {
                            bool on = i == sel;
                            rowImgs[i].color = on ? new Color(1f, 0.90f, 0.72f) : new Color(1f, 0.98f, 0.94f, 0.85f);
                            var ring = rowImgs[i].transform.Find("Ring"); if (ring != null) ring.gameObject.SetActive(on);
                            var chk = rowImgs[i].transform.Find("Chk"); if (chk != null) chk.gameObject.SetActive(on);
                        }
                if (eatT != null) eatT.text = Loc.T("먹기", "Eat") + (list.Count > 1 ? " · " + nm : "");
            }

            // 본문 — 시안처럼 가운데 이름 + 배지 + 효과 줄
            float nameY = list.Count <= 1 ? -150f : -120f;
            nameT = CoastHudLayout.MakeText(crt, "Name", "", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-160f, nameY - 50f), new Vector2(120f, nameY));
            nameT.color = StoryPopupKit.Ink; nameT.fontStyle = FontStyle.Bold; nameT.raycastTarget = false;
            nameT.resizeTextForBestFit = true; nameT.resizeTextMinSize = 18; nameT.resizeTextMaxSize = CoastHudLayout.Scaled(34);

            var badge = CoastUiArt.Panel(crt, "Badge", StoryPopupKit.BadgeOrange, 22); badge.raycastTarget = false;
            badgeRt = badge.rectTransform; badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 1f); badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(110f, nameY - 25f); badgeRt.sizeDelta = new Vector2(48f, 48f);
            badgeT = CoastHudLayout.MakeText(badgeRt, "T", "x1", 18, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            badgeT.color = Color.white; badgeT.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(badgeT, new Color(0.45f, 0.2f, 0.1f, 0.45f), 1.2f);

            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(-170f, nameY - 20f), 14, StoryPopupKit.Sparkle);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(-195f, nameY - 8f), 10, StoryPopupKit.Sparkle);

            fxT = CoastHudLayout.MakeText(crt, "Fx", "", 15, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, nameY - 95f), new Vector2(-24f, nameY - 55f));
            fxT.color = StoryPopupKit.Ink; fxT.raycastTarget = false;
            fxT.resizeTextForBestFit = true; fxT.resizeTextMinSize = 11; fxT.resizeTextMaxSize = CoastHudLayout.Scaled(15);

            if (list.Count > 1)
            {
                // 여러 요리 — 아래 목록으로 선택
                nameT.gameObject.SetActive(false);
                badge.gameObject.SetActive(false);
                fxT.gameObject.SetActive(false);
                var listHost = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
                listHost.SetParent(crt, false);
                listHost.anchorMin = new Vector2(0f, 0f); listHost.anchorMax = new Vector2(1f, 1f);
                listHost.offsetMin = new Vector2(28f, 100f); listHost.offsetMax = new Vector2(-28f, -96f);
                rowImgs = new Image[list.Count];
                float y = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    int idx = i;
                    var def = list[i].def; int n = list[i].n;
                    var row = CoastUiArt.CutePill(listHost, "R" + i, new Color(1f, 0.98f, 0.94f, 0.65f), 14, 0);
                    rowImgs[i] = row;
                    var rt = row.rectTransform; rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(0f, 76f);
                    row.raycastTarget = true;
                    var nm = CoastHudLayout.MakeText(rt, "N", "✨ " + LifeItems.Name(def), 20, TextAnchor.MiddleLeft,
                        Vector2.zero, Vector2.one, new Vector2(14f, 18f), new Vector2(-64f, 0f));
                    nm.color = StoryPopupKit.Ink; nm.fontStyle = FontStyle.Bold;
                    var b = CoastUiArt.Panel(rt, "B", StoryPopupKit.BadgeOrange, 14); b.raycastTarget = false;
                    var brr = b.rectTransform; brr.anchorMin = brr.anchorMax = new Vector2(1f, 0.5f); brr.pivot = new Vector2(1f, 0.5f);
                    brr.anchoredPosition = new Vector2(-8f, 8f); brr.sizeDelta = new Vector2(40f, 40f);
                    var bt = CoastHudLayout.MakeText(brr, "T", "x" + n, 15, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                    bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
                    var ef = CoastHudLayout.MakeText(rt, "E", "🌱 " + LifeItems.EffectText(def), 12, TextAnchor.LowerLeft,
                        Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, 34f));
                    ef.color = StoryPopupKit.Ink;
                    // 선택 표시: 분홍 테두리(Ring) + 체크(Chk) — ShowDish 가 켜고 끈다
                    var ring = CoastUiArt.Panel(rt, "Ring", new Color(0.93f, 0.35f, 0.55f), 16); ring.raycastTarget = false;
                    var rrt = ring.rectTransform; rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = new Vector2(-3f, -3f); rrt.offsetMax = new Vector2(3f, 3f);
                    ring.transform.SetAsFirstSibling();
                    var ringIn = CoastUiArt.Panel(ring.transform, "In", new Color(1f, 0.90f, 0.72f), 13); ringIn.raycastTarget = false;
                    var rirt = ringIn.rectTransform; rirt.anchorMin = Vector2.zero; rirt.anchorMax = Vector2.one; rirt.offsetMin = new Vector2(3f, 3f); rirt.offsetMax = new Vector2(-3f, -3f);
                    var chk = CoastHudLayout.MakeText(rt, "Chk", "✔", 22, TextAnchor.MiddleCenter, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, -14f), new Vector2(-60f, 18f));
                    chk.color = new Color(0.93f, 0.35f, 0.55f); chk.fontStyle = FontStyle.Bold; chk.raycastTarget = false;
                    ring.gameObject.SetActive(false); chk.gameObject.SetActive(false);
                    var rb = row.gameObject.AddComponent<Button>(); rb.transition = Selectable.Transition.None;
                    rb.onClick.AddListener(() => { CoastPrefs.Vibrate(); ShowDish(idx); });
                    y -= 80f;
                }
            }

            ShowDish(0);

            void Pick()
            {
                CoastPrefs.Vibrate();
                string id = list[sel].def.id;
                Close();
                onPicked?.Invoke(id);
            }

            StoryPopupKit.SoftPill(crt, "Cancel", Loc.T("취소", "Cancel"), StoryPopupKit.SoftBlue,
                new Vector2(-136f, 30f), new Vector2(236f, 72f), () => { Close(); onCancel?.Invoke(); }, backIcon: true);
            var eatBtn = StoryPopupKit.SoftPill(crt, "Eat", Loc.T("먹기", "Eat"), StoryPopupKit.SoftPink,
                new Vector2(136f, 30f), new Vector2(236f, 72f), Pick, forkIcon: true);
            eatT = eatBtn.GetComponentInChildren<Text>();
            if (eatT != null) { eatT.resizeTextForBestFit = true; eatT.resizeTextMinSize = 12; eatT.resizeTextMaxSize = CoastHudLayout.Scaled(22); }
            ShowDish(sel);
        }

        /// Dev/캡쳐용 — 세이브와 무관하게 시안 단일 카드(삼계탕 ×2)를 띄운다.
        public static void OpenDemo(Action onClose = null)
        {
            Close();
            var crt = StoryPopupKit.Frame("MealPick", 466, new Vector2(620f, 420f), out _canvas, 40f);
            TitlePlate(crt);
            SignDeco(crt, 420f, 40f);
            StoryPopupKit.TitleMeal(crt, Loc.T("오늘 뭐 먹을까?", "What shall we eat?"), -8f);

            const float nameY = -150f;
            var nameT = CoastHudLayout.MakeText(crt, "Name", "✨ " + Loc.T("삼계탕", "Ginseng Chicken Soup"), 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-160f, nameY - 50f), new Vector2(100f, nameY));
            nameT.color = StoryPopupKit.Ink; nameT.fontStyle = FontStyle.Bold; nameT.raycastTarget = false;

            var badge = CoastUiArt.Panel(crt, "Badge", StoryPopupKit.BadgeOrange, 22); badge.raycastTarget = false;
            var badgeRt = badge.rectTransform; badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 1f); badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(120f, nameY - 25f); badgeRt.sizeDelta = new Vector2(48f, 48f);
            var badgeT = CoastHudLayout.MakeText(badgeRt, "T", "x2", 18, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            badgeT.color = Color.white; badgeT.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(badgeT, new Color(0.45f, 0.2f, 0.1f, 0.45f), 1.2f);

            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(-175f, nameY - 18f), 14, StoryPopupKit.Sparkle);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(-198f, nameY - 6f), 10, StoryPopupKit.Sparkle);

            var fx = CoastHudLayout.MakeText(crt, "Fx",
                "🌱 " + Loc.T("배부름+40 · 컨디션+18 · 스트레스-5 · 체력+5", "Full+40 · Cond+18 · Stress-5 · Sta+5"),
                15, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, nameY - 95f), new Vector2(-24f, nameY - 55f));
            fx.color = StoryPopupKit.Ink; fx.raycastTarget = false;

            StoryPopupKit.SoftPill(crt, "Cancel", Loc.T("취소", "Cancel"), StoryPopupKit.SoftBlue,
                new Vector2(-136f, 30f), new Vector2(236f, 72f), () => { Close(); onClose?.Invoke(); }, backIcon: true);
            StoryPopupKit.SoftPill(crt, "Eat", Loc.T("먹기", "Eat"), StoryPopupKit.SoftPink,
                new Vector2(136f, 30f), new Vector2(236f, 72f), () => { Close(); onClose?.Invoke(); }, forkIcon: true);
        }


        /// 117차(사용자 시안): 밥 팝업은 **마당에 선 나무 간판**이다 — 아래로 기둥 두 개,
        ///   제목은 카드 위쪽에 걸친 크림 리본 판에 얹는다.
        static void SignDeco(RectTransform crt, float cardH, float cardY)
        {
            if (_canvas == null) return;
            var root = CoastUiCanvas.Root(_canvas);
            var postCol = new Color(0.60f, 0.42f, 0.29f);
            var postTop = new Color(0.72f, 0.53f, 0.37f);
            foreach (float fx in new[] { -210f, 210f })
            {
                var post = CoastUiArt.Panel(root, "Post", postCol, 9); post.raycastTarget = false;
                var rt = post.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(fx, cardY - cardH * 0.5f + 16f);
                rt.sizeDelta = new Vector2(26f, 210f);
                post.transform.SetSiblingIndex(1);   // 어둠 바로 위 = 액자 뒤
                var cap = CoastUiArt.Panel(root, "PostCap", postTop, 8); cap.raycastTarget = false;
                var crt2 = cap.rectTransform;
                crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 0.5f); crt2.pivot = new Vector2(0.5f, 1f);
                crt2.anchoredPosition = new Vector2(fx, cardY - cardH * 0.5f + 20f);
                crt2.sizeDelta = new Vector2(34f, 18f);
                cap.transform.SetSiblingIndex(1);
            }
        }

        /// 제목을 얹는 크림 리본 판(시안: 갈색 테두리 + 잎사귀 사이에 걸친 판).
        static void TitlePlate(RectTransform crt)
        {
            var edge = CoastUiArt.Panel(crt, "PlateEdge", new Color(0.62f, 0.45f, 0.32f), 26); edge.raycastTarget = false;
            var er = edge.rectTransform;
            er.anchorMin = new Vector2(0.5f, 1f); er.anchorMax = new Vector2(0.5f, 1f); er.pivot = new Vector2(0.5f, 1f);
            er.anchoredPosition = new Vector2(0f, 26f); er.sizeDelta = new Vector2(470f, 92f);
            var plate = CoastUiArt.Panel(crt, "Plate", new Color(0.996f, 0.95f, 0.82f), 22); plate.raycastTarget = false;
            var pr = plate.rectTransform;
            pr.anchorMin = new Vector2(0.5f, 1f); pr.anchorMax = new Vector2(0.5f, 1f); pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, 20f); pr.sizeDelta = new Vector2(458f, 80f);
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }
    }
}
