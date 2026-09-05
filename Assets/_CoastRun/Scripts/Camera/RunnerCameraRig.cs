using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// Chase cam with speed FOV, opposite-lane roll, bob, Perlin shake, FOV kick offset.
    public class RunnerCameraRig : MonoBehaviour
    {
        [SerializeField] private PlayerController target;

        [Header("Framing")]
        [SerializeField] private Vector3 offset = new Vector3(0.12f, 2.95f, -10.8f);
        [SerializeField] private float lookAhead = 20f;
        [SerializeField] private float lookHeight = 3.4f;
        [SerializeField] private float pitchUp = -1.5f;
        [SerializeField] private float lateralDamping = 0.14f;

        [Header("FOV kick")]
        [SerializeField] private float baseFov = 55f;
        [SerializeField] private float maxFov = 74f;
        [SerializeField] private float fovAccelSeconds = 0.4f;
        [SerializeField] private float fovDecelSeconds = 0.25f;

        [Header("Lane roll")]
        [SerializeField] private float rollMaxDegrees = 4f;
        [SerializeField] private float rollReturnSeconds = 0.25f;

        [Header("Bob")]
        [SerializeField] private float bobAmplitude = 0.02f;
        [SerializeField] private float bobCyclesPerUnitSpeed = 0.55f;

        [Header("Feel")]
        [SerializeField] private float anticipationSeconds = 0.1f;

        [Header("Curve lean")]
        [Tooltip("How far the aim point follows the visual bend at the look-ahead distance (0..1).")]
        [SerializeField] private float curveAimFollow = 0.7f;
        [Tooltip("Degrees of roll into the curve at peak curvature.")]
        [SerializeField] private float curveRollDegrees = 2.5f;

        private Camera _camera;
        private float _lateral;
        private float _lateralVelocity;
        private float _prevLateral;
        private float _roll;
        private float _rollVelocity;
        private float _bobPhase;

        // Speed FOV blend 0..1 (smoothed with accel/decel easing)
        private float _speedFovT;
        private float _anticipatedSpeedT;
        private float _speedAnticipationTimer;

        // Additive FOV kick (SoftHit / NearMiss juice)
        private float _fovKick;
        private float _fovKickVel;
        private float _fovKickTarget;
        private float _fovKickReturnDuration;

        // Perlin shake
        private float _shakeTimeLeft;
        private float _shakeDuration;
        private float _shakeMagnitude;
        private float _shakeSeed;

        // Land dip (visual only)
        private float _landDip;

        private bool _handoffActive;
        private bool _followSuspended;
        private SpeedLineFx _speedLines;

        public bool HandoffActive => _handoffActive;
        public bool FollowSuspended => _followSuspended;
        public PlayerController Target => target;

        public void SetTarget(PlayerController player)
        {
            target = player;
            if (player != null)
            {
                _lateral = player.LateralOffset;
                _prevLateral = _lateral;
            }
        }

        /// Freeze chase follow (prologue wait / same-frame snap).
        public void SetFollowSuspended(bool suspended)
        {
            _followSuspended = suspended;
            if (!suspended)
                _handoffActive = false;
        }

        /// Copy cinematic camera pose exactly — no lerp. Used by prologue P4 handoff.
        public void SnapPose(Vector3 worldPos, Quaternion worldRot, float fieldOfView)
        {
            transform.SetPositionAndRotation(worldPos, worldRot);
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _camera.fieldOfView = fieldOfView;
                baseFov = fieldOfView;
            }

            _speedFovT = 0f;
            _anticipatedSpeedT = 0f;
            _fovKick = 0f;
            _fovKickTarget = 0f;
            _bobOffset = 0f;
            _landDip = 0f;
            _shakeTimeLeft = 0f;
            _followSuspended = false;
            _handoffActive = false;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null)
                _camera.fieldOfView = baseFov;

            _speedLines = GetComponent<SpeedLineFx>() ?? gameObject.AddComponent<SpeedLineFx>();
            _speedLines.EnsureBuilt();
        }

        private void Start()
        {
            if (target == null)
                target = Object.FindAnyObjectByType<PlayerController>();
            if (target != null)
            {
                _lateral = target.LateralOffset;
                _prevLateral = _lateral;
            }
        }

        /// SoftHit / juice entry — duration first, then magnitude (prompt API).
        public void Shake(float duration, float magnitude)
        {
            duration = Mathf.Max(0.02f, duration);
            magnitude = Mathf.Max(0f, magnitude);
            if (magnitude >= _shakeMagnitude * (_shakeTimeLeft / Mathf.Max(0.01f, _shakeDuration)))
            {
                _shakeDuration = duration;
                _shakeTimeLeft = duration;
                _shakeMagnitude = magnitude;
                _shakeSeed = Random.value * 100f;
            }
        }

        /// Temporary FOV punch — additive offset on top of speed FOV.
        public void FovKick(float delta, float duration)
        {
            _fovKickTarget = delta;
            _fovKickReturnDuration = Mathf.Max(0.05f, duration);
            // Anticipation: tiny opposite dip first
            _fovKick = -delta * 0.12f;
            _fovKickVel = 0f;
        }

        /// Brief downward dip on landing (seconds).
        public void LandDip(float duration)
        {
            duration = Mathf.Max(0.04f, duration);
            _landDip = -0.12f;
            StartCoroutine(LandDipReturn(duration));
        }

        private System.Collections.IEnumerator LandDipReturn(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                // Ease back to 0 from dip
                _landDip = Mathf.Lerp(-0.12f, 0f, u * u * (3f - 2f * u));
                yield return null;
            }

            _landDip = 0f;
        }

        public IEnumerator PlayGameplayHandoff(float duration)
        {
            if (target == null)
                yield break;

            _handoffActive = true;
            duration = Mathf.Max(0.5f, duration);

            Vector3 playerPos = target.transform.position;
            Quaternion playerFrame = target.PathRotation;
            Vector3 sidePos = playerPos + playerFrame * new Vector3(4.5f, 2.2f, -2f);
            Quaternion sideRot = Quaternion.LookRotation(
                (playerPos + Vector3.up * 1.2f - sidePos).normalized, Vector3.up);

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(sidePos, ComputeChasePosition(), u);
                transform.rotation = Quaternion.Slerp(sideRot, ComputeChaseRotation(0f), u);
                yield return null;
            }

            _handoffActive = false;
        }

        private void LateUpdate()
        {
            if (target == null || _handoffActive || _followSuspended)
                return;

            float dt = Time.unscaledDeltaTime;
            float speedT = Mathf.Clamp01(target.NormalizedSpeed);
            float speed = target.Speed;

            UpdateSpeedFov(speedT, dt);
            UpdateFovKick(dt);
            UpdateLateralAndRoll(dt);
            UpdateBob(speed, speedT, dt);

            Quaternion frame = SlopeFrame();
            Vector3 pivot = ComputePivot(frame);
            Vector3 camPos = pivot + frame * offset;
            camPos += frame * Vector3.up * (_bobOffset + _landDip);
            camPos += EvaluateShakeOffset();

            transform.position = camPos;

            // Lean into the bend: the world sweeps right → tilt right, like a skater
            // carving the turn. Curvature is tiny (m/m²) so scale it up to degrees.
            float k = CurveDirector.Instance != null ? CurveDirector.Instance.Curvature : 0f;
            float roll = _roll + Mathf.Clamp(k * 1000f, -1f, 1f) * curveRollDegrees;
            transform.rotation = ComputeChaseRotation(roll);

            if (_camera != null)
            {
                float speedFov = Mathf.Lerp(baseFov, maxFov, _speedFovT);
                if (target.IsTucking)
                    speedFov += 2f;
                _camera.fieldOfView = speedFov + _fovKick;
            }

            _speedLines?.SetSpeedRatio(speedT);
            RoadUvScroller.SetScrollSpeed(speed);
        }

        private float _bobOffset;

        private void UpdateBob(float speed, float speedT, float dt)
        {
            if (speed < 0.15f || !target.IsGrounded)
            {
                _bobOffset = Mathf.MoveTowards(_bobOffset, 0f, dt * 0.4f);
                return;
            }

            _bobPhase += dt * speed * bobCyclesPerUnitSpeed * Mathf.PI * 2f;
            _bobOffset = Mathf.Sin(_bobPhase) * bobAmplitude * Mathf.Clamp01(speedT + 0.15f);
        }

        private void UpdateSpeedFov(float speedT, float dt)
        {
            // Anticipation: delay following speed by a short window.
            if (Mathf.Abs(speedT - _anticipatedSpeedT) > 0.001f)
            {
                _speedAnticipationTimer += dt;
                if (_speedAnticipationTimer >= anticipationSeconds)
                    _anticipatedSpeedT = speedT;
            }
            else
                _speedAnticipationTimer = 0f;

            float targetT = _anticipatedSpeedT;
            bool accelerating = targetT > _speedFovT + 0.001f;
            float duration = accelerating ? fovAccelSeconds : fovDecelSeconds;

            // Move toward target with ease (EaseOut accel / EaseIn decel via SmoothDamp-ish curve).
            float u = 1f - Mathf.Exp(-dt / Mathf.Max(0.05f, duration * 0.55f));
            if (accelerating)
                u = EaseOut(u);
            else
                u = EaseIn(u);

            _speedFovT = Mathf.Lerp(_speedFovT, targetT, u);
        }

        private void UpdateFovKick(float dt)
        {
            // Punch toward target then return to 0.
            if (Mathf.Abs(_fovKickTarget) > 0.01f)
            {
                _fovKick = Mathf.SmoothDamp(_fovKick, _fovKickTarget, ref _fovKickVel, 0.06f);
                if (Mathf.Abs(_fovKick - _fovKickTarget) < 0.15f)
                    _fovKickTarget = 0f;
            }
            else
            {
                _fovKick = Mathf.SmoothDamp(_fovKick, 0f, ref _fovKickVel,
                    Mathf.Max(0.05f, _fovKickReturnDuration * 0.5f));
            }
        }

        private void UpdateLateralAndRoll(float dt)
        {
            float wantLateral = target.LateralOffset;
            // Anticipation: ease toward a slightly exaggerated lateral when changing lanes.
            float lateralDelta = wantLateral - _prevLateral;
            float anticipateLateral = wantLateral + Mathf.Sign(lateralDelta) * 0.15f *
                                      (Mathf.Abs(lateralDelta) > 0.001f ? 1f : 0f);

            float damp = lateralDamping + anticipationSeconds * 0.4f;
            _lateral = Mathf.SmoothDamp(_lateral, anticipateLateral, ref _lateralVelocity, damp);

            // Opposite roll = centrifugal feel (move left → roll right).
            float lateralSpeed = (_lateral - _prevLateral) / Mathf.Max(0.0001f, dt);
            float rollTarget = Mathf.Clamp(-lateralSpeed * 0.35f, -rollMaxDegrees, rollMaxDegrees);
            // Also lean opposite to lane position slightly while sliding.
            rollTarget += Mathf.Clamp(-(_lateral - wantLateral) * 8f, -rollMaxDegrees, rollMaxDegrees);
            rollTarget = Mathf.Clamp(rollTarget, -rollMaxDegrees, rollMaxDegrees);

            _roll = Mathf.SmoothDamp(_roll, rollTarget, ref _rollVelocity, rollReturnSeconds);
            _prevLateral = _lateral;
        }

        private Vector3 EvaluateShakeOffset()
        {
            if (_shakeTimeLeft <= 0f || _shakeMagnitude <= 0f)
                return Vector3.zero;

            _shakeTimeLeft -= Time.unscaledDeltaTime;
            float life = Mathf.Clamp01(_shakeTimeLeft / Mathf.Max(0.01f, _shakeDuration));
            float envelope = life * life; // ease-out decay
            float t = Time.unscaledTime;
            float nx = (Mathf.PerlinNoise(_shakeSeed, t * 26f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(_shakeSeed + 19.1f, t * 23f) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(_shakeSeed + 7.3f, t * 21f) - 0.5f) * 2f;
            return new Vector3(nx, ny, nz) * (_shakeMagnitude * envelope);
        }

        private Quaternion SlopeFrame() => target != null ? target.PathRotation : DownhillPath.Rotation;

        private Vector3 ComputePivot(Quaternion frame)
        {
            Vector3 playerPos = target.transform.position;
            Vector3 centre = playerPos - frame * new Vector3(target.LateralOffset, 0f, 0f);
            return centre + frame * new Vector3(_lateral, 0f, 0f);
        }

        private Vector3 ComputeChasePosition()
        {
            return ComputePivot(SlopeFrame()) + SlopeFrame() * offset;
        }

        private Quaternion ComputeChaseRotation(float rollZ)
        {
            Quaternion frame = SlopeFrame();
            Vector3 pivot = ComputePivot(frame);
            // Aim where the bent road actually is at lookAhead, so the camera pans through
            // the curve instead of staring at where the straight road would have been.
            float k = CurveDirector.Instance != null ? CurveDirector.Instance.Curvature : 0f;
            float kv = CurveDirector.Instance != null ? CurveDirector.Instance.VerticalCurvature : 0f;
            float d2 = lookAhead * lookAhead * curveAimFollow;
            Vector3 aim = pivot + frame * new Vector3(k * d2, lookHeight + kv * d2, lookAhead);
            Vector3 toAim = aim - transform.position;
            if (toAim.sqrMagnitude < 0.001f)
                return transform.rotation;
            Quaternion look = Quaternion.LookRotation(toAim.normalized, Vector3.up);
            return look * Quaternion.Euler(pitchUp, 0f, rollZ);
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }
    }
}
