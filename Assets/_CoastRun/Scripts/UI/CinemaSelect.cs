using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 42차: 타이틀 더보기 › 「시네마」 — 컷씬을 골라 다시 본다.
    /// 목록 = 오프닝 영상 + 챕터별 오프닝/클로징 VN(ChapterScript 에 대본이 있는 것만).
    /// 해금: 오프닝은 항상. 챕터 N 오프닝은 세이브 챕터 ≥ N(그 챕터에 들어갔으면), 클로징은 그 챕터를 클리어했으면.
    /// 세이브가 없으면 오프닝만. 비밀코드(devUnlockAll)면 전부. 39차 KpopChapterSelect 와 같은 룩(딤 + 카드 + 우상단 X).
    public static class CinemaSelect
    {
        private static Canvas _canvas;
        private static Action _onClose;

        private struct Entry
        {
            public string Label, Sub, VnId;
            public bool Opening, Unlocked;
            public int Chapter;
        }

        private static readonly Color[] SeasonFill =
        {
            new Color(1f, 0.90f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.65f, 0.35f), new Color(0.72f, 0.62f, 0.95f),
        };
        private static readonly Color Navy = new Color(0.10f, 0.13f, 0.30f);
        private static readonly Color Locked = new Color(0.66f, 0.66f, 0.70f);

        /// onPlayStart: 컷씬 재생 직전(타이틀 음악 정지 등). onClose: 페이지를 닫거나 컷씬이 끝나 돌아왔을 때.
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
            var sub = CoastHudLayout.MakeText(root, "Sub", Loc.T("다시 보고 싶은 장면을 고르세요", "Pick a scene to watch again"), 13, TextAnchor.MiddleCenter,
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

            // 스크롤 목록 — 항목 1줄 = 카드(오프닝은 큰 카드 1장, 챕터는 [오프닝][클로징] 2장)
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

            const float rowH = 96f, gap = 12f;
            float y = 0f;
            float w = 664f - 40f;
            // 오프닝 카드(한 줄 가득)
            var op = entries[0];
            MakeCard(content, op, new Vector2(0f, -y), new Vector2(w, rowH), gm, onPlayStart);
            y += rowH + gap;
            // 챕터 줄: 왼쪽 라벨 + [오프닝] [클로징]
            for (int c = 1; c <= Timeline.Chapters; c++)
            {
                Entry? open = null, close = null;
                foreach (var e in entries) { if (e.Chapter != c || e.Opening) continue; if (e.VnId == ChapterScript.OpenId(c)) open = e; else if (e.VnId == ChapterScript.CloseId(c)) close = e; }
                if (open == null && close == null) continue;
                var lab = CoastHudLayout.MakeText(content, "Ch" + c, Loc.T($"챕터 {c}", $"Ch. {c}"), 20, TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -y - rowH), new Vector2(150f, -y));
                lab.color = new Color(1f, 0.92f, 0.70f); CoastUiArt.OutlineText(lab, new Color(0f, 0f, 0f, 0.6f), 1.2f);
                var sm = CoastHudLayout.MakeText(content, "ChT" + c, ChapterScript.Title(c), 11, TextAnchor.LowerLeft,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -y - rowH + 6f), new Vector2(160f, -y - rowH * 0.5f));
                sm.color = new Color(0.85f, 0.88f, 0.95f); sm.horizontalOverflow = HorizontalWrapMode.Wrap;
                float cx = 160f; float cw = (w - cx - gap) * 0.5f;
                if (open != null) MakeCard(content, open.Value, new Vector2(cx, -y), new Vector2(cw, rowH), gm, onPlayStart);
                if (close != null) MakeCard(content, close.Value, new Vector2(cx + cw + gap, -y), new Vector2(cw, rowH), gm, onPlayStart);
                y += rowH + gap;
            }
            content.sizeDelta = new Vector2(0f, y + 20f);
        }

        private static void MakeCard(RectTransform parent, Entry e, Vector2 pos, Vector2 size, GameManager gm, Action onPlayStart)
        {
            var fill = e.Unlocked ? (e.Opening ? new Color(1f, 0.55f, 0.65f) : SeasonFill[(Mathf.Clamp(e.Chapter, 1, 20) - 1) / 5]) : Locked;
            var card = CoastUiArt.GlossyPill(parent, "Card_" + (e.Opening ? "Opening" : e.VnId), fill, 16, 9);
            var crt = card.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = pos; crt.sizeDelta = size;
            card.raycastTarget = true;
            card.color = Color.Lerp(fill, Color.black, 0.62f);

            var t = CoastHudLayout.MakeText(crt, "T", e.Label, e.Opening ? 26 : 20, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, -6f));
            t.color = e.Unlocked ? Navy : new Color(0.33f, 0.33f, 0.38f);
            var s = CoastHudLayout.MakeText(crt, "S", e.Unlocked ? e.Sub : Loc.T("잠김", "Locked"), 11, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 10f), new Vector2(0f, 4f));
            s.color = e.Unlocked ? Color.Lerp(fill, Color.black, 0.45f) : new Color(0.30f, 0.30f, 0.36f);

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
                CoastToast.Show(e.Opening ? "" : (e.VnId == ChapterScript.OpenId(e.Chapter)
                    ? Loc.T($"챕터 {e.Chapter}에 들어가면 볼 수 있어요", $"Reach chapter {e.Chapter} to unlock")
                    : Loc.T($"챕터 {e.Chapter}를 클리어하면 볼 수 있어요", $"Clear chapter {e.Chapter} to unlock")));
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
                ChapterVN.HoldBlackOnNext = false;   // 다시보기: 러너로 안 넘어간다
                string card = $"CHAPTER {e.Chapter}\n「{ChapterScript.Title(e.Chapter)}」\n<size=18>{ChapterLocation.Get(e.Chapter).Name}</size>";
                ChapterVN.Play(e.VnId, () => cb?.Invoke(), e.VnId == ChapterScript.OpenId(e.Chapter) ? card : null);
            }
        }

        private static List<Entry> BuildEntries(GameManager gm)
        {
            var list = new List<Entry>();
            list.Add(new Entry { Label = Loc.T("오프닝 영상", "Opening"), Sub = Loc.T("우리의 송전탑 — 프롤로그", "Our Pylon — Prologue"), Opening = true, Unlocked = true, Chapter = 0 });
            var save = gm != null && gm.HasSave ? (gm.Save ?? gm.SaveSys.Load()) : null;
            bool all = gm != null && gm.DevUnlockAll;
            int reached = save != null ? Mathf.Clamp(save.chapter, 1, Timeline.Chapters) : 0;
            for (int c = 1; c <= Timeline.Chapters; c++)
            {
                string oid = ChapterScript.OpenId(c), cid = ChapterScript.CloseId(c);
                bool cleared = save != null && save.chapters != null && c - 1 < save.chapters.Length && save.chapters[c - 1] != null && save.chapters[c - 1].cleared;
                if (ChapterScript.Has(oid))
                    list.Add(new Entry { Label = Loc.T("오프닝", "Opening"), Sub = ChapterLocation.Get(c).Name, VnId = oid, Chapter = c, Unlocked = all || c <= reached });
                if (ChapterScript.Has(cid))
                    list.Add(new Entry { Label = Loc.T("클로징", "Closing"), Sub = ChapterLocation.Get(c).Name, VnId = cid, Chapter = c, Unlocked = all || cleared || c < reached });
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
