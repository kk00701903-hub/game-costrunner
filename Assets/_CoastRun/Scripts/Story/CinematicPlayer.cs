using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoastRun
{
    /// 68차: 공용 시네마틱 플레이어 — 오프닝과 컷씬 8편이 **같은 재생기**를 쓴다(CinematicTable 대본).
    ///   컷 = 클립(StreamingAssets/Opening/<clip>.mp4, 있으면) 또는 스틸 켄번즈 + 자막 한 줄(+ 회상 태그) · 크로스페이드 · 음악 한 곡 ·
    ///   마무리 카드(오프닝은 게임 제목을 음악 끝까지, 컷씬은 챕터 제목 2.2초). 탭 = 다음 컷, 길게/건너뛰기 = 스킵.
    ///   채도 곡선: 편마다 sat(0~1) — UIDesaturate 셰이더로 스틸을 회색 쪽으로, 회상 컷은 세피아.
    public class CinematicPlayer : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }
        public static string CurrentId { get; private set; }

        public static void Play(string id, Action onDone) => Play(id, onDone, null, null);

        /// 105차(사용자: 「한 컷씬 끝나면 우측 하단에 다음화 이어보기」): 마무리 카드가 떠 있는 동안
        ///   오른쪽 아래에 「다음화 이어보기」가 나타난다. 누르면 onNext(다음 편을 바로 재생),
        ///   안 누르면 예전처럼 onDone(시네마 목록으로). nextLabel 은 버튼 위에 적히는 다음 편 이름.
        public static void Play(string id, Action onDone, string nextLabel, Action onNext)
        {
            var def = CinematicTable.Get(id);
            if (def == null) { Debug.LogWarning("[Cine] 대본 없음: " + id); onDone?.Invoke(); return; }
            if (IsPlaying) { Debug.LogWarning("[Cine] 이미 재생 중: " + CurrentId); onDone?.Invoke(); return; }
            var go = new GameObject("Cinematic_" + id);
            DontDestroyOnLoad(go);
            go.AddComponent<CinematicPlayer>().Begin(def, onDone, nextLabel, onNext);
        }

        private CinematicTable.Def _def;
        private Action _onDone;
        private Canvas _canvas;
        private Image _a, _b, _fader;
        private Material _matA, _matB;
        private Text _caption, _tag;
        private Image _band;   // 85차: 자막 띠 — 긴 자막(>90자)이면 더 높게
        private CanvasGroup _titleCg;
        private Transform _nowPlaying;
        private AudioSource _music;
        private RawImage _video;
        private VideoPlayer _player;
        private RenderTexture _rt;
        private bool _skip;
        private Button _skipBtn;
        // 105차: 다음화 이어보기
        private Action _onNext; private string _nextLabel; private bool _goNext;
        private CanvasGroup _nextCg; private RectTransform _nextRt;
        private float _hold;
        private const float FinalFade = 0.9f;
        // 72차(사용자 「이미지 움직이는 효과」): 빛 입자 · 광선 스윕 · 숨 쉬는 비네트 · 살짝 도는 켄번즈
        private Image _barTop, _barBot, _flash, _grain; private bool _wasSepia;   // 75차: 회상 연출(레터박스·화이트 플래시·그레인)
        private RectTransform _fx; private Image _vignette, _sweep; private readonly RectTransform[] _motes = new RectTransform[26]; private readonly float[] _moteSeed = new float[26];

        private void Begin(CinematicTable.Def def, Action onDone, string nextLabel = null, Action onNext = null)
        {
            _def = def; _onDone = onDone; _onNext = onNext; _nextLabel = nextLabel;
            IsPlaying = true; CurrentId = def.id;
            // 스토리 모드(육성) BGM과 컷씬 BGM이 겹치지 않게 — VnMusic.Cue 와 동일
            TitleAudio.StopMenuGlobal();
            VnMusic.Stop(0f);
            BuildUi();
            StartCoroutine(Run());
        }

        private static Shader DesatShader()
        {
            var s = Shader.Find("CoastRun/UIDesaturate");
            if (s == null) s = Resources.Load<Shader>("CoastRun/Shaders/UIDesaturate");
            return s;
        }

        private void BuildUi()
        {
            _canvas = CoastUiCanvas.Create("CinematicCanvas", 480);
            DontDestroyOnLoad(_canvas.gameObject);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad + 400f;

            var black = CoastHudLayout.MakeImage(root, "Black", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.black);
            black.raycastTarget = true;

            var sh = DesatShader();
            _a = MakeShot(root, "ShotA"); _b = MakeShot(root, "ShotB");
            if (sh != null) { _matA = new Material(sh); _matB = new Material(sh); _a.material = _matA; _b.material = _matB; }

            var vgo = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
            vgo.transform.SetParent(root, false);
            var vrt = vgo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad); vrt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            _video = vgo.GetComponent<RawImage>(); _video.raycastTarget = false; _video.color = new Color(1f, 1f, 1f, 0f);
            _rt = new RenderTexture(720, 1280, 0); _video.texture = _rt;
            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false; _player.renderMode = VideoRenderMode.RenderTexture; _player.targetTexture = _rt;
            _player.audioOutputMode = VideoAudioOutputMode.None; _player.isLooping = false; _player.skipOnDrop = true;

            BuildFx(root, pad);
            // 75차: 회상 연출 — 위아래 검은 레터박스(회상 컷에서 100 까지 내려옴), 진입 화이트 플래시, 필름 그레인
            _barTop = CoastHudLayout.MakeImage(root, "BarTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-pad, 0f), new Vector2(pad, pad), Color.black); _barTop.raycastTarget = false;
            _barBot = CoastHudLayout.MakeImage(root, "BarBot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-pad, -pad), new Vector2(pad, 0f), Color.black); _barBot.raycastTarget = false;
            _grain = CoastHudLayout.MakeImage(root, "Grain", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(1f, 1f, 1f, 0f)); _grain.raycastTarget = false; _grain.sprite = GrainSprite(); _grain.type = Image.Type.Tiled;
            _flash = CoastHudLayout.MakeImage(root, "Flash", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(1f, 0.97f, 0.9f, 0f)); _flash.raycastTarget = false;

            // 자막 띠(아래) + 회상 태그(위)
            var band = CoastHudLayout.MakeImage(root, "CaptionBand", new Vector2(0f, 0.06f), new Vector2(1f, 0.21f),
                new Vector2(-CoastUiCanvas.HudPad, 0f), new Vector2(CoastUiCanvas.HudPad, 0f), new Color(0f, 0f, 0f, 0.46f));
            band.raycastTarget = false; _band = band;
            _caption = CoastOrnate.Label(band.transform, "Caption", "", 27, new Color(1f, 0.97f, 0.9f));
            _caption.lineSpacing = 1.3f; _caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            // 75차: 두 문장 자막(≤64자)이 세 줄로 갈려 마지막 줄에 한두 단어만 남던 것 — 여백 40→26, 최대 27→25 로 두 줄에 맞춘다
            var crt = _caption.rectTransform; crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = new Vector2(26f, 8f); crt.offsetMax = new Vector2(-26f, -8f);
            _caption.resizeTextForBestFit = true; _caption.resizeTextMinSize = 16; _caption.resizeTextMaxSize = CoastHudLayout.Scaled(25);
            CoastUiArt.OutlineText(_caption, new Color(0f, 0f, 0f, 0.7f), 1.6f);
            // 75차(사용자): 회상 태그를 크게(18 → 30) — 레터박스 위 띠 안에 「회상 · 여덟 해 전」
            _tag = CoastOrnate.Label(root, "Tag", "", 30, new Color(1f, 0.90f, 0.68f, 1f));
            var trt = _tag.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.anchoredPosition = new Vector2(0f, -150f); trt.sizeDelta = new Vector2(640f, 56f);
            _tag.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_tag, new Color(0.15f, 0.05f, 0f, 0.85f), 2f);

            // 마무리 카드
            var tgo = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup));
            tgo.transform.SetParent(root, false);
            CoastOrnate.Stretch(tgo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            _titleCg = tgo.GetComponent<CanvasGroup>(); _titleCg.alpha = 0f; _titleCg.blocksRaycasts = false;
            var t1 = CoastOrnate.Label(tgo.transform, "Main", StoryEn.T(_def.cardMain ?? _def.title), _def.gameTitleCard ? 64 : 54, new Color(1f, 0.97f, 0.88f));
            var r1 = t1.rectTransform; r1.anchorMin = r1.anchorMax = new Vector2(0.5f, 0.70f); r1.sizeDelta = new Vector2(680f, 90f);
            t1.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t1, new Color(0.55f, 0.22f, 0.08f, 0.9f), 2.5f);
            if (!string.IsNullOrEmpty(_def.cardSub))
            {
                var t2 = CoastOrnate.Label(tgo.transform, "Sub", StoryEn.T(_def.cardSub), 24, new Color(1f, 0.93f, 0.78f, 0.95f));
                bool multi = _def.cardSub.Contains("\n");   // 75차: MV 카드의 세 줄 안내는 제목과 안 겹치게 조금 더 아래(0.625 → 0.585)
                var r2 = t2.rectTransform; r2.anchorMin = r2.anchorMax = new Vector2(0.5f, multi ? 0.585f : 0.625f); r2.sizeDelta = new Vector2(660f, multi ? 120f : 90f);
                t2.horizontalOverflow = HorizontalWrapMode.Wrap; t2.verticalOverflow = VerticalWrapMode.Overflow; t2.lineSpacing = 1.25f;   // 75차: MV 카드의 긴 스트리밍 안내도 두 줄로
                CoastUiArt.OutlineText(t2, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            }

            _fader = CoastHudLayout.MakeImage(root, "Fader", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.black);
            _fader.raycastTarget = false;
            _skipBtn = CoastOrnate.MenuButton(root, "Skip", Loc.T("건너뛰기", "Skip"), new Vector2(1f, 1f), new Vector2(-70f, -40f), new Vector2(120f, 46f), () => _skip = true, CoastOrnate.WoodDark, 18);
            if (_onNext != null) BuildNextButton(root);

            var music = new GameObject("CineMusic"); music.transform.SetParent(transform, false);
            _music = music.AddComponent<AudioSource>();
            _music.playOnAwake = false; _music.spatialBlend = 0f; _music.volume = 0.85f; _music.loop = true;   // 77차: 컷씬이 곡보다 길어져(1:42~1:57) 루프
            _music.clip = CoastBgmLibrary.Load(_def.bgm);
            _nowPlaying = BuildNowPlaying(root, _def.bgm);
        }

        /// 105차: 오른쪽 아래 「다음화 이어보기 ▶」 — 마무리 카드가 뜰 때까지는 숨어 있다(ShowNext).
        ///   버튼 위 작은 줄에 다음 편 이름을 적어, 무엇으로 넘어가는지 보고 누르게 한다.
        private void BuildNextButton(RectTransform root)
        {
            var go = new GameObject("NextUp", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            _nextRt = go.GetComponent<RectTransform>();
            _nextRt.anchorMin = _nextRt.anchorMax = new Vector2(1f, 0f); _nextRt.pivot = new Vector2(1f, 0f);
            _nextRt.anchoredPosition = new Vector2(-24f, 40f); _nextRt.sizeDelta = new Vector2(280f, 96f);
            _nextCg = go.GetComponent<CanvasGroup>(); _nextCg.alpha = 0f; _nextCg.blocksRaycasts = false;
            go.SetActive(false);

            if (!string.IsNullOrEmpty(_nextLabel))
            {
                var cap = CoastHudLayout.MakeText(_nextRt, "Label", _nextLabel, 15, TextAnchor.MiddleRight,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(-8f, -2f));
                cap.color = new Color(1f, 0.95f, 0.85f, 0.92f);
                cap.resizeTextForBestFit = true; cap.resizeTextMinSize = 10; cap.resizeTextMaxSize = 15;   // 제목이 길면 줄여서 한 줄로
                CoastUiArt.OutlineText(cap, new Color(0f, 0f, 0f, 0.75f), 1.5f);
            }
            CoastOrnate.MenuButton(_nextRt, "Btn", Loc.T("다음화 이어보기 ▶", "Next episode ▶"),
                new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(256f, 54f),
                () => { if (!_goNext) { _goNext = true; CoastAudioManager.PlayAnywhere(CoastSfx.Coin); } },
                CoastOrnate.WoodDark, 20);
        }

        private void ShowNext()
        {
            if (_nextRt == null) return;
            _nextRt.gameObject.SetActive(true);
            _nextRt.SetAsLastSibling();
            if (_nextCg != null) _nextCg.blocksRaycasts = true;
        }

        /// 이어보기 버튼 위를 눌렀으면 「아무 데나 탭 = 넘기기」로 세지 않는다 — 안 그러면 버튼이 눌리기 전에 카드가 끝난다.
        private bool PointerOverNext()
        {
            if (_nextRt == null || !_nextRt.gameObject.activeSelf) return false;
            Vector2 p = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            return RectTransformUtility.RectangleContainsScreenPoint(_nextRt, p, null);
        }

        /// 좌상단 — 지금 흐르는 컷씬 BGM 제목.
        /// 79차(사용자): 남색 알약 대신 **K-POP 러닝과 같은 표시**(KpopNowPlaying) — 보라→분홍 그라데이션 알약 +
        ///   멜로디를 따라 움직이는 이퀄라이저 막대 5개 + 「NOW PLAYING ♫ / 곡명 — 우히&히시」. 자리만 좌상단으로.
        private static Transform BuildNowPlaying(RectTransform root, string bgmKey)
        {
            string title = RecordTable.TitleFromBgm(bgmKey);
            if (string.IsNullOrEmpty(title)) return null;
            string credit = title + " — " + KpopTrackMeta.Artist;
            var np = KpopNowPlaying.Build(root, credit, new Vector2(0f, 1f), new Vector2(10f, -36f));
            return np != null ? np.transform : null;
        }

        private static Image MakeShot(RectTransform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720f + 2f * CoastUiCanvas.HudPad, 1280f + 2f * CoastUiCanvas.HudPad);
            var img = go.GetComponent<Image>(); img.raycastTarget = false; img.preserveAspect = true; img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private static void SetLook(Material m, float sat, bool sepia)
        {
            if (m == null) return;
            m.SetFloat("_Sat", sepia ? 0.15f : sat);
            m.SetFloat("_Sepia", sepia ? 0.85f : 0f);
            m.SetFloat("_Keep", sat < 0.99f ? 1f : 0f);   // 채도를 죽인 편에서만 하트·우비·부표(빨강/주황)를 원색으로
        }

        private IEnumerator Run()
        {
            // 226차: 이 컷씬 그림이 아직 안 받아진 팩에 있으면 받을 때까지 기다림(대개 설치 직후 몇 분 안에 끝남)
            yield return CutPack.EnsureReady(CutPack.IdsOf(_def));
            if (_music.clip != null) _music.Play();
            float started = Time.unscaledTime;
            bool cutShort = false;
            Image cur = _a, nxt = _b; Material curM = _matA, nxtM = _matB;
            _fader.color = Color.black;
            var cuts = _def.cuts;
            for (int i = 0; i < cuts.Length && !_skip; i++)
            {
                var s = cuts[i];
                string cap = StoryEn.T(s.caption);   // 219차: 영어 모드 자막(→ ja·es 번역표)
                var tex = ArtAssets.LoadTexture(s.still);
                if (tex == null && !string.IsNullOrEmpty(s.fallback)) tex = ArtAssets.LoadTexture(s.fallback);
                cur.sprite = tex != null ? CoastUiArt.AsSprite(tex, 100f) : null;
                // 72차: 진짜 크로스페이드 — 새 컷은 위에서 알파 0 으로 시작해 0.8초에 걸쳐 나타난다(전엔 위에 바로 불투명으로 올라와 「탁」 바뀌는 게 깜빡임처럼 보였다)
                cur.color = tex != null ? new Color(1f, 1f, 1f, i == 0 ? 1f : 0f) : new Color(0.2f, 0.18f, 0.22f, 1f);
                SetLook(curM, s.sat >= 0f ? s.sat : _def.sat, s.sepia);   // 85차: 컷별 채도 키프레임(END_TRUE 2컷부터 1.0)
                cur.transform.SetAsLastSibling();
                if (_band != null)
                {   // 85차(v4): 자막이 세 문장 넘는 컷은 띠를 높여 세 줄까지
                    bool longCap = cap != null && cap.Length > (Loc.IsKo ? 90 : 150);
                    _band.rectTransform.anchorMin = new Vector2(0f, longCap ? 0.05f : 0.06f); _band.rectTransform.anchorMax = new Vector2(1f, longCap ? 0.25f : 0.21f);
                }
                cur.rectTransform.localScale = Vector3.one * s.from.x; cur.rectTransform.anchoredPosition = new Vector2(s.from.y * 720f, 0f);
                bool useVideo = false;
                yield return TryPrepareVideo(s.clip, v => useVideo = v);
                if (useVideo)
                {
                    _video.transform.SetAsLastSibling(); _video.color = Color.white; _player.Play();
                    cur.color = new Color(1f, 1f, 1f, 0f); nxt.color = new Color(1f, 1f, 1f, 0f);
                }
                else _video.color = new Color(1f, 1f, 1f, 0f);
                if (_fx != null) { _fx.SetAsLastSibling(); _fx.gameObject.SetActive(!useVideo); }
                _grain.transform.SetAsLastSibling(); _barTop.transform.SetAsLastSibling(); _barBot.transform.SetAsLastSibling(); _flash.transform.SetAsLastSibling();
                if (s.sepia && !_wasSepia) { StartCoroutine(FlashCo()); CoastPrefs.VibrateEvent(); }   // 회상으로 들어갈 때 하얗게 번쩍(109차: 여기서만 진동)
                _wasSepia = s.sepia;
                _fader.transform.SetAsLastSibling();
                _caption.transform.parent.SetAsLastSibling();
                _tag.transform.SetAsLastSibling();
                if (_nowPlaying != null) _nowPlaying.SetAsLastSibling();
                _titleCg.transform.SetAsLastSibling();
                if (_skipBtn != null) _skipBtn.transform.SetAsLastSibling();
                _caption.text = ""; _tag.text = StoryEn.T(s.tag) ?? "";

                float t = 0f; bool isLast = i == cuts.Length - 1;
                float dur = useVideo ? Mathf.Max(3f, (float)_player.length - 0.15f) : s.dur;
                // 136차(사용자): 자막을 통째로 띄우지 않고 「읽어 주듯」 한 글자씩 — 컷 길이에 맞춰 속도 자동
                float capShown = 0f, capCps = CoastRun.Story.TextReveal.Cps(cap, dur - 0.5f);
                _caption.supportRichText = true;
                { var cc0 = _caption.color; cc0.a = 1f; _caption.color = cc0; }
                while (t < dur && !_skip)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    if (!useVideo)
                    {
                        // 72차: 켄번즈를 더 크게(1.5배 폭) + 세로로도 살짝 흐르고 ±0.8° 천천히 돈다
                        // 109차(사용자: 「사진이 좀 움직였으면 — 동영상 말고」): 폭을 2배로, 확대만 하는 컷도 옆으로 흘러 어느 컷이든 움직임이 보이게
                        float sc = Mathf.Lerp(s.from.x, s.to.x, k); sc = 1f + (sc - 1f) * 2.0f;
                        float px = Mathf.Lerp(s.from.y, s.to.y, k) * 720f * 1.8f;
                        if (Mathf.Approximately(s.from.y, s.to.y)) px += Mathf.Sin((t / dur) * Mathf.PI) * (i % 2 == 0 ? 26f : -26f);
                        float py = Mathf.Sin((t / dur) * Mathf.PI) * (i % 2 == 0 ? 22f : -22f) + Mathf.Lerp(-12f, 12f, k) * (i % 3 == 0 ? 1f : -1f);
                        cur.rectTransform.localScale = Vector3.one * sc;
                        cur.rectTransform.anchoredPosition = new Vector2(px, py);
                        cur.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-0.8f, 0.8f, k) * (i % 2 == 0 ? 1f : -1f));
                        if (i > 0 && t < 0.8f) { var c = cur.color; c.a = t / 0.8f; cur.color = c; }
                        else if (i > 0 && cur.color.a < 1f && tex != null) cur.color = Color.white;
                        UpdateFx(t, s.sepia);
                    }
                    UpdateFlashback(s.sepia, dt: Time.unscaledDeltaTime);
                    if (i == 0) { var c = _fader.color; c.a = 1f - Mathf.Clamp01(t / 0.9f); _fader.color = c; }
                    else if (t >= 0.8f && nxt.color.a > 0f) nxt.color = new Color(1f, 1f, 1f, 0f);
                    if (t > 0.5f && !string.IsNullOrEmpty(cap) && capShown < cap.Length + 1f)
                    {
                        capShown += Time.unscaledDeltaTime * capCps;
                        _caption.text = CoastRun.Story.TextReveal.Build(cap, capShown);
                    }
                    if (isLast && t > dur - 2.4f) _titleCg.alpha = Mathf.Clamp01((t - (dur - 2.4f)) / 0.9f);
                    if (Tapped()) { cutShort = true; break; }
                    yield return null;
                }
                var tmp = cur; cur = nxt; nxt = tmp; var tm = curM; curM = nxtM; nxtM = tm;
            }

            _titleCg.alpha = 1f; _caption.text = ""; _tag.text = "";
            float h = 0f;
            float hold = _def.holdToSeconds > 0f && !cutShort ? Mathf.Max(2.2f, _def.holdToSeconds - FinalFade - (Time.unscaledTime - started)) : 2.2f;
            // 105차: 마무리 카드와 함께 「다음화 이어보기」를 띄우고, 누를 틈이 있게 카드를 조금 더 붙잡는다.
            if (_onNext != null && !_skip) { ShowNext(); hold = Mathf.Max(hold, 5f); }
            while (h < hold && !_skip && !_goNext)
            {
                h += Time.unscaledDeltaTime;
                if (_nextCg != null && _nextCg.alpha < 1f) _nextCg.alpha = Mathf.Clamp01(_nextCg.alpha + Time.unscaledDeltaTime / 0.35f);
                if (Tapped() && !PointerOverNext()) break;
                yield return null;
            }
            float f = 0f, v0 = _music.volume;
            while (f < FinalFade)
            {
                f += Time.unscaledDeltaTime;
                var c = _fader.color; c.a = Mathf.Clamp01(f / FinalFade); _fader.color = c;
                _music.volume = Mathf.Lerp(v0, 0f, f / FinalFade);
                // 버튼은 암전과 함께 사라진다 — 손가락을 떼는 순간(onClick)까지는 살아 있게 마지막에 끈다.
                if (_nextCg != null) _nextCg.alpha = Mathf.Clamp01(1f - f / FinalFade);
                yield return null;
            }
            Finish();
        }

        // 75차: 회상 연출 값 — 레터박스 높이(0 ↔ 100), 그레인 깜빡임
        private float _bar;
        private void UpdateFlashback(bool sepia, float dt)
        {
            float target = sepia ? 100f : 0f;
            _bar = Mathf.MoveTowards(_bar, target, dt * 260f);
            if (_barTop != null) _barTop.rectTransform.offsetMin = new Vector2(_barTop.rectTransform.offsetMin.x, -_bar);
            if (_barBot != null) _barBot.rectTransform.offsetMax = new Vector2(_barBot.rectTransform.offsetMax.x, _bar);
            if (_grain != null)
            {
                float a = sepia ? 0.10f + 0.06f * Mathf.PerlinNoise(Time.unscaledTime * 23f, 0.5f) : 0f;
                _grain.color = new Color(1f, 1f, 1f, a);
                _grain.rectTransform.anchoredPosition = sepia ? new Vector2(UnityEngine.Random.Range(-6f, 6f), UnityEngine.Random.Range(-6f, 6f)) : Vector2.zero;
            }
            if (_tag != null && sepia) { float p = 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 2f); _tag.transform.localScale = Vector3.one * p; }
        }

        private IEnumerator FlashCo()
        {
            float t = 0f;
            while (t < 0.55f && _flash != null)
            {
                t += Time.unscaledDeltaTime;
                float a = t < 0.08f ? t / 0.08f : Mathf.Clamp01(1f - (t - 0.08f) / 0.47f);
                _flash.color = new Color(1f, 0.97f, 0.9f, a * 0.85f);
                yield return null;
            }
            if (_flash != null) _flash.color = new Color(1f, 0.97f, 0.9f, 0f);
        }

        private static Sprite _grainSpr;
        private static Sprite GrainSprite()
        {
            if (_grainSpr != null) return _grainSpr;
            int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var rng = new System.Random(75);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float v = (float)rng.NextDouble(); tex.SetPixel(x, y, new Color(v, v, v, v > 0.5f ? (v - 0.5f) * 2f : 0f)); }
            tex.Apply();
            return _grainSpr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// 72차: 스틸 위 움직임 — 떠오르는 빛 입자 26개, 6초마다 지나가는 비스듬한 광선, 숨 쉬는 비네트.
        private void BuildFx(RectTransform root, float pad)
        {
            var fxGo = new GameObject("Fx", typeof(RectTransform));
            fxGo.transform.SetParent(root, false);
            _fx = fxGo.GetComponent<RectTransform>();
            _fx.anchorMin = Vector2.zero; _fx.anchorMax = Vector2.one; _fx.offsetMin = new Vector2(-pad, -pad); _fx.offsetMax = new Vector2(pad, pad);
            _vignette = new GameObject("Vignette", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _vignette.transform.SetParent(_fx, false); _vignette.raycastTarget = false; _vignette.sprite = VignetteSprite();
            var vrt = _vignette.rectTransform; vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            _vignette.color = new Color(0f, 0f, 0f, 0.35f);
            _sweep = new GameObject("Sweep", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _sweep.transform.SetParent(_fx, false); _sweep.raycastTarget = false; _sweep.sprite = SweepSprite();
            var srt = _sweep.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.sizeDelta = new Vector2(260f, 2600f); srt.localRotation = Quaternion.Euler(0f, 0f, 22f);
            _sweep.color = new Color(1f, 0.97f, 0.85f, 0.10f);
            var dot = CoastUiArt.RoundedRect(16);
            for (int i = 0; i < _motes.Length; i++)
            {
                var m = new GameObject("Mote" + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                m.transform.SetParent(_fx, false); m.raycastTarget = false; m.sprite = dot; m.type = Image.Type.Simple;
                var r = m.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                float sz = UnityEngine.Random.Range(5f, 14f); r.sizeDelta = new Vector2(sz, sz);
                m.color = new Color(1f, 0.96f, 0.85f, 0f);
                _motes[i] = r; _moteSeed[i] = UnityEngine.Random.value * 1000f;
            }
        }

        private void UpdateFx(float t, bool sepia)
        {
            if (_fx == null) return;
            float T = Time.unscaledTime;
            if (_vignette != null) _vignette.color = new Color(0f, 0f, 0f, sepia ? 0.45f + 0.05f * Mathf.Sin(T * 0.9f) : 0.30f + 0.06f * Mathf.Sin(T * 0.7f));
            if (_sweep != null)
            {
                float cyc = Mathf.Repeat(T, 7f);                          // 7초마다 한 번, 2.6초 동안 지나간다
                float u = Mathf.Clamp01(cyc / 2.6f);
                _sweep.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-760f, 760f, u), 0f);
                _sweep.color = new Color(1f, 0.97f, 0.85f, cyc < 2.6f ? 0.11f * Mathf.Sin(u * Mathf.PI) : 0f);
            }
            for (int i = 0; i < _motes.Length; i++)
            {
                float sd = _moteSeed[i];
                float life = Mathf.Repeat(T * 0.11f + sd, 1f);           // 약 9초에 화면 아래→위
                float x = Mathf.Repeat(sd * 0.37f, 1f) * 720f - 360f + Mathf.Sin(T * 0.6f + sd) * 26f;
                float y = Mathf.Lerp(-700f, 700f, life);
                _motes[i].anchoredPosition = new Vector2(x, y);
                float a = Mathf.Sin(life * Mathf.PI) * (0.35f + 0.25f * Mathf.Sin(T * 2.3f + sd));
                _motes[i].GetComponent<Image>().color = sepia ? new Color(1f, 0.9f, 0.7f, a * 0.8f) : new Color(1f, 0.97f, 0.88f, a);
            }
        }

        private static Sprite _vigSpr, _swSpr;
        private static Sprite VignetteSprite()
        {
            if (_vigSpr != null) return _vigSpr;
            int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;             // 0 중심 ~ 1.41 모서리
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.75f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return _vigSpr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
        private static Sprite SweepSprite()
        {
            if (_swSpr != null) return _swSpr;
            int w = 64, h = 4; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w; float a = Mathf.Sin(u * Mathf.PI); a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return _swSpr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private IEnumerator TryPrepareVideo(string clip, Action<bool> result)
        {
            if (_player == null || string.IsNullOrEmpty(clip)) { result(false); yield break; }
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Opening", clip + ".mp4");
            if (Application.platform != RuntimePlatform.Android && !System.IO.File.Exists(path)) { result(false); yield break; }
            bool failed = false;
            VideoPlayer.ErrorEventHandler onErr = (vp, msg) => failed = true;
            _player.errorReceived += onErr;
            _player.Stop(); _player.url = path; _player.Prepare();
            float t = 0f;
            while (!_player.isPrepared && !failed && t < 4f) { t += Time.unscaledDeltaTime; yield return null; }
            _player.errorReceived -= onErr;
            result(_player.isPrepared && !failed);
        }

        private bool Tapped()
        {
            bool down = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                        (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.touchCount > 0;
            _hold = held ? _hold + Time.unscaledDeltaTime : 0f;
            if (_hold > 1.0f || Input.GetKeyDown(KeyCode.Escape)) _skip = true;
            return down;
        }

        private void Finish()
        {
            IsPlaying = false; CurrentId = null;
            // 105차: 이어보기를 눌렀으면 목록으로 돌아가지 않고 곧장 다음 편으로.
            var cb = _goNext && _onNext != null ? _onNext : _onDone;
            _onDone = null; _onNext = null;
            if (_music != null) _music.Stop();
            if (_player != null) _player.Stop();
            if (_rt != null) _rt.Release();
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
            cb?.Invoke();
            // 다음 컷씬/러닝으로 이어지지 않고 육성 허브에 남으면 스토리 BGM 복구
            if (!IsPlaying && !ChapterVN.IsPlaying && !OpeningCinematic.IsPlaying
                && SceneManager.GetActiveScene().name == CoastScenes.Raising)
                TitleAudio.PlayRaising();
        }
    }
}
