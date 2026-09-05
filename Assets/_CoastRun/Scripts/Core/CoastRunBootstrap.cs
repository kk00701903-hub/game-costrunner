using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// Forces 720×1280 portrait framing in Game View / builds.
    public class CoastPortraitViewport : MonoBehaviour
    {
        public const float TargetAspect = 720f / 1280f;

        [SerializeField] private bool letterbox = true;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            if (!letterbox || _camera == null)
                return;

            float windowAspect = (float)Screen.width / Screen.height;
            if (windowAspect > TargetAspect)
            {
                float scale = TargetAspect / windowAspect;
                _camera.rect = new Rect((1f - scale) * 0.5f, 0f, scale, 1f);
            }
            else
            {
                float scale = windowAspect / TargetAspect;
                _camera.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
            }
        }
    }

    [DefaultExecutionOrder(-200)]
    public class CoastRunBootstrap : MonoBehaviour
    {
        /// Time.timeScale and AudioListener.pause persist across play sessions in the
        /// editor (and across scenes in a build). Nothing in Boot may start frozen.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void UnfreezeOnBoot()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public static string ScenePath => CoastScenes.Path(CoastScenes.Run);

        private CoastSky _sky;
        private CoastSea _sea;
        private EnvironmentManager _env;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootIfNeeded()
        {
            if (Object.FindAnyObjectByType<CoastRunBootstrap>() != null)
                return;
            if (Object.FindAnyObjectByType<GameSession>() != null)
                return;

            var scene = SceneManager.GetActiveScene();
            // Only auto-boot the Run scene — never Title / Boot / Cutscene / Ending.
            string n = scene.name;
            bool isRun = n == SceneFlowController.RunScene || scene.path.Contains("/02_Run.unity");
            if (!isRun)
                return;

            var root = new GameObject("CoastRunBootstrap");
            root.AddComponent<CoastRunBootstrap>();
        }

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            Application.targetFrameRate = 60;
            CoastPalette.Bind(CoastConfigRegistry.CoastPaletteConfig);
            EnsureLighting();
            CoastPostStack.EnsureGlobalVolume();
            EnsureWorld();
            var player = EnsurePlayer();
            var cam = EnsureCamera(player);
            WireWorldFollow(player.transform);
            EnsureSession(player, cam);
        }

        private static void EnsureLighting()
        {
            Light light = null;
            foreach (var candidate in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.type != LightType.Directional)
                    continue;
                if (candidate.gameObject.name == "RimLight")
                    continue;
                light = candidate;
                break;
            }

            GameObject lightGo;
            if (light != null)
            {
                lightGo = light.gameObject;
            }
            else
            {
                lightGo = GameObject.Find("Directional Light");
                if (lightGo == null)
                    lightGo = new GameObject("Directional Light");

                light = lightGo.GetComponent<Light>();
                if (light == null)
                    light = lightGo.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.color = CoastPalette.Sun;
            light.intensity = 1.55f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.88f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.35f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -42f, 0f);
            CoastUrpShadows.Apply();
            EnsureRimLight(lightGo.transform.rotation);
        }

        private static void EnsureRimLight(Quaternion keyRotation)
        {
            var existing = GameObject.Find("RimLight");
            Light rim;
            if (existing == null)
            {
                existing = new GameObject("RimLight");
                rim = existing.AddComponent<Light>();
            }
            else
            {
                rim = existing.GetComponent<Light>();
                if (rim == null)
                    rim = existing.AddComponent<Light>();
            }

            rim.type = LightType.Directional;
            rim.shadows = LightShadows.None;
            rim.intensity = 0.62f;
            rim.color = Color.Lerp(CoastPalette.SkyBlue, Color.white, 0.55f);
            // Opposite side of the key sun for a cool ink-edge rim.
            Vector3 keyEuler = keyRotation.eulerAngles;
            existing.transform.rotation = Quaternion.Euler(-12f, keyEuler.y + 180f, 0f);
        }

        private void EnsureWorld()
        {
            var existingMap = Object.FindAnyObjectByType<MapGenerator>();
            if (existingMap != null)
            {
                _sky = Object.FindAnyObjectByType<CoastSky>();
                _sea = Object.FindAnyObjectByType<CoastSea>();
                _env = Object.FindAnyObjectByType<EnvironmentManager>();
                return;
            }

            var world = new GameObject("World");
            var map = world.AddComponent<MapGenerator>();

            var skyGo = new GameObject("Sky");
            skyGo.transform.SetParent(world.transform);
            _sky = skyGo.AddComponent<CoastSky>();

            var seaGo = new GameObject("Sea");
            seaGo.transform.SetParent(world.transform);
            _sea = seaGo.AddComponent<CoastSea>();

            _env = world.AddComponent<EnvironmentManager>();
            _env.Configure(_sea);
        }

        private void WireWorldFollow(Transform player)
        {
            _sky?.Build(player);
            _sea?.Build(player);
            _env?.SetFollow(player);
            _env?.ApplyPalette(CoastPalette.SkyTop, CoastPalette.SkyTop, 0.0011f);
        }

        private static PlayerController EnsurePlayer()
        {
            var existing = Object.FindAnyObjectByType<PlayerController>();
            if (existing != null)
                return existing;

            var go = new GameObject("Player");
            var player = go.AddComponent<PlayerController>();
            var visual = go.AddComponent<CoastPlayerVisual>();
            visual.Build();
            return player;
        }

        private static Camera EnsureCamera(PlayerController player)
        {
            // 02_Run is preloaded additively behind the title so the handoff is seamless,
            // which means Build() runs while 01_Title is still loaded. Camera.main then
            // returns the *title's* camera — and when the title unloads a moment later it
            // takes that camera with it, leaving the run with nothing to render through.
            // Only reuse a camera that actually lives in this scene.
            Scene mine = SceneManager.GetSceneByName(CoastScenes.Run);
            Camera cam = null;
            foreach (var candidate in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.gameObject.scene == mine)
                {
                    cam = candidate;
                    break;
                }
            }

            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                if (mine.IsValid() && camGo.scene != mine)
                    SceneManager.MoveGameObjectToScene(camGo, mine);
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CoastPalette.SkyTop;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 320f;

            var ctrl = cam.GetComponent<CameraController>();
            if (ctrl == null)
                ctrl = cam.gameObject.AddComponent<CameraController>();
            if (cam.GetComponent<RunnerCameraRig>() == null)
                cam.gameObject.AddComponent<RunnerCameraRig>();
            ctrl.SetTarget(player);

            if (cam.GetComponent<CoastPortraitViewport>() == null)
                cam.gameObject.AddComponent<CoastPortraitViewport>();

            return cam;
        }

        private static void EnsureSession(PlayerController player, Camera cam)
        {
            if (Object.FindAnyObjectByType<GameSession>() != null)
                return;

            var sessionGo = new GameObject("GameSession");
            var session = sessionGo.AddComponent<GameSession>();
            session.InitializeFromBootstrap(player, cam);
        }
    }
}
