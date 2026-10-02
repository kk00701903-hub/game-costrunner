using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 194차(사용자: 「은행, 제주 스타일 브런치 카페, 지도 1.5배, 지도 끝 차도·버스 정류장, 제주 스타일」):
    /// 정자 동쪽으로 새 동네 — 돌담 귤밭 · 유채꽃밭 · 방사탑 · 돌하르방 · 올레 리본 · 은행 · 귤빛 브런치 카페,
    /// 동쪽 끝 연석 너머 2차선 차도(버스·트럭이 오가고 버스는 정류장에 선다).
    public static class VillageEast
    {
        public const float BankX = 50f, BankZ = 19.5f;     // 문은 남쪽(yaw 180)
        public const float CafeX = 57f, CafeZ = -12.5f;    // 문은 북쪽(yaw 0) — 뒤(남쪽)는 바다 테라스
        public const float StopX = 68f, StopZ = 8f;        // 버스 정류장(연석 안쪽)
        public const float RoadX = 74f, RoadHalfW = 3f;    // 차도 중심·반폭

        public static Transform Bank, Cafe, BusStop;   // 195차: Bank 은 시내(VillageZones.CityBank)로 — 여기선 null
        public static readonly Vector2 PenCenter = new Vector2(57.5f, 14.5f);   // 195차: 축사 우리
        public const float HiveX = 53.2f;                                      // 195차: 유채꽃밭 서쪽 벌통 줄(z 30~35)

        // 정자 앞 → 동쪽 새 동네 → 버스 정류장
        public static readonly Vector2[] EastPath = {
            // 220차(도로 정비): 정자 몸체(콜라이더)를 관통하던 첫 구간((30,−4.5)·(37,−1))을 정자 북쪽으로 돌림
            new Vector2(23.8f, -6.2f), new Vector2(25.2f, -2.6f), new Vector2(27.2f, 1.2f), new Vector2(31.5f, 0.2f), new Vector2(36.2f, -0.8f), new Vector2(44f, 2.5f),
            new Vector2(51f, 5f), new Vector2(58f, 6.5f), new Vector2(64f, 7.5f), new Vector2(67.2f, 8f) };
        public static readonly Vector2[] BankPath = { new Vector2(44f, 2.5f), new Vector2(47f, 8f), new Vector2(49.5f, 12.5f), new Vector2(50f, 15.6f) };
        public static readonly Vector2[] CafePath = { new Vector2(58f, 6.5f), new Vector2(57.6f, 0f), new Vector2(57.1f, -8.6f) };

        // 차도(북쪽 언덕에서 내려와 해안 앞에서 동쪽 산 너머로 굽는다)
        public static readonly Vector2[] RoadLine = {
            new Vector2(RoadX, 140f), new Vector2(RoadX, 60f), new Vector2(RoadX, -6f), new Vector2(78f, -13f),
            new Vector2(88f, -17f), new Vector2(140f, -19f) };

        public static float RoadDist(float x, float z)
        {
            if (x < 60f) return 999f;
            var p = new Vector2(x, z); float best = 999f;
            for (int i = 0; i < RoadLine.Length - 1; i++)
            {
                var a = RoadLine[i]; var ab = RoadLine[i + 1] - a;
                float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * k));
            }
            return best;
        }

        static Material Lit(Color c, float s = 0.05f) => CoastMaterials.CreateLit(c, s);
        static GameObject Prim(Transform parent, PrimitiveType t, string name, Vector3 worldPos, Vector3 scale, Material m, bool collide = false, Quaternion? rot = null)
        {
            var g = GameObject.CreatePrimitive(t); if (!collide) Object.Destroy(g.GetComponent<Collider>());
            g.name = name; g.transform.SetParent(parent, false); g.transform.position = worldPos; g.transform.rotation = rot ?? Quaternion.identity; g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }

        public static void Build(Transform root)
        {
            var host = new GameObject("EastTown").transform; host.SetParent(root, false);
            var rng = new System.Random(194);

            // ── 195차(사용자: 「은행은 시내로 옮겨줘」): 은행 자리는 축사(소·흑돼지) + 울타리 우리 ──
            var barn = VillageHouses.Build(root, VillageWorld.Ground(BankX, BankZ), 180f, VillageHouses.Style.Pastel, "축사", "Barn",
                new Color(0.80f, 0.30f, 0.26f), new Color(0.44f, 0.46f, 0.50f), 0.95f);   // 207-2차: 크림 벽 → 빨간 헛간 벽 + 회색 지붕
            if (barn != null) { barn.name = "House_Barn"; foreach (Transform ch in barn) if (ch.name == "Attic" || ch.name == "AtticFrame") ch.gameObject.SetActive(false); BarnLook(barn, host); }
            {
                var fence = Lit(new Color(0.62f, 0.44f, 0.30f)); float hx = 3.6f, hz = 2.8f;
                for (float x = -hx; x <= hx + 0.01f; x += 1.2f) for (int s2 = -1; s2 <= 1; s2 += 2) Prim(host, PrimitiveType.Cube, "PenPost", VillageWorld.Ground(PenCenter.x + x, PenCenter.y + s2 * hz) + Vector3.up * 0.5f, new Vector3(0.14f, 1f, 0.14f), fence, true);
                for (float z = -hz; z <= hz + 0.01f; z += 1.2f) for (int s2 = -1; s2 <= 1; s2 += 2) Prim(host, PrimitiveType.Cube, "PenPost", VillageWorld.Ground(PenCenter.x + s2 * hx, PenCenter.y + z) + Vector3.up * 0.5f, new Vector3(0.14f, 1f, 0.14f), fence, true);
                for (int s2 = -1; s2 <= 1; s2 += 2) { Prim(host, PrimitiveType.Cube, "PenRail", VillageWorld.Ground(PenCenter.x, PenCenter.y + s2 * hz) + Vector3.up * 0.75f, new Vector3(hx * 2f, 0.1f, 0.08f), fence); Prim(host, PrimitiveType.Cube, "PenRail", VillageWorld.Ground(PenCenter.x + s2 * hx, PenCenter.y) + Vector3.up * 0.75f, new Vector3(0.08f, 0.1f, hz * 2f), fence); }
                Prim(host, PrimitiveType.Cube, "HayBale", VillageWorld.Ground(PenCenter.x + 2.6f, PenCenter.y + 1.8f) + Vector3.up * 0.35f, new Vector3(1.1f, 0.7f, 0.7f), Lit(new Color(0.90f, 0.80f, 0.45f)));
            }

            // ── 귤빛 브런치 카페: 흰 벽 + 귤색 지붕, 바다 쪽 테라스(파라솔·테왁) ──
            Cafe = VillageHouses.Build(root, VillageWorld.Ground(CafeX, CafeZ), 0f, VillageHouses.Style.Pastel, "귤빛 브런치", "Tangerine Brunch",
                new Color(1f, 0.98f, 0.94f), new Color(0.98f, 0.58f, 0.18f), 0.95f);
            if (Cafe != null)
            {
                Cafe.name = "House_Cafe";
                VillageWorld.Reg(Cafe, "귤빛 브런치");
                // 줄무늬 차양
                var awA = Lit(new Color(0.98f, 0.62f, 0.22f)); var awB = Lit(Color.white);
                for (int i = 0; i < 6; i++) Prim(Cafe, PrimitiveType.Cube, "Awning", Cafe.TransformPoint(new Vector3(-1.9f + i * 0.76f, 2.45f, 2.55f)), new Vector3(0.76f, 0.06f, 0.9f), i % 2 == 0 ? awA : awB, false, Cafe.rotation * Quaternion.Euler(-18f, 0f, 0f));
            }
            for (int i = 0; i < 3; i++) VillageWorld.Place(root, "Prop_CafeSet", CafeX - 4f + i * 4f, CafeZ - 5f, i * 40f, 1f);
            // 테왁(주황 부표)·그물 — 해녀 동네 느낌
            var tewak = Lit(new Color(1f, 0.52f, 0.12f), 0.3f); var net = Lit(new Color(0.25f, 0.45f, 0.40f));
            for (int i = 0; i < 4; i++)
            {
                var g = VillageWorld.Ground(CafeX + 6.5f + (i % 2) * 0.9f, CafeZ - 3.5f - (i / 2) * 0.9f);
                Prim(host, PrimitiveType.Sphere, "Tewak", g + Vector3.up * 0.32f, Vector3.one * 0.62f, tewak, i == 0);
                Prim(host, PrimitiveType.Cylinder, "TewakNet", g + Vector3.up * 0.08f, new Vector3(0.7f, 0.08f, 0.7f), net);
            }

            // ── 돌담 귤밭(3×3 귤나무, 남쪽 가운데가 입구) ──
            var stoneTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Stone");
            var stoneM = stoneTex != null ? ArtAssets.CreateTexturedLit(stoneTex, new Color(0.92f, 0.90f, 0.86f), 0.02f) : Lit(new Color(0.66f, 0.63f, 0.58f));
            void Stone(float x, float z, float yaw)
            {
                var t = VillageWorld.Place(root, "Prop_StoneWall", x, z, yaw, 1f, true); if (t == null) return;
                t.position -= new Vector3(0f, 0.18f, 0f);
                foreach (var r in t.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = VillageWorld.BatdamMat; r.sharedMaterials = arr; }
            }
            const float ox0 = 34.5f, ox1 = 45.5f, oz0 = 22f, oz1 = 32f;
            for (float x = ox0; x <= ox1 + 0.1f; x += 1.6f) { if (Mathf.Abs(x - (ox0 + ox1) * 0.5f) > 1.6f) Stone(x, oz0, 90f); Stone(x, oz1, 90f); }
            for (float z = oz0 + 0.8f; z <= oz1 - 0.6f; z += 1.6f) { Stone(ox0, z, 0f); Stone(ox1, z, 0f); }
            int oi = 0;
            for (int a = 0; a < 3; a++) for (int b = 0; b < 3; b++)
            {
                var tr = VillageWorld.Place(root, "Prop_OrangeTree", ox0 + 2.2f + a * 3.4f, oz0 + 2.3f + b * 3.0f, (a * 3 + b) * 41f, 0.9f);
                if (tr != null) { tr.name = "Tree_Orange_E" + (oi++); VillageWorld.Trees.Add(tr); GroundBlob.Static(root, tr.position, 2.0f, 1.6f, 0.22f); }
            }

            // ── 유채꽃밭(노란 꽃 + 초록 줄기, 한 메시) ──
            BuildCanola(host, 55f, 67f, 27f, 44f, rng);

            // 195차(사용자: 「맵에서 제주도 테마 더 살아나게 — 파이어플라이로」): 유채꽃밭 바닥 그림 + 북쪽 하늘 아래 한라산
            TexPatch(host, 54.5f, 67.5f, 26.5f, 44.5f, "Tex_Canola", new Vector2(4f, 5f));
            var hal = Resources.Load<Texture2D>("CoastRun/Textures/Village/Vista_Hallasan");
            if (hal != null)
            {
                var hm = CoastMaterials.CreateTexturedTransparentNoFog(hal, new Color(0.86f, 0.92f, 1f, 0.92f));
                for (int side = 0; side < 2; side++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "Vista"; q.transform.SetParent(root, false);
                    q.transform.position = new Vector3(-10f, 22f, 235f); q.transform.rotation = Quaternion.Euler(0f, side == 0 ? 0f : 180f, 0f); q.transform.localScale = new Vector3(190f, 58f, 1f);
                    var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = hm; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // ── 방사탑(돌탑) 둘 · 올레 리본 이정표 ──
            Bangsatap(host, 41f, -14f, stoneM); Bangsatap(host, 64.5f, -22f, stoneM);
            OlleSign(host, 36.5f, 1.8f);

            // ── 초가(장식) · 야자수 · 가로등 · 덤불 ──
            var th = VillageWorld.Place(root, "JHouse_Thatch_A", 62f, 22f, 200f, 1f); if (th != null) th.name = "EastThatch";
            float[] palmZ = { -22f, -4f, 18f, 34f, 50f };
            for (int i = 0; i < palmZ.Length; i++) { var tr = VillageHouses.Palm(root, VillageWorld.Ground(69.2f, palmZ[i]), i * 70f, 4.6f); tr.name = "Tree_Palm_E" + i; VillageWorld.Trees.Add(tr); }
            VillageWorld.Lamp(root, 33f, -1.5f); VillageWorld.Lamp(root, 47f, 2.2f); VillageWorld.Lamp(root, 61f, 5.2f); VillageWorld.Lamp(root, 52.5f, 14f);
            for (int i = 0; i < 10; i++)
            {
                float x = 30f + (float)rng.NextDouble() * 38f, z = -24f + (float)rng.NextDouble() * 80f;
                if (VillageWorld.PathDist(x, z) < 3.2f || VillageWorld.NearBuildingPublic(x, z, 6f) || (x > ox0 - 2f && x < ox1 + 2f && z > oz0 - 2f && z < oz1 + 2f) || (x > 53f && z > 25f && z < 46f) || x > 67.5f) continue;
                VillageWorld.Bush(root, x, z, rng);
            }

            // ── 차도 · 연석 가드레일 · 버스 정류장 · 오가는 차 ──
            BuildRoad(host);
            BuildGuardRail(host);
            BusStop = BuildBusStop(host);
            // 차도 너머 언덕 — 민둥 초록 벽이 되지 않게 소나무·귤나무 숲
            for (int i = 0; i < 26; i++)
            {
                float z = -8f + i * 3.6f + (float)rng.NextDouble() * 1.5f, x = 80.5f + (float)rng.NextDouble() * 9f;
                if (RoadDist(x, z) < 5f) continue;
                var pt = VillageHouses.Pine(host, VillageWorld.Ground(x, z), (float)rng.NextDouble() * 360f, 4.5f + (float)rng.NextDouble() * 3f); if (pt != null) pt.name = "Tree_PineFar";
                if (i % 3 == 0) { float x2 = 84f + (float)rng.NextDouble() * 8f, z2 = z + 1.8f; if (RoadDist(x2, z2) > 5f) VillageWorld.Place(host, "Prop_OrangeTree", x2, z2, i * 30f, 1f, false); }
            }
            var traffic = new GameObject("Traffic").AddComponent<EastTraffic>(); traffic.transform.SetParent(host, false);
        }

        /// 지형을 따라가는 그림 판(유채꽃밭 바닥 등)
        static void TexPatch(Transform host, float x0, float x1, float z0, float z1, string tex, Vector2 tiling)
        {
            var t = Resources.Load<Texture2D>("CoastRun/Textures/Village/" + tex); if (t == null) return;
            int nx = Mathf.CeilToInt((x1 - x0) / 1.5f), nz = Mathf.CeilToInt((z1 - z0) / 1.5f);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int j = 0; j <= nz; j++) for (int i = 0; i <= nx; i++) { float x = Mathf.Lerp(x0, x1, i / (float)nx), z = Mathf.Lerp(z0, z1, j / (float)nz); v.Add(new Vector3(x, VillageWorld.Height(x, z) + 0.04f, z)); uv.Add(new Vector2(i / (float)nx * tiling.x, j / (float)nz * tiling.y)); }
            for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { int a = j * (nx + 1) + i; tri.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 }); }
            var mesh = new Mesh { name = tex, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var g = new GameObject("Patch_" + tex, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(host, false); g.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = CoastMaterials.CreateToon(new Color(0.98f, 0.98f, 0.98f), t, 0.05f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void BuildCanola(Transform host, float x0, float x1, float z0, float z1, System.Random rng)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); var sph = tmp.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp);
            var tmp2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder); var cyl = tmp2.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp2);
            var heads = new List<CombineInstance>(); var stems = new List<CombineInstance>();
            for (float z = z0; z < z1; z += 0.75f)
                for (float x = x0; x < x1; x += 0.75f)
                {
                    float px = x + ((float)rng.NextDouble() - 0.5f) * 0.6f, pz = z + ((float)rng.NextDouble() - 0.5f) * 0.6f;
                    if (VillageWorld.PathDist(px, pz) < 2.2f) continue;
                    float h = VillageWorld.Height(px, pz); float tall = 0.55f + (float)rng.NextDouble() * 0.35f;
                    stems.Add(new CombineInstance { mesh = cyl, transform = Matrix4x4.TRS(new Vector3(px, h + tall * 0.5f, pz), Quaternion.identity, new Vector3(0.05f, tall * 0.5f, 0.05f)) });
                    for (int k = 0; k < 3; k++)
                        heads.Add(new CombineInstance { mesh = sph, transform = Matrix4x4.TRS(new Vector3(px + (k - 1) * 0.1f, h + tall + (k == 1 ? 0.08f : 0f), pz + (k % 2) * 0.08f), Quaternion.identity, Vector3.one * (0.2f + (float)rng.NextDouble() * 0.08f)) });
                }
            void Mesh(string n, List<CombineInstance> list, Color c)
            {
                var mesh = new Mesh { name = n, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(list.ToArray(), true, true, false); mesh.RecalculateBounds();
                var go = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(host, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh; var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = Lit(c); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var sway = go.AddComponent<WindSway>(); sway.Amp = 1.2f; sway.Speed = 1.0f; sway.Nudgeable = false;
            }
            Mesh("Canola_Stem", stems, new Color(0.40f, 0.66f, 0.30f));
            Mesh("Canola_Flower", heads, new Color(1f, 0.88f, 0.18f));
        }

        static void Bangsatap(Transform host, float x, float z, Material stone)
        {
            var g = VillageWorld.Ground(x, z); var t = new GameObject("Bangsatap").transform; t.SetParent(host, false); t.position = g;
            for (int i = 0; i < 5; i++)
            {
                float r = 1.6f - i * 0.26f;
                var c = Prim(t, PrimitiveType.Cylinder, "Tier", g + Vector3.up * (0.3f + i * 0.55f), new Vector3(r, 0.28f, r), stone, i == 0);
                c.transform.rotation = Quaternion.Euler(0f, i * 23f, 0f);
            }
            Prim(t, PrimitiveType.Sphere, "Top", g + Vector3.up * 3.1f, new Vector3(0.55f, 0.7f, 0.55f), stone);
        }

        static void OlleSign(Transform host, float x, float z)
        {
            var g = VillageWorld.Ground(x, z);
            Prim(host, PrimitiveType.Cylinder, "OllePost", g + Vector3.up * 0.9f, new Vector3(0.14f, 0.9f, 0.14f), Lit(new Color(0.48f, 0.33f, 0.20f)), true);
            Prim(host, PrimitiveType.Cube, "OlleBlue", g + new Vector3(0.28f, 1.55f, 0f), new Vector3(0.5f, 0.1f, 0.03f), Lit(new Color(0.20f, 0.55f, 0.90f)), false, Quaternion.Euler(0f, 0f, -12f));
            Prim(host, PrimitiveType.Cube, "OlleOrange", g + new Vector3(0.28f, 1.38f, 0f), new Vector3(0.5f, 0.1f, 0.03f), Lit(new Color(1f, 0.55f, 0.12f)), false, Quaternion.Euler(0f, 0f, -18f));
            Prim(host, PrimitiveType.Cube, "OlleArrow", g + new Vector3(0f, 1.95f, 0f), new Vector3(0.7f, 0.22f, 0.06f), Lit(new Color(0.20f, 0.55f, 0.90f)), false, Quaternion.Euler(0f, 20f, 0f));
        }

        // ── 차도: 아스팔트 띠 + 가운데 노란 겹선 + 흰 가장자리선 ──
        static List<Vector2> RoadPts()
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < RoadLine.Length - 1; i++)
            {
                var a = RoadLine[i]; var b = RoadLine[i + 1]; int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 1.5f));
                for (int k = 0; k < n; k++) pts.Add(Vector2.Lerp(a, b, k / (float)n));
            }
            pts.Add(RoadLine[RoadLine.Length - 1]);
            return pts;
        }
        static void Strip(Transform host, string name, List<Vector2> pts, float offA, float offB, float lift, Material m, float dash = 0f)
        {
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                var d = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized; var nrm = new Vector2(-d.y, d.x);
                var pa = pts[i] + nrm * offA; var pb = pts[i] + nrm * offB;
                v.Add(new Vector3(pa.x, VillageWorld.Height(pa.x, pa.y) + lift, pa.y)); v.Add(new Vector3(pb.x, VillageWorld.Height(pb.x, pb.y) + lift, pb.y));
                if (i == 0) continue;
                if (dash > 0f && (i % 2) == 1) continue;   // 점선: 한 칸 건너
                int q = v.Count - 4; tr.AddRange(new[] { q, q + 2, q + 1, q + 1, q + 2, q + 3 });
            }
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetTriangles(tr, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(host, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void BuildRoad(Transform host)
        {
            var pts = RoadPts();
            Strip(host, "CarRoad", pts, -RoadHalfW, RoadHalfW, 0.12f, Lit(new Color(0.42f, 0.44f, 0.48f), 0.1f));
            Strip(host, "CarRoadShoulder", pts, -RoadHalfW - 0.9f, -RoadHalfW, 0.10f, Lit(new Color(0.80f, 0.78f, 0.72f)));
            Strip(host, "CarRoadShoulderE", pts, RoadHalfW, RoadHalfW + 0.9f, 0.10f, Lit(new Color(0.80f, 0.78f, 0.72f)));
            var yellow = Lit(new Color(1f, 0.82f, 0.20f)); var white = Lit(Color.white);
            Strip(host, "RoadCenterA", pts, -0.16f, -0.06f, 0.14f, yellow); Strip(host, "RoadCenterB", pts, 0.06f, 0.16f, 0.14f, yellow);
            Strip(host, "RoadEdgeW", pts, -RoadHalfW + 0.2f, -RoadHalfW + 0.32f, 0.14f, white, 1f);
            Strip(host, "RoadEdgeE", pts, RoadHalfW - 0.32f, RoadHalfW - 0.2f, 0.14f, white, 1f);
        }
        static void BuildGuardRail(Transform host)
        {
            var post = Lit(new Color(0.92f, 0.93f, 0.95f), 0.3f); var rail = Lit(new Color(0.75f, 0.78f, 0.82f), 0.4f); float x = VillageWorld.EastLim + 0.15f;
            for (float z = -28f; z <= 58f; z += 2.5f)
            {
                if (z > StopZ - 3.5f && z < StopZ + 3.5f) continue;   // 정류장 앞은 트임
                Prim(host, PrimitiveType.Cube, "RailPost", VillageWorld.Ground(x, z) + Vector3.up * 0.4f, new Vector3(0.12f, 0.8f, 0.12f), post);
                float z2 = z + 2.5f; if (z2 > StopZ - 3.5f && z2 < StopZ + 3.5f || z2 > 58f) continue;
                var a = VillageWorld.Ground(x, z) + Vector3.up * 0.62f; var b = VillageWorld.Ground(x, z2) + Vector3.up * 0.62f;
                var r = Prim(host, PrimitiveType.Cube, "Rail", (a + b) * 0.5f, new Vector3(0.06f, 0.18f, Vector3.Distance(a, b)), rail);
                r.transform.rotation = Quaternion.LookRotation(b - a);
            }
        }

        /// 207-2차(사용자: 「계속해줘」 — 축사가 집 모양 그대로): 헛간 얼굴 — 흰 X 버팀 문짝 · 박공 건초창 · 옆에 사일로(블렌더 VSilo).
        /// 문·간판·콜라이더(입장 로직)는 그대로 두고 앞에 판자만 덧댄다.
        static void BarnLook(Transform barn, Transform host)
        {
            var white = Lit(new Color(0.97f, 0.96f, 0.93f)); var red = Lit(new Color(0.70f, 0.24f, 0.20f)); var hay = Lit(new Color(0.93f, 0.80f, 0.42f));
            void B(string n, Vector3 lp, Vector3 sc, Material m, float rz = 0f)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(g.GetComponent<Collider>()); g.name = n;
                g.transform.SetParent(barn, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(0f, 0f, rz); g.transform.localScale = sc;
                g.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            const float W = 4.6f, H = 2.7f, D = 4.0f; float fz = D * 0.5f;
            // 문짝: 흰 테두리 + X 버팀(문 크기 1.0 × 1.9)
            B("BarnDoorTop", new Vector3(0f, 1.93f, fz + 0.13f), new Vector3(1.1f, 0.1f, 0.05f), white);
            B("BarnDoorBot", new Vector3(0f, 0.1f, fz + 0.13f), new Vector3(1.1f, 0.1f, 0.05f), white);
            B("BarnDoorMid", new Vector3(0f, 1.0f, fz + 0.13f), new Vector3(1.0f, 0.08f, 0.05f), white);
            float ang = Mathf.Atan2(0.85f, 0.95f) * Mathf.Rad2Deg;
            foreach (var y in new[] { 0.55f, 1.46f }) { B("BarnDoorX", new Vector3(0f, y, fz + 0.14f), new Vector3(1.22f, 0.08f, 0.04f), white, ang); B("BarnDoorX", new Vector3(0f, y, fz + 0.14f), new Vector3(1.22f, 0.08f, 0.04f), white, -ang); }
            // 박공 건초창(다락창 자리)
            B("LoftFrame", new Vector3(0f, H + 0.85f, fz + 0.47f), new Vector3(1.0f, 0.95f, 0.08f), white);
            B("LoftDoor", new Vector3(0f, H + 0.85f, fz + 0.5f), new Vector3(0.82f, 0.78f, 0.06f), red);
            B("LoftHay", new Vector3(0f, H + 0.55f, fz + 0.55f), new Vector3(0.7f, 0.18f, 0.12f), hay);
            B("LoftX", new Vector3(0f, H + 0.9f, fz + 0.54f), new Vector3(0.95f, 0.06f, 0.03f), white, 45f); B("LoftX", new Vector3(0f, H + 0.9f, fz + 0.54f), new Vector3(0.95f, 0.06f, 0.03f), white, -45f);
            // 사일로(축사 옆, 사다리는 앞쪽)
            var sp = barn.TransformPoint(new Vector3(-(W * 0.5f + 1.5f), 0f, -0.6f));
            var silo = JejuKit.Spawn("VSilo", host, Vector3.zero, barn.eulerAngles.y + 180f, 1f);
            if (silo != null) { silo.name = "Silo"; silo.transform.position = VillageWorld.Ground(sp.x, sp.z) - Vector3.up * 0.05f; VillageWorld.AddCollider(silo); BuildingOutline.Attach(silo.transform, 0.02f); }
        }

        static Transform BuildBusStop(Transform host)
        {
            var g = VillageWorld.Ground(StopX, StopZ); var t = new GameObject("BusStop").transform; t.SetParent(host, false); t.position = g; t.rotation = Quaternion.Euler(0f, 90f, 0f);   // 차도(동쪽)를 본다
            var blue = Lit(new Color(0.22f, 0.50f, 0.85f), 0.2f); var glass = CoastMaterials.CreateLit(new Color(0.80f, 0.92f, 1f), 0.6f); var white = Lit(Color.white);
            // 바닥 · 기둥 · 지붕 · 뒷벽(유리) · 벤치
            Prim(t, PrimitiveType.Cube, "StopFloor", t.TransformPoint(new Vector3(0f, 0.05f, 0f)), new Vector3(3.6f, 0.1f, 1.8f), Lit(new Color(0.78f, 0.76f, 0.72f)), false, t.rotation);
            for (int s = -1; s <= 1; s += 2) Prim(t, PrimitiveType.Cube, "StopPost", t.TransformPoint(new Vector3(s * 1.7f, 1.2f, -0.7f)), new Vector3(0.1f, 2.4f, 0.1f), blue, true, t.rotation);
            Prim(t, PrimitiveType.Cube, "StopRoof", t.TransformPoint(new Vector3(0f, 2.45f, 0f)), new Vector3(3.8f, 0.12f, 1.9f), blue, false, t.rotation);
            Prim(t, PrimitiveType.Cube, "StopBack", t.TransformPoint(new Vector3(0f, 1.3f, -0.72f)), new Vector3(3.3f, 1.9f, 0.05f), glass, true, t.rotation);
            Prim(t, PrimitiveType.Cube, "StopBench", t.TransformPoint(new Vector3(0f, 0.45f, -0.45f)), new Vector3(2.6f, 0.08f, 0.4f), Lit(new Color(0.74f, 0.54f, 0.34f)), false, t.rotation);
            // 정류장 표지판(동그란 파랑 + 흰 버스)
            var pole = Prim(t, PrimitiveType.Cylinder, "StopSignPole", t.TransformPoint(new Vector3(2.3f, 1.3f, 0.5f)), new Vector3(0.08f, 1.3f, 0.08f), Lit(new Color(0.55f, 0.58f, 0.62f)));
            var disc = Prim(t, PrimitiveType.Cylinder, "StopSign", t.TransformPoint(new Vector3(2.3f, 2.75f, 0.5f)), new Vector3(0.7f, 0.03f, 0.7f), blue);
            disc.transform.rotation = t.rotation * Quaternion.Euler(90f, 0f, 0f);
            Prim(t, PrimitiveType.Cube, "StopSignBus", t.TransformPoint(new Vector3(2.3f, 2.75f, 0.54f)), new Vector3(0.36f, 0.24f, 0.02f), white, false, t.rotation);
            // 시간표 판
            Prim(t, PrimitiveType.Cube, "StopTable", t.TransformPoint(new Vector3(-1.2f, 1.45f, -0.68f)), new Vector3(0.6f, 0.8f, 0.03f), Lit(new Color(1f, 0.97f, 0.85f)), false, t.rotation);
            VillageZones.ShelterModel(t);   // 207차: 블렌더 정류장 모델(Firefly 포스터)
            return t;
        }

        /// 정류장에서 차도 쪽 버스 정차 위치(차도 서쪽 차선).
        public static Vector3 BusBay => VillageWorld.Ground(RoadX - 1.5f, StopZ);
        public static bool BusAtStop => EastTraffic.I != null && EastTraffic.I.BusStopped;
    }

    /// 194차: 차도를 오가는 차 — 버스 1(정류장에 5초 정차) + 트럭·밴 2. 남행은 서쪽 차선, 북행은 동쪽 차선.
    public class EastTraffic : MonoBehaviour
    {
        public static EastTraffic I { get; private set; }
        class Car { public Transform t; public float s, speed, lane; public bool bus, north; public float wait; }
        readonly List<Car> _cars = new List<Car>();
        List<Vector2> _pts; float[] _acc; float _len;
        public bool BusStopped { get; private set; }

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            _pts = new List<Vector2>(VillageEast.RoadLine);
            _acc = new float[_pts.Count]; for (int i = 1; i < _pts.Count; i++) _acc[i] = _acc[i - 1] + Vector2.Distance(_pts[i - 1], _pts[i]); _len = _acc[_pts.Count - 1];
            Add("Obs3_Bus", 3.0f, true, false, 20f, 7.5f);
            Add("Obs3_Van", 2.0f, false, true, 90f, 10f);
            Add("Obs3_Van", 2.0f, false, false, 150f, 9f);
        }
        void Add(string model, float h, bool bus, bool north, float s0, float speed)
        {
            var go = VillageWorld.SpawnScaled(model, transform, VillageEast.RoadX, 60f, 0f, h);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(go.GetComponent<Collider>()); go.transform.SetParent(transform, false);
                go.transform.localScale = bus ? new Vector3(2.4f, 2.6f, 8f) : new Vector3(1.9f, 1.8f, 4.2f);
                go.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(bus ? new Color(0.25f, 0.55f, 0.90f) : new Color(0.95f, 0.85f, 0.70f));
            }
            go.name = bus ? "Bus" : "Car";
            var holder = new GameObject(go.name + "Root").transform; holder.SetParent(transform, false); go.transform.SetParent(holder, true); go.transform.localPosition = new Vector3(0f, go.transform.localPosition.y - VillageWorld.Height(VillageEast.RoadX, 60f), 0f); go.transform.localRotation = Quaternion.identity;
            _cars.Add(new Car { t = holder, s = s0, speed = speed, lane = north ? 1.5f : -1.5f, bus = bus, north = north });
        }

        Vector2 At(float s, out Vector2 dir)
        {
            s = Mathf.Clamp(s, 0f, _len);
            for (int i = 1; i < _pts.Count; i++)
                if (s <= _acc[i]) { var a = _pts[i - 1]; var b = _pts[i]; dir = (b - a).normalized; return Vector2.Lerp(a, b, (s - _acc[i - 1]) / Mathf.Max(0.001f, _acc[i] - _acc[i - 1])); }
            dir = (_pts[_pts.Count - 1] - _pts[_pts.Count - 2]).normalized; return _pts[_pts.Count - 1];
        }

        void Update()
        {
            if (_pts == null) return;
            float stopS = VillageEast.RoadLine[0].y - VillageEast.StopZ;   // 첫 구간은 x 고정·z 감소 → 거리 = 140 − z
            BusStopped = false;
            foreach (var c in _cars)
            {
                if (c.wait > 0f) { c.wait -= Time.deltaTime; if (c.bus) BusStopped = true; }
                else
                {
                    float prev = c.s;
                    c.s += (c.north ? -1f : 1f) * c.speed * Time.deltaTime;
                    if (c.bus && !c.north && prev < stopS && c.s >= stopS) { c.s = stopS; c.wait = 5f; }
                    if (c.s > _len) c.s = 0f; if (c.s < 0f) c.s = _len;
                }
                var p = At(c.s, out var d);
                var fwd = c.north ? -d : d; var right = new Vector2(fwd.y, -fwd.x);   // 우측 통행: 진행 방향 오른쪽 차선
                var q = p + right * 1.5f;
                c.t.position = new Vector3(q.x, VillageWorld.Height(q.x, q.y) + 0.12f, q.y);
                c.t.rotation = Quaternion.LookRotation(new Vector3(fwd.x, 0f, fwd.y));
            }
        }
    }
}
