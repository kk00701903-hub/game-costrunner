using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Drop-in slot for real music. Every name in BGM_제작발주서.md maps to
    /// `Assets/Resources/CoastRun/BGM/<name>.ogg` (or .wav/.mp3). While a file is
    /// missing the game keeps its procedural placeholder, so tracks can land one at a
    /// time — put `BGM_Menu.ogg` in the folder and the title picks it up on next Play.
    public static class CoastBgmLibrary
    {
        public const string Folder = "CoastRun/BGM/";

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> Missing = new HashSet<string>();

        public static AudioClip Load(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (Cache.TryGetValue(name, out var clip))
                return clip;
            if (Missing.Contains(name))
                return null;

            clip = Resources.Load<AudioClip>(Folder + name);
            if (clip != null)
                Cache[name] = clip;
            else
                Missing.Add(name);
            return clip;
        }

        public static bool Has(string name) => Load(name) != null;

        /// 타이틀 대문 테마(BGM_Title)가 있으면 그것, 없으면 옛 BGM_Menu(_Cleared).
        public static string Menu(bool cleared) =>
            Has("BGM_Title") ? "BGM_Title"
            : cleared && Has("BGM_Menu_Cleared") ? "BGM_Menu_Cleared" : "BGM_Menu";
        /// 26차: K-POP 러닝모드 트랙 — Resources/CoastRun/BGM/BGM_KPOP_1.ogg … 순서대로. 없으면 null(챕터 스템으로 폴백).
        public static AudioClip Kpop(int index)
        {
            int n = KpopCount();
            if (n == 0) return null;
            return Load("BGM_KPOP_" + (1 + ((index % n) + n) % n));
        }
        private static int _kpopCount = -1;
        public static int KpopCount()
        {
            if (_kpopCount >= 0) return _kpopCount;
            int n = 0;
            while (n < 32 && Has("BGM_KPOP_" + (n + 1))) n++;
            _kpopCount = n;
            return n;
        }

        public static string ChapterStem(int chapter, int stem) => $"BGM_CH{Mathf.Clamp(chapter, 1, 5)}_{(char)('a' + stem)}";
        public static string Memory(int chapter) => chapter >= 5 ? "BGM_Memory_Cold" : chapter >= 3 ? "BGM_Memory_Mid" : "BGM_Memory_Warm";
        public static string CineOpen(int chapter) => chapter <= 1 ? "BGM_Cine_Prologue" : $"BGM_Cine_CH{chapter}_Open";
        public static string CineClose(int chapter) => $"BGM_Cine_CH{chapter}_Close";
    }
}
