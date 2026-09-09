using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 38차: 도로 점유표 — 장애물·코인/말랑이·아이템(물약/별/하트) 스포너가 서로 모르고 겹쳐 놓던 것을 한 표로 맞춘다.
    /// 규칙(톰히어로 기준): 장애물 앞뒤 3 m 같은 레인엔 코인·말랑이 없음(오리 장애물처럼 전 레인짜리는 전 레인),
    /// 아이템은 장애물에서 4.5 m·다른 픽업에서 3.5 m 떨어진 자리로 미룬다.
    public static class RoadOccupancy
    {
        public enum Kind : byte { Obstacle = 0, Pickup = 1, Item = 2 }
        private struct Mark { public float z; public int lane; public Kind kind; }
        private static readonly List<Mark> _m = new List<Mark>(256);
        public const int AllLanes = 9;

        public static void Clear() => _m.Clear();

        public static void Add(Kind kind, float z, int lane)
        {
            _m.Add(new Mark { z = z, lane = lane, kind = kind });
            if (_m.Count > 220) Prune(z - 60f);
        }

        public static void Prune(float behindZ)
        {
            for (int i = _m.Count - 1; i >= 0; i--) if (_m[i].z < behindZ) _m.RemoveAt(i);
        }

        private static bool LaneHit(int a, int b) => a == AllLanes || b == AllLanes || a == b;

        public static bool Near(Kind kind, float z, int lane, float dz)
        {
            for (int i = 0; i < _m.Count; i++)
            {
                var m = _m[i];
                if (m.kind != kind) continue;
                if (Mathf.Abs(m.z - z) <= dz && LaneHit(m.lane, lane)) return true;
            }
            return false;
        }

        /// 아이템 자리 찾기: z 부터 2 m 씩 앞으로 6번까지 밀어 본다. 못 찾으면 원래 z.
        public static float FindClear(float z, int lane, float obstacleDz, float pickupDz)
        {
            for (int k = 0; k < 6; k++)
            {
                float zz = z + k * 2f;
                if (!Near(Kind.Obstacle, zz, lane, obstacleDz) && !Near(Kind.Pickup, zz, AllLanes, pickupDz) && !Near(Kind.Item, zz, AllLanes, pickupDz))
                    return zz;
            }
            return z;
        }

        /// 장애물이 나중에 놓이면 이미 깔린 코인·말랑이를 걷어 낸다(스포너 순서 무관).
        public static void OnObstacle(float z, int lane, float dz = 3f)
        {
            Add(Kind.Obstacle, z, lane);
            CoinSpawner.Instance?.RemoveNear(z, lane, dz);
            JellySpawner.Instance?.RemoveNear(z, lane, dz);
        }
    }
}
