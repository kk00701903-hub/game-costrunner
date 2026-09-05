using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 프린세스 메이커 오마주 육성 화면(런타임 빌드).
    ///   상단: 주차·계절 / 챕터 / 말랑이 하트 / 돈
    ///   좌:   이번 주 스케줄 3칸 + 실행
    ///   중앙: 캐릭터(표정 = 스트레스/체력)
    ///   우:   스탯창(체력·순발력·매력·스트레스 + 번아웃 마커)
    ///   하단: 알바 / 자기계발 / 휴식 / 스토리 탭 + 스케줄 카드
    ///   오버레이: 정산 로그, 돌발 이벤트, 상점, 타임라인, 확인창
    public partial class RaisingUI : MonoBehaviour
    {
        private static readonly Color Cream = new Color(0.99f, 0.97f, 0.92f);
        private static readonly Color Navy = new Color(0.16f, 0.20f, 0.34f);
        private static readonly Color Ink = new Color(0.22f, 0.20f, 0.26f);
        private static readonly Color Coral = new Color(1f, 0.52f, 0.48f);
        private static readonly Color Mint = new Color(0.45f, 0.82f, 0.74f);
        private static readonly Color Sky = new Color(0.42f, 0.70f, 0.95f);
        private static readonly Color Sun = new Color(1f, 0.80f, 0.30f);
        private static readonly Color Grape = new Color(0.72f, 0.55f, 0.92f);
        private static readonly Color Pink = new Color(1f, 0.62f, 0.75f);

        private GameManager _gm;
        private Canvas _canvas;
        private Canvas _overlayCanvas;
        private RectTransform _root;
        private RectTransform _overlay;

        private Image _bg;
        private Image _bgBand;
        private Text _weekLabel;
        private Text _chapterLabel;
        private Text _heartLabel;
        private Text _moneyLabel;
        private Text _modeLabel;

        private readonly Text[] _slotName = new Text[Timeline.PhasesPerWeek];
        private readonly Text[] _slotGlyph = new Text[Timeline.PhasesPerWeek];
        private readonly Image[] _slotFill = new Image[Timeline.PhasesPerWeek];
        private Button _runButton;
        private Text _runLabel;

        private Image _charImage;
        private Text _charFace;
        private Text _bubble;
        private RectTransform _charRoot;

        private readonly Dictionary<StatKind, Image> _statFill = new Dictionary<StatKind, Image>();
        private readonly Dictionary<StatKind, Text> _statValue = new Dictionary<StatKind, Text>();
        private RectTransform _burnoutMarker;
        private Text _burnoutHint;

        private ScheduleCategory _tab = ScheduleCategory.Job;
        private readonly List<Button> _tabButtons = new List<Button>();
        private RectTransform _cardRow;
        private int _selectedSlot = -1;

        private GameObject _logPanel;
        private Text _logTitle;
        private Text _logBody;
        private Text _logHint;
        private bool _tapped;
        private bool _busy;

        private GameObject _toast;
        private Text _toastText;
        /// 열린 모달의 기본 동작(확인/닫기) — 키보드(Return/Space)로도 닫을 수 있게.
        private Action _modalPrimary;
        private int _timelinePage;   // 에디터 키: 타임라인에서 1~9 = 챕터 (page×9 + n)

        public void Bind(GameManager gm)
        {
            _gm = gm;
            Build();
            Refresh();
            _gm.OnSaveChanged -= HandleSaveChanged;
            _gm.OnSaveChanged += HandleSaveChanged;
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnSaveChanged -= HandleSaveChanged;
        }

        private void HandleSaveChanged(SaveData _) => Refresh();

        private SaveData Save => _gm != null ? _gm.Save : null;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.GetKeyDown(KeyCode.Space))
                _tapped = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevKeys();
#endif
        }

        /// 에디터 검증용 키: Q/W/E/R 탭, 1~9 카드, Backspace 칸 비우기, Return 실행, S 상점, T 타임라인.
        private void DevKeys()
        {
            if (Save == null) return;
            if (Input.GetKeyDown(KeyCode.Q)) { _tab = ScheduleCategory.Job; RefreshCards(); }
            if (Input.GetKeyDown(KeyCode.W)) { _tab = ScheduleCategory.SelfDev; RefreshCards(); }
            if (Input.GetKeyDown(KeyCode.E)) { _tab = ScheduleCategory.Rest; RefreshCards(); }
            if (Input.GetKeyDown(KeyCode.R)) { _tab = ScheduleCategory.Story; RefreshCards(); }
            for (int n = 0; n < 9; n++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + n)) continue;
                if (_shopModal != null) { if (n < PetShop.ForSale.Length) ShopAct(PetShop.ForSale[n]); continue; }
                if (_timelineModal != null) { int ch = n + 1 + _timelinePage * 9; if (_gm.CanRetry(ch)) { Destroy(_timelineModal); _timelineModal = null; _modalPrimary = null; _gm.BeginRetry(ch); } continue; }
                var defs = ScheduleTable.ByCategory(_tab, Timeline.SeasonOf(Save.week));
                if (n < defs.Count) OnCardTapped(defs[n]);
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                for (int i = Timeline.PhasesPerWeek - 1; i >= Save.phaseIndex; i--)
                    if (!string.IsNullOrEmpty(Save.queuedSchedule[i])) { _gm.SetQueued(i, null); break; }
                RefreshSlots();
            }
            if (_modalPrimary != null && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                var act = _modalPrimary; _modalPrimary = null; act();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) && !_busy) OnRunPressed();
            if (Input.GetKeyDown(KeyCode.S) && !_busy) OpenShop();
            if (Input.GetKeyDown(KeyCode.T) && !_busy) OpenTimeline();
        }

        // ────────────────────────────────────────────────────────────────
        // Build
        // ────────────────────────────────────────────────────────────────

        private void Build()
        {
            _canvas = CoastUiCanvas.Create("RaisingCanvas", 100);
            _root = CoastUiCanvas.Root(_canvas);
            _overlayCanvas = CoastUiCanvas.Create("RaisingOverlay", 120);
            _overlay = CoastUiCanvas.Root(_overlayCanvas);

            BuildBackground();
            BuildTopBar();
            BuildCalendar();
            BuildCharacter();
            BuildStats();
            BuildBottom();
            BuildLogPanel();
            BuildToast();
        }

        private void BuildBackground()
        {
            _bg = CoastHudLayout.MakeImage(_root, "Background", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad),
                new Color(0.93f, 0.90f, 0.82f));
            _bg.transform.SetAsFirstSibling();
            // 계절 키아트(Resources/CoastRun/UI_Raising_<Season>)가 있으면 그림으로.
            _bgBand = CoastHudLayout.MakeImage(_root, "SkyBand", new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                new Vector2(-CoastUiCanvas.HudPad, 0f), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad),
                new Color(0.62f, 0.82f, 0.96f));
            _bgBand.transform.SetSiblingIndex(1);
        }

        private void BuildTopBar()
        {
            var week = CoastUiArt.CutePill(_root, "WeekPill", Navy, 18, 4);
            Place(week.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(250f, 58f), new Vector2(0f, 1f));
            _weekLabel = Label(week.transform, "Text", "1주차 · 봄", 22, Color.white);
            CoastUiArt.OutlineText(_weekLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);

            var chapter = CoastUiArt.CutePill(_root, "ChapterPill", Grape, 18, 4);
            Place(chapter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(170f, 58f), new Vector2(0.5f, 1f));
            _chapterLabel = Label(chapter.transform, "Text", "CH 1 / 20", 22, Color.white);
            CoastUiArt.OutlineText(_chapterLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);

            var heart = CoastUiArt.CutePill(_root, "HeartPill", Coral, 18, 4);
            Place(heart.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(230f, 58f), new Vector2(1f, 1f));
            var heartIcon = CoastUiArt.Icon("Heart");
            if (heartIcon != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(heart.transform, false);
                ic.sprite = heartIcon; ic.preserveAspect = true; ic.raycastTarget = false;
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 2f), new Vector2(40f, 40f), new Vector2(0f, 0.5f));
            }
            _heartLabel = Label(heart.transform, "Text", "0 / 41", 22, Color.white);
            _heartLabel.rectTransform.offsetMin = new Vector2(48f, 0f);
            CoastUiArt.OutlineText(_heartLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);

            var money = CoastUiArt.CutePill(_root, "MoneyPill", Sun, 18, 4);
            Place(money.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -74f), new Vector2(230f, 52f), new Vector2(1f, 1f));
            var coinIcon = CoastUiArt.AsSprite(CoastUiArt.CoinIcon);
            if (coinIcon != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(money.transform, false);
                ic.sprite = coinIcon; ic.preserveAspect = true; ic.raycastTarget = false;
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 2f), new Vector2(34f, 34f), new Vector2(0f, 0.5f));
            }
            _moneyLabel = Label(money.transform, "Text", "300", 22, Navy);
            _moneyLabel.rectTransform.offsetMin = new Vector2(48f, 0f);

            var mode = CoastUiArt.CutePill(_root, "ModePill", Mint, 16, 3);
            Place(mode.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -74f), new Vector2(250f, 44f), new Vector2(0f, 1f));
            _modeLabel = Label(mode.transform, "Text", "러닝", 17, Navy);

            // 우측 상단 세 버튼: 상점 / 타임라인 / 저장·타이틀
            SmallButton(_root, "ShopBtn", "상점", Sun, new Vector2(1f, 1f), new Vector2(-8f, -134f), 110f, OpenShop);
            SmallButton(_root, "TimelineBtn", "타임라인", Sky, new Vector2(1f, 1f), new Vector2(-124f, -134f), 130f, OpenTimeline);
            SmallButton(_root, "TitleBtn", "저장·타이틀", new Color(0.6f, 0.62f, 0.7f), new Vector2(1f, 1f), new Vector2(-260f, -134f), 140f,
                () => Confirm("타이틀로 돌아갈까?", "진행은 자동 저장돼.", () => _gm.ToTitle()));
        }

        private void BuildCalendar()
        {
            var panel = CoastUiArt.Panel(_root, "Calendar", new Color(1f, 1f, 1f, 0.72f), 22);
            Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -192f), new Vector2(210f, 500f), new Vector2(0f, 1f));
            var title = Label(panel.transform, "Title", "이번 주 스케줄", 20, Navy);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));

            for (int i = 0; i < Timeline.PhasesPerWeek; i++)
            {
                int slot = i;
                var card = CoastUiArt.CutePill(panel.transform, "Slot" + i, new Color(0.88f, 0.90f, 0.94f), 16, 3);
                Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f - i * 92f), new Vector2(190f, 84f), new Vector2(0.5f, 1f));
                var btn = card.gameObject.AddComponent<Button>();
                card.raycastTarget = true;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnSlotTapped(slot));
                _slotFill[i] = card.transform.Find("Fill")?.GetComponent<Image>();

                var num = Label(card.transform, "Num", (i + 1).ToString(), 26, Navy);
                Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(30f, 40f), new Vector2(0f, 0.5f));
                _slotGlyph[i] = Label(card.transform, "Glyph", "", 20, Navy);
                Place(_slotGlyph[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(56f, 40f), new Vector2(0f, 0.5f));
                _slotName[i] = Label(card.transform, "Name", "비어 있음", 16, Ink);
                _slotName[i].alignment = TextAnchor.MiddleLeft;
                Place(_slotName[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 60f), new Vector2(0.5f, 0.5f));
                _slotName[i].rectTransform.offsetMin = new Vector2(100f, -30f);
                _slotName[i].rectTransform.offsetMax = new Vector2(-8f, 30f);
            }

            var run = CoastUiArt.CutePill(panel.transform, "Run", Coral, 18, 4);
            Place(run.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(190f, 56f), new Vector2(0.5f, 0f));
            _runButton = run.gameObject.AddComponent<Button>();
            run.raycastTarget = true;
            _runButton.transition = Selectable.Transition.None;
            _runButton.onClick.AddListener(OnRunPressed);
            _runLabel = Label(run.transform, "Text", "실행", 24, Color.white);
            CoastUiArt.OutlineText(_runLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);
        }

        private void BuildCharacter()
        {
            _charRoot = new GameObject("Character", typeof(RectTransform)).GetComponent<RectTransform>();
            _charRoot.SetParent(_root, false);
            Place(_charRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(300f, 500f), new Vector2(0.5f, 1f));

            var plate = CoastUiArt.Panel(_charRoot, "Plate", new Color(1f, 1f, 1f, 0.35f), 40);
            Place(plate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(260f, 60f), new Vector2(0.5f, 0f));

            var img = new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(_charRoot, false);
            img.preserveAspect = true;
            img.raycastTarget = false;
            Place(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(300f, 440f), new Vector2(0.5f, 0f));
            _charImage = img;

            _charFace = Label(_charRoot, "Face", "", 64, Navy);
            Place(_charFace.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 100f), new Vector2(0.5f, 0.5f));

            var bubble = CoastUiArt.Panel(_charRoot, "Bubble", new Color(1f, 1f, 1f, 0.92f), 18);
            Place(bubble.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(280f, 54f), new Vector2(0.5f, 1f));
            _bubble = Label(bubble.transform, "Text", "오늘도 송전탑이 잘 보여.", 16, Ink);
        }

        private void BuildStats()
        {
            var panel = CoastUiArt.Panel(_root, "Stats", new Color(1f, 1f, 1f, 0.72f), 22);
            Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -192f), new Vector2(200f, 500f), new Vector2(1f, 1f));
            var title = Label(panel.transform, "Title", "스탯", 20, Navy);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));

            StatRow(panel.transform, StatKind.Stamina, "체력", Mint, 0);
            StatRow(panel.transform, StatKind.Agility, "순발력", Sky, 1);
            StatRow(panel.transform, StatKind.Charm, "매력", Pink, 2);
            StatRow(panel.transform, StatKind.Stress, "스트레스", Grape, 3);

            _burnoutHint = Label(panel.transform, "Burnout", "", 14, Coral);
            Place(_burnoutHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 48f), new Vector2(0.5f, 0f));
            _burnoutHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void StatRow(Transform parent, StatKind kind, string name, Color color, int index)
        {
            float y = -56f - index * 100f;
            var label = Label(parent, name + "Label", name, 16, Ink);
            label.alignment = TextAnchor.MiddleLeft;
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(0f, 24f), new Vector2(0.5f, 1f));
            label.rectTransform.offsetMin = new Vector2(14f, y - 24f);
            label.rectTransform.offsetMax = new Vector2(-14f, y);

            var value = Label(parent, name + "Value", "0", 16, Navy);
            value.alignment = TextAnchor.MiddleRight;
            Place(value.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(0f, 24f), new Vector2(0.5f, 1f));
            value.rectTransform.offsetMin = new Vector2(14f, y - 24f);
            value.rectTransform.offsetMax = new Vector2(-14f, y);
            _statValue[kind] = value;

            var track = CoastUiArt.Panel(parent, name + "Track", new Color(0.86f, 0.87f, 0.90f), 9);
            Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 28f), new Vector2(0f, 18f), new Vector2(0.5f, 1f));
            track.rectTransform.offsetMin = new Vector2(14f, y - 46f);
            track.rectTransform.offsetMax = new Vector2(-14f, y - 28f);

            var fill = CoastUiArt.Panel(track.transform, "Fill", color, 8);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            fill.rectTransform.offsetMin = new Vector2(2f, 2f);
            fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _statFill[kind] = fill;

            if (kind == StatKind.Stress)
            {
                // 체력 위치 마커: 이 선을 넘으면 번아웃.
                var marker = CoastHudLayout.MakeImage(track.transform, "Marker", new Vector2(0.3f, 0f), new Vector2(0.3f, 1f),
                    new Vector2(-2f, -4f), new Vector2(2f, 4f), Coral);
                _burnoutMarker = marker.rectTransform;
            }
        }

        private void BuildBottom()
        {
            var panel = CoastUiArt.Panel(_root, "Bottom", new Color(1f, 1f, 1f, 0.80f), 26);
            Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 330f), new Vector2(0.5f, 0f));
            panel.rectTransform.offsetMin = new Vector2(8f, 8f);
            panel.rectTransform.offsetMax = new Vector2(-8f, 428f);

            // 탭
            var tabs = new (ScheduleCategory cat, string name, Color color)[]
            {
                (ScheduleCategory.Job, "알바", Sun),
                (ScheduleCategory.SelfDev, "자기계발", Sky),
                (ScheduleCategory.Rest, "휴식", Mint),
                (ScheduleCategory.Story, "스토리", Coral),
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                var t = tabs[i];
                var pill = CoastUiArt.CutePill(panel.transform, "Tab" + t.cat, t.color, 16, 3);
                Place(pill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f + i * 172f, -10f), new Vector2(162f, 52f), new Vector2(0f, 1f));
                var btn = pill.gameObject.AddComponent<Button>();
                pill.raycastTarget = true;
                btn.transition = Selectable.Transition.None;
                var cat = t.cat;
                btn.onClick.AddListener(() => { _tab = cat; RefreshCards(); });
                var lbl = Label(pill.transform, "Text", t.name, 20, Color.white);
                CoastUiArt.OutlineText(lbl, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                _tabButtons.Add(btn);
            }

            _cardRow = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            _cardRow.SetParent(panel.transform, false);
            _cardRow.anchorMin = new Vector2(0f, 0f);
            _cardRow.anchorMax = new Vector2(1f, 1f);
            _cardRow.offsetMin = new Vector2(10f, 10f);
            _cardRow.offsetMax = new Vector2(-10f, -72f);
        }

        private void RefreshCards()
        {
            if (_cardRow == null || Save == null) return;
            for (int i = _cardRow.childCount - 1; i >= 0; i--)
                Destroy(_cardRow.GetChild(i).gameObject);

            for (int i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].transform.localScale = Vector3.one * ((int)_tab == i ? 1.06f : 0.96f);

            var season = Timeline.SeasonOf(Save.week);
            var defs = ScheduleTable.ByCategory(_tab, season);
            // 2열 그리드 카드
            const float w = 336f, h = 118f, gap = 8f;
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                int col = i % 2, row = i / 2;
                var card = CoastUiArt.CutePill(_cardRow, "Card_" + d.id, CardColor(d), 16, 3);
                Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(col * (w + gap), -row * (h + gap)), new Vector2(w, h), new Vector2(0f, 1f));
                var btn = card.gameObject.AddComponent<Button>();
                card.raycastTarget = true;
                btn.transition = Selectable.Transition.None;
                var def = d;
                btn.onClick.AddListener(() => OnCardTapped(def));

                var glyph = Label(card.transform, "Glyph", d.glyph, 22, Color.white);
                CoastUiArt.OutlineText(glyph, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                Place(glyph.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(64f, 34f), new Vector2(0f, 1f));

                var name = Label(card.transform, "Name", d.displayName, 19, Color.white);
                name.alignment = TextAnchor.MiddleLeft;
                CoastUiArt.OutlineText(name, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 34f), new Vector2(0.5f, 1f));
                name.rectTransform.offsetMin = new Vector2(78f, -42f);
                name.rectTransform.offsetMax = new Vector2(-10f, -8f);

                var desc = Label(card.transform, "Desc", Describe(d, season), 14, Color.white);
                desc.alignment = TextAnchor.UpperLeft;
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.verticalOverflow = VerticalWrapMode.Truncate;
                Place(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                desc.rectTransform.offsetMin = new Vector2(12f, 8f);
                desc.rectTransform.offsetMax = new Vector2(-10f, -46f);
            }

            if (defs.Count == 0)
            {
                var none = Label(_cardRow, "None", "이 계절엔 할 수 있는 게 없어.", 16, Ink);
                Place(none.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            }
        }

        private static Color CardColor(ScheduleDef d)
        {
            switch (d.category)
            {
                case ScheduleCategory.Job: return new Color(0.96f, 0.70f, 0.30f);
                case ScheduleCategory.SelfDev: return new Color(0.40f, 0.66f, 0.92f);
                case ScheduleCategory.Rest: return new Color(0.42f, 0.78f, 0.68f);
                default: return new Color(0.98f, 0.50f, 0.50f);
            }
        }

        private string Describe(ScheduleDef d, SeasonKind season)
        {
            if (d.category == ScheduleCategory.Story)
            {
                var rec = Save?.CurrentChapter;
                string target = rec != null ? $"목표 {rec.heartsTarget}개 · 지금 {Save.chapterHearts}개" : "";
                return $"장애물을 피해 송전탑까지. 하트를 모아 S급을 노려.\n{target}";
            }
            var parts = new List<string>();
            if (d.dMoney != 0) parts.Add($"돈 {Signed(d.dMoney)}");
            if (d.dStamina != 0) parts.Add($"체력 {Signed(d.dStamina)}");
            if (d.dAgility != 0) parts.Add($"순발력 {Signed(d.dAgility)}");
            if (d.dCharm != 0) parts.Add($"매력 {Signed(d.dCharm)}");
            if (d.dStress != 0) parts.Add($"스트레스 {Signed(d.dStress)}");
            string line1 = string.Join("  ", parts);
            string line2 = d.category == ScheduleCategory.Rest
                ? "판정 없음 · 항상 성공"
                : $"{StatName(d.primaryStat)} 판정 · 성공률 {ScheduleJudge.SuccessChance(d, Save.stats):P0}";
            if (d.heartsOnGreat > 0) line2 += $" · 대성공 시 ♥{d.heartsOnGreat}";
            if (d.hasBonusSeason && d.bonusSeason == season) line2 += $" · {Timeline.SeasonName(season)} 보너스";
            return line1 + "\n" + line2;
        }

        private static string Signed(int v) => (v > 0 ? "+" : "") + v;

        private static string StatName(StatKind k)
        {
            switch (k)
            {
                case StatKind.Stamina: return "체력";
                case StatKind.Agility: return "순발력";
                case StatKind.Charm: return "매력";
                case StatKind.Stress: return "스트레스";
                default: return "-";
            }
        }

        private void BuildLogPanel()
        {
            _logPanel = new GameObject("LogPanel", typeof(RectTransform), typeof(Image)).gameObject;
            _logPanel.transform.SetParent(_overlay, false);
            var dim = _logPanel.GetComponent<Image>();
            dim.color = new Color(0.05f, 0.06f, 0.12f, 0.55f);
            dim.raycastTarget = true;
            Place(_logPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), Vector2.zero, new Vector2(0.5f, 0.5f));
            var rt = _logPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);

            var panel = CoastUiArt.Panel(_logPanel.transform, "Panel", Cream, 26);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(560f, 420f), new Vector2(0.5f, 0.5f));
            _logTitle = Label(panel.transform, "Title", "", 26, Navy);
            Place(_logTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            _logBody = Label(panel.transform, "Body", "", 19, Ink);
            _logBody.alignment = TextAnchor.UpperLeft;
            _logBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_logBody.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            _logBody.rectTransform.offsetMin = new Vector2(28f, 60f);
            _logBody.rectTransform.offsetMax = new Vector2(-28f, -64f);
            _logHint = Label(panel.transform, "Hint", "화면을 터치하면 계속", 15, new Color(0.5f, 0.52f, 0.6f));
            Place(_logHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), new Vector2(0f, 30f), new Vector2(0.5f, 0f));
            _logPanel.SetActive(false);
        }

        private void BuildToast()
        {
            var pill = CoastUiArt.CutePill(_overlay, "Toast", Navy, 18, 4);
            Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(520f, 60f), new Vector2(0.5f, 0.5f));
            _toastText = Label(pill.transform, "Text", "", 20, Color.white);
            _toast = pill.gameObject;
            _toast.SetActive(false);
        }

        // ────────────────────────────────────────────────────────────────
        // Refresh
        // ────────────────────────────────────────────────────────────────

        public void Refresh()
        {
            var s = Save;
            if (s == null) return;
            var season = Timeline.SeasonOf(s.week);
            var rec = s.CurrentChapter;

            _weekLabel.text = $"{s.week}주차 · {Timeline.SeasonName(season)}" + (rec != null ? $"  ({rec.weekEnd - s.week + 1}주 남음)" : "");
            _chapterLabel.text = _gm.IsRetry ? $"재도전 CH {s.chapter}" : $"CH {s.chapter} / {Timeline.Chapters}";
            _heartLabel.text = rec != null ? $"{s.chapterHearts} / {rec.heartsTarget}" : s.chapterHearts.ToString();
            _moneyLabel.text = s.stats.money.ToString("N0");
            _modeLabel.text = (s.runMode == RunMode.Skateboard ? "스케이트보드 (×1.3)" : "러닝") +
                              (s.equippedPet != PetKind.None ? " · " + PetCompanion.Names[(int)s.equippedPet] : "");

            ApplySeasonSkin(season);
            RefreshSlots();
            RefreshStats();
            RefreshCharacter();
            RefreshCards();
        }

        private void ApplySeasonSkin(SeasonKind season)
        {
            Color ground, sky;
            switch (season)
            {
                case SeasonKind.Spring: ground = new Color(0.93f, 0.90f, 0.78f); sky = new Color(0.80f, 0.88f, 0.98f); break;
                case SeasonKind.Summer: ground = new Color(0.90f, 0.93f, 0.86f); sky = new Color(0.55f, 0.78f, 0.97f); break;
                case SeasonKind.Autumn: ground = new Color(0.95f, 0.88f, 0.74f); sky = new Color(0.98f, 0.80f, 0.60f); break;
                default: ground = new Color(0.90f, 0.92f, 0.96f); sky = new Color(0.78f, 0.84f, 0.92f); break;
            }
            _bg.color = ground;
            _bgBand.color = sky;
            var art = ArtAssets.LoadTexture("UI_Raising_" + season);
            if (art != null)
            {
                _bgBand.sprite = CoastUiArt.AsSprite(art);
                _bgBand.color = Color.white;
                _bgBand.preserveAspect = false;
            }
            else
                _bgBand.sprite = null;
        }

        private void RefreshSlots()
        {
            var s = Save;
            for (int i = 0; i < Timeline.PhasesPerWeek; i++)
            {
                var def = ScheduleTable.Get(s.queuedSchedule != null && i < s.queuedSchedule.Length ? s.queuedSchedule[i] : null);
                bool done = i < s.phaseIndex;
                _slotName[i].text = done ? "완료" : def != null ? def.displayName : "비어 있음";
                _slotGlyph[i].text = def != null ? def.glyph : "";
                if (_slotFill[i] != null)
                    _slotFill[i].color = done ? new Color(0.75f, 0.76f, 0.80f)
                        : def != null ? Color.Lerp(CardColor(def), Color.white, 0.35f)
                        : i == _selectedSlot ? new Color(1f, 0.95f, 0.78f) : new Color(0.93f, 0.94f, 0.97f);
            }
            bool ready = true;
            for (int i = s.phaseIndex; i < Timeline.PhasesPerWeek; i++)
                if (ScheduleTable.Get(s.queuedSchedule[i]) == null) ready = false;
            // 회차가 끝난 뒤(엔딩 시청)엔 타임라인 재도전만 남는다.
            bool finished = !_gm.IsRetry && s.chapter >= Timeline.Chapters && s.CurrentChapter != null && s.CurrentChapter.cleared;
            if (finished) ready = false;
            _runButton.interactable = ready && !_busy;
            _runLabel.text = finished ? "타임라인에서 재도전" : s.phaseIndex > 0 ? "이어서 실행" : "실행";
            _runLabel.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }

        private void RefreshStats()
        {
            var st = Save.stats;
            SetStat(StatKind.Stamina, st.stamina);
            SetStat(StatKind.Agility, st.agility);
            SetStat(StatKind.Charm, st.charm);
            SetStat(StatKind.Stress, st.stress);
            if (_burnoutMarker != null)
            {
                float x = Mathf.Clamp01(st.stamina / (float)PlayerStats.StatMax);
                _burnoutMarker.anchorMin = new Vector2(x, 0f);
                _burnoutMarker.anchorMax = new Vector2(x, 1f);
            }
            _burnoutHint.text = st.Burnout ? "번아웃! 스트레스가 체력을 넘었어.\n휴식이 필요해." :
                st.stress > st.stamina * 0.7f ? "스트레스가 꽤 쌓였어." : "";
        }

        private void SetStat(StatKind kind, int value)
        {
            if (_statFill.TryGetValue(kind, out var fill))
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / (float)PlayerStats.StatMax), 1f);
            if (_statValue.TryGetValue(kind, out var txt))
                txt.text = value.ToString();
        }

        private enum Mood { Happy, Normal, Tired, Great, Fail }

        private void RefreshCharacter(Mood? force = null)
        {
            var st = Save.stats;
            float ratio = st.stamina > 0 ? st.stress / (float)st.stamina : 2f;
            Mood mood = force ?? (ratio < 0.5f ? Mood.Happy : ratio < 1f ? Mood.Normal : Mood.Tired);

            string key = mood == Mood.Great ? "Happy" : mood == Mood.Fail ? "Tired" : mood.ToString();
            var tex = ArtAssets.LoadTexture("Raise_Girl_" + key) ?? ArtAssets.LoadTexture("Raise_Girl_Normal");
            if (tex == null)
                tex = ArtAssets.LoadTexture("GirlSkater_Back");
            if (tex != null)
            {
                _charImage.sprite = CoastUiArt.AsSprite(ChromaKeyed(tex));
                _charImage.enabled = true;
                _charFace.text = "";
            }
            else
            {
                _charImage.enabled = false;
                _charFace.text = mood == Mood.Happy || mood == Mood.Great ? "(^▽^)" : mood == Mood.Normal ? "(・ω・)" : "(>_<)";
            }

            if (force == null)
            {
                var season = Timeline.SeasonOf(Save.week);
                _bubble.text = mood == Mood.Tired ? "…좀 쉬고 싶어."
                    : mood == Mood.Happy ? (season == SeasonKind.Winter ? "눈 오면 송전탑에 가자." : "오늘도 송전탑이 잘 보여.")
                    : "라디오 주파수, 오늘은 맞을까.";
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Interaction
        // ────────────────────────────────────────────────────────────────

        private void OnSlotTapped(int slot)
        {
            if (_busy || Save == null || slot < Save.phaseIndex) return;
            if (!string.IsNullOrEmpty(Save.queuedSchedule[slot]))
            {
                _gm.SetQueued(slot, null);
                _selectedSlot = slot;
            }
            else
                _selectedSlot = _selectedSlot == slot ? -1 : slot;
            RefreshSlots();
        }

        private void OnCardTapped(ScheduleDef def)
        {
            if (_busy || Save == null) return;
            int slot = _selectedSlot;
            if (slot < Save.phaseIndex || slot < 0 || !string.IsNullOrEmpty(Save.queuedSchedule[slot]))
            {
                slot = -1;
                for (int i = Save.phaseIndex; i < Timeline.PhasesPerWeek; i++)
                    if (string.IsNullOrEmpty(Save.queuedSchedule[i])) { slot = i; break; }
            }
            if (slot < 0)
            {
                Toast("칸이 다 찼어. 칸을 눌러 비운 뒤 골라줘.");
                return;
            }
            if (def.category == ScheduleCategory.Story)
            {
                // 스토리는 그 이후 칸을 비운다(돌입하면 주가 끝난다).
                for (int i = slot + 1; i < Timeline.PhasesPerWeek; i++)
                    _gm.SetQueued(i, ScheduleTable.StoryId);
            }
            _gm.SetQueued(slot, def.id);
            _selectedSlot = -1;
            RefreshSlots();
        }

        private void OnRunPressed()
        {
            if (_busy || Save == null) return;
            StartCoroutine(ExecuteWeek());
        }

        private IEnumerator ExecuteWeek()
        {
            _busy = true;
            _runButton.interactable = false;
            for (int i = Save.phaseIndex; i < Timeline.PhasesPerWeek; i++)
            {
                var def = ScheduleTable.Get(Save.queuedSchedule[i]);
                if (def != null && def.category == ScheduleCategory.Story)
                {
                    _gm.ResolvePhase(i);   // phaseIndex 전진
                    yield return ShowLog("스토리 돌입", "송전탑 가는 길로. 장애물을 피해 하트를 모으자.\n\n" +
                                          $"이번 챕터 목표 ♥{Save.CurrentChapter?.heartsTarget}  ·  지금 ♥{Save.chapterHearts}", 0.6f);
                    _gm.StartStoryRun();
                    yield break;
                }

                var result = _gm.ResolvePhase(i);
                if (!result.HasValue) continue;
                var r = result.Value;
                RefreshSlots();
                RefreshCharacter(r.outcome == Outcome.GreatSuccess ? Mood.Great : r.outcome == Outcome.Fail ? Mood.Fail : (Mood?)null);
                _bubble.text = r.outcome == Outcome.GreatSuccess ? "해냈다!" : r.outcome == Outcome.Fail ? "으으… 망했어." : "그럭저럭.";
                yield return ShowLogTyped($"{i + 1}페이즈 · {r.def.displayName}", r.logLines, r.outcome);
                RefreshStats();
            }

            bool forced = _gm.AdvanceWeek();
            Refresh();
            if (forced)
            {
                yield return ShowLog("챕터 마지막 주", "이번 주가 이 챕터의 마지막 주야.\n이제 스토리로 가야 해.", 0.4f);
                _gm.StartStoryRun();
                yield break;
            }

            _busy = false;
            RefreshSlots();
            var ev = _gm.RollRandomEvent();
            if (ev.HasValue) ShowEvent(ev.Value);
        }

        private IEnumerator ShowLogTyped(string title, string[] lines, Outcome outcome)
        {
            _logPanel.SetActive(true);
            _logTitle.text = title;
            _logTitle.color = outcome == Outcome.GreatSuccess ? Sun : outcome == Outcome.Fail ? Coral : Navy;
            _logBody.text = "";
            _logHint.text = "";
            _tapped = false;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine(lines[i]);
                _logBody.text = sb.ToString();
                float wait = i == 0 ? 0.45f : 0.22f;
                float t = 0f;
                while (t < wait && !_tapped) { t += Time.unscaledDeltaTime; yield return null; }
                if (_tapped) { _logBody.text = string.Join("\n", lines); break; }
            }
            _logHint.text = "화면을 터치하면 계속";
            _tapped = false;
            float idle = 0f;
            while (!_tapped && idle < 2.5f) { idle += Time.unscaledDeltaTime; yield return null; }
            _logPanel.SetActive(false);
        }

        private IEnumerator ShowLog(string title, string body, float minSeconds)
        {
            _logPanel.SetActive(true);
            _logTitle.text = title;
            _logTitle.color = Navy;
            _logBody.text = body;
            _logHint.text = "화면을 터치하면 계속";
            _tapped = false;
            float t = 0f;
            while (t < minSeconds) { t += Time.unscaledDeltaTime; yield return null; }
            _tapped = false;
            float idle = 0f;
            while (!_tapped && idle < 2.5f) { idle += Time.unscaledDeltaTime; yield return null; }
            _logPanel.SetActive(false);
        }

        public void Toast(string text)
        {
            StopCoroutine(nameof(ToastRoutine));
            StartCoroutine(nameof(ToastRoutine), text);
        }

        private IEnumerator ToastRoutine(string text)
        {
            _toastText.text = text;
            _toast.SetActive(true);
            float t = 0f;
            while (t < 1.6f) { t += Time.unscaledDeltaTime; yield return null; }
            _toast.SetActive(false);
        }

        // ────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────

        private static readonly Dictionary<Texture2D, Texture2D> KeyedCache = new Dictionary<Texture2D, Texture2D>();

        /// 월드 스프라이트(마젠타 키)를 UI에 그리기 위해 마젠타를 알파로 바꾼 사본. 읽기 불가
        /// 텍스처도 RenderTexture 경유로 읽는다. 한 번 만들면 캐시.
        private static Texture2D ChromaKeyed(Texture2D src)
        {
            if (src == null) return null;
            if (KeyedCache.TryGetValue(src, out var cached) && cached != null) return cached;

            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var px = copy.GetPixels32();
            bool anyKey = false;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                // 마젠타 거리: R·B 높고 G 낮음. 경계는 부드럽게.
                float d = Mathf.Max(0f, (c.r - c.g) + (c.b - c.g)) / 255f;   // 0..2
                float key = Mathf.Clamp01((d - 0.9f) / 0.5f);
                if (key > 0f) anyKey = true;
                c.a = (byte)Mathf.RoundToInt(255f * (1f - key));
                px[i] = c;
            }
            if (!anyKey) { UnityEngine.Object.Destroy(copy); KeyedCache[src] = src; return src; }
            copy.SetPixels32(px);
            copy.Apply(false, false);
            copy.filterMode = FilterMode.Bilinear;
            KeyedCache[src] = copy;
            return copy;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (anchorMin == anchorMax)
                rt.sizeDelta = size;
            else if (Mathf.Approximately(anchorMin.x, anchorMax.x))
                rt.sizeDelta = new Vector2(size.x, 0f);
            else if (Mathf.Approximately(anchorMin.y, anchorMax.y))
                rt.sizeDelta = new Vector2(0f, size.y);
        }

        private static Text Label(Transform parent, string name, string text, int size, Color color)
        {
            var t = CoastHudLayout.MakeText(parent, name, text, size, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = color;
            return t;
        }

        private Button SmallButton(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, float width, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 14, 3);
            Place(pill.rectTransform, anchor, anchor, pos, new Vector2(width, 44f), anchor);
            var btn = pill.gameObject.AddComponent<Button>();
            pill.raycastTarget = true;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = Label(pill.transform, "Text", label, 17, Color.white);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            return btn;
        }

        private Button BigButton(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 18, 4);
            Place(pill.rectTransform, anchor, anchor, pos, size, anchor);
            var btn = pill.gameObject.AddComponent<Button>();
            pill.raycastTarget = true;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = Label(pill.transform, "Text", label, 21, Color.white);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return btn;
        }

        private GameObject Modal(string name, float width, float height, out RectTransform panel)
        {
            var dimGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(_overlay, false);
            var dim = dimGo.GetComponent<Image>();
            dim.color = new Color(0.05f, 0.06f, 0.12f, 0.6f);
            dim.raycastTarget = true;
            var rt = dimGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);

            var p = CoastUiArt.Panel(dimGo.transform, "Panel", Cream, 26);
            p.raycastTarget = true;
            Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), new Vector2(0.5f, 0.5f));
            panel = p.rectTransform;
            return dimGo;
        }

        private void Confirm(string title, string body, Action onYes)
        {
            var modal = Modal("Confirm", 520f, 280f, out var panel);
            var t = Label(panel, "Title", title, 24, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            var b = Label(panel, "Body", body, 18, Ink);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(b.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            b.rectTransform.offsetMin = new Vector2(24f, 0f); b.rectTransform.offsetMax = new Vector2(-24f, 0f);
            BigButton(panel, "Yes", "응", Coral, new Vector2(0.5f, 0f), new Vector2(110f, 18f), new Vector2(190f, 56f), () => { _modalPrimary = null; Destroy(modal); onYes?.Invoke(); });
            BigButton(panel, "No", "아니", new Color(0.6f, 0.62f, 0.7f), new Vector2(0.5f, 0f), new Vector2(-110f, 18f), new Vector2(190f, 56f), () => { _modalPrimary = null; Destroy(modal); });
            _modalPrimary = () => { Destroy(modal); onYes?.Invoke(); };
        }
    }
}
