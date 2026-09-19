
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.EditorTools
{
    /// 136차: 화면의 모든 uGUI Text를 훑어 글자가 상자 밖으로 새거나 잘린 것을 파일로 기록한다.
    public static class TextOverflowAudit
    {
        [MenuItem("Coast Run/Dev/Audit - Text overflow", priority = 900)]
        public static void Run()
        {
            var sb = new StringBuilder();
            int bad = 0, total = 0;
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                var cv = t.canvas; if (cv == null || !cv.enabled) continue;
                var cg = t.GetComponentInParent<CanvasGroup>(); if (cg != null && cg.alpha < 0.05f) continue;
                total++;
                var rt = t.rectTransform; var r = rt.rect;
                if (r.width < 2f || r.height < 2f) continue;
                var settings = t.GetGenerationSettings(r.size);
                var gen = new TextGenerator();
                gen.Populate(t.text, settings);
                float ppu = t.pixelsPerUnit; if (ppu <= 0f) ppu = 1f;
                var verts = gen.verts;
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                int shown = 0;
                for (int i = 0; i < verts.Count; i += 4)
                {
                    var a = verts[i].position; var c = verts[i + 2].position;
                    if (Mathf.Abs(c.x - a.x) < 0.01f) continue;   // 공백/보이지 않는 글자
                    shown++;
                    minX = Mathf.Min(minX, a.x / ppu); maxX = Mathf.Max(maxX, c.x / ppu);
                    minY = Mathf.Min(minY, c.y / ppu); maxY = Mathf.Max(maxY, a.y / ppu);
                }
                if (shown == 0) continue;
                int visibleChars = gen.characterCountVisible;
                int nonSpace = 0; foreach (var ch in t.text) if (!char.IsWhiteSpace(ch)) nonSpace++;
                int visNonSpace = 0; for (int i = 0; i < Mathf.Min(visibleChars, t.text.Length); i++) if (!char.IsWhiteSpace(t.text[i])) visNonSpace++;
                bool truncated = visNonSpace < nonSpace;
                const float tol = 1.5f;
                bool hOver = minX < r.xMin - tol || maxX > r.xMax + tol;
                bool vOver = minY < r.yMin - 3f || maxY > r.yMax + 3f;   // 줄간격 반올림 오차 허용
                if (!hOver && !vOver && !truncated) continue;
                bad++;
                sb.Append(hOver ? "[H]" : "   ").Append(vOver ? "[V]" : "   ").Append(truncated ? "[CUT]" : "     ");
                sb.Append(' ').Append(Path(t.transform)).Append("  rect=").Append(r.width.ToString("0")).Append('x').Append(r.height.ToString("0"));
                sb.Append(" text=").Append((maxX - minX).ToString("0")).Append('x').Append((maxY - minY).ToString("0"));
                sb.Append(" font=").Append(t.fontSize).Append(t.resizeTextForBestFit ? "(bf" + t.resizeTextMinSize + "-" + t.resizeTextMaxSize + ")" : "");
                sb.Append(" h=").Append(t.horizontalOverflow).Append(" v=").Append(t.verticalOverflow);
                sb.Append(" vis=").Append(visNonSpace).Append('/').Append(nonSpace);
                sb.Append("  \"").Append(t.text.Replace("\n", "\\n")).Append("\"\n");
            }
            var dir = System.IO.Path.Combine(Application.dataPath, "..", "Tools", "_shots");
            Directory.CreateDirectory(dir);
            var file = System.IO.Path.Combine(dir, "text_audit.txt");
            File.AppendAllText(file, $"===== {System.DateTime.Now:HH:mm:ss} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} texts={total} bad={bad}\n" + sb, Encoding.UTF8);
            Debug.Log($"[TextAudit] texts={total} bad={bad} -> {file}");
        }

        static string Path(Transform tr)
        {
            var parts = new List<string>();
            int n = 0;
            while (tr != null && n++ < 6) { parts.Insert(0, tr.name); tr = tr.parent; }
            return string.Join("/", parts);
        }
    }
}
