#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun.Editor
{
    /// Removes bootstrap components that were copy-pasted into the wrong scenes.
    ///
    /// The five flow scenes were duplicated from one template, so MainMenuBootstrap
    /// rode along into 00_Boot, 03_Cutscene and 04_Ending. 03_Cutscene is loaded
    /// additively over the running game, so its stray copy raised a full title menu on
    /// top of gameplay every time a chapter cutscene played.
    ///
    /// None of these components need to sit in a scene at all. Every driver is a
    /// [RuntimeInitializeOnLoadMethod(AfterSceneLoad)] static that attaches itself
    /// after checking the active scene:
    ///
    ///     BootLoader          → 00_Boot
    ///     TitleSceneDriver    → 01_Title / MainMenu
    ///     CoastRunBootstrap   → 02_Run / Run
    ///     EndingSceneDriver   → 04_Ending
    ///     (03_Cutscene is driven explicitly by SceneFlowController)
    ///
    /// MainMenuBootstrap.Awake also guards itself, but that only suppresses the
    /// symptom. This clears the cause.
    ///
    /// Menu: Coast Run/Fix Scene Bootstraps
    public static class SceneBootstrapCleanup
    {
        private const string SceneDir = "Assets/_CoastRun/Scenes";

        /// 02_Run keeps CoastRunBootstrap as an explicit anchor — it is the scene a
        /// developer opens by hand most often, and seeing the entry point there helps.
        /// Everything else relies on auto-attach.
        private static readonly Dictionary<string, System.Type> Allowed = new()
        {
            { "02_Run", typeof(CoastRunBootstrap) },
            { "Run", typeof(CoastRunBootstrap) },
            { "01_Title", null },
            { "MainMenu", null },
            { "00_Boot", null },
            { "03_Cutscene", null },
            { "04_Ending", null },
        };

        [MenuItem("Coast Run/Fix Scene Bootstraps")]
        public static void Fix()
        {
            var report = new List<string>();
            string openBefore = EditorSceneManager.GetActiveScene().path;

            foreach (var (sceneName, keep) in Allowed)
            {
                string path = $"{SceneDir}/{sceneName}.unity";
                if (!System.IO.File.Exists(path))
                    continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int removed = 0;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (var boot in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (boot == null || !IsBootstrap(boot))
                            continue;
                        if (keep != null && boot.GetType() == keep)
                            continue;

                        report.Add($"{sceneName}: removed {boot.GetType().Name}");
                        Object.DestroyImmediate(boot.gameObject);
                        removed++;
                        break;
                    }
                }

                if (removed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(openBefore) && System.IO.File.Exists(openBefore))
                EditorSceneManager.OpenScene(openBefore, OpenSceneMode.Single);

            Debug.Log(report.Count == 0
                ? "Scene bootstraps already correct."
                : "Scene bootstraps fixed:\n  " + string.Join("\n  ", report));
        }

        [MenuItem("Coast Run/Verify Scene Bootstraps")]
        public static void Verify()
        {
            var problems = new List<string>();

            foreach (var (sceneName, keep) in Allowed)
            {
                string path = $"{SceneDir}/{sceneName}.unity";
                if (!System.IO.File.Exists(path))
                    continue;

                // Read the YAML rather than opening scenes, so this is cheap enough to
                // run before a build without disturbing what the developer has open.
                string yaml = System.IO.File.ReadAllText(path);
                foreach (var (guid, typeName) in BootstrapGuids())
                {
                    if (!yaml.Contains(guid))
                        continue;
                    if (keep != null && typeName == keep.Name)
                        continue;
                    problems.Add($"{sceneName} still carries {typeName}");
                }
            }

            Debug.Log(problems.Count == 0
                ? "Scene bootstraps OK."
                : "Scene bootstrap problems:\n  " + string.Join("\n  ", problems));
        }

        private static bool IsBootstrap(MonoBehaviour mb) =>
            mb is MainMenuBootstrap || mb is CoastRunBootstrap;

        private static IEnumerable<(string guid, string typeName)> BootstrapGuids()
        {
            foreach (string typeName in new[] { "MainMenuBootstrap", "CoastRunBootstrap" })
            {
                string[] hits = AssetDatabase.FindAssets($"{typeName} t:MonoScript");
                foreach (string guid in hits)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(p) == typeName)
                        yield return (guid, typeName);
                }
            }
        }
    }
}
#endif
