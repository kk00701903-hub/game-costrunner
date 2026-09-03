using System;
using UnityEngine;

/// One slot, overwritten on pickup. No inventory to manage, so the only
/// decision is when to spend what you are holding.
public class ItemSlot : MonoBehaviour
{
    public static ItemSlot Instance { get; private set; }

    [SerializeField] private float boosterSeconds = 4f;
    [SerializeField] private float shieldSeconds = 8f;
    [SerializeField] private float scanSeconds = 6f;
    [SerializeField] private float boosterSpeedBonus = 0.4f;
    [SerializeField] private float boosterGapBonus = 30f;

    private PlayerController _player;
    private PlayerVitals _vitals;
    private PickupKind _held;
    private bool _hasHeld;
    private PickupKind _active;
    private bool _hasActive;
    private float _activeLeft;
    private float _activeTotal;
    private PickupKind _entryItem;
    private bool _hasEntryItem;

    public bool HasItem => _hasHeld;
    public PickupKind Held => _held;
    public bool HasActive => _hasActive;
    public PickupKind Active => _active;

    /// 1 at activation, 0 when it runs out. Drives the slot's timer ring.
    public float ActiveFraction => _hasActive && _activeTotal > 0f ? Mathf.Clamp01(_activeLeft / _activeTotal) : 0f;

    /// Extra telegraph time granted by the reverse scan.
    public static float TelegraphBonus =>
        Instance != null && Instance._hasActive && Instance._active == PickupKind.ReverseScan ? 1f : 0f;

    public event Action OnSlotChanged;
    private float _pulse;

    public float Pulse => _pulse;

    public void PulseHint()
    {
        _pulse = 1f;
        OnSlotChanged?.Invoke();
    }

    private void LateUpdate()
    {
        if (_pulse > 0f)
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 2.5f);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        _vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
    }

    public void Store(PickupKind kind)
    {
        if (!Pickup.IsActiveItem(kind))
            return;

        _held = kind;
        _hasHeld = true;
        OnSlotChanged?.Invoke();
    }

    public bool Activate()
    {
        if (!_hasHeld)
            return false;

        PickupKind kind = _held;
        _hasHeld = false;
        StartEffect(kind);
        OnSlotChanged?.Invoke();
        return true;
    }

    private void StartEffect(PickupKind kind)
    {
        StopEffect();

        _active = kind;
        _hasActive = true;

        switch (kind)
        {
            case PickupKind.BoosterCell:
                _activeTotal = boosterSeconds;
                if (UpgradeSystem.Instance != null)
                    _activeTotal *= UpgradeSystem.Instance.Mods().boosterDurationMul;
                if (_player != null)
                    _player.SpeedMultiplier = 1f + boosterSpeedBonus;
                if (_vitals != null)
                    _vitals.Invincible = true;
                if (CollapseLine.Instance != null)
                    CollapseLine.Instance.Push(boosterGapBonus);
                break;
            case PickupKind.Shield:
                _activeTotal = shieldSeconds;
                if (_vitals != null)
                    _vitals.Shields = 1;
                break;
            default:
                _activeTotal = scanSeconds;
                break;
        }

        _activeLeft = _activeTotal;
    }

    private void StopEffect()
    {
        if (!_hasActive)
            return;

        if (_active == PickupKind.BoosterCell)
        {
            if (_player != null)
                _player.SpeedMultiplier = 1f;
            if (_vitals != null)
                _vitals.Invincible = false;
        }
        else if (_active == PickupKind.Shield && _vitals != null)
        {
            _vitals.Shields = 0;
        }

        _hasActive = false;
        _activeLeft = 0f;
        _activeTotal = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
            Activate();

        if (!_hasActive)
            return;

        // The shield ends on its timer or on the hit it absorbed, whichever
        // comes first, so the burst sound is always the last thing you hear.
        if (_active == PickupKind.Shield && _vitals != null && _vitals.Shields <= 0)
        {
            StopEffect();
            OnSlotChanged?.Invoke();
            return;
        }

        _activeLeft -= Time.deltaTime;
        if (_activeLeft > 0f)
            return;

        StopEffect();
        OnSlotChanged?.Invoke();
    }

    /// Recorded at the arena mouth so a lost boss fight hands the item back.
    public void RecordEntryLoadout()
    {
        _hasEntryItem = _hasHeld;
        _entryItem = _held;
    }

    public void RestoreEntryLoadout()
    {
        StopEffect();
        _hasHeld = _hasEntryItem;
        _held = _entryItem;
        OnSlotChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
