#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 191차(사용자: 「하드디스크 분석해서 용량 확보」): 게임 프로젝트 폴더 안 폴더별 크기 → Tools/_disk.txt (프로젝트 폴더만 잰다)
    public static class DiskUsageDev
    {
        static long Size(string dir)
        {
            long s = 0;
            try { foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)) { try { s += new FileInfo(f).Length; } catch { } } } catch { }
            return s;
        }
        /// 빌드 중간 캐시(Library/Bee = IL2CPP·안드로이드 빌드 산출물, PlayerDataCache) — 지워도 다음 빌드 때 다시 만들어진다(첫 빌드만 오래 걸림)
        [MenuItem("Coast Run/Dev/191 - Clear build caches (Bee, PlayerDataCache)")]
        public static void ClearBuildCaches()
        {
            if (BuildPipeline.isBuildingPlayer) { Debug.LogWarning("[Disk] building — skip"); return; }
            string lib = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library"));
            foreach (var n in new[] { "Bee", "PlayerDataCache" })
            {
                var d = Path.Combine(lib, n); if (!Directory.Exists(d)) continue;
                long before = Size(d);
                try { Directory.Delete(d, true); Debug.LogWarning($"[Disk] deleted {n} {before / 1e9:F2} GB"); }
                catch (System.Exception e) { Debug.LogWarning($"[Disk] {n} partial: {e.Message} (left {Size(d) / 1e9:F2} GB of {before / 1e9:F2})"); }
            }
            Run();
        }
        [MenuItem("Coast Run/Dev/191 - Disk usage (project)")]
        public static void Run()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var sb = new StringBuilder();
            var drive = new DriveInfo(Path.GetPathRoot(root));
            sb.AppendLine($"drive {drive.Name} free {drive.AvailableFreeSpace / 1e9:F2} GB / total {drive.TotalSize / 1e9:F1} GB");
            void Level(string dir, int depth, long minBytes)
            {
                var rows = Directory.GetDirectories(dir).Select(d => (d, Size(d))).OrderByDescending(x => x.Item2).ToList();
                foreach (var (d, s) in rows)
                {
                    if (s < minBytes) continue;
                    sb.AppendLine($"{new string(' ', depth * 2)}{s / 1e6,10:F0} MB  {d.Substring(root.Length)}");
                    if (depth < 1 && s > 300e6) Level(d, depth + 1, 50_000_000);
                }
                if (depth == 0) foreach (var f in Directory.GetFiles(dir)) { var l = new FileInfo(f).Length; if (l > 50e6) sb.AppendLine($"{l / 1e6,10:F0} MB  {f.Substring(root.Length)} (file)"); }
            }
            Level(root, 0, 20_000_000);
            File.WriteAllText(Path.Combine(root, "Tools/_disk.txt"), sb.ToString());
            Debug.LogWarning("[Disk] written Tools/_disk.txt");
        }
    }
}
#endif
