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

        [Header("Feel")]
        public float softHitSlowFactor = 0.55f;
        public float softHitRecoverSeconds = 1.2f;
        public float swipeThresholdPx = 48f;
    }
}
