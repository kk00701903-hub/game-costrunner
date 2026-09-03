using UnityEngine;

namespace CoastRun
{
    /// Minimal boot for Coast Run scenes. Attach to an empty GameObject in Run.unity.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private RunConfig config;
        [SerializeField] private StoryConfig storyConfig;
        [SerializeField] private PlayerController player;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private MapGenerator map;
        [SerializeField] private EnvironmentManager environment;
        [SerializeField] private MobileSwipeInput input;

        [Header("Core Loop")]
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private NearMissSystem nearMiss;
        [SerializeField] private ObstacleSpawner obstacles;
        [SerializeField] private CoinSpawner coins;
        [SerializeField] private DestinationGate destination;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private StageManager stages;
        [SerializeField] private StageClearUI stageClearUi;

        [Header("Story — 우리의 송전탑")]
        [SerializeField] private StoryManager story;
        [SerializeField] private StoryProgressDirector storyProgress;
        [SerializeField] private StoryEndingController storyEnding;
        [SerializeField] private LandmarkManager landmarks;
        [SerializeField] private DynamicEnvironmentManager dayCycle;
        [SerializeField] private UI_FinalDestinationController destinationUi;
        [SerializeField] private UI_PhoneOverlay phoneOverlay;
        [SerializeField] private SeasonWeatherDirector seasonWeather;
        [SerializeField] private WeatherFx weatherFx;
        [SerializeField] private CoastAudioManager audio;
        [SerializeField] private JuiceDirector juice;

        [Header("Palette")]
        [SerializeField] private Color sky = new Color(0.22f, 0.52f, 0.92f);
        [SerializeField] private Color fog = new Color(0.75f, 0.88f, 0.95f);
        [SerializeField] private float fogDensity = 0.0028f;

        public bool IsRunning { get; private set; }
        public CoinWallet Wallet => wallet;
        public UpgradeManager Upgrades => upgrades;
        public StageManager Stages => stages;
        // Not named Camera: that would shadow UnityEngine.Camera inside this class.
        public CameraController CameraRig => cameraController;

        private bool _sessionBooted;
        private bool _suspendedForHandoff;
        private UpgradeShopUI _shopUi;

        /// Called by CoastRunBootstrap after world build.
        public void InitializeFromBootstrap(PlayerController bootPlayer, Camera cam)
        {
            player = bootPlayer;
            map = Object.FindFirstObjectByType<MapGenerator>();
            environment = Object.FindFirstObjectByType<EnvironmentManager>();
            input = gameObject.AddComponent<MobileSwipeInput>();

            if (cam != null)
            {
                cameraController = cam.GetComponent<CameraController>();
                if (cameraController == null)
                    cameraController = cam.gameObject.AddComponent<CameraController>();
            }

            EnsureCoreLoop();
            BeginWithStory();
            _sessionBooted = true;
        }

        private void Start()
        {
            if (_sessionBooted || IsRunning)
                return;

            if (player == null)
            {
                Application.targetFrameRate = 60;

                if (input == null)
                    input = FindFirstObjectByType<MobileSwipeInput>() ?? gameObject.AddComponent<MobileSwipeInput>();
                if (map == null)
                    map = FindFirstObjectByType<MapGenerator>() ?? new GameObject("MapGenerator").AddComponent<MapGenerator>();
                if (environment == null)
                    environment = FindFirstObjectByType<EnvironmentManager>() ?? gameObject.AddComponent<EnvironmentManager>();
                if (player == null)
                    player = FindFirstObjectByType<PlayerController>();
                if (cameraController == null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        cameraController = cam.GetComponent<CameraController>();
                        if (cameraController == null)
                            cameraController = cam.gameObject.AddComponent<CameraController>();
                    }
                }
            }

            EnsureCoreLoop();
            BeginWithStory();
        }

        private void EnsureCoreLoop()
        {
            if (wallet == null)
                wallet = gameObject.GetComponent<CoinWallet>() ?? gameObject.AddComponent<CoinWallet>();
            if (upgrades == null)
                upgrades = gameObject.GetComponent<UpgradeManager>() ?? gameObject.AddComponent<UpgradeManager>();
            if (nearMiss == null)
                nearMiss = gameObject.GetComponent<NearMissSystem>() ?? gameObject.AddComponent<NearMissSystem>();
            if (feedback == null)
                feedback = gameObject.GetComponent<UI_FeedbackController>() ?? gameObject.AddComponent<UI_FeedbackController>();
            if (obstacles == null)
                obstacles = gameObject.GetComponent<ObstacleSpawner>() ?? gameObject.AddComponent<ObstacleSpawner>();
            if (coins == null)
                coins = gameObject.GetComponent<CoinSpawner>() ?? gameObject.AddComponent<CoinSpawner>();
            if (destination == null)
                destination = gameObject.GetComponent<DestinationGate>() ?? gameObject.AddComponent<DestinationGate>();

            // Prefer GameDirector DDOL services when present.
            var dir = GameDirector.Instance;
            if (dir != null)
            {
                if (stages == null)
                    stages = dir.Stages;
                if (dayCycle == null)
                    dayCycle = dir.Environment;
                if (story == null)
                    story = dir.Story;
            }

            if (stages == null)
                stages = gameObject.GetComponent<StageManager>() ?? gameObject.AddComponent<StageManager>();
            if (stageClearUi == null)
                stageClearUi = gameObject.GetComponent<StageClearUI>() ?? gameObject.AddComponent<StageClearUI>();

            if (storyConfig == null)
                storyConfig = CoastConfigRegistry.StoryConfig;

            if (config == null)
                config = CoastConfigRegistry.RunConfig;

            if (story == null)
                story = gameObject.GetComponent<StoryManager>() ?? gameObject.AddComponent<StoryManager>();
            if (storyProgress == null)
                storyProgress = gameObject.GetComponent<StoryProgressDirector>() ??
                                gameObject.AddComponent<StoryProgressDirector>();
            if (storyEnding == null)
                storyEnding = gameObject.GetComponent<StoryEndingController>() ??
                              gameObject.AddComponent<StoryEndingController>();
            if (landmarks == null)
                landmarks = gameObject.GetComponent<LandmarkManager>() ?? gameObject.AddComponent<LandmarkManager>();
            if (dayCycle == null)
                dayCycle = gameObject.GetComponent<DynamicEnvironmentManager>() ??
                           gameObject.AddComponent<DynamicEnvironmentManager>();
            if (destinationUi == null)
                destinationUi = gameObject.GetComponent<UI_FinalDestinationController>() ??
                                gameObject.AddComponent<UI_FinalDestinationController>();
            if (phoneOverlay == null)
                phoneOverlay = gameObject.GetComponent<UI_PhoneOverlay>() ??
                               gameObject.AddComponent<UI_PhoneOverlay>();
            if (seasonWeather == null)
                seasonWeather = gameObject.GetComponent<SeasonWeatherDirector>() ??
                                gameObject.AddComponent<SeasonWeatherDirector>();
            if (weatherFx == null)
                weatherFx = gameObject.GetComponent<WeatherFx>() ?? gameObject.AddComponent<WeatherFx>();

            var shop = gameObject.GetComponent<UpgradeShopHotkeys>() ?? gameObject.AddComponent<UpgradeShopHotkeys>();
            _shopUi = gameObject.GetComponent<UpgradeShopUI>() ?? gameObject.AddComponent<UpgradeShopUI>();

            feedback.BuildRuntime(wallet);
            upgrades.Bind(CoastConfigRegistry.UpgradeConfig, wallet, feedback);
            nearMiss.Bind(wallet, upgrades, feedback);
            weatherFx.Bind(player != null ? player.transform : transform);
            seasonWeather.Bind(player, dayCycle, weatherFx);

            // Season/weather HUD is gone for good — the one-day lightingT design has no
            // season cycle to display, and nothing builds that widget any more.

            if (audio == null)
                audio = gameObject.GetComponent<CoastAudioManager>() ?? gameObject.AddComponent<CoastAudioManager>();
            audio.Bind(player, seasonWeather);

            if (juice == null)
                juice = gameObject.GetComponent<JuiceDirector>() ?? gameObject.AddComponent<JuiceDirector>();
            RunnerCameraRig rig = null;
            if (cameraController != null)
                rig = cameraController.GetComponent<RunnerCameraRig>() ??
                      cameraController.gameObject.AddComponent<RunnerCameraRig>();
            else if (Camera.main != null)
                rig = Camera.main.GetComponent<RunnerCameraRig>() ??
                      Camera.main.gameObject.AddComponent<RunnerCameraRig>();
            juice.Bind(player, nearMiss, wallet, feedback, destinationUi, audio, rig);

            obstacles.Bind(player, seasonWeather);
            coins.Bind(player, wallet, upgrades, feedback);

            // StageManager owns clear/retry; tower gate only for legacy / S20 assist.
            destination.enabled = false;
            destination.Bind(upgrades, player, this, feedback);

            _shopUi.Bind(upgrades, wallet, feedback);
            stageClearUi.Bind(upgrades, wallet, feedback, _shopUi);
            shop.Bind(upgrades, feedback);
            shop.SetEnabledWhen(() => stageClearUi != null && stageClearUi.IsVisible);

            stages.Bind(CoastConfigRegistry.StageTable, player, dayCycle, stageClearUi, feedback);
            stages.OnStageStart -= HandleStageStart;
            stages.OnStageStart += HandleStageStart;
            stages.OnStageClear -= HandleStageClear;
            stages.OnStageClear += HandleStageClear;
            stages.OnChapterComplete -= HandleChapterComplete;
            stages.OnChapterComplete += HandleChapterComplete;

            // Prefer GameDirector memory services; rebind to this StageManager (run scene).
            var director = GameDirector.EnsureExists();
            director.MemoryLog?.Bind(director.Progression);
            director.Memory?.Bind(director.MemoryLog, stages);

            story.Bind(storyConfig, this, player, cameraController);
            storyProgress.Bind(storyConfig, story, player, upgrades, dayCycle, destinationUi);
            storyEnding.Bind(storyConfig, storyProgress);
            landmarks.Bind(storyConfig, player, upgrades, feedback, destinationUi);
            dayCycle.Bind(storyConfig, player, upgrades);
            phoneOverlay.Bind();
            destinationUi.Bind(storyConfig, player, upgrades, nearMiss, dayCycle, stages, feedback, phoneOverlay);
            destinationUi.AttachPhoneCanvasGroup(phoneOverlay.IconCanvasGroup);

            // Tower landmark near cumulative end of S20 (still one scene).
            float towerZ = 0f;
            var table = CoastConfigRegistry.StageTable;
            table.EnsurePopulated();
            for (int i = 0; i < table.stages.Length; i++)
                towerZ += table.stages[i].targetDistance;
            DestinationGate.CreateVisual(transform, towerZ);
            // Beacon anchors to tower — ensure after visual spawn.
            dayCycle?.ResetLightingTo(dayCycle.LightingT);
        }

        private void HandleStageStart(StageDef stage)
        {
            seasonWeather?.SetChapterTheme(stage.chapterIndex);
            IsRunning = true;
            if (input != null)
                input.enabled = true;
            if (player != null)
                player.enabled = true;
        }

        private void HandleStageClear(StageDef stage)
        {
            IsRunning = false;
            if (input != null)
                input.enabled = false;
            if (player != null)
                player.enabled = false;
            wallet?.Persist();
            upgrades?.SaveAll();
        }

        private void HandleChapterComplete(int chapter)
        {
            phoneOverlay?.SetChapter(chapter);
            if (chapter >= 4)
                phoneOverlay?.SetTwistStage(2);
        }

        private void BeginWithStory()
        {
            if (player == null)
                return;

            if (config == null)
                config = CoastConfigRegistry.RunConfig;

            wallet?.ResetSession();
            player.Bind(input, map, config, upgrades);
            environment?.SetFollow(player.transform);
            cameraController?.SetTarget(player);
            environment?.ApplyPalette(sky, fog, fogDensity);

            // SceneFlow owns prologue (Cutscene) — skip embedded StoryManager prologue.
            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (flow != null)
            {
                if (flow.AwaitingPrologueHandoff)
                {
                    SuspendForPrologueHandoff();
                    return;
                }

                StartSession();
                return;
            }

            story.OnPrologueFinished -= StartSession;
            story.OnPrologueFinished += StartSession;
            story.BeginPrologue();
        }

        /// Run is loaded under the cinematic — cam/input/HUD off until P4 handoff.
        public void SuspendForPrologueHandoff()
        {
            _suspendedForHandoff = true;
            IsRunning = false;
            if (input != null)
                input.enabled = false;
            if (player != null)
                player.enabled = false;

            cameraController?.SetFollowSuspended(true);
            var cam = cameraController != null ? cameraController.GetComponent<Camera>() : null;
            if (cam != null)
                cam.enabled = false;
            var listener = cameraController != null
                ? cameraController.GetComponent<AudioListener>()
                : null;
            if (listener != null)
                listener.enabled = false;

            SetRunHudAlpha(0f);
        }

        /// Same-frame pose copy from cine cam. No lerp / fade.
        public void ApplyPrologueCameraSnap(Camera cine)
        {
            if (cine == null)
                return;

            if (cameraController == null)
            {
                Camera main = Camera.main;
                if (main != null)
                {
                    cameraController = main.GetComponent<CameraController>() ??
                                       main.gameObject.AddComponent<CameraController>();
                }
            }

            if (cameraController == null)
                return;

            cameraController.SnapPose(
                cine.transform.position,
                cine.transform.rotation,
                cine.fieldOfView);

            var cam = cameraController.GetComponent<Camera>();
            if (cam != null)
            {
                cam.enabled = true;
                var al = cam.GetComponent<AudioListener>() ?? cam.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }

            cine.enabled = false;
            var cineAl = cine.GetComponent<AudioListener>();
            if (cineAl != null)
                cineAl.enabled = false;
        }

        /// Input on + HUD fade-in 0.5s after seamless camera swap.
        public System.Collections.IEnumerator ReleaseAfterPrologueHandoff()
        {
            _suspendedForHandoff = false;
            if (!IsRunning)
                StartSession();
            else
            {
                if (input != null)
                    input.enabled = true;
                if (player != null)
                    player.enabled = true;
            }

            yield return FadeRunHud(0f, 1f, 0.5f);
        }

        private static void SetRunHudAlpha(float alpha)
        {
            foreach (var name in new[] { "CoastRunHUD", "JourneyHUD", "PhoneHUD" })
            {
                var go = GameObject.Find(name);
                if (go == null)
                    continue;
                var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
                cg.blocksRaycasts = alpha > 0.5f;
            }
        }

        private static System.Collections.IEnumerator FadeRunHud(float from, float to, float duration)
        {
            var groups = new System.Collections.Generic.List<CanvasGroup>();
            foreach (var name in new[] { "CoastRunHUD", "JourneyHUD", "PhoneHUD" })
            {
                var go = GameObject.Find(name);
                if (go == null)
                    continue;
                groups.Add(go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>());
            }

            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                for (int i = 0; i < groups.Count; i++)
                {
                    groups[i].alpha = a;
                    groups[i].blocksRaycasts = a > 0.5f;
                }

                yield return null;
            }

            SetRunHudAlpha(to);
        }

        private void StartSession()
        {
            if (player == null)
                return;
            if (_suspendedForHandoff)
                return;

            IsRunning = true;
            if (input != null)
                input.enabled = true;
            player.enabled = true;

            int start = 1;
            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (flow != null)
                start = Mathf.Max(1, flow.PendingStage);
            stages?.BeginCampaign(start);
        }

        private void Update()
        {
            if (player == null)
                return;

            map?.SetPlayerDistance(player.PathDistance);
        }

        /// Legacy single-run finish (S20 / external). Prefer StageManager clear flow.
        public void EndRun()
        {
            IsRunning = false;
            destinationUi?.ShowArrival();
            storyEnding?.PlayArrivalEnding();
            wallet?.Persist();
            upgrades?.SaveAll();
            player?.FinishRun();
        }
    }
}
