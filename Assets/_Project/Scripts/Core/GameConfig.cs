using UnityEngine;

/// Single source of truth for combat and movement numbers. Balancing happens
/// here — never by editing player or king scripts.
[CreateAssetMenu(menuName = "347/GameConfig", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    public const string ResourcePath = "347/GameConfig";

    [Header("체력")]
    public int maxHp = 3;
    public float invulnDuration = 1.2f;
    public float hurtSlowFactor = 0.70f;
    public float hurtRecoverTime = 1.5f;
    public float reviveInvulnDuration = 3f;

    [Header("속도")]
    public float baseSpeed = 9f;
    public float maxSpeed = 20f;
    public float speedPerMeter = 0.0035f;
    public float bossFixedSpeed = 14f;
    [Tooltip("M1 completion target: lane change finishes within this.")]
    public float laneChangeSeconds = 0.18f;
    public float laneOffset = 2.5f;

    [Header("점프 / 슬라이드")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float slideDuration = 0.7f;
    public float slideHeight = 0.5f;
    public float standHeight = 1.7f;
    public float standRadius = 0.35f;
    public float slideRadius = 0.22f;

    [Header("붕괴선")]
    public float collapseBaseGap = 45f;
    public float collapseSpeedRatio = 0.96f;
    public float collapseOnHit = -12f;
    public float collapseOnCounter = 6f;
    public float collapseWarnGap = 15f;
    public float collapseKillGap = 0.5f;

    [Header("입력")]
    public float inputBufferSec = 0.12f;
    public float coyoteTimeSec = 0.10f;
    public float swipeThresholdPx = 50f;
    public float turnGraceMetres = 3f;

    [Header("전투")]
    [Tooltip("절대 하한. 위반 시 Assert.")]
    public float minTelegraphSec = 0.45f;
    public float counterHitStopSec = 0.15f;

    [Header("비주얼")]
    [Tooltip("On: PlayerCharacterView (3D). Off: PlayerSpriteView (2D sprite — Temple Run read).")]
    public bool use3DCharacter = false;

    [Header("카메라")]
    public CameraProfile cameraProfile;

    [Header("왕 페이즈")]
    public KingPhaseData[] kingPhases;

    private static GameConfig _cached;

    /// Loads from Resources, or builds an in-memory default so Play Mode never
    /// hard-depends on a baked asset existing yet.
    public static GameConfig Active
    {
        get
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<GameConfig>(ResourcePath);
            if (_cached == null)
            {
                _cached = CreateInstance<GameConfig>();
                _cached.name = "GameConfig (runtime)";
            }

            return _cached;
        }
    }

    public static void Override(GameConfig config)
    {
        _cached = config;
    }

    private void OnValidate()
    {
        maxHp = Mathf.Clamp(maxHp, 1, 3);
        minTelegraphSec = Mathf.Max(0.45f, minTelegraphSec);
        laneChangeSeconds = Mathf.Max(0.05f, laneChangeSeconds);
        collapseSpeedRatio = Mathf.Clamp(collapseSpeedRatio, 0.5f, 1.2f);
    }

    /// Distance that must be spawned ahead so a telegraph of minTelegraphSec
    /// still has wall-clock time at the current speed. Frame drops inflate this.
    public float TelegraphLeadMetres(float speed, float measuredDt)
    {
        float safeSpeed = Mathf.Max(1f, speed);
        float frameFactor = measuredDt > 0f ? Mathf.Clamp(measuredDt / (1f / 60f), 1f, 3f) : 1f;
        return minTelegraphSec * safeSpeed * frameFactor;
    }
}
