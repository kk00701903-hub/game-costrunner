using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 텔레그램 보고 워처를 에디터에서 켠다(Tools/Telegram/watch.py). Cowork 세션의 샌드박스·VM은
    /// api.telegram.org 가 막혀 있어서 PC에서 도는 이 워처가 큐(queue.txt)를 실제로 보낸다.
    /// 48차: 「Send outbox」 — Tools/Telegram/outbox.txt 한 통을 즉시 보낸다(.env 토큰, report.py).
    ///       원격에서 unity_cmd "menu Coast Run/Telegram/Send outbox" 로 호출. 결과는 Tools/Telegram/outbox.result 와 에디터 로그.
    public static class TelegramMenu
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Coast Run/Telegram/Start watcher (별도 창) %#&t")]
        public static void StartWatcher()
        {
            var psi = new ProcessStartInfo("cmd.exe",
                "/c start \"Coast Run Telegram\" python \"Tools\\Telegram\\watch.py\"")
            {
                WorkingDirectory = Root,
                UseShellExecute = true,
            };
            Process.Start(psi);
            UnityEngine.Debug.Log("[Telegram] watcher started in a console window (Tools/Telegram/watch.py)");
        }

        [MenuItem("Coast Run/Telegram/Send pending once")]
        public static void SendPending()
        {
            var psi = new ProcessStartInfo("cmd.exe",
                "/c start \"Coast Run Telegram\" cmd /k python \"Tools\\Telegram\\send_pending.py\"")
            {
                WorkingDirectory = Root,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }

        [MenuItem("Coast Run/Telegram/Send outbox")]
        public static void SendOutbox()
        {
            var psi = new ProcessStartInfo("python", "\"Tools\\Telegram\\send_outbox.py\"")
            {
                WorkingDirectory = Root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    if (p == null) { UnityEngine.Debug.LogError("[Telegram] python 실행 실패"); return; }
                    string outp = p.StandardOutput.ReadToEnd();
                    string err = p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(40000)) { UnityEngine.Debug.LogError("[Telegram] send_outbox 40초 초과"); return; }
                    UnityEngine.Debug.LogWarning("[Telegram] outbox exit=" + p.ExitCode + " " + outp.Trim() + (string.IsNullOrEmpty(err) ? "" : "\n" + err.Trim()));
                }
            }
            catch (System.Exception e) { UnityEngine.Debug.LogError("[Telegram] " + e.Message); }
        }
    }
}
