using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CoastRun;

/// Full integration audit: Resources, scenes, textures, prefabs.
public static class CoastIntegrationTest
{
    private const string ReportRel = "Assets/_Guide/Capture/visual_compare/integration_report_unity.txt";

    [MenuItem("Tools/Coast Run/Integration Test (Audit + Report)")]
    public static void RunAuditMenu()
    {
        string report = RunAudit();
        Debug.Log(report);
    }

    public static void RunAuditBatch()
    {
        RunAudit();
        EditorApplication.Exit(0);
    }

    public static string RunAudit()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Coast Run Integration Audit ===");
        sb.AppendLine(System.DateTime.Now.ToString("u"));
        sb.AppendLine();

        int pass = 0, fail = 0;
        void Check(bool ok, string label)
        {
            sb.AppendLine((ok ? "[PASS] " : "[FAIL] ") + label);
            if (ok) pass++; else fail++;
        }

        // Textures
        string[] textures =
        {
            "SummerSky_Portrait", "Sea_Turquoise_Tile", "Road_Promenade",
            "Icon_Coin", "Icon_Speed", "Icon_Magnet", "Icon_Tower", "Icon_Him",
            "Watch_Frame", "UI_Panel_Memory", "UI_TitleBackground"
        };
        sb.AppendLine("--- Textures (Resources/CoastRun) ---");
        foreach (var t in textures)
            Check(ArtAssets.LoadTexture(t) != null, "Texture " + t);

        // Scene stills
        sb.AppendLine("--- Scene stills ---");
        for (int i = 1; i <= 5; i++)
            Check(CoastSceneArt.LoadFrame(i) != null, "Scene_Frame_" + i);

        // Prefabs
        string[] prefabs =
        {
            "GirlSkater", "Pole_WireSet", "Tile_Promenade_30m", "Tile_TownL_ShopA",
            "Tile_SeaWallR_30m", "Obstacle_Cone", "Prop_Bench", "Prop_StreetLamp"
        };
        sb.AppendLine("--- Prefabs ---");
        foreach (var p in prefabs)
            Check(PrefabLibrary.HasPrefab(p), "Prefab " + p);

        // Scenes in build
        sb.AppendLine("--- Scenes ---");
        foreach (string s in CoastScenes.BuildOrder)
            Check(File.Exists(CoastScenes.Path(s)), s + ".unity exists");

        bool menuInBuild = false, runInBuild = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path.Contains(CoastScenes.Title)) menuInBuild = true;
            if (s.path.Contains(CoastScenes.Run)) runInBuild = true;
        }
        Check(menuInBuild, CoastScenes.Title + " in Build Settings");
        Check(runInBuild, CoastScenes.Run + " in Build Settings");

        // Boot smoke (edit mode)
        sb.AppendLine("--- Edit-mode boot smoke ---");
        try
        {
            EditorSceneManager.OpenScene(CoastScenes.Path(CoastScenes.Run));
            var boot = Object.FindAnyObjectByType<CoastRunBootstrap>();
            if (boot == null)
            {
                var go = new GameObject("CoastRunBootstrap");
                boot = go.AddComponent<CoastRunBootstrap>();
            }
            boot.Build();
            Check(Object.FindAnyObjectByType<CoastRun.PlayerController>() != null, "Player spawned");
            Check(Object.FindAnyObjectByType<MapGenerator>() != null, "MapGenerator spawned");
            Check(Camera.main != null, "Camera exists");
        }
        catch (System.Exception ex)
        {
            Check(false, "Boot smoke: " + ex.Message);
        }

        // Soft: tower can be procedural at runtime
        sb.AppendLine("--- Soft checks ---");
        bool hasTower = PrefabLibrary.HasPrefab("TransmissionTower");
        sb.AppendLine((hasTower ? "[PASS] " : "[WARN] ") + "Prefab TransmissionTower (optional — DestinationGate falls back)");

        sb.AppendLine();
        sb.AppendLine("Summary: " + pass + " pass, " + fail + " fail");
        string report = sb.ToString();
        string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportRel));
        Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? "Temp");
        File.WriteAllText(outPath, report);
        // Also mirror to Temp for scripts
        string tempMirror = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp/visual_compare/integration_report.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(tempMirror) ?? "Temp");
        File.WriteAllText(tempMirror, report);
        return report;
    }
}
