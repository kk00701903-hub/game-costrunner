using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 48차-15(사용자): Resources/CoastRun/BGM 에서 BGM_M* 만 남기고 전부 삭제. 원격 unity_cmd "menu Coast Run/Audio/Delete non-M BGM".
    public static class BgmCleanupMenu
    {
        /// 56차(사용자): M 곡 말고 옛 음악은 절대 안 들리게 — 프로젝트 전체 AudioClip 중 BGM_M*·SFX_* 가 아닌 것을 지운다(Art/Audio 의 옛 드론 등).
        [MenuItem("Coast Run/Audio/Delete old music (all non-M, non-SFX clips)")]
        public static void DeleteOldMusic()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^BGM_M\d+$") || name.StartsWith("SFX_")) continue;
                if (path.StartsWith("Packages/")) continue;
                if (AssetDatabase.DeleteAsset(path)) { n++; Debug.LogWarning("[BGM] deleted old clip " + path); }
                else Debug.LogError("[BGM] delete failed " + path);
            }
            AssetDatabase.Refresh();
            Debug.LogWarning($"[BGM] old clips deleted: {n}");
        }

        /// 확인용: 프로젝트에 남은 AudioClip 목록.
        [MenuItem("Coast Run/Audio/List all clips")]
        public static void ListClips()
        {
            var sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                sb.Append(path).Append(" | "); n++;
            }
            Debug.LogWarning($"[BGM] clips in Assets ({n}): " + sb);
        }

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
