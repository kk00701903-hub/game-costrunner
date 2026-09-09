using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 28차: 방 꾸미기 — 장식 카탈로그·보유·배치. 보유/배치는 MetaProfile(회차를 넘어 남는다)에 저장.
    /// 장식은 두 경로로 얻는다: (a) 러닝 클리어/정산 때 확률 드롭(price 0), (b) 방 꾸미기 화면에서 G로 구매.
    /// 그림은 Resources/CoastRun/UI_Deco_<id>(마젠타 키 가능) 가 있으면 쓰고, 없으면 이름표 플레이스홀더.
    public enum DecoSlot { WallL = 0, WallR = 1, SillL = 2, SillR = 3, FloorL = 4, FloorR = 5 }

    public class DecoDef
    {
        public string id;
        public string ko, en;
        public string blurbKo, blurbEn;
        public DecoSlot slot;
        public int price;          // 0 = 러닝에서만 획득
        public string tag;         // 플레이스홀더용 짧은 글자(이모지 대신 한 글자)
        public Color color;
        public bool FromRun => price <= 0;
        public string Name => Loc.T(ko, en);
        public string Blurb => Loc.T(blurbKo, blurbEn);
    }

    public static class RoomDeco
    {
        public const int SlotCount = 6;

        public static readonly DecoDef[] All =
        {
            // ── 상점 구매 ──
            new DecoDef { id = "plant",    ko = "몬스테라 화분",   en = "Monstera Pot",   slot = DecoSlot.FloorL, price = 120, tag = "잎", color = new Color(0.45f, 0.72f, 0.42f), blurbKo = "창가 햇빛을 좋아하는 큰 잎.",        blurbEn = "Big leaves that love the window light." },
            new DecoDef { id = "clock",    ko = "벽시계",          en = "Wall Clock",     slot = DecoSlot.WallL,  price = 150, tag = "時", color = new Color(0.93f, 0.85f, 0.65f), blurbKo = "째깍째깍. 늦잠은 이제 그만.",          blurbEn = "Tick tock. No more oversleeping." },
            new DecoDef { id = "poster",   ko = "라디오 포스터",   en = "Radio Poster",   slot = DecoSlot.WallR,  price = 90,  tag = "♪",  color = new Color(0.98f, 0.62f, 0.55f), blurbKo = "DJ 사인이 들어간 방송국 포스터.",     blurbEn = "Station poster signed by the DJ." },
            new DecoDef { id = "rug",      ko = "줄무늬 러그",     en = "Striped Rug",    slot = DecoSlot.FloorR, price = 200, tag = "▤", color = new Color(0.80f, 0.55f, 0.45f), blurbKo = "맨발로 밟으면 폭신.",                 blurbEn = "Soft under bare feet." },
            new DecoDef { id = "lamp",     ko = "조개 램프",       en = "Shell Lamp",     slot = DecoSlot.SillL,  price = 180, tag = "☼", color = new Color(1.0f, 0.88f, 0.55f), blurbKo = "밤에 켜면 방이 노을빛.",              blurbEn = "Turns the room sunset-orange at night." },
            new DecoDef { id = "books",    ko = "책 더미",         en = "Book Stack",     slot = DecoSlot.SillR,  price = 80,  tag = "冊", color = new Color(0.60f, 0.70f, 0.90f), blurbKo = "읽다 만 책 세 권.",                   blurbEn = "Three half-read books." },
            // ── 러닝 드롭 ──
            new DecoDef { id = "harubang", ko = "미니 돌하르방",   en = "Mini Dol Hareubang", slot = DecoSlot.FloorL, price = 0, tag = "石", color = new Color(0.55f, 0.55f, 0.58f), blurbKo = "산책로에서 주운 기념품.",         blurbEn = "A souvenir picked up on the promenade." },
            new DecoDef { id = "tangerine",ko = "감귤 바구니",     en = "Tangerine Basket", slot = DecoSlot.SillR, price = 0, tag = "橘", color = new Color(1.0f, 0.65f, 0.25f),  blurbKo = "할머니 가게 앞에서 굴러온 감귤.",   blurbEn = "Tangerines that rolled out of Grandma's shop." },
            new DecoDef { id = "haenyeo",  ko = "해녀 인형",       en = "Haenyeo Doll",   slot = DecoSlot.SillL,  price = 0,   tag = "海", color = new Color(0.35f, 0.62f, 0.85f), blurbKo = "물안경까지 꼼꼼히 만든 인형.",        blurbEn = "Down to the tiny goggles." },
            new DecoDef { id = "lighthouse", ko = "등대 모형",     en = "Lighthouse Model", slot = DecoSlot.WallR, price = 0, tag = "燈", color = new Color(0.95f, 0.95f, 0.98f), blurbKo = "밤엔 진짜로 깜빡인다.",             blurbEn = "It actually blinks at night." },
            new DecoDef { id = "surf",     ko = "미니 서핑보드",   en = "Mini Surfboard", slot = DecoSlot.FloorR, price = 0,   tag = "波", color = new Color(0.40f, 0.80f, 0.75f), blurbKo = "파도 무늬가 그려진 장식판.",          blurbEn = "A little board painted with waves." },
            new DecoDef { id = "stars",    ko = "별 조명 줄",      en = "Star Lights",    slot = DecoSlot.WallL,  price = 0,   tag = "★", color = new Color(1.0f, 0.92f, 0.45f),  blurbKo = "송전탑 불빛을 닮은 작은 별들.",       blurbEn = "Tiny stars like the tower lights." },
        };

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].id == id) return i;
            return -1;
        }

        public static DecoDef Find(string id)
        {
            int i = IndexOf(id);
            return i >= 0 ? All[i] : null;
        }

        public static void Ensure(MetaProfile p)
        {
            if (p == null) return;
            if (p.roomSlots == null || p.roomSlots.Length < SlotCount)
            {
                var n = new string[SlotCount];
                if (p.roomSlots != null) for (int i = 0; i < p.roomSlots.Length && i < SlotCount; i++) n[i] = p.roomSlots[i];
                p.roomSlots = n;
            }
        }

        public static bool Owns(MetaProfile p, DecoDef d) => p != null && d != null && (p.decoOwnedMask & (1 << IndexOf(d.id))) != 0;
        public static bool IsNew(MetaProfile p, DecoDef d) => p != null && d != null && (p.decoNewMask & (1 << IndexOf(d.id))) != 0;
        public static bool AnyNew(MetaProfile p) => p != null && p.decoNewMask != 0;
        public static void ClearNew(MetaProfile p, DecoDef d) { if (p != null && d != null) p.decoNewMask &= ~(1 << IndexOf(d.id)); }
        public static void ClearAllNew(MetaProfile p) { if (p != null) p.decoNewMask = 0; }

        public static int OwnedCount(MetaProfile p)
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (Owns(p, All[i])) n++;
            return n;
        }

        public static void Grant(MetaProfile p, DecoDef d, bool markNew = true)
        {
            if (p == null || d == null) return;
            int b = 1 << IndexOf(d.id);
            p.decoOwnedMask |= b;
            if (markNew) p.decoNewMask |= b;
        }

        public static bool CanAfford(SaveData s, DecoDef d) => s != null && d != null && d.price > 0 && s.stats.money >= d.price;

        /// 구매: G 차감 + 보유. 이미 보유면 false.
        public static bool TryBuy(SaveData s, MetaProfile p, DecoDef d)
        {
            if (s == null || p == null || d == null || d.FromRun || Owns(p, d)) return false;
            if (s.stats.money < d.price) return false;
            s.stats.money -= d.price;
            Grant(p, d, markNew: false);
            return true;
        }

        public static DecoDef At(MetaProfile p, DecoSlot slot)
        {
            Ensure(p);
            if (p == null) return null;
            var id = p.roomSlots[(int)slot];
            return string.IsNullOrEmpty(id) ? null : Find(id);
        }

        public static bool IsPlaced(MetaProfile p, DecoDef d) => d != null && At(p, d.slot) == d;

        /// 배치: 그 자리에 있던 장식은 치워진다(보유는 유지).
        public static bool Place(MetaProfile p, DecoDef d)
        {
            if (p == null || d == null || !Owns(p, d)) return false;
            Ensure(p);
            p.roomSlots[(int)d.slot] = d.id;
            ClearNew(p, d);
            return true;
        }

        public static void Remove(MetaProfile p, DecoSlot slot)
        {
            Ensure(p);
            if (p != null) p.roomSlots[(int)slot] = null;
        }

        /// 러닝 정산 드롭: 아직 없는 '러닝 전용' 장식 중 하나를 확률로 준다. 반환 null = 없음.
        /// 스토리 런 클리어 45%, K-POP/무한 런 25%(거리 800m 이상일 때만).
        public static DecoDef LastDrop { get; private set; }
        public static DecoDef TryDropFromRun(MetaProfile p, int seed, float chance)
        {
            LastDrop = null;
            if (p == null) return null;
            var pool = new List<DecoDef>();
            foreach (var d in All) if (d.FromRun && !Owns(p, d)) pool.Add(d);
            if (pool.Count == 0) return null;
            var rng = new System.Random(seed);
            if (rng.NextDouble() > chance) return null;
            var pick = pool[rng.Next(pool.Count)];
            Grant(p, pick, markNew: true);
            LastDrop = pick;
            return pick;
        }

        /// 방 안의 앵커(정규화 0~1, 방 프레임 기준)와 크기(px, 720 기준).
        public static void SlotLayout(DecoSlot slot, out Vector2 anchor, out Vector2 size)
        {
            switch (slot)
            {
                // 좌상단 하트 명패(위 62px)·오른쪽 독 버튼(x≥0.83)을 피해 놓는다.
                case DecoSlot.WallL:  anchor = new Vector2(0.12f, 0.60f); size = new Vector2(110f, 110f); break;
                case DecoSlot.WallR:  anchor = new Vector2(0.76f, 0.60f); size = new Vector2(110f, 110f); break;
                case DecoSlot.SillL:  anchor = new Vector2(0.15f, 0.40f); size = new Vector2(100f, 100f); break;
                case DecoSlot.SillR:  anchor = new Vector2(0.72f, 0.40f); size = new Vector2(100f, 100f); break;
                case DecoSlot.FloorL: anchor = new Vector2(0.13f, 0.05f); size = new Vector2(140f, 140f); break;
                default:              anchor = new Vector2(0.72f, 0.05f); size = new Vector2(140f, 140f); break;
            }
        }

        public static string SlotName(DecoSlot slot)
        {
            switch (slot)
            {
                case DecoSlot.WallL: return Loc.T("왼쪽 벽", "Left wall");
                case DecoSlot.WallR: return Loc.T("오른쪽 벽", "Right wall");
                case DecoSlot.SillL: return Loc.T("창턱 왼쪽", "Sill left");
                case DecoSlot.SillR: return Loc.T("창턱 오른쪽", "Sill right");
                case DecoSlot.FloorL: return Loc.T("바닥 왼쪽", "Floor left");
                default: return Loc.T("바닥 오른쪽", "Floor right");
            }
        }
    }
}
