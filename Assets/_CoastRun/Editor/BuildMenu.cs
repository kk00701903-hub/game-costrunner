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

        /// 9차: 스토어 스크린샷 — 플레이 중 게임 뷰를 3배로 캡쳐해 Builds/shots/ 에 저장. (Ctrl+Shift+Alt+S)
        [MenuItem("Coast Run/Screenshot x3 (play mode) %#&s")]
        public static void ShotX3()
        {
            Directory.CreateDirectory("Builds/shots");
            string path = $"Builds/shots/shot_{System.DateTime.Now:HHmmss}.png";
            ScreenCapture.CaptureScreenshot(path, 3);
            Debug.Log("[Screenshot] " + path);
        }

        /// 67차: 브랜딩 — 스플래시는 Unity 로고 없이 「스튜디오 우히히시」 로고만(Unity 6 부터 Personal 도 끌 수 있다), 앱 아이콘은 Art/Brand/AppIcon*.png.
        [MenuItem("Coast Run/Build/Apply branding (splash + icon)")]
        public static void ApplyBranding()
        {
            const string dir = "Assets/_CoastRun/Art/Brand/";
            var logoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Splash_Studio.png");
            if (logoTex != null)
            {
                var imp = AssetImporter.GetAtPath(dir + "Splash_Studio.png") as TextureImporter;
                if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled))
                {
                    imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.mipmapEnabled = false; imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var logo = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "Splash_Studio.png");
                if (logo != null)
                {
                    PlayerSettings.SplashScreen.show = true;
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                    PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
                    PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
                    PlayerSettings.SplashScreen.backgroundColor = new Color(1f, 0.98f, 0.94f);
                    PlayerSettings.SplashScreen.overlayOpacity = 0f;
                    PlayerSettings.SplashScreen.blurBackgroundImage = false;
                    PlayerSettings.SplashScreen.background = null;
                    PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.4f, logo) };
                }
            }
            else Debug.LogWarning("[Branding] Splash_Studio.png 없음 — 스플래시 그대로");

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "AppIcon.png");
            var iconFg = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "AppIcon_Fg.png") ?? icon;
            var iconBg = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "AppIcon_Bg.png");
            if (icon != null)
            {
                foreach (string f in new[] { "AppIcon.png", "AppIcon_Fg.png", "AppIcon_Bg.png" })
                {
                    var ti = AssetImporter.GetAtPath(dir + f) as TextureImporter;
                    if (ti != null && (ti.mipmapEnabled || ti.textureCompression != TextureImporterCompression.Uncompressed))
                    { ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
                }
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
                var nbt = UnityEditor.Build.NamedBuildTarget.Android;
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(nbt))
                {
                    var icons = PlayerSettings.GetPlatformIcons(nbt, kind);
                    foreach (var ic in icons)
                    {
                        var texs = new Texture2D[ic.maxLayerCount];
                        for (int i = 0; i < texs.Length; i++)
                            texs[i] = texs.Length >= 2 ? (i == 0 ? (iconBg ?? icon) : iconFg) : icon;   // adaptive: 0 = 배경, 1 = 전경
                        ic.SetTextures(texs);
                    }
                    PlayerSettings.SetPlatformIcons(nbt, kind, icons);
                }
                Debug.Log("[Branding] 아이콘 적용: " + icon.width + "px");
            }
            else Debug.LogWarning("[Branding] AppIcon.png 없음 — 아이콘 그대로");
            AssetDatabase.SaveAssets();
        }

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
            // 67차-1(사용자: 폰에서 러닝 화면이 안 보임): 엔진 코드 스트리핑이 MeshCollider 등을 빼 버려 CreatePrimitive 가 실패 → 월드/캐릭터 생성이 깨졌다.
            //   스트리핑을 끄고(APK 몇 MB 증가), Assets/link.xml + DeviceBoot.KeepTypes 로 이중 보호.
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            ApplyBranding();   // 67차-2/3: 스플래시(Unity 로고 → 스튜디오 우히히시) + 앱 아이콘
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
