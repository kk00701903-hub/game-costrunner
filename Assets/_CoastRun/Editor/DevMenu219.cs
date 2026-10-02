using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 219차(사용자: 「플레이 스토어 올릴 준비」): 안드로이드 빌드 용량 — 무압축(RGBA32)으로 들어가던 텍스처에 안드로이드 전용 ASTC 압축을 건다.
    /// 기본(PC) 설정은 건드리지 않고 Android 오버라이드만. CPU 로 픽셀을 읽는 텍스처(Read/Write)·폰트·이미 압축된 것은 건너뛴다.
    /// 결과 로그: Tools/_xfer/astc219.txt
    public static class DevMenu219
    {
        const string Log = "Tools/_xfer/astc219.txt";

        [MenuItem("Coast Run/Store/219 - Texture ASTC (dry run)")] static void Dry() => Run(false);
        [MenuItem("Coast Run/Store/219 - Texture ASTC (apply)")] static void Apply() => Run(true);

        [MenuItem("Coast Run/Store/219 - Build texture diag")]
        static void Diag()
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/CoastRun/Sky_Village_NOON_2.png");
            Debug.LogWarning($"[219] active={EditorUserBuildSettings.activeBuildTarget} subtarget={EditorUserBuildSettings.androidBuildSubtarget} overrideTexComp={EditorUserBuildSettings.overrideTextureCompression} overrideMax={EditorUserBuildSettings.overrideMaxTextureSize} sky={(t != null ? t.format + " " + t.width + "x" + t.height + " mips=" + t.mipmapCount : "null")}");
        }
        [MenuItem("Coast Run/Store/219 - Reimport one sky (test)")]
        static void ReimportOne()
        {
            const string p = "Assets/Resources/CoastRun/Sky_Village_NOON_2.png";
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p); var a = ti.GetPlatformTextureSettings("Android");
            Debug.LogWarning($"[219] compressOnImport={EditorPrefs.GetBool("kCompressTexturesOnImport", true)} cacheServer={EditorSettings.cacheServerMode}");
            Debug.LogWarning($"[219] reimport → {t.format} {t.width}x{t.height}; android ov={a.overridden} fmt={a.format} q={a.compressionQuality}; type={ti.textureType} npot={ti.npotScale} alphaSrc={ti.alphaSource}");
        }
        [MenuItem("Coast Run/Store/219 - Format census")]
        static void Census()
        {
            var c = new System.Collections.Generic.Dictionary<string, int>(); long bytes = 0; var sb = new StringBuilder();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g); var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (t == null) continue;
                var k = t.format.ToString(); c[k] = (c.TryGetValue(k, out var v) ? v : 0) + 1;
                long sz = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t); bytes += sz;
                if (sz > 8 * 1048576) sb.AppendLine($"  {sz / 1048576}MB {t.format} {t.width}x{t.height} {p}");
            }
            Debug.LogWarning("[219] census " + string.Join(", ", c.Select(kv => kv.Key + "=" + kv.Value)) + $" total={bytes / 1048576}MB\n" + sb);
            File.WriteAllText("Tools/_xfer/census219.txt", "[219] census " + string.Join(", ", c.Select(kv => kv.Key + "=" + kv.Value)) + $" total={bytes / 1048576}MB\n" + sb);
        }
        /// ArtImportSettings(219차) 반영 — 하늘·원경·빌보드(비압축이던 것)만 다시 가져온다
        [MenuItem("Coast Run/Store/219 - Reimport world textures (ASTC)")]
        static void ReimportWorld()
        {
            string[] pre = { "Sky_", "Cloud_", "Far_", "GirlSkater_", "Obs_", "Raise_", "Stand_" }; int n = 0;
            var sb = new StringBuilder();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/CoastRun" }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g); var f = Path.GetFileName(p);
                    if (!pre.Any(x => f.StartsWith(x))) continue;
                    AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); n++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            File.WriteAllText("Tools/_xfer/reimport219.txt", $"done {n} {System.DateTime.Now}");
            Debug.LogWarning($"[219] reimported world textures: {n}");
        }
        [MenuItem("Coast Run/Store/219 - ASTC experiment")]
        static void Exp()
        {
            const string p = "Assets/_CoastRun/_t219.png"; AssetDatabase.ImportAsset(p);
            var sb = new StringBuilder();
            foreach (var (mips, npot, fmt) in new[] { (true, TextureImporterNPOTScale.None, TextureImporterFormat.ASTC_6x6), (false, TextureImporterNPOTScale.None, TextureImporterFormat.ASTC_6x6), (true, TextureImporterNPOTScale.ToNearest, TextureImporterFormat.ASTC_6x6), (true, TextureImporterNPOTScale.None, TextureImporterFormat.ETC2_RGB4) })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.textureType = TextureImporterType.Default; ti.mipmapEnabled = mips; ti.npotScale = npot; ti.textureCompression = TextureImporterCompression.Compressed; ti.alphaIsTransparency = true;
                var a = ti.GetPlatformTextureSettings("Android"); a.overridden = true; a.format = fmt; a.maxTextureSize = 2048; a.compressionQuality = 50; ti.SetPlatformTextureSettings(a);
                ti.SaveAndReimport();
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                sb.Append($" [mips={mips} npot={npot} {fmt} → {t.format} {t.width}x{t.height}]");
            }
            Debug.LogWarning("[219] exp" + sb);
        }
        [MenuItem("Coast Run/Store/219 - Android SDK diag")]
        static void SdkDiag()
        {
            string sdk = UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath, jdk = UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath;
            string plats = Directory.Exists(Path.Combine(sdk ?? "", "platforms")) ? string.Join(",", Directory.GetDirectories(Path.Combine(sdk, "platforms")).Select(Path.GetFileName)) : "-";
            string bt = Directory.Exists(Path.Combine(sdk ?? "", "build-tools")) ? string.Join(",", Directory.GetDirectories(Path.Combine(sdk, "build-tools")).Select(Path.GetFileName)) : "-";
            var msg = $"[219] sdk={sdk}\n platforms={plats}\n build-tools={bt}\n jdk={jdk}\n target={PlayerSettings.Android.targetSdkVersion} min={PlayerSettings.Android.minSdkVersion} id={PlayerSettings.applicationIdentifier} ver={PlayerSettings.bundleVersion}({PlayerSettings.Android.bundleVersionCode}) arch={PlayerSettings.Android.targetArchitectures} keystore={PlayerSettings.Android.useCustomKeystore}:{PlayerSettings.Android.keystoreName}";
            File.WriteAllText("Tools/_xfer/sdk219.txt", msg); Debug.LogWarning(msg);
        }
        static void Run(bool apply)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            var sb = new StringBuilder(); int n = 0, skipped = 0; long before = 0;
            if (apply) AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.Contains("/Editor/") || p.Contains("/Tools/") || p.StartsWith("Assets/_Recovery")) continue;
                    var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                    if (ti.isReadable) { skipped++; sb.AppendLine("skip readable  " + p); continue; }
                    if (ti.textureType == TextureImporterType.SingleChannel || ti.textureType == TextureImporterType.Cursor) { skipped++; continue; }
                    var a = ti.GetPlatformTextureSettings("Android");
                    bool compressedAlready = a.overridden && a.format != TextureImporterFormat.RGBA32 && a.format != TextureImporterFormat.RGB24 && a.format != TextureImporterFormat.Automatic;
                    if (compressedAlready) { skipped++; continue; }
                    if (!a.overridden && ti.textureCompression != TextureImporterCompression.Uncompressed) { skipped++; continue; }
                    var fi = new FileInfo(p); before += fi.Exists ? fi.Length : 0;
                    n++;
                    bool ui = ti.textureType == TextureImporterType.Sprite || p.Contains("/UI") || Path.GetFileName(p).StartsWith("UI_") || Path.GetFileName(p).StartsWith("Icon_") || p.Contains("/Items/");
                    var fmt = ui ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;   // 글자·아이콘은 선명하게 4x4, 배경·컷씬은 6x6
                    sb.AppendLine($"{(apply ? "set" : "would")} {fmt} max={Mathf.Min(ti.maxTextureSize, 2048)}  {p}");
                    if (!apply) continue;
                    a.overridden = true; a.format = fmt; a.maxTextureSize = Mathf.Min(ti.maxTextureSize, 2048); a.compressionQuality = 50;
                    ti.SetPlatformTextureSettings(a);
                    ti.SaveAndReimport();
                }
            }
            finally { if (apply) { AssetDatabase.StopAssetEditing(); AssetDatabase.SaveAssets(); } }
            sb.Insert(0, $"[219] ASTC {(apply ? "APPLIED" : "dry run")}: {n} textures (source files {before / 1048576} MB), skipped {skipped}\n");
            File.WriteAllText(Log, sb.ToString());
            Debug.LogWarning($"[219] ASTC {(apply ? "applied" : "dry")}: {n} textures, skipped {skipped} → {Log}");
        }
    }
}
