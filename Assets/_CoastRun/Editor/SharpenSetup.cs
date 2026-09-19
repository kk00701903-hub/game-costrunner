#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Editor
{
    /// 151차: CoastSharpenFeature(전체 화면 언샤프)를 렌더러 에셋에 한 번 박아 둔다(중복 방지). LookDevMenu 의 SSAO 방식과 같음.
    public static class SharpenSetup
    {
        const string RendererPath = "Assets/_CoastRun/Settings/CoastRun_Renderer.asset";
        [MenuItem("Coast Run/Look/Add Sharpen feature (151차)")]
        public static void Apply()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (renderer == null) { Debug.LogError("SharpenSetup: renderer asset not found"); return; }
            foreach (var f in renderer.rendererFeatures) if (f is CoastSharpenFeature) { Debug.LogWarning("[SharpenSetup] already present"); return; }
            var feature = ScriptableObject.CreateInstance<CoastSharpenFeature>(); feature.name = "CoastSharpen"; feature.amount = 0.45f;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            var so = new SerializedObject(renderer);
            var list = so.FindProperty("m_RendererFeatures"); var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++; list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            if (map != null) { map.arraySize++; AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id); map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id; }
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(feature); EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssets();
            Debug.LogWarning("[SharpenSetup] CoastSharpen feature added");
        }
    }
}
#endif
