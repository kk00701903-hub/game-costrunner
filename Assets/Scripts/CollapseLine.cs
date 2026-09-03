using UnityEngine;

/// The ground being chewed away from behind. Numbers come from GameConfig.
public class CollapseLine : MonoBehaviour
{
    public static CollapseLine Instance { get; private set; }

    [SerializeField] private float emitterHeight = 3.5f;

    private GameConfig _cfg;
    private ParticleSystem _dust;
    private Transform _wall;
    private PlayerController _player;
    private float _gap;
    private bool _visible;
    private bool _subscribed;

    public float Gap => _gap;
    public float StartGap => _cfg != null ? _cfg.collapseBaseGap : 45f;
    public float Pressure => Mathf.Clamp01(1f - Mathf.InverseLerp(KillGap, StartGap, _gap));
    public float Warning => Mathf.Clamp01(1f - Mathf.InverseLerp(KillGap, WarnGap, _gap));

    private float PaceFactor => _cfg != null ? _cfg.collapseSpeedRatio : 0.96f;
    private float HitPenalty => _cfg != null ? Mathf.Abs(_cfg.collapseOnHit) : 12f;
    private float CounterReward => _cfg != null ? _cfg.collapseOnCounter : 6f;
    private float WarnGap => _cfg != null ? _cfg.collapseWarnGap : 15f;
    private float KillGap => _cfg != null ? _cfg.collapseKillGap : 0.5f;

    private void Awake()
    {
        Instance = this;
        _cfg = GameConfig.Active;
    }

    private void Start()
    {
        _gap = StartGap;
        _player = FindObjectOfType<PlayerController>();
        BuildDust();
        BuildWall();
        SetVisible(false);
        Subscribe();
    }

    private void Subscribe()
    {
        if (_subscribed || GameManager.Instance == null)
            return;

        GameManager.Instance.OnHit += HandleHit;
        _subscribed = true;
    }

    private void HandleHit()
    {
        _gap = Mathf.Max(0f, _gap - HitPenalty);
    }

    public void Push(float metres)
    {
        _gap = Mathf.Clamp(_gap + metres, 0f, StartGap);
    }

    public void Reward()
    {
        Push(CounterReward);
    }

    public void ResetGap()
    {
        _gap = StartGap;
    }

    private void Update()
    {
        Subscribe();

        GameManager gm = GameManager.Instance;
        if (gm == null || _player == null)
            return;

        if (!gm.IsPlaying || _player.IsDead)
        {
            SetVisible(false);
            return;
        }

        if (TutorialDirector.Instance != null && TutorialDirector.Instance.HoldCollapseLine)
        {
            _gap = Mathf.Max(_gap, StartGap);
            SetVisible(false);
            return;
        }

        float closing = _player.CurrentSpeed - PaceFactor * _player.RampSpeed;
        _gap = Mathf.Clamp(_gap + closing * Time.deltaTime, 0f, StartGap);

        if (!Follow())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        ApplyPressure();

        if (_gap <= KillGap)
            gm.GameOver(DeathCause.Collapsed);
    }

    private bool Follow()
    {
        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner == null)
            return false;

        float behind = _player.PathDistance - _gap;
        if (behind < 0f)
            return false;

        Vector3 point;
        float yaw;
        if (!spawner.TryGetPoint(behind, out point, out yaw))
            return false;

        transform.SetPositionAndRotation(point + Vector3.up * emitterHeight, Quaternion.Euler(0f, yaw, 0f));
        return true;
    }

    private void ApplyPressure()
    {
        float p = Pressure;

        if (_dust != null)
        {
            ParticleSystem.EmissionModule emission = _dust.emission;
            emission.rateOverTime = Mathf.Lerp(20f, 320f, p);

            ParticleSystem.MainModule main = _dust.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(14f, 30f, p),
                Mathf.Lerp(22f, 46f, p));
        }

        if (_wall != null)
        {
            Vector3 scale = _wall.localScale;
            scale.y = Mathf.Lerp(6f, 14f, p);
            _wall.localScale = scale;
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.SetCollapsePressure(p);

        if (UIManager.Instance != null)
            UIManager.Instance.SetCollapseWarning(Warning);
    }

    private void SetVisible(bool on)
    {
        if (_visible == on || _dust == null)
            return;

        _visible = on;
        if (on)
            _dust.Play();
        else
            _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void BuildDust()
    {
        GameObject go = new GameObject("ConcreteDust");
        go.transform.SetParent(transform, false);
        _dust = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = _dust.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 900;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.68f, 0.67f, 0.65f, 0.34f),
            new Color(0.44f, 0.44f, 0.45f, 0.18f));
        main.gravityModifier = -0.02f;

        ParticleSystem.ShapeModule shape = _dust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 9f, 2f);

        ParticleSystem.EmissionModule emission = _dust.emission;
        emission.enabled = true;
        emission.rateOverTime = 20f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = BuildDustMaterial();
        renderer.sortingFudge = 40f;
    }

    private void BuildWall()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "DustWall";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 4f, -1.2f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(28f, 8f, 1f);

        Renderer renderer = go.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                    Shader.Find("Standard"));
        Color c = new Color(0.52f, 0.50f, 0.48f, 0.42f);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color"))
            mat.color = c;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _wall = go.transform;
    }

    private static Material BuildDustMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit") ??
                        Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply") ??
                        Shader.Find("Sprites/Default");

        var material = new Material(shader);
        Texture2D tex = Resources.Load<Texture2D>("FX/FX_Mote");
        if (tex != null)
            material.mainTexture = tex;

        return material;
    }

    private void OnDestroy()
    {
        if (_subscribed && GameManager.Instance != null)
            GameManager.Instance.OnHit -= HandleHit;

        if (Instance == this)
            Instance = null;
    }
}
