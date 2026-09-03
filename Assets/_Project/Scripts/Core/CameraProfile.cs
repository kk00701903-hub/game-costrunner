using UnityEngine;

[CreateAssetMenu(menuName = "347/CameraProfile", fileName = "CameraProfile")]
public class CameraProfile : ScriptableObject
{
    public const string ResourcePath = "347/CameraProfile";

    public float baseFov = 54f;
    public float maxFov = 62f;
    public float fovLerpSpeed = 2.5f;
    public Vector3 offset = new Vector3(0f, 5.8f, -11.5f);
    public float bossOffsetZ = -9f;
    public float axisFlipDuration = 0.6f;
    public float hitShakeAmp = 0.35f;
    public float hitShakeDur = 0.25f;
    [Range(0f, 1f)] public float shakeUserScale = 1f;
    public float yawDamping = 0.15f;
    public float xDamping = 0.18f;
    public float fallPitch = 34f;
    public float nearClip = 0.1f;
    public float farClip = 800f;

    private static CameraProfile _cached;

    public static CameraProfile Active
    {
        get
        {
            if (_cached != null)
                return _cached;

            GameConfig cfg = GameConfig.Active;
            if (cfg != null && cfg.cameraProfile != null)
            {
                _cached = cfg.cameraProfile;
                return _cached;
            }

            _cached = Resources.Load<CameraProfile>(ResourcePath);
            if (_cached == null)
            {
                _cached = CreateInstance<CameraProfile>();
                _cached.name = "CameraProfile (runtime)";
            }

            return _cached;
        }
    }

    public static void Override(CameraProfile profile)
    {
        _cached = profile;
    }
}
