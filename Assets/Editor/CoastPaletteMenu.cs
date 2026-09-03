using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Ensures CoastPalette.asset exists under Config (+ Resources mirror).
    public static class CoastPaletteMenu
    {
        private const string ConfigPath = "Assets/_CoastRun/Config/CoastPalette.asset";
        private const string ResourcesPath = "Assets/Resources/CoastRun/Config/CoastPalette.asset";

        [MenuItem("Tools/Coast Run/Ensure Coast Palette Asset")]
        public static void EnsureAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CoastPaletteConfig>(ConfigPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CoastPaletteConfig>();
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath) ?? "Assets/_CoastRun/Config");
                AssetDatabase.CreateAsset(asset, ConfigPath);
            }

            CoastPalette.Bind(asset);
            Directory.CreateDirectory("Assets/Resources/CoastRun/Config");
            AssetDatabase.CopyAsset(ConfigPath, ResourcesPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CoastPalette ready at " + ConfigPath);
        }

        [InitializeOnLoadMethod]
        private static void AutoEnsure()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<CoastPaletteConfig>(ConfigPath) == null)
                    EnsureAsset();
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<CoastPaletteConfig>(ConfigPath);
                    CoastPalette.Bind(asset);
                }
            };
        }
    }
}
