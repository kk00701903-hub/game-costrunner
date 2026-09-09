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

        // 24차-6(점검 2-1): 스폰마다 만든 머티리얼을 강참조 리스트가 영원히 붙잡아 씬 재로드 후에도 해제되지 않았다
        // (타일당 ~50개 + 젤리 스테이지당 1000개 이상). 라이브 팔레트 갱신은 에디터 OnValidate에서만 쓰므로
        // 에디터에서만, 그것도 약참조로 추적한다. 빌드에선 추적 자체를 하지 않는다.
        private class Tracked
        {
            public WeakReference<Material> Ref;
            public Func<Color> Getter;
            public bool Unlit;
            public bool CustomShadow;   // 25차-1: SetShadow 로 지정한 그림자색은 팔레트 갱신 때 덮어쓰지 않는다
        }

#if UNITY_EDITOR
        private static readonly List<Tracked> TrackedMats = new List<Tracked>(128);
#endif

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
            // 14차: 텍스처 없는 파티클은 네모로 찍힌다(코인 터짐이 주황 사각형이던 원인) → 부드러운 원판.
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", BlobShadow.SoftDisc());
            else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", BlobShadow.SoftDisc());
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

        /// Alpha-blended, fog-free painted backdrop (the Hallasan far layer). Stock URP
        /// Unlit always applies distance fog, and at 150 m — right at fog end — that
        /// bleached the whole layer into one pale band on the horizon. The curved
        /// shader exposes _FogWeight, so it can draw the painting exactly as painted.
        public static Material CreateTexturedTransparentNoFog(Texture2D tex, Color tint)
        {
            var shader = Shader.Find("CoastRun/UnlitCurved");
            if (shader == null)
                return CreateTexturedTransparent(tex, tint);
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", tint);
            if (tex != null)
                mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_CurveWeight", 0f);
            mat.SetFloat("_FogWeight", 0f);
            mat.renderQueue = 3000;
            return mat;
        }

        /// 12차: 도로와 같이 휘는 투명 텍스처 언릿 — 블롭 그림자·아이템 광원·장애물 경고 링처럼
        /// 바닥/소품에 붙어 다니는 것들. (구름·원경은 그대로 CreateTexturedTransparent: 곧게.)
        public static Material CreateTexturedTransparentCurved(Texture2D tex, Color tint, bool additive = false)
        {
            var shader = Shader.Find("CoastRun/UnlitCurved");
            if (shader == null)
                return CreateTexturedTransparent(tex, tint);
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", tint);
            if (tex != null)
                mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_CurveWeight", 1f);
            mat.SetFloat("_FogWeight", additive ? 0f : 1f);
            mat.renderQueue = 3000;
            return mat;
        }

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
#if UNITY_EDITOR
            for (int i = TrackedMats.Count - 1; i >= 0; i--)
            {
                var t = TrackedMats[i];
                if (t.Ref == null || !t.Ref.TryGetTarget(out var mat) || mat == null)
                {
                    TrackedMats.RemoveAt(i);
                    continue;
                }

                Color c = t.Getter != null ? t.Getter() : Color.magenta;
                ApplyColor(mat, c, t.Unlit);
                if (!t.Unlit && !t.CustomShadow && mat.HasProperty("_ShadowColor"))
                    mat.SetColor("_ShadowColor", CoastPalette.ShadowCool);
            }
#endif
        }

        /// 에디터 진단용: 현재 추적 중인 머티리얼 수.
        public static int TrackedCount
        {
            get
            {
#if UNITY_EDITOR
                return TrackedMats.Count;
#else
                return 0;
#endif
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

        /// 25차-1: 주인공 피부처럼 따뜻한 그림자가 필요한 머티리얼. 에디터 팔레트 갱신(RefreshTracked)이
        /// 이 값을 ShadowCool 로 되돌려 왼팔이 파랗게 보이던 원인 → 지정한 색을 기억해 둔다.
        public static Material SetShadow(Material mat, Color shadow, float? threshold = null)
        {
            if (mat == null) return null;
            if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", shadow);
            if (threshold.HasValue && mat.HasProperty("_ShadowThreshold")) mat.SetFloat("_ShadowThreshold", threshold.Value);
#if UNITY_EDITOR
            for (int i = TrackedMats.Count - 1; i >= 0; i--)
                if (TrackedMats[i].Ref != null && TrackedMats[i].Ref.TryGetTarget(out var m) && m == mat) { TrackedMats[i].CustomShadow = true; break; }
#endif
            return mat;
        }

        private static void Track(Material mat, Func<Color> getter, bool unlit)
        {
#if UNITY_EDITOR
            if (mat == null || getter == null)
                return;
            // 죽은 항목이 쌓이지 않게 512개마다 한 번 정리
            if ((TrackedMats.Count & 511) == 511)
                for (int i = TrackedMats.Count - 1; i >= 0; i--)
                    if (TrackedMats[i].Ref == null || !TrackedMats[i].Ref.TryGetTarget(out var m) || m == null)
                        TrackedMats.RemoveAt(i);
            TrackedMats.Add(new Tracked { Ref = new WeakReference<Material>(mat), Getter = getter, Unlit = unlit });
#endif
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
