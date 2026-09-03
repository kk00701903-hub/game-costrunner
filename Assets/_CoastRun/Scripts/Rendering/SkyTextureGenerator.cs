using UnityEngine;

namespace CoastRun
{
    /// Runtime painterly summer sky for StyleBible reference match.
    public static class SkyTextureGenerator
    {
        public static Texture2D CreatePortraitSky(int w = 512, int h = 896)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var rng = new System.Random(7);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                // t=0 bottom (horizon, bright) → t=1 top (zenith, deep).
                Color zenith = CoastPalette.SkyTop * 0.72f;
                zenith.a = 1f;
                Color horizon = Color.Lerp(CoastPalette.SkyHorizon, Color.white, 0.28f);
                Color band = Color.Lerp(horizon, zenith, Mathf.Pow(t, 0.85f));
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    Color c = band;

                    // Soft cloud puffs only in upper-mid sky
                    if (t > 0.35f)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            float cx = 0.12f + (i * 0.09f) % 0.85f;
                            float cy = 0.48f + (i % 4) * 0.1f;
                            float sx = 0.16f + (i % 3) * 0.05f;
                            float sy = 0.07f + (i % 2) * 0.03f;
                            float d = Mathf.Pow((nx - cx) / sx, 2f) + Mathf.Pow((t - cy) / sy, 2f);
                            float puff = Mathf.Exp(-d * 1.8f);
                            Color cloud = Color.Lerp(CoastPalette.CloudShadow, CoastPalette.CloudLight, puff);
                            c = Color.Lerp(c, cloud, puff * 0.75f);
                        }
                    }

                    float n = ((float)rng.NextDouble() - 0.5f) * 0.012f;
                    c.r = Mathf.Clamp01(c.r + n);
                    c.g = Mathf.Clamp01(c.g + n);
                    c.b = Mathf.Clamp01(c.b + n);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
