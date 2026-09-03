#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// Economy brief guards. Run from Tools/347 — no Test Runner setup required.
public static class EconomyValidate
{
    [MenuItem("Tools/Archive A-0347/Validate Economy Numbers")]
    public static void Validate()
    {
        int fails = 0;
        fails += Check(UpgradeCosts.TotalCoinForSlot() == 47200, "coin/slot = 47200");
        fails += Check(UpgradeCosts.TotalAlloyForSlot() == 123, "alloy/slot = 123");
        fails += Check(UpgradeCosts.TotalCoinForSlot() * 5 == 236000, "coin total = 236000");
        fails += Check(UpgradeCosts.TotalAlloyForSlot() * 5 == 615, "alloy total = 615");
        fails += Check(UpgradeCosts.AlloyFormula(4) == 3, "alloy@4 = 3");
        fails += Check(UpgradeCosts.AlloyFormula(14) == 19, "alloy@14 = 19");
        fails += Check(Vendor.SingleCost == 300 && Vendor.TenCost == 2700, "gacha costs");
        fails += Check(Vendor.PityLimit == 60, "pity 60");
        fails += Check(Vendor.TenCost * 6 == 16200, "ceiling via 10-pull = 16200");
        fails += Check(Vendor.AlloyByRarity[0] == 2 && Vendor.AlloyByRarity[3] == 40, "smelt alloy");
        fails += Check(Wallet.SoftCapRuns == 20, "soft cap 20");
        fails += Check(UpgradeSystem.PerformanceCap == 0.25f, "perf cap 25%");
        fails += Check(GameConfig.Active.maxHp <= 3, "HP cap 3");
        fails += Check(GameConfig.Active.minTelegraphSec >= 0.45f, "telegraph floor");

        if (fails == 0)
            Debug.Log("347 Economy: all checks passed.");
        else
            Debug.LogError("347 Economy: " + fails + " check(s) failed.");
    }

    private static int Check(bool ok, string label)
    {
        if (ok)
            return 0;
        Debug.LogError("FAIL: " + label);
        return 1;
    }
}
#endif
