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
        public CanvasGroup WeatherGroup => _wxCg;
        public Text CoinText => _coinText;
        public int Score => Mathf.FloorToInt(_distanceScore) + _bonus;
        public bool IsPaused => _paused;

        // 상단 가운데: 낮/밤 + 날씨 칩
        private CanvasGroup _wxCg;
        private Image _timeDisc;
        private Image _timeAccent;
        private Image _wxDot;
        private Text _wxLabel;
        private RectTransform _timeDiscRt;
        private WeatherKind _shownWx = (WeatherKind)(-1);
        private bool _shownNight;
        private int _shownPhase = -1;
        private DynamicEnvironmentManager _env;
        private SeasonWeatherDirector _wx;

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
            BuildWeatherChip(root);
            BuildHealthBar(root);
            // (노을 시계는 UI_FinalDestinationController 의 여정 바/타이머가 맡는다 — BuildSunMeter 는 예비)
            BuildHeartsGoal(root);
            BuildBonusBanner(root);
            BuildTutorial(root);
            BuildFlash(root);
            if (ArcadeRun.KpopMode) { BuildKpopChips(root); BuildKpopSongLabel(root); }

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
            ArcadeRun.OnKpopMissionDone -= HandleKpopMission;
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
            _sunBar.anchoredPosition = new Vector2(240f, -74f);   // 39차-5: 2줄 가운데 칸(240~430), 코인 알약과 같은 높이
            _sunBar.sizeDelta = new Vector2(190f, 52f);
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
            _sunTrack = CoastHudLayout.MakeImage(_sunBar, "Track", Vector2.zero, Vector2.one, new Vector2(10f, 15f), new Vector2(-10f, -15f), Color.white);
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
            _sunLabel = CoastHudLayout.MakeText(_sunBar, "Label", Loc.T("노을까지", "until sunset"), 13, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(0f, 0f));
            _sunLabel.color = new Color(1f, 1f, 1f, 0.9f);
            CoastUiArt.OutlineText(_sunLabel, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
            // 56차-2: 「노을까지 1:51」이 상자보다 5px 넓어 별 아이콘에 걸렸다 → 상자에 맞춰 줄어들기
            _sunLabel.resizeTextForBestFit = true; _sunLabel.resizeTextMinSize = 9; _sunLabel.resizeTextMaxSize = CoastHudLayout.Scaled(13);
            _sunLabel.rectTransform.offsetMin = new Vector2(6f, 0f); _sunLabel.rectTransform.offsetMax = new Vector2(-6f, 0f);
        }

        /// StageManager 가 매 프레임 호출. tau 0..1 = 해가 지기까지 남은 시간 비율.
        public void SetSunset(float tau, bool late)
        {
            if (_sunBar == null) return;
            float w = _sunBar.sizeDelta.x - 36f;
            _sunDot.anchoredPosition = new Vector2(18f + w * Mathf.Clamp01(tau), late ? -2f : 0f);
            if (!late)
            {
                float left = 1f - tau;
                // 39차-5: 남은 시간(m:ss)을 같이 — 폰 캡처와 동일하게 「노을까지 2:10」
                int secs = StageManager.Instance != null ? Mathf.CeilToInt(left * StageManager.Instance.SunsetSeconds) : 0;
                string clock = secs > 0 ? $" {secs / 60}:{secs % 60:00}" : "";
                _sunLabel.text = left > 0.3f ? Loc.T("노을까지", "until sunset") + clock : Loc.T("해가 진다…", "sun is setting…") + clock;
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
            _hpBar.anchoredPosition = new Vector2(6f, -74f);   // 39차-5: 2줄 y -74, 높이 52
            _hpBar.sizeDelta = new Vector2(200f, 52f);
            _hpCg = wrap.GetComponent<CanvasGroup>();

            // 게이지 트랙(남색 알약 + 크림 테두리)
            var track = CoastUiArt.CutePill(_hpBar, "Track", PillNavy, 14, 3);
            var trt = track.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0f, 1f); trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(26f, -4f); trt.sizeDelta = new Vector2(174f, 44f);   // 39차-5: 2줄 알약들과 같은 높이감
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
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-18f, 0f));
            _hpGaugeText.fontStyle = FontStyle.Bold; _hpGaugeText.color = Color.white; _hpGaugeText.raycastTarget = false;
            CoastUiArt.OutlineText(_hpGaugeText, new Color(0.05f, 0.07f, 0.18f, 0.95f), 2f);

            // 하트 아이콘이 게이지 왼쪽 끝을 살짝 덮는다
            var heartIcon = CoastUiArt.Icon("Heart");
            var heart = CoastUiArt.Panel(_hpBar, "HeartIcon", Color.white, 12);
            _hpHeartRt = heart.rectTransform; _hpHeartRt.anchorMin = _hpHeartRt.anchorMax = new Vector2(0f, 1f); _hpHeartRt.pivot = new Vector2(0.5f, 0.5f);
            _hpHeartRt.anchoredPosition = new Vector2(26f, -26f); _hpHeartRt.sizeDelta = new Vector2(52f, 52f);
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
            _heartsPill.sizeDelta = new Vector2(300f, 60f);   // 39차-5: 점수 알약과 같은 칸
            var icon = CoastUiArt.Icon("Heart");
            if (icon != null)
            {
                var im = CoastHudLayout.MakeImage(_heartsPill, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, -27f), new Vector2(60f, 27f), Color.white);
                im.sprite = icon; im.preserveAspect = true; im.raycastTarget = false;
            }
            _heartsText = CoastHudLayout.MakeText(_heartsPill, "Value", "", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(68f, 0f), new Vector2(-20f, 0f));
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

        // ── 48차: K-POP 한 곡 달리기 — 오늘의 미션 칩 3개(체력 바 아래 왼쪽). 달성하면 체크 + 살짝 튐 ──
        private readonly Text[] _kpopChipText = new Text[3];
        private readonly Image[] _kpopChipBg = new Image[3];
        private RectTransform[] _kpopChipRt = new RectTransform[3];
        private static readonly Color ChipOff = new Color(0.07f, 0.16f, 0.30f, 0.80f), ChipOn = new Color(0.20f, 0.62f, 0.38f, 0.92f);
        private void BuildKpopChips(RectTransform root)
        {
            var conds = ArcadeRun.Conditions;
            for (int i = 0; i < 3 && i < conds.Length; i++)
            {
                var pill = CoastUiArt.CutePill(root, "KpopChip" + i, ArcadeRun.IsKpopMissionDone(i) ? ChipOn : ChipOff, 12, 2);
                var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(8f, -134f - i * 36f); rt.sizeDelta = new Vector2(230f, 32f);
                pill.raycastTarget = false;
                var t = CoastHudLayout.MakeText(pill.transform, "T", ChipLabel(i), 14, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-8f, 0f));
                t.color = Color.white; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Truncate;
                CoastUiArt.OutlineText(t, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
                _kpopChipText[i] = t; _kpopChipBg[i] = pill; _kpopChipRt[i] = rt;
            }
            ArcadeRun.OnKpopMissionDone -= HandleKpopMission;
            ArcadeRun.OnKpopMissionDone += HandleKpopMission;
        }
        /// 48차-5: 우하단 곡 정보 「♪ 제목 — 우히&히시」.
        private void BuildKpopSongLabel(RectTransform root)
        {
            var pill = CoastUiArt.CutePill(root, "KpopSong", new Color(0.07f, 0.16f, 0.30f, 0.72f), 12, 2);
            // 48차-6(사용자): 우상단 동전 알약(y -74, 높이 52) 바로 아래
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -132f); rt.sizeDelta = new Vector2(400f, 32f);
            pill.raycastTarget = false;
            var t = CoastHudLayout.MakeText(pill.transform, "T", "♪ " + ArcadeRun.KpopTrack.Credit, 15, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-12f, 0f));
            t.color = new Color(1f, 0.96f, 0.85f); t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Truncate;
            CoastUiArt.OutlineText(t, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
            // 글자 폭에 맞춰 알약 줄이기
            float w = t.preferredWidth + 26f;
            rt.sizeDelta = new Vector2(Mathf.Clamp(w, 120f, 420f), 32f);
        }

        private static string ChipLabel(int i) => (ArcadeRun.IsKpopMissionDone(i) ? "☑ " : "☐ ") + ArcadeRun.Conditions[i].Text;
        private void HandleKpopMission(int i)
        {
            if (i < 0 || i >= 3 || _kpopChipText[i] == null) return;
            _kpopChipText[i].text = ChipLabel(i);
            _kpopChipBg[i].color = ChipOn;
            StartCoroutine(SimpleTween.PunchScale(_kpopChipRt[i], 0.25f, 0.28f));
            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.6f);
        }

        /// 36차: 도착 실패(체력 0 / 노을 놓침) 결과 — 사용자 시안: 노을 하늘 위 리본 "아쉽지만 다음에!", 주저앉은 하늘, 별 3개(실패 N/3),
        /// 크림 패널 [결과] 거리·점수 / 코인·아이템 / 최고 콤보·하트, 주황 경고 "목표 거리 N m 미달", [다시 도전하기] [나가기].
        /// 65차(사용자 시안): 「한 곡 완주!」 결과 화면 — 보라 은하 배경(UI_Result_Galaxy) + 금색 젤리 제목 + 네온 분홍 부제 알약 +
        ///   보석 금테 얼굴(UI_Ring_Gold) + 큰 별 3개 + 「⭐ 오늘 미션 N/3」 금 알약 + 크림 결과 카드(둥근 아이콘 Icon_R_*) +
        ///   주황 「👑 최고 점수 갱신!」 띠(미션 체크) + 분홍 「♪ 한 곡 더 ♪」 / 회색 「나가기」.
        private void ShowSongComplete(UnityEngine.Events.UnityAction retry, UnityEngine.Events.UnityAction toTitle, string secondLabel)
        {
            var canvas = CoastUiCanvas.Create("RunOverOverlay", 410);
            _runOverOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);
            float pad = CoastUiCanvas.HudPad;
            Color gold = new Color(1f, 0.84f, 0.30f), goldEdge = new Color(0.62f, 0.34f, 0.04f), pink = new Color(1f, 0.42f, 0.70f), ink = new Color(0.28f, 0.20f, 0.14f), brown = new Color(0.52f, 0.38f, 0.26f), navy = new Color(0.22f, 0.20f, 0.45f);

            // 배경: 은하 그림(없으면 보라 단색) + 반짝이
            var galaxy = ArtAssets.LoadTexture("UI_Result_Galaxy");
            var sky = CoastHudLayout.MakeImage(root, "Sky", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.22f, 0.10f, 0.32f, 1f));
            sky.raycastTarget = true;
            if (galaxy != null)
            {
                var bg = new GameObject("Galaxy", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                bg.transform.SetParent(root, false); bg.sprite = CoastUiArt.AsSprite(galaxy); bg.raycastTarget = false;
                // 66차: AspectRatioFitter(EnvelopeParent)는 안전영역(root)까지만 덮어 가장자리에 보라 띠가 남았다 → 화면 전체를 덮는 크기를 직접 계산
                float gw = root.rect.width + pad * 2f + 40f, gh = root.rect.height + pad * 2f + 40f, ga = galaxy.width / (float)galaxy.height;
                if (gw / gh < ga) gw = gh * ga; else gh = gw / ga;
                var brt0 = bg.rectTransform; brt0.anchorMin = brt0.anchorMax = new Vector2(0.5f, 0.5f); brt0.sizeDelta = new Vector2(gw, gh);
            }
            var srng = new System.Random(65);
            for (int i = 0; i < 22; i++)
            {
                float x = (float)srng.NextDouble(), y = 0.42f + (float)srng.NextDouble() * 0.56f;
                EventCardKit.Sparkle(root, new Vector2(x, y), Vector2.zero, 10 + srng.Next(16), new Color(1f, 0.92f, 0.6f, 0.55f + (float)srng.NextDouble() * 0.45f));
            }

            // 제목(금색 젤리) + 네온 분홍 부제 알약
            EventCardKit.JellyTitle(root, Loc.T("한 곡 완주! ♪", "SONG COMPLETE! ♪"), gold, goldEdge, 44f, 120f, 78);
            var subPill = CoastUiArt.Panel(root, "SubPill", pink, 28); subPill.raycastTarget = false;
            var sprt = subPill.rectTransform; sprt.anchorMin = sprt.anchorMax = new Vector2(0.5f, 1f); sprt.pivot = new Vector2(0.5f, 1f); sprt.anchoredPosition = new Vector2(0f, -188f); sprt.sizeDelta = new Vector2(616f, 54f);
            var subIn = CoastUiArt.Panel(sprt, "In", new Color(0.34f, 0.12f, 0.46f, 0.92f), 24); subIn.raycastTarget = false;
            subIn.rectTransform.anchorMin = Vector2.zero; subIn.rectTransform.anchorMax = Vector2.one; subIn.rectTransform.offsetMin = new Vector2(4f, 4f); subIn.rectTransform.offsetMax = new Vector2(-4f, -4f);
            string subTxt = ArcadeRun.BossRush ? Loc.T($"보스전 · 보스 {ArcadeRun.BossesCleared}마리 퇴치!", $"Boss Rush · {ArcadeRun.BossesCleared} bosses cleared!") : Loc.T("K-POP 한 곡 달리기 · ", "One-Song Run · ") + ArcadeRun.KpopTrack.Credit;
            var sub = CoastHudLayout.MakeText(sprt, "Sub", subTxt, 21, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            sub.color = Color.white; sub.fontStyle = FontStyle.Bold; sub.horizontalOverflow = HorizontalWrapMode.Wrap; sub.verticalOverflow = VerticalWrapMode.Truncate; sub.resizeTextForBestFit = true; sub.resizeTextMinSize = 11; sub.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            CoastUiArt.OutlineText(sub, new Color(1f, 0.3f, 0.6f, 0.6f), 1.2f);

            // 얼굴 + 보석 금테
            var faceTex = ArtAssets.LoadTexture("UI_Face_Girl") ?? ArtAssets.LoadTexture("UI_Face_Ring");
            var ringTex = ArtAssets.LoadTexture("UI_Ring_Gold");
            Vector2 faceC = new Vector2(188f, -416f); float faceR = 124f;
            if (faceTex != null)
            {
                var maskGo = new GameObject("FaceMask", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<Image>();
                maskGo.transform.SetParent(root, false); maskGo.sprite = CoastUiArt.RoundedRect(200); maskGo.type = Image.Type.Sliced; maskGo.raycastTarget = false;
                var mrt = maskGo.rectTransform; mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 1f); mrt.anchoredPosition = faceC; mrt.sizeDelta = new Vector2(faceR * 2f, faceR * 2f);
                maskGo.GetComponent<Mask>().showMaskGraphic = false;
                var face = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                face.transform.SetParent(mrt, false); face.sprite = CoastUiArt.AsSprite(faceTex); face.preserveAspect = false; face.raycastTarget = false;
                face.rectTransform.anchorMin = Vector2.zero; face.rectTransform.anchorMax = Vector2.one; face.rectTransform.offsetMin = new Vector2(-14f, -30f); face.rectTransform.offsetMax = new Vector2(14f, 10f);
            }
            if (ringTex != null)
            {
                var ring = CoastHudLayout.MakeImage(root, "Ring", new Vector2(0f, 1f), new Vector2(0f, 1f), faceC - new Vector2(faceR + 26f, faceR + 26f), faceC + new Vector2(faceR + 26f, faceR + 26f), Color.white);
                ring.sprite = CoastUiArt.AsSprite(ringTex); ring.preserveAspect = true; ring.raycastTarget = false;
            }
            else
            {
                var ring = CoastUiArt.Panel(root, "Ring", gold, 200); ring.raycastTarget = false;
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0f, 1f); ring.rectTransform.anchoredPosition = faceC; ring.rectTransform.sizeDelta = new Vector2(faceR * 2f + 24f, faceR * 2f + 24f);
                ring.transform.SetSiblingIndex(Mathf.Max(0, root.childCount - 2));
            }
            foreach (var (sx, sy, sz) in new[] { (-150f, 120f, 30), (150f, 130f, 26), (-160f, -120f, 22), (150f, -125f, 28) })
                EventCardKit.Sparkle(root, new Vector2(0f, 1f), faceC + new Vector2(sx, sy), sz, new Color(1f, 0.95f, 0.7f, 0.9f));

            // 별 3개 + 「⭐ 오늘 미션 N/3」
            int kpopDoneCount = 0; for (int i = 0; i < 3 && i < ArcadeRun.Conditions.Length; i++) if (ArcadeRun.ConditionDone[i]) kpopDoneCount++;
            int stars = kpopDoneCount;
            var starSp = CoastUiArt.Icon("Star");
            for (int i = 0; i < 3; i++)
            {
                float sz = i == 1 ? 124f : 112f; float sx = 420f + i * 92f; float sy = -388f - (i == 1 ? 6f : 0f);
                var star = CoastHudLayout.MakeImage(root, "Star" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(sx - sz * 0.5f, sy - sz * 0.5f), new Vector2(sx + sz * 0.5f, sy + sz * 0.5f), i < stars ? Color.white : new Color(0.85f, 0.78f, 0.70f, 0.95f));   // 66차: 시안처럼 셋 다 금색(안 이룬 별은 살짝만 흐리게)
                if (starSp != null) { star.sprite = starSp; star.preserveAspect = true; } else { star.sprite = CoastUiArt.RoundedRect(40); star.type = Image.Type.Sliced; star.color = i < stars ? gold : new Color(0.5f, 0.5f, 0.55f); }
                star.raycastTarget = false;
            }
            var mp = CoastUiArt.CutePill(root, "MissionPill", new Color(0.72f, 0.24f, 0.62f), 24, 3); mp.raycastTarget = false;   // 66차: 시안처럼 자주색 알약 + 금테
            mp.color = new Color(1f, 0.84f, 0.35f);
            var mprt = mp.rectTransform; mprt.anchorMin = mprt.anchorMax = new Vector2(0f, 1f); mprt.pivot = new Vector2(0.5f, 0.5f); mprt.anchoredPosition = new Vector2(512f, -476f); mprt.sizeDelta = new Vector2(262f, 50f);
            var mpt = CoastHudLayout.MakeText(mprt, "T", Loc.T($"★ 오늘 미션 {stars}/3", $"★ Today's missions {stars}/3"), 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            mpt.color = Color.white; mpt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(mpt, new Color(0.35f, 0.05f, 0.30f, 0.8f), 1.5f);

            // 결과 카드
            var sm = StageManager.Instance; var st = StageRunStats.Instance;
            int dist = Mathf.RoundToInt(ArcadeRun.Distance); int score = ArcadeRun.LastScore;
            var prof = GameManager.I != null ? GameManager.I.Profile : null;
            var frame = CoastUiArt.CutePill(root, "Panel", new Color(0.99f, 0.96f, 0.90f), 30, 5);
            var prt = frame.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 40f); prt.sizeDelta = new Vector2(648f, 652f); frame.raycastTarget = true;   // 66차: 시안 비율(카드 652, 아래 여백 40)
            var head = CoastHudLayout.MakeText(prt, "Head", Loc.T("결과", "Result"), 24, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(0f, -4f));
            head.color = ink; head.fontStyle = FontStyle.Bold;
            foreach (int sgn in new[] { -1, 1 })
            {
                var line = CoastHudLayout.MakeImage(prt, "HeadLine", new Vector2(0.5f + sgn * 0.5f, 1f), new Vector2(0.5f + sgn * 0.5f, 1f), new Vector2(sgn < 0 ? 40f : -250f, -28f), new Vector2(sgn < 0 ? 250f : -40f, -26f), new Color(0.75f, 0.65f, 0.52f, 0.7f));
                line.raycastTarget = false;
            }
            Image Badge(Transform parent, string icon, Color bg, Vector2 center, float size)
            {
                var c = CoastUiArt.Panel(parent, "Badge", bg, (int)(size * 0.5f)); c.raycastTarget = false;
                var cr = c.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0f, 1f); cr.pivot = new Vector2(0.5f, 0.5f); cr.anchoredPosition = center; cr.sizeDelta = new Vector2(size, size);
                var tex = ArtAssets.LoadTexture("Icon_R_" + icon) ?? ArtAssets.LoadTexture("Icon_" + icon);
                if (tex != null)
                {
                    var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(cr, false); im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; im.raycastTarget = false;
                    im.rectTransform.anchorMin = Vector2.zero; im.rectTransform.anchorMax = Vector2.one; im.rectTransform.offsetMin = new Vector2(4f, 4f); im.rectTransform.offsetMax = new Vector2(-4f, -4f);
                    if (ArtAssets.LoadTexture("Icon_R_" + icon) != null) c.color = Color.clear;   // 시트 아이콘은 동그라미가 그려져 있다
                }
                return c;
            }
            void Big(float cx, string icon, Color bg, string label, string value, Color valueCol)
            {
                Badge(prt, icon, bg, new Vector2(cx, -91f), 68f);
                var lb = CoastHudLayout.MakeText(prt, "Lb", label, 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx - 150f, -150f), new Vector2(cx + 150f, -124f));
                lb.color = brown; lb.fontStyle = FontStyle.Bold;
                var v = CoastHudLayout.MakeText(prt, "V", value, 40, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx - 155f, -194f), new Vector2(cx + 155f, -150f));
                v.color = valueCol; v.fontStyle = FontStyle.Bold; v.resizeTextForBestFit = true; v.resizeTextMinSize = 20; v.resizeTextMaxSize = CoastHudLayout.Scaled(36);
                v.horizontalOverflow = HorizontalWrapMode.Wrap; v.verticalOverflow = VerticalWrapMode.Truncate;
            }
            Big(165f, "Shoe", new Color(0.70f, 0.45f, 0.95f), Loc.T("거리", "Distance"), $"{dist:N0} m", navy);
            Big(495f, "Trophy", new Color(1f, 0.78f, 0.25f), Loc.T("점수", "Score"), Loc.T($"점수 {score:N0}", $"{score:N0}"), new Color(0.85f, 0.55f, 0.05f));
            var vline = CoastHudLayout.MakeImage(prt, "VLine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1f, -200f), new Vector2(1f, -60f), new Color(0.75f, 0.65f, 0.52f, 0.6f)); vline.raycastTarget = false;
            var hline = CoastHudLayout.MakeImage(prt, "HLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -216f), new Vector2(-28f, -214f), new Color(0.75f, 0.65f, 0.52f, 0.6f)); hline.raycastTarget = false;
            int items = st != null ? st.Potions + st.Stars : 0;
            (string icon, Color bg, string text)[] small =
            {
                ("Coin", new Color(1f, 0.80f, 0.20f), Loc.T($"코인 {(st != null ? st.Coins : 0)}", $"Coins {(st != null ? st.Coins : 0)}")),
                ("Combo", new Color(1f, 0.55f, 0.20f), Loc.T($"최고 콤보 {(st != null ? st.BestCombo : 0)}", $"Best combo {(st != null ? st.BestCombo : 0)}")),
                ("Star", new Color(1f, 0.80f, 0.20f), Loc.T($"아이템 {items}", $"Items {items}")),
                ("Heart", new Color(1f, 0.45f, 0.65f), Loc.T($"하트 {(st != null ? st.Hearts : 0)}", $"Hearts {(st != null ? st.Hearts : 0)}")),
            };
            for (int i = 0; i < 4; i++)
            {
                float x0 = (i % 2) * 290f, y = -232f - (i / 2) * 72f;
                Badge(prt, small[i].icon, small[i].bg, new Vector2(x0 + 115f, y - 28f), 52f);
                var t = CoastHudLayout.MakeText(prt, "S" + i, small[i].text, 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 150f, y - 56f), new Vector2(x0 + 330f, y));
                t.color = ink; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            }
            var vline2 = CoastHudLayout.MakeImage(prt, "VLine2", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1f, -372f), new Vector2(1f, -226f), new Color(0.75f, 0.65f, 0.52f, 0.5f)); vline2.raycastTarget = false;

            // 주황 띠: 최고 점수 갱신 / 도장 + 미션 체크
            bool newBestKpop = ArcadeRun.LastNewBest;
            string reason = ArcadeRun.LastStamped ? Loc.T($"★ 오늘 도장 ✓  연속 {prof?.dailyStreak ?? 1}일째  +{ArcadeRun.LastStampCoins}G", $"★ Stamped ✓  {prof?.dailyStreak ?? 1}-day streak  +{ArcadeRun.LastStampCoins}G")
                          : newBestKpop ? Loc.T("★ 최고 점수 갱신!", "★ New best score!") : Loc.T("♪ 한 곡 완주 — 오늘 도장은 이미 찍었어", "♪ Song done — already stamped today");
            var sbW = new System.Text.StringBuilder();
            for (int i = 0; i < 3 && i < ArcadeRun.Conditions.Length; i++) { if (i > 0) sbW.Append("   "); sbW.Append(ArcadeRun.ConditionDone[i] ? "☑ " : "☐ ").Append(ArcadeRun.Conditions[i].Text); }
            if (ArcadeRun.LastAllClearCoins > 0) sbW.Append(Loc.T($"   미션 올클리어! +{ArcadeRun.LastAllClearCoins}G", $"   All clear! +{ArcadeRun.LastAllClearCoins}G"));
            var warn = CoastUiArt.GlossyPill(prt, "Warn", new Color(1f, 0.70f, 0.20f), 20, 8); warn.raycastTarget = false;
            warn.rectTransform.anchorMin = new Vector2(0f, 1f); warn.rectTransform.anchorMax = new Vector2(1f, 1f); warn.rectTransform.offsetMin = new Vector2(30f, -502f); warn.rectTransform.offsetMax = new Vector2(-30f, -408f);
            var w1 = CoastHudLayout.MakeText(warn.transform, "W1", reason, 26, TextAnchor.MiddleCenter, new Vector2(0f, 0.48f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -8f));
            w1.color = Color.white; w1.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(w1, new Color(0.55f, 0.25f, 0f, 0.8f), 1.8f);
            w1.resizeTextForBestFit = true; w1.resizeTextMinSize = 14; w1.resizeTextMaxSize = CoastHudLayout.Scaled(26);
            var w2 = CoastHudLayout.MakeText(warn.transform, "W2", sbW.ToString(), 15, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(12f, 12f), new Vector2(-12f, 0f));
            w2.color = new Color(0.45f, 0.20f, 0.02f); w2.fontStyle = FontStyle.Bold; w2.resizeTextForBestFit = true; w2.resizeTextMinSize = 10; w2.resizeTextMaxSize = CoastHudLayout.Scaled(15);

            // 버튼
            _runOverRetry = retry; _runOverSecond = toTitle;
            Button Btn(string name, string label, Color col, float x0, float x1, System.Action onClick)
            {
                var pill = CoastUiArt.GlossyPill(prt, name, col, 26, 10);
                pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0f, 0f); pill.rectTransform.pivot = new Vector2(0f, 0f);
                pill.rectTransform.anchoredPosition = new Vector2(x0, 50f); pill.rectTransform.sizeDelta = new Vector2(x1 - x0, 64f); pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => onClick());
                var t = CoastHudLayout.MakeText(pill.transform, "T", label, 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 6f), Vector2.zero);
                t.color = Color.white; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = CoastHudLayout.Scaled(26);
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.8f);
                return b;
            }
            Btn("Retry", Loc.T("♪ 한 곡 더 ♪", "♪ One more ♪"), new Color(0.96f, 0.36f, 0.52f), 36f, 316f, () => { CloseRunOver(); retry?.Invoke(); });
            Btn("Exit", secondLabel == "메인으로" ? Loc.T("나가기", "Exit") : secondLabel, new Color(0.56f, 0.58f, 0.64f), 334f, 612f, () => { CloseRunOver(); toTitle?.Invoke(); });
        }

        public void ShowRunOver(UnityEngine.Events.UnityAction retry, UnityEngine.Events.UnityAction toTitle, string secondLabel = "메인으로")
        {
            if (_runOverOverlay != null)
                Destroy(_runOverOverlay);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (ArcadeRun.KpopMode && ArcadeRun.KpopFinished) { ShowSongComplete(retry, toTitle, secondLabel); return; }   // 65차: 한 곡 완주 전용 화면

            var canvas = CoastUiCanvas.Create("RunOverOverlay", 410);
            _runOverOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);
            float pad = CoastUiCanvas.HudPad;

            // ── 배경: 노을이 진 하늘(위 진남색 → 가운데 주황 → 아래 어두운 땅) + 반딧불 점 ──
            var sky = CoastHudLayout.MakeImage(root, "Sky", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.13f, 0.09f, 0.16f, 1f));
            sky.raycastTarget = true;
            var glow = CoastHudLayout.MakeImage(root, "Glow", new Vector2(0f, 0.50f), new Vector2(1f, 0.80f), new Vector2(-pad, 0f), new Vector2(pad, 0f), new Color(0.85f, 0.45f, 0.22f, 0.30f));
            glow.sprite = CoastUiArt.RoundedRect(60); glow.type = Image.Type.Sliced; glow.raycastTarget = false;
            var glow2 = CoastHudLayout.MakeImage(root, "Glow2", new Vector2(0f, 0.56f), new Vector2(1f, 0.72f), new Vector2(-pad, 0f), new Vector2(pad, 0f), new Color(1f, 0.70f, 0.35f, 0.18f));
            glow2.sprite = CoastUiArt.RoundedRect(60); glow2.type = Image.Type.Sliced; glow2.raycastTarget = false;
            var ground = CoastHudLayout.MakeImage(root, "Ground", new Vector2(0f, 0f), new Vector2(1f, 0.56f), new Vector2(-pad, -pad), new Vector2(pad, 0f), new Color(0.16f, 0.11f, 0.12f, 1f));
            ground.raycastTarget = false;
            var grassLine = CoastHudLayout.MakeImage(root, "Horizon", new Vector2(0f, 0.555f), new Vector2(1f, 0.565f), new Vector2(-pad, 0f), new Vector2(pad, 0f), new Color(0.32f, 0.22f, 0.16f, 1f));
            grassLine.raycastTarget = false;
            var frng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)frng.NextDouble(), y = 0.45f + (float)frng.NextDouble() * 0.5f; float r = 2f + (float)frng.NextDouble() * 4f;
                var dot = CoastHudLayout.MakeImage(root, "Firefly", new Vector2(x, y), new Vector2(x, y), new Vector2(-r, -r), new Vector2(r, r), new Color(1f, 0.85f, 0.45f, 0.35f + (float)frng.NextDouble() * 0.4f));
                dot.sprite = CoastUiArt.RoundedRect(8); dot.type = Image.Type.Sliced; dot.raycastTarget = false;
            }

            // ── 리본 배너 "아쉽지만 다음에!" + 부제 "달리기 결과" ──
            var ribbon = new GameObject("Ribbon", typeof(RectTransform)).GetComponent<RectTransform>();
            ribbon.SetParent(root, false);
            ribbon.anchorMin = ribbon.anchorMax = new Vector2(0.5f, 1f); ribbon.pivot = new Vector2(0.5f, 1f);
            ribbon.anchoredPosition = new Vector2(0f, -54f); ribbon.sizeDelta = new Vector2(520f, 92f);
            Color ribbonCol = new Color(0.93f, 0.88f, 0.80f), ribbonDark = new Color(0.72f, 0.62f, 0.52f);
            foreach (int sgn in new[] { -1, 1 })
            {
                var tail = CoastUiArt.Panel(ribbon, "Tail", ribbonDark, 8);
                tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(0.5f + sgn * 0.5f, 0.5f); tail.rectTransform.pivot = new Vector2(0.5f - sgn * 0.5f, 0.5f);
                tail.rectTransform.anchoredPosition = new Vector2(sgn * -14f, -18f); tail.rectTransform.sizeDelta = new Vector2(74f, 64f);
                tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, sgn * 8f); tail.raycastTarget = false;
            }
            var band = CoastUiArt.Panel(ribbon, "Band", ribbonCol, 10);
            band.rectTransform.anchorMin = Vector2.zero; band.rectTransform.anchorMax = Vector2.one; band.rectTransform.offsetMin = new Vector2(0f, 10f); band.rectTransform.offsetMax = new Vector2(0f, -4f);
            band.raycastTarget = false;
            var bandShade = CoastUiArt.Panel(band.transform, "Shade", new Color(0f, 0f, 0f, 0.08f), 10);
            bandShade.rectTransform.anchorMin = new Vector2(0f, 0f); bandShade.rectTransform.anchorMax = new Vector2(1f, 0.35f); bandShade.rectTransform.offsetMin = bandShade.rectTransform.offsetMax = Vector2.zero; bandShade.raycastTarget = false;
            bool kpopDone = ArcadeRun.KpopMode && ArcadeRun.KpopFinished;   // 48차: 한 곡 완주 결과
            var title = CoastHudLayout.MakeText(band.transform, "Title", kpopDone ? Loc.T("한 곡 완주! ♪", "Song complete! ♪") : Loc.T("아쉽지만 다음에!", "Next time!"), 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(0f, 0f));
            title.color = new Color(0.30f, 0.20f, 0.14f); title.fontStyle = FontStyle.Bold;
            // 51차: 보스전은 곡 크레딧까지 넣으면 한 줄을 넘쳐서 — 퇴치 수만(곡 제목은 HUD 에서 이미 봤다).
            var sub = CoastHudLayout.MakeText(root, "Sub", ArcadeRun.BossRush ? Loc.T($"보스전 · 보스 {ArcadeRun.BossesCleared}마리 퇴치!", $"Boss Rush · {ArcadeRun.BossesCleared} bosses cleared!")
                                                              : ArcadeRun.KpopMode ? Loc.T("K-POP 한 곡 달리기 · ", "One-Song Run · ") + ArcadeRun.KpopTrack.Credit : Loc.T("달리기 결과", "Run result"), 22, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -196f), new Vector2(0f, -156f));
            sub.color = new Color(1f, 0.82f, 0.35f); sub.fontStyle = FontStyle.Bold;
            sub.horizontalOverflow = HorizontalWrapMode.Wrap; sub.resizeTextForBestFit = true; sub.resizeTextMinSize = 14; sub.resizeTextMaxSize = 22;
            CoastUiArt.OutlineText(sub, new Color(0.25f, 0.12f, 0.05f, 0.9f), 2f);

            // ── 주저앉은 하늘(UI_RunOver_Sad) + 별 3개 ──
            var sad = kpopDone ? null : ArtAssets.LoadTexture("UI_RunOver_Sad");
            if (kpopDone)
            {
                // 51차(사용자): 완주 결과에 그림이 빠져 갈색 빈 판만 보였다 → 스테이지 클리어와 같은 얼굴 컷인 + 「오늘도 찢었다! 오운완」 말풍선
                var faceTex = ArtAssets.LoadTexture("UI_Face_Ring") ?? ArtAssets.LoadTexture("UI_Face_Girl");
                if (faceTex != null)
                {
                    var face = CoastHudLayout.MakeImage(root, "Face", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -540f), new Vector2(300f, -280f), Color.white);
                    face.sprite = CoastUiArt.AsSprite(faceTex); face.preserveAspect = true; face.raycastTarget = false;
                    var bub = CoastUiArt.CutePill(root, "FaceBubble", Color.white, 14, 3);
                    var brt2 = bub.rectTransform; brt2.anchorMin = brt2.anchorMax = new Vector2(0f, 1f); brt2.pivot = new Vector2(0.5f, 0.5f);
                    brt2.anchoredPosition = new Vector2(170f, -572f); brt2.sizeDelta = new Vector2(270f, 48f); bub.raycastTarget = false;
                    foreach (var im in bub.GetComponentsInChildren<Image>()) if (im.name == "Lip") im.color = new Color(0.82f, 0.82f, 0.86f, 1f);
                    var bt = CoastHudLayout.MakeText(brt2, "T", Loc.T("오늘도 찢었다! 오운완", "Nailed it! Song done"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
                    bt.color = new Color(0.16f, 0.16f, 0.22f); bt.fontStyle = FontStyle.Bold;
                }
            }
            if (sad != null)
            {
                var pic = CoastHudLayout.MakeImage(root, "Sad", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -560f), new Vector2(316f, -206f), Color.white);
                pic.sprite = CoastUiArt.AsSprite(sad); pic.preserveAspect = true; pic.raycastTarget = false;
                var shadow = CoastHudLayout.MakeImage(root, "SadShadow", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -572f), new Vector2(300f, -546f), new Color(0f, 0f, 0f, 0.35f));
                shadow.sprite = CoastUiArt.RoundedRect(40); shadow.type = Image.Type.Sliced; shadow.raycastTarget = false; shadow.transform.SetSiblingIndex(pic.transform.GetSiblingIndex());
            }
            var sm = StageManager.Instance;
            var st = StageRunStats.Instance;
            int dist = sm != null ? Mathf.RoundToInt(sm.StageLocalDistance) : 0;
            float goal = sm != null && sm.Current != null ? sm.Current.targetDistance : 0f;
            // 38차: K-POP(아케이드) 런 — 목표 대신 최고 기록 기준
            bool arcade = ArcadeRun.Active;
            var prof = GameManager.I != null ? GameManager.I.Profile : null;
            int bestDist = prof != null ? prof.endlessBestDist : 0;
            if (arcade) { dist = Mathf.RoundToInt(ArcadeRun.Distance); goal = 0f; }
            float prog = goal > 1f ? Mathf.Clamp01(dist / goal) : (arcade && bestDist > 0 ? Mathf.Clamp01(dist / (float)bestDist) : 0f);
            int stars = arcade ? (dist >= bestDist && dist > 0 ? 3 : prog >= 0.66f ? 2 : prog >= 0.30f ? 1 : 0) : (prog >= 0.66f ? 2 : prog >= 0.30f ? 1 : 0);
            int kpopDoneCount = 0; if (ArcadeRun.KpopMode) { for (int i = 0; i < 3 && i < ArcadeRun.Conditions.Length; i++) if (ArcadeRun.ConditionDone[i]) kpopDoneCount++; stars = kpopDoneCount; }
            var starSp = CoastUiArt.Icon("Star");
            for (int i = 0; i < 3; i++)
            {
                float sz = i == 1 ? 58f : 50f; float sx = 470f + i * 64f; float sy = -400f - (i == 1 ? 6f : 0f);
                var star = CoastHudLayout.MakeImage(root, "Star" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(sx - sz * 0.5f, sy - sz * 0.5f), new Vector2(sx + sz * 0.5f, sy + sz * 0.5f), i < stars ? Color.white : new Color(0.55f, 0.55f, 0.58f, 0.9f));
                if (starSp != null) { star.sprite = starSp; star.preserveAspect = true; }
                else { star.sprite = CoastUiArt.RoundedRect(24); star.type = Image.Type.Sliced; star.color = i < stars ? new Color(1f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.52f); }
                star.raycastTarget = false;
            }
            var starLabel = CoastHudLayout.MakeText(root, "StarLabel", ArcadeRun.KpopMode ? Loc.T($"오늘 미션 {stars} / 3", $"Today's missions {stars} / 3") : arcade ? Loc.T($"기록 · 별 {stars} / 3", $"Record · {stars} / 3 stars") : Loc.T($"실패 · 별 {stars} / 3", $"Failed · {stars} / 3 stars"), 14, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, -470f), new Vector2(660f, -440f));
            starLabel.color = new Color(1f, 0.92f, 0.75f); CoastUiArt.OutlineText(starLabel, new Color(0.2f, 0.1f, 0.05f, 0.8f), 1.5f);

            // ── 결과 패널 ──
            Color cream = new Color(0.96f, 0.93f, 0.86f), tan = new Color(0.78f, 0.66f, 0.50f), ink = new Color(0.28f, 0.20f, 0.14f), brown = new Color(0.45f, 0.35f, 0.26f);
            var frame = CoastUiArt.Panel(root, "PanelFrame", tan, 26);
            var frt = frame.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0f); frt.pivot = new Vector2(0.5f, 0f);
            frt.anchoredPosition = new Vector2(0f, 34f); frt.sizeDelta = new Vector2(640f, 560f); frame.raycastTarget = true;
            var panel = CoastUiArt.Panel(frame.transform, "Panel", cream, 22);
            var prt = panel.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(5f, 5f); prt.offsetMax = new Vector2(-5f, -5f);

            var head = CoastHudLayout.MakeText(prt, "Head", Loc.T("결과", "Result"), 20, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(0f, -14f));
            head.color = ink; head.fontStyle = FontStyle.Bold;
            foreach (int sgn in new[] { -1, 1 })
            {
                var line = CoastHudLayout.MakeImage(prt, "HeadLine", new Vector2(0.5f + sgn * 0.5f, 1f), new Vector2(0.5f + sgn * 0.5f, 1f), new Vector2(sgn < 0 ? 40f : -250f, -36f), new Vector2(sgn < 0 ? 250f : -40f, -34f), new Color(0.75f, 0.65f, 0.52f, 0.7f));
                line.raycastTarget = false;
            }
            // 큰 두 칸: 거리 / 점수
            int score = arcade ? ArcadeRun.LastScore : Score;
            void Big(float x0, float x1, string icon, string label, string value)
            {
                var lb = CoastHudLayout.MakeText(prt, "Lb", label, 16, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, -100f), new Vector2(x1, -66f));
                lb.color = brown; lb.fontStyle = FontStyle.Bold;
                var v = CoastHudLayout.MakeText(prt, "V", value, 30, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, -160f), new Vector2(x1, -104f));
                v.color = ink; v.fontStyle = FontStyle.Bold; v.resizeTextForBestFit = true; v.resizeTextMinSize = 20; v.resizeTextMaxSize = CoastHudLayout.Scaled(30);
                v.horizontalOverflow = HorizontalWrapMode.Wrap; v.verticalOverflow = VerticalWrapMode.Truncate;
            }
            Big(0f, 315f, "", Loc.T("거리", "Distance"), $"{dist:N0} m");
            Big(315f, 630f, "", Loc.T("점수", "Score"), Loc.T($"점수 {score:N0}", $"{score:N0}"));
            var vline = CoastHudLayout.MakeImage(prt, "VLine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1f, -168f), new Vector2(1f, -64f), new Color(0.75f, 0.65f, 0.52f, 0.6f)); vline.raycastTarget = false;
            var hline = CoastHudLayout.MakeImage(prt, "HLine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -184f), new Vector2(-28f, -182f), new Color(0.75f, 0.65f, 0.52f, 0.6f)); hline.raycastTarget = false;
            // 작은 네 칸: 코인 / 아이템 / 최고 콤보 / 하트
            int items = st != null ? st.Potions + st.Stars : 0;
            (string icon, string text)[] small =
            {
                ("Coin", Loc.T($"코인 {(st != null ? st.Coins : 0)}", $"Coins {(st != null ? st.Coins : 0)}")),
                ("Star", Loc.T($"아이템 {items}", $"Items {items}")),
                ("Combo", Loc.T($"최고 콤보 {(st != null ? st.BestCombo : 0)}", $"Best combo {(st != null ? st.BestCombo : 0)}")),
                ("Heart", Loc.T($"하트 {(st != null ? st.Hearts : 0)}", $"Hearts {(st != null ? st.Hearts : 0)}")),
            };
            for (int i = 0; i < 4; i++)
            {
                float x0 = (i % 2) * 315f, y = -200f - (i / 2) * 52f;
                var t = CoastHudLayout.MakeText(prt, "S" + i, small[i].text, 17, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 96f, y - 44f), new Vector2(x0 + 305f, y));
                t.color = ink; t.fontStyle = FontStyle.Bold;
                var isp = CoastUiArt.Icon(small[i].icon);
                var ii = CoastHudLayout.MakeImage(prt, "SI" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + 56f, y - 38f), new Vector2(x0 + 88f, y - 6f), Color.white);
                if (isp != null) { ii.sprite = isp; ii.preserveAspect = true; } else { ii.sprite = CoastUiArt.RoundedRect(16); ii.type = Image.Type.Sliced; ii.color = new Color(0.95f, 0.55f, 0.25f); ii.rectTransform.offsetMin += new Vector2(6f, 6f); ii.rectTransform.offsetMax -= new Vector2(6f, 6f); }
                ii.raycastTarget = false;
            }
            var vline2 = CoastHudLayout.MakeImage(prt, "VLine2", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1f, -300f), new Vector2(1f, -196f), new Color(0.75f, 0.65f, 0.52f, 0.5f)); vline2.raycastTarget = false;
            // 경고 상자
            var warn = CoastUiArt.Panel(prt, "Warn", new Color(0.98f, 0.80f, 0.60f), 16);
            warn.rectTransform.anchorMin = new Vector2(0f, 1f); warn.rectTransform.anchorMax = new Vector2(1f, 1f); warn.rectTransform.offsetMin = new Vector2(24f, -422f); warn.rectTransform.offsetMax = new Vector2(-24f, -318f);
            warn.raycastTarget = false;
            var warnEdge = CoastUiArt.Panel(warn.transform, "Edge", new Color(0.92f, 0.55f, 0.25f, 0.9f), 16);
            warnEdge.rectTransform.anchorMin = Vector2.zero; warnEdge.rectTransform.anchorMax = Vector2.one; warnEdge.rectTransform.offsetMin = Vector2.zero; warnEdge.rectTransform.offsetMax = Vector2.zero; warnEdge.raycastTarget = false;
            var warnFill = CoastUiArt.Panel(warn.transform, "Fill", new Color(0.99f, 0.85f, 0.68f), 14);
            warnFill.rectTransform.anchorMin = Vector2.zero; warnFill.rectTransform.anchorMax = Vector2.one; warnFill.rectTransform.offsetMin = new Vector2(3f, 3f); warnFill.rectTransform.offsetMax = new Vector2(-3f, -3f); warnFill.raycastTarget = false;
            bool newBest = arcade && dist > 0 && dist >= bestDist;
            bool newBestKpop = ArcadeRun.KpopMode && ArcadeRun.LastNewBest;
            string reason = arcade ? (newBest ? Loc.T("★ 최고 기록 갱신!", "★ New best!") : Loc.T($"최고 기록 {bestDist:N0} m · {(prof != null ? prof.endlessBestScore : 0):N0}점", $"Best {bestDist:N0} m · {(prof != null ? prof.endlessBestScore : 0):N0} pts"))
                          : goal > 1f ? Loc.T($"! 목표 거리 {goal:N0} m 미달", $"! Short of {goal:N0} m goal") : Loc.T("! 체력이 다 떨어졌어요", "! Out of stamina");
            string kpopW2 = null;
            if (ArcadeRun.KpopMode)
            {
                // 48차: 도장/스트릭/올클리어 + 미션 3줄
                if (ArcadeRun.LastStamped) reason = Loc.T($"오늘 도장 ✓  연속 {prof?.dailyStreak ?? 1}일째  +{ArcadeRun.LastStampCoins}G", $"Stamped ✓  {prof?.dailyStreak ?? 1}-day streak  +{ArcadeRun.LastStampCoins}G");
                else if (kpopDone) reason = Loc.T(newBestKpop ? "★ 최고 점수 갱신!" : "오늘 도장은 이미 찍었어", newBestKpop ? "★ New best score!" : "Already stamped today");
                else reason = Loc.T($"곡의 {Mathf.RoundToInt(ArcadeRun.KpopProgress01 * 100f)}%까지 — 한 곡 더?", $"{Mathf.RoundToInt(ArcadeRun.KpopProgress01 * 100f)}% of the song — one more?");
                var sbW = new System.Text.StringBuilder();
                for (int i = 0; i < 3 && i < ArcadeRun.Conditions.Length; i++) { if (i > 0) sbW.Append("   "); sbW.Append(ArcadeRun.ConditionDone[i] ? "☑ " : "☐ ").Append(ArcadeRun.Conditions[i].Text); }
                if (ArcadeRun.LastAllClearCoins > 0) sbW.Append(Loc.T($"   미션 올클리어! +{ArcadeRun.LastAllClearCoins}G", $"   All clear! +{ArcadeRun.LastAllClearCoins}G"));
                kpopW2 = sbW.ToString();
            }
            var w1 = CoastHudLayout.MakeText(warn.transform, "W1", reason, 19, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(10f, -4f), new Vector2(-10f, -8f));
            w1.color = new Color(0.55f, 0.25f, 0.08f); w1.fontStyle = FontStyle.Bold;
            var w2 = CoastHudLayout.MakeText(warn.transform, "W2", kpopW2 ?? (arcade ? Loc.T("코인은 그대로 챙겼어요. 한 번 더 달려봐요!", "Coins are yours. Run once more!") : Loc.T("이번에는 실패했어요. 다음에는 더 멀리 달려봐요!", "Not this time. Run farther next time!")), 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 8f), new Vector2(-10f, 2f));
            w2.color = new Color(0.55f, 0.35f, 0.22f);

            // 버튼
            _runOverRetry = retry;
            _runOverSecond = toTitle;
            Button Btn(string name, string label, Color col, float x0, float x1, System.Action onClick)
            {
                var pill = CoastUiArt.CutePill(prt, name, col, 18, 4);
                pill.rectTransform.anchorMin = new Vector2(0f, 0f); pill.rectTransform.anchorMax = new Vector2(0f, 0f); pill.rectTransform.pivot = new Vector2(0f, 0f);
                pill.rectTransform.anchoredPosition = new Vector2(x0, 22f); pill.rectTransform.sizeDelta = new Vector2(x1 - x0, 64f);
                pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => onClick());
                var t = CoastHudLayout.MakeText(pill.transform, "T", label, 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = CoastHudLayout.Scaled(19);
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                return b;
            }
            Btn("Retry", ArcadeRun.KpopMode ? Loc.T("한 곡 더 ♪", "One more song ♪") : Loc.T("다시 도전하기", "Try again"), new Color(0.93f, 0.55f, 0.22f), 24f, 322f, () => { CloseRunOver(); retry?.Invoke(); });
            Btn("Exit", secondLabel == "메인으로" ? Loc.T("나가기", "Exit") : secondLabel, new Color(0.72f, 0.60f, 0.45f), 338f, 606f, () => { CloseRunOver(); toTitle?.Invoke(); });
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

        /// 상단 가운데 빈 공간: 낮/밤 + 현재 날씨. ChapterClock / SeasonWeatherDirector 연동.
        private void BuildWeatherChip(RectTransform root)
        {
            var pill = CoastUiArt.CutePill(root, "WeatherChip", PillNavy, 18, 3);
            var rt = pill.rectTransform;
            // 39차-5: 일시정지(6~66)와 점수 알약(358~)의 사이 칸에 왼쪽 정렬 — 겹침 없이 1줄 격자
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(78f, -6f);
            rt.sizeDelta = new Vector2(264f, 60f);
            _wxCg = pill.gameObject.AddComponent<CanvasGroup>();
            _wxCg.blocksRaycasts = false;
            _wxCg.interactable = false;

            var disc = CoastUiArt.Panel(rt, "TimeDisc", new Color(1f, 0.86f, 0.35f), 16);
            _timeDisc = disc;
            _timeDiscRt = disc.rectTransform;
            _timeDiscRt.anchorMin = _timeDiscRt.anchorMax = new Vector2(0f, 0.5f);
            _timeDiscRt.pivot = new Vector2(0.5f, 0.5f);
            _timeDiscRt.anchoredPosition = new Vector2(30f, 1f);
            _timeDiscRt.sizeDelta = new Vector2(32f, 32f);
            disc.raycastTarget = false;

            var accent = CoastUiArt.Panel(_timeDiscRt, "Accent", new Color(0.12f, 0.16f, 0.32f, 1f), 12);
            _timeAccent = accent;
            var art = accent.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.55f, 0.55f);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(24f, 24f);
            accent.raycastTarget = false;
            accent.enabled = false;

            var glow = CoastUiArt.Panel(_timeDiscRt, "Glow", new Color(1f, 0.9f, 0.45f, 0.35f), 18);
            var grt = glow.rectTransform;
            grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(-4f, -4f); grt.offsetMax = new Vector2(4f, 4f);
            glow.raycastTarget = false;
            glow.transform.SetAsFirstSibling();

            var wxDot = CoastUiArt.Panel(rt, "WxDot", SeasonWeatherDirector.WeatherTint(WeatherKind.Clear), 8);
            _wxDot = wxDot;
            var drt = wxDot.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.anchoredPosition = new Vector2(58f, 1f);
            drt.sizeDelta = new Vector2(14f, 14f);
            wxDot.raycastTarget = false;

            _wxLabel = CoastHudLayout.MakeText(rt, "Label", Loc.T("낮 · 맑음", "Day · Clear"), 22, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(74f, 0f), new Vector2(-12f, 0f));
            _wxLabel.color = Color.white;
            _wxLabel.fontStyle = FontStyle.Bold;
            _wxLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _wxLabel.resizeTextForBestFit = true;
            _wxLabel.resizeTextMinSize = 14;
            _wxLabel.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            CoastUiArt.OutlineText(_wxLabel, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.5f);

            RefreshWeatherChip(true);
        }

        private void RefreshWeatherChip(bool force = false)
        {
            if (_wxLabel == null) return;
            if (_wx == null)
                _wx = Object.FindAnyObjectByType<SeasonWeatherDirector>();

            int ch = 1;
            if (StageManager.Instance != null) ch = StageManager.Instance.ChapterIndex;
            else if (GameManager.Active) ch = GameManager.I.Save.chapter;
            // 42차: 칩은 챕터 고정표(ChapterClock)가 아니라 **지금 화면의 조명 시각**을 읽는다.
            // 전엔 노을이 다 저물어 하늘이 남색인데도 「낮」이라고 떠 있었다(사용자 캡처 "노을까지 0:18").
            // 낮(t<0.55) → 노을(0.55~0.88, DynamicEnvironmentManager.GoldenHour) → 밤(≥0.88, BlueHour).
            if (_env == null) _env = Object.FindAnyObjectByType<DynamicEnvironmentManager>();
            int phase;   // 0 낮, 1 노을, 2 밤
            if (_env != null)
            {
                float t = _env.LightingT;
                phase = t >= 0.88f ? 2 : t >= 0.55f ? 1 : 0;
            }
            else phase = ChapterClock.IsNight(ch) ? 2 : 0;
            bool night = phase == 2;
            var wx = _wx != null ? _wx.CurrentWeather : WeatherKind.Clear;
            if (!force && phase == _shownPhase && wx == _shownWx) return;
            _shownPhase = phase;
            _shownNight = night;
            _shownWx = wx;

            if (_timeDisc != null)
            {
                _timeDisc.color = night
                    ? new Color(0.78f, 0.86f, 1f)
                    : phase == 1 ? new Color(1f, 0.55f, 0.28f)   // 노을: 주황 해
                    : new Color(1f, 0.86f, 0.32f);
            }
            if (_timeAccent != null)
            {
                // 밤: 해 디스크 위에 남색 원을 살짝 겹쳐 초승달처럼 읽힌다.
                _timeAccent.enabled = night;
                if (night)
                {
                    var art = _timeAccent.rectTransform;
                    art.anchoredPosition = new Vector2(5f, 4f);
                    art.sizeDelta = new Vector2(24f, 24f);
                    _timeAccent.color = PillNavy;
                }
            }
            if (_wxDot != null)
                _wxDot.color = SeasonWeatherDirector.WeatherTint(wx);

            string time = phase == 2 ? Loc.T("밤", "Night") : phase == 1 ? Loc.T("노을", "Dusk") : Loc.T("낮", "Day");
            string weather = SeasonWeatherDirector.WeatherName(wx);
            _wxLabel.text = Loc.T($"{time} · {weather}", $"{time} · {weather}");
        }

        private void BuildPause(RectTransform root)
        {
            var outer = CoastUiArt.CutePill(root, "PauseButton", PillNavy, 20);
            var go = outer.gameObject;
            outer.raycastTarget = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -6f);
            rt.sizeDelta = new Vector2(60f, 60f);   // 39차-5: 1줄 높이 60으로 통일
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
            rt.sizeDelta = new Vector2(300f, 60f);   // 39차-5
            _scoreCg = pill.gameObject.AddComponent<CanvasGroup>();

            _scoreText = CoastHudLayout.MakeText(rt, "Score", "00000", 36, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(138f, 0f), new Vector2(-22f, 0f));
            _scoreText.color = ScoreYellow;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.horizontalOverflow = HorizontalWrapMode.Wrap;   // 32차: 6자리 점수도 칸 안에
            _scoreText.verticalOverflow = VerticalWrapMode.Truncate; _scoreText.resizeTextForBestFit = true; _scoreText.resizeTextMinSize = 20; _scoreText.resizeTextMaxSize = CoastHudLayout.Scaled(36);
            CoastUiArt.OutlineText(_scoreText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);

            // Multiplier badge: orange lozenge with a big star poking out of the pill.
            var badge = CoastUiArt.CutePill(rt, "MultBadge", BadgeOrange, 16, 3);
            _multBadge = badge.rectTransform;
            _multBadge.anchorMin = _multBadge.anchorMax = new Vector2(0f, 0.5f);
            _multBadge.pivot = new Vector2(0f, 0.5f);
            _multBadge.anchoredPosition = new Vector2(10f, 0f);
            _multBadge.sizeDelta = new Vector2(96f, 48f);
            _multCg = badge.gameObject.AddComponent<CanvasGroup>();
            var star = CoastUiArt.Icon("Star");
            if (star != null)
            {
                var sgo = new GameObject("Star", typeof(RectTransform), typeof(Image));
                sgo.transform.SetParent(_multBadge, false);
                var srt = sgo.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                // 점수 숫자와 겹치지 않게 배지 안쪽으로
                srt.anchoredPosition = new Vector2(-14f, 6f);
                srt.sizeDelta = new Vector2(44f, 44f);
                var si = sgo.GetComponent<Image>();
                si.sprite = star; si.preserveAspect = true; si.raycastTarget = false;
            }
            _multText = CoastHudLayout.MakeText(_multBadge, "Mult", "x1", 26, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(star != null ? -28f : -8f, 0f));
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
            rt.anchoredPosition = new Vector2(-6f, -74f);   // 39차-5: 2줄 오른쪽 칸(468~658)
            rt.sizeDelta = new Vector2(190f, 52f);
            _coinCg = pill.gameObject.AddComponent<CanvasGroup>();

            var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rt, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(1f, 0.5f);
            irt.pivot = new Vector2(1f, 0.5f);
            irt.anchoredPosition = new Vector2(-10f, 4f);
            irt.sizeDelta = new Vector2(46f, 46f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_Coin"), 100f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            _coinText = CoastHudLayout.MakeText(rt, "Coins", "0", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(22f, 0f), new Vector2(-60f, 0f));
            _coinText.color = ScoreYellow;
            _coinText.fontStyle = FontStyle.Bold;
            _coinText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _coinText.verticalOverflow = VerticalWrapMode.Truncate; _coinText.resizeTextForBestFit = true; _coinText.resizeTextMinSize = 18; _coinText.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            CoastUiArt.OutlineText(_coinText, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);
        }

        // ── Runtime ──────────────────────────────────────────────────────────

        private float _stuckScaleSince = -1f;

        private void Update()
        {
            if (_heartsPill != null)
            {
                RefreshHearts();
                if (_heartsPop > 0f) { _heartsPop -= Time.unscaledDeltaTime; float k = Mathf.Clamp01(_heartsPop / 0.22f); _heartsPill.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(k * Mathf.PI)); }
                else if (_heartsPill.localScale != Vector3.one) _heartsPill.localScale = Vector3.one;
            }

            // Esc / P — accidental pause is a common "keyboard stopped working" report.
            if (_runOverOverlay == null
                && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
                TogglePause();

            // Hit-stop is ≤0.15s. If time stays near-zero while not paused, force resume.
            if (!_paused && _player != null && _player.Speed > 0.5f && Time.timeScale < 0.05f)
            {
                if (_stuckScaleSince < 0f) _stuckScaleSince = Time.unscaledTime;
                else if (Time.unscaledTime - _stuckScaleSince > 0.35f)
                {
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    _stuckScaleSince = -1f;
                }
            }
            else
                _stuckScaleSince = -1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터 검증용: 런오버 패널에서 R = 다시, Return = 두 번째 버튼.
            if (_runOverOverlay != null)
            {
                if (Input.GetKeyDown(KeyCode.R)) { var a = _runOverRetry; CloseRunOver(); a?.Invoke(); }
                else if (Input.GetKeyDown(KeyCode.Return)) { var a = _runOverSecond; CloseRunOver(); a?.Invoke(); }
            }
#endif
            UpdateCookieHud();
            RefreshWeatherChip();
            if (_timeDiscRt != null)
            {
                float pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * (_shownNight ? 2.2f : 3.4f));
                _timeDiscRt.localScale = Vector3.one * pulse;
            }
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
                    _hpBar.anchoredPosition = new Vector2(6f + Mathf.Sin(Time.unscaledTime * 60f) * 6f * k, -74f);
                }
                else
                    _hpBar.anchoredPosition = new Vector2(6f, -74f);
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
            if (_pauseCardCanvas != null) _pauseCardCanvas.SetActive(true);
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
            if (_pauseCardCanvas != null)
                _pauseCardCanvas.SetActive(false);
        }

        private GameObject _pauseCardCanvas;   // 60차: 육성 모드 일시정지 카드(별도 캔버스)
        private void BuildPauseOverlay()
        {
            var canvas = CoastUiCanvas.Create("PauseOverlay", 400);
            _pauseOverlay = canvas.gameObject;
            var root = CoastUiCanvas.Root(canvas);

            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad),
                new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(0.02f, 0.05f, 0.12f, 0.80f));   // 39차: 시안만큼 어둡게
            dim.raycastTarget = true;

            System.Action resume = Resume;
            System.Action retry = () => { Resume(); StageManager.Instance?.RetryCurrent(); };
            // 60차(사용자): 육성 모드의 러닝(대회)은 「메인으로」 대신 **육성으로** — 주차 그대로 육성 화면 복귀(GameManager.ContestFail).
            bool story = GameManager.Active && !ArcadeRun.Active;
            System.Action toTitle = () =>
            {
                Resume();
                if (story) { StoryContest.End(); GameManager.I.ContestFail(); return; }
                var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
                if (flow != null)
                    _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
            };
            if (story)
            {
                // 육성 모드 전용 카드(EventCardKit): 젤리 「일시정지」 + 초록 계속 / 주황 다시 / 파랑 육성으로
                var crt = EventCardKit.Card("PauseOverlayCard", 401, new Vector2(560f, 560f), out var cardCanvas, 10f);
                _pauseCardCanvas = cardCanvas.gameObject;
                EventCardKit.JellyTitle(crt, Loc.T("일시정지", "Paused"), new Color(0.40f, 0.60f, 0.98f), new Color(0.12f, 0.20f, 0.55f), 34f, 96f, 56);
                EventCardKit.Divider(crt, 140f);
                EventCardKit.IconButton(crt, "Resume", "Icon_Arrow", Loc.T("계속하기", "Resume"), new Color(0.30f, 0.75f, 0.40f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(440f, 88f), () => resume(), 28);
                EventCardKit.IconButton(crt, "Retry", "Icon_Refresh", Loc.T("다시 시작", "Restart"), new Color(1f, 0.52f, 0.10f), new Vector2(0.5f, 1f), new Vector2(0f, -284f), new Vector2(440f, 88f), () => retry(), 28);
                EventCardKit.IconButton(crt, "Home", "Icon_Home", Loc.T("육성으로 돌아가기", "Back to raising"), new Color(0.30f, 0.55f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0f, -392f), new Vector2(440f, 88f), () => toTitle(), 26);
                var note = CoastHudLayout.MakeText(crt, "Note", Loc.T("육성으로 가면 이 대회는 미달 — 이 주를 다시 키운다", "Leaving fails this contest — redo this week"), 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 52f));
                note.color = new Color(0.45f, 0.40f, 0.38f); note.resizeTextForBestFit = true; note.resizeTextMinSize = 9; note.resizeTextMaxSize = CoastHudLayout.Scaled(14);
                return;
            }

            // 39차: 시안(크림 카드 + 남색 테두리 + "일시정지" 입체 제목 + 초록/주황/파랑 버튼)을 그림 한 장으로(UI_PauseCard),
            // 버튼은 그림 위 투명 히트 영역. 그림이 없으면 옛 코드 카드.
            var cardArt = CoastUiArt.Art("UI_PauseCard");
            if (cardArt != null)
            {
                var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(root, false);
                var img = card.GetComponent<Image>();
                img.sprite = cardArt; img.preserveAspect = true; img.raycastTarget = true;
                var crt = card.GetComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                // 시안 1152×2128 기준 크롭(1070×1330) → 720 단위 ×0.61
                crt.anchoredPosition = new Vector2(0f, 18f);
                crt.sizeDelta = new Vector2(653f, 811f);
                HitButton(crt, "Resume", new Vector2(2f, 135f), new Vector2(462f, 144f), resume);
                HitButton(crt, "Retry", new Vector2(2f, -49f), new Vector2(462f, 144f), retry);
                HitButton(crt, "Title", new Vector2(2f, -232f), new Vector2(462f, 144f), toTitle);
                return;
            }

            var panel = CoastUiArt.Panel(root, "Panel", new Color(0.97f, 0.95f, 0.90f, 1f), 28);
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(480f, 380f);
            panel.raycastTarget = true;

            var title = CoastHudLayout.MakeText(prt, "Title", "일시정지", 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, -24f));
            title.color = PillNavy;

            MakeBigButton(prt, "Resume", "계속하기", new Color(0.30f, 0.72f, 0.36f), -150f, () => resume());
            MakeBigButton(prt, "Retry", "다시 시작", BadgeOrange, -230f, () => retry());
            MakeBigButton(prt, "Title", "메인으로", new Color(0.35f, 0.45f, 0.70f), -310f, () => toTitle());
        }

        /// 39차: 그림 위 투명 버튼(히트 영역). pos는 부모 중심 기준.
        public static Button HitButton(Transform parent, string name, Vector2 pos, Vector2 size, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
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
