using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 187차(사용자: 「돈으로 바닥에 도로를 깔 수 있게. 한 칸씩, 한 칸 500G. 도로가 이어진 곳만 자동이동」).
    /// 2 m 격자. 큰길(Path)·방목장 갈림길(RanchPath)은 처음부터 도로망, 플레이어가 깐 칸(SaveData.roadCells)이 더해진다.
    /// 자동이동은 내 발밑과 목적지가 각각 도로망 칸에 닿아 있고(SnapM 안) 그 둘이 8방향으로 이어져 있을 때만 된다.
    public static class VillageRoad
    {
        public const float Cell = 2f;
        public const int Price = 500;
        public const float SnapM = 7f;      // 도로망에서 이만큼 안이면 「도로에 닿음」(집 문·가게 앞 등)
        public static int Key(int cx, int cz) => (cx + 1000) * 2000 + (cz + 1000);
        public static void Unkey(int k, out int cx, out int cz) { cx = k / 2000 - 1000; cz = k % 2000 - 1000; }
        public static int KeyAt(Vector3 p) => Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
        static Vector2 Center2(int k) { Unkey(k, out int cx, out int cz); return new Vector2((cx + 0.5f) * Cell, (cz + 0.5f) * Cell); }
        public static Vector3 Center(int k) { Unkey(k, out int cx, out int cz); return VillageWorld.Ground((cx + 0.5f) * Cell, (cz + 0.5f) * Cell); }

        static HashSet<int> _base;
        /// 기본 도로망(큰길·방목장 갈림길) — 길 폭 3.5 m 가 덮는 칸
        public static HashSet<int> Base()
        {
            if (_base != null) return _base;
            _base = new HashSet<int>();
            void Line(Vector2[] path)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    Vector2 a = path[i], b = path[i + 1]; int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f);
                    for (int s = 0; s <= n; s++)
                    {
                        var p = Vector2.Lerp(a, b, s / (float)n);
                        for (float ox = -1.2f; ox <= 1.21f; ox += 1.2f) for (float oz = -1.2f; oz <= 1.21f; oz += 1.2f)
                            _base.Add(Key(Mathf.FloorToInt((p.x + ox) / Cell), Mathf.FloorToInt((p.y + oz) / Cell)));
                    }
                }
            }
            Line(VillageWorld.Path); Line(VillageWorld.RanchPath);
            foreach (var b in VillageWorld.Branches) Line(b);   // 187차: 알바나라·송전탑·정자 갈림길
            _base.RemoveWhere(k => { var c = Center2(k); return VillageWorld.InGap(c.x, c.y); });   // 190차: 끊긴 길
            return _base;
        }
        public static bool IsBase(int k) => Base().Contains(k);
        public static bool IsRoad(SaveData s, int k) => IsBase(k) || (s != null && s.roadCells != null && s.roadCells.Contains(k));

        static HashSet<int> Net(SaveData s) { var n = new HashSet<int>(Base()); if (s != null && s.roadCells != null) foreach (var k in s.roadCells) n.Add(k); return n; }

        static int Nearest(HashSet<int> net, Vector3 p, float maxM)
        {
            int cx0 = Mathf.FloorToInt(p.x / Cell), cz0 = Mathf.FloorToInt(p.z / Cell), r = Mathf.CeilToInt(maxM / Cell);
            int best = int.MinValue; float bd = maxM * maxM;
            for (int dx = -r; dx <= r; dx++) for (int dz = -r; dz <= r; dz++)
            {
                int k = Key(cx0 + dx, cz0 + dz); if (!net.Contains(k)) continue;
                float x = (cx0 + dx + 0.5f) * Cell - p.x, z = (cz0 + dz + 0.5f) * Cell - p.z, d = x * x + z * z;
                if (d <= bd) { bd = d; best = k; }
            }
            return best;
        }

        /// 도로망 위 경로. 못 이으면 null, why 에 이유(「start」 = 내가 도로 밖, 「goal」 = 목적지가 도로 밖, 「gap」 = 끊김)
        /// 220차: 열림/잠김은 이 격자(끊긴 길·내가 깐 칸 포함)로 정하고, 걷는 모양은 길 가운데선(VillageRoadNet)을 따른다 — 격자 경로는 모서리를 가로질러 담·가로등에 걸렸다.
        ///   가운데선으로 못 이으면(내가 깐 도로로만 이어진 곳 등) 예전 격자 경로 그대로.
        public static List<Vector3> Route(SaveData s, Vector3 from, Vector3 to, out string why)
        {
            var grid = GridRoute(s, from, to, out why);
            if (grid == null) return null;
            return VillageRoadNet.Route(from, to, SnapM) ?? grid;
        }

        static List<Vector3> GridRoute(SaveData s, Vector3 from, Vector3 to, out string why)
        {
            why = null; var net = Net(s);
            int a = Nearest(net, from, SnapM), b = Nearest(net, to, SnapM);
            if (a == int.MinValue) { why = "start"; return null; }
            if (b == int.MinValue) { why = "goal"; return null; }
            var prev = new Dictionary<int, int> { [a] = a }; var q = new Queue<int>(); q.Enqueue(a);
            while (q.Count > 0 && !prev.ContainsKey(b))
            {
                int k = q.Dequeue(); Unkey(k, out int cx, out int cz);
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue; int n = Key(cx + dx, cz + dz);
                    if (!net.Contains(n) || prev.ContainsKey(n)) continue;
                    if (dx != 0 && dz != 0 && !net.Contains(Key(cx + dx, cz)) && !net.Contains(Key(cx, cz + dz))) continue;   // 대각선은 모서리가 붙어 있을 때만
                    prev[n] = k; q.Enqueue(n);
                }
            }
            if (!prev.ContainsKey(b)) { why = "gap"; return null; }
            var cells = new List<int>(); for (int k = b; ; k = prev[k]) { cells.Add(k); if (k == a) break; }
            cells.Reverse();
            var pts = new List<Vector3>();
            for (int i = 1; i < cells.Count; i++) if (i % 2 == 0 || i == cells.Count - 1) pts.Add(Center(cells[i]));
            pts.Add(to);
            return pts;
        }

        // ── 보이는 도로 칸 ──
        static Material _mat, _ghostM;
        static Material Mat()
        {
            if (_mat != null) return _mat;
            var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Path");
            _mat = tex != null ? ArtAssets.CreateTexturedLit(tex, new Color(0.98f, 0.96f, 0.92f), 0.02f) : CoastMaterials.CreateLit(new Color(0.86f, 0.78f, 0.62f));
            if (_mat.HasProperty("_CurveWeight")) _mat.SetFloat("_CurveWeight", 1f);
            return _mat;
        }
        public static Material GhostMat(bool ok)
        {
            if (_ghostM == null) { _ghostM = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.45f)); if (_ghostM.HasProperty("_CurveWeight")) _ghostM.SetFloat("_CurveWeight", 1f); }
            VillageSky.SetTint(_ghostM, ok ? new Color(0.45f, 1f, 0.55f, 0.72f) : new Color(1f, 0.35f, 0.40f, 0.72f));
            return _ghostM;
        }
        /// 칸 하나를 지형을 따라 휘는 4×4 판으로
        public static Mesh TileMesh(int k, float lift)
        {
            Unkey(k, out int cx, out int cz); float x0 = cx * Cell, z0 = cz * Cell; const int N = 4;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= N; j++) for (int i = 0; i <= N; i++)
            {
                float x = x0 + Cell * i / N, z = z0 + Cell * j / N;
                v.Add(new Vector3(x, VillageWorld.Height(x, z) + lift, z)); uv.Add(new Vector2(x / 2.7f, z / 2.7f));
            }
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { int a = j * (N + 1) + i, b = a + N + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "RoadTile" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        public static GameObject Spawn(Transform parent, int k)
        {
            var go = new GameObject("RoadTile", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = TileMesh(k, 0.09f);
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = Mat(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = true;
            return go;
        }
        public static Transform BuildAll(Transform world, SaveData s)
        {
            var old = world.Find("Roads"); if (old != null) Object.Destroy(old.gameObject);
            var root = new GameObject("Roads").transform; root.SetParent(world, false);
            if (s != null && s.roadCells != null) foreach (var k in s.roadCells) Spawn(root, k);
            return root;
        }
    }
}
