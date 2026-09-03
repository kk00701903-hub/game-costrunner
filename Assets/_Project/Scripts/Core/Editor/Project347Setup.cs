#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// Creates the ScriptableObject defaults and the three scenes the brief requires.
public static class Project347Setup
{
    private const string DataRoot = "Assets/_Project/Data";
    private const string ResourcesRoot = "Assets/Resources/347";
    private const string ScenesRoot = "Assets/_Project/Scenes";

    [MenuItem("Tools/Archive A-0347/Setup Default Data")]
    public static void SetupDefaultData()
    {
        EnsureFolders();

        GameConfig config = LoadOrCreate<GameConfig>(ResourcesRoot + "/GameConfig.asset");
        CameraProfile camera = LoadOrCreate<CameraProfile>(ResourcesRoot + "/CameraProfile.asset");
        KingPhaseData p1 = LoadOrCreate<KingPhaseData>(DataRoot + "/Combat/KingPhase_P1.asset");
        KingPhaseData p2 = LoadOrCreate<KingPhaseData>(DataRoot + "/Combat/KingPhase_P2.asset");
        KingPhaseData p3 = LoadOrCreate<KingPhaseData>(DataRoot + "/Combat/KingPhase_P3.asset");

        p1.hp = 3;
        p1.cycleDuration = 4.0f;
        p1.aimTime = 0.6f;
        p1.throwTime = 0.5f;
        p1.counterWindow = 1.4f;
        p1.recoverTime = 1.0f;
        p1.counterLanesPerCycle = 1;
        p1.hasFakeCounterLane = false;
        p1.missPenaltySpeedMul = 1.30f;

        p2.hp = 3;
        p2.cycleDuration = 3.4f;
        p2.aimTime = 0.55f;
        p2.throwTime = 0.45f;
        p2.counterWindow = 1.2f;
        p2.recoverTime = 0.8f;
        p2.counterLanesPerCycle = 1;
        p2.missPenaltySpeedMul = 1.30f;

        p3.hp = 3;
        p3.cycleDuration = 2.8f;
        p3.aimTime = 0.5f;
        p3.throwTime = 0.4f;
        p3.counterWindow = 1.0f;
        p3.recoverTime = 0.6f;
        p3.counterLanesPerCycle = 2;
        p3.hasFakeCounterLane = true;
        p3.missPenaltySpeedMul = 1.30f;

        config.cameraProfile = camera;
        config.kingPhases = new[] { p1, p2, p3 };
        config.minTelegraphSec = 0.45f;
        config.bossFixedSpeed = 14f;
        config.laneChangeSeconds = 0.18f;
        config.maxHp = 3;

        EditorUtility.SetDirty(p1);
        EditorUtility.SetDirty(p2);
        EditorUtility.SetDirty(p3);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameConfig.Override(config);
        CameraProfile.Override(camera);
        Debug.Log("347: default GameConfig / CameraProfile / KingPhase data ready under Resources/347.");
    }

    [MenuItem("Tools/Archive A-0347/Fix Render Pipeline (URP)")]
    public static void SetupRenderPipeline()
    {
        EnsureFolders();
        const string path = "Assets/_Project/Settings/UniversalRP.asset";
        Directory.CreateDirectory("Assets/_Project/Settings");

        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset == null)
        {
            asset = UniversalRenderPipelineAsset.Create();
            AssetDatabase.CreateAsset(asset, path);
        }

        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline = asset;
        AssetDatabase.SaveAssets();
        Debug.Log("347: URP assigned. Re-enter Play if the scene was pink or empty.");
    }

    [MenuItem("Tools/Archive A-0347/Setup Visual Pipeline (URP + Materials + Volume)")]
    public static void SetupVisualPipeline()
    {
        SetupRenderPipeline();
        SetupMaterialLibrary();
        SetupRunVolumeProfile();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("347: Visual pipeline ready — URP, MaterialLibrary, RunVolumeProfile.");
    }

    [MenuItem("Tools/Archive A-0347/Quality/Low")]
    public static void QualityLow() => ApplyQualityMenu(VisualTier.Low);

    [MenuItem("Tools/Archive A-0347/Quality/Galaxy S26 (default)")]
    public static void QualityS26() => ApplyQualityMenu(VisualTier.S26);

    [MenuItem("Tools/Archive A-0347/Quality/High")]
    public static void QualityHigh() => ApplyQualityMenu(VisualTier.High);

    private static void ApplyQualityMenu(VisualTier tier)
    {
        if (!Application.isPlaying)
        {
            PlayerPrefs.SetInt("r347_visual_tier", (int)tier);
            PlayerPrefs.Save();
            Debug.Log("347: Quality tier " + tier + " will apply on next Play.");
            return;
        }

        VisualQuality.Apply(tier);
    }

    [InitializeOnLoadMethod]
    private static void AutoWireRenderPipelineOnLoad()
    {
        if (GraphicsSettings.defaultRenderPipeline != null)
            return;

        UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
            "Assets/_Project/Settings/UniversalRP.asset");
        if (asset == null)
            return;

        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline = asset;
    }

    private static void SetupMaterialLibrary()
    {
        Directory.CreateDirectory("Assets/_Project/Data/Art");
        Directory.CreateDirectory("Assets/_Project/Data/Art/Materials");
        Directory.CreateDirectory("Assets/Resources/347");

        MaterialLibrary library = LoadOrCreate<MaterialLibrary>("Assets/_Project/Data/Art/MaterialLibrary.asset");
        library.asphalt = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_Asphalt.mat", "Road_Asphalt", "Road_Asphalt_Normal", new Color(0.32f, 0.32f, 0.34f));
        library.concrete = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_Concrete.mat", "Wall_Concrete", "Wall_Concrete_Normal", new Color(0.42f, 0.40f, 0.38f));
        library.metal = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_Metal.mat", null, null, new Color(0.48f, 0.46f, 0.44f), 0.55f);
        library.emissiveSign = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_EmissiveSign.mat", null, null, new Color(0.98f, 0.82f, 0.52f), 0.2f, emissive: true);
        library.water = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_Water.mat", null, null, new Color(0.22f, 0.46f, 0.42f, 0.75f), 0.85f);
        library.characterSkin = LoadOrCreateMaterial("Assets/_Project/Data/Art/Materials/M_CharacterSkin.mat", null, null, new Color(0.92f, 0.78f, 0.66f), 0.18f);

        EditorUtility.SetDirty(library);

        MaterialLibrary runtimeCopy = LoadOrCreate<MaterialLibrary>("Assets/Resources/347/MaterialLibrary.asset");
        runtimeCopy.asphalt = library.asphalt;
        runtimeCopy.concrete = library.concrete;
        runtimeCopy.metal = library.metal;
        runtimeCopy.emissiveSign = library.emissiveSign;
        runtimeCopy.water = library.water;
        runtimeCopy.characterSkin = library.characterSkin;
        EditorUtility.SetDirty(runtimeCopy);
    }

    private static Material LoadOrCreateMaterial(string path, string albedo, string normal, Color tint, float smoothness = 0.12f, bool emissive = false)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color"))
            mat.color = tint;

        if (!string.IsNullOrEmpty(albedo))
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/" + albedo + ".jpg");
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }
        }

        if (!string.IsNullOrEmpty(normal))
        {
            Texture2D bump = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/" + normal + ".jpg");
            if (bump != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", bump);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        if (emissive && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tint * 1.4f);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SetupRunVolumeProfile()
    {
        Directory.CreateDirectory("Assets/_Project/Settings");
        Directory.CreateDirectory("Assets/Resources/347");

        const string projectPath = "Assets/_Project/Settings/RunVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(projectPath);
        if (profile == null)
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.saturation.Override(8f);
        color.contrast.Override(6f);

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.05f);

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.45f);

        if (!profile.TryGet(out Tonemapping tonemap))
            tonemap = profile.Add<Tonemapping>(true);
        tonemap.active = true;
        tonemap.mode.Override(TonemappingMode.ACES);

        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(projectPath) == null)
            AssetDatabase.CreateAsset(profile, projectPath);
        else
            EditorUtility.SetDirty(profile);

        VolumeProfile runtime = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Resources/347/RunVolumeProfile.asset");
        if (runtime == null)
        {
            runtime = Object.Instantiate(profile);
            AssetDatabase.CreateAsset(runtime, "Assets/Resources/347/RunVolumeProfile.asset");
        }
    }

    [MenuItem("Tools/Archive A-0347/Create Boot Meta Run Scenes")]
    public static void CreateScenes()
    {
        EnsureFolders();
        SetupDefaultData();

        CreateEmptyScene(ScenesRoot + "/Boot.unity", "Boot");
        CreateEmptyScene(ScenesRoot + "/Meta.unity", "Meta");
        CreateEmptyScene(ScenesRoot + "/Run.unity", "Run");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesRoot + "/Boot.unity", true),
            new EditorBuildSettingsScene(ScenesRoot + "/Meta.unity", true),
            new EditorBuildSettingsScene(ScenesRoot + "/Run.unity", true)
        };

        Debug.Log("347: Boot / Meta / Run scenes created and added to Build Settings.");
    }

    private static void CreateEmptyScene(string path, string name)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = name;
        EditorSceneManager.SaveScene(scene, path);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(DataRoot + "/Combat");
        Directory.CreateDirectory(DataRoot + "/Core");
        Directory.CreateDirectory(DataRoot + "/Camera");
        Directory.CreateDirectory(ResourcesRoot);
        Directory.CreateDirectory(ScenesRoot);
    }
}
#endif
