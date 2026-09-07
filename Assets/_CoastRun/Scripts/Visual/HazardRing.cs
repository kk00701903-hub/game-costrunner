using UnityEngine;

namespace CoastRun
{
    /// 12차 시인성 규칙 ② — 피하는 것은 바닥에 붉은 경고 링을 깔고 있다.
    /// 장애물마다 발밑에 주황-빨강 링 데칼 하나. 아이템의 노란 광원과 색·형태가 겹치지 않아
    /// 멀리서도 '먹을 것/피할 것'이 한눈에 갈린다(서브웨이 서퍼의 빨간 바리케이드 규칙).
    [DisallowMultipleComponent]
    public class HazardRing : MonoBehaviour
    {
        private Transform _quad;
        private Material _mat;
        private float _phase;
        private static Texture2D _ring;

        public static readonly Color Warn = new Color(1f, 0.32f, 0.16f, 1f);

        public static HazardRing Attach(Transform host, float radius = 0.7f)
        {
            if (host == null)
                return null;
            var h = host.GetComponent<HazardRing>() ?? host.gameObject.AddComponent<HazardRing>();
            h._phase = Random.value * 6.28f;
            if (h._quad == null)
                h.Build(radius);
            else
                h._quad.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            return h;
        }

        private void Build(float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "HazardRing";
            go.transform.SetParent(transform, false);
            CoastEditUtil.DestroyCollider(go);
            _quad = go.transform;
            _quad.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _quad.localPosition = new Vector3(0f, 0.035f, 0f);
            _quad.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            _mat = CoastMaterials.CreateTexturedTransparentCurved(RingTexture(), new Color(Warn.r, Warn.g, Warn.b, 0.8f));
            _mat.renderQueue = 2955;   // 블롭 그림자(2950) 바로 위
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (_mat == null)
                return;
            // 천천히 맥동 — 정적인 데칼은 바닥 무늬로 묻힌다.
            float a = 0.62f + 0.18f * Mathf.Sin(Time.time * 3.1f + _phase);
            _mat.SetColor("_BaseColor", new Color(Warn.r, Warn.g, Warn.b, a));
        }

        /// 128² 링: 바깥 테두리 진하고 안쪽으로 옅어지는 도넛 + 얇은 중심 점.
        private static Texture2D RingTexture()
        {
            if (_ring != null)
                return _ring;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, false)
            {
                name = "HazardRing", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float band = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.82f) / 0.14f);   // 링
                float inner = (1f - Mathf.SmoothStep(0.0f, 0.78f, r)) * 0.28f;  // 안쪽 옅은 채움
                float a = Mathf.Clamp01(band * band + inner);
                if (r > 0.98f) a = 0f;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ring = tex;
            return tex;
        }
    }
}
