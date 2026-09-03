#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Editor
{
    /// Moves the render pipeline out of the retired A-0347 tree and repairs it.
    ///
    /// Two defects this fixes, both real:
    ///
    ///  1. Assets/_Project/Settings/UniversalRP.asset — the live pipeline asset per
    ///     ProjectSettings/GraphicsSettings — points its renderer list at GUID
    ///     ba6e6911…, which has never existed in this repo. The project therefore has
    ///     NO UniversalRendererData at all. Without a renderer URP cannot tell which
    ///     features are in use, so the build compiles every shader variant; that is the
    ///     36 GB out-of-memory death on the WebGL build, not a WebGL problem as such.
    ///
    ///  2. It also lives inside Assets/_Project, which is scheduled for teardown.
    ///     Deleting that folder would sever GraphicsSettings.
    ///
    /// After running this the pipeline lives in Assets/_CoastRun/Settings and
    /// Assets/_Project can be removed safely.
    ///
    /// Menu: Coast Run/Migrate URP Settings to _CoastRun
    public static class UrpMigrate
    {
        private const string SettingsDir = "Assets/_CoastRun/Settings";
        private const string RendererPath = SettingsDir + "/CoastRun_Renderer.asset";
        private const string PipelinePath = SettingsDir + "/CoastRun_URP.asset";
        private const string VolumePath = SettingsDir + "/RunVolumeProfile.asset";

        private const string LegacyPipeline = "Assets/_Project/Settings/UniversalRP.asset";
        private const string LegacyVolume = "Assets/_Project/Settings/RunVolumeProfile.asset";

        [MenuItem("Coast Run/Migrate URP Settings to _CoastRun")]
        public static void Migrate()
        {
            EnsureFolder(SettingsDir);

            var renderer = CreateRenderer();
            var pipeline = CreatePipeline(renderer);

            Assign(pipeline);
            MigrateVolumeProfile();
            int stripped = FixVariantStripping();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "URP migrated to Assets/_CoastRun/Settings.\n" +
                $"  renderer : {RendererPath}\n" +
                $"  pipeline : {PipelinePath}\n" +
                $"  stripping flags corrected: {stripped}\n" +
                "Assets/_Project can now be deleted without breaking GraphicsSettings.");
        }

        // ────────────────────────────────────────────────────────────────────

        private static UniversalRendererData CreateRenderer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (existing != null)
                return existing;

            // URP 17 resolves shader resources from the pipeline global settings, so a
            // bare instance is complete. This is the asset the project was missing.
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            data.name = "CoastRun_Renderer";
            AssetDatabase.CreateAsset(data, RendererPath);
            return data;
        }

        private static UniversalRenderPipelineAsset CreatePipeline(UniversalRendererData renderer)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (existing != null)
            {
                BindRenderer(existing, renderer);
                return existing;
            }

            var asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.name = "CoastRun_URP";
            AssetDatabase.CreateAsset(asset, PipelinePath);

            CopyLegacySettings(asset);
            BindRenderer(asset, renderer);
            return asset;
        }

        /// The renderer list is not public API; go through SerializedObject so this keeps
        /// working across URP minor versions.
        private static void BindRenderer(UniversalRenderPipelineAsset asset, UniversalRendererData renderer)
        {
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null)
                return;

            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;

            var index = so.FindProperty("m_DefaultRendererIndex");
            if (index != null)
                index.intValue = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Carries the tuned values across so the look does not shift. These match the
        /// render settings the project already relies on (shadow distance 60, 2 cascades).
        private static void CopyLegacySettings(UniversalRenderPipelineAsset target)
        {
            var legacy = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(LegacyPipeline);
            if (legacy == null)
            {
                Debug.LogWarning("URP migrate: legacy pipeline asset not found, using defaults.");
                return;
            }

            var from = new SerializedObject(legacy);
            var to = new SerializedObject(target);

            // Everything except the renderer link, which is the broken part we are replacing.
            string[] carry =
            {
                "m_SupportsHDR", "m_MSAA", "m_RenderScale",
                "m_MainLightRenderingMode", "m_MainLightShadowsSupported",
                "m_MainLightShadowmapResolution",
                "m_AdditionalLightsRenderingMode", "m_AdditionalLightsPerObjectLimit",
                "m_AdditionalLightShadowsSupported", "m_AdditionalLightsShadowmapResolution",
                "m_ShadowDistance", "m_ShadowCascadeCount",
                "m_SoftShadowsSupported", "m_ColorGradingMode", "m_ColorGradingLutSize",
                "m_UseSRPBatcher", "m_SupportsDynamicBatching",
            };

            foreach (string name in carry)
            {
                var src = from.FindProperty(name);
                var dst = to.FindProperty(name);
                if (src != null && dst != null)
                    to.CopyFromSerializedProperty(src);
            }

            to.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;

            // Quality levels can each override the pipeline; leave none pointing at the
            // old asset or the teardown reintroduces the dangling reference.
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != null)
                    QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        private static void MigrateVolumeProfile()
        {
            if (File.Exists(VolumePath) || !File.Exists(LegacyVolume))
                return;

            // Move keeps the GUID, so anything already referencing the profile survives.
            string error = AssetDatabase.MoveAsset(LegacyVolume, VolumePath);
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning("URP migrate: volume profile move failed — " + error);
        }

        /// WebGL builds every post-processing and terrain variant unless told not to.
        /// These two flags were set once and lost in the worktree restore; walk the whole
        /// serialized tree so nested copies are caught too.
        private static int FixVariantStripping()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                "Assets/UniversalRenderPipelineGlobalSettings.asset");
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ────────────────────────────────────────────────────────────────────

        [MenuItem("Coast Run/Verify URP Setup")]
        public static void Verify()
        {
            var problems = new List<string>();

            var rp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (rp == null)
                problems.Add("GraphicsSettings has no URP asset assigned.");
            else
            {
                string path = AssetDatabase.GetAssetPath(rp);
                if (path.StartsWith("Assets/_Project"))
                    problems.Add($"Pipeline still lives in the legacy tree: {path}");

                var so = new SerializedObject(rp);
                var list = so.FindProperty("m_RendererDataList");
                if (list == null || list.arraySize == 0)
                    problems.Add("Pipeline has no renderer entries.");
                else
                    for (int i = 0; i < list.arraySize; i++)
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                            problems.Add($"Renderer slot {i} is a broken reference — " +
                                         "this is what makes the build compile every shader variant.");
            }

            if (problems.Count == 0)
            {
                Debug.Log("URP setup OK — pipeline assigned, renderer resolved, outside the legacy tree.");
                return;
            }

            Debug.LogError("URP setup problems:\n  " + string.Join("\n  ", problems));
        }
    }
}
#endif
