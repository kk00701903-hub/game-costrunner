using UnityEngine;

namespace CoastRun
{
    /// Save / campaign / memory-fragment flags for title + flow.
    public class ProgressionManager : MonoBehaviour
    {
        public const string ClearedKey = "CoastRun_Cleared";
        private const string LegacyClearedKey = "CoastRun_CampaignCleared";
        private const string StageKey = "CoastRun_LastStage";
        private const string ChapterKey = "CoastRun_LastChapter";
        private const string HasSaveKey = "CoastRun_HasSave";
        private const string MemoryMaskKey = "CoastRun_MemoryMask";
        public const int MemorySlotCount = 15;

        public int LastChapter { get; private set; } = 1;
        public int LastStage { get; private set; } = 1;
        public bool HasClearedCampaign { get; private set; }
        public bool HasSave { get; private set; }
        public int MemoryMask { get; private set; }

        public int UnlockedMemoryCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MemorySlotCount; i++)
                {
                    if (IsMemoryUnlocked(i))
                        n++;
                }

                return n;
            }
        }

        public void Load()
        {
            HasClearedCampaign = PlayerPrefs.GetInt(ClearedKey, 0) == 1 ||
                                 PlayerPrefs.GetInt(LegacyClearedKey, 0) == 1;
            if (HasClearedCampaign && PlayerPrefs.GetInt(ClearedKey, 0) == 0)
                PlayerPrefs.SetInt(ClearedKey, 1);

            LastChapter = Mathf.Clamp(PlayerPrefs.GetInt(ChapterKey, 1), 1, 5);
            LastStage = Mathf.Clamp(PlayerPrefs.GetInt(StageKey, 1), 1, 20);
            HasSave = PlayerPrefs.GetInt(HasSaveKey, 0) == 1 || LastStage > 1 || HasClearedCampaign;
            MemoryMask = PlayerPrefs.GetInt(MemoryMaskKey, 0);
        }

        public void SaveCheckpoint(int chapter, int stage)
        {
            LastChapter = chapter;
            LastStage = stage;
            HasSave = true;
            PlayerPrefs.SetInt(ChapterKey, chapter);
            PlayerPrefs.SetInt(StageKey, stage);
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.Save();
        }

        public void MarkCampaignCleared()
        {
            HasClearedCampaign = true;
            HasSave = true;
            PlayerPrefs.SetInt(ClearedKey, 1);
            PlayerPrefs.SetInt(LegacyClearedKey, 1);
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.Save();
        }

        public bool IsMemoryUnlocked(int index)
        {
            if (index < 0 || index >= MemorySlotCount)
                return false;
            return (MemoryMask & (1 << index)) != 0;
        }

        public void UnlockMemory(int index)
        {
            if (index < 0 || index >= MemorySlotCount)
                return;
            MemoryMask |= 1 << index;
            PlayerPrefs.SetInt(MemoryMaskKey, MemoryMask);
            PlayerPrefs.Save();
        }
    }
}
