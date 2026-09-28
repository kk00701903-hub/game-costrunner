#if UNITY_EDITOR
using System.Collections.Generic;
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

        // 82차: GraphicsSettings Always Included — GUID 로 고정해 빌드마다 되돌아가는 회귀 방지.
        private static readonly string[] AlwaysIncludedGuids =
        {
            "933532a4fcc9baf4fa0491de14d08ed7", // URP Lit
            "8d2bb70cbf9db8d4da26e15b26e74248", // URP Simple Lit
            "650dd9526735d5b46b79224bc6e94025", // URP Unlit
            "0406db5a14f94604a8c57ccfbc9f3b46", // URP Particles/Unlit
            "26082a001c76cd74e93465ea0efaf12b", // CoastRun/ChromaUnlit
            "469ae6d6502344340817f98f2a758fc1", // CoastRun/UnlitCurved
            "423505ce6d2d39540b356891aef87c7c", // CoastRun/ToonLit
            "7fc3f2d63771496b81faa4d6fbc284ce", // CoastRun/InkOutline
            "ce98457354cb3e543ba64bcbc39293e0", // CoastRun/UIDesaturate
        };

        [MenuItem("Coast Run/Build/Android APK (IL2CPP, ARM64+ARMv7) %#&k")]
        public static void BuildAndroidApk() => Build(BuildKind.Release);

        [MenuItem("Coast Run/Build/Android APK — Development (IL2CPP, ARM64+ARMv7)")]
        public static void BuildAndroidApkDevelopment() => Build(BuildKind.Development);

        /// x86_64 for Android Emulator (ARM APKs SIGILL under native_bridge on API30 x86 images).
        [MenuItem("Coast Run/Build/Android APK — Emulator (IL2CPP, x86_64)")]
        public static void BuildAndroidApkEmulator() => Build(BuildKind.Emulator);

        [MenuItem("Coast Run/Build/Android APK — quick (Mono, ARMv7, dev)")]
        public static void BuildAndroidApkQuick() => Build(BuildKind.Quick);

        private enum BuildKind { Release, Development, Emulator, Quick }

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
                            texs[i] = texs.Length >= 2 ? (i == 0 ? (iconBg ?? icon) : iconFg) : icon;
                        ic.SetTextures(texs);
                    }
                    PlayerSettings.SetPlatformIcons(nbt, kind, icons);
                }
                Debug.Log("[Branding] 아이콘 적용: " + icon.width + "px");
            }
            else Debug.LogWarning("[Branding] AppIcon.png 없음 — 아이콘 그대로");
            AssetDatabase.SaveAssets();
        }

        /// Forces URP + CoastRun shaders into GraphicsSettings Always Included (survives editor resets).
        public static void ApplyAlwaysIncludedShaders()
        {
            var graphics = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(graphics);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null || !arr.isArray)
            {
                Debug.LogWarning("[Build] m_AlwaysIncludedShaders missing");
                return;
            }

            var keep = new List<Shader>();
            for (int i = 0; i < arr.arraySize; i++)
            {
                var sh = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (sh != null && !keep.Contains(sh)) keep.Add(sh);
            }

            int added = 0;
            foreach (var guid in AlwaysIncludedGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("[Build] Always Included GUID missing: " + guid);
                    continue;
                }
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (sh == null) continue;
                if (keep.Contains(sh)) continue;
                keep.Add(sh);
                added++;
            }

            arr.arraySize = keep.Count;
            for (int i = 0; i < keep.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = keep[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] Always Included shaders locked (total={keep.Count}, added={added})");
        }

        /// 215차: IL2CPP .dbg/.sym.so 를 만들지 않게 해 C: 디스크 부족으로 빌드가 깨지지 않게 한다.
        static void DisableAndroidDebugSymbols()
        {
            try
            {
                var t = System.Type.GetType("UnityEditor.Android.UserBuildSettings, UnityEditor.Android.Extensions");
                var dbgProp = t?.GetProperty("DebugSymbols", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var dbg = dbgProp?.GetValue(null);
                var levelProp = dbg?.GetType().GetProperty("level");
                if (levelProp != null)
                {
                    var none = System.Enum.Parse(levelProp.PropertyType, "None");
                    levelProp.SetValue(dbg, none);
                    Debug.Log("[Build] DebugSymbols.level = None");
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[Build] DebugSymbols.level 설정 실패: " + e.Message); }
#pragma warning disable CS0618
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            EditorUserBuildSettings.androidCreateSymbolsZip = false;
#pragma warning restore CS0618
        }

        private static void Build(BuildKind kind)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Build: no scenes in Build Settings — run Coast Run/Scenes/Setup first.");
                return;
            }

            ApplyAlwaysIncludedShaders();

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
            // preferExternal 는 SD 없는 기기에서 설치 실패하는 경우가 있어 Auto(내부 저장소)로 고정
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode + 1);
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.stripEngineCode = false;
            // 82차 shader-null 재현 메모: Managed Stripping 을 Minimal 로 낮춰 빌드해도 ArgumentNullException(shader) 가
            // 그대로면 → Always Included / Resources 미포함. Minimal 에서만 사라지고 Low+ 에서 나면 stripping 영향.
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            ApplyBranding();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            DisableAndroidDebugSymbols(); // 215차: 디스크 부족 시 .dbg/.sym.so 복사 실패 방지

            string apkName;
            BuildOptions optsFlags = BuildOptions.None;
            string kindLabel;
            switch (kind)
            {
                case BuildKind.Quick:
                    // Mono는 ARMv7만 나와 최신 폰(ARM64-only)에 설치가 거부된다 → IL2CPP ARM64(+v7) 로 맞춤
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                    PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
                    apkName = "CoastRun_quick.apk";
                    optsFlags = BuildOptions.Development;
                    kindLabel = "IL2CPP/ARM64+ARMv7/dev";
                    break;
                case BuildKind.Emulator:
                    // Unity 6000.5+: AndroidArchitecture.X86_64 is obsolete / stripped from builds
                    // ("Target architecture x86_64 is no longer supported"). Emulator visual tests
                    // must use an ARM64 AVD (CoastRun_ARM64) + this ARM APK (same as release).
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures =
                        AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                    Debug.Log("[Build] Emulator (=ARM for ARM AVD) targetArchitectures=" + PlayerSettings.Android.targetArchitectures);
                    PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
                    apkName = "CoastRun_emu.apk";
                    kindLabel = "IL2CPP/Emulator-ARM/" + PlayerSettings.Android.targetArchitectures;
                    break;
                case BuildKind.Development:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                    PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Debug);
                    apkName = "CoastRun_dev.apk";
                    optsFlags = BuildOptions.Development | BuildOptions.AllowDebugging;
                    kindLabel = "IL2CPP/ARM64+ARMv7/Development";
                    break;
                default:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    // 215차: 디스크 부족으로 듀얼 아키텍처 IL2CPP 링크/심볼 복사 실패 → ARM64만(최신 폰 OK). 공간 여유 생기면 ARMv7 복원.
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
                    apkName = "CoastRun.apk";
                    kindLabel = "IL2CPP/ARM64";
                    break;
            }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(dir);
            string apk = Path.Combine(dir, apkName);

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = optsFlags,
            };
            Debug.Log($"Build: Android APK → {apk}  scenes={scenes.Length}  {kindLabel}");
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
