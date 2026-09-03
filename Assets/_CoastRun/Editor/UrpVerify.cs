#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Editor
{
    /// Guards the two render settings that have each broken a build once.
    ///
    ///  1. A dangling renderer reference. GraphicsSettings used to point at
    ///     Assets/_Project/Settings/UniversalRP.asset whose renderer list held GUID
    ///     ba6e6911…, never present in this repo. With no UniversalRendererData URP
    ///     cannot tell which features are used, so it compiles every shader variant —
    ///     that was the 36 GB out-of-memory death on the WebGL build.
    ///
    ///  2. Variant stripping flags. m_StripUnusedPostProcessingVariants and
    ///     m_IncludeTerrainShaders were set correctly once and then silently reverted
    ///     by a worktree restore. They sit in several nested copies inside
    ///     UniversalRenderPipelineGlobalSettings.asset, so fixing one is not enough.
    ///
    /// The one-shot migration that moved the pipeline into Assets/_CoastRun/Settings
    /// has been run and removed; what remains is the check and the repair.
    public static class UrpVerify
    {
        private const string GlobalSettings = "Assets/UniversalRenderPipelineGlobalSettings.asset";

        [MenuItem("Coast Run/Verify URP Setup")]
        public static void Verify()
        {
            var problems = new List<string>();

            var rp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (rp == null)
            {
                problems.Add("GraphicsSettings has no URP asset assigned.");
            }
            else
            {
                string path = AssetDatabase.GetAssetPath(rp);
                if (path.StartsWith("Assets/_Project"))
                    problems.Add($"Pipeline still lives in the retired tree: {path}");

                var so = new SerializedObject(rp);
                var list = so.FindProperty("m_RendererDataList");
                if (list == null || list.arraySize == 0)
                {
                    problems.Add("Pipeline has no renderer entries — every shader variant will compile.");
                }
                else
                {
                    for (int i = 0; i < list.arraySize; i++)
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                            problems.Add($"Renderer slot {i} is a broken reference — " +
                                         "this is what makes the build compile every shader variant.");
                }
            }

            problems.AddRange(StrippingProblems());

            if (!PlayerSettings.WebGL.decompressionFallback)
                problems.Add(
                    "WebGL decompressionFallback is off. GitHub Pages serves .unityweb without " +
                    "Content-Encoding: gzip, so the loader must decompress in JS. " +
                    "Turning this off breaks the deployed page immediately.");

            if (problems.Count == 0)
            {
                Debug.Log("URP setup OK — pipeline assigned, renderer resolved, stripping on, " +
                          "WebGL decompression fallback on.");
                return;
            }

            Debug.LogError("URP setup problems:\n  " + string.Join("\n  ", problems));
        }

        [MenuItem("Coast Run/Repair Variant Stripping")]
        public static void RepairStripping()
        {
            int changed = FixVariantStripping();
            if (!PlayerSettings.WebGL.decompressionFallback)
            {
                PlayerSettings.WebGL.decompressionFallback = true;
                changed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(changed == 0
                ? "Variant stripping already correct."
                : $"Variant stripping repaired — {changed} setting(s) corrected.");
        }

        // ────────────────────────────────────────────────────────────────────

        private static IEnumerable<string> StrippingProblems()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GlobalSettings);
            if (settings == null)
            {
                yield return "UniversalRenderPipelineGlobalSettings.asset not found.";
                yield break;
            }

            var it = new SerializedObject(settings).GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.Boolean)
                    continue;

                if (it.name == "m_StripUnusedPostProcessingVariants" && !it.boolValue)
                    yield return $"{it.propertyPath} is off — post-processing variants will not be stripped.";
                else if (it.name == "m_IncludeTerrainShaders" && it.boolValue)
                    yield return $"{it.propertyPath} is on — this project has no terrain.";
            }
        }

        /// Walks the whole serialized tree; the flags exist in several nested copies.
        private static int FixVariantStripping()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GlobalSettings);
            if (settings == null)
                return 0;

            var so = new SerializedObject(settings);
            var it = so.GetIterator();
            int changed = 0;

            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.Boolean)
                    continue;

                if (it.name == "m_StripUnusedPostProcessingVariants" && !it.boolValue)
                {
                    it.boolValue = true;
                    changed++;
                }
                else if (it.name == "m_IncludeTerrainShaders" && it.boolValue)
                {
                    it.boolValue = false;
                    changed++;
                }
            }

            if (changed > 0)
                so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }
    }
}
#endif
