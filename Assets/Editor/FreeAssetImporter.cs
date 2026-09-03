#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// Downloads Kenney CC0 packs and maps them to Resources/ paths in AssetRequest.md.
public static class FreeAssetImporter
{
    private const string TempRoot = "Temp/free-assets";
    private const string KenneyRoadsUrl = "https://opengameart.org/sites/default/files/kenney_city-kit-roads.zip";
    private const string KenneyCharsUrl = "https://opengameart.org/sites/default/files/kenney_animated-characters-3.zip";

    [MenuItem("Tools/Archive A-0347/Import Free CC0 Assets")]
    public static void ImportAll()
    {
        EnsureDownloaded();
        CopyMappedFiles();
        ConfigureImporters();
        BuildRetrievalPrefabs();
        BuildDohaPrefab();
        BuildItemPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("347: Free CC0 assets imported. See Assets/_Guide/FreeAssetMap.json for mapping.");
    }

    private static void EnsureDownloaded()
    {
        string root = Path.Combine(Directory.GetCurrentDirectory(), TempRoot);
        Directory.CreateDirectory(root);

        DownloadIfMissing(KenneyRoadsUrl, Path.Combine(root, "kenney_city-kit-roads.zip"), "city-roads");
        DownloadIfMissing(KenneyCharsUrl, Path.Combine(root, "kenney_animated-characters-3.zip"), "characters");
    }

    private static void DownloadIfMissing(string url, string zipPath, string extractFolder)
    {
        if (!File.Exists(zipPath))
        {
            Debug.Log("347: Downloading " + Path.GetFileName(zipPath) + " …");
            using (var client = new WebClient())
                client.DownloadFile(url, zipPath);
        }

        string dest = Path.Combine(Path.GetDirectoryName(zipPath) ?? TempRoot, extractFolder);
        if (!Directory.Exists(dest) || Directory.GetFiles(dest, "*", SearchOption.AllDirectories).Length == 0)
            ZipFile.ExtractToDirectory(zipPath, dest);
    }

    private static void CopyMappedFiles()
    {
        string root = Path.Combine(Directory.GetCurrentDirectory(), TempRoot);
        string roads = FindSubfolder(root, "city-roads", "Models", "FBX format");
        string chars = FindSubfolder(root, "characters", "Model");

        CopyPair(roads, "road-straight.fbx", "Assets/Resources/Props/Kenney/Track_Arcade.fbx");
        CopyPair(roads, "road-bridge.fbx", "Assets/Resources/Props/Kenney/Track_Overpass.fbx");
        CopyPair(roads, "road-slant-flat.fbx", "Assets/Resources/Props/Kenney/Track_Flooded.fbx");
        CopyPair(roads, "road-end-barrier.fbx", "Assets/Resources/Props/Kenney/Track_Depot.fbx");
        CopyPair(roads, "road-bend-square.fbx", "Assets/Resources/Props/Kenney/Track_CornerR.fbx");
        CopyPair(roads, "road-bend-square-barrier.fbx", "Assets/Resources/Props/Kenney/Track_CornerL.fbx");

        CopyPair(roads, "sign-highway.fbx", "Assets/Resources/Props/Prop_Shopfront.fbx");
        CopyPair(roads, "light-square-cross.fbx", "Assets/Resources/Props/Prop_TrafficLight.fbx");
        CopyPair(roads, "construction-light.fbx", "Assets/Resources/Props/Prop_StreetLamp_Dead.fbx");
        CopyPair(roads, "road-side-barrier.fbx", "Assets/Resources/Props/Prop_Guardrail.fbx");

        CopyPair(roads, "construction-barrier.fbx", "Assets/Resources/Hazards/Barrier_Low.fbx");
        CopyPair(roads, "construction-cone.fbx", "Assets/Resources/Hazards/Debris.fbx");

        CopyPair(chars, "characterMedium.fbx", "Assets/Resources/Character/Doha/characterMedium.fbx");
        CopyPair(FindSubfolder(root, "characters", "Animations"), "idle.fbx", "Assets/Resources/Character/Doha/Animations/idle.fbx");
        CopyPair(FindSubfolder(root, "characters", "Animations"), "run.fbx", "Assets/Resources/Character/Doha/Animations/run.fbx");
        CopyPair(FindSubfolder(root, "characters", "Animations"), "jump.fbx", "Assets/Resources/Character/Doha/Animations/jump.fbx");
        CopyPair(FindSubfolder(root, "characters", "Skins"), "humanFemaleA.png", "Assets/Resources/Character/Doha/humanFemaleA.png");
    }

    private static string FindSubfolder(string root, params string[] parts)
    {
        string path = root;
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return string.Empty;

            string[] dirs = Directory.GetDirectories(path, part, SearchOption.TopDirectoryOnly);
            path = dirs.Length > 0 ? dirs[0] : Path.Combine(path, part);
        }

        return Directory.Exists(path) ? path : string.Empty;
    }

    private static void CopyPair(string sourceDir, string fileName, string destAssetPath)
    {
        if (string.IsNullOrEmpty(sourceDir))
            return;

        string src = Path.Combine(sourceDir, fileName);
        if (!File.Exists(src))
        {
            Debug.LogWarning("347: Missing source " + fileName);
            return;
        }

        string dest = Path.Combine(Directory.GetCurrentDirectory(), destAssetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? "Assets");
        File.Copy(src, dest, true);
    }

    private static void ConfigureImporters()
    {
        ConfigureProp("Assets/Resources/Props/Kenney/Track_Arcade.fbx", 12f);
        ConfigureProp("Assets/Resources/Props/Kenney/Track_Overpass.fbx", 10f);
        ConfigureProp("Assets/Resources/Props/Kenney/Track_Flooded.fbx", 10f);
        ConfigureProp("Assets/Resources/Props/Kenney/Track_Depot.fbx", 10f);
        ConfigureProp("Assets/Resources/Props/Kenney/Track_CornerR.fbx", 8f);
        ConfigureProp("Assets/Resources/Props/Kenney/Track_CornerL.fbx", 8f);

        ConfigureProp("Assets/Resources/Props/Prop_Shopfront.fbx", 2.5f);
        ConfigureProp("Assets/Resources/Props/Prop_TrafficLight.fbx", 2f);
        ConfigureProp("Assets/Resources/Props/Prop_StreetLamp_Dead.fbx", 2f);
        ConfigureProp("Assets/Resources/Props/Prop_Guardrail.fbx", 2f);

        ConfigureCharacter("Assets/Resources/Character/Doha/characterMedium.fbx", 1.8f);
        ConfigureAnimation("Assets/Resources/Character/Doha/Animations/idle.fbx");
        ConfigureAnimation("Assets/Resources/Character/Doha/Animations/run.fbx");
        ConfigureAnimation("Assets/Resources/Character/Doha/Animations/jump.fbx");
    }

    private static void ConfigureTrack(string path, float scale)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        importer.globalScale = scale;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.animationType = ModelImporterAnimationType.None;
        importer.SaveAndReimport();
    }

    private static void ConfigureProp(string path, float scale)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        importer.globalScale = scale;
        importer.animationType = ModelImporterAnimationType.None;
        importer.SaveAndReimport();
    }

    private static void ConfigureCharacter(string path, float scale)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        importer.globalScale = scale;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    private static void ConfigureAnimation(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.importAnimation = true;
        importer.SaveAndReimport();
    }

    private static void BuildRetrievalPrefabs()
    {
        Directory.CreateDirectory("Assets/Resources/Retrieval");
        SavePrefab(BuildDroneMesh(), "Assets/Resources/Retrieval/Drone_Retrieval.prefab");
        SavePrefab(BuildCollectorMesh(), "Assets/Resources/Retrieval/King_Collector.prefab");
    }

    private static GameObject BuildDroneMesh()
    {
        GameObject root = new GameObject("Drone_Retrieval");
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.55f, 0.32f, 0.55f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        Paint(body, new Color(0.90f, 0.90f, 0.88f));

        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lens.name = "ScannerLens";
        lens.transform.SetParent(root.transform, false);
        lens.transform.localPosition = new Vector3(0f, -0.12f, 0.36f);
        lens.transform.localScale = Vector3.one * 0.16f;
        Object.DestroyImmediate(lens.GetComponent<Collider>());
        Paint(lens, new Color(0.92f, 0.18f, 0.14f));

        return root;
    }

    private static GameObject BuildCollectorMesh()
    {
        GameObject root = new GameObject("King_Collector");
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(root.transform, false);
        torso.transform.localScale = new Vector3(1.1f, 1.4f, 1.1f);
        Object.DestroyImmediate(torso.GetComponent<Collider>());
        Paint(torso, new Color(0.90f, 0.90f, 0.88f));

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(root.transform, false);
        arm.transform.localPosition = new Vector3(0f, 0.4f, 0.6f);
        arm.transform.localScale = new Vector3(0.35f, 0.35f, 1.6f);
        Object.DestroyImmediate(arm.GetComponent<Collider>());
        Paint(arm, new Color(0.82f, 0.82f, 0.80f));

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "ScannerHead";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.5f, 0.1f);
        head.transform.localScale = Vector3.one * 0.45f;
        Object.DestroyImmediate(head.GetComponent<Collider>());
        Paint(head, new Color(0.88f, 0.88f, 0.86f));

        return root;
    }

    private static void BuildDohaPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Character/Doha/characterMedium.fbx");
        if (model == null)
            return;

        string prefabPath = "Assets/Resources/Character/Doha/DohaModel.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "DohaModel";
        instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
            animator = instance.AddComponent<Animator>();

        RuntimeAnimatorController controller = BuildRunnerController();
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        SavePrefab(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static RuntimeAnimatorController BuildRunnerController()
    {
        string path = "Assets/Resources/Character/Doha/DohaRunner.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null)
            return controller;

        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine root = controller.layers[0].stateMachine;

        AnimationClip idle = LoadClip("Assets/Resources/Character/Doha/Animations/idle.fbx");
        AnimationClip run = LoadClip("Assets/Resources/Character/Doha/Animations/run.fbx");
        AnimationClip jump = LoadClip("Assets/Resources/Character/Doha/Animations/jump.fbx");

        AnimatorState idleState = root.AddState("Idle", new Vector3(250, 0, 0));
        AnimatorState runState = root.AddState("Run", new Vector3(450, 0, 0));
        AnimatorState jumpState = root.AddState("Jump", new Vector3(450, 80, 0));

        if (idle != null)
            idleState.motion = idle;
        if (run != null)
            runState.motion = run;
        if (jump != null)
            jumpState.motion = jump;

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Slide", AnimatorControllerParameterType.Bool);

        root.defaultState = runState != null ? runState : idleState;

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview"))
                return clip;
        }

        return null;
    }

    private static void BuildItemPrefabs()
    {
        Directory.CreateDirectory("Assets/Resources/Items");
        List<GameObject> items = TestCatalog.CreateSupplies();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            string path = "Assets/Resources/Items/" + items[i].name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                continue;

            GameObject clone = Object.Instantiate(items[i]);
            clone.name = items[i].name;
            SavePrefab(clone, path);
            Object.DestroyImmediate(clone);
        }
    }

    private static void SavePrefab(GameObject go, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
        PrefabUtility.SaveAsPrefabAsset(go, path);
    }

    private static void Paint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.color = color;
        renderer.sharedMaterial = mat;
    }
}
#endif
