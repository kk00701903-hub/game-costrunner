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
        // 14차-14: '주인공 레인의 가장 가까운 장애물'만 경고한다. 22 m 안(피할 시간이 있을 때)에서 켜지고
        // 5 m 안이면(이미 늦음) 끈다. 레인을 바꾸면 그 레인의 것으로 바로 옮겨 간다. 한 장애물은 한 번만.
        private const float WarnFar = 26f, WarnNear = 5f;
        private const float Duration = 1.25f;
        private const float StripeW = 2.21f;   // 1.7 * 1.3
        private const float StripeLen = 9.1f;  // 7.0 * 1.3
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly System.Collections.Generic.List<ObstacleWarning> _all = new();
        private static ObstacleWarning _current;
        private int _lane;

        private void OnEnable()
        {
            _all.Add(this);
            _lane = Mathf.RoundToInt(Vector3.Dot(transform.position, DownhillPath.Rotation * Vector3.right) / 2.2f);
        }

        private void OnDisable()
        {
            _all.Remove(this);
            // Pop/Smash 로 꺼질 때 바닥에 경고 띠·! 배지가 남는 것 방지
            if (_t >= 0f || _stripe != null)
                Cancel();
            else if (_current == this)
                _current = null;
        }

        /// 프레임마다 한 번: 주인공 레인에서 앞쪽 [WarnNear, WarnFar] 안 가장 가까운, 아직 안 울린 장애물을 고른다.
        private static void Pick(PlayerController player)
        {
            if (player != null && player.IsGliding) { if (_current != null) _current.Cancel(); return; }   // 17차: 활공 중엔 경고 없음
            // 레인을 바꿨거나 이미 지나쳤으면 지금 경고를 접는다
            if (_current != null && _current._t >= 0f)
            {
                float ca = DownhillPath.DistanceAlong(_current.transform.position) - player.PathDistance;
                if (_current._lane != player.Lane || ca < WarnNear * 0.6f) _current.Cancel();
                else return;
            }
            float pz = player.PathDistance;
            ObstacleWarning best = null; float bestAhead = float.MaxValue;
            foreach (var w in _all)
            {
                if (w == null || w._fired || !w.isActiveAndEnabled) continue;
                if (!w.HasReadableVisual()) continue;   // 웅덩이·빈 루트 등 '이미지 없는' 장애물은 띠를 안 띄움
                w._lane = Mathf.RoundToInt(Vector3.Dot(w.transform.position, DownhillPath.Rotation * Vector3.right) / 2.2f);   // 차는 움직인다
                if (w._lane != player.Lane) continue;
                float ahead = DownhillPath.DistanceAlong(w.transform.position) - pz;
                // 마주 오는 차는 닫히는 속도가 빨라 더 먼 거리에서 경고
                float far = w.GetComponent<OncomingCar>() != null ? 40f : WarnFar;
                float near = w.GetComponent<OncomingCar>() != null ? 6f : WarnNear;
                if (ahead < near || ahead > far) continue;
                if (ahead < bestAhead) { bestAhead = ahead; best = w; }
            }
            if (best != null) { _current = best; best.Fire(); }
        }

        private void Cancel()
        {
            _t = -1f;
            WarnHud.Hide(transform);
            if (_stripe != null) { Destroy(_stripe.gameObject); _stripe = null; _stripeR = null; }
            if (_targets != null && _mpb != null)
                for (int i = 0; i < _targets.Length; i++)
                {
                    var r = _targets[i]; if (r == null) continue;
                    r.GetPropertyBlock(_mpb); _mpb.SetColor(BaseColorId, _baseColors[i]); _mpb.SetColor(ColorId, _baseColors[i]); r.SetPropertyBlock(_mpb);
                }
            if (_current == this) _current = null;
        }

        private void Fire()
        {
            if (!HasReadableVisual())
            {
                // 시각이 없는 채로는 띠만 뜨지 않게 — 다음에 다시 고르지 않도록 소비만 한다
                _fired = true;
                _t = -1f;
                return;
            }
            _fired = true; _t = 0f;
            WarnHud.Show(transform, _topY + 0.25f, Duration);
            SpawnStripe();
        }

        /// Painted_ 빌보드·키 큰 메시가 있어야 '장애물 이미지'로 읽힌다. 바닥 데칼(웅덩이)만 있으면 false.
        private bool HasReadableVisual()
        {
            EnsureTargets();
            if (_targets == null || _targets.Length == 0)
                return false;
            float maxH = 0f;
            float maxXZ = 0f;
            bool painted = false;
            for (int i = 0; i < _targets.Length; i++)
            {
                var r = _targets[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.name.StartsWith("Painted_")) painted = true;
                var s = r.bounds.size;
                maxH = Mathf.Max(maxH, s.y);
                maxXZ = Mathf.Max(maxXZ, s.x, s.z);
            }
            // 낮고 넓은 장애물(낙엽·자전거)도 경고 대상 — 웅덩이(데칼만)는 targets 비어서 걸러짐
            return painted || maxH >= 0.18f || maxXZ >= 0.55f;
        }

        private void EnsureTargets()
        {
            if (_targets != null && _targets.Length > 0) return;
            CacheTargets();
        }

        private void CacheTargets()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
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
            _mpb ??= new MaterialPropertyBlock();
            for (int i = 0; i < _targets.Length; i++)
            {
                var m = _targets[i].sharedMaterial;
                _baseColors[i] = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : (m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white);
                _targets[i].GetPropertyBlock(_mpb);
                var pb = _mpb.GetColor(BaseColorId);
                if (pb != default) _baseColors[i] = pb;
            }
        }

        private void Start() => CacheTargets();

        private static PlayerController _player;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Material _stripeMat;
        private static Texture2D _stripeTexPush;

        private bool _fired;
        private float _t = -1f;
        private Renderer[] _targets;
        private Color[] _baseColors;
        private MaterialPropertyBlock _mpb;
        private Transform _stripe;
        private Renderer _stripeR;
        private float _topY = 1f;

        /// 41차: 사용자 요청 — 장애물 효과(머리 위 '!' 배지·바닥 경고 띠·본체 색 펄스)를 전부 끈다.
        /// 다시 켜려면 이 플래그만 true 로. Attach 가 컴포넌트를 안 붙이므로 어디서도 다시 나오지 않는다.
        public static readonly bool Enabled = false;

        public static void Attach(GameObject root)
        {
            if (!Enabled) return;
            if (root != null && root.GetComponent<ObstacleWarning>() == null)
                root.AddComponent<ObstacleWarning>();
        }

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;
            EnsureTargets();
            if (_targets == null) return;
            _mpb ??= new MaterialPropertyBlock();
            // 첫 번째 인스턴스가 대표로 고른다(프레임당 한 번)
            if (_all.Count > 0 && _all[0] == this) Pick(_player);
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
            // 바닥 위험 띠: 깜빡이며 옆으로 밀려나고, 셰브론이 플레이어 쪽으로 흘러간다
            if (_stripe != null && _stripeR != null)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(_t * Mathf.PI * 14f);
                // 옆으로 퍼지는 경고파 — 깜빡일 때마다 폭이 넓어졌다 좁아진다
                float push = 1f + 0.28f * blink;
                _stripe.localScale = new Vector3(StripeW * push, StripeLen, 1f);
                // 셰브론이 앞으로(플레이어 쪽) 밀려나가는 UV 스크롤
                float scroll = _t * 4.2f;
                _mpb.Clear();
                _mpb.SetColor(BaseColorId, new Color(1f, 0.02f, 0.02f, (0.42f + 0.52f * blink) * (1f - k * k)));
                _mpb.SetVector(BaseMapStId, new Vector4(1f, 1f, 0f, -scroll));
                _stripeR.SetPropertyBlock(_mpb);
                if (_t >= Duration) { Destroy(_stripe.gameObject); _stripe = null; _stripeR = null; }
            }
            if (_t >= Duration) { _t = -1f; if (_current == this) _current = null; }
        }

        private void SpawnStripe()
        {
            _stripeMat ??= CoastMaterials.CreateTexturedTransparentCurved(StripeTexture(), new Color(1f, 0.02f, 0.02f, 0.6f));
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Decal_Warn";
            q.transform.SetParent(transform, false);
            Object.Destroy(q.GetComponent<Collider>());
            // 플레이어 쪽(다가오는 쪽)에 띠 — 정적 장애물은 local −Z, 마주 오는 차(180°)는 local +Z
            float towardPlayer = GetComponent<OncomingCar>() != null ? 1f : -1f;
            q.transform.localPosition = new Vector3(0f, 0.028f, towardPlayer * (StripeLen * 0.42f));
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(StripeW, StripeLen, 1f);
            _stripe = q.transform;
            _stripeR = q.GetComponent<Renderer>();
            _stripeR.sharedMaterial = _stripeMat;
            _stripeR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _stripeR.receiveShadows = false;
        }

        private static Texture2D StripeTexture()
        {
            if (_stripeTexPush != null) return _stripeTexPush;
            const int W = 48, H = 160;
            // V 반복 → 스크롤로 '신호가 밀려나가는' 느낌
            _stripeTexPush = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = y / (H - 1f);
                    float u = Mathf.Abs(x / (W - 1f) - 0.5f) * 2f;
                    // 양옆 굵은 경고선 + 바깥으로 퍼지는 페더
                    float edge = u > 0.78f ? 1f : (u > 0.55f ? 0.7f : 0.45f);
                    float sidePush = Mathf.SmoothStep(0.35f, 1f, u); // 옆으로 갈수록 진하게
                    float fade = Mathf.SmoothStep(0.05f, 1f, v);
                    // 셰브론: 플레이어 쪽(−v 스크롤)으로 화살표가 밀려감
                    float chev = Mathf.Repeat(v * 8f + (1f - u) * 0.55f, 1f);
                    float chevron = chev < 0.45f ? 1f : 0.55f;
                    float a = Mathf.Clamp01(edge * fade * chevron * (0.75f + 0.35f * sidePush));
                    _stripeTexPush.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            _stripeTexPush.Apply(false, false);
            return _stripeTexPush;
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

        public static void Hide(Transform target)
        {
            if (_inst == null) return;
            for (int i = _inst._items.Count - 1; i >= 0; i--)
                if (_inst._items[i].target == target) { if (_inst._items[i].rt != null) Destroy(_inst._items[i].rt.gameObject); _inst._items.RemoveAt(i); }
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
