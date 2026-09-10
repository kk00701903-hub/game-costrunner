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
        private float _radius = 0.7f;
        private static Texture2D _ring;

        public static readonly Color Warn = new Color(1f, 0.02f, 0.02f, 1f);   // 순수에 가까운 경고 빨강
        /// 38차: 아이템(물약·별·하트) 발밑 파란 링
        public static readonly Color Item = new Color(0.25f, 0.62f, 1f, 1f);
        private Color _color = Warn;

        /// 38차: color 를 주면 그 색(아이템=파랑), groundY 를 주면 링을 그 월드 높이에 깐다(떠 있는 아이템용).
        public static HazardRing Attach(Transform host, float radius = 0.7f, Color? color = null, float? groundY = null)
        {
            if (host == null)
                return null;
            var h = host.GetComponent<HazardRing>() ?? host.gameObject.AddComponent<HazardRing>();
            h._phase = Random.value * 6.28f;
            h._color = color ?? Warn;
            if (!color.HasValue) radius *= 1.25f;   // 38차: 장애물 링은 더 크게
            h._radius = Mathf.Max(0.35f, radius);
            if (h._quad == null)
                h.Build(h._radius);
            else
                h._quad.localScale = new Vector3(h._radius * 2f, h._radius * 2f, 1f);
            if (groundY.HasValue && h._quad != null)
                h._quad.position = new Vector3(host.position.x, groundY.Value + 0.038f, host.position.z);
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
            _quad.localPosition = new Vector3(0f, 0.038f, 0f);
            _quad.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            // 인스턴스 머티리얼 — 깜빡임이 다른 링과 섞이지 않게
            _mat = CoastMaterials.CreateTexturedTransparentCurved(RingTexture(), new Color(_color.r, _color.g, _color.b, 0.85f));
            _mat.renderQueue = 2955;   // 블롭 그림자(2950) 바로 위
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (_quad == null)
                return;

            // 도로에 붙이기 — 부모(차·허들)가 떠 있어도 경고등은 발밑에
            float gy = DownhillPath.Point(DownhillPath.DistanceAlong(transform.position)).y;
            _quad.SetPositionAndRotation(
                new Vector3(transform.position.x, gy + 0.038f, transform.position.z),
                Quaternion.Euler(90f, transform.eulerAngles.y, 0f));
            _quad.localScale = new Vector3(_radius * 2f, _radius * 2f, 1f);

            if (_mat == null)
                return;
            // ~4회/초, 켜짐↔거의 꺼짐 대비를 키워 멀리서도 경고가 읽히게.
            float w = 0.5f + 0.5f * Mathf.Sin(Time.time * 25f + _phase);
            float a = Mathf.Lerp(0.18f, 1f, w * w);   // 제곱으로 켜짐이 짧게·또렷하게
            _mat.SetColor("_BaseColor", new Color(_color.r, _color.g, _color.b, a));
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
                // 38차: 굵은 선(폭 0.22)의 또렷한 동그라미 + 아주 옅은 안쪽 채움
                float band = Mathf.Clamp01((0.11f - Mathf.Abs(r - 0.80f)) / 0.025f);   // 링 (r 0.69~0.91, 가장자리만 부드럽게)
                float inner = (1f - Mathf.SmoothStep(0.0f, 0.7f, r)) * 0.10f;
                float a = Mathf.Clamp01(band + inner);
                if (r > 0.95f) a = 0f;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ring = tex;
            return tex;
        }
    }
}
