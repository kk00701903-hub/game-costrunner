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
            if (_inst != null)
            {
                if (_inst._canvas != null)
                {
                    _inst._canvas.enabled = true;
                    if (_inst._canvas.sortingOrder < 560)
                        _inst._canvas.sortingOrder = 560;
                }
                return _inst;
            }
            var go = new GameObject("PickupFloat");
            _inst = go.AddComponent<PickupFloat>();
            _inst.Build();
            return _inst;
        }

        /// 42차: PickupFloat 오브젝트를 host 가 있는 씬으로 옮긴다.
        /// 런 씬은 타이틀 위에 additive 로 미리 로드되므로, 「챕터 N 시작!」을 페이드 중에 띄우면
        /// PickupFloat 이 그때의 활성 씬(타이틀)에 만들어졌다가 타이틀 언로드와 함께 사라졌다(카드가 0.5초 만에 끊김).
        public static void BindToScene(GameObject host)
        {
            var f = Ensure();
            if (f == null || host == null) return;
            var target = host.scene;
            if (!target.IsValid() || !target.isLoaded || f.gameObject.scene == target) return;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(f.gameObject, target);
        }

        /// 스테이지 클리어 등 — 떠 있는 +N·날아가는 코인·배너를 전부 치우고 캔버스를 끈다.
        public static void ClearAll()
        {
            if (_inst == null) return;
            var self = _inst;
            self.StopAllCoroutines();
            self._goCo = null;
            self._impactCo = null;
            for (int i = self._live.Count - 1; i >= 0; i--)
                self.RecycleFloat(self._live[i].t);
            self._live.Clear();
            if (self._goText != null) self._goText.gameObject.SetActive(false);
            if (self._goSub != null) self._goSub.gameObject.SetActive(false);
            if (self._bannerText != null) self._bannerText.gameObject.SetActive(false);
            if (self._slam != null) self._slam.gameObject.SetActive(false);
            if (self._vignette != null) self._vignette.gameObject.SetActive(false);
            if (self._flash != null) self._flash.gameObject.SetActive(false);
            if (self._fx != null)
            {
                self._fx.localRotation = Quaternion.identity;
                self._fx.anchoredPosition = Vector2.zero;
            }
            if (self._root != null)
            {
                for (int i = 0; i < self._root.childCount; i++)
                {
                    var ch = self._root.GetChild(i);
                    if (ch == null) continue;
                    if (ch.name == "Float" || ch.name == "FlyCoin")
                        ch.gameObject.SetActive(false);
                }
            }
            if (self._canvas != null) self._canvas.enabled = false;
        }

        public static void Resume()
        {
            if (_inst != null && _inst._canvas != null)
                _inst._canvas.enabled = true;
        }

        private void Build()
        {
            // 페이드 베일(FlowUIRoot=500)보다 위 — 챕터 시작·출발 글자가 가려지지 않게
            _canvas = CoastUiCanvas.Create("PickupFloatCanvas", 560, transform);
            var gr = _canvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
            // HudInset(디자인 720×1280)에 올려 다른 HUD와 같은 좌표계 사용
            _root = CoastUiCanvas.Root(_canvas);
            var fx = new GameObject("Fx", typeof(RectTransform)).GetComponent<RectTransform>();
            fx.SetParent(_root, false); fx.anchorMin = Vector2.zero; fx.anchorMax = Vector2.one; fx.offsetMin = Vector2.zero; fx.offsetMax = Vector2.zero;
            _fx = fx;
            var tex = PaintedProp.Load("Coin_Gold");
            if (tex != null) _coinSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private void OnDestroy()
        {
            if (_inst == this) _inst = null;
        }

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
        private Coroutine _goCo;
        public static void Banner(string text, Color color, float seconds)
        {
            var f = Ensure();
            f.StartCoroutine(f.BannerSeq(text, color, seconds));
        }

        /// 러닝 출발: 화면 한가운데에 크게 「출발」이 팡 하고 뜬다.
        public static void Go(string text, float seconds = 0.95f)
        {
            var f = Ensure();
            if (f._goCo != null) f.StopCoroutine(f._goCo);
            f._goCo = f.StartCoroutine(f.GoSeq(text, null, seconds));
        }

        /// 러닝 출발: 「챕터 N 시작!」을 크게, 아래엔 장소/제목.
        public static void ChapterStart(int chapter, string place, string storyTitle, float seconds = 1.45f)
        {
            var f = Ensure();
            if (f._goCo != null) f.StopCoroutine(f._goCo);
            string head = Loc.T($"챕터 {chapter} 시작!", $"CHAPTER {chapter}");
            string sub = !string.IsNullOrEmpty(place) ? place : storyTitle;
            if (!string.IsNullOrEmpty(place) && !string.IsNullOrEmpty(storyTitle) && place != storyTitle)
                sub = $"{place}\n{storyTitle}";
            f._goCo = f.StartCoroutine(f.GoSeq(head, sub, seconds));
        }

        /// 63차(사용자): 러닝 시작 때 장애물을 살짝 소개 — 챕터 카드 아래 반투명 띠 한 줄(피하기), seconds 뒤 사라짐.
        /// 65차(사용자): 아이템(모으기) 줄은 빼고 장애물만, 배경은 살짝 반투명, 2초.
        public static void ItemGuide(float seconds)
        {
            var f = Ensure();
            f.StartCoroutine(f.ItemGuideSeq(seconds));
        }
        private IEnumerator ItemGuideSeq(float seconds)
        {
            var parent = _fx != null ? _fx : _root;
            var strip = CoastUiArt.Panel(parent, "ItemGuide", new Color(0.10f, 0.08f, 0.16f, 0.55f), 26);
            var srt = strip.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, -300f); srt.sizeDelta = new Vector2(600f, 132f); strip.raycastTarget = false;
            var cg = strip.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
            string[] row = { Loc.T("피하기", "AVOID"), "Obs_Cone", Loc.T("콘 · 점프", "Cone · jump"), "Obs_Barrier", Loc.T("바리케이드", "Barrier"), "Obs_Clothesline", Loc.T("빨래줄 · 숙이기", "Line · duck"), "Obs_BusFront", Loc.T("버스 · 피하기", "Bus · dodge") };
            float y = -14f;
            var lab = CoastUiArt.GlossyPill(srt, "Lab", new Color(0.85f, 0.25f, 0.30f), 14, 5); lab.raycastTarget = false;
            var lrt = lab.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 1f); lrt.pivot = new Vector2(0f, 1f); lrt.anchoredPosition = new Vector2(14f, y - 30f); lrt.sizeDelta = new Vector2(84f, 34f);
            var lt = CoastHudLayout.MakeText(lrt, "T", row[0], 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            lt.color = Color.white; lt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lt, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            for (int i = 0; i < 4; i++)
            {
                float x = 112f + i * 122f;
                var tex = ArtAssets.LoadTexture(row[1 + i * 2]);
                if (tex != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(srt, false); im.sprite = CoastUiArt.AsSprite(tex, 100f); im.preserveAspect = true; im.raycastTarget = false;
                    var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f); irt.pivot = new Vector2(0.5f, 1f); irt.anchoredPosition = new Vector2(x + 50f, y - 4f); irt.sizeDelta = new Vector2(64f, 64f);
                }
                var nt = CoastHudLayout.MakeText(srt, "N", row[2 + i * 2], 13, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x - 6f, y - 96f), new Vector2(x + 106f, y - 70f));
                nt.color = new Color(1f, 0.96f, 0.90f); nt.fontStyle = FontStyle.Bold; nt.raycastTarget = false; CoastUiArt.OutlineText(nt, new Color(0f, 0f, 0f, 0.5f), 1.2f);
                nt.resizeTextForBestFit = true; nt.resizeTextMinSize = 8; nt.resizeTextMaxSize = CoastHudLayout.Scaled(13);
            }
            float t = 0f;
            while (t < seconds && strip != null)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((seconds - t) / 0.3f);
                cg.alpha = a; srt.localScale = Vector3.one * (0.92f + 0.08f * Mathf.Clamp01(t / 0.25f));
                yield return null;
            }
            if (strip != null) Destroy(strip.gameObject);
        }

        /// 65차(사용자): 보스 등장 때 「이 보스는 뭘 하나」 안내 — 반투명 띠(왼쪽 보스 그림 + 이름 + 효과 두 줄), seconds 뒤 사라짐.
        public static void InfoStrip(string iconRes, string title, string body, Color titleCol, float seconds)
        {
            var f = Ensure();
            f.StartCoroutine(f.InfoStripSeq(iconRes, title, body, titleCol, seconds));
        }
        private IEnumerator InfoStripSeq(string iconRes, string title, string body, Color titleCol, float seconds)
        {
            var parent = _fx != null ? _fx : _root;
            var strip = CoastUiArt.Panel(parent, "InfoStrip", new Color(0.10f, 0.08f, 0.16f, 0.62f), 26);
            var srt = strip.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, 150f); srt.sizeDelta = new Vector2(620f, 150f); strip.raycastTarget = false;
            var cg = strip.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
            var tex = ArtAssets.LoadTexture(iconRes);
            float textL = 24f;
            if (tex != null)
            {
                var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(srt, false); im.sprite = CoastUiArt.AsSprite(tex, 100f); im.preserveAspect = true; im.raycastTarget = false;
                var irt = im.rectTransform; irt.anchorMin = new Vector2(0f, 0f); irt.anchorMax = new Vector2(0f, 1f); irt.pivot = new Vector2(0f, 0.5f); irt.anchoredPosition = new Vector2(14f, 0f); irt.sizeDelta = new Vector2(122f, -14f);
                textL = 150f;
            }
            var tt = CoastHudLayout.MakeText(srt, "T", title, 24, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, -56f), new Vector2(-14f, -10f));
            tt.color = titleCol; tt.fontStyle = FontStyle.Bold; tt.raycastTarget = false; CoastUiArt.OutlineText(tt, new Color(0f, 0f, 0f, 0.6f), 1.6f);
            tt.resizeTextForBestFit = true; tt.resizeTextMinSize = 12; tt.resizeTextMaxSize = CoastHudLayout.Scaled(24);
            var bt = CoastHudLayout.MakeText(srt, "B", body, 16, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(textL, 10f), new Vector2(-14f, -60f));
            bt.color = new Color(1f, 0.96f, 0.90f); bt.fontStyle = FontStyle.Bold; bt.raycastTarget = false; bt.horizontalOverflow = HorizontalWrapMode.Wrap; CoastUiArt.OutlineText(bt, new Color(0f, 0f, 0f, 0.5f), 1.2f);
            bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 10; bt.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            float t = 0f;
            while (t < seconds && strip != null)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((seconds - t) / 0.35f);
                cg.alpha = a; srt.localScale = Vector3.one * (0.92f + 0.08f * Mathf.Clamp01(t / 0.25f));
                yield return null;
            }
            if (strip != null) Destroy(strip.gameObject);
        }

        private Text _goText;
        private Text _goSub;
        private IEnumerator GoSeq(string text, string sub, float seconds)
        {
            if (_goText == null)
            {
                var parent = _fx != null ? _fx : _root;
                _goText = CoastHudLayout.MakeText(parent, "Go", "", 128, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480f, -100f), new Vector2(480f, 160f));
                _goText.fontStyle = FontStyle.Bold; _goText.raycastTarget = false;
                _goText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _goText.verticalOverflow = VerticalWrapMode.Overflow;
                CoastUiArt.OutlineText(_goText, new Color(0.35f, 0.12f, 0.02f, 1f), 5f);
                _goSub = CoastHudLayout.MakeText(parent, "GoSub", "", 36, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460f, -220f), new Vector2(460f, -40f));
                _goSub.fontStyle = FontStyle.Bold; _goSub.raycastTarget = false;
                _goSub.horizontalOverflow = HorizontalWrapMode.Wrap;
                _goSub.verticalOverflow = VerticalWrapMode.Overflow;
                CoastUiArt.OutlineText(_goSub, new Color(0.08f, 0.05f, 0.18f, 0.95f), 3f);
            }
            bool hasSub = !string.IsNullOrEmpty(sub);
            _goText.gameObject.SetActive(true);
            _goText.text = text;
            _goText.fontSize = CoastHudLayout.Scaled(hasSub ? 72 : 84);
            if (_goSub != null)
            {
                _goSub.gameObject.SetActive(hasSub);
                _goSub.text = hasSub ? sub : "";
                _goSub.fontSize = CoastHudLayout.Scaled(28);
            }
            var srt = _goText.rectTransform;
            var subRt = _goSub != null ? _goSub.rectTransform : null;
            float t = 0f;
            while (t < seconds)
            {
                // 42차: 씬 로드 직후의 긴 프레임(1초 이상)이 한 번에 더해져 카드가 0.3초 만에 끝났다 → 프레임당 최대 50 ms
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float u = Mathf.Clamp01(t / seconds);
                float pop = u < 0.16f ? Mathf.Lerp(2.6f, 1.08f, u / 0.16f)
                    : u < 0.72f ? 1.08f + Mathf.Sin(t * 9f) * 0.035f
                    : Mathf.Lerp(1.08f, 1.28f, (u - 0.72f) / 0.28f);
                srt.localScale = Vector3.one * pop;
                srt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 7f) * 2.8f);
                srt.anchoredPosition = new Vector2(0f, hasSub ? 70f : 40f);
                var c = Color.Lerp(new Color(1f, 0.88f, 0.35f), Color.white, (Mathf.Sin(t * 14f) + 1f) * 0.16f);
                c.a = u > 0.78f ? 1f - (u - 0.78f) / 0.22f : 1f;
                _goText.color = c;
                if (hasSub && subRt != null)
                {
                    float su = Mathf.Clamp01((u - 0.08f) / 0.2f);
                    subRt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, su);
                    subRt.anchoredPosition = new Vector2(0f, -55f);
                    var sc = new Color(1f, 0.96f, 0.9f, c.a * su);
                    _goSub.color = sc;
                }
                yield return null;
            }
            _goText.gameObject.SetActive(false);
            if (_goSub != null) _goSub.gameObject.SetActive(false);
            _goCo = null;
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
            if (f._impactCo != null) f.StopCoroutine(f._impactCo);
            f._impactCo = f.StartCoroutine(f.ImpactSeq(word));
        }

        private Coroutine _impactCo;
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
            // 「출발」 글자와 겹치면 꽈당이 안 보이는 것처럼 느껴져서 끈다.
            if (_goText != null) _goText.gameObject.SetActive(false);
            if (_goCo != null) { StopCoroutine(_goCo); _goCo = null; }

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
            _impactCo = null;
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

        private const float FloatLife = 0.65f;
        private readonly List<(Text t, float dieAt)> _live = new List<(Text, float)>(16);

        private void LateUpdate()
        {
            // 코루틴이 끊겨도(씬 전환·예외) +N 이 화면에 남는 걸 막는다
            float now = Time.unscaledTime;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var e = _live[i];
                if (e.t == null) { _live.RemoveAt(i); continue; }
                if (now < e.dieAt) continue;
                RecycleFloat(e.t);
                _live.RemoveAt(i);
            }
        }

        private IEnumerator FloatText(Vector3 world, string text, Color color, float size)
        {
            Text t = RentFloat();
            var cg = t.GetComponent<CanvasGroup>() ?? t.gameObject.AddComponent<CanvasGroup>();
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = new Color(color.r, color.g, color.b, 1f);
            t.fontSize = Mathf.RoundToInt(CoastHudLayout.Scaled(52) * size);
            cg.alpha = 1f;
            var rt = t.rectTransform;
            rt.localScale = Vector3.one;
            Vector2 start = ToCanvas(world) + new Vector2(Random.Range(-18f, 18f), 110f);
            float dieAt = Time.unscaledTime + FloatLife + 0.15f;
            _live.Add((t, dieAt));

            float dur = FloatLife, time = 0f;
            try
            {
                while (time < dur)
                {
                    if (t == null) yield break;
                    time += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(time / dur);
                    float pop = u < 0.18f ? Mathf.Lerp(0.4f, 1.25f, u / 0.18f)
                        : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((u - 0.18f) / 0.2f));
                    rt.localScale = Vector3.one * pop;
                    rt.anchoredPosition = start + new Vector2(0f, 150f * u);
                    cg.alpha = u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
                    yield return null;
                }
            }
            finally
            {
                if (t != null)
                {
                    for (int i = _live.Count - 1; i >= 0; i--)
                        if (_live[i].t == t) _live.RemoveAt(i);
                    RecycleFloat(t);
                }
            }
        }

        private Text RentFloat()
        {
            while (_pool.Count > 0)
            {
                var t = _pool.Pop();
                if (t != null) return t;
            }
            return MakeText();
        }

        private void RecycleFloat(Text t)
        {
            if (t == null) return;
            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
            t.gameObject.SetActive(false);
            t.rectTransform.localScale = Vector3.one;
            _pool.Push(t);
        }

        private Text MakeText()
        {
            var go = new GameObject("Float", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 90f);
            var t = go.AddComponent<Text>();
            t.font = CoastHudLayout.Font();
            t.fontSize = CoastHudLayout.Scaled(52);
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            CoastUiArt.OutlineText(t, new Color(0.05f, 0.05f, 0.15f, 0.95f), 2.5f);
            go.SetActive(false);
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
