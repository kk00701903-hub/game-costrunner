using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 199차(사용자: 「광산 만들어서 던전처럼 만들어 주고, 광산에서 보스도 만들어줘 — 보스는 kpop 러닝에 나오는 보스들로」):
    /// 오름 광산 안쪽 「깊은 갱도」 — B1~B4 는 방 9칸(3×3)이 굴로 이어진 미로(몬스터·광맥·내려가는 계단), B5 는 보스 방.
    /// 몬스터: 동굴 박쥐(날아서 빠름)·돌 슬라임(통통 튐)·B3 부터 가시 두더지(단단함). 방망이·곡괭이·도끼로 친다(잠자리채는 안 됨).
    /// 보스: K-POP 러닝의 갈매기 해적·돌하르방 골렘·태풍 도깨비(주마다 바뀜) — BossModel3D 입체 모형이 주인공 둘레를 돌며 다가왔다 물러났다 한다.
    ///   갈매기: 날아올라 주인공 쪽으로 미사일 3발 · 골렘: 주인공 발밑에 바위 4개(빨간 원 1초 전 예고) · 도깨비: 쫓아오는 회오리 2개.
    ///   공격이 끝나면 3초 어지러움(별) — 이때 맞히면 두 배. 이긴 주 첫 번엔 큰 보상, 이후엔 작은 보상.
    /// 월드 위치: 광산(−330,·,330) 과 떨어진 (−330,·,560). VillageZones.At 이 여기를 광산 구역으로 본다(바닥 높이 0 · 벌레·지도 없음).
    public class VillageDungeon : MonoBehaviour
    {
        public static readonly Vector3 C = new Vector3(-330f, 0f, 560f);
        public const float HX = 25f, HZ = 25f;
        public const int BossFloor = 5;
        public static bool Contains(Vector3 p) => Mathf.Abs(p.x - C.x) < HX + 4f && Mathf.Abs(p.z - C.z) < HZ + 4f;

        public Transform Player;
        public Func<bool> Locked;
        public Action<int, string> OnHurt;        // (피해, 누구에게)
        public Action<string> OnToast;
        public Action<int> OnBossDown;            // 보스 종류
        public int Floor { get; private set; }
        public bool Active => _root != null;
        public Vector3 StartPos { get; private set; }
        public Vector3 StairsDown { get; private set; }
        public Vector3 StairsUp { get; private set; }
        public bool BossAlive => _boss != null && _boss.hp > 0;
        public int BossKind => _boss != null ? _boss.kind : -1;

        Transform _root; System.Random _rng;
        const int N = 25; const float Cell = 2f;   // 2 m 칸 25×25 = 50 m
        bool[,] _open;

        class Mob { public Vector3 kb; public float v; public Transform t, vis; public int kind, hp, maxHp; public float hitCd, stun, phase, speed; public Vector3 home, wander; }
        readonly List<Mob> _mobs = new List<Mob>();
        public struct Ore { public Transform t; public int kind; }
        public readonly List<Ore> Ores = new List<Ore>();

        class Boss { public Vector3 kb; public Transform t; public BossModel3D model; public int kind, hp, maxHp; public float stateT, ang, radius, radiusTarget, dizzy, hitCd, hover; public int state; public Vector3 prev; public Transform stars; }
        Boss _boss;
        class Shot { public Transform t; public Vector3 vel; public float life; public int kind; public float r; public int dmg; public float hitCd; }   // kind 0 미사일 1 바위(떨어짐) 2 회오리
        readonly List<Shot> _shots = new List<Shot>();
        GameObject _bar; Image _barFill; Text _barName;

        // ── 재질 ──
        static Material _rock, _rockD, _dirt, _wood, _dark;
        static Material Lit(Color c, float s = 0.06f) => CoastMaterials.CreateLit(c, s);
        static void Mats()
        {
            if (_rock != null) return;
            _rock = Lit(new Color(0.33f, 0.29f, 0.27f)); _rockD = Lit(new Color(0.24f, 0.21f, 0.20f)); _dirt = Lit(new Color(0.36f, 0.29f, 0.23f));
            _wood = Lit(new Color(0.50f, 0.34f, 0.20f)); _dark = CoastMaterials.CreateUnlit(new Color(0.03f, 0.02f, 0.03f));
        }
        static GameObject Prim(Transform p, PrimitiveType t, Vector3 pos, Vector3 scl, Material m, bool col = false, float yaw = 0f)
        {
            var g = GameObject.CreatePrimitive(t); if (!col) Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(p, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0f, yaw, 0f); g.transform.localScale = scl;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }
        Vector3 W(int i, int j) => new Vector3(C.x - HX + (i + 0.5f) * Cell, 0f, C.z - HZ + (j + 0.5f) * Cell);

        // ── 층 만들기 ─────────────────────────────────────────────────────
        public void Clear()
        {
            if (_root != null) Destroy(_root.gameObject); _root = null;
            _mobs.Clear(); Ores.Clear(); _shots.Clear(); _boss = null; Floor = 0;
            if (_bar != null) Destroy(_bar); _bar = null;
        }

        public void Build(int floor, int seed)
        {
            Clear(); Mats(); Floor = floor; _rng = new System.Random(seed * 7919 + floor * 131);
            _root = new GameObject("Dungeon_B" + floor).transform; _root.SetParent(transform, false);
            _open = new bool[N, N];
            var floorMat = floor >= BossFloor ? Lit(new Color(0.30f, 0.22f, 0.24f)) : _dirt;
            // 206차(사용자: 「동굴 등 모자란 부분은 파이어플라이·블렌더로」): 바닥 = Firefly 손그림 흙바닥 타일(4 m 반복), 보스 방은 붉게 물들임
            var ftex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_CaveFloor");
            if (ftex != null) { floorMat = CoastMaterials.CreateToon(floor >= BossFloor ? new Color(0.58f, 0.48f, 0.56f) : new Color(0.78f, 0.78f, 0.88f)   /* 209차: 쨍한 보정 뒤 주황으로 타서 푸른 기 틴트 */, ftex, 0.04f); floorMat.mainTextureScale = new Vector2((HX * 2f + 6f) / 7f, (HZ * 2f + 6f) / 7f); if (floorMat.HasProperty("_BaseMap")) floorMat.SetTextureScale("_BaseMap", floorMat.mainTextureScale); }
            Prim(_root, PrimitiveType.Cube, C + new Vector3(0f, -0.06f, 0f), new Vector3(HX * 2f + 6f, 0.1f, HZ * 2f + 6f), floorMat);
            if (floor >= BossFloor) BuildArena(); else BuildMaze();
            // 벽: 열린 칸 둘레의 막힌 칸마다 울퉁불퉁한 바위
            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
            {
                if (_open[i, j]) continue; bool edge = false;
                for (int di = -1; di <= 1 && !edge; di++) for (int dj = -1; dj <= 1 && !edge; dj++) { int a = i + di, b = j + dj; if (a >= 0 && b >= 0 && a < N && b < N && _open[a, b]) edge = true; }
                if (!edge) continue;
                float h = 1.5f + (float)_rng.NextDouble() * 0.9f;   // 낮은 바위벽 — 위에서 미로가 읽히게
                var wc = Prim(_root, PrimitiveType.Cube, W(i, j) + Vector3.up * (h * 0.5f), new Vector3(Cell, h, Cell), (i + j) % 3 == 0 ? _rockD : _rock, true, (float)_rng.NextDouble() * 8f - 4f);
                // 206차: 블렌더 동굴 바위(VCaveWall_A/B/C)로 겉모습만 바꾼다(충돌은 상자 그대로)
                var wm = JejuKit.Spawn("VCaveWall_" + "ABC"[_rng.Next(3)], _root, Vector3.zero, _rng.Next(360));
                if (wm != null)
                {
                    wc.GetComponent<MeshRenderer>().enabled = false; wm.transform.position = W(i, j) + Vector3.down * 0.15f;
                    foreach (var r in wm.GetComponentsInChildren<MeshRenderer>()) { var ms = r.sharedMaterials; for (int mi = 0; mi < ms.Length; mi++) if (ms[mi] != null && ms[mi].name.StartsWith("Moss")) ms[mi] = ms[0]; r.sharedMaterials = ms; }   // 땅속엔 이끼 없이 바위 한 가지 색
                    float sxz = Cell * 0.62f * (0.95f + (float)_rng.NextDouble() * 0.2f); wm.transform.localScale = new Vector3(sxz, h * 1.12f, sxz);
                }
                else if (_rng.NextDouble() < 0.45) Prim(_root, PrimitiveType.Sphere, W(i, j) + new Vector3(0f, h, 0f), new Vector3(Cell * 1.1f, 0.9f, Cell * 1.1f), _rockD);
            }
            // 바깥 막이(혹시 모를 틈)
            var b0 = new GameObject("Bounds").transform; b0.SetParent(_root, false);
            void Wl(Vector3 c, Vector3 s) { var w = new GameObject("B", typeof(BoxCollider)); w.transform.SetParent(b0, false); w.transform.position = c; w.GetComponent<BoxCollider>().size = s; }
            Wl(C + new Vector3(0f, 3f, HZ + 0.5f), new Vector3(HX * 2f + 2f, 8f, 1f)); Wl(C + new Vector3(0f, 3f, -HZ - 0.5f), new Vector3(HX * 2f + 2f, 8f, 1f));
            Wl(C + new Vector3(HX + 0.5f, 3f, 0f), new Vector3(1f, 8f, HZ * 2f + 2f)); Wl(C + new Vector3(-HX - 0.5f, 3f, 0f), new Vector3(1f, 8f, HZ * 2f + 2f));
            // 땅속 어둠: 둘레에 높은 검은 벽(하늘이 안 보이게)
            var voidM = CoastMaterials.CreateUnlit(new Color(0.05f, 0.04f, 0.05f));
            Prim(_root, PrimitiveType.Cube, C + new Vector3(0f, 14f, HZ + 3f), new Vector3(HX * 2f + 12f, 32f, 2f), voidM); Prim(_root, PrimitiveType.Cube, C + new Vector3(0f, 14f, -HZ - 3f), new Vector3(HX * 2f + 12f, 32f, 2f), voidM);
            Prim(_root, PrimitiveType.Cube, C + new Vector3(HX + 3f, 14f, 0f), new Vector3(2f, 32f, HZ * 2f + 12f), voidM); Prim(_root, PrimitiveType.Cube, C + new Vector3(-HX - 3f, 14f, 0f), new Vector3(2f, 32f, HZ * 2f + 12f), voidM);
            // 올라가는 사다리
            // 사다리는 시작 자리 오른쪽(동쪽) 벽에 — 카메라(남쪽)와 주인공 사이를 막지 않게
            for (int s2 = -1; s2 <= 1; s2 += 2) Prim(_root, PrimitiveType.Cube, StairsUp + new Vector3(0.9f, 1.4f, s2 * 0.5f), new Vector3(0.12f, 2.8f, 0.12f), _wood);
            for (int k = 0; k < 5; k++) Prim(_root, PrimitiveType.Cube, StairsUp + new Vector3(0.9f, 0.4f + k * 0.5f, 0f), new Vector3(0.1f, 0.08f, 1.0f), _wood);
            VillageZones.Sign(_root, StairsUp + new Vector3(0.95f, 3.1f, 0f), -90f, floor == 1 ? "⬆ 광산으로" : $"⬆ B{floor - 1} 로", floor == 1 ? "⬆ To the mine" : $"⬆ To B{floor - 1}", new Color(0.45f, 0.30f, 0.18f), 2.4f, 0.55f);
            if (floor < BossFloor)
            {
                Prim(_root, PrimitiveType.Cylinder, StairsDown + new Vector3(0f, 0.02f, 0f), new Vector3(2.4f, 0.02f, 2.4f), _dark);
                for (int k = 0; k < 6; k++) { float a = k / 6f * Mathf.PI * 2f; Prim(_root, PrimitiveType.Sphere, StairsDown + new Vector3(Mathf.Cos(a) * 1.45f, 0.15f, Mathf.Sin(a) * 1.45f), new Vector3(0.6f, 0.35f, 0.6f), _rock); }
                VillageZones.Sign(_root, StairsDown + new Vector3(0f, 2.2f, 1.6f), 180f, floor + 1 >= BossFloor ? $"⬇ B{floor + 1} · 보스" : $"⬇ B{floor + 1}", floor + 1 >= BossFloor ? $"⬇ B{floor + 1} · Boss" : $"⬇ B{floor + 1}", floor + 1 >= BossFloor ? new Color(0.65f, 0.20f, 0.22f) : new Color(0.45f, 0.30f, 0.18f), 2.0f, 0.55f);
            }
            else StairsDown = new Vector3(0f, -999f, 0f);
        }

        void BuildMaze()
        {
            // 3×3 방(방 칸 8×8 = 16 m 칸 안에 6~7칸짜리 방) + 무작위 신장 트리로 굴 연결 + 고리 1~2개
            int[] rx = new int[9], rz = new int[9], rw = new int[9], rh = new int[9];
            for (int a = 0; a < 3; a++) for (int b = 0; b < 3; b++)
            {
                int k = a + b * 3; rw[k] = 4 + _rng.Next(3); rh[k] = 4 + _rng.Next(3);
                int cx = a * 8 + 4, cz = b * 8 + 4; rx[k] = cx - rw[k] / 2; rz[k] = cz - rh[k] / 2;
                for (int i = rx[k]; i < rx[k] + rw[k]; i++) for (int j = rz[k]; j < rz[k] + rh[k]; j++) Open(i, j);
            }
            var inTree = new bool[9]; var edges = new List<(int, int)>(); inTree[1] = true;   // 1 = 남쪽 가운데(시작 방)
            var frontier = new List<(int, int)>();
            void AddF(int k) { int a = k % 3, b = k / 3; if (a > 0) frontier.Add((k, k - 1)); if (a < 2) frontier.Add((k, k + 1)); if (b > 0) frontier.Add((k, k - 3)); if (b < 2) frontier.Add((k, k + 3)); }
            AddF(1);
            while (frontier.Count > 0)
            {
                int pick = _rng.Next(frontier.Count); var e = frontier[pick]; frontier.RemoveAt(pick);
                if (inTree[e.Item2]) continue; inTree[e.Item2] = true; edges.Add(e); AddF(e.Item2);
            }
            for (int x = 0; x < 2; x++) { int a = _rng.Next(9); int b = a % 3 < 2 && _rng.Next(2) == 0 ? a + 1 : a + 3; if (b < 9 && (b == a + 1 ? a % 3 < 2 : true)) edges.Add((a, b)); }
            foreach (var e in edges)
            {
                int ax = e.Item1 % 3 * 8 + 4, az = e.Item1 / 3 * 8 + 4, bx = e.Item2 % 3 * 8 + 4, bz = e.Item2 / 3 * 8 + 4;
                for (int i = Mathf.Min(ax, bx); i <= Mathf.Max(ax, bx); i++) { Open(i, az); Open(i, az + 1); }
                for (int j = Mathf.Min(az, bz); j <= Mathf.Max(az, bz); j++) { Open(bx, j); Open(bx + 1, j); }
            }
            // 가장 먼 방(너비 우선) = 내려가는 계단
            var dist = new int[9]; for (int k = 0; k < 9; k++) dist[k] = -1; dist[1] = 0; var q = new Queue<int>(); q.Enqueue(1);
            while (q.Count > 0) { int k = q.Dequeue(); foreach (var e in edges) { int o = e.Item1 == k ? e.Item2 : e.Item2 == k ? e.Item1 : -1; if (o >= 0 && dist[o] < 0) { dist[o] = dist[k] + 1; q.Enqueue(o); } } }
            int far = 1; for (int k = 0; k < 9; k++) if (dist[k] > dist[far]) far = k;
            Vector3 RoomC(int k) => (W(rx[k], rz[k]) + W(rx[k] + rw[k] - 1, rz[k] + rh[k] - 1)) * 0.5f;
            StartPos = RoomC(1) + new Vector3(-0.6f, 0f, -0.6f); StairsUp = StartPos + new Vector3(2.4f, 0f, 0f); StairsDown = RoomC(far);
            // 등불(방마다)
            for (int k = 0; k < 9; k++) Lamp(RoomC(k) + new Vector3(rw[k] * 0.9f - 1.2f, 0f, rh[k] * 0.9f - 1.2f), k == far ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 0.78f, 0.45f));
            // 206차: 장식 — 방마다 석순 1~2, 방 사이 버팀목, 시작 방 광차
            for (int k = 0; k < 9; k++)
            {
                if (k == far) continue;
                int n = 1 + _rng.Next(2);
                for (int d = 0; d < n; d++)
                {
                    var sp = W(rx[k] + (_rng.Next(2) == 0 ? 0 : rw[k] - 1), rz[k] + (_rng.Next(2) == 0 ? 0 : rh[k] - 1)) + new Vector3((float)_rng.NextDouble() * 0.6f - 0.3f, 0f, (float)_rng.NextDouble() * 0.6f - 0.3f);
                    if (k == 1 && Vector3.Distance(sp, StartPos) < 3f) continue;
                    var st = JejuKit.Spawn(_rng.Next(2) == 0 ? "VCaveStalag_A" : "VCaveStalag_B", _root, Vector3.zero, _rng.Next(360), 0.8f + (float)_rng.NextDouble() * 0.5f); if (st != null) st.transform.position = sp;
                }
            }
            foreach (var e in edges)
            {
                int ax = e.Item1 % 3 * 8 + 4, az = e.Item1 / 3 * 8 + 4, bx = e.Item2 % 3 * 8 + 4, bz = e.Item2 / 3 * 8 + 4;
                var mid = (W(ax, az) + W(bx, bz)) * 0.5f + new Vector3(Cell * 0.5f, 0f, Cell * 0.5f);
                var bm = JejuKit.Spawn("VMineBeam", _root, Vector3.zero, ax == bx ? 0f : 90f, 1.0f); if (bm != null) { bm.transform.position = mid; bm.transform.localScale = new Vector3(1.95f, 1f, 1f); }   // 굴 폭 4 m — 기둥이 양 벽에 붙게
            }
            var cart = JejuKit.Spawn("VMineCart", _root, Vector3.zero, 90f, 1.0f); if (cart != null) cart.transform.position = StartPos + new Vector3(-2.2f, 0f, 1.4f);
            // 광맥 2 + 층 수
            int ores = 2 + Floor; var rooms = new List<int>(); for (int k = 0; k < 9; k++) if (k != 1) rooms.Add(k);
            for (int o = 0; o < ores; o++)
            {
                int k = rooms[_rng.Next(rooms.Count)]; var p = W(rx[k] + _rng.Next(rw[k]), rz[k] + (_rng.Next(2) == 0 ? 0 : rh[k] - 1));
                if (Vector3.Distance(p, StairsDown) < 2.6f) continue;
                int kind = OreKind(); var node = new GameObject("DunOre").transform; node.SetParent(_root, false); node.position = p;
                var body = Prim(node, PrimitiveType.Sphere, p + Vector3.up * 0.55f, new Vector3(1.5f, 1.1f, 1.3f), _rock, true);
                var mat = kind == 4 ? Lit(VillageZones.NodeColor(kind)) : CoastMaterials.CreateUnlit(VillageZones.NodeColor(kind));
                // 206차: 블렌더 광맥(VCaveCrystal_A/B) — 결정 색 = 광석 색
                var cm = JejuKit.Spawn(_rng.Next(2) == 0 ? "VCaveCrystal_A" : "VCaveCrystal_B", node, Vector3.zero, _rng.Next(360));
                if (cm != null)
                {
                    body.GetComponent<MeshRenderer>().enabled = false; cm.transform.position = p; cm.transform.localScale = Vector3.one * 1.25f;
                    foreach (var r in cm.GetComponentsInChildren<MeshRenderer>()) { var ms = r.sharedMaterials; for (int mi = 0; mi < ms.Length; mi++) if (ms[mi] != null && ms[mi].name.StartsWith("CaveCrystal")) ms[mi] = mat; r.sharedMaterials = ms; }
                    Ores.Add(new Ore { t = node, kind = kind }); continue;
                }
                for (int c = 0; c < 4; c++) { var cr = Prim(node, PrimitiveType.Cube, p + new Vector3((c % 2 - 0.5f) * 0.6f, 0.95f + (c / 2) * 0.22f, (c / 2 - 0.5f) * 0.5f), new Vector3(0.2f, 0.46f, 0.2f), mat); cr.transform.rotation = Quaternion.Euler(20f + c * 13f, c * 40f, 30f); }
                Ores.Add(new Ore { t = node, kind = kind });
            }
            // 몬스터: 시작 방 말고, 층마다 하나씩 더
            int mobs = 3 + Floor;
            for (int m = 0; m < mobs; m++)
            {
                int k = rooms[_rng.Next(rooms.Count)]; var p = W(rx[k] + _rng.Next(rw[k]), rz[k] + _rng.Next(rh[k]));
                int kind = Floor >= 3 && _rng.Next(100) < 30 ? 2 : _rng.Next(2);
                SpawnMob(kind, p);
            }
        }
        void Open(int i, int j) { if (i >= 1 && j >= 1 && i < N - 1 && j < N - 1) _open[i, j] = true; }
        int OreKind() { int r = _rng.Next(100) + Floor * 6; return r < 40 ? 0 : r < 66 ? 1 : r < 84 ? 2 : r < 96 ? 3 : 4; }
        void Lamp(Vector3 at, Color c)
        {
            // 206차: 블렌더 갱도 등불(VMineLamp) — 없으면 옛 기둥+구
            var lm = JejuKit.Spawn("VMineLamp", _root, Vector3.zero, _rng != null ? _rng.Next(360) : 0f);
            if (lm != null) lm.transform.position = at;
            else
            {
                Prim(_root, PrimitiveType.Cube, at + new Vector3(0f, 1.1f, 0f), new Vector3(0.14f, 2.2f, 0.14f), _wood);
                Prim(_root, PrimitiveType.Sphere, at + new Vector3(0f, 2.3f, 0f), Vector3.one * 0.32f, CoastMaterials.CreateUnlit(c));
            }
            var l = new GameObject("DunLamp").AddComponent<Light>(); l.transform.SetParent(_root, false); l.transform.position = at + new Vector3(0f, 2.6f, 0f);
            l.type = LightType.Point; l.color = c; l.range = 11f; l.intensity = 1.6f; l.shadows = LightShadows.None;
        }

        void BuildArena()
        {
            for (int i = 3; i < N - 3; i++) for (int j = 3; j < N - 3; j++) { float dx = i - 12, dz = j - 12; if (dx * dx + dz * dz < 92f) Open(i, j); }
            for (int j = 1; j < 4; j++) { Open(11, j); Open(12, j); Open(13, j); }
            StartPos = W(12, 4); StairsUp = StartPos + new Vector3(2.4f, 0f, 0f);
            for (int k = 0; k < 6; k++) { float a = k / 6f * Mathf.PI * 2f; Lamp(C + new Vector3(Mathf.Cos(a) * 15.5f, 0f, Mathf.Sin(a) * 15.5f), new Color(1f, 0.55f, 0.5f)); }
            // 바닥 무늬(링) — 보스 방 느낌
            // 206차: 바닥이 Firefly 흙 타일(붉게 물들임)이 되어 큰 단색 원판은 뺀다 — 가장자리 석순 링으로 보스 방 느낌
            if (Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_CaveFloor") == null)
            {
                Prim(_root, PrimitiveType.Cylinder, C + new Vector3(0f, 0.005f, 0f), new Vector3(20f, 0.004f, 20f), Lit(new Color(0.42f, 0.26f, 0.28f)));
                Prim(_root, PrimitiveType.Cylinder, C + new Vector3(0f, 0.01f, 0f), new Vector3(16f, 0.004f, 16f), floorRing());
            }
            for (int k = 0; k < 12; k++) { if (k == 8 || k == 9) continue;   // 남쪽 입구는 비움
                float a = (k + 0.5f) / 12f * Mathf.PI * 2f; var st = JejuKit.Spawn(k % 2 == 0 ? "VCaveStalag_A" : "VCaveStalag_B", _root, Vector3.zero, k * 37f, 1.4f); if (st != null) st.transform.position = C + new Vector3(Mathf.Cos(a) * 17.2f, 0f, Mathf.Sin(a) * 17.2f); }
        }
        static Material floorRing() => CoastMaterials.CreateLit(new Color(0.30f, 0.22f, 0.24f), 0.06f);

        public void SpawnBoss(int kind, int hp)
        {
            if (_root == null) return;
            var t = new GameObject("DunBoss").transform; t.SetParent(_root, false); t.position = C + new Vector3(0f, 0f, 6f);
            var model = BossModel3D.Build(t, kind, kind == 1 ? 3.6f : 3.3f, 0.035f); model.speedRef = 3.2f; model.flying = false;
            _boss = new Boss { t = t, model = model, kind = kind, hp = hp, maxHp = hp, state = 0, stateT = 3.5f, ang = 90f * Mathf.Deg2Rad, radius = 8f, radiusTarget = 8f, prev = t.position };
            var st = new GameObject("Stars").transform; st.SetParent(t, false); st.localPosition = new Vector3(0f, (kind == 1 ? 3.6f : 3.3f) + 0.3f, 0f);
            var sm = CoastMaterials.CreateUnlit(new Color(1f, 0.92f, 0.35f));
            for (int k = 0; k < 4; k++) { var s = Prim(st, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.22f, sm); s.transform.localPosition = new Vector3(Mathf.Cos(k * 1.57f) * 0.55f, 0f, Mathf.Sin(k * 1.57f) * 0.55f); }
            st.gameObject.SetActive(false); _boss.stars = st;
            BuildBar(BossModel3D.NameKo(kind));
        }

        void BuildBar(string name)
        {
            _bar = new GameObject("DunBossBar", typeof(Canvas), typeof(CanvasScaler)); var cv = _bar.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 40;
            var sc = _bar.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080f, 1920f); sc.matchWidthOrHeight = 0.5f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image)); bg.transform.SetParent(_bar.transform, false);
            var br = bg.GetComponent<RectTransform>(); br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f); br.pivot = new Vector2(0.5f, 1f); br.anchoredPosition = new Vector2(0f, -330f); br.sizeDelta = new Vector2(760f, 46f);
            bg.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.08f, 0.85f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(bg.transform, false);
            var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = new Vector2(0f, 0f); fr.anchorMax = new Vector2(1f, 1f); fr.offsetMin = new Vector2(5f, 5f); fr.offsetMax = new Vector2(-5f, -5f); fr.pivot = new Vector2(0f, 0.5f);
            _barFill = fill.GetComponent<Image>(); _barFill.color = new Color(0.95f, 0.32f, 0.38f);
            _barName = CoastHudLayout.MakeText(bg.transform, "Name", "👑 " + name, 34, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(0f, 52f));
            _barName.color = Color.white; _barName.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_barName, new Color(0.2f, 0.05f, 0.08f, 0.9f), 2.5f);
        }

        // ── 몬스터 ──
        void SpawnMob(int kind, Vector3 p)
        {
            var t = new GameObject(kind == 0 ? "CaveBat" : kind == 1 ? "RockSlime" : "SpikeMole").transform; t.SetParent(_root, false); t.position = p;
            var vis = new GameObject("Vis").transform; vis.SetParent(t, false);
            var black = CoastMaterials.CreateUnlit(new Color(0.06f, 0.05f, 0.07f)); var white = CoastMaterials.CreateUnlit(Color.white);
            if (kind == 0)
            {
                var body = Lit(new Color(0.42f, 0.30f, 0.55f), 0.3f); var wing = Lit(new Color(0.30f, 0.20f, 0.40f), 0.2f);
                vis.localPosition = new Vector3(0f, 1.4f, 0f);
                LocalPrim(vis, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.45f, 0.42f, 0.40f), body);
                for (int s = -1; s <= 1; s += 2)
                {
                    var wp = new GameObject(s < 0 ? "WingL" : "WingR").transform; wp.SetParent(vis, false); wp.localPosition = new Vector3(s * 0.2f, 0.05f, 0f);
                    LocalPrim(wp, PrimitiveType.Sphere, new Vector3(s * 0.28f, 0f, 0f), new Vector3(0.55f, 0.06f, 0.32f), wing);
                    LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.09f, 0.08f, 0.18f), Vector3.one * 0.09f, white);
                    LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.09f, 0.08f, 0.22f), Vector3.one * 0.05f, black);
                    LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.13f, 0.24f, 0f), new Vector3(0.1f, 0.16f, 0.06f), body);
                }
            }
            else if (kind == 1)
            {
                var body = CoastMaterials.CreateTransparent(new Color(0.55f, 0.62f, 0.66f, 0.85f)); var core = Lit(new Color(0.40f, 0.42f, 0.44f));
                LocalPrim(vis, PrimitiveType.Sphere, new Vector3(0f, 0.34f, 0f), new Vector3(0.8f, 0.62f, 0.8f), body);
                LocalPrim(vis, PrimitiveType.Sphere, new Vector3(0f, 0.30f, 0f), new Vector3(0.36f, 0.3f, 0.36f), core);
                for (int s = -1; s <= 1; s += 2) { LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.14f, 0.45f, 0.33f), Vector3.one * 0.11f, white); LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.14f, 0.45f, 0.38f), Vector3.one * 0.06f, black); }
            }
            else
            {
                var body = Lit(new Color(0.45f, 0.32f, 0.24f)); var spike = Lit(new Color(0.85f, 0.82f, 0.76f), 0.3f); var nose = Lit(new Color(0.95f, 0.55f, 0.6f));
                LocalPrim(vis, PrimitiveType.Sphere, new Vector3(0f, 0.38f, 0f), new Vector3(0.75f, 0.62f, 0.9f), body);
                LocalPrim(vis, PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0.46f), Vector3.one * 0.14f, nose);
                for (int k = 0; k < 7; k++) { var sp = LocalPrim(vis, PrimitiveType.Capsule, new Vector3((k % 3 - 1) * 0.2f, 0.66f, -0.2f + (k / 3) * 0.18f), new Vector3(0.08f, 0.14f, 0.08f), spike); sp.localRotation = Quaternion.Euler(-30f + (k / 3) * 20f, 0f, (k % 3 - 1) * 25f); }
                for (int s = -1; s <= 1; s += 2) LocalPrim(vis, PrimitiveType.Sphere, new Vector3(s * 0.15f, 0.48f, 0.38f), Vector3.one * 0.07f, black);
            }
            BuildingOutline.AttachThick(vis, 0.02f);
            int hp = kind == 0 ? 2 : kind == 1 ? 3 : 5;
            _mobs.Add(new Mob { t = t, vis = vis, kind = kind, hp = hp, maxHp = hp, home = p, wander = p, phase = (float)_rng.NextDouble() * 6f, speed = kind == 0 ? 3.4f : kind == 1 ? 2.2f : 1.7f });
        }
        static Transform LocalPrim(Transform p, PrimitiveType t, Vector3 lp, Vector3 scl, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(p, false); g.transform.localPosition = lp; g.transform.localScale = scl; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g.transform;
        }
        bool Walkable(Vector3 p)
        {
            int i = Mathf.FloorToInt((p.x - (C.x - HX)) / Cell), j = Mathf.FloorToInt((p.z - (C.z - HZ)) / Cell);
            return i >= 0 && j >= 0 && i < N && j < N && _open[i, j];
        }
        static readonly string[] MobKo = { "동굴 박쥐", "돌 슬라임", "가시 두더지" };

        // ── 매 프레임 ──
        void Update()
        {
            if (_root == null || Player == null) return;
            if (Locked != null && Locked()) return;
            float dt = Time.deltaTime; var pp = Player.position;
            for (int i = _mobs.Count - 1; i >= 0; i--)
            {
                var m = _mobs[i]; if (m.t == null) { _mobs.RemoveAt(i); continue; }
                m.phase += dt; m.hitCd -= dt; m.stun -= dt;
                var p = m.t.position; var d = pp - p; d.y = 0f; float dist = d.magnitude;
                Vector3 goal;
                if (dist < 7f) goal = pp; else { if (Vector3.Distance(p, m.wander) < 0.6f || m.phase % 4f < dt) m.wander = m.home + new Vector3((float)_rng.NextDouble() * 6f - 3f, 0f, (float)_rng.NextDouble() * 6f - 3f); goal = m.wander; }
                var dir = goal - p; dir.y = 0f;
                float sp = m.stun > 0f ? 0f : (dist < 7f ? m.speed : m.speed * 0.4f);
                if (m.kind == 1) sp *= Mathf.Clamp01(Mathf.Sin(m.phase * 5f) * 1.4f);   // 슬라임: 뛰었다 멈췄다
                // 209차(사용자: 「몬스터 움직임 디테일」): 속도는 가감속(딱 서고 딱 출발 X), 방향은 초당 최대 540° 로 스르륵, 맞으면 미끄러지듯 밀려남
                bool go = dir.sqrMagnitude > 0.04f && dist > 0.9f;
                m.v = Mathf.MoveTowards(m.v, go ? sp : 0f, dt * (m.kind == 1 ? 12f : 5f));
                if (m.v > 0.001f || m.kb.sqrMagnitude > 0.0004f)
                {
                    var mv = (go ? dir.normalized * m.v : m.t.forward * m.v) + m.kb; var np = p + mv * dt;
                    if (Walkable(np)) m.t.position = np; else m.kb = Vector3.zero;
                }
                m.kb *= Mathf.Exp(-dt * 7f);
                if (go) m.t.rotation = Quaternion.RotateTowards(m.t.rotation, Quaternion.Slerp(m.t.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 1f - Mathf.Exp(-dt * 8f)), 540f * dt);
                // 모양 움직임
                if (m.kind == 0) { m.vis.localPosition = new Vector3(0f, 1.4f + Mathf.Sin(m.phase * 3f) * 0.25f, 0f); for (int c = 0; c < m.vis.childCount; c++) { var ch = m.vis.GetChild(c); if (ch.name == "WingL") ch.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m.phase * 18f) * 40f); else if (ch.name == "WingR") ch.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(m.phase * 18f) * 40f); } }
                else if (m.kind == 1) { float s = Mathf.Sin(m.phase * 5f); m.vis.localScale = new Vector3(1f + s * 0.08f, 1f - s * 0.12f, 1f + s * 0.08f); m.vis.localPosition = new Vector3(0f, Mathf.Max(0f, s) * 0.25f, 0f); }
                else { m.vis.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m.phase * 8f) * 5f * Mathf.Clamp01(m.v / Mathf.Max(0.1f, m.speed))); }   // 209차: 흔들림도 속도에 비례(딱 멈춤 X)
                if (m.stun > 0f) m.vis.localPosition += new Vector3(Mathf.Sin(m.phase * 50f) * 0.04f, 0f, 0f);
                // 닿으면 아프다
                if (dist < 1.0f && m.hitCd <= 0f && m.stun <= 0f)
                {
                    m.hitCd = 1.3f; int dmg = (m.kind == 0 ? 2 : m.kind == 1 ? 3 : 5) + Floor / 2;
                    OnHurt?.Invoke(dmg, MobKo[m.kind]);
                    var push = (p - pp); push.y = 0f; if (push.sqrMagnitude > 0.01f) { m.kb = push.normalized * 8.4f; m.v = 0f; }   // 209차: 1.2 m 순간이동 → 0.15 s 에 걸쳐 밀려남
                }
            }
            if (_boss != null) TickBoss(dt, pp);
            TickShots(dt, pp);
        }

        // ── 보스 ──
        // state: 0 돌기(다가왔다 물러남) 1 공격 2 어지러움 3 쓰러짐
        void TickBoss(float dt, Vector3 pp)
        {
            var b = _boss; if (b.t == null) { _boss = null; return; }
            b.hitCd -= dt;
            if (b.state == 3) { b.stateT -= dt; if (b.model != null) b.model.transform.localScale = Vector3.one * Mathf.Max(0.01f, b.stateT / 1.2f); if (b.stateT <= 0f) { Destroy(b.t.gameObject); _boss = null; } return; }
            b.stateT -= dt;
            if (b.stars != null) { b.stars.gameObject.SetActive(b.state == 2); b.stars.Rotate(0f, 240f * dt, 0f); }
            float wantH = 0f;
            if (b.state == 0)
            {
                // 둘레를 돌며 반지름을 바꾼다(가까이 4 m ~ 멀리 11 m) — 원근이 살아나게. 가끔 바로 앞까지 덮친다.
                b.ang += dt * (b.kind == 1 ? 0.35f : b.kind == 0 ? 0.7f : 0.9f) * (Mathf.Sin(Time.time * 0.37f) > -0.3f ? 1f : -1f);
                if (_rng.Next(1000) < 12) b.radiusTarget = 4f + (float)_rng.NextDouble() * 7f;
                b.radius = Mathf.MoveTowards(b.radius, b.radiusTarget, dt * (b.kind == 1 ? 1.6f : 3.2f));
                wantH = b.kind == 0 ? 1.6f + Mathf.Sin(Time.time * 1.3f) * 0.4f : b.kind == 2 ? 0.5f + Mathf.Sin(Time.time * 2f) * 0.25f : 0f;
                if (b.stateT <= 0f) { b.state = 1; b.stateT = b.kind == 2 ? 1.4f : 2.2f; b.model?.Attack(); StartAttack(b, pp); }
            }
            else if (b.state == 1)
            {
                wantH = b.kind == 0 ? 3.0f : b.kind == 2 ? 0.8f : 0f;
                if (b.stateT <= 0f) { b.state = 2; b.stateT = 3f; b.radiusTarget = 6f; OnToast?.Invoke(Loc.T("★ 보스가 어지러워한다 — 지금 때리면 두 배!", "★ The boss is dizzy — hits deal double!")); }
            }
            else if (b.state == 2)
            {
                wantH = 0f;
                if (b.stateT <= 0f) { b.state = 0; b.stateT = 4f + (float)_rng.NextDouble() * 2.5f; }
            }
            b.hover = Mathf.MoveTowards(b.hover, wantH, dt * 2.5f);
            Vector3 target = b.state == 2 ? b.t.position : pp + new Vector3(Mathf.Cos(b.ang) * b.radius, 0f, Mathf.Sin(b.ang) * b.radius);
            target.x = Mathf.Clamp(target.x, C.x - 16f, C.x + 16f); target.z = Mathf.Clamp(target.z, C.z - 16f, C.z + 16f);
            var cur = b.t.position; var flat = new Vector3(cur.x, 0f, cur.z);
            float spd = b.kind == 1 ? 2.4f : b.kind == 0 ? 4.5f : 5.2f;
            var np = Vector3.MoveTowards(flat, new Vector3(target.x, 0f, target.z), spd * dt);
            if (b.kb.sqrMagnitude > 0.0004f) { var kp = np + b.kb * dt; if (Walkable(kp)) np = kp; b.kb *= Mathf.Exp(-dt * 7f); }   // 209차: 맞고 밀려나기(미끄러지듯)
            b.t.position = new Vector3(np.x, b.hover, np.z);
            if (b.model != null)
            {
                b.model.vel = (b.t.position - b.prev) / Mathf.Max(0.0001f, dt); b.model.vel = new Vector3(b.model.vel.x, 0f, b.model.vel.z); b.prev = b.t.position;
                b.model.lookAt = pp; b.model.flying = b.kind == 0 && b.hover > 0.5f;
            }
            // 몸통 박치기
            var dd = pp - b.t.position; dd.y = 0f;
            if (dd.magnitude < 1.4f && b.hitCd <= 0f && b.state != 2) { b.hitCd = 1.5f; OnHurt?.Invoke(6 + (b.kind == 1 ? 3 : 0), BossModel3D.NameKo(b.kind)); b.radiusTarget = 8f; }
            if (_barFill != null) _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(b.hp / (float)b.maxHp), 1f);
        }

        void StartAttack(Boss b, Vector3 pp)
        {
            if (b.kind == 0)
            {
                // 미사일 3발 — 주인공 자리로(0.25 s 간격 = 발사 시각을 음수 life 로 늦춤)
                for (int k = 0; k < 3; k++)
                {
                    var from = b.t.position + Vector3.up * 2.2f; var aim = pp + new Vector3((k - 1) * 1.2f, 0.6f, 0f);
                    var t = Prim(_root, PrimitiveType.Capsule, from, new Vector3(0.35f, 0.5f, 0.35f), Lit(new Color(1f, 0.55f, 0.2f), 0.4f)).transform;
                    t.rotation = Quaternion.LookRotation(aim - from) * Quaternion.Euler(90f, 0f, 0f);
                    _shots.Add(new Shot { t = t, vel = (aim - from).normalized * 10f, life = 3f + k * 0.25f, kind = 0, r = 0.8f, dmg = 8, hitCd = -k * 0.25f });
                }
            }
            else if (b.kind == 1)
            {
                // 바위 4개 — 빨간 원(1초 예고) 뒤 떨어진다
                for (int k = 0; k < 4; k++)
                {
                    var at = k == 0 ? pp : pp + new Vector3((float)_rng.NextDouble() * 6f - 3f, 0f, (float)_rng.NextDouble() * 6f - 3f); at.y = 0f;
                    var warn = Prim(_root, PrimitiveType.Cylinder, at + Vector3.up * 0.02f, new Vector3(2.4f, 0.01f, 2.4f), CoastMaterials.CreateTransparent(new Color(1f, 0.2f, 0.2f, 0.45f))).transform;
                    var rock = Prim(warn, PrimitiveType.Sphere, at + Vector3.up * 9f, Vector3.one, _rockD).transform; rock.localScale = new Vector3(0.55f, 130f, 0.55f);   // 부모(원판) 비율 보정
                    _shots.Add(new Shot { t = warn, vel = Vector3.zero, life = 1.1f + k * 0.18f, kind = 1, r = 1.3f, dmg = 10 });
                }
            }
            else
            {
                // 회오리 2개 — 5초 동안 천천히 쫓아온다
                for (int k = 0; k < 2; k++)
                {
                    var at = b.t.position + new Vector3(k == 0 ? -1.5f : 1.5f, 0f, 0f); at.y = 0f;
                    var t = new GameObject("Tornado").transform; t.SetParent(_root, false); t.position = at;
                    var tm = CoastMaterials.CreateTransparent(new Color(0.55f, 0.90f, 0.95f, 0.55f));
                    for (int L = 0; L < 5; L++) { var ring = Prim(t, PrimitiveType.Cylinder, at + Vector3.up * (0.3f + L * 0.45f), new Vector3(0.5f + L * 0.28f, 0.18f, 0.5f + L * 0.28f), tm); ring.name = "Ring" + L; }
                    _shots.Add(new Shot { t = t, vel = Vector3.zero, life = 5f, kind = 2, r = 1.0f, dmg = 6 });
                }
            }
        }

        void TickShots(float dt, Vector3 pp)
        {
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i]; if (s.t == null) { _shots.RemoveAt(i); continue; }
                s.life -= dt; bool dead = s.life <= 0f;
                if (s.kind == 0)
                {
                    if (s.hitCd < 0f) { s.hitCd += dt; continue; }
                    s.t.position += s.vel * dt; if (s.t.position.y < 0.3f) { s.vel.y = 0f; }
                    var d = s.t.position - (pp + Vector3.up * 0.8f);
                    if (d.magnitude < s.r) { OnHurt?.Invoke(s.dmg, BossModel3D.NameKo(0)); VillagePang.Burst(s.t.position, new Color(1f, 0.6f, 0.3f), Color.white, 0.9f); dead = true; }
                    if (!Walkable(s.t.position)) { VillagePang.Burst(s.t.position, new Color(0.7f, 0.6f, 0.5f), Color.white, 0.7f); dead = true; }
                }
                else if (s.kind == 1)
                {
                    var rock = s.t.childCount > 0 ? s.t.GetChild(0) : null; float k = Mathf.Clamp01(1f - s.life / 1.1f);
                    if (rock != null) rock.position = s.t.position + Vector3.up * Mathf.Lerp(9f, 0.4f, k * k);
                    if (dead)
                    {
                        var d = s.t.position - pp; d.y = 0f;
                        VillagePang.Burst(s.t.position + Vector3.up * 0.4f, new Color(0.6f, 0.55f, 0.5f), new Color(0.9f, 0.85f, 0.8f), 1.2f);
                        if (d.magnitude < s.r) OnHurt?.Invoke(s.dmg, BossModel3D.NameKo(1));
                    }
                }
                else
                {
                    var d = pp - s.t.position; d.y = 0f;
                    s.t.position += (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.zero) * 2.3f * dt; s.t.Rotate(0f, 540f * dt, 0f);
                    for (int c = 0; c < s.t.childCount; c++) { var ch = s.t.GetChild(c); ch.localPosition = new Vector3(Mathf.Sin(Time.time * 6f + c) * 0.12f * c, ch.localPosition.y, Mathf.Cos(Time.time * 6f + c) * 0.12f * c); }
                    s.hitCd -= dt;
                    if (d.magnitude < s.r && s.hitCd <= 0f) { s.hitCd = 1.0f; OnHurt?.Invoke(s.dmg, BossModel3D.NameKo(2)); }
                }
                if (dead) { if (s.t != null) Destroy(s.t.gameObject); _shots.RemoveAt(i); }
            }
        }

        /// 휘두르기 — 앞(또는 가까이) 2.6 m 안 몬스터·보스를 친다. 아무것도 없으면 null.
        public string Swing(int dmg, Vector3 fwd)
        {
            if (_root == null || Player == null) return null;
            var pp = Player.position; fwd.y = 0f; fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            if (_boss != null && _boss.state != 3 && _boss.t != null)
            {
                var d = _boss.t.position - pp; d.y = 0f;
                if (d.magnitude < 3.3f)
                {
                    int dd = _boss.state == 2 ? dmg * 2 : dmg; _boss.hp -= dd; _boss.model?.Hurt();
                    VillagePang.Burst(_boss.t.position + Vector3.up * 1.6f, new Color(1f, 0.45f, 0.45f), Color.white, 1.3f);
                    var push = d.sqrMagnitude > 0.01f ? d.normalized : fwd; _boss.kb = push * 10.5f;   // 209차: 1.5 m 순간이동 → 0.15 s 밀려남
                    if (_boss.hp <= 0)
                    {
                        _boss.hp = 0; _boss.state = 3; _boss.stateT = 1.2f; int kind = _boss.kind;
                        VillagePang.Burst(_boss.t.position + Vector3.up * 1.6f, new Color(1f, 0.85f, 0.3f), Color.white, 2.4f);
                        foreach (var s in _shots) if (s.t != null) Destroy(s.t.gameObject); _shots.Clear();
                        if (_bar != null) Destroy(_bar); _bar = null;
                        OnBossDown?.Invoke(kind);
                        return "";
                    }
                    return _boss.state == 2 ? Loc.T($"💥 크리티컬! −{dd} (남은 HP {_boss.hp})", $"💥 Critical! −{dd} (HP {_boss.hp})") : Loc.T($"🏏 보스에게 −{dd} (남은 HP {_boss.hp})", $"🏏 Boss −{dd} (HP {_boss.hp})");
                }
            }
            Mob best = null; float bd = 2.6f;
            foreach (var m in _mobs) { if (m.t == null) continue; var d = m.t.position - pp; d.y = 0f; float dist = d.magnitude; if (dist < bd && (dist < 1.4f || Vector3.Dot(d.normalized, fwd) > 0.2f)) { bd = dist; best = m; } }
            if (best == null) return null;
            best.hp -= dmg; best.stun = 0.6f;
            var away = best.t.position - pp; away.y = 0f; best.kb = (away.sqrMagnitude > 0.01f ? away.normalized : fwd) * 9.8f; best.v = 0f;   /* 209차: 맞으면 1.4 m 순간이동 → 미끄러지듯 날아감 */
            VillagePang.Burst(best.t.position + Vector3.up * 0.7f, new Color(0.85f, 0.75f, 1f), Color.white, 0.8f);
            if (best.hp > 0) return Loc.T($"🏏 {MobKo[best.kind]} −{dmg}", $"🏏 Hit −{dmg}");
            _mobs.Remove(best); Destroy(best.t.gameObject);
            return "mob:" + best.kind;
        }

        public bool NearStairsDown(Vector3 p) => Floor > 0 && Floor < BossFloor && Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(StairsDown.x, 0f, StairsDown.z)) < 2.2f;
        public bool NearStairsUp(Vector3 p) => Floor > 0 && Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(StairsUp.x, 0f, StairsUp.z)) < 2.0f;
        public int NearOre(Vector3 p)
        {
            for (int i = 0; i < Ores.Count; i++) { var o = Ores[i]; if (o.t == null) continue; if (Vector3.Distance(new Vector3(o.t.position.x, 0f, o.t.position.z), new Vector3(p.x, 0f, p.z)) < 2.1f) return i; }
            return -1;
        }
        public int TakeOre(int i) { var o = Ores[i]; Ores.RemoveAt(i); if (o.t != null) { VillagePang.Burst(o.t.position + Vector3.up, VillageZones.NodeColor(o.kind), Color.white, 0.9f); Destroy(o.t.gameObject); } return o.kind; }
        public int MobsLeft => _mobs.Count;
        public void DevTracked(List<KeyValuePair<string, Transform>> l)   // 209차
        {
            foreach (var m in _mobs) if (m.t != null) l.Add(new KeyValuePair<string, Transform>("mob" + m.kind, m.t));
            if (_boss != null && _boss.t != null) l.Add(new KeyValuePair<string, Transform>("boss", _boss.t));
        }
        public Vector3? DevTarget() { if (_boss != null && _boss.t != null && _boss.state != 3) return _boss.t.position; foreach (var m in _mobs) if (m.t != null) return m.t.position; return null; }
        public void DevHurtBoss(int n) { if (_boss != null) { _boss.hp = Mathf.Max(1, _boss.hp - n); } }
        public string DevState() => $"floor={Floor} mobs={_mobs.Count} ores={Ores.Count} boss={(_boss != null ? BossModel3D.NameKo(_boss.kind) + " hp=" + _boss.hp + " st=" + _boss.state : "none")} shots={_shots.Count}";
    }
}
