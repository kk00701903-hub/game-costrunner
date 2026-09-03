using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// Builds the WebGL player for static hosting (GitHub Pages).
    public static class CoastRunWebGLBuild
    {
        private const string OutRel = "Build/WebGL";

        [MenuItem("Tools/Coast Run/Build WebGL (Pages)")]
        public static void BuildMenu()
        {
            Debug.Log(Build());
        }

        /// Entry point for -executeMethod in batch mode.
        public static void BuildBatch()
        {
            string report = Build();
            Debug.Log(report);
            EditorApplication.Exit(report.Contains("RESULT=OK") ? 0 : 1);
        }

        public static string Build()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(projectRoot, OutRel);

            var scenes = CollectScenes();
            if (scenes.Count == 0)
                return "RESULT=FAIL\nNo enabled scenes in Build Settings.";

            ApplyPlayerSettings();

            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;

            if (ok)
                WriteHostingFiles(outDir);

            var text = new System.Text.StringBuilder();
            text.AppendLine("=== Coast Run WebGL Build ===");
            text.AppendLine(System.DateTime.Now.ToString("u"));
            text.AppendLine("out=" + outDir);
            text.AppendLine("scenes=" + string.Join(", ", scenes));
            text.AppendLine("result=" + summary.result);
            text.AppendLine("sizeBytes=" + summary.totalSize);
            text.AppendLine("time=" + summary.totalTime);
            text.AppendLine("errors=" + summary.totalErrors);
            text.AppendLine(ok ? "RESULT=OK" : "RESULT=FAIL");
            return text.ToString();
        }

        private static List<string> CollectScenes()
        {
            var enabled = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                    enabled.Add(s.path);
            }
            return enabled;
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Coast Run";
            PlayerSettings.companyName = "CoastRun";
            PlayerSettings.runInBackground = true;

            // Portrait framing the game was authored against; the WebGL template
            // letterboxes this aspect into the browser viewport.
            PlayerSettings.defaultWebScreenWidth = 720;
            PlayerSettings.defaultWebScreenHeight = 1280;

            PlayerSettings.WebGL.template = "PROJECT:CoastRunMobile";
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;

            // GitHub Pages cannot set Content-Encoding, so gzip the payload and let
            // Unity ship the JS fallback decompressor instead of relying on headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // SharedArrayBuffer needs COOP/COEP headers that Pages does not send.
            PlayerSettings.WebGL.threadsSupport = false;

            PlayerSettings.SetIl2CppCompilerConfiguration(
                NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        }

        private static void WriteHostingFiles(string outDir)
        {
            // Without this, Pages runs Jekyll and drops files Unity needs.
            File.WriteAllText(Path.Combine(outDir, ".nojekyll"), string.Empty);

            // SPA-style fallback so a refresh on a subpath still loads the game.
            string index = Path.Combine(outDir, "index.html");
            if (File.Exists(index))
                File.Copy(index, Path.Combine(outDir, "404.html"), true);
        }
    }
}
