using System.Collections.Generic;
using UnityEngine;

public static class TestCatalog
{
    private static Transform Root
    {
        get
        {
            GameObject existing = GameObject.Find("_TestCatalog");
            if (existing != null)
                return existing.transform;

            GameObject root = new GameObject("_TestCatalog");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(root);
            root.SetActive(false);
            return root.transform;
        }
    }

    public static List<GameObject> CreateTracks()
    {
        return new List<GameObject>
        {
            MakeTrack("Track_Straight", new Color(0.38f, 0.38f, 0.40f)),
            MakeTrack("Track_Cracked", new Color(0.42f, 0.36f, 0.30f)),
            MakeTrack("Track_CoastEdge", new Color(0.36f, 0.42f, 0.44f)),
            MakeTrack("Track_Arcade", new Color(0.30f, 0.28f, 0.32f), AddArcadeDressing),
            MakeTrack("Track_Overpass", new Color(0.26f, 0.30f, 0.36f), AddOverpassDressing),
            MakeTrack("Track_Flooded", new Color(0.24f, 0.34f, 0.32f), AddFloodedDressing),
            MakeTrack("Track_Depot", new Color(0.32f, 0.28f, 0.26f), AddDepotDressing)
        };
    }

    /// Placeholder L tiles so turning is testable before Track_Corner*.glb lands.
    public static List<GameObject> CreateCorners()
    {
        return new List<GameObject>
        {
            MakeCorner("Track_CornerL", new Color(0.30f, 0.29f, 0.27f), -1),
            MakeCorner("Track_CornerR", new Color(0.30f, 0.29f, 0.27f), 1)
        };
    }

    public static List<GameObject> CreateObstacles()
    {
        return new List<GameObject>
        {
            MakeBox("Wreck_Car", new Vector3(1.6f, 1.2f, 2.2f), new Color(0.45f, 0.18f, 0.14f), false),
            MakeBox("Debris", new Vector3(1.1f, 1.4f, 1.1f), new Color(0.40f, 0.38f, 0.35f), false),
            MakeBox("Barrier_Low", new Vector3(1.4f, 0.55f, 0.4f), new Color(0.45f, 0.42f, 0.32f), false)
        };
    }

    /// One primitive per pickup kind, so the whole item economy runs before any
    /// of the city art exists.
    public static List<GameObject> CreateSupplies()
    {
        return new List<GameObject>
        {
            MakeItem("Item_Coin", PrimitiveType.Cylinder, new Vector3(0.42f, 0.05f, 0.42f), new Color(0.82f, 0.68f, 0.24f)),
            MakeItem("Item_Tag", PrimitiveType.Cube, new Vector3(0.46f, 0.30f, 0.05f), new Color(0.88f, 0.88f, 0.84f)),
            MakeItem("Item_BoosterCell", PrimitiveType.Cylinder, new Vector3(0.24f, 0.32f, 0.24f), new Color(0.30f, 0.72f, 0.42f)),
            MakeItem("Item_Shield", PrimitiveType.Cube, new Vector3(0.52f, 0.52f, 0.10f), new Color(0.34f, 0.52f, 0.78f)),
            MakeItem("Item_Scan", PrimitiveType.Sphere, new Vector3(0.38f, 0.38f, 0.38f), new Color(0.42f, 0.78f, 0.80f)),
            MakeItem("Item_Tape", PrimitiveType.Cylinder, new Vector3(0.30f, 0.12f, 0.30f), new Color(0.86f, 0.34f, 0.30f)),
            MakeItem("Item_DeckPiece", PrimitiveType.Cube, new Vector3(0.58f, 0.08f, 0.24f), new Color(0.44f, 0.34f, 0.26f)),
            MakeItem("Item_Letter", PrimitiveType.Cube, new Vector3(0.38f, 0.26f, 0.03f), new Color(0.94f, 0.92f, 0.78f))
        };
    }

    public static List<GameObject> CreateProps()
    {
        return new List<GameObject>
        {
            MakeBox("Prop_Guardrail", new Vector3(0.12f, 0.9f, 8f), new Color(0.35f, 0.28f, 0.22f), false),
            MakeBox("Prop_DeadTree", new Vector3(0.4f, 5f, 0.4f), new Color(0.28f, 0.22f, 0.16f), false),
            MakeBox("Prop_StreetLamp_Dead", new Vector3(0.15f, 4f, 0.15f), new Color(0.22f, 0.22f, 0.22f), false),
            MakeBox("Prop_BoatWreck", new Vector3(3.2f, 1.1f, 1.2f), new Color(0.30f, 0.28f, 0.26f), false),
            MakeBox("Prop_HouseRuin", new Vector3(4f, 3.2f, 3f), new Color(0.38f, 0.32f, 0.28f), false)
        };
    }

    // One repeat of the asphalt and concrete textures covers this many metres.
    private const float SurfaceTileMeters = 2.5f;

    private static GameObject MakeTrack(string name, Color color, System.Action<GameObject> dress = null)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(Root, false);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(root.transform, false);
        road.transform.localPosition = new Vector3(0f, 0f, 15f);
        road.transform.localScale = new Vector3(10f, 0.3f, 30f);
        Surface(road, "Road_Asphalt", "Road_Asphalt_Normal", color, 10f, 30f);

        dress?.Invoke(root);

        TrackSegment segment = root.AddComponent<TrackSegment>();
        segment.EnsurePlayable();
        root.SetActive(false);
        return root;
    }

    private static void AddArcadeDressing(GameObject root)
    {
        AddWall(root, -6.2f, new Color(0.42f, 0.36f, 0.44f), "Shopfront");
        AddWall(root, 6.2f, new Color(0.40f, 0.34f, 0.42f), "Shopfront");
        AddEmissiveSign(root, new Vector3(-4.5f, 2.2f, 18f), new Color(1f, 0.82f, 0.52f));
    }

    private static void AddOverpassDressing(GameObject root)
    {
        AddWall(root, -6.2f, new Color(0.34f, 0.36f, 0.40f), "Concrete");
        AddWall(root, 6.2f, new Color(0.34f, 0.36f, 0.40f), "Concrete");
        AddBox(root, "Guardrail", new Vector3(5.4f, 0.6f, 15f), new Vector3(0.12f, 0.9f, 28f), new Color(0.35f, 0.28f, 0.22f));
    }

    private static void AddFloodedDressing(GameObject root)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Water";
        water.transform.SetParent(root.transform, false);
        water.transform.localPosition = new Vector3(0f, -0.05f, 15f);
        water.transform.localScale = new Vector3(14f, 0.08f, 30f);
        Object.Destroy(water.GetComponent<Collider>());
        Surface(water, "Road_Asphalt", "Road_Asphalt_Normal", new Color(0.22f, 0.46f, 0.42f, 0.65f), 14f, 30f);
    }

    private static void AddDepotDressing(GameObject root)
    {
        AddEmissiveSign(root, new Vector3(0f, 3.2f, 24f), new Color(0.98f, 0.42f, 0.18f));
        AddBox(root, "Scanner", new Vector3(0f, 2.4f, 8f), new Vector3(2.4f, 0.2f, 0.2f), new Color(0.92f, 0.18f, 0.14f));
    }

    private static void AddWall(GameObject root, float x, Color color, string token)
    {
        AddBox(root, token, new Vector3(x, 1.8f, 15f), new Vector3(0.35f, 3.6f, 28f), color);
    }

    private static void AddBox(GameObject root, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root.transform, false);
        box.transform.localPosition = pos;
        box.transform.localScale = scale;
        Object.Destroy(box.GetComponent<Collider>());
        Surface(box, "Wall_Concrete", "Wall_Concrete_Normal", color, scale.x, scale.z);
    }

    private static void AddEmissiveSign(GameObject root, Vector3 pos, Color glow)
    {
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sign.name = "EmissiveSign";
        sign.transform.SetParent(root.transform, false);
        sign.transform.localPosition = pos;
        sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        sign.transform.localScale = new Vector3(2.4f, 1.1f, 1f);
        Object.Destroy(sign.GetComponent<Collider>());

        Material mat = ArtLibrary.Surface(null, null, glow, Vector2.one, 0.1f);
        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glow * 1.6f);
        }

        sign.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GameObject MakeCorner(string name, Color color, int direction)
    {
        const float arm = 15f;
        const float halfWidth = 5f;

        GameObject root = new GameObject(name);
        root.transform.SetParent(Root, false);

        GameObject entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        entry.name = "Road_Entry";
        entry.transform.SetParent(root.transform, false);
        entry.transform.localPosition = new Vector3(0f, 0f, arm * 0.5f);
        entry.transform.localScale = new Vector3(halfWidth * 2f, 0.3f, arm);
        Surface(entry, "Road_Asphalt", "Road_Asphalt_Normal", color, halfWidth * 2f, arm);

        // Reaches from the far kerb across to the exit so the bend has no gap.
        GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "Road_Exit";
        exit.transform.SetParent(root.transform, false);
        exit.transform.localPosition = new Vector3(direction * (arm - halfWidth) * 0.5f, 0f, arm);
        exit.transform.localScale = new Vector3(arm + halfWidth, 0.3f, halfWidth * 2f);
        Surface(exit, "Road_Asphalt", "Road_Asphalt_Normal", color, arm + halfWidth, halfWidth * 2f);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Corner_OuterWall";
        wall.transform.SetParent(root.transform, false);
        wall.transform.localPosition = new Vector3(0f, 1.2f, arm + halfWidth + 0.7f);
        wall.transform.localScale = new Vector3(halfWidth * 2.4f, 2.4f, 1.2f);
        Surface(wall, "Wall_Concrete", "Wall_Concrete_Normal", new Color(0.55f, 0.52f, 0.48f), halfWidth * 2.4f, 2.4f);
        Collider wallCol = wall.GetComponent<Collider>();
        if (wallCol != null)
            Object.Destroy(wallCol);

        TrackSegment segment = root.AddComponent<TrackSegment>();
        segment.SetKind(direction > 0 ? SegmentKind.CornerRight : SegmentKind.CornerLeft);
        segment.EnsurePlayable();
        root.SetActive(false);
        return root;
    }

    private static GameObject MakeItem(string name, PrimitiveType shape, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.SetParent(Root, false);
        go.transform.localScale = scale;
        go.transform.localPosition = new Vector3(0f, scale.y * 0.5f + 0.25f, 0f);
        SetColor(go, color);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Pickup pickup = go.GetComponent<Pickup>();
        if (pickup == null)
            pickup = go.AddComponent<Pickup>();
        pickup.Kind = Pickup.KindFromName(name);

        go.SetActive(false);
        return go;
    }

    private static GameObject MakeBox(string name, Vector3 scale, Color color, bool pickup)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(Root, false);
        go.transform.localScale = scale;
        go.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);
        SetColor(go, color);

        if (pickup)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
            if (go.GetComponent<Pickup>() == null)
                go.AddComponent<Pickup>();
        }

        go.SetActive(false);
        return go;
    }

    /// Textured variant of SetColor. Tiling is derived from the face size in
    /// metres so a 15 m corner arm and a 30 m straight look like the same road.
    private static void Surface(GameObject go, string colorTex, string normalTex, Color tint, float width, float length)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Vector2 tiling = new Vector2(width / SurfaceTileMeters, length / SurfaceTileMeters);
        Material material = ArtLibrary.Surface(colorTex, normalTex, tint, tiling);
        if (material != null)
            renderer.sharedMaterial = material;
        else
            SetColor(go, tint);
    }

    private static void SetColor(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (renderer.material.HasProperty("_Color"))
            renderer.material.color = color;
        if (renderer.material.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", color);
    }
}
