using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 72차(사용자 시안): K-POP 러닝 우하단 「NOW PLAYING ♫ / 곡명 — 우히&히시」 알약.
    ///   보라→분홍 가로 그라데이션(둥근 모서리, 흰 테두리) + 왼쪽 어두운 사각 안 이퀄라이저 막대 5개(청록→파랑) + 오른쪽 반짝이.
    ///   막대는 AudioListener 스펙트럼(저역~중역 5밴드)을 따라 멜로디에 맞춰 오르락내리락 — 공격은 빠르게, 감쇠는 천천히.
    public class KpopNowPlaying : MonoBehaviour
    {
        private readonly RectTransform[] _bars = new RectTransform[5];
        private readonly float[] _level = new float[5];
        private readonly float[] _spec = new float[256];
        private Text _spark; private float _t;
        private const float BarMin = 6f, BarMax = 30f;

        public static KpopNowPlaying Build(RectTransform root, string credit)
        {
            const float W = 262f, H = 60f;
            var go = new GameObject("NowPlaying", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-6f, 10f); rt.sizeDelta = new Vector2(W, H);
            var bg = go.GetComponent<Image>();
            bg.sprite = GradientPill(Mathf.RoundToInt(W), Mathf.RoundToInt(H), 18, new Color(0.50f, 0.32f, 0.92f), new Color(0.96f, 0.38f, 0.72f), 3);
            bg.raycastTarget = false;
            var np = go.AddComponent<KpopNowPlaying>();

            // 왼쪽 이퀄라이저 상자
            var box = CoastUiArt.Panel(rt, "EqBox", new Color(0.09f, 0.08f, 0.24f, 0.95f), 10);
            var brt = box.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0f, 0.5f); brt.pivot = new Vector2(0f, 0.5f);
            brt.anchoredPosition = new Vector2(9f, 0f); brt.sizeDelta = new Vector2(44f, 44f);
            for (int i = 0; i < 5; i++)
            {
                var bar = CoastUiArt.Panel(brt, "Bar" + i, Color.Lerp(new Color(0.35f, 0.95f, 1f), new Color(0.30f, 0.45f, 1f), i / 4f), 2);
                var r = bar.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0f, 0f); r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(8f + i * 7f, 6f); r.sizeDelta = new Vector2(4.5f, BarMin);
                np._bars[i] = r;
            }

            // 글자 두 줄
            var top = CoastHudLayout.MakeText(rt, "Top", "NOW PLAYING ♫", 10, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(62f, -2f), new Vector2(-30f, -6f));
            top.color = new Color(1f, 0.95f, 1f); top.fontStyle = FontStyle.Bold; top.resizeTextForBestFit = true; top.resizeTextMinSize = 8; top.resizeTextMaxSize = CoastHudLayout.Scaled(10);
            CoastUiArt.OutlineText(top, new Color(0.30f, 0.10f, 0.45f, 0.8f), 1f);
            var title = CoastHudLayout.MakeText(rt, "Title", credit, 13, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(62f, 5f), new Vector2(-18f, 2f));
            title.color = Color.white; title.fontStyle = FontStyle.Bold; title.resizeTextForBestFit = true; title.resizeTextMinSize = 9; title.resizeTextMaxSize = CoastHudLayout.Scaled(13);
            title.horizontalOverflow = HorizontalWrapMode.Wrap; title.verticalOverflow = VerticalWrapMode.Truncate;
            CoastUiArt.OutlineText(title, new Color(0.30f, 0.10f, 0.45f, 0.9f), 1.2f);

            // 오른쪽 위 반짝이
            np._spark = CoastHudLayout.MakeText(rt, "Spark", "✦", 13, TextAnchor.MiddleCenter, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -26f), new Vector2(-6f, -4f));
            np._spark.color = new Color(1f, 1f, 1f, 0.95f);
            return np;
        }

        private void Update()
        {
            AudioListener.GetSpectrumData(_spec, 0, FFTWindow.BlackmanHarris);
            // 5밴드: 저역(킥) ~ 중고역(보컬) — 빈 인덱스는 44.1k/256 ≈ 86Hz 간격
            int[] lo = { 1, 3, 6, 12, 24 }, hi = { 3, 6, 12, 24, 48 };
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 5; i++)
            {
                float sum = 0f; for (int k = lo[i]; k < hi[i]; k++) sum += _spec[k];
                float v = Mathf.Clamp01(Mathf.Sqrt(sum / (hi[i] - lo[i])) * (2.2f + i * 0.6f));
                _level[i] = v > _level[i] ? Mathf.Lerp(_level[i], v, dt * 22f) : Mathf.Lerp(_level[i], v, dt * 5f);
                // 음악이 없어도 살짝 숨 쉰다
                float idle = 0.08f + 0.06f * Mathf.Sin(Time.unscaledTime * (3f + i) + i);
                float h = Mathf.Lerp(BarMin, BarMax, Mathf.Max(_level[i], idle));
                if (_bars[i] != null) _bars[i].sizeDelta = new Vector2(4.5f, h);
            }
            _t += dt;
            if (_spark != null) { float s = 0.75f + 0.25f * Mathf.Sin(_t * 4f); _spark.transform.localScale = Vector3.one * s; _spark.color = new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(_t * 4f + 1f)); }
        }

        /// 가로 그라데이션 둥근 알약 스프라이트(흰 테두리 포함). 크기 그대로 그린다(9-slice 아님).
        private static Sprite GradientPill(int w, int h, int radius, Color a, Color b, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = x < radius ? radius - 0.5f : (x >= w - radius ? w - radius - 0.5f : x);
                    float cy = y < radius ? radius - 0.5f : (y >= h - radius ? h - radius - 0.5f : y);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float aa = Mathf.Clamp01(radius - d + 0.5f);
                    if (aa <= 0f) { tex.SetPixel(x, y, clear); continue; }
                    bool edge = d > radius - border - 0.5f || x < border || x >= w - border || y < border || y >= h - border;
                    Color c = edge ? new Color(1f, 1f, 1f, 0.95f) : Color.Lerp(a, b, x / (float)(w - 1));
                    // 윗광택
                    if (!edge && y > h * 0.55f) c = Color.Lerp(c, Color.white, 0.10f);
                    c.a *= aa;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
