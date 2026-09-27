using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// 210차(사용자: 「설정에 프레임우선 모드를 디폴트로」): 그래픽 모드 — 프레임 우선(기본) / 화질 우선.
    /// 209차 측정에서 MSAA 4x 가 GPU 를 가장 많이 먹었다(끄면 +60 %). 프레임 우선 = MSAA 끔(대신 카메라 SMAA 가 테두리 정리) · 그림자 거리 60 → 40 m.
    /// 파이프라인 에셋을 직접 고치면 에디터에서 파일이 바뀌므로, 시작할 때 한 번 복제본을 만들어 그걸 쓴다.
    public static class GraphicsMode
    {
        static UniversalRenderPipelineAsset _orig, _clone;
        public static bool FramePriority => CoastPrefs.FramePriority;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { Apply(); }

        public static void Apply()
        {
            var cur = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (cur == null) return;
            if (_clone == null || (cur != _clone))
            {
                _orig = cur; _clone = Object.Instantiate(cur); _clone.name = cur.name + " (Runtime)";
                QualitySettings.renderPipeline = _clone; if (GraphicsSettings.defaultRenderPipeline == _orig) GraphicsSettings.defaultRenderPipeline = _clone;
            }
            bool fp = FramePriority;
            _clone.msaaSampleCount = fp ? 1 : _orig.msaaSampleCount;
            _clone.shadowDistance = fp ? Mathf.Min(40f, _orig.shadowDistance) : _orig.shadowDistance;
            _clone.renderScale = _orig.renderScale;
            Application.targetFrameRate = 60;
            // 켜져 있는 카메라들: MSAA 가 꺼지면 SMAA 로(테두리 계단 방지), 켜지면 SMAA 끔(겹치면 뭉개짐)
            foreach (var cam in Camera.allCameras)
            {
                var cd = cam.GetComponent<UniversalAdditionalCameraData>(); if (cd == null || cd.renderType != CameraRenderType.Base) continue;
                cd.antialiasing = _clone.msaaSampleCount > 1 ? AntialiasingMode.None : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cd.antialiasingQuality = fp ? AntialiasingQuality.Medium : AntialiasingQuality.High;
            }
        }

        public static string Label(bool ko) => ko ? ("그래픽  " + (FramePriority ? "프레임 우선" : "화질 우선")) : ("Graphics  " + (FramePriority ? "Smooth (FPS)" : "Quality"));
    }
}
