using System;
using UnityEngine;

/// Five board parts, Lv0–15. Stats never raise HP past 3 and never shrink telegraphs.
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    public const float PerformanceCap = 0.25f;

    public event Action OnChanged;

    private SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ServiceLocator.Register(this);
    }

    public int Level(PartSlot slot)
    {
        if (Save == null || Save.partLevels == null)
            return 0;
        int i = (int)slot;
        if (i < 0 || i >= Save.partLevels.Length)
            return 0;
        return Mathf.Clamp(Save.partLevels[i], 0, UpgradeCosts.MaxLevel);
    }

    public bool IsUnlocked(PartSlot slot)
    {
        switch (slot)
        {
            case PartSlot.Deck:
                return true;
            case PartSlot.Truck:
                return ZoneUnlocked(2);
            case PartSlot.Wheel:
                return ZoneUnlocked(3);
            case PartSlot.Bearing:
            case PartSlot.Grip:
                return Save != null && Save.deckShards >= 0 && OwnedOrShardUnlock(slot);
            default:
                return false;
        }
    }

    private bool OwnedOrShardUnlock(PartSlot slot)
    {
        // Bearing/Grip unlock costs 15 shards each, once.
        string flag = slot == PartSlot.Bearing ? "unlock_bearing" : "unlock_grip";
        if (Save.flags != null && Save.flags.Contains(flag))
            return true;
        return false;
    }

    public bool TryUnlockWithShards(PartSlot slot)
    {
        if (slot != PartSlot.Bearing && slot != PartSlot.Grip)
            return false;
        if (IsUnlocked(slot))
            return true;
        if (Wallet.Instance == null || !Wallet.Instance.SpendDeckShards(15))
            return false;

        string flag = slot == PartSlot.Bearing ? "unlock_bearing" : "unlock_grip";
        if (!Save.flags.Contains(flag))
            Save.flags.Add(flag);
        Dirty();
        return true;
    }

    private static bool ZoneUnlocked(int zone)
    {
        if (SaveSystem.Instance == null)
            return false;
        int i = zone - 1;
        bool[] cleared = SaveSystem.Instance.Data.zoneCleared;
        return cleared != null && i >= 0 && i < cleared.Length && cleared[i];
    }

    public bool CanUpgrade(PartSlot slot)
    {
        if (!IsUnlocked(slot))
            return false;
        int lv = Level(slot);
        if (lv >= UpgradeCosts.MaxLevel)
            return false;
        if (Wallet.Instance == null)
            return false;
        return Wallet.Instance.CanAfford(UpgradeCosts.CoinCost(lv), UpgradeCosts.AlloyCost(lv), 0);
    }

    public bool TryUpgrade(PartSlot slot)
    {
        if (!CanUpgrade(slot))
            return false;

        int lv = Level(slot);
        long coin = UpgradeCosts.CoinCost(lv);
        long alloy = UpgradeCosts.AlloyCost(lv);
        if (!Wallet.Instance.SpendCoins(coin))
            return false;
        if (alloy > 0 && !Wallet.Instance.SpendAlloy(alloy))
        {
            Wallet.Instance.AddCoins(coin, false);
            return false;
        }

        Save.partLevels[(int)slot] = lv + 1;
        Dirty();
        return true;
    }

    /// Aggregate performance multiplier in [1, 1.25]. Used by tests and HUD.
    public float PerformanceMultiplier()
    {
        float sum = 0f;
        for (int i = 0; i < UpgradeCosts.SlotCount; i++)
            sum += Level((PartSlot)i) / (float)UpgradeCosts.MaxLevel;

        float avg = sum / UpgradeCosts.SlotCount;
        return 1f + PerformanceCap * avg;
    }

    public PlayerStatMods Mods()
    {
        var mods = PlayerStatMods.Identity;
        int deck = Level(PartSlot.Deck);
        int truck = Level(PartSlot.Truck);
        int wheel = Level(PartSlot.Wheel);
        int bearing = Level(PartSlot.Bearing);
        int grip = Level(PartSlot.Grip);

        // Deck: hurt recover 1.5 → 1.0
        mods.hurtRecoverMul = Mathf.Lerp(1f, 1f / 1.5f, deck / 15f);

        // Truck: lane 0.18→0.13, buffer 120→160ms
        mods.laneChangeMul = Mathf.Lerp(1f, 0.13f / 0.18f, truck / 15f);
        mods.inputBufferMul = Mathf.Lerp(1f, 160f / 120f, truck / 15f);

        // Wheel: max speed 20→21.5, booster 4→5
        mods.maxSpeedBonus = Mathf.Lerp(0f, 1.5f, wheel / 15f);
        mods.boosterDurationMul = Mathf.Lerp(1f, 5f / 4f, wheel / 15f);

        // Bearing: grind collapse reward 6→9, balance +35%
        mods.grindCollapseBonus = Mathf.Lerp(0f, 3f, bearing / 15f);
        mods.grindBalanceMul = Mathf.Lerp(1f, 1.35f, bearing / 15f);

        // Grip: counter window 1.4→1.7 (+0.3 cap), land stun -40%
        mods.counterWindowBonus = Mathf.Lerp(0f, 0.3f, grip / 15f);
        mods.landStunMul = Mathf.Lerp(1f, 0.6f, grip / 15f);

        return mods;
    }

    private void Dirty()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();
        OnChanged?.Invoke();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}

public struct PlayerStatMods
{
    public float hurtRecoverMul;
    public float laneChangeMul;
    public float inputBufferMul;
    public float maxSpeedBonus;
    public float boosterDurationMul;
    public float grindCollapseBonus;
    public float grindBalanceMul;
    public float counterWindowBonus;
    public float landStunMul;

    public static PlayerStatMods Identity => new PlayerStatMods
    {
        hurtRecoverMul = 1f,
        laneChangeMul = 1f,
        inputBufferMul = 1f,
        maxSpeedBonus = 0f,
        boosterDurationMul = 1f,
        grindCollapseBonus = 0f,
        grindBalanceMul = 1f,
        counterWindowBonus = 0f,
        landStunMul = 1f
    };
}
