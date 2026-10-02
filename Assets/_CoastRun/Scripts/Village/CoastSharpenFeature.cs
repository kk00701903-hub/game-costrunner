using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// 151차: 전체 화면 언샤프 마스크(렌더 그래프). 포스트 프로세싱 직전에 컬러 버퍼를 한 번 선명하게.
    /// 강도는 전역 _CoastSharpen(0 = 끔) — 마을(VillagePalette.ApplySoftLook)에서 켜고 다른 씬은 0.
    public class CoastSharpenFeature : ScriptableRendererFeature
    {
        [Range(0f, 2f)] public float amount = 0.45f;
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        Material _mat; SharpenPass _pass;

        class SharpenPass : ScriptableRenderPass
        {
            public Material mat; public float amount;
            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                if (mat == null) return;
                float g = Shader.GetGlobalFloat("_CoastSharpen");
                if (g <= 0.001f) return;
                var res = frameData.Get<UniversalResourceData>();
                var cam = frameData.Get<UniversalCameraData>();
                if (cam.cameraType != CameraType.Game) return;
                if (cam.camera != null && cam.camera.clearFlags == CameraClearFlags.SolidColor && cam.camera.backgroundColor.a < 0.01f) return;   // 217차: 투명 배경 RT 카메라(낚시 주인공 뷰)는 알파를 지키려고 건너뜀
                var src = res.activeColorTexture;
                var desc = rg.GetTextureDesc(src); desc.name = "_CoastSharpenTmp"; desc.clearBuffer = false; desc.depthBufferBits = 0;
                var tmp = rg.CreateTexture(desc);
                mat.SetFloat("_Amount", amount * g);
                rg.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(src, tmp, mat, 0), "CoastSharpen");
                rg.AddBlitPass(tmp, src, Vector2.one, Vector2.zero, passName: "CoastSharpenCopy");
            }
        }

        public override void Create()
        {
            var sh = Shader.Find("CoastRun/CoastSharpen");
            if (sh != null && _mat == null) _mat = CoreUtils.CreateEngineMaterial(sh);
            _pass = new SharpenPass { mat = _mat, amount = amount, renderPassEvent = passEvent };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _mat == null) return;
            _pass.amount = amount; _pass.mat = _mat;
            renderer.EnqueuePass(_pass);
        }
        protected override void Dispose(bool disposing) { if (_mat != null) CoreUtils.Destroy(_mat); _mat = null; }
    }
}
