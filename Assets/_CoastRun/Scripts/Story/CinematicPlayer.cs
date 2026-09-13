using System;
using System.Collections;
using UnityEngine;
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

        public static void Play(string id, Action onDone)
        {
            var def = CinematicTable.Get(id);
            if (def == null) { Debug.LogWarning("[Cine] 대본 없음: " + id); onDone?.Invoke(); return; }
            if (IsPlaying) { Debug.LogWarning("[Cine] 이미 재생 중: " + CurrentId); onDone?.Invoke(); return; }
            var go = new GameObject("Cinematic_" + id);
            DontDestroyOnLoad(go);
            go.AddComponent<CinematicPlayer>().Begin(def, onDone);
        }

        private CinematicTable.Def _def;
        private Action _onDone;
        private Canvas _canvas;
        private Image _a, _b, _fader;
        private Material _matA, _matB;
        private Text _caption, _tag;
        private CanvasGroup _titleCg;
        private AudioSource _music;
        private RawImage _video;
        private VideoPlayer _player;
        private RenderTexture _rt;
        private bool _skip;
        private Button _skipBtn;
        private float _hold;
        private const float FinalFade = 0.9f;
        // 72차(사용자 「이미지 움직이는 효과」): 빛 입자 · 광선 스윕 · 숨 쉬는 비네트 · 살짝 도는 켄번즈
        private Image _barTop, _barBot, _flash, _grain; private bool _wasSepia;   // 75차: 회상 연출(레터박스·화이트 플래시·그레인)
        private RectTransform _fx; private Image _vignette, _sweep; private readonly RectTransform[] _motes = new RectTransform[26]; private readonly float[] _moteSeed = new float[26];

        private void Begin(CinematicTable.Def def, Action onDone)
        {
            _def = def; _onDone = onDone;
            IsPlaying = true; CurrentId = def.id;
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
            band.raycastTarget = false;
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
            var t1 = CoastOrnate.Label(tgo.transform, "Main", _def.cardMain ?? _def.title, _def.gameTitleCard ? 64 : 54, new Color(1f, 0.97f, 0.88f));
            var r1 = t1.rectTransform; r1.anchorMin = r1.anchorMax = new Vector2(0.5f, 0.70f); r1.sizeDelta = new Vector2(680f, 90f);
            t1.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t1, new Color(0.55f, 0.22f, 0.08f, 0.9f), 2.5f);
            if (!string.IsNullOrEmpty(_def.cardSub))
            {
                var t2 = CoastOrnate.Label(tgo.transform, "Sub", _def.cardSub, 24, new Color(1f, 0.93f, 0.78f, 0.95f));
                bool multi = _def.cardSub.Contains("\n");   // 75차: MV 카드의 세 줄 안내는 제목과 안 겹치게 조금 더 아래(0.625 → 0.585)
                var r2 = t2.rectTransform; r2.anchorMin = r2.anchorMax = new Vector2(0.5f, multi ? 0.585f : 0.625f); r2.sizeDelta = new Vector2(660f, multi ? 120f : 90f);
                t2.horizontalOverflow = HorizontalWrapMode.Wrap; t2.verticalOverflow = VerticalWrapMode.Overflow; t2.lineSpacing = 1.25f;   // 75차: MV 카드의 긴 스트리밍 안내도 두 줄로
                CoastUiArt.OutlineText(t2, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            }

            _fader = CoastHudLayout.MakeImage(root, "Fader", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), Color.black);
            _fader.raycastTarget = false;
            _skipBtn = CoastOrnate.MenuButton(root, "Skip", Loc.T("건너뛰기", "Skip"), new Vector2(1f, 1f), new Vector2(-70f, -40f), new Vector2(120f, 46f), () => _skip = true, CoastOrnate.WoodDark, 18);

            var music = new GameObject("CineMusic"); music.transform.SetParent(transform, false);
            _music = music.AddComponent<AudioSource>();
            _music.playOnAwake = false; _music.spatialBlend = 0f; _music.volume = 0.85f; _music.loop = true;   // 77차: 컷씬이 곡보다 길어져(1:42~1:57) 루프
            _music.clip = CoastBgmLibrary.Load(_def.bgm);
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
            if (_music.clip != null) _music.Play();
            float started = Time.unscaledTime;
            bool cutShort = false;
            Image cur = _a, nxt = _b; Material curM = _matA, nxtM = _matB;
            _fader.color = Color.black;
            var cuts = _def.cuts;
            for (int i = 0; i < cuts.Length && !_skip; i++)
            {
                var s = cuts[i];
                var tex = ArtAssets.LoadTexture(s.still);
                if (tex == null && !string.IsNullOrEmpty(s.fallback)) tex = ArtAssets.LoadTexture(s.fallback);
                cur.sprite = tex != null ? CoastUiArt.AsSprite(tex, 100f) : null;
                // 72차: 진짜 크로스페이드 — 새 컷은 위에서 알파 0 으로 시작해 0.8초에 걸쳐 나타난다(전엔 위에 바로 불투명으로 올라와 「탁」 바뀌는 게 깜빡임처럼 보였다)
                cur.color = tex != null ? new Color(1f, 1f, 1f, i == 0 ? 1f : 0f) : new Color(0.2f, 0.18f, 0.22f, 1f);
                SetLook(curM, _def.sat, s.sepia);
                cur.transform.SetAsLastSibling();
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
                if (s.sepia && !_wasSepia) StartCoroutine(FlashCo());   // 회상으로 들어갈 때 하얗게 번쩍
                _wasSepia = s.sepia;
                _fader.transform.SetAsLastSibling();
                _caption.transform.parent.SetAsLastSibling();
                _tag.transform.SetAsLastSibling();
                _titleCg.transform.SetAsLastSibling();
                if (_skipBtn != null) _skipBtn.transform.SetAsLastSibling();
                _caption.text = ""; _tag.text = s.tag ?? "";

                float t = 0f; bool isLast = i == cuts.Length - 1;
                float dur = useVideo ? Mathf.Max(3f, (float)_player.length - 0.15f) : s.dur;
                while (t < dur && !_skip)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    if (!useVideo)
                    {
                        // 72차: 켄번즈를 더 크게(1.5배 폭) + 세로로도 살짝 흐르고 ±0.8° 천천히 돈다
                        float sc = Mathf.Lerp(s.from.x, s.to.x, k); sc = 1f + (sc - 1f) * 1.5f;
                        float px = Mathf.Lerp(s.from.y, s.to.y, k) * 720f * 1.4f;
                        float py = Mathf.Sin((t / dur) * Mathf.PI) * (i % 2 == 0 ? 14f : -14f);
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
                    if (t > 0.5f && _caption.text.Length == 0) _caption.text = s.caption;
                    if (_caption.text.Length > 0) { var cc = _caption.color; cc.a = Mathf.Clamp01((t - 0.5f) / 0.6f); _caption.color = cc; }
                    if (isLast && t > dur - 2.4f) _titleCg.alpha = Mathf.Clamp01((t - (dur - 2.4f)) / 0.9f);
                    if (Tapped()) { cutShort = true; break; }
                    yield return null;
                }
                var tmp = cur; cur = nxt; nxt = tmp; var tm = curM; curM = nxtM; nxtM = tm;
            }

            _titleCg.alpha = 1f; _caption.text = ""; _tag.text = "";
            float h = 0f;
            float hold = _def.holdToSeconds > 0f && !cutShort ? Mathf.Max(2.2f, _def.holdToSeconds - FinalFade - (Time.unscaledTime - started)) : 2.2f;
            while (h < hold && !_skip) { h += Time.unscaledDeltaTime; if (Tapped()) break; yield return null; }
            float f = 0f, v0 = _music.volume;
            while (f < FinalFade)
            {
                f += Time.unscaledDeltaTime;
                var c = _fader.color; c.a = Mathf.Clamp01(f / FinalFade); _fader.color = c;
                _music.volume = Mathf.Lerp(v0, 0f, f / FinalFade);
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
