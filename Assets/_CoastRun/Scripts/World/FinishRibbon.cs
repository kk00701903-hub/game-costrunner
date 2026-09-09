using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 22차-5 / 23차-1·3: 스테이지 끝의 골인 게이트 — 높은 기둥 둘, 위에 빨간 FINISH 현수막, 가슴 높이에 **굵은 진빨강 리본**.
    /// 주인공이 지나면 리본 가운데가 끊겨 양쪽으로 펄럭이고, 뒤편 양옆에서 박수치는 관중(23차-3)이 튀어나온다.
    public class FinishRibbon : MonoBehaviour
    {
        private static FinishRibbon _current;
        private Transform _left, _right;
        private readonly List<Transform> _crowd = new List<Transform>();
        private readonly List<float> _crowdPhase = new List<float>();
        private bool _broken;
        private float _t;
        public float PathZ { get; private set; }

        public static FinishRibbon Spawn(float pathZ)
        {
            if (_current != null) Destroy(_current.gameObject);
            var go = new GameObject("FinishRibbon");
            go.transform.SetPositionAndRotation(RoadPlacement.OnRoad(pathZ, 0f), DownhillPath.Rotation);
            var fr = go.AddComponent<FinishRibbon>();
            fr.PathZ = pathZ;
            float half = PromenadeSegmentBuilder.RoadHalfWidth + 0.7f;
            const float postH = 3.4f;
            var post = CoastMaterials.CreateLit(new Color(0.97f, 0.97f, 0.99f), 0.25f);
            var red = CoastMaterials.CreateUnlit(new Color(0.93f, 0.10f, 0.16f));
            var gold = CoastMaterials.CreateLit(new Color(1f, 0.82f, 0.25f), 0.5f);
            // 기둥(굵게) + 밑동 + 꼭대기 금색 공
            foreach (float x in new[] { -half, half })
            {
                Box(go.transform, "Post", new Vector3(x, postH * 0.5f, 0f), new Vector3(0.26f, postH, 0.26f), post);
                Box(go.transform, "Base", new Vector3(x, 0.12f, 0f), new Vector3(0.7f, 0.24f, 0.7f), red);
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere); cap.name = "Cap"; cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(x, postH + 0.18f, 0f); cap.transform.localScale = Vector3.one * 0.42f;
                CoastEditUtil.DestroyCollider(cap); cap.GetComponent<Renderer>().sharedMaterial = gold;
                // 풍선 다발
                for (int i = 0; i < 4; i++)
                {
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Balloon"; b.transform.SetParent(go.transform, false);
                    float a = i * 1.7f;
                    b.transform.localPosition = new Vector3(x + Mathf.Cos(a) * 0.32f, postH + 0.55f + (i % 2) * 0.28f, Mathf.Sin(a) * 0.25f);
                    b.transform.localScale = new Vector3(0.36f, 0.44f, 0.36f);
                    CoastEditUtil.DestroyCollider(b);
                    var c = i switch { 0 => new Color(1f, 0.35f, 0.45f), 1 => new Color(1f, 0.85f, 0.3f), 2 => new Color(0.45f, 0.75f, 1f), _ => new Color(0.6f, 0.9f, 0.5f) };
                    b.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c, 0.6f);
                }
            }
            // 위 현수막: 빨간 판 + 흰 FINISH
            var banner = Box(go.transform, "Banner", new Vector3(0f, postH - 0.35f, 0f), new Vector3(half * 2f + 0.3f, 0.7f, 0.08f), red);
            Box(go.transform, "BannerTrim", new Vector3(0f, postH - 0.35f, 0f), new Vector3(half * 2f + 0.34f, 0.76f, 0.05f), CoastMaterials.CreateUnlit(Color.white));
            Label(go.transform, "FINISH", new Vector3(0f, postH - 0.35f, -0.06f), 0.52f, Quaternion.Euler(0f, 180f, 0f));
            Label(go.transform, "FINISH", new Vector3(0f, postH - 0.35f, 0.06f), 0.52f, Quaternion.identity);
            // 리본: 가슴 높이(1.25 m), 두껍게(0.5 m), 진빨강 언릿 + 흰 가장자리 줄 — 멀리서도 '빨간 띠'로 읽힌다
            var tape = CoastMaterials.CreateUnlit(Color.white);
            tape.mainTexture = RibbonTex();
            if (tape.HasProperty("_BaseMap")) tape.SetTexture("_BaseMap", RibbonTex());
            fr._left = Half(go.transform, -half, half, tape);
            fr._right = Half(go.transform, half, half, tape);
            fr.SpawnCrowd(half);
            _current = fr;
            return fr;
        }

        private static void Label(Transform parent, string text, Vector3 pos, float size, Quaternion rot)
        {
            var t = new GameObject("Label").AddComponent<TextMesh>();
            t.transform.SetParent(parent, false);
            t.transform.localPosition = pos; t.transform.localRotation = rot;
            t.text = text; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center;
            t.fontSize = 64; t.characterSize = size * 0.16f; t.color = Color.white; t.fontStyle = FontStyle.Bold;
            var r = t.GetComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Transform Half(Transform parent, float postX, float half, Material m)
        {
            var pivot = new GameObject("Half").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(postX, 1.25f, 0f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Tape"; q.transform.SetParent(pivot, false);
            CoastEditUtil.DestroyCollider(q);
            float dir = postX < 0 ? 1f : -1f;
            q.transform.localPosition = new Vector3(dir * half * 0.5f, 0f, 0f);
            q.transform.localScale = new Vector3(half, 0.5f, 1f);
            q.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 주인공(뒤에서 오는 쪽)을 향해
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var q2 = Instantiate(q, pivot); q2.transform.localRotation = Quaternion.identity;
            return pivot;
        }

        private static Texture2D _ribbon;
        private static Texture2D RibbonTex()
        {
            if (_ribbon != null) return _ribbon;
            const int w = 256, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[w * h];
            var red = new Color(0.95f, 0.08f, 0.15f); var dark = new Color(0.62f, 0.02f, 0.08f); var white = Color.white;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = red;
                    if (y < 5 || y >= h - 5) c = white;                       // 흰 가장자리
                    else if (y < 9 || y >= h - 9) c = dark;                   // 그 안쪽 어두운 선(두께감)
                    else if (((x / 32) % 2 == 0) && y > h / 2 - 3 && y < h / 2 + 3) c = Color.Lerp(red, white, 0.55f);   // 점선 무늬
                    px[y * w + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _ribbon = tex; return tex;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name; b.transform.SetParent(parent, false);
            b.transform.localPosition = pos; b.transform.localScale = size;
            CoastEditUtil.DestroyCollider(b);
            b.GetComponent<Renderer>().sharedMaterial = m;
            return b;
        }

        // 23차-3: 관중 — 결승선 뒤 양옆 인도에 박수치는 사람들(그림이 있으면 Crowd_A/B, 없으면 Tourists/Haenyeo).
        private void SpawnCrowd(float half)
        {
            string[] keys = { "Crowd_A", "Crowd_B", "Tourists", "Haenyeo" };
            var avail = new List<string>();
            foreach (var k in keys) if (PaintedProp.Available(k)) avail.Add(k);
            if (avail.Count == 0) return;
            int n = 0;
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    string key = avail[(n++) % avail.Count];
                    float x = side * (half + 1.3f + (i % 2) * 0.9f);
                    float z = 3f + i * 3.2f;
                    var root = new GameObject("Crowd").transform;
                    root.SetParent(transform, false);
                    root.localPosition = new Vector3(x, 0f, z);
                    float h = key.StartsWith("Crowd") ? 2.1f : 1.6f;
                    PaintedProp.Attach(root, key, h, replace: false);
                    root.localScale = new Vector3(1f, 0.001f, 1f);   // 리본이 끊길 때 튀어나온다
                    _crowd.Add(root); _crowdPhase.Add(Random.value * 6.28f);
                }
        }

        /// 주인공이 지나감 — 테이프가 끊긴다.
        public void Break()
        {
            if (_broken) return;
            _broken = true; _t = 0f;
            JuiceDirector.Instance?.OnLineGrab(transform.position + Vector3.up * 1.2f);
            JuiceDirector.Instance?.OnFinishConfetti(transform.position + Vector3.up * 2.6f, PromenadeSegmentBuilder.RoadHalfWidth);
        }

        private void Update()
        {
            if (!_broken) return;
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / 1.2f);
            float swing = Mathf.Sin(u * Mathf.PI * 1.5f) * (1f - u) * 40f;
            if (_left != null) _left.localRotation = Quaternion.Euler(0f, -70f * u, -35f * u + swing);
            if (_right != null) _right.localRotation = Quaternion.Euler(0f, 70f * u, 35f * u - swing);
            // 관중: 튀어나와서(0.35 s) 박수 — 위아래로 콩콩 + 살짝 좌우
            for (int i = 0; i < _crowd.Count; i++)
            {
                var c = _crowd[i]; if (c == null) continue;
                float pop = Mathf.Clamp01((_t - i * 0.05f) / 0.35f);
                float ease = 1f - Mathf.Pow(1f - pop, 3f);
                float over = 1f + Mathf.Sin(pop * Mathf.PI) * 0.18f;
                float clap = 1f + Mathf.Abs(Mathf.Sin(Time.time * 7f + _crowdPhase[i])) * 0.08f;
                c.localScale = new Vector3(1f * over, Mathf.Max(0.001f, ease * over * clap), 1f);
            }
        }
    }
}
