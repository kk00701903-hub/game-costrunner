#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// One-shot rebuild of story ScriptableObjects + Resources mirrors.
    public static class StoryAssetsRebuildMenu
    {
        [MenuItem("Coast Run/Rebuild Story Config Assets")]
        public static void RebuildAll()
        {
            RebuildStageTable();
            RebuildCutsceneTable();
            EnsureFolders();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Coast Run] Story config rebuilt: StageTable + CutsceneTable (+ Resources mirrors).");
        }

        private static void RebuildStageTable()
        {
            const string path = "Assets/_CoastRun/Config/StageTable.asset";
            const string resPath = "Assets/Resources/CoastRun/Config/StageTable.asset";

            var table = AssetDatabase.LoadAssetAtPath<StageTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<StageTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            table.stages = StageTable.BuildDefaultStages();
            EditorUtility.SetDirty(table);

            var res = AssetDatabase.LoadAssetAtPath<StageTable>(resPath);
            if (res == null)
                AssetDatabase.CopyAsset(path, resPath);
            else
            {
                res.stages = StageTable.BuildDefaultStages();
                EditorUtility.SetDirty(res);
            }
        }

        private static void RebuildCutsceneTable()
        {
            const string path = "Assets/_CoastRun/Config/CutsceneTable.asset";
            const string resPath = "Assets/Resources/CoastRun/Config/CutsceneTable.asset";

            var table = AssetDatabase.LoadAssetAtPath<CutsceneTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<CutsceneTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            table.cutscenes = CutsceneTable.BuildDefault();
            EditorUtility.SetDirty(table);

            var res = AssetDatabase.LoadAssetAtPath<CutsceneTable>(resPath);
            if (res == null)
                AssetDatabase.CopyAsset(path, resPath);
            else
            {
                res.cutscenes = CutsceneTable.BuildDefault();
                EditorUtility.SetDirty(res);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Resources/CoastRun", "Memory");
            EnsureFolder("Assets/Resources/CoastRun", "Audio");
            EnsureFolder("Assets/_CoastRun/Art", "Memory");
            EnsureFolder("Assets/_CoastRun/Art", "Ending");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
