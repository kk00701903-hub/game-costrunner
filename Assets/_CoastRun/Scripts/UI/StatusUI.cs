using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 53차(사용자): 육성 **상태창** — 레벨·칭호·경험치 바, 스탯 6개(체력·순발력·매력·감성·평판·스트레스), 돈/코인(k 표기),
    ///   레벨 보너스(코인 +%), 다음 롱컷 해금 힌트, 경험치 얻는 법. TamaRaisingUI 의 [상태창] 버튼에서.
    public static class StatusUI
    {
        private static Canvas _canvas;
        private static Action _onClose;
        public static bool IsOpen => _canvas != null;
        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        private static readonly Color Cream = new Color(0.99f, 0.96f, 0.88f);

        public static void Open(GameManager gm, Action onClose = null)
        {
            Close();
            _onClose = onClose;
            var save = gm != null ? gm.Save : null;
            if (save == null) return;
            _canvas = CoastUiCanvas.Create("StatusCanvas", 462);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.05f, 0.04f, 0.10f, 0.72f));
            dim.raycastTarget = true;
            var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None; db.onClick.AddListener(Close);

            var card = CoastUiArt.CutePill(root, "Card", Cream, 28, 5);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 10f); crt.sizeDelta = new Vector2(640f, 850f); card.raycastTarget = true;   // 56차-2: 생활 구역만큼 키움

            int lv = Mathf.Max(1, save.level);
            // 머리: 얼굴 + 레벨 배지 + 칭호
            var face = ArtAssets.LoadTexture("UI_Face_Girl");
            var fimg = CoastHudLayout.MakeImage(crt, "Face", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -136f), new Vector2(136f, -26f), Color.white);
            if (face != null) { fimg.sprite = CoastUiArt.AsSprite(face); fimg.preserveAspect = true; } else fimg.color = new Color(1f, 0.85f, 0.7f);
            fimg.raycastTarget = false;
            var badge = CoastUiArt.GlossyPill(crt, "Lv", new Color(0.55f, 0.40f, 0.95f), 16, 5); badge.raycastTarget = false;
            var brt = badge.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 1f); brt.anchoredPosition = new Vector2(150f, -30f); brt.sizeDelta = new Vector2(130f, 48f);
            var bl = CoastHudLayout.MakeText(brt, "T", $"Lv {lv}", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); bl.color = Color.white; bl.fontStyle = FontStyle.Bold;
            var nm = CoastHudLayout.MakeText(crt, "Name", Loc.T("하늘", "Haneul") + " · " + LevelSystem.Title(lv), 18, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(292f, -70f), new Vector2(-20f, -30f));
            nm.color = Navy; nm.fontStyle = FontStyle.Bold; nm.horizontalOverflow = HorizontalWrapMode.Overflow;
            // 경험치 바
            int need = LevelSystem.Need(lv);
            var eb = CoastUiArt.CutePill(crt, "ExpBg", new Color(0.85f, 0.82f, 0.90f), 12, 2); eb.raycastTarget = false;
            var ert = eb.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0f, 1f); ert.pivot = new Vector2(0f, 1f); ert.anchoredPosition = new Vector2(150f, -88f); ert.sizeDelta = new Vector2(464f, 30f);
            var ef = CoastUiArt.Panel(ert, "Fill", new Color(0.60f, 0.45f, 1f), 10); ef.raycastTarget = false;
            ef.rectTransform.anchorMin = new Vector2(0f, 0f); ef.rectTransform.anchorMax = new Vector2(lv >= LevelSystem.MaxLevel ? 1f : Mathf.Clamp01(save.exp / (float)need), 1f); ef.rectTransform.offsetMin = new Vector2(3f, 3f); ef.rectTransform.offsetMax = new Vector2(-3f, -3f);
            var et = CoastHudLayout.MakeText(ert, "T", lv >= LevelSystem.MaxLevel ? "MAX" : $"EXP {save.exp} / {need}", 13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            et.color = Color.white; et.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(et, new Color(0f, 0f, 0f, 0.5f), 1.2f);
            var hint = CoastHudLayout.MakeText(crt, "Hint", LevelSystem.NextUnlockHint(), 13, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(150f, -146f), new Vector2(-20f, -122f));
            hint.color = new Color(0.55f, 0.30f, 0.15f); hint.fontStyle = FontStyle.Bold;

            // 스탯 6개
            var st = save.stats;
            (string ko, string en, int v, int max, Color c)[] rows =
            {
                ("체력", "Stamina", st.stamina, PlayerStats.StatMax, new Color(0.95f, 0.40f, 0.42f)),
                ("순발력", "Agility", st.agility, PlayerStats.StatMax, new Color(0.35f, 0.75f, 0.95f)),
                ("매력", "Charm", st.charm, PlayerStats.StatMax, new Color(0.98f, 0.55f, 0.80f)),
                ("감성", "Sense", st.sense, PlayerStats.StatMax, new Color(0.60f, 0.45f, 1f)),
                ("평판", "Trust", st.trust, 100, new Color(0.95f, 0.75f, 0.30f)),
                ("스트레스", "Stress", st.stress, 100, new Color(0.55f, 0.55f, 0.62f)),
            };
            float y0 = -176f;
            var sh = CoastHudLayout.MakeText(crt, "StatsH", Loc.T("스탯", "Stats"), 16, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, y0 - 4f), new Vector2(-20f, y0 + 20f));
            sh.color = Navy; sh.fontStyle = FontStyle.Bold;
            for (int i = 0; i < rows.Length; i++)
            {
                float y = y0 - 34f - i * 46f;
                var l = CoastHudLayout.MakeText(crt, "L" + i, Loc.T(rows[i].ko, rows[i].en), 15, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, y - 30f), new Vector2(120f, y));
                l.color = Navy; l.fontStyle = FontStyle.Bold;
                var bg = CoastUiArt.CutePill(crt, "B" + i, new Color(0.88f, 0.86f, 0.90f), 10, 2); bg.raycastTarget = false;
                var bgr = bg.rectTransform; bgr.anchorMin = bgr.anchorMax = new Vector2(0f, 1f); bgr.pivot = new Vector2(0f, 1f); bgr.anchoredPosition = new Vector2(126f, y - 2f); bgr.sizeDelta = new Vector2(400f, 26f);
                var fl = CoastUiArt.Panel(bgr, "F", rows[i].c, 8); fl.raycastTarget = false;
                fl.rectTransform.anchorMin = Vector2.zero; fl.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(rows[i].v / (float)rows[i].max), 1f); fl.rectTransform.offsetMin = new Vector2(3f, 3f); fl.rectTransform.offsetMax = new Vector2(-3f, -3f);
                var v = CoastHudLayout.MakeText(crt, "V" + i, $"{rows[i].v}", 15, TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(530f, y - 30f), new Vector2(-24f, y));
                v.color = Navy; v.fontStyle = FontStyle.Bold;
            }
            // 돈·코인·레벨 보너스
            float y2 = y0 - 34f - rows.Length * 46f - 8f;
            string money = Loc.T($"돈 {LevelSystem.FormatK(st.money)}G  ·  코인 {LevelSystem.FormatK(CoinWallet.TotalStatic)}", $"Money {LevelSystem.FormatK(st.money)}G  ·  Coins {LevelSystem.FormatK(CoinWallet.TotalStatic)}");
            var mt = CoastHudLayout.MakeText(crt, "Money", money, 16, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, y2 - 30f), new Vector2(-20f, y2));
            mt.color = new Color(0.45f, 0.30f, 0.10f); mt.fontStyle = FontStyle.Bold;
            int coinPct = Mathf.RoundToInt((LevelSystem.CoinMul(save) - 1f) * 100f);
            var perk = CoastHudLayout.MakeText(crt, "Perk", Loc.T($"레벨 보너스: 러닝 코인 +{coinPct}%  ·  레벨업마다 체력 +2 순발력 +1 매력 +1 감성 +1", $"Level bonus: run coins +{coinPct}%  ·  each level: Stamina +2 Agility +1 Charm +1 Sense +1"), 12, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, y2 - 56f), new Vector2(-20f, y2 - 32f));
            perk.color = new Color(0.35f, 0.32f, 0.45f); perk.horizontalOverflow = HorizontalWrapMode.Wrap;
            var how = CoastHudLayout.MakeText(crt, "How", Loc.T($"경험치: 젤리 {LevelSystem.ExpJelly} · 큰 젤리 {LevelSystem.ExpBigJelly} · 행동 {LevelSystem.ExpAction}(대성공 {LevelSystem.ExpActionGreat}) · 이야기 {LevelSystem.ExpChapterRead} · 미니게임 {LevelSystem.ExpMinigame} · K-POP 완주 {LevelSystem.ExpKpopFinish} · 보스 {LevelSystem.ExpBoss} · 스토리 러닝 {LevelSystem.ExpStoryRun}",
                $"EXP: jelly {LevelSystem.ExpJelly} · big jelly {LevelSystem.ExpBigJelly} · action {LevelSystem.ExpAction}(great {LevelSystem.ExpActionGreat}) · story {LevelSystem.ExpChapterRead} · mini-game {LevelSystem.ExpMinigame} · K-POP finish {LevelSystem.ExpKpopFinish} · boss {LevelSystem.ExpBoss} · story run {LevelSystem.ExpStoryRun}"), 12, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y2 - 112f), new Vector2(-24f, y2 - 60f));
            how.color = new Color(0.40f, 0.36f, 0.48f); how.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 56차-2(UX): 비어 있던 아래쪽에 「생활」 — 배부름·컨디션 막대 + 쌀·반찬·옷 재고
            float y3 = y2 - 124f;
            var lh = CoastHudLayout.MakeText(crt, "LifeH", Loc.T("생활", "Life"), 16, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, y3 - 24f), new Vector2(-20f, y3));
            lh.color = Navy; lh.fontStyle = FontStyle.Bold;
            (string ko, string en, int v, Color c)[] life = { ("배부름", "Fullness", save.hunger, new Color(1f, 0.70f, 0.30f)), ("컨디션", "Condition", save.condition, new Color(0.40f, 0.80f, 0.55f)) };
            for (int i = 0; i < life.Length; i++)
            {
                float y = y3 - 30f - i * 36f;
                var l = CoastHudLayout.MakeText(crt, "LL" + i, Loc.T(life[i].ko, life[i].en), 14, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, y - 26f), new Vector2(120f, y));
                l.color = Navy; l.fontStyle = FontStyle.Bold;
                var bg = CoastUiArt.CutePill(crt, "LB" + i, new Color(0.88f, 0.86f, 0.90f), 10, 2); bg.raycastTarget = false;
                var bgr = bg.rectTransform; bgr.anchorMin = bgr.anchorMax = new Vector2(0f, 1f); bgr.pivot = new Vector2(0f, 1f); bgr.anchoredPosition = new Vector2(126f, y - 2f); bgr.sizeDelta = new Vector2(400f, 22f);
                var fl = CoastUiArt.Panel(bgr, "F", life[i].v < 30 ? new Color(0.95f, 0.35f, 0.35f) : life[i].c, 8); fl.raycastTarget = false;
                fl.rectTransform.anchorMin = Vector2.zero; fl.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(life[i].v / 100f), 1f); fl.rectTransform.offsetMin = new Vector2(3f, 3f); fl.rectTransform.offsetMax = new Vector2(-3f, -3f);
                var v = CoastHudLayout.MakeText(crt, "LV" + i, $"{life[i].v}", 14, TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(530f, y - 26f), new Vector2(-24f, y));
                v.color = Navy; v.fontStyle = FontStyle.Bold;
            }
            string stock = Loc.T($"쌀 {save.rice}주분  ·  반찬 {save.sideDish}주분  ·  옷 {(save.clothesWeeks <= 0 ? "낡음!" : save.clothesWeeks + "주 남음")}  ·  {(save.restedThisWeek ? "이번 주 잠 잤음" : "이번 주 아직 안 잠")}",
                $"Rice {save.rice}w  ·  Side {save.sideDish}w  ·  Clothes {(save.clothesWeeks <= 0 ? "worn!" : save.clothesWeeks + "w")}  ·  {(save.restedThisWeek ? "rested this week" : "not rested yet")}");
            var stT = CoastHudLayout.MakeText(crt, "Stock", stock, 13, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, y3 - 128f), new Vector2(-20f, y3 - 100f));
            stT.color = new Color(0.45f, 0.30f, 0.10f); stT.fontStyle = FontStyle.Bold; stT.horizontalOverflow = HorizontalWrapMode.Wrap;
            stT.resizeTextForBestFit = true; stT.resizeTextMinSize = 10; stT.resizeTextMaxSize = CoastHudLayout.Scaled(13);

            var close = CoastUiArt.CutePill(crt, "Close", new Color(0.62f, 0.62f, 0.70f), 18, 3);
            var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f); clrt.anchoredPosition = new Vector2(0f, 16f); clrt.sizeDelta = new Vector2(220f, 50f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
            var cb = _onClose; _onClose = null; cb?.Invoke();
        }
    }
}
