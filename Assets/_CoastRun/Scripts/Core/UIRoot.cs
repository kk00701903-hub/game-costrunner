using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// DDOL transition overlay — fade, white flash, minimal loader (dot only).
    public class UIRoot : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _veil;
        private Image _loaderDot;
        private CanvasGroup _veilCg;

        public void EnsureBuilt()
        {
            if (_canvas != null)
                return;

            _canvas = CoastUiCanvas.Create("FlowUIRoot", 500);
            DontDestroyOnLoad(_canvas.gameObject);

            var veilGo = new GameObject("Veil", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            veilGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rt = veilGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _veil = veilGo.GetComponent<Image>();
            _veil.color = Color.black;
            _veil.raycastTarget = true;
            _veilCg = veilGo.GetComponent<CanvasGroup>();
            _veilCg.alpha = 1f;
            _veilCg.blocksRaycasts = true;

            var dotGo = new GameObject("LoaderDot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(veilGo.transform, false);
            var drt = dotGo.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.12f);
            drt.sizeDelta = new Vector2(10f, 10f);
            _loaderDot = dotGo.GetComponent<Image>();
            _loaderDot.color = new Color(1f, 1f, 1f, 0.35f);
            _loaderDot.raycastTarget = false;
            SetLoader(false);
        }

        public void SetLoader(bool on)
        {
            if (_loaderDot != null)
                _loaderDot.enabled = on;
        }

        public IEnumerator Fade(float from, float to, float duration, Color? color = null)
        {
            EnsureBuilt();
            if (color.HasValue)
                _veil.color = color.Value;
            _veilCg.blocksRaycasts = true;
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                _veilCg.alpha = Mathf.Lerp(from, to, u);
                yield return null;
            }

            _veilCg.alpha = to;
            _veilCg.blocksRaycasts = to > 0.01f;
        }

        public IEnumerator WhiteFlash(float flashSeconds, float fadeSeconds)
        {
            EnsureBuilt();
            _veil.color = Color.white;
            _veilCg.alpha = 1f;
            _veilCg.blocksRaycasts = true;
            float t = 0f;
            while (t < flashSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(1f, 0f, fadeSeconds, Color.white);
            _veil.color = Color.black;
        }

        public void Snap(float alpha, Color? color = null)
        {
            EnsureBuilt();
            if (color.HasValue)
                _veil.color = color.Value;
            _veilCg.alpha = alpha;
            _veilCg.blocksRaycasts = alpha > 0.01f;
        }
    }
}
