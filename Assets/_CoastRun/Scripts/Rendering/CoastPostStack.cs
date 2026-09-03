using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// Global post stack for Coast Run. VP_Base + chapter grade overlays VP_CH1..CH5.
    public static class CoastPostStack
    {
        public const string ResourcePath = "CoastRun/Config/Volumes/VP_Base";
        public const string EditorPath = "Assets/_CoastRun/Config/Volumes/VP_Base.asset";

        public static Volume EnsureGlobalVolume()
        {
            var existing = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].isGlobal && existing[i].name == "CoastVolume_VP_Base")
                    return existing[i];
            }

            var go = new GameObject("CoastVolume_VP_Base");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.profile = LoadOrBuildVpBase();
            return volume;
        }

        public static VolumeProfile LoadOrBuildVpBase()
        {
            var baked = Resources.Load<VolumeProfile>(ResourcePath);
            if (baked != null && baked.components != null && baked.components.Count > 0)
                return baked;

            return BuildVpBase();
        }

        public static VolumeProfile LoadOrBuildChapterProfile(int chapter1Based)
        {
            chapter1Based = Mathf.Clamp(chapter1Based, 1, 5);
            string res = "CoastRun/Config/Volumes/VP_CH" + chapter1Based;
            var baked = Resources.Load<VolumeProfile>(res);
            if (baked != null && baked.components != null && baked.components.Count > 0)
                return baked;

            return BuildChapterProfile(chapter1Based);
        }

        public static VolumeProfile BuildVpBase()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_Base";
            ApplyVpBaseSettings(profile);
            return profile;
        }

        /// Chapter grade — ColorAdjustments temperature (via colorFilter) + saturation only.
        public static VolumeProfile BuildChapterProfile(int chapter1Based)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_CH" + chapter1Based;
            ApplyChapterGrade(profile, chapter1Based);
            return profile;
        }

        public static void ApplyVpBaseSettings(VolumeProfile profile)
        {
            if (profile == null)
                return;

            if (!profile.TryGet(out Tonemapping tonemap))
                tonemap = profile.Add<Tonemapping>(true);
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.ACES);

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.7f);

            if (!profile.TryGet(out ColorAdjustments color))
                color = profile.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.Override(0.2f);
            color.contrast.Override(12f);
            color.saturation.Override(18f);

            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);

            if (!profile.TryGet(out ShadowsMidtonesHighlights smh))
                smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.active = true;
            smh.shadows.Override(new Vector4(0.72f, 0.84f, 1.18f, 0f));
            smh.midtones.Override(new Vector4(1f, 1f, 1.02f, 0f));
            smh.highlights.Override(new Vector4(1.02f, 1.01f, 0.98f, 0f));
        }

        public static void ApplyChapterGrade(VolumeProfile profile, int chapter1Based)
        {
            if (profile == null)
                return;

            if (!profile.TryGet(out ColorAdjustments color))
                color = profile.Add<ColorAdjustments>(true);
            color.active = true;

            Color filter;
            float sat;
            float contrast;
            float exposure;
            switch (chapter1Based)
            {
                case 1: // noon — high sat, cool
                    filter = new Color(0.92f, 0.96f, 1f);
                    sat = 22f;
                    contrast = 14f;
                    exposure = 0.22f;
                    break;
                case 2: // afternoon
                    filter = new Color(1f, 0.98f, 0.95f);
                    sat = 18f;
                    contrast = 12f;
                    exposure = 0.18f;
                    break;
                case 3: // low sun — sat down
                    filter = new Color(1f, 0.94f, 0.88f);
                    sat = 12f;
                    contrast = 11f;
                    exposure = 0.12f;
                    break;
                case 4: // golden
                    filter = new Color(1f, 0.88f, 0.72f);
                    sat = 16f;
                    contrast = 13f;
                    exposure = 0.08f;
                    break;
                default: // blue hour — deep violet, lower contrast
                    filter = new Color(0.78f, 0.82f, 1f);
                    sat = 6f;
                    contrast = 8f;
                    exposure = -0.05f;
                    break;
            }

            color.colorFilter.Override(filter);
            color.saturation.Override(sat);
            color.contrast.Override(contrast);
            color.postExposure.Override(exposure);
            color.hueShift.Override(0f);
        }
    }
}
