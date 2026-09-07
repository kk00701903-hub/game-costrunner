using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 텔레그램 보고 워처를 에디터에서 켠다(Tools/Telegram/watch.py). Cowork 세션의 샌드박스·VM은
    /// api.telegram.org 가 막혀 있어서 PC에서 도는 이 워처가 큐(queue.txt)를 실제로 보낸다.
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
    }
}
