using System;
using UnityEngine;

namespace CoastRun
{
    /// Persistent coin wallet. Session gains flush to PlayerPrefs on run end / upgrade.
    public class CoinWallet : MonoBehaviour
    {
        public const string PrefsKey = "CoastRun.Coins";

        [SerializeField] private int sessionCoins;

        public int TotalCoins { get; private set; }
        public int SessionCoins => sessionCoins;

        public event Action<int, int> OnCoinsChanged; // total, delta

        private void Awake()
        {
            TotalCoins = PlayerPrefs.GetInt(PrefsKey, 0);
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            sessionCoins += amount;
            TotalCoins += amount;
            Persist();
            OnCoinsChanged?.Invoke(TotalCoins, amount);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || TotalCoins < amount)
                return false;

            TotalCoins -= amount;
            Persist();
            OnCoinsChanged?.Invoke(TotalCoins, -amount);
            return true;
        }

        public void Persist()
        {
            PlayerPrefs.SetInt(PrefsKey, TotalCoins);
            PlayerPrefs.Save();
        }

        public void ResetSession() => sessionCoins = 0;
    }
}
