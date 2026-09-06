using UnityEngine;

namespace CoastRun
{
    /// 챕터(스테이지 1~20)가 오를수록 살짝씩 어려워진다 — 속도·행 간격·2레인 행·마주 오는 차 속도,
    /// 그리고 장애물 종류가 챕터 단계별로 열린다. 모든 값은 스테이지 인덱스 하나에서 나온다.
    ///
    ///   스테이지   1     5     9     13    17    20
    ///   속도       1.00  1.07  1.14  1.20  1.27  1.32
    ///   행 간격    1.00  0.94  0.89  0.83  0.78  0.74
    ///   2레인 +    0     .03   .06   .09   .12   .14
    ///   해금       기본  석상  스쿠터  구르는 귤  등불줄
    public static class ChapterDifficulty
    {
        public static int Stage
        {
            get
            {
                var sm = StageManager.Instance;
                if (sm != null && sm.Current != null) return Mathf.Clamp(sm.Current.stageIndex, 1, 20);
                return 1;
            }
        }

        /// 0(1챕터) → 1(20챕터).
        public static float T => (Stage - 1) / 19f;

        public static float SpeedMul => 1f + 0.32f * T;
        public static float GapMul => Mathf.Lerp(1f, 0.74f, T);
        public static float DoubleLaneBonus => 0.14f * T;
        public static float CarSpeedMul => 1f + 0.25f * T;
        /// 마주 오는 차 간격도 좁아진다(행 수 기준 배율).
        public static float CarEveryMul => Mathf.Lerp(1f, 0.7f, T);

        public const int StatueFrom = 5;     // 돌하르방 석상 — 옆으로 피해야 함(점프 불가)
        public const int ScooterFrom = 7;    // 세워 둔 스쿠터 — 낮음, 점프 가능
        public const int OrangeFrom = 11;    // 구르는 귤 상자 — 마주 오며 굴러온다(점프 가능)
        public const int LanternFrom = 15;   // 축제 등불 줄 — 숙이기
        public const int DoubleCarFrom = 17; // 차 두 대 연속

        public static bool Allows(ObstacleId id, int stage)
        {
            switch (id)
            {
                case ObstacleId.StoneStatue: return stage >= StatueFrom;
                case ObstacleId.ScooterParked: return stage >= ScooterFrom;
                case ObstacleId.LanternString: return stage >= LanternFrom;
                default: return true;
            }
        }

        /// 구르는 귤(마주 오는 이동 장애물) 등장 확률 — 11챕터부터, 20챕터에 40%.
        public static float RollingOrangeChance(int stage) =>
            stage < OrangeFrom ? 0f : Mathf.Lerp(0.2f, 0.4f, Mathf.InverseLerp(OrangeFrom, 20, stage));
    }
}
