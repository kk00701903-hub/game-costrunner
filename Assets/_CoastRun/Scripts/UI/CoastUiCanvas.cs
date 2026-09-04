using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// Overlay HUD clipped to the portrait camera, with inner padding so labels aren't cropped.
    public static class CoastUiCanvas
    {
        public const string SafeAreaName = "PortraitSafeArea";
        public const string InsetName = "HudInset";
        public const float HudPad = 28f;

        /// Every scene in the flow is an empty shell — the world, the canvases and the
        /// buttons are all built at runtime. Nothing was building the one object Unity UI
        /// needs to deliver a click: an EventSystem. Each canvas got a GraphicRaycaster,
        /// which finds the button under the finger, but with no EventSystem there was
        /// nobody to ask. START sat on screen and ignored every tap.
        ///
        /// One persistent EventSystem is enough for the whole app; it survives scene loads.
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static Canvas Create(string name, int sortingOrder, Transform parent = null)
        {
            EnsureEventSystem();

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var safeGo = new GameObject(SafeAreaName, typeof(RectTransform));
            safeGo.transform.SetParent(canvas.transform, false);
            var safe = safeGo.GetComponent<RectTransform>();
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;

            var insetGo = new GameObject(InsetName, typeof(RectTransform));
            insetGo.transform.SetParent(safe, false);
            var inset = insetGo.GetComponent<RectTransform>();
            inset.anchorMin = Vector2.zero;
            inset.anchorMax = Vector2.one;
            inset.offsetMin = new Vector2(HudPad, HudPad);
            inset.offsetMax = new Vector2(-HudPad, -HudPad);

            if (go.GetComponent<CoastPortraitSafeArea>() == null)
                go.AddComponent<CoastPortraitSafeArea>();

            return canvas;
        }

        public static RectTransform Root(Canvas canvas)
        {
            if (canvas == null)
                return null;
            var safe = canvas.transform.Find(SafeAreaName);
            if (safe != null)
            {
                var inset = safe.Find(InsetName) as RectTransform;
                if (inset != null)
                    return inset;
                return safe as RectTransform;
            }

            return canvas.GetComponent<RectTransform>();
        }
    }

    public class CoastPortraitSafeArea : MonoBehaviour
    {
        private RectTransform _safe;

        private void Awake()
        {
            var t = transform.Find(CoastUiCanvas.SafeAreaName);
            _safe = t as RectTransform;
        }

        private void LateUpdate()
        {
            if (_safe == null)
                return;

            var cam = Camera.main;
            Rect r = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            _safe.anchorMin = new Vector2(r.x / w, r.y / h);
            _safe.anchorMax = new Vector2((r.x + r.width) / w, (r.y + r.height) / h);
            _safe.offsetMin = Vector2.zero;
            _safe.offsetMax = Vector2.zero;
        }
    }
}
