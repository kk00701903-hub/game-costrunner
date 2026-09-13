using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 48차-14: 뮤직 컬렉션(레코드) 화면 — 사용자 시안(UI_Music_Mock, 1056×2208 좌우 93px 확장 → 1080×1920)을 배경으로 깔고
    /// 7곡 행의 제목·부제·PLAY·잠금·수집 수·K-POP 전체재생만 실제 데이터로 얹는다. 시안 px → 디자인(720×1280) 변환 MM().
    public partial class CollectionUI
    {
        private GameObject _mockRec;
        private Coroutine _playAll;

        private static Vector2 MM(float mx, float my) => new Vector2((mx + 93f) * (720f / 1242f) - 360f, 640f - my * (1280f / 2208f));
        private static RectTransform MMRect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = MM((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            rt.sizeDelta = new Vector2((x1 - x0) * (720f / 1242f), (y1 - y0) * (1280f / 2208f));
            return rt;
        }

        private void ShowMockRecords()
        {
            if (_mockRec != null) Destroy(_mockRec);
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            var root = new GameObject("MockRecords", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(_root, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f); root.sizeDelta = new Vector2(720f, 1280f);
            _mockRec = root.gameObject;
            var bg = CoastHudLayout.MakeImage(root, "Bg", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.55f, 0.45f, 0.70f));
            bg.raycastTarget = true;
            var art = CoastHudLayout.MakeImage(root, "Art", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var sp = CoastUiArt.Art("UI_Music_Mock"); if (sp != null) art.sprite = sp; else art.color = new Color(0.62f, 0.52f, 0.78f);
            art.raycastTarget = true;

            // 52차(사용자): 「7/7」 수집 수 삭제 → 그 자리(COLLECTED 배지)를 「AI 생성 음원」 표시 필로 덮는다.
            int owned = RecordTable.UnlockedCount(p);
            var aiRt = MMRect(root, "AiTag", 596f, 410f, 910f, 494f);
            var aiPill = CoastUiArt.CutePill(aiRt, "Pill", new Color(0.99f, 0.95f, 0.84f), 16, 3); aiPill.raycastTarget = false;
            Stretch(aiPill.rectTransform);
            var aiL = CoastOrnate.Label(aiPill.transform, "T", Loc.T("✦ AI 생성 음원 · 가상 듀오 우히&히시", "✦ AI-generated music · virtual duo"), 12, new Color(0.45f, 0.28f, 0.12f)); aiL.fontStyle = FontStyle.Bold;
            Stretch(aiL.rectTransform);

            // 52차(사용자): 홈으로 가기 버튼. 72차(사용자): 「돌아가기」는 없애고 홈 하나로 — 시안에 박힌 좌상단 「<」 동그라미 자리를
            //   파란 둥근 홈 버튼(새 집 아이콘)으로 덮는다(제목 글자와 안 겹치는 유일한 빈 자리).
            var homeRt = MMRect(root, "Home", 26f, 36f, 156f, 166f);
            var homePill = CoastUiArt.GlossyPill(homeRt, "Pill", new Color(0.30f, 0.55f, 0.95f), 38, 7);
            Stretch(homePill.rectTransform);
            var homeIc = CoastUiArt.Art("Icon_Home");
            if (homeIc != null)
            {
                var hi = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                hi.transform.SetParent(homePill.transform, false); hi.sprite = homeIc; hi.preserveAspect = true; hi.raycastTarget = false;
                var hrt = hi.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f); hrt.anchoredPosition = new Vector2(0f, 3f); hrt.sizeDelta = new Vector2(40f, 40f);
            }
            else { var homeL = CoastOrnate.Label(homePill.transform, "T", "⌂", 26, Color.white); homeL.fontStyle = FontStyle.Bold; Stretch(homeL.rectTransform); }
            var homeBtn = homePill.gameObject.AddComponent<Button>(); homeBtn.transition = Selectable.Transition.None;
            homeBtn.onClick.AddListener(() => { CoastPrefs.Vibrate(); StopPlayAll(); Close(); });

            // 7곡 행 — 시안 x 147~920, y0 = 500 + 203.3·i, 높이 167
            for (int i = 0; i < RecordTable.All.Length && i < 7; i++)
            {
                var t = RecordTable.All[i];
                bool has = RecordTable.IsUnlocked(p, t);
                bool playing = _playingNum == t.num;
                float y0 = 500f + 203.3f * i;
                var row = MMRect(root, "Row" + t.num, 147f, y0, 920f, y0 + 167f);
                var hit = row.gameObject.AddComponent<Image>(); hit.color = new Color(1f, 1f, 1f, 0f); hit.raycastTarget = true;
                // 제목·부제 (시안 292~684 × y0+22 ~ y0+150)
                var textRt = MMRect(root, "Txt" + t.num, 296f, y0 + 24f, 684f, y0 + 150f);
                string title = has ? (Loc.IsKo ? t.ko : t.en) : "???";
                var name = CoastOrnate.Label(textRt, "T", title, title.Length > 12 ? 15 : 21, has ? new Color(0.22f, 0.14f, 0.30f) : new Color(0.45f, 0.40f, 0.52f), TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold; name.horizontalOverflow = HorizontalWrapMode.Overflow; name.verticalOverflow = VerticalWrapMode.Overflow;
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(0f, 44f));
                name.rectTransform.offsetMax = new Vector2(-6f, name.rectTransform.offsetMax.y);
                string sub = has ? "♪ " + (Loc.IsKo ? t.noteKo : t.noteEn) : Loc.T("잠김 · ", "Locked · ") + (Loc.IsKo ? t.unlockKo : t.unlockEn);
                var subL = CoastOrnate.Label(textRt, "S", sub, 13, has ? new Color(0.42f, 0.34f, 0.50f) : new Color(0.55f, 0.48f, 0.60f), TextAnchor.LowerLeft);
                Place(subL.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(0f, 22f));
                subL.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (has && RecordTable.IsNew(p, t))
                {
                    var n = CoastUiArt.Panel(textRt, "New", CoastOrnate.Red, 8); n.raycastTarget = false;
                    Place(n.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -4f), new Vector2(36f, 15f));
                    CoastOrnate.Label(n.transform, "T", "NEW", 10, Color.white);
                }
                // PLAY 버튼(시안 693~887 × y0+45 ~ y0+125): 재생 중이면 주황 ■ 로 덮고, 잠기면 회색으로 덮는다
                var btn = MMRect(root, "Play" + t.num, 690f, y0 + 40f, 890f, y0 + 130f);
                if (playing || !has)
                {
                    var cover = CoastUiArt.CutePill(btn, "Cover", playing ? new Color(1f, 0.55f, 0.32f) : new Color(0.62f, 0.60f, 0.68f), 14, 3); cover.raycastTarget = false;
                    Place(cover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, btn.sizeDelta - new Vector2(6f, 10f));
                    var cl = CoastOrnate.Label(cover.transform, "T", playing ? "■  STOP" : Loc.T("잠김", "Locked"), 15, playing ? new Color(0.4f, 0.15f, 0.05f) : new Color(1f, 1f, 1f, 0.9f)); cl.fontStyle = FontStyle.Bold;
                }
                if (!has)
                {
                    var dim = CoastUiArt.Panel(row, "Dim", new Color(0.35f, 0.30f, 0.45f, 0.35f), 22); dim.raycastTarget = false;
                    Stretch(dim.rectTransform);
                }
                var tr = t; bool h = has;
                var b = row.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    CoastPrefs.Vibrate();
                    if (!h) { Toast(Loc.T("잠김 — ", "Locked — ") + (Loc.IsKo ? tr.unlockKo : tr.unlockEn)); return; }
                    StopPlayAll(); ToggleRecord(tr); ShowMockRecords();
                });
            }

            // 52차: 기부 선물 ① 히든 트랙 — 7행 아래 얇은 금색 띠(시안 y 1893~1943). 탭하면 M9/M10 번갈아 재생.
            if (RecordTable.HiddenOpen)
            {
                var hRt = MMRect(root, "Hidden", 147f, 1893f, 920f, 1943f);
                var hPill = CoastUiArt.GlossyPill(hRt, "Pill", new Color(1f, 0.84f, 0.35f), 14, 4); hPill.raycastTarget = true;
                Stretch(hPill.rectTransform);
                bool hp = _playingNum == 9 || _playingNum == 10;
                string hLabel = hp ? Loc.T($"■  히든 트랙 {(_playingNum == 9 ? 1 : 2)} 재생 중", $"■  Hidden track {(_playingNum == 9 ? 1 : 2)} playing") : Loc.T("★ 히든 트랙 2곡 (기부 선물)  ▶", "★ 2 hidden tracks (donor gift)  ▶");
                var hl = CoastOrnate.Label(hPill.transform, "T", hLabel, 14, new Color(0.40f, 0.22f, 0.05f)); hl.fontStyle = FontStyle.Bold;
                Stretch(hl.rectTransform);
                var hb = hPill.gameObject.AddComponent<Button>(); hb.transition = Selectable.Transition.None;
                hb.onClick.AddListener(() =>
                {
                    CoastPrefs.Vibrate(); StopPlayAll();
                    var next = _playingNum == 9 ? RecordTable.Hidden[1] : _playingNum == 10 ? null : RecordTable.Hidden[0];
                    if (next == null) { _preview.Stop(); _playingNum = 0; ShowMockRecords(); return; }
                    if (_playingNum != 0) { _preview.Stop(); _playingNum = 0; }
                    ToggleRecord(next); ShowMockRecords();
                });
            }

            // K-POP 전체재생 (시안 105~950 × 1948~2130)
            bool all = _playAllOn;
            var allRt = MMRect(root, "PlayAll", 105f, 1948f, 950f, 2130f);
            if (all)
            {
                var cover = CoastUiArt.GlossyPill(allRt, "Cover", new Color(1f, 0.55f, 0.32f), 26, 10); cover.raycastTarget = false;
                Place(cover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, allRt.sizeDelta - new Vector2(10f, 10f));
                var cl = CoastOrnate.Label(cover.transform, "T", Loc.T("■  전체재생 중지", "■  Stop playlist"), 26, new Color(0.4f, 0.15f, 0.05f)); cl.fontStyle = FontStyle.Bold;
                var cs = CoastOrnate.Label(cover.transform, "S", Loc.T($"{_playAllIdx + 1}/{RecordTable.All.Length} · 지금 {_playAllTitle}", $"{_playAllIdx + 1}/{RecordTable.All.Length} · now {_playAllTitle}"), 13, new Color(0.45f, 0.2f, 0.05f));
                Place(cs.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 20f));
            }
            var allHit = allRt.gameObject.AddComponent<Image>(); allHit.color = new Color(1f, 1f, 1f, 0f); allHit.raycastTarget = true;
            var ab = allRt.gameObject.AddComponent<Button>(); ab.transition = Selectable.Transition.None;
            ab.onClick.AddListener(() =>
            {
                CoastPrefs.Vibrate();
                if (_playAllOn) { StopPlayAll(); _preview.Stop(); _playingNum = 0; ShowMockRecords(); return; }
                if (owned == 0) { Toast(Loc.T("아직 열린 곡이 없어요", "No tracks unlocked yet")); return; }
                _playAllOn = true; _playAll = StartCoroutine(PlayAllCo());
            });
            // 72차: 옛 「뒤로」 투명 히트 영역 삭제 — 같은 자리에 홈 버튼이 있다.
        }

        private int _playAllIdx; private string _playAllTitle = ""; private bool _playAllOn;
        private void StopPlayAll() { _playAllOn = false; if (_playAll != null) { StopCoroutine(_playAll); _playAll = null; } }

        /// 열린 곡을 1번부터 차례로 이어서 재생(K-POP 전체재생)
        private IEnumerator PlayAllCo()
        {
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            for (int i = 0; i < RecordTable.All.Length; i++)
            {
                var t = RecordTable.All[i];
                if (!RecordTable.IsUnlocked(p, t)) continue;
                var clip = CoastBgmLibrary.Load(t.Clip);
                if (clip == null) continue;
                _preview.Stop(); _preview.clip = clip; _preview.time = 0f; _preview.loop = false; _preview.Play();
                _playingNum = t.num; _playAllIdx = i; _playAllTitle = Loc.IsKo ? t.ko : t.en;
                RecordTable.MarkSeen(p, t);
                CoastAudioManager.Instance?.SetBedMuted(true);
                ShowMockRecords();
                while (_preview != null && _preview.isPlaying) yield return null;
                if (_preview == null) yield break;
            }
            _playAll = null; _playAllOn = false; _playingNum = 0;
            ShowMockRecords();
        }
    }
}
