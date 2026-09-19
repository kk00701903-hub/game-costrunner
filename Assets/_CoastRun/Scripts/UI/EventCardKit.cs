using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 60차(사용자 시안 「대회 미달…」): 육성 이벤트 팝업 공통 카드 — 크림 카드(은은한 흰 테두리·모서리 ✦), 젤리 글씨 제목,
    ///   점·✦ 구분선, 동그란 아이콘 + 글 줄(오른쪽 알약 배지), 흰 안내 상자, 아이콘 달린 큰 버튼 두 개.
    ///   ContestResultUI·ContestIntroUI·GameOverUI 가 쓴다.
    internal static class EventCardKit
    {
        public static readonly Color Navy = new Color(0.16f, 0.14f, 0.30f);
        public static readonly Color Cream = new Color(0.99f, 0.96f, 0.90f);
        public static readonly Color Ink = new Color(0.22f, 0.18f, 0.16f);

        /// 캔버스 + 어둠 + 나무 액자 카드(112차 스토리모드 팝업 표준). 되돌림: 크림 속지 RectTransform.
        public static RectTransform Card(string canvasName, int order, Vector2 size, out Canvas canvas, float y = 0f)
        {
            var crt = StoryPopupKit.Frame(canvasName, order, size, out canvas, y);
            return crt;
        }

        public static void Sparkle(RectTransform parent, Vector2 anchor, Vector2 pos, int size, Color col)
        {
            var t = CoastHudLayout.MakeText(parent, "Sp", "✦", size, TextAnchor.MiddleCenter, anchor, anchor, new Vector2(pos.x - size, pos.y - size), new Vector2(pos.x + size, pos.y + size));
            t.color = col; t.raycastTarget = false; CoastUiArt.OutlineText(t, new Color(1f, 0.85f, 0.5f, 0.5f), 1f);
        }

        /// 젤리 글씨 제목(두꺼운 진한 테두리 + 위쪽 하이라이트 겹 글자). yTop = 카드 위에서 내려온 거리(양수).
        public static Text JellyTitle(RectTransform card, string text, Color fill, Color edge, float yTop, float height, int size = 52)
        {
            // 그림자
            var sh = CoastHudLayout.MakeText(card, "TitleShadow", text, size, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -yTop - height - 4f), new Vector2(4f, -yTop - 4f));
            sh.color = new Color(edge.r * 0.6f, edge.g * 0.6f, edge.b * 0.6f, 0.55f); sh.fontStyle = FontStyle.Bold; sh.raycastTarget = false;
            sh.resizeTextForBestFit = true; sh.resizeTextMinSize = 20; sh.resizeTextMaxSize = CoastHudLayout.Scaled(size);
            var t = CoastHudLayout.MakeText(card, "Title", text, size, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -yTop - height), new Vector2(0f, -yTop));
            t.color = fill; t.fontStyle = FontStyle.Bold; t.raycastTarget = false; CoastUiArt.OutlineText(t, edge, 3.2f);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = CoastHudLayout.Scaled(size);
            // 하이라이트(윗부분만 살짝 밝게 — 같은 글자를 위로 2px 올려 반투명 흰색)
            var hl = CoastHudLayout.MakeText(card, "TitleHl", text, size, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -yTop - height + 3f), new Vector2(0f, -yTop + 3f));
            hl.color = new Color(1f, 1f, 1f, 0.22f); hl.fontStyle = FontStyle.Bold; hl.raycastTarget = false;
            hl.resizeTextForBestFit = true; hl.resizeTextMinSize = 20; hl.resizeTextMaxSize = CoastHudLayout.Scaled(size);
            return t;
        }

        /// 「· · · ✦ ✦ ✦ · · ·」 구분선.
        public static void Divider(RectTransform card, float yTop)
        {
            var t = CoastHudLayout.MakeText(card, "Div", "· · · · · · ✦ ✦ ✦ · · · · · ·", 16, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -yTop - 22f), new Vector2(-20f, -yTop));
            t.color = new Color(0.95f, 0.75f, 0.35f); t.raycastTarget = false;
            var l = CoastUiArt.Panel(card, "DivLine", new Color(0.95f, 0.85f, 0.55f, 0.5f), 2); l.raycastTarget = false;
            l.rectTransform.anchorMin = new Vector2(0.1f, 1f); l.rectTransform.anchorMax = new Vector2(0.9f, 1f); l.rectTransform.offsetMin = new Vector2(0f, -yTop - 13f); l.rectTransform.offsetMax = new Vector2(0f, -yTop - 11f);
        }

        /// 동그란 아이콘 + 글 한 줄(+ 오른쪽 알약 배지). 카드 위에서 yTop 만큼 내려온 자리, 높이 h.
        public static Text IconRow(RectTransform parent, string icon, Color iconBg, string text, float yTop, float h, int size = 24, string badge = null, Color? badgeCol = null, float left = 36f, float right = 36f)
        {
            var circle = CoastUiArt.Panel(parent, "IcBg", iconBg, (int)(h * 0.5f)); circle.raycastTarget = false;
            var cr = circle.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0f, 1f); cr.pivot = new Vector2(0f, 1f); cr.anchoredPosition = new Vector2(left, -yTop); cr.sizeDelta = new Vector2(h, h);
            var sp = CoastUiArt.Art(icon);
            if (sp != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(cr, false); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.sizeDelta = new Vector2(h * 0.68f, h * 0.68f);
            }
            float badgeW = badge != null ? 96f : 0f;
            var t = CoastHudLayout.MakeText(parent, "Row", text, size, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(left + h + 16f, -yTop - h), new Vector2(-right - badgeW, -yTop));
            t.color = Ink; t.fontStyle = FontStyle.Bold; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(size);
            if (badge != null)
            {
                var pill = CoastUiArt.GlossyPill(parent, "Badge", badgeCol ?? new Color(0.98f, 0.45f, 0.35f), 16, 5); pill.raycastTarget = false;
                var pr = pill.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(1f, 1f); pr.pivot = new Vector2(1f, 1f); pr.anchoredPosition = new Vector2(-right, -yTop - (h - 36f) * 0.5f); pr.sizeDelta = new Vector2(88f, 36f);
                var bt = CoastHudLayout.MakeText(pr, "T", badge, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                bt.color = Color.white; bt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(bt, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            }
            return t;
        }

        /// 109차(사용자: 「팝업에 꼬마 사진을 넣어 꼬마가 하라고 하는 것처럼 — 꼬마와 함께하고 있다는 느낌」):
        ///   카드 왼쪽 위 모서리에 걸친 꼬마(UI_Kid_Bust, 주황 우비) + 오른쪽 말풍선 한 줄. 버튼과 겹치지 않게 카드 위쪽에만.
        ///   right = true 면 오른쪽 위. size = 꼬마 키(디자인 단위).
        public static void Kid(RectTransform card, string line, bool right = false, float size = 170f)
            => KidAt(card, line, new Vector2(right ? 1f : 0f, 1f), new Vector2(right ? 34f : -34f, 64f), size, right, true);

        /// 꼬마를 아무 데나 — anchor/pivot 같은 점, pos = 그 점에서의 위치. flip = 오른쪽을 보게 뒤집기. bubbleAbove = 말풍선을 위(카드 밖)에 / 아니면 머리 옆.
        public static void KidAt(RectTransform parent, string line, Vector2 anchor, Vector2 pos, float size, bool flip, bool bubbleAbove)
        {
            var tex = ArtAssets.LoadTexture("UI_Kid_Bust") ?? ArtAssets.LoadTexture("UI_Butler_Boy");
            if (tex == null) return;
            float w = size * tex.width / Mathf.Max(1, tex.height);
            var host = new GameObject("KidHost", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(parent, false);
            host.anchorMin = host.anchorMax = anchor; host.pivot = anchor;
            host.anchoredPosition = pos; host.sizeDelta = new Vector2(w, size);
            var kid = CoastHudLayout.MakeImage(host, "Kid", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            kid.sprite = CoastUiArt.AsSprite(tex); kid.preserveAspect = true; kid.raycastTarget = false;
            if (flip) kid.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            host.gameObject.AddComponent<KidBob>();
            if (string.IsNullOrEmpty(line)) return;
            bool right = anchor.x > 0.5f;
            var bub = CoastUiArt.CutePill(parent, "KidBubble", Color.white, 16, 0); bub.raycastTarget = false;
            var br = bub.rectTransform; br.anchorMin = br.anchorMax = anchor; br.pivot = new Vector2(right ? 1f : 0f, 0f);
            float bw = Mathf.Clamp(line.Length * 15f + 40f, 150f, 330f);
            br.sizeDelta = new Vector2(bw, 46f);
            if (bubbleAbove) br.anchoredPosition = new Vector2(right ? pos.x - (w - 10f) : pos.x + w - 10f, 20f);
            else br.anchoredPosition = new Vector2(right ? pos.x - (w - 6f) : pos.x + w - 6f, pos.y + size * 0.70f);
            var tail = CoastHudLayout.MakeText(bub.rectTransform, "Tail", right ? "◗" : "◖", 22, TextAnchor.MiddleCenter, new Vector2(right ? 1f : 0f, 0f), new Vector2(right ? 1f : 0f, 0f), new Vector2(right ? -4f : -18f, 2f), new Vector2(right ? 18f : 4f, 26f));
            tail.color = Color.white; tail.raycastTarget = false;
            var bt = CoastHudLayout.MakeText(br, "T", line, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, 0f));
            bt.color = new Color(0.30f, 0.18f, 0.12f); bt.fontStyle = FontStyle.Bold; bt.raycastTarget = false;
            bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 10; bt.resizeTextMaxSize = CoastHudLayout.Scaled(16);
        }

        /// 꼬마가 숨 쉬듯 위아래로 살짝(±4) 움직인다.
        public class KidBob : MonoBehaviour
        {
            private RectTransform _rt; private Vector2 _base; private float _t;
            private void Awake() { _rt = (RectTransform)transform; _base = _rt.anchoredPosition; _t = UnityEngine.Random.value * 6f; }
            private void Update() { _t += Time.unscaledDeltaTime; _rt.anchoredPosition = _base + new Vector2(0f, Mathf.Sin(_t * 2.2f) * 4f); _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 1.3f) * 2.5f); }
        }

        /// 109차(사용자: 「사진이 좀 움직였으면 — 동영상 말고」): 정지 그림에 켄번즈(천천히 확대·표류). 그림의 부모에 RectMask2D 를 붙여 틀 밖으로 안 나가게.
        ///   amount = 최대 확대 비율(0.08 = 8%), period = 한 번 갔다 오는 시간(초).
        public static void Animate(Image art, float amount = 0.08f, float period = 9f)
        {
            if (art == null) return;
            var parent = art.transform.parent as RectTransform;
            if (parent != null && parent.GetComponent<RectMask2D>() == null && parent.GetComponent<Mask>() == null) parent.gameObject.AddComponent<RectMask2D>();
            var m = art.gameObject.AddComponent<StillMotion>(); m.amount = amount; m.period = period;
        }

        public class StillMotion : MonoBehaviour
        {
            public float amount = 0.08f, period = 9f;
            private RectTransform _rt; private Vector2 _base; private float _t; private Vector2 _dir;
            private void Awake()
            {
                _rt = (RectTransform)transform; _base = _rt.anchoredPosition; _t = UnityEngine.Random.value * 3f;
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); _dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f);
            }
            private void Update()
            {
                if (_rt == null) return;
                _t += Time.unscaledDeltaTime;
                float u = 0.5f - 0.5f * Mathf.Cos(_t / period * Mathf.PI * 2f);   // 0→1→0 부드럽게
                float sc = 1f + amount * (0.35f + 0.65f * u);
                _rt.localScale = new Vector3(sc, sc, 1f);
                float drift = _rt.rect.width * amount * 0.35f;
                _rt.anchoredPosition = _base + _dir * (drift * (u - 0.5f));
            }
        }

        /// 흰 안내 상자(둥근 흰 패널). 되돌림: 상자 RectTransform — 안에 IconRow 를 쌓는다.
        public static RectTransform InfoBox(RectTransform card, float yTop, float height, float side = 28f)
        {
            var box = CoastUiArt.Panel(card, "Info", new Color(1f, 1f, 1f, 0.92f), 22); box.raycastTarget = false;
            var r = box.rectTransform; r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(side, -yTop - height); r.offsetMax = new Vector2(-side, -yTop);
            return r;
        }

        /// 아이콘 달린 큰 버튼(젤리 알약 + ✦).
        public static Button IconButton(RectTransform parent, string name, string icon, string label, Color col, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int font = 24)
        {
            var b = CoastUiArt.GlossyPill(parent, name, col, 26, 9);
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size; b.raycastTarget = true;
            float textLeft = 10f;
            var sp = CoastUiArt.Art(icon);
            if (sp != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(rt, false); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0f, 0.5f); im.rectTransform.pivot = new Vector2(0f, 0.5f);
                im.rectTransform.anchoredPosition = new Vector2(16f, 1f); im.rectTransform.sizeDelta = new Vector2(size.y * 0.5f, size.y * 0.5f);
                textLeft = 16f + size.y * 0.5f + 6f;
            }
            var t = CoastHudLayout.MakeText(rt, "T", label, font, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(textLeft, 3f), new Vector2(-10f, 0f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.4f); t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(font);
            Sparkle(rt, new Vector2(1f, 1f), new Vector2(-14f, -10f), 14, new Color(1f, 1f, 1f, 0.95f));
            Sparkle(rt, new Vector2(0f, 0f), new Vector2(14f, 10f), 10, new Color(1f, 1f, 1f, 0.85f));
            var bt = b.gameObject.AddComponent<Button>(); bt.transition = Selectable.Transition.None;
            bt.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            return bt;
        }

        // ── 스토리 모드 선택 팝업 (시안: 헬섬 이벤트 / 어디 알바?) ──

        public static readonly Color SoftPink = new Color(0.99f, 0.90f, 0.93f);
        public static readonly Color SoftBlue = new Color(0.89f, 0.95f, 0.99f);
        public static readonly Color TagPink = new Color(0.95f, 0.45f, 0.62f);
        public static readonly Color BrownInk = new Color(0.30f, 0.20f, 0.18f);
        public static readonly Color GoldJob = new Color(1f, 0.82f, 0.38f);
        public static readonly Color LavenderJob = new Color(0.72f, 0.62f, 0.95f);

        /// 상단 분홍 알약 태그 (예: 「✨ 헬섬 이벤트」).
        public static Text HellsumTag(RectTransform card, string label, float yTop = 18f)
        {
            var pill = CoastUiArt.CutePill(card, "Tag", TagPink, 16, 0);
            var pr = pill.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f); pr.pivot = new Vector2(0.5f, 1f);
            pr.anchoredPosition = new Vector2(0f, -yTop); pr.sizeDelta = new Vector2(220f, 36f); pill.raycastTarget = false;
            var t = CoastHudLayout.MakeText(pr, "T", label, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, -1f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            return t;
        }

        /// A/B 본문 블록 (연분홍·연파랑 + 아이콘 + 「A:」 헤더).
        public static RectTransform ChoiceBlock(RectTransform card, string name, bool isA, string icon, string body, float yTop, float height)
        {
            Color bg = isA ? SoftPink : SoftBlue;
            Color head = isA ? new Color(0.85f, 0.28f, 0.48f) : new Color(0.28f, 0.48f, 0.82f);
            var box = CoastUiArt.CutePill(card, name, bg, 18, 0);
            var r = box.rectTransform; r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(28f, -yTop - height); r.offsetMax = new Vector2(-28f, -yTop); box.raycastTarget = false;

            var circle = CoastUiArt.Panel(r, "IcBg", head, 14); circle.raycastTarget = false;
            var cr = circle.rectTransform; cr.anchorMin = cr.anchorMax = new Vector2(0f, 1f); cr.pivot = new Vector2(0f, 1f);
            cr.anchoredPosition = new Vector2(14f, -12f); cr.sizeDelta = new Vector2(28f, 28f);
            var sp = CoastUiArt.Art(icon);
            if (sp != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(cr, false); im.sprite = sp; im.preserveAspect = true; im.color = Color.white; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.sizeDelta = new Vector2(18f, 18f);
            }
            var letter = CoastHudLayout.MakeText(r, "Letter", isA ? "A:" : "B:", 18, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(50f, -40f), new Vector2(-14f, -10f));
            letter.color = head; letter.fontStyle = FontStyle.Bold; letter.raycastTarget = false;

            var bodyT = CoastHudLayout.MakeText(r, "Body", body, 15, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(16f, 12f), new Vector2(-16f, -44f));
            bodyT.color = BrownInk; bodyT.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyT.verticalOverflow = VerticalWrapMode.Truncate;
            bodyT.resizeTextForBestFit = true; bodyT.resizeTextMinSize = CoastHudLayout.MinFontSize;
            bodyT.resizeTextMaxSize = CoastHudLayout.Scaled(15); bodyT.raycastTarget = false;
            return r;
        }

        /// 하단 나란히 A/B 버튼 — 연한 배경 + 갈색 글씨(시안).
        public static Button SoftChoiceButton(RectTransform parent, string name, string icon, string label, bool isA, Vector2 pos, Vector2 size, Action onClick)
        {
            Color fill = isA ? SoftPink : SoftBlue;
            Color edge = isA ? new Color(0.92f, 0.55f, 0.68f) : new Color(0.55f, 0.72f, 0.92f);
            var glow = CoastUiArt.Panel(parent, name + "Edge", edge, 22); glow.raycastTarget = false;
            var ge = glow.rectTransform; ge.anchorMin = ge.anchorMax = new Vector2(0.5f, 0f); ge.pivot = new Vector2(0.5f, 0f);
            ge.anchoredPosition = pos; ge.sizeDelta = size + new Vector2(6f, 6f);

            var b = CoastUiArt.CutePill(parent, name, fill, 20, 3);
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; b.raycastTarget = true;

            float textLeft = 8f;
            var sp = CoastUiArt.Art(icon);
            if (sp != null)
            {
                var im = new GameObject("Ic", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(rt, false); im.sprite = sp; im.preserveAspect = true; im.color = BrownInk; im.raycastTarget = false;
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0f, 0.5f); im.rectTransform.pivot = new Vector2(0f, 0.5f);
                im.rectTransform.anchoredPosition = new Vector2(14f, 1f); im.rectTransform.sizeDelta = new Vector2(size.y * 0.42f, size.y * 0.42f);
                textLeft = 14f + size.y * 0.42f + 4f;
            }
            var t = CoastHudLayout.MakeText(rt, "T", label, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(textLeft, 2f), new Vector2(-10f, 0f));
            t.color = BrownInk; t.fontStyle = FontStyle.Bold; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            var bt = b.gameObject.AddComponent<Button>(); bt.transition = Selectable.Transition.None;
            bt.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            return bt;
        }

        /// 「어디 알바?」식 젤리 배너 제목.
        public static Text PickBanner(RectTransform parent, string text, Color fill, Color edge, float yTop = 70f)
        {
            var bar = CoastUiArt.GlossyPill(parent, "Banner", fill, 28, 10);
            var br = bar.rectTransform; br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f); br.pivot = new Vector2(0.5f, 1f);
            br.anchoredPosition = new Vector2(0f, -yTop); br.sizeDelta = new Vector2(420f, 64f); bar.raycastTarget = false;
            return JellyTitle(br, text, Color.white, edge, 4f, 56f, 34);
        }

        /// 알바/쉼/놀기 선택 카드 — 112차 시안: 세로 큰 카드(배지·그림·제목·네이비 미리보기 버튼).
        public static Button ThemedPickCard(RectTransform parent, string name, string tab, string title, Color fill, Color tabCol, Vector2 pos, Vector2 size, Action onClick)
            => StoryPickCard(parent, name, tab, title, fill, tabCol, pos, size, onClick, null);

        /// <param name="preview">네이비 버튼 안 글(예: 「⚡ 기운 +10」). null 이면 버튼 숨김.</param>
        /// 135차(사용자 시안): 세로형 선택 카드 — 위 가운데 배지, 큰 그림(가로 4:3), 제목, 아래 남색 효과 칩. 두 장을 나란히 놓는다.
        public static Button TallPickCard(RectTransform parent, string name, string tab, string title, Color fill, Color tabCol,
            Vector2 pos, Vector2 size, Action onClick, string preview)
        {
            var navy = new Color(0.16f, 0.18f, 0.36f);
            var edgeCol = Color.Lerp(tabCol, fill, 0.30f);
            var edge = CoastUiArt.CutePill(parent, name + "Edge", edgeCol, 30, 0); edge.raycastTarget = false;
            var er = edge.rectTransform; er.anchorMin = er.anchorMax = new Vector2(0.5f, 0.5f); er.pivot = new Vector2(0.5f, 0.5f);
            er.anchoredPosition = pos + new Vector2(0f, -4f); er.sizeDelta = size + new Vector2(10f, 10f);
            var card = CoastUiArt.CutePill(parent, name, fill, 26, 0);
            var rt = card.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; card.raycastTarget = true;

            // 그림 자리(호출측에서 ArtFrame 채움): 위 여백 22, 가로 4:3
            float artW = size.x - 28f, artH = artW * 0.82f;   // 136차(시안): 그림을 더 꽉 차게
            var artSlot = new GameObject("ArtSlot", typeof(RectTransform)).GetComponent<RectTransform>();
            artSlot.SetParent(rt, false);
            artSlot.anchorMin = new Vector2(0.5f, 1f); artSlot.anchorMax = new Vector2(0.5f, 1f); artSlot.pivot = new Vector2(0.5f, 1f);
            artSlot.anchoredPosition = new Vector2(0f, -24f); artSlot.sizeDelta = new Vector2(artW, artH);
            var artBorder = CoastUiArt.CutePill(artSlot, "ArtBorder", Color.white, 18, 0); artBorder.raycastTarget = false;
            var abr = artBorder.rectTransform; abr.anchorMin = Vector2.zero; abr.anchorMax = Vector2.one; abr.offsetMin = new Vector2(-4f, -4f); abr.offsetMax = new Vector2(4f, 4f);

            // 제목(그림 아래) — 굵은 갈색
            float titleTop = 24f + artH + 10f;
            string stars = null; int si = title.IndexOf(" ★");   // 136차: 숙련 별은 제목 아래 작은 금색 줄로 — 제목은 시안처럼 크게 한 줄
            if (si > 0) { stars = title.Substring(si + 1); title = title.Substring(0, si); }
            var nm = CoastHudLayout.MakeText(rt, "N", title, 30, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -(titleTop + 56f)), new Vector2(-10f, -titleTop));
            nm.color = BrownInk; nm.fontStyle = FontStyle.Bold; nm.horizontalOverflow = HorizontalWrapMode.Overflow;   // 136차: 한 줄 유지(길면 CoastTextFit이 줄임)
            nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 14; nm.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            CoastUiArt.OutlineText(nm, new Color(1f, 1f, 1f, 0.55f), 1.5f);
            nm.raycastTarget = false;
            if (!string.IsNullOrEmpty(stars))
            {
                var st = CoastHudLayout.MakeText(rt, "Stars", stars, 18, TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -(titleTop + 84f)), new Vector2(-10f, -(titleTop + 56f)));
                st.color = new Color(0.95f, 0.70f, 0.20f); st.fontStyle = FontStyle.Bold; st.raycastTarget = false;
                CoastUiArt.OutlineText(st, new Color(1f, 1f, 1f, 0.6f), 1.2f);
            }

            if (!string.IsNullOrEmpty(preview))
            {
                var btnPill = CoastUiArt.CutePill(rt, "PvBg", navy, 18, 0);
                var pr = btnPill.rectTransform; pr.anchorMin = new Vector2(0.5f, 0f); pr.anchorMax = new Vector2(0.5f, 0f); pr.pivot = new Vector2(0.5f, 0f);
                // 136차: 미리보기 글이 길면(스탯 4개 + 돈 + 기운 + 실패%) 한 줄에 못 들어가 칩 밖으로 새던 문제 → 두 줄 칩 + 줄바꿈 + 넘치면 자름
                float chipH = size.x >= 280f ? 72f : 90f;
                pr.anchoredPosition = new Vector2(0f, 18f); pr.sizeDelta = new Vector2(size.x - 32f, chipH);
                btnPill.raycastTarget = false;
                Sparkle(pr, new Vector2(0f, 1f), new Vector2(12f, -8f), 9, Color.white);
                Sparkle(pr, new Vector2(1f, 0f), new Vector2(-12f, 8f), 9, Color.white);
                var pvT = CoastHudLayout.MakeText(pr, "T", preview, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-14f, -6f));
                pvT.color = Color.white; pvT.fontStyle = FontStyle.Bold; pvT.raycastTarget = false;
                pvT.horizontalOverflow = HorizontalWrapMode.Wrap; pvT.verticalOverflow = VerticalWrapMode.Truncate;
                pvT.resizeTextForBestFit = true; pvT.resizeTextMinSize = 12; pvT.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            }

            // 위 가운데 배지(카드 윗변에 걸침) — 카드보다 나중에 만들어 위에 그려짐
            var tabPill = CoastUiArt.CutePill(parent, name + "Tab", tabCol, 14, 0);
            var tr = tabPill.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f); tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = pos + new Vector2(0f, size.y * 0.5f + 2f); tr.sizeDelta = new Vector2(Mathf.Clamp(tab.Length * 20f + 44f, 120f, 220f), 40f);
            tabPill.raycastTarget = false;
            var tt = CoastHudLayout.MakeText(tr, "T", tab, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 1f), new Vector2(-6f, -1f));
            tt.color = Color.white; tt.fontStyle = FontStyle.Bold; tt.raycastTarget = false;
            CoastUiArt.OutlineText(tt, new Color(0f, 0f, 0f, 0.25f), 1.2f);

            var btn = card.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            return btn;
        }

        public static Button StoryPickCard(RectTransform parent, string name, string tab, string title, Color fill, Color tabCol,
            Vector2 pos, Vector2 size, Action onClick, string preview)
        {
            var navy = new Color(0.18f, 0.20f, 0.32f);
            var card = CoastUiArt.CutePill(parent, name, fill, 28, 0);
            var rt = card.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; card.raycastTarget = true;
            // 시안: 카드 테는 본색보다 살짝 진한 파스텔(노랑/보라)
            var edgeCol = Color.Lerp(tabCol, fill, 0.35f);
            var edge = CoastUiArt.Panel(rt, "Edge", edgeCol, 28); edge.raycastTarget = false;
            var er = edge.rectTransform; er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
            er.offsetMin = new Vector2(-6f, -6f); er.offsetMax = new Vector2(6f, 6f);
            edge.transform.SetAsFirstSibling();
            // 안쪽 크림 패딩 느낌
            var inner = CoastUiArt.Panel(rt, "Inner", fill, 24); inner.raycastTarget = false;
            var ir0 = inner.rectTransform; ir0.anchorMin = Vector2.zero; ir0.anchorMax = Vector2.one;
            ir0.offsetMin = new Vector2(5f, 5f); ir0.offsetMax = new Vector2(-5f, -5f);

            // 좌상단 배지
            var tabPill = CoastUiArt.CutePill(rt, "Tab", tabCol, 12, 0);
            var tr = tabPill.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0f, 1f); tr.pivot = new Vector2(0f, 1f);
            tr.anchoredPosition = new Vector2(18f, -14f); tr.sizeDelta = new Vector2(Mathf.Clamp(tab.Length * 18f + 36f, 100f, 200f), 34f);
            tabPill.raycastTarget = false;
            var tt = CoastHudLayout.MakeText(tr, "T", tab, 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 1f), new Vector2(-6f, -1f));
            tt.color = Color.white; tt.fontStyle = FontStyle.Bold; tt.raycastTarget = false;

            // 그림 자리(호출측에서 ArtFrame 채움) — 앵커만 잡아 둠
            var artSlot = new GameObject("ArtSlot", typeof(RectTransform)).GetComponent<RectTransform>();
            artSlot.SetParent(rt, false);
            artSlot.anchorMin = new Vector2(0f, 1f); artSlot.anchorMax = new Vector2(1f, 1f); artSlot.pivot = new Vector2(0.5f, 1f);
            artSlot.anchoredPosition = new Vector2(0f, -56f); artSlot.sizeDelta = new Vector2(-36f, size.y * 0.48f);

            // 제목(그림 아래)
            var nm = CoastHudLayout.MakeText(rt, "N", title, 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 78f), new Vector2(-20f, 128f));
            nm.color = BrownInk; nm.fontStyle = FontStyle.Bold; nm.horizontalOverflow = HorizontalWrapMode.Wrap;
            nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 14; nm.resizeTextMaxSize = CoastHudLayout.Scaled(24);
            nm.raycastTarget = false;

            if (!string.IsNullOrEmpty(preview))
            {
                var btnPill = CoastUiArt.CutePill(rt, "PvBg", navy, 18, 0);
                var pr = btnPill.rectTransform; pr.anchorMin = new Vector2(0.5f, 0f); pr.anchorMax = new Vector2(0.5f, 0f); pr.pivot = new Vector2(0.5f, 0f);
                pr.anchoredPosition = new Vector2(0f, 14f); pr.sizeDelta = new Vector2(size.x - 40f, 62f);   // 136차: 두 줄 칩
                btnPill.raycastTarget = false;
                Sparkle(rt, new Vector2(0.5f, 0f), new Vector2(-(size.x * 0.40f), 46f), 12, Color.white);
                Sparkle(rt, new Vector2(0.5f, 0f), new Vector2(size.x * 0.40f, 46f), 12, Color.white);
                var pvT = CoastHudLayout.MakeText(pr, "T", preview, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));
                pvT.color = Color.white; pvT.fontStyle = FontStyle.Bold; pvT.raycastTarget = false;
                pvT.horizontalOverflow = HorizontalWrapMode.Wrap; pvT.verticalOverflow = VerticalWrapMode.Truncate;
                pvT.resizeTextForBestFit = true; pvT.resizeTextMinSize = 11; pvT.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            }

            var btn = card.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            return btn;
        }

        /// 선택 화면 라벤더 바탕에 꽃·나비 장식(시안).
        public static void DecoratePickBg(RectTransform root)
        {
            Color flower = new Color(1f, 0.70f, 0.80f, 0.55f);
            Color butterfly = new Color(0.62f, 0.52f, 0.88f, 0.42f);
            Vector2[] flowerPos = {
                new Vector2(0.12f, 0.88f), new Vector2(0.88f, 0.84f), new Vector2(0.18f, 0.55f),
                new Vector2(0.82f, 0.48f), new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.18f),
                new Vector2(0.50f, 0.92f), new Vector2(0.72f, 0.70f)
            };
            for (int i = 0; i < flowerPos.Length; i++)
            {
                var a = flowerPos[i];
                for (int p = 0; p < 5; p++)
                {
                    float ang = p * 72f * Mathf.Deg2Rad;
                    var petal = CoastHudLayout.MakeImage(root, "Fl", a, a,
                        new Vector2(Mathf.Cos(ang) * 8f - 5f, Mathf.Sin(ang) * 8f - 5f),
                        new Vector2(Mathf.Cos(ang) * 8f + 5f, Mathf.Sin(ang) * 8f + 5f), flower);
                    petal.raycastTarget = false; petal.sprite = CoastUiArt.RoundedRect(16);
                }
            }
            Vector2[] bfPos = { new Vector2(0.28f, 0.78f), new Vector2(0.70f, 0.62f), new Vector2(0.35f, 0.30f), new Vector2(0.65f, 0.25f) };
            for (int i = 0; i < bfPos.Length; i++)
            {
                var a = bfPos[i];
                var L = CoastHudLayout.MakeImage(root, "BfL", a, a, new Vector2(-14f, -6f), new Vector2(-1f, 8f), butterfly);
                L.raycastTarget = false; L.sprite = CoastUiArt.RoundedRect(20);
                var R = CoastHudLayout.MakeImage(root, "BfR", a, a, new Vector2(1f, -6f), new Vector2(14f, 8f), butterfly);
                R.raycastTarget = false; R.sprite = CoastUiArt.RoundedRect(20);
            }
            for (int si = 0; si < 8; si++)
            {
                float sx = 0.08f + (si * 41 % 85) / 100f, sy = 0.10f + (si * 57 % 80) / 100f;
                Sparkle(root, new Vector2(sx, sy), Vector2.zero, si % 2 == 0 ? 16 : 11, new Color(1f, 1f, 1f, 0.55f));
            }
        }

        public static void PickStatusPills(RectTransform root, string money, string hearts, string clock)
        {
            // 136차(사용자 시안): 큰 알약 3개가 위 폭을 꽉 채움 — 테두리 + 왼쪽 큰 아이콘 원 + 굵고 큰 숫자
            void Pill(float x, string iconRes, string glyph, string text, Color fill, Color edge, Color iconBg)
            {
                var e = CoastUiArt.CutePill(root, "StatE", edge, 24, 0); e.raycastTarget = false;
                var er = e.rectTransform; er.anchorMin = er.anchorMax = new Vector2(0.5f, 1f); er.pivot = new Vector2(0.5f, 1f);
                er.anchoredPosition = new Vector2(x, -12f); er.sizeDelta = new Vector2(214f, 80f);
                var pill = CoastUiArt.CutePill(root, "Stat", fill, 22, 0);
                var pr = pill.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f); pr.pivot = new Vector2(0.5f, 1f);
                pr.anchoredPosition = new Vector2(x, -16f); pr.sizeDelta = new Vector2(206f, 72f); pill.raycastTarget = false;
                var ic = CoastUiArt.GlossyPill(pr, "Ic", iconBg, 26, 6); ic.raycastTarget = false;
                var ir = ic.rectTransform; ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f); ir.pivot = new Vector2(0f, 0.5f);
                ir.anchoredPosition = new Vector2(8f, 0f); ir.sizeDelta = new Vector2(54f, 54f);
                var sp = iconRes != null ? CoastUiArt.Icon(iconRes) : null;
                if (sp != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(ir, false); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
                    im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.sizeDelta = new Vector2(38f, 38f);
                }
                else
                {
                    var it = CoastHudLayout.MakeText(ir, "T", glyph, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
                    it.color = Color.white; it.fontStyle = FontStyle.Bold; it.raycastTarget = false;
                }
                var tx = CoastHudLayout.MakeText(pr, "V", text, 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(66f, 0f), new Vector2(-12f, 0f));
                tx.color = BrownInk; tx.fontStyle = FontStyle.Bold; tx.raycastTarget = false;
                tx.resizeTextForBestFit = true; tx.resizeTextMinSize = 14; tx.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            }
            Pill(-226f, "Coin", "●", money, new Color(1f, 0.96f, 0.82f), new Color(0.95f, 0.78f, 0.45f), new Color(0.98f, 0.78f, 0.28f));
            Pill(0f, "Heart", "♥", hearts, new Color(1f, 0.90f, 0.94f), new Color(0.96f, 0.62f, 0.75f), new Color(0.95f, 0.45f, 0.58f));
            Pill(226f, null, "◷", clock, new Color(0.92f, 0.88f, 0.99f), new Color(0.70f, 0.58f, 0.92f), new Color(0.62f, 0.48f, 0.90f));
        }
    }
}
