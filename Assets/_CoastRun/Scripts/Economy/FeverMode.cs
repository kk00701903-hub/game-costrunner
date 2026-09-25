using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 꼬마(집사) 도움 — 달리는 중 가끔 오른쪽에 꼬마 얼굴이 뜨고,
    /// 누르면 피버: 주변 코인·말랑이가 강하게 빨려 들어온다.
    ///
    /// 90차(사용자 "피버 안 눌렀는데 돈이 모인다"): 제안 얼굴이 화면 오른쪽 42 % 높이에 있는데
    /// 러닝의 "오른쪽 레인" 탭 영역(화면 우 35 %)과 겹쳐서, 레인 이동 탭·스와이프가 그대로
    /// 피버 버튼 클릭이 되었다(코인 카펫 = 돈이 쏟아지는 느낌). 그래서
    ///   1) 클릭을 Button 대신 TapGate 로 받아 **짧고 안 움직인 탭**만 피버로 인정하고,
    ///   2) 얼굴 위에서 시작한 스와이프는 MobileSwipeInput 이 그대로 조작으로 쓰게 열어 주고,
    ///   3) 얼굴이 막 뜬 직후 0.25 초는 입력을 받지 않는다(반사 탭 방지).
    public class FeverMode : MonoBehaviour
    {
        public static FeverMode Instance { get; private set; }
        public static bool Active => Instance != null && Instance._until > Time.time;
        /// 피버 중 자석 반경 보정(코인·말랑이 Update에서 더한다). 전 레인·전방 넉넉히.
        public static float MagnetBonus => Active ? 26f : 0f;
        /// 피버 흡입: 경로 앞쪽(m) / 좌우(m, 3레인 전체).
        public const float PullAhead = 28f;
        public const float PullBehind = 2.5f;
        public const float PullLateral = 4.6f;

        public float Duration = 6f;
        public float FirstOfferAfter = 8f;
        public float OfferEvery = 22f;
        public float OfferWindow = 7f;

        private float _until = -1f;
        private float _nextOffer;
        private float _offerUntil = -1f;
        private RectTransform _btn;
        private Image _face;
        private CanvasGroup _cg;
        private Canvas _canvas;
        private PlayerController _player;
        private Vector2 _btnBasePos;
        private CanvasGroup _tapCg;   // 40차-b: TAP! 깜빡임
        private RectTransform _tapPill;
        private float _tapArmedAt = -1f;   // 제안이 뜬 뒤 이 시각부터 탭을 받는다

        /// 지금 손가락이 제안 얼굴(또는 TAP! 알약) 위인가 — MobileSwipeInput 이 묻는다.
        /// 여기서 시작한 스와이프는 그대로 조작으로 쓰고(레인 이동이 먹히지 않던 문제),
        /// 짧은 탭만 피버로 준다.
        public static bool PointerOverOffer(Vector2 screenPos)
        {
            var f = Instance;
            if (f == null || f._offerUntil < 0f || f._btn == null || !f._btn.gameObject.activeInHierarchy) return false;
            var cam = f._canvas != null && f._canvas.renderMode != RenderMode.ScreenSpaceOverlay ? f._canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(f._btn, screenPos, cam)) return true;
            return f._tapPill != null && RectTransformUtility.RectangleContainsScreenPoint(f._tapPill, screenPos, cam);
        }

        public static FeverMode Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("FeverMode");
            Instance = go.AddComponent<FeverMode>();
            return Instance;
        }

        /// 스테이지 시작/재도전 때 제안 타이머를 다시 맞춘다.
        public void ArmForStage()
        {
            HideOfferQuiet();
            _until = -1f;
            _nextOffer = Time.time + FirstOfferAfter;
            RestoreInvincible();
        }

        /// 클리어 UI 등 — 제안 버튼만 즉시 숨긴다(타이머는 유지).
        public void DismissOffer() => HideOfferQuiet();

        /// 48차: 다음 프레임에 바로 제안(K-POP 후렴 진입).
        public void ForceOffer() { if (!Active) { HideOfferQuiet(); _nextOffer = Time.time; } }

        private void RestoreInvincible()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player == null) return;
            _player.Invincible = GiantMode.Active || PlayerController.DebugGod;
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
            // HUD(100)·여정(105)·주스(110)보다 위 — 탭이 스와이프/다른 UI에 먹히지 않게
            _canvas = CoastUiCanvas.Create("FeverCanvas", 160, transform);
            var root = CoastUiCanvas.Root(_canvas);
            var go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            _btn = go.GetComponent<RectTransform>();
            _btn.anchorMin = _btn.anchorMax = new Vector2(1f, 0.42f);
            _btn.pivot = new Vector2(1f, 0.5f);
            // 기존 150 → 150% = 225 → 40차-b(사용자): 30% 축소 = 158
            _btn.sizeDelta = new Vector2(158f, 158f);
            // 러닝 TAP 옆 꼬마 얼굴 — ~3cm 위로(디자인 px ≈ 40/cm)
            _btnBasePos = new Vector2(-10f, 120f);
            _btn.anchoredPosition = _btnBasePos;

            _face = go.GetComponent<Image>();
            var tex = ArtAssets.LoadTexture("UI_Face_Butler")
                      ?? ArtAssets.LoadTexture("UI_Butler_Bust")
                      ?? ArtAssets.LoadTexture("UI_Face_Boy");
            if (tex != null) _face.sprite = CoastUiArt.AsSprite(tex);
            _face.preserveAspect = true;
            _face.raycastTarget = true;
            _face.color = Color.white;

            _cg = go.GetComponent<CanvasGroup>();
            _cg.blocksRaycasts = true;
            _cg.interactable = true;

            // Button 은 손가락이 얼마나 끌렸든 뗄 때 클릭이 되어, 레인 스와이프가 피버로 새어 들어왔다.
            var gate = go.AddComponent<TapGate>();
            gate.OnTap = OnPressed;

            // 40차-b(사용자): TAP! 알약은 얼굴과 분리해 **얼굴 아래**에, 깜빡인다(_tapCg). 알약도 눌리게(Button 자식 → 부모로 버블).
            var tap = CoastUiArt.CutePill(go.transform, "Tap", new Color(1f, 0.55f, 0.28f), 12, 2);
            var trt = tap.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -6f);
            trt.sizeDelta = new Vector2(88f, 34f);
            tap.raycastTarget = true;
            _tapPill = trt;
            _tapCg = tap.gameObject.AddComponent<CanvasGroup>();
            var tl = CoastHudLayout.MakeText(trt, "T", "TAP!", 18, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tl.color = Color.white;
            tl.fontStyle = FontStyle.Bold;
            tl.raycastTarget = true;

            go.SetActive(false);
        }

        private bool IsRacing()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player == null || !_player.enabled) return false;
            if (_player.State == SkateState.Finish) return false;
            if (ArcadeRun.Active && !ArcadeRun.KpopMode) return false;   // 48차: K-POP 한 곡 달리기는 후렴에서 제안
            if (StageManager.Instance == null || !StageManager.Instance.IsStageActive) return false;
            if (RunHudChrome.Instance != null && RunHudChrome.Instance.IsPaused) return false;
            return true;
        }

        private void Update()
        {
            bool racing = IsRacing();
            if (KpopTutorial.HoldFever && !Active && _offerUntil < 0f) _nextOffer = Mathf.Max(_nextOffer, Time.time + 0.5f);   // 194차
            if (!Active && racing && Time.time >= _nextOffer && _offerUntil < 0f)
            {
                _offerUntil = Time.time + OfferWindow;
                _tapArmedAt = Time.unscaledTime + 0.25f;   // 막 뜬 순간의 반사 탭은 피버로 세지 않는다
                _btn.gameObject.SetActive(true);
                _cg.alpha = 1f;
                _cg.blocksRaycasts = true;
                StartCoroutine(SimpleTween.PunchScale(_btn, 0.35f, 0.3f));
                CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss);
            }
            if (_offerUntil > 0f)
            {
                float left = _offerUntil - Time.time;
                if (left <= 0f || !racing) { HideOffer(); }
                else
                {
                    float bob = Mathf.Sin(Time.time * 6f) * 8f;
                    _btn.anchoredPosition = _btnBasePos + new Vector2(0f, bob);
                    if (_tapCg != null) _tapCg.alpha = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(Time.time * 5f));   // TAP! 깜빡임(초당 ~1.6회)
                    // 깜빡여도 레이캐스트는 유지(알파만 살짝)
                    _cg.alpha = left < 1.5f ? (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 14f))) : 1f;
                    _cg.blocksRaycasts = true;
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 디버그 키는 에디터·개발 빌드에서만 — 배포 빌드에서 키보드/원격으로 피버가 켜지지 않게.
            if (CoastRemoteKeys.Down(KeyCode.H) && racing) Trigger("debugKey");
#endif
        }

        private void HideOfferQuiet()
        {
            _offerUntil = -1f;
            _tapArmedAt = -1f;
            if (_btn != null) _btn.gameObject.SetActive(false);
        }

        private void HideOffer()
        {
            HideOfferQuiet();
            _nextOffer = Time.time + OfferEvery;
        }

        private void OnPressed()
        {
            if (Active) return;
            if (_offerUntil < 0f) return;
            if (_tapArmedAt > 0f && Time.unscaledTime < _tapArmedAt) return;
            HideOffer();
            Trigger("tap");
        }

        public void Trigger(string by = "code")
        {
            Debug.Log($"[Fever] on by={by} t={Time.time:0.0}");   // "안 눌렀는데 켜졌다" 를 로그로 가릴 수 있게
            ArcadeRun.NoteFever();   // 48차: K-POP 미션(후렴에서 피버)
            _until = Time.time + Duration;
            _nextOffer = Time.time + OfferEvery;
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player != null) _player.Invincible = true;   // 피버 중 장애물 피해 없음
            VacuumNearby();
            StartCoroutine(FeverFx());
        }

        /// 피버 시작 즉시 주변 코인·말랑이·하트 등을 자석 상태로 켠다.
        private void VacuumNearby()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerController>();
            if (_player == null) return;
            var pt = _player.transform;
            foreach (var c in FindObjectsByType<CoinPickup>(FindObjectsSortMode.None))
                c?.BeginFeverPull(pt);
            foreach (var j in FindObjectsByType<JellyPickup>(FindObjectsSortMode.None))
                j?.BeginFeverPull(pt);
        }

        /// 경로 기준 피버 흡입 범위(전방·전 레인). 구형 자석보다 러너에 맞게.
        public static bool InPullRange(Transform player, Vector3 itemPos)
        {
            if (!Active || player == null) return false;
            float ahead = DownhillPath.DistanceAlong(itemPos) - DownhillPath.DistanceAlong(player.position);
            if (ahead < -PullBehind || ahead > PullAhead) return false;
            float lat = Mathf.Abs(Vector3.Dot(itemPos - player.position, DownhillPath.Rotation * Vector3.right));
            return lat <= PullLateral;
        }

        private IEnumerator FeverFx()
        {
            var juice = JuiceDirector.Instance;
            juice?.OnFeverStart();
            PickupFloat.Banner(Loc.T("FEVER!", "FEVER!"), new Color(1f, 0.85f, 0.25f), Duration);
            CoastPrefs.VibrateEvent();
            // 피버 중에도 새로 스폰된 아이템을 주기적으로 끌어당긴다
            float pulse = 0f;
            while (Active)
            {
                pulse += Time.deltaTime;
                if (pulse >= 0.35f)
                {
                    pulse = 0f;
                    VacuumNearby();
                }
                // 거인/갓과 겹쳐도 피버 동안은 무적 유지
                if (_player != null) _player.Invincible = true;
                yield return null;
            }
            RestoreInvincible();
            juice?.OnFeverEnd();
        }

        /// 짧고 거의 움직이지 않은 탭만 콜백을 부른다. 레인 스와이프(0.6 cm 이상 끌기)나
        /// 길게 누르기는 무시 — Button.onClick 은 그 둘을 구분하지 못해 피버가 저절로 켜졌다.
        private class TapGate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public System.Action OnTap;
            private const float MaxSeconds = 0.3f;
            private Vector2 _down;
            private float _t;
            private bool _armed;

            /// MobileSwipeInput 과 같은 물리 거리(0.6 cm) — 스와이프로 인정되는 거리면 탭이 아니다.
            private static float MaxMovePx
            {
                get
                {
                    float dpi = Screen.dpi > 1f ? Screen.dpi : 320f;
                    return 0.6f / 2.54f * dpi;
                }
            }

            public void OnPointerDown(PointerEventData e)
            {
                _down = e.position;
                _t = Time.unscaledTime;
                _armed = true;
            }

            public void OnPointerUp(PointerEventData e)
            {
                if (!_armed) return;
                _armed = false;
                if (Time.unscaledTime - _t > MaxSeconds) return;
                float move = MaxMovePx;
                if ((e.position - _down).sqrMagnitude > move * move) return;
                OnTap?.Invoke();
            }
        }
    }
}
