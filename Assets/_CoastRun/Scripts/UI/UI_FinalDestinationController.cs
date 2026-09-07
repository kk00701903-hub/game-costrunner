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
                _towerIcon.anchoredPosition = new Vector2(-2f, 6f);
            }

            if (_himIcon != null && _himIcon.gameObject.activeSelf)
            {
                _himIcon.anchorMin = _himIcon.anchorMax = new Vector2(1f, 0.5f);
                _himIcon.anchoredPosition = new Vector2(8f, 28f);
            }

            // 8차 노을 규칙: 진짜 시간으로 해가 진다. 남은 초를 보여주고, 지나면 '해가 졌어'.
            var sm = StageManager.Instance;
            if (_timerLabel != null && (_timerCg == null || _timerCg.gameObject.activeSelf))
            {
                if (sm != null && !ArcadeRun.Active)
                {
                    float left = sm.SunsetSeconds * (1f - sm.SunsetT);
                    if (!sm.SunsetLate)
                    {
                        int sec = Mathf.CeilToInt(left);
                        _timerLabel.text = Loc.T("노을까지  ", "sunset in  ") + string.Format("{0}:{1:00}", sec / 60, sec % 60);
                        _timerLabel.color = left < 12f ? new Color(1f, 0.55f, 0.35f) : new Color(0.9f, 0.95f, 1f);
                    }
                    else
                    {
                        _timerLabel.text = Loc.T("해가 졌어…", "sun is down…");
                        _timerLabel.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), new Color(1f, 0.8f, 0.8f), 0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
                    }
                }
                else if (ArcadeRun.Active)
                {
                    // 9차: 무한 모드엔 노을 시계가 없다 — 달린 거리를 보여준다.
                    float d = sm != null ? sm.StageLocalDistance : (player != null ? player.PathDistance : 0f);
                    _timerLabel.text = Loc.T("거리  ", "dist  ") + Mathf.RoundToInt(d) + " m";
                    _timerLabel.color = new Color(0.9f, 0.95f, 1f);
                }
                else
                {
                    float remaining = (1f - progress) * SunsetSpanSeconds;
                    int sec = Mathf.CeilToInt(remaining);
                    int h = sec / 3600, m = (sec % 3600) / 60, s2 = sec % 60;
                    _timerLabel.text = h > 0 ? string.Format("노을까지  {0}:{1:00}:{2:00}", h, m, s2) : string.Format("노을까지  {0:00}:{1:00}", m, s2);
                    _timerLabel.color = new Color(0.9f, 0.95f, 1f);
                }
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
            // 14차: 상단 중앙 타임바 하나 — 좌(하트) / 중(노을·여정) / 우(점수·코인) 세 덩어리로 정리.
            wrt.anchorMin = new Vector2(0.265f, 1f);
            wrt.anchorMax = new Vector2(0.72f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = new Vector2(0f, -84f);   // 하트·코인과 같은 둘째 줄 → 상단 2줄로 끝
            wrt.sizeDelta = new Vector2(0f, 42f);
            _progressCg = wrap.GetComponent<CanvasGroup>();

            var start = MakeText(wrap.transform, "Start", "◀", 14,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(24f, 24f));
            start.alignment = TextAnchor.MiddleLeft;

            // 10차: 크림 테두리를 자식이 아니라 '바깥 알약'으로 — 자식이던 테두리가 남색 트랙을 덮어 바 전체가 크림색으로 보였다.
            var ring = CoastUiArt.Panel(wrap.transform, "Ring", CoastUiArt.CreamOutline, 16);
            var ringRt = ring.rectTransform;
            ringRt.anchorMin = new Vector2(0.06f, 0.06f);
            ringRt.anchorMax = new Vector2(0.90f, 0.94f);
            ringRt.offsetMin = Vector2.zero; ringRt.offsetMax = Vector2.zero;
            ring.raycastTarget = false;
            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(ring.transform, false);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = Vector2.zero; trackRt.anchorMax = Vector2.one;
            trackRt.offsetMin = new Vector2(3f, 3f); trackRt.offsetMax = new Vector2(-3f, -3f);
            _track = track.GetComponent<Image>();
            _track.sprite = CoastUiArt.RoundedRect(13);
            _track.type = Image.Type.Sliced;
            _track.color = new Color(0.08f, 0.12f, 0.26f, 0.95f);
            _track.raycastTarget = false;

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
            // 12차: 하늘 위에 회색 탑 아이콘만 떠 있어 '우상단 글자 찌꺼기'처럼 보였다.
            // 남색 동그라미 배지 위에 얹어 목적지 마커로 읽히게.
            var towerBadge = CoastUiArt.Panel(trackRt, "TowerBadge", new Color(0.08f, 0.12f, 0.26f, 0.95f), 20);
            var tbRt = towerBadge.rectTransform;
            tbRt.anchorMin = tbRt.anchorMax = new Vector2(1f, 0.5f);
            tbRt.pivot = new Vector2(0.5f, 0.5f);
            tbRt.anchoredPosition = new Vector2(-2f, 6f);
            tbRt.sizeDelta = new Vector2(42f, 42f);
            towerBadge.raycastTarget = false;
            var tbRing = CoastUiArt.Panel(tbRt, "Ring", CoastUiArt.CreamOutline, 22);
            var tbrRt = tbRing.rectTransform; tbrRt.anchorMin = Vector2.zero; tbrRt.anchorMax = Vector2.one;
            tbrRt.offsetMin = new Vector2(-2f, -2f); tbrRt.offsetMax = new Vector2(2f, 2f);
            tbRing.raycastTarget = false; tbRing.transform.SetAsFirstSibling();
            _towerIcon = CreateMarker(trackRt, "Tower", new Color(0.75f, 0.8f, 0.85f), null, "Icon_Tower");
            _towerIcon.sizeDelta = new Vector2(34f, 34f);
            var towerImg = _towerIcon.GetComponent<Image>();
            if (towerImg != null)
                towerImg.color = new Color(1f, 1f, 1f, 1f);

            _himIcon = CreateMarker(trackRt, "Him", new Color(0.95f, 0.55f, 0.45f), null, "Icon_Him");
            _himIcon.sizeDelta = new Vector2(22f, 22f);
            _himCg = _himIcon.gameObject.AddComponent<CanvasGroup>();
            _himCg.alpha = 0f;
            _himIcon.gameObject.SetActive(false);
        }

        private void BuildTimer()
        {
            // 10차: 별도 알약을 없애고 여정 바 '안'에 노을 시계를 넣는다 — 상단 3줄 → 2줄, 스타일 통일.
            var go = new GameObject("DDay", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_track != null ? _track.transform : _root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _timerCg = go.GetComponent<CanvasGroup>();
            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(40f, 0f); trt.offsetMax = new Vector2(-44f, -1f);
            _timerLabel = textGo.AddComponent<Text>();
            CoastUiArt.OutlineText(_timerLabel, new Color(0.05f, 0.07f, 0.18f, 0.95f), 1.5f);
            _timerLabel.font = CoastHudLayout.Font();
            _timerLabel.fontSize = CoastHudLayout.Scaled(18);
            _timerLabel.fontStyle = FontStyle.Bold;
            _timerLabel.alignment = TextAnchor.MiddleCenter;
            _timerLabel.color = new Color(0.9f, 0.95f, 1f);
            _timerLabel.raycastTarget = false;
            _timerLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
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
            _cheerLabel.fontSize = CoastHudLayout.Scaled(22);
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
            _monologueLabel.fontSize = CoastHudLayout.Scaled(20);
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
            _remainingLabel.fontSize = CoastHudLayout.Scaled(28);
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
