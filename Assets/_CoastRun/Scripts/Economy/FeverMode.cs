using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 23차-9: 꼬마(집사) 도움 — 달리는 중 가끔 화면 오른쪽에 꼬마 얼굴 버튼이 떠오르고,
    /// 누르면 3초 동안 피버: 주변 코인·말랑이가 전부 빨려 들어온다(자석 반경 ↑, 속도선, 채도 킥, "FEVER!").
    public class FeverMode : MonoBehaviour
    {
        public static FeverMode Instance { get; private set; }
        public static bool Active => Instance != null && Instance._until > Time.time;
        /// 피버 중 자석 반경 보정(코인·말랑이 Update에서 더한다).
        public static float MagnetBonus => Active ? 14f : 0f;

        public float Duration = 3f;
        public float FirstOfferAfter = 9f;     // 스테이지 시작 후 첫 제안
        public float OfferEvery = 24f;         // 그 다음부터 간격
        public float OfferWindow = 6f;         // 버튼이 떠 있는 시간

        private float _until = -1f;
        private float _nextOffer;
        private float _offerUntil = -1f;
        private RectTransform _btn; private Image _face; private Text _label; private CanvasGroup _cg;
        private Canvas _canvas;
        private PlayerController _player;

        public static FeverMode Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("FeverMode");
            Instance = go.AddComponent<FeverMode>();
            return Instance;
        }

        private void Awake()
        {
            Instance = this;
            _nextOffer = Time.time + FirstOfferAfter;
            BuildButton();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void BuildButton()
        {
            _canvas = CoastUiCanvas.Create("FeverCanvas", 55, transform);
            var root = _canvas.GetComponent<RectTransform>();
            var go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            _btn = go.GetComponent<RectTransform>();
            _btn.anchorMin = _btn.anchorMax = new Vector2(1f, 0.5f); _btn.pivot = new Vector2(1f, 0.5f);
            _btn.anchoredPosition = new Vector2(-14f, 120f); _btn.sizeDelta = new Vector2(150f, 150f);
            _face = go.GetComponent<Image>();
            var tex = ArtAssets.LoadTexture("UI_Face_Butler") ?? ArtAssets.LoadTexture("UI_Face_Boy");
            if (tex != null) _face.sprite = CoastUiArt.AsSprite(tex);
            _face.preserveAspect = true;
            _cg = go.GetComponent<CanvasGroup>();
            var b = go.GetComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(OnPressed);
            // 말풍선 "도와줄까?"
            var pill = CoastUiArt.CutePill(go.transform, "Bubble", Color.white, 16, 3);
            var prt = pill.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(1f, 0.5f);
            prt.anchoredPosition = new Vector2(-6f, -30f); prt.sizeDelta = new Vector2(170f, 50f); pill.raycastTarget = false;
            _label = CoastHudLayout.MakeText(prt, "T", Loc.T("도와줄까요?", "Need a hand?"), 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            _label.color = new Color(0.25f, 0.12f, 0.2f); _label.fontStyle = FontStyle.Bold; _label.raycastTarget = false;
            // 손가락 힌트: 작은 "TAP" 배지
            var tap = CoastUiArt.CutePill(go.transform, "Tap", new Color(1f, 0.55f, 0.28f), 12, 2);
            var trt = tap.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f); trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 4f); trt.sizeDelta = new Vector2(76f, 30f); tap.raycastTarget = false;
            var tl = CoastHudLayout.MakeText(trt, "T", "TAP!", 17, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tl.color = Color.white; tl.fontStyle = FontStyle.Bold; tl.raycastTarget = false;
            go.SetActive(false);
        }

        private void Update()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            bool running = _player != null && _player.enabled && _player.State == SkateState.Run && !ArcadeRun.Active
                           && (StageManager.Instance == null || StageManager.Instance.IsStageActive);
            if (!Active && running && Time.time >= _nextOffer && _offerUntil < 0f)
            {
                _offerUntil = Time.time + OfferWindow;
                _btn.gameObject.SetActive(true);
                StartCoroutine(SimpleTween.PunchScale(_btn, 0.35f, 0.3f));
                CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss);
            }
            if (_offerUntil > 0f)
            {
                float left = _offerUntil - Time.time;
                if (left <= 0f || !running) { HideOffer(); }
                else
                {
                    // 콩콩 튀며 시선 끌기, 마지막 1.5초는 깜빡임
                    float bob = Mathf.Sin(Time.time * 6f) * 6f;
                    _btn.anchoredPosition = new Vector2(-14f, 120f + bob);
                    _cg.alpha = left < 1.5f ? (Mathf.Sin(Time.time * 18f) > 0f ? 1f : 0.35f) : 1f;
                }
            }
            if (CoastRemoteKeys.Down(KeyCode.H) && running) Trigger();   // 에디터/원격 디버그
        }

        private void HideOffer()
        {
            _offerUntil = -1f;
            _btn.gameObject.SetActive(false);
            _nextOffer = Time.time + OfferEvery;
        }

        private void OnPressed()
        {
            if (Active) return;
            HideOffer();
            Trigger();
        }

        public void Trigger()
        {
            _until = Time.time + Duration;
            _nextOffer = Time.time + OfferEvery;
            StartCoroutine(FeverFx());
        }

        private IEnumerator FeverFx()
        {
            var juice = JuiceDirector.Instance;
            juice?.OnFeverStart();
            PickupFloat.Banner(Loc.T("FEVER!", "FEVER!"), new Color(1f, 0.85f, 0.25f), Duration);
            CoastPrefs.Vibrate();
            while (Active)
            {
                yield return null;
            }
            juice?.OnFeverEnd();
        }
    }
}
