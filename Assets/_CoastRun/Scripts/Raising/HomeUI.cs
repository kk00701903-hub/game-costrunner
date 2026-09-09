using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 30차: 집 화면(방 꾸미기 v2) — 전체 화면 오버레이.
    ///   [방]     가구·장식을 방 안에 자유 배치(끌어서 이동), 주인공이 방 안을 돌아다니고 바닥을 탭하면 걸어간다. 러닝머신 탭 → 운동.
    ///   [베란다] 화분 4개: 씨앗 심기(G) → 페이즈마다 물 주기 → 꽃이 피면 팔기(G).
    ///   [놀이]   윷놀이·구슬치기·공기놀이 미니게임(주 3회 보상, 그 뒤는 연습).
    /// 다른 방꾸미기 모바일 게임(플레이투게더·동물의숲 포켓캠프·헬로키티 아일랜드)의 '트레이에서 꺼내 놓고 끌어 옮기기' 문법을 따른다.
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

        private GameManager _gm;
        private Sprite _charSprite;
        private Action _onClose;
        private Canvas _canvas;
        private RectTransform _root, _body, _tray, _tabBar;
        private Text _money, _hint;
        private readonly Image[] _tabImgs = new Image[3];
        private Tab _tab = Tab.Room;

        // 방
        private RectTransform _roomHost, _decoLayer, _charRt;
        private Image _charImg;
        private Vector2 _charPos = new Vector2(0.5f, 0.12f), _charTarget = new Vector2(0.5f, 0.12f);
        private float _wanderT = 2f, _walkPhase;
        private Action _onArrive;
        private readonly Dictionary<string, RectTransform> _placed = new Dictionary<string, RectTransform>();
        private string _dragging;

        // 베란다
        private int _selectedPot = -1;
        private readonly List<RectTransform> _potRoots = new List<RectTransform>();

        // 미니게임
        private GameObject _miniRoot;

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

            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), new Color(0.16f, 0.10f, 0.16f, 0.985f));
            dim.raycastTarget = true;

            // 헤더
            var head = CoastUiArt.CutePill(_root, "Head", new Color(0.29f, 0.18f, 0.33f), 22, 4);
            Rect(head.rectTransform, new Vector2(0f, 0.925f), new Vector2(1f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -2f));
            var title = Text(head.transform, "Title", Loc.T("우리 집", "Home"), 24, Cream, TextAnchor.MiddleLeft);
            Rect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-260f, 0f));
            CoastUiArt.OutlineText(title, new Color(0f, 0f, 0f, 0.4f), 1.5f);
            _money = Text(head.transform, "Money", "", 18, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleRight);
            Rect(_money.rectTransform, Vector2.zero, Vector2.one, new Vector2(200f, 0f), new Vector2(-150f, 0f));
            Button(head.transform, "Close", Loc.T("나가기", "Leave"), Grey, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(124f, 46f), Close);

            // 탭
            _tabBar = new GameObject("Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
            _tabBar.SetParent(_root, false);
            Rect(_tabBar, new Vector2(0f, 0.868f), new Vector2(1f, 0.92f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            string[] names = { Loc.T("방", "Room"), Loc.T("베란다", "Balcony"), Loc.T("놀이", "Play") };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var pill = CoastUiArt.CutePill(_tabBar, "Tab" + i, Grey, 16, 3);
                Rect(pill.rectTransform, new Vector2(i / 3f, 0f), new Vector2((i + 1) / 3f, 1f), new Vector2(i == 0 ? 0f : 4f, 0f), new Vector2(i == 2 ? 0f : -4f, 0f));
                pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => SetTab((Tab)idx));
                var t = Text(pill.transform, "T", names[i], 19, Color.white, TextAnchor.MiddleCenter);
                CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                _tabImgs[i] = pill;
            }

            // 본문(장면) + 트레이
            var bodyFrame = CoastUiArt.CutePill(_root, "BodyFrame", new Color(0.95f, 0.85f, 0.70f), 22, 4);
            Rect(bodyFrame.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 0.862f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            _body = new GameObject("Body", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            _body.SetParent(bodyFrame.transform, false);
            Rect(_body, Vector2.zero, Vector2.one, new Vector2(7f, 10f), new Vector2(-7f, -7f));

            var trayFrame = CoastUiArt.CutePill(_root, "TrayFrame", new Color(1f, 0.97f, 0.90f), 22, 4);
            Rect(trayFrame.rectTransform, new Vector2(0f, 0.015f), new Vector2(1f, 0.292f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            _tray = new GameObject("Tray", typeof(RectTransform)).GetComponent<RectTransform>();
            _tray.SetParent(trayFrame.transform, false);
            Rect(_tray, Vector2.zero, Vector2.one, new Vector2(8f, 10f), new Vector2(-8f, -8f));

            SetTab(Tab.Room);
        }

        private void SetTab(Tab t)
        {
            _tab = t;
            for (int i = 0; i < 3; i++)
            {
                var fill = _tabImgs[i].transform.Find("Fill")?.GetComponent<Image>();
                var lip = _tabImgs[i].transform.Find("Lip")?.GetComponent<Image>();
                Color c = i == (int)t ? (i == 0 ? Coral : i == 1 ? Mint : Sun) : Grey;
                if (fill != null) fill.color = c;
                if (lip != null) lip.color = Color.Lerp(c, Color.black, 0.45f);
            }
            Clear(_body); Clear(_tray);
            _placed.Clear(); _potRoots.Clear(); _selectedPot = -1;
            switch (t)
            {
                case Tab.Room: BuildRoom(); BuildRoomTray(); break;
                case Tab.Balcony: BuildBalcony(); BuildSeedTray(); break;
                case Tab.Play: BuildPlay(); BuildPlayTray(); break;
            }
            RefreshMoney();
        }

        private void RefreshMoney()
        {
            if (_money != null && Save != null) _money.text = $"{Save.stats.money:N0} G";
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
            // 배경(방 그림, cover)
            var bgMask = new GameObject("BgMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            bgMask.transform.SetParent(_roomHost, false);
            Rect(bgMask.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bgMask.GetComponent<Image>().color = new Color(0.63f, 0.53f, 0.50f);
            bgMask.GetComponent<Mask>().showMaskGraphic = true;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
            bg.transform.SetParent(bgMask.transform, false);
            var brt = bg.rectTransform; brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = Vector2.zero; brt.sizeDelta = Vector2.zero;
            var fit = bg.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fit.aspectRatio = 810f / 1440f;
            var art = ArtAssets.LoadTexture("UI_Raising_Room_" + Timeline.SeasonOf(Save != null ? Save.week : 1)) ?? ArtAssets.LoadTexture("UI_Raising_Room");
            if (art != null) bg.sprite = CoastUiArt.AsSprite(art); bg.raycastTarget = false;
            // 바닥 탭 → 걸어가기
            var floor = CoastHudLayout.MakeImage(_roomHost, "FloorTap", new Vector2(0f, 0f), new Vector2(1f, 0.46f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            floor.raycastTarget = true;
            var tap = floor.gameObject.AddComponent<PointerTap>();
            tap.OnTap = pos => { var n = ToNorm(pos); _charTarget = new Vector2(Mathf.Clamp(n.x, 0.06f, 0.94f), Mathf.Clamp(n.y, 0.02f, 0.40f)); _wanderT = 6f; _onArrive = null; };
            // 벽 탭(아무 일 없음) — 드래그 중 이벤트가 바닥으로 새지 않게 투명 판
            _decoLayer = new GameObject("Deco", typeof(RectTransform)).GetComponent<RectTransform>();
            _decoLayer.SetParent(_roomHost, false);
            Rect(_decoLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 주인공
            _charRt = new GameObject("Girl", typeof(RectTransform)).GetComponent<RectTransform>();
            _charRt.SetParent(_roomHost, false);
            _charRt.anchorMin = _charRt.anchorMax = new Vector2(0.5f, 0.12f); _charRt.pivot = new Vector2(0.5f, 0f);
            _charRt.sizeDelta = new Vector2(210f, 300f);
            var sh = CoastHudLayout.MakeImage(_charRt, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-64f, -4f), new Vector2(64f, 12f), new Color(0f, 0f, 0f, 0.22f));
            sh.sprite = CoastUiArt.RoundedRect(30); sh.type = Image.Type.Sliced;
            _charImg = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _charImg.transform.SetParent(_charRt, false);
            Rect(_charImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _charImg.sprite = _charSprite; _charImg.preserveAspect = true; _charImg.raycastTarget = false;
            if (_charSprite == null) _charImg.color = new Color(1f, 0.8f, 0.6f);
            _charPos = _charTarget = new Vector2(0.5f, 0.12f);
            RefreshPlaced();
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
            Clear(_decoLayer); _placed.Clear();
            var p = Profile; HomeData.Ensure(p);
            // 벽걸이 먼저(뒤), 바닥은 y가 높을수록(멀수록) 먼저 그려서 앞뒤가 맞게
            var items = new List<HomeItem>(p.homeItems);
            items.Sort((a, b) =>
            {
                var da = HomeData.Find(a.id); var db = HomeData.Find(b.id);
                bool wa = da != null && HomeData.IsWall(da), wb = db != null && HomeData.IsWall(db);
                if (wa != wb) return wa ? -1 : 1;
                return b.y.CompareTo(a.y);
            });
            foreach (var h in items)
            {
                var d = HomeData.Find(h.id); if (d == null) continue;
                var size = HomeData.Size(d);
                var go = DecoVisual(_decoLayer, d, size, !HomeData.IsWall(d));
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(h.x, h.y); rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
                var hit = go.AddComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0f); hit.raycastTarget = true;
                var drag = go.AddComponent<DecoDrag>();
                string id = h.id;
                drag.OnDrag = pos =>
                {
                    var n = HomeData.ClampPos(d, ToNorm(pos) - new Vector2(0f, size.y * 0.5f / _roomHost.rect.height));
                    rt.anchorMin = rt.anchorMax = n; _dragging = id;
                };
                drag.OnEnd = pos =>
                {
                    var n = HomeData.ClampPos(d, ToNorm(pos) - new Vector2(0f, size.y * 0.5f / _roomHost.rect.height));
                    HomeData.Place(Profile, d, n.x, n.y); _gm.WriteProfileNow(); _dragging = null; RefreshPlaced();
                };
                drag.OnTap = () =>
                {
                    if (id == "treadmill")
                    {
                        if (!HomeData.TreadmillReady(Save, Profile)) { CoastToast.Show(Loc.T("이번 페이즈엔 이미 운동했어.", "Already trained this phase.")); return; }
                        _charTarget = new Vector2(Mathf.Clamp(h.x + 0.14f, 0.06f, 0.94f), Mathf.Clamp(h.y, 0.02f, 0.40f)); _wanderT = 8f;
                        _onArrive = () => { if (HomeData.UseTreadmill(Save, Profile)) { _gm.Persist(); RefreshMoney(); CoastToast.Show(Loc.T("러닝머신 30분! 체력 +2, 스트레스 +1", "30 min on the treadmill! Stamina +2, stress +1")); } };
                    }
                    else CoastToast.Show(Loc.T($"{d.Name} — 끌어서 옮길 수 있어. 트레이에서 [치우기].", $"{d.Name} — drag to move. Remove from the tray."));
                };
                _placed[h.id] = rt;
            }
            if (_charRt != null) _charRt.SetParent(_decoLayer, false);
            ReorderDepth();
        }

        private void BuildRoomTray()
        {
            var p = Profile;
            _hint = Text(_tray, "Hint", Loc.T("가구를 방 안에서 끌어 옮길 수 있어 · 러닝머신은 탭하면 운동", "Drag furniture to move it · tap the treadmill to train"), 13, Ink, TextAnchor.MiddleCenter);
            Rect(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, 0f));
            var scroll = MakeHScroll(_tray, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -30f), out var content);
            const float cw = 150f, ch = 0f, gap = 8f;
            var list = new List<DecoDef>(HomeData.All);
            int Rank(DecoDef d) => HomeData.IsNew(p, d) ? 0 : HomeData.IsPlaced(p, d) ? 2 : HomeData.Owns(p, d) ? 1 : d.FromRun ? 4 : 3;
            list.Sort((a, b) => { int c = Rank(a).CompareTo(Rank(b)); return c != 0 ? c : HomeData.IndexOf(a.id).CompareTo(HomeData.IndexOf(b.id)); });
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                bool owned = HomeData.Owns(p, d), placed = HomeData.IsPlaced(p, d), isNew = HomeData.IsNew(p, d);
                Color fill = placed ? new Color(0.72f, 0.90f, 0.86f) : owned ? new Color(0.92f, 0.97f, 0.90f) : d.FromRun ? new Color(0.90f, 0.88f, 0.94f) : new Color(1f, 0.94f, 0.85f);
                var card = CoastUiArt.CutePill(content, "C_" + d.id, fill, 16, 3);
                card.rectTransform.anchorMin = new Vector2(0f, 0f); card.rectTransform.anchorMax = new Vector2(0f, 1f); card.rectTransform.pivot = new Vector2(0f, 0.5f);
                card.rectTransform.anchoredPosition = new Vector2(i * (cw + gap), 0f); card.rectTransform.sizeDelta = new Vector2(cw, ch);
                var frame = CoastUiArt.Panel(card.transform, "Frame", new Color(1f, 1f, 1f, 0.6f), 12);
                Rect(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                frame.rectTransform.pivot = new Vector2(0.5f, 1f); frame.rectTransform.anchoredPosition = new Vector2(0f, -10f); frame.rectTransform.sizeDelta = new Vector2(96f, 96f);
                var vis = DecoVisual(frame.transform, d, new Vector2(96f, 96f), false);
                Rect(vis.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
                if (!owned) foreach (var g in vis.GetComponentsInChildren<Graphic>()) g.color = new Color(g.color.r * 0.7f, g.color.g * 0.7f, g.color.b * 0.72f, g.color.a);
                var nm = Text(card.transform, "Name", d.Name + (isNew ? " ●" : ""), 14, isNew ? Coral : Navy, TextAnchor.MiddleCenter);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = 19;
                Rect(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 66f), new Vector2(-6f, 94f));
                string label; Color col; bool enabled = true;
                if (placed) { label = Loc.T("치우기", "Remove"); col = Grey; }
                else if (owned) { label = Loc.T("놓기", "Place"); col = Sky; }
                else if (d.FromRun) { label = Loc.T("러닝 보상", "Run drop"); col = Grey; enabled = false; }
                else { label = $"{d.price:N0}G"; col = Save.stats.money >= d.price ? Coral : Grey; enabled = Save.stats.money >= d.price; }
                var b = Button(card.transform, "Act", label, col, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(120f, 46f), () => RoomAct(d));
                b.interactable = enabled;
            }
            content.sizeDelta = new Vector2(list.Count * (cw + gap), 0f);
        }

        private void RoomAct(DecoDef d)
        {
            var p = Profile;
            if (HomeData.IsPlaced(p, d)) { HomeData.Remove(p, d.id); CoastToast.Show(Loc.T($"{d.Name}를 치웠어.", $"Removed {d.Name}.")); }
            else if (HomeData.Owns(p, d))
            {
                var pos = HomeData.IsWall(d) ? new Vector2(0.5f, 0.66f) : new Vector2(0.5f, 0.18f);
                HomeData.Place(p, d, pos.x, pos.y);
                CoastToast.Show(Loc.T($"{d.Name}를 놓았어. 끌어서 옮겨 봐.", $"Placed {d.Name}. Drag to move."));
            }
            else if (!d.FromRun)
            {
                if (!HomeData.TryBuy(Save, p, d)) { CoastToast.Show(Loc.T("G가 모자라.", "Not enough G.")); return; }
                var pos = HomeData.IsWall(d) ? new Vector2(0.5f, 0.66f) : new Vector2(0.5f, 0.18f);
                HomeData.Place(p, d, pos.x, pos.y);
                _gm.Persist();
                CoastToast.Show(Loc.T($"{d.Name} 구매! 방에 놓았어.", $"Bought {d.Name}!"));
            }
            _gm.WriteProfileNow();
            RefreshPlaced(); Clear(_tray); BuildRoomTray(); RefreshMoney();
        }

        // ── 베란다 ───────────────────────────────────────────────────────

        private void BuildBalcony()
        {
            // 하늘 → 바다 → 난간 → 선반 위 화분 4개
            var sky = CoastHudLayout.MakeImage(_body, "Sky", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.62f, 0.84f, 0.98f));
            var glow = CoastHudLayout.MakeImage(_body, "Glow", new Vector2(0f, 0.45f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0.75f, 0.55f));
            var sea = CoastHudLayout.MakeImage(_body, "Sea", new Vector2(0f, 0.30f), new Vector2(1f, 0.52f), Vector2.zero, Vector2.zero, new Color(0.25f, 0.62f, 0.80f));
            var sun = CoastHudLayout.MakeImage(_body, "Sun", new Vector2(0.78f, 0.62f), new Vector2(0.78f, 0.62f), new Vector2(-34f, -34f), new Vector2(34f, 34f), new Color(1f, 0.85f, 0.45f));
            sun.sprite = CoastUiArt.RoundedRect(34); sun.type = Image.Type.Sliced;
            // 난간
            var rail = CoastHudLayout.MakeImage(_body, "Rail", new Vector2(0f, 0.30f), new Vector2(1f, 0.30f), new Vector2(0f, 0f), new Vector2(0f, 8f), new Color(0.93f, 0.93f, 0.96f));
            for (int i = 0; i <= 12; i++)
            {
                float x = i / 12f;
                var bar = CoastHudLayout.MakeImage(_body, "Bar" + i, new Vector2(x, 0.12f), new Vector2(x, 0.30f), new Vector2(-3f, 0f), new Vector2(3f, 0f), new Color(0.93f, 0.93f, 0.96f));
            }
            var shelf = CoastHudLayout.MakeImage(_body, "Shelf", new Vector2(0f, 0f), new Vector2(1f, 0.13f), Vector2.zero, Vector2.zero, new Color(0.55f, 0.38f, 0.28f));
            var shelfTop = CoastHudLayout.MakeImage(_body, "ShelfTop", new Vector2(0f, 0.13f), new Vector2(1f, 0.13f), new Vector2(0f, -4f), new Vector2(0f, 6f), new Color(0.72f, 0.52f, 0.38f));
            _potRoots.Clear();
            for (int i = 0; i < HomeData.PotCount; i++)
            {
                float x = 0.14f + i * 0.24f;
                var root = new GameObject("Pot" + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                root.SetParent(_body, false);
                root.anchorMin = root.anchorMax = new Vector2(x, 0.13f); root.pivot = new Vector2(0.5f, 0f);
                root.anchoredPosition = Vector2.zero; root.sizeDelta = new Vector2(150f, 250f);
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
                // 화분(사다리꼴 대신 둥근 통) + 흙
                var body = CoastUiArt.Panel(root, "Pot", sel ? new Color(0.95f, 0.55f, 0.40f) : new Color(0.85f, 0.45f, 0.32f), 14);
                Rect(body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                body.rectTransform.pivot = new Vector2(0.5f, 0f); body.rectTransform.anchoredPosition = new Vector2(0f, 2f); body.rectTransform.sizeDelta = new Vector2(86f, 62f);
                var rim = CoastUiArt.Panel(root, "Rim", new Color(0.95f, 0.60f, 0.45f), 10);
                Rect(rim.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                rim.rectTransform.pivot = new Vector2(0.5f, 0f); rim.rectTransform.anchoredPosition = new Vector2(0f, 56f); rim.rectTransform.sizeDelta = new Vector2(98f, 16f);
                var soil = CoastUiArt.Panel(root, "Soil", new Color(0.35f, 0.22f, 0.14f), 8);
                Rect(soil.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                soil.rectTransform.pivot = new Vector2(0.5f, 0f); soil.rectTransform.anchoredPosition = new Vector2(0f, 60f); soil.rectTransform.sizeDelta = new Vector2(84f, 10f);
                // 식물
                if (stage >= 2)
                {
                    float h = stage == 2 ? 26f : stage == 3 ? 70f : stage == 4 ? 100f : 118f;
                    var stem = CoastUiArt.Panel(root, "Stem", new Color(0.35f, 0.65f, 0.35f), 4);
                    Rect(stem.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    stem.rectTransform.pivot = new Vector2(0.5f, 0f); stem.rectTransform.anchoredPosition = new Vector2(0f, 66f); stem.rectTransform.sizeDelta = new Vector2(7f, h);
                    // 잎 두 장
                    for (int k = 0; k < (stage >= 3 ? 2 : 1); k++)
                    {
                        var leaf = CoastUiArt.Panel(root, "Leaf" + k, new Color(0.40f, 0.72f, 0.40f), 12);
                        Rect(leaf.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                        leaf.rectTransform.pivot = new Vector2(k == 0 ? 1f : 0f, 0.5f); leaf.rectTransform.anchoredPosition = new Vector2(k == 0 ? -2f : 2f, 66f + h * (k == 0 ? 0.35f : 0.6f));
                        leaf.rectTransform.sizeDelta = new Vector2(30f, 16f); leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? 25f : -25f);
                    }
                    if (stage >= 4 && seed != null)
                    {
                        // 봉오리(작게) / 꽃(꽃잎 6장 + 중심)
                        float r = stage == 4 ? 12f : 22f;
                        var center = root;
                        if (stage == 5)
                            for (int k = 0; k < 6; k++)
                            {
                                float a = k * 60f * Mathf.Deg2Rad;
                                var petal = CoastUiArt.Panel(root, "Petal" + k, seed.petal, 14);
                                Rect(petal.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                                petal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                                petal.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * 18f, 66f + h + Mathf.Sin(a) * 18f);
                                petal.rectTransform.sizeDelta = new Vector2(26f, 26f);
                            }
                        var mid = CoastUiArt.Panel(root, "Mid", stage == 5 ? seed.center : Color.Lerp(seed.petal, new Color(0.4f, 0.7f, 0.4f), 0.5f), 14);
                        Rect(mid.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                        mid.rectTransform.pivot = new Vector2(0.5f, 0.5f); mid.rectTransform.anchoredPosition = new Vector2(0f, 66f + h); mid.rectTransform.sizeDelta = new Vector2(r * 1.4f, r * 1.4f);
                    }
                }
                else if (stage == 1)
                {
                    var sprout = CoastUiArt.Panel(root, "Seed", new Color(0.75f, 0.65f, 0.45f), 4);
                    Rect(sprout.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
                    sprout.rectTransform.pivot = new Vector2(0.5f, 0f); sprout.rectTransform.anchoredPosition = new Vector2(0f, 64f); sprout.rectTransform.sizeDelta = new Vector2(12f, 8f);
                }
                // 이름표
                string cap = seed == null ? Loc.T("빈 화분", "Empty")
                    : stage == 5 ? Loc.T($"{seed.Name.Replace(" 씨앗", "")} 만개!", $"{seed.Name} in bloom!")
                    : $"{seed.Name.Replace(" 씨앗", "")} {pot.growth}/{seed.waters}";
                var t = Text(root, "Cap", cap, 12, Navy, TextAnchor.MiddleCenter);
                Rect(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -60f), new Vector2(20f, -36f));
                CoastUiArt.OutlineText(t, new Color(1f, 1f, 1f, 0.8f), 1.5f);
                // 상태 버튼(작게, 위)
                string label; Color col;
                if (seed == null) { label = sel ? Loc.T("씨앗 고르기 ↓", "Pick a seed ↓") : Loc.T("심기", "Plant"); col = Mint; }
                else if (stage == 5) { label = Loc.T($"팔기 {seed.sell}G", $"Sell {seed.sell}G"); col = Coral; }
                else if (HomeData.CanWater(s, i)) { label = Loc.T("물 주기", "Water"); col = Sky; }
                else { label = Loc.T("자라는 중", "Growing"); col = Grey; }
                var b = Button(root, "Act", label, col, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(118f, 34f), () => PotTapped(i));
                b.GetComponentInChildren<Text>().fontSize = CoastHudLayout.Scaled(12);
                b.interactable = !(seed != null && stage < 5 && !HomeData.CanWater(s, i));
            }
        }

        private void PotTapped(int i)
        {
            var s = Save; HomeData.EnsurePots(s);
            var pot = s.pots[i]; var seed = HomeData.Seed(pot.seed);
            if (seed == null) { _selectedPot = _selectedPot == i ? -1 : i; RefreshPots(); Clear(_tray); BuildSeedTray(); return; }
            if (HomeData.IsBloomed(s, i))
            {
                int g = HomeData.Sell(s, i); _gm.Persist();
                CoastToast.Show(Loc.T($"꽃을 팔았어! +{g}G", $"Sold the flowers! +{g}G"));
            }
            else if (HomeData.Water(s, i))
            {
                _gm.Persist();
                CoastToast.Show(HomeData.IsBloomed(s, i) ? Loc.T("꽃이 피었다!", "It bloomed!") : Loc.T("물을 줬어. 다음 페이즈에 또.", "Watered. Again next phase."));
            }
            else CoastToast.Show(Loc.T("이번 페이즈엔 이미 물을 줬어. 스케줄을 진행하면 또 줄 수 있어.", "Already watered this phase."));
            RefreshPots(); RefreshMoney();
        }

        private void BuildSeedTray()
        {
            string head = _selectedPot >= 0 ? Loc.T($"화분 {_selectedPot + 1}에 심을 씨앗을 골라", $"Pick a seed for pot {_selectedPot + 1}") : Loc.T("빈 화분을 탭하고 씨앗을 고르면 심어져 · 페이즈마다 물 주기 · 꽃이 피면 팔기", "Tap an empty pot, pick a seed · water each phase · sell when it blooms");
            _hint = Text(_tray, "Hint", head, 13, Ink, TextAnchor.MiddleCenter);
            Rect(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, 0f));
            var scroll = MakeHScroll(_tray, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -30f), out var content);
            const float cw = 160f, gap = 8f;
            for (int i = 0; i < HomeData.Seeds.Length; i++)
            {
                var sd = HomeData.Seeds[i];
                var card = CoastUiArt.CutePill(content, "S_" + sd.id, new Color(0.92f, 0.97f, 0.88f), 16, 3);
                card.rectTransform.anchorMin = new Vector2(0f, 0f); card.rectTransform.anchorMax = new Vector2(0f, 1f); card.rectTransform.pivot = new Vector2(0f, 0.5f);
                card.rectTransform.anchoredPosition = new Vector2(i * (cw + gap), 0f); card.rectTransform.sizeDelta = new Vector2(cw, 0f);
                // 꽃 아이콘(꽃잎 6 + 중심)
                var ic = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
                ic.SetParent(card.transform, false); ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 1f); ic.pivot = new Vector2(0.5f, 1f); ic.anchoredPosition = new Vector2(0f, -14f); ic.sizeDelta = new Vector2(70f, 70f);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * 60f * Mathf.Deg2Rad;
                    var petal = CoastUiArt.Panel(ic, "P" + k, sd.petal, 12);
                    petal.rectTransform.anchorMin = petal.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    petal.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * 20f, Mathf.Sin(a) * 20f); petal.rectTransform.sizeDelta = new Vector2(26f, 26f);
                }
                var mid = CoastUiArt.Panel(ic, "M", sd.center, 12);
                mid.rectTransform.anchorMin = mid.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); mid.rectTransform.sizeDelta = new Vector2(24f, 24f);
                var nm = Text(card.transform, "Name", sd.Name, 14, Navy, TextAnchor.MiddleCenter);
                nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 12; nm.resizeTextMaxSize = 19;
                Rect(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 92f), new Vector2(-4f, 118f));
                var info = Text(card.transform, "Info", Loc.T($"물 {sd.waters}번 → {sd.sell}G", $"{sd.waters} waters → {sd.sell}G"), 12, Ink, TextAnchor.MiddleCenter);
                Rect(info.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 66f), new Vector2(-4f, 92f));
                bool can = _selectedPot >= 0 && Save.stats.money >= sd.price;
                var b = Button(card.transform, "Buy", $"{sd.price}G " + Loc.T("심기", "plant"), can ? Mint : Grey, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(130f, 46f), () =>
                {
                    if (_selectedPot < 0) { CoastToast.Show(Loc.T("먼저 빈 화분을 탭해.", "Tap an empty pot first.")); return; }
                    if (!HomeData.Plant(Save, _selectedPot, sd)) { CoastToast.Show(Loc.T("G가 모자라거나 빈 화분이 아니야.", "Not enough G or pot not empty.")); return; }
                    _gm.Persist(); CoastToast.Show(Loc.T($"{sd.Name}을 심었어. 물을 줘!", $"Planted {sd.Name}. Water it!"));
                    _selectedPot = -1; RefreshPots(); RefreshMoney(); Clear(_tray); BuildSeedTray();
                });
                b.interactable = can;
            }
            content.sizeDelta = new Vector2(HomeData.Seeds.Length * (cw + gap), 0f);
        }

        // ── 놀이 ─────────────────────────────────────────────────────────

        private void BuildPlay()
        {
            var bg = CoastHudLayout.MakeImage(_body, "Bg", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.98f, 0.93f, 0.82f));
            (string, string, string, int, HomeMiniGames.Kind)[] games =
            {
                (Loc.T("윷놀이", "Yut Nori"), Loc.T("윷 네 개를 던져 도담이보다 먼저 20칸을 돌아와. 윷·모는 한 번 더!", "Throw four sticks and get around 20 cells before Dodam. Yut/Mo = throw again!"), "윷", 60, HomeMiniGames.Kind.Yut),
                (Loc.T("구슬치기", "Marbles"), Loc.T("구슬을 끌어서 쏘고, 마당 밖으로 밀어낸 구슬마다 +8G. 3발.", "Drag to shoot; each marble knocked out of the yard is +8G. 3 shots."), "●", 40, HomeMiniGames.Kind.Marbles),
                (Loc.T("공기놀이", "Gonggi"), Loc.T("움직이는 손이 노란 구간에 왔을 때 잡아! 5번 중 성공한 만큼 +10G.", "Catch when the moving hand is in the yellow zone! +10G per success, 5 tries."), "✋", 50, HomeMiniGames.Kind.Gonggi),
            };
            for (int i = 0; i < games.Length; i++)
            {
                var g = games[i];
                var card = CoastUiArt.CutePill(_body, "G" + i, i == 0 ? new Color(0.98f, 0.80f, 0.70f) : i == 1 ? new Color(0.75f, 0.88f, 0.98f) : new Color(0.98f, 0.92f, 0.65f), 18, 4);
                Rect(card.rectTransform, new Vector2(0.03f, 0.68f - i * 0.325f), new Vector2(0.97f, 0.98f - i * 0.325f), Vector2.zero, Vector2.zero);
                var glyph = Text(card.transform, "Glyph", g.Item3, 34, Navy, TextAnchor.MiddleCenter);
                Rect(glyph.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(100f, 0f));
                var nm = Text(card.transform, "Name", g.Item1, 22, Navy, TextAnchor.MiddleLeft);
                Rect(nm.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(104f, 0f), new Vector2(-150f, -6f));
                var desc = Text(card.transform, "Desc", g.Item2, 12, Ink, TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Rect(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.58f), new Vector2(104f, 8f), new Vector2(-150f, 0f));
                var kind = g.Item5;
                Button(card.transform, "Play", Loc.T("하기", "Play"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(124f, 52f), () => StartMini(kind));
            }
        }

        private void BuildPlayTray()
        {
            int left = HomeData.RewardPlaysLeft(Save);
            var t = Text(_tray, "Info", Loc.T($"이번 주 보상 남은 횟수 {left}/{HomeData.MiniGameRewardPerWeek}\n횟수가 다 되면 연습(보상 없음)으로 놀 수 있어. 주차가 바뀌면 다시 채워져.",
                $"Reward plays left this week: {left}/{HomeData.MiniGameRewardPerWeek}\nAfter that it's practice (no reward). Refills each week."), 15, Ink, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            Rect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        }

        private void StartMini(HomeMiniGames.Kind kind)
        {
            if (_miniRoot != null) Destroy(_miniRoot);
            _miniRoot = new GameObject("Mini", typeof(RectTransform), typeof(Image));
            _miniRoot.transform.SetParent(_root, false);
            var rt = _miniRoot.GetComponent<RectTransform>();
            Rect(rt, Vector2.zero, Vector2.one, new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad));
            var im = _miniRoot.GetComponent<Image>(); im.color = new Color(0.12f, 0.08f, 0.12f, 0.96f); im.raycastTarget = true;
            bool rewardable = HomeData.RewardPlaysLeft(Save) > 0;
            HomeMiniGames.Start(kind, rt, rewardable, reward =>
            {
                int given = HomeData.GiveReward(Save, reward);
                if (given > 0) { _gm.Persist(); CoastToast.Show(Loc.T($"+{given}G!", $"+{given}G!")); }
                else if (reward > 0) CoastToast.Show(Loc.T("연습 게임 — 보상은 없어.", "Practice — no reward."));
                if (_miniRoot != null) Destroy(_miniRoot); _miniRoot = null;
                RefreshMoney(); Clear(_tray); BuildPlayTray();
            });
        }

        // ── 주인공 걷기 ──────────────────────────────────────────────────

        private void Update()
        {
            if (_tab != Tab.Room || _charRt == null || _roomHost == null) return;
            float dt = Time.unscaledDeltaTime;
            _wanderT -= dt;
            if (_wanderT <= 0f)
            {
                _wanderT = UnityEngine.Random.Range(3f, 7f);
                _charTarget = new Vector2(UnityEngine.Random.Range(0.15f, 0.85f), UnityEngine.Random.Range(0.04f, 0.36f));
                _onArrive = null;
            }
            var delta = _charTarget - _charPos;
            float speed = 0.28f;
            bool walking = delta.magnitude > 0.005f;
            if (walking)
            {
                var step = delta.normalized * speed * dt;
                if (step.magnitude >= delta.magnitude) { _charPos = _charTarget; walking = false; var cb = _onArrive; _onArrive = null; cb?.Invoke(); }
                else _charPos += step;
                if (Mathf.Abs(delta.x) > 0.01f) _charImg.rectTransform.localScale = new Vector3(delta.x < 0f ? -1f : 1f, 1f, 1f);
                _walkPhase += dt * 11f;
            }
            else _walkPhase = Mathf.MoveTowards(_walkPhase, 0f, dt * 6f);
            float bob = walking ? Mathf.Abs(Mathf.Sin(_walkPhase)) * 7f : 0f;
            float depth = Mathf.Lerp(1.0f, 0.78f, _charPos.y / 0.42f);   // 멀수록 작게
            _charRt.anchorMin = _charRt.anchorMax = new Vector2(_charPos.x, _charPos.y);
            _charRt.anchoredPosition = new Vector2(0f, bob);
            _charRt.localScale = new Vector3(depth, depth * (walking ? 1f + 0.03f * Mathf.Sin(_walkPhase * 2f) : 1f), 1f);
            if (_dragging == null) ReorderDepth();
        }

        /// 앞뒤 정렬: 벽걸이 → (바닥 가구·주인공을 y 내림차순: 멀수록 먼저) 순으로 형제 순서를 맞춘다.
        private void ReorderDepth()
        {
            if (_decoLayer == null) return;
            var floor = new List<(RectTransform rt, float y)>();
            int wallCount = 0;
            foreach (var kv in _placed)
            {
                var d = HomeData.Find(kv.Key);
                if (d != null && HomeData.IsWall(d)) { kv.Value.SetSiblingIndex(wallCount++); }
                else floor.Add((kv.Value, kv.Value.anchorMin.y));
            }
            if (_charRt != null && _charRt.parent == _decoLayer) floor.Add((_charRt, _charPos.y + 0.015f));
            floor.Sort((a, b) => b.y.CompareTo(a.y));
            for (int i = 0; i < floor.Count; i++) floor[i].rt.SetSiblingIndex(wallCount + i);
        }

        // ── 장식 그림(공용) ────────────────────────────────────────────────

        /// Resources/CoastRun/UI_Deco_<id>(마젠타 키 가능)가 있으면 그 그림, 없으면 색 알약 + 글자 플레이스홀더.
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
                var tag = Text(pill.transform, "Tag", d.tag, Mathf.RoundToInt(size.y * 0.30f), new Color(1f, 1f, 1f, 0.95f), TextAnchor.MiddleCenter);
                CoastUiArt.OutlineText(tag, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                if (size.y >= 100f)
                {
                    tag.rectTransform.offsetMin = new Vector2(0f, size.y * 0.14f);
                    var nm = Text(pill.transform, "Name", d.Name, 11, new Color(0.25f, 0.15f, 0.12f), TextAnchor.MiddleCenter);
                    nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 9; nm.resizeTextMaxSize = 14;
                    Rect(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(6f, 26f));
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
    }
}
