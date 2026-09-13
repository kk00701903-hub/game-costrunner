using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 73차(사용자 시안): **일반상점 + 펫상점 통합** 팝업 — 육성 화면 장바구니 버튼 하나로 연다.
    ///   금테 크림 카드 · 위에 탭 알약 두 개(「일반상점」 / 「펫상점」, 선택 = 노랑 + 반짝, 비선택 = 베이지) · 「★ 포인트 N · Lv N ★」 ·
    ///   줄 4개(흰 초상 틀 · 이름 · 설명 · 금 알약 「◆ 가격 · Lv ◆」 · 파란 「구매」) · 회색 「닫기」.
    ///   일반상점 = 생활용품(쌀 1주·쌀 4주·반찬·새 옷, 돈 G) — 옛 GroceryUI 의 규칙(Survival.Buy*) 그대로.
    ///   펫상점 = 옛 PetShopUI 의 규칙(PetShop.TryBuy/Equip, 코인+젤리, 20주차 해금) 그대로.
    public static class ShopUI
    {
        private static Canvas _canvas;
        private static RectTransform _root;
        private static Action _onClose;
        private static GameManager _gm;
        private static int _tab;   // 0 일반 / 1 펫
        public static bool IsOpen => _canvas != null;

        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.34f);
        private static readonly Color Gold = new Color(0.98f, 0.80f, 0.32f);

        public static void Open(GameManager gm, int tab = 0, Action onClose = null)
        {
            Close();
            _gm = gm; _onClose = onClose; _tab = Mathf.Clamp(tab, 0, 1);
            var save = gm != null ? gm.Save : null;
            if (save == null) return;
            _canvas = CoastUiCanvas.Create("ShopCanvas", 462);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.05f, 0.04f, 0.10f, 0.72f));
            dim.raycastTarget = true;
            var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None; db.onClick.AddListener(Close);
            Build();
        }

        private static void Build()
        {
            var save = _gm != null ? _gm.Save : null; if (save == null || _root == null) return;
            foreach (Transform c in _root) if (c.name == "Card") UnityEngine.Object.Destroy(c.gameObject);
            const float rowH = 138f, rowGap = 14f, top = 236f;
            int rows = 4;
            float h = top + rows * (rowH + rowGap) + 76f;
            var card = CoastUiArt.Panel(_root, "Card", Gold, 34); card.raycastTarget = true;
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 24f); crt.sizeDelta = new Vector2(668f, h);
            var inner = CoastUiArt.Panel(crt, "Inner", new Color(0.996f, 0.96f, 0.85f), 30); inner.raycastTarget = false;
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one; inner.rectTransform.offsetMin = new Vector2(6f, 6f); inner.rectTransform.offsetMax = new Vector2(-6f, -6f);
            var rng = new System.Random(73);
            Color[] conf = { new Color(0.55f, 0.85f, 1f), new Color(1f, 0.55f, 0.75f), new Color(0.98f, 0.85f, 0.35f), new Color(0.75f, 0.60f, 0.98f), new Color(0.55f, 0.90f, 0.65f) };
            for (int i = 0; i < 14; i++)
            {
                var cf = CoastUiArt.Panel(crt, "Confetti", conf[i % conf.Length], 3); cf.raycastTarget = false;
                var cr = cf.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2((float)rng.NextDouble(), 1f - (float)rng.NextDouble() * 0.30f); cr.sizeDelta = new Vector2(10f + rng.Next(8), 5f + rng.Next(4)); cr.localRotation = Quaternion.Euler(0f, 0f, rng.Next(360));
            }

            // 탭 두 개
            Tab(crt, 0, Loc.T("일반상점", "Shop"), -152f);
            Tab(crt, 1, Loc.T("펫상점", "Pet Shop"), 152f);

            string walletTxt = _tab == 0
                ? Loc.T($"★ 돈 {LevelSystem.FormatK(save.stats.money)}G  ·  Lv {Mathf.Max(1, save.level)} ★", $"★ Money {LevelSystem.FormatK(save.stats.money)}G  ·  Lv {Mathf.Max(1, save.level)} ★")
                : Loc.T($"★ 포인트 {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★", $"★ Points {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★");
            var wallet = CoastHudLayout.MakeText(crt, "Wallet", walletTxt, 24, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -226f), new Vector2(0f, -182f));
            wallet.color = new Color(0.62f, 0.36f, 0.04f); wallet.fontStyle = FontStyle.Bold;

            Color[] rowCols = { new Color(1f, 0.95f, 0.76f), new Color(1f, 0.82f, 0.88f), new Color(0.86f, 0.80f, 0.98f), new Color(0.80f, 0.95f, 0.84f) };
            if (_tab == 0)
            {
                var s = save;
                GoodsRow(crt, 0, rowCols[0], top, rowH, rowGap, "UI_Goods_Rice", Loc.T("쌀 1주분", "Rice · 1 week"), Loc.T("주마다 1주분을 먹는다. 없으면 굶는다.", "Eaten weekly. None = starving."), $"{Survival.RicePrice}G", Loc.T($"보유 {s.rice}주분", $"Have {s.rice}w"), true,
                    () => { if (Survival.BuyRice(s)) Bought(Loc.T("쌀을 샀어.", "Bought rice.")); else Short(); });
                GoodsRow(crt, 1, rowCols[1], top, rowH, rowGap, "UI_Goods_Rice", Loc.T("쌀 4주분", "Rice · 4 weeks"), Loc.T("한 달치 한 번에. 굶을 걱정 없이.", "A month at once."), $"{Survival.RicePrice * 4}G", Loc.T($"보유 {s.rice}주분", $"Have {s.rice}w"), true,
                    () => { if (Survival.BuyRice(s, 4)) Bought(Loc.T("쌀 4주분을 샀어.", "Bought 4 weeks of rice.")); else Short(); });
                GoodsRow(crt, 2, rowCols[2], top, rowH, rowGap, "UI_Goods_Side", Loc.T("반찬 1주분", "Side dish · 1 week"), Loc.T("배부름·컨디션 ↑. 텃밭 채소로도 얻는다.", "Fullness/condition ↑. Also from the garden."), $"{Survival.SidePrice}G", Loc.T($"보유 {s.sideDish}", $"Have {s.sideDish}"), true,
                    () => { if (Survival.BuySide(s)) Bought(Loc.T("반찬을 샀어.", "Bought side dish.")); else Short(); });
                GoodsRow(crt, 3, rowCols[3], top, rowH, rowGap, "UI_Goods_Shirt", Loc.T("새 옷 (12주)", "New clothes (12 wk)"), Loc.T("3개월이면 낡는다. 낡으면 컨디션·매력 ↓", "Wears out in 3 months. Worn = condition/charm ↓"), $"{Survival.ClothesPrice}G", s.clothesWeeks <= 0 ? Loc.T("낡음!", "Worn!") : Loc.T($"{s.clothesWeeks}주 남음", $"{s.clothesWeeks}w left"), true,
                    () => { if (Survival.BuyClothes(s)) Bought(Loc.T("새 옷! 12주 동안 입는다.", "New clothes! Good for 12 weeks.")); else Short(); });
            }
            else
            {
                bool unlocked = PetShop.Unlocked(save);
                var kinds = PetShop.ForSale;
                for (int i = 0; i < kinds.Length && i < rows; i++)
                {
                    var k = kinds[i];
                    bool owned = PetShop.Owns(save, k), equipped = save.equippedPet == k;
                    string priceTxt = owned ? (equipped ? Loc.T("장착 중", "Equipped") : Loc.T("보유", "Owned")) : $"{PetShop.Price[k]:N0}c · Lv{PetShop.LevelReq[k]}";
                    string label = !unlocked ? Loc.T($"{PetShop.UnlockWeek}주차", $"Wk {PetShop.UnlockWeek}") : !owned ? Loc.T("구매", "Buy") : equipped ? Loc.T("해제", "Unequip") : Loc.T("장착", "Equip");
                    bool can = unlocked && (!owned ? PetShop.CanAfford(save, k) : !equipped);
                    var pk = k;
                    GoodsRow(crt, i, rowCols[i % rowCols.Length], top, rowH, rowGap, "UI_Pet_" + k, PetCompanion.Names[(int)k], PetCompanion.Blurbs[(int)k], priceTxt, null, can, () => ActPet(pk), label, "Obs_Pet_" + k);
                }
                if (!unlocked)
                {
                    var lockT = CoastHudLayout.MakeText(crt, "Lock", Loc.T($"펫은 스토리 {PetShop.UnlockWeek}주차부터 (지금 {save.week}주차)", $"Pets unlock on week {PetShop.UnlockWeek} (now {save.week})"), 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 74f), new Vector2(0f, 100f));
                    lockT.color = new Color(0.62f, 0.36f, 0.04f); lockT.fontStyle = FontStyle.Bold;
                }
            }

            var close = CoastUiArt.GlossyPill(crt, "Close", new Color(0.60f, 0.62f, 0.70f), 20, 7);
            var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f); clrt.anchoredPosition = new Vector2(0f, 14f); clrt.sizeDelta = new Vector2(300f, 54f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
        }

        private static void Tab(RectTransform crt, int idx, string label, float x)
        {
            bool on = _tab == idx;
            var pill = CoastUiArt.GlossyPill(crt, "Tab" + idx, on ? new Color(1f, 0.86f, 0.30f) : new Color(0.86f, 0.80f, 0.68f), 30, 8);
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -56f); rt.sizeDelta = new Vector2(288f, 84f); pill.raycastTarget = true;
            var ic = CoastUiArt.Art("Icon_Cart");
            if (ic != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(rt, false); im.sprite = ic; im.preserveAspect = true; im.raycastTarget = false; im.color = on ? new Color(0.45f, 0.26f, 0.04f) : new Color(0.55f, 0.48f, 0.36f);
                var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.pivot = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(22f, 4f); irt.sizeDelta = new Vector2(38f, 38f);
            }
            var t = CoastHudLayout.MakeText(rt, "T", label, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(56f, 4f), new Vector2(-10f, 0f));
            t.color = on ? new Color(0.45f, 0.26f, 0.04f) : new Color(0.50f, 0.44f, 0.34f); t.fontStyle = FontStyle.Bold;
            if (on) { EventCardKit.Sparkle(rt, new Vector2(0f, 1f), new Vector2(18f, -14f), 16, Color.white); EventCardKit.Sparkle(rt, new Vector2(1f, 0f), new Vector2(-16f, 14f), 12, Color.white); }
            int pick = idx;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { if (_tab == pick) return; CoastPrefs.Vibrate(); _tab = pick; Build(); });
        }

        /// 줄 하나: 흰 초상 틀(그림) · 이름 · 설명 · 금 알약(가격/보유) · 파란 버튼.
        private static void GoodsRow(RectTransform crt, int i, Color col, float top, float rowH, float rowGap, string art, string name, string blurb, string priceTxt, string haveTxt, bool can, Action act, string btnLabel = null, string artFallback = null)
        {
            var row = CoastUiArt.Panel(crt, "Row" + i, Color.white, 22); row.raycastTarget = false;
            var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -top - i * (rowH + rowGap)); rrt.sizeDelta = new Vector2(612f, rowH);
            var rowIn = CoastUiArt.Panel(row.transform, "Fill", col, 20); rowIn.raycastTarget = false;
            rowIn.rectTransform.anchorMin = Vector2.zero; rowIn.rectTransform.anchorMax = Vector2.one; rowIn.rectTransform.offsetMin = new Vector2(4f, 4f); rowIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
            var frame = CoastUiArt.Panel(row.transform, "Frame", Color.white, 16); frame.raycastTarget = false;
            var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f); frt.anchoredPosition = new Vector2(14f, 0f); frt.sizeDelta = new Vector2(112f, 112f);
            var tex = ArtAssets.LoadTexture(art) ?? (artFallback != null ? ArtAssets.LoadTexture(artFallback) : null);
            if (tex != null)
            {
                var pi = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                pi.transform.SetParent(frame.transform, false); pi.sprite = CoastUiArt.AsSprite(tex); pi.preserveAspect = true; pi.raycastTarget = false;
                var prt = pi.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(4f, 4f); prt.offsetMax = new Vector2(-4f, -4f);
            }
            var nm = CoastHudLayout.MakeText(row.transform, "Name", name, 28, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(142f, -60f), new Vector2(-160f, -14f));
            nm.color = Navy; nm.fontStyle = FontStyle.Bold; nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 14; nm.resizeTextMaxSize = CoastHudLayout.Scaled(28);
            var bl = CoastHudLayout.MakeText(row.transform, "Blurb", blurb, 15, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(142f, 8f), new Vector2(-166f, -64f));
            bl.color = new Color(0.22f, 0.20f, 0.36f); bl.fontStyle = FontStyle.Bold; bl.horizontalOverflow = HorizontalWrapMode.Wrap;
            bl.resizeTextForBestFit = true; bl.resizeTextMinSize = 10; bl.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var pp = CoastUiArt.GlossyPill(row.transform, "PricePill", new Color(1f, 0.82f, 0.30f), 14, 5); pp.raycastTarget = false;
            var pprt = pp.rectTransform; pprt.anchorMin = pprt.anchorMax = new Vector2(1f, 1f); pprt.pivot = new Vector2(1f, 1f); pprt.anchoredPosition = new Vector2(-12f, -10f); pprt.sizeDelta = new Vector2(haveTxt != null ? 190f : 150f, 34f);
            var price = CoastHudLayout.MakeText(pprt, "T", "◆ " + priceTxt + (haveTxt != null ? " · " + haveTxt : "") + " ◆", 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 2f), Vector2.zero);
            price.color = new Color(0.45f, 0.24f, 0f); price.fontStyle = FontStyle.Bold; price.resizeTextForBestFit = true; price.resizeTextMinSize = 8; price.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var btn = CoastUiArt.GlossyPill(row.transform, "Act", can ? new Color(0.30f, 0.62f, 1f) : new Color(0.55f, 0.57f, 0.64f), 22, 8);
            var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(1f, 0f); brt.anchoredPosition = new Vector2(-12f, 10f); brt.sizeDelta = new Vector2(150f, 56f); btn.raycastTarget = true;
            var lb = CoastHudLayout.MakeText(brt, "T", btnLabel ?? Loc.T("구매", "Buy"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero); lb.color = Color.white; lb.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lb, new Color(0f, 0f, 0.2f, 0.35f), 1.5f);
            var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); act?.Invoke(); });
        }

        private static void ActPet(PetKind k)
        {
            var save = _gm != null ? _gm.Save : null; if (save == null) return;
            if (!PetShop.Unlocked(save)) { CoastToast.Show(Loc.T($"펫은 스토리 {PetShop.UnlockWeek}주차부터 데려올 수 있어요", $"Pets unlock on week {PetShop.UnlockWeek}")); return; }
            if (!PetShop.Owns(save, k))
            {
                if (PetShop.TryBuy(save, k)) { CoastToast.Show(Loc.T($"{PetCompanion.Names[(int)k]}를 데려왔어!", $"{PetCompanion.Names[(int)k]} joined!")); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.6f); _gm.Persist(); }
                else { CoastToast.Show(Loc.T($"코인이 모자라거나 레벨(Lv{PetShop.LevelReq[k]}) 이 부족해 — 러닝에서 더 모아 오자", $"Not enough coins or level (Lv{PetShop.LevelReq[k]}) — run more!")); CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f); }
            }
            else if (save.equippedPet == k) { save.equippedPet = PetKind.None; _gm.Persist(); }
            else { PetShop.Equip(save, k); _gm.Persist(); }
            Build();
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
}
