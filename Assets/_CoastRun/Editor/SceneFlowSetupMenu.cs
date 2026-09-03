#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun.Editor
{
    public static class SceneFlowSetupMenu
    {
        private const string ScenesDir = "Assets/_CoastRun/Scenes";

        [MenuItem("Coast Run/Setup Scene Flow (5 scenes + Build Settings)")]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(ScenesDir))
                AssetDatabase.CreateFolder("Assets/_CoastRun", "Scenes");

            EnsureScene("00_Boot", typeof(BootLoader));
            EnsureScene("01_Title", typeof(TitleSceneDriver));
            EnsureRunScene();
            EnsureScene("03_Cutscene", typeof(CutsceneHost));
            EnsureScene("04_Ending", typeof(EndingController));

            var scenes = new[]
            {
                ScenesDir + "/00_Boot.unity",
                ScenesDir + "/01_Title.unity",
                ScenesDir + "/02_Run.unity",
                ScenesDir + "/03_Cutscene.unity",
                ScenesDir + "/04_Ending.unity"
            };

            var list = new EditorBuildSettingsScene[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                list[i] = new EditorBuildSettingsScene(scenes[i], true);
            EditorBuildSettings.scenes = list;

            AssetDatabase.SaveAssets();
            Debug.Log("[Coast Run] Scene flow ready — 5 scenes in Build Settings. Boot = 00_Boot.");
        }

        private static void EnsureScene(string name, System.Type bootComponent)
        {
            string path = ScenesDir + "/" + name + ".unity";
            if (File.Exists(path))
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject(name + "_Root");
            go.AddComponent(bootComponent);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureRunScene()
        {
            string dest = ScenesDir + "/02_Run.unity";
            string src = ScenesDir + "/Run.unity";
            if (File.Exists(dest))
                return;

            if (File.Exists(src))
            {
                AssetDatabase.CopyAsset(src, dest);
                return;
            }

            EnsureScene("02_Run", typeof(CoastRunBootstrap));
        }
    }
}
#endif
