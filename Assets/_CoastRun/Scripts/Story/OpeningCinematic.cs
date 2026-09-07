using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoastRun
{
    /// 시네마틱 오프닝 — 1분 37초. 「돌아온 제주」(BGM_Opening, 1:33부터 페이드아웃) 길이에 맞춘다.
    /// 12차: 어린 시절 제주의 여름 — 돌담길·구슬치기·딱지치기·첫사랑 — 을 앞에 넣어 9컷으로. 컷마다 StreamingAssets/Opening/<clip>.mp4 가 있으면
    /// 그 길이만큼(10초) 재생하고, 없으면 tex 스틸(그것도 없으면 fallback 배경)을 dur 초 켄번즈. 새 컷의 영상·스틸은 Tools/KlingGen/make_opening_memory.py 로 뽑는다.
    /// 첫 실행에 자동 재생, 타이틀의 「오프닝」 버튼으로 다시 볼 수 있다. 탭 = 다음 컷, 길게 = 스킵.
    public class OpeningCinematic : MonoBehaviour
    {
        public const string SeenKey = "CoastRun_OpeningSeen";
        public static bool IsPlaying { get; private set; }

        /// 전체 길이(초). 음악(1:37)에 맞춰 타이틀 카드를 이 시각까지 잡아 두고 마지막 0.8초에 암전한다.
        public const float TargetLength = 97f;
        private const float FinalFade = 0.8f;

        private struct Shot
        {
            public string clip, tex, fallback, caption; public float dur; public Vector2 from, to;
            public Shot(string clip, string tex, string fallback, string caption, float dur, Vector2 from, Vector2 to)
            { this.clip = clip; this.tex = tex; this.fallback = fallback; this.caption = caption; this.dur = dur; this.from = from; this.to = to; }
        }

        // 9컷 × 10초 = 90초 + 타이틀 카드 ≈ 7초. 영상이 없는 컷은 dur 초 스틸.
        private static readonly Shot[] Shots =
        {
            new Shot("open_1", "Cut_Open_1", null, "열두 살 봄, 송전탑 아래서 처음 만났다.", 10f, new Vector2(1.08f, 0.02f), new Vector2(1.18f, -0.02f)),
            new Shot("open_7", "Cut_Open_4", "BG_CoastRoad", "제주의 여름은 짧았고,\n우리는 늘 바빴다.", 10f, new Vector2(1.16f, -0.02f), new Vector2(1.06f, 0.02f)),
            new Shot("open_8", "Cut_Open_5", "BG_DoyunHouse", "유리구슬 한 알에\n온 세상을 걸던 나이.", 10f, new Vector2(1.05f, 0.0f), new Vector2(1.16f, 0.03f)),
            new Shot("open_9", "Cut_Open_6", "BG_HanulHouse", "딱지를 다 잃어도\n그 애가 웃으면 괜찮았다.", 10f, new Vector2(1.14f, 0.0f), new Vector2(1.04f, 0.02f)),
            new Shot("open_2", "Cut_Open_2", null, "그 애는 늘 내 앞에 섰다.\n한 번도 이유를 말하지 않고.", 10f, new Vector2(1.16f, -0.02f), new Vector2(1.06f, 0.02f)),
            new Shot("open_10", "Cut_Open_7", "BG_BusStop", "말하지 못한 게 하나 있었다.\n그때도, 지금도.", 10f, new Vector2(1.06f, 0.02f), new Vector2(1.16f, -0.02f)),
            new Shot("open_3", "Cut_Open_3", null, "여섯 해 뒤, 그가 돌아왔다.\n딱 1년만.", 10f, new Vector2(1.05f, 0.0f), new Vector2(1.16f, 0.03f)),
            new Shot("open_4", "Cut_CH01_Close", null, "노을이 지기 전에 송전탑까지 달리면\n들린다는 목소리가 있다.", 10f, new Vector2(1.14f, 0.0f), new Vector2(1.04f, 0.02f)),
            new Shot("open_6", "Cut_END_A3", null, "열아홉 살, 마지막 1년의 이야기.", 10f, new Vector2(1.04f, 0.0f), new Vector2(1.14f, 0.0f)),
        };

        public static void Play(Action onDone)
        {
            var go = new GameObject("OpeningCinematic");
            DontDestroyOnLoad(go);
            go.AddComponent<OpeningCinematic>().Begin(onDone);
        }

        private Action _onDone;
        private Canvas _canvas;
        private Image _a, _b;
        private Image _fader;
        private Text _caption;
        private CanvasGroup _titleCg;
        private AudioSource _music;
        private RawImage _video;
        private VideoPlayer _player;
        private RenderTexture _rt;
        private bool _skip;
        private Button _skipBtn;
        private float _hold;

        private void Begin(Action onDone)
        {
            _onDone = onDone;
            IsPlaying = true;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            BuildUi();
            StartCoroutine(Run());
        }

        private void BuildUi()
        {
            _canvas = CoastUiCanvas.Create("OpeningCanvas", 480);
            DontDestroyOnLoad(_canvas.gameObject);
            var root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad + 400f;

            var black = CoastHudLayout.MakeImage(root, "Black", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.black);
            black.raycastTarget = true;

            _a = MakeShot(root, "ShotA");
            _b = MakeShot(root, "ShotB");

            // Kling 영상(StreamingAssets/Opening/open_N.mp4)이 있으면 이 표면에 재생한다.
            var vgo = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
            vgo.transform.SetParent(root, false);
            var vrt = vgo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
            vrt.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            _video = vgo.GetComponent<RawImage>();
            _video.raycastTarget = false;
            _video.color = new Color(1f, 1f, 1f, 0f);
            _rt = new RenderTexture(720, 1280, 0);
            _video.texture = _rt;
            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _rt;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.isLooping = false;
            _player.skipOnDrop = true;

            var band = CoastHudLayout.MakeImage(root, "CaptionBand", new Vector2(0f, 0.06f), new Vector2(1f, 0.20f),
                new Vector2(-CoastUiCanvas.HudPad, 0f), new Vector2(CoastUiCanvas.HudPad, 0f), new Color(0f, 0f, 0f, 0.42f));
            band.raycastTarget = false;
            _caption = CoastOrnate.Label(band.transform, "Caption", "", 27, new Color(1f, 0.97f, 0.9f));
            _caption.lineSpacing = 1.3f;
            CoastUiArt.OutlineText(_caption, new Color(0f, 0f, 0f, 0.7f), 1.6f);

            // 타이틀 카드
            var tgo = new GameObject("Title", typeof(RectTransform), typeof(CanvasGroup));
            tgo.transform.SetParent(root, false);
            CoastOrnate.Stretch(tgo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            _titleCg = tgo.GetComponent<CanvasGroup>();
            _titleCg.alpha = 0f;
            _titleCg.blocksRaycasts = false;
            var t1 = CoastOrnate.Label(tgo.transform, "Main", "너와 나의 주파수", 64, new Color(1f, 0.97f, 0.88f));
            var r1 = t1.rectTransform; r1.anchorMin = r1.anchorMax = new Vector2(0.5f, 0.70f); r1.sizeDelta = new Vector2(680f, 90f);
            t1.fontStyle = FontStyle.Bold;
            CoastUiArt.OutlineText(t1, new Color(0.55f, 0.22f, 0.08f, 0.9f), 2.5f);
            var t2 = CoastOrnate.Label(tgo.transform, "Sub", "우리의 송전탑  ·  COAST RUN", 24, new Color(1f, 0.93f, 0.78f, 0.95f));
            var r2 = t2.rectTransform; r2.anchorMin = r2.anchorMax = new Vector2(0.5f, 0.635f); r2.sizeDelta = new Vector2(600f, 40f);
            CoastUiArt.OutlineText(t2, new Color(0f, 0f, 0f, 0.6f), 1.5f);

            _fader = CoastHudLayout.MakeImage(root, "Fader", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.black);
            _fader.raycastTarget = false;

            // 11차: 매 실행마다 나오므로 '건너뛰기' 버튼(우상단) — 길게 누르기도 그대로.
            _skipBtn = CoastOrnate.MenuButton(root, "Skip", Loc.T("건너뛰기", "Skip"), new Vector2(1f, 1f), new Vector2(-70f, -40f), new Vector2(120f, 46f), () => _skip = true, CoastOrnate.WoodDark, 18);

            var music = new GameObject("OpeningMusic");
            music.transform.SetParent(transform, false);
            _music = music.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.clip = CoastBgmLibrary.Load("BGM_Opening");
            _music.volume = 0.85f;
        }

        private static Image MakeShot(RectTransform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.None;
            rt.sizeDelta = new Vector2(720f + 2f * CoastUiCanvas.HudPad, 1280f + 2f * CoastUiCanvas.HudPad);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        private IEnumerator Run()
        {
            if (_music.clip != null) _music.Play();
            float started = Time.unscaledTime;
            bool cutShort = false;   // 탭으로 컷을 넘겼으면 타이틀 카드를 음악 끝까지 붙들지 않는다
            Image cur = _a, nxt = _b;
            _fader.color = Color.black;
            for (int i = 0; i < Shots.Length && !_skip; i++)
            {
                var s = Shots[i];
                var tex = ArtAssets.LoadTexture(s.tex);
                if (tex == null && !string.IsNullOrEmpty(s.fallback)) tex = ArtAssets.LoadTexture(s.fallback);
                cur.sprite = tex != null ? CoastUiArt.AsSprite(tex, 100f) : null;
                cur.color = tex != null ? Color.white : new Color(0.2f, 0.18f, 0.22f, 1f);
                cur.transform.SetAsLastSibling();
                bool useVideo = false;
                yield return TryPrepareVideo(s.clip, v => useVideo = v);
                if (useVideo)
                {
                    _video.transform.SetAsLastSibling();
                    _video.color = Color.white;
                    _player.Play();
                    cur.color = new Color(1f, 1f, 1f, 0f);
                    nxt.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                    _video.color = new Color(1f, 1f, 1f, 0f);
                _fader.transform.SetAsLastSibling();
                _caption.transform.parent.SetAsLastSibling();
                _titleCg.transform.SetAsLastSibling();
                if (_skipBtn != null) _skipBtn.transform.SetAsLastSibling();
                _caption.text = "";

                // 첫 컷은 암전에서 열고, 이후는 크로스페이드
                float t = 0f;
                bool isLast = i == Shots.Length - 1;
                float dur = useVideo ? Mathf.Max(3f, (float)_player.length - 0.15f) : s.dur;
                while (t < dur && !_skip)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / dur);
                    float sc = Mathf.Lerp(s.from.x, s.to.x, k);
                    float px = Mathf.Lerp(s.from.y, s.to.y, k) * 720f;
                    if (!useVideo)
                    {
                        cur.rectTransform.localScale = Vector3.one * sc;
                        cur.rectTransform.anchoredPosition = new Vector2(px, 0f);
                    }
                    if (i == 0) { var c = _fader.color; c.a = 1f - Mathf.Clamp01(t / 0.9f); _fader.color = c; }
                    else if (t < 0.7f) { var c = nxt.color; c.a = 1f - t / 0.7f; nxt.color = c; }
                    else if (nxt.color.a > 0f) { nxt.color = new Color(1f, 1f, 1f, 0f); }
                    if (t > 0.6f && _caption.text.Length == 0) _caption.text = s.caption;
                    if (isLast && t > dur - 2.4f)
                        _titleCg.alpha = Mathf.Clamp01((t - (dur - 2.4f)) / 0.9f);
                    if (Tapped()) { cutShort = true; break; }
                    yield return null;
                }
                var tmp = cur; cur = nxt; nxt = tmp;
            }

            // 타이틀 카드 유지 후 페이드아웃 — 음악이 끝나는 TargetLength 에 맞춰 잡아 둔다(최소 2.2초). 탭이면 바로.
            _titleCg.alpha = 1f;
            _caption.text = "";
            float h = 0f;
            float hold = cutShort ? 2.2f : Mathf.Max(2.2f, TargetLength - FinalFade - (Time.unscaledTime - started));
            while (h < hold && !_skip)
            {
                h += Time.unscaledDeltaTime;
                if (Tapped()) break;
                yield return null;
            }
            float f = 0f;
            float v0 = _music.volume;
            while (f < FinalFade)
            {
                f += Time.unscaledDeltaTime;
                var c = _fader.color; c.a = Mathf.Clamp01(f / FinalFade); _fader.color = c;
                _music.volume = Mathf.Lerp(v0, 0f, f / FinalFade);
                yield return null;
            }
            Finish();
        }

        /// StreamingAssets/Opening/<clip>.mp4 를 준비한다. 4초 안에 준비되지 않거나 오류면 스틸로 대체.
        private IEnumerator TryPrepareVideo(string clip, Action<bool> result)
        {
            if (_player == null || string.IsNullOrEmpty(clip)) { result(false); yield break; }
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Opening", clip + ".mp4");
            if (Application.platform != RuntimePlatform.Android && !System.IO.File.Exists(path))
            {
                result(false); yield break;
            }
            bool failed = false;
            VideoPlayer.ErrorEventHandler onErr = (vp, msg) => failed = true;
            _player.errorReceived += onErr;
            _player.Stop();
            _player.url = path;
            _player.Prepare();
            float t = 0f;
            while (!_player.isPrepared && !failed && t < 4f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
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
            IsPlaying = false;
            var cb = _onDone; _onDone = null;
            if (_music != null) _music.Stop();
            if (_player != null) _player.Stop();
            if (_rt != null) _rt.Release();
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
