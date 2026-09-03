using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 『우리의 송전탑』 title — live world backdrop + quiet UI.
    /// Cleared state changes the world; never explains itself.
    public class MainMenuController : MonoBehaviour
    {
        public const string RunSceneName = "02_Run";
        public const string SkipPrologueKey = "CoastRun_SkipPrologue";

        private Canvas _canvas;
        private CanvasGroup _uiCg;
        private CanvasGroup _splashCg;
        private TitleWorldBackdrop _world;
        private TitleAudio _audio;
        private ProgressionManager _progress;
        private GameObject _galleryPanel;
        private GameObject _creditsPanel;
        private GameObject _settingsPanel;
        private Toggle _skipToggle;
        private bool _cleared;
        private bool _ready;

        private void Start()
        {
            Application.targetFrameRate = 60;
            var dir = GameDirector.EnsureExists();
            _progress = dir.Progression;
            _progress.Load();
            _cleared = _progress.HasClearedCampaign ||
                       PlayerPrefs.GetInt(ProgressionManager.ClearedKey, 0) == 1;

            _audio = gameObject.GetComponent<TitleAudio>() ?? gameObject.AddComponent<TitleAudio>();
            _world = gameObject.GetComponent<TitleWorldBackdrop>() ?? gameObject.AddComponent<TitleWorldBackdrop>();
            _world.Build(_cleared);
            _audio.PlayMenu(_cleared);

            BuildSplashAndUi();
            StartCoroutine(SplashThenUi());
        }

        private IEnumerator SplashThenUi()
        {
            // Logo splash 1.5s — skippable.
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.unscaledDeltaTime;
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    break;
                yield return null;
            }

            if (_splashCg != null)
            {
                float f = 0f;
                while (f < 0.45f)
                {
                    f += Time.unscaledDeltaTime;
                    _splashCg.alpha = 1f - f / 0.45f;
                    yield return null;
                }

                _splashCg.gameObject.SetActive(false);
            }

            if (_uiCg != null)
            {
                _uiCg.gameObject.SetActive(true);
                float f = 0f;
                while (f < 0.6f)
                {
                    f += Time.unscaledDeltaTime;
                    _uiCg.alpha = Mathf.Clamp01(f / 0.6f);
                    yield return null;
                }

                _uiCg.alpha = 1f;
            }

            _ready = true;
        }

        public void OnStartRun()
        {
            if (!_ready)
                return;
            _audio?.PlayStart();
            _audio?.StopMenu();
            GameDirector.EnsureExists().Flow.OnTitleStartPressed();
        }

        public void OnContinue()
        {
            if (!_ready || _progress == null || !_progress.HasSave)
                return;
            _audio?.PlayClick();
            _audio?.StopMenu();
            GameDirector.EnsureExists().Flow.OnContinuePressed(_progress.LastStage);
        }

        private void BuildSplashAndUi()
        {
            _canvas = CoastUiCanvas.Create("MainMenuCanvas", 100);
            var root = CoastUiCanvas.Root(_canvas);

            // Transparent UI over live 3D — no full-screen background image.
            BuildSplash(root);
            BuildMainUi(root);
        }

        private void BuildSplash(Transform root)
        {
            var go = new GameObject("Splash", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            _splashCg = go.GetComponent<CanvasGroup>();

            CreateLabel(go.transform, "SplashLogo", "우리의 송전탑", 44, FontStyle.Bold,
                new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.52f), new Vector2(560f, 64f));
            CreateLabel(go.transform, "SplashSub", "Coast Run", 22, FontStyle.Italic,
                new Color(0.75f, 0.88f, 0.95f), new Vector2(0.5f, 0.44f), new Vector2(400f, 36f));
        }

        private void BuildMainUi(Transform root)
        {
            var ui = new GameObject("TitleUI", typeof(RectTransform), typeof(CanvasGroup));
            ui.transform.SetParent(root, false);
            var urt = ui.GetComponent<RectTransform>();
            urt.anchorMin = Vector2.zero;
            urt.anchorMax = Vector2.one;
            urt.offsetMin = Vector2.zero;
            urt.offsetMax = Vector2.zero;
            _uiCg = ui.GetComponent<CanvasGroup>();
            _uiCg.alpha = 0f;
            ui.SetActive(false);

            // Soft bottom veil for readability only — not a still background.
            var veil = CreateImage(ui.transform, "BottomVeil",
                new Vector2(0f, 0f), new Vector2(1f, 0.38f));
            veil.color = new Color(0.02f, 0.05f, 0.1f, 0.55f);

            CreateLabel(ui.transform, "Logo", "우리의 송전탑", 42, FontStyle.Bold,
                new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.88f), new Vector2(600f, 56f));
            CreateLabel(ui.transform, "Subtitle", "Coast Run", 20, FontStyle.Italic,
                new Color(0.8f, 0.9f, 0.98f), new Vector2(0.5f, 0.82f), new Vector2(400f, 32f));

            float y = 0.28f;
            _startButton = CreateMenuButton(ui.transform, "START", y, OnStartRun);
            y -= 0.07f;

            if (_progress != null && _progress.HasSave)
            {
                CreateMenuButton(ui.transform, "이어하기", y, OnContinue);
                y -= 0.07f;
            }

            CreateMenuButton(ui.transform, "설정", y, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_settingsPanel, true);
            });
            y -= 0.07f;

            bool showGallery = _cleared || (_progress != null && _progress.UnlockedMemoryCount >= 1);
            if (showGallery)
            {
                CreateMenuButton(ui.transform, "회상 갤러리", y, () =>
                {
                    _audio?.PlayClick();
                    ShowPanel(_galleryPanel, true);
                });
                y -= 0.07f;
            }

            CreateMenuButton(ui.transform, "크레딧", y, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_creditsPanel, true);
            });

            // Skip prologue — only on replay (has save or cleared).
            if (_progress != null && (_progress.HasSave || _cleared))
                BuildSkipToggle(ui.transform);

            BuildGalleryPanel(root);
            BuildCreditsPanel(root);
            BuildSettingsPanel(root);
        }

        private Button _startButton;

        private void BuildSkipToggle(Transform parent)
        {
            var go = new GameObject("SkipPrologue", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 36f);
            rt.sizeDelta = new Vector2(360f, 36f);

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(go.transform, false);
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, 0f);
            brt.sizeDelta = new Vector2(28f, 28f);
            box.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);

            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            var crt = check.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(4f, 4f);
            crt.offsetMax = new Vector2(-4f, -4f);
            var checkImg = check.GetComponent<Image>();
            checkImg.color = new Color(0.4f, 0.85f, 0.75f, 1f);

            _skipToggle = go.GetComponent<Toggle>();
            _skipToggle.targetGraphic = box.GetComponent<Image>();
            _skipToggle.graphic = checkImg;
            _skipToggle.isOn = PlayerPrefs.GetInt(SkipPrologueKey, 0) == 1;
            check.SetActive(_skipToggle.isOn);
            _skipToggle.onValueChanged.AddListener(v =>
            {
                _audio?.PlayClick();
                check.SetActive(v);
                PlayerPrefs.SetInt(SkipPrologueKey, v ? 1 : 0);
                PlayerPrefs.Save();
            });

            var label = CreateLabel(go.transform, "L", "프롤로그 건너뛰기", 16, FontStyle.Normal,
                new Color(0.85f, 0.9f, 0.95f, 0.85f), new Vector2(0.58f, 0.5f), new Vector2(280f, 32f));
            label.raycastTarget = false;
        }

        private void BuildGalleryPanel(Transform root)
        {
            _galleryPanel = CreateOverlayPanel(root, "Gallery");
            CreateLabel(_galleryPanel.transform, "T", "회상", 28, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.92f), new Vector2(400f, 40f));

            var grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(_galleryPanel.transform, false);
            var grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.08f, 0.18f);
            grt.anchorMax = new Vector2(0.92f, 0.84f);
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(110f, 110f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < ProgressionManager.MemorySlotCount; i++)
            {
                bool unlocked = _progress != null && _progress.IsMemoryUnlocked(i);
                int slot = i;
                var cell = new GameObject("M" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(grid.transform, false);
                var img = cell.GetComponent<Image>();
                if (unlocked)
                {
                    // R15 thumb is phone-like; others cool fill — no completion caption.
                    bool isR15 = slot == 14;
                    img.color = isR15
                        ? new Color(0.2f, 0.22f, 0.28f, 0.95f)
                        : new Color(0.55f, 0.72f, 0.85f, 0.9f);
                    var btn = cell.GetComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        _audio?.PlayClick();
                        var mem = GameDirector.Instance != null
                            ? GameDirector.Instance.Memory
                            : Object.FindFirstObjectByType<MemoryDirector>();
                        if (mem != null)
                            mem.ReplayFromGalleryIndex(slot);
                        else
                            UI_MemoryPopup.Ensure().Play(StoryDatabase.GetByIndex(slot), null, true);
                    });
                }
                else
                {
                    // Silhouette — dark, no label explaining why.
                    img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                    cell.GetComponent<Button>().interactable = false;
                }
            }

            // ★ No completion bonus text even at 15/15.
            CreateMenuButton(_galleryPanel.transform, "닫기", 0.08f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_galleryPanel, false);
            }, absoluteBottom: true);
            _galleryPanel.SetActive(false);
        }

        private void BuildCreditsPanel(Transform root)
        {
            _creditsPanel = CreateOverlayPanel(root, "Credits");
            CreateLabel(_creditsPanel.transform, "T", "크레딧", 28, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.7f), new Vector2(400f, 40f));
            CreateLabel(_creditsPanel.transform, "B", "Coast Run\n우리의 송전탑", 20, FontStyle.Normal,
                new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(480f, 120f));
            CreateMenuButton(_creditsPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_creditsPanel, false);
            }, absoluteBottom: true);
            _creditsPanel.SetActive(false);
        }

        private void BuildSettingsPanel(Transform root)
        {
            _settingsPanel = CreateOverlayPanel(root, "Settings");
            CreateLabel(_settingsPanel.transform, "T", "설정", 28, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.7f), new Vector2(400f, 40f));
            CreateLabel(_settingsPanel.transform, "B", "오디오 · 언어는 준비 중", 18, FontStyle.Normal,
                new Color(0.8f, 0.85f, 0.9f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(400f, 40f));
            CreateMenuButton(_settingsPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_settingsPanel, false);
            }, absoluteBottom: true);
            _settingsPanel.SetActive(false);
        }

        private void ShowPanel(GameObject panel, bool on)
        {
            if (panel != null)
                panel.SetActive(on);
        }

        private GameObject CreateOverlayPanel(Transform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.1f, 0.92f);
            return go;
        }

        private Button CreateMenuButton(Transform parent, string label, float anchorY, UnityEngine.Events.UnityAction onClick,
            bool absoluteBottom = false)
        {
            var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (absoluteBottom)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 48f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, anchorY);
                rt.anchorMax = new Vector2(0.5f, anchorY);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            rt.sizeDelta = new Vector2(360f, 56f);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (label != "START")
                    _audio?.PlayClick();
                onClick?.Invoke();
            });

            CreateLabel(go.transform, "L", label, 22, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(340f, 48f));
            return btn;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 amin, Vector2 amax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin;
            rt.anchorMax = amax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go.GetComponent<Image>();
        }

        private static Text CreateLabel(Transform parent, string name, string text, int size, FontStyle style,
            Color color, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.font = CoastHudLayout.Font();
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }
    }
}
