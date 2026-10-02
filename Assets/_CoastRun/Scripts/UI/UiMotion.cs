using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 218차(사용자: 「AAA 급 모바일 게임처럼 부드럽게」): 팝업이 「툭」 나타나지 않게 — 어두운 막은 0.16 초 페이드,
    /// 카드는 92 % → 103 % → 100 % 로 살짝 튕기며 커진다(시간 멈춤에도 돌도록 unscaled).
    public class UiPop : MonoBehaviour
    {
        public RectTransform[] Targets; public float Dur = 0.22f; public Vector2 Slide;
        CanvasGroup _cg; float _t;
        public static UiPop Attach(GameObject host, float dur, Vector2 slide, params RectTransform[] targets)
        {
            var p = host.AddComponent<UiPop>(); p.Targets = targets; p.Dur = dur; p.Slide = slide; return p;
        }
        void Awake() { _cg = GetComponent<CanvasGroup>(); if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>(); _cg.alpha = 0.05f; }   // 0.02 이하면 CoastRaycastWatchdog 가 「투명 막」으로 보고 끈다
        void Start() { Apply(0f); }
        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / Dur); Apply(k);
            if (k >= 1f) enabled = false;
        }
        void Apply(float k)
        {
            _cg.alpha = Mathf.Max(0.05f, Mathf.Clamp01(k / 0.7f));
            // ease-out-back(오버슈트 1.3)
            float c1 = 1.3f, c3 = c1 + 1f, u = k - 1f; float e = 1f + c3 * u * u * u + c1 * u * u;
            float s = Mathf.LerpUnclamped(0.92f, 1f, e);
            if (Targets != null) foreach (var t in Targets) if (t != null) { t.localScale = new Vector3(s, s, 1f); }
        }
    }

    /// 218차: 누르는 동안 버튼이 살짝 눌리고(96 %) 떼면 튕겨 돌아온다 — 손맛
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float _want = 1f, _cur = 1f, _vel;
        public void OnPointerDown(PointerEventData e) { _want = 0.95f; enabled = true; }
        public void OnPointerUp(PointerEventData e) { _want = 1f; enabled = true; }
        public void OnPointerExit(PointerEventData e) { _want = 1f; enabled = true; }
        void Update()
        {
            _cur = Mathf.SmoothDamp(_cur, _want, ref _vel, 0.05f, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.localScale = new Vector3(_cur, _cur, 1f);
            if (Mathf.Abs(_cur - _want) < 0.001f && Mathf.Abs(_vel) < 0.01f) { transform.localScale = new Vector3(_want, _want, 1f); enabled = false; }
        }
    }

    /// 218차: 대화 한 줄을 글자씩 흘려 보여 준다(한 글자 0.022 초, 최대 0.9 초). Done 이 거짓일 때 탭하면 다 보여 주기만.
    public class TypeReveal : MonoBehaviour
    {
        public Text Target; string _full; float _t, _dur; public bool Done { get; private set; }
        public static TypeReveal Run(Text t, string full)
        {
            var r = t.gameObject.AddComponent<TypeReveal>(); r.Target = t; r._full = full ?? ""; r._dur = Mathf.Min(0.9f, r._full.Length * 0.022f);
            if (r._full.IndexOf('<') >= 0 || r._dur < 0.05f) r.Finish(); else t.text = "";
            return r;
        }
        public void Finish() { Done = true; if (Target != null) Target.text = _full; enabled = false; }
        void Update()
        {
            if (Done) return; _t += Time.unscaledDeltaTime;
            int n = Mathf.Clamp(Mathf.CeilToInt(_full.Length * (_t / _dur)), 0, _full.Length);
            Target.text = _full.Substring(0, n);
            if (n >= _full.Length) Finish();
        }
    }
}
