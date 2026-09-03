using UnityEngine;

namespace CoastRun
{
    /// Runtime color accessors — always derived from CoastPaletteConfig (no rogue hues).
    public static class CoastPalette
    {
        private const string ResourcePath = "CoastRun/Config/CoastPalette";
        private const string EditorPath = "Assets/_CoastRun/Config/CoastPalette.asset";

        private static CoastPaletteConfig _config;
        private static CoastPaletteConfig _fallback;

        public static CoastPaletteConfig Active
        {
            get
            {
                if (_config != null)
                    return _config;
                _config = Resources.Load<CoastPaletteConfig>(ResourcePath);
#if UNITY_EDITOR
                if (_config == null)
                    _config = UnityEditor.AssetDatabase.LoadAssetAtPath<CoastPaletteConfig>(EditorPath);
#endif
                if (_config == null)
                {
                    if (_fallback == null)
                    {
                        _fallback = ScriptableObject.CreateInstance<CoastPaletteConfig>();
                        _fallback.name = "CoastPalette (runtime fallback)";
                    }

                    return _fallback;
                }

                return _config;
            }
        }

        public static void Bind(CoastPaletteConfig config)
        {
            if (config != null)
                _config = config;
        }

        // —— Core ——
        public static Color SkyBlue => Active.skyBlue;
        public static Color SeaTeal => Active.seaTeal;
        public static Color RoadGrey => Active.roadGrey;
        public static Color TownCream => Active.townCream;
        public static Color AccentOrange => Active.accentOrange;
        public static Color CoinYellow => Active.coinYellow;

        // —— Derived (still inside the same 6-swatch family) ——
        public static Color SkyTop => SkyBlue;
        public static Color SkyHorizon => Color.Lerp(SkyBlue, Color.white, 0.35f);
        public static Color Fog => Color.Lerp(SkyBlue, TownCream, 0.45f);
        public static Color CloudLight => Color.Lerp(TownCream, Color.white, 0.55f);
        public static Color CloudShadow => Color.Lerp(SkyBlue, RoadGrey, 0.35f);

        public static Color Road => RoadGrey;
        public static Color RoadLine => CoinYellow;
        public static Color Curb => Color.Lerp(TownCream, RoadGrey, 0.25f);
        public static Color Sidewalk => Color.Lerp(TownCream, RoadGrey, 0.4f);

        public static Color BuildingWarm => TownCream;
        public static Color BuildingCool => Color.Lerp(TownCream, SkyBlue, 0.28f);
        public static Color Roof => AccentOrange;
        public static Color Window => Color.Lerp(SkyBlue, SeaTeal, 0.35f);

        public static Color Pole => Color.Lerp(RoadGrey, Color.black, 0.25f);
        public static Color Wire => Color.Lerp(RoadGrey, Color.black, 0.4f);

        public static Color SeaWall => Color.Lerp(RoadGrey, TownCream, 0.35f);
        public static Color Sand => Color.Lerp(TownCream, CoinYellow, 0.15f);
        public static Color Sea => SeaTeal;
        public static Color SeaFoam => Color.Lerp(SeaTeal, Color.white, 0.7f);

        public static Color Skin => Color.Lerp(TownCream, AccentOrange, 0.18f);
        public static Color Hair => Color.Lerp(RoadGrey, AccentOrange, 0.25f);
        public static Color Shirt => CoinYellow;
        public static Color Shorts => Color.Lerp(SeaTeal, RoadGrey, 0.35f);
        public static Color Backpack => Color.Lerp(RoadGrey, SeaTeal, 0.2f);
        public static Color Shoes => SeaTeal;
        public static Color BoardDeck => TownCream;
        public static Color WheelOrange => AccentOrange;

        public static Color ShadowCool => Color.Lerp(SkyBlue, Color.black, 0.45f);
        public static Color Sun => Color.Lerp(TownCream, CoinYellow, 0.2f);
        public static Color BlobShadow => new Color(ShadowCool.r, ShadowCool.g, ShadowCool.b, 0.42f);
    }
}
