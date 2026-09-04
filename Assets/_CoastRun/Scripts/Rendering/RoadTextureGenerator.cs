using UnityEngine;

namespace CoastRun
{
    /// Procedural, tileable flagstone for the promenade surface.
    ///
    /// The painted Road_Promenade.png is a perspective concept frame — it has a sea and a
    /// curb baked into its edges, so tiled ten times along a segment it turned into
    /// stripes of beach. A generated running-bond stone pattern tiles cleanly, stays
    /// in the cream/tan family of the palette, and reads as the cosy seaside pavement
    /// the reference boards ask for.
    public static class RoadTextureGenerator
    {
        private static Texture2D _flagstone;
        private static Texture2D _crosswalk;

        public static Texture2D Flagstone()
        {
            if (_flagstone != null)
                return _flagstone;

            const int size = 256;
            const int cols = 4;              // stones across one repeat
            const int rows = 6;              // stones along one repeat
            const int grout = 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color cream = new Color(0.93f, 0.87f, 0.74f);
            Color tan = new Color(0.84f, 0.74f, 0.58f);
            Color warm = new Color(0.88f, 0.78f, 0.62f);
            Color groutCol = new Color(0.70f, 0.62f, 0.50f);

            var rng = new System.Random(7);
            float cellW = size / (float)cols;
            float cellH = size / (float)rows;

            // Per-stone tint, chosen once per cell so each stone is flat-shaded.
            var tints = new Color[cols + 1, rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c <= cols; c++)
                {
                    double pick = rng.NextDouble();
                    Color baseCol = pick < 0.55 ? cream : (pick < 0.8 ? warm : tan);
                    float v = 0.96f + (float)rng.NextDouble() * 0.08f;
                    tints[c, r] = baseCol * v;
                }

            for (int y = 0; y < size; y++)
            {
                int r = Mathf.FloorToInt(y / cellH);
                float shift = (r % 2 == 1) ? cellW * 0.5f : 0f;          // running bond
                float yIn = y - r * cellH;
                bool groutY = yIn < grout;

                for (int x = 0; x < size; x++)
                {
                    float xs = x + shift;
                    int c = Mathf.FloorToInt(xs / cellW);
                    float xIn = xs - c * cellW;
                    bool groutX = xIn < grout;
                    c %= cols;

                    Color col;
                    if (groutX || groutY)
                        col = groutCol;
                    else
                    {
                        col = tints[c, r];
                        // Faint speckle so big flat stones don't band under bilinear.
                        float n = Mathf.PerlinNoise(x * 0.11f, y * 0.11f) - 0.5f;
                        col *= 1f + n * 0.05f;
                    }

                    col.a = 1f;
                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply(true);
            _flagstone = tex;
            return tex;
        }

        /// Zebra crosswalk strip — white bars on the stone, tiled across the road once.
        public static Texture2D Crosswalk()
        {
            if (_crosswalk != null)
                return _crosswalk;

            const int w = 256, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            var clear = new Color(1f, 1f, 1f, 0f);
            var bar = new Color(0.97f, 0.96f, 0.92f, 0.9f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, (x / 32) % 2 == 0 ? bar : clear);
            tex.Apply();
            _crosswalk = tex;
            return tex;
        }
    }
}
