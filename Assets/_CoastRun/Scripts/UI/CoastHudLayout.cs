using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Shared top/bottom chrome matching the coastal UI mock.
    public static class CoastHudLayout
    {
        public static readonly Color BarColor = new Color(0.07f, 0.16f, 0.30f, 0.86f);
        public static readonly Color AccentCyan = new Color(0.35f, 0.82f, 0.95f, 1f);

        /// Chapter-complete and combo accents — the warm side of the palette, so a
        /// chapter ending reads differently from a plain stage clear.
        public static readonly Color AccentWarm = new Color(0.98f, 0.72f, 0.38f, 1f);

        public static RectTransform EnsureTopBar(Canvas canvas)
        {
            var root = CoastUiCanvas.Root(canvas);
            var existing = root.Find("TopBar") as RectTransform;
            if (existing != null)
                return existing;

            var go = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 108f);
            go.GetComponent<Image>().color = BarColor;
            go.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        public static RectTransform EnsureBottomBar(Canvas canvas)
        {
            var root = CoastUiCanvas.Root(canvas);
            var existing = root.Find("BottomBar") as RectTransform;
            if (existing != null)
                return existing;

            var go = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 118f);
            go.GetComponent<Image>().color = BarColor;
            return rt;
        }

        static Font _bold, _medium;
        /// 8차: Pretendard(본문 Bold) — 없으면 내장 폰트. 일·태국어 등은 OS 폴백.
        public static Font Font()
        {
            if (_bold == null) _bold = Resources.Load<Font>("CoastRun/Fonts/Pretendard-Bold");
            if (_bold != null) return _bold;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// 10차: 모바일 가독성 — 모든 헬퍼 글자를 한 번에 키운다(×1.15, 최소 14). 레이아웃 숫자는 그대로, Overflow 로 넘친다.
        public const float TextScale = 1.3f;   // 11차: 갤럭시 S(6.7", 2340×1080) 기준 본문 16sp ≈ 기준 캔버스 22 → 최소 16
        public static int Scaled(int size) => Mathf.Max(size >= 10 ? 16 : size, Mathf.RoundToInt(size * TextScale));

        public static Text MakeText(Transform parent, string name, string content, int size, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var text = go.AddComponent<Text>();
            text.font = Font();
            text.fontSize = Scaled(size);
            text.fontStyle = _bold != null ? FontStyle.Normal : FontStyle.Bold;   // 8차: 볼드 폰트면 가짜 볼드 끔
            text.color = Color.white;
            text.alignment = align;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
