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
        [SerializeField] private float hitDamage = 30f;
        /// 71차(사용자): 피해는 「최대 체력의 비율」로 — ObstacleHazard.DamageMul 이 곧 비율(버스 0.60 · 허들/바리케이드 0.30 · 콘 0.18 …, 표는 ObstacleCatalog.DamageFrac).
        /// 1 이상이면 즉사. 65~66차의 hitDamage×배율×DamageScale 방식은 폐기(육성 스탯의 피격 감소는 DamageReduce 로 남긴다).
        public const float DamageScale = 1f;
        /// 74차: 장애물 한 방 피해 범위. 76차(사용자 「최소 데미지 HP 30」): HUD 숫자(0~100 = 최대 체력 비율)로 30~60 이 되게 **최대 체력의 비율**로 깎는다
        /// — 체력 스탯으로 최대 HP 가 200 이어도 게이지에선 30~60 이 사라진다(전엔 절대값 30 이라 게이지 15 만 빠져 보였다).
        public const float MinHitFrac = 0.30f, MaxHitFrac = 0.60f;
        public const float MinHitHp = 30f, MaxHitHp = 60f;   // (호환용 — HUD 100 기준 값)
        [SerializeField] private float jellyHeal = 0.4f;
        [SerializeField] private float potionHeal = 30f;   // 17차: 물약 회복 25→40 · 71차(사용자 「너무 쉽다」): 30

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
            float frac = _player != null ? Mathf.Max(0.05f, _player.PendingHitDamageMul) : 0.25f;
            if (_player != null) _player.PendingHitDamageMul = ObstacleHazard.DefaultFrac;
            // 71차(사용자): 피해 = 최대 체력 × 장애물 비율(버스 60 %·허들 30 %·크기별). 육성 스탯 감소분(hitDamage/30 기준)만큼 조금 덜 받는다. 1 이상 = 즉사.
            float reduce = max > 0f ? Mathf.Clamp(hitDamage / (max * 0.45f), 0.7f, 1f) : 1f;   // RunTuning.HitDamage = MaxHp×0.45×(1−0.15×체력) → 기본 1.0, 체력 만렙 0.85
            // 74차(사용자 「어떤 장애물은 1 정도밖에 안 단다 — 장애물별 최소 30, 30~60」): 비율로 계산한 값을 **HP 30~60 으로 고정 클램프**(최대 체력과 무관). 1 이상 = 즉사 그대로.
            float dmg = frac >= 1f ? max + 1f : max * Mathf.Clamp(frac * reduce, MinHitFrac, MaxHitFrac);
            Debug.LogWarning($"[HP] hit frac={frac:0.00} reduce={reduce:0.00} → dmg={dmg:0} = 게이지 {dmg / Mathf.Max(1f, max) * 100f:0} (hp {_current:0}/{max:0})");
            Apply(-dmg, silent: false);
            OnDamaged?.Invoke(dmg);
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
