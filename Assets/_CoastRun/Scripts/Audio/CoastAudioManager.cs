using UnityEngine;

namespace CoastRun
{
    public enum CoastSfx
    {
        Coin,
        NearMiss,
        SoftHit,
        Land,
        Jump
    }

    /// Procedural ambient + skate SFX (no external clips required).
    /// ★ Never stops ambient / BGM loops — one-shot SFX only on a separate source.
    public class CoastAudioManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SeasonWeatherDirector weather;

        private AudioSource _ambient;
        private AudioSource _wheel;
        private AudioSource _wind;
        private AudioSource _sfx;
        private AudioClip _clipCoin;
        private AudioClip _clipNearMiss;
        private AudioClip _clipSoftHit;
        private AudioClip _clipLand;
        private AudioClip _clipJump;
        private bool _bedMuted;
        private bool _stemFrozen;
        private BedStemSnapshot _savedStem;
        private AudioSource _runBgm; // chapter stem a (the bed) when a real track exists

        // Stems b/c/d ride on top of `a` in sample-sync; their target volumes follow the
        // stage inside the chapter (see SetChapterStage). CH5 runs the other way round.
        private readonly AudioSource[] _stems = new AudioSource[4];
        private readonly float[] _stemTarget = new float[4];
        private int _bgmChapter = -1;
        private const float StemVolume = 0.9f;   // music leads; the procedural bed ducks under it

        private struct BedStemSnapshot
        {
            public bool valid;
            public bool ambientPlaying;
            public bool windPlaying;
            public bool wheelPlaying;
            public bool runBgmPlaying;
            public float ambientVol;
            public float windVol;
            public float wheelVol;
            public float runBgmVol;
            public float ambientTime;
            public float windTime;
            public float wheelTime;
            public float runBgmTime;
            public float ambientPitch;
            public float windPitch;
            public float wheelPitch;
            public float runBgmPitch;
        }

        public void Bind(PlayerController p, SeasonWeatherDirector w)
        {
            player = p;
            weather = w;
            EnsureSources();
        }

        /// Mute run bed (ambient/wind/wheels) during cutscenes.
        /// Never invent fill during intentional silence (e.g. CH4_Close 0:50–0:58).
        public void SetBedMuted(bool muted)
        {
            _bedMuted = muted;
            EnsureSources();
            if (_stemFrozen)
                return;
            ApplyMuteFlags(muted);
        }

        /// Memory popup: stop run bed over 0.5s, remember stem state for exact restore.
        public void BeginMemoryBed(float fadeOutSeconds = 0.5f)
        {
            EnsureSources();
            if (_stemFrozen)
                return;

            _savedStem = CaptureStem();
            _stemFrozen = true;
            StartCoroutine(FadeBedToSilent(Mathf.Max(0.05f, fadeOutSeconds)));
        }

        /// Restore run bed to the exact stem volumes / playhead captured at BeginMemoryBed.
        public void EndMemoryBed(float fadeInSeconds = 0.5f)
        {
            if (!_stemFrozen)
                return;
            StartCoroutine(RestoreStemRoutine(Mathf.Max(0.05f, fadeInSeconds)));
        }

        private BedStemSnapshot CaptureStem()
        {
            return new BedStemSnapshot
            {
                valid = true,
                ambientPlaying = _ambient != null && _ambient.isPlaying,
                windPlaying = _wind != null && _wind.isPlaying,
                wheelPlaying = _wheel != null && _wheel.isPlaying,
                runBgmPlaying = _runBgm != null && _runBgm.isPlaying,
                ambientVol = _ambient != null ? _ambient.volume : 0f,
                windVol = _wind != null ? _wind.volume : 0f,
                wheelVol = _wheel != null ? _wheel.volume : 0f,
                runBgmVol = _runBgm != null ? _runBgm.volume : 0f,
                ambientTime = _ambient != null ? _ambient.time : 0f,
                windTime = _wind != null ? _wind.time : 0f,
                wheelTime = _wheel != null ? _wheel.time : 0f,
                runBgmTime = _runBgm != null ? _runBgm.time : 0f,
                ambientPitch = _ambient != null ? _ambient.pitch : 1f,
                windPitch = _wind != null ? _wind.pitch : 1f,
                wheelPitch = _wheel != null ? _wheel.pitch : 1f,
                runBgmPitch = _runBgm != null ? _runBgm.pitch : 1f
            };
        }

        private System.Collections.IEnumerator FadeBedToSilent(float duration)
        {
            float a0 = _ambient != null ? _ambient.volume : 0f;
            float w0 = _wind != null ? _wind.volume : 0f;
            float wh0 = _wheel != null ? _wheel.volume : 0f;
            float r0 = _runBgm != null ? _runBgm.volume : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                if (_ambient != null) _ambient.volume = Mathf.Lerp(a0, 0f, u);
                if (_wind != null) _wind.volume = Mathf.Lerp(w0, 0f, u);
                if (_wheel != null) _wheel.volume = Mathf.Lerp(wh0, 0f, u);
                if (_runBgm != null) _runBgm.volume = Mathf.Lerp(r0, 0f, u);
                for (int i = 1; i < _stems.Length; i++)
                    if (_stems[i] != null) _stems[i].volume = Mathf.Lerp(_stemTarget[i], 0f, u);
                yield return null;
            }

            if (_ambient != null) { _ambient.volume = 0f; _ambient.Pause(); }
            if (_wind != null) { _wind.volume = 0f; _wind.Pause(); }
            if (_wheel != null) { _wheel.volume = 0f; _wheel.Pause(); }
            if (_runBgm != null) { _runBgm.volume = 0f; _runBgm.Pause(); }
            for (int i = 1; i < _stems.Length; i++)
                if (_stems[i] != null) { _stems[i].volume = 0f; _stems[i].Pause(); }
        }

        private System.Collections.IEnumerator RestoreStemRoutine(float duration)
        {
            var snap = _savedStem;
            // Resume paused sources at saved playheads before fading volumes back.
            ResumeSource(_ambient, snap.ambientPlaying, snap.ambientTime, snap.ambientPitch);
            ResumeSource(_wind, snap.windPlaying, snap.windTime, snap.windPitch);
            ResumeSource(_wheel, snap.wheelPlaying, snap.wheelTime, snap.wheelPitch);
            ResumeSource(_runBgm, snap.runBgmPlaying, snap.runBgmTime, snap.runBgmPitch);
            // Extra stems re-lock to the bed's playhead; TickStems fades them back in.
            for (int i = 1; i < _stems.Length; i++)
            {
                var s = _stems[i];
                if (s == null || s.clip == null || !snap.runBgmPlaying)
                    continue;
                s.UnPause();
                if (!s.isPlaying) s.Play();
                if (_runBgm != null && _runBgm.clip != null)
                    s.timeSamples = Mathf.Min(_runBgm.timeSamples, s.clip.samples - 1);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                if (_ambient != null) _ambient.volume = Mathf.Lerp(0f, snap.ambientVol, u);
                if (_wind != null) _wind.volume = Mathf.Lerp(0f, snap.windVol, u);
                if (_wheel != null) _wheel.volume = Mathf.Lerp(0f, snap.wheelVol, u);
                if (_runBgm != null) _runBgm.volume = Mathf.Lerp(0f, snap.runBgmVol, u);
                yield return null;
            }

            if (_ambient != null) _ambient.volume = snap.ambientVol;
            if (_wind != null) _wind.volume = snap.windVol;
            if (_wheel != null) _wheel.volume = snap.wheelVol;
            if (_runBgm != null) _runBgm.volume = snap.runBgmVol;

            _stemFrozen = false;
            _savedStem = default;
            ApplyMuteFlags(_bedMuted);
        }

        private static void ResumeSource(AudioSource src, bool wasPlaying, float time, float pitch)
        {
            if (src == null || !wasPlaying)
                return;
            src.pitch = pitch;
            if (!src.isPlaying)
                src.UnPause();
            if (!src.isPlaying)
                src.Play();
            if (src.clip != null)
                src.time = Mathf.Clamp(time, 0f, Mathf.Max(0.01f, src.clip.length - 0.05f));
        }

        private void ApplyMuteFlags(bool muted)
        {
            if (_ambient != null) _ambient.mute = muted;
            if (_wind != null) _wind.mute = muted;
            if (_wheel != null) _wheel.mute = muted;
            if (_runBgm != null) _runBgm.mute = muted;
            for (int i = 1; i < _stems.Length; i++)
                if (_stems[i] != null) _stems[i].mute = muted;
        }

        // ── Chapter stems ────────────────────────────────────────────────────

        /// Called on every stage start. Loads `BGM_CH{n}_a..d` from Resources/CoastRun/BGM
        /// when the chapter changes and sets which stems are audible for this stage:
        ///   CH1–4: stage 1 → a, stage 2 → a+b, stage 3+ → a+b+c   (build up)
        ///   CH5:   stage 1 → a+b+c, 2 → a+b, 3 → a, 4 → d only   (strip down, with the HUD)
        /// With no files present nothing plays and the procedural bed carries on.
        public void SetChapterStage(int chapter, int stageInChapter)
        {
            EnsureSources();
            chapter = Mathf.Clamp(chapter, 1, 5);
            stageInChapter = Mathf.Clamp(stageInChapter, 1, 4);

            if (chapter != _bgmChapter)
            {
                _bgmChapter = chapter;
                for (int i = 0; i < _stems.Length; i++)
                {
                    var clip = CoastBgmLibrary.Load(CoastBgmLibrary.ChapterStem(chapter, i));
                    // No split stems yet? Play the full mix as the bed so the chapter
                    // still has music while the stem pass is pending.
                    if (clip == null && i == 0)
                        clip = CoastBgmLibrary.Load($"BGM_CH{chapter}");
                    if (_stems[i] == null)
                    {
                        _stems[i] = CreateSource("Stem_" + (char)('a' + i), 0f, true);
                        _stems[i].mute = _bedMuted;
                    }
                    _stems[i].Stop();
                    _stems[i].clip = clip;
                    _stems[i].volume = 0f;
                    _stemTarget[i] = 0f;
                }
                _runBgm = _stems[0];

                // Start every stem on the same DSP tick so they stay phase-locked.
                double start = AudioSettings.dspTime + 0.1;
                for (int i = 0; i < _stems.Length; i++)
                    if (_stems[i].clip != null)
                        _stems[i].PlayScheduled(start);
            }

            bool reverse = chapter == 5;
            for (int i = 0; i < _stems.Length; i++)
            {
                bool on;
                if (!reverse)
                    on = i < Mathf.Min(3, stageInChapter);
                else
                    on = stageInChapter >= 4 ? i == 3 : i < 4 - stageInChapter;
                _stemTarget[i] = on ? StemVolume : 0f;
            }
        }

        private void TickStems(float dt)
        {
            if (_stemFrozen)
                return;
            for (int i = 0; i < _stems.Length; i++)
            {
                var s = _stems[i];
                if (s == null || s.clip == null)
                    continue;
                // 3 s fades — the 발주서 asks for stems to breathe in, never to pop.
                s.volume = Mathf.MoveTowards(s.volume, _stemTarget[i], dt / 3f * StemVolume);
            }
        }

        private void EnsureSources()
        {
            if (_ambient == null)
            {
                _ambient = CreateSource("Ambient", 0.35f, true);
                _ambient.clip = ProceduralAudio.CreateLoop(220f, 0.08f, 4f);
                _ambient.Play();
            }

            if (_wind == null)
            {
                _wind = CreateSource("Wind", 0.2f, true);
                _wind.clip = ProceduralAudio.CreateLoop(90f, 0.04f, 6f);
                _wind.Play();
            }

            if (_wheel == null)
            {
                _wheel = CreateSource("Wheels", 0f, false);
                _wheel.clip = ProceduralAudio.CreateOneShot(180f, 0.12f, 0.08f);
                _wheel.loop = true;
                _wheel.Play();
            }

            if (_sfx == null)
                _sfx = CreateSource("Sfx", 0.55f, false);

            if (_clipCoin == null)
                _clipCoin = ProceduralAudio.CreateBlip(880f, 0.06f);
            if (_clipNearMiss == null)
                _clipNearMiss = ProceduralAudio.CreateBlip(520f, 0.1f);
            if (_clipSoftHit == null)
                _clipSoftHit = ProceduralAudio.CreateBlip(140f, 0.14f);
            if (_clipLand == null)
                _clipLand = ProceduralAudio.CreateBlip(200f, 0.05f);
            if (_clipJump == null)
                _clipJump = ProceduralAudio.CreateBlip(360f, 0.05f);
        }

        private AudioSource CreateSource(string name, float vol, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.volume = vol;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            return src;
        }

        /// Play a short SFX. Never touches ambient / wheel / wind (BGM) sources.
        public void PlaySfx(CoastSfx kind)
        {
            EnsureSources();
            if (_sfx == null)
                return;

            AudioClip clip;
            float vol = 0.55f;
            float pitch = 1f;
            switch (kind)
            {
                case CoastSfx.Coin:
                    clip = _clipCoin;
                    vol = 0.45f;
                    pitch = Random.Range(0.95f, 1.1f);
                    break;
                case CoastSfx.NearMiss:
                    clip = _clipNearMiss;
                    vol = 0.5f;
                    pitch = 1.15f;
                    break;
                case CoastSfx.SoftHit:
                    clip = _clipSoftHit;
                    vol = 0.6f;
                    pitch = 0.75f;
                    break;
                case CoastSfx.Land:
                    clip = _clipLand;
                    vol = 0.3f;
                    pitch = 0.9f;
                    break;
                case CoastSfx.Jump:
                    clip = _clipJump;
                    vol = 0.28f;
                    pitch = 1.2f;
                    break;
                default:
                    return;
            }

            if (clip == null)
                return;

            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, vol);
        }

        private void Update()
        {
            if (player == null || _stemFrozen)
                return;

            TickStems(Time.deltaTime);

            // Keep loops alive — never Pause/Stop ambient here.
            float speed = player.NormalizedSpeed;
            // Real music present → the procedural ambient bed steps back.
            float bedScale = _runBgm != null && _runBgm.clip != null ? 0.15f : 1f;
            if (_wheel != null && !_bedMuted)
            {
                _wheel.volume = Mathf.Lerp(0.02f, 0.28f, speed) * (_runBgm != null && _runBgm.clip != null ? 0.5f : 1f);
                _wheel.pitch = Mathf.Lerp(0.85f, 1.35f, speed);
            }

            float rain = weather != null && weather.CurrentWeather == WeatherKind.Rain ? 0.25f : 0f;
            float snow = weather != null && weather.CurrentWeather == WeatherKind.Snow ? 0.15f : 0f;
            if (_wind != null && !_bedMuted)
                _wind.volume = (0.12f + speed * 0.15f) * bedScale + rain + snow;
            if (_ambient != null && !_bedMuted)
                _ambient.volume = (0.22f + speed * 0.08f) * bedScale;
        }
    }

    public static class ProceduralAudio
    {
        public static AudioClip CreateLoop(float baseFreq, float noise, float seconds)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];
            var rng = new System.Random(11);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.5f;
                wave += Mathf.Sin(2f * Mathf.PI * baseFreq * 1.5f * t) * 0.2f;
                wave += ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = wave * 0.35f;
            }

            var clip = AudioClip.Create("Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateOneShot(float baseFreq, float noise, float seconds)
        {
            return CreateLoop(baseFreq, noise, seconds);
        }

        public static AudioClip CreateBlip(float freq, float seconds)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (t / seconds);
                env *= env;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
                wave += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f * env;
                data[i] = wave * 0.5f;
            }

            var clip = AudioClip.Create("Blip", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
