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

        // Ctrl+Shift+B belongs to File/Build Profiles in Unity 6 — Ctrl+Alt+B is free.
        [MenuItem("Coast Run/Play From Boot (recommended) %&b")]
        public static void PlayFromBoot()
        {
            if (!EnsureScenesReady())
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // A dev fast-path may have left this set; a run from Boot should show
            // everything a player sees, prologue included.
            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 0);
            PlayerPrefs.Save();

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
            if (File.Exists(RunScene))
                EditorSceneManager.OpenScene(RunScene, OpenSceneMode.Single);
            else
                Debug.LogWarning("[Coast Run] 02_Run missing — run Setup Scene Flow first.");
        }

        /// Dev fast path: straight into the run with the prologue skipped. Useful while
        /// tuning gameplay, but it is not the player's experience — Play From Boot is.
        [MenuItem("Coast Run/▶ PLAY 주행만 (프롤로그 건너뜀) %#c")]
        public static void PlayRunOnly()
        {
            if (!File.Exists(RunScene))
            {
                Debug.LogWarning("[Coast Run] 02_Run missing — run Setup Scene Flow first.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 1);
            PlayerPrefs.Save();

            EditorSceneManager.OpenScene(RunScene, OpenSceneMode.Single);
            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
        }

        [MenuItem("Coast Run/▶ PLAY 주행만 — 9스테이지(3챕터)부터")]
        public static void PlayRunFromChapter3() => PlayRunFromStage(9);

        [MenuItem("Coast Run/▶ PLAY 주행만 — 13스테이지(4챕터)부터")]
        public static void PlayRunFromChapter4() => PlayRunFromStage(13);

        [MenuItem("Coast Run/▶ PLAY 주행만 — 17스테이지(5챕터)부터")]
        public static void PlayRunFromChapter5() => PlayRunFromStage(17);

        private static void PlayRunFromStage(int stage)
        {
            PlayerPrefs.SetInt(GameSession.DevStartStageKey, stage);
            PlayerPrefs.Save();
            PlayRunOnly();
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
                "Scene flow + story config ready.\n\nNext:\nCoast Run → Play From Boot\n(or Ctrl+Alt+B)\n\nGame View: 720 × 1280",
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
