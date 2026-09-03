using UnityEngine;

/// Distant city + sky fill for URP (built-in skybox is unreliable). Follows the
/// runner so the world always reads as a collapsing Korean city, not a white void.
public class RunBackdrop : MonoBehaviour
{
    public static RunBackdrop Instance { get; private set; }

    private Transform followTarget;
    private PlayerController _player;
    [SerializeField] private float skyDistance = 95f;
    [SerializeField] private float skylineDistance = 72f;
    [SerializeField] private int buildingCount = 14;

    private Transform _sky;
    private Transform _skyline;
    private Material _skyMat;
    private Material _buildingMat;
    private Material _windowMat;
    private readonly System.Collections.Generic.List<Transform> _buildings = new System.Collections.Generic.List<Transform>();

    public static RunBackdrop Ensure(Transform follow)
    {
        RunBackdrop existing = Instance;
        if (existing != null)
        {
            existing.followTarget = follow;
            existing._player = follow != null ? follow.GetComponent<PlayerController>() : null;
            return existing;
        }

        GameObject go = new GameObject("RunBackdrop");
        existing = go.AddComponent<RunBackdrop>();
        existing.followTarget = follow;
        existing._player = follow != null ? follow.GetComponent<PlayerController>() : null;
        return existing;
    }

    private void Start()
    {
        if (_player == null && followTarget != null)
            _player = followTarget.GetComponent<PlayerController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Build();
    }

    private void Build()
    {
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");

        _skyMat = new Material(unlit) { name = "RunSky" };
        _buildingMat = new Material(unlit) { name = "RunBuilding" };
        _windowMat = new Material(unlit) { name = "RunWindow" };

        SetColor(_skyMat, new Color(0.42f, 0.46f, 0.52f));
        SetColor(_buildingMat, new Color(0.22f, 0.21f, 0.24f));
        SetColor(_windowMat, new Color(1f, 0.82f, 0.48f));

        Texture2D concept = Resources.Load<Texture2D>("Concept/Concept_Opening");
        if (concept != null && _skyMat.HasProperty("_BaseMap"))
            _skyMat.SetTexture("_BaseMap", concept);

        _sky = MakeQuad("SkyDome", new Vector3(120f, 48f, 1f), _skyMat);
        _skyline = MakeQuad("Skyline", new Vector3(90f, 28f, 1f), _buildingMat);

        for (int i = 0; i < buildingCount; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = (i / 2) * 11f - 18f;
            float h = 6f + (i % 5) * 2.4f;
            float w = 3.5f + (i % 3) * 1.2f;
            Transform block = MakeBuilding(side, z, w, h);
            _buildings.Add(block);
        }
    }

    private Transform MakeQuad(string name, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    private Transform MakeBuilding(float side, float z, float width, float height)
    {
        GameObject root = new GameObject("Building");
        root.transform.SetParent(transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(body.GetComponent<Collider>());
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(width, height, width * 0.85f);
        body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = _buildingMat;

        if (Random.value > 0.35f)
        {
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(win.GetComponent<Collider>());
            win.name = "Sign";
            win.transform.SetParent(root.transform, false);
            win.transform.localScale = new Vector3(width * 0.7f, height * 0.12f, 0.15f);
            win.transform.localPosition = new Vector3(0f, height * 0.72f, width * 0.42f);
            win.GetComponent<Renderer>().sharedMaterial = _windowMat;
        }

        root.transform.localPosition = new Vector3(side * (16f + width), 0f, z);
        return root.transform;
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                followTarget = player.transform;
        }

        if (followTarget == null)
            return;

        Camera cam = Camera.main;
        Vector3 centre = followTarget.position;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 flatForward = forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;
        flatForward.Normalize();

        transform.position = new Vector3(centre.x, 0f, centre.z);

        float yaw = _player != null ? _player.Yaw : (cam != null ? cam.transform.eulerAngles.y : 0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (_sky != null && cam != null)
        {
            _sky.position = cam.transform.position + cam.transform.forward * skyDistance;
            _sky.rotation = cam.transform.rotation;
        }

        if (_skyline != null && cam != null)
        {
            _skyline.position = centre + flatForward * skylineDistance + Vector3.up * 8f;
            _skyline.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }

        ApplyZoneTint();
    }

    public void ApplyZoneTint(Color sky, Color fog, Color accent)
    {
        if (_skyMat != null)
            SetColor(_skyMat, Color.Lerp(sky, fog, 0.18f));
        if (_buildingMat != null)
            SetColor(_buildingMat, Color.Lerp(fog * 0.55f, accent * 0.35f, 0.25f));
        if (_windowMat != null)
            SetColor(_windowMat, Color.Lerp(accent, Color.white, 0.35f));
    }

    private void ApplyZoneTint()
    {
        if (ZoneDirector.Instance == null)
            return;

        Color sky = RenderSettings.ambientSkyColor;
        Color fog = RenderSettings.fogColor;
        Color accent = new Color(1f, 0.78f, 0.42f);
        ApplyZoneTint(sky, fog, accent);
    }

    private static void SetColor(Material mat, Color color)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.color = color;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
