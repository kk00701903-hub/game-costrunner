using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 프린세스 메이커풍 장식 패널/버튼 — 육성·타이틀·컷씬이 같이 쓴다.
    /// (RaisingUI.OrnatePanel과 같은 룩, 공용 버전)
    public static class CoastOrnate
    {
        public static readonly Color Ivory = Hex("#FFF8E7");
        public static readonly Color Wood = Hex("#8D6E63");
        public static readonly Color WoodDark = Hex("#4E342E");
        public static readonly Color Gold = Hex("#D4AF37");
        public static readonly Color GoldLight = Hex("#F1D37A");
        public static readonly Color Red = Hex("#E53935");
        public static readonly Color Ink = Hex("#3E2723");
        public static readonly Color Navy = new Color(0.10f, 0.14f, 0.30f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// 골드/우드 테두리 + 아이보리 속 + 코너 장식. 앵커 스트레치 기준(offset은 좌하/우상).
        public static Image Panel(Transform parent, string name, Color edge, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color? inner = null)
        {
            var outer = CoastUiArt.Panel(parent, name, edge, 16);
            var rt = outer.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            outer.raycastTarget = false;
            Decorate(outer, edge, inner);
            return outer;
        }

        /// 고정 크기 패널(앵커 한 점 + 피벗 중앙).
        public static Image PanelSized(Transform parent, string name, Color edge, Vector2 anchor, Vector2 pos, Vector2 size, Color? inner = null)
        {
            var outer = CoastUiArt.Panel(parent, name, edge, 16);
            var rt = outer.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            outer.raycastTarget = false;
            Decorate(outer, edge, inner);
            return outer;
        }

        private static void Decorate(Image outer, Color edge, Color? inner)
        {
            Color fill = inner ?? (edge == WoodDark ? Wood : Ivory);
            var body = CoastUiArt.Panel(outer.transform, "Inner", fill, 13);
            Stretch(body.rectTransform, 4f, 4f, -4f, -4f);
            body.raycastTarget = false;
            var shTop = CoastHudLayout.MakeImage(body.transform, "ShadowTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(6f, -10f), new Vector2(-6f, 0f), new Color(0f, 0f, 0f, 0.10f));
            shTop.sprite = CoastUiArt.RoundedRect(6); shTop.type = Image.Type.Sliced; shTop.raycastTarget = false;
            foreach (var c in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) })
            {
                var d = CoastHudLayout.MakeImage(outer.transform, "Corner", c, c, new Vector2(-9f, -9f), new Vector2(9f, 9f), edge == Gold ? GoldLight : Gold);
                d.sprite = CoastUiArt.RoundedRect(3); d.type = Image.Type.Sliced; d.raycastTarget = false;
                d.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                d.rectTransform.anchoredPosition = new Vector2(c.x == 0f ? 6f : -6f, c.y == 0f ? 6f : -6f);
                var dot = CoastHudLayout.MakeImage(d.transform, "Dot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-3f, -3f), new Vector2(3f, 3f), Ivory);
                dot.sprite = CoastUiArt.RoundedRect(3); dot.type = Image.Type.Sliced; dot.raycastTarget = false;
            }
        }

        /// 프메식 메뉴 버튼: 우드 배경 + 골드 테두리 + 아이보리 글자.
        public static Button MenuButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, Color? fill = null, int fontSize = 26)
        {
            var outer = CoastUiArt.Panel(parent, name, Gold, 14);
            var rt = outer.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            outer.raycastTarget = true;
            var inner = CoastUiArt.Panel(outer.transform, "Fill", fill ?? Wood, 12);
            Stretch(inner.rectTransform, 3f, 3f, -3f, -3f);
            inner.raycastTarget = false;
            var t = CoastHudLayout.MakeText(outer.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = Ivory;
            t.raycastTarget = false;
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.45f), 1.5f);
            var btn = outer.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        /// 반투명 유리 버튼: 배경 그림을 가리지 않는 작은 메뉴용. 어두운 유리 + 얇은 금테 + 아이보리 글자.
        public static Button GlassButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, float alpha = 0.32f, int fontSize = 18, bool primary = false)
        {
            var outer = CoastUiArt.Panel(parent, name, new Color(GoldLight.r, GoldLight.g, GoldLight.b, primary ? 0.75f : 0.45f), 12);
            var rt = outer.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            outer.raycastTarget = true;
            var inner = CoastUiArt.Panel(outer.transform, "Fill", primary ? new Color(0.55f, 0.18f, 0.08f, alpha + 0.25f) : new Color(0.08f, 0.06f, 0.10f, alpha), 11);
            Stretch(inner.rectTransform, 1.5f, 1.5f, -1.5f, -1.5f);
            inner.raycastTarget = false;
            var t = CoastHudLayout.MakeText(outer.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = new Color(1f, 0.97f, 0.88f, 0.96f);
            t.raycastTarget = false;
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.5f), 1.2f);
            var btn = outer.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = inner;
            var cb = btn.colors; cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f); cb.pressedColor = new Color(1.6f, 1.6f, 1.6f, 1f); btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        public static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(r, t);
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = CoastHudLayout.MakeText(parent, name, text, size, align, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.color = color;
            t.raycastTarget = false;
            return t;
        }
    }
}
