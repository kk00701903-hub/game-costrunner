using UnityEngine;

/// Retrieval flies between the collapse line and Doha. They are not hunting
/// her; they are filing her. The tags she picks up for points are the same tags
/// that tell them where she is, so the score and the danger are one number.
public class RetrievalDrones : MonoBehaviour
{
    public static RetrievalDrones Instance { get; private set; }

    [SerializeField] private int droneCount = 3;
    [SerializeField] private float idleGap = 34f;
    [SerializeField] private float closeGap = 6f;
    [SerializeField] private float catchGap = 1.2f;
    [Tooltip("Tags held before the drones start closing at all.")]
    [SerializeField] private int lockOnTags = 6;
    [Tooltip("Tags at which they are as accurate as they get.")]
    [SerializeField] private int fullLockTags = 24;
    [SerializeField] private float height = 4.2f;

    private Transform[] _drones;
    private PlayerController _player;
    private float _gap;

    /// 0 while Doha is anonymous, 1 when the list knows exactly where she is.
    public float Accuracy
    {
        get
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(lockOnTags, fullLockTags, gm.Tags));
        }
    }

    public float Gap => _gap;

    private void Awake()
    {
        Instance = this;
        _gap = idleGap;
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        Build();
        SetVisible(false);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || _player == null)
            return;

        if (!gm.IsPlaying || _player.IsDead)
        {
            SetVisible(false);
            return;
        }

        float target = Mathf.Lerp(idleGap, closeGap, Accuracy);
        _gap = Mathf.MoveTowards(_gap, target, 6f * Time.deltaTime);

        if (!Follow())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (_gap <= catchGap)
            gm.GameOver(DeathCause.Retrieved);
    }

    private bool Follow()
    {
        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner == null || _drones == null)
            return false;

        float behind = _player.PathDistance - _gap;
        if (behind < 0f)
            return false;

        Vector3 point;
        float yaw;
        if (!spawner.TryGetPoint(behind, out point, out yaw))
            return false;

        Quaternion frame = Quaternion.Euler(0f, yaw, 0f);
        float lane = _player.LaneOffset;

        for (int i = 0; i < _drones.Length; i++)
        {
            // They fan out and bob out of phase, which reads as three machines
            // doing the same job rather than one object with three heads.
            float side = _drones.Length > 1 ? (i / (float)(_drones.Length - 1)) * 2f - 1f : 0f;
            float bob = Mathf.Sin(Time.time * 2.2f + i * 1.7f) * 0.35f;
            Vector3 local = new Vector3(side * lane * 0.9f, height + bob, -i * 1.4f);
            _drones[i].position = point + frame * local;
            _drones[i].rotation = frame;
        }

        return true;
    }

    private void SetVisible(bool on)
    {
        if (_drones == null)
            return;

        for (int i = 0; i < _drones.Length; i++)
        {
            if (_drones[i] != null && _drones[i].gameObject.activeSelf != on)
                _drones[i].gameObject.SetActive(on);
        }
    }

    private void Build()
    {
        if (_drones != null)
            return;

        GameObject prefab = Resources.Load<GameObject>("Retrieval/Drone_Retrieval");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Retrieval/Drone_RetrievalModel");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Character/Retrieval_Drone");
        int count = Mathf.Max(1, droneCount);
        _drones = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, transform);
                go.name = "Drone" + i;
                ArtLibrary.EnsureVisible(go);
                Collider[] cols = go.GetComponentsInChildren<Collider>();
                for (int c = 0; c < cols.Length; c++)
                    Destroy(cols[c]);
            }
            else
            {
                go = BuildDronePrimitive(i);
            }

            _drones[i] = go.transform;
        }
    }

    private GameObject BuildDronePrimitive(int index)
    {
        GameObject go = new GameObject("Drone" + index);
        go.transform.SetParent(transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        body.transform.localScale = new Vector3(0.5f, 0.28f, 0.5f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Destroy(body.GetComponent<Collider>());
        Paint(body, new Color(0.86f, 0.86f, 0.84f));

        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "Lamp";
        lamp.transform.SetParent(go.transform, false);
        lamp.transform.localPosition = new Vector3(0f, -0.1f, 0.32f);
        lamp.transform.localScale = Vector3.one * 0.18f;
        Destroy(lamp.GetComponent<Collider>());
        Paint(lamp, new Color(0.92f, 0.20f, 0.16f));

        return go;
    }

    private static void Paint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (material.HasProperty("_Color"))
            material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        renderer.sharedMaterial = material;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
