using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 프린세스 메이커 2 감성의 모바일 세로형 육성 메인 화면 (720×1280 기준, 런타임 빌드).
    ///
    ///   1. Top HUD      (7.5%)  주차·요일 / 돈 / 챕터 Lv / 컨디션 하트 — 스크롤과 무관하게 고정
    ///   2. Character Room (42.5%) 우드 클럽하우스 배경 + 스탠딩 캐릭터 (컨디션에 따라 표정·자세)
    ///   3. Stats Tab Area (30%)  [신체 능력] [정신/생활] 탭 + 10칸 빨간 블럭 바 + 숫자, 롱프레스 툴팁, 스와이프 탭 전환
    ///   4. Coach & Action (20%)  원형 초상 + 말풍선 + [스케줄] [실행] [스토리]
    ///
    /// 아트: 아이보리 #FFF8E7 · 우드 #8D6E63 · 골드 #D4AF37 · 레드 #E53935. 모든 패널은 골드 장식 코너 +
    /// 얇은 내부 섀도우(플랫 금지). 터치 영역 최소 48dp, 버튼 hitSlop 10px.
    public partial class RaisingUI : MonoBehaviour
    {
        // ── 팔레트 ──────────────────────────────────────────────────────
        private static readonly Color Ivory = Hex("#FFF8E7");
        private static readonly Color Wood = Hex("#8D6E63");
        private static readonly Color WoodDark = Hex("#4E342E");
        private static readonly Color Gold = Hex("#D4AF37");
        private static readonly Color GoldLight = Hex("#F1D37A");
        private static readonly Color Red = Hex("#E53935");
        private static readonly Color RedEmpty = new Color(0.36f, 0.10f, 0.10f, 0.28f);
        private static readonly Color Cream = Ivory;
        private static readonly Color Navy = Hex("#3E2723");
        private static readonly Color Ink = Hex("#4E342E");
        private static readonly Color Coral = Hex("#FF6F91");
        private static readonly Color Mint = Hex("#4DB6AC");
        private static readonly Color Sky = Hex("#64B5F6");
        private static readonly Color Sun = Hex("#FFB74D");
        private static readonly Color Grape = Hex("#9575CD");
        private static readonly Color Pink = Hex("#F48FB1");

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private GameManager _gm;
        private Canvas _canvas;
        private Canvas _overlayCanvas;
        private RectTransform _root;
        private RectTransform _overlay;

        // HUD
        private Text _dateLabel;
        private Text _moneyLabel;
        private Text _levelLabel;
        private Text _condLabel;
        private Image _condHeart;

        // Room
        private Image _roomBg;
        private Image _roomTint;
        private Image _charImage;
        private Text _charFace;
        private RectTransform _charRoot;
        private Image _darkCircles;
        private Text _heartsLabel;

        // Stats
        private enum StatTab { Body, Mind }
        private StatTab _statTab = StatTab.Body;
        private Image _tabBody, _tabMind;
        private Text _tabBodyText, _tabMindText;
        private RectTransform _statList;
        private ScrollRect _statScroll;
        private readonly List<StatRowView> _statRows = new List<StatRowView>();
        private GameObject _tooltip;
        private Text _tooltipText;
        private Coroutine _longPress;

        private class StatRowView
        {
            public StatKind kind;
            public string key;          // stat 외 항목(하트 등)
            public Text value;
            public Image[] blocks = new Image[10];
            public RectTransform marker; // 스트레스: 체력 마커
        }

        // Bottom
        private Image _portraitFace;
        private Text _bubble;
        private readonly Text[] _slotChip = new Text[Timeline.PhasesPerWeek];
        private readonly Image[] _slotChipBg = new Image[Timeline.PhasesPerWeek];
        private Button _scheduleBtn, _runButton, _storyBtn;
        private Text _runLabel, _storyLabel;

        // Schedule sheet
        private GameObject _sheet;
        private RectTransform _cardRow;
        private ScheduleCategory _tab = ScheduleCategory.Job;
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly Text[] _slotName = new Text[Timeline.PhasesPerWeek];
        private readonly Text[] _slotGlyph = new Text[Timeline.PhasesPerWeek];
        private readonly Image[] _slotFill = new Image[Timeline.PhasesPerWeek];
        private Text _sheetTitle;
        private int _selectedSlot = -1;

        // Log / toast / modal
        private GameObject _logPanel;
        private Image _logArt;
        private Image _logArtFrame;
        private Text _logTitle;
        private Text _logBody;
        private Text _logHint;
        private bool _tapped;
        private bool _busy;
        private GameObject _toast;
        private Text _toastText;
        private Action _modalPrimary;
        private int _timelinePage;

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

        /// 에디터 검증용 키: Q/W/E/R 탭, 1~9 카드, Backspace 칸 비우기, Return 실행, S 상점, T 타임라인, A 스케줄 시트, Tab 스탯 탭.
        private void DevKeys()
        {
            if (Save == null) return;
            if (Input.GetKeyDown(KeyCode.Tab)) SetStatTab(_statTab == StatTab.Body ? StatTab.Mind : StatTab.Body);
            if (Input.GetKeyDown(KeyCode.A) && !_busy) ToggleSheet(true);
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
            if (Input.GetKeyDown(KeyCode.Escape) && _sheet != null && _sheet.activeSelf) ToggleSheet(false);
            if (Input.GetKeyDown(KeyCode.Return) && !_busy) OnRunPressed();
            if (Input.GetKeyDown(KeyCode.S) && !_busy) OpenShop();
            if (Input.GetKeyDown(KeyCode.T) && !_busy) OpenTimeline();
        }

        // ────────────────────────────────────────────────────────────────
        // Build
        // ────────────────────────────────────────────────────────────────

        private const float HudH = 0.925f;    // HUD 아래 경계
        private const float RoomB = 0.50f;    // 룸 아래 경계
        private const float StatsB = 0.205f;  // 스탯 아래 경계

        private void Build()
        {
            _canvas = CoastUiCanvas.Create("RaisingCanvas", 100);
            _root = CoastUiCanvas.Root(_canvas);
            _overlayCanvas = CoastUiCanvas.Create("RaisingOverlay", 120);
            _overlay = CoastUiCanvas.Root(_overlayCanvas);

            // 전체 바탕: 아이보리 + 우드 프레임 (안전 영역 밖까지)
            var bg = CoastHudLayout.MakeImage(_root, "Background", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), Wood);
            bg.transform.SetAsFirstSibling();
            var paper = CoastHudLayout.MakeImage(_root, "Paper", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad + 10f, -CoastUiCanvas.HudPad + 10f), new Vector2(CoastUiCanvas.HudPad - 10f, CoastUiCanvas.HudPad - 10f), Ivory);
            paper.sprite = CoastUiArt.RoundedRect(22);
            paper.type = Image.Type.Sliced;
            paper.transform.SetSiblingIndex(1);

            BuildRoom();
            BuildHud();
            BuildStats();
            BuildBottom();
            BuildScheduleSheet();
            BuildLogPanel();
            BuildToast();
            BuildTooltip();
        }

        // ── 1. Top HUD ──────────────────────────────────────────────────

        private void BuildHud()
        {
            var bar = OrnatePanel(_root, "HudBar", WoodDark, new Vector2(0f, HudH), new Vector2(1f, 1f), new Vector2(6f, 2f), new Vector2(-6f, -4f));
            var inner = bar.transform;

            // [2월 2일 화 📅] 자리 → 주차·계절·요일(챕터 남은 주)
            _dateLabel = Pill(inner, "Date", "1주차 · 봄", 20, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(236f, 52f), Wood, GoldLight);
            AddHit(_dateLabel.transform.parent.gameObject, OpenTimeline);
            // [💰 500G]
            _moneyLabel = Pill(inner, "Money", "300", 20, new Vector2(0f, 0.5f), new Vector2(256f, 0f), new Vector2(160f, 52f), Wood, GoldLight, iconTex: CoastUiArt.CoinIcon);
            AddHit(_moneyLabel.transform.parent.gameObject, OpenShop);
            // [⭐ Lv.10] → 챕터
            _levelLabel = Pill(inner, "Level", "CH 1", 20, new Vector2(0f, 0.5f), new Vector2(426f, 0f), new Vector2(136f, 52f), Wood, GoldLight, iconSprite: CoastUiArt.Icon("Star"));
            // [컨디션 ❤️]
            _condLabel = Pill(inner, "Cond", "최상", 18, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(116f, 52f), Wood, GoldLight, iconSprite: CoastUiArt.Icon("Heart"));
            _condHeart = _condLabel.transform.parent.Find("Icon")?.GetComponent<Image>();
        }

        // ── 2. Character Room ───────────────────────────────────────────

        private void BuildRoom()
        {
            var frame = OrnatePanel(_root, "Room", Gold, new Vector2(0f, RoomB), new Vector2(1f, HudH), new Vector2(6f, 4f), new Vector2(-6f, -4f));
            var host = frame.transform.Find("Inner") as RectTransform;

            // 배경: Resources/CoastRun/UI_Raising_Room(계절별 _<Season> 우선). 없으면 우드 그라데이션 + 소품 실루엣.
            _roomBg = new GameObject("RoomBg", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _roomBg.transform.SetParent(host, false);
            Stretch(_roomBg.rectTransform, 0f, 0f, 0f, 0f);
            _roomBg.color = Hex("#A1887F");
            _roomBg.raycastTarget = false;
            _roomBg.preserveAspect = false;

            // 간접조명: 위쪽 따뜻한 빛, 아래 어두운 바닥
            _roomTint = CoastHudLayout.MakeImage(host, "Glow", new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 0.85f, 0.55f, 0.18f));
            var floor = CoastHudLayout.MakeImage(host, "Floor", new Vector2(0f, 0f), new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero, new Color(0.25f, 0.12f, 0.08f, 0.22f));
            floor.raycastTarget = false;

            // 캐릭터 (탭 → 의상 변경 팝업은 추후; 지금은 말풍선 갱신)
            _charRoot = new GameObject("Character", typeof(RectTransform)).GetComponent<RectTransform>();
            _charRoot.SetParent(host, false);
            Place(_charRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(330f, 470f), new Vector2(0.5f, 0f));
            var shadow = CoastHudLayout.MakeImage(_charRoot, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, -4f), new Vector2(90f, 18f), new Color(0f, 0f, 0f, 0.22f));
            shadow.sprite = CoastUiArt.RoundedRect(40);
            shadow.type = Image.Type.Sliced;

            var img = new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(_charRoot, false);
            img.preserveAspect = true;
            img.raycastTarget = true;
            Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            _charImage = img;
            AddHit(img.gameObject, OnCharacterTapped);

            _darkCircles = CoastHudLayout.MakeImage(_charRoot, "DarkCircles", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -150f), new Vector2(40f, -132f), new Color(0.35f, 0.25f, 0.45f, 0.35f));
            _darkCircles.sprite = CoastUiArt.RoundedRect(10);
            _darkCircles.type = Image.Type.Sliced;
            _darkCircles.gameObject.SetActive(false);

            _charFace = Label(_charRoot, "Face", "", 64, Navy);
            Place(_charFace.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 100f), new Vector2(0.5f, 0.5f));

            // 챕터 하트 진행(좌상단 작은 명패)
            var plate = OrnatePanel(host, "HeartsPlate", Gold, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(230f, -62f), anchoredSize: true);
            _heartsLabel = Label(plate.transform.Find("Inner"), "Text", "♥ 0 / 41", 18, Red);
            var heartIcon = CoastUiArt.Icon("Heart");
            if (heartIcon != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(plate.transform.Find("Inner"), false);
                ic.sprite = heartIcon; ic.preserveAspect = true; ic.raycastTarget = false;
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f));
                _heartsLabel.rectTransform.offsetMin = new Vector2(40f, 0f);
            }

            // 우상단 미니 버튼: 상점 / 타임라인 / 저장·타이틀
            SmallButton(host, "ShopBtn", "상점", Sun, new Vector2(1f, 1f), new Vector2(-10f, -10f), 96f, OpenShop);
            SmallButton(host, "TimelineBtn", "타임라인", Sky, new Vector2(1f, 1f), new Vector2(-112f, -10f), 120f, OpenTimeline);
            SmallButton(host, "TitleBtn", "저장", new Color(0.55f, 0.50f, 0.48f), new Vector2(1f, 1f), new Vector2(-10f, -60f), 96f,
                () => Confirm("타이틀로 돌아갈까?", "진행은 자동 저장돼.", () => _gm.ToTitle()));
        }

        // ── 3. Stats Tab Area ───────────────────────────────────────────

        private void BuildStats()
        {
            var frame = OrnatePanel(_root, "Stats", Gold, new Vector2(0f, StatsB), new Vector2(1f, RoomB), new Vector2(6f, 4f), new Vector2(-6f, -4f));
            var host = frame.transform.Find("Inner") as RectTransform;

            // 탭 바
            _tabBody = TabButton(host, "TabBody", "신체 능력", new Vector2(0f, 1f), new Vector2(0.5f, 1f), () => SetStatTab(StatTab.Body), out _tabBodyText);
            _tabMind = TabButton(host, "TabMind", "정신/생활", new Vector2(0.5f, 1f), new Vector2(1f, 1f), () => SetStatTab(StatTab.Mind), out _tabMindText);

            // 세로 스크롤 리스트 + 가로 스와이프로 탭 전환
            var scrollGo = new GameObject("StatScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(host, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            Stretch(srt, 6f, 6f, -6f, -62f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);   // 드래그 수신용
            _statScroll = scrollGo.GetComponent<ScrollRect>();
            _statScroll.horizontal = false;
            _statScroll.vertical = true;
            _statScroll.movementType = ScrollRect.MovementType.Clamped;
            _statScroll.scrollSensitivity = 30f;
            var swipe = scrollGo.AddComponent<SwipeTabs>();
            swipe.OnSwipe = dir => SetStatTab(dir < 0 ? StatTab.Mind : StatTab.Body);

            _statList = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _statList.SetParent(srt, false);
            _statList.anchorMin = new Vector2(0f, 1f);
            _statList.anchorMax = new Vector2(1f, 1f);
            _statList.pivot = new Vector2(0.5f, 1f);
            _statList.anchoredPosition = Vector2.zero;
            _statScroll.content = _statList;
            _statScroll.viewport = srt;
        }

        private Image TabButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Action onClick, out Text text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(aMin.x == 0f ? 8f : 4f, -54f);
            rt.offsetMax = new Vector2(aMax.x == 1f ? -8f : -4f, -6f);
            var img = go.GetComponent<Image>();
            img.sprite = CoastUiArt.RoundedRect(14);
            img.type = Image.Type.Sliced;
            img.color = Ivory;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Haptic(); onClick(); });
            var edge = CoastUiArt.Panel(go.transform, "Edge", Gold, 14);
            Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
            edge.transform.SetAsFirstSibling();
            var fill = CoastUiArt.Panel(go.transform, "Fill", Ivory, 12);
            Stretch(fill.rectTransform, 3f, 3f, -3f, -3f);
            text = Label(go.transform, "Text", label, 21, Navy);
            return fill;
        }

        private void SetStatTab(StatTab tab)
        {
            _statTab = tab;
            RefreshStats();
        }

        private void RebuildStatRows()
        {
            for (int i = _statList.childCount - 1; i >= 0; i--)
                Destroy(_statList.GetChild(i).gameObject);
            _statRows.Clear();
            HideTooltip();

            bool body = _statTab == StatTab.Body;
            _tabBody.color = body ? Gold : Ivory;
            _tabMind.color = body ? Ivory : Gold;
            _tabBodyText.color = body ? WoodDark : Ink;
            _tabMindText.color = body ? Ink : WoodDark;

            var rows = body
                ? new (StatKind kind, string key, string label, string glyph)[]
                {
                    (StatKind.Stamina, null, "체력", "♥"),
                    (StatKind.Agility, null, "순발력", "⚡"),
                    (StatKind.None, "speed", "달리기", "»"),
                    (StatKind.None, "hp", "런닝 HP", "+"),
                }
                : new (StatKind kind, string key, string label, string glyph)[]
                {
                    (StatKind.Charm, null, "매력", "★"),
                    (StatKind.Stress, null, "스트레스", "~"),
                    (StatKind.None, "hearts", "말랑이 하트", "♥"),
                    (StatKind.None, "luck", "대성공률", "☆"),
                };

            const float rowH = 62f;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                var view = new StatRowView { kind = r.kind, key = r.key };
                var row = new GameObject("Row_" + (r.key ?? r.kind.ToString()), typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                row.SetParent(_statList, false);
                row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.anchoredPosition = new Vector2(0f, -i * rowH);
                row.sizeDelta = new Vector2(0f, rowH - 6f);
                var rowImg = row.GetComponent<Image>();
                rowImg.sprite = CoastUiArt.RoundedRect(12);
                rowImg.type = Image.Type.Sliced;
                rowImg.color = i % 2 == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.18f);

                var glyph = Label(row, "Glyph", r.glyph, 22, Red);
                Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(34f, 40f), new Vector2(0f, 0.5f));
                var label = Label(row, "Label", r.label, 20, Navy);
                label.alignment = TextAnchor.MiddleLeft;
                Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(130f, 40f), new Vector2(0f, 0.5f));

                // 10칸 블럭 바
                var bar = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
                bar.SetParent(row, false);
                Place(bar, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 26f), new Vector2(0.5f, 0.5f));
                bar.offsetMin = new Vector2(184f, -13f);
                bar.offsetMax = new Vector2(-84f, 13f);
                for (int b = 0; b < 10; b++)
                {
                    var block = new GameObject("B" + b, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    block.transform.SetParent(bar, false);
                    var brt = block.rectTransform;
                    brt.anchorMin = new Vector2(b / 10f, 0f);
                    brt.anchorMax = new Vector2((b + 1) / 10f, 1f);
                    brt.offsetMin = new Vector2(2f, 0f);
                    brt.offsetMax = new Vector2(-2f, 0f);
                    block.sprite = CoastUiArt.RoundedRect(4);
                    block.type = Image.Type.Sliced;
                    block.raycastTarget = false;
                    block.color = RedEmpty;
                    view.blocks[b] = block;
                }
                if (r.kind == StatKind.Stress)
                {
                    var marker = CoastHudLayout.MakeImage(bar, "Marker", new Vector2(0.3f, 0f), new Vector2(0.3f, 1f), new Vector2(-2f, -5f), new Vector2(2f, 5f), Gold);
                    view.marker = marker.rectTransform;
                }

                var value = Label(row, "Value", "0", 22, Navy);
                value.alignment = TextAnchor.MiddleRight;
                Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(70f, 40f), new Vector2(1f, 0.5f));
                view.value = value;

                // 롱프레스 툴팁 (0.45 s)
                var lp = row.gameObject.AddComponent<LongPress>();
                var captured = view;
                var rowRt = row;
                lp.OnLongPress = () => ShowTooltip(captured, rowRt);
                lp.OnRelease = HideTooltip;

                _statRows.Add(view);
            }
            _statList.sizeDelta = new Vector2(0f, rows.Length * rowH + 4f);
        }

        // ── 4. Coach & Action ───────────────────────────────────────────

        private void BuildBottom()
        {
            var frame = OrnatePanel(_root, "Bottom", Gold, new Vector2(0f, 0f), new Vector2(1f, StatsB), new Vector2(6f, 6f), new Vector2(-6f, -4f));
            var host = frame.transform.Find("Inner") as RectTransform;

            // 코치 원형 초상 (골드 프레임) — 주인공 얼굴 크롭
            var ring = new GameObject("PortraitRing", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ring.transform.SetParent(host, false);
            ring.sprite = CoastUiArt.RoundedRect(60);
            ring.type = Image.Type.Simple;
            ring.color = Gold;
            // 상단 38% 띠에 맞춘 원형 초상 (높이 기준 정사각)
            ring.rectTransform.anchorMin = new Vector2(0f, 0.62f); ring.rectTransform.anchorMax = new Vector2(0f, 1f);
            ring.rectTransform.pivot = new Vector2(0f, 1f);
            ring.rectTransform.anchoredPosition = new Vector2(12f, -6f);
            ring.rectTransform.sizeDelta = new Vector2(84f, -12f);
            ring.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(ring.transform, false);
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = CoastUiArt.RoundedRect(60);
            maskImg.color = Ivory;
            maskGo.GetComponent<Mask>().showMaskGraphic = true;
            Stretch(maskGo.GetComponent<RectTransform>(), 5f, 5f, -5f, -5f);
            _portraitFace = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _portraitFace.transform.SetParent(maskGo.transform, false);
            _portraitFace.preserveAspect = true;
            _portraitFace.raycastTarget = false;
            // 스탠딩 스프라이트의 머리 부분이 원 안에 오도록 크게 배치
            Place(_portraitFace.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 18f), new Vector2(180f, 270f), new Vector2(0.5f, 1f));

            // 말풍선
            var bubble = OrnatePanel(host, "Bubble", Gold, new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(112f, 4f), new Vector2(-10f, -6f));
            var tail = CoastHudLayout.MakeImage(bubble.transform, "Tail", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-14f, -10f), new Vector2(6f, 10f), Gold);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.transform.SetAsFirstSibling();
            _bubble = Label(bubble.transform.Find("Inner"), "Text", "오늘도 송전탑이 잘 보여.", 19, Ink);
            _bubble.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bubble.rectTransform.offsetMin = new Vector2(14f, 4f);
            _bubble.rectTransform.offsetMax = new Vector2(-14f, -4f);

            // 이번 주 슬롯 3칩
            for (int i = 0; i < Timeline.PhasesPerWeek; i++)
            {
                int slot = i;
                var chip = CoastUiArt.Panel(host, "Chip" + i, Ivory, 12);
                chip.rectTransform.anchorMin = new Vector2(i / 3f, 0.44f); chip.rectTransform.anchorMax = new Vector2((i + 1) / 3f, 0.60f);
                chip.rectTransform.offsetMin = new Vector2(i == 0 ? 12f : 5f, 0f); chip.rectTransform.offsetMax = new Vector2(i == 2 ? -12f : -5f, 0f);
                chip.raycastTarget = true;
                var edge = CoastUiArt.Panel(chip.transform, "Edge", Gold, 12);
                Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
                edge.transform.SetAsFirstSibling();
                var fill = CoastUiArt.Panel(chip.transform, "Fill", Ivory, 10);
                Stretch(fill.rectTransform, 2f, 2f, -2f, -2f);
                _slotChipBg[i] = fill;
                _slotChip[i] = Label(chip.transform, "Text", (i + 1) + "  비어 있음", 15, Ink);
                AddHit(chip.gameObject, () => { _selectedSlot = slot; ToggleSheet(true); });
            }

            // 액션 버튼 3개 (높이 ≥ 56dp → 112px)
            // 아래 40% 띠를 3등분 — 어떤 높이에서도 프레임 안에 남는다 (hitSlop 10px 포함).
            _scheduleBtn = ActionButton(host, "Schedule", "스케줄", Coral, Hex("#FF8FAB"), 0, () => ToggleSheet(true));
            _runButton = ActionButton(host, "Run", "실행", Mint, Hex("#80CBC4"), 1, OnRunPressed);
            _runLabel = _runButton.GetComponentInChildren<Text>();
            _storyBtn = ActionButton(host, "Story", "★ 스토리", Sun, Hex("#FFCC80"), 2, OnStoryPressed);
            _storyLabel = _storyBtn.GetComponentInChildren<Text>();
        }

        /// 둥근 직사각형 + 그림자 + 위쪽 밝은 그라데이션(두 톤). hitSlop 10px은 버튼 렉트를 사방 10px 키워 구현.
        private Button ActionButton(Transform parent, string name, string label, Color color, Color light, int column, Action onClick)
        {
            var hit = new GameObject(name + "Hit", typeof(RectTransform), typeof(Image), typeof(Button));
            hit.transform.SetParent(parent, false);
            var hrt = hit.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(column / 3f, 0.02f);
            hrt.anchorMax = new Vector2((column + 1) / 3f, 0.42f);
            hrt.offsetMin = new Vector2(column == 0 ? 2f : -4f, 0f);   // hitSlop: 버튼 본체(10px 안쪽)보다 10px 넓게
            hrt.offsetMax = new Vector2(column == 2 ? -2f : 4f, 0f);
            hit.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // hitSlop 영역(투명)
            var btn = hit.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Haptic(); onClick?.Invoke(); });

            var shadow = CoastUiArt.Panel(hit.transform, "Shadow", new Color(0f, 0f, 0f, 0.25f), 20);
            Stretch(shadow.rectTransform, 12f, 4f, -8f, -16f);
            var body = CoastUiArt.Panel(hit.transform, "Body", color, 20);
            Stretch(body.rectTransform, 10f, 10f, -10f, -10f);
            var gloss = CoastUiArt.Panel(body.transform, "Gloss", light, 16);
            gloss.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            gloss.rectTransform.anchorMax = new Vector2(1f, 1f);
            gloss.rectTransform.offsetMin = new Vector2(4f, 0f);
            gloss.rectTransform.offsetMax = new Vector2(-4f, -4f);
            gloss.color = new Color(light.r, light.g, light.b, 0.55f);
            var edge = CoastUiArt.Panel(body.transform, "Edge", new Color(1f, 1f, 1f, 0.35f), 20);
            Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
            edge.transform.SetAsFirstSibling();
            var fill2 = CoastUiArt.Panel(body.transform, "Fill", color, 18);
            Stretch(fill2.rectTransform, 2f, 2f, -2f, -2f);
            fill2.transform.SetSiblingIndex(1);
            var t = Label(body.transform, "Text", label, 24, Color.white);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            t.transform.SetAsLastSibling();
            return btn;
        }

        /// 버튼 탭 햅틱. 모바일에서만 진동 — 에디터/데스크톱은 무시.
        private static void Haptic()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        // ── 스케줄 시트 (하단 모달) ────────────────────────────────────

        private void BuildScheduleSheet()
        {
            _sheet = new GameObject("ScheduleSheet", typeof(RectTransform), typeof(Image)).gameObject;
            _sheet.transform.SetParent(_overlay, false);
            var dim = _sheet.GetComponent<Image>();
            dim.color = new Color(0.15f, 0.08f, 0.05f, 0.55f);
            dim.raycastTarget = true;
            var rt = _sheet.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            AddHit(_sheet, () => ToggleSheet(false));

            var panel = OrnatePanel(_sheet.transform, "Sheet", Gold, new Vector2(0f, 0f), new Vector2(1f, 0.66f), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Vector2(-CoastUiCanvas.HudPad, 0f));
            panel.raycastTarget = true;
            var host = panel.transform.Find("Inner") as RectTransform;
            _sheetTitle = Label(host, "Title", "이번 주 스케줄", 24, Navy);
            Place(_sheetTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 36f), new Vector2(0.5f, 1f));

            // 3 슬롯 카드 (가로)
            for (int i = 0; i < Timeline.PhasesPerWeek; i++)
            {
                int slot = i;
                var card = CoastUiArt.Panel(host, "Slot" + i, Ivory, 14);
                card.rectTransform.anchorMin = new Vector2(i / 3f, 1f); card.rectTransform.anchorMax = new Vector2((i + 1) / 3f, 1f);
                card.rectTransform.pivot = new Vector2(0.5f, 1f);
                card.rectTransform.offsetMin = new Vector2(i == 0 ? 10f : 4f, -124f); card.rectTransform.offsetMax = new Vector2(i == 2 ? -10f : -4f, -50f);
                card.raycastTarget = true;
                var edge = CoastUiArt.Panel(card.transform, "Edge", Gold, 14);
                Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
                edge.transform.SetAsFirstSibling();
                var fill = CoastUiArt.Panel(card.transform, "Fill", Ivory, 12);
                Stretch(fill.rectTransform, 3f, 3f, -3f, -3f);
                _slotFill[i] = fill;
                var num = Label(card.transform, "Num", (i + 1).ToString(), 24, Gold);
                Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(30f, 40f), new Vector2(0f, 0.5f));
                _slotGlyph[i] = Label(card.transform, "Glyph", "", 18, Red);
                Place(_slotGlyph[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(50f, 40f), new Vector2(0f, 0.5f));
                _slotName[i] = Label(card.transform, "Name", "비어 있음", 15, Ink);
                _slotName[i].alignment = TextAnchor.MiddleLeft;
                Place(_slotName[i].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                _slotName[i].rectTransform.offsetMin = new Vector2(92f, 0f);
                _slotName[i].rectTransform.offsetMax = new Vector2(-6f, 0f);
                AddHit(card.gameObject, () => OnSlotTapped(slot));
            }

            // 카테고리 탭
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
                var pill = CoastUiArt.CutePill(host, "Tab" + t.cat, t.color, 14, 3);
                pill.rectTransform.anchorMin = new Vector2(i / 4f, 1f); pill.rectTransform.anchorMax = new Vector2((i + 1) / 4f, 1f);
                pill.rectTransform.pivot = new Vector2(0.5f, 1f);
                pill.rectTransform.offsetMin = new Vector2(i == 0 ? 10f : 4f, -184f); pill.rectTransform.offsetMax = new Vector2(i == 3 ? -10f : -4f, -136f);
                var btn = pill.gameObject.AddComponent<Button>();
                pill.raycastTarget = true;
                btn.transition = Selectable.Transition.None;
                var cat = t.cat;
                btn.onClick.AddListener(() => { Haptic(); _tab = cat; RefreshCards(); });
                var lbl = Label(pill.transform, "Text", t.name, 19, Color.white);
                CoastUiArt.OutlineText(lbl, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                _tabButtons.Add(btn);
            }

            // 카드 영역 (세로 스크롤)
            var scrollGo = new GameObject("CardScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(host, false);
            var srt = scrollGo.GetComponent<RectTransform>();
            Stretch(srt, 8f, 76f, -8f, -192f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            _cardRow = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            _cardRow.SetParent(srt, false);
            _cardRow.anchorMin = new Vector2(0f, 1f); _cardRow.anchorMax = new Vector2(1f, 1f);
            _cardRow.pivot = new Vector2(0.5f, 1f);
            sr.content = _cardRow; sr.viewport = srt;

            BigButton(host, "Close", "닫기", new Color(0.55f, 0.50f, 0.48f), new Vector2(0.5f, 0f), new Vector2(-120f, 10f), new Vector2(220f, 58f), () => ToggleSheet(false));
            BigButton(host, "RunSheet", "실행", Mint, new Vector2(0.5f, 0f), new Vector2(120f, 10f), new Vector2(220f, 58f), () => { ToggleSheet(false); OnRunPressed(); });

            _sheet.SetActive(false);
        }

        private void ToggleSheet(bool on)
        {
            if (_sheet == null) return;
            if (on && _busy) return;
            _sheet.SetActive(on);
            if (on) { RefreshSlots(); RefreshCards(); }
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
            const float h = 112f, gap = 8f;
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                int col = i % 2, row = i / 2;
                var card = CoastUiArt.CutePill(_cardRow, "Card_" + d.id, CardColor(d), 14, 3);
                // 2열: 컨테이너 너비의 절반씩 (고정 폭이면 좁은 안전 영역에서 오른쪽으로 삐져나간다)
                card.rectTransform.anchorMin = new Vector2(col * 0.5f, 1f); card.rectTransform.anchorMax = new Vector2(col * 0.5f + 0.5f, 1f);
                card.rectTransform.pivot = new Vector2(0.5f, 1f);
                card.rectTransform.offsetMin = new Vector2(col == 0 ? 0f : gap * 0.5f, -row * (h + gap) - h);
                card.rectTransform.offsetMax = new Vector2(col == 1 ? 0f : -gap * 0.5f, -row * (h + gap));
                var btn = card.gameObject.AddComponent<Button>();
                card.raycastTarget = true;
                btn.transition = Selectable.Transition.None;
                var def = d;
                btn.onClick.AddListener(() => { Haptic(); OnCardTapped(def); });

                var glyph = Label(card.transform, "Glyph", d.glyph, 20, Color.white);
                CoastUiArt.OutlineText(glyph, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                Place(glyph.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(64f, 32f), new Vector2(0f, 1f));
                var name = Label(card.transform, "Name", d.displayName, 18, Color.white);
                name.alignment = TextAnchor.MiddleLeft;
                CoastUiArt.OutlineText(name, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 32f), new Vector2(0.5f, 1f));
                name.rectTransform.offsetMin = new Vector2(78f, -40f);
                name.rectTransform.offsetMax = new Vector2(-10f, -8f);
                var desc = Label(card.transform, "Desc", Describe(d, season), 13, Color.white);
                desc.alignment = TextAnchor.UpperLeft;
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.verticalOverflow = VerticalWrapMode.Truncate;
                Place(desc.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                desc.rectTransform.offsetMin = new Vector2(12f, 8f);
                desc.rectTransform.offsetMax = new Vector2(-10f, -44f);
            }
            int rowsN = (defs.Count + 1) / 2;
            _cardRow.sizeDelta = new Vector2(0f, rowsN * (h + gap));
            if (defs.Count == 0)
            {
                var none = Label(_cardRow, "None", "이 계절엔 할 수 있는 게 없어.", 16, Ink);
                Place(none.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
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

        // ── 로그 / 토스트 / 툴팁 ──────────────────────────────────────

        private void BuildLogPanel()
        {
            _logPanel = new GameObject("LogPanel", typeof(RectTransform), typeof(Image)).gameObject;
            _logPanel.transform.SetParent(_overlay, false);
            var dim = _logPanel.GetComponent<Image>();
            dim.color = new Color(0.15f, 0.08f, 0.05f, 0.55f);
            dim.raycastTarget = true;
            var rt = _logPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);

            var panel = OrnatePanel(_logPanel.transform, "Panel", Gold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(600f, 760f), anchoredSize: true, centered: true);
            var host = panel.transform.Find("Inner");
            _logTitle = Label(host, "Title", "", 26, Navy);
            Place(_logTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            // 프메식 활동 일러스트 (Resources/CoastRun/Sched_<id>.png, 4:3). 없으면 칸을 접는다.
            _logArtFrame = CoastUiArt.Panel(host, "ArtFrame", Gold, 14);
            Place(_logArtFrame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(544f, 410f), new Vector2(0.5f, 1f));
            _logArt = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _logArt.transform.SetParent(_logArtFrame.transform, false);
            Stretch(_logArt.rectTransform, 4f, 4f, -4f, -4f);
            _logArt.preserveAspect = true;
            _logArt.raycastTarget = false;
            _logBody = Label(host, "Body", "", 19, Ink);
            _logBody.alignment = TextAnchor.UpperLeft;
            _logBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(_logBody.rectTransform, 28f, 52f, -28f, -480f);
            _logHint = Label(host, "Hint", "화면을 터치하면 계속", 15, new Color(0.5f, 0.45f, 0.4f));
            Place(_logHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), new Vector2(0f, 30f), new Vector2(0.5f, 0f));
            _logPanel.SetActive(false);
        }

        private void BuildToast()
        {
            var pill = CoastUiArt.CutePill(_overlay, "Toast", WoodDark, 18, 4);
            Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(520f, 60f), new Vector2(0.5f, 0.5f));
            _toastText = Label(pill.transform, "Text", "", 20, Color.white);
            _toast = pill.gameObject;
            _toast.SetActive(false);
        }

        private void BuildTooltip()
        {
            var panel = OrnatePanel(_overlay, "Tooltip", Gold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 72f), anchoredSize: true, centered: true);
            _tooltip = panel.gameObject;
            _tooltipText = Label(panel.transform.Find("Inner"), "Text", "", 16, Navy);
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltip.SetActive(false);
        }

        private void ShowTooltip(StatRowView v, RectTransform row)
        {
            if (_tooltip == null || Save == null) return;
            _tooltipText.text = TooltipFor(v);
            _tooltip.SetActive(true);
            // 행 위쪽에 표시
            var rt = _tooltip.GetComponent<RectTransform>();
            Vector3 world = row.TransformPoint(new Vector3(0f, row.rect.yMax, 0f));
            rt.position = world + Vector3.up * 0.001f;
            rt.anchoredPosition += new Vector2(0f, 46f);
        }

        private void HideTooltip()
        {
            if (_tooltip != null) _tooltip.SetActive(false);
        }

        private string TooltipFor(StatRowView v)
        {
            var st = Save.stats;
            RunTuning.Configure(Save);
            switch (v.kind)
            {
                case StatKind.Stamina: return $"체력 {st.stamina} = 런닝 최대 HP {RunTuning.MaxHp:0} · 피격 -{RunTuning.HitDamage:0.#}";
                case StatKind.Agility: return $"순발력 {st.agility} = 피격 후 무적 {RunTuning.DashInvincible:0.0}초 · 경직 ×{RunTuning.HitFreezeMul:0.00}";
                case StatKind.Charm: return $"매력 {st.charm} = 대성공률 +{st.charm * ScheduleJudge.GreatCharmCoef:P1} · 니어미스 하트 ×{RunTuning.NearMissBonus:0.00}";
                case StatKind.Stress:
                    return st.Burnout ? $"스트레스 {st.stress} > 체력 {st.stamina} = 번아웃! 실패율 급증 · 휴식 필요"
                        : $"스트레스 {st.stress} = 성공률 -{ScheduleJudge.MildStressCoef * st.stress / Mathf.Max(1, st.stamina):P0} · 체력({st.stamina})을 넘으면 번아웃";
            }
            switch (v.key)
            {
                case "speed": return Save.runMode == RunMode.Skateboard ? "스케이트보드: 속도 ×1.3 · 코인 ×1.3 (고급)" : "러닝: 속도 ×1.0 · 코인 ×1.0";
                case "hp": return $"런닝 시작 HP {(RunTuning.BurnoutStart ? RunTuning.MaxHp * 0.7f : RunTuning.MaxHp):0} / {RunTuning.MaxHp:0}" + (RunTuning.BurnoutStart ? " (번아웃 -30%)" : "");
                case "hearts": { var rec = Save.CurrentChapter; return rec != null ? $"이번 챕터 ♥{Save.chapterHearts} / {rec.heartsTarget} · S급 컷 {Mathf.CeilToInt(rec.heartsTarget * ChapterGrading.S_Ratio)}" : ""; }
                case "luck": return $"대성공 확률 {ScheduleJudge.GreatBase + st.charm * ScheduleJudge.GreatCharmCoef:P1}" + (st.Burnout ? " (번아웃 ×0.25)" : "");
            }
            return "";
        }

        // ────────────────────────────────────────────────────────────────
        // Refresh
        // ────────────────────────────────────────────────────────────────

        private static readonly string[] Weekdays = { "월", "화", "수", "목", "금", "토", "일" };

        public void Refresh()
        {
            var s = Save;
            if (s == null) return;
            var season = Timeline.SeasonOf(s.week);
            var rec = s.CurrentChapter;

            // 주차 → 달력 느낌: 1주=봄 1월… 13주 단위 계절, 페이즈 = 요일
            string day = Weekdays[Mathf.Clamp(s.phaseIndex * 2, 0, 6)];
            _dateLabel.text = $"{s.week}주차 {day} · {Timeline.SeasonName(season)}";
            _moneyLabel.text = s.stats.money.ToString("N0") + "G";
            _levelLabel.text = _gm.IsRetry ? $"재도전 {s.chapter}" : $"CH {s.chapter}";
            var cond = Condition(s.stats);
            _condLabel.text = cond.label;
            if (_condHeart != null) _condHeart.color = cond.color;
            _heartsLabel.text = rec != null ? $"{s.chapterHearts} / {rec.heartsTarget}" : s.chapterHearts.ToString();

            ApplySeasonRoom(season);
            RefreshSlots();
            RefreshStats();
            RefreshCharacter();
            if (_sheet != null && _sheet.activeSelf) RefreshCards();
        }

        private (string label, Color color) Condition(PlayerStats st)
        {
            float r = st.stamina > 0 ? st.stress / (float)st.stamina : 2f;
            if (r >= 1f) return ("부상", new Color(0.5f, 0.5f, 0.55f));
            if (r >= 0.7f) return ("피로", new Color(0.85f, 0.55f, 0.25f));
            if (r >= 0.4f) return ("보통", new Color(0.95f, 0.75f, 0.30f));
            return ("최상", Red);
        }

        private void ApplySeasonRoom(SeasonKind season)
        {
            var art = ArtAssets.LoadTexture("UI_Raising_Room_" + season) ?? ArtAssets.LoadTexture("UI_Raising_Room");
            if (art != null)
            {
                _roomBg.sprite = CoastUiArt.AsSprite(art);
                _roomBg.color = Color.white;
                _roomBg.type = Image.Type.Simple;
                _roomBg.preserveAspect = false;
            }
            else
            {
                _roomBg.sprite = null;
                _roomBg.color = season == SeasonKind.Winter ? Hex("#8D7B74") : season == SeasonKind.Autumn ? Hex("#A8867A") : Hex("#A1887F");
            }
            Color glow = season == SeasonKind.Summer ? new Color(1f, 0.95f, 0.7f, 0.16f)
                : season == SeasonKind.Winter ? new Color(0.8f, 0.85f, 1f, 0.14f)
                : new Color(1f, 0.85f, 0.55f, 0.18f);
            _roomTint.color = glow;
        }

        private void RefreshSlots()
        {
            var s = Save;
            bool ready = true;
            for (int i = 0; i < Timeline.PhasesPerWeek; i++)
            {
                var def = ScheduleTable.Get(s.queuedSchedule != null && i < s.queuedSchedule.Length ? s.queuedSchedule[i] : null);
                bool done = i < s.phaseIndex;
                string name = done ? "완료" : def != null ? def.displayName : "비어 있음";
                if (_slotName[i] != null) _slotName[i].text = name;
                if (_slotGlyph[i] != null) _slotGlyph[i].text = def != null ? def.glyph : "";
                Color fillCol = done ? new Color(0.85f, 0.82f, 0.78f)
                    : def != null ? Color.Lerp(CardColor(def), Ivory, 0.45f)
                    : i == _selectedSlot ? GoldLight : Ivory;
                if (_slotFill[i] != null) _slotFill[i].color = fillCol;
                if (_slotChip[i] != null) _slotChip[i].text = $"{i + 1}  {(def != null && !done ? def.glyph + " " : "")}{name}";
                if (_slotChipBg[i] != null) _slotChipBg[i].color = fillCol;
                if (i >= s.phaseIndex && def == null) ready = false;
            }
            bool finished = !_gm.IsRetry && s.chapter >= Timeline.Chapters && s.CurrentChapter != null && s.CurrentChapter.cleared;
            if (finished) ready = false;
            if (_runButton != null) _runButton.interactable = ready && !_busy;
            if (_runLabel != null)
            {
                _runLabel.text = finished ? "재도전" : s.phaseIndex > 0 ? "이어서" : "실행";
                _runLabel.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
            if (_storyBtn != null) _storyBtn.interactable = !finished && !_busy;
            if (_sheetTitle != null)
                _sheetTitle.text = $"{s.week}주차 스케줄  ·  {Timeline.SeasonName(Timeline.SeasonOf(s.week))}" + (s.CurrentChapter != null ? $"  (챕터 {s.CurrentChapter.weekEnd - s.week + 1}주 남음)" : "");
        }

        private void RefreshStats()
        {
            if (_statList == null || Save == null) return;
            if (_statRows.Count == 0 || (_statTab == StatTab.Body) != (_statRows[0].kind == StatKind.Stamina))
                RebuildStatRows();
            var st = Save.stats;
            RunTuning.Configure(Save);
            foreach (var v in _statRows)
            {
                int value; int max = PlayerStats.StatMax;
                switch (v.kind)
                {
                    case StatKind.Stamina: value = st.stamina; break;
                    case StatKind.Agility: value = st.agility; break;
                    case StatKind.Charm: value = st.charm; break;
                    case StatKind.Stress: value = st.stress; break;
                    default:
                        switch (v.key)
                        {
                            case "speed": value = Mathf.RoundToInt(RunTuning.SpeedMul * 100f); max = 130; break;
                            case "hp": value = Mathf.RoundToInt(RunTuning.MaxHp); max = 200; break;
                            case "hearts": { var rec = Save.CurrentChapter; value = Save.chapterHearts; max = rec != null ? rec.heartsTarget : 41; break; }
                            default: value = Mathf.RoundToInt((ScheduleJudge.GreatBase + st.charm * ScheduleJudge.GreatCharmCoef) * 100f); max = 30; break;
                        }
                        break;
                }
                int filled = Mathf.Clamp(Mathf.RoundToInt(10f * value / Mathf.Max(1, max)), 0, 10);
                for (int b = 0; b < 10; b++)
                    v.blocks[b].color = b < filled ? (v.kind == StatKind.Stress ? Grape : Red) : RedEmpty;
                v.value.text = v.key == "speed" ? $"{value}%" : value.ToString();
                if (v.marker != null)
                {
                    float x = Mathf.Clamp01(st.stamina / (float)PlayerStats.StatMax);
                    v.marker.anchorMin = new Vector2(x, 0f);
                    v.marker.anchorMax = new Vector2(x, 1f);
                }
            }
        }

        private enum Mood { Happy, Normal, Tired, Great, Fail }

        private void RefreshCharacter(Mood? force = null)
        {
            var st = Save.stats;
            float ratio = st.stamina > 0 ? st.stress / (float)st.stamina : 2f;
            Mood mood = force ?? (ratio < 0.4f ? Mood.Happy : ratio < 0.7f ? Mood.Normal : Mood.Tired);

            string key = mood == Mood.Great ? "Happy" : mood == Mood.Fail ? "Tired" : mood.ToString();
            var tex = ArtAssets.LoadTexture("Raise_Girl_" + key) ?? ArtAssets.LoadTexture("Raise_Girl_Normal");
            if (tex == null)
                tex = ArtAssets.LoadTexture("GirlSkater_Back");
            if (tex != null)
            {
                var sprite = CoastUiArt.AsSprite(ChromaKeyed(tex));
                _charImage.sprite = sprite;
                _charImage.enabled = true;
                _charFace.text = "";
                if (_portraitFace != null) { _portraitFace.sprite = sprite; _portraitFace.enabled = true; }
            }
            else
            {
                _charImage.enabled = false;
                _charFace.text = mood == Mood.Happy || mood == Mood.Great ? "(^▽^)" : mood == Mood.Normal ? "(・ω・)" : "(>_<)";
            }
            // 피로 이하: 다크서클 / 부상(번아웃): 살짝 기울어진 자세
            bool tired = mood == Mood.Tired || mood == Mood.Fail;
            _darkCircles.gameObject.SetActive(tired && tex != null && !tex.name.Contains("Tired"));
            _charRoot.localRotation = Quaternion.Euler(0f, 0f, st.Burnout ? -4f : 0f);
            _charRoot.localScale = mood == Mood.Great ? Vector3.one * 1.04f : Vector3.one;

            if (force == null)
            {
                var season = Timeline.SeasonOf(Save.week);
                _bubble.text = st.Burnout ? "…몸이 안 따라줘. 오늘은 쉬어야 할 것 같아."
                    : mood == Mood.Tired ? "…좀 쉬고 싶어."
                    : mood == Mood.Happy ? (season == SeasonKind.Winter ? "눈 오면 송전탑에 가자." : "오늘도 송전탑이 잘 보여.")
                    : "라디오 주파수, 오늘은 맞을까.";
            }
        }

        private void OnCharacterTapped()
        {
            if (_busy || Save == null) return;
            // 의상/신발 변경은 후속 — 지금은 상태 한 줄 + 컨디션 설명
            var cond = Condition(Save.stats);
            Toast($"컨디션 {cond.label} · 스트레스 {Save.stats.stress} / 체력 {Save.stats.stamina}");
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
                for (int i = slot + 1; i < Timeline.PhasesPerWeek; i++)
                    _gm.SetQueued(i, ScheduleTable.StoryId);
            _gm.SetQueued(slot, def.id);
            _selectedSlot = -1;
            RefreshSlots();
        }

        /// [스토리] 버튼: 남은 칸을 스토리로 채우고 바로 실행.
        private void OnStoryPressed()
        {
            if (_busy || Save == null) return;
            Confirm("지금 스토리로 갈까?", "이번 주 남은 칸은 스토리로 채워져. 챕터가 끝나면 다음 챕터 첫 주로 넘어가.", () =>
            {
                for (int i = Save.phaseIndex; i < Timeline.PhasesPerWeek; i++)
                    _gm.SetQueued(i, ScheduleTable.StoryId);
                RefreshSlots();
                OnRunPressed();
            });
        }

        private void OnRunPressed()
        {
            if (_busy || Save == null) return;
            for (int i = Save.phaseIndex; i < Timeline.PhasesPerWeek; i++)
                if (ScheduleTable.Get(Save.queuedSchedule[i]) == null) { ToggleSheet(true); Toast("이번 주 스케줄을 먼저 채워줘."); return; }
            if (_sheet != null && _sheet.activeSelf) _sheet.SetActive(false);
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
                    _gm.ResolvePhase(i);
                    yield return ShowLog("스토리 돌입", "송전탑 가는 길로. 장애물을 피해 하트를 모으자.\n\n" +
                                          $"이번 챕터 목표 ♥{Save.CurrentChapter?.heartsTarget}  ·  지금 ♥{Save.chapterHearts}", 0.6f, ScheduleTable.StoryId);
                    _gm.StartStoryRun();
                    yield break;
                }

                var result = _gm.ResolvePhase(i);
                if (!result.HasValue) continue;
                var r = result.Value;
                RefreshSlots();
                RefreshCharacter(r.outcome == Outcome.GreatSuccess ? Mood.Great : r.outcome == Outcome.Fail ? Mood.Fail : (Mood?)null);
                _bubble.text = r.outcome == Outcome.GreatSuccess ? "해냈다!" : r.outcome == Outcome.Fail ? "으으… 망했어." : "그럭저럭.";
                yield return ShowLogTyped($"{i + 1}페이즈 · {r.def.displayName}", r.logLines, r.outcome, r.def.id);
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

        /// 활동 일러스트를 로그 패널에 건다. 없으면 이미지 칸을 접고 본문을 위로 올린다.
        private void SetLogArt(string scheduleId)
        {
            var tex = string.IsNullOrEmpty(scheduleId) ? null : ArtAssets.LoadTexture("Sched_" + scheduleId);
            bool has = tex != null;
            _logArtFrame.gameObject.SetActive(has);
            if (has) _logArt.sprite = CoastUiArt.AsSprite(tex);
            _logBody.rectTransform.offsetMax = new Vector2(-28f, has ? -480f : -64f);
        }

        private IEnumerator ShowLogTyped(string title, string[] lines, Outcome outcome, string scheduleId = null)
        {
            SetLogArt(scheduleId);
            _logPanel.SetActive(true);
            _logTitle.text = title;
            _logTitle.color = outcome == Outcome.GreatSuccess ? Hex("#B8860B") : outcome == Outcome.Fail ? Red : Navy;
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

        private IEnumerator ShowLog(string title, string body, float minSeconds, string scheduleId = null)
        {
            SetLogArt(scheduleId);
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

        /// 골드 테두리 + 아이보리(또는 지정색) 안쪽 + 얇은 내부 섀도우 + 네 모서리 장식. 자식은 "Inner"에 붙인다.
        private static Image OrnatePanel(Transform parent, string name, Color edge, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax,
            bool anchoredSize = false, bool centered = false, Color? inner = null)
        {
            var outer = CoastUiArt.Panel(parent, name, edge, 16);
            var rt = outer.rectTransform;
            if (anchoredSize)
            {
                rt.anchorMin = aMin; rt.anchorMax = aMax;
                if (centered)
                {
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = offMin;
                    rt.sizeDelta = offMax;
                }
                else if (aMin == aMax)
                {
                    // offMin = 좌상단 위치, offMax = 우하단 위치 (앵커 기준, 우/하는 음수)
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = offMin;
                    rt.sizeDelta = new Vector2(offMax.x - offMin.x, offMin.y - offMax.y);
                }
                else
                {
                    rt.offsetMin = new Vector2(offMin.x, offMax.y);
                    rt.offsetMax = new Vector2(offMax.x, offMin.y);
                }
            }
            else
            {
                rt.anchorMin = aMin; rt.anchorMax = aMax;
                rt.offsetMin = offMin; rt.offsetMax = offMax;
            }
            outer.raycastTarget = false;

            Color fill = inner ?? (edge == WoodDark ? Wood : Ivory);
            var body = CoastUiArt.Panel(outer.transform, "Inner", fill, 13);
            Stretch(body.rectTransform, 4f, 4f, -4f, -4f);
            body.raycastTarget = false;
            // 내부 섀도우: 위/왼쪽에 얇은 어두운 띠
            var shTop = CoastHudLayout.MakeImage(body.transform, "ShadowTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -10f), new Vector2(-6f, 0f), new Color(0f, 0f, 0f, 0.10f));
            shTop.sprite = CoastUiArt.RoundedRect(6); shTop.type = Image.Type.Sliced;
            var shLeft = CoastHudLayout.MakeImage(body.transform, "ShadowLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 6f), new Vector2(8f, -6f), new Color(0f, 0f, 0f, 0.07f));
            shLeft.sprite = CoastUiArt.RoundedRect(6); shLeft.type = Image.Type.Sliced;
            // 골드 코너 장식(마름모 + 크림 점)
            foreach (var c in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) })
            {
                var d = CoastHudLayout.MakeImage(outer.transform, "Corner", c, c, new Vector2(-9f, -9f), new Vector2(9f, 9f), edge == Gold ? GoldLight : Gold);
                d.sprite = CoastUiArt.RoundedRect(3); d.type = Image.Type.Sliced;
                d.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                d.rectTransform.anchoredPosition = new Vector2(c.x == 0f ? 6f : -6f, c.y == 0f ? 6f : -6f);
                var dot = CoastHudLayout.MakeImage(d.transform, "Dot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-3f, -3f), new Vector2(3f, 3f), Ivory);
                dot.sprite = CoastUiArt.RoundedRect(3); dot.type = Image.Type.Sliced;
            }
            return outer;
        }

        /// HUD 알약: 우드 배경 + 골드 테두리 + (아이콘) + 텍스트. 반환은 텍스트(부모가 알약).
        private Text Pill(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 pos, Vector2 sizeDelta, Color fill, Color edge,
            Texture2D iconTex = null, Sprite iconSprite = null)
        {
            var outer = CoastUiArt.Panel(parent, name, edge, 14);
            Place(outer.rectTransform, anchor, anchor, pos, sizeDelta, anchor);
            outer.raycastTarget = true;
            var inner = CoastUiArt.Panel(outer.transform, "Fill", fill, 12);
            Stretch(inner.rectTransform, 2f, 2f, -2f, -2f);
            float textLeft = 10f;
            var sprite = iconSprite ?? (iconTex != null ? CoastUiArt.AsSprite(iconTex) : null);
            if (sprite != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(outer.transform, false);
                ic.sprite = sprite; ic.preserveAspect = true; ic.raycastTarget = false;
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f));
                textLeft = 44f;
            }
            var t = Label(outer.transform, "Text", text, size, GoldLight);
            t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            t.rectTransform.offsetMax = new Vector2(-8f, 0f);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            return t;
        }

        private static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(r, t);
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

        /// 이미지에 Button을 붙여 탭을 받는다(터치 영역은 부모 렉트 그대로, 최소 48dp 유지).
        private static void AddHit(GameObject go, Action onClick)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Haptic(); onClick?.Invoke(); });
        }

        private Button SmallButton(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, float width, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 12, 3);
            Place(pill.rectTransform, anchor, anchor, pos, new Vector2(width, 44f), anchor);
            var btn = pill.gameObject.AddComponent<Button>();
            pill.raycastTarget = true;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Haptic(); onClick?.Invoke(); });
            var t = Label(pill.transform, "Text", label, 16, Color.white);
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
            btn.onClick.AddListener(() => { Haptic(); onClick?.Invoke(); });
            var t = Label(pill.transform, "Text", label, 21, Color.white);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return btn;
        }

        private GameObject Modal(string name, float width, float height, out RectTransform panel)
        {
            var dimGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(_overlay, false);
            var dim = dimGo.GetComponent<Image>();
            dim.color = new Color(0.15f, 0.08f, 0.05f, 0.6f);
            dim.raycastTarget = true;
            var rt = dimGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            rt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);

            var p = OrnatePanel(dimGo.transform, "Panel", Gold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), anchoredSize: true, centered: true);
            p.raycastTarget = true;
            panel = p.transform.Find("Inner") as RectTransform;
            return dimGo;
        }

        private void Confirm(string title, string body, Action onYes)
        {
            var modal = Modal("Confirm", 540f, 300f, out var panel);
            var t = Label(panel, "Title", title, 24, Navy);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 40f), new Vector2(0.5f, 1f));
            var b = Label(panel, "Body", body, 18, Ink);
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(b.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.8f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            b.rectTransform.offsetMin = new Vector2(24f, 0f); b.rectTransform.offsetMax = new Vector2(-24f, 0f);
            BigButton(panel, "Yes", "응", Coral, new Vector2(0.5f, 0f), new Vector2(110f, 18f), new Vector2(190f, 56f), () => { _modalPrimary = null; Destroy(modal); onYes?.Invoke(); });
            BigButton(panel, "No", "아니", new Color(0.55f, 0.50f, 0.48f), new Vector2(0.5f, 0f), new Vector2(-110f, 18f), new Vector2(190f, 56f), () => { _modalPrimary = null; Destroy(modal); });
            _modalPrimary = () => { Destroy(modal); onYes?.Invoke(); };
        }

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
                float d = Mathf.Max(0f, (c.r - c.g) + (c.b - c.g)) / 255f;
                float key = Mathf.Clamp01((d - 0.9f) / 0.5f);
                if (key > 0f) anyKey = true;
                c.a = (byte)Mathf.RoundToInt(255f * (1f - key));
                px[i] = c;
            }
            if (!anyKey) { UnityEngine.Object.Destroy(copy); KeyedCache[src] = src; return src; }
            copy.SetPixels32(px);
            copy.Apply(false, false);
            copy.filterMode = FilterMode.Bilinear;
            copy.name = src.name + "_keyed";
            KeyedCache[src] = copy;
            return copy;
        }

        /// 가로 스와이프 → 탭 전환 (세로 ScrollRect와 공존: 가로 성분이 크면 스와이프로 본다).
        private class SwipeTabs : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
        {
            public Action<int> OnSwipe;
            private Vector2 _start;
            public void OnBeginDrag(PointerEventData e) => _start = e.position;
            public void OnDrag(PointerEventData e) { }
            public void OnEndDrag(PointerEventData e)
            {
                Vector2 d = e.position - _start;
                if (Mathf.Abs(d.x) > 80f && Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.5f)
                    OnSwipe?.Invoke(d.x < 0 ? -1 : 1);
            }
        }

        /// 롱프레스(0.45 s) — 스탯 행 툴팁.
        private class LongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public Action OnLongPress;
            public Action OnRelease;
            private float _downAt = -1f;
            private bool _fired;
            public void OnPointerDown(PointerEventData e) { _downAt = Time.unscaledTime; _fired = false; }
            public void OnPointerUp(PointerEventData e) { _downAt = -1f; if (_fired) OnRelease?.Invoke(); }
            public void OnPointerExit(PointerEventData e) { _downAt = -1f; if (_fired) OnRelease?.Invoke(); }
            private void Update()
            {
                if (_downAt < 0f || _fired) return;
                if (Time.unscaledTime - _downAt >= 0.45f) { _fired = true; OnLongPress?.Invoke(); }
            }
        }
    }
}
