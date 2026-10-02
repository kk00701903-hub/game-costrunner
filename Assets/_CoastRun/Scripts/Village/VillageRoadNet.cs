using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 220차(사용자: 「자동이동을 위해 도로 정비 — 도로 따라 자동이동, 도로엔 장애물 없음」):
    ///   ① 돌길 메시와 똑같은 곡선(Catmull-Rom, 0.6 m)으로 길 가운데선을 만들고, 자동이동은 격자 대신 **그 가운데선을 따라** 걷는다
    ///      (예전 2 m 격자 경로는 모서리를 가로질러 담·집·가로등에 걸렸다).
    ///   ② 마을이 다 지어진 뒤 길 위(가운데선 1.3 m 안)에 놓인 소품(가로등·벤치·야자수·덤불·표지판·화분·나무…)은 길 밖으로 비켜 세우고,
    ///      길을 가로지르는 울타리·돌담 토막은 그 토막만 치운다(길이 지나가는 문 자리). 집·가게·정자·절벽·버스 정류장 같은 큰 것은 그대로.
    ///   끊긴 길(RoadGaps, 도로 깔기로 잇는 곳) 규칙은 그대로 — 열림/잠김은 VillageRoad 격자가 정하고, 여기선 걷는 모양만 정한다.
    public static class VillageRoadNet
    {
        static List<Vector2[]> _curves;
        /// 길 가운데선(돌길 메시와 같은 곡선)
        public static List<Vector2[]> Curves()
        {
            if (_curves != null) return _curves;
            _curves = new List<Vector2[]>();
            var lines = new List<Vector2[]> { VillageWorld.Path, VillageWorld.RanchPath }; lines.AddRange(VillageWorld.Branches);
            foreach (var L in lines)
            {
                if (L == null || L.Length < 2) continue;
                var pts = new List<Vector2>();
                for (int i = 0; i < L.Length - 1; i++)
                {
                    Vector2 p0 = L[Mathf.Max(0, i - 1)], p1 = L[i], p2 = L[i + 1], p3 = L[Mathf.Min(L.Length - 1, i + 2)];
                    float seg = Vector2.Distance(p1, p2); int n = Mathf.Max(2, Mathf.CeilToInt(seg / 0.6f));
                    for (int k = 0; k < n; k++)
                    {
                        float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                        pts.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                    }
                }
                pts.Add(L[L.Length - 1]);
                _curves.Add(pts.ToArray());
            }
            return _curves;
        }

        /// 가운데선까지 거리(가장 가까운 점)
        public static float CenterDist(Vector2 p, out Vector2 closest)
        {
            float bd = float.MaxValue; closest = p;
            foreach (var C in Curves())
                for (int i = 0; i < C.Length - 1; i++)
                {
                    var a = C[i]; var ab = C[i + 1] - a; float l2 = ab.sqrMagnitude; if (l2 < 1e-6f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2); var q = a + ab * t; float d = (q - p).sqrMagnitude;
                    if (d < bd) { bd = d; closest = q; }
                }
            return Mathf.Sqrt(bd);
        }
        /// 사각형(XZ 경계)과 가운데선 사이 최소 거리
        public static float CenterDistRect(Bounds b, out Vector2 closestOnLine)
        {
            float bd = float.MaxValue; closestOnLine = new Vector2(b.center.x, b.center.z);
            float x0 = b.min.x, x1 = b.max.x, z0 = b.min.z, z1 = b.max.z;
            foreach (var C in Curves())
                for (int i = 0; i < C.Length; i++)
                {
                    var q = C[i];
                    float dx = Mathf.Max(x0 - q.x, 0f, q.x - x1), dz = Mathf.Max(z0 - q.y, 0f, q.y - z1); float d = dx * dx + dz * dz;
                    if (d < bd) { bd = d; closestOnLine = q; }
                }
            return Mathf.Sqrt(bd);
        }

        // ── 걷는 길(그래프) ──────────────────────────────────────────
        static List<Vector2> _n; static List<List<(int to, float w)>> _e;
        static void BuildGraph()
        {
            if (_n != null) return;
            _n = new List<Vector2>(); _e = new List<List<(int, float)>>();
            var firstLast = new List<(int a, int b)>(); var curveNodes = new List<List<int>>();
            foreach (var C in Curves())
            {
                var ids = new List<int>(); float acc = 0f;
                for (int i = 0; i < C.Length; i++)
                {
                    if (i > 0) acc += Vector2.Distance(C[i], C[i - 1]);
                    if (i == 0 || i == C.Length - 1 || acc >= 1.5f) { ids.Add(Add(C[i])); acc = 0f; }
                }
                for (int k = 0; k < ids.Count - 1; k++) Link(ids[k], ids[k + 1]);
                curveNodes.Add(ids); firstLast.Add((ids[0], ids[ids.Count - 1]));
            }
            // 이음: 곡선 끝점 → 다른 곡선의 가장 가까운 점(2.5 m 안)
            for (int c = 0; c < curveNodes.Count; c++)
                foreach (int end in new[] { firstLast[c].a, firstLast[c].b })
                {
                    int best = -1; float bd = 2.5f;
                    for (int o = 0; o < curveNodes.Count; o++) { if (o == c) continue; foreach (int id in curveNodes[o]) { float d = Vector2.Distance(_n[end], _n[id]); if (d < bd) { bd = d; best = id; } } }
                    if (best >= 0) Link(end, best);
                }
        }
        static int Add(Vector2 p) { _n.Add(p); _e.Add(new List<(int, float)>()); return _n.Count - 1; }
        static void Link(int a, int b) { if (a == b) return; float w = Vector2.Distance(_n[a], _n[b]); _e[a].Add((b, w)); _e[b].Add((a, w)); }

        /// 가장 가까운 길 선분 위 점(snapM 안). 없으면 false
        static bool Project(Vector2 p, float snapM, out Vector2 q, out int a, out int b)
        {
            BuildGraph(); q = p; a = b = -1; float bd = snapM * snapM;
            for (int i = 0; i < _n.Count; i++)
                foreach (var (to, _) in _e[i])
                {
                    if (to < i) continue;
                    var A = _n[i]; var ab = _n[to] - A; float l2 = ab.sqrMagnitude; if (l2 < 1e-6f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(p - A, ab) / l2); var c = A + ab * t; float d = (c - p).sqrMagnitude;
                    if (d < bd) { bd = d; q = c; a = i; b = to; }
                }
            return a >= 0;
        }

        /// 길 가운데선을 따라가는 경유점(마지막은 목적지). 못 찾으면 null
        public static List<Vector3> Route(Vector3 from, Vector3 to, float snapM = 7f)
        {
            var f2 = new Vector2(from.x, from.z); var t2 = new Vector2(to.x, to.z);
            if (!Project(f2, snapM, out var fq, out int fa, out int fb)) return null;
            if (!Project(t2, snapM, out var tq, out int ta, out int tb)) return null;
            var pts = new List<Vector3>();
            bool sameEdge = (fa == ta && fb == tb) || (fa == tb && fb == ta);
            if (!sameEdge)
            {
                // 다익스트라(출발 선분 두 끝 → 도착 선분 두 끝)
                int N = _n.Count; var dist = new float[N]; var prev = new int[N]; var done = new bool[N];
                for (int i = 0; i < N; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
                dist[fa] = Vector2.Distance(fq, _n[fa]); dist[fb] = Mathf.Min(dist[fb], Vector2.Distance(fq, _n[fb]));
                var open = new List<int> { fa, fb };
                while (open.Count > 0)
                {
                    int bi = 0; for (int k = 1; k < open.Count; k++) if (dist[open[k]] < dist[open[bi]]) bi = k;
                    int u = open[bi]; open.RemoveAt(bi); if (done[u]) continue; done[u] = true;
                    if (u == ta && done[tb] || u == tb && done[ta]) break;
                    foreach (var (v, w) in _e[u]) { if (done[v]) continue; float nd = dist[u] + w; if (nd < dist[v]) { dist[v] = nd; prev[v] = u; open.Add(v); } }
                }
                float ca = dist[ta] + Vector2.Distance(_n[ta], tq), cb = dist[tb] + Vector2.Distance(_n[tb], tq);
                int end = ca <= cb ? ta : tb; if (dist[end] == float.MaxValue) return null;
                var chain = new List<int>(); for (int k = end; k >= 0; k = prev[k]) chain.Add(k); chain.Reverse();
                if (Vector2.Distance(f2, fq) > 0.8f) pts.Add(G(fq));
                foreach (int k in chain) pts.Add(G(_n[k]));
            }
            else if (Vector2.Distance(f2, fq) > 0.8f) pts.Add(G(fq));
            pts.Add(G(tq)); pts.Add(to);
            // 이미 지나친 첫 점(내 바로 뒤)은 뺀다
            while (pts.Count > 2 && Vector2.Distance(f2, new Vector2(pts[1].x, pts[1].z)) < Vector2.Distance(new Vector2(pts[0].x, pts[0].z), new Vector2(pts[1].x, pts[1].z))) pts.RemoveAt(0);
            return pts;
        }
        static Vector3 G(Vector2 p) => VillageWorld.Ground(p.x, p.y);

        // ── 길 위 소품 비키기 ────────────────────────────────────────
        public static int Moved, Removed;
        public static readonly List<string> Report = new List<string>();
        static readonly string[] Keep = { "House", "Shop", "Jeongja", "Cliff", "BusStop", "StopBack", "Tower", "Barn", "Hospital", "Bound", "Mine", "Lighthouse", "Terrain", "Sea", "Bank", "Cafe", "Hotel", "Waterfall", "Bridge", "Gate", "Wall_", "EastTown", "Marina" };
        static bool IsKeep(string n) { foreach (var k in Keep) if (n.Contains(k)) return true; return false; }
        static bool IsThin(string n) => n.Contains("Fence") || n.Contains("StoneWall") || n.Contains("Wall");
        static readonly HashSet<string> Containers = new HashSet<string> { "VillageWorld", "VillageHub", "EastTown", "Ranch", "Garden", "Roads", "Flora", "VillageFlora", "Props", "Season", "Village" };

        static Transform PropRoot(Transform t, Transform stop)
        {
            var r = t;
            while (r.parent != null && r.parent != stop && !Containers.Contains(r.parent.name))
            {
                var p = r.parent; if (p.childCount > 12) break;
                var rs = p.GetComponentsInChildren<Renderer>(); Bounds b = default; bool any = false;
                foreach (var x in rs) { if (!any) { b = x.bounds; any = true; } else b.Encapsulate(x.bounds); }
                if (any && (b.size.x > 5f || b.size.z > 5f)) break;
                r = p;
            }
            return r;
        }

        /// 마을이 다 지어진 뒤 한 번(VillageHub). 길 위 소품을 옮기거나 치운다.
        public static void ClearProps(Transform player)
        {
            Moved = 0; Removed = 0; Report.Clear(); _yards = null;
            var done = new HashSet<Transform>();
            var buf = new Collider[16];
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c == null || c.isTrigger || !c.enabled || c is TerrainCollider) continue;
                if (player != null && c.transform.IsChildOf(player)) continue;
                if (c.GetComponentInParent<VillageCreatures>() != null) continue;
                var b = c.bounds; if (b.center.x < -60f || b.center.x > 90f || b.center.z < -80f || b.center.z > 70f) continue;   // 마을 밖(시내·관광지 구역)은 제외
                string n = c.name; string pn = c.transform.parent != null ? c.transform.parent.name : "";
                if (IsKeep(n) || IsKeep(pn) || n.StartsWith("Ground") || n.Contains("Road") || n.Contains("Path") || n.Contains("Floor")) continue;
                if (n.Contains("Garden") || pn.Contains("Garden")) continue;   // 텃밭 울타리는 그대로(길은 옆으로 지나간다)
                bool thin = IsThin(n) || IsThin(pn);
                if (!thin && (b.size.x > 4.5f || b.size.z > 4.5f)) continue;
                float d = CenterDistRect(b, out var cl);
                if (thin)
                {
                    if (d > 0.5f) continue;   // 가운데선이 실제로 지나가는 토막만(옆으로 스치는 담은 그대로)
                    // 길을 가로지르는 울타리·돌담 토막 — 그 토막만 치운다(길이 지나가는 자리)
                    var root = c.transform; if (done.Contains(root)) continue; done.Add(root);
                    root.gameObject.SetActive(false); Removed++; Report.Add($"치움 {n} ({b.center.x:0.0},{b.center.z:0.0})");
                    continue;
                }
                if (d >= 1.3f) continue;
                var pr = PropRoot(c.transform, null); if (done.Contains(pr)) continue; done.Add(pr);
                if (TryPush(pr, c, cl, buf)) { Moved++; Report.Add($"옮김 {pr.name} ({b.center.x:0.0},{b.center.z:0.0}) → ({pr.position.x:0.0},{pr.position.z:0.0})"); }
                else { pr.gameObject.SetActive(false); Removed++; Report.Add($"치움 {pr.name} ({b.center.x:0.0},{b.center.z:0.0}) — 옮길 빈자리 없음"); }
            }
            // 콜라이더 없는 야자수(관광단지·해변)도 길 한가운데면 비킨다
            for (int i = VillageHouses.Palms.Count - 1; i >= 0; i--)
            {
                var t = VillageHouses.Palms[i]; if (t == null || done.Contains(t)) continue;
                float d = CenterDist(new Vector2(t.position.x, t.position.z), out var cl); if (d >= 1.2f) continue;
                done.Add(t); var dir = new Vector2(t.position.x, t.position.z) - cl; if (dir.sqrMagnitude < 1e-4f) dir = Vector2.right; dir.Normalize();
                var np = cl + dir * 2.2f; MoveWithShadow(t, new Vector3(np.x, VillageWorld.Height(np.x, np.y), np.y) - t.position); Moved++; Report.Add($"옮김 야자수 → ({np.x:0.0},{np.y:0.0})");
            }
        }

        /// 소품과 그 발밑 접지 그림자(ContactShadow — 따로 놓인 반투명 원판)를 같이 옮긴다
        static void MoveWithShadow(Transform root, Vector3 delta)
        {
            var old = root.position;
            if (root.parent != null)
                for (int i = 0; i < root.parent.childCount; i++)
                {
                    var ch = root.parent.GetChild(i); if (ch == root || ch.name != "ContactShadow") continue;
                    var d = ch.position - old; d.y = 0f; if (d.magnitude > 0.8f) continue;
                    var np = ch.position + delta; ch.position = new Vector3(np.x, VillageWorld.Height(np.x, np.z) + 0.02f, np.z);
                }
            root.position += delta;
        }

        static List<Bounds> _yards;
        static bool InYard(Bounds nb)
        {
            if (_yards == null)
            {
                _yards = new List<Bounds>();
                foreach (var name in new[] { "Garden", "Farm", "Ranch", "Orchard", "Hives", "Barn" })
                {
                    var go = GameObject.Find(name); if (go == null) continue; bool any = false; Bounds b = default;
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                    if (any && b.size.x < 40f && b.size.z < 40f) _yards.Add(b);
                }
            }
            foreach (var y in _yards) { var a = y; a.Expand(new Vector3(0.3f, 100f, 0.3f)); if (a.Intersects(nb)) return true; }
            return false;
        }

        static bool TryPush(Transform root, Collider c, Vector2 closest, Collider[] buf)
        {
            var b = c.bounds; var ctr = new Vector2(b.center.x, b.center.z);
            var dir = ctr - closest; if (dir.sqrMagnitude < 1e-4f) { CenterDist(ctr, out var q); dir = ctr - q; if (dir.sqrMagnitude < 1e-4f) dir = Vector2.right; }
            dir.Normalize();
            float half = Mathf.Max(b.extents.x, b.extents.z);
            foreach (float sgn in new[] { 1f, -1f })
            {
                var target = closest + dir * sgn * (1.9f + half);
                var delta = target - ctr;
                var nb = new Bounds(b.center + new Vector3(delta.x, 0f, delta.y), b.size);
                if (CenterDistRect(nb, out var _) < 1.5f) continue;
                if (VillageWorld.Height(target.x, target.y) < VillageWorld.SeaLevel + 0.2f) continue;
                if (InYard(nb)) continue;   // 텃밭·농장 울타리 안으로는 옮기지 않는다
                int n = Physics.OverlapBoxNonAlloc(nb.center, nb.extents * 0.9f, buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < n; i++) { var o = buf[i]; if (o == null || o.transform.IsChildOf(root) || o is TerrainCollider || o.name == "Terrain" || o.name == "Sea" || o.name.StartsWith("Ground")) continue; blocked = true; break; }
                if (blocked) continue;
                float dy = VillageWorld.Height(target.x, target.y) - VillageWorld.Height(ctr.x, ctr.y);
                MoveWithShadow(root, new Vector3(delta.x, dy, delta.y));
                return true;
            }
            return false;
        }
    }
}
