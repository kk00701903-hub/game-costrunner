using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CoastRun.Editor
{
    public static class CoastVolumeMenu
    {
        private const string Path = "Assets/_CoastRun/Config/Volumes/VP_Base.asset";
        private const string ResourcesPath = "Assets/Resources/CoastRun/Config/Volumes/VP_Base.asset";

        [MenuItem("Tools/Coast Run/Ensure VP_Base Volume Profile")]
        public static void EnsureVpBase()
        {
            Directory.CreateDirectory("Assets/_CoastRun/Config/Volumes");
            Directory.CreateDirectory("Assets/Resources/CoastRun/Config/Volumes");

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Path);
            if (profile == null)
            {
                profile = CoastPostStack.BuildVpBase();
                AssetDatabase.CreateAsset(profile, Path);
            }
            else
            {
                CoastPostStack.ApplyVpBaseSettings(profile);
                EditorUtility.SetDirty(profile);
            }

            // Keep components as sub-assets so duplicates stay editable.
            foreach (var c in profile.components)
            {
                if (c != null && AssetDatabase.GetAssetPath(c) != Path)
                    AssetDatabase.AddObjectToAsset(c, profile);
            }

            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ResourcesPath) == null)
                AssetDatabase.CopyAsset(Path, ResourcesPath);
            else
            {
                AssetDatabase.DeleteAsset(ResourcesPath);
                AssetDatabase.CopyAsset(Path, ResourcesPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VP_Base ready at " + Path);
        }
    }
}
