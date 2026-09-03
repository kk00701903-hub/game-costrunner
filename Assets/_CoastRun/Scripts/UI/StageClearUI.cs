using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Stage clear / retry panel — upgrades live here (not on the in-run HUD).
    public class StageClearUI : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private UpgradeShopUI shop;

        private Canvas _canvas;
        private GameObject _root;
        private Text _title;
        private Text _body;
        private Button _continueBtn;
        private Button _retryBtn;
        private Action _onContinue;
        private Action _onRetry;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void Bind(UpgradeManager upgradeManager, CoinWallet coinWallet,
            UI_FeedbackController ui, UpgradeShopUI shopUi)
        {
            upgrades = upgradeManager;
            wallet = coinWallet;
            feedback = ui;
            shop = shopUi;
            EnsureBuilt();
            Hide();
        }

        public void Show(StageDef stage, bool chapterComplete, Action onContinue, Action onRetry)
        {
            EnsureBuilt();
            _onContinue = onContinue;
            _onRetry = onRetry;

            _title.text = "STAGE CLEAR";
            string ch = chapterComplete ? "\nCHAPTER " + stage.chapterIndex + " COMPLETE" : "";
            _body.text = "S" + stage.stageIndex.ToString("00") + "  " + stage.stageName + ch;

            if (_continueBtn != null)
            {
                var label = _continueBtn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = stage.stageIndex >= 20 ? "ENDING" : "NEXT STAGE";
            }

            _root.SetActive(true);
            shop?.ShowInPanel(_root.transform);
        }

        public void ShowFinal(StageDef stage, Action onContinue, Action onRetry)
        {
            Show(stage, true, onContinue, onRetry);
            _title.text = "ARRIVAL";
            _body.text = "S20  송전탑\n도착했어… 여기야.";
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
            shop?.HidePanel();
        }

        private void EnsureBuilt()
        {
            if (_root != null)
                return;

            _canvas = CoastUiCanvas.Create("StageClearCanvas", 200);
            _root = new GameObject("StageClearRoot", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _root.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.12f, 0.82f);

            _title = CoastHudLayout.MakeText(_root.transform, "Title", "STAGE CLEAR", 36,
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            _title.color = CoastHudLayout.AccentCyan;

            _body = CoastHudLayout.MakeText(_root.transform, "Body", "", 22,
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);

            // Upgrade strip sits mid-panel (was the in-run bottom bar).
            var shopHost = new GameObject("UpgradeHost", typeof(RectTransform));
            shopHost.transform.SetParent(_root.transform, false);
            var sht = shopHost.GetComponent<RectTransform>();
            sht.anchorMin = new Vector2(0.06f, 0.28f);
            sht.anchorMax = new Vector2(0.94f, 0.58f);
            sht.offsetMin = Vector2.zero;
            sht.offsetMax = Vector2.zero;

            _continueBtn = MakeButton(_root.transform, "Continue", new Vector2(0.52f, 0.08f),
                new Vector2(0.92f, 0.2f), "NEXT STAGE", () => _onContinue?.Invoke());
            _retryBtn = MakeButton(_root.transform, "Retry", new Vector2(0.08f, 0.08f),
                new Vector2(0.48f, 0.2f), "RETRY", () => _onRetry?.Invoke());
        }

        private static Button MakeButton(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            string label, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.12f, 0.28f, 0.42f, 0.95f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            CoastHudLayout.MakeText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }
    }
}
