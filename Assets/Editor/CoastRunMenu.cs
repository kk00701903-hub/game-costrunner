using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CoastRun;

/// Coast Run editor entry.
///
/// The previous version pointed at MainMenu.unity and Run.unity, both removed when the
/// project moved to the numbered five-scene flow. Worse, "Open Main Menu" recreated the
/// deleted scene and inserted it at build index 0, displacing 00_Boot as the entry
/// point. Everything now goes through CoastScenes.
public static class CoastRunMenu
{
    // ── Opening scenes ──────────────────────────────────────────────────

    [MenuItem("Tools/Coast Run/Open/00 Boot", false, 10)]
    public static void OpenBootScene() => Open(CoastScenes.Boot);

    [MenuItem("Tools/Coast Run/Open/01 Title", false, 11)]
    public static void OpenMainMenuScene() => Open(CoastScenes.Title);

    [MenuItem("Tools/Coast Run/Open/02 Run", false, 12)]
    public static void OpenRunScene() => Open(CoastScenes.Run);

    [MenuItem("Tools/Coast Run/Open/04 Ending", false, 13)]
    public static void OpenEndingScene() => Open(CoastScenes.Ending);

    private static void Open(string sceneName)
    {
        string path = CoastScenes.Path(sceneName);
        if (!File.Exists(path))
        {
            Debug.LogError($"Coast Run: {path} is missing. The five flow scenes are " +
                           "tracked in the repo — restore it rather than regenerating.");
            return;
        }

        EditorSceneManager.OpenScene(path);
        Debug.Log($"Coast Run: {sceneName} open.");
    }

    // ── Playing ─────────────────────────────────────────────────────────
    //
    // Play entry points live in CoastRun.Editor.CoastRunPlayMenu:
    //   Coast Run/Play From Boot (recommended)   Ctrl+Shift+B — full flow
    //   Coast Run/▶ PLAY 주행만                   Ctrl+Shift+C — dev fast path
    //
    // Editor Play runs whatever scene is open, not build index 0. That is why
    // pressing Play with 02_Run open drops you straight into gameplay with no
    // title screen — use Play From Boot to see what a player sees.

    [MenuItem("Tools/Coast Run/Portrait Game View (720x1280)", false, 20)]
    public static void PortraitGameView()
    {
        GameViewPortrait.Set(720, 1280);
        Debug.Log("Coast Run: Game View → 720×1280 portrait (reference video aspect).");
    }

    // ── Build settings ──────────────────────────────────────────────────

    [MenuItem("Tools/Coast Run/Rebuild Scene List", false, 30)]
    public static void RebuildSceneList()
    {
        var list = new List<EditorBuildSettingsScene>();
        var missing = new List<string>();

        foreach (string sceneName in CoastScenes.BuildOrder)
        {
            string path = CoastScenes.Path(sceneName);
            if (File.Exists(path))
                list.Add(new EditorBuildSettingsScene(path, true));
            else
                missing.Add(path);
        }

        EditorBuildSettings.scenes = list.ToArray();

        if (missing.Count > 0)
            Debug.LogError("Coast Run: missing scenes —\n  " + string.Join("\n  ", missing));
        else
            Debug.Log($"Coast Run: build scene list rebuilt — {list.Count} scenes, 00_Boot at index 0.");
    }

    [MenuItem("Tools/Coast Run/Verify Scene List", false, 31)]
    public static void VerifySceneList()
    {
        var problems = new List<string>();
        var scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0 || !scenes[0].path.EndsWith($"{CoastScenes.Boot}.unity"))
            problems.Add($"Build index 0 must be {CoastScenes.Boot} — it is the entry point.");

        foreach (string sceneName in CoastScenes.BuildOrder)
        {
            string path = CoastScenes.Path(sceneName);
            if (!File.Exists(path))
                problems.Add($"Scene file missing: {path}");
            else if (System.Array.FindIndex(scenes, s => s.path == path) < 0)
                problems.Add($"Not in build settings: {path}");
        }

        foreach (var s in scenes)
            if (!File.Exists(s.path))
                problems.Add($"Build settings points at a deleted scene: {s.path}");

        Debug.Log(problems.Count == 0
            ? "Coast Run: scene list OK — 00_Boot at index 0, all five present."
            : "Coast Run: scene list problems:\n  " + string.Join("\n  ", problems));
    }

    // ── Misc ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Coast Run/Integration Test", false, 40)]
    public static void RunIntegrationTest()
    {
        CoastIntegrationTest.RunAuditMenu();
    }

    [MenuItem("Tools/Coast Run/Sync Reference Scene Frames", false, 41)]
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
}
