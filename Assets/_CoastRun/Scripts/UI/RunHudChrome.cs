using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// The endless-runner top row, laid out the way Subway Surfers players expect:
    ///
    ///   ┌────┐                       ┌──────────────────┐
    ///   │ ⏸ │                       │ x3 ★   0 3 6 2 4 │  score + multiplier badge
    ///   └────┘                       └──────────────────┘
    ///                                    ┌────────────┐
    ///                                    │  ◎   89    │  coins
    ///                                    └────────────┘
    ///
    /// Score climbs with distance and multiplies with the near-miss combo, so it moves
    /// every frame and rewards risk. The old design stripped all chrome for the story;
    /// the chapter-5 fade-outs still work because the pills expose their CanvasGroups.
    public class RunHudChrome : MonoBehaviour
    {
        public static readonly Color PillNavy = new Color(0.10f, 0.14f, 0.30f, 0.92f);
        public static readonly Color ScoreYellow = new Color(1f, 0.85f, 0.25f, 1f);
        public static readonly Color BadgeOrange = new Color(1f, 0.55f, 0.15f, 1f);
        public const string BestScoreKey = "CoastRun.BestScore";

        public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public static RunHudChrome Instance { get; private set; }
        private float _nextBestCheck;

        // Cookie-Run additions: stamina bar, bonus-time banner, run-over panel.
        private Image _hpFill;
        private RectTransform _hpBar;
        private Text _hpText;
        private CanvasGroup _hpCg;
        private GameObject _bonusBanner;
        private Image _bonusFill;
        private Text _bonusLabel;
        private GameObject _runOverOverlay;
        private Image _flash;
        private float _hpShown = 1f;
        private float _hpShake;
        private int _floatEvery;

        private Canvas _canvas;
        private PlayerController _player;
        private CoinWallet _wallet;

        private Text _scoreText;
        private Text _multText;
        private Text _coinText;
        private RectTransform _multBadge;
        private CanvasGroup _scoreCg;
        private CanvasGroup _coinCg;
        private CanvasGroup _multCg;

        private float _distanceScore;
        private int _bonus;
        private int _combo = 1;
        private float _comboExpire;
        private int _shownScore = -1;
        private int _shownCoins = -1;

        private GameObject _pauseOverlay;
        private bool _paused;

        public CanvasGroup ScoreGroup => _scoreCg;
        public CanvasGroup ComboGroup => _multCg;
        public CanvasGroup CoinGroup => _coinCg;
        public Text CoinText => _coinText;
        public int Score => Mathf.FloorToInt(_distanceScore) + _bonus;
        public bool IsPaused => _paused;

        public void Build(Canvas canvas, PlayerController player, CoinWallet wallet, NearMissSystem nearMiss)
        {
            _canvas = canvas;
            _player = player;
            _wallet = wallet;
            var root = CoastUiCanvas.Root(canvas);

            Instance = this;
            BuildPause(root);
            BuildScorePill(root);
            BuildCoinPill(root);
            BuildHealthBar(root);
            BuildBonusBanner(root);
            BuildFlash(root);

            var health = HealthSystem.Instance;
            if (health != null)
            {
                health.OnChanged -= HandleHealth;
                health.OnChanged += HandleHealth;
                health.OnDamaged -= HandleDamaged;
                health.OnDamaged += HandleDamaged;
                HandleHealth(health.Current, health.Max);
            }

            if (nearMiss != null)
            {
                nearMiss.OnNearMissRewarded -= HandleNearMiss;
                nearMiss.OnNearMissRewarded += HandleNearMiss;
            }
            if (_wallet != null)
            {
                _wallet.OnCoinsChanged -= HandleCoins;
                _wallet.OnCoinsChanged += HandleCoins;
            }
            RefreshCoins();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (_wallet != null)
                _wallet.OnCoinsChanged -= HandleCoins;
            var health = HealthSystem.Instance;
            if (health != null)
            {
                health.OnChanged -= HandleHealth;
                health.OnDamaged -= HandleDamaged;
            }
            // The pause and run-over overlays freeze time; if the HUD goes away while
            // one is up (scene change, editor stop), unfreeze — a frozen timeScale
            // survives into the next session and every WaitForSeconds hangs.
            if (_paused || _runOverOverlay != null)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
            SaveBest();
        }

        // ── Cookie-Run HUD ───────────────────────────────────────────────────

        /// GameSession creates HealthSystem after the chrome; call once it exists.
        public void RebindHealth()
        {
            var health = HealthSystem.Instance;
            if (health == null)
                return;
            health.OnChanged -= HandleHealth;
            health.OnChanged += HandleHealth;
            health.OnDamaged -= HandleDamaged;
            health.OnDamaged += HandleDamaged;
            HandleHealth(health.Current, health.Max);
        }

        private void BuildHealthBar(RectTransform root)
        {
            // Under the pause button, left-aligned: the thing you glance at most.
            var track = CoastUiArt.CutePill(root, "HpBar", new Color(0.08f, 0.12f, 0.26f, 0.95f), 18);
            _hpBar = track.rectTransform;
            _hpBar.anchorMin = _hpBar.anchorMax = new Vector2(0f, 1f);
            _hpBar.pivot = new Vector2(0f, 1f);
            _hpBar.anchoredPosition = new Vector2(6f, -88f);
            _hpBar.sizeDelta = new Vector2(330f, 40f);
            _hpCg = track.gameObject.AddComponent<CanvasGroup>();

            var fill = CoastUiArt.Panel(_hpBar, "Fill", new Color(1f, 0.42f, 0.55f, 1f), 11);
            _hpFill = fill;
            var frt = fill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(26f, 8f);
            frt.offsetMax = new Vector2(-8f, -8f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            // Heart badge on the left edge.
            var heartIcon = CoastUiArt.Icon("Heart");
            var heart = heartIcon != null
                ? CoastUiArt.Panel(_hpBar, "Heart", Color.white, 2)
                : CoastUiArt.Panel(_hpBar, "Heart", new Color(1f, 0.3f, 0.45f), 12);
            var hrt = heart.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(4f, 2f);
            hrt.sizeDelta = heartIcon != null ? new Vector2(58f, 58f) : new Vector2(40f, 40f);
            if (heartIcon != null)
            {
                heart.sprite = heartIcon;
                heart.type = Image.Type.Simple;
                heart.preserveAspect = true;
            }
            else
            {
                var hl = CoastHudLayout.MakeText(hrt, "Glyph", "♥", 26, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 2f));
                hl.color = Color.white;
            }

            _hpText = CoastHudLayout.MakeText(_hpBar, "Value", "100", 18, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-10f, 0f));
            _hpText.color = Color.white;
            _hpText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_hpText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.5f);
        }

        private void BuildBonusBanner(RectTransform root)
        {
            var banner = CoastUiArt.Panel(root, "BonusBanner", new Color(0.55f, 0.2f, 0.8f, 0.92f), 22);
            _bonusBanner = banner.gameObject;
            var rt = banner.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -210f);
            rt.sizeDelta = new Vector2(440f, 78f);

            _bonusLabel = CoastHudLayout.MakeText(rt, "Label", "BONUS TIME!", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.35f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _bonusLabel.color = ScoreYellow;
            _bonusLabel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);

            var track = CoastUiArt.Panel(rt, "Track", new Color(0f, 0f, 0f, 0.35f), 6);
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0.06f, 0.12f);
            trt.anchorMax = new Vector2(0.94f, 0.3f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            _bonusFill = CoastUiArt.Panel(trt, "Fill", ScoreYellow, 5);
            var brt = _bonusFill.rectTransform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            _bonusFill.type = Image.Type.Filled;
            _bonusFill.fillMethod = Image.FillMethod.Horizontal;
            _bonusBanner.SetActive(false);
        }

        private void BuildFlash(RectTransform root)
        {
            _flash = CoastHudLayout.MakeImage(root, "Flash", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad),
                new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(1f, 1f, 1f, 0f));
            _flash.raycastTarget = false;
            _flash.transform.SetAsFirstSibling();
        }

        private void HandleHealth(float current, float max)
        {
            if (_hpText != null)
                _hpText.text = Mathf.CeilToInt(current).ToString();
        }

        private void HandleDamaged(float amount)
        {
            _hpShake = 0.35f;
            Flash(new Color(1f, 0.2f, 0.2f, 0.3f));
        }

        public void Flash(Color c)
        {
            if (_flash == null)
                return;
            _flash.color = c;
        }

        /// Jelly / potion / star points. `big` shows a floating number; jellies only
        /// float every fifth pickup so a trail does not paper the screen.
        public void AddScore(int amount, Vector3 worldPos, bool big)
        {
            _bonus += amount * Mathf.Max(1, _combo);
            if (big || (++_floatEvery % 5) == 0)
            {
                var fb = GetComponent<UI_FeedbackController>();
                fb?.ShowFloatingReward(worldPos, amount * Mathf.Max(1, _combo), big ? 3 : 1);
            }
        }

        public void ShowBonusBanner(bool on)
        {
            if (_bonusBanner != null)
                _bonusBanner.SetActive(on);
            if (on && _bonusBanner != null)
                StartCoroutine(SimpleTween.PunchScale(_bonusBanner.transform, 0.3f, 0.35f));
        }

        public void SetBonusProgress(float t)
        {
            if (_bonusFill != null)
                _bonusFill.fillAmount = t;
            if (_bonusLabel != null)
                _bonusLabel.color = Color.Lerp(ScoreYellow, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f));
        }

        /// Stamina hit zero. Freezes time and offers retry / title.
        public void ShowRunOver(UnityEngine.Events.UnityAction retry, UnityEngine.Events.UnityAction toTitle)
        {
            if (_runOverOverlay != null)
                Destroy(_runOverOverlay);
            Time.timeScale = 0f;
            AudioListener.pause = true;

            var canvas = CoastUiCanvas.Create("RunOverOverlay", 410);
            _runOverOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad),
                new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(0.15f, 0.02f, 0.06f, 0.78f));
            dim.raycastTarget = true;

            var panel = CoastUiArt.Panel(root, "Panel", new Color(0.97f, 0.95f, 0.90f, 1f), 28);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(500f, 360f);
            panel.raycastTarget = true;

            var title = CoastHudLayout.MakeText(prt, "Title", "체력이 다 떨어졌어…", 36, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, -24f));
            title.color = PillNavy;
            var sub = CoastHudLayout.MakeText(prt, "Sub", "젤리를 먹으면서 달려야 해. 포션은 크게 회복돼.", 18,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -140f), new Vector2(0f, -100f));
            sub.color = new Color(0.3f, 0.32f, 0.4f);

            MakeBigButton(prt, "Retry", "다시 달리기", new Color(0.30f, 0.72f, 0.36f), -210f, () =>
            {
                CloseRunOver();
                retry?.Invoke();
            });
            MakeBigButton(prt, "Title", "메인으로", new Color(0.35f, 0.45f, 0.70f), -290f, () =>
            {
                CloseRunOver();
                toTitle?.Invoke();
            });
        }

        private void CloseRunOver()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (_runOverOverlay != null)
                Destroy(_runOverOverlay);
            _runOverOverlay = null;
        }

        private void SaveBest()
        {
            int score = Score;
            if (score > BestScore)
            {
                PlayerPrefs.SetInt(BestScoreKey, score);
                PlayerPrefs.Save();
            }
        }

        // ── Layout ───────────────────────────────────────────────────────────

        private void BuildPause(RectTransform root)
        {
            var outer = CoastUiArt.CutePill(root, "PauseButton", PillNavy, 20);
            var go = outer.gameObject;
            outer.raycastTarget = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -6f);
            rt.sizeDelta = new Vector2(72f, 72f);
            go.AddComponent<Button>();

            // Two bars — no glyph font dependency.
            for (int i = 0; i < 2; i++)
            {
                var bar = CoastHudLayout.MakeImage(go.transform, "Bar" + i,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(i == 0 ? -15f : 5f, -14f), new Vector2(i == 0 ? -5f : 15f, 14f), Color.white);
                bar.raycastTarget = false;
            }

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(TogglePause);
        }

        private void BuildScorePill(RectTransform root)
        {
            var pill = CoastUiArt.CutePill(root, "ScorePill", PillNavy, 24);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -6f);
            rt.sizeDelta = new Vector2(300f, 66f);
            _scoreCg = pill.gameObject.AddComponent<CanvasGroup>();

            _scoreText = CoastHudLayout.MakeText(rt, "Score", "00000", 36, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(110f, 0f), new Vector2(-18f, 0f));
            _scoreText.color = ScoreYellow;
            _scoreText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_scoreText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);

            // Multiplier badge: orange lozenge with a big star poking out of the pill.
            var badge = CoastUiArt.CutePill(rt, "MultBadge", BadgeOrange, 16, 3);
            _multBadge = badge.rectTransform;
            _multBadge.anchorMin = _multBadge.anchorMax = new Vector2(0f, 0.5f);
            _multBadge.pivot = new Vector2(0f, 0.5f);
            _multBadge.anchoredPosition = new Vector2(8f, 0f);
            _multBadge.sizeDelta = new Vector2(104f, 48f);
            _multCg = badge.gameObject.AddComponent<CanvasGroup>();
            var star = CoastUiArt.Icon("Star");
            if (star != null)
            {
                var sgo = new GameObject("Star", typeof(RectTransform), typeof(Image));
                sgo.transform.SetParent(_multBadge, false);
                var srt = sgo.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(-2f, 6f);
                srt.sizeDelta = new Vector2(54f, 54f);
                var si = sgo.GetComponent<Image>();
                si.sprite = star; si.preserveAspect = true; si.raycastTarget = false;
            }
            _multText = CoastHudLayout.MakeText(_multBadge, "Mult", "x1", 26, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(star != null ? -30f : -6f, 0f));
            _multText.color = Color.white;
            _multText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_multText, new Color(0.45f, 0.18f, 0.02f, 0.9f), 1.5f);
        }

        private void BuildCoinPill(RectTransform root)
        {
            var pill = CoastUiArt.CutePill(root, "CoinPill", PillNavy, 22);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -80f);
            rt.sizeDelta = new Vector2(176f, 56f);
            _coinCg = pill.gameObject.AddComponent<CanvasGroup>();

            var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rt, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(1f, 0.5f);
            irt.pivot = new Vector2(1f, 0.5f);
            irt.anchoredPosition = new Vector2(-4f, 4f);
            irt.sizeDelta = new Vector2(50f, 50f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_Coin"), 100f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            _coinText = CoastHudLayout.MakeText(rt, "Coins", "0", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-56f, 0f));
            _coinText.color = ScoreYellow;
            _coinText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_coinText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);
        }

        // ── Runtime ──────────────────────────────────────────────────────────

        private void Update()
        {
            UpdateCookieHud();
            if (_player != null && _player.Speed > 0.5f)
                _distanceScore += _player.Speed * Time.deltaTime * 2f * _combo;

            if (_combo > 1 && Time.time > _comboExpire)
                SetCombo(1);

            int score = Score;
            if (score != _shownScore)
            {
                _shownScore = score;
                if (_scoreText != null)
                    _scoreText.text = score.ToString("00000");
            }

            // Cheap periodic flush so a crash or a scene swap never loses a record.
            if (Time.unscaledTime > _nextBestCheck)
            {
                _nextBestCheck = Time.unscaledTime + 5f;
                SaveBest();
            }
        }


        private void UpdateCookieHud()
        {
            var health = HealthSystem.Instance;
            if (_hpFill != null && health != null)
            {
                _hpShown = Mathf.Lerp(_hpShown, health.Normalized, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
                _hpFill.fillAmount = _hpShown;
                bool low = _hpShown < 0.25f;
                Color c = low
                    ? Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(1f, 0.6f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f))
                    : Color.Lerp(new Color(1f, 0.42f, 0.55f), new Color(0.45f, 0.9f, 0.5f), _hpShown);
                if (health.Frozen)
                    c = ScoreYellow;
                _hpFill.color = c;
            }
            if (_hpBar != null)
            {
                if (_hpShake > 0f)
                {
                    _hpShake -= Time.unscaledDeltaTime;
                    float k = _hpShake / 0.35f;
                    _hpBar.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 6f * k, -84f);
                }
                else
                    _hpBar.anchoredPosition = new Vector2(0f, -84f);
            }
            if (_flash != null && _flash.color.a > 0f)
            {
                var c = _flash.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 1.2f);
                _flash.color = c;
            }
        }

        private void HandleNearMiss(int reward, int combo, Vector3 worldPos)
        {
            _bonus += reward;
            SetCombo(Mathf.Clamp(combo, 1, 9));
            _comboExpire = Time.time + 4f;
            StartCoroutine(SimpleTween.PunchScale(_multBadge, 0.25f, 0.18f));
        }

        private void SetCombo(int combo)
        {
            _combo = Mathf.Max(1, combo);
            if (_multText != null)
                _multText.text = "x" + _combo;
        }

        private void HandleCoins(int total, int delta) => RefreshCoins();

        private void RefreshCoins()
        {
            if (_coinText == null || _wallet == null)
                return;
            int c = _wallet.TotalCoins;
            if (c == _shownCoins)
                return;
            _shownCoins = c;
            _coinText.text = c.ToString();
            StartCoroutine(SimpleTween.PunchScale(_coinText.transform, 0.15f, 0.12f));
        }

        // ── Pause ────────────────────────────────────────────────────────────

        public void TogglePause()
        {
            if (_paused) Resume(); else Pause();
        }

        public void Pause()
        {
            if (_paused || _player == null || _player.Speed < 0.5f)
                return;
            _paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (_pauseOverlay == null)
                BuildPauseOverlay();
            _pauseOverlay.SetActive(true);
        }

        public void Resume()
        {
            if (!_paused)
                return;
            _paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (_pauseOverlay != null)
                _pauseOverlay.SetActive(false);
        }

        private void BuildPauseOverlay()
        {
            var canvas = CoastUiCanvas.Create("PauseOverlay", 400);
            _pauseOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);

            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad),
                new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(0.02f, 0.05f, 0.12f, 0.72f));
            dim.raycastTarget = true;

            var panel = CoastUiArt.Panel(root, "Panel", new Color(0.97f, 0.95f, 0.90f, 1f), 28);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(480f, 380f);
            panel.raycastTarget = true;

            var title = CoastHudLayout.MakeText(prt, "Title", "일시정지", 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, -24f));
            title.color = PillNavy;

            MakeBigButton(prt, "Resume", "계속하기", new Color(0.30f, 0.72f, 0.36f), -150f, Resume);
            MakeBigButton(prt, "Retry", "다시 시작", BadgeOrange, -230f, () =>
            {
                Resume();
                StageManager.Instance?.RetryCurrent();
            });
            MakeBigButton(prt, "Title", "메인으로", new Color(0.35f, 0.45f, 0.70f), -310f, () =>
            {
                Resume();
                var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
                if (flow != null)
                    _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
            });
        }

        public static Button MakeBigButton(Transform parent, string name, string label, Color color, float y,
            UnityEngine.Events.UnityAction onClick)
        {
            var img = CoastUiArt.Panel(parent, name, color, 20);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(360f, 64f);
            img.raycastTarget = true;

            var text = CoastHudLayout.MakeText(rt, "Label", label, 28, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.color = Color.white;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
