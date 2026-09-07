using UnityEngine;

namespace CoastRun
{
    /// 14차: 목표 이미지의 '살아 있는 제주 거리' — 수국 덤불, 길 건너 만국기, 바다 쪽 화단.
    /// 전부 프리미티브 + 팔레트 색이라 에셋 없이 돌고, 세그먼트당 드로우콜 30개 안쪽.
    public static class StreetDressing
    {
        private static Material _leaf, _stem, _string;
        private static Material[] _petals, _flags;

        private static void Ensure()
        {
            _leaf ??= CoastMaterials.CreateLit(() => new Color(0.30f, 0.58f, 0.30f), 0.05f);
            _stem ??= CoastMaterials.CreateLit(() => new Color(0.36f, 0.30f, 0.22f), 0.02f);
            _string ??= CoastMaterials.CreateLit(() => new Color(0.92f, 0.90f, 0.84f), 0.02f);
            _petals ??= new[]
            {
                CoastMaterials.CreateLit(() => new Color(0.48f, 0.62f, 0.95f), 0.06f),   // 하늘빛 수국
                CoastMaterials.CreateLit(() => new Color(0.66f, 0.55f, 0.92f), 0.06f),   // 보라
                CoastMaterials.CreateLit(() => new Color(0.95f, 0.66f, 0.82f), 0.06f),   // 분홍
                CoastMaterials.CreateLit(() => new Color(0.86f, 0.90f, 1.0f), 0.06f),    // 연하늘
            };
            _flags ??= new[]
            {
                CoastMaterials.CreateLit(() => new Color(1.0f, 0.55f, 0.45f), 0.05f),    // 코랄
                CoastMaterials.CreateLit(() => new Color(0.30f, 0.78f, 0.78f), 0.05f),   // 청록
                CoastMaterials.CreateLit(() => new Color(1.0f, 0.85f, 0.35f), 0.05f),    // 노랑
                CoastMaterials.CreateLit(() => new Color(0.98f, 0.97f, 0.92f), 0.05f),   // 크림
                CoastMaterials.CreateLit(() => new Color(0.55f, 0.72f, 0.98f), 0.05f),   // 하늘
            };
        }

        /// 수국 덤불: 잎 덩어리 위에 꽃송이 5~8개. radius ≈ 0.55 m.
        public static GameObject Hydrangea(Transform parent, Vector3 localPos, System.Random rng, float scale = 1f)
        {
            Ensure();
            var root = new GameObject("Hydrangea");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = Vector3.one * scale;
            root.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            Sphere(root.transform, "Leaves", new Vector3(0f, 0.32f, 0f), new Vector3(1.15f, 0.75f, 1.05f), _leaf);
            Sphere(root.transform, "Leaves2", new Vector3(0.25f, 0.28f, -0.2f), new Vector3(0.8f, 0.6f, 0.8f), _leaf);
            int n = 5 + rng.Next(4);
            int tint = rng.Next(_petals.Length);
            for (int i = 0; i < n; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 0.18f + (float)rng.NextDouble() * 0.32f;
                float h = 0.55f + (float)rng.NextDouble() * 0.22f;
                float s = 0.26f + (float)rng.NextDouble() * 0.14f;
                int m = rng.Next(4) == 0 ? rng.Next(_petals.Length) : tint;
                Sphere(root.transform, "Bloom", new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r), Vector3.one * s, _petals[m]);
            }
            return root;
        }

        /// 만국기: 두 점을 잇는 줄 + 삼각 깃발 n장, 가운데가 살짝 처진다.
        public static GameObject Bunting(Transform parent, Vector3 a, Vector3 b, System.Random rng, int flags = 11, float sag = 0.45f)
        {
            Ensure();
            var root = new GameObject("Bunting");
            root.transform.SetParent(parent, false);
            Vector3 prev = a;
            int segs = flags * 2;
            for (int i = 1; i <= segs; i++)
            {
                float t = i / (float)segs;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t));
                Vector3 mid = (prev + p) * 0.5f;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "String";
                seg.transform.SetParent(root.transform, false);
                seg.transform.localPosition = mid;
                seg.transform.localRotation = Quaternion.LookRotation(p - prev, Vector3.up);
                seg.transform.localScale = new Vector3(0.03f, 0.03f, (p - prev).magnitude + 0.01f);
                seg.GetComponent<Renderer>().sharedMaterial = _string;
                seg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                CoastEditUtil.DestroyCollider(seg);
                if (i % 2 == 0)
                {
                    // 깃발: 위가 넓고 아래로 뾰족한 얇은 상자 두 장(앞/뒤 같은 색).
                    var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    flag.name = "Flag";
                    flag.transform.SetParent(root.transform, false);
                    flag.transform.localPosition = mid + Vector3.down * 0.19f;
                    flag.transform.localRotation = Quaternion.LookRotation(p - prev, Vector3.up) * Quaternion.Euler(0f, 0f, 45f);
                    flag.transform.localScale = new Vector3(0.24f, 0.24f, 0.02f);
                    flag.GetComponent<Renderer>().sharedMaterial = _flags[rng.Next(_flags.Length)];
                    flag.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    CoastEditUtil.DestroyCollider(flag);
                }
                prev = p;
            }
            return root;
        }

        // ── 14차-2: 차양·간판 ─────────────────────────────────────────
        private static Material[] _awnings;
        private static Material _plate, _plateEdge;
        private static Font _signFont;
        private static readonly string[] SignTexts = { "귤주스", "해녀의 집", "카페 노을", "제주 흑돼지", "바다 민박", "한라봉 아이스" };
        private static int _signIx;

        private static Texture2D Stripes(Color a, Color b)
        {
            var t = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[4 * 64];
            for (int y = 0; y < 64; y++)
            {
                Color c = (y / 8) % 2 == 0 ? a : b;
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        private static void EnsureAwnings()
        {
            if (_awnings != null) return;
            var white = new Color(0.98f, 0.97f, 0.93f);
            _awnings = new[]
            {
                ArtAssets.CreateTexturedLit(Stripes(new Color(1.0f, 0.52f, 0.36f), white), Color.white, 0.05f),   // 코랄
                ArtAssets.CreateTexturedLit(Stripes(new Color(0.22f, 0.66f, 0.66f), white), Color.white, 0.05f),  // 청록
                ArtAssets.CreateTexturedLit(Stripes(new Color(1.0f, 0.80f, 0.32f), white), Color.white, 0.05f),   // 노랑
            };
            _plate = CoastMaterials.CreateLit(() => new Color(0.99f, 0.96f, 0.86f), 0.05f);
            _plateEdge = CoastMaterials.CreateLit(() => new Color(0.85f, 0.35f, 0.22f), 0.05f);
            _signFont = Resources.Load<Font>("CoastRun/Fonts/Pretendard-Bold");
        }

        /// 상가 정면 차양(줄무늬, 20° 기울기) + 그 위 간판(한글). 피벗 = 상가 lot(도로 쪽 +x).
        public static void ShopFront(Transform pivot, System.Random rng, float width = 3.0f)
        {
            EnsureAwnings();
            var awn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awn.name = "Awning";
            awn.transform.SetParent(pivot, false);
            awn.transform.localPosition = new Vector3(0.62f, 2.55f, 0f);
            awn.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            awn.transform.localScale = new Vector3(1.15f, 0.05f, width);
            CoastEditUtil.DestroyCollider(awn);
            var ar = awn.GetComponent<Renderer>();
            ar.sharedMaterial = _awnings[rng.Next(_awnings.Length)];
            var mpb = new MaterialPropertyBlock(); ar.GetPropertyBlock(mpb);
            mpb.SetVector(Shader.PropertyToID("_BaseMap_ST"), new Vector4(1f, width * 1.6f, 0f, 0f));
            ar.SetPropertyBlock(mpb);
            // 차양 앞 스캘럽(늘어진 단)
            var hem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hem.name = "AwningHem";
            hem.transform.SetParent(awn.transform, false);
            hem.transform.localPosition = new Vector3(0.5f, -1.6f, 0f);
            hem.transform.localScale = new Vector3(0.04f, 3.2f, 1f);
            CoastEditUtil.DestroyCollider(hem);
            hem.GetComponent<Renderer>().sharedMaterial = ar.sharedMaterial;

            // 간판
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Sign";
            plate.transform.SetParent(pivot, false);
            plate.transform.localPosition = new Vector3(0.14f, 3.35f, 0f);
            plate.transform.localScale = new Vector3(0.08f, 0.62f, width * 0.8f);
            CoastEditUtil.DestroyCollider(plate);
            plate.GetComponent<Renderer>().sharedMaterial = _plate;
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "SignEdge";
            edge.transform.SetParent(plate.transform, false);
            edge.transform.localPosition = new Vector3(-0.2f, 0f, 0f);
            edge.transform.localScale = new Vector3(0.9f, 1.12f, 1.06f);
            CoastEditUtil.DestroyCollider(edge);
            edge.GetComponent<Renderer>().sharedMaterial = _plateEdge;

            if (_signFont != null)
            {
                var tgo = new GameObject("SignText");
                tgo.transform.SetParent(pivot, false);
                tgo.transform.localPosition = new Vector3(0.20f, 3.35f, 0f);
                tgo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                tgo.transform.localScale = Vector3.one * 0.06f;
                var tm = tgo.AddComponent<TextMesh>();
                tm.font = _signFont;
                tm.text = SignTexts[(_signIx++) % SignTexts.Length];
                tm.fontSize = 64;
                tm.characterSize = 1f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.25f, 0.20f, 0.18f);
                var tr = tgo.GetComponent<MeshRenderer>();
                tr.sharedMaterial = _signFont.material;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// 바다 쪽 난간 앞 화단: 낮은 돌 화분 + 수국.
        public static void Planter(Transform parent, Vector3 localPos, System.Random rng)
        {
            Ensure();
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "PlanterBox";
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos + new Vector3(0f, 0.22f, 0f);
            box.transform.localScale = new Vector3(0.9f, 0.44f, 0.9f);
            box.GetComponent<Renderer>().sharedMaterial = _stem;
            CoastEditUtil.DestroyCollider(box);
            Hydrangea(parent, localPos + new Vector3(0f, 0.3f, 0f), rng, 0.75f);
        }

        private static void Sphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            CoastEditUtil.DestroyCollider(go);
        }
    }
}
