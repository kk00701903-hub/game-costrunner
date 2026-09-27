using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 150차(사용자: 「스타듀밸리처럼 식물 키우기」): 텃밭 3×3 = 9칸을 마을 안에서 직접 — 칸 위에 서서 씨 뿌리기 → 물 주기(페이즈마다 한 번) → 수확.
    /// 주가 바뀌면 자라고, 빈 칸엔 잡초가 나서 뽑아야 심을 수 있다. 데이터는 SaveData.farm(PotState 9개) — 옛 베란다 화분(pots 3개)은 첫 진입 때 옮겨 온다.
    public static class VillageFarm
    {
        public const int Cols = 3, MaxRows = 5;
        /// 205차(사용자: 「밭 넓히기」): 줄 수는 세이브(farmRows 3→4→5) — 늘어난 칸은 울타리 서쪽 빈터(TileCenter 참고)
        public static int Rows = 3;
        public static int Tiles => Rows * Cols;
        public static readonly int[] ExpandPrice = { 0, 0, 0, 3000, 6000 };   // [다음 줄 수] 값
        public const float CellW = 2.66f, CellD = 2.33f;
        public const float Top = 0.20f;   // 밭 표면 높이(지형 + 흙 상자)
        static float X0 => VillageWorld.GardenX - 4f;
        static float Z0 => VillageWorld.GardenZ - 3.5f;

        /// 205차: 넓힌 칸(9번부터)은 울타리 서쪽 빈터에 3칸씩 한 줄 — 북쪽은 돌길, 동쪽은 꽃밭, 남쪽은 엄마 집이라 서쪽으로(탐색 로그 기준)
        public const float WestGap = 8.4f;
        public static float WestX0 => X0 - WestGap;
        public static Vector3 WestCenter => new Vector3(WestX0 + Cols * CellW * 0.5f, 0f, Z0 + (Rows - 3) * CellD * 0.5f);
        public static Vector3 TileCenter(int i)
        {
            if (i >= 9) { int j = i - 9, rr = j / Cols, cc = j % Cols; return new Vector3(WestX0 + (cc + 0.5f) * CellW, 0f, Z0 + (rr + 0.5f) * CellD); }
            int r = i / Cols, c = i % Cols; return new Vector3(X0 + (c + 0.5f) * CellW, 0f, Z0 + (r + 0.5f) * CellD);
        }
        public static int TileAt(Vector3 p)
        {
            float u = (p.x - X0) / CellW, v = (p.z - Z0) / CellD;
            if (u >= 0f && v >= 0f && u < Cols && v < 3) return (int)v * Cols + (int)u;
            float wu = (p.x - WestX0) / CellW;
            if (wu >= 0f && v >= 0f && wu < Cols && v < Rows - 3) return 9 + (int)v * Cols + (int)wu;
            return -1;
        }
        public static int Stamp(SaveData s) => s.week * 4 + s.phaseIndex;

        public static void Ensure(SaveData s)
        {
            if (s == null) return;
            if (s.farmRows < 3) s.farmRows = 3; Rows = Mathf.Clamp(s.farmRows, 3, MaxRows);
            if (s.farm != null && s.farm.Length > 0 && s.farm.Length < Tiles && s.farm.Length % Cols == 0)
            {   // 205차: 밭을 넓혔을 때 — 기존 칸은 그대로 두고 뒤에 빈 칸을 붙인다
                var g = new PotState[Tiles]; for (int i = 0; i < Tiles; i++) g[i] = i < s.farm.Length && s.farm[i] != null ? s.farm[i] : new PotState(); s.farm = g;
            }
            if (s.farmFert == null || s.farmFert.Length < Tiles) { var a = new int[Tiles]; if (s.farmFert != null) System.Array.Copy(s.farmFert, a, s.farmFert.Length); s.farmFert = a; }
            if (s.farmMiss == null || s.farmMiss.Length < Tiles) { var a = new int[Tiles]; if (s.farmMiss != null) System.Array.Copy(s.farmMiss, a, s.farmMiss.Length); s.farmMiss = a; }
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
            s.farmFert[i] = 0; s.farmMiss[i] = 0;   // 205차
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
        /// 205차: 마지막 수확 품질(★1~3). 0 = 시듦.
        public static int LastStar;
        /// 비료(품질 ★+1) — 심은 칸에 한 번
        public static bool CanFertilize(SaveData s, int i) { Ensure(s); return SeedOf(s, i) != null && !Bloomed(s, i) && s.farmFert[i] == 0; }
        public const int FertPrice = 150;
        public static bool Fertilize(SaveData s, int i) { if (!CanFertilize(s, i) || s.stats.money < FertPrice) return false; s.stats.money -= FertPrice; s.farmFert[i] = 1; return true; }
        /// 비 오는 날: 물이 필요한 칸에 저절로 물이 든다. 돌려주는 값 = 적신 칸 수.
        public static int RainWater(SaveData s)
        {
            if (s == null) return 0; Ensure(s); int n = 0;
            for (int i = 0; i < Tiles; i++) if (CanWater(s, i) && Water(s, i)) n++;
            return n;
        }
        public static int Star(SaveData s, int i) { Ensure(s); return 1 + (s.farmFert[i] > 0 ? 1 : 0) + (s.farmMiss[i] == 0 ? 1 : 0); }
        /// 수확 — 205차(사용자: 「실패는 물을 빼먹었을 때만」): 물을 두 주 넘게 못 받았을 때만 시든다. 품질 ★ = 1 + 비료 + (한 번도 안 빼먹음) → 그만큼 더 거둔다.
        public static bool Harvest(SaveData s, int i, out SeedDef seed, out string gotKo, out string gotEn)
        {
            seed = null; gotKo = gotEn = ""; LastStar = 0;
            if (!Bloomed(s, i)) return false;
            seed = SeedOf(s, i); var p = s.farm[i];
            int star = Star(s, i); bool ok = s.farmMiss[i] < 2;
            p.seed = null; p.growth = 0; p.waterStamp = -1; s.farmFert[i] = 0; s.farmMiss[i] = 0;
            if (!ok) return false;
            LastStar = star; int extra = star - 1;
            int year = s.week / Timeline.Weeks; if (s.farmStarYear != year) { s.farmStarYear = year; s.farmBestStar = 0; }
            s.farmBestStar = Mathf.Max(s.farmBestStar, star);
            if (seed.season >= 0)
            {
                int n = Mathf.Max(1, seed.food) + extra; LifeItems.Add(s, "crop_" + seed.id, n);
                gotKo = $"{seed.ko} ×{n} {new string('★', star)}"; gotEn = $"{seed.en} ×{n} {new string('★', star)}";
                s.flowersSold++; LifeItems.SyncLegacy(s); return true;
            }
            switch (seed.id)
            {
                case "tomato": LifeItems.Add(s, "crop_tomato", seed.food + 1); gotKo = $"토마토 ×{seed.food + 1}"; gotEn = $"Tomato ×{seed.food + 1}"; break;
                case "potato": LifeItems.Add(s, "crop_potato", seed.food + 1); gotKo = $"감자 ×{seed.food + 1}"; gotEn = $"Potato ×{seed.food + 1}"; break;
                case "rice": LifeItems.Add(s, "ing_rice", seed.rice); LifeItems.Add(s, "crop_rice", 1); gotKo = $"쌀 {seed.rice}주분 · 볏단 1"; gotEn = $"Rice ×{seed.rice}w · sheaf 1"; break;
                case "rose": LifeItems.Add(s, "flower_rose", 1); gotKo = "장미 한 송이"; gotEn = "a rose"; break;
                case "lavender": LifeItems.Add(s, "flower_lavender", 1); gotKo = "라벤더 다발"; gotEn = "lavender"; break;
                default: LifeItems.Add(s, "ing_veg", Mathf.Max(1, seed.food)); gotKo = $"채소 ×{Mathf.Max(1, seed.food)}"; gotEn = $"Veg ×{Mathf.Max(1, seed.food)}"; break;
            }
            if (extra > 0) { string xid = seed.id == "tomato" ? "crop_tomato" : seed.id == "potato" ? "crop_potato" : seed.id == "rice" ? "crop_rice" : seed.id == "rose" ? "flower_rose" : seed.id == "lavender" ? "flower_lavender" : "ing_veg"; LifeItems.Add(s, xid, extra); }
            gotKo += " " + new string('★', star); gotEn += " " + new string('★', star);
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
                if (sd != null)
                {
                    // 205차: 이번·지난 주에 물을 한 번도 못 받았으면 「빼먹은 주」 +1 (두 번이면 시든다), 받았으면 0
                    if (s.farm[i].growth < sd.weeks) { bool wet = s.farm[i].waterStamp >= 0 && s.farm[i].waterStamp / 4 >= s.week - 1; s.farmMiss[i] = wet ? 0 : s.farmMiss[i] + 1; }
                    if (s.farm[i].growth < sd.weeks) s.farm[i].growth++; if (s.farm[i].growth >= sd.weeks) { ripe++; r?.harvestNames.Add(sd.Name); }
                }
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
        static Material _soil, _wet, _weed, _stem, _leaf, _sprout, _ring, _rim;
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
                if (i >= 9)
                {   // 205차: 넓힌 칸 — 원래 밭처럼 흙 상자 + 밝은 두둑 테두리를 먼저 깐다
                    float h0 = VillageWorld.Height(c.x, c.z);
                    Prim(host, PrimitiveType.Cube, new Vector3(c.x, h0 + 0.05f, c.z), new Vector3(CellW - 0.11f, 0.10f, CellD - 0.08f), M(ref _rim, new Color(0.82f, 0.68f, 0.53f)));
                    Prim(host, PrimitiveType.Cube, new Vector3(c.x, h0 + 0.08f, c.z), new Vector3(CellW - 0.36f, 0.16f, CellD - 0.33f), soil);
                }
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
                    // 169차(사용자: 「식물별 육성 사진이 달라야 한다」): 작물 5종 × 성장 4단계 블렌더 모델(VCrop_<종>_<2..5>).
                    // 씨앗(1단계)은 어느 작물이나 같은 흙더미 + 떡잎, 2단계부터 종이 갈린다. 모델이 없으면 옛 공용 줄기+잎으로 떨어진다.
                    if (stage >= 2 && SpawnCropModel(host, sd, stage, new Vector3(x, gy + 0.01f, z), (i * 4 + q) * 53f)) continue;
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

        // ── 169차: 작물별 모델(Tools/blender/village_crop_kit.py → Resources/CoastRun/Models/VCrop_*) ──
        public static string CropKey(string id)
        {
            switch (id)
            {
                case "tomato": return "Tomato";
                case "potato": return "Potato";
                case "rice": return "Rice";
                case "rose": return "Rose";
                case "lavender": return "Lavender";
                default: return null;
            }
        }

        /// 한 포기를 심는다. 모델이 없으면 false 를 돌려 옛 프리미티브 조합으로 떨어진다.
        static bool SpawnCropModel(Transform host, SeedDef sd, int stage, Vector3 pos, float yaw)
        {
            string key = sd != null ? CropKey(sd.id) : null;
            if (key == null) return false;
            var t = new GameObject("Crop_" + key + "_" + stage).transform;
            t.SetParent(host, false); t.position = pos;
            var go = JejuKit.Spawn("VCrop_" + key + "_" + stage, t, Vector3.zero, yaw % 360f, 1.35f);   // 게임 카메라 거리에서 읽히게
            if (go == null) { Object.Destroy(t.gameObject); return false; }
            return true;
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
