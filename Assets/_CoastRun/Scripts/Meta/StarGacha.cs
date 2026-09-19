using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 131차(사용자 「스토리모드 재미요소 — 이벤트 가챠」): **별빛 캡슐** — 돈이 안 드는 이벤트 뽑기.
    ///   재화 = 별조각(★): 스토리 러닝 끝(완주·실패 모두)에 성과만큼 + 러닝 중 「럭키 캡슐」 아이템(뽑기권) + 하루 1회 무료.
    ///   등급 N/R/SR/SSR, 9회 동안 SR 이상이 없으면 10회째 SR 확정(천장). 보상은 즉시 적용된다.
    ///   구매 가챠가 아니다 — 실제 결제 없음.
    public enum GachaRarity { N = 0, R = 1, SR = 2, SSR = 3 }

    public class GachaReward
    {
        public GachaRarity rarity;
        public string title;      // 「코인 120」
        public string detail;     // 「지갑에 바로 들어갔어」
        public string icon;       // Icon_*(CoastUiArt.Icon) 이름 — 없으면 색 원
        public string art;        // UI 그림(CoastUiArt.Art) 이름 — 포토카드 등
        public bool isNew;        // 새 카드·새 장식
        public Color Color => StarGacha.RarityColor(rarity);
    }

    public static class StarGacha
    {
        public const int PullCost = 5;
        public const int TenCost = 45;
        public const int PityAt = 10;        // 10회째 SR 이상 확정
        public const int ShardCapPerRun = 12;

        public static Color RarityColor(GachaRarity r) =>
            r == GachaRarity.SSR ? new Color(1f, 0.72f, 0.20f) :
            r == GachaRarity.SR ? new Color(0.80f, 0.45f, 1f) :
            r == GachaRarity.R ? new Color(0.35f, 0.70f, 1f) : new Color(0.70f, 0.78f, 0.72f);
        public static string RarityName(GachaRarity r) => r.ToString();

        static int Today => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        public static bool FreePullReady(MetaProfile p) => p != null && p.gachaFreeDate != Today;

        /// 스토리 러닝이 끝났을 때(완주·실패 모두) 별조각 지급. 반환: 지급량.
        public static int OnStoryRunEnd(SaveData s, StageRunStats st, bool cleared)
        {
            if (s == null) return 0;
            int n = 2 + (cleared ? 3 : 0);
            if (st != null)
            {
                n += st.NearMissCount / 6;
                n += st.BestCombo / 10;
                n += st.Hearts / 3;
                if (st.Flawless) n += 2;
            }
            n = Mathf.Clamp(n, 1, ShardCapPerRun);
            s.starShards += n;
            s.starShardsTotal += n;
            return n;
        }

        /// 러닝 중 럭키 캡슐을 먹었다 → 뽑기권 +1.
        public static void OnCapsulePicked(SaveData s)
        {
            if (s == null) return;
            s.capsuleTickets++;
        }

        public static bool CanPull(SaveData s, MetaProfile p, int count, out string howKo)
        {
            howKo = null;
            if (s == null) return false;
            if (count == 1)
            {
                if (FreePullReady(p)) { howKo = "free"; return true; }
                if (s.capsuleTickets > 0) { howKo = "ticket"; return true; }
                if (s.starShards >= PullCost) { howKo = "shard"; return true; }
                return false;
            }
            if (s.starShards >= TenCost) { howKo = "shard"; return true; }
            return false;
        }

        /// 비용을 치른다. 1회: 무료 → 뽑기권 → 별조각 순.
        static bool Pay(SaveData s, MetaProfile p, int count)
        {
            if (count == 1)
            {
                if (FreePullReady(p)) { p.gachaFreeDate = Today; return true; }
                if (s.capsuleTickets > 0) { s.capsuleTickets--; return true; }
                if (s.starShards >= PullCost) { s.starShards -= PullCost; return true; }
                return false;
            }
            if (s.starShards >= TenCost) { s.starShards -= TenCost; return true; }
            return false;
        }

        /// 뽑기 실행. 보상은 즉시 세이브/프로필에 적용된다(호출측이 Persist/WriteProfileNow).
        /// payCount = 1(1회 비용) 또는 10(10회 비용), draws = 실제 뽑는 수(133차: 10+1 → 11).
        public static List<GachaReward> Pull(GameManager gm, int payCount, int draws = -1)
        {
            var list = new List<GachaReward>();
            if (gm == null || gm.Save == null) return list;
            var s = gm.Save; var p = gm.Profile;
            if (!Pay(s, p, payCount)) return list;
            if (draws < 1) draws = payCount;
            var rng = new System.Random(unchecked(Environment.TickCount * 7919 + s.gachaPulls * 104729 + s.seed));
            for (int i = 0; i < draws; i++)
            {
                var r = RollRarity(s, rng);
                s.gachaPulls++;
                if (r >= GachaRarity.SR) s.gachaPity = 0; else s.gachaPity++;
                list.Add(Grant(gm, r, rng));
            }
            return list;
        }

        static GachaRarity RollRarity(SaveData s, System.Random rng)
        {
            if (s.gachaPity >= PityAt - 1) return rng.NextDouble() < 0.15 ? GachaRarity.SSR : GachaRarity.SR;   // 천장
            double x = rng.NextDouble();
            if (x < 0.02) return GachaRarity.SSR;
            if (x < 0.12) return GachaRarity.SR;
            if (x < 0.42) return GachaRarity.R;
            return GachaRarity.N;
        }

        // ── 보상 풀 ──────────────────────────────────────────────────────
        static GachaReward Grant(GameManager gm, GachaRarity r, System.Random rng)
        {
            var s = gm.Save; var p = gm.Profile;
            var rw = new GachaReward { rarity = r };
            switch (r)
            {
                case GachaRarity.N:
                {
                    // 135차(사용자): 상품별 확률 차등 — 코인 50% · 재료 30% · 휴식 20%. 금액 크게 ↑
                    int k = Weighted(rng, 50, 30, 20);
                    if (k == 0)
                    {
                        int coins = 300 + rng.Next(6) * 100;
                        s.stats.money += coins;
                        rw.title = Loc.T($"코인 {coins}", $"{coins} coins"); rw.detail = Loc.T("지갑에 바로 들어갔어", "Added to your wallet"); rw.icon = "Icon_Coin";
                    }
                    else if (k == 1)
                    {
                        var d = RandomShopItem(rng, LifeItemCat.Ingredient);
                        if (d.HasValue) { LifeItems.Add(s, d.Value.id, 3); rw.title = Loc.T($"{LifeItems.Name(d.Value)} ×3", $"{LifeItems.Name(d.Value)} ×3"); rw.detail = Loc.T("가방에 넣어 뒀어 — 조리해서 먹자", "In your bag — cook it"); rw.icon = "Icon_Cart"; }
                        else { s.stats.money += 400; rw.title = Loc.T("코인 400", "400 coins"); rw.icon = "Icon_Coin"; }
                    }
                    else
                    {
                        s.stats.stress = Mathf.Max(0, s.stats.stress - 8);
                        rw.title = Loc.T("달콤한 휴식", "Sweet rest"); rw.detail = Loc.T("스트레스 −8", "Stress −8"); rw.icon = "Icon_Heart";
                    }
                    break;
                }
                case GachaRarity.R:
                {
                    // 하트 35% · 훈련 부적 40% · 방 장식 25%
                    int k = Weighted(rng, 35, 40, 25);
                    if (k == 0)
                    {
                        s.chapterHearts += 3;
                        rw.title = Loc.T("말랑이 하트 +3", "Hearts +3"); rw.detail = Loc.T("이번 챕터 하트에 더해졌어", "Added to this chapter"); rw.icon = "Icon_Heart";
                    }
                    else if (k == 1)
                    {
                        int which = rng.Next(3);
                        if (which == 0) { s.stats.stamina += 3; rw.title = Loc.T("훈련 부적 · 체력 +3", "Charm · Stamina +3"); }
                        else if (which == 1) { s.stats.agility += 3; rw.title = Loc.T("훈련 부적 · 순발력 +3", "Charm · Agility +3"); }
                        else { s.stats.charm += 3; rw.title = Loc.T("훈련 부적 · 매력 +3", "Charm · Charm +3"); }
                        rw.detail = Loc.T("스탯이 바로 올랐어", "Stat raised now"); rw.icon = "Icon_Bolt";
                    }
                    else
                    {
                        var d = RandomDeco(p, rng, shopOnly: true);
                        if (d != null) { RoomDeco.Grant(p, d, true); rw.title = Loc.T($"방 장식 · {d.Name}", $"Deco · {d.Name}"); rw.detail = Loc.T("마이룸에서 놓아 봐", "Place it in My Room"); rw.icon = "Icon_Home"; rw.isNew = true; }
                        else { s.capsuleTickets++; s.stats.money += 500; rw.title = Loc.T("뽑기권 +1 · 코인 500", "Ticket +1 · 500 coins"); rw.detail = Loc.T("장식은 다 모았어 — 한 번 더!", "All decos owned — one more pull!"); rw.icon = "Icon_Star"; }
                    }
                    break;
                }
                case GachaRarity.SR:
                {
                    // 포토카드 45% · 희귀 장식 30% · 행운의 부적 25%
                    int k = Weighted(rng, 45, 30, 25);
                    if (k == 0)
                    {
                        int id = RandomCard(rng, unownedFirst: true);
                        if (id > 0)
                        {
                            bool fresh = Collection.GiveCard(id, false);
                            var def = PhotocardTable.Get(id); var g = PhotocardTable.GradeOf(id);
                            rw.title = Loc.T($"포토카드 [{Collection.GradeName(g)}] {def.Name}", $"Photocard [{Collection.GradeName(g)}] {def.Name}"); rw.detail = fresh ? Loc.T("새 카드! 컬렉션에서 확인", "New card! See Collection") : Loc.T("이미 있던 카드 — 코인 +1,500", "Duplicate — +1,500 coins");
                            if (!fresh) s.stats.money += 1500;
                            rw.icon = "Icon_Card"; rw.isNew = fresh;
                        }
                        else { s.stats.money += 2000; rw.title = Loc.T("코인 2,000", "2,000 coins"); rw.icon = "Icon_Coin"; }
                    }
                    else if (k == 1)
                    {
                        var d = RandomDeco(p, rng, shopOnly: false);
                        if (d != null) { RoomDeco.Grant(p, d, true); rw.title = Loc.T($"희귀 장식 · {d.Name}", $"Rare deco · {d.Name}"); rw.detail = Loc.T("러닝에서만 나오던 장식이야", "A run-only deco"); rw.icon = "Icon_Home"; rw.isNew = true; }
                        else { s.stats.money += 2000; rw.title = Loc.T("코인 2,000", "2,000 coins"); rw.icon = "Icon_Coin"; }
                    }
                    else
                    {
                        s.luckyRunPending = true;
                        rw.title = Loc.T("행운의 부적", "Lucky charm"); rw.detail = Loc.T("다음 러닝 코인 ×2!", "Next run: coins ×2!"); rw.icon = "Icon_Bolt";
                    }
                    break;
                }
                default:
                {
                    // 사인 카드 40% · 별의 축복 35% · 코인 잭팟 25%
                    int k = Weighted(rng, 40, 35, 25);
                    if (k == 2)
                    {
                        int jackpot = 10000 + rng.Next(6) * 1000;
                        s.stats.money += jackpot;
                        rw.title = Loc.T($"코인 잭팟 {jackpot:N0}!", $"Coin jackpot {jackpot:N0}!"); rw.detail = Loc.T("별빛이 쏟아졌다 — 지갑에 바로!", "Starlight rain — straight to your wallet!"); rw.icon = "Icon_Coin";
                    }
                    else if (k == 0)
                    {
                        int id = RandomCard(rng, unownedFirst: false);
                        if (id > 0)
                        {
                            bool fresh = Collection.GiveCard(id, true);
                            var def = PhotocardTable.Get(id);
                            rw.title = Loc.T($"사인 포토카드 ✦ {def.Name}", $"Signed card ✦ {def.Name}"); rw.detail = fresh ? Loc.T("사인 버전! 컬렉션에서 확인", "Signed! See Collection") : Loc.T("이미 사인이 있던 카드 — 코인 +5,000", "Already signed — +5,000 coins");
                            if (!fresh) s.stats.money += 5000;
                            rw.icon = "Icon_Card"; rw.isNew = fresh;
                        }
                        else { s.stats.money += 8000; rw.title = Loc.T("코인 8,000", "8,000 coins"); rw.icon = "Icon_Coin"; }
                    }
                    else
                    {
                        s.stats.stamina += 6; s.stats.agility += 6; s.stats.charm += 6; s.stats.stress = Mathf.Max(0, s.stats.stress - 20);
                        rw.title = Loc.T("별의 축복", "Star blessing"); rw.detail = Loc.T("체력·순발력·매력 +6 · 스트레스 −20", "All stats +6 · Stress −20"); rw.icon = "Icon_Star";
                    }
                    break;
                }
            }
            s.stats.Clamp();
            if (string.IsNullOrEmpty(rw.detail)) rw.detail = "";
            return rw;
        }

        /// 135차: 가중 추첨 — 인덱스 반환(합이 100 이 아니어도 됨)
        static int Weighted(System.Random rng, params int[] w)
        {
            int sum = 0; foreach (var x in w) sum += x;
            int r = rng.Next(Mathf.Max(1, sum));
            for (int i = 0; i < w.Length; i++) { r -= w[i]; if (r < 0) return i; }
            return w.Length - 1;
        }

        static LifeItemDef? RandomShopItem(System.Random rng, LifeItemCat cat)
        {
            var pool = new List<LifeItemDef>();
            foreach (var d in LifeItems.All) if (d.shop && d.cat == cat && d.price > 0) pool.Add(d);
            if (pool.Count == 0) return null;
            return pool[rng.Next(pool.Count)];
        }

        static DecoDef RandomDeco(MetaProfile p, System.Random rng, bool shopOnly)
        {
            var pool = new List<DecoDef>();
            foreach (var d in RoomDeco.All)
            {
                if (d == null || d.retired) continue;
                if (shopOnly ? d.price <= 0 : d.price > 0) continue;
                if (RoomDeco.Owns(p, d)) continue;
                pool.Add(d);
            }
            if (pool.Count == 0) return null;
            return pool[rng.Next(pool.Count)];
        }

        static int RandomCard(System.Random rng, bool unownedFirst)
        {
            var pool = new List<int>();
            for (int id = 1; id <= PhotocardTable.Count; id++)
                if (!unownedFirst || !Collection.HasCard(id)) pool.Add(id);
            if (pool.Count == 0) for (int id = 1; id <= PhotocardTable.Count; id++) pool.Add(id);
            if (pool.Count == 0) return 0;
            return pool[rng.Next(pool.Count)];
        }
    }
}
