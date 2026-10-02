using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 170차(사용자: 「하늘에 구름하고 비행기·새 등 그려주고」): 카메라를 따라다니는 하늘 층.
    /// 168차 구름은 마을 좌표에 고정이라 어느 쪽을 봐도 빈 하늘이 자주 나왔다 → 이 층은 카메라 x·z 를 따라가며
    /// 360° 링에 구름 14, 비행기 1(비행운 끌며 가끔 가로지름), 새 떼 2(V 자, 날개짓)를 항상 시야 안에 둔다.
    /// 밤(VillageDayNight.Night)엔 구름이 어두워지고 비행기·새는 사라진다.
    /// ★ 보이는 높이 계산: 마을 카메라는 24° 내려다보고 세로 FOV 50 이라 화면 위 끝이 겨우 +1°. 대신 방사형 곡면(VillageHub.CurveK)이 먼 땅을
    ///   K·d² 만큼 끌어내려 그 위가 하늘로 보인다. 그래서 하늘 물건은 **곡면 재질(_CurveWeight 1)** 로 같이 휘게 하고,
    ///   높이를 `y = 카메라 높이 + d·tan(겉보기 고도) + K·d²` 로 둔다(겉보기 고도 −12°~−2° 가 화면의 하늘 띠). 평평한(SetFlat) 재질은 절대 안 보인다.
    public class VillageSky : MonoBehaviour
    {
        const float RingR = 130f;                   // 구름 링 반지름(안개 밖·far 420 안)
        const float CamH = 9.0f;                    // VillageHub.CamHeight 와 같은 값(219차 6.4→7.1 · 230차 → 9.0)
        static float SkyY(float d, float apparentDeg) => CamH + d * Mathf.Tan(apparentDeg * Mathf.Deg2Rad) + VillageHub.CurveK * d * d;
        static Material Curved(Material m) { if (m != null && m.HasProperty("_CurveWeight")) m.SetFloat("_CurveWeight", 1f); return m; }
        /// 셰이더가 _BaseColor/_Color 어느 쪽이든 틴트를 먹인다(Material.color 는 [MainColor] 가 없는 셰이더에서 빗나간다)
        public static void SetTint(Material m, Color c) { if (m == null) return; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else if (m.HasProperty("_Color")) m.SetColor("_Color", c); else m.color = c; }
        static Color GetTint(Material m) => m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : m.color;
        readonly List<Material> _cloudMats = new List<Material>();
        readonly List<Transform> _clouds = new List<Transform>();
        Transform _plane, _flockA, _flockB; readonly List<Transform> _trail = new List<Transform>(); readonly List<float> _trailT = new List<float>();
        readonly List<Material> _trailM = new List<Material>();
        float _planeA0, _planeA1, _planeR = 150f, _planeT = -1f, _planeWait = 12f, _planeDur = 55f, _trailTick;
        Material _planeM, _birdM;

        public static VillageSky I;
        /// 개발 메뉴: 비행기를 지금 띄운다(카메라 정면 위 호로)
        public void DevPlaneNow()
        {
            var cam = Camera.main; if (cam == null) return;
            if (_planeT >= 0f) { _planeT = -1f; _plane.gameObject.SetActive(false); }
            var lf = transform.InverseTransformDirection(cam.transform.forward); float f = Mathf.Atan2(lf.z, lf.x);   // 링 로컬 각도
            _planeA0 = f - Mathf.Deg2Rad * 40f; _planeA1 = f + Mathf.Deg2Rad * 40f; _planeR = 100f;
            _planeT = 0f; _planeDur = 30f; _plane.gameObject.SetActive(true); _plane.localScale = Vector3.one * 2.3f;
        }

        public static VillageSky Create(Transform root)
        {
            var go = new GameObject("VillageSky"); go.transform.SetParent(root, false);
            var s = go.AddComponent<VillageSky>(); I = s; s.Build(); return s;
        }

        void Build()
        {
            var rng = new System.Random(170);
            var painted = new List<Texture2D>();
            foreach (var n in new[] { "Cloud_Cumulus_A", "Cloud_Cumulus_B", "Cloud_Cumulus_C" }) { var t = ArtAssets.LoadTexture(n + "_V") ?? ArtAssets.LoadTexture(n); if (t != null) painted.Add(t); }   // 187차: 마을용 _V(남색 테두리 → 하늘색)
            // 구름 링: 14개, 25° 간격 근처에 흩뿌림, 겉보기 고도 −11~−3°(하늘 띠의 아래~위)
            for (int i = 0; i < 14; i++)
            {
                float ang = i * (Mathf.PI * 2f / 14f) + (float)(rng.NextDouble() - 0.5) * 0.3f;
                float r = RingR * (0.85f + (float)rng.NextDouble() * 0.5f);
                float app = -14f + (float)rng.NextDouble() * 8f;   // 캡처로 보정: 계산보다 ≈3° 위에 보인다
                var c = new GameObject("SkyCloud").transform; c.SetParent(transform, false);
                c.localPosition = new Vector3(Mathf.Cos(ang) * r, SkyY(r, app), Mathf.Sin(ang) * r);
                float sc = (38f + (float)rng.NextDouble() * 30f) * (r / RingR);
                if (painted.Count > 0)
                {
                    var tex = painted[rng.Next(painted.Count)];
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(c, false);
                    float aspect = tex.width / (float)tex.height;
                    q.transform.localScale = new Vector3(sc * (rng.NextDouble() < 0.5 ? -1f : 1f), sc / aspect, 1f);
                    var m = CoastMaterials.CreateTexturedTransparentCurved(tex, new Color(1f, 1f, 1f, 0.92f)); CoastMaterials.SetNoFog(m, 0f); Curved(m);
                    var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                    _cloudMats.Add(m);
                }
                else
                {
                    var m = Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTransparent(new Color(0.96f, 0.98f, 1f, 1f))));
                    int n = 3 + rng.Next(3);
                    for (int k = 0; k < n; k++)
                    {
                        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(c, false);
                        float sx = sc * (0.25f + (float)rng.NextDouble() * 0.2f);
                        b.transform.localPosition = new Vector3((k - n * 0.5f) * sc * 0.28f, (float)rng.NextDouble() * sc * 0.08f, 0f); b.transform.localScale = new Vector3(sx, sx * 0.5f, sx * 0.7f);
                        b.GetComponent<MeshRenderer>().sharedMaterial = m;
                    }
                    _cloudMats.Add(m);
                }
                c.gameObject.AddComponent<VillageWorld.SkyBillboard>();
                _clouds.Add(c);
            }
            // 비행기: 흰 동체 + 날개 + 꼬리날개, 창문 띠. 길이 ≈ 9 m(120 m 밖에서 손톱만 하게 보인다)
            // ★ CreateUnlit 은 곡면 셰이더가 아니라 하늘 띠 위로 벗어난다(캡처 확인) — 곡면 투명 셰이더(alpha 1)에 _CurveWeight 1
            _planeM = Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTransparent(new Color(0.62f, 0.66f, 0.78f, 1f))));   // 옅은 하늘에 묻히지 않게 회청색(DoF 로 뭉개져도 읽히게)
            var accent = Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTransparent(new Color(0.88f, 0.22f, 0.30f, 1f))));
            _plane = new GameObject("Airplane").transform; _plane.SetParent(transform, false);
            Part(_plane, PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(1.1f, 4.6f, 1.1f), Quaternion.Euler(90f, 0f, 0f), _planeM);          // 동체(+Z 앞)
            Part(_plane, PrimitiveType.Cube, new Vector3(0f, -0.15f, 0.3f), new Vector3(9.5f, 0.12f, 1.6f), Quaternion.Euler(0f, 0f, 0f), _planeM);         // 주날개
            Part(_plane, PrimitiveType.Cube, new Vector3(0f, 0.1f, -3.6f), new Vector3(3.6f, 0.1f, 1.0f), Quaternion.identity, _planeM);                   // 수평 꼬리
            Part(_plane, PrimitiveType.Cube, new Vector3(0f, 0.9f, -3.7f), new Vector3(0.12f, 1.7f, 1.2f), Quaternion.Euler(-30f, 0f, 0f), accent);        // 수직 꼬리(빨강)
            Part(_plane, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0.6f), new Vector3(1.14f, 0.22f, 2.6f), Quaternion.identity, accent);                  // 창문 띠
            for (int k = -1; k <= 1; k += 2) Part(_plane, PrimitiveType.Capsule, new Vector3(k * 2.4f, -0.55f, 0.6f), new Vector3(0.55f, 1.2f, 0.55f), Quaternion.Euler(90f, 0f, 0f), _planeM);   // 엔진
            _plane.gameObject.SetActive(false);
            // 새 떼 2: V 자 5마리 — 187차(사용자: 「새를 파이어플라이로 다시 그리고 배경과 겹치지 않게」): Firefly 갈매기 그림(Tex_Bird) 빌보드.
            // 바다 쪽 하늘(남쪽 −90°±50°)만 오가고, 겉보기 고도 −12.5/−11°(188차 캡처로 보정 — 위 버튼 줄 아래 하늘 가운데)로 두어 언덕·집과 겹치지 않게 한다
            var birdTex = ArtAssets.LoadTexture("Tex_Bird");
            _birdM = birdTex != null ? Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTexturedTransparentCurved(birdTex, Color.white), 0f))
                                     : Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTransparent(new Color(0.16f, 0.18f, 0.26f, 1f))));
            _flockA = Flock(0.2f, 70f, SkyY(70f, -12.5f), 0.10f, rng); _flockB = Flock(2.4f, 85f, SkyY(85f, -11f), -0.075f, rng);
        }


        // ── 193차(사용자: 「새들 밑에 이런 게 보인다」·「날갯짓 모션」) ──
        // 네모 한 장(투명도에 기대는 판)은 기기에 따라 알파가 빠지면 어두운 네모로 보였다 → Firefly 갈매기 그림의 윤곽(32×32 칸)대로만 면을 만든다.
        // 날개(윗부분)는 어깨선 피벗 아래로 따로 두어 세로로 뒤집히며 퍼덕인다.
        const string BirdMask = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000111000000000000000000100000000001111100000000000000011100000000011111110000000000001111000000000011111111100000000011111000000000111111111110000001111110000000001111111111111000111111100000000001111111111111001111111000000000001111111111110111111110000000000011111111111111111111000000000000011111111111111111111100000000000001111111111111111111110000000000011111111111111111111111100000000001111111111111111111111110000000011111111111111111111111100000000011111111111111111111100000000000011111111111111111000000000000001111111111111111100000000000011111111111111111110000000000111111111111111111111000000000001111111111111111111100000000000001111111111111111110000000000000000000001111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        const int BirdN = 32, BirdShoulderRow = 15;
        static Mesh _birdBody, _birdWing;
        static Mesh BirdPart(bool wing)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float ys = 0.5f - BirdShoulderRow / (float)BirdN;
            int r0 = wing ? 0 : BirdShoulderRow, r1 = wing ? BirdShoulderRow : BirdN;
            for (int gy = r0; gy < r1; gy++)
                for (int gx = 0; gx < BirdN; gx++)
                {
                    if (BirdMask[gy * BirdN + gx] != '1') continue;
                    int run = 1; while (gx + run < BirdN && BirdMask[gy * BirdN + gx + run] == '1') run++;
                    float x0 = gx / (float)BirdN - 0.5f, x1 = (gx + run) / (float)BirdN - 0.5f;
                    float yT = 0.5f - gy / (float)BirdN, yB = 0.5f - (gy + 1) / (float)BirdN;
                    float oy = wing ? -ys : 0f; int b = v.Count;
                    v.Add(new Vector3(x0, yB + oy, 0f)); v.Add(new Vector3(x1, yB + oy, 0f)); v.Add(new Vector3(x1, yT + oy, 0f)); v.Add(new Vector3(x0, yT + oy, 0f));
                    uv.Add(new Vector2(x0 + 0.5f, yB + 0.5f)); uv.Add(new Vector2(x1 + 0.5f, yB + 0.5f)); uv.Add(new Vector2(x1 + 0.5f, yT + 0.5f)); uv.Add(new Vector2(x0 + 0.5f, yT + 0.5f));
                    tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                    gx += run - 1;
                }
            var m = new Mesh { name = wing ? "BirdWing" : "BirdBody" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
        }
        /// 갈매기 한 마리(몸 + 어깨 피벗의 날개). 반환 = 빌보드로 돌릴 루트("BirdSprite")
        public static Transform MakeBird(Transform parent, Material m, float size)
        {
            if (_birdBody == null) { _birdBody = BirdPart(false); _birdWing = BirdPart(true); }
            var root = new GameObject("BirdSprite").transform; root.SetParent(parent, false); root.localScale = Vector3.one * size;
            GameObject Part(string n, Transform p, Mesh mesh) { var g = new GameObject(n, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(p, false); g.GetComponent<MeshFilter>().sharedMesh = mesh; var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return g; }
            Part("Body", root, _birdBody);
            var piv = new GameObject("WingPivot").transform; piv.SetParent(root, false); piv.localPosition = new Vector3(0f, 0.5f - BirdShoulderRow / (float)BirdN, 0f);
            Part("Wing", piv, _birdWing);
            return root;
        }
        /// 날갯짓: 위로 편 날개(1) → 아래로 접어 내림(−0.6). 가끔 글라이드(날개 편 채)
        public static void FlapBird(Transform sprite, float phase)
        {
            if (sprite == null || sprite.childCount < 2) return;
            float glide = Mathf.PerlinNoise(phase * 0.05f, Time.time * 0.25f) > 0.62f ? 1f : 0f;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 8.5f + phase);
            float y = Mathf.Lerp(Mathf.Lerp(-0.6f, 1f, k), 0.95f, glide);
            sprite.GetChild(1).localScale = new Vector3(1f, y, 1f);
        }

        static GameObject Part(Transform p, PrimitiveType t, Vector3 pos, Vector3 scale, Quaternion rot, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localRotation = rot;
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return g;
        }

        Transform Flock(float ang0, float r, float h, float speed, System.Random rng)
        {
            var f = new GameObject("Flock").transform; f.SetParent(transform, false);
            var fl = f.gameObject.AddComponent<FlockFly>(); fl.ang = ang0; fl.r = r; fl.h = h; fl.speed = speed; f.localScale = Vector3.one * 3.0f;
            for (int i = 0; i < 5; i++)
            {
                var b = new GameObject("Bird").transform; b.SetParent(f, false);
                int row = (i + 1) / 2, side = i == 0 ? 0 : (i % 2 == 0 ? 1 : -1);
                b.localPosition = new Vector3(side * row * 2.2f, -row * 0.5f, -row * 1.6f);
                MakeBird(b, _birdM, 1.0f + (float)rng.NextDouble() * 0.25f);   // 193차: 윤곽 메시 + 날개 피벗
                fl.phases.Add((float)rng.NextDouble() * 6f);
            }
            return f;
        }

        /// 170차: 하늘 돔 밤낮 틴트 — 155차의 CreateUnlit(() => SkyTint) 는 에디터 전용 추적이라 빌드·플레이 중 갱신되지 않았다(밤이 된 뒤 낮이 돼도 남색 하늘). 매 프레임 직접 넣는다.
        public class SkyDomeTint : MonoBehaviour { Material _m; void Start() { var mr = GetComponent<MeshRenderer>(); _m = mr != null ? mr.sharedMaterial : null; } void Update() { if (_m != null) SetTint(_m, VillageDayNight.SkyTint); } }

        /// 187차: 새 떼 — 세계 기준(구름 링 회전과 무관) 바다 쪽 하늘 호를 천천히 오가고, 새 한 마리 한 마리는 카메라를 보는 그림(날갯짓 = 세로 눌림)
        public class FlockFly : MonoBehaviour
        {
            public float ang, r, h, speed; public readonly List<float> phases = new List<float>();
            float _t;
            void Update()
            {
                _t += Time.deltaTime * Mathf.Abs(speed);
                float a = -Mathf.PI * 0.5f + 0.87f * Mathf.Sin(_t + ang);   // 바다(−Z) 중심 ±50°
                var par = transform.parent; var c = par != null ? par.position : Vector3.zero;
                var p = c + new Vector3(Mathf.Cos(a) * r, h + Mathf.Sin(_t * 3f) * 1.5f, Mathf.Sin(a) * r);
                var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * Mathf.Cos(_t + ang);   // 호 위 진행 방향
                transform.position = p; transform.rotation = tan.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(tan.normalized, Vector3.up) : transform.rotation;   // V 자가 진행 방향 뒤로 벌어지게
                var cam = Camera.main;
                float dir = 1f;
                if (cam != null) dir = Vector3.Dot(tan, cam.transform.right) >= 0f ? 1f : -1f;
                for (int i = 0; i < transform.childCount && i < phases.Count; i++)
                {
                    var b = transform.GetChild(i); if (b.childCount == 0) continue; var q = b.GetChild(0);
                    if (cam != null) q.rotation = Quaternion.LookRotation(q.position - cam.transform.position, Vector3.up);
                    float w = Mathf.Abs(q.localScale.x);
                    q.localScale = new Vector3(w * dir, w, 1f); FlapBird(q, phases[i]);   // 193차: 날개 퍼덕임
                }
                transform.localScale = Vector3.one * 3.0f * Mathf.Clamp01(1f - VillageDayNight.Night * 1.8f);
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            var cp = cam.transform.position; transform.position = new Vector3(cp.x, 0f, cp.z);
            float t = Time.time, night = VillageDayNight.Night;
            // 구름: 링 전체가 아주 천천히 돌고, 밤엔 어두워진다
            transform.rotation = Quaternion.Euler(0f, t * 0.25f, 0f);
            var cc = Color.Lerp(new Color(1f, 1f, 1f, 0.92f), new Color(0.32f, 0.35f, 0.50f, 0.85f), night);
            foreach (var m in _cloudMats) if (m != null) SetTint(m, cc);
            TickPlane(Time.deltaTime, night);
        }

        void TickPlane(float dt, float night)
        {
            if (_planeT < 0f)
            {
                _planeWait -= dt;
                if (_planeWait <= 0f && night < 0.5f)
                {
                    // 링(반지름 150 m) 위 호를 따라 130° 를 지나간다 — 카메라가 어느 쪽을 봐도 하늘 띠 안에 머문다
                    _planeA0 = Random.Range(0f, Mathf.PI * 2f); _planeA1 = _planeA0 + Mathf.Deg2Rad * 130f * (Random.value < 0.5f ? -1f : 1f);
                    _planeR = Random.Range(95f, 110f);
                    _planeT = 0f; _planeDur = Random.Range(48f, 62f); _plane.gameObject.SetActive(true);
                    _plane.localScale = Vector3.one * 2.3f;
                }
                return;
            }
            _planeT += dt / _planeDur;
            float a = Mathf.Lerp(_planeA0, _planeA1, _planeT), sgn = Mathf.Sign(_planeA1 - _planeA0);
            var pos = new Vector3(Mathf.Cos(a) * _planeR, SkyY(_planeR, -6.5f), Mathf.Sin(a) * _planeR);
            var fwd = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * sgn;
            _plane.localPosition = pos; _plane.localRotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 0f, sgn * 8f + Mathf.Sin(Time.time * 0.6f) * 3f);
            // 비행운: 0.35 s 마다 흰 알갱이를 뒤에 남기고 14 s 동안 커지며 옅어진다
            _trailTick -= dt;
            if (_trailTick <= 0f) { _trailTick = 0.35f; SpawnPuff(pos - fwd * 5f); }
            for (int i = _trail.Count - 1; i >= 0; i--)
            {
                _trailT[i] += dt; float k = _trailT[i] / 14f;
                if (k >= 1f || _trail[i] == null) { if (_trail[i] != null) Destroy(_trail[i].gameObject); _trail.RemoveAt(i); _trailT.RemoveAt(i); _trailM.RemoveAt(i); continue; }
                _trail[i].localScale = Vector3.one * (4f + k * 9f); var c = GetTint(_trailM[i]); c.a = 0.8f * (1f - k) * (1f - night); SetTint(_trailM[i], c);
            }
            if (_planeT >= 1f || night >= 0.75f) { _planeT = -1f; _planeWait = Random.Range(25f, 50f); _plane.gameObject.SetActive(false); }
        }

        void SpawnPuff(Vector3 localPos)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "Contrail"; g.transform.SetParent(transform, false);
            g.transform.localPosition = localPos; g.transform.localScale = Vector3.one * 4f;
            var m = Curved(CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.8f))); CoastMaterials.SetNoFog(m, 0f);
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            _trail.Add(g.transform); _trailT.Add(0f); _trailM.Add(m);
        }
    }
}
