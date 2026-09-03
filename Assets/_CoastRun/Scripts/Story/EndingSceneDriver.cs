using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// 04_Ending scene auto-hook.
    public class EndingSceneDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Auto()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != SceneFlowController.EndingScene && !scene.path.Contains("04_Ending"))
                return;
            if (Object.FindAnyObjectByType<EndingController>() != null)
                return;

            var go = new GameObject("EndingController");
            go.AddComponent<EndingController>();
        }
    }
}
