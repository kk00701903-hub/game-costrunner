#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    public static class CutsceneTableMenu
    {
        [MenuItem("Coast Run/Rebuild Cutscene Table Defaults")]
        public static void Rebuild()
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
            {
                AssetDatabase.CopyAsset(path, resPath);
            }
            else
            {
                res.cutscenes = CutsceneTable.BuildDefault();
                EditorUtility.SetDirty(res);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Coast Run] CutsceneTable rebuilt with " + table.cutscenes.Length + " entries.");
        }
    }
}
#endif
