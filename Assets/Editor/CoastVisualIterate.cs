using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CoastRun;

/// StyleBible compare loop: boot → measure → report (batch-safe PNG + checklist).
public static class CoastVisualIterate
{
    private const string OutDirRel = "Assets/_Guide/Capture/visual_compare";

    [MenuItem("Tools/Coast Run/Visual Iterate (Capture + Score)")]
    public static void RunMenu() => RunOnce();

    public static void RunBatch()
    {
        RunOnce();
        EditorApplication.Exit(0);
    }

    public static void RunOnce()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDirRel));
        Directory.CreateDirectory(outDir);

        if (!File.Exists(CoastRunBootstrap.ScenePath))
            CoastRunMenu.CreateRunScene();
        EditorSceneManager.OpenScene(CoastRunBootstrap.ScenePath);

        var boot = Object.FindAnyObjectByType<CoastRunBootstrap>();
        if (boot == null)
        {
            var go = new GameObject("CoastRunBootstrap");
            boot = go.AddComponent<CoastRunBootstrap>();
        }

        boot.Build();

        var player = Object.FindAnyObjectByType<CoastRun.PlayerController>(FindObjectsInactive.Include);
        var map = Object.FindAnyObjectByType<MapGenerator>();
        const float captureDistance = 48f;
        if (player != null)
        {
            player.SnapForCapture(captureDistance);
            var visual = player.GetComponent<CoastPlayerVisual>();
            if (visual == null)
                visual = player.gameObject.AddComponent<CoastPlayerVisual>();
            visual.Build();
        }
        map?.SetPlayerDistance(captureDistance);

        var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null && player != null)
            CoastOfflineCaptureForceFrame(cam, player);

        string png = Path.Combine(outDir, "game_capture.png");
        if (cam != null)
            RenderToPng(cam, 720, 1280, png);

        string report = BuildScoreReport(player, cam);
        File.WriteAllText(Path.Combine(outDir, "checklist.txt"), report, Encoding.UTF8);
        File.WriteAllText(Path.Combine(outDir, "iteration_log.txt"),
            System.DateTime.Now.ToString("u") + "\n" + report, Encoding.UTF8);

        Debug.Log("CoastVisualIterate:\n" + report);
    }

    internal static void CoastOfflineCaptureForceFrame(Camera cam, CoastRun.PlayerController player)
    {
        Vector3 offset = new Vector3(0.12f, 2.95f, -10.8f);
        Quaternion frame = DownhillPath.Rotation;
        Vector3 pivot = player.transform.position;
        cam.transform.position = pivot + frame * offset;
        Vector3 aim = pivot + frame * new Vector3(0f, 3.4f, 20f);
        cam.transform.rotation = Quaternion.LookRotation((aim - cam.transform.position).normalized, Vector3.up)
                                 * Quaternion.Euler(-1.5f, 0f, 0f);
        cam.fieldOfView = 54f;
        cam.aspect = 720f / 1280f;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = CoastPalette.SkyTop;
    }

    private static void RenderToPng(Camera cam, int w, int h, string path)
    {
        try
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            if (!rt.Create())
            {
                Object.DestroyImmediate(rt);
                File.WriteAllText(path + ".skipped", "RenderTexture unavailable (-nographics?)");
                return;
            }

            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CoastVisualIterate: PNG capture skipped — " + ex.Message);
            File.WriteAllText(path + ".skipped", ex.Message);
        }
    }

    private static string BuildScoreReport(CoastRun.PlayerController player, Camera cam)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Coast Run vs style_reference (StyleBible) ===");
        sb.AppendLine("capture=Assets/_Guide/Capture/visual_compare/game_capture.png");
        sb.AppendLine();

        float girlY = -1f;
        float horizonY = -1f;
        if (player != null && cam != null)
        {
            girlY = cam.WorldToViewportPoint(player.transform.position + Vector3.up * 0.55f).y;
            horizonY = cam.WorldToViewportPoint(player.transform.position + Vector3.up * 0.5f + cam.transform.forward * 80f).y;
        }

        int pass = 0;
        void Check(bool ok, string id, string detail)
        {
            sb.AppendLine((ok ? "[PASS] " : "[FAIL] ") + id + " — " + detail);
            if (ok) pass++;
        }

        bool validFraming = girlY >= 0f && horizonY >= 0f;
        Check(validFraming && horizonY > 0.38f, "sky_third",
            "horizonY=" + horizonY.ToString("F2") + " girlY=" + girlY.ToString("F2") + " (sky should dominate upper frame)");
        Check(validFraming && girlY >= 0.22f && girlY <= 0.40f, "girl_lower_third",
            "girlViewportY=" + girlY.ToString("F3") + " want 0.25-0.35 (tol 0.22-0.40)");
        Check(PrefabLibrary.HasPrefab("GirlSkater"), "girl_prefab", "Resources GirlSkater");
        Check(PrefabLibrary.HasPrefab("Pole_WireSet"), "poles", "Pole_WireSet prefab");
        Check(ArtAssets.LoadTexture("SummerSky_Portrait") != null, "sky_tex", "SummerSky_Portrait");
        Check(ArtAssets.LoadTexture("Sea_Turquoise_Tile") != null, "sea_tex", "Sea_Turquoise_Tile");

        int poles = 0, buildings = 0, wires = 0, npcs = 0, clouds = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("Pole")) poles++;
            if (t.name.StartsWith("Building") || t.name.Contains("Shop")) buildings++;
            if (t.name.StartsWith("Wire")) wires++;
            if (t.name.StartsWith("NPC")) npcs++;
            if (t.name.StartsWith("Cloud_")) clouds++;
        }

        Check(buildings >= 2, "left_town", "buildings=" + buildings);
        Check(poles >= 2, "utility_poles", "poles=" + poles);
        Check(wires >= 2, "power_wires", "wires=" + wires);
        Check(npcs >= 1, "npc_walkers", "npcs=" + npcs);
        Check(clouds >= 8, "summer_clouds", "cloudGroups=" + clouds);

        sb.AppendLine();
        sb.AppendLine("Score: " + pass + " / 11");
        sb.AppendLine("camFov=" + (cam != null ? cam.fieldOfView.ToString("F1") : "n/a"));
        sb.AppendLine("camPos=" + (cam != null ? cam.transform.position.ToString("F2") : "n/a"));
        return sb.ToString();
    }
}
