#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Downloads and processes Pexels photos for 『347』 UI/concept art.
public static class PexelsImageImporter
{
    private const string ManifestPath = "Assets/_Guide/PexelsImagePack.json";
    private const string CreditsPath = "Assets/_Guide/PexelsImageCredits.json";

    [MenuItem("Tools/Archive A-0347/Fetch Pexels Images")]
    public static void FetchAll()
    {
        if (!TryFetchAll(out int ok, out int total))
        {
            EditorUtility.DisplayDialog(
                "Pexels API key missing",
                "Add your key to .env at the project root:\n\nPEXELS_API_KEY=your_key\n\nGet one at https://www.pexels.com/api/",
                "OK");
            return;
        }

        Debug.Log("347 Pexels: downloaded " + ok + " / " + total + ". Credits → " + CreditsPath);
    }

    /// <summary>Unity batchmode: -executeMethod PexelsImageImporter.FetchAllBatch</summary>
    public static void FetchAllBatch()
    {
        TryFetchAll(out int ok, out int total);
        Debug.Log("347 Pexels batch: " + ok + " / " + total);
        EditorApplication.Exit(ok == total ? 0 : 1);
    }

    private static bool TryFetchAll(out int ok, out int total)
    {
        ok = 0;
        total = 0;

        string apiKey = LoadEnv("PEXELS_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        PexelsImagePack pack = LoadManifest();
        if (pack?.assets == null || pack.assets.Length == 0)
        {
            Debug.LogError("347 Pexels: empty manifest at " + ManifestPath);
            return false;
        }

        string baseUrl = LoadEnv("PEXELS_API_BASE_URL") ?? "https://api.pexels.com/v1";
        string projectRoot = Directory.GetCurrentDirectory();
        var credits = new List<PexelsCreditEntry>();
        total = pack.assets.Length;

        for (int i = 0; i < pack.assets.Length; i++)
        {
            PexelsImageEntry entry = pack.assets[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.path))
                continue;

            EditorUtility.DisplayProgressBar("347 Pexels", entry.id ?? entry.path, (float)i / total);

            try
            {
                if (FetchOne(baseUrl, apiKey, projectRoot, pack.drop_root, entry, out PexelsCreditEntry credit))
                {
                    ok++;
                    if (credit != null)
                        credits.Add(credit);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("347 Pexels: failed " + entry.id + " — " + ex.Message);
            }

            System.Threading.Thread.Sleep(350);
        }

        EditorUtility.ClearProgressBar();
        WriteCredits(credits);
        AssetDatabase.Refresh();
        ConfigureImportedTextures(pack);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static bool FetchOne(
        string baseUrl,
        string apiKey,
        string projectRoot,
        string dropRoot,
        PexelsImageEntry entry,
        out PexelsCreditEntry credit)
    {
        credit = null;
        PexelsPhoto photo = entry.photo_id > 0
            ? FetchPhoto(baseUrl, apiKey, entry.photo_id)
            : SearchPhoto(baseUrl, apiKey, entry);

        if (photo == null)
        {
            Debug.LogWarning("347 Pexels: no photo for " + entry.id);
            return false;
        }

        string imageUrl = PickSourceUrl(photo, entry.process);
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning("347 Pexels: missing src URL for " + entry.id);
            return false;
        }

        byte[] raw = DownloadBytes(imageUrl);
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(raw))
        {
            Debug.LogWarning("347 Pexels: decode failed " + entry.id);
            return false;
        }

        Texture2D processed = ProcessForGame(source, entry.process);
        UnityEngine.Object.DestroyImmediate(source);

        string fileName = Path.GetFileNameWithoutExtension(entry.path);
        string relDir = Path.GetDirectoryName(entry.path)?.Replace('\\', '/') ?? string.Empty;
        string destDir = Path.Combine(projectRoot, dropRoot ?? "Assets/Resources", relDir);
        Directory.CreateDirectory(destDir);

        string destPath = Path.Combine(destDir, fileName + ".png");
        File.WriteAllBytes(destPath, processed.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(processed);

        credit = new PexelsCreditEntry
        {
            id = entry.id,
            path = (dropRoot ?? "Assets/Resources") + "/" + relDir + "/" + fileName + ".png",
            pexels_id = photo.id,
            photographer = photo.photographer,
            photographer_url = photo.photographer_url,
            photo_url = photo.url,
            query = entry.query,
            process = entry.process
        };

        Debug.Log("347 Pexels: " + entry.id + " ← photo " + photo.id + " → " + destPath);
        return true;
    }

    private static PexelsPhoto FetchPhoto(string baseUrl, string apiKey, int photoId)
    {
        string json = GetJson(baseUrl.TrimEnd('/') + "/photos/" + photoId, apiKey);
        return JsonUtility.FromJson<PexelsPhoto>(json);
    }

    private static PexelsPhoto SearchPhoto(string baseUrl, string apiKey, PexelsImageEntry entry)
    {
        string query = Uri.EscapeDataString(entry.query ?? entry.id ?? "abstract");
        string orientation = string.IsNullOrWhiteSpace(entry.orientation) ? string.Empty : "&orientation=" + entry.orientation;
        string json = GetJson(baseUrl.TrimEnd('/') + "/search?query=" + query + "&per_page=1" + orientation, apiKey);
        PexelsSearchResponse response = JsonUtility.FromJson<PexelsSearchResponse>(json);
        return response?.photos != null && response.photos.Length > 0 ? response.photos[0] : null;
    }

    private static string PickSourceUrl(PexelsPhoto photo, string process)
    {
        if (photo?.src == null)
            return null;

        bool large = process == "concept";
        if (large)
            return FirstNonEmpty(photo.src.large2x, photo.src.large, photo.src.original, photo.src.medium);

        return FirstNonEmpty(photo.src.large, photo.src.medium, photo.src.large2x, photo.src.original);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrEmpty(values[i]))
                return values[i];
        }

        return null;
    }

    private static Texture2D ProcessForGame(Texture2D source, string process)
    {
        switch (process)
        {
            case "concept":
                return ApplyConcept(source);
            case "panel":
                return ApplyPanel(source);
            case "button":
                return ApplyButton(source);
            case "frame":
                return ApplyFrame(source);
            case "deck_ok":
                return ApplyDeck(source, new Color(0.55f, 0.82f, 0.95f), 0.18f, 1f);
            case "deck_cracked":
                return ApplyDeck(source, new Color(1f, 0.55f, 0.22f), 0.32f, 0.82f);
            case "deck_broken":
                return ApplyDeck(source, new Color(0.95f, 0.25f, 0.22f), 0.42f, 0.55f);
            case "icon":
            default:
                return ApplyIcon(source);
        }
    }

    private static Texture2D ApplyIcon(Texture2D source)
    {
        const int size = 256;
        Texture2D square = CenterCrop(source, 1f, 1f);
        Texture2D scaled = Resize(square, size, size);
        if (square != source)
            UnityEngine.Object.DestroyImmediate(square);

        Color[] px = scaled.GetPixels();
        float cx = (size - 1) * 0.5f;
        float cy = cx;
        float radius = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01((dist - radius * 0.55f) / (radius * 0.65f));
                float vignette = 1f - t * t * 0.95f;
                Color c = px[i];
                c.r = Mathf.Clamp01(c.r * vignette);
                c.g = Mathf.Clamp01(c.g * vignette);
                c.b = Mathf.Clamp01(c.b * vignette);
                c.r = Mathf.Pow(c.r, 0.92f);
                c.g = Mathf.Pow(c.g, 0.92f);
                c.b = Mathf.Pow(c.b, 0.92f);
                px[i] = c;
            }
        }

        scaled.SetPixels(px);
        scaled.Apply();
        return scaled;
    }

    private static Texture2D ApplyPanel(Texture2D source)
    {
        Texture2D crop = CenterCrop(source, 2f, 1f);
        Texture2D scaled = Resize(crop, 1024, 512);
        if (crop != source)
            UnityEngine.Object.DestroyImmediate(crop);

        TintMultiply(scaled, new Color(0.22f, 0.21f, 0.20f), 0.88f);
        return scaled;
    }

    private static Texture2D ApplyButton(Texture2D source)
    {
        Texture2D crop = CenterCrop(source, 3.5f, 1f);
        Texture2D scaled = Resize(crop, 1024, 256);
        if (crop != source)
            UnityEngine.Object.DestroyImmediate(crop);

        Color[] px = scaled.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            c.r = Mathf.Clamp01(Mathf.Pow(c.r, 0.85f) * 1.08f);
            c.g = Mathf.Clamp01(Mathf.Pow(c.g, 0.9f) * 0.92f);
            c.b = Mathf.Clamp01(c.b * 0.72f);
            px[i] = c;
        }

        scaled.SetPixels(px);
        scaled.Apply();
        return scaled;
    }

    private static Texture2D ApplyFrame(Texture2D source)
    {
        const int size = 512;
        Texture2D square = CenterCrop(source, 1f, 1f);
        Texture2D scaled = Resize(square, size, size);
        if (square != source)
            UnityEngine.Object.DestroyImmediate(square);

        Color[] px = scaled.GetPixels();
        float cx = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                float edge = Mathf.Min(x, y, size - 1 - x, size - 1 - y) / (size * 0.12f);
                float frame = 1f - Mathf.Clamp01(edge);
                Color c = px[i];
                c.r *= 0.28f + frame * 0.22f;
                c.g *= 0.27f + frame * 0.20f;
                c.b *= 0.26f + frame * 0.18f;
                c.r += frame * 0.55f;
                c.g += frame * 0.42f;
                c.b += frame * 0.12f;
                px[i] = c;
            }
        }

        scaled.SetPixels(px);
        scaled.Apply();
        return scaled;
    }

    private static Texture2D ApplyConcept(Texture2D source)
    {
        Texture2D crop = CenterCrop(source, 9f, 16f);
        Texture2D scaled = Resize(crop, 1080, 1920);
        if (crop != source)
            UnityEngine.Object.DestroyImmediate(crop);

        Color[] px = scaled.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            c.r = Mathf.Lerp(lum, c.r, 0.82f) * 0.92f;
            c.g = Mathf.Lerp(lum, c.g, 0.82f) * 0.93f;
            c.b = Mathf.Lerp(lum, c.b, 0.82f) * 0.96f;
            c.b += 0.02f;
            px[i] = c;
        }

        scaled.SetPixels(px);
        scaled.Apply();
        return scaled;
    }

    private static Texture2D ApplyDeck(Texture2D source, Color tint, float tintStrength, float brightness)
    {
        Texture2D crop = CenterCrop(source, 3f, 4f);
        Texture2D scaled = Resize(crop, 256, 341);
        if (crop != source)
            UnityEngine.Object.DestroyImmediate(crop);

        Color[] px = scaled.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            c.r *= brightness;
            c.g *= brightness;
            c.b *= brightness;
            c.r = Mathf.Lerp(c.r, c.r * tint.r, tintStrength);
            c.g = Mathf.Lerp(c.g, c.g * tint.g, tintStrength);
            c.b = Mathf.Lerp(c.b, c.b * tint.b, tintStrength);
            px[i] = c;
        }

        scaled.SetPixels(px);
        scaled.Apply();
        return scaled;
    }

    private static void TintMultiply(Texture2D tex, Color tint, float strength)
    {
        Color[] px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i];
            c.r = Mathf.Lerp(c.r, c.r * tint.r, strength);
            c.g = Mathf.Lerp(c.g, c.g * tint.g, strength);
            c.b = Mathf.Lerp(c.b, c.b * tint.b, strength);
            px[i] = c;
        }

        tex.SetPixels(px);
        tex.Apply();
    }

    private static Texture2D CenterCrop(Texture2D source, float aspectW, float aspectH)
    {
        float target = aspectW / aspectH;
        float srcAspect = (float)source.width / source.height;
        int cropW = source.width;
        int cropH = source.height;

        if (srcAspect > target)
            cropW = Mathf.RoundToInt(source.height * target);
        else
            cropH = Mathf.RoundToInt(source.width / target);

        int x0 = (source.width - cropW) / 2;
        int y0 = (source.height - cropH) / 2;
        Color[] pixels = source.GetPixels(x0, y0, cropW, cropH);
        Texture2D cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }

    private static Texture2D Resize(Texture2D source, int width, int height)
    {
        // Batchmode has no GPU blit — scale on CPU so icons/concepts stay distinct.
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
                dst[y * width + x] = source.GetPixelBilinear(
                    sx / source.width,
                    sy / source.height);
            }
        }

        result.SetPixels(dst);
        result.Apply();
        return result;
    }

    private static byte[] DownloadBytes(string url)
    {
        using (var client = new WebClient())
            return client.DownloadData(url);
    }

    private static string GetJson(string url, string apiKey)
    {
        using (var client = new WebClient())
        {
            client.Headers[HttpRequestHeader.Authorization] = apiKey;
            client.Encoding = Encoding.UTF8;
            return client.DownloadString(url);
        }
    }

    private static void ConfigureImportedTextures(PexelsImagePack pack)
    {
        for (int i = 0; i < pack.assets.Length; i++)
        {
            PexelsImageEntry entry = pack.assets[i];
            if (entry == null)
                continue;

            string folder = Path.GetDirectoryName(entry.path)?.Replace('\\', '/') ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(entry.path);
            string assetDir = (pack.drop_root ?? "Assets/Resources") + "/" + folder;

            string[] guids = AssetDatabase.FindAssets(baseName + " t:Texture2D", new[] { assetDir });
            for (int g = 0; g < guids.Length; g++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (Path.GetFileNameWithoutExtension(assetPath) != baseName)
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                bool isUi = folder.StartsWith("UI", StringComparison.OrdinalIgnoreCase);
                bool isIcon = isUi && entry.id != null && entry.id.StartsWith("UI_Icon", StringComparison.Ordinal);
                bool isConcept = folder.StartsWith("Concept", StringComparison.OrdinalIgnoreCase);

                importer.textureType = isUi ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.spriteImportMode = isUi ? SpriteImportMode.Single : SpriteImportMode.None;
                importer.alphaIsTransparency = isUi;
                importer.isReadable = isIcon;
                importer.mipmapEnabled = !isUi && !isConcept;
                importer.maxTextureSize = isIcon ? 256 : isConcept ? 2048 : 1024;
                importer.SaveAndReimport();
            }
        }
    }

    private static PexelsImagePack LoadManifest()
    {
        TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
        if (text == null)
        {
            Debug.LogError("347 Pexels: missing " + ManifestPath);
            return null;
        }

        return JsonUtility.FromJson<PexelsImagePack>(text.text);
    }

    private static void WriteCredits(List<PexelsCreditEntry> credits)
    {
        var wrapper = new PexelsCreditsFile { source = "https://www.pexels.com/", assets = credits.ToArray() };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), CreditsPath), json, Encoding.UTF8);
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
    private class PexelsImagePack
    {
        public string project;
        public string drop_root;
        public PexelsImageEntry[] assets;
    }

    [Serializable]
    private class PexelsImageEntry
    {
        public string id;
        public string path;
        public int photo_id;
        public string process;
        public string query;
        public string orientation;
        public string use;
    }

    [Serializable]
    private class PexelsSearchResponse
    {
        public PexelsPhoto[] photos;
    }

    [Serializable]
    private class PexelsPhoto
    {
        public int id;
        public string url;
        public string photographer;
        public string photographer_url;
        public PexelsSrc src;
    }

    [Serializable]
    private class PexelsSrc
    {
        public string original;
        public string large2x;
        public string large;
        public string medium;
    }

    [Serializable]
    private class PexelsCreditsFile
    {
        public string source;
        public PexelsCreditEntry[] assets;
    }

    [Serializable]
    private class PexelsCreditEntry
    {
        public string id;
        public string path;
        public int pexels_id;
        public string photographer;
        public string photographer_url;
        public string photo_url;
        public string query;
        public string process;
    }
}
#endif
