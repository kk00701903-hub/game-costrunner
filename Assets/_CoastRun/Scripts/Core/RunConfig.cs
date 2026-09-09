using UnityEngine;

namespace CoastRun
{
    /// Tunables for a single downhill run. Create via Assets > Create > Coast Run > Run Config.
    [CreateAssetMenu(menuName = "Coast Run/Run Config", fileName = "RunConfig")]
    public class RunConfig : ScriptableObject
    {
        [Header("Speed")]
        public float baseSpeed = 11f;
        public float maxSpeed = 26f;
        public float accelPerSecond = 0.7f;
        [Tooltip("Hold-to-tuck speed multiplier.")]
        public float tuckMultiplier = 1.15f;

        [Header("Lanes")]
        public float laneOffset = 2.2f;
        public float laneChangeSeconds = 0.15f;

        [Header("Jump / Crouch")]
        public float jumpForce = 8.6f;
        public float gravity = -27f;
        public float crouchDuration = 0.9f;
        public float standHeight = 1.6f;
        public float crouchHeight = 0.55f;

        [Header("Juice (27차 — 톰 히어로식 쫀득 모션)")]
        [Tooltip("레인 이동 오버슈트. 0 = 정확히 도착, 0.6 ≈ 목표를 3~4% 지나쳤다 돌아옴")]
        public float laneOvershoot = 0.6f;
        [Tooltip("도약 순간 세로 늘어남 배율")]
        public float jumpStretch = 1.16f;
        [Tooltip("착지 순간 납작해지는 배율")]
        public float landSquash = 0.78f;
        [Tooltip("슬라이드 진입 납작 배율")]
        public float crouchSquash = 0.86f;
        [Tooltip("걸음 착지 스프링이 스케일에 주는 세기(0 = 위치만)")]
        public float stepSquash = 1.2f;
        [Tooltip("스프링 강성(클수록 빠르게 복귀) / 감쇠(작을수록 더 통통 튐)")]
        public float squashStiffness = 260f;
        public float squashDamping = 14f;
        [Tooltip("가방·머리가 한 박자 늦게 따라오는 세기")]
        public float secondaryAmount = 1f;

        [Header("Feel")]
        public float softHitSlowFactor = 0.55f;
        public float softHitRecoverSeconds = 1.2f;
        public float swipeThresholdPx = 48f;
    }
}
