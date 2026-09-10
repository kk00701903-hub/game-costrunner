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
        private readonly List<GameObject> _overhead = new List<GameObject>();   // 24차-5c: 현수막·라벨 — 통과 뒤 숨김
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
            // 43차: Kling 그림 골인 게이트(Obs_FinishGate — 기둥·FINISH 현수막·풍선이 한 장, 실제 알파)가 있으면
            //       절차 기둥/현수막/풍선 대신 도로 폭에 맞춘 **고정 판(빌보드 아님, 양면)** 하나로 세운다. 리본·관중은 그대로.
            var gateTex = PaintedProp.Load("FinishGate");
            if (gateTex != null)
            {
                float gw = half * 2f + 0.6f;
                float gh = gw * gateTex.height / (float)gateTex.width;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Painted_FinishGate";
                q.transform.SetParent(go.transform, false);
                CoastEditUtil.DestroyCollider(q);
                q.transform.localPosition = new Vector3(0f, gh * 0.5f, 0f);
                q.transform.localScale = new Vector3(gw, gh, 1f);
                var shader = Shader.Find("CoastRun/ChromaUnlit") ?? CoastMaterials.UnlitShader;
                var gm = new Material(shader);
                if (gm.HasProperty("_BaseMap")) gm.SetTexture("_BaseMap", gateTex); else gm.mainTexture = gateTex;
                if (gm.HasProperty("_BaseColor")) gm.SetColor("_BaseColor", Color.white);
                if (gm.HasProperty("_KeyColor")) gm.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
                if (gm.HasProperty("_Shade")) gm.SetFloat("_Shade", 0.35f);
                var gr = q.GetComponent<Renderer>();
                gr.sharedMaterial = gm;
                gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gr.receiveShadows = false;
                fr._overhead.Add(q);   // 통과 뒤 위로 올리며 숨김(24차-5c 규칙 그대로)
                var tapeG = CoastMaterials.CreateUnlit(Color.white);
                tapeG.mainTexture = RibbonTex();
                if (tapeG.HasProperty("_BaseMap")) tapeG.SetTexture("_BaseMap", RibbonTex());
                fr._left = Half(go.transform, -half, half, tapeG);
                fr._right = Half(go.transform, half, half, tapeG);
                fr.SpawnCrowd(half);
                _current = fr;
                return fr;
            }
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
            // 위 현수막: 도로 폭에 맞춘 빨간 판 + 가운데 FINISH(가로비 맞춘 텍스처 → 글자 안 늘어남)
            float bannerW = half * 2f + 0.3f;
            float bannerH = 0.7f;
            float bannerY = postH - 0.35f;
            var bannerMat = CoastMaterials.CreateUnlit(Color.white);
            var finishMap = FinishBannerTex(bannerW / bannerH);
            if (bannerMat.HasProperty("_BaseMap")) bannerMat.SetTexture("_BaseMap", finishMap);
            else bannerMat.mainTexture = finishMap;
            var bannerFront = BannerFace(go.transform, "BannerFront", new Vector3(0f, bannerY, -0.04f), bannerW, bannerH, Quaternion.identity, bannerMat);
            var bannerBack = BannerFace(go.transform, "BannerBack", new Vector3(0f, bannerY, 0.04f), bannerW, bannerH, Quaternion.Euler(0f, 180f, 0f), bannerMat);
            var trim = Box(go.transform, "BannerTrim", new Vector3(0f, bannerY, 0f), new Vector3(bannerW + 0.04f, bannerH + 0.06f, 0.05f), CoastMaterials.CreateUnlit(Color.white));
            // 24차-5c: 골인 고정 카메라(리본 뒤 2.8 m, 높이 1.2 m)에서 머리 위 현수막 뒷면이 화면 상단을 붉게 덮었다 → 통과 0.9 s 뒤 숨김
            fr._overhead.Add(bannerFront); fr._overhead.Add(bannerBack); fr._overhead.Add(trim);
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

        private static GameObject BannerFace(Transform parent, string name, Vector3 pos, float width, float height, Quaternion rot, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos;
            q.transform.localRotation = rot;
            q.transform.localScale = new Vector3(width, height, 1f);
            CoastEditUtil.DestroyCollider(q);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q;
        }

        private static Texture2D _finishBannerTexV3;
        private static float _finishBannerAspect = -1f;

        /// 빨간 배경 + 가운데 흰 FINISH. aspect(=width/height)에 맞춰 그려 가로로 안 늘어나게.
        private static Texture2D FinishBannerTex(float aspect)
        {
            aspect = Mathf.Clamp(aspect, 2f, 16f);
            if (_finishBannerTexV3 != null && Mathf.Abs(_finishBannerAspect - aspect) < 0.05f)
                return _finishBannerTexV3;
            string[] glyphs =
            {
                "11110;10000;11110;10000;10000;10000;10000", // F
                "11111;00100;00100;00100;00100;00100;11111", // I
                "10001;11001;10101;10011;10001;10001;10001", // N
                "11111;00100;00100;00100;00100;00100;11111", // I
                "01110;10001;10000;01110;00001;10001;01110", // S
                "10001;10001;10001;11111;10001;10001;10001", // H
            };
            const int gw = 5, gh = 7, gap = 1, scale = 12;
            int textCols = glyphs.Length * gw + (glyphs.Length - 1) * gap;
            // 글자 블록이 높이의 ~70% — 양옆은 빨간 여백
            int hCells = gh + 4;
            int wCells = Mathf.Max(textCols + 8, Mathf.RoundToInt(hCells * aspect));
            int w = wCells * scale;
            int h = hCells * scale;
            int textOx = (wCells - textCols) / 2;
            int textOy = (hCells - gh) / 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "FinishBanner",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 4,
            };
            var red = new Color32(237, 26, 41, 255);
            var white = new Color32(255, 255, 255, 255);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = red;

            void Plot(int cx, int cy)
            {
                for (int oy = 0; oy < scale; oy++)
                for (int ox = 0; ox < scale; ox++)
                {
                    int x = (textOx + cx) * scale + ox;
                    int y = (textOy + cy) * scale + oy;
                    if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;
                    float nx = (ox + 0.5f) / scale * 2f - 1f;
                    float ny = (oy + 0.5f) / scale * 2f - 1f;
                    if (nx * nx + ny * ny > 1.15f) continue;
                    px[y * w + x] = white;
                }
            }

            for (int gi = 0; gi < glyphs.Length; gi++)
            {
                string[] rows = glyphs[gi].Split(';');
                int ox = gi * (gw + gap);
                for (int ry = 0; ry < gh; ry++)
                {
                    string row = rows[ry];
                    int cy = gh - 1 - ry;
                    for (int rx = 0; rx < gw; rx++)
                        if (rx < row.Length && row[rx] == '1')
                            Plot(ox + rx, cy);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            _finishBannerTexV3 = tex;
            _finishBannerAspect = aspect;
            return tex;
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
            _t += Time.unscaledDeltaTime;   // 24차-5b: 정산 UI가 timeScale 0으로 멈추면 테이프가 낙하 도중에 얼어 카메라 앞을 가렸다
            float u = Mathf.Clamp01(_t / 1.2f);
            float swing = Mathf.Sin(u * Mathf.PI * 1.5f) * (1f - u) * 40f;
            if (_left != null) _left.localRotation = Quaternion.Euler(0f, -70f * u, -35f * u + swing);
            if (_right != null) _right.localRotation = Quaternion.Euler(0f, 70f * u, 35f * u - swing);
            // 24차-5: 끊긴 테이프가 허공에 그대로 남아 고정 카메라(높이 1.2 m) 앞을 붉게 가렸다 → 흔들림이 끝나면 바닥으로 떨어져 눕는다.
            float fall = Mathf.Clamp01((_t - 0.9f) / 0.4f);
            if (fall > 0f)
            {
                if (_overhead.Count > 0)
                {
                    // 현수막은 위로 훅 올라가며 사라진다(풍선처럼) → 0.4 s 뒤 비활성
                    foreach (var o in _overhead) if (o != null) { o.transform.localPosition += Vector3.up * (Time.unscaledDeltaTime * 6f); if (fall >= 1f) o.SetActive(false); }
                    if (fall >= 1f) _overhead.Clear();
                }
                float fe = fall * fall;
                float y = Mathf.Lerp(1.25f, 0.04f, fe);
                if (_left != null) { var p = _left.localPosition; p.y = y; _left.localPosition = p; _left.localRotation = Quaternion.Euler(-88f * fe, -70f, -35f * (1f - fe)); }
                if (_right != null) { var p = _right.localPosition; p.y = y; _right.localPosition = p; _right.localRotation = Quaternion.Euler(-88f * fe, 70f, 35f * (1f - fe)); }
            }
            // 관중: 튀어나와서(0.35 s) 박수 — 위아래로 콩콩 + 살짝 좌우
            for (int i = 0; i < _crowd.Count; i++)
            {
                var c = _crowd[i]; if (c == null) continue;
                float pop = Mathf.Clamp01((_t - i * 0.05f) / 0.35f);
                float ease = 1f - Mathf.Pow(1f - pop, 3f);
                float over = 1f + Mathf.Sin(pop * Mathf.PI) * 0.18f;
                float clap = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7f + _crowdPhase[i])) * 0.08f;
                c.localScale = new Vector3(1f * over, Mathf.Max(0.001f, ease * over * clap), 1f);
            }
        }
    }
}
