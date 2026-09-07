using UnityEngine;

namespace CoastRun
{
    /// 14차: 리듬 기반 레벨 — 30초 단위로 '쉬움 → 코인 라인 → 위기'가 돈다.
    /// 랜덤 배치는 다음이 기대되지 않는다. 서브웨이 서퍼처럼 음악의 마디를 만든다:
    /// 10초 숨 고르기(장애물 드묾) → 10초 코인 줄 따라가기(장애물 거의 없음, 긴 코인 라인) → 10초 위기(빽빽한 줄).
    public static class RunRhythm
    {
        public enum Phase { Easy, CoinLine, Crisis }

        /// 한 마디 길이(m). 평균 14 m/s × 30 s.
        public const float BarLength = 420f;

        public static Phase At(float pathDistance)
        {
            float t = Mathf.Repeat(Mathf.Max(0f, pathDistance), BarLength) / BarLength;
            if (t < 0.3333f) return Phase.Easy;
            if (t < 0.6667f) return Phase.CoinLine;
            return Phase.Crisis;
        }

        /// 마디 안 진행도 0..1 (위기 구간 끝으로 갈수록 빽빽해진다).
        public static float PhaseT(float pathDistance)
        {
            float t = Mathf.Repeat(Mathf.Max(0f, pathDistance), BarLength) / BarLength;
            return Mathf.Repeat(t * 3f, 1f);
        }

        /// 장애물 줄 간격 배율.
        public static float ObstacleGapMul(float pathDistance)
        {
            switch (At(pathDistance))
            {
                case Phase.Easy: return 1.1f;    // 14차-13: 밀도 ↑
                case Phase.CoinLine: return 1.45f;
                default: return Mathf.Lerp(0.72f, 0.55f, PhaseT(pathDistance));
            }
        }

        /// 코인 패턴 간격 배율(작을수록 자주).
        public static float CoinIntervalMul(float pathDistance)
        {
            switch (At(pathDistance))
            {
                case Phase.Easy: return 1.0f;
                case Phase.CoinLine: return 0.55f;
                default: return 1.3f;
            }
        }
    }
}
