using UnityEngine;

namespace CoastRun
{
    /// 52차(사용자): 펫은 스토리 20주차부터 잠금해제, **코인 + 젤리** 로 산다 — 러닝에서 먹은 젤리를 통장처럼 쌓는다.
    /// PlayerPrefs 한 칸(CoastRun.Jelly). 코인은 CoinWallet(CoastRun.Coins).
    public static class JellyWallet
    {
        public const string PrefsKey = "CoastRun.Jelly";
        private static int _cache = -1;
        private static float _nextFlush;
        private static bool _dirty;

        public static int Total
        {
            get { if (_cache < 0) _cache = PlayerPrefs.GetInt(PrefsKey, 0); return _cache; }
        }

        public static void Add(int n)
        {
            if (n <= 0) return;
            _cache = Total + n; _dirty = true;
            if (Time.unscaledTime >= _nextFlush) Flush();
        }

        public static bool TrySpend(int n)
        {
            if (n <= 0) return true;
            if (Total < n) return false;
            _cache = Total - n; _dirty = true; Flush();
            return true;
        }

        public static void Flush()
        {
            if (!_dirty) return;
            PlayerPrefs.SetInt(PrefsKey, _cache); PlayerPrefs.Save();
            _dirty = false; _nextFlush = Time.unscaledTime + 5f;
        }
    }
}
