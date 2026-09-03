using UnityEngine;

namespace CoastRun
{
    /// Puts gameplay props on the promenade surface (never on a floating "ceiling").
    public static class RoadPlacement
    {
        public static Vector3 OnRoad(float pathZ, float lateral, float height = 0f)
        {
            return DownhillPath.Point(pathZ, lateral, height);
        }

        public static void Snap(GameObject go, float pathZ, float lateral)
        {
            if (go == null)
                return;

            SanitizeScale(go.transform);
            go.transform.SetPositionAndRotation(OnRoad(pathZ, lateral), DownhillPath.Rotation);

            float bottom = float.MaxValue;
            bool found = false;
            var rends = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null || !r.enabled)
                    continue;
                Vector3 size = r.bounds.size;
                // Skip broken/oversized import meshes that poison grounding.
                if (size.x > 12f || size.y > 12f || size.z > 12f)
                    continue;
                bottom = Mathf.Min(bottom, r.bounds.min.y);
                found = true;
            }

            if (!found)
                return;

            float pathY = OnRoad(pathZ, lateral).y;
            float dy = Mathf.Clamp(pathY - bottom, -1f, 1.25f);
            go.transform.position += Vector3.up * dy;
        }

        public static bool IsPrefabUsable(GameObject go)
        {
            if (go == null)
                return false;

            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
                return false;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);

            // Reject FBX imports that are room-sized or tiny stubs.
            float maxAxis = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            return maxAxis > 0.15f && maxAxis < 8f;
        }

        /// Scales a prefab so its world height matches targetHeight (keeps proportions).
        public static void FitHeight(GameObject go, float targetHeight)
        {
            if (go == null || targetHeight <= 0.01f)
                return;

            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
                return;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null && rends[i].enabled)
                    b.Encapsulate(rends[i].bounds);
            }

            float h = b.size.y;
            if (h < 0.05f)
                return;

            float s = targetHeight / h;
            s = Mathf.Clamp(s, 0.08f, 2.5f);
            go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * s);
        }

        private static void SanitizeScale(Transform t)
        {
            Vector3 s = t.localScale;
            // Only clamp oversized imports — small scales are intentional (FitHeight).
            if (s.x > 2.5f || s.y > 2.5f || s.z > 2.5f)
                t.localScale = Vector3.one;
        }
    }
}
