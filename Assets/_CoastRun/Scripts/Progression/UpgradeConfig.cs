using UnityEngine;

namespace CoastRun
{
    /// Balance knobs for the grind loop. Cost(L) = base * growth^L
    /// Tuned for ~3h first clear (ride + upgrades).
    [CreateAssetMenu(menuName = "Coast Run/Upgrade Config", fileName = "UpgradeConfig")]
    public class UpgradeConfig : ScriptableObject
    {
        [System.Serializable]
        public struct StatCurve
        {
            public UpgradeStat stat;
            public int maxLevel;
            public int baseCost;
            public float costGrowth;
            public float baseValue;
            public float perLevel;
            [Tooltip("If true, value = base * (1 + perLevel * level). Else base + perLevel * level.")]
            public bool multiplicative;
        }

        public StatCurve[] stats =
        {
            new StatCurve
            {
                stat = UpgradeStat.MaxSpeed,
                maxLevel = 80,
                baseCost = 30,
                costGrowth = 1.38f,
                baseValue = 12f,
                perLevel = 0.55f,
                multiplicative = false
            },
            new StatCurve
            {
                stat = UpgradeStat.CoinMultiplier,
                maxLevel = 60,
                baseCost = 45,
                costGrowth = 1.42f,
                baseValue = 1f,
                perLevel = 0.07f,
                multiplicative = true
            },
            new StatCurve
            {
                stat = UpgradeStat.MagnetRadius,
                maxLevel = 45,
                baseCost = 40,
                costGrowth = 1.4f,
                baseValue = 0f,
                perLevel = 0.32f,
                multiplicative = false
            }
        };

        [Header("Destination — 송전탑 (~3h)")]
        [Tooltip("Required MaxSpeed level to unlock tower approach.")]
        public int towerRequiredMaxSpeedLevel = ContentPace.TowerRequiredMaxSpeedLevel;
        [Tooltip("Path distance where the transmission tower waits.")]
        public float towerDistanceMetres = ContentPace.TowerDistanceMetres;
        public float towerApproachSpeedNeed = 18f;
    }
}
