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

            _gm = GameManager.Ensure();
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

        private GameManager _gm;
        private GameObject _charSelectPanel;

        /// v2: 세이브가 있으면 이어하기, 없으면 캐릭터 선택 → 새 회차.
        public void OnStartRun()
        {
            if (!_ready)
                return;
            if (_gm != null && _gm.HasSave)
            {
                OnContinue();
                return;
            }
            _audio?.PlayStart();
            ShowPanel(_charSelectPanel, true);
        }

        public void OnContinue()
        {
            if (!_ready || _gm == null || !_gm.HasSave)
                return;
            _audio?.PlayClick();
            _audio?.StopMenu();
            _gm.Continue();
        }

        private void StartNewPlaythrough(RunMode mode)
        {
            if (_gm == null) return;
            if (mode == RunMode.Skateboard && !_gm.Profile.skateboardUnlocked)
                return;
            _audio?.PlayStart();
            _audio?.StopMenu();
            _gm.NewGame(mode);
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
            var splashImg = go.GetComponent<Image>();
            splashImg.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            _splashCg = go.GetComponent<CanvasGroup>();

            // Key art (Firefly, Resources/CoastRun/UI_TitleBackground) when present: the
            // splash becomes the painted poster, with a soft dark band so the logo reads.
            var keyArt = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "UI_TitleBackground");
            if (keyArt != null)
            {
                splashImg.sprite = CoastUiArt.AsSprite(keyArt, 100f);
                splashImg.color = Color.white;
                splashImg.preserveAspect = false;

                var band = new GameObject("LogoBand", typeof(RectTransform), typeof(Image));
                band.transform.SetParent(go.transform, false);
                var brt = band.GetComponent<RectTransform>();
                // Up in the sky, so the painted skater below stays untouched.
                brt.anchorMin = new Vector2(0f, 0.68f);
                brt.anchorMax = new Vector2(1f, 0.90f);
                brt.offsetMin = brt.offsetMax = Vector2.zero;
                band.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.16f, 0.55f);
            }

            CreateLabel(go.transform, "SplashLogo", "우리의 송전탑", 44, FontStyle.Bold,
                new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(560f, 64f));
            CreateLabel(go.transform, "SplashSub", "Coast Run", 22, FontStyle.Italic,
                new Color(0.75f, 0.88f, 0.95f), new Vector2(0.5f, 0.745f), new Vector2(400f, 36f));
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

            // Layout follows the Subway Surfers title: the whole screen is the start
            // button, the logo sits high, "tap to play" pulses mid-screen, and the three
            // big rounded buttons along the bottom hold everything else.

            // Full-screen tap-to-play (lowest sibling so the bottom buttons win clicks).
            var tap = new GameObject("TapToPlay", typeof(RectTransform), typeof(Image), typeof(Button));
            tap.transform.SetParent(ui.transform, false);
            var tapRt = tap.GetComponent<RectTransform>();
            tapRt.anchorMin = Vector2.zero;
            tapRt.anchorMax = Vector2.one;
            tapRt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            tapRt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            var tapImg = tap.GetComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            _startButton = tap.GetComponent<Button>();
            _startButton.transition = Selectable.Transition.None;
            _startButton.onClick.AddListener(OnStartRun);

            // Soft bottom veil for readability only — not a still background.
            var veil = CreateImage(ui.transform, "BottomVeil",
                new Vector2(0f, 0f), new Vector2(1f, 0.30f));
            veil.color = new Color(0.02f, 0.05f, 0.1f, 0.55f);
            veil.raycastTarget = false;

            // Top row — coins (left) and best score (right), in the run HUD's pill style.
            int coins = PlayerPrefs.GetInt(CoinWallet.PrefsKey, 0);
            BuildTopPill(ui.transform, "CoinPill", coins.ToString(), "Icon_Coin", new Vector2(0f, 1f));
            BuildTopPill(ui.transform, "BestPill", "BEST " + RunHudChrome.BestScore.ToString("00000"), null,
                new Vector2(1f, 1f));

            // Logo block: shadow + title + subtitle on a rounded cream plate.
            var plate = CoastUiArt.Panel(ui.transform, "LogoPlate", new Color(0.98f, 0.95f, 0.88f, 0.92f), 28);
            var prt = plate.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.80f);
            prt.sizeDelta = new Vector2(560f, 150f);
            prt.localRotation = Quaternion.Euler(0f, 0f, 2.5f);
            var shadow = CreateLabel(plate.transform, "LogoShadow", "우리의 송전탑", 60, FontStyle.Bold,
                new Color(0.10f, 0.14f, 0.30f, 0.35f), new Vector2(0.5f, 0.58f), new Vector2(600f, 80f));
            shadow.rectTransform.anchoredPosition = new Vector2(4f, -4f);
            CreateLabel(plate.transform, "Logo", "우리의 송전탑", 60, FontStyle.Bold,
                new Color(1f, 0.55f, 0.15f), new Vector2(0.5f, 0.58f), new Vector2(600f, 80f));
            CreateLabel(plate.transform, "Subtitle", "COAST RUN", 20, FontStyle.Bold,
                new Color(0.10f, 0.14f, 0.30f, 0.8f), new Vector2(0.5f, 0.18f), new Vector2(400f, 32f));

            // Pulsing prompt.
            _tapLabel = CreateLabel(ui.transform, "TapPrompt", "화면을 터치하면 출발", 30, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.36f), new Vector2(600f, 48f));
            _tapLabel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            // Bottom row: three big rounded buttons.
            bool hasSave = _gm != null && _gm.HasSave;
            bool showGallery = _cleared || (_progress != null && _progress.UnlockedMemoryCount >= 1);
            BuildBottomButton(ui.transform, hasSave ? "새로 시작" : "기록", 0,
                new Color(0.30f, 0.72f, 0.36f), () =>
                {
                    _audio?.PlayClick();
                    if (hasSave) ShowPanel(_charSelectPanel, true);
                    else ShowPanel(_recordPanel, true);
                });
            BuildBottomButton(ui.transform, "회상", 1, new Color(0.35f, 0.45f, 0.70f), () =>
            {
                _audio?.PlayClick();
                ShowPanel(showGallery ? _galleryPanel : _recordPanel, true);
            });
            BuildBottomButton(ui.transform, "설정", 2, new Color(1f, 0.55f, 0.15f), () =>
            {
                _audio?.PlayClick();
                ShowPanel(_settingsPanel, true);
            });

            // Skip prologue — only on replay (has save or cleared).
            if (_progress != null && (_progress.HasSave || _cleared))
                BuildSkipToggle(ui.transform);

            BuildGalleryPanel(root);
            BuildCreditsPanel(root);
            BuildSettingsPanel(root);
            BuildRecordPanel(root);
            BuildCharacterSelect(root);
            if (hasSave)
                _tapLabel.text = "화면을 터치하면 이어하기";
        }

        /// 회차 시작 캐릭터 선택: 러닝 / 스케이트보드(엔딩 1회 후 해금, 속도·코인 ×1.3).
        private void BuildCharacterSelect(Transform root)
        {
            _charSelectPanel = CreateOverlayPanel(root, "CharacterSelect");
            bool unlocked = _gm != null && _gm.Profile.skateboardUnlocked;
            bool hasSave = _gm != null && _gm.HasSave;

            CreateLabel(_charSelectPanel.transform, "Title", "누구로 달릴까?", 34, FontStyle.Bold,
                new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.86f), new Vector2(600f, 50f));
            if (hasSave)
                CreateLabel(_charSelectPanel.transform, "Warn", "새로 시작하면 지금 진행 중인 회차는 지워져.", 16, FontStyle.Normal,
                    new Color(1f, 0.6f, 0.6f), new Vector2(0.5f, 0.81f), new Vector2(600f, 30f));

            BuildCharCard(_charSelectPanel.transform, "러닝", "달려서 송전탑까지.\n속도 ×1.0 · 코인 ×1.0\n처음이라면 이쪽.",
                new Color(0.30f, 0.72f, 0.36f), 0.60f, true, () => StartNewPlaythrough(RunMode.Running));
            BuildCharCard(_charSelectPanel.transform, "스케이트보드",
                unlocked ? "보드로 질주. 속도 ×1.3 · 코인 ×1.3\n반응 시간이 짧은 고급 난이도." : "잠김 — 엔딩을 한 번 보면 열려.\n속도 ×1.3 · 코인 ×1.3 (고급)",
                unlocked ? new Color(1f, 0.55f, 0.15f) : new Color(0.35f, 0.36f, 0.42f), 0.36f, unlocked,
                () => StartNewPlaythrough(RunMode.Skateboard));

            CreateMenuButton(_charSelectPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_charSelectPanel, false);
            }, absoluteBottom: true);
            _charSelectPanel.SetActive(false);
        }

        private void BuildCharCard(Transform parent, string title, string body, Color color, float anchorY, bool enabled,
            UnityEngine.Events.UnityAction onClick)
        {
            var card = CoastUiArt.CutePill(parent, title + "Card", color, 24, 5);
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 210f);
            card.raycastTarget = true;
            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                if (!enabled) { _audio?.PlayClick(); return; }
                onClick?.Invoke();
            });
            var t = CreateLabel(card.transform, "T", title + (enabled ? "" : "  (잠김)"), 30, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.74f), new Vector2(520f, 44f));
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var b = CreateLabel(card.transform, "B", body, 17, FontStyle.Normal, new Color(1f, 1f, 1f, enabled ? 0.95f : 0.7f),
                new Vector2(0.5f, 0.36f), new Vector2(520f, 90f));
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private Button _startButton;
        private Text _tapLabel;
        private GameObject _recordPanel;

        private void Update()
        {
            if (_tapLabel != null && _ready)
            {
                float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
                var c = _tapLabel.color;
                c.a = a;
                _tapLabel.color = c;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터 검증용: N = 캐릭터 선택, 1 = 러닝, 2 = 스케이트보드, C = 이어하기, Escape = 닫기.
            if (!_ready) return;
            if (Input.GetKeyDown(KeyCode.N)) ShowPanel(_charSelectPanel, true);
            if (Input.GetKeyDown(KeyCode.C)) OnContinue();
            if (_charSelectPanel != null && _charSelectPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) StartNewPlaythrough(RunMode.Running);
                if (Input.GetKeyDown(KeyCode.Alpha2)) StartNewPlaythrough(RunMode.Skateboard);
                if (Input.GetKeyDown(KeyCode.Escape)) ShowPanel(_charSelectPanel, false);
            }
#endif
        }

        private void BuildTopPill(Transform parent, string name, string text, string iconName, Vector2 corner)
        {
            var pill = CoastUiArt.Panel(parent, name, RunHudChrome.PillNavy, 20);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = corner;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(iconName != null ? 170f : 230f, 54f);

            float textRight = -16f;
            if (iconName != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(rt, false);
                var irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0f, 0.5f);
                irt.anchoredPosition = new Vector2(10f, 0f);
                irt.sizeDelta = new Vector2(36f, 36f);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture(iconName), 100f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var label = CoastHudLayout.MakeText(rt, "Label", text, 26, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(iconName != null ? 52f : 16f, 0f), new Vector2(textRight, 0f));
            label.color = RunHudChrome.ScoreYellow;
        }

        private void BuildBottomButton(Transform parent, string label, int slot, Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var img = CoastUiArt.Panel(parent, label + "Btn", color, 22);
            var rt = img.rectTransform;
            float x = (slot - 1) * 0.31f;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f + x, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 96f);
            rt.sizeDelta = new Vector2(190f, 96f);
            img.raycastTarget = true;

            var text = CoastHudLayout.MakeText(rt, "Label", label, 28, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, 0f));
            text.color = Color.white;
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.45f);

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
        }

        private void BuildRecordPanel(Transform root)
        {
            _recordPanel = CreateOverlayPanel(root, "Record");
            CreateLabel(_recordPanel.transform, "T", "기록", 28, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.7f), new Vector2(400f, 40f));
            int coins = PlayerPrefs.GetInt(CoinWallet.PrefsKey, 0);
            CreateLabel(_recordPanel.transform, "B",
                "최고 점수  " + RunHudChrome.BestScore.ToString("00000") + "\n보유 코인  " + coins +
                "\n회상 조각  " + (_progress != null ? _progress.UnlockedMemoryCount : 0) + " / " +
                ProgressionManager.MemorySlotCount,
                22, FontStyle.Normal, new Color(0.85f, 0.9f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(480f, 140f));
            CreateMenuButton(_recordPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_recordPanel, false);
            }, absoluteBottom: true);
            _recordPanel.SetActive(false);
        }

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
            CreateMenuButton(_settingsPanel.transform, "크레딧", 0.36f, () =>
            {
                ShowPanel(_settingsPanel, false);
                ShowPanel(_creditsPanel, true);
            });

            // Pet picker — cycles through the three companions; the run reads
            // PetCompanion.Selected when it builds the pet.
            Text petLabel = null;
            var petBtn = CreateMenuButton(_settingsPanel.transform, "펫", 0.6f, () =>
            {
                PetCompanion.Selected = (PetKind)(((int)PetCompanion.Selected + 1) % 4);
                if (petLabel != null)
                    petLabel.text = PetLabel();
            });
            petLabel = petBtn.GetComponentInChildren<Text>();
            if (petLabel != null)
            {
                petLabel.text = PetLabel();
                petLabel.fontSize = 18;
            }
            CreateMenuButton(_settingsPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_settingsPanel, false);
            }, absoluteBottom: true);
            _settingsPanel.SetActive(false);
        }

        private static string PetLabel()
        {
            int k = (int)PetCompanion.Selected;
            return "펫: " + PetCompanion.Names[k] + "  ▸  " + PetCompanion.Blurbs[k];
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
