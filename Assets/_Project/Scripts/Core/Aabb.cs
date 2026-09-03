using UnityEngine;

/// Axis-aligned box helpers for physics-free overlap tests.
public static class Aabb
{
    public static bool Overlaps(Vector3 aCenter, Vector3 aSize, Vector3 bCenter, Vector3 bSize)
    {
        Vector3 aMin = aCenter - aSize * 0.5f;
        Vector3 aMax = aCenter + aSize * 0.5f;
        Vector3 bMin = bCenter - bSize * 0.5f;
        Vector3 bMax = bCenter + bSize * 0.5f;

        return aMin.x <= bMax.x && aMax.x >= bMin.x
            && aMin.y <= bMax.y && aMax.y >= bMin.y
            && aMin.z <= bMax.z && aMax.z >= bMin.z;
    }

    public static bool OverlapsXZ(Vector3 aCenter, Vector3 aSize, Vector3 bCenter, Vector3 bSize)
    {
        float aMinX = aCenter.x - aSize.x * 0.5f;
        float aMaxX = aCenter.x + aSize.x * 0.5f;
        float aMinZ = aCenter.z - aSize.z * 0.5f;
        float aMaxZ = aCenter.z + aSize.z * 0.5f;
        float bMinX = bCenter.x - bSize.x * 0.5f;
        float bMaxX = bCenter.x + bSize.x * 0.5f;
        float bMinZ = bCenter.z - bSize.z * 0.5f;
        float bMaxZ = bCenter.z + bSize.z * 0.5f;

        return aMinX <= bMaxX && aMaxX >= bMinX
            && aMinZ <= bMaxZ && aMaxZ >= bMinZ;
    }
}
