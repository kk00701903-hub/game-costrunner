using System.Collections.Generic;
using UnityEngine;

/// Physics-free hazard book. Volumes register themselves; the player tests
/// overlaps against this list instead of relying on CharacterController hits.
public sealed class HazardRegistry
{
    public static readonly HazardRegistry Instance = new HazardRegistry();

    private readonly List<HazardVolume> _volumes = new List<HazardVolume>(64);

    public int Count => _volumes.Count;

    public void Register(HazardVolume volume)
    {
        if (volume != null && !_volumes.Contains(volume))
            _volumes.Add(volume);
    }

    public void Unregister(HazardVolume volume)
    {
        _volumes.Remove(volume);
    }

    public void Clear()
    {
        _volumes.Clear();
    }

    public HazardVolume FirstOverlap(Vector3 center, Vector3 size)
    {
        for (int i = _volumes.Count - 1; i >= 0; i--)
        {
            HazardVolume volume = _volumes[i];
            if (volume == null || !volume.isActiveAndEnabled)
            {
                _volumes.RemoveAt(i);
                continue;
            }

            if (Aabb.Overlaps(center, size, volume.Center, volume.Size))
                return volume;
        }

        return null;
    }
}
