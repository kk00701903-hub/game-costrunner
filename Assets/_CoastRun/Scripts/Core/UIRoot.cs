using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// DDOL transition overlay — fade, white flash, loading art (not bare black).
    /// Veil is parented to the Canvas (full bleed), not HudInset, so letterbox margins
    /// never show the live 3D world during a fade.
    public class UIRoot : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _veil;
        private Image _loaderDot;
        private CanvasGroup _veilCg;
        private Sprite _loadingSprite;
        private bool _usingArt;

        public void EnsureBuilt()
        {
            if (_canvas != null)
                return;

            _canvas = CoastUiCanvas.Create("FlowUIRoot", 500);
            DontDestroyOnLoad(_canvas.gameObject);

            // Full-screen under the canvas root — NOT under PortraitSafeArea/HudInset.
            // Inset padding + portrait letterbox left a black "card" with the run world
            // peeking around the edges (exactly the bug the loading page was meant to hide).
            var veilGo = new GameObject("Veil", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            veilGo.transform.SetParent(_canvas.transform, false);
            veilGo.transform.SetAsLastSibling();
            var rt = veilGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _veil = veilGo.GetComponent<Image>();
            _veil.color = Color.black;
            _veil.raycastTarget = true;
            _veil.preserveAspect = false;
            _veilCg = veilGo.GetComponent<CanvasGroup>();
            _veilCg.alpha = 0f;
            _veilCg.blocksRaycasts = false;

            CacheLoadingSprite();
            ApplyLoadingArt();

            var tipParent = CoastUiCanvas.Root(_canvas);
            var dotGo = new GameObject("LoaderDot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(tipParent != null ? tipParent : veilGo.transform, false);
            var drt = dotGo.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.12f);
            drt.sizeDelta = new Vector2(10f, 10f);
            _loaderDot = dotGo.GetComponent<Image>();
            _loaderDot.color = new Color(1f, 1f, 1f, 0.35f);
            _loaderDot.raycastTarget = false;
            SetLoader(false);
        }

        private void CacheLoadingSprite()
        {
            if (_loadingSprite != null) return;
            var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "UI_Loading_Mock")
                ?? Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "UI_LoadingScreen");
            if (tex != null)
                _loadingSprite = CoastUiArt.AsSprite(tex, 100f);
        }

        /// Prefer the painted loading page over a flat black rectangle.
        private void ApplyLoadingArt()
        {
            CacheLoadingSprite();
            if (_veil == null) return;
            if (_loadingSprite != null)
            {
                _veil.sprite = _loadingSprite;
                _veil.type = Image.Type.Simple;
                _veil.preserveAspect = false;
                _veil.color = Color.white;
                _usingArt = true;
            }
            else
            {
                _veil.sprite = null;
                _veil.color = Color.black;
                _usingArt = false;
            }
        }

        private void ApplySolid(Color c)
        {
            if (_veil == null) return;
            _veil.sprite = null;
            _veil.color = c;
            _usingArt = false;
        }

        /// 씬 전환 페이드 베일 알파(1=완전 가림). 챕터 시작 연출은 이게 내려간 뒤에 띄운다.
        public float VeilAlpha
        {
            get
            {
                EnsureBuilt();
                return _veilCg != null ? _veilCg.alpha : 0f;
            }
        }

        /// 18차-5: 자기 치유 — 페이드 코루틴이 중간에 끊겨 '투명한데 입력만 막는' 베일이 남지 않게,
        /// 매 프레임 알파와 레이캐스트 차단을 맞춘다(알파 1% 이하 = 통과).
        private void LateUpdate()
        {
            if (_veilCg == null) return;
            bool block = _veilCg.alpha > 0.01f;
            if (_veilCg.blocksRaycasts != block) _veilCg.blocksRaycasts = block;
            // Keep veil above any late-spawned siblings on this canvas.
            if (block && _veil != null && _veil.transform.GetSiblingIndex() != _veil.transform.parent.childCount - 1)
                _veil.transform.SetAsLastSibling();
        }

        public void SetLoader(bool on)
        {
            // Loading art already communicates progress — hide the tiny white dot when art is up.
            if (_loaderDot != null)
                _loaderDot.enabled = on && !_usingArt;
        }

        /// color null or black → loading art. Any other color → solid (e.g. white flash).
        public IEnumerator Fade(float from, float to, float duration, Color? color = null)
        {
            EnsureBuilt();
            bool useArt = !color.HasValue || IsNearBlack(color.Value);
            if (useArt) ApplyLoadingArt();
            else ApplySolid(color.Value);

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
            if (to <= 0.01f)
                ApplyLoadingArt(); // restore default look for the next cover
        }

        public IEnumerator WhiteFlash(float flashSeconds, float fadeSeconds)
        {
            EnsureBuilt();
            ApplySolid(Color.white);
            _veilCg.alpha = 1f;
            _veilCg.blocksRaycasts = true;
            float t = 0f;
            while (t < flashSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(1f, 0f, fadeSeconds, Color.white);
            ApplyLoadingArt();
        }

        public void Snap(float alpha, Color? color = null)
        {
            EnsureBuilt();
            bool useArt = !color.HasValue || IsNearBlack(color.Value);
            if (useArt) ApplyLoadingArt();
            else ApplySolid(color.Value);
            _veilCg.alpha = alpha;
            _veilCg.blocksRaycasts = alpha > 0.01f;
        }

        private static bool IsNearBlack(Color c) =>
            c.r < 0.08f && c.g < 0.08f && c.b < 0.08f && c.a > 0.5f;
    }
}
