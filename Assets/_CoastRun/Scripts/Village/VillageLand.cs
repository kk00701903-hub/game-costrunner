using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 183차(사용자: 「동물의 숲처럼 돈을 벌 수 있게 — 돈을 벌면 땅을 살 수 있고, 땅을 사면 매달 임대료가 들어온다」).
    /// 마을 빈터 네 곳에 「매물」 푯말. 사면 작은 임대 주택이 서고, 4주(한 달)마다 월세가 코인으로 들어온다.
    /// 월세는 마을에 들어올 때(잠 뒤 아침 포함) 지난 달 수만큼 한꺼번에 정산한다.
    public static class VillageLand
    {
        public struct Lot { public string ko, en; public Vector2 pos; public float yaw; public int price, rent; public Color wall, roof; }
        public const int WeeksPerMonth = 4;
        // 184차(사용자: 「동쪽 풀밭은 러닝 30번쯤 완주해야 벌 수 있게, 월세도 그만큼」):
        // 스토리 러닝 한 판 완주 ≈ 200G(코인+니어미스, 대회 목표 CH1 120 ~ CH18 600 의 초중반 평균) × 30판 = 6,000G.
        // 나머지 땅은 처음 비율(1 : 1.5 : 2 : 3.3) 그대로, 월세는 값의 10%/달(4주) 그대로.
        // 186차: 기준을 KPOP 한 판 보통 수입 700G(EconomyScale.KpopRunRef)로 → 동쪽 풀밭 700×30 = 21,000G
        public const int RunCoinRef = EconomyScale.KpopRunRef, RunsForFirstLot = 30;
        public static readonly Lot[] Lots =
        {
            new Lot { ko = "동쪽 풀밭 땅", en = "East meadow lot", pos = new Vector2(18f, -14f), yaw = 200f, price = 21000, rent = 2100, wall = new Color(1f, 0.86f, 0.70f), roof = new Color(0.90f, 0.50f, 0.36f) },
            new Lot { ko = "정자 옆 땅", en = "Pavilion lot", pos = new Vector2(34f, -12f), yaw = 250f, price = 31500, rent = 3150, wall = new Color(0.80f, 0.90f, 1f), roof = new Color(0.40f, 0.52f, 0.80f) },
            new Lot { ko = "서쪽 들판 땅", en = "West field lot", pos = new Vector2(-34f, -6f), yaw = 110f, price = 42000, rent = 4200, wall = new Color(0.86f, 0.96f, 0.82f), roof = new Color(0.46f, 0.66f, 0.40f) },
            new Lot { ko = "언덕 전망 땅", en = "Hill-view lot", pos = new Vector2(36f, 36f), yaw = 225f, price = 70000, rent = 7000, wall = new Color(0.96f, 0.84f, 0.92f), roof = new Color(0.72f, 0.42f, 0.62f) },
        };
        public static bool Owns(SaveData s, int i) => s != null && (s.landMask & (1 << i)) != 0;
        public static int OwnedCount(SaveData s) { int n = 0; for (int i = 0; i < Lots.Length; i++) if (Owns(s, i)) n++; return n; }
        public static int MonthlyRent(SaveData s) { int r = 0; for (int i = 0; i < Lots.Length; i++) if (Owns(s, i)) r += Lots[i].rent; return r; }
        public static Vector3 Ground(int i) => VillageWorld.Ground(Lots[i].pos.x, Lots[i].pos.y);
        /// 푯말·현관 앞(주인공이 서는 자리)
        public static Vector3 Front(int i) { var q = Quaternion.Euler(0f, Lots[i].yaw, 0f) * new Vector3(0f, 0f, 4.2f); var p = Lots[i].pos + new Vector2(q.x, q.z); return VillageWorld.Ground(p.x, p.y); }

        static readonly Transform[] _vis = new Transform[4];
        public static void Build(Transform root, SaveData s)
        {
            for (int i = 0; i < Lots.Length; i++) BuildLot(root, s, i);
        }
        public static void BuildLot(Transform root, SaveData s, int i)
        {
            if (_vis[i] != null) Object.Destroy(_vis[i].gameObject);
            var host = new GameObject("Land_" + i).transform; host.SetParent(root, false); _vis[i] = host;
            var L = Lots[i]; var g = Ground(i); var f = Front(i);
            // 자리의 작은 꽃·풀 소품은 치운다(집이 꽃밭 위에 서지 않게)
            foreach (Transform ch in root)
            {
                if (ch == host) continue; string n = ch.name;
                if (!(n.Contains("Flower") || n.Contains("Pampas") || n.Contains("Grass") || n.Contains("Mushroom") || n.Contains("Pickup"))) continue;
                var d = ch.position - g; d.y = 0f; if (d.magnitude < 4.2f) ch.gameObject.SetActive(false);
            }
            // 땅 경계 말뚝 네 개 + 흙 판
            var dirt = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(dirt.GetComponent<Collider>()); dirt.name = "LotDirt";
            dirt.transform.SetParent(host, false); dirt.transform.position = g + new Vector3(0f, 0.02f, 0f); dirt.transform.localScale = new Vector3(7.6f, 0.02f, 7.6f);
            dirt.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(Owns(s, i) ? new Color(0.62f, 0.78f, 0.46f) : new Color(0.80f, 0.68f, 0.50f), 0.02f);
            if (Owns(s, i))
            {
                var h = VillageHouses.Build(host, g, L.yaw, VillageHouses.Style.Pastel, "임대 주택", "Rental", L.wall, L.roof, 0.92f);
                h.name = "RentalHouse_" + i;
                var sp = f + (Quaternion.Euler(0f, L.yaw, 0f) * new Vector3(2.2f, 0f, 0f));
                VillageHouses.Sign(host, sp - host.position, $"월세 {L.rent}G", $"Rent {L.rent}G", new Color(0.36f, 0.62f, 0.42f));
            }
            else
            {
                for (int k = 0; k < 4; k++)
                {
                    var c = new Vector3((k & 1) == 0 ? -3.4f : 3.4f, 0f, (k & 2) == 0 ? -3.4f : 3.4f);
                    var pp = g + Quaternion.Euler(0f, L.yaw, 0f) * c; pp.y = VillageWorld.Height(pp.x, pp.z);
                    var stake = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(stake.GetComponent<Collider>()); stake.name = "LotStake";
                    stake.transform.SetParent(host, false); stake.transform.position = pp + new Vector3(0f, 0.35f, 0f); stake.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                    stake.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.95f, 0.80f, 0.30f), 0.1f);
                }
                // 192차(사용자: 「에셋 보완」): 빈 땅이 흙 판만 있어 허전 — 말뚝 사이 줄 + 삼각 깃발(빨강·흰색 번갈아)
                var rope = CoastMaterials.CreateLit(new Color(0.97f, 0.95f, 0.88f), 0.05f); var flagR = CoastMaterials.CreateLit(new Color(0.92f, 0.36f, 0.34f), 0.1f); var flagW = CoastMaterials.CreateLit(Color.white, 0.1f);
                int[] ring = { 0, 1, 3, 2 };
                for (int e = 0; e < 4; e++)
                {
                    Vector3 P(int k) { var c = new Vector3((k & 1) == 0 ? -3.4f : 3.4f, 0f, (k & 2) == 0 ? -3.4f : 3.4f); var q = g + Quaternion.Euler(0f, L.yaw, 0f) * c; q.y = VillageWorld.Height(q.x, q.z) + 0.6f; return q; }
                    var a0 = P(ring[e]); var a1 = P(ring[(e + 1) % 4]); var mid = (a0 + a1) * 0.5f; var d = a1 - a0;
                    var ln = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(ln.GetComponent<Collider>()); ln.name = "LotRope"; ln.transform.SetParent(host, false);
                    ln.transform.position = mid; ln.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up); ln.transform.localScale = new Vector3(0.035f, 0.035f, d.magnitude);
                    ln.GetComponent<Renderer>().sharedMaterial = rope;
                    for (int k = 1; k < 7; k++)
                    {
                        var fp = Vector3.Lerp(a0, a1, k / 7f) + Vector3.down * 0.1f;
                        var fl = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(fl.GetComponent<Collider>()); fl.name = "LotFlag"; fl.transform.SetParent(host, false);
                        fl.transform.position = fp; fl.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, d.normalized), Vector3.up) * Quaternion.Euler(0f, 0f, 45f); fl.transform.localScale = new Vector3(0.2f, 0.2f, 0.015f);
                        fl.GetComponent<Renderer>().sharedMaterial = (k % 2 == 0) ? flagW : flagR;
                    }
                }
                VillageHouses.Sign(host, f - host.position, $"매물 {L.price:N0}G", $"For sale {L.price:N0}G", new Color(0.85f, 0.45f, 0.40f));
            }
            host.rotation = Quaternion.identity;
        }

        /// 월세 정산 — 마지막 정산 주에서 지난 달 수 × 보유 땅 월세. 받은 금액 반환(0 = 없음)
        public static int CollectRent(SaveData s, out int months)
        {
            months = 0; if (s == null) return 0;
            if (s.landMask == 0) { s.landRentWeek = s.week; return 0; }
            if (s.landRentWeek < 0) { s.landRentWeek = s.week; return 0; }
            months = (s.week - s.landRentWeek) / WeeksPerMonth;
            if (months <= 0) return 0;
            int pay = months * MonthlyRent(s);
            s.stats.money += pay; s.landRentWeek += months * WeeksPerMonth;
            return pay;
        }
        public static int WeeksToRent(SaveData s) => s == null || s.landRentWeek < 0 ? WeeksPerMonth : Mathf.Max(1, WeeksPerMonth - (s.week - s.landRentWeek));

        public static bool Buy(SaveData s, int i)
        {
            if (s == null || Owns(s, i) || s.stats.money < Lots[i].price) return false;
            if (s.landMask == 0) s.landRentWeek = s.week;   // 첫 땅: 이번 주부터 한 달 세기
            s.stats.money -= Lots[i].price; s.landMask |= 1 << i;
            return true;
        }
    }

    /// 183차: 동물의 숲식 팔기 — 가방의 채집물·벌레·작물·고기를 상점에 판다
    public static class VillageSell
    {
        public static readonly (string id, int price)[] Prices =
        {
            ("bug_butterfly", 30), ("bug_ladybug", 25), ("bug_dragonfly", 60), ("bug_bigbeetle", 90), ("bug_hornet", 75),
            ("bug_crab", 50), ("bug_hermit", 40), ("ing_fish", 80),
            ("bug_rare_butterfly", 600), ("bug_rare_dragonfly", 800), ("bug_rare_ladybug", 500), ("bug_rare_beetle", 1200),   // 198차: 희귀 벌레
            ("fish_0", 30), ("fish_1", 60), ("fish_2", 130), ("fish_3", 180), ("fish_4", 400),   // 198차: 물고기(예전 즉시 코인 ×0.8 수준)
            ("crop_tomato", 100), ("crop_potato", 90), ("crop_rice", 110), ("flower_rose", 130), ("flower_lavender", 110),   // 195차: 돈 밸런스 −25 %
            ("gath_coconut", 35), ("gath_shell", 20), ("gath_mushroom", 30), ("mat_wood", 10), ("mat_stone", 15),
            ("ing_egg", 25), ("ing_meat", 70), ("ing_milk", 30),
            // 195차: 광산·화석·과수원·양봉
            ("ore_iron", 20), ("ore_silver", 50), ("ore_gold", 120), ("gem_amethyst", 180), ("gem_jade", 280), ("gem_ruby", 550),   // 195차 밸런스: 광산 한 주 ≈ 1,000G
            ("fossil_ammonite", 120), ("fossil_trilobite", 150), ("fossil_fern", 100), ("fossil_shark", 250),
            ("souv_doll", 150), ("gift_snack", 60), ("fruit_tangerine", 20), ("fruit_hallabong", 60), ("honey_jar", 70), ("jam_tangerine", 180),
        };
        public static List<(string id, int n, int price)> Sellable(SaveData s)
        {
            var list = new List<(string, int, int)>();
            foreach (var (id, price) in Prices) { int n = LifeItems.Count(s, id); if (n > 0) list.Add((id, n, price)); }
            return list;
        }
        public static int Sell(SaveData s, string id, int n, int price)
        {
            int have = LifeItems.Count(s, id); n = Mathf.Min(n, have); if (n <= 0) return 0;
            LifeItems.Take(s, id, n); s.stats.money += n * price; return n * price;
        }
    }
}
