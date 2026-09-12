using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 48차-14: 포토카드 화면 — 사용자 시안(UI_Photocard_Mock, 1152×2128 → 1080×1920)을 배경 아트로 깔고
    /// 사진·캡션·등급·수집 수·다음 3장 슬롯·버튼만 실제 데이터로 얹는다. 시안 픽셀 좌표(1152×2128) → 디자인(720×1280) 변환 M().
    ///   진입: 컬렉션(포토카드) 열면 대표 카드(가장 최근 얻은 공개 카드) → [바인더] 로 기존 3×3 그리드, 그리드 카드 탭 → 이 화면.
    public partial class CollectionUI
    {
        private GameObject _mock;
        private int _mockId;

        private static Vector2 M(float mx, float my) => new Vector2(mx * (720f / 1152f) - 360f, 640f - my * (1280f / 2128f));
        private static Vector2 MSize(float w, float h) => new Vector2(w * (720f / 1152f), h * (1280f / 2128f));
        private static RectTransform MRect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = M((x0 + x1) * 0.5f, (y0 + y1) * 0.5f); rt.sizeDelta = MSize(x1 - x0, y1 - y0);
            return rt;
        }

        private int FeaturedCard()
        {
            for (int id = PhotocardTable.Count; id >= PublicFrom; id--) if (Collection.HasCard(id)) return id;
            return PublicFrom;
        }

        private void ShowMockCard(int id)
        {
            if (_mock != null) Destroy(_mock);
            _mockId = id;
            bool isNew = Collection.CardIsNew(id);
            Collection.ClearNew(id);
            var card = PhotocardTable.Get(id);
            bool has = Collection.HasCard(id);
            var grade = PhotocardTable.GradeOf(id);
            var pad = CoastUiCanvas.HudPad;

            // 전체 화면 컨테이너(720×1280 디자인, HudInset 바깥 패드까지)
            var root = new GameObject("MockCard", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(_root, false);
            root.anchorMin = new Vector2(0.5f, 0.5f); root.anchorMax = new Vector2(0.5f, 0.5f); root.sizeDelta = new Vector2(720f, 1280f);
            _mock = root.gameObject;
            var bg = CoastHudLayout.MakeImage(root, "Bg", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.11f, 0.07f, 0.26f));
            bg.raycastTarget = true;
            var art = CoastHudLayout.MakeImage(root, "Art", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var sp = CoastUiArt.Art("UI_Photocard_Mock"); if (sp != null) art.sprite = sp; else art.color = new Color(0.16f, 0.10f, 0.32f);
            art.raycastTarget = true;

            // 사진(폴라로이드 안) — 시안 (300,383)~(869,979)
            var photoRt = MRect(root, "Photo", 232f, 372f, 903f, 1195f);   // 카드 아트 자체가 폴라로이드(사진+캡션)라 종이 영역 전체를 덮는다
            var paper = photoRt.gameObject.AddComponent<Image>(); paper.color = new Color(0.965f, 0.96f, 0.93f); paper.raycastTarget = false;
            var img = CoastHudLayout.MakeImage(photoRt, "Img", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            if (has)
            {
                var tex = ArtAssets.LoadTexture(card.Image) ?? ArtAssets.LoadTexture(card.FallbackImage);
                if (tex != null) { img.sprite = CoastUiArt.AsSprite(tex, 100f); img.preserveAspect = true; }
            }
            else
            {
                img.color = new Color(0.22f, 0.16f, 0.40f);
                var q = CoastOrnate.Label(img.transform, "Q", "?", 90, new Color(1f, 1f, 1f, 0.35f));
                Stretch(q.rectTransform);
                var hint = CoastOrnate.Label(img.transform, "Hint", card.Hint, 14, new Color(1f, 1f, 1f, 0.8f));
                Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 26f), new Vector2(-24f, 40f));
                hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            // 등급 — SSR 은 시안에 그려진 배지 그대로, 그 외는 종이색으로 덮고 등급 알약
            if (grade != CardGrade.SSR || !has)
            {
                var cover = MRect(root, "GradeCover", 212f, 250f, 600f, 366f);
                var ci = cover.gameObject.AddComponent<Image>(); ci.color = new Color(0.965f, 0.96f, 0.93f); ci.raycastTarget = false;
                var badge = CoastUiArt.CutePill(cover, "Badge", has ? Collection.GradeColor(grade) : new Color(0.55f, 0.52f, 0.62f), 14, 3); badge.raycastTarget = false;
                Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 8f), new Vector2(84f, 38f));
                var gl = CoastOrnate.Label(badge.transform, "T", has ? Collection.GradeName(grade) : "?", 20, Color.white); gl.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(gl, new Color(0f, 0f, 0f, 0.35f), 1.2f);
                string stars = grade == CardGrade.SR ? "★★★★ · PHOTOCARD" : grade == CardGrade.R ? "★★★ · PHOTOCARD" : "★★ · PHOTOCARD";
                var rare = CoastOrnate.Label(cover, "Rare", has ? stars : Loc.T("잠김 · 러닝 중 포토카드 아이템", "Locked · photocard items while running"), 11, has ? new Color(0.85f, 0.45f, 0.1f) : new Color(0.5f, 0.45f, 0.55f), TextAnchor.MiddleLeft);
                Place(rare.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 6f), new Vector2(0f, 26f)); rare.fontStyle = FontStyle.Bold;
            }
            // COLLECTION n/N
            int pubTotal = PhotocardTable.Count - (PublicFrom - 1), pubOwned = 0;
            for (int k = PublicFrom; k <= PhotocardTable.Count; k++) if (Collection.HasCard(k)) pubOwned++;
            var cntRt = MRect(root, "Cnt", 950f, 186f, 1030f, 226f);
            var cnt = CoastOrnate.Label(cntRt, "T", $"{pubOwned}/{pubTotal}", 12, new Color(0.79f, 0.67f, 0.47f)); cnt.fontStyle = FontStyle.Bold;
            Stretch(cnt.rectTransform);
            // 52차(사용자): 「AI 생성 이미지」 표시 — 프레임 상단 띠 오른쪽(SSR 배지 반대편)
            var aiRt = MRect(root, "AiTag", 560f, 266f, 1010f, 322f);
            var aiPill = CoastUiArt.CutePill(aiRt, "Pill", new Color(0.16f, 0.10f, 0.30f, 0.92f), 14, 2); aiPill.raycastTarget = false;
            Stretch(aiPill.rectTransform);
            var aiL = CoastOrnate.Label(aiPill.transform, "T", Loc.T("✦ AI 생성 이미지 · 가상 인물", "✦ AI-generated image · virtual"), 11, new Color(0.95f, 0.85f, 0.55f)); aiL.fontStyle = FontStyle.Bold;
            Stretch(aiL.rectTransform);

            // 다음 3장 슬롯 — 시안 x [100,387][437,717][775,1053] · y 1350~1786
            float[] sx0 = { 100f, 437f, 775f }, sx1 = { 387f, 717f, 1053f };
            for (int i = 0; i < 3; i++)
            {
                int sid = id + 1 + i;
                if (sid > PhotocardTable.Count) sid = PublicFrom + ((sid - PublicFrom) % pubTotal);
                if (sid == id) continue;
                var slot = MRect(root, "Slot" + i, sx0[i], 1350f, sx1[i], 1800f);
                var hit = slot.gameObject.AddComponent<Image>(); hit.color = new Color(1f, 1f, 1f, 0f); hit.raycastTarget = true;
                bool sHas = Collection.HasCard(sid);
                var sCard = PhotocardTable.Get(sid); var sGrade = PhotocardTable.GradeOf(sid);
                if (sHas)
                {
                    var frame = CoastUiArt.CutePill(slot, "Frame", new Color(0.99f, 0.97f, 0.93f), 12, 3); frame.raycastTarget = false;
                    Place(frame.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    frame.rectTransform.offsetMin = new Vector2(6f, 6f); frame.rectTransform.offsetMax = new Vector2(-6f, -6f);
                    var th = CoastHudLayout.MakeImage(frame.transform, "Img", Vector2.zero, Vector2.one, new Vector2(8f, 42f), new Vector2(-8f, -8f), Color.white);
                    var stex = ArtAssets.LoadTexture(sCard.Image) ?? ArtAssets.LoadTexture(sCard.FallbackImage);
                    if (stex != null) { th.sprite = CoastUiArt.AsSprite(stex, 100f); th.preserveAspect = true; }
                    var gb = CoastUiArt.CutePill(frame.transform, "G", Collection.GradeColor(sGrade), 9, 2); gb.raycastTarget = false;
                    Place(gb.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -18f), new Vector2(44f, 20f));
                    var gt = CoastOrnate.Label(gb.transform, "T", Collection.GradeName(sGrade), 10, Color.white); gt.fontStyle = FontStyle.Bold;
                    var nm = CoastOrnate.Label(frame.transform, "N", $"{sid:00}  {sCard.Name}", 11, new Color(0.3f, 0.2f, 0.35f));
                    Place(nm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 30f)); nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                    if (Collection.CardIsNew(sid))
                    {
                        var n = CoastUiArt.Panel(frame.transform, "New", CoastOrnate.Red, 8); n.raycastTarget = false;
                        Place(n.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -16f), new Vector2(40f, 18f));
                        CoastOrnate.Label(n.transform, "T", "NEW", 10, Color.white);
                    }
                }
                else
                {
                    // 시안의 자물쇠·??? 그대로 두고 등급만 살짝
                    var gt = CoastOrnate.Label(slot, "G", Collection.GradeName(sGrade), 11, new Color(1f, 0.85f, 0.45f, 0.75f));
                    Place(gt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(0f, 22f));
                }
                int cid = sid; bool ch = sHas;
                var b = slot.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { if (ch) ShowMockCard(cid); else Toast(Loc.T($"잠김 · [{Collection.GradeName(sGrade)}] 러닝 중 포토카드 아이템에서 {Collection.GradeWeights[(int)sGrade]}%", $"Locked · [{Collection.GradeName(sGrade)}] {Collection.GradeWeights[(int)sGrade]}% from photocard items")); });
            }

            // 버튼(시안 그림 위 투명 히트 영역): 이미지 저장 공유 / → 바인더 / ← 닫기
            var capture = MRect(root, "Capture", 215f, 250f, 905f, 1200f);   // 저장·공유용 캡처 영역(폴라로이드+프레임)
            MockButton(root, "Share", 83f, 1825f, 558f, 1933f, () => { if (has) StartCoroutine(SaveCardImage(capture, card)); else Toast(Loc.T("아직 없는 카드예요", "You don't have this card yet")); });
            MockButton(root, "Binder", 608f, 1825f, 1067f, 1933f, () => { Destroy(_mock); _mock = null; Refresh(); });
            MockButton(root, "Close", 317f, 1983f, 833f, 2075f, Close);

            if (isNew && has) { CoastAudioManager.PlayAnywhere(CoastSfx.CardReveal); StartCoroutine(Reveal(photoRt)); _askReviewAfter = true; }
        }

        private static void MockButton(Transform parent, string name, float x0, float y0, float x1, float y1, Action onClick)
        {
            var rt = MRect(parent, name, x0, y0, x1, y1);
            var im = rt.gameObject.AddComponent<Image>(); im.color = new Color(1f, 1f, 1f, 0f); im.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
        }
    }
}
