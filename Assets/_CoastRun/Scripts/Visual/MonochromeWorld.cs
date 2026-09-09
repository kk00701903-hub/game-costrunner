using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun
{
    /// 19차-1: 20장(송전탑) 러닝 — 처음 10초는 평소처럼, 그 뒤 세상이 서서히 흑백이 된다.
    /// URP ColorAdjustments(채도 −100)를 전역 Volume(우선순위 30, 주스 볼륨보다 위)으로 올려 2.5초에 걸쳐 물들인다.
    /// UI(ScreenSpaceOverlay)는 후처리 밖이라 그대로 색이 남는다 — 화면 속 세상만 빛이 빠진다.
    public class MonochromeWorld : MonoBehaviour
    {
        public const int Chapter = 20;
        public const float DelaySeconds = 10f;
        public const float FadeSeconds = 2.5f;

        private static MonochromeWorld _inst;
        private Volume _volume;
        private ColorAdjustments _color;
        private float _elapsed;
        private bool _armed;

        /// StageManager가 스테이지 시작 때 호출. 20장이면 타이머를 켜고, 아니면 색을 되돌린다.
        public static void Arm(int chapter)
        {
            if (_inst == null)
            {
                var go = new GameObject("MonochromeWorld");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<MonochromeWorld>();
                _inst.Build();
            }
            _inst._armed = chapter == Chapter;
            _inst._elapsed = 0f;
            _inst._volume.weight = 0f;
            if (_inst._armed) _inst.EnsureCamera(false);
        }

        /// 카메라가 후처리를 켜고 있고, 이 볼륨의 레이어가 카메라 볼륨 마스크에 들어가는지 보장한다.
        private void EnsureCamera(bool log)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            int mask = data.volumeLayerMask.value;
            if ((mask & (1 << gameObject.layer)) == 0)
            {
                for (int l = 0; l < 32; l++) if ((mask & (1 << l)) != 0) { gameObject.layer = l; break; }
            }
            if (log)
            {
                var stack = data.volumeStack;
                var ca = stack != null ? stack.GetComponent<ColorAdjustments>() : null;
                Debug.Log($"[Monochrome] cam={cam.name} post={data.renderPostProcessing} mask={mask} layer={gameObject.layer} weight={_volume.weight} stackSat={(ca != null ? ca.saturation.value.ToString("0") : "n/a")} aa={data.antialiasing}");
            }
        }

        public static void Disarm()
        {
            if (_inst == null) return;
            _inst._armed = false;
            _inst._volume.weight = 0f;
        }

        private void Build()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 30f;
            _volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_Monochrome";
            _color = profile.Add<ColorAdjustments>(true);
            _color.active = true;
            _color.saturation.Override(-100f);
            _color.contrast.Override(12f);          // 흑백에서 형태가 뭉개지지 않게 대비 살짝
            _color.postExposure.Override(-0.15f);   // 아주 조금 어둡게 — '빛이 빠진' 느낌
            _volume.profile = profile;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (CoastRemoteKeys.Down(KeyCode.B)) { _armed = true; _elapsed = DelaySeconds; EnsureCamera(true); }   // 디버그: 즉시 흑백
#endif
            if (!_armed) return;
            var p = FindAnyObjectByType<PlayerController>();
            if (p == null || p.Speed < 0.5f) return;   // 실제로 달리기 시작한 뒤부터 센다
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01((_elapsed - DelaySeconds) / FadeSeconds);
            float w = t * t * (3f - 2f * t);
            if (_volume.weight != w)
            {
                _volume.weight = w;
                if (t > 0f && t < 1f && _elapsed - DelaySeconds < Time.deltaTime * 2f)
                    JuiceDirector.Instance?.OnWorldFade();
            }
        }
    }
}
