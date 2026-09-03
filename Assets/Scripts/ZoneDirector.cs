using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZonePalette
{
    public Color skyTint = Color.white;
    public Color fogTint = Color.white;
    public Color sunTint = Color.white;
    public Color ambientTint = Color.white;
    public Color roadColor = new Color(0.55f, 0.55f, 0.58f);
    public Color obstacleColor = new Color(0.72f, 0.42f, 0.38f);
    public float fogDensityMultiplier = 1f;
    public float sunIntensityMultiplier = 1f;
}

/// Drives the look of the five zones off distance travelled rather than time,
/// so the city always falls apart in the same order however fast Doha rides.
/// The rule is that it stays pretty while it goes: nothing here gets uglier,
/// only emptier.
public class ZoneDirector : MonoBehaviour
{
    public static ZoneDirector Instance { get; private set; }

    [Header("Progression")]
    [Tooltip("Metres of blend before a zone boundary.")]
    [SerializeField] private float blendMetres = 220f;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 emitterOffset = new Vector3(0f, 6f, 8f);
    [SerializeField] private Vector3 emitterBox = new Vector3(14f, 10f, 22f);

    [Header("Palettes")]
    [SerializeField] private ZonePalette arcade = new ZonePalette();
    [SerializeField] private ZonePalette overpass = new ZonePalette();
    [SerializeField] private ZonePalette flooded = new ZonePalette();
    [SerializeField] private ZonePalette tower = new ZonePalette();
    [SerializeField] private ZonePalette depot = new ZonePalette();

    [Header("Particle textures (auto-loads Resources/FX if empty)")]
    [SerializeField] private Texture2D petalTexture;
    [SerializeField] private Texture2D moteTexture;
    [SerializeField] private Texture2D leafTexture;
    [SerializeField] private Texture2D snowTexture;

    private ParticleSystem[] _fx;
    private readonly List<Texture2D> _ownedFx = new List<Texture2D>();
    private Zone _zone = Zone.Arcade;
    private Zone _nextZone = Zone.Overpass;
    private float _blend;
    private bool _paused;
    private PlayerController _followPlayer;

    public Zone CurrentZone => _zone;
    public event Action<Zone> OnZoneChanged;

    /// Mechanic gates. Each zone teaches exactly one thing and the ones before
    /// it never see it, so an unlock has a place to be introduced.
    public bool GrindEnabled => Zones.Index(_zone) >= 2;
    public bool DarknessEnabled => Zones.Index(_zone) >= 3;
    public bool WallRunEnabled => Zones.Index(_zone) >= 4;
    public bool ReverseEnabled => Zones.Index(_zone) >= 5;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ApplyDefaultPalettes();
    }

    private void Start()
    {
        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                followTarget = player.transform;
        }

        _zone = Zone.Arcade;
        _nextZone = Next(_zone);
        _blend = 0f;

        BuildFxSystems();
        ApplyFxRates(true);
    }

    private void LateUpdate()
    {
        bool pausedWorld = GameManager.Instance != null && !GameManager.Instance.IsPlaying;
        if (pausedWorld)
        {
            if (!_paused)
            {
                SetFxPaused(true);
                _paused = true;
            }

            FollowTarget();
            return;
        }

        if (_paused)
        {
            SetFxPaused(false);
            _paused = false;
        }

        AdvanceZone();
        FollowTarget();
        ApplyFxRates(false);
    }

    public void FilterEnvironment(
        ref Color sky,
        ref Color fog,
        ref Color sun,
        ref Color ambient,
        ref float fogDensity,
        ref float sunIntensity)
    {
        ZonePalette from = Palette(_zone);
        ZonePalette to = Palette(_nextZone);
        float u = _blend;

        sky *= Color.Lerp(from.skyTint, to.skyTint, u);
        fog *= Color.Lerp(from.fogTint, to.fogTint, u);
        sun *= Color.Lerp(from.sunTint, to.sunTint, u);
        ambient *= Color.Lerp(from.ambientTint, to.ambientTint, u);
        fogDensity *= Mathf.Lerp(from.fogDensityMultiplier, to.fogDensityMultiplier, u);
        sunIntensity *= Mathf.Lerp(from.sunIntensityMultiplier, to.sunIntensityMultiplier, u);
    }

    public void TintSegment(TrackSegment segment)
    {
        if (segment == null)
            return;

        ZonePalette from = Palette(_zone);
        ZonePalette to = Palette(_nextZone);
        Color road = Color.Lerp(from.roadColor, to.roadColor, _blend);
        Color obstacle = Color.Lerp(from.obstacleColor, to.obstacleColor, _blend);
        segment.ApplyZoneColors(road, obstacle);
    }

    /// Zones pick from the tile catalogue by name. A missing match means the
    /// zone has no preference rather than no road.
    public bool AllowsTile(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return true;

        string[] tokens = TileTokens(_zone);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (prefabName.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static string[] TileTokens(Zone zone)
    {
        // Zone-named tiles come first so dropping Track_Overpass.glb into
        // Resources/Tracks starts appearing in zone 2 with no code change. The
        // generic names are the fallback for the tiles that exist today.
        switch (zone)
        {
            case Zone.Arcade:
                return new[] { "Arcade", "Straight", "Cracked" };
            case Zone.Overpass:
                return new[] { "Overpass", "CoastEdge", "Edge", "Straight" };
            case Zone.Flooded:
                return new[] { "Flooded", "Cracked", "CoastEdge" };
            case Zone.Tower:
                return new[] { "Tower", "Straight", "Arcade" };
            default:
                return new[] { "Depot", "Straight", "Cracked" };
        }
    }

    private void AdvanceZone()
    {
        GameManager gm = GameManager.Instance;
        float distance = gm != null ? gm.TraveledDistance : 0f;

        Zone here = Zones.At(distance);
        if (here != _zone)
        {
            _zone = here;
            _nextZone = Next(_zone);
            _blend = 0f;
            Announce();
            RetintActiveSegments();
            return;
        }

        _nextZone = Next(_zone);

        float boundary = Zones.StartDistance(_nextZone);
        float blend = Mathf.Max(20f, blendMetres);
        _blend = _nextZone == _zone ? 0f : Mathf.Clamp01(1f - (boundary - distance) / blend);
    }

    private void Announce()
    {
        OnZoneChanged?.Invoke(_zone);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner(Zones.Label(_zone));

        if (StoryEngine.Instance != null)
            StoryEngine.Instance.ReportZone(Zones.Index(_zone));

        // Entering a zone is what proves the one before it was survived.
        int previous = Zones.Index(_zone) - 1;
        if (previous >= 1)
            FlagStore.SetBool(FlagStore.ZoneCleared(previous), true);
    }

    private static Zone Next(Zone zone)
    {
        int index = Zones.Index(zone);
        return index >= Zones.Count ? zone : (Zone)(index + 1);
    }

    private void FollowTarget()
    {
        if (followTarget == null)
            return;

        if (_followPlayer == null)
            _followPlayer = followTarget.GetComponent<PlayerController>();

        // Turn with the run so the emitter box always sits ahead of the player,
        // not ahead of world +Z.
        Quaternion frame = Quaternion.Euler(0f, _followPlayer != null ? _followPlayer.Yaw : 0f, 0f);
        transform.SetPositionAndRotation(followTarget.position + frame * emitterOffset, frame);
    }

    private void ApplyFxRates(bool forceTint)
    {
        if (_fx == null)
            return;

        int current = Zones.Index(_zone) - 1;
        int next = Zones.Index(_nextZone) - 1;
        bool blending = _blend > 0.001f;

        for (int i = 0; i < _fx.Length; i++)
        {
            float rate = 0f;
            if (i == current)
                rate = BaseRate(_zone) * (blending ? 1f - _blend : 1f);
            else if (blending && i == next)
                rate = BaseRate(_nextZone) * _blend;

            ParticleSystem.EmissionModule emission = _fx[i].emission;
            emission.rateOverTime = rate;
        }

        if (forceTint || blending)
            RetintActiveSegments();
    }

    private void RetintActiveSegments()
    {
        if (RoadSpawner.Instance != null)
        {
            RoadSpawner.Instance.ForEachActiveSegment(TintSegment);
            return;
        }

        TrackSegment[] segments = FindObjectsOfType<TrackSegment>();
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].gameObject.activeInHierarchy)
                TintSegment(segments[i]);
        }
    }

    private static float BaseRate(Zone zone)
    {
        switch (zone)
        {
            case Zone.Arcade:
                return 45f;
            case Zone.Overpass:
                return 60f;
            case Zone.Flooded:
                return 80f;
            case Zone.Tower:
                return 38f;
            default:
                return 30f;
        }
    }

    private ZonePalette Palette(Zone zone)
    {
        switch (zone)
        {
            case Zone.Arcade:
                return arcade;
            case Zone.Overpass:
                return overpass;
            case Zone.Flooded:
                return flooded;
            case Zone.Tower:
                return tower;
            default:
                return depot;
        }
    }

    private void SetFxPaused(bool paused)
    {
        if (_fx == null)
            return;

        for (int i = 0; i < _fx.Length; i++)
        {
            if (paused)
                _fx[i].Pause(true);
            else
                _fx[i].Play(true);
        }
    }

    private void BuildFxSystems()
    {
        _fx = new ParticleSystem[Zones.Count];
        _fx[0] = CreateFx("Flyers", ResolveFx(leafTexture, "FX_Leaf", BuildLeafTexture), ConfigureFlyers);
        _fx[1] = CreateFx("ConcreteDust", ResolveFx(moteTexture, "FX_Mote", () => BuildSoftDisc(new Color(0.85f, 0.84f, 0.80f, 1f))), ConfigureDust);
        _fx[2] = CreateFx("WaterMist", ResolveFx(snowTexture, "FX_Snow", BuildSnowflakeTexture), ConfigureMist);
        _fx[3] = CreateFx("GlassShards", ResolveFx(petalTexture, "FX_Petal", BuildPetalTexture), ConfigureGlass);
        _fx[4] = CreateFx("SodiumHaze", ResolveFx(moteTexture, "FX_Mote", () => BuildSoftDisc(new Color(1f, 0.72f, 0.34f, 1f))), ConfigureSodium);
    }

    private ParticleSystem CreateFx(string name, Texture2D texture, Action<ParticleSystem> configure)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        configure(ps);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(texture);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = emitterBox;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ps.Play(true);
        return ps;
    }

    /// Zone 1: takeaway flyers off the shutters. Warm, cheap paper.
    private static void ConfigureFlyers(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 420;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        main.startSizeZ = 0.04f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.98f, 0.94f, 0.82f),
            new Color(0.95f, 0.72f, 0.42f));
        main.gravityModifier = 0.06f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.15f);

        EnableNoise(ps, 0.7f, 0.4f);
        EnableSpin(ps, 160f);
    }

    /// Zone 2: concrete going to powder somewhere behind and above.
    private static void ConfigureDust(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.30f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.78f, 0.76f, 0.65f),
            new Color(0.58f, 0.60f, 0.62f, 0.35f));
        main.gravityModifier = 0.02f;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.35f, 0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        EnableNoise(ps, 0.3f, 0.25f);
    }

    /// Zone 3: standing water breathing. Verdigris, and too much of it.
    private static void ConfigureMist(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 700;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.62f, 0.82f, 0.78f, 0.34f),
            new Color(0.42f, 0.62f, 0.60f, 0.18f));
        main.gravityModifier = -0.02f;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.1f);

        EnableNoise(ps, 0.25f, 0.18f);
    }

    /// Zone 4: window glass coming down from forty floors of empty flats.
    private static void ConfigureGlass(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 320;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.startSizeZ = 0.03f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.90f, 0.95f, 1f, 0.85f),
            new Color(0.72f, 0.82f, 0.90f, 0.55f));
        main.gravityModifier = 0.6f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        vel.y = new ParticleSystem.MinMaxCurve(-2.4f, -1.2f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.1f);

        EnableSpin(ps, 240f);
    }

    /// Zone 5: sodium lamps still on over the yard, and nothing moving under them.
    private static void ConfigureSodium(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 260;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.78f, 0.42f, 0.55f),
            new Color(0.95f, 0.58f, 0.28f, 0.30f));
        main.gravityModifier = -0.01f;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        EnableNoise(ps, 0.2f, 0.15f);
    }

    private static void EnableNoise(ParticleSystem ps, float strength, float frequency)
    {
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = 0.25f;
        noise.damping = true;
    }

    private static void EnableSpin(ParticleSystem ps, float degrees)
    {
        ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-degrees * Mathf.Deg2Rad, degrees * Mathf.Deg2Rad);
    }

    private static Material CreateParticleMaterial(Texture2D texture)
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
            "Sprites/Default",
            "Unlit/Transparent"
        };

        Shader shader = null;
        for (int i = 0; i < shaderNames.Length; i++)
        {
            shader = Shader.Find(shaderNames[i]);
            if (shader != null)
                break;
        }

        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0.1f);
        material.renderQueue = 3000;
        return material;
    }

    private Texture2D ResolveFx(Texture2D assigned, string resourceName, Func<Texture2D> fallback)
    {
        Texture2D src = assigned != null ? assigned : Resources.Load<Texture2D>("FX/" + resourceName);
        if (src == null)
            return fallback();

        Texture2D keyed = KeyBlackToAlpha(src);
        if (keyed != src)
            _ownedFx.Add(keyed);
        return keyed;
    }

    private static Texture2D KeyBlackToAlpha(Texture2D src)
    {
        Color32[] pixels;
        try
        {
            pixels = src.GetPixels32();
        }
        catch (UnityException)
        {
            return src;
        }

        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
        {
            name = src.name + "_FX",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int i = 0; i < pixels.Length; i++)
        {
            byte lum = pixels[i].r;
            if (pixels[i].g > lum)
                lum = pixels[i].g;
            if (pixels[i].b > lum)
                lum = pixels[i].b;
            if (pixels[i].a < lum)
                lum = pixels[i].a;
            pixels[i] = new Color32(255, 255, 255, lum);
        }

        copy.SetPixels32(pixels);
        copy.Apply(false, true);
        return copy;
    }

    private static Texture2D BuildSoftDisc(Color color)
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x - c) / c;
                float v = (y - c) / c;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                a *= a;
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a * color.a));
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BuildPetalTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x - c) / c;
                float v = (y - c) / c;
                float d = Mathf.Sqrt(u * u * 3.2f + v * v * 0.85f);
                float notch = Mathf.Abs(u) + (v + 0.35f) * 0.25f;
                float a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1.2f - notch);
                a = Mathf.Pow(a, 1.4f);
                tex.SetPixel(x, y, new Color(0.92f, 0.96f, 1f, a));
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BuildLeafTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x - c) / c;
                float v = (y - c) / c;
                float box = Mathf.Max(Mathf.Abs(u) * 1.15f, Mathf.Abs(v) * 1.6f);
                float a = Mathf.Clamp01(1.05f - box);
                tex.SetPixel(x, y, new Color(0.98f, 0.94f, 0.84f, a));
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D BuildSnowflakeTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x - c) / c;
                float v = (y - c) / c;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                a = Mathf.Pow(a, 1.8f);
                tex.SetPixel(x, y, new Color(0.85f, 0.95f, 0.92f, a));
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    [ContextMenu("Reset Zone Palettes")]
    private void ApplyDefaultPalettes()
    {
        // Zone 1: shutters half down, fluorescent tubes still warm.
        arcade = new ZonePalette
        {
            skyTint = new Color(1f, 0.90f, 0.82f),
            fogTint = new Color(1f, 0.88f, 0.78f),
            sunTint = new Color(1f, 0.92f, 0.80f),
            ambientTint = new Color(1f, 0.90f, 0.80f),
            roadColor = new Color(0.42f, 0.38f, 0.36f),
            obstacleColor = new Color(0.82f, 0.48f, 0.30f),
            fogDensityMultiplier = 0.9f,
            sunIntensityMultiplier = 1.0f
        };

        // Zone 2: raw concrete and open sky, nothing underneath.
        overpass = new ZonePalette
        {
            skyTint = new Color(0.82f, 0.88f, 0.95f),
            fogTint = new Color(0.80f, 0.86f, 0.92f),
            sunTint = new Color(0.88f, 0.92f, 0.98f),
            ambientTint = new Color(0.78f, 0.84f, 0.92f),
            roadColor = new Color(0.52f, 0.55f, 0.58f),
            obstacleColor = new Color(0.62f, 0.64f, 0.66f),
            fogDensityMultiplier = 0.8f,
            sunIntensityMultiplier = 1.1f
        };

        // Zone 3: verdigris under water, and almost no light reaching it.
        flooded = new ZonePalette
        {
            skyTint = new Color(0.42f, 0.58f, 0.56f),
            fogTint = new Color(0.36f, 0.55f, 0.52f),
            sunTint = new Color(0.50f, 0.66f, 0.62f),
            ambientTint = new Color(0.34f, 0.48f, 0.48f),
            roadColor = new Color(0.30f, 0.40f, 0.38f),
            obstacleColor = new Color(0.38f, 0.52f, 0.46f),
            fogDensityMultiplier = 2.2f,
            sunIntensityMultiplier = 0.42f
        };

        // Zone 4: dust in the air turning daylight flat and white.
        tower = new ZonePalette
        {
            skyTint = new Color(0.92f, 0.92f, 0.90f),
            fogTint = new Color(0.90f, 0.90f, 0.88f),
            sunTint = new Color(0.98f, 0.97f, 0.94f),
            ambientTint = new Color(0.88f, 0.88f, 0.86f),
            roadColor = new Color(0.66f, 0.65f, 0.63f),
            obstacleColor = new Color(0.72f, 0.70f, 0.66f),
            fogDensityMultiplier = 1.3f,
            sunIntensityMultiplier = 0.9f
        };

        // Zone 5: sodium orange with the scanner's red cutting across it.
        depot = new ZonePalette
        {
            skyTint = new Color(0.55f, 0.45f, 0.42f),
            fogTint = new Color(0.72f, 0.48f, 0.32f),
            sunTint = new Color(1f, 0.68f, 0.36f),
            ambientTint = new Color(0.62f, 0.44f, 0.34f),
            roadColor = new Color(0.46f, 0.36f, 0.30f),
            obstacleColor = new Color(0.80f, 0.34f, 0.24f),
            fogDensityMultiplier = 1.6f,
            sunIntensityMultiplier = 0.65f
        };
    }

    private void Reset()
    {
        ApplyDefaultPalettes();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _ownedFx.Count; i++)
        {
            if (_ownedFx[i] != null)
                Destroy(_ownedFx[i]);
        }

        _ownedFx.Clear();

        if (Instance == this)
            Instance = null;
    }
}
