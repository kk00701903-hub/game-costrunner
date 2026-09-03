using UnityEngine;

namespace CoastRun
{
    internal static class CoastEditUtil
    {
        internal static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }

        internal static void DestroyObject(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
