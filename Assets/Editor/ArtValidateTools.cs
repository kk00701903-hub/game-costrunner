#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ArtValidateTools
{
    [MenuItem("Tools/Archive A-0347/Validate Art")]
    public static void ValidateArt()
    {
        ValidateArt(showDialog: true);
    }

    public static void ValidateArt(bool showDialog)
    {
        var report = new StringBuilder();
        report.AppendLine("347 Art Validate");
        report.AppendLine("================");

        ValidateFolder("Assets/Resources/Tracks", report);
        ValidateFolder("Assets/Resources/Props", report);
        ValidateFolder("Assets/Resources/Hazards", report);
        ValidateFolder("Assets/Resources/Retrieval", report);
        ValidateFolder("Assets/Resources/Items", report);
        ValidateFolder("Assets/Resources/Character/Doha", report);

        MaterialLibrary library = Resources.Load<MaterialLibrary>("347/MaterialLibrary");
        report.AppendLine(library != null && library.HasBakedSurfaces
            ? "MaterialLibrary: OK (baked surfaces found)"
            : "MaterialLibrary: MISSING — run Tools > 347 > Setup Visual Pipeline");

        report.AppendLine(GraphicsSettings.defaultRenderPipeline != null
            ? "URP: OK"
            : "URP: NOT ASSIGNED — run Tools > 347 > Fix Render Pipeline");

        Debug.Log(report.ToString());
        File.WriteAllText("Temp/347-art-validate.txt", report.ToString());
        if (showDialog && !Application.isBatchMode)
            EditorUtility.DisplayDialog("347 Validate Art", report.ToString(), "OK");
    }

    private static void ValidateFolder(string folder, StringBuilder report)
    {
        if (!Directory.Exists(folder))
        {
            report.AppendLine(folder + ": (missing)");
            return;
        }

        string[] glbs = Directory.GetFiles(folder, "*.glb", SearchOption.AllDirectories);
        string[] prefabs = Directory.GetFiles(folder, "*.prefab", SearchOption.AllDirectories);
        report.AppendLine(folder + ": " + glbs.Length + " GLB, " + prefabs.Length + " prefab");

        foreach (string path in glbs)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            if (asset == null)
                continue;

            int tris = CountTriangles(asset);
            string note = tris > 20000 ? " HIGH POLY" : tris > 8000 ? " check mobile" : "";
            report.AppendLine("  " + Path.GetFileName(path) + ": ~" + tris + " tris" + note);
        }
    }

    private static int CountTriangles(GameObject root)
    {
        int total = 0;
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh != null)
                total += mesh.triangles.Length / 3;
        }

        SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            Mesh mesh = skinned[i].sharedMesh;
            if (mesh != null)
                total += mesh.triangles.Length / 3;
        }

        return total;
    }
}
#endif
