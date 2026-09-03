using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// Day lighting driven by lightingT: 0 = 13:20 noon → 1 = 19:04 blue hour.
    /// t never decreases except via ResetLightingTo (stage retry → stage lightingTStart only).
    public class DynamicEnvironmentManager : MonoBehaviour
    {
        [Header("Time curves (t 0..1)")]
        [SerializeField] private Gradient sunColor;
        [SerializeField] private AnimationCurve sunElevation;   // 0..1 → degrees (90 zenith → -2)
        [SerializeField] private AnimationCurve sunIntensity;
        [SerializeField] private Gradient skyTint;
        [SerializeField] private Gradient fogColor;
        [SerializeField] private AnimationCurve fogDensity;
        [SerializeField] private AnimationCurve atmosphereThickness;

        [Header("Chapter post volumes (VP_CH1..CH5)")]
        [SerializeField] private Volume[] chapterVolumes = new Volume[5];

        [Header("Refs")]
        [SerializeField] private Light sun;
        [SerializeField] private Light towerBeacon;
        [SerializeField] private CoastSky coastSky;
        [SerializeField] private StoryConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private CoastFogSettings fog = new CoastFogSettings();
        [SerializeField] private float beaconBlinkHz = 1.15f;

        // Chapter blend keys — matches design §7-3 clock table.
        private static readonly float[] ChapterKeys = { 0f, 0.25f, 0.50f, 0.72f, 0.90f, 1f };

        private float _lightingT;
        private DayPhase _phase = DayPhase.BrightNoon;
        private bool _defaultsReady;
        private Renderer _skyDomeRenderer;

        public DayPhase CurrentPhase => _phase;
        public float NormalizedProgress => _lightingT;
        public float LightingT => _lightingT;
        public CoastFogSettings Fog => fog;

        public void Bind(StoryConfig storyConfig, PlayerController playerController, UpgradeManager upgradeManager)
        {
            config = storyConfig;
            player = playerController;
            upgrades = upgradeManager;
            EnsureDefaults();
            EnsureSun();
            EnsureChapterVolumes();
            EnsureCoastSky();
            EnsureTowerBeacon();
            ResetLightingTo(0f);
        }

        /// Monotonic — values below current t are ignored (prevents accidental rewind).
        public void SetTime(float t)
        {
            t = Mathf.Clamp01(t);
            if (t + 0.0001f < _lightingT)
                return;
            ApplyTime(t, false);
        }

        /// Stage load / retry only — may set t to this stage's lightingTStart (never earlier chapters).
        public void ResetLightingTo(float t)
        {
            ApplyTime(Mathf.Clamp01(t), true);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// Debug scrubber — stripped from release player builds.
        public void DebugForceTime(float t) => ApplyTime(Mathf.Clamp01(t), true);
#endif

        private void ApplyTime(float t, bool instant)
        {
            EnsureDefaults();
            _lightingT = t;

            if (t >= 0.88f)
                _phase = DayPhase.BlueHour;
            else if (t >= 0.55f)
                _phase = DayPhase.GoldenHour;
            else
                _phase = DayPhase.BrightNoon;

            // 1) Sun color
            Color sunCol = sunColor.Evaluate(t);
            // 2) Sun elevation (pitch)
            float elevation = sunElevation.Evaluate(t);
            float intensity = sunIntensity != null && sunIntensity.length > 0
                ? sunIntensity.Evaluate(t)
                : Mathf.Lerp(1.35f, 0.55f, t);

            EnsureSun();
            if (sun != null)
            {
                sun.color = sunCol;
                sun.intensity = intensity;
                sun.transform.rotation = Quaternion.Euler(elevation, -35f, 0f);
            }

            // 3 + 4) Sky + fog — MUST be identical (fogColor authoring is mirrored from skyTint).
            Color sky = skyTint.Evaluate(t);
            Color fogCol = sky;
            float dens = fogDensity != null && fogDensity.length > 0
                ? fogDensity.Evaluate(t)
                : Mathf.Lerp(0.0022f, 0.0065f, t);
            float atmo = atmosphereThickness != null && atmosphereThickness.length > 0
                ? atmosphereThickness.Evaluate(t)
                : Mathf.Lerp(0.85f, 1.35f, t);

            ApplySkyAndFog(sky, fogCol, dens, atmo, instant);

            // 6) Chapter volume crossfade (adjacent pair only).
            BlendChapterVolumes(t);

            // 7) Tower beacon for late day.
            UpdateBeacon(t, instant);
        }

        private void ApplySkyAndFog(Color sky, Color fogCol, float dens, float atmo, bool instant)
        {
            RenderSettings.fog = fog == null || fog.enabled;
            if (fog != null)
            {
                RenderSettings.fogMode = fog.mode;
                if (fog.mode == FogMode.Linear)
                {
                    // Density curve → pull fog end closer as evening thickens.
                    float end = Mathf.Lerp(fog.end, fog.start + 25f, Mathf.InverseLerp(0.002f, 0.008f, dens));
                    RenderSettings.fogStartDistance = fog.start;
                    RenderSettings.fogEndDistance = Mathf.Max(fog.start + 10f, end);
                }
                else
                {
                    RenderSettings.fogDensity = dens;
                }
            }
            else
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = dens;
            }

            RenderSettings.fogColor = fogCol;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(sky, fogCol, 0.35f) * Mathf.Lerp(0.9f, 0.55f, _lightingT);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = sky;
            }

            EnsureCoastSky();
            if (coastSky != null)
            {
                coastSky.SetSkyColor(sky, instant);
                coastSky.SetAtmosphere(sky, atmo);
            }

            TintSkyDome(sky, atmo);
        }

        private void BlendChapterVolumes(float t)
        {
            EnsureChapterVolumes();
            if (chapterVolumes == null || chapterVolumes.Length < 5)
                return;

            ResolveChapterBlend(t, out int a, out int b, out float wB);
            for (int i = 0; i < 5; i++)
            {
                if (chapterVolumes[i] == null)
                    continue;
                float w = 0f;
                if (i == a)
                    w = 1f - wB;
                if (i == b)
                    w = wB;
                chapterVolumes[i].weight = w;
            }
        }

        /// Maps t onto chapter keyframes; only two adjacent volumes are non-zero.
        public static void ResolveChapterBlend(float t, out int indexA, out int indexB, out float weightB)
        {
            t = Mathf.Clamp01(t);
            // Keys: 0, 0.25, 0.50, 0.72, 0.90, 1.0 → chapters 1..5 (indices 0..4)
            for (int i = 0; i < 4; i++)
            {
                float a = ChapterKeys[i];
                float b = ChapterKeys[i + 1];
                if (t < b - 0.0001f)
                {
                    indexA = i;
                    indexB = i + 1;
                    weightB = Mathf.InverseLerp(a, b, t);
                    return;
                }
            }

            // t in [0.90, 1.00] → full CH5
            indexA = 4;
            indexB = 4;
            weightB = 0f;
        }

        private void UpdateBeacon(float t, bool instant)
        {
            EnsureTowerBeacon();
            if (towerBeacon == null)
                return;

            bool on = t > 0.85f;
            towerBeacon.enabled = on;
            if (!on)
                return;

            // Red aviation obstruction light — blink.
            float blink = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * beaconBlinkHz) > 0f) ? 1f : 0.08f;
            towerBeacon.color = new Color(1f, 0.12f, 0.08f);
            towerBeacon.intensity = Mathf.Lerp(0f, 4.5f, blink);
            if (instant)
                towerBeacon.intensity = 4.5f;
        }

        private void LateUpdate()
        {
            // Keep fog locked to live sky every frame (camera clear / CoastSky may blend).
            Camera cam = Camera.main;
            if (cam != null && RenderSettings.fog)
                RenderSettings.fogColor = cam.backgroundColor;

            if (_lightingT > 0.85f)
                UpdateBeacon(_lightingT, false);
        }

        // ── Setup ──────────────────────────────────────────────────────────

        private void EnsureDefaults()
        {
            if (_defaultsReady && sunColor != null && sunColor.colorKeys != null && sunColor.colorKeys.Length > 0)
                return;

            sunColor = BuildSunGradient();
            skyTint = BuildSkyGradient();
            fogColor = BuildSkyGradient(); // identical keys — sky/fog stay matched
            sunElevation = new AnimationCurve(
                new Keyframe(0f, 52f),
                new Keyframe(0.25f, 42f),
                new Keyframe(0.50f, 28f),
                new Keyframe(0.72f, 14f),
                new Keyframe(0.90f, 2f),
                new Keyframe(1f, -2f));
            sunIntensity = new AnimationCurve(
                new Keyframe(0f, 1.35f),
                new Keyframe(0.5f, 1.15f),
                new Keyframe(0.72f, 1.05f),
                new Keyframe(0.9f, 0.7f),
                new Keyframe(1f, 0.5f));
            fogDensity = new AnimationCurve(
                new Keyframe(0f, 0.0022f),
                new Keyframe(0.5f, 0.003f),
                new Keyframe(0.72f, 0.0042f),
                new Keyframe(1f, 0.0065f));
            atmosphereThickness = new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(0.72f, 1.05f),
                new Keyframe(1f, 1.4f));
            _defaultsReady = true;
        }

        private static Gradient BuildSunGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.97f, 0.88f), 0f),       // noon
                    new GradientColorKey(new Color(1f, 0.92f, 0.75f), 0.25f),
                    new GradientColorKey(new Color(1f, 0.82f, 0.55f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.72f, 0.42f), 0.72f),     // golden
                    new GradientColorKey(new Color(0.65f, 0.7f, 0.95f), 0.9f),    // blue hour
                    new GradientColorKey(new Color(0.45f, 0.5f, 0.85f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private static Gradient BuildSkyGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(CoastPalette.SkyTop, 0f),                 // 13:20 summer noon
                    new GradientColorKey(new Color(0.35f, 0.62f, 0.88f), 0.25f),   // 14:30
                    new GradientColorKey(new Color(0.55f, 0.58f, 0.72f), 0.5f),    // 16:00
                    new GradientColorKey(new Color(0.95f, 0.55f, 0.28f), 0.72f),   // 17:20 golden
                    new GradientColorKey(new Color(0.18f, 0.2f, 0.48f), 0.9f),     // 18:40 blue/violet
                    new GradientColorKey(new Color(0.1f, 0.12f, 0.28f), 1f)        // 19:04 residual
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private void EnsureSun()
        {
            if (sun != null)
                return;
            var go = GameObject.Find("Directional Light");
            if (go != null)
                sun = go.GetComponent<Light>();
            if (sun == null)
            {
                go = new GameObject("Directional Light");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
        }

        private void EnsureCoastSky()
        {
            if (coastSky == null)
                coastSky = Object.FindAnyObjectByType<CoastSky>();
        }

        private void EnsureTowerBeacon()
        {
            if (towerBeacon != null)
                return;

            var tower = GameObject.Find("TransmissionTower");
            Transform anchor = tower != null ? tower.transform : transform;
            var existing = anchor.Find("TowerBeacon");
            if (existing != null)
            {
                towerBeacon = existing.GetComponent<Light>();
                if (towerBeacon != null)
                    return;
            }

            var go = new GameObject("TowerBeacon");
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = tower != null
                ? new Vector3(0f, 22f, 0f)
                : new Vector3(-6f, 22f, 0f);
            towerBeacon = go.AddComponent<Light>();
            towerBeacon.type = LightType.Point;
            towerBeacon.range = 28f;
            towerBeacon.color = new Color(1f, 0.12f, 0.08f);
            towerBeacon.intensity = 0f;
            towerBeacon.enabled = false;
            towerBeacon.shadows = LightShadows.None;
        }

        private void EnsureChapterVolumes()
        {
            if (chapterVolumes != null && chapterVolumes.Length == 5)
            {
                bool ok = true;
                for (int i = 0; i < 5; i++)
                {
                    if (chapterVolumes[i] == null)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                    return;
            }

            chapterVolumes = new Volume[5];
            var root = transform.Find("ChapterVolumes");
            if (root == null)
            {
                var go = new GameObject("ChapterVolumes");
                go.transform.SetParent(transform, false);
                root = go.transform;
            }

            // Keep VP_Base as low-priority foundation.
            CoastPostStack.EnsureGlobalVolume();

            for (int i = 0; i < 5; i++)
            {
                string name = "CoastVolume_VP_CH" + (i + 1);
                Transform child = root.Find(name);
                Volume vol;
                if (child != null)
                {
                    vol = child.GetComponent<Volume>();
                }
                else
                {
                    var go = new GameObject(name);
                    go.transform.SetParent(root, false);
                    vol = go.AddComponent<Volume>();
                }

                vol.isGlobal = true;
                vol.priority = 10 + i;
                vol.weight = 0f;
                vol.profile = CoastPostStack.LoadOrBuildChapterProfile(i + 1);
                chapterVolumes[i] = vol;
            }
        }

        private void TintSkyDome(Color sky, float atmo)
        {
            if (_skyDomeRenderer == null && coastSky != null)
            {
                var dome = coastSky.transform.Find("SkyGradient");
                if (dome != null)
                    _skyDomeRenderer = dome.GetComponent<Renderer>();
            }

            if (_skyDomeRenderer == null || _skyDomeRenderer.sharedMaterial == null)
                return;

            var mat = _skyDomeRenderer.material;
            Color tint = Color.Lerp(Color.white, sky, 0.55f);
            tint *= Mathf.Lerp(1.05f, 0.75f, Mathf.InverseLerp(0.8f, 1.4f, atmo));
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            if (mat.HasProperty("_SkyTint"))
                mat.SetColor("_SkyTint", sky);
            if (mat.HasProperty("_AtmosphereThickness"))
                mat.SetFloat("_AtmosphereThickness", atmo);
        }

        private void OnValidate()
        {
            if (sunColor == null || sunColor.colorKeys == null || sunColor.colorKeys.Length == 0)
                _defaultsReady = false;
        }
    }
}
