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
            IapBridge.Init();   // 스토어 연결·구매 복원(비동기, 실패해도 무시)
            Application.targetFrameRate = 60;
            var dir = GameDirector.EnsureExists();
            _progress = dir.Progression;
            _progress.Load();
            _cleared = _progress.HasClearedCampaign ||
                       PlayerPrefs.GetInt(ProgressionManager.ClearedKey, 0) == 1;

            _gm = GameManager.Ensure();
            _audio = gameObject.GetComponent<TitleAudio>() ?? gameObject.AddComponent<TitleAudio>();
            // 14차-8: 키아트 한 장(로고 없음) + 제목은 글자로 언어별 표시
            _gateArt = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "UI_Title_Gate");
            if (_gateArt == null)
            {
                // 대문 아트가 없을 때만 옛 3D 배경을 세운다(모바일 메모리 절약).
                _world = gameObject.GetComponent<TitleWorldBackdrop>() ?? gameObject.AddComponent<TitleWorldBackdrop>();
                _world.Build(_cleared);
            }
            else
            {
                // 옛 3D 배경이 카메라를 만들던 자리 — 대문 아트만 쓸 때도 카메라/리스너는 있어야 한다.
                var cam = Camera.main;
                if (cam == null)
                {
                    var go = new GameObject("TitleCamera");
                    go.tag = "MainCamera";
                    cam = go.AddComponent<Camera>();
                    go.AddComponent<AudioListener>();
                    cam.transform.position = new Vector3(0f, 0f, -10f);
                }
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.08f, 0.10f);
                cam.cullingMask = 0;
                if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
                if (cam.GetComponent<CoastPortraitViewport>() == null)
                    cam.gameObject.AddComponent<CoastPortraitViewport>();
            }

            BuildSplashAndUi();
            // 11차: 오프닝은 게임을 켤 때마다 타이틀 앞에서(건너뛰기 버튼). 씬 재로드(언어 전환 등)에는 안 나온다.
            // 14차-9: 오프닝 강제 재생 제거 — 첫 실행에서 1분 37초 영상은 글로벌 캐주얼 기준 이탈 1순위.
            // 오프닝은 더보기 > 오프닝, 또는 첫 런을 마친 뒤 제안(GameSession)으로만 본다.
            bool firstLaunch = false;
            _openingShownThisSession = true;
            if (firstLaunch && _gateArt != null)
            {
                // 앱 시작: 오프닝 영상(최대 60초) → 타이틀. 메뉴 음악은 오프닝이 끝나고 시작.
                OpeningCinematic.Play(() =>
                {
                    if (this == null) return;
                    _audio.PlayMenu(_cleared);
                    StartCoroutine(SplashThenUi(0.2f));
                });
            }
            else
            {
                _audio.PlayMenu(_cleared);
                StartCoroutine(SplashThenUi(_gateArt != null ? 0.6f : 1.5f));
            }
        }

        private Texture2D _gateArt;
        private static bool _openingShownThisSession;

        private IEnumerator SplashThenUi(float splashSeconds)
        {
            // Logo splash — skippable.
            float t = 0f;
            while (t < splashSeconds)
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
            ShowAiNoticeOnce();
        }

        /// 첫 실행 1회: AI 제작 혼성 듀오 고지(스토어 정책·팬덤 신뢰). 확인 전엔 메뉴가 안 눌린다.
        private void ShowAiNoticeOnce()
        {
            var p = _gm?.Profile;
            if (p == null || p.aiNoticeSeen) return;
            _ready = false;
            var root = CoastUiCanvas.Root(_canvas);
            var dim = CoastHudLayout.MakeImage(root, "AiNoticeDim", Vector2.zero, Vector2.one, new Vector2(-40f, -40f), new Vector2(40f, 40f), new Color(0f, 0f, 0f, 0.7f));
            dim.raycastTarget = true;
            var panel = CoastOrnate.PanelSized(root, "AiNotice", CoastOrnate.Gold, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 420f), new Color(0.98f, 0.95f, 0.88f, 0.97f));
            var t = CoastOrnate.Label(panel.transform, "T", Loc.T("『제주』는 AI로 만든 가상 듀오예요", "“JEJU” is an AI-produced virtual duo"), 22, new Color(0.16f, 0.12f, 0.10f));
            t.rectTransform.anchorMin = new Vector2(0f, 1f); t.rectTransform.anchorMax = new Vector2(1f, 1f); t.rectTransform.anchoredPosition = new Vector2(0f, -40f); t.rectTransform.sizeDelta = new Vector2(0f, 40f);
            var b = CoastOrnate.Label(panel.transform, "B", Loc.T(
                "하늘과 도윤의 목소리·노래·그림은 AI로 제작했고, 이야기와 게임은 사람이 만들었습니다. 실존 인물이나 그룹을 흉내 내지 않습니다.\n\n봄 시즌은 무료, 나머지는 디지털 앨범(1회 결제)으로 열립니다. 광고는 없습니다.",
                "Haneul and Doyun's voices, songs and art are AI-produced; the story and the game are made by people. They do not imitate any real person or group.\n\nSpring is free; the rest opens with the digital album (one purchase). No ads."), 16, new Color(0.16f, 0.12f, 0.10f), TextAnchor.UpperLeft);
            b.rectTransform.anchorMin = new Vector2(0f, 0f); b.rectTransform.anchorMax = new Vector2(1f, 1f); b.rectTransform.offsetMin = new Vector2(30f, 90f); b.rectTransform.offsetMax = new Vector2(-30f, -80f);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            _aiNoticeOk = () =>
            {
                _aiNoticeOk = null;
                p.aiNoticeSeen = true; _gm.WriteProfileNow();
                Destroy(panel.gameObject); Destroy(dim.gameObject); _ready = true;
            };
            CoastOrnate.GlassButton(panel.transform, "Ok", Loc.T("알겠어요", "Got it"), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(220f, 46f), () => _aiNoticeOk?.Invoke(), 0.5f, 18, true);
        }

        private GameManager _gm;
        private System.Action _aiNoticeOk;
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
            var keyArt = _gateArt != null ? null : Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "UI_TitleBackground");
            if (_gateArt != null)
                splashImg.color = new Color(0.12f, 0.08f, 0.10f, 1f);
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

            if (_gateArt == null)
            {
                CreateLabel(go.transform, "SplashLogo", "우리의 송전탑", 44, FontStyle.Bold,
                    new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(560f, 64f));
                CreateLabel(go.transform, "SplashSub", "Coast Run", 22, FontStyle.Italic,
                    new Color(0.75f, 0.88f, 0.95f), new Vector2(0.5f, 0.745f), new Vector2(400f, 36f));
            }
        }

        private void BuildMainUi(Transform root)
        {
            if (_gateArt != null)
            {
                BuildGateUi(root);
                return;
            }
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


        /// 프린세스 메이커 대문식 타이틀: 전면 키아트 + 상단 로고 + 장식 메뉴 패널.
        private void BuildGateUi(Transform root)
        {
            var pad = CoastUiCanvas.HudPad;
            var bg = CoastHudLayout.MakeImage(root, "GateArt", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.white);
            bg.sprite = CoastUiArt.AsSprite(_gateArt, 100f);
            bg.preserveAspect = false;
            bg.raycastTarget = false;
            bg.transform.SetAsFirstSibling();

            var ui = new GameObject("TitleUI", typeof(RectTransform), typeof(CanvasGroup));
            ui.transform.SetParent(root, false);
            CoastOrnate.Stretch(ui.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            _uiCg = ui.GetComponent<CanvasGroup>();
            _uiCg.alpha = 0f;
            ui.SetActive(false);

            // 18차-5: 빈 공간(버튼 밖) 어디를 눌러도 이어하기 — 골드런처럼 '탭하면 바로 시작'.
            // 투명 버튼을 TitleUI의 맨 뒤에 깔아 두고, 위에 있는 버튼·더보기 컬럼이 먼저 레이캐스트를 가져간다.
            var tapAny = new GameObject("TapAnywhere", typeof(RectTransform), typeof(Image), typeof(Button));
            tapAny.transform.SetParent(ui.transform, false);
            CoastOrnate.Stretch(tapAny.GetComponent<RectTransform>(), -CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad, CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            var tapImg = tapAny.GetComponent<Image>(); tapImg.color = new Color(0f, 0f, 0f, 0f); tapImg.raycastTarget = true;
            var tapBtn = tapAny.GetComponent<Button>(); tapBtn.transition = Selectable.Transition.None;
            tapBtn.onClick.AddListener(() =>
            {
                if (!_ready) return;
                if (_moreOpen) { ToggleMore(); return; }          // 더보기 열린 채면 먼저 닫기
                if (_gm != null && _gm.HasSave) OnContinue();
                else { _audio?.PlayStart(); ShowPanel(_charSelectPanel, true); }   // 세이브 없으면 새로하기 흐름
            });
            tapAny.transform.SetAsFirstSibling();

            // 14차-8: 제목은 글자로(언어별) — 키아트엔 로고가 없다. 상단 하늘 영역, 크림색 + 짙은 테두리.
            string title = Loc.T(AlbumTable.AlbumKo, AlbumTable.AlbumEn);
            var titleLbl = CreateLabel(ui.transform, "Title", title, 60, FontStyle.Bold,
                new Color(1f, 0.96f, 0.86f), new Vector2(0.5f, 0.905f), new Vector2(680f, 90f));
            CoastUiArt.OutlineText(titleLbl, new Color(0.22f, 0.10f, 0.06f, 0.95f), 3f);
            var sub = CreateLabel(ui.transform, "TitleSub", Loc.IsKo ? "COAST RUN · JEJU" : "너와 나의 주파수 · COAST RUN", 20, FontStyle.Normal,
                new Color(1f, 0.93f, 0.78f, 0.9f), new Vector2(0.5f, 0.855f), new Vector2(600f, 30f));
            CoastUiArt.OutlineText(sub, new Color(0.22f, 0.10f, 0.06f, 0.8f), 1.5f);

            // 메뉴 3개(이어하기 / 새로하기 / 더보기) — 화면 아래 가로 한 줄. 나머지는 '더보기'로 옆에서 슬라이드.
            bool hasSave = _gm != null && _gm.HasSave;
            float btnW = 188f, btnH = 62f, gapX = 10f;   // 18차-3: 폴드(22:9) 폭 615 안에 3개(188×3+10×2 = 584)
            float rowY = 92f;
            var contBtn = CoastOrnate.GlassButton(ui.transform, "ContinueBtn", Loc.T("이어하기", "Continue"), new Vector2(0.5f, 0f),
                new Vector2(-(btnW + gapX), rowY), new Vector2(btnW, btnH), () => { if (_ready) OnContinue(); }, 0.4f, 26, hasSave);
            if (!hasSave)
            {
                contBtn.interactable = false;
                var cg = contBtn.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0.45f;
            }
            CoastOrnate.GlassButton(ui.transform, "NewBtn", Loc.T("새로하기", "New Game"), new Vector2(0.5f, 0f),
                new Vector2(0f, rowY), new Vector2(btnW, btnH), () => { if (_ready) { _audio?.PlayStart(); ShowPanel(_charSelectPanel, true); } }, 0.4f, 26, !hasSave);
            _moreBtn = CoastOrnate.GlassButton(ui.transform, "MoreBtn", Loc.T("더보기", "More"), new Vector2(0.5f, 0f),
                new Vector2(btnW + gapX, rowY), new Vector2(btnW, btnH), () => { if (_ready) ToggleMore(); }, 0.4f, 26, false);
            _moreLabel = _moreBtn.GetComponentInChildren<Text>();
            // 14차-9: 오프닝을 안 본 유저에게 한 줄 힌트(강제 재생 대신)
            if (PlayerPrefs.GetInt("CoastRun_OpeningSeen", 0) == 0)
            {
                _openingHint = CreateLabel(ui.transform, "OpeningHint", Loc.T("이야기가 궁금하면  더보기 › 오프닝", "Curious about the story?  More › Opening"), 17, FontStyle.Normal,
                    new Color(1f, 0.96f, 0.86f, 0.9f), new Vector2(0.5f, 0f), new Vector2(600f, 26f));
                _openingHint.rectTransform.anchoredPosition = new Vector2(0f, rowY + btnH * 0.5f + 20f);
                CoastUiArt.OutlineText(_openingHint, new Color(0.2f, 0.1f, 0.06f, 0.85f), 1.5f);
            }

            // 더보기 열: 오른쪽 가장자리에서 슬라이드 인. 챕터 선택 / 노을 달리기 / 컬렉션 / 오프닝 / 설정.
            var more = new System.Collections.Generic.List<(string, System.Action)>();
            if (hasSave) more.Add((Loc.T("챕터 선택", "Chapters"), OnChapterSelect));
            more.Add((Loc.T("노을 달리기", "Sunset Run"), () => { _audio?.PlayClick(); ArcadeUI.Open(false); }));
            more.Add((Loc.T("컬렉션", "Collection"), () => { _audio?.PlayClick(); CollectionUI.Open(); }));
            more.Add((Loc.T("오프닝", "Opening"), () =>
            {
                _audio?.PlayClick();
                _audio?.StopMenu();
                PlayerPrefs.SetInt("CoastRun_OpeningSeen", 1);
                if (_openingHint != null) _openingHint.gameObject.SetActive(false);
                _ready = false;
                OpeningCinematic.Play(() => { if (this == null) return; _audio?.PlayMenu(_cleared); _ready = true; });
            }));
            more.Add((Loc.T("설정", "Settings"), () => { _audio?.PlayClick(); ShowPanel(_settingsPanel, true); }));

            var col = new GameObject("MoreColumn", typeof(RectTransform), typeof(CanvasGroup));
            col.transform.SetParent(ui.transform, false);
            _moreRt = col.GetComponent<RectTransform>();
            _moreCg = col.GetComponent<CanvasGroup>();
            float mW = 236f, mH = 58f, mGap = 10f;
            float total = more.Count * mH + (more.Count - 1) * mGap;
            _moreRt.anchorMin = _moreRt.anchorMax = new Vector2(1f, 0f);
            _moreRt.pivot = new Vector2(1f, 0f);
            _moreRt.sizeDelta = new Vector2(mW + 24f, total + 24f);
            _moreHidden = new Vector2(mW + 60f, rowY + btnH * 0.5f + 26f);
            _moreShown = new Vector2(-10f, rowY + btnH * 0.5f + 26f);
            _moreRt.anchoredPosition = _moreHidden;
            _moreCg.alpha = 0f; _moreCg.interactable = false; _moreCg.blocksRaycasts = false;
            var backing = CoastUiArt.Panel(col.transform, "Backing", new Color(0.05f, 0.04f, 0.08f, 0.35f), 16);
            CoastOrnate.Stretch(backing.rectTransform, 0f, 0f, 0f, 0f);
            backing.raycastTarget = false;
            for (int i = 0; i < more.Count; i++)
            {
                var (label, act) = more[i];
                float y = 12f + mH * 0.5f + (more.Count - 1 - i) * (mH + mGap);
                CoastOrnate.GlassButton(col.transform, label + "Btn", label, new Vector2(0.5f, 0f), new Vector2(0f, y),
                    new Vector2(mW, mH), () => { if (_ready) act(); }, 0.42f, 24, false);
            }

            var ver = CreateLabel(ui.transform, "Version", "v" + Application.version, 14, FontStyle.Normal,
                new Color(1f, 1f, 1f, 0.55f), new Vector2(0.5f, 0.018f), new Vector2(300f, 20f));

            BuildGalleryPanel(root);
            BuildCreditsPanel(root);
            BuildSettingsPanel(root);
            BuildRecordPanel(root);
            BuildCharacterSelect(root);
        }

        /// 세이브가 있을 때: 육성 화면을 챕터 선택(타임라인)이 열린 상태로 연다.
        private void OnChapterSelect()
        {
            if (!_ready || _gm == null || !_gm.HasSave) return;
            _audio?.PlayClick();
            _audio?.StopMenu();
            _gm.OpenTimelineOnRaising = true;
            _gm.Continue();
        }

        /// 회차 시작 캐릭터 선택: 러닝 / 스케이트보드(엔딩 1회 후 해금, 속도·코인 ×1.3).
        private void BuildCharacterSelect(Transform root)
        {
            _charSelectPanel = CreateOverlayPanel(root, "CharacterSelect");
            bool unlocked = _gm != null && _gm.Profile.skateboardUnlocked;
            bool hasSave = _gm != null && _gm.HasSave;

            CreateLabel(_charSelectPanel.transform, "Title", Loc.T("누구로 달릴까?", "How will you run?"), 34, FontStyle.Bold,
                new Color(1f, 0.95f, 0.82f), new Vector2(0.5f, 0.86f), new Vector2(600f, 50f));
            if (hasSave)
                CreateLabel(_charSelectPanel.transform, "Warn", Loc.T("새로 시작하면 지금 진행 중인 회차는 지워져.", "Starting over erases the current playthrough."), 16, FontStyle.Normal,
                    new Color(1f, 0.6f, 0.6f), new Vector2(0.5f, 0.81f), new Vector2(600f, 30f));

            BuildCharCard(_charSelectPanel.transform, Loc.T("러닝", "Running"), Loc.T("달려서 송전탑까지.\n속도 ×1.0 · 코인 ×1.0\n처음이라면 이쪽.", "Run to the tower.\nSpeed ×1.0 · Coins ×1.0\nStart here."),
                new Color(0.30f, 0.72f, 0.36f), 0.60f, true, () => StartNewPlaythrough(RunMode.Running), "Raise_Girl_Happy", false);
            BuildCharCard(_charSelectPanel.transform, Loc.T("스케이트보드", "Skateboard"),
                unlocked ? Loc.T("보드로 질주. 속도 ×1.3 · 코인 ×1.3\n반응 시간이 짧은 고급 난이도.", "Ride the board. Speed ×1.3 · Coins ×1.3\nShorter reaction time — advanced.")
                         : Loc.T("잠김 — 엔딩을 한 번 보면 열려.\n속도 ×1.3 · 코인 ×1.3 (고급)", "Locked — see one ending to unlock.\nSpeed ×1.3 · Coins ×1.3 (advanced)"),
                unlocked ? new Color(1f, 0.55f, 0.15f) : new Color(0.35f, 0.36f, 0.42f), 0.36f, unlocked,
                () => StartNewPlaythrough(RunMode.Skateboard), "Sched_dev_skate", true);

            CreateMenuButton(_charSelectPanel.transform, "닫기", 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_charSelectPanel, false);
            }, absoluteBottom: true);
            _charSelectPanel.SetActive(false);
        }

        private void BuildCharCard(Transform parent, string title, string body, Color color, float anchorY, bool enabled,
            UnityEngine.Events.UnityAction onClick, string artName = null, bool cover = false)
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
            // 9차: 카드 왼쪽에 그림(투명 PNG는 그대로, 삽화는 둥근 마스크 cover) — 글자만 있던 첫 선택 화면에 얼굴을.
            var tex = string.IsNullOrEmpty(artName) ? null : ArtAssets.LoadTexture(artName);
            float left = 24f;
            if (tex != null)
            {
                left = 190f;
                var frameGo = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(Mask));
                frameGo.transform.SetParent(card.transform, false);
                var frt = frameGo.GetComponent<RectTransform>();
                frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(0f, 1f); frt.pivot = new Vector2(0f, 0.5f);
                frt.anchoredPosition = new Vector2(12f, 0f); frt.sizeDelta = new Vector2(160f, -16f);
                var fi = frameGo.GetComponent<Image>(); fi.sprite = CoastUiArt.RoundedRect(18); fi.type = Image.Type.Sliced;
                fi.color = cover ? Color.white : new Color(1f, 1f, 1f, 0.16f); fi.raycastTarget = false;
                frameGo.GetComponent<Mask>().showMaskGraphic = true;
                var pic = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                pic.transform.SetParent(frameGo.transform, false);
                pic.sprite = CoastUiArt.AsSprite(tex); pic.raycastTarget = false;
                var prt = pic.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
                if (cover)
                {
                    var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = tex.width / (float)tex.height;
                }
                else { pic.preserveAspect = true; prt.offsetMin = new Vector2(4f, 4f); prt.offsetMax = new Vector2(-4f, -4f); }
                if (!enabled) pic.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            }
            var t = CreateLabel(card.transform, "T", title + (enabled ? "" : Loc.T("  (잠김)", "  (locked)")), 28, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.74f), new Vector2(520f, 44f));
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.anchorMin = new Vector2(0f, 0.74f); t.rectTransform.anchorMax = new Vector2(1f, 0.74f);
            t.rectTransform.sizeDelta = new Vector2(0f, 44f); t.rectTransform.offsetMin = new Vector2(left, -22f); t.rectTransform.offsetMax = new Vector2(-16f, 22f);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var b = CreateLabel(card.transform, "B", body, 16, FontStyle.Normal, new Color(1f, 1f, 1f, enabled ? 0.95f : 0.7f),
                new Vector2(0.5f, 0.36f), new Vector2(520f, 90f));
            b.alignment = TextAnchor.MiddleLeft;
            b.rectTransform.anchorMin = new Vector2(0f, 0.36f); b.rectTransform.anchorMax = new Vector2(1f, 0.36f);
            b.rectTransform.sizeDelta = new Vector2(0f, 90f); b.rectTransform.offsetMin = new Vector2(left, -45f); b.rectTransform.offsetMax = new Vector2(-16f, 45f);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private Button _startButton;
        private Text _tapLabel;
        private GameObject _recordPanel;

        private void Update()
        {
            AnimateMore();
            if (_tapLabel != null && _ready)
            {
                float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
                var c = _tapLabel.color;
                c.a = a;
                _tapLabel.color = c;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터 검증용: N = 캐릭터 선택, 1 = 러닝, 2 = 스케이트보드, C = 이어하기, Escape = 닫기.
            if (_aiNoticeOk != null && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) { _aiNoticeOk(); return; }
            if (!_ready) return;
            if (Input.GetKeyDown(KeyCode.M))
            {
                // 14차-8 디버그: 마우스 아래 UI 레이캐스트 결과
                var es = UnityEngine.EventSystems.EventSystem.current;
                var pd = new UnityEngine.EventSystems.PointerEventData(es) { position = Input.mousePosition };
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                es.RaycastAll(pd, hits);
                var sb = new System.Text.StringBuilder("[UI raycast] " + Input.mousePosition + " es=" + es.name + " module=" + es.currentInputModule);
                foreach (var h in hits) sb.Append("\n  ").Append(h.gameObject.name).Append(" <- ").Append(h.gameObject.transform.parent ? h.gameObject.transform.parent.name : "");
                if (_moreBtn != null)
                {
                    var brt = _moreBtn.GetComponent<RectTransform>();
                    var c = new Vector3[4]; brt.GetWorldCorners(c);
                    sb.Append("\n moreBtn corners ").Append(c[0]).Append(" .. ").Append(c[2])
                      .Append(" contains=").Append(RectTransformUtility.RectangleContainsScreenPoint(brt, Input.mousePosition))
                      .Append(" active=").Append(_moreBtn.gameObject.activeInHierarchy).Append(" screen=").Append(Screen.width).Append("x").Append(Screen.height)
                      .Append(" raycaster=").Append(_canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null).Append(" canvasEnabled=").Append(_canvas.enabled);
                }
                Debug.Log(sb.ToString());
            }
            if (Input.GetKeyDown(KeyCode.N)) ShowPanel(_charSelectPanel, true);
            if (Input.GetKeyDown(KeyCode.C)) OnContinue();
            if (Input.GetKeyDown(KeyCode.L)) { Loc.Toggle(); UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); }
            if (Input.GetKeyDown(KeyCode.S)) ShowPanel(_settingsPanel, true);
            if (Input.GetKeyDown(KeyCode.A)) ArcadeUI.Open(false);
            if (Input.GetKeyDown(KeyCode.K)) CollectionUI.Open(null, 3);
            // V/B/T: 사이드 씬·엔딩 변형·진엔딩 미리보기
            if (Input.GetKeyDown(KeyCode.V)) ChapterVN.Play("SIDE_RUA_3", null);
            if (Input.GetKeyDown(KeyCode.B)) ChapterVN.Play("END_A_TRUST", null);
            if (Input.GetKeyDown(KeyCode.T)) ChapterVN.Play("END_TRUE", null);
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
            CreateLabel(_settingsPanel.transform, "T", Loc.T("설정", "Settings"), 32, FontStyle.Bold,
                Color.white, new Vector2(0.5f, 0.80f), new Vector2(400f, 44f));
            // 9차: 볼륨·진동을 추가하고 줄 간격을 좁혀 카드가 비어 보이지 않게. (위에서부터 소리 → 진동 → 펫 → 언어 → 크레딧)
            Text volLabel = null;
            var volBtn = CreateMenuButton(_settingsPanel.transform, Loc.T("소리", "Sound"), 0.70f, () =>
            {
                CoastPrefs.VolumeStep = (CoastPrefs.VolumeStep + 4) % 5;   // 100 → 75 → 50 → 25 → OFF → 100
                if (volLabel != null) volLabel.text = VolumeText();
            });
            volLabel = volBtn.GetComponentInChildren<Text>();
            if (volLabel != null) volLabel.text = VolumeText();
            Text hapLabel = null;
            var hapBtn = CreateMenuButton(_settingsPanel.transform, Loc.T("진동", "Vibration"), 0.62f, () =>
            {
                CoastPrefs.Haptic = !CoastPrefs.Haptic;
                if (hapLabel != null) hapLabel.text = HapticText();
            });
            hapLabel = hapBtn.GetComponentInChildren<Text>();
            if (hapLabel != null) hapLabel.text = HapticText();

            // Pet picker — cycles through the three companions; the run reads
            // PetCompanion.Selected when it builds the pet.
            Text petLabel = null;
            var petBtn = CreateMenuButton(_settingsPanel.transform, "펫", 0.54f, () =>
            {
                PetCompanion.Selected = (PetKind)(((int)PetCompanion.Selected + 1) % 4);
                if (petLabel != null)
                    petLabel.text = PetLabel();
            });
            petLabel = petBtn.GetComponentInChildren<Text>();
            if (petLabel != null)
            {
                petLabel.text = PetLabel();
                petLabel.fontSize = CoastHudLayout.Scaled(17);
            }
            // 언어 토글: 바꾸면 타이틀을 다시 열어 모든 문구·대문 아트를 새 언어로 만든다.
            CreateMenuButton(_settingsPanel.transform, Loc.T($"언어: 한국어  →  {Loc.Native(Loc.NextLang)}", Loc.Tr("Language") + $": {Loc.Native(Loc.Lang)}  →  {Loc.Native(Loc.NextLang)}"), 0.46f, () =>
            {
                _audio?.PlayClick();
                Loc.Toggle();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
            CreateMenuButton(_settingsPanel.transform, Loc.T("크레딧", "Credits"), 0.38f, () =>
            {
                ShowPanel(_settingsPanel, false);
                ShowPanel(_creditsPanel, true);
            });
            CreateLabel(_settingsPanel.transform, "Ver", "v0.9  ·  Coast Run · Jeju", 14, FontStyle.Normal,
                new Color(1f, 0.95f, 0.85f, 0.55f), new Vector2(0.5f, 0.27f), new Vector2(400f, 24f));
            CreateMenuButton(_settingsPanel.transform, Loc.T("닫기", "Close"), 0.12f, () =>
            {
                _audio?.PlayClick();
                ShowPanel(_settingsPanel, false);
            }, absoluteBottom: true);
            _settingsPanel.SetActive(false);
        }

        private static string VolumeText()
        {
            int s = CoastPrefs.VolumeStep;
            string bar = new string('■', s) + new string('□', 4 - s);
            return (Loc.IsKo ? "소리  " : "Sound  ") + bar + "  " + CoastPrefs.VolumeLabel(s);
        }

        private static string HapticText() => (Loc.IsKo ? "진동  " : "Vibration  ") + (CoastPrefs.Haptic ? "ON" : "OFF");

        private static string PetLabel()
        {
            int k = (int)PetCompanion.Selected;
            string name = Loc.Data("pet." + PetCompanion.Names[k], PetCompanion.Names[k]);
            string[] blurbsEn = { "no pet", "coins ×1.2 while running", "smashes blocking obstacles (12 s cooldown, ×3)", "pulls coins & hearts within 7 m", "saves you once per run (40% HP)" };
            return (Loc.IsKo ? "펫: " : "Pet: ") + name + "  ▸  " + (Loc.IsKo ? PetCompanion.Blurbs[k] : blurbsEn[Mathf.Clamp(k, 0, blurbsEn.Length - 1)]);
        }

        // 14차-8: 더보기 슬라이드
        private Button _moreBtn; private Text _moreLabel; private Text _openingHint;
        private RectTransform _moreRt; private CanvasGroup _moreCg;
        private Vector2 _moreHidden, _moreShown; private bool _moreOpen; private float _moreT;

        private void ToggleMore()
        {
            _audio?.PlayClick();
            _moreOpen = !_moreOpen;
            if (_moreLabel != null) _moreLabel.text = _moreOpen ? Loc.T("닫기", "Close") : Loc.T("더보기", "More");
            if (_moreCg != null) { _moreCg.interactable = _moreOpen; _moreCg.blocksRaycasts = _moreOpen; }
        }

        private void AnimateMore()
        {
            if (_moreRt == null) return;
            float goal = _moreOpen ? 1f : 0f;
            if (Mathf.Approximately(_moreT, goal)) return;
            _moreT = Mathf.MoveTowards(_moreT, goal, Time.unscaledDeltaTime * 4.5f);
            float e = 1f - Mathf.Pow(1f - _moreT, 3f);   // ease-out
            _moreRt.anchoredPosition = Vector2.LerpUnclamped(_moreHidden, _moreShown, e);
            _moreCg.alpha = _moreT;
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
            // 8차: 검정 반투명 → 반투명 딤 + 가운데 크라프트지 카드(금테). 글은 그대로 흰색.
            go.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.06f, 0.55f);
            var card = CoastUiArt.Panel(go.transform, "Card", new Color(0.83f, 0.69f, 0.22f, 0.9f), 26);
            // 18차: 카드는 화면 높이의 6~94% — 안의 버튼이 화면 비율(anchorY)로 놓이므로 긴 폰에서도 카드 밖으로 안 나간다
            var crt = card.rectTransform; crt.anchorMin = new Vector2(0.5f, 0.06f); crt.anchorMax = new Vector2(0.5f, 0.94f); crt.sizeDelta = new Vector2(620f, 0f); crt.anchoredPosition = Vector2.zero;
            card.raycastTarget = false;
            var inner = CoastUiArt.Panel(card.transform, "Inner", new Color(0.24f, 0.17f, 0.13f, 0.96f), 24);
            var irt = inner.rectTransform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = new Vector2(3f, 3f); irt.offsetMax = new Vector2(-3f, -3f);
            inner.raycastTarget = false;
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
                rt.anchoredPosition = new Vector2(0f, 118f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, anchorY);
                rt.anchorMax = new Vector2(0.5f, anchorY);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            rt.sizeDelta = new Vector2(400f, 58f);
            var img = go.GetComponent<Image>();
            img.sprite = CoastUiArt.RoundedRect(16); img.type = Image.Type.Sliced;
            img.color = new Color(1f, 0.92f, 0.72f, 0.16f);
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
            label.fontSize = CoastHudLayout.Scaled(size);
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = text;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;   // 11차: 글자를 키우면서 세로 잘림 방지
            return label;
        }
    }
}
