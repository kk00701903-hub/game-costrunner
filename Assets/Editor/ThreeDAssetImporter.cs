#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Imports stylized 3D-rendered PNG packs (FX sprites + legacy UI fallbacks).
public static class ThreeDAssetImporter
{
    private const string ManifestPath = "Assets/_Guide/ThreeDAssetPack.json";
    private const string TempRoot = "Temp/3d-assets";

    [MenuItem("Tools/Archive A-0347/Import 3D Assets Zip")]
    public static void ImportFromDialog()
    {
        string zip = EditorUtility.OpenFilePanel("347 — 3D Assets Zip", ResolveDefaultZipFolder(), "zip");
        if (string.IsNullOrEmpty(zip))
            return;

        if (!ImportZip(zip, out int ok, out int total))
        {
            EditorUtility.DisplayDialog("347 3D Assets", "Import failed. Check Console for details.", "OK");
            return;
        }

        Debug.Log("347 3D Assets: imported " + ok + " / " + total + " from " + zip);
    }

    /// <summary>Unity batchmode: -executeMethod ThreeDAssetImporter.ImportBatch</summary>
    public static void ImportBatch()
    {
        string zip = ResolveZipPath();
        if (!ImportZip(zip, out int ok, out int total))
        {
            Debug.LogError("347 3D Assets batch failed — zip not found or empty: " + zip);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("347 3D Assets batch: " + ok + " / " + total);
        EditorApplication.Exit(ok == total ? 0 : 1);
    }

    private static bool ImportZip(string zipPath, out int ok, out int total)
    {
        ok = 0;
        total = 0;

        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
        {
            Debug.LogError("347 3D Assets: zip missing — " + zipPath);
            return false;
        }

        ThreeDAssetPack pack = LoadManifest();
        if (pack?.assets == null || pack.assets.Length == 0)
        {
            Debug.LogError("347 3D Assets: empty manifest at " + ManifestPath);
            return false;
        }

        string extractDir = Path.Combine(Directory.GetCurrentDirectory(), TempRoot);
        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, true);

        ZipFile.ExtractToDirectory(zipPath, extractDir);

        string projectRoot = Directory.GetCurrentDirectory();
        total = pack.assets.Length;

        for (int i = 0; i < pack.assets.Length; i++)
        {
            ThreeDAssetEntry entry = pack.assets[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.path))
                continue;

            EditorUtility.DisplayProgressBar("347 3D Assets", entry.path, (float)i / total);

            string src = FindSource(extractDir, entry.source);
            if (string.IsNullOrEmpty(src))
            {
                Debug.LogWarning("347 3D Assets: missing " + entry.source);
                continue;
            }

            byte[] raw = File.ReadAllBytes(src);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(raw))
            {
                Debug.LogWarning("347 3D Assets: decode failed " + entry.source);
                UnityEngine.Object.DestroyImmediate(source);
                continue;
            }

            Texture2D processed = ProcessForGame(source, entry.process);
            UnityEngine.Object.DestroyImmediate(source);

            string destPath = Path.Combine(projectRoot, pack.drop_root ?? "Assets/Resources", entry.path);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? projectRoot);
            File.WriteAllBytes(destPath, processed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(processed);

            ok++;
            Debug.Log("347 3D Assets: " + entry.path + " ← " + Path.GetFileName(src));
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        ConfigureImportedTextures(pack);
        AssetDatabase.SaveAssets();
        return ok > 0;
    }

    private static string FindSource(string root, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        string direct = Path.Combine(root, fileName);
        if (File.Exists(direct))
            return direct;

        string[] matches = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
        return matches.Length > 0 ? matches[0] : null;
    }

    private static Texture2D ProcessForGame(Texture2D source, string process)
    {
        switch (process)
        {
            case "fx":
                return ResizeSquare(source, 256);
            case "panel":
                return Resize(CenterCrop(source, 16f / 10f), 1024, 640);
            case "button":
                return Resize(CenterCrop(source, 4f, 1f), 1024, 256);
            case "icon":
            default:
                return ResizeSquare(CenterCrop(source, 1f, 1f), 256);
        }
    }

    private static Texture2D ResizeSquare(Texture2D source, int size)
    {
        return Resize(CenterCrop(source, 1f, 1f), size, size);
    }

    private static Texture2D CenterCrop(Texture2D source, float aspectW, float aspectH)
    {
        float target = aspectW / aspectH;
        float current = (float)source.width / source.height;
        int cropW;
        int cropH;

        if (current > target)
        {
            cropH = source.height;
            cropW = Mathf.RoundToInt(cropH * target);
        }
        else
        {
            cropW = source.width;
            cropH = Mathf.RoundToInt(cropW / target);
        }

        cropW = Mathf.Clamp(cropW, 1, source.width);
        cropH = Mathf.Clamp(cropH, 1, source.height);
        int x0 = (source.width - cropW) / 2;
        int y0 = (source.height - cropH) / 2;

        Color[] pixels = source.GetPixels(x0, y0, cropW, cropH);
        Texture2D cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }

    private static Texture2D CenterCrop(Texture2D source, float aspect)
    {
        return CenterCrop(source, aspect, 1f);
    }

    private static Texture2D Resize(Texture2D source, int width, int height)
    {
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] dst = new Color[width * height];
        float xScale = (float)source.width / width;
        float yScale = (float)source.height / height;

        for (int y = 0; y < height; y++)
        {
            float sy = (y + 0.5f) * yScale;
            for (int x = 0; x < width; x++)
            {
                float sx = (x + 0.5f) * xScale;
                dst[y * width + x] = source.GetPixelBilinear(sx / source.width, sy / source.height);
            }
        }

        result.SetPixels(dst);
        result.Apply();
        return result;
    }

    private static void ConfigureImportedTextures(ThreeDAssetPack pack)
    {
        for (int i = 0; i < pack.assets.Length; i++)
        {
            ThreeDAssetEntry entry = pack.assets[i];
            if (entry == null)
                continue;

            string assetPath = (pack.drop_root ?? "Assets/Resources") + "/" + entry.path;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            bool isFx = entry.process == "fx";
            bool isIcon = entry.process == "icon";
            bool isPanel = entry.process == "panel";
            bool isButton = entry.process == "button";

            importer.textureType = isFx ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.spriteImportMode = isFx ? SpriteImportMode.None : SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.isReadable = isFx || isIcon;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = isFx || isIcon ? 256 : 1024;

            if (isPanel)
                importer.spriteBorder = new Vector4(64f, 64f, 64f, 64f);
            if (isButton)
                importer.spriteBorder = new Vector4(48f, 32f, 48f, 32f);

            importer.SaveAndReimport();
        }
    }

    private static ThreeDAssetPack LoadManifest()
    {
        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
        if (text != null)
            return JsonUtility.FromJson<ThreeDAssetPack>(text.text);

        string path = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<ThreeDAssetPack>(File.ReadAllText(path, Encoding.UTF8));
    }

    private static string ResolveZipPath()
    {
        string env = LoadEnv("THREE_D_ASSETS_ZIP");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            return env;

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(userProfile, "Downloads", "3D_Assets.zip");
        if (File.Exists(downloads))
            return downloads;

        return Path.Combine(Directory.GetCurrentDirectory(), TempRoot, "3D_Assets.zip");
    }

    private static string ResolveDefaultZipFolder()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(userProfile, "Downloads");
        return Directory.Exists(downloads) ? downloads : Directory.GetCurrentDirectory();
    }

    private static string LoadEnv(string key)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(path))
            return null;

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || !line.Contains("="))
                continue;

            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            string name = line.Substring(0, eq).Trim();
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                continue;

            return line.Substring(eq + 1).Trim().Trim('"');
        }

        return null;
    }

    [Serializable]
    private class ThreeDAssetPack
    {
        public string drop_root;
        public ThreeDAssetEntry[] assets;
    }

    [Serializable]
    private class ThreeDAssetEntry
    {
        public string source;
        public string path;
        public string process;
    }
}
#endif
