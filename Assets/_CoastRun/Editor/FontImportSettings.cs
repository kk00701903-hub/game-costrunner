using UnityEditor;

namespace CoastRun.EditorTools
{
    /// Resources/CoastRun/Fonts/*.ttf — 동적 폰트 + OS 폴백(일·태·인니·스페인어는 시스템 폰트로).
    public class FontImportSettings : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/Resources/CoastRun/Fonts/")) return;
            if (!(assetImporter is TrueTypeFontImporter imp)) return;
            imp.fontTextureCase = FontTextureCase.Dynamic;
            imp.includeFontData = true;
            imp.fontNames = new[] { "Pretendard", "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans Thai", "Arial" };
            imp.fontRenderingMode = FontRenderingMode.Smooth;
            imp.characterSpacing = 0; imp.characterPadding = 1;
        }
    }
}
