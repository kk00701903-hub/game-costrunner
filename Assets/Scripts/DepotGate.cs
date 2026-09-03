using UnityEngine;

/// The western collection depot. The red light on top is not a warning beacon,
/// it is a tag scanner, and it is already looking for Doha's number.
public class DepotGate : MonoBehaviour
{
    public static DepotGate Instance { get; private set; }

    [SerializeField] private Light scanner;
    [SerializeField] private float sweepSpeed = 1.6f;
    [SerializeField] private float minIntensity = 0.35f;
    [SerializeField] private float maxIntensity = 4.2f;
    [SerializeField] private Color scannerColor = new Color(0.92f, 0.16f, 0.10f);

    private Transform _beam;
    private bool _placed;

    private void Awake()
    {
        Instance = this;
        if (scanner == null)
            scanner = GetComponentInChildren<Light>();
    }

    private void Start()
    {
        LoadModelIfNeeded();
        EnsureScanner();
        BuildBeam();
        SetVisible(false);
    }

    /// The path only exists a little way ahead of the player, so the gate waits
    /// until the goal tile has actually been generated before it shows up.
    private void TryPlaceOnPath()
    {
        if (_placed || GameManager.Instance == null || RoadSpawner.Instance == null)
            return;

        float goal = GameManager.Instance.DepotDistance;
        if (!RoadSpawner.Instance.TryGetPoint(goal, out Vector3 point, out float yaw))
            return;

        transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
        _placed = true;
        SetVisible(true);

        AudioSource src = GetComponent<AudioSource>();
        if (src != null && src.clip != null && !src.isPlaying)
            src.Play();
    }

    private void SetVisible(bool on)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = on;

        if (scanner != null)
            scanner.enabled = on;
    }

    private void LoadModelIfNeeded()
    {
        if (GetComponentInChildren<MeshFilter>(true) != null)
            return;

        // The lattice tower reads as a gantry crane, so it stands in for the
        // depot gate until the real asset arrives.
        GameObject prefab = Resources.Load<GameObject>("Depot/DepotGate") ??
                            Resources.Load<GameObject>("Tower/RadioTower");
        if (prefab == null)
            return;

        GameObject model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.name = "DepotGateModel";
    }

    /// A thin red plane across the road: the scanner line the player rides into.
    private void BuildBeam()
    {
        if (_beam != null)
            return;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ScannerBeam";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(11f, 1.4f, 1f);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Transparent") ??
                        Shader.Find("Sprites/Default");
        var material = new Material(shader);
        Color tint = new Color(scannerColor.r, scannerColor.g, scannerColor.b, 0.45f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        material.renderQueue = 3060;
        go.GetComponent<Renderer>().sharedMaterial = material;

        _beam = go.transform;
    }

    private void EnsureScanner()
    {
        if (scanner == null)
            scanner = GetComponentInChildren<Light>();

        Transform socket = FindNamed(transform, "Beacon") ?? FindNamed(transform, "Scanner");
        if (scanner == null)
        {
            GameObject lightGo = new GameObject("Scanner");
            lightGo.transform.SetParent(socket != null ? socket : transform, false);
            if (socket == null)
                lightGo.transform.localPosition = new Vector3(0f, 24f, 0f);

            scanner = lightGo.AddComponent<Light>();
            scanner.type = LightType.Point;
            scanner.range = 80f;
        }
        else if (socket != null && scanner.transform.parent != socket)
        {
            scanner.transform.SetParent(socket, false);
            scanner.transform.localPosition = Vector3.zero;
        }

        scanner.color = scannerColor;
        ConfigureAudio();
    }

    /// Not a radio. Retrieval reading the day's list, on a loop.
    private void ConfigureAudio()
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/Audio_Depot_List") ??
                         Resources.Load<AudioClip>("Audio/Audio_Tower_Radio");
        if (clip == null)
            return;

        AudioSource src = GetComponent<AudioSource>();
        if (src == null)
            src = gameObject.AddComponent<AudioSource>();

        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 40f;
        src.maxDistance = 480f;
        src.volume = 0.42f;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private void Update()
    {
        TryPlaceOnPath();

        if (!_placed)
            return;

        float wave = Mathf.PingPong(Time.time * sweepSpeed, 1f);
        wave = wave * wave;

        if (scanner != null)
            scanner.intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);

        if (_beam != null)
            _beam.localScale = new Vector3(11f, Mathf.Lerp(0.6f, 2.2f, wave), 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
