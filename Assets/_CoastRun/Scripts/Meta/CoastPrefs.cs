using UnityEngine;

namespace CoastRun
{
    /// 9차: 기기 설정(세이브와 별개) — 마스터 볼륨·진동. 설정 카드에서 바꾸고 부팅 때 적용한다.
    public static class CoastPrefs
    {
        const string VolKey = "coast.volume";
        const string HapticKey = "coast.haptic";

        /// 0~4 (0 = 무음, 4 = 100%)
        public static int VolumeStep
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(VolKey, 4), 0, 4);
            set { PlayerPrefs.SetInt(VolKey, Mathf.Clamp(value, 0, 4)); PlayerPrefs.Save(); Apply(); }
        }

        // 210차(사용자: 「설정에 프레임우선 모드를 디폴트로」): 그래픽 모드 — 1 = 프레임 우선(기본), 0 = 화질 우선
        const string FramePriorityKey = "coast.gfx.framePriority";
        public static bool FramePriority
        {
            get => PlayerPrefs.GetInt(FramePriorityKey, 1) != 0;
            set { PlayerPrefs.SetInt(FramePriorityKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
        public static bool Haptic
        {
            get => PlayerPrefs.GetInt(HapticKey, 1) != 0;
            set { PlayerPrefs.SetInt(HapticKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string VolumeLabel(int step) => step == 0 ? "OFF" : (step * 25) + "%";

        public static void Apply()
        {
            AudioListener.volume = VolumeStep / 4f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Apply();
#if UNITY_EDITOR
            // 9차: 스토어 스크린샷용 — '\' 키로 게임 뷰 3배 캡쳐(Builds/shots/). 에디터 전용.
            var go = new GameObject("DevScreenshot");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DevScreenshot>();
#endif
        }

        /// 버튼 탭 진동 — 109차(사용자: 「스토리 모드의 진동을 너무 남발한다」): 버튼 탭은 더 이상 진동하지 않는다(호출부는 그대로 두고 여기서 무시).
        ///   진동은 `VibrateEvent()` 를 부르는 특정 이벤트에서만 — 단서 획득 · 대회/축제 결과 · 돌발 이벤트 결과 · 회상 번쩍임 · 러닝 부활/충돌.
        public static void Vibrate() { }

        /// 109차: 특정 이벤트 진동(설정에서 끄면 무시, 에디터/데스크톱은 항상 무시). 0.6초 안에 두 번 울리지 않는다.
        public static void VibrateEvent()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!Haptic) return;
            if (Time.unscaledTime - _lastEvent < 0.6f) return;
            _lastEvent = Time.unscaledTime;
            Handheld.Vibrate();
#endif
        }
        private static float _lastEvent = -10f;
    }

#if UNITY_EDITOR
    public class DevScreenshot : MonoBehaviour
    {
        static int _n;
        void Update()
        {
            if (CoastRemoteKeys.Down(KeyCode.Backslash))
            {
                System.IO.Directory.CreateDirectory("Builds/shots");
                string path = $"Builds/shots/shot_{System.DateTime.Now:HHmmss}_{_n++:00}.png";
                ScreenCapture.CaptureScreenshot(path, 3);
                Debug.Log("[DevScreenshot] " + path);
            }
        }
    }
#endif
}
