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

    /// ① 「일주일이 지났다」 — 행동 3번이 끝나 한 턴이 넘어갈 때. 이번 주 생활 결산을 보여 주고 터치하면 다음 턴.
    public static class WeekPassUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void Show(int fromWeek, int toWeek, SeasonKind season, Survival.WeekReport rep, string nextNote, Action onDone)
        {
            Close();
            Action finish = () => { Close(); onDone?.Invoke(); };
            _canvas = LifeUiKit.Dim("WeekPassCanvas", 466, out var root, finish);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            var card = CoastUiArt.CutePill(root, "Card", LifeUiKit.Cream, 30, 5);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            int n = rep != null ? rep.lines.Count : 0;
            float h = 300f + n * 34f + (string.IsNullOrEmpty(nextNote) ? 0f : 60f);
            crt.anchoredPosition = new Vector2(0f, 30f); crt.sizeDelta = new Vector2(620f, h); card.raycastTarget = true;
            var cb = card.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(() => finish());

            var moon = CoastHudLayout.MakeText(crt, "Moon", "☾  ·  ☀", 26, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(0f, -16f));
            moon.color = new Color(0.55f, 0.45f, 0.30f);
            var title = CoastHudLayout.MakeText(crt, "Title", Loc.T("일주일이 지났다", "A week has passed"), 34, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -56f));
            title.color = LifeUiKit.Navy; title.fontStyle = FontStyle.Bold;
            string subTxt = fromWeek == toWeek
                ? Loc.T($"{toWeek}주차 · 챕터의 마지막 주가 끝났다  ·  {Timeline.SeasonName(season)}", $"Week {toWeek} · last week of the chapter  ·  {Timeline.SeasonName(season)}")
                : Loc.T($"{fromWeek}주차  →  {toWeek}주차  ·  {Timeline.SeasonName(season)}", $"Week {fromWeek}  →  Week {toWeek}  ·  {Timeline.SeasonName(season)}");
            var sub = CoastHudLayout.MakeText(crt, "Sub", subTxt, 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -146f), new Vector2(0f, -110f));
            sub.color = new Color(0.86f, 0.32f, 0.45f); sub.fontStyle = FontStyle.Bold;
            float y = -172f;
            if (rep != null)
                foreach (var line in rep.lines)
                {
                    bool bad = line.Contains("굶") || line.Contains("없") || line.Contains("못") || line.Contains("!!") || line.Contains("낡") || line.StartsWith("No ");
                    var t = CoastHudLayout.MakeText(crt, "L", (bad ? "▪ " : "· ") + line, 16, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, y - 30f), new Vector2(-30f, y));
                    t.color = bad ? new Color(0.75f, 0.20f, 0.25f) : new Color(0.30f, 0.26f, 0.38f); t.horizontalOverflow = HorizontalWrapMode.Wrap;
                    y -= 34f;
                }
            if (!string.IsNullOrEmpty(nextNote))
            {
                var pill = CoastUiArt.CutePill(crt, "Next", new Color(1f, 0.92f, 0.78f), 14, 2); pill.raycastTarget = false;
                var prt = pill.rectTransform; prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(1f, 1f); prt.pivot = new Vector2(0.5f, 1f); prt.anchoredPosition = new Vector2(0f, y - 6f); prt.sizeDelta = new Vector2(-60f, 50f);
                var nt = CoastHudLayout.MakeText(prt, "T", nextNote, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
                nt.color = new Color(0.48f, 0.29f, 0f); nt.fontStyle = FontStyle.Bold; nt.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            var tap = CoastHudLayout.MakeText(crt, "Tap", Loc.T("터치해서 다음 턴으로  ▶", "Tap for the next turn  ▶"), 17, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(0f, 62f));
            tap.color = new Color(0.45f, 0.38f, 0.34f); tap.fontStyle = FontStyle.Bold;
            _canvas.gameObject.AddComponent<TapPulse>().t = tap;
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.35f);
        }

        private class TapPulse : MonoBehaviour { public Text t; private void Update() { if (t != null) { var c = t.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)); t.color = c; } } }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }

    /// ② 장보기 — 돈으로 쌀(주 단위)·반찬·옷(12주). 씨앗은 마이룸에서.
    public static class GroceryUI
    {
        private static Canvas _canvas; private static RectTransform _root; private static GameManager _gm; private static Action _onClose;
        public static bool IsOpen => _canvas != null;

        public static void Open(GameManager gm, Action onClose = null)
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

    /// ③ 쓰러짐 — 다마고치의 죽음. 처음부터(새 회차) / 병원에서 깨어나기(돈 절반, 주차 그대로).
    public static class GameOverUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;

        public static void Show(GameManager gm, Sprite girl, Action onRevive)
        {
            Close();
            var s = gm != null ? gm.Save : null; if (s == null) return;
            _canvas = LifeUiKit.Dim("GameOverCanvas", 480, out var root);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            var pad = CoastUiCanvas.HudPad;
            var black = CoastHudLayout.MakeImage(root, "Black", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.02f, 0.02f, 0.04f, 0.92f));
            black.raycastTarget = true;
            if (girl != null)
            {
                var im = new GameObject("Girl", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(root, false); im.sprite = girl; im.preserveAspect = true; im.color = new Color(0.55f, 0.55f, 0.62f); im.raycastTarget = false;
                var rt = im.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(0f, 250f); rt.sizeDelta = new Vector2(360f, 440f);
                rt.localRotation = Quaternion.Euler(0f, 0f, 78f);
            }
            var title = CoastHudLayout.MakeText(root, "Title", Loc.T("하늘이 쓰러졌다…", "Haneul collapsed…"), 40, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -40f), new Vector2(0f, 40f));
            title.color = new Color(0.95f, 0.85f, 0.80f); title.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(title, new Color(0.4f, 0f, 0.1f, 0.6f), 2f);
            string why = s.starveWeeks >= 4 ? Loc.T("몇 주째 쌀이 없었다.", "No rice for weeks.") : s.sleepDebt >= 2 ? Loc.T("잠을 계속 안 잤다.", "Kept skipping sleep.") : Loc.T("배고프고 지친 채로 컨디션이 바닥났다.", "Hungry, exhausted, condition hit zero.");
            var body = CoastHudLayout.MakeText(root, "Body", why + "\n" + Loc.T($"{s.week}주차 · Lv {Mathf.Max(1, s.level)} · 돈 {LevelSystem.FormatK(s.stats.money)}G", $"Week {s.week} · Lv {Mathf.Max(1, s.level)} · {LevelSystem.FormatK(s.stats.money)}G"), 18, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(30f, -120f), new Vector2(-30f, -50f));
            body.color = new Color(0.75f, 0.72f, 0.78f); body.horizontalOverflow = HorizontalWrapMode.Wrap;
            LifeUiKit.Btn(root, "Hospital", Loc.T("병원에서 깨어나기 (돈 절반 · 이 주에서 계속)", "Wake up in hospital (half money · same week)"), new Color(0.35f, 0.65f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(560f, 64f), () =>
            {
                Survival.Revive(s); gm.Persist(); Close(); onRevive?.Invoke();
                CoastToast.Show(Loc.T("병원에서 깨어났다. 쌀부터 사자.", "Woke up in hospital. Buy rice first."));
            }, 18);
            LifeUiKit.Btn(root, "Restart", Loc.T("처음부터 다시 (컬렉션·기록은 유지)", "Start over (collection kept)"), new Color(0.75f, 0.30f, 0.35f), new Vector2(0.5f, 0.5f), new Vector2(0f, -284f), new Vector2(560f, 64f), () =>
            {
                Close(); gm.RestartAfterDeath();
            }, 18);
            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.8f);
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
