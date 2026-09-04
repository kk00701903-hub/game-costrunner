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
        [SerializeField] private float bufferSeconds = 0.15f;

        private Vector2 _anchor;
        private Vector2 _lastPos;
        private bool _touchActive;
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

        private void PollKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                IssueLane(-1);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                IssueLane(1);

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
                IssueJump();

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                IssueCrouch();

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _crouchHeld = true;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
                return;
            }

            if (!_touchActive)
                return;

            if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
            {
                _touchActive = false;
                return;
            }

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
