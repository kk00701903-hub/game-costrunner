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
        const float CamH = 6.4f;                    // VillageHub.CamHeight 와 같은 값
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
            foreach (var n in new[] { "Cloud_Cumulus_A", "Cloud_Cumulus_B", "Cloud_Cumulus_C" }) { var t = ArtAssets.LoadTexture(n); if (t != null) painted.Add(t); }
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
            // 새 떼 2: V 자 5마리, 날개 두 장(검은 얇은 상자)이 퍼덕인다
            _birdM = Curved(CoastMaterials.SetNoFog(CoastMaterials.CreateTransparent(new Color(0.16f, 0.18f, 0.26f, 1f))));
            _flockA = Flock(0f, 65f, SkyY(65f, -9f), 0.11f, rng); _flockB = Flock(2.2f, 80f, SkyY(80f, -7f), -0.08f, rng);
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
                b.localPosition = new Vector3(side * row * 2.6f, -row * 0.35f, -row * 2.4f);
                for (int k = -1; k <= 1; k += 2)
                {
                    var w = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(b, false);
                    w.transform.localPosition = new Vector3(k * 0.9f, 0.2f, 0f); w.transform.localScale = new Vector3(1.9f, 0.10f, 0.35f);
                    var mr = w.GetComponent<MeshRenderer>(); mr.sharedMaterial = _birdM; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                }
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(b, false); body.transform.localScale = new Vector3(0.35f, 0.3f, 0.7f);
                body.GetComponent<MeshRenderer>().sharedMaterial = _birdM;
                fl.phases.Add((float)rng.NextDouble() * 6f);
            }
            return f;
        }

        /// 170차: 하늘 돔 밤낮 틴트 — 155차의 CreateUnlit(() => SkyTint) 는 에디터 전용 추적이라 빌드·플레이 중 갱신되지 않았다(밤이 된 뒤 낮이 돼도 남색 하늘). 매 프레임 직접 넣는다.
        public class SkyDomeTint : MonoBehaviour { Material _m; void Start() { var mr = GetComponent<MeshRenderer>(); _m = mr != null ? mr.sharedMaterial : null; } void Update() { if (_m != null) SetTint(_m, VillageDayNight.SkyTint); } }

        public class FlockFly : MonoBehaviour
        {
            public float ang, r, h, speed; public readonly List<float> phases = new List<float>();
            void Update()
            {
                ang += speed * Time.deltaTime;
                var p = new Vector3(Mathf.Cos(ang) * r, h + Mathf.Sin(ang * 3f) * 2f, Mathf.Sin(ang) * r);
                var fwd = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * Mathf.Sign(speed);
                transform.localPosition = p; transform.localRotation = Quaternion.LookRotation(fwd, Vector3.up);
                for (int i = 0; i < transform.childCount && i < phases.Count; i++)
                {
                    var b = transform.GetChild(i); float flap = Mathf.Sin(Time.time * 7.5f + phases[i]) * 32f;
                    for (int k = 0; k < 2 && k < b.childCount; k++) b.GetChild(k).localRotation = Quaternion.Euler(0f, 0f, (k == 0 ? -1f : 1f) * (18f + flap));
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
