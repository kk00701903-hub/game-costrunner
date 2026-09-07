using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// Soft main-light shadows for Coast Run look-dev (no gameplay impact).
    public static class CoastUrpShadows
    {
        public static void Apply()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
                return;

            urp.shadowDistance = 60f;
            urp.shadowCascadeCount = 2;
            urp.shadowDepthBias = 1.0f;    // 10차: 벽면 아크네 방지
            urp.shadowNormalBias = 1.0f;
            // Soft shadows enabled on UniversalRP.asset (supportsSoftShadows is read-only at runtime).
        }
    }
}
