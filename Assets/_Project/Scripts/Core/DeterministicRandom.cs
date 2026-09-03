using System;

/// Seeded RNG for gameplay. UnityEngine.Random is banned from gameplay paths —
/// keep it for juice only (shake direction, particle jitter).
public sealed class DeterministicRandom
{
    private readonly Random _rng;

    public int Seed { get; }

    public DeterministicRandom(int seed)
    {
        Seed = seed;
        _rng = new Random(seed);
    }

    public float Value()
    {
        return (float)_rng.NextDouble();
    }

    public float Range(float minInclusive, float maxInclusive)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;
        return minInclusive + (float)_rng.NextDouble() * (maxInclusive - minInclusive);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;
        return _rng.Next(minInclusive, maxExclusive);
    }

    public bool Chance(float probability)
    {
        return Value() < probability;
    }

    public void Shuffle<T>(T[] items)
    {
        if (items == null)
            return;

        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = Range(0, i + 1);
            T tmp = items[i];
            items[i] = items[j];
            items[j] = tmp;
        }
    }

    public void Shuffle<T>(System.Collections.Generic.IList<T> items)
    {
        if (items == null)
            return;

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Range(0, i + 1);
            T tmp = items[i];
            items[i] = items[j];
            items[j] = tmp;
        }
    }
}
