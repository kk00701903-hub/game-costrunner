using UnityEngine;

namespace CoastRun
{
    /// Static look tables for prop/obstacle bias. Not driven by looping path bands.
    public static class SeasonPalettes
    {
        public struct Snapshot
        {
            public SeasonKind season;
            public Color sky;
            public Color fog;
            public Color sun;
            public Color roadTint;
            public Color foliage;
            public float sunIntensity;
            public float sunPitch;
            public float fogDensity;
        }

        // SeasonAt(float pathMetres) removed — distance no longer selects a season.
        // Season is a per-chapter art theme now: StageManager.ChapterAsSeason(chapter).

        public static Snapshot Get(SeasonKind season)
        {
            switch (season)
            {
                case SeasonKind.Spring:
                    return new Snapshot
                    {
                        season = season,
                        sky = new Color(0.55f, 0.78f, 0.95f),
                        fog = new Color(0.9f, 0.92f, 0.95f),
                        sun = new Color(1f, 0.97f, 0.9f),
                        roadTint = new Color(0.88f, 0.86f, 0.8f),
                        foliage = new Color(0.55f, 0.82f, 0.45f),
                        sunIntensity = 1.2f,
                        sunPitch = 52f,
                        fogDensity = 0.0022f
                    };
                case SeasonKind.Autumn:
                    return new Snapshot
                    {
                        season = season,
                        sky = new Color(0.75f, 0.55f, 0.4f),
                        fog = new Color(0.85f, 0.7f, 0.55f),
                        sun = new Color(1f, 0.7f, 0.4f),
                        roadTint = new Color(0.78f, 0.68f, 0.55f),
                        foliage = new Color(0.85f, 0.42f, 0.18f),
                        sunIntensity = 1.05f,
                        sunPitch = 38f,
                        fogDensity = 0.0035f
                    };
                case SeasonKind.Winter:
                    return new Snapshot
                    {
                        season = season,
                        sky = new Color(0.72f, 0.8f, 0.88f),
                        fog = new Color(0.88f, 0.9f, 0.94f),
                        sun = new Color(0.9f, 0.92f, 0.98f),
                        roadTint = new Color(0.82f, 0.84f, 0.88f),
                        foliage = new Color(0.55f, 0.62f, 0.55f),
                        sunIntensity = 0.95f,
                        sunPitch = 28f,
                        fogDensity = 0.0042f
                    };
                default: // Summer
                    return new Snapshot
                    {
                        season = SeasonKind.Summer,
                        sky = CoastPalette.SkyTop,
                        fog = CoastPalette.Fog,
                        sun = CoastPalette.Sun,
                        roadTint = CoastPalette.Road,
                        foliage = new Color(0.25f, 0.55f, 0.28f),
                        sunIntensity = 1.35f,
                        sunPitch = 48f,
                        fogDensity = 0.0028f
                    };
            }
        }
    }
}
