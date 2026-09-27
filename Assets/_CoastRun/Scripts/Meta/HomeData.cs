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

    /// 56차(사용자): 텃밭 작물 — 키우는 기간 2~3주(주가 바뀌면 자란다), 다 자라 수확할 때 **성공 확률**을 굴린다.
    ///   식품 가치가 다 다르다(반찬 몇 주분 / 쌀 몇 주분), 장미는 수확하면 스트레스 0.
    public class SeedDef
    {
        public string id, ko, en;
        public int price;
        public int weeks;                 // 다 자라는 데 걸리는 주
        public float chance;              // 수확 성공 확률(0~1)
        public int food;                  // 성공 시 반찬 주분
        public int rice;                  // 성공 시 쌀 주분
        public bool rose;                 // 성공 시 스트레스 -25(꽃)
        public Color petal, center;
        public string emoji;              // 텃밭 말풍선·카드 아이콘
        public string growHintKo, growHintEn; // "성장 2-3분" 식 표기
        public int season = -1;           // 205차: 심을 수 있는 계절(0 봄 1 여름 2 가을 3 겨울, -1 사계절)
        public int sell;                  // 205차: 계절 작물 판매가(G, 한 개)
        public string Name => Loc.T(ko, en);
        public int waters => weeks;       // 구 코드 호환(성장 단계 수)
        public bool Edible => food > 0 || rice > 0;
        public string RewardText => rose ? Loc.T("스트레스 -25", "Stress -25") : rice > 0 ? Loc.T($"쌀 {rice}주분", $"Rice ×{rice}w") : Loc.T($"반찬 {food}주분", $"Side ×{food}w");
        public string GrowHint => Loc.T(growHintKo ?? "", growHintEn ?? "");
        public string Emoji => string.IsNullOrEmpty(emoji) ? "🌱" : emoji;
    }

    public static class HomeData
    {
        public const int PotCount = 3;
        public const int MiniGameRewardPerWeek = 3;
        /// 74차: 놀이 탭은 「스트레스를 푼다」고 써 있었지만 실제로는 돈만 줬다 → 보상 1회당 스트레스 -3(주 최대 -9).
        public const int MiniGameStressRelief = 3;

        // ── 가구(방 꾸미기 v2에서 추가된 것; 장식 12종은 RoomDeco.All) ──
        public static readonly DecoDef[] Furniture =
        {
            new DecoDef { id = "bed",       ko = "침대",       en = "Bed",        slot = DecoSlot.FloorR, price = 1250, tag = "寢", color = new Color(0.95f, 0.75f, 0.80f), blurbKo = "폭신한 이불. 주말엔 여기서 안 나온다.",  blurbEn = "Fluffy blanket. Weekend HQ." },
            new DecoDef { id = "desk",      ko = "책상",       en = "Desk",       slot = DecoSlot.FloorL, price = 1100, tag = "机", color = new Color(0.80f, 0.62f, 0.45f), blurbKo = "라디오 사연을 쓰는 자리.",              blurbEn = "Where the radio letters get written." },
            new DecoDef { id = "sofa",      ko = "소파",       en = "Sofa",       slot = DecoSlot.FloorR, price = 1500, tag = "沙", color = new Color(0.55f, 0.70f, 0.85f), blurbKo = "셋이 앉으면 딱 맞는 크기.",             blurbEn = "Seats exactly three." },
            new DecoDef { id = "treadmill", ko = "러닝머신",   en = "Treadmill",  slot = DecoSlot.FloorL, price = 2000, tag = "走", color = new Color(0.45f, 0.48f, 0.55f), blurbKo = "탭하면 운동: 체력 +2 (페이즈마다 1번).",  blurbEn = "Tap to train: stamina +2 (once per phase)." },
            new DecoDef { id = "shelf",     ko = "책장",       en = "Bookshelf",  slot = DecoSlot.WallL,  price = 900, tag = "架", color = new Color(0.70f, 0.50f, 0.35f), blurbKo = "앨범과 포토카드가 꽂혀 있다.",          blurbEn = "Albums and photocards live here." },
            new DecoDef { id = "window",    ko = "창가 커튼",  en = "Curtains",   slot = DecoSlot.WallR,  price = 700, tag = "帘", color = new Color(0.98f, 0.85f, 0.60f), blurbKo = "노을이 들어오면 방이 주황색.",          blurbEn = "The sunset turns the room orange." },
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
        public static bool IsActive(DecoDef d) => RoomDeco.IsActive(d);
        public static int ActiveCount
        {
            get { int n = 0; foreach (var d in All) if (IsActive(d)) n++; return n; }
        }

        // 보유 비트: RoomDeco 12종은 decoOwnedMask(하위 비트), 가구 6종은 decoOwnedMask의 12번 비트부터.
        public static bool Owns(MetaProfile p, DecoDef d) { int i = IndexOf(d.id); return p != null && i >= 0 && (p.decoOwnedMask & (1 << i)) != 0; }
        public static bool IsNew(MetaProfile p, DecoDef d) { int i = IndexOf(d.id); return p != null && i >= 0 && (p.decoNewMask & (1 << i)) != 0; }
        public static void Grant(MetaProfile p, DecoDef d, bool markNew) { int i = IndexOf(d.id); if (p == null || i < 0 || !IsActive(d)) return; p.decoOwnedMask |= 1 << i; if (markNew) p.decoNewMask |= 1 << i; }
        public static bool TryBuy(SaveData s, MetaProfile p, DecoDef d)
        {
            if (s == null || p == null || d == null || !IsActive(d) || d.FromRun || Owns(p, d) || s.stats.money < d.price) return false;
            s.stats.money -= d.price; Grant(p, d, false); return true;
        }
        public static int OwnedCount(MetaProfile p) { int n = 0; foreach (var d in All) if (IsActive(d) && Owns(p, d)) n++; return n; }

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
            // 방 그림에 이미 있는 장식(retired)은 방에서·슬롯에서 걷어 낸다.
            if (p.homeItems.Length > 0)
            {
                var kept = new List<HomeItem>(p.homeItems.Length);
                foreach (var h in p.homeItems)
                {
                    var d = Find(h.id);
                    if (d == null || !IsActive(d)) continue;
                    kept.Add(h);
                }
                if (kept.Count != p.homeItems.Length) p.homeItems = kept.ToArray();
            }
        }
        public static HomeItem Placed(MetaProfile p, string id) { Ensure(p); foreach (var h in p.homeItems) if (h.id == id) return h; return null; }
        public static bool IsPlaced(MetaProfile p, DecoDef d) => d != null && IsActive(d) && Placed(p, d.id) != null;
        public static int PlacedCount(MetaProfile p) { Ensure(p); int n = 0; foreach (var h in p.homeItems) { var d = Find(h.id); if (IsActive(d)) n++; } return n; }
        public static HomeItem Place(MetaProfile p, DecoDef d, float x, float y)
        {
            if (p == null || d == null || !IsActive(d) || !Owns(p, d)) return null;
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
        public static bool IsComplete(MetaProfile p) => PlacedCount(p) >= ActiveCount;
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
            s.stats.stamina += 2; s.stats.stress += 2; s.stats.Clamp();
            return true;
        }

        // ── 베란다 화분 ──
        // 213차(사용자: 「단점들 커버해줘」 — 212차 평가: 씨값 > 수확 가치라 심을수록 손해): 씨값 ≈ 판매가 × 수확량 × 0.55 로 내림.
        //   ★1 이면 조금 남고 ★3(비료 + 물 안 빼먹기)이면 씨값의 2.5~3배. 판매가(VillageLand.Prices)는 그대로.
        public static readonly SeedDef[] Seeds =
        {
            new SeedDef { id = "tomato",   ko = "토마토",  en = "Tomato",   price = 170, weeks = 2, chance = 0.70f, food = 2, petal = new Color(0.95f, 0.25f, 0.20f), center = new Color(0.40f, 0.70f, 0.35f), emoji = "🍅", growHintKo = "성장 2-3분", growHintEn = "grow 2-3m" },
            new SeedDef { id = "potato",   ko = "감자",    en = "Potato",   price = 190, weeks = 3, chance = 0.80f, food = 3, petal = new Color(0.82f, 0.66f, 0.38f), center = new Color(0.45f, 0.70f, 0.35f), emoji = "🥔", growHintKo = "성장 3-4분", growHintEn = "grow 3-4m" },
            new SeedDef { id = "rice",     ko = "벼",      en = "Rice",     price = 300, weeks = 3, chance = 0.50f, rice = 2, petal = new Color(0.90f, 0.80f, 0.35f), center = new Color(0.55f, 0.75f, 0.30f), emoji = "🌾", growHintKo = "성장 2-3분", growHintEn = "grow 2-3m" },
            new SeedDef { id = "rose",     ko = "장미",    en = "Rose",     price = 300, weeks = 2, chance = 0.60f, rose = true, petal = new Color(0.98f, 0.45f, 0.62f), center = new Color(0.35f, 0.60f, 0.30f), emoji = "🌹", growHintKo = "성장 1-2분", growHintEn = "grow 1-2m" },
            new SeedDef { id = "lavender", ko = "라벤더",  en = "Lavender", price = 250, weeks = 2, chance = 0.40f, food = 1, petal = new Color(0.72f, 0.55f, 0.92f), center = new Color(0.40f, 0.68f, 0.40f), emoji = "💜", growHintKo = "성장 1-2분", growHintEn = "grow 1-2m" },
        };
        // 205차(사용자: 「농사 확장 — 계절 씨앗」): 마을 텃밭 전용 계절 작물(베란다 화분 목록은 그대로). 수확물 = crop_<id>.
        static Color C(float r, float g, float b) => new Color(r, g, b);
        public static readonly SeedDef[] SeasonSeeds =
        {
            new SeedDef { id = "strawberry", ko = "딸기", en = "Strawberry", price = 150, weeks = 2, chance = 1f, food = 2, season = 0, sell = 140, petal = C(0.95f, 0.22f, 0.30f), center = C(0.40f, 0.70f, 0.35f), emoji = "🍓" },
            new SeedDef { id = "cabbage", ko = "양배추", en = "Cabbage", price = 150, weeks = 2, chance = 1f, food = 3, season = 0, sell = 90, petal = C(0.70f, 0.90f, 0.55f), center = C(0.45f, 0.72f, 0.35f), emoji = "🥬" },
            new SeedDef { id = "pea", ko = "완두콩", en = "Pea", price = 30, weeks = 1, chance = 1f, food = 1, season = 0, sell = 55, petal = C(0.55f, 0.85f, 0.40f), center = C(0.40f, 0.70f, 0.35f), emoji = "🫛" },
            new SeedDef { id = "corn", ko = "옥수수", en = "Corn", price = 120, weeks = 2, chance = 1f, food = 2, season = 1, sell = 110, petal = C(1.00f, 0.85f, 0.25f), center = C(0.45f, 0.70f, 0.30f), emoji = "🌽" },
            new SeedDef { id = "watermelon", ko = "수박", en = "Watermelon", price = 400, weeks = 3, chance = 1f, food = 3, season = 1, sell = 260, petal = C(0.25f, 0.60f, 0.25f), center = C(0.45f, 0.72f, 0.35f), emoji = "🍉" },
            new SeedDef { id = "pepper", ko = "고추", en = "Chili", price = 35, weeks = 1, chance = 1f, food = 1, season = 1, sell = 60, petal = C(0.90f, 0.15f, 0.12f), center = C(0.40f, 0.68f, 0.30f), emoji = "🌶" },
            new SeedDef { id = "sweetpotato", ko = "고구마", en = "Sweet potato", price = 190, weeks = 2, chance = 1f, food = 3, season = 2, sell = 115, petal = C(0.70f, 0.30f, 0.45f), center = C(0.45f, 0.70f, 0.35f), emoji = "🍠" },
            new SeedDef { id = "pumpkin", ko = "호박", en = "Pumpkin", price = 380, weeks = 3, chance = 1f, food = 3, season = 2, sell = 240, petal = C(1.00f, 0.55f, 0.15f), center = C(0.45f, 0.70f, 0.30f), emoji = "🎃" },
            new SeedDef { id = "carrot", ko = "구좌 당근", en = "Carrot", price = 35, weeks = 1, chance = 1f, food = 1, season = 2, sell = 65, petal = C(1.00f, 0.50f, 0.12f), center = C(0.40f, 0.72f, 0.35f), emoji = "🥕" },
            new SeedDef { id = "radish", ko = "월동무", en = "Winter radish", price = 90, weeks = 2, chance = 1f, food = 2, season = 3, sell = 85, petal = C(0.95f, 0.96f, 0.92f), center = C(0.45f, 0.72f, 0.35f), emoji = "●" },
            new SeedDef { id = "broccoli", ko = "브로콜리", en = "Broccoli", price = 130, weeks = 2, chance = 1f, food = 2, season = 3, sell = 120, petal = C(0.25f, 0.55f, 0.25f), center = C(0.40f, 0.68f, 0.30f), emoji = "🥦" },
            new SeedDef { id = "spinach", ko = "시금치", en = "Spinach", price = 30, weeks = 1, chance = 1f, food = 1, season = 3, sell = 50, petal = C(0.30f, 0.60f, 0.25f), center = C(0.40f, 0.68f, 0.30f), emoji = "●" },
        };
        public static SeedDef Seed(string id) { foreach (var s in Seeds) if (s.id == id) return s; foreach (var s in SeasonSeeds) if (s.id == id) return s; return null; }
        public static void EnsurePots(SaveData s)
        {
            if (s == null) return;
            if (s.pots == null || s.pots.Length != PotCount)
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
        /// 56차: 수확 — 성공 확률을 굴려 성공이면 보상(반찬/쌀/꽃은 스트레스↓), 실패면 시든다. 어느 쪽이든 화분은 비운다.
        public static bool Harvest(SaveData s, int pot, out SeedDef seed)
        {
            seed = null;
            if (!IsBloomed(s, pot)) return false;
            var p = s.pots[pot]; seed = Seed(p.seed);
            p.seed = null; p.growth = 0; p.waterStamp = -1;
            bool ok = UnityEngine.Random.value < seed.chance;
            if (ok)
            {
                if (seed.rice > 0) LifeItems.Add(s, "ing_rice", seed.rice);
                if (seed.food > 0) LifeItems.Add(s, "ing_veg", seed.food);
                // 74차: 꽃 한 송이로 스트레스를 0 으로 지우던 건 너무 셌다(관리가 무의미) → -25.
                if (seed.rose) { s.stats.stress = Mathf.Max(0, s.stats.stress - 25); s.stats.Clamp(); }
                s.flowersSold++;
                LifeItems.SyncLegacy(s);
            }
            return ok;
        }
        /// 구 호출 호환 — 수확(성공이면 반찬/쌀 주분 합, 실패 0).
        public static int Sell(SaveData s, int pot) { return Harvest(s, pot, out var sd) ? (sd.food + sd.rice) : 0; }
        /// 55차: 주가 바뀔 때 — 심어 둔 화분이 한 주 자란다(수확은 텃밭에서 직접 — 성공 확률을 굴린다).
        public static int WeeklyGrow(SaveData s)
        {
            EnsurePots(s); int ripe = 0;
            for (int i = 0; i < s.pots.Length; i++)
            {
                var p = s.pots[i]; var sd = Seed(p.seed);
                if (sd == null) continue;
                if (p.growth < sd.weeks) p.growth++;
                if (p.growth >= sd.weeks) ripe++;
            }
            return ripe;   // 수확할 수 있는 화분 수
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
            s.stats.stress = Mathf.Max(0, s.stats.stress - MiniGameStressRelief);
            return amount;
        }
    }
}
