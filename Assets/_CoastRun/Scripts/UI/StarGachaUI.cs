using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 131차: 별빛 캡슐 — 133차(사용자 시안): **슬롯머신** 형태. 금테 3릴에 알록달록 캡슐이 돌다가 등급 색 캡슐 3개로 멈추고,
    ///   아래 결과 패널(등급 배지 · 보상 이름 · 효과 · 획득!)에 [다시 뽑기][확인]. 맨 아래 [1회 뽑기][10+1 뽑기(HOT)] + 보유 별조각.
    ///   결제 없음 — 별조각(러닝)·뽑기권(럭키 캡슐)·하루 1회 무료.
    public class StarGachaUI : MonoBehaviour
    {
        private GameManager _gm;
        private Action _onClose;
        private Canvas _canvas;
        private RectTransform _root, _lever;
        private Text _shardT, _coinT, _haveT, _oneT, _tenT, _hintT;
        private Button _bOne, _bTen, _bAgain, _bOk;
        private RectTransform _resultHost;
        private bool _busy;
        private int _lastCount = 1;

        class Reel { public RectTransform host; public List<Image> caps = new List<Image>(); public List<int> colorIdx = new List<int>(); public float offset; }
        private readonly Reel[] _reels = new Reel[3];
        const float CapSpacing = 150f, ReelH = 440f;
        const int CapCount = 12;

        static readonly Color Night = new Color(0.10f, 0.08f, 0.24f), NightBot = new Color(0.18f, 0.14f, 0.36f);
        static readonly Color Gold = new Color(0.86f, 0.66f, 0.30f), GoldDark = new Color(0.62f, 0.44f, 0.16f), GoldLight = new Color(0.98f, 0.86f, 0.50f);
        static readonly Color Navy = new Color(0.14f, 0.16f, 0.36f), Blue = new Color(0.36f, 0.48f, 0.90f), Cyan = new Color(0.45f, 0.85f, 1f);
        static readonly Color[] CapCols = { new Color(0.98f, 0.55f, 0.70f), new Color(0.45f, 0.82f, 0.72f), new Color(0.98f, 0.82f, 0.30f), new Color(0.40f, 0.72f, 0.98f), new Color(0.72f, 0.50f, 0.98f), new Color(0.98f, 0.62f, 0.38f) };
        // 등급 → 릴에 멈추는 캡슐 색 인덱스: N 연두(1), R 파랑(3), SR 보라(4), SSR 노랑(2)
        static int RarityCap(GachaRarity r) => r == GachaRarity.SSR ? 2 : r == GachaRarity.SR ? 4 : r == GachaRarity.R ? 3 : 1;

        private SaveData Save => _gm != null ? _gm.Save : null;
        private MetaProfile Profile => _gm != null ? _gm.Profile : null;

        public static StarGachaUI Open(GameManager gm, Action onClose)
        {
            if (gm == null || gm.Save == null) return null;
            var go = new GameObject("StarGachaUI");
            var ui = go.AddComponent<StarGachaUI>();
            ui._gm = gm; ui._onClose = onClose;
            ui.Build();
            CoastAudioManager.PlayAnywhere(CoastSfx.MenuOpen, 0.6f);
            return ui;
        }

        static void Rect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax) { rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax; }
        static Text T(Transform parent, string name, string s, int size, Color c, TextAnchor a) { var t = CoastHudLayout.MakeText(parent, name, s, size, a, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); t.color = c; t.fontStyle = FontStyle.Bold; t.raycastTarget = false; return t; }
        static Image Top(RectTransform parent, string name, Color col, int radius, float x0, float y0, float x1, float y1)
        {
            var im = CoastUiArt.Panel(parent, name, col, radius); im.raycastTarget = false;
            Rect(im.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x0, y1), new Vector2(x1, y0)); return im;
        }
        /// 파란 보석(마름모): 회전한 둥근 사각형 + 흰 하이라이트
        static void Gem(RectTransform parent, float x, float y, float s)
        {
            var g = Top(parent, "Gem", new Color(0.40f, 0.55f, 0.98f), 6, x - s, -y - s, x + s, -y + s); g.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            var h = CoastHudLayout.MakeImage(g.transform, "Hi", new Vector2(0.35f, 0.65f), new Vector2(0.35f, 0.65f), new Vector2(-s * 0.25f, -s * 0.2f), new Vector2(s * 0.25f, s * 0.2f), new Color(1f, 1f, 1f, 0.8f)); h.sprite = CoastUiArt.RoundedRect(6); h.type = Image.Type.Sliced; h.raycastTarget = false;
        }

        private void Build()
        {
            _canvas = CoastUiCanvas.Create("StarGachaCanvas", 420, transform);
            _root = CoastUiCanvas.Root(_canvas);
            float pad = CoastUiCanvas.HudPad;
            var starSp = CoastUiArt.Icon("Star"); var coinSp = CoastUiArt.Icon("Coin");

            // 밤하늘 + 보케
            var tex = new Texture2D(1, 3, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { NightBot, new Color(0.13f, 0.10f, 0.30f), Night }); tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.Apply();
            var sky = CoastHudLayout.MakeImage(_root, "Sky", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), Color.white);
            sky.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, 1, 3), new Vector2(0.5f, 0.5f), 1f); sky.raycastTarget = true;
            var rng = new System.Random(23);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)rng.NextDouble(), y = (float)rng.NextDouble(); float r = 14f + (float)rng.NextDouble() * 40f;
                var d = CoastHudLayout.MakeImage(_root, "Bokeh", new Vector2(x, y), new Vector2(x, y), new Vector2(-r, -r), new Vector2(r, r), new Color(0.55f, 0.45f, 0.95f, 0.05f + (float)rng.NextDouble() * 0.08f));
                d.sprite = CoastUiArt.RoundedRect(60); d.type = Image.Type.Sliced; d.raycastTarget = false;
            }
            for (int i = 0; i < 30; i++)
            {
                float x = (float)rng.NextDouble(), y = (float)rng.NextDouble(); float r = 2f + (float)rng.NextDouble() * 4f;
                var d = CoastHudLayout.MakeImage(_root, "Dot", new Vector2(x, y), new Vector2(x, y), new Vector2(-r, -r), new Vector2(r, r), new Color(1f, 0.95f, 0.8f, 0.3f + (float)rng.NextDouble() * 0.5f));
                d.sprite = CoastUiArt.RoundedRect(8); d.type = Image.Type.Sliced; d.raycastTarget = false;
            }

            // 왼쪽 위 재화 알약 두 개(별조각 · 코인)
            Image Pill(string name, float y0, Sprite icon, out Text label)
            {
                var p = Top(_root, name, GoldDark, 18, -330f, y0, -140f, y0 + 50f);
                var pin = CoastUiArt.Panel(p.transform, "In", new Color(0.14f, 0.14f, 0.30f), 15); pin.raycastTarget = false; Rect(pin.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                var ic = CoastHudLayout.MakeImage(p.transform, "I", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -17f), new Vector2(42f, 17f), Color.white);
                if (icon != null) { ic.sprite = icon; ic.preserveAspect = true; } else { ic.sprite = CoastUiArt.RoundedRect(16); ic.type = Image.Type.Sliced; ic.color = GoldLight; }
                ic.raycastTarget = false;
                label = T(p.transform, "T", "", 22, Color.white, TextAnchor.MiddleLeft); Rect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(50f, 0f), new Vector2(-8f, 0f));
                return p;
            }
            Pill("Shards", -34f, starSp, out _shardT);
            Pill("Coins", -92f, coinSp, out _coinT);

            // 제목 리본(금테 + 남색)
            var rib = Top(_root, "Ribbon", Gold, 18, -190f, -36f, 190f, -118f);
            var ribIn = CoastUiArt.Panel(rib.transform, "In", new Color(0.24f, 0.22f, 0.52f), 14); ribIn.raycastTarget = false; Rect(ribIn.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            Gem(_root, -190f, 77f, 16f); Gem(_root, 190f, 77f, 16f);
            var title = T(rib.transform, "T", Loc.T("★ 별빛 캡슐 ★", "★ Star Capsule ★"), 36, new Color(0.85f, 0.80f, 1f), TextAnchor.MiddleCenter);
            CoastUiArt.OutlineText(title, new Color(0.30f, 0.16f, 0.55f), 2f);
            // 닫기
            var close = CoastUiArt.CutePill(_root, "Close", new Color(0.74f, 0.66f, 0.95f), 18, 4);
            Rect(close.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(200f, -104f), new Vector2(330f, -50f)); close.raycastTarget = true;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(Close);
            T(close.transform, "T", Loc.T("닫기 ✕", "Close ✕"), 22, new Color(0.30f, 0.20f, 0.55f), TextAnchor.MiddleCenter);
            var sub = T(_root, "Sub", Loc.T("러닝으로 모은 별조각으로 뽑는 이벤트 뽑기 — 결제 없음", "Event capsule — earned by running, never bought"), 18, new Color(0.80f, 0.72f, 1f), TextAnchor.MiddleCenter);
            Rect(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -166f), new Vector2(0f, -128f));

            // ── 슬롯머신 ──
            var frame = Top(_root, "Frame", GoldDark, 22, -270f, -196f, 270f, -736f);
            var frame2 = Top(_root, "Frame2", Gold, 18, -258f, -208f, 258f, -724f);
            var frameIn = Top(_root, "FrameIn", new Color(0.30f, 0.22f, 0.12f), 14, -240f, -226f, 240f, -706f);
            for (int i = 0; i < 3; i++)
            {
                float x0 = -226f + i * 156f, x1 = x0 + 144f;
                var win = Top(_root, "Win" + i, new Color(0.62f, 0.50f, 0.32f), 12, x0 - 4f, -236f, x1 + 4f, -696f);
                var winIn = Top(_root, "WinIn" + i, new Color(0.96f, 0.93f, 0.85f), 10, x0, -240f, x1, -692f);
                var host = new GameObject("Reel" + i, typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                host.SetParent(_root, false);
                Rect(host, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x0, -692f), new Vector2(x1, -240f));
                var reel = new Reel { host = host };
                for (int k = 0; k < CapCount; k++)
                {
                    int ci = (k * 5 + i * 2) % CapCols.Length;
                    reel.colorIdx.Add(ci);
                    var cap = MakeCapsule(host, CapCols[ci]);
                    reel.caps.Add(cap);
                }
                reel.offset = i * 37f;
                _reels[i] = reel;
                LayoutReel(reel);
                // 창 위아래 그늘(원통 느낌)
                var shTop = Top(_root, "ShT" + i, new Color(0f, 0f, 0f, 0.28f), 10, x0, -240f, x1, -300f);
                var shBot = Top(_root, "ShB" + i, new Color(0f, 0f, 0f, 0.28f), 10, x0, -632f, x1, -692f);
            }
            // 가운데 줄(당첨선)
            var line = Top(_root, "Line", new Color(1f, 0.85f, 0.35f, 0.55f), 2, -236f, -464f, 236f, -468f);
            Gem(_root, -262f, 208f, 22f); Gem(_root, 262f, 208f, 22f); Gem(_root, -262f, 724f, 22f); Gem(_root, 262f, 724f, 22f);
            Gem(_root, -262f, 466f, 12f); Gem(_root, 262f, 466f, 12f);
            // 레버(오른쪽): 받침 + 막대 + 공 — 당길 때 피벗 기준 회전
            var mount = Top(_root, "Mount", GoldDark, 10, 268f, -560f, 300f, -640f);
            _lever = new GameObject("Lever", typeof(RectTransform)).GetComponent<RectTransform>();
            _lever.SetParent(_root, false);
            _lever.anchorMin = _lever.anchorMax = new Vector2(0.5f, 1f); _lever.pivot = new Vector2(0.5f, 0f);
            _lever.anchoredPosition = new Vector2(292f, -600f); _lever.sizeDelta = new Vector2(30f, 190f);
            var rod = CoastUiArt.Panel(_lever, "Rod", Gold, 8); rod.raycastTarget = false; Rect(rod.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-8f, 0f), new Vector2(8f, -30f));
            var ball = CoastUiArt.Panel(_lever, "Ball", GoldLight, 30); ball.raycastTarget = false; Rect(ball.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-30f, -60f), new Vector2(30f, 0f));
            var ballHi = CoastHudLayout.MakeImage(ball.transform, "Hi", new Vector2(0.35f, 0.7f), new Vector2(0.35f, 0.7f), new Vector2(-9f, -7f), new Vector2(9f, 7f), new Color(1f, 1f, 1f, 0.7f)); ballHi.sprite = CoastUiArt.RoundedRect(10); ballHi.type = Image.Type.Sliced; ballHi.raycastTarget = false;
            _lever.localRotation = Quaternion.Euler(0f, 0f, -12f);
            var hit = CoastHudLayout.MakeImage(_lever, "Hit", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-60f, -30f), new Vector2(60f, 60f), new Color(0f, 0f, 0f, 0f)); hit.raycastTarget = true;
            var leverBtn = hit.gameObject.AddComponent<Button>(); leverBtn.transition = Selectable.Transition.None; leverBtn.targetGraphic = hit;
            rod.raycastTarget = true; ball.raycastTarget = true;
            leverBtn.onClick.AddListener(() => DoPull(_lastCount));

            // ── 결과 패널 ──
            var rf = Top(_root, "ResFrame", Blue, 22, -290f, -752f, 290f, -1150f);
            var rin = CoastUiArt.Panel(rf.transform, "In", new Color(0.12f, 0.14f, 0.34f), 18); rin.raycastTarget = false; Rect(rin.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            Gem(_root, -290f, 752f, 12f); Gem(_root, 290f, 752f, 12f); Gem(_root, -290f, 1150f, 12f); Gem(_root, 290f, 1150f, 12f);
            _resultHost = new GameObject("ResHost", typeof(RectTransform)).GetComponent<RectTransform>();
            _resultHost.SetParent(rf.transform, false); Rect(_resultHost, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ShowIdle();

            // ── 아래 버튼 줄 ──
            _bOne = BigBtn("One", Navy, new Color(0.55f, 0.62f, 0.98f), -320f, -1170f, -10f, -1250f, out _oneT, () => DoPull(1));
            _bTen = BigBtn("Ten", new Color(0.95f, 0.58f, 0.18f), new Color(1f, 0.85f, 0.45f), 10f, -1170f, 320f, -1250f, out _tenT, () => DoPull(11));
            var hot = Top(_root, "Hot", new Color(0.95f, 0.22f, 0.22f), 10, 246f, -1160f, 326f, -1194f);
            T(hot.transform, "T", "HOT", 16, Color.white, TextAnchor.MiddleCenter);
            var have = Top(_root, "Have", new Color(0.22f, 0.20f, 0.44f), 20, -300f, -1266f, 300f, -1314f);   // 136차: 글이 길어 400폭을 넘던 것 → 600폭
            _haveT = T(have.transform, "T", "", 20, new Color(0.85f, 0.82f, 1f), TextAnchor.MiddleCenter);
            _haveT.resizeTextForBestFit = true; _haveT.resizeTextMinSize = 11; _haveT.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            _hintT = T(_root, "Hint", Loc.T("N 58%(코인 300~800 50%·재료 30%·휴식 20%) · R 30%(부적 40%·하트 35%·장식 25%) · SR 10%(카드 45%·희귀장식 30%·부적 25%) · SSR 2%(사인카드 40%·축복 35%·잭팟 1만+ 25%) — 10회째 SR+ 확정", "N 58% (coins 300–800 50%·ingredients 30%·rest 20%) · R 30% (charm 40%·hearts 35%·deco 25%) · SR 10% (card 45%·rare deco 30%·lucky 25%) · SSR 2% (signed 40%·blessing 35%·jackpot 10k+ 25%) — SR+ on the 10th"), 14, new Color(0.62f, 0.58f, 0.85f), TextAnchor.MiddleCenter);
            Rect(_hintT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -1400f), new Vector2(-24f, -1318f)); _hintT.horizontalOverflow = HorizontalWrapMode.Wrap; _hintT.verticalOverflow = VerticalWrapMode.Truncate;
            _hintT.resizeTextForBestFit = true; _hintT.resizeTextMinSize = 9; _hintT.resizeTextMaxSize = CoastHudLayout.Scaled(14);   // 136차: 세 줄 힌트가 상자 아래로 새지 않게
            Refresh();
        }

        private Image MakeCapsule(RectTransform host, Color col)
        {
            // 바깥(어두운 테두리) → 안쪽 채움 → 띠/하이라이트. 자식이 부모 위에 그려지므로 테두리를 부모로 둔다.
            var cap = CoastHudLayout.MakeImage(host, "Cap", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-33f, -53f), new Vector2(33f, 53f), new Color(0.32f, 0.20f, 0.14f, 0.85f));
            cap.sprite = CoastUiArt.RoundedRect(33); cap.type = Image.Type.Sliced; cap.raycastTarget = false;
            cap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            var fill = CoastHudLayout.MakeImage(cap.transform, "Fill", Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f), col);
            fill.sprite = CoastUiArt.RoundedRect(30); fill.type = Image.Type.Sliced; fill.raycastTarget = false;
            var shade = CoastHudLayout.MakeImage(fill.transform, "Shade", new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(2f, 2f), new Vector2(-2f, 0f), new Color(0f, 0f, 0f, 0.10f)); shade.sprite = CoastUiArt.RoundedRect(28); shade.type = Image.Type.Sliced; shade.raycastTarget = false;
            var band = CoastHudLayout.MakeImage(fill.transform, "Band", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -2.5f), new Vector2(0f, 2.5f), new Color(1f, 1f, 1f, 0.55f)); band.raycastTarget = false;
            var hi = CoastHudLayout.MakeImage(fill.transform, "Hi", new Vector2(0.35f, 0.78f), new Vector2(0.35f, 0.78f), new Vector2(-9f, -7f), new Vector2(9f, 7f), new Color(1f, 1f, 1f, 0.75f)); hi.sprite = CoastUiArt.RoundedRect(9); hi.type = Image.Type.Sliced; hi.raycastTarget = false;
            return cap;
        }

        private void LayoutReel(Reel r)
        {
            float total = CapCount * CapSpacing;
            for (int k = 0; k < r.caps.Count; k++)
            {
                float y = ((k * CapSpacing - r.offset) % total + total) % total;   // 0..total, 위에서 아래로
                r.caps[k].rectTransform.anchoredPosition = new Vector2(0f, ReelH * 0.5f - y + CapSpacing * 0.5f);
            }
        }

        private Button BigBtn(string name, Color col, Color edge, float x0, float y0, float x1, float y1, out Text label, Action onClick)
        {
            var e = Top(_root, name + "Edge", edge, 22, x0, y0, x1, y1);
            var pill = CoastUiArt.GlossyPill(_root, name, col, 20, 5);
            Rect(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x0 + 4f, y1 + 4f), new Vector2(x1 - 4f, y0 - 4f)); pill.raycastTarget = true;
            foreach (var im in pill.GetComponentsInChildren<Image>(true)) im.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = pill;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick(); });
            var sp = CoastUiArt.Icon("Star");
            if (sp != null) { var ic = CoastHudLayout.MakeImage(pill.transform, "I", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -20f), new Vector2(54f, 20f), new Color(1f, 0.88f, 0.35f)); ic.sprite = sp; ic.preserveAspect = true; ic.raycastTarget = false; }
            label = T(pill.transform, "T", "", 22, Color.white, TextAnchor.MiddleCenter); Rect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(56f, 0f), new Vector2(-10f, 0f));
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = CoastHudLayout.Scaled(22); label.horizontalOverflow = HorizontalWrapMode.Wrap;
            CoastUiArt.OutlineText(label, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return b;
        }

        private void Refresh()
        {
            var s = Save; var p = Profile; if (s == null) return;
            _shardT.text = s.starShards.ToString("N0");
            _coinT.text = s.stats.money.ToString("N0");
            bool free = StarGacha.FreePullReady(p);
            string oneCost = free ? Loc.T("오늘 무료", "free today") : s.capsuleTickets > 0 ? Loc.T($"뽑기권 {s.capsuleTickets}장", $"ticket ×{s.capsuleTickets}") : Loc.T($"별조각 {StarGacha.PullCost}", $"★{StarGacha.PullCost}");
            _oneT.text = Loc.T($"1회 뽑기 ({oneCost})", $"Pull ×1 ({oneCost})");
            _tenT.text = Loc.T($"10+1 뽑기\n(별조각 {StarGacha.TenCost})", $"Pull 10+1\n(★{StarGacha.TenCost})");
            int left = Mathf.Max(1, StarGacha.PityAt - s.gachaPity);
            _haveT.text = Loc.T($"보유 별조각: {s.starShards:N0}  ·  뽑기권 {s.capsuleTickets}  ·  SR 확정까지 {left}회", $"Shards: {s.starShards:N0} · tickets {s.capsuleTickets} · SR+ in {left}");
            SetEnabled(_bOne, StarGacha.CanPull(s, p, 1, out _));
            SetEnabled(_bTen, s.starShards >= StarGacha.TenCost);
        }
        static void SetEnabled(Button b, bool on)
        {
            b.interactable = on;
            foreach (var im in b.GetComponentsInChildren<Image>(true)) { var c = im.color; c.a = on ? 1f : 0.45f; im.color = c; }
        }

        // ── 결과 패널 내용 ──
        private void ClearResult() { for (int i = _resultHost.childCount - 1; i >= 0; i--) Destroy(_resultHost.GetChild(i).gameObject); }
        private void ShowIdle()
        {
            ClearResult();
            var t = T(_resultHost, "Idle", Loc.T("레버를 당기거나 아래 버튼으로\n캡슐을 뽑아 봐!", "Pull the lever or tap a button\nto draw a capsule!"), 26, new Color(0.80f, 0.78f, 1f), TextAnchor.MiddleCenter);
            Rect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        }
        private void GradeBadge(Transform parent, GachaRarity r, float y)
        {
            var col = StarGacha.RarityColor(r);
            var b = CoastUiArt.Panel(parent, "Badge", col, 16); b.raycastTarget = false;
            Rect(b.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, y - 52f), new Vector2(110f, y));
            var bin = CoastUiArt.Panel(b.transform, "In", new Color(0.12f, 0.16f, 0.40f), 13); bin.raycastTarget = false; Rect(bin.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var t = T(b.transform, "T", Loc.T($"◆ {StarGacha.RarityName(r)} 등급", $"◆ {StarGacha.RarityName(r)} grade"), 26, col, TextAnchor.MiddleCenter);
            var tag = CoastUiArt.Panel(parent, "Tag", col, 8); tag.raycastTarget = false; Rect(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(118f, y - 46f), new Vector2(160f, y - 6f));
            T(tag.transform, "T", StarGacha.RarityName(r), 18, new Color(0.10f, 0.12f, 0.30f), TextAnchor.MiddleCenter);
        }
        private void ShowReward(GachaReward rw)
        {
            ClearResult();
            GradeBadge(_resultHost, rw.rarity, -14f);
            var nm = T(_resultHost, "Name", rw.title, 44, new Color(0.88f, 0.85f, 1f), TextAnchor.MiddleCenter);
            Rect(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -170f), new Vector2(-16f, -80f)); nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 20; nm.resizeTextMaxSize = CoastHudLayout.Scaled(44); nm.horizontalOverflow = HorizontalWrapMode.Wrap;
            CoastUiArt.OutlineText(nm, new Color(0.30f, 0.20f, 0.60f, 0.8f), 2f);
            var dt = T(_resultHost, "Detail", rw.detail + (rw.isNew ? "  · NEW!" : ""), 26, Cyan, TextAnchor.MiddleCenter);
            Rect(dt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -226f), new Vector2(-16f, -176f)); dt.resizeTextForBestFit = true; dt.resizeTextMinSize = 14; dt.resizeTextMaxSize = CoastHudLayout.Scaled(26); dt.horizontalOverflow = HorizontalWrapMode.Wrap;
            var got = T(_resultHost, "Got", Loc.T("★ 획득! ★", "★ Got it! ★"), 24, new Color(1f, 0.85f, 0.40f), TextAnchor.MiddleCenter);
            Rect(got.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -270f), new Vector2(0f, -232f));
            ResultButtons();
        }
        private void ShowSummary(List<GachaReward> rewards)
        {
            ClearResult();
            GachaRarity best = GachaRarity.N; foreach (var r in rewards) if (r.rarity > best) best = r.rarity;
            GradeBadge(_resultHost, best, -14f);
            for (int i = 0; i < rewards.Count && i < 12; i++)
            {
                var rw = rewards[i]; int col = i % 3, row = i / 3;
                var chip = CoastUiArt.Panel(_resultHost, "C" + i, rw.Color, 10); chip.raycastTarget = false;
                Rect(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f + col * 186f, -78f - row * 50f - 44f), new Vector2(16f + col * 186f + 178f, -78f - row * 50f));
                var cin = CoastUiArt.Panel(chip.transform, "In", new Color(0.10f, 0.12f, 0.30f), 8); cin.raycastTarget = false; Rect(cin.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
                var t = T(chip.transform, "T", $"{StarGacha.RarityName(rw.rarity)} · {rw.title}" + (rw.isNew ? " NEW" : ""), 14, new Color(0.92f, 0.90f, 1f), TextAnchor.MiddleCenter);
                Rect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f)); t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = CoastHudLayout.Scaled(14); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            }
            ResultButtons();
        }
        private void ResultButtons()
        {
            Button Mk(string name, string label, Color col, float x0, float x1)
            {
                var edge = CoastUiArt.Panel(_resultHost, name + "E", new Color(0.55f, 0.65f, 1f), 16); edge.raycastTarget = false;
                Rect(edge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x0, 22f), new Vector2(x1, 92f));
                var pill = CoastUiArt.Panel(_resultHost, name, col, 13); pill.raycastTarget = true;
                Rect(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x0 + 4f, 26f), new Vector2(x1 - 4f, 88f));
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = pill;
                var t = T(pill.transform, "T", label, 26, Color.white, TextAnchor.MiddleCenter); CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                return b;
            }
            _bAgain = Mk("Again", Loc.T("다시 뽑기", "Pull again"), new Color(0.18f, 0.20f, 0.46f), -262f, -12f);
            _bAgain.onClick.AddListener(() => { CoastPrefs.Vibrate(); DoPull(_lastCount); });
            _bOk = Mk("Ok", Loc.T("확인", "OK"), new Color(0.30f, 0.55f, 0.95f), 12f, 262f);
            _bOk.onClick.AddListener(() => { CoastPrefs.Vibrate(); ShowIdle(); });
        }

        // ── 뽑기 ──
        private void DoPull(int count)
        {
            if (_busy || Save == null) return;
            int payCount = count >= 10 ? 10 : 1;
            if (!StarGacha.CanPull(Save, Profile, payCount, out _)) { CoastToast.Show(Loc.T("별조각이 모자라 — 달리기로 모아 오자!", "Not enough shards — go run!")); return; }
            _lastCount = count;
            var rewards = StarGacha.Pull(_gm, payCount, count >= 10 ? 11 : 1);   // 10+1
            if (rewards.Count == 0) return;
            _gm.Persist(); _gm.WriteProfileNow();
            Refresh();
            StartCoroutine(SpinSeq(rewards));
        }

        private IEnumerator SpinSeq(List<GachaReward> rewards)
        {
            _busy = true;
            SetEnabled(_bOne, false); SetEnabled(_bTen, false);
            ClearResult();
            var spinning = T(_resultHost, "Spin", Loc.T("두구두구…", "Rolling…"), 30, new Color(0.80f, 0.78f, 1f), TextAnchor.MiddleCenter); Rect(spinning.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // 레버 당김
            float t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.35f); _lever.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 62f, Mathf.Sin(u * Mathf.PI * 0.5f))); yield return null; }
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
            GachaRarity best = GachaRarity.N; foreach (var r in rewards) if (r.rarity > best) best = r.rarity;
            int target = RarityCap(rewards.Count == 1 ? rewards[0].rarity : best);
            float[] stopAt = { 1.2f, 1.7f, 2.2f };
            float[] vel = { 1900f, 2100f, 2000f };
            bool[] stopped = { false, false, false };
            float[] snapFrom = new float[3], snapTo = new float[3], snapT = new float[3];
            float el = 0f;
            bool all = false;
            while (!all)
            {
                float dt = Time.unscaledDeltaTime; el += dt;
                // 레버 복귀
                _lever.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(_lever.localRotation.eulerAngles.z > 180f ? _lever.localRotation.eulerAngles.z - 360f : _lever.localRotation.eulerAngles.z, -12f, dt * 4f));
                all = true;
                for (int i = 0; i < 3; i++)
                {
                    var r = _reels[i];
                    if (stopped[i]) continue;
                    all = false;
                    if (el < stopAt[i]) { r.offset += vel[i] * dt; }
                    else if (snapT[i] <= 0f)
                    {
                        // 앞으로 가장 가까운 목표색 캡슐이 가운데(창 중앙)에 오는 offset 을 잡아 0.45 s 동안 감속 스냅
                        float total = CapCount * CapSpacing;
                        float center = ReelH * 0.5f + CapSpacing * 0.5f;   // LayoutReel: pos = ReelH/2 - y + Spacing/2 → pos 0(창 중앙)은 y == center
                        float bestD = float.MaxValue, bestOff = r.offset;
                        for (int k = 0; k < CapCount; k++)
                        {
                            if (r.colorIdx[k] != target) continue;
                            float want = k * CapSpacing - center;             // offset 이 이 값(+n·total)이면 k 가 가운데
                            float d = ((want - r.offset) % total + total) % total;
                            if (d < CapSpacing * 2f) d += total;   // 최소 두 칸은 더 돌고 멈춤(한 바퀴 추가)
                            if (d < bestD) { bestD = d; bestOff = r.offset + d; }
                        }
                        snapFrom[i] = r.offset; snapTo[i] = bestOff; snapT[i] = 0.0001f;
                    }
                    else
                    {
                        snapT[i] += dt; float u = Mathf.Clamp01(snapT[i] / 0.45f);
                        float e = 1f - (1f - u) * (1f - u) * (1f - u);
                        r.offset = Mathf.Lerp(snapFrom[i], snapTo[i], e) + (u > 0.9f ? Mathf.Sin((u - 0.9f) * 60f) * 6f * (1f - u) : 0f);
                        if (u >= 1f) { r.offset = snapTo[i]; stopped[i] = true; CoastAudioManager.PlayAnywhere(CoastSfx.Land, 0.35f); }
                    }
                    LayoutReel(r);
                }
                yield return null;
            }
            _lever.localRotation = Quaternion.Euler(0f, 0f, -12f);
            // 당첨 번쩍
            var rarity = rewards.Count == 1 ? rewards[0].rarity : best;
            var col = StarGacha.RarityColor(rarity);
            var flash = CoastHudLayout.MakeImage(_root, "Flash", Vector2.zero, Vector2.one, new Vector2(-500f, -500f), new Vector2(500f, 500f), new Color(col.r, col.g, col.b, 0f)); flash.raycastTarget = false;
            CoastAudioManager.PlayAnywhere(rarity >= GachaRarity.SR ? CoastSfx.RankS : CoastSfx.CardReveal, rarity >= GachaRarity.SR ? 0.8f : 0.5f);
            if (rarity >= GachaRarity.SR) CoastPrefs.VibrateEvent();
            t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; float u = t / 0.5f; flash.color = new Color(col.r, col.g, col.b, (1f - u) * (rarity >= GachaRarity.SR ? 0.5f : 0.22f)); yield return null; }
            Destroy(flash.gameObject);
            if (rewards.Count == 1) ShowReward(rewards[0]); else ShowSummary(rewards);
            _busy = false;
            Refresh();
        }

        private void Close()
        {
            if (_busy) return;
            CoastPrefs.Vibrate();
            var cb = _onClose; _onClose = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
