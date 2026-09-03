using System;
using UnityEngine;

namespace CoastRun
{
    [Serializable]
    public class StageDef
    {
        public int chapterIndex;      // 1..5
        public int stageIndex;        // 1..20 (global)
        public string stageName;
        public float targetDistance;
        public float timeLimit;       // 0 = none
        public string newMechanic;
        public float lightingTStart;  // 0..1
        public float lightingTEnd;
        /// e.g. "R01" — null/empty on chapter-end stages & S20.
        public string rewardFragmentId;
    }

    [CreateAssetMenu(menuName = "Coast Run/Stage Table", fileName = "StageTable")]
    public class StageTable : ScriptableObject
    {
        public StageDef[] stages = Array.Empty<StageDef>();

        public StageDef GetByIndex(int stageIndex1Based)
        {
            if (stages == null)
                return null;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null && stages[i].stageIndex == stageIndex1Based)
                    return stages[i];
            }

            int idx = stageIndex1Based - 1;
            if (idx >= 0 && idx < stages.Length)
                return stages[idx];
            return null;
        }

        public int Count => stages != null ? stages.Length : 0;

        /// Design §5 master table — trackSeconds × 11 m/s ≈ ContentPace envelope.
        public static StageDef[] BuildDefaultStages()
        {
            return new[]
            {
                S(1, 1, "첫 발 구르기", 1650f, 0f, "Jump", 0.00f, 0.05f),
                S(1, 2, "방파제", 2200f, 0f, "Slide", 0.05f, 0.10f),
                S(1, 3, "비린내 나는 골목", 2475f, 225f, "Grind", 0.10f, 0.15f),
                S(1, 4, "갈매기 언덕", 2805f, 0f, "Collect", 0.15f, 0.20f),
                S(2, 5, "천막 아래", 2420f, 0f, "QuickDodge", 0.20f, 0.25f),
                S(2, 6, "수레와 상자", 2640f, 0f, "MovingObstacle", 0.25f, 0.30f),
                S(2, 7, "풍선 아치", 2695f, 0f, "FallingDebris", 0.30f, 0.35f),
                S(2, 8, "행렬 돌파", 2750f, 250f, "Boost", 0.35f, 0.40f),
                S(3, 9, "둑길", 2585f, 0f, "SpeedTiers", 0.40f, 0.45f),
                S(3, 10, "갈대밭", 2750f, 0f, "VisionBlock", 0.45f, 0.50f),
                S(3, 11, "폐공장", 2805f, 0f, "GapJump", 0.50f, 0.55f),
                S(3, 12, "컨베이어", 2805f, 255f, "MovingPlatform", 0.55f, 0.60f),
                S(4, 13, "갓길", 2805f, 0f, "VehicleTraffic", 0.60f, 0.65f),
                S(4, 14, "터널", 2970f, 0f, "LightsOut", 0.65f, 0.72f),
                S(4, 15, "내리막 국도", 2981f, 0f, "SpeedHold", 0.72f, 0.80f),
                S(4, 16, "사거리", 3003f, 0f, "SignalPattern", 0.80f, 0.86f),
                S(5, 17, "언덕 초입", 2849f, 0f, "UphillPush", 0.86f, 0.91f),
                S(5, 18, "억새밭", 3003f, 0f, "NightVision", 0.91f, 0.96f),
                S(5, 19, "마지막 오르막", 3014f, 274f, "Stamina", 0.96f, 1.00f),
                S(5, 20, "송전탑", 3300f, 0f, "", 1.00f, 1.00f),
            };
        }

        public void EnsurePopulated()
        {
            if (stages == null || stages.Length == 0)
                stages = BuildDefaultStages();
        }

        public static StageTable CreateDefault()
        {
            var table = CreateInstance<StageTable>();
            table.name = "StageTable";
            table.stages = BuildDefaultStages();
            return table;
        }

        private static StageDef S(int ch, int idx, string name, float dist, float limit,
            string mechanic, float t0, float t1)
        {
            return new StageDef
            {
                chapterIndex = ch,
                stageIndex = idx,
                stageName = name,
                targetDistance = dist,
                timeLimit = limit,
                newMechanic = mechanic,
                lightingTStart = t0,
                lightingTEnd = t1,
                rewardFragmentId = StoryDatabase.RewardFragmentIdForStage(idx)
            };
        }
    }
}
