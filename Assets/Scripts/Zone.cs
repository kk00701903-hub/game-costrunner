using UnityEngine;

/// The five stretches of the run, in the order Doha crosses them.
public enum Zone
{
    Arcade = 1,
    Overpass = 2,
    Flooded = 3,
    Tower = 4,
    Depot = 5
}

public static class Zones
{
    public const int Count = 5;

    /// Zones are measured in metres, not minutes, so a fast run and a slow run
    /// see the same city in the same order.
    public const float MetresPerZone = 1600f;

    public static Zone At(float distance)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(distance / MetresPerZone) + 1, 1, Count);
        return (Zone)index;
    }

    /// 0 at the zone boundary, 1 at the next one.
    public static float Progress(float distance)
    {
        float within = distance - (Index(At(distance)) - 1) * MetresPerZone;
        return Mathf.Clamp01(within / MetresPerZone);
    }

    public static int Index(Zone zone)
    {
        return Mathf.Clamp((int)zone, 1, Count);
    }

    public static float StartDistance(Zone zone)
    {
        return (Index(zone) - 1) * MetresPerZone;
    }

    public static string Name(Zone zone)
    {
        switch (zone)
        {
            case Zone.Arcade:
                return "상가 골목";
            case Zone.Overpass:
                return "고가도로";
            case Zone.Flooded:
                return "침수 지하상가";
            case Zone.Tower:
                return "주거 타워";
            default:
                return "집하장";
        }
    }

    public static string Label(Zone zone)
    {
        return "구역 " + Index(zone) + " · " + Name(zone);
    }
}
