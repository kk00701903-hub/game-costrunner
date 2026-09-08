using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 22차-3: 픽업 타격감 — 화면 위 '+N' 플로팅 텍스트와 HUD 코인 칸으로 날아가는 코인.
    /// 자체 오버레이 캔버스(HUD보다 위, 레이캐스트 없음). 스케일러는 HUD와 같은 1080×1920/0.5.
    public class PickupFloat : MonoBehaviour
    {
        private static PickupFloat _inst;
        private Canvas _canvas;
        private RectTransform _root;
        private readonly Stack<Text> _pool = new Stack<Text>();
        private readonly Stack<Image> _coinPool = new Stack<Image>();
        private Sprite _coinSprite;

        public static PickupFloat Ensure()
        {
            if (_inst != null) return _inst;
            var go = new GameObject("PickupFloat");
            _inst = go.AddComponent<PickupFloat>();
            _inst.Build();
            return _inst;
        }

        private void Build()
        {
            _canvas = CoastUiCanvas.Create("PickupFloatCanvas", 60, transform);
            var gr = _canvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
            _root = _canvas.GetComponent<RectTransform>();
            var tex = PaintedProp.Load("Coin_Gold");
            if (tex != null) _coinSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private void OnDestroy() { if (_inst == this) _inst = null; }

        /// 월드 위치 → 캔버스 로컬 좌표.
        private Vector2 ToCanvas(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            Vector2 sp = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, null, out var local);
            return local;
        }

        public static void Text(Vector3 world, string text, Color color, float size = 1f)
        {
            var f = Ensure();
            f.StartCoroutine(f.FloatText(world, text, color, size));
        }

        /// 코인이 화면 위 HUD 코인 숫자 쪽으로 날아간다(있으면).
        public static void FlyCoin(Vector3 world, int count = 1)
        {
            var f = Ensure();
            for (int i = 0; i < count; i++)
                f.StartCoroutine(f.FlyToHud(world, i * 0.05f));
        }

        private IEnumerator FloatText(Vector3 world, string text, Color color, float size)
        {
            Text t = _pool.Count > 0 ? _pool.Pop() : MakeText();
            t.gameObject.SetActive(true);
            t.text = text; t.color = color;
            t.fontSize = Mathf.RoundToInt(52 * size);
            var rt = t.rectTransform;
            Vector2 start = ToCanvas(world) + new Vector2(Random.Range(-18f, 18f), 110f);
            float dur = 0.62f, time = 0f;
            while (time < dur)
            {
                time += Time.unscaledDeltaTime;
                float u = time / dur;
                float pop = u < 0.18f ? Mathf.Lerp(0.4f, 1.25f, u / 0.18f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((u - 0.18f) / 0.2f));
                rt.localScale = Vector3.one * pop;
                rt.anchoredPosition = start + new Vector2(0f, 150f * u);
                var c = color; c.a = u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;
                t.color = c;
                yield return null;
            }
            t.gameObject.SetActive(false);
            _pool.Push(t);
        }

        private Text MakeText()
        {
            var t = CoastHudLayout.MakeText(_root, "Float", "+1", 44, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -40f), new Vector2(120f, 40f));
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            CoastUiArt.OutlineText(t, new Color(0.05f, 0.05f, 0.15f, 0.95f), 2.5f);
            return t;
        }

        private IEnumerator FlyToHud(Vector3 world, float delay)
        {
            if (_coinSprite == null) yield break;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            var hud = RunHudChrome.Instance;
            RectTransform target = hud != null && hud.CoinText != null ? hud.CoinText.rectTransform : null;
            if (target == null) yield break;
            Image img = _coinPool.Count > 0 ? _coinPool.Pop() : MakeCoin();
            img.gameObject.SetActive(true);
            var rt = img.rectTransform;
            Vector2 from = ToCanvas(world);
            // HUD 코인 숫자의 월드 → 이 캔버스 로컬
            Vector3[] corners = new Vector3[4]; target.GetWorldCorners(corners);
            Vector3 tw = (corners[0] + corners[2]) * 0.5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, RectTransformUtility.WorldToScreenPoint(null, tw), null, out var to);
            Vector2 ctrl = (from + to) * 0.5f + new Vector2(Random.Range(-140f, 140f), 160f);
            float dur = 0.42f, time = 0f;
            while (time < dur)
            {
                time += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(time / dur); float e = u * u * (3f - 2f * u);
                Vector2 p = (1 - e) * (1 - e) * from + 2 * (1 - e) * e * ctrl + e * e * to;
                rt.anchoredPosition = p;
                rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.55f, e);
                rt.localRotation = Quaternion.Euler(0f, 0f, 360f * e);
                yield return null;
            }
            img.gameObject.SetActive(false);
            _coinPool.Push(img);
            if (hud != null && hud.CoinText != null)
                hud.StartCoroutine(SimpleTween.PunchScale(hud.CoinText.transform, 0.22f, 0.14f));
        }

        private Image MakeCoin()
        {
            var go = new GameObject("FlyCoin", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var img = go.GetComponent<Image>();
            img.sprite = _coinSprite; img.raycastTarget = false; img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(64f, 64f);
            return img;
        }
    }
}
