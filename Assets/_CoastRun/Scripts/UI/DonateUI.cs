using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 52차(사용자): 「기부부탁」 — 타이틀 우상단에 반짝이며 떠 있는 커피잔 아이콘(다른 페이지에선 숨김) + 팝업.
    ///   첫 로딩엔 팝업이 저절로 한 번 뜬다(Donation.PopupSeen). 팝업: 개발자 메시지 → 선물 고르기(히든 트랙 / 모든 게임 열림 패스코드 / 안 받기) → 구글 결제 $3.
    public static class DonateUI
    {
        private static Canvas _canvas;
        private static RectTransform _root;
        private static Action _onClose;
        private static Donation.Gift _gift = Donation.Gift.HiddenTrack;
        private static readonly Image[] _giftPills = new Image[3];
        private static Text _cups, _status;
        private static Button _payBtn;
        public static bool IsOpen => _canvas != null;

        private static readonly Color Ink = new Color(0.20f, 0.14f, 0.10f);
        private static readonly Color Cream = new Color(0.99f, 0.96f, 0.90f);
        private static readonly Color Coffee = new Color(0.45f, 0.28f, 0.16f);
        private static readonly Color Rose = new Color(0.93f, 0.22f, 0.52f);
        private static readonly Color PillOff = new Color(0.93f, 0.88f, 0.80f);
        private static readonly Color PillOn = new Color(1f, 0.80f, 0.35f);

        // ── 우상단 떠 있는 아이콘 ──
        public static GameObject AttachIcon(Transform titleUi, Func<bool> visible, Action onTap)
        {
            var go = new GameObject("DonateIcon", typeof(RectTransform), typeof(Image), typeof(Button), typeof(DonateIconAnim));
            go.transform.SetParent(titleUi, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-78f, -286f); rt.sizeDelta = new Vector2(124f, 124f);
            var img = go.GetComponent<Image>();
            var art = ArtAssets.LoadTexture("UI_Donate_Cup");
            if (art != null) { img.sprite = CoastUiArt.AsSprite(art); img.preserveAspect = true; img.color = Color.white; }
            else { img.sprite = CoastUiArt.RoundedRect(40); img.type = Image.Type.Sliced; img.color = new Color(1f, 0.86f, 0.45f, 0.95f); }
            img.raycastTarget = true;
            if (art == null)
            {
                var glyph = CoastHudLayout.MakeText(rt, "Glyph", "☕", 46, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 10f), new Vector2(0f, 0f));
                glyph.color = Coffee;
            }
            // 라벨 「기부부탁」
            var tag = CoastUiArt.CutePill(rt, "Tag", Rose, 12, 2); tag.raycastTarget = false;
            var trt = tag.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f); trt.pivot = new Vector2(0.5f, 0.5f); trt.anchoredPosition = new Vector2(0f, 2f); trt.sizeDelta = new Vector2(104f, 30f);
            var tl = CoastHudLayout.MakeText(trt, "T", Loc.T("기부부탁", "Donate?"), 15, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            tl.color = Color.white; tl.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(tl, new Color(0f, 0f, 0f, 0.35f), 1.2f);
            // 반짝이 4개
            var anim = go.GetComponent<DonateIconAnim>();
            anim.visible = visible;
            for (int i = 0; i < 4; i++)
            {
                var sp = CoastHudLayout.MakeText(rt, "Spark" + i, "✦", 18 + (i % 2) * 8, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-16f, -16f), new Vector2(16f, 16f));
                sp.color = new Color(1f, 0.95f, 0.6f); sp.raycastTarget = false;
                anim.sparks[i] = sp;
            }
            var b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onTap?.Invoke(); });
            return go;
        }

        /// 둥실 + 맥동 + 반짝이 회전. visible() 이 false 면 숨긴다(다른 페이지).
        public class DonateIconAnim : MonoBehaviour
        {
            public Func<bool> visible;
            public readonly Text[] sparks = new Text[4];
            private CanvasGroup _cg; private float _t;
            private void Awake() { _cg = gameObject.AddComponent<CanvasGroup>(); }
            private void Update()
            {
                bool on = visible == null || visible();
                _cg.alpha = Mathf.MoveTowards(_cg.alpha, on ? 1f : 0f, Time.unscaledDeltaTime * 6f);
                _cg.blocksRaycasts = on; _cg.interactable = on;
                if (!on) return;
                _t += Time.unscaledDeltaTime;
                float pulse = 1f + Mathf.Sin(_t * 3.2f) * 0.05f;
                transform.localScale = Vector3.one * pulse;
                var rt = (RectTransform)transform;
                rt.anchoredPosition = new Vector2(-78f, -286f + Mathf.Sin(_t * 1.6f) * 6f);
                for (int i = 0; i < sparks.Length; i++)
                {
                    var s = sparks[i]; if (s == null) continue;
                    float a = _t * 1.1f + i * Mathf.PI * 0.5f;
                    s.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * 66f, 10f + Mathf.Sin(a) * 52f);
                    float tw = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(_t * 4f + i * 1.3f));
                    s.color = new Color(1f, 0.95f, 0.6f, tw);
                    s.transform.localScale = Vector3.one * (0.7f + 0.5f * tw);
                }
            }
        }

        // ── 팝업 ──
        public static void Open(Action onClose = null)
        {
            Close();
            _onClose = onClose;
            Donation.MarkPopupSeen();
            _canvas = CoastUiCanvas.Create("DonateCanvas", 472);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad - 400f, -pad - 400f), new Vector2(pad + 400f, pad + 400f), new Color(0.05f, 0.03f, 0.08f, 0.72f));
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>(); dimBtn.transition = Selectable.Transition.None; dimBtn.onClick.AddListener(Close);

            var card = CoastUiArt.CutePill(_root, "Card", Cream, 30, 6);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 10f); crt.sizeDelta = new Vector2(640f, 1010f); card.raycastTarget = true;

            // 머리: 커피잔 + 제목
            var cup = ArtAssets.LoadTexture("UI_Donate_Cup");
            var head = CoastHudLayout.MakeImage(crt, "Cup", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -150f), new Vector2(60f, -30f), Color.white);
            if (cup != null) { head.sprite = CoastUiArt.AsSprite(cup); head.preserveAspect = true; }
            else { head.sprite = CoastUiArt.RoundedRect(40); head.type = Image.Type.Sliced; head.color = new Color(1f, 0.86f, 0.45f); var g = CoastHudLayout.MakeText(head.rectTransform, "G", "☕", 56, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 8f), Vector2.zero); g.color = Coffee; }
            head.raycastTarget = false;
            var title = CoastHudLayout.MakeText(crt, "Title", Loc.T("커피 한 잔 값, 기부 부탁드려요 ☕", "A coffee's worth — please donate ☕"), 24, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -200f), new Vector2(-20f, -156f));
            title.color = Ink; title.fontStyle = FontStyle.Bold; title.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 본문(개발자 메시지)
            string bodyKo = "이 게임은 잠깐 짬짬이 하는 러닝 게임에까지 광고와 현질이 넘쳐나는 게 짜증났던 개발자가 만들었어요. 그래서 광고도, 강제 결제도 없습니다.\n\n" +
                            "또 하나의 목적은 K-POP을 전 세계에 널리 알리는 것. 가상 듀오 우히&히시의 노래를 달리면서 들어 주세요.\n\n" +
                            "다만 꾸준한 업데이트를 위해 커피 한 잔 값(" + Donation.PriceLabel + ") 정도 기부해 주시면 더 감사하겠습니다. 기부하신 분께는 작은 선물이 있어요 — 여러 번 기부하셔도 좋아요.";
            string bodyEn = "This game was made by a developer fed up with ads and paywalls even in quick pick-up-and-play runners. So: no ads, no forced purchases.\n\n" +
                            "The other goal is to spread K-POP worldwide — run to the songs of the virtual duo Woohee & Heesi.\n\n" +
                            "To keep the updates coming, a coffee's worth (" + Donation.PriceLabel + ") would mean a lot. Donors get a small gift — and you can donate more than once.";
            var body = CoastHudLayout.MakeText(crt, "Body", Loc.T(bodyKo, bodyEn), 15, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -520f), new Vector2(-34f, -206f));
            body.color = new Color(0.30f, 0.22f, 0.16f); body.horizontalOverflow = HorizontalWrapMode.Wrap; body.verticalOverflow = VerticalWrapMode.Truncate; body.lineSpacing = 1.25f;

            // 선물 고르기
            var gl = CoastHudLayout.MakeText(crt, "GiftLabel", Loc.T("선물 고르기", "Pick your gift"), 15, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -556f), new Vector2(-34f, -530f));
            gl.color = Coffee; gl.fontStyle = FontStyle.Bold;
            string[] giftKo = { "♪  히든 트랙 2곡 (레코드 + K-POP 런)", "★  모든 게임 열림 (히든 패스코드)", "♥  아무것도 안 받을래요" };
            string[] giftEn = { "♪  2 hidden tracks (records + K-POP run)", "★  Everything unlocked (hidden passcode)", "♥  Nothing, thanks" };
            Donation.Gift[] kinds = { Donation.Gift.HiddenTrack, Donation.Gift.UnlockAll, Donation.Gift.None };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var pill = CoastUiArt.CutePill(crt, "Gift" + i, PillOff, 16, 3);
                var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f); prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -562f - i * 60f); prt.sizeDelta = new Vector2(572f, 52f); pill.raycastTarget = true;
                var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); _gift = kinds[idx]; RefreshGifts(); });
                var t = CoastHudLayout.MakeText(prt, "T", Loc.T(giftKo[i], giftEn[i]), 16, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(22f, 2f), new Vector2(-16f, 0f));
                t.color = Ink; t.fontStyle = FontStyle.Bold;
                _giftPills[i] = pill;
            }
            _gift = Donation.Gift.HiddenTrack; RefreshGifts();

            // 결제 버튼
            var pay = CoastUiArt.GlossyPill(crt, "Pay", Rose, 26, 10);
            var pyt = pay.rectTransform; pyt.anchorMin = pyt.anchorMax = new Vector2(0.5f, 0f); pyt.pivot = new Vector2(0.5f, 0f); pyt.anchoredPosition = new Vector2(0f, 118f); pyt.sizeDelta = new Vector2(520f, 78f); pay.raycastTarget = true;
            _payBtn = pay.gameObject.AddComponent<Button>(); _payBtn.transition = Selectable.Transition.None;
            _payBtn.onClick.AddListener(Pay);
            var pt = CoastHudLayout.MakeText(pyt, "T", Loc.T($"☕ 커피 한 잔 기부하기 · {Donation.PriceLabel}", $"☕ Buy me a coffee · {Donation.PriceLabel}"), 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 2f));
            pt.color = Color.white; pt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(pt, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            var ps = CoastHudLayout.MakeText(crt, "PaySub", Loc.T("Google Play 결제 · 자율 기부", "Google Play billing · voluntary"), 12, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 96f), new Vector2(0f, 116f));
            ps.color = new Color(0.45f, 0.38f, 0.32f);

            // 다음에 / 잔 수
            var later = CoastUiArt.CutePill(crt, "Later", new Color(0.86f, 0.82f, 0.76f), 18, 3);
            var lrt = later.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f); lrt.pivot = new Vector2(0.5f, 0f); lrt.anchoredPosition = new Vector2(0f, 40f); lrt.sizeDelta = new Vector2(240f, 50f); later.raycastTarget = true;
            var lb = later.gameObject.AddComponent<Button>(); lb.transition = Selectable.Transition.None; lb.onClick.AddListener(() => { CoastPrefs.Vibrate(); Close(); });
            var lt = CoastHudLayout.MakeText(lrt, "T", Loc.T("다음에 할게요", "Maybe later"), 17, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            lt.color = Ink; lt.fontStyle = FontStyle.Bold;
            _cups = CoastHudLayout.MakeText(crt, "Cups", "", 13, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(0f, 34f));
            _cups.color = Coffee; _cups.fontStyle = FontStyle.Bold;
            _status = CoastHudLayout.MakeText(crt, "Status", "", 14, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 200f), new Vector2(-20f, 250f));
            _status.color = new Color(0.1f, 0.5f, 0.25f); _status.fontStyle = FontStyle.Bold; _status.horizontalOverflow = HorizontalWrapMode.Wrap;
            RefreshCups();
        }

        private static void RefreshGifts()
        {
            Donation.Gift[] kinds = { Donation.Gift.HiddenTrack, Donation.Gift.UnlockAll, Donation.Gift.None };
            for (int i = 0; i < 3; i++) if (_giftPills[i] != null) _giftPills[i].color = kinds[i] == _gift ? PillOn : PillOff;
        }

        private static void RefreshCups()
        {
            if (_cups == null) return;
            int n = Donation.Cups;
            string extra = "";
            if (Donation.HiddenTrack) extra += Loc.T(" · 히든 트랙 열림", " · hidden tracks on");
            if (Donation.AllOpen) extra += Loc.T($" · 패스코드 {Donation.DonorPasscode}", $" · passcode {Donation.DonorPasscode}");
            _cups.text = n > 0 ? Loc.T($"지금까지 {n}잔 ☕ 고마워요{extra}", $"{n} cup(s) so far ☕ thank you{extra}") : Loc.T("아직 0잔 — 광고 없이 만들고 있어요", "0 cups so far — made without ads");
        }

        private static void Pay()
        {
            if (_payBtn == null) return;
            CoastPrefs.Vibrate();
            _payBtn.interactable = false;
            if (_status != null) _status.text = Loc.T("결제 창을 여는 중…", "Opening store…");
            var gift = _gift;
            Donation.Donate(gift, ok =>
            {
                if (_canvas == null) return;
                _payBtn.interactable = true;
                if (!ok) { _status.text = Loc.T("결제가 취소됐거나 실패했어요. 괜찮아요!", "Payment cancelled or failed — that's okay!"); _status.color = new Color(0.6f, 0.2f, 0.2f); return; }
                _status.color = new Color(0.1f, 0.5f, 0.25f);
                switch (gift)
                {
                    case Donation.Gift.HiddenTrack: _status.text = Loc.T("고마워요! ♪ 히든 트랙 2곡이 레코드와 K-POP 런에 열렸어요", "Thank you! ♪ 2 hidden tracks unlocked in Records and the K-POP run"); break;
                    case Donation.Gift.UnlockAll: _status.text = Loc.T($"고마워요! ★ 모든 게임이 열렸어요\n히든 패스코드 {Donation.DonorPasscode} — 다른 기기에선 설정 › 비밀코드에 입력", $"Thank you! ★ Everything unlocked\nHidden passcode {Donation.DonorPasscode} — enter it in Settings › Secret code on another device"); break;
                    default: _status.text = Loc.T("고마워요! ♥ 그 마음만으로 충분해요", "Thank you! ♥ That means a lot"); break;
                }
                CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.7f);
                RefreshCups();
            });
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null; _cups = null; _status = null; _payBtn = null;
            var cb = _onClose; _onClose = null; cb?.Invoke();
        }
    }
}
