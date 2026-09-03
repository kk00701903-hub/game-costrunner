using System.IO;
using UnityEditor;
using UnityEngine;
using CoastRun;

/// Creates default ScriptableObject configs and copies to Resources.
public static class CoastRunSetupMenu
{
    private const string ConfigDir = "Assets/_CoastRun/Config";
    private const string ResourcesDir = "Assets/Resources/CoastRun/Config";

    [MenuItem("Tools/Coast Run/Setup Default Config Assets")]
    public static void SetupConfigs()
    {
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(ResourcesDir);

        CreateOrUpdate<RunConfig>(ConfigDir + "/RunConfig.asset", ResourcesDir + "/RunConfig.asset");
        CreateOrUpdate<UpgradeConfig>(ConfigDir + "/UpgradeConfig.asset", ResourcesDir + "/UpgradeConfig.asset");
        CreateOrUpdate<StoryConfig>(ConfigDir + "/StoryConfig.asset", ResourcesDir + "/StoryConfig.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Coast Run: Config assets ready in " + ResourcesDir);
    }

    private static void CreateOrUpdate<T>(string editorPath, string resourcesPath) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(editorPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, editorPath);
        }

        if (!File.Exists(resourcesPath.Replace("Assets/", Application.dataPath + "/")))
            AssetDatabase.CopyAsset(editorPath, resourcesPath);
    }

    public static void FullSetupBatch()
    {
        FullSetup();
        EditorApplication.Exit(0);
    }

    [MenuItem("Tools/Coast Run/Full Setup (Config + Art Import)")]
    public static void FullSetup()
    {
        SetupConfigs();
        CoastRunMenu.SyncReferenceSceneFrames();
        CoastRun.Editor.CoastArtImportMenu.AutoImportArtToResources();
        CoastRun.Editor.CoastArtImportMenu.EnsureTransmissionTowerPrefab();
    }
}
