using System;
using System.Collections.Generic;
using UnityEngine;

public struct GachaResult
{
    public PartRarity rarity;
    public string partId;
    public bool duplicate;
    public int alloyGained;
    public bool pityForced;
}

/// Vending machine. Duplicates auto-smelt to alloy — N is never a dead pull.
public class Vendor : MonoBehaviour
{
    public static Vendor Instance { get; private set; }

    public const int SingleCost = 300;
    public const int TenCost = 2700;
    public const int PityLimit = 60;
    public const int ShardPityBoost = 3;

    public static readonly int[] AlloyByRarity = { 2, 6, 18, 40 };

    public event Action<GachaResult> OnPull;

    private DeterministicRandom _rng;
    private readonly List<string>[] _pool = new List<string>[4];

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
        BuildDefaultPools();
        int seed = Save != null ? Save.runCount * 997 + 347 : 347;
        _rng = new DeterministicRandom(seed);
    }

    private void BuildDefaultPools()
    {
        for (int i = 0; i < 4; i++)
            _pool[i] = new List<string>();

        // Placeholder ids until art/data SOs land. Enough to exercise pity + smelt.
        for (int i = 0; i < 20; i++)
            _pool[0].Add("n_part_" + i);
        for (int i = 0; i < 12; i++)
            _pool[1].Add("r_part_" + i);
        for (int i = 0; i < 8; i++)
            _pool[2].Add("sr_part_" + i);

        string[] ssr =
        {
            "ssr_ihan_deck",
            "ssr_reverse_bearing",
            "ssr_wet_grip",
            "ssr_silent_truck",
            "ssr_glass_wheel",
            "ssr_night_deck",
            "ssr_courier_truck",
            "ssr_flood_bearing",
            "ssr_scanner_grip",
            "ssr_last_wheel"
        };
        for (int i = 0; i < ssr.Length; i++)
            _pool[3].Add(ssr[i]);
    }

    public bool TryPullSingle(out GachaResult result)
    {
        result = default;
        if (Wallet.Instance == null || !Wallet.Instance.SpendCoins(SingleCost))
            return false;

        result = PullOne(forceRPlus: false);
        OnPull?.Invoke(result);
        return true;
    }

    public bool TryPullTen(out GachaResult[] results)
    {
        results = new GachaResult[10];
        bool free = Save != null && Save.freeTenPullTickets > 0;
        if (!free)
        {
            if (Wallet.Instance == null || !Wallet.Instance.SpendCoins(TenCost))
                return false;
        }
        else
        {
            Save.freeTenPullTickets--;
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.MarkDirty();
        }

        bool hasRPlus = false;
        for (int i = 0; i < 10; i++)
        {
            results[i] = PullOne(forceRPlus: false);
            if (results[i].rarity >= PartRarity.R)
                hasRPlus = true;
            OnPull?.Invoke(results[i]);
        }

        if (!hasRPlus)
        {
            // Replace the last N with a forced R+.
            results[9] = PullOne(forceRPlus: true);
            OnPull?.Invoke(results[9]);
        }

        return true;
    }

    public bool TrySpendShardOnPity()
    {
        if (Wallet.Instance == null || !Wallet.Instance.SpendDeckShards(1))
            return false;
        if (Save == null)
            return false;

        Save.gachaPityCounter = Mathf.Min(PityLimit, Save.gachaPityCounter + ShardPityBoost);
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();
        return true;
    }

    private GachaResult PullOne(bool forceRPlus)
    {
        var result = new GachaResult();
        if (Save == null)
            return result;

        bool pity = Save.gachaPityCounter + 1 >= PityLimit;
        PartRarity rarity;
        if (pity)
        {
            rarity = PartRarity.SSR;
            result.pityForced = true;
            Save.gachaPityCounter = 0;
        }
        else if (forceRPlus)
        {
            rarity = RollRPlus();
            Save.gachaPityCounter++;
        }
        else
        {
            rarity = RollRarity();
            if (rarity == PartRarity.SSR)
                Save.gachaPityCounter = 0;
            else
                Save.gachaPityCounter++;
        }

        result.rarity = rarity;
        result.partId = PickId(rarity);
        result.duplicate = Save.ownedParts.Contains(result.partId);

        if (result.duplicate)
        {
            result.alloyGained = AlloyByRarity[(int)rarity];
            if (Wallet.Instance != null)
                Wallet.Instance.AddAlloy(result.alloyGained);
        }
        else
        {
            Save.ownedParts.Add(result.partId);
        }

        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();

        return result;
    }

    private PartRarity RollRarity()
    {
        float roll = _rng != null ? _rng.Value() : 0f;
        // N70 / R22 / SR7 / SSR1
        if (roll < 0.01f)
            return PartRarity.SSR;
        if (roll < 0.08f)
            return PartRarity.SR;
        if (roll < 0.30f)
            return PartRarity.R;
        return PartRarity.N;
    }

    private PartRarity RollRPlus()
    {
        float roll = _rng != null ? _rng.Value() : 0f;
        // Renormalized among R/SR/SSR (22+7+1 = 30)
        float t = roll * 0.30f;
        if (t < 0.01f)
            return PartRarity.SSR;
        if (t < 0.08f)
            return PartRarity.SR;
        return PartRarity.R;
    }

    private string PickId(PartRarity rarity)
    {
        List<string> pool = _pool[(int)rarity];
        if (pool == null || pool.Count == 0)
            return rarity.ToString().ToLowerInvariant() + "_empty";
        int i = _rng != null ? _rng.Range(0, pool.Count) : 0;
        return pool[i];
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}
