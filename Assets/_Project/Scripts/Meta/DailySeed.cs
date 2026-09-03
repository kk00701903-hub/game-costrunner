using UnityEngine;

/// Shared daily course seed. Ranking comes later — seed itself is local for now.
public static class DailySeed
{
    public static int Today()
    {
        string key = EconomyClock.TodayKey();
        unchecked
        {
            int h = 347;
            for (int i = 0; i < key.Length; i++)
                h = h * 33 + key[i];
            return h == 0 ? 347 : Mathf.Abs(h);
        }
    }
}
