#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Android APK 원클릭 빌드 — Builds/CoastRun.apk. 세로 고정, IL2CPP ARM64(+ARMv7), 디버그 키스토어.
    public static class BuildMenu
    {
        private const string Bundle = "com.jette.coastrun";

        [MenuItem("Coast Run/Build/Android APK (IL2CPP, ARM64+ARMv7) %#&k")]
        public static void BuildAndroidApk() => Build(false);

        [MenuItem("Coast Run/Build/Android APK — quick (Mono, ARMv7, dev)")]
        public static void BuildAndroidApkQuick() => Build(true);

        private static void Build(bool quick)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Build: no scenes in Build Settings — run Coast Run/Scenes/Setup first.");
                return;
            }

            PlayerSettings.productName = "너와 나의 주파수";
            PlayerSettings.companyName = "jette";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, Bundle);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode + 1);
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            if (quick)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
            }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(dir);
            string apk = Path.Combine(dir, quick ? "CoastRun_quick.apk" : "CoastRun.apk");

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = quick ? BuildOptions.Development : BuildOptions.None,
            };
            Debug.Log($"Build: Android APK → {apk}  scenes={scenes.Length}  {(quick ? "Mono/ARMv7/dev" : "IL2CPP/ARM64+ARMv7")}");
            var report = BuildPipeline.BuildPlayer(opts);
            var sum = report.summary;
            string msg = $"Build {sum.result}: {sum.totalSize / (1024 * 1024)} MB, {sum.totalTime.TotalMinutes:0.0} min, errors={sum.totalErrors}, warnings={sum.totalWarnings} → {apk}";
            File.WriteAllText(Path.Combine(dir, "last_build.txt"), msg + "\n" +
                string.Join("\n", report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content)));
            if (sum.result == BuildResult.Succeeded) Debug.Log(msg);
            else Debug.LogError(msg);
        }
    }
}
#endif
