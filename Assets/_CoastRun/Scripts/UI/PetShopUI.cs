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

        public static void Open(GameManager gm, Action onClose = null)
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

        private static void Build(SaveData save)
        {
            foreach (Transform c in _root) if (c.name == "Card") UnityEngine.Object.Destroy(c.gameObject);
            bool unlocked = PetShop.Unlocked(save);
            var kinds = PetShop.ForSale;
            float h = unlocked ? 190f + kinds.Length * 150f : 360f;
            var card = CoastUiArt.CutePill(_root, "Card", Cream, 28, 5);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 20f); crt.sizeDelta = new Vector2(640f, h); card.raycastTarget = true;

            var title = CoastHudLayout.MakeText(crt, "Title", Loc.T("펫 상점", "Pet Shop"), 28, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(0f, -16f));
            title.color = Navy; title.fontStyle = FontStyle.Bold;
            var wallet = CoastHudLayout.MakeText(crt, "Wallet", Loc.T($"코인 {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)}", $"Coins {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)}"), 16, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(0f, -66f));
            wallet.color = new Color(0.45f, 0.30f, 0.10f); wallet.fontStyle = FontStyle.Bold;

            if (!unlocked)
            {
                var lockT = CoastHudLayout.MakeText(crt, "Lock", Loc.T($"펫은 스토리 {PetShop.UnlockWeek}주차부터 데려올 수 있어요\n(지금 {save.week}주차)\n\n러닝에서 모은 코인으로 사요(레벨 조건 있음).", $"Pets unlock on story week {PetShop.UnlockWeek}\n(now week {save.week})\n\nBuy with coins from runs (level required)."), 18, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 80f), new Vector2(-30f, -110f));
                lockT.color = Navy; lockT.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            else
            {
                Color[] rowCols = { new Color(0.81f, 0.91f, 1f), new Color(1f, 0.84f, 0.90f), new Color(1f, 0.94f, 0.72f), new Color(0.84f, 0.96f, 0.85f) };
                for (int i = 0; i < kinds.Length; i++)
                {
                    var k = kinds[i];
                    bool owned = PetShop.Owns(save, k), equipped = save.equippedPet == k;
                    var row = CoastUiArt.CutePill(crt, "Pet_" + k, rowCols[i % rowCols.Length], 20, 3);
                    var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
                    rrt.anchoredPosition = new Vector2(0f, -112f - i * 150f); rrt.sizeDelta = new Vector2(590f, 138f); row.raycastTarget = false;
                    var frame = CoastUiArt.Panel(row.transform, "Frame", Color.white, 16); frame.raycastTarget = false;
                    var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f); frt.anchoredPosition = new Vector2(14f, 0f); frt.sizeDelta = new Vector2(108f, 108f);
                    var petTex = ArtAssets.LoadTexture("Obs_Pet_" + k);
                    if (petTex != null)
                    {
                        var pi = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                        pi.transform.SetParent(frame.transform, false); pi.sprite = CoastUiArt.AsSprite(petTex); pi.preserveAspect = true; pi.raycastTarget = false;
                        var prt = pi.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(6f, 6f); prt.offsetMax = new Vector2(-6f, -6f);
                        if (!owned) pi.color = new Color(0.8f, 0.8f, 0.83f, 1f);
                    }
                    var name = CoastHudLayout.MakeText(row.transform, "Name", PetCompanion.Names[(int)k], 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(136f, -48f), new Vector2(-150f, -10f));
                    name.color = Navy; name.fontStyle = FontStyle.Bold;
                    var blurb = CoastHudLayout.MakeText(row.transform, "Blurb", PetCompanion.Blurbs[(int)k], 13, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(136f, 10f), new Vector2(-150f, -52f));
                    blurb.color = new Color(0.30f, 0.28f, 0.42f); blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
                    string priceTxt = owned ? (equipped ? Loc.T("장착 중", "Equipped") : Loc.T("보유", "Owned")) : $"{PetShop.Price[k]:N0}c · Lv{PetShop.LevelReq[k]}";
                    var pp = CoastUiArt.CutePill(row.transform, "PricePill", new Color(1f, 0.96f, 0.84f), 12, 2); pp.raycastTarget = false;
                    var pprt = pp.rectTransform; pprt.anchorMin = pprt.anchorMax = new Vector2(1f, 1f); pprt.pivot = new Vector2(1f, 1f); pprt.anchoredPosition = new Vector2(-12f, -10f); pprt.sizeDelta = new Vector2(138f, 32f);
                    var price = CoastHudLayout.MakeText(pprt, "T", priceTxt, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 1f), Vector2.zero);
                    price.color = new Color(0.48f, 0.29f, 0f); price.fontStyle = FontStyle.Bold;
                    string label = !owned ? Loc.T("구매", "Buy") : equipped ? Loc.T("해제", "Unequip") : Loc.T("장착", "Equip");
                    Color col = !owned ? (PetShop.CanAfford(save, k) ? new Color(0.31f, 0.66f, 1f) : new Color(0.65f, 0.65f, 0.70f)) : equipped ? new Color(0.6f, 0.62f, 0.7f) : new Color(0.31f, 0.66f, 1f);
                    var btn = CoastUiArt.GlossyPill(row.transform, "Act", col, 18, 6);
                    var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(1f, 0f); brt.anchoredPosition = new Vector2(-12f, 10f); brt.sizeDelta = new Vector2(126f, 46f); btn.raycastTarget = true;
                    var bl = CoastHudLayout.MakeText(brt, "T", label, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); bl.color = Color.white; bl.fontStyle = FontStyle.Bold;
                    var pk = k;
                    var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => Act(pk));
                }
            }
            var close = CoastUiArt.CutePill(crt, "Close", new Color(0.62f, 0.62f, 0.70f), 18, 3);
            var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f); clrt.anchoredPosition = new Vector2(0f, 16f); clrt.sizeDelta = new Vector2(220f, 50f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
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
