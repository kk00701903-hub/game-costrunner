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
        private UnityEngine.Events.UnityAction _runOverRetry, _runOverSecond;
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
            // (노을 시계는 UI_FinalDestinationController 의 여정 바/타이머가 맡는다 — BuildSunMeter 는 예비)
            BuildHeartsGoal(root);
            BuildBonusBanner(root);
            BuildTutorial(root);
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

        // ── 8차 노을 규칙 HUD ─────────────────────────────────────────
        private RectTransform _sunBar; private RectTransform _sunDot; private Text _sunLabel; private Image _sunTrack; private CanvasGroup _sunCg;
        private bool _sunLate; private float _sunPulse;

        private void BuildSunMeter(RectTransform root)
        {
            // 체력바 아래, 같은 왼쪽 정렬. 노랑(낮) → 주황(노을) → 남보라(밤) 띠 위를 해가 오른쪽으로 간다.
            var track = CoastUiArt.CutePill(root, "SunBar", new Color(0.08f, 0.12f, 0.26f, 0.95f), 14, 3);
            _sunBar = track.rectTransform;
            _sunBar.anchorMin = _sunBar.anchorMax = new Vector2(0f, 1f);
            _sunBar.pivot = new Vector2(0f, 1f);
            _sunBar.anchoredPosition = new Vector2(6f, -134f);
            _sunBar.sizeDelta = new Vector2(330f, 26f);
            _sunCg = track.gameObject.AddComponent<CanvasGroup>();
            var grad = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++)
            {
                float u = x / 63f;
                Color c = u < 0.55f ? Color.Lerp(new Color(1f, 0.86f, 0.35f), new Color(1f, 0.55f, 0.25f), u / 0.55f)
                                    : Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(0.30f, 0.22f, 0.55f), (u - 0.55f) / 0.45f);
                grad.SetPixel(x, 0, c);
            }
            grad.Apply();
            _sunTrack = CoastHudLayout.MakeImage(_sunBar, "Track", Vector2.zero, Vector2.one, new Vector2(8f, 7f), new Vector2(-8f, -7f), Color.white);
            _sunTrack.sprite = Sprite.Create(grad, new Rect(0, 0, 64, 1), new Vector2(0.5f, 0.5f), 100f);
            _sunTrack.type = Image.Type.Simple; _sunTrack.raycastTarget = false;
            var dot = CoastUiArt.Panel(_sunBar, "Sun", new Color(1f, 0.95f, 0.6f, 1f), 9);
            _sunDot = dot.rectTransform;
            _sunDot.anchorMin = _sunDot.anchorMax = new Vector2(0f, 0.5f);
            _sunDot.pivot = new Vector2(0.5f, 0.5f);
            _sunDot.sizeDelta = new Vector2(18f, 18f);
            _sunDot.anchoredPosition = new Vector2(16f, 0f);
            var glow = CoastUiArt.Panel(_sunDot, "Glow", new Color(1f, 0.85f, 0.4f, 0.45f), 13);
            var grt = glow.rectTransform; grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one; grt.offsetMin = new Vector2(-5f, -5f); grt.offsetMax = new Vector2(5f, 5f);
            glow.raycastTarget = false;
            _sunLabel = CoastHudLayout.MakeText(_sunBar, "Label", Loc.T("노을까지", "until sunset"), 12, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-10f, 0f));
            _sunLabel.color = new Color(1f, 1f, 1f, 0.9f);
            CoastUiArt.OutlineText(_sunLabel, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
        }

        /// StageManager 가 매 프레임 호출. tau 0..1 = 해가 지기까지 남은 시간 비율.
        public void SetSunset(float tau, bool late)
        {
            if (_sunBar == null) return;
            float w = _sunBar.sizeDelta.x - 32f;
            _sunDot.anchoredPosition = new Vector2(16f + w * Mathf.Clamp01(tau), late ? -2f : 0f);
            if (!late)
            {
                float left = 1f - tau;
                _sunLabel.text = left > 0.3f ? Loc.T("노을까지", "until sunset") : Loc.T("해가 진다…", "sun is setting…");
                _sunLabel.color = left > 0.3f ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.75f, 0.45f, 1f);
            }
        }

        public void OnSunsetPassed()
        {
            _sunLate = true;
            if (_sunLabel != null) { _sunLabel.text = Loc.T("해가 졌어", "sun is down"); _sunLabel.color = new Color(1f, 0.45f, 0.45f, 1f); }
            GetComponent<UI_FeedbackController>()?.ShowWatchMessage(Loc.T("해가 졌어", "SUN DOWN"), Loc.T("늦었어… 그래도 달려.", "Late… keep running."));
        }


        /// 14차-7: 레퍼런스 HUD — 하트 1개 + 초록 게이지 + 숫자(0~100). 하트 3개는 크고 답답했다.
        private Image _hpGaugeFill; private Text _hpGaugeText; private RectTransform _hpHeartRt;
        private void BuildHealthBar(RectTransform root)
        {
            var wrap = new GameObject("Hearts", typeof(RectTransform), typeof(CanvasGroup));
            wrap.transform.SetParent(root, false);
            _hpBar = wrap.GetComponent<RectTransform>();
            _hpBar.anchorMin = _hpBar.anchorMax = new Vector2(0f, 1f);
            _hpBar.pivot = new Vector2(0f, 1f);
            _hpBar.anchoredPosition = new Vector2(6f, -84f);
            _hpBar.sizeDelta = new Vector2(190f, 48f);
            _hpCg = wrap.GetComponent<CanvasGroup>();

            // 게이지 트랙(남색 알약 + 크림 테두리)
            var track = CoastUiArt.CutePill(_hpBar, "Track", PillNavy, 14, 3);
            var trt = track.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f); trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(26f, -9f); trt.sizeDelta = new Vector2(150f, 30f);
            track.raycastTarget = false;

            var fill = CoastUiArt.Panel(trt, "Fill", new Color(0.45f, 0.9f, 0.25f, 1f), 10);
            var frt = fill.rectTransform; frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(5f, 5f); frt.offsetMax = new Vector2(-5f, -5f);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f; fill.raycastTarget = false;
            _hpGaugeFill = fill;
            // 윗면 하이라이트(광택)
            var shine = CoastUiArt.Panel(frt, "Shine", new Color(1f, 1f, 1f, 0.28f), 6);
            var srt = shine.rectTransform; srt.anchorMin = new Vector2(0f, 0.55f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(4f, 0f); srt.offsetMax = new Vector2(-4f, -3f); shine.raycastTarget = false;

            _hpGaugeText = CoastHudLayout.MakeText(trt, "Value", "100", 20, TextAnchor.MiddleRight,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-12f, 0f));
            _hpGaugeText.fontStyle = FontStyle.Bold; _hpGaugeText.color = Color.white; _hpGaugeText.raycastTarget = false;
            CoastUiArt.OutlineText(_hpGaugeText, new Color(0.05f, 0.07f, 0.18f, 0.95f), 2f);

            // 하트 아이콘이 게이지 왼쪽 끝을 살짝 덮는다
            var heartIcon = CoastUiArt.Icon("Heart");
            var heart = CoastUiArt.Panel(_hpBar, "HeartIcon", Color.white, 12);
            _hpHeartRt = heart.rectTransform; _hpHeartRt.anchorMin = _hpHeartRt.anchorMax = new Vector2(0f, 1f); _hpHeartRt.pivot = new Vector2(0.5f, 0.5f);
            _hpHeartRt.anchoredPosition = new Vector2(26f, -24f); _hpHeartRt.sizeDelta = new Vector2(52f, 52f);
            if (heartIcon != null) { heart.sprite = heartIcon; heart.type = Image.Type.Simple; heart.preserveAspect = true; }
            heart.raycastTarget = false;
            _hpFill = _hpGaugeFill;
            _hpText = null;   // 숫자는 UpdateCookieHud 에서 퍼센트로 쓴다
        }

        // ── 9차: 스토리 런 목표 — 챕터 하트 (지금까지 + 이번 런) / 목표. 여정 바 아래 오른쪽.
        private RectTransform _heartsPill; private Text _heartsText; private int _shownHearts = -1; private float _heartsPop;

        private void BuildHeartsGoal(RectTransform root)
        {
            if (ArcadeRun.Active) return;
            var gm = GameManager.I;
            var rec = gm != null && gm.Save != null ? gm.Save.CurrentChapter : null;
            if (rec == null) return;
            // 10차: 스토리 런에선 점수 대신 하트 목표가 우상단 큰 알약 자리(점수 알약은 숨김) — 상단 밀집 해소.
            if (_scoreCg != null) _scoreCg.gameObject.SetActive(false);
            var pill = CoastUiArt.CutePill(root, "HeartsGoal", PillNavy, 24);
            _heartsPill = pill.rectTransform;
            _heartsPill.anchorMin = _heartsPill.anchorMax = new Vector2(1f, 1f);
            _heartsPill.pivot = new Vector2(1f, 1f);
            _heartsPill.anchoredPosition = new Vector2(-6f, -6f);
            _heartsPill.sizeDelta = new Vector2(232f, 66f);
            var icon = CoastUiArt.Icon("Heart");
            if (icon != null)
            {
                var im = CoastHudLayout.MakeImage(_heartsPill, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, -27f), new Vector2(60f, 27f), Color.white);
                im.sprite = icon; im.preserveAspect = true; im.raycastTarget = false;
            }
            _heartsText = CoastHudLayout.MakeText(_heartsPill, "Value", "", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-16f, 0f));
            _heartsText.color = new Color(1f, 0.85f, 0.9f);
            _heartsText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_heartsText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);
            RefreshHearts(true);
        }

        private void RefreshHearts(bool force = false)
        {
            if (_heartsText == null) return;
            var gm = GameManager.I;
            var rec = gm != null && gm.Save != null ? gm.Save.CurrentChapter : null;
            if (rec == null) return;
            int run = StageRunStats.Instance != null ? StageRunStats.Instance.Hearts : 0;
            int have = gm.Save.chapterHearts + run;
            if (!force && have == _shownHearts) return;
            bool grew = have > _shownHearts && _shownHearts >= 0;
            _shownHearts = have;
            _heartsText.text = $"{have} / {rec.heartsTarget}";
            _heartsText.color = have >= rec.heartsTarget ? new Color(1f, 0.93f, 0.5f) : new Color(1f, 0.85f, 0.9f);
            if (grew) _heartsPop = 0.22f;
        }

        // ── 9차 온보딩: 첫 런에만 조작 힌트 3줄(좌우·점프·슬라이드). 5초 뒤 저절로 사라진다. ──
        private void BuildTutorial(RectTransform root)
        {
            if (PlayerPrefs.GetInt("coast.tut.run", 0) != 0) return;
            PlayerPrefs.SetInt("coast.tut.run", 1);
            var group = new GameObject("RunTutorial", typeof(RectTransform), typeof(CanvasGroup));
            group.transform.SetParent(root, false);
            var grt = group.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0f, 120f);
            grt.sizeDelta = new Vector2(420f, 220f);
            var cg = group.GetComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
            var rows = new (string glyph, string text)[]
            {
                ("◀ ▶", Loc.T("좌우로 밀어 레인 이동", "Swipe left / right to change lane")),
                ("▲", Loc.T("위로 밀어 점프", "Swipe up to jump")),
                ("▼", Loc.T("아래로 밀어 슬라이드", "Swipe down to slide")),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var pill = CoastUiArt.CutePill(grt, "Row" + i, new Color(0.08f, 0.12f, 0.26f, 0.9f), 18, 3);
                pill.raycastTarget = false;
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f); prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -i * 68f); prt.sizeDelta = new Vector2(400f, 58f);
                var g = CoastHudLayout.MakeText(prt, "G", rows[i].glyph, 26, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(90f, 0f));
                g.color = ScoreYellow; g.fontStyle = FontStyle.Bold;
                var t = CoastHudLayout.MakeText(prt, "T", rows[i].text, 19, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(100f, 0f), new Vector2(-12f, 0f));
                t.color = Color.white; t.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(t, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
            }
            StartCoroutine(TutorialRoutine(cg));
        }

        private System.Collections.IEnumerator TutorialRoutine(CanvasGroup cg)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            float t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; cg.alpha = t / 0.35f; yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(4.5f);
            t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; cg.alpha = 1f - t / 0.5f; yield return null; }
            if (cg != null) Destroy(cg.gameObject);
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

        /// 짧은 안내 토스트 (펫 발동 등). UI_FeedbackController의 워치 메시지를 재사용.
        public void ShowToast(string text)
        {
            GetComponent<UI_FeedbackController>()?.ShowWatchMessage("PET", text);
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
        public void ShowRunOver(UnityEngine.Events.UnityAction retry, UnityEngine.Events.UnityAction toTitle, string secondLabel = "메인으로")
        {
            if (_runOverOverlay != null)
                Destroy(_runOverOverlay);
            Time.timeScale = 0f;
            AudioListener.pause = true;

            var canvas = CoastUiCanvas.Create("RunOverOverlay", 410);
            _runOverOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);
            // 8차: 세로 풀스크린 그림(UI_RunOver 810×1440 — 위: 연석에 앉아 숨 고르는 하늘, 아래: 빈 모래빛 길) 위에 글·버튼.
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f),
                new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), new Color(0.15f, 0.02f, 0.06f, 0.55f));
            dim.raycastTarget = true;
            var tex = ArtAssets.LoadTexture("UI_RunOver");
            RectTransform prt;
            if (tex != null)
            {
                var artGo = new GameObject("ArtMask", typeof(RectTransform), typeof(Image), typeof(Mask));
                artGo.transform.SetParent(root, false);
                var art = artGo.GetComponent<RectTransform>();
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
                art.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad); art.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
                var maskImg = artGo.GetComponent<Image>(); maskImg.color = Color.white; maskImg.raycastTarget = true;
                artGo.GetComponent<Mask>().showMaskGraphic = false;
                var pic = CoastHudLayout.MakeImage(art, "Art", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
                pic.sprite = CoastUiArt.AsSprite(tex); pic.raycastTarget = false;
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 810f / 1440f;
                pic.rectTransform.anchorMin = Vector2.zero; pic.rectTransform.anchorMax = Vector2.one;
                pic.rectTransform.offsetMin = Vector2.zero; pic.rectTransform.offsetMax = Vector2.zero;
                // 아래쪽을 살짝 어둡게 해 글이 읽히게
                var shade = CoastHudLayout.MakeImage(art, "Shade", new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.2f, 0.12f, 0.30f));
                shade.raycastTarget = false;
                prt = art;
            }
            else
            {
                var panel = CoastUiArt.Panel(root, "Panel", new Color(0.976f, 0.941f, 0.851f, 1f), 27);
                prt = panel.rectTransform;
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = new Vector2(520f, 560f);
                panel.raycastTarget = true;
            }

            var title = CoastHudLayout.MakeText(prt, "Title", Loc.T("체력이 다 떨어졌어…", "Out of stamina…"), 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -60f), new Vector2(0f, 0f));
            title.color = PillNavy;
            CoastUiArt.OutlineText(title, new Color(1f, 0.97f, 0.9f, 0.9f), 1.6f);
            var sub = CoastHudLayout.MakeText(prt, "Sub", Loc.T("젤리를 먹으면서 달려야 해. 포션은 크게 회복돼.", "Eat jellies as you run. Potions restore a lot."), 19,
                TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(30f, -100f), new Vector2(-30f, -62f));
            sub.color = new Color(0.30f, 0.26f, 0.32f);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            CoastUiArt.OutlineText(sub, new Color(1f, 0.97f, 0.9f, 0.8f), 1f);

            _runOverRetry = retry;
            _runOverSecond = toTitle;
            var b1 = MakeBigButton(prt, "Retry", Loc.T("다시 달리기", "Run again"), new Color(0.36f, 0.72f, 0.42f), 0f, () =>
            {
                CloseRunOver();
                retry?.Invoke();
            });
            b1.GetComponent<RectTransform>().anchorMin = b1.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            b1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -180f);
            var b2 = MakeBigButton(prt, "Title", secondLabel, new Color(0.42f, 0.52f, 0.78f), 0f, () =>
            {
                CloseRunOver();
                toTitle?.Invoke();
            });
            b2.GetComponent<RectTransform>().anchorMin = b2.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            b2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -262f);

            // 9차: 이번 런 기록 3칸(거리·하트·코인) — 빈 아래 절반을 채우고, 실패해도 뭘 얻었는지 보이게.
            var st = StageRunStats.Instance;
            var sm = StageManager.Instance;
            int dist = sm != null ? Mathf.RoundToInt(sm.StageLocalDistance) : 0;
            var stats = new (string label, string value)[]
            {
                (Loc.T("달린 거리", "Distance"), sm != null ? $"{dist} m" : (st != null ? $"{Mathf.RoundToInt(st.Seconds)} s" : "-")),
                (Loc.T("하트", "Hearts"), st != null ? $"+{st.Hearts}" : "-"),
                (Loc.T("코인", "Coins"), st != null ? $"+{st.CoinValue}" : "-"),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                var cell = CoastUiArt.CutePill(prt, "Stat" + i, new Color(1f, 0.98f, 0.94f, 0.85f), 14, 2);
                cell.raycastTarget = false;
                var crt = cell.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(-172f + i * 172f, -350f); crt.sizeDelta = new Vector2(160f, 62f);
                var v = CoastHudLayout.MakeText(crt, "V", stats[i].value, 22, TextAnchor.MiddleCenter, new Vector2(0f, 0.42f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                v.color = PillNavy; v.fontStyle = FontStyle.Bold;
                var l = CoastHudLayout.MakeText(crt, "L", stats[i].label, 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.42f), Vector2.zero, new Vector2(0f, 2f));
                l.color = new Color(0.45f, 0.38f, 0.36f);
            }
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
            rt.sizeDelta = new Vector2(66f, 66f);
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
            rt.anchoredPosition = new Vector2(-6f, -76f);
            rt.sizeDelta = new Vector2(176f, 54f);
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
            if (_heartsPill != null)
            {
                RefreshHearts();
                if (_heartsPop > 0f) { _heartsPop -= Time.unscaledDeltaTime; float k = Mathf.Clamp01(_heartsPop / 0.22f); _heartsPill.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(k * Mathf.PI)); }
                else if (_heartsPill.localScale != Vector3.one) _heartsPill.localScale = Vector3.one;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터 검증용: 런오버 패널에서 R = 다시, Return = 두 번째 버튼.
            if (_runOverOverlay != null)
            {
                if (Input.GetKeyDown(KeyCode.R)) { var a = _runOverRetry; CloseRunOver(); a?.Invoke(); }
                else if (Input.GetKeyDown(KeyCode.Return)) { var a = _runOverSecond; CloseRunOver(); a?.Invoke(); }
            }
#endif
            UpdateCookieHud();
            if (_sunLate && _sunLabel != null)
            {
                _sunPulse += Time.unscaledDeltaTime * 6f;
                _sunLabel.color = Color.Lerp(new Color(1f, 0.45f, 0.45f, 1f), new Color(1f, 0.85f, 0.85f, 1f), 0.5f + 0.5f * Mathf.Sin(_sunPulse));
            }
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
            if (_hpGaugeFill != null && health != null)
            {
                _hpShown = Mathf.Lerp(_hpShown, health.Normalized, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
                bool low = _hpShown < 0.25f;
                Color c = low
                    ? Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(1f, 0.6f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f))
                    : Color.Lerp(new Color(0.95f, 0.8f, 0.2f), new Color(0.45f, 0.9f, 0.25f), Mathf.Clamp01((_hpShown - 0.25f) / 0.35f));
                if (health.Frozen)
                    c = ScoreYellow;
                _hpGaugeFill.fillAmount = _hpShown;
                _hpGaugeFill.color = c;
                if (_hpGaugeText != null) _hpGaugeText.text = Mathf.RoundToInt(_hpShown * 100f).ToString();
                // 체력이 낮으면 하트가 뛴다.
                float pulse = low ? 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 9f) : 1f;
                if (_hpHeartRt != null) _hpHeartRt.localScale = Vector3.one * pulse;
            }
            if (_hpBar != null)
            {
                if (_hpShake > 0f)
                {
                    _hpShake -= Time.unscaledDeltaTime;
                    float k = _hpShake / 0.35f;
                    _hpBar.anchoredPosition = new Vector2(6f + Mathf.Sin(Time.unscaledTime * 60f) * 6f * k, -84f);
                }
                else
                    _hpBar.anchoredPosition = new Vector2(6f, -84f);
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
            // 7차: 크림 테두리 + 아랫입술 그림자 + 광택(CutePill)로 통일.
            var img = CoastUiArt.CutePill(parent, name, color, 22, 4);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(360f, 64f);
            img.raycastTarget = true;

            var text = CoastHudLayout.MakeText(rt, "Label", label, 26, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(0f, 2f));
            text.color = Color.white;
            CoastUiArt.OutlineText(text, new Color(0f, 0f, 0f, 0.35f), 1f);

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
