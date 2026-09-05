using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// 05_Raising — 프린세스 메이커식 육성 화면의 씬 루트. 카메라 + 배경 + RaisingUI를
    /// 런타임에 세우고, 진입 시 돌발 이벤트(30%)와 타임라인 자동 열기를 처리한다.
    [DefaultExecutionOrder(-500)]
    public class RaisingSceneDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != CoastScenes.Raising && !scene.path.Contains(CoastScenes.Raising))
                return;
            if (Object.FindAnyObjectByType<RaisingSceneDriver>() != null)
                return;
            new GameObject("RaisingSceneDriver").AddComponent<RaisingSceneDriver>();
        }

        private RaisingUI _ui;

        private void Start()
        {
            GameDirector.EnsureExists();
            var gm = GameManager.Ensure();

            // 에디터에서 05_Raising을 바로 열었을 때: 세이브가 있으면 잇고, 없으면 새 회차.
            if (gm.Save == null)
            {
                var loaded = gm.SaveSys.Load();
                if (loaded != null) gm.Continue();
                else gm.NewGame(RunMode.Running);
                if (gm.Save == null) return;
            }

            EnsureCamera();
            _ui = gameObject.AddComponent<RaisingUI>();
            _ui.Bind(gm);

            if (gm.OpenTimelineOnRaising)
            {
                gm.OpenTimelineOnRaising = false;
                _ui.OpenTimeline();
            }
            else if (gm.Save.phaseIndex == 0 && !gm.Save.HasQueuedSchedule)
            {
                var ev = gm.RollRandomEvent();
                if (ev.HasValue)
                    _ui.ShowEvent(ev.Value);
            }
        }

        private void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.94f, 0.90f, 0.82f);
            // 다른 씬과 같은 9:16 레터박스 — 캔버스 SafeArea가 이 카메라 rect를 따라간다.
            if (cam.GetComponent<CoastPortraitViewport>() == null)
                cam.gameObject.AddComponent<CoastPortraitViewport>();
            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
        }
    }
}
