using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 136차(사용자: 「동물의 숲 포켓캠프처럼 바닷가 마을」): 스토리 허브 마을의 3D 세계를 절차적으로 세운다.
    /// 언덕 꼭대기 = 주인공 집 + 송전탑, 가운데 마을(엄마 집·구멍가게·텃밭), 남쪽 해변(낚시·등대).
    /// 기존 자산 재활용: JejuKit 모델(제주 초가/기와집·FShop 상가·야자수·귤나무·돌담·돌하르방·벤치…), TransmissionTower 프리팹, 바다 셰이더.
    public static class VillageWorld
    {
        public const float Half = 80f;          // 마을 반폭(m) — 스플랫 영역. 194차(사용자: 「지도 1.5배」): 50 → 80(동쪽 새 동네·북쪽 언덕까지)
        /// 194차: 걸을 수 있는 경계 — 동쪽은 차도 앞 연석(x 70.5), 북쪽 언덕 z 60, 서쪽 x −46.
        public const float EastLim = 70.5f, NorthLim = 60f, WestLim = -46f;
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
        // 178차: 큰길 굽이(−7, 25)에서 언덕 서쪽 방목장 문(−23, 30)까지 갈림길 — 우리집 현무암 담 남서 모서리 아래로 돈다
        public static readonly Vector2[] RanchPath = {
            new Vector2(-7f, 25f), new Vector2(-13f, 23.2f), new Vector2(-18.5f, 25.2f), new Vector2(-21.6f, 28.6f), new Vector2(-23.4f, 30f)
        };
        // 187차(사용자: 「정자, 알바나라, 송전탑에는 짧은 갈림길」): 막힘 지도(개발 메뉴 187 - Obstacle map)로 건물·담을 피해 그은 갈림길 3개
        public static readonly Vector2[] JobPath = { new Vector2(0.5f, 31f), new Vector2(4.5f, 30.8f), new Vector2(8.9f, 31.1f) };                                            // 우리집 앞 → 알바나라 문
        public static readonly Vector2[] TowerPath = { new Vector2(4.5f, 30.8f), new Vector2(4.8f, 33.5f), new Vector2(5.5f, 36.6f), new Vector2(8.2f, 37.6f), new Vector2(11f, 38f) };   // 우리집 담과 알바나라 사이 → 송전탑 언덕
        public static readonly Vector2[] PlayPath = { new Vector2(-1.5f, 0.5f), new Vector2(8f, -0.6f), new Vector2(14.3f, -1.6f), new Vector2(16.8f, -5.2f), new Vector2(23.8f, -6.2f) };  // 하르방 사이 큰길 → 동쪽 정자
        public static readonly Vector2[] HospitalPath = { new Vector2(-1.5f, 10.5f), new Vector2(10f, 10.5f), new Vector2(16f, 12.6f), new Vector2(20f, 13.5f) };   // 189차: 큰길 → 병원 문(서쪽)
        public static readonly Vector2[][] Branches = { JobPath, TowerPath, PlayPath, HospitalPath, VillageEast.EastPath, VillageEast.BankPath, VillageEast.CafePath };   // 194차: 동쪽 새 동네(은행·브런치 카페·버스 정류장)
        // 190차(사용자: 「처음엔 자동이동 우리집·알바나라만. 가는 길 중간을 끊어서, 끊어진 길을 이어야 자동이동」): 돌길이 끊긴 곳(주황 고깔 2개).
        // 여기 반경 GapR 안은 돌길 메시를 그리지 않고 도로망(VillageRoad.Base)에서도 빠진다 → 도로 깔기(한 칸 500G)로 이어야 한다
        public const float GapR = 3.0f;
        public static readonly Vector2[] RoadGaps = {
            new Vector2(-3.5f, 28f),     // 큰길: 우리집 ↔ 마을(농장·방목장·아래 전부)
            new Vector2(-1f, 7f),        // 큰길: 병원 갈림길 ↔ 텃밭·정자·엄마 집
            new Vector2(-2f, -21f),      // 큰길: 엄마 집 ↔ 바닷가·상점·반도 집들
            new Vector2(-9.6f, -57.5f),  // 큰길: 반도 ↔ 등대
            new Vector2(5.2f, 35.2f),    // 송전탑 갈림길
            new Vector2(-15.7f, 24.2f),  // 방목장 갈림길
            new Vector2(11f, -1.1f),     // 정자 갈림길
            new Vector2(8f, 10.5f),      // 병원 갈림길
        };
        public static bool InGap(float x, float z, float pad = 0f) { foreach (var g in RoadGaps) if ((g.x - x) * (g.x - x) + (g.y - z) * (g.y - z) < (GapR + pad) * (GapR + pad)) return true; return false; }

        // ── 높이 ─────────────────────────────────────────────────────────
        public static VillageInterior Interior;
        // 154차(사용자: 「건물 아래로 내려가거나 몸이 들어간다」): 집 발치를 평평한 받침(pad)으로 — 집은 중심 높이에 세워지는데
        // 경사에서 문 앞 지면이 더 낮아 주인공이 집 바닥 아래로 내려가던 문제. 받침 밖 1.6 m 에서 부드럽게 원래 지형으로.
        struct Pad { public float x, z, hw, hd, yaw; public Pad(float x, float z, float hw, float hd, float yaw) { this.x = x; this.z = z; this.hw = hw; this.hd = hd; this.yaw = yaw; } }
        static readonly Pad[] Pads = {
            new Pad(0f, 36f, 3.8f, 3.4f, 180f), new Pad(4.8f, -19.9f, 3.7f, 3.1f, 25f), new Pad(-5.8f, -33.5f, 3.0f, 2.7f, 20f),
            new Pad(-6.2f, -44f, 2.2f, 1.9f, -80f), new Pad(-23f, -58f, 2.2f, 1.9f, 90f), new Pad(-23f, -35f, 2.1f, 1.8f, 90f),   // 190~191차: 반도 집 간격 벌림(191: 서쪽 줄을 x −23 으로 더 멀리)
            new Pad(-23.5f, -46.5f, 2.2f, 1.9f, 90f), new Pad(-6.3f, -54.5f, 2.1f, 1.8f, -80f),
            new Pad(9.8f, 33.5f, 2.4f, 2.1f, 200f),   // 156차: 알바나라(우리집 옆)
            new Pad(-10f, 34.5f, 3.6f, 2.8f, 0f),       // 171차: 농장(우리집 서쪽)
            new Pad(VillageEast.BankX, VillageEast.BankZ, 3.4f, 3.0f, 180f), new Pad(VillageEast.CafeX, VillageEast.CafeZ, 3.4f, 3.0f, 0f) };   // 194차: 은행·브런치 카페
        static float[] _padY;
        public static float Height(float x, float z)
        {
            if (Interior != null && Interior.Contains(x, z)) return Interior.FloorY;
            if (VillageZones.TryFloor(x, z, out float zy)) return zy;   // 195차: 시내·관광지·광산
            float h = HeightRaw(x, z);
            if (_padY == null) { _padY = new float[Pads.Length]; for (int i = 0; i < Pads.Length; i++) _padY[i] = HeightRaw(Pads[i].x, Pads[i].z); }
            for (int i = 0; i < Pads.Length; i++)
            {
                var p = Pads[i]; float dx = x - p.x, dz = z - p.z;
                if (Mathf.Abs(dx) > p.hw + 2f || Mathf.Abs(dz) > p.hd + 2f) continue;
                float c = Mathf.Cos(-p.yaw * Mathf.Deg2Rad), s = Mathf.Sin(-p.yaw * Mathf.Deg2Rad);
                float lx = dx * c - dz * s, lz = dx * s + dz * c;
                float ox = Mathf.Max(0f, Mathf.Abs(lx) - p.hw), oz = Mathf.Max(0f, Mathf.Abs(lz) - p.hd);
                float d = Mathf.Sqrt(ox * ox + oz * oz);
                if (d < 1.6f) { float k = 1f - Mathf.SmoothStep(0f, 1f, d / 1.6f); h = Mathf.Lerp(h, _padY[i], k); }
            }
            return h;
        }
        static float HeightRaw(float x, float z)
        {
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
            // 194차: 지도 1.5배 — 동쪽은 차도 너머(x 80), 북쪽은 z 62 부터 오른다
            float hRoad = h;
            float outside = Mathf.Max(Mathf.Max(x - 80f, -x - 46f), z - 62f);
            if (outside > 0f && z > -20f)
            {
                float k = Mathf.SmoothStep(0f, 1f, outside / 40f);
                h += k * (7f + 9f * Mathf.PerlinNoise(x * 0.03f + 1.7f, z * 0.03f + 9.2f));
            }
            // 194차: 차도는 매끈하게(언덕을 깎아 지나간다)
            float rdist = VillageEast.RoadDist(x, z);
            if (rdist < 7f) h = Mathf.Lerp(h, hRoad, 1f - Mathf.SmoothStep(0f, 1f, (rdist - 4.5f) / 2.5f));
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
            var p = new Vector2(x, z);
            float d = Mathf.Min(PolyDist(Path, p), PolyDist(RanchPath, p));
            foreach (var b in Branches) d = Mathf.Min(d, PolyDist(b, p));   // 187차: 갈림길 3개
            return d;   // 178차: 방목장 갈림길도 길로 친다(흙·평탄화·소품 비키기)
        }
        static float PolyDist(Vector2[] line, Vector2 p)
        {
            float best = 999f;
            for (int i = 0; i < line.Length - 1; i++)
            {
                var a = line[i]; var b = line[i + 1]; var ab = b - a;
                float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * k));
            }
            return best;
        }

        public static Vector3 Ground(float x, float z) => new Vector3(x, Height(x, z), z);

        // ── 세우기 ───────────────────────────────────────────────────────
        public static Transform Build(Transform parent, SaveData save)
        {
            VillageRanch.Alive = VillageRanch.AliveFor(save);   // 183차: 이번 주 잡힌 말 수만큼 빼고
            var root = new GameObject("VillageWorld").transform;
            root.SetParent(parent, false);
            BuildLight(root);
            BuildTerrain(root, save);
            BuildSea(root);
            BuildVista(root, save);
            BuildProps(root);
            VillageZones.Build(root);        // 195차: 버스로 가는 시내·중문관광단지 + 광산
            VillageSeason.Build(root, save); // 195차: 계절 이벤트 장식·부스
            BuildRocks(root, save); ApplyFelledTrees(root, save);   // 154차: 캘 수 있는 바위·쓰러진 나무(8주 뒤 재생)
            VillageLand.Build(root, save);   // 183차: 임대 땅(매물 푯말 / 산 땅은 임대 주택)
            // 147차: 조경 밀도 — 잔디에 꽃·클로버·풀포기 자동 배치(종류별 메시 합침)
            FloraCount = VillageFlora.Scatter(root);
            // 146차: 곡면 가중치 — 하늘·구름·새·먼 산은 고정(0), 거품·반짝임 등 투명 장식은 지면과 같이 휘게(1)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name; var t = r.transform;
                bool flat = n == "SkyDome" || n == "Vista" || n == "VistaSide" || n == "FarHill" || n == "Cloud" || n == "SunDisc" || n == "Bird" || (t.parent != null && (t.parent.name == "Cloud" || t.parent.name == "Bird")) || t.GetComponentInParent<BirdFly>() != null;   // 193차: 새 윤곽 메시(Body/Wing)
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
            // 151차: 손그림풍 디테일(R 잔디 결 · G 모래 결, 밑색 초록 정도로 갈라 씀) — 없으면 옛 노이즈
            var dmTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Detail");
            var dm = dmTex != null ? dmTex : DetailTex();
            var tm = mr.sharedMaterial;
            // 174차: 디테일 맵 R 채널을 Firefly 잔디로 바꾸고, 타일 3.3 m·세기 0.48 로 올려 잔디 포기가 보이게
            if (tm != null && tm.HasProperty("_DetailMap"))
            {
                tm.SetTexture("_DetailMap", dm);
                tm.SetFloat("_DetailScale", dmTex != null ? 0.30f : 0.5f);
                tm.SetFloat("_DetailStrength", dmTex != null ? 0.48f : 0.16f);
                if (tm.HasProperty("_DetailSplit")) tm.SetFloat("_DetailSplit", dmTex != null ? 1f : 0f);
            }
            mr.receiveShadows = true; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BuildPathMesh(root);   // 157차: 돌길
            BuildPathMesh(root, RanchPath, "PathMesh_Ranch", 0.085f);
            for (int bi = 0; bi < Branches.Length; bi++) BuildPathMesh(root, Branches[bi], "PathMesh_Branch" + bi, 0.08f - bi * 0.004f);
            BuildGapCones(root);   // 190차   // 187차   // 178차: 방목장 갈림길(큰길보다 살짝 낮게 — 만나는 곳 깜빡임 방지)

            // 바깥 울타리(보이지 않는 벽) — 마을 밖으로 못 나가게
            // 194차(지도 1.5배): 북쪽 z 60, 동쪽 차도 연석 x 70.5, 서쪽 x −46
            Wall(root, new Vector3((WestLim + EastLim) * 0.5f, 5f, NorthLim), new Vector3(EastLim - WestLim + 2f, 12f, 1f));
            // 192차(사용자: 「바닷가는 들어갈 수 있게, 10초 넘게 있으면 병원행」): 바다 앞 보이지 않는 벽을 물가에서 바다 쪽으로 약 10 m 밀어낸다
            // (예전: 모래사장 끝 z −26·−34, 반도 둘레 바로 바깥). 물속 10초 → 병원은 VillageHubSea.cs
            Wall(root, new Vector3(38.5f, 5f, -38f), new Vector3(65f, 12f, 1f));    // 동쪽 바다 남단 (x 6~71) — 194차: 새 동네까지
            Wall(root, new Vector3(6f, 5f, -56.5f), new Vector3(1f, 12f, 37f));     // 반도 동쪽 바다 (z −38~−75)
            Wall(root, new Vector3(-15.5f, 5f, -75f), new Vector3(43f, 12f, 1f));   // 반도 남쪽 바다
            Wall(root, new Vector3(-37f, 5f, -54.5f), new Vector3(1f, 12f, 41f));   // 반도 서쪽 바다
            Wall(root, new Vector3(-44f, 5f, -34f), new Vector3(14f, 12f, 1f));     // 서쪽 바다 남단 (x −51~−37)
            Wall(root, new Vector3(EastLim, 5f, 11f), new Vector3(1f, 12f, 98f));
            Wall(root, new Vector3(WestLim, 5f, 13f), new Vector3(1f, 12f, 94f));
        }

        /// 171차(사용자: 「산 쪽에 분명히 갈 수 있는 곳인데 못 움직이는 벽」): 보이지 않는 경계(z=46, x=±46)가 땅 위를 지나므로
        /// 그 선을 따라 소나무·바위 띠를 세워 「여기까지」가 보이게 한다(콜라이더는 기존 Bound 그대로).
        static void BuildBoundaryDeco(Transform root)
        {
            var rng = new System.Random(171); int n = 0;
            _boulderMats = null;   // 173차: 씬마다 새로 만든다(옛 머티리얼 참조가 남지 않게)
            void Post(float x, float z)
            {
                if (Height(x, z) < SeaLevel + 0.3f) return;
                // 173차(사용자 시안): 경계는 둥근 파스텔 바위가 이어진 담 — 바위는 칸마다, 소나무는 네 칸에 한 번 뒤로
                BoulderCluster(root, x + (float)(rng.NextDouble() - 0.5) * 0.8f, z + (float)(rng.NextDouble() - 0.5) * 0.8f, rng);
                if ((n++ % 4) != 3) return;
                var t = VillageHouses.Pine(root, Ground(x + (float)(rng.NextDouble() - 0.5) * 1.4f, z + (float)(rng.NextDouble() - 0.5) * 1.4f), (float)rng.NextDouble() * 360f, 3.8f + (float)rng.NextDouble() * 2.2f);
                if (t != null) t.name = "Tree_PineEdge";
            }
            // 194차(지도 1.5배): 북쪽 담 z 59.3 · 서쪽 x −45.3. 동쪽은 차도 연석 가드레일(VillageEast)
            for (float x = -44f; x <= 66f; x += 3.4f) Post(x, NorthLim - 0.7f);
            // 192차(사용자: 「바닷가 끝에서 올라갈 것처럼 보이는데 못 간다 — 그런 곳엔 바위 등 장애물」): 동·서 경계 바위 담을 모래사장(z −18 → −30)까지 잇는다
            for (float z = -30f; z <= 58f; z += 3.4f) Post(WestLim + 0.7f, z);
        }

        // ── 173차: 경계 바위 담(시안: 둥글둥글한 파스텔 바위가 쌓이고 위에 이끼·꽃) ──
        // 173차: 색은 Firefly 그림에서 그대로 뽑아 쓴다 — 바위마다 그림의 다른 자리를 보게 오프셋만 돌린다
        static readonly Vector2[] BoulderUv = {
            new Vector2(0.00f, 0.00f), new Vector2(0.33f, 0.11f), new Vector2(0.66f, 0.22f), new Vector2(0.12f, 0.45f),
            new Vector2(0.45f, 0.56f), new Vector2(0.78f, 0.67f), new Vector2(0.24f, 0.80f), new Vector2(0.57f, 0.91f),
            new Vector2(0.90f, 0.05f), new Vector2(0.06f, 0.28f), new Vector2(0.39f, 0.72f), new Vector2(0.71f, 0.38f)
        };
        // 174차(시안): 크림·연노랑·민트·라벤더·청회색·따뜻한 회색 — 한 덩이에 한 색
        static readonly Color[] BoulderTint = {
            new Color(0.97f, 0.93f, 0.78f), new Color(0.86f, 0.91f, 0.72f), new Color(0.70f, 0.86f, 0.76f),
            new Color(0.72f, 0.83f, 0.90f), new Color(0.76f, 0.74f, 0.88f), new Color(0.82f, 0.79f, 0.74f)
        };
        static Material[] _boulderMats;
        static Material BoulderMat(int i)
        {
            if (_boulderMats == null)
            {
                var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_RockSurf");   // 174차: 자갈 그림 대신 매끈한 돌 표면(Firefly)
                _boulderMats = new Material[BoulderUv.Length];
                for (int k = 0; k < BoulderUv.Length; k++)
                {
                    var tint = BoulderTint[k % BoulderTint.Length];
                    var m = tex != null ? ArtAssets.CreateTexturedLit(tex, tint, 0.02f) : CoastMaterials.CreateLit(tint, 0.02f);
                    m.mainTextureScale = new Vector2(1.0f, 1.0f);
                    m.mainTextureOffset = BoulderUv[k];
                    _boulderMats[k] = m;
                }
            }
            int n = _boulderMats.Length;
            return _boulderMats[((i % n) + n) % n];
        }

        /// 174차: 마을 바위는 전부 이 한 덩이로 만든다(옛 VStone 키트·회색 구 폐기) — 콜라이더를 남겨 통과하지 않게.
        public static Transform PastelBoulder(Transform parent, Vector3 pos, float s, System.Random rng, bool moss, bool keepCollider = true, int matIdx = -1)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (!keepCollider) Object.Destroy(b.GetComponent<Collider>());
            b.name = "Boulder"; b.transform.SetParent(parent, false); b.transform.position = pos;
            b.transform.localScale = new Vector3(s * 1.30f, s * 1.0f, s * 1.15f);
            b.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 16f, (float)rng.NextDouble() * 360f, (float)(rng.NextDouble() - 0.5) * 16f);
            b.GetComponent<Renderer>().sharedMaterial = BoulderMat(matIdx >= 0 ? matIdx : rng.Next(64));
            if (moss) BoulderMoss(b.transform, rng);
            return b.transform;
        }

        /// 바위 한 무더기(아래 두세 덩이 + 위에 한두 덩이) — 경계 담 한 칸
        internal static void BoulderCluster(Transform root, float x, float z, System.Random rng)
        {
            var host = new GameObject("RockBand").transform; host.SetParent(root, false); host.position = Ground(x, z);
            int count = 3 + rng.Next(2);   // 173차: 담이 이어져 보이되 오브젝트 수는 억제
            for (int i = 0; i < count; i++)
            {
                float s = 0.74f + (float)rng.NextDouble() * 0.62f;   // 173차: 시안처럼 담이 어깨높이로 쌓이게
                bool upper = i >= count - 2 && rng.NextDouble() < 0.7;
                var off = new Vector3((float)(rng.NextDouble() - 0.5) * 2.9f, (upper ? 1.05f : 0.30f) * s + 0.04f, (float)(rng.NextDouble() - 0.5) * 0.7f);
                PastelBoulder(host, host.position + off, s, rng, rng.NextDouble() < 0.40);
            }
        }

        /// 바위 위 이끼 방석 + 작은 꽃 두세 송이
        static void BoulderMoss(Transform rock, System.Random rng)
        {
            var moss = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(moss.GetComponent<Collider>());
            moss.name = "RockMoss"; moss.transform.SetParent(rock, false);
            float mx = (float)(rng.NextDouble() - 0.5) * 0.3f, mz = (float)(rng.NextDouble() - 0.5) * 0.3f;
            moss.transform.localPosition = new Vector3(mx, 0.40f, mz);
            moss.transform.localScale = new Vector3(0.54f, 0.20f, 0.50f);
            moss.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.78f, 0.45f), 0.02f);
            Color[] petals = { new Color(1f, 0.72f, 0.82f), new Color(1f, 0.95f, 0.62f), Color.white };
            int fl = 3;
            for (int i = 0; i < fl; i++)
            {
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(f.GetComponent<Collider>());
                f.name = "RockFlower"; f.transform.SetParent(rock, false);
                f.transform.localPosition = new Vector3(mx + (float)(rng.NextDouble() - 0.5) * 0.24f, 0.47f, mz + (float)(rng.NextDouble() - 0.5) * 0.24f);
                f.transform.localScale = new Vector3(0.17f, 0.07f, 0.17f);
                f.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(petals[rng.Next(petals.Length)], 0.02f);
            }
        }

        /// 153차: 언덕(z 14~44, 마당·집·송전탑 비켜서) 채우기
        static void BuildHillDeco(Transform root)
        {
            var rng = new System.Random(153);
            float[,] pines = { { -16f, 33f }, { -20f, 27f }, { -13f, 40f }, { 19f, 31f }, { 23f, 24f }, { 15f, 19f }, { -23f, 20f }, { 26f, 34f }, { -18f, 15f }, { 20f, 40f } };
            for (int i = 0; i < pines.GetLength(0); i++)
            {
                float x = pines[i, 0], z = pines[i, 1]; if (PathDist(x, z) < 3f) continue;
                var t = VillageHouses.Pine(root, Ground(x, z), i * 71f, 4.2f + (float)rng.NextDouble() * 1.6f); t.name = "Tree_Pine_" + i;
                GroundBlob.Static(root, t.position, 2.0f, 1.6f, 0.22f);
            }
            var o1 = Place(root, "Prop_OrangeTree", -12f, 24f, 30f, 0.9f); if (o1 != null) { o1.name = "Tree_Orange_H1"; Trees.Add(o1); GroundBlob.Static(root, o1.position, 2.0f, 1.6f, 0.22f); }
            var o2 = Place(root, "Prop_OrangeTree", 13f, 27f, 200f, 0.9f); if (o2 != null) { o2.name = "Tree_Orange_H2"; Trees.Add(o2); GroundBlob.Static(root, o2.position, 2.0f, 1.6f, 0.22f); }
            VillageHouses.Windmill(root, Ground(-17f, 38.5f), 25f, 4.6f);
            Place(root, "Prop_Bench", -11f, 28f, 120f, 1f); Place(root, "Prop_Bench", 12f, 22f, -60f, 1f);
            Place(root, "Prop_Hareubang", -4.5f, 18.5f, 180f, 0.9f); Place(root, "Prop_Hareubang", 4.5f, 18.5f, 180f, 0.9f);
            float[,] rocks = { { -12.5f, 21f }, { 14f, 25.5f }, { -19f, 36f }, { 17f, 36f }, { -9f, 41f }, { 9.5f, 41.5f }, { 22f, 28f } };
            for (int i = 0; i < rocks.GetLength(0); i++) Rock(root, rocks[i, 0], rocks[i, 1], rng);
            float[,] beds = { { -14f, 30f }, { 16f, 33f }, { -7f, 16f }, { 8f, 15.5f } };
            for (int i = 0; i < beds.GetLength(0); i++) FlowerBed(root, beds[i, 0], beds[i, 1], rng);
            float[,] bushes = { { -10f, 36f }, { 13f, 38f }, { -15f, 25f }, { 17f, 28.5f }, { -20f, 31f }, { 21f, 21f }, { -6f, 20.5f }, { 6.5f, 21f } };
            for (int i = 0; i < bushes.GetLength(0); i++) Bush(root, bushes[i, 0], bushes[i, 1], rng);
            float[,] pamp = { { -13f, 17f }, { 13.5f, 17.5f }, { -21f, 24f }, { 24f, 30f }, { -15f, 43f }, { 17f, 43f } };
            for (int i = 0; i < pamp.GetLength(0); i++) Pampas(root, pamp[i, 0], pamp[i, 1], rng);
            for (int i = 0; i < 3; i++) FlowerClump(root, -8f + i * 8f, 13f + (i % 2) * 1.5f, rng);
            // 길가 통나무 울타리(언덕길 양쪽, 문 앞은 비움)
            VillageHouses.LogFence(root, Ground(-5.5f, 12f), Ground(-6.5f, 22f)); VillageHouses.LogFence(root, Ground(5.5f, 12f), Ground(6.5f, 22f));
            Lamp(root, -4f, 24f); Lamp(root, 4f, 24f);
        }

        // ── 154차: 캘 수 있는 바위(언덕) + 베어 쓰러지는 나무 — 8주 뒤 다시 생김 ──
        public const int RegrowWeeks = 8;
        public static readonly List<(Transform t, int idx)> Rocks = new List<(Transform, int)>();
        static readonly float[,] RockSpots = { { -11f, 19f }, { 14.5f, 22f }, { -17f, 30.5f }, { 19f, 35f }, { -21f, 23.5f }, { 23.5f, 27.5f }, { -8f, 42.5f }, { 8.5f, 43f }, { -14f, 36f }, { 16.5f, 38.5f }, { -24f, 34f }, { 25f, 22f } };
        public static int RockCount => RockSpots.GetLength(0);
        public static void EnsureGather(SaveData s)
        {
            if (s == null) return;
            if (s.villageRockHits == null || s.villageRockHits.Length < 32) { var a = new int[32]; if (s.villageRockHits != null) System.Array.Copy(s.villageRockHits, a, s.villageRockHits.Length); s.villageRockHits = a; }
            if (s.villageRockGone == null || s.villageRockGone.Length < 32) { var a = new int[32]; for (int i = 0; i < a.Length; i++) a[i] = -1; if (s.villageRockGone != null) System.Array.Copy(s.villageRockGone, a, s.villageRockGone.Length); s.villageRockGone = a; }
            if (s.villageTreeHits == null || s.villageTreeHits.Length < 64) { var a = new int[64]; if (s.villageTreeHits != null) System.Array.Copy(s.villageTreeHits, a, s.villageTreeHits.Length); s.villageTreeHits = a; }
            if (s.villageTreeGone == null || s.villageTreeGone.Length < 64) { var a = new int[64]; for (int i = 0; i < a.Length; i++) a[i] = -1; if (s.villageTreeGone != null) System.Array.Copy(s.villageTreeGone, a, s.villageTreeGone.Length); s.villageTreeGone = a; }
            // 8주 지난 것은 되살림
            for (int i = 0; i < s.villageRockGone.Length; i++) if (s.villageRockGone[i] >= 0 && s.week - s.villageRockGone[i] >= RegrowWeeks) { s.villageRockGone[i] = -1; s.villageRockHits[i] = 0; }
            for (int i = 0; i < s.villageTreeGone.Length; i++) if (s.villageTreeGone[i] >= 0 && s.week - s.villageTreeGone[i] >= RegrowWeeks) { s.villageTreeGone[i] = -1; s.villageTreeHits[i] = 0; }
        }
        public static bool RockGone(SaveData s, int i) => s != null && s.villageRockGone != null && i < s.villageRockGone.Length && s.villageRockGone[i] >= 0;
        public static bool TreeGone(SaveData s, int i) => s != null && s.villageTreeGone != null && i < s.villageTreeGone.Length && s.villageTreeGone[i] >= 0;

        /// 광석 바위: 큰 둥근 바위 + 어두운 반점 + 살짝 반짝이는 광맥. 이미 캔 것은 작은 잔해만.
        public static void BuildRocks(Transform root, SaveData save)
        {
            var old = root.Find("Rocks"); if (old != null) Object.Destroy(old.gameObject);
            var host = new GameObject("Rocks").transform; host.SetParent(root, false);
            Rocks.Clear(); EnsureGather(save);
            var rockM = CoastMaterials.CreateLit(new Color(0.60f, 0.60f, 0.58f)); var darkM = CoastMaterials.CreateLit(new Color(0.42f, 0.42f, 0.44f)); var oreM = CoastMaterials.CreateLit(new Color(0.85f, 0.75f, 0.45f), 0.3f);
            var rng = new System.Random(154);
            for (int i = 0; i < RockCount; i++)
            {
                float x = RockSpots[i, 0], z = RockSpots[i, 1]; var g = Ground(x, z);
                if (RockGone(save, i))
                {
                    var r = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(r.GetComponent<Collider>()); r.name = "RockRubble"; r.transform.SetParent(host, false);
                    r.transform.position = g + new Vector3(0f, 0.08f, 0f); r.transform.localScale = new Vector3(0.6f, 0.18f, 0.5f); r.GetComponent<Renderer>().sharedMaterial = darkM;
                    continue;
                }
                var t = new GameObject("Rock_" + i).transform; t.SetParent(host, false); t.position = g; t.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                float s = 1.3f + (float)rng.NextDouble() * 0.5f;
                int hits = save != null && save.villageRockHits != null ? save.villageRockHits[i] : 0;
                float shrink = 1f - hits * 0.18f;
                // 174차(사용자: 「옛 껍데기 걷어내고 다시 다 그려줘」): 각진 현무암 키트(VRock)를 버리고 캘 수 있는 바위도 시안 파스텔 덩이로.
                // 광석 결정만 남겨 「캘 수 있는 것」임을 알아보게 한다.
                var body = PastelBoulder(t, t.position + new Vector3(0f, s * 0.34f * shrink, 0f), s * 0.92f * shrink, rng, true, false).gameObject;
                body.name = "Body"; body.transform.SetParent(t, true);
                var b2go = PastelBoulder(t, t.position + new Vector3(s * 0.38f, s * 0.20f * shrink, s * 0.12f), s * 0.5f * shrink, rng, false, false).gameObject;
                b2go.name = "Body2"; b2go.transform.SetParent(t, true);
                for (int k = 0; k < 3; k++)
                {
                    var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(o.GetComponent<Collider>()); o.name = "Ore"; o.transform.SetParent(t, false);
                    o.transform.localPosition = new Vector3(Mathf.Cos(k * 2.1f) * s * 0.36f, s * (0.25f + k * 0.12f) * shrink, Mathf.Sin(k * 2.1f) * s * 0.3f); o.transform.localScale = Vector3.one * 0.16f * shrink; o.GetComponent<Renderer>().sharedMaterial = oreM;
                }
                var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, s * 0.35f, 0f); bc.size = new Vector3(s * 0.9f, s * 0.7f, s * 0.8f) * shrink;
                GroundBlob.Static(root, g, s * 1.1f, s * 0.9f, 0.2f);
                Rocks.Add((t, i));
            }
            BuildingOutline.Attach(host, 0.02f);
        }
        /// 베어 쓰러진 나무 → 그루터기로 교체(Trees 목록에서는 뺀다)
        public static void ApplyFelledTrees(Transform root, SaveData save)
        {
            EnsureGather(save); if (save == null) return;
            for (int i = 0; i < Trees.Count; i++)
            {
                var t = Trees[i]; if (t == null || !TreeGone(save, i)) continue;
                var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(stump.GetComponent<Collider>()); stump.name = "Stump_" + i; stump.transform.SetParent(root, false);
                stump.transform.position = t.position + new Vector3(0f, 0.22f, 0f); stump.transform.localScale = new Vector3(0.55f, 0.22f, 0.55f); stump.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.62f, 0.45f, 0.30f));
                var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(top.GetComponent<Collider>()); top.transform.SetParent(stump.transform, false); top.transform.localPosition = new Vector3(0f, 1.0f, 0f); top.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f); top.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.85f, 0.72f, 0.50f));
                Object.Destroy(t.gameObject); Trees[i] = null;
            }
        }

        /// 190차: 끊긴 길 양옆에 주황 고깔(콜라이더 없음 — 그 사이에 도로를 깔 수 있게)
        static void BuildGapCones(Transform root)
        {
            var orange = CoastMaterials.CreateLit(new Color(1f, 0.52f, 0.18f), 0.1f); var white = CoastMaterials.CreateLit(Color.white, 0.1f);
            var lines = new List<Vector2[]> { Path, RanchPath }; lines.AddRange(Branches);
            foreach (var g in RoadGaps)
            {
                // 가장 가까운 선분의 방향 → 수직으로 길 양옆
                Vector2 dir = Vector2.right; float bd = 1e9f;
                foreach (var L in lines) for (int i = 0; i < L.Length - 1; i++) { var a = L[i]; var ab = L[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(g - a, ab) / ab.sqrMagnitude); float d = (a + ab * t - g).sqrMagnitude; if (d < bd) { bd = d; dir = ab.normalized; } }
                var nrm = new Vector2(-dir.y, dir.x);
                for (int k = -1; k <= 1; k += 2)
                {
                    var q = g + nrm * (k * 2.2f); var basePos = Ground(q.x, q.y);
                    // 192차: 러닝 장애물 고깔 모델(Obs3_Cone)이 있으면 그것 — 콜라이더 없음
                    var kc = SpawnScaled("Obs3_Cone", root, q.x, q.y, k * 25f, 0.8f); if (kc != null) { kc.name = "GapCone"; continue; }
                    var cone = new GameObject("GapCone").transform; cone.SetParent(root, false); cone.position = basePos;
                    void C(float y, float r, float h, Material m) { var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(c.GetComponent<Collider>()); c.transform.SetParent(cone, false); c.transform.localPosition = new Vector3(0f, y, 0f); c.transform.localScale = new Vector3(r, h, r); c.GetComponent<MeshRenderer>().sharedMaterial = m; }
                    C(0.03f, 0.55f, 0.03f, orange); C(0.18f, 0.36f, 0.12f, orange); C(0.36f, 0.27f, 0.06f, white); C(0.48f, 0.2f, 0.06f, orange); C(0.62f, 0.12f, 0.08f, orange);
                }
            }
        }

        /// 192차: 정자 재질 — 키트 공용 재질(지붕 = 흰 파스텔, 돌 = 어두움) 대신 기와 청회색·밝은 화강석·단청 민트
        static void TintJeongja(Transform t)
        {
            var roof = CoastMaterials.CreateLit(new Color(0.38f, 0.43f, 0.50f), 0.1f); var stone = CoastMaterials.CreateLit(new Color(0.80f, 0.78f, 0.72f), 0.05f);
            var trim = CoastMaterials.CreateLit(new Color(0.40f, 0.72f, 0.62f), 0.1f); var wood = CoastMaterials.CreateLit(new Color(0.62f, 0.30f, 0.22f), 0.05f);
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) { string n = ms[i] != null ? ms[i].name : ""; if (n.StartsWith("Roof")) ms[i] = roof; else if (n.StartsWith("Stone")) ms[i] = stone; else if (n.StartsWith("Trim")) ms[i] = trim; else if (n == "Wood") ms[i] = wood; }
                r.sharedMaterials = ms;
            }
        }

        static void Wall(Transform root, Vector3 c, Vector3 size)
        {
            var w = new GameObject("Bound", typeof(BoxCollider)); w.transform.SetParent(root, false);
            w.transform.localPosition = c; w.GetComponent<BoxCollider>().size = size;
        }

        static Texture2D Splat()
        {
            const int S = 2560;   // 194차: 지도 1.5배(±80 m)라 2048→2560 · 144차: 1024(10 px/m)→2048(20 px/m) — 발밑 잔디가 뭉개지지 않게
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            Color grassA = VillagePalette.Grass, grassB = VillagePalette.GrassShade, grassC = VillagePalette.GrassLight;
            Color sand = VillagePalette.Sand, sandWet = VillagePalette.SandWet;
            Color path = VillagePalette.Path, pathEdge = VillagePalette.PathEdge;
            Color soil = new Color(0.45f, 0.31f, 0.20f), rock = new Color(0.62f, 0.60f, 0.56f);
            var rng = new System.Random(1234);
            // 174차(사용자: 「동물의 숲 대비 잔디가 미흡」): Firefly 잔디 타일을 스플랫에 구워 넣는다.
            // 읽기 불가 텍스처면 예전 퍼린 잔디 그대로.
            var gTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_GrassTile");
            Color32[] gPx = null; int gW = 0, gH = 0; const float GrassTileM = 5.5f;
            if (gTex != null) { try { gPx = gTex.GetPixels32(); gW = gTex.width; gH = gTex.height; } catch { gPx = null; Debug.LogWarning("[Splat] Tex_GrassTile 읽기 불가(Read/Write 꺼짐) — 옛 잔디 사용"); } }
            bool cobble = HasPathTex;   // 157차: 돌길 메시가 덮으니 스플랫 길은 밝은 흙 어깨만(진한 테두리·바퀴 자국 없음)
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
                    if (gPx != null)
                    {
                        float gu = Mathf.Repeat(x / GrassTileM, 1f), gv = Mathf.Repeat(z / GrassTileM, 1f);
                        int gi = Mathf.Clamp((int)(gv * gH), 0, gH - 1) * gW + Mathf.Clamp((int)(gu * gW), 0, gW - 1);
                        c = Color.Lerp(c, (Color)gPx[gi], 0.38f);
                    }
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
                    if (cobble) { float sh = Mathf.Clamp01((w + 0.9f - pd) / 0.9f); if (pd < w + 0.9f) c = Color.Lerp(c, Color.Lerp(path, sand, 0.45f), sh * 0.85f); }
                    else if (pd < w + 0.5f) c = Color.Lerp(c, pd < w ? Color.Lerp(path, pathEdge, n2 * 0.5f) : pathEdge, pd < w ? 1f : (w + 0.5f - pd) / 0.5f);
                    if (pd < w && !cobble)
                    {
                        // 149차(사용자: 「도로를 명확히」): 가운데가 밝고 가장자리로 갈수록 진한 모래길 + 자갈 점 + 바퀴 자국 결
                        c = Color.Lerp(Color.Lerp(path, Color.white, 0.10f), Color.Lerp(path, pathEdge, 0.6f), Mathf.Pow(pd / w, 1.6f));
                        float pebble = Mathf.PerlinNoise(x * 9f + 3f, z * 9f + 61f);
                        if (pebble > 0.72f) c = Color.Lerp(c, new Color(0.62f, 0.53f, 0.40f), Mathf.Clamp01((pebble - 0.72f) * 6f) * 0.85f);
                        if (pebble < 0.30f) c = Color.Lerp(c, Color.Lerp(path, Color.white, 0.18f), 0.35f);
                    }
                    if (!cobble && pd > w - 0.14f && pd < w + 0.26f) c = Color.Lerp(c, new Color(0.46f, 0.34f, 0.22f), 0.8f);   // 길 가장자리 진한 선(굵게)
                    else if (!cobble && pd >= w + 0.26f && pd < w + 0.55f) c = Color.Lerp(c, new Color(0.62f, 0.80f, 0.36f), 0.35f);   // 바깥 밝은 풀 테두리
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


        /// 157차(사용자: 「마을 이미지를 동물의 숲 수준으로」): 흙길 위에 진짜 돌길 — 폴리라인을 캣멀롬으로 부드럽게 잇고
        /// 띠 메시(폭 3.5 m, 지형 높이 +0.06)에 클링 생성 돌길 텍스처(Tex_Path)를 길이 방향으로 타일링. 텍스처가 없으면 그리지 않는다(스플랫 흙길 유지).
        static bool HasPathTex => Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Path") != null;
        static void BuildPathMesh(Transform root) => BuildPathMesh(root, Path, "PathMesh", 0.10f);
        static void BuildPathMesh(Transform root, Vector2[] Path, string goName, float lift)
        {
            var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Path");
            if (tex == null) { Debug.LogError("[PathMesh] Tex_Path not found"); return; }
            var pts = new List<Vector2>();
            for (int i = 0; i < Path.Length - 1; i++)
            {
                Vector2 p0 = Path[Mathf.Max(0, i - 1)], p1 = Path[i], p2 = Path[i + 1], p3 = Path[Mathf.Min(Path.Length - 1, i + 2)];
                float seg = Vector2.Distance(p1, p2); int n = Mathf.Max(2, Mathf.CeilToInt(seg / 0.6f));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    pts.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            pts.Add(Path[Path.Length - 1]);
            const float hw = 1.75f; float dist = 0f;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var tan = (i < pts.Count - 1 ? pts[i + 1] - p : p - pts[i - 1]).normalized;
                if (i > 0) dist += Vector2.Distance(p, pts[i - 1]);
                var nrm = new Vector2(-tan.y, tan.x);
                // 5 열(가장자리 살짝 낮게·어둡게 → 둥근 길 느낌)
                float[] off = { -hw, -hw * 0.55f, 0f, hw * 0.55f, hw };
                for (int c = 0; c < 5; c++)
                {
                    var q = p + nrm * off[c]; float edge = Mathf.Abs(off[c]) / hw;
                    verts.Add(new Vector3(q.x, Height(q.x, q.y) + lift - edge * edge * 0.05f, q.y));
                    uvs.Add(new Vector2(c / 4f * (hw * 2f / 2.7f), dist / 2.7f));   // 돌 한 장 ≈ 2.7 m 타일
                    cols.Add(Color.Lerp(Color.white, new Color(0.80f, 0.76f, 0.70f), edge * edge));
                }
                if (i > 0 && !(InGap(p.x, p.y) && InGap(pts[i - 1].x, pts[i - 1].y)))   // 190차: 끊긴 곳은 비운다
                {
                    int a = (i - 1) * 5, b = i * 5;
                    for (int c = 0; c < 4; c++) { tris.AddRange(new[] { a + c, a + c + 1, b + c, a + c + 1, b + c + 1, b + c }); }   // 위에서 보이게(시계 방향)
                }
            }
            var mesh = new Mesh { name = "VillagePath", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(goName, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            var m = ArtAssets.CreateTexturedLit(tex, new Color(0.98f, 0.96f, 0.92f), 0.02f); m.mainTextureScale = Vector2.one;
            if (m.HasProperty("_VertexColor")) m.SetFloat("_VertexColor", 1f);
            mr.sharedMaterial = m; mr.receiveShadows = true; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            int sx = 84, sz = 60; float w = 420f, len = 300f;   // 155차: 5 m 격자(셰이더 너울이 각지지 않게)
            var verts = new Vector3[(sx + 1) * (sz + 1)]; var uvs = new Vector2[verts.Length]; var uv2 = new Vector2[verts.Length]; var tris = new int[sx * sz * 6];
            for (int j = 0; j <= sz; j++) for (int i = 0; i <= sx; i++)
            {
                float fx = i / (float)sx, fz = j / (float)sz; float vx = fx * w - w * 0.5f, vz = -fz * len;
                // 161차(사용자: 「바닷물이 육지 가운데서 생성해서 왔다갔다」): 물가(z −20~−26)와 만·반도 가장자리는 땅이 해수면 근처라
                // 너울(±0.64 m)이 잔디 위로 솟았다 꺼졌다 했다 → 땅 높이가 해수면보다 1.2 m 이상 낮은 곳만 너울(uv2.x), 얕은 곳은 판을 가라앉힌다.
                float gh = Height(vx, vz + -20f) - SeaLevel;   // 판은 z=−20 에서 시작(부모 오프셋)
                float wgt = Mathf.Clamp01(Mathf.InverseLerp(-0.3f, -1.4f, gh));
                float sink = gh > -0.6f ? -1.0f : 0f;
                verts[j * (sx + 1) + i] = new Vector3(vx, sink, vz); uvs[j * (sx + 1) + i] = new Vector2(fx * 22f, fz * 16f); uv2[j * (sx + 1) + i] = new Vector2(wgt, 0f);
            }
            int t = 0;
            for (int j = 0; j < sz; j++) for (int i = 0; i < sx; i++)
            { int a = j * (sx + 1) + i; tris[t++] = a; tris[t++] = a + 1; tris[t++] = a + sx + 1; tris[t++] = a + 1; tris[t++] = a + sx + 2; tris[t++] = a + sx + 1; }
            var mesh = new Mesh { name = "VillageSea" }; mesh.vertices = verts; mesh.uv = uvs; mesh.uv2 = uv2; mesh.triangles = tris; mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            // 154차: 클링 생성 바다 텍스처(Tex_Sea) 우선, 없으면 옛 타일
            var klingSea = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Sea");
            var seaTex = klingSea != null ? klingSea : ArtAssets.LoadTexture("Sea_Turquoise_Tile");
            // 155차: 전용 바다 셰이더(두 겹 스크롤 + 셰이더 너울 + 마루 거품). 없으면 옛 툰 + SeaWaves
            var seaSh = Shader.Find("CoastRun/CoastSea");
            Material mat;
            if (seaSh != null && klingSea != null)
            {
                mat = new Material(seaSh) { name = "M_VillageSea" }; mat.SetTexture("_BaseMap", seaTex); mat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
                mat.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f)); mat.SetFloat("_Amp", 0.28f); mat.SetFloat("_Speed", 0.035f); mat.SetFloat("_Foam", 0.55f);
            }
            else mat = CoastMaterials.CreateToon(klingSea != null ? new Color(0.92f, 0.97f, 1f) : VillagePalette.SeaDeep, seaTex, 0.40f);
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (seaSh == null || klingSea == null) go.AddComponent<SeaWaves>();
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
                    // 154차: 파도 더 크게(멀리서도 보이게) — 긴 너울 + 잔물결
                    v[i] = b + Vector3.up * (Mathf.Sin(b.x * 0.09f + _t * 1.1f) * 0.26f + Mathf.Sin(b.z * 0.07f - _t * 0.8f) * 0.22f + Mathf.Sin((b.x + b.z) * 0.25f + _t * 2.2f) * 0.06f);
                }
                _m.vertices = v;
                if (_mat != null)
                {
                    var off = new Vector2(Mathf.Sin(_t * 0.15f) * 0.03f + _t * 0.006f, _t * 0.03f);
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
            VillageSky.Create(root);   // 170차: 카메라를 따라다니는 구름 링 + 비행기(비행운) + 새 떼
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
            var m = CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(() => VillageDayNight.SkyTint)); m.mainTexture = tex;   // 155차: 밤낮 틴트
            var mr = dome.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            dome.AddComponent<VillageSky.SkyDomeTint>();   // 170차: 밤낮 틴트를 매 프레임 갱신(라이브 색 추적은 에디터 전용이었다)
        }

        static void BuildSkyDressing(Transform root)
        {
            var cloudM = CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(new Color(0.94f, 0.97f, 1f)));
            var rng = new System.Random(7);
            // 168차(사용자: 「구름은 K-POP 러닝 구름으로」): 러닝 모드 CloudLayerScroller 가 쓰는 손그림 뭉게구름(Cloud_Cumulus_A/B/C, 알파) 빌보드
            var painted = new List<Texture2D>(); foreach (var n in new[] { "Cloud_Cumulus_A", "Cloud_Cumulus_B", "Cloud_Cumulus_C" }) { var t = ArtAssets.LoadTexture(n + "_V") ?? ArtAssets.LoadTexture(n); if (t != null) painted.Add(t); }   // 187차: 마을용 _V(남색 테두리 → 하늘색)
            // 168차: 태양 — 남동쪽 하늘 높이, 밤엔 사라짐(SunDisc)
            BuildSunDisc(root);
            for (int i = 0; i < 13; i++)
            {
                var c = new GameObject("Cloud").transform; c.SetParent(root, false);
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f), r = 140f + (float)rng.NextDouble() * 120f;
                if (i < 5) { ang = -Mathf.PI * 0.5f + (float)(rng.NextDouble() - 0.5) * 1.6f; r = 190f + (float)rng.NextDouble() * 90f; }
                // 157차: 안개 안쪽(70~110 m)·낮게 떠서 세로 화면에도 보이는 뭉게구름 4개(남쪽 하늘)
                else if (i >= 9) { ang = -Mathf.PI * 0.5f + (float)(rng.NextDouble() - 0.5) * 2.2f; r = 70f + (float)rng.NextDouble() * 40f; }
                c.localPosition = new Vector3(Mathf.Cos(ang) * r, (i >= 9 ? 22f : 34f) + (float)rng.NextDouble() * (i >= 9 ? 12f : 30f), Mathf.Sin(ang) * r);
                if (painted.Count > 0)
                {
                    var tex = painted[rng.Next(painted.Count)];
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "Billboard"; q.transform.SetParent(c, false);
                    float sc = (i >= 9 ? 26f : 40f) * (0.8f + (float)rng.NextDouble() * 0.5f), aspect = tex.width / (float)tex.height;
                    q.transform.localScale = new Vector3(sc * (rng.NextDouble() < 0.5 ? -1f : 1f), sc / aspect, 1f);
                    var cm = CoastMaterials.CreateTexturedTransparent(tex, new Color(1f, 1f, 1f, i >= 9 ? 0.95f : 0.8f)); CoastMaterials.SetNoFog(cm, 0f);
                    var qr = q.GetComponent<MeshRenderer>(); qr.sharedMaterial = CoastMaterials.SetFlat(cm); qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; qr.receiveShadows = false;
                    c.gameObject.AddComponent<CloudDrift>(); c.gameObject.AddComponent<SkyBillboard>();
                    continue;
                }
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
            // 187차(사용자: 「새를 파이어플라이로 다시」): 바다 위 갈매기 = Firefly 그림(Tex_Bird) 빌보드, 날갯짓은 세로 눌림
            var birdTex = ArtAssets.LoadTexture("Tex_Bird");
            var birdM = birdTex != null ? CoastMaterials.SetNoFog(CoastMaterials.CreateTexturedTransparent(birdTex, Color.white), 0f) : CoastMaterials.SetNoFog(CoastMaterials.CreateUnlit(new Color(0.20f, 0.22f, 0.30f)));
            for (int i = 0; i < 10; i++)
            {
                var b = new GameObject("Bird").transform; b.SetParent(root, false);
                VillageSky.MakeBird(b, birdM, 1.6f);   // 193차: 윤곽 메시 + 날개 피벗
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
        /// 168차: 하늘 빌보드 — 카메라를 향해 Y축만 돌린다(구름·태양)
        public class SkyBillboard : MonoBehaviour { void LateUpdate() { var cam = Camera.main; if (cam == null) return; var d = transform.position - cam.transform.position; d.y = 0f; if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d); } }
        /// 168차: 태양 원반 — 절차 텍스처(흰 중심·노란 테·부드러운 글로우), 밤(VillageDayNight.Night)엔 사라진다
        public class SunDisc : MonoBehaviour { Material _m; Color _c; void Start() { _m = GetComponent<MeshRenderer>().sharedMaterial; _c = _m.color; } void Update() { if (_m == null) return; var c = _c; c.a = _c.a * Mathf.Clamp01(1f - VillageDayNight.Night * 1.6f); _m.color = c; } }
        static Texture2D _sunTex;
        static void BuildSunDisc(Transform root)
        {
            if (_sunTex == null)
            {
                const int N = 256; _sunTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "SunDisc", wrapMode = TextureWrapMode.Clamp };
                var px = new Color[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f);
                    float core = Mathf.Clamp01((0.42f - d) / 0.04f);                       // 흰 원반(가장자리 부드럽게)
                    float glow = Mathf.Pow(Mathf.Clamp01(1f - d), 2.6f) * 0.55f;           // 바깥 글로우
                    var col = Color.Lerp(new Color(1f, 0.86f, 0.45f), new Color(1f, 0.99f, 0.92f), core);
                    px[y * N + x] = new Color(col.r, col.g, col.b, Mathf.Clamp01(core + glow));
                }
                _sunTex.SetPixels(px); _sunTex.Apply(true);
            }
            var sun = new GameObject("SunDisc").transform; sun.SetParent(root, false);
            sun.localPosition = new Vector3(150f, 62f, -270f);   // 남동쪽 하늘, 고도 ≈11°(마을 카메라는 24° 내려다봐서 지평선 위 조금만 보인다)
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.transform.SetParent(sun, false); q.transform.localScale = new Vector3(70f, 70f, 1f);
            var m = CoastMaterials.CreateTexturedTransparent(_sunTex, new Color(1f, 1f, 1f, 0.95f)); CoastMaterials.SetNoFog(m, 0f);
            var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = CoastMaterials.SetFlat(m); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            sun.gameObject.AddComponent<SkyBillboard>(); q.AddComponent<SunDisc>();
        }
        public class CloudDrift : MonoBehaviour { Vector3 _p; float _ph; void Start() { _p = transform.localPosition; _ph = Random.value * 6f; } void Update() { transform.localPosition = _p + new Vector3(Mathf.Sin(Time.time * 0.05f + _ph) * 8f, 0f, 0f); } }
        public class BirdFly : MonoBehaviour
        {
            public Vector3 center; public float radius = 12f, phase, speed = 0.3f; float _dir = 1f;
            void Update()
            {
                float t = Time.time * speed + phase;
                var p = center + new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t * 2.3f) * 1.5f, Mathf.Sin(t) * radius * 0.6f);
                var d = p - transform.position; transform.position = p;
                var cam = Camera.main; if (cam == null || transform.childCount == 0) return;
                var q = transform.GetChild(0);   // 187차: 카메라를 보는 그림 — 진행 방향이 화면 왼쪽이면 좌우 뒤집고, 날갯짓은 세로 눌림
                q.rotation = Quaternion.LookRotation(q.position - cam.transform.position, Vector3.up);
                if (d.sqrMagnitude > 1e-6f) _dir = Vector3.Dot(d, cam.transform.right) >= 0f ? 1f : -1f;
                q.localScale = new Vector3(1.6f * _dir, 1.6f, 1f); VillageSky.FlapBird(q, phase * 3f);   // 193차: 날개 퍼덕임
            }
        }
        public class Twinkle : MonoBehaviour { public float phase; Vector3 _s; void Start() { _s = transform.localScale; } void Update() { float k = Mathf.Max(0f, Mathf.Sin(Time.time * 2.2f + phase)); transform.localScale = new Vector3(_s.x * k, _s.y * k, 1f); } }

        // ── 소품 ─────────────────────────────────────────────────────────
        public static readonly List<Transform> Trees = new List<Transform>();
        public static Transform HeroHouse, MomHouse, Shop, Tower, Lighthouse;
        /// 156차(사용자: 「알바는 옆 건물 알바나라에서」): 우리집 옆 알바 소개소.
        public static Transform JobHouse;
        public static Transform Hospital;   // 189차(사용자: 「병원도 만들어줘」)

        static void BuildProps(Transform root)
        {
            Trees.Clear(); Houses.Clear();
            // 언덕 꼭대기: 주인공 집(제주 기와집) + 송전탑
            HeroHouse = VillageHouses.Build(root, Ground(0f, 36f), 180f, VillageHouses.Style.Hero, "우리집", "Our home"); Reg(HeroHouse, "우리집");
            Tower = PlaceTower(root, 15f, 41f);
            // 156차: 알바나라 — 우리집 오른쪽(동쪽) 옆, 문은 마당 쪽(남서)
            JobHouse = VillageHouses.Build(root, Ground(9.8f, 33.5f), 200f, VillageHouses.Style.Pastel, "알바나라", "Job Center", new Color(1f, 0.96f, 0.86f), VillagePalette.RoofSky, 0.8f); Reg(JobHouse, "알바나라");
            // 189차(사용자: 「병원도 만들어줘」): 큰길 동쪽 빈 풀밭(막힘 지도로 확인)에 흰 벽·빨간 지붕 병원, 문은 서쪽(큰길 쪽). 다락 창 자리에 빨간 십자
            Hospital = VillageHouses.Build(root, Ground(23f, 13.5f), -90f, VillageHouses.Style.Pastel, "병원", "Hospital", new Color(0.97f, 0.98f, 1f), new Color(0.90f, 0.36f, 0.38f), 0.85f); Reg(Hospital, "병원");
            if (Hospital != null)
            {
                foreach (Transform ch in Hospital) if (ch.name == "Attic" || ch.name == "AtticFrame") ch.gameObject.SetActive(false);
                var crossM = CoastMaterials.CreateLit(new Color(0.92f, 0.20f, 0.24f), 0.1f); var plateM = CoastMaterials.CreateLit(Color.white, 0.1f);
                void CB(string n, Vector3 lp, Vector3 s, Material m) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(Hospital, false); g.transform.localPosition = lp; g.transform.localScale = s; g.GetComponent<MeshRenderer>().sharedMaterial = m; }
                float cz = 2.0f + 0.5f, cy = 2.7f + 0.85f;
                CB("CrossPlate", new Vector3(0f, cy, cz), new Vector3(1.05f, 1.05f, 0.08f), plateM);
                CB("Cross", new Vector3(0f, cy, cz + 0.05f), new Vector3(0.78f, 0.24f, 0.06f), crossM);
                CB("Cross", new Vector3(0f, cy, cz + 0.05f), new Vector3(0.24f, 0.78f, 0.06f), crossM);
            }
            // 마을: 엄마 집(초가) · 구멍가게(그림 파사드 상가)
            // 140차(시안): 텃밭(0,-6)에서 바다 쪽을 보면 왼쪽에 초가(엄마 집), 오른쪽 아래로 파스텔 집들 + 상점, 절벽 위 등대
            // (세로 화면은 가로 시야가 좁아(±18°) 텃밭에서 남쪽으로 보이는 원뿔 안에 배치)
            MomHouse = VillageHouses.Build(root, Ground(4.8f, -19.9f), 25f, VillageHouses.Style.Mom, "엄마 집", "Mom's", null, null, 1.1f); Reg(MomHouse, "엄마 집");   // 189차(사용자: 「바닷가 끝쪽에서 길 쪽으로 못 감」): −21.5 → −19.9 로 1.6 m 북쪽 — 동쪽 모래사장에서 엄마 집 남쪽(바다 쪽)으로 큰길까지 걸어갈 틈을 낸다
            Shop = VillageHouses.Build(root, Ground(-5.8f, -33.5f), 20f, VillageHouses.Style.Shop, "마을상점", "Village shop", null, null, 0.75f);
            // 147차: 해녀네는 상점 바로 뒤(북)·바다(동)에 막혀 → 서쪽 골목(꽃집 쪽 길)을 보게 돌려 문 앞에 설 자리를 만든다
            Reg(VillageHouses.Build(root, Ground(-6.2f, -44f), -80f, VillageHouses.Style.Pastel, "해녀네", "Haenyeo's", VillagePalette.WallCream, VillagePalette.RoofMint, 0.75f), "해녀네");
            Reg(VillageHouses.Build(root, Ground(-23f, -58f), 90f, VillageHouses.Style.Pastel, "등대지기 집", "Keeper's", new Color(0.93f, 0.96f, 1f), VillagePalette.RoofSky, 0.75f), "등대지기 집");
            Reg(VillageHouses.Build(root, Ground(-23f, -35f), 90f, VillageHouses.Style.Pastel, "꽃집", "Flower shop", VillagePalette.WallCream, VillagePalette.RoofPink, 0.7f), "꽃집");
            Reg(VillageHouses.Build(root, Ground(-23.5f, -46.5f), 90f, VillageHouses.Style.Pastel, "바다 카페", "Sea cafe", new Color(1f, 0.94f, 0.95f), VillagePalette.RoofPink, 0.75f), "바다 카페");
            Reg(VillageHouses.Build(root, Ground(-6.3f, -54.5f), -80f, VillageHouses.Style.Pastel, "서퍼 하우스", "Surf house", new Color(1f, 0.97f, 0.88f), VillagePalette.RoofCream, 0.7f), "서퍼 하우스");
            // 153차(사용자: 「언덕이 휑하다」): 언덕 채우기 — 소나무 숲·풍차·벤치·바위·꽃밭·통나무 울타리·억새
            BuildHillDeco(root);
            VillageRanch.Build(root);   // 176차: 언덕 서쪽 넓은 방목장(말·헛간·목장집)
            BuildBoundaryDeco(root);   // 171차: 경계 나무·바위 띠
            VillageEast.Build(root);   // 194차: 동쪽 새 동네(은행·제주 브런치 카페·차도·버스 정류장·귤밭·유채꽃)
            // 정자·벤치·돌하르방·귤 매대
            var jj = Place(root, "Prop_Jeongja", 28f, -6f, 200f, 1f); if (jj == null) Place(root, "Prop_Pavilion", 28f, -6f, 200f, 1f); else TintJeongja(jj);   // 192차: 블렌더로 새로 만든 한옥 정자(곡선 기와지붕·마루·난간), 없으면 옛 정자
            Place(root, "Prop_Bench", -3f, -24f, -90f, 1f); Place(root, "Prop_Bench", -14.5f, -43.5f, 90f, 1f);   // 190차: 집 간격 벌리며 길 서쪽으로
            Place(root, "Prop_Hareubang", 6.5f, 2f, 200f, 1f); Place(root, "Prop_Hareubang", -6.5f, 2f, 160f, 1f);
            Place(root, "Prop_OrangeStall", 12f, 8f, 240f, 1f);
            Place(root, "Kerb_Onggi", -16f, -4f, 0f, 1f); Place(root, "Kerb_PlanterWood", -15f, -13f, 20f, 1f);
            Place(root, "Kerb_Buoys", -1f, -32f, 0f, 1f); Place(root, "Kerb_CrateStack", 3f, -32f, 30f, 1f);
            Place(root, "Prop_UtilityPole", 9f, 4f, 0f, 1f); Place(root, "Prop_UtilityPole", 26f, -8f, 0f, 1f);
            // 돌담(길가·집 앞)
            // 141차: 현무암 텍스처는 마을 조명에서 새카만 상자로 보여서 밝은 돌색으로
            var stoneTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Stone");
            var stoneM = stoneTex != null ? ArtAssets.CreateTexturedLit(stoneTex, new Color(0.92f, 0.90f, 0.86f), 0.02f) : CoastMaterials.CreateLit(new Color(0.66f, 0.63f, 0.58f));   // 151차: 현무암 돌담 텍스처
            void StoneWall(float x, float z) { var t = Place(root, "Prop_StoneWall", x, z, 0f, 1f, true); /* 154차: 돌담도 막힘 */ if (t == null) return; foreach (var r in t.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = stoneM; r.sharedMaterials = arr; } }
            // 174차-3(사용자: 「집앞에 길게 뻗은 현무암을 집 둘레 경계로」): 한 줄로 뻗던 돌담을 우리집 마당을 두르는 담으로.
            // 앞 가운데는 길이 지나가게 비우고, 송전탑 자리는 건너뛴다.
            void StoneRing(float x, float z, float yaw)
            {
                if (Mathf.Abs(x) < 3.2f && z < 28f) return;                      // 대문(길)
                if (Vector2.Distance(new Vector2(x, z), new Vector2(11f, 38f)) < 5.5f) return;   // 송전탑
                var t = Place(root, "Prop_StoneWall", x, z, yaw, 1f, true);
                if (t == null) return;
                // 176차-2(사용자 캡처: 「경사에서 담 조각이 떠 보인다」): 경사지에선 조각을 땅에 묻어 아랫단 틈을 없앤다.
                // 양옆 땅 높이 차이만큼 더 내려 앉힌다(평지 0.18 m, 가파를수록 깊게).
                float hL = Height(x - 1.1f, z), hR = Height(x + 1.1f, z), hB = Height(x, z - 1.1f), hF = Height(x, z + 1.1f);
                float drop = 0.18f + Mathf.Max(Mathf.Abs(hL - hR), Mathf.Abs(hB - hF)) * 0.9f;
                t.position -= new Vector3(0f, Mathf.Min(drop, 0.85f), 0f);
                foreach (var r in t.GetComponentsInChildren<Renderer>()) { var arr = r.sharedMaterials; for (int k = 0; k < arr.Length; k++) arr[k] = stoneM; r.sharedMaterials = arr; }
            }
            // 176차-2: 조각 사이가 벌어져 판때기처럼 보여서 간격을 1.1 m 로 좁혀 겹치게 놓는다
            // 176차-2 재수정: Prop_StoneWall 은 길이가 Z 축이다 — 앞뒤 담(x 방향)은 yaw 90, 옆 담(z 방향)은 yaw 0
            for (float x = -14.7f; x <= 14.8f; x += 1.6f) { StoneRing(x, 26.4f, 90f); StoneRing(x, 40.8f, 90f); }
            for (float z = 27.0f; z <= 40.3f; z += 1.6f) { StoneRing(-14.7f, z, 0f); StoneRing(14.7f, z, 0f); }
            for (int i = 0; i < 3; i++) StoneWall(-20f + i * 4.2f, 2f);
            for (int i = 0; i < 3; i++) StoneWall(12f + i * 4.2f, 5f);
            // 나무: 귤나무(흔들면 귤) + 야자수(시안처럼 텃밭 주변·바다 쪽에 여럿)
            float[,] oranges = { { -9f, 15f }, { 11f, 14f }, { 24f, 6f }, { -27f, 8f }, { -24f, -14f }, { 30f, -2f }, { -7f, 33f }, { 13.5f, 29.5f } };   // 156차: (8,32) 귤나무는 알바나라 자리라 옮김
            for (int i = 0; i < oranges.GetLength(0); i++) { var tr = Place(root, "Prop_OrangeTree", oranges[i, 0], oranges[i, 1], i * 47f, 1f); if (tr != null) { tr.name = "Tree_Orange_" + i; Trees.Add(tr); GroundBlob.Static(root, tr.position, 2.2f, 1.7f, 0.22f); } }
            float[,] palms = { { -2.8f, -20.5f, 0.62f }, { 5.5f, -27.5f, 0.7f }, { 8.5f, -27f, 0.75f }, { 11.5f, -26.5f, 0.7f }, { 2.5f, -30.5f, 0.6f }, { -14.5f, -33f, 0.8f }, { -13f, -50f, 0.8f }, { -4.9f, -48.5f, 0.7f }, { -10f, -60f, 0.8f }, { 11f, -14f, 0.9f }, { -20f, -44f, 0.9f }, { -4f, -62f, 0.7f }, { 11f, -17f, 0.85f } };   // 190차: 집 간격을 벌리며 길·집과 겹치던 야자수 3그루 비킴
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
                if (n == "Terrain" || n == "Sea" || n == "Foam" || n == "Vista" || n == "VistaSide" || n == "FarHill" || n == "Bound" || n == "Sun" || n == "SunDisc" || n == "Fill" || n == "Crops" || n == "Yard" || n == "Ghost" || n == "Pampas" || n == "FlowerClump" || n == "Sparkle" || n == "Cloud" || n == "Bird" || n == "SkyDome" || n == "CliffRock" || n.Contains("StoneWall")) continue;
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

        static readonly Vector2[] BuildingCenters = { new Vector2(VillageEast.BankX, VillageEast.BankZ), new Vector2(VillageEast.CafeX, VillageEast.CafeZ), new Vector2(VillageEast.StopX, VillageEast.StopZ), new Vector2(0f, 36f), new Vector2(4.8f, -19.9f), new Vector2(-5.8f, -33.5f), new Vector2(15f, 41f), new Vector2(28f, -6f), new Vector2(-6.2f, -44f), new Vector2(-23f, -58f), new Vector2(-23f, -35f), new Vector2(-23.5f, -46.5f), new Vector2(-6.3f, -54.5f), new Vector2(0f, -6f) };

        internal static void Lamp(Transform root, float x, float z)
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

        internal static void Bush(Transform root, float x, float z, System.Random rng)
        {
            var go = new GameObject("Bush"); go.transform.SetParent(root, false); go.transform.position = Ground(x, z);
            // 162차(사용자: 「원형 장애물 더 이쁘게」): 키트 VBush_A/B(잎 뭉치 + 열매, AO 정점색). 없으면 옛 구 뭉치
            var kb = JejuKit.Spawn("VBush_" + (rng.NextDouble() < 0.5 ? "A" : "B"), go.transform, Vector3.zero, (float)rng.NextDouble() * 360f, 1.0f + (float)rng.NextDouble() * 0.4f);
            if (kb != null)
            {
                var s2 = kb.gameObject.AddComponent<WindSway>(); s2.Amp = 2.5f; s2.Speed = 1.1f;
                var bc2 = go.AddComponent<BoxCollider>(); bc2.center = new Vector3(0f, 0.4f, 0f); bc2.size = new Vector3(1.3f, 0.8f, 1.3f);
                return;
            }
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
            // 174차-3(사용자: 「바닷가 바위도 귀여운 스타일로」): 각진 현무암 키트(VCliff)를 버리고
            // 마을 바위와 같은 둥근 덩이로 — 바닷가는 청회색·라벤더·회색(찬 색)만 골라 쓴다.
            int[] coolMat = { 3, 4, 5, 9, 10, 11 };
            void R(float x, float z, float s, Material m)
            {
                float baseY = SeaLevel + s * 0.10f;
                var t = PastelBoulder(root, new Vector3(x, baseY, z), s * 0.95f, rng, rng.NextDouble() < 0.22, true, coolMat[rng.Next(coolMat.Length)]);
                t.name = "CliffRock";
                int extra = 1 + rng.Next(2);
                for (int i = 0; i < extra; i++)
                {
                    float s2 = s * (0.38f + (float)rng.NextDouble() * 0.36f);
                    float a = (float)rng.NextDouble() * 6.283f;
                    var p2 = new Vector3(x + Mathf.Cos(a) * s * 1.05f, baseY - s * 0.06f, z + Mathf.Sin(a) * s * 1.05f);
                    PastelBoulder(root, p2, s2, rng, false, true, coolMat[rng.Next(coolMat.Length)]).name = "CliffRock";
                }
            }
            for (float z = -38f; z > -64f; z -= 2.4f) { float s = 1.2f + (float)rng.NextDouble() * 0.7f; R(-3.6f + (float)rng.NextDouble() * 0.6f - (z < -46f ? 0.8f : 0f), z, s, (z % 4.8f) > -2.4f ? rockM : rockD); }
            // 반도 근처 가장자리는 덤불로(시안: 오른쪽 절벽 위 초록)
            for (float z = -27f; z > -38f; z -= 3.6f) Bush(root, -2.8f + (float)rng.NextDouble() * 1.0f, z, rng);
            for (float x = -3f; x > -28f; x -= 3f) { float s = 2.4f + (float)rng.NextDouble() * 1.6f; R(x, -63.5f + (float)rng.NextDouble(), s, rockM); }
            // 171차(사용자: 「끝에 절벽 모습」): 반도 서쪽 가장자리(x≈−28)와 끝(z≈−65)에 절벽 바위 띠를 두 겹으로 — 땅이 그냥 끊기던 곳이 바위 절벽으로 보인다
            for (float z = -36f; z > -64f; z -= 2.6f) { float s = 1.6f + (float)rng.NextDouble() * 1.0f; R(-27.6f + (float)rng.NextDouble() * 0.8f, z, s, (z % 5.2f) > -2.6f ? rockD : rockM); }
            for (float z = -38f; z > -62f; z -= 4.5f) { float s = 1.0f + (float)rng.NextDouble() * 0.6f; R(-26.2f + (float)rng.NextDouble() * 0.6f, z, s, rockM); }
            for (float x = -4f; x > -27f; x -= 2.2f) { float s = 1.4f + (float)rng.NextDouble() * 0.8f; R(x, -65.6f + (float)rng.NextDouble() * 0.5f, s, (x % 4.4f) > -2.2f ? rockD : rockM); }
            for (float z = -31f; z > -62f; z -= 3.2f) { float s = 2.2f + (float)rng.NextDouble() * 1.4f; R(-28.5f, z, s, rockD); }
            // 만(灣) 쪽 물가 바위
            R(6.8f, -27.5f, 1.3f, rockD); R(10.5f, -28f, 1.6f, rockM); R(4.2f, -29f, 1.1f, rockM);
        }

        static void Rock(Transform root, float x, float z, System.Random rng)
        {
            // 174차(사용자: 「옛 껍데기 걷어내고 다시 다 그려줘」): 옛 VStone 키트·칙칙한 회색 구를 버리고 시안 파스텔 덩이로 통일
            float s = 0.55f + (float)rng.NextDouble() * 0.7f;
            var g = Ground(x, z) + new Vector3(0f, s * 0.22f, 0f);
            var t = PastelBoulder(root, g, s, rng, rng.NextDouble() < 0.45);
            t.name = "Rock";
            if (rng.NextDouble() < 0.55)   // 시안처럼 큰 덩이 옆에 작은 덩이가 붙어 있다
            {
                float s2 = s * (0.45f + (float)rng.NextDouble() * 0.28f);
                float a = (float)rng.NextDouble() * 6.28f;
                var g2 = Ground(x + Mathf.Cos(a) * s * 1.15f, z + Mathf.Sin(a) * s * 1.15f) + new Vector3(0f, s2 * 0.22f, 0f);
                PastelBoulder(root, g2, s2, rng, rng.NextDouble() < 0.3).name = "Rock";
            }
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
                case "Prop_Jeongja": return 4.4f;
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
            SharpMesh.Apply(go, model);   // 151차: 블렌더에서 각 정리·AO 정점색을 구운 메시로 교체(있을 때)
            Normalize(go, targetH ?? TargetHeight(model), g.y - 0.04f);
            return go;
        }

        internal static Transform Place(Transform root, string model, float x, float z, float yaw, float scale, bool collide = true)
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

        internal static void Reg(Transform house, string name) { if (house == null) return; float fz = 2.2f; foreach (Transform ch in house) if (ch.name == "Door") { fz = ch.localPosition.z; break; } Houses.Add((house, name, house.TransformPoint(new Vector3(0f, 0f, fz + 1.1f)))); }

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
            // 190차(사용자: 「등대 밑으로 주인공이 들어가 버린다」): 바위 덩어리에 콜라이더가 없어 절벽 속(등대 밑)으로 걸어 들어갔다 → 바위 모양 그대로 막는다
            foreach (Transform ch in go.transform) { var mc = ch.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = ch.GetComponent<MeshFilter>().sharedMesh; }
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

        internal static void Pampas(Transform root, float x, float z, System.Random rng)
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

        internal static void FlowerClump(Transform root, float x, float z, System.Random rng)
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
            VillageLivestock.Build(root, save);   // 171차: 농장 가축도 같이 다시 세운다
            var host = root.Find("Crops"); if (host != null) BuildingOutline.Attach(host, 0.02f);
        }
    }
}
