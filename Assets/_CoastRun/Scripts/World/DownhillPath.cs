using UnityEngine;

namespace CoastRun
{
    /// Elevated coastal promenade. The road itself is nearly flat;
    /// the downhill read comes from the drop to sea/city on the right.
    public static class DownhillPath
    {
        public const float SlopeDegrees = 0f;

        public static Quaternion Rotation => Quaternion.Euler(SlopeDegrees, 0f, 0f);

        public static Quaternion UprightLocal => Quaternion.Euler(-SlopeDegrees, 0f, 0f);

        public static Vector3 Tangent => Rotation * Vector3.forward;

        public static Vector3 Normal => Rotation * Vector3.up;

        public static Vector3 Point(float pathDistance)
        {
            return Rotation * new Vector3(0f, 0f, pathDistance);
        }

        public static Vector3 Point(float pathDistance, float lateral, float hop = 0f)
        {
            return Rotation * new Vector3(lateral, hop, pathDistance);
        }

        public static float DistanceAlong(Vector3 world)
        {
            return Vector3.Dot(world, Tangent);
        }
    }
}
