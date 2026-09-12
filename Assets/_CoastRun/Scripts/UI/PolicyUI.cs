using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 50차(사용자): 타이틀 더보기 › 「이용약관·정책」 — 이용약관(AI 기반 K-POP 음악·사이버 가수 우히&히시 조항 포함) /
    /// 개인정보 처리지침 / 운영정책 / 청소년 보호정책 4개 문서를 탭으로 보는 오버레이. 본문은 PolicyUI.Texts.cs.
    public static partial class PolicyUI
    {
        public enum Doc { Terms = 0, Privacy = 1, Ops = 2, Youth = 3 }

        private static Canvas _canvas;
        private static RectTransform _root, _content;
        private static ScrollRect _scroll;
        private static Text _body, _title;
        private static readonly Button[] _tabs = new Button[4];
        private static Action _onClose;
        private static Doc _doc;

        public static bool IsOpen => _canvas != null;

        private static readonly Color Ink = new Color(0.16f, 0.12f, 0.10f);
        private static readonly Color Paper = new Color(0.99f, 0.97f, 0.92f, 1f);
        private static readonly Color TabOn = new Color(0.93f, 0.22f, 0.52f);
        private static readonly Color TabOff = new Color(0.30f, 0.26f, 0.40f);

        public static string Title(Doc d) => d switch
        {
            Doc.Privacy => Loc.T("개인정보 처리지침", "Privacy Policy"),
            Doc.Ops => Loc.T("운영정책", "Operating Policy"),
            Doc.Youth => Loc.T("청소년 보호", "Youth Protection"),
            _ => Loc.T("이용약관", "Terms of Service"),
        };

        public static string Body(Doc d) => d switch
        {
            Doc.Privacy => Loc.IsKo ? PrivacyKo : PrivacyEn,
            Doc.Ops => Loc.IsKo ? OpsKo : OpsEn,
            Doc.Youth => Loc.IsKo ? YouthKo : YouthEn,
            _ => Loc.IsKo ? TermsKo : TermsEn,
        };

        public static void Open(Doc doc = Doc.Terms, Action onClose = null)
        {
            Close();
            _onClose = onClose;
            _canvas = CoastUiCanvas.Create("PolicyCanvas", 465);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;

            // 배경: 남보라 딤(컬렉션과 같은 계열) + 종이 카드
            var bg = CoastHudLayout.MakeImage(_root, "Bg", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.11f, 0.07f, 0.26f, 1f));
            bg.raycastTarget = true;
            var glow = CoastHudLayout.MakeImage(_root, "Glow", new Vector2(0f, 0.6f), new Vector2(1f, 1f), new Vector2(-pad - 400f, 0f), new Vector2(pad + 400f, pad + 400f), new Color(0.30f, 0.16f, 0.52f, 0.5f));
            glow.raycastTarget = false;

            // 머리: 제목 + 닫기
            _title = CoastHudLayout.MakeText(_root, "Title", Loc.T("이용약관·정책", "Terms & Policies"), 30, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -74f), new Vector2(-150f, -18f));
            _title.color = new Color(1f, 0.96f, 0.86f); CoastUiArt.OutlineText(_title, new Color(0.15f, 0.08f, 0.25f, 0.9f), 2f);
            CoastOrnate.GlassButton(_root, "Close", Loc.T("✕ 닫기", "✕ Close"), new Vector2(1f, 1f), new Vector2(-76f, -46f), new Vector2(124f, 48f), Close, 0.5f, 18, false);   // 56차-2: 피벗이 가운데라 반이 화면 밖으로 나가던 것 안쪽으로

            // 탭 4개(2줄 × 2)
            string[] labels = { Title(Doc.Terms), Title(Doc.Privacy), Title(Doc.Ops), Title(Doc.Youth) };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var pill = CoastUiArt.CutePill(_root, "Tab" + i, TabOff, 16, 3);
                var rt = pill.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(i % 2 == 0 ? 0.26f : 0.74f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -92f - (i / 2) * 58f); rt.sizeDelta = new Vector2(318f, 50f); pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); Show((Doc)idx); });
                var t = CoastHudLayout.MakeText(rt, "T", labels[i], 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), new Vector2(0f, 1f));
                t.color = Color.white; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                _tabs[i] = b;
            }

            // 종이 카드 + 스크롤 본문
            var paper = CoastUiArt.CutePill(_root, "Paper", Paper, 22, 4);
            var prt = paper.rectTransform; prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = new Vector2(14f, 18f); prt.offsetMax = new Vector2(-14f, -216f); paper.raycastTarget = true;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            viewport.SetParent(paper.transform, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(14f, 14f); viewport.offsetMax = new Vector2(-14f, -14f);
            var vimg = viewport.GetComponent<Image>(); vimg.color = new Color(0f, 0f, 0f, 0f); vimg.raycastTarget = true;
            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = new Vector2(1f, 1f); _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = new Vector2(0f, 0f); _content.offsetMax = new Vector2(0f, 0f);
            _body = CoastHudLayout.MakeText(_content, "Body", "", 15, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 12f), new Vector2(-12f, -12f));
            _body.color = Ink; _body.horizontalOverflow = HorizontalWrapMode.Wrap; _body.verticalOverflow = VerticalWrapMode.Overflow; _body.lineSpacing = 1.25f;
            _scroll = paper.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport; _scroll.content = _content; _scroll.horizontal = false; _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 40f; _scroll.inertia = true; _scroll.decelerationRate = 0.12f;
            // 스크롤바(가는 막대)
            var sbGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(paper.transform, false);
            var sbRt = sbGo.GetComponent<RectTransform>(); sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f); sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.offsetMin = new Vector2(-10f, 16f); sbRt.offsetMax = new Vector2(-6f, -16f);
            sbGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.06f);
            var sb = sbGo.GetComponent<Scrollbar>(); sb.direction = Scrollbar.Direction.BottomToTop;
            var handle = CoastUiArt.Panel(sbRt, "Handle", new Color(0.93f, 0.22f, 0.52f, 0.7f), 3); handle.raycastTarget = false;
            var hrt = handle.rectTransform; hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one; hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            sb.handleRect = hrt; sb.targetGraphic = handle;
            _scroll.verticalScrollbar = sb; _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            // 아래 안내(시행일)
            var foot = CoastHudLayout.MakeText(_root, "Foot", Loc.T($"시행일 {Effective} · 문의 {Contact}", $"Effective {Effective} · {Contact}"), 12, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 16f));
            foot.color = new Color(1f, 1f, 1f, 0.6f);

            Show(doc);
        }

        private static void Show(Doc d)
        {
            _doc = d;
            _body.text = Body(d);
            for (int i = 0; i < 4; i++)
            {
                var fill = _tabs[i].transform.Find("Fill")?.GetComponent<Image>();
                if (fill != null) fill.color = i == (int)d ? TabOn : TabOff;
                var lip = _tabs[i].transform.Find("Lip")?.GetComponent<Image>();
                if (lip != null) lip.color = Color.Lerp(i == (int)d ? TabOn : TabOff, Color.black, 0.45f);
            }
            _content.sizeDelta = new Vector2(0f, Mathf.Max(200f, _body.preferredHeight + 24f));
            _body.rectTransform.offsetMin = new Vector2(12f, 12f); _body.rectTransform.offsetMax = new Vector2(-12f, -12f);
            _scroll.verticalNormalizedPosition = 1f;
            if (_body.GetComponent<PolicyFit>() == null) _body.gameObject.AddComponent<PolicyFit>().Bind(_body, _content);
        }

        /// 폰트 로드·해상도 변경으로 본문 높이가 바뀌면 콘텐츠 높이를 따라가게 (한 번 붙여 두면 됨).
        private class PolicyFit : MonoBehaviour
        {
            private Text _t; private RectTransform _c; private float _last;
            public void Bind(Text t, RectTransform c) { _t = t; _c = c; }
            private void LateUpdate()
            {
                if (_t == null || _c == null) return;
                float h = _t.preferredHeight + 24f;
                if (Mathf.Abs(h - _last) > 1f) { _last = h; _c.sizeDelta = new Vector2(0f, Mathf.Max(200f, h)); }
            }
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null; _content = null; _scroll = null; _body = null;
            var cb = _onClose; _onClose = null; cb?.Invoke();
        }
    }
}
