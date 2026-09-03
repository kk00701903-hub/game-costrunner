#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    public static class StageTableMenu
    {
        [MenuItem("Coast Run/Rebuild Stage Table Defaults")]
        public static void Rebuild()
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
            {
                AssetDatabase.CopyAsset(path, resPath);
            }
            else
            {
                res.stages = StageTable.BuildDefaultStages();
                EditorUtility.SetDirty(res);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Coast Run] StageTable rebuilt with " + table.stages.Length + " stages.");
        }
    }
}
#endif
