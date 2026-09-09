using System;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// Firefly 웹에서 생성한 영상(프리사인 URL)을 에디터가 직접 내려받아 Resources/CoastRun/Video 에 넣는다.
    /// Tools/FireflyVideo/fetch.txt — 한 줄에 `파일명<TAB>URL`. 도메인 리로드마다 빠진 파일만 받는다. 결과는 fetch.log.
    [InitializeOnLoad]
    public static class VideoFetch
    {
        const string ListPath = "Tools/FireflyVideo/fetch.txt";
        const string LogPath = "Tools/FireflyVideo/fetch.log";
        const string OutDir = "Assets/Resources/CoastRun/Video";

        static VideoFetch()
        {
            // 24차-12(점검 5): 도메인 리로드마다 텍스트 파일(run.txt)에 적힌 .bat 를 확인 없이 실행하던 자동 실행을 뺀다.
            // 메뉴 Coast Run/Fetch Firefly videos 로만 수동 실행.
        }

        const string RunPath = "Tools/FireflyVideo/run.txt";   // 한 줄: 실행할 .bat 절대경로 — 한 번 실행하고 지운다

        [MenuItem("Coast Run/Fetch Firefly videos")]
        public static void Run()
        {
            if (File.Exists(RunPath))
            {
                string bat = File.ReadAllText(RunPath).Trim();
                File.Delete(RunPath);
                if (File.Exists(bat))
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"") { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(bat) };
                        System.Diagnostics.Process.Start(psi);
                        Debug.Log("[VideoFetch] started " + bat);
                    }
                    catch (Exception e) { Debug.LogWarning("[VideoFetch] run failed: " + e.Message); }
                }
            }
            if (!File.Exists(ListPath)) return;
            var log = new System.Text.StringBuilder();
            bool any = false;
            Directory.CreateDirectory(OutDir);
            foreach (var raw in File.ReadAllLines(ListPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string name = line.Substring(0, tab).Trim();
                string url = line.Substring(tab + 1).Trim();
                string dst = name.Contains("/") ? name : Path.Combine(OutDir, name);   // 경로가 있으면 프로젝트 루트 기준
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                if (File.Exists(dst) && new FileInfo(dst).Length > 1000) { log.AppendLine($"skip {name} (exists)"); continue; }
                try
                {
                    using (var http = new HttpClient())
                    {
                        http.Timeout = TimeSpan.FromSeconds(120);
                        var bytes = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
                        File.WriteAllBytes(dst, bytes);
                        log.AppendLine($"ok {name} {bytes.Length} bytes");
                        any = true;
                    }
                }
                catch (Exception e)
                {
                    log.AppendLine($"FAIL {name}: {e.Message}");
                }
            }
            log.AppendLine("done " + DateTime.Now.ToString("HH:mm:ss"));
            File.WriteAllText(LogPath, log.ToString());
            if (any) AssetDatabase.Refresh();
            Debug.Log("[VideoFetch]\n" + log);
        }
    }
}
