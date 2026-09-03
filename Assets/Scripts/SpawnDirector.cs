using UnityEngine;

/// Quiet difficulty correction. Reads how badly the last half minute went and
/// leans on the spawn tables accordingly. The player is never told this exists,
/// and nothing in the UI refers to it.
public static class SpawnDirector
{
    private const float Window = 30f;

    public static float Tension
    {
        get
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return 0.5f;

            PlayerVitals vitals = gm.PlayerVitals;
            float hpTerm = vitals != null && vitals.MaxHp > 0
                ? 1f - (float)vitals.Hp / vitals.MaxHp
                : 0f;

            CollapseLine collapse = CollapseLine.Instance;
            float gapTerm = collapse != null && collapse.StartGap > 0f
                ? 1f - Mathf.Clamp01(collapse.Gap / collapse.StartGap)
                : 0f;

            float hitTerm = Mathf.Clamp01(gm.RecentHits(Window) / 3f);

            return Mathf.Clamp01(0.4f * hpTerm + 0.3f * gapTerm + 0.3f * hitTerm);
        }
    }

    /// A run that is going well gets a denser road.
    public static float ObstacleScale
    {
        get
        {
            float t = Tension;
            if (t < 0.3f)
                return 1.25f;

            return t > 0.7f ? 0.8f : 1f;
        }
    }

    /// Deck tape only enters the pool when the deck is already cracked, and the
    /// chance of it appearing goes up the worse things get.
    public static float HealChance
    {
        get
        {
            GameManager gm = GameManager.Instance;
            PlayerVitals vitals = gm != null ? gm.PlayerVitals : null;
            if (vitals == null || vitals.Hp >= vitals.MaxHp)
                return 0f;

            float t = Tension;
            if (t < 0.3f)
                return 0f;

            return t > 0.7f ? 0.6f : 0.3f;
        }
    }

    /// At high tension one lane is always clear, so a bad patch has a way out.
    public static bool GuaranteeSafeLane => Tension > 0.7f;

    /// Riding the dangerous line pays more when the run is comfortable.
    public static float RiskBonus => Tension < 0.3f ? 0.5f : 0f;
}
