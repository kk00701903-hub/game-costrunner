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

        public static void Show(string msg)
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
