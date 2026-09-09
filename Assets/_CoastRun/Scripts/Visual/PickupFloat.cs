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
            // 23차-2: 오버레이 캔버스 루트는 화면에 고정돼 회전이 안 먹는다 → 흔들림용 자식 컨테이너
            var fx = new GameObject("Fx", typeof(RectTransform)).GetComponent<RectTransform>();
            fx.SetParent(_root, false); fx.anchorMin = Vector2.zero; fx.anchorMax = Vector2.one; fx.offsetMin = Vector2.zero; fx.offsetMax = Vector2.zero;
            _fx = fx;
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

        // 23차-9: 화면 위쪽 배너(FEVER!) — 지속 시간 동안 흔들리며 떠 있다가 사라진다
        private Text _bannerText;
        public static void Banner(string text, Color color, float seconds)
        {
            var f = Ensure();
            f.StartCoroutine(f.BannerSeq(text, color, seconds));
        }
        private IEnumerator BannerSeq(string text, Color color, float seconds)
        {
            if (_bannerText == null)
            {
                _bannerText = CoastHudLayout.MakeText(_root, "Banner", "", 96, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400f, -330f), new Vector2(400f, -200f));
                _bannerText.fontStyle = FontStyle.Bold; _bannerText.raycastTarget = false;
                CoastUiArt.OutlineText(_bannerText, new Color(0.35f, 0.12f, 0.02f, 1f), 4f);
            }
            _bannerText.gameObject.SetActive(true);
            _bannerText.text = text;
            var rt = _bannerText.rectTransform;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float u = t / seconds;
                float pop = t < 0.15f ? Mathf.Lerp(1.8f, 1f, t / 0.15f) : 1f + Mathf.Sin(t * 9f) * 0.05f;
                rt.localScale = Vector3.one * pop;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 4f);
                var c = Color.Lerp(color, Color.white, (Mathf.Sin(t * 14f) + 1f) * 0.25f);
                c.a = u > 0.85f ? 1f - (u - 0.85f) / 0.15f : 1f;
                _bannerText.color = c;
                yield return null;
            }
            _bannerText.gameObject.SetActive(false);
        }

        // 23차-2: 꽈당 — 붉은 비네트 플래시 + 화면 기울기 + 큰 글자
        private Image _vignette, _flash; private Text _slam; private RectTransform _fx;
        public static void Impact(string word)
        {
            var f = Ensure();
            f.StartCoroutine(f.ImpactSeq(word));
        }

        private IEnumerator ImpactSeq(string word)
        {
            if (_vignette == null)
            {
                _vignette = MakeFull("HitVignette", VignetteTex());
                _flash = MakeFull("HitFlash", null);
                _slam = CoastHudLayout.MakeText(_fx, "Slam", "", 120, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -120f), new Vector2(400f, 120f));
                _slam.fontStyle = FontStyle.Bold; _slam.raycastTarget = false;
                CoastUiArt.OutlineText(_slam, new Color(0.35f, 0.02f, 0.05f, 1f), 5f);
            }
            _vignette.gameObject.SetActive(true); _flash.gameObject.SetActive(true); _slam.gameObject.SetActive(true);
            _slam.text = word;
            var srt = _slam.rectTransform;
            float t = 0f; const float dur = 0.55f;
            Quaternion r0 = _fx.localRotation; Vector2 p0 = _fx.anchoredPosition;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                // 흰 번쩍(0.06 s) → 붉은 비네트가 남았다가 사라진다
                _flash.color = new Color(1f, 0.95f, 0.9f, Mathf.Clamp01(1f - t / 0.06f) * 0.7f);
                _vignette.color = new Color(0.9f, 0.05f, 0.08f, (1f - u) * (1f - u) * 0.85f);
                // 화면 전체가 기우뚱(감쇠 진동)
                float wob = Mathf.Sin(t * 42f) * (1f - u) * (1f - u) * 5f;
                _fx.localRotation = Quaternion.Euler(0f, 0f, wob);
                _fx.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 60f) * 22f, Mathf.Cos(t * 50f) * 14f) * (1f - u) * (1f - u);
                // 글자: 크게 튀어나왔다가 자리 잡고 흐려진다
                float pop = u < 0.12f ? Mathf.Lerp(2.2f, 0.95f, u / 0.12f) : Mathf.Lerp(0.95f, 1.05f, (u - 0.12f) / 0.88f);
                srt.localScale = Vector3.one * pop;
                srt.localRotation = Quaternion.Euler(0f, 0f, -9f + wob * 0.5f);
                srt.anchoredPosition = new Vector2(0f, 140f + 40f * u);
                _slam.color = new Color(1f, 0.92f, 0.3f, u < 0.65f ? 1f : 1f - (u - 0.65f) / 0.35f);
                yield return null;
            }
            _fx.localRotation = r0; _fx.anchoredPosition = p0;
            _vignette.gameObject.SetActive(false); _flash.gameObject.SetActive(false); _slam.gameObject.SetActive(false);
        }

        private Image MakeFull(string name, Texture2D tex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_fx, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            if (tex != null) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(-80f, -80f); rt.offsetMax = new Vector2(80f, 80f);
            img.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);
            return img;
        }

        private static Texture2D _vig;
        private static Texture2D VignetteTex()
        {
            if (_vig != null) return _vig;
            const int N = 128; _vig = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float px = (x + 0.5f) / N * 2f - 1f, py = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(px * px * 0.8f + py * py * 0.55f);
                float a = Mathf.Clamp01((r - 0.35f) / 0.6f); a = a * a;
                _vig.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _vig.Apply(); return _vig;
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
