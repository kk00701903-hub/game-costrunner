using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum DayPeriod
{
    Night,
    Dawn,
    Day,
    Dusk
}

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Clock")]
    [Tooltip("Real seconds for a full 24 in-game hours.")]
    [SerializeField] private float dayLengthSeconds = 180f;
    [SerializeField] [Range(0f, 24f)] private float startHour = 11f;
    [SerializeField] private Text clockText;
    [SerializeField] private Text periodText;

    [Header("Scene")]
    [SerializeField] private Light sun;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool driveCameraBackground = true;
    [SerializeField] private float sunAzimuth = -30f;

    [Header("Palette (normalized 0 = 00:00, 1 = 24:00)")]
    [SerializeField] private Gradient skyColor;
    [SerializeField] private Gradient fogColor;
    [SerializeField] private Gradient sunColor;
    [SerializeField] private Gradient ambientColor;
    [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve fogDensity = AnimationCurve.Linear(0f, 0.015f, 1f, 0.015f);

    private float _hour;
    private DayPeriod _period;
    private Material _skybox;

    public float Hour => _hour;
    public float NormalizedTime => _hour / 24f;
    public DayPeriod Period => _period;
    public event Action<DayPeriod> OnPeriodChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (skyColor == null || skyColor.colorKeys.Length < 3)
            ApplyDefaultPalette();
    }

    private void Start()
    {
        if (sun == null)
            sun = RenderSettings.sun;

        if (sun == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        LoadCoastSkybox();
        _hour = Mathf.Repeat(startHour, 24f);
        _period = EvaluatePeriod(_hour);
        ApplyVisuals();
        RefreshClockUi();
    }

    /// Called after bootstrap so sky/fog show during the prologue too.
    public void RefreshEnvironment()
    {
        if (_skybox == null)
            LoadCoastSkybox();
        ApplyVisuals();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            float hoursPerSecond = 24f / Mathf.Max(1f, dayLengthSeconds);
            _hour = Mathf.Repeat(_hour + hoursPerSecond * Time.deltaTime, 24f);

            DayPeriod next = EvaluatePeriod(_hour);
            if (next != _period)
            {
                _period = next;
                OnPeriodChanged?.Invoke(_period);
            }
        }

        ApplyVisuals();
        RefreshClockUi();
    }

    private void ApplyVisuals()
    {
        float t = NormalizedTime;

        Color sky = skyColor.Evaluate(t);
        Color fog = fogColor.Evaluate(t);
        Color sunCol = sunColor.Evaluate(t);
        Color ambient = ambientColor.Evaluate(t);
        float fogDens = fogDensity.Evaluate(t);
        float sunInt = sunIntensity.Evaluate(t);

        if (ZoneDirector.Instance != null)
        {
            ZoneDirector.Instance.FilterEnvironment(
                ref sky, ref fog, ref sunCol, ref ambient, ref fogDens, ref sunInt);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = Mathf.Min(fogDens, 0.0085f);

        // Flat/Tricolor — Skybox mode ignores ambientLight and stays too dark under URP.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.ambientLight = ambient;
        RenderSettings.ambientSkyColor = Color.Lerp(ambient, sky, 0.55f);
        RenderSettings.ambientEquatorColor = Color.Lerp(ambient, Color.Lerp(sky, fog, 0.5f), 0.65f);
        RenderSettings.ambientGroundColor = Color.Lerp(ambient * 0.85f, fog * 0.45f, 0.5f);

        if (driveCameraBackground && targetCamera != null)
        {
            // URP: built-in skybox shaders often render white — always drive camera bg.
            Color horizon = Color.Lerp(sky, fog, 0.42f);
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = horizon;

            if (_skybox != null)
            {
                if (_skybox.HasProperty("_Tint"))
                    _skybox.SetColor("_Tint", Color.Lerp(Color.white, sky, 0.25f));
                if (_skybox.HasProperty("_Exposure"))
                    _skybox.SetFloat("_Exposure", Mathf.Clamp(sunInt * 0.65f + 0.28f, 0.55f, 1.05f));
                if (_skybox.shader != null && _skybox.shader.name.IndexOf("Procedural", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _skybox.SetColor("_SkyTint", Color.Lerp(sky, fog, 0.12f));
                    _skybox.SetColor("_GroundColor", Color.Lerp(fog * 0.5f, ambient * 0.45f, 0.45f));
                    _skybox.SetFloat("_Exposure", Mathf.Clamp(sunInt * 0.55f + 0.35f, 0.6f, 0.95f));
                }

                RenderSettings.skybox = _skybox;
            }

            if (RunBackdrop.Instance != null)
                RunBackdrop.Instance.ApplyZoneTint(sky, fog, sunCol);
        }

        if (sun != null)
        {
            sun.color = sunCol;
            sun.intensity = sunInt;

            float elevation = (_hour / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(elevation, sunAzimuth, 0f);
        }
    }

    private void RefreshClockUi()
    {
        int h = Mathf.FloorToInt(_hour);
        int m = Mathf.FloorToInt((_hour - h) * 60f);

        if (clockText != null)
            clockText.text = string.Format("{0:00}:{1:00}", h, m);

        if (periodText != null)
            periodText.text = PeriodLabel(_period);
    }

    public static DayPeriod EvaluatePeriod(float hour)
    {
        hour = Mathf.Repeat(hour, 24f);
        if (hour >= 5f && hour < 7f)
            return DayPeriod.Dawn;
        if (hour >= 7f && hour < 17f)
            return DayPeriod.Day;
        if (hour >= 17f && hour < 20f)
            return DayPeriod.Dusk;
        return DayPeriod.Night;
    }

    public static string PeriodLabel(DayPeriod period)
    {
        switch (period)
        {
            case DayPeriod.Dawn:
                return "Dawn";
            case DayPeriod.Day:
                return "Day";
            case DayPeriod.Dusk:
                return "Dusk";
            default:
                return "Night";
        }
    }

    [ContextMenu("Reset Coastal Palette")]
    private void ApplyDefaultPalette()
    {
        skyColor = BuildGradient(
            new Color(0.08f, 0.09f, 0.12f),
            new Color(0.42f, 0.40f, 0.40f),
            new Color(0.52f, 0.55f, 0.58f),
            new Color(0.48f, 0.52f, 0.56f),
            new Color(0.38f, 0.36f, 0.36f),
            new Color(0.10f, 0.11f, 0.14f));

        fogColor = BuildGradient(
            new Color(0.10f, 0.11f, 0.14f),
            new Color(0.50f, 0.50f, 0.50f),
            new Color(0.58f, 0.60f, 0.62f),
            new Color(0.55f, 0.58f, 0.60f),
            new Color(0.42f, 0.40f, 0.40f),
            new Color(0.09f, 0.10f, 0.13f));

        sunColor = BuildGradient(
            new Color(0.35f, 0.45f, 0.75f),
            new Color(1f, 0.62f, 0.35f),
            new Color(1f, 0.96f, 0.88f),
            new Color(1f, 0.95f, 0.85f),
            new Color(1f, 0.48f, 0.22f),
            new Color(0.28f, 0.38f, 0.70f));

        ambientColor = BuildGradient(
            new Color(0.22f, 0.24f, 0.32f),
            new Color(0.62f, 0.42f, 0.36f),
            new Color(0.62f, 0.66f, 0.68f),
            new Color(0.60f, 0.64f, 0.66f),
            new Color(0.58f, 0.38f, 0.32f),
            new Color(0.20f, 0.22f, 0.30f));

        sunIntensity = new AnimationCurve(
            new Keyframe(0.00f, 0.42f),
            new Keyframe(0.22f, 0.55f),
            new Keyframe(0.35f, 0.88f),
            new Keyframe(0.50f, 1.05f),
            new Keyframe(0.72f, 0.78f),
            new Keyframe(0.80f, 0.58f),
            new Keyframe(1.00f, 0.42f));

        fogDensity = new AnimationCurve(
            new Keyframe(0.00f, 0.034f),
            new Keyframe(0.25f, 0.022f),
            new Keyframe(0.50f, 0.018f),
            new Keyframe(0.72f, 0.024f),
            new Keyframe(1.00f, 0.034f));
    }

    private static Gradient BuildGradient(
        Color midnight,
        Color dawn,
        Color morning,
        Color noon,
        Color dusk,
        Color evening)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(midnight, 0.00f),
                new GradientColorKey(dawn, 0.24f),
                new GradientColorKey(morning, 0.32f),
                new GradientColorKey(noon, 0.50f),
                new GradientColorKey(dusk, 0.76f),
                new GradientColorKey(evening, 0.88f),
                new GradientColorKey(midnight, 1.00f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }

    private void LoadCoastSkybox()
    {
        // HDR often imports as Cubemap; JPG fallback is equirectangular 2D.
        if (TryLoadSkyCubemap("Sky/Sky_MistyGrey"))
            return;
        if (TryLoadSkyPanorama("Sky/Sky_MistyGrey"))
            return;
        if (TryLoadSkyPanorama("Concept/Skybox_CoastApoc"))
            return;

        EnsureProceduralSkybox();
        Debug.LogWarning("DayNightCycle: sky HDR missing or unreadable — using procedural sky.");
    }

    private void EnsureProceduralSkybox()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
            return;

        if (_skybox == null || _skybox.shader != shader)
        {
            if (_skybox != null)
                Destroy(_skybox);

            _skybox = new Material(shader) { name = "347_ProceduralSky" };
        }

        float t = NormalizedTime;
        Color sky = skyColor.Evaluate(t);
        Color fog = fogColor.Evaluate(t);

        _skybox.SetColor("_SkyTint", Color.Lerp(sky, fog, 0.15f));
        _skybox.SetColor("_GroundColor", Color.Lerp(fog * 0.55f, ambientColor.Evaluate(t) * 0.45f, 0.5f));
        _skybox.SetFloat("_SunSize", 0.04f);
        _skybox.SetFloat("_SunSizeConvergence", 5f);
        _skybox.SetFloat("_AtmosphereThickness", 0.55f);
        _skybox.SetFloat("_Exposure", 0.82f);

        RenderSettings.skybox = _skybox;
        DynamicGI.UpdateEnvironment();
    }

    private bool TryLoadSkyCubemap(string resourcePath)
    {
        Cubemap cube = Resources.Load<Cubemap>(resourcePath);
        if (cube == null)
            return false;

        Shader shader = Shader.Find("Skybox/Cubemap") ?? Shader.Find("Skybox/6 Sided");
        if (shader == null)
            return false;

        _skybox = new Material(shader) { name = resourcePath + "_Cube" };
        _skybox.SetTexture("_Tex", cube);
        if (_skybox.HasProperty("_Exposure"))
            _skybox.SetFloat("_Exposure", 1.0f);
        if (_skybox.HasProperty("_Rotation"))
            _skybox.SetFloat("_Rotation", 0f);

        RenderSettings.skybox = _skybox;
        DynamicGI.UpdateEnvironment();
        return true;
    }

    private bool TryLoadSkyPanorama(string resourcePath)
    {
        Texture2D pano = Resources.Load<Texture2D>(resourcePath);
        if (pano == null)
        {
            Texture generic = Resources.Load<Texture>(resourcePath);
            pano = generic as Texture2D;
        }

        if (pano == null)
            return false;

        Shader shader = Shader.Find("Skybox/Panoramic") ?? Shader.Find("Skybox/Cubemap");
        if (shader == null)
            return false;

        _skybox = new Material(shader) { name = resourcePath + "_Pano" };
        _skybox.SetTexture("_MainTex", pano);
        _skybox.SetTexture("_Tex", pano);

        if (_skybox.HasProperty("_Mapping"))
            _skybox.SetFloat("_Mapping", 1f);
        if (_skybox.HasProperty("_ImageType"))
            _skybox.SetFloat("_ImageType", 0f);
        if (_skybox.HasProperty("_Layout"))
            _skybox.SetFloat("_Layout", 0f);
        if (_skybox.HasProperty("_Exposure"))
            _skybox.SetFloat("_Exposure", 1.0f);

        RenderSettings.skybox = _skybox;
        DynamicGI.UpdateEnvironment();
        return true;
    }

    private void Reset()
    {
        ApplyDefaultPalette();
    }

    private void OnDestroy()
    {
        if (_skybox != null)
            Destroy(_skybox);

        if (Instance == this)
            Instance = null;
    }
}
