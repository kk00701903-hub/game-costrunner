using UnityEngine;
using UnityEngine.UI;

/// Full-screen black pillarbox bars so wide Game View still reads as mobile portrait.
public class PortraitBarsOverlay : MonoBehaviour
{
    private static PortraitBarsOverlay _instance;
    private RectTransform _left;
    private RectTransform _right;
    private RectTransform _top;
    private RectTransform _bottom;

    public static void Ensure()
    {
        if (_instance != null)
        {
            _instance.Refresh();
            return;
        }

        GameObject root = new GameObject("PortraitBars");
        _instance = root.AddComponent<PortraitBarsOverlay>();
        _instance.Build();
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1000;
        canvas.pixelPerfect = false;
        gameObject.AddComponent<GraphicRaycaster>().enabled = false;

        _left = CreateBar("BarLeft");
        _right = CreateBar("BarRight");
        _top = CreateBar("BarTop");
        _bottom = CreateBar("BarBottom");
        Refresh();
    }

    private RectTransform CreateBar(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        Rect n = PortraitViewport.NormalizedRect;
        StretchLeft(_left, 0f, n.xMin);
        StretchLeft(_right, n.xMax, 1f);
        StretchTop(_top, n.yMax, 1f);
        StretchTop(_bottom, 0f, n.yMin);
    }

    private static void StretchLeft(RectTransform rt, float xMin, float xMax)
    {
        if (rt == null)
            return;
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchTop(RectTransform rt, float yMin, float yMax)
    {
        if (rt == null)
            return;
        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
