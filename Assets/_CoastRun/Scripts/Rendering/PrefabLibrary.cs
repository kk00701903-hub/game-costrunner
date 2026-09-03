using UnityEngine;

namespace CoastRun
{
    /// Prefab slots for MCP FBX imports. Falls back to procedural builders until Prefabs exist.
    public static class PrefabLibrary
    {
        public static GameObject TryInstantiate(string resourcesName, Transform parent, Vector3 localPos)
        {
            var prefab = ArtAssets.LoadPrefabOrNull(resourcesName);
            if (prefab == null)
                return null;

            var go = Object.Instantiate(prefab, parent);
            go.transform.localPosition = localPos;
            go.name = resourcesName;
            return go;
        }

        public static bool HasPrefab(string resourcesName)
        {
            return ArtAssets.LoadPrefabOrNull(resourcesName) != null;
        }
    }
}
