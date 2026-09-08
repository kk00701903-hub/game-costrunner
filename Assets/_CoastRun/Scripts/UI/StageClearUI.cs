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

            _title.text = chapterComplete ? $"CHAPTER {stage.chapterIndex} COMPLETE!" : "STAGE CLEAR!";
            _title.color = new Color(1f, 0.93f, 0.55f);
            _stageLabel.text = $"S{stage.stageIndex:00}  {stage.stageName}";

            string continueLabel = stage.stageIndex >= 20 ? Loc.T("도착", "Arrived") : Loc.T("다음 스테이지", "Next stage");
            if (GameManager.Active)
            {
                // v2: 스테이지 = 챕터. 말랑이 하트와 등급이 이 화면의 주인공.
                var gm = GameManager.I;
                var rec = gm.Save.CurrentChapter;
                var grade = gm.LastGrade;
                _title.text = Loc.T($"CHAPTER {gm.Save.chapter}  ·  {ChapterGrading.GradeLabel(grade)}급", $"CHAPTER {gm.Save.chapter}  ·  RANK {ChapterGrading.GradeLabel(grade)}");
                _title.color = grade == ChapterGrade.S ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.93f, 0.55f);
                CoastAudioManager.PlayAnywhere(grade == ChapterGrade.S ? CoastSfx.RankS : CoastSfx.ChapterClear);
                string heartLine = rec != null
                    ? Loc.T($"말랑이 하트 {rec.heartsEarned} / {rec.heartsTarget}  (런닝 +{gm.LastRunHearts})", $"Hearts {rec.heartsEarned} / {rec.heartsTarget}  (run +{gm.LastRunHearts})")
                    : Loc.T($"말랑이 하트 +{gm.LastRunHearts}", $"Hearts +{gm.LastRunHearts}");
                // 8차 노을 규칙
                if (gm.LastRunLate) heartLine = Loc.T("해가 진 뒤에 도착했어… 하트 −40%   ·   ", "Arrived after sunset… hearts −40%   ·   ") + heartLine;
                else if (gm.LastRunEarly) heartLine = Loc.T("노을 안에 도착! 하트 +10%   ·   ", "Made it before sunset! hearts +10%   ·   ") + heartLine;
                if (grade != ChapterGrade.S)
                {
                    int need = Mathf.CeilToInt((rec != null ? rec.heartsTarget : 0) * ChapterGrading.S_Ratio) - (rec != null ? rec.heartsEarned : 0);
                    heartLine += Loc.T($"   ·   S급까지 {need}개", $"   ·   {need} more for S");
                }
                if (gm.IsRetry)
                    heartLine += gm.LastImproved ? Loc.T("   ·   기록 갱신!", "   ·   New record!") : Loc.T("   ·   이전 기록 유지", "   ·   Previous record kept");
                // 6차: 미션 별 3개 + 조건 텍스트
                var prof = gm.Profile;
                string stars = "";
                for (int b = 0; b < 3; b++) stars += MissionTable.Has(prof, gm.Save.chapter, b) ? "★" : "☆";
                string m1 = MissionTable.Get(gm.Save.chapter, 0).Text, m2 = MissionTable.Get(gm.Save.chapter, 1).Text;
                string starLine = $"{stars}  {Loc.T("클리어", "Clear")} · {m1} · {m2}";
                if (gm.LastStarsGained > 0) starLine += Loc.T($"   (+{gm.LastStarsGained}★, 총 {prof.StarsTotal}/60)", $"   (+{gm.LastStarsGained}★, total {prof.StarsTotal}/60)");
                if (gm.LastRecord) starLine += Loc.T("   · 개인 기록", "   · Personal best");
                _stageLabel.text = $"{ChapterLocation.Get(gm.Save.chapter).Name}\n{heartLine}\n{starLine}";
                continueLabel = gm.IsRetry ? Loc.T("타임라인으로", "To timeline") : gm.Save.chapter >= Timeline.Chapters ? Loc.T("송전탑으로", "To the tower") : Loc.T("육성으로", "Back home");
            }

            if (_continueBtn != null)
            {
                var label = _continueBtn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = continueLabel;
            }

            // 9차: 등급 배지(큰 글자 원) — S 금, A 코랄, 그 외 하늘색
            if (_gradeBadge != null && GameManager.Active)
            {
                var g = GameManager.I.LastGrade;
                _gradeText.text = ChapterGrading.GradeLabel(g);
                _gradeBadge.color = g == ChapterGrade.S ? new Color(1f, 0.80f, 0.25f) : g == ChapterGrade.A ? new Color(1f, 0.44f, 0.57f) : new Color(0.45f, 0.68f, 0.90f);
                _gradeBadge.gameObject.SetActive(true);
            }
            else if (_gradeBadge != null) _gradeBadge.gameObject.SetActive(false);

            HideRunHud(true);
            _root.SetActive(true);

            if (_settle != null)
                StopCoroutine(_settle);
            _settle = StartCoroutine(Settle(stage));
        }

        // 9차: 정산 카드가 뜰 때 런 HUD(체력·점수·여정 바)를 숨긴다 — 겹쳐 보이던 것 정리.
        private readonly System.Collections.Generic.List<Canvas> _hiddenHud = new System.Collections.Generic.List<Canvas>();
        private void HideRunHud(bool hide)
        {
            if (hide)
            {
                _hiddenHud.Clear();
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (c == null || c == _canvas || !c.enabled || c.sortingOrder >= 200) continue;
                    c.enabled = false; _hiddenHud.Add(c);
                }
            }
            else
            {
                foreach (var c in _hiddenHud) if (c != null) c.enabled = true;
                _hiddenHud.Clear();
            }
        }

        public void ShowFinal(StageDef stage, Action onContinue, Action onRetry)
        {
            Show(stage, true, onContinue, onRetry);
            _title.text = "ARRIVAL";
            _stageLabel.text = Loc.T("S20  송전탑", "S20  The Tower");
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
            HideRunHud(false);
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
            int jellies = stats != null ? stats.Jellies : 0;
            int potions = stats != null ? stats.Potions : 0;
            int hearts = stats != null ? stats.Hearts : 0;
            int stars = stats != null ? stats.Stars : 0;

            foreach (var c in _chips) c.SetActive(false);
            _lineTotal.text = "";
            _lineHeld.text = "";
            _lineCombo.text = "";
            if (_shopHost != null)
                _shopHost.SetActive(false);
            SetButtons(false);

            // 제목이 '쾅' 들어온다
            yield return PunchIn(_banner, 0.35f);
            yield return Wait(0.15f);

            // 22차-5: 먹은 아이템을 ×N 칩으로 하나씩(게임 화면 위에서)
            int i = 0;
            yield return Chip(i++, "Coin_Gold", Loc.T("코인", "Coins"), coinCount, coinValue);
            if (jellies > 0) yield return Chip(i++, "Jelly_Lemon", Loc.T("말랑이", "Jellies"), jellies, 0);
            if (hearts > 0) yield return Chip(i++, "Heart", Loc.T("하트", "Hearts"), hearts, 0);
            if (potions > 0) yield return Chip(i++, "Potion", Loc.T("물약", "Potions"), potions, 0);
            if (stars > 0) yield return Chip(i++, "Star", Loc.T("보너스 별", "Bonus stars"), stars, 0);
            if (nmCount > 0) yield return Chip(i++, null, Loc.T("니어미스", "Near miss"), nmCount, nmValue);

            if (bestCombo > 1)
                _lineCombo.text = Loc.T($"최고 콤보 ×{bestCombo}", $"Best combo ×{bestCombo}");
            else if (flawless)
                _lineCombo.text = Loc.T("무피해 클리어!", "No damage!");
            yield return Wait(0.2f);

            int total = coinValue + nmValue;
            yield return CountUp(_lineTotal, Loc.T("합계", "Total"), total, 0.45f);
            _lineHeld.text = Row(Loc.T("보유", "Wallet"), "", wallet != null ? wallet.TotalCoins : 0);
            yield return Wait(0.15f);

            UpdateJourney(stage, seconds);
            yield return Wait(0.2f);

            // 22차-5: 인게임 정산에선 업그레이드 상점을 띄우지 않는다(주인공 포즈를 가린다; 펫 상점이 대신).
            SetButtons(true);
            _settle = null;
        }

        private readonly System.Collections.Generic.List<GameObject> _chips = new System.Collections.Generic.List<GameObject>();
        private RectTransform _chipHost; private RectTransform _banner;

        /// 아이콘 + 이름 + ×N (+ 코인값) 칩. 왼쪽에서 톡 튀어 들어온다.
        private IEnumerator Chip(int index, string iconKey, string label, int count, int value)
        {
            while (_chips.Count <= index)
            {
                var go = new GameObject("Chip" + _chips.Count, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_chipHost, false);
                var img = go.GetComponent<Image>();
                img.sprite = CoastUiArt.RoundedRect(14); img.type = Image.Type.Sliced; img.color = new Color(0.06f, 0.05f, 0.12f, 0.62f); img.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(330f, 58f);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(go.transform, false);
                var irt = icon.GetComponent<RectTransform>(); irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(34f, 0f); irt.sizeDelta = new Vector2(46f, 46f);
                icon.GetComponent<Image>().preserveAspect = true; icon.GetComponent<Image>().raycastTarget = false;
                var t = CoastHudLayout.MakeText(rt, "T", "", 22, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(68f, 0f), new Vector2(-12f, 0f));
                t.color = Color.white; t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                _chips.Add(go);
            }
            var chip = _chips[index];
            chip.SetActive(true);
            var crt = chip.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0f, -index * 66f);
            var iconImg = chip.transform.Find("Icon").GetComponent<Image>();
            var tex = iconKey != null ? PaintedProp.Load(iconKey) : null;
            iconImg.enabled = tex != null;
            if (tex != null) iconImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            var txt = chip.transform.Find("T").GetComponent<Text>();
            txt.text = value > 0 ? $"{label}  ×{count}   <color=#FFD54A>+{value:N0}</color>" : $"{label}  ×{count}";
            txt.supportRichText = true;
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            float t0 = 0f; const float dur = 0.22f;
            while (t0 < dur)
            {
                t0 += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t0 / dur);
                float e = 1f - (1f - u) * (1f - u);
                crt.anchoredPosition = new Vector2(Mathf.Lerp(-200f, 0f, e), -index * 66f);
                crt.localScale = Vector3.one * (u < 0.7f ? Mathf.Lerp(0.8f, 1.08f, u / 0.7f) : Mathf.Lerp(1.08f, 1f, (u - 0.7f) / 0.3f));
                yield return null;
            }
            crt.localScale = Vector3.one;
        }

        private IEnumerator PunchIn(RectTransform rt, float dur)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / dur);
                float sc = u < 0.6f ? Mathf.Lerp(1.8f, 0.94f, u / 0.6f) : Mathf.Lerp(0.94f, 1f, (u - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * sc;
                yield return null;
            }
            rt.localScale = Vector3.one;
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
                Loc.T($"송전탑까지 {remainingKm:0.0} km   ·   {ClockAt(stage.lightingTEnd)}", $"{remainingKm:0.0} km to the tower   ·   {ClockAt(stage.lightingTEnd)}") +
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
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            // 22차-5: 팝업 카드가 아니라 게임 화면 위에 얹히는 정산 — 배경 딤 없음(주인공 골인 포즈가 보인다).
            _root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            _root.GetComponent<Image>().raycastTarget = false;
            _card = rt;

            // 상단 배너: STAGE CLEAR! + 등급 배지
            _banner = new GameObject("Banner", typeof(RectTransform)).GetComponent<RectTransform>();
            _banner.SetParent(_card, false);
            _banner.anchorMin = new Vector2(0f, 1f); _banner.anchorMax = new Vector2(1f, 1f); _banner.pivot = new Vector2(0.5f, 1f);
            _banner.anchoredPosition = new Vector2(0f, -70f); _banner.sizeDelta = new Vector2(0f, 150f);
            var band = CoastUiArt.Panel(_banner, "Band", new Color(0.05f, 0.04f, 0.12f, 0.45f), 0);
            var brt = band.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero; band.raycastTarget = false;
            _title = CoastHudLayout.MakeText(_banner, "Title", "STAGE CLEAR!", 46, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -70f), new Vector2(-20f, -6f));
            _title.color = new Color(1f, 0.93f, 0.55f); _title.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_title, new Color(0.25f, 0.08f, 0.05f, 0.95f), 3f);
            _stageLabel = CoastHudLayout.MakeText(_banner, "Stage", "", 15, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -150f), new Vector2(-16f, -72f));
            _stageLabel.resizeTextForBestFit = true; _stageLabel.resizeTextMinSize = 12; _stageLabel.resizeTextMaxSize = 17;
            _stageLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _stageLabel.color = Color.white; CoastUiArt.OutlineText(_stageLabel, new Color(0f, 0f, 0f, 0.7f), 1.5f);

            _gradeBadge = CoastUiArt.Panel(_banner, "Grade", new Color(1f, 0.80f, 0.25f), 40);
            var grt = _gradeBadge.rectTransform; grt.anchorMin = grt.anchorMax = new Vector2(1f, 1f); grt.pivot = new Vector2(1f, 1f);
            grt.anchoredPosition = new Vector2(-14f, 6f); grt.sizeDelta = new Vector2(80f, 80f);
            _gradeBadge.raycastTarget = false;
            var ring = CoastUiArt.Panel(_gradeBadge.transform, "Ring", new Color(1f, 1f, 1f, 0.55f), 36);
            var rrt = ring.rectTransform; rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = new Vector2(5f, 5f); rrt.offsetMax = new Vector2(-5f, -5f);
            ring.raycastTarget = false;
            _gradeText = CoastHudLayout.MakeText(_gradeBadge.rectTransform, "T", "S", 40, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 2f));
            _gradeText.color = Color.white; _gradeText.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_gradeText, new Color(0.3f, 0.15f, 0.05f, 0.6f), 1.6f);

            // 왼쪽 위: 아이템 칩 열(정산). 화면 왼쪽 30%~, 주인공은 가운데 아래에 보인다.
            _chipHost = new GameObject("Chips", typeof(RectTransform)).GetComponent<RectTransform>();
            _chipHost.SetParent(_card, false);
            _chipHost.anchorMin = new Vector2(0f, 1f); _chipHost.anchorMax = new Vector2(0f, 1f); _chipHost.pivot = new Vector2(0f, 1f);
            _chipHost.anchoredPosition = new Vector2(18f, -240f); _chipHost.sizeDelta = new Vector2(340f, 400f);

            // 아래쪽: 콤보/합계/보유/여정 + 버튼(반투명 띠 위)
            var foot = CoastUiArt.Panel(_card, "Foot", new Color(0.05f, 0.04f, 0.12f, 0.55f), 22);
            var frt = foot.rectTransform; frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 0f); frt.pivot = new Vector2(0.5f, 0f);
            frt.anchoredPosition = new Vector2(0f, 14f); frt.sizeDelta = new Vector2(-24f, 300f); foot.raycastTarget = false;
            _lineCombo = FootLabel(frt, "Combo", 18, -12f, 30f); _lineCombo.color = new Color(1f, 0.72f, 0.45f);
            _lineTotal = FootLabel(frt, "Total", 30, -44f, 46f); _lineTotal.color = new Color(1f, 0.93f, 0.55f); _lineTotal.fontStyle = FontStyle.Bold;
            _lineHeld = FootLabel(frt, "Held", 15, -92f, 26f); _lineHeld.color = new Color(1f, 1f, 1f, 0.8f);
            _lineCoins = FootLabel(frt, "Coins", 1, -200f, 1f); _lineNearMiss = FootLabel(frt, "NearMiss", 1, -200f, 1f);   // (칩으로 대체, 자리만)

            BuildJourneyBar(frt);

            _shopHost = new GameObject("UpgradeHost", typeof(RectTransform));
            _shopHost.transform.SetParent(_card, false);
            var sht = _shopHost.GetComponent<RectTransform>();
            sht.anchorMin = new Vector2(0.06f, 0.36f);
            sht.anchorMax = new Vector2(0.94f, 0.52f);
            sht.offsetMin = Vector2.zero;
            sht.offsetMax = Vector2.zero;

            _continueBtn = MakeButton(frt, "Continue", new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(520f, 60f),
                Loc.T("다음 스테이지", "Next stage"), new Color(1f, 0.44f, 0.57f), () => _onContinue?.Invoke());
            _retryBtn = MakeButton(frt, "Retry", new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(520f, 56f),
                Loc.T("다시 달리기", "Run again"), new Color(0.62f, 0.60f, 0.66f), () => _onRetry?.Invoke());
        }

        private Text FootLabel(RectTransform host, string name, int size, float y, float h)
        {
            var t = CoastHudLayout.MakeText(host, name, "", size, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y - h), new Vector2(-24f, y));
            t.color = Color.white; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            return t;
        }

        private RectTransform _card; private Image _gradeBadge; private Text _gradeText;

        private void BuildJourneyBar(RectTransform host)
        {
            var track = CoastUiArt.CutePill(host, "JourneyTrack", new Color(0.86f, 0.80f, 0.68f, 1f), 8, 2);
            var trt = track.rectTransform;
            trt.anchorMin = new Vector2(0.1f, 1f); trt.anchorMax = new Vector2(0.9f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -124f); trt.sizeDelta = new Vector2(0f, 14f);
            track.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            _journeyFill = fill.GetComponent<Image>();
            _journeyFill.sprite = CoastUiArt.RoundedRect(6); _journeyFill.type = Image.Type.Sliced;
            _journeyFill.color = new Color(1f, 0.55f, 0.28f, 1f);
            _journeyFill.raycastTarget = false;
            var frt = _journeyFill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(0f, 1f);
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(0f, -3f);

            _journey = FootLabel(host, "Journey", 14, -142f, 24f);
            _journey.color = new Color(1f, 1f, 1f, 0.85f);
        }

        /// 카드 상단 기준 y(음수)·높이로 놓는 가운데 정렬 글자.
        private Text CardLabel(string name, string value, int size, float y, float h)
        {
            var t = CoastHudLayout.MakeText(_card, name, value, size, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, y - h), new Vector2(-28f, y));
            return t;
        }

        private static Button MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
            string label, Color color, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 18, 4);
            var rt = pill.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            pill.raycastTarget = true;
            var btn = pill.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = CoastHudLayout.MakeText(pill.transform, "Label", label, 21, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = Color.white; t.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return btn;
        }
    }
}
