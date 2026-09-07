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

        /// 버튼 탭 진동 — 설정에서 끄면 무시. 에디터/데스크톱은 항상 무시.
        public static void Vibrate()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Haptic) Handheld.Vibrate();
#endif
        }
    }

#if UNITY_EDITOR
    public class DevScreenshot : MonoBehaviour
    {
        static int _n;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Backslash))
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
