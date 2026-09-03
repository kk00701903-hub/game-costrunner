using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum VisualTier
{
    Low,
    S26,
    High
}

/// Mobile quality presets for Galaxy S26 target (60 fps) and fallbacks.
public static class VisualQuality
{
    private const string PrefKey = "r347_visual_tier";
    private static bool _applied;

    public static VisualTier Current { get; private set; } = VisualTier.S26;

    public static void EnsureApplied()
    {
        if (_applied)
            return;

        int stored = PlayerPrefs.GetInt(PrefKey, (int)VisualTier.S26);
        Apply((VisualTier)Mathf.Clamp(stored, 0, 2));
    }

    public static void Apply(VisualTier tier)
    {
        Current = tier;
        _applied = true;
        PlayerPrefs.SetInt(PrefKey, (int)tier);
        PlayerPrefs.Save();

        switch (tier)
        {
            case VisualTier.Low:
                QualitySettings.SetQualityLevel(0, true);
                ApplyVolume(post: false, bloom: false);
                ApplyShadowDistance(28f);
                break;
            case VisualTier.High:
                QualitySettings.SetQualityLevel(5, true);
                ApplyVolume(post: true, bloom: true);
                ApplyShadowDistance(70f);
                break;
            default:
                QualitySettings.SetQualityLevel(2, true);
                ApplyVolume(post: true, bloom: true);
                ApplyShadowDistance(50f);
                break;
        }
    }

    private static void ApplyVolume(bool post, bool bloom)
    {
        Volume volume = Object.FindObjectOfType<Volume>();
        if (volume == null || volume.profile == null)
            return;

        if (volume.profile.TryGet(out Bloom b))
            b.active = bloom;

        if (volume.profile.TryGet(out ColorAdjustments c))
            c.active = post;

        if (volume.profile.TryGet(out Vignette v))
            v.active = post;
    }

    private static void ApplyShadowDistance(float metres)
    {
        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
            return;

        urp.shadowDistance = metres;
    }
}
