using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 200차(사용자: 「텔레그램에서 입력한 내용이 반영이 안 된다」): 예전엔 「Fetch inbox」 메뉴를 눌러야만 답장을 받아 왔다.
    /// 이제 에디터가 켜져 있으면 90초마다 fetch_inbox.py --ack 를 백그라운드로 돌려 Tools/Telegram/inbox/ 에 저장하고,
    /// 새 요청마다 텔레그램에 「📥 요청 받음」 답장을 보낸다. 실제 작업은 Claude 채팅 세션이 inbox 를 읽고 확인한 뒤 진행한다.
    /// 끄기: 메뉴 Coast Run/Telegram/Auto fetch (on·off).
    [InitializeOnLoad]
    public static class TelegramInboxPoller
    {
        const string Key = "CoastRun.TelegramAutoFetch";
        const double Interval = 90.0;
        static double _next;
        static Process _proc;
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static TelegramInboxPoller()
        {
            _next = EditorApplication.timeSinceStartup + 20.0;
            EditorApplication.update += Tick;
        }

        static bool On => EditorPrefs.GetBool(Key, true);

        [MenuItem("Coast Run/Telegram/Auto fetch (on·off)")]
        static void Toggle() { EditorPrefs.SetBool(Key, !On); UnityEngine.Debug.LogWarning("[Telegram] auto fetch = " + On); }

        static void Tick()
        {
            if (!On || EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + Interval;
            if (_proc != null && !_proc.HasExited) return;
            try
            {
                var psi = new ProcessStartInfo("python", "\"Tools\\Telegram\\fetch_inbox.py\" --ack")
                { WorkingDirectory = Root, UseShellExecute = false, CreateNoWindow = true };
                _proc = Process.Start(psi);
            }
            catch (System.Exception e) { UnityEngine.Debug.LogWarning("[Telegram] auto fetch failed: " + e.Message); _next = EditorApplication.timeSinceStartup + 600.0; }
        }
    }
}
