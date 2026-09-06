using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoastRun
{
    /// 컷씬 사이 짧은 영상(Firefly 생성, 세로 720×1280, 6초).
    /// Resources/CoastRun/Video/VID_<sceneId>.mp4 가 있으면 그 씬이 열리기 직전에 한 번 재생한다(탭·SKIP 으로 건너뜀).
    /// 계절 첫 챕터(CH01·CH06·CH11·CH16) 오프닝에 붙어 있다.
    public static class StoryVideo
    {
        public static VideoClip ClipFor(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return null;
            return Resources.Load<VideoClip>("CoastRun/Video/VID_" + sceneId);
        }

        /// parent(화면 전체 RectTransform) 위에 RawImage 로 재생. pressed 는 프레임마다 '탭' 여부, skip 은 강제 종료.
        public static IEnumerator Play(VideoClip clip, RectTransform parent, System.Func<bool> pressed, System.Func<bool> skip)
        {
            if (clip == null) yield break;
            int w = (int)clip.width, h = (int)clip.height;
            if (w <= 0 || h <= 0) { w = 720; h = 1280; }
            var rt = new RenderTexture(w, h, 0);
            rt.Create();

            var go = new GameObject("StoryVideo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter), typeof(VideoPlayer), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var raw = go.GetComponent<RawImage>();
            raw.texture = rt; raw.color = Color.black; raw.raycastTarget = false;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = (float)w / h;
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            var vp = go.GetComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.clip = clip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.isLooping = false;
            vp.skipOnDrop = true;
            vp.Prepare();
            float t = 0f;
            while (!vp.isPrepared && t < 4f) { t += Time.unscaledDeltaTime; yield return null; }
            Log(vp.isPrepared ? $"play {clip.name} {w}x{h} {clip.length:0.0}s (prepared in {t:0.00}s)" : "prepare timeout " + clip.name);
            if (!vp.isPrepared) { Cleanup(go, rt); yield break; }
            vp.Play();
            yield return null;
            raw.color = Color.white;
            // fade in
            t = 0f;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(t / 0.35f); yield return null; }
            cg.alpha = 1f;
            // play until end / tap / skip
            bool done = false;
            vp.loopPointReached += _ => done = true;
            float guard = (float)clip.length + 2f;
            t = 0f;
            while (!done && t < guard)
            {
                t += Time.unscaledDeltaTime;
                if (skip != null && skip()) { Log("skipped at " + t.ToString("0.0")); break; }
                if (pressed != null && pressed() && t > 0.6f) { Log("tapped at " + t.ToString("0.0")); break; }
                yield return null;
            }
            if (done) Log("finished " + clip.name);
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; cg.alpha = 1f - Mathf.Clamp01(t / 0.3f); yield return null; }
            vp.Stop();
            Cleanup(go, rt);
        }

        static void Log(string m)
        {
            Debug.Log("[StoryVideo] " + m);
        }

        static void Cleanup(GameObject go, RenderTexture rt)
        {
            if (go != null) Object.Destroy(go);
            if (rt != null) { rt.Release(); Object.Destroy(rt); }
        }
    }
}
