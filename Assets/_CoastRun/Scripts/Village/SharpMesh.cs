using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 151차: Resources/CoastRun/Models/Sharp/<model>.fbx(블렌더 sharpen_assets.py: 40° 각 스무딩 + AO 정점색)가 있으면
    /// 스폰된 소품의 메시를 같은 이름의 구운 메시로 바꾼다. 재질·계층은 그대로.
    public static class SharpMesh
    {
        static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
        public static int Swapped;
        public static void Apply(GameObject go, string model)
        {
            if (go == null || string.IsNullOrEmpty(model)) return;
            if (!_cache.TryGetValue(model, out var src)) { src = Resources.Load<GameObject>("CoastRun/Models/Sharp/" + model); _cache[model] = src; }
            if (src == null) return;
            var byName = new Dictionary<string, Mesh>();
            var srcFilters = src.GetComponentsInChildren<MeshFilter>(true);
            foreach (var f in srcFilters) if (f.sharedMesh != null && !byName.ContainsKey(f.sharedMesh.name)) byName[f.sharedMesh.name] = f.sharedMesh;
            var dst = go.GetComponentsInChildren<MeshFilter>(true);
            int k = 0;
            for (int i = 0; i < dst.Length; i++)
            {
                var f = dst[i]; if (f.sharedMesh == null) continue;
                Mesh m = null;
                if (!byName.TryGetValue(f.sharedMesh.name, out m) && i < srcFilters.Length && srcFilters[i].sharedMesh != null && srcFilters[i].sharedMesh.vertexCount > 0) m = srcFilters[i].sharedMesh;
                if (m == null || m == f.sharedMesh) continue;
                f.sharedMesh = m; k++;
            }
            Swapped += k;
        }
    }
}
