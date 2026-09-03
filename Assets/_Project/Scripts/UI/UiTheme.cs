using UnityEngine;

/// Cinematic runner HUD — dark brown pills, white type, gold accents. Frame = Galaxy S26 portrait.
public static class UiTheme
{
    public static Vector2 Reference => MobileDisplay.Reference;

    public static readonly Color Ink = new Color(0.98f, 0.98f, 0.96f, 1f);
    public static readonly Color InkMuted = new Color(0.82f, 0.80f, 0.76f, 0.92f);
    public static readonly Color Gold = new Color(1f, 0.84f, 0.32f, 1f);
    public static readonly Color GoldHot = new Color(1f, 0.92f, 0.55f, 1f);
    public static readonly Color Danger = new Color(0.92f, 0.22f, 0.18f, 1f);
    public static readonly Color PillBg = new Color(0.14f, 0.11f, 0.09f, 0.72f);
    public static readonly Color PillBgSolid = new Color(0.12f, 0.10f, 0.08f, 0.88f);
    public static readonly Color Plate = PillBg;
    public static readonly Color PlateHard = PillBgSolid;
    public static readonly Color Dim = new Color(0.02f, 0.02f, 0.03f, 0.82f);
    public static readonly Color DeckOk = new Color(0.55f, 0.82f, 0.95f, 1f);
    public static readonly Color DeckWarn = new Color(1f, 0.55f, 0.22f, 1f);
    public static readonly Color DeckCrit = new Color(0.95f, 0.25f, 0.22f, 1f);
    public static readonly Color SlotIdle = new Color(0.14f, 0.12f, 0.10f, 0.78f);
    public static readonly Color SlotReady = new Color(0.20f, 0.38f, 0.48f, 0.88f);

    public static float SafeTop => MobileDisplay.DesignSafeTop;
    public static float SafeBottom => MobileDisplay.DesignSafeBottom;
    public static float SafeSide => MobileDisplay.DesignSafeSide;

    public const float HudTopInset = 14f;
    public const float PillHeight = 52f;
    public const float PauseSize = 52f;

    public const int PillLabelSize = 22;
    public const int ScoreSize = 24;
    public const int DistanceSize = 22;
    public const int MetaSize = 18;
    public const int BodySize = 28;
    public const int BannerSize = 40;
    public const int HintSize = 30;
    public const int CtaSize = 30;
    public const int SubtitleSize = 24;
}
