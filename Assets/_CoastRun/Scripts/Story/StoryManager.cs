using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoastRun
{
    /// Plays cinematic prologue with StyleBible scene stills + text beats.
    public class StoryManager : MonoBehaviour
    {
        [SerializeField] private StoryConfig config;
        [SerializeField] private GameSession session;
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private bool skipPrologueInEditor;

        private Canvas _canvas;
        private Image _sceneBg;
        private RectTransform _captionPanel;
        private Text _title;
        private Text _body;
        private Button _skip;
        private VideoPlayer _video;
        private bool _finished;
        private Coroutine _routine;

        public bool PrologueComplete => _finished || config == null || !config.playPrologue;
        public event Action OnPrologueFinished;

        public void Bind(StoryConfig storyConfig, GameSession gameSession, PlayerController playerController,
            CameraController chaseCamera = null)
        {
            config = storyConfig != null ? storyConfig : CreateDefaultConfig();
            session = gameSession;
            player = playerController;
            cameraController = chaseCamera;
        }

        private static StoryConfig CreateDefaultConfig() => CoastConfigRegistry.StoryConfig;

        public void BeginPrologue()
        {
            if (_finished)
            {
                OnPrologueFinished?.Invoke();
                return;
            }

            if (config == null)
                config = CreateDefaultConfig();

#if UNITY_EDITOR
            if (skipPrologueInEditor)
            {
                FinishPrologue();
                return;
            }
#endif

            if (PlayerPrefs.GetInt(MainMenuController.SkipPrologueKey, 0) == 1)
            {
                FinishPrologue();
                return;
            }

            if (!config.playPrologue)
            {
                FinishPrologue();
                return;
            }

            SetPlayerFrozen(true);
            EnsureUi();
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(RunPrologue());
        }

        private IEnumerator RunPrologue()
        {
            if (config.prologueVideo != null)
            {
                yield return PlayVideo(config.prologueVideo);
            }
            else if (config.prologueBeats != null)
            {
                int last = config.prologueBeats.Length - 1;
                for (int i = 0; i < config.prologueBeats.Length; i++)
                {
                    var beat = config.prologueBeats[i];
                    ShowBeat(i, beat);

                    if (i == last && cameraController != null && player != null)
                    {
                        float hold = Mathf.Max(1.5f, beat.holdSeconds);
                        if (_captionPanel != null)
                        {
                            var cg = _captionPanel.GetComponent<CanvasGroup>();
                            if (cg == null)
                                cg = _captionPanel.gameObject.AddComponent<CanvasGroup>();
                            StartCoroutine(SimpleTween.MoveFade(_captionPanel, cg,
                                _captionPanel.anchoredPosition, _captionPanel.anchoredPosition,
                                1f, 0f, hold * 0.45f));
                        }

                        Coroutine handoff = StartCoroutine(cameraController.PlayGameplayHandoff(hold));
                        float t = 0f;
                        while (t < hold)
                        {
                            t += Time.unscaledDeltaTime;
                            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                                break;
                            yield return null;
                        }

                        if (handoff != null)
                            StopCoroutine(handoff);

                        if (_sceneBg != null)
                        {
                            var bgCg = _sceneBg.GetComponent<CanvasGroup>();
                            if (bgCg == null)
                                bgCg = _sceneBg.gameObject.AddComponent<CanvasGroup>();
                            bgCg.alpha = 1f;
                            float fade = 0f;
                            while (fade < 0.5f)
                            {
                                fade += Time.unscaledDeltaTime;
                                bgCg.alpha = 1f - fade * 2f;
                                yield return null;
                            }

                            _sceneBg.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        float hold = Mathf.Max(1.5f, beat.holdSeconds);
                        float t = 0f;
                        while (t < hold)
                        {
                            t += Time.unscaledDeltaTime;
                            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                                break;
                            yield return null;
                        }
                    }
                }
            }

            FinishPrologue();
        }

        private IEnumerator PlayVideo(VideoClip clip)
        {
            if (_video == null)
            {
                var go = new GameObject("PrologueVideo");
                go.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
                _video = go.AddComponent<VideoPlayer>();
                _video.playOnAwake = false;
                _video.renderMode = VideoRenderMode.CameraFarPlane;
                _video.targetCamera = Camera.main;
            }

            _video.clip = clip;
            _video.Prepare();
            while (!_video.isPrepared)
                yield return null;

            ShowBeat(0, new PrologueBeat { title = "우리의 송전탑", body = "…" });
            _video.Play();
            while (_video.isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    _video.Stop();
                    break;
                }

                yield return null;
            }
        }

        public void SkipPrologue() => FinishPrologue();

        private void FinishPrologue()
        {
            if (_finished)
                return;

            _finished = true;
            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 1);
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
            if (_video != null)
                _video.Stop();

            SetPlayerFrozen(false);
            OnPrologueFinished?.Invoke();
        }

        private void SetPlayerFrozen(bool frozen)
        {
            if (player == null)
                return;
            player.enabled = !frozen;
            var input = FindObjectOfType<MobileSwipeInput>();
            if (input != null)
                input.enabled = !frozen;
        }

        private void EnsureUi()
        {
            if (_canvas != null)
                return;

            _canvas = CoastUiCanvas.Create("PrologueCanvas", 200);

            // Full-screen reference scene still
            var bgGo = new GameObject("SceneBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            _sceneBg = bgGo.GetComponent<Image>();
            _sceneBg.preserveAspect = true;
            _sceneBg.color = Color.white;

            // Bottom caption card
            var panelGo = new GameObject("Caption", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(CoastUiCanvas.Root(_canvas), false);
            _captionPanel = panelGo.GetComponent<RectTransform>();
            _captionPanel.anchorMin = new Vector2(0f, 0f);
            _captionPanel.anchorMax = new Vector2(1f, 0f);
            _captionPanel.pivot = new Vector2(0.5f, 0f);
            _captionPanel.anchoredPosition = Vector2.zero;
            _captionPanel.sizeDelta = new Vector2(0f, 420f);
            panelGo.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.1f, 0.82f);

            _title = CreateText(_captionPanel, "Title", new Vector2(0f, -36f), 30, FontStyle.Bold,
                new Color(0.55f, 0.9f, 1f));
            _body = CreateText(_captionPanel, "Body", new Vector2(0f, -120f), 20, FontStyle.Normal,
                new Color(0.95f, 0.97f, 1f));
            var bodyRt = _body.GetComponent<RectTransform>();
            bodyRt.sizeDelta = new Vector2(640f, 260f);

            var skipGo = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(_captionPanel, false);
            var srt = skipGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(-20f, -16f);
            srt.sizeDelta = new Vector2(120f, 40f);
            skipGo.GetComponent<Image>().color = new Color(0.15f, 0.35f, 0.45f, 0.9f);
            _skip = skipGo.GetComponent<Button>();
            _skip.onClick.AddListener(FinishPrologue);
            var skipLabel = CreateText(srt, "SkipLabel", Vector2.zero, 16, FontStyle.Bold, Color.white);
            skipLabel.text = "SKIP ›";
            skipLabel.alignment = TextAnchor.MiddleCenter;
            var slRt = skipLabel.GetComponent<RectTransform>();
            slRt.anchorMin = Vector2.zero;
            slRt.anchorMax = Vector2.one;
            slRt.offsetMin = Vector2.zero;
            slRt.offsetMax = Vector2.zero;
        }

        private void ShowBeat(int index, PrologueBeat beat)
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(true);

            string sceneName = string.IsNullOrEmpty(beat.sceneImage)
                ? CoastSceneArt.PrologueSceneForBeat(index)
                : beat.sceneImage;

            if (_sceneBg != null)
            {
                _sceneBg.gameObject.SetActive(true);
                var sprite = CoastSceneArt.AsSprite(sceneName);
                if (sprite == null)
                    sprite = CoastUiArt.AsSprite(CoastUiArt.LoadOrFallback("UI_TitleBackground",
                        () => CoastUiArt.TitleBackground));
                _sceneBg.sprite = sprite;
                var cg = _sceneBg.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.alpha = 1f;
            }

            if (_title != null)
                _title.text = beat.title;
            if (_body != null)
                _body.text = beat.body;
        }

        private static Text CreateText(Transform parent, string name, Vector2 pos, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(660f, 80f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
