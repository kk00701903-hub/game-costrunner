using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// Legacy run-scene ending hook.
    /// Reunion / explanatory endings are forbidden — S20 must use 04_Ending via SceneFlow.
    public class StoryEndingController : MonoBehaviour
    {
        [SerializeField] private StoryConfig config;
        [SerializeField] private StoryProgressDirector progress;

        public void Bind(StoryConfig storyConfig, StoryProgressDirector director)
        {
            config = storyConfig ?? CoastConfigRegistry.StoryConfig;
            progress = director;
        }

        public void PlayArrivalEnding()
        {
            progress?.NotifyArrival();
            // Do not show reunion or explanatory copy.
            // Prefer SceneFlow → 04_Ending (single ambiguous ending).
            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (flow != null)
            {
                // Already routing via StageManager → Ending when S20 clears.
                return;
            }

            // Editor / no-flow fallback: load ending scene path only.
            StartCoroutine(FallbackToEndingScene());
        }

        private IEnumerator FallbackToEndingScene()
        {
            yield return null;
            if (Application.CanStreamedLevelBeLoaded(SceneFlowController.EndingScene))
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneFlowController.EndingScene);
        }
    }
}
