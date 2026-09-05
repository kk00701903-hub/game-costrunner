using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Story journey HUD — progress bar, D-Day (distance-linked), coins, phone, monologue, cheer.
    /// Nothing else belongs on the run screen.
    public class UI_FinalDestinationController : MonoBehaviour
    {
        // In-game clock span 13:20 → 19:04 = 5h 44m.
        private const float SunsetSpanSeconds = (5f * 3600f) + (44f * 60f);

        [SerializeField] private StoryConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private NearMissSystem nearMiss;
        [SerializeField] private DynamicEnvironmentManager dayCycle;
        [SerializeField] private StageManager stages;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private UI_PhoneOverlay phone;

        private Canvas _canvas;
        private RectTransform _root;
        private Image _fill;
        private Image _track;
        private RectTransform _playerDot;
        private RectTransform _towerIcon;
        private RectTransform _himIcon;
        private CanvasGroup _himCg;
        private Text _timerLabel;
        private Text _remainingLabel;
        private Text _cheerLabel;
        private RectTransform _cheerRt;
        private Text _monologueLabel;
        private CanvasGroup _monologueCg;
        private CanvasGroup _scoreCg;
        private CanvasGroup _comboCg;
        private CanvasGroup _coinCg;
        private CanvasGroup _progressCg;
        private CanvasGroup _timerCg;
        private CanvasGroup _phoneCg;
        private int _cheerIndex;
        private Coroutine _cheerRoutine;
        private Coroutine _monologueRoutine;
        private readonly bool[] _hudLayerRemoved = new bool[4];
        private int _score;
        private int _lastCombo;

        private static readonly string[][] CheerByChapter =
        {
            new[] { "좋아", "감 잡았어", "이 정도야 뭐" },
            new[] { "지나갈게요", "조금만 더 기다려줘", "미안해요" },
            new[] { "아직 안 늦었어", "빨리", "괜찮아" },
            new[] { "제발", "조금만", "거의 다 왔어" }
        };

        /// S17=0 score, S18=1 combo, S19=2 coins, S20=3 chrome (bar/timer/phone).
        public event Action<int> OnHudLayerRemoved;

        public void Bind(StoryConfig storyConfig, PlayerController playerController,
            UpgradeManager upgradeManager, NearMissSystem nearMissSystem,
            DynamicEnvironmentManager env, StageManager stageManager = null,
            UI_FeedbackController feedbackUi = null, UI_PhoneOverlay phoneOverlay = null)
        {
            config = storyConfig != null ? storyConfig : ScriptableObject.CreateInstance<StoryConfig>();
            player = playerController;
            upgrades = upgradeManager;
            nearMiss = nearMissSystem;
            dayCycle = env;
            stages = stageManager != null ? stageManager : StageManager.Instance;
            feedback = feedbackUi;
            phone = phoneOverlay;

            BuildUi();
            feedback?.StripRunChrome();
            if (feedback != null)
            {
                _coinCg = feedback.CoinCanvasGroup;
                // Score / combo now live in the Subway-Surfers-style pills; the CH5
                // strip schedule fades those groups instead of private labels.
                if (feedback.Chrome != null)
                {
                    _scoreCg = feedback.Chrome.ScoreGroup;
                    _comboCg = feedback.Chrome.ComboGroup;
                    _coinCg = feedback.Chrome.CoinGroup;
                }
            }

            if (nearMiss != null)
            {
                nearMiss.OnNearMissRewarded -= HandleNearMiss;
                nearMiss.OnNearMissRewarded += HandleNearMiss;
            }

            if (stages != null)
            {
                stages.OnStageStart -= HandleStageStart;
                stages.OnStageStart += HandleStageStart;
            }
        }

        private void OnDestroy()
        {
            if (nearMiss != null)
                nearMiss.OnNearMissRewarded -= HandleNearMiss;
            if (stages != null)
                stages.OnStageStart -= HandleStageStart;
        }

        private void HandleStageStart(StageDef stage)
        {
            if (stage == null)
                return;

            ApplyChapterVisuals(stage.chapterIndex);
            phone?.SetChapter(stage.chapterIndex);

            // CH5 strip schedule — fade 3s, fire stem event once per layer.
            if (stage.stageIndex == 17)
                BeginRemoveHudLayer(0, _scoreCg);
            else if (stage.stageIndex == 18)
                BeginRemoveHudLayer(1, _comboCg);
            else if (stage.stageIndex == 19)
                BeginRemoveHudLayer(2, _coinCg);
            else if (stage.stageIndex == 20)
                BeginRemoveHudLayer(3, null); // special: bar+timer+phone, keep remaining distance
        }

        private void ApplyChapterVisuals(int chapter)
        {
            bool showHim = chapter >= 2 && chapter <= 4;
            if (_himIcon != null)
                _himIcon.gameObject.SetActive(showHim);
            if (_himCg != null)
                _himCg.alpha = showHim ? 0.35f : 0f;

            if (_fill != null)
            {
                // CH5: orange → blue. Earlier chapters: warm cyan/orange journey feel.
                _fill.color = chapter >= 5
                    ? new Color(0.35f, 0.55f, 0.95f, 1f)
                    : new Color(1f, 0.55f, 0.28f, 1f);
            }
        }

        private void BeginRemoveHudLayer(int layer, CanvasGroup group)
        {
            if (layer < 0 || layer >= _hudLayerRemoved.Length || _hudLayerRemoved[layer])
                return;
            _hudLayerRemoved[layer] = true;
            OnHudLayerRemoved?.Invoke(layer);
            StartCoroutine(FadeOutLayer(layer, group));
        }

        private IEnumerator FadeOutLayer(int layer, CanvasGroup group)
        {
            const float dur = 3f;
            if (layer == 3)
            {
                // Progress + timer + phone → remaining distance only.
                yield return FadeGroups(dur, _progressCg, _timerCg, _phoneCg);
                if (_remainingLabel != null)
                {
                    _remainingLabel.gameObject.SetActive(true);
                    var cg = _remainingLabel.GetComponent<CanvasGroup>() ??
                             _remainingLabel.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    float t = 0f;
                    while (t < 0.8f)
                    {
                        t += Time.unscaledDeltaTime;
                        cg.alpha = Mathf.Clamp01(t / 0.8f);
                        yield return null;
                    }

                    cg.alpha = 1f;
                }

                yield break;
            }

            if (group != null)
                yield return FadeGroups(dur, group);
        }

        private static IEnumerator FadeGroups(float duration, params CanvasGroup[] groups)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / duration);
                for (int i = 0; i < groups.Length; i++)
                {
                    if (groups[i] != null)
                        groups[i].alpha = a;
                }

                yield return null;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null)
                    continue;
                groups[i].alpha = 0f;
                groups[i].gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            float progress = stages != null
                ? stages.JourneyProgress01
                : (player != null && upgrades != null
                    ? Mathf.Clamp01(player.PathDistance / Mathf.Max(1f, upgrades.TowerDistance))
                    : 0f);

            if (_fill != null)
                _fill.fillAmount = progress;

            // Player dot on the bar — tower stays pinned at the RIGHT end (never "fills").
            if (_playerDot != null)
            {
                _playerDot.anchorMin = _playerDot.anchorMax = new Vector2(Mathf.Clamp01(progress), 0.5f);
                _playerDot.anchoredPosition = Vector2.zero;
            }

            if (_towerIcon != null)
            {
                _towerIcon.anchorMin = _towerIcon.anchorMax = new Vector2(1f, 0.5f);
                _towerIcon.anchoredPosition = new Vector2(8f, 10f);
            }

            if (_himIcon != null && _himIcon.gameObject.activeSelf)
            {
                _himIcon.anchorMin = _himIcon.anchorMax = new Vector2(1f, 0.5f);
                _himIcon.anchoredPosition = new Vector2(8f, 28f);
            }

            // Distance-linked D-Day (not wall-clock).
            float remaining = (1f - progress) * SunsetSpanSeconds;
            if (_timerLabel != null && (_timerCg == null || _timerCg.gameObject.activeSelf))
            {
                int sec = Mathf.CeilToInt(remaining);
                int h = sec / 3600;
                int m = (sec % 3600) / 60;
                int s = sec % 60;
                _timerLabel.text = h > 0
                    ? string.Format("노을까지  {0}:{1:00}:{2:00}", h, m, s)
                    : string.Format("노을까지  {0:00}:{1:00}", m, s);
                _timerLabel.color = remaining < 1200f
                    ? new Color(1f, 0.55f, 0.35f)
                    : new Color(0.9f, 0.95f, 1f);
            }

            if (_remainingLabel != null && _remainingLabel.gameObject.activeSelf && stages != null)
            {
                _remainingLabel.text = Mathf.CeilToInt(stages.RemainingJourneyDistance) + " m";
            }

            UpdateCheerFollow();
        }

        private void UpdateCheerFollow()
        {
            if (_cheerRt == null || !_cheerRt.gameObject.activeSelf || player == null)
                return;

            Camera cam = Camera.main;
            if (cam == null || _canvas == null)
                return;

            Vector3 world = player.transform.position + Vector3.up * 2.1f;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, screen, _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out Vector2 local);
            _cheerRt.anchoredPosition = local;
        }

        private void HandleNearMiss(int reward, int combo, Vector3 worldPos)
        {
            _score += reward;
            _lastCombo = combo;

            int chapter = stages != null ? stages.ChapterIndex : 1;
            // CH5: no cheer — coins rise quietly.
            if (chapter >= 5)
                return;

            if (combo < 3 || UnityEngine.Random.value > 0.5f)
                return;

            var pool = CheerByChapter[Mathf.Clamp(chapter - 1, 0, CheerByChapter.Length - 1)];
            _cheerIndex = (_cheerIndex + 1) % pool.Length;
            ShowCheer(pool[_cheerIndex]);
        }

        private void ShowCheer(string line)
        {
            if (_cheerLabel == null)
                return;

            _cheerLabel.text = line;
            _cheerLabel.gameObject.SetActive(true);
            if (_cheerRoutine != null)
                StopCoroutine(_cheerRoutine);
            _cheerRoutine = StartCoroutine(HideCheer(2.2f));
        }

        private IEnumerator HideCheer(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_cheerLabel != null)
                _cheerLabel.gameObject.SetActive(false);
            _cheerRoutine = null;
        }

        public void ShowArrival()
        {
            ShowMonologue("도착했어… 여기야.");
            if (_timerLabel != null)
                _timerLabel.text = "만남";
        }

        public void ShowStoryLine(string line) => ShowMonologue(line);

        public void ShowMonologue(string line)
        {
            if (_monologueLabel == null)
                return;

            _monologueLabel.text = line;
            _monologueLabel.gameObject.SetActive(true);
            if (_monologueCg != null)
                _monologueCg.alpha = 1f;
            if (_monologueRoutine != null)
                StopCoroutine(_monologueRoutine);
            _monologueRoutine = StartCoroutine(HideMonologue(3.5f));
        }

        private IEnumerator HideMonologue(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_monologueCg != null)
            {
                float f = 0f;
                while (f < 0.4f)
                {
                    f += Time.unscaledDeltaTime;
                    _monologueCg.alpha = 1f - f / 0.4f;
                    yield return null;
                }
            }

            if (_monologueLabel != null)
                _monologueLabel.gameObject.SetActive(false);
            _monologueRoutine = null;
        }

        /// Legacy act-label hook — do not paint permanent HUD text (journey HUD has no act strip).
        public void SetActLabel(string label)
        {
            // Intentionally empty: act names must not reappear as run chrome.
        }

        private void BuildUi()
        {
            if (_root != null)
                return;

            var hud = GameObject.Find("CoastRunHUD");
            if (hud != null)
                _canvas = hud.GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = CoastUiCanvas.Create("JourneyHUD", 105);

            // No full-width navy chrome — floating journey widgets only.
            var rootGo = new GameObject("JourneyHudRoot", typeof(RectTransform));
            rootGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            _root = rootGo.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            BuildProgressBar();
            BuildTimer();
            BuildCheer();
            BuildMonologue();
            BuildRemainingDistance();
        }

        private void BuildProgressBar()
        {
            var wrap = new GameObject("ProgressWrap", typeof(RectTransform), typeof(CanvasGroup));
            wrap.transform.SetParent(_root, false);
            var wrt = wrap.GetComponent<RectTransform>();
            // Below the pause / score / coin row so the top corners stay clean.
            wrt.anchorMin = new Vector2(0.10f, 1f);
            wrt.anchorMax = new Vector2(0.90f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = new Vector2(0f, -138f);
            wrt.sizeDelta = new Vector2(0f, 40f);
            _progressCg = wrap.GetComponent<CanvasGroup>();

            var start = MakeText(wrap.transform, "Start", "◀", 14,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(24f, 24f));
            start.alignment = TextAnchor.MiddleLeft;

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(wrap.transform, false);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.06f, 0.25f);
            trackRt.anchorMax = new Vector2(0.88f, 0.65f);
            trackRt.offsetMin = Vector2.zero;
            trackRt.offsetMax = Vector2.zero;
            _track = track.GetComponent<Image>();
            // Same chunky pill language as the top bar: cream ring around a navy track.
            _track.sprite = CoastUiArt.RoundedRect(12);
            _track.type = Image.Type.Sliced;
            _track.color = new Color(0.08f, 0.12f, 0.26f, 0.95f);
            var ring = CoastUiArt.Panel(track.transform, "Ring", CoastUiArt.CreamOutline, 14);
            var ringRt = ring.rectTransform;
            ringRt.anchorMin = Vector2.zero; ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = new Vector2(-3f, -3f); ringRt.offsetMax = new Vector2(3f, 3f);
            ring.transform.SetAsFirstSibling();

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _fill = fillGo.GetComponent<Image>();
            _fill.sprite = CoastUiArt.RoundedRect(10);
            _fill.color = new Color(1f, 0.55f, 0.28f, 1f);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 0f;

            _playerDot = CreateMarker(trackRt, "PlayerDot", new Color(1f, 0.95f, 0.85f), "●");
            _playerDot.sizeDelta = new Vector2(22f, 22f);
            var dotStar = CoastUiArt.Icon("Star");
            if (dotStar != null)
            {
                // The runner's marker is the same little star as the multiplier badge.
                foreach (Transform c in _playerDot) UnityEngine.Object.Destroy(c.gameObject);
                var di = _playerDot.GetComponent<Image>() ?? _playerDot.gameObject.AddComponent<Image>();
                di.sprite = dotStar; di.color = Color.white; di.preserveAspect = true; di.raycastTarget = false;
                _playerDot.sizeDelta = new Vector2(30f, 30f);
            }

            // Tower always at RIGHT end — empty/unfilled silhouette.
            _towerIcon = CreateMarker(trackRt, "Tower", new Color(0.75f, 0.8f, 0.85f), null, "Icon_Tower");
            _towerIcon.sizeDelta = new Vector2(26f, 26f);
            var towerImg = _towerIcon.GetComponent<Image>();
            if (towerImg != null)
                towerImg.color = new Color(1f, 1f, 1f, 0.55f); // "empty" look

            _himIcon = CreateMarker(trackRt, "Him", new Color(0.95f, 0.55f, 0.45f), null, "Icon_Him");
            _himIcon.sizeDelta = new Vector2(22f, 22f);
            _himCg = _himIcon.gameObject.AddComponent<CanvasGroup>();
            _himCg.alpha = 0f;
            _himIcon.gameObject.SetActive(false);
        }

        private void BuildTimer()
        {
            var go = new GameObject("DDay", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -176f);
            rt.sizeDelta = new Vector2(260f, 28f);
            _timerCg = go.GetComponent<CanvasGroup>();
            var pill = CoastUiArt.CutePill(go.transform, "Pill", new Color(0.10f, 0.14f, 0.30f, 0.92f), 14, 3);
            var prt = pill.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(190f, 30f);
            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = new Vector2(0f, -2f);
            _timerLabel = textGo.AddComponent<Text>();
            CoastUiArt.OutlineText(_timerLabel, new Color(0.05f, 0.07f, 0.18f, 0.9f), 1.2f);
            _timerLabel.font = CoastHudLayout.Font();
            _timerLabel.fontSize = 16;
            _timerLabel.fontStyle = FontStyle.Bold;
            _timerLabel.alignment = TextAnchor.MiddleCenter;
            _timerLabel.color = new Color(0.9f, 0.95f, 1f);
            _timerLabel.raycastTarget = false;
            _timerLabel.text = "노을까지  --:--";
        }

        private void BuildCheer()
        {
            var go = new GameObject("Cheer", typeof(RectTransform));
            go.transform.SetParent(_root, false);
            _cheerRt = go.GetComponent<RectTransform>();
            _cheerRt.anchorMin = _cheerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _cheerRt.sizeDelta = new Vector2(360f, 40f);
            _cheerLabel = go.AddComponent<Text>();
            _cheerLabel.font = CoastHudLayout.Font();
            _cheerLabel.fontSize = 22;
            _cheerLabel.fontStyle = FontStyle.Bold;
            _cheerLabel.alignment = TextAnchor.MiddleCenter;
            _cheerLabel.color = new Color(1f, 0.92f, 0.55f);
            _cheerLabel.raycastTarget = false;
            go.SetActive(false);
        }

        private void BuildMonologue()
        {
            var go = new GameObject("Monologue", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 48f);
            rt.sizeDelta = new Vector2(560f, 72f);
            _monologueCg = go.GetComponent<CanvasGroup>();
            _monologueLabel = go.AddComponent<Text>();
            _monologueLabel.font = CoastHudLayout.Font();
            _monologueLabel.fontSize = 20;
            _monologueLabel.fontStyle = FontStyle.Bold;
            _monologueLabel.alignment = TextAnchor.MiddleCenter;
            _monologueLabel.color = new Color(0.92f, 0.95f, 1f);
            _monologueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _monologueLabel.raycastTarget = false;
            go.SetActive(false);
        }

        private void BuildRemainingDistance()
        {
            var go = new GameObject("RemainingDistance", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(240f, 40f);
            _remainingLabel = go.AddComponent<Text>();
            _remainingLabel.font = CoastHudLayout.Font();
            _remainingLabel.fontSize = 28;
            _remainingLabel.fontStyle = FontStyle.Bold;
            _remainingLabel.alignment = TextAnchor.MiddleCenter;
            _remainingLabel.color = new Color(0.85f, 0.9f, 1f);
            _remainingLabel.raycastTarget = false;
            go.SetActive(false);
        }

        public void AttachPhoneCanvasGroup(CanvasGroup phoneGroup)
        {
            _phoneCg = phoneGroup;
        }

        private static RectTransform CreateMarker(Transform parent, string name, Color color,
            string glyph = null, string iconResource = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(28f, 28f);
            var img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(iconResource))
            {
                var sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture(iconResource), 100f);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                    img.color = color;
            }
            else
                img.color = color;

            if (!string.IsNullOrEmpty(glyph))
            {
                var t = MakeText(go.transform, "G", glyph, 16,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
                t.color = color;
            }

            return rt;
        }

        private static Text MakeText(Transform parent, string name, string content, int size,
            Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = CoastHudLayout.Font();
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }
    }
}
