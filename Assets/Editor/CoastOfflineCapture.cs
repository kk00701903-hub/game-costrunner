using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CoastRun;

/// Batchmode play-mode capture for reference comparison.
/// Unity -batchmode -nographics -projectPath ... -executeMethod CoastOfflineCapture.Run -logFile ...
public static class CoastOfflineCapture
{
    private const string OutDirRel = "Assets/_Guide/Capture/visual_compare";
    private const string Flag = "CoastOfflineCapture.Pending";
    private static double _enteredAt;
    private static bool _busy;

    [InitializeOnLoadMethod]
    private static void Hook()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    /// Checklist-only path (works with -nographics).
    public static void RunChecklistOnly()
    {
        try
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDirRel));
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(CoastScenes.Path(CoastScenes.Run));

            var boot = Object.FindAnyObjectByType<CoastRunBootstrap>();
            if (boot == null)
            {
                var go = new GameObject("CoastRunBootstrap");
                boot = go.AddComponent<CoastRunBootstrap>();
            }

            boot.Build();

            var player = Object.FindAnyObjectByType<CoastRun.PlayerController>();
            var map = Object.FindAnyObjectByType<MapGenerator>();
            const float captureDistance = 48f;
            if (player != null)
                player.SnapForCapture(captureDistance);
            map?.SetPlayerDistance(captureDistance);

            var sky = Object.FindAnyObjectByType<CoastSky>();
            var sea = Object.FindAnyObjectByType<CoastSea>();
            if (player != null)
            {
                sky?.Build(player.transform);
                sea?.Build(player.transform);
            }

            var cam = Camera.main;
            if (cam != null && player != null)
                ForceCameraFrame(cam, player);

            string pngPath = Path.Combine(outDir, "game_capture.png");
            if (cam != null)
                RenderCameraToPng(cam, 720, 1280, pngPath);

            string checklist = BuildChecklist(player, cam);
            File.WriteAllText(Path.Combine(outDir, "checklist.txt"), checklist, System.Text.Encoding.UTF8);
            Debug.Log("CoastOfflineCapture checklist-only OK\n" + checklist);
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            EditorApplication.Exit(1);
        }
    }

    public static void Run()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDirRel));
        Directory.CreateDirectory(outDir);

        EditorSceneManager.OpenScene(CoastScenes.Path(CoastScenes.Run));

        SessionState.SetBool(Flag, true);
        _busy = false;
        Debug.Log("CoastOfflineCapture: entering Play Mode…");
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        if (!SessionState.GetBool(Flag, false))
            return;

        _enteredAt = EditorApplication.timeSinceStartup;
        _busy = true;
        Debug.Log("CoastOfflineCapture: Play entered — waiting for boot.");
    }

    private static void Tick()
    {
        if (!_busy || !EditorApplication.isPlaying)
            return;

        double elapsed = EditorApplication.timeSinceStartup - _enteredAt;

        if (elapsed > 1.0)
        {
            var story = Object.FindFirstObjectByType<StoryManager>();
            if (story != null && !story.PrologueComplete)
                story.SkipPrologue();

            var player = Object.FindAnyObjectByType<CoastRun.PlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                player.enabled = true;
                var map = Object.FindAnyObjectByType<MapGenerator>();
                map?.SetPlayerDistance(55f);
            }
        }

        if (elapsed < 4.0)
            return;

        _busy = false; // prevent re-entry while capturing
        try
        {
            CaptureNow();
            SessionState.SetBool(Flag, false);
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CoastOfflineCapture FAILED: " + ex);
            try
            {
                string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDirRel));
                Directory.CreateDirectory(outDir);
                var player = Object.FindAnyObjectByType<CoastRun.PlayerController>();
                var cam = Camera.main;
                File.WriteAllText(Path.Combine(outDir, "checklist.txt"),
                    "CAPTURE_FAILED: " + ex.Message + "\n\n" + BuildChecklist(player, cam));
            }
            catch { /* ignore */ }

            SessionState.SetBool(Flag, false);
            EditorApplication.Exit(1);
        }
    }

    private static void CaptureNow()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDirRel));
        Directory.CreateDirectory(outDir);

        var player = Object.FindAnyObjectByType<CoastRun.PlayerController>(FindObjectsInactive.Include);
        var cam = Camera.main;
        var map = Object.FindFirstObjectByType<MapGenerator>();

        if (map != null && player != null)
            map.SetPlayerDistance(Mathf.Max(25f, player.PathDistance));

        if (cam == null)
            throw new System.Exception("No Main Camera");

        if (player != null)
            ForceCameraFrame(cam, player);

        string pngPath = Path.Combine(outDir, "game_capture.png");
        RenderCameraToPng(cam, 720, 1280, pngPath);
        File.WriteAllText(Path.Combine(outDir, "checklist.txt"), BuildChecklist(player, cam));
        Debug.Log("CoastOfflineCapture OK → " + pngPath);
    }

    private static void ForceCameraFrame(Camera cam, CoastRun.PlayerController player)
    {
        CoastVisualIterate.CoastOfflineCaptureForceFrame(cam, player);
    }

    private static void RenderCameraToPng(Camera cam, int width, int height, string path)
    {
        // Prefer GPU RT; fall back to ScreenCapture if NullGfxDevice (batch -nographics).
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        if (!rt.IsCreated() && !rt.Create())
        {
            Object.DestroyImmediate(rt);
            Debug.LogWarning("CoastOfflineCapture: RenderTexture unavailable — using ScreenCapture.");
            ScreenCapture.CaptureScreenshot(path);
            // Give filesystem a moment in batchmode.
            System.Threading.Thread.Sleep(500);
            if (!File.Exists(path))
                throw new System.Exception("ScreenCapture also failed (likely -nographics).");
            return;
        }

        var prev = cam.targetTexture;
        var prevRt = RenderTexture.active;
        float prevAspect = cam.aspect;

        cam.aspect = (float)width / height;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());

        cam.targetTexture = prev;
        cam.aspect = prevAspect;
        RenderTexture.active = prevRt;
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    private static string BuildChecklist(CoastRun.PlayerController player, Camera cam)
    {
        float girlY = -1f;
        if (player != null && cam != null)
        {
            Vector3 sp = cam.WorldToViewportPoint(player.transform.position + Vector3.up * 0.9f);
            girlY = sp.y;
        }

        int poles = 0, buildings = 0, wires = 0, npcs = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("Pole")) poles++;
            if (t.name.StartsWith("Building")) buildings++;
            if (t.name.StartsWith("Wire")) wires++;
            if (t.name.StartsWith("NPC")) npcs++;
        }

        return
            "=== Coast Run vs style_reference.mp4 ===\n" +
            "capture=Assets/_Guide/Capture/visual_compare/game_capture.png\n\n" +
            "[Assets in Resources]\n" +
            "sky=" + (ArtAssets.LoadTexture("SummerSky_Portrait") != null) + "\n" +
            "sea=" + (ArtAssets.LoadTexture("Sea_Turquoise_Tile") != null) + "\n" +
            "road=" + (ArtAssets.LoadTexture("Road_Promenade") != null) + "\n" +
            "Icon_Tower=" + (ArtAssets.LoadTexture("Icon_Tower") != null) + "\n" +
            "Icon_Him=" + (ArtAssets.LoadTexture("Icon_Him") != null) + "\n" +
            "UI_Panel_Memory=" + (ArtAssets.LoadTexture("UI_Panel_Memory") != null) + "\n" +
            "Scene_Frame_1=" + (CoastSceneArt.LoadFrame(1) != null) + "\n" +
            "Scene_Frame_5=" + (CoastSceneArt.LoadFrame(5) != null) + "\n" +
            "TransmissionTower=" + PrefabLibrary.HasPrefab("TransmissionTower") + "\n" +
            "GirlSkater=" + PrefabLibrary.HasPrefab("GirlSkater") + "\n" +
            "Pole_WireSet=" + PrefabLibrary.HasPrefab("Pole_WireSet") + "\n" +
            "Tile_Promenade=" + PrefabLibrary.HasPrefab("Tile_Promenade_30m") + "\n" +
            "Tile_TownL=" + PrefabLibrary.HasPrefab("Tile_TownL_ShopA") + "\n" +
            "Tile_SeaWall=" + PrefabLibrary.HasPrefab("Tile_SeaWallR_30m") + "\n\n" +
            "[Framing]\n" +
            "girlViewportY=" + girlY.ToString("F3") + " (want 0.25-0.35)\n" +
            "fov=" + cam.fieldOfView.ToString("F1") + "\n" +
            "camPos=" + cam.transform.position.ToString("F2") + "\n" +
            "playerPos=" + (player != null ? player.transform.position.ToString("F2") : "n/a") + "\n\n" +
            "[World]\n" +
            "poles=" + poles + " buildings=" + buildings + " wires=" + wires + " npcs=" + npcs + "\n";
    }
}
