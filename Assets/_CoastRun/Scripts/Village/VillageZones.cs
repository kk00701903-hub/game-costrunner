using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 195차(사용자: 「정류장에서 버스 타면 시내·관광지로. 시내엔 도시 빌딩·옷가게·음식점·청음샵·은행, 관광지는 중문관광단지 모티브. 광산에서 광물 캐기」):
    /// 같은 마을 씬 안, 마을에서 멀리 떨어진 평지 세 곳 — 시내(−330, 0) · 중문관광단지(+330, 0) · 광산 굴(−330, +330).
    /// VillageWorld.Height 는 이 구역 안에서 구역 바닥 높이를 돌려준다(주인공·카메라·자동이동이 그대로 동작).
    public static class VillageZones
    {
        public enum Zone { None, City, Tour, Mine }
        public static readonly Vector3 CityC = new Vector3(-330f, 0f, 0f), TourC = new Vector3(330f, 0f, 0f), MineC = new Vector3(-330f, 0f, 330f);
        public const float CityHX = 46f, CityHZ = 34f, TourHX = 50f, TourHZ = 42f, MineHX = 18f, MineHZ = 15f;

        public static Zone At(float x, float z)
        {
            if (Mathf.Abs(x - CityC.x) < CityHX + 6f && Mathf.Abs(z - CityC.z) < CityHZ + 6f) return Zone.City;
            if (Mathf.Abs(x - TourC.x) < TourHX + 6f && Mathf.Abs(z - TourC.z) < TourHZ + 12f) return Zone.Tour;
            if (Mathf.Abs(x - MineC.x) < MineHX + 6f && Mathf.Abs(z - MineC.z) < MineHZ + 6f) return Zone.Mine;
            if (VillageDungeon.Contains(new Vector3(x, 0f, z))) return Zone.Mine;   // 199차: 깊은 갱도(던전)도 광산 구역
            return Zone.None;
        }
        public static Zone At(Vector3 p) => At(p.x, p.z);
        public static string Name(Zone z) => z == Zone.City ? Loc.T("제주 시내", "Jeju City") : z == Zone.Tour ? Loc.T("중문관광단지", "Jungmun Resort") : z == Zone.Mine ? Loc.T("오름 광산", "Oreum Mine") : Loc.T("하늘 바닷가 마을", "Haneul Seaside");

        public static bool TryFloor(float x, float z, out float y)
        {
            var zn = At(x, z); y = 0f;
            if (zn == Zone.None) return false;
            if (zn == Zone.Tour) y = TourFloor(x, z);
            return true;
        }
        /// 관광지 바닥: 북쪽 광장 0.3 m, 해변(z < −10)으로 내려가 바다 속 −1.5 m.
        public static float TourFloor(float x, float z)
        {
            float lz = z - TourC.z;
            if (lz > -10f) return 0.3f;
            float t = Mathf.Clamp01((-10f - lz) / 20f);
            return Mathf.Lerp(0.3f, -1.5f, t * t * (3f - 2f * t) * 0.6f + t * 0.4f);
        }

        // 버스 정류장(구역 안) · 광산 입구
        public static Vector3 CityStop => new Vector3(CityC.x + 38f, 0f, CityC.z - 6.5f);
        public static Vector3 TourStop => new Vector3(TourC.x, 0.3f, TourC.z + 34f);
        public static Vector3 MineEntry => new Vector3(MineC.x, 0f, MineC.z - MineHZ + 2.5f);
        public static readonly Vector2 MineMouth = new Vector2(-24f, 55.5f);   // 마을 북쪽 언덕 광산 입구

        public static Transform CityBank, Boutique, Records, Noodle, Pork, Mart, Museum, Market, Souvenir;
        public static Transform Teddy;   // 196차: 테디베어 박물관
        public static Transform Gacha, Workshop;   // 198차: 시내 가챠샵 · 도구 공방
        public static Vector3 MarinaSpot, SurfSpot, TrackSpot, PhotoSpot;   // 196차: 중문 액티비티
        public static Transform Root;
        /// 관광지 활동 스팟 5곳(해변·주상절리·폭포·식물원·호텔 정원)
        public static readonly Vector3[] TourSpots = new Vector3[5];
        public struct OreNode { public Transform t; public int idx; }
        public static readonly List<OreNode> OreNodes = new List<OreNode>();
        public static readonly List<Transform> Shells = new List<Transform>();

        static Material Lit(Color c, float s = 0.06f) => CoastMaterials.CreateLit(c, s);
        static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m, bool col = false, float yaw = 0f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); if (!col) Object.Destroy(g.GetComponent<Collider>());
            g.name = n; g.transform.SetParent(p, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0f, yaw, 0f); g.transform.localScale = size;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }
        static GameObject Prim(Transform p, PrimitiveType t, string n, Vector3 pos, Vector3 size, Material m, bool col = false)
        {
            var g = GameObject.CreatePrimitive(t); if (!col) Object.Destroy(g.GetComponent<Collider>());
            g.name = n; g.transform.SetParent(p, false); g.transform.position = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }
        /// 격자 평면(곡면 셰이더용). down=true 면 아래를 보는 면(천장).
        static GameObject Grid(Transform p, string n, Vector3 center, float sx, float sz, float cell, float y, Material m, bool down = false)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt(sx / cell)), nz = Mathf.Max(1, Mathf.CeilToInt(sz / cell));
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int j = 0; j <= nz; j++) for (int i = 0; i <= nx; i++) { v.Add(new Vector3(center.x - sx * 0.5f + sx * i / nx, y, center.z - sz * 0.5f + sz * j / nz)); uv.Add(new Vector2(i / (float)nx, j / (float)nz)); }
            for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i;
                if (!down) tri.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 });
                else tri.AddRange(new[] { a, a + 1, a + nx + 1, a + 1, a + nx + 2, a + nx + 1 });
            }
            var mesh = new Mesh { name = n, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var g = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(p, false);
            g.GetComponent<MeshFilter>().sharedMesh = mesh; var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        static void Wall(Transform p, Vector3 c, Vector3 size) { var w = new GameObject("Bound", typeof(BoxCollider)); w.transform.SetParent(p, false); w.transform.position = c; w.GetComponent<BoxCollider>().size = size; }
        static void Bounds(Transform p, Vector3 c, float hx, float hz)
        {
            Wall(p, c + new Vector3(0f, 3f, hz), new Vector3(hx * 2f + 2f, 8f, 1f)); Wall(p, c + new Vector3(0f, 3f, -hz), new Vector3(hx * 2f + 2f, 8f, 1f));
            Wall(p, c + new Vector3(hx, 3f, 0f), new Vector3(1f, 8f, hz * 2f + 2f)); Wall(p, c + new Vector3(-hx, 3f, 0f), new Vector3(1f, 8f, hz * 2f + 2f));
        }
        /// 간판(판 + 월드 캔버스 글자) — 판은 facing 방향으로 읽힌다.
        public static void Sign(Transform p, Vector3 pos, float yaw, string ko, string en, Color board, float w = 2.6f, float h = 0.7f)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            Box(p, "SignBoard", pos, new Vector3(w, h, 0.12f), Lit(board), false, yaw);
            var go = new GameObject("SignText", typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(p, false);
            go.transform.position = pos + q * new Vector3(0f, 0f, 0.075f); go.transform.rotation = q * Quaternion.Euler(0f, 180f, 0f);
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.sortingOrder = 5;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(w * 100f, h * 100f); rt.localScale = Vector3.one * 0.01f;
            var t = CoastHudLayout.MakeText(rt, "T", Loc.T(ko, en), 40, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(40);
            CoastUiArt.OutlineText(t, new Color(0.15f, 0.10f, 0.08f, 0.9f), 2.5f);
            // 뒷면에서도 읽히게
            var back = Object.Instantiate(go, p); back.transform.position = pos - q * new Vector3(0f, 0f, 0.075f); back.transform.rotation = q;
        }

        public static void Build(Transform root)
        {
            Root = new GameObject("Zones").transform; Root.SetParent(root, false);
            OreNodes.Clear(); Shells.Clear();
            try { BuildCity(Root); } catch (System.Exception e) { Debug.LogError("[Zones] city " + e); }
            try { BuildTour(Root, root); } catch (System.Exception e) { Debug.LogError("[Zones] tour " + e); }
            try { BuildMine(Root); } catch (System.Exception e) { Debug.LogError("[Zones] mine " + e); }
            try { BuildMineMouth(root); } catch (System.Exception e) { Debug.LogError("[Zones] mouth " + e); }
        }

        // ───────────────────────── 시내 ─────────────────────────
        /// 가게: 그림 파사드 상가(러닝 거리와 같은 모델)를 품은 홀더. 홀더 앞(+Z)이 길 쪽, 문(Door)은 앞면.
        static Transform Store(Transform p, string ko, string en, float x, float z, float yaw, int variant, Color board, float height = 6f, bool enter = true)
        {
            var holder = new GameObject("House_" + en.Replace(" ", "")).transform; holder.SetParent(p, false);
            holder.position = new Vector3(x, 0f, z); holder.rotation = Quaternion.Euler(0f, yaw, 0f);
            var go = JejuKit.SpawnBuilding(variant, holder, Vector3.zero, 180f);
            float front = 2.4f;
            if (go != null)
            {
                VillageWorld.Normalize(go, height, 0f); VillageWorld.AddCollider(go);
                var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); front = Mathf.Max(1.5f, holder.InverseTransformPoint(b.center).z + b.size.z * 0.5f); }
            }
            else
            {
                Box(holder, "StoreBody", holder.TransformPoint(new Vector3(0f, height * 0.5f, 0f)), new Vector3(7f, height, 5f), Lit(Color.Lerp(board, Color.white, 0.6f)), true, yaw);
                front = 2.5f;
            }
            var door = new GameObject("Door").transform; door.SetParent(holder, false); door.localPosition = new Vector3(0f, 0f, front);
            Sign(holder, holder.TransformPoint(new Vector3(0f, 3.4f, front + 0.25f)), yaw, ko, en, board, 3.2f, 0.8f);
            if (enter) VillageWorld.Reg(holder, ko);
            return holder;
        }

        static void BuildCity(Transform p)
        {
            var c = new GameObject("City").transform; c.SetParent(p, false);
            var C = CityC;
            var asphalt = Lit(new Color(0.40f, 0.42f, 0.46f), 0.1f); var walk = Lit(new Color(0.82f, 0.80f, 0.76f)); var tile = Lit(new Color(0.74f, 0.66f, 0.58f)); var white = Lit(Color.white); var yellow = Lit(new Color(1f, 0.82f, 0.2f));
            // 바닥(콜라이더) = 보도 색, 그 위 차도
            var ground = Box(c, "Terrain", C + new Vector3(0f, -0.5f, 0f), new Vector3(CityHX * 2f + 20f, 1f, CityHZ * 2f + 20f), walk, true); ground.GetComponent<MeshRenderer>().enabled = false;
            Grid(c, "CityGround", C, CityHX * 2f + 20f, CityHZ * 2f + 20f, 3f, 0f, walk);   // 195차: 곡면 셰이더 — 큰 판은 격자로(모서리가 꺼지지 않게)
            Grid(c, "Avenue", C, CityHX * 2f, 10f, 3f, 0.01f, asphalt);
            Grid(c, "CrossSt", C, 10f, CityHZ * 2f, 3f, 0.012f, asphalt);
            for (float x = -CityHX + 2f; x < CityHX - 2f; x += 4f) if (Mathf.Abs(x) > 7f) Box(c, "Lane", C + new Vector3(x, 0.03f, 0f), new Vector3(2f, 0.02f, 0.18f), yellow);
            for (float z = -CityHZ + 2f; z < CityHZ - 2f; z += 4f) if (Mathf.Abs(z) > 7f) Box(c, "Lane", C + new Vector3(0f, 0.03f, z), new Vector3(0.18f, 0.02f, 2f), yellow);
            for (int i = -4; i <= 4; i++) { Box(c, "Zebra", C + new Vector3(-7.5f, 0.035f, i * 1.0f), new Vector3(2.4f, 0.02f, 0.5f), white); Box(c, "Zebra", C + new Vector3(7.5f, 0.035f, i * 1.0f), new Vector3(2.4f, 0.02f, 0.5f), white); }
            for (int i = -4; i <= 4; i++) { Box(c, "Zebra", C + new Vector3(i * 1.0f, 0.035f, 7.5f), new Vector3(0.5f, 0.02f, 2.4f), white); Box(c, "Zebra", C + new Vector3(i * 1.0f, 0.035f, -7.5f), new Vector3(0.5f, 0.02f, 2.4f), white); }
            // 보도 블록 무늬(연한 벽돌색 띠)
            for (int s = -1; s <= 1; s += 2) Grid(c, "WalkTile", C + new Vector3(0f, 0f, s * 7.8f), CityHX * 2f, 3f, 3f, 0.008f, tile);

            // 가게 줄: 북쪽 줄(길을 남쪽으로 봄 = yaw 180), 남쪽 줄(yaw 0)
            float nz = C.z + 12.5f, sz = C.z - 12.5f;
            CityBank = Store(c, "은행", "Bank", C.x - 38f, nz, 180f, 11, new Color(0.25f, 0.40f, 0.70f), 7f);
            Boutique = Store(c, "탐라 부티크", "Tamra Boutique", C.x - 26f, nz, 180f, 3, new Color(0.90f, 0.40f, 0.60f));
            Records = Store(c, "섬소리 청음샵", "Islesound Records", C.x - 14f, nz, 180f, 5, new Color(0.35f, 0.30f, 0.55f));
            Noodle = Store(c, "올레 고기국수", "Olle Noodles", C.x + 14f, nz, 180f, 7, new Color(0.85f, 0.45f, 0.20f));
            Mart = Store(c, "시내 마트", "City Mart", C.x + 26f, nz, 180f, 9, new Color(0.25f, 0.60f, 0.40f), 6.5f);
            Museum = Store(c, "민속자연사박물관", "Folk & Natural History Museum", C.x + 38f, nz + 1f, 180f, 13, new Color(0.45f, 0.36f, 0.28f), 7.5f);
            Pork = Store(c, "돔베 흑돼지", "Dombe Black Pork", C.x - 38f, sz, 0f, 2, new Color(0.25f, 0.22f, 0.22f));
            Market = Store(c, "축협 가축시장", "Livestock Market", C.x - 26f, sz, 0f, 6, new Color(0.55f, 0.42f, 0.25f));
            Gacha = Store(c, "럭키 가챠샵", "Lucky Gacha", C.x - 14f, sz, 0f, 4, new Color(0.95f, 0.45f, 0.62f), 6f);   // 198차: 카페 거리 자리 → 가챠샵
            Workshop = Store(c, "탐라 도구 공방", "Tamra Tool Workshop", C.x + 14f, sz, 0f, 8, new Color(0.62f, 0.38f, 0.20f), 7f);   // 198차: 제주 오피스 자리 → 도구 공방
            Store(c, "호텔 탐라", "Hotel Tamra", C.x + 26f, sz, 0f, 10, new Color(0.60f, 0.55f, 0.45f), 11f, false);
            // 뒤 줄 빌딩(스카이라인)
            var rng = new System.Random(195);
            for (int i = 0; i < 12; i++)
            {
                float x = C.x - 40f + i * 7.3f;
                if (Mathf.Abs(x - C.x) < 8f) continue;   // 가운데 길 끝은 비움(광장·분수)
                for (int s = -1; s <= 1; s += 2)
                {
                    float h = 10f + (float)rng.NextDouble() * 12f;
                    var tower = new GameObject("Tower").transform; tower.SetParent(c, false); tower.position = new Vector3(x, 0f, C.z + s * 27f); tower.rotation = Quaternion.Euler(0f, s > 0 ? 180f : 0f, 0f);
                    var go = JejuKit.SpawnBuilding(20 + i * 2 + (s > 0 ? 1 : 0), tower, Vector3.zero, 180f);
                    if (go != null) { VillageWorld.Normalize(go, h, 0f); VillageWorld.AddCollider(go); }
                    else Box(tower, "TowerBody", tower.position + Vector3.up * h * 0.5f, new Vector3(6.5f, h, 6f), Lit(Color.HSVToRGB((float)rng.NextDouble(), 0.12f, 0.92f)), true);
                }
            }
            // 광장: 분수 + 돌하르방 + 벤치 + 가로수(교차로 북쪽 끝)
            var fz = C.z + 22f; var stone = Lit(new Color(0.78f, 0.76f, 0.72f)); var water = Lit(new Color(0.45f, 0.75f, 0.95f), 0.6f);
            Prim(c, PrimitiveType.Cylinder, "Fountain", new Vector3(C.x, 0.35f, fz), new Vector3(5f, 0.35f, 5f), stone, true);
            Prim(c, PrimitiveType.Cylinder, "FountainWater", new Vector3(C.x, 0.62f, fz), new Vector3(4.4f, 0.04f, 4.4f), water);
            Prim(c, PrimitiveType.Cylinder, "FountainTop", new Vector3(C.x, 1.3f, fz), new Vector3(0.6f, 0.9f, 0.6f), stone);
            var jet = Prim(c, PrimitiveType.Sphere, "FountainJet", new Vector3(C.x, 2.4f, fz), new Vector3(0.7f, 1.0f, 0.7f), water); jet.AddComponent<Bob>();
            VillageWorld.SpawnScaled("Prop_Hareubang", c, C.x - 4.5f, fz - 3.5f, 200f, 1.8f); VillageWorld.SpawnScaled("Prop_Hareubang", c, C.x + 4.5f, fz - 3.5f, 160f, 1.8f);
            for (int i = -1; i <= 1; i += 2) VillageWorld.SpawnScaled("Prop_Bench", c, C.x + i * 6f, fz, 90f * i, 0.9f);
            // 가로수(야자수 화분)·가로등·신호등
            for (float x = -40f; x <= 40f; x += 10f)
                for (int s = -1; s <= 1; s += 2)
                {
                    if (Mathf.Abs(x) < 8f) continue;
                    var tp = new Vector3(C.x + x + 5f, 0f, C.z + s * 6.6f);
                    Prim(c, PrimitiveType.Cylinder, "Planter", tp + Vector3.up * 0.25f, new Vector3(1.0f, 0.25f, 1.0f), Lit(new Color(0.40f, 0.38f, 0.36f)), true);
                    VillageHouses.Palm(c, tp + Vector3.up * 0.45f, x * 13f, 3.8f);
                    var lamp = new Vector3(C.x + x, 0f, C.z + s * 6.8f);
                    Prim(c, PrimitiveType.Cylinder, "StreetLampPole", lamp + Vector3.up * 2f, new Vector3(0.12f, 2f, 0.12f), Lit(new Color(0.25f, 0.27f, 0.32f)));
                    Prim(c, PrimitiveType.Sphere, "StreetLamp", lamp + Vector3.up * 4.1f, Vector3.one * 0.45f, CoastMaterials.CreateUnlit(new Color(1f, 0.95f, 0.75f)));
                }
            for (int i = 0; i < 4; i++)
            {
                var tl = new Vector3(C.x + (i % 2 == 0 ? -6f : 6f), 0f, C.z + (i < 2 ? -6f : 6f));
                Prim(c, PrimitiveType.Cylinder, "SignalPole", tl + Vector3.up * 1.8f, new Vector3(0.12f, 1.8f, 0.12f), Lit(new Color(0.3f, 0.3f, 0.32f)));
                Box(c, "Signal", tl + Vector3.up * 3.7f, new Vector3(0.35f, 0.9f, 0.3f), Lit(new Color(0.15f, 0.15f, 0.17f)));
                Prim(c, PrimitiveType.Sphere, "SignalG", tl + new Vector3(0f, 3.45f, 0.17f), Vector3.one * 0.2f, CoastMaterials.CreateUnlit(new Color(0.3f, 1f, 0.5f)));
                Prim(c, PrimitiveType.Sphere, "SignalR", tl + new Vector3(0f, 3.95f, 0.17f), Vector3.one * 0.2f, CoastMaterials.CreateUnlit(new Color(0.5f, 0.15f, 0.15f)));
            }
            // 시내 정류장
            BusShelter(c, CityStop, 180f, Loc.T("시내 정류장", "City stop"));
            // 오가는 차 · 사람
            var tr = new GameObject("CityTraffic").AddComponent<LoopTraffic>(); tr.transform.SetParent(c, false); tr.Setup(new Vector3(C.x - CityHX + 2f, 0f, C.z), new Vector3(C.x + CityHX - 2f, 0f, C.z), 2.4f);
            string[] models = { "Npc_Cafe", "Npc_Florist", "Npc_Surfer", "Npc_FisherBoy", "Npc_Keeper", "Npc_Haenyeo" };
            for (int i = 0; i < 9; i++)
            {
                int s = i % 2 == 0 ? 1 : -1;
                var w = Walker.Create(c, models[i % models.Length], new Vector3(C.x - 40f + i * 9f, 0f, C.z + s * 7.8f), new Vector2(C.x - 42f, C.x + 42f), C.z + s * 7.8f, i);
            }
            Bounds(c, C, CityHX, CityHZ);
            foreach (Transform ch in c) if (ch.name != "Terrain" && !ch.name.StartsWith("Zebra") && !ch.name.StartsWith("Lane") && !ch.name.StartsWith("Avenue") && !ch.name.StartsWith("Cross") && ch.name != "Bound") BuildingOutline.Attach(ch, 0.02f);
        }

        public static void BusShelter(Transform p, Vector3 pos, float yaw, string title)
        {
            var t = new GameObject("BusShelter").transform; t.SetParent(p, false); t.position = pos; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var blue = Lit(new Color(0.22f, 0.50f, 0.85f), 0.2f); var glass = Lit(new Color(0.82f, 0.93f, 1f), 0.6f);
            for (int s = -1; s <= 1; s += 2) Box(t, "StopPost", t.TransformPoint(new Vector3(s * 1.7f, 1.2f, -0.7f)), new Vector3(0.1f, 2.4f, 0.1f), blue, true, yaw);
            Box(t, "StopRoof", t.TransformPoint(new Vector3(0f, 2.45f, 0f)), new Vector3(3.8f, 0.12f, 1.9f), blue, false, yaw);
            Box(t, "StopBack", t.TransformPoint(new Vector3(0f, 1.3f, -0.72f)), new Vector3(3.3f, 1.9f, 0.05f), glass, true, yaw);
            Box(t, "StopBench", t.TransformPoint(new Vector3(0f, 0.45f, -0.45f)), new Vector3(2.6f, 0.08f, 0.4f), Lit(new Color(0.74f, 0.54f, 0.34f)), false, yaw);
            Sign(t, t.TransformPoint(new Vector3(0f, 2.8f, 0.6f)), yaw, "🚌 " + title, "🚌 " + title, new Color(0.22f, 0.50f, 0.85f), 2.6f, 0.5f);
        }

        // ───────────────────────── 중문관광단지 ─────────────────────────
        /// 196차(사용자: 「중문관광지 놀거리 액티비티」): 서쪽 해변 마리나(요트·제트스키) · 해변 서핑 숍 · 북동 승마·카트 체험장 · 북서 테디베어 박물관 + 포토존
        static void BuildActivities(Transform t, Vector3 C)
        {
            var wood = Lit(new Color(0.62f, 0.44f, 0.30f)); var white = Lit(Color.white); var navy = Lit(new Color(0.20f, 0.30f, 0.55f));
            // ① 마리나: 나무 잔교 + 요트 + 제트스키
            Box(t, "Pier", new Vector3(C.x - 34f, 0.45f, C.z - 20f), new Vector3(2.4f, 0.2f, 13f), wood, true);
            for (int k = 0; k < 6; k++) for (int s = -1; s <= 1; s += 2) Prim(t, PrimitiveType.Cylinder, "PierPost", new Vector3(C.x - 34f + s * 1.1f, -0.2f, C.z - 14.5f - k * 2.2f), new Vector3(0.18f, 0.9f, 0.18f), wood);
            var yacht = new GameObject("Yacht").transform; yacht.SetParent(t, false); yacht.position = new Vector3(C.x - 30.2f, VillageWorld.SeaLevel + 0.35f, C.z - 24f);
            Box(yacht, "Hull", yacht.position, new Vector3(2.2f, 0.7f, 6f), white); Box(yacht, "HullStripe", yacht.position + new Vector3(0f, -0.1f, 0f), new Vector3(2.25f, 0.18f, 6.05f), navy);
            Box(yacht, "Cabin", yacht.position + new Vector3(0f, 0.65f, -0.6f), new Vector3(1.5f, 0.6f, 2.2f), white);
            Prim(yacht, PrimitiveType.Cylinder, "Mast", yacht.position + new Vector3(0f, 3.2f, 0.6f), new Vector3(0.1f, 3f, 0.1f), wood);
            var sail = Box(yacht, "Sail", yacht.position + new Vector3(0f, 3.2f, 1.6f), new Vector3(0.05f, 4.6f, 1.8f), Lit(new Color(1f, 0.98f, 0.94f))); sail.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            yacht.gameObject.AddComponent<SeaFloat>();
            var jet = new GameObject("JetSki").transform; jet.SetParent(t, false); jet.position = new Vector3(C.x - 37.4f, VillageWorld.SeaLevel + 0.3f, C.z - 22.5f);
            Box(jet, "JetBody", jet.position, new Vector3(0.9f, 0.45f, 2.3f), Lit(new Color(1f, 0.55f, 0.18f))); Box(jet, "JetSeat", jet.position + new Vector3(0f, 0.32f, -0.3f), new Vector3(0.5f, 0.2f, 0.9f), navy);
            Box(jet, "JetBar", jet.position + new Vector3(0f, 0.55f, 0.5f), new Vector3(0.8f, 0.08f, 0.08f), navy);
            jet.gameObject.AddComponent<SeaFloat>();
            Sign(t, new Vector3(C.x - 34f, 2.4f, C.z - 12.4f), 180f, "중문 마리나", "Jungmun Marina", new Color(0.20f, 0.45f, 0.75f), 3.2f, 0.7f);
            MarinaSpot = new Vector3(C.x - 34f, TourFloor(C.x - 34f, C.z - 11f), C.z - 11f);
            // ② 서핑 숍: 오두막 + 보드 걸이
            Box(t, "SurfShack", new Vector3(C.x + 16f, 1.4f, C.z - 5f), new Vector3(3.6f, 2.2f, 2.6f), Lit(new Color(0.40f, 0.75f, 0.85f)), true);
            Box(t, "SurfRoof", new Vector3(C.x + 16f, 2.7f, C.z - 5f), new Vector3(4.2f, 0.3f, 3.2f), Lit(new Color(1f, 0.85f, 0.35f)));
            Color[] bc = { new Color(1f, 0.45f, 0.45f), new Color(1f, 0.85f, 0.30f), new Color(0.40f, 0.80f, 0.55f), new Color(0.95f, 0.60f, 0.85f) };
            for (int k = 0; k < 4; k++) { var bd = Prim(t, PrimitiveType.Sphere, "SurfBoard", new Vector3(C.x + 13.2f + k * 0.55f, 1.3f, C.z - 7f), new Vector3(0.45f, 2.2f, 0.12f), Lit(bc[k], 0.4f)); bd.transform.rotation = Quaternion.Euler(0f, 0f, -8f + k * 5f); }
            Sign(t, new Vector3(C.x + 16f, 3.3f, C.z - 6.4f), 180f, "서핑 숍", "Surf Shop", new Color(0.20f, 0.55f, 0.70f), 2.6f, 0.6f);
            SurfSpot = new Vector3(C.x + 14f, 0.3f, C.z - 8.6f);
            // ③ 승마·카트 체험장: 타이어 벽 타원 트랙 + 카트 + 조랑말
            var tire = Lit(new Color(0.12f, 0.12f, 0.14f)); var tireR = Lit(new Color(0.90f, 0.20f, 0.22f));
            for (int k = 0; k < 28; k++)
            {
                float a = k / 28f * Mathf.PI * 2f; var p = new Vector3(C.x + 38f + Mathf.Cos(a) * 7f, 0.45f, C.z + 31f + Mathf.Sin(a) * 4.2f);
                if (Mathf.Abs(Mathf.Cos(a)) < 0.2f && Mathf.Sin(a) < 0f) continue;   // 남쪽 입구
                Prim(t, PrimitiveType.Cylinder, "Tire", p, new Vector3(0.7f, 0.25f, 0.7f), k % 2 == 0 ? tire : tireR, true);
            }
            var kart = new GameObject("Kart").transform; kart.SetParent(t, false); kart.position = new Vector3(C.x + 35f, 0.55f, C.z + 30f); kart.rotation = Quaternion.Euler(0f, 90f, 0f);
            Box(kart, "KartBody", kart.position, new Vector3(1f, 0.35f, 1.7f), Lit(new Color(0.95f, 0.30f, 0.35f))); Box(kart, "KartSeat", kart.position + new Vector3(0f, 0.3f, -0.2f), new Vector3(0.6f, 0.4f, 0.5f), navy);
            for (int s = -1; s <= 1; s += 2) for (int f = -1; f <= 1; f += 2) { var w = Prim(kart, PrimitiveType.Cylinder, "KartWheel", kart.position + new Vector3(s * 0.55f, -0.15f, f * 0.6f), new Vector3(0.35f, 0.1f, 0.35f), tire); w.transform.rotation = Quaternion.Euler(0f, 0f, 90f); }
            var pony = new GameObject("Pony").transform; pony.SetParent(t, false); pony.position = new Vector3(C.x + 41f, 0.3f, C.z + 31.5f);
            var brown = Lit(new Color(0.55f, 0.36f, 0.22f)); var mane = Lit(new Color(0.25f, 0.16f, 0.10f));
            Box(pony, "PonyBody", pony.position + new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 0.7f, 1.6f), brown);
            for (int s = -1; s <= 1; s += 2) for (int f = -1; f <= 1; f += 2) Box(pony, "PonyLeg", pony.position + new Vector3(s * 0.22f, 0.4f, f * 0.55f), new Vector3(0.16f, 0.8f, 0.16f), brown);
            Box(pony, "PonyNeck", pony.position + new Vector3(0f, 1.55f, 0.75f), new Vector3(0.35f, 0.8f, 0.4f), brown).transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            Box(pony, "PonyHead", pony.position + new Vector3(0f, 1.95f, 1.05f), new Vector3(0.32f, 0.35f, 0.6f), brown);
            Box(pony, "PonyMane", pony.position + new Vector3(0f, 1.75f, 0.68f), new Vector3(0.1f, 0.7f, 0.3f), mane).transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            for (int k = 0; k < 3; k++) { Box(t, "Hurdle", new Vector3(C.x + 36f + k * 2.5f, 0.55f, C.z + 33f), new Vector3(1.4f, 0.08f, 0.08f), white); for (int s = -1; s <= 1; s += 2) Box(t, "HurdlePost", new Vector3(C.x + 36f + k * 2.5f + s * 0.7f, 0.5f, C.z + 33f), new Vector3(0.08f, 0.6f, 0.08f), tireR); }
            Sign(t, new Vector3(C.x + 38f, 2.4f, C.z + 26.2f), 180f, "승마·전동카트 체험장", "Riding & E-kart Park", new Color(0.70f, 0.35f, 0.25f), 4.2f, 0.7f);
            TrackSpot = new Vector3(C.x + 38f, 0.3f, C.z + 25f);
            // ④ 테디베어 박물관 + 포토존(큰 곰)
            Teddy = VillageHouses.Build(t, new Vector3(C.x - 36f, 0.3f, C.z + 32f), 180f, VillageHouses.Style.Pastel, "테디베어 박물관", "Teddy Bear Museum", new Color(0.98f, 0.90f, 0.82f), new Color(0.62f, 0.40f, 0.26f), 1.0f);
            if (Teddy != null) VillageWorld.Reg(Teddy, "테디베어 박물관");
            var bear = new GameObject("GiantTeddy").transform; bear.SetParent(t, false); bear.position = new Vector3(C.x - 28f, 0.3f, C.z + 24f);
            var fur = Lit(new Color(0.72f, 0.50f, 0.32f)); var furL = Lit(new Color(0.92f, 0.78f, 0.60f)); var eye = Lit(new Color(0.08f, 0.06f, 0.06f));
            Prim(bear, PrimitiveType.Sphere, "TeddyBody", bear.position + new Vector3(0f, 1.1f, 0f), new Vector3(1.9f, 2.0f, 1.6f), fur, true);
            Prim(bear, PrimitiveType.Sphere, "TeddyBelly", bear.position + new Vector3(0f, 1.0f, -0.62f), new Vector3(1.1f, 1.2f, 0.5f), furL);
            Prim(bear, PrimitiveType.Sphere, "TeddyHead", bear.position + new Vector3(0f, 2.55f, 0f), new Vector3(1.5f, 1.35f, 1.35f), fur);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(bear, PrimitiveType.Sphere, "TeddyEar", bear.position + new Vector3(s * 0.6f, 3.2f, 0f), new Vector3(0.5f, 0.5f, 0.3f), fur);
                Prim(bear, PrimitiveType.Sphere, "TeddyEye", bear.position + new Vector3(s * 0.28f, 2.7f, -0.62f), new Vector3(0.14f, 0.16f, 0.08f), eye);
                Prim(bear, PrimitiveType.Sphere, "TeddyArm", bear.position + new Vector3(s * 1.0f, 1.4f, -0.2f), new Vector3(0.55f, 1.1f, 0.55f), fur);
                Prim(bear, PrimitiveType.Sphere, "TeddyLeg", bear.position + new Vector3(s * 0.55f, 0.35f, -0.35f), new Vector3(0.65f, 0.55f, 0.9f), fur);
            }
            Prim(bear, PrimitiveType.Sphere, "TeddySnout", bear.position + new Vector3(0f, 2.45f, -0.62f), new Vector3(0.5f, 0.38f, 0.3f), furL);
            Prim(bear, PrimitiveType.Sphere, "TeddyNose", bear.position + new Vector3(0f, 2.52f, -0.78f), new Vector3(0.16f, 0.11f, 0.08f), eye);
            var frame = Lit(new Color(1f, 0.55f, 0.70f));
            Box(t, "PhotoFrameTop", new Vector3(C.x - 28f, 4.1f, C.z + 23.2f), new Vector3(3.4f, 0.25f, 0.15f), frame); for (int s = -1; s <= 1; s += 2) Box(t, "PhotoFrameSide", new Vector3(C.x - 28f + s * 1.7f, 2.0f, C.z + 23.2f), new Vector3(0.25f, 4.2f, 0.15f), frame);
            Sign(t, new Vector3(C.x - 28f, 4.6f, C.z + 23.1f), 180f, "포토존", "Photo zone", new Color(0.95f, 0.45f, 0.60f), 2.4f, 0.55f);
            PhotoSpot = new Vector3(C.x - 28f, 0.3f, C.z + 21.4f);
            BuildingOutline.Attach(bear, 0.02f);
        }

        static void BuildTour(Transform p, Transform worldRoot)
        {
            var t = new GameObject("Tour").transform; t.SetParent(p, false);
            var C = TourC; var sand = Lit(new Color(0.96f, 0.88f, 0.70f)); var grass = Lit(new Color(0.52f, 0.78f, 0.40f)); var paving = Lit(new Color(0.86f, 0.82f, 0.74f));
            // 지형: 2 m 격자 높이 메시(MeshCollider) — 잔디 광장 → 모래 → 바닷속
            int nx = 60, nz = 56; float x0 = C.x - 60f, z0 = C.z - 56f, step = 2f;
            var verts = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            for (int j = 0; j <= nz; j++) for (int i = 0; i <= nx; i++)
            {
                float x = x0 + i * step, z = z0 + j * step; float y = TourFloor(x, z); verts.Add(new Vector3(x, y, z));
                float lz = z - C.z; var col = lz > -6f ? new Color(0.52f, 0.78f, 0.40f) : Color.Lerp(new Color(0.96f, 0.88f, 0.70f), new Color(0.80f, 0.72f, 0.55f), Mathf.Clamp01((-10f - lz) / 16f));
                if (lz > -6f && lz < -2f) col = Color.Lerp(col, new Color(0.96f, 0.88f, 0.70f), (-2f - lz) / 4f);
                cols.Add(col);
            }
            for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { int a = j * (nx + 1) + i; tris.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 }); }
            var mesh = new Mesh { name = "TourGround", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var g = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)); g.transform.SetParent(t, false);
            g.GetComponent<MeshFilter>().sharedMesh = mesh; g.GetComponent<MeshCollider>().sharedMesh = mesh;
            g.GetComponent<MeshRenderer>().sharedMaterial = sand;
            Grid(t, "TourLawn", new Vector3(C.x, 0f, C.z + 18f), TourHX * 2f + 20f, 48f, 3f, 0.34f, grass);
            // 바다(마을 바다 재질 재사용) — 남쪽
            var seaM = CoastMaterials.CreateUnlit(new Color(0.30f, 0.66f, 0.90f));   // 마을 바다 셰이더는 판 하나로는 안 보여서 단색 반짝 재질
            var sea = Grid(t, "TourSea", new Vector3(C.x, 0f, C.z - 75f), 240f, 110f, 4f, VillageWorld.SeaLevel + 0.05f, seaM);



            // 산책로(북쪽 광장 → 동서)
            Grid(t, "Promenade", new Vector3(C.x, 0f, C.z + 18f), TourHX * 2f - 6f, 4f, 3f, 0.37f, paving);
            Box(t, "Promenade2", new Vector3(C.x, 0.32f, C.z + 8f), new Vector3(4f, 0.04f, 24f), paving);
            // 입구 간판 + 정류장
            BusShelter(t, TourStop + new Vector3(4f, 0f, 1.5f), 180f, Loc.T("중문관광단지", "Jungmun Resort"));
            Sign(t, new Vector3(C.x, 3.2f, C.z + 30.08f), 180f, "중문관광단지", "Jungmun Tourist Resort", new Color(0.95f, 0.55f, 0.20f), 6f, 1.1f);
            Sign(t, new Vector3(C.x, 3.2f, C.z + 29.92f), 0f, "어서 오세요 · 중문관광단지", "Welcome to Jungmun", new Color(0.95f, 0.55f, 0.20f), 6f, 1.1f);
            for (int s = -1; s <= 1; s += 2) Box(t, "GatePost", new Vector3(C.x + s * 3.2f, 1.7f, C.z + 30f), new Vector3(0.4f, 3.4f, 0.4f), Lit(new Color(0.35f, 0.30f, 0.28f)), true);
            // 야자수 가로수
            for (float x = -44f; x <= 44f; x += 8f) { VillageHouses.Palm(t, new Vector3(C.x + x, 0.3f, C.z + 21f), x * 11f, 5f); if (Mathf.Abs(x) > 10f) VillageHouses.Palm(t, new Vector3(C.x + x + 3f, 0.3f, C.z - 3f), x * 7f, 4.6f); }
            // 리조트 호텔(배경) — 흰 벽 · 귤색 지붕
            for (int i = 0; i < 3; i++)
            {
                var hx = C.x + 14f + i * 14f; var hz = C.z + 47f; float h = 10f + i * 3f;
                Box(t, "Hotel", new Vector3(hx, h * 0.5f + 0.3f, hz), new Vector3(10f, h, 7f), Lit(new Color(0.97f, 0.96f, 0.92f)), true);
                Box(t, "HotelRoof", new Vector3(hx, h + 0.6f, hz), new Vector3(10.6f, 0.8f, 7.6f), Lit(new Color(0.95f, 0.55f, 0.22f)));
                for (int f = 0; f < (int)(h / 2.2f); f++) Box(t, "HotelWin", new Vector3(hx, 1.6f + f * 2.2f, hz - 3.52f), new Vector3(8f, 0.7f, 0.05f), Lit(new Color(0.55f, 0.78f, 0.95f), 0.5f));
            }
            // ① 색달해변 — 파라솔 · 선베드 · 조개
            TourSpots[0] = new Vector3(C.x - 2f, TourFloor(C.x - 2f, C.z - 12f), C.z - 12f);
            var umb = new[] { new Color(1f, 0.45f, 0.45f), new Color(0.35f, 0.65f, 0.95f), new Color(1f, 0.80f, 0.30f), new Color(0.45f, 0.80f, 0.55f) };
            for (int i = 0; i < 6; i++)
            {
                var up = new Vector3(C.x - 20f + i * 8f, 0f, C.z - 9f - (i % 2) * 3f); up.y = TourFloor(up.x, up.z);
                Prim(t, PrimitiveType.Cylinder, "UmbPole", up + Vector3.up * 1.2f, new Vector3(0.08f, 1.2f, 0.08f), Lit(Color.white));
                var top = Prim(t, PrimitiveType.Sphere, "Umbrella", up + Vector3.up * 2.4f, new Vector3(2.8f, 0.5f, 2.8f), Lit(umb[i % umb.Length]));
                Box(t, "SunBed", up + new Vector3(1.4f, 0.25f, 0f), new Vector3(0.7f, 0.12f, 1.8f), Lit(Color.white));
            }
            var shellM = Lit(new Color(1f, 0.85f, 0.80f), 0.4f);
            for (int i = 0; i < 10; i++)
            {
                float sx = C.x - 40f + i * 8.7f, szz = C.z - 16f - (i % 3) * 2.5f;
                var s = Prim(t, PrimitiveType.Sphere, "Shell", new Vector3(sx, TourFloor(sx, szz) + 0.06f, szz), new Vector3(0.35f, 0.12f, 0.3f), shellM);
                Shells.Add(s.transform);
            }
            // ② 주상절리대(동쪽 해안) — 육각 기둥 무리 + 나무 전망 데크
            var hex = HexMesh(); var basalt = Lit(new Color(0.28f, 0.29f, 0.32f)); var basaltL = Lit(new Color(0.40f, 0.41f, 0.44f));
            var rngH = new System.Random(7); var capM = Lit(new Color(0.62f, 0.62f, 0.60f));
            for (int i = 0; i < 90; i++)
            {
                float hx = C.x + 36f + (float)rngH.NextDouble() * 13f, hz = C.z - 34f + (float)rngH.NextDouble() * 20f;
                float top = 0.3f + (float)rngH.NextDouble() * 0.7f + 1.4f * Mathf.Clamp01((hx - C.x - 36f) / 12f);
                var col = new GameObject("Jusangjeolli", typeof(MeshFilter), typeof(MeshRenderer)); col.transform.SetParent(t, false);
                col.transform.position = new Vector3(hx, -2f, hz); col.transform.localScale = new Vector3(0.9f, top + 2f, 0.9f); col.transform.rotation = Quaternion.Euler(0f, (float)rngH.NextDouble() * 60f, 0f);
                col.GetComponent<MeshFilter>().sharedMesh = hex; col.GetComponent<MeshRenderer>().sharedMaterial = i % 3 == 0 ? basaltL : basalt;
                var mc = col.AddComponent<MeshCollider>(); mc.sharedMesh = hex; mc.convex = true;
                var cap = new GameObject("JusangjeolliTop", typeof(MeshFilter), typeof(MeshRenderer)); cap.transform.SetParent(t, false);
                cap.transform.position = new Vector3(hx, top - 0.02f, hz); cap.transform.localScale = new Vector3(0.93f, 0.08f, 0.93f); cap.transform.rotation = col.transform.rotation;
                cap.GetComponent<MeshFilter>().sharedMesh = hex; cap.GetComponent<MeshRenderer>().sharedMaterial = capM;
            }
            var deck = Lit(new Color(0.62f, 0.44f, 0.30f));
            Box(t, "Deck", new Vector3(C.x + 28f, 0.55f, C.z - 12f), new Vector3(8f, 0.2f, 6f), deck, true);
            for (int s = -1; s <= 1; s += 2) Box(t, "DeckRail", new Vector3(C.x + 28f + s * 4f, 0.95f, C.z - 12f), new Vector3(0.08f, 0.08f, 6f), deck, true);
            Box(t, "DeckRailS", new Vector3(C.x + 28f, 0.95f, C.z - 15f), new Vector3(8f, 0.08f, 0.08f), deck, true);
            for (int k = 0; k < 7; k++) Box(t, "DeckPost", new Vector3(C.x + 24f + k * 1.33f, 0.8f, C.z - 15f), new Vector3(0.08f, 0.5f, 0.08f), deck);
            Sign(t, new Vector3(C.x + 28f, 2.2f, C.z - 8.6f), 180f, "주상절리대", "Jusangjeolli Cliffs", new Color(0.30f, 0.32f, 0.36f), 3.2f, 0.7f);
            TourSpots[1] = new Vector3(C.x + 28f, 0.65f, C.z - 12f);
            // ③ 천제연 폭포(서쪽) — 절벽 · 흐르는 물줄기 · 못 · 선녀 다리
            var rock = Lit(new Color(0.36f, 0.34f, 0.32f)); var moss = Lit(new Color(0.35f, 0.55f, 0.30f));
            // 절벽: 키트 절벽 모델(없으면 바위 공)
            var cliffB = new Bounds(new Vector3(C.x - 47f, 4f, C.z + 2f), new Vector3(2f, 8f, 2f));
            for (int i = 0; i < 4; i++)
            {
                var cl = VillageWorld.SpawnScaled(i % 2 == 0 ? "VCliff_A" : "VCliff_B", t, C.x - 47f, C.z - 8f + i * 6.5f, 90f + i * 25f, 9f);
                if (cl != null) { cl.name = "FallsCliff"; VillageWorld.AddCollider(cl); foreach (var r in cl.GetComponentsInChildren<Renderer>()) cliffB.Encapsulate(r.bounds); }
                else Prim(t, PrimitiveType.Sphere, "FallsCliff", new Vector3(C.x - 47f, 3.5f, C.z - 8f + i * 6.5f), new Vector3(8f, 9f, 8f), i % 2 == 0 ? rock : moss, true);
            }
            var fallTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Waterfall");
            var fallM = fallTex != null ? CoastMaterials.CreateTexturedTransparent(fallTex, new Color(1f, 1f, 1f, 0.9f)) : Lit(new Color(0.80f, 0.92f, 1f), 0.7f);
            var fall = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(fall.GetComponent<Collider>()); fall.name = "Waterfall"; fall.transform.SetParent(t, false);
            float fx0 = Mathf.Max(C.x - 44f, cliffB.max.x + 0.2f); float fh = Mathf.Clamp(cliffB.size.y * 0.8f, 5f, 10f);
            fall.transform.position = new Vector3(fx0, 0.3f + fh * 0.5f, C.z + 2f); fall.transform.localScale = new Vector3(0.3f, fh, 3.2f);
            for (int k = 0; k < 5; k++) { var mist = Prim(t, PrimitiveType.Sphere, "FallsMist", new Vector3(fx0 + 1.2f + (k % 2) * 0.8f, 0.6f, C.z + 0.6f + k * 0.7f), Vector3.one * (1.1f + (k % 3) * 0.3f), CoastMaterials.CreateUnlit(new Color(0.93f, 0.97f, 1f))); mist.AddComponent<Bob>(); }
            float poolX = fx0 + 5.2f;
            fall.GetComponent<MeshRenderer>().sharedMaterial = fallM; fall.AddComponent<ScrollUV>();
            Prim(t, PrimitiveType.Cylinder, "FallsPool", new Vector3(poolX, 0.32f, C.z + 2f), new Vector3(9f, 0.03f, 9f), Lit(new Color(0.35f, 0.70f, 0.80f), 0.7f));
            Prim(t, PrimitiveType.Cylinder, "FallsRim", new Vector3(poolX, 0.2f, C.z + 2f), new Vector3(9.6f, 0.2f, 9.6f), rock, true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 9f * Mathf.PI; var bp = new Vector3(poolX + Mathf.Cos(a) * 5.5f, 1.0f + Mathf.Sin(a) * 1.6f, C.z + 9f);
                Box(t, "Seonimgyo", bp, new Vector3(1.2f, 0.18f, 1.6f), Lit(new Color(0.85f, 0.25f, 0.22f)), false, 0f);
            }
            Sign(t, new Vector3(poolX + 6.5f, 2.2f, C.z + 2f), -90f, "천제연 폭포", "Cheonjeyeon Falls", new Color(0.25f, 0.50f, 0.45f), 3.2f, 0.7f);
            TourSpots[2] = new Vector3(poolX + 7f, 0.3f, C.z + 2f);
            // ④ 식물원(유리 온실 돔)
            var glassTex = new Texture2D(2, 2); glassTex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); glassTex.Apply();
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(dome.GetComponent<Collider>()); dome.name = "GreenhouseDome"; dome.transform.SetParent(t, false);
            dome.transform.position = new Vector3(C.x - 18f, 0.3f, C.z + 30f); dome.transform.localScale = new Vector3(12f, 12f, 12f);
            dome.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTexturedTransparent(glassTex, new Color(0.75f, 0.92f, 1f, 0.45f));
            for (int i = 0; i < 8; i++) { var rib = Prim(t, PrimitiveType.Cylinder, "DomeRib", dome.transform.position + Vector3.up * 3f, new Vector3(0.12f, 6.2f, 0.12f), Lit(Color.white)); rib.transform.rotation = Quaternion.Euler(0f, i * 22.5f, 90f); }
            Box(t, "GreenhouseCol", new Vector3(C.x - 18f, 3f, C.z + 30f), new Vector3(9f, 6f, 9f), Lit(Color.white), true).GetComponent<MeshRenderer>().enabled = false;
            for (int i = 0; i < 4; i++) VillageHouses.Palm(t, new Vector3(C.x - 20f + (i % 2) * 4f, 0.3f, C.z + 28f + (i / 2) * 4f), i * 80f, 3.6f);
            Sign(t, new Vector3(C.x - 18f, 2.2f, C.z + 23.4f), 180f, "여미지 식물원", "Yeomiji Botanical Garden", new Color(0.30f, 0.62f, 0.40f), 3.4f, 0.7f);
            TourSpots[3] = new Vector3(C.x - 18f, 0.3f, C.z + 22f);
            // ⑤ 호텔 정원 벤치(쉬기)
            VillageWorld.SpawnScaled("Prop_Bench", t, C.x + 22f, C.z + 25f, 180f, 0.9f); VillageWorld.SpawnScaled("Prop_Bench", t, C.x + 27f, C.z + 25f, 180f, 0.9f);
            TourSpots[4] = new Vector3(C.x + 24.5f, 0.3f, C.z + 23f);
            // 제주 느낌: 입구 돌하르방 · 산책로 돌담 · 꽃밭 · 유채 무리
            VillageWorld.SpawnScaled("Prop_Hareubang", t, C.x - 5f, C.z + 30f, 180f, 1.8f); VillageWorld.SpawnScaled("Prop_Hareubang", t, C.x + 5f, C.z + 30f, 180f, 1.8f);
            var stoneTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Stone");
            var stoneM = stoneTex != null ? ArtAssets.CreateTexturedLit(stoneTex, new Color(0.92f, 0.90f, 0.86f), 0.02f) : Lit(new Color(0.40f, 0.40f, 0.42f));
            for (float x = -44f; x <= 44f; x += 1.6f) { if (Mathf.Abs(x) < 4f) continue; var sw = VillageWorld.SpawnScaled("Prop_StoneWall", t, C.x + x, C.z + 15.6f, 90f, 1.0f); if (sw != null) foreach (var r in sw.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int k2 = 0; k2 < arr.Length; k2++) arr[k2] = stoneM; r.sharedMaterials = arr; } }
            var rngF = new System.Random(19); var yellowF = Lit(new Color(1f, 0.86f, 0.2f)); var pinkF = Lit(new Color(1f, 0.55f, 0.72f)); var stem = Lit(new Color(0.35f, 0.62f, 0.30f));
            for (int i = 0; i < 160; i++)
            {
                float fx = C.x - 44f + (float)rngF.NextDouble() * 88f, fzz = C.z + 2f + (float)rngF.NextDouble() * 12f; if (Mathf.Abs(fx - C.x) < 4f) continue;
                Prim(t, PrimitiveType.Sphere, "Flower", new Vector3(fx, 0.62f, fzz), Vector3.one * 0.22f, i % 4 == 0 ? pinkF : yellowF);
                Prim(t, PrimitiveType.Cylinder, "Stem", new Vector3(fx, 0.46f, fzz), new Vector3(0.04f, 0.16f, 0.04f), stem);
            }
            // 기념품 가게(들어가기)
            Souvenir = VillageHouses.Build(t, new Vector3(C.x + 10f, 0.3f, C.z + 32f), 180f, VillageHouses.Style.Pastel, "기념품 가게", "Souvenir Shop", new Color(1f, 0.95f, 0.85f), new Color(0.98f, 0.58f, 0.18f), 0.95f);
            if (Souvenir != null) VillageWorld.Reg(Souvenir, "기념품 가게");
            BuildActivities(t, C);   // 196차: 마리나 · 서핑 숍 · 승마/카트 체험장 · 테디베어 박물관·포토존
            // 관광객
            string[] models = { "Npc_Surfer", "Npc_Florist", "Npc_Cafe", "Npc_FisherBoy" };
            for (int i = 0; i < 6; i++) Walker.Create(t, models[i % models.Length], new Vector3(C.x - 30f + i * 12f, 0.3f, C.z + 18f), new Vector2(C.x - 40f, C.x + 40f), C.z + 18f + (i % 2) * 1.2f, 100 + i);
            // 경계: 바다 쪽은 물가 조금 안(z −28)까지
            Wall(t, new Vector3(C.x, 3f, C.z + TourHZ - 4f), new Vector3(TourHX * 2f, 8f, 1f));
            Wall(t, new Vector3(C.x, 3f, C.z - 28f), new Vector3(TourHX * 2f, 8f, 1f));
            Wall(t, new Vector3(C.x + TourHX, 3f, C.z), new Vector3(1f, 8f, TourHZ * 2f)); Wall(t, new Vector3(C.x - TourHX + 6f, 3f, C.z), new Vector3(1f, 8f, TourHZ * 2f));
            foreach (Transform ch in t) if (ch.name == "Hotel" || ch.name == "Deck" || ch.name == "BusShelter" || ch.name.StartsWith("House_")) BuildingOutline.Attach(ch, 0.02f);
        }

        /// 육각 기둥(높이 1, 반지름 0.5, 바닥 y 0)
        static Mesh HexMesh()
        {
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                float a0 = i * Mathf.PI / 3f, a1 = (i + 1) * Mathf.PI / 3f;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f); var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int b = v.Count; v.Add(p0); v.Add(p1); v.Add(p1 + Vector3.up); v.Add(p0 + Vector3.up); tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                int c = v.Count; v.Add(Vector3.up); v.Add(p0 + Vector3.up); v.Add(p1 + Vector3.up); tri.AddRange(new[] { c, c + 2, c + 1 });
            }
            var m = new Mesh { name = "Hex" }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ───────────────────────── 광산 ─────────────────────────
        static void BuildMine(Transform p)
        {
            var m = new GameObject("Mine").transform; m.SetParent(p, false);
            var C = MineC; var rock = Lit(new Color(0.34f, 0.30f, 0.28f)); var rockD = Lit(new Color(0.24f, 0.21f, 0.20f)); var dirt = Lit(new Color(0.42f, 0.34f, 0.26f)); var wood = Lit(new Color(0.50f, 0.34f, 0.20f));
            var mg = Box(m, "Terrain", C + new Vector3(0f, -0.5f, 0f), new Vector3(MineHX * 2f + 8f, 1f, MineHZ * 2f + 8f), dirt, true); mg.GetComponent<MeshRenderer>().enabled = false; Grid(m, "MineGround", C, MineHX * 2f + 8f, MineHZ * 2f + 8f, 2f, 0f, dirt);
            Grid(m, "MineCeil", C, MineHX * 2f + 8f, MineHZ * 2f + 8f, 2f, 7.5f, rockD, true);   // 아래를 보는 천장 — 위(카메라가 천장 위로 갈 때)에서는 안 보인다
            var rng = new System.Random(33);
            // 울퉁불퉁한 벽(바위 공)
            for (int i = 0; i < 44; i++)
            {
                float u = i / 44f * 4f; Vector3 wp;
                if (u < 1f) wp = new Vector3(Mathf.Lerp(-MineHX, MineHX, u), 0f, MineHZ); else if (u < 2f) wp = new Vector3(MineHX, 0f, Mathf.Lerp(MineHZ, -MineHZ, u - 1f)); else if (u < 3f) wp = new Vector3(Mathf.Lerp(MineHX, -MineHX, u - 2f), 0f, -MineHZ); else wp = new Vector3(-MineHX, 0f, Mathf.Lerp(-MineHZ, MineHZ, u - 3f));
                if (Mathf.Abs(wp.x) < 3f && wp.z < 0f) continue;   // 입구 자리
                float s = 4f + (float)rng.NextDouble() * 3f;
                Prim(m, PrimitiveType.Sphere, "MineWall", C + wp + Vector3.up * 2.5f, new Vector3(s, 7f, s), i % 2 == 0 ? rock : rockD, true);
            }
            // 버팀목 · 레일 · 등불
            for (int k = -1; k <= 1; k++)
            {
                float z = C.z + k * 8f;
                for (int s = -1; s <= 1; s += 2) Box(m, "Beam", new Vector3(C.x + s * 6f, 3f, z), new Vector3(0.35f, 6f, 0.35f), wood);
                Box(m, "BeamTop", new Vector3(C.x, 6.1f, z), new Vector3(12.4f, 0.4f, 0.4f), wood);
                var lamp = new GameObject("MineLamp").AddComponent<Light>(); lamp.transform.SetParent(m, false); lamp.transform.position = new Vector3(C.x, 5.2f, z);
                lamp.type = LightType.Point; lamp.color = new Color(1f, 0.78f, 0.45f); lamp.range = 16f; lamp.intensity = 2.2f; lamp.shadows = LightShadows.None;
                Prim(m, PrimitiveType.Sphere, "Lantern", new Vector3(C.x, 5.6f, z), Vector3.one * 0.35f, CoastMaterials.CreateUnlit(new Color(1f, 0.85f, 0.5f)));
            }
            for (int s = -1; s <= 1; s += 2) Box(m, "Rail", new Vector3(C.x + s * 0.6f, 0.05f, C.z), new Vector3(0.1f, 0.1f, MineHZ * 2f - 2f), Lit(new Color(0.45f, 0.45f, 0.48f), 0.4f));
            for (float z = -MineHZ + 1f; z < MineHZ - 1f; z += 1.2f) Box(m, "Tie", new Vector3(C.x, 0.02f, C.z + z), new Vector3(1.8f, 0.06f, 0.25f), wood);
            Box(m, "MineCart", new Vector3(C.x, 0.6f, C.z + 5f), new Vector3(1.2f, 0.8f, 1.6f), Lit(new Color(0.40f, 0.38f, 0.36f), 0.3f), true);
            // 광맥 14곳(자리 고정, 종류는 주마다)
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f; float r = 0.62f + (i % 3) * 0.1f;
                var np = new Vector3(C.x + Mathf.Cos(a) * MineHX * r, 0f, C.z + Mathf.Sin(a) * MineHZ * r);
                if (Mathf.Abs(np.x - C.x) < 2.5f) np.x += 3.5f;
                var node = new GameObject("OreNode").transform; node.SetParent(m, false); node.position = np;
                Prim(node, PrimitiveType.Sphere, "OreRock", np + Vector3.up * 0.6f, new Vector3(1.7f, 1.3f, 1.5f), rock, true);
                OreNodes.Add(new OreNode { t = node, idx = i });
            }
            // 출구 사다리·표지
            Box(m, "Ladder", MineEntry + new Vector3(0f, 1.5f, -1.8f), new Vector3(1.2f, 3f, 0.15f), wood);
            Sign(m, MineEntry + new Vector3(0f, 3.4f, -1.7f), 0f, "출구 ↑ 마을로", "Exit ↑ to village", new Color(0.45f, 0.30f, 0.18f), 2.6f, 0.55f);
            Bounds(m, C, MineHX + 1f, MineHZ + 1f);
        }
        /// 광맥 색(주 시드): 0 철 1 은 2 금 3 보석 4 화석
        public static int NodeKind(SaveData s, int idx)
        {
            int seed = (s != null ? s.week * 311 + s.seed * 17 : 0) + idx * 97;
            int r = Mathf.Abs(seed * 1103515245 + 12345) % 100;
            return r < 46 ? 0 : r < 70 ? 1 : r < 82 ? 2 : r < 92 ? 3 : 4;
        }
        public static Color NodeColor(int k) => k == 0 ? new Color(0.70f, 0.45f, 0.35f) : k == 1 ? new Color(0.85f, 0.88f, 0.95f) : k == 2 ? new Color(1f, 0.82f, 0.25f) : k == 3 ? new Color(0.75f, 0.45f, 0.95f) : new Color(0.92f, 0.86f, 0.70f);
        /// 이번 주 광맥 모양(수정 결정) 다시 그림
        public static void RefreshOre(SaveData s)
        {
            if (s != null && s.mineWeek != s.week) { s.mineWeek = s.week; s.mineDone.Clear(); }
            foreach (var n in OreNodes)
            {
                if (n.t == null) continue;
                for (int i = n.t.childCount - 1; i >= 0; i--) if (n.t.GetChild(i).name == "Crystal") Object.Destroy(n.t.GetChild(i).gameObject);
                bool done = s != null && s.mineDone.Contains(n.idx);
                var rockR = n.t.Find("OreRock"); if (rockR != null) rockR.gameObject.SetActive(!done);
                if (done) continue;
                int k = NodeKind(s, n.idx); var mat = k == 4 ? Lit(NodeColor(k)) : CoastMaterials.CreateUnlit(NodeColor(k));
                for (int c = 0; c < 4; c++)
                {
                    var cr = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(cr.GetComponent<Collider>()); cr.name = "Crystal"; cr.transform.SetParent(n.t, false);
                    cr.transform.position = n.t.position + new Vector3((c % 2 - 0.5f) * 0.7f, 1.0f + (c / 2) * 0.25f, (c / 2 - 0.5f) * 0.6f); cr.transform.rotation = Quaternion.Euler(20f + c * 13f, c * 40f, 30f); cr.transform.localScale = new Vector3(0.22f, 0.5f, 0.22f);
                    cr.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }

        /// 마을 북쪽 언덕 광산 입구(바위 아치 + 나무 틀 + 등불 + 간판)
        static void BuildMineMouth(Transform worldRoot)
        {
            var g = VillageWorld.Ground(MineMouth.x, MineMouth.y);
            var t = new GameObject("MineMouth").transform; t.SetParent(worldRoot, false); t.position = g;
            var rock = Lit(new Color(0.46f, 0.42f, 0.38f)); var dark = Lit(new Color(0.06f, 0.05f, 0.05f)); var wood = Lit(new Color(0.50f, 0.34f, 0.20f));
            Prim(t, PrimitiveType.Sphere, "MineHill", g + new Vector3(0f, 1.2f, 2.6f), new Vector3(8f, 6f, 5f), rock, true);
            Box(t, "MineHole", g + new Vector3(0f, 1.2f, 0.25f), new Vector3(2.2f, 2.4f, 0.1f), dark);
            for (int s = -1; s <= 1; s += 2) Box(t, "MinePost", g + new Vector3(s * 1.25f, 1.3f, 0.1f), new Vector3(0.3f, 2.6f, 0.3f), wood);
            Box(t, "MineLintel", g + new Vector3(0f, 2.6f, 0.1f), new Vector3(3f, 0.35f, 0.35f), wood);
            Prim(t, PrimitiveType.Sphere, "MineLantern", g + new Vector3(1.6f, 2.2f, -0.1f), Vector3.one * 0.3f, CoastMaterials.CreateUnlit(new Color(1f, 0.85f, 0.5f)));
            Sign(t, g + new Vector3(0f, 3.2f, 0f), 180f, "⛏ 오름 광산", "⛏ Oreum Mine", new Color(0.45f, 0.30f, 0.18f), 2.4f, 0.55f);
            BuildingOutline.Attach(t, 0.02f);
        }
    }

    /// 분수 물줄기 둥실
    public class Bob : MonoBehaviour { Vector3 _p; void Start() => _p = transform.localPosition; void Update() { float k = 1f + Mathf.Sin(Time.time * 3f) * 0.12f; transform.localScale = new Vector3(0.7f, k, 0.7f); } }
    /// 폭포 물결 흐름
    public class ScrollUV : MonoBehaviour { Material _m; void Start() { var r = GetComponent<Renderer>(); if (r != null) _m = r.material; } void Update() { if (_m != null) _m.mainTextureOffset = new Vector2(0f, Time.time * 0.6f); } }

    /// 시내 큰길을 오가는 차(버스·밴) — 한쪽 끝에서 사라지고 반대편에서 다시
    public class LoopTraffic : MonoBehaviour
    {
        Vector3 _a, _b; readonly List<(Transform t, float s, float v, bool back)> _cars = new List<(Transform, float, float, bool)>();
        public void Setup(Vector3 a, Vector3 b, float lane)
        {
            _a = a; _b = b;
            string[] models = { "Obs3_Bus", "Obs3_Van", "Obs3_Van" };
            for (int i = 0; i < 3; i++)
            {
                bool back = i == 1;
                var holder = new GameObject("CityCar").transform; holder.SetParent(transform, false);
                var go = VillageWorld.SpawnScaled(models[i], holder, 0f, 0f, 0f, i == 0 ? 3f : 2f);
                if (go != null) { go.transform.SetParent(holder, false); go.transform.localPosition = new Vector3(0f, go.transform.localPosition.y - VillageWorld.Height(0f, 0f), 0f); go.transform.localRotation = Quaternion.identity; }
                holder.position = a + new Vector3(0f, 0f, back ? lane : -lane);
                _cars.Add((holder, i * 0.33f, i == 0 ? 0.08f : 0.12f, back));
            }
        }
        void Update()
        {
            for (int i = 0; i < _cars.Count; i++)
            {
                var c = _cars[i]; float s = Mathf.Repeat(c.s + c.v * Time.deltaTime, 1f); _cars[i] = (c.t, s, c.v, c.back);
                float k = c.back ? 1f - s : s; var p = Vector3.Lerp(_a, _b, k); p.z += c.back ? 2.4f : -2.4f; p.y = 0.02f;
                c.t.position = p; c.t.rotation = Quaternion.LookRotation(c.back ? Vector3.left : Vector3.right);
            }
        }
    }

    /// 보도를 오가는 사람(치비 리그) — 좌우로 걸었다 멈췄다
    public class Walker : MonoBehaviour
    {
        Vector2 _range; float _z, _target, _wait; int _seed;
        public static Walker Create(Transform p, string model, Vector3 pos, Vector2 xRange, float z, int seed)
        {
            var root = new GameObject("Walker_" + model).transform; root.SetParent(p, false); root.position = pos;
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(root, false);
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/" + model, pivot, 1.0f, true);
            if (rig != null)
            {
                var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, 0.12f); anim.speed = 0f; }
                var mo = pivot.gameObject.AddComponent<CharacterMotion>(); mo.Anim = anim; mo.WalkSpeed = 1.2f; mo.RunSpeed = 3f; mo.FootDust = false;
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
            }
            var w = root.gameObject.AddComponent<Walker>(); w._range = xRange; w._z = z; w._seed = seed; w._target = pos.x; w._wait = seed % 3;
            return w;
        }
        void Update()
        {
            var p = transform.position;
            if (_wait > 0f) { _wait -= Time.deltaTime; return; }
            float d = _target - p.x;
            if (Mathf.Abs(d) < 0.2f) { _target = Random.Range(_range.x, _range.y); _wait = Random.Range(1f, 4f); return; }
            float step = Mathf.Sign(d) * 1.1f * Time.deltaTime; p.x += step; p.z = _z; p.y = VillageWorld.Height(p.x, p.z);
            transform.position = p; transform.rotation = Quaternion.LookRotation(new Vector3(Mathf.Sign(d), 0f, 0f));
        }
    }
}
