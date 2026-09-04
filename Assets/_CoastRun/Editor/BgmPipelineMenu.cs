#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// One-click BGM pipeline (ACE-Step 1.5, Tools/AceStepGen). Each entry launches the
    /// matching batch file in its own console so the editor never blocks; the batch
    /// files write logs next to themselves (install_log.txt / api_log.txt / gen_log.txt).
    public static class BgmPipelineMenu
    {
        private static string ToolDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "AceStepGen"));

        [MenuItem("Coast Run/BGM/1. Install ACE-Step (CPU, ~12GB download)")]
        public static void Install() => Launch("install_cpu.bat");

        [MenuItem("Coast Run/BGM/2. Start ACE-Step API server")]
        public static void StartApi() => Launch("start_api_cpu.bat");

        [MenuItem("Coast Run/BGM/3. Generate BGM_Menu (test track)")]
        public static void GenerateMenu() => Launch("gen_menu.bat");

        [MenuItem("Coast Run/BGM/4. Generate P0 tracks + stems")]
        public static void GenerateP0() => Launch("run_p0.bat");

        [MenuItem("Coast Run/BGM/5. Re-split chapter stems (no render)")]
        public static void Resplit() => Launch("resplit.bat");

        [MenuItem("Coast Run/BGM/Open tool folder")]
        public static void OpenFolder() => EditorUtility.RevealInFinder(ToolDir);

        [MenuItem("Coast Run/BGM/Probe environment")]
        public static void Probe() => Launch("probe_env.bat");

        [MenuItem("Coast Run/BGM/Repair torch versions")]
        public static void FixTorch() => Launch("fix_torch.bat");

        private static void Launch(string bat)
        {
            string path = Path.Combine(ToolDir, bat);
            if (!File.Exists(path))
            {
                UnityEngine.Debug.LogError($"BGM pipeline: {path} not found.");
                return;
            }

            var psi = new ProcessStartInfo("cmd.exe", $"/c start \"ACE-Step {bat}\" \"{path}\"")
            {
                WorkingDirectory = ToolDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi);
            UnityEngine.Debug.Log($"BGM pipeline: launched {bat} — log in {ToolDir}");
        }
    }
}
#endif
