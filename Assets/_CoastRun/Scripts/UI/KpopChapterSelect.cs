using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 39차-4: 타이틀 CHAPTER 칩 → K-POP 러닝모드 챕터 선택 페이지 (CH 1~20).
    /// 선택 안 하면 마지막으로 클리어한 K-POP 챕터의 다음 챕터가 자동으로 잡힌다(ArcadeRun.KpopDefaultChapter).
    /// 71차(사용자 시안): 유채밭 배경(UI_Chapter_Bg2) · 주황 「CHAPTER」(UI_ChapterTitle2) · 부제/안내 · 4열×5행 카드
    ///   (완료 = 파스텔 5색 + 리본 메달(UI_Rosette 틴트 + 등급 글자) + 초록 「✓ COMPLETED」 알약,
    ///    다음 챕터 = 분홍 + 흰 원 재생 배지(UI_PlayBadge) + 「READY!」, 잠김 = 회색 + 자물쇠 + 「LOCKED」)
    ///   · 아래 분홍 큰 「챕터 n 도전하기!」 · 바닥 「K-POP 러닝 · JEJU」. 720×1280 디자인 단위, 런타임 빌드.
    public static class KpopChapterSelect
    {
        private static Canvas _canvas;
        private static Image[] _cards = new Image[Timeline.Chapters];
        private static Image[] _cardFills = new Image[Timeline.Chapters];
        private static bool[] _unlocked = new bool[Timeline.Chapters];
        private static int[] _gradeOf = new int[Timeline.Chapters];
        private static Text _bigLabel, _hint;
        private static int _picked;
        private static Action<int> _onPick;
        private static Action<int> _onPlay;

        // 시안 파스텔 5색(노랑·하늘·분홍·연두·보라) — 완료 카드가 순서대로 돌아가며 쓴다
        private static readonly Color[] Pastel =
        {
            new Color(1f, 0.88f, 0.40f), new Color(0.56f, 0.83f, 1f), new Color(1f, 0.70f, 0.78f), new Color(0.72f, 0.94f, 0.69f), new Color(0.79f, 0.71f, 0.96f),
        };
        private static readonly Color Ready = new Color(1f, 0.55f, 0.74f);
        private static readonly Color Locked = new Color(0.62f, 0.62f, 0.65f);
        private static readonly Color Navy = new Color(0.16f, 0.20f, 0.40f);

        /// onPick: 카드를 골랐을 때(닫기 포함) 현재 선택 챕터. onPlay: 「CH n 달리기」.
        /// 47차: 타이틀 UI(제목 글자·K-POP Play 바)가 딤 뒤로 비쳐 시안과 달랐다 → 열려 있는 동안 숨긴다(MainMenuController 가 넣어 줌).
        public static CanvasGroup TitleUi;
        /// 71차 개발용: −1 이면 실제 진행, 0 이상이면 「마지막 클리어 = 이 값」으로 꾸며서 READY/LOCKED 모양을 본다(Coast Run/Dev/Chapter select - preview).
        public static int DebugLastClear = -1;

        public static void Open(GameManager gm, Action<int> onPick, Action<int> onPlay)
        {
            Close();
            if (TitleUi != null) TitleUi.alpha = 0f;
            _onPick = onPick; _onPlay = onPlay;
            _picked = ArcadeRun.KpopChapter(gm);
            if (!IsUnlocked(_picked, gm != null ? gm.Profile : null)) _picked = Mathf.Clamp(ArcadeRun.KpopLastClear + 1, 1, Timeline.Chapters);

            _canvas = CoastUiCanvas.Create("KpopChapterSelect", 320);
            var root = CoastUiCanvas.Root(_canvas);

            // 배경: 유채밭(화면 전체를 덮게 캔버스 루트에 Envelope). 없으면 어두운 딤.
            var bgSpr = CoastUiArt.Art("UI_Chapter_Bg2");
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(_canvas.transform, false); bg.transform.SetAsFirstSibling();
            bg.raycastTarget = true;   // 뒤 타이틀 버튼이 눌리지 않게
            var brt = bg.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            if (bgSpr != null)
            {
                bg.sprite = bgSpr;
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = 720f / 1280f;
            }
            else bg.color = new Color(0.03f, 0.05f, 0.14f, 0.85f);

            // 제목 「CHAPTER」(주황 입체 그림) + 부제 + 안내
            var titleSpr = CoastUiArt.Art("UI_ChapterTitle2") ?? CoastUiArt.Art("UI_ChapterTitle");
            if (titleSpr != null)
            {
                var ti = new GameObject("Title", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ti.transform.SetParent(root, false); ti.sprite = titleSpr; ti.preserveAspect = true; ti.raycastTarget = false;
                var trt = ti.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f);
                trt.anchoredPosition = new Vector2(0f, -22f); trt.sizeDelta = new Vector2(420f, 105f);
            }
            var sub = CoastHudLayout.MakeText(root, "Sub", Loc.T("너와 나의 주파수  COAST RUN · JEJU", "You & My Frequency  COAST RUN · JEJU"), 14, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -158f), new Vector2(0f, -128f));
            sub.color = Navy; sub.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(sub, new Color(1f, 1f, 1f, 0.85f), 1.5f);
            _hint = CoastHudLayout.MakeText(root, "Hint", "", 11, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -204f), new Vector2(-18f, -160f));
            _hint.color = Navy; _hint.horizontalOverflow = HorizontalWrapMode.Wrap; _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 10; _hint.resizeTextMaxSize = CoastHudLayout.Scaled(11);
            CoastUiArt.OutlineText(_hint, new Color(1f, 1f, 1f, 0.8f), 1.2f);

            // 우상단 X
            var xSpr = CoastUiArt.Art("UI_CloseX");
            var xgo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            xgo.transform.SetParent(root, false);
            var xi = xgo.GetComponent<Image>(); xi.raycastTarget = true;
            if (xSpr != null) { xi.sprite = xSpr; xi.preserveAspect = true; } else xi.color = new Color(0.25f, 0.45f, 0.85f);
            var xrt = xgo.GetComponent<RectTransform>(); xrt.anchorMin = xrt.anchorMax = new Vector2(1f, 1f); xrt.pivot = new Vector2(0.5f, 0.5f);
            xrt.anchoredPosition = new Vector2(-40f, -44f); xrt.sizeDelta = new Vector2(64f, 64f);
            var xb = xgo.GetComponent<Button>(); xb.transition = Selectable.Transition.None;
            xb.onClick.AddListener(() => { int c = _picked; var cb = _onPick; Close(); cb?.Invoke(c); });

            // 카드 4×5 — 인셋 폭에 맞춰 카드 폭 계산(좁은 폰도 4열이 다 들어온다)
            const float gap = 12f, margin = 22f;
            float rootW = root.rect.width > 100f ? root.rect.width : 664f;
            float cw = (rootW - 2f * margin - 3f * gap) / 4f, ch = cw * 0.98f;
            float x0 = margin, y0 = 214f, pitch = ch + 12f;
            var prof = gm != null ? gm.Profile : null;
            var rosette = CoastUiArt.Art("UI_Rosette");
            var playBadge = CoastUiArt.Art("UI_PlayBadge");
            var lockSpr = CoastUiArt.Art("UI_Lock_Q");
            var check = CoastUiArt.Art("UI_Check_W");
            for (int i = 0; i < Timeline.Chapters; i++)
            {
                int n = i + 1;
                int col = i % 4, row = i / 4;
                bool open = IsUnlocked(n, prof);
                _unlocked[i] = open;
                int g = prof != null && prof.trackGrade != null && i < prof.trackGrade.Length ? prof.trackGrade[i] : 0;
                if (DebugLastClear >= 0) g = n <= DebugLastClear ? 4 : 0;
                _gradeOf[i] = g;
                bool completed = open && g > 0;
                bool current = open && !completed;
                var fill = completed ? Pastel[i % 5] : current ? Ready : Locked;

                // 카드 = 진한 테두리 판 + 안쪽 채움
                var card = CoastUiArt.Panel(root, "Card" + n, Color.Lerp(fill, Color.black, 0.30f), 22);
                var crt = card.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(x0 + col * (cw + gap) + cw * 0.5f, -(y0 + row * pitch + ch * 0.5f));
                crt.sizeDelta = new Vector2(cw, ch);
                card.raycastTarget = true;
                _cards[i] = card;
                var inner = CoastUiArt.Panel(crt, "Fill", fill, 19);
                var irt0 = inner.rectTransform; irt0.anchorMin = Vector2.zero; irt0.anchorMax = Vector2.one; irt0.offsetMin = new Vector2(3f, 3f); irt0.offsetMax = new Vector2(-3f, -3f);
                _cardFills[i] = inner;
                if (!open) { var shade = CoastUiArt.Panel(crt, "Shade", new Color(0f, 0f, 0f, 0.10f), 19); var srt = shade.rectTransform; srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = new Vector2(3f, 3f); srt.offsetMax = new Vector2(-3f, -3f); }

                var num = CoastHudLayout.MakeText(crt, "N", Loc.T($"챕터 {n}", $"Ch. {n}"), 15, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, -8f));
                num.color = open ? Navy : new Color(0.22f, 0.22f, 0.26f); num.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(num, new Color(1f, 1f, 1f, open ? 0.7f : 0.35f), 1.2f);

                // 가운데 그림: 리본 메달(완료) / 재생 배지(다음) / 자물쇠(잠김)
                var im = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(crt, false); im.preserveAspect = true; im.raycastTarget = false;
                var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.pivot = new Vector2(0.5f, 0.5f);
                if (completed)
                {
                    im.sprite = rosette; im.color = Deep(fill);   // 카드보다 진하고 선명한 같은 색 리본
                    irt.anchoredPosition = new Vector2(0f, -4f); irt.sizeDelta = new Vector2(cw * 0.46f, cw * 0.54f);
                    var grade = CoastHudLayout.MakeText(irt, "G", g >= 4 ? "S" : g == 3 ? "A" : g == 2 ? "B" : "C", 20, TextAnchor.MiddleCenter,
                        new Vector2(0f, 0.36f), new Vector2(1f, 0.96f), Vector2.zero, Vector2.zero);
                    grade.color = Color.white; grade.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(grade, Color.Lerp(fill, Color.black, 0.55f), 1.6f);
                    if (rosette == null) im.enabled = false;
                }
                else if (current)
                {
                    im.sprite = playBadge; im.color = Color.white;
                    irt.anchoredPosition = new Vector2(0f, -6f); irt.sizeDelta = new Vector2(cw * 0.40f, cw * 0.40f);
                    if (playBadge == null) im.enabled = false;
                }
                else
                {
                    im.sprite = lockSpr; im.color = new Color(0.36f, 0.36f, 0.40f);
                    irt.anchoredPosition = new Vector2(0f, -6f); irt.sizeDelta = new Vector2(cw * 0.30f, cw * 0.30f);
                    if (lockSpr == null) im.enabled = false;
                }

                // 카드 안 아래 상태 알약
                var pillCol = completed ? new Color(0.30f, 0.72f, 0.36f) : current ? Color.Lerp(Ready, Color.black, 0.25f) : new Color(0.32f, 0.32f, 0.36f);
                var pill = CoastUiArt.Panel(crt, "Pill", pillCol, 12);
                var prt0 = pill.rectTransform; prt0.anchorMin = new Vector2(0.5f, 0f); prt0.anchorMax = new Vector2(0.5f, 0f); prt0.pivot = new Vector2(0.5f, 0f);
                prt0.anchoredPosition = new Vector2(0f, 8f); prt0.sizeDelta = new Vector2(cw - 24f, 24f);
                string stateTxt = completed ? "COMPLETED" : current ? "READY!" : "LOCKED";
                float textL = 4f;
                if (completed && check != null)
                {
                    var ck = new GameObject("Ck", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    ck.transform.SetParent(prt0, false); ck.sprite = check; ck.preserveAspect = true; ck.raycastTarget = false;
                    var ckr = ck.rectTransform; ckr.anchorMin = ckr.anchorMax = new Vector2(0f, 0.5f); ckr.pivot = new Vector2(0f, 0.5f);
                    ckr.anchoredPosition = new Vector2(8f, 0f); ckr.sizeDelta = new Vector2(13f, 13f);
                    textL = 20f;
                }
                var st = CoastHudLayout.MakeText(prt0, "St", stateTxt, 9, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(textL, 0f), new Vector2(-4f, 0f));
                st.resizeTextForBestFit = true; st.resizeTextMinSize = 7; st.resizeTextMaxSize = CoastHudLayout.Scaled(9);
                st.color = Color.white; st.fontStyle = FontStyle.Bold;

                int pick = n;
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Select(pick));
            }

            // 아래: 큰 「챕터 n 도전하기!」
            var play = CoastUiArt.GlossyPill(root, "Play", new Color(0.98f, 0.32f, 0.56f), 40, 12);
            var prt = play.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, 118f); prt.sizeDelta = new Vector2(Mathf.Min(500f, rootW - 40f), 88f); play.raycastTarget = true;
            _bigLabel = CoastHudLayout.MakeText(prt, "T", "", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 8f), new Vector2(0f, 4f));
            _bigLabel.color = Color.white; _bigLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_bigLabel, new Color(0.45f, 0.05f, 0.20f, 0.6f), 1.5f);
            var pb = play.gameObject.AddComponent<Button>(); pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(() => { int c = _picked; var cb = _onPlay; Close(); cb?.Invoke(c); });

            var foot = CoastHudLayout.MakeText(root, "Foot", "K-POP 러닝  ·  JEJU", 11, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 34f), new Vector2(0f, 62f));
            foot.color = Navy; foot.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(foot, new Color(1f, 1f, 1f, 0.8f), 1.2f);

            Refresh();
        }

        private static Color Deep(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(Mathf.Max(s * 1.6f, 0.62f)), Mathf.Clamp01(v * 0.92f));
        }

        /// 해금: K-POP 마지막 클리어 + 1 까지, 또는 스토리에서 이미 달린 트랙(trackGrade > 0).
        public static bool IsUnlocked(int n, MetaProfile prof)
        {
            if (DebugLastClear >= 0) return n <= DebugLastClear + 1;
            if (n <= ArcadeRun.KpopLastClear + 1) return true;
            return prof != null && prof.trackGrade != null && n - 1 < prof.trackGrade.Length && prof.trackGrade[n - 1] > 0;
        }

        private static void Select(int n)
        {
            if (n >= 1 && n <= _unlocked.Length && !_unlocked[n - 1])
            {
                CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss);
                CoastToast.Show(Loc.T($"챕터 {n}은 아직 잠겨 있어요 — 앞 챕터를 먼저 달려요", $"Chapter {n} is locked — clear the one before it"));
                return;
            }
            _picked = n;
            ArcadeRun.SetKpopPick(n);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            Refresh();
        }

        private static void Refresh()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var c = _cards[i]; if (c == null) continue;
                bool on = i + 1 == _picked;
                var fill = _unlocked[i] ? (_gradeOf[i] > 0 ? Pastel[i % 5] : Ready) : Locked;
                c.color = on ? new Color(1f, 0.93f, 0.35f) : Color.Lerp(fill, Color.black, 0.30f);   // 선택 = 노란 테두리(시안의 READY 글로우)
                c.rectTransform.localScale = Vector3.one * (on ? 1.06f : 1f);
                if (on) c.transform.SetAsLastSibling();   // 커진 카드가 옆 카드 위로
            }
            if (_bigLabel != null) { _bigLabel.text = Loc.T($"챕터 {_picked} 도전하기!", $"Challenge Chapter {_picked}!"); _bigLabel.transform.parent.SetAsLastSibling(); }
            if (_hint != null) _hint.text = Loc.T($"달릴 준비됐어요? 스케이트보드 타고 주파수 맞춰서 즐기는 K-POP 런, 챕터 {_picked}부터 시작해볼까요!",
                $"Ready to run? Hop on the board and ride the frequency — start from chapter {_picked}!");
        }

        public static void Close()
        {
            if (TitleUi != null && _canvas != null) TitleUi.alpha = 1f;
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
            for (int i = 0; i < _cards.Length; i++) { _cards[i] = null; _cardFills[i] = null; }
            _bigLabel = null; _hint = null;
        }

        public static bool IsOpen => _canvas != null;
    }
}
