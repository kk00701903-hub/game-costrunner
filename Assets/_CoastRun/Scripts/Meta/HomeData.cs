using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 30차: 집(방 꾸미기 v2) 데이터 — 가구 자유 배치·베란다 화분(씨앗→물주기→판매)·미니게임 보상 한도·러닝머신.
    /// 가구 카탈로그는 RoomDeco.All(장식 12종)에 가구 6종을 더한 것. 배치는 슬롯이 아니라 방 안 정규화 좌표(x,y).

    [Serializable]
    public class HomeItem { public string id; public float x; public float y; }

    [Serializable]
    public class PotState
    {
        public string seed;        // null/"" = 빈 화분
        public int growth;         // 물 준 횟수
        public int waterStamp;     // 마지막으로 물 준 (week*4+phase) — 한 페이즈에 한 번
    }

    public class SeedDef
    {
        public string id, ko, en;
        public int price, sell, waters;   // waters: 다 자라는 데 필요한 물주기 횟수
        public Color petal, center;
        public string Name => Loc.T(ko, en);
    }

    public static class HomeData
    {
        public const int PotCount = 4;
        public const int MiniGameRewardPerWeek = 3;

        // ── 가구(방 꾸미기 v2에서 추가된 것; 장식 12종은 RoomDeco.All) ──
        public static readonly DecoDef[] Furniture =
        {
            new DecoDef { id = "bed",       ko = "침대",       en = "Bed",        slot = DecoSlot.FloorR, price = 250, tag = "寢", color = new Color(0.95f, 0.75f, 0.80f), blurbKo = "폭신한 이불. 주말엔 여기서 안 나온다.",  blurbEn = "Fluffy blanket. Weekend HQ." },
            new DecoDef { id = "desk",      ko = "책상",       en = "Desk",       slot = DecoSlot.FloorL, price = 220, tag = "机", color = new Color(0.80f, 0.62f, 0.45f), blurbKo = "라디오 사연을 쓰는 자리.",              blurbEn = "Where the radio letters get written." },
            new DecoDef { id = "sofa",      ko = "소파",       en = "Sofa",       slot = DecoSlot.FloorR, price = 300, tag = "沙", color = new Color(0.55f, 0.70f, 0.85f), blurbKo = "셋이 앉으면 딱 맞는 크기.",             blurbEn = "Seats exactly three." },
            new DecoDef { id = "treadmill", ko = "러닝머신",   en = "Treadmill",  slot = DecoSlot.FloorL, price = 400, tag = "走", color = new Color(0.45f, 0.48f, 0.55f), blurbKo = "탭하면 운동: 체력 +2 (페이즈마다 1번).",  blurbEn = "Tap to train: stamina +2 (once per phase)." },
            new DecoDef { id = "shelf",     ko = "책장",       en = "Bookshelf",  slot = DecoSlot.WallL,  price = 180, tag = "架", color = new Color(0.70f, 0.50f, 0.35f), blurbKo = "앨범과 포토카드가 꽂혀 있다.",          blurbEn = "Albums and photocards live here." },
            new DecoDef { id = "window",    ko = "창가 커튼",  en = "Curtains",   slot = DecoSlot.WallR,  price = 140, tag = "帘", color = new Color(0.98f, 0.85f, 0.60f), blurbKo = "노을이 들어오면 방이 주황색.",          blurbEn = "The sunset turns the room orange." },
        };

        private static DecoDef[] _all;
        /// 장식 12 + 가구 6.
        public static DecoDef[] All
        {
            get
            {
                if (_all == null)
                {
                    var l = new List<DecoDef>(RoomDeco.All); l.AddRange(Furniture); _all = l.ToArray();
                }
                return _all;
            }
        }
        public static DecoDef Find(string id) { foreach (var d in All) if (d.id == id) return d; return null; }
        public static bool IsWall(DecoDef d) => d.slot == DecoSlot.WallL || d.slot == DecoSlot.WallR;
        public static int IndexOf(string id) { for (int i = 0; i < All.Length; i++) if (All[i].id == id) return i; return -1; }

        // 보유 비트: RoomDeco 12종은 decoOwnedMask(하위 비트), 가구 6종은 decoOwnedMask의 12번 비트부터.
        public static bool Owns(MetaProfile p, DecoDef d) { int i = IndexOf(d.id); return p != null && i >= 0 && (p.decoOwnedMask & (1 << i)) != 0; }
        public static bool IsNew(MetaProfile p, DecoDef d) { int i = IndexOf(d.id); return p != null && i >= 0 && (p.decoNewMask & (1 << i)) != 0; }
        public static void Grant(MetaProfile p, DecoDef d, bool markNew) { int i = IndexOf(d.id); if (p == null || i < 0) return; p.decoOwnedMask |= 1 << i; if (markNew) p.decoNewMask |= 1 << i; }
        public static bool TryBuy(SaveData s, MetaProfile p, DecoDef d)
        {
            if (s == null || p == null || d == null || d.FromRun || Owns(p, d) || s.stats.money < d.price) return false;
            s.stats.money -= d.price; Grant(p, d, false); return true;
        }
        public static int OwnedCount(MetaProfile p) { int n = 0; foreach (var d in All) if (Owns(p, d)) n++; return n; }

        // ── 배치 ──
        public static void Ensure(MetaProfile p)
        {
            if (p == null) return;
            if (p.homeItems == null) p.homeItems = new HomeItem[0];
            RoomDeco.Ensure(p);
            // 28차 슬롯 배치 → 좌표로 옮긴다(한 번).
            bool any = false;
            for (int i = 0; i < p.roomSlots.Length; i++) if (!string.IsNullOrEmpty(p.roomSlots[i])) any = true;
            if (any)
            {
                var l = new List<HomeItem>(p.homeItems);
                for (int i = 0; i < p.roomSlots.Length; i++)
                {
                    var id = p.roomSlots[i]; if (string.IsNullOrEmpty(id)) continue;
                    RoomDeco.SlotLayout((DecoSlot)i, out var a, out _);
                    if (l.Find(h => h.id == id) == null) l.Add(new HomeItem { id = id, x = a.x, y = a.y });
                    p.roomSlots[i] = null;
                }
                p.homeItems = l.ToArray();
            }
        }
        public static HomeItem Placed(MetaProfile p, string id) { Ensure(p); foreach (var h in p.homeItems) if (h.id == id) return h; return null; }
        public static bool IsPlaced(MetaProfile p, DecoDef d) => d != null && Placed(p, d.id) != null;
        public static int PlacedCount(MetaProfile p) { Ensure(p); return p.homeItems.Length; }
        public static HomeItem Place(MetaProfile p, DecoDef d, float x, float y)
        {
            if (p == null || d == null || !Owns(p, d)) return null;
            Ensure(p);
            var h = Placed(p, d.id);
            if (h == null) { h = new HomeItem { id = d.id }; var l = new List<HomeItem>(p.homeItems) { h }; p.homeItems = l.ToArray(); }
            h.x = Mathf.Clamp01(x); h.y = Mathf.Clamp01(y);
            int i = IndexOf(d.id); if (i >= 0) p.decoNewMask &= ~(1 << i);
            return h;
        }
        public static void Remove(MetaProfile p, string id)
        {
            Ensure(p);
            var l = new List<HomeItem>(p.homeItems); l.RemoveAll(h => h.id == id); p.homeItems = l.ToArray();
        }
        /// 31차(Dreamy Room 오마주): 장식마다 '제자리'가 있다. [놓기]하면 제자리로 날아가 팡 하고 놓이고, 끌다가 제자리 근처에 놓으면 스냅.
        public static Vector2 Spot(string id)
        {
            switch (id)
            {
                case "bed": return new Vector2(0.24f, 0.10f);
                case "rug": return new Vector2(0.50f, 0.04f);
                case "sofa": return new Vector2(0.74f, 0.12f);
                case "desk": return new Vector2(0.20f, 0.30f);
                case "treadmill": return new Vector2(0.80f, 0.30f);
                case "plant": return new Vector2(0.08f, 0.08f);
                case "harubang": return new Vector2(0.12f, 0.22f);
                case "surf": return new Vector2(0.92f, 0.10f);
                case "lamp": return new Vector2(0.40f, 0.36f);
                case "books": return new Vector2(0.60f, 0.36f);
                case "tangerine": return new Vector2(0.88f, 0.22f);
                case "haenyeo": return new Vector2(0.34f, 0.24f);
                case "clock": return new Vector2(0.12f, 0.72f);
                case "poster": return new Vector2(0.88f, 0.70f);
                case "stars": return new Vector2(0.50f, 0.84f);
                case "lighthouse": return new Vector2(0.24f, 0.56f);
                case "shelf": return new Vector2(0.76f, 0.52f);
                case "window": return new Vector2(0.50f, 0.62f);
                default: return new Vector2(0.5f, 0.2f);
            }
        }
        public const float SnapRadius = 0.11f;
        public static bool NearSpot(string id, Vector2 pos) => Vector2.Distance(Spot(id), pos) <= SnapRadius;
        public static bool IsComplete(MetaProfile p) => PlacedCount(p) >= All.Length;
        public const int CompleteReward = 300;

        /// 바닥 가구는 y 0.02~0.42, 벽걸이는 y 0.45~0.85 안에만.
        public static Vector2 ClampPos(DecoDef d, Vector2 v)
        {
            float x = Mathf.Clamp(v.x, 0.06f, 0.94f);
            float y = IsWall(d) ? Mathf.Clamp(v.y, 0.45f, 0.85f) : Mathf.Clamp(v.y, 0.02f, 0.42f);
            return new Vector2(x, y);
        }
        /// 방 안 표시 크기(720 기준 px).
        public static Vector2 Size(DecoDef d)
        {
            switch (d.id)
            {
                case "bed": return new Vector2(210f, 120f);
                case "sofa": return new Vector2(190f, 110f);
                case "desk": return new Vector2(170f, 120f);
                case "treadmill": return new Vector2(150f, 130f);
                case "shelf": return new Vector2(120f, 160f);
                case "window": return new Vector2(150f, 140f);
                case "rug": return new Vector2(180f, 70f);
                case "plant": case "harubang": case "surf": return new Vector2(120f, 130f);
                default: return IsWall(d) ? new Vector2(100f, 100f) : new Vector2(100f, 100f);
            }
        }

        // ── 러닝머신 ──
        public static int Stamp(SaveData s) => s == null ? 0 : s.week * 4 + s.phaseIndex;
        public static bool TreadmillReady(SaveData s, MetaProfile p) => s != null && Placed(p, "treadmill") != null && s.treadmillStamp != Stamp(s);
        public static bool UseTreadmill(SaveData s, MetaProfile p)
        {
            if (!TreadmillReady(s, p)) return false;
            s.treadmillStamp = Stamp(s);
            s.stats.stamina += 2; s.stats.stress += 1; s.stats.Clamp();
            return true;
        }

        // ── 베란다 화분 ──
        public static readonly SeedDef[] Seeds =
        {
            new SeedDef { id = "rape",      ko = "유채 씨앗",    en = "Rapeseed",   price = 20, sell = 55,  waters = 3, petal = new Color(1f, 0.90f, 0.25f), center = new Color(0.55f, 0.40f, 0.10f) },
            new SeedDef { id = "cosmos",    ko = "코스모스 씨앗", en = "Cosmos",     price = 40, sell = 120, waters = 5, petal = new Color(1f, 0.55f, 0.75f), center = new Color(1f, 0.85f, 0.30f) },
            new SeedDef { id = "hydrangea", ko = "수국 씨앗",    en = "Hydrangea",  price = 70, sell = 220, waters = 7, petal = new Color(0.55f, 0.65f, 0.95f), center = new Color(0.85f, 0.90f, 1f) },
            new SeedDef { id = "sunflower", ko = "해바라기 씨앗", en = "Sunflower",  price = 90, sell = 320, waters = 9, petal = new Color(1f, 0.78f, 0.15f), center = new Color(0.35f, 0.22f, 0.10f) },
        };
        public static SeedDef Seed(string id) { foreach (var s in Seeds) if (s.id == id) return s; return null; }
        public static void EnsurePots(SaveData s)
        {
            if (s == null) return;
            if (s.pots == null || s.pots.Length < PotCount)
            {
                var n = new PotState[PotCount];
                for (int i = 0; i < n.Length; i++) n[i] = s.pots != null && i < s.pots.Length && s.pots[i] != null ? s.pots[i] : new PotState();
                s.pots = n;
            }
            for (int i = 0; i < s.pots.Length; i++) if (s.pots[i] == null) s.pots[i] = new PotState();
        }
        public static bool Plant(SaveData s, int pot, SeedDef seed)
        {
            EnsurePots(s);
            var p = s.pots[pot];
            if (!string.IsNullOrEmpty(p.seed) || seed == null || s.stats.money < seed.price) return false;
            s.stats.money -= seed.price; p.seed = seed.id; p.growth = 0; p.waterStamp = -1;
            return true;
        }
        public static bool CanWater(SaveData s, int pot)
        {
            EnsurePots(s); var p = s.pots[pot]; var sd = Seed(p.seed);
            return sd != null && p.growth < sd.waters && p.waterStamp != Stamp(s);
        }
        public static bool Water(SaveData s, int pot)
        {
            if (!CanWater(s, pot)) return false;
            var p = s.pots[pot]; p.growth++; p.waterStamp = Stamp(s); return true;
        }
        public static bool IsBloomed(SaveData s, int pot) { EnsurePots(s); var p = s.pots[pot]; var sd = Seed(p.seed); return sd != null && p.growth >= sd.waters; }
        public static int Sell(SaveData s, int pot)
        {
            if (!IsBloomed(s, pot)) return 0;
            var p = s.pots[pot]; var sd = Seed(p.seed);
            s.stats.money += sd.sell; p.seed = null; p.growth = 0; p.waterStamp = -1;
            s.flowersSold++;
            return sd.sell;
        }
        /// 0 빈 화분 / 1 씨앗 / 2 새싹 / 3 줄기 / 4 봉오리 / 5 꽃
        public static int Stage(SaveData s, int pot)
        {
            EnsurePots(s); var p = s.pots[pot]; var sd = Seed(p.seed);
            if (sd == null) return 0;
            if (p.growth >= sd.waters) return 5;
            float t = p.growth / (float)sd.waters;
            return t < 0.2f ? 1 : t < 0.5f ? 2 : t < 0.8f ? 3 : 4;
        }

        // ── 미니게임 보상 한도(주 3회) ──
        public static int RewardPlaysLeft(SaveData s)
        {
            if (s == null) return 0;
            if (s.miniGameWeek != s.week) { s.miniGameWeek = s.week; s.miniGamePlays = 0; }
            return Mathf.Max(0, MiniGameRewardPerWeek - s.miniGamePlays);
        }
        /// 보상 지급: 남은 횟수가 있으면 전액, 없으면 0(연습).
        public static int GiveReward(SaveData s, int amount)
        {
            if (s == null || amount <= 0) return 0;
            if (RewardPlaysLeft(s) <= 0) return 0;
            s.miniGamePlays++;
            s.stats.money += amount;
            return amount;
        }
    }
}
