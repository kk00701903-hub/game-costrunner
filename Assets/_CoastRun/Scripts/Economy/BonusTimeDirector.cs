using System;
using UnityEngine;

namespace CoastRun
{
    /// Cookie-Run Bonus Time: for ten seconds the road is obstacle-free, every lane is
    /// carpeted with big jellies, the player is invincible and faster, and the stamina
    /// bar stops draining. Triggered by the BonusStar pickup.
    public class BonusTimeDirector : MonoBehaviour
    {
        public static BonusTimeDirector Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance._active;

        [SerializeField] private float duration = 10f;
        [SerializeField] private float speedBoost = 1.35f;

        private PlayerController _player;
        private ObstacleSpawner _obstacles;
        private JellySpawner _jellies;
        private HealthSystem _health;
        private RunnerCameraRig _camera;
        private bool _active;
        private float _timeLeft;

        public float Remaining01 => _active ? Mathf.Clamp01(_timeLeft / duration) : 0f;

        public event Action OnStarted;
        public event Action OnEnded;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Bind(PlayerController player, ObstacleSpawner obstacles, JellySpawner jellies,
            HealthSystem health, RunnerCameraRig camera)
        {
            _player = player;
            _obstacles = obstacles;
            _jellies = jellies;
            _health = health;
            _camera = camera;
        }

        public void Activate()
        {
            if (_player == null)
                return;
            if (_active)
            {
                _timeLeft = duration;   // another star extends it
                return;
            }

            _active = true;
            _timeLeft = duration;
            _player.SpeedBoost = speedBoost;
            _player.Invincible = true;
            if (_health != null)
                _health.Frozen = true;
            _obstacles?.SetSuppressed(true);
            _jellies?.SetBonusMode(true);
            _camera?.FovKick(6f, 0.5f);
            RunHudChrome.Instance?.ShowBonusBanner(true);
            JuiceDirector.Instance?.PlayBonusStart();
            OnStarted?.Invoke();
        }

        private void Update()
        {
            if (!_active)
                return;
            _timeLeft -= Time.deltaTime;
            RunHudChrome.Instance?.SetBonusProgress(Remaining01);
            if (_timeLeft <= 0f)
                End();
        }

        /// Stage retry / clear must never leave Bonus Time half-applied.
        public void ForceEnd()
        {
            if (_active)
                End();
        }

        private void End()
        {
            _active = false;
            if (_player != null)
            {
                _player.SpeedBoost = 1f;
                _player.Invincible = false;
            }
            if (_health != null)
                _health.Frozen = false;
            _obstacles?.SetSuppressed(false);
            _jellies?.SetBonusMode(false);
            RunHudChrome.Instance?.ShowBonusBanner(false);
            OnEnded?.Invoke();
        }
    }
}
