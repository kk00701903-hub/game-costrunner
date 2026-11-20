using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 198차(사용자): ⑪ 도구 등급(재료 모아 업그레이드 — 시내 「탐라 도구 공방」) · ⑫ 젤리 가챠(시내 「럭키 가챠샵」, 모든 아이템 희귀도별)
    public partial class VillageHub
    {
        // ── ⑪ 도구 등급 ─────────────────────────────────────────────────
        // 도구 번호: 0 잠자리채 · 1 방망이 · 2 도끼 · 3 곡괭이 · 4 낚싯대
        public const int ToolCount = 5, TierMax = 3;
        static readonly string[] ToolKo = { "잠자리채", "방망이", "도끼", "곡괭이", "낚싯대" };
        static readonly string[] ToolEn = { "Net", "Bat", "Axe", "Pickaxe", "Rod" };
        static readonly string[] TierKo = { "", "튼튼한 ", "은빛 ", "황금 " };
        static readonly string[] TierEn = { "", "Sturdy ", "Silver ", "Golden " };
        static readonly string[] ToolPerkKo = {
            "잡을 때 덜 도망가고, 희귀 벌레가 더 자주 나온다",
            "큰 벌레·산적을 칠 때 돈을 조금 더 받는다",
            "나무를 팰 때 장작이 더 나온다",
            "돌·광석이 더 나오고 보석이 잘 나온다",
            "귀한 물고기가 더 잘 문다",
        };
        struct Recipe { public int money; public (string id, int n)[] mats; }
        static readonly Recipe[] Recipes = {
            new Recipe { money = 300,  mats = new[] { ("mat_wood", 8), ("mat_stone", 6), ("ore_iron", 2) } },                      // → 튼튼한
            new Recipe { money = 900,  mats = new[] { ("mat_wood", 10), ("ore_iron", 5), ("ore_silver", 3) } },                    // → 은빛
            new Recipe { money = 2500, mats = new[] { ("ore_silver", 4), ("ore_gold", 4), ("gem_amethyst", 1) } },                 // → 황금
        };

        int[] ToolTiers { get { if (Save.toolTier == null || Save.toolTier.Length < ToolCount) { var a = new int[ToolCount]; if (Save.toolTier != null) Array.Copy(Save.toolTier, a, Mathf.Min(ToolCount, Save.toolTier.Length)); Save.toolTier = a; } return Save.toolTier; } }
        public int Tier(int tool) => Save == null ? 0 : Mathf.Clamp(ToolTiers[Mathf.Clamp(tool, 0, ToolCount - 1)], 0, TierMax);
        string ToolLabel(int tool) => Loc.T(TierKo[Tier(tool)] + ToolKo[tool], TierEn[Tier(tool)] + ToolEn[tool]);
        /// 등급을 크리처·낚시에 알려 준다(도구를 바꿀 때·업그레이드 때·시작 때)
        void PushToolTiers()
        {
            if (Save == null) return;
            VillageCreatures.NetTier = Tier(0); VillageCreatures.BatTier = Tier(1); FishingMini.RodTier = Tier(4);
        }

        bool HasMats(Recipe r) { if (Save.stats.money < r.money) return false; foreach (var m in r.mats) if (LifeItems.Count(Save, m.id) < m.n) return false; return true; }
        string MatsText(Recipe r)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var m in r.mats) { var d = LifeItems.Get(m.id); string nm = d.HasValue ? LifeItems.Name(d.Value) : m.id; int have = LifeItems.Count(Save, m.id); sb.Append($"{nm} {Mathf.Min(have, m.n)}/{m.n} · "); }
            sb.Append($"{r.money:N0}G");
            return sb.ToString();
        }

        void WorkshopMenu()
        {
            if (Save == null) return;
            var items = new List<(string, Color, Action)>();
            for (int t = 0; t < ToolCount; t++)
            {
                int tool = t, tier = Tier(t);
                if (tier >= TierMax) { items.Add((Loc.T($"★ {ToolLabel(tool)} — 최고 등급", $"★ {ToolLabel(tool)} — max"), new Color(0.95f, 0.78f, 0.25f), null)); continue; }
                var r = Recipes[tier]; bool ok = HasMats(r);
                string next = Loc.T(TierKo[tier + 1] + ToolKo[tool], TierEn[tier + 1] + ToolEn[tool]);
                items.Add((Loc.T($"{ToolLabel(tool)} → {next}", $"{ToolLabel(tool)} → {next}"), ok ? new Color(0.45f, 0.72f, 0.95f) : new Color(0.66f, 0.66f, 0.70f), () => ShowUpgrade(tool)));
            }
            _hud.Choice(Loc.T("탐라 도구 공방", "Tamra Tool Workshop"), Loc.T("재료를 가져오면 도구를 한 단계씩 올려 줄게. 튼튼한 → 은빛 → 황금.", "Bring materials and I'll upgrade your tools: Sturdy → Silver → Golden."), items.ToArray());
        }
        void ShowUpgrade(int tool)
        {
            int tier = Tier(tool); if (tier >= TierMax) return;
            var r = Recipes[tier]; bool ok = HasMats(r);
            string next = Loc.T(TierKo[tier + 1] + ToolKo[tool], TierEn[tier + 1] + ToolEn[tool]);
            _hud.Choice(next, Loc.T($"필요: {MatsText(r)}\n효과: {ToolPerkKo[tool]}", $"Needs: {MatsText(r)}"),
                new (string, Color, Action)[] {
                    (ok ? Loc.T("▲ 업그레이드", "▲ Upgrade") : Loc.T("재료가 모자라", "Not enough materials"), ok ? new Color(0.35f, 0.78f, 0.50f) : new Color(0.66f, 0.66f, 0.70f), ok ? (Action)(() => DoUpgrade(tool)) : null),
                    (Loc.T("다른 도구 보기", "Other tools"), new Color(0.6f, 0.6f, 0.66f), WorkshopMenu),
                });
        }
        void DoUpgrade(int tool)
        {
            int tier = Tier(tool); if (tier >= TierMax) return; var r = Recipes[tier]; if (!HasMats(r)) return;
            Save.stats.money -= r.money; foreach (var m in r.mats) LifeItems.Take(Save, m.id, m.n);
            ToolTiers[tool] = tier + 1; PushToolTiers(); _gm.Persist(); RefreshStatus(); ApplyTool();
            VillagePang.Burst(_player.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.35f), Color.white, 1.4f); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            _hud.Bubble(Loc.T("공방 주인", "Smith"), Loc.T($"자, {ToolLabel(tool)}! {ToolPerkKo[tool]}.", $"Here — {ToolLabel(tool)}!"));
        }

        // ── ⑫ 젤리 가챠 ─────────────────────────────────────────────────
        public const int GachaCost = 30, GachaCost10 = 270;
        enum Rar { Common, Uncommon, Rare, Epic, Legend }
        static readonly string[] RarKo = { "보통", "고급", "희귀", "영웅", "전설" };
        static readonly Color[] RarCol = { new Color(0.72f, 0.74f, 0.78f), new Color(0.45f, 0.80f, 0.50f), new Color(0.40f, 0.62f, 0.98f), new Color(0.75f, 0.45f, 0.95f), new Color(1f, 0.72f, 0.20f) };
        static readonly float[] RarP = { 0.55f, 0.27f, 0.12f, 0.05f, 0.01f };
        static List<(string id, Rar r)> _pool;
        /// 모든 아이템(파는 값 표 + 생활 아이템 가격)을 값으로 희귀도를 나눈다
        static List<(string id, Rar r)> Pool()
        {
            if (_pool != null) return _pool;
            _pool = new List<(string, Rar)>(); var seen = new HashSet<string>();
            void Put(string id, int v)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id) || !LifeItems.Get(id).HasValue) return;
                if (id == "fish_5") return;   // 낡은 장화는 빼기
                Rar r = v >= 600 ? Rar.Legend : v >= 250 ? Rar.Epic : v >= 120 ? Rar.Rare : v >= 50 ? Rar.Uncommon : Rar.Common;
                _pool.Add((id, r));
            }
            foreach (var p in VillageSell.Prices) Put(p.id, p.price);
            foreach (var d in LifeItems.All) if (d.price > 0) Put(d.id, d.price / Mathf.Max(1, EconomyScale.Living));
            return _pool;
        }
        (string id, Rar r) Roll()
        {
            var pool = Pool(); float x = UnityEngine.Random.value; Rar want = Rar.Common; float acc = 0f;
            for (int i = 0; i < RarP.Length; i++) { acc += RarP[i]; if (x <= acc) { want = (Rar)i; break; } }
            for (int w = (int)want; w >= 0; w--)
            {
                var cand = pool.FindAll(p => (int)p.r == w); if (cand.Count > 0) return cand[UnityEngine.Random.Range(0, cand.Count)];
            }
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        void GachaMenu()
        {
            if (Save == null) return;
            int j = JellyWallet.Total;
            _hud.Choice(Loc.T("럭키 가챠샵", "Lucky Gacha"), Loc.T($"젤리로 캡슐을 뽑아요. 가게의 모든 물건이 들어 있어요 — 전설 1% · 영웅 5% · 희귀 12% · 고급 27% · 보통 55%\n가진 젤리 {j}", $"Pull capsules with jelly. Legend 1% · Epic 5% · Rare 12% · Uncommon 27% · Common 55%\nJelly {j}"),
                new (string, Color, Action)[] {
                    (Loc.T($"● 1회 뽑기 · 젤리 {GachaCost}", $"● 1 pull · {GachaCost} jelly"), j >= GachaCost ? new Color(1f, 0.62f, 0.72f) : new Color(0.66f, 0.66f, 0.70f), () => Pull(1)),
                    (Loc.T($"●● 10회 뽑기 · 젤리 {GachaCost10} (1회 덤)", $"●● 10 pulls · {GachaCost10} jelly"), j >= GachaCost10 ? new Color(0.95f, 0.55f, 0.35f) : new Color(0.66f, 0.66f, 0.70f), () => Pull(10)),
                    (Loc.T("확률표 보기", "Odds"), new Color(0.6f, 0.6f, 0.66f), GachaOdds),
                });
        }
        void GachaOdds()
        {
            var pool = Pool(); var sb = new System.Text.StringBuilder();
            for (int r = 4; r >= 0; r--) { int n = pool.FindAll(p => (int)p.r == r).Count; sb.Append($"{RarKo[r]} {RarP[r] * 100f:0.#}% ({n}종)  "); }
            _hud.Bubble(Loc.T("가챠샵", "Gacha"), sb.ToString());
        }
        void Pull(int n)
        {
            int cost = n >= 10 ? GachaCost10 : GachaCost;
            if (!JellyWallet.TrySpend(cost)) { _hud.Bubble(Hero, Loc.T("젤리가 모자라… K-POP 러닝에서 모아 오자.", "Not enough jelly… collect some in a K-POP run.")); return; }
            JellyWallet.Flush();
            var got = new List<(string id, Rar r)>();
            for (int i = 0; i < n; i++) got.Add(Roll());
            if (n >= 10) { var extra = Roll(); if ((int)extra.r < (int)Rar.Rare) { var rares = Pool().FindAll(p => (int)p.r >= (int)Rar.Rare); if (rares.Count > 0) extra = rares[UnityEngine.Random.Range(0, rares.Count)]; } got.Add(extra); }   // 10연: 희귀 이상 1개 보장
            foreach (var g in got) LifeItems.Add(Save, g.id, 1);
            _gm.Persist(); RefreshStatus();
            StartCoroutine(GachaShow(got));
        }
        IEnumerator GachaShow(List<(string id, Rar r)> got)
        {
            _busy = true;
            var cv = CoastUiCanvas.Create("GachaResult", 168); var root = CoastUiCanvas.Root(cv);
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.08f, 0.05f, 0.15f, 0.88f)); dim.raycastTarget = true;
            var title = CoastHudLayout.MakeText(root, "T", Loc.T("캡슐 오픈!", "Capsules!"), 38, TextAnchor.MiddleCenter, new Vector2(0f, 0.86f), new Vector2(1f, 0.93f), Vector2.zero, Vector2.zero);
            title.color = Color.white; title.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(title, new Color(0f, 0f, 0f, 0.6f), 2f);
            int cols = got.Count > 1 ? 3 : 1; float cw = got.Count > 1 ? 0.3f : 0.6f, ch = got.Count > 1 ? 0.155f : 0.3f;
            int best = 0; foreach (var g in got) best = Mathf.Max(best, (int)g.r);
            for (int i = 0; i < got.Count; i++)
            {
                var g = got[i]; int cx = i % cols, cy = i / cols;
                float x0 = got.Count > 1 ? 0.04f + cx * 0.32f : 0.2f, y1 = got.Count > 1 ? 0.84f - cy * (ch + 0.01f) : 0.7f;
                var card = CoastUiArt.CutePill(root, "Cap", Color.Lerp(Color.white, RarCol[(int)g.r], 0.35f), 18, 4); card.raycastTarget = false;
                var rt = card.rectTransform; rt.anchorMin = new Vector2(x0, y1 - ch); rt.anchorMax = new Vector2(x0 + cw, y1); rt.offsetMin = rt.offsetMax = Vector2.zero;
                var d = LifeItems.Get(g.id);
                var tx = CoastHudLayout.MakeText(rt, "N", $"<b>{RarKo[(int)g.r]}</b>\n{(d.HasValue ? LifeItems.Name(d.Value) : g.id)}", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));
                tx.color = EventCardKit.BrownInk; tx.supportRichText = true; tx.horizontalOverflow = HorizontalWrapMode.Wrap; tx.resizeTextForBestFit = true; tx.resizeTextMinSize = 12; tx.resizeTextMaxSize = CoastHudLayout.Scaled(22);
                rt.localScale = Vector3.zero;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f) { rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t) * (1f + 0.15f * Mathf.Sin(t * Mathf.PI)); yield return null; }
                rt.localScale = Vector3.one;
                if ((int)g.r >= (int)Rar.Rare) CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            }
            if (best >= (int)Rar.Epic) VillagePang.Burst(_player.position + Vector3.up * 1.4f, RarCol[best], Color.white, 1.6f);
            bool ok = false;
            var btn = CoastUiArt.GlossyPill(root, "Ok", new Color(1f, 0.55f, 0.65f), 22, 8); btn.raycastTarget = true;
            var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.08f); brt.sizeDelta = new Vector2(260f, 72f);
            var bt = CoastHudLayout.MakeText(brt, "T", Loc.T("가방에 넣기", "Into the bag"), 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
            var b = btn.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => { CoastPrefs.Vibrate(); ok = true; });
            float w = 0f; while (!ok) { w += Time.deltaTime; if (DevAutoBubble && w > 1.5f) ok = true; yield return null; }
            Destroy(cv.gameObject); _busy = false;
            CoastToast.Pop(Loc.T($"캡슐 {got.Count}개 → 가방", $"{got.Count} capsules → bag"));
        }

        // ── 개발용 ────────────────────────────────────────────────────────
        public void DevGacha(int n) { JellyWallet.Add(n >= 10 ? GachaCost10 : GachaCost); Pull(n); }
        public void DevWorkshop() { if (Save == null) return; Save.stats.money += 5000; foreach (var r in Recipes) foreach (var m in r.mats) LifeItems.Add(Save, m.id, m.n); _gm.Persist(); WorkshopMenu(); }
        public void DevStoryManShow() { if (VillageStoryMan.I != null) VillageStoryMan.I.DevShow(); }
        public void DevFish() { _rod = true; _bat = _axe = _pick = _road = false; ApplyTool(); OpenFishing(); }
        public void DevToolLog() { var sb = new System.Text.StringBuilder("[Tools] "); for (int t = 0; t < ToolCount; t++) sb.Append(ToolLabel(t) + " "); sb.Append($" pool={Pool().Count} jelly={JellyWallet.Total}"); Debug.LogWarning(sb.ToString()); }
    }
}
