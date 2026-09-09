using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 37차: 타이틀 더보기 › 레코드 — 컷씬 음악 M1~M7 을 레코드판 7장으로. 해금된 판은 뱅글뱅글 돌고, 누르면 그 곡이 나온다.
    /// 잠긴 판은 회색 + 해금 조건(롱컷 N / S급 18/20). 그림 없이 전부 코드로 그린다(RecordTable 참고).
    public class RecordsUI : MonoBehaviour
    {
        public static bool IsOpen => _active != null;
        private static RecordsUI _active;

        public static void Open(Action onClose)
        {
            if (_active != null) return;
            var go = new GameObject("RecordsUI");
            _active = go.AddComponent<RecordsUI>();
            _active._onClose = onClose;
            _active.Build();
        }

        private Action _onClose;
        private Canvas _canvas;
        private AudioSource _player;
        private MetaProfile _p;
        private readonly List<Disc> _discs = new List<Disc>();
        private Disc _playing;
        private Text _nowTitle, _nowSub;
        private Image _nowBar;
        private Image _nowPanel;
        private float _t;

        private class Disc
        {
            public RecordTable.Track track;
            public RectTransform root, spin;
            public Image glow, vinyl, label, newDot;
            public Text name, status;
            public bool unlocked;
            public float angle, speed;
            public float scale = 1f;
        }

        // ── 그림: 원 텍스처(안티에일리어스) ────────────────────────────────
        private static Sprite _circle, _vinyl;
        private static Sprite Circle()
        {
            if (_circle != null) return _circle;
            int s = 128; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            float c = (s - 1) * 0.5f, R = s * 0.5f - 1f;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(R - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _circle;
        }

        /// 비닐 판: 검정 바탕에 홈(동심원 명암) + 위쪽 왼편에 비스듬한 광택. 가운데(라벨 자리)는 투명.
        private static Sprite Vinyl()
        {
            if (_vinyl != null) return _vinyl;
            int s = 320; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            float c = (s - 1) * 0.5f, R = s * 0.5f - 1f;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float r = d / R;
                float aOut = Mathf.Clamp01(R - d + 0.5f);
                float aIn = Mathf.Clamp01(d - R * 0.34f + 0.5f);   // 라벨 자리 뚫기
                float a = aOut * aIn;
                if (a <= 0f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                // 홈: 반지름을 따라 촘촘한 명암. 바깥 테두리와 라벨 가장자리는 민무늬 띠.
                float groove = 0.5f + 0.5f * Mathf.Sin(r * 260f);
                float lum = 0.09f + 0.045f * groove;
                if (r > 0.96f || r < 0.40f) lum = 0.10f;
                // 광택: 각도에 따라 두 군데(좌상·우하) 부드럽게 밝아진다
                float ang = Mathf.Atan2(dy, dx);
                float shine = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang - 2.3f)), 6f) * 0.22f + Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang + 0.85f)), 8f) * 0.10f;
                shine *= Mathf.SmoothStep(0f, 1f, (r - 0.38f) / 0.25f);
                lum += shine;
                tex.SetPixel(x, y, new Color(lum, lum, lum + 0.012f, a));
            }
            tex.Apply();
            _vinyl = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _vinyl;
        }

        private static Image Img(Transform parent, string name, Sprite sp, Color col, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.GetComponent<Image>(); im.sprite = sp; im.color = col; im.raycastTarget = false;
            return im;
        }

        private static Text Txt(Transform parent, string name, string s, int size, Color col, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter, bool bold = true)
        {
            var t = CoastHudLayout.MakeText(parent, name, s, size, align, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = box;
            t.color = col; t.raycastTarget = false; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void Build()
        {
            _p = GameManager.I != null ? GameManager.I.Profile : null;
            _canvas = CoastUiCanvas.Create("RecordsCanvas", 470);
            var root = CoastUiCanvas.Root(_canvas);
            _player = gameObject.AddComponent<AudioSource>();
            _player.playOnAwake = false; _player.spatialBlend = 0f; _player.loop = true; _player.volume = 0.85f;

            // 배경: 짙은 밤색 → 아래로 갈수록 따뜻하게. 레터박스 밖까지 덮는다.
            var bg = CoastHudLayout.MakeImage(root, "Bg", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), new Color(0.09f, 0.07f, 0.12f, 1f));
            bg.raycastTarget = true;
            var warm = CoastHudLayout.MakeImage(root, "Warm", new Vector2(0f, 0f), new Vector2(1f, 0.45f),
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, 0f), new Color(0.32f, 0.16f, 0.12f, 0.35f));
            warm.raycastTarget = false;
            // 은은한 점광(레코드 뒤 조명)
            Img(root, "Light", Circle(), new Color(1f, 0.85f, 0.6f, 0.07f), new Vector2(0f, 60f), new Vector2(900f, 900f));

            // 제목
            var title = Txt(root, "Title", Loc.T("레코드", "Records"), 34, new Color(1f, 0.95f, 0.85f), new Vector2(0f, 560f), new Vector2(500f, 60f));
            CoastUiArt.OutlineText(title, new Color(0.35f, 0.2f, 0.1f, 0.8f), 2f);
            int unlocked = RecordTable.UnlockedCount(_p), sc = RecordTable.SCount(_p);
            string sub = Loc.T($"해금 {unlocked} / {RecordTable.All.Length}   ·   S급 {sc} / {RecordTable.Chapters}", $"Unlocked {unlocked} / {RecordTable.All.Length}   ·   Rank S {sc} / {RecordTable.Chapters}");
            if (_p != null && _p.devUnlockAll) sub += Loc.T("   ·   비밀코드", "   ·   secret code");
            Txt(root, "Sub", sub, 16, new Color(1f, 0.9f, 0.75f, 0.7f), new Vector2(0f, 515f), new Vector2(600f, 30f), TextAnchor.MiddleCenter, false);
            Txt(root, "Hint", Loc.T("롱컷 하나에 레코드 하나 · 보너스 3곡은 20챕터 S급 90% 이상", "One record per long cut · 3 bonus tracks at 90% Rank S"), 13,
                new Color(1f, 0.9f, 0.75f, 0.45f), new Vector2(0f, 488f), new Vector2(640f, 26f), TextAnchor.MiddleCenter, false);

            // 판 7장: 3열 × 3행(마지막 행은 1장 + 지금 재생 패널)
            float colW = 226f, disc = 196f;
            float[] rowY = { 320f, 60f, -200f };
            for (int i = 0; i < RecordTable.All.Length; i++)
            {
                var t = RecordTable.All[i];
                int row = i / 3, col = i % 3;
                Vector2 pos = row < 2 ? new Vector2((col - 1) * colW, rowY[row]) : new Vector2(-colW, rowY[2]);
                _discs.Add(BuildDisc(root, t, pos, disc));
            }

            // 지금 재생 패널(마지막 행 오른쪽 2칸)
            _nowPanel = CoastUiArt.Panel(root, "Now", new Color(0.05f, 0.04f, 0.08f, 0.55f), 18);
            var nrt = _nowPanel.rectTransform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = new Vector2(colW * 0.5f + 8f, rowY[2]); nrt.sizeDelta = new Vector2(colW * 2f - 36f, disc + 10f);
            _nowPanel.raycastTarget = false;
            _nowTitle = Txt(_nowPanel.transform, "T", Loc.T("판을 눌러 봐", "Tap a record"), 20, new Color(1f, 0.95f, 0.85f), new Vector2(0f, 46f), new Vector2(colW * 2f - 60f, 60f));
            _nowSub = Txt(_nowPanel.transform, "S", Loc.T("해금된 레코드는 돌아간다", "Unlocked records spin"), 14, new Color(1f, 0.9f, 0.75f, 0.65f), new Vector2(0f, -4f), new Vector2(colW * 2f - 60f, 60f), TextAnchor.MiddleCenter, false);
            var track = CoastUiArt.Panel(_nowPanel.transform, "Track", new Color(1f, 1f, 1f, 0.12f), 4);
            var trt = track.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f); trt.anchoredPosition = new Vector2(0f, 26f); trt.sizeDelta = new Vector2(colW * 2f - 80f, 8f);
            track.raycastTarget = false;
            _nowBar = CoastUiArt.Panel(track.transform, "Fill", new Color(1f, 0.8f, 0.45f, 0.95f), 4);
            var brt = _nowBar.rectTransform; brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 0.5f);
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero; brt.sizeDelta = new Vector2(0f, 0f);
            _nowBar.raycastTarget = false;

            // 닫기
            CoastOrnate.GlassButton(root, "Close", Loc.T("닫기", "Close"), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(260f, 56f), Close, 0.42f, 22, false);
        }

        private Disc BuildDisc(Transform parent, RecordTable.Track t, Vector2 pos, float d)
        {
            var disc = new Disc { track = t, unlocked = RecordTable.IsUnlocked(_p, t) };
            var go = new GameObject("Rec" + t.num, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            disc.root = go.GetComponent<RectTransform>();
            disc.root.anchorMin = disc.root.anchorMax = new Vector2(0.5f, 0.5f);
            disc.root.anchoredPosition = pos; disc.root.sizeDelta = new Vector2(d + 20f, d + 70f);
            var hit = go.GetComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0.02f); hit.raycastTarget = true;
            var btn = go.GetComponent<Button>(); btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => Tap(disc));

            // 뒤 조명(재생 중일 때만 보인다)
            disc.glow = Img(go.transform, "Glow", Circle(), new Color(t.label.r, t.label.g, t.label.b, 0f), new Vector2(0f, 24f), new Vector2(d * 1.35f, d * 1.35f));
            // 그림자
            Img(go.transform, "Shadow", Circle(), new Color(0f, 0f, 0f, 0.45f), new Vector2(4f, 16f), new Vector2(d, d));
            // 회전하는 부분
            var spinGo = new GameObject("Spin", typeof(RectTransform));
            spinGo.transform.SetParent(go.transform, false);
            disc.spin = spinGo.GetComponent<RectTransform>();
            disc.spin.anchorMin = disc.spin.anchorMax = new Vector2(0.5f, 0.5f);
            disc.spin.anchoredPosition = new Vector2(0f, 24f); disc.spin.sizeDelta = new Vector2(d, d);
            disc.vinyl = Img(disc.spin, "Vinyl", Vinyl(), Color.white, Vector2.zero, new Vector2(d, d));
            disc.label = Img(disc.spin, "Label", Circle(), t.label, Vector2.zero, new Vector2(d * 0.34f, d * 0.34f));
            // 라벨 위 글자(곡 번호) + 라벨 무늬 한 줄 + 가운데 구멍
            Img(disc.spin, "Ring", Circle(), new Color(1f, 1f, 1f, 0.22f), Vector2.zero, new Vector2(d * 0.28f, d * 0.28f));
            Img(disc.spin, "RingIn", Circle(), t.label, Vector2.zero, new Vector2(d * 0.25f, d * 0.25f));
            var num = Txt(disc.spin, "Num", "M" + t.num, 15, new Color(1f, 1f, 1f, 0.95f), new Vector2(0f, d * 0.075f), new Vector2(d * 0.3f, 24f));
            CoastUiArt.OutlineText(num, new Color(0f, 0f, 0f, 0.35f), 1f);
            Img(disc.spin, "Hole", Circle(), new Color(0.06f, 0.05f, 0.08f, 1f), new Vector2(0f, -d * 0.04f), new Vector2(d * 0.05f, d * 0.05f));

            // 이름 / 상태
            disc.name = Txt(go.transform, "Name", Loc.IsKo ? t.ko : t.en, 15, new Color(1f, 0.95f, 0.85f), new Vector2(0f, -d * 0.5f + 6f), new Vector2(d + 30f, 40f));
            CoastUiArt.OutlineText(disc.name, new Color(0f, 0f, 0f, 0.6f), 1.2f);
            string st = disc.unlocked ? (t.bonus ? Loc.T("보너스", "Bonus") : Loc.IsKo ? t.unlockKo : t.unlockEn)
                                      : Loc.T("잠김 · ", "Locked · ") + (Loc.IsKo ? t.unlockKo : t.unlockEn);
            disc.status = Txt(go.transform, "Status", st, 11, disc.unlocked ? new Color(1f, 0.85f, 0.55f, 0.85f) : new Color(1f, 1f, 1f, 0.45f),
                new Vector2(0f, -d * 0.5f - 18f), new Vector2(d + 30f, 30f), TextAnchor.MiddleCenter, false);

            // 새 레코드 점
            disc.newDot = Img(go.transform, "New", Circle(), new Color(1f, 0.3f, 0.3f, 1f), new Vector2(d * 0.42f, d * 0.42f + 24f), new Vector2(18f, 18f));
            disc.newDot.gameObject.SetActive(disc.unlocked && RecordTable.IsNew(_p, t));

            if (!disc.unlocked)
            {
                disc.vinyl.color = new Color(0.55f, 0.55f, 0.58f, 0.75f);
                disc.label.color = new Color(0.45f, 0.45f, 0.48f, 1f);
                disc.name.color = new Color(1f, 1f, 1f, 0.55f);
                foreach (var im in disc.spin.GetComponentsInChildren<Image>()) if (im.name == "RingIn") im.color = new Color(0.45f, 0.45f, 0.48f, 1f);
                disc.speed = 0f;
            }
            else disc.speed = 24f + t.num * 3f;   // 판마다 조금씩 다른 속도로 돈다
            disc.angle = UnityEngine.Random.Range(0f, 360f);
            return disc;
        }

        private void Tap(Disc d)
        {
            if (!d.unlocked)
            {
                CoastToast.Show(Loc.T($"잠김 — {d.track.unlockKo}", $"Locked — {d.track.unlockEn}"));
                d.scale = 0.94f;
                return;
            }
            if (_playing == d) { StopPlay(); return; }
            var clip = CoastBgmLibrary.Load(d.track.Clip);
            if (clip == null) { CoastToast.Show(Loc.T("음악 파일이 없어 (Resources/CoastRun/BGM/" + d.track.Clip + ")", "Missing " + d.track.Clip)); return; }
            if (_playing != null) _playing.speed = 24f + _playing.track.num * 3f;
            _playing = d;
            _player.clip = clip; _player.pitch = 1f; _player.Play();
            d.speed = 140f; d.scale = 1.08f;
            RecordTable.MarkSeen(_p, d.track);
            d.newDot.gameObject.SetActive(false);
            _nowTitle.text = "♪ " + (Loc.IsKo ? d.track.ko : d.track.en);
            _nowSub.text = (Loc.IsKo ? d.track.noteKo : d.track.noteEn) + $"  ·  {Mathf.FloorToInt(clip.length / 60f)}:{Mathf.FloorToInt(clip.length % 60f):00}";
        }

        private void StopPlay()
        {
            if (_playing != null) { _playing.speed = 24f + _playing.track.num * 3f; _playing = null; }
            _player.Stop();
            _nowTitle.text = Loc.T("판을 눌러 봐", "Tap a record");
            _nowSub.text = Loc.T("해금된 레코드는 돌아간다", "Unlocked records spin");
            _nowBar.rectTransform.sizeDelta = new Vector2(0f, 0f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime; _t += dt;
            foreach (var d in _discs)
            {
                d.angle -= d.speed * dt;
                d.spin.localRotation = Quaternion.Euler(0f, 0f, d.angle);
                float target = d == _playing ? 1.08f : 1f;
                d.scale = Mathf.Lerp(d.scale, target, dt * 8f);
                d.root.localScale = Vector3.one * d.scale;
                var gc = d.glow.color; gc.a = Mathf.Lerp(gc.a, d == _playing ? 0.30f + 0.08f * Mathf.Sin(_t * 4f) : 0f, dt * 6f); d.glow.color = gc;
                if (d.newDot.gameObject.activeSelf) d.newDot.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(_t * 6f));
            }
            if (_playing != null && _player.clip != null)
            {
                float w = (_nowPanel.rectTransform.sizeDelta.x - 44f) * Mathf.Clamp01(_player.time / _player.clip.length);
                _nowBar.rectTransform.sizeDelta = new Vector2(w, 0f);
            }
            if (CoastRemoteKeys.Down(KeyCode.Escape)) Close();
        }

        private void Close()
        {
            if (_p != null && GameManager.I != null) GameManager.I.WriteProfileNow();
            _player.Stop();
            if (_canvas != null) Destroy(_canvas.gameObject);
            var cb = _onClose; _onClose = null;
            if (_active == this) _active = null;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
