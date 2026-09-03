using UnityEngine;

namespace CoastRun
{
    /// Temporary shop / debug keys until a real meta UI exists.
    /// U = MaxSpeed, I = CoinMult, O = Magnet. Costs from UpgradeManager.
    public class UpgradeShopHotkeys : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private UI_FeedbackController feedback;

        private System.Func<bool> _enabledWhen;

        public void Bind(UpgradeManager upgradeManager, UI_FeedbackController ui)
        {
            upgrades = upgradeManager;
            feedback = ui;
        }

        public void SetEnabledWhen(System.Func<bool> predicate) => _enabledWhen = predicate;

        private void Update()
        {
            if (upgrades == null)
                return;
            if (_enabledWhen != null && !_enabledWhen())
                return;

            if (Input.GetKeyDown(KeyCode.U))
                Try(UpgradeStat.MaxSpeed);
            if (Input.GetKeyDown(KeyCode.I))
                Try(UpgradeStat.CoinMultiplier);
            if (Input.GetKeyDown(KeyCode.O))
                Try(UpgradeStat.MagnetRadius);
        }

        private void Try(UpgradeStat stat)
        {
            int cost = upgrades.GetUpgradeCost(stat);
            if (cost < 0)
            {
                feedback?.ShowWatchMessage("MAXED", stat.ToString());
                return;
            }

            if (!upgrades.TryUpgrade(stat))
                feedback?.ShowWatchMessage("NEED COINS", cost + " for " + stat);
        }
    }
}
