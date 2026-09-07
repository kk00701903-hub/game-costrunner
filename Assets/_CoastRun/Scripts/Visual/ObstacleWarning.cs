using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 14차-12: 장애물 첫 등장 경고 — 다른 러너들이 쓰는 두 가지를 합쳤다.
    ///  1) 화면 배지(소닉 대시·미니언 러시): 장애물 머리 위에 항상 같은 크기(64px)로 뜨는 빨간 '!' 원.
    ///     거리와 무관하게 크기가 같아서 멀리서도 확실히 보인다. 1.1 초 뒤 페이드.
    ///  2) 바닥 위험 띠(템플런 2·탈주 러너): 장애물 앞 7 m 레인에 빨간 반투명 띠가 깜빡인다 — "이 레인 피해".
    ///  본체 색 반짝임은 약하게 남긴다.
    public class ObstacleWarning : MonoBehaviour
    {
        private const float TriggerAhead = 30f;
        private const float Duration = 1.15f;
        private static PlayerController _player;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Material _stripeMat;

        private bool _fired;
        private float _t = -1f;
        private Renderer[] _targets;
        private Color[] _baseColors;
        private MaterialPropertyBlock _mpb;
        private Transform _stripe;
        private Renderer _stripeR;
        private float _topY = 1f;

        public static void Attach(GameObject root)
        {
            if (root != null && root.GetComponent<ObstacleWarning>() == null)
                root.AddComponent<ObstacleWarning>();
        }

        private void Start()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r == null || r.sharedMaterial == null) continue;
                string n = r.gameObject.name;
                if (n == "BlobShadow" || n == "HazardRing" || n == "PickupGlow" || n == "Outline" || n == "Ink" || n == "Painted_Back" || n.StartsWith("Decal_")) continue;
                if (r is ParticleSystemRenderer) continue;
                list.Add(r);
                if (r.bounds.size.y < 10f) _topY = Mathf.Max(_topY, r.bounds.max.y - transform.position.y);
            }
            _targets = list.ToArray();
            _baseColors = new Color[_targets.Length];
            _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _targets.Length; i++)
            {
                var m = _targets[i].sharedMaterial;
                _baseColors[i] = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : (m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white);
                _targets[i].GetPropertyBlock(_mpb);
                var pb = _mpb.GetColor(BaseColorId);
                if (pb != default) _baseColors[i] = pb;
            }
        }

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null || _targets == null) return;
            _mpb ??= new MaterialPropertyBlock();
            if (!_fired)
            {
                float ahead = DownhillPath.DistanceAlong(transform.position) - _player.PathDistance;
                if (ahead > TriggerAhead || ahead < 3f) return;
                _fired = true; _t = 0f;
                WarnHud.Show(transform, _topY + 0.25f, Duration);
                SpawnStripe();
            }
            if (_t < 0f) return;
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Duration);
            // 본체: 두 번 살짝 밝아진다(약하게)
            float pulse = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * (1f - k);
            for (int i = 0; i < _targets.Length; i++)
            {
                var r = _targets[i]; if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                Color c = _t >= Duration ? _baseColors[i] : Color.Lerp(_baseColors[i], new Color(1.35f, 0.75f, 0.7f, 1f) * _baseColors[i] + new Color(0.25f, 0.05f, 0.05f, 0f), pulse * 0.7f);
                _mpb.SetColor(BaseColorId, c); _mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(_mpb);
            }
            // 바닥 띠: 3번 깜빡이며 사라진다
            if (_stripeR != null)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(k * Mathf.PI * 6f);
                var sc = _stripeR.sharedMaterial.HasProperty(BaseColorId) ? _stripeR.sharedMaterial.GetColor(BaseColorId) : Color.red;
                _mpb.Clear();
                _mpb.SetColor(BaseColorId, new Color(1f, 0.25f, 0.15f, (0.22f + 0.30f * blink) * (1f - k * k)));
                _stripeR.SetPropertyBlock(_mpb);
                if (_t >= Duration) { Destroy(_stripe.gameObject); _stripe = null; _stripeR = null; }
            }
            if (_t >= Duration) { _t = -1f; enabled = false; }
        }

        private void SpawnStripe()
        {
            _stripeMat ??= CoastMaterials.CreateTexturedTransparentCurved(StripeTexture(), new Color(1f, 0.25f, 0.15f, 0.4f));
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Decal_Warn";
            q.transform.SetParent(transform, false);
            Object.Destroy(q.GetComponent<Collider>());
            // 장애물 앞(플레이어 쪽, −z) 7 m 띠. Quad 법선 −Z → X축 −90° 로 눕히면 +Y 를 본다.
            q.transform.localPosition = new Vector3(0f, 0.025f, -3.9f);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(1.7f, 7.0f, 1f);
            _stripe = q.transform;
            _stripeR = q.GetComponent<Renderer>();
            _stripeR.sharedMaterial = _stripeMat;
            _stripeR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _stripeR.receiveShadows = false;
        }

        private static Texture2D _stripeTex;
        private static Texture2D StripeTexture()
        {
            if (_stripeTex != null) return _stripeTex;
            const int W = 32, H = 128;
            _stripeTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = y / (H - 1f);                       // 0 = 플레이어 쪽 끝, 1 = 장애물 쪽
                    float u = Mathf.Abs(x / (W - 1f) - 0.5f) * 2f; // 0 중앙 .. 1 가장자리
                    float edge = u > 0.82f ? 1f : 0.55f;           // 양옆 진한 선
                    float fade = Mathf.SmoothStep(0f, 1f, v);      // 장애물 쪽으로 갈수록 진하게
                    float chevron = (Mathf.Repeat(v * 6f + (1f - u) * 0.5f, 1f) < 0.5f) ? 1f : 0.75f;   // 셰브론 무늬
                    _stripeTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(edge * fade * chevron)));
                }
            _stripeTex.Apply();
            return _stripeTex;
        }
    }

    /// 화면 공간 '!' 배지 — 장애물 머리 위에 같은 크기로 뜬다(멀어도 작아지지 않는다).
    public class WarnHud : MonoBehaviour
    {
        private static WarnHud _inst;
        private Canvas _canvas;
        private Sprite _badge;
        private readonly System.Collections.Generic.List<(RectTransform rt, Image img, Transform target, float lift, float t, float dur)> _items = new();

        public static void Show(Transform target, float lift, float duration)
        {
            if (_inst == null)
            {
                var go = new GameObject("WarnHud");
                _inst = go.AddComponent<WarnHud>();
                _inst._canvas = CoastUiCanvas.Create("WarnHudCanvas", 90, go.transform);
                _inst._badge = MakeBadge();
            }
            var root = CoastUiCanvas.Root(_inst._canvas);
            var bgo = new GameObject("Warn", typeof(RectTransform), typeof(Image));
            bgo.transform.SetParent(root, false);
            var rt = bgo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(72f, 72f);
            var img = bgo.GetComponent<Image>(); img.sprite = _inst._badge; img.raycastTarget = false;
            _inst._items.Add((rt, img, target, lift, 0f, duration));
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            var root = CoastUiCanvas.Root(_canvas);
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var it = _items[i];
                it.t += Time.deltaTime;
                if (it.target == null || it.t >= it.dur || cam == null)
                {
                    if (it.rt != null) Destroy(it.rt.gameObject);
                    _items.RemoveAt(i); continue;
                }
                Vector3 sp = cam.WorldToScreenPoint(it.target.position + Vector3.up * it.lift);
                if (sp.z < 0f) { it.img.enabled = false; _items[i] = it; continue; }
                it.img.enabled = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out var lp);
                // 루트는 인셋(HudPad) 이라 앵커 0,0 기준 좌표로 변환
                it.rt.anchoredPosition = lp + root.rect.size * 0.5f;
                float k = it.t / it.dur;
                float pop = k < 0.15f ? Mathf.SmoothStep(0.2f, 1.25f, k / 0.15f) : 1.25f - 0.25f * Mathf.SmoothStep(0f, 1f, (k - 0.15f) / 0.25f);
                float bob = 1f + 0.08f * Mathf.Sin(it.t * 14f);
                it.rt.localScale = Vector3.one * pop * bob;
                var c = it.img.color; c.a = k > 0.75f ? 1f - (k - 0.75f) / 0.25f : 1f; it.img.color = c;
                _items[i] = it;
            }
        }

        private static Sprite MakeBadge()
        {
            const int N = 96;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N - 0.5f, dy = (y + 0.5f) / N - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    Color c;
                    if (d > 1f) c = new Color(0, 0, 0, 0);
                    else if (d > 0.90f) c = new Color(0.10f, 0.06f, 0.12f, 1f);
                    else if (d > 0.80f) c = Color.white;
                    else c = new Color(1f, 0.22f, 0.16f, 1f);
                    bool bar = Mathf.Abs(dx) < 0.075f && dy > -0.06f && dy < 0.30f;
                    bool dot = Mathf.Abs(dx) < 0.085f && dy > -0.32f && dy < -0.17f;
                    if (d <= 0.80f && (bar || dot)) c = Color.white;
                    // 원 가장자리 부드럽게
                    if (d > 0.97f) c.a *= Mathf.Clamp01((1f - d) / 0.03f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
