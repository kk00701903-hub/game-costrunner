using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 144차: 마을에서 쓰는 텍스처 임포트 프리셋 — 뭉개짐/도트 방지.
    /// Trilinear + Aniso 8 + Mipmap(Kaiser) + sRGB + 고품질 압축(ASTC 6x6/BC7), 알파 없는 배경은 최대 2048.
    public static class TextureSoftPreset
    {
        [MenuItem("Coast Run/Dev/Textures - Soft preset (village)")]
        static void Run()
        {
            string[] prefixes = { "Sky_", "Sea_", "Tex_", "Far_", "BG_", "Cloud_", "Side_", "Raise_", "Icon_", "UI_" };
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/CoastRun" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileName(p);
                bool hit = false; foreach (var pre in prefixes) if (name.StartsWith(pre)) { hit = true; break; }
                if (!hit) continue;
                var imp = AssetImporter.GetAtPath(p) as TextureImporter; if (imp == null) continue;
                bool ui = name.StartsWith("Icon_") || name.StartsWith("UI_");
                bool changed = false;
                void Set<T>(ref T field, T v) { if (!Equals(field, v)) { field = v; changed = true; } }
                var fm = imp.filterMode; Set(ref fm, FilterMode.Trilinear); imp.filterMode = fm;
                var an = imp.anisoLevel; Set(ref an, ui ? 1 : 8); imp.anisoLevel = an;
                var mm = imp.mipmapEnabled; Set(ref mm, !ui); imp.mipmapEnabled = mm;              // UI 스프라이트는 밉맵 X(번짐), 월드는 O
                if (!ui) { var mf = imp.mipmapFilter; Set(ref mf, TextureImporterMipFilter.KaiserFilter); imp.mipmapFilter = mf; imp.mipMapsPreserveCoverage = false; }
                var srgb = imp.sRGBTexture; Set(ref srgb, true); imp.sRGBTexture = srgb;
                var tc = imp.textureCompression; Set(ref tc, TextureImporterCompression.CompressedHQ); imp.textureCompression = tc;
                var ms = imp.maxTextureSize; Set(ref ms, 2048); imp.maxTextureSize = ms;
                if (imp.streamingMipmaps) { imp.streamingMipmaps = false; changed = true; }
                if (changed) { imp.SaveAndReimport(); n++; }
            }
            Debug.Log("[TextureSoftPreset] reimported " + n + " textures");
        }
    }
}
