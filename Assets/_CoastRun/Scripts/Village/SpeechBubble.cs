using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 146차: 월드 스페이스 말풍선 — 머리 위에 떠서 카메라를 보고(세로축만), 꼬리가 말하는 이를 가리키며,
    /// 열릴 때 통통 커지고 말하는 동안 위아래로 둥실거린다.
    public class SpeechBubble : MonoBehaviour
    {
        public Transform Anchor; public float Height = 1.55f;
        Canvas _cv; RectTransform _rt, _body, _tail, _tailEdge; Text _text; float _t, _open; bool _shown; string _pending;
        // 217차(마감 B2): 화면 가장자리에서 잘리지 않게 안쪽으로 밀고, 겹치면 먼 쪽 말풍선을 위로 쌓는다
        static readonly System.Collections.Generic.List<SpeechBubble> _all = new System.Collections.Generic.List<SpeechBubble>();
        static int _seq; readonly int _id = ++_seq; Rect _scr; float _camDist; bool _hasScr; float _lift, _shift, _mult = 1f, _multW = 1f;
        void OnEnable() { if (!_all.Contains(this)) _all.Add(this); }
        void OnDisable() { _all.Remove(this); _hasScr = false; }

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
            var ert = edge.GetComponent<RectTransform>(); b._tailEdge = ert; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0f); ert.sizeDelta = new Vector2(28f, 28f); ert.anchoredPosition = new Vector2(0f, 13f); ert.localRotation = Quaternion.Euler(0f, 0f, 45f);
            b._text = CoastHudLayout.MakeText(b._body, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -2f));
            b._text.color = new Color(0.30f, 0.25f, 0.40f); b._text.fontStyle = FontStyle.Bold; b._text.raycastTarget = false;
            b._text.horizontalOverflow = HorizontalWrapMode.Wrap; b._text.verticalOverflow = VerticalWrapMode.Truncate;   // 217차: 긴 대사는 줄바꿈(한 줄에 우겨 넣어 깨알 글씨)
            b._text.resizeTextForBestFit = true; b._text.resizeTextMinSize = 10; b._text.resizeTextMaxSize = CoastHudLayout.Scaled(20);
            b._rt.localScale = Vector3.zero; b._shown = false; go.SetActive(false);
            return b;
        }

        public bool Visible => _shown;
        float _baseH = -1f;
        public void Show(string text)
        {
            _text.text = text;
            // 217차(마감 B11): 긴 대사는 두 줄 높이로 — 한 줄에 우겨 넣느라 글씨가 깨알만 해지던 것
            if (_baseH < 0f) _baseH = _rt.sizeDelta.y;
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.x, text != null && text.Length > 16 ? _baseH + 40f : _baseH);
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
            var cam = Camera.main;
            if (cam != null) { var f = cam.transform.forward; f.y = 0f; if (f.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 1.3f) * 1.5f); }
            float shiftW = 0f, liftW = 0f;
            if (cam != null && _open > 0.2f) Fit(cam, pos, out shiftW, out liftW);
            float kf = 1f - Mathf.Exp(-dt * 10f);
            _shift = Mathf.Lerp(_shift, shiftW, kf); _lift = Mathf.Lerp(_lift, liftW, kf); _mult = Mathf.Lerp(_mult, _multW, kf);
            s *= _mult;
            pos += transform.right * _shift + Vector3.up * _lift;
            // 꼬리는 말하는 이를 계속 가리키게 몸통이 밀린 만큼 반대로
            float tx = Mathf.Clamp(-_shift / (0.01f * _mult), -_rt.sizeDelta.x * 0.5f + 34f, _rt.sizeDelta.x * 0.5f - 34f);
            if (_tail != null) _tail.anchoredPosition = new Vector2(tx, 14f);
            if (_tailEdge != null) _tailEdge.anchoredPosition = new Vector2(tx, 13f);
            transform.position = pos;
            _rt.localScale = new Vector3(s, s, s);
        }

        /// 기본 위치(pos, 크기 0.01 기준)의 화면 사각형을 구해 ① 화면 좌우 밖이면 안쪽으로 밀 거리 ② 더 가까운 말풍선과 겹치면 위로 올릴 높이(월드 m)
        void Fit(Camera cam, Vector3 pos, out float shiftW, out float liftW)
        {
            shiftW = 0f; liftW = 0f;
            float w = _rt.sizeDelta.x * 0.01f, h = _rt.sizeDelta.y * 0.01f;
            var right = transform.right;
            var a = cam.WorldToScreenPoint(pos - right * (w * 0.5f)); var b = cam.WorldToScreenPoint(pos + right * (w * 0.5f) + Vector3.up * h);
            if (a.z <= 0f || b.z <= 0f) { _hasScr = false; return; }
            float x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x), y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            float ppmX = (x1 - x0) / Mathf.Max(0.01f, w), ppmY = (y1 - y0) / Mathf.Max(0.01f, h);   // 월드 1 m 당 화면 px
            // 217차(마감 B11): 멀리 있어 글씨가 안 읽히면 화면 폭 50 % 까지 키운다(최대 2.6배)
            _multW = Mathf.Clamp(Screen.width * 0.5f / Mathf.Max(1f, x1 - x0), 1f, 2.6f);
            { float cx = (x0 + x1) * 0.5f, half = (x1 - x0) * 0.5f * _multW; x0 = cx - half; x1 = cx + half; y1 = y0 + (y1 - y0) * _multW; }
            float margin = Screen.width * 0.02f, dx = 0f;
            if (x1 - x0 < Screen.width - margin * 2f)
            {
                if (x0 < margin) dx = margin - x0; else if (x1 > Screen.width - margin) dx = Screen.width - margin - x1;
            }
            shiftW = dx / Mathf.Max(1f, ppmX);
            x0 += dx; x1 += dx;
            _camDist = (pos - cam.transform.position).magnitude;
            // 가까운(카메라 쪽) 말풍선은 그대로, 먼 쪽이 그 위로 올라간다 — 여러 개면 차례로 쌓인다
            float top = y0, hh = y1 - y0;
            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                foreach (var o in _all)
                {
                    if (o == this || !o._hasScr || !o._shown) continue;
                    if (o._camDist > _camDist || (Mathf.Approximately(o._camDist, _camDist) && o._id > _id)) continue;
                    var r = o._scr;
                    if (x1 > r.xMin + 4f && x0 < r.xMax - 4f && top + hh > r.yMin + 2f && top < r.yMax - 2f) { top = r.yMax + 4f; moved = true; }
                }
                if (!moved) break;
            }
            liftW = (top - y0) / Mathf.Max(1f, ppmY);
            _scr = new Rect(x0, top, x1 - x0, hh); _hasScr = true;
        }
    }
}
