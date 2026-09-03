using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// Boots title with live world (replaces still-image menu).
    [DefaultExecutionOrder(-200)]
    public class MainMenuBootstrap : MonoBehaviour
    {
        public const string ScenePath = "Assets/_CoastRun/Scenes/MainMenu.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootIfNeeded()
        {
            if (Object.FindAnyObjectByType<MainMenuBootstrap>() != null)
                return;
            if (Object.FindAnyObjectByType<MainMenuController>() != null)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("MainMenu") && !scene.path.Contains("01_Title") &&
                scene.name != SceneFlowController.TitleScene && scene.name != SceneFlowController.LegacyMenuScene)
                return;

            // TitleSceneDriver owns 01_Title; only attach for legacy MainMenu path.
            if (scene.path.Contains("01_Title") || scene.name == SceneFlowController.TitleScene)
                return;

            var root = new GameObject("MainMenuBootstrap");
            root.AddComponent<MainMenuBootstrap>();
        }

        /// True only for the legacy MainMenu scene. 01_Title is owned by TitleSceneDriver,
        /// and 00_Boot / 02_Run / 03_Cutscene / 04_Ending must never raise a title menu.
        private static bool IsMenuScene(Scene scene)
        {
            if (scene.path.Contains("01_Title") || scene.name == SceneFlowController.TitleScene)
                return false;
            return scene.path.Contains("MainMenu") || scene.name == SceneFlowController.LegacyMenuScene;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;

            // Scene-placed copies of this component leaked into 00_Boot / 03_Cutscene /
            // 04_Ending when those scenes were duplicated from the boot template.
            // 03_Cutscene loads additively, so an unguarded Awake() spawns a full title
            // menu on top of the running game. Guard the instance, not just AutoBoot.
            if (!IsMenuScene(gameObject.scene))
            {
                Destroy(this);
                return;
            }

            if (Object.FindAnyObjectByType<MainMenuController>() == null)
                gameObject.AddComponent<MainMenuController>();
        }
    }
}
