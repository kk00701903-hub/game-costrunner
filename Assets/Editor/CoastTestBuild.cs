using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds a Windows x64 standalone for playtesting (no installer wizard — unzip & run).
public static class CoastTestBuild
{
    private const string DistRel = "Dist/CoastRun_Test";
    private const string ExeName = "CoastRun.exe";

    [MenuItem("Tools/Coast Run/Build Test Windows (x64)")]
    public static void BuildMenu()
    {
        var r = BuildWindows();
        Debug.Log(r);
    }

    public static void BuildBatch()
    {
        string report = BuildWindows();
        Debug.Log(report);
        bool ok = report.Contains("RESULT=OK");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static string BuildWindows()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outDir = Path.Combine(projectRoot, DistRel);
        string exePath = Path.Combine(outDir, ExeName);

        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        // Ensure build scenes are present.
        EnsureBuildScenes();
        CoastRun.Editor.CoastArtImportMenu.EnsureTransmissionTowerPrefab();

        PlayerSettings.productName = "Coast Run";
        PlayerSettings.companyName = "CoastRun";
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 720;
        PlayerSettings.defaultScreenHeight = 1280;
        PlayerSettings.resizableWindow = true;

        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>();
        foreach (var s in scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                enabled.Add(s.path);
        }

        if (enabled.Count == 0)
            return "RESULT=FAIL\nNo enabled scenes in Build Settings.";

        var options = new BuildPlayerOptions
        {
            scenes = enabled.ToArray(),
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        string logPath = Path.Combine(outDir, "build_report.txt");
        string text =
            "=== Coast Run Test Build ===\n" +
            System.DateTime.Now.ToString("u") + "\n" +
            "target=StandaloneWindows64\n" +
            "exe=" + exePath + "\n" +
            "scenes=" + string.Join(", ", enabled) + "\n" +
            "result=" + summary.result + "\n" +
            "sizeBytes=" + summary.totalSize + "\n" +
            "time=" + summary.totalTime + "\n" +
            (summary.result == BuildResult.Succeeded ? "RESULT=OK\n" : "RESULT=FAIL\n");

        File.WriteAllText(logPath, text);

        // Short README for testers.
        File.WriteAllText(Path.Combine(outDir, "README_TEST.txt"),
            "Coast Run — Test Build\n" +
            "======================\n" +
            "1. Unzip this folder anywhere.\n" +
            "2. Run CoastRun.exe\n" +
            "3. Portrait 720x1280 windowed (resize OK).\n" +
            "4. Main Menu → Start → skate run.\n" +
            "\n" +
            "Controls: swipe / A-D lane, Space jump, S crouch.\n" +
            "Dev build: console logs enabled.\n");

        return text;
    }

    private static void EnsureBuildScenes()
    {
        const string menu = "Assets/_CoastRun/Scenes/MainMenu.unity";
        const string run = "Assets/_CoastRun/Scenes/Run.unity";

        if (!File.Exists(menu))
            CoastRunMenu.CreateMainMenuScene();
        if (!File.Exists(run))
            CoastRunMenu.CreateRunScene();

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(menu, true),
            new EditorBuildSettingsScene(run, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
    }
}
