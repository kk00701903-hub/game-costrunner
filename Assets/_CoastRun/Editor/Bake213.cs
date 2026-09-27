using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 213차(사용자: 「단점들 커버해줘」 — 212차 평가: 마을 진입 약 100초): 마을 지형 스플랫(2560²)을 미리 구워 PNG 로.
    /// 이름이 Tex_ 로 시작하지 않게 해서 ArtImportSettings(2048·안드로이드 1024 제한)를 비켜 가고, 여기서 직접 4096·NPOT 그대로·ASTC 로 맞춘다.
    public static class Bake213
    {
        [MenuItem("Coast Run/Dev/213 - Bake village splat")]
        public static void BakeSplat()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tex = CoastRun.Village.VillageWorld.SplatForBake();
            string path = "Assets/Resources/" + CoastRun.Village.VillageWorld.BakedSplatPath + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 16;
            imp.npotScale = TextureImporterNPOTScale.None; imp.maxTextureSize = 4096; imp.alphaSource = TextureImporterAlphaSource.None;
            imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.isReadable = true;   // VillageFlora·VillageMap 이 GetPixelBilinear 로 읽는다
            var android = imp.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = 4096; android.format = TextureImporterFormat.ASTC_6x6; android.compressionQuality = 100; imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
            Debug.LogWarning($"[213] baked splat {tex.width}x{tex.height} → {path} in {sw.ElapsedMilliseconds} ms");
        }

        [MenuItem("Coast Run/Dev/213 - Fix splat import")]
        public static void FixSplatImport()
        {
            string path = "Assets/Resources/" + CoastRun.Village.VillageWorld.BakedSplatPath + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); if (imp == null) { Debug.LogWarning("[213] no splat"); return; }
            imp.isReadable = true; imp.npotScale = TextureImporterNPOTScale.None; imp.maxTextureSize = 4096; imp.mipmapEnabled = true;
            imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.SaveAndReimport();
            Debug.LogWarning("[213] splat import fixed (readable)");
        }

        /// 213차: 아이템 아이콘(Resources/CoastRun/Items) 가져오기 설정 — 투명 가장자리 · 256 · 밉 없음
        [MenuItem("Coast Run/Dev/213 - Fix item icon import")]
        public static void FixIcons()
        {
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/CoastRun/Items" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g); var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                imp.textureType = TextureImporterType.Default; imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.maxTextureSize = 256;
                imp.wrapMode = TextureWrapMode.Clamp; imp.textureCompression = TextureImporterCompression.Compressed;
                var android = imp.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = 256; android.format = TextureImporterFormat.ASTC_6x6; android.compressionQuality = 100; imp.SetPlatformTextureSettings(android);
                imp.SaveAndReimport(); n++;
            }
            Debug.LogWarning($"[213] item icons reimported: {n}");
        }
    }
}
