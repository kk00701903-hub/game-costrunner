using UnityEngine;

/// Keeps HUD inside punch-hole + gesture insets on device; uses S26 design insets in editor.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafe;
    private int _lastW;
    private int _lastH;

    private void Awake()
    {
        _rt = (RectTransform)transform;
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        Rect sa = Screen.safeArea;
        if (sa == _lastSafe && Screen.width == _lastW && Screen.height == _lastH)
            return;

        _lastSafe = sa;
        _lastW = Screen.width;
        _lastH = Screen.height;
        Apply();
    }

    private void Apply()
    {
        _lastSafe = Screen.safeArea;
        MobileDisplay.ApplySafeArea(_rt);
    }
}
