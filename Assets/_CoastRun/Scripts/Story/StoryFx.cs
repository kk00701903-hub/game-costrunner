using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Story
{
    /// 240차(그래픽 업그레이드): 스토리 화면(시네마틱 · VN)이 같이 쓰는 그림 재료.
    ///   전부 코드로 만든 작은 텍스처라 그림 파일이 늘지 않는다 — 부드러운 빛 알갱이, 세로 그라데이션, 가는 장식 선.
    public static class StoryFx
    {
        static Sprite _dot, _gradUp, _gradDown, _line, _vig;

        /// 가운데가 밝고 가장자리로 사라지는 둥근 빛(64px). 예전 「둥근 사각형 점」은 그림 위에서 회색 먼지처럼 보였다.
        public static Sprite SoftDot()
        {
            if (_dot != null) return _dot;
            const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                float core = Mathf.Clamp01(1f - d / 0.28f);            // 작은 밝은 심
                float halo = Mathf.Pow(1f - d, 2.4f);                  // 넓은 번짐
                float a = Mathf.Clamp01(core * 0.75f + halo * 0.55f);
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return _dot = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// 세로 그라데이션(흰색, 알파만). up=true 면 아래가 진하고 위로 사라진다.
        public static Sprite VGradient(bool up)
        {
            if (up && _gradUp != null) return _gradUp;
            if (!up && _gradDown != null) return _gradDown;
            const int h = 128; var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < h; y++)
            {
                float v = (y + 0.5f) / h; if (up) v = 1f - v;          // v: 0 = 사라지는 쪽, 1 = 진한 쪽
                float a = Mathf.SmoothStep(0f, 1f, v); a = Mathf.Pow(a, 0.85f);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply(false, true);
            var sp = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            if (up) _gradUp = sp; else _gradDown = sp;
            return sp;
        }

        /// 양 끝이 사라지는 가는 가로 선(장식 · 진행 표시).
        public static Sprite FadeLine()
        {
            if (_line != null) return _line;
            const int w = 128; var tex = new Texture2D(w, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w; float a = Mathf.Clamp01(Mathf.Sin(u * Mathf.PI)); a = Mathf.Pow(a, 0.6f);
                tex.SetPixel(x, 0, new Color(1f, 1f, 1f, a)); tex.SetPixel(x, 1, new Color(1f, 1f, 1f, a));
            }
            tex.Apply(false, true);
            return _line = Sprite.Create(tex, new Rect(0, 0, w, 2), new Vector2(0.5f, 0.5f), 100f);
        }

        /// 부드러운 비네트(256px) — 가운데는 완전히 비고 모서리로 갈수록 천천히 어두워진다.
        public static Sprite Vignette()
        {
            if (_vig != null) return _vig;
            const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;             // 0 중심 ~ 1.41 모서리
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.50f) / 0.85f));
                // 8비트 띠가 보이지 않게 아주 약한 디더
                a += ((x * 7 + y * 13) % 5 - 2) * (0.5f / 255f);
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return _vig = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// 부모를 꽉 채우는(여백 지정) 클릭 통과 이미지.
        public static Image Fill(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false; img.type = Image.Type.Simple;
            return img;
        }

        /// 가운데 정렬 가는 선(폭 w, 두께 h) — 제목 카드 위아래 장식.
        public static Image Line(Transform parent, string name, Vector2 anchor, Vector2 pos, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>(); img.sprite = FadeLine(); img.color = color; img.raycastTarget = false;
            return img;
        }

        /// 스토리 그림용 재질 값 — 또렷하게 · 밝은 곳 번짐 · 필름 톤.
        public static void ApplyPicture(Material m, float feather)
        {
            if (m == null) return;
            m.SetFloat("_Sharp", 0.75f);
            m.SetFloat("_Glow", 0.22f);
            m.SetFloat("_Grade", 0.7f);
            m.SetFloat("_Feather", feather);
            m.SetFloat("_Blur", 0f);
        }

        /// 그림 뒤를 채우는 흐린 배경용 재질 값.
        public static void ApplyBackdrop(Material m)
        {
            if (m == null) return;
            m.SetFloat("_Sharp", 0f); m.SetFloat("_Glow", 0f); m.SetFloat("_Grade", 0.5f); m.SetFloat("_Feather", 0f);
            m.SetFloat("_Blur", 0.045f);
        }

        public static Shader PictureShader()
        {
            var s = Shader.Find("CoastRun/UIDesaturate");
            if (s == null) s = Resources.Load<Shader>("CoastRun/Shaders/UIDesaturate");
            return s;
        }
    }
}
