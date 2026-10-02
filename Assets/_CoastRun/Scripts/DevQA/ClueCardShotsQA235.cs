#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CoastRun.DevQA
{
    /// 235차: 단서 카드 겹침 수정 확인 — 단서 카드 3가지(바로 얻음·고르기·보류)를 띄우고 스크린샷(Builds/qa/clue235/).
    ///   빈 SaveData 사본으로 띄우므로 실제 세이브는 바뀌지 않는다(카드만 보고 닫음).
    public class ClueCardShotsQA235 : MonoBehaviour
    {
        [MenuItem("Coast Run/QA/235 - Clue card shots")]
        public static void Begin() { if (!Application.isPlaying) { Debug.LogError("[Clue235] 플레이 중에만"); return; } var go = new GameObject("ClueCardShotsQA235"); DontDestroyOnLoad(go); go.AddComponent<ClueCardShotsQA235>(); }

        IEnumerator Start()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/clue235"); Directory.CreateDirectory(dir);
            var t = typeof(ClueSystem); const BindingFlags BF = BindingFlags.Static | BindingFlags.NonPublic;
            var show = t.GetMethod("Show", BF); var notYet = t.GetMethod("ShowNotYet", BF);
            var cases = new (string name, ClueSystem.Clue c, int kind)[] { ("radio_take", ClueSystem.Clue.Radio, 0), ("name_take", ClueSystem.Clue.Name, 0), ("stones_choice", ClueSystem.Clue.Stones, 1), ("heart_notyet", ClueSystem.Clue.Heart, 2) };
            foreach (var cs in cases)
            {
                var sv = new SaveData();
                if (cs.kind == 2) notYet.Invoke(null, new object[] { sv, cs.c, null });
                else show.Invoke(null, new object[] { sv, cs.c, cs.kind == 1, null });
                yield return new WaitForSecondsRealtime(1.2f);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, cs.name + ".png"));
                yield return new WaitForSecondsRealtime(0.6f);
                ClueSystem.Close();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            Debug.LogWarning("[Clue235] shots → Builds/qa/clue235");
            Destroy(gameObject);
        }
    }
}
#endif
