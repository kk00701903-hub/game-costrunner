using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 146차: 월드 스페이스 말풍선 — 머리 위에 떠서 카메라를 보고(세로축만), 꼬리가 말하는 이를 가리키며,
    /// 열릴 때 통통 커지고 말하는 동안 위아래로 둥실거린다.
    public class SpeechBubble : MonoBehaviour
    {
        public Transform Anchor; public float Height = 1.55f;
        Canvas _cv; RectTransform _rt, _body, _tail; Text _text; float _t, _open; bool _shown; string _pending;

        public static SpeechBubble Create(Transform anchor, float height, float width = 300f, Color? fill = null)
        {
            var go = new GameObject("SpeechBubble", typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(anchor, false);
            var b = go.AddComponent<SpeechBubble>(); b.Anchor = anchor; b.Height = height;
            b._cv = go.GetComponent<Canvas>(); b._cv.renderMode = RenderMode.WorldSpace; b._cv.sortingOrder = 6;
            b._rt = go.GetComponent<RectTransform>(); b._rt.sizeDelta = new Vector2(width, 76f); b._rt.localScale = Vector3.one * 0.01f; b._rt.pivot = new Vector2(0.5f, 0f);
            var col = fill ?? new Color(1f, 1f, 1f, 0.94f);
            // 몸통(알약) — 아래 16 px 은 꼬리 자리
            var body = CoastUiArt.CutePill(b._rt, "Body", col, 22, 3); body.raycastTarget = false;
            b._body = body.rectTransform; b._body.anchorMin = new Vector2(0f, 0f); b._body.anchorMax = new Vector2(1f, 1f);
            b._body.offsetMin = new Vector2(0f, 16f); b._body.offsetMax = Vector2.zero;
            // 꼬리: 45° 돌린 작은 사각형(같은 색), 몸통 아래 중앙에서 말하는 이를 향해 아래로
            var tailGo = new GameObject("Tail", typeof(RectTransform), typeof(Image)); tailGo.transform.SetParent(b._rt, false);
            var tail = tailGo.GetComponent<Image>(); tail.color = col; tail.raycastTarget = false; tail.sprite = CoastUiArt.RoundedRect(4);
            b._tail = tailGo.GetComponent<RectTransform>(); b._tail.anchorMin = b._tail.anchorMax = new Vector2(0.5f, 0f);
            b._tail.sizeDelta = new Vector2(22f, 22f); b._tail.anchoredPosition = new Vector2(0f, 14f); b._tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var edge = new GameObject("TailEdge", typeof(RectTransform), typeof(Image)); edge.transform.SetParent(b._rt, false); edge.transform.SetAsFirstSibling();
            var ei = edge.GetComponent<Image>(); ei.color = CoastUiArt.CreamOutline; ei.raycastTarget = false; ei.sprite = CoastUiArt.RoundedRect(4);
            var ert = edge.GetComponent<RectTransform>(); ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0f); ert.sizeDelta = new Vector2(28f, 28f); ert.anchoredPosition = new Vector2(0f, 13f); ert.localRotation = Quaternion.Euler(0f, 0f, 45f);
            b._text = CoastHudLayout.MakeText(b._body, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -2f));
            b._text.color = new Color(0.30f, 0.25f, 0.40f); b._text.fontStyle = FontStyle.Bold; b._text.raycastTarget = false;
            b._text.resizeTextForBestFit = true; b._text.resizeTextMinSize = 10; b._text.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            b._rt.localScale = Vector3.zero; b._shown = false; go.SetActive(false);
            return b;
        }

        public bool Visible => _shown;
        public void Show(string text)
        {
            _text.text = text;
            if (!_shown) { gameObject.SetActive(true); _shown = true; _open = 0f; }
        }
        public void Hide() { _shown = false; }

        void LateUpdate()
        {
            if (Anchor == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            // 열림/닫힘: 튕기는 스케일(오버슈트)
            _open = Mathf.MoveTowards(_open, _shown ? 1f : 0f, dt * 6f);
            float e = _shown ? 1f + Mathf.Sin(_open * Mathf.PI) * 0.18f : 1f;
            float s = Mathf.SmoothStep(0f, 1f, _open) * e * 0.01f;
            if (!_shown && _open <= 0.001f) { gameObject.SetActive(false); return; }
            // 둥실: 머리 위에서 위아래 4 cm, 아주 살짝 좌우 기울기
            float bob = Mathf.Sin(_t * 2.6f) * 0.04f;
            var pos = Anchor.position + new Vector3(0f, Height + bob, 0f);
            // 곡면 월드 보정: 셰이더가 지면·캐릭터를 원점에서 멀수록 아래로 휘므로 UI 도 같은 만큼 내린다
            var cr = Shader.GetGlobalVector("_CoastCurveRadial");
            if (cr.x > 0f) { float dr = Mathf.Min(new Vector2(pos.x - cr.y, pos.z - cr.z).magnitude, cr.w); pos.y -= cr.x * dr * dr; }
            transform.position = pos;
            var cam = Camera.main;
            if (cam != null) { var f = cam.transform.forward; f.y = 0f; if (f.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 1.3f) * 1.5f); }
            _rt.localScale = new Vector3(s, s, s);
        }
    }
}
