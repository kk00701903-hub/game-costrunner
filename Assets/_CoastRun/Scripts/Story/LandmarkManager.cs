using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Fires story monologues at distance landmarks along the run to the tower.
    public class LandmarkManager : MonoBehaviour
    {
        [SerializeField] private StoryConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private UI_FinalDestinationController destinationUi;

        private bool[] _fired;
        private Canvas _canvas;
        private RectTransform _panel;
        private Text _title;
        private Text _body;
        private Coroutine _hideRoutine;

        public void Bind(StoryConfig storyConfig, PlayerController playerController,
            UpgradeManager upgradeManager, UI_FeedbackController ui,
            UI_FinalDestinationController destUi = null)
        {
            config = storyConfig;
            player = playerController;
            upgrades = upgradeManager;
            feedback = ui;
            destinationUi = destUi;
            if (config?.landmarks != null)
                _fired = new bool[config.landmarks.Length];
        }

        private void Update()
        {
            if (player == null || config?.landmarks == null || _fired == null)
                return;

            float tower = upgrades != null ? Mathf.Max(1f, upgrades.TowerDistance) : 1800f;
            float dist = player.PathDistance;
            float norm = Mathf.Clamp01(dist / tower);

            for (int i = 0; i < config.landmarks.Length; i++)
            {
                if (_fired[i])
                    continue;

                var beat = config.landmarks[i];
                bool hit = beat.useAbsoluteMetres
                    ? dist >= beat.trigger
                    : norm >= beat.trigger;

                if (!hit)
                    continue;

                _fired[i] = true;
                // Bottom monologue only — memory fragments unlock on stage clear, not landmarks.
                if (destinationUi != null)
                    destinationUi.ShowMonologue(beat.monologue);
                else
                    ShowMemory(beat.title, beat.monologue);
            }
        }

        private void ShowMemory(string title, string body)
        {
            EnsureUi();
            _panel.gameObject.SetActive(true);
            _title.text = title;
            _body.text = body;
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfter(4.2f));
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_panel != null)
                _panel.gameObject.SetActive(false);
        }

        private void EnsureUi()
        {
            if (_canvas != null)
                return;

            var existing = GameObject.Find("CoastRunHUD");
            if (existing != null)
                _canvas = existing.GetComponent<Canvas>();

            if (_canvas == null)
                _canvas = CoastUiCanvas.Create("MemoryCanvas", 110);

            var panelGo = new GameObject("MemoryPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.55f);
            _panel.anchorMax = new Vector2(0.5f, 0.55f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(560f, 160f);
            var panelImg = panelGo.GetComponent<Image>();
            var panelSprite = CoastUiArt.AsSprite(ArtAssets.LoadTexture("UI_Panel_Memory"), 100f);
            if (panelSprite != null)
            {
                panelImg.sprite = panelSprite;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = new Color(1f, 1f, 1f, 0.95f);
            }
            else
                panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.82f);

            _title = CreateLabel(_panel, "MemTitle", new Vector2(0f, 48f), 20, FontStyle.Bold,
                new Color(0.7f, 0.9f, 1f));
            _body = CreateLabel(_panel, "MemBody", new Vector2(0f, -10f), 18, FontStyle.Normal, Color.white);
            _body.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 100f);
            _panel.gameObject.SetActive(false);
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 pos, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(520f, 36f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
