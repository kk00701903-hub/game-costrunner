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
        private AudioSource _runBgm; // optional chapter stem if present

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
                yield return null;
            }

            if (_ambient != null) { _ambient.volume = 0f; _ambient.Pause(); }
            if (_wind != null) { _wind.volume = 0f; _wind.Pause(); }
            if (_wheel != null) { _wheel.volume = 0f; _wheel.Pause(); }
            if (_runBgm != null) { _runBgm.volume = 0f; _runBgm.Pause(); }
        }

        private System.Collections.IEnumerator RestoreStemRoutine(float duration)
        {
            var snap = _savedStem;
            // Resume paused sources at saved playheads before fading volumes back.
            ResumeSource(_ambient, snap.ambientPlaying, snap.ambientTime, snap.ambientPitch);
            ResumeSource(_wind, snap.windPlaying, snap.windTime, snap.windPitch);
            ResumeSource(_wheel, snap.wheelPlaying, snap.wheelTime, snap.wheelPitch);
            ResumeSource(_runBgm, snap.runBgmPlaying, snap.runBgmTime, snap.runBgmPitch);

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

            // Keep loops alive — never Pause/Stop ambient here.
            float speed = player.NormalizedSpeed;
            if (_wheel != null && !_bedMuted)
            {
                _wheel.volume = Mathf.Lerp(0.02f, 0.28f, speed);
                _wheel.pitch = Mathf.Lerp(0.85f, 1.35f, speed);
            }

            float rain = weather != null && weather.CurrentWeather == WeatherKind.Rain ? 0.25f : 0f;
            float snow = weather != null && weather.CurrentWeather == WeatherKind.Snow ? 0.15f : 0f;
            if (_wind != null && !_bedMuted)
                _wind.volume = 0.12f + speed * 0.15f + rain + snow;
            if (_ambient != null && !_bedMuted)
                _ambient.volume = 0.22f + speed * 0.08f;
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
