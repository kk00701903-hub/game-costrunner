using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Spawns the global post stack and applies mobile camera clip from CameraProfile.
public static class VisualBootstrap
{
    private const string VolumeResourcePath = "347/RunVolumeProfile";

    public static void EnsureRunStack(Camera camera)
    {
        ApplyCameraClip(camera);
        EnsureGlobalVolume();
        VisualQuality.EnsureApplied();
    }

    private static void ApplyCameraClip(Camera camera)
    {
        if (camera == null)
            return;

        CameraProfile profile = CameraProfile.Active;
        if (profile == null)
            return;

        camera.nearClipPlane = profile.nearClip;
        camera.farClipPlane = profile.farClip;
    }

    private static void EnsureGlobalVolume()
    {
        Volume existing = Object.FindObjectOfType<Volume>();
        if (existing != null && existing.isGlobal)
            return;

        GameObject go = new GameObject("RunVolume");
        Object.DontDestroyOnLoad(go);

        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.profile = LoadOrBuildProfile();
    }

    private static VolumeProfile LoadOrBuildProfile()
    {
        VolumeProfile baked = Resources.Load<VolumeProfile>(VolumeResourcePath);
        if (baked != null && baked.components != null && baked.components.Count > 0)
            return baked;

        return BuildRuntimeProfile();
    }

    private static VolumeProfile BuildRuntimeProfile()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.saturation.Override(8f);
        color.contrast.Override(6f);

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.05f);

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.45f);

        if (!profile.TryGet(out Tonemapping tonemap))
            tonemap = profile.Add<Tonemapping>(true);
        tonemap.active = true;
        tonemap.mode.Override(TonemappingMode.ACES);

        return profile;
    }
}
