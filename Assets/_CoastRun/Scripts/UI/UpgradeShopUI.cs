using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Upgrade shop — shown on stage-clear panel only (not during the run).
    public class UpgradeShopUI : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UI_FeedbackController feedback;

        private Text _speedLabel;
        private Text _coinLabel;
        private Text _magnetLabel;
        private GameObject _panel;
        private bool _built;

        public void Bind(UpgradeManager upgradeManager, CoinWallet coinWallet, UI_FeedbackController ui)
        {
            upgrades = upgradeManager;
            wallet = coinWallet;
            feedback = ui;
            // Do NOT build into the in-run bottom bar — wait for ShowInPanel.
            if (wallet != null)
                wallet.OnCoinsChanged += (_, __) => Refresh();
            if (upgrades != null)
                upgrades.OnUpgraded += (_, __) => Refresh();
        }

        public void ShowInPanel(Transform clearRoot)
        {
            EnsurePanel(clearRoot);
            if (_panel != null)
                _panel.SetActive(true);
            Refresh();
        }

        public void HidePanel()
        {
            if (_panel != null)
                _panel.SetActive(false);
        }

        private void EnsurePanel(Transform clearRoot)
        {
            if (_built && _panel != null)
            {
                if (clearRoot != null && _panel.transform.parent != clearRoot)
                    _panel.transform.SetParent(clearRoot.Find("UpgradeHost") ?? clearRoot, false);
                return;
            }

            Transform host = clearRoot != null
                ? (clearRoot.Find("UpgradeHost") ?? clearRoot)
                : transform;

            _panel = new GameObject("UpgradePanel", typeof(RectTransform));
            _panel.transform.SetParent(host, false);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _speedLabel = AddSlot(_panel.transform, "SPEED", UpgradeStat.MaxSpeed, 0);
            _coinLabel = AddSlot(_panel.transform, "COIN", UpgradeStat.CoinMultiplier, 1);
            _magnetLabel = AddSlot(_panel.transform, "MAGNET", UpgradeStat.MagnetRadius, 2);
            _built = true;
        }

        private Text AddSlot(Transform bar, string title, UpgradeStat stat, int index)
        {
            float x0 = index / 3f;
            float x1 = (index + 1) / 3f;

            var go = new GameObject(title + "Slot", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(bar, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, 0f);
            rt.anchorMax = new Vector2(x1, 1f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
            go.GetComponent<Button>().onClick.AddListener(() => TryUpgrade(stat));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.55f);
            irt.anchorMax = new Vector2(0.5f, 0.55f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(40f, 40f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("Icon_" + IconKey(stat)), 100f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var titleText = CoastHudLayout.MakeText(go.transform, "Title", title, 14, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            titleText.fontSize = 13;
            titleText.color = new Color(0.85f, 0.92f, 1f, 0.9f);

            return CoastHudLayout.MakeText(go.transform, "Label", "Lv.0", 16, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero);
        }

        private void TryUpgrade(UpgradeStat stat)
        {
            if (upgrades == null)
                return;

            int cost = upgrades.GetUpgradeCost(stat);
            if (cost < 0)
            {
                feedback?.ShowWatchMessage("MAXED", stat.ToString());
                return;
            }

            if (!upgrades.TryUpgrade(stat))
                feedback?.ShowWatchMessage("NEED COINS", cost + " for " + StatName(stat));
            Refresh();
        }

        private void Refresh()
        {
            if (upgrades == null || !_built)
                return;
            SetLabel(_speedLabel, UpgradeStat.MaxSpeed);
            SetLabel(_coinLabel, UpgradeStat.CoinMultiplier);
            SetLabel(_magnetLabel, UpgradeStat.MagnetRadius);
        }

        private void SetLabel(Text label, UpgradeStat stat)
        {
            if (label == null)
                return;
            int lv = upgrades.GetLevel(stat);
            int cost = upgrades.GetUpgradeCost(stat);
            string costText = cost < 0 ? "MAX" : cost + "c";
            label.text = "Lv." + lv + "  ·  " + costText;
        }

        private static string StatName(UpgradeStat stat)
        {
            switch (stat)
            {
                case UpgradeStat.MaxSpeed: return "SPEED";
                case UpgradeStat.CoinMultiplier: return "COIN";
                case UpgradeStat.MagnetRadius: return "MAGNET";
                default: return stat.ToString();
            }
        }

        private static string IconKey(UpgradeStat stat)
        {
            switch (stat)
            {
                case UpgradeStat.MaxSpeed: return "Speed";
                case UpgradeStat.CoinMultiplier: return "Coin";
                case UpgradeStat.MagnetRadius: return "Magnet";
                default: return "Coin";
            }
        }
    }
}
