using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 201차: 원격 세션 VM 에서는 git status 가 너무 느려서(마운트 폴더) PC 쪽 git 을 에디터에서 돌린다.
    /// Tools/_xfer/git_cmds.txt 의 줄을 한 줄씩 `git <줄>` 로 실행하고, 결과를 Tools/_xfer/git.result 에 쓴다(백그라운드).
    public static class GitRunner
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Coast Run/Dev/Git - Run Tools/_xfer/git_cmds.txt")]
        public static void Run()
        {
            string cmds = Path.Combine(Root, "Tools/_xfer/git_cmds.txt"), res = Path.Combine(Root, "Tools/_xfer/git.result");
            if (!File.Exists(cmds)) { File.WriteAllText(res, "ERR no git_cmds.txt"); return; }
            var lines = File.ReadAllLines(cmds, Encoding.UTF8);
            File.WriteAllText(res, "RUNNING\n", Encoding.UTF8);
            var th = new System.Threading.Thread(() =>
            {
                var sb = new StringBuilder();
                foreach (var raw in lines)
                {
                    var line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#")) continue;
                    sb.Append("$ git ").Append(line).Append('\n');
                    try
                    {
                        var psi = new ProcessStartInfo("git", line)
                        { WorkingDirectory = Root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
                        using (var p = Process.Start(psi))
                        {
                            var o = p.StandardOutput.ReadToEndAsync(); var e = p.StandardError.ReadToEndAsync();
                            p.WaitForExit(600000);
                            string outp = o.Result, err = e.Result;
                            if (outp.Length > 20000) outp = outp.Substring(0, 20000) + "\n…(생략)";
                            sb.Append(outp).Append(err).Append("[exit ").Append(p.HasExited ? p.ExitCode : -1).Append("]\n");
                            if (p.HasExited && p.ExitCode != 0) break;
                        }
                    }
                    catch (System.Exception ex) { sb.Append("ERR ").Append(ex.Message).Append('\n'); break; }
                }
                sb.Append("DONE\n");
                File.WriteAllText(res, sb.ToString(), Encoding.UTF8);
            });
            th.IsBackground = true; th.Start();
        }
    }
}
