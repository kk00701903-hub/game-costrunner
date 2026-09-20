using System.Collections;
using UnityEngine;

namespace CoastRun.Village
{
    /// 162차(사용자: 「정령 부딪히면 팡 터지게」): 마을용 가벼운 팡 — 러닝의 JuiceDirector 없이 동작.
    /// 별·하트 조각 10개(빌보드 쿼드, 회전하며 튀어 오르고 중력 낙하) + 흰 뭉게 퍼프 6개(커지며 사라짐) + 납작 링(퍼지며 사라짐). 0.55 s.
    public static class VillagePang
    {
        static Mesh _star, _heart;
        public static void Burst(Vector3 at, Color a, Color b, float scale = 1f)
        {
            var host = new GameObject("Pang"); host.transform.position = at;
            var runner = host.AddComponent<PangRunner>(); runner.StartCoroutine(runner.Run(at, a, b, scale));
        }
        class PangRunner : MonoBehaviour
        {
            public IEnumerator Run(Vector3 at, Color a, Color b, float sc)
            {
                var cam = Camera.main; var parts = new System.Collections.Generic.List<(Transform t, Vector3 v, float spin, float life, int kind, Material m, Color c)>();
                for (int i = 0; i < 10; i++)
                {
                    bool star = i % 2 == 0; var g = new GameObject(star ? "Star" : "Heart", typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(transform, false);
                    g.GetComponent<MeshFilter>().sharedMesh = star ? Star() : Heart();
                    var col = star ? a : b; var m = CoastMaterials.CreateUnlit(col); g.GetComponent<MeshRenderer>().sharedMaterial = m; g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    float ang = (i / 10f) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                    var v = new Vector3(Mathf.Cos(ang) * Random.Range(1.6f, 2.6f), Random.Range(2.4f, 3.6f), Mathf.Sin(ang) * Random.Range(1.6f, 2.6f)) * sc;
                    g.transform.position = at; g.transform.localScale = Vector3.one * Random.Range(0.16f, 0.26f) * sc;
                    parts.Add((g.transform, v, Random.Range(-540f, 540f), 0.55f, 0, m, col));
                }
                for (int i = 0; i < 6; i++)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(g.GetComponent<Collider>()); g.name = "Puff"; g.transform.SetParent(transform, false);
                    var m = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.9f)); g.GetComponent<MeshRenderer>().sharedMaterial = m; g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    float ang = (i / 6f) * Mathf.PI * 2f; var v = new Vector3(Mathf.Cos(ang), 0.4f, Mathf.Sin(ang)) * 1.4f * sc;
                    g.transform.position = at + v * 0.08f; g.transform.localScale = Vector3.one * 0.3f * sc;
                    parts.Add((g.transform, v, 0f, 0.4f, 1, m, Color.white));
                }
                var ring = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(ring.GetComponent<Collider>()); ring.transform.SetParent(transform, false);
                ring.transform.position = at; ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var rm = CoastMaterials.CreateTransparent(new Color(1f, 0.9f, 0.95f, 0.7f)); ring.GetComponent<MeshRenderer>().sharedMaterial = rm;
                CoastPrefs.VibrateEvent();
                float t = 0f;
                while (t < 0.6f)
                {
                    float dt = Time.deltaTime; t += dt;
                    foreach (var p in parts)
                    {
                        if (p.t == null) continue; float k = Mathf.Clamp01(t / p.life);
                        if (p.kind == 0)
                        {
                            var v = p.v; v.y -= 9.8f * t; p.t.position += v * dt;
                            if (cam != null) p.t.rotation = Quaternion.LookRotation(p.t.position - cam.transform.position) * Quaternion.Euler(0f, 0f, p.spin * t);
                            p.t.localScale = Vector3.one * Mathf.Lerp(p.t.localScale.x, 0f, k > 0.7f ? dt * 12f : 0f);
                        }
                        else
                        {
                            p.t.position += p.v * dt * (1f - k); p.t.localScale = Vector3.one * (0.3f + k * 0.9f) * sc;
                            if (cam != null) p.t.rotation = Quaternion.LookRotation(p.t.position - cam.transform.position);
                            p.m.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
                        }
                    }
                    float rk = Mathf.Clamp01(t / 0.35f); ring.transform.localScale = Vector3.one * (0.4f + rk * 2.6f) * sc; rm.color = new Color(1f, 0.9f, 0.95f, 0.7f * (1f - rk));
                    yield return null;
                }
                Object.Destroy(gameObject);
            }
        }
        static Mesh Star()
        {
            if (_star != null) return _star;
            var v = new Vector3[11]; v[0] = Vector3.zero;
            for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2f + i * Mathf.PI / 5f; float r = i % 2 == 0 ? 0.5f : 0.22f; v[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f); }
            var tr = new int[60]; for (int i = 0; i < 10; i++) { tr[i * 3] = 0; tr[i * 3 + 1] = 1 + (i + 1) % 10; tr[i * 3 + 2] = 1 + i; tr[30 + i * 3] = 0; tr[30 + i * 3 + 1] = 1 + i; tr[30 + i * 3 + 2] = 1 + (i + 1) % 10; }   // 양면
            _star = new Mesh { name = "PangStar", vertices = v, triangles = tr }; _star.RecalculateNormals(); _star.RecalculateBounds(); return _star;
        }
        static Mesh Heart()
        {
            if (_heart != null) return _heart;
            const int N = 24; var v = new Vector3[N + 1]; v[0] = new Vector3(0f, -0.05f, 0f);
            for (int i = 0; i < N; i++) { float t = i / (float)N * Mathf.PI * 2f; float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f); float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); v[i + 1] = new Vector3(x, y, 0f) * 0.03f; }
            var tr = new int[N * 6]; for (int i = 0; i < N; i++) { tr[i * 3] = 0; tr[i * 3 + 1] = 1 + (i + 1) % N; tr[i * 3 + 2] = 1 + i; tr[N * 3 + i * 3] = 0; tr[N * 3 + i * 3 + 1] = 1 + i; tr[N * 3 + i * 3 + 2] = 1 + (i + 1) % N; }   // 양면
            _heart = new Mesh { name = "PangHeart", vertices = v, triangles = tr }; _heart.RecalculateNormals(); _heart.RecalculateBounds(); return _heart;
        }
    }
}
