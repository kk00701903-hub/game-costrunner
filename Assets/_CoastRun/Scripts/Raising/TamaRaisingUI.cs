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
        private Image _bubbleBg, _staminaFill, _energyFill, _gateMark;
        private Text _gateFlag;
        private Text _staminaTxt, _energyTxt;
        private Image _lifeEdge, _lifeBand; private Text _lifeIcon;   // 63차: 생활 경고 띠
        private readonly Button[] _actBtn = new Button[3];
        private readonly Text[] _actDots = new Text[3];
        private readonly Image[] _actRing = new Image[3]; private readonly Text[] _actCheck = new Text[3];   // 60차: 이번 주 행동 3칸(큰 동그라미)
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
            LevelSystem.FlushPending();   // 53차: 세이브 없이 K-POP 에서 모은 경험치 합치기
            Refresh();
            ShowBubble(Loc.T("오늘도 힘내자!", "Let's do our best today!"), 2.5f);
            _gm.OnSaveChanged -= OnSaveChanged; _gm.OnSaveChanged += OnSaveChanged;
            // 55차: 지난 턴이 챕터 마지막 주로 끝났으면(앱을 껐다 켰어도) 이번 턴 시작에 컷씬·대회부터.
            if (Save != null && Save.boundaryPending) StartCoroutine(ResumeBoundary());
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
            // 53차(사용자): 주차 아래 [상태창] [마이룸] 세로 버튼 — 레벨 배지가 상태 버튼에
            // 60차(사용자 시안): 주차 알약에 ✦, 왼쪽 버튼 3개는 아이콘 + 글자
            Sparkle(wk.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -12f), 14); Sparkle(wk.rectTransform, new Vector2(1f, 0f), new Vector2(-30f, 10f), 10);
            // 63차(사용자 시안): 둘째 줄 한 줄에 [★ 상태창(둥근)] [🏠 마이룸] [🛒 장보기(둥근)] ○ ○ ○
            var stBtn = CoastUiArt.GlossyPill(_root, "StatusBtn", new Color(0.55f, 0.40f, 0.95f), 30, 6);
            Anchor(stBtn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(156f, -84f), new Vector2(62f, 62f)); stBtn.raycastTarget = true;
            SideIcon(stBtn.rectTransform, "Icon_Star", 34f, 14f);
            _levelLabel = CoastHudLayout.MakeText(stBtn.rectTransform, "T", "", 10, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(0f, 0f));
            _levelLabel.color = Color.white; _levelLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_levelLabel, new Color(0f, 0f, 0f, 0.5f), 1f);
            _levelLabel.color = Color.white; _levelLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_levelLabel, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            var sb = stBtn.gameObject.AddComponent<Button>(); sb.transition = Selectable.Transition.None;
            sb.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); StatusUI.Open(_gm, Refresh); });
            var roomBtn = CoastUiArt.GlossyPill(_root, "RoomBtn", new Color(0.30f, 0.70f, 0.55f), 20, 6);
            Anchor(roomBtn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(226f, -84f), new Vector2(140f, 62f)); roomBtn.raycastTarget = true;
            SideIcon(roomBtn.rectTransform, "Icon_Home", 28f, 14f); Sparkle(roomBtn.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -12f), 12);
            var rl = CoastHudLayout.MakeText(roomBtn.rectTransform, "T", Loc.T("마이룸", "My Room"), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(40f, 3f), new Vector2(-6f, 0f));
            rl.color = Color.white; rl.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(rl, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            var rb = roomBtn.gameObject.AddComponent<Button>(); rb.transition = Selectable.Transition.None;
            rb.onClick.AddListener(OpenRoom);
            // 55차(사용자): 장보기(쌀·반찬·옷) 버튼 + 생활 알약(쌀·반찬·옷·배부름·컨디션)
            var shopBtn = CoastUiArt.GlossyPill(_root, "ShopBtn", new Color(0.95f, 0.60f, 0.25f), 30, 6);
            Anchor(shopBtn.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(376f, -84f), new Vector2(62f, 62f)); shopBtn.raycastTarget = true;
            SideIcon(shopBtn.rectTransform, "Icon_Cart", 34f, 14f);
            var shb = shopBtn.gameObject.AddComponent<Button>(); shb.transition = Selectable.Transition.None;
            shb.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); GroceryUI.Open(_gm, Refresh); });
            // 63차(시안): 생활 경고는 카드 위 **가로 띠**(노란 테두리 + 빨간 바탕 + ⚠) — 경고가 없으면 남색 띠에 생활 요약
            _lifeEdge = CoastUiArt.CutePill(_root, "LifeEdge", new Color(1f, 0.85f, 0.20f), 20, 3);
            Anchor(_lifeEdge.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 282f), new Vector2(596f, 70f)); _lifeEdge.raycastTarget = false;
            _lifeBand = CoastUiArt.Panel(_lifeEdge.transform, "Band", new Color(0.86f, 0.14f, 0.20f), 16); _lifeBand.raycastTarget = false;
            Anchor(_lifeBand.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); _lifeBand.rectTransform.offsetMin = new Vector2(5f, 5f); _lifeBand.rectTransform.offsetMax = new Vector2(-5f, -5f);
            _lifeIcon = CoastHudLayout.MakeText(_lifeBand.rectTransform, "W", "⚠", 30, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(62f, 0f));
            _lifeIcon.color = new Color(1f, 0.88f, 0.25f); _lifeIcon.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_lifeIcon, new Color(0f, 0f, 0f, 0.5f), 1.4f);
            _lifeLabel = CoastHudLayout.MakeText(_lifeBand.rectTransform, "T", "", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-16f, 0f));
            _lifeLabel.color = Color.white; _lifeLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_lifeLabel, new Color(0f, 0f, 0f, 0.45f), 1.4f);
            _lifeLabel.resizeTextForBestFit = true; _lifeLabel.resizeTextMinSize = 10; _lifeLabel.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            _lifeLabel.horizontalOverflow = HorizontalWrapMode.Wrap; _lifeLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _lifeLabel.resizeTextForBestFit = true; _lifeLabel.resizeTextMinSize = 9; _lifeLabel.resizeTextMaxSize = CoastHudLayout.Scaled(13);
            _weekLabel = CoastHudLayout.MakeText(wk.rectTransform, "T", "", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-10f, 0f));
            _weekLabel.color = Color.white; CoastUiArt.OutlineText(_weekLabel, new Color(0f, 0f, 0f, 0.4f), 1.2f);
            var money = CoastUiArt.CutePill(_root, "Money", new Color(0.98f, 0.84f, 0.42f), 18, 3);   // 60차: 금색 알약
            Anchor(money.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-146f, -6f), new Vector2(200f, 60f));
            Sparkle(money.rectTransform, new Vector2(0f, 1f), new Vector2(12f, -10f), 10); Sparkle(money.rectTransform, new Vector2(1f, 0f), new Vector2(-12f, 10f), 10);
            // 52차(사용자): 펫 상점 버튼(20주차부터 열림 · 코인+젤리)
            var pet = CoastUiArt.GlossyPill(_root, "PetBtn", new Color(0.95f, 0.55f, 0.30f), 18, 6);
            Anchor(pet.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -6f), new Vector2(64f, 60f)); pet.raycastTarget = true;
            var petT = CoastHudLayout.MakeText(pet.rectTransform, "T", Loc.T("펫", "Pet"), 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            petT.color = Color.white; petT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(petT, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            var pb = pet.gameObject.AddComponent<Button>(); pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(() => { if (_busy) return; CoastPrefs.Vibrate(); PetShopUI.Open(_gm, Refresh); });
            _moneyLabel = CoastHudLayout.MakeText(money.rectTransform, "T", "", 20, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-18f, 0f));
            _moneyLabel.color = Navy; _moneyLabel.fontStyle = FontStyle.Bold; _moneyLabel.alignment = TextAnchor.MiddleCenter;
            _moneyLabel.resizeTextForBestFit = true; _moneyLabel.resizeTextMinSize = 11; _moneyLabel.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            // 60차: 오른쪽 위 큰 동그라미 3개 = 이번 주 행동(한 번 하면 초록 ✓)
            for (int i = 0; i < 3; i++)
            {
                var ring = CoastUiArt.Panel(_root, "ActRing" + i, new Color(0.62f, 0.64f, 0.70f, 0.95f), 44);
                Anchor(ring.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-176f + i * 72f, -115f), new Vector2(62f, 62f)); ring.rectTransform.pivot = new Vector2(0.5f, 0.5f); ring.raycastTarget = false;
                var inner = CoastUiArt.Panel(ring.transform, "In", new Color(0.30f, 0.32f, 0.40f, 0.55f), 26); inner.raycastTarget = false;
                Anchor(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f)); inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var ck = CoastHudLayout.MakeText(ring.rectTransform, "Ck", "✓", 32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                ck.color = Color.white; ck.fontStyle = FontStyle.Bold; ck.raycastTarget = false; CoastUiArt.OutlineText(ck, new Color(0f, 0f, 0f, 0.3f), 1.5f);
                _actRing[i] = ring; _actCheck[i] = ck;
            }
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
            _gateLabel = CoastHudLayout.MakeText(_root, "Gate", "", 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 254f), new Vector2(0f, 280f));
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
            ab.onClick.AddListener(() => { _auto = !_auto; _autoTimer = 0f; RefreshAuto(); ShowBubble(_auto ? Loc.T("내가 알아서 할게!", "I'll take care of myself!") : Loc.T("같이 하자.", "Let's do it together."), 2f); });
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
            _moneyLabel.text = $"{LevelSystem.FormatK(s.money)}G  ♥{Save.chapterHearts}";   // 53차: 1000 단위 k
            if (_levelLabel != null) _levelLabel.text = $"Lv{Mathf.Max(1, Save.level)}";   // 63차: 둥근 ★ 버튼 아래 작은 레벨
            int need = StoryGate.Required(Save);
            // 57차(사용자 「체력바 확인」): 「123 / 36」이 헷갈렸다 → 막대 = 체력/최대(200), 게이트 자리에 흰 눈금, 글자 = 「체력 123 · 게이트 36 ✓」
            float gx = Mathf.Clamp01(need / (float)PlayerStats.StatMax);
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
            int done = Save.phaseIndex;
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
                case "rest_home": return "Sleep";
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
            float ratio = st.stamina > 0 ? st.stress / (float)st.stamina : 2f;
            string key = moodKey ?? (ratio < 0.4f ? "Happy" : ratio < 0.7f ? "Normal" : "Tired");
            string sfx = SeasonLook.Suffix(Timeline.SeasonOf(Save.week));
            string pose = _pose != null && Time.unscaledTime < _poseUntil ? _pose : (moodKey == null && ratio >= 0.95f ? "Angry" : null);
            var tex = (pose != null ? ArtAssets.LoadTexture("Raise_Girl_Pose_" + pose) : null)
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
                if (_autoTimer >= 1.2f) { _autoTimer = 0f; if (Save.phaseIndex >= Timeline.PhasesPerWeek) StartCoroutine(WeekendOnly()); else DoAction(AutoPick()); }
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
            // 55차-2: 행동 3번을 다 했으면 「다음 턴」 버튼으로 넘어간다
            if (Save.phaseIndex >= Timeline.PhasesPerWeek) { ShowBubble(Loc.T("이번 주 행동은 다 했어 — 「다음 턴」을 눌러!", "All actions done — press Next turn!"), 2f); return; }
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
            HoldPose(PoseFor(def.id, null), 30f);   // 52차: 활동 포즈로 돌아온다
            var result = _gm.ResolvePhase(slot);
            if (idx == 0 && result.HasValue)
            {
                // 밥: v3 규칙 — 체력 +1 보너스(주당 상한은 게이트 자체가 낮아 필요 없음), 스트레스 −5 추가
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 5);
                Survival.OnRestAction(Save);   // 55차: 밥/휴식 = 이번 주 잠을 잤다(+쌀이 있으면 배부름)
                if (Save.rice <= 0) ShowBubble(Loc.T("쌀이 없어… 맨입으로 잤다.", "No rice… slept hungry."), 2.5f);
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
            // 52차: 격주 주말 미니게임. 55차-2(사용자): **져도 다음으로 넘어간다** — 이기면 돈 보상(ChapterMissionUI 안에서 지급).
            if (StoryProgress.WeeklyMinigame(Save.week, out var miniKind) && Save.weekMiniDone < Save.week)
            {
                var md = ChapterMission.Get(miniKind);
                bool wasAuto = _auto; _auto = false; RefreshAuto();
                ShowBubble(Loc.T($"주말 미니게임 — {md.nameKo}! 이기면 {ChapterMission.Reward(_gm)}G.", $"Weekend mini-game — {md.nameEn}! Win for {ChapterMission.Reward(_gm)}G."), 2.5f);
                yield return new WaitForSecondsRealtime(1.2f);
                bool? miniRes = null;
                ChapterMissionUI.Play(miniKind, false, ok => miniRes = ok);
                while (miniRes == null) yield return null;
                Save.weekMiniDone = Save.week; _gm.Persist();
                if (miniRes == true) { HoldPose("Laugh", 4f); ShowBubble(Loc.T($"이겼다! +{ChapterMission.Reward(_gm)}G", $"Won! +{ChapterMission.Reward(_gm)}G"), 2.5f); }
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
            _rubBudget = 10;
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
            _auto = _auto && !forced && !rep.died; RefreshAuto();
            WeekPassUI.Show(fromWeek, Save.week, Timeline.SeasonOf(Save.week), rep, nextNote, () => passDone = true);
            if (_auto) { float w = 0f; while (!passDone && w < 1.4f) { w += Time.unscaledDeltaTime; yield return null; } if (!passDone) WeekPassUI.Close(); }
            else while (!passDone) yield return null;
            if (rep.died)
            {
                _auto = false; RefreshAuto();
                Save.boundaryPending = false; _gm.Persist();
                bool revived = false;
                GameOverUI.Show(_gm, _girl != null ? _girl.sprite : null, () => revived = true);
                while (!revived && GameOverUI.IsOpen) yield return null;   // 「처음부터」는 씬이 바뀌므로 여기서 끝
                if (revived) { HoldPose("Cry", 5f); Refresh(); ShowBubble(Loc.T("…병원에서 깨어났다. 장부터 보자.", "…Woke up in hospital. Shop first."), 4f); }
                yield break;
            }
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
            if (forced) { yield return BoundaryRoutine(); yield break; }
            var ev = _gm.RollRandomEvent();
            if (ev.HasValue) ShowEvent(ev.Value);
        }

        /// 55차: 챕터 경계 — **다음 턴 시작**에 실행. 레벨 게이트 → (1장이면 프롤로그) → 이야기(리더) → 러닝 없는 챕터면 완료 /
        ///   러닝 챕터면 체력 게이트 → 대회 안내 → 러닝. 대회에 지면 GameManager.ContestFail 로 돌아와 이 주를 다시 키운다.
        private IEnumerator BoundaryRoutine()
        {
            if (Save == null || !Save.boundaryPending) yield break;
            _auto = false; RefreshAuto();
            // 53차(사용자): 롱컷씬(4·7·10·13·15)과 엔딩(20)은 **레벨**이 되어야 열린다 — 젤리·행동·러닝으로 경험치를 모으자(K-POP 11챕터+의 열쇠).
            if ((StoryProgress.IsLongCut(Save.chapter) || Save.chapter == Timeline.Chapters) && !LevelSystem.LongCutOpen(Save.chapter))
            {
                int needLv = LevelSystem.LevelForLongCut(Save.chapter);
                _gm.GateFail();
                Refresh();
                ShowBubble(Loc.T($"이 이야기는 Lv {needLv} 이 되어야 열려 (지금 Lv {LevelSystem.Level}). 젤리와 달리기로 경험치를 모으자!", $"This story opens at Lv {needLv} (now Lv {LevelSystem.Level}). Gather EXP with jelly and runs!"), 5f);
                CoastToast.Show(Loc.T($"롱컷씬 잠김 — Lv {needLv} 필요 · 한 주 더", $"Long cut locked — Lv {needLv} needed · one more week"));
                yield break;
            }
            if (Save.chapter == 1 && !Save.prologueSeen)
            {
                // 55차: 프롤로그 VN 은 러닝 앞이 아니라 여기(첫 이야기 앞)에서
                bool donePro = false;
                ChapterVN.Play("PRO", () => donePro = true);
                while (!donePro) yield return null;
                Save.prologueSeen = true; _gm.Persist();
            }
            // 61차(사용자): 컷씬은 8개 — 러닝 챕터(1·4·7·10·13·15·18·20)에 닿았을 때 그 사이 챕터 이야기를 한 편으로 읽는다. 다른 챕터는 그냥 지나간다.
            int cut = StoryProgress.CutsceneIndex(Save.chapter);
            if (cut > 0 && !StoryProgress.CutsceneRead(cut))
            {
                ShowBubble(Loc.T($"컷씬 {cut} — 이야기를 읽자.", $"Cutscene {cut} — story time."), 1.5f);
                yield return new WaitForSecondsRealtime(0.8f);
                bool doneVn = false;
                StoryReaderUI.OpenCutscene(cut, () => doneVn = true);
                while (!doneVn) yield return null;
                LevelSystem.Add(LevelSystem.ExpChapterRead);   // 53차: 이야기 한 편 = 경험치
            }
            // 52차: 러닝은 이벤트(52주에 8번, StoryProgress.RunChapters) — 러닝 없는 챕터는 이야기를 읽은 것으로 넘어간다.
            if (!StoryProgress.IsRunChapter(Save.chapter))
            {
                int done = Save.chapter;
                _gm.CompleteChapterNoRun();
                Refresh();
                int nextCut = 0; foreach (var rc in StoryProgress.RunChapters) if (rc > done) { nextCut = StoryProgress.CutsceneIndex(rc); break; }
                ShowBubble(Loc.T($"{done}장이 지나갔어. 이제 {Save.chapter}장, {Save.week}주차." + (nextCut > 0 ? $" 컷씬 {nextCut}은 {StoryProgress.CutsceneChapter(nextCut)}장에서." : ""), $"Chapter {done} done. Now chapter {Save.chapter}, week {Save.week}."), 3.5f);
                CoastToast.Show(Loc.T($"챕터 {done} 완료", $"Chapter {done} complete"));
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
        }

        /// 53차(사용자): 마이룸(방 꾸미기 HomeUI) — 주인공 꼬마와 펫이 같이 있다.
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
