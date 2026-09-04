using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// Attaches each scene's driver when that scene loads.
    ///
    /// Every driver used to install itself from its own
    /// [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] static. That callback fires
    /// **once**, at application start, against whatever scene happens to be open — it is
    /// not re-run for later scene loads. So booting from 00_Boot installed BootLoader and
    /// nothing else: SceneFlowController would load 01_Title and no TitleSceneDriver ever
    /// attached, leaving an empty scene with no camera. The Game view sat on
    /// "No cameras rendering" forever, which read as "the title screen does not exist"
    /// even though all of its code was present and correct.
    ///
    /// It only looked fine when a developer opened 02_Run directly and pressed Play,
    /// because then the run scene *was* the startup scene.
    ///
    /// Subscribing to sceneLoaded fixes every scene, including additive cutscene loads.
    /// The per-driver statics can stay — each checks for an existing instance first, so
    /// the two paths do not fight.
    public static class SceneDriverInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Filter by scene, not by load mode. 02_Run is preloaded additively behind the
            // title so the handoff is seamless — a Single-only filter silently skipped it
            // and the run came up with no camera. The one scene that must not get a
            // driver is 03_Cutscene, which SceneFlowController drives by hand.
            if (Matches(scene.name, scene.path, CoastScenes.Cutscene))
                return;

            Install(scene);
        }

        /// Idempotent: safe to call for a scene that already has its driver.
        public static void Install(Scene scene)
        {
            string path = scene.path;
            string name = scene.name;

            if (Matches(name, path, CoastScenes.Boot))
                Ensure<BootLoader>("BootLoader", scene);
            else if (Matches(name, path, CoastScenes.Title))
                Ensure<TitleSceneDriver>("TitleSceneDriver", scene);
            else if (Matches(name, path, CoastScenes.Run))
                Ensure<CoastRunBootstrap>("CoastRunBootstrap", scene);
            else if (Matches(name, path, CoastScenes.Ending))
                Ensure<EndingSceneDriver>("EndingSceneDriver", scene);
        }

        private static bool Matches(string sceneName, string scenePath, string target) =>
            sceneName == target || (!string.IsNullOrEmpty(scenePath) && scenePath.Contains(target));

        private static void Ensure<T>(string goName, Scene scene) where T : Component
        {
            if (Object.FindAnyObjectByType<T>() != null)
                return;

            var go = new GameObject(goName);

            // During a Single-mode sceneLoaded callback the *outgoing* scene can still be
            // the active one, so a plain `new GameObject` lands in the scene that is about
            // to be torn down and the driver dies with it — which is exactly why the title
            // screen came up empty. Park it in the scene that just loaded.
            if (scene.IsValid() && scene.isLoaded && go.scene != scene)
                SceneManager.MoveGameObjectToScene(go, scene);

            go.AddComponent<T>();
        }
    }
}
