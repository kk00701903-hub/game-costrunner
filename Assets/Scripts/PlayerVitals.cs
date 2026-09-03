using System;
using UnityEngine;

public enum HitKind
{
    Obstacle,
    KingThrow,
    TagLauncher
}

public enum HitResult
{
    Ignored,
    Damaged,
    Revived,
    Fatal
}

/// Deck integrity. Three cracks is the allowance for two mistakes — the real
/// cost of a hit is the deceleration that lets the collapse line back in.
public class PlayerVitals : MonoBehaviour
{
    private GameConfig _cfg;
    private float _invulnTimer;
    private float _speedFactor = 1f;

    public int MaxHp => _cfg != null ? Mathf.Clamp(_cfg.maxHp, 1, 3) : 3;
    public int Hp { get; private set; }
    public int Shields { get; set; }
    public bool Invincible { get; set; }
    public bool IsInvulnerable => _invulnTimer > 0f || Invincible;
    public float SpeedFactor => Invincible ? 1f : _speedFactor;
    public float CounterLockout { get; private set; }

    public event Action<int> OnHpChanged;
    public event Action<HitKind> OnDamaged;
    public event Action OnRevived;

    private void Awake()
    {
        _cfg = GameConfig.Active;
        Hp = MaxHp;
    }

    private void Update()
    {
        if (_invulnTimer > 0f)
            _invulnTimer -= Time.deltaTime;

        if (CounterLockout > 0f)
            CounterLockout -= Time.deltaTime;

        if (_speedFactor < 1f && _cfg != null)
        {
            float recover = _cfg.hurtRecoverTime;
            if (UpgradeSystem.Instance != null)
                recover *= UpgradeSystem.Instance.Mods().hurtRecoverMul;
            float rate = (1f - _cfg.hurtSlowFactor) / Mathf.Max(0.1f, recover);
            _speedFactor = Mathf.MoveTowards(_speedFactor, 1f, rate * Time.deltaTime);
        }
    }

    public HitResult ApplyHit(HitKind kind)
    {
        if (IsInvulnerable)
            return HitResult.Ignored;

        if (Shields > 0)
        {
            Shields--;
            _invulnTimer = _cfg.invulnDuration;
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayShieldBreak();
            return HitResult.Ignored;
        }

        Hp = Mathf.Max(0, Hp - 1);
        _invulnTimer = _cfg.invulnDuration;
        _speedFactor = _cfg.hurtSlowFactor;

        if (kind == HitKind.TagLauncher)
            CounterLockout = 3f;

        OnHpChanged?.Invoke(Hp);
        OnDamaged?.Invoke(kind);

        if (Hp > 0)
            return HitResult.Damaged;

        return TryRevive() ? HitResult.Revived : HitResult.Fatal;
    }

    public bool Heal(int amount)
    {
        if (Hp >= MaxHp || amount <= 0)
            return false;

        // Hard rule: HP never exceeds 3 by any path.
        Hp = Mathf.Min(3, Mathf.Min(MaxHp, Hp + amount));
        OnHpChanged?.Invoke(Hp);
        return true;
    }

    private bool TryRevive()
    {
        if (GameManager.Instance == null || !GameManager.Instance.TryBurnLetter())
            return false;

        Hp = 1;
        _invulnTimer = _cfg.reviveInvulnDuration;
        _speedFactor = 1f;
        CounterLockout = 0f;
        OnHpChanged?.Invoke(Hp);
        OnRevived?.Invoke();
        return true;
    }

    public void Restore()
    {
        Hp = MaxHp;
        _invulnTimer = 0f;
        _speedFactor = 1f;
        CounterLockout = 0f;
        Shields = 0;
        Invincible = false;
        OnHpChanged?.Invoke(Hp);
    }
}
