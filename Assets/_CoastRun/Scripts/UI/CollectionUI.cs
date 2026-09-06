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
            var bg = CoastHudLayout.MakeImage(_root, "Bg", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.10f, 0.08f, 0.10f, 0.97f));
            bg.raycastTarget = true;
            var tex = ArtAssets.LoadTexture(Loc.ResName("UI_Title_Gate")) ?? ArtAssets.LoadTexture("UI_Title_Gate");
            if (tex != null)
            {
                var art = CoastHudLayout.MakeImage(_root, "Art", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(1f, 1f, 1f, 0.18f));
                art.sprite = CoastUiArt.AsSprite(tex, 100f); art.raycastTarget = false;
            }

            // 헤더: 듀오 이름 + 앨범명 + AI 태그
            var head = CoastOrnate.Label(_root, "Head", $"{AlbumTable.Artist}  ·  {Loc.T(AlbumTable.AlbumKo, AlbumTable.AlbumEn)}", 26, CoastOrnate.Ivory);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(0f, 40f));
            CoastUiArt.OutlineText(head, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            var tag = CoastOrnate.Label(_root, "Tag", AlbumTable.ArtistTag + "  ·  " + Loc.T("컬렉션", "Collection"), 14, new Color(1f, 0.9f, 0.7f, 0.85f));
            Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(0f, 22f));

            // 탭
            string[] names = { Loc.T("레코드", "Records"), Loc.T("포토카드", "Photocards"), Loc.T("팬아트", "Fan Art"), Loc.T("트로피", "Trophies") };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x = -258f + i * 172f;
                _tabBtns[i] = CoastOrnate.GlassButton(_root, "Tab" + i, names[i], new Vector2(0.5f, 1f), new Vector2(x, -96f), new Vector2(164f, 42f), () => { _tab = idx; Refresh(); }, 0.45f, 16, false);
            }

            // 본문
            var cgo = new GameObject("Content", typeof(RectTransform));
            cgo.transform.SetParent(_root, false);
            _content = cgo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 0f); _content.anchorMax = new Vector2(1f, 1f);
            _content.offsetMin = new Vector2(16f, 84f); _content.offsetMax = new Vector2(-16f, -126f);

            // 닫기 + 구매/상태
            CoastOrnate.GlassButton(_root, "Close", Loc.T("닫기", "Close"), new Vector2(0.5f, 0f), new Vector2(-130f, 40f), new Vector2(220f, 46f), Close, 0.45f, 18, false);
            var buyLabel = Collection.AlbumOwned ? Loc.T("디지털 앨범 보유", "Album owned") : Loc.T("디지털 앨범 구매", "Get the album");
            CoastOrnate.GlassButton(_root, "Buy", buyLabel, new Vector2(0.5f, 0f), new Vector2(130f, 40f), new Vector2(220f, 46f), () => { if (!Collection.AlbumOwned) ShowPaywall(); else Toast(Loc.T("고마워요. 전 트랙이 열려 있어요.", "Thank you. Every track is yours.")); }, 0.45f, 18, !Collection.AlbumOwned);

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
            if (Input.GetKeyDown(KeyCode.Return) && _detail == null) { if (_tab == 0) { if (Collection.TrackUnlocked(1)) OpenTrack(1); } else if (_tab == 1 && Collection.HasCard(1)) OpenCard(1); }
            if (Input.GetKeyDown(KeyCode.P)) ShowPaywall();
#endif
        }

        private void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
            for (int i = 0; i < 4; i++) _tabBtns[i].transform.localScale = Vector3.one * (i == _tab ? 1.06f : 0.96f);
            if (_tab == 0) BuildRecords();
            else if (_tab == 1) BuildCards();
            else if (_tab == 2) BuildFanArt();
            else BuildTrophies();
        }

        // ── 레코드 ──────────────────────────────────────────────────────
        private void BuildRecords()
        {
            var sr = MakeScroll(_content, out var list);
            string[] seasons = { Loc.T("봄 — Spring EP", "Spring EP"), Loc.T("여름 — Summer EP", "Summer EP"), Loc.T("가을 — Autumn EP", "Autumn EP"), Loc.T("겨울 — Winter EP", "Winter EP") };
            float y = 0f;
            const float rowH = 66f, headH = 40f;
            for (int c = 1; c <= 20; c++)
            {
                var t = AlbumTable.Get(c);
                if ((c - 1) % 5 == 0)
                {
                    var h = CoastOrnate.Label(list, "S" + c, seasons[(c - 1) / 5], 18, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleLeft);
                    Top(h.rectTransform, y, headH, 12f); y += headH;
                }
                bool owned = Collection.TrackUnlocked(c);
                bool paywalled = !Collection.CanPlayChapter(c);
                var grade = Collection.TrackGrade(c);
                var row = CoastUiArt.Panel(list, "T" + c, owned ? Paper : new Color(0.25f, 0.22f, 0.24f, 0.8f), 14);
                Top(row.rectTransform, y, rowH - 6f, 0f); y += rowH;
                row.raycastTarget = true;
                var num = CoastOrnate.Label(row.transform, "N", c.ToString("00"), 20, owned ? CoastOrnate.Red : new Color(0.6f, 0.55f, 0.55f));
                Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(44f, 40f));
                string title = owned ? t.Title : (paywalled ? Loc.T("앨범 구매 시 해금", "Unlock with the album") : "???");
                var name = CoastOrnate.Label(row.transform, "T", title, 18, owned ? Ink : new Color(0.75f, 0.72f, 0.72f), TextAnchor.MiddleLeft);
                Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 26f));
                name.rectTransform.offsetMin = new Vector2(58f, -2f); name.rectTransform.offsetMax = new Vector2(-120f, 26f);
                string sub = owned
                    ? $"{t.KindLabel} · {(AlbumTable.FullVersion(grade) ? Loc.T("풀버전", "Full") : Loc.T("1절 (A급 이상이면 풀버전)", "Verse 1 (rank A+ for full)"))}"
                    : (paywalled ? "" : (t.kind == TrackKind.Duet ? Loc.T($"{c}챕터 S급", $"Chapter {c} rank S") : Loc.T($"{c}챕터 클리어", $"Clear chapter {c}")));
                var subL = CoastOrnate.Label(row.transform, "Sub", sub, 12, owned ? new Color(0.4f, 0.35f, 0.32f) : new Color(0.7f, 0.66f, 0.66f), TextAnchor.MiddleLeft);
                Place(subL.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 20f));
                subL.rectTransform.offsetMin = new Vector2(58f, -26f); subL.rectTransform.offsetMax = new Vector2(-120f, -6f);
                var g = CoastOrnate.Label(row.transform, "G", owned ? GradeText(grade) : (paywalled ? "🔒" : ""), 16, owned ? new Color(0.83f, 0.66f, 0.2f) : new Color(0.7f, 0.66f, 0.66f));
                Place(g.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(100f, 30f));
                int ch = c;
                var btn = row.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { if (paywalled) ShowPaywall(); else if (owned) OpenTrack(ch); else Toast(sub); });
            }
            list.sizeDelta = new Vector2(0f, y + 20f);
            var foot = CoastOrnate.Label(list, "Foot", Loc.T($"{Collection.TracksUnlocked}/20 트랙 · 러닝 중 그 챕터 곡이 흐른다", $"{Collection.TracksUnlocked}/20 tracks · each chapter's track plays while you run"), 13, new Color(1f, 1f, 1f, 0.7f));
            Top(foot.rectTransform, y, 20f, 0f);
        }

        private void OpenTrack(int ch)
        {
            var t = AlbumTable.Get(ch);
            var d = MakeDetail();
            var jacket = ArtAssets.LoadTexture($"Album/Jacket_{SeasonLook.Suffix(t.Season)}") ?? ArtAssets.LoadTexture("Album/Jacket_NOON") ?? ArtAssets.LoadTexture($"Cut_CH{ch:00}_Close");
            var disc = CoastHudLayout.MakeImage(d.transform, "Disc", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-150f, -340f), new Vector2(150f, -40f), new Color(0.08f, 0.08f, 0.09f));
            disc.sprite = CoastUiArt.RoundedRect(150); disc.type = Image.Type.Sliced;
            var label = CoastHudLayout.MakeImage(disc.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100f, -100f), new Vector2(100f, 100f), Color.white);
            label.sprite = jacket != null ? CoastUiArt.AsSprite(jacket, 100f) : CoastUiArt.RoundedRect(100); label.type = Image.Type.Simple; label.preserveAspect = true;
            if (jacket == null) label.color = CoastOrnate.Red;
            var hole = CoastHudLayout.MakeImage(disc.transform, "Hole", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(6f, 6f), new Color(0.1f, 0.1f, 0.1f));
            hole.sprite = CoastUiArt.RoundedRect(6);
            StartCoroutine(Spin(disc.rectTransform));

            var title = CoastOrnate.Label(d.transform, "T", $"{ch:00}. {t.Title}", 24, Ink);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -372f), new Vector2(0f, 34f));
            var sub = CoastOrnate.Label(d.transform, "S", $"{AlbumTable.Artist} · {t.KindLabel} · {GradeText(Collection.TrackGrade(ch))}", 14, new Color(0.45f, 0.4f, 0.38f));
            Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -400f), new Vector2(0f, 22f));

            // 가사 (ko | roman | en 3줄, Resources/CoastRun/Lyrics_CHnn.txt)
            var lyr = Resources.Load<TextAsset>(ArtAssets.ResourceRoot + t.Lyrics);
            var box = MakeScroll(d.transform, out var list, new Vector2(20f, 90f), new Vector2(-20f, -420f));
            string text = lyr != null ? FormatLyrics(lyr.text) : Loc.T("(가사 준비 중 — Suno 곡이 들어오면 함께 채워진다)", "(Lyrics coming with the Suno track)");
            var body = CoastOrnate.Label(list, "L", text, 15, Ink, TextAnchor.UpperCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap; body.verticalOverflow = VerticalWrapMode.Overflow;
            Top(body.rectTransform, 0f, 900f, 0f);
            list.sizeDelta = new Vector2(0f, 900f);

            bool full = AlbumTable.FullVersion(Collection.TrackGrade(ch));
            CoastOrnate.GlassButton(d.transform, "Play", full ? Loc.T("▶ 재생", "▶ Play") : Loc.T("▶ 1절 미리듣기", "▶ Preview"), new Vector2(0.5f, 0f), new Vector2(-110f, 40f), new Vector2(200f, 44f), () => PlayTrack(ch, full), 0.5f, 17, true);
            CoastOrnate.GlassButton(d.transform, "Back", Loc.T("← 목록", "← Back"), new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(200f, 44f), () => { _preview.Stop(); CloseDetail(); }, 0.45f, 17, false);
        }

        private void PlayTrack(int ch, bool full)
        {
            var t = AlbumTable.Get(ch);
            var clip = CoastBgmLibrary.Load(t.Clip) ?? CoastBgmLibrary.Load(CoastBgmLibrary.ChapterStem(Timeline.ArcOf(ch), 0));
            if (clip == null) { Toast(Loc.T("아직 음원이 없어요.", "No audio yet.")); return; }
            _preview.Stop(); _preview.clip = clip; _preview.time = 0f; _preview.Play();
            if (!full) StartCoroutine(StopAfter(40f));
            CoastAudioManager.Instance?.SetBedMuted(true);
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
            var title = CoastOrnate.Label(_content, "Cnt", Loc.T($"바인더 {_cardPage + 1}/{pages}  ·  {Collection.CardsOwned}/{PhotocardTable.Count}장", $"Binder {_cardPage + 1}/{pages}  ·  {Collection.CardsOwned}/{PhotocardTable.Count}"), 16, new Color(1f, 0.92f, 0.75f));
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(0f, 26f));
            var binder = CoastUiArt.Panel(_content, "Binder", new Color(0.35f, 0.22f, 0.16f, 0.95f), 18);
            binder.rectTransform.anchorMin = new Vector2(0.5f, 1f); binder.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            binder.rectTransform.pivot = new Vector2(0.5f, 1f);
            binder.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            binder.rectTransform.sizeDelta = new Vector2(660f, 900f);
            float cw = 196f, chh = 280f, gap = 14f;
            for (int i = 0; i < perPage; i++)
            {
                int id = _cardPage * perPage + i + 1;
                if (id > PhotocardTable.Count) break;
                int col = i % 3, row = i / 3;
                var slot = CoastUiArt.Panel(binder.transform, "Slot" + id, new Color(0f, 0f, 0f, 0.25f), 10);
                slot.rectTransform.anchorMin = slot.rectTransform.anchorMax = new Vector2(0f, 1f);
                slot.rectTransform.pivot = new Vector2(0f, 1f);
                slot.rectTransform.anchoredPosition = new Vector2(20f + col * (cw + gap), -18f - row * (chh + gap));
                slot.rectTransform.sizeDelta = new Vector2(cw, chh);
                slot.raycastTarget = true;
                var card = PhotocardTable.Get(id);
                bool has = Collection.HasCard(id);
                bool paywalled = card.chapter > Collection.FreeChapters && !Collection.AlbumOwned || (card.kind != CardKind.Chapter && card.id > 21 && !Collection.AlbumOwned);
                var img = CoastHudLayout.MakeImage(slot.transform, "Img", Vector2.zero, Vector2.one, new Vector2(6f, 30f), new Vector2(-6f, -6f), has ? Color.white : new Color(0.2f, 0.18f, 0.2f));
                img.raycastTarget = false;
                if (has)
                {
                    var tex = ArtAssets.LoadTexture(card.Image) ?? ArtAssets.LoadTexture(card.FallbackImage);
                    if (tex != null) { img.sprite = CoastUiArt.AsSprite(tex, 100f); img.preserveAspect = false; }
                }
                else
                {
                    var q = CoastOrnate.Label(img.transform, "Q", paywalled ? "🔒" : "?", 40, new Color(1f, 1f, 1f, 0.35f));
                    Place(q.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 60f));
                }
                var cap = CoastOrnate.Label(slot.transform, "Cap", has ? (Collection.CardSigned(id) ? "★ " : "") + card.Name : (paywalled ? Loc.T("앨범 구매", "Album") : card.Hint), 11, has ? Color.white : new Color(1f, 1f, 1f, 0.6f));
                Place(cap.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), new Vector2(0f, 24f));
                cap.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (Collection.CardIsNew(id))
                {
                    var n = CoastUiArt.Panel(slot.transform, "New", CoastOrnate.Red, 8);
                    n.rectTransform.anchorMin = n.rectTransform.anchorMax = new Vector2(1f, 1f);
                    n.rectTransform.anchoredPosition = new Vector2(-4f, -4f); n.rectTransform.pivot = new Vector2(1f, 1f); n.rectTransform.sizeDelta = new Vector2(44f, 20f);
                    var nl = CoastOrnate.Label(n.transform, "T", "NEW", 11, Color.white); Place(nl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                int cid = id;
                var b = slot.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { if (has) OpenCard(cid); else if (paywalled) ShowPaywall(); else Toast(card.Hint); });
            }
            CoastOrnate.GlassButton(_content, "Prev", "◀", new Vector2(0.5f, 0f), new Vector2(-70f, 20f), new Vector2(110f, 40f), () => { _cardPage = Mathf.Max(0, _cardPage - 1); Refresh(); }, 0.45f, 18, false);
            CoastOrnate.GlassButton(_content, "Next", "▶", new Vector2(0.5f, 0f), new Vector2(70f, 20f), new Vector2(110f, 40f), () => { _cardPage = Mathf.Min(pages - 1, _cardPage + 1); Refresh(); }, 0.45f, 18, false);
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
            var front = CoastUiArt.Panel(holder, "Front", Color.white, 18);
            Stretch(front.rectTransform);
            var tex = ArtAssets.LoadTexture(card.Image) ?? ArtAssets.LoadTexture(card.FallbackImage);
            var img = CoastHudLayout.MakeImage(front.transform, "Img", Vector2.zero, Vector2.one, new Vector2(12f, 60f), new Vector2(-12f, -12f), Color.white);
            if (tex != null) img.sprite = CoastUiArt.AsSprite(tex, 100f);
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
                    var lt = CoastOrnate.Label(lockF.transform, "T", Loc.T($"🔒 팬아트 #{i:00} — 별 {i * MissionTable.StarsPerFanArt}개에 열려요", $"🔒 Fan art #{i:00} — opens at {i * MissionTable.StarsPerFanArt} stars"), 16, new Color(1f, 0.92f, 0.75f));
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
                14, Ink, TextAnchor.MiddleLeft);
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
                Top(row.rectTransform, y, 48f, 8f);
                var t = CoastOrnate.Label(row.transform, "T", (has ? "🏆  " : "○  ") + a.Name, 16, has ? Ink : new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleLeft);
                CoastOrnate.Stretch(t.rectTransform, 14f, 0f, -220f, 0f);
                var h = CoastOrnate.Label(row.transform, "H", a.Hint, 12, has ? new Color(0.45f, 0.4f, 0.38f) : new Color(0.65f, 0.6f, 0.58f), TextAnchor.MiddleRight);
                CoastOrnate.Stretch(h.rectTransform, 0f, 0f, -12f, 0f);
                y += 54f;
            }
            list.sizeDelta = new Vector2(0f, y + 20f);
        }

        // ── 구매 패널 ──────────────────────────────────────────────────
        private void ShowPaywall()
        {
            var d = MakeDetail();
            var t = CoastOrnate.Label(d.transform, "T", Loc.T("디지털 앨범 『너와 나의 주파수』", "Digital Album “Our Frequency”"), 22, Ink);
            Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(0f, 34f));
            var body = CoastOrnate.Label(d.transform, "B",
                Loc.T("봄 시즌(1~5챕터·5트랙·포토카드 5장)은 무료예요.\n\n앨범을 구매하면\n· 여름·가을·겨울 15챕터\n· 20트랙 전곡 + 가사\n· 포토카드 30장 바인더\n· 엔딩 2종\n이 모두 열립니다. 한 번 결제, 광고 없음.\n\n" + AlbumTable.Artist + " — " + AlbumTable.ArtistTag + " · 이야기는 사람이 썼습니다.",
                      "Spring (chapters 1–5, 5 tracks, 5 photocards) is free.\n\nThe album unlocks\n· Summer, Autumn, Winter — 15 chapters\n· all 20 tracks with lyrics\n· the 30-card photocard binder\n· both endings.\nOne purchase, no ads.\n\n" + AlbumTable.ArtistEn + " — " + AlbumTable.ArtistTag + " · the story is written by humans."),
                16, Ink, TextAnchor.UpperLeft);
            Place(body.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -320f), new Vector2(-60f, 500f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            CoastOrnate.GlassButton(d.transform, "Buy", Loc.T($"앨범 구매  {Collection.PriceLabel}", $"Buy album  {Collection.PriceLabel}"), new Vector2(0.5f, 0f), new Vector2(-110f, 40f), new Vector2(200f, 46f), () =>
            {
                IapBridge.Purchase(IapBridge.AlbumProductId, ok =>
                {
                    if (ok) { Collection.GrantAlbum(); CoastAudioManager.PlayAnywhere(CoastSfx.Purchase); Toast(Loc.T("고마워요! 전곡이 열렸어요.", "Thank you! Everything is unlocked.")); CloseDetail(); Refresh(); }
                    else Toast(Loc.T("결제가 완료되지 않았어요.", "Purchase not completed."));
                });
            }, 0.5f, 17, true);
            CoastOrnate.GlassButton(d.transform, "Restore", Loc.T("복원 / 닫기", "Restore / Close"), new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(200f, 46f), () => { IapBridge.Restore(ok => { if (ok) { Collection.GrantAlbum(); Refresh(); } }); CloseDetail(); }, 0.45f, 16, false);
        }

        public static void OpenPaywall() { if (_active == null) Open(null, 0); _active.ShowPaywall(); }

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
