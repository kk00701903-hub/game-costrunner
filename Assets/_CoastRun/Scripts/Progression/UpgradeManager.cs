using System;
using UnityEngine;

namespace CoastRun
{
    /// Stat levels + exponential upgrade costs. Persists via PlayerPrefs.
    public class UpgradeManager : MonoBehaviour
    {
        public const string PrefsPrefix = "CoastRun.Upgrade.";

        [SerializeField] private UpgradeConfig config;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UI_FeedbackController feedback;

        private readonly int[] _levels = new int[3];

        public event Action<UpgradeStat, int> OnUpgraded;

        public void Bind(UpgradeConfig upgradeConfig, CoinWallet coinWallet, UI_FeedbackController ui)
        {
            config = upgradeConfig;
            wallet = coinWallet;
            feedback = ui;
            Load();
        }

        private void Awake()
        {
            if (config == null)
                config = CoastConfigRegistry.UpgradeConfig;

            Load();
        }

        public int GetLevel(UpgradeStat stat) => _levels[(int)stat];

        public float GetValue(UpgradeStat stat)
        {
            var curve = FindCurve(stat);
            int level = GetLevel(stat);
            if (curve.multiplicative)
                return curve.baseValue * (1f + curve.perLevel * level);
            return curve.baseValue + curve.perLevel * level;
        }

        public float GetMaxSpeed() => GetValue(UpgradeStat.MaxSpeed);
        public float GetCoinMultiplier() => GetValue(UpgradeStat.CoinMultiplier);
        public float GetMagnetRadius() => GetValue(UpgradeStat.MagnetRadius);

        /// Cost to go from current level → level+1. Exponential grind curve.
        public int GetUpgradeCost(UpgradeStat stat)
        {
            var curve = FindCurve(stat);
            int level = GetLevel(stat);
            if (level >= curve.maxLevel)
                return -1;

            // cost = baseCost * growth^level
            double cost = curve.baseCost * Math.Pow(curve.costGrowth, level);
            return Mathf.Max(1, (int)Math.Round(cost));
        }

        public bool CanAfford(UpgradeStat stat)
        {
            int cost = GetUpgradeCost(stat);
            return cost > 0 && wallet != null && wallet.TotalCoins >= cost;
        }

        public bool TryUpgrade(UpgradeStat stat)
        {
            int cost = GetUpgradeCost(stat);
            if (cost < 0 || wallet == null || !wallet.TrySpend(cost))
                return false;

            int idx = (int)stat;
            _levels[idx]++;
            SaveStat(stat);
            OnUpgraded?.Invoke(stat, _levels[idx]);
            feedback?.ShowWatchLapPopup(stat, _levels[idx], GetValue(stat));
            return true;
        }

        public bool MeetsTowerRequirement()
        {
            if (config == null)
                return false;
            return GetLevel(UpgradeStat.MaxSpeed) >= config.towerRequiredMaxSpeedLevel
                   && GetMaxSpeed() >= config.towerApproachSpeedNeed * 0.95f;
        }

        public float TowerDistance => config != null ? config.towerDistanceMetres : ContentPace.TowerDistanceMetres;
        public int TowerRequiredMaxSpeedLevel =>
            config != null ? config.towerRequiredMaxSpeedLevel : 12;

        public void Load()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                var stat = (UpgradeStat)i;
                _levels[i] = PlayerPrefs.GetInt(PrefsPrefix + stat, 0);
            }
        }

        public void SaveAll()
        {
            for (int i = 0; i < _levels.Length; i++)
                SaveStat((UpgradeStat)i);
            PlayerPrefs.Save();
        }

        private void SaveStat(UpgradeStat stat)
        {
            PlayerPrefs.SetInt(PrefsPrefix + stat, _levels[(int)stat]);
            PlayerPrefs.Save();
        }

        private UpgradeConfig.StatCurve FindCurve(UpgradeStat stat)
        {
            if (config?.stats != null)
            {
                for (int i = 0; i < config.stats.Length; i++)
                {
                    if (config.stats[i].stat == stat)
                        return config.stats[i];
                }
            }

            return new UpgradeConfig.StatCurve
            {
                stat = stat,
                maxLevel = 30,
                baseCost = 30,
                costGrowth = 1.5f,
                baseValue = 1f,
                perLevel = 0.1f,
                multiplicative = false
            };
        }

#if UNITY_EDITOR
        [ContextMenu("Debug / Reset All Upgrades")]
        private void DebugReset()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] = 0;
                PlayerPrefs.DeleteKey(PrefsPrefix + (UpgradeStat)i);
            }

            PlayerPrefs.Save();
        }
#endif
    }
}
