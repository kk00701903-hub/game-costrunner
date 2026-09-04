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
        private float _nextBestCheck;

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

            BuildPause(root);
            BuildScorePill(root);
            BuildCoinPill(root);

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
            if (_wallet != null)
                _wallet.OnCoinsChanged -= HandleCoins;
            if (_paused)
                Time.timeScale = 1f;
            SaveBest();
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
            var go = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(72f, 72f);
            var img = go.GetComponent<Image>();
            img.sprite = CoastUiArt.RoundedRect(16, 3);
            img.type = Image.Type.Sliced;
            img.color = PillNavy;

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
            var pill = CoastUiArt.Panel(root, "ScorePill", PillNavy, 22);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(300f, 64f);
            _scoreCg = pill.gameObject.AddComponent<CanvasGroup>();

            _scoreText = CoastHudLayout.MakeText(rt, "Score", "00000", 36, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(110f, 0f), new Vector2(-18f, 0f));
            _scoreText.color = ScoreYellow;

            // Multiplier badge: orange lozenge with a star.
            var badge = CoastUiArt.Panel(rt, "MultBadge", BadgeOrange, 14);
            _multBadge = badge.rectTransform;
            _multBadge.anchorMin = _multBadge.anchorMax = new Vector2(0f, 0.5f);
            _multBadge.pivot = new Vector2(0f, 0.5f);
            _multBadge.anchoredPosition = new Vector2(10f, 0f);
            _multBadge.sizeDelta = new Vector2(92f, 44f);
            _multCg = badge.gameObject.AddComponent<CanvasGroup>();
            _multText = CoastHudLayout.MakeText(_multBadge, "Mult", "x1", 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            _multText.color = Color.white;
        }

        private void BuildCoinPill(RectTransform root)
        {
            var pill = CoastUiArt.Panel(root, "CoinPill", PillNavy, 20);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, -74f);
            rt.sizeDelta = new Vector2(170f, 54f);
            _coinCg = pill.gameObject.AddComponent<CanvasGroup>();

            var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rt, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(1f, 0.5f);
            irt.pivot = new Vector2(1f, 0.5f);
            irt.anchoredPosition = new Vector2(-10f, 0f);
            irt.sizeDelta = new Vector2(38f, 38f);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_Coin"), 100f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            _coinText = CoastHudLayout.MakeText(rt, "Coins", "0", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-56f, 0f));
            _coinText.color = ScoreYellow;
        }

        // ── Runtime ──────────────────────────────────────────────────────────

        private void Update()
        {
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
