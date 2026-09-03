using UnityEngine;

/// Visible growth with zero combat power. Performance stays capped at +25%.
public class PrestigeVisuals : MonoBehaviour
{
    public static PrestigeVisuals Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public int PartFxTier(PartSlot slot)
    {
        int lv = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.Level(slot) : 0;
        if (lv >= 15)
            return 3;
        if (lv >= 10)
            return 2;
        if (lv >= 5)
            return 1;
        return 0;
    }

    public int DistanceTrailTier()
    {
        long metres = SaveSystem.Instance != null ? SaveSystem.Instance.Data.totalDistanceMetres : 0;
        if (metres >= 500000)
            return 5;
        if (metres >= 200000)
            return 4;
        if (metres >= 80000)
            return 3;
        if (metres >= 20000)
            return 2;
        if (metres >= 5000)
            return 1;
        return 0;
    }

    public int CodexLogStyleTier()
    {
        if (Codex.Instance == null)
            return 0;
        float t = Codex.Instance.TagCompletion;
        if (t >= 1f)
            return 4;
        if (t >= 0.75f)
            return 3;
        if (t >= 0.5f)
            return 2;
        if (t >= 0.25f)
            return 1;
        return 0;
    }

    public int MenuCollapseTier()
    {
        int runs = SaveSystem.Instance != null ? SaveSystem.Instance.Data.runCount : 0;
        if (runs >= 347)
            return 5;
        if (runs >= 100)
            return 4;
        if (runs >= 50)
            return 3;
        if (runs >= 15)
            return 2;
        if (runs >= 3)
            return 1;
        return 0;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
