using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 59차: 원격 세션이 Tools/blender 의 스크립트를 읽지 못할 때(하드링크 거부) 복사본을 만든다.
    ///   Tools/blender/*.py|*.bat|*.txt → Tools/blender/_copy/ (매번 덮어씀).
    public static class ToolsCopyMenu
    {
        [MenuItem("Coast Run/Dev/Tools - Copy blender scripts")]
        public static void CopyBlenderScripts()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "blender"));
            string dst = Path.Combine(root, "_copy");
            Directory.CreateDirectory(dst);
            int n = 0;
            foreach (var f in Directory.GetFiles(root))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext != ".py" && ext != ".bat" && ext != ".txt") continue;
                string to = Path.Combine(dst, Path.GetFileName(f));
                File.WriteAllBytes(to, File.ReadAllBytes(f));
                n++;
            }
            Debug.LogWarning($"[ToolsCopy] {n} files → {dst}");
        }

        /// 60차: 캡처(Tools/_shots/*.png)도 하드링크로 거부될 때 — 최근 30분 안 캡처를 _shots/_copy/ 로 복사.
        [MenuItem("Coast Run/Dev/Tools - Copy recent shots")]
        public static void CopyRecentShots()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "_shots"));
            string dst = Path.Combine(root, "_copy");
            Directory.CreateDirectory(dst);
            int n = 0; var cutoff = System.DateTime.Now.AddMinutes(-30);
            foreach (var f in Directory.GetFiles(root, "*.png"))
            {
                if (File.GetLastWriteTime(f) < cutoff) continue;
                File.WriteAllBytes(Path.Combine(dst, Path.GetFileName(f)), File.ReadAllBytes(f));
                n++;
            }
            foreach (var f in Directory.GetFiles(root, "*.txt")) File.WriteAllBytes(Path.Combine(dst, Path.GetFileName(f)), File.ReadAllBytes(f));
            Debug.LogWarning($"[ToolsCopy] {n} shots → {dst}");
        }

        /// 반대 방향: _copy/<name> 을 원본 자리에 덮어쓴다(원격이 커밋한 새 스크립트 반영용).
        [MenuItem("Coast Run/Dev/Tools - Apply _copy to blender")]
        public static void ApplyCopy()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "blender"));
            string src = Path.Combine(root, "_copy");
            if (!Directory.Exists(src)) { Debug.LogWarning("[ToolsCopy] no _copy"); return; }
            int n = 0;
            foreach (var f in Directory.GetFiles(src, "*.py"))
            {
                string to = Path.Combine(root, Path.GetFileName(f));
                File.WriteAllBytes(to, File.ReadAllBytes(f));
                n++;
            }
            Debug.LogWarning($"[ToolsCopy] applied {n} files → {root}");
        }
    }
}
