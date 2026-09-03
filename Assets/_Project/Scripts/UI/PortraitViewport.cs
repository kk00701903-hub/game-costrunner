using UnityEngine;

/// Locks gameplay + HUD to Galaxy S26 portrait (9:19.5) with pillarbox/letterbox on wide screens.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Camera))]
public class PortraitViewport : MonoBehaviour
{
    public static Rect NormalizedRect { get; private set; } = new Rect(0f, 0f, 1f, 1f);

    private static PortraitViewport _instance;
    private Camera _camera;
    private Camera _letterboxCamera;
    private int _lastW;
    private int _lastH;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LockPortraitOrientation()
    {
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;

#if !UNITY_EDITOR
        Screen.orientation = ScreenOrientation.Portrait;
#endif
    }

    public static PortraitViewport Ensure(Camera gameplayCamera)
    {
        if (gameplayCamera == null)
            return null;

        PortraitViewport vp = gameplayCamera.GetComponent<PortraitViewport>();
        if (vp == null)
            vp = gameplayCamera.gameObject.AddComponent<PortraitViewport>();

        vp.ApplyNow();
        return vp;
    }

    public static Rect ComputeNormalizedRect(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
            return new Rect(0f, 0f, 1f, 1f);

        float targetAspect = MobileDisplay.AspectWidth / MobileDisplay.AspectHeight;
        float windowAspect = screenWidth / (float)screenHeight;

        if (windowAspect > targetAspect + 0.001f)
        {
            float width = targetAspect / windowAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        if (windowAspect < targetAspect - 0.001f)
        {
            float height = windowAspect / targetAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        return new Rect(0f, 0f, 1f, 1f);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _instance = this;
        ApplyNow();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (Screen.width == _lastW && Screen.height == _lastH)
            return;

        ApplyNow();
    }

    private void ApplyNow()
    {
        _lastW = Screen.width;
        _lastH = Screen.height;
        NormalizedRect = ComputeNormalizedRect(_lastW, _lastH);

        if (_camera != null)
            _camera.rect = NormalizedRect;

        EnsureLetterboxCamera();
    }

    private void EnsureLetterboxCamera()
    {
        if (_letterboxCamera == null)
        {
            GameObject go = GameObject.Find("LetterboxCamera");
            if (go == null)
            {
                go = new GameObject("LetterboxCamera");
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            _letterboxCamera = go.GetComponent<Camera>();
            if (_letterboxCamera == null)
                _letterboxCamera = go.AddComponent<Camera>();

            _letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            _letterboxCamera.backgroundColor = Color.black;
            _letterboxCamera.cullingMask = 0;
            _letterboxCamera.depth = -100f;
            _letterboxCamera.orthographic = true;
        }

        _letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
        _letterboxCamera.enabled = NormalizedRect.width < 0.999f || NormalizedRect.height < 0.999f;
    }
}
