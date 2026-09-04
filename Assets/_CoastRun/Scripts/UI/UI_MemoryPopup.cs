using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoastRun
{
    /// Overlay memory fragment — Ken Burns stills + captions. Never loads a scene.
    public class UI_MemoryPopup : MonoBehaviour
    {
        public static UI_MemoryPopup Instance { get; private set; }

        private Canvas _canvas;
        private CanvasGroup _rootCg;
        private RectTransform _slide;
        private Image _edgeGlow;
        private Image _stillA;
        private Image _stillB;
        private Text _title;
        private Text _body;
        private GameObject _callRoot;
        private Image _phoneScreen;
        private Text _callTimer;
        private Text _callSubtitle;
        private Image _handSilhouette;
        private Volume _memVolume;
        private ColorAdjustments _memColor;
        private AudioSource _memBgm;
        private AudioSource _callNoise;
        private Coroutine _playRoutine;
        private Action _onClosed;
        private bool _skipRequested;
        private bool _playing;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public static UI_MemoryPopup Ensure()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("UI_MemoryPopup");
            DontDestroyOnLoad(go);
            return go.AddComponent<UI_MemoryPopup>();
        }

        public void Play(MemoryFragmentDef def, Action onClosed, bool fromGallery = false)
        {
            if (def == null)
            {
                onClosed?.Invoke();
                return;
            }

            EnsureUi();
            _onClosed = onClosed;
            _skipRequested = false;
            if (_playRoutine != null)
                StopCoroutine(_playRoutine);
            _playRoutine = StartCoroutine(PlayRoutine(def, fromGallery));
        }

        public void RequestSkip() => _skipRequested = true;

        private IEnumerator PlayRoutine(MemoryFragmentDef def, bool fromGallery)
        {
            _playing = true;
            _rootCg.gameObject.SetActive(true);
            _rootCg.alpha = 0f;
            _slide.anchoredPosition = new Vector2(0f, -80f);

            ApplySaturationVolume(def);
            var audio = UnityEngine.Object.FindAnyObjectByType<CoastAudioManager>();
            if (!fromGallery)
                audio?.BeginMemoryBed(0.5f);

            bool isR15 = def.isCallOnly || def.id == "R15";
            SetupVisualMode(isR15);
            if (isR15)
            {
                // ★ No Memory music — fade any bed and use call noise + dialogue only.
                yield return FadeOutMemoryBgm(0.4f);
                PlayCallNoise(true);
                yield return PlayCallSequence(def);
            }
            else
            {
                StartMemoryBgm(def);
                BindStills(def);
                _title.text = def.title;
                _body.text = def.body;
                yield return EdgeGlowThenSlideIn();
                yield return KenBurnsHold(Mathf.Clamp(def.duration, 20f, 30f));
            }

            yield return FadeOutPopup(0.35f);
            PlayCallNoise(false);
            StopMemoryBgm();
            ClearSaturationVolume();

            if (!fromGallery)
                audio?.EndMemoryBed(0.5f);

            _rootCg.gameObject.SetActive(false);
            _playing = false;
            _playRoutine = null;
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();
        }

        private IEnumerator EdgeGlowThenSlideIn()
        {
            // Edge glow pulse
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / 0.45f);
                if (_edgeGlow != null)
                {
                    var c = _edgeGlow.color;
                    c.a = Mathf.Sin(u * Mathf.PI) * 0.55f;
                    _edgeGlow.color = c;
                }

                _rootCg.alpha = Mathf.Lerp(0f, 1f, u);
                yield return null;
            }

            // Slide in
            t = 0f;
            Vector2 from = new Vector2(0f, -80f);
            Vector2 to = Vector2.zero;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                _slide.anchoredPosition = Vector2.Lerp(from, to, u);
                if (_edgeGlow != null)
                {
                    var c = _edgeGlow.color;
                    c.a = Mathf.Lerp(0.55f, 0.22f, u);
                    _edgeGlow.color = c;
                }

                yield return null;
            }

            _slide.anchoredPosition = to;
        }

        private IEnumerator KenBurnsHold(float seconds)
        {
            float t = 0f;
            var rtA = _stillA != null ? _stillA.rectTransform : null;
            var rtB = _stillB != null ? _stillB.rectTransform : null;
            while (t < seconds && !_skipRequested)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / seconds);
                // Ken Burns: slow zoom + pan
                float scale = Mathf.Lerp(1.05f, 1.18f, u);
                Vector2 pan = new Vector2(Mathf.Lerp(-12f, 18f, u), Mathf.Lerp(8f, -10f, u));
                if (rtA != null)
                {
                    rtA.localScale = Vector3.one * scale;
                    rtA.anchoredPosition = pan;
                }

                if (rtB != null && rtB.gameObject.activeSelf)
                {
                    float s2 = Mathf.Lerp(1.12f, 1.02f, u);
                    rtB.localScale = Vector3.one * s2;
                    rtB.anchoredPosition = -pan * 0.5f;
                    var c = _stillB.color;
                    c.a = u < 0.45f ? Mathf.Lerp(0f, 0.85f, u / 0.45f) :
                        u > 0.7f ? Mathf.Lerp(0.85f, 0.35f, (u - 0.7f) / 0.3f) : 0.85f;
                    _stillB.color = c;
                }

                PollSkip();
                yield return null;
            }
        }

        private IEnumerator PlayCallSequence(MemoryFragmentDef def)
        {
            _title.text = "";
            _body.text = "";
            if (_callTimer != null)
                _callTimer.text = "00:00";

            yield return EdgeGlowThenSlideIn();

            var lines = def.callLines;
            if (lines == null || lines.Length == 0)
            {
                // Fallback: body lines
                string[] parts = (def.body ?? "").Split('\n');
                float elapsed = 0f;
                for (int i = 0; i < parts.Length && !_skipRequested; i++)
                {
                    if (_callSubtitle != null)
                        _callSubtitle.text = parts[i].Trim();
                    float hold = 3.2f;
                    float h = 0f;
                    while (h < hold && !_skipRequested)
                    {
                        h += Time.unscaledDeltaTime;
                        elapsed += Time.unscaledDeltaTime;
                        if (_callTimer != null)
                            _callTimer.text = FormatCallTime(elapsed);
                        PollSkip();
                        yield return null;
                    }
                }
            }
            else
            {
                float elapsed = 0f;
                for (int i = 0; i < lines.Length && !_skipRequested; i++)
                {
                    var line = lines[i];
                    // Voice (subtitle) only — never draw Lua on screen.
                    if (_callSubtitle != null)
                        _callSubtitle.text = line.text;
                    float hold = string.IsNullOrEmpty(line.text) || line.text == "…" ? 2.2f : 3.8f;
                    float h = 0f;
                    while (h < hold && !_skipRequested)
                    {
                        h += Time.unscaledDeltaTime;
                        elapsed += Time.unscaledDeltaTime;
                        if (_callTimer != null)
                            _callTimer.text = FormatCallTime(elapsed);
                        PollSkip();
                        yield return null;
                    }
                }
            }

            // Brief hang on call UI — no place/time clues beyond call duration.
            float tail = 0f;
            while (tail < 1.2f && !_skipRequested)
            {
                tail += Time.unscaledDeltaTime;
                PollSkip();
                yield return null;
            }
        }

        private static string FormatCallTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        private void PollSkip()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Escape) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                _skipRequested = true;
        }

        private IEnumerator FadeOutPopup(float duration)
        {
            float start = _rootCg.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _rootCg.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
                yield return null;
            }

            _rootCg.alpha = 0f;
        }

        private void SetupVisualMode(bool callOnly)
        {
            if (_stillA != null)
                _stillA.gameObject.SetActive(!callOnly);
            if (_stillB != null)
                _stillB.gameObject.SetActive(!callOnly);
            if (_callRoot != null)
                _callRoot.SetActive(callOnly);

            // R15: out-of-focus darkness only — no interior/exterior props.
            var dim = _slide.GetComponent<Image>();
            if (dim != null)
                dim.color = callOnly
                    ? new Color(0.02f, 0.02f, 0.04f, 0.94f)
                    : new Color(0.04f, 0.05f, 0.08f, 0.88f);
        }

        private void BindStills(MemoryFragmentDef def)
        {
            if (_stillA == null)
                return;

            _stillA.rectTransform.localScale = Vector3.one * 1.05f;
            _stillA.rectTransform.anchoredPosition = Vector2.zero;
            _stillA.color = Color.white;
            _stillA.sprite = LoadStillSprite(def, 0);

            bool hasSecond = def.stillKeys != null && def.stillKeys.Length > 1;
            if (_stillB != null)
            {
                _stillB.gameObject.SetActive(hasSecond);
                if (hasSecond)
                {
                    _stillB.sprite = LoadStillSprite(def, 1);
                    _stillB.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        private static Sprite LoadStillSprite(MemoryFragmentDef def, int stillIndex)
        {
            string key = null;
            if (def.stillKeys != null && stillIndex < def.stillKeys.Length)
                key = def.stillKeys[stillIndex];

            Texture2D tex = null;
            if (!string.IsNullOrEmpty(key))
                tex = Resources.Load<Texture2D>("CoastRun/Memory/" + key);

            if (tex == null)
                tex = MakePlaceholderStill(def, stillIndex);

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D MakePlaceholderStill(MemoryFragmentDef def, int index)
        {
            int w = 256;
            int h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // Warm vs cold tint by chapter — no identifiable place.
            Color a = def.IsColdTone
                ? new Color(0.25f, 0.32f, 0.42f)
                : new Color(0.55f, 0.48f, 0.38f);
            Color b = def.IsColdTone
                ? new Color(0.18f, 0.22f, 0.34f)
                : new Color(0.72f, 0.62f, 0.48f);
            if (index > 0)
            {
                var tmp = a;
                a = b;
                b = tmp;
            }

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float v = y / (float)(h - 1);
                pixels[y * w + x] = Color.Lerp(a, b, (u + v) * 0.5f);
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            tex.name = "MemPlaceholder_" + def.id + "_" + index;
            return tex;
        }

        // ── Saturation volume (±15%) ───────────────────────────────────────

        private void ApplySaturationVolume(MemoryFragmentDef def)
        {
            EnsureMemVolume();
            // Memory sat is 15% higher than current scene — inverted in CH5.
            float delta = def.IsColdTone ? -15f : 15f;
            if (def.IsColdTone)
            {
                // Colder: slight blue filter + lower sat
                _memColor.colorFilter.Override(new Color(0.82f, 0.88f, 1f));
                _memColor.postExposure.Override(-0.08f);
            }
            else
            {
                _memColor.colorFilter.Override(new Color(1.02f, 1.0f, 0.96f));
                _memColor.postExposure.Override(0.05f);
            }

            _memColor.saturation.Override(delta);
            _memVolume.weight = 1f;
        }

        private void ClearSaturationVolume()
        {
            if (_memVolume != null)
                _memVolume.weight = 0f;
        }

        private void EnsureMemVolume()
        {
            if (_memVolume != null)
                return;

            var go = new GameObject("CoastVolume_Memory");
            DontDestroyOnLoad(go);
            _memVolume = go.AddComponent<Volume>();
            _memVolume.isGlobal = true;
            _memVolume.priority = 40f;
            _memVolume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_Memory";
            _memColor = profile.Add<ColorAdjustments>(true);
            _memColor.saturation.Override(0f);
            _memVolume.profile = profile;
        }

        // ── Memory BGM / call noise ───────────────────────────────────────

        private void StartMemoryBgm(MemoryFragmentDef def)
        {
            EnsureAudio();
            if (string.IsNullOrEmpty(def.bgmKey))
                return;

            var clip = CoastBgmLibrary.Load(def.bgmKey)
                       ?? Resources.Load<AudioClip>("CoastRun/Audio/" + def.bgmKey);
            if (clip == null)
            {
                float freq = def.IsColdTone ? 130f : def.chapter >= 3 ? 165f : 196f;
                clip = ProceduralAudio.CreateLoop(freq, def.IsColdTone ? 0.06f : 0.03f, 8f);
            }

            _memBgm.clip = clip;
            _memBgm.volume = 0f;
            _memBgm.loop = true;
            _memBgm.Play();
            StartCoroutine(FadeSource(_memBgm, 0f, 0.42f, 0.45f));
        }

        private IEnumerator FadeOutMemoryBgm(float duration)
        {
            if (_memBgm == null || !_memBgm.isPlaying)
                yield break;
            float start = _memBgm.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _memBgm.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
                yield return null;
            }

            _memBgm.Stop();
        }

        private void StopMemoryBgm()
        {
            if (_memBgm != null && _memBgm.isPlaying)
                _memBgm.Stop();
        }

        private void PlayCallNoise(bool on)
        {
            EnsureAudio();
            if (_callNoise == null)
                return;
            if (on)
            {
                if (_callNoise.clip == null)
                    _callNoise.clip = ProceduralAudio.CreateLoop(70f, 0.22f, 4f);
                _callNoise.volume = 0.12f;
                _callNoise.loop = true;
                if (!_callNoise.isPlaying)
                    _callNoise.Play();
            }
            else if (_callNoise.isPlaying)
                _callNoise.Stop();
        }

        private static IEnumerator FadeSource(AudioSource src, float from, float to, float duration)
        {
            if (src == null)
                yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            src.volume = to;
        }

        private void EnsureAudio()
        {
            if (_memBgm == null)
            {
                var go = new GameObject("MemoryBgm");
                go.transform.SetParent(transform, false);
                _memBgm = go.AddComponent<AudioSource>();
                _memBgm.playOnAwake = false;
                _memBgm.spatialBlend = 0f;
            }

            if (_callNoise == null)
            {
                var go = new GameObject("CallNoise");
                go.transform.SetParent(transform, false);
                _callNoise = go.AddComponent<AudioSource>();
                _callNoise.playOnAwake = false;
                _callNoise.spatialBlend = 0f;
            }
        }

        // ── UI build ──────────────────────────────────────────────────────

        private void EnsureUi()
        {
            if (_canvas != null)
                return;

            _canvas = CoastUiCanvas.Create("MemoryPopupCanvas", 360);
            DontDestroyOnLoad(_canvas.gameObject);

            var root = new GameObject("Root", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;
            _rootCg = root.GetComponent<CanvasGroup>();
            _rootCg.alpha = 0f;

            // Edge glow
            var glow = new GameObject("EdgeGlow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(root.transform, false);
            var grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;
            _edgeGlow = glow.GetComponent<Image>();
            _edgeGlow.color = new Color(0.85f, 0.92f, 1f, 0f);
            _edgeGlow.raycastTarget = false;

            var slide = new GameObject("Slide", typeof(RectTransform), typeof(Image));
            slide.transform.SetParent(root.transform, false);
            _slide = slide.GetComponent<RectTransform>();
            _slide.anchorMin = new Vector2(0.06f, 0.12f);
            _slide.anchorMax = new Vector2(0.94f, 0.88f);
            _slide.offsetMin = Vector2.zero;
            _slide.offsetMax = Vector2.zero;
            slide.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            // Stills
            _stillA = MakeStill(slide.transform, "StillA", 1f);
            _stillB = MakeStill(slide.transform, "StillB", 0f);
            _stillB.gameObject.SetActive(false);

            _title = CoastHudLayout.MakeText(slide.transform, "Title", "", 26,
                TextAnchor.LowerCenter,
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            _title.color = new Color(0.95f, 0.93f, 0.88f);

            _body = CoastHudLayout.MakeText(slide.transform, "Body", "", 18,
                TextAnchor.UpperCenter,
                new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);
            _body.color = new Color(0.9f, 0.9f, 0.88f, 0.95f);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;

            BuildCallUi(slide.transform);

            // Tap catcher
            var tap = new GameObject("Tap", typeof(RectTransform), typeof(Image), typeof(Button));
            tap.transform.SetParent(root.transform, false);
            var trt = tap.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var timg = tap.GetComponent<Image>();
            timg.color = new Color(0, 0, 0, 0.01f);
            tap.GetComponent<Button>().onClick.AddListener(RequestSkip);

            root.SetActive(false);
        }

        private void BuildCallUi(Transform parent)
        {
            _callRoot = new GameObject("CallOnly", typeof(RectTransform));
            _callRoot.transform.SetParent(parent, false);
            var crt = _callRoot.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            // Sky's hand silhouette — abstract, no environment.
            var hand = new GameObject("Hand", typeof(RectTransform), typeof(Image));
            hand.transform.SetParent(_callRoot.transform, false);
            var hrt = hand.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.15f, 0.08f);
            hrt.anchorMax = new Vector2(0.85f, 0.42f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            _handSilhouette = hand.GetComponent<Image>();
            _handSilhouette.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            _handSilhouette.raycastTarget = false;

            // Phone screen only — call duration, no place/time stamp.
            var phone = new GameObject("Phone", typeof(RectTransform), typeof(Image));
            phone.transform.SetParent(_callRoot.transform, false);
            var prt = phone.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.28f, 0.38f);
            prt.anchorMax = new Vector2(0.72f, 0.82f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            _phoneScreen = phone.GetComponent<Image>();
            _phoneScreen.color = new Color(0.12f, 0.14f, 0.16f, 0.98f);

            _callTimer = CoastHudLayout.MakeText(phone.transform, "CallTime", "00:00", 14,
                TextAnchor.UpperCenter,
                new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.92f), Vector2.zero, Vector2.zero);
            _callTimer.color = new Color(0.7f, 0.75f, 0.8f, 0.7f);

            // Avatar area blank — never draw Lua.
            var avatar = new GameObject("AvatarBlank", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(phone.transform, false);
            var art = avatar.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.3f, 0.42f);
            art.anchorMax = new Vector2(0.7f, 0.72f);
            art.offsetMin = Vector2.zero;
            art.offsetMax = Vector2.zero;
            avatar.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.22f, 1f);

            _callSubtitle = CoastHudLayout.MakeText(phone.transform, "Sub", "", 16,
                TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
            _callSubtitle.color = new Color(0.92f, 0.93f, 0.95f);
            _callSubtitle.horizontalOverflow = HorizontalWrapMode.Wrap;

            _callRoot.SetActive(false);
        }

        private static Image MakeStill(Transform parent, string name, float alpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.34f);
            rt.anchorMax = new Vector2(0.95f, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, alpha);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }
    }
}
