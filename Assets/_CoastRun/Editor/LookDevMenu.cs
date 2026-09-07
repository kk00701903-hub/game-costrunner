#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Editor
{
    /// 14차: 한 번 눌러 라이팅 품질을 올린다 — SSAO 렌더러 피처, 메인 라이트 섀도맵 2048, 소프트 섀도.
    /// 렌더러 피처는 런타임에 못 넣으므로 에디터에서 에셋에 박아 둔다(재실행해도 중복 추가 안 함).
    public static class LookDevMenu
    {
        private const string RendererPath = "Assets/_CoastRun/Settings/CoastRun_Renderer.asset";
        private const string PipelinePath = "Assets/_CoastRun/Settings/CoastRun_URP.asset";

        [MenuItem("Coast Run/Look/Apply SSAO + Soft Shadows (14차)")]
        public static void Apply()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (renderer == null || pipeline == null)
            {
                Debug.LogError("LookDev: renderer/pipeline asset not found under Assets/_CoastRun/Settings");
                return;
            }

            // --- SSAO ---
            var ssaoType = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (ssaoType == null)
            {
                Debug.LogError("LookDev: ScreenSpaceAmbientOcclusion type not found (URP version?)");
            }
            else
            {
                bool has = false;
                foreach (var f in renderer.rendererFeatures)
                    if (f != null && f.GetType() == ssaoType) has = true;
                if (!has)
                {
                    var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(ssaoType);
                    feature.name = "ScreenSpaceAmbientOcclusion";
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    var so = new SerializedObject(renderer);
                    var list = so.FindProperty("m_RendererFeatures");
                    var map = so.FindProperty("m_RendererFeatureMap");
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                    if (map != null)
                    {
                        map.arraySize++;
                        long id = 0;
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out id);
                        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                    }
                    so.ApplyModifiedProperties();

                    // 모바일 친화 설정: 낮은 강도, 짧은 반경, 다운샘플, 깊이 노멀 기반.
                    var fso = new SerializedObject(feature);
                    var s = fso.FindProperty("m_Settings");
                    if (s != null)
                    {
                        Set(s, "Intensity", 0.9f); Set(s, "DirectLightingStrength", 0.35f);
                        Set(s, "Radius", 0.55f); Set(s, "Falloff", 60f);
                        SetBool(s, "Downsample", true); SetBool(s, "AfterOpaque", false);
                        SetInt(s, "SampleCount", 4); SetInt(s, "AOMethod", 1); SetInt(s, "Source", 1); SetInt(s, "NormalSamples", 1);
                        SetInt(s, "BlurQuality", 1);
                    }
                    fso.ApplyModifiedProperties();
                    EditorUtility.SetDirty(feature);
                    Debug.Log("LookDev: SSAO renderer feature added");
                }
                else Debug.Log("LookDev: SSAO already present");
            }

            // --- shadows ---
            var pso = new SerializedObject(pipeline);
            SetIntProp(pso, "m_MainLightShadowmapResolution", 2048);
            SetIntProp(pso, "m_ShadowCascadeCount", 2);
            SetBoolProp(pso, "m_SoftShadowsSupported", true);
            SetFloatProp(pso, "m_ShadowDistance", 55f);
            pso.ApplyModifiedProperties();

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LookDev: shadows 2048 / soft / 55 m applied");
        }

        private static void Set(SerializedProperty parent, string name, float v) { var p = parent.FindPropertyRelative(name); if (p != null) p.floatValue = v; }
        private static void SetBool(SerializedProperty parent, string name, bool v) { var p = parent.FindPropertyRelative(name); if (p != null) p.boolValue = v; }
        private static void SetInt(SerializedProperty parent, string name, int v) { var p = parent.FindPropertyRelative(name); if (p != null) { if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = Mathf.Clamp(v, 0, Mathf.Max(0, p.enumNames.Length - 1)); else p.intValue = v; } }
        private static void SetIntProp(SerializedObject so, string name, int v) { var p = so.FindProperty(name); if (p != null) p.intValue = v; }
        private static void SetBoolProp(SerializedObject so, string name, bool v) { var p = so.FindProperty(name); if (p != null) p.boolValue = v; }
        private static void SetFloatProp(SerializedObject so, string name, float v) { var p = so.FindProperty(name); if (p != null) p.floatValue = v; }
    }
}
#endif
