using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// 04_Ending scene auto-hook.
    ///
    /// SceneDriverInstaller attaches this component on every 04_Ending load; the static
    /// hook below only fires at application start. Both paths end in Ensure(), which
    /// creates the EndingController — without the instance path the ending scene loaded
    /// from the flow was an empty scene ("No cameras rendering").
    public class EndingSceneDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Auto()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != SceneFlowController.EndingScene && !scene.path.Contains("04_Ending"))
                return;
            Ensure();
        }

        private void Start() => Ensure();

        private static void Ensure()
        {
            if (Object.FindAnyObjectByType<EndingController>() != null)
                return;
            var go = new GameObject("EndingController");
            go.AddComponent<EndingController>();
        }
    }
}
