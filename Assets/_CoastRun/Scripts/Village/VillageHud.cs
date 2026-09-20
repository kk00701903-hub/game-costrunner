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
        public RectTransform JoyRect => _joy != null ? _joy.transform as RectTransform : null;   // 160차: 터치 영역(JoyZone)
        public bool Locked;   // 팝업이 떠 있는 동안 이동·행동 막기
        public CameraPad CamPad;   // 162차: 오른쪽 반 화면(카메라 회전 드래그 · 탭 = 행동)

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
            // 160차(사용자): 탕탕특공대식 플로팅 조이스틱 — 아래 화면 아무 곳이나 누르면 그 자리에 생긴다(힌트는 왼쪽 아래)
            _joy = VirtualJoystick.Create(_root, new Vector2(150f, 260f), 200f);

            // ── 오른쪽 둥근 버튼 4개 ── 149차(사용자): 전체를 188 만큼 위로(엄지 닿는 높이)
            // 137차: 잡기 버튼 위 「도구」(잠자리채/방망이 고르기) — 행동 버튼 라벨은 고른 도구를 따른다
            // 153차(사용자): 둥근 버튼 4개를 왼쪽 위(체력 알약 아래)에 세로로
            var tl = new Vector2(0f, 1f);
            Round(_root, "Tool", "⚒", Loc.T("도구", "Tool"), new Color(0.60f, 0.52f, 0.92f), new Vector2(64f, -150f), 96f, () => _onTool?.Invoke(), out _toolT, tl);
            _toolGlyph = _toolT != null ? _toolT.transform.parent.Find("G")?.GetComponent<Text>() : null;
            // 162차(사용자: 「좌측 상단의 휘두르기 버튼은 지워줘」): 행동(잡기/휘두르기/들어가기…) 라운드 버튼 삭제 — 오른쪽 반 화면 탭이 행동이다(CameraPad.OnTap)
            _actBtn = null; _actT = null;
            Round(_root, "Talk", "…", Loc.T("대화", "Talk"), new Color(0.98f, 0.62f, 0.72f), new Vector2(64f, -272f), 96f, () => _onTalk?.Invoke(), out _, tl);
            Round(_root, "Bag", "▣", Loc.T("가방", "Bag"), new Color(0.98f, 0.78f, 0.35f), new Vector2(64f, -394f), 96f, () => _onBag?.Invoke(), out _, tl);
            // 162차: 오른쪽 반 = 투명 카메라 패드(드래그 = 카메라 돌리기, 탭 = 행동/휘두르기). 힌트 링은 조이스틱과 같은 크기로 오른쪽 아래에
            CamPad = CameraPad.Create(_root, new Vector2(-150f, 260f), 200f);

            // ── 아래 마을 알약 ──
            var vp = CoastUiArt.CutePill(_root, "Village", new Color(0.36f, 0.30f, 0.52f, 0.92f), 20, 3); vp.raycastTarget = false;
            var vrt = vp.rectTransform; vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0f); vrt.pivot = new Vector2(0.5f, 0f);
            vrt.anchoredPosition = new Vector2(0f, 14f); vrt.sizeDelta = new Vector2(360f, 52f);
            _villageT = CoastHudLayout.MakeText(vrt, "T", "", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, 0f));
            _villageT.color = Color.white; _villageT.fontStyle = FontStyle.Bold;
            _villageT.resizeTextForBestFit = true; _villageT.resizeTextMinSize = 11; _villageT.resizeTextMaxSize = CoastHudLayout.Scaled(18);

            // ── 160차: 오늘 미션 띠(위 알약 아래, 오른쪽) ──
            var mp = CoastUiArt.CutePill(_root, "Mission", new Color(0.98f, 0.72f, 0.42f, 0.94f), 18, 3); mp.raycastTarget = false;
            _mission = mp.rectTransform; _mission.anchorMin = _mission.anchorMax = new Vector2(1f, 1f); _mission.pivot = new Vector2(1f, 1f);
            _mission.anchoredPosition = new Vector2(-12f, -92f); _mission.sizeDelta = new Vector2(330f, 46f);
            _missionT = CoastHudLayout.MakeText(_mission, "T", "", 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, 0f));
            _missionT.color = Color.white; _missionT.fontStyle = FontStyle.Bold;
            _missionT.resizeTextForBestFit = true; _missionT.resizeTextMinSize = 10; _missionT.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            CoastUiArt.OutlineText(_missionT, new Color(0.45f, 0.25f, 0.10f, 0.7f), 1.4f);
            _mission.gameObject.SetActive(false);

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

        static Button Round(RectTransform parent, string name, string glyph, string label, Color col, Vector2 pos, float size, Action on, out Text labelT, Vector2? anchor = null)
        {
            var an = anchor ?? new Vector2(1f, 0f);
            var edge = CoastUiArt.CutePill(parent, name + "E", Color.Lerp(col, Color.black, 0.25f), (int)(size * 0.5f), 0); edge.raycastTarget = false;
            var ert = edge.rectTransform; ert.anchorMin = ert.anchorMax = an; ert.pivot = new Vector2(0.5f, 0.5f);
            ert.anchoredPosition = pos + new Vector2(0f, -4f); ert.sizeDelta = new Vector2(size, size);
            var b = CoastUiArt.GlossyPill(parent, name, col, (int)(size * 0.5f), 10); b.raycastTarget = true;
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = an; rt.pivot = new Vector2(0.5f, 0.5f);
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

        RectTransform _mission; Text _missionT;
        /// 160차: 오늘 미션 한 줄(없으면 숨김)
        public void SetMission(string text)
        {
            if (_mission == null) return;
            bool on = !string.IsNullOrEmpty(text);
            if (_mission.gameObject.activeSelf != on) _mission.gameObject.SetActive(on);
            if (on && _missionT != null && _missionT.text != text) _missionT.text = text;
        }
        public void SetAction(string label) { if (_actT != null && _actT.text != label) _actT.text = label; }
        public void SetTool(string label, int toolIdx = 0)
        {
            if (_toolT != null) _toolT.text = label;
            // 154차: 클링 생성 도구 아이콘(UI_Tool_Net/Bat/Axe/Pick) 이 있으면 글리프 대신 그림
            if (_toolGlyph != null)
            {
                string[] keys = { "UI_Tool_Net", "UI_Tool_Bat", "UI_Tool_Axe", "UI_Tool_Pick", "UI_Tool_Rod" };   // 168차: 낚싯대
                string key = keys[Mathf.Clamp(toolIdx, 0, 4)];
                var sp = Resources.Load<Sprite>("CoastRun/Textures/Village/" + key);
                if (sp == null)
                {
                    // 임포터가 Sprite 가 아니어도(메타가 되돌아가는 경우) 텍스처에서 직접 만든다
                    if (!_toolSprites.TryGetValue(key, out sp))
                    {
                        var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/" + key);
                        sp = tex != null ? Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
                        _toolSprites[key] = sp;
                    }
                }
                if (sp != null)
                {
                    if (_toolImg == null) { var go = new GameObject("I", typeof(RectTransform), typeof(Image)); go.transform.SetParent(_toolGlyph.transform.parent, false); _toolImg = go.GetComponent<Image>(); _toolImg.raycastTarget = false; _toolImg.preserveAspect = true; var r = _toolImg.rectTransform; r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = new Vector2(0f, 10f); r.sizeDelta = new Vector2(56f, 56f); }
                    _toolImg.sprite = sp; _toolImg.gameObject.SetActive(true); _toolGlyph.gameObject.SetActive(false);
                }
                else { if (_toolImg != null) _toolImg.gameObject.SetActive(false); _toolGlyph.gameObject.SetActive(true); }
            }
        }
        Text _toolGlyph; Image _toolImg; readonly System.Collections.Generic.Dictionary<string, Sprite> _toolSprites = new System.Collections.Generic.Dictionary<string, Sprite>();

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
        /// 개발용: 화면 비율 좌표(0~1)에서 조이스틱을 잡고 있는 것처럼
        public void DevJoy(float nx, float ny, float sec) { if (_joy != null) _joy.DebugHold(new Vector2(nx * Screen.width, ny * Screen.height), sec); }
        public string JoyDiag() => _joy != null ? _joy.Diag() : "no joystick";

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
    /// 160차(사용자: 「탕탕특공대 같은 플로팅 조이스틱」): 화면 아래 아무 곳이나 누르면 그 자리에 조이스틱이 생기고,
    /// 손가락을 따라 링이 끌려오며(원 밖으로 나가면 링이 따라붙는다), 떼면 사라진다. 평소엔 아주 연한 힌트만.
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        RectTransform _zone, _ring, _knob, _hint; CanvasGroup _ringGrp, _hintGrp; float _radius, _size;
        Vector2 _origin; float _fade;

        /// parent 아래에 「투명한 터치 영역 + 떠다니는 링」을 만든다. zoneHeight = 아래에서부터 몇 px(720 기준)을 조이스틱 영역으로 쓸지.
        public static VirtualJoystick Create(RectTransform parent, Vector2 hintCenter, float size)
        {
            // 1) 터치 영역(투명) — 맨 아래 형제로 두어 버튼·프롬프트가 먼저 먹는다
            // 화면 아래 78% 를 조이스틱 영역으로(위 알약·☰ 는 제외). 로컬 좌표 = 화면 좌표가 되도록 오프셋 0.
            // 162차(사용자: 「화면 반을 나눠 왼쪽 이동 · 오른쪽 카메라」): 조이스틱 영역은 왼쪽 반(0~0.5), 오른쪽 반은 CameraPad
            var zoneImg = CoastHudLayout.MakeImage(parent, "JoyZone", new Vector2(0f, 0f), new Vector2(0.5f, 0.78f),
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.004f));
            zoneImg.raycastTarget = true;
            var zone = zoneImg.rectTransform; zone.SetAsFirstSibling();

            var j = zoneImg.gameObject.AddComponent<VirtualJoystick>();
            j._zone = zone; j._size = size; j._radius = size * 0.34f;

            // 2) 평소 힌트(아주 연한 링) — 어디를 눌러야 하는지 한 번은 보이게
            var hint = CoastUiArt.CutePill(zone, "JoyHint", new Color(1f, 1f, 1f, 0.05f), (int)(size * 0.5f), 0);   // 166차(사용자: 「거의 투명하게」) hint.raycastTarget = false;
            var hrt = hint.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 0f); hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = hintCenter; hrt.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
            var ht = CoastHudLayout.MakeText(hrt, "T", "✥", 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ht.color = new Color(1f, 1f, 1f, 0.35f);
            j._hint = hrt; j._hintGrp = hint.gameObject.AddComponent<CanvasGroup>(); j._hintGrp.alpha = 0.22f; j._hintGrp.blocksRaycasts = false;

            // 3) 떠다니는 링 + 손잡이
            var ring = CoastUiArt.CutePill(zone, "Joystick", new Color(1f, 1f, 1f, 0.10f), (int)(size * 0.5f), 4); ring.raycastTarget = false;
            // ScreenPointToLocalPointInRectangle 은 **영역의 피벗(가운데) 기준** 좌표를 준다 → 링도 가운데 앵커여야 손가락 자리에 정확히 뜬다
            var rt = ring.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(size, size);
            var knob = CoastUiArt.GlossyPill(rt, "Knob", new Color(1f, 1f, 1f, 0.32f), (int)(size * 0.2f), 6); knob.raycastTarget = false;
            var krt = knob.rectTransform; krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f); krt.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
            string[] ar = { "▲", "▼", "◀", "▶" }; Vector2[] ap = { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var t = CoastHudLayout.MakeText(rt, "A" + i, ar[i], 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                t.rectTransform.sizeDelta = new Vector2(30f, 30f); t.rectTransform.anchoredPosition = ap[i] * (size * 0.40f); t.color = new Color(1f, 1f, 1f, 0.40f);
            }
            j._ring = rt; j._knob = krt;
            j._ringGrp = ring.gameObject.AddComponent<CanvasGroup>(); j._ringGrp.alpha = 0f; j._ringGrp.blocksRaycasts = false;
            j._origin = Vector2.zero;
            return j;
        }

        int _pointerId = int.MinValue; Camera _cam;
        // 개발/검증용: 손가락 없이 화면 좌표를 누른 것처럼 잡고 있는다(원 그리며 드래그)
        float _dbgUntil; Vector2 _dbgStart;
        public void DebugHold(Vector2 screen, float seconds)
        { _cam = null; _pointerId = -999; _dbgStart = screen; Begin(screen); _dbgUntil = Time.unscaledTime + seconds; }

        bool ToLocal(Vector2 screen, out Vector2 lp) => RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, screen, _cam, out lp);

        void Begin(Vector2 screen)
        {
            if (!ToLocal(screen, out var lp)) return;
            _origin = lp; _ring.anchoredPosition = lp; _knob.anchoredPosition = Vector2.zero; Value = Vector2.zero;
            _fade = 1f; _ringGrp.alpha = 1f; _ring.localScale = Vector3.one * 0.82f;
        }

        void MoveTo(Vector2 screen)
        {
            if (!ToLocal(screen, out var lp)) return;
            var d = lp - _origin;
            // 탕탕특공대식: 반지름을 넘어가면 링(기준점)이 손가락을 따라 끌려온다
            if (d.magnitude > _radius) { _origin = lp - d.normalized * _radius; _ring.anchoredPosition = _origin; d = d.normalized * _radius; }
            Value = d / _radius;
            _knob.anchoredPosition = d;
        }

        void Release()
        {
            _pointerId = int.MinValue; Value = Vector2.zero; _knob.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData e) { _cam = e.pressEventCamera; _pointerId = e.pointerId; Begin(e.position); }
        public void OnDrag(PointerEventData e) { if (e.pointerId == _pointerId) { _cam = e.pressEventCamera; MoveTo(e.position); } }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId != _pointerId && _pointerId != int.MinValue) return; Release(); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool held = _pointerId != int.MinValue;
            // 링 나타나기/사라지기 + 살짝 커지는 연출
            _fade = Mathf.MoveTowards(_fade, held ? 1f : 0f, dt * (held ? 12f : 6f));
            if (_ringGrp != null) _ringGrp.alpha = _fade * 0.6f;   // 166차: 누르는 동안도 60% 만
            if (_ring != null) _ring.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, _fade);
            if (_hintGrp != null) _hintGrp.alpha = Mathf.MoveTowards(_hintGrp.alpha, held ? 0f : 0.22f, dt * 4f);
            if (!held) return;
            if (_dbgUntil > 0f)
            {
                if (Time.unscaledTime < _dbgUntil)
                { float a = Time.unscaledTime * 2.2f; MoveTo(_dbgStart + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 120f); return; }
                _dbgUntil = 0f; Release(); return;
            }
            // 152차: 폴링은 보조 — 이벤트로 눌린 손가락을 못 찾아도 놓지 않는다(놓기는 OnPointerUp/터치 Ended 에서만)
            if (_pointerId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var tc = Input.GetTouch(i);
                    if (tc.fingerId != _pointerId) continue;
                    if (tc.phase == TouchPhase.Ended || tc.phase == TouchPhase.Canceled) Release(); else MoveTo(tc.position);
                    return;
                }
                if (Input.touchCount == 0 && Input.GetMouseButton(0)) MoveTo(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0)) MoveTo(Input.mousePosition);
            else Release();
        }
        public string Diag()
        {
            var zr = _zone.rect;
            return $"zone={zr.size} ring={_ring.anchoredPosition} knob={_knob.anchoredPosition} value={Value} alpha={_ringGrp.alpha:F2} hint={_hintGrp.alpha:F2} held={_pointerId != int.MinValue} raycast={_zone.GetComponent<Image>().raycastTarget}";
        }
        void OnDisable() { Release(); }
    }

    /// 162차(사용자: 「오른쪽에는 카메라 무빙 조절하는 투명 버튼 · 오른쪽 화면을 타격하면 휘두르기」):
    /// 화면 오른쪽 반(아래 78%)의 투명 터치 영역. 가로 드래그 → OnOrbit(픽셀 Δx, 720 기준), 짧게 톡(0.28 s·18 px 안) → OnTap.
    /// 힌트 링(「⟲ 카메라 / 톡 = 휘두르기」)은 조이스틱 힌트와 같은 크기·투명도로 오른쪽 아래.
    public class CameraPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<float> OnOrbit; public Action OnTap;
        RectTransform _zone, _hint; CanvasGroup _hintGrp, _ringGrp; RectTransform _ring;
        int _pointerId = int.MinValue; Vector2 _down, _last; float _downAt; bool _dragged; Camera _cam;
        public static CameraPad Create(RectTransform parent, Vector2 hintFromRight, float size)
        {
            var zoneImg = CoastHudLayout.MakeImage(parent, "CamPad", new Vector2(0.5f, 0f), new Vector2(1f, 0.78f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.004f));
            zoneImg.raycastTarget = true; var zone = zoneImg.rectTransform; zone.SetSiblingIndex(1);   // JoyZone 바로 위(버튼·프롬프트보다 아래)
            var p = zoneImg.gameObject.AddComponent<CameraPad>(); p._zone = zone;
            var hint = CoastUiArt.CutePill(zone, "CamHint", new Color(1f, 1f, 1f, 0.05f), (int)(size * 0.5f), 0); hint.raycastTarget = false;
            var hrt = hint.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 0f); hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = hintFromRight; hrt.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
            var ht = CoastHudLayout.MakeText(hrt, "T", "⟲", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 14f)); ht.color = new Color(1f, 1f, 1f, 0.35f);
            var hl = CoastHudLayout.MakeText(hrt, "L", Loc.T("카메라 · 톡=휘두르기", "camera · tap=swing"), 13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(-40f, -8f), new Vector2(40f, -60f)); hl.color = new Color(1f, 1f, 1f, 0.35f);
            p._hint = hrt; p._hintGrp = hint.gameObject.AddComponent<CanvasGroup>(); p._hintGrp.alpha = 0.22f; p._hintGrp.blocksRaycasts = false;
            // 누른 자리에 뜨는 얇은 링(드래그 중 표시)
            var ring = CoastUiArt.CutePill(zone, "CamRing", new Color(1f, 1f, 1f, 0.08f), (int)(size * 0.5f), 4); ring.raycastTarget = false;
            var rt = ring.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(size * 0.8f, size * 0.8f);
            var rl = CoastHudLayout.MakeText(rt, "A", "◀  ⟲  ▶", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); rl.color = new Color(1f, 1f, 1f, 0.5f);
            p._ring = rt; p._ringGrp = ring.gameObject.AddComponent<CanvasGroup>(); p._ringGrp.alpha = 0f; p._ringGrp.blocksRaycasts = false;
            return p;
        }
        float Scale => Screen.width > 0 ? 720f / Screen.width : 1f;   // 720 기준 픽셀로 정규화(폰 해상도 무관하게 같은 회전량)
        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = e.pointerId; _cam = e.pressEventCamera; _down = _last = e.position; _downAt = Time.unscaledTime; _dragged = false;
            if (_ring != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, _cam, out var lp)) _ring.anchoredPosition = lp;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            var d = e.position - _last; _last = e.position;
            if (!_dragged && (e.position - _down).magnitude * Scale > 18f) _dragged = true;
            if (_dragged) OnOrbit?.Invoke(d.x * Scale);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            bool tap = !_dragged && Time.unscaledTime - _downAt < 0.28f;
            _pointerId = int.MinValue;
            if (tap) OnTap?.Invoke();
        }
        void Update()
        {
            float dt = Time.unscaledDeltaTime; bool held = _pointerId != int.MinValue && _dragged;
            if (_ringGrp != null) _ringGrp.alpha = Mathf.MoveTowards(_ringGrp.alpha, held ? 0.6f : 0f, dt * (held ? 12f : 6f));
            if (_hintGrp != null) _hintGrp.alpha = Mathf.MoveTowards(_hintGrp.alpha, _pointerId != int.MinValue ? 0f : 0.22f, dt * 4f);
            // 손가락을 놓쳤을 때(이벤트 유실) 안전 해제
            if (_pointerId >= 0 && Input.touchCount == 0 && !Input.GetMouseButton(0)) _pointerId = int.MinValue;
            else if (_pointerId == -1 && !Input.GetMouseButton(0)) _pointerId = int.MinValue;
        }
    }
}
