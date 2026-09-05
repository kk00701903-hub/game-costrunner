using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Stage clear: settle the run, offer upgrades, then hand off to the story beat.
    ///
    ///     정산 (count-up)  →  아이템/업그레이드  →  회상 조각  →  다음 스테이지
    ///
    /// The settlement doubles as the journey's pacing beat. Subway Surfers ends a run
    /// with a score; this ends it with how much closer the tower is and how much light
    /// is left, so the numbers carry the story instead of interrupting it.
    public class StageClearUI : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private UpgradeShopUI shop;

        private Canvas _canvas;
        private GameObject _root;
        private Text _title;
        private Text _stageLabel;
        private Text _lineCoins;
        private Text _lineNearMiss;
        private Text _lineCombo;
        private Text _lineTotal;
        private Text _lineHeld;
        private Text _journey;
        private Image _journeyFill;
        private GameObject _shopHost;
        private Button _continueBtn;
        private Button _retryBtn;
        private Action _onContinue;
        private Action _onRetry;
        private Coroutine _settle;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void Bind(UpgradeManager upgradeManager, CoinWallet coinWallet,
            UI_FeedbackController ui, UpgradeShopUI shopUi)
        {
            upgrades = upgradeManager;
            wallet = coinWallet;
            feedback = ui;
            shop = shopUi;
            EnsureBuilt();
            Hide();
        }

        public void Show(StageDef stage, bool chapterComplete, Action onContinue, Action onRetry)
        {
            EnsureBuilt();
            _onContinue = onContinue;
            _onRetry = onRetry;

            _title.text = chapterComplete ? $"CHAPTER {stage.chapterIndex} COMPLETE" : "STAGE CLEAR";
            _title.color = chapterComplete ? CoastHudLayout.AccentWarm : CoastHudLayout.AccentCyan;
            _stageLabel.text = $"S{stage.stageIndex:00}  {stage.stageName}";

            string continueLabel = stage.stageIndex >= 20 ? "도착" : "다음 스테이지";
            if (GameManager.Active)
            {
                // v2: 스테이지 = 챕터. 말랑이 하트와 등급이 이 화면의 주인공.
                var gm = GameManager.I;
                var rec = gm.Save.CurrentChapter;
                var grade = gm.LastGrade;
                _title.text = $"CHAPTER {gm.Save.chapter}  ·  {ChapterGrading.GradeLabel(grade)}급";
                _title.color = grade == ChapterGrade.S ? new Color(1f, 0.85f, 0.3f) : CoastHudLayout.AccentCyan;
                string heartLine = rec != null
                    ? $"말랑이 하트 {rec.heartsEarned} / {rec.heartsTarget}  (런닝 +{gm.LastRunHearts})"
                    : $"말랑이 하트 +{gm.LastRunHearts}";
                if (grade != ChapterGrade.S)
                    heartLine += $"   ·   S급까지 {Mathf.CeilToInt((rec != null ? rec.heartsTarget : 0) * ChapterGrading.S_Ratio) - (rec != null ? rec.heartsEarned : 0)}개";
                if (gm.IsRetry)
                    heartLine += gm.LastImproved ? "   ·   기록 갱신!" : "   ·   이전 기록 유지";
                _stageLabel.text = $"{stage.stageName}\n{heartLine}";
                continueLabel = gm.IsRetry ? "타임라인으로" : gm.Save.chapter >= Timeline.Chapters ? "송전탑으로" : "육성으로";
            }

            if (_continueBtn != null)
            {
                var label = _continueBtn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = continueLabel;
            }

            _root.SetActive(true);

            if (_settle != null)
                StopCoroutine(_settle);
            _settle = StartCoroutine(Settle(stage));
        }

        public void ShowFinal(StageDef stage, Action onContinue, Action onRetry)
        {
            Show(stage, true, onContinue, onRetry);
            _title.text = "ARRIVAL";
            _stageLabel.text = "S20  송전탑";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            // 에디터 검증용: 정산 화면에서 Return = 계속, R = 다시.
            if (!IsVisible || _settle != null) return;
            if (Input.GetKeyDown(KeyCode.Return)) _onContinue?.Invoke();
            else if (Input.GetKeyDown(KeyCode.R)) _onRetry?.Invoke();
        }
#endif

        public void Hide()
        {
            if (_settle != null)
            {
                StopCoroutine(_settle);
                _settle = null;
            }
            if (_root != null)
                _root.SetActive(false);
            shop?.HidePanel();
        }

        // ────────────────────────────────────────────────────────────────
        // Settlement
        // ────────────────────────────────────────────────────────────────

        /// Lines land one at a time. The pause between them is the point — it lets the
        /// run settle before the next stage asks for attention again.
        private IEnumerator Settle(StageDef stage)
        {
            var stats = StageRunStats.Instance;
            int coinValue = stats != null ? stats.CoinValue : 0;
            int coinCount = stats != null ? stats.Coins : 0;
            int nmValue = stats != null ? stats.NearMissValue : 0;
            int nmCount = stats != null ? stats.NearMissCount : 0;
            int bestCombo = stats != null ? stats.BestCombo : 0;
            bool flawless = stats != null && stats.Flawless;
            float seconds = stats != null ? stats.Seconds : 0f;

            _lineCoins.text = "";
            _lineNearMiss.text = "";
            _lineCombo.text = "";
            _lineTotal.text = "";
            _lineHeld.text = "";
            if (_shopHost != null)
                _shopHost.SetActive(false);
            SetButtons(false);

            yield return Wait(0.25f);

            _lineCoins.text = Row("코인", $"×{coinCount}", coinValue);
            yield return Wait(0.22f);

            _lineNearMiss.text = Row("니어미스", $"×{nmCount}", nmValue);
            yield return Wait(0.22f);

            if (bestCombo > 1)
                _lineCombo.text = Row("최고 콤보", $"×{bestCombo}", 0, showValue: false);
            else if (flawless)
                _lineCombo.text = Row("무피해", "", 0, showValue: false);
            yield return Wait(0.22f);

            // Count the total up rather than stamping it — the same trick the coin HUD
            // uses in-run, so the two read as one language.
            int total = coinValue + nmValue;
            yield return CountUp(_lineTotal, "합계", total, 0.45f);

            _lineHeld.text = Row("보유", "", wallet != null ? wallet.TotalCoins : 0);
            yield return Wait(0.15f);

            UpdateJourney(stage, seconds);
            yield return Wait(0.2f);

            // v2: 업그레이드 상점은 펫 상점(육성 화면)으로 대체 — 정산엔 숫자만.
            if (!GameManager.Active)
            {
                if (_shopHost != null)
                    _shopHost.SetActive(true);
                shop?.ShowInPanel(_shopHost != null ? _shopHost.transform : _root.transform);
            }
            SetButtons(true);
            _settle = null;
        }

        private IEnumerator CountUp(Text target, string label, int value, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                int shown = Mathf.RoundToInt(Mathf.Lerp(0f, value, EaseOutQuad(t / duration)));
                target.text = Row(label, "", shown);
                yield return null;
            }
            target.text = Row(label, "", value);
        }

        private static float EaseOutQuad(float x)
        {
            x = Mathf.Clamp01(x);
            return 1f - (1f - x) * (1f - x);
        }

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static string Row(string label, string count, int value, bool showValue = true)
        {
            string mid = string.IsNullOrEmpty(count) ? "" : "   " + count;
            string right = showValue ? "   " + value.ToString("N0") : "";
            return label + mid + right;
        }

        /// Distance left to the tower and the light that is left, in one line each.
        private void UpdateJourney(StageDef stage, float seconds)
        {
            var stages = StageManager.Instance;
            if (stages == null)
            {
                _journey.text = "";
                return;
            }

            float progress = Mathf.Clamp01(stages.JourneyProgress01);
            if (_journeyFill != null)
                _journeyFill.rectTransform.anchorMax = new Vector2(progress, 1f);

            float remainingKm = stages.RemainingJourneyDistance / 1000f;
            _journey.text =
                $"송전탑까지 {remainingKm:0.0} km   ·   {ClockAt(stage.lightingTEnd)}" +
                $"   ·   {StageRunStats.FormatTime(seconds)}";
        }

        /// The run spans 13:20 → 19:04 as one unbroken afternoon; lightingT is that clock.
        private static string ClockAt(float t)
        {
            const int startMinutes = 13 * 60 + 20;
            const int endMinutes = 19 * 60 + 4;
            int m = Mathf.RoundToInt(Mathf.Lerp(startMinutes, endMinutes, Mathf.Clamp01(t)));
            return $"{m / 60:00}:{m % 60:00}";
        }

        private void SetButtons(bool on)
        {
            if (_continueBtn != null)
                _continueBtn.gameObject.SetActive(on);
            if (_retryBtn != null)
                _retryBtn.gameObject.SetActive(on);
        }

        // ────────────────────────────────────────────────────────────────

        private void EnsureBuilt()
        {
            if (_root != null)
                return;

            _canvas = CoastUiCanvas.Create("StageClearCanvas", 200);
            _root = new GameObject("StageClearRoot", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _root.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.12f, 0.86f);

            _title = Label("Title", "STAGE CLEAR", 34, 0.86f, 0.94f);
            _title.color = CoastHudLayout.AccentCyan;

            _stageLabel = Label("Stage", "", 20, 0.80f, 0.86f);
            _stageLabel.color = new Color(0.75f, 0.82f, 0.88f);

            _lineCoins = Label("Coins", "", 21, 0.705f, 0.755f);
            _lineNearMiss = Label("NearMiss", "", 21, 0.655f, 0.705f);
            _lineCombo = Label("Combo", "", 19, 0.61f, 0.655f);
            _lineCombo.color = CoastHudLayout.AccentWarm;

            _lineTotal = Label("Total", "", 26, 0.545f, 0.605f);
            _lineTotal.color = CoastHudLayout.AccentCyan;

            _lineHeld = Label("Held", "", 17, 0.505f, 0.545f);
            _lineHeld.color = new Color(0.62f, 0.68f, 0.74f);

            BuildJourneyBar();

            _shopHost = new GameObject("UpgradeHost", typeof(RectTransform));
            _shopHost.transform.SetParent(_root.transform, false);
            var sht = _shopHost.GetComponent<RectTransform>();
            sht.anchorMin = new Vector2(0.06f, 0.24f);
            sht.anchorMax = new Vector2(0.94f, 0.44f);
            sht.offsetMin = Vector2.zero;
            sht.offsetMax = Vector2.zero;

            _continueBtn = MakeButton(_root.transform, "Continue", new Vector2(0.52f, 0.08f),
                new Vector2(0.92f, 0.185f), "다음 스테이지", () => _onContinue?.Invoke());
            _retryBtn = MakeButton(_root.transform, "Retry", new Vector2(0.08f, 0.08f),
                new Vector2(0.48f, 0.185f), "다시", () => _onRetry?.Invoke());
        }

        private void BuildJourneyBar()
        {
            var track = new GameObject("JourneyTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(_root.transform, false);
            var trt = track.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.1f, 0.475f);
            trt.anchorMax = new Vector2(0.9f, 0.487f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            _journeyFill = fill.GetComponent<Image>();
            _journeyFill.color = CoastHudLayout.AccentCyan;
            var frt = _journeyFill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;

            _journey = Label("Journey", "", 15, 0.44f, 0.472f);
            _journey.color = new Color(0.68f, 0.75f, 0.82f);
        }

        private Text Label(string name, string value, int size, float yMin, float yMax)
        {
            return CoastHudLayout.MakeText(_root.transform, name, value, size,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, yMin), new Vector2(0.92f, yMax), Vector2.zero, Vector2.zero);
        }

        private static Button MakeButton(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            string label, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.12f, 0.28f, 0.42f, 0.95f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            CoastHudLayout.MakeText(go.transform, "Label", label, 19, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }
    }
}
