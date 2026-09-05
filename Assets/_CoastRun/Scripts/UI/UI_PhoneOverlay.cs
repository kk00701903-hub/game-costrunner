using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Phone message overlay — battery by chapter, send-time always present (tiny until twist).
    public class UI_PhoneOverlay : MonoBehaviour
    {
        private static readonly int[] BatteryByChapter = { 74, 51, 33, 8, 3 };

        [SerializeField] private int twistStage;
        [SerializeField] private int chapterIndex = 1;

        private Canvas _canvas;
        private RectTransform _iconRt;
        private CanvasGroup _iconCg;
        private GameObject _panel;
        private CanvasGroup _panelCg;
        private Text _batteryLabel;
        private Text _bodyLabel;
        private Text _sendTimeLabel;
        private Image _crackOverlay;
        private bool _crackPermanent;
        private Coroutine _typeRoutine;

        public CanvasGroup IconCanvasGroup => _iconCg;
        public int TwistStage => twistStage;

        public void Bind()
        {
            EnsureIcon();
            EnsurePanel();
            ApplyTwistVisual();
            SetChapter(chapterIndex);
            HidePanel();
        }

        public void SetChapter(int chapter)
        {
            chapterIndex = Mathf.Clamp(chapter, 1, 5);
            if (_batteryLabel != null)
                _batteryLabel.text = BatteryByChapter[chapterIndex - 1] + "%";

            // CH4 closing → permanent crack.
            if (chapterIndex >= 4)
                ApplyCrack(true);
        }

        public void SetTwistStage(int stage)
        {
            twistStage = Mathf.Max(0, stage);
            ApplyTwistVisual();
            if (twistStage >= 2 && _panel != null && _panel.activeSelf)
                PlaySendTimeReveal();
        }

        public void OpenFromPauseOrTap()
        {
            EnsurePanel();
            _panel.SetActive(true);
            if (_panelCg != null)
                _panelCg.alpha = 1f;
            ApplyTwistVisual();
            if (twistStage >= 2)
                PlaySendTimeReveal();
        }

        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void EnsureIcon()
        {
            if (_iconRt != null)
                return;

            var hud = GameObject.Find("CoastRunHUD");
            _canvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (_canvas == null)
                _canvas = CoastUiCanvas.Create("PhoneHUD", 115);

            var go = new GameObject("PhoneIcon", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            _iconRt = go.GetComponent<RectTransform>();
            _iconRt.anchorMin = new Vector2(0f, 1f);
            _iconRt.anchorMax = new Vector2(0f, 1f);
            _iconRt.pivot = new Vector2(0f, 1f);
            // Below the HP bar (pause 6..78, bar 88..128) so nothing overlaps the chrome.
            _iconRt.anchoredPosition = new Vector2(8f, -138f);
            _iconRt.sizeDelta = new Vector2(52f, 52f);
            _iconCg = go.GetComponent<CanvasGroup>();

            var img = go.GetComponent<Image>();
            var sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_Phone"), 100f);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                // Cute pill fallback with a phone glyph until Icon_Phone is painted.
                img.sprite = CoastUiArt.RoundedRect(16);
                img.type = Image.Type.Sliced;
                img.color = CoastUiArt.CreamOutline;
                var fill = CoastUiArt.Panel(go.transform, "Fill", new Color(0.22f, 0.36f, 0.62f, 1f), 12);
                var frt = fill.rectTransform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = new Vector2(4f, 4f); frt.offsetMax = new Vector2(-4f, -4f);
                var glyph = CoastHudLayout.MakeText(go.transform, "Glyph", "☎", 26, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 1f));
                glyph.color = Color.white;
                glyph.raycastTarget = false;
            }

            go.GetComponent<Button>().onClick.AddListener(OpenFromPauseOrTap);
        }

        private void EnsurePanel()
        {
            if (_panel != null)
                return;

            EnsureIcon();
            _panel = new GameObject("PhonePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            _panel.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 560f);
            rt.anchoredPosition = Vector2.zero;
            _panelCg = _panel.GetComponent<CanvasGroup>();
            var bg = _panel.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.09f, 0.14f, 0.96f);

            _batteryLabel = MakeLabel(_panel.transform, "Battery", "74%", 16,
                new Vector2(0.7f, 0.92f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleRight);

            _bodyLabel = MakeLabel(_panel.transform, "Body",
                "노을 질 때, 우리 어릴 적 비밀 기지였던\n그 송전탑 아래에서 만나자.\n꼭 할 말이 있어.",
                20, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.85f), TextAnchor.UpperLeft);
            _bodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bodyLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // ★ Always rendered from CH1 — small & faint until twist.
            _sendTimeLabel = MakeLabel(_panel.transform, "SendTime", "발신  어제 19:04", 10,
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.32f), TextAnchor.MiddleLeft);

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_panel.transform, false);
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.3f, 0.04f);
            crt.anchorMax = new Vector2(0.7f, 0.12f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            closeGo.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 1f);
            closeGo.GetComponent<Button>().onClick.AddListener(HidePanel);
            MakeLabel(closeGo.transform, "L", "닫기", 18, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);

            var crackGo = new GameObject("Crack", typeof(RectTransform), typeof(Image));
            crackGo.transform.SetParent(_panel.transform, false);
            var krt = crackGo.GetComponent<RectTransform>();
            krt.anchorMin = Vector2.zero;
            krt.anchorMax = Vector2.one;
            krt.offsetMin = Vector2.zero;
            krt.offsetMax = Vector2.zero;
            _crackOverlay = crackGo.GetComponent<Image>();
            var crackSprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("UI_PhoneCrack"), 100f);
            if (crackSprite != null)
            {
                _crackOverlay.sprite = crackSprite;
                _crackOverlay.color = new Color(1f, 1f, 1f, 0.85f);
            }
            else
            {
                // Procedural faint crack lines via tinted overlay.
                _crackOverlay.color = new Color(0.7f, 0.75f, 0.8f, 0f);
            }

            _crackOverlay.raycastTarget = false;
            crackGo.SetActive(false);
        }

        private void ApplyTwistVisual()
        {
            if (_sendTimeLabel == null)
                return;

            if (twistStage < 2)
            {
                _sendTimeLabel.fontSize = 10;
                var c = _sendTimeLabel.color;
                c.a = 0.35f;
                _sendTimeLabel.color = c;
                _sendTimeLabel.text = "발신  어제 19:04";
            }
            else
            {
                _sendTimeLabel.fontSize = 22;
                var c = _sendTimeLabel.color;
                c.a = 1f;
                _sendTimeLabel.color = c;
            }
        }

        private void PlaySendTimeReveal()
        {
            if (_sendTimeLabel == null)
                return;
            if (_typeRoutine != null)
                StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypeSendTime("발신  어제 19:04"));
        }

        private IEnumerator TypeSendTime(string full)
        {
            ApplyTwistVisual();
            _sendTimeLabel.text = "";
            for (int i = 0; i < full.Length; i++)
            {
                _sendTimeLabel.text = full.Substring(0, i + 1);
                float wait = 0f;
                while (wait < 0.045f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _typeRoutine = null;
        }

        private void ApplyCrack(bool permanent)
        {
            if (_crackOverlay == null)
                EnsurePanel();
            if (_crackOverlay == null)
                return;

            _crackPermanent = permanent || _crackPermanent;
            _crackOverlay.gameObject.SetActive(_crackPermanent);
            if (_crackOverlay.sprite == null)
                _crackOverlay.color = new Color(0.85f, 0.88f, 0.9f, 0.25f);
        }

        private static Text MakeLabel(Transform parent, string name, string content, int size,
            Vector2 aMin, Vector2 aMax, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            text.font = CoastHudLayout.Font();
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = align;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }
    }
}
