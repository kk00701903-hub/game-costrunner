using UnityEngine;

namespace CoastRun
{
    /// 199차(사용자: 「kpop 러닝 보스들의 모션이 너무 2d 같다. 움직임 자연스럽게, 앞으로 갔다 뒤로 갔다 원근감 잘 구현」):
    /// 그림(빌보드) 대신 입체 모형 — 갈매기 해적 · 돌하르방 골렘 · 태풍 도깨비(러닝 보스 그림과 같은 생김새).
    /// 몸은 카메라 쪽 요(yaw)로만 돌고 움직이는 쪽으로 반쯤 틀어서, 옆으로 갈 땐 옆모습·다가올 땐 앞으로 숙인 모습이 보인다.
    /// 부위(날개·팔·발·머리털·눈)가 따로 움직인다: 날갯짓, 걸음(발 들기·팔 흔들기·몸 좌우 흔들림), 숨쉬기, 눈 깜빡임, 공격 전 움츠림→튕김, 맞으면 흔들림.
    /// 러닝(BossDirector)과 광산 던전 보스(VillageDungeon) 둘 다 쓴다. 크기는 Build 의 height(발끝~모자 끝, m).
    public class BossModel3D : MonoBehaviour
    {
        public int kind;                 // 0 갈매기 해적 · 1 돌하르방 골렘 · 2 태풍 도깨비
        public Vector3 vel;              // 월드 속도(부르는 쪽이 매 프레임 넣는다)
        public Vector3? lookAt;          // 이 점 쪽을 본다(보통 카메라나 주인공)
        public bool flying;              // 날고 있으면 날갯짓·발 늘어짐
        public float speedRef = 4f;      // 이 속도쯤이면 「걷는 중」
        public float baseWalk;           // 가만히 있어도 이만큼은 걷는/나는 듯(러닝 보스)
        float _t, _phase, _attack, _hurt, _blinkT = 2f, _blink, _yaw, _yawVel, _leanP, _leanR;
        Transform _body, _head, _wingL, _wingR, _armL, _armR, _legL, _legR, _eyes, _hair;
        Vector3 _bodyBase, _legLBase, _legRBase;
        float _h;

        public static string NameKo(int k) => k == 0 ? "갈매기 해적" : k == 1 ? "돌하르방 골렘" : "태풍 도깨비";

        public void Attack() { _attack = 1f; }
        public void Hurt() { _hurt = 1f; }

        public static BossModel3D Build(Transform parent, int kind, float height, float ink = 0.05f)
        {
            var root = new GameObject("BossModel_" + kind).transform; root.SetParent(parent, false);
            var m = root.gameObject.AddComponent<BossModel3D>(); m.kind = kind; m._h = height;
            var scaler = new GameObject("Scale").transform; scaler.SetParent(root, false); scaler.localScale = Vector3.one * height;
            m._body = new GameObject("Body").transform; m._body.SetParent(scaler, false);
            switch (kind) { case 0: m.BuildGull(scaler); break; case 1: m.BuildGolem(scaler); break; default: m.BuildDokkaebi(scaler); break; }
            m._bodyBase = m._body.localPosition;
            if (m._legL != null) m._legLBase = m._legL.localPosition; if (m._legR != null) m._legRBase = m._legR.localPosition;
            if (ink > 0f) BuildingOutline.AttachThick(root, ink);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m._yaw = parent != null ? parent.eulerAngles.y : 0f;
            return m;
        }

        // ── 부위 도우미 ──
        static Material M(Color c, float s = 0.25f) => CoastMaterials.CreateLit(c, s);
        static Transform P(Transform p, PrimitiveType t, Vector3 pos, Vector3 scl, Material m, Vector3 rot = default)
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = scl; g.transform.localRotation = Quaternion.Euler(rot);
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g.transform;
        }
        static Transform Pivot(Transform p, string n, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(p, false); t.localPosition = pos; return t; }
        static Mesh _cone;
        static Transform Cone(Transform p, Vector3 pos, Vector3 scl, Material m, Vector3 rot)
        {
            if (_cone == null)
            {
                const int N = 12; var v = new Vector3[N + 2]; var tri = new int[N * 6];
                for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2f / N; v[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f); }
                v[N] = new Vector3(0f, 1f, 0f); v[N + 1] = Vector3.zero;
                for (int i = 0; i < N; i++) { int j = (i + 1) % N; tri[i * 6] = i; tri[i * 6 + 1] = N; tri[i * 6 + 2] = j; tri[i * 6 + 3] = j; tri[i * 6 + 4] = N + 1; tri[i * 6 + 5] = i; }
                _cone = new Mesh { name = "BossCone", vertices = v, triangles = tri }; _cone.RecalculateNormals(); _cone.RecalculateBounds();
            }
            var g = new GameObject("Horn", typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(p, false);
            g.GetComponent<MeshFilter>().sharedMesh = _cone; g.GetComponent<MeshRenderer>().sharedMaterial = m;
            g.transform.localPosition = pos; g.transform.localScale = scl; g.transform.localRotation = Quaternion.Euler(rot); return g.transform;
        }
        void Eyes(Transform head, float y, float x, float z, float s, bool patchLeft)
        {
            _eyes = Pivot(head, "Eyes", Vector3.zero);
            var black = M(new Color(0.07f, 0.06f, 0.08f), 0.7f); var white = CoastMaterials.CreateUnlit(Color.white);
            for (int k = -1; k <= 1; k += 2)
            {
                if (patchLeft && k < 0) continue;
                var e = P(_eyes, PrimitiveType.Sphere, new Vector3(k * x, y, z), new Vector3(s, s * 1.25f, s * 0.7f), black);
                P(e, PrimitiveType.Sphere, new Vector3(0.22f, 0.25f, 0.35f), Vector3.one * 0.32f, white);   // 눈 반짝
            }
        }

        // ── 갈매기 해적: 흰 달걀 몸 · 분홍 배 · 주황 부리 · 한쪽 안대 · 검은 해적 모자(초록 십자) · 날개 · 주황 발 ──
        void BuildGull(Transform s)
        {
            var white = M(new Color(0.97f, 0.97f, 0.96f)); var pink = M(new Color(1f, 0.86f, 0.90f)); var orange = M(new Color(1f, 0.62f, 0.16f), 0.4f);
            var navy = M(new Color(0.13f, 0.15f, 0.22f), 0.35f); var green = M(new Color(0.30f, 0.78f, 0.52f));
            _body.localPosition = new Vector3(0f, 0.05f, 0f);
            P(_body, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.76f, 0.84f, 0.72f), white);
            P(_body, PrimitiveType.Sphere, new Vector3(0f, 0.30f, 0.09f), new Vector3(0.64f, 0.50f, 0.56f), pink);
            _head = Pivot(_body, "Head", Vector3.zero);
            P(_head, PrimitiveType.Sphere, new Vector3(0f, 0.53f, 0.36f), new Vector3(0.17f, 0.09f, 0.17f), orange);
            P(_head, PrimitiveType.Sphere, new Vector3(0f, 0.49f, 0.34f), new Vector3(0.13f, 0.06f, 0.12f), orange);
            Eyes(_head, 0.62f, 0.13f, 0.31f, 0.075f, true);
            P(_head, PrimitiveType.Sphere, new Vector3(-0.13f, 0.63f, 0.30f), new Vector3(0.17f, 0.13f, 0.07f), navy);   // 안대
            P(_head, PrimitiveType.Cylinder, new Vector3(0f, 0.65f, 0f), new Vector3(0.78f, 0.012f, 0.74f), navy, new Vector3(0f, 0f, 14f));   // 안대 끈
            P(_head, PrimitiveType.Cylinder, new Vector3(0f, 0.83f, 0f), new Vector3(0.98f, 0.03f, 0.74f), navy, new Vector3(-6f, 0f, 0f));   // 챙
            P(_head, PrimitiveType.Sphere, new Vector3(0f, 0.87f, -0.02f), new Vector3(0.64f, 0.38f, 0.54f), navy);
            P(_head, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.25f), new Vector3(0.10f, 0.03f, 0.02f), green, new Vector3(-20f, 0f, 0f));
            P(_head, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.25f), new Vector3(0.03f, 0.10f, 0.02f), green, new Vector3(-20f, 0f, 0f));
            _wingL = Pivot(_body, "WingL", new Vector3(-0.36f, 0.50f, 0f)); _wingR = Pivot(_body, "WingR", new Vector3(0.36f, 0.50f, 0f));
            P(_wingL, PrimitiveType.Sphere, new Vector3(-0.06f, -0.13f, 0f), new Vector3(0.12f, 0.34f, 0.26f), white, new Vector3(0f, 0f, -14f));
            P(_wingR, PrimitiveType.Sphere, new Vector3(0.06f, -0.13f, 0f), new Vector3(0.12f, 0.34f, 0.26f), white, new Vector3(0f, 0f, 14f));
            _legL = Pivot(s, "LegL", new Vector3(-0.12f, 0.06f, 0.02f)); _legR = Pivot(s, "LegR", new Vector3(0.12f, 0.06f, 0.02f));
            P(_legL, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.06f), new Vector3(0.15f, 0.05f, 0.21f), orange);
            P(_legR, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.06f), new Vector3(0.15f, 0.05f, 0.21f), orange);
        }

        // ── 돌하르방 골렘: 울퉁불퉁 회색 돌 몸 · 머리 위 이끼 · 분홍 볼 · 웃는 입 · 둥근 돌 팔 · 짧은 발 ──
        void BuildGolem(Transform s)
        {
            var rock = M(new Color(0.56f, 0.56f, 0.54f), 0.1f); var rockD = M(new Color(0.45f, 0.45f, 0.44f), 0.1f); var moss = M(new Color(0.46f, 0.60f, 0.26f), 0.1f);
            var pink = M(new Color(0.98f, 0.62f, 0.66f)); var black = M(new Color(0.08f, 0.07f, 0.08f), 0.6f);
            _body.localPosition = new Vector3(0f, 0.08f, 0f);
            P(_body, PrimitiveType.Sphere, new Vector3(0f, 0.44f, 0f), new Vector3(0.80f, 0.88f, 0.68f), rock);
            P(_body, PrimitiveType.Sphere, new Vector3(0.20f, 0.70f, -0.08f), new Vector3(0.36f, 0.30f, 0.34f), rock);
            P(_body, PrimitiveType.Sphere, new Vector3(-0.22f, 0.30f, -0.06f), new Vector3(0.34f, 0.32f, 0.32f), rockD);
            P(_body, PrimitiveType.Sphere, new Vector3(0.10f, 0.18f, 0.18f), new Vector3(0.30f, 0.22f, 0.24f), rock);
            _head = Pivot(_body, "Head", Vector3.zero);
            P(_head, PrimitiveType.Sphere, new Vector3(0f, 0.84f, -0.02f), new Vector3(0.56f, 0.18f, 0.46f), moss);
            P(_head, PrimitiveType.Sphere, new Vector3(0.14f, 0.90f, 0.02f), new Vector3(0.22f, 0.12f, 0.2f), moss);
            Eyes(_head, 0.63f, 0.12f, 0.31f, 0.06f, false);
            for (int k = -1; k <= 1; k += 2) P(_head, PrimitiveType.Sphere, new Vector3(k * 0.22f, 0.55f, 0.28f), new Vector3(0.10f, 0.06f, 0.04f), pink);
            for (int i = -2; i <= 2; i++) P(_head, PrimitiveType.Sphere, new Vector3(i * 0.028f, 0.545f + Mathf.Abs(i) * 0.008f, 0.335f), Vector3.one * 0.028f, black);   // 웃는 입
            _armL = Pivot(_body, "ArmL", new Vector3(-0.40f, 0.52f, 0f)); _armR = Pivot(_body, "ArmR", new Vector3(0.40f, 0.52f, 0f));
            P(_armL, PrimitiveType.Sphere, new Vector3(-0.04f, -0.14f, 0.03f), new Vector3(0.22f, 0.30f, 0.22f), rock);
            P(_armR, PrimitiveType.Sphere, new Vector3(0.04f, -0.14f, 0.03f), new Vector3(0.22f, 0.30f, 0.22f), rock);
            _legL = Pivot(s, "LegL", new Vector3(-0.16f, 0.06f, 0.02f)); _legR = Pivot(s, "LegR", new Vector3(0.16f, 0.06f, 0.02f));
            P(_legL, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.13f, 0.27f), rockD);
            P(_legR, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.13f, 0.27f), rockD);
        }

        // ── 태풍 도깨비: 민트 달걀 몸 · 곱슬 청록 머리털 · 노란 뿔 둘 · 이 보이는 웃음 · 팔·다리 ──
        void BuildDokkaebi(Transform s)
        {
            var mint = M(new Color(0.56f, 0.88f, 0.78f)); var mintD = M(new Color(0.36f, 0.78f, 0.72f)); var hair = M(new Color(0.22f, 0.78f, 0.80f), 0.05f);
            var horn = M(new Color(1f, 0.90f, 0.48f), 0.4f); var mouth = M(new Color(0.35f, 0.08f, 0.10f)); var tooth = CoastMaterials.CreateUnlit(Color.white); var pink = M(new Color(0.98f, 0.66f, 0.70f));
            _body.localPosition = new Vector3(0f, 0.10f, 0f);
            P(_body, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.84f, 0.64f), mint);
            _head = Pivot(_body, "Head", Vector3.zero);
            _hair = Pivot(_head, "Hair", new Vector3(0f, 0.74f, -0.02f));
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f; float r = 0.30f + (i % 2) * 0.04f;
                if (Mathf.Sin(a) > 0.55f) continue;   // 이마(앞)는 비운다
                P(_hair, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, 0.02f + (i % 3) * 0.04f, Mathf.Sin(a) * r * 0.9f), Vector3.one * (0.24f + (i % 3) * 0.03f), hair);
            }
            P(_hair, PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.04f), new Vector3(0.48f, 0.26f, 0.44f), hair);
            P(_hair, PrimitiveType.Sphere, new Vector3(0.12f, 0.16f, 0.06f), Vector3.one * 0.22f, hair);
            P(_hair, PrimitiveType.Sphere, new Vector3(-0.14f, 0.15f, 0.04f), Vector3.one * 0.22f, hair);
            Cone(_head, new Vector3(-0.20f, 0.96f, 0f), new Vector3(0.08f, 0.22f, 0.08f), horn, new Vector3(0f, 0f, 22f));
            Cone(_head, new Vector3(0.20f, 0.96f, 0f), new Vector3(0.08f, 0.22f, 0.08f), horn, new Vector3(0f, 0f, -22f));
            Eyes(_head, 0.60f, 0.11f, 0.29f, 0.062f, false);
            P(_head, PrimitiveType.Sphere, new Vector3(0f, 0.50f, 0.30f), new Vector3(0.17f, 0.075f, 0.06f), mouth);
            P(_head, PrimitiveType.Cube, new Vector3(0f, 0.522f, 0.318f), new Vector3(0.12f, 0.022f, 0.02f), tooth);
            for (int k = -1; k <= 1; k += 2) P(_head, PrimitiveType.Sphere, new Vector3(k * 0.21f, 0.53f, 0.26f), new Vector3(0.09f, 0.055f, 0.04f), pink);
            _armL = Pivot(_body, "ArmL", new Vector3(-0.35f, 0.46f, 0f)); _armR = Pivot(_body, "ArmR", new Vector3(0.35f, 0.46f, 0f));
            P(_armL, PrimitiveType.Capsule, new Vector3(-0.03f, -0.13f, 0f), new Vector3(0.13f, 0.14f, 0.13f), mintD, new Vector3(0f, 0f, -12f));
            P(_armR, PrimitiveType.Capsule, new Vector3(0.03f, -0.13f, 0f), new Vector3(0.13f, 0.14f, 0.13f), mintD, new Vector3(0f, 0f, 12f));
            _legL = Pivot(s, "LegL", new Vector3(-0.14f, 0.12f, 0f)); _legR = Pivot(s, "LegR", new Vector3(0.14f, 0.12f, 0f));
            P(_legL, PrimitiveType.Capsule, new Vector3(0f, -0.06f, 0f), new Vector3(0.15f, 0.08f, 0.15f), mintD);
            P(_legR, PrimitiveType.Capsule, new Vector3(0f, -0.06f, 0f), new Vector3(0.15f, 0.08f, 0.15f), mintD);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime; if (dt <= 0f) return; _t += dt;
            var v = vel; v.y = 0f; float sp = v.magnitude; float walk = Mathf.Max(baseWalk, Mathf.Clamp01(sp / Mathf.Max(0.1f, speedRef)));
            // ── 방향: 볼 곳(카메라·주인공)을 보되, 움직이는 쪽으로 반쯤 튼다 → 옆으로 갈 땐 옆모습(입체감)
            float want = _yaw;
            if (lookAt.HasValue) { var d = lookAt.Value - transform.position; d.y = 0f; if (d.sqrMagnitude > 0.01f) want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
            if (sp > 0.3f)
            {
                float mv = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg; float diff = Mathf.DeltaAngle(want, mv);
                want += Mathf.Clamp(diff, -70f, 70f) * 0.55f * walk;
            }
            _yaw = Mathf.SmoothDampAngle(_yaw, want, ref _yawVel, kind == 1 ? 0.45f : 0.25f);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            // ── 몸 기울기: 앞으로 가면 숙이고, 옆으로 가면 그쪽으로 기운다
            var local = Quaternion.Euler(0f, -_yaw, 0f) * v;
            float wantP = Mathf.Clamp(local.z / Mathf.Max(0.1f, speedRef) * (kind == 1 ? 7f : 14f), -16f, 18f);
            float wantR = Mathf.Clamp(-local.x / Mathf.Max(0.1f, speedRef) * (kind == 1 ? 6f : 16f), -22f, 22f);
            _leanP = Mathf.Lerp(_leanP, wantP, dt * 5f); _leanR = Mathf.Lerp(_leanR, wantR, dt * 5f);
            // ── 걸음 위상
            float stepHz = kind == 1 ? 1.3f : kind == 0 ? 2.6f : 2.2f;
            _phase += dt * Mathf.PI * 2f * stepHz * Mathf.Lerp(0.35f, 1.2f, walk);
            float step = Mathf.Sin(_phase);
            // ── 공격(움츠렸다가 앞으로 튕김) · 맞음(흔들림)
            _attack = Mathf.MoveTowards(_attack, 0f, dt * 1.8f); _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 2.5f);
            float wind = _attack > 0.55f ? Mathf.Sin((1f - _attack) / 0.45f * Mathf.PI) : 0f;      // 앞쪽 0.25 s 움츠림
            float lunge = _attack > 0f && _attack <= 0.55f ? Mathf.Sin(_attack / 0.55f * Mathf.PI) : 0f;
            float shake = _hurt > 0f ? Mathf.Sin(_t * 60f) * _hurt : 0f;
            // ── 숨쉬기·통통 튀기
            float breathe = Mathf.Sin(_t * (kind == 1 ? 1.6f : 2.4f));
            float bounce = kind == 1 ? Mathf.Abs(step) * 0.018f * walk : kind == 2 ? Mathf.Abs(step) * 0.05f * walk : flying ? Mathf.Sin(_t * 10.4f) * 0.015f : Mathf.Abs(step) * 0.03f * walk;
            var bp = _bodyBase + new Vector3(shake * 0.02f, bounce + breathe * 0.008f - wind * 0.03f, lunge * 0.10f);
            _body.localPosition = bp;
            float sway = kind == 1 ? step * 5f * walk : kind == 2 ? Mathf.Sin(_t * 3.1f) * 6f : step * 3f * walk;
            _body.localRotation = Quaternion.Euler(_leanP + lunge * 14f - wind * 8f, kind == 2 ? Mathf.Sin(_t * 2.3f) * 10f : 0f, _leanR + sway + shake * 6f);
            _body.localScale = new Vector3(1f + wind * 0.10f - breathe * 0.01f, 1f - wind * 0.12f + breathe * 0.015f + lunge * 0.05f, 1f + wind * 0.08f);
            // ── 머리: 살짝 늦게 따라오며 갸웃
            if (_head != null) _head.localRotation = Quaternion.Euler(-_leanP * 0.35f + Mathf.Sin(_t * 0.9f) * 3f, Mathf.Sin(_t * 0.6f) * 8f, Mathf.Sin(_t * 0.7f + 1f) * 5f);
            // ── 눈 깜빡
            _blinkT -= dt; if (_blinkT <= 0f) { _blink = 0.14f; _blinkT = Random.Range(2.2f, 4.8f); }
            if (_blink > 0f) _blink -= dt;
            if (_eyes != null) _eyes.localScale = new Vector3(1f, _blink > 0f ? 0.15f : 1f, 1f);
            // ── 날개(갈매기)
            if (_wingL != null)
            {
                float flap = flying ? Mathf.Sin(_t * 10.4f) * 48f + 20f : Mathf.Sin(_t * 2.2f) * 8f + walk * step * 14f;
                flap += wind * 30f - lunge * 25f;
                _wingL.localRotation = Quaternion.Euler(0f, 0f, -flap); _wingR.localRotation = Quaternion.Euler(0f, 0f, flap);
            }
            // ── 팔(골렘·도깨비): 걸을 땐 반대로 흔들고, 공격 땐 번쩍 든다
            if (_armL != null)
            {
                float swing = step * (kind == 1 ? 26f : 34f) * walk;
                float lift = wind * 110f + lunge * 60f; float wave = kind == 2 ? Mathf.Sin(_t * 4.2f) * 12f : 0f;
                _armL.localRotation = Quaternion.Euler(-swing - lift, 0f, -8f - wave - lift * 0.3f);
                _armR.localRotation = Quaternion.Euler(swing - lift, 0f, 8f + wave + lift * 0.3f);
            }
            // ── 발: 번갈아 들고 앞뒤로(날 땐 늘어뜨림)
            if (_legL != null)
            {
                if (flying) { float dang = Mathf.Sin(_t * 3f) * 10f; _legL.localRotation = Quaternion.Euler(35f + dang, 0f, 0f); _legR.localRotation = Quaternion.Euler(35f - dang, 0f, 0f); _legL.localPosition = _legLBase; _legR.localPosition = _legRBase; }
                else
                {
                    float lift = kind == 1 ? 0.07f : 0.05f; float stride = kind == 1 ? 0.06f : 0.07f;
                    _legL.localPosition = _legLBase + new Vector3(0f, Mathf.Max(0f, step) * lift * walk, step * stride * walk);
                    _legR.localPosition = _legRBase + new Vector3(0f, Mathf.Max(0f, -step) * lift * walk, -step * stride * walk);
                    _legL.localRotation = Quaternion.Euler(-step * 20f * walk, 0f, 0f); _legR.localRotation = Quaternion.Euler(step * 20f * walk, 0f, 0f);
                }
            }
            // ── 도깨비 머리털 출렁
            if (_hair != null) _hair.localScale = new Vector3(1f + Mathf.Sin(_t * 5f) * 0.03f, 1f + Mathf.Sin(_t * 5f + 1.2f) * 0.04f, 1f);
        }
    }
}
