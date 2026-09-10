using UnityEngine;

namespace CoastRun
{
    /// Gold Run–style rhythm: clear lead-in → single-lane challenges → coin guide → denser cluster.
    /// One bar ≈ 30 s at ~14 m/s.
    public static class RunRhythm
    {
        public enum Phase { Easy, CoinLine, Crisis }

        public const float BarLength = 420f;

        public static Phase At(float pathDistance)
        {
            float t = Mathf.Repeat(Mathf.Max(0f, pathDistance), BarLength) / BarLength;
            if (t < 0.3333f) return Phase.Easy;
            if (t < 0.6667f) return Phase.CoinLine;
            return Phase.Crisis;
        }

        public static float PhaseT(float pathDistance)
        {
            float t = Mathf.Repeat(Mathf.Max(0f, pathDistance), BarLength) / BarLength;
            return Mathf.Repeat(t * 3f, 1f);
        }

        public static float ObstacleGapMul(float pathDistance)
        {
            switch (At(pathDistance))
            {
                case Phase.Easy: return 1.65f;     // empty asphalt between singles
                case Phase.CoinLine: return 1.85f; // almost clear while following coins
                default: return Mathf.Lerp(0.85f, 0.62f, PhaseT(pathDistance));
            }
        }

        public static float CoinIntervalMul(float pathDistance)
        {
            switch (At(pathDistance))
            {
                case Phase.Easy: return 1.35f;
                case Phase.CoinLine: return 0.85f;
                default: return 1.4f;
            }
        }
    }
}
