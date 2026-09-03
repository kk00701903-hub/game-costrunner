using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Lightweight tween — swap internals for DOTween when the package is added.
    public static class SimpleTween
    {
        public static IEnumerator MoveFade(RectTransform rt, CanvasGroup cg, Vector2 from, Vector2 to,
            float fromAlpha, float toAlpha, float duration, AnimationCurve curve = null)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                if (curve != null)
                    u = curve.Evaluate(u);
                else
                    u = u * u * (3f - 2f * u);

                if (rt != null)
                    rt.anchoredPosition = Vector2.LerpUnclamped(from, to, u);
                if (cg != null)
                    cg.alpha = Mathf.Lerp(fromAlpha, toAlpha, u);
                yield return null;
            }

            if (rt != null)
                rt.anchoredPosition = to;
            if (cg != null)
                cg.alpha = toAlpha;
        }

        public static IEnumerator PunchScale(Transform t, float punch, float duration)
        {
            if (t == null)
                yield break;

            Vector3 baseScale = t.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = elapsed / duration;
                float s = 1f + Mathf.Sin(u * Mathf.PI) * punch;
                t.localScale = baseScale * s;
                yield return null;
            }

            t.localScale = baseScale;
        }
    }

    /// Object-pooled world/screen floating "+N" rewards.
    public class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private int prewarm = 12;
        [SerializeField] private Font font;

        private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();
        private Canvas _canvas;
        private Camera _cam;

        public void Init(Canvas canvas, Camera cam)
        {
            _canvas = canvas;
            _cam = cam;
            for (int i = 0; i < prewarm; i++)
                _pool.Enqueue(CreateItem());
        }

        public void Show(Vector3 worldPos, string text, Color color)
        {
            var item = _pool.Count > 0 ? _pool.Dequeue() : CreateItem();
            item.Play(worldPos, text, color, _cam, () => _pool.Enqueue(item));
        }

        private FloatingText CreateItem()
        {
            var go = new GameObject("FloatText");
            go.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 48f);
            var cg = go.AddComponent<CanvasGroup>();
            var label = go.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 36;
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            if (font != null)
                label.font = font;
            else
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var item = go.AddComponent<FloatingText>();
            item.Bind(rt, cg, label);
            go.SetActive(false);
            return item;
        }

        private sealed class FloatingText : MonoBehaviour
        {
            private RectTransform _rt;
            private CanvasGroup _cg;
            private Text _label;
            private System.Action _onDone;

            public void Bind(RectTransform rt, CanvasGroup cg, Text label)
            {
                _rt = rt;
                _cg = cg;
                _label = label;
            }

            public void Play(Vector3 worldPos, string text, Color color, Camera cam, System.Action onDone)
            {
                _onDone = onDone;
                _label.text = text;
                _label.color = color;
                gameObject.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(Animate(worldPos, cam));
            }

            private IEnumerator Animate(Vector3 worldPos, Camera cam)
            {
                if (cam == null)
                    cam = Camera.main;

                Vector2 screen = cam != null
                    ? (Vector2)cam.WorldToScreenPoint(worldPos)
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rt.parent as RectTransform, screen, null, out Vector2 local);

                Vector2 from = local;
                Vector2 to = local + new Vector2(Random.Range(-24f, 24f), 90f);
                _rt.anchoredPosition = from;
                _cg.alpha = 1f;

                yield return SimpleTween.MoveFade(_rt, _cg, from, to, 1f, 0f, 0.85f);
                gameObject.SetActive(false);
                _onDone?.Invoke();
            }
        }
    }

    /// Garmin-style lap / upgrade notification strip.
    public class WatchLapPopup : MonoBehaviour
    {
        private RectTransform _root;
        private CanvasGroup _cg;
        private Text _title;
        private Text _body;
        private Coroutine _routine;

        public void Build(Transform parent)
        {
            var go = new GameObject("WatchLapPopup", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _root = go.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, -48f);
            _root.sizeDelta = new Vector2(420f, 110f);

            _cg = go.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_root, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var img = bg.GetComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.16f, 0.92f);
            img.raycastTarget = false;
            Texture2D frame = ArtAssets.LoadTexture("Watch_Frame");
            if (frame != null)
            {
                img.sprite = Sprite.Create(frame, new Rect(0, 0, frame.width, frame.height),
                    new Vector2(0.5f, 0.5f), 100f);
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            _title = CreateLabel(_root, "Title", new Vector2(0f, -14f), 22, FontStyle.Bold,
                new Color(0.45f, 0.95f, 0.55f));
            _body = CreateLabel(_root, "Body", new Vector2(0f, -52f), 18, FontStyle.Normal, Color.white);

            go.SetActive(false);
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 pos, int size,
            FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 36f);
            var text = go.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        public void Show(string title, string body, float holdSeconds = 1.6f)
        {
            if (_root == null)
                return;

            gameObject.SetActive(true);
            _root.gameObject.SetActive(true);
            _title.text = title;
            _body.text = body;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(Run(holdSeconds));
        }

        private IEnumerator Run(float hold)
        {
            Vector2 shown = new Vector2(0f, -48f);
            Vector2 hidden = new Vector2(0f, 40f);
            _root.anchoredPosition = hidden;
            _cg.alpha = 0f;

            yield return SimpleTween.MoveFade(_root, _cg, hidden, shown, 0f, 1f, 0.28f);
            yield return SimpleTween.PunchScale(_root, 0.06f, 0.22f);
            yield return new WaitForSecondsRealtime(hold);
            yield return SimpleTween.MoveFade(_root, _cg, shown, hidden, 1f, 0f, 0.35f);
            _root.gameObject.SetActive(false);
        }
    }

    /// HUD feedback hub: floating rewards + smartwatch upgrade / destination alerts.
    public class UI_FeedbackController : MonoBehaviour
    {
        [SerializeField] private FloatingTextPool floatPool;
        [SerializeField] private WatchLapPopup watchPopup;
        [SerializeField] private Text coinHud;
        [SerializeField] private Color rewardColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color comboColor = new Color(0.45f, 1f, 0.7f);

        private CoinWallet _wallet;
        private Canvas _canvas;
        private bool _coinDriveExternal;
        private GameObject _coinHudRoot;
        private CanvasGroup _coinCg;

        public CanvasGroup CoinCanvasGroup => _coinCg;

        public void SetCoinDriveExternal(bool external)
        {
            _coinDriveExternal = external;
        }

        public void SetDisplayedCoins(int amount)
        {
            if (coinHud == null)
                return;
            coinHud.text = amount.ToString();
        }

        /// Remove endless-runner chrome (pause/settings/top bar) from the run HUD.
        public void StripRunChrome()
        {
            EnsureCanvas();
            var root = CoastUiCanvas.Root(_canvas);
            var top = root.Find("TopBar");
            if (top != null)
            {
                // Keep coin child if present; remove opaque bar visuals & buttons.
                var pause = top.Find("Pause");
                if (pause != null)
                    Destroy(pause.gameObject);
                var settings = top.Find("Settings");
                if (settings != null)
                    Destroy(settings.gameObject);
                var img = top.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0f, 0f, 0f, 0f);
                var topRt = top as RectTransform;
                if (topRt != null)
                    topRt.sizeDelta = new Vector2(0f, 72f);
            }
        }

        public void BuildRuntime(CoinWallet wallet)
        {
            _wallet = wallet;
            EnsureCanvas();
            if (floatPool == null)
            {
                var poolGo = new GameObject("FloatingTextPool");
                poolGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
                floatPool = poolGo.AddComponent<FloatingTextPool>();
                floatPool.Init(_canvas, Camera.main);
            }

            if (watchPopup == null)
            {
                watchPopup = gameObject.AddComponent<WatchLapPopup>();
                watchPopup.Build(CoastUiCanvas.Root(_canvas));
            }

            BuildCoinHud();
            if (_wallet != null)
                _wallet.OnCoinsChanged += HandleCoins;
            RefreshCoinHud();
        }

        private void OnDestroy()
        {
            if (_wallet != null)
                _wallet.OnCoinsChanged -= HandleCoins;
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
                return;

            _canvas = CoastUiCanvas.Create("CoastRunHUD", 100);
        }

        private void BuildCoinHud()
        {
            if (coinHud != null)
                return;

            EnsureCanvas();
            // Floating top-left — no navy bottom/upgrade chrome.
            var go = new GameObject("CoinHud", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            _coinHudRoot = go;
            _coinCg = go.GetComponent<CanvasGroup>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -14f);
            rt.sizeDelta = new Vector2(160f, 44f);

            var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(36f, 36f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_Coin"), 100f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(42f, 0f);
            trt.offsetMax = Vector2.zero;
            coinHud = textGo.AddComponent<Text>();
            coinHud.font = CoastHudLayout.Font();
            coinHud.fontSize = 24;
            coinHud.fontStyle = FontStyle.Bold;
            coinHud.color = Color.white;
            coinHud.alignment = TextAnchor.MiddleLeft;
            coinHud.raycastTarget = false;
        }

        private void HandleCoins(int total, int delta)
        {
            if (_coinDriveExternal)
                return;
            RefreshCoinHud();
        }

        private void RefreshCoinHud()
        {
            if (coinHud == null || _wallet == null)
                return;
            coinHud.text = _wallet.TotalCoins.ToString();
        }

        public void ShowFloatingReward(Vector3 worldPos, int amount, int combo)
        {
            if (floatPool == null)
                return;

            Color c = combo >= 3 ? comboColor : rewardColor;
            string text = combo >= 2 ? "+" + amount + "  x" + combo : "+" + amount;
            floatPool.Show(worldPos, text, c);
        }

        public void ShowWatchLapPopup(UpgradeStat stat, int level, float value)
        {
            string title = "LAP COMPLETE";
            string body = StatLabel(stat) + "  →  Lv." + level + "  (" + value.ToString("0.##") + ")";
            watchPopup?.Show(title, body);
        }

        public void ShowWatchMessage(string title, string body)
        {
            watchPopup?.Show(title, body, 2.1f);
        }

        private static string StatLabel(UpgradeStat stat)
        {
            switch (stat)
            {
                case UpgradeStat.MaxSpeed: return "MAX SPEED";
                case UpgradeStat.CoinMultiplier: return "COIN MULT";
                case UpgradeStat.MagnetRadius: return "MAGNET";
                default: return stat.ToString();
            }
        }
    }
}
