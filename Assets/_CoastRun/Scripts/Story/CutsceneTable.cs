using System;
using UnityEngine;
using UnityEngine.Playables;

namespace CoastRun
{
    [Serializable]
    public class CutsceneDef
    {
        public string id;             // "Prologue", "CH2_Open", "CH3_Close" ...
        public float duration;
        public string bgmKey;         // matches BGM_제작발주서 filenames (no extension)
        public PlayableAsset timeline; // TimelineAsset when package/assets land
        public bool isTwistCut;       // CH3_Close, CH4_Close → WhiteFlash entry

        /// CH4_Close intentional silence window (seconds into timeline). Do not fill with ambient.
        public float silenceStart = -1f;
        public float silenceEnd = -1f;
    }

    [CreateAssetMenu(menuName = "Coast Run/Cutscene Table", fileName = "CutsceneTable")]
    public class CutsceneTable : ScriptableObject
    {
        public CutsceneDef[] cutscenes = Array.Empty<CutsceneDef>();

        public CutsceneDef GetById(string id)
        {
            if (cutscenes == null || string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < cutscenes.Length; i++)
            {
                if (cutscenes[i] != null && cutscenes[i].id == id)
                    return cutscenes[i];
            }

            return null;
        }

        public CutsceneDef Resolve(CutsceneKind kind, int chapter)
        {
            switch (kind)
            {
                case CutsceneKind.Prologue:
                    return GetById("Prologue");
                case CutsceneKind.ChapterOpening:
                    return GetById("CH" + chapter + "_Open");
                case CutsceneKind.ChapterClosing:
                    return GetById("CH" + chapter + "_Close");
                default:
                    return null;
            }
        }

        public void EnsurePopulated()
        {
            if (cutscenes != null && cutscenes.Length > 0)
                return;
            cutscenes = BuildDefault();
        }

        public static CutsceneTable CreateDefault()
        {
            var t = CreateInstance<CutsceneTable>();
            t.name = "CutsceneTable";
            t.cutscenes = BuildDefault();
            return t;
        }

        /// 13 entries: Prologue + 4 Open + 4 Close + 3 Ending stubs.
        public static CutsceneDef[] BuildDefault()
        {
            return new[]
            {
                Def("Prologue", 130f, "BGM_Cine_Prologue", false),
                Def("CH2_Open", 45f, "BGM_Cine_CH2_Open", false),
                Def("CH3_Open", 45f, "BGM_Cine_CH3_Open", false),
                Def("CH4_Open", 45f, "BGM_Cine_CH4_Open", false),
                Def("CH5_Open", 45f, "BGM_Cine_CH5_Open", false),
                Def("CH1_Close", 90f, "BGM_Cine_CH1_Close", false),
                Def("CH2_Close", 90f, "BGM_Cine_CH2_Close", false),
                Def("CH3_Close", 90f, "BGM_Cine_CH3_Close", true),
                // CH4 「16:40」 — silence 0:50~0:58 intentional. Never auto-fill ambient.
                Silence("CH4_Close", 90f, "BGM_Cine_CH4_Close", true, 50f, 58f),
                // Ending stubs — filled in a later prompt.
                Def("Ending_A", 0f, "BGM_Cine_Ending_A", false),
                Def("Ending_B", 0f, "BGM_Cine_Ending_B", false),
                Def("Ending_C", 0f, "BGM_Cine_Ending_C", false),
                Def("Ending_Sting", 0f, "BGM_Cine_Sting", false),
            };
        }

        private static CutsceneDef Def(string id, float dur, string bgm, bool twist) =>
            new CutsceneDef
            {
                id = id,
                duration = dur,
                bgmKey = bgm,
                isTwistCut = twist,
                timeline = null,
                silenceStart = -1f,
                silenceEnd = -1f
            };

        private static CutsceneDef Silence(string id, float dur, string bgm, bool twist, float a, float b) =>
            new CutsceneDef
            {
                id = id,
                duration = dur,
                bgmKey = bgm,
                isTwistCut = twist,
                timeline = null,
                silenceStart = a,
                silenceEnd = b
            };
    }
}
