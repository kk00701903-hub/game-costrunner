using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 52차(사용자): 펫 상점(다마고치식 육성 화면용 오버레이) — **스토리 20주차부터** 열리고, 값은 러닝에서 모은 **코인 + 젤리**.
    ///   PetShop(가격·소유·장착 규칙)을 그대로 쓰고 그림만 새로 그린다. 잠겨 있으면 자물쇠 안내만.
    public static class PetShopUI
    {
        private static Canvas _canvas;
        private static RectTransform _root;
        private static Action _onClose;
        private static GameManager _gm;
        public static bool IsOpen => _canvas != null;

        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        private static readonly Color Cream = new Color(0.99f, 0.96f, 0.88f);

        /// 73차(사용자): 일반상점과 통합 — 이제 ShopUI(펫 탭)로 연다. 아래 옛 화면은 폴백으로만 남긴다.
        public static void Open(GameManager gm, Action onClose = null) { ShopUI.Open(gm, 1, onClose); }

        public static void OpenLegacy(GameManager gm, Action onClose = null)
        {
            Close();
            _gm = gm; _onClose = onClose;
            var save = gm != null ? gm.Save : null;
            if (save == null) return;
            _canvas = CoastUiCanvas.Create("PetShopCanvas", 462);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.05f, 0.04f, 0.10f, 0.72f));
            dim.raycastTarget = true;
            var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None; db.onClick.AddListener(Close);
            Build(save);
        }

        /// 66차(사용자 시안): 금테 크림 카드 + 「펫 상점」 금색 젤리 제목 그림(시안에서 오려냄 UI_PetShop_Title) + 「★ 포인트 N · Lv N ★」 +
        ///   파스텔 줄 4개(노랑·분홍·보라·초록: 흰 초상 틀(UI_Pet_<Kind>, 시안 초상) · 이름 · 설명 · 금 알약 「◆ 800c · Lv3 ◆」 · 파란 젤리 「구매」(못 사면 회색)) + 회색 「닫기」.
        private static void Build(SaveData save)
        {
            foreach (Transform c in _root) if (c.name == "Card") UnityEngine.Object.Destroy(c.gameObject);
            bool unlocked = PetShop.Unlocked(save);
            var kinds = PetShop.ForSale;
            float rowH = 138f, rowGap = 14f, top = 232f;
            float h = unlocked ? top + kinds.Length * (rowH + rowGap) + 76f : 420f;
            var gold = new Color(0.98f, 0.80f, 0.32f);
            var card = CoastUiArt.Panel(_root, "Card", gold, 34); card.raycastTarget = true;
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 24f); crt.sizeDelta = new Vector2(668f, h);
            var inner = CoastUiArt.Panel(crt, "Inner", new Color(0.996f, 0.96f, 0.85f), 30); inner.raycastTarget = false;
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one; inner.rectTransform.offsetMin = new Vector2(6f, 6f); inner.rectTransform.offsetMax = new Vector2(-6f, -6f);
            // 색종이 조각
            var rng = new System.Random(66);
            Color[] conf = { new Color(0.55f, 0.85f, 1f), new Color(1f, 0.55f, 0.75f), new Color(0.98f, 0.85f, 0.35f), new Color(0.75f, 0.60f, 0.98f), new Color(0.55f, 0.90f, 0.65f) };
            for (int i = 0; i < 16; i++)
            {
                var cf = CoastUiArt.Panel(crt, "Confetti", conf[i % conf.Length], 3); cf.raycastTarget = false;
                var cr = cf.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2((float)rng.NextDouble(), 1f - (float)rng.NextDouble() * 0.35f); cr.sizeDelta = new Vector2(10f + rng.Next(8), 5f + rng.Next(4)); cr.localRotation = Quaternion.Euler(0f, 0f, rng.Next(360));
            }
            var titleTex = ArtAssets.LoadTexture("UI_PetShop_Title");
            if (titleTex != null)
            {
                var ti = new GameObject("TitleArt", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ti.transform.SetParent(crt, false); ti.sprite = CoastUiArt.AsSprite(titleTex); ti.preserveAspect = true; ti.raycastTarget = false;
                var tr = ti.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f); tr.pivot = new Vector2(0.5f, 1f); tr.anchoredPosition = new Vector2(0f, -10f); tr.sizeDelta = new Vector2(520f, 170f);
            }
            else
            {
                var title = CoastHudLayout.MakeText(crt, "Title", Loc.T("펫 상점", "Pet Shop"), 56, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -30f));
                title.color = gold; title.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(title, new Color(0.62f, 0.34f, 0.04f), 3f);
            }
            var wallet = CoastHudLayout.MakeText(crt, "Wallet", Loc.T($"★ 포인트 {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★", $"★ Points {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★"), 24, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -226f), new Vector2(0f, -182f));
            wallet.color = new Color(0.62f, 0.36f, 0.04f); wallet.fontStyle = FontStyle.Bold;

            if (!unlocked)
            {
                var lockT = CoastHudLayout.MakeText(crt, "Lock", Loc.T($"펫은 스토리 {PetShop.UnlockWeek}주차부터 데려올 수 있어요\n(지금 {save.week}주차)\n\n러닝에서 모은 코인으로 사요(레벨 조건 있음).", $"Pets unlock on story week {PetShop.UnlockWeek}\n(now week {save.week})\n\nBuy with coins from runs (level required)."), 18, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 80f), new Vector2(-30f, -230f));
                lockT.color = Navy; lockT.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            else
            {
                Color[] rowCols = { new Color(1f, 0.95f, 0.76f), new Color(1f, 0.82f, 0.88f), new Color(0.86f, 0.80f, 0.98f), new Color(0.80f, 0.95f, 0.84f) };
                for (int i = 0; i < kinds.Length; i++)
                {
                    var k = kinds[i];
                    bool owned = PetShop.Owns(save, k), equipped = save.equippedPet == k;
                    var row = CoastUiArt.Panel(crt, "Pet_" + k, Color.white, 22); row.raycastTarget = false;
                    var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
                    rrt.anchoredPosition = new Vector2(0f, -top - i * (rowH + rowGap)); rrt.sizeDelta = new Vector2(612f, rowH);
                    var rowIn = CoastUiArt.Panel(row.transform, "Fill", rowCols[i % rowCols.Length], 20); rowIn.raycastTarget = false;
                    rowIn.rectTransform.anchorMin = Vector2.zero; rowIn.rectTransform.anchorMax = Vector2.one; rowIn.rectTransform.offsetMin = new Vector2(4f, 4f); rowIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
                    var frame = CoastUiArt.Panel(row.transform, "Frame", Color.white, 16); frame.raycastTarget = false;
                    var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f); frt.anchoredPosition = new Vector2(14f, 0f); frt.sizeDelta = new Vector2(112f, 112f);
                    var petTex = ArtAssets.LoadTexture("UI_Pet_" + k) ?? ArtAssets.LoadTexture("Obs_Pet_" + k);
                    if (petTex != null)
                    {
                        var pi = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                        pi.transform.SetParent(frame.transform, false); pi.sprite = CoastUiArt.AsSprite(petTex); pi.preserveAspect = true; pi.raycastTarget = false;
                        var prt = pi.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(3f, 3f); prt.offsetMax = new Vector2(-3f, -3f);
                    }
                    var name = CoastHudLayout.MakeText(row.transform, "Name", PetCompanion.Names[(int)k], 28, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(142f, -60f), new Vector2(-150f, -14f));
                    name.color = new Color(0.16f, 0.14f, 0.34f); name.fontStyle = FontStyle.Bold;
                    var blurb = CoastHudLayout.MakeText(row.transform, "Blurb", PetCompanion.Blurbs[(int)k], 15, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(142f, 8f), new Vector2(-160f, -64f));
                    blurb.color = new Color(0.22f, 0.20f, 0.36f); blurb.fontStyle = FontStyle.Bold; blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
                    blurb.resizeTextForBestFit = true; blurb.resizeTextMinSize = 10; blurb.resizeTextMaxSize = CoastHudLayout.Scaled(15);
                    string priceTxt = owned ? (equipped ? Loc.T("장착 중", "Equipped") : Loc.T("보유", "Owned")) : $"◆ {PetShop.Price[k]:N0}c · Lv{PetShop.LevelReq[k]} ◆";
                    var pp = CoastUiArt.GlossyPill(row.transform, "PricePill", new Color(1f, 0.82f, 0.30f), 14, 5); pp.raycastTarget = false;
                    var pprt = pp.rectTransform; pprt.anchorMin = pprt.anchorMax = new Vector2(1f, 1f); pprt.pivot = new Vector2(1f, 1f); pprt.anchoredPosition = new Vector2(-12f, -10f); pprt.sizeDelta = new Vector2(150f, 34f);
                    var price = CoastHudLayout.MakeText(pprt, "T", priceTxt, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 2f), Vector2.zero);
                    price.color = new Color(0.45f, 0.24f, 0f); price.fontStyle = FontStyle.Bold; price.resizeTextForBestFit = true; price.resizeTextMinSize = 9; price.resizeTextMaxSize = CoastHudLayout.Scaled(15);
                    string label = !owned ? Loc.T("구매", "Buy") : equipped ? Loc.T("해제", "Unequip") : Loc.T("장착", "Equip");
                    Color col = !owned ? (PetShop.CanAfford(save, k) ? new Color(0.30f, 0.62f, 1f) : new Color(0.55f, 0.57f, 0.64f)) : equipped ? new Color(0.55f, 0.57f, 0.64f) : new Color(0.30f, 0.62f, 1f);
                    var btn = CoastUiArt.GlossyPill(row.transform, "Act", col, 22, 8);
                    var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(1f, 0f); brt.anchoredPosition = new Vector2(-12f, 10f); brt.sizeDelta = new Vector2(150f, 56f); btn.raycastTarget = true;
                    var bl = CoastHudLayout.MakeText(brt, "T", label, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero); bl.color = Color.white; bl.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(bl, new Color(0f, 0f, 0.2f, 0.35f), 1.5f);
                    var pk = k;
                    var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => Act(pk));
                }
            }
            var close = CoastUiArt.GlossyPill(crt, "Close", new Color(0.60f, 0.62f, 0.70f), 20, 7);
            var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f); clrt.anchoredPosition = new Vector2(0f, 14f); clrt.sizeDelta = new Vector2(300f, 54f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
        }

        private static void Act(PetKind k)
        {
            var save = _gm != null ? _gm.Save : null; if (save == null) return;
            CoastPrefs.Vibrate();
            if (!PetShop.Owns(save, k))
            {
                if (PetShop.TryBuy(save, k)) { CoastToast.Show(Loc.T($"{PetCompanion.Names[(int)k]}를 데려왔어!", $"{PetCompanion.Names[(int)k]} joined!")); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.6f); _gm.Persist(); }
                else { CoastToast.Show(Loc.T($"코인이 모자라거나 레벨(Lv{PetShop.LevelReq[k]}) 이 부족해 — 러닝에서 더 모아 오자", $"Not enough coins or level (Lv{PetShop.LevelReq[k]}) — run more!")); CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f); }
            }
            else if (save.equippedPet == k) { save.equippedPet = PetKind.None; _gm.Persist(); }
            else { PetShop.Equip(save, k); _gm.Persist(); }
            Build(save);
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null;
            var cb = _onClose; _onClose = null; cb?.Invoke();
        }
    }
}
