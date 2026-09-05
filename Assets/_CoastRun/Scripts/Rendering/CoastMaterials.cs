using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Runtime URP materials — tracked so CoastPalette edits refresh live.
    public static class CoastMaterials
    {
        private static Shader _lit;
        private static Shader _unlit;
        private static Shader _toon;

        private struct Tracked
        {
            public Material Material;
            public Func<Color> Getter;
            public bool Unlit;
        }

        private static readonly List<Tracked> TrackedMats = new List<Tracked>(128);

        public static Shader LitShader
        {
            get
            {
                if (_lit == null)
                {
                    _lit = Shader.Find("Universal Render Pipeline/Lit")
                           ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                           ?? Shader.Find("Standard");
                }

                return _lit;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (_unlit == null)
                {
                    // The curved variant first, so sea, coins, wires and outlines bend with
                    // the road. Sky and clouds opt out via SetFlat.
                    _unlit = Shader.Find("CoastRun/UnlitCurved")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Sprites/Default");
                }

                return _unlit;
            }
        }

        public static Shader ToonShader
        {
            get
            {
                if (_toon == null)
                    _toon = Shader.Find("CoastRun/ToonLit");
                return _toon;
            }
        }

        public static Material CreateToon(Color color, Texture2D tex = null, float smoothness = 0.05f)
        {
            return CreateToon(color, null, tex, smoothness);
        }

        public static Material CreateToon(Color color, Func<Color> liveColor, Texture2D tex = null,
            float smoothness = 0.05f)
        {
            Material mat;
            if (ToonShader != null)
            {
                mat = new Material(ToonShader);
                ApplyColor(mat, color, false);
                if (mat.HasProperty("_ShadowColor"))
                    mat.SetColor("_ShadowColor", CoastPalette.ShadowCool);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", smoothness);
                if (tex != null)
                {
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", tex);
                    else if (mat.HasProperty("_MainTex"))
                        mat.SetTexture("_MainTex", tex);
                }
            }
            else
            {
                mat = new Material(LitShader);
                ApplyColor(mat, color, false);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", smoothness);
            }

            Track(mat, liveColor ?? (() => color), false);
            return mat;
        }

        public static Material CreateLit(Color color, float smoothness = 0.08f) =>
            CreateToon(color, null, null, smoothness);

        public static Material CreateLit(Func<Color> liveColor, float smoothness = 0.08f) =>
            CreateToon(liveColor(), liveColor, null, smoothness);

        public static Material CreateUnlit(Color color) => CreateUnlit(color, null);

        public static Material CreateUnlit(Color color, Func<Color> liveColor)
        {
            var mat = new Material(UnlitShader);
            ApplyColor(mat, color, true);
            Track(mat, liveColor ?? (() => color), true);
            return mat;
        }

        public static Material CreateUnlit(Func<Color> liveColor) =>
            CreateUnlit(liveColor(), liveColor);

        public static Material CreateTransparent(Color color) =>
            CreateTransparent(color, null);

        public static Material CreateTransparent(Color color, Func<Color> liveColor)
        {
            var mat = CreateUnlit(color, liveColor);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                // Sprites/Default / legacy unlit
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_ALPHABLEND_ON");
            }

            return mat;
        }

        private static Shader _particle;

        /// Particle systems must not use the curved-world shaders: ParticleSystemRenderer
        /// streams billboard vertices the bend maths misreads, and bursts turned into
        /// screen-sized blobs. Stock URP particle unlit, alpha-blended.
        public static Material CreateParticle(Color color)
        {
            if (_particle == null)
                _particle = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(_particle);
            ApplyColor(mat, color, true);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            return mat;
        }

        /// Pins a material in place while the rest of the world bends (sky, clouds, UI-ish
        /// billboards that must not sweep off screen on a hard curve).
        public static Material SetFlat(Material mat)
        {
            if (mat != null && mat.HasProperty("_CurveWeight"))
                mat.SetFloat("_CurveWeight", 0f);
            return mat;
        }

        /// Painted backdrops keep their own painted haze: no distance fog on top.
        public static Material SetNoFog(Material mat, float weight = 0f)
        {
            if (mat != null && mat.HasProperty("_FogWeight"))
                mat.SetFloat("_FogWeight", weight);
            return mat;
        }

        private static Shader _urpUnlit;

        /// Alpha-blended textured unlit for painted billboards (clouds, far town).
        /// Deliberately the stock URP Unlit, not the curved shader: through the curved
        /// shader's transparent path the quad's fully transparent texels still rendered
        /// as a pale slab (fog interaction), while URP Unlit draws the same texture
        /// cleanly. These billboards are pinned flat anyway, so nothing is lost.
        public static Material CreateTexturedTransparent(Texture2D tex, Color tint)
        {
            if (_urpUnlit == null)
                _urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (_urpUnlit == null)
            {
                var fallback = CreateTransparent(tint);
                if (tex != null && fallback.HasProperty("_BaseMap")) fallback.SetTexture("_BaseMap", tex);
                return fallback;
            }

            var mat = new Material(_urpUnlit);
            mat.SetColor("_BaseColor", tint);
            if (tex != null)
                mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }

        public static void RefreshTracked()
        {
            for (int i = TrackedMats.Count - 1; i >= 0; i--)
            {
                var t = TrackedMats[i];
                if (t.Material == null)
                {
                    TrackedMats.RemoveAt(i);
                    continue;
                }

                Color c = t.Getter != null ? t.Getter() : Color.magenta;
                ApplyColor(t.Material, c, t.Unlit);
                if (!t.Unlit && t.Material.HasProperty("_ShadowColor"))
                    t.Material.SetColor("_ShadowColor", CoastPalette.ShadowCool);
            }
        }

        public static void ApplyToonToHierarchy(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial == null)
                    continue;
                Color c = r.sharedMaterial.HasProperty("_BaseColor")
                    ? r.sharedMaterial.GetColor("_BaseColor")
                    : r.sharedMaterial.color;
                Texture t = r.sharedMaterial.HasProperty("_BaseMap")
                    ? r.sharedMaterial.GetTexture("_BaseMap")
                    : r.sharedMaterial.mainTexture;
                r.sharedMaterial = CreateToon(c, t as Texture2D);
            }
        }

        private static void Track(Material mat, Func<Color> getter, bool unlit)
        {
            if (mat == null || getter == null)
                return;
            TrackedMats.Add(new Tracked { Material = mat, Getter = getter, Unlit = unlit });
        }

        private static void ApplyColor(Material mat, Color color, bool unlit)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            else
                mat.color = color;
        }
    }
}
