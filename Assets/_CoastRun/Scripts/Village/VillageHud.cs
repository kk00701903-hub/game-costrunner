using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 136차: 마을 화면 HUD — 시안(포켓캠프풍): 위 알약(체력·코인·시간/날씨·☰), 왼쪽 아래 가상 조이스틱,
    /// 오른쪽 둥근 버튼(행동/대화/가방), 아래 「마을: … Lv.N」 알약, 가까운 장소 안내 버튼.
    public class VillageHud : MonoBehaviour
    {
        public Vector2 Joy => _joy != null ? _joy.Value : Vector2.zero;
        public RectTransform JoyRect => _joy != null ? _joy.transform as RectTransform : null;
        public bool Locked;   // 팝업이 떠 있는 동안 이동·행동 막기

        Canvas _canvas; RectTransform _root;
        VirtualJoystick _joy;
        Text _hpT, _coinT, _timeT, _villageT, _promptT, _actT;
        Image _hpFill; RectTransform _prompt; Button _promptBtn, _actBtn;
        Action _onAct, _onTalk, _onBag, _onMenu, _onPrompt, _onTool;
        Text _toolT;
        public RectTransform Root => _root;
        RectTransform _popup;

        public static VillageHud Create(Action onAct, Action onTalk, Action onBag, Action onMenu, Action onTool = null)
        {
            var go = new GameObject("VillageHud");
            var h = go.AddComponent<VillageHud>();
            h._onAct = onAct; h._onTalk = onTalk; h._onBag = onBag; h._onMenu = onMenu; h._onTool = onTool;
            h.Build();
            return h;
        }

        void Build()
        {
            CoastUiCanvas.EnsureEventSystem();
            _canvas = CoastUiCanvas.Create("VillageHudCanvas", 120, transform);
            _root = CoastUiCanvas.Root(_canvas);

            // ── 위 알약 3개 + ☰ ──
            float y = -14f, hgt = 66f;
            var hp = Pill(_root, "Hp", new Color(1f, 0.90f, 0.93f), new Vector2(0f, 1f), new Vector2(14f, y), new Vector2(206f, hgt));
            IconIn(hp, "Heart", "♥", new Color(0.98f, 0.40f, 0.50f));
            var hpLab = CoastHudLayout.MakeText(hp, "L", Loc.T("체력", "HP"), 15, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(58f, -26f), new Vector2(-8f, -4f));
            hpLab.color = new Color(0.55f, 0.35f, 0.45f); hpLab.fontStyle = FontStyle.Bold;
            var bar = CoastUiArt.Panel(hp, "Bar", new Color(1f, 1f, 1f, 0.9f), 8); bar.raycastTarget = false;
            var brt = bar.rectTransform; brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0f); brt.offsetMin = new Vector2(58f, 10f); brt.offsetMax = new Vector2(-10f, 24f);
            _hpFill = CoastUiArt.Panel(bar.transform, "Fill", new Color(0.98f, 0.45f, 0.55f), 6); _hpFill.raycastTarget = false;
            _hpFill.rectTransform.anchorMin = Vector2.zero; _hpFill.rectTransform.anchorMax = new Vector2(1f, 1f); _hpFill.rectTransform.offsetMin = new Vector2(2f, 2f); _hpFill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            _hpT = CoastHudLayout.MakeText(hp, "V", "", 14, TextAnchor.LowerRight, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 26f), new Vector2(-10f, -4f));
            _hpT.color = new Color(0.45f, 0.30f, 0.40f); _hpT.fontStyle = FontStyle.Bold;

            var coin = Pill(_root, "Coin", new Color(1f, 0.96f, 0.80f), new Vector2(0f, 1f), new Vector2(230f, y), new Vector2(190f, hgt));
            IconIn(coin, "Coin", "●", new Color(0.98f, 0.75f, 0.20f));
            var coinLab = CoastHudLayout.MakeText(coin, "L", Loc.T("코인", "Coins"), 15, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(58f, -26f), new Vector2(-8f, -4f));
            coinLab.color = new Color(0.60f, 0.45f, 0.20f); coinLab.fontStyle = FontStyle.Bold;
            _coinT = CoastHudLayout.MakeText(coin, "V", "", 22, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 6f), new Vector2(-8f, -26f));
            _coinT.color = new Color(0.45f, 0.32f, 0.12f); _coinT.fontStyle = FontStyle.Bold;
            _coinT.resizeTextForBestFit = true; _coinT.resizeTextMinSize = 12; _coinT.resizeTextMaxSize = CoastHudLayout.Scaled(22);

            var time = Pill(_root, "Time", new Color(0.88f, 0.95f, 1f), new Vector2(1f, 1f), new Vector2(-80f, y), new Vector2(206f, hgt));
            IconIn(time, "Clock", "◔", new Color(0.35f, 0.65f, 0.95f));
            var timeLab = CoastHudLayout.MakeText(time, "L", Loc.T("시간", "Time"), 15, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(58f, -26f), new Vector2(-8f, -4f));
            timeLab.color = new Color(0.30f, 0.45f, 0.65f); timeLab.fontStyle = FontStyle.Bold;
            _timeT = CoastHudLayout.MakeText(time, "V", "", 18, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 6f), new Vector2(-6f, -26f));
            _timeT.color = new Color(0.20f, 0.35f, 0.55f); _timeT.fontStyle = FontStyle.Bold;
            _timeT.resizeTextForBestFit = true; _timeT.resizeTextMinSize = 11; _timeT.resizeTextMaxSize = CoastHudLayout.Scaled(18);

            var menu = CoastUiArt.CutePill(_root, "Menu", new Color(0.30f, 0.32f, 0.48f), 16, 3);
            var mrt = menu.rectTransform; mrt.anchorMin = mrt.anchorMax = new Vector2(1f, 1f); mrt.pivot = new Vector2(1f, 1f);
            mrt.anchoredPosition = new Vector2(-12f, y); mrt.sizeDelta = new Vector2(58f, hgt); menu.raycastTarget = true;
            var mt = CoastHudLayout.MakeText(mrt, "T", "☰", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); mt.color = Color.white;
            var mb = menu.gameObject.AddComponent<Button>(); mb.transition = Selectable.Transition.None; mb.onClick.AddListener(() => { CoastPrefs.Vibrate(); _onMenu?.Invoke(); });

            // ── 왼쪽 아래 조이스틱 ──
            _joy = VirtualJoystick.Create(_root, new Vector2(130f, 300f), 200f);   // 149차: 버튼과 함께 위로(하단 제스처 영역 회피), 조금 크게

            // ── 오른쪽 둥근 버튼 4개 ── 149차(사용자): 전체를 188 만큼 위로(엄지 닿는 높이)
            // 137차: 잡기 버튼 위 「도구」(잠자리채/방망이 고르기) — 행동 버튼 라벨은 고른 도구를 따른다
            Round(_root, "Tool", "⚒", Loc.T("도구", "Tool"), new Color(0.60f, 0.52f, 0.92f), new Vector2(-64f, 640f), 96f, () => _onTool?.Invoke(), out _toolT);
            _actBtn = Round(_root, "Act", "◎", Loc.T("잡기", "Act"), new Color(0.45f, 0.78f, 0.55f), new Vector2(-64f, 518f), 108f, () => _onAct?.Invoke(), out _actT);
            Round(_root, "Talk", "…", Loc.T("대화", "Talk"), new Color(0.98f, 0.62f, 0.72f), new Vector2(-64f, 396f), 96f, () => _onTalk?.Invoke(), out _);
            Round(_root, "Bag", "▣", Loc.T("가방", "Bag"), new Color(0.98f, 0.78f, 0.35f), new Vector2(-64f, 284f), 96f, () => _onBag?.Invoke(), out _);

            // ── 아래 마을 알약 ──
            var vp = CoastUiArt.CutePill(_root, "Village", new Color(0.36f, 0.30f, 0.52f, 0.92f), 20, 3); vp.raycastTarget = false;
            var vrt = vp.rectTransform; vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0f); vrt.pivot = new Vector2(0.5f, 0f);
            vrt.anchoredPosition = new Vector2(0f, 14f); vrt.sizeDelta = new Vector2(360f, 52f);
            _villageT = CoastHudLayout.MakeText(vrt, "T", "", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, 0f));
            _villageT.color = Color.white; _villageT.fontStyle = FontStyle.Bold;
            _villageT.resizeTextForBestFit = true; _villageT.resizeTextMinSize = 11; _villageT.resizeTextMaxSize = CoastHudLayout.Scaled(18);

            // ── 장소 안내(가까이 가면 뜸) ──
            var pr = CoastUiArt.GlossyPill(_root, "Prompt", new Color(1f, 0.72f, 0.84f), 24, 8);
            _prompt = pr.rectTransform; _prompt.anchorMin = _prompt.anchorMax = new Vector2(0.5f, 0f); _prompt.pivot = new Vector2(0.5f, 0f);
            _prompt.anchoredPosition = new Vector2(46f, 300f); _prompt.sizeDelta = new Vector2(372f, 76f); pr.raycastTarget = true;
            _promptT = CoastHudLayout.MakeText(_prompt, "T", "", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, 0f));
            _promptT.color = Color.white; _promptT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_promptT, new Color(0.55f, 0.18f, 0.35f, 0.8f), 1.8f);
            _promptT.resizeTextForBestFit = true; _promptT.resizeTextMinSize = 12; _promptT.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            _promptBtn = pr.gameObject.AddComponent<Button>(); _promptBtn.transition = Selectable.Transition.None;
            _promptBtn.onClick.AddListener(() => { CoastPrefs.Vibrate(); _onPrompt?.Invoke(); });
            _prompt.gameObject.SetActive(false);
        }

        static RectTransform Pill(RectTransform parent, string name, Color fill, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var p = CoastUiArt.CutePill(parent, name, fill, 20, 4); p.raycastTarget = false;
            var rt = p.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(anchor.x, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static void IconIn(RectTransform pill, string icon, string glyph, Color col)
        {
            var circ = CoastUiArt.GlossyPill(pill, "Ic", col, 20, 5); circ.raycastTarget = false;
            var crt = circ.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f); crt.pivot = new Vector2(0f, 0.5f);
            crt.anchoredPosition = new Vector2(8f, 0f); crt.sizeDelta = new Vector2(44f, 44f);
            var sp = CoastUiArt.Icon(icon);
            if (sp != null)
            {
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(crt, false); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.sizeDelta = new Vector2(32f, 32f);
            }
            else
            {
                var g = CoastHudLayout.MakeText(crt, "G", glyph, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                g.color = Color.white; g.fontStyle = FontStyle.Bold;
            }
        }

        static Button Round(RectTransform parent, string name, string glyph, string label, Color col, Vector2 pos, float size, Action on, out Text labelT)
        {
            var edge = CoastUiArt.CutePill(parent, name + "E", Color.Lerp(col, Color.black, 0.25f), (int)(size * 0.5f), 0); edge.raycastTarget = false;
            var ert = edge.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(1f, 0f); ert.pivot = new Vector2(0.5f, 0.5f);
            ert.anchoredPosition = pos + new Vector2(0f, -4f); ert.sizeDelta = new Vector2(size, size);
            var b = CoastUiArt.GlossyPill(parent, name, col, (int)(size * 0.5f), 10); b.raycastTarget = true;
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(size, size);
            var g = CoastHudLayout.MakeText(rt, "G", glyph, (int)(size * 0.34f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 14f), new Vector2(0f, -2f));
            g.color = Color.white; g.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(g, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            labelT = CoastHudLayout.MakeText(rt, "L", label, 14, TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(4f, 10f), new Vector2(-4f, 0f));
            labelT.color = Color.white; labelT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(labelT, new Color(0f, 0f, 0f, 0.45f), 1.5f);
            labelT.resizeTextForBestFit = true; labelT.resizeTextMinSize = 10; labelT.resizeTextMaxSize = CoastHudLayout.Scaled(14);
            var btn = b.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); on?.Invoke(); });
            return btn;
        }

        public void SetStatus(int hp, int hpMax, int coins, string time, string village)
        {
            if (_hpT != null) _hpT.text = $"{hp}/{hpMax}";
            if (_hpFill != null) _hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(hpMax > 0 ? hp / (float)hpMax : 0f), 1f);
            if (_coinT != null) _coinT.text = coins.ToString("N0");
            if (_timeT != null) _timeT.text = time;
            if (_villageT != null) _villageT.text = village;
        }

        public void SetAction(string label) { if (_actT != null && _actT.text != label) _actT.text = label; }
        public void SetTool(string label) { if (_toolT != null) _toolT.text = label; }

        public void SetPrompt(string text, Action on)
        {
            _onPrompt = on;
            if (_prompt == null) return;
            bool show = !string.IsNullOrEmpty(text) && !Locked;
            if (_prompt.gameObject.activeSelf != show) _prompt.gameObject.SetActive(show);
            if (show && _promptT.text != text) _promptT.text = text;
        }

        /// 선택 팝업(2~4개): 제목 + 세로 버튼. 닫기 버튼 포함.
        public void Choice(string title, string sub, (string label, Color col, Action on)[] items)
        {
            ClosePopup(); Locked = true;
            var dim = CoastHudLayout.MakeImage(_root, "Popup", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0f, 0f, 0f, 0.45f));
            dim.raycastTarget = true; _popup = dim.rectTransform;
            float h = 150f + items.Length * 84f + (string.IsNullOrEmpty(sub) ? 0f : 40f);
            var edge = CoastUiArt.CutePill(_popup, "CardE", new Color(0.95f, 0.62f, 0.75f), 30, 0); edge.raycastTarget = false;
            var ert = edge.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0.5f); ert.sizeDelta = new Vector2(570f, h + 10f);
            var cardImg = CoastUiArt.CutePill(_popup, "Card", new Color(1f, 0.97f, 0.92f), 26, 0); cardImg.raycastTarget = true;
            var card = cardImg.rectTransform; card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f); card.sizeDelta = new Vector2(560f, h);
            var t = CoastHudLayout.MakeText(card, "Title", title, 30, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -28f));
            t.color = EventCardKit.BrownInk; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            float top = 96f;
            if (!string.IsNullOrEmpty(sub))
            {
                var s = CoastHudLayout.MakeText(card, "Sub", sub, 17, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -top - 40f), new Vector2(-28f, -top));
                s.color = new Color(0.45f, 0.38f, 0.42f); s.horizontalOverflow = HorizontalWrapMode.Wrap; s.verticalOverflow = VerticalWrapMode.Truncate;
                s.resizeTextForBestFit = true; s.resizeTextMinSize = 11; s.resizeTextMaxSize = CoastHudLayout.Scaled(17);
                top += 44f;
            }
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                var b = CoastUiArt.GlossyPill(card, "B" + i, it.col, 22, 8); b.raycastTarget = true;
                var brt = b.rectTransform; brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -(top + i * 84f)); brt.sizeDelta = new Vector2(-64f, 72f);
                var bt = CoastHudLayout.MakeText(brt, "T", it.label, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 3f), new Vector2(-10f, 0f));
                bt.color = Color.white; bt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(bt, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 12; bt.resizeTextMaxSize = CoastHudLayout.Scaled(22);
                var btn = b.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
                var cap = it.on;
                btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); ClosePopup(); cap?.Invoke(); });
            }
            var close = CoastUiArt.CutePill(card, "Close", new Color(0.92f, 0.90f, 0.95f), 18, 2);
            var crt = close.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-10f, -10f); crt.sizeDelta = new Vector2(44f, 44f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(crt, "T", "✕", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); ct.color = new Color(0.35f, 0.32f, 0.45f);
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(() => { CoastPrefs.Vibrate(); ClosePopup(); });
        }

        public void ClosePopup()
        {
            if (_popup != null) { Destroy(_popup.gameObject); _popup = null; }
            Locked = false;
        }

        public bool PopupOpen => _popup != null;

        /// 화면 가운데 큰 말풍선 한 줄(대화). 탭하면 닫힘.
        public void Bubble(string who, string line)
        {
            ClosePopup(); Locked = true;
            var dim = CoastHudLayout.MakeImage(_root, "Popup", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0f, 0f, 0f, 0.01f));
            dim.raycastTarget = true; _popup = dim.rectTransform;
            var bub = CoastUiArt.CutePill(_popup, "Bub", new Color(1f, 0.97f, 0.99f, 0.97f), 26, 5);
            var brt = bub.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f); brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 430f); brt.sizeDelta = new Vector2(600f, 150f); bub.raycastTarget = false;
            var nm = CoastHudLayout.MakeText(brt, "N", who, 17, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -36f), new Vector2(-24f, -10f));
            nm.color = new Color(0.95f, 0.45f, 0.60f); nm.fontStyle = FontStyle.Bold;
            var tx = CoastHudLayout.MakeText(brt, "T", line, 22, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(24f, 14f), new Vector2(-24f, -40f));
            tx.color = EventCardKit.BrownInk; tx.horizontalOverflow = HorizontalWrapMode.Wrap; tx.verticalOverflow = VerticalWrapMode.Truncate;
            tx.resizeTextForBestFit = true; tx.resizeTextMinSize = 13; tx.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            var hint = CoastHudLayout.MakeText(brt, "H", Loc.T("탭해서 닫기 ▼", "Tap to close ▼"), 12, TextAnchor.LowerRight, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(-18f, 26f));
            hint.color = new Color(0.6f, 0.55f, 0.6f);
            var b = dim.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => ClosePopup());
        }

        void OnDestroy() { if (_canvas != null) Destroy(_canvas.gameObject); }
    }

    /// 가상 조이스틱: 왼쪽 아래 고정 링 + 손잡이. 값은 -1..1.
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        RectTransform _ring, _knob; float _radius;

        public static VirtualJoystick Create(RectTransform parent, Vector2 center, float size)
        {
            var ring = CoastUiArt.CutePill(parent, "Joystick", new Color(1f, 1f, 1f, 0.28f), (int)(size * 0.5f), 4);
            var rt = ring.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center; rt.sizeDelta = new Vector2(size, size); ring.raycastTarget = true;
            var knob = CoastUiArt.GlossyPill(rt, "Knob", new Color(1f, 1f, 1f, 0.85f), (int)(size * 0.2f), 6); knob.raycastTarget = false;
            var krt = knob.rectTransform; krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f); krt.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
            var j = ring.gameObject.AddComponent<VirtualJoystick>();
            j._ring = rt; j._knob = krt; j._radius = size * 0.34f;
            // 방향 화살표 4개(연하게)
            string[] ar = { "▲", "▼", "◀", "▶" }; Vector2[] ap = { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var t = CoastHudLayout.MakeText(rt, "A" + i, ar[i], 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                t.rectTransform.sizeDelta = new Vector2(30f, 30f); t.rectTransform.anchoredPosition = ap[i] * (size * 0.40f); t.color = new Color(1f, 1f, 1f, 0.55f);
            }
            return j;
        }

        // 149차(사용자: 「상하좌우 움직여지지 않는다」): 드래그 이벤트(임계값·이벤트 유실)에 기대지 않고, 누른 손가락/마우스를
        // Update 에서 직접 추적해 매 프레임 값을 갱신한다. 손가락 id 를 기억해 멀티터치(버튼 동시 조작)에도 흔들리지 않는다.
        int _pointerId = int.MinValue; Camera _cam;
        void MoveTo(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_ring, screen, _cam, out var lp);
            var v = Vector2.ClampMagnitude(lp / _radius, 1f);
            Value = v; _knob.anchoredPosition = v * _radius;
        }
        void Move(PointerEventData e) { _cam = e.pressEventCamera; MoveTo(e.position); }
        public void OnPointerDown(PointerEventData e) { _pointerId = e.pointerId; Move(e); }
        public void OnDrag(PointerEventData e) { if (e.pointerId == _pointerId) Move(e); }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId != _pointerId && _pointerId != int.MinValue) return; Release(); }
        void Release() { _pointerId = int.MinValue; Value = Vector2.zero; _knob.anchoredPosition = Vector2.zero; }
        void Update()
        {
            if (_pointerId == int.MinValue) return;
            if (_pointerId >= 0)
            {
                // 터치: fingerId 로 찾기(없으면 놓은 것)
                bool found = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var tc = Input.GetTouch(i);
                    if (tc.fingerId != _pointerId) continue;
                    found = true;
                    if (tc.phase == TouchPhase.Ended || tc.phase == TouchPhase.Canceled) Release(); else MoveTo(tc.position);
                    break;
                }
                if (!found && Input.touchCount == 0 && !Input.GetMouseButton(0)) Release();
                else if (!found && Input.GetMouseButton(0)) MoveTo(Input.mousePosition);   // 에디터 시뮬레이션(터치 id 0 = 마우스)
            }
            else
            {
                if (!Input.GetMouseButton(0)) Release(); else MoveTo(Input.mousePosition);
            }
        }
        void OnDisable() { Release(); }
    }
}
