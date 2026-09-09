// 25차-1 진단용: 플레이 중 주인공 리그의 렌더러/머티리얼/셰이더/그림자색을 파일로 덤프한다.
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    public static class PlayerMaterialDump
    {
        [MenuItem("Coast Run/Debug/Dump Player Materials")]
        public static void Dump()
        {
            var sb = new StringBuilder();
            var rig = Object.FindFirstObjectByType<SkaterRig>();
            var root = rig != null ? rig.transform : (Object.FindFirstObjectByType<PlayerController>()?.transform);
            if (root == null) { sb.AppendLine("no player"); }
            else
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    sb.AppendLine($"[{r.GetType().Name}] {Path(r.transform, root)} enabled={r.enabled} active={r.gameObject.activeInHierarchy} bounds={r.bounds.size}");
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null) { sb.AppendLine($"    [{i}] null"); continue; }
                        string sc = m.HasProperty("_ShadowColor") ? m.GetColor("_ShadowColor").ToString() : "-";
                        string bc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : (m.HasProperty("_Color") ? m.color.ToString() : "-");
                        string tex = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : (m.mainTexture != null ? m.mainTexture.name : "-");
                        sb.AppendLine($"    [{i}] {m.name} shader={m.shader.name} base={bc} shadow={sc} tex={tex} q={m.renderQueue}");
                    }
                    var smr = r as SkinnedMeshRenderer;
                    if (smr != null && smr.sharedMesh != null)
                        sb.AppendLine($"    mesh={smr.sharedMesh.name} sub={smr.sharedMesh.subMeshCount} verts={smr.sharedMesh.vertexCount}");
                }
            }
            Directory.CreateDirectory("Tools/_shots");
            File.WriteAllText("Tools/_shots/player_mats.txt", sb.ToString());
            Debug.Log("[Dump] Tools/_shots/player_mats.txt\n" + sb);
        }

        static string Path(Transform t, Transform root)
        {
            var s = t.name;
            while (t.parent != null && t != root) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }
    }
}
#endif
