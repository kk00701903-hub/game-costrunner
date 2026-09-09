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
            _dirty = true;   // 24차-11(점검 3-1): 코인 1개마다 PlayerPrefs.Save() 동기 디스크 쓰기 → 코인 라인에서 프레임 스파이크. 모아서 쓴다.
            OnCoinsChanged?.Invoke(TotalCoins, amount);
        }

        private bool _dirty;
        private float _nextFlush;

        private void LateUpdate()
        {
            if (!_dirty || Time.unscaledTime < _nextFlush) return;
            Persist();
        }

        private void OnApplicationPause(bool pause) { if (pause) Persist(); }
        private void OnApplicationQuit() => Persist();
        private void OnDisable() => Persist();

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
            if (!_dirty && PlayerPrefs.GetInt(PrefsKey, -1) == TotalCoins) return;
            PlayerPrefs.SetInt(PrefsKey, TotalCoins);
            PlayerPrefs.Save();
            _dirty = false;
            _nextFlush = Time.unscaledTime + 5f;   // 최대 5초에 한 번
        }

        public void ResetSession() => sessionCoins = 0;
    }
}
