using UnityEngine;

namespace CoastRun
{
    /// 14차-7: 아이템 수집 판정을 물리 트리거가 아니라 거리로 한다.
    /// 주인공 transform 은 몸 중간 높이(약 0.8 m)에 있다. 경로 진행 거리와 좌우 차이만 본다.
    public static class PickupReach
    {
        public const float Ahead = 1.1f;      // 이 거리 안으로 들어오면 먹는다(몸 앞에서 터지도록)
        public const float Behind = 0.35f;    // 이미 지나친 아이템은 조금만 봐준다
        public const float Lateral = 0.8f;    // 레인 폭의 절반 남짓

        public static bool InReach(Transform player, Vector3 itemPos)
        {
            if (player == null) return false;
            float dz = DownhillPath.DistanceAlong(itemPos) - DownhillPath.DistanceAlong(player.position);
            if (dz > Ahead || dz < -Behind) return false;
            Vector3 right = DownhillPath.Rotation * Vector3.right;
            float dx = Vector3.Dot(itemPos - player.position, right);
            if (Mathf.Abs(dx) > Lateral) return false;
            // 14차-10: 높이도 본다 — 하늘 코인은 점프해야 먹는다(주인공 transform 은 몸 중간 ≈ 0.8 m).
            float dy = itemPos.y - player.position.y;
            return dy > -1.1f && dy < 1.25f;
        }

        /// 자석에 끌리는 아이템은 몸속이 아니라 몸 앞(가슴 높이)으로 온다.
        public static Vector3 MagnetTarget(Transform player)
            => player.position + DownhillPath.Tangent * 0.7f + Vector3.up * 0.25f;

        /// 터짐 위치: 주인공 가슴 높이에서 카메라 쪽으로 1.0 m — 항상 몸 앞에 보인다.
        public static Vector3 PopPos(Transform player, Vector3 itemPos)
        {
            Vector3 basePos = player != null ? player.position + Vector3.up * 0.35f : itemPos;
            var cam = Camera.main;
            if (cam == null) return basePos;
            Vector3 toCam = cam.transform.position - basePos; toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.01f) return basePos;
            return basePos + toCam.normalized * 1.0f;
        }
    }
}
