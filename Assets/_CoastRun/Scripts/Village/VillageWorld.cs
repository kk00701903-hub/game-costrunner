using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 136차(사용자: 「동물의 숲 포켓캠프처럼 바닷가 마을」): 스토리 허브 마을의 3D 세계를 절차적으로 세운다.
    /// 언덕 꼭대기 = 주인공 집 + 송전탑, 가운데 마을(엄마 집·구멍가게·텃밭), 남쪽 해변(낚시·등대).
    /// 기존 자산 재활용: JejuKit 모델(제주 초가/기와집·FShop 상가·야자수·귤나무·돌담·돌하르방·벤치…), TransmissionTower 프리팹, 바다 셰이더.
    public static class VillageWorld
    {
        public const float Half = 50f;          // 마을 반폭(m) — 스플랫·산책 영역
        public const float MeshHalf = 120f;     // 137차: 지형 메시는 더 멀리(언덕 위에서 하늘 여백이 보이지 않게)
        public const int Res = 120;             // 지형 격자 수(2m)
        public static Texture2D SplatTex { get; private set; }
        public static int FloraCount;
        /// 147차: 들어갈 수 있는 집 목록(간판 이름, 문 앞 월드 위치)
        public static readonly List<(Transform house, string name, Vector3 door)> Houses = new List<(Transform, string, Vector3)>();
        public const float SeaLevel = -0.9f;

        // 길(언덕 집 → 마을 → 해변) — 그림용·안내용 폴리라인
        public static readonly Vector2[] Path = {
            new Vector2(0f, 31f), new Vector2(-7f, 25f), new Vector2(3f, 18f), new Vector2(-2f, 11f),
            new Vector2(0f, 3f), new Vector2(-5.6f, -4f), new Vector2(-3.4f, -12f), new Vector2(-1.5f, -18f), new Vector2(-2.5f, -24f), new Vector2(-8f, -30f), new Vector2(-11f, -37f), new Vector2(-12f, -47f), new Vector2(-9f, -60f)
        };

        // ── 높이 ─────────────────────────────────────────────────────────
        public static VillageInterior Interior;
        public static float Height(float x, float z)
        {
            if (Interior != null && Interior.Contains(x, z)) return Interior.FloorY;
            float t = Mathf.InverseLerp(6f, 36f, z);
            float ridge = Mathf.SmoothStep(0f, 1f, t) * 11f;
            float xs = Mathf.Exp(-(x * x) / (2f * 24f * 24f));
            float h = ridge * (0.50f + 0.50f * xs);
            if (z < -16f)
            {
                float b = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-16f, -44f, z));
                h -= b * 2.2f;
            }
            h += (Mathf.PerlinNoise(x * 0.07f + 7.3f, z * 0.07f + 3.1f) - 0.5f) * 0.5f;
            // 140차(시안): 오른쪽(−x) 바다 쪽으로 뻗은 절벽 반도 — 파스텔 집·상점·등대가 올라앉는다
            float pen = Peninsula(x, z);
            if (pen > 0f) h = Mathf.Lerp(h, 0.5f + Mathf.PerlinNoise(x * 0.11f + 2f, z * 0.11f + 5f) * 0.4f, pen);
            float bay = Bay(x, z);
            if (bay > 0f) h = Mathf.Lerp(h, SeaLevel - 1.4f, bay);
            // 137차: 마을 밖(±50 너머)은 완만한 구릉으로 올라가 지평선을 가린다(남쪽 바다 쪽은 제외)
            float outside = Mathf.Max(Mathf.Abs(x) - 46f, z - 46f);
            if (outside > 0f && z > -20f)
            {
                float k = Mathf.SmoothStep(0f, 1f, outside / 40f);
                h += k * (7f + 9f * Mathf.PerlinNoise(x * 0.03f + 1.7f, z * 0.03f + 9.2f));
            }
            // 길 위는 살짝 평평하게(걷기 편하게)
            float pd = PathDist(x, z);
            if (pd < 2.2f)
            {
                float pz = Mathf.PerlinNoise(x * 0.07f + 7.3f, z * 0.07f + 3.1f) - 0.5f;
                h -= pz * 0.5f * (1f - pd / 2.2f);
            }
            return h;
        }

        public static float Peninsula(float x, float z)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-2.5f, -6f, x)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-32f, -26f, x));
            k *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-70f, -64f, z)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-25f, -31f, z));
            return k;
        }

        // 140차(시안): 왼쪽(+x) 초가집 뒤로 바다가 들어온 만(灣)
        public static float Bay(float x, float z)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 6f, x)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-25.5f, -29.5f, z));
        }

        public static float PathDist(float x, float z)
        {
            var p = new Vector2(x, z); float best = 999f;
            for (int i = 0; i < Path.Length - 1; i++)
            {
                var a = Path[i]; var b = Path[i + 1]; var ab = b - a;
                float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * k));
            }
            return best;
        }

        public static Vector3 Ground(float x, float z) => new Vector3(x, Height(x, z), z);

        // ── 세우기 ───────────────────────────────────────────────────────
        public static Transform Build(Transform parent, SaveData save)
        {
            var root = new GameObject("VillageWorld").transform;
            root.SetParent(parent, false);
            BuildLight(root);
            BuildTerrain(root, save);
            BuildSea(root);
            BuildVista(root, save);
            BuildProps(root);
            // 147차: 조경 밀도 — 잔디에 꽃·클로버·풀포기 자동 배치(종류별 메시 합침)
            FloraCount = VillageFlora.Scatter(root);
            // 146차: 곡면 가중치 — 하늘·구름·새·먼 산은 고정(0), 거품·반짝임 등 투명 장식은 지면과 같이 휘게(1)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name; var t = r.transform;
                bool flat = n == "SkyDome" || n == "Vista" || n == "VistaSide" || n == "FarHill" || n == "Cloud" || n == "Bird" || (t.parent != null && (t.parent.name == "Cloud" || t.parent.name == "Bird"));
                foreach (var m in r.sharedMaterials) if (m != null && m.HasProperty("_CurveWeight")) m.SetFloat("_CurveWeight", flat ? 0f : 1f);
            }
            return root;
        }

        static void BuildLight(Transform root)
        {
            // 143차: 동물의 숲풍 소프트 룩 — 셰이더 전역(하프 램버트·라벤더 그림자·가는 잉크) + 마을 후처리 볼륨
            VillagePalette.ApplySoftLook(true);
            VillagePalette.BuildPostVolume(root);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.90f); sun.intensity = 1.05f;
            sun.transform.rotation = Quaternion.Euler(50f, 160f, 0f);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.45f; sun.shadowNormalBias = 1.6f; sun.shadowBias = 0.03f;
            // 141차: 남쪽에서 비추는 보조광 — 집 앞면(남향)이 그늘로 죽지 않게(동물의 숲처럼 부드러운 조명)
            var fill = new GameObject("Fill").AddComponent<Light>(); fill.transform.SetParent(root, false);
            fill.type = LightType.Directional; fill.color = new Color(0.90f, 0.94f, 1f); fill.intensity = 0.32f; fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(38f, -25f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = VillagePalette.SkyMid;
            RenderSettings.ambientEquatorColor = new Color(0.90f, 0.88f, 0.86f);
            RenderSettings.ambientGroundColor = new Color(0.66f, 0.68f, 0.56f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f; RenderSettings.fogEndDistance = 210f;   // 146차: 대기 원근 — 등대(~55 m)부터 하늘색에 섞임
            RenderSettings.fogColor = VillagePalette.Fog;
        }

        // 지형: 100×100 격자 + 절차 스플랫 텍스처(잔디·모래·흙길·텃밭·꽃)
        static void BuildTerrain(Transform root, SaveData save)
        {
            int n = Res + 1;
            var verts = new Vector3[n * n]; var uvs = new Vector2[n * n]; var tris = new int[Res * Res * 6];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -MeshHalf + i * (2f * MeshHalf / Res), z = -MeshHalf + j * (2f * MeshHalf / Res);
                    verts[j * n + i] = new Vector3(x, Height(x, z), z);
                    uvs[j * n + i] = new Vector2((x + Half) / (2f * Half), (z + Half) / (2f * Half));   // 스플랫은 ±50, 바깥은 clamp 로 가장자리 색이 이어짐
                }
            int t = 0;
            for (int j = 0; j < Res; j++)
                for (int i = 0; i < Res; i++)
                {
                    int a = j * n + i;
                    tris[t++] = a; tris[t++] = a + n; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = a + n; tris[t++] = a + n + 1;
                }
            var mesh = new Mesh { name = "VillageTerrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            SplatTex = Splat();
            mr.sharedMaterial = ArtAssets.CreateTexturedLit(SplatTex, Color.white, 0.02f);
            // 144차: 디테일 맵(2 m 타일 잔결) — 큰 스플랫 위에 미세 명암을 곱해 가까이서도 표면이 살아 있게
            var dm = DetailTex();
            var tm = mr.sharedMaterial;
            if (tm != null && tm.HasProperty("_DetailMap")) { tm.SetTexture("_DetailMap", dm); tm.SetFloat("_DetailScale", 0.5f); tm.SetFloat("_DetailStrength", 0.16f); }
            mr.receiveShadows = true; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 바깥 울타리(보이지 않는 벽) — 마을 밖으로 못 나가게
            float lim = Half - 4f;
            Wall(root, new Vector3(0f, 5f, lim), new Vector3(2f * Half, 12f, 1f));
            Wall(root, new Vector3(0.5f, 5f, -34f), new Vector3(10f, 12f, 1f)); Wall(root, new Vector3(28f, 5f, -26f), new Vector3(46f, 12f, 1f)); Wall(root, new Vector3(4.5f, 5f, -30f), new Vector3(1f, 12f, 9f));   // 바다 앞(반도 오른쪽은 열어둠)
            // 140차: 절벽 반도 둘레
            Wall(root, new Vector3(-4.2f, 5f, -49f), new Vector3(1f, 12f, 32f));
            Wall(root, new Vector3(-16f, 5f, -65f), new Vector3(30f, 12f, 1f));
            Wall(root, new Vector3(-29f, 5f, -50f), new Vector3(1f, 12f, 32f));
            Wall(root, new Vector3(-40f, 5f, -34f), new Vector3(22f, 12f, 1f));
            Wall(root, new Vector3(lim, 5f, 0f), new Vector3(1f, 12f, 2f * Half));
            Wall(root, new Vector3(-lim, 5f, 0f), new Vector3(1f, 12f, 2f * Half));
        }

        static void Wall(Transform root, Vector3 c, Vector3 size)
        {
            var w = new GameObject("Bound", typeof(BoxCollider)); w.transform.SetParent(root, false);
            w.transform.localPosition = c; w.GetComponent<BoxCollider>().size = size;
        }

        static Texture2D Splat()
        {
            const int S = 2048;   // 144차: 1024(10 px/m)→2048(20 px/m) — 발밑 잔디가 뭉개지지 않게
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            Color grassA = VillagePalette.Grass, grassB = VillagePalette.GrassShade, grassC = VillagePalette.GrassLight;
            Color sand = VillagePalette.Sand, sandWet = VillagePalette.SandWet;
            Color path = VillagePalette.Path, pathEdge = VillagePalette.PathEdge;
            Color soil = new Color(0.45f, 0.31f, 0.20f), rock = new Color(0.62f, 0.60f, 0.56f);
            var rng = new System.Random(1234);
            for (int j = 0; j < S; j++)
                for (int i = 0; i < S; i++)
                {
                    float x = -Half + (i + 0.5f) / S * 2f * Half, z = -Half + (j + 0.5f) / S * 2f * Half;
                    float h = Height(x, z);
                    float n1 = Mathf.PerlinNoise(x * 0.35f + 11f, z * 0.35f + 5f), n2 = Mathf.PerlinNoise(x * 1.7f + 3f, z * 1.7f + 9f);
                    Color c = Color.Lerp(Color.Lerp(grassA, grassB, n1), grassC, n2 * 0.35f);
                    // 149차(사용자: 「잔디가 먹먹하다」): 잔디 결 — 잔 노이즈로 밝은 연두 포기·진한 그늘 점을 섞어 채도·명암 대비를 올림
                    float n3 = Mathf.PerlinNoise(x * 5.5f + 71f, z * 5.5f + 23f), n4 = Mathf.PerlinNoise(x * 11f + 7f, z * 11f + 99f);
                    c = Color.Lerp(c, new Color(0.70f, 0.90f, 0.42f), Mathf.Clamp01((n3 - 0.55f) * 3.2f) * 0.42f);
                    c = Color.Lerp(c, new Color(0.40f, 0.66f, 0.30f), Mathf.Clamp01((0.42f - n4) * 3.5f) * 0.45f);
                    // 언덕 정상부 바위 얼룩
                    if (h > 8.5f) c = Color.Lerp(c, rock, Mathf.Clamp01((h - 8.5f) / 2.5f) * n2 * 0.9f);
                    // 해변 모래
                    float penK = Peninsula(x, z);
                    float sandK = Mathf.Clamp01(Mathf.InverseLerp(-27f, -31f, z) + (h < -0.25f ? 0.6f : 0f) + Bay(x, z + 2.5f) * 0.9f) * (1f - penK);
                    sandK = Mathf.Clamp01(sandK + (n1 - 0.5f) * 0.3f);
                    c = Color.Lerp(c, Color.Lerp(sand, sandWet, Mathf.Clamp01(Mathf.InverseLerp(-27f, -34f, z))), sandK);
                    // 138차: 붓결 디테일(방향성 있는 줄무늬 노이즈) — 잔디는 세로 결, 모래는 가로 결
                    float stroke = Mathf.PerlinNoise(x * 2.6f + 31f, z * 0.6f + 17f) * 0.5f + Mathf.PerlinNoise(x * 0.5f + 5f, z * 2.8f + 41f) * 0.5f;
                    float strokeK = sandK < 0.5f ? 0.16f : 0.10f;
                    c = Color.Lerp(c, c * (0.86f + stroke * 0.28f), strokeK / 0.16f);
                    // 모래↔잔디 경계에 얇은 진한 선(만화 윤곽)
                    float edgeSand = Mathf.Abs(sandK - 0.5f);
                    if (edgeSand < 0.06f) c = Color.Lerp(c, new Color(0.55f, 0.48f, 0.30f), 0.55f * (1f - edgeSand / 0.06f));
                    // 흙길
                    float pd = PathDist(x, z);
                    float w = 1.7f + (n1 - 0.5f) * 0.6f;
                    if (pd < w + 0.5f) c = Color.Lerp(c, pd < w ? Color.Lerp(path, pathEdge, n2 * 0.5f) : pathEdge, pd < w ? 1f : (w + 0.5f - pd) / 0.5f);
                    if (pd < w)
                    {
                        // 149차(사용자: 「도로를 명확히」): 가운데가 밝고 가장자리로 갈수록 진한 모래길 + 자갈 점 + 바퀴 자국 결
                        c = Color.Lerp(Color.Lerp(path, Color.white, 0.10f), Color.Lerp(path, pathEdge, 0.6f), Mathf.Pow(pd / w, 1.6f));
                        float pebble = Mathf.PerlinNoise(x * 9f + 3f, z * 9f + 61f);
                        if (pebble > 0.72f) c = Color.Lerp(c, new Color(0.62f, 0.53f, 0.40f), Mathf.Clamp01((pebble - 0.72f) * 6f) * 0.85f);
                        if (pebble < 0.30f) c = Color.Lerp(c, Color.Lerp(path, Color.white, 0.18f), 0.35f);
                    }
                    if (pd > w - 0.14f && pd < w + 0.26f) c = Color.Lerp(c, new Color(0.46f, 0.34f, 0.22f), 0.8f);   // 길 가장자리 진한 선(굵게)
                    else if (pd >= w + 0.26f && pd < w + 0.55f) c = Color.Lerp(c, new Color(0.62f, 0.80f, 0.36f), 0.35f);   // 바깥 밝은 풀 테두리
                    // 텃밭(0,-6) 8×7 — 140차: 시안처럼 3×3 밭 격자
                    if (Mathf.Abs(x) < 4.0f && Mathf.Abs(z + 6f) < 3.5f)
                    {
                        bool ridge = Mathf.Repeat(x + 4f, 2.66f) < 0.28f || Mathf.Repeat(z + 9.5f, 2.33f) < 0.28f;
                        c = ridge ? new Color(0.55f, 0.42f, 0.28f) : Color.Lerp(soil, Color.Lerp(soil, Color.black, 0.25f), n2 * 0.4f);
                    }
                    // 마당(언덕 집 앞) 잔디는 밝게
                    if (Mathf.Abs(x) < 9f && z > 24f && z < 33f) c = Color.Lerp(c, grassC, 0.35f);
                    // 꽃 점
                    if (sandK < 0.2f && pd > w + 0.6f && rng.NextDouble() < 0.012)
                        c = rng.NextDouble() < 0.5 ? new Color(1f, 0.72f, 0.82f) : rng.NextDouble() < 0.5 ? new Color(1f, 0.95f, 0.65f) : Color.white;
                    px[j * S + i] = c;
                }
            tex.SetPixels32(px); tex.Apply(true, false);
            tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 16; tex.mipMapBias = -0.35f;   // 144차: 2048 이라 바이어스는 -0.35 면 충분(과하면 도트가 튄다)
            return tex;
        }

        static Texture2D _detail;
        /// 타일 가능한 부드러운 노이즈(256) — 잔디·모래 결. 회색 0.5 중심, ±0.5
        static Texture2D DetailTex()
        {
            if (_detail != null) return _detail;
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, name = "VillageDetail" };
            var px = new Color32[S * S];
            for (int j = 0; j < S; j++) for (int i = 0; i < S; i++)
            {
                // 주기적(타일) 노이즈: 두 옥타브를 원환면(torus) 좌표로 샘플
                float u = i / (float)S * Mathf.PI * 2f, v = j / (float)S * Mathf.PI * 2f;
                float nx = Mathf.Cos(u) * 3f + 10f, ny = Mathf.Sin(u) * 3f + 10f, nz = Mathf.Cos(v) * 3f + 20f, nw = Mathf.Sin(v) * 3f + 20f;
                float n = Mathf.PerlinNoise(nx + nz, ny + nw) * 0.6f + Mathf.PerlinNoise((nx + nz) * 2.3f + 5f, (ny + nw) * 2.3f + 7f) * 0.4f;
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(n * 255f), 0, 255);
                px[j * S + i] = new Color32(g, g, g, 255);
            }
            t.SetPixels32(px); t.Apply(true, false); _detail = t; return t;
        }

        // 바다: 큰 판 + 파도 흔들림(CoastSea 방식 축약)
        static void BuildSea(Transform root)
        {
            var go = new GameObject("Sea", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(0f, SeaLevel, -20f);
            int sx = 40, sz = 30; float w = 420f, len = 300f;
            var verts = new Vector3[(sx + 1) * (sz + 1)]; var uvs = new Vector2[verts.Length]; var tris = new int[sx * sz * 6];
            for (int j = 0; j <= sz; j++) for (int i = 0; i <= sx; i++)
            { float fx = i / (float)sx, fz = j / (float)sz; verts[j * (sx + 1) + i] = new Vector3(fx * w - w * 0.5f, 0f, -fz * len); uvs[j * (sx + 1) + i] = new Vector2(fx * 22f, fz * 16f); }
            int t = 0;
            for (int j = 0; j < sz; j++) for (int i = 0; i < sx; i++)
            { int a = j * (sx + 1) + i; tris[t++] = a; tris[t++] = a + 1; tris[t++] = a + sx + 1; tris[t++] = a + 1; tris[t++] = a + sx + 2; tris[t++] = a + sx + 1; }
            var mesh = new Mesh { name = "VillageSea" }; mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris; mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var seaTex = ArtAssets.LoadTexture("Sea_Turquoise_Tile");
            var mat = CoastMaterials.CreateToon(VillagePalette.SeaDeep, seaTex, 0.40f);
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<SeaWaves>();
            // 물가 거품 띠
            var foam = GameObject.CreatePrimitive(PrimitiveType.Quad); foam.name = "Foam"; Object.Destroy(foam.GetComponent<Collider>());
            foam.transform.SetParent(root, false); foam.transform.localPosition = new Vector3(0f, SeaLevel + 0.06f, -31.5f);
            foam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); foam.transform.localScale = new Vector3(110f, 2.2f, 1f);
            var fm = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.32f)); foam.GetComponent<MeshRenderer>().sharedMaterial = fm;
            foam.AddComponent<FoamPulse>();
        }

        public class SeaWaves : MonoBehaviour
        {
            Mesh _m; Vector3[] _base; float _t; Material _mat;
            void Start() { _m = GetComponent<MeshFilter>().sharedMesh; _base = _m.vertices; _mat = GetComponent<MeshRenderer>().sharedMaterial; }
            void Update()
            {
                if (_m == null) return;
                _t += Time.deltaTime;
                var v = _m.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    var b = _base[i];
                    v[i] = b + Vector3.up * (Mathf.Sin(b.x * 0.18f + _t * 1.4f) * 0.10f + Mathf.Sin(b.z * 0.13f - _t * 1.1f) * 0.08f);
                }
                _m.vertices = v;
                if (_mat != null)
                {
                    var off = new Vector2(Mathf.Sin(_t * 0.15f) * 0.03f, _t * 0.02f);
                    if (_mat.HasProperty("_BaseMap")) _mat.SetTextureOffset("_BaseMap", off); else _mat.mainTextureOffset = off;
                }
            }
        }
        public class FoamPulse : MonoBehaviour
        {
            Vector3 _p; void Start() { _p = transform.localPosition; }
            void Update() { transform.localPosition = _p + new Vector3(0f, 0f, Mathf.Sin(Time.time * 0.9f) * 0.9f); }
        }

        // 먼 풍경(클링 비스타) — 언덕 뒤 하늘
        static void BuildVista(Transform root, SaveData save)
        {
            string season = save != null ? Timeline.SeasonOf(save.week) switch { SeasonKind.Spring => "SPRING", SeasonKind.Summer => "NOON", SeasonKind.Autumn => "AUTUMN", _ => "WINTER" } : "NOON";
            var tex = ArtAssets.LoadTexture("Sky_Village_" + season) ?? ArtAssets.LoadTexture("Sky_Coast_" + season) ?? ArtAssets.LoadTexture("Sky_Coast_NOON");
            BuildSkyDome(root);
            if (tex == null) return;
            // 140차: 평면 비스타 그림 대신 하늘 돔(BuildSkyDome)으로 교체 — 세로 화면에서 그림 수평선이 안 보이던 문제
            BuildSkyDressing(root);
            // 137차: 먼 산 능선(안개색 구릉) — 언덕 위에서도 파란 여백이 남지 않게
            var mist = CoastMaterials.CreateLit(new Color(0.62f, 0.74f, 0.86f));
            for (int i = 0; i < 18; i++)
            {
                float ang = i * 20f * Mathf.Deg2Rad; if (Mathf.Sin(ang) < -0.35f) continue;   // 남쪽(바다)은 비움
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(hill.GetComponent<Collider>()); hill.name = "FarHill";
                hill.transform.SetParent(root, false);
                float r = 150f + (i % 3) * 18f;
                hill.transform.localPosition = new Vector3(Mathf.Cos(ang) * r, -6f, Mathf.Sin(ang) * r);
                hill.transform.localScale = new Vector3(120f + (i % 4) * 25f, 34f + (i % 3) * 12f, 90f);
                hill.GetComponent<MeshRenderer>().sharedMaterial = mist;
            }
        }

        /// 140차: 구름·갈매기·바다 반짝임(시안 분위기)
        // 140차(시안): 하늘 그라데이션 돔 — 위는 파랑, 수평선은 연한 하늘색, 수평선 아래는 먼바다색으로 이어 붙인다
        static void BuildSkyDome(Transform root)
        {
            var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "SkyDomeGrad" };
            Color seaFar = VillagePalette.SeaDeep, horizon = VillagePalette.SkyHorizon, mid = VillagePalette.SkyMid, top = VillagePalette.SkyTop;
            for (int y = 0; y < 256; y++)
            {
                float v = y / 255f; Color c;
                // 146차: 곡면 월드에선 바다 메시가 수평선 아래로 휘어 내려가므로 돔은 눈높이 아래도 하늘(안개색)로 —
                // 바다색 띠를 돔에 그리면 휘어 내려간 바다 뒤로 파란 벽이 남는다
                if (v < 0.50f) c = Color.Lerp(VillagePalette.Fog, horizon, Mathf.Clamp01((v - 0.30f) / 0.20f));
                else if (v < 0.54f) c = Color.Lerp(horizon, mid, (v - 0.50f) / 0.04f);
                else c = Color.Lerp(mid, top, Mathf.Clamp01((v - 0.54f) / 0.25f));
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(dome.GetComponent<Collider>()); dome.name = "SkyDome";
            dome.transform.SetParent(root, false); dome.transform.localPosition = new Vector3(0f, 2f, 0f); dome.transform.localScale = new Vector3(-780f, 780f, 780f);
            var m = CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(Color.white)); m.mainTexture = tex;
            var mr = dome.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        static void BuildSkyDressing(Transform root)
        {
            var cloudM = CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(new Color(0.94f, 0.97f, 1f)));
            var rng = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                var c = new GameObject("Cloud").transform; c.SetParent(root, false);
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f), r = 140f + (float)rng.NextDouble() * 120f;
                if (i < 5) { ang = -Mathf.PI * 0.5f + (float)(rng.NextDouble() - 0.5) * 1.6f; r = 190f + (float)rng.NextDouble() * 90f; }
                c.localPosition = new Vector3(Mathf.Cos(ang) * r, 34f + (float)rng.NextDouble() * 30f, Mathf.Sin(ang) * r);
                int n = 3 + rng.Next(3);
                for (int k = 0; k < n; k++)
                {
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(b.GetComponent<Collider>()); b.transform.SetParent(c, false);
                    float sx = 5f + (float)rng.NextDouble() * 7f;
                    b.transform.localPosition = new Vector3((k - n * 0.5f) * 9f, (float)rng.NextDouble() * 3f, (float)(rng.NextDouble() - 0.5) * 6f); b.transform.localScale = new Vector3(sx, sx * 0.45f, sx * 0.7f);
                    b.GetComponent<MeshRenderer>().sharedMaterial = cloudM;
                }
                c.gameObject.AddComponent<CloudDrift>();
            }
            var birdM = CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(new Color(0.20f, 0.22f, 0.30f)));
            for (int i = 0; i < 10; i++)
            {
                var b = new GameObject("Bird").transform; b.SetParent(root, false);
                for (int k = -1; k <= 1; k += 2)
                {
                    var wgo = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(wgo.GetComponent<Collider>()); wgo.transform.SetParent(b, false);
                    wgo.transform.localPosition = new Vector3(k * 0.55f, 0.18f, 0f); wgo.transform.localScale = new Vector3(1.1f, 0.08f, 0.16f); wgo.transform.localRotation = Quaternion.Euler(0f, 0f, k * 22f);
                    wgo.GetComponent<MeshRenderer>().sharedMaterial = birdM;
                }
                var fl = b.gameObject.AddComponent<BirdFly>(); fl.center = new Vector3((float)rng.NextDouble() * 30f - 12f, 9f + (float)rng.NextDouble() * 7f, -34f - (float)rng.NextDouble() * 30f); fl.radius = 5f + (float)rng.NextDouble() * 9f; b.localScale = Vector3.one * 0.85f; fl.phase = (float)rng.NextDouble() * 6f; fl.speed = 0.25f + (float)rng.NextDouble() * 0.2f;
            }
            var sparkM = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.85f));
            for (int i = 0; i < 70; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "Sparkle"; q.transform.SetParent(root, false);
                q.transform.localPosition = new Vector3((float)(rng.NextDouble() - 0.5) * 220f, SeaLevel + 0.25f, -36f - (float)rng.NextDouble() * 110f);
                q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(0.9f + (float)rng.NextDouble() * 1.4f, 0.35f, 1f);
                q.GetComponent<MeshRenderer>().sharedMaterial = sparkM;
                var tw = q.AddComponent<Twinkle>(); tw.phase = (float)rng.NextDouble() * 6f;
            }
        }
        public class CloudDrift : MonoBehaviour { Vector3 _p; float _ph; void Start() { _p = transform.localPosition; _ph = Random.value * 6f; } void Update() { transform.localPosition = _p + new Vector3(Mathf.Sin(Time.time * 0.05f + _ph) * 8f, 0f, 0f); } }
        public class BirdFly : MonoBehaviour
        {
            public Vector3 center; public float radius = 12f, phase, speed = 0.3f;
            void Update()
            {
                float t = Time.time * speed + phase;
                var p = center + new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t * 2.3f) * 1.5f, Mathf.Sin(t) * radius * 0.6f);
                var d = p - transform.position; transform.position = p; if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d, Vector3.up);
                float flap = Mathf.Sin(Time.time * 9f + phase) * 28f;
                for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? -1f : 1f) * (22f + flap));
            }
        }
        public class Twinkle : MonoBehaviour { public float phase; Vector3 _s; void Start() { _s = transform.localScale; } void Update() { float k = Mathf.Max(0f, Mathf.Sin(Time.time * 2.2f + phase)); transform.localScale = new Vector3(_s.x * k, _s.y * k, 1f); } }

        // ── 소품 ─────────────────────────────────────────────────────────
        public static readonly List<Transform> Trees = new List<Transform>();
        public static Transform HeroHouse, MomHouse, Shop, Tower, Lighthouse;

        static void BuildProps(Transform root)
        {
            Trees.Clear(); Houses.Clear();
            // 언덕 꼭대기: 주인공 집(제주 기와집) + 송전탑
            HeroHouse = VillageHouses.Build(root, Ground(0f, 36f), 180f, VillageHouses.Style.Hero, "우리집", "Our home"); Reg(HeroHouse, "우리집");
            Tower = PlaceTower(root, 15f, 41f);
            // 마을: 엄마 집(초가) · 구멍가게(그림 파사드 상가)
            // 140차(시안): 텃밭(0,-6)에서 바다 쪽을 보면 왼쪽에 초가(엄마 집), 오른쪽 아래로 파스텔 집들 + 상점, 절벽 위 등대
            // (세로 화면은 가로 시야가 좁아(±18°) 텃밭에서 남쪽으로 보이는 원뿔 안에 배치)
            MomHouse = VillageHouses.Build(root, Ground(4.8f, -21.5f), 25f, VillageHouses.Style.Mom, "엄마 집", "Mom's", null, null, 1.1f); Reg(MomHouse, "엄마 집");
            Shop = VillageHouses.Build(root, Ground(-5.8f, -33.5f), 20f, VillageHouses.Style.Shop, "해변 상점", "Beach shop", null, null, 0.75f);
            // 147차: 해녀네는 상점 바로 뒤(북)·바다(동)에 막혀 → 서쪽 골목(꽃집 쪽 길)을 보게 돌려 문 앞에 설 자리를 만든다
            Reg(VillageHouses.Build(root, Ground(-5.2f, -39.5f), -80f, VillageHouses.Style.Pastel, "해녀네", "Haenyeo's", VillagePalette.WallCream, VillagePalette.RoofMint, 0.75f), "해녀네");
            Reg(VillageHouses.Build(root, Ground(-8.5f, -43f), 5f, VillageHouses.Style.Pastel, "등대지기 집", "Keeper's", new Color(0.93f, 0.96f, 1f), VillagePalette.RoofSky, 0.75f), "등대지기 집");
            Reg(VillageHouses.Build(root, Ground(-12.5f, -39f), -8f, VillageHouses.Style.Pastel, "꽃집", "Flower shop", VillagePalette.WallCream, VillagePalette.RoofPink, 0.7f), "꽃집");
            Reg(VillageHouses.Build(root, Ground(-9.5f, -51f), -5f, VillageHouses.Style.Pastel, "바다 카페", "Sea cafe", new Color(1f, 0.94f, 0.95f), VillagePalette.RoofPink, 0.75f), "바다 카페");
            Reg(VillageHouses.Build(root, Ground(-6.2f, -46.5f), -15f, VillageHouses.Style.Pastel, "서퍼 하우스", "Surf house", new Color(1f, 0.97f, 0.88f), VillagePalette.RoofCream, 0.7f), "서퍼 하우스");
            // 정자·벤치·돌하르방·귤 매대
            Place(root, "Prop_Pavilion", 28f, -6f, 200f, 1f);
            Place(root, "Prop_Bench", -3f, -24f, -90f, 1f); Place(root, "Prop_Bench", -11f, -42f, 60f, 1f);
            Place(root, "Prop_Hareubang", 6.5f, 2f, 200f, 1f); Place(root, "Prop_Hareubang", -6.5f, 2f, 160f, 1f);
            Place(root, "Prop_OrangeStall", 12f, 8f, 240f, 1f);
            Place(root, "Kerb_Onggi", -16f, -4f, 0f, 1f); Place(root, "Kerb_PlanterWood", -15f, -13f, 20f, 1f);
            Place(root, "Kerb_Buoys", -1f, -32f, 0f, 1f); Place(root, "Kerb_CrateStack", 3f, -32f, 30f, 1f);
            Place(root, "Prop_UtilityPole", 9f, 4f, 0f, 1f); Place(root, "Prop_UtilityPole", 26f, -8f, 0f, 1f);
            // 돌담(길가·집 앞)
            // 141차: 현무암 텍스처는 마을 조명에서 새카만 상자로 보여서 밝은 돌색으로
            var stoneM = CoastMaterials.CreateLit(new Color(0.66f, 0.63f, 0.58f));
            void StoneWall(float x, float z) { var t = Place(root, "Prop_StoneWall", x, z, 0f, 1f, false); if (t == null) return; foreach (var r in t.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = stoneM; r.sharedMaterials = arr; } }
            for (int i = 0; i < 4; i++) StoneWall(-9f + i * 4.2f, 27.5f);
            for (int i = 0; i < 3; i++) StoneWall(-20f + i * 4.2f, 2f);
            for (int i = 0; i < 3; i++) StoneWall(12f + i * 4.2f, 5f);
            // 나무: 귤나무(흔들면 귤) + 야자수(시안처럼 텃밭 주변·바다 쪽에 여럿)
            float[,] oranges = { { -9f, 15f }, { 11f, 14f }, { 24f, 6f }, { -27f, 8f }, { -24f, -14f }, { 30f, -2f }, { -7f, 33f }, { 8f, 32f } };
            for (int i = 0; i < oranges.GetLength(0); i++) { var tr = Place(root, "Prop_OrangeTree", oranges[i, 0], oranges[i, 1], i * 47f, 1f); if (tr != null) { tr.name = "Tree_Orange_" + i; Trees.Add(tr); GroundBlob.Static(root, tr.position, 2.2f, 1.7f, 0.22f); } }
            float[,] palms = { { -2.8f, -20.5f, 0.62f }, { 5.5f, -27.5f, 0.7f }, { 8.5f, -27f, 0.75f }, { 11.5f, -26.5f, 0.7f }, { 2.5f, -30.5f, 0.6f }, { -11f, -36f, 0.8f }, { -13f, -50f, 0.8f }, { -4f, -44.5f, 0.7f }, { -10f, -60f, 0.8f }, { 11f, -14f, 0.9f }, { -20f, -44f, 0.9f }, { -4f, -62f, 0.7f }, { 9.5f, -19f, 0.85f } };
            // 149차: 시안식 절차 야자수(마디 줄기·코코넛·잎) — Prop_Palm FBX 대신
            for (int i = 0; i < palms.GetLength(0); i++) { var tr = VillageHouses.Palm(root, Ground(palms[i, 0], palms[i, 1]), i * 63f, 5.2f * palms[i, 2]); tr.name = "Tree_Palm_" + i; Trees.Add(tr); GroundBlob.Static(root, tr.position, 1.4f, 1.1f, 0.22f); }   // 150차: 야자수도 흔들기(코코넛)·도끼(장작) 대상
            // 등대(절차) — 바다 쪽 절벽 위(시안: 오른쪽 가운데 수평선)
            BuildCliff(root, -7f, -66f);
            Lighthouse = BuildLighthouse(root, -7f, -66f); Lighthouse.localScale = Vector3.one * 0.65f;
            BuildPeninsulaRocks(root);
            // 이정표
            BuildSignpost(root, -6f, -9.5f);
            // 텃밭: 통나무 울타리 + 3×3 밭 + 새싹 + 물웅덩이
            BuildGarden(root);
            // 갈대·바위·꽃무리(시안 채우기)
            var rngD = new System.Random(140);
            float[,] pampas = { { -5.2f, -2f }, { 5.4f, -2.5f }, { 5.5f, -11f }, { -5.5f, -12.5f }, { 3.4f, -15.5f }, { -4.2f, -21f }, { 2.6f, -26f }, { -6f, -28f }, { 3.2f, -13f }, { -0.8f, -28f }, { 6.5f, -17f }, { -7f, -15f }, { -4.6f, -14.2f }, { 4.4f, -12.6f } };
            for (int i = 0; i < pampas.GetLength(0); i++) Pampas(root, pampas[i, 0], pampas[i, 1], rngD);
            float[,] flowers = { { -4.8f, -10.5f }, { 4.8f, -10.5f }, { 2.4f, -14.2f }, { -3.6f, -16f }, { 1.6f, -22.5f }, { -1.6f, -22f }, { 5.5f, -16f } };
            for (int i = 0; i < flowers.GetLength(0); i++) FlowerClump(root, flowers[i, 0], flowers[i, 1], rngD);
            // 가로등·덤불·바위·꽃밭(포켓캠프풍 채우기)
            Lamp(root, -4f, -7f); Lamp(root, -6f, 7f); Lamp(root, 7f, 21f); Lamp(root, -9f, -20f); Lamp(root, 20f, -8f);
            var rng = new System.Random(77);
            int placed = 0, tries = 0;
            while (placed < 26 && tries++ < 400)
            {
                float x = (float)(rng.NextDouble() * 84 - 42), z = (float)(rng.NextDouble() * 78 - 36);
                if (PathDist(x, z) < 3.2f || z < -17f && Height(x, z) < 0.5f) continue;
                if (Mathf.Abs(x) < 6f && Mathf.Abs(z + 6f) < 5.5f) continue;   // 텃밭
                if (Mathf.Abs(x) < 10f && z > 24f) continue;                        // 마당
                if (NearBuilding(x, z)) continue;
                if (rng.NextDouble() < 0.7) Bush(root, x, z, rng); else Rock(root, x, z, rng);
                placed++;
            }
            FlowerBed(root, -12f, 6f, rng); FlowerBed(root, 11f, -9f, rng); FlowerBed(root, 4f, 28f, rng); FlowerBed(root, -6f, 27f, rng);
            OutlineProps(root);
        }

        /// 138차(사용자: 「윤곽을 또렷하게」): 지형·바다·하늘을 뺀 모든 소품에 잉크 윤곽 셸(InkOutline, Cull Front).
        public static void OutlineProps(Transform root)
        {
            foreach (Transform ch in root)
            {
                string n = ch.name;
                if (n == "Terrain" || n == "Sea" || n == "Foam" || n == "Vista" || n == "VistaSide" || n == "FarHill" || n == "Bound" || n == "Sun" || n == "Fill" || n == "Crops" || n == "Yard" || n == "Ghost" || n == "Pampas" || n == "FlowerClump" || n == "Sparkle" || n == "Cloud" || n == "Bird" || n == "SkyDome" || n == "CliffRock" || n.Contains("StoneWall")) continue;
                BuildingOutline.Attach(ch, 0.022f);
            }
        }

        public static bool NearBuildingPublic(float x, float z, float r)
        {
            foreach (var c in BuildingCenters) if (Vector2.Distance(new Vector2(x, z), c) < r) return true;
            return false;
        }
        static bool NearBuilding(float x, float z)
        {
            foreach (var c in BuildingCenters) if (Vector2.Distance(new Vector2(x, z), c) < 6.5f) return true;
            return false;
        }

        static readonly Vector2[] BuildingCenters = { new Vector2(0f, 36f), new Vector2(4.8f, -21.5f), new Vector2(-5.8f, -33.5f), new Vector2(15f, 41f), new Vector2(28f, -6f), new Vector2(-5.2f, -39.5f), new Vector2(-8.5f, -43f), new Vector2(-12.5f, -39f), new Vector2(-9.5f, -51f), new Vector2(-6.2f, -46.5f), new Vector2(0f, -6f) };

        static void Lamp(Transform root, float x, float z)
        {
            var go = new GameObject("Lamp"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(pole.GetComponent<Collider>()); pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.5f, 0f); pole.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.22f, 0.24f, 0.30f));
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(head.GetComponent<Collider>()); head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 3.1f, 0f); head.transform.localScale = Vector3.one * 0.42f;
            head.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(1f, 0.93f, 0.70f));
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 1.5f, 0f); bc.size = new Vector3(0.4f, 3f, 0.4f);
        }

        static void Bush(Transform root, float x, float z, System.Random rng)
        {
            var go = new GameObject("Bush"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            int n = 2 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(b.GetComponent<Collider>()); b.transform.SetParent(go.transform, false);
                float s = 0.7f + (float)rng.NextDouble() * 0.6f;
                b.transform.localPosition = new Vector3((float)(rng.NextDouble() - 0.5) * 1.1f, s * 0.38f, (float)(rng.NextDouble() - 0.5) * 1.1f); b.transform.localScale = new Vector3(s, s * 0.8f, s);
                float g = 0.55f + (float)rng.NextDouble() * 0.2f;
                b.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.30f + (float)rng.NextDouble() * 0.1f, g, 0.30f));
            }
            if (rng.NextDouble() < 0.5)
                for (int i = 0; i < 3; i++)
                {
                    var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(f.GetComponent<Collider>()); f.transform.SetParent(go.transform, false);
                    f.transform.localPosition = new Vector3((float)(rng.NextDouble() - 0.5) * 1.2f, 0.55f + (float)rng.NextDouble() * 0.3f, (float)(rng.NextDouble() - 0.5) * 1.2f); f.transform.localScale = Vector3.one * 0.16f;
                    f.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(rng.NextDouble() < 0.5 ? new Color(1f, 0.55f, 0.70f) : new Color(1f, 0.92f, 0.45f));
                }
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 0.4f, 0f); bc.size = new Vector3(1.3f, 0.8f, 1.3f);
        }

        // 140차(시안): 반도 가장자리 절벽 바위
        static void BuildPeninsulaRocks(Transform root)
        {
            var rockM = CoastMaterials.CreateLit(VillagePalette.Rock); var rockD = CoastMaterials.CreateLit(VillagePalette.RockShade);
            var rng = new System.Random(141);
            void R(float x, float z, float s, Material m)
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(r.GetComponent<Collider>()); r.name = "CliffRock"; r.transform.SetParent(root, false);
                r.transform.position = new Vector3(x, SeaLevel + s * 0.12f, z); r.transform.localScale = new Vector3(s * 1.3f, s * 1.3f, s * 1.1f); r.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                r.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            for (float z = -38f; z > -64f; z -= 2.4f) { float s = 1.2f + (float)rng.NextDouble() * 0.7f; R(-3.6f + (float)rng.NextDouble() * 0.6f - (z < -46f ? 0.8f : 0f), z, s, (z % 4.8f) > -2.4f ? rockM : rockD); }
            // 반도 근처 가장자리는 덤불로(시안: 오른쪽 절벽 위 초록)
            for (float z = -27f; z > -38f; z -= 3.6f) Bush(root, -2.8f + (float)rng.NextDouble() * 1.0f, z, rng);
            for (float x = -3f; x > -28f; x -= 3f) { float s = 2.4f + (float)rng.NextDouble() * 1.6f; R(x, -63.5f + (float)rng.NextDouble(), s, rockM); }
            for (float z = -31f; z > -62f; z -= 3.2f) { float s = 2.2f + (float)rng.NextDouble() * 1.4f; R(-28.5f, z, s, rockD); }
            // 만(灣) 쪽 물가 바위
            R(6.8f, -27.5f, 1.3f, rockD); R(10.5f, -28f, 1.6f, rockM); R(4.2f, -29f, 1.1f, rockM);
        }

        static void Rock(Transform root, float x, float z, System.Random rng)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Sphere); r.name = "Rock"; r.transform.SetParent(root, false);
            float s = 0.6f + (float)rng.NextDouble() * 0.9f;
            r.transform.position = Ground(x, z) + new Vector3(0f, s * 0.2f, 0f); r.transform.localScale = new Vector3(s, s * 0.55f, s * 0.8f); r.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            r.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(VillagePalette.RockShade);
        }

        static void FlowerBed(Transform root, float x, float z, System.Random rng)
        {
            var go = new GameObject("FlowerBed"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(bed.GetComponent<Collider>()); bed.transform.SetParent(go.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.06f, 0f); bed.transform.localScale = new Vector3(2.6f, 0.08f, 1.8f);
            bed.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.42f, 0.30f, 0.20f));
            Color[] cols = { new Color(1f, 0.55f, 0.70f), new Color(1f, 0.92f, 0.45f), Color.white, new Color(0.70f, 0.55f, 0.95f) };
            for (int i = 0; i < 12; i++)
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(f.GetComponent<Collider>()); f.transform.SetParent(go.transform, false);
                f.transform.localPosition = new Vector3((float)(rng.NextDouble() - 0.5) * 2.2f, 0.30f, (float)(rng.NextDouble() - 0.5) * 1.4f); f.transform.localScale = Vector3.one * 0.22f;
                f.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(cols[rng.Next(cols.Length)]);
                var st = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(st.GetComponent<Collider>()); st.transform.SetParent(go.transform, false);
                st.transform.localPosition = f.transform.localPosition - new Vector3(0f, 0.14f, 0f); st.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
                st.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.35f, 0.62f, 0.28f));
            }
        }

        /// 모델별 목표 높이(m) — 키트 모델은 러닝 도로용 단위라 마을·치비(1.25m)에 맞춰 줄인다.
        public static float TargetHeight(string model)
        {
            if (model.StartsWith("JHouse")) return 3.8f;
            if (model.StartsWith("House_")) return 3.8f;
            if (model.StartsWith("FShop") || model.StartsWith("Shop_") || model.StartsWith("Bldg_")) return 4.6f;
            switch (model)
            {
                case "Prop_StoneWall": return 1.0f;
                case "Prop_OrangeTree": return 3.0f;
                case "Prop_Palm": return 5.2f;
                case "Prop_Bench": return 0.9f;
                case "Prop_Pavilion": return 3.6f;
                case "Prop_Hareubang": return 1.6f;
                case "Prop_OrangeStall": return 2.2f;
                case "Prop_CafeSet": return 1.3f;
                case "Prop_UtilityPole": return 6.0f;
                case "Kerb_PlanterWood": return 0.7f;
                case "Kerb_PlanterBasalt": return 0.6f;
                case "Kerb_Onggi": return 0.9f;
                case "Kerb_Buoys": return 0.8f;
                case "Kerb_CrateStack": return 1.1f;
                case "Kerb_StoneBed": return 0.5f;
            }
            return 1.5f;
        }

        /// 렌더러 경계 높이를 목표 높이로 맞추고, 바닥(경계 최저점)이 지면에 닿게 내린다.
        public static void Normalize(GameObject go, float targetH, float groundY)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float h = Mathf.Max(0.01f, b.size.y);
            float k = targetH / h;
            go.transform.localScale = go.transform.localScale * k;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var p = go.transform.position; p.y += groundY - b.min.y; go.transform.position = p;
        }

        public static GameObject SpawnScaled(string model, Transform root, float x, float z, float yaw, float? targetH = null)
        {
            var g = Ground(x, z);
            var go = JejuKit.Spawn(model, root, g, yaw, 1f);
            if (go == null) return null;
            Normalize(go, targetH ?? TargetHeight(model), g.y - 0.04f);
            return go;
        }

        static Transform Place(Transform root, string model, float x, float z, float yaw, float scale, bool collide = true)
        {
            var go = SpawnScaled(model, root, x, z, yaw, TargetHeight(model) * scale);
            if (go == null) return null;
            if (collide) AddCollider(go);
            return go.transform;
        }

        /// 렌더러 경계로 박스 콜라이더 한 개(나무는 줄기만 좁게).
        public static void AddCollider(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var bc = go.AddComponent<BoxCollider>();
            var lc = go.transform.InverseTransformPoint(b.center);
            var ls = go.transform.InverseTransformVector(b.size); ls = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            if (go.name.Contains("Tree") || go.name.Contains("Palm") || go.name.Contains("Pole"))
            { ls.x = Mathf.Min(ls.x, 0.9f); ls.z = Mathf.Min(ls.z, 0.9f); }
            bc.center = lc; bc.size = ls;
        }

        static void Reg(Transform house, string name) { if (house == null) return; float fz = 2.2f; foreach (Transform ch in house) if (ch.name == "Door") { fz = ch.localPosition.z; break; } Houses.Add((house, name, house.TransformPoint(new Vector3(0f, 0f, fz + 1.1f)))); }

        static Transform PlaceTower(Transform root, float x, float z)
        {
            var prefab = ArtAssets.LoadPrefabOrNull("TransmissionTower");
            GameObject go;
            if (prefab != null) { go = Object.Instantiate(prefab, root); go.name = "Tower"; }
            else
            {
                go = new GameObject("Tower"); go.transform.SetParent(root, false);
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(pole.GetComponent<Collider>()); pole.transform.SetParent(go.transform, false);
                pole.transform.localPosition = new Vector3(0f, 10f, 0f); pole.transform.localScale = new Vector3(0.6f, 10f, 0.6f);
                pole.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.45f, 0.48f, 0.5f));
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(arm.GetComponent<Collider>()); arm.transform.SetParent(go.transform, false);
                arm.transform.localPosition = new Vector3(0f, 17f, 0f); arm.transform.localScale = new Vector3(8f, 0.35f, 0.35f);
                arm.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.35f, 0.36f, 0.38f));
            }
            go.transform.position = Ground(x, z); go.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var steel = CoastMaterials.CreateLit(new Color(0.42f, 0.44f, 0.48f));
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int i = 0; i < arr.Length; i++) arr[i] = steel; r.sharedMaterials = arr; }
            Normalize(go, 16f, Ground(x, z).y - 0.1f);
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 6f, 0f); bc.size = new Vector3(3.2f, 12f, 3.2f);
            return go.transform;
        }

        static Transform PlaceShop(Transform root, float x, float z, float yaw)
        {
            var go = JejuKit.SpawnFHouse(3, root, Ground(x, z), yaw) ?? JejuKit.SpawnBuilding(2, root, Ground(x, z), yaw) ?? JejuKit.Spawn("Shop_A", root, Ground(x, z), yaw, 1f);
            if (go == null) return null;
            go.name = "Shop"; Normalize(go, 4.6f, Ground(x, z).y - 0.04f); AddCollider(go);
            // 간판(작은 알림판)
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(sign.GetComponent<Collider>()); sign.name = "ShopSign";
            sign.transform.SetParent(root, false); sign.transform.position = Ground(x, z) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 2.9f, -2.4f); sign.transform.rotation = Quaternion.Euler(0f, yaw, 0f); sign.transform.localScale = new Vector3(2.4f, 0.6f, 0.12f);
            sign.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.95f, 0.55f, 0.35f));
            return go.transform;
        }

        /// 140차: 등대 절벽(바위 덩어리) — 등대는 그 위에
        public static float CliffTop = 6.5f;
        static void BuildCliff(Transform root, float x, float z)
        {
            var go = new GameObject("Cliff"); go.transform.SetParent(root, false); go.transform.position = new Vector3(x, SeaLevel, z);
            var rockM = CoastMaterials.CreateLit(VillagePalette.Rock); var rockD = CoastMaterials.CreateLit(VillagePalette.RockShade); var grass = CoastMaterials.CreateLit(VillagePalette.Grass);
            void R(Vector3 p, Vector3 s, Material m) { var r = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(r.GetComponent<Collider>()); r.transform.SetParent(go.transform, false); r.transform.localPosition = p; r.transform.localScale = s; r.GetComponent<MeshRenderer>().sharedMaterial = m; }
            R(new Vector3(0f, 3.2f, 0f), new Vector3(16f, 8f, 12f), rockM);
            R(new Vector3(-4f, 2f, 3f), new Vector3(9f, 6f, 8f), rockD);
            R(new Vector3(5f, 1.5f, -3f), new Vector3(8f, 5f, 7f), rockD);
            R(new Vector3(0f, CliffTop, 0f), new Vector3(12f, 1.2f, 9f), grass);
        }

        static Transform BuildLighthouse(Transform root, float x, float z)
        {
            var go = new GameObject("Lighthouse"); go.transform.SetParent(root, false); go.transform.position = new Vector3(x, SeaLevel + CliffTop + 0.3f, z);
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(rock.GetComponent<Collider>()); rock.transform.SetParent(go.transform, false);
            rock.transform.localPosition = new Vector3(0f, -0.3f, 0f); rock.transform.localScale = new Vector3(4f, 1.2f, 4f);
            rock.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.40f, 0.40f, 0.42f));
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 4.2f, 0f); body.transform.localScale = new Vector3(1.8f, 4.2f, 1.8f);
            body.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.97f, 0.96f, 0.92f));
            for (int i = 0; i < 2; i++)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(band.GetComponent<Collider>()); band.transform.SetParent(go.transform, false);
                band.transform.localPosition = new Vector3(0f, 2.4f + i * 3.0f, 0f); band.transform.localScale = new Vector3(1.86f, 0.4f, 1.86f);
                band.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.92f, 0.28f, 0.28f));
            }
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(top.GetComponent<Collider>()); top.transform.SetParent(go.transform, false);
            top.transform.localPosition = new Vector3(0f, 8.6f, 0f); top.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);
            top.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.25f, 0.28f, 0.34f));
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(lamp.GetComponent<Collider>()); lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 9.4f, 0f); lamp.transform.localScale = Vector3.one * 1.3f;
            lamp.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(1f, 0.92f, 0.55f));
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(roof.GetComponent<Collider>()); roof.transform.SetParent(go.transform, false);
            roof.transform.localPosition = new Vector3(0f, 10.3f, 0f); roof.transform.localScale = new Vector3(1.6f, 0.3f, 1.6f);
            roof.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.85f, 0.25f, 0.25f));
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 3f, 0f); bc.size = new Vector3(5f, 8f, 5f);
            return go.transform;
        }

        static void BuildSignpost(Transform root, float x, float z)
        {
            var go = new GameObject("Signpost"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(post.GetComponent<Collider>()); post.transform.SetParent(go.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.1f, 0f); post.transform.localScale = new Vector3(0.16f, 1.1f, 0.16f);
            post.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.48f, 0.33f, 0.20f));
            Color[] cols = { new Color(0.55f, 0.78f, 0.42f), new Color(0.95f, 0.60f, 0.40f) };
            for (int i = 0; i < 2; i++)
            {
                var board = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(board.GetComponent<Collider>()); board.transform.SetParent(go.transform, false);
                board.transform.localPosition = new Vector3(i == 0 ? 0.45f : -0.45f, 1.9f - i * 0.5f, 0f); board.transform.localScale = new Vector3(1.1f, 0.34f, 0.08f);
                board.transform.localRotation = Quaternion.Euler(0f, i == 0 ? -12f : 14f, 0f);
                board.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(cols[i]);
            }
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 1f, 0f); bc.size = new Vector3(0.5f, 2f, 0.5f);
        }

        /// 140차(시안): 통나무 울타리로 둘러싼 3×3 밭 — 위쪽(북, 주인공 서는 곳)은 열어 둔다
        public const float GardenX = 0f, GardenZ = -6f;
        static void BuildGarden(Transform root)
        {
            var host = new GameObject("Garden").transform; host.SetParent(root, false);
            var log = CoastMaterials.CreateLit(VillagePalette.Log); var logD = CoastMaterials.CreateLit(VillagePalette.LogDark);
            var soil = CoastMaterials.CreateLit(VillagePalette.Soil); var soilL = CoastMaterials.CreateLit(VillagePalette.SoilLight);
            void Cyl(Vector3 p, Vector3 s, Quaternion q, Material m) { var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(host, false); g.transform.position = p; g.transform.localScale = s; g.transform.rotation = q; g.GetComponent<MeshRenderer>().sharedMaterial = m; }
            float x0 = GardenX - 4.2f, x1 = GardenX + 4.2f, z0 = GardenZ - 3.7f, z1 = GardenZ + 3.7f;
            // 말뚝 + 가로 통나무 2단(남·동·서), 북쪽은 양끝만
            for (float x = x0; x <= x1 + 0.01f; x += 2.1f) { Cyl(Ground(x, z0) + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.5f, 0.22f), Quaternion.identity, logD); }
            for (float z = z0; z <= z1 + 0.01f; z += 1.85f) { Cyl(Ground(x0, z) + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.5f, 0.22f), Quaternion.identity, logD); Cyl(Ground(x1, z) + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.5f, 0.22f), Quaternion.identity, logD); }
            for (int k = 0; k < 2; k++)
            {
                float y = 0.30f + k * 0.35f;
                Cyl(Ground(GardenX, z0) + new Vector3(0f, y, 0f), new Vector3(0.14f, 4.3f, 0.14f), Quaternion.Euler(0f, 0f, 90f), log);
                Cyl(Ground(x0, GardenZ) + new Vector3(0f, y, 0f), new Vector3(0.14f, 3.8f, 0.14f), Quaternion.Euler(90f, 0f, 0f), log);
                Cyl(Ground(x1, GardenZ) + new Vector3(0f, y, 0f), new Vector3(0.14f, 3.8f, 0.14f), Quaternion.Euler(90f, 0f, 0f), log);
                Cyl(Ground(x0 + 1.2f, z1) + new Vector3(0f, y, 0f), new Vector3(0.14f, 1.2f, 0.14f), Quaternion.Euler(0f, 0f, 90f), log);
                Cyl(Ground(x1 - 1.2f, z1) + new Vector3(0f, y, 0f), new Vector3(0.14f, 1.2f, 0.14f), Quaternion.Euler(0f, 0f, 90f), log);
            }
            Cyl(Ground(x0, z1) + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.5f, 0.22f), Quaternion.identity, logD);
            Cyl(Ground(x1, z1) + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.5f, 0.22f), Quaternion.identity, logD);
            // 밭 3×3(흙 상자 + 두둑 테두리)
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
            {
                float cx = GardenX - 2.6f + i * 2.6f, cz = GardenZ - 2.3f + j * 2.3f;
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(b.GetComponent<Collider>()); b.transform.SetParent(host, false);
                b.transform.position = Ground(cx, cz) + new Vector3(0f, 0.08f, 0f); b.transform.localScale = new Vector3(2.3f, 0.16f, 2.0f); b.GetComponent<MeshRenderer>().sharedMaterial = soil;
                var rim = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(rim.GetComponent<Collider>()); rim.transform.SetParent(host, false);
                rim.transform.position = Ground(cx, cz) + new Vector3(0f, 0.05f, 0f); rim.transform.localScale = new Vector3(2.55f, 0.10f, 2.25f); rim.GetComponent<MeshRenderer>().sharedMaterial = soilL;
            }
            // 150차(스타듀식): 밭 안으로 들어가 칸 위에 선다 — 울타리 자리에만 얇은 콜라이더(북쪽 가운데는 문)
            void Wall(float cx, float cz, float sx, float sz) { var wgo = new GameObject("GardenFence", typeof(BoxCollider)); wgo.transform.SetParent(host, false); var b2 = wgo.GetComponent<BoxCollider>(); wgo.transform.position = Ground(cx, cz) + new Vector3(0f, 0.5f, 0f); b2.size = new Vector3(sx, 1.2f, sz); }
            Wall(GardenX, z0, 8.8f, 0.3f); Wall(x0, GardenZ, 0.3f, 7.8f); Wall(x1, GardenZ, 0.3f, 7.8f);
            Wall(x0 + 0.9f, z1, 1.8f, 0.3f); Wall(x1 - 0.9f, z1, 1.8f, 0.3f);
            BuildingOutline.Attach(host, 0.02f);
        }

        static void Pampas(Transform root, float x, float z, System.Random rng)
        {
            var go = new GameObject("Pampas"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            var stem = CoastMaterials.CreateLit(new Color(0.72f, 0.78f, 0.45f)); var plume = CoastMaterials.CreateLit(new Color(0.97f, 0.95f, 0.88f));
            int n = 4 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                float a = (float)rng.NextDouble() * 6.28f, r = 0.15f + (float)rng.NextDouble() * 0.35f, h = 1.2f + (float)rng.NextDouble() * 0.9f;
                float tilt = -12f + (float)rng.NextDouble() * 24f;
                var st = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(st.GetComponent<Collider>()); st.transform.SetParent(go.transform, false);
                st.transform.localPosition = new Vector3(Mathf.Cos(a) * r, h * 0.5f, Mathf.Sin(a) * r); st.transform.localScale = new Vector3(0.03f, h * 0.5f, 0.03f); st.transform.localRotation = Quaternion.Euler(tilt, 0f, tilt * 0.5f);
                st.GetComponent<MeshRenderer>().sharedMaterial = stem;
                var pl = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(pl.GetComponent<Collider>()); pl.transform.SetParent(go.transform, false);
                pl.transform.localPosition = st.transform.localPosition + st.transform.localRotation * new Vector3(0f, h * 0.5f + 0.15f, 0f); pl.transform.localScale = new Vector3(0.14f, 0.45f, 0.14f); pl.transform.localRotation = st.transform.localRotation;
                pl.GetComponent<MeshRenderer>().sharedMaterial = plume;
            }
        }

        static void FlowerClump(Transform root, float x, float z, System.Random rng)
        {
            var go = new GameObject("FlowerClump"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            Color[] cols = { new Color(1f, 0.55f, 0.72f), new Color(1f, 0.70f, 0.80f), Color.white };
            for (int i = 0; i < 9; i++)
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(f.GetComponent<Collider>()); f.transform.SetParent(go.transform, false);
                f.transform.localPosition = new Vector3((float)(rng.NextDouble() - 0.5) * 1.6f, 0.25f + (float)rng.NextDouble() * 0.15f, (float)(rng.NextDouble() - 0.5) * 1.2f); f.transform.localScale = Vector3.one * 0.2f;
                f.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(cols[rng.Next(cols.Length)]);
                var st = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(st.GetComponent<Collider>()); st.transform.SetParent(go.transform, false);
                st.transform.localPosition = f.transform.localPosition - new Vector3(0f, 0.15f, 0f); st.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f);
                st.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.35f, 0.62f, 0.28f));
            }
        }

        static void Post(Transform root, float x, float z)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(p.GetComponent<Collider>()); p.name = "Post";
            p.transform.SetParent(root, false); p.transform.position = Ground(x, z) + new Vector3(0f, 0.35f, 0f); p.transform.localScale = new Vector3(0.14f, 0.7f, 0.14f);
            p.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.62f, 0.45f, 0.28f));
        }

        // 텃밭 작물(저장의 화분 상태를 3D로): 단계별 크기·색
        /// 150차: 텃밭은 VillageFarm(9칸 스타듀식) 이 그린다. 이름은 HomeUI 콜백 호환.
        public static void BuildCrops(Transform root, SaveData save)
        {
            VillageFarm.Build(root, save);
            var host = root.Find("Crops"); if (host != null) BuildingOutline.Attach(host, 0.02f);
        }
    }
}
