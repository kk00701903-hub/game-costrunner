using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// 거인 무적 — 주인공 200% 크기 + 10초간 장애물 피격 HP 감소 없음.
    public class GiantMode : MonoBehaviour
    {
        public static GiantMode Instance { get; private set; }
        public static bool Active => Instance != null && Instance._until > Time.time;

        public const float Duration = 10f;
        public const float ScaleMul = 2f;

        private float _until = -1f;
        private PlayerController _player;
        private float _shownScale = 1f;
        private Coroutine _fx;

        public static GiantMode Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GiantMode");
            Instance = go.AddComponent<GiantMode>();
            return Instance;
        }

        private void Awake() => Instance = this;
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            EndNow();
        }

        public void Activate()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;

            _until = Time.time + Duration;
            _player.Invincible = true;
            _player.VisualScaleMul = ScaleMul;
            if (_fx != null) StopCoroutine(_fx);
            _fx = StartCoroutine(RunFx());
        }

        /// 스테이지 시작·재도전 때 강제 종료.
        public void EndNow()
        {
            _until = -1f;
            if (_fx != null) { StopCoroutine(_fx); _fx = null; }
            if (_player != null)
            {
                if (!PlayerController.DebugGod && !BonusTimeDirector.IsActive)
                    _player.Invincible = false;
                _player.VisualScaleMul = 1f;
            }
            _shownScale = 1f;
        }

        private IEnumerator RunFx()
        {
            PickupFloat.Banner(Loc.T("거인 무적!", "GIANT!"), new Color(1f, 0.55f, 0.15f), 1.4f);
            CoastPrefs.Vibrate();
            JuiceDirector.Instance?.OnFeverStart();   // 속도선·채도 킥 재사용

            float pop = 0f;
            while (pop < 0.28f)
            {
                pop += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(pop / 0.28f);
                float s = Mathf.Lerp(1f, ScaleMul, 1f - (1f - u) * (1f - u));
                ApplyScale(s);
                yield return null;
            }
            ApplyScale(ScaleMul);

            while (Active)
            {
                ApplyScale(ScaleMul);
                // 남은 1.5초 깜빡임 — 곧 끝난다는 신호
                float left = _until - Time.time;
                if (left < 1.5f && _player != null)
                {
                    float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f);
                    ApplyScale(Mathf.Lerp(1.15f, ScaleMul, blink));
                }
                yield return null;
            }

            float t = 0f;
            const float shrink = 0.35f;
            while (t < shrink)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / shrink);
                ApplyScale(Mathf.Lerp(ScaleMul, 1f, u * u));
                yield return null;
            }

            if (_player != null)
            {
                if (!PlayerController.DebugGod && !BonusTimeDirector.IsActive)
                    _player.Invincible = false;
                _player.VisualScaleMul = 1f;
            }
            ApplyScale(1f);
            JuiceDirector.Instance?.OnFeverEnd();
            _fx = null;
        }

        private void ApplyScale(float s)
        {
            _shownScale = s;
            if (_player != null) _player.VisualScaleMul = s;
        }

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            // DebugGod / BonusTime 과 겹쳐도 거인 중엔 무적 유지
            if (Active && _player != null) _player.Invincible = true;
        }
    }
}
