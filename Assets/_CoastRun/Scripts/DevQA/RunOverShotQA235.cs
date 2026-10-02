#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CoastRun.DevQA
{
    /// 235차: 새 하늘 그림(UI_RunOver_Bg·UI_Face_Ring/Girl)이 런 결과창에 제대로 나오는지 — 무한 달리기 시작 → 결과창 띄워 스크린샷(Builds/qa/runover235.png).
    public class RunOverShotQA235 : MonoBehaviour
    {
        [MenuItem("Coast Run/QA/235 - RunOver shot")]
        public static void Begin() { if (!Application.isPlaying) { Debug.LogError("[RunOver235] 플레이 중에만"); return; } var go = new GameObject("RunOverShotQA235"); DontDestroyOnLoad(go); go.AddComponent<RunOverShotQA235>(); }

        IEnumerator Start()
        {
            var gm = GameManager.I; float w = 0f; while (gm == null && w < 30f) { yield return null; w += Time.unscaledDeltaTime; gm = GameManager.I; }
            ArcadeRun.Start(ArcadeKind.Endless, gm);
            w = 0f; while (RunHudChrome.Instance == null && w < 60f) { yield return null; w += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(4f);
            if (RunHudChrome.Instance == null) { Debug.LogWarning("[RunOver235] HUD 없음"); yield break; }
            RunHudChrome.Instance.ShowRunOver(null, null, "육성으로 돌아가기");
            yield return new WaitForSecondsRealtime(2.5f);
            Directory.CreateDirectory("Builds/qa");
            ScreenCapture.CaptureScreenshot(Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/runover235.png"));
            yield return new WaitForSecondsRealtime(1f);
            Debug.LogWarning("[RunOver235] shot → Builds/qa/runover235.png");
            Destroy(gameObject);
        }
    }
}
#endif
