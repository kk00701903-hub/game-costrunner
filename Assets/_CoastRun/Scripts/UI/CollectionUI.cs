using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 컬렉션 오버레이 — 레코드(20트랙) · 포토카드(30장) · 팬아트. 타이틀·육성 어디서든 `CollectionUI.Open()`.
    /// 앨범 미구매면 봄(1~5) 밖의 항목은 잠금 표시 + 구매 패널. 결제 자체는 IapBridge가 담당(지금은 테스트 언락).
    public class CollectionUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static CollectionUI _active;

        public static void Open(Action onClose = null, int tab = 0)
        {
            if (_active != null) return;
            var go = new GameObject("CollectionUI");
            DontDestroyOnLoad(go);
            _active = go.AddComponent<CollectionUI>();
            _active._onClose = onClose;
            _active._tab = tab;
            _active.Build();
        }

        private Action _onClose;
        private Canvas _canvas;
        private RectTransform _root, _content;
        private int _tab;                       // 0 레코드 / 1 포토카드 / 2 팬아트 / 3 트로피
        private Button[] _tabBtns = new Button[4];
        private AudioSource _preview;
        private GameObject _detail;             // 카드 상세 / 트랙 상세
        private int _cardPage;

        private static readonly Color Ink = new Color(0.16f, 0.12f, 0.10f);
        private static readonly Color Paper = new Color(0.98f, 0.95f, 0.88f, 0.96f);

        private void Build()
        {
            IsOpen = true;
            _canvas = CoastUiCanvas.Create("CollectionCanvas", 460);
            DontDestroyOnLoad(_canvas.gameObject);
            _root = CoastUiCanvas.Root(_canvas);

            var pad = CoastUiCanvas.HudPad;
            // 38차: 시안(밤하늘 보라 + 별·네온) — 어두운 남보라 → 보라 그라데이션, 반짝이 점, 아래에 네온 파형 띠
            var bg = CoastHudLayout.MakeImage(_root, "Bg", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.11f, 0.07f, 0.26f, 1f));
            bg.raycastTarget = true;
            var glowTop = CoastHudLayout.MakeImage(_root, "GlowTop", new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(-pad - 400f, 0f), new Vector2(pad + 400f, pad + 400f), new Color(0.30f, 0.16f, 0.52f, 0.55f));
            glowTop.raycastTarget = false;
            var glowBot = CoastHudLayout.MakeImage(_root, "GlowBot", new Vector2(0f, 0f), new Vector2(1f, 0.22f), new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, 0f), new Color(0.42f, 0.18f, 0.55f, 0.45f));
            glowBot.raycastTarget = false;
            var rng = new System.Random(7);
            for (int i = 0; i < 46; i++)
            {
                float sz = 3f + (float)rng.NextDouble() * 6f;
                var star = CoastUiArt.Panel(_root, "Star" + i, new Color(1f, 0.95f, 0.75f, 0.35f + (float)rng.NextDouble() * 0.5f), 3);
                star.raycastTarget = false;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2((float)rng.NextDouble(), 0.12f + (float)rng.NextDouble() * 0.86f);
                star.rectTransform.sizeDelta = new Vector2(sz, sz);
                star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            // 네온 파형 띠(아래)
            Color[] neon = { new Color(1f, 0.35f, 0.6f), new Color(0.4f, 0.9f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.7f, 0.5f, 1f) };
            for (int i = 0; i < 40; i++)
            {
                float h = 10f + Mathf.Abs(Mathf.Sin(i * 0.9f)) * 34f + (float)rng.NextDouble() * 10f;
                var bar = CoastUiArt.Panel(_root, "Eq" + i, new Color(neon[i % 4].r, neon[i % 4].g, neon[i % 4].b, 0.55f), 3);
                bar.raycastTarget = false;
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                bar.rectTransform.pivot = new Vector2(0.5f, 0f);
                bar.rectTransform.anchoredPosition = new Vector2(-340f + i * 17.5f, 100f);
                bar.rectTransform.sizeDelta = new Vector2(8f, h);
            }

            // 헤더: 제목(노란 크림, 짙은 테두리) + AI 태그
            var head = CoastOrnate.Label(_root, "Head", Loc.T("제주 · 너와 나의 주파수", "JEJU · Our Frequency"), 40, new Color(1f, 0.93f, 0.62f));
            head.fontStyle = FontStyle.Bold;
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(0f, 60f));
            CoastUiArt.OutlineText(head, new Color(0.35f, 0.12f, 0.45f, 1f), 3f);
            var tag = CoastOrnate.Label(_root, "Tag", "✦ " + AlbumTable.ArtistTag + " · " + Loc.T("컬렉션", "Collection"), 14, new Color(1f, 0.9f, 1f, 0.85f));
            Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -84f), new Vector2(0f, 22f));

            // 탭 4개 — 흰 알약(선택) / 반투명 알약, 앞에 작은 아이콘 색 점
            string[] names = { Loc.T("레코드", "Records"), Loc.T("포토카드", "Photocards"), Loc.T("팬아트", "Fan Art"), Loc.T("트로피", "Trophies") };
            Color[] dots = { new Color(1f, 0.45f, 0.35f), new Color(0.45f, 0.8f, 1f), new Color(0.75f, 0.55f, 1f), new Color(1f, 0.8f, 0.3f) };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x = -255f + i * 170f;
                var pill = CoastUiArt.CutePill(_root, "Tab" + i, Color.white, 20, 3);
                Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -128f), new Vector2(160f, 46f));
                pill.raycastTarget = true;
                var dot = CoastUiArt.Panel(pill.transform, "Dot", dots[i], 8);
                dot.raycastTarget = false;
                Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 1f), new Vector2(14f, 14f));
                var tl = CoastOrnate.Label(pill.transform, "T", names[i], 16, new Color(0.25f, 0.15f, 0.35f));
                tl.fontStyle = FontStyle.Bold;
                tl.rectTransform.offsetMin = new Vector2(30f, 0f);
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { _tab = idx; Refresh(); });
                _tabBtns[i] = b;
            }

            // 본문
            var cgo = new GameObject("Content", typeof(RectTransform));
            cgo.transform.SetParent(_root, false);
            _content = cgo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 0f); _content.anchorMax = new Vector2(1f, 1f);
            _content.offsetMin = new Vector2(16f, 96f); _content.offsetMax = new Vector2(-16f, -160f);

            // 닫기 (38차: 디지털 앨범 구매 삭제)
            CoastOrnate.GlassButton(_root, "Close", Loc.T("↩  닫기", "↩  Close"), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(300f, 52f), Close, 0.5f, 20, false);

            _preview = gameObject.AddComponent<AudioSource>();
            _preview.playOnAwake = false; _preview.loop = false; _preview.volume = 0.85f;
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) { if (_detail != null) { _preview.Stop(); CloseDetail(); } else Close(); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F9)) { Collection.DebugUnlockAll(); Refresh(); Toast("DEBUG: unlock all"); }
            if (Input.GetKeyDown(KeyCode.Alpha1)) { _tab = 0; Refresh(); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { _tab = 1; Refresh(); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { _tab = 2; Refresh(); }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { _tab = 3; Refresh(); }
#endif
        }

        private void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
            for (int i = 0; i < 4; i++)
            {
                bool on = i == _tab;
                _tabBtns[i].transform.localScale = Vector3.one * (on ? 1.06f : 0.96f);
                var fill = _tabBtns[i].transform.Find("Fill")?.GetComponent<Image>();
                if (fill != null) fill.color = on ? Color.white : new Color(0.55f, 0.45f, 0.75f, 0.9f);
                var tl = _tabBtns[i].GetComponentInChildren<Text>(); if (tl != null) tl.color = on ? new Color(0.25f, 0.15f, 0.35f) : new Color(1f, 1f, 1f, 0.95f);
            }
            if (_tab == 0) BuildRecords();
            else if (_tab == 1) BuildCards();
            else if (_tab == 2) BuildFanArt();
            else BuildTrophies();
        }

        // ── 레코드 ──────────────────────────────────────────────────────
        private void BuildRecords()
        {
            var sr = MakeScroll(_content, out var list);
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            float y = 0f;
            int owned = RecordTable.UnlockedCount(p);
            // 헤더 알약: "너와 나의 주파수" OST · 7곡  ·  COLLECTED n/7
            var hp = CoastUiArt.CutePill(list, "Album", new Color(0.36f, 0.22f, 0.62f), 22, 3);
            Top(hp.rectTransform, y, 52f, 8f); y += 62f; hp.raycastTarget = false;
            var ht = CoastOrnate.Label(hp.transform, "T", "♪  " + Loc.T($"\"{AlbumTable.AlbumKo}\" OST · {RecordTable.All.Length}곡", $"\"{AlbumTable.AlbumEn}\" OST · {RecordTable.All.Length} tracks"), 17, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleLeft);
            ht.fontStyle = FontStyle.Bold; ht.rectTransform.offsetMin = new Vector2(20f, 0f);
            var col = CoastUiArt.CutePill(hp.transform, "Collected", new Color(1f, 0.85f, 0.35f), 14, 2);
            Place(col.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-84f, 0f), new Vector2(150f, 30f)); col.raycastTarget = false;
            var ct = CoastOrnate.Label(col.transform, "T", $"COLLECTED {owned}/{RecordTable.All.Length}", 12, new Color(0.35f, 0.2f, 0.05f)); ct.fontStyle = FontStyle.Bold;

            // 행 색(파스텔 7색: 시안처럼 노랑/분홍/하늘/연두/살구/라벤더/민트)
            Color[] rows = { new Color(1f, 0.94f, 0.72f), new Color(1f, 0.82f, 0.88f), new Color(0.78f, 0.90f, 1f), new Color(0.82f, 0.96f, 0.80f), new Color(1f, 0.88f, 0.74f), new Color(0.88f, 0.82f, 1f), new Color(0.78f, 0.98f, 0.94f) };
            const float rowH = 74f;
            for (int i = 0; i < RecordTable.All.Length; i++)
            {
                var t = RecordTable.All[i];
                bool has = RecordTable.IsUnlocked(p, t);
                var row = CoastUiArt.CutePill(list, "R" + t.num, has ? rows[i % rows.Length] : new Color(0.55f, 0.52f, 0.62f), 18, 3);
                Top(row.rectTransform, y, rowH - 8f, 0f); y += rowH;
                row.raycastTarget = true;
                // 왼쪽: 레코드 디스크(검정 + 색 라벨 + 구멍), 재생 중이면 돈다
                var disc = CoastUiArt.Panel(row.transform, "Disc", new Color(0.12f, 0.10f, 0.14f), 25);
                disc.raycastTarget = false;
                Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(50f, 50f));
                var groove = CoastUiArt.Panel(disc.transform, "Groove", new Color(1f, 1f, 1f, 0.10f), 19); groove.raycastTarget = false;
                Place(groove.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
                var lbl = CoastUiArt.Panel(disc.transform, "Label", has ? t.label : new Color(0.5f, 0.48f, 0.52f), 11); lbl.raycastTarget = false;
                Place(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
                var hole = CoastUiArt.Panel(lbl.transform, "Hole", new Color(0.12f, 0.10f, 0.14f), 3); hole.raycastTarget = false;
                Place(hole.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
                if (_playingNum == t.num) StartCoroutine(Spin(disc.rectTransform));
                // 제목 / 부제
                string title = has ? (Loc.IsKo ? t.ko : t.en) : "???";
                var name = CoastOrnate.Label(row.transform, "T", $"{t.num:00}  {title}", 19, has ? new Color(0.22f, 0.14f, 0.30f) : new Color(0.92f, 0.9f, 0.95f), TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 26f));
                name.rectTransform.offsetMin = new Vector2(76f, 2f); name.rectTransform.offsetMax = new Vector2(-120f, 30f);
                string sub = has ? "♪ " + (Loc.IsKo ? t.noteKo : t.noteEn) : Loc.T("잠김 · ", "Locked · ") + (Loc.IsKo ? t.unlockKo : t.unlockEn);
                var subL = CoastOrnate.Label(row.transform, "Sub", sub, 12, has ? new Color(0.45f, 0.35f, 0.5f) : new Color(0.9f, 0.88f, 0.95f, 0.8f), TextAnchor.MiddleLeft);
                Place(subL.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 20f));
                subL.rectTransform.offsetMin = new Vector2(76f, -24f); subL.rectTransform.offsetMax = new Vector2(-120f, -4f);
                // 오른쪽: [ S ▶ ] 노란 알약(해금) / 자물쇠
                if (has)
                {
                    bool playing = _playingNum == t.num;
                    var gp = CoastUiArt.CutePill(row.transform, "Play", playing ? new Color(1f, 0.55f, 0.35f) : new Color(1f, 0.82f, 0.25f), 14, 2);
                    gp.raycastTarget = false;
                    Place(gp.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, 0f), new Vector2(88f, 34f));
                    var g = CoastOrnate.Label(gp.transform, "G", playing ? "■" : "S ▶", 15, new Color(0.35f, 0.2f, 0.05f)); g.fontStyle = FontStyle.Bold;
                }
                else
                {
                    var lp = CoastUiArt.CutePill(row.transform, "Lock", new Color(0.35f, 0.32f, 0.42f), 14, 2); lp.raycastTarget = false;
                    Place(lp.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, 0f), new Vector2(88f, 34f));
                    var g = CoastOrnate.Label(lp.transform, "G", Loc.T("잠김", "Locked"), 13, new Color(1f, 1f, 1f, 0.9f));
                }
                if (has && RecordTable.IsNew(p, t))
                {
                    var n = CoastUiArt.Panel(row.transform, "New", CoastOrnate.Red, 8); n.raycastTarget = false;
                    Place(n.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -4f), new Vector2(40f, 18f));
                    var nl = CoastOrnate.Label(n.transform, "T", "NEW", 10, Color.white);
                }
                var tr = t;
                var btn = row.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { if (has) ToggleRecord(tr); else Toast(Loc.T("잠김 — ", "Locked — ") + (Loc.IsKo ? tr.unlockKo : tr.unlockEn)); });
            }
            y += 8f;
            var foot = CoastOrnate.Label(list, "Foot", Loc.T($"롱컷 하나에 레코드 하나 · 보너스 3곡은 20챕터 S급 90% 이상 (지금 S급 {RecordTable.SCount(p)}/20)", $"One record per long cut · bonus tracks at 90% Rank S (now {RecordTable.SCount(p)}/20)"), 12, new Color(1f, 1f, 1f, 0.7f));
            Top(foot.rectTransform, y, 20f, 0f); y += 24f;
            list.sizeDelta = new Vector2(0f, y + 20f);
        }

        private int _playingNum;
        private void ToggleRecord(RecordTable.Track t)
        {
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            if (_playingNum == t.num) { _preview.Stop(); _playingNum = 0; Refresh(); return; }
            var clip = CoastBgmLibrary.Load(t.Clip);
            if (clip == null) { Toast(Loc.T("음악 파일이 없어 (BGM/" + t.Clip + ")", "Missing " + t.Clip)); return; }
            _preview.Stop(); _preview.clip = clip; _preview.time = 0f; _preview.loop = true; _preview.Play();
            _playingNum = t.num;
            RecordTable.MarkSeen(p, t);
            CoastAudioManager.Instance?.SetBedMuted(true);
            Refresh();
        }

        private IEnumerator StopAfter(float s) { yield return new WaitForSecondsRealtime(s); if (_preview != null) _preview.Stop(); }
        private IEnumerator Spin(RectTransform rt) { while (rt != null) { rt.Rotate(0f, 0f, -Time.unscaledDeltaTime * 33f); yield return null; } }

        private static string FormatLyrics(string raw)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var line in raw.Split('\n'))
            {
                var l = line.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(l)) { sb.Append('\n'); continue; }
                var p = l.Split('|');
                if (p.Length >= 3) sb.Append($"<b>{p[0].Trim()}</b>\n<color=#7a6a60>{p[1].Trim()}</color>\n<i>{p[2].Trim()}</i>\n\n");
                else sb.Append(l).Append('\n');
            }
            return sb.ToString();
        }

        // ── 포토카드 ────────────────────────────────────────────────────
        private void BuildCards()
        {
            int perPage = 9, pages = Mathf.CeilToInt(PhotocardTable.Count / (float)perPage);
            _cardPage = Mathf.Clamp(_cardPage, 0, pages - 1);
            var title = CoastOrnate.Label(_content, "Cnt", Loc.T($"바인더 {_cardPage + 1}/{pages}  ·  {Collection.CardsOwned}/{PhotocardTable.Count}장  ·  러닝 중 포토카드 아이템으로 얻는다", $"Binder {_cardPage + 1}/{pages}  ·  {Collection.CardsOwned}/{PhotocardTable.Count}  ·  found as items while running"), 14, new Color(1f, 0.92f, 0.75f));
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(0f, 24f));
            // 등급 확률 표
            var odds = CoastOrnate.Label(_content, "Odds", $"N {Collection.GradeWeights[0]}%  ·  R {Collection.GradeWeights[1]}%  ·  SR {Collection.GradeWeights[2]}%  ·  SSR {Collection.GradeWeights[3]}%", 12, new Color(1f, 0.85f, 1f, 0.75f));
            Place(odds.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), new Vector2(0f, 20f));
            float cw = 196f, chh = 268f, gap = 14f;
            float x0 = -(cw * 1.5f + gap);
            for (int i = 0; i < perPage; i++)
            {
                int id = _cardPage * perPage + i + 1;
                if (id > PhotocardTable.Count) break;
                int col = i % 3, row = i / 3;
                var card = PhotocardTable.Get(id);
                bool has = Collection.HasCard(id);
                var grade = PhotocardTable.GradeOf(id);
                // 카드 틀: 소유 = 크림 폴라로이드, 잠김 = 짙은 남보라 + ? + 자물쇠
                var slot = CoastUiArt.CutePill(_content, "Slot" + id, has ? new Color(0.99f, 0.97f, 0.93f) : new Color(0.22f, 0.16f, 0.40f), 14, 3);
                slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                slot.rectTransform.pivot = new Vector2(0f, 1f);
                slot.rectTransform.anchoredPosition = new Vector2(x0 + col * (cw + gap), -58f - row * (chh + gap));
                slot.rectTransform.sizeDelta = new Vector2(cw, chh);
                slot.raycastTarget = true;
                var img = CoastHudLayout.MakeImage(slot.transform, "Img", Vector2.zero, Vector2.one, new Vector2(10f, 44f), new Vector2(-10f, -10f), has ? Color.white : new Color(0.16f, 0.11f, 0.30f));
                img.raycastTarget = false;
                if (has)
                {
                    var tex = ArtAssets.LoadTexture(card.Image) ?? ArtAssets.LoadTexture(card.FallbackImage);
                    if (tex != null) { img.sprite = CoastUiArt.AsSprite(tex, 100f); img.preserveAspect = false; }
                    // 등급 배지(좌상단)
                    var badge = CoastUiArt.CutePill(slot.transform, "Grade", Collection.GradeColor(grade), 10, 2); badge.raycastTarget = false;
                    Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -22f), new Vector2(50f, 22f));
                    var gl = CoastOrnate.Label(badge.transform, "T", Collection.GradeName(grade), 11, Color.white); gl.fontStyle = FontStyle.Bold;
                }
                else
                {
                    var q = CoastOrnate.Label(img.transform, "Q", "?", 54, new Color(1f, 1f, 1f, 0.28f));
                    Place(q.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(80f, 70f));
                    // 자물쇠(둥근 몸통 + 고리)
                    var body = CoastUiArt.Panel(img.transform, "LockBody", new Color(1f, 1f, 1f, 0.35f), 5); body.raycastTarget = false;
                    Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(26f, 20f));
                    var ring = CoastUiArt.Panel(img.transform, "LockRing", new Color(1f, 1f, 1f, 0.35f), 8); ring.raycastTarget = false;
                    Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(18f, 16f));
                    var ringIn = CoastUiArt.Panel(ring.transform, "In", new Color(0.16f, 0.11f, 0.30f), 4); ringIn.raycastTarget = false;
                    Place(ringIn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(8f, 10f));
                    var gb = CoastUiArt.Panel(slot.transform, "GradeDim", new Color(Collection.GradeColor(grade).r, Collection.GradeColor(grade).g, Collection.GradeColor(grade).b, 0.55f), 10); gb.raycastTarget = false;
                    Place(gb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -22f), new Vector2(50f, 22f));
                    var gl = CoastOrnate.Label(gb.transform, "T", Collection.GradeName(grade), 11, new Color(1f, 1f, 1f, 0.85f));
                }
                var cap = CoastOrnate.Label(slot.transform, "Cap", has ? $"{id:00}  " + card.Name : $"{id:00}  ???", 12, has ? new Color(0.3f, 0.2f, 0.35f) : new Color(1f, 1f, 1f, 0.6f));
                Place(cap.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 26f));
                cap.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (has && Collection.CardIsNew(id))
                {
                    var n = CoastUiArt.Panel(slot.transform, "New", CoastOrnate.Red, 8);
                    n.rectTransform.anchorMin = n.rectTransform.anchorMax = new Vector2(1f, 1f);
                    n.rectTransform.anchoredPosition = new Vector2(-6f, -6f); n.rectTransform.pivot = new Vector2(1f, 1f); n.rectTransform.sizeDelta = new Vector2(44f, 20f);
                    var nl = CoastOrnate.Label(n.transform, "T", "NEW", 11, Color.white); Place(nl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                int cid = id;
                var b = slot.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { if (has) OpenCard(cid); else Toast(Loc.T($"잠김 · [{Collection.GradeName(grade)}] 러닝 중 포토카드 아이템에서 {Collection.GradeWeights[(int)grade]}%", $"Locked · [{Collection.GradeName(grade)}] {Collection.GradeWeights[(int)grade]}% from photocard items")); });
            }
            CoastOrnate.GlassButton(_content, "Prev", "◀", new Vector2(0.5f, 0f), new Vector2(-70f, 12f), new Vector2(110f, 40f), () => { _cardPage = Mathf.Max(0, _cardPage - 1); Refresh(); }, 0.45f, 18, false);
            CoastOrnate.GlassButton(_content, "Next", "▶", new Vector2(0.5f, 0f), new Vector2(70f, 12f), new Vector2(110f, 40f), () => { _cardPage = Mathf.Min(pages - 1, _cardPage + 1); Refresh(); }, 0.45f, 18, false);
        }

        private void OpenCard(int id)
        {
            var card = PhotocardTable.Get(id);
            bool isNew = Collection.CardIsNew(id);
            Collection.ClearNew(id);
            var d = MakeDetail();
            // 카드 본체(앞/뒤 뒤집기)
            var holder = new GameObject("Card", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(d.transform, false);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f); holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -30f); holder.sizeDelta = new Vector2(440f, 640f);
            // 38차: 등급 프레임 — SSR 무지개(4색 겹침), SR 보라, R 하늘, N 연두
            var grade = PhotocardTable.GradeOf(id);
            if (grade == CardGrade.SSR)
            {
                Color[] rim = { new Color(1f, 0.55f, 0.75f), new Color(1f, 0.85f, 0.4f), new Color(0.5f, 0.9f, 1f), new Color(0.75f, 0.6f, 1f) };
                for (int k = 0; k < 4; k++)
                {
                    var r = CoastUiArt.Panel(holder, "Rim" + k, rim[k], 22 - k * 2); r.raycastTarget = false;
                    r.rectTransform.anchorMin = Vector2.zero; r.rectTransform.anchorMax = Vector2.one;
                    r.rectTransform.offsetMin = new Vector2(-12f + k * 3f, -12f + k * 3f); r.rectTransform.offsetMax = new Vector2(12f - k * 3f, 12f - k * 3f);
                }
            }
            else
            {
                var r = CoastUiArt.Panel(holder, "Rim", Collection.GradeColor(grade), 22); r.raycastTarget = false;
                r.rectTransform.anchorMin = Vector2.zero; r.rectTransform.anchorMax = Vector2.one;
                r.rectTransform.offsetMin = new Vector2(-8f, -8f); r.rectTransform.offsetMax = new Vector2(8f, 8f);
            }
            var front = CoastUiArt.Panel(holder, "Front", Color.white, 18);
            Stretch(front.rectTransform);
            var tex = ArtAssets.LoadTexture(card.Image) ?? ArtAssets.LoadTexture(card.FallbackImage);
            var img = CoastHudLayout.MakeImage(front.transform, "Img", Vector2.zero, Vector2.one, new Vector2(12f, 60f), new Vector2(-12f, -12f), Color.white);
            if (tex != null) img.sprite = CoastUiArt.AsSprite(tex, 100f);
            // 등급 배지(좌상단) + SSR 이면 "RARE PHOTOCARD" 리본
            var gb = CoastUiArt.CutePill(front.transform, "GradeBadge", Collection.GradeColor(grade), 14, 3);
            Place(gb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(84f, 40f));
            var gbl = CoastOrnate.Label(gb.transform, "T", Collection.GradeName(grade), 20, Color.white); gbl.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(gbl, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            if (grade == CardGrade.SSR)
            {
                var rare = CoastOrnate.Label(front.transform, "Rare", "★★★★★  RARE PHOTOCARD", 11, new Color(0.85f, 0.45f, 0.1f));
                Place(rare.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -58f), new Vector2(180f, 16f));
                rare.fontStyle = FontStyle.Bold;
            }
            var cap = CoastOrnate.Label(front.transform, "Cap", $"{card.id:00}  {card.Name}" + (Collection.CardSigned(id) ? "  ★" : ""), 18, Ink);
            Place(cap.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 30f), new Vector2(0f, 30f));
            var artist = CoastOrnate.Label(front.transform, "A", $"{AlbumTable.Artist} · {Loc.T(AlbumTable.AlbumKo, AlbumTable.AlbumEn)}", 12, new Color(0.5f, 0.45f, 0.42f));
            Place(artist.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 18f));
            if (Collection.CardSigned(id))
            {
                var sign = CoastOrnate.Label(front.transform, "Sign", Loc.T("— 하늘 ♡", "— Haneul ♡"), 22, new Color(0.85f, 0.2f, 0.25f, 0.9f));
                Place(sign.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-90f, 110f), new Vector2(160f, 40f));
                sign.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            }
            var back = CoastUiArt.Panel(holder, "Back", Paper, 18);
            Stretch(back.rectTransform); back.gameObject.SetActive(false);
            var msg = CoastOrnate.Label(back.transform, "Msg", card.Back, 22, Ink);
            Place(msg.rectTransform, Vector2.zero, Vector2.one, new Vector2(30f, 30f), new Vector2(-30f, -30f));
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            var stamp = CoastOrnate.Label(back.transform, "Stamp", $"No.{card.id:00} / {PhotocardTable.Count}   {AlbumTable.ArtistEn}", 12, new Color(0.5f, 0.45f, 0.42f));
            Place(stamp.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(0f, 18f));

            bool showingFront = true;
            var flipBtn = holder.gameObject.AddComponent<Button>(); flipBtn.transition = Selectable.Transition.None;
            front.raycastTarget = true; back.raycastTarget = true;
            flipBtn.onClick.AddListener(() => StartCoroutine(Flip(holder, () => { showingFront = !showingFront; front.gameObject.SetActive(showingFront); back.gameObject.SetActive(!showingFront); })));

            var hint = CoastOrnate.Label(d.transform, "H", Loc.T("카드를 탭하면 뒷면 · 아래 버튼으로 저장·공유", "Tap the card to flip · save & share below"), 13, new Color(0.45f, 0.4f, 0.38f));
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -690f), new Vector2(0f, 22f));
            CoastOrnate.GlassButton(d.transform, "Share", Loc.T("이미지 저장·공유", "Save & share"), new Vector2(0.5f, 0f), new Vector2(-110f, 40f), new Vector2(200f, 44f), () => StartCoroutine(SaveCardImage(holder, card)), 0.5f, 16, true);
            CoastOrnate.GlassButton(d.transform, "Back", Loc.T("← 바인더", "← Binder"), new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(200f, 44f), CloseDetail, 0.45f, 16, false);

            if (isNew) StartCoroutine(Reveal(holder));
            _askReviewAfter = true;
        }

        private IEnumerator Reveal(RectTransform card)
        {
            // 봉투 개봉: 작게·뒤집혀서 시작 → 커지며 정면
            float t = 0f; card.localScale = new Vector3(0.2f, 0.2f, 1f); card.localRotation = Quaternion.Euler(0f, 90f, 0f);
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime; float k = Mathf.SmoothStep(0f, 1f, t / 0.6f);
                card.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k);
                card.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 0f, k), 0f);
                yield return null;
            }
            card.localScale = Vector3.one; card.localRotation = Quaternion.identity;
            CoastAudioManager.PlayAnywhere(CoastSfx.CardReveal);
        }

        private IEnumerator Flip(RectTransform card, Action atHalf)
        {
            float t = 0f; bool done = false;
            while (t < 0.36f)
            {
                t += Time.unscaledDeltaTime; float k = t / 0.36f;
                float ang = k < 0.5f ? Mathf.Lerp(0f, 90f, k * 2f) : Mathf.Lerp(-90f, 0f, (k - 0.5f) * 2f);
                if (k >= 0.5f && !done) { done = true; atHalf(); }
                card.localRotation = Quaternion.Euler(0f, ang, 0f);
                yield return null;
            }
            card.localRotation = Quaternion.identity;
        }

        /// 카드 영역을 캡처해 PNG로 저장(Pictures/CoastRun). 안드로이드 갤러리 등록·공유 시트는 플러그인(NativeShare) 연결 자리.
        private IEnumerator SaveCardImage(RectTransform card, CardDef def)
        {
            yield return new WaitForEndOfFrame();
            var cam = _canvas.worldCamera;
            var corners = new Vector3[4]; card.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            int x = Mathf.Clamp(Mathf.RoundToInt(min.x), 0, Screen.width - 1), y = Mathf.Clamp(Mathf.RoundToInt(min.y), 0, Screen.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(max.x - min.x), 8, Screen.width - x), h = Mathf.Clamp(Mathf.RoundToInt(max.y - min.y), 8, Screen.height - y);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0); tex.Apply();
            string dir = Path.Combine(Application.persistentDataPath, "Share");
            Directory.CreateDirectory(dir);
            string name = $"JEJU_card_{def.id:00}.png";
            string file = Path.Combine(dir, name);
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(file, png);
            Destroy(tex);
            var p = GameManager.I?.Profile; if (p != null) { p.shareCount++; GameManager.I.WriteProfileNow(); }
            string caption = $"{AlbumTable.ArtistEn} — {AlbumTable.AlbumEn} · {def.Name} #JEJU #OurFrequency";
            bool shared = NativeShareLite.SharePng(png, name, Loc.T("포토카드 공유", "Share photocard"), caption);
            Toast(shared ? Loc.T("갤러리(Pictures/JEJU)에 저장했어요", "Saved to gallery (Pictures/JEJU)") : Loc.T("저장했어요: " + file, "Saved: " + file));
        }

        // ── 팬아트 ──────────────────────────────────────────────────────
        private void BuildFanArt()
        {
            var sr = MakeScroll(_content, out var list);
            float y = 0f;
            var prof = GameManager.I != null ? GameManager.I.Profile : null;
            int open = MissionTable.FanArtUnlocked(prof);
            var intro = CoastOrnate.Label(list, "I", Loc.T($"팬아트 갤러리 — 미션 별 {MissionTable.StarsPerFanArt}개마다 한 장이 열려요. (별 {(prof != null ? prof.StarsTotal : 0)}/60 → {open}장)", $"Fan art gallery — one piece per {MissionTable.StarsPerFanArt} mission stars. (stars {(prof != null ? prof.StarsTotal : 0)}/60 → {open} open)"), 14, new Color(1f, 0.92f, 0.75f));
            Top(intro.rectTransform, y, 30f, 0f); y += 36f;
            int n = 0;
            for (int i = 1; i <= 40; i++)
            {
                var tex = ArtAssets.LoadTexture($"FanArt/FanArt_{i:00}");
                if (tex == null) { if (i > 4) break; continue; }
                n++;
                if (i > open)
                {
                    var lockF = CoastUiArt.Panel(list, "L" + i, new Color(0.2f, 0.17f, 0.2f, 0.9f), 12);
                    Top(lockF.rectTransform, y, 90f, 10f);
                    var lt = CoastOrnate.Label(lockF.transform, "T", Loc.T($"잠김 · 팬아트 #{i:00} — 별 {i * MissionTable.StarsPerFanArt}개에 열려요", $"Locked · Fan art #{i:00} — opens at {i * MissionTable.StarsPerFanArt} stars"), 16, new Color(1f, 0.92f, 0.75f));
                    CoastOrnate.Stretch(lt.rectTransform, 0f, 0f, 0f, 0f);
                    y += 102f;
                    continue;
                }
                float h = 660f * tex.height / tex.width;
                var frame = CoastUiArt.Panel(list, "F" + i, Paper, 12);
                Top(frame.rectTransform, y, h + 44f, 10f);
                var img = CoastHudLayout.MakeImage(frame.transform, "Img", Vector2.zero, Vector2.one, new Vector2(10f, 34f), new Vector2(-10f, -10f), Color.white);
                img.sprite = CoastUiArt.AsSprite(tex, 100f); img.preserveAspect = true;
                var cap = CoastOrnate.Label(frame.transform, "C", Loc.T($"팬아트 #{i:00}", $"Fan art #{i:00}"), 12, new Color(0.45f, 0.4f, 0.38f));
                Place(cap.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 20f));
                y += h + 56f;
            }
            if (n == 0) { var none = CoastOrnate.Label(list, "N", Loc.T("아직 없어요.", "Nothing yet."), 16, Color.white); Top(none.rectTransform, y, 30f, 0f); y += 40f; }
            list.sizeDelta = new Vector2(0f, y + 20f);
        }

        // ── 트로피(업적 40 + 기록) ────────────────────────────────────
        private void BuildTrophies()
        {
            var sr = MakeScroll(_content, out var list);
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            if (p == null) { list.sizeDelta = new Vector2(0f, 40f); return; }
            p.EnsureArrays();
            if (p.achNewCount > 0) { p.achNewCount = 0; GameManager.I.WriteProfileNow(); }
            float y = 0f;
            int got = AchievementTable.Count(p);
            var head = CoastOrnate.Label(list, "H", Loc.T($"업적 {got}/{AchievementTable.All.Length}  ·  미션 별 {p.StarsTotal}/60  ·  오늘의 런 도장 {p.DailyCount}  ·  엔딩 {p.EndingsSeenCount}/7", $"Achievements {got}/{AchievementTable.All.Length}  ·  Stars {p.StarsTotal}/60  ·  Daily stamps {p.DailyCount}  ·  Endings {p.EndingsSeenCount}/7"), 16, new Color(1f, 0.92f, 0.75f));
            Top(head.rectTransform, y, 30f, 0f); y += 36f;
            // 기록
            var rec = CoastUiArt.Panel(list, "Rec", Paper, 14);
            Top(rec.rectTransform, y, 118f, 8f);
            var rt = CoastOrnate.Label(rec.transform, "T",
                Loc.T($"무한 달리기  최고 {p.endlessBestDist:N0}m · {p.endlessBestScore:N0}점\n오늘의 런  최고 {p.dailyBestScore:N0}점 · 연속 {p.dailyStreak}일 (최고 {p.dailyStreakBest})\n누적  런 {p.totalRuns}회 · {p.totalDistance / 1000f:0.0}km · 코인 {p.totalCoins:N0} · 니어미스 {p.totalNearMiss:N0} · 무피격 {p.flawlessRuns}",
                      $"Endless  best {p.endlessBestDist:N0} m · {p.endlessBestScore:N0} pts\nDaily  best {p.dailyBestScore:N0} · streak {p.dailyStreak} (best {p.dailyStreakBest})\nTotal  {p.totalRuns} runs · {p.totalDistance / 1000f:0.0} km · {p.totalCoins:N0} coins · {p.totalNearMiss:N0} near misses · {p.flawlessRuns} flawless"),
                15, Ink, TextAnchor.MiddleLeft);
            CoastOrnate.Stretch(rt.rectTransform, 14f, 6f, -14f, -6f); rt.horizontalOverflow = HorizontalWrapMode.Wrap;
            y += 126f;
            // 챕터 별
            var starsRow = CoastUiArt.Panel(list, "Stars", Paper, 14);
            Top(starsRow.rectTransform, y, 150f, 8f);
            var sb = new System.Text.StringBuilder();
            for (int c = 1; c <= 20; c++)
            {
                int n = MissionTable.Stars(p, c);
                sb.Append($"{c:00} ").Append(n >= 1 ? "★" : "☆").Append(n >= 2 ? "★" : "☆").Append(n >= 3 ? "★" : "☆");
                sb.Append(c % 4 == 0 ? "\n" : "    ");
            }
            var st = CoastOrnate.Label(starsRow.transform, "T", Loc.T("챕터 미션 별\n", "Chapter mission stars\n") + sb.ToString(), 14, Ink, TextAnchor.UpperLeft);
            CoastOrnate.Stretch(st.rectTransform, 14f, 6f, -14f, -8f);
            y += 158f;
            // 업적 목록
            foreach (var a in AchievementTable.All)
            {
                bool has = AchievementTable.Has(p, a.id);
                var row = CoastUiArt.Panel(list, "A" + a.id, has ? Paper : new Color(0.2f, 0.17f, 0.2f, 0.85f), 10);
                Top(row.rectTransform, y, 56f, 8f);   // 18차: 행 48→56, 글자 16→18 / 12→14 (폰 가독성)
                var t = CoastOrnate.Label(row.transform, "T", (has ? "🏆  " : "○  ") + a.Name, 18, has ? Ink : new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleLeft);
                CoastOrnate.Stretch(t.rectTransform, 14f, 0f, -230f, 0f);
                var h = CoastOrnate.Label(row.transform, "H", a.Hint, 14, has ? new Color(0.45f, 0.4f, 0.38f) : new Color(0.65f, 0.6f, 0.58f), TextAnchor.MiddleRight);
                CoastOrnate.Stretch(h.rectTransform, 0f, 0f, -12f, 0f);
                y += 62f;
            }
            list.sizeDelta = new Vector2(0f, y + 20f);
        }

        // 38차: 디지털 앨범 구매 삭제 — 옛 호출처는 컬렉션만 연다.
        public static void OpenPaywall() { if (_active == null) Open(null, 0); }

        // ── 리뷰 유도 (첫 포토카드 개봉 이후 1회) ──
        private void MaybeAskReview()
        {
            var p = GameManager.I?.Profile;
            if (p == null || p.ratePrompted || Collection.CardsOwned < 3) return;
            p.ratePrompted = true; GameManager.I.WriteProfileNow();
            var d = MakeDetail();
            var t = CoastOrnate.Label(d.transform, "T", Loc.T("카드 세 장째네요.", "Three cards already."), 22, Ink);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(0f, 34f));
            var b = CoastOrnate.Label(d.transform, "B", Loc.T("『제주』가 마음에 들면 별점 하나 남겨 주세요. 다음 곡을 만드는 힘이 돼요.", "If you like JEJU, a rating helps us make the next song."), 16, Ink);
            Place(b.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(-60f, 80f));
            b.horizontalOverflow = HorizontalWrapMode.Wrap;
            CoastOrnate.GlassButton(d.transform, "Rate", Loc.T("별점 남기기", "Rate"), new Vector2(0.5f, 0f), new Vector2(-110f, 40f), new Vector2(200f, 44f), () => { Application.OpenURL("market://details?id=" + Application.identifier); CloseDetail(); }, 0.5f, 16, true);
            CoastOrnate.GlassButton(d.transform, "Later", Loc.T("나중에", "Later"), new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(200f, 44f), CloseDetail, 0.45f, 16, false);
        }

        // ── 공용 ──
        private GameObject MakeDetail()
        {
            if (_detail != null) Destroy(_detail);
            var panel = CoastUiArt.Panel(_root, "Detail", Paper, 20);
            panel.rectTransform.anchorMin = new Vector2(0f, 0f); panel.rectTransform.anchorMax = new Vector2(1f, 1f);
            panel.rectTransform.offsetMin = new Vector2(14f, 96f); panel.rectTransform.offsetMax = new Vector2(-14f, -80f);
            panel.raycastTarget = true;
            _detail = panel.gameObject;
            return _detail;
        }
        private bool _askReviewAfter;
        private void CloseDetail()
        {
            if (_detail != null) Destroy(_detail);
            _detail = null; Refresh();
            if (_askReviewAfter) { _askReviewAfter = false; MaybeAskReview(); }
        }

        private static ScrollRect MakeScroll(Transform parent, out RectTransform list, Vector2? offMin = null, Vector2? offMax = null)
        {
            var go = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = offMin ?? Vector2.zero; rt.offsetMax = offMax ?? Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var sr = go.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            list = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
            list.SetParent(rt, false);
            list.anchorMin = new Vector2(0f, 1f); list.anchorMax = new Vector2(1f, 1f); list.pivot = new Vector2(0.5f, 1f);
            sr.content = list; sr.viewport = rt;
            return sr;
        }
        private static void Top(RectTransform rt, float y, float h, float sideInset)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(sideInset, -y - h); rt.offsetMax = new Vector2(-sideInset, -y);
        }
        private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            if (aMin == aMax) { rt.anchoredPosition = pos; rt.sizeDelta = size; }
            else { rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(0f, size.y); rt.offsetMin = new Vector2(0f, rt.offsetMin.y); rt.offsetMax = new Vector2(0f, rt.offsetMax.y); }
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static string GradeText(ChapterGrade g) => g == ChapterGrade.None ? "" : g.ToString();

        private Text _toast; private Coroutine _toastCo;
        private void Toast(string msg)
        {
            if (_toast == null)
            {
                var p = CoastUiArt.Panel(_root, "Toast", new Color(0f, 0f, 0f, 0.75f), 12);
                p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                p.rectTransform.anchoredPosition = new Vector2(0f, 110f); p.rectTransform.sizeDelta = new Vector2(600f, 44f);
                _toast = CoastOrnate.Label(p.transform, "T", "", 14, Color.white);
                Place(_toast.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _toast.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            _toast.transform.parent.gameObject.SetActive(true); _toast.text = msg;
            if (_toastCo != null) StopCoroutine(_toastCo);
            _toastCo = StartCoroutine(HideToast());
        }
        private IEnumerator HideToast() { yield return new WaitForSecondsRealtime(2.2f); if (_toast != null) _toast.transform.parent.gameObject.SetActive(false); }

        private void Close()
        {
            IsOpen = false;
            if (_preview != null) _preview.Stop();
            CoastAudioManager.Instance?.SetBedMuted(false);
            var cb = _onClose; _onClose = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            _active = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
