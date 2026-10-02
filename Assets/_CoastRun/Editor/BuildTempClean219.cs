using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 219차(사용자: 「C 드라이브 확보 — 필요 없는 건 지워 줘」): 빌드 때마다 다시 만들어지는 Gradle 임시 폴더
    /// (launcher/build · unityLibrary/build, 한 번에 약 3 GB)와 예전에 옮겨 둔 Library/_old* 를 지운다.
    /// 빌드 메뉴(219 - Build Play Store AAB)가 시작할 때 자동으로 부른다 — 파일 잠김(AccessDenied)·디스크 부족 예방.
    public static class BuildTempClean219
    {
        const string G = "Library/Bee/Android/Prj/IL2CPP/Gradle";

        [MenuItem("Coast Run/Store/219 - Clean build temp (free C drive)")]
        public static void CleanAllNow()
        {
            int n = CleanGradleBuild();
            if (Directory.Exists("Library"))
                foreach (var d in Directory.GetDirectories("Library", "_old*")) if (Del(d)) n++;
            Debug.LogWarning($"[BuildTemp] cleaned {n} folder(s)");
        }

        public static int CleanGradleBuild()
        {
            int n = 0;
            foreach (var sub in new[] { "launcher", "unityLibrary" }) if (Del($"{G}/{sub}/build")) n++;
            return n;
        }

        static bool Del(string dir)
        {
            if (!Directory.Exists(dir)) return false;
            try { Directory.Delete(dir, true); Debug.Log("[BuildTemp] deleted " + dir); return true; }
            catch (System.Exception e)
            {   // 동기화 프로그램 등이 잡고 있으면 옆으로 치워 두고 다음에 지운다
                try { string alt = "Library/_old_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Path.GetFileName(Path.GetDirectoryName(dir)); Directory.Move(dir, alt); Debug.LogWarning($"[BuildTemp] 삭제 실패 → {alt} 로 이동(다음 정리 때 삭제): {e.Message}"); return true; }
                catch (System.Exception e2) { Debug.LogError("[BuildTemp] " + dir + " 정리 실패: " + e2.Message); return false; }
            }
        }
    }
}
