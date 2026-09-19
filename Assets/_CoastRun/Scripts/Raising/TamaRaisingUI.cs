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
    ///   행동 3번 = 한 주(턴), 「다음 턴」 버튼(NextTurn)을 눌러야 넘어간다(55차-2; 행동이 남았으면 두 번 눌러 확인) → 생활 결산·「일주일이 지났다」(WeekPassUI) → 다음 턴. 챕터 마감 주였으면 다음 턴 시작에
    ///   이야기(리더) → 대회(러닝, StoryContest) — 55차: 컷씬은 러닝과 무관, 대회를 깨야 주차가 넘어간다.
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
        private Text _bubble, _weekLabel, _moneyLabel, _gateLabel, _autoLabel, _actionsLeft, _levelLabel, _lifeLabel;
        private Text _goalRibbon;
        private Image _bubbleBg, _staminaFill, _energyFill, _gateMark, _hpFill, _stressFill;
        private RectTransform _stressTick;   // 74차: 스트레스 게이지의 번아웃 한계선
        private Image _goalRibbonBg;
        private Text _gateFlag;
        private Text _staminaTxt, _energyTxt;
        private Image _lifeEdge, _lifeBand; private Text _lifeIcon;   // 63차: 생활 경고 띠
        private readonly Button[] _actBtn = new Button[3];
        private readonly Text[] _actDots = new Text[3];
        private readonly Image[] _actRing = new Image[3]; private readonly Text[] _actCheck = new Text[3];   // 60차: 이번 주 행동 3칸(큰 동그라미)
        private GameObject _cardPickOverlay, _eventOverlay;
        private bool _busy, _auto;
        private float _hop, _autoTimer, _bubbleUntil;
        private int _rubBudget = RubBudgetPerWeek;   // 이번 주 쓰다듬기로 내릴 수 있는 스트레스
        /// 74차: 공짜 회복이 주 10이나 되어 스트레스가 쌓이지 않았다 → 3으로. 애정 표현은 남기고 양만 줄인다.
        private const int RubBudgetPerWeek = 3;
        private float _rubDist;
        private int _lastAutoPick;
        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        private static readonly Color Pink = new Color(0.93f, 0.22f, 0.52f);

        public void Bind(GameManager gm)
        {
            _gm = gm;
            Build();
            LevelSystem.FlushPending();   // 53차: 세이브 없이 K-POP 에서 모은 경험치 합치기
            // 135차(사용자): 자동은 끌 때까지 계속 — 세이브(autoMode)에서 복원(대회·이야기 다녀와도 유지)
            if (Save != null && Save.autoMode) { _auto = true; _autoTimer = -1.5f; RefreshAuto(); }
            Refresh();
            // 105차(P2-3): 2회차 계승 안내 1회
            if (Save != null && Save.playthrough >= 2 && !Save.inheritedShown)
            {
                Save.inheritedShown = true; _gm.Persist();
                CoastToast.Show(Loc.T($"{Save.playthrough}회차 — 지난 회차 스탯 10%·숙련 절반을 물려받았어", $"Playthrough {Save.playthrough} — inherited 10% stats & half mastery"));
            }
            ShowBubble(Loc.T("오늘도 힘내자!", "Let's do our best today!"), 2.5f);
            _gm.OnSaveChanged -= OnSaveChanged; _gm.OnSaveChanged += OnSaveChanged;
            // 55차: 지난 턴이 챕터 마지막 주로 끝났으면(앱을 껐다 켰어도) 이번 턴 시작에 컷씬·대회.
            // 67차-8(사용자): 들어오자마자 팝업을 띄우지 않는다 — 동그라미 3개가 찬 상태로 보여 주고 「다음 턴」을 눌러야 이야기·대회가 시작.
            if (Save != null && Save.boundaryPending) ShowBubble(BoundaryHint(), 4f);
            // 72차(사용자): 육성 모드를 **처음 시작할 때** 바로 오프닝(1장 첫 「다음 턴」이 아니라 들어오자마자).
            if (Save != null && Save.chapter == 1 && !Save.prologueSeen && !Save.boundaryPending) StartCoroutine(OpeningFirst());
            else if (PlayerPrefs.GetInt(RaisingTutorial.PrefKey, 0) == 0)
                StartCoroutine(TamaTutorial());
        }

        private IEnumerator TamaTutorial()
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            yield return null;
            bool done = false;
            RaisingTutorial.Open(_root, RaisingTutorial.TamaSteps(), () => done = true);
            while (!done) yield return null;
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
        }

        private IEnumerator OpeningFirst()
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            yield return null;
            TitleAudio.StopMenuGlobal();   // 육성 BGM 잔향·더블 재생 방지(모바일)
            bool done = false;
            OpeningCinematic.Play(() => done = true);
            while (!done) yield return null;
            if (Save != null) { Save.prologueSeen = true; _gm.Persist(); }
            TitleAudio.PlayRaising();   // 오프닝 끝 → 스토리 모드 BGM
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
            ShowBubble(Loc.T("스무 살 생일까지 1년. 오늘부터 시작이야.", "One year to my 20th birthday. It starts today."), 3.5f);
            if (PlayerPrefs.GetInt(RaisingTutorial.PrefKey, 0) == 0)
                StartCoroutine(TamaTutorial());
        }

        private string BoundaryHint()
        {
            return Loc.T("이번주는 끝", "Week's over");
        }

        private IEnumerator ResumeBoundary()
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            yield return new WaitForSecondsRealtime(0.8f);
            yield return BoundaryRoutine();
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
        }

        private void OnDestroy() { if (_gm != null) _gm.OnSaveChanged -= OnSaveChanged; }
        private void OnSaveChanged(SaveData s) { if (this != null) Refresh(); }

        /// RaisingSceneDriver 호환: 타임라인 대신 현재 챕터 안내 말풍선.
        public void OpenTimeline()
        {
            if (Save == null) return;
            ShowBubble(Loc.T($"챕터 {Save.chapter} · {ChapterLocation.Get(Save.chapter).Name}", $"Chapter {Save.chapter} · {ChapterLocation.Get(Save.chapter).Name}"), 3f);
        }

        /// RaisingSceneDriver 호환: 돌발 이벤트 — A/B 선택 후 스탯 적용.
        /// 108차: Dev 메뉴용 — 카드 선택·이벤트 오버레이 닫기(코루틴은 취소 플래그로 끝냄).
        public void DevCloseOverlays()
        {
            if (_cardPickOverlay != null) { Destroy(_cardPickOverlay); _cardPickOverlay = null; }
            if (_eventOverlay != null) { Destroy(_eventOverlay); _eventOverlay = null; }
            StopAllCoroutines(); _busy = false;
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
        }
        public void ShowEvent(RandomEventDef ev)
        {
            if (ev == null) return;
            StartCoroutine(ShowEventWhenFree(ev));
        }
        /// 135차(사용자 「미니게임과 이벤트가 동시에 뜬다」): 미니게임·축제·다른 루틴이 떠 있으면 끝날 때까지 기다렸다가 돌발 카드를 띄운다.
        private IEnumerator ShowEventWhenFree(RandomEventDef ev)
        {
            float w = 0f;
            while ((_busy || ChapterMissionUI.IsOpen || FestivalUI.IsOpen || ContestIntroUI.IsOpen) && w < 120f) { w += Time.unscaledDeltaTime; yield return null; }
            if (ChapterMissionUI.IsOpen || FestivalUI.IsOpen) yield break;   // 놀이가 아직이면 이번 돌발은 접는다
            yield return EventChoiceRoutine(ev);
        }
        /// 109차: Dev 메뉴용 — 카드 고르기 오버레이(idx 0 쉼 / 1 놀기 / 2 알바)를 바로 연다.
        public void DevOpenPick(int idx)
        {
            if (Save == null || _busy) return;
            var list = BuildChoices(idx, Timeline.SeasonOf(Save.week));
            if (list.Count == 0) return;
            StartCoroutine(CardPickRoutine(idx, list));
        }

        /// Legacy: already-applied result (toast only). Prefer ShowEvent(RandomEventDef).
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
            // 67차-7(사용자: 갤럭시 S25 울트라 등 19.5:9~22:9 폰): 이 화면 배치는 인셋 664×1224(9:16) 기준 절대 좌표라 좁은 인셋(≈596)에선
            //   동그라미 3개가 장보기 버튼을 덮고 아래 버튼 줄이 잘렸다 → 「Fit」 상자를 두고 화면에 맞춰 통째로 축소(비율 유지).
            // 74차(사용자: 갤럭시 16:9에서 상하 잘림): 전엔 가로만 재서 9:16보다 짧은 비율(16:9 게임뷰·태블릿·폴더블)에선
            //   배율이 1로 남아 주차 알약과 「다음 턴」 줄이 화면 밖으로 나갔다 → 세로도 같이 재는 CoastUiDesignFit 으로.
            //   매 프레임 갱신이라 에디터에서 게임뷰 크기를 바꿔도 즉시 맞춰진다.
            _root = CoastUiCanvas.MakeFitBox(_root) ?? _root;

            // ── 상단 HUD: 주차·계절 / 챕터 / 돈·하트 / 홈 ──
            var wk = CoastUiArt.CutePill(_root, "Week", new Color(0.10f, 0.13f, 0.30f, 0.92f), 18, 3);
            Anchor(wk.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(CoastRun.Village.VillageHub.Enabled ? 196f : 300f, 60f));   // 137차: 오른쪽에 「마을」 알약 자리
            // 53차(사용자): 주차 아래 [상태창] [마이룸] 세로 버튼 — 레벨 배지가 상태 버튼에
            // 60차(사용자 시안): 주차 알약에 ✦, 왼쪽 버튼 3개는 아이콘 + 글자
            Sparkle(wk.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -12f), 14); Sparkle(wk.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 10f), 10);
            // 135차(사용자 시안): 둘째 줄 = 둥근 남색 아이콘 6개(도움·뽑기·Lv·마이룸·상점·가방, Icon_Top_*) + 글자 아래 — 오른쪽은 행동 ○○○
            Image TopIcon(string goName, string sprite, string label, float x, Action onClick, out Text labelT)
            {
                var img = new GameObject(goName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.transform.SetParent(_root, false);
                var sp = ArtAssets.LoadTexture(sprite);
                if (sp != null) { img.sprite = CoastUiArt.AsSprite(sp, 100f); img.preserveAspect = true; }
                else img.color = new Color(0.18f, 0.24f, 0.55f);
                Anchor(img.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -114f), new Vector2(64f, 64f)); img.raycastTarget = true;
                labelT = CoastHudLayout.MakeText(img.rectTransform, "T", label, 12, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-14f, -14f), new Vector2(14f, 8f));
                labelT.color = Color.white; labelT.fontStyle = FontStyle.Bold; labelT.raycastTarget = false; CoastUiArt.OutlineText(labelT, new Color(0.05f, 0.08f, 0.25f, 0.95f), 1.6f);
                var b = img.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); onClick?.Invoke(); });
                return img;
            }
            const float IcoX0 = 46f, IcoDx = 70f;
            var tutBtn = TopIcon("TutorialBtn", "Icon_Top_Help", Loc.T("도움", "Help"), IcoX0, () => StartCoroutine(TamaTutorial()), out _);
            // 131차/135차: ★ 별빛 캡슐(이벤트 뽑기) — 도움과 Lv 사이. 무료·뽑기권이 있으면 배지.
            var gachaBtn = TopIcon("GachaBtn", "Icon_Top_Gacha", Loc.T("뽑기", "Gacha"), IcoX0 + IcoDx, () => { _busy = true; StarGachaUI.Open(_gm, () => { _busy = false; Refresh(); }); }, out _);
            _gachaBadge = CoastUiArt.Panel(gachaBtn.rectTransform, "Badge", new Color(1f, 0.85f, 0.25f), 12); _gachaBadge.raycastTarget = false;
            Anchor(_gachaBadge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(26f, 26f));
            _gachaBadgeT = CoastHudLayout.MakeText(_gachaBadge.rectTransform, "T", "!", 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _gachaBadgeT.color = new Color(0.45f, 0.26f, 0.06f); _gachaBadgeT.fontStyle = FontStyle.Bold; _gachaBadgeT.raycastTarget = false;
            var stBtn = TopIcon("StatusBtn", "Icon_Top_Lv", "Lv1", IcoX0 + IcoDx * 2f, () => StatusUI.Open(_gm, Refresh), out _levelLabel);
            var roomBtn = TopIcon("RoomBtn", "Icon_Top_Room", Loc.T("마이룸", "My Room"), IcoX0 + IcoDx * 3f, OpenRoom, out _);
            var shopBtn = TopIcon("ShopBtn", "Icon_Top_Shop", Loc.T("상점", "Shop"), IcoX0 + IcoDx * 4f, () => ShopUI.Open(_gm, 0, Refresh), out _);
            var bagBtn = TopIcon("BagBtn", "Icon_Top_Bag", Loc.T("가방", "Bag"), IcoX0 + IcoDx * 5f, () => InventoryUI.Open(_gm, Refresh), out _);
            // 63차(시안): 생활 경고는 카드 위 **가로 띠**
            _lifeEdge = CoastUiArt.CutePill(_root, "LifeEdge", new Color(1f, 0.85f, 0.20f), 20, 3);
            Anchor(_lifeEdge.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 266f), new Vector2(596f, 86f)); _lifeEdge.raycastTarget = false;   // 65차: 띠 키움(글자 크게)
            _lifeBand = CoastUiArt.Panel(_lifeEdge.transform, "Band", new Color(0.86f, 0.14f, 0.20f), 16); _lifeBand.raycastTarget = false;
            Anchor(_lifeBand.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); _lifeBand.rectTransform.offsetMin = new Vector2(5f, 5f); _lifeBand.rectTransform.offsetMax = new Vector2(-5f, -5f);
            _lifeIcon = CoastHudLayout.MakeText(_lifeBand.rectTransform, "W", "⚠", 36, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(62f, 0f));
            _lifeIcon.color = new Color(1f, 0.88f, 0.25f); _lifeIcon.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_lifeIcon, new Color(0f, 0f, 0f, 0.5f), 1.4f);
            _lifeLabel = CoastHudLayout.MakeText(_lifeBand.rectTransform, "T", "", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-16f, 0f));
            _lifeLabel.color = Color.white; _lifeLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_lifeLabel, new Color(0f, 0f, 0f, 0.45f), 1.4f);
            _lifeLabel.resizeTextForBestFit = true; _lifeLabel.resizeTextMinSize = 10; _lifeLabel.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            _lifeLabel.horizontalOverflow = HorizontalWrapMode.Wrap; _lifeLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _lifeLabel.resizeTextForBestFit = true; _lifeLabel.resizeTextMinSize = 10; _lifeLabel.resizeTextMaxSize = CoastHudLayout.Scaled(20);   // 66차: 상자 안 한 줄에 들어가게(최대 20, 넘치면 줄어듦)
            _weekLabel = CoastHudLayout.MakeText(wk.rectTransform, "T", "", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-10f, 0f));
            _weekLabel.color = Color.white; CoastUiArt.OutlineText(_weekLabel, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            // 105차(재미요소 P1-4): 주차 알약을 누르면 1년 달력(대회·컷씬·축제 위치)
            wk.raycastTarget = true;
            var wkb = wk.gameObject.AddComponent<Button>(); wkb.transition = Selectable.Transition.None;
            wkb.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); CalendarUI.Open(Save, Refresh); });
            // 목표 리본: 짧은 종류만(대회/미니게임) — 상세는 상태창
            _goalRibbonBg = CoastUiArt.CutePill(_root, "GoalRibbon", new Color(0.18f, 0.22f, 0.48f, 0.94f), 14, 3);
            var grt = _goalRibbonBg.rectTransform;
            grt.anchorMin = new Vector2(0f, 1f); grt.anchorMax = new Vector2(1f, 1f); grt.pivot = new Vector2(0.5f, 1f);
            // 109차(사용자: 「2주 뒤 대회 글자가 상자보다 튀어나왔다」): 상자를 가로 400 → 680, 세로 38 → 48 로 키우고 글자는 상자 안에서 줄바꿈(넘치지 않게).
            grt.offsetMin = new Vector2(20f, -116f); grt.offsetMax = new Vector2(-20f, -68f);
            _goalRibbonBg.raycastTarget = true;
            _goalRibbon = CoastHudLayout.MakeText(_goalRibbonBg.rectTransform, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, -2f));
            _goalRibbon.color = new Color(1f, 0.96f, 0.82f); _goalRibbon.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(_goalRibbon, new Color(0f, 0f, 0f, 0.55f), 1.6f);
            _goalRibbon.resizeTextForBestFit = true; _goalRibbon.resizeTextMinSize = 13; _goalRibbon.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            _goalRibbon.horizontalOverflow = HorizontalWrapMode.Wrap;
            _goalRibbon.verticalOverflow = VerticalWrapMode.Truncate;
            _goalRibbon.raycastTarget = false;
            var grb = _goalRibbonBg.gameObject.AddComponent<Button>(); grb.transition = Selectable.Transition.None;
            grb.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); StatusUI.Open(_gm, Refresh); });
            // 골드 박스 — 러닝 CoinPill 과 동일: 남색 알약 + 노란 숫자 + 코인 아이콘
            var money = CoastUiArt.CutePill(_root, "Money", RunHudChrome.PillNavy, 18, 3);
            Anchor(money.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-146f, -6f), new Vector2(200f, 60f));
            money.raycastTarget = false;
            var coinIcon = ArtAssets.LoadTexture("Icon_Coin");
            if (coinIcon != null)
            {
                var ci = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ci.transform.SetParent(money.transform, false);
                ci.sprite = CoastUiArt.AsSprite(coinIcon, 100f); ci.preserveAspect = true; ci.raycastTarget = false;
                Anchor(ci.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(40f, 40f));
                ci.rectTransform.pivot = new Vector2(0f, 0.5f);
            }
            _moneyLabel = CoastHudLayout.MakeText(money.rectTransform, "T", "", 22, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(48f, 0f), new Vector2(-14f, 0f));
            _moneyLabel.color = RunHudChrome.ScoreYellow; _moneyLabel.fontStyle = FontStyle.Bold; _moneyLabel.alignment = TextAnchor.MiddleCenter;
            CoastUiArt.OutlineText(_moneyLabel, new Color(0.05f, 0.07f, 0.18f, 0.9f), 2f);
            _moneyLabel.resizeTextForBestFit = true; _moneyLabel.resizeTextMinSize = 10; _moneyLabel.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            // 60차: 오른쪽 위 큰 동그라미 3개 = 이번 주 행동(한 번 하면 초록 ✓)
            for (int i = 0; i < 3; i++)
            {
                // 67차-7(사용자: 폰에서 채움이 동그라미 밖으로 삐져나옴): 9-slice 반지름(44)이 지름의 절반(31)보다 커서 모서리 조각이 겹쳐 그려졌다 → 반지름 = 지름/2
                var ring = CoastUiArt.Panel(_root, "ActRing" + i, new Color(0.62f, 0.64f, 0.70f, 0.95f), 31);
                Anchor(ring.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-176f + i * 72f, -148f), new Vector2(62f, 62f)); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f); ring.raycastTarget = false;
                var inner = CoastUiArt.Panel(ring.transform, "In", new Color(0.30f, 0.32f, 0.40f, 0.55f), 25); inner.raycastTarget = false;
                Anchor(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f)); inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var ck = CoastHudLayout.MakeText(ring.rectTransform, "Ck", "✓", 32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                ck.color = Color.white; ck.fontStyle = FontStyle.Bold; ck.raycastTarget = false; CoastUiArt.OutlineText(ck, new Color(0f, 0f, 0f, 0.3f), 1.5f);
                _actRing[i] = ring; _actCheck[i] = ck;
            }
            var home = CoastUiArt.GlossyPill(_root, "Home", new Color(0.25f, 0.55f, 0.95f), 18, 6);
            Anchor(home.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(134f, 60f)); home.raycastTarget = true;   // 73차: 펫 버튼 자리까지 길게(64 → 134)
            var homeIcon = CoastUiArt.Art("Icon_Home");
            if (homeIcon != null)
            {
                var hi = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                hi.transform.SetParent(home.transform, false); hi.sprite = homeIcon; hi.preserveAspect = true; hi.raycastTarget = false;
                Anchor(hi.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 2f), new Vector2(34f, 34f)); hi.rectTransform.pivot = new Vector2(0f, 0.5f);
            }
            var homeT = CoastHudLayout.MakeText(home.rectTransform, "T", Loc.T("홈", "Home"), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(52f, 3f), new Vector2(-8f, 0f));
            homeT.color = Color.white; homeT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(homeT, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            var hb = home.gameObject.AddComponent<Button>(); hb.transition = Selectable.Transition.None;
            hb.onClick.AddListener(() => { if (_busy) return; _auto = false; RefreshAuto(); _gm.Persist(); _gm.ToTitle(); });   // 137차(사용자): 홈 = 메인화면
            // 137차: 「마을」 알약(주차 알약 오른쪽) — 이 화면은 우리집 안, 나가면 바닷가 마을
            if (CoastRun.Village.VillageHub.Enabled)
            {
                var vil = CoastUiArt.GlossyPill(_root, "Village", new Color(0.30f, 0.70f, 0.55f), 18, 6);
                Anchor(vil.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(208f, -6f), new Vector2(112f, 60f)); vil.raycastTarget = true;
                var vt = CoastHudLayout.MakeText(vil.rectTransform, "T", Loc.T("◀ 마을", "◀ Village"), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 3f), new Vector2(-4f, 0f));
                vt.color = Color.white; vt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(vt, new Color(0f, 0f, 0f, 0.35f), 1.2f);
                vt.resizeTextForBestFit = true; vt.resizeTextMinSize = 12; vt.resizeTextMaxSize = CoastHudLayout.Scaled(20);
                var vb = vil.gameObject.AddComponent<Button>(); vb.transition = Selectable.Transition.None;
                vb.onClick.AddListener(() => { if (_busy) return; _auto = false; RefreshAuto(); _gm.ToVillage(); });
            }

            // ── 무대: 하늘이 + 말풍선 ──
            var girlGo = new GameObject("Girl", typeof(RectTransform), typeof(Image), typeof(TouchRelay));
            girlGo.transform.SetParent(_root, false);
            _girl = girlGo.GetComponent<Image>(); _girl.preserveAspect = true; _girl.raycastTarget = true;
            _girlRt = girlGo.GetComponent<RectTransform>();
            Anchor(_girlRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 372f), new Vector2(470f, 570f));
            _girlRt.pivot = new Vector2(0.5f, 0f);
            var relay = girlGo.GetComponent<TouchRelay>(); relay.ui = this;
            // 56차(사용자): 말풍선은 오른쪽(하늘이 머리 옆)에 꼬리 달린 풍선으로 — 위쪽 버튼·생활 알약과 안 겹치게
            _bubbleBg = CoastUiArt.CutePill(_root, "Bubble", Color.white, 20, 3);
            Anchor(_bubbleBg.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 880f), new Vector2(320f, 92f)); _bubbleBg.raycastTarget = false;
            var tail = CoastUiArt.Panel(_bubbleBg.transform, "Tail", Color.white, 4); tail.raycastTarget = false;
            var trt = tail.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0f, 0f); trt.pivot = new Vector2(0.5f, 0.5f); trt.anchoredPosition = new Vector2(26f, 2f); trt.sizeDelta = new Vector2(26f, 26f); trt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _bubble = CoastHudLayout.MakeText(_bubbleBg.rectTransform, "T", "", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-14f, -4f));
            _bubble.color = Navy; _bubble.resizeTextForBestFit = true; _bubble.resizeTextMinSize = 12; _bubble.resizeTextMaxSize = CoastHudLayout.Scaled(18); _bubble.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bubbleBg.gameObject.SetActive(false);

            // ── 게이지 2개: 체력(게이트) · 기운(100−스트레스) ──
            // 63차(시안): 체력·기운 게이지는 메인에서 뺀다(상태창에 있음) — 게이트는 아래 안내 줄
            _staminaFill = null; _staminaTxt = null; _energyFill = null; _energyTxt = null; _gateMark = null; _gateFlag = null;
            if (false) _staminaFill = Gauge(new Vector2(0f, 0f), new Vector2(6f, 318f), new Vector2(322f, 54f), Loc.T("체력", "Stamina"), new Color(0.95f, 0.35f, 0.40f), out _staminaTxt);
            // 66차(사용자 시안): 주인공 양옆 **세로 게이지** — 왼쪽 ♥ HP(체력, 빨강) / 오른쪽 STRESS(보라). 아래에서 위로 찬다.
            _hpFill = SideGauge(false, "HP", new Color(0.95f, 0.22f, 0.28f), "Icon_Heart", null);
            _stressFill = SideGauge(true, "STRESS", new Color(0.62f, 0.38f, 0.92f), null, "×_×");
            _gateLabel = CoastHudLayout.MakeText(_root, "Gate", "", 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 254f), new Vector2(0f, 280f));
            _gateLabel.color = new Color(1f, 0.95f, 0.80f); CoastUiArt.OutlineText(_gateLabel, new Color(0f, 0f, 0f, 0.6f), 1.2f);
            _gateLabel.gameObject.SetActive(false);   // 65차(사용자): 생활 띠 아래 작은 글(챕터·체력·기운 줄) 삭제 — 수치는 상태창에

            // ── 행동 3개 ──
            string[] keys = { "UI_Tama_Feed", "UI_Tama_Play", "UI_Tama_Work" };
            string[] names = { Loc.T("밥", "Feed"), Loc.T("놀기", "Play"), Loc.T("알바", "Work") };
            Color[] fills = { new Color(1f, 0.80f, 0.35f), new Color(0.55f, 0.85f, 1f), new Color(0.75f, 0.65f, 0.95f) };
            float cw = 208f, gap = 12f, x0 = (664f - (3 * cw + 2 * gap)) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var card = CoastUiArt.GlossyPill(_root, "Act" + i, fills[i], 24, 10);
                var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0f, 0f); crt.pivot = new Vector2(0f, 0f);
                // 60차(시안): 카드 = 위 큰 아이콘 + 아래 이름, 모서리 ✦ — 효과는 작은 글씨 한 줄
                crt.anchoredPosition = new Vector2(x0 + i * (cw + gap), 118f); crt.sizeDelta = new Vector2(cw, 136f); card.raycastTarget = true;
                var icon = CoastUiArt.Art(keys[i]);
                if (icon != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(crt, false); im.sprite = icon; im.preserveAspect = true; im.raycastTarget = false;
                    Anchor(im.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(70f, 70f)); im.rectTransform.pivot = new Vector2(0.5f, 1f);
                }
                // 63차(사용자): 이름 아래 작은 효과 글자는 없앤다 — 아이콘 + 이름만
                var n = CoastHudLayout.MakeText(crt, "N", names[i], 28, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 10f), new Vector2(-4f, 60f));
                n.color = Navy; n.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(n, new Color(1f, 1f, 1f, 0.6f), 1.2f);
                Sparkle(crt, new Vector2(i == 2 ? 1f : 0f, i == 1 ? 1f : 0f), new Vector2(i == 2 ? -14f : 14f, i == 1 ? -14f : 14f), 16);
                Sparkle(crt, new Vector2(i == 0 ? 1f : 0f, 1f), new Vector2(i == 0 ? -12f : 12f, -12f), 10);
                int idx = i;
                _actBtn[i] = card.gameObject.AddComponent<Button>(); _actBtn[i].transition = Selectable.Transition.None;
                _actBtn[i].onClick.AddListener(() => DoAction(idx));
            }

            // ── 아래: 자동 토글 + 이번 주 진행 ──
            var auto = CoastUiArt.GlossyPill(_root, "Auto", new Color(0.55f, 0.55f, 0.62f), 22, 8);
            Anchor(auto.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 6f), new Vector2(176f, 100f)); auto.raycastTarget = true;
            _autoLabel = CoastHudLayout.MakeText(auto.rectTransform, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 2f));
            _autoLabel.color = Color.white; CoastUiArt.OutlineText(_autoLabel, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var ab = auto.gameObject.AddComponent<Button>(); ab.transition = Selectable.Transition.None;
            ab.onClick.AddListener(() => { _auto = !_auto; _autoTimer = 0f; if (Save != null) { Save.autoMode = _auto; _gm.Persist(); } RefreshAuto(); ShowBubble(_auto ? Loc.T("내가 알아서 할게!", "I'll take care of myself!") : Loc.T("같이 하자.", "Let's do it together."), 2f); });
            // 55차-2(사용자): 「이번 주 행동」 알약이 곧 **다음 턴** 버튼 — 누르면 한 주가 끝난다(행동을 다 안 했으면 한 번 더 눌러 확인).
            var prog = CoastUiArt.GlossyPill(_root, "NextTurn", Pink, 30, 12);
            Anchor(prog.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 6f), new Vector2(464f, 100f)); prog.raycastTarget = true;
            _actionsLeft = CoastHudLayout.MakeText(prog.rectTransform, "T", "", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, 2f));
            _actionsLeft.color = Color.white; _actionsLeft.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_actionsLeft, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            _actionsLeft.resizeTextForBestFit = true; _actionsLeft.resizeTextMinSize = 12; _actionsLeft.resizeTextMaxSize = CoastHudLayout.Scaled(34);
            Sparkle(prog.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -16f), 18); Sparkle(prog.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 18f), 14); Sparkle(prog.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -12f), 10);
            var ntb = prog.gameObject.AddComponent<Button>(); ntb.transition = Selectable.Transition.None;
            ntb.onClick.AddListener(OnNextTurnPressed);
            RefreshAuto();
        }

        private Image Gauge(Vector2 anchor, Vector2 pos, Vector2 size, string label, Color color, out Text valueTxt)
        {
            var track = CoastUiArt.CutePill(_root, "Gauge_" + label, new Color(0.10f, 0.13f, 0.30f, 0.92f), 18, 3);
            Anchor(track.rectTransform, anchor, anchor, pos, size);
            var fillBg = CoastUiArt.Panel(track.transform, "Bg", new Color(0f, 0f, 0f, 0.35f), 12);
            Anchor(fillBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            // 59차(사용자 「체력바 이상」): 숫자가 막대 위에 겹쳐 게이트 눈금이 「1|27」처럼 보였다 → 숫자는 막대 오른쪽 칸으로, 막대엔 채움과 눈금만.
            // 60차(시안): 글자는 다시 막대 **안** 가운데(굵게·테두리), 게이트는 막대 위 ▼ 깃발만(막대 안 눈금 없음 → 글자와 안 겹친다)
            fillBg.rectTransform.offsetMin = new Vector2(74f, 12f); fillBg.rectTransform.offsetMax = new Vector2(-12f, -12f); fillBg.raycastTarget = false;
            var fill = CoastUiArt.Panel(fillBg.transform, "Fill", color, 10);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f); fill.rectTransform.anchorMax = new Vector2(0.5f, 1f); fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero; fill.raycastTarget = false;
            var l = CoastHudLayout.MakeText(track.rectTransform, "L", label, 16, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(14f, 0f), new Vector2(74f, 0f));
            l.color = Color.white;
            valueTxt = CoastHudLayout.MakeText(track.rectTransform, "V", "", 17, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(74f, 0f), new Vector2(-12f, 0f));
            valueTxt.color = Color.white; valueTxt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(valueTxt, new Color(0.05f, 0.05f, 0.15f, 0.8f), 1.6f);
            valueTxt.resizeTextForBestFit = true; valueTxt.resizeTextMinSize = 10; valueTxt.resizeTextMaxSize = CoastHudLayout.Scaled(17);
            return fill;
        }

        /// 60차: 알약 왼쪽 아이콘(Resources/CoastRun/Icon_*.png).
        private static void SideIcon(RectTransform pill, string name, float size = 26f, float left = 12f)
        {
            var sp = CoastUiArt.Art(name); if (sp == null || pill == null) return;
            var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            im.transform.SetParent(pill, false); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
            Anchor(im.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left, 0f), new Vector2(size, size)); im.rectTransform.pivot = new Vector2(0f, 0.5f);
        }
        /// 60차: 모서리 ✦ 반짝이.
        private static void Sparkle(RectTransform parent, Vector2 anchor, Vector2 pos, int size)
        {
            var t = CoastHudLayout.MakeText(parent, "Sp", "✦", size, TextAnchor.MiddleCenter, anchor, anchor, new Vector2(pos.x - size, pos.y - size), new Vector2(pos.x + size, pos.y + size));
            t.color = new Color(1f, 1f, 1f, 0.85f); t.raycastTarget = false;
        }

        /// 66차: 세로 게이지 한 개 — 위 아이콘 + 라벨, 아래로 긴 막대(어두운 트랙 + 색 채움 + 흰 하이라이트).
        private Text _hpTxt, _stressTxt;
        /// 135차(사용자): 세로 막대(화면 양옆) → 모바일 육성게임식 **가로 상태 바** 두 개(아이콘 알약 + 채움 + 숫자), 아이콘 줄 바로 아래.
        private Image SideGauge(bool right, string label, Color col, string iconRes, string iconGlyph)
        {
            float x0 = right ? 366f : 12f, w = 342f, y = -206f, h = 46f;
            var track = CoastUiArt.CutePill(_root, (right ? "Stress" : "Hp") + "Track", new Color(0.10f, 0.13f, 0.30f, 0.90f), 14, 3); track.raycastTarget = false;
            Anchor(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0, y), new Vector2(w, h));
            var bg = CoastUiArt.Panel(track.transform, "Bg", new Color(0f, 0f, 0f, 0.35f), 9); bg.raycastTarget = false;
            Anchor(bg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            bg.rectTransform.offsetMin = new Vector2(112f, 9f); bg.rectTransform.offsetMax = new Vector2(-10f, -9f);
            var fill = CoastUiArt.Panel(bg.transform, "Fill", col, 8); fill.raycastTarget = false;
            fill.rectTransform.anchorMin = new Vector2(0f, 0f); fill.rectTransform.anchorMax = new Vector2(0.5f, 1f); fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            var hi = CoastUiArt.Panel(fill.transform, "Hi", new Color(1f, 1f, 1f, 0.35f), 3); hi.raycastTarget = false;
            hi.rectTransform.anchorMin = new Vector2(0f, 0.56f); hi.rectTransform.anchorMax = new Vector2(1f, 0.82f); hi.rectTransform.offsetMin = new Vector2(6f, 0f); hi.rectTransform.offsetMax = new Vector2(-6f, 0f);
            if (right)
            {
                // 번아웃 한계선 눈금(세로) — 체력이 오르면 오른쪽으로 이동
                var tick = CoastUiArt.Panel(bg.transform, "Limit", new Color(1f, 0.95f, 0.35f), 2); tick.raycastTarget = false;
                var trt = tick.rectTransform;
                trt.anchorMin = new Vector2(0.7f, 0f); trt.anchorMax = new Vector2(0.7f, 1f); trt.pivot = new Vector2(0.5f, 0.5f);
                trt.offsetMin = new Vector2(-2f, -3f); trt.offsetMax = new Vector2(2f, 3f);
                _stressTick = trt;
            }
            // 아이콘 동그라미 + 라벨
            var circ = CoastUiArt.GlossyPill(track.transform, "Ic", col, 20, 5); circ.raycastTarget = false;
            Anchor(circ.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(38f, 38f)); circ.rectTransform.pivot = new Vector2(0f, 0.5f);
            var tex = iconRes != null ? ArtAssets.LoadTexture(iconRes) : null;
            if (tex != null)
            {
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(circ.transform, false); im.sprite = CoastUiArt.AsSprite(tex); im.preserveAspect = true; im.raycastTarget = false;
                Anchor(im.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f)); im.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                var g = CoastHudLayout.MakeText(circ.rectTransform, "G", iconGlyph ?? "!", 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
                g.color = Color.white; g.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(g, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            }
            var lab = CoastHudLayout.MakeText(track.rectTransform, "L", label, 14, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(50f, 0f), new Vector2(112f, 0f));
            lab.color = Color.Lerp(col, Color.white, 0.55f); lab.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lab, new Color(0f, 0f, 0f, 0.5f), 1.2f); lab.raycastTarget = false;
            lab.resizeTextForBestFit = true; lab.resizeTextMinSize = 9; lab.resizeTextMaxSize = CoastHudLayout.Scaled(14);   // 136차: STRESS 글자가 칸 밖으로 새지 않게
            var v = CoastHudLayout.MakeText(bg.rectTransform, "V", "", 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
            v.color = Color.white; v.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(v, new Color(0.05f, 0.05f, 0.15f, 0.85f), 1.5f); v.raycastTarget = false;
            v.resizeTextForBestFit = true; v.resizeTextMinSize = 10; v.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            if (right) _stressTxt = v; else _hpTxt = v;
            return fill;
        }

        /// 74차: 스트레스 구간 색 — 한눈에 「아 이제 위험하다」가 보이게.
        private static Color StressColor(StressStage st)
        {
            switch (st)
            {
                case StressStage.Crisis: return new Color(0.95f, 0.15f, 0.20f);
                case StressStage.Burnout: return new Color(0.98f, 0.35f, 0.30f);
                case StressStage.Worn: return new Color(0.98f, 0.62f, 0.25f);
                case StressStage.Tired: return new Color(0.55f, 0.62f, 0.95f);
                default: return new Color(0.62f, 0.38f, 0.92f);
            }
        }

        private static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(aMin.x, aMin.y);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        // ── 표시 갱신 ───────────────────────────────────────────────────
        public void Refresh()
        {
            RefreshGachaBadge();   // 131차
            if (Save == null) return;
            _gm?.SyncWallet();   // 109차: 코인·돈 일원화 — 러닝에서 모은 코인이 바로 보이게
            LifeItems.Ensure(Save);
            var s = Save.stats;
            var season = Timeline.SeasonOf(Save.week);
            string seasonKo = season == SeasonKind.Spring ? "봄" : season == SeasonKind.Summer ? "여름" : season == SeasonKind.Autumn ? "가을" : "겨울";
            var rec = Save.CurrentChapter;
            int weeksLeft = rec != null ? Mathf.Max(0, rec.weekEnd - Save.week) : 0;
            _weekLabel.text = Loc.T($"{Save.week}주차 · {seasonKo}", $"Week {Save.week} · {season}");   // 66차(사용자): CH 표기 삭제
            _moneyLabel.text = $"{LevelSystem.FormatK(s.money)}G  ♥{Save.chapterHearts}";   // 53차: 1000 단위 k
            if (_levelLabel != null) _levelLabel.text = $"Lv{Mathf.Max(1, Save.level)}";   // 63차: 둥근 ★ 버튼 아래 작은 레벨
            int need = StoryGate.Required(Save);
            // 57차(사용자 「체력바 확인」): 「123 / 36」이 헷갈렸다 → 막대 = 체력/최대(200), 게이트 자리에 흰 눈금, 글자 = 「체력 123 · 게이트 36 ✓」
            float gx = Mathf.Clamp01(need / (float)PlayerStats.StatMax);
            if (_hpFill != null) _hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp(s.stamina / (float)PlayerStats.StatMax, 0.03f, 1f), 1f);
            if (_hpTxt != null) _hpTxt.text = s.stamina >= need ? $"{s.stamina} / {PlayerStats.StatMax}  ·  " + Loc.T($"게이트 {need} ✓", $"gate {need} ✓") : $"{s.stamina} / {PlayerStats.StatMax}  ·  " + Loc.T($"게이트 {need}", $"gate {need}");
            // 74차(사용자: 스트레스가 너무 적게 쌓인다): 게이지를 0~100 척도로(전엔 /200 이라 절반으로 보였다) +
            //   번아웃 한계선 눈금 + 구간별 색(평온 라벤더 → 피곤 하늘 → 지침 주황 → 번아웃 빨강).
            if (_stressFill != null)
            {
                _stressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp(s.stress / (float)PlayerStats.StressMax, 0.03f, 1f), 1f);
                _stressFill.color = StressColor(s.Stage);
                if (_stressTxt != null) _stressTxt.text = $"{s.stress} / {PlayerStats.StressMax}  ·  " + (s.Burnout ? Loc.T("번아웃!", "Burnout!") : Loc.T($"한계 {s.StressLimit}", $"limit {s.StressLimit}"));
            }
            if (_stressTick != null)
            {
                float ly = Mathf.Clamp01(s.StressLimit / (float)PlayerStats.StressMax);
                _stressTick.anchorMin = new Vector2(ly, 0f); _stressTick.anchorMax = new Vector2(ly, 1f);
                var tick = _stressTick.GetComponent<Image>();
                if (tick != null) tick.color = s.Burnout ? new Color(1f, 0.30f, 0.30f) : new Color(1f, 0.95f, 0.35f);
            }
            if (_staminaFill != null)
            {
            _staminaFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.stamina / (float)PlayerStats.StatMax), 1f);
            _staminaFill.color = s.stamina >= need ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.95f, 0.35f, 0.40f);
            if (_gateMark != null) { _gateMark.rectTransform.anchorMin = new Vector2(gx, 0f); _gateMark.rectTransform.anchorMax = new Vector2(gx, 0f); }
            if (_gateFlag != null) { _gateFlag.rectTransform.anchorMin = _gateFlag.rectTransform.anchorMax = new Vector2(gx, 1f); _gateFlag.color = s.stamina >= need ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.85f, 0.55f); }
            // 59차: 막대 안 글자 없음 — 오른쪽 칸에 숫자만(게이트 통과면 ✓), 게이트 수치는 아래 안내 줄에
            _staminaTxt.text = s.stamina >= need ? Loc.T($"{s.stamina} / 게이트 {need} ✓", $"{s.stamina} / gate {need} ✓") : Loc.T($"{s.stamina} / 게이트 {need}", $"{s.stamina} / gate {need}");
            }
            int energy = Mathf.Clamp(100 - s.stress, 0, 100);
            if (_energyFill != null)
            {
            _energyFill.rectTransform.anchorMax = new Vector2(energy / 100f, 1f);
            _energyFill.color = energy < 30 ? new Color(0.95f, 0.55f, 0.25f) : new Color(0.35f, 0.75f, 0.95f);
            _energyTxt.text = Loc.T($"기운 {energy}", $"Energy {energy}");
            }
            _gateLabel.text = s.stamina >= need
                ? Loc.T($"챕터 {Save.chapter} 송전탑까지 {weeksLeft}주 · 체력 {s.stamina} ▼{need} 게이트 통과! · 기운 {energy}", $"{weeksLeft} weeks to the tower · stamina {s.stamina} gate ▼{need} OK! · energy {energy}")
                : Loc.T($"챕터 {Save.chapter} 송전탑까지 {weeksLeft}주 · 체력 {s.stamina} ▼{need} 까지 {need - s.stamina} 더 · 기운 {energy}", $"{weeksLeft} weeks to the tower · {need - s.stamina} more to gate ▼{need} · energy {energy}");
            int done = Save.boundaryPending ? Timeline.PhasesPerWeek : Save.phaseIndex;   // 67차-8: 경계 대기 중엔 동그라미 3개 다 찬 상태
            var contest = StoryContest.Get(Save.chapter);
            string turnTail = weeksLeft == 0 ? (contest != null ? Loc.T("이야기·대회", "story·contest") : Loc.T("이야기", "story")) : "";
            // 60차(시안): 진행은 위 동그라미 3개가 보여 주니 버튼은 「다음 턴」만(마감 주엔 작게 꼬리)
            _actionsLeft.text = Loc.T("다음 턴", "Next turn");   // 63차(사용자): 글자는 「다음 턴」만
            for (int i = 0; i < 3; i++)
            {
                if (_actRing[i] == null) continue;
                bool on = i < done;
                _actRing[i].color = on ? new Color(0.30f, 0.78f, 0.40f) : new Color(0.62f, 0.64f, 0.70f, 0.95f);
                var inner = _actRing[i].transform.Find("In")?.GetComponent<Image>();
                if (inner != null) inner.color = on ? new Color(0.36f, 0.86f, 0.48f) : new Color(0.30f, 0.32f, 0.40f, 0.55f);
                if (_actCheck[i] != null) _actCheck[i].gameObject.SetActive(on);
            }
            if (_lifeLabel != null)
            {
                string warn = Survival.Warning(Save);
                _lifeLabel.text = warn != null ? warn : Survival.Summary(Save);
                if (_lifeBand != null) _lifeBand.color = warn != null ? new Color(0.86f, 0.14f, 0.20f) : new Color(0.10f, 0.13f, 0.30f, 0.92f);
                if (_lifeEdge != null) _lifeEdge.color = warn != null ? new Color(1f, 0.85f, 0.20f) : new Color(0.55f, 0.60f, 0.80f);
                if (_lifeIcon != null) _lifeIcon.text = warn != null ? "⚠" : "✦";
            }
            if (_goalRibbon != null) _goalRibbon.text = GoalRibbonText();
            RefreshGirl(null);
        }

        private string GoalRibbonText()
        {
            if (Save == null) return "";
            // 105차(재미요소): 「N주 뒤 대회 · 사진 2 · 매력 32/40」 — 다음 대회와 추천 스탯(게이트는 체력 그대로)
            return RaisingFun.GoalRibbon(Save);
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

        // 52차(사용자): 육성 캐릭터 그림 10장(Raise_Girl_Pose_* — Kling, 얼굴 고정): 자기·밥·카페 알바·배달·해녀·웃음·화남·춤·스케이트·울음.
        //   행동하는 동안 그 활동 포즈, 대성공 = 웃음, 실패 = 울음, 스트레스가 아주 높으면 화남. 포즈는 잠깐(_poseUntil) 붙잡았다가 기분 그림으로.
        private string _pose; private float _poseUntil;
        private static string PoseFor(string scheduleId, Outcome? outcome)
        {
            if (outcome == Outcome.GreatSuccess) return "Laugh";
            if (outcome == Outcome.Fail) return "Cry";
            switch (scheduleId)
            {
                case "job_cafe": case "job_sashimi": case "job_hall": case "job_salon": return "Cafe";
                case "job_delivery": case "job_orange": case "job_market": case "job_night_delivery": case "job_tower_fix": case "job_lighthouse": return "Delivery";
                case "job_haenyeo": case "les_swim": case "rest_sea": return "Haenyeo";
                case "dev_dance": case "les_dance": return "Dance";
                case "dev_skate": case "dev_oreum": case "les_skate": case "les_gym": return "Skate";
                case "rest_home": case "rest_nap": return "Sleep";
                case "les_cook": return "Eat";
                case "dev_radio": case "job_dj_assist": case "les_ham": case "job_dangsan": return "Laugh";
                default: return null;
            }
        }
        public void HoldPose(string pose, float seconds) { _pose = pose; _poseUntil = Time.unscaledTime + seconds; RefreshGirl(null); }

        private void RefreshGirl(string moodKey)
        {
            if (Save == null) return;
            var st = Save.stats;
            // 74차: 표정도 스트레스 구간(0~100)을 본다 — 전엔 stress/stamina 비율이라 체력이 오르면 늘 웃고 있었다.
            var stage0 = st.Stage;
            string key = moodKey ?? (stage0 <= StressStage.Calm ? "Happy" : stage0 == StressStage.Tired ? "Normal" : "Tired");
            string sfx = SeasonLook.Suffix(Timeline.SeasonOf(Save.week));
            string pose = _pose != null && Time.unscaledTime < _poseUntil ? _pose : (moodKey == null && stage0 >= StressStage.Burnout ? "Angry" : null);
            // 95차: 성장 단계(주차 1~13 = S1 봄 초보 → 14~26 S2 여름 → 27~39 S3 가을 → 40~ S4 겨울·스무 살 직전) 그림이 있으면 우선
            string stage = "S" + Mathf.Clamp(1 + (Mathf.Max(1, Save.week) - 1) / 13, 1, 4);
            // 95차: 옛 포즈 이름(52차 갈색 머리 세트)은 새 수채화 세트(검은 머리·노란 핀)로 바꿔 쓴다 — 그림체가 섞이지 않게
            if (pose != null)
            {
                switch (pose)
                {
                    case "Laugh": pose = null; key = "Happy"; break;
                    case "Cry": pose = "Letter"; break;
                    case "Sleep": pose = "SleepLying"; break;
                    case "Cafe": pose = "Cook"; break;
                    case "Delivery": case "Skate": pose = "Run"; break;
                    case "Haenyeo": case "Dance": pose = "Wave"; break;
                    case "Eat": pose = "Milk"; break;
                    case "Angry": pose = null; key = "Tired"; break;
                }
            }
            var tex = (pose != null ? ArtAssets.LoadTexture("Raise_Girl_Pose_" + pose) : null)
                      ?? ArtAssets.LoadTexture("Raise_Girl_" + stage + "_" + key) ?? ArtAssets.LoadTexture("Raise_Girl_" + stage + "_Normal")
                      ?? ArtAssets.LoadTexture("Raise_Girl_" + key + "_" + sfx) ?? ArtAssets.LoadTexture("Raise_Girl_" + key)
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
            _pose = null; RefreshGirl("Happy");
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
                if (_rubBudget % 5 == 0) LevelSystem.Add(LevelSystem.ExpRubMax);
                RefreshGirl("Happy");
                ShowBubble(Loc.T("기분 좋아~", "That feels nice~"), 1.2f);
                SpawnHeart(2);
                _gm.Persist();
            }
            // 74차: 쓰다듬기로 더 못 내릴 때는 무엇을 해야 하는지 알려 준다(주 상한 = RubBudgetPerWeek).
            else ShowBubble(Save.stats.stress > 0
                ? Loc.T("쓰다듬는 건 이제 됐어… 놀거나 쉬어야 풀릴 것 같아.", "Petting won't cut it… I need to play or rest.")
                : Loc.T("이제 됐어, 고마워!", "That's enough, thanks!"), 1.6f);
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
                if (_autoTimer >= 1.2f) { _autoTimer = 0f; if (Save.boundaryPending) StartCoroutine(ResumeBoundary()); else if (Save.phaseIndex >= Timeline.PhasesPerWeek) StartCoroutine(WeekendOnly()); else DoAction(AutoPick()); }
            }
#if UNITY_EDITOR
            if (CoastRemoteKeys.Down(KeyCode.F1) && !_busy)
            {
                PlayerPrefs.SetInt(RaisingTutorial.PrefKey, 0);
                StartCoroutine(TamaTutorial());
            }
#endif
        }

        private int AutoPick()
        {
            var s = Save.stats;
            LifeItems.Ensure(Save);
            bool canFeed = LifeItems.HasEdible(Save);
            // 74차: 자동도 스트레스 구간을 본다 — 지침(55↑)부터는 놀기로 풀고, 번아웃이면 무조건 놀기.
            if (s.Burnout) return 1;
            // 요리가 없으면 「밥」을 고르지 않음(매 틱 실패로 오토가 멈춘 것처럼 보임)
            if (canFeed && !Save.restedThisWeek && s.Stage >= StressStage.Worn) return 0;
            if (s.Stage >= StressStage.Worn) return 1;
            if (s.money < 100) return 2;
            if (canFeed && s.stamina < StoryGate.Required(Save) && s.Stage <= StressStage.Tired) return 0;
            _lastAutoPick = _lastAutoPick == 1 ? 2 : 1;
            return _lastAutoPick;
        }

        // ── 행동 → 카드 2장 고르기 → 기존 스케줄 판정 ──────────────────
        private void DoAction(int idx)
        {
            if (_busy || Save == null) return;
            // 95차: 행동별 포즈 그림(밥 → Cook, 놀기 → Marbles, 알바 → Run) — 그림이 없으면 무시됨
            HoldPose(idx == 0 ? "Cook" : idx == 1 ? "Marbles" : "Run", 2.5f);
            if (Save.boundaryPending) { ShowBubble(BoundaryHint(), 2.5f); return; }
            if (Save.phaseIndex >= Timeline.PhasesPerWeek) { ShowBubble(Loc.T("이번 주 행동은 다 했어 — 「다음 턴」을 눌러!", "All actions done — press Next turn!"), 2f); return; }
            var season = Timeline.SeasonOf(Save.week);
            if (idx == 0)
            {
                LifeItems.Ensure(Save);
                if (!LifeItems.HasEdible(Save))
                {
                    int ings = LifeItems.CountCat(Save, LifeItemCat.Ingredient);
                    ShowBubble(ings > 0
                        ? Loc.T("재료만 있어 — 마이룸에서 조리한 뒤 먹어!", "Ingredients only — cook in My Room first!")
                        : Loc.T("먹을 요리가 없어 — 상점에서 재료·요리를 사자!", "No meals — buy ingredients or dishes!"), 2.8f);
                    return;
                }
            }
            if (_auto)
            {
                if (idx == 0)
                {
                    var ed = LifeItems.ListEdible(Save);
                    if (ed.Count > 0) LifeItems.Eat(Save, ed[0].def.id);
                }
                ScheduleDef def = idx == 0 ? PickRest(season) : idx == 1 ? PickDev(season) : PickJob(season);
                if (def == null) { ShowBubble(Loc.T("지금은 할 게 없네…", "Nothing to do right now…"), 1.5f); return; }
                if (def.dMoney < 0 && Save.stats.money + def.dMoney < 0) { ShowBubble(Loc.T("돈이 모자라… 알바부터!", "Not enough money… work first!"), 1.8f); return; }
                StartCoroutine(ActionRoutine(idx, def));
                return;
            }
            if (idx == 0)
            {
                MealPickUI.Open(_gm, dishId =>
                {
                    if (!LifeItems.Eat(Save, dishId))
                    {
                        ShowBubble(Loc.T("먹을 수 없어…", "Can't eat that…"), 1.5f);
                        return;
                    }
                    _gm.Persist();
                    var cards = BuildChoices(0, season);
                    if (cards.Count == 0) { ShowBubble(Loc.T("지금은 할 게 없네…", "Nothing to do right now…"), 1.5f); return; }
                    if (cards.Count == 1) StartCoroutine(ActionRoutine(0, cards[0]));
                    else StartCoroutine(CardPickRoutine(0, cards));
                });
                return;
            }
            var cards2 = BuildChoices(idx, season);
            if (cards2.Count == 0) { ShowBubble(Loc.T("지금은 할 게 없네…", "Nothing to do right now…"), 1.5f); return; }
            if (cards2.Count == 1) { StartCoroutine(ActionRoutine(idx, cards2[0])); return; }
            StartCoroutine(CardPickRoutine(idx, cards2));
        }

        private List<ScheduleDef> BuildChoices(int idx, SeasonKind season)
        {
            var pool = new List<ScheduleDef>();
            if (idx == 0)
            {
                foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.Rest, season)) pool.Add(d);
            }
            else if (idx == 1)
            {
                foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.SelfDev, season))
                    if (d.LockReason(Save.stats) == null) pool.Add(d);
                // 돈 있으면 교육 1장 섞기
                var lessons = new List<ScheduleDef>();
                foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.Lesson, season))
                    if (d.LockReason(Save.stats) == null) lessons.Add(d);
                if (lessons.Count > 0 && Save.stats.money >= 40)
                    pool.Add(lessons[UnityEngine.Random.Range(0, lessons.Count)]);
            }
            else
            {
                var night = new List<ScheduleDef>();
                foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.Job, season))
                {
                    if (d.LockReason(Save.stats) != null) continue;
                    if (d.dTrouble > 0) night.Add(d);
                    else pool.Add(d);
                }
                // Prefer at most one night job in the two-card pick
                if (night.Count > 0)
                    pool.Add(night[UnityEngine.Random.Range(0, night.Count)]);
            }
            // Shuffle and take up to 2 distinct
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }
            var pick = new List<ScheduleDef>();
            // 105차(재미요소): 2장 중 1장은 지난번에 고른 카드 — 계획을 이어 갈 수 있게(숙련 ★을 쌓는 길)
            // 105차(P2-1): 보류 단서에 도움이 되는 카드가 풀에 있으면 먼저(라디오 교육·송전탑 알바·해녀·배달)
            var helpful = ClueSystem.HelpfulCards(Save);
            if (helpful.Count > 0) { var h = pool.Find(x => helpful.Contains(x.id)); if (h != null) pick.Add(h); }
            string lastId = Save.lastCardIds != null && idx < Save.lastCardIds.Length ? Save.lastCardIds[idx] : null;
            if (!string.IsNullOrEmpty(lastId) && pick.Count < 2) { var last = pool.Find(x => x.id == lastId); if (last != null && !pick.Exists(x => x.id == last.id)) pick.Add(last); }
            // If a night job is in the pool, try to keep one in the final two
            ScheduleDef nightPick = null;
            foreach (var d in pool) if (d.dTrouble > 0 && !pick.Exists(x => x.id == d.id)) { nightPick = d; break; }
            if (nightPick != null && pick.Count < 2) pick.Add(nightPick);
            foreach (var d in pool)
            {
                if (pick.Count >= 2) break;   // 108차: 우선 카드가 이미 2장이면 더 안 뽑음(3장이 되어 화면을 넘던 버그)
                if (pick.Exists(x => x.id == d.id)) continue;
                pick.Add(d);
            }
            if (pick.Count == 0 && idx == 0) { var h = ScheduleTable.Get("rest_home"); if (h != null) pick.Add(h); }
            if (pick.Count == 0 && idx == 2) { var c = ScheduleTable.Get("job_cafe"); if (c != null) pick.Add(c); }
            return pick;
        }

        private IEnumerator CardPickRoutine(int idx, List<ScheduleDef> cards)
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            ScheduleDef chosen = null;
            if (_cardPickOverlay != null) Destroy(_cardPickOverlay);
            _cardPickOverlay = new GameObject("CardPick", typeof(RectTransform), typeof(Image));
            _cardPickOverlay.transform.SetParent(_root, false);
            var dim = _cardPickOverlay.GetComponent<Image>();
            // 112차 시안: 라벤더 바탕 + 꽃·나비
            dim.color = new Color(0.93f, 0.88f, 0.98f, 1f); dim.raycastTarget = true;   // 135차: 시안처럼 불투명 라벤더
            var drt = dim.rectTransform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = new Vector2(-400f, -400f); drt.offsetMax = new Vector2(400f, 400f);

            // 콘텐츠는 Fit(세이프존) 안에만 — 딤만 화면 밖까지 덮는다
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(_root, false); // will reparent under overlay
            content.SetParent(drt, false);
            content.anchorMin = new Vector2(0.5f, 0.5f); content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            // Fit 안쪽 크기(HudInset 기준 ≈ 세이프존). 오버레이가 -400 확장돼 있으므로 실제 보이는 박스로 맞춤.
            float cw = 664f, ch = 1500f;   // 135차: 시안처럼 화면 위아래를 꽉 쓴다(알약은 맨 위, 다음 턴은 맨 아래)
            content.sizeDelta = new Vector2(cw, ch); content.anchoredPosition = Vector2.zero;
            EventCardKit.DecoratePickBg(content);

            // 상단 상태 알약
            string moneyStr = LevelSystem.FormatK(Save.stats.money);
            string heartStr = Save.chapterHearts.ToString();
            int energy = Mathf.Clamp(100 - Save.stats.stress, 0, 100);
            string clockStr = $"{Save.stats.stamina}:{energy:D2}";
            EventCardKit.PickStatusPills(content, moneyStr, heartStr, clockStr);

            string banner = idx == 0
                ? Loc.T($"이번 주말 휴식  |  치유 시간 {Save.stats.stamina}:{energy:D2}", $"Weekend rest  |  Heal {Save.stats.stamina}:{energy:D2}")
                : idx == 1 ? Loc.T("이번 주말, 뭐 할까?", "What to do this weekend?")
                : Loc.T("이번 주말, 어디 알바?", "Which job this weekend?");
            var title = CoastHudLayout.MakeText(content, "Banner", "✦  " + banner + "  ✦", 27, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -158f), new Vector2(-16f, -104f));   // 136차: 오른쪽 ✕ 자리 확보
            title.color = EventCardKit.BrownInk; title.fontStyle = FontStyle.Bold; title.raycastTarget = false;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 14; title.resizeTextMaxSize = CoastHudLayout.Scaled(27);

            // 시안: 세로 2장 스택 (노랑=집밥 / 라벤더=낮잠 순으로 맞춤)
            if (idx == 0 && cards.Count >= 2)
            {
                int hi = cards.FindIndex(c => c != null && c.id == "rest_home");
                int ni = cards.FindIndex(c => c != null && c.id == "rest_nap");
                if (hi >= 0 && ni >= 0 && hi > ni)
                {
                    var tmp = cards[hi]; cards[hi] = cards[ni]; cards[ni] = tmp;
                }
                else if (hi > 0) { var tmp = cards[0]; cards[0] = cards[hi]; cards[hi] = tmp; }
            }

            Color[] fills = {
                new Color(1f, 0.95f, 0.78f),
                new Color(0.90f, 0.86f, 0.98f),
                new Color(0.82f, 0.93f, 0.98f)
            };
            Color[] tabs = {
                new Color(0.98f, 0.78f, 0.28f),
                new Color(0.62f, 0.48f, 0.90f),
                new Color(0.35f, 0.65f, 0.95f)
            };
            string[] badgeRest = { Loc.T("☀ 힐링", "☀ Heal"), Loc.T("☾ 힐링", "☾ Heal") };
            string[] badgePlay = { Loc.T("🎮 놀기", "Play"), Loc.T("✨ 놀기", "Play") };
            string[] badgeJob = { Loc.T("💼 알바", "Job"), Loc.T("🏃 알바", "Job") };

            // 135차(사용자 시안): 두 장을 **나란히**(세로형 카드) — 그림 4:3 + 제목 + 남색 효과 칩. 3장이면 좁게 3열.
            int n = cards.Count;
            float cardW = n <= 2 ? 320f : 206f, cardH = n <= 2 ? 500f : 400f, gap = n <= 2 ? 14f : 16f;   // 136차: 시안처럼 조금 더 큼직하게
            float rowW = n * cardW + (n - 1) * gap;
            float cy = 30f;   // 카드 중심(콘텐츠 중앙 기준) — 시안: 화면 세로 중앙보다 살짝 아래
            for (int i = 0; i < n; i++)
            {
                var def = cards[i];
                string tab = idx == 0 ? badgeRest[Mathf.Min(i, badgeRest.Length - 1)]
                    : idx == 1 ? badgePlay[Mathf.Min(i, badgePlay.Length - 1)]
                    : badgeJob[Mathf.Min(i, badgeJob.Length - 1)];
                int npcOf = Affinity.NpcOf(def.id);
                if (npcOf >= 0) tab = "♥" + Affinity.Get(Save, npcOf) + " " + Affinity.ShortName(npcOf);

                int mlv = RaisingFun.MasteryLevel(Save, def.id);
                string emoji = PickEmoji(def);
                string titleTxt = emoji + " " + (mlv > 1 ? def.Name + " " + RaisingFun.Stars(mlv) : def.Name);
                string pv = RaisingFun.Preview(def, Save, out bool warn);
                if (string.IsNullOrEmpty(pv)) pv = Loc.T("♥ 쉬기", "♥ Rest");
                if (!pv.StartsWith("⚡") && !pv.StartsWith("💗") && !pv.StartsWith("♥"))
                    pv = (warn ? "⚠ " : "⚡ ") + pv;

                var captured = def;
                float x = -rowW * 0.5f + cardW * 0.5f + i * (cardW + gap);
                var cbtn = EventCardKit.TallPickCard(
                    content, "C" + i, tab, titleTxt,
                    fills[Mathf.Clamp(i, 0, fills.Length - 1)],
                    tabs[Mathf.Clamp(i, 0, tabs.Length - 1)],
                    new Vector2(x, cy),
                    new Vector2(cardW, cardH),
                    () => { chosen = captured; },
                    pv);
                var crt2 = cbtn.GetComponent<RectTransform>();

                var artTex = ArtAssets.LoadTexture("Sched_" + def.id);
                var artSlot = crt2.Find("ArtSlot") as RectTransform;
                if (artSlot != null && artTex != null)
                {
                    var frame = CoastUiArt.CutePill(artSlot, "ArtFrame", Color.white, 16, 0); frame.raycastTarget = false;
                    var fr = frame.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
                    var mask = frame.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
                    var art = CoastHudLayout.MakeImage(fr, "Art", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
                    art.sprite = CoastUiArt.AsSprite(artTex); art.preserveAspect = false; art.raycastTarget = false;
                    EventCardKit.Animate(art, 0.04f, 8f);
                }
            }

            // 꼬마(왼쪽 아래, 크게) + 큰 말풍선(오른쪽)
            {
                string kidLine = idx == 0 ? Loc.T("나도 푹 쉬어!", "Rest well too!")
                    : idx == 1 ? Loc.T("누나, 같이 놀자!", "Let's play!")
                    : Loc.T("누나, 돈 벌어 오자!", "Let's earn!");
                var kidTex = ArtAssets.LoadTexture("UI_Kid_Bust") ?? ArtAssets.LoadTexture("UI_Butler_Boy");
                float ks = 262f;
                if (kidTex != null)
                {
                    float kw = ks * kidTex.width / Mathf.Max(1, kidTex.height);
                    var kid = CoastHudLayout.MakeImage(content, "Kid", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(4f, 206f), new Vector2(4f + kw, 206f + ks), Color.white);
                    kid.sprite = CoastUiArt.AsSprite(kidTex); kid.preserveAspect = true; kid.raycastTarget = false;
                    kid.gameObject.AddComponent<EventCardKit.KidBob>();
                }
                var bub = CoastUiArt.CutePill(content, "KidBubble", new Color(1f, 0.93f, 0.96f), 26, 0); bub.raycastTarget = false;
                var br = bub.rectTransform; br.anchorMin = br.anchorMax = new Vector2(0f, 0f); br.pivot = new Vector2(0f, 0f);
                br.anchoredPosition = new Vector2(200f, 262f); br.sizeDelta = new Vector2(400f, 108f);
                var bubIn = CoastUiArt.CutePill(br, "In", Color.white, 22, 0); bubIn.raycastTarget = false;
                var bir = bubIn.rectTransform; bir.anchorMin = Vector2.zero; bir.anchorMax = Vector2.one; bir.offsetMin = new Vector2(5f, 5f); bir.offsetMax = new Vector2(-5f, -5f);
                var tail = CoastHudLayout.MakeText(br, "Tail", "◖", 34, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-22f, -20f), new Vector2(8f, 20f));
                tail.color = Color.white; tail.raycastTarget = false;
                var bt = CoastHudLayout.MakeText(br, "T", kidLine, 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));
                bt.color = EventCardKit.BrownInk; bt.fontStyle = FontStyle.Bold; bt.raycastTarget = false;
                bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 16; bt.resizeTextMaxSize = CoastHudLayout.Scaled(30);
                EventCardKit.Sparkle(br, new Vector2(0f, 1f), new Vector2(26f, -10f), 14, new Color(1f, 0.85f, 0.40f));
                EventCardKit.Sparkle(br, new Vector2(1f, 0f), new Vector2(-26f, 12f), 12, new Color(1f, 0.85f, 0.40f));
            }

            // 아래 줄: [⚠ 이번 주말 …(무작위)] [놀기] [일하기] + 큰 「다음 턴」
            bool cancelled = false; int switchTo = -1; bool nextTurn = false;
            Button RowBtn(string name, string label, Color fill, Color edge, float x0, float x1, Action on)
            {
                var e = CoastUiArt.CutePill(content, name + "E", edge, 22, 0); e.raycastTarget = false;
                var ert = e.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0f); ert.pivot = new Vector2(0.5f, 0f);
                ert.anchoredPosition = new Vector2((x0 + x1) * 0.5f, 122f); ert.sizeDelta = new Vector2(x1 - x0, 78f);
                var pill = CoastUiArt.CutePill(content, name, fill, 20, 0); pill.raycastTarget = true;
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
                prt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, 126f); prt.sizeDelta = new Vector2(x1 - x0 - 8f, 70f);
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); on?.Invoke(); });
                var t = CoastHudLayout.MakeText(prt, "T", label, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, 0f));
                t.color = EventCardKit.BrownInk; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(24);
                return b;
            }
            string randLabel = idx == 0 ? Loc.T("⚠ 이번 주말 휴식(무작위)", "⚠ Weekend rest (random)") : idx == 1 ? Loc.T("⚠ 이번 주말 놀기(무작위)", "⚠ Weekend play (random)") : Loc.T("⚠ 이번 주말 알바(무작위)", "⚠ Weekend job (random)");
            // 136차(시안): [무작위 320] [놀기 150] [일하기 172] — 세 알약이 한 줄에 고르게, 글자도 크게
            RowBtn("Rand", randLabel, new Color(1f, 0.90f, 0.93f), new Color(0.95f, 0.62f, 0.75f), -332f, -10f, () => { chosen = cards[UnityEngine.Random.Range(0, cards.Count)]; });
            var lav = new Color(0.90f, 0.86f, 0.98f); var lavE = new Color(0.70f, 0.60f, 0.92f);
            var others = new System.Collections.Generic.List<(string, string, int)>();
            if (idx != 1) others.Add(("Play", Loc.T("❀ 놀기", "❀ Play"), 1));
            if (idx != 2) others.Add(("Job", Loc.T("◆ 일하기", "◆ Work"), 2));
            if (idx != 0) others.Add(("Rest", Loc.T("☾ 휴식", "☾ Rest"), 0));
            {
                float x0 = 0f; float[] w = { 150f, 172f };
                for (int oi = 0; oi < others.Count && oi < 2; oi++)
                {
                    var o = others[oi]; int target = o.Item3;
                    RowBtn(o.Item1, o.Item2, lav, lavE, x0, x0 + w[oi], () => { switchTo = target; });
                    x0 += w[oi] + 10f;
                }
            }
            {
                var e = CoastUiArt.CutePill(content, "NextE", new Color(0.95f, 0.55f, 0.72f), 30, 0); e.raycastTarget = false;
                var ert = e.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0f); ert.pivot = new Vector2(0.5f, 0f);
                ert.anchoredPosition = new Vector2(0f, 18f); ert.sizeDelta = new Vector2(560f, 92f);
                var pill = CoastUiArt.GlossyPill(content, "Next", new Color(1f, 0.72f, 0.84f), 28, 8); pill.raycastTarget = true;
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f);
                prt.anchoredPosition = new Vector2(0f, 22f); prt.sizeDelta = new Vector2(552f, 84f);
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); nextTurn = true; });
                var t = CoastHudLayout.MakeText(prt, "T", Loc.T("다음 턴", "Next turn"), 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 0f));
                t.color = Color.white; t.fontStyle = FontStyle.Bold; t.raycastTarget = false; CoastUiArt.OutlineText(t, new Color(0.55f, 0.18f, 0.35f, 0.8f), 2f);
            }

            // 136차(시안): ✕ 버튼 없음 — 뒤로 가기(Android back / Esc)로 취소
            while (chosen == null && !cancelled && switchTo < 0 && !nextTurn)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { CoastPrefs.Vibrate(); cancelled = true; }
                yield return null;
            }
            if (_cardPickOverlay != null) { Destroy(_cardPickOverlay); _cardPickOverlay = null; }
            if (switchTo >= 0 || nextTurn)
            {
                foreach (var b in _actBtn) if (b != null) b.interactable = true;
                _busy = false;
                if (nextTurn) OnNextTurnPressed(); else DoAction(switchTo);
                yield break;
            }
            if (cancelled || chosen == null)
            {
                foreach (var b in _actBtn) if (b != null) b.interactable = true;
                _busy = false;
                yield break;
            }
            yield return ActionRoutine(idx, chosen);
        }

        private static string PickEmoji(ScheduleDef def)
        {
            if (def == null) return "✦";
            switch (def.id)
            {
                case "rest_home": return "🍚";
                case "rest_nap": return "🌙";
                case "rest_sea": return "🌊";
                case "dev_oreum": return "⛰";
                case "dev_skate": return "🛹";
                case "dev_dance": return "💃";
                case "dev_radio": return "📻";
                default:
                    if (def.category == ScheduleCategory.Job) return "💼";
                    if (def.category == ScheduleCategory.Lesson) return "📚";
                    return "✦";
            }
        }

        private IEnumerator EventChoiceRoutine(RandomEventDef ev)
        {
            // 135차: 미니게임·축제 카드가 떠 있는 동안은 절대 겹쳐 띄우지 않는다
            while (ChapterMissionUI.IsOpen || FestivalUI.IsOpen) yield return null;
            bool manageBusy = !_busy;
            if (manageBusy)
            {
                _busy = true;
                foreach (var b in _actBtn) if (b != null) b.interactable = false;
            }
            int choice = -1;
            if (_eventOverlay != null) Destroy(_eventOverlay);
            _eventOverlay = new GameObject("EventPick", typeof(RectTransform), typeof(Image));
            _eventOverlay.transform.SetParent(_root, false);
            var dim = _eventOverlay.GetComponent<Image>();
            dim.color = new Color(0.08f, 0.06f, 0.14f, 0.70f); dim.raycastTarget = true;
            var drt = dim.rectTransform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = new Vector2(-400f, -400f); drt.offsetMax = new Vector2(400f, 400f);   // 74차: 짧은 비율에서 좌우 여백까지 덮게

            // 108차(사용자): 글자만 있던 선택 화면에 그림 — UI_Ev_<id>(16:9 수채) 를 태그 아래에. 없으면 옛 배치 그대로.
            var evTex = ArtAssets.LoadTexture("UI_Ev_" + ev.id);
            float shift = evTex != null ? 340f : 30f;   // 그림 틀 296(−66~−362) 아래로 제목(−36 기준)이 오게 · 109차: 그림 없어도 제목이 태그와 안 겹치게 30
            var panel = CoastUiArt.CutePill(_eventOverlay.transform, "Panel", new Color(1f, 0.98f, 0.95f), 28, 4);
            var prt = panel.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f); prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero; prt.sizeDelta = new Vector2(560f, 620f + shift); panel.raycastTarget = true;

            EventCardKit.HellsumTag(prt, Loc.T("✨ 헬섬 이벤트", "✦ Story event"), 20f);
            if (evTex != null)
            {
                var frame = CoastUiArt.CutePill(prt, "ArtFrame", new Color(0.92f, 0.86f, 0.80f), 18, 0); frame.raycastTarget = false;
                var fr = frame.rectTransform; fr.anchorMin = new Vector2(0f, 1f); fr.anchorMax = new Vector2(1f, 1f); fr.pivot = new Vector2(0.5f, 1f);
                fr.anchoredPosition = new Vector2(0f, -66f); fr.sizeDelta = new Vector2(-48f, 296f);
                var art = CoastHudLayout.MakeImage(fr, "Art", Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f), Color.white);
                art.sprite = CoastUiArt.AsSprite(evTex); art.preserveAspect = true; art.raycastTarget = false;
                EventCardKit.Animate(art);   // 109차: 그림이 천천히 움직인다(켄번즈)
            }
            var title = CoastHudLayout.MakeText(prt, "Title", Loc.Data("ev." + ev.id, ev.title), 28, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(24f, -78f - shift), new Vector2(-24f, -36f - shift));
            title.color = EventCardKit.BrownInk; title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 16; title.resizeTextMaxSize = CoastHudLayout.Scaled(28);
            var ask = CoastHudLayout.MakeText(prt, "Ask", Loc.T("? 어떻게 할까?", "? What should I do?"), 18, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(24f, -118f - shift), new Vector2(-24f, -88f - shift));
            ask.color = new Color(0.55f, 0.40f, 0.36f);

            string bodyA = string.IsNullOrEmpty(ev.body) ? "—" : ev.body;
            string bodyB = string.IsNullOrEmpty(ev.altBody) ? Loc.T("다른 길로.", "Another way.") : ev.altBody;
            EventCardKit.ChoiceBlock(prt, "BlkA", true, "Icon_Him", bodyA, 140f + shift, 130f);
            EventCardKit.ChoiceBlock(prt, "BlkB", false, "Icon_Eye", bodyB, 286f + shift, 130f);

            // 105차(재미요소 P1-1): 선택 A 에 스탯 체크 — 통과하면 라벨에 「✓ 매력 40」, 모자라면 잠기고 「매력 8 더」
            bool passA = ev.CheckPasses(Save.stats);
            string labelA = ev.ChoiceALabel;
            if (ev.HasStatCheck) labelA += passA ? $"\n✓ {RaisingFun.StatName(ev.condStat)} {ev.condMin}" : Loc.T($"\n{RaisingFun.StatName(ev.condStat)} {ev.CheckShort(Save.stats)} 더", $"\nneed {RaisingFun.StatName(ev.condStat)} +{ev.CheckShort(Save.stats)}");
            var btnA = EventCardKit.SoftChoiceButton(prt, "A", "Icon_Him", labelA, true, new Vector2(-132f, 28f), new Vector2(236f, 64f), () => { if (passA) choice = 0; else CoastToast.Show(Loc.T($"{RaisingFun.StatName(ev.condStat)}이 {ev.CheckShort(Save.stats)} 모자라.", $"Need {ev.CheckShort(Save.stats)} more {RaisingFun.StatName(ev.condStat)}.")); });
            // 109차(사용자): 꼬마가 옆에서 거든다 — 통과면 「누나, 나는 A!」, 모자라면 「B가 낫겠다」
            EventCardKit.Kid(prt, passA ? Loc.T("누나, 나는 A!", "I'd pick A!") : Loc.T("누나, 이번엔 B가 낫겠다", "B this time, maybe"));
            if (!passA && btnA != null) { var ai = btnA.GetComponent<Image>(); if (ai != null) ai.color = new Color(0.62f, 0.60f, 0.66f); }
            EventCardKit.SoftChoiceButton(prt, "B", "Icon_Eye", ev.ChoiceBLabel, false, new Vector2(132f, 28f), new Vector2(236f, 64f), () => { choice = 1; });

            while (choice < 0) yield return null;
            if (_eventOverlay != null) { Destroy(_eventOverlay); _eventOverlay = null; }
            var res = _gm.CommitRandomEvent(ev, choice);
            ShowBubble(res.Body, 4f);
            string d = "";
            if (res.dMoney != 0) d += $" 돈 {res.dMoney:+#;-#}G";
            if (res.dStamina != 0) d += $" 체력 {res.dStamina:+#;-#}";
            if (res.dStress != 0) d += $" 스트레스 {res.dStress:+#;-#}";
            if (res.dHearts != 0) d += $" 하트 {res.dHearts:+#;-#}";
            if (d.Length > 0) CoastToast.Show(Loc.T("돌발 ·", "Event ·") + d);
            Refresh();
            if (manageBusy)
            {
                foreach (var b in _actBtn) if (b != null) b.interactable = true;
                _busy = false;
            }
        }

        private ScheduleDef PickRest(SeasonKind season)
        {
            var list = ScheduleTable.ByCategory(ScheduleCategory.Rest, season);
            return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : ScheduleTable.Get("rest_home");
        }
        private ScheduleDef PickDev(SeasonKind season)
        {
            var list = BuildChoices(1, season);
            if (list.Count == 0) return null;
            // 74차: 지쳐 있으면 자동은 「푸는 놀이」(스트레스가 내려가는 카드)를 고른다 — 연습은 더 지치게 만든다.
            if (Save != null && Save.stats.Stage >= StressStage.Worn)
            {
                ScheduleDef best = null;
                foreach (var d in list) if (best == null || d.dStress < best.dStress) best = d;
                if (best != null && best.dStress < 0) return best;
            }
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
        private ScheduleDef PickJob(SeasonKind season)
        {
            var list = BuildChoices(2, season);
            return list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : ScheduleTable.Get("job_cafe");
        }

        private IEnumerator ActionRoutine(int idx, ScheduleDef def)
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            int slot = Mathf.Clamp(Save.phaseIndex, 0, Timeline.PhasesPerWeek - 1);
            _gm.SetQueued(slot, def.id);
            // 나가는 연출: 말풍선 + 살짝 사라졌다 돌아오기
            string[] going = idx == 0
                ? new[] {
                    Loc.T("밥 먹고 쉴게!", "Gonna eat and rest!"),
                    Loc.T("배고파… 집밥!", "Hungry… home cooking!"),
                    Loc.T("좀 쉬다 올게~", "Gonna rest a bit~"),
                  }
                : new[] {
                    Loc.T($"{def.Name} 다녀올게!", $"Off to {def.Name}!"),
                    Loc.T($"{def.Name}!", $"{def.Name}!"),
                    Loc.T($"{def.place}로 출발~", $"To {def.place}~"),
                  };
            ShowBubble(going[UnityEngine.Random.Range(0, going.Length)], 1.2f);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
            float t = 0f;
            while (t < 0.45f) { t += Time.unscaledDeltaTime; _girl.color = new Color(1f, 1f, 1f, 1f - t / 0.45f); yield return null; }
            HoldPose(PoseFor(def.id, null), 30f);   // 52차: 활동 포즈로 돌아온다
            // 105차(재미요소): 자동 모드는 효율 85% · 지난번 카드 기억(다음 2장 중 1장) · 숙련 ★
            _gm.AutoActing = _auto;
            if (Save.lastCardIds == null || Save.lastCardIds.Length < 3) Save.lastCardIds = new string[3];
            Save.lastCardIds[Mathf.Clamp(idx, 0, 2)] = def.id;
            var result = _gm.ResolvePhase(slot);
            _gm.AutoActing = false;
            if (idx == 0 && result.HasValue)
            {
                // 밥: v3 규칙 — 체력 +1 보너스(주당 상한은 게이트 자체가 낮아 필요 없음), 스트레스 추가 회복
                //   74차: −5 → −2. 밥 한 칸으로 스트레스가 다 지워지면 관리할 게 없어진다(대신 주간 결산에서 잠 보너스가 붙는다).
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 2);
                Survival.OnRestAction(Save);   // 잠 + 식사는 MealPick에서 이미 소진
                if (!Save.ateThisWeek) ShowBubble(Loc.T("식사를 안 한 채 쉬었어…", "Rested without eating…"), 2.5f);
                _gm.Persist();
            }
            yield return new WaitForSecondsRealtime(0.25f);
            t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; _girl.color = new Color(1f, 1f, 1f, t / 0.35f); yield return null; }
            _girl.color = Color.white;
            if (result.HasValue)
            {
                var r = result.Value;
                var pz = PoseFor(def.id, r.outcome); if (pz != null) HoldPose(pz, 6f);
                LevelSystem.Add(r.outcome == Outcome.GreatSuccess ? LevelSystem.ExpActionGreat : r.outcome == Outcome.Fail ? LevelSystem.ExpActionFail : LevelSystem.ExpAction);   // 53차
                RefreshGirl(r.outcome == Outcome.GreatSuccess ? "Happy" : r.outcome == Outcome.Fail ? "Tired" : null);
                string line = r.logLines != null && r.logLines.Length > 0 ? r.logLines[r.logLines.Length - 1] : "";
                string head = r.outcome == Outcome.GreatSuccess ? Loc.T("대성공! ", "Great! ") : r.outcome == Outcome.Fail ? Loc.T("으으… ", "Ugh… ") : "";
                int dM = r.after.money - r.before.money, dS = r.after.stamina - r.before.stamina, dSt = r.after.stress - r.before.stress;
                string delta = (dM != 0 ? $" {dM:+#;-#}G" : "") + (dS != 0 ? $" 체력{dS:+#;-#}" : "") + (dSt != 0 ? $" 기운{-dSt:+#;-#}" : "") + (r.heartsGained > 0 ? $" ♥+{r.heartsGained}" : "");
                ShowBubble(head + (string.IsNullOrEmpty(line) ? def.Name : line) + delta, 3.2f);
                if (r.outcome == Outcome.GreatSuccess) SpawnHeart(3);
                // 105차(재미요소): 성장 순간 — 숙련 ★ 상승 · 스탯 10 단위 돌파 · 새 카드 해금
                if (_gm.LastMasteryUp > 0) { CoastToast.Show(Loc.T($"숙련 {RaisingFun.Stars(_gm.LastMasteryUp)} — {def.Name} +{Mathf.RoundToInt((RaisingFun.MasteryMul(_gm.LastMasteryUp) - 1f) * 100f)}%", $"Mastery {RaisingFun.Stars(_gm.LastMasteryUp)} — {def.Name}")); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.45f); SpawnHeart(2); }
                string ms = RaisingFun.Milestone(r.before, r.after);
                if (ms != null) { yield return new WaitForSecondsRealtime(1.6f); ShowBubble(ms, 3f); SpawnHeart(4); CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f); }
                var unlocked = RaisingFun.NewlyUnlocked(r.before, r.after, Timeline.SeasonOf(Save.week));
                if (unlocked.Count > 0) CoastToast.Show(Loc.T($"새 카드: {unlocked[0]}", $"New card: {unlocked[0]}") + (unlocked.Count > 1 ? $" +{unlocked.Count - 1}" : ""));
                // 105차(P1-2): 하트 문턱 → 일상 장면 카드
                if (_gm.LastDailyScene != null && _gm.LastDailyScene[0] >= 0)
                {
                    var ds = _gm.LastDailyScene; _gm.LastDailyScene = null;
                    yield return new WaitForSecondsRealtime(0.8f);
                    bool dsDone = false;
                    DailySceneUI.Show(ds[0], ds[1], () => dsDone = true);
                    while (!dsDone) yield return null;
                }
                // 105차(P2-1): 보류 단서의 조건이 방금 채워졌으면 단서 카드
                if (Save.cluePendingMask != 0)
                {
                    bool clueDone = false;
                    ClueSystem.CheckPending(Save, () => clueDone = true);
                    while (!clueDone) yield return null;
                }
            }
            Refresh();
            // 63차(사용자): 행동 3개를 다 하면 **바로 다음 턴**. 분홍 「다음 턴」 버튼은 행동이 남았을 때 건너뛰는 용도.
            if (Save.phaseIndex >= Timeline.PhasesPerWeek && !_auto)
            {
                yield return new WaitForSecondsRealtime(1.2f);
                ShowBubble(Loc.T("이번 주 행동 끝 — 다음 턴!", "Actions done — next turn!"), 2f);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return EndWeek();
            }
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
        }

        private float _nextTurnConfirmUntil;
        /// 55차-2: 다음 턴 버튼 — 행동이 남았으면 한 번 더 눌러 확인(6초 안), 다 했으면 바로.
        private void OnNextTurnPressed()
        {
            if (_busy || Save == null) return;
            CoastPrefs.Vibrate();
            if (Save.boundaryPending) { StartCoroutine(ResumeBoundary()); return; }   // 67차-8: 여기서 비로소 이야기·대회 팝업
            int left = Timeline.PhasesPerWeek - Save.phaseIndex;
            if (left > 0 && Time.unscaledTime > _nextTurnConfirmUntil)
            {
                _nextTurnConfirmUntil = Time.unscaledTime + 6f;
                ShowBubble(Loc.T($"행동이 {left}번 남았어. 그냥 넘어가려면 한 번 더 눌러.", $"{left} action(s) left. Press again to skip them."), 5f);
                return;
            }
            _nextTurnConfirmUntil = 0f;
            StartCoroutine(WeekendOnly());
        }

        /// 한 주 끝(행동 3번) — 기존 RaisingUI.ExecuteWeek 의 주말 처리 그대로: 주간 감쇠 → 사이드 씬 → 컨디션 → 챕터 경계(오프닝 → 게이트 → 러닝).
        private IEnumerator WeekendOnly()
        {
            _busy = true;
            foreach (var b in _actBtn) if (b != null) b.interactable = false;
            yield return EndWeek();
            foreach (var b in _actBtn) if (b != null) b.interactable = true;
            _busy = false;
        }

        private IEnumerator EndWeek()
        {
            // 111차: 이번 주말에 놀이(축제·격주 미니게임)를 띄웠으면, 주가 넘어간 뒤에도 돌발을 이어서 안 띄운다.
            bool playThisWeekend = false;
            // 105차(재미요소 P1-3): 계절 축제 주(6·14·21·42) — 안내 카드 → 미니게임 → 스탯 보너스로 등수 → 결과 카드. 이 주의 격주 미니게임은 대신한다.
            var fest = Festival.AtWeek(Save.week);
            if (fest != null && Festival.Place(Save, fest.index) == 0)
            {
                playThisWeekend = true;
                bool wasAutoF = _auto; _auto = false; RefreshAuto();
                bool go = false;
                FestivalUI.ShowIntro(fest, Save, () => go = true);
                while (!go) yield return null;
                bool? festRes = null;
                ChapterMissionUI.Play(fest.game, true, ok => festRes = ok);   // replay=true: 미션 보상·해금은 안 건드림(축제 보상은 Festival.Settle)
                while (festRes == null) yield return null;
                int place = Festival.Settle(_gm, fest, festRes == true);
                Save.weekMiniDone = Save.week; _gm.Persist();
                HoldPose(place <= 2 ? "Laugh" : "Cry", 4f);
                bool shown = false;
                FestivalUI.ShowResult(fest, place, () => shown = true);
                while (!shown) yield return null;
                Refresh();
                yield return new WaitForSecondsRealtime(0.5f);
                _auto = wasAutoF; RefreshAuto();
            }
            // 52차: 격주 주말 미니게임. 55차-2(사용자): **져도 다음으로 넘어간다** — 이기면 돈 보상(ChapterMissionUI 안에서 지급).
            if (StoryProgress.WeeklyMinigame(Save.week, out var miniKind) && Save.weekMiniDone < Save.week)
            {
                playThisWeekend = true;
                var md = ChapterMission.Get(miniKind);
                bool wasAuto = _auto; _auto = false; RefreshAuto();
                ShowBubble(Loc.T($"주말 미니게임 — {md.nameKo}! 이기면 {ChapterMission.Reward(_gm)}G.", $"Weekend mini-game — {md.nameEn}! Win for {ChapterMission.Reward(_gm)}G."), 2.5f);
                yield return new WaitForSecondsRealtime(1.2f);
                bool? miniRes = null;
                ChapterMissionUI.Play(miniKind, false, ok => miniRes = ok);
                while (miniRes == null) yield return null;
                Save.weekMiniDone = Save.week; _gm.Persist();
                if (miniRes == true)
                {
                    HoldPose("Laugh", 4f);
                    ShowBubble(Loc.T($"이겼다! +{ChapterMission.Reward(_gm)}G", $"Won! +{ChapterMission.Reward(_gm)}G"), 2.5f);
                    // Phase2: 주간 의식 — 미니게임 승리 시 마이룸 장식 드롭
                    var drop = RoomDeco.TryDropFromRun(_gm.Profile, Save.seed * 131 + Save.week * 17 + 7, 0.55f);
                    if (drop != null)
                    {
                        _gm.Persist();
                        CoastToast.Show(Loc.T($"마이룸 보너스 — 「{drop.Name}」!", $"My Room bonus — \"{drop.Name}\"!"));
                    }
                    else
                        CoastToast.Show(Loc.T("주말 미니게임 승리!", "Weekend mini-game win!"));
                }
                else { HoldPose("Cry", 3f); ShowBubble(Loc.T("졌지만 한 주는 지나간다.", "Lost, but the week moves on."), 2.5f); }
                Refresh();
                yield return new WaitForSecondsRealtime(0.8f);
                _auto = wasAuto; RefreshAuto();
            }
            // 55차(사용자): 한 턴(행동 3번) 끝 — 생활 결산(쌀·반찬·잠·옷·컨디션) → 「일주일이 지났다」 → 다음 턴.
            //   챕터 마지막 주였으면 다음 턴 시작에 컷씬(리더) → 대회(러닝)가 온다. 러닝 중엔 컷씬 없음.
            int fromWeek = Save.week;
            var rep = Survival.WeekTick(Save);
            bool forced = _gm.AdvanceWeek();
            _rubBudget = RubBudgetPerWeek;
            Refresh();
            string nextNote = null;
            if (rep.died) nextNote = Loc.T("…하늘이 일어나지 못한다.", "…Haneul can't get up.");
            else if (forced)
            {
                var c = StoryContest.Get(Save.chapter);
                nextNote = c != null
                    ? Loc.T($"다음 턴: 챕터 {Save.chapter} 이야기 → 대회 「{c.Name}」", $"Next turn: chapter {Save.chapter} story → contest \"{c.Name}\"")
                    : Loc.T($"다음 턴: 챕터 {Save.chapter} 이야기", $"Next turn: chapter {Save.chapter} story");
            }
            bool passDone = false;
            _auto = _auto && !rep.died; RefreshAuto();   // 135차: 챕터 경계(forced)에서도 자동 유지 — 끌 때까지 계속
            WeekPassUI.Show(fromWeek, Save.week, Timeline.SeasonOf(Save.week), rep, nextNote, () => passDone = true);
            // 74차(사용자: 결산 카드 3초): 자동 진행에서도 카드가 제 시간(WeekPassUI.AutoCloseSeconds)만큼 보이게 —
            //   전엔 1.4초에 강제로 닫아 자동 모드에서만 더 빨리 사라졌다. 여유 0.3초는 자동 닫힘이 못 돌 때의 안전장치.
            if (_auto) { float w = 0f, lim = WeekPassUI.AutoCloseSeconds + 0.3f; while (!passDone && w < lim) { w += Time.unscaledDeltaTime; yield return null; } if (!passDone) WeekPassUI.Close(); }
            else while (!passDone) yield return null;
            if (rep.died)
            {
                _auto = false; if (Save != null) Save.autoMode = false; RefreshAuto();
                Save.boundaryPending = false; _gm.Persist();
                bool revived = false;
                GameOverUI.Show(_gm, _girl != null ? _girl.sprite : null, () => revived = true);
                while (!revived && GameOverUI.IsOpen) yield return null;   // 「처음부터」는 씬이 바뀌므로 여기서 끝
                if (revived) { HoldPose("Cry", 5f); Refresh(); ShowBubble(Loc.T("…병원에서 깨어났다. 장부터 보자.", "…Woke up in hospital. Shop first."), 4f); }
                yield break;
            }
            ShowBubble(Loc.T($"{Save.week}주차 아침이야.", $"Week {Save.week} morning."), 1.5f);
            // 옛 SIDE 미니컷씬 큐가 세이브에 남아 있으면 재생 없이 보상만 주고 비움.
            if (!string.IsNullOrEmpty(_gm.PendingSideScene))
            {
                string side = _gm.PendingSideScene; _gm.PendingSideScene = null;
                int lvl = side.EndsWith("_3") ? 3 : side.EndsWith("_2") ? 2 : 1;
                Affinity.Reward(Save, lvl); _gm.Persist(); Refresh();
            }
            if (!string.IsNullOrEmpty(_gm.PendingWeekNote))
            {
                ShowBubble(_gm.PendingWeekNote, 3f);
                CoastToast.Show(_gm.PendingWeekNote);
                _gm.PendingWeekNote = null;
                Refresh();
                if (Save.forfeitPending) { _auto = false; Save.autoMode = false; RefreshAuto(); _gm.ForfeitChapter(); yield break; }
                yield return new WaitForSecondsRealtime(_auto ? 0.5f : 1.5f);
            }
            if (forced) { ShowBubble(BoundaryHint(), 4f); yield break; }   // 67차-8: 바로 띄우지 않고 「다음 턴」을 기다린다(동그라미 3개 찬 상태)
            // 111차: 놀이(축제·격주 미니게임)를 방금 띄웠으면 돌발은 다음 기회에 — 동시에 안 올라오게.
            if (playThisWeekend) yield break;
            var ev = _gm.PeekRandomEvent();
            if (ev != null) yield return EventChoiceRoutine(ev);
        }

        /// 55차: 챕터 경계 — **다음 턴 시작**에 실행. 레벨 게이트 → (1장이면 프롤로그) → 이야기(리더) → 러닝 없는 챕터면 완료 /
        ///   러닝 챕터면 체력 게이트 → 대회 안내 → 러닝. 대회에 지면 GameManager.ContestFail 로 돌아와 이 주를 다시 키운다.
        private IEnumerator BoundaryRoutine()
        {
            if (Save == null || !Save.boundaryPending) yield break;
            bool autoWas = _auto; _auto = false; RefreshAuto();   // 135차: 이야기·대회 동안만 잠시 끄고, 끝나면 복원
            // K-POP = 돈·아이템 파밍. 스토리 컷씬/엔딩은 레벨로 잠그지 않는다(예전 롱컷 Lv 게이트 제거).
            if (Save.chapter == 1 && !Save.prologueSeen)
            {
                // 55차: 프롤로그는 러닝 앞이 아니라 여기(첫 이야기 앞)에서. 68차: 오프닝 시네마틱(M3)으로 — 「PRO」 VN 대신.
                TitleAudio.StopMenuGlobal();
                bool donePro = false;
                OpeningCinematic.Play(() => donePro = true);
                while (!donePro) yield return null;
                Save.prologueSeen = true; _gm.Persist();
                TitleAudio.PlayRaising();
            }
            // 61차(사용자): 컷씬은 8개. 85차(대본 v4): 컷씬 챕터는 StoryProgress.CutsceneChapters(1·3·5·7·10·12·17·20) — 러닝 챕터와 다르다.
            int cut = StoryProgress.CutsceneIndex(Save.chapter);
            if (cut > 0 && !StoryProgress.CutsceneRead(cut))
            {
                ShowBubble(Loc.T($"컷씬 {cut} — 이야기.", $"Cutscene {cut} — story time."), 1.5f);
                yield return new WaitForSecondsRealtime(0.8f);
                bool doneVn = false;
                // 68차: 컷씬은 시네마틱(스틸+자막+음악, 오프닝 형식)으로 본다. 소설식 리더는 시네마 메뉴 「읽기」에 남는다.
                if (CinematicTable.Cutscene(cut) != null) { CinematicPlayer.Play("CS" + cut, () => { StoryProgress.MarkCutsceneSeen(cut); doneVn = true; }); }
                else StoryReaderUI.OpenCutscene(cut, () => doneVn = true);
                while (!doneVn) yield return null;
                LevelSystem.Add(LevelSystem.ExpChapterRead);   // 53차: 이야기 한 편 = 경험치
                // 85차: 컷씬 직후 「단서」 카드(하트·라디오·돌·이름·머리띠) — 엔딩 분기(ClueSystem)
                bool doneClue = false;
                ClueSystem.ShowAfterScene(Save, "CS" + cut, () => doneClue = true);
                while (!doneClue) yield return null;
                Refresh();
            }
            // 85차: 보조 컷씬 EV1~10(CH2·4·6·8·9·11·13·14·15·19) — 4컷 × 7초 짧은 이야기. EV9 뒤엔 편지 단서 카드.
            int ev = StoryProgress.EventIndex(Save.chapter);
            if (ev > 0 && !StoryProgress.EventSeen(ev) && CinematicTable.Event(ev) != null)
            {
                ShowBubble(Loc.T($"이야기 — 「{StoryProgress.EventTitle(ev)}」", $"Story — '{StoryProgress.EventTitle(ev)}'"), 1.5f);
                yield return new WaitForSecondsRealtime(0.8f);
                bool doneEv = false;
                CinematicPlayer.Play("EV" + ev, () => { StoryProgress.MarkEventSeen(ev); doneEv = true; });
                while (!doneEv) yield return null;
                LevelSystem.Add(LevelSystem.ExpChapterRead / 2);
                // 중간 이벤트 직후 선택 beat (패시브 시네마 → 손맛)
                bool doneBeat = false;
                StoryEventBeat.ShowAfterEvent(Save, ev, () => doneBeat = true);
                while (!doneBeat) yield return null;
                bool doneClue = false;
                ClueSystem.ShowAfterScene(Save, "EV" + ev, () => doneClue = true);
                while (!doneClue) yield return null;
                Refresh();
            }
            // 52차: 러닝은 이벤트(52주에 8번, StoryProgress.RunChapters) — 러닝 없는 챕터는 이야기를 읽은 것으로 넘어간다.
            if (!StoryProgress.IsRunChapter(Save.chapter))
            {
                int done = Save.chapter;
                _gm.CompleteChapterNoRun();
                Refresh();
                int nextCut = 0; foreach (var cc in StoryProgress.CutsceneChapters) if (cc > done) { nextCut = StoryProgress.CutsceneIndex(cc); break; }
                ShowBubble(Loc.T($"{done}장이 지나갔어. 이제 {Save.chapter}장, {Save.week}주차." + (nextCut > 0 ? $" 컷씬 {nextCut}은 {StoryProgress.CutsceneChapter(nextCut)}장에서." : ""), $"Chapter {done} done. Now chapter {Save.chapter}, week {Save.week}."), 3.5f);
                CoastToast.Show(Loc.T($"챕터 {done} 완료", $"Chapter {done} complete"));
                if (autoWas || Save.autoMode) { _auto = true; _autoTimer = -2f; RefreshAuto(); }
                yield break;
            }
            if (StoryGate.Passes(Save))
            {
                // 55차: 러닝 = 이야기와 무관한 마을 대회 — 안내 카드 → 출발
                bool go = false;
                ContestIntroUI.Show(StoryContest.Get(Save.chapter), () => go = true);
                while (!go) yield return null;
                ShowBubble(Loc.T("가자, 대회장으로!", "To the race!"), 1.5f);
                yield return new WaitForSecondsRealtime(0.6f);
                _gm.StartStoryRun();
                yield break;
            }
            _gm.GateFail();
            Refresh();
            ShowBubble(StoryGate.FailText(Save), 4f);
            CoastToast.Show(Loc.T("아직 못 달려 — 한 주 더 키우자", "Not yet — one more week"));
            if (autoWas || Save.autoMode) { _auto = true; _autoTimer = -2f; RefreshAuto(); }
        }

        /// 53차(사용자): 마이룸(방 꾸미기 HomeUI) — 주인공 꼬마와 펫이 같이 있다.
        private Image _gachaBadge; private Text _gachaBadgeT;
        /// 131차: 뽑기 버튼 배지 — 오늘 무료가 남았거나 뽑기권이 있으면 표시.
        private void RefreshGachaBadge()
        {
            if (_gachaBadge == null || Save == null) return;
            bool free = StarGacha.FreePullReady(_gm != null ? _gm.Profile : null);
            int tickets = Save.capsuleTickets;
            bool show = free || tickets > 0 || Save.starShards >= StarGacha.PullCost;
            _gachaBadge.gameObject.SetActive(show);
            if (show) _gachaBadgeT.text = tickets > 0 ? tickets.ToString() : "!";
        }

        private void OpenRoom()
        {
            if (_busy || Save == null || _gm == null) return;
            CoastPrefs.Vibrate();
            _busy = true;
            HomeUI.Open(_gm, _girl != null ? _girl.sprite : null, () =>
            {
                _busy = false;
                RoomDeco.ClearAllNew(_gm.Profile);
                _gm.WriteProfileNow();
                Refresh();
            });
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
