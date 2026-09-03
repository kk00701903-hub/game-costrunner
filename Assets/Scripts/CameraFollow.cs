using UnityEngine;

/// Temple Run-style chase camera: far back, slight downward tilt, smooth corner swing.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5.8f, -11.5f);
    [SerializeField] private float lookAhead = 7f;
    [SerializeField] private float lookHeight = 1.15f;
    [SerializeField] private float xDamping = 0.22f;
    [SerializeField] private float yawDamping = 0.18f;
    [SerializeField] private float pitchDown = 8f;
    [SerializeField] private float bobAmplitude = 0.07f;
    [SerializeField] private float bobFrequency = 7.5f;

    [Header("Speed Framing")]
    [SerializeField] private float baseFov = 54f;
    [SerializeField] private float fastFov = 62f;

    [Header("Fall")]
    [SerializeField] private float fallPitch = 28f;

    private float _xVelocity;
    private float _yawVelocity;
    private float _yaw;
    private float _camLateral;
    private float _pitch;
    private float _shake;
    private float _shakeRate = 1f;
    private float _bobPhase;
    private PlayerController _player;
    private Camera _camera;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        _player = followTarget != null ? followTarget.GetComponent<PlayerController>() : null;
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyProfile(CameraProfile.Active);
    }

    private void ApplyProfile(CameraProfile profile)
    {
        if (profile == null)
            return;

        offset = profile.offset;
        xDamping = profile.xDamping;
        yawDamping = profile.yawDamping;
        baseFov = profile.baseFov;
        fastFov = profile.maxFov;
        fallPitch = profile.fallPitch;

        if (_camera != null)
            _camera.fieldOfView = baseFov;
    }

    public void Shake(float strength, float seconds)
    {
        CameraProfile profile = CameraProfile.Active;
        float scale = profile != null ? profile.shakeUserScale : 1f;
        _shake = Mathf.Max(_shake, strength * scale);
        _shakeRate = _shake / Mathf.Max(0.05f, seconds);
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                SetTarget(player.transform);
        }
        else if (_player == null)
        {
            _player = target.GetComponent<PlayerController>();
        }

        _yaw = _player != null ? _player.Yaw : 0f;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float targetYaw = _player != null ? _player.Yaw : 0f;
        _yaw = Mathf.SmoothDampAngle(_yaw, targetYaw, ref _yawVelocity, yawDamping);
        Quaternion frame = Quaternion.Euler(0f, _yaw, 0f);

        float lateral = _player != null ? _player.LateralOffset : 0f;
        _camLateral = Mathf.SmoothDamp(_camLateral, lateral, ref _xVelocity, xDamping);

        Quaternion playerFrame = Quaternion.Euler(0f, _player != null ? _player.Yaw : 0f, 0f);
        Vector3 centre = target.position - playerFrame * new Vector3(lateral, 0f, 0f);
        Vector3 pivot = centre + frame * new Vector3(_camLateral, 0f, 0f);

        _shake = Mathf.MoveTowards(_shake, 0f, _shakeRate * Time.deltaTime);
        Vector3 jitter = _shake > 0f ? Random.insideUnitSphere * _shake : Vector3.zero;

        Vector3 useOffset = offset;
        if (KingFight.Instance != null && KingFight.Instance.Active)
            useOffset.z = CameraProfile.Active != null ? CameraProfile.Active.bossOffsetZ : offset.z;

        transform.position = pivot + frame * useOffset + jitter;

        if (_player != null && !_player.IsDead && _player.IsGrounded)
        {
            float speed = Mathf.Clamp01(_player.NormalizedSpeed);
            _bobPhase += Time.deltaTime * Mathf.Lerp(bobFrequency * 0.45f, bobFrequency, speed);
            transform.position += Vector3.up * (Mathf.Sin(_bobPhase) * bobAmplitude * speed);
        }

        bool falling = _player != null && _player.IsFalling;
        _pitch = Mathf.Lerp(_pitch, falling ? fallPitch : pitchDown, Time.deltaTime * 3.5f);

        Vector3 aim = pivot + frame * new Vector3(0f, lookHeight, lookAhead);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.LookAt(aim);

        UpdateFov();
    }

    private void UpdateFov()
    {
        if (_camera == null)
            return;

        float t = _player != null ? Mathf.Clamp01(_player.NormalizedSpeed) : 0f;
        float want = Mathf.Lerp(baseFov, fastFov, t);
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, want, Time.deltaTime * 2f);
    }
}
