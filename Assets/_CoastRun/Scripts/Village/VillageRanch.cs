using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 176차(사용자: 「언덕쪽이 휑한데 집같은거 하고, 울타리 안에 말 뛰어노는 농장같은거 만들어줘, 넓게」).
    /// 언덕 서쪽(x −39~−23, z 19~41)에 넓은 방목장 — 통나무 울타리 + 헛간 + 목장집 + 여물통·건초, 말 네 마리가 안에서 어슬렁·달린다.
    public static class VillageRanch
    {
        public const float CX = -31f, CZ = 30f, HW = 8f, HD = 11f;   // 중심·반폭·반깊이
        public static Vector3 Gate => VillageWorld.Ground(CX + HW, CZ - 3f);
        // 183차(사용자: 「말도 때려서 식량으로」): 살아 있는 말 목록 · 이번 주 살아 있는 수(죽은 말은 주가 바뀌면 다시 태어난다)
        public static readonly List<RanchHorse> Horses = new List<RanchHorse>();
        public static int Alive = 4;
        public static int AliveFor(SaveData s) { if (s == null) return 4; if (s.ranchHorseWeek != s.week) { s.ranchHorseWeek = s.week; s.ranchHorseDead = 0; } return Mathf.Clamp(4 - s.ranchHorseDead, 0, 4); }

        public static void Build(Transform root)
        {
            var host = new GameObject("Ranch").transform; host.SetParent(root, false);
            var rng = new System.Random(176);

            // ── 울타리(동쪽 가운데 2.4 m 는 문) ─────────────────────────────
            var a = new Vector3(CX - HW, 0f, CZ - HD);
            var b = new Vector3(CX + HW, 0f, CZ - HD);
            var c = new Vector3(CX + HW, 0f, CZ + HD);
            var d = new Vector3(CX - HW, 0f, CZ + HD);
            VillageHouses.LogFence(host, a, b);
            VillageHouses.LogFence(host, b, new Vector3(CX + HW, 0f, CZ - 1.2f));
            VillageHouses.LogFence(host, new Vector3(CX + HW, 0f, CZ + 1.2f), c);
            VillageHouses.LogFence(host, c, d);
            VillageHouses.LogFence(host, d, a);
            // 보이지 않는 벽(말은 안에, 주인공은 문으로만) — 문 양쪽만 막는다
            void Wall(float cx, float cz, float sx, float sz)
            {
                var w = new GameObject("FenceCol", typeof(BoxCollider)); w.transform.SetParent(host, false);
                w.transform.position = VillageWorld.Ground(cx, cz) + new Vector3(0f, 0.6f, 0f);
                w.GetComponent<BoxCollider>().size = new Vector3(sx, 1.2f, sz);
            }
            Wall(CX, CZ - HD, HW * 2f, 0.3f); Wall(CX, CZ + HD, HW * 2f, 0.3f);
            Wall(CX - HW, CZ, 0.3f, HD * 2f);
            Wall(CX + HW, CZ - HD * 0.5f - 0.6f, 0.3f, HD - 1.2f);
            Wall(CX + HW, CZ + HD * 0.5f + 0.6f, 0.3f, HD - 1.2f);

            // ── 헛간(빨간 박공) ───────────────────────────────────────────
            Barn(host, VillageWorld.Ground(CX - HW + 3.4f, CZ + HD - 3.2f), 8f);
            // ── 목장집(울타리 밖, 문 옆) ─────────────────────────────────
            RanchHouse(host, VillageWorld.Ground(CX + HW + 5.5f, CZ + 5.5f), -100f);
            // ── 여물통·건초더미·물통 ──────────────────────────────────────
            Trough(host, VillageWorld.Ground(CX + HW - 2.6f, CZ + HD - 2.2f));
            for (int i = 0; i < 3; i++) HayBale(host, VillageWorld.Ground(CX - HW + 1.8f + i * 1.5f, CZ - HD + 2.0f), i * 37f);

            // ── 말 네 마리 ────────────────────────────────────────────────
            var mane = new[] { new Color(0.24f, 0.17f, 0.13f), new Color(0.95f, 0.92f, 0.86f), new Color(0.20f, 0.16f, 0.18f), new Color(0.42f, 0.28f, 0.18f) };
            var coat = new[] { new Color(0.55f, 0.36f, 0.22f), new Color(0.78f, 0.62f, 0.45f), new Color(0.35f, 0.28f, 0.26f), new Color(0.90f, 0.86f, 0.80f) };
            Horses.Clear();
            for (int i = 0; i < 4; i++)
            {
                if (i >= Alive) break;   // 183차: 이번 주 잡힌 말은 빼고
                float x = CX + (float)(rng.NextDouble() - 0.5) * (HW * 1.4f);
                float z = CZ + (float)(rng.NextDouble() - 0.5) * (HD * 1.4f);
                var h = Horse(host, VillageWorld.Ground(x, z), (float)rng.NextDouble() * 360f, coat[i], mane[i]);
                var w = h.gameObject.AddComponent<RanchHorse>();
                w.Init(new Vector2(CX, CZ), new Vector2(HW - 1.2f, HD - 1.2f), 0.9f + (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 6f);
                Horses.Add(w);
            }
        }

        // ── 조각들 ────────────────────────────────────────────────────────
        static Material M(Color c, float smooth = 0.04f) => CoastMaterials.CreateLit(c, smooth);

        static GameObject P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }

        /// 치비 말 — 앞뒤로 긴 몸통 + 짧고 굵은 목 + 주둥이 · 갈기 · 꼬리 · 네 다리(RanchHorse 가 흔든다)
        internal static Transform Horse(Transform parent, Vector3 pos, float yaw, Color coat, Color mane)
        {
            var t = new GameObject("Horse").transform; t.SetParent(parent, false);
            t.position = pos; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var body = M(coat); var dark = M(mane);
            var hoof = M(new Color(0.20f, 0.17f, 0.16f));
            var muzzleM = M(Color.Lerp(coat, new Color(0.35f, 0.26f, 0.22f), 0.45f));

            // 몸통: 앞뒤(Z)로 누운 캡슐 — 길이 ≈ 1.25 m
            P(t, PrimitiveType.Capsule, new Vector3(0f, 1.02f, 0f), new Vector3(0.60f, 0.62f, 0.60f), body, new Vector3(90f, 0f, 0f));
            // 엉덩이·가슴을 공으로 살짝 부풀린다
            P(t, PrimitiveType.Sphere, new Vector3(0f, 1.02f, -0.34f), new Vector3(0.64f, 0.60f, 0.60f), body);
            P(t, PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0.30f), new Vector3(0.60f, 0.58f, 0.58f), body);

            // 목: 짧고 굵게, 앞위로
            P(t, PrimitiveType.Capsule, new Vector3(0f, 1.32f, 0.44f), new Vector3(0.30f, 0.26f, 0.30f), body, new Vector3(38f, 0f, 0f));

            // 머리: 앞으로 뻗은 캡슐 + 주둥이 + 귀 + 눈
            var head = new GameObject("Head").transform; head.SetParent(t, false);
            head.localPosition = new Vector3(0f, 1.58f, 0.66f); head.localRotation = Quaternion.Euler(20f, 0f, 0f);
            P(head, PrimitiveType.Capsule, new Vector3(0f, 0f, 0.14f), new Vector3(0.25f, 0.24f, 0.25f), body, new Vector3(90f, 0f, 0f));
            P(head, PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0.34f), new Vector3(0.21f, 0.18f, 0.22f), muzzleM);
            for (int e = 0; e < 2; e++)
            {
                P(head, PrimitiveType.Capsule, new Vector3(e == 0 ? 0.10f : -0.10f, 0.18f, -0.06f), new Vector3(0.07f, 0.09f, 0.07f), body, new Vector3(-12f, 0f, e == 0 ? 14f : -14f));
                P(head, PrimitiveType.Sphere, new Vector3(e == 0 ? 0.12f : -0.12f, 0.04f, 0.16f), new Vector3(0.07f, 0.08f, 0.07f), M(new Color(0.10f, 0.09f, 0.11f)));
            }

            // 갈기: 목을 따라 작은 덩이 넷
            for (int k = 0; k < 4; k++)
                P(t, PrimitiveType.Sphere, new Vector3(0f, 1.22f + k * 0.11f, 0.30f + k * 0.09f), new Vector3(0.20f, 0.17f, 0.16f), dark);
            // 꼬리: 엉덩이 뒤로 처짐
            P(t, PrimitiveType.Capsule, new Vector3(0f, 1.08f, -0.60f), new Vector3(0.13f, 0.28f, 0.13f), dark, new Vector3(-42f, 0f, 0f));

            float[] lx = { 0.24f, -0.24f, 0.24f, -0.24f }; float[] lz = { 0.34f, 0.34f, -0.36f, -0.36f };
            for (int i = 0; i < 4; i++)
            {
                var leg = new GameObject("Leg" + i).transform; leg.SetParent(t, false);
                leg.localPosition = new Vector3(lx[i], 0.80f, lz[i]);
                P(leg, PrimitiveType.Capsule, new Vector3(0f, -0.32f, 0f), new Vector3(0.15f, 0.32f, 0.15f), body);
                P(leg, PrimitiveType.Cylinder, new Vector3(0f, -0.66f, 0f), new Vector3(0.17f, 0.06f, 0.17f), hoof);
            }
            BuildingOutline.Attach(t, 0.022f);
            return t;
        }

        static void Barn(Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject("Barn").transform; t.SetParent(parent, false);
            t.position = pos; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wall = M(new Color(0.78f, 0.28f, 0.26f)); var trim = M(new Color(0.97f, 0.95f, 0.90f));
            var roofTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Roof");
            var roof = roofTex != null ? ArtAssets.CreateTexturedLit(roofTex, new Color(0.55f, 0.20f, 0.19f), 0.02f) : M(new Color(0.55f, 0.20f, 0.19f));
            // 몸체
            P(t, PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(4.4f, 2.5f, 3.6f), wall);
            // 176차-2: 평평한 판 대신 박공 지붕 — 앞뒤로 기울어진 두 장
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.98f, -0.92f), new Vector3(4.8f, 0.26f, 2.3f), roof, new Vector3(-34f, 0f, 0f));
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.98f, 0.92f), new Vector3(4.8f, 0.26f, 2.3f), roof, new Vector3(34f, 0f, 0f));
            P(t, PrimitiveType.Cube, new Vector3(0f, 3.52f, 0f), new Vector3(4.9f, 0.20f, 0.30f), roof);   // 용마루
            // 큰 문(방목장 쪽 = −Z) + 흰 트림
            P(t, PrimitiveType.Cube, new Vector3(0f, 1.00f, -1.82f), new Vector3(1.9f, 2.0f, 0.12f), trim);
            P(t, PrimitiveType.Cube, new Vector3(0f, 1.00f, -1.86f), new Vector3(0.14f, 2.0f, 0.08f), wall);
            P(t, PrimitiveType.Cube, new Vector3(0f, 1.00f, -1.86f), new Vector3(1.9f, 0.14f, 0.08f), wall);
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.15f, -1.84f), new Vector3(2.2f, 0.16f, 0.1f), trim);
            P(t, PrimitiveType.Cube, new Vector3(-1.6f, 2.15f, -1.84f), new Vector3(1.0f, 0.14f, 0.1f), trim);
            P(t, PrimitiveType.Cube, new Vector3(1.6f, 2.15f, -1.84f), new Vector3(1.0f, 0.14f, 0.1f), trim);
            // 건초 다락 창
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.62f, -1.80f), new Vector3(0.8f, 0.7f, 0.1f), M(new Color(0.42f, 0.30f, 0.22f)));
            var col = new GameObject("BarnCol", typeof(BoxCollider)); col.transform.SetParent(t, false);
            col.transform.localPosition = new Vector3(0f, 1.25f, 0f); col.GetComponent<BoxCollider>().size = new Vector3(4.4f, 2.5f, 3.6f);
            BuildingOutline.Attach(t, 0.022f);
        }

        static void RanchHouse(Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject("RanchHouse").transform; t.SetParent(parent, false);
            t.position = pos; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var plank = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Plank");
            var wall = plank != null ? ArtAssets.CreateTexturedLit(plank, new Color(0.96f, 0.90f, 0.78f), 0.02f) : M(new Color(0.96f, 0.90f, 0.78f));
            var roofTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Roof");
            var roof = roofTex != null ? ArtAssets.CreateTexturedLit(roofTex, new Color(0.40f, 0.55f, 0.45f), 0.02f) : M(new Color(0.40f, 0.55f, 0.45f));
            var trim = M(new Color(0.55f, 0.38f, 0.26f));
            P(t, PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(3.6f, 2.2f, 3.0f), wall);
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.34f, 0f), new Vector3(4.0f, 0.34f, 3.4f), roof);
            P(t, PrimitiveType.Cube, new Vector3(0f, 2.72f, 0f), new Vector3(2.6f, 0.5f, 3.2f), roof);
            P(t, PrimitiveType.Cube, new Vector3(0f, 0.85f, 1.52f), new Vector3(0.9f, 1.7f, 0.12f), trim);
            P(t, PrimitiveType.Cube, new Vector3(-1.1f, 1.3f, 1.52f), new Vector3(0.7f, 0.7f, 0.1f), M(new Color(0.70f, 0.88f, 0.95f)));
            P(t, PrimitiveType.Cube, new Vector3(1.1f, 1.3f, 1.52f), new Vector3(0.7f, 0.7f, 0.1f), M(new Color(0.70f, 0.88f, 0.95f)));
            var col = new GameObject("HouseCol", typeof(BoxCollider)); col.transform.SetParent(t, false);
            col.transform.localPosition = new Vector3(0f, 1.1f, 0f); col.GetComponent<BoxCollider>().size = new Vector3(3.6f, 2.2f, 3.0f);
            BuildingOutline.Attach(t, 0.022f);
        }

        static void Trough(Transform parent, Vector3 pos)
        {
            var t = new GameObject("Trough").transform; t.SetParent(parent, false); t.position = pos;
            var wood = M(new Color(0.55f, 0.40f, 0.28f));
            P(t, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(1.8f, 0.44f, 0.7f), wood);
            P(t, PrimitiveType.Cube, new Vector3(0f, 0.40f, 0f), new Vector3(1.6f, 0.08f, 0.5f), M(new Color(0.45f, 0.72f, 0.92f)));
            BuildingOutline.Attach(t, 0.022f);
        }

        static void HayBale(Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject("Hay").transform; t.SetParent(parent, false); t.position = pos; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var hay = M(new Color(0.90f, 0.78f, 0.38f));
            P(t, PrimitiveType.Cylinder, new Vector3(0f, 0.46f, 0f), new Vector3(0.92f, 0.46f, 0.92f), hay, new Vector3(0f, 0f, 90f));
            P(t, PrimitiveType.Cube, new Vector3(0f, 0.46f, 0f), new Vector3(0.30f, 0.94f, 0.94f), M(new Color(0.72f, 0.60f, 0.28f)));
            BuildingOutline.Attach(t, 0.022f);
        }
    }

    /// 방목장 안을 어슬렁거리다 가끔 달리는 말. 다리는 걷는 속도에 맞춰 흔든다.
    public class RanchHorse : MonoBehaviour
    {
        Vector2 _c, _half; float _speedK, _phase, _wait, _run; Vector3 _dir;
        Transform[] _legs; Transform _head;
        // 183차: 방망이 세 방이면 쓰러진다. 맞으면 반대쪽으로 달아난다
        public int Hp = 3; public bool Dead; float _deadT;
        /// 맞기 — 쓰러지면 true
        public bool Hit(Vector3 from)
        {
            if (Dead) return false;
            Hp--; var away = transform.position - from; away.y = 0f; _dir = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
            _run = 2.5f; _wait = 2.5f;
            if (Hp > 0) return false;
            Dead = true; _deadT = 0f; VillageRanch.Horses.Remove(this);
            return true;
        }

        public void Init(Vector2 center, Vector2 half, float speedK, float phase)
        {
            _c = center; _half = half; _speedK = speedK; _phase = phase;
            _legs = new Transform[4];
            for (int i = 0; i < 4; i++) _legs[i] = transform.Find("Leg" + i);
            _head = transform.Find("Head");
            _dir = new Vector3(Mathf.Cos(phase), 0f, Mathf.Sin(phase));
        }

        void Update()
        {
            float dt = Time.deltaTime; _phase += dt;
            if (Dead)
            {
                // 옆으로 쓰러졌다 2.5초 뒤 사라짐
                _deadT += dt; transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 88f), dt * 5f);
                if (_deadT > 2.5f) Destroy(gameObject);
                return;
            }
            _wait -= dt; _run -= dt;
            if (_wait <= 0f)
            {
                _wait = Random.Range(2.5f, 6f);
                if (Random.value < 0.35f) { _run = Random.Range(2f, 4f); }                       // 가끔 달린다
                float ang = Random.Range(0f, Mathf.PI * 2f);
                _dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                if (Random.value < 0.22f) _dir = Vector3.zero;                                   // 가끔 풀을 뜯는다
            }
            bool running = _run > 0f;
            float sp = (_dir == Vector3.zero ? 0f : (running ? 3.2f : 0.9f)) * _speedK;
            var p = transform.position + _dir * sp * dt;
            // 울타리 안으로 되돌린다
            if (Mathf.Abs(p.x - _c.x) > _half.x) { _dir.x = -_dir.x; p.x = transform.position.x; }
            if (Mathf.Abs(p.z - _c.y) > _half.y) { _dir.z = -_dir.z; p.z = transform.position.z; }
            transform.position = new Vector3(p.x, VillageWorld.Height(p.x, p.z), p.z);
            if (_dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_dir, Vector3.up), dt * 4f);
            // 다리 흔들기 · 달릴 때 몸이 살짝 뛴다
            float swing = sp > 0.05f ? Mathf.Sin(_phase * (running ? 13f : 6f)) * (running ? 34f : 16f) : 0f;
            if (_legs != null)
                for (int i = 0; i < 4; i++)
                {
                    if (_legs[i] == null) continue;
                    float s = (i == 0 || i == 3) ? swing : -swing;
                    _legs[i].localRotation = Quaternion.Euler(s, 0f, 0f);
                }
            if (_head != null)
            {
                float graze = _dir == Vector3.zero ? Mathf.Max(0f, Mathf.Sin(_phase * 1.2f)) * 26f : 0f;
                _head.localRotation = Quaternion.Euler(20f + graze, 0f, 0f);
            }
        }
    }
}
