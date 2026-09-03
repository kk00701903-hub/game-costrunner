using System.Collections.Generic;
using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    public static RoadSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private List<GameObject> trackPrefabs = new List<GameObject>();
    [SerializeField] private GameObject safePrefab;
    [SerializeField] private int safeInitialCount = 2;

    [Header("Pooling")]
    [SerializeField] private int initialSpawnCount = 8;
    [SerializeField] private int poolSize = 4;
    [SerializeField] private float spawnAheadDistance = 90f;
    [SerializeField] private float recycleDistance = 40f;

    [Header("Corners")]
    [Tooltip("Straight tiles that must sit between two corners.")]
    [SerializeField] private int minStraightBetweenCorners = 2;
    [SerializeField] [Range(0f, 1f)] private float cornerChanceStart = 0.22f;
    [SerializeField] [Range(0f, 1f)] private float cornerChanceEnd = 0.48f;
    [Tooltip("Keep the goal tile straight so the tower is not planted inside a corner.")]
    [SerializeField] private float goalStraightMargin = 60f;

    [Header("Lane Spawns")]
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private List<GameObject> obstaclePrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> supplyPrefabs = new List<GameObject>();
    [SerializeField] [Range(0f, 1f)] private float obstacleChance = 0.72f;
    [SerializeField] [Range(0f, 1f)] private float supplyChance = 0.58f;
    [SerializeField] private float obstacleY = 0.15f;
    [SerializeField] private float supplyY = 0.15f;
    [SerializeField] private int spawnPoolSize = 16;

    [Header("Roadside Dressing")]
    [SerializeField] private List<GameObject> propPrefabs = new List<GameObject>();
    [SerializeField] private float roadHalfWidth = 5f;
    [SerializeField] private float dressY = 0.15f;

    private readonly Dictionary<int, Queue<TrackSegment>> _tilePools = new Dictionary<int, Queue<TrackSegment>>();
    private readonly Queue<GameObject> _obstaclePool = new Queue<GameObject>();
    private readonly Dictionary<string, Queue<GameObject>> _supplyPools = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<PickupKind, GameObject> _supplyByKind = new Dictionary<PickupKind, GameObject>();
    private readonly Dictionary<string, Queue<GameObject>> _propPools = new Dictionary<string, Queue<GameObject>>();
    private readonly List<GameObject> _leftProps = new List<GameObject>();
    private readonly List<GameObject> _rightProps = new List<GameObject>();
    private readonly List<int> _straightIndices = new List<int>();
    private readonly List<int> _cornerIndices = new List<int>();

    /// The boss arena keeps the runner's rules but not its geometry: judgement
    /// is being measured, so the road must not also be asking a question.
    public bool ForceStraight { get; set; }

    /// Set while the king fight owns spawning, so the road stops laying hazards.
    public bool SuppressHazards { get; set; }

    private readonly TrackPath _path = new TrackPath();
    private readonly List<int> _zonedStraights = new List<int>();
    private GameObject _guardrailPrefab;
    private PlayerController _playerController;
    private System.Random _rng;
    private int _tilesSpawned;
    private int _straightsSinceCorner;
    private int _lastCornerDirection;
    private int _sameDirectionStreak;

    private void Awake()
    {
        Instance = this;
        _rng = new System.Random(GameManager.Instance != null ? GameManager.Instance.Seed : 347);
    }

    // Course generation runs off its own stream so the same seed always lays
    // the same road, whatever else is calling UnityEngine.Random.
    private float Value()
    {
        return (float)_rng.NextDouble();
    }

    private float RangeF(float min, float max)
    {
        return min + (max - min) * Value();
    }

    private int RangeI(int min, int max)
    {
        return max <= min ? min : _rng.Next(min, max);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null)
            _playerController = player.GetComponent<PlayerController>();

        TryLoadBuiltinTracks();
        TryLoadBuiltinHazards();
        TryLoadBuiltinSupplies();
        TryLoadBuiltinProps();

        if (!HasPrefab(trackPrefabs) || !PrefabsAreRenderable(trackPrefabs))
        {
            if (HasPrefab(trackPrefabs))
                Debug.LogWarning("RoadSpawner: track meshes not renderable — trying Kenney, then primitives.");

            trackPrefabs = TestCatalog.CreateTracks();
        }
        else
        {
            Debug.Log("347 Road: " + trackPrefabs.Count + " track prefabs (Kenney/GLB).");
        }
        if (!HasPrefab(obstaclePrefabs))
            obstaclePrefabs = TestCatalog.CreateObstacles();
        if (!HasPrefab(supplyPrefabs))
            supplyPrefabs = TestCatalog.CreateSupplies();
        if (!HasPrefab(propPrefabs))
            propPrefabs = TestCatalog.CreateProps();

        if (trackPrefabs == null || !HasPrefab(trackPrefabs))
        {
            Debug.LogError("RoadSpawner: assign track prefabs, or put Track_* under Resources/Tracks (GLB/FBX).");
            enabled = false;
            return;
        }

        if (safePrefab != null && !trackPrefabs.Contains(safePrefab))
            trackPrefabs.Insert(0, safePrefab);

        EnsureCornerPrefabs();
        BuildPools();
        ClassifyTracks();

        _path.Reset(Vector3.zero, 0f);

        int count = Mathf.Max(1, initialSpawnCount);
        for (int i = 0; i < count; i++)
            SpawnNext(i < safeInitialCount);
    }

    private void Update()
    {
        if (player == null)
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        float travelled = PlayerPathDistance();

        int guard = 0;
        while (_path.CursorDistance < travelled + spawnAheadDistance && guard++ < 32)
            SpawnNext(false);

        while (_path.Count > 2)
        {
            TrackSegment front = _path.Oldest;
            if (front == null)
            {
                _path.RemoveOldest();
                continue;
            }

            if (front.PathEnd >= travelled - recycleDistance)
                break;

            Recycle(_path.RemoveOldest());
        }
    }

    private float PlayerPathDistance()
    {
        if (_playerController != null)
            return _playerController.PathDistance;

        return player != null ? player.position.z : 0f;
    }

    /// Used by the king fight, which owns its own item pacing while the road's
    /// hazard tables are suppressed.
    public bool DropItem(PickupKind kind, int lane, float leadMetres)
    {
        if (_playerController == null)
            return false;

        float distance = _playerController.PathDistance + leadMetres;
        TrackSegment segment = _path.SegmentAt(distance);
        if (segment == null)
            return false;

        GameObject go = GetSupply(kind);
        if (go == null)
            return false;

        PlaceLocal(segment, go, new Vector3(lane * laneOffset, supplyY, distance - segment.PathStart));
        return true;
    }

    /// The boss sits further out than the normal spawn horizon, so the arena
    /// has to lay road ahead of him.
    public void SetSpawnAhead(float metres)
    {
        spawnAheadDistance = Mathf.Max(60f, metres);
    }

    public bool TryGetTurn(float pathDistance, out TurnPrompt prompt)
    {
        return _path.TryGetTurn(pathDistance, out prompt);
    }

    public bool TryGetPoint(float pathDistance, out Vector3 position, out float yaw)
    {
        return _path.TryGetPoint(pathDistance, out position, out yaw);
    }

    private void TryLoadBuiltinTracks()
    {
        var merged = new List<GameObject>();
        if (HasPrefab(trackPrefabs))
            merged.AddRange(trackPrefabs);

        AppendResourceTracks(merged, "Tracks");
        AppendResourceTracks(merged, "Props/Kenney");

        if (merged.Count > 0)
        {
            trackPrefabs = merged;
            EnsureSafePrefab();
            return;
        }

        EnsureSafePrefab();
    }

    private static void AppendResourceTracks(List<GameObject> list, string resourceFolder)
    {
        GameObject[] loaded = Resources.LoadAll<GameObject>(resourceFolder);
        if (loaded == null || loaded.Length == 0)
            return;

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));

        for (int i = 0; i < loaded.Length; i++)
        {
            GameObject prefab = loaded[i];
            if (prefab == null || prefab.name.IndexOf("Track_", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (resourceFolder.IndexOf("Kenney", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (prefab.GetComponentsInChildren<MeshFilter>(true).Length == 0)
                    continue;
            }
            else if (!IsPlayableTrackMesh(prefab))
            {
                continue;
            }

            if (ContainsPrefab(list, prefab))
                continue;

            list.Add(prefab);
        }
    }

    private static bool ContainsPrefab(List<GameObject> list, GameObject prefab)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].name == prefab.name)
                return true;
        }

        return false;
    }

    private static bool IsPlayableTrackMesh(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        float maxX = 0f;
        float maxZ = 0f;
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Vector3 size = mesh.bounds.size;
            Vector3 scale = filters[i].transform.localScale;
            // Walk up parents for FBX import scale.
            Transform t = filters[i].transform;
            while (t != null && t != root.transform.parent)
            {
                scale = Vector3.Scale(scale, t.localScale);
                if (t == root.transform)
                    break;
                t = t.parent;
            }

            maxX = Mathf.Max(maxX, Mathf.Abs(size.x * scale.x));
            maxZ = Mathf.Max(maxZ, Mathf.Abs(size.z * scale.z));
            maxX = Mathf.Max(maxX, Mathf.Abs(size.x)); // lossy fallback for imported scale
            maxZ = Mathf.Max(maxZ, Mathf.Abs(size.z));
        }

        // Real Track_Straight.glb is ~10×30. Reject Kenney 1-unit tiles even if scaled in importer later.
        return maxX >= 8f && maxZ >= 20f;
    }

    private void TryLoadBuiltinHazards()
    {
        if (HasPrefab(obstaclePrefabs))
            return;

        GameObject[] loaded = Resources.LoadAll<GameObject>("Hazards");
        if (loaded == null || loaded.Length == 0)
            return;

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        obstaclePrefabs = new List<GameObject>(loaded);
    }

    private void TryLoadBuiltinSupplies()
    {
        if (HasPrefab(supplyPrefabs))
            return;

        GameObject[] loaded = Resources.LoadAll<GameObject>("Items");
        if (loaded == null || loaded.Length == 0)
            return;

        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
        supplyPrefabs = new List<GameObject>(loaded);
    }

    private void TryLoadBuiltinProps()
    {
        if (!HasPrefab(propPrefabs))
        {
            GameObject[] loaded = Resources.LoadAll<GameObject>("Props");
            if (loaded != null && loaded.Length > 0)
            {
                System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
                propPrefabs = new List<GameObject>(loaded);
            }
        }

        ClassifyProps();
    }

    /// GLB corner tiles are optional; fall back to primitives so turning always works.
    private void EnsureCornerPrefabs()
    {
        bool hasLeft = false;
        bool hasRight = false;
        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] == null)
                continue;

            string n = trackPrefabs[i].name;
            if (n.IndexOf("CornerR", System.StringComparison.OrdinalIgnoreCase) >= 0)
                hasRight = true;
            else if (n.IndexOf("CornerL", System.StringComparison.OrdinalIgnoreCase) >= 0)
                hasLeft = true;
        }

        if (hasLeft && hasRight)
            return;

        List<GameObject> corners = TestCatalog.CreateCorners();
        for (int i = 0; i < corners.Count; i++)
        {
            if (corners[i] == null)
                continue;

            bool right = corners[i].name.IndexOf("CornerR", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (right ? hasRight : hasLeft)
                continue;

            trackPrefabs.Add(corners[i]);
        }
    }

    private void ClassifyTracks()
    {
        _straightIndices.Clear();
        _cornerIndices.Clear();

        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] == null)
                continue;

            TrackSegment segment = trackPrefabs[i].GetComponent<TrackSegment>();
            bool corner = segment != null
                ? segment.IsCorner
                : trackPrefabs[i].name.IndexOf("Corner", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (corner)
                _cornerIndices.Add(i);
            else
                _straightIndices.Add(i);
        }
    }

    private void ClassifyProps()
    {
        _leftProps.Clear();
        _rightProps.Clear();
        _guardrailPrefab = null;

        for (int i = 0; i < propPrefabs.Count; i++)
        {
            GameObject prefab = propPrefabs[i];
            if (prefab == null)
                continue;

            string n = prefab.name;
            if (n.IndexOf("Guardrail", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _guardrailPrefab = prefab;
                continue;
            }

            bool left = n.IndexOf("House", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Lamp", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool right = n.IndexOf("Boat", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Lamp", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (left)
                _leftProps.Add(prefab);
            if (right)
                _rightProps.Add(prefab);
            if (!left && !right)
                _rightProps.Add(prefab);
        }
    }

    private void EnsureSafePrefab()
    {
        if (safePrefab != null)
            return;

        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] != null &&
                trackPrefabs[i].name.IndexOf("Straight", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                safePrefab = trackPrefabs[i];
                return;
            }
        }

        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] != null &&
                trackPrefabs[i].name.IndexOf("Corner", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                safePrefab = trackPrefabs[i];
                return;
            }
        }
    }

    public void ForEachActiveSegment(System.Action<TrackSegment> visitor)
    {
        _path.ForEach(visitor);
    }

    private void BuildPools()
    {
        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] == null)
                continue;

            _tilePools[i] = new Queue<TrackSegment>();
            for (int n = 0; n < poolSize; n++)
                _tilePools[i].Enqueue(CreateTile(i));
        }

        FillObstaclePool();
        FillSupplyPools();
    }

    private void FillObstaclePool()
    {
        if (!HasPrefab(obstaclePrefabs))
            return;

        for (int i = 0; i < obstaclePrefabs.Count; i++)
        {
            if (obstaclePrefabs[i] == null)
                continue;

            _obstaclePool.Enqueue(CreatePooled(obstaclePrefabs[i], true));
            _obstaclePool.Enqueue(CreatePooled(obstaclePrefabs[i], true));
        }

        int extra = Mathf.Max(0, spawnPoolSize - _obstaclePool.Count);
        for (int i = 0; i < extra; i++)
            _obstaclePool.Enqueue(CreatePooled(obstaclePrefabs[RangeI(0, obstaclePrefabs.Count)], true));
    }

    /// Items are pooled per prefab because the director has to be able to ask
    /// for one specific kind, not for whatever came back first.
    private void FillSupplyPools()
    {
        _supplyByKind.Clear();
        if (!HasPrefab(supplyPrefabs))
            return;

        for (int i = 0; i < supplyPrefabs.Count; i++)
        {
            GameObject prefab = supplyPrefabs[i];
            if (prefab == null)
                continue;

            PickupKind kind = Pickup.KindFromName(prefab.name);
            if (!_supplyByKind.ContainsKey(kind))
                _supplyByKind[kind] = prefab;

            Queue<GameObject> pool = SupplyPool(prefab.name);
            pool.Enqueue(CreatePooled(prefab, false));
            pool.Enqueue(CreatePooled(prefab, false));
        }
    }

    private Queue<GameObject> SupplyPool(string key)
    {
        Queue<GameObject> pool;
        if (!_supplyPools.TryGetValue(key, out pool))
        {
            pool = new Queue<GameObject>();
            _supplyPools[key] = pool;
        }

        return pool;
    }

    private TrackSegment CreateTile(int prefabIndex)
    {
        GameObject go = Instantiate(trackPrefabs[prefabIndex], transform);
        go.name = trackPrefabs[prefabIndex].name;
        go.SetActive(false);

        TrackSegment segment = go.GetComponent<TrackSegment>();
        if (segment == null)
            segment = go.AddComponent<TrackSegment>();

        segment.PrefabIndex = prefabIndex;
        segment.EnsurePlayable();
        ArtLibrary.EnsureVisible(go);
        return segment;
    }

    private GameObject CreatePooled(GameObject prefab, bool asObstacle)
    {
        GameObject go = Instantiate(prefab, transform);
        go.name = prefab.name;
        go.SetActive(false);
        if (asObstacle)
            PrepareObstacle(go);
        else
            PrepareSupply(go);
        return go;
    }

    private static void PrepareSupply(GameObject go)
    {
        if (go == null)
            return;

        go.tag = "Supply";
        Transform[] children = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.tag = "Supply";

        if (go.name.IndexOf("Bottle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            go.name.IndexOf("Can", System.StringComparison.OrdinalIgnoreCase) >= 0)
            ScaleToHeight(go, 0.6f);

        Pickup pickup = go.GetComponent<Pickup>();
        if (pickup == null)
            pickup = go.AddComponent<Pickup>();
        pickup.Kind = Pickup.KindFromName(go.name);

        Collider col = go.GetComponentInChildren<Collider>(true);
        if (col == null)
        {
            var box = go.AddComponent<BoxCollider>();
            FitBoxToMeshes(go, box, new Vector3(0.6f, 0.6f, 0.6f), new Vector3(0f, 0.3f, 0f));
            col = box;
        }

        col.isTrigger = true;
    }

    private static void PrepareObstacle(GameObject go)
    {
        if (go == null)
            return;

        go.tag = "Obstacle";
        Transform[] children = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.tag = "Obstacle";

        // Physics is off. Hits go through HazardVolume + AABB.
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        HazardVolume hazard = go.GetComponent<HazardVolume>();
        if (hazard == null)
            hazard = go.AddComponent<HazardVolume>();
        hazard.ConfigureFromRenderer();
    }

    private static void FitSlideBarrier(GameObject go)
    {
        Collider[] existing = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < existing.Length; i++)
            existing[i].enabled = false;

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
            box = go.AddComponent<BoxCollider>();
        box.enabled = true;
        box.isTrigger = false;
        FitBoxToMeshes(go, box, new Vector3(1.4f, 0.55f, 0.45f), new Vector3(0f, 0.28f, 0f));
        if (box.size.y > 0.58f)
        {
            box.size = new Vector3(box.size.x, 0.55f, box.size.z);
            box.center = new Vector3(box.center.x, 0.28f, box.center.z);
        }
    }

    private static void FitBoxToMeshes(GameObject go, BoxCollider box, Vector3 fallbackSize, Vector3 fallbackCenter)
    {
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
        {
            box.size = fallbackSize;
            box.center = fallbackCenter;
            return;
        }

        bool any = false;
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null)
                continue;
            Bounds mb = filters[i].sharedMesh.bounds;
            Vector3 worldCenter = filters[i].transform.TransformPoint(mb.center);
            Vector3 worldSize = filters[i].transform.TransformVector(mb.size);
            Bounds wb = new Bounds(worldCenter, new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z)));
            if (!any)
            {
                b = wb;
                any = true;
            }
            else
                b.Encapsulate(wb);
        }

        if (!any)
        {
            box.size = fallbackSize;
            box.center = fallbackCenter;
            return;
        }

        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 localSize = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private static void ScaleToHeight(GameObject go, float height)
    {
        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);
        float maxY = 0f;
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null)
                continue;
            float y = Mathf.Abs(filters[i].transform.TransformVector(filters[i].sharedMesh.bounds.size).y);
            if (y > maxY)
                maxY = y;
        }

        if (maxY < 0.05f || Mathf.Abs(maxY - height) < 0.08f)
            return;

        go.transform.localScale *= height / maxY;
    }

    private void SpawnNext(bool useSafe)
    {
        int index = PickPrefabIndex(useSafe);
        if (index < 0)
            return;

        TrackSegment segment = GetTile(index);
        _path.Place(segment);
        segment.gameObject.SetActive(true);

        if (segment.IsCorner)
        {
            _straightsSinceCorner = 0;
            _sameDirectionStreak = segment.TurnDirection == _lastCornerDirection ? _sameDirectionStreak + 1 : 1;
            _lastCornerDirection = segment.TurnDirection;
        }
        else
        {
            _straightsSinceCorner++;
        }

        Populate(segment, useSafe || _tilesSpawned < safeInitialCount);
        DressSegment(segment);
        _tilesSpawned++;
    }

    private void Populate(TrackSegment segment, bool safe)
    {
        if (segment.IsCorner)
        {
            PopulateCorner(segment);
            return;
        }

        if (safe || SuppressHazards)
            return;

        int[] lanes = { -1, 0, 1 };
        for (int i = 0; i < lanes.Length; i++)
        {
            int swap = RangeI(i, lanes.Length);
            int tmp = lanes[i];
            lanes[i] = lanes[swap];
            lanes[swap] = tmp;
        }

        int laneIndex = 0;

        // The director never adds lanes of hazards, it only scales how likely a
        // tile is to carry one, and guarantees a way through when it is bad.
        int maxHazardLanes = SpawnDirector.GuaranteeSafeLane ? 1 : 2;
        float chance = Mathf.Clamp01(obstacleChance * SpawnDirector.ObstacleScale);

        if (HasPrefab(obstaclePrefabs) && Value() <= chance)
        {
            Place(segment, GetObstacle(), lanes[laneIndex], obstacleY);
            laneIndex++;

            if (laneIndex < maxHazardLanes && Value() <= chance * 0.35f)
            {
                Place(segment, GetObstacle(), lanes[laneIndex], obstacleY);
                laneIndex++;
            }
        }

        if (laneIndex < lanes.Length && Value() <= supplyChance)
        {
            GameObject item = GetDirectedSupply();
            if (item != null)
                Place(segment, item, lanes[laneIndex], supplyY);
        }

        if (ZoneDirector.Instance != null)
            ZoneDirector.Instance.TintSegment(segment);
    }

    /// The tell of a good correction is that it never shows. A player who is
    /// doing well simply never sees deck tape, and never knows why.
    private GameObject GetDirectedSupply()
    {
        if (_supplyByKind.Count == 0)
            return null;

        float heal = SpawnDirector.HealChance;
        if (heal > 0f && Value() < heal && _supplyByKind.ContainsKey(PickupKind.DeckTape))
            return GetSupply(PickupKind.DeckTape);

        // Letters are the one thing the director never hands out on request.
        PickupKind kind = RollSupplyKind();
        if (kind == PickupKind.DeckTape)
            kind = PickupKind.BoosterCell;

        return GetSupply(kind);
    }

    private PickupKind RollSupplyKind()
    {
        float roll = Value();
        if (roll < 0.42f)
            return PickupKind.Coin;
        if (roll < 0.66f)
            return PickupKind.Tag;
        if (roll < 0.78f)
            return PickupKind.BoosterCell;
        if (roll < 0.86f)
            return PickupKind.Shield;
        if (roll < 0.93f)
            return PickupKind.ReverseScan;
        if (roll < 0.98f)
            return PickupKind.DeckPiece;

        return PickupKind.Letter;
    }

    /// Corners stay hazard free. A supply line hugs the inside of the turn so
    /// cutting the corner tight is rewarded instead of punished.
    private void PopulateCorner(TrackSegment segment)
    {
        if (HasPrefab(supplyPrefabs) && !SuppressHazards)
        {
            int lane = segment.TurnDirection;
            float arm = segment.Length;
            for (int i = 0; i < 3; i++)
            {
                GameObject go = GetSupply(PickupKind.Coin);
                if (go == null)
                    break;

                PlaceLocal(segment, go, new Vector3(lane * laneOffset, supplyY, arm * (0.45f + i * 0.2f)));
            }
        }

        if (ZoneDirector.Instance != null)
            ZoneDirector.Instance.TintSegment(segment);
    }

    private void DressSegment(TrackSegment segment)
    {
        if (segment == null || !HasPrefab(propPrefabs))
            return;

        float len = segment.Length;
        float edge = roadHalfWidth + 0.4f;

        if (segment.IsCorner)
        {
            // Only the outside of the bend gets dressing; the inside must stay
            // readable so the player can see where the road goes.
            float outside = -segment.TurnDirection * edge;
            if (_guardrailPrefab != null)
                PlaceDressing(segment, GetProp(_guardrailPrefab), outside, dressY, len * 0.5f);

            List<GameObject> pool = segment.TurnDirection > 0 ? _leftProps : _rightProps;
            if (pool.Count > 0)
            {
                PlaceDressing(
                    segment,
                    GetProp(pool[RangeI(0, pool.Count)]),
                    -segment.TurnDirection * RangeF(edge + 3f, 13.5f),
                    dressY,
                    RangeF(len * 0.6f, len + 8f));
            }

            return;
        }

        if (_guardrailPrefab != null)
            PlaceDressing(segment, GetProp(_guardrailPrefab), edge, dressY, len * 0.5f);

        if (_leftProps.Count > 0 && Value() < 0.9f)
        {
            PlaceDressing(
                segment,
                GetProp(_leftProps[RangeI(0, _leftProps.Count)]),
                RangeF(-13.5f, -edge),
                dressY,
                RangeF(4f, Mathf.Max(6f, len - 4f)));
        }

        if (_rightProps.Count > 0 && Value() < 0.75f)
        {
            PlaceDressing(
                segment,
                GetProp(_rightProps[RangeI(0, _rightProps.Count)]),
                RangeF(edge, 13.5f),
                dressY,
                RangeF(4f, Mathf.Max(6f, len - 4f)));
        }
    }

    private void PlaceDressing(TrackSegment segment, GameObject go, float x, float y, float z)
    {
        if (go == null || segment == null)
            return;

        go.transform.SetParent(segment.transform, false);
        go.transform.localPosition = new Vector3(x, y, z);
        bool rail = go.name.IndexOf("Guardrail", System.StringComparison.OrdinalIgnoreCase) >= 0;
        go.transform.localRotation = rail
            ? Quaternion.identity
            : Quaternion.Euler(0f, RangeF(-25f, 25f), 0f);
        if (go.GetComponent<MeshLodGroup>() == null)
            go.AddComponent<MeshLodGroup>();
        go.SetActive(true);
        segment.RegisterSpawned(go);
    }

    private GameObject GetProp(GameObject prefab)
    {
        if (prefab == null)
            return null;

        string key = prefab.name;
        if (!_propPools.ContainsKey(key))
            _propPools[key] = new Queue<GameObject>();

        if (_propPools[key].Count > 0)
            return _propPools[key].Dequeue();

        GameObject go = Instantiate(prefab, transform);
        go.name = key;
        go.SetActive(false);
        PrepareDressing(go);
        return go;
    }

    private static void PrepareDressing(GameObject go)
    {
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
    }

    private void Place(TrackSegment segment, GameObject go, int lane, float y)
    {
        if (go == null || segment == null)
            return;

        float z = RangeF(6f, Mathf.Max(8f, segment.Length - 5f));
        PlaceLocal(segment, go, new Vector3(lane * laneOffset, y, z));
    }

    private void PlaceLocal(TrackSegment segment, GameObject go, Vector3 local)
    {
        go.transform.SetParent(segment.transform, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.identity;
        go.SetActive(true);
        segment.RegisterSpawned(go);
    }

    private GameObject GetObstacle()
    {
        if (_obstaclePool.Count > 0)
            return _obstaclePool.Dequeue();

        if (!HasPrefab(obstaclePrefabs))
            return null;

        return CreatePooled(obstaclePrefabs[RangeI(0, obstaclePrefabs.Count)], true);
    }

    private GameObject GetSupply(PickupKind kind)
    {
        GameObject prefab;
        if (!_supplyByKind.TryGetValue(kind, out prefab) || prefab == null)
        {
            // Kind is missing from the catalogue, fall back to anything.
            foreach (KeyValuePair<PickupKind, GameObject> entry in _supplyByKind)
            {
                prefab = entry.Value;
                break;
            }
        }

        if (prefab == null)
            return null;

        Queue<GameObject> pool = SupplyPool(prefab.name);
        return pool.Count > 0 ? pool.Dequeue() : CreatePooled(prefab, false);
    }

    private void Recycle(TrackSegment segment)
    {
        if (segment == null)
            return;

        segment.ReleaseSpawned(ReturnSpawned);
        segment.gameObject.SetActive(false);

        int index = segment.PrefabIndex;
        if (!_tilePools.ContainsKey(index))
            _tilePools[index] = new Queue<TrackSegment>();

        _tilePools[index].Enqueue(segment);
    }

    private void ReturnSpawned(GameObject go)
    {
        if (go == null)
            return;

        go.SetActive(false);
        go.transform.SetParent(transform, false);

        if (go.CompareTag("Supply"))
            SupplyPool(CleanName(go.name)).Enqueue(go);
        else if (go.CompareTag("Obstacle"))
            _obstaclePool.Enqueue(go);
        else
            ReturnProp(go);
    }

    private static string CleanName(string name)
    {
        return name.Replace("(Clone)", string.Empty).Trim();
    }

    private void ReturnProp(GameObject go)
    {
        string key = CleanName(go.name);
        if (!_propPools.ContainsKey(key))
            _propPools[key] = new Queue<GameObject>();
        _propPools[key].Enqueue(go);
    }

    private TrackSegment GetTile(int index)
    {
        if (!_tilePools.ContainsKey(index))
            _tilePools[index] = new Queue<TrackSegment>();

        if (_tilePools[index].Count > 0)
            return _tilePools[index].Dequeue();

        return CreateTile(index);
    }

    private int PickPrefabIndex(bool useSafe)
    {
        if (useSafe)
            return SafeIndex();

        if (ShouldSpawnCorner())
        {
            int corner = PickCornerIndex();
            if (corner >= 0)
                return corner;
        }

        int zoned = PickZonedStraight();
        if (zoned >= 0)
            return zoned;

        if (_straightIndices.Count > 0)
            return _straightIndices[RangeI(0, _straightIndices.Count)];

        return SafeIndex();
    }

    /// Each zone draws from its own slice of the tile catalogue, which is how
    /// the road itself changes without any new geometry.
    private int PickZonedStraight()
    {
        ZoneDirector zones = ZoneDirector.Instance;
        if (zones == null || _straightIndices.Count == 0)
            return -1;

        _zonedStraights.Clear();
        for (int i = 0; i < _straightIndices.Count; i++)
        {
            int index = _straightIndices[i];
            if (trackPrefabs[index] != null && zones.AllowsTile(trackPrefabs[index].name))
                _zonedStraights.Add(index);
        }

        return _zonedStraights.Count > 0 ? _zonedStraights[RangeI(0, _zonedStraights.Count)] : -1;
    }

    private bool ShouldSpawnCorner()
    {
        if (ForceStraight || _cornerIndices.Count == 0)
            return false;

        if (_tilesSpawned < safeInitialCount + 2)
            return false;

        if (_straightsSinceCorner < Mathf.Max(1, minStraightBetweenCorners))
            return false;

        if (GoalIsNear())
            return false;

        float t = 0f;
        if (GameManager.Instance != null && GameManager.Instance.DepotDistance > 0f)
            t = Mathf.Clamp01(1f - GameManager.Instance.RemainingDistance / GameManager.Instance.DepotDistance);

        return Value() < Mathf.Lerp(cornerChanceStart, cornerChanceEnd, t);
    }

    private bool GoalIsNear()
    {
        if (GameManager.Instance == null)
            return false;

        float goal = GameManager.Instance.DepotDistance;
        return _path.CursorDistance + goalStraightMargin >= goal && _path.CursorDistance <= goal + goalStraightMargin;
    }

    private int PickCornerIndex()
    {
        // Three same-direction corners in a row would fold the path back over itself.
        int forbidden = _sameDirectionStreak >= 2 ? _lastCornerDirection : 0;

        List<int> pick = new List<int>();
        for (int i = 0; i < _cornerIndices.Count; i++)
        {
            int index = _cornerIndices[i];
            TrackSegment segment = trackPrefabs[index] != null ? trackPrefabs[index].GetComponent<TrackSegment>() : null;
            int dir = segment != null
                ? segment.TurnDirection
                : trackPrefabs[index].name.IndexOf("CornerR", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : -1;

            if (dir == forbidden)
                continue;

            pick.Add(index);
        }

        if (pick.Count == 0)
            return -1;

        return pick[RangeI(0, pick.Count)];
    }

    private int SafeIndex()
    {
        if (safePrefab != null)
        {
            int safeIndex = trackPrefabs.IndexOf(safePrefab);
            if (safeIndex >= 0)
                return safeIndex;
        }

        if (_straightIndices.Count > 0)
            return _straightIndices[0];

        for (int i = 0; i < trackPrefabs.Count; i++)
        {
            if (trackPrefabs[i] != null)
                return i;
        }

        return -1;
    }

    private static bool PrefabsAreRenderable(List<GameObject> prefabs)
    {
        if (prefabs == null)
            return false;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] == null)
                continue;

            if (IsPlayableTrackMesh(prefabs[i]))
                return true;

            Renderer[] renderers = prefabs[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] is MeshRenderer || renderers[r] is SkinnedMeshRenderer)
                    return true;
            }

            if (prefabs[i].GetComponentsInChildren<MeshFilter>(true).Length > 0)
                return true;
        }

        return false;
    }

    private static bool HasPrefab(List<GameObject> prefabs)
    {
        if (prefabs == null)
            return false;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] != null)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
