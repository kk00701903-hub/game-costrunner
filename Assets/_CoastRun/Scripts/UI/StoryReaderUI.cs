using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 52차(사용자): 스토리 모드 컷씬을 **웹소설 읽듯** 보는 리더 — 챕터마다 하나(오프닝+엔딩 대본을 이어서).
    /// 55차(사용자, 첨부 시안): 화면 구성을 시안대로 — 위 리본 제목「제 N화 제목」+ A-/A+/건너뛰기, 위 42 %는 **삽화 고정**(스크롤에
    ///   따라 그 장면의 컷으로 바뀜), 아래는 **어두운 반투명 패널** 안에 본문 스크롤(지문은 밝은 글씨, 대사는 색 알약 이름표 + 「대사」),
    ///   맨 아래 「다음」 단추. 57차(사용자): 글자가 작다 → 본문 20(≈12pt)·대사 20·이름표 15·캡션 15·요약 18 — 스크롤이 길어지는 건 괜찮다. 54차-2 산문 층(지난 이야기·장면 캡션·소설 지문·제주말 풀이)은 그대로.
    ///   다 읽으면 ChapterVN 과 같은 흔적을 남긴다(PlayerPrefs CoastRun_VN_<id>, RecordTable.OnSceneWatched) — 롱컷 카운트·레코드 해금 공유.
    public static class StoryReaderUI
    {
        private static Canvas _canvas;
        private static RectTransform _root, _content, _illustBox;
        private static Image _illust, _illustFade, _scrollHandle;
        private static ScrollRect _scroll;
        private static Action _onDone;
        private static readonly List<Text> _texts = new List<Text>();
        private static float _fontScale = 1f;
        private static string[] _ids;
        private static int _chapter;
        private static readonly List<string> _dialogue = new List<string>();   // 54차-2: 제주말 풀이용(대사만)
        // 55차: 스크롤 위치에 따라 바뀌는 삽화 — (본문 안 표식, 텍스처)
        private static readonly List<KeyValuePair<RectTransform, Texture2D>> _marks = new List<KeyValuePair<RectTransform, Texture2D>>();
        private static Texture2D _shown;
        private static float _fadeT;
        public static bool IsOpen => _canvas != null;

        private static readonly Color Paper = new Color(0.99f, 0.96f, 0.90f);
        private static readonly Color PanelDark = new Color(0.10f, 0.09f, 0.16f, 0.90f);
        private static readonly Color Ink = new Color(0.93f, 0.90f, 0.86f);          // 어두운 패널 위 본문
        private static readonly Color Soft = new Color(0.72f, 0.69f, 0.78f);
        private static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        private static readonly Color Rose = new Color(0.98f, 0.55f, 0.70f);
        private static readonly Color Sea = new Color(0.55f, 0.78f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.38f);

        /// 챕터 리더 열기. 오프닝(+엔딩) 대본을 한 편으로.
        public static void OpenChapter(int chapter, Action onDone)
        {
            string id = $"CH{chapter:00}";
            var cover = ArtAssets.LoadTexture("Cut_" + id + "_Open") ?? ArtAssets.LoadTexture("Cut_" + id + "_Mid") ?? ArtAssets.LoadTexture("Cut_" + id + "_Close");
            Open(StoryProgress.ChapterSceneIds(chapter), Loc.T($"제 {chapter}화", $"Ch. {chapter}"), ChapterScript.Title(chapter), onDone, cover, chapter);
        }

        /// 54차-2: chapter 를 주면 「지난 이야기」 요약·장면 캡션·소설 지문(StoryProse)·제주말 풀이를 붙인다(한국어).
        public static void Open(string[] sceneIds, string kicker, string title, Action onDone, Texture2D cover = null, int chapter = 0)
        {
            Close();
            _onDone = onDone; _ids = sceneIds ?? new string[0]; _chapter = chapter; _dialogue.Clear(); _marks.Clear(); _shown = null; _fadeT = 0f;
            if (_ids.Length == 0) { var cb = _onDone; _onDone = null; cb?.Invoke(); return; }
            foreach (var id in _ids) PlayerPrefs.SetInt("CoastRun_VN_" + id, 1);   // ChapterVN 과 같이 시작 시점에 본 것으로
            _canvas = CoastUiCanvas.Create("StoryReaderCanvas", 468);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            _canvas.gameObject.AddComponent<ReaderKeys>();   // 에디터/원격: Return = 다 읽음, Esc = 건너뛰기, F8/F7 = 한 화면씩
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            _texts.Clear(); _baseSizes.Clear();

            // 종이 배경(가장자리까지)
            var bg = CoastHudLayout.MakeImage(_root, "Paper", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), Paper);
            bg.raycastTarget = true;

            // ── 위 42 %: 삽화(고정, 스크롤 따라 장면 컷으로 교체) ──
            _illustBox = new GameObject("IllustBox", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            _illustBox.SetParent(_root, false);
            _illustBox.anchorMin = new Vector2(0f, 1f); _illustBox.anchorMax = new Vector2(1f, 1f);
            _illustBox.offsetMin = new Vector2(-pad, -600f); _illustBox.offsetMax = new Vector2(pad, pad);
            var boxImg = _illustBox.GetComponent<Image>(); boxImg.color = new Color(0.90f, 0.85f, 0.78f); boxImg.raycastTarget = false;
            _illust = new GameObject("Illust", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _illust.transform.SetParent(_illustBox, false); _illust.raycastTarget = false; _illust.preserveAspect = false;
            _illustFade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _illustFade.transform.SetParent(_illustBox, false); _illustFade.raycastTarget = false; _illustFade.preserveAspect = false; _illustFade.color = new Color(1f, 1f, 1f, 0f);
            // 삽화 아래 그림자(패널로 스며들게)
            var shade = CoastHudLayout.MakeImage(_illustBox, "Shade", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 70f), new Color(0.10f, 0.09f, 0.16f, 0.55f));
            shade.raycastTarget = false;

            // ── 리본 제목(왼쪽 위) + A-/A+/건너뛰기(오른쪽 위) ──
            var ribbon = CoastUiArt.GlossyPill(_root, "Ribbon", new Color(0.98f, 0.45f, 0.62f), 22, 7);
            var rrt = ribbon.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(0f, 1f); rrt.pivot = new Vector2(0f, 1f); rrt.anchoredPosition = new Vector2(-6f, -8f); rrt.sizeDelta = new Vector2(330f, 62f); ribbon.raycastTarget = false;
            var bow = CoastUiArt.CutePill(rrt, "Bow", new Color(1f, 0.75f, 0.85f), 10, 2); bow.raycastTarget = false;
            var bwr = bow.rectTransform; bwr.anchorMin = bwr.anchorMax = new Vector2(0f, 0.5f); bwr.pivot = new Vector2(0.5f, 0.5f); bwr.anchoredPosition = new Vector2(22f, 4f); bwr.sizeDelta = new Vector2(26f, 26f);
            var rt = CoastHudLayout.MakeText(rrt, "T", $"{kicker}  {title}", 24, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(46f, 3f), new Vector2(-12f, 0f));
            rt.color = Color.white; rt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(rt, new Color(0.45f, 0.08f, 0.22f, 0.6f), 1.6f);
            rt.resizeTextForBestFit = true; rt.resizeTextMinSize = 14; rt.resizeTextMaxSize = CoastHudLayout.Scaled(24);
            SmallPill(_root, "Aminus", "A-", new Vector2(-172f, -38f), () => SetScale(_fontScale - 0.1f));
            SmallPill(_root, "Aplus", "A+", new Vector2(-120f, -38f), () => SetScale(_fontScale + 0.1f));
            SmallPill(_root, "Skip", Loc.T("건너뛰기 ›", "Skip ›"), new Vector2(-8f, -38f), Finish, 104f);

            // ── 아래: 어두운 반투명 패널 + 본문 스크롤 ──
            var panel = CoastUiArt.Panel(_root, "Panel", PanelDark, 26);
            var prt = panel.rectTransform; prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(6f, 86f); prt.offsetMax = new Vector2(-6f, -592f); panel.raycastTarget = true;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            viewport.SetParent(prt, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(16f, 14f); viewport.offsetMax = new Vector2(-30f, -14f);
            var vimg = viewport.GetComponent<Image>(); vimg.color = new Color(0f, 0f, 0f, 0f); vimg.raycastTarget = true;
            _content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = new Vector2(1f, 1f); _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero; _content.offsetMax = Vector2.zero;
            var vl = _content.GetComponent<VerticalLayoutGroup>();
            vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childForceExpandWidth = true;
            vl.spacing = 14f; vl.padding = new RectOffset(6, 6, 12, 30);
            var fit = _content.GetComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scroll = panel.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport; _scroll.content = _content; _scroll.horizontal = false; _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 40f; _scroll.inertia = true; _scroll.decelerationRate = 0.12f;
            // 스크롤바(표시만): 오른쪽 얇은 줄 + 손잡이
            var track = CoastHudLayout.MakeImage(prt, "Track", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-18f, 18f), new Vector2(-12f, -18f), new Color(1f, 1f, 1f, 0.10f));
            track.raycastTarget = false;
            _scrollHandle = CoastHudLayout.MakeImage(track.rectTransform, "Handle", new Vector2(0f, 0.7f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.45f));
            _scrollHandle.raycastTarget = false;
            _canvas.gameObject.AddComponent<ReaderScroll>();

            // 54차-2: 지난 이야기(1화는 「이야기의 시작」 한 줄) — 본문 맨 위.
            string recap = Loc.IsKo && _chapter > 0 ? StoryProse.Recap(_chapter) : null;
            if (!string.IsNullOrEmpty(recap)) Recap(_chapter <= 1 ? "이야기의 시작" : "지난 이야기", recap);

            // 본문: 씬마다 이어 붙이고 사이에 「· · ·」. 씬의 컷 그림(Cut_<id>)은 위 삽화로(스크롤 표식).
            for (int s = 0; s < _ids.Length; s++)
            {
                if (s > 0) Divider();
                var sceneArt = ArtAssets.LoadTexture("Cut_" + _ids[s]) ?? (s == 0 ? cover : null);
                if (sceneArt != null) Mark(sceneArt);
                Build(_ids[s]);
            }
            // 54차-2: 대사에 나온 제주말 풀이(있을 때만)
            string gloss = Loc.IsKo ? StoryProse.Glossary(_dialogue) : null;
            if (!string.IsNullOrEmpty(gloss)) Glossary(gloss);
            var endT = Block("End", new RectOffset(6, 6, 6, 0));
            MakeBody(endT.transform, Loc.T("— 이번 화 끝 —", "— end of chapter —"), 17, Soft, TextAnchor.MiddleCenter);

            // ── 맨 아래 「다음」 ──
            var next = CoastUiArt.GlossyPill(_root, "Next", new Color(0.99f, 0.98f, 0.96f), 26, 6);
            var nrt = next.rectTransform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0f); nrt.pivot = new Vector2(0.5f, 0f); nrt.anchoredPosition = new Vector2(0f, 14f); nrt.sizeDelta = new Vector2(320f, 62f); next.raycastTarget = true;
            var nb = next.gameObject.AddComponent<Button>(); nb.transition = Selectable.Transition.None;
            nb.onClick.AddListener(() => { CoastPrefs.Vibrate(); Finish(); });
            var nt = CoastHudLayout.MakeText(nrt, "T", Loc.T("다음  ▶", "Next  ▶"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), new Vector2(0f, 1f));
            nt.color = Navy; nt.fontStyle = FontStyle.Bold;

            var first = _marks.Count > 0 ? _marks[0].Value : cover;
            SetIllust(first, true);
            VnMusic.Cue("M6", "0.35", "1");   // 아빠의 곡(inst.) 을 나지막이
            SetScale(PlayerPrefs.GetFloat("CoastRun_ReaderScale", 1f));
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// 삽화 교체 표식(본문 안 0 높이 상자).
        private static void Mark(Texture2D tex)
        {
            var box = Box(0f); box.name = "Mark";
            _marks.Add(new KeyValuePair<RectTransform, Texture2D>(box, tex));
        }

        /// 위 삽화를 바꾼다(가득 채워 자르기, 위쪽을 조금 더 보이게). 처음이 아니면 짧게 교차 페이드.
        private static void SetIllust(Texture2D tex, bool instant)
        {
            if (tex == null || _illust == null || tex == _shown) return;
            _shown = tex;
            if (!instant && _illust.sprite != null)
            {
                _illustFade.sprite = _illust.sprite;
                var f = _illustFade.rectTransform; var o = _illust.rectTransform;
                f.anchorMin = o.anchorMin; f.anchorMax = o.anchorMax; f.offsetMin = o.offsetMin; f.offsetMax = o.offsetMax;
                _illustFade.color = Color.white; _fadeT = 1f;
            }
            _illust.sprite = CoastUiArt.AsSprite(tex, 100f); _illust.color = Color.white;
            Cover(_illust.rectTransform, tex);
        }

        private static void Cover(RectTransform rt, Texture2D tex)
        {
            // 상자(가로 720, 세로 600+여백)를 가득 채우도록 — 세로가 남으면 위쪽 35 % 지점을 보인다(얼굴이 위에 있다)
            float bw = 720f, bh = 600f + CoastUiCanvas.HudPad;
            float ta = (float)tex.width / tex.height, ba = bw / bh;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            if (ta < ba) { float h = bw / ta; float extra = h - bh; rt.offsetMin = new Vector2(0f, -extra * 0.65f); rt.offsetMax = new Vector2(0f, extra * 0.35f); }
            else { float w = bh * ta; float extra = w - bw; rt.offsetMin = new Vector2(-extra * 0.5f, 0f); rt.offsetMax = new Vector2(extra * 0.5f, 0f); }
        }

        private static void Build(string sceneId)
        {
            var lines = ChapterScript.Get(sceneId);
            int bgIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                string txt = Loc.IsKo ? line.B : Loc.Tr(ChapterScript.TextEn(sceneId, i) ?? line.B);
                string speaker = line.A; bool pass = true; bool gate = false;
                if (line.Kind == "SAY") speaker = StoryCond.Strip(line.A, out pass);
                else if (line.Kind == "NARR" || line.Kind == "LETTER")
                {
                    gate = !string.IsNullOrEmpty(line.B) && line.B.StartsWith("[");   // [게이트…] 속마음 줄 — 흐리게
                    StoryCond.Strip(line.B, out pass); txt = StoryCond.Strip(txt, out _);
                    if (Loc.IsKo && !gate) txt = StoryProse.Prose(txt);   // 54차-2: 지문 → 소설 문장
                }
                if (!pass) continue;
                switch (line.Kind)
                {
                    case "BG":
                        if (Loc.IsKo) Caption(StoryProse.Caption(sceneId, bgIndex) ?? CaptionFromVariant(line.D));
                        bgIndex++;
                        { var bt = ArtAssets.LoadTexture("BG_" + line.A); if (bt != null) Mark(bt); }
                        break;
                    case "CG": { var ct = ArtAssets.LoadTexture("Cut_" + line.A); if (ct != null) Mark(ct); } break;
                    case "SAY": _dialogue.Add(txt); Say(ChapterScript.SpeakerName(speaker), txt); break;
                    case "NARR": if (gate) Aside(txt); else Para(txt); break;
                    case "LETTER": Letter(txt); break;
                }
            }
        }

        private static RectTransform Box(float h)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_content, false);
            var le = go.GetComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h;
            return rt;
        }

        private static readonly List<int> _baseSizes = new List<int>();
        private static Text MakeBody(Transform parent, string s, int size, Color c, TextAnchor a)
        {
            var t = CoastHudLayout.MakeText(parent, "T", s, size, a, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = c; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.lineSpacing = 1.4f;
            _texts.Add(t); _baseSizes.Add(size);
            return t;
        }

        /// 세로 레이아웃 상자 — 안의 Text 들은 preferredHeight 로 높이가 잡힌다(ContentSizeFitter 불필요).
        private static VerticalLayoutGroup Block(string name, RectOffset pad, float spacing = 2f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(_content, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            v.spacing = spacing; v.padding = pad;
            return v;
        }

        /// 장면 캡션(장소 · 시간) — 표가 없으면 변형(세피아=기억, 밤, 비)으로 짐작.
        private static string CaptionFromVariant(string variant)
        {
            if (string.IsNullOrEmpty(variant)) return null;
            if (variant.Contains("세피아")) return "기억";
            if (variant.Contains("밤")) return "밤";
            if (variant.Contains("비")) return "비";
            return null;
        }

        private static void Caption(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            s = s.Trim(' ', '—', '-', '–');
            var v = Block("Caption", new RectOffset(6, 6, 8, 0));
            var t = MakeBody(v.transform, "—  " + s + "  —", 15, Soft, TextAnchor.MiddleCenter); t.fontStyle = FontStyle.Bold;
        }

        /// [게이트] 속마음 줄 — 작고 흐린 기울임.
        private static void Aside(string s)
        {
            var v = Block("Aside", new RectOffset(18, 6, 0, 0));
            var t = MakeBody(v.transform, s, 18, Soft, TextAnchor.UpperLeft); t.fontStyle = FontStyle.Italic;
        }

        private static void Recap(string kicker, string s)
        {
            var v = Block("Recap", new RectOffset(16, 16, 10, 12), 4f);
            var im = v.gameObject.AddComponent<Image>(); im.sprite = CoastUiArt.RoundedRect(12); im.type = Image.Type.Sliced; im.color = new Color(1f, 1f, 1f, 0.08f); im.raycastTarget = false;
            var k = MakeBody(v.transform, kicker, 14, Rose, TextAnchor.UpperLeft); k.fontStyle = FontStyle.Bold;
            MakeBody(v.transform, s, 18, Soft, TextAnchor.UpperLeft);
        }

        private static void Glossary(string s)
        {
            Divider();
            var v = Block("Gloss", new RectOffset(16, 16, 10, 12), 4f);
            var im = v.gameObject.AddComponent<Image>(); im.sprite = CoastUiArt.RoundedRect(12); im.type = Image.Type.Sliced; im.color = new Color(1f, 1f, 1f, 0.08f); im.raycastTarget = false;
            var k = MakeBody(v.transform, "제주말 풀이", 14, Sea, TextAnchor.UpperLeft); k.fontStyle = FontStyle.Bold;
            MakeBody(v.transform, s, 17, Soft, TextAnchor.UpperLeft);
        }

        private static void Para(string s)
        {
            var v = Block("Para", new RectOffset(4, 4, 0, 0));
            MakeBody(v.transform, s, 20, Ink, TextAnchor.UpperLeft);
        }

        /// 55차(시안): 색 알약 이름표 + 「대사」 — 하늘 = 하늘색, 아빠 = 금색, 그 외 = 분홍.
        private static void Say(string who, string s)
        {
            var go = new GameObject("Say", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(_content, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.childControlHeight = true; h.childControlWidth = true; h.childForceExpandHeight = false; h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.UpperLeft; h.spacing = 8f; h.padding = new RectOffset(2, 2, 0, 0);
            Color pillCol = who == "하늘" ? Sea : (who.Contains("아빠") || who.Contains("아버지")) ? Gold : Rose;
            var pill = CoastUiArt.CutePill(go.transform, "Name", pillCol, 10, 2); pill.raycastTarget = false;
            var le = pill.gameObject.AddComponent<LayoutElement>();
            float w = Mathf.Max(52f, 17f * Mathf.Max(2, who.Length) + 20f);
            le.preferredWidth = w; le.minWidth = w; le.preferredHeight = 32f; le.minHeight = 32f; le.flexibleWidth = 0f;
            var n = CoastHudLayout.MakeText(pill.rectTransform, "T", who, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(2f, 1f), new Vector2(-2f, 0f));
            n.color = Navy; n.fontStyle = FontStyle.Bold; n.resizeTextForBestFit = true; n.resizeTextMinSize = 9; n.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var t = MakeBody(go.transform, "「" + s + "」", 20, Ink, TextAnchor.UpperLeft);
            var tle = t.gameObject.AddComponent<LayoutElement>(); tle.flexibleWidth = 1f; tle.minWidth = 100f;
        }

        private static void Letter(string s)
        {
            var v = Block("Letter", new RectOffset(18, 18, 12, 12));
            var im = v.gameObject.AddComponent<Image>(); im.sprite = CoastUiArt.RoundedRect(12); im.type = Image.Type.Sliced; im.color = new Color(0.98f, 0.94f, 0.82f, 0.92f); im.raycastTarget = false;
            var t = MakeBody(v.transform, s, 18, new Color(0.35f, 0.27f, 0.20f), TextAnchor.UpperLeft); t.fontStyle = FontStyle.Italic;
        }

        private static void Divider()
        {
            var box = Box(30f);
            var t = CoastHudLayout.MakeText(box, "T", "· · ·", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = Soft;
        }

        private static void SmallPill(Transform parent, string name, string label, Vector2 pos, Action onClick, float w = 44f)
        {
            var pill = CoastUiArt.CutePill(parent, name, new Color(0.99f, 0.98f, 0.96f), 12, 2);
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(w, 40f); pill.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            var t = CoastHudLayout.MakeText(rt, "T", label, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero);
            t.color = Navy; t.fontStyle = FontStyle.Bold;
        }

        private static void SetScale(float s)
        {
            _fontScale = Mathf.Clamp(s, 0.9f, 1.6f);
            PlayerPrefs.SetFloat("CoastRun_ReaderScale", _fontScale);
            for (int i = 0; i < _texts.Count && i < _baseSizes.Count; i++) if (_texts[i] != null) _texts[i].fontSize = Mathf.RoundToInt(_baseSizes[i] * _fontScale);
        }

        private static void Finish()
        {
            var ids = _ids; var cb = _onDone;
            Close();
            VnMusic.Stop(0.8f);
            if (ids != null) foreach (var id in ids) RecordTable.OnSceneWatched(id);
            cb?.Invoke();
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null; _content = null; _scroll = null; _onDone = null; _illust = null; _illustFade = null; _scrollHandle = null;
            _texts.Clear(); _baseSizes.Clear(); _marks.Clear(); _shown = null;
        }

        /// 스크롤 위치 → 삽화 교체 + 스크롤바 손잡이 + 교차 페이드.
        private class ReaderScroll : MonoBehaviour
        {
            private void LateUpdate()
            {
                if (_scroll == null || _content == null) return;
                float viewH = _scroll.viewport.rect.height, total = _content.rect.height;
                float y = _content.anchoredPosition.y;   // 위로 스크롤한 양(0 = 맨 위)
                // 삽화: 뷰포트 위쪽 40 % 지점을 지난 마지막 표식
                Texture2D want = _marks.Count > 0 ? _marks[0].Value : null;
                float probe = y + viewH * 0.40f;
                for (int i = 0; i < _marks.Count; i++)
                {
                    var m = _marks[i].Key; if (m == null) continue;
                    float my = -m.anchoredPosition.y;   // 본문 안 위치(위에서부터)
                    if (my <= probe) want = _marks[i].Value; else break;
                }
                if (want != null && want != _shown) SetIllust(want, false);
                if (_fadeT > 0f && _illustFade != null) { _fadeT = Mathf.MoveTowards(_fadeT, 0f, Time.unscaledDeltaTime * 2.5f); _illustFade.color = new Color(1f, 1f, 1f, _fadeT); }
                if (_scrollHandle != null && total > 1f)
                {
                    float frac = Mathf.Clamp01(viewH / total), top = Mathf.Clamp01(y / Mathf.Max(1f, total - viewH));
                    float a = 1f - top * (1f - frac);
                    _scrollHandle.rectTransform.anchorMin = new Vector2(0f, Mathf.Max(0f, a - frac)); _scrollHandle.rectTransform.anchorMax = new Vector2(1f, a);
                    _scrollHandle.enabled = frac < 0.999f;
                }
            }
        }

        private class ReaderKeys : MonoBehaviour
        {
            private void Update()
            {
                if (CoastRemoteKeys.Down(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return)) Finish();
                // PageDown/PageUp(원격은 F8/F7 — PageDown 은 StageManager 가 씀): 한 화면씩
                bool dn = CoastRemoteKeys.Down(KeyCode.F8) || Input.GetKeyDown(KeyCode.PageDown);
                bool up = CoastRemoteKeys.Down(KeyCode.F7) || Input.GetKeyDown(KeyCode.PageUp);
                if ((dn || up) && _scroll != null && _content != null)
                {
                    float page = _scroll.viewport.rect.height * 0.9f, total = Mathf.Max(1f, _content.rect.height - _scroll.viewport.rect.height);
                    _scroll.verticalNormalizedPosition = Mathf.Clamp01(_scroll.verticalNormalizedPosition + (dn ? -page : page) / total);
                }
            }
        }
    }
}
