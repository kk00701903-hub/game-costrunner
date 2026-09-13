using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 77차: PC 쪽 스크립트 실행 통로 — Tools/_clip/run.bat 를 cmd 로 돌리고 stdout/stderr 를 Tools/_clip/run.log 에 남긴다.
    /// (Photoshop app.system 이 막혔을 때 파이썬 다운로드 등 PC 작업을 원격 `menu Coast Run/Dev/Run - Tools/_clip/run.bat` 로.)
    /// 오래 걸리는 작업은 bat 안에서 `start /b` 로 띄우면 즉시 돌아온다(기본 대기 180초).
    public static class RunBatMenu
    {
        [MenuItem("Coast Run/Dev/Run - Tools/_clip/run.bat")]
        public static void Run()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "_clip"));
            string bat = Path.Combine(dir, "run.bat");
            string log = Path.Combine(dir, "run.log");
            if (!File.Exists(bat)) { UnityEngine.Debug.LogError("[RunBat] 없음: " + bat); return; }
            var psi = new ProcessStartInfo("cmd.exe", "/c \"\"" + bat + "\" > \"" + log + "\" 2>&1\"")
            {
                WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) { UnityEngine.Debug.LogError("[RunBat] 실행 실패"); return; }
                bool done = p.WaitForExit(180000);
                string tail = "";
                try { var lines = File.ReadAllLines(log); tail = string.Join("\n", lines, Mathf.Max(0, lines.Length - 15), Mathf.Min(15, lines.Length)); } catch { }
                UnityEngine.Debug.LogWarning("[RunBat] " + (done ? "exit=" + p.ExitCode : "180초 초과(백그라운드 계속)") + "\n" + tail);
            }
        }
    }
}
