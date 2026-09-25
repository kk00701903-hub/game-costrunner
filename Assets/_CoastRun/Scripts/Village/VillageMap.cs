using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 137차(사용자): 오른쪽 위 미니맵(지형 스플랫 텍스처 + 장소 점 + 주인공 점) — 누르면 마을 전체 지도(이름표 포함).
    public class VillageMap : MonoBehaviour
    {
        public struct Mark { public string ko, en; public Vector2 pos; public Color col; }
        public static readonly Mark[] Marks = {
            new Mark { ko = "우리집", en = "Home", pos = new Vector2(0f, 36f), col = new Color(0.98f, 0.45f, 0.45f) },
            new Mark { ko = "송전탑", en = "Tower", pos = new Vector2(15f, 41f), col = new Color(0.45f, 0.48f, 0.55f) },
            new Mark { ko = "엄마 집", en = "Mom's", pos = new Vector2(4.8f, -19.9f), col = new Color(0.95f, 0.72f, 0.35f) },
            new Mark { ko = "상점", en = "Shop", pos = new Vector2(-5.8f, -33.5f), col = new Color(0.98f, 0.60f, 0.30f) },
            new Mark { ko = "텃밭", en = "Garden", pos = new Vector2(0f, -6f), col = new Color(0.45f, 0.30f, 0.20f) },
            new Mark { ko = "바닷가", en = "Beach", pos = new Vector2(-1f, -31f), col = new Color(0.30f, 0.65f, 0.95f) },
            new Mark { ko = "등대", en = "Lighthouse", pos = new Vector2(-7f, -64f), col = new Color(0.92f, 0.30f, 0.30f) },
            new Mark { ko = "정자", en = "Pavilion", pos = new Vector2(28f, -6f), col = new Color(0.55f, 0.40f, 0.30f) },
            new Mark { ko = "병원", en = "Hospital", pos = new Vector2(23f, 13.5f), col = new Color(0.92f, 0.36f, 0.40f) },
            new Mark { ko = "축사", en = "Barn", pos = new Vector2(VillageEast.BankX, VillageEast.BankZ), col = new Color(0.75f, 0.30f, 0.25f) },          // 195차: 은행은 시내로
            new Mark { ko = "광산", en = "Mine", pos = VillageZones.MineMouth, col = new Color(0.45f, 0.40f, 0.36f) },
            new Mark { ko = "과수원", en = "Orchard", pos = new Vector2(40f, 27f), col = new Color(1f, 0.55f, 0.15f) },
            new Mark { ko = "브런치 카페", en = "Brunch cafe", pos = new Vector2(VillageEast.CafeX, VillageEast.CafeZ), col = new Color(1f, 0.60f, 0.20f) },   // 194차
            new Mark { ko = "버스(시내·중문)", en = "Bus", pos = new Vector2(VillageEast.StopX, VillageEast.StopZ), col = new Color(0.25f, 0.55f, 0.90f) },   // 194차
        };

        /// 194차(지도 1.5배): 지도 그림 — 스플랫 색 + 바다(해수면 아래)는 파랑. 예전엔 아래 17 % 를 바다 띠로 덮었는데 지도가 넓어지며 반도·등대가 띠에 묻혔다.
        static Texture2D _mapTex;
        static Texture2D MapTex()
        {
            var splat = VillageWorld.SplatTex; if (splat == null) return null;
            if (_mapTex != null) return _mapTex;
            const int N = 256; _mapTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "VillageMapTex" };
            var px = new Color[N * N]; var sea = new Color(0.40f, 0.70f, 0.92f); var seaDeep = new Color(0.30f, 0.58f, 0.86f);
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
            {
                float u = (i + 0.5f) / N, v = (j + 0.5f) / N; float x = -VillageWorld.Half + u * 2f * VillageWorld.Half, z = -VillageWorld.Half + v * 2f * VillageWorld.Half;
                float h = VillageWorld.Height(x, z);
                Color c;
                try { c = splat.GetPixelBilinear(u, v); } catch { c = new Color(0.55f, 0.78f, 0.40f); }
                if (h < VillageWorld.SeaLevel + 0.05f) c = Color.Lerp(sea, seaDeep, Mathf.Clamp01((VillageWorld.SeaLevel - h) / 3f));
                if (VillageEast.RoadDist(x, z) < VillageEast.RoadHalfW) c = new Color(0.45f, 0.47f, 0.52f);
                px[j * N + i] = c;
            }
            _mapTex.SetPixels(px); _mapTex.Apply(false, false);
            return _mapTex;
        }

        /// 196차: 이야기 장소(빛나는 곳) — 미니맵·지도에 금빛 별
        public static Vector2? StoryTarget; public static string StoryLabel; public static Color StoryCol = new Color(1f, 0.82f, 0.30f);
        RectTransform _storyMk; Image _storyImg;
        Transform _player; RectTransform _mini, _dot; UnityEngine.UI.Image _dotImg; readonly List<RectTransform> _miniMarks = new List<RectTransform>();
        RectTransform _full, _fullDot; Canvas _fullCv;

        public static VillageMap Create(RectTransform hudRoot, Transform player)
        {
            var go = new GameObject("VillageMap"); var m = go.AddComponent<VillageMap>(); m._player = player; m.Build(hudRoot); return m;
        }

        static Vector2 Norm(Vector2 world) => new Vector2((world.x + VillageWorld.Half) / (2f * VillageWorld.Half), (world.y + VillageWorld.Half) / (2f * VillageWorld.Half));

        void Build(RectTransform root)
        {
            float size = 150f;
            // 140차(시안): 둥근 미니맵 — 크림색 테두리 원 + 원형 마스크
            var frameGo = new GameObject("MiniMap", typeof(RectTransform), typeof(Image)); frameGo.transform.SetParent(root, false);
            var frame = frameGo.GetComponent<Image>(); frame.sprite = CoastUiArt.RoundedRect(60); frame.type = Image.Type.Simple; frame.color = CoastUiArt.CreamOutline; frame.raycastTarget = true;
            _mini = frame.rectTransform; _mini.anchorMin = _mini.anchorMax = new Vector2(1f, 1f); _mini.pivot = new Vector2(1f, 1f);
            _mini.anchoredPosition = new Vector2(-14f, -148f); _mini.sizeDelta = new Vector2(size, size);   // 187차: 오늘 미션 띠(−92~−138) 아래로
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask)); maskGo.transform.SetParent(_mini, false);
            var mrt = maskGo.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = new Vector2(6f, 6f); mrt.offsetMax = new Vector2(-6f, -6f);
            maskGo.GetComponent<Image>().sprite = CoastUiArt.RoundedRect(60); maskGo.GetComponent<Image>().type = Image.Type.Simple; maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var raw = new GameObject("Map", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); raw.transform.SetParent(mrt, false); raw.raycastTarget = false;
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one; raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
            raw.texture = MapTex() ?? (Texture)VillageWorld.SplatTex;
            // 바다(남쪽) 파랑 띠
            var sea = CoastHudLayout.MakeImage(mrt, "Sea", new Vector2(0f, 0f), new Vector2(1f, 0.17f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.65f, 0.90f, 0.9f)); sea.raycastTarget = false;
            foreach (var mk in Marks)
            {
                var d = CoastUiArt.GlossyPill(mrt, "Mk", mk.col, 6, 2); d.raycastTarget = false;
                var drt = d.rectTransform; var n = Norm(mk.pos); drt.anchorMin = drt.anchorMax = n; drt.sizeDelta = new Vector2(12f, 12f); _miniMarks.Add(drt);
            }
            _storyImg = CoastUiArt.GlossyPill(mrt, "Story", StoryCol, 8, 3); _storyImg.raycastTarget = false; _storyMk = _storyImg.rectTransform; _storyMk.sizeDelta = new Vector2(16f, 16f); _storyMk.gameObject.SetActive(false);
            var dot = CoastUiArt.GlossyPill(mrt, "Me", new Color(1f, 0.10f, 0.12f), 9, 2); dot.raycastTarget = false;   // 187차: 더 붉게 + 반짝(LateUpdate)
            _dot = dot.rectTransform; _dot.sizeDelta = new Vector2(18f, 18f); _dotImg = dot; _dot.SetAsLastSibling();
            var lab = CoastHudLayout.MakeText(_mini, "L", Loc.T("미니맵", "Minimap"), 13, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 26f));
            lab.color = Color.white; lab.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lab, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            var b = frame.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => { CoastPrefs.Vibrate(); OpenFull(); });
        }

        public void SetHidden(bool h) { if (_mini != null) _mini.gameObject.SetActive(!h); }   // 195차: 시내·관광지·광산에선 마을 미니맵 숨김
        void LateUpdate()
        {
            if (_player == null) return;
            var n = Norm(new Vector2(_player.position.x, _player.position.z));
            if (_dot != null)
            {
                _dot.anchorMin = _dot.anchorMax = n;
                // 187차: 1.4초마다 한 번 크게 반짝(흰빛) — 평소엔 진한 빨강으로 두근두근
                float t = Time.unscaledTime % 1.4f; float flash = t < 0.18f ? 1f - t / 0.18f : 0f;
                float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
                _dot.localScale = Vector3.one * (1f + 0.18f * beat + 0.45f * flash);
                if (_dotImg != null) _dotImg.color = Color.Lerp(new Color(1f, 0.10f, 0.12f), Color.white, flash * 0.8f);
            }
            if (_fullDot != null) { _fullDot.anchorMin = _fullDot.anchorMax = n; }
            if (_storyMk != null)
            {
                bool on = StoryTarget.HasValue; if (_storyMk.gameObject.activeSelf != on) _storyMk.gameObject.SetActive(on);
                if (on) { _storyMk.anchorMin = _storyMk.anchorMax = Norm(StoryTarget.Value); _storyMk.localScale = Vector3.one * (1f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f))); _storyImg.color = StoryCol; }
            }
        }

        public bool FullOpen => _fullCv != null;

        public void OpenFull()
        {
            if (_fullCv != null) return;
            _fullCv = CoastUiCanvas.Create("VillageMapFull", 160);
            var root = CoastUiCanvas.Root(_fullCv);
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.10f, 0.08f, 0.18f, 0.75f)); dim.raycastTarget = true;
            var card = CoastUiArt.CutePill(root, "Card", new Color(1f, 0.97f, 0.92f), 28, 6); card.raycastTarget = true;
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(680f, 880f);
            var title = CoastHudLayout.MakeText(crt, "T", Loc.T("🗺 하늘 바닷가 마을 지도", "🗺 Haneul Seaside map"), 30, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -78f), new Vector2(-20f, -18f));
            title.color = EventCardKit.BrownInk; title.fontStyle = FontStyle.Bold;
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask)); maskGo.transform.SetParent(crt, false);
            var mrt = maskGo.GetComponent<RectTransform>(); mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f); mrt.anchoredPosition = new Vector2(0f, 10f); mrt.sizeDelta = new Vector2(640f, 640f);
            maskGo.GetComponent<Image>().sprite = CoastUiArt.RoundedRect(22); maskGo.GetComponent<Image>().type = Image.Type.Sliced; maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var raw = new GameObject("Map", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); raw.transform.SetParent(mrt, false); raw.raycastTarget = false;
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one; raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
            raw.texture = MapTex() ?? (Texture)VillageWorld.SplatTex;
            var sea = CoastHudLayout.MakeImage(mrt, "Sea", new Vector2(0.62f, 0.02f), new Vector2(0.98f, 0.10f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f)); sea.raycastTarget = false;
            var seaT = CoastHudLayout.MakeText(sea.rectTransform, "T", Loc.T("바다", "Sea"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); seaT.color = new Color(1f, 1f, 1f, 0.85f);
            foreach (var mk in Marks)
            {
                var n = Norm(mk.pos);
                var d = CoastUiArt.GlossyPill(mrt, "Mk", mk.col, 10, 3); d.raycastTarget = false;
                var drt = d.rectTransform; drt.anchorMin = drt.anchorMax = n; drt.sizeDelta = new Vector2(22f, 22f);
                var lp = CoastUiArt.CutePill(mrt, "Lp", new Color(1f, 1f, 1f, 0.92f), 12, 2); lp.raycastTarget = false;
                var lrt = lp.rectTransform; lrt.anchorMin = lrt.anchorMax = n; lrt.pivot = new Vector2(0.5f, 0f); lrt.anchoredPosition = new Vector2(0f, 14f); lrt.sizeDelta = new Vector2(96f, 32f);
                var lt = CoastHudLayout.MakeText(lrt, "T", Loc.T(mk.ko, mk.en), 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, 0f));
                lt.color = EventCardKit.BrownInk; lt.fontStyle = FontStyle.Bold; lt.resizeTextForBestFit = true; lt.resizeTextMinSize = 10; lt.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            }
            if (StoryTarget.HasValue)
            {
                var sn = Norm(StoryTarget.Value);
                var sd = CoastUiArt.GlossyPill(mrt, "StoryMk", StoryCol, 12, 3); sd.raycastTarget = false; sd.rectTransform.anchorMin = sd.rectTransform.anchorMax = sn; sd.rectTransform.sizeDelta = new Vector2(30f, 30f);
                var sl = CoastUiArt.CutePill(mrt, "StoryLp", new Color(1f, 0.93f, 0.70f, 0.95f), 12, 2); sl.raycastTarget = false;
                var slr = sl.rectTransform; slr.anchorMin = slr.anchorMax = sn; slr.pivot = new Vector2(0.5f, 1f); slr.anchoredPosition = new Vector2(0f, -18f); slr.sizeDelta = new Vector2(170f, 32f);
                var st = CoastHudLayout.MakeText(slr, "T", "✨ " + (StoryLabel ?? ""), 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 1f), new Vector2(-4f, 0f));
                st.color = EventCardKit.BrownInk; st.fontStyle = FontStyle.Bold; st.resizeTextForBestFit = true; st.resizeTextMinSize = 10; st.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            }
            var dot = CoastUiArt.GlossyPill(mrt, "Me", new Color(1f, 0.35f, 0.55f), 12, 3); dot.raycastTarget = false;
            _fullDot = dot.rectTransform; _fullDot.sizeDelta = new Vector2(26f, 26f);
            var meT = CoastHudLayout.MakeText(_fullDot, "T", Loc.T("나", "Me"), 12, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); meT.color = Color.white; meT.fontStyle = FontStyle.Bold;
            var hint = CoastHudLayout.MakeText(crt, "H", Loc.T("분홍 점이 나. 가까이 가면 뜨는 분홍 버튼으로 들어갈 수 있어.", "Pink dot is you. Walk close and tap the pink prompt to enter."), 15, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 86f), new Vector2(-20f, 120f));
            hint.color = new Color(0.45f, 0.38f, 0.42f); hint.horizontalOverflow = HorizontalWrapMode.Wrap; hint.resizeTextForBestFit = true; hint.resizeTextMinSize = 10; hint.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            var close = CoastUiArt.GlossyPill(crt, "Close", new Color(1f, 0.55f, 0.65f), 22, 8); close.raycastTarget = true;
            var clr = close.rectTransform; clr.anchorMin = clr.anchorMax = new Vector2(0.5f, 0f); clr.pivot = new Vector2(0.5f, 0f); clr.anchoredPosition = new Vector2(0f, 18f); clr.sizeDelta = new Vector2(260f, 60f);
            var ct = CoastHudLayout.MakeText(clr, "T", Loc.T("닫기", "Close"), 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); ct.color = Color.white; ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(() => { CoastPrefs.Vibrate(); CloseFull(); });
            var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None; db.onClick.AddListener(CloseFull);
        }

        public void CloseFull() { if (_fullCv != null) Destroy(_fullCv.gameObject); _fullCv = null; _fullDot = null; }
        void OnDestroy() { CloseFull(); }
    }
}
