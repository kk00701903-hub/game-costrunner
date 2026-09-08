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
            // 21차: 렌더러의 PostProcessData가 비어 있어 후처리가 20라운드 동안 한 번도 돌지 않았다(블룸·톤매핑·비네트·주스 채도 전부 무효).
            // 켜고 보니 예전 수치는 너무 어둡고 진해서, 굽힌 에셋 값 대신 코드 값을 항상 덮어쓴다(코드가 진실).
            var baked = Resources.Load<VolumeProfile>(ResourcePath);
            if (baked != null && baked.components != null && baked.components.Count > 0)
            {
                ApplyVpBaseSettings(baked);
                return baked;
            }

            return BuildVpBase();
        }

        public static VolumeProfile LoadOrBuildChapterProfile(int chapter1Based)
        {
            chapter1Based = Mathf.Clamp(chapter1Based, 1, 5);
            string res = "CoastRun/Config/Volumes/VP_CH" + chapter1Based;
            var baked = Resources.Load<VolumeProfile>(res);
            if (baked != null && baked.components != null && baked.components.Count > 0)
            {
                ApplyChapterGrade(baked, chapter1Based);   // 21차: 코드 값이 진실
                return baked;
            }

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
            tonemap.mode.Override(TonemappingMode.Neutral);   // 21차: ACES는 그림자가 눌려 어둡다 — 모바일 카툰 룩은 Neutral

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(1.15f);   // 14차-6: 캐릭터·코인이 뿌옇게 번지지 않게 문턱을 올리고 세기를 낮춘다
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet(out ColorAdjustments color))
                color = profile.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.Override(0.15f);   // 21차: 밝고 가벼운 카툰 룩 — 대비·채도는 살짝만
            color.contrast.Override(4f);
            color.saturation.Override(10f);

            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.10f);
            vignette.smoothness.Override(0.6f);

            if (!profile.TryGet(out ShadowsMidtonesHighlights smh))
                smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.active = true;
            smh.shadows.Override(new Vector4(0.96f, 0.98f, 1.06f, 0f));   // 그림자만 아주 살짝 푸르게
            smh.midtones.Override(new Vector4(1f, 1f, 1.01f, 0f));
            smh.highlights.Override(new Vector4(1.01f, 1f, 0.99f, 0f));
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
                case 1: // noon — 14차: 목표 이미지(한낮, 채도 높고 살짝 따뜻한 햇빛)
                    filter = new Color(1f, 0.99f, 0.96f);
                    sat = 12f;
                    contrast = 6f;
                    exposure = 0.08f;
                    break;
                case 2: // afternoon
                    filter = new Color(1f, 0.98f, 0.95f);
                    sat = 10f;
                    contrast = 5f;
                    exposure = 0.06f;
                    break;
                case 3: // low sun — sat down
                    filter = new Color(1f, 0.95f, 0.90f);
                    sat = 8f;
                    contrast = 4f;
                    exposure = 0.03f;
                    break;
                case 4: // golden
                    filter = new Color(1f, 0.88f, 0.72f);
                    sat = 6f;
                    contrast = 3f;
                    exposure = 0f;
                    break;
                default: // blue hour — deep violet, lower contrast
                    filter = new Color(0.78f, 0.82f, 1f);
                    sat = 0f;
                    contrast = 0f;
                    exposure = -0.06f;
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
