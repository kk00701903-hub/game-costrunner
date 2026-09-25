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
        [SerializeField] private float drainPerSecond = 1.6f;   // (호환) ApplyTuning 이 게이지 비율로 덮어씀
        [SerializeField] private float hitDamage = 30f;
        /// 피해·회복·드레인 모두 HUD 게이지(0~100 = 최대 체력 %) 기준. K-POP·스토리 동일.
        public const float DamageScale = 1f;
        public const float MinHitFrac = 0.30f, MaxHitFrac = 0.60f;
        public const float MinHitHp = 30f, MaxHitHp = 60f;
        public const float PotionHealFrac = 0.30f;     // 물약 +30 게이지
        public const float HeartHealFrac = 0.15f;      // 194차(사용자: 「하트 먹으면 채워지는 것」): 하트 +15 게이지(예전엔 말랑이와 같은 +0.4 라 거의 안 찼다)
        public const float JellyHealFrac = 0.004f;     // 말랑이 +0.4 게이지(기존 절대 0.4 @ max100)
        public const float DrainFracPerSec = 0.016f;   // 초당 −1.6 게이지(기존 절대 1.6 @ max100)
        [SerializeField] private float jellyHeal = 0.4f;
        [SerializeField] private float potionHeal = 30f;

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
                _player.OnHitDamage -= HandleHit;
        }

        public void Bind(PlayerController player)
        {
            if (_player != null)
                _player.OnHitDamage -= HandleHit;
            _player = player;
            if (_player != null)
                _player.OnHitDamage += HandleHit;   // 76차: OnSoftHit(경직) 대신 OnHitDamage — 무적프레임 안 충돌도 피해를 낸다
        }

        /// 육성 스탯 → 최대 HP. 회복·드레인·피격은 전부 게이지 % 로 맞춰 max 에 비례한다.
        public void ApplyTuning()
        {
            max = Mathf.Max(1f, RunTuning.MaxHp);
            hitDamage = RunTuning.HitDamage;
            potionHeal = max * PotionHealFrac;
            jellyHeal = max * JellyHealFrac;
            drainPerSecond = max * DrainFracPerSec;
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

            if (KpopTutorial.Active && _current <= max * 0.2f) return;   // 194차: 튜토리얼 바닥
            Apply(-drainPerSecond * (KpopTutorial.Active ? 0.5f : 1f) * Time.deltaTime, silent: true);
        }

        private void HandleHit()
        {
            if (!_active || Frozen)
                return;
            float frac = _player != null ? _player.PendingHitDamageMul : ObstacleHazard.DefaultFrac;
            if (_player != null) _player.PendingHitDamageMul = ObstacleHazard.DefaultFrac;
            if (frac < 0.01f) frac = ObstacleHazard.DefaultFrac;
            if (frac >= 1f)
            {
                float kill = max + 1f;
                Apply(-kill, silent: false);
                OnDamaged?.Invoke(kill);
                return;
            }
            // 장애물 표 = 게이지 그대로(콘 −30 · … · 버스 −60). 체력 스탯은 MaxHp 만 키운다.
            frac = Mathf.Clamp(frac, MinHitFrac, MaxHitFrac);
            float dmg = max * frac;
            // 194차: 튜토리얼에선 아프기만 하고 끝나지 않게 — 10 % 만 깎고 20 % 밑으로는 안 내려간다.
            if (KpopTutorial.Active) dmg = Mathf.Min(max * 0.10f, Mathf.Max(0f, _current - max * 0.20f));
            Apply(-dmg, silent: false);
            OnDamaged?.Invoke(dmg);
        }

        public void Heal(float amount)
        {
            if (!_active || amount <= 0f)
                return;
            float before = _current;
            Apply(amount, silent: true);
            float gained = _current - before;
            if (gained > 0.01f) OnHealed?.Invoke(gained);
        }

        public void HealJelly() => Heal(jellyHeal);
        public void HealPotion() => Heal(potionHeal);
        public void HealHeart() => Heal(max * HeartHealFrac);
        /// 194차: 튜토리얼 — 하트가 차오르는 걸 보여 주려고 게이지를 맞춘다.
        public void SetFraction(float f) => Apply(Mathf.Clamp01(f) * max - _current, silent: true);

        private void Apply(float delta, bool silent)
        {
            if (!_active)
                return;
            _current = Mathf.Clamp(_current + delta, 0f, max);
            if (_current <= 0f && PetCompanion.TryRevive())
            {
                // 14차 흑돼지: 바닥 대신 40%에서 다시.
                _current = max * 0.4f;
                OnChanged?.Invoke(_current, max);
                OnHealed?.Invoke(_current);
                return;
            }
            OnChanged?.Invoke(_current, max);
            if (_current <= 0f)
            {
                _active = false;
                OnDepleted?.Invoke();
            }
        }
    }
}
