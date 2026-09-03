using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// 00_Boot — create GameDirector, load save, warm up → Title.
    [DefaultExecutionOrder(-2000)]
    public class BootLoader : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != SceneFlowController.BootScene && !scene.path.Contains("00_Boot"))
                return;
            if (Object.FindAnyObjectByType<BootLoader>() != null)
                return;

            var go = new GameObject("BootLoader");
            go.AddComponent<BootLoader>();
        }

        private void Start()
        {
            var dir = GameDirector.EnsureExists();
            dir.Flow.BootToTitle();
        }
    }
}
