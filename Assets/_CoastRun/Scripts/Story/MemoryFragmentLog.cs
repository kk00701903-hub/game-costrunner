using System;
using UnityEngine;

namespace CoastRun
{
    /// Unlocked memory fragments — save-backed gallery source. No 15/15 bonus text.
    public class MemoryFragmentLog : MonoBehaviour
    {
        public static MemoryFragmentLog Instance { get; private set; }

        public event Action<MemoryFragmentDef> OnFragmentUnlocked;

        private ProgressionManager _progress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Bind(ProgressionManager progress)
        {
            _progress = progress;
            _progress?.Load();
            StoryDatabase.EnsureLoaded();
        }

        public bool IsUnlocked(string fragmentId)
        {
            var def = StoryDatabase.GetById(fragmentId);
            if (def == null)
                return false;
            return IsUnlockedIndex(def.Index0Based);
        }

        public bool IsUnlockedIndex(int index0)
        {
            EnsureProgress();
            return _progress != null && _progress.IsMemoryUnlocked(index0);
        }

        public int UnlockedCount
        {
            get
            {
                EnsureProgress();
                return _progress != null ? _progress.UnlockedMemoryCount : 0;
            }
        }

        /// Returns newly unlocked def, or null if already unlocked / no reward.
        public MemoryFragmentDef TryUnlockForStage(StageDef stage)
        {
            if (stage == null)
                return null;

            string id = stage.rewardFragmentId;
            if (string.IsNullOrEmpty(id))
                id = StoryDatabase.RewardFragmentIdForStage(stage.stageIndex);

            return TryUnlock(id);
        }

        public MemoryFragmentDef TryUnlock(string fragmentId)
        {
            var def = StoryDatabase.GetById(fragmentId);
            if (def == null)
                return null;

            int idx = def.Index0Based;
            if (idx < 0)
                return null;

            EnsureProgress();
            if (_progress == null)
                return null;

            if (_progress.IsMemoryUnlocked(idx))
                return null;

            _progress.UnlockMemory(idx);
            OnFragmentUnlocked?.Invoke(def);
            return def;
        }

        private void EnsureProgress()
        {
            if (_progress != null)
                return;
            _progress = GameDirector.Instance != null
                ? GameDirector.Instance.Progression
                : Object.FindFirstObjectByType<ProgressionManager>();
            _progress?.Load();
        }
    }
}
