using System;
using UnityEngine;

/// Soft / mid / hard currencies. Tags are catalogue-only and live on Codex.
public class Wallet : MonoBehaviour
{
    public static Wallet Instance { get; private set; }

    public const int SoftCapRuns = 20;

    public long Coins => Save != null ? Save.coins : 0;
    public long Alloy => Save != null ? Save.alloy : 0;
    public long DeckShards => Save != null ? Save.deckShards : 0;

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

    /// Run-pickup and mission payouts go through here so the soft cap applies.
    public long AddCoins(long amount, bool applySoftCap)
    {
        if (Save == null || amount == 0)
            return 0;

        long granted = amount;
        if (applySoftCap && amount > 0 && Save.dailyRunsToday > SoftCapRuns)
            granted = amount / 2;

        if (granted == 0)
            return 0;

        Save.coins = Math.Max(0, Save.coins + granted);
        Dirty();
        return granted;
    }

    public long AddAlloy(long amount)
    {
        if (Save == null || amount == 0)
            return 0;

        Save.alloy = Math.Max(0, Save.alloy + amount);
        Dirty();
        return amount;
    }

    public long AddDeckShards(long amount)
    {
        if (Save == null || amount == 0)
            return 0;

        Save.deckShards = Math.Max(0, Save.deckShards + amount);
        Dirty();
        return amount;
    }

    public bool SpendCoins(long amount)
    {
        if (Save == null || amount < 0 || Save.coins < amount)
            return false;

        Save.coins -= amount;
        Dirty();
        return true;
    }

    public bool SpendAlloy(long amount)
    {
        if (Save == null || amount < 0 || Save.alloy < amount)
            return false;

        Save.alloy -= amount;
        Dirty();
        return true;
    }

    public bool SpendDeckShards(long amount)
    {
        if (Save == null || amount < 0 || Save.deckShards < amount)
            return false;

        Save.deckShards -= amount;
        Dirty();
        return true;
    }

    public bool CanAfford(long coins, long alloy, long shards)
    {
        if (Save == null)
            return false;
        return Save.coins >= coins && Save.alloy >= alloy && Save.deckShards >= shards;
    }

    /// Soft-cap messaging is world voice, not a system toast.
    public bool IsSoftCapped
    {
        get { return Save != null && Save.dailyRunsToday > SoftCapRuns; }
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
