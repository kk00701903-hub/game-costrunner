#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Firefly-painted art under Resources/CoastRun: alpha is transparency (no dark
    /// fringes on keyed clouds/town), clamped wrap so quads never show the far edge,
    /// mipmaps for world billboards, none for UI. Runs automatically on import.
    public class ArtImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/CoastRun/";

        // GirlSkater_* sprites are chroma-keyed billboards: keep them uncompressed and
        // at native 1024×1536 — the default importer rounded them to 1024×2048 (NPOT
        // scale) which squashed the character, and DXT bled magenta into the outline.
        private static readonly string[] WorldPrefixes = { "Sky_", "Cloud_", "Far_", "GirlSkater_", "Obs_" };
        private static readonly string[] TilePrefixes = { "Tex_", "Sea_" };
        private static readonly string[] UiPrefixes = { "UI_", "Icon_", "Watch_", "Raise_", "Sched_", "Cut_" };

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Folder) || path.StartsWith(Folder + "BGM/"))
                return;

            string file = System.IO.Path.GetFileName(path);
            bool world = StartsWithAny(file, WorldPrefixes);
            bool tile = StartsWithAny(file, TilePrefixes);
            bool ui = StartsWithAny(file, UiPrefixes);
            if (!world && !ui && !tile)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            // No mips on the keyed sprite: mip blending mixes the magenta key into the
            // outline and the chroma test then turns the whole edge dark.
            importer.mipmapEnabled = (world || tile) && !file.StartsWith("GirlSkater_") && !file.StartsWith("Obs_") && !file.StartsWith("Raise_");
            importer.maxTextureSize = 2048;
            // Keyed billboards stay uncompressed: the DXT5 path inflated alpha in the
            // fully transparent regions (readback showed a≈90–140 where the PNG has 0),
            // which drew every cloud/town quad as a pale slab.
            // Raise_ 스탠딩(RGBA 컷아웃)도 비압축: DXT가 완전 투명 텍셀의 RGB를 검게 만들어
            // 육성 화면 초상 주변에 검은 상자가 생겼다.
            importer.textureCompression = world || file.StartsWith("Raise_")
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        private static bool StartsWithAny(string s, string[] prefixes)
        {
            foreach (var p in prefixes)
                if (s.StartsWith(p)) return true;
            return false;
        }
    }
}
#endif
