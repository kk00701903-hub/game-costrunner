using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 42차: 타이틀 더보기 › 「시네마」 — 컷씬을 골라 다시 본다.
    /// 61차(사용자): 목록 = 프롤로그 영상 + 컷씬 8개(러닝 챕터 1·4·7·10·13·15·18·20 마다 하나, 오프닝/클로징 구분 없음).
    /// 해금: 프롤로그는 항상, 컷씬 N 은 그 챕터에 닿았으면(또는 읽었으면). 비밀코드(devUnlockAll)면 전부.
    public static class CinemaSelect
    {
        private static Canvas _canvas;
        private static Action _onClose;

        private struct Entry
        {
            public string Label, Sub, Title;
            public bool Opening, Unlocked;
            public int Index, Chapter;   // Index = 컷씬 번호(1..8), Chapter = 그 컷씬이 열리는 챕터
            public Texture2D Cover;
        }

        private static readonly Color[] SeasonFill =
        {
            new Color(1f, 0.90f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.65f, 0.35f), new Color(0.72f, 0.62f, 0.95f),
        };
        private static readonly Color Navy = new Color(0.10f, 0.13f, 0.30f);
        private static readonly Color Locked = new Color(0.66f, 0.66f, 0.70f);

        /// onPlayStart: 컷씬 재생 직전(타이틀 음악 정지 등). onClose: 페이지를 닫거나 컷씬이 끝나 돌아왔을 때.
        /// 61차(사용자): 목록 = 프롤로그 영상 + **컷씬 8개**(오프닝/클로징 구분 없이) — 각 컷씬은 리더(StoryReaderUI.OpenCutscene)로 읽는다.
        public static void Open(GameManager gm, Action onPlayStart, Action onClose)
        {
            Close();
            _onClose = onClose;
            var entries = BuildEntries(gm);

            _canvas = CoastUiCanvas.Create("CinemaSelect", 320);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;

            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.03f, 0.05f, 0.14f, 0.72f));
            dim.raycastTarget = true;

            var title = CoastHudLayout.MakeText(root, "Title", Loc.T("시네마", "CINEMA"), 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -30f));
            title.color = new Color(1f, 0.85f, 0.30f);
            CoastUiArt.OutlineText(title, new Color(0.30f, 0.12f, 0.02f, 0.9f), 2.5f);
            int read = 0; foreach (var e in entries) if (!e.Opening && e.Unlocked && StoryProgress.CutsceneRead(e.Index)) read++;
            var sub = CoastHudLayout.MakeText(root, "Sub", Loc.T($"컷씬 {StoryProgress.CutsceneCount}개 · 읽은 것 {read}개 — 다시 읽고 싶은 컷씬을 고르세요", $"{StoryProgress.CutsceneCount} cutscenes · {read} read — pick one to read again"), 13, TextAnchor.MiddleCenter,
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
            xb.onClick.AddListener(() => { var cb = _onClose; Close(); cb?.Invoke(); });

            // 스크롤 목록 — 한 줄 = 카드 하나(왼쪽 표지 그림 + 컷씬 번호·제목·화 범위)
            var viewGo = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewGo.transform.SetParent(root, false);
            var vrt = viewGo.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(20f, 40f); vrt.offsetMax = new Vector2(-20f, -166f);
            var vimg = viewGo.GetComponent<Image>(); vimg.color = new Color(0f, 0f, 0f, 0.001f); vimg.raycastTarget = true;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(vrt, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var scroll = viewGo.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = vrt; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f; scroll.inertia = true;

            const float rowH = 108f, gap = 12f;
            float y = 0f;
            float w = 664f - 40f;
            foreach (var e in entries)
            {
                MakeCard(content, e, new Vector2(0f, -y), new Vector2(w, rowH), gm, onPlayStart);
                y += rowH + gap;
            }
            content.sizeDelta = new Vector2(0f, y + 20f);
        }

        private static void MakeCard(RectTransform parent, Entry e, Vector2 pos, Vector2 size, GameManager gm, Action onPlayStart)
        {
            var fill = e.Unlocked ? (e.Opening ? new Color(1f, 0.55f, 0.65f) : SeasonFill[(Mathf.Clamp(e.Chapter, 1, 20) - 1) / 5]) : Locked;
            var card = CoastUiArt.GlossyPill(parent, "Card_" + (e.Opening ? "Prologue" : "Cut" + e.Index), fill, 16, 9);
            var crt = card.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = pos; crt.sizeDelta = size;
            card.raycastTarget = true;
            card.color = Color.Lerp(fill, Color.black, 0.62f);

            // 왼쪽 표지(있으면) — 둥근 상자 안에 가득 채워 자르기
            float textLeft = 20f;
            if (e.Cover != null)
            {
                var frame = new GameObject("Cover", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).GetComponent<RectTransform>();
                frame.SetParent(crt, false); frame.anchorMin = new Vector2(0f, 0f); frame.anchorMax = new Vector2(0f, 1f); frame.pivot = new Vector2(0f, 0.5f);
                frame.anchoredPosition = new Vector2(10f, 0f); frame.sizeDelta = new Vector2(150f, -16f);
                var fimg = frame.GetComponent<Image>(); fimg.sprite = CoastUiArt.RoundedRect(12); fimg.type = Image.Type.Sliced; fimg.color = new Color(0f, 0f, 0f, 0.35f); fimg.raycastTarget = false;
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(frame, false); im.sprite = CoastUiArt.AsSprite(e.Cover, 100f); im.raycastTarget = false; im.preserveAspect = false;
                im.color = e.Unlocked ? Color.white : new Color(0.45f, 0.45f, 0.5f);
                var irt = im.rectTransform; float ar = e.Cover.width / (float)e.Cover.height; float fw = 150f, fh = size.y - 16f;
                if (ar > fw / fh) { irt.anchorMin = new Vector2(0.5f, 0f); irt.anchorMax = new Vector2(0.5f, 1f); irt.sizeDelta = new Vector2(fh * ar, 0f); }
                else { irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(1f, 0.5f); irt.sizeDelta = new Vector2(0f, fw / ar); irt.anchoredPosition = new Vector2(0f, fw / ar * 0.12f); }
                textLeft = 176f;
            }
            var t = CoastHudLayout.MakeText(crt, "T", e.Label, 22, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.62f), new Vector2(1f, 1f), new Vector2(textLeft, -8f), new Vector2(-16f, -6f));
            t.color = e.Unlocked ? Navy : new Color(0.33f, 0.33f, 0.38f); t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            var tt = CoastHudLayout.MakeText(crt, "Title", e.Unlocked ? e.Title : Loc.T("잠김", "Locked"), 18, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.32f), new Vector2(1f, 0.62f), new Vector2(textLeft, 0f), new Vector2(-16f, 0f));
            tt.color = e.Unlocked ? Color.Lerp(fill, Color.black, 0.55f) : new Color(0.30f, 0.30f, 0.36f); tt.fontStyle = FontStyle.Bold;
            tt.resizeTextForBestFit = true; tt.resizeTextMinSize = 10; tt.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            var s = CoastHudLayout.MakeText(crt, "S", e.Sub, 12, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.32f), new Vector2(textLeft, 8f), new Vector2(-16f, 0f));
            s.color = e.Unlocked ? Color.Lerp(fill, Color.black, 0.45f) : new Color(0.30f, 0.30f, 0.36f);
            s.resizeTextForBestFit = true; s.resizeTextMinSize = 9; s.resizeTextMaxSize = CoastHudLayout.Scaled(12);
            if (!e.Opening && e.Unlocked && StoryProgress.CutsceneRead(e.Index))
            {
                var chk = CoastHudLayout.MakeText(crt, "Read", "✓", 26, TextAnchor.MiddleCenter, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -16f), new Vector2(-12f, 16f));
                chk.color = new Color(0.20f, 0.60f, 0.35f); chk.fontStyle = FontStyle.Bold;
            }

            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var entry = e;
            btn.onClick.AddListener(() => Play(entry, onPlayStart));
        }

        private static void Play(Entry e, Action onPlayStart)
        {
            if (!e.Unlocked)
            {
                CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss);
                CoastToast.Show(Loc.T($"챕터 {e.Chapter}에 닿으면 열려요", $"Reach chapter {e.Chapter} to unlock"));
                return;
            }
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            var cb = _onClose;
            Close();
            onPlayStart?.Invoke();
            if (e.Opening)
            {
                PlayerPrefs.SetInt("CoastRun_OpeningSeen", 1);
                OpeningCinematic.Play(() => cb?.Invoke());
            }
            else
            {
                StoryReaderUI.OpenCutscene(e.Index, () => cb?.Invoke());
            }
        }

        private static List<Entry> BuildEntries(GameManager gm)
        {
            var list = new List<Entry>();
            list.Add(new Entry { Label = Loc.T("프롤로그 영상", "Prologue film"), Title = Loc.T("너와 나의 주파수 — 1:37", "Our Frequency — 1:37"), Sub = Loc.T("영상 · 언제나 볼 수 있어요", "Film · always available"), Opening = true, Unlocked = true, Chapter = 0, Cover = ArtAssets.LoadTexture("Cut_CH01_Open") });
            var save = gm != null && gm.HasSave ? (gm.Save ?? gm.SaveSys.Load()) : null;
            bool all = gm != null && gm.DevUnlockAll;
            int reached = save != null ? Mathf.Clamp(save.chapter, 1, Timeline.Chapters) : 0;
            for (int i = 1; i <= StoryProgress.CutsceneCount; i++)
            {
                int ch = StoryProgress.CutsceneChapter(i), first = StoryProgress.CutsceneFirstChapter(i);
                string range = first == ch ? Loc.T($"제 {ch}화", $"Ch. {ch}") : Loc.T($"제 {first}~{ch}화", $"Ch. {first}–{ch}");
                list.Add(new Entry
                {
                    Label = Loc.T($"컷씬 {i}", $"Cutscene {i}"), Title = StoryProgress.CutsceneTitle(i),
                    Sub = range + " · " + ChapterLocation.Get(ch).Name,
                    Index = i, Chapter = ch, Unlocked = all || reached >= ch || StoryProgress.CutsceneRead(i),
                    Cover = ArtAssets.LoadTexture($"Cut_CH{ch:00}_Open") ?? ArtAssets.LoadTexture($"Cut_CH{first:00}_Open"),
                });
            }
            return list;
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }

        public static bool IsOpen => _canvas != null;
    }
}
