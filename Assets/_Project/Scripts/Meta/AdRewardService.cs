using System;
using UnityEngine;

/// Ad reward surface. SDK stays out — editor / stub always succeeds.
public interface IAdRewardService
{
    bool CanDoubleCoins { get; }
    bool CanRevive { get; }
    bool CanFreePull { get; }
    bool CanRerollMission { get; }

    bool TryDoubleCoins(Action<bool> onFinished);
    bool TryRevive(Action<bool> onFinished);
    bool TryFreePull(Action<bool> onFinished);
    bool TryRerollMission();
}

public class AdRewardService : MonoBehaviour, IAdRewardService
{
    public static AdRewardService Instance { get; private set; }

    public const int DailyDoubleLimit = 5;
    public const int DailyGachaLimit = 1;
    public const int DailyRerollLimit = 1;

    private bool _reviveUsedThisRun;
    private bool _blockRevive; // final king phase

    private SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

    public bool CanDoubleCoins =>
        Save != null && Save.adUsesToday_Double < DailyDoubleLimit;

    public bool CanRevive =>
        !_blockRevive && !_reviveUsedThisRun;

    public bool CanFreePull =>
        Save != null && Save.adUsesToday_Gacha < DailyGachaLimit;

    public bool CanRerollMission =>
        Save != null && Save.adUsesToday_Reroll < DailyRerollLimit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ServiceLocator.Register<IAdRewardService>(this);
        ServiceLocator.Register(this);
    }

    public void BeginRun()
    {
        _reviveUsedThisRun = false;
        _blockRevive = false;
    }

    public void SetFinalPhaseBlock(bool block)
    {
        _blockRevive = block;
    }

    public bool TryDoubleCoins(Action<bool> onFinished)
    {
        if (!CanDoubleCoins)
        {
            onFinished?.Invoke(false);
            return false;
        }

        ShowStub(() =>
        {
            Save.adUsesToday_Double++;
            Dirty();
            onFinished?.Invoke(true);
        });
        return true;
    }

    public bool TryRevive(Action<bool> onFinished)
    {
        if (!CanRevive)
        {
            onFinished?.Invoke(false);
            return false;
        }

        ShowStub(() =>
        {
            _reviveUsedThisRun = true;
            Save.adUsesToday_Revive++;
            Dirty();
            onFinished?.Invoke(true);
        });
        return true;
    }

    public bool TryFreePull(Action<bool> onFinished)
    {
        if (!CanFreePull)
        {
            onFinished?.Invoke(false);
            return false;
        }

        ShowStub(() =>
        {
            Save.adUsesToday_Gacha++;
            Dirty();
            if (Wallet.Instance != null)
                Wallet.Instance.AddCoins(Vendor.SingleCost, false);
            onFinished?.Invoke(true);
        });
        return true;
    }

    public bool TryRerollMission()
    {
        if (!CanRerollMission)
            return false;

        // Instant in stub — MissionSystem already gates the call.
        Save.adUsesToday_Reroll++;
        Dirty();
        return true;
    }

    private static void ShowStub(Action onSuccess)
    {
        // Real SDK later. Editor / stub: instant success.
        onSuccess?.Invoke();
    }

    private void Dirty()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        ServiceLocator.Unregister<IAdRewardService>(this);
        if (Instance == this)
            Instance = null;
    }
}
