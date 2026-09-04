using System;
using UnityEngine;

namespace CoastRun
{
    public enum SkateState
    {
        Run,
        Air,
        Crouch,
        SoftHit,
        Finish
    }

    /// Physics-free downhill skater: path distance + lane offset + jump/crouch/tuck.
    /// Wire MobileSwipeInput + MapGenerator (IMapStream) in the inspector or at boot.
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private RunConfig config;
        [SerializeField] private MonoBehaviour inputBehaviour;
        [SerializeField] private MonoBehaviour mapBehaviour;
        [SerializeField] private UpgradeManager upgrades;

        private IInputReader _input;
        private IMapStream _map;

        private int _lane; // -1, 0, 1
        private float _lateral;
        private float _pathDistance;
        private float _speed;
        private float _verticalVelocity;
        private float _groundY;
        private float _hop;
        private float _bodyHeight = 1.6f;
        private float _crouchTimer;
        private float _softHitTimer;
        private float _inputFreezeTimer;
        private bool _tucking;
        private SkateState _state = SkateState.Run;

        public event Action OnSoftHit;
        public event Action OnLanded;
        public event Action OnJumped;
        public event Action<int> OnLaneChanged;

        private CapsuleCollider _bodyCollider;

        public float PathDistance => _pathDistance;
        public float Speed => _speed;
        public float NormalizedSpeed
        {
            get
            {
                float max = upgrades != null ? upgrades.GetMaxSpeed() : (config != null ? config.maxSpeed : 1f);
                float min = config != null ? config.baseSpeed : 0f;
                return Mathf.InverseLerp(min, max, _speed);
            }
        }
        public float Yaw { get; private set; }
        public Quaternion PathRotation => DownhillPath.Rotation;
        public float LateralOffset => _lateral;
        public int Lane => _lane;
        public SkateState State => _state;
        public bool IsGrounded => _state != SkateState.Air && _state != SkateState.Finish;
        public bool IsCrouching => _state == SkateState.Crouch;
        public bool IsTucking => _tucking;

        /// Height above grounded hop pose — used by BlobShadow (visual only).
        public float GroundClearance
        {
            get
            {
                float groundedHop = _groundY + _bodyHeight * 0.5f;
                return Mathf.Max(0f, _hop - groundedHop);
            }
        }

        public void SetPathDistance(float distance)
        {
            _pathDistance = Mathf.Max(0f, distance);
            SnapToPath();
        }

        /// Clear SoftHit / air state for stage retry without destroying the player.
        public void ResetSoftState()
        {
            _softHitTimer = 0f;
            _inputFreezeTimer = 0f;
            _verticalVelocity = 0f;
            if (config != null)
            {
                _bodyHeight = config.standHeight;
                _speed = config.baseSpeed;
                _hop = _groundY + _bodyHeight * 0.5f;
            }

            if (_state == SkateState.SoftHit || _state == SkateState.Air || _state == SkateState.Finish)
                _state = SkateState.Run;
            SnapToPath();
        }

        public void Bind(IInputReader input, IMapStream map, RunConfig runConfig, UpgradeManager upgradeManager = null)
        {
            _input = input;
            _map = map;
            if (runConfig != null)
                config = runConfig;
            if (upgradeManager != null)
                upgrades = upgradeManager;
        }

        private void Awake()
        {
            EnsurePlayerPhysics();
            ResolveDeps();
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RunConfig>();
                config.name = "RunConfig (runtime)";
            }

            _speed = config.baseSpeed;
            _bodyHeight = config.standHeight;
            _groundY = 0f;
            _hop = _bodyHeight * 0.5f;
            SnapToPath();
        }

        /// Kinematic body so NearMiss / Hazard triggers fire without physics movement.
        private void EnsurePlayerPhysics()
        {
            try
            {
                if (!CompareTag("Player"))
                    gameObject.tag = "Player";
            }
            catch (UnityException)
            {
                // Tag missing in TagManager — NearMissZone also checks PlayerController.
            }

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var col = GetComponent<CapsuleCollider>();
            if (col == null)
                col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = false;
            col.radius = 0.32f;
            col.height = 1.5f;

            // The transform already sits at the body's mid-height: _hop starts at
            // _groundY + _bodyHeight * 0.5 = 0.8. Offsetting the capsule by another 0.85
            // lifted its base to world y 0.90, while every ground obstacle's HardHit tops
            // out at 0.645 (localPosition 0.32 + height 0.65 / 2). The two never touched,
            // so ten of the twelve obstacle types were scenery and only the overhead duck
            // hazards could actually hit. Keep this at zero — the offset belongs to _hop.
            col.center = Vector3.zero;
            _bodyCollider = col;
        }

        private void ResolveDeps()
        {
            if (_input == null && inputBehaviour is IInputReader reader)
                _input = reader;
            if (_map == null && mapBehaviour is IMapStream stream)
                _map = stream;

            if (_input == null)
                _input = GetComponent<IInputReader>() ?? FindFirstObjectByType<MobileSwipeInput>();
            if (_map == null)
                _map = FindFirstObjectByType<MapGenerator>();
        }

        private void Update()
        {
            if (_state == SkateState.Finish || config == null)
                return;

            ResolveDeps();
            _input?.Tick();

            if (_inputFreezeTimer > 0f)
                _inputFreezeTimer -= Time.unscaledDeltaTime;

            UpdateSoftHit();
            UpdateSpeed();
            HandleInput();
            UpdateCrouch();
            SyncBodyCollider();
            Move();
            _map?.SetPlayerDistance(_pathDistance);
        }

        /// Temporary control lock (SoftHit juice) — uses unscaled time so hit-stop does not extend it oddly.
        public void FreezeInput(float seconds)
        {
            _inputFreezeTimer = Mathf.Max(_inputFreezeTimer, Mathf.Max(0f, seconds));
        }

        private void UpdateSoftHit()
        {
            if (_softHitTimer <= 0f)
                return;

            _softHitTimer -= Time.deltaTime;
            if (_softHitTimer <= 0f && _state == SkateState.SoftHit)
                _state = SkateState.Run;
        }

        private void UpdateSpeed()
        {
            float maxSpeed = upgrades != null ? upgrades.GetMaxSpeed() : config.maxSpeed;
            float target = Mathf.Min(maxSpeed, _speed + config.accelPerSecond * Time.deltaTime);
            if (_state == SkateState.SoftHit)
                target = config.baseSpeed * config.softHitSlowFactor;

            _tucking = _input != null && _input.TuckHeld && IsGrounded && _state != SkateState.Crouch;
            if (_tucking)
                target *= config.tuckMultiplier;

            _speed = Mathf.MoveTowards(_speed, target, 20f * Time.deltaTime);
        }

        private void HandleInput()
        {
            if (_input == null || _state == SkateState.SoftHit || _inputFreezeTimer > 0f)
                return;

            int laneDelta = _input.ConsumeLaneDelta();
            if (laneDelta != 0)
                ChangeLane(laneDelta);

            if (_input.ConsumeJump())
                TryJump();
            if (_input.ConsumeCrouch())
                TryCrouch();
            if (_input.CrouchHeld)
                HoldCrouch();
        }

        private void HoldCrouch()
        {
            if (!IsGrounded || _state == SkateState.Air)
                return;

            _state = SkateState.Crouch;
            _crouchTimer = Mathf.Max(_crouchTimer, 0.12f);
            _bodyHeight = config.crouchHeight;
        }

        private void ChangeLane(int direction)
        {
            int prev = _lane;
            _lane = Mathf.Clamp(_lane + direction, -1, 1);
            if (_lane != prev)
                OnLaneChanged?.Invoke(_lane - prev);
        }

        private void TryJump()
        {
            if (!IsGrounded || _state == SkateState.Crouch)
                return;

            _verticalVelocity = config.jumpForce;
            _state = SkateState.Air;
            OnJumped?.Invoke();
        }

        private void TryCrouch()
        {
            if (!IsGrounded)
                return;

            _state = SkateState.Crouch;
            _crouchTimer = config.crouchDuration;
            _bodyHeight = config.crouchHeight;
        }

        private void UpdateCrouch()
        {
            if (_state != SkateState.Crouch)
                return;

            if (_input != null && _input.CrouchHeld)
            {
                _crouchTimer = Mathf.Max(_crouchTimer, 0.12f);
                _bodyHeight = config.crouchHeight;
                return;
            }

            _crouchTimer -= Time.deltaTime;
            if (_crouchTimer > 0f)
                return;

            _bodyHeight = config.standHeight;
            _state = SkateState.Run;
        }

        private void SyncBodyCollider()
        {
            if (_bodyCollider == null)
                _bodyCollider = GetComponent<CapsuleCollider>();
            if (_bodyCollider == null)
                return;

            // The transform is already at the body's mid-height (_hop), so the capsule
            // centre belongs at zero. It used to be pushed up another 0.85 every frame,
            // which lifted the collider base to world y 0.90 — above the 0.645 top of
            // every ground obstacle. Ten of the twelve obstacle types could not touch the
            // player at all; only the overhead duck hazards ever registered a hit.
            //
            // Crouching drops the capsule instead of shrinking it in place, so ducking
            // actually moves the body under an overhead bar.
            if (_state == SkateState.Crouch)
            {
                _bodyCollider.height = 0.7f;
                _bodyCollider.center = new Vector3(0f, -0.40f, 0f);
                _bodyCollider.radius = 0.3f;
            }
            else
            {
                _bodyCollider.height = 1.5f;
                _bodyCollider.center = Vector3.zero;
                _bodyCollider.radius = 0.32f;
            }
        }

        private void Move()
        {
            float step = _speed * Time.deltaTime;
            _pathDistance += step;

            float laneTarget = _lane * config.laneOffset;
            float laneSpeed = config.laneOffset / Mathf.Max(0.05f, config.laneChangeSeconds);
            _lateral = Mathf.MoveTowards(_lateral, laneTarget, laneSpeed * Time.deltaTime);

            _verticalVelocity += config.gravity * Time.deltaTime;
            _hop += _verticalVelocity * Time.deltaTime;
            float minHop = _groundY + _bodyHeight * 0.5f;
            if (_hop <= minHop)
            {
                _hop = minHop;
                if (_verticalVelocity < 0f)
                    _verticalVelocity = 0f;
                if (_state == SkateState.Air)
                {
                    _state = SkateState.Run;
                    OnLanded?.Invoke();
                }
            }

            ApplyPathPose();
        }

        private void SnapToPath()
        {
            _hop = _groundY + _bodyHeight * 0.5f;
            ApplyPathPose();
        }

        /// Offline capture / editor framing — Update does not run in batch edit mode.
        public void SnapForCapture(float pathDistance)
        {
            _pathDistance = pathDistance;
            ResolveDeps();
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RunConfig>();
                config.name = "RunConfig (capture)";
            }

            _groundY = 0f;
            _bodyHeight = config.standHeight;
            _hop = _bodyHeight * 0.5f;
            _speed = config.baseSpeed;
            SnapToPath();
        }

        private void ApplyPathPose()
        {
            Yaw = 0f;
            Quaternion rot = DownhillPath.Rotation;
            Vector3 world = DownhillPath.Point(_pathDistance, _lateral, _hop);
            transform.SetPositionAndRotation(world, rot);
        }

        /// Call from obstacle triggers — casual soft fail, no hard death by default.
        /// Camera / SFX juice is owned by JuiceDirector (subscribed to OnSoftHit).
        public void SoftHit()
        {
            StageRunStats.Instance?.NotifySoftHit();

            if (_state == SkateState.Finish)
                return;

            _state = SkateState.SoftHit;
            _softHitTimer = config.softHitRecoverSeconds;
            _speed *= config.softHitSlowFactor;
            _tucking = false;
            OnSoftHit?.Invoke();
        }

        public void FinishRun()
        {
            _state = SkateState.Finish;
            _speed = Mathf.MoveTowards(_speed, 0f, 30f * Time.deltaTime);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (inputBehaviour != null && inputBehaviour is not IInputReader)
                Debug.LogWarning("PlayerController.inputBehaviour must implement IInputReader.", this);
            if (mapBehaviour != null && mapBehaviour is not IMapStream)
                Debug.LogWarning("PlayerController.mapBehaviour must implement IMapStream.", this);
        }
#endif
    }
}
