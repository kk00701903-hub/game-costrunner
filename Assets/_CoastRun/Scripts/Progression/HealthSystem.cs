using System;
using UnityEngine;

namespace CoastRun
{
    /// Cookie-Run-style stamina bar. It drains every second, drops a chunk on a hit, and
    /// is refilled by jellies, potions and the pet. Empty bar = stage over (retry).
    ///
    /// Why a draining bar on top of obstacles: it turns "avoid things" into "keep
    /// moving and keep eating" — the player is pulled toward jelly trails instead of
    /// playing safe in an empty lane, which is what makes the run feel fast.
    public class HealthSystem : MonoBehaviour
    {
        public static HealthSystem Instance { get; private set; }

        [SerializeField] private float max = 100f;
        [Tooltip("Passive drain per second. 1.6 → ~60 s with no pickups at all.")]
        // Budget at ~19 m/s on a 1650–2800 m stage (90–150 s): trails lay ~0.41 jelly/m,
        // so jellies are worth ~3 HP/s at full pickup; a potion (~every 190 m) ~2.5 HP/s.
        // A middling run (30% jellies, one hit per 10 s, 60% potions) nets about −0.6 HP/s
        // and finishes a long stage in the red; a sloppy one dies near the minute mark.
        [SerializeField] private float drainPerSecond = 1.6f;
        [SerializeField] private float hitDamage = 14f;
        [SerializeField] private float jellyHeal = 0.4f;
        [SerializeField] private float potionHeal = 25f;

        private PlayerController _player;
        private float _current;
        private bool _active;
        private float _lowPulse;

        public float Max => max;
        public float Current => _current;
        public float Normalized => max > 0f ? Mathf.Clamp01(_current / max) : 0f;
        public float JellyHeal => jellyHeal;
        public float PotionHeal => potionHeal;
        public bool IsActive => _active;

        /// Bonus Time: no drain, no damage.
        public bool Frozen { get; set; }

        public event Action<float, float> OnChanged;      // current, max
        public event Action<float> OnDamaged;             // amount
        public event Action<float> OnHealed;              // amount
        public event Action OnDepleted;

        private void Awake()
        {
            Instance = this;
            _current = max;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (_player != null)
                _player.OnSoftHit -= HandleHit;
        }

        public void Bind(PlayerController player)
        {
            if (_player != null)
                _player.OnSoftHit -= HandleHit;
            _player = player;
            if (_player != null)
                _player.OnSoftHit += HandleHit;
        }

        /// v2: 육성 스탯 → 최대 HP / 피격 감소량. 스테이지 시작마다 호출.
        public void ApplyTuning()
        {
            max = RunTuning.MaxHp;
            hitDamage = RunTuning.HitDamage;
        }

        public void ResetFull()
        {
            _current = RunTuning.BurnoutStart ? max * 0.7f : max;
            _active = true;
            Frozen = false;
            OnChanged?.Invoke(_current, max);
        }

        public void SetActive(bool active) => _active = active;

        private void Update()
        {
            if (!_active || Frozen || _player == null)
                return;
            if (_player.State == SkateState.Finish || _player.Speed < 0.5f)
                return;

            Apply(-drainPerSecond * Time.deltaTime, silent: true);
        }

        private void HandleHit()
        {
            if (!_active || Frozen)
                return;
            Apply(-hitDamage, silent: false);
            OnDamaged?.Invoke(hitDamage);
        }

        public void Heal(float amount)
        {
            if (!_active || amount <= 0f)
                return;
            float before = _current;
            Apply(amount, silent: true);
            OnHealed?.Invoke(_current - before);
        }

        public void HealJelly() => Heal(jellyHeal);
        public void HealPotion() => Heal(potionHeal);

        private void Apply(float delta, bool silent)
        {
            if (!_active)
                return;
            _current = Mathf.Clamp(_current + delta, 0f, max);
            OnChanged?.Invoke(_current, max);
            if (_current <= 0f)
            {
                _active = false;
                OnDepleted?.Invoke();
            }
        }
    }
}
