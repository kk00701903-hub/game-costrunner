using System;
using UnityEngine;

namespace CoastRun
{
    /// Listens for near-miss completes → wallet + UI + optional combo bonus.
    public class NearMissSystem : MonoBehaviour
    {
        public static NearMissSystem Instance { get; private set; }

        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private int comboWindowFrames = 90;   // (안 씀 — 228차부터 초 단위 comboWindowSeconds)
        // 228차: 콤보 창을 프레임(60fps 1.5초 · 30fps 3초 — 폰마다 달랐음) → 초 단위로
        private const float comboWindowSeconds = 3f;
        [SerializeField] private float comboBonusPerStack = 0.15f;

        private int _combo;
        private float _comboExpireTime = -1f;

        public int Combo => _combo;
        public event Action<int, int, Vector3> OnNearMissRewarded; // reward, combo, worldPos

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Bind(CoinWallet coinWallet, UpgradeManager upgradeManager, UI_FeedbackController ui)
        {
            wallet = coinWallet;
            upgrades = upgradeManager;
            feedback = ui;
        }

        public void BeginPass(NearMissZone zone)
        {
            // Hook for VFX / tension meter later.
        }

        public void CancelPass(NearMissZone zone)
        {
            _combo = 0;
        }

        public void CompletePass(NearMissZone zone, int baseReward)
        {
            if (Time.time <= _comboExpireTime)
                _combo++;
            else
                _combo = 1;

            _comboExpireTime = Time.time + comboWindowSeconds;

            float mult = upgrades != null ? upgrades.GetCoinMultiplier() : 1f;
            float comboMult = 1f + (_combo - 1) * comboBonusPerStack;
            int reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * mult * comboMult));

            wallet?.Add(reward);

            Vector3 pos = zone != null ? zone.transform.position + Vector3.up * 1.2f : Vector3.zero;
            feedback?.ShowFloatingReward(pos, reward, _combo);
            // Juice (hit-stop, sat punch, FOV, cheer) lives on JuiceDirector via this event.
            OnNearMissRewarded?.Invoke(reward, _combo, pos);
        }
    }
}
