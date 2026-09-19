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

        private TamaRaisingUI _ui;   // 45차: 다마고치식 터치 육성(TamaRaisingUI) — 프메식 RaisingUI 는 파일만 남김

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
                // 148차(사용자: 「이 화면이 처음으로 자꾸 올라온다」): Continue/NewGame 은 씬을 다시 로드한다(Flow.GoTo(Raising)).
                // 여기서 계속 세우면 첫 Start 가 마을을 짓고 → 재로드된 두 번째 Start 가 스케줄 화면을 띄우던 찌꺼기.
                if (GameDirector.Instance != null && GameDirector.Instance.Flow != null) return;
            }

            EnsureCamera();
            // 첫 오프닝이 바로 뜨면 M13 을 먼저 틀지 않음 — 모바일에서 오프닝 BGM 과 겹침
            bool pendingOpen = gm.Save.chapter == 1 && !gm.Save.prologueSeen && !gm.Save.boundaryPending;
            // 136차: 스토리 모드는 바닷가 마을부터. 148차(사용자): 05_Raising 에 들어오는 모든 길(계속하기·대회 뒤·러닝 실패 뒤·에디터 직접 열기)이
            // 마을이 기본 — 스케줄(홈) 화면은 마을에서 「우리집 → 집에 들어가기」로만. 예외: 첫 오프닝 대기, 타임라인 열기 요청.
            bool wantVillage = CoastRun.Village.VillageHub.Enabled && !pendingOpen && !gm.OpenTimelineOnRaising && !gm.SleepFromVillage;
            if (wantVillage)
            {
                _hub = CoastRun.Village.VillageHub.Create(gm, this);
                return;
            }
            if (!pendingOpen)
                TitleAudio.PlayRaising();   // 스토리 모드(육성) 배경 — BGM_M13 「하늘의 약속」
            else
                TitleAudio.StopMenuGlobal();
            _ui = gameObject.AddComponent<TamaRaisingUI>();
            _ui.Bind(gm);

            if (gm.OpenTimelineOnRaising)
            {
                gm.OpenTimelineOnRaising = false;
                _ui.OpenTimeline();
            }
            else if (gm.Save.phaseIndex == 0 && !gm.Save.HasQueuedSchedule)
            {
                var ev = gm.PeekRandomEvent();
                if (ev != null)
                    _ui.ShowEvent(ev);
            }
        }

        private CoastRun.Village.VillageHub _hub;

        /// 136차: 마을의 송전탑 언덕에서 스케줄 화면으로 — 마을을 걷어내고 기존 육성 UI를 그대로 세운다.
        public void OpenScheduleFromVillage()
        {
            var gm = GameManager.Ensure();
            if (_hub != null) { Destroy(_hub.gameObject); _hub = null; }
            RenderSettings.fog = false;
            EnsureCamera();
            TitleAudio.PlayRaising();
            _ui = gameObject.AddComponent<TamaRaisingUI>();
            _ui.Bind(gm);
            if (gm.Save.phaseIndex == 0 && !gm.Save.HasQueuedSchedule)
            {
                var ev = gm.PeekRandomEvent();
                if (ev != null) _ui.ShowEvent(ev);
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
