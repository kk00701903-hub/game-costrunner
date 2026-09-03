using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    [Serializable]
    public class MemoryCallLine
    {
        public string speaker;
        public string text;
    }

    [Serializable]
    public class MemoryFragmentDef
    {
        public string id;
        public string title;
        public string body;
        public int chapter;
        public int unlockedAtStage;
        public string[] stillKeys;
        public float duration = 25f;
        public string bgmKey;
        public bool isCallOnly;
        public MemoryCallLine[] callLines;

        public int Index0Based
        {
            get
            {
                if (string.IsNullOrEmpty(id) || id.Length < 3)
                    return -1;
                if (int.TryParse(id.Substring(1), out int n))
                    return n - 1;
                return -1;
            }
        }

        public bool IsColdTone => chapter >= 5;
    }

    [Serializable]
    public class StoryDataFile
    {
        public MemoryFragmentDef[] fragments;
    }

    /// Loads memory / story text from Resources/CoastRun/story_data.json.
    public static class StoryDatabase
    {
        private const string ResourcePath = "CoastRun/story_data";
        private static StoryDataFile _cache;
        private static Dictionary<string, MemoryFragmentDef> _byId;
        private static Dictionary<int, MemoryFragmentDef> _byStage;

        public static void EnsureLoaded()
        {
            if (_cache != null)
                return;

            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                _cache = JsonUtility.FromJson<StoryDataFile>(ta.text);
            }

            if (_cache == null || _cache.fragments == null || _cache.fragments.Length == 0)
                _cache = new StoryDataFile { fragments = BuildFallbackFragments() };

            _byId = new Dictionary<string, MemoryFragmentDef>();
            _byStage = new Dictionary<int, MemoryFragmentDef>();
            for (int i = 0; i < _cache.fragments.Length; i++)
            {
                var f = _cache.fragments[i];
                if (f == null || string.IsNullOrEmpty(f.id))
                    continue;
                _byId[f.id] = f;
                if (f.unlockedAtStage > 0)
                    _byStage[f.unlockedAtStage] = f;
            }
        }

        public static MemoryFragmentDef[] All
        {
            get
            {
                EnsureLoaded();
                return _cache.fragments;
            }
        }

        public static MemoryFragmentDef GetById(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
                return null;
            return _byId.TryGetValue(id, out var f) ? f : null;
        }

        public static MemoryFragmentDef GetByIndex(int index0)
        {
            EnsureLoaded();
            if (index0 < 0 || index0 >= _cache.fragments.Length)
                return null;
            return _cache.fragments[index0];
        }

        public static MemoryFragmentDef GetForStage(int stageIndex1Based)
        {
            EnsureLoaded();
            return _byStage.TryGetValue(stageIndex1Based, out var f) ? f : null;
        }

        /// S01–S03,S05–S07,…,S17–S19 → R01–R15. Chapter-end stages (×4) and S20 have none.
        public static string RewardFragmentIdForStage(int stageIndex1Based)
        {
            if (stageIndex1Based < 1 || stageIndex1Based > 20)
                return null;
            int indexInChapter = (stageIndex1Based - 1) % 4;
            if (indexInChapter == 3)
                return null;
            int chapter = (stageIndex1Based - 1) / 4;
            int frag = chapter * 3 + indexInChapter + 1;
            return "R" + frag.ToString("00");
        }

        private static MemoryFragmentDef[] BuildFallbackFragments()
        {
            // Minimal titles if JSON missing — bodies still match design.
            string[] titles =
            {
                "열두 살, 부러진 트럭", "비밀 기지", "라디오",
                "여름 방학", "이사 얘기", "스티커",
                "폐공장 탐험", "자전거", "못 한 말",
                "새벽 두 시", "국도", "8월 12일",
                "마지막 통화", "깡통", "루아"
            };
            int[] stages = { 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19 };
            var list = new MemoryFragmentDef[15];
            for (int i = 0; i < 15; i++)
            {
                int ch = i / 3 + 1;
                list[i] = new MemoryFragmentDef
                {
                    id = "R" + (i + 1).ToString("00"),
                    title = titles[i],
                    body = titles[i],
                    chapter = ch,
                    unlockedAtStage = stages[i],
                    stillKeys = new[] { "Mem_R" + (i + 1).ToString("00") + "_A" },
                    duration = 25f,
                    bgmKey = ch >= 5 ? "BGM_Memory_Cold" : ch >= 3 ? "BGM_Memory_Mid" : "BGM_Memory_Warm",
                    isCallOnly = i == 14
                };
            }

            list[14].bgmKey = "";
            list[14].stillKeys = Array.Empty<string>();
            list[14].callLines = new[]
            {
                new MemoryCallLine { speaker = "루아", text = "언니는 왜 안 왔어요?" },
                new MemoryCallLine { speaker = "하늘", text = "…" },
                new MemoryCallLine { speaker = "루아", text = "왜 안 왔냐고 묻는 게 이상해요?" },
                new MemoryCallLine { speaker = "루아", text = "오빠 물건 다 뺐어요. 언니 것도 있던데." }
            };
            return list;
        }
    }
}
