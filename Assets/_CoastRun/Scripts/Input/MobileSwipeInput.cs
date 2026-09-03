using UnityEngine;

namespace CoastRun
{
    /// One-finger swipe + hold. Editor keyboard fallback for playtests.
    public class MobileSwipeInput : MonoBehaviour, IInputReader
    {
        [SerializeField] private float swipeThresholdPx = 48f;

        private Vector2 _touchStart;
        private bool _touchActive;
        private int _laneDelta;
        private bool _jump;
        private bool _crouch;
        private bool _crouchHeld;
        private bool _tuckHeld;

        public bool TuckHeld => _tuckHeld;
        public bool CrouchHeld => _crouchHeld;

        public void Tick()
        {
            _laneDelta = 0;
            _jump = false;
            _crouch = false;
            _crouchHeld = false;
            _tuckHeld = false;

            PollKeyboard();
            PollTouch();
        }

        public int ConsumeLaneDelta()
        {
            int d = _laneDelta;
            _laneDelta = 0;
            return d;
        }

        public bool ConsumeJump()
        {
            bool v = _jump;
            _jump = false;
            return v;
        }

        public bool ConsumeCrouch()
        {
            bool v = _crouch;
            _crouch = false;
            return v;
        }

        private void PollKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                _laneDelta = -1;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                _laneDelta = 1;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
                _jump = true;

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                _crouch = true;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _crouchHeld = true;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                _tuckHeld = true;
        }

        private void PollTouch()
        {
            if (Input.touchCount == 0)
                return;

            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _touchStart = t.position;
                _touchActive = true;
                return;
            }

            if (!_touchActive)
                return;

            Vector2 delta = t.position - _touchStart;

            if (t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved)
            {
                if (delta.y < -swipeThresholdPx && Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                    _crouchHeld = true;
                else if (delta.magnitude < swipeThresholdPx)
                    _tuckHeld = true;
                return;
            }

            if (t.phase == TouchPhase.Canceled)
            {
                _touchActive = false;
                return;
            }

            if (t.phase != TouchPhase.Ended)
                return;

            _touchActive = false;
            if (delta.magnitude < swipeThresholdPx)
                return;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                _laneDelta = delta.x > 0f ? 1 : -1;
            else if (delta.y > 0f)
                _jump = true;
            else
                _crouch = true;
        }
    }
}
