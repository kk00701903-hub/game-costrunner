using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 39차-4: 타이틀 CHAPTER 칩 → K-POP 러닝모드 챕터 선택 페이지 (CH 1~20).
    /// 선택 안 하면 마지막으로 클리어한 K-POP 챕터의 다음 챕터가 자동으로 잡힌다(ArcadeRun.KpopDefaultChapter).
    /// 720×1280 디자인 단위, 런타임 빌드. 카드 5열×4행, 계절별 색(봄 노랑·여름 하늘·가을 주황·겨울 보라).
    public static class KpopChapterSelect
    {
        private static Canvas _canvas;
        private static Image[] _cards = new Image[Timeline.Chapters];
        private static bool[] _unlocked = new bool[Timeline.Chapters];
        private static int[] _gradeOf = new int[Timeline.Chapters];
        private static Text _bigLabel;
        private static int _picked;
        private static Action<int> _onPick;
        private static Action<int> _onPlay;

        private static readonly Color[] SeasonFill =
        {
            new Color(1f, 0.90f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.65f, 0.35f), new Color(0.72f, 0.62f, 0.95f),
        };
        private static readonly Color Navy = new Color(0.10f, 0.13f, 0.30f);

        /// onPick: 카드를 골랐을 때(닫기 포함) 현재 선택 챕터. onPlay: 「CH n 달리기」.
        /// 47차: 타이틀 UI(제목 글자·K-POP Play 바)가 딤 뒤로 비쳐 시안과 달랐다 → 열려 있는 동안 숨긴다(MainMenuController 가 넣어 줌).
        public static CanvasGroup TitleUi;

        public static void Open(GameManager gm, Action<int> onPick, Action<int> onPlay)
        {
            Close();
            if (TitleUi != null) TitleUi.alpha = 0f;
            _onPick = onPick; _onPlay = onPlay;
            _picked = ArcadeRun.KpopChapter(gm);
            if (!IsUnlocked(_picked, gm != null ? gm.Profile : null)) _picked = Mathf.Clamp(ArcadeRun.KpopLastClear + 1, 1, Timeline.Chapters);

            _canvas = CoastUiCanvas.Create("KpopChapterSelect", 320);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;

            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.03f, 0.05f, 0.14f, 0.70f));   // 39차-6: 시안처럼 뒤(키아트)가 비쳐 보이게 · 47차 0.70
            dim.raycastTarget = true;

            // 39차-6: 시안(ref_chapter) — 금색 입체 CHAPTER(크롭 그림), 우상단 파란 X, 「챕터 N」 카드 5×4(계절별 2줄씩 묶음), 아래 핑크 큰 버튼
            var titleSpr = CoastUiArt.Art("UI_ChapterTitle");
            if (titleSpr != null)
            {
                var ti = new GameObject("Title", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ti.transform.SetParent(root, false); ti.sprite = titleSpr; ti.preserveAspect = true; ti.raycastTarget = false;
                var trt = ti.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f);
                trt.anchoredPosition = new Vector2(-6f, -22f); trt.sizeDelta = new Vector2(400f, 103f);
            }
            else
            {
                var title = CoastHudLayout.MakeText(root, "Title", "CHAPTER", 44, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -30f));
                title.color = new Color(1f, 0.85f, 0.30f); title.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(title, new Color(0.30f, 0.12f, 0.02f, 0.9f), 2.5f);
            }
            var sub = CoastHudLayout.MakeText(root, "Sub", Loc.T("달릴 챕터를 고르세요 · 고르지 않으면 마지막 플레이한 다음 챕터", "Pick a chapter · default is the one after your last run"), 13, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -148f), new Vector2(0f, -120f));
            sub.color = Color.white; CoastUiArt.OutlineText(sub, new Color(0f, 0f, 0f, 0.6f), 1.2f);

            // 우상단 X
            var xSpr = CoastUiArt.Art("UI_CloseX");
            var xgo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            xgo.transform.SetParent(root, false);
            var xi = xgo.GetComponent<Image>(); xi.raycastTarget = true;
            if (xSpr != null) { xi.sprite = xSpr; xi.preserveAspect = true; } else xi.color = new Color(0.25f, 0.45f, 0.85f);
            var xrt = xgo.GetComponent<RectTransform>(); xrt.anchorMin = xrt.anchorMax = new Vector2(1f, 1f); xrt.pivot = new Vector2(0.5f, 0.5f);
            xrt.anchoredPosition = new Vector2(-46f, -66f); xrt.sizeDelta = new Vector2(84f, 84f);
            var xb = xgo.GetComponent<Button>(); xb.transition = Selectable.Transition.None;
            xb.onClick.AddListener(() => { int c = _picked; var cb = _onPick; Close(); cb?.Invoke(c); });

            // 카드 5×4 — 112×131, 가로 간격 15, 세로: 1·2줄 붙고(35) 3·4줄은 아래 묶음(사이 112)
            const float cw = 116f, ch = 138f, gap = 14f;
            float x0 = (664f - (5 * cw + 4 * gap)) * 0.5f;
            float[] rowY = { 170f, 342f, 566f, 738f };
            var prof = gm != null ? gm.Profile : null;
            int lastClear = ArcadeRun.KpopLastClear;
            for (int i = 0; i < Timeline.Chapters; i++)
            {
                int n = i + 1;
                int col = i % 5, row = i / 5;
                bool open = IsUnlocked(n, prof);
                _unlocked[i] = open;
                int g = prof != null && prof.trackGrade != null && i < prof.trackGrade.Length ? prof.trackGrade[i] : 0;
                // 46차(사용자 시안): COMPLETED(등급 메달) / CURRENT(금색·자물쇠) / UNREACHED(회색·자물쇠)
                _gradeOf[i] = g;
                bool completed = open && g > 0;
                bool current = open && !completed;
                var fill = completed ? SeasonFill[(n - 1) / 5] : current ? new Color(1f, 0.80f, 0.25f) : new Color(0.30f, 0.30f, 0.34f);
                var card = CoastUiArt.GlossyPill(root, "Card" + n, fill, 18, 9);
                var crt = card.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 1f);
                crt.anchoredPosition = new Vector2(x0 + col * (cw + gap), -rowY[row]);
                crt.sizeDelta = new Vector2(cw, ch);
                card.raycastTarget = true;
                _cards[i] = card;

                var num = CoastHudLayout.MakeText(crt, "N", Loc.T($"챕터 {n}", $"Ch. {n}"), 21, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(0f, -4f));
                num.color = completed ? Navy : current ? Navy : new Color(0.12f, 0.12f, 0.14f);
                CoastUiArt.OutlineText(num, new Color(1f, 1f, 1f, completed || current ? 0.55f : 0.15f), 1.2f);
                // 가운데 그림: 등급 메달(S/A/B/C) 또는 자물쇠
                string art = completed ? (g >= 4 ? "UI_Medal_S" : g == 3 ? "UI_Medal_A" : g == 2 ? "UI_Medal_B" : "UI_Medal_C") : "UI_Lock_Q";
                var spr = CoastUiArt.Art(art);
                if (spr != null)
                {
                    var im = new GameObject("Art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(crt, false); im.sprite = spr; im.preserveAspect = true; im.raycastTarget = false;
                    var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0f); irt.pivot = new Vector2(0.5f, 0f);
                    irt.anchoredPosition = new Vector2(0f, 8f); irt.sizeDelta = completed ? new Vector2(88f, 88f) : new Vector2(64f, 64f);
                    if (!completed && !current) im.color = new Color(0.75f, 0.75f, 0.78f, 0.85f);
                }
                // 카드 아래 상태 글자
                string stateTxt = completed ? "COMPLETED" : current ? "CURRENT" : "UNREACHED";
                var st = CoastHudLayout.MakeText(root, "St" + n, stateTxt, 11, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x0 + col * (cw + gap) - 6f, -rowY[row] - ch - 22f), new Vector2(x0 + col * (cw + gap) + cw + 6f, -rowY[row] - ch - 2f));
                st.color = completed ? new Color(0.75f, 0.90f, 1f) : current ? new Color(1f, 0.85f, 0.30f) : new Color(0.62f, 0.62f, 0.66f);
                st.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(st, new Color(0f, 0f, 0f, 0.6f), 1.2f);

                int pick = n;
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Select(pick));
            }

            // 아래: 큰 「챕터 n 달리기!」
            var play = CoastUiArt.GlossyPill(root, "Play", new Color(0.93f, 0.22f, 0.52f), 40, 14);
            var prt = play.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, 205f); prt.sizeDelta = new Vector2(503f, 106f); play.raycastTarget = true;
            _bigLabel = CoastHudLayout.MakeText(prt, "T", "", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 10f), new Vector2(0f, 6f));
            _bigLabel.color = Color.white; _bigLabel.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_bigLabel, new Color(0f, 0f, 0f, 0.3f), 1.5f);
            var pb = play.gameObject.AddComponent<Button>(); pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(() => { int c = _picked; var cb = _onPlay; Close(); cb?.Invoke(c); });

            Refresh();
        }

        /// 해금: K-POP 마지막 클리어 + 1 까지, 또는 스토리에서 이미 달린 트랙(trackGrade > 0).
        public static bool IsUnlocked(int n, MetaProfile prof)
        {
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
                var baseFill = _unlocked[i] ? (_gradeOf[i] > 0 ? SeasonFill[i / 5] : new Color(1f, 0.80f, 0.25f)) : new Color(0.30f, 0.30f, 0.34f);
                c.color = on ? new Color(1f, 0.95f, 0.55f) : Color.Lerp(baseFill, Color.black, 0.62f);   // 바깥 테두리색: 선택은 밝은 금(시안의 CURRENT 글로우)
                c.rectTransform.localScale = Vector3.one * (on ? 1.08f : 1f);
            }
            if (_bigLabel != null) _bigLabel.text = Loc.T($"챕터 {_picked} 도전하기!", $"Challenge Chapter {_picked}!");
        }

        public static void Close()
        {
            if (TitleUi != null && _canvas != null) TitleUi.alpha = 1f;
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
            for (int i = 0; i < _cards.Length; i++) _cards[i] = null;
            _bigLabel = null;
        }

        public static bool IsOpen => _canvas != null;
    }
}
