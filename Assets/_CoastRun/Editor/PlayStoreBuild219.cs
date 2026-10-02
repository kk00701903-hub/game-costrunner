using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CoastRun.EditorTools
{
    /// 219차(사용자: 「플레이 스토어 올릴 준비」): 구글 플레이 업로드용 AAB 빌드.
    /// - 업로드 키: Keystore/coastrun_upload.keystore (없으면 JDK keytool 로 만든다). 비밀번호는 .env 의 KEYSTORE_PASS (둘 다 git 제외).
    ///   ※ 이 키스토어와 비밀번호를 잃어버리면 업로드 키 재설정을 신청해야 한다 — 따로 백업할 것.
    /// - AAB + Split Application Binary(설치 시 에셋 팩, 기본 모듈 한도 안에 들도록), IL2CPP ARM64, targetSdk 36(2026-08-31 부터 필수).
    /// - 결과: Builds/CoastRun_play.aab, Builds/last_build_aab.txt
    public static class PlayStoreBuild219
    {
        const string KsDir = "Keystore", KsFile = "Keystore/coastrun_upload.keystore", Alias = "upload";

        static string EnvGet(string key)
        {
            if (!File.Exists(".env")) return null;
            foreach (var l in File.ReadAllLines(".env")) { var i = l.IndexOf('='); if (i > 0 && l.Substring(0, i).Trim() == key) return l.Substring(i + 1).Trim(); }
            return null;
        }
        static void EnvSet(string key, string val)
        {
            var lines = File.Exists(".env") ? File.ReadAllLines(".env").ToList() : new System.Collections.Generic.List<string>();
            int idx = lines.FindIndex(l => l.StartsWith(key + "="));
            if (idx >= 0) lines[idx] = key + "=" + val; else lines.Add(key + "=" + val);
            File.WriteAllLines(".env", lines);
        }

        [MenuItem("Coast Run/Store/219 - Create upload keystore (once)")]
        public static bool EnsureKeystore()
        {
            if (File.Exists(KsFile) && !string.IsNullOrEmpty(EnvGet("KEYSTORE_PASS"))) { Debug.LogWarning("[219] keystore exists: " + Path.GetFullPath(KsFile)); return true; }
            if (File.Exists(KsFile)) { Debug.LogError("[219] keystore 파일은 있는데 .env 에 KEYSTORE_PASS 가 없다 — 새로 만들지 않는다(덮어쓰기 방지)."); return false; }
            Directory.CreateDirectory(KsDir);
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var rng = new System.Random(System.Guid.NewGuid().GetHashCode()); var pw = new string(Enumerable.Range(0, 24).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            string keytool = Path.Combine(UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath, "bin", Application.platform == RuntimePlatform.WindowsEditor ? "keytool.exe" : "keytool");
            var psi = new ProcessStartInfo(keytool, $"-genkeypair -v -keystore \"{Path.GetFullPath(KsFile)}\" -alias {Alias} -keyalg RSA -keysize 2048 -validity 10000 -storepass {pw} -keypass {pw} -dname \"CN=jette, OU=Studio, O=jette, L=Jeju, ST=Jeju, C=KR\"")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var p = Process.Start(psi)) { string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd(); p.WaitForExit(); if (p.ExitCode != 0 || !File.Exists(KsFile)) { Debug.LogError("[219] keytool 실패: " + o); return false; } }
            EnvSet("KEYSTORE_PATH", Path.GetFullPath(KsFile)); EnvSet("KEYSTORE_PASS", pw); EnvSet("KEYSTORE_ALIAS", Alias);
            Debug.LogWarning("[219] upload keystore created → " + Path.GetFullPath(KsFile) + " (비밀번호는 .env KEYSTORE_PASS)");
            return true;
        }

        [MenuItem("Coast Run/Store/219 - Build Play Store AAB (release)")]
        public static void BuildAab()
        {
            string res = "Builds/last_build_aab.txt"; Directory.CreateDirectory("Builds");
            File.WriteAllText(res, "building… " + System.DateTime.Now);
            if (!EnsureKeystore()) { File.WriteAllText(res, "FAIL keystore"); return; }
            BuildTempClean219.CleanGradleBuild();   // 219차: 지난 빌드 Gradle 임시 폴더 삭제(다시 만들어짐, C 드라이브 확보)
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            CoastRun.Editor.BuildMenu.ApplyAlwaysIncludedShaders();
            CoastRun.Editor.BuildMenu.ApplyBranding();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ourfrequency.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;   // 2026-08-31 부터 신규·업데이트 모두 API 36
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode + 1);
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(KsFile);
            PlayerSettings.Android.keystorePass = EnvGet("KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Alias;
            PlayerSettings.Android.keyaliasPass = EnvGet("KEYSTORE_PASS");
            PlayerSettings.Android.splitApplicationBinary = true;   // 설치 시 에셋 팩(PAD) — 기본 모듈을 작게
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            // 226차: 컷씬 그림 fast-follow 팩(CutImages) 다시 만들기
            if (!CutPackBuilder226.BuildPack()) { File.WriteAllText(res, "FAIL cut image pack"); PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = ""; return; }
            var opts = new BuildPlayerOptions { scenes = scenes, locationPathName = "Builds/CoastRun_play.aab", target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.None };
            Debug.Log($"[219] AAB build start v{PlayerSettings.bundleVersion}({PlayerSettings.Android.bundleVersionCode}) scenes={scenes.Length}");
            var r = BuildPipeline.BuildPlayer(opts); var s = r.summary;
            long aab = File.Exists("Builds/CoastRun_play.aab") ? new FileInfo("Builds/CoastRun_play.aab").Length : 0;
            string msg = $"AAB {s.result}: file={aab / 1048576} MB, {s.totalTime.TotalMinutes:0.0} min, errors={s.totalErrors}, warnings={s.totalWarnings}, versionCode={PlayerSettings.Android.bundleVersionCode}, target=36";
            File.WriteAllText(res, msg + "\n" + string.Join("\n", r.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content)));
            // 서명 비밀번호는 에디터 세션에만 — 프로젝트 파일에 남지 않게 비운다
            PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = "";
            if (s.result == BuildResult.Succeeded) Debug.LogWarning("[219] " + msg); else Debug.LogError("[219] " + msg);
        }
    }
}
