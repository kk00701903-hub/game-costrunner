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
        /// 배치 코드의 단위(720×1280) → 캔버스 기준(1080×1920) 배율
        public const float DesignScale = 1.5f;

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
            go.AddComponent<CoastRaycastWatchdog>();
#if UNITY_EDITOR
            go.AddComponent<CoastDebugClicker>();
#endif
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
            // 18차-3: 기준 해상도 1080×1920(FHD 9:16), Match 0.5 — 16:9~22:9 대응 표준 설정.
            // 화면 배치 코드는 720×1280 단위로 쓰여 있으므로 HudInset을 1.5배로 두어 그대로 맞춘다(DesignScale).
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
            inset.anchorMin = inset.anchorMax = new Vector2(0.5f, 0.5f);
            inset.pivot = new Vector2(0.5f, 0.5f);
            inset.anchoredPosition = Vector2.zero;
            inset.localScale = new Vector3(DesignScale, DesignScale, 1f);
            inset.sizeDelta = new Vector2(720f - 2f * HudPad, 1280f - 2f * HudPad);   // 실제 크기는 CoastPortraitSafeArea가 매 프레임 갱신

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
        private RectTransform _safe, _inset;

        private void Awake()
        {
            var t = transform.Find(CoastUiCanvas.SafeAreaName);
            _safe = t as RectTransform;
            Apply();   // 첫 프레임 배치 코드가 실제 크기를 보게 즉시 한 번
        }

        private void LateUpdate() => Apply();

        public void Apply()
        {
            if (_safe == null)
                return;

            var cam = Camera.main;
            Rect r = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            // 12차: 기기 안전 영역(펀치홀·노치·제스처 바)과 교집합. 전엔 카메라 사각형만 써서
            // 좌상단 하트·우상단 알약이 실기기에서 반쯤 잘렸다.
            Rect sa = Screen.safeArea;
            float x0 = Mathf.Max(r.xMin, sa.xMin), y0 = Mathf.Max(r.yMin, sa.yMin);
            float x1 = Mathf.Min(r.xMax, sa.xMax), y1 = Mathf.Min(r.yMax, sa.yMax);
            if (x1 - x0 > 8f && y1 - y0 > 8f) r = Rect.MinMaxRect(x0, y0, x1, y1);
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            _safe.anchorMin = new Vector2(r.x / w, r.y / h);
            _safe.anchorMax = new Vector2((r.x + r.width) / w, (r.y + r.height) / h);
            _safe.offsetMin = Vector2.zero;
            _safe.offsetMax = Vector2.zero;
            // HudInset: 안전 영역 크기를 디자인 단위(÷1.5)로 환산하고 안쪽 여백(HudPad)을 뺀다
            if (_inset == null) _inset = _safe.Find(CoastUiCanvas.InsetName) as RectTransform;
            if (_inset != null)
            {
                // CanvasScaler(Match 0.5)와 같은 식으로 배율을 직접 계산 — 첫 프레임에도 정확하다
                float lw = Mathf.Log(w / 1080f, 2f), lh = Mathf.Log(h / 1920f, 2f);
                float scale = Mathf.Pow(2f, Mathf.Lerp(lw, lh, 0.5f));
                var sz = new Vector2(r.width, r.height) / scale / CoastUiCanvas.DesignScale;
                _inset.sizeDelta = new Vector2(Mathf.Max(100f, sz.x - 2f * CoastUiCanvas.HudPad), Mathf.Max(100f, sz.y - 2f * CoastUiCanvas.HudPad));
            }
        }
    }

    /// 18차-5: 입력 감시견 — "버튼이 눌리다가 어느 순간부터 안 눌림"의 전형적 원인은
    /// 보이지 않는데 레이캐스트만 막는 UI(알파 0인 CanvasGroup/Image가 화면을 덮음)다.
    /// 1초마다 훑어서 그런 것을 풀고 한 번만 로그를 남긴다. 버튼(Selectable)이 달린 투명 이미지는 의도된 것이라 건드리지 않는다.
    public class CoastRaycastWatchdog : MonoBehaviour
    {
        private float _next;
        private readonly System.Collections.Generic.HashSet<GameObject> _logged = new();

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            foreach (var cg in FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None))
            {
                if (!cg.blocksRaycasts || cg.alpha > 0.02f || !cg.gameObject.activeInHierarchy) continue;
                if (cg.GetComponentInParent<Selectable>() != null) continue;
                if (!CoversScreen(cg.transform as RectTransform)) continue;
                cg.blocksRaycasts = false;
                Log(cg.gameObject, "CanvasGroup alpha≈0");
            }
            foreach (var img in FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!img.raycastTarget || img.color.a > 0.02f || !img.gameObject.activeInHierarchy) continue;
                if (img.GetComponentInParent<Selectable>() != null) continue;
                if (!CoversScreen(img.rectTransform)) continue;
                img.raycastTarget = false;
                Log(img.gameObject, "Image alpha≈0");
            }
        }

        private static readonly Vector3[] _c = new Vector3[4];
        private static bool CoversScreen(RectTransform rt)
        {
            if (rt == null) return false;
            rt.GetWorldCorners(_c);
            float w = _c[2].x - _c[0].x, h = _c[2].y - _c[0].y;
            return w >= Screen.width * 0.6f && h >= Screen.height * 0.6f;
        }

        private void Log(GameObject go, string why)
        {
            if (_logged.Contains(go)) return;
            _logged.Add(go);
            Debug.LogWarning($"[RaycastWatchdog] 입력을 막던 투명 UI를 풀었다: {go.name} ({why}) 부모={go.transform.parent?.name}");
        }
    }

#if UNITY_EDITOR
    /// 18차 에디터 검증용: K = 마우스 아래 UI 요소에 클릭 이벤트를 직접 보낸다(원격 제어에서 왼쪽 클릭이 안 들어올 때).
    public class CoastDebugClicker : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J)) Dump();
            if (!Input.GetKeyDown(KeyCode.K)) return;
            var es = EventSystem.current; if (es == null) return;
            var pd = new PointerEventData(es) { position = Input.mousePosition, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(pd, hits);
            foreach (var h in hits)
            {
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject);
                if (target == null) continue;
                pd.pointerPress = target; pd.pointerPressRaycast = h; pd.pointerCurrentRaycast = h;
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerClickHandler);
                Debug.Log("[DebugClick] " + target.name);
                return;
            }
            Debug.Log("[DebugClick] no handler under " + Input.mousePosition);
        }

        /// J = 레이캐스트 진단 덤프(Tools/ui_debug.txt): 캔버스별 Raycast 결과·설정, 마우스/터치 상태
        private void Dump()
        {
            var sb = new System.Text.StringBuilder();
            var es = EventSystem.current;
            sb.Append($"t={Time.unscaledTime:0.0} ts={Time.timeScale} mouse={Input.mousePosition} screen={Screen.width}x{Screen.height} es={(es ? es.name : "null")} module={(es && es.currentInputModule ? es.currentInputModule.GetType().Name : "null")} enabled={(es ? es.enabled.ToString() : "-")} sel={(es && es.currentSelectedGameObject ? es.currentSelectedGameObject.name : "-")}\n");
            sb.Append($"mouseBtn0={Input.GetMouseButton(0)} touches={Input.touchCount} simulateMouse={Input.simulateMouseWithTouches} cursorLock={Cursor.lockState} visible={Cursor.visible}\n");
            var pd = new PointerEventData(es) { position = Input.mousePosition };
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var gr = c.GetComponent<GraphicRaycaster>();
                var hits = new System.Collections.Generic.List<RaycastResult>();
                if (gr != null && gr.isActiveAndEnabled) gr.Raycast(pd, hits);
                sb.Append($"  canvas {c.name} order={c.sortingOrder} root={c.isRootCanvas} enabled={c.enabled} active={c.gameObject.activeInHierarchy} mode={c.renderMode} cam={(c.worldCamera ? c.worldCamera.name : "-")} scale={c.scaleFactor:0.00} rect={c.GetComponent<RectTransform>().rect.size} gr={(gr ? gr.isActiveAndEnabled.ToString() : "none")} hits={hits.Count}");
                foreach (var h in hits) sb.Append(" [" + h.gameObject.name + "]");
                var cgs = c.GetComponentsInChildren<CanvasGroup>(true);
                foreach (var g in cgs) if (g.blocksRaycasts && g.alpha < 0.05f && g.gameObject.activeInHierarchy) sb.Append($" BLOCKER?{g.name}(a={g.alpha:0.00})");
                sb.Append("\n");
            }
            foreach (var e in FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append($"  eventsystem {e.name} enabled={e.enabled} active={e.gameObject.activeInHierarchy}\n");
            Debug.Log("[UIDump]\n" + sb);
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/ui_debug.txt"), sb + "\n"); } catch { }
        }
    }
#endif
}
