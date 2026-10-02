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
        private float _startHold;       // 출발 연출: 잠시 속도 0
        private float _laneFrom;        // lane easing: where the last change started
        private float _laneT = 1f;      // 0..1 progress of the current lane change
        private float _speedRecoverBoost;   // 피격 감속 후 가속 회복을 잠깐 빠르게
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
        public const float LaneEaseScale = 1.7f;    // 14차-9: 0.20s ease-out (골드런 0.18~0.22s) · 130차: 0.255s 로 조금 길게(부드럽게)

        private CapsuleCollider _bodyCollider;

        public float PathDistance => _pathDistance;
        public float Speed => _speed;
        public RunConfig Config => config;   // 14차-10: 점프대 코인 아치 계산용

        /// The transform sits at the body's mid-height (see EnsurePlayerPhysics), so a
        /// visual built with its feet at local y = 0 must hang this far below it.
        public float BodyHalfHeight => _bodyHeight * 0.5f;

        /// Bonus Time and similar power-ups: multiplies the speed target (1 = normal).
        public float SpeedBoost { get; set; } = 1f;
        /// 52차: true 면 피격 감속(소프트히트 0.55·바디체크 0.8)을 건너뛴다 — 보스전(BossDirector.Active) 동안.
        public static bool NoHitSlow => BossDirector.Active;

        /// While true obstacle hits are ignored (Bonus Time / Giant). Hazards still fire
        /// OnSoftHit-free feedback through JuiceDirector if they want to.
        public bool Invincible { get; set; }

        /// 거인 모드 등 — 비주얼·콜라이더 배율(1 = 기본, 2 = 200%).
        public float VisualScaleMul { get; set; } = 1f;

        /// 에디터 디버그(Coast Run/Dev/God mode - ON|OFF): 피격 무시. PlayerPrefs에 남는다.
        /// 76차: 옛 키가 에디터 PlayerPrefs 에 켜진 채 남아 장애물 피해가 전부 무시되고 있었다(「1 정도만 닳는다」의 진짜 원인) — 키를 바꾸고,
        /// 에디터·개발 빌드에서만 읽으며(출시 빌드는 항상 false), 켜져 있으면 HUD 에 빨간 GOD 배지를 띄운다.
        public const string DebugGodKey = "CoastRun.Debug.God2";
        public static bool DebugGod
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            get => PlayerPrefs.GetInt(DebugGodKey, 0) != 0;
#else
            get => false;
#endif
            set { PlayerPrefs.SetInt(DebugGodKey, value ? 1 : 0); PlayerPrefs.DeleteKey("CoastRun.Debug.God"); PlayerPrefs.Save(); }
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
            _iFrameTimer = 0f;
            _speedRecoverBoost = 0f;
            _startHold = 0f;
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

        /// 출발 연출: 잠깐 멈춰 있다 「출발」이 뜨면 다시 달린다.
        public void HoldForStart(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            _startHold = Mathf.Max(_startHold, seconds);
            _speed = 0f;
            FreezeInput(seconds);
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
                _iFrameTimer -= Time.unscaledDeltaTime;   // HitStop 중에도 경직 창이 실시간으로 끝나게
            if (_softHitTimer <= 0f)
                return;

            _softHitTimer -= Time.unscaledDeltaTime;
            if (_softHitTimer <= 0f && _state == SkateState.SoftHit)
                _state = SkateState.Run;
        }

        private void UpdateSpeed()
        {
            if (_startHold > 0f)
            {
                _startHold -= Time.deltaTime;
                _speed = 0f;
                return;
            }
            // v2 이동 모드: 스케이트보드는 기본·최대·가속 모두 ×1.3 (규칙은 동일, 반응 시간만 짧다).
            float mode = RunTuning.SpeedMul * ChapterDifficulty.SpeedMul;
            float maxSpeed = (upgrades != null ? upgrades.GetMaxSpeed() : config.maxSpeed) * mode;
            // 14차-9: 선형 가속 → 30초마다 +6% 계단(골드런/서브웨이 방식). 후반이 '반응 불가'로 치닫지 않고,
            // 속도 상한은 기본의 1.7배(≈18.7 m/s)에서 멈춘다. 계단 사이는 1.5 m/s² 로 부드럽게 붙는다.
            _runClock += Time.deltaTime;
            int tier = Mathf.FloorToInt(_runClock / 30f);
            float stepped = config.baseSpeed * mode * Mathf.Min(1.7f, 1f + 0.06f * tier);
            float target = Mathf.Min(maxSpeed, stepped);
            // 52차(사용자): 보스전 중엔 「느려지는」 느낌을 없앤다 — 피격해도 감속 없이 계속 달린다(보스 공격에 맞으면 HP 만 깎임).
            if (_state == SkateState.SoftHit && !NoHitSlow)
                target = config.baseSpeed * mode * config.softHitSlowFactor;

            _tucking = _input != null && _input.TuckHeld && IsGrounded && _state != SkateState.Crouch;
            if (_tucking)
                target *= config.tuckMultiplier;
            target *= Mathf.Max(0.1f, SpeedBoost);

            // 계단 사이는 1.5 m/s², 피격 직후 회복은 더 빠르게(전엔 수 초간 「갑자기 느려짐」). 부스터·감속은 즉시.
            if (_speedRecoverBoost > 0f) _speedRecoverBoost -= Time.deltaTime;
            float climb = (_speedRecoverBoost > 0f) ? 10f : 1.5f;
            float rate = (target > _speed && SpeedBoost <= 1.01f) ? climb : 20f;
            _speed = Mathf.MoveTowards(_speed, target, rate * Time.deltaTime);
        }

        private void HandleInput()
        {
            if (_input == null)
                return;

            // Lane changes stay live through a stumble. Jump/crouch only wait out the
            // short FreezeInput window — locking for the whole SoftHit recover (~0.9s)
            // felt like "keyboard died" mid-run.
            int laneDelta = _input.ConsumeLaneDelta();
            if (laneDelta != 0)
                ChangeLane(laneDelta);

            if (_inputFreezeTimer > 0f)
                return;

            // 228차: 일찍 누른 점프를 잠깐 들고 있다가 맞는 때에 뛴다(EarlyJumpDelay)
            if (_jumpDelay >= 0f)
            {
                if (!IsGrounded || _state == SkateState.SoftHit) _jumpDelay = -1f;
                else { _jumpDelay -= Time.deltaTime; if (_jumpDelay <= 0f) { _jumpDelay = -1f; GroundJump(); } }
            }

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

        // ── 47차: 2단 점프 ─────────────────────────────────────────────
        private bool _doubleJumpUsed;
        public bool DoubleJumpUsed => _doubleJumpUsed;
        /// 48차-9(사용자): 2단 점프 최고점 = 1단 점프 최고점의 2배. 현재 높이에서 그 최고점까지 남은 거리로 초기속도를 역산한다.
        public const float DoubleJumpHeightMul = 3.4f;   // 127차(사용자 「2단점프 지금보다 30% 높이」): 2.0 → 2.6 · 132차: 한 번 더 +30% → 3.4
        /// 공중에서 두 번째 점프가 나간 순간(허공 디딤 연출용). OnJumped 도 같이 나간다.
        public event Action OnDoubleJumped;

        private void TryJump()
        {
            // Coyote time: a jump issued just after the wheels leave the ground still
            // counts. Without it, a jump at the lip of anything reads as ignored.
            bool canJump = IsGrounded || _coyoteTimer > 0f;
            if (!canJump)
            {
                // 47차: 2단 점프 — 공중에서 위로 한 번 더 밀면 허공을 딛고 다시 뛴다(공중당 1회, 활공·피니시 제외).
                if (_state == SkateState.Air && !_doubleJumpUsed && !_gliding)
                {
                    _doubleJumpUsed = true;
                    // 1단 최고 높이 h1 = v²/2g → 목표 최고점 2·h1(바닥 기준). 지금 높이 y0 에서 남은 (2h1 − y0) 만큼 오르는 속도.
                    float g = Mathf.Max(0.01f, -config.gravity);
                    float h1 = config.jumpForce * config.jumpForce / (2f * g);
                    float y0 = Mathf.Max(0f, _hop - (_groundY + _bodyHeight * 0.5f));
                    float remain = Mathf.Max(h1 * 1.0f, h1 * DoubleJumpHeightMul - y0);   // 너무 늦게 눌러도 최소 1.0·h1 은 더 오른다(127차 0.78 → 132차 1.0)
                    _verticalVelocity = Mathf.Sqrt(2f * g * remain);
                    OnDoubleJumped?.Invoke();
                    OnJumped?.Invoke();
                }
                return;
            }

            // 228차(게임성 테스트: 처음 하는 사람은 장애물이 보이자마자 0.6~0.7초 전에 눌러 장애물 바로 앞에 착지 → 맞음):
            //   같은 레인의 낮은 장애물이 0.42~0.9초 앞에 있으면 그 점프를 최대 0.25초 늦춰 「0.32초 전」 쯤에 뛴다.
            //   다시 누르면(두 번째 탭) 기다리지 않고 바로 뛴다.
            if (_jumpDelay < 0f && IsGrounded)
            {
                float d = EarlyJumpDelay();
                if (d > 0.02f) { _jumpDelay = d; return; }
            }
            _jumpDelay = -1f;
            GroundJump();
        }

        private float _jumpDelay = -1f;
        private const float JumpAssistMin = 0.42f, JumpAssistMax = 0.9f, JumpAssistIdeal = 0.32f, JumpAssistMaxDelay = 0.25f;

        /// 같은 레인 바로 앞 「넘을 수 있는(낮은)」 장애물까지 남은 시간으로 점프를 얼마나 늦출지(초). 해당 없으면 0.
        private float EarlyJumpDelay()
        {
            if (_speed < 1f) return 0f;
            float best = 99f; ObstacleHazard bestH = null;
            var list = ObstacleHazard.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i]; if (h == null) continue;
                var c = h.GetComponent<Collider>(); if (c == null || !c.enabled) continue;
                var rel = transform.InverseTransformPoint(c.bounds.center);
                if (rel.z <= 0.3f || Mathf.Abs(rel.x) > 1.0f) continue;
                float ttc = rel.z / _speed;
                if (ttc < best) { best = ttc; bestH = h; }
            }
            if (bestH == null || best < JumpAssistMin || best > JumpAssistMax) return 0f;
            if (bestH.ClassifyHit(this) == HitKind.Bounce) return 0f;   // 높은 장애물은 어차피 못 넘음 — 손대지 않음
            return Mathf.Min(JumpAssistMaxDelay, best - JumpAssistIdeal);
        }

        private void GroundJump()
        {
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
            _crouchTimer = Mathf.Min(config.crouchDuration, 0.6f);   // 228차: 0.4 → 0.6초(일찍 숙여도 빨래줄·허들 밑을 지나가게)
            _crouchAge = 0f;
            _bodyHeight = config.crouchHeight;
        }

        /// 236차(난이도 점검): 숙인 채 일어나려는 순간 머리 위 장애물(빨랫줄·등불 줄·허들)이 바로 앞이나 몸 위에 있으면
        ///   그게 지나갈 때까지 조금 더 숙인다(한 번 숙임에 최대 1.2 s). 0.6 s 쯤 일찍 숙였다가 막 일어나며 걸리던 것 — 테스트 피격의 상당수.
        private float _crouchAge;
        private const float CrouchAssistMax = 1.2f;
        private bool DuckAhead()
        {
            float fwd = Mathf.Max(1f, _speed) * 0.3f;
            foreach (var d in FindObjectsByType<DuckHazard>(FindObjectsSortMode.None))
            {
                if (d == null) continue;
                var c = d.GetComponent<Collider>(); if (c == null || !c.enabled) continue;
                var rel = transform.InverseTransformPoint(c.bounds.center);
                var e = c.bounds.extents; float half = Mathf.Max(e.x, e.z);
                if (rel.z > -half - 0.6f && rel.z < half + fwd && Mathf.Abs(rel.x) < half + 1.2f) return true;
            }
            return false;
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
            _crouchAge += Time.deltaTime;
            if (_crouchTimer > 0f)
                return;
            if (_crouchAge < CrouchAssistMax && DuckAhead()) { _crouchTimer = 0.05f; return; }   // 236차: 머리 위 장애물이 지나갈 때까지

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
            float m = Mathf.Max(0.5f, VisualScaleMul);
            if (_state == SkateState.Crouch)
            {
                _bodyCollider.height = 0.55f * m;
                _bodyCollider.center = new Vector3(0f, -0.47f * m, 0f);
                _bodyCollider.radius = 0.2f * m;
            }
            else
            {
                _bodyCollider.height = 1.3f * m;
                _bodyCollider.center = new Vector3(0f, -0.05f * m, 0f);
                _bodyCollider.radius = 0.21f * m;
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
                // 7차: 부드럽게 — 시간을 늘리고 ease. HitStop(timeScale↓) 중에도 좌우가 안 늘어지게 unscaled 사용.
                float dur = Mathf.Max(0.16f, config.laneChangeSeconds * LaneEaseScale * RunTuning.LaneMul);
                _laneT = Mathf.Min(1f, _laneT + Time.unscaledDeltaTime / dur);
                float t = _laneT;
                // 14차-9: ease-out · 27차: easeOutBack
                float c1 = config != null ? config.laneOvershoot : 0f;
                float u = t - 1f;
                // 130차(사용자): 출발이 툭 튀지 않게 — 앞 30 % 는 살짝 ease-in(smoothstep 과 섞음), 뒤는 기존 ease-out(-back)
                float eOut = c1 > 0.001f ? 1f + (c1 + 1f) * u * u * u + c1 * u * u : 1f - (1f - t) * (1f - t) * (1f - t);
                float eSmooth = t * t * (3f - 2f * t);
                float e = Mathf.Lerp(eSmooth, eOut, Mathf.Clamp01(t / 0.3f) * 0.6f + 0.4f);
                _lateral = Mathf.Lerp(_laneFrom, laneTarget, e);
            }
            else
            {
                _lateral = laneTarget;
            }
            // 레인 이동이 unscaled 이므로 속도도 unscaled 기준(히트스톱 중 deltaTime≈0 이면 카메라 기울기가 폭발하던 것).
            float latDt = Time.unscaledDeltaTime;
            LateralVelocity = latDt > 1e-5f ? (_lateral - prevLateral) / latDt : 0f;

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
                _doubleJumpUsed = false;
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

            Vector3 sweepFrom = transform.position;
            ApplyPathPose();
            SweepHazards(sweepFrom, transform.position);
        }

        // ── 219차(사용자: 「통과되는 장애물이 있음」, 움직임 QA 재현): 뚫림 방지 ──────────────
        // 몸은 매 프레임 위치를 옮기는 키네마틱이라, 프레임이 늦어 한 번에 0.5~1.6 m 를 건너뛰면 두께 0.3~0.6 m 장애물의
        // 트리거를 아예 안 거치고 지나갈 수 있다(연속 충돌 검사는 트리거에 안 먹는다). 지난 위치 → 지금 위치를 몸 캡슐로 훑어,
        // 「지나오는 중에 닿았는데 지금은 벗어난」 장애물·머리 위 장애물에만 같은 OnTriggerEnter 를 대신 불러 준다.
        // 시작 때부터 겹쳐 있던 것(지난 프레임에 이미 처리)과 지금도 겹친 것(물리 트리거가 처리)은 건드리지 않는다 — 두 번 맞지 않게.
        private static readonly RaycastHit[] _sweepHits = new RaycastHit[24];
        private static readonly Collider[] _sweepOverlap = new Collider[24];

        private void SweepHazards(Vector3 from, Vector3 to)
        {
            if (_bodyCollider == null || !_bodyCollider.enabled || !_bodyCollider.gameObject.activeInHierarchy) return;
            Vector3 d = to - from; float dist = d.magnitude;
            if (dist > 8f) return;   // 순간이동(재시작·워프)
            BodyCapsule(to, out var b1, out var b2, out float r);
            int m = Physics.OverlapCapsuleNonAlloc(b1, b2, r, _sweepOverlap, ~0, QueryTriggerInteraction.Collide);
            // (1) 지금 겹친 장애물(머리 위 장애물 포함): 물리 트리거가 놓쳐도 여기서 맞는다(QA 20fps 에서 한가운데 콘·빨랫줄이 트리거 없이 지나간 사례).
            //     머리 위 장애물은 숙인 동안엔 DuckHazard 쪽에서 그냥 넘어간다(기존 규칙 그대로).
            //     장애물 쪽 _popped 가드로 물리 트리거와 겹쳐 불려도 한 번만 처리된다.
            for (int j = 0; j < m; j++)
            {
                var c = _sweepOverlap[j];
                if (c == null || !c.enabled || !c.isTrigger) continue;
                if (c.GetComponent<ObstacleHazard>() != null || c.GetComponent<DuckHazard>() != null) c.SendMessage("OnTriggerEnter", _bodyCollider, SendMessageOptions.DontRequireReceiver);
            }
            if (dist < 0.02f) return;
            // (2) 이번 프레임에 건너뛴 장애물(지나오며 닿았지만 지금은 벗어남) — 머리 위 장애물(숙이기)도 포함
            BodyCapsule(from, out var a1, out var a2, out _);
            int n = Physics.CapsuleCastNonAlloc(a1, a2, r, d / dist, _sweepHits, dist, ~0, QueryTriggerInteraction.Collide);
            if (n <= 0) return;
            for (int i = 0; i < n; i++)
            {
                var c = _sweepHits[i].collider;
                if (c == null || !c.enabled || !c.isTrigger || _sweepHits[i].distance <= 0f) continue;
                if (c.GetComponent<ObstacleHazard>() == null && c.GetComponent<DuckHazard>() == null) continue;
                bool stillInside = false;
                for (int j = 0; j < m; j++) if (_sweepOverlap[j] == c) { stillInside = true; break; }
                if (stillInside) continue;
                c.SendMessage("OnTriggerEnter", _bodyCollider, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void BodyCapsule(Vector3 pos, out Vector3 p1, out Vector3 p2, out float radius)
        {
            var s = transform.lossyScale;
            radius = _bodyCollider.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            float h = _bodyCollider.height * Mathf.Abs(s.y);
            Vector3 c = pos + transform.rotation * Vector3.Scale(_bodyCollider.center, s);
            float half = Mathf.Max(0f, h * 0.5f - radius);
            Vector3 up = transform.up;
            p1 = c + up * half; p2 = c - up * half;
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
        public void SoftHit()
        {
            PendingHitDamageMul = ObstacleHazard.DefaultFrac;
            SoftHit(HitKind.Trip, 0);
        }

        /// Trip: a knee-high thing (hurdle, cone) — she stumbles over it and keeps her
        /// lane. Bounce: a solid body (car, crate, bench) — she is knocked sideways into
        /// the neighbouring lane, which is what a chest-high hit looks like. `bounceDir`
        /// is the side she deflects to (+1 right); 0 lets the controller choose.
        /// 피격 직후 경직 중복 방지 창(순발력 ↑ → 길어짐). 76차(사용자 「최소 데미지 HP 30」): 이 창은 **경직·넉백이 겹치는 것만** 막고
        /// 피해는 막지 않는다 — 전엔 무적프레임(0.8~2 s) 안에 두 번째 장애물을 치면 꽈당만 나고 HP 가 안 닳아 「1 정도만 닳는다」로 보였다.
        private float _iFrameTimer;
        public bool InIFrames => _iFrameTimer > 0f;
        /// 22차-7: 다음 피격의 피해 배율(장애물이 SoftHit 직전에 넣고, HealthSystem이 쓰고 1로 되돌린다).
        public float PendingHitDamageMul = ObstacleHazard.DefaultFrac;   // 71차: 최대 체력 비율
        /// 76차: 피해 이벤트(HealthSystem 이 듣는다). OnSoftHit(경직·꽈당 연출)와 분리 — 경직 창 안의 충돌도 피해는 낸다.
        public event Action OnHitDamage;
        /// 같은 장애물의 겹친 콜라이더가 두 번 피해를 내지 않게 하는 디바운스(실시간).
        /// HitStop 중 Time.time 이 거의 안 가서 피해가 수 초간 막히던 버그 → unscaled 사용.
        public const float SameHitDebounce = 0.15f;
        private float _lastDamageTime = -10f;

        public void SoftHit(HitKind kind, int bounceDir)
        {
            SoftHitApplied(kind, bounceDir);
        }

        /// true = 경직·상태까지 적용됨. false = 무적(피버·거인·보너스)이거나 경직 창 안(피해는 이미 냈을 수 있음 — 호출측이 꽈당을 낸다).
        public bool SoftHitApplied(HitKind kind, int bounceDir)
        {
            if (_state == SkateState.Finish)
                return false;

            // 활공 중 충돌은 활공을 끊고 일반 피격으로 — 꽈당이 빠지지 않게.
            if (_gliding)
                EndGlide();

            if (Invincible || FeverMode.Active)
            {
                PendingHitDamageMul = ObstacleHazard.DefaultFrac;
                if (DebugGod) Debug.LogWarning("[Hit] 무시 — God mode(Coast Run/Dev/God mode - OFF 로 끌 것)");
                return false;
            }

            // 피해는 무적프레임과 무관하게 매 충돌마다. 겹친 콜라이더만 0.15 s(실시간) 디바운스.
            if (Time.unscaledTime - _lastDamageTime > SameHitDebounce)
            {
                _lastDamageTime = Time.unscaledTime;
                StageRunStats.Instance?.NotifySoftHit();
                if (ArcadeRun.Active) ArcadeRun.OnHit();
                OnHitDamage?.Invoke();
            }
            else
                PendingHitDamageMul = ObstacleHazard.DefaultFrac;

            if (_iFrameTimer > 0f)
                return false;   // 경직·넉백은 겹치지 않게(피해는 위에서 이미 적용)

            _state = SkateState.SoftHit;
            _softHitTimer = config.softHitRecoverSeconds * RunTuning.HitFreezeMul;
            _iFrameTimer = RunTuning.DashInvincible;
            if (!NoHitSlow)
            {
                _speed *= config.softHitSlowFactor;
                _speedRecoverBoost = 2.8f;   // SoftHit 끝난 뒤 가속 회복 빠르게
            }
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
                if (!NoHitSlow) _speed *= 0.8f;                 // a body check bleeds more speed than a trip
                ChangeLane(dir);
                FreezeInput(0.25f * RunTuning.HitFreezeMul);
            }
            OnSoftHit?.Invoke();
            return true;
        }

        public HitKind LastHitKind { get; private set; }
        public int LastBounceDir { get; private set; }

        public void FinishRun()
        {
            // 활공 중 골인이면 즉시 줄·공중 상태 해제 후 땅에 붙인다(하늘에 남는 버그)
            if (_gliding) EndGlide();
            _state = SkateState.Finish;
            _glideTimer = 0f;
            _laneT = 1f;
            _laneFrom = _lane * config.laneOffset;
            _verticalVelocity = 0f;
            _hop = _groundY + _bodyHeight * 0.5f;
            SnapToPath();
        }

        /// 결과창(사망·완주) — 즉시 정지. FinishRun 감속만으로는 timeScale 0 동안 속도가 안 줄어 워치독이 월드를 다시 푼다.
        public void HaltForResult()
        {
            if (_gliding) EndGlide();
            _state = SkateState.Finish;
            _speed = 0f;
            _verticalVelocity = 0f;
            _softHitTimer = 0f;
            _inputFreezeTimer = 0f;
            SnapToPath();
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
