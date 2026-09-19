using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 150차(사용자: 「스타듀밸리처럼 식물 키우기」): 텃밭 3×3 = 9칸을 마을 안에서 직접 — 칸 위에 서서 씨 뿌리기 → 물 주기(페이즈마다 한 번) → 수확.
    /// 주가 바뀌면 자라고, 빈 칸엔 잡초가 나서 뽑아야 심을 수 있다. 데이터는 SaveData.farm(PotState 9개) — 옛 베란다 화분(pots 3개)은 첫 진입 때 옮겨 온다.
    public static class VillageFarm
    {
        public const int Rows = 3, Cols = 3, Tiles = 9;
        public const float CellW = 2.66f, CellD = 2.33f;
        public const float Top = 0.20f;   // 밭 표면 높이(지형 + 흙 상자)
        static float X0 => VillageWorld.GardenX - 4f;
        static float Z0 => VillageWorld.GardenZ - 3.5f;

        public static Vector3 TileCenter(int i) { int r = i / Cols, c = i % Cols; return new Vector3(X0 + (c + 0.5f) * CellW, 0f, Z0 + (r + 0.5f) * CellD); }
        public static int TileAt(Vector3 p)
        {
            float u = (p.x - X0) / CellW, v = (p.z - Z0) / CellD;
            if (u < 0f || v < 0f || u >= Cols || v >= Rows) return -1;
            return (int)v * Cols + (int)u;
        }
        public static int Stamp(SaveData s) => s.week * 4 + s.phaseIndex;

        public static void Ensure(SaveData s)
        {
            if (s == null) return;
            if (s.farm == null || s.farm.Length != Tiles)
            {
                var n = new PotState[Tiles];
                for (int i = 0; i < Tiles; i++) n[i] = s.farm != null && i < s.farm.Length && s.farm[i] != null ? s.farm[i] : new PotState();
                // 옛 베란다 화분 3개(줄 단위) → 각 줄 가운데 칸으로 옮김(한 번)
                if (s.farm == null && s.pots != null)
                    for (int r = 0; r < Rows && r < s.pots.Length; r++)
                        if (s.pots[r] != null && !string.IsNullOrEmpty(s.pots[r].seed)) { n[r * Cols + 1] = new PotState { seed = s.pots[r].seed, growth = s.pots[r].growth, waterStamp = s.pots[r].waterStamp }; s.pots[r] = new PotState(); }
                s.farm = n;
            }
            for (int i = 0; i < Tiles; i++) if (s.farm[i] == null) s.farm[i] = new PotState();
        }

        public static bool HasWeed(SaveData s, int i) => s != null && (s.farmWeedMask & (1 << i)) != 0;
        public static bool IsWet(SaveData s, int i) => s != null && s.farmWetStamp == Stamp(s) && (s.farmWetMask & (1 << i)) != 0;
        public static SeedDef SeedOf(SaveData s, int i) { Ensure(s); return HomeData.Seed(s.farm[i].seed); }
        public static bool Empty(SaveData s, int i) { Ensure(s); return string.IsNullOrEmpty(s.farm[i].seed); }
        /// 0 빈 칸 / 1 씨앗 / 2 새싹 / 3 줄기 / 4 봉오리 / 5 다 자람
        public static int Stage(SaveData s, int i)
        {
            var sd = SeedOf(s, i); if (sd == null) return 0;
            var p = s.farm[i];
            if (p.growth >= sd.weeks) return 5;
            float t = p.growth / (float)sd.weeks;
            return t < 0.2f ? 1 : t < 0.5f ? 2 : t < 0.8f ? 3 : 4;
        }
        public static bool Bloomed(SaveData s, int i) { var sd = SeedOf(s, i); return sd != null && s.farm[i].growth >= sd.weeks; }
        public static bool CanWater(SaveData s, int i) { var sd = SeedOf(s, i); return sd != null && s.farm[i].growth < sd.weeks && !IsWet(s, i); }

        public static bool Plant(SaveData s, int i, SeedDef seed)
        {
            Ensure(s);
            if (seed == null || !Empty(s, i) || HasWeed(s, i) || s.stats.money < seed.price) return false;
            s.stats.money -= seed.price; s.farm[i].seed = seed.id; s.farm[i].growth = 0; s.farm[i].waterStamp = -1;
            return true;
        }
        public static bool Water(SaveData s, int i)
        {
            if (!CanWater(s, i)) return false;
            if (s.farmWetStamp != Stamp(s)) { s.farmWetStamp = Stamp(s); s.farmWetMask = 0; }
            s.farmWetMask |= 1 << i; s.farm[i].growth++; s.farm[i].waterStamp = Stamp(s);
            return true;
        }
        public static bool ClearWeed(SaveData s, int i)
        {
            if (!HasWeed(s, i)) return false;
            s.farmWeedMask &= ~(1 << i); return true;
        }
        /// 수확 — 성공 확률(SeedDef.chance)을 굴려 성공이면 작물 아이템(가방), 실패면 시든다. 어느 쪽이든 칸은 비운다.
        public static bool Harvest(SaveData s, int i, out SeedDef seed, out string gotKo, out string gotEn)
        {
            seed = null; gotKo = gotEn = "";
            if (!Bloomed(s, i)) return false;
            seed = SeedOf(s, i); var p = s.farm[i];
            p.seed = null; p.growth = 0; p.waterStamp = -1;
            bool ok = Random.value < seed.chance;
            if (!ok) return false;
            switch (seed.id)
            {
                case "tomato": LifeItems.Add(s, "crop_tomato", seed.food + 1); gotKo = $"토마토 ×{seed.food + 1}"; gotEn = $"Tomato ×{seed.food + 1}"; break;
                case "potato": LifeItems.Add(s, "crop_potato", seed.food + 1); gotKo = $"감자 ×{seed.food + 1}"; gotEn = $"Potato ×{seed.food + 1}"; break;
                case "rice": LifeItems.Add(s, "ing_rice", seed.rice); LifeItems.Add(s, "crop_rice", 1); gotKo = $"쌀 {seed.rice}주분 · 볏단 1"; gotEn = $"Rice ×{seed.rice}w · sheaf 1"; break;
                case "rose": LifeItems.Add(s, "flower_rose", 1); gotKo = "장미 한 송이"; gotEn = "a rose"; break;
                case "lavender": LifeItems.Add(s, "flower_lavender", 1); gotKo = "라벤더 다발"; gotEn = "lavender"; break;
                default: LifeItems.Add(s, "ing_veg", Mathf.Max(1, seed.food)); gotKo = $"채소 ×{Mathf.Max(1, seed.food)}"; gotEn = $"Veg ×{Mathf.Max(1, seed.food)}"; break;
            }
            s.flowersSold++; LifeItems.SyncLegacy(s);
            return true;
        }

        /// 주가 바뀔 때(Survival.WeekTick): 심은 것은 한 단계 자라고, 빈 칸엔 25% 로 잡초, 난로 장작은 1개 타며 집이 따뜻하면 컨디션 +3.
        public static void WeekTick(SaveData s, Survival.WeekReport r)
        {
            if (s == null) return; Ensure(s);
            int ripe = 0; var rng = new System.Random(s.week * 977 + 150);
            for (int i = 0; i < Tiles; i++)
            {
                var sd = HomeData.Seed(s.farm[i].seed);
                if (sd != null) { if (s.farm[i].growth < sd.weeks) s.farm[i].growth++; if (s.farm[i].growth >= sd.weeks) { ripe++; r?.harvestNames.Add(sd.Name); } }
                else if (!HasWeed(s, i) && rng.NextDouble() < 0.25) s.farmWeedMask |= 1 << i;
            }
            if (r != null) r.harvested += ripe;
            if (s.villageFuel > 0)
            {
                s.villageFuel--; s.condition = Mathf.Clamp(s.condition + 3, 0, 100);
                r?.lines.Add(Loc.T($"🔥 난로에 장작을 땠다 — 집이 따뜻해서 컨디션 +3 (남은 장작 {s.villageFuel})", $"🔥 Stove burned a log — cozy home, condition +3 ({s.villageFuel} left)"));
            }
            else r?.lines.Add(Loc.T("🥶 난로에 장작이 없었다. 나무를 패거나 나뭇가지를 주워 오자.", "🥶 No firewood for the stove. Chop or gather some."));
        }

        // ── 마을 안 표시 ─────────────────────────────────────────────────
        public static Transform Marker;
        static Material _soil, _wet, _weed, _stem, _leaf, _sprout, _ring;
        static Material M(ref Material m, Color c) { if (m == null) m = CoastMaterials.CreateLit(c); return m; }

        public static void Build(Transform root, SaveData s)
        {
            var old = root.Find("Crops"); if (old != null) Object.Destroy(old.gameObject);
            var host = new GameObject("Crops").transform; host.SetParent(root, false);
            if (s == null) return;
            Ensure(s);
            var soil = M(ref _soil, new Color(0.55f, 0.40f, 0.27f)); var wet = M(ref _wet, new Color(0.36f, 0.25f, 0.17f));
            var weed = M(ref _weed, new Color(0.45f, 0.62f, 0.28f)); var stem = M(ref _stem, new Color(0.35f, 0.62f, 0.28f));
            var leaf = M(ref _leaf, new Color(0.45f, 0.76f, 0.34f)); var sprout = M(ref _sprout, new Color(0.55f, 0.84f, 0.40f));
            for (int i = 0; i < Tiles; i++)
            {
                var c = TileCenter(i); float gy = VillageWorld.Height(c.x, c.z) + Top;
                // 이랑(칸) — 옛 흙 상자(높이 0.16) 위에 얹는 표면, 물 주면 진한 흙
                var bed = Prim(host, PrimitiveType.Cube, new Vector3(c.x, gy - 0.05f, c.z), new Vector3(CellW - 0.5f, 0.10f, CellD - 0.5f), IsWet(s, i) ? wet : soil);
                bed.name = "Bed" + i;
                int stage = Stage(s, i); var sd = SeedOf(s, i);
                if (HasWeed(s, i))
                {
                    var rng = new System.Random(i * 31 + 7);
                    for (int k = 0; k < 5; k++)
                    {
                        float ox = (float)(rng.NextDouble() - 0.5) * (CellW - 0.9f), oz = (float)(rng.NextDouble() - 0.5) * (CellD - 0.9f);
                        var w = Prim(host, PrimitiveType.Sphere, new Vector3(c.x + ox, gy + 0.18f, c.z + oz), new Vector3(0.30f, 0.22f, 0.30f), weed);
                        Prim(host, PrimitiveType.Cylinder, new Vector3(c.x + ox, gy + 0.30f, c.z + oz), new Vector3(0.03f, 0.14f, 0.03f), weed);
                    }
                    continue;
                }
                if (sd == null) continue;
                for (int q = 0; q < 4; q++)
                {
                    float x = c.x + (q % 2 == 0 ? -0.55f : 0.55f), z = c.z + (q / 2 == 0 ? -0.45f : 0.45f);
                    if (stage <= 1)
                    {
                        // 씨앗: 작은 흙더미 + 떡잎
                        Prim(host, PrimitiveType.Sphere, new Vector3(x, gy + 0.14f, z), new Vector3(0.40f, 0.12f, 0.40f), soil);
                        Prim(host, PrimitiveType.Cylinder, new Vector3(x, gy + 0.24f, z), new Vector3(0.03f, 0.08f, 0.03f), sprout);
                        for (int lf = -1; lf <= 1; lf += 2) { var sp = Prim(host, PrimitiveType.Sphere, new Vector3(x + lf * 0.08f, gy + 0.32f, z), new Vector3(0.14f, 0.05f, 0.09f), sprout); sp.transform.rotation = Quaternion.Euler(0f, 0f, lf * 28f); }
                        continue;
                    }
                    float h = 0.25f + stage * 0.16f;
                    Prim(host, PrimitiveType.Cylinder, new Vector3(x, gy + 0.10f + h * 0.5f, z), new Vector3(0.07f, h * 0.5f, 0.07f), stem);
                    Prim(host, PrimitiveType.Sphere, new Vector3(x, gy + 0.10f + h, z), new Vector3(h * 0.9f, h * 0.55f, h * 0.9f), stage >= 3 ? leaf : sprout);
                    if (stage >= 4)
                    {
                        // 봉오리/열매 — 다 자라면 씨앗 색으로 크게
                        float fs = stage == 5 ? 0.22f : 0.12f; var fc = stage == 5 ? sd.petal : Color.Lerp(sd.petal, leaf.color, 0.5f);
                        for (int f = 0; f < (stage == 5 ? 3 : 1); f++)
                        {
                            var fr = Prim(host, PrimitiveType.Sphere, new Vector3(x + Mathf.Cos(f * 2.1f) * 0.18f, gy + 0.10f + h * 0.85f + f * 0.05f, z + Mathf.Sin(f * 2.1f) * 0.18f), Vector3.one * fs, null);
                            fr.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(fc);
                        }
                    }
                }
            }
            // 서 있는 칸 표시(노란 링) — VillageHub 가 옮긴다
            var ring = Prim(host, PrimitiveType.Cylinder, Vector3.zero, new Vector3(CellW - 0.35f, 0.012f, CellD - 0.35f), M(ref _ring, new Color(1f, 0.92f, 0.45f)));
            ring.name = "TileMarker"; ring.SetActive(false); Marker = ring.transform;
        }

        static GameObject Prim(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            g.transform.position = pos; g.transform.localScale = scale;
            if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }
    }
}
