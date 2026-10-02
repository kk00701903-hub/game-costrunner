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
        private static readonly string[] UiPrefixes = { "UI_", "Icon_", "Watch_", "Raise_", "Sched_", "Cut_", "BG_", "Stand_", "Card_", "FanArt_", "Jacket_" };

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
            importer.filterMode = tile ? FilterMode.Trilinear : FilterMode.Bilinear;
            // 10차: 바닥·바다 타일은 멀어질수록 늘어져 보였다 → 이방성 8 + 트라이리니어(원근 압축에서 줄무늬·번짐 억제)
            importer.anisoLevel = tile ? 8 : 1;
            // No mips on the keyed sprite: mip blending mixes the magenta key into the
            // outline and the chroma test then turns the whole edge dark.
            importer.mipmapEnabled = (world || tile) && !file.StartsWith("GirlSkater_") && !file.StartsWith("Obs_") && !file.StartsWith("Raise_") && !file.StartsWith("Stand_");
            importer.maxTextureSize = 2048;
            // 219차: 가로세로가 2의 거듭제곱이 아닌(NPOT) 그림에 밉맵을 켜면 ASTC 압축이 거부되고 RGBA32 로 들어간다(실험으로 확인).
            // 하늘·원경은 화면 크기 가까이 그려져 밉맵 이득이 작다 → NPOT 이면 밉맵을 끈다(크기·비율은 그대로).
            if (importer.mipmapEnabled)
            {
                importer.GetSourceTextureWidthAndHeight(out int sw, out int sh);
                if (sw > 0 && sh > 0 && (!Mathf.IsPowerOfTwo(sw) || !Mathf.IsPowerOfTwo(sh))) importer.mipmapEnabled = false;
            }
            // Keyed billboards stay uncompressed: the DXT5 path inflated alpha in the
            // fully transparent regions (readback showed a≈90–140 where the PNG has 0),
            // which drew every cloud/town quad as a pale slab.
            // Raise_ 스탠딩(RGBA 컷아웃)도 비압축: DXT가 완전 투명 텍셀의 RGB를 검게 만들어
            // 육성 화면 초상 주변에 검은 상자가 생겼다.
            // 219차(플레이 스토어 준비): 기본 압축이 「비압축」이면 안드로이드 ASTC 오버라이드가 무시되고 RGBA32 로 들어갔다
            // (하늘 배경 73장 = 약 1 GB → APK 832 MB 의 주범). 기본은 압축으로 두고, PC(Standalone)만 비압축(RGBA32) 오버라이드로 예전 그대로.
            bool keepPcRaw = world || file.StartsWith("Raise_") || file.StartsWith("Stand_");
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (keepPcRaw)
            {
                var pc = importer.GetPlatformTextureSettings("Standalone");
                pc.overridden = true; pc.format = TextureImporterFormat.RGBA32; pc.maxTextureSize = 2048;
                importer.SetPlatformTextureSettings(pc);
            }
            importer.npotScale = TextureImporterNPOTScale.None;

            // ── Android 용량 절감 (플레이 스토어 200MB 목표) ──
            // 에디터/PC는 위 설정 그대로(비압축 확인용), 안드로이드만 ASTC로 덮어쓴다.
            // ASTC는 DXT처럼 투명 영역 알파를 부풀리지 않아 키드 빌보드에도 안전하다.
            bool big = file.StartsWith("Sky_") || file.StartsWith("Far_") || file.StartsWith("UI_Title") || file.StartsWith("UI_Act") || file.StartsWith("UI_RunOver") || file.StartsWith("UI_Raising_Room") || file.StartsWith("BG_") || file.StartsWith("Cut_");   // 컷씬 풀스크린 810×1440
            int androidMax = big ? 2048 : 1024;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = androidMax;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = keepPcRaw ? 50 : 100;   // 219차: 큰 하늘 배경은 보통 품질(최고 품질은 한 장에 수십 초)
            android.allowsAlphaSplitting = false;
            importer.SetPlatformTextureSettings(android);
        }

        [MenuItem("Coast Run/Art/Reimport CoastRun textures (apply Android ASTC)")]
        public static void ReimportAll()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') });
            int n = 0;
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("/BGM/")) continue;
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
                n++;
            }
            Debug.Log("[Art] reimported " + n + " textures");
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
