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

    /// How an obstacle hit plays out: Trip = stumble over something knee-high and
    /// keep the lane; Bounce = a solid body (car, crate) knocks her into the next lane.
    public enum HitKind
    {
        Trip,
        Bounce
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
        private float _coyoteTimer;     // grace after leaving ground where a jump still counts
        private float _runClock;        // 14차-9: 계단식 가속용 런 경과 시간
        private float _laneFrom;        // lane easing: where the last change started
        private float _laneT = 1f;      // 0..1 progress of the current lane change
        private bool _tucking;
        private SkateState _state = SkateState.Run;

        public event Action OnSoftHit;
        public event Action OnLanded;
        public event Action OnJumped;
        public event Action<int> OnLaneChanged;
        /// 27차: 슬라이드(웅크림) 시작 — 리그가 납작 squash 를 건다.
        public event Action OnCrouched;
        /// 레인 이동 속도(m/s, +우). 리그가 몸을 기울이는 데 쓴다.
        public float LateralVelocity { get; private set; }
        /// 레인 이동 시간 배율 (config.laneChangeSeconds × 이 값). 0.15s 기본에 2.0 → 0.30s.
        public const float LaneEaseScale = 1.35f;   // 14차-9: 0.20s ease-out (골드런 0.18~0.22s)

        private CapsuleCollider _bodyCollider;

        public float PathDistance => _pathDistance;
        public float Speed => _speed;
        public RunConfig Config => config;   // 14차-10: 점프대 코인 아치 계산용

        /// The transform sits at the body's mid-height (see EnsurePlayerPhysics), so a
        /// visual built with its feet at local y = 0 must hang this far below it.
        public float BodyHalfHeight => _bodyHeight * 0.5f;

        /// Bonus Time and similar power-ups: multiplies the speed target (1 = normal).
        public float SpeedBoost { get; set; } = 1f;

        /// While true obstacle hits are ignored (Bonus Time). Hazards still fire
        /// OnSoftHit-free feedback through JuiceDirector if they want to.
        public bool Invincible { get; set; }

        /// 에디터 디버그(Coast Run/Debug/God mode): 피격 무시. PlayerPrefs에 남는다.
        public const string DebugGodKey = "CoastRun.Debug.God";
        public static bool DebugGod
        {
            get => PlayerPrefs.GetInt(DebugGodKey, 0) != 0;
            set { PlayerPrefs.SetInt(DebugGodKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
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
            EndGlide();
            _softHitTimer = 0f;
            _inputFreezeTimer = 0f;
            _verticalVelocity = 0f;
            if (config != null)
            {
                _bodyHeight = config.standHeight;
                _speed = config.baseSpeed;
            _runClock = 0f;
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
            _runClock = 0f;
            _bodyHeight = config.standHeight;
            _groundY = 0f;
            if (DebugGod) Invincible = true;
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
            if (config == null) return;
            if (_state == SkateState.Finish)
            {
                // 22차-5: 골인 뒤 몇 걸음 더 달리다 멈춘다(리본을 끊고 지나가는 느낌)
                if (_speed > 0.01f) { _speed = Mathf.MoveTowards(_speed, 0f, 7.5f * Time.deltaTime); Move(); _map?.SetPlayerDistance(_pathDistance); }
                return;
            }

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
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
            if (_softHitTimer <= 0f)
                return;

            _softHitTimer -= Time.deltaTime;
            if (_softHitTimer <= 0f && _state == SkateState.SoftHit)
                _state = SkateState.Run;
        }

        private void UpdateSpeed()
        {
            // v2 이동 모드: 스케이트보드는 기본·최대·가속 모두 ×1.3 (규칙은 동일, 반응 시간만 짧다).
            float mode = RunTuning.SpeedMul * ChapterDifficulty.SpeedMul;
            float maxSpeed = (upgrades != null ? upgrades.GetMaxSpeed() : config.maxSpeed) * mode;
            // 14차-9: 선형 가속 → 30초마다 +6% 계단(골드런/서브웨이 방식). 후반이 '반응 불가'로 치닫지 않고,
            // 속도 상한은 기본의 1.7배(≈18.7 m/s)에서 멈춘다. 계단 사이는 1.5 m/s² 로 부드럽게 붙는다.
            _runClock += Time.deltaTime;
            int tier = Mathf.FloorToInt(_runClock / 30f);
            float stepped = config.baseSpeed * mode * Mathf.Min(1.7f, 1f + 0.06f * tier);
            float target = Mathf.Min(maxSpeed, stepped);
            if (_state == SkateState.SoftHit)
                target = config.baseSpeed * mode * config.softHitSlowFactor;

            _tucking = _input != null && _input.TuckHeld && IsGrounded && _state != SkateState.Crouch;
            if (_tucking)
                target *= config.tuckMultiplier;
            target *= Mathf.Max(0.1f, SpeedBoost);

            // 계단 사이는 1.5 m/s² 로 붙고, 부스터 아이템·감속은 즉시.
            float rate = (target > _speed && SpeedBoost <= 1.01f) ? 1.5f : 20f;
            _speed = Mathf.MoveTowards(_speed, target, rate * Time.deltaTime);
        }

        private void HandleInput()
        {
            if (_input == null)
                return;

            // A hit slows you down; it must not also make you deaf. Lane changes stay
            // live through the stumble so a player can still steer out of the next
            // obstacle — that is the difference between "I got hit" and "the game
            // stopped listening". Jump and crouch wait for the freeze to lift, but the
            // input buffer keeps them warm so a flick during the freeze still lands.
            int laneDelta = _input.ConsumeLaneDelta();
            if (laneDelta != 0)
                ChangeLane(laneDelta);

            bool locked = _state == SkateState.SoftHit || _inputFreezeTimer > 0f;
            if (locked)
                return;

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

            bool wasCrouch = _state == SkateState.Crouch;
            _state = SkateState.Crouch;
            _crouchTimer = Mathf.Max(_crouchTimer, 0.12f);
            if (!wasCrouch) OnCrouched?.Invoke();
            _bodyHeight = config.crouchHeight;
        }

        private void ChangeLane(int direction)
        {
            int prev = _lane;
            _lane = Mathf.Clamp(_lane + direction, -1, 1);
            if (_lane == prev)
                return;

            // Start the ease from wherever we actually are, so a second flick mid-move
            // does not snap back and restart — it just bends toward the new lane.
            _laneFrom = _lateral;
            _laneT = 0f;
            OnLaneChanged?.Invoke(_lane - prev);
        }

        private void TryJump()
        {
            // Coyote time: a jump issued just after the wheels leave the ground still
            // counts. Without it, a jump at the lip of anything reads as ignored.
            bool canJump = IsGrounded || _coyoteTimer > 0f;
            if (!canJump)
                return;

            // Jumping out of a crouch is allowed — it is the natural way to cancel a duck
            // when the next obstacle is a low one. Stand up first so the capsule and
            // visuals agree.
            if (_state == SkateState.Crouch)
            {
                _bodyHeight = config.standHeight;
                _crouchTimer = 0f;
            }

            _verticalVelocity = config.jumpForce;
            _state = SkateState.Air;
            _coyoteTimer = 0f;
            OnJumped?.Invoke();
        }

        /// 14차-3: 점프 패드 — 입력 없이 큰 점프. 웅크린 중이면 일으켜 세우고, 피격 상태는 풀어 준다.
        public void LaunchFromPad(float velocityMul)
        {
            if (_state == SkateState.Finish)
                return;
            if (_state == SkateState.Crouch)
            {
                _bodyHeight = config.standHeight;
                _crouchTimer = 0f;
            }
            _softHitTimer = 0f;
            _verticalVelocity = Mathf.Max(_verticalVelocity, config.jumpForce * velocityMul);
            _state = SkateState.Air;
            _coyoteTimer = 0f;
            OnJumped?.Invoke();
        }

        // ── 17차: 빨래줄 잡고 활공 ─────────────────────────────────────
        private bool _gliding;
        private float _glideTimer, _glideHeight, _glideBoostPrev;
        public bool IsGliding => _gliding;
        public float GlideHeight => _glideHeight;
        public event Action OnGlideStart, OnGlideEnd;

        /// 점프대로 떠서 빨래줄에 닿으면 호출: `seconds` 동안 지면 `height` m 위를 `speedMul` 배속으로 날아간다.
        /// 날아가는 동안 무적, 레인 이동은 자유. 끝나면 중력으로 내려온다.
        public void GrabLine(float seconds, float height, float speedMul)
        {
            if (_state == SkateState.Finish || _gliding) return;
            _gliding = true;
            _glideTimer = seconds;
            _glideHeight = height;
            _softHitTimer = 0f; _inputFreezeTimer = 0f;
            _iFrameTimer = Mathf.Max(_iFrameTimer, seconds + 0.4f);
            _glideBoostPrev = SpeedBoost;
            SpeedBoost = Mathf.Max(SpeedBoost, speedMul);
            if (_state == SkateState.Crouch) { _bodyHeight = config.standHeight; _crouchTimer = 0f; }
            _state = SkateState.Air;
            _verticalVelocity = 0f;
            OnGlideStart?.Invoke();
        }

        private void EndGlide()
        {
            if (!_gliding) return;
            _gliding = false;
            SpeedBoost = _glideBoostPrev;
            OnGlideEnd?.Invoke();
        }

        private void TryCrouch()
        {
            if (!IsGrounded)
                return;

            // A tap ducks for just long enough to clear an overhead bar. Holding the
            // finger down keeps extending it via HoldCrouch. The old fixed 0.9 s pinned
            // the player for ~16 m at top speed with no way out — now a jump cancels it
            // (TryJump) and a lane flick still steers through it (HandleInput).
            _state = SkateState.Crouch;
            _crouchTimer = Mathf.Min(config.crouchDuration, 0.4f);
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
            // 14차-9: 골드런식 관용 — 몸 판정을 그림의 65% 폭으로, 슬라이드 중엔 절반 높이.
            // "안 닿은 것 같은데 죽었다"가 사라지고, 니어미스가 자주 나며 손맛이 붙는다.
            if (_state == SkateState.Crouch)
            {
                _bodyCollider.height = 0.55f;
                _bodyCollider.center = new Vector3(0f, -0.47f, 0f);
                _bodyCollider.radius = 0.2f;
            }
            else
            {
                _bodyCollider.height = 1.3f;
                _bodyCollider.center = new Vector3(0f, -0.05f, 0f);
                _bodyCollider.radius = 0.21f;
            }
        }

        private void Move()
        {
            float step = _speed * Time.deltaTime;
            _pathDistance += step;

            // Ease into the lane instead of sliding at constant speed. A constant-rate
            // MoveTowards reads as a conveyor belt; an ease-out reads as a body leaning.
            float laneTarget = _lane * config.laneOffset;
            float prevLateral = _lateral;
            if (_laneT < 1f)
            {
                // 7차: 부드럽게 — 시간을 늘리고(0.15→0.30s) ease-in-out(smootherstep)으로 출발·도착이 둘 다 완만하게.
                // 이동 중 다시 스와이프하면 _laneFrom이 현재 위치라 꺾이지 않고 이어서 휜다.
                float dur = Mathf.Max(0.12f, config.laneChangeSeconds * LaneEaseScale * RunTuning.LaneMul);
                _laneT = Mathf.Min(1f, _laneT + Time.deltaTime / dur);
                float t = _laneT;
                // 14차-9: ease-out(즉시 출발, 부드럽게 도착) — 스와이프 직후 몸이 바로 움직여 반응이 '붙는다'.
                // 27차: easeOutBack — 목표를 살짝 지나쳤다가 튕겨 돌아온다(laneOvershoot 0.6 ≈ 레인 폭의 3~4%). 고무 같은 도착.
                float c1 = config != null ? config.laneOvershoot : 0f;
                float u = t - 1f;
                float e = c1 > 0.001f ? 1f + (c1 + 1f) * u * u * u + c1 * u * u : 1f - (1f - t) * (1f - t) * (1f - t);
                _lateral = Mathf.Lerp(_laneFrom, laneTarget, e);
            }
            else
            {
                _lateral = laneTarget;
            }
            LateralVelocity = Time.deltaTime > 0f ? (_lateral - prevLateral) / Time.deltaTime : 0f;

            bool wasGrounded = _state != SkateState.Air;
            if (_gliding)
            {
                // 17차: 활공 — 중력 대신 줄 높이로 부드럽게 붙는다.
                _glideTimer -= Time.deltaTime;
                _hop = Mathf.MoveTowards(_hop, _groundY + _glideHeight, 9f * Time.deltaTime);
                _verticalVelocity = 0f;
                if (_glideTimer <= 0f) EndGlide();
            }
            else
            {
                _verticalVelocity += config.gravity * Time.deltaTime;
                _hop += _verticalVelocity * Time.deltaTime;
            }
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
                _coyoteTimer = 0.12f;
            }
            else if (wasGrounded && _state != SkateState.Air)
            {
                // Left the ground without jumping (a drop) — open the grace window.
                _coyoteTimer -= Time.deltaTime;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);
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
            _runClock = 0f;
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
        public void SoftHit() => SoftHit(HitKind.Trip, 0);

        /// Trip: a knee-high thing (hurdle, cone) — she stumbles over it and keeps her
        /// lane. Bounce: a solid body (car, crate, bench) — she is knocked sideways into
        /// the neighbouring lane, which is what a chest-high hit looks like. `bounceDir`
        /// is the side she deflects to (+1 right); 0 lets the controller choose.
        /// 피격 직후 무적(순발력 ↑ → 길어짐). 연속 충돌로 HP가 녹는 것을 막는 '무적 대시'.
        private float _iFrameTimer;
        public bool InIFrames => _iFrameTimer > 0f;
        /// 22차-7: 다음 피격의 피해 배율(장애물이 SoftHit 직전에 넣고, HealthSystem이 쓰고 1로 되돌린다).
        public float PendingHitDamageMul = 1f;

        public void SoftHit(HitKind kind, int bounceDir)
        {
            if (Invincible || _state == SkateState.Finish || _iFrameTimer > 0f || _gliding)
                return;

            StageRunStats.Instance?.NotifySoftHit();
            if (ArcadeRun.Active) ArcadeRun.OnHit();

            _state = SkateState.SoftHit;
            _softHitTimer = config.softHitRecoverSeconds * RunTuning.HitFreezeMul;
            _iFrameTimer = RunTuning.DashInvincible;
            _speed *= config.softHitSlowFactor;
            _tucking = false;

            LastHitKind = kind;
            LastBounceDir = 0;
            if (kind == HitKind.Bounce)
            {
                int dir = bounceDir;
                if (dir == 0)
                    dir = _lane >= 0 ? -1 : 1;
                if (_lane + dir < -1 || _lane + dir > 1)
                    dir = -dir;
                LastBounceDir = dir;
                _speed *= 0.8f;                 // a body check bleeds more speed than a trip
                ChangeLane(dir);
                FreezeInput(0.25f * RunTuning.HitFreezeMul);
            }
            OnSoftHit?.Invoke();
        }

        public HitKind LastHitKind { get; private set; }
        public int LastBounceDir { get; private set; }

        public void FinishRun()
        {
            _state = SkateState.Finish;
            _gliding = false; _laneT = 1f; _laneFrom = _lane * config.laneOffset;
            _hop = _groundY + _bodyHeight * 0.5f;   // 23차-1: 0으로 두면 한 프레임 땅 밑으로 꺼졌다 올라온다
            _verticalVelocity = 0f;
        }

        /// 23차-1: 발 위치(월드) — transform은 몸 중심(캡슐)이라 카메라 기준으로는 이게 편하다.
        public Vector3 FeetPosition => transform.position - DownhillPath.Normal * (_bodyHeight * 0.5f);

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
