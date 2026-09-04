using UnityEngine;

namespace CoastRun
{
    /// Per-stage tally for the clear screen.
    ///
    /// Coins already land in CoinWallet the moment they are picked up, so the settlement
    /// panel is not paying anything out — it is showing the player what the last run was
    /// worth. That distinction matters: a retry must never feel like it costs money.
    public class StageRunStats : MonoBehaviour
    {
        public static StageRunStats Instance { get; private set; }

        public int Coins { get; private set; }
        public int CoinValue { get; private set; }
        public int NearMissCount { get; private set; }
        public int NearMissValue { get; private set; }
        public int BestCombo { get; private set; }
        public int SoftHits { get; private set; }
        public float Seconds { get; private set; }

        public int Total => CoinValue + NearMissValue;

        /// No hit for the whole stage. Subway Surfers has nothing like this; here it is
        /// the one place the run can say "that was clean" without a score number.
        public bool Flawless => SoftHits == 0;

        private bool _running;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_running)
                Seconds += Time.deltaTime;
        }

        public void BeginStage()
        {
            Coins = 0;
            CoinValue = 0;
            NearMissCount = 0;
            NearMissValue = 0;
            BestCombo = 0;
            SoftHits = 0;
            Seconds = 0f;
            _running = true;
        }

        public void EndStage() => _running = false;

        public void NotifyCoin(int value)
        {
            if (!_running)
                return;
            Coins++;
            CoinValue += value;
        }

        public void NotifyNearMiss(int reward, int combo)
        {
            if (!_running)
                return;
            NearMissCount++;
            NearMissValue += reward;
            if (combo > BestCombo)
                BestCombo = combo;
        }

        public void NotifySoftHit()
        {
            if (!_running)
                return;
            SoftHits++;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60:0}:{s % 60:00}";
        }
    }
}
