using UnityEngine;

namespace CoastRun
{
    /// 14차-10: 장애물이 처음 사정거리(약 26 m)에 들어올 때 한 번 '나 장애물이야' 하고 알린다 —
    /// 빨간 '!' 말풍선이 튀어 오르고, 그림이 한 번 통통 커졌다 돌아오며, 테두리가 0.5 초 빨갛게 번쩍인다.
    /// 골드런의 첫 등장 하이라이트. 한 번만.
    public class ObstacleWarning : MonoBehaviour
    {
        private const float TriggerAhead = 26f;
        private static PlayerController _player;
        private static Material _bubbleMat;
        private static Font _font;

        private bool _fired;
        private float _t = -1f;
        private Transform _bubble;
        private Renderer[] _painted;
        private MaterialPropertyBlock _mpb;
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private Vector3 _baseScale = Vector3.one;
        private Transform _visual;

        public static void Attach(GameObject root)
        {
            if (root != null && root.GetComponent<ObstacleWarning>() == null)
                root.AddComponent<ObstacleWarning>();
        }

        private void Start()
        {
            // 그림 소품(ChromaUnlit)을 찾아 둔다 — 테두리 색 번쩍임용
            var rs = GetComponentsInChildren<Renderer>();
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in rs)
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(OutlineColorId)) list.Add(r);
            _painted = list.ToArray();
            _visual = _painted.Length > 0 ? _painted[0].transform : transform;
            _baseScale = _visual.localScale;
        }

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;
            if (!_fired)
            {
                float ahead = DownhillPath.DistanceAlong(transform.position) - _player.PathDistance;
                if (ahead > TriggerAhead || ahead < 2f) return;
                _fired = true; _t = 0f;
                SpawnBubble();
            }
            if (_t < 0f) return;
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / 0.55f);
            // 통통: 0→1.22→1 (sin 반주기)
            float punch = 1f + 0.22f * Mathf.Sin(k * Mathf.PI);
            if (_visual != null) _visual.localScale = _baseScale * punch;
            // 테두리 빨강 → 원래 색
            if (_painted != null && _painted.Length > 0)
            {
                _mpb ??= new MaterialPropertyBlock();
                Color c = Color.Lerp(new Color(1f, 0.25f, 0.15f, 1f), new Color(0.10f, 0.08f, 0.16f, 1f), k);
                foreach (var r in _painted) { r.GetPropertyBlock(_mpb); _mpb.SetColor(OutlineColorId, c); r.SetPropertyBlock(_mpb); }
            }
            // 말풍선: 0.2 s 튀어 올라 0.9 s 머물다 사라진다
            if (_bubble != null)
            {
                float bt = _t;
                float rise = Mathf.Clamp01(bt / 0.2f);
                float pop = 1f + 0.35f * Mathf.Sin(rise * Mathf.PI);
                _bubble.localScale = Vector3.one * (0.55f * pop);
                _bubble.localPosition = new Vector3(0f, _bubbleBaseY + 0.25f * (1f - (1f - rise) * (1f - rise)) + Mathf.Sin(bt * 9f) * 0.03f, 0f);
                if (Camera.main != null) _bubble.rotation = Quaternion.LookRotation(_bubble.position - Camera.main.transform.position);
                if (bt > 1.1f) { Destroy(_bubble.gameObject); _bubble = null; }
            }
            if (_t > 1.2f) { _t = -1f; if (_visual != null) _visual.localScale = _baseScale; }
        }

        private float _bubbleBaseY;

        private void SpawnBubble()
        {
            // 높이: 소품 위 0.3 m
            float top = 1.0f;
            var rs = GetComponentsInChildren<Renderer>();
            foreach (var r in rs) if (r.bounds.size.y < 10f) top = Mathf.Max(top, r.bounds.max.y - transform.position.y);
            _bubbleBaseY = top + 0.35f;

            var go = new GameObject("WarnBubble");
            go.transform.SetParent(transform, false);
            _bubble = go.transform;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            disc.name = "Disc";
            disc.transform.SetParent(go.transform, false);
            Object.Destroy(disc.GetComponent<Collider>());
            _bubbleMat ??= CoastMaterials.CreateTexturedTransparentCurved(BubbleTexture(), Color.white);
            var dr = disc.GetComponent<Renderer>();
            dr.sharedMaterial = _bubbleMat;
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = Vector3.zero;
        }

        private static Texture2D _bubbleTex;
        private static Texture2D BubbleTexture()
        {
            if (_bubbleTex != null) return _bubbleTex;
            const int N = 128;
            _bubbleTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N - 0.5f, dy = (y + 0.5f) / N - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;   // 0 center .. 1 edge
                    Color c;
                    if (d > 1f) c = new Color(0, 0, 0, 0);
                    else if (d > 0.86f) c = new Color(0.12f, 0.08f, 0.14f, 1f);          // 짙은 테두리
                    else c = new Color(1f, 0.28f, 0.2f, 1f);                            // 빨강 원
                    // 느낌표: 세로 막대 + 점 (흰색)
                    float ax = Mathf.Abs(dx), ay = dy;
                    bool bar = ax < 0.07f && ay > -0.05f && ay < 0.30f;
                    bool dot = ax < 0.08f && ay > -0.30f && ay < -0.16f;
                    if (d <= 0.86f && (bar || dot)) c = Color.white;
                    _bubbleTex.SetPixel(x, y, c);
                }
            _bubbleTex.Apply();
            return _bubbleTex;
        }
    }
}
