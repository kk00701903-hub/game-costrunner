#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace CoastRun.Editor
{
    /// 154차: 클링 도구 아이콘 PNG 를 Sprite 로 임포트(Resources.Load<Sprite>)
    public static class ToolIconImport
    {
        [MenuItem("Coast Run/Look/Import tool icons as sprites (154차)")]
        public static void Apply()
        {
            string[] names = { "UI_Tool_Net", "UI_Tool_Pick", "UI_Tool_Axe", "UI_Tool_Bat" };
            foreach (var n in names)
            {
                string path = "Assets/Resources/CoastRun/Textures/Village/" + n + ".png";
                var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) { Debug.LogWarning("[ToolIcon] missing " + path); continue; }
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
            Debug.LogWarning("[ToolIcon] done");
        }
    }
}
#endif
