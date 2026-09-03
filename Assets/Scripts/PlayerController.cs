using UnityEngine;

public enum Lane
{
    Left = -1,
    Center = 0,
    Right = 1
}

public enum DeathCause
{
    None,
    Crash,
    Fell,
    WrongTurn,
    Collapsed,
    Retrieved,
    TimedOut
}

/// Listed low to high. Whoever sits further down this list wins when two things
/// happen on the same frame.
public enum PlayerState
{
    Run,
    Slide,
    Air,
    WallRun,
    Grind,
    Hurt,
    Invuln,
    Counter,
    Dead
}

/// Physics-free runner. Movement is path-distance + yaw, hits are AABB overlaps.
[RequireComponent(typeof(PlayerVitals))]
public class PlayerController : MonoBehaviour
{
    private GameConfig _cfg;
    private PlayerVitals _vitals;

    private Lane _currentLane = Lane.Center;
    private float _currentSpeed;
    private float _rampSpeed;
    private float _verticalVelocity;
    private bool _isSliding;
    private float _slideTimer;
    private float _bodyHeight;
    private float _bodyRadius;
    private Vector2 _touchStart;
    private bool _touchActive;
    private bool _isDead;
    private bool _falling;
    private float _groundY = 0f;

    private float _yaw;
    private float _lateral;
    private float _pathDistance;

    private bool _hasPrompt;
    private TurnPrompt _prompt;
    private int _pendingTurn;
    private float _handledTurnDistance = float.NegativeInfinity;
    private int _lastTurnDirection;
    private float _turnLeanTimer;

    private float _hurtTimer;
    private float _counterTimer;
    private bool _grinding;
    private bool _wallRunning;
    private float _speedLock;
    private float _hitStopTimer;

    private float _jumpBufferedAt = float.NegativeInfinity;
    private float _slideBufferedAt = float.NegativeInfinity;
    private float _lastGroundedAt = float.NegativeInfinity;

    public float CurrentSpeed => _currentSpeed;
    public bool IsDead => _isDead;
    public bool IsSliding => _isSliding;
    public bool IsGrounded => !_isDead && HeightAboveGround <= 0.02f && _verticalVelocity <= 0.01f;
    public bool IsJumping => !IsGrounded && _verticalVelocity > 0.5f;
    public Lane CurrentLane => _currentLane;
    public float LaneOffset => _cfg != null ? _cfg.laneOffset : 2.5f;
    public float VerticalVelocity => _verticalVelocity;
    public float PathDistance => _pathDistance;
    public float Yaw => _yaw;
    public float LateralOffset => _lateral;
    public float LaneTargetOffset => (int)_currentLane * LaneOffset;
    public bool IsHurt => _hurtTimer > 0f;
    public bool IsFalling => _falling;
    public bool IsGrinding => _grinding;
    public DeathCause Cause { get; private set; } = DeathCause.None;
    public PlayerVitals Vitals => _vitals;
    public float SpeedMultiplier { get; set; } = 1f;
    public int TurnHintDirection => _hasPrompt && _pathDistance >= _prompt.WindowStart ? _prompt.Direction : 0;
    public int TurnLeanDirection => _turnLeanTimer > 0f ? _lastTurnDirection : 0;
    public float NormalizedSpeed
    {
        get
        {
            if (_cfg == null)
                return 0f;
            return Mathf.InverseLerp(_cfg.baseSpeed, MaxSpeed, _currentSpeed);
        }
    }

    private float HeightAboveGround => transform.position.y - (_groundY + _bodyHeight * 0.5f);

    public PlayerState State
    {
        get
        {
            if (_isDead)
                return PlayerState.Dead;
            if (_counterTimer > 0f)
                return PlayerState.Counter;
            if (_vitals != null && _vitals.IsInvulnerable)
                return PlayerState.Invuln;
            if (_hurtTimer > 0f)
                return PlayerState.Hurt;
            if (_grinding)
                return PlayerState.Grind;
            if (_wallRunning)
                return PlayerState.WallRun;
            if (!IsGrounded)
                return PlayerState.Air;
            if (_isSliding)
                return PlayerState.Slide;
            return PlayerState.Run;
        }
    }

    public bool CanCounter =>
        !_isDead &&
        _hurtTimer <= 0f &&
        _counterTimer <= 0f &&
        (_vitals == null || _vitals.CounterLockout <= 0f) &&
        !_isSliding;

    private void Awake()
    {
        _cfg = GameConfig.Active;
        _vitals = GetComponent<PlayerVitals>();
        _bodyHeight = _cfg.standHeight;
        _bodyRadius = _cfg.standRadius;
        _currentSpeed = _cfg.bossFixedSpeed;
        _rampSpeed = _cfg.bossFixedSpeed;
        _groundY = 0f;

        // Strip any leftover CharacterController — movement is transform-only.
        CharacterController legacy = GetComponent<CharacterController>();
        if (legacy != null)
            Destroy(legacy);

        Collider selfCol = GetComponent<Collider>();
        if (selfCol != null)
            Destroy(selfCol);

        _pathDistance = 0f;
        _yaw = Mathf.Repeat(transform.eulerAngles.y, 360f);
        SnapToPose();
    }

    private void Update()
    {
        if (_hitStopTimer > 0f)
        {
            _hitStopTimer -= Time.unscaledDeltaTime;
            return;
        }

        if (_isDead)
        {
            UpdateDeathFall();
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        if (IsGrounded)
            _lastGroundedAt = Time.time;

        UpdateTimers();
        UpdateSpeed();
        UpdateCorner();

        if (_isDead)
            return;

        HandleInput();
        ConsumeBufferedInput();
        UpdateSlide();
        Move();
        ResolveHazards();
    }

    private void UpdateTimers()
    {
        if (_hurtTimer > 0f)
            _hurtTimer -= Time.deltaTime;
        if (_counterTimer > 0f)
            _counterTimer -= Time.deltaTime;
        if (_turnLeanTimer > 0f)
            _turnLeanTimer -= Time.deltaTime;
    }

    private void UpdateSpeed()
    {
        _rampSpeed = _speedLock > 0f
            ? _speedLock
            : Mathf.Min(MaxSpeed, _cfg.baseSpeed + _cfg.speedPerMeter * _pathDistance);

        float factor = (_vitals != null ? _vitals.SpeedFactor : 1f) * Mathf.Max(0.1f, SpeedMultiplier);
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _rampSpeed * factor, 25f * Time.deltaTime);
    }

    private float MaxSpeed
    {
        get
        {
            float bonus = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.Mods().maxSpeedBonus : 0f;
            return _cfg.maxSpeed + bonus;
        }
    }

    private float InputBuffer
    {
        get
        {
            float mul = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.Mods().inputBufferMul : 1f;
            return _cfg.inputBufferSec * mul;
        }
    }

    private float LaneChangeSeconds
    {
        get
        {
            float mul = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.Mods().laneChangeMul : 1f;
            return Mathf.Max(0.05f, _cfg.laneChangeSeconds * mul);
        }
    }

    public float RampSpeed => _rampSpeed;

    public void LockSpeed(float speed)
    {
        _speedLock = Mathf.Max(0f, speed);
    }

    public void UnlockSpeed()
    {
        _speedLock = 0f;
    }

    /// Brief unscaled freeze used on a successful counter.
    public void HitStop(float seconds)
    {
        _hitStopTimer = Mathf.Max(_hitStopTimer, seconds);
    }

    private void UpdateCorner()
    {
        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner == null)
        {
            _hasPrompt = false;
            return;
        }

        _hasPrompt = spawner.TryGetTurn(_pathDistance, out _prompt);
        if (_hasPrompt && _prompt.TurnDistance <= _handledTurnDistance + 0.01f)
            _hasPrompt = false;

        if (!_hasPrompt)
        {
            _pendingTurn = 0;
            return;
        }

        if (_pendingTurn != 0)
        {
            if (_pathDistance >= _prompt.TurnDistance)
                ExecuteTurn();
            return;
        }

        if (_pathDistance > _prompt.TurnDistance + _cfg.turnGraceMetres)
            Die(DeathCause.Fell);
    }

    private void ExecuteTurn()
    {
        int direction = _pendingTurn;
        _pendingTurn = 0;
        _handledTurnDistance = _prompt.TurnDistance;
        _hasPrompt = false;
        _lastTurnDirection = direction;
        _turnLeanTimer = 0.45f;
        _currentLane = Lane.Center;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayTurn();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            Steer(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            Steer(1);

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            _jumpBufferedAt = Time.time;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            _slideBufferedAt = Time.time;

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            _touchStart = touch.position;
            _touchActive = true;
            return;
        }

        if (!_touchActive)
            return;

        if (touch.phase == TouchPhase.Canceled)
        {
            _touchActive = false;
            return;
        }

        if (touch.phase != TouchPhase.Ended)
            return;

        _touchActive = false;
        Vector2 delta = touch.position - _touchStart;
        if (delta.magnitude < _cfg.swipeThresholdPx)
        {
            if (ItemSlot.Instance != null)
                ItemSlot.Instance.Activate();
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            Steer(delta.x > 0f ? 1 : -1);
        else if (delta.y > 0f)
            _jumpBufferedAt = Time.time;
        else
            _slideBufferedAt = Time.time;
    }

    private void ConsumeBufferedInput()
    {
        if (Time.time - _jumpBufferedAt <= InputBuffer && TryJump())
            _jumpBufferedAt = float.NegativeInfinity;

        if (Time.time - _slideBufferedAt <= InputBuffer && TrySlide())
            _slideBufferedAt = float.NegativeInfinity;
    }

    private void Steer(int direction)
    {
        if (_hurtTimer > 0f || _grinding)
            return;

        if (_hasPrompt && _pathDistance >= _prompt.WindowStart)
        {
            if (direction == _prompt.Direction)
                _pendingTurn = direction;
            else
                Die(DeathCause.WrongTurn);
            return;
        }

        ChangeLane(direction);
    }

    private void ChangeLane(int direction)
    {
        int next = Mathf.Clamp((int)_currentLane + direction, (int)Lane.Left, (int)Lane.Right);
        _currentLane = (Lane)next;
    }

    /// Used by the king onboarding when the arena is forcibly narrowed to one lane.
    public void ForceLane(Lane lane)
    {
        _currentLane = lane;
    }

    private bool GroundedEnough => IsGrounded || Time.time - _lastGroundedAt <= _cfg.coyoteTimeSec;

    private bool TryJump()
    {
        if (_isSliding || _hurtTimer > 0f || _grinding)
            return false;
        if (!GroundedEnough || _verticalVelocity > 0.5f)
            return false;

        _verticalVelocity = _cfg.jumpForce;
        _lastGroundedAt = float.NegativeInfinity;
        return true;
    }

    private bool TrySlide()
    {
        if (!GroundedEnough || _hurtTimer > 0f || _grinding)
            return false;

        _slideTimer = _cfg.slideDuration;
        if (_isSliding)
            return true;

        _isSliding = true;
        _bodyHeight = _cfg.slideHeight;
        _bodyRadius = _cfg.slideRadius;
        return true;
    }

    private void UpdateSlide()
    {
        if (!_isSliding)
            return;

        _slideTimer -= Time.deltaTime;
        if (_slideTimer > 0f)
            return;

        EndSlide();
    }

    private void EndSlide()
    {
        _isSliding = false;
        _bodyHeight = _cfg.standHeight;
        _bodyRadius = _cfg.standRadius;
    }

    private void Move()
    {
        float step = _currentSpeed * Time.deltaTime;
        _pathDistance += step;

        // Hard deadline: lane change finishes inside laneChangeSeconds.
        float laneSpeed = LaneOffset / LaneChangeSeconds;
        _lateral = Mathf.MoveTowards(_lateral, LaneTargetOffset, laneSpeed * Time.deltaTime);

        _verticalVelocity += _cfg.gravity * Time.deltaTime;

        float y = transform.position.y + _verticalVelocity * Time.deltaTime;
        float minY = _groundY + _bodyHeight * 0.5f;
        if (y <= minY)
        {
            y = minY;
            if (_verticalVelocity < 0f)
                _verticalVelocity = 0f;
        }

        ApplyPathPose(y);
    }

    private void SnapToPose()
    {
        float y = _groundY + _bodyHeight * 0.5f;
        ApplyPathPose(y);
    }

    /// Places the runner on the centre line at the current path distance, then
    /// offsets sideways into the active lane — corners arc instead of snapping.
    private void ApplyPathPose(float y)
    {
        Vector3 centre = transform.position;
        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner != null && spawner.TryGetPoint(_pathDistance, out centre, out float pathYaw))
        {
            _yaw = pathYaw;
            Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
            centre += rot * new Vector3(_lateral, 0f, 0f);
            transform.position = new Vector3(centre.x, y, centre.z);
            transform.rotation = rot;
            return;
        }

        Quaternion fallback = Quaternion.Euler(0f, _yaw, 0f);
        transform.position = new Vector3(centre.x, y, centre.z);
        transform.rotation = fallback;
    }

    private void ResolveHazards()
    {
        Vector3 size = new Vector3(_bodyRadius * 2f, _bodyHeight, _bodyRadius * 2f);
        Pickup.CollectOverlaps(transform.position, size);

        if (_isDead || (_vitals != null && _vitals.Invincible))
            return;

        HazardVolume hit = HazardRegistry.Instance.FirstOverlap(transform.position, size);
        if (hit == null)
            return;

        if (hit.IsLowBarrier && (_isSliding || _grinding))
            return;

        TakeHit(HitKind.Obstacle);
    }

    private void UpdateDeathFall()
    {
        if (!_falling)
            return;

        _verticalVelocity += _cfg.gravity * Time.deltaTime;
        Vector3 forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        transform.position += forward * (6f * Time.deltaTime) + Vector3.up * (_verticalVelocity * Time.deltaTime);

        if (transform.position.y < -60f)
            _falling = false;
    }

    /// Soft-fail from the first-run teaching units: keep HP, lose speed.
    public void ApplySoftFail(float speedFactor)
    {
        _currentSpeed *= Mathf.Clamp(speedFactor, 0.1f, 1f);
        if (_isSliding)
            EndSlide();
        Shake(0.2f, 0.25f);
    }

    public bool TakeHit(HitKind kind)
    {
        return TakeHit(kind, false);
    }

    public bool TakeHit(HitKind kind, bool scripted)
    {
        if (_isDead || _vitals == null)
            return false;

        if (!scripted &&
            TutorialDirector.Instance != null &&
            TutorialDirector.Instance.TrySoftFail(this))
            return false;

        HitResult result = _vitals.ApplyHit(kind);
        if (result == HitResult.Ignored)
            return false;

        if (KingFight.Instance != null)
            KingFight.Instance.NoteHit();

        if (result == HitResult.Fatal)
        {
            if (KingFight.Instance != null && KingFight.Instance.TryAbsorbDefeat())
                return true;

            Die(DeathCause.Crash);
            return true;
        }

        _hurtTimer = 0.3f;
        if (_isSliding)
            EndSlide();

        if (GameManager.Instance != null)
            GameManager.Instance.ReportHit();

        Shake(_cfg.cameraProfile != null ? _cfg.cameraProfile.hitShakeAmp : 0.35f,
              _cfg.cameraProfile != null ? _cfg.cameraProfile.hitShakeDur : 0.25f);

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayHit();

        return true;
    }

    public void EnterCounter(float seconds)
    {
        _counterTimer = Mathf.Max(_counterTimer, seconds);
        _hurtTimer = 0f;
    }

    public void SetGrinding(bool on)
    {
        if (_grinding == on)
            return;
        _grinding = on;
        if (on && _isSliding)
            EndSlide();
    }

    public void SetWallRunning(bool on)
    {
        _wallRunning = on;
    }

    public void Kill(DeathCause cause)
    {
        Die(cause);
    }

    private void Shake(float strength, float seconds)
    {
        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null)
            cam.Shake(strength, seconds);
    }

    private void Die(DeathCause cause)
    {
        if (_isDead)
            return;

        _isDead = true;
        Cause = cause;

        if (cause == DeathCause.Fell)
        {
            _falling = true;
            _verticalVelocity = 0f;
        }
        else
        {
            _currentSpeed = 0f;
        }

        Shake(cause == DeathCause.Fell ? 0.15f : 0.7f, 0.6f);

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver(cause);
    }
}
