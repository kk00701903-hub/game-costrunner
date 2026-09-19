using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 147차: 절차적 조경 배치 — 잔디 영역에 꽃·클로버·풀포기·작은 덤불을 촘촘히 뿌린다.
    /// 규칙: 스플랫이 잔디색인 곳만, 길·텃밭·마당·건물 발치·바다/만 제외, 지터 격자(간격 1.4 m) + 확률로 종류 선택.
    /// 그리기: 종류별로 하나의 메시로 합쳐(CombineMeshes) 드로우콜 6개 — 수천 개를 개별 오브젝트로 두지 않는다.
    public static class VillageFlora
    {
        struct Kind { public string name; public Color col; public float weight; public int type; }   // type 0 꽃 1 클로버 2 풀포기 3 덤불
        static readonly Kind[] Kinds = {
            new Kind { name = "FlowerPink",   col = new Color(1f, 0.62f, 0.76f), weight = 0.14f, type = 0 },
            new Kind { name = "FlowerWhite",  col = new Color(1f, 0.98f, 0.94f), weight = 0.14f, type = 0 },
            new Kind { name = "FlowerYellow", col = new Color(1f, 0.90f, 0.42f), weight = 0.10f, type = 0 },
            new Kind { name = "FlowerLilac",  col = new Color(0.78f, 0.66f, 0.96f), weight = 0.06f, type = 0 },
            new Kind { name = "Clover",       col = new Color(0.42f, 0.74f, 0.36f), weight = 0.26f, type = 1 },
            new Kind { name = "GrassTuft",    col = new Color(0.55f, 0.82f, 0.40f), weight = 0.30f, type = 2 },
        };

        public static int Scatter(Transform root, float spacing = 1.4f, int seed = 147)
        {
            var splat = VillageWorld.SplatTex; if (splat == null) return 0;
            var rng = new System.Random(seed);
            var lists = new List<CombineInstance>[Kinds.Length];
            for (int i = 0; i < lists.Length; i++) lists[i] = new List<CombineInstance>();
            var protoMesh = new Mesh[4]; protoMesh[0] = FlowerMesh(); protoMesh[1] = CloverMesh(); protoMesh[2] = TuftMesh(); protoMesh[3] = FlowerMesh();
            int placed = 0; float half = VillageWorld.Half - 3f;
            for (float z = -half; z < half; z += spacing)
                for (float x = -half; x < half; x += spacing)
                {
                    float px = x + ((float)rng.NextDouble() - 0.5f) * spacing * 0.9f, pz = z + ((float)rng.NextDouble() - 0.5f) * spacing * 0.9f;
                    if (!GrassAt(splat, px, pz)) continue;
                    if (VillageWorld.PathDist(px, pz) < 2.4f) continue;
                    if (Mathf.Abs(px - VillageWorld.GardenX) < 5.4f && Mathf.Abs(pz - VillageWorld.GardenZ) < 4.9f) continue;   // 텃밭
                    if (Mathf.Abs(px) < 10f && pz > 24f && pz < 34f) continue;                                                     // 마당
                    if (VillageWorld.NearBuildingPublic(px, pz, 4.2f)) continue;
                    if (VillageWorld.Bay(px, pz) > 0.02f) continue;
                    float h = VillageWorld.Height(px, pz); if (h < VillageWorld.SeaLevel + 0.4f) continue;
                    // 밀도: 잔디는 촘촘히, 언덕 위는 듬성
                    if (rng.NextDouble() < 0.28 + Mathf.Clamp01(h / 11f) * 0.4f) continue;
                    int k = Pick(rng);
                    float s = 0.7f + (float)rng.NextDouble() * 0.6f; float yaw = (float)rng.NextDouble() * 360f;
                    var m = Matrix4x4.TRS(new Vector3(px, h, pz), Quaternion.Euler(0f, yaw, 0f), Vector3.one * s);
                    lists[k].Add(new CombineInstance { mesh = protoMesh[Kinds[k].type], transform = m });
                    placed++;
                }
            var host = new GameObject("Flora").transform; host.SetParent(root, false);
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (lists[i].Count == 0) continue;
                // 65k 정점 한도 → 32비트 인덱스
                var mesh = new Mesh { name = "Flora_" + Kinds[i].name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(lists[i].ToArray(), true, true, false); mesh.RecalculateBounds();
                var go = new GameObject(Kinds[i].name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(host, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = CoastMaterials.CreateLit(Kinds[i].col, 0.05f);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = true;
                var sway = go.AddComponent<WindSway>(); sway.Amp = 1.2f; sway.Speed = 1.1f; sway.Nudgeable = false;
            }
            return placed;
        }

        static int Pick(System.Random rng)
        {
            float total = 0f; foreach (var k in Kinds) total += k.weight;
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < Kinds.Length; i++) { r -= Kinds[i].weight; if (r <= 0f) return i; }
            return Kinds.Length - 1;
        }

        static bool GrassAt(Texture2D splat, float x, float z)
        {
            float u = (x + VillageWorld.Half) / (2f * VillageWorld.Half), v = (z + VillageWorld.Half) / (2f * VillageWorld.Half);
            if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
            var c = splat.GetPixelBilinear(u, v);
            return c.g > c.r * 1.08f && c.g > c.b * 1.3f;   // 잔디(초록)만 — 모래·길·흙 제외
        }

        // ── 아주 가벼운 메시들(양면 쿼드) ──
        static void Quad(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            var nn = Vector3.Cross(b - a, c - a).normalized; for (int k = 0; k < 4; k++) n.Add(nn);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            i = v.Count; v.Add(a); v.Add(d); v.Add(c); v.Add(b); for (int k = 0; k < 4; k++) n.Add(-nn);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        static Mesh Finish(List<Vector3> v, List<Vector3> n, List<int> t, string name)
        {
            var m = new Mesh { name = name }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }
        static Mesh FlowerMesh()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            // 줄기(교차 쿼드) + 꽃(5장 꽃잎을 위에서 본 별 모양 + 중심)
            Quad(v, n, t, new Vector3(-0.015f, 0f, 0f), new Vector3(0.015f, 0f, 0f), new Vector3(0.015f, 0.26f, 0f), new Vector3(-0.015f, 0.26f, 0f));
            for (int p = 0; p < 5; p++)
            {
                float a = p * 72f * Mathf.Deg2Rad, a2 = a + 0.55f, a1 = a - 0.55f;
                var c = new Vector3(0f, 0.27f, 0f);
                Quad(v, n, t, c, c + new Vector3(Mathf.Cos(a1) * 0.06f, 0.01f, Mathf.Sin(a1) * 0.06f), c + new Vector3(Mathf.Cos(a) * 0.11f, 0.02f, Mathf.Sin(a) * 0.11f), c + new Vector3(Mathf.Cos(a2) * 0.06f, 0.01f, Mathf.Sin(a2) * 0.06f));
            }
            return Finish(v, n, t, "Flower");
        }
        static Mesh CloverMesh()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int p = 0; p < 3; p++)
            {
                float a = p * 120f * Mathf.Deg2Rad; var c = new Vector3(0f, 0.07f, 0f);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var side = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                Quad(v, n, t, c, c + side * 0.06f + dir * 0.05f, c + dir * 0.13f, c - side * 0.06f + dir * 0.05f);
            }
            Quad(v, n, t, new Vector3(-0.01f, 0f, 0f), new Vector3(0.01f, 0f, 0f), new Vector3(0.01f, 0.07f, 0f), new Vector3(-0.01f, 0.07f, 0f));
            return Finish(v, n, t, "Clover");
        }
        static Mesh TuftMesh()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int b = 0; b < 3; b++)
            {
                float a = b * 60f * Mathf.Deg2Rad; var side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.10f;
                Quad(v, n, t, -side, side, side * 0.15f + new Vector3(0f, 0.22f, 0f), -side * 0.15f + new Vector3(0f, 0.22f, 0f));
            }
            return Finish(v, n, t, "Tuft");
        }
    }
}
