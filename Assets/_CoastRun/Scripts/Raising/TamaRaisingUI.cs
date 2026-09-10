using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 45차: 스토리 모드 육성 화면 — **다마고치식 터치 육성**(RAISING_TAMAGOTCHI_v3 설계, 기존 RaisingUI 스케줄표 대체).
    ///   화면 한 장: Kling 마당 배경(UI_Tama_Yard) 위에 하늘이 스탠딩 → 만지면 반응(탭=웃음·콩, 문지르기=쓰다듬기 → 스트레스 ↓),
    ///   행동 3개(밥·놀기·알바, Kling 아이콘) = 기존 ScheduleTable 을 그대로 판정(Rest/SelfDev/Job 카드 자동 선택 → GameManager.ResolvePhase),
    ///   행동 3번 = 한 주 → AdvanceWeek → 챕터 마감 주면 오프닝 컷씬 → 체력 게이트 → 러닝(기존 흐름 그대로).
    ///   「자동」 토글이면 1.2초마다 규칙으로 스스로 행동(기운<30 밥 / 돈<100 알바 / 그 외 놀기·밥 번갈아).
    /// 기존 RaisingUI.cs 는 파일로 남겨 두되 RaisingSceneDriver 가 이 클래스를 쓴다.
    public class TamaRaisingUI : MonoBehaviour
    {
        private GameManager _gm;
        private SaveData Save => _gm != null ? _gm.Save : null;
        private Canvas _canvas;
        private RectTransform _root;
        private Image _girl;
        private RectTransform _girlRt;
        private Text _bubble, _weekLabel, _moneyLabel, _gateLabel, _autoLabel, _actionsLeft;
        private Image _bubbleBg, _staminaFill, _energyFill;
        private Text _staminaTxt, _energyTxt;
        private readonly Button[] _actBtn = new Button[3];
        private readonly Text[] _actDots = new Text[3];
        private bool _busy, _auto;
        private float _hop, _autoTimer, _bubbleUntil;
        private int _rubBudget = 10;     // 이번 주 쓰다듬기로 내릴 수 있는 스트레스
        private float _rubDist;
        private int _lastAutoPick;
        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        private static readonly Color Pink = new Color(0.93f, 0.22f, 0.52f);

        public void Bind(GameManager gm)
        {
            _gm = gm;
            Build();
            Refresh();
            ShowBubble(Loc.T("오늘도 힘내자!", "Let's do our best today!"), 2.5f);
            _gm.OnSaveChanged -= OnSaveChanged; _gm.OnSaveChanged += OnSaveChanged;
        }

        private void OnDestroy() { if (_gm != null) _gm.OnSaveChanged -= OnSaveChanged; }
        private void OnSaveChanged(SaveData s) { if (this != null) Refresh(); }

        /// RaisingSceneDriver 호환: 타임라인 대신 현재 챕터 안내 말풍선.
        public void OpenTimeline()
        {
            if (Save == null) return;
            ShowBubble(Loc.T($"챕터 {Save.chapter} · {ChapterLocation.Get(Save.chapter).Name}", $"Chapter {Save.chapter} · {ChapterLocation.Get(Save.chapter).Name}"), 3f);
        }

        /// RaisingSceneDriver 호환: 돌발 이벤트는 말풍선 + 토스트로.
        public void ShowEvent(RandomEventResult ev)
        {
            string body = ev.Body;
            ShowBubble(body, 4f);
            string d = "";
            if (ev.dMoney != 0) d += $" 돈 {ev.dMoney:+#;-#}G";
            if (ev.dStamina != 0) d += $" 체력 {ev.dStamina:+#;-#}";
            if (ev.dStress != 0) d += $" 스트레스 {ev.dStress:+#;-#}";
            if (ev.dHearts != 0) d += $" 하트 {ev.dHearts:+#;-#}";
            if (d.Length > 0) CoastToast.Show(Loc.T("돌발 ·", "Event ·") + d);
            Refresh();
        }

        // ── 빌드 ────────────────────────────────────────────────────────
        private void Build()
        {
            _canvas = CoastUiCanvas.Create("TamaRaisingCanvas", 100);
            _root = CoastUiCanvas.Root(_canvas);
            float pad = CoastUiCanvas.HudPad;

            // 배경(Kling 마당) — 인셋 밖까지 꽉
            var bgTex = ArtAssets.LoadTexture("UI_Tama_Yard");
            var bg = CoastHudLayout.MakeImage(_root, "Yard", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), bgTex != null ? Color.white : new Color(0.80f, 0.90f, 0.75f));
            if (bgTex != null) { bg.sprite = CoastUiArt.AsSprite(bgTex); bg.preserveAspect = false; }
            bg.raycastTarget = true;
            // 바닥 쪽 살짝 어둡게(카드 가독)
            var shade = CoastHudLayout.MakeImage(_root, "Shade", new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(-pad, -pad), new Vector2(pad, 0f), new Color(0.05f, 0.08f, 0.16f, 0.30f));
            shade.raycastTarget = false;

            // ── 상단 HUD: 주차·계절 / 챕터 / 돈·하트 / 홈 ──
            var wk = CoastUiArt.CutePill(_root, "Week", new Color(0.10f, 0.13f, 0.30f, 0.92f), 18, 3);
            Anchor(wk.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(300f, 60f));
            _weekLabel = CoastHudLayout.MakeText(wk.rectTransform, "T", "", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-10f, 0f));
            _weekLabel.color = Color.white; CoastUiArt.OutlineText(_weekLabel, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            var money = CoastUiArt.CutePill(_root, "Money", new Color(0.10f, 0.13f, 0.30f, 0.92f), 18, 3);
            Anchor(money.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -6f), new Vector2(270f, 60f));
            _moneyLabel = CoastHudLayout.MakeText(money.rectTransform, "T", "", 20, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-18f, 0f));
            _moneyLabel.color = new Color(1f, 0.90f, 0.45f); CoastUiArt.OutlineText(_moneyLabel, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            var home = CoastUiArt.GlossyPill(_root, "Home", new Color(0.25f, 0.55f, 0.95f), 18, 6);
            Anchor(home.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(64f, 60f)); home.raycastTarget = true;
            var homeIcon = CoastUiArt.Art("Icon_Home");
            if (homeIcon != null)
            {
                var hi = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                hi.transform.SetParent(home.transform, false); hi.sprite = homeIcon; hi.preserveAspect = true; hi.raycastTarget = false;
                Anchor(hi.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            }
            var hb = home.gameObject.AddComponent<Button>(); hb.transition = Selectable.Transition.None;
            hb.onClick.AddListener(() => { if (_busy) return; _auto = false; RefreshAuto(); _gm.Persist(); _gm.ToTitle(); });

            // ── 무대: 하늘이 + 말풍선 ──
            var girlGo = new GameObject("Girl", typeof(RectTransform), typeof(Image), typeof(TouchRelay));
            girlGo.transform.SetParent(_root, false);
            _girl = girlGo.GetComponent<Image>(); _girl.preserveAspect = true; _girl.raycastTarget = true;
            _girlRt = girlGo.GetComponent<RectTransform>();
            Anchor(_girlRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 440f), new Vector2(420f, 520f));
            _girlRt.pivot = new Vector2(0.5f, 0f);
            var relay = girlGo.GetComponent<TouchRelay>(); relay.ui = this;
            _bubbleBg = CoastUiArt.CutePill(_root, "Bubble", Color.white, 18, 3);
            Anchor(_bubbleBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 985f), new Vector2(460f, 56f)); _bubbleBg.raycastTarget = false;
            _bubble = CoastHudLayout.MakeText(_bubbleBg.rectTransform, "T", "", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            _bubble.color = Navy; _bubble.resizeTextForBestFit = true; _bubble.resizeTextMinSize = 12; _bubble.resizeTextMaxSize = CoastHudLayout.Scaled(18); _bubble.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bubbleBg.gameObject.SetActive(false);

            // ── 게이지 2개: 체력(게이트) · 기운(100−스트레스) ──
            _staminaFill = Gauge(new Vector2(0f, 0f), new Vector2(6f, 372f), new Vector2(322f, 54f), Loc.T("체력", "Stamina"), new Color(0.95f, 0.35f, 0.40f), out _staminaTxt);
            _energyFill = Gauge(new Vector2(1f, 0f), new Vector2(-6f, 372f), new Vector2(322f, 54f), Loc.T("기운", "Energy"), new Color(0.35f, 0.75f, 0.95f), out _energyTxt);
            _gateLabel = CoastHudLayout.MakeText(_root, "Gate", "", 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 340f), new Vector2(0f, 368f));
            _gateLabel.color = new Color(1f, 0.95f, 0.80f); CoastUiArt.OutlineText(_gateLabel, new Color(0f, 0f, 0f, 0.6f), 1.2f);

            // ── 행동 3개 ──
            string[] keys = { "UI_Tama_Feed", "UI_Tama_Play", "UI_Tama_Work" };
            string[] names = { Loc.T("밥", "Feed"), Loc.T("놀기", "Play"), Loc.T("알바", "Work") };
            string[] subs = { Loc.T("기운 ↑ 체력 +", "Energy ↑ Stamina +"), Loc.T("순발력·매력 ↑", "Agility · Charm ↑"), Loc.T("돈 ↑ 기운 ↓", "Money ↑ Energy ↓") };
            Color[] fills = { new Color(1f, 0.80f, 0.35f), new Color(0.55f, 0.85f, 1f), new Color(0.75f, 0.65f, 0.95f) };
            float cw = 208f, gap = 12f, x0 = (664f - (3 * cw + 2 * gap)) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var card = CoastUiArt.GlossyPill(_root, "Act" + i, fills[i], 24, 10);
                var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0f, 0f); crt.pivot = new Vector2(0f, 0f);
                crt.anchoredPosition = new Vector2(x0 + i * (cw + gap), 200f); crt.sizeDelta = new Vector2(cw, 128f); card.raycastTarget = true;
                var icon = CoastUiArt.Art(keys[i]);
                if (icon != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(crt, false); im.sprite = icon; im.preserveAspect = true; im.raycastTarget = false;
                    Anchor(im.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(78f, 78f));
                }
                var n = CoastHudLayout.MakeText(crt, "N", names[i], 22, TextAnchor.MiddleLeft, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(96f, 0f), new Vector2(-6f, -10f));
                n.color = Navy;
                var s2 = CoastHudLayout.MakeText(crt, "S", subs[i], 11, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(96f, 6f), new Vector2(-4f, -2f));
                s2.color = new Color(0.30f, 0.28f, 0.45f); s2.horizontalOverflow = HorizontalWrapMode.Wrap;
                int idx = i;
                _actBtn[i] = card.gameObject.AddComponent<Button>(); _actBtn[i].transition = Selectable.Transition.None;
                _actBtn[i].onClick.AddListener(() => DoAction(idx));
            }

            // ── 아래: 자동 토글 + 이번 주 진행 ──
            var auto = CoastUiArt.GlossyPill(_root, "Auto", new Color(0.55f, 0.55f, 0.62f), 22, 8);
            Anchor(auto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 60f), new Vector2(190f, 110f)); auto.raycastTarget = true;
            _autoLabel = CoastHudLayout.MakeText(auto.rectTransform, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 2f));
            _autoLabel.color = Color.white; CoastUiArt.OutlineText(_autoLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var ab = auto.gameObject.AddComponent<Button>(); ab.transition = Selectable.Transition.None;
            ab.onClick.AddListener(() => { _auto = !_auto; _autoTimer = 0f; RefreshAuto(); ShowBubble(_auto ? Loc.T("내가 알아서 할게!", "I'll take care of myself!") : Loc.T("같이 하자.", "Let's do it together."), 2f); });
            var prog = CoastUiArt.GlossyPill(_root, "Prog", Pink, 30, 12);
            Anchor(prog.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 60f), new Vector2(446f, 110f)); prog.raycastTarget = false;
            _actionsLeft = CoastHudLayout.MakeText(prog.rectTransform, "T", "", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, 2f));
            _actionsLeft.color = Color.white; CoastUiArt.OutlineText(_actionsLeft, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            _actionsLeft.resizeTextForBestFit = true; _actionsLeft.resizeTextMinSize = 12; _actionsLeft.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            RefreshAuto();
        }

        private Image Gauge(Vector2 anchor, Vector2 pos, Vector2 size, string label, Color color, out Text valueTxt)
        {
            var track = CoastUiArt.CutePill(_root, "Gauge_" + label, new Color(0.10f, 0.13f, 0.30f, 0.92f), 18, 3);
            Anchor(track.rectTransform, anchor, anchor, pos, size);
            var fillBg = CoastUiArt.Panel(track.transform, "Bg", new Color(0f, 0f, 0f, 0.35f), 12);
            Anchor(fillBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            fillBg.rectTransform.offsetMin = new Vector2(86f, 12f); fillBg.rectTransform.offsetMax = new Vector2(-12f, -12f); fillBg.raycastTarget = false;
            var fill = CoastUiArt.Panel(fillBg.transform, "Fill", color, 10);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f); fill.rectTransform.anchorMax = new Vector2(0.5f, 1f); fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero; fill.raycastTarget = false;
            var l = CoastHudLayout.MakeText(track.rectTransform, "L", label, 16, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 0f), new Vector2(86f, 0f));
            l.color = Color.white;
            valueTxt = CoastHudLayout.MakeText(fillBg.rectTransform, "V", "", 13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            valueTxt.color = Color.white; CoastUiArt.OutlineText(valueTxt, new Color(0f, 0f, 0f, 0.5f), 1.2f);
            return fill;
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(aMin.x, aMin.y);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        // ── 표시 갱신 ───────────────────────────────────────────────────
        public void Refresh()
        {
            if (Save == null) return;
            var s = Save.stats;
            var season = Timeline.SeasonOf(Save.week);
            string seasonKo = season == SeasonKind.Spring ? "봄" : season == SeasonKind.Summer ? "여름" : season == SeasonKind.Autumn ? "가을" : "겨울";
            var rec = Save.CurrentChapter;
            int weeksLeft = rec != null ? Mathf.Max(0, rec.weekEnd - Save.week) : 0;
            _weekLabel.text = Loc.T($"{Save.week}주차 · {seasonKo} · CH{Save.chapter}", $"Week {Save.week} · {season} · CH{Save.chapter}");
            _moneyLabel.text = $"{s.money}G  ♥{Save.chapterHearts}";
            int need = StoryGate.Required(Save);
            _staminaFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.stamina / (float)Mathf.Max(need, 1) * 0.5f + (s.stamina >= need ? 0.5f * Mathf.Clamp01((s.stamina - need) / (float)Mathf.Max(1, PlayerStats.StatMax - need)) : 0f)), 1f);
            _staminaFill.color = s.stamina >= need ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.95f, 0.35f, 0.40f);
            _staminaTxt.text = $"{s.stamina} / {need}";
            int energy = Mathf.Clamp(100 - s.stress, 0, 100);
            _energyFill.rectTransform.anchorMax = new Vector2(energy / 100f, 1f);
            _energyFill.color = energy < 30 ? new Color(0.95f, 0.55f, 0.25f) : new Color(0.35f, 0.75f, 0.95f);
            _energyTxt.text = $"{energy}";
            _gateLabel.text = s.stamina >= need
                ? Loc.T($"챕터 {Save.chapter} 송전탑까지 {weeksLeft}주 · 체력 게이트 통과!", $"{weeksLeft} weeks to the tower · gate OK!")
                : Loc.T($"챕터 {Save.chapter} 송전탑까지 {weeksLeft}주 · 체력 {need} 필요", $"{weeksLeft} weeks to the tower · need stamina {need}");
            int done = Save.phaseIndex;
            _actionsLeft.text = weeksLeft == 0 && done >= Timeline.PhasesPerWeek - 1
                ? Loc.T($"이번 행동 뒤 → CH{Save.chapter} 송전탑으로!", $"After this → CH{Save.chapter} tower!")
                : Loc.T($"이번 주 행동 {done}/{Timeline.PhasesPerWeek}  " + Dots(done), $"This week {done}/{Timeline.PhasesPerWeek}  " + Dots(done));
            RefreshGirl(null);
        }

        private static string Dots(int done) { string d = ""; for (int i = 0; i < Timeline.PhasesPerWeek; i++) d += i < done ? "●" : "○"; return d; }

        private void RefreshAuto()
        {
            _autoLabel.text = _auto ? Loc.T("자동 ON", "Auto ON") : Loc.T("자동 OFF", "Auto OFF");
            var img = _autoLabel.transform.parent.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(_auto ? new Color(0.35f, 0.80f, 0.45f) : new Color(0.55f, 0.55f, 0.62f), Color.black, 0.62f);
            var fill = _autoLabel.transform.parent.Find("Fill")?.GetComponent<Image>();
            if (fill != null) fill.color = _auto ? new Color(0.35f, 0.80f, 0.45f) : new Color(0.55f, 0.55f, 0.62f);
        }

        private void RefreshGirl(string moodKey)
        {
            if (Save == null) return;
            var st = Save.stats;
            float ratio = st.stamina > 0 ? st.stress / (float)st.stamina : 2f;
            string key = moodKey ?? (ratio < 0.4f ? "Happy" : ratio < 0.7f ? "Normal" : "Tired");
            string sfx = SeasonLook.Suffix(Timeline.SeasonOf(Save.week));
            var tex = ArtAssets.LoadTexture("Raise_Girl_" + key + "_" + sfx) ?? ArtAssets.LoadTexture("Raise_Girl_" + key)
                      ?? ArtAssets.LoadTexture("Raise_Girl_Normal_" + sfx) ?? ArtAssets.LoadTexture("Raise_Girl_Normal");
            if (tex != null) { _girl.sprite = CoastUiArt.AsSprite(tex); _girl.enabled = true; }
        }

        private void ShowBubble(string text, float seconds)
        {
            _bubble.text = text; _bubbleBg.gameObject.SetActive(true); _bubbleUntil = Time.unscaledTime + seconds;
        }

        // ── 터치(다마고치 손맛) ──────────────────────────────────────────
        public void OnGirlTap()
        {
            if (Save == null) return;
            _hop = 1f;
            RefreshGirl("Happy");
            string[] lines = { "헤헤.", "왜?", "오늘 날씨 좋다!", "송전탑 보여?", "간지러워~", "같이 달릴래?" };
            ShowBubble(Loc.T(lines[UnityEngine.Random.Range(0, lines.Length)], "Hehe."), 1.6f);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.35f);
            SpawnHeart(1);
        }

        public void OnGirlRub(float px)
        {
            if (Save == null) return;
            _rubDist += px;
            if (_rubDist < 90f) return;
            _rubDist = 0f;
            _girlRt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-3f, 3f));
            if (_rubBudget > 0 && Save.stats.stress > 0)
            {
                _rubBudget--;
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 1);
                RefreshGirl("Happy");
                ShowBubble(Loc.T("기분 좋아~", "That feels nice~"), 1.2f);
                SpawnHeart(2);
                _gm.Persist();
            }
            else ShowBubble(Loc.T("이제 됐어, 고마워!", "That's enough, thanks!"), 1.2f);
        }

        private void SpawnHeart(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var h = CoastHudLayout.MakeText(_root, "Heart", "♥", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                h.color = new Color(1f, 0.45f, 0.6f);
                h.rectTransform.sizeDelta = new Vector2(40f, 40f);
                h.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-120f, 120f), 700f + UnityEngine.Random.Range(0f, 120f));
                StartCoroutine(FloatHeart(h.rectTransform));
            }
        }

        private IEnumerator FloatHeart(RectTransform rt)
        {
            float t = 0f; var p0 = rt.anchoredPosition; var txt = rt.GetComponent<Text>();
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime; float u = t / 0.9f;
                rt.anchoredPosition = p0 + new Vector2(Mathf.Sin(u * 8f) * 12f, u * 110f);
                if (txt != null) txt.color = new Color(1f, 0.45f, 0.6f, 1f - u);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private void Update()
        {
            if (_girlRt != null)
            {
                _hop = Mathf.MoveTowards(_hop, 0f, Time.unscaledDeltaTime * 3f);
                float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.012f;
                _girlRt.localScale = new Vector3(breathe, breathe + _hop * 0.10f, 1f);
                _girlRt.localRotation = Quaternion.Slerp(_girlRt.localRotation, Quaternion.identity, Time.unscaledDeltaTime * 6f);
            }
            if (_bubbleBg != null && _bubbleBg.gameObject.activeSelf && Time.unscaledTime > _bubbleUntil) _bubbleBg.gameObject.SetActive(false);
            if (_auto && !_busy && Save != null)
            {
                _autoTimer += Time.unscaledDeltaTime;
                if (_autoTimer >= 1.2f) { _autoTimer = 0f; DoAction(AutoPick()); }
            }
        }

        private int AutoPick()
        {
            var s = Save.stats;
            if (100 - s.stress < 30) return 0;
            if (s.money < 100) return 2;
            if (s.stamina < StoryGate.Required(Save) && 100 - s.stress >= 40) return 0;
            _lastAutoPick = _lastAutoPick == 1 ? 2 : 1;
            return _lastAutoPick;
        }

        // ── 행동 → 기존 스케줄 판정 ──────────────────────────────────────
        private void DoAction(int idx)
        {
            if (_busy || Save == null) return;
            var season = Timeline.SeasonOf(Save.week);
            ScheduleDef def = idx == 0 ? PickRest(season) : idx == 1 ? PickDev(season) : PickJob(season);
            if (def == null) { ShowBubble(Loc.T("지금은 할 게 없네…", "Nothing to do right now…"), 1.5f); return; }
            if (def.dMoney < 0 && Save.stats.money + def.dMoney < 0) { ShowBubble(Loc.T("돈이 모자라… 알바부터!", "Not enough money… work first!"), 1.8f); return; }
            StartCoroutine(ActionRoutine(idx, def));
        }

        private ScheduleDef PickRest(SeasonKind season)
        {
            var list = ScheduleTable.ByCategory(ScheduleCategory.Rest, season);
            return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : ScheduleTable.Get("rest_home");
        }
        private ScheduleDef PickDev(SeasonKind season)
        {
            var list = ScheduleTable.ByCategory(ScheduleCategory.SelfDev, season);
            var ok = new List<ScheduleDef>();
            foreach (var d in list) if (Save.stats.money + d.dMoney >= 0) ok.Add(d);
            if (ok.Count == 0) ok = list;
            return ok.Count > 0 ? ok[UnityEngine.Random.Range(0, ok.Count)] : null;
        }
        private ScheduleDef PickJob(SeasonKind season)
        {
            // 말썽·야간 계열은 제외(간소화), 조건(평판·스탯) 충족만
            var list = ScheduleTable.ByCategory(ScheduleCategory.Job, season);
            var ok = new List<ScheduleDef>();
            foreach (var d in list)
            {
                if (d.dTrouble > 0) continue;
                if (Save.stats.trust < d.condTrust) continue;
                if (Save.stats.stamina < d.condStamina || Save.stats.agility < d.condAgility) continue;
                ok.Add(d);
            }
            return ok.Count > 0 ? ok[UnityEngine.Random.Range(0, ok.Count)] : ScheduleTable.Get("job_cafe");
        }

        private IEnumerator ActionRoutine(int idx, ScheduleDef def)
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            int slot = Mathf.Clamp(Save.phaseIndex, 0, Timeline.PhasesPerWeek - 1);
            _gm.SetQueued(slot, def.id);
            // 나가는 연출: 말풍선 + 살짝 사라졌다 돌아오기
            string[] going = { Loc.T($"{def.Name} 다녀올게!", $"Off to {def.Name}!"), Loc.T($"{def.Name}!", $"{def.Name}!"), Loc.T($"{def.place}로 출발~", $"To {def.place}~") };
            ShowBubble(going[UnityEngine.Random.Range(0, going.Length)], 1.2f);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
            float t = 0f;
            while (t < 0.45f) { t += Time.unscaledDeltaTime; _girl.color = new Color(1f, 1f, 1f, 1f - t / 0.45f); yield return null; }
            var result = _gm.ResolvePhase(slot);
            if (idx == 0 && result.HasValue)
            {
                // 밥: v3 규칙 — 체력 +1 보너스(주당 상한은 게이트 자체가 낮아 필요 없음), 스트레스 −5 추가
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 5);
                _gm.Persist();
            }
            yield return new WaitForSecondsRealtime(0.25f);
            t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; _girl.color = new Color(1f, 1f, 1f, t / 0.35f); yield return null; }
            _girl.color = Color.white;
            if (result.HasValue)
            {
                var r = result.Value;
                RefreshGirl(r.outcome == Outcome.GreatSuccess ? "Happy" : r.outcome == Outcome.Fail ? "Tired" : null);
                string line = r.logLines != null && r.logLines.Length > 0 ? r.logLines[r.logLines.Length - 1] : "";
                string head = r.outcome == Outcome.GreatSuccess ? Loc.T("대성공! ", "Great! ") : r.outcome == Outcome.Fail ? Loc.T("으으… ", "Ugh… ") : "";
                int dM = r.after.money - r.before.money, dS = r.after.stamina - r.before.stamina, dSt = r.after.stress - r.before.stress;
                string delta = (dM != 0 ? $" {dM:+#;-#}G" : "") + (dS != 0 ? $" 체력{dS:+#;-#}" : "") + (dSt != 0 ? $" 기운{-dSt:+#;-#}" : "") + (r.heartsGained > 0 ? $" ♥+{r.heartsGained}" : "");
                ShowBubble(head + (string.IsNullOrEmpty(line) ? def.Name : line) + delta, 3.2f);
                if (r.outcome == Outcome.GreatSuccess) SpawnHeart(3);
            }
            Refresh();
            if (Save.phaseIndex >= Timeline.PhasesPerWeek)
            {
                yield return new WaitForSecondsRealtime(_auto ? 0.4f : 1.2f);
                yield return EndWeek();
            }
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
        }

        /// 한 주 끝(행동 3번) — 기존 RaisingUI.ExecuteWeek 의 주말 처리 그대로: 주간 감쇠 → 사이드 씬 → 컨디션 → 챕터 경계(오프닝 → 게이트 → 러닝).
        private IEnumerator EndWeek()
        {
            bool forced = _gm.AdvanceWeek();
            _rubBudget = 10;
            Refresh();
            ShowBubble(Loc.T($"{Save.week}주차 아침이야.", $"Week {Save.week} morning."), 1.5f);
            if (!string.IsNullOrEmpty(_gm.PendingSideScene))
            {
                string side = _gm.PendingSideScene; _gm.PendingSideScene = null;
                int lvl = side.EndsWith("_3") ? 3 : side.EndsWith("_2") ? 2 : 1;
                bool doneVn = false;
                ChapterVN.Play(side, () => doneVn = true);
                while (!doneVn) yield return null;
                Affinity.Reward(Save, lvl);
                _gm.Persist(); Refresh();
            }
            if (!string.IsNullOrEmpty(_gm.PendingWeekNote))
            {
                ShowBubble(_gm.PendingWeekNote, 3f);
                CoastToast.Show(_gm.PendingWeekNote);
                _gm.PendingWeekNote = null;
                Refresh();
                if (Save.forfeitPending) { _auto = false; RefreshAuto(); _gm.ForfeitChapter(); yield break; }
                yield return new WaitForSecondsRealtime(_auto ? 0.5f : 1.5f);
            }
            if (forced)
            {
                _auto = false; RefreshAuto();
                var rec = Save.CurrentChapter;
                bool firstTime = rec == null || rec.gateFails == 0;
                if (firstTime)
                {
                    ShowBubble(Loc.T("이번 주가 이 챕터의 마지막 주야.", "Last week of this chapter."), 2f);
                    yield return new WaitForSecondsRealtime(1.2f);
                    bool doneVn = false;
                    ChapterVN.HoldBlackOnNext = false;
                    ChapterVN.PlayChapterOpening(Save.chapter, () => doneVn = true);
                    while (!doneVn) yield return null;
                }
                if (StoryGate.Passes(Save))
                {
                    ShowBubble(Loc.T($"체력 {StoryGate.Stamina(Save)} / 필요 {StoryGate.Required(Save)} — 달릴 수 있어! 송전탑으로!", $"Stamina {StoryGate.Stamina(Save)} / need {StoryGate.Required(Save)} — to the tower!"), 3f);
                    yield return new WaitForSecondsRealtime(1.4f);
                    _gm.StartStoryRun();
                    yield break;
                }
                _gm.GateFail();
                Refresh();
                ShowBubble(StoryGate.FailText(Save), 4f);
                CoastToast.Show(Loc.T("아직 못 달려 — 한 주 더 키우자", "Not yet — one more week"));
                yield break;
            }
            var ev = _gm.RollRandomEvent();
            if (ev.HasValue) ShowEvent(ev.Value);
        }

        /// 하늘이 그림 위 터치: 짧게 = 탭, 움직이면 = 쓰다듬기.
        private class TouchRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
        {
            public TamaRaisingUI ui;
            private float _down; private float _moved;
            public void OnPointerDown(PointerEventData e) { _down = Time.unscaledTime; _moved = 0f; }
            public void OnDrag(PointerEventData e) { _moved += e.delta.magnitude; ui?.OnGirlRub(e.delta.magnitude); }
            public void OnPointerUp(PointerEventData e) { if (_moved < 12f && Time.unscaledTime - _down < 0.5f) ui?.OnGirlTap(); }
        }
    }
}
