using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoastRun;

/// Coast Run editor entry — reference-video replication mode.
public static class CoastRunMenu
{
    [MenuItem("Tools/Coast Run/Integration Test")]
    public static void RunIntegrationTest()
    {
        CoastIntegrationTest.RunAuditMenu();
    }

    [MenuItem("Tools/Coast Run/Sync Reference Scene Frames")]
    public static void SyncReferenceSceneFrames()
    {
        const string refDir = "Assets/_Guide/Reference";
        const string resDir = "Assets/Resources/CoastRun/Scene";
        const string artDir = "Assets/_CoastRun/Art/Scene";
        Directory.CreateDirectory(resDir);
        Directory.CreateDirectory(artDir);

        for (int i = 1; i <= 5; i++)
        {
            string src = $"{refDir}/style_frame_{i}.jpg";
            if (!File.Exists(src))
                continue;
            File.Copy(src, $"{resDir}/Scene_Frame_{i}.jpg", true);
            File.Copy(src, $"{artDir}/Scene_Frame_{i}.jpg", true);
        }

        AssetDatabase.Refresh();
        Debug.Log("Coast Run: synced style_frame_1~5 → Resources/CoastRun/Scene/");
    }

    public static void CreateMainMenuSceneBatch()
    {
        CreateMainMenuScene();
        EditorApplication.Exit(0);
    }

    [MenuItem("Tools/Coast Run/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MainMenuBootstrap.ScenePath) ?? "Assets/_CoastRun/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrap = new GameObject("MainMenuBootstrap");
        bootstrap.AddComponent<MainMenuBootstrap>();

        EditorSceneManager.SaveScene(scene, MainMenuBootstrap.ScenePath);
        EnsureSceneInBuildSettings(MainMenuBootstrap.ScenePath, 0);
        EnsureSceneInBuildSettings(CoastRunBootstrap.ScenePath, 1);

        AssetDatabase.Refresh();
        Debug.Log("Coast Run: created " + MainMenuBootstrap.ScenePath + " (build index 0)");
    }

    [MenuItem("Tools/Coast Run/Open Main Menu")]
    public static void OpenMainMenuScene()
    {
        if (!File.Exists(MainMenuBootstrap.ScenePath))
            CreateMainMenuScene();
        else
            EditorSceneManager.OpenScene(MainMenuBootstrap.ScenePath);

        Debug.Log("Coast Run: Main Menu open.");
    }

    private static void EnsureSceneInBuildSettings(string scenePath, int insertIndex)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].path == scenePath)
                list.RemoveAt(i);
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, list.Count);
        list.Insert(insertIndex, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    [MenuItem("Tools/Coast Run/Create Run Scene")]
    public static void CreateRunScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CoastRunBootstrap.ScenePath) ?? "Assets/_CoastRun/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrap = new GameObject("CoastRunBootstrap");
        bootstrap.AddComponent<CoastRunBootstrap>();

        EditorSceneManager.SaveScene(scene, CoastRunBootstrap.ScenePath);
        EnsureSceneInBuildSettings(MainMenuBootstrap.ScenePath, 0);
        EnsureSceneInBuildSettings(CoastRunBootstrap.ScenePath, 1);

        AssetDatabase.Refresh();
        Debug.Log("Coast Run: created " + CoastRunBootstrap.ScenePath + "\n→ Tools > Coast Run > Play (Ctrl+Shift+C)");
    }

    [MenuItem("Tools/Coast Run/Open Run Scene")]
    public static void OpenRunScene()
    {
        if (!File.Exists(CoastRunBootstrap.ScenePath))
            CreateRunScene();
        else
            EditorSceneManager.OpenScene(CoastRunBootstrap.ScenePath);

        Debug.Log("Coast Run: scene open. Press Play or Ctrl+Shift+C.");
    }

    [MenuItem("Tools/Coast Run/Portrait Game View (720x1280)")]
    public static void PortraitGameView()
    {
        GameViewPortrait.Set(720, 1280);
        Debug.Log("Coast Run: Game View → 720×1280 portrait (reference video aspect).");
    }

    [MenuItem("Coast Run/▶ PLAY 해안 주행 %#c", false, 1)]
    [MenuItem("Tools/Coast Run/▶ PLAY 해안 주행 %#c", false, 1)]
    public static void PlayCoastRun()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += PlayCoastRun;
            return;
        }

        if (!File.Exists(CoastRunBootstrap.ScenePath))
            CreateRunScene();

        PlayerPrefs.SetInt("CoastRun_SkipPrologue", 1);
        PlayerPrefs.Save();

        if (EditorSceneManager.GetActiveScene().path != CoastRunBootstrap.ScenePath)
            OpenRunScene();

        EditorApplication.delayCall += () =>
        {
            try { PortraitGameView(); }
            catch (System.Exception ex) { Debug.LogWarning("Coast Run: portrait skipped — " + ex.Message); }
            EditorApplication.isPlaying = true;
        };
    }
}
