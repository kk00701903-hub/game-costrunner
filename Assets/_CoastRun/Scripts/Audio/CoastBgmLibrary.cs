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

        public static string Menu(bool cleared) => cleared && Has("BGM_Menu_Cleared") ? "BGM_Menu_Cleared" : "BGM_Menu";
        public static string ChapterStem(int chapter, int stem) => $"BGM_CH{Mathf.Clamp(chapter, 1, 5)}_{(char)('a' + stem)}";
        public static string Memory(int chapter) => chapter >= 5 ? "BGM_Memory_Cold" : chapter >= 3 ? "BGM_Memory_Mid" : "BGM_Memory_Warm";
        public static string CineOpen(int chapter) => chapter <= 1 ? "BGM_Cine_Prologue" : $"BGM_Cine_CH{chapter}_Open";
        public static string CineClose(int chapter) => $"BGM_Cine_CH{chapter}_Close";
    }
}
