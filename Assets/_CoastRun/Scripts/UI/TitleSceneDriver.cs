using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// Ensures Title scene wires MainMenu into SceneFlow.
    public class TitleSceneDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Auto()
        {
            var scene = SceneManager.GetActiveScene();
            string n = scene.name;
            bool isTitle = n == SceneFlowController.TitleScene || scene.path.Contains("01_Title");
            if (!isTitle)
                return;
            if (Object.FindAnyObjectByType<TitleSceneDriver>() != null)
                return;

            var go = new GameObject("TitleSceneDriver");
            go.AddComponent<TitleSceneDriver>();
        }

        private void Start()
        {
            GameDirector.EnsureExists();
            // MainMenuController builds live TitleWorldBackdrop + splash + UI.
            if (Object.FindAnyObjectByType<MainMenuController>() == null)
                gameObject.AddComponent<MainMenuController>();
        }
    }
}
