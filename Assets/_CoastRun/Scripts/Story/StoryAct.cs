using System;
using UnityEngine;

namespace CoastRun
{
    /// Official story act order for 『우리의 송전탑』.
    public enum StoryAct
    {
        /// 씬1~4: 약속 → 장애 → 결심 → 게임플레이 전환
        Prologue = 0,
        /// 주행: NearMiss·업그레이드·추억 독백
        Run = 1,
        /// 45%+: 노을 (Golden Hour)
        GoldenHour = 2,
        /// 82%+: 블루아워, 송전탑 접근
        BlueHourApproach = 3,
        /// 송전탑 도착 · 만남
        Arrival = 4
    }

    [Serializable]
    public struct StoryActBeat
    {
        public StoryAct act;
        [Range(0f, 1f)] public float normalizedProgress;
        public string hudLabel;
        [TextArea(1, 3)] public string narratorLine;
    }
}
