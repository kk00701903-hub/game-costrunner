#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Hub / Play Mode entry points for source testing.
    public static class CoastRunPlayMenu
    {
        private const string BootScene = "Assets/_CoastRun/Scenes/00_Boot.unity";
        private const string RunScene = "Assets/_CoastRun/Scenes/02_Run.unity";

        [MenuItem("Coast Run/Play From Boot (recommended) _%#B")]
        public static void PlayFromBoot()
        {
            if (!EnsureScenesReady())
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
            // delayCall so -executeMethod / menu both enter Play reliably
            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
        }

        [MenuItem("Coast Run/Open Boot Scene")]
        public static void OpenBoot()
        {
            EnsureScenesReady();
            if (File.Exists(BootScene))
                EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
        }

        [MenuItem("Coast Run/Open Run Scene (direct)")]
        public static void OpenRun()
        {
            EnsureScenesReady();
            string path = File.Exists(RunScene) ? RunScene : "Assets/_CoastRun/Scenes/Run.unity";
            if (File.Exists(path))
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            else
                Debug.LogWarning("[Coast Run] Run scene missing — run Setup Scene Flow first.");
        }

        [MenuItem("Coast Run/Prepare Project For Hub Test")]
        public static void PrepareForHubTest()
        {
            SceneFlowSetupMenu.Setup();
            StoryAssetsRebuildMenu.RebuildAll();
            // Portrait game view hint
            Debug.Log("[Coast Run] Ready. Use Coast Run → Play From Boot. Game View: 720×1280 portrait.");
            EditorUtility.DisplayDialog(
                "Coast Run",
                "Scene flow + story config ready.\n\nNext:\nCoast Run → Play From Boot\n(or Ctrl+Shift+B)\n\nGame View: 720 × 1280",
                "OK");
        }

        private static bool EnsureScenesReady()
        {
            if (!File.Exists(BootScene))
            {
                if (EditorUtility.DisplayDialog(
                        "Coast Run",
                        "Boot scene missing. Run scene-flow setup now?",
                        "Setup", "Cancel"))
                {
                    SceneFlowSetupMenu.Setup();
                }
            }

            return File.Exists(BootScene);
        }
    }
}
#endif
