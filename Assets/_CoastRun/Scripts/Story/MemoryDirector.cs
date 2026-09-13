using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// Queues memory popups on stage clear — overlay only, never scene-loads.
    public class MemoryDirector : MonoBehaviour
    {
        public static MemoryDirector Instance { get; private set; }

        private MemoryFragmentLog _log;
        private StageManager _stages;
        private bool _bound;
        private MemoryFragmentDef _queued;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Bind(MemoryFragmentLog log, StageManager stages)
        {
            if (_stages != null)
                _stages.OnStageClear -= HandleStageClear;

            _log = log;
            _stages = stages;
            if (_stages != null)
                _stages.OnStageClear += HandleStageClear;
            _bound = true;
        }

        private void OnDestroy()
        {
            if (_stages != null)
                _stages.OnStageClear -= HandleStageClear;
            if (Instance == this)
                Instance = null;
        }

        private void HandleStageClear(StageDef stage)
        {
            if (_log == null)
                _log = MemoryFragmentLog.Instance ?? Object.FindFirstObjectByType<MemoryFragmentLog>();

            var unlocked = _log != null ? _log.TryUnlockForStage(stage) : null;
            if (unlocked == null)
                return;

            _queued = unlocked;
        }

        /// 81차(사용자): 스테이지 클리어 뒤 옛 수채화 회상 팝업은 더 이상 뜨지 않는다.
        /// 해금 기록(_log)은 그대로 쌓이고, 갤러리에서 직접 고르면 ReplayFromGallery 로 볼 수 있다.
        public IEnumerator PlayQueuedIfAny()
        {
            _queued = null;
            yield break;
        }

        public void ReplayFromGallery(string fragmentId)
        {
            var def = StoryDatabase.GetById(fragmentId);
            if (def == null)
                return;
            if (_log != null && !_log.IsUnlocked(fragmentId))
                return;
            if (_log == null)
            {
                var progress = GameDirector.Instance?.Progression;
                if (progress == null || !progress.IsMemoryUnlocked(def.Index0Based))
                    return;
            }

            UI_MemoryPopup.Ensure().Play(def, null, fromGallery: true);
        }

        public void ReplayFromGalleryIndex(int index0)
        {
            var def = StoryDatabase.GetByIndex(index0);
            if (def != null)
                ReplayFromGallery(def.id);
        }
    }
}
