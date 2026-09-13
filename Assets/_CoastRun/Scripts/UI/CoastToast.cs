using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 씬과 무관한 상단 토스트(업적·기록 갱신). 여러 개면 줄을 서서 차례로.
    public class CoastToast : MonoBehaviour
    {
        static CoastToast _i;
        readonly Queue<string> _q = new Queue<string>();
        Canvas _canvas; Image _panel; Text _text; bool _showing;

        /// 74차(사용자): 러닝 중(HUD 가 있을 때)엔 위 띠 대신 **분홍 팝 텍스트**(「+2」처럼 화면 가운데서 터져 위로 떠오른다)로 뿌린다 — 「포토카드 전부 모았어」 같은 미션 메시지.
        public static void Show(string msg)
        {
            if (RunHudChrome.Instance != null) { Pop(msg); return; }
            ShowBar(msg);
        }

        /// 분홍 팝 텍스트 — 가운데 위(0.62)에서 0.6 → 1.15 → 1.0 배로 터지고 1.6초 뒤 위로 떠오르며 사라진다. 여러 개면 살짝 아래로 겹쳐 쌓인다.
        public static void Pop(string msg)
        {
            if (_i == null)
            {
                var go = new GameObject("CoastToast");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<CoastToast>();
            }
            _i.StartCoroutine(_i.PopCo(msg));
        }

        private int _popStack;
        IEnumerator PopCo(string msg)
        {
            if (_canvas == null) Build();
            var root = CoastUiCanvas.Root(_canvas);
            var go = new GameObject("Pop", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f); rt.sizeDelta = new Vector2(640f, 120f);
            float yOff = -_popStack * 70f; _popStack++;
            var cg = go.GetComponent<CanvasGroup>();
            var t = CoastHudLayout.MakeText(rt, "T", msg, 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = new Color(1f, 0.36f, 0.66f); t.fontStyle = FontStyle.Bold; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 18; t.resizeTextMaxSize = CoastHudLayout.Scaled(34);
            CoastUiArt.OutlineText(t, Color.white, 2.5f);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0.35f, 0.05f, 0.20f, 0.6f); sh.effectDistance = new Vector2(3f, -4f);
            float k = 0f;
            while (k < 0.35f)
            {
                k += Time.unscaledDeltaTime; float u = Mathf.Clamp01(k / 0.35f);
                float sc = u < 0.6f ? Mathf.Lerp(0.6f, 1.15f, u / 0.6f) : Mathf.Lerp(1.15f, 1f, (u - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * sc; rt.anchoredPosition = new Vector2(0f, yOff); cg.alpha = Mathf.Clamp01(u * 3f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            float hold = 0f;
            while (hold < 1.6f) { hold += Time.unscaledDeltaTime; rt.anchoredPosition = new Vector2(Mathf.Sin(hold * 6f) * 2f, yOff + Mathf.Sin(hold * 2.5f) * 4f); yield return null; }
            k = 0f;
            while (k < 0.5f) { k += Time.unscaledDeltaTime; float u = k / 0.5f; rt.anchoredPosition = new Vector2(0f, yOff + u * 90f); cg.alpha = 1f - u; yield return null; }
            _popStack = Mathf.Max(0, _popStack - 1);
            Destroy(go);
        }

        /// 위 띠 토스트(육성·타이틀 등 러닝 밖).
        public static void ShowBar(string msg)
        {
            if (_i == null)
            {
                var go = new GameObject("CoastToast");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<CoastToast>();
            }
            _i._q.Enqueue(msg);
            if (!_i._showing) _i.StartCoroutine(_i.Pump());
        }

        void Build()
        {
            _canvas = CoastUiCanvas.Create("ToastCanvas", 900);
            DontDestroyOnLoad(_canvas.gameObject);
            var root = CoastUiCanvas.Root(_canvas);
            _panel = CoastUiArt.Panel(root, "Toast", new Color(0.05f, 0.04f, 0.07f, 0.85f), 16);
            var rt = _panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -70f); rt.sizeDelta = new Vector2(640f, 52f);
            _panel.raycastTarget = false;
            var edge = CoastUiArt.Panel(_panel.transform, "Edge", new Color(0.83f, 0.69f, 0.22f, 0.6f), 16);
            CoastOrnate.Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
            var inner = CoastUiArt.Panel(edge.transform, "Inner", new Color(0.05f, 0.04f, 0.07f, 0.9f), 14);
            CoastOrnate.Stretch(inner.rectTransform, 2f, 2f, -2f, -2f);
            edge.raycastTarget = false; inner.raycastTarget = false;
            _text = CoastOrnate.Label(_panel.transform, "T", "", 16, CoastOrnate.Ivory);
            CoastOrnate.Stretch(_text.rectTransform, 16f, 4f, -16f, -4f);
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _panel.gameObject.SetActive(false);
        }

        IEnumerator Pump()
        {
            _showing = true;
            if (_canvas == null) Build();
            while (_q.Count > 0)
            {
                _text.text = _q.Dequeue();
                _panel.gameObject.SetActive(true);
                var rt = _panel.rectTransform;
                float t = 0f;
                while (t < 0.25f) { t += Time.unscaledDeltaTime; rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(40f, -70f, t / 0.25f)); yield return null; }
                rt.anchoredPosition = new Vector2(0f, -70f);
                yield return new WaitForSecondsRealtime(2.4f);
                t = 0f;
                while (t < 0.2f) { t += Time.unscaledDeltaTime; rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-70f, 40f, t / 0.2f)); yield return null; }
                _panel.gameObject.SetActive(false);
            }
            _showing = false;
        }
    }
}
