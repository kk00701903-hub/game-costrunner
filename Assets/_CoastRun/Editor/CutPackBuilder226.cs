using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 226차(사용자: 「한번에 AAB 받으면 용량이 크니 나눠서 받게」): 컷씬 그림 대부분을 Play Asset Delivery fast-follow 팩 「CutImages」로.
    ///   ① 「226 - Move late cut images to pack source」(한 번만): 첫 이야기(오프닝·기억 조각·CS1·EV1·MV·프롤로그·1~2장 VN·시네마 목록 표지)에
    ///      안 쓰는 그림을 Resources/CoastRun/컷씬이미지/** → Assets/_CoastRun/CutPackSrc/** 로 옮긴다(AssetDatabase.MoveAsset — .meta·압축 설정 그대로, 삭제 없음).
    ///      옮긴 그림 목록은 Resources/CoastRun/컷씬이미지/_packindex.txt (컷 id \t 에셋 경로).
    ///   ② 「226 - Build cut image pack」(AAB 빌드 때 자동): _packindex 의 그림을 AssetBundle(cutimages, LZ4) 하나로 만들어
    ///      Assets/CutImages.androidpack/src/main/assets/cutimages.bundle + build.gradle(fast-follow) 로 둔다.
    ///   되돌리기: 「226 - Restore cut images to Resources」 — 옮긴 그림을 원래 자리로.
    public static class CutPackBuilder226
    {
        const string ResRoot = "Assets/Resources/CoastRun/컷씬이미지";
        const string SrcRoot = "Assets/_CoastRun/CutPackSrc";
        const string PackDir = "Assets/CutImages.androidpack";
        const string PackIndex = ResRoot + "/_packindex.txt";
        const string MoveLog = SrcRoot + "/_moved226.txt";   // 새 경로 \t 원래 경로 (되돌리기용)

        // 설치하자마자 볼 수 있는 장면 — 본체에 남김
        static readonly string[] EarlyDefs = { "OPEN", "OPEN_F1", "OPEN_F2", "OPEN_F3", "CS1", "EV1", "MV" };
        static readonly string[] EarlyVn = { "PRO", "CH01_Open", "CH01_Close", "CH02_Open", "CH02_Close" };

        static HashSet<string> EarlyIds()
        {
            var set = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var d in EarlyDefs) foreach (var id in CutPack.IdsOf(CinematicTable.Get(d))) set.Add(id);
            foreach (var v in EarlyVn) foreach (var id in CutPack.IdsOf(ChapterScript.Get(v), v)) set.Add(id);
            // 스크립트 안에 그대로 적힌 컷 id(시네마 목록 표지 등)
            foreach (var f in Directory.GetFiles("Assets/_CoastRun/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string n = Path.GetFileName(f);
                if (n == "CinematicTable.cs" || n.StartsWith("ChapterScript")) continue;
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(f), "\"(Cut_[A-Za-z0-9_]+)\""))
                    set.Add(m.Groups[1].Value);
            }
            return set;
        }

        static List<string> Folders()
        {
            var folders = new List<string>();
            string pathsFile = Path.Combine(ResRoot, "_paths.txt");
            if (File.Exists(pathsFile)) foreach (var l in File.ReadAllLines(pathsFile, Encoding.UTF8)) { var t = l.Trim().TrimStart('﻿'); if (t.Length > 0) folders.Add(t); }
            else folders.Add("CoastRun/컷씬이미지");
            return folders;
        }

        static bool IsImg(string f) { string e = Path.GetExtension(f).ToLowerInvariant(); return e == ".jpg" || e == ".png" || e == ".jpeg"; }
        static string IdOf(string file) { string n = Path.GetFileNameWithoutExtension(file); int i = n.LastIndexOf("Cut_", System.StringComparison.Ordinal); return i < 0 ? null : n.Substring(i); }

        [MenuItem("Coast Run/Store/226 - Move late cut images to pack source")]
        public static void MoveLate()
        {
            var early = EarlyIds();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            var moves = new List<(string from, string to, string id, bool first)>();
            foreach (var f in Folders())
            {
                string dir = "Assets/Resources/" + f;
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir).Where(IsImg).Select(p => p.Replace('\\', '/')).ToArray();
                System.Array.Sort(files, System.StringComparer.Ordinal);
                foreach (var file in files)
                {
                    string id = IdOf(file); if (id == null) continue;
                    bool first = seen.Add(id);
                    if (early.Contains(id)) continue;   // 처음 쓰는 장면 그림(중복 사본 포함)은 본체에
                    string rel = file.Substring(ResRoot.Length + 1);
                    moves.Add((file, SrcRoot + "/" + rel, id, first));
                }
            }
            long bytes = 0; foreach (var m in moves) bytes += new FileInfo(m.from).Length;
            // 폴더 먼저 만들기
            foreach (var d in moves.Select(m => Path.GetDirectoryName(m.to).Replace('\\', '/')).Distinct()) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
            var log = new StringBuilder(); var idx = new StringBuilder();
            if (File.Exists(PackIndex)) idx.Append(File.ReadAllText(PackIndex, Encoding.UTF8));
            if (File.Exists(MoveLog)) log.Append(File.ReadAllText(MoveLog, Encoding.UTF8));
            int ok = 0, fail = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var m in moves)
                {
                    string err = AssetDatabase.MoveAsset(m.from, m.to);
                    if (!string.IsNullOrEmpty(err)) { fail++; Debug.LogWarning($"[CutPack226] move fail {m.from}: {err}"); continue; }
                    ok++; log.Append(m.to).Append('\t').Append(m.from).Append('\n');
                    if (m.first) idx.Append(m.id).Append('\t').Append(m.to).Append('\n');
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            File.WriteAllText(MoveLog, log.ToString(), new UTF8Encoding(false));
            File.WriteAllText(PackIndex, idx.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            CutIndexBuilder220.Build();
            string msg = $"[CutPack226] early ids={early.Count}, moved {ok} (fail {fail}), {bytes / 1048576f:0.0} MB source → {SrcRoot}";
            Directory.CreateDirectory("Builds/qa"); File.WriteAllText("Builds/qa/cutpack226_move.txt", msg + "\n");
            Debug.Log(msg);
        }

        [MenuItem("Coast Run/Store/226 - Restore cut images to Resources")]
        public static void RestoreAll()
        {
            if (!File.Exists(MoveLog)) { Debug.Log("[CutPack226] nothing to restore"); return; }
            int ok = 0;
            foreach (var l in File.ReadAllLines(MoveLog, Encoding.UTF8))
            {
                int t = l.IndexOf('\t'); if (t <= 0) continue;
                string to = l.Substring(0, t), from = l.Substring(t + 1);
                if (!File.Exists(to)) continue;
                if (string.IsNullOrEmpty(AssetDatabase.MoveAsset(to, from))) ok++;
            }
            File.WriteAllText(MoveLog, "", new UTF8Encoding(false));
            File.WriteAllText(PackIndex, "", new UTF8Encoding(false));
            AssetDatabase.Refresh(); CutIndexBuilder220.Build();
            Debug.Log($"[CutPack226] restored {ok}");
        }

        /// 확인용: 안드로이드 플레이어 스크립트만 컴파일(#if UNITY_ANDROID 코드 점검) → Builds/qa/cutpack226_compile.txt
        [MenuItem("Coast Run/Store/226 - Compile Android player scripts")]
        public static void CompileAndroid()
        {
            var set = new UnityEditor.Build.Player.ScriptCompilationSettings { target = BuildTarget.Android, group = BuildTargetGroup.Android, options = UnityEditor.Build.Player.ScriptCompilationOptions.None };
            string tmp = "Temp/cp226_player"; Directory.CreateDirectory(tmp);
            var r = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(set, tmp);
            string msg = $"assemblies={r.assemblies?.Count ?? 0}";
            Directory.CreateDirectory("Builds/qa"); File.WriteAllText("Builds/qa/cutpack226_compile.txt", msg + "\n");
            Debug.Log("[CutPack226] player compile " + msg);
        }

        /// 확인용: 모든 컷씬·VN 장면이 쓰는 그림 id 가 (본체 목록 ∪ 팩 목록)에 있으면 실제로 불러와지는지 → Builds/qa/cutpack226_verify.txt
        [MenuItem("Coast Run/Store/226 - Verify cut images load")]
        public static void Verify()
        {
            var known = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var f in new[] { ResRoot + "/_index.txt", PackIndex })
                if (File.Exists(f)) foreach (var l in File.ReadAllLines(f, Encoding.UTF8)) { int t = l.IndexOf('\t'); if (t > 0) known.Add(l.Substring(0, t).Trim().TrimStart('\uFEFF')); }
            var ids = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var d in new[] { "OPEN", "OPEN_F1", "OPEN_F2", "OPEN_F3", "CS1", "CS2", "CS3", "CS4", "CS5", "CS6", "CS7", "CS8", "EV1", "EV2", "EV3", "EV4", "EV5", "EV6", "EV7", "EV8", "EV9", "EV10", "END_A", "END_B", "END_TRUE", "MV" })
                foreach (var id in CutPack.IdsOf(CinematicTable.Get(d))) ids.Add(id);
            foreach (var kv in ChapterScript.Scenes) foreach (var id in CutPack.IdsOf(kv.Value, kv.Key)) ids.Add(id);
            int used = 0, ok = 0, inPack = 0, absent = 0; var bad = new List<string>();
            foreach (var id in ids)
            {
                if (!known.Contains(id)) { absent++; continue; }   // 원래 그림이 없는 id(없으면 대체 화면 — 이전과 같음)
                used++; if (CutPack.InPack(id)) inPack++;
                if (ArtAssets.LoadTexture(id) != null) ok++; else bad.Add(id);
            }
            string msg = $"[CutPack226] verify: referenced ids={ids.Count}, with image={used}, loaded={ok} (from pack {inPack}), FAILED={bad.Count}, no-image ids(unchanged)={absent}" + (bad.Count > 0 ? "\n" + string.Join("\n", bad) : "");
            Directory.CreateDirectory("Builds/qa"); File.WriteAllText("Builds/qa/cutpack226_verify.txt", msg + "\n");
            Debug.Log(msg);
        }

        /// AAB 빌드 직전에 부른다. 팩에 넣을 그림이 없으면 팩 폴더도 비운다(true = 계속 빌드).
        [MenuItem("Coast Run/Store/226 - Build cut image pack")]
        public static void BuildPackMenu() { BuildPack(); }

        public static bool BuildPack()
        {
            var names = new List<string>(); var paths = new List<string>();
            if (File.Exists(PackIndex))
                foreach (var l in File.ReadAllLines(PackIndex, Encoding.UTF8))
                {
                    int t = l.IndexOf('\t'); if (t <= 0) continue;
                    string id = l.Substring(0, t).Trim().TrimStart('﻿'), p = l.Substring(t + 1).Trim();
                    if (!File.Exists(p)) { Debug.LogError($"[CutPack226] missing {p}"); return false; }
                    names.Add(id); paths.Add(p);
                }
            string assetsDir = PackDir + "/src/main/assets";
            if (names.Count == 0)
            {
                if (Directory.Exists(PackDir)) { AssetDatabase.DeleteAsset(PackDir); }
                Debug.Log("[CutPack226] no pack images — pack removed"); return true;
            }
            string outDir = "Builds/cutpack226";
            Directory.CreateDirectory(outDir);
            var build = new AssetBundleBuild { assetBundleName = "cutimages", assetNames = paths.ToArray(), addressableNames = names.ToArray() };
            var man = BuildPipeline.BuildAssetBundles(outDir, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.Android);
            string bundle = Path.Combine(outDir, "cutimages");
            if (man == null || !File.Exists(bundle)) { Debug.LogError("[CutPack226] bundle build failed"); return false; }
            Directory.CreateDirectory(assetsDir);
            File.Copy(bundle, Path.Combine(assetsDir, CutPack.BundleFile), true);
            File.WriteAllText(PackDir + "/build.gradle",
                "apply plugin: 'com.android.asset-pack'\n\nassetPack {\n    packName = \"" + CutPack.PackName + "\"\n    dynamicDelivery {\n        deliveryType = \"fast-follow\"\n    }\n}\n", new UTF8Encoding(false));
            AssetDatabase.Refresh();
            long sz = new FileInfo(Path.Combine(assetsDir, CutPack.BundleFile)).Length;
            string msg = $"[CutPack226] pack {CutPack.PackName}: {names.Count} images, bundle {sz / 1048576f:0.0} MB (fast-follow)";
            Directory.CreateDirectory("Builds/qa"); File.WriteAllText("Builds/qa/cutpack226_build.txt", msg + "\n");
            Debug.Log(msg);
            return true;
        }
    }
}
