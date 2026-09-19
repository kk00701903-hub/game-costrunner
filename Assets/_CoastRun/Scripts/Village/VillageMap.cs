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
            new Mark { ko = "엄마 집", en = "Mom's", pos = new Vector2(4.8f, -21.5f), col = new Color(0.95f, 0.72f, 0.35f) },
            new Mark { ko = "상점", en = "Shop", pos = new Vector2(-5.8f, -33.5f), col = new Color(0.98f, 0.60f, 0.30f) },
            new Mark { ko = "텃밭", en = "Garden", pos = new Vector2(0f, -6f), col = new Color(0.45f, 0.30f, 0.20f) },
            new Mark { ko = "바닷가", en = "Beach", pos = new Vector2(-1f, -31f), col = new Color(0.30f, 0.65f, 0.95f) },
            new Mark { ko = "등대", en = "Lighthouse", pos = new Vector2(-7f, -64f), col = new Color(0.92f, 0.30f, 0.30f) },
            new Mark { ko = "정자", en = "Pavilion", pos = new Vector2(28f, -6f), col = new Color(0.55f, 0.40f, 0.30f) },
        };

        Transform _player; RectTransform _mini, _dot; readonly List<RectTransform> _miniMarks = new List<RectTransform>();
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
            _mini.anchoredPosition = new Vector2(-14f, -96f); _mini.sizeDelta = new Vector2(size, size);
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask)); maskGo.transform.SetParent(_mini, false);
            var mrt = maskGo.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = new Vector2(6f, 6f); mrt.offsetMax = new Vector2(-6f, -6f);
            maskGo.GetComponent<Image>().sprite = CoastUiArt.RoundedRect(60); maskGo.GetComponent<Image>().type = Image.Type.Simple; maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var raw = new GameObject("Map", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); raw.transform.SetParent(mrt, false); raw.raycastTarget = false;
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one; raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
            raw.texture = VillageWorld.SplatTex;
            // 바다(남쪽) 파랑 띠
            var sea = CoastHudLayout.MakeImage(mrt, "Sea", new Vector2(0f, 0f), new Vector2(1f, 0.17f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.65f, 0.90f, 0.9f)); sea.raycastTarget = false;
            foreach (var mk in Marks)
            {
                var d = CoastUiArt.GlossyPill(mrt, "Mk", mk.col, 6, 2); d.raycastTarget = false;
                var drt = d.rectTransform; var n = Norm(mk.pos); drt.anchorMin = drt.anchorMax = n; drt.sizeDelta = new Vector2(12f, 12f); _miniMarks.Add(drt);
            }
            var dot = CoastUiArt.GlossyPill(mrt, "Me", new Color(1f, 0.35f, 0.55f), 8, 2); dot.raycastTarget = false;
            _dot = dot.rectTransform; _dot.sizeDelta = new Vector2(16f, 16f);
            var lab = CoastHudLayout.MakeText(_mini, "L", Loc.T("미니맵", "Minimap"), 13, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 26f));
            lab.color = Color.white; lab.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(lab, new Color(0f, 0f, 0f, 0.6f), 1.5f);
            var b = frame.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => { CoastPrefs.Vibrate(); OpenFull(); });
        }

        void LateUpdate()
        {
            if (_player == null) return;
            var n = Norm(new Vector2(_player.position.x, _player.position.z));
            if (_dot != null) { _dot.anchorMin = _dot.anchorMax = n; }
            if (_fullDot != null) { _fullDot.anchorMin = _fullDot.anchorMax = n; }
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
            raw.texture = VillageWorld.SplatTex;
            var sea = CoastHudLayout.MakeImage(mrt, "Sea", new Vector2(0f, 0f), new Vector2(1f, 0.17f), Vector2.zero, Vector2.zero, new Color(0.35f, 0.65f, 0.90f, 0.9f)); sea.raycastTarget = false;
            var seaT = CoastHudLayout.MakeText(sea.rectTransform, "T", Loc.T("바다", "Sea"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); seaT.color = new Color(1f, 1f, 1f, 0.8f);
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
