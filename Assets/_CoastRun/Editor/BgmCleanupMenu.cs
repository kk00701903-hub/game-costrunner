using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 48차-15(사용자): Resources/CoastRun/BGM 에서 BGM_M* 만 남기고 전부 삭제. 원격 unity_cmd "menu Coast Run/Audio/Delete non-M BGM".
    public static class BgmCleanupMenu
    {
        [MenuItem("Coast Run/Audio/Delete non-M BGM")]
        public static void DeleteNonM()
        {
            const string dir = "Assets/Resources/CoastRun/BGM";
            int n = 0;
            foreach (var f in Directory.GetFiles(dir))
            {
                string name = Path.GetFileName(f);
                if (name.EndsWith(".meta") || System.Text.RegularExpressions.Regex.IsMatch(name, @"^BGM_M\d+\.") || name == "README.md") continue;
                string asset = dir + "/" + name;
                if (AssetDatabase.DeleteAsset(asset)) { n++; Debug.LogWarning("[BGM] deleted " + name); }
                else Debug.LogError("[BGM] delete failed " + name);
            }
            AssetDatabase.Refresh();
            Debug.LogWarning($"[BGM] non-M BGM deleted: {n}");
        }
    }
}
