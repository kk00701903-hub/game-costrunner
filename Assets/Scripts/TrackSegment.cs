using System;
using System.Collections.Generic;
using UnityEngine;

public enum SegmentKind
{
    Straight,
    CornerLeft,
    CornerRight
}

public class TrackSegment : MonoBehaviour
{
    [SerializeField] private SegmentKind kind = SegmentKind.Straight;

    [Tooltip("Centre-line length of a straight tile. Origin must sit at the entry (min Z) of the mesh.")]
    [SerializeField] private float length = 30f;

    [Tooltip("Entry to corner centre, and corner centre to exit. Corner tiles ignore Length.")]
    [SerializeField] private float cornerArm = 15f;

    [SerializeField] private float roadHalfWidth = 5f;

    public SegmentKind Kind => kind;
    public bool IsCorner => kind != SegmentKind.Straight;
    public int PrefabIndex { get; set; }

    /// Centre-line distance this tile adds to the path.
    public float PathLength => IsCorner ? cornerArm * 2f : length;

    /// Usable length measured along the entry direction. Dressing and hazards use this.
    public float Length => IsCorner ? cornerArm : length;

    public float PathStart { get; set; }
    public float PathEnd => PathStart + PathLength;

    /// Path distance of the corner centre, where the actual rotation happens.
    public float TurnDistance => PathStart + cornerArm;

    public int TurnDirection =>
        kind == SegmentKind.CornerRight ? 1 :
        kind == SegmentKind.CornerLeft ? -1 : 0;

    public Vector3 CornerCenterWorld => transform.TransformPoint(new Vector3(0f, 0f, cornerArm));

    public Vector3 ExitLocalPosition => IsCorner
        ? new Vector3(TurnDirection * cornerArm, 0f, cornerArm)
        : new Vector3(0f, 0f, length);

    public float ExitYawDelta => TurnDirection * 90f;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private MaterialPropertyBlock _block;

    public void SetKind(SegmentKind value)
    {
        kind = value;
    }

    public void EnsurePlayable()
    {
        DetectKindFromName();

        if (!IsCorner)
            CaptureLengthFromMeshes();

        EnsureGroundCollider();
        ArtLibrary.EnsureVisible(gameObject);
        ApplyLibraryMaterials();
    }

    private void DetectKindFromName()
    {
        if (IsCorner)
            return;

        string n = name;
        if (n.IndexOf("CornerR", StringComparison.OrdinalIgnoreCase) >= 0)
            kind = SegmentKind.CornerRight;
        else if (n.IndexOf("CornerL", StringComparison.OrdinalIgnoreCase) >= 0)
            kind = SegmentKind.CornerLeft;
    }

    private void CaptureLengthFromMeshes()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        float maxZ = 0f;
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
                continue;

            Bounds b = mesh.bounds;
            Vector3 worldSize = filters[i].transform.TransformVector(b.size);
            maxZ = Mathf.Max(maxZ, Mathf.Abs(worldSize.z));
        }

        if (maxZ > 1f)
            length = maxZ;
    }

    private void EnsureGroundCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
            return;

        float w = roadHalfWidth * 2f;

        if (!IsCorner)
        {
            var straight = gameObject.AddComponent<BoxCollider>();
            straight.center = new Vector3(0f, 0.15f, length * 0.5f);
            straight.size = new Vector3(w, 0.35f, length);
            return;
        }

        // L shape: the entry arm up to the corner square, plus the exit arm across it.
        var entry = gameObject.AddComponent<BoxCollider>();
        entry.center = new Vector3(0f, 0.15f, cornerArm * 0.5f);
        entry.size = new Vector3(w, 0.35f, cornerArm);

        // Spans from the far kerb of the entry arm out to the exit, so the
        // corner square itself stays solid whichever way the player is facing.
        var exit = gameObject.AddComponent<BoxCollider>();
        exit.center = new Vector3(TurnDirection * (cornerArm - roadHalfWidth) * 0.5f, 0.15f, cornerArm);
        exit.size = new Vector3(cornerArm + roadHalfWidth, 0.35f, w);
    }

    private void OnEnable()
    {
        if (ZoneDirector.Instance != null)
            ZoneDirector.Instance.TintSegment(this);
    }

    public void RegisterSpawned(GameObject go)
    {
        if (go != null && !_spawned.Contains(go))
            _spawned.Add(go);
    }

    public void ReleaseSpawned(Action<GameObject> release)
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null && release != null)
                release(_spawned[i]);
        }

        _spawned.Clear();
    }

    public void ApplyZoneColors(Color roadColor, Color obstacleColor)
    {
        ApplyLibraryMaterials();

        if (_block == null)
            _block = new MaterialPropertyBlock();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            string n = renderer.gameObject.name;
            if (renderer.gameObject.CompareTag("Supply") ||
                n.IndexOf("Supply", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            Color color = IsObstacle(renderer) ? obstacleColor : roadColor;
            if (n.IndexOf("Lane", StringComparison.OrdinalIgnoreCase) >= 0)
                color = Color.Lerp(roadColor, Color.white, 0.55f);

            renderer.GetPropertyBlock(_block);
            _block.SetColor("_Color", color);
            _block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(_block);
        }
    }

    public void ApplyLibraryMaterials()
    {
        MaterialLibrary library = MaterialLibrary.Active;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (library != null && library.HasBakedSurfaces)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer)
                    continue;

                string n = renderer.gameObject.name;
                Material mat = library.Surface(IsObstacle(renderer) ? "Metal" : n);
                if (mat != null)
                    renderer.sharedMaterial = mat;
            }

            return;
        }

        Material fallbackRoad = ArtLibrary.Surface(
            "Road_Asphalt", "Road_Asphalt_Normal", new Color(0.38f, 0.38f, 0.40f), new Vector2(4f, 4f), 0.16f);
        if (fallbackRoad == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || IsObstacle(renderer))
                continue;

            renderer.sharedMaterial = fallbackRoad;
        }
    }

    private static bool IsObstacle(Renderer renderer)
    {
        if (renderer.gameObject.CompareTag("Obstacle"))
            return true;

        return renderer.gameObject.name.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
