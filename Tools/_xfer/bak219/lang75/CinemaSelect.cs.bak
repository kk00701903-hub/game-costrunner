using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 42차: 타이틀 더보기 › 「시네마」 — 컷씬을 골라 다시 본다.
    /// 61차(사용자): 목록 = 프롤로그 영상 + 컷씬 8개(러닝 챕터 1·4·7·10·13·15·18·20 마다 하나, 오프닝/클로징 구분 없음).
    /// 해금: 프롤로그는 항상, 컷씬 N 은 그 챕터에 닿았으면(또는 읽었으면). 비밀코드(devUnlockAll)면 전부.
    /// 103차(사용자 시안): **주차 타임라인** — 왼쪽 주차 알약 + 점선 + 오른쪽 카드. 「컷씬」은 **「메인스토리」**로 부른다.
    ///   메인스토리(펼침 카드: 표지·제목·보기·✓) / 짧은 이야기(EV)는 접힌 한 줄(▼ 펼치기 → 카드) / 잠긴 것은 회색 「다음 콘텐츠 · 예정」.
    ///   위에 「전체 펼치기 / 전체 접기」, 아래에 「팁: 이야기는 접혀있어요」 상자.
    /// 109차(사용자): **4계절 탭**(봄·여름·가을·겨울) — 1주차 계절 탭에서 시작, 각 편은 시작 주차의 계절 탭에 들어간다. 엔딩은 마지막 계절(겨울) 탭 맨 끝.
    ///   주차 줄 간격을 위아래 1cm 쯤 더 벌리고(gap 14 → 110), EV 는 「서브스토리」— 번호 없이 제목만.
    public static class CinemaSelect
    {
        /// 109차: 지금 보는 계절 탭(0 봄 … 3 겨울). Open 마다 1주차의 계절로 시작.
        private static int _season; private static bool _keepSeason;
        private static int SeasonOfEntry(Entry e) => e.Opening ? (int)Timeline.SeasonOf(1) : !string.IsNullOrEmpty(e.EndingId) ? 3 : (int)Timeline.SeasonOf(Timeline.WeekStart(e.Chapter));
        /// 79차(사용자): 엔딩 다시보기를 일단 전부 열어 둔다. 실제 해금(깬 엔딩만)으로 되돌리려면 false.
        public const bool OpenAllEndings = true;

        private static Canvas _canvas;
        private static Action _onClose;
        private static Action _onPlayStart; private static GameManager _gm;   // 73차: 감상 뒤 이 페이지로 돌아오기 위해
        private static RectTransform _content; private static List<Entry> _entries;
        /// 103차: 펼쳐진 줄(세션 동안 유지). 메인스토리·프롤로그·엔딩은 처음부터 펼침, 이야기(EV)는 접힘.
        private static readonly HashSet<string> _expanded = new HashSet<string>();
        private static bool _expandInit;

        private struct Entry
        {
            public string Label, Sub, Title;
            public bool Opening, Unlocked;
            public string EndingId;   // 77차: "END_A" | "END_B" | "END_TRUE" — 엔딩 다시보기(본 엔딩만 열림)
            public int Index, Chapter;   // Index = 메인스토리 번호(1..8), Chapter = 그 메인스토리가 열리는 챕터
            public int Event;         // 85차: 보조 컷씬 EV n(1..10) — 0 이면 아님
            public Texture2D Cover;
            public string Key => Opening ? "OP" : !string.IsNullOrEmpty(EndingId) ? EndingId : Event > 0 ? "EV" + Event : "CS" + Index;
            /// 103차: 주차 라벨 — 프롤로그 1주차, 챕터는 Timeline.WeekStart, 엔딩은 「엔딩」
            public string Week => Opening ? Loc.T("1주차", "Wk 1") : !string.IsNullOrEmpty(EndingId) ? Loc.T("엔딩", "Ending") : Loc.T($"{Timeline.WeekStart(Chapter)}주차", $"Wk {Timeline.WeekStart(Chapter)}");
        }

        private static readonly Color[] SeasonFill =
        {
            new Color(1f, 0.90f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.65f, 0.35f), new Color(0.72f, 0.62f, 0.95f),
        };
        private static readonly Color Navy = new Color(0.10f, 0.13f, 0.30f);
        private static readonly Color Locked = new Color(0.66f, 0.66f, 0.70f);
        // 103차 시안 색: 메인스토리 = 노랑 카드, 이야기 = 하늘색 줄, 주차 알약 = 노랑(메인)/분홍보라(이야기)/회색(잠김)
        private static readonly Color CardMain = new Color(1f, 0.93f, 0.55f);
        private static readonly Color CardStory = new Color(0.72f, 0.88f, 1f);
        private static readonly Color CardPro = new Color(1f, 0.80f, 0.86f);
        private static readonly Color PillMain = new Color(1f, 0.88f, 0.35f);
        private static readonly Color PillStory = new Color(0.93f, 0.70f, 0.95f);
        private static readonly Color PillPro = new Color(1f, 0.70f, 0.80f);
        private static readonly Color LineCol = new Color(0.62f, 0.55f, 0.85f, 0.9f);

        /// onPlayStart: 컷씬 재생 직전(타이틀 음악 정지 등). onClose: 페이지를 닫거나 컷씬이 끝나 돌아왔을 때.
        /// 61차(사용자): 목록 = 프롤로그 영상 + **메인스토리 8개**(오프닝/클로징 구분 없이) — 각 메인스토리는 리더(StoryReaderUI.OpenCutscene)로 읽는다.
        public static void Open(GameManager gm, Action onPlayStart, Action onClose)
        {
            Close();
            _onClose = onClose; _onPlayStart = onPlayStart; _gm = gm;
            if (!_keepSeason) _season = (int)Timeline.SeasonOf(1);   // 109차: 1주차 계절 탭부터(감상 뒤 돌아올 땐 보던 탭 유지)
            _keepSeason = false;
            TitleAudio.PlayRaising();   // 시네마 목록 = 스토리 모드 BGM(컷씬 재생 시 onPlayStart 가 정지)
            var entries = BuildEntries(gm);
            _entries = entries;
            if (!_expandInit)
            {
                _expandInit = true;
                foreach (var e in entries) if (e.Event == 0) _expanded.Add(e.Key);   // 메인스토리·프롤로그·엔딩만 펼침
            }

            _canvas = CoastUiCanvas.Create("CinemaSelect", 320);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;

            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.03f, 0.05f, 0.14f, 0.72f));
            dim.raycastTarget = true;

            // 103차 시안: 상단 보라 띠 「시네마 - 너와 나의 주파수」 + 오른쪽 「나가기」
            var bar = CoastUiArt.GlossyPill(root, "Bar", new Color(0.36f, 0.24f, 0.60f), 18, 6);
            var brt = bar.rectTransform; brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -24f); brt.offsetMin = new Vector2(16f, -96f); brt.offsetMax = new Vector2(-16f, -24f);
            bar.color = new Color(0.30f, 0.18f, 0.52f, 0.95f); bar.raycastTarget = false;
            var title = CoastHudLayout.MakeText(brt, "Title", Loc.T("시네마 - 너와 나의 주파수", "CINEMA - Our Frequency"), 28, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22f, 0f), new Vector2(-130f, 0f));
            title.color = new Color(1f, 0.85f, 0.30f); title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 14; title.resizeTextMaxSize = CoastHudLayout.Scaled(28);
            CoastUiArt.OutlineText(title, new Color(0.30f, 0.12f, 0.02f, 0.9f), 2.5f);
            {
                var xb = CoastUiArt.GlossyPill(brt, "Close", new Color(0.55f, 0.58f, 0.68f), 18, 5); xb.raycastTarget = true;
                var xrt = xb.rectTransform; xrt.anchorMin = xrt.anchorMax = new Vector2(1f, 0.5f); xrt.pivot = new Vector2(1f, 0.5f);
                xrt.anchoredPosition = new Vector2(-12f, 0f); xrt.sizeDelta = new Vector2(104f, 44f);
                var xt = CoastHudLayout.MakeText(xrt, "T", Loc.T("나가기", "Exit"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                xt.color = Color.white; xt.fontStyle = FontStyle.Bold;
                var xbb = xb.gameObject.AddComponent<Button>(); xbb.transition = Selectable.Transition.None;
                xbb.onClick.AddListener(() => { var cb = _onClose; Close(); cb?.Invoke(); });
            }

            int read = 0; foreach (var e in entries) if (!e.Opening && string.IsNullOrEmpty(e.EndingId) && e.Event == 0 && e.Unlocked && StoryProgress.CutsceneRead(e.Index)) read++;
            var saveForClue = gm != null && gm.HasSave ? (gm.Save ?? gm.SaveSys.Load()) : null;
            string clueLine = saveForClue != null ? " · " + ClueSystem.Summary(saveForClue) : "";
            // 103차(사용자): 「컷씬」 → 「메인스토리」
            var subBox = CoastUiArt.GlossyPill(root, "SubBox", new Color(1f, 0.96f, 0.86f), 16, 4); subBox.raycastTarget = false;
            var srt = subBox.rectTransform; srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(20f, -150f); srt.offsetMax = new Vector2(-20f, -106f);
            var sub = CoastHudLayout.MakeText(srt, "Sub", Loc.T($"메인스토리 {StoryProgress.CutsceneCount}편 + 서브스토리 {StoryProgress.EventCount}편 · 본 메인스토리 {read}{clueLine}", $"{StoryProgress.CutsceneCount} main stories + {StoryProgress.EventCount} side stories · {read} seen{clueLine}"), 14, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            sub.color = new Color(0.45f, 0.25f, 0.10f); sub.fontStyle = FontStyle.Bold;
            sub.resizeTextForBestFit = true; sub.resizeTextMinSize = 9; sub.resizeTextMaxSize = CoastHudLayout.Scaled(14);

            // 109차: 4계절 탭 — 봄·여름·가을·겨울(1주차 계절부터). 탭을 누르면 그 계절의 편만 목록에.
            _tabs = new Image[4];
            for (int si = 0; si < 4; si++)
            {
                int pick = si;
                var tb = CoastUiArt.GlossyPill(root, "Season" + si, SeasonFill[si], 20, 6); tb.raycastTarget = true;
                var trt0 = tb.rectTransform; trt0.anchorMin = trt0.anchorMax = new Vector2(0.5f, 1f); trt0.pivot = new Vector2(0.5f, 1f);
                trt0.anchoredPosition = new Vector2(-243f + 162f * si, -160f); trt0.sizeDelta = new Vector2(152f, 52f);
                string nm = Timeline.SeasonName((SeasonKind)si);
                var tt = CoastHudLayout.MakeText(trt0, "T", nm, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                tt.color = new Color(0.30f, 0.16f, 0.06f); tt.fontStyle = FontStyle.Bold;
                var tbb = tb.gameObject.AddComponent<Button>(); tbb.transition = Selectable.Transition.None;
                tbb.onClick.AddListener(() => { if (_season == pick) return; CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _season = pick; Rebuild(); });
                _tabs[si] = tb;
            }
            // 103차: 전체 펼치기 / 전체 접기(109차: 탭 아래로, 작게)
            MakeTopButton(root, "ExpandAll", Loc.T("전체 펼치기", "Expand all"), new Color(1f, 0.86f, 0.30f), new Color(0.45f, 0.25f, 0.05f), -92f, () =>
            {
                foreach (var e in _entries) if (e.Unlocked) _expanded.Add(e.Key);
                Rebuild();
            });
            MakeTopButton(root, "CollapseAll", Loc.T("전체 접기", "Collapse all"), new Color(0.60f, 0.85f, 1f), new Color(0.05f, 0.25f, 0.50f), 92f, () =>
            {
                _expanded.Clear();
                Rebuild();
            });

            // 스크롤 목록 — 타임라인(주차 알약 + 점선 + 카드)
            var viewGo = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewGo.transform.SetParent(root, false);
            var vrt = viewGo.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(20f, 40f); vrt.offsetMax = new Vector2(-20f, -270f);   // 109차: 계절 탭 한 줄만큼 아래로
            var vimg = viewGo.GetComponent<Image>(); vimg.color = new Color(0f, 0f, 0f, 0.001f); vimg.raycastTarget = true;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(vrt, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var scroll = viewGo.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = vrt; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f; scroll.inertia = true;
            _content = content;
            Rebuild();
        }

        private static void MakeTopButton(Transform root, string name, string label, Color fill, Color textCol, float x, Action onTap)
        {
            var b = CoastUiArt.GlossyPill(root, name, fill, 20, 6); b.raycastTarget = true;
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -220f); rt.sizeDelta = new Vector2(172f, 42f);   // 109차: 계절 탭 아래
            var t = CoastHudLayout.MakeText(rt, "T", label, 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            t.color = textCol; t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(19);
            var bb = b.gameObject.AddComponent<Button>(); bb.transition = Selectable.Transition.None;
            bb.onClick.AddListener(() => { CoastAudioManager.PlayAnywhere(CoastSfx.Coin); onTap(); });
        }

        /// 103차: 목록만 다시 그린다(펼침/접힘 바뀔 때).
        private static Image[] _tabs;
        private static void Rebuild()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
            // 109차: 계절 탭 색 — 고른 탭만 진하게, 나머지는 반투명
            if (_tabs != null) for (int si = 0; si < _tabs.Length; si++) if (_tabs[si] != null) { var c = SeasonFill[si]; c.a = si == _season ? 1f : 0.45f; _tabs[si].color = c; _tabs[si].transform.localScale = Vector3.one * (si == _season ? 1.06f : 1f); }
            const float pillW = 122f, lineX = 152f, cardX = 178f, gap = 110f;   // 109차(사용자): 주차 사이를 위아래 1cm 쯤 더(14 → 110)
            float w = 664f - 40f;
            float cardW = w - cardX;
            float y = 0f;
            var shown = new List<Entry>();
            foreach (var e0 in _entries) if (SeasonOfEntry(e0) == _season) shown.Add(e0);
            if (shown.Count == 0)
            {
                var none = CoastHudLayout.MakeText(_content, "None", Loc.T("이 계절엔 아직 이야기가 없어요", "No stories in this season yet"), 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -20f));
                none.color = new Color(1f, 0.95f, 0.85f); _content.sizeDelta = new Vector2(0f, 120f); return;
            }
            for (int i = 0; i < shown.Count; i++)
            {
                var e = shown[i];
                bool open = e.Unlocked && _expanded.Contains(e.Key);
                float h = !e.Unlocked ? 52f : open ? (e.Event > 0 ? 138f : 150f) : 52f;
                // 점선(줄 높이 + 간격만큼)
                float segTop = y, segBot = y + h + (i < shown.Count - 1 ? gap : 0f);
                for (float sy = segTop + 4f; sy < segBot; sy += 16f)
                {
                    var d = CoastHudLayout.MakeImage(_content, "Dash", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(lineX - 2f, -(sy + 9f)), new Vector2(lineX + 2f, -sy), LineCol);
                    d.raycastTarget = false;
                }
                // 주차 알약
                var pillCol = !e.Unlocked ? Locked : e.Opening ? PillPro : !string.IsNullOrEmpty(e.EndingId) ? PillMain : e.Event > 0 ? PillStory : PillMain;
                var pill = CoastUiArt.GlossyPill(_content, "Week_" + e.Key, pillCol, 22, 6); pill.raycastTarget = false;
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = new Vector2(0f, -y); prt.sizeDelta = new Vector2(pillW, 50f);
                var pt = CoastHudLayout.MakeText(prt, "T", e.Week, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                pt.color = !e.Unlocked ? new Color(0.30f, 0.30f, 0.36f) : new Color(0.38f, 0.16f, 0.50f); pt.fontStyle = FontStyle.Bold;
                pt.resizeTextForBestFit = true; pt.resizeTextMinSize = 11; pt.resizeTextMaxSize = CoastHudLayout.Scaled(22);
                // 마름모 연결점
                var dia = CoastHudLayout.MakeImage(_content, "Dia", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(lineX - 9f, -(y + 34f)), new Vector2(lineX + 9f, -(y + 16f)), Color.Lerp(pillCol, Color.white, 0.2f));
                dia.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f); dia.raycastTarget = false;
                // 연결선(마름모 → 카드)
                var link = CoastHudLayout.MakeImage(_content, "Link", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(lineX + 8f, -(y + 26.5f)), new Vector2(cardX, -(y + 23.5f)), Color.Lerp(pillCol, Color.white, 0.2f));
                link.raycastTarget = false;
                if (!e.Unlocked) MakeLockedRow(_content, e, new Vector2(cardX, -y), new Vector2(cardW, h));
                else if (open) MakeCard(_content, e, new Vector2(cardX, -y), new Vector2(cardW, h), _gm, _onPlayStart);
                else MakeCollapsedRow(_content, e, new Vector2(cardX, -y), new Vector2(cardW, h));
                y += h + gap;
            }
            // 팁 상자
            y += 8f;
            var tip = CoastUiArt.GlossyPill(_content, "Tip", new Color(1f, 0.84f, 0.88f), 18, 5); tip.raycastTarget = false;
            var trt = tip.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -y); trt.sizeDelta = new Vector2(w * 0.92f, 78f);
            var t1 = CoastHudLayout.MakeText(trt, "T1", Loc.T("팁: 서브스토리는 접혀있어요", "Tip: side stories are folded"), 20, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -6f));
            t1.color = new Color(0.55f, 0.20f, 0.30f); t1.fontStyle = FontStyle.Bold;
            var t2 = CoastHudLayout.MakeText(trt, "T2", Loc.T("전체 펼치기 버튼으로 한 번에 확인할 수 있어요", "Use Expand all to open them at once"), 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 6f), new Vector2(0f, 0f));
            t2.color = new Color(0.45f, 0.25f, 0.30f);
            y += 78f;
            _content.sizeDelta = new Vector2(0f, y + 24f);
        }

        /// 103차: 카드 위 한 줄 「제N화 제목 : 장소 (N주차)」 / 이야기는 「짧은 이야기 제N화-1 …」
        private static string HeadLine(Entry e)
        {
            if (e.Opening) return Loc.T($"프롤로그 · 너와 나의 주파수 : 오프닝 ({e.Week})", $"Prologue · Our Frequency : Opening ({e.Week})");
            if (!string.IsNullOrEmpty(e.EndingId)) return Loc.T($"{e.Label} · {e.Title}", $"{e.Label} · {e.Title}");
            string place = ChapterLocation.Get(e.Chapter).Name;
            if (e.Event > 0) return Loc.T($"서브스토리 · {e.Title} : {place} ({e.Week})", $"Side story · {e.Title} : {place} ({e.Week})");   // 109차(사용자): 번호 없이 제목만
            return Loc.T($"제{e.Chapter}화 {e.Title} : {place} ({e.Week})", $"Ch.{e.Chapter} {e.Title} : {place} ({e.Week})");
        }

        private static Image MakeRowPill(RectTransform parent, Entry e, Vector2 pos, Vector2 size, Color fill, int radius)
        {
            var card = CoastUiArt.GlossyPill(parent, "Row_" + e.Key, fill, radius, 6);
            var crt = card.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = pos; crt.sizeDelta = size;
            card.raycastTarget = true;
            return card;
        }

        /// 103차: 잠긴 줄 — 회색 「다음 콘텐츠 · 예정」
        private static void MakeLockedRow(RectTransform parent, Entry e, Vector2 pos, Vector2 size)
        {
            var card = MakeRowPill(parent, e, pos, size, Locked, 16);
            card.color = new Color(0.80f, 0.80f, 0.84f);
            bool ending = !string.IsNullOrEmpty(e.EndingId);
            var t = CoastHudLayout.MakeText(card.rectTransform, "T", ending ? Loc.T("??? · 이 엔딩을 보면 열려요", "??? · reach this ending") : Loc.T("다음 콘텐츠 · 예정", "Next content · coming"), 15, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-90f, 0f));
            t.color = new Color(0.36f, 0.36f, 0.42f); t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var a = CoastHudLayout.MakeText(card.rectTransform, "A", Loc.T("▼ 펼치기", "▼ Open"), 14, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-14f, 0f));
            a.color = new Color(0.45f, 0.45f, 0.52f); a.fontStyle = FontStyle.Bold;
            var btn = card.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            var entry = e; btn.onClick.AddListener(() => Play(entry, _onPlayStart));   // 잠김 안내 토스트
        }

        /// 103차: 접힌 줄(이야기·접은 메인스토리) — 한 줄 제목 + 「▼ 펼치기」
        private static void MakeCollapsedRow(RectTransform parent, Entry e, Vector2 pos, Vector2 size)
        {
            bool isEv = e.Event > 0;
            var fill = e.Opening ? CardPro : isEv ? CardStory : CardMain;
            var card = MakeRowPill(parent, e, pos, size, fill, 16);
            card.color = Color.Lerp(fill, Color.white, 0.15f);
            var t = CoastHudLayout.MakeText(card.rectTransform, "T", HeadLine(e), 15, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-104f, 0f));
            t.color = isEv ? new Color(0.08f, 0.28f, 0.55f) : new Color(0.45f, 0.25f, 0.05f); t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 8; t.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;   // 103차: 「▼ 펼치기」와 겹치지 않게
            var a = CoastHudLayout.MakeText(card.rectTransform, "A", Loc.T("▼ 펼치기", "▼ Open"), 14, TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-14f, 0f));
            a.color = isEv ? new Color(0.10f, 0.35f, 0.70f) : new Color(0.55f, 0.30f, 0.05f); a.fontStyle = FontStyle.Bold;
            var btn = card.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            var key = e.Key; btn.onClick.AddListener(() => { CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _expanded.Add(key); Rebuild(); });
        }

        /// 펼친 카드: 위 한 줄(제N화 제목 : 장소 (N주차)) + 왼쪽 표지 + 「메인스토리 N · 제목」 + 「보기」 + ✓
        private static void MakeCard(RectTransform parent, Entry e, Vector2 pos, Vector2 size, GameManager gm, Action onPlayStart)
        {
            bool ending = !string.IsNullOrEmpty(e.EndingId);
            bool isEv = e.Event > 0;
            var fill = e.Opening ? CardPro : ending ? new Color(0.95f, 0.78f, 0.35f) : isEv ? CardStory : CardMain;
            var card = MakeRowPill(parent, e, pos, size, fill, 18);
            card.color = Color.Lerp(fill, Color.white, 0.12f);
            var dark = isEv ? new Color(0.08f, 0.28f, 0.55f) : new Color(0.45f, 0.25f, 0.05f);

            // 위 한 줄
            var head = CoastHudLayout.MakeText(card.rectTransform, "Head", HeadLine(e), 14, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -36f), new Vector2(-96f, -6f));
            head.color = dark; head.fontStyle = FontStyle.Bold;
            head.resizeTextForBestFit = true; head.resizeTextMinSize = 8; head.resizeTextMaxSize = CoastHudLayout.Scaled(14);
            head.horizontalOverflow = HorizontalWrapMode.Wrap; head.verticalOverflow = VerticalWrapMode.Truncate;

            // 왼쪽 표지(있으면) — 둥근 상자 안에 가득 채워 자르기
            float textLeft = 16f; float coverW = 128f; float top = 40f;
            if (e.Cover != null)
            {
                var frame = new GameObject("Cover", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
                frame.SetParent(card.rectTransform, false); frame.anchorMin = new Vector2(0f, 0f); frame.anchorMax = new Vector2(0f, 1f); frame.pivot = new Vector2(0f, 0.5f);
                frame.anchoredPosition = new Vector2(10f, -top * 0.5f + 4f); frame.sizeDelta = new Vector2(coverW, -(top + 8f));
                var fimg = frame.GetComponent<Image>(); fimg.sprite = CoastUiArt.RoundedRect(12); fimg.type = Image.Type.Sliced; fimg.color = new Color(0f, 0f, 0f, 0.35f); fimg.raycastTarget = false;
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(frame, false); im.sprite = CoastUiArt.AsSprite(e.Cover, 100f); im.raycastTarget = false; im.preserveAspect = false;
                im.color = Color.white;
                var irt = im.rectTransform; float ar = e.Cover.width / (float)e.Cover.height; float fw = coverW, fh = size.y - top - 8f;
                if (ar > fw / fh) { irt.anchorMin = new Vector2(0.5f, 0f); irt.anchorMax = new Vector2(0.5f, 1f); irt.sizeDelta = new Vector2(fh * ar, 0f); }
                else { irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(1f, 0.5f); irt.sizeDelta = new Vector2(0f, fw / ar); irt.anchoredPosition = new Vector2(0f, fw / ar * 0.12f); }
                textLeft = coverW + 20f;
            }
            // 103차(사용자): 「컷씬 N」 → 「메인스토리 N」
            string label = e.Opening ? Loc.T("프롤로그 · 너와 나의 주파수", "Prologue · Our Frequency") : ending ? e.Label : isEv ? Loc.T($"서브스토리 · {e.Title}", $"Side story · {e.Title}") : Loc.T($"메인스토리 {e.Index} · {e.Title}", $"Main story {e.Index} · {e.Title}");   // 109차: 서브스토리는 번호 없이 제목만
            var t = CoastHudLayout.MakeText(card.rectTransform, "T", label, 21, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.40f), new Vector2(1f, 1f), new Vector2(textLeft, 0f), new Vector2(-16f, -top));
            t.color = Navy; t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(21);
            string subLine = e.Sub;   // 109차: 서브스토리 제목은 위 줄에만
            var s = CoastHudLayout.MakeText(card.rectTransform, "S", subLine, 12, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.40f), new Vector2(textLeft, 10f), new Vector2(-84f, 0f));
            s.color = Color.Lerp(fill, Color.black, 0.5f);
            s.resizeTextForBestFit = true; s.resizeTextMinSize = 9; s.resizeTextMaxSize = CoastHudLayout.Scaled(12);
            if (!e.Opening && !ending && e.Unlocked && (isEv ? StoryProgress.EventSeen(e.Event) : StoryProgress.CutsceneRead(e.Index)))
            {
                var ring = CoastHudLayout.MakeImage(card.rectTransform, "ReadRing", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -42f), new Vector2(-10f, -6f), new Color(0.25f, 0.70f, 0.40f));
                ring.sprite = CoastUiArt.RoundedRect(18); ring.type = Image.Type.Sliced; ring.raycastTarget = false;
                var chk = CoastHudLayout.MakeText(ring.rectTransform, "Read", "✓", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero);
                chk.color = Color.white; chk.fontStyle = FontStyle.Bold;
            }

            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var entry = e;
            btn.onClick.AddListener(() => Play(entry, onPlayStart));
            // 「보기」(재생) — 오른쪽 아래 파란 알약
            {
                var pb = CoastUiArt.GlossyPill(card.rectTransform, "PlayBtn", new Color(0.25f, 0.50f, 0.95f), 14, 4); pb.raycastTarget = true;
                var prt = pb.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(1f, 0f); prt.pivot = new Vector2(1f, 0f);
                prt.anchoredPosition = new Vector2(-10f, 8f); prt.sizeDelta = new Vector2(66f, 32f);
                var ptx = CoastHudLayout.MakeText(prt, "T", Loc.T("보기", "Play"), 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                ptx.color = Color.white; ptx.fontStyle = FontStyle.Bold;
                var pbb = pb.gameObject.AddComponent<Button>(); pbb.transition = Selectable.Transition.None;
                pbb.onClick.AddListener(() => Play(entry, onPlayStart));
            }
            // 68차: 카드 오른쪽 아래 작은 「읽기」(소설식 리더) — 보기 왼쪽
            if (!e.Opening && !ending && e.Unlocked)
            {
                var rb = CoastUiArt.GlossyPill(card.rectTransform, "ReadBtn", new Color(0.55f, 0.45f, 0.85f), 14, 4); rb.raycastTarget = true;
                var rrt = rb.rectTransform; rrt.anchorMin = rrt.anchorMax = new Vector2(1f, 0f); rrt.pivot = new Vector2(1f, 0f);
                rrt.anchoredPosition = new Vector2(-82f, 8f); rrt.sizeDelta = new Vector2(58f, 32f);
                var rt = CoastHudLayout.MakeText(rrt, "T", Loc.T("읽기", "Read"), 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                rt.color = Color.white; rt.fontStyle = FontStyle.Bold;
                var rbb = rb.gameObject.AddComponent<Button>(); rbb.transition = Selectable.Transition.None;
                rbb.onClick.AddListener(() => Read(entry, onPlayStart));
            }
            // 103차: 오른쪽 위 「▲ 접기」
            {
                var a = CoastHudLayout.MakeText(card.rectTransform, "Fold", Loc.T("▲", "▲"), 14, TextAnchor.MiddleCenter, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-84f, -40f), new Vector2(-52f, -6f));
                a.color = dark; a.fontStyle = FontStyle.Bold;
                var fb = a.gameObject.AddComponent<Button>(); fb.transition = Selectable.Transition.None;
                a.raycastTarget = true;
                var key = e.Key; fb.onClick.AddListener(() => { CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _expanded.Remove(key); Rebuild(); });
            }
        }

        private static void Play(Entry e, Action onPlayStart)
        {
            if (!e.Unlocked)
            {
                CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss);
                if (!string.IsNullOrEmpty(e.EndingId)) CoastToast.Show(Loc.T("아직 보지 못한 엔딩이에요 — 스토리를 끝까지 깨면 열려요", "An ending you haven't reached yet"));
                else CoastToast.Show(Loc.T($"챕터 {e.Chapter}에 닿으면 열려요", $"Reach chapter {e.Chapter} to unlock"));
                return;
            }
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            // 73차(사용자): 다 보고 나면 메인이 아니라 **이 시네마 선택 페이지로** 돌아온다(닫기 X 를 눌러야 메인).
            var back = Reopener();
            // 105차(사용자: 「한 컷씬 끝나면 우측 하단에 다음화 이어보기」): 목록 순서에서 다음으로 볼 편을 미리 골라 둔다.
            string nextLabel = null; Action nextPlay = null;
            if (TryNextPlayable(e, out var nx))
            {
                var entry = nx; var ps = onPlayStart;
                nextLabel = string.IsNullOrEmpty(entry.Title) ? entry.Label : entry.Label + " · " + entry.Title;
                nextPlay = () => Play(entry, ps);
            }
            Close();
            onPlayStart?.Invoke();
            if (e.Opening)
            {
                PlayerPrefs.SetInt("CoastRun_OpeningSeen", 1);
                OpeningCinematic.Play(back, nextLabel, nextPlay);
            }
            else if (!string.IsNullOrEmpty(e.EndingId))
            {
                CinematicPlayer.Play(e.EndingId, back, nextLabel, nextPlay);   // 77차: 엔딩 다시보기(시네마틱)
            }
            else if (e.Event > 0)
            {
                int ev = e.Event;   // 85차: 보조 컷씬
                CinematicPlayer.Play("EV" + ev, () => { StoryProgress.MarkEventSeen(ev); back(); },
                    nextLabel, nextPlay == null ? null : () => { StoryProgress.MarkEventSeen(ev); nextPlay(); });
            }
            else if (CinematicTable.Cutscene(e.Index) != null)
            {
                int idx = e.Index;   // 68차: 시네마틱
                CinematicPlayer.Play("CS" + idx, () => { StoryProgress.MarkCutsceneSeen(idx); back(); },
                    nextLabel, nextPlay == null ? null : () => { StoryProgress.MarkCutsceneSeen(idx); nextPlay(); });
            }
            else
            {
                StoryReaderUI.OpenCutscene(e.Index, back);
            }
        }

        /// 68차: 카드의 작은 「읽기」 — 소설식 리더(StoryReaderUI)로.
        private static void Read(Entry e, Action onPlayStart)
        {
            if (!e.Unlocked || e.Opening) return;
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            var back = Reopener();
            Close();
            onPlayStart?.Invoke();
            if (e.Event > 0) StoryReaderUI.OpenChapter(e.Chapter, back);
            else StoryReaderUI.OpenCutscene(e.Index, back);
        }

        /// 105차: 목록 순서에서 지금 보는 편 뒤에 있는, **열려 있고 시네마틱이 있는** 첫 항목.
        ///   잠긴 편·리더로만 읽는 편·프롤로그는 건너뛴다. 없으면 false — 그러면 이어보기 버튼이 안 뜬다.
        private static bool TryNextPlayable(Entry cur, out Entry next)
        {
            next = default;
            var list = _entries != null && _entries.Count > 0 ? _entries : BuildEntries(_gm);
            int at = list.FindIndex(x => x.Key == cur.Key);
            if (at < 0) return false;
            for (int i = at + 1; i < list.Count; i++)
            {
                var e = list[i];
                if (!e.Unlocked || e.Opening) continue;
                bool playable = !string.IsNullOrEmpty(e.EndingId)
                    || (e.Event > 0 && CinematicTable.Event(e.Event) != null)
                    || (e.Event == 0 && e.Index > 0 && CinematicTable.Cutscene(e.Index) != null);
                if (!playable) continue;
                next = e; return true;
            }
            return false;
        }

        /// 73차: 지금 열린 페이지의 인자를 붙들어 두었다가, 감상이 끝나면 같은 인자로 다시 연다.
        private static Action Reopener()
        {
            var gm = _gm; var ps = _onPlayStart; var oc = _onClose;
            return () => { _keepSeason = true; Open(gm, ps, oc); };
        }

        private static List<Entry> BuildEntries(GameManager gm)
        {
            var list = new List<Entry>();
            var openDef = CinematicTable.Get("OPEN"); float openLen = openDef != null ? openDef.Length : 148f;
            list.Add(new Entry { Label = Loc.T("프롤로그 영상", "Prologue film"), Title = Loc.T($"너와 나의 주파수 — {Mathf.FloorToInt(openLen / 60f)}:{Mathf.RoundToInt(openLen % 60f):00}", $"Our Frequency — {Mathf.FloorToInt(openLen / 60f)}:{Mathf.RoundToInt(openLen % 60f):00}"), Sub = Loc.T($"{Mathf.FloorToInt(openLen / 60f)}:{Mathf.RoundToInt(openLen % 60f):00} — 시네마틱 — 언제나 볼 수 있어요", $"{Mathf.FloorToInt(openLen / 60f)}:{Mathf.RoundToInt(openLen % 60f):00} — Cinematic — always available"), Opening = true, Unlocked = true, Chapter = 0, Cover = (openDef != null && openDef.cuts.Length > 0 ? ArtAssets.LoadTexture(openDef.cuts[0].still) ?? ArtAssets.LoadTexture(openDef.cuts[0].fallback) : null) ?? ArtAssets.LoadTexture("Cut_T_OP_01") ?? ArtAssets.LoadTexture("Cut_S_OP_1") ?? ArtAssets.LoadTexture("Cut_V_OP_1") });   // 103차: v7 오프닝 첫 컷
            var save = gm != null && gm.HasSave ? (gm.Save ?? gm.SaveSys.Load()) : null;
            bool all = gm != null && gm.DevUnlockAll;
            int reached = save != null ? Mathf.Clamp(save.chapter, 1, Timeline.Chapters) : 0;
            // 85차(대본 v4): 챕터 순서대로 메인스토리(CS)와 보조 컷씬(EV)을 섞어 나열 — 1 CS1 · 2 EV1 · 3 CS2 · 4 EV2 · 5 CS3 · 6 EV3 · 7 CS4 · 8 EV4 · 9 EV5 · 10 CS5 · 11 EV6 · 12 CS6 · 13 EV7 · 14 EV8 · 15 EV9 · 17 CS7 · 19 EV10 · 20 CS8
            for (int ch = 1; ch <= Timeline.Chapters; ch++)
            {
                int i = StoryProgress.CutsceneIndex(ch);
                if (i > 0)
                {
                    int first = StoryProgress.CutsceneFirstChapter(i);
                    var def = CinematicTable.Cutscene(i); float len = def != null ? def.Length : 0f;
                    list.Add(new Entry
                    {
                        Label = Loc.T($"메인스토리 {i}", $"Main story {i}"), Title = StoryProgress.CutsceneTitle(i),
                        Sub = Loc.T($"제 {ch}화 · {ChapterLocation.Get(ch).Name} · {Mathf.FloorToInt(len / 60f)}:{Mathf.RoundToInt(len % 60f):00}", $"Ch. {ch} · {ChapterLocation.Get(ch).Name} · {Mathf.FloorToInt(len / 60f)}:{Mathf.RoundToInt(len % 60f):00}"),
                        Index = i, Chapter = ch, Unlocked = all || reached >= ch || StoryProgress.CutsceneRead(i),
                        Cover = (def != null && def.cuts.Length > 0 ? ArtAssets.LoadTexture(def.cuts[0].still) ?? ArtAssets.LoadTexture(def.cuts[0].fallback) : null) ?? ArtAssets.LoadTexture($"Cut_S_N{i}_01") ?? ArtAssets.LoadTexture($"Cut_V_CH{ch:00}_Open") ?? ArtAssets.LoadTexture($"Cut_CH{first:00}_Open"),
                    });
                    continue;
                }
                int ev = StoryProgress.EventIndex(ch);
                if (ev > 0 && CinematicTable.Event(ev) != null)
                {
                    var def = CinematicTable.Event(ev);
                    list.Add(new Entry
                    {
                        Label = Loc.T("서브스토리", "Side story"), Title = def.title,   // 109차: 번호 없음
                        Sub = Loc.T($"{def.cuts.Length}컷 · {Mathf.RoundToInt(def.Length)}초 · {ChapterLocation.Get(ch).Name}", $"{def.cuts.Length} cuts · {Mathf.RoundToInt(def.Length)}s · {ChapterLocation.Get(ch).Name}"),
                        Event = ev, Chapter = ch, Unlocked = all || reached >= ch || StoryProgress.EventSeen(ev),
                        Cover = def.cuts.Length > 0 ? ArtAssets.LoadTexture(def.cuts[0].still) ?? ArtAssets.LoadTexture(def.cuts[0].fallback) : null,
                    });
                }
            }
            // 77차(사용자): 엔딩 3편 — 이미 깬 엔딩만 열리고(endingMask), 나머지는 제목도 안 보이는 회색 잠금
            // 79차(사용자): 「엔딩도 일단은 열어줘」 → OpenAllEndings = true 인 동안은 셋 다 열어 둔다.
            //               잠금 규칙을 되살리려면 이 상수만 false 로. (비밀코드 devUnlockAll 로는 여전히 안 열린다)
            var prof = gm != null ? gm.Profile : null;
            int mask = prof != null ? prof.endingMask : 0;
            bool sawA = OpenAllEndings || (mask & 0b111) != 0, sawB = OpenAllEndings || (mask & 0b111000) != 0, sawT = OpenAllEndings || (mask & (1 << 6)) != 0;
            list.Add(EndingEntry("END_A", 1, sawA));
            list.Add(EndingEntry("END_B", 2, sawB));
            list.Add(EndingEntry("END_TRUE", 3, sawT));
            return list;
        }

        private static Entry EndingEntry(string id, int n, bool unlocked)
        {
            var def = CinematicTable.Get(id);
            string title = def != null ? def.title : id;
            float len = def != null ? def.Length : 0f;
            return new Entry
            {
                Label = n == 1 ? Loc.T("엔딩 A", "Ending A") : n == 2 ? Loc.T("엔딩 B", "Ending B") : Loc.T("진엔딩", "True ending"), Title = title,
                Sub = unlocked ? Loc.T($"시네마틱 · {Mathf.FloorToInt(len / 60f)}:{Mathf.RoundToInt(len % 60f):00}", $"Cinematic · {Mathf.FloorToInt(len / 60f)}:{Mathf.RoundToInt(len % 60f):00}") : Loc.T("잠김 · 이 엔딩을 보면 열려요", "Locked · reach this ending"),
                EndingId = id, Unlocked = unlocked, Chapter = 20,
                Cover = unlocked && def != null && def.cuts.Length > 0 ? (ArtAssets.LoadTexture(def.cuts[0].still) ?? ArtAssets.LoadTexture(def.cuts[0].fallback)) : null,
            };
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _content = null;
        }

        public static bool IsOpen => _canvas != null;
    }
}
