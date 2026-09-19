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
        public const string BgName = "BgFullBleed";
        public const float HudPad = 28f;
        /// 배치 코드의 단위(720×1280) → 시안 픽셀(1080×1920) 배율. 99차부터는 **픽셀 환산에만** 쓰고
        /// (MockPixelRectToBgAnchors 등), 캔버스 트리에는 더 이상 이 배율을 localScale 로 걸지 않는다.
        public const float DesignScale = 1.5f;

        // ── 99차(사용자: 「20:9 로 바꾸니 전체가 또렷하지 않고 글자가 아른거린다」) ──────────────
        // 원인: CanvasScaler 기준을 1080×1920 으로 두고 HudInset 을 localScale 1.5 로 키워 720×1280 좌표계를
        //   맞췄다. uGUI Text 는 캔버스 배율(scaleFactor)만 보고 글자를 굽고 RectTransform 의 스케일은
        //   모르기 때문에, 1080 폭 폰에서는 배율 1.0 으로 구운 글자를 1.5배 늘려 그렸다 → 모든 글자가
        //   흐릿하고, 움직이면 텍셀 사이를 보간하며 아른거렸다(79차 전엔 match 0.5 라 1.1배로 구워 조금
        //   덜했을 뿐 같은 문제). 아이콘·그림도 같은 이유로 1.5배 확대돼 있었다.
        // 해결: 기준 해상도를 배치 단위 그대로 **720×1280** 으로 두고 인셋 스케일을 1(×fit)로 — 캔버스
        //   단위 = 디자인 단위이므로 배치 코드는 한 줄도 안 바뀌고, scaleFactor 가 1.5(1080 폭)·2.0(1440 폭)
        //   이 되어 글자가 실제 픽셀 크기로 구워진다. 화면에서 차지하는 크기는 전과 완전히 같다.
        /// CanvasScaler 기준 해상도(= 배치 단위, 720×1280).
        public const float RefWidth = 720f;
        public const float RefHeight = 1280f;

        // ── 79차(사용자) 캔버스 전략 ───────────────────────────────────────────────
        // 제작 기준   1080×2400 (20:9) — 배경은 이 크기로 길게 그려 두고 화면을 덮는다.
        // S25 대응    1080×2340 — 제작 기준보다 60px 짧으니 위아래 30px씩만 잘린다.
        // 세이프존    1080×1920 (16:9) 중앙 — 스토리 모드·더보기·Play 는 반드시 이 안.
        /// 배경 제작 기준(디자인 단위). 1080×2400 ÷ DesignScale.
        public const float BgDesignWidth = 720f;
        public const float BgDesignHeight = 1600f;
        /// 필수 UI 세이프존(디자인 단위). 1080×1920 ÷ DesignScale.
        public const float SafeZoneWidth = 720f;
        public const float SafeZoneHeight = 1280f;
        /// CanvasScaler match — 0 = 가로(1080) 고정. 세로 여유는 UI 축소가 아니라
        /// 배경 노출로 흡수한다(전엔 0.5여서 S25에서 UI가 10% 쪼그라들었다).
        public const float ScalerMatch = 0f;
        /// 배경 안에서 세이프존이 시작되는 높이 — 제작 기준 대비 위아래 여유(=160 디자인 = 240px).
        public static float BgSafeZoneMarginY => (BgDesignHeight - SafeZoneHeight) * 0.5f;

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
            // 18차-3: 기준 해상도 1080×1920(FHD 9:16). 화면 배치 코드는 720×1280 단위로 쓰여 있으므로
            //   HudInset을 1.5배로 두어 그대로 맞춘다(DesignScale).
            // 79차(사용자 캔버스 전략): Match 0.5 → 0(가로 기준). 0.5 에서는 세로가 길어질수록 배율이
            //   올라가 UI 가 통째로 작아졌다(S25에서 0.898). 가로를 1080 으로 고정하면 세이프존(720×1280)이
            //   19.5:9~20:9 에서 언제나 원래 크기로 들어가고, 남는 세로는 배경이 채운다.
            // 99차: 기준 720×1280(배치 단위) — 글자·그림이 실제 픽셀 밀도로 구워지도록. 위 주석 참조.
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = ScalerMatch;

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
            inset.localScale = Vector3.one;   // 99차: 캔버스 단위 = 디자인 단위 (fit 축소만 CoastPortraitSafeArea 가 건다)
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

        /// 러닝 HUD 배치 기준 폭(인셋 664 = 720−2×HudPad). 갤럭시 S25U 등 좁은 세로폰에선
        /// 인셋이 ~596으로 줄어 상단 알약이 겹친다 → HudFit 으로 664 기준 배치를 통째로 축소.
        public const float HudDesignWidth = 664f;
        /// 같은 기준의 높이(1280−2×HudPad). 9:16보다 **짧은** 비율(16:9 게임뷰·태블릿·폴더블 펼침)에서
        /// 절대 좌표 배치가 위아래로 잘리던 것을 막기 위해 세로도 이 값을 기준으로 축소한다.
        public const float HudDesignHeight = 1224f;
        /// 축소 하한 — 여기까지는 잘리지 않고 줄어든다. 79차: Match 0 으로 바꾸면서 가로 화면
        /// (1920×1080 에디터 게임뷰)에 필요한 배율이 0.285 까지 내려가므로 하한도 함께 내렸다.
        /// 세로 기기는 0.55 아래로 내려가는 경우가 없다.
        public const float MinFitScale = 0.28f;

        /// 절대 좌표(664×1224) 배치를 화면에 맞춰 통째로 축소하는 상자. 배경처럼 꽉 차야 하는 것은
        /// 이 상자 **바깥**(인셋)에 먼저 붙이고, 좌표로 놓는 UI만 이 상자의 자식으로 둔다.
        public static RectTransform MakeFitBox(RectTransform parent, string name = "Fit")
        {
            if (parent == null) return null;
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform), typeof(CoastUiDesignFit));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<CoastUiDesignFit>().ApplyNow();
            return rt;
        }

        /// 부모 인셋 크기로 664×1224 기준 배율을 구한다(가로·세로 중 더 빡빡한 쪽, 1 초과 없음).
        public static float FitScale(float insetW, float insetH)
        {
            if (insetW < 8f || insetH < 8f) return 1f;
            float byW = insetW / HudDesignWidth;
            float byH = insetH / HudDesignHeight;
            return Mathf.Clamp(Mathf.Min(byW, byH), MinFitScale, 1f);
        }

        /// 해상도(+안전 영역) → HudInset 의 디자인 단위 크기와 축소 배율. 런타임과 에디터 점검 메뉴가
        /// 같은 식을 쓰게 한 곳에 모아 둔다. fit==1 이면 기준(664×1224)이 그대로 들어간다는 뜻이고,
        /// fit 이 MinFitScale 에 걸린 경우(=반환 크기가 664×1224보다 작다)에만 잘림이 생긴다.
        public static void DesignMetrics(float screenW, float screenH, float safeW, float safeH,
                                         out Vector2 insetSize, out float fit)
        {
            // 99차: CanvasScaler(720×1280 기준)와 같은 식 — scale 은 캔버스 scaleFactor(1080 폭이면 1.5).
            float lw = Mathf.Log(Mathf.Max(1f, screenW) / RefWidth, 2f);
            float lh = Mathf.Log(Mathf.Max(1f, screenH) / RefHeight, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(lw, lh, ScalerMatch));
            var sz = new Vector2(safeW, safeH) / scale;
            float iw = Mathf.Max(100f, sz.x - 2f * HudPad);
            float ih = Mathf.Max(100f, sz.y - 2f * HudPad);
            fit = FitScale(iw, ih);
            insetSize = new Vector2(iw / fit, ih / fit);
        }

        /// 러닝 크롬·여정 바가 같은 Fit 자식을 쓰도록. 폭이 충분하면 scale=1(에디터 720×1280 동일).
        public static RectTransform HudFitRoot(Canvas canvas)
        {
            var inset = Root(canvas);
            if (inset == null) return null;
            var existing = inset.Find("HudFit") as RectTransform;
            if (existing != null) return existing;
            var go = new GameObject("HudFit", typeof(RectTransform), typeof(CoastHudNarrowFit));
            go.transform.SetParent(inset, false);
            var frt = go.GetComponent<RectTransform>();
            // 위쪽 고정 — 축소해도 노치 아래 상단 바가 아래로 밀리지 않음
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 1f);
            frt.pivot = new Vector2(0.5f, 1f);
            frt.anchoredPosition = Vector2.zero;
            go.GetComponent<CoastHudNarrowFit>().ApplyNow();
            return frt;
        }

        /// 79차: 배경 자리 — **안전 영역이 아니라 캔버스 전체**를 덮는 상자(노치 아래까지). 디자인 단위로
        /// 캔버스 크기를 그대로 들고 있어, 자식 배경을 제작 기준(720×1600)으로 놓으면 남는 쪽이 잘린다.
        public static RectTransform BgRoot(Canvas canvas)
        {
            if (canvas == null) return null;
            var crt = canvas.GetComponent<RectTransform>();
            var existing = crt.Find(BgName) as RectTransform;
            if (existing != null) return existing;
            var go = new GameObject(BgName, typeof(RectTransform), typeof(CoastUiCanvasBox));
            go.transform.SetParent(crt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.SetAsFirstSibling();   // 배경은 늘 맨 뒤
            go.GetComponent<CoastUiCanvasBox>().ApplyNow();
            return rt;
        }

        /// 제작 기준(1080×2400)으로 그린 배경을 화면에 깐다. 20:9 에서 딱 맞고, S25(19.5:9)에선
        /// 위아래 30px 씩 잘리고, 16:9 에선 위아래 240px 씩 잘려 옛 16:9 구도가 그대로 보인다.
        /// 16:9 보다 **짧은** 화면(태블릿·가로)에서는 세이프존이 다 보이도록 배경을 축소하고,
        /// 그때 생기는 좌우 여백은 같은 그림을 늘려 깐 판으로 메운다.
        /// 반환값은 제작 기준 좌표계를 가진 배경 이미지 — 그림에 그려진 버튼의 히트 영역을
        /// 이 이미지의 자식으로 두면 어떤 비율에서도 그림과 어긋나지 않는다.
        public static Image FullBleedBackground(Canvas canvas, string name, Sprite sprite)
        {
            var root = BgRoot(canvas);
            if (root == null) return null;
            var fillGo = new GameObject(name + "Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(root, false);
            var fill = fillGo.GetComponent<Image>();
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fill.sprite = sprite; fill.preserveAspect = false; fill.raycastTarget = false;
            fill.color = new Color(0.72f, 0.72f, 0.72f, 1f);   // 여백 채움은 살짝 어둡게 — 본 그림과 구분

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CoastUiBgCover));
            go.transform.SetParent(root, false);
            var img = go.GetComponent<Image>();
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BgDesignWidth, BgDesignHeight);
            img.sprite = sprite; img.preserveAspect = false; img.raycastTarget = false;
            go.GetComponent<CoastUiBgCover>().ApplyNow();
            return img;
        }

        /// 79차: 필수 UI 세이프존 — 언제나 화면 안에 다 들어가는 중앙 16:9 상자(720×1280 좌표계).
        /// 스토리 모드·더보기·Play 처럼 못 누르면 게임이 막히는 버튼은 이 상자의 자식으로 둔다.
        public static RectTransform SafeZoneRoot(Canvas canvas, string name = "SafeZone")
            => SafeZoneBox(Root(canvas), name);

        /// 같은 세이프존 상자를 임의의 부모(예: 타이틀 UI) 안에 만든다.
        public static RectTransform SafeZoneBox(RectTransform inset, string name = "SafeZone")
        {
            if (inset == null) return null;
            var existing = inset.Find(name) as RectTransform;
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform), typeof(CoastUiSafeZoneFit));
            go.transform.SetParent(inset, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(HudDesignWidth, HudDesignHeight);
            go.GetComponent<CoastUiSafeZoneFit>().ApplyNow();
            return rt;
        }

        /// 시안 좌표(720×1280, 좌하단 원점)로 찍은 사각형을 제작 기준 배경(720×1600) 안의
        /// 앵커 비율로 바꾼다 — 그림에 그려진 버튼 위에 히트 영역을 얹을 때 쓴다.
        public static void SafeZoneRectToBgAnchors(Vector2 center, Vector2 size, out Vector2 aMin, out Vector2 aMax)
        {
            float y0 = BgSafeZoneMarginY + center.y - size.y * 0.5f;
            float y1 = BgSafeZoneMarginY + center.y + size.y * 0.5f;
            aMin = new Vector2((center.x - size.x * 0.5f) / BgDesignWidth, y0 / BgDesignHeight);
            aMax = new Vector2((center.x + size.x * 0.5f) / BgDesignWidth, y1 / BgDesignHeight);
        }

        /// 시안 그림 픽셀(1080×1920, 좌상단 원점)로 찍은 사각형 → 제작 기준 배경(1080×2400) 앵커 비율.
        /// 그림에 박힌 요소 위에 칩·버튼을 얹을 때. 세로만 위아래 240px 만큼 밀린다.
        public static void MockPixelRectToBgAnchors(float left, float top, float right, float bottom,
                                                    out Vector2 aMin, out Vector2 aMax)
        {
            float wpx = SafeZoneWidth * DesignScale;        // 1080
            float hpx = BgDesignHeight * DesignScale;       // 2400
            float marginPx = BgSafeZoneMarginY * DesignScale;   // 240
            aMin = new Vector2(left / wpx, 1f - (marginPx + bottom) / hpx);
            aMax = new Vector2(right / wpx, 1f - (marginPx + top) / hpx);
        }

        /// 화면 → 배경 배율·상하 잘림(px)·세이프존이 다 보이는지. 에디터 점검 메뉴와 식을 공유한다.
        ///   bgScale       : 제작 기준(720×1600) 배경에 걸리는 배율(1 = 원본 크기로 가로 꽉)
        ///   bgCropPerEdge : 배경이 위아래로 잘리는 양(px, 한쪽). 20:9 = 0, S25(19.5:9) ≈ 30, 16:9 = 360
        ///   safeZoneFits  : 중앙 16:9(1080×1920) 안의 그림·UI 가 한 점도 안 잘리는지
        public static void SafeZoneMetrics(float screenW, float screenH,
                                           out float bgScale, out float bgCropPerEdge, out bool safeZoneFits)
        {
            float lw = Mathf.Log(Mathf.Max(1f, screenW) / RefWidth, 2f);
            float lh = Mathf.Log(Mathf.Max(1f, screenH) / RefHeight, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(lw, lh, ScalerMatch));   // 99차: = 캔버스 scaleFactor
            float designW = screenW / scale;   // 화면을 디자인 단위로 (가로는 늘 720)
            float designH = screenH / scale;
            bgScale = Mathf.Min(1f, designH / SafeZoneHeight);
            bgCropPerEdge = Mathf.Max(0f, (BgDesignHeight * bgScale - designH) * 0.5f) * scale;
            // 세이프존이 실제로 다 보이는가 — 가로·세로 둘 다 본다(가로 화면에서는 가로가 먼저 걸린다).
            float need = Mathf.Min(designW / SafeZoneWidth, designH / SafeZoneHeight);
            safeZoneFits = Mathf.Min(1f, need) * SafeZoneHeight <= designH + 0.5f
                        && Mathf.Min(1f, need) * SafeZoneWidth <= designW + 0.5f;
        }
    }

    /// 캔버스 전체를 디자인 단위로 들고 있는 상자(배경 자리). 자식이 제작 기준(720×1600)으로
    /// 놓이면 화면이 짧은 쪽에서 저절로 잘린다.
    public class CoastUiCanvasBox : MonoBehaviour
    {
        private RectTransform _self, _parent;
        private float _lastW = -1f, _lastH = -1f;

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (_self == null) _self = transform as RectTransform;
            if (_parent == null) _parent = _self != null ? _self.parent as RectTransform : null;
            if (_self == null || _parent == null) return;
            float rw = _parent.rect.width, rh = _parent.rect.height;
            if (rw < 8f || rh < 8f) return;
            if (Mathf.Abs(rw - _lastW) < 0.25f && Mathf.Abs(rh - _lastH) < 0.25f) return;
            _lastW = rw; _lastH = rh;
            // 99차: 캔버스 단위 = 디자인 단위 → 스케일 없이 캔버스 크기 그대로.
            _self.localScale = Vector3.one;
            _self.sizeDelta = new Vector2(rw, rh);
        }
    }

    /// 제작 기준 배경(720×1600)을 화면에 맞춘다. 기본은 1배(가로 꽉 차고 남는 세로가 잘림),
    /// 16:9 보다 짧은 화면에서는 세이프존(720×1280)이 다 보이는 배율까지만 줄인다.
    public class CoastUiBgCover : MonoBehaviour
    {
        private RectTransform _self, _parent;
        private float _lastH = -1f;

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (_self == null) _self = transform as RectTransform;
            if (_parent == null) _parent = _self != null ? _self.parent as RectTransform : null;
            if (_self == null || _parent == null) return;
            float rh = _parent.rect.height;
            if (rh < 8f) return;
            if (Mathf.Abs(rh - _lastH) < 0.25f) return;
            _lastH = rh;
            float s = Mathf.Min(1f, rh / CoastUiCanvas.SafeZoneHeight);
            _self.localScale = new Vector3(s, s, 1f);
            _self.sizeDelta = new Vector2(CoastUiCanvas.BgDesignWidth, CoastUiCanvas.BgDesignHeight);
        }
    }

    /// 중앙 16:9 세이프존 상자 — 화면이 좁거나 짧으면 비율 유지로 줄여, 안에 놓인 필수 버튼이
    /// 절대 화면 밖으로 나가지 않게 한다.
    public class CoastUiSafeZoneFit : MonoBehaviour
    {
        private RectTransform _self, _parent;
        private float _lastW = -1f, _lastH = -1f;

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (_self == null) _self = transform as RectTransform;
            if (_parent == null) _parent = _self != null ? _self.parent as RectTransform : null;
            if (_self == null || _parent == null) return;
            float rw = _parent.rect.width, rh = _parent.rect.height;
            if (rw < 8f || rh < 8f) return;
            if (Mathf.Abs(rw - _lastW) < 0.25f && Mathf.Abs(rh - _lastH) < 0.25f) return;
            _lastW = rw; _lastH = rh;
            // 인셋(안전영역 − HudPad) 기준 배율. CoastUiDesignFit 과 달리 상자를 늘리지 않아,
            // 세로가 긴 화면에서도 자식 UI 가 중앙 16:9 밖으로 나가지 않는다.
            float s = CoastUiCanvas.FitScale(rw, rh);
            _self.localScale = new Vector3(s, s, 1f);
            _self.sizeDelta = new Vector2(CoastUiCanvas.HudDesignWidth, CoastUiCanvas.HudDesignHeight);
        }
    }

    /// 인셋이 HudDesignWidth×HudDesignHeight 보다 좁거나 **짧을** 때 Fit 자식을 비율 유지로 축소
    /// (매 프레임 안전영역 갱신 반영). 74차(사용자: 갤럭시 16:9 게임뷰에서 상하 잘림): 전엔 가로만
    /// 봤기 때문에 9:16보다 짧은 비율에서는 배율이 1로 남아 위아래가 그대로 잘렸다.
    public class CoastHudNarrowFit : MonoBehaviour
    {
        private RectTransform _self, _parent;
        private float _lastW = -1f, _lastH = -1f;

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (_self == null) _self = transform as RectTransform;
            if (_parent == null) _parent = _self != null ? _self.parent as RectTransform : null;
            if (_self == null || _parent == null) return;
            float rw = _parent.rect.width, rh = _parent.rect.height;
            if (rw < 8f || rh < 8f) return;
            if (Mathf.Abs(rw - _lastW) < 0.25f && Mathf.Abs(rh - _lastH) < 0.25f) return;
            _lastW = rw; _lastH = rh;
            float fit = CoastUiCanvas.FitScale(rw, rh);
            _self.localScale = new Vector3(fit, fit, 1f);
            // 스케일 후 부모와 같은 높이를 덮도록 내부 높이 = rh/fit
            _self.sizeDelta = new Vector2(CoastUiCanvas.HudDesignWidth, rh / fit);
        }
    }

    /// 664×1224 절대 좌표 배치를 화면 비율에 맞춰 통째로 축소하는 상자(가로·세로 동시 고려).
    /// 상자 크기는 부모(안전 영역 인셋)를 덮도록 rw/fit × rh/fit 로 잡아, 화면 가장자리에 붙인
    /// UI가 축소 후에도 실제 가장자리에 붙는다. 세로가 기준(1224)보다 남으면 그만큼 더 넓게 퍼진다.
    public class CoastUiDesignFit : MonoBehaviour
    {
        private RectTransform _self, _parent;
        private float _lastW = -1f, _lastH = -1f;

        private void LateUpdate() => ApplyNow();

        public void ApplyNow()
        {
            if (_self == null) _self = transform as RectTransform;
            if (_parent == null) _parent = _self != null ? _self.parent as RectTransform : null;
            if (_self == null || _parent == null) return;
            float rw = _parent.rect.width, rh = _parent.rect.height;
            if (rw < 8f || rh < 8f) return;
            if (Mathf.Abs(rw - _lastW) < 0.25f && Mathf.Abs(rh - _lastH) < 0.25f) return;
            _lastW = rw; _lastH = rh;
            float fit = CoastUiCanvas.FitScale(rw, rh);
            _self.localScale = new Vector3(fit, fit, 1f);
            // 가로는 배치 기준(664)을 넘기지 않는다 — 가운데 정렬 좌표(x0 = (664−…)/2)가 어긋나지 않게.
            _self.sizeDelta = new Vector2(Mathf.Min(CoastUiCanvas.HudDesignWidth, rw / fit), rh / fit);
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

            // Android: translucent status bar can overlay HUD while Screen.safeArea
            // still reports full height. Keep a minimum top clear when the bar is up,
            // or when top inset is suspiciously zero on a tall phone.
            float topClear = h - (r.y + r.height);
            float minTop = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
            float dp = Screen.dpi > 40f ? Screen.dpi : 160f;
            if (CoastSystemBars.StatusBarVisible || topClear < 2f)
                minTop = Mathf.Clamp(dp * 0.28f, 40f, h * 0.055f);   // ~28dp status / cutout
#endif
            if (topClear < minTop)
                r = Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, h - minTop);

            _safe.anchorMin = new Vector2(r.x / w, r.y / h);
            _safe.anchorMax = new Vector2((r.x + r.width) / w, (r.y + r.height) / h);
            _safe.offsetMin = Vector2.zero;
            _safe.offsetMax = Vector2.zero;
            // HudInset: 안전 영역 크기를 디자인 단위(÷1.5)로 환산하고 안쪽 여백(HudPad)을 뺀다
            if (_inset == null) _inset = _safe.Find(CoastUiCanvas.InsetName) as RectTransform;
            if (_inset != null)
            {
                // 74차(사용자: 갤럭시 16:9 게임뷰에서 상하 잘림) — 배치 좌표는 664×1224(9:16) 기준인데
                // 9:16보다 **짧은** 비율(16:9·3:4·태블릿·폴더블 펼침)에선 인셋 높이가 1224보다 작아져
                // 위(주차 알약)와 아래(다음 턴 줄)가 화면 밖으로 나갔다. 인셋을 「최소 664×1224 보장 +
                // 비율 유지 축소」로 바꾼다: 자식 좌표계는 늘 기준 크기 이상이고, 축소분을 스케일로 돌려
                // 화면에서 차지하는 영역(sizeDelta×scale)은 예전과 동일하다 → 꽉 찬 배경도 그대로 full-bleed.
                CoastUiCanvas.DesignMetrics(w, h, r.width, r.height, out var insetSize, out float fit);
                // 99차: DesignScale 을 더 이상 곱하지 않는다(캔버스 기준이 720×1280) — fit 축소만.
                _inset.localScale = new Vector3(fit, fit, 1f);
                _inset.sizeDelta = insetSize;
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
                // TitleUI is intentionally faded (splash / K-POP chapter select). Clearing
                // blocksRaycasts here used to leave the title permanently dead after restore.
                if (cg.gameObject.name == "TitleUI") continue;
                if (cg.GetComponentInParent<Selectable>() != null) continue;
                if (cg.GetComponentInParent<ScrollRect>() != null) continue;   // 24차-10(점검 2-5): 스크롤 수신 영역은 투명이 정상
                if (cg.GetComponentInParent<IPointerDownHandler>() != null) continue;   // 160차
                if (!CoversScreen(cg.transform as RectTransform)) continue;
                cg.blocksRaycasts = false;
                Log(cg.gameObject, "CanvasGroup alpha≈0");
            }
            foreach (var img in FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (!img.raycastTarget || img.color.a > 0.02f || !img.gameObject.activeInHierarchy) continue;
                if (img.GetComponentInParent<Selectable>() != null) continue;
                // 24차-10(점검 2-5): ScrollRect 의 투명 수신 이미지(알파 0.01)를 1초 뒤 꺼 버려 도감 팬아트·트로피 탭 스크롤이 죽었다.
                if (img.GetComponentInParent<ScrollRect>() != null) continue;
                // 160차: 투명한 **입력 수신판**(플로팅 조이스틱 영역 등)은 의도된 것 — 누르기 처리기가 달려 있으면 건드리지 않는다
                if (img.GetComponentInParent<IPointerDownHandler>() != null) continue;
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
            if (CoastRemoteKeys.Down(KeyCode.J)) Dump();
            if (!CoastRemoteKeys.Down(KeyCode.K)) return;
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
