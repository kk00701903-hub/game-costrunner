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
