using UnityEngine;

/// Galaxy S26 portrait frame: 1080×2340, 19.5:9.
/// Editor Game View uses the same numbers so WYSIWYG matches device builds.
public static class MobileDisplay
{
    public const string DeviceName = "Galaxy S26";
    public const int Width = 1080;
    public const int Height = 2340;
    public static readonly Vector2 Reference = new Vector2(Width, Height);

    /// 19.5:9 — taller than legacy 9:16 (2340 vs 1920 at 1080 wide).
    public const float AspectWidth = 9f;
    public const float AspectHeight = 19.5f;

    /// Design safe area at reference px (status + centered punch-hole, gesture bar).
    public const float DesignSafeTop = 88f;
    public const float DesignSafeBottom = 56f;
    public const float DesignSafeSide = 24f;

    public static float ScaleX => Screen.width > 0 ? Screen.width / Reference.x : 1f;
    public static float ScaleY => Screen.height > 0 ? Screen.height / Reference.y : 1f;

    /// left, bottom, right, top in screen pixels.
    public static Vector4 SafeInsetsPx()
    {
        Rect sa = Screen.safeArea;
        if (sa.width > 0f && sa.height > 0f && !IsNearlyFullScreen(sa))
        {
            return new Vector4(
                sa.xMin,
                sa.yMin,
                Screen.width - sa.xMax,
                Screen.height - sa.yMax);
        }

        return new Vector4(
            DesignSafeSide * ScaleX,
            DesignSafeBottom * ScaleY,
            DesignSafeSide * ScaleX,
            DesignSafeTop * ScaleY);
    }

    public static void ApplySafeArea(RectTransform stretchRoot)
    {
        if (stretchRoot == null)
            return;

        Vector4 inset = SafeInsetsPx();
        stretchRoot.anchorMin = Vector2.zero;
        stretchRoot.anchorMax = Vector2.one;
        stretchRoot.offsetMin = new Vector2(inset.x, inset.y);
        stretchRoot.offsetMax = new Vector2(-inset.z, -inset.w);
    }

    /// Screen-space portrait frame in pixels (for overlay canvases).
    public static Rect PortraitRectPx()
    {
        Rect n = PortraitViewport.NormalizedRect;
        return new Rect(
            n.x * Screen.width,
            n.y * Screen.height,
            n.width * Screen.width,
            n.height * Screen.height);
    }

    private static bool IsNearlyFullScreen(Rect sa)
    {
        const float eps = 2f;
        return sa.xMin <= eps &&
               sa.yMin <= eps &&
               Mathf.Abs(sa.width - Screen.width) <= eps &&
               Mathf.Abs(sa.height - Screen.height) <= eps;
    }
}
