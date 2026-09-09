using UnityEngine;

namespace CoastRun
{
    /// One-finger swipe + hold, with the three things that separate "responsive" from
    /// "sluggish" in a lane runner:
    ///
    ///  1. Swipes fire the moment the finger crosses the threshold, not when it lifts.
    ///     The old version waited for TouchPhase.Ended, which on a real thumb adds
    ///     60–120 ms of felt latency to every single move.
    ///
    ///  2. Inputs are buffered for a short window instead of being wiped every frame.
    ///     A jump swiped 100 ms before landing used to vanish; now it fires on landing.
    ///     This is the single biggest contributor to "the game ate my input".
    ///
    ///  3. The threshold is in physical distance (cm via DPI), not raw pixels. 48 px is
    ///     a twitch on a 160-dpi phone and a full drag on a 480-dpi one.
    ///
    /// One finger can also chain swipes without lifting — swipe left, keep holding,
    /// swipe left again — by re-anchoring after each recognised gesture.
    public class MobileSwipeInput : MonoBehaviour, IInputReader
    {
        [Header("Recognition")]
        [Tooltip("Physical swipe distance in centimetres. ~0.6 cm is a firm flick.")]
        [SerializeField] private float swipeThresholdCm = 0.6f;
        [Tooltip("Fallback when the platform reports no DPI.")]
        [SerializeField] private float fallbackDpi = 320f;

        [Header("Buffering")]
        [Tooltip("How long a swipe stays valid waiting for the player to be able to act on it.")]
        [SerializeField] private float bufferSeconds = 0.25f;   // 14차-9: 선입력 버퍼 넉넉히

        private Vector2 _anchor;
        private Vector2 _lastPos;
        private bool _touchActive;
        private bool _touchOnUi;
        private float _touchStart;
        private bool _gestureLocked;   // fired once for this excursion; unlock when the finger pauses
        private float _thresholdPx;
        private float _settlePx;       // per-frame movement below this counts as "paused"

        // Buffered discrete inputs: time they were issued, or -1 for none.
        private float _laneStamp = -1f;
        private int _laneDir;
        private float _jumpStamp = -1f;
        private float _crouchStamp = -1f;

        private bool _crouchHeld;
        private bool _tuckHeld;

        public bool TuckHeld => _tuckHeld;
        public bool CrouchHeld => _crouchHeld;

        private void Awake()
        {
            float dpi = Screen.dpi > 1f ? Screen.dpi : fallbackDpi;
            _thresholdPx = swipeThresholdCm / 2.54f * dpi;
            _settlePx = _thresholdPx * 0.08f;
        }

        public void Tick()
        {
            _crouchHeld = false;
            _tuckHeld = false;

            PollKeyboard();
            PollTouch();
            ExpireBuffers();
        }

        // ────────────────────────────────────────────────────────────────
        // Consumption — each returns the buffered input once, then clears it.
        // ────────────────────────────────────────────────────────────────

        /// 23차: 원격(MCP) 입력 주입 — 실제 스와이프와 같은 버퍼를 탄다.
        public void Inject(int laneDir, bool jump, bool crouch)
        {
            float now = Time.unscaledTime;
            if (laneDir != 0) { _laneDir = laneDir; _laneStamp = now; }
            if (jump) _jumpStamp = now;
            if (crouch) _crouchStamp = now;
        }

        public int ConsumeLaneDelta()
        {
            if (_laneStamp < 0f)
                return 0;
            int d = _laneDir;
            _laneStamp = -1f;
            return d;
        }

        public bool ConsumeJump()
        {
            if (_jumpStamp < 0f)
                return false;
            _jumpStamp = -1f;
            return true;
        }

        public bool ConsumeCrouch()
        {
            if (_crouchStamp < 0f)
                return false;
            _crouchStamp = -1f;
            return true;
        }

        /// Whether a jump is waiting in the buffer — lets the player peek without eating it.
        public bool JumpPending => _jumpStamp >= 0f;

        // ────────────────────────────────────────────────────────────────

        private void ExpireBuffers()
        {
            float now = Time.unscaledTime;
            if (_laneStamp >= 0f && now - _laneStamp > bufferSeconds) _laneStamp = -1f;
            if (_jumpStamp >= 0f && now - _jumpStamp > bufferSeconds) _jumpStamp = -1f;
            if (_crouchStamp >= 0f && now - _crouchStamp > bufferSeconds) _crouchStamp = -1f;
        }

        private void IssueLane(int dir)
        {
            _laneDir = dir;
            _laneStamp = Time.unscaledTime;
        }

        private void IssueJump() => _jumpStamp = Time.unscaledTime;
        private void IssueCrouch() => _crouchStamp = Time.unscaledTime;

        // 25차-3: 키보드/게임패드 입력 보강 — 모바일에 블루투스 키보드를 붙이면 화살표가 KeyCode 로 안 오고
        // 축(Horizontal/Vertical, D-Pad)으로만 오는 기기가 있다. 키 + 축(엣지 검출) + 키패드까지 전부 받는다.
        private int _axisXPrev, _axisYPrev;
        private static bool AxisOk(string name)
        {
            try { Input.GetAxisRaw(name); return true; } catch { return false; }
        }
        private static readonly bool HasHAxis = AxisOk("Horizontal"), HasVAxis = AxisOk("Vertical");

        private static bool KeyDown(params KeyCode[] keys)
        {
            for (int i = 0; i < keys.Length; i++) if (Input.GetKeyDown(keys[i])) return true;
            return false;
        }
        private static bool KeyHeld(params KeyCode[] keys)
        {
            for (int i = 0; i < keys.Length; i++) if (Input.GetKey(keys[i])) return true;
            return false;
        }

        // 38차: 키보드가 또 안 먹는 문제 — 세 겹으로 받는다.
        //  ① Input.GetKeyDown(키·키패드)  ② 축(Horizontal/Vertical) 엣지  ③ OnGUI 의 KeyDown 이벤트(에디터 포커스 상태·안드로이드 하드웨어 키보드처럼
        //  Input.GetKeyDown 이 놓치는 경우) — ③은 같은 프레임에 ①이 이미 받았으면 무시(중복 방지). 원격(MCP) 화살표 키도 여기로.
        private int _guiLane; private bool _guiJump, _guiCrouch; private int _guiFrame = -1;
        private int _keyLaneFrame = -9, _keyJumpFrame = -9, _keyCrouchFrame = -9;
        private void OnGUI()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow: case KeyCode.A: case KeyCode.Keypad4: _guiLane = -1; break;
                case KeyCode.RightArrow: case KeyCode.D: case KeyCode.Keypad6: _guiLane = 1; break;
                case KeyCode.UpArrow: case KeyCode.W: case KeyCode.Keypad8: case KeyCode.Space: _guiJump = true; break;
                case KeyCode.DownArrow: case KeyCode.S: case KeyCode.Keypad2: _guiCrouch = true; break;
                default: return;
            }
            _guiFrame = Time.frameCount;
        }

        private void PollKeyboard()
        {
            int ax = 0, ay = 0;
            if (HasHAxis) { float h = Input.GetAxisRaw("Horizontal"); ax = h > 0.5f ? 1 : h < -0.5f ? -1 : 0; }
            if (HasVAxis) { float v = Input.GetAxisRaw("Vertical");   ay = v > 0.5f ? 1 : v < -0.5f ? -1 : 0; }
            bool axLeft = ax < 0 && _axisXPrev >= 0, axRight = ax > 0 && _axisXPrev <= 0;
            bool axUp = ay > 0 && _axisYPrev <= 0, axDown = ay < 0 && _axisYPrev >= 0;
            _axisXPrev = ax; _axisYPrev = ay;

            bool kLeft = KeyDown(KeyCode.A, KeyCode.LeftArrow, KeyCode.Keypad4) || axLeft || CoastRemoteKeys.Down(KeyCode.LeftArrow);
            bool kRight = KeyDown(KeyCode.D, KeyCode.RightArrow, KeyCode.Keypad6) || axRight || CoastRemoteKeys.Down(KeyCode.RightArrow);
            bool kUp = KeyDown(KeyCode.W, KeyCode.UpArrow, KeyCode.Keypad8, KeyCode.Space, KeyCode.JoystickButton0) || axUp || CoastRemoteKeys.Down(KeyCode.UpArrow);
            bool kDown = KeyDown(KeyCode.S, KeyCode.DownArrow, KeyCode.Keypad2, KeyCode.JoystickButton1) || axDown || CoastRemoteKeys.Down(KeyCode.DownArrow);
            // ③ OnGUI 이벤트 — 지난 프레임에 잡힌 키. ①②가 이미 받았으면 버린다.
            // (OnGUI 는 Update 뒤에 오므로 ③은 한 프레임 늦게 보인다 → ①이 그 프레임에 이미 받았으면 중복이라 버린다)
            int now = Time.frameCount;
            if (kLeft || kRight) _keyLaneFrame = now;
            if (kUp) _keyJumpFrame = now;
            if (kDown) _keyCrouchFrame = now;
            bool guiFresh = _guiFrame >= 0 && now - _guiFrame <= 1;
            if (guiFresh)
            {
                if (_guiLane != 0 && !kLeft && !kRight && _keyLaneFrame != _guiFrame) { if (_guiLane < 0) kLeft = true; else kRight = true; }
                if (_guiJump && !kUp && _keyJumpFrame != _guiFrame) kUp = true;
                if (_guiCrouch && !kDown && _keyCrouchFrame != _guiFrame) kDown = true;
            }
            _guiLane = 0; _guiJump = false; _guiCrouch = false; _guiFrame = -1;

            if (kLeft) IssueLane(-1);
            else if (kRight) IssueLane(1);
            if (kUp) IssueJump();
            if (kDown) IssueCrouch();

            if (KeyHeld(KeyCode.S, KeyCode.DownArrow, KeyCode.Keypad2, KeyCode.JoystickButton1) || ay < 0)
                _crouchHeld = true;

            if (KeyHeld(KeyCode.LeftShift, KeyCode.RightShift, KeyCode.JoystickButton2))
                _tuckHeld = true;
        }

        private void PollTouch()
        {
            if (Input.touchCount == 0)
            {
                _touchActive = false;
                return;
            }

            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                _anchor = t.position;
                _lastPos = t.position;
                _touchActive = true;
                _gestureLocked = false;
                _touchStart = Time.unscaledTime;
                // 18차-3: 일시정지 등 UI 위에서 시작한 터치는 게임 조작으로 쓰지 않는다
                var es = UnityEngine.EventSystems.EventSystem.current;
                _touchOnUi = es != null && es.IsPointerOverGameObject(t.fingerId);
                return;
            }

            if (!_touchActive)
                return;

            if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
            {
                _touchActive = false;
                // 18차-3: 탭 폴백 — 스와이프 없이 짧게 톡(0.22 s, 이동 < 임계) 치면 화면 좌 1/3 = 왼쪽 레인,
                // 우 1/3 = 오른쪽 레인, 가운데 = 점프. 손이 큰 기기·장갑·젖은 손에서도 조작이 먹게.
                if (t.phase == TouchPhase.Ended && !_gestureLocked && !_touchOnUi
                    && Time.unscaledTime - _touchStart < 0.22f && (t.position - _anchor).magnitude < _thresholdPx)
                {
                    float fx = t.position.x / Mathf.Max(1f, Screen.width);
                    if (fx < 0.35f) IssueLane(-1);
                    else if (fx > 0.65f) IssueLane(1);
                    else IssueJump();
                }
                return;
            }
            if (_touchOnUi) return;

            Vector2 delta = t.position - _anchor;
            float mag = delta.magnitude;
            float step = (t.position - _lastPos).magnitude;
            _lastPos = t.position;

            if (_gestureLocked)
            {
                // A swipe already fired for this excursion. Two things can happen next:
                //  - the finger keeps dragging the same way → that is one long swipe, not
                //    five; hold the lock so a screen-wide drag is still a single lane move.
                //  - the finger pauses → treat that as the end of the gesture and re-anchor
                //    here, so a second flick from this spot fires again without lifting.
                if (step < _settlePx)
                {
                    _anchor = t.position;
                    _gestureLocked = false;
                }

                // Holding low after a downward flick keeps the crouch alive.
                if (delta.y < -_thresholdPx * 0.5f && Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                    _crouchHeld = true;
                return;
            }

            // A finger resting or barely moving is a tuck.
            if (mag < _thresholdPx)
            {
                _tuckHeld = true;
                return;
            }

            // Fire on threshold crossing — this is what makes it feel immediate.
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                IssueLane(delta.x > 0f ? 1 : -1);
            else if (delta.y > 0f)
                IssueJump();
            else
            {
                IssueCrouch();
                _crouchHeld = true;
            }

            _gestureLocked = true;
        }
    }
}
