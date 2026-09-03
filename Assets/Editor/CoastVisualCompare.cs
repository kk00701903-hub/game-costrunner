using System.IO;
using UnityEditor;
using UnityEngine;
using CoastRun;

/// Captures Game View after Coast Run boots for reference-video comparison.
public static class CoastVisualCompare
{
    private const string OutDir = "Temp/visual_compare";
    private static double _playStartedAt;
    private static bool _armed;
    private static bool _captured;
    private static bool _skippedPrologue;

    [InitializeOnLoadMethod]
    private static void Hook()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static bool WantCapture()
    {
        foreach (string a in System.Environment.GetCommandLineArgs())
        {
            if (a == "-CoastPlay" || a == "-CoastCompare")
                return true;
        }

        return SessionState.GetBool("CoastCompare.Armed", false);
    }

    [MenuItem("Tools/Coast Run/Visual Compare Capture Once")]
    public static void ArmAndPlay()
    {
        SessionState.SetBool("CoastCompare.Armed", true);
        CoastRunMenu.PlayCoastRun();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && WantCapture())
        {
            _armed = true;
            _captured = false;
            _skippedPrologue = false;
            _playStartedAt = EditorApplication.timeSinceStartup;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", OutDir));
            Debug.Log("CoastVisualCompare: armed — will skip prologue and capture.");
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            _armed = false;
            SessionState.SetBool("CoastCompare.Armed", false);
        }
    }

    private static void Tick()
    {
        if (!_armed || _captured || !EditorApplication.isPlaying)
            return;

        double elapsed = EditorApplication.timeSinceStartup - _playStartedAt;

        // Skip text prologue quickly so gameplay framing is visible.
        if (!_skippedPrologue && elapsed > 0.8)
        {
            var story = Object.FindFirstObjectByType<StoryManager>();
            if (story != null && !story.PrologueComplete)
            {
                // Drive SKIP via public API if present; else simulate clicks.
                story.SendMessage("SkipPrologue", SendMessageOptions.DontRequireReceiver);
                story.SendMessage("FinishPrologue", SendMessageOptions.DontRequireReceiver);
            }

            _skippedPrologue = true;
        }

        if (elapsed < 3.5)
            return;

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir, "game_capture.png"));
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("CoastVisualCompare: wrote " + path);

        // Also dump a checklist log.
        string report = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir, "checklist.txt"));
        File.WriteAllText(report, BuildChecklist());
        Debug.Log("CoastVisualCompare: wrote " + report);

        _captured = true;
        _armed = false;
        SessionState.SetBool("CoastCompare.Armed", false);

        // Leave Play after short delay so PNG flushes.
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        };
    }

    private static string BuildChecklist()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        var cam = Camera.main;
        var visual = Object.FindFirstObjectByType<CoastPlayerVisual>();
        bool girlPrefab = PrefabLibrary.HasPrefab("GirlSkater");
        bool polePrefab = PrefabLibrary.HasPrefab("Pole_WireSet");
        bool skyTex = ArtAssets.LoadTexture("SummerSky_Portrait") != null;
        bool seaTex = ArtAssets.LoadTexture("Sea_Turquoise_Tile") != null;

        float girlScreenY = -1f;
        if (player != null && cam != null)
        {
            Vector3 sp = cam.WorldToViewportPoint(player.transform.position + Vector3.up * 0.9f);
            girlScreenY = sp.y;
        }

        return
            "Coast Run vs style_reference.mp4 checklist\n" +
            "girlPrefab=" + girlPrefab + "\n" +
            "polePrefab=" + polePrefab + "\n" +
            "skyTexture=" + skyTex + "\n" +
            "seaTexture=" + seaTex + "\n" +
            "girlViewportY=" + girlScreenY.ToString("F3") + " (want ~0.25-0.35)\n" +
            "camFov=" + (cam != null ? cam.fieldOfView.ToString("F1") : "n/a") + "\n" +
            "playerPresent=" + (player != null) + "\n" +
            "visualPresent=" + (visual != null) + "\n" +
            "layoutRule=leftTown_rightSea (code)\n";
    }
}
