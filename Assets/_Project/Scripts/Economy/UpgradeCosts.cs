using UnityEngine;

/// Upgrade costs and the 25% performance ceiling. Numbers match the economy brief.
public static class UpgradeCosts
{
    public const int MaxLevel = 15;
    public const int SlotCount = 5;

    /// Coin to go FROM level i TO i+1. Sum = 47,200.
    private static readonly int[] CoinToNext =
    {
        100, 220, 390, 680, 1030,
        1450, 1960, 2520, 3160, 3850,
        4620, 5440, 6310, 7240, 8230
    };

    /// Alloy to go FROM level i TO i+1. Sum = 123. Truncated form of 2+(from-3)*1.6.
    private static readonly int[] AlloyToNext =
    {
        0, 0, 0, 0, 3,
        5, 6, 8, 10, 11,
        13, 14, 16, 18, 19
    };

    public static int CoinCost(int fromLevel)
    {
        if (fromLevel < 0 || fromLevel >= MaxLevel)
            return 0;
        return CoinToNext[fromLevel];
    }

    public static int AlloyCost(int fromLevel)
    {
        if (fromLevel < 0 || fromLevel >= MaxLevel)
            return 0;
        return AlloyToNext[fromLevel];
    }

    public static int TotalCoinForSlot()
    {
        int sum = 0;
        for (int i = 0; i < CoinToNext.Length; i++)
            sum += CoinToNext[i];
        return sum;
    }

    public static int TotalAlloyForSlot()
    {
        int sum = 0;
        for (int i = 0; i < AlloyToNext.Length; i++)
            sum += AlloyToNext[i];
        return sum;
    }

    /// Formula check used by tests: alloy(from) = from<4 ? 0 : floor(2+(from-3)*1.6)
    public static int AlloyFormula(int fromLevel)
    {
        if (fromLevel < 4)
            return 0;
        return (int)(2f + (fromLevel - 3) * 1.6f);
    }
}
