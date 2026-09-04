using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Procedural UI textures when PNG art is missing from Resources.
    public static class CoastUiArt
    {
        private static Texture2D _titleBg;
        private static Texture2D _button;
        private static Texture2D _coinIcon;
        private static Texture2D _speedIcon;
        private static Texture2D _magnetIcon;
        private static Texture2D _watchFrame;
        private static Texture2D _towerIcon;
        private static Texture2D _himIcon;
        private static Texture2D _memoryPanel;

        public static Texture2D TitleBackground =>
            LoadOrCreate("UI_TitleBackground", ref _titleBg, MakeTitleBackground);

        public static Texture2D ButtonNormal =>
            LoadOrCreate("UI_ButtonPrimary", ref _button, MakeButton);

        public static Texture2D CoinIcon =>
            LoadOrCreate("Icon_Coin", ref _coinIcon, () => MakeIcon(new Color(0.95f, 0.78f, 0.22f)));

        public static Texture2D SpeedIcon =>
            LoadOrCreate("Icon_Speed", ref _speedIcon, () => MakeIcon(new Color(0.35f, 0.85f, 0.92f)));

        public static Texture2D MagnetIcon =>
            LoadOrCreate("Icon_Magnet", ref _magnetIcon, () => MakeIcon(new Color(0.72f, 0.45f, 0.95f)));

        public static Texture2D TowerIcon =>
            LoadOrCreate("Icon_Tower", ref _towerIcon, () => MakeIcon(new Color(0.55f, 0.58f, 0.62f)));

        public static Texture2D HimIcon =>
            LoadOrCreate("Icon_Him", ref _himIcon, () => MakeIcon(new Color(0.95f, 0.55f, 0.45f)));

        public static Texture2D MemoryPanel =>
            LoadOrCreate("UI_Panel_Memory", ref _memoryPanel, MakeMemoryPanel);

        public static Texture2D WatchFrame =>
            LoadOrCreate("Watch_Frame", ref _watchFrame, MakeWatchFrame);

        public static Texture2D LoadOrFallback(string resourceName, System.Func<Texture2D> fallback)
        {
            var tex = ArtAssets.LoadTexture(resourceName);
            return tex != null ? tex : fallback();
        }

        private static Texture2D LoadOrCreate(string name, ref Texture2D cache, System.Func<Texture2D> factory)
        {
            if (cache != null)
                return cache;

            cache = ArtAssets.LoadTexture(name);
            if (cache == null)
                cache = factory();
            return cache;
        }

        private static Texture2D MakeTitleBackground()
        {
            const int w = 360;
            const int h = 640;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                Color sky = Color.Lerp(CoastPalette.SkyTop, CoastPalette.SkyHorizon, Mathf.Pow(v, 0.65f));
                if (v < 0.55f)
                {
                    float cloud = Mathf.PerlinNoise(v * 3f, 0.2f) * 0.15f;
                    sky = Color.Lerp(sky, CoastPalette.CloudLight, cloud);
                }
                else
                {
                    float coast = (v - 0.55f) / 0.45f;
                    sky = Color.Lerp(sky, CoastPalette.Road, coast * 0.35f);
                }

                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, sky);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D MakeButton()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var fill = new Color(0.22f, 0.62f, 0.58f, 1f);
            var edge = new Color(0.95f, 0.92f, 0.55f, 1f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool border = x < 3 || y < 3 || x >= s - 3 || y >= s - 3;
                    tex.SetPixel(x, y, border ? edge : fill);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D MakeIcon(Color tint)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    tex.SetPixel(x, y, d < 22f ? tint : clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMemoryPanel()
        {
            const int w = 128;
            const int h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(0.04f, 0.08f, 0.12f, 0.88f);
            var edge = new Color(0.25f, 0.75f, 0.85f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 4 || y < 4 || x >= w - 4 || y >= h - 4;
                    bool corner = (x < 12 || x >= w - 12) && (y < 12 || y >= h - 12);
                    tex.SetPixel(x, y, border ? edge : (corner ? fill * 0.9f : fill));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D MakeWatchFrame()
        {
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var ring = new Color(0.2f, 0.75f, 0.85f, 0.95f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(s * 0.5f, s * 0.5f));
                    tex.SetPixel(x, y, d > 52f && d < 60f ? ring : clear);
                }
            }

            tex.Apply();
            return tex;
        }

        public static Sprite AsSprite(Texture2D tex, float ppu = 100f)
        {
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> RoundedCache =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        /// 9-sliced rounded rectangle for the Subway-Surfers-style pills and buttons: white
        /// fill with an optional border, tinted through Image.color at use. Cached per shape.
        public static Sprite RoundedRect(int radius, int border = 0)
        {
            radius = Mathf.Clamp(radius, 2, 60);
            border = Mathf.Clamp(border, 0, radius - 1);
            string key = radius + ":" + border;
            if (RoundedCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            int s = radius * 2 + 4;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color(1f, 1f, 1f, 0f);
            var fill = Color.white;
            var edge = new Color(1f, 1f, 1f, 0.35f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Distance from the nearest corner centre, only inside corner squares.
                    float cx = x < radius ? radius - 0.5f : (x >= s - radius ? s - radius - 0.5f : x);
                    float cy = y < radius ? radius - 0.5f : (y >= s - radius ? s - radius - 0.5f : y);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);          // anti-aliased edge
                    bool inBorder = border > 0 && d > radius - border - 0.5f;
                    Color c = inBorder ? edge : fill;
                    c.a *= a;
                    tex.SetPixel(x, y, a <= 0f ? clear : c);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            RoundedCache[key] = sprite;
            return sprite;
        }

        /// Filled rounded panel helper used by the run HUD and title screen.
        public static Image Panel(Transform parent, string name, Color color, int radius = 18)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = RoundedRect(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
