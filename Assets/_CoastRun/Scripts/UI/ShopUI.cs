using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 일반상점 + 펫상점 통합.
    ///   일반 = 카테고리(재료·기본요리·고급요리·약·케어·옷) + 수량 선택 구매.
    ///   펫 = 기존 PetShop 규칙.
    public static class ShopUI
    {
        private static Canvas _canvas;
        private static RectTransform _root;
        private static Action _onClose;
        private static GameManager _gm;
        private static int _tab;   // 0 일반 / 1 펫
        private static LifeItemCat _cat = LifeItemCat.Ingredient;
        private static string _pickId;
        private static int _qty = 1;
        public static bool IsOpen => _canvas != null;

        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.34f);
        private static readonly Color Gold = new Color(0.98f, 0.80f, 0.32f);
        private static readonly LifeItemCat[] Cats =
        {
            LifeItemCat.Ingredient, LifeItemCat.BasicDish, LifeItemCat.PremiumDish,
            LifeItemCat.Medicine, LifeItemCat.Care, LifeItemCat.Clothes
        };

        public static void Open(GameManager gm, int tab = 0, Action onClose = null)
        {
            Close();
            _gm = gm; _onClose = onClose; _tab = Mathf.Clamp(tab, 0, 1);
            _cat = LifeItemCat.Ingredient; _pickId = null; _qty = 1;
            var save = gm != null ? gm.Save : null;
            if (save == null) return;
            LifeItems.Ensure(save);
            _canvas = CoastUiCanvas.Create("ShopCanvas", 462);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            // 94차 시안: 어두운 딤 → 라벤더 파스텔 바탕 + 꽃 장식(글자 없음)
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.90f, 0.85f, 0.96f, 0.985f));
            dim.raycastTarget = true;
            var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None; db.onClick.AddListener(Close);
            var deco = new GameObject("Deco", typeof(RectTransform)).GetComponent<RectTransform>();
            deco.SetParent(_root, false);
            deco.anchorMin = Vector2.zero; deco.anchorMax = Vector2.one; deco.offsetMin = new Vector2(-pad + 4f, -pad + 4f); deco.offsetMax = new Vector2(pad - 4f, pad - 4f);
            HomeUI.SprinklePlayDecor(deco);
            if (_tab == 1) PetShop.NotifyUnlockToast(save);
            Build();
        }

        private static void Build()
        {
            var save = _gm != null ? _gm.Save : null; if (save == null || _root == null) return;
            PetShop.EnsureEquipped(save);
            LifeItems.Ensure(save);
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (c != null && c.name == "Card") UnityEngine.Object.Destroy(c.gameObject);
            }

            // 94차 시안: 카드 안 맨 위에 보라 헤더(집 아이콘 「상점」 · 코인 돈 · ♥ · 나가기), 탭·지갑·칩은 그만큼 아래로
            float h = _tab == 0 ? 1150f : 1060f;
            var card = CoastUiArt.Panel(_root, "Card", Gold, 34); card.raycastTarget = true;
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 0f); crt.sizeDelta = new Vector2(684f, h);
            var inner = CoastUiArt.Panel(crt, "Inner", new Color(0.996f, 0.96f, 0.85f), 30); inner.raycastTarget = false;
            inner.rectTransform.anchorMin = Vector2.zero; inner.rectTransform.anchorMax = Vector2.one;
            inner.rectTransform.offsetMin = new Vector2(6f, 6f); inner.rectTransform.offsetMax = new Vector2(-6f, -6f);

            BuildHeader(crt, save);
            Tab(crt, 0, Loc.T("일반상점", "Shop"), -152f);
            Tab(crt, 1, Loc.T("펫상점", "Pet Shop"), 152f);

            string walletTxt = _tab == 0
                ? Loc.T($"★ {LevelSystem.FormatK(save.stats.money)}G  ·  Lv {Mathf.Max(1, save.level)} ★", $"★ {LevelSystem.FormatK(save.stats.money)}G  ·  Lv {Mathf.Max(1, save.level)} ★")
                : Loc.T($"★ 코인 {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★", $"★ Coins {CoinWallet.TotalStatic:N0}  ·  Lv {Mathf.Max(1, save.level)} ★");   // 109차: 코인 = 돈
            var wpill = CoastUiArt.GlossyPill(crt, "WalletPill", new Color(0.52f, 0.40f, 0.82f), 22, 6); wpill.raycastTarget = false;
            var wrt = wpill.rectTransform; wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 1f); wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = new Vector2(0f, -HeaderShift - 158f); wrt.sizeDelta = new Vector2(300f, 46f);
            var wallet = CoastHudLayout.MakeText(wrt, "Wallet", walletTxt, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 3f), new Vector2(-8f, 0f));
            wallet.color = Color.white; wallet.fontStyle = FontStyle.Bold;
            wallet.resizeTextForBestFit = true; wallet.resizeTextMinSize = 12; wallet.resizeTextMaxSize = CoastHudLayout.Scaled(20);

            // 105차(재미요소 P0-4): 지갑 알약 오른쪽 작은 「정기 장보기」 토글 — 켜 두면 결산 때 먹을 게 없을 때 흰밥을 자동으로 산다
            if (_tab == 0)
            {
                var ag = CoastUiArt.GlossyPill(crt, "AutoGrocery", save.autoGrocery ? new Color(0.30f, 0.72f, 0.45f) : new Color(0.62f, 0.60f, 0.66f), 18, 5);
                var agr = ag.rectTransform; agr.anchorMin = agr.anchorMax = new Vector2(1f, 1f); agr.pivot = new Vector2(1f, 1f);
                agr.anchoredPosition = new Vector2(-22f, -HeaderShift - 158f); agr.sizeDelta = new Vector2(150f, 46f); ag.raycastTarget = true;
                var agt = CoastHudLayout.MakeText(agr, "T", Loc.T(save.autoGrocery ? "정기 장보기 ON" : "정기 장보기 OFF", save.autoGrocery ? "Auto buy ON" : "Auto buy OFF"), 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, 0f));
                agt.color = Color.white; agt.fontStyle = FontStyle.Bold; agt.resizeTextForBestFit = true; agt.resizeTextMinSize = 10; agt.resizeTextMaxSize = CoastHudLayout.Scaled(14);
                var agb = ag.gameObject.AddComponent<Button>(); agb.transition = Selectable.Transition.None;
                agb.onClick.AddListener(() => { CoastPrefs.Vibrate(); save.autoGrocery = !save.autoGrocery; _gm?.Persist(); CoastToast.Show(save.autoGrocery ? Loc.T("먹을 게 없으면 결산 때 흰밥을 자동으로 살게.", "Auto-buys rice at week end when out of food.") : Loc.T("정기 장보기 끔.", "Auto buy off.")); Build(); });
            }
            if (_tab == 0) BuildGoods(crt, save);
            else BuildPets(crt, save);

            if (_tab == 1)
            {
                var close = CoastUiArt.GlossyPill(crt, "Close", new Color(0.60f, 0.62f, 0.70f), 20, 7);
                var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0f); clrt.pivot = new Vector2(0.5f, 0f);
                clrt.anchoredPosition = new Vector2(0f, 14f); clrt.sizeDelta = new Vector2(300f, 54f); close.raycastTarget = true;
                var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero);
                ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
                var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
            }
        }

        /// 헤더 높이만큼 아래 요소를 내리는 값.
        private const float HeaderShift = 70f;

        private static void BuildHeader(RectTransform crt, SaveData save)
        {
            var head = CoastUiArt.CutePill(crt, "Head", new Color(0.42f, 0.30f, 0.58f), 22, 4); head.raycastTarget = false;
            var hrt = head.rectTransform; hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -14f); hrt.sizeDelta = new Vector2(-28f, 62f);
            var homeIc = CoastUiArt.Icon("Home");
            if (homeIc != null)
            {
                var hi = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                hi.transform.SetParent(hrt, false); hi.sprite = homeIc; hi.preserveAspect = true; hi.raycastTarget = false;
                hi.rectTransform.anchorMin = hi.rectTransform.anchorMax = new Vector2(0f, 0.5f); hi.rectTransform.pivot = new Vector2(0f, 0.5f);
                hi.rectTransform.anchoredPosition = new Vector2(16f, 0f); hi.rectTransform.sizeDelta = new Vector2(36f, 36f);
            }
            var title = CoastHudLayout.MakeText(hrt, "Title", Loc.T("상점", "Shop"), 26, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(homeIc != null ? 62f : 20f, 0f), new Vector2(-300f, 0f));
            title.color = Color.white; title.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(title, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var coinIc = CoastUiArt.Icon("Coin");
            if (coinIc != null)
            {
                var ci = new GameObject("Coin", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ci.transform.SetParent(hrt, false); ci.sprite = coinIc; ci.preserveAspect = true; ci.raycastTarget = false;
                ci.rectTransform.anchorMin = ci.rectTransform.anchorMax = new Vector2(1f, 0.5f); ci.rectTransform.pivot = new Vector2(1f, 0.5f);
                ci.rectTransform.anchoredPosition = new Vector2(-236f, 0f); ci.rectTransform.sizeDelta = new Vector2(28f, 28f);
            }
            var money = CoastHudLayout.MakeText(hrt, "Money", Loc.T($"{LevelSystem.FormatK(save.stats.money)}G", $"{LevelSystem.FormatK(save.stats.money)}G"), 20, TextAnchor.MiddleLeft, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-232f, 0f), new Vector2(-150f, 0f));
            money.color = new Color(1f, 0.88f, 0.50f); money.fontStyle = FontStyle.Bold;
            money.resizeTextForBestFit = true; money.resizeTextMinSize = 12; money.resizeTextMaxSize = CoastHudLayout.Scaled(20);   // 136차
            var hearts = CoastHudLayout.MakeText(hrt, "Hearts", $"♥{Mathf.Max(0, save.stats.hearts)}", 20, TextAnchor.MiddleLeft, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-148f, 0f), new Vector2(-96f, 0f));
            hearts.color = new Color(1f, 0.55f, 0.70f); hearts.fontStyle = FontStyle.Bold;
            var leave = CoastUiArt.GlossyPill(hrt, "Leave", new Color(0.66f, 0.60f, 0.76f), 16, 5);
            var lrt = leave.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(1f, 0.5f); lrt.pivot = new Vector2(1f, 0.5f);
            lrt.anchoredPosition = new Vector2(-8f, 0f); lrt.sizeDelta = new Vector2(84f, 42f); leave.raycastTarget = true;
            var lt = CoastHudLayout.MakeText(lrt, "T", Loc.T("나가기", "Leave"), 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            lt.color = Color.white; lt.fontStyle = FontStyle.Bold;
            var lb = leave.gameObject.AddComponent<Button>(); lb.transition = Selectable.Transition.None; lb.onClick.AddListener(() => { CoastPrefs.Vibrate(); Close(); });
        }

        private static void BuildGoods(RectTransform crt, SaveData save)
        {
            // 카테고리 칩
            float chipW = 100f;
            for (int i = 0; i < Cats.Length; i++)
            {
                var cat = Cats[i];
                bool on = _cat == cat;
                var pill = CoastUiArt.GlossyPill(crt, "Cat" + i, on ? new Color(1f, 0.86f, 0.30f) : new Color(0.88f, 0.82f, 0.70f), 14, 4);
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = new Vector2(14f + i * (chipW + 5f), -HeaderShift - 214f); prt.sizeDelta = new Vector2(chipW, 42f); pill.raycastTarget = true;
                var t = CoastHudLayout.MakeText(prt, "T", LifeItems.CatLabel(cat), 12, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.color = on ? new Color(0.40f, 0.22f, 0.04f) : Navy; t.fontStyle = FontStyle.Bold;
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = 12;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                LifeItemCat pick = cat;
                b.onClick.AddListener(() => { if (_cat == pick) return; CoastPrefs.Vibrate(); _cat = pick; _pickId = null; _qty = 1; Build(); });
            }

            string hint = _cat == LifeItemCat.Ingredient
                ? Loc.T("재료는 싸요. 마이룸에서 조리해야 먹을 수 있어요.", "Ingredients are cheap — cook in My Room to eat.")
                : _cat == LifeItemCat.BasicDish || _cat == LifeItemCat.PremiumDish
                    ? Loc.T("완제품 요리 — 「밥」 버튼으로 식사하면 소진돼요.", "Ready meals — Feed button consumes them.")
                    : Loc.T("약·케어는 보유 가방에서 바로 사용.", "Medicine/care — use from your bag.");
            var ht = CoastHudLayout.MakeText(crt, "Hint", hint, 12, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -HeaderShift - 290f), new Vector2(-16f, -HeaderShift - 260f));
            ht.color = new Color(0.45f, 0.32f, 0.18f); ht.horizontalOverflow = HorizontalWrapMode.Wrap;
            ht.resizeTextForBestFit = true; ht.resizeTextMinSize = 10; ht.resizeTextMaxSize = CoastHudLayout.Scaled(12);

            var items = new List<LifeItemDef>();
            foreach (var d in LifeItems.ShopOf(_cat)) items.Add(d);
            if (items.Count > 0 && string.IsNullOrEmpty(_pickId)) _pickId = items[0].id;
            var cur = string.IsNullOrEmpty(_pickId) ? (LifeItemDef?)null : LifeItems.Get(_pickId);
            if (cur.HasValue && cur.Value.cat != _cat)
                _pickId = items.Count > 0 ? items[0].id : null;

            var scrollGo = new GameObject("GScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(crt, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(18f, 176f); srt.offsetMax = new Vector2(-18f, -HeaderShift - 294f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            var sr = scrollGo.GetComponent<ScrollRect>(); sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(srt, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            sr.content = content; sr.viewport = srt;

            Color[] rowCols = { new Color(1f, 0.95f, 0.76f), new Color(1f, 0.82f, 0.88f), new Color(0.86f, 0.80f, 0.98f), new Color(0.80f, 0.95f, 0.84f) };
            float y = 0f; const float rowH = 100f;
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i];
                bool sel = d.id == _pickId;
                var row = CoastUiArt.Panel(content, "G" + i, sel ? new Color(1f, 0.92f, 0.55f) : Color.white, 16); row.raycastTarget = true;
                var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
                rrt.anchoredPosition = new Vector2(0f, -y); rrt.sizeDelta = new Vector2(600f, rowH);
                var fill = CoastUiArt.Panel(row.transform, "F", rowCols[i % rowCols.Length], 14); fill.raycastTarget = false;
                fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = new Vector2(3f, 3f); fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

                var frame = CoastUiArt.Panel(row.transform, "Art", Color.white, 12); frame.raycastTarget = false;
                var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f);
                frt.anchoredPosition = new Vector2(10f, 0f); frt.sizeDelta = new Vector2(72f, 72f);
                var tex = ArtAssets.LoadTexture(d.art);
                if (tex != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(frame.transform, false); im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; im.raycastTarget = false;
                    var irt = im.rectTransform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = new Vector2(3f, 3f); irt.offsetMax = new Vector2(-3f, -3f);
                }

                int have = LifeItems.Count(save, d.id);
                var nm = CoastHudLayout.MakeText(row.transform, "N", LifeItems.Name(d), 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(94f, -44f), new Vector2(-96f, -6f));
                nm.color = Navy; nm.fontStyle = FontStyle.Bold;
                string line2 = $"{d.price}G" + (have > 0 ? Loc.T($" · 보유 {have}", $" · have {have}") : "") + " · " + LifeItems.EffectText(d);
                var bl = CoastHudLayout.MakeText(row.transform, "B", line2, 13, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(94f, 8f), new Vector2(-96f, -46f));
                bl.color = new Color(0.28f, 0.24f, 0.36f); bl.horizontalOverflow = HorizontalWrapMode.Wrap;
                bl.resizeTextForBestFit = true; bl.resizeTextMinSize = 10; bl.resizeTextMaxSize = 13;

                string id = d.id;
                var btn = row.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); _pickId = id; _qty = 1; Build(); });

                // 94차 시안: 줄 오른쪽 노랑 장바구니 버튼 — 고르고 바로 1개 구매
                var cartPill = CoastUiArt.GlossyPill(row.transform, "Cart", new Color(1f, 0.82f, 0.30f), 14, 5);
                var cprt = cartPill.rectTransform; cprt.anchorMin = cprt.anchorMax = new Vector2(1f, 0.5f); cprt.pivot = new Vector2(1f, 0.5f);
                cprt.anchoredPosition = new Vector2(-12f, 0f); cprt.sizeDelta = new Vector2(72f, 52f); cartPill.raycastTarget = true;
                var cartIc = CoastUiArt.Icon("Cart");
                if (cartIc != null)
                {
                    var ci = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    ci.transform.SetParent(cprt, false); ci.sprite = cartIc; ci.preserveAspect = true; ci.raycastTarget = false; ci.color = new Color(0.40f, 0.26f, 0.08f);
                    ci.rectTransform.anchorMin = ci.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); ci.rectTransform.anchoredPosition = new Vector2(0f, 2f); ci.rectTransform.sizeDelta = new Vector2(30f, 30f);
                }
                var cbtn = cartPill.gameObject.AddComponent<Button>(); cbtn.transition = Selectable.Transition.None;
                var dd = d;
                cbtn.onClick.AddListener(() =>
                {
                    CoastPrefs.Vibrate(); _pickId = id; _qty = 1;
                    if (LifeItems.Buy(save, id, 1)) Bought(Loc.T($"{LifeItems.Name(dd)} ×1 구매!", $"Bought {LifeItems.Name(dd)} ×1!"));
                    else Short();
                });
                y += rowH + 8f;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Max(y + 8f, 120f));

            // 하단: 수량 + 구매
            BuildQtyBar(crt, save);
        }

        private static void BuildQtyBar(RectTransform crt, SaveData save)
        {
            var bar = CoastUiArt.Panel(crt, "QtyBar", new Color(1f, 0.98f, 0.92f), 18); bar.raycastTarget = false;
            var brt = bar.rectTransform; brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0f);
            brt.offsetMin = new Vector2(18f, 44f); brt.offsetMax = new Vector2(-18f, 166f);
            // 94차 시안: 오른쪽 아래 「닫기」(구매 아래) · 맨 아래 작은 안내 띠
            var close = CoastUiArt.GlossyPill(brt, "Close", new Color(0.60f, 0.62f, 0.72f), 16, 6);
            var clrt = close.rectTransform; clrt.anchorMin = clrt.anchorMax = new Vector2(1f, 0f); clrt.pivot = new Vector2(1f, 0f);
            clrt.anchoredPosition = new Vector2(-12f, 8f); clrt.sizeDelta = new Vector2(150f, 48f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(clrt, "T", Loc.T("닫기", "Close"), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
            var foot = CoastHudLayout.MakeText(crt, "Foot", Loc.T("❀  선택한 아이템을 구매하면 가방에 들어가요  ❀", "❀  Bought items go to your bag  ❀"), 12, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 12f), new Vector2(-24f, 38f));
            foot.color = new Color(0.45f, 0.32f, 0.28f); foot.raycastTarget = false;

            var def = string.IsNullOrEmpty(_pickId) ? (LifeItemDef?)null : LifeItems.Get(_pickId);
            if (!def.HasValue)
            {
                var empty = CoastHudLayout.MakeText(brt, "E", Loc.T("위에서 상품을 골라 주세요", "Pick an item above"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                empty.color = Navy; return;
            }
            var d = def.Value;
            _qty = Mathf.Clamp(_qty, 1, 99);
            int total = d.price * _qty;
            bool can = save.stats.money >= total;

            var label = CoastHudLayout.MakeText(brt, "L", Loc.T($"{LifeItems.Name(d)} × {_qty}  =  {total}G", $"{LifeItems.Name(d)} × {_qty}  =  {total}G"), 22, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-170f, -4f));
            label.color = Navy; label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = 20;

            // − / +
            MakeQtyBtn(brt, "M", "−", new Vector2(0f, 0f), new Vector2(16f, 12f), () => { _qty = Mathf.Max(1, _qty - 1); Build(); });
            var qt = CoastHudLayout.MakeText(brt, "Q", _qty.ToString(), 24, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(78f, 12f), new Vector2(130f, 56f));
            qt.color = Navy; qt.fontStyle = FontStyle.Bold;
            // fix qt anchors
            var qrt = qt.rectTransform; qrt.anchorMin = qrt.anchorMax = new Vector2(0f, 0f); qrt.pivot = new Vector2(0.5f, 0f);
            qrt.anchoredPosition = new Vector2(104f, 14f); qrt.sizeDelta = new Vector2(48f, 44f);
            MakeQtyBtn(brt, "P", "+", new Vector2(0f, 0f), new Vector2(140f, 12f), () => { _qty = Mathf.Min(99, _qty + 1); Build(); });

            var buy = CoastUiArt.GlossyPill(brt, "Buy", can ? new Color(0.30f, 0.62f, 1f) : new Color(0.55f, 0.57f, 0.64f), 18, 6);
            var buyRt = buy.rectTransform; buyRt.anchorMin = buyRt.anchorMax = new Vector2(1f, 1f); buyRt.pivot = new Vector2(1f, 1f);
            buyRt.anchoredPosition = new Vector2(-12f, -10f); buyRt.sizeDelta = new Vector2(150f, 50f); buy.raycastTarget = true;
            var bt = CoastHudLayout.MakeText(buyRt, "T", Loc.T("구매", "Buy"), 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
            var bb = buy.gameObject.AddComponent<Button>(); bb.transition = Selectable.Transition.None;
            string id = d.id; int q = _qty;
            bb.onClick.AddListener(() =>
            {
                CoastPrefs.Vibrate();
                if (LifeItems.Buy(save, id, q)) Bought(Loc.T($"{LifeItems.Name(d)} ×{q} 구매!", $"Bought {LifeItems.Name(d)} ×{q}!"));
                else Short();
            });
        }

        private static void MakeQtyBtn(RectTransform parent, string name, string label, Vector2 anchor, Vector2 pos, Action act)
        {
            var pill = CoastUiArt.GlossyPill(parent, name, new Color(0.92f, 0.70f, 0.35f), 14, 4);
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(52f, 44f); pill.raycastTarget = true;
            var t = CoastHudLayout.MakeText(rt, "T", label, 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            t.color = Color.white; t.fontStyle = FontStyle.Bold;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); act(); });
        }

        private static void BuildPets(RectTransform crt, SaveData save)
        {
            const float rowH = 138f, rowGap = 14f, top = 236f + HeaderShift;
            bool unlocked = PetShop.Unlocked(save);
            var kinds = PetShop.ForSale;
            Color[] rowCols = { new Color(1f, 0.95f, 0.76f), new Color(1f, 0.82f, 0.88f), new Color(0.86f, 0.80f, 0.98f), new Color(0.80f, 0.95f, 0.84f) };
            for (int i = 0; i < kinds.Length && i < 4; i++)
            {
                var k = kinds[i];
                bool owned = PetShop.Owns(save, k), equipped = save.equippedPet == k;
                string priceTxt = owned ? (equipped ? Loc.T("장착 중", "Equipped") : Loc.T("보유", "Owned")) : $"{PetShop.Price[k]:N0}c · Lv{PetShop.LevelReq[k]}";
                string label = !unlocked ? Loc.T("잠김", "Locked") : !owned ? Loc.T("구매", "Buy") : equipped ? Loc.T("장착 중", "Equipped") : Loc.T("장착", "Equip");
                bool can = unlocked && (!owned ? PetShop.CanAfford(save, k) : !equipped);
                var pk = k;
                GoodsRow(crt, i, rowCols[i % rowCols.Length], top, rowH, rowGap, "UI_Pet_" + k, PetCompanion.Names[(int)k], PetCompanion.Blurbs[(int)k], priceTxt, null, can, () => ActPet(pk), label, "Obs_Pet_" + k);
            }
            if (!unlocked)
            {
                var lockT = CoastHudLayout.MakeText(crt, "Lock", Loc.T($"펫은 {PetShop.UnlockWeek}주차 또는 대회 {PetShop.UnlockContests}회 클리어 후 (지금 {save.week}주 · 대회 {PetShop.ContestsCleared(save)}회)", $"Pets unlock at week {PetShop.UnlockWeek} or {PetShop.UnlockContests} contests (now wk {save.week} · {PetShop.ContestsCleared(save)} contests)"), 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 74f), new Vector2(-8f, 100f));
                lockT.color = new Color(0.62f, 0.36f, 0.04f); lockT.fontStyle = FontStyle.Bold; lockT.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        private static void Tab(RectTransform crt, int idx, string label, float x)
        {
            bool on = _tab == idx;
            var pill = CoastUiArt.GlossyPill(crt, "Tab" + idx, on ? new Color(1f, 0.86f, 0.30f) : new Color(0.86f, 0.80f, 0.68f), 30, 8);
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -HeaderShift - 60f); rt.sizeDelta = new Vector2(288f, 80f); pill.raycastTarget = true;
            var ic = CoastUiArt.Art("Icon_Cart");
            if (ic != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(rt, false); im.sprite = ic; im.preserveAspect = true; im.raycastTarget = false;
                im.color = on ? new Color(0.45f, 0.26f, 0.04f) : new Color(0.55f, 0.48f, 0.36f);
                var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.pivot = new Vector2(0f, 0.5f);
                irt.anchoredPosition = new Vector2(22f, 4f); irt.sizeDelta = new Vector2(38f, 38f);
            }
            var t = CoastHudLayout.MakeText(rt, "T", label, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(56f, 4f), new Vector2(-10f, 0f));
            t.color = on ? new Color(0.45f, 0.26f, 0.04f) : new Color(0.50f, 0.44f, 0.34f); t.fontStyle = FontStyle.Bold;
            if (on) { EventCardKit.Sparkle(rt, new Vector2(0f, 1f), new Vector2(18f, -14f), 16, Color.white); EventCardKit.Sparkle(rt, new Vector2(1f, 0f), new Vector2(-16f, 14f), 12, Color.white); }
            int pick = idx;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { if (_tab == pick) return; CoastPrefs.Vibrate(); _tab = pick; _pickId = null; _qty = 1; if (_tab == 1 && _gm != null) PetShop.NotifyUnlockToast(_gm.Save); Build(); });
        }

        private static void GoodsRow(RectTransform crt, int i, Color col, float top, float rowH, float rowGap, string art, string name, string blurb, string priceTxt, string haveTxt, bool can, Action act, string btnLabel = null, string artFallback = null)
        {
            var row = CoastUiArt.Panel(crt, "Row" + i, Color.white, 22); row.raycastTarget = false;
            var rrt = row.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1f); rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -top - i * (rowH + rowGap)); rrt.sizeDelta = new Vector2(612f, rowH);
            var rowIn = CoastUiArt.Panel(row.transform, "Fill", col, 20); rowIn.raycastTarget = false;
            rowIn.rectTransform.anchorMin = Vector2.zero; rowIn.rectTransform.anchorMax = Vector2.one;
            rowIn.rectTransform.offsetMin = new Vector2(4f, 4f); rowIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
            var frame = CoastUiArt.Panel(row.transform, "Frame", Color.white, 16); frame.raycastTarget = false;
            var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0f, 0.5f); frt.pivot = new Vector2(0f, 0.5f);
            frt.anchoredPosition = new Vector2(14f, 0f); frt.sizeDelta = new Vector2(112f, 112f);
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
            var pprt = pp.rectTransform; pprt.anchorMin = pprt.anchorMax = new Vector2(1f, 1f); pprt.pivot = new Vector2(1f, 1f);
            pprt.anchoredPosition = new Vector2(-12f, -10f); pprt.sizeDelta = new Vector2(haveTxt != null ? 190f : 150f, 34f);
            var price = CoastHudLayout.MakeText(pprt, "T", "◆ " + priceTxt + (haveTxt != null ? " · " + haveTxt : "") + " ◆", 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 2f), Vector2.zero);
            price.color = new Color(0.45f, 0.24f, 0f); price.fontStyle = FontStyle.Bold; price.resizeTextForBestFit = true; price.resizeTextMinSize = 10; price.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var btn = CoastUiArt.GlossyPill(row.transform, "Act", can ? new Color(0.30f, 0.62f, 1f) : new Color(0.55f, 0.57f, 0.64f), 22, 8);
            var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-12f, 10f); brt.sizeDelta = new Vector2(150f, 56f); btn.raycastTarget = true;
            var lb = CoastHudLayout.MakeText(brt, "T", btnLabel ?? Loc.T("구매", "Buy"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero);
            lb.color = Color.white; lb.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lb, new Color(0f, 0f, 0.2f, 0.35f), 1.5f);
            var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); act?.Invoke(); });
        }

        private static void ActPet(PetKind k)
        {
            var save = _gm != null ? _gm.Save : null; if (save == null) return;
            if (!PetShop.Unlocked(save)) { CoastToast.Show(PetShop.LockReason(save) ?? Loc.T("펫 상점 잠김", "Pet shop locked")); return; }
            if (!PetShop.Owns(save, k))
            {
                if (PetShop.TryBuy(save, k)) { CoastToast.Show(Loc.T($"{PetCompanion.Names[(int)k]}를 데려왔어!", $"{PetCompanion.Names[(int)k]} joined!")); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.6f); _gm.Persist(); }
                else { CoastToast.Show(Loc.T($"코인이 모자라거나 레벨(Lv{PetShop.LevelReq[k]}) 이 부족해 — 러닝에서 더 모아 오자", $"Not enough coins or level (Lv{PetShop.LevelReq[k]}) — run more!")); CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f); }
            }
            else if (save.equippedPet != k) { PetShop.Equip(save, k); _gm.Persist(); }
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
