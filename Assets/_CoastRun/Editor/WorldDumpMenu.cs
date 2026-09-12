using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 59차: 러닝 중 플레이어 앞 도로변 소품을 덤프(이름·부모·크기·색) — 「반복되는 네모 박스」 정체 확인용.
    public static class WorldDumpMenu
    {
        [MenuItem("Coast Run/Dev/World - Dump nearby props")]
        public static void DumpNearby()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[WorldDump] play mode only"); return; }
            var player = Object.FindAnyObjectByType<PlayerController>();
            float pz = player != null ? player.transform.position.z : 0f;
            var sb = new StringBuilder();
            sb.AppendLine($"playerZ={pz:F1}");
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled) continue;
                var b = r.bounds;
                if (b.center.z < pz - 6f || b.center.z > pz + 60f) continue;
                if (Mathf.Abs(b.center.x) > 7f) continue;
                if (b.size.z > 12f || b.size.x > 12f) continue;     // 바닥·긴 벽 제외
                if (b.size.y < 0.25f) continue;                       // 데칼 제외
                string path = r.gameObject.name; var t = r.transform.parent; int d = 0;
                while (t != null && d++ < 4) { path = t.name + "/" + path; t = t.parent; }
                string col = "";
                var m = r.sharedMaterial;
                if (m != null && m.HasProperty("_BaseColor")) { var c = m.GetColor("_BaseColor"); col = $"rgb({c.r:F2},{c.g:F2},{c.b:F2})"; }
                else if (m != null && m.HasProperty("_Color")) { var c = m.color; col = $"rgb({c.r:F2},{c.g:F2},{c.b:F2})"; }
                string mesh = r is MeshRenderer ? (r.GetComponent<MeshFilter>()?.sharedMesh?.name ?? "-") : r.GetType().Name;
                sb.AppendLine($"{path} | z={b.center.z:F1} x={b.center.x:F2} y={b.center.y:F2} | size=({b.size.x:F2},{b.size.y:F2},{b.size.z:F2}) | mesh={mesh} | mat={(m != null ? m.name : "-")} {col}");
            }
            string file = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "_shots", "world_dump.txt"));
            File.WriteAllText(file, sb.ToString());
            Debug.LogWarning($"[WorldDump] {file}");
        }
    }
}
