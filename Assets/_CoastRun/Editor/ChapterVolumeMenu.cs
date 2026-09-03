#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Editor
{
    public static class ChapterVolumeMenu
    {
        [MenuItem("Coast Run/Build Chapter Volume Profiles (VP_CH1-5)")]
        public static void Build()
        {
            const string dir = "Assets/_CoastRun/Config/Volumes";
            const string resDir = "Assets/Resources/CoastRun/Config/Volumes";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_CoastRun/Config", "Volumes");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/CoastRun/Config"))
                AssetDatabase.CreateFolder("Assets/Resources/CoastRun", "Config");
            if (!AssetDatabase.IsValidFolder(resDir))
                AssetDatabase.CreateFolder("Assets/Resources/CoastRun/Config", "Volumes");

            for (int i = 1; i <= 5; i++)
            {
                string path = dir + "/VP_CH" + i + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                // Clear and rebuild grade so re-run is idempotent.
                profile.components.Clear();
                CoastPostStack.ApplyChapterGrade(profile, i);
                EditorUtility.SetDirty(profile);

                string resPath = resDir + "/VP_CH" + i + ".asset";
                AssetDatabase.CopyAsset(path, resPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Coast Run] VP_CH1..VP_CH5 built.");
        }
    }
}
#endif
