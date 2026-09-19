using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 55차(사용자): 육성 생활 화면 3종 — ① 한 주가 지났다(WeekPassUI) ② 장보기(GroceryUI: 쌀·반찬·옷) ③ 쓰러짐(GameOverUI).
    internal static class LifeUiKit
    {
        public static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        public static readonly Color Cream = new Color(0.99f, 0.96f, 0.88f);
        public static Canvas Dim(string name, int order, out RectTransform root, Action onDim = null)
        {
            var canvas = CoastUiCanvas.Create(name, order);
            root = CoastUiCanvas.Root(canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.05f, 0.04f, 0.10f, 0.76f));
            dim.raycastTarget = true;
            if (onDim != null) { var b = dim.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => onDim()); }
            return canvas;
        }
        public static Button Btn(Transform parent, string name, string label, Color col, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int font = 20)
        {
            var b = CoastUiArt.GlossyPill(parent, name, col, 20, 7);
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size; b.raycastTarget = true;
            var t = CoastHudLayout.MakeText(rt, "T", label, font, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-4f, 0f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(font);
            var bt = b.gameObject.AddComponent<Button>(); bt.transition = Selectable.Transition.None;
            bt.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            return bt;
        }
    }

    /// ① 「일주일이 지났다」 — 시안: 리본 제목 + 파스텔 상태 카드(아이콘) + 배부름(하트)·컨디션(별) + 하단 노트.
    public static class WeekPassUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;
        private static readonly Color TitleInk = new Color(0.32f, 0.18f, 0.14f);
        private static readonly Color Pink = new Color(0.86f, 0.32f, 0.48f);
        private static readonly Color Ribbon = new Color(1f, 0.86f, 0.72f);
        // 110차(사용자 시안): 금빛 액자·초콜릿 리본
        private static readonly Color GoldDeep = new Color(0.78f, 0.60f, 0.30f);
        private static readonly Color GoldSoft = new Color(0.91f, 0.78f, 0.48f);
        private static readonly Color GoldLight = new Color(0.98f, 0.90f, 0.66f);
        private static readonly Color RibbonInk = new Color(0.42f, 0.27f, 0.20f);
        private const float RowStep = 78f;
        private const float MeterStep = 88f;
        private static readonly Color[] RowCols =
        {
            new Color(0.90f, 0.84f, 0.96f), // 주식 — 보라
            new Color(0.98f, 0.86f, 0.90f), // 반찬 — 분홍
            new Color(0.99f, 0.93f, 0.78f), // 잠 — 노랑
            new Color(0.82f, 0.90f, 0.98f), // 옷 — 하늘
            new Color(0.84f, 0.94f, 0.82f), // 텃밭 — 연두
        };

        public static void Show(int fromWeek, int toWeek, SeasonKind season, Survival.WeekReport rep, string nextNote, Action onDone)
        {
            Close();
            bool done = false;
            Action finish = () => { if (done) return; done = true; Close(); onDone?.Invoke(); };
            _canvas = LifeUiKit.Dim("WeekPassCanvas", 466, out var root, null);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);

            // 110차(사용자 시안): 금빛 덩굴 액자 + 크림 속지 + 초콜릿 리본 제목.
            bool hasNote = !string.IsNullOrEmpty(nextNote);
            int rows = 4 + (rep != null && rep.harvested > 0 ? 1 : 0) + (rep != null && rep.goodMonth ? 1 : 0) + (rep != null && rep.autoBuy != null ? 1 : 0);   // 105차: 좋은 한 달·정기 장보기 줄
            float listH = 12f + rows * RowStep;
            float h = 140f + listH + (3f * MeterStep + 8f) + (hasNote ? 60f : 0f) + 22f;

            var frame = CoastUiArt.Panel(root, "Frame", new Color(1f, 0.975f, 0.92f), 38); frame.raycastTarget = true;
            var frt0 = frame.rectTransform; frt0.anchorMin = frt0.anchorMax = new Vector2(0.5f, 0.5f); frt0.pivot = new Vector2(0.5f, 0.5f);
            frt0.anchoredPosition = new Vector2(0f, 10f); frt0.sizeDelta = new Vector2(684f, h + 44f);
            var gold = CoastUiArt.Panel(frt0, "Gold", GoldDeep, 34); gold.raycastTarget = false;
            Rect(gold.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            var goldIn = CoastUiArt.Panel(gold.transform, "GoldIn", GoldSoft, 30); goldIn.raycastTarget = false;
            Rect(goldIn.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var card = CoastUiArt.Panel(goldIn.transform, "Card", LifeUiKit.Cream, 26); card.raycastTarget = true;
            Rect(card.rectTransform, Vector2.zero, Vector2.one, new Vector2(13f, 13f), new Vector2(-13f, -13f));
            var crt = card.rectTransform;

            // 금빛 덩굴 — 네 모서리 + 위 가운데(액자 밖으로 살짝 뻗는다)
            CornerFlourish(frt0, new Vector2(0f, 1f)); CornerFlourish(frt0, new Vector2(1f, 1f));
            CornerFlourish(frt0, new Vector2(0f, 0f)); CornerFlourish(frt0, new Vector2(1f, 0f));
            TopFlourish(frt0);
            // 코너 꽃 — 시안대로 왼쪽 위 분홍 · 오른쪽 위 보라 · 아래 연두/주황
            PlaceFlower(frt0, new Vector2(0.018f, 0.95f), new Color(0.97f, 0.52f, 0.68f));
            PlaceFlower(frt0, new Vector2(0.982f, 0.95f), new Color(0.72f, 0.56f, 0.94f));
            PlaceFlower(frt0, new Vector2(0.018f, 0.05f), new Color(0.58f, 0.80f, 0.48f));
            PlaceFlower(frt0, new Vector2(0.982f, 0.05f), new Color(0.97f, 0.66f, 0.36f));
            // 109차(사용자): 꼬마 동행 — 액자 오른쪽 위에서 「누나, 일주일 지났어!」
            EventCardKit.Kid(frt0, Loc.T("누나, 일주일 지났어!", "A week went by!"), true, 150f);

            // 리본 제목 — 초콜릿 바탕 + 금테 + 양 끝 금빛 날개
            var ribbon = CoastUiArt.Panel(crt, "Ribbon", GoldDeep, 18);
            var rrt = ribbon.rectTransform; rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -18f); rrt.sizeDelta = new Vector2(520f, 70f); ribbon.raycastTarget = false;
            for (int s = -1; s <= 1; s += 2)
            {
                var wa = new Vector2(s < 0 ? 0f : 1f, 0.5f);
                GoldLeaf(rrt, wa, new Vector2(s * -14f, 13f), 36f, 12f, s * 24f, GoldSoft);
                GoldLeaf(rrt, wa, new Vector2(s * -14f, -13f), 36f, 12f, s * -24f, GoldSoft);
            }
            var ribIn = CoastUiArt.Panel(rrt, "RibIn", RibbonInk, 14); ribIn.raycastTarget = false;
            Rect(ribIn.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var moon = CoastHudLayout.MakeText(ribIn.rectTransform, "Moon", "☾", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 4f), new Vector2(62f, -4f));
            moon.color = new Color(0.99f, 0.86f, 0.42f); moon.fontStyle = FontStyle.Bold;
            var sun = CoastHudLayout.MakeText(ribIn.rectTransform, "Sun", "☀", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(58f, 4f), new Vector2(100f, -4f));
            sun.color = new Color(1f, 0.66f, 0.24f); sun.fontStyle = FontStyle.Bold;
            var title = CoastHudLayout.MakeText(ribIn.rectTransform, "Title", Loc.T("일주일이 지났다", "A week has passed"), 32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(102f, 0f), new Vector2(-18f, 0f));
            title.color = new Color(1f, 0.98f, 0.93f); title.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(title, new Color(0.18f, 0.09f, 0.05f, 0.55f), 2f);
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 16; title.resizeTextMaxSize = CoastHudLayout.Scaled(32);

            string subTxt = fromWeek == toWeek
                ? Loc.T($"{toWeek}주차 → {toWeek}주차 · {Timeline.SeasonName(season)} (챕터 마지막 주)", $"Week {toWeek} → {toWeek} · {Timeline.SeasonName(season)} (last)")
                : Loc.T($"{fromWeek}주차 → {toWeek}주차 · {Timeline.SeasonName(season)}", $"Week {fromWeek} → Week {toWeek} · {Timeline.SeasonName(season)}");
            var sub = CoastHudLayout.MakeText(crt, "Sub", subTxt, 19, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -120f), new Vector2(-20f, -92f));
            sub.color = TitleInk; sub.fontStyle = FontStyle.Bold;   // 110차(시안): 분홍 → 진한 갈색
            sub.resizeTextForBestFit = true; sub.resizeTextMinSize = 12; sub.resizeTextMaxSize = CoastHudLayout.Scaled(19);

            float y = -130f;
            if (rep != null)
            {
                int dishes = Mathf.Max(0, rep.riceLeft);
                ColorRow(crt, ref y, RowCols[0], "WK_Rice",
                    rep.ateRice
                        ? Loc.T($"남은 주식 마셔 {Mathf.Max(1, dishes)}", $"Drank remaining stock {Mathf.Max(1, dishes)}")
                        : Loc.T("식사를 안 했다… 「밥」으로 요리를 먹자", "No meal… eat with Feed"),
                    !rep.ateRice);
                ColorRow(crt, ref y, RowCols[1], "WK_Side",
                    rep.ateSide
                        ? Loc.T("요리반찬 효과 반영", "Meal / side dish effect applied")
                        : Loc.T("요리가 부족하거나 안 먹음", "Few/no dishes eaten"),
                    !rep.ateSide, cutlery: true);
                ColorRow(crt, ref y, RowCols[2], "WK_Sleep",
                    rep.slept ? Loc.T("잠 잤다", "Slept") : Loc.T("잠을 못 잤다", "Didn't sleep"),
                    !rep.slept);
                ColorRow(crt, ref y, RowCols[3], "WK_Shirt",
                    rep.clothesWorn
                        ? Loc.T("옷이 낡았다 — 새 옷을 사자", "Clothes worn out — buy new")
                        : Loc.T($"옷 {rep.clothesLeft}주 남음", $"Clothes {rep.clothesLeft}w left"),
                    rep.clothesWorn);
                if (rep.harvested > 0)
                {
                    string crops = rep.harvestNames != null && rep.harvestNames.Count > 0
                        ? string.Join(", ", rep.harvestNames)
                        : Loc.T($"{rep.harvested}개", $"{rep.harvested}");
                    ColorRow(crt, ref y, RowCols[4], "WK_Garden",
                        Loc.T($"텃밭에 다 자랐다: {crops} — 마이룸에서 수확", $"Garden ready: {crops} — harvest in My Room"),
                        false, drawTomatoPot: true);
                }
                // 105차(재미요소): 잘 산 4주 → 「좋은 한 달」 도장, 정기 장보기 결과
                if (rep.goodMonth)
                    ColorRow(crt, ref y, new Color(1f, 0.92f, 0.62f), "WK_Stamp", Loc.T("좋은 한 달! — 다음 주 성장 +20%", "A good month! — next week growth +20%"), false);
                if (rep.autoBuy != null)
                    ColorRow(crt, ref y, RowCols[0], "WK_Rice", Loc.T($"정기 장보기: {rep.autoBuy}", $"Auto grocery: {rep.autoBuy}"), false);
            }

            y -= 8f;
            if (rep != null)
            {
                Meter(crt, ref y, "Hunger", Loc.T("배부름", "Fullness"), "♥", "♥", "♡", new Color(1f, 0.93f, 0.95f), new Color(0.96f, 0.36f, 0.55f), rep.hungerBefore, rep.hungerAfter);
                Meter(crt, ref y, "Cond", Loc.T("컨디션", "Condition"), "★", "★", "☆", new Color(1f, 0.97f, 0.86f), new Color(0.98f, 0.70f, 0.15f), rep.condBefore, rep.condAfter);
                // 74차(사용자: 스트레스가 너무 적게 쌓인다): 결산에 스트레스 미터를 추가 — 한 주에 얼마나 쌓였는지,
                //   번아웃 한계(체력에 따라 70~90)까지 얼마 남았는지 카드에서 바로 보이게.
                bool over = rep.stressAfter >= rep.stressLimit;
                var stressInk = over ? new Color(0.92f, 0.28f, 0.28f)
                    : rep.stressAfter >= PlayerStats.StressWorn ? new Color(0.95f, 0.55f, 0.20f)
                    : new Color(0.55f, 0.42f, 0.86f);
                Meter(crt, ref y, "Stress", Loc.T($"스트레스 (한계 {rep.stressLimit})", $"Stress (limit {rep.stressLimit})"),
                    "◆", "◆", "◇", new Color(0.95f, 0.93f, 0.99f), stressInk, rep.stressBefore, rep.stressAfter);
                if (!rep.died && rep.condAfter <= 0)
                {
                    var warn = CoastHudLayout.MakeText(crt, "Warn", Loc.T("!! 컨디션 0 — 한 주 더 이러면 쓰러진다", "!! Condition 0 — one more week and she collapses"), 14, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y - 24f), new Vector2(-24f, y));
                    warn.color = new Color(0.85f, 0.15f, 0.20f); warn.fontStyle = FontStyle.Bold; y -= 26f;
                }
                else if (!rep.died && over)
                {
                    var warn = CoastHudLayout.MakeText(crt, "WarnS", Loc.T("!! 번아웃 — 이번 주는 놀기로 풀자", "!! Burnout — spend next week playing"), 14, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y - 24f), new Vector2(-24f, y));
                    warn.color = new Color(0.85f, 0.15f, 0.20f); warn.fontStyle = FontStyle.Bold; y -= 26f;
                }
            }
            if (hasNote)
            {
                var pill = CoastUiArt.CutePill(crt, "Next", new Color(0.55f, 0.42f, 0.72f), 14, 2); pill.raycastTarget = false;
                var prt = pill.rectTransform; prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(1f, 1f); prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, y - 4f); prt.sizeDelta = new Vector2(-48f, 48f);
                var nt = CoastHudLayout.MakeText(prt, "T", nextNote, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
                nt.color = Color.white; nt.fontStyle = FontStyle.Bold; nt.horizontalOverflow = HorizontalWrapMode.Wrap;
                nt.resizeTextForBestFit = true; nt.resizeTextMinSize = 11; nt.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            }

            var x = CoastUiArt.GlossyPill(root, "X", new Color(0.55f, 0.58f, 0.66f), 18, 5);
            var xrt = x.rectTransform; xrt.anchorMin = xrt.anchorMax = new Vector2(1f, 1f); xrt.pivot = new Vector2(1f, 1f);
            xrt.anchoredPosition = new Vector2(-CoastUiCanvas.HudPad - 16f, -CoastUiCanvas.HudPad - 16f); xrt.sizeDelta = new Vector2(56f, 56f);
            x.raycastTarget = true;
            foreach (var im in x.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
            var xb = x.gameObject.AddComponent<Button>(); xb.transition = Selectable.Transition.None;
            xb.targetGraphic = x;
            xb.onClick.AddListener(() => { CoastPrefs.Vibrate(); finish(); });
            var xt = CoastHudLayout.MakeText(xrt, "T", "✕", 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            xt.color = Color.white; xt.fontStyle = FontStyle.Bold; xt.raycastTarget = false;
            CoastUiArt.OutlineText(xt, new Color(0f, 0f, 0f, 0.4f), 1.5f);
            xrt.SetAsLastSibling();
            var ac = _canvas.gameObject.AddComponent<AutoCloser>(); ac.delay = AutoCloseSeconds; ac.act = finish;
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.35f);
        }

        private static void ColorRow(RectTransform crt, ref float y, Color bg, string icon, string text, bool bad, bool drawTomatoPot = false, bool cutlery = false)
        {
            var row = CoastUiArt.CutePill(crt, "Row", bg, 18, 3); row.raycastTarget = false;
            var rrt = row.rectTransform; rrt.anchorMin = new Vector2(0f, 1f); rrt.anchorMax = new Vector2(1f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, y); rrt.sizeDelta = new Vector2(-40f, 70f);

            var iconHost = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            iconHost.SetParent(rrt, false);
            iconHost.anchorMin = iconHost.anchorMax = new Vector2(0f, 0.5f);
            iconHost.pivot = new Vector2(0.5f, 0.5f);
            iconHost.anchoredPosition = new Vector2(38f, 0f);   // 110차(시안): 아이콘을 왼쪽 끝 가까이
            iconHost.sizeDelta = new Vector2(58f, 58f);

            var tex = ArtAssets.LoadTexture(icon) ?? (drawTomatoPot ? null : ArtAssets.LoadTexture("WK_Side"));
            if (tex != null)
            {
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(iconHost, false);
                im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; im.raycastTarget = false;
                var irt = im.rectTransform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = irt.offsetMax = Vector2.zero;
            }
            else if (drawTomatoPot) DrawTomatoPot(iconHost);

            float right = cutlery ? -74f : -18f;
            var t = CoastHudLayout.MakeText(rrt, "T", text, 17, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(78f, 6f), new Vector2(right, -6f));
            // 110차(시안): 나쁜 줄도 빨강이 아니라 조금 더 붉은 진한 갈색 — 시안과 같은 톤
            t.color = bad ? new Color(0.35f, 0.13f, 0.14f) : TitleInk; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(17);

            // 110차(시안): 반찬 줄 오른쪽 끝에 숟가락·포크
            if (cutlery) DrawCutlery(rrt);
            // 연두 행 반짝이
            if (drawTomatoPot)
            {
                var sp1 = CoastHudLayout.MakeText(rrt, "Sp1", "✦", 15, TextAnchor.MiddleCenter, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -24f), new Vector2(-6f, 2f));
                sp1.color = new Color(1f, 0.82f, 0.28f);
                var sp2 = CoastHudLayout.MakeText(rrt, "Sp2", "✦", 12, TextAnchor.MiddleCenter, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 6f), new Vector2(-2f, 26f));
                sp2.color = new Color(1f, 0.82f, 0.28f);
            }
            y -= RowStep;
        }

        /// 110차(시안): 반찬 줄 오른쪽 — 회색 숟가락 + 진회색 포크.
        private static void DrawCutlery(RectTransform row)
        {
            var host = new GameObject("Cutlery", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(row, false);
            host.anchorMin = host.anchorMax = new Vector2(1f, 0.5f); host.pivot = new Vector2(1f, 0.5f);
            host.anchoredPosition = new Vector2(-18f, 0f); host.sizeDelta = new Vector2(52f, 46f);
            var steel = new Color(0.60f, 0.64f, 0.71f);
            var dark = new Color(0.36f, 0.39f, 0.46f);
            // 숟가락
            var sb = CoastUiArt.Panel(host, "SpoonB", steel, 12); sb.raycastTarget = false;
            Rect(sb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -20f), new Vector2(20f, -2f));
            var ss = CoastUiArt.Panel(host, "SpoonS", steel, 4); ss.raycastTarget = false;
            Rect(ss.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(7f, 2f), new Vector2(15f, 28f));
            // 포크
            var fs = CoastUiArt.Panel(host, "ForkS", dark, 4); fs.raycastTarget = false;
            Rect(fs.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-17f, 2f), new Vector2(-9f, 26f));
            for (int i = 0; i < 3; i++)
            {
                var ti = CoastUiArt.Panel(host, "Tine" + i, dark, 3); ti.raycastTarget = false;
                Rect(ti.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f + i * 7f, -20f), new Vector2(-18f + i * 7f, -2f));
            }
        }

        /// 110차(시안): 금빛 잎 하나 — 둥근 막대를 돌려 덩굴 장식으로 쓴다.
        private static void GoldLeaf(RectTransform host, Vector2 anchor, Vector2 pos, float w, float hgt, float rot, Color col)
        {
            var im = CoastHudLayout.MakeImage(host, "Leaf", anchor, anchor,
                new Vector2(pos.x - w * 0.5f, pos.y - hgt * 0.5f), new Vector2(pos.x + w * 0.5f, pos.y + hgt * 0.5f), col);
            im.sprite = CoastUiArt.RoundedRect(Mathf.Max(3, Mathf.RoundToInt(Mathf.Min(w, hgt) * 0.5f)));
            im.type = Image.Type.Sliced; im.raycastTarget = false;
            im.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// 모서리 덩굴 — 가로로 두 갈래, 세로로 두 갈래.
        private static void CornerFlourish(RectTransform host, Vector2 anchor)
        {
            float dx = anchor.x < 0.5f ? 1f : -1f;
            float dy = anchor.y < 0.5f ? 1f : -1f;
            float r = -dx * dy;
            // 110차(시안): 굵은 막대 대신 가늘게 감기는 덩굴 — 가로 두 갈래, 세로 두 갈래, 안쪽에 작은 봉오리.
            GoldLeaf(host, anchor, new Vector2(dx * 34f, dy * 11f), 46f, 8f, r * 7f, GoldDeep);
            GoldLeaf(host, anchor, new Vector2(dx * 66f, dy * 17f), 30f, 7f, r * 16f, GoldSoft);
            GoldLeaf(host, anchor, new Vector2(dx * 11f, dy * 36f), 8f, 44f, r * 6f, GoldDeep);
            GoldLeaf(host, anchor, new Vector2(dx * 17f, dy * 66f), 7f, 28f, r * 15f, GoldSoft);
            GoldLeaf(host, anchor, new Vector2(dx * 22f, dy * 22f), 11f, 11f, 45f, GoldLight);
        }

        /// 위 가운데 덩굴 — 좌우 대칭으로 액자 위로 뻗는다.
        private static void TopFlourish(RectTransform host)
        {
            var a = new Vector2(0.5f, 1f);
            for (int s = -1; s <= 1; s += 2)
            {
                GoldLeaf(host, a, new Vector2(s * 26f, 6f), 44f, 9f, s * 11f, GoldDeep);
                GoldLeaf(host, a, new Vector2(s * 58f, 2f), 32f, 8f, s * 20f, GoldSoft);
                GoldLeaf(host, a, new Vector2(s * 14f, 16f), 22f, 8f, s * 40f, GoldSoft);
            }
            var dot = CoastHudLayout.MakeImage(host, "Bud", a, a, new Vector2(-6f, 5f), new Vector2(6f, 17f), GoldLight);
            dot.sprite = CoastUiArt.RoundedRect(6); dot.type = Image.Type.Sliced; dot.raycastTarget = false;
        }

        private static void DrawTomatoPot(RectTransform host)
        {
            var pot = CoastUiArt.Panel(host, "Pot", new Color(0.78f, 0.42f, 0.28f), 10);
            Rect(pot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            pot.rectTransform.pivot = new Vector2(0.5f, 0f); pot.rectTransform.anchoredPosition = new Vector2(0f, 2f); pot.rectTransform.sizeDelta = new Vector2(36f, 22f);
            var rim = CoastUiArt.Panel(host, "Rim", new Color(0.90f, 0.55f, 0.38f), 8);
            Rect(rim.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            rim.rectTransform.pivot = new Vector2(0.5f, 0f); rim.rectTransform.anchoredPosition = new Vector2(0f, 20f); rim.rectTransform.sizeDelta = new Vector2(42f, 8f);
            float[] xs = { -12f, 0f, 12f };
            for (int i = 0; i < 3; i++)
            {
                var leaf = CoastUiArt.Panel(host, "L" + i, new Color(0.35f, 0.72f, 0.35f), 6);
                Rect(leaf.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                leaf.rectTransform.pivot = new Vector2(0.5f, 0f); leaf.rectTransform.anchoredPosition = new Vector2(xs[i], 28f); leaf.rectTransform.sizeDelta = new Vector2(10f, 8f);
                var tom = CoastUiArt.Panel(host, "T" + i, new Color(0.92f, 0.22f, 0.22f), 12);
                Rect(tom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                tom.rectTransform.pivot = new Vector2(0.5f, 0f); tom.rectTransform.anchoredPosition = new Vector2(xs[i], 32f); tom.rectTransform.sizeDelta = new Vector2(14f, 14f);
            }
        }

        private static void PlaceFlower(RectTransform host, Vector2 anchor, Color col)
        {
            // 110차(시안): 액자 모서리 꽃 — 전보다 1.5배 크게, 꽃잎에 옅은 테두리
            for (int i = 0; i < 5; i++)
            {
                float ang = i * 72f * Mathf.Deg2Rad + 0.3f;
                var p = CoastHudLayout.MakeImage(host, "F", anchor, anchor,
                    new Vector2(Mathf.Cos(ang) * 13f - 9f, Mathf.Sin(ang) * 13f - 9f),
                    new Vector2(Mathf.Cos(ang) * 13f + 9f, Mathf.Sin(ang) * 13f + 9f),
                    new Color(col.r, col.g, col.b, 0.95f));
                p.raycastTarget = false; p.sprite = CoastUiArt.RoundedRect(28);
            }
            var c = CoastHudLayout.MakeImage(host, "FC", anchor, anchor, new Vector2(-6f, -6f), new Vector2(6f, 6f), new Color(1f, 0.92f, 0.45f, 0.98f));
            c.raycastTarget = false; c.sprite = CoastUiArt.RoundedRect(24);
        }

        private static void Rect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        /// 배부름/컨디션/스트레스: 큰 글리프 + 제목 + **둥근 트랙 바**(칸 기호가 바 위에) + 「↓ before → after」.
        ///   110차(사용자 시안): 기호만 늘어놓던 것을 어두운 트랙 + 색 채움 위에 얹고, 화살표는 내려가면 파랑·올라가면 빨강.
        private static void Meter(RectTransform crt, ref float y, string name, string label, string bigGlyph, string full, string empty, Color bg, Color accent, int before, int after)
        {
            var box = CoastUiArt.CutePill(crt, name, bg, 18, 3); box.raycastTarget = false;
            var brt = box.rectTransform; brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, y); brt.sizeDelta = new Vector2(-40f, 78f);
            var g = CoastHudLayout.MakeText(brt, "G", bigGlyph, 40, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 4f), new Vector2(66f, -4f));
            g.color = accent; g.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(g, new Color(0.25f, 0.15f, 0.10f, 0.35f), 2f);
            var l = CoastHudLayout.MakeText(brt, "L", label, 17, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(74f, -36f), new Vector2(-150f, -6f));
            l.color = Color.Lerp(accent, TitleInk, 0.45f); l.fontStyle = FontStyle.Bold;

            float pct = Mathf.Clamp01(after / 100f);
            var track = CoastUiArt.Panel(brt, "Track", Color.Lerp(accent, new Color(0.22f, 0.15f, 0.16f), 0.62f), 13); track.raycastTarget = false;
            Rect(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(74f, 10f), new Vector2(-150f, 36f));
            if (pct > 0.001f)
            {
                var fill = CoastUiArt.Panel(track.transform, "Fill", accent, 11); fill.raycastTarget = false;
                var flr = fill.rectTransform; flr.anchorMin = new Vector2(0f, 0f); flr.anchorMax = new Vector2(pct, 1f);
                flr.offsetMin = new Vector2(3f, 3f); flr.offsetMax = new Vector2(-3f, -3f);
            }
            int filled = Mathf.Clamp(Mathf.RoundToInt(pct * 10f), 0, 10);
            string meter = ""; for (int i = 0; i < 10; i++) meter += i < filled ? full : empty;
            var m = CoastHudLayout.MakeText(track.rectTransform, "M", meter, 17, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-8f, 0f));
            m.color = Color.Lerp(accent, Color.white, 0.55f); m.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(m, new Color(0.20f, 0.12f, 0.10f, 0.45f), 1.2f);
            m.raycastTarget = false;

            string arrow = after < before ? "<color=#3E82F7>↓</color>" : after > before ? "<color=#E2483C>↑</color>" : "<color=#8D6E63>→</color>";
            var v = CoastHudLayout.MakeText(brt, "V", $"{arrow} {before} → {after}", 17, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, -18f), new Vector2(-16f, 18f));
            v.color = TitleInk; v.fontStyle = FontStyle.Bold; v.supportRichText = true;
            y -= MeterStep;
        }

        public static float AutoCloseSeconds = 3.0f;   // 74차(사용자): 결산 카드는 3초 떠 있게(전 2초)
        private class AutoCloser : MonoBehaviour { public Action act; public float delay = 2f; private System.Collections.IEnumerator Start() { yield return new WaitForSecondsRealtime(delay); act?.Invoke(); } }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }

    /// ② 장보기 — 돈으로 쌀(주 단위)·반찬·옷(12주). 씨앗은 마이룸에서.
    public static class GroceryUI
    {
        private static Canvas _canvas; private static RectTransform _root; private static GameManager _gm; private static Action _onClose;
        public static bool IsOpen => _canvas != null;

        /// 73차(사용자): 펫상점과 통합 — ShopUI(일반 탭)로 연다. 옛 화면은 OpenLegacy.
        public static void Open(GameManager gm, Action onClose = null) { ShopUI.Open(gm, 0, onClose); }

        public static void OpenLegacy(GameManager gm, Action onClose = null)
        {
            Close(); _gm = gm; _onClose = onClose;
            if (gm == null || gm.Save == null) return;
            _canvas = LifeUiKit.Dim("GroceryCanvas", 462, out _root, Close);
            Build();
        }

        private static void Build()
        {
            foreach (Transform c in _root) if (c.name == "Card") UnityEngine.Object.Destroy(c.gameObject);
            var s = _gm.Save;
            var card = CoastUiArt.CutePill(_root, "Card", LifeUiKit.Cream, 28, 5);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 20f); crt.sizeDelta = new Vector2(640f, 700f); card.raycastTarget = true;
            var title = CoastHudLayout.MakeText(crt, "Title", Loc.T("장보기 · 마을 가게", "Groceries · Village shop"), 28, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -16f));
            title.color = LifeUiKit.Navy; title.fontStyle = FontStyle.Bold;
            var wallet = CoastHudLayout.MakeText(crt, "Wallet", Loc.T($"돈 {LevelSystem.FormatK(s.stats.money)}G", $"Money {LevelSystem.FormatK(s.stats.money)}G") + "   ·   " + Survival.Summary(s), 13, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -100f), new Vector2(-10f, -66f));
            wallet.color = new Color(0.45f, 0.30f, 0.10f); wallet.fontStyle = FontStyle.Bold; wallet.horizontalOverflow = HorizontalWrapMode.Wrap;

            Row(crt, 0, Loc.T("쌀 1주분", "Rice · 1 week"), Loc.T("주마다 1주분을 먹는다. 없으면 굶는다.", "Eaten weekly. None = starving."), Survival.RicePrice, new Color(1f, 0.96f, 0.80f), Loc.T($"보유 {s.rice}주분", $"Have {s.rice}w"),
                () => { if (Survival.BuyRice(s)) Bought(Loc.T("쌀을 샀어.", "Bought rice.")); else Short(); },
                () => { if (Survival.BuyRice(s, 4)) Bought(Loc.T("쌀 4주분을 샀어.", "Bought 4 weeks of rice.")); else Short(); }, 4);
            Row(crt, 1, Loc.T("반찬 1주분", "Side dish · 1 week"), Loc.T("배부름·컨디션 ↑. 텃밭 채소로도 얻는다.", "Fullness/condition ↑. Also from the garden."), Survival.SidePrice, new Color(0.88f, 0.97f, 0.86f), Loc.T($"보유 {s.sideDish}", $"Have {s.sideDish}"),
                () => { if (Survival.BuySide(s)) Bought(Loc.T("반찬을 샀어.", "Bought side dish.")); else Short(); },
                () => { if (Survival.BuySide(s, 4)) Bought(Loc.T("반찬 4주분을 샀어.", "Bought 4 weeks of side dish.")); else Short(); }, 4);
            Row(crt, 2, Loc.T("새 옷 (12주)", "New clothes (12 wk)"), Loc.T("옷은 3개월이면 낡아서 못 입는다. 낡으면 컨디션·매력 ↓", "Wears out in 3 months. Worn = condition/charm ↓"), Survival.ClothesPrice, new Color(0.86f, 0.90f, 1f), s.clothesWeeks <= 0 ? Loc.T("낡음!", "Worn!") : Loc.T($"{s.clothesWeeks}주 남음", $"{s.clothesWeeks}w left"),
                () => { if (Survival.BuyClothes(s)) Bought(Loc.T("새 옷! 12주 동안 입는다.", "New clothes! Good for 12 weeks.")); else Short(); }, null, 0);
            var hint = CoastHudLayout.MakeText(crt, "Hint", Loc.T("씨앗은 마이룸 화분에서 · 채소는 자라면 저절로 반찬이 된다", "Seeds: My Room pots · veggies become side dishes"), 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 78f), new Vector2(-10f, 108f));
            hint.color = new Color(0.45f, 0.40f, 0.36f);
            LifeUiKit.Btn(crt, "Close", Loc.T("닫기", "Close"), new Color(0.62f, 0.62f, 0.70f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(220f, 50f), Close, 18);
        }

        private static void Row(RectTransform parent, int i, string name, string blurb, int price, Color col, string have, Action buy1, Action buyN, int n)
        {
            var row = CoastUiArt.CutePill(parent, "Row" + i, col, 18, 3); row.raycastTarget = false;
            var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -112f - i * 156f); rrt.sizeDelta = new Vector2(590f, 144f);
            var nm = CoastHudLayout.MakeText(row.transform, "N", name, 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -44f), new Vector2(-180f, -8f));
            nm.color = LifeUiKit.Navy; nm.fontStyle = FontStyle.Bold;
            var hv = CoastHudLayout.MakeText(row.transform, "H", have, 14, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -40f), new Vector2(-14f, -10f));
            hv.color = new Color(0.48f, 0.29f, 0f); hv.fontStyle = FontStyle.Bold;
            var bl = CoastHudLayout.MakeText(row.transform, "B", blurb, 13, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 56f), new Vector2(-20f, -48f));
            bl.color = new Color(0.30f, 0.28f, 0.42f); bl.horizontalOverflow = HorizontalWrapMode.Wrap;
            LifeUiKit.Btn(row.transform, "Buy1", $"{price}G", new Color(0.31f, 0.66f, 1f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(120f, 44f), buy1, 17);
            if (buyN != null) LifeUiKit.Btn(row.transform, "BuyN", Loc.T($"{n}주분 {price * n}G", $"×{n} {price * n}G"), new Color(0.45f, 0.55f, 0.85f), new Vector2(1f, 0f), new Vector2(-142f, 10f), new Vector2(150f, 44f), buyN, 15);
        }
        private static void Bought(string msg) { CoastToast.Show(msg); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f); _gm.Persist(); Build(); }
        private static void Short() { CoastToast.Show(Loc.T("돈이 모자라 — 알바나 대회로 벌자", "Not enough money — work or race")); CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.4f); }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null;
            var cb = _onClose; _onClose = null; cb?.Invoke();
        }
    }

    /// ③ 쓰러짐 — 다마고치의 죽음. 시안: 밤하늘 + 크림 카드(꽃) + 잠든 포즈 + 병원/처음부터.
    public static class GameOverUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void Show(GameManager gm, Sprite girl, Action onRevive)
        {
            Close();
            var s = gm != null ? gm.Save : null; if (s == null) return;
            _canvas = CoastUiCanvas.Create("GameOverCanvas", 480);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            var root = CoastUiCanvas.Root(_canvas);
            float pad = CoastUiCanvas.HudPad;

            // 밤하늘 딤
            var night = CoastHudLayout.MakeImage(root, "Night", Vector2.zero, Vector2.one,
                new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f),
                new Color(0.06f, 0.08f, 0.22f, 0.94f));
            night.raycastTarget = true;
            PaintNightSky(root);

            // 크림 카드
            var cardSize = new Vector2(620f, 980f);   // 94차 시안: 화면을 거의 채우는 큰 카드
            var glow = CoastUiArt.Panel(root, "Glow", new Color(1f, 1f, 1f, 0.28f), 36); glow.raycastTarget = false;
            var gr = glow.rectTransform; gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f); gr.pivot = new Vector2(0.5f, 0.5f);
            gr.anchoredPosition = Vector2.zero; gr.sizeDelta = cardSize + new Vector2(18f, 18f);
            var cardImg = CoastUiArt.CutePill(root, "Card", new Color(0.99f, 0.96f, 0.90f), 32, 5);
            var card = cardImg.rectTransform; card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f); card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero; card.sizeDelta = cardSize; cardImg.raycastTarget = true;
            DecorateFlowers(card);

            // 잠든 하늘 일러스트 (Sleep 우선, 없으면 전달 스프라이트)
            Sprite pose = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Raise_Girl_Pose_SleepLying")) ?? CoastUiArt.AsSprite(ArtAssets.LoadTexture("Raise_Girl_Pose_Sleep")) ?? girl
                ?? CoastUiArt.AsSprite(ArtAssets.LoadTexture("Raise_Girl_Pose_Cry"));
            if (pose != null)
            {
                var im = new GameObject("Girl", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(card, false); im.sprite = pose; im.preserveAspect = true; im.raycastTarget = false;
                var rt = im.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                // 95차: 누운 그림(가로가 긴 SleepLying)은 카드 폭에 맞춰 크게, 선 그림은 예전 크기
                bool wide = pose.rect.width > pose.rect.height * 1.3f;
                rt.anchoredPosition = new Vector2(0f, wide ? -60f : -30f); rt.sizeDelta = wide ? new Vector2(560f, 260f) : new Vector2(340f, 300f);
            }
            var zzz = CoastHudLayout.MakeText(card, "Zzz", "z z z", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, -80f), new Vector2(180f, -46f));
            zzz.color = new Color(0.55f, 0.62f, 0.85f, 0.85f); zzz.fontStyle = FontStyle.Bold; zzz.raycastTarget = false;

            // 분홍 젤리 제목
            EventCardKit.JellyTitle(card, Loc.T("하늘이 쓰러졌다…", "Haneul collapsed…"),
                new Color(0.98f, 0.42f, 0.62f), new Color(0.55f, 0.12f, 0.28f), 330f, 84f, 50);

            string why = s.starveWeeks >= 4 ? Loc.T("몇 주째 쌀이 없었다.", "No rice for weeks.")
                : s.sleepDebt >= 2 ? Loc.T("잠을 계속 안 잤다.", "Kept skipping sleep.")
                : Loc.T("배고프고 지친 채로 컨디션이 바닥났다.", "Hungry, exhausted, condition hit zero.");
            string meta = Loc.T($"{s.week}주차 · Lv {Mathf.Max(1, s.level)} · 돈 {LevelSystem.FormatK(s.stats.money)}G",
                $"Week {s.week} · Lv {Mathf.Max(1, s.level)} · {LevelSystem.FormatK(s.stats.money)}G");

            var info = EventCardKit.InfoBox(card, 430f, 100f, 40f);
            var infoT = CoastHudLayout.MakeText(info, "Info", why + "\n" + meta, 17, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            infoT.fontSize = CoastHudLayout.Scaled(19);
            infoT.color = EventCardKit.Ink; infoT.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoT.fontStyle = FontStyle.Bold;

            // 병원에서 깨어나기 (파랑) — 두 줄
            TwoLineAction(card, "Hospital", "Icon_Heart",
                Loc.T("병원에서 깨어나기", "Wake up in hospital"),
                Loc.T("(돈 20% · 이 주에서 계속)", "(−20% money · continue this week)"),
                new Color(0.35f, 0.62f, 0.95f), new Vector2(0f, 214f), new Vector2(520f, 104f), () =>
                {
                    Survival.Revive(s); gm.Persist(); Close(); onRevive?.Invoke();
                    CoastToast.Show(Loc.T("병원에서 깨어났다. 쌀부터 사자.", "Woke up in hospital. Buy rice first."));
                });

            // 처음부터 다시 (분홍)
            TwoLineAction(card, "Restart", "Icon_Refresh",
                Loc.T("처음부터 다시", "Start over"),
                Loc.T("(컬렉션 기록은 유지)", "(Collection records kept)"),
                new Color(0.95f, 0.48f, 0.58f), new Vector2(0f, 90f), new Vector2(520f, 104f), () =>
                {
                    Close(); gm.RestartAfterDeath();
                });

            // 하단 꽃 푸터
            var footBar = CoastUiArt.Panel(card, "FootBar", new Color(0.96f, 0.90f, 0.82f, 0.95f), 14); footBar.raycastTarget = false;
            var fr = footBar.rectTransform; fr.anchorMin = new Vector2(0f, 0f); fr.anchorMax = new Vector2(1f, 0f); fr.pivot = new Vector2(0.5f, 0f);
            fr.offsetMin = new Vector2(40f, 26f); fr.offsetMax = new Vector2(-40f, 66f);
            var foot = CoastHudLayout.MakeText(fr, "Foot",
                Loc.T("❀  슬프지만 다시 일어날 수 있어요 · 다시 도전하기  ❀", "❀  It's sad, but you can get back up · Try again  ❀"),
                13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            foot.color = new Color(0.45f, 0.32f, 0.28f); foot.raycastTarget = false;

            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.8f);
        }

        private static void TwoLineAction(RectTransform card, string name, string icon, string main, string sub, Color col, Vector2 pos, Vector2 size, Action onClick)
        {
            var b = CoastUiArt.GlossyPill(card, name, col, 24, 8);
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; b.raycastTarget = true;

            float textLeft = 12f;
            var sp = CoastUiArt.Art(icon);
            if (sp != null)
            {
                var circle = CoastUiArt.Panel(rt, "IcBg", new Color(1f, 1f, 1f, 0.28f), 22); circle.raycastTarget = false;
                var cr = circle.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0f, 0.5f); cr.pivot = new Vector2(0f, 0.5f);
                cr.anchoredPosition = new Vector2(16f, 0f); cr.sizeDelta = new Vector2(56f, 56f);
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(cr, false); im.sprite = sp; im.preserveAspect = true; im.color = Color.white; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.sizeDelta = new Vector2(34f, 34f);
                textLeft = 84f;
            }

            // 94차: 위 줄(큰 글자) / 아래 줄(작은 괄호 설명) 영역을 나눠 둘 다 보이게
            var t1 = CoastHudLayout.MakeText(rt, "Main", main, 24, TextAnchor.LowerLeft,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(textLeft, -2f), new Vector2(-14f, -10f));
            t1.color = Color.white; t1.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t1, new Color(0f, 0f, 0f, 0.28f), 1.2f);
            t1.resizeTextForBestFit = true; t1.resizeTextMinSize = 12; t1.resizeTextMaxSize = CoastHudLayout.Scaled(24);
            var t2 = CoastHudLayout.MakeText(rt, "Sub", sub, 15, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(textLeft, 10f), new Vector2(-14f, 0f));
            t2.color = new Color(1f, 1f, 1f, 0.92f); t2.raycastTarget = false; t2.fontStyle = FontStyle.Bold;
            t2.resizeTextForBestFit = true; t2.resizeTextMinSize = 10; t2.resizeTextMaxSize = CoastHudLayout.Scaled(15);

            EventCardKit.Sparkle(rt, new Vector2(1f, 1f), new Vector2(-16f, -12f), 12, new Color(1f, 1f, 1f, 0.9f));
            var bt = b.gameObject.AddComponent<Button>(); bt.transition = Selectable.Transition.None;
            bt.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
        }

        private static void PaintNightSky(RectTransform root)
        {
            var moon = CoastHudLayout.MakeText(root, "Moon", "☾", 56, TextAnchor.MiddleCenter,
                new Vector2(0.82f, 0.88f), new Vector2(0.82f, 0.88f), new Vector2(-36f, -36f), new Vector2(36f, 36f));
            moon.color = new Color(0.95f, 0.92f, 0.75f, 0.9f); moon.raycastTarget = false;
            System.Random rng = new System.Random(19);
            for (int i = 0; i < 28; i++)
            {
                float ax = 0.05f + (float)rng.NextDouble() * 0.9f;
                float ay = 0.08f + (float)rng.NextDouble() * 0.9f;
                int sz = 8 + rng.Next(14);
                var st = CoastHudLayout.MakeText(root, "St" + i, "✦", sz, TextAnchor.MiddleCenter,
                    new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(-sz, -sz), new Vector2(sz, sz));
                st.color = new Color(1f, 0.95f, 0.8f, 0.35f + (float)rng.NextDouble() * 0.55f);
                st.raycastTarget = false;
            }
        }

        private static void DecorateFlowers(RectTransform card)
        {
            // 모서리 꽃 장식 (상점 시안과 같은 파스텔 스티커 톤)
            string[] glyphs = { "❀", "✿", "❁", "✾" };
            Color[] cols =
            {
                new Color(0.95f, 0.55f, 0.70f), new Color(0.70f, 0.60f, 0.95f),
                new Color(0.95f, 0.78f, 0.40f), new Color(0.55f, 0.82f, 0.70f),
            };
            Vector2[] corners =
            {
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0.55f), new Vector2(1f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
            };
            Vector2[] offs =
            {
                new Vector2(22f, -22f), new Vector2(-22f, -28f), new Vector2(24f, 28f), new Vector2(-24f, 24f),
                new Vector2(18f, 0f), new Vector2(-18f, 0f), new Vector2(0f, -16f), new Vector2(0f, 18f),
            };
            for (int i = 0; i < corners.Length; i++)
            {
                int sz = i < 4 ? 26 : 18;
                var t = CoastHudLayout.MakeText(card, "Fl" + i, glyphs[i % glyphs.Length], sz, TextAnchor.MiddleCenter,
                    corners[i], corners[i],
                    new Vector2(offs[i].x - sz, offs[i].y - sz), new Vector2(offs[i].x + sz, offs[i].y + sz));
                t.color = cols[i % cols.Length]; t.raycastTarget = false;
            }
            EventCardKit.Sparkle(card, new Vector2(0.15f, 0.92f), Vector2.zero, 14, new Color(1f, 1f, 1f, 0.85f));
            EventCardKit.Sparkle(card, new Vector2(0.88f, 0.18f), Vector2.zero, 12, new Color(1f, 1f, 1f, 0.8f));
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
