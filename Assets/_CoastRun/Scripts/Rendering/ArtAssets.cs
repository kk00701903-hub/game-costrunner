using UnityEngine;

namespace CoastRun
{
    /// Loads MCP-exported art from Resources/CoastRun.
    public static class ArtAssets
    {
        public const string ResourceRoot = "CoastRun/";

        public static Texture2D LoadTexture(string fileNameWithoutExt)
        {
            var tex = Resources.Load<Texture2D>(ResourceRoot + fileNameWithoutExt);
            if (tex != null)
                return tex;

            tex = CoastSceneArt.Load(fileNameWithoutExt);
            if (tex != null)
                return tex;

            switch (fileNameWithoutExt)
            {
                case "UI_TitleBackground": return CoastUiArt.TitleBackground;
                case "UI_CharacterHero":
                    return CoastUiArt.LoadOrFallback("UI_TitleBackground", () => CoastUiArt.TitleBackground);
                case "Icon_Coin": return CoastUiArt.CoinIcon;
                case "Icon_Speed": return CoastUiArt.SpeedIcon;
                case "Icon_Magnet": return CoastUiArt.MagnetIcon;
                case "Icon_Tower": return CoastUiArt.TowerIcon;
                case "Icon_Him": return CoastUiArt.HimIcon;
                case "UI_Panel_Memory": return CoastUiArt.MemoryPanel;
                case "Watch_Frame": return CoastUiArt.WatchFrame;
                default: return null;
            }
        }

        public static Material CreateTexturedUnlit(Texture2D tex, Color tint)
        {
            var mat = CoastMaterials.CreateUnlit(tint);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            return mat;
        }

        public static Material CreateTexturedLit(Texture2D tex, Color tint, float smoothness = 0.1f)
        {
            var mat = CoastMaterials.CreateLit(tint, smoothness);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            return mat;
        }

        public static GameObject LoadPrefabOrNull(string resourceName)
        {
            return Resources.Load<GameObject>(ResourceRoot + resourceName);
        }
    }
}
