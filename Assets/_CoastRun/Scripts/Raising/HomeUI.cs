using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 집 화면 — 전체 화면 오버레이.
    ///   [방]     주인공·꼬마·선택 펫 2D 고정 표시(배치물·완성도 없음).
    ///   [텃밭]   화분 3개: 씨앗 심기(G) → 주마다 성장 → 수확(성공 확률).
    ///   [놀이]   스토리 미션과 같은 놀이(구슬치기·윷·투호·딱지·무궁화).
    public class HomeUI : MonoBehaviour
    {
        public enum Tab { Room, Balcony, Play }

        private static readonly Color Navy = new Color(0.23f, 0.16f, 0.29f);
        private static readonly Color Ink = new Color(0.29f, 0.21f, 0.31f);
        private static readonly Color Coral = new Color(1f, 0.44f, 0.57f);
        private static readonly Color Mint = new Color(0.30f, 0.71f, 0.67f);
        private static readonly Color Sky = new Color(0.39f, 0.71f, 0.96f);
        private static readonly Color Sun = new Color(1f, 0.72f, 0.30f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.90f);
        private static readonly Color Grey = new Color(0.62f, 0.63f, 0.70f);
        private static readonly Color TabOff = new Color(0.78f, 0.72f, 0.93f);   // 94차 시안: 비선택 탭 라벤더 · 126차: 시안 라벤더로

        private GameManager _gm;
        private Sprite _charSprite;
        private Action _onClose;
        private Canvas _canvas;
        private RectTransform _root, _body, _tray, _tabBar;
        private RectTransform _bodyFrame, _trayFrame;   // 117차: 탭마다 본문/트레이 비율을 바꾼다
        private ScrollRect _trayScroll;
        private RectTransform _trayGhost;
        private Text _money, _hint;
        private readonly Image[] _tabImgs = new Image[3];
        private Tab _tab = Tab.Room;

        // 방
        private RectTransform _roomHost, _decoLayer, _charRt, _kidRt;
        private Image _charImg, _kidImg;
        private Vector2 _charPos = new Vector2(0.42f, 0.12f);
        private Vector2 _kidPos = new Vector2(0.58f, 0.11f);
        private readonly Dictionary<string, RectTransform> _placed = new Dictionary<string, RectTransform>();
        private string _dragging;
        private const bool ShowRoomGirl = true;    // 마이룸: 주인공 + 펫(고정 포즈)
        private const bool ShowRoomKid = false;    // 94차 시안: 방에는 하늘이만 (꼬마는 컷씬·이벤트에서)
        private bool _sleepPreview;                // 94차: 「침대 모양」 = 잠자는 포즈 미리보기 토글
        private RectTransform _petRt; private Image _petImg; private Vector2 _petPos;

        // 베란다
        private int _selectedPot = -1;
        private readonly List<RectTransform> _potRoots = new List<RectTransform>();

        private SaveData Save => _gm != null ? _gm.Save : null;
        private MetaProfile Profile => _gm != null ? _gm.Profile : null;

        public static HomeUI Open(GameManager gm, Sprite charSprite, Action onClose)
        {
            var go = new GameObject("HomeUI");
            var ui = go.AddComponent<HomeUI>();
            ui._gm = gm; ui._charSprite = charSprite; ui._onClose = onClose;
            ui.Build();
            return ui;
        }

        // ── 뼈대 ─────────────────────────────────────────────────────────

        private void Build()
        {
            HomeData.Ensure(Profile);
            HomeData.EnsurePots(Save);
            _canvas = CoastUiCanvas.Create("HomeCanvas", 130, transform);
            _root = CoastUiCanvas.Root(_canvas);

            // 94차(시안 「우리 집」): 어두운 딤 → 크림·라벤더 바탕 + 꽃 테두리 프레임
            // 126차(사용자 시안): 위 진보라 → 아래 라벤더 그라데이션 바탕 + 라벤더 프레임
            var dimTex = new Texture2D(1, 3, TextureFormat.RGBA32, false);
            dimTex.SetPixels(new[] { new Color(0.84f, 0.78f, 0.95f), new Color(0.62f, 0.50f, 0.82f), new Color(0.36f, 0.24f, 0.56f) });
            dimTex.wrapMode = TextureWrapMode.Clamp; dimTex.filterMode = FilterMode.Bilinear; dimTex.Apply();
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), Color.white);
            dim.sprite = Sprite.Create(dimTex, new Rect(0, 0, 1, 3), new Vector2(0.5f, 0.5f), 1f); dim.type = Image.Type.Simple; dim.raycastTarget = true;
            var frame = CoastUiArt.Panel(_root, "Frame", new Color(0.80f, 0.72f, 0.94f), 26);
            Rect(frame.rectTransform, Vector2.zero, Vector2.one, new Vector2(-6f, -6f), new Vector2(6f, 6f)); frame.raycastTarget = false;
            var frameIn = CoastUiArt.Panel(frame.transform, "In", new Color(0.99f, 0.97f, 0.93f), 22);
            Rect(frameIn.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f)); frameIn.raycastTarget = false;
            var decoHost = new GameObject("FrameDeco", typeof(RectTransform)).GetComponent<RectTransform>();
            decoHost.SetParent(_root, false);
            Rect(decoHost, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            SprinklePlayDecor(decoHost);

            // 헤더 — 시안: 보라 알약 + 집 아이콘 「우리 집」 · 코인 아이콘 + 돈 · 「나가기」
            var head = CoastUiArt.CutePill(_root, "Head", new Color(0.50f, 0.36f, 0.72f), 22, 4);   // 126차 시안: 보라
            Rect(head.rectTransform, new Vector2(0f, 0.925f), new Vector2(1f, 1f), new Vector2(10f, 4f), new Vector2(-10f, -6f));
            var homeIc = CoastUiArt.Icon("Home");
            if (homeIc != null)
            {
                var hi = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                hi.transform.SetParent(head.transform, false); hi.sprite = homeIc; hi.preserveAspect = true; hi.raycastTarget = false; hi.color = Color.white;
                hi.rectTransform.anchorMin = hi.rectTransform.anchorMax = new Vector2(0f, 0.5f); hi.rectTransform.pivot = new Vector2(0f, 0.5f);
                hi.rectTransform.anchoredPosition = new Vector2(18f, 0f); hi.rectTransform.sizeDelta = new Vector2(40f, 40f);
            }
            var title = Text(head.transform, "Title", Loc.T("우리 집", "Home"), 30, Color.white, TextAnchor.MiddleLeft);   // 109차: 글자 키움
            Rect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(homeIc != null ? 68f : 24f, 0f), new Vector2(-320f, 0f));
            title.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(title, new Color(0f, 0f, 0f, 0.4f), 1.5f);
            var coinIc = CoastUiArt.Icon("Coin");
            if (coinIc != null)
            {
                var ci = new GameObject("Coin", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ci.transform.SetParent(head.transform, false); ci.sprite = coinIc; ci.preserveAspect = true; ci.raycastTarget = false;
                ci.rectTransform.anchorMin = ci.rectTransform.anchorMax = new Vector2(1f, 0.5f); ci.rectTransform.pivot = new Vector2(1f, 0.5f);
                ci.rectTransform.anchoredPosition = new Vector2(-150f, 0f); ci.rectTransform.sizeDelta = new Vector2(30f, 30f);
            }
            _money = Text(head.transform, "Money", "", 24, new Color(1f, 0.88f, 0.50f), TextAnchor.MiddleRight);   // 109차: 글자 키움
            _money.fontStyle = FontStyle.Bold;
            Rect(_money.rectTransform, Vector2.zero, Vector2.one, new Vector2(200f, 0f), new Vector2(-196f, 0f));
            var closeB = Button(head.transform, "Close", Loc.T("나가기", "Leave"), new Color(0.72f, 0.64f, 0.94f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(150f, 56f), Close);   // 109차: 버튼 키움 · 126차: 라벤더
            { var ct0 = closeB.GetComponentInChildren<Text>(); if (ct0 != null) { ct0.resizeTextMaxSize = CoastHudLayout.Scaled(22); ct0.fontSize = CoastHudLayout.Scaled(22); } }

            // 탭 — 헤더 바로 아래, 클릭 보장(Fill에도 raycast)
            _tabBar = new GameObject("Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
            _tabBar.SetParent(_root, false);
            Rect(_tabBar, new Vector2(0f, 0.855f), new Vector2(1f, 0.918f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            string[] names = { Loc.T("방", "Room"), Loc.T("텃밭", "Garden"), Loc.T("놀이", "Play") };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var pill = CoastUiArt.CutePill(_tabBar, "Tab" + i, TabOff, 16, 3);
                Rect(pill.rectTransform, new Vector2(i / 3f, 0f), new Vector2((i + 1) / 3f, 1f), new Vector2(i == 0 ? 0f : 4f, 0f), new Vector2(i == 2 ? 0f : -4f, 0f));
                pill.raycastTarget = true;
                foreach (var im in pill.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.targetGraphic = pill;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); SetTab((Tab)idx); });
                var t = Text(pill.transform, "T", names[i], 24, Color.white, TextAnchor.MiddleCenter);   // 109차: 탭 글자 키움
                t.raycastTarget = false; t.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                _tabImgs[i] = pill;
            }
            _tabBar.SetAsLastSibling();

            // 본문(장면) + 트레이 — 탭과 겹치지 않게 본문 상단을 살짝 내림
            var bodyFrame = CoastUiArt.CutePill(_root, "BodyFrame", new Color(0.93f, 0.84f, 0.74f), 22, 4);
            _bodyFrame = bodyFrame.rectTransform;
            Rect(bodyFrame.rectTransform, new Vector2(0f, 0.272f), new Vector2(1f, 0.848f), new Vector2(10f, 0f), new Vector2(-10f, 0f));   // 109차: 트레이(씨앗·펫 카드)를 키워 본문은 조금 줄임
            _body = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _body.SetParent(bodyFrame.transform, false);
            Rect(_body, Vector2.zero, Vector2.one, new Vector2(7f, 10f), new Vector2(-7f, -7f));

            var trayFrame = CoastUiArt.CutePill(_root, "TrayFrame", new Color(1f, 0.97f, 0.90f), 22, 4);
            _trayFrame = trayFrame.rectTransform;
            Rect(trayFrame.rectTransform, new Vector2(0f, 0.012f), new Vector2(1f, 0.264f), new Vector2(10f, 0f), new Vector2(-10f, 0f));   // 109차: 트레이 키움
            _tray = new GameObject("Tray", typeof(RectTransform)).GetComponent<RectTransform>();
            _tray.SetParent(trayFrame.transform, false);
            Rect(_tray, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            _trayScroll = null;

            SetTab(Tab.Room);
            _tabBar.SetAsLastSibling();
        }

        /// 136차: 마을 텃밭에서 바로 텃밭 탭으로.
        public void OpenBalcony() { SetTab(Tab.Balcony); }

        private void SetTab(Tab t)
        {
            _tab = t;
            // 117차(사용자 시안): 놀이 탭은 카드가 화면을 꽉 채우고 안내는 아래 띠 한 줄.
            if (_bodyFrame != null && _trayFrame != null)
            {
                bool playTab = t == Tab.Play;
                Rect(_bodyFrame, new Vector2(0f, playTab ? 0.118f : 0.272f), new Vector2(1f, 0.848f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
                Rect(_trayFrame, new Vector2(0f, 0.012f), new Vector2(1f, playTab ? 0.110f : 0.264f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            }
            for (int i = 0; i < 3; i++)
            {
                var fill = _tabImgs[i].transform.Find("Fill")?.GetComponent<Image>();
                var lip = _tabImgs[i].transform.Find("Lip")?.GetComponent<Image>();
                Color c = i == (int)t ? (i == 0 ? new Color(1f, 0.66f, 0.78f) : i == 1 ? Mint : Sun) : TabOff;   // 126차 시안: 방 탭 분홍
                if (fill != null) fill.color = c;
                if (lip != null) lip.color = Color.Lerp(c, Color.black, 0.45f);
            }
            Clear(_body); Clear(_tray);
            _placed.Clear(); _potRoots.Clear(); _selectedPot = -1;
            _petRt = null; _petImg = null; _charRt = null; _charImg = null; _kidRt = null; _kidImg = null;
            switch (t)
            {
                case Tab.Room: BuildRoom(); BuildRoomTray(); break;
                case Tab.Balcony: BuildBalcony(); BuildSeedTray(); break;
                case Tab.Play: BuildPlay(); BuildPlayTray(); break;
            }
            RefreshMoney();
            if (_tabBar != null) _tabBar.SetAsLastSibling();
        }

        private void RefreshMoney()
        {
            if (_money != null && Save != null) _money.text = LevelSystem.FormatK(Save.stats.money);   // 53차: k 표기 · 94차: 코인 아이콘이 있어 숫자만
        }

        private void Close()
        {
            _gm?.Persist(); _gm?.WriteProfileNow();
            var cb = _onClose; _onClose = null;
            Destroy(gameObject);
            cb?.Invoke();
        }

        // ── 방 ───────────────────────────────────────────────────────────

        private void BuildRoom()
        {
            _roomHost = _body;
            // 배경(방 그림) — 여백 없이 본문 전체를 cover로 채운다
            var bgMask = new GameObject("BgMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            bgMask.transform.SetParent(_roomHost, false);
            Rect(bgMask.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bgMask.GetComponent<Image>().color = new Color(0.63f, 0.53f, 0.50f);
            bgMask.GetComponent<Mask>().showMaskGraphic = true;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
            bg.transform.SetParent(bgMask.transform, false);
            var brt = bg.rectTransform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var fit = bg.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // 여백 없이 가득
            fit.aspectRatio = 1f;
            // 38차: 쿼터뷰(아이소메트릭) 방 그림(UI_Room_Iso)
            var iso = ArtAssets.LoadTexture("UI_Room_Cozy") ?? ArtAssets.LoadTexture("UI_Room_Iso");   // 128차: 클링 AI 웹에서 뽑은 아늑한 방(시안 ②) 우선
            var art = iso ?? ArtAssets.LoadTexture("UI_Raising_Room_" + Timeline.SeasonOf(Save != null ? Save.week : 1)) ?? ArtAssets.LoadTexture("UI_Raising_Room");
            if (iso != null)
            {
                fit.aspectRatio = (float)iso.width / Mathf.Max(1, iso.height);
                bgMask.GetComponent<Image>().color = new Color(0.22f, 0.16f, 0.14f);
            }
            if (art != null) bg.sprite = CoastUiArt.AsSprite(art); bg.raycastTarget = false;
            // 바닥 탭(장식 드래그용 투명 판 — 캐릭터는 고정)
            var floor = CoastHudLayout.MakeImage(_roomHost, "FloorTap", new Vector2(0f, 0f), new Vector2(1f, 0.46f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            floor.raycastTarget = false;
            _decoLayer = new GameObject("Deco", typeof(RectTransform)).GetComponent<RectTransform>();
            _decoLayer.SetParent(_roomHost, false);
            Rect(_decoLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 주인공 + 꼬마(+펫) — 방 중앙에 나란히 고정
            if (ShowRoomGirl)
            {
                _charRt = new GameObject("Girl", typeof(RectTransform)).GetComponent<RectTransform>();
                _charRt.SetParent(_roomHost, false);
                _charRt.anchorMin = _charRt.anchorMax = new Vector2(0.42f, 0.12f); _charRt.pivot = new Vector2(0.5f, 0f);
                _charRt.sizeDelta = iso != null ? new Vector2(150f, 214f) : new Vector2(210f, 300f);
                var sh = CoastHudLayout.MakeImage(_charRt, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-64f, -4f), new Vector2(64f, 12f), new Color(0f, 0f, 0f, 0.22f));
                sh.sprite = CoastUiArt.RoundedRect(30); sh.type = Image.Type.Sliced;
                _charImg = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                _charImg.transform.SetParent(_charRt, false);
                Rect(_charImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _charImg.preserveAspect = true; _charImg.raycastTarget = false;
                var sleepTex = _sleepPreview ? ArtAssets.LoadTexture("Raise_Girl_Pose_Sleep") : null;
                var chibiTex = ArtAssets.LoadTexture("UI_Haneul_Stand");   // 126차: 러닝 캐릭터(치비 하늘)와 같은 얼굴로 — 블렌더 렌더(투명 PNG)
                if (sleepTex != null) _charImg.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(sleepTex));
                else if (chibiTex != null)
                {
                    // 129차(클링 AI 웹 생성, 마젠타 키 제거한 RGBA): 시안처럼 러그 한가운데에 크게 세운다
                    _charImg.sprite = CoastUiArt.AsSprite(chibiTex);
                    _charRt.anchorMin = _charRt.anchorMax = new Vector2(0.50f, 0.05f);
                    _charRt.sizeDelta = new Vector2(250f, 440f);
                    _charPos = new Vector2(0.50f, 0.05f);
                }
                else if (_charSprite != null) _charImg.sprite = _charSprite;
                else
                {
                    var girlTex = ArtAssets.LoadTexture("Raise_Girl_Normal")
                                  ?? ArtAssets.LoadTexture("Raise_Girl_Happy")
                                  ?? ArtAssets.LoadTexture("UI_Face_Girl");
                    if (girlTex != null) _charImg.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(girlTex));
                    else _charImg.color = new Color(1f, 0.8f, 0.6f);
                }
                if (chibiTex == null || _sleepPreview) _charPos = new Vector2(0.42f, 0.12f);

                // 꼬마 — 주인공 오른쪽 (94차: 시안대로 방에서는 숨김)
                if (ShowRoomKid) {
                _kidRt = new GameObject("Kid", typeof(RectTransform)).GetComponent<RectTransform>();
                _kidRt.SetParent(_roomHost, false);
                _kidRt.anchorMin = _kidRt.anchorMax = new Vector2(0.58f, 0.11f); _kidRt.pivot = new Vector2(0.5f, 0f);
                _kidRt.sizeDelta = iso != null ? new Vector2(118f, 168f) : new Vector2(160f, 230f);
                var ksh = CoastHudLayout.MakeImage(_kidRt, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-48f, -3f), new Vector2(48f, 10f), new Color(0f, 0f, 0f, 0.2f));
                ksh.sprite = CoastUiArt.RoundedRect(24); ksh.type = Image.Type.Sliced;
                _kidImg = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                _kidImg.transform.SetParent(_kidRt, false);
                Rect(_kidImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _kidImg.preserveAspect = true; _kidImg.raycastTarget = false;
                var kidTex = ArtAssets.LoadTexture("Raise_Kid")
                             ?? ArtAssets.LoadTexture("MG_Token_Kid")
                             ?? ArtAssets.LoadTexture("UI_Face_Kid");
                if (kidTex != null) _kidImg.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(kidTex));
                else _kidImg.color = new Color(0.85f, 0.72f, 0.55f);
                _kidPos = new Vector2(0.58f, 0.11f);
                }

                // 장착 펫 — 주인공 왼쪽
                var petKind = Save != null ? Save.equippedPet : PetKind.None;
                if (petKind != PetKind.None)
                {
                    _petRt = new GameObject("Pet", typeof(RectTransform)).GetComponent<RectTransform>();
                    _petRt.SetParent(_roomHost, false);
                    _petRt.anchorMin = _petRt.anchorMax = new Vector2(0.28f, 0.10f); _petRt.pivot = new Vector2(0.5f, 0f);
                    _petRt.sizeDelta = new Vector2(96f, 96f);
                    var psh = CoastHudLayout.MakeImage(_petRt, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-34f, -3f), new Vector2(34f, 8f), new Color(0f, 0f, 0f, 0.2f));
                    psh.sprite = CoastUiArt.RoundedRect(20); psh.type = Image.Type.Sliced;
                    _petImg = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    _petImg.transform.SetParent(_petRt, false);
                    Rect(_petImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    _petImg.preserveAspect = true; _petImg.raycastTarget = false;
                    var petTex = ArtAssets.LoadTexture("UI_Pet_" + petKind) ?? ArtAssets.LoadTexture("Obs_Pet_" + petKind);
                    if (petTex != null) _petImg.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(petTex));
                    _petPos = new Vector2(0.28f, 0.10f);
                }
            }
            RefreshPlaced();
            BuildRoomChrome();
        }

        /// 94차(시안 「방」): 위 「★ 방 완성도 N/14」 게이지 + 오른쪽 세로 버튼 3개(침대 모양 · 방 장식 꾸미기 · 간식 바꾸기).
        private const int RoomGoal = 14;
        private int RoomScore()
        {
            int n = 0;
            if (Save != null) foreach (var k in PetShop.ForSale) if (PetShop.Owns(Save, k)) n++;
            if (Profile != null) n += RoomDeco.OwnedCount(Profile);
            if (Save != null) { LifeItems.Ensure(Save); if (LifeItems.HasEdible(Save)) n++; }
            return Mathf.Clamp(n, 0, RoomGoal);
        }
        private void BuildRoomChrome()
        {
            // 완성도 게이지 (크림 알약 + 노랑 바 + 별)
            var bar = CoastUiArt.CutePill(_roomHost, "Done", new Color(1f, 0.97f, 0.82f, 0.97f), 16, 3); bar.raycastTarget = false;   // 126차 시안: 노란 크림
            Rect(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            bar.rectTransform.pivot = new Vector2(0.5f, 1f); bar.rectTransform.anchoredPosition = new Vector2(0f, -10f); bar.rectTransform.sizeDelta = new Vector2(620f, 64f);
            int score = RoomScore();
            var pct = Text(bar.transform, "Pct", $"{Mathf.RoundToInt(100f * score / RoomGoal)}%", 22, new Color(0.78f, 0.52f, 0.10f), TextAnchor.MiddleRight); pct.fontStyle = FontStyle.Bold;   // 126차 시안: 오른쪽 퍼센트
            Rect(pct.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-96f, 0f), new Vector2(-12f, 0f));
            var star = Text(bar.transform, "Star", "★", 22, new Color(1f, 0.72f, 0.15f), TextAnchor.MiddleCenter);
            Rect(star.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(44f, 0f));
            var lb = Text(bar.transform, "T", Loc.T($"방 완성도 {score}/{RoomGoal}", $"Room {score}/{RoomGoal}"), 20, PlayInk, TextAnchor.MiddleLeft); lb.fontStyle = FontStyle.Bold;   // 109차: 글자 키움
            Rect(lb.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(48f, 0f), new Vector2(-12f, -2f));
            var track = CoastUiArt.Panel(bar.transform, "Track", new Color(0.93f, 0.88f, 0.72f), 8); track.raycastTarget = false;
            Rect(track.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(48f, 8f), new Vector2(-100f, -4f));
            var fill = CoastUiArt.Panel(track.transform, "Fill", new Color(1f, 0.80f, 0.25f), 8); fill.raycastTarget = false;
            Rect(fill.rectTransform, new Vector2(0f, 0f), new Vector2(Mathf.Clamp01((float)score / RoomGoal), 1f), new Vector2(2f, 2f), new Vector2(0f, -2f));
            var sp = Text(track.transform, "Sp", "★", 14, new Color(1f, 0.72f, 0.15f), TextAnchor.MiddleCenter);
            Rect(sp.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, -12f), new Vector2(-4f, 12f));

            // 109차(사용자): 「침대 모양 · 방 장식 · 간식 바꾸기」 버튼은 모두 없애고, 펫이 있으면 **펫 명령 2택**(펫 특성별, 주 1회).
            BuildPetCommands();
        }

        /// 109차: 오른쪽 세로 — 「펫 명령 · 참새」 머리 + 명령 버튼 2개(큰 글씨). 펫이 없으면 상점으로 가는 회색 알약 하나.
        private void BuildPetCommands()
        {
            var petKind = Save != null ? Save.equippedPet : PetKind.None;
            var cmds = PetCommands.For(petKind);
            var ink = new Color(0.40f, 0.26f, 0.08f);
            if (petKind == PetKind.None || cmds == null)
            {
                var b0 = CoastUiArt.GlossyPill(_roomHost, "NoPet", new Color(0.80f, 0.78f, 0.86f), 18, 6);
                var r0 = b0.rectTransform; r0.anchorMin = r0.anchorMax = new Vector2(1f, 0.5f); r0.pivot = new Vector2(1f, 0.5f);
                r0.anchoredPosition = new Vector2(-10f, 60f); r0.sizeDelta = new Vector2(210f, 92f); b0.raycastTarget = true;
                foreach (var im in b0.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
                var t0 = Text(r0, "T", Loc.T("펫이 없어요\n상점에서 데려오기 →", "No pet yet\nGet one at the shop →"), 18, new Color(0.30f, 0.28f, 0.38f), TextAnchor.MiddleCenter);
                t0.fontStyle = FontStyle.Bold; t0.raycastTarget = false; t0.resizeTextForBestFit = true; t0.resizeTextMinSize = 12; t0.resizeTextMaxSize = CoastHudLayout.Scaled(18);
                Rect(t0.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -2f));
                var bb0 = b0.gameObject.AddComponent<Button>(); bb0.transition = Selectable.Transition.None; bb0.targetGraphic = b0;
                bb0.onClick.AddListener(() => { CoastPrefs.Vibrate(); ShopUI.Open(_gm, 1, () => { if (_tab == Tab.Room) SetTab(Tab.Room); }); });
                return;
            }
            bool used = PetCommands.UsedThisWeek(Save);
            string petName = PetCompanion.Names[Mathf.Clamp((int)petKind, 0, PetCompanion.Names.Length - 1)];
            // 머리 알약
            var head = CoastUiArt.CutePill(_roomHost, "PetCmdHead", new Color(1f, 0.98f, 0.93f, 0.97f), 14, 3); head.raycastTarget = false;
            var hrt = head.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 0.5f); hrt.pivot = new Vector2(1f, 0.5f);
            hrt.anchoredPosition = new Vector2(-10f, 150f); hrt.sizeDelta = new Vector2(230f, 46f);
            var ht = Text(hrt, "T", used ? Loc.T($"{petName} 명령 · 이번 주 완료", $"{petName} · done this week") : Loc.T($"{petName} 명령 · 주 1회", $"{petName} command · 1/week"), 17, PlayInk, TextAnchor.MiddleCenter);
            ht.fontStyle = FontStyle.Bold; ht.raycastTarget = false; ht.resizeTextForBestFit = true; ht.resizeTextMinSize = 11; ht.resizeTextMaxSize = CoastHudLayout.Scaled(17);
            Rect(ht.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, 0f));
            for (int i = 0; i < cmds.Length && i < 2; i++)
            {
                int idx = i; var c = cmds[i];
                // 126차(사용자 시안): 크림 카드 + 라벤더 테두리, 아이콘은 위 가운데, 제목·설명 두 줄
                var b = CoastUiArt.Panel(_roomHost, "PetCmd" + i, used ? new Color(0.84f, 0.82f, 0.88f) : new Color(0.82f, 0.74f, 0.95f), 18);
                var brt = b.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(1f, 0.5f); brt.pivot = new Vector2(1f, 0.5f);
                brt.anchoredPosition = new Vector2(-10f, 96f - i * 160f); brt.sizeDelta = new Vector2(236f, 148f); b.raycastTarget = true;
                var bIn = CoastUiArt.Panel(brt, "In", used ? new Color(0.95f, 0.94f, 0.96f) : new Color(1f, 0.98f, 0.93f), 15); bIn.raycastTarget = true;
                Rect(bIn.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
                var ic = CoastUiArt.Art(c.icon);
                if (ic != null)
                {
                    var ii = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    ii.transform.SetParent(brt, false); ii.sprite = ic; ii.preserveAspect = true; ii.raycastTarget = false; ii.color = i == 0 ? new Color(0.98f, 0.70f, 0.20f) : new Color(0.55f, 0.40f, 0.25f);
                    ii.rectTransform.anchorMin = ii.rectTransform.anchorMax = new Vector2(0.5f, 1f); ii.rectTransform.pivot = new Vector2(0.5f, 1f);
                    ii.rectTransform.anchoredPosition = new Vector2(0f, -10f); ii.rectTransform.sizeDelta = new Vector2(40f, 40f);
                }
                var nm = Text(brt, "N", c.Name, 19, ink, TextAnchor.MiddleCenter);
                nm.fontStyle = FontStyle.Bold; nm.raycastTarget = false; nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = CoastHudLayout.Scaled(19);
                Rect(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -86f), new Vector2(-8f, -52f));
                var bl = Text(brt, "B", c.Blurb, 15, i == 0 ? new Color(0.32f, 0.55f, 0.22f) : new Color(0.40f, 0.30f, 0.16f), TextAnchor.MiddleCenter);
                bl.raycastTarget = false; bl.resizeTextForBestFit = true; bl.resizeTextMinSize = 10; bl.resizeTextMaxSize = CoastHudLayout.Scaled(15); bl.horizontalOverflow = HorizontalWrapMode.Wrap;
                Rect(bl.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 8f), new Vector2(-8f, 62f));
                var btn = b.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None; btn.targetGraphic = b;
                btn.onClick.AddListener(() =>
                {
                    CoastPrefs.Vibrate();
                    string msg = PetCommands.Run(_gm, petKind, idx, out bool ok);
                    CoastToast.Show(msg);
                    if (ok) { CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.6f); if (_petRt != null) StartCoroutine(PopIn(_petRt)); SetTab(Tab.Room); }
                });
            }
        }
        private Vector2 ToNorm(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_roomHost, screen, null, out var local);
            var r = _roomHost.rect;
            return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        }

        private void RefreshPlaced()
        {
            if (_decoLayer == null) return;
            if (_charRt != null) _charRt.SetParent(_roomHost, false);
            if (_kidRt != null) _kidRt.SetParent(_roomHost, false);
            Clear(_decoLayer); _placed.Clear();
            var p = Profile; HomeData.Ensure(p);
            // 등대·별조명 등 배치물 연출 제거 — 마이룸은 캐릭터만
            if (_charRt != null) _charRt.SetParent(_decoLayer, false);
            if (_kidRt != null) _kidRt.SetParent(_decoLayer, false);
            if (_petRt != null) _petRt.SetParent(_decoLayer, false);
            ReorderDepth();
        }

        /// 놓을 때 팡 — 0.35초 스케일 바운스 + 반짝이 네 알.
        private System.Collections.IEnumerator PopIn(RectTransform rt)
        {
            if (rt == null) yield break;
            float t = 0f;
            var sparks = new List<RectTransform>();
            for (int i = 0; i < 6; i++)
            {
                var s = CoastUiArt.Panel(rt, "Spark", new Color(1f, 0.95f, 0.6f, 0.95f), 4);
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); s.rectTransform.sizeDelta = new Vector2(8f, 8f);
                sparks.Add(s.rectTransform);
            }
            while (t < 0.45f && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / 0.35f);
                float sc = u < 0.6f ? Mathf.Lerp(0.3f, 1.18f, u / 0.6f) : Mathf.Lerp(1.18f, 1f, (u - 0.6f) / 0.4f);
                rt.localScale = new Vector3(sc, sc, 1f);
                for (int i = 0; i < sparks.Count; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad; float r = 30f + 70f * u;
                    sparks[i].anchoredPosition = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r + 20f);
                    sparks[i].GetComponent<Image>().color = new Color(1f, 0.95f, 0.6f, 1f - u);
                }
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
            foreach (var s in sparks) if (s != null) Destroy(s.gameObject);
        }

        /// 방 탭 하단 — 펫 선택 + 조리 버튼.
        private void BuildRoomTray()
        {
            // 94차 시안: 안내 문구 없이 펫 카드 줄 + 오른쪽 아래 큰 노랑 「조리!!」 버튼
            var cook = CoastUiArt.GlossyPill(_tray, "Cook", new Color(1f, 0.80f, 0.30f), 18, 6);
            cook.raycastTarget = true;
            foreach (var im in cook.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
            Rect(cook.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 4f), new Vector2(-6f, 70f));   // 109차: 버튼 키움 · 126차 시안: 전폭
            var ct = Text(cook.transform, "T", Loc.T("조리!!", "Cook!!"), 30, new Color(0.62f, 0.38f, 0.08f), TextAnchor.MiddleCenter);   // 109차: 글자 키움
            ct.fontStyle = FontStyle.Bold; ct.raycastTarget = false;
            var cb = cook.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.targetGraphic = cook;
            cb.onClick.AddListener(() => { CoastPrefs.Vibrate(); InventoryUI.Open(_gm, () => { if (_tab == Tab.Room) { Clear(_tray); BuildRoomTray(); RefreshMoney(); } }, cookMode: true); });

            _trayScroll = MakeHScroll(_tray, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(4f, 74f), new Vector2(-4f, -4f), out var content);
            const float cw = 150f, gap = 10f;   // 109차: 펫 카드 키움 · 126차 시안: 4장이 한 줄에 다 보이게
            float x = 4f;
            int n = 0;
            foreach (var k in PetShop.ForSale)
            {
                if (Save == null || !PetShop.Owns(Save, k)) continue;
                var tex = ArtAssets.LoadTexture("UI_Pet_" + k) ?? ArtAssets.LoadTexture("Obs_Pet_" + k);
                AddPetCard(content, x, k, PetCompanion.Names[(int)k], tex, cw);
                x += cw + gap;
                n++;
            }
            if (n == 0)
            {
                var empty = Text(_tray, "Empty", Loc.T("상점에서 펫을 데려오세요", "Get a pet from the shop"), 14, Grey, TextAnchor.MiddleCenter);
                Rect(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 70f), new Vector2(-12f, -8f));
            }
            content.sizeDelta = new Vector2(Mathf.Max(x, cw), 0f);
        }

        private void AddPetCard(RectTransform content, float x, PetKind k, string name, Texture2D tex, float cw)
        {
            bool on = (Save != null ? Save.equippedPet : PetKind.None) == k;
            var chip = CoastUiArt.CutePill(content, "Pet_" + k, on ? new Color(1f, 0.88f, 0.50f) : new Color(0.90f, 0.86f, 0.98f), 14, 3);   // 126차 시안: 선택 노랑 / 라벤더
            chip.raycastTarget = true;
            foreach (var im in chip.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
            var crt = chip.rectTransform;
            crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 0.5f);
            crt.anchoredPosition = new Vector2(x, 0f); crt.sizeDelta = new Vector2(cw, 0f);

            // 하단 이름 띠
            const float labelH = 40f;   // 109차: 이름 띠·글자 키움
            var labelBg = CoastUiArt.Panel(crt, "LabelBg", on ? new Color(1f, 0.92f, 0.70f) : new Color(0.86f, 0.84f, 0.94f), 10);
            Rect(labelBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 4f), new Vector2(-4f, labelH));
            labelBg.raycastTarget = false;

            if (tex != null)
            {
                var pi = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                pi.transform.SetParent(crt, false);
                pi.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(tex));
                pi.preserveAspect = true; pi.raycastTarget = false;
                // 카드 안 정사각에 가깝게 — 비율 유지, 늘리지 않음
                Rect(pi.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                pi.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                pi.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                pi.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                pi.rectTransform.anchoredPosition = new Vector2(0f, labelH * 0.5f);
                pi.rectTransform.sizeDelta = new Vector2(cw - 16f, -(labelH + 10f));
            }
            var t = Text(crt, "T", on ? "★ " + name : name, 20, Navy, TextAnchor.MiddleCenter);   // 109차: 글자 키움
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(17);
            CoastUiArt.OutlineText(t, new Color(1f, 1f, 1f, 0.9f), 1.4f);
            Rect(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 4f), new Vector2(-6f, labelH));
            var pk = k;
            var b = chip.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = chip;
            b.onClick.AddListener(() => SelectPet(pk));
        }

        private void SelectPet(PetKind k)
        {
            if (Save == null) return;
            if (k == PetKind.None || !PetShop.Owns(Save, k)) { CoastToast.Show(Loc.T("아직 없는 펫이야.", "You don't own that pet.")); return; }
            PetShop.Equip(Save, k);
            PetCompanion.Selected = k;
            RunTuning.Pet = k;
            _gm.Persist();
            CoastToast.Show(Loc.T($"{PetCompanion.Names[(int)k]} 선택! 러닝에 같이 나와.", $"{PetCompanion.Names[(int)k]} selected — joins runs."));
            if (_tab == Tab.Room) SetTab(Tab.Room);
        }

        private void BeginTrayGhost(DecoDef d)
        {
            ClearTrayGhost();
            _trayGhost = new GameObject("TrayGhost", typeof(RectTransform)).GetComponent<RectTransform>();
            _trayGhost.SetParent(_root, false);
            _trayGhost.pivot = new Vector2(0.5f, 0f);
            _trayGhost.sizeDelta = HomeData.Size(d) * 0.85f;
            var vis = DecoVisual(_trayGhost, d, _trayGhost.sizeDelta, true);
            Rect(vis.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foreach (var g in _trayGhost.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            CanvasGroup cg = _trayGhost.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.alpha = 0.92f;
        }

        private void MoveTrayGhost(Vector2 screenPos)
        {
            if (_trayGhost == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local);
            _trayGhost.anchorMin = _trayGhost.anchorMax = new Vector2(0.5f, 0.5f);
            _trayGhost.anchoredPosition = local;
        }

        private void EndTrayGhost(DecoDef d, Vector2 screenPos)
        {
            ClearTrayGhost();
            if (_roomHost == null || d == null) return;
            // 방(_body) 위에 떨어뜨렸을 때만 배치
            if (!RectTransformUtility.RectangleContainsScreenPoint(_roomHost, screenPos, null))
            {
                CoastToast.Show(Loc.T("방 안으로 끌어다 놓아.", "Drop it inside the room."));
                return;
            }
            var p = Profile;
            if (!HomeData.Owns(p, d))
            {
                if (d.FromRun || !HomeData.TryBuy(Save, p, d)) { CoastToast.Show(Loc.T("G가 모자라.", "Not enough G.")); return; }
                _gm.Persist();
                CoastToast.Show(Loc.T($"{d.Name} 구매!", $"Bought {d.Name}!"));
            }
            if (HomeData.IsPlaced(p, d)) HomeData.Remove(p, d.id);
            var n = HomeData.ClampPos(d, ToNorm(screenPos));
            if (HomeData.NearSpot(d.id, n)) n = HomeData.Spot(d.id);
            HomeData.Place(p, d, n.x, n.y);
            _gm.WriteProfileNow();
            RefreshPlaced(); Clear(_tray); BuildRoomTray(); RefreshMoney();
            if (_placed.TryGetValue(d.id, out var prt)) StartCoroutine(PopIn(prt));
        }

        private void ClearTrayGhost()
        {
            if (_trayGhost != null) { Destroy(_trayGhost.gameObject); _trayGhost = null; }
        }

        private void RoomAct(DecoDef d)
        {
            var p = Profile; string pop = null;
            if (HomeData.IsPlaced(p, d)) { HomeData.Remove(p, d.id); CoastToast.Show(Loc.T($"{d.Name}를 치웠어.", $"Removed {d.Name}.")); }
            else if (HomeData.Owns(p, d))
            {
                var pos = HomeData.Spot(d.id);   // 31차: 제자리로 팡
                HomeData.Place(p, d, pos.x, pos.y);
                pop = d.id;
            }
            else if (!d.FromRun)
            {
                if (!HomeData.TryBuy(Save, p, d)) { CoastToast.Show(Loc.T("G가 모자라.", "Not enough G.")); return; }
                var pos = HomeData.Spot(d.id);
                HomeData.Place(p, d, pos.x, pos.y);
                _gm.Persist();
                CoastToast.Show(Loc.T($"{d.Name} 구매!", $"Bought {d.Name}!"));
                pop = d.id;
            }
            _gm.WriteProfileNow();
            RefreshPlaced(); Clear(_tray); BuildRoomTray(); RefreshMoney();
            if (pop != null && _placed.TryGetValue(pop, out var prt)) StartCoroutine(PopIn(prt));
        }

        // ── 베란다 ───────────────────────────────────────────────────────

        // ── 텃밭: 제주 바다 농장 시안(화분 5 · 말풍선 · 씨앗 카드) ──
        private static readonly Vector2[] PotSpots =
        {
            new Vector2(0.22f, 0.10f), new Vector2(0.50f, 0.06f), new Vector2(0.78f, 0.10f)
        };
        private void BuildBalcony()
        {
            var bgMask = new GameObject("BgMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            bgMask.transform.SetParent(_body, false);
            Rect(bgMask.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bgMask.GetComponent<Image>().color = new Color(0.35f, 0.55f, 0.40f);
            bgMask.GetComponent<Mask>().showMaskGraphic = true;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
            bg.transform.SetParent(bgMask.transform, false);
            var brt = bg.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var fit = bg.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            var yard = ArtAssets.LoadTexture("UI_Tama_Yard")
                       ?? ArtAssets.LoadTexture("BG_OrangeFarm")
                       ?? ArtAssets.LoadTexture("UI_Raising_Room");
            if (yard != null) { fit.aspectRatio = (float)yard.width / Mathf.Max(1, yard.height); bg.sprite = CoastUiArt.AsSprite(yard); }
            bg.raycastTarget = false;

            // 상단 배너 — 94차 시안: 크림 알약 + 양쪽 꽃 + 갈색 글자
            var banner = CoastUiArt.CutePill(_body, "Banner", new Color(1f, 0.96f, 0.90f, 0.96f), 16, 3);
            Rect(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            banner.rectTransform.pivot = new Vector2(0.5f, 1f);
            banner.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            banner.rectTransform.sizeDelta = new Vector2(470f, 48f);
            banner.raycastTarget = false;
            PlaceCornerFlower(banner.rectTransform, new Vector2(0.05f, 0.5f), new Color(0.55f, 0.78f, 0.50f));
            PlaceCornerFlower(banner.rectTransform, new Vector2(0.95f, 0.5f), new Color(0.95f, 0.55f, 0.72f));
            var head = Text(banner.transform, "Head", Loc.T("텃밭 - 제주 바다 농장", "Garden - Jeju Sea Farm"), 19, PlayInk, TextAnchor.MiddleCenter);
            head.fontStyle = FontStyle.Bold;
            Rect(head.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 0f), new Vector2(-36f, 0f));
            CoastUiArt.OutlineText(head, new Color(1f, 1f, 1f, 0.55f), 1.2f);

            _potRoots.Clear();
            for (int i = 0; i < HomeData.PotCount; i++)
            {
                var root = new GameObject("Pot" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                root.SetParent(_body, false);
                root.anchorMin = root.anchorMax = PotSpots[i]; root.pivot = new Vector2(0.5f, 0f);
                root.anchoredPosition = Vector2.zero; root.sizeDelta = new Vector2(118f, 200f);
                root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); root.GetComponent<Image>().raycastTarget = true;
                int pi = i;
                var b = root.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => PotTapped(pi));
                _potRoots.Add(root);
            }
            RefreshPots();
        }

        private void RefreshPots()
        {
            var s = Save; HomeData.EnsurePots(s);
            for (int i = 0; i < _potRoots.Count; i++)
            {
                var root = _potRoots[i]; Clear(root);
                int stage = HomeData.Stage(s, i);
                var pot = s.pots[i]; var seed = HomeData.Seed(pot.seed);
                bool sel = _selectedPot == i;
                var sh = CoastUiArt.Panel(root, "Shadow", new Color(0f, 0f, 0f, 0.22f), 20);
                Rect(sh.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                sh.rectTransform.pivot = new Vector2(0.5f, 0.5f); sh.rectTransform.anchoredPosition = new Vector2(0f, 4f); sh.rectTransform.sizeDelta = new Vector2(96f, 22f);
                var body = CoastUiArt.Panel(root, "Pot", sel ? new Color(0.98f, 0.62f, 0.45f) : new Color(0.78f, 0.42f, 0.28f), 14);
                Rect(body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                body.rectTransform.pivot = new Vector2(0.5f, 0f); body.rectTransform.anchoredPosition = new Vector2(0f, 4f); body.rectTransform.sizeDelta = new Vector2(72f, 48f);
                var rim = CoastUiArt.Panel(root, "Rim", new Color(0.92f, 0.58f, 0.42f), 10);
                Rect(rim.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                rim.rectTransform.pivot = new Vector2(0.5f, 0f); rim.rectTransform.anchoredPosition = new Vector2(0f, 48f); rim.rectTransform.sizeDelta = new Vector2(84f, 12f);
                var soil = CoastUiArt.Panel(root, "Soil", new Color(0.35f, 0.22f, 0.14f), 8);
                Rect(soil.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                soil.rectTransform.pivot = new Vector2(0.5f, 0f); soil.rectTransform.anchoredPosition = new Vector2(0f, 52f); soil.rectTransform.sizeDelta = new Vector2(70f, 8f);
                if (seed != null) DrawPlant(root, seed, stage);

                // 말풍선 (시안)
                string bubble = PotBubbleText(seed, pot, stage, sel);
                var bub = CoastUiArt.Panel(root, "Bubble", new Color(1f, 0.98f, 0.94f, 0.96f), 14);
                Rect(bub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                bub.rectTransform.pivot = new Vector2(0.5f, 1f);
                bub.rectTransform.anchoredPosition = new Vector2(0f, -4f);
                bub.rectTransform.sizeDelta = new Vector2(148f, 64f);
                var tip = CoastUiArt.Panel(bub.transform, "Tip", new Color(1f, 0.98f, 0.94f, 0.96f), 6);
                Rect(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                tip.rectTransform.pivot = new Vector2(0.5f, 1f); tip.rectTransform.anchoredPosition = new Vector2(0f, 2f); tip.rectTransform.sizeDelta = new Vector2(14f, 10f);
                tip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var bt = Text(bub.transform, "T", bubble, 11, Navy, TextAnchor.MiddleCenter);
                Rect(bt.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -4f));
                bt.horizontalOverflow = HorizontalWrapMode.Wrap;
                bt.verticalOverflow = VerticalWrapMode.Truncate;
                if (sel)
                {
                    var ring = CoastUiArt.Panel(root, "Sel", new Color(0.30f, 0.71f, 0.67f, 0.35f), 16);
                    Rect(ring.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    ring.rectTransform.pivot = new Vector2(0.5f, 0f); ring.rectTransform.anchoredPosition = new Vector2(0f, 0f); ring.rectTransform.sizeDelta = new Vector2(100f, 66f);
                    ring.transform.SetAsFirstSibling();
                }
            }
        }

        private static string PotBubbleText(SeedDef seed, PotState pot, int stage, bool sel)
        {
            if (seed == null)
                return sel
                    ? Loc.T("빈 화분!\n아래서 씨앗을 골라 ↓", "Empty!\nPick a seed below ↓")
                    : Loc.T("빈 화분\n탭해서 심기", "Empty pot\nTap to plant");
            int pct = Mathf.RoundToInt(seed.chance * 100f);
            string head = Loc.T($"수확 성공 {pct}%!", $"Harvest {pct}%!");
            if (stage >= 5)
                return Loc.T($"{head}\n{seed.Emoji} {seed.Name} 다 자랐다! ⭐", $"{head}\n{seed.Emoji} {seed.Name} ready! ⭐");
            if (stage <= 1)
                return Loc.T($"{head}\n{seed.Emoji} {seed.Name} 막 싹텄다!", $"{head}\n{seed.Emoji} {seed.Name} just sprouted!");
            if (stage == 2)
                return Loc.T($"{head}\n{seed.Emoji} {seed.Name} 새싹!", $"{head}\n{seed.Emoji} {seed.Name} sprout!");
            int left = Mathf.Max(0, seed.weeks - pot.growth);
            if (stage >= 4)
                return Loc.T($"{head}\n{seed.Emoji} {seed.Name} 거의 다 자람~", $"{head}\n{seed.Emoji} {seed.Name} almost ready~");
            return Loc.T($"{head}\n{seed.Emoji} {seed.Name} 자라는 중... {left}주 남음", $"{head}\n{seed.Emoji} {seed.Name} growing... {left}w left");
        }

        /// 작물 그림(단계 1 씨앗 → 2 새싹 → 3 줄기 → 4 봉오리 → 5 열매/꽃) — 작물마다 색·모양 다르게.
        private static void DrawPlant(RectTransform root, SeedDef seed, int stage)
        {
            if (stage <= 1)
            {
                var sprout = CoastUiArt.Panel(root, "Seed", new Color(0.75f, 0.65f, 0.45f), 4);
                Rect(sprout.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                sprout.rectTransform.pivot = new Vector2(0.5f, 0f); sprout.rectTransform.anchoredPosition = new Vector2(0f, 62f); sprout.rectTransform.sizeDelta = new Vector2(12f, 8f);
                return;
            }
            float h = stage == 2 ? 24f : stage == 3 ? 60f : stage == 4 ? 88f : 104f;
            bool rice = seed.id == "rice";
            int stems = rice ? 5 : 1;
            for (int sIdx = 0; sIdx < stems; sIdx++)
            {
                float dx = rice ? (sIdx - 2) * 10f : 0f;
                var stem = CoastUiArt.Panel(root, "Stem" + sIdx, rice && stage == 5 ? new Color(0.80f, 0.72f, 0.35f) : new Color(0.35f, 0.65f, 0.35f), 4);
                Rect(stem.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                stem.rectTransform.pivot = new Vector2(0.5f, 0f); stem.rectTransform.anchoredPosition = new Vector2(dx, 64f); stem.rectTransform.sizeDelta = new Vector2(rice ? 4f : 7f, h * (rice ? 0.9f + 0.05f * (sIdx % 3) : 1f));
                stem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rice ? (sIdx - 2) * 6f : 0f);
            }
            if (!rice)
                for (int k = 0; k < (stage >= 3 ? 2 : 1); k++)
                {
                    var leaf = CoastUiArt.Panel(root, "Leaf" + k, new Color(0.40f, 0.72f, 0.40f), 12);
                    Rect(leaf.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    leaf.rectTransform.pivot = new Vector2(k == 0 ? 1f : 0f, 0.5f); leaf.rectTransform.anchoredPosition = new Vector2(k == 0 ? -2f : 2f, 64f + h * (k == 0 ? 0.35f : 0.6f));
                    leaf.rectTransform.sizeDelta = new Vector2(30f, 16f); leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? 25f : -25f);
                }
            if (stage < 4) return;
            float r = stage == 4 ? 10f : 20f;
            if (seed.id == "rose" || seed.id == "lavender")
            {
                for (int k = 0; k < 6; k++)
                {
                    float a = k * 60f * Mathf.Deg2Rad;
                    var petal = CoastUiArt.Panel(root, "Petal" + k, seed.petal, 14);
                    Rect(petal.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    petal.rectTransform.pivot = new Vector2(0.5f, 0.5f); petal.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * r * 0.85f, 64f + h + Mathf.Sin(a) * r * 0.85f); petal.rectTransform.sizeDelta = new Vector2(r * 1.2f, r * 1.2f);
                }
                var mid = CoastUiArt.Panel(root, "Mid", Color.Lerp(seed.petal, Color.white, 0.4f), 14);
                Rect(mid.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                mid.rectTransform.pivot = new Vector2(0.5f, 0.5f); mid.rectTransform.anchoredPosition = new Vector2(0f, 64f + h); mid.rectTransform.sizeDelta = new Vector2(r, r);
            }
            else if (rice)
            {
                for (int k = 0; k < 5; k++)
                {
                    var ear = CoastUiArt.Panel(root, "Ear" + k, seed.petal, 6);
                    Rect(ear.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    ear.rectTransform.pivot = new Vector2(0.5f, 0f); ear.rectTransform.anchoredPosition = new Vector2((k - 2) * 10f + (k - 2) * 2f, 64f + h * 0.8f); ear.rectTransform.sizeDelta = new Vector2(8f, r * 1.3f);
                    ear.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (k - 2) * 14f);
                }
            }
            else
            {
                // 토마토·감자: 열매 3알
                int n = stage == 5 ? 3 : 1;
                for (int k = 0; k < n; k++)
                {
                    var fruit = CoastUiArt.Panel(root, "Fruit" + k, seed.petal, 12);
                    Rect(fruit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    fruit.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    fruit.rectTransform.anchoredPosition = new Vector2((k - 1) * 18f, 64f + h * (0.55f + 0.2f * (k % 2)) - (seed.id == "potato" ? h * 0.5f : 0f));
                    fruit.rectTransform.sizeDelta = new Vector2(r, r * 0.9f);
                }
            }
        }

        /// 씨앗 카드 위 작은 작물 그림(화분 + 5단계 작물, 0.5배).
        private static void SeedIcon(RectTransform card, SeedDef sd)
        {
            var host = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(card, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 1f); host.pivot = new Vector2(0.5f, 0f);
            host.anchoredPosition = new Vector2(0f, -150f); host.sizeDelta = new Vector2(120f, 200f);   // 109차: 작물 그림 키움(0.5 → 0.72)
            host.localScale = new Vector3(0.72f, 0.72f, 1f);
            var body = CoastUiArt.Panel(host, "Pot", new Color(0.82f, 0.46f, 0.30f), 14); body.raycastTarget = false;
            Rect(body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            body.rectTransform.pivot = new Vector2(0.5f, 0f); body.rectTransform.anchoredPosition = new Vector2(0f, 20f); body.rectTransform.sizeDelta = new Vector2(64f, 44f);
            var rim = CoastUiArt.Panel(host, "Rim", new Color(0.94f, 0.62f, 0.44f), 10); rim.raycastTarget = false;
            Rect(rim.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            rim.rectTransform.pivot = new Vector2(0.5f, 0f); rim.rectTransform.anchoredPosition = new Vector2(0f, 60f); rim.rectTransform.sizeDelta = new Vector2(76f, 12f);
            DrawPlant(host, sd, 5);
            foreach (var im in host.GetComponentsInChildren<Image>(true)) im.raycastTarget = false;
        }

        private void PotTapped(int i)
        {
            var s = Save; HomeData.EnsurePots(s);
            var pot = s.pots[i]; var seed = HomeData.Seed(pot.seed);
            if (seed == null) { _selectedPot = _selectedPot == i ? -1 : i; RefreshPots(); Clear(_tray); BuildSeedTray(); return; }
            if (HomeData.IsBloomed(s, i))
            {
                bool ok = HomeData.Harvest(s, i, out var sd); _gm.Persist();
                if (ok) { CoastToast.Show(Loc.T($"수확 성공! {sd.Name} → {sd.RewardText}", $"Harvest! {sd.Name} → {sd.RewardText}")); CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.6f); }
                else { CoastToast.Show(Loc.T($"{sd.Name}이(가) 시들었다… (성공 {Mathf.RoundToInt(sd.chance * 100)}%)", $"{sd.Name} withered… ({Mathf.RoundToInt(sd.chance * 100)}%)")); CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f); }
            }
            else CoastToast.Show(Loc.T($"아직 자라는 중 — {seed.weeks - pot.growth}주 남았어(주가 지나면 자라).", $"Still growing — {seed.weeks - pot.growth}w left."));
            RefreshPots(); RefreshMoney(); Clear(_tray); BuildSeedTray();
        }

        private void BuildSeedTray()
        {
            string head = Loc.T("🌱 식물 키우기 · 고르기  (씨앗을 선택해 심자!)", "🌱 Grow · pick  (choose a seed to plant!)");
            if (_selectedPot >= 0) head = Loc.T($"🌱 화분 {_selectedPot + 1}에 심을 씨앗을 골라!", $"🌱 Pick a seed for pot {_selectedPot + 1}!");
            _hint = Text(_tray, "Hint", head, 17, Ink, TextAnchor.MiddleCenter); _hint.fontStyle = FontStyle.Bold;   // 109차(사용자: 텃밭 글자·그림 너무 작다): 키움
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 11; _hint.resizeTextMaxSize = CoastHudLayout.Scaled(17);
            Rect(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(0f, 0f));
            var scroll = MakeHScroll(_tray, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -32f), out var content);
            const float cw = 200f, gap = 12f;
            Color[] cardCols =
            {
                new Color(1.00f, 0.86f, 0.88f), // 토마토
                new Color(1.00f, 0.94f, 0.78f), // 감자
                new Color(0.88f, 0.96f, 0.80f), // 벼
                new Color(0.98f, 0.86f, 0.94f), // 장미
                new Color(0.90f, 0.86f, 0.98f), // 라벤더
            };
            Color[] rateCols =
            {
                new Color(0.98f, 0.55f, 0.62f),
                new Color(0.45f, 0.72f, 0.55f),
                new Color(0.55f, 0.78f, 0.45f),
                new Color(0.95f, 0.55f, 0.70f),
                new Color(0.70f, 0.55f, 0.90f),
            };
            for (int i = 0; i < HomeData.Seeds.Length; i++)
            {
                var sd = HomeData.Seeds[i];
                var card = CoastUiArt.CutePill(content, "S_" + sd.id, cardCols[i % cardCols.Length], 16, 3);
                card.rectTransform.anchorMin = new Vector2(0f, 0f); card.rectTransform.anchorMax = new Vector2(0f, 1f); card.rectTransform.pivot = new Vector2(0f, 0.5f);
                card.rectTransform.anchoredPosition = new Vector2(i * (cw + gap), 0f); card.rectTransform.sizeDelta = new Vector2(cw, 0f);

                // 94차: 이모지(폰트에 없음) 대신 다 자란 작물 미니 그림
                SeedIcon(card.rectTransform, sd);

                var nm = Text(card.transform, "Name", sd.Name, 22, Navy, TextAnchor.MiddleCenter); nm.fontStyle = FontStyle.Bold;   // 109차: 글자 키움
                Rect(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 104f), new Vector2(-4f, 134f));
                var pr = Text(card.transform, "Price", $"{sd.price}G", 19, new Color(0.32f, 0.52f, 0.22f), TextAnchor.MiddleCenter); pr.fontStyle = FontStyle.Bold;
                Rect(pr.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 78f), new Vector2(-4f, 104f));
                var info = Text(card.transform, "Info", Loc.T($"{sd.weeks}주 · {sd.GrowHint}", $"{sd.weeks}w · {sd.GrowHint}"), 15, Ink, TextAnchor.MiddleCenter);
                info.resizeTextForBestFit = true; info.resizeTextMinSize = 10; info.resizeTextMaxSize = CoastHudLayout.Scaled(15);
                Rect(info.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 54f), new Vector2(-2f, 78f));

                bool can = _selectedPot >= 0 && Save.stats.money >= sd.price;
                int pct = Mathf.RoundToInt(sd.chance * 100f);
                var b = Button(card.transform, "Buy", Loc.T($"성공률 {pct}% 심기", $"Plant · {pct}%"), can ? rateCols[i % rateCols.Length] : Grey, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(176f, 44f), () =>   // 109차: 버튼 키움
                {
                    if (_selectedPot < 0) { CoastToast.Show(Loc.T("먼저 빈 화분을 탭해.", "Tap an empty pot first.")); return; }
                    if (!HomeData.Plant(Save, _selectedPot, sd)) { CoastToast.Show(Loc.T("G가 모자라거나 빈 화분이 아니야.", "Not enough G or pot not empty.")); return; }
                    _gm.Persist(); CoastToast.Show(Loc.T($"{sd.Name}을(를) 심었어. {sd.weeks}주 뒤에 수확!", $"Planted {sd.Name}. Harvest in {sd.weeks}w!"));
                    _selectedPot = -1; RefreshPots(); RefreshMoney(); Clear(_tray); BuildSeedTray();
                });
                { var bt0 = b.GetComponentInChildren<Text>(); bt0.fontSize = CoastHudLayout.Scaled(17); bt0.resizeTextMaxSize = CoastHudLayout.Scaled(17); }
                b.interactable = can;
            }
            content.sizeDelta = new Vector2(HomeData.Seeds.Length * (cw + gap), 0f);
        }

        // ── 놀이 (시안: 전통놀이 한마당) ─────────────────────────────────

        private static readonly Color PlayInk = new Color(0.32f, 0.18f, 0.14f);
        private static readonly Color PlayDo = new Color(0.95f, 0.38f, 0.55f);
        private static readonly Color[] PlayCardCols =
        {
            new Color(0.78f, 0.90f, 0.98f), // 구슬 — 하늘
            new Color(0.98f, 0.84f, 0.88f), // 윷 — 분홍
            new Color(0.84f, 0.94f, 0.78f), // 투호 — 연두
            new Color(0.90f, 0.84f, 0.96f), // 딱지 — 보라
            new Color(0.99f, 0.90f, 0.78f), // 무궁화 — 살구
        };
        private static readonly string[] PlayIcons = { "UI_MG_Marbles", "UI_MG_Yut", "UI_MG_Tuho", "UI_MG_Ddakji", "UI_MG_Mugunghwa" };
        private static readonly string[] PlayBlurbsKo =
        {
            "(방향 선택) → [발사]로 흰 구슬을 쳐요. 3번 안에 상대편 구슬 2개 이하로 남기면 승리!",
            "윷을 던져 보드판을 돌아요! 먼저 도착하면 승리",
            "(방향 선택) → 힘 게이지로 화살 던져 5번 중 3번 항아리에 넣으면 승리",
            "힘 게이지로 위로 던져 노란 구역에서 뒤집기! 3번 안에 한 번 뒤집으면 승리",
            "[달리기]를 누르면 앞으로! 술래가 돌아보면 멈춰, 3번 안에 도달하면 승리",
        };
        private static readonly string[] PlayBlurbsEn =
        {
            "Aim → [Shoot] the white marble. Leave 2 or fewer opponent marbles in 3 turns!",
            "Throw yut and race the board — first to finish wins!",
            "Aim → throw with the power gauge. Land 3 of 5 in the jar to win!",
            "Hit the yellow zone on the gauge to flip! Once in 3 tries wins.",
            "Hold [Run]! Freeze when the tagger turns — reach them in 3 tries.",
        };
        private static readonly Color[] PlayFlowerCols =
        {
            new Color(0.45f, 0.72f, 0.95f),
            new Color(0.95f, 0.55f, 0.70f),
            new Color(0.45f, 0.78f, 0.48f),
            new Color(0.72f, 0.52f, 0.92f),
            new Color(0.98f, 0.72f, 0.40f),
        };

        private void BuildPlay()
        {
            CoastHudLayout.MakeImage(_body, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.99f, 0.96f, 0.90f));
            SprinklePlayDecor(_body);

            // 상단 배너 — 시안: 놀이 - 전통놀이 한마당
            var banner = CoastUiArt.CutePill(_body, "Banner", new Color(1f, 0.96f, 0.88f), 16, 3);
            Rect(banner.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.99f), Vector2.zero, Vector2.zero);
            PlaceCornerFlower(banner.rectTransform, new Vector2(0.04f, 0.5f), new Color(0.95f, 0.55f, 0.72f));
            PlaceCornerFlower(banner.rectTransform, new Vector2(0.96f, 0.5f), new Color(0.55f, 0.78f, 0.50f));
            var title = Text(banner.transform, "Title", Loc.T("놀이 - 전통놀이 한마당", "Play - Festival of Games"), 26, PlayInk, TextAnchor.MiddleCenter);   // 127차 시안: 제목 크게
            Rect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 0f), new Vector2(-36f, 0f));
            title.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(title, new Color(1f, 1f, 1f, 0.55f), 1.2f);
            banner.transform.SetAsLastSibling();

            var defs = ChapterMission.All;
            const float listTop = 0.885f, listBot = 0.02f, gap = 0.012f;
            float rowH = (listTop - listBot - gap * (defs.Length - 1)) / Mathf.Max(1, defs.Length);
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                float top = listTop - i * (rowH + gap);
                float bot = top - rowH;
                var card = CoastUiArt.CutePill(_body, "G" + i, PlayCardCols[i % PlayCardCols.Length], 18, 3);
                Rect(card.rectTransform, new Vector2(0.025f, bot), new Vector2(0.975f, top), Vector2.zero, Vector2.zero);

                BuildPlayIcon(card.transform, i);

                var nm = Text(card.transform, "Name", Loc.T(d.nameKo, d.nameEn), 24, PlayInk, TextAnchor.MiddleLeft);   // 127차 시안: 이름 크게
                Rect(nm.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 1f), new Vector2(112f, 2f), new Vector2(-134f, -4f));
                nm.fontStyle = FontStyle.Bold;
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = CoastHudLayout.Scaled(24);

                string blurb = Loc.T(PlayBlurbsKo[i % PlayBlurbsKo.Length], PlayBlurbsEn[i % PlayBlurbsEn.Length]);
                var desc = Text(card.transform, "Desc", blurb, 13, new Color(0.38f, 0.26f, 0.20f), TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.verticalOverflow = VerticalWrapMode.Truncate;
                desc.resizeTextForBestFit = true; desc.resizeTextMinSize = 10; desc.resizeTextMaxSize = CoastHudLayout.Scaled(14);
                Rect(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.56f), new Vector2(112f, 8f), new Vector2(-134f, 0f));

                var kind = d.kind;
                // 127차 시안: 분홍 글로시 「하기」 + 반짝이
                var go = CoastUiArt.GlossyPill(card.transform, "Play", PlayDo, 18, 5);
                go.rectTransform.anchorMin = go.rectTransform.anchorMax = go.rectTransform.pivot = new Vector2(1f, 0.5f);
                go.rectTransform.anchoredPosition = new Vector2(-14f, 0f); go.rectTransform.sizeDelta = new Vector2(118f, 52f); go.raycastTarget = true;
                foreach (var im in go.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
                var gb = go.gameObject.AddComponent<Button>(); gb.transition = Selectable.Transition.None; gb.targetGraphic = go;
                gb.onClick.AddListener(() => { CoastPrefs.Vibrate(); StartMini(kind); });
                var gt = Text(go.transform, "T", Loc.T("하기", "Play"), 22, Color.white, TextAnchor.MiddleCenter); gt.fontStyle = FontStyle.Bold; gt.raycastTarget = false;
                CoastUiArt.OutlineText(gt, new Color(0.55f, 0.10f, 0.25f, 0.6f), 1.5f);
                var spk = CoastUiArt.Icon("Star");
                if (spk != null)
                {
                    var s1 = CoastHudLayout.MakeImage(go.transform, "Spk", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -20f), new Vector2(20f, -6f), new Color(1f, 1f, 1f, 0.9f)); s1.sprite = spk; s1.preserveAspect = true; s1.raycastTarget = false;
                    var s2 = CoastHudLayout.MakeImage(go.transform, "Spk2", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 6f), new Vector2(-8f, 16f), new Color(1f, 1f, 1f, 0.8f)); s2.sprite = spk; s2.preserveAspect = true; s2.raycastTarget = false;
                }
            }
        }

        private static void BuildPlayIcon(Transform card, int i)
        {
            // 127차 시안: 꽃잎 대신 흰 원 + 연한 색 테두리
            Color flower = PlayFlowerCols[i % PlayFlowerCols.Length];
            var rim = CoastHudLayout.MakeImage(card, "IcoRim", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, -42f), new Vector2(93f, 42f), new Color(flower.r, flower.g, flower.b, 0.55f));
            rim.raycastTarget = false; rim.sprite = CoastUiArt.RoundedRect(44);
            var ring = CoastHudLayout.MakeImage(card, "IcoRing", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, -39f), new Vector2(90f, 39f), Color.white);
            ring.raycastTarget = false;
            ring.sprite = CoastUiArt.RoundedRect(40);
            ring.color = new Color(1f, 1f, 1f, 0.98f);
            if (ring.gameObject.GetComponent<Mask>() == null)
            {
                var m = ring.gameObject.AddComponent<Mask>();
                m.showMaskGraphic = true;
            }
            string iconName = PlayIcons[i % PlayIcons.Length];
            var tex = ArtAssets.LoadTexture(iconName);
            if (tex != null)
            {
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(ring.transform, false);
                im.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(tex));
                im.preserveAspect = true; im.raycastTarget = false;
                Rect(im.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            }
        }

        public static void SprinklePlayDecor(RectTransform host)
        {
            Color[] dots =
            {
                new Color(1f, 0.55f, 0.70f, 0.55f), new Color(0.55f, 0.75f, 1f, 0.50f),
                new Color(0.95f, 0.80f, 0.30f, 0.50f), new Color(0.55f, 0.88f, 0.55f, 0.45f),
                new Color(0.78f, 0.55f, 0.95f, 0.50f),
            };
            Vector2[] spots =
            {
                new Vector2(0.06f, 0.92f), new Vector2(0.94f, 0.90f), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.10f),
                new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.48f), new Vector2(0.50f, 0.97f), new Vector2(0.72f, 0.05f),
                new Vector2(0.28f, 0.04f), new Vector2(0.15f, 0.75f), new Vector2(0.85f, 0.70f), new Vector2(0.60f, 0.95f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                float s = (i % 3 == 0) ? 14f : 8f;
                var c = dots[i % dots.Length];
                var d = CoastHudLayout.MakeImage(host, "Dot" + i, spots[i], spots[i],
                    new Vector2(-s, -s), new Vector2(s, s), c);
                d.raycastTarget = false;
                d.sprite = CoastUiArt.RoundedRect(32);
            }
            PlaceCornerFlower(host, new Vector2(0.02f, 0.02f), new Color(0.92f, 0.45f, 0.70f));
            PlaceCornerFlower(host, new Vector2(0.98f, 0.02f), new Color(0.70f, 0.50f, 0.95f));
            PlaceCornerFlower(host, new Vector2(0.02f, 0.98f), new Color(0.45f, 0.78f, 0.95f));
            PlaceCornerFlower(host, new Vector2(0.98f, 0.98f), new Color(0.98f, 0.72f, 0.35f));
        }

        public static void PlaceCornerFlower(RectTransform host, Vector2 anchor, Color col)
        {
            for (int i = 0; i < 5; i++)
            {
                float ang = i * 72f * Mathf.Deg2Rad;
                var p = CoastHudLayout.MakeImage(host, "F", anchor, anchor,
                    new Vector2(Mathf.Cos(ang) * 10f - 7f, Mathf.Sin(ang) * 10f - 7f),
                    new Vector2(Mathf.Cos(ang) * 10f + 7f, Mathf.Sin(ang) * 10f + 7f),
                    new Color(col.r, col.g, col.b, 0.85f));
                p.raycastTarget = false;
                p.sprite = CoastUiArt.RoundedRect(28);
            }
            var center = CoastHudLayout.MakeImage(host, "FC", anchor, anchor, new Vector2(-5f, -5f), new Vector2(5f, 5f), new Color(1f, 0.92f, 0.45f, 0.95f));
            center.raycastTarget = false;
            center.sprite = CoastUiArt.RoundedRect(24);
        }

        private void BuildPlayTray()
        {
            int left = HomeData.RewardPlaysLeft(Save);
            int max = HomeData.MiniGameRewardPerWeek;
            // 74차: 보상은 돈 + 스트레스 −3(HomeData.MiniGameStressRelief) — 문구도 실제 효과를 말하게.
            int relief = HomeData.MiniGameStressRelief;
            string ko = left > 0
                ? $"이기면 돈 + 스트레스 -{relief} · 이번 주 보상 {left}/{max}!!\n다 쓰면 연습(보상 없음). 주차가 바뀌면 다시 채워져."
                : $"이기면 돈 + 스트레스 -{relief} · 이번 주 보상 0/{max}!!\n연습(보상 없음). 주차가 바뀌면 다시 채워져.";
            string en = left > 0
                ? $"Win = money + stress -{relief} · rewards {left}/{max} this week!!\nThen practice (no reward). Refills each week."
                : $"Win = money + stress -{relief} · rewards 0/{max} this week!!\nPractice only. Refills each week.";
            // 127차 시안: 보라 배너 + 위 가운데 리본·별 + 흰 글씨(외곽선)
            var panel = CoastUiArt.CutePill(_tray, "InfoBox", new Color(0.60f, 0.48f, 0.86f), 16, 4);
            Rect(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var ribbon = CoastUiArt.Panel(panel.transform, "Ribbon", new Color(0.50f, 0.36f, 0.80f), 8); ribbon.raycastTarget = false;
            Rect(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, -14f), new Vector2(120f, 10f));
            var rStar = CoastUiArt.Icon("Star");
            if (rStar != null)
            {
                var rs = CoastHudLayout.MakeImage(panel.transform, "RStar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-22f, -22f), new Vector2(22f, 22f), new Color(1f, 0.85f, 0.30f)); rs.sprite = rStar; rs.preserveAspect = true; rs.raycastTarget = false;
                foreach (var (sx, sy) in new[] { (0.05f, 0.75f), (0.95f, 0.78f), (0.08f, 0.22f), (0.93f, 0.20f) })
                {
                    var sp = CoastHudLayout.MakeImage(panel.transform, "Spk", new Vector2(sx, sy), new Vector2(sx, sy), new Vector2(-10f, -10f), new Vector2(10f, 10f), new Color(1f, 1f, 1f, 0.85f)); sp.sprite = rStar; sp.preserveAspect = true; sp.raycastTarget = false;
                }
            }
            var t = Text(panel.transform, "Info", Loc.T(ko, en), 18, Color.white, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(t, new Color(0.30f, 0.18f, 0.50f, 0.9f), 1.6f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;   // 절대 10 미만으로 줄어들지 않게
            t.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            t.fontSize = CoastHudLayout.Scaled(18);
            Rect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 6f), new Vector2(-36f, -6f));
        }

        private void StartMini(ChapterMission.Kind kind)
        {
            if (ChapterMissionUI.IsOpen) return;
            bool rewardable = HomeData.RewardPlaysLeft(Save) > 0;
            // replay=true: 스토리 게이트와 무관하게 언제든 플레이(미션 클리어 비트는 안 건드림)
            ChapterMissionUI.Play(kind, true, won =>
            {
                if (won && rewardable)
                {
                    int pay = ChapterMission.Reward(_gm);
                    int given = HomeData.GiveReward(Save, pay);
                    if (given > 0)
                    {
                        _gm.Persist();
                        CoastToast.Show(Loc.T($"+{given}G · 스트레스 -{HomeData.MiniGameStressRelief}!", $"+{given}G · stress -{HomeData.MiniGameStressRelief}!"));
                    }
                }
                else if (won && !rewardable)
                    CoastToast.Show(Loc.T("연습 게임 — 보상은 없어.", "Practice — no reward."));
                RefreshMoney();
                if (_tab == Tab.Play) { Clear(_tray); BuildPlayTray(); }
            });
        }

        // ── 주인공·꼬마·펫 고정 포즈 ───────────────────────────────────────

        private void Update()
        {
            if (_tab != Tab.Room || _charRt == null || _roomHost == null) return;
            float depth = Mathf.Lerp(1.0f, 0.78f, _charPos.y / 0.42f);
            _charRt.anchorMin = _charRt.anchorMax = _charPos;
            _charRt.anchoredPosition = Vector2.zero;
            _charRt.localScale = new Vector3(depth, depth, 1f);
            if (_kidRt != null)
            {
                float kd = Mathf.Lerp(1.0f, 0.78f, _kidPos.y / 0.42f);
                _kidRt.anchorMin = _kidRt.anchorMax = _kidPos;
                _kidRt.anchoredPosition = Vector2.zero;
                _kidRt.localScale = new Vector3(kd, kd, 1f);
            }
            if (_petRt != null)
            {
                float pd = Mathf.Lerp(1.0f, 0.78f, _petPos.y / 0.42f);
                _petRt.anchorMin = _petRt.anchorMax = _petPos;
                _petRt.anchoredPosition = Vector2.zero;
                _petRt.localScale = new Vector3(pd, pd, 1f);
            }
            if (_dragging == null) ReorderDepth();
        }

        /// 앞뒤 정렬: 주인공·꼬마·펫만 y 기준으로 형제 순서 맞춤.
        private void ReorderDepth()
        {
            if (_decoLayer == null) return;
            var floor = new List<(RectTransform rt, float y)>();
            if (_charRt != null && _charRt.parent == _decoLayer) floor.Add((_charRt, _charPos.y + 0.015f));
            if (_kidRt != null && _kidRt.parent == _decoLayer) floor.Add((_kidRt, _kidPos.y + 0.014f));
            if (_petRt != null && _petRt.parent == _decoLayer) floor.Add((_petRt, _petPos.y + 0.012f));
            floor.Sort((a, b) => b.y.CompareTo(a.y));
            for (int i = 0; i < floor.Count; i++) floor[i].rt.SetSiblingIndex(i);
        }

        // ── 장식 그림(공용) ────────────────────────────────────────────────

        /// Resources/CoastRun/UI_Deco_<id>(마젠타 키 가능)가 있으면 그 그림, 없으면 색 알약(한자 없음).
        public static GameObject DecoVisual(Transform parent, DecoDef d, Vector2 size, bool shadow)
        {
            var root = new GameObject("Deco_" + d.id, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            if (shadow)
            {
                var sh = CoastHudLayout.MakeImage(root.transform, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-size.x * 0.38f, -4f), new Vector2(size.x * 0.38f, 10f), new Color(0f, 0f, 0f, 0.18f));
                sh.sprite = CoastUiArt.RoundedRect(20); sh.type = Image.Type.Sliced; sh.raycastTarget = false;
            }
            var tex = ArtAssets.LoadTexture("UI_Deco_" + d.id);
            if (tex != null)
            {
                var img = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.transform.SetParent(root.transform, false);
                img.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(tex)); img.preserveAspect = true; img.raycastTarget = false;
                Rect(img.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                var pill = CoastUiArt.CutePill(root.transform, "Ph", d.color, 16, 3);
                pill.raycastTarget = false;
                Rect(pill.rectTransform, Vector2.zero, Vector2.one, new Vector2(size.x * 0.08f, size.y * 0.12f), new Vector2(-size.x * 0.08f, -size.y * 0.06f));
                // 한자 tag 대신 짧은 이름만(공간 있을 때)
                if (size.y >= 72f)
                {
                    var nm = Text(pill.transform, "Name", d.Name, Mathf.Clamp(Mathf.RoundToInt(size.y * 0.14f), 10, 16), new Color(0.18f, 0.12f, 0.10f), TextAnchor.MiddleCenter);
                    nm.fontStyle = FontStyle.Bold;
                    nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 10; nm.resizeTextMaxSize = 16;
                    nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                    Rect(nm.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
                }
            }
            return root;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        private static void Rect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        private static Text Text(Transform parent, string name, string s, int size, Color color, TextAnchor align)
        {
            var t = CoastHudLayout.MakeText(parent, name, s, size, align, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = color; return t;
        }

        private static Button Button(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 16, 3);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = anchor; pill.rectTransform.pivot = anchor;
            pill.rectTransform.anchoredPosition = pos; pill.rectTransform.sizeDelta = size;
            pill.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            var t = Text(pill.transform, "T", label, 16, Color.white, TextAnchor.MiddleCenter);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return b;
        }

        private static ScrollRect MakeHScroll(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, out RectTransform content)
        {
            var go = new GameObject("HScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); Rect(rt, aMin, aMax, oMin, oMax);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var sr = go.GetComponent<ScrollRect>(); sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;
            content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(rt, false);
            content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(0f, 1f); content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(100f, 0f);
            sr.content = content; sr.viewport = rt;
            return sr;
        }

        private static void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        /// 탭(클릭) 위치를 넘겨주는 간단한 핸들러.
        private class PointerTap : MonoBehaviour, IPointerClickHandler
        {
            public Action<Vector2> OnTap;
            public void OnPointerClick(PointerEventData e) { if (e.dragging) return; OnTap?.Invoke(e.position); }
        }

        /// 가구 끌기 + 탭 구분.
        private class DecoDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            public Action<Vector2> OnDrag, OnEnd; public Action OnTap;
            private bool _dragged;
            public void OnBeginDrag(PointerEventData e) { _dragged = true; transform.SetAsLastSibling(); }
            void IDragHandler.OnDrag(PointerEventData e) => OnDrag?.Invoke(e.position);
            public void OnEndDrag(PointerEventData e) { OnEnd?.Invoke(e.position); }
            public void OnPointerClick(PointerEventData e) { if (_dragged) { _dragged = false; return; } OnTap?.Invoke(); }
        }

        /// 트레이 → 방 드래그. 위로 끌면 방에 놓고, 가로는 트레이 스크롤.
        private class TrayDecoDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public ScrollRect Scroll;
            public Action OnBegin;
            public Action<Vector2> OnMove, OnEnd;
            private bool _armed;
            private Vector2 _start;
            private float _contentStart;
            public void OnBeginDrag(PointerEventData e)
            {
                _armed = false;
                _start = e.position;
                if (Scroll != null && Scroll.content != null)
                    _contentStart = Scroll.content.anchoredPosition.x;
            }
            public void OnDrag(PointerEventData e)
            {
                var d = e.position - _start;
                if (!_armed)
                {
                    if (d.y > 18f && Mathf.Abs(d.y) > Mathf.Abs(d.x) * 0.85f)
                    {
                        _armed = true;
                        if (Scroll != null) Scroll.enabled = false;
                        OnBegin?.Invoke();
                        OnMove?.Invoke(e.position);
                        return;
                    }
                    // 가로 스크롤 수동
                    if (Scroll != null && Scroll.content != null && Mathf.Abs(d.x) > 4f)
                    {
                        var pos = Scroll.content.anchoredPosition;
                        pos.x = _contentStart + d.x;
                        float minX = Mathf.Min(0f, Scroll.viewport.rect.width - Scroll.content.rect.width);
                        pos.x = Mathf.Clamp(pos.x, minX, 0f);
                        Scroll.content.anchoredPosition = pos;
                    }
                    return;
                }
                OnMove?.Invoke(e.position);
            }
            public void OnEndDrag(PointerEventData e)
            {
                if (Scroll != null) Scroll.enabled = true;
                if (_armed) OnEnd?.Invoke(e.position);
                _armed = false;
            }
        }
    }
}
