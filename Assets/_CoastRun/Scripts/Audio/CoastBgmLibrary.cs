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
            if (clip == null)
            {
                // 56차(사용자): M 곡만 들린다 — 옛 이름(BGM_End_*/Memory_*/Cine_*/Menu/Title …)은 M 곡으로 바꿔 튼다(합성 대체음 금지).
                string alt = Alias(name);
                if (alt != null && alt != name) clip = Resources.Load<AudioClip>(Folder + alt);
            }
            if (clip != null)
                Cache[name] = clip;
            else
                Missing.Add(name);
            return clip;
        }

        /// 옛 BGM 키 → M 곡. 모르는 키(스팅어 등)는 null = 아무것도 안 튼다.
        public static string Alias(string name)
        {
            if (string.IsNullOrEmpty(name) || name.StartsWith("BGM_M")) return null;
            if (name.StartsWith("BGM_Menu") || name == "BGM_Title") return "BGM_M5";
            if (name.StartsWith("BGM_End")) return name.Contains("Descent") ? "BGM_M1" : "BGM_M3";
            if (name.StartsWith("BGM_Memory") || name.StartsWith("BGM_Cine")) return "BGM_M6";
            if (name.StartsWith("BGM_CH") || name.StartsWith("Track_")) return "BGM_M9";
            if (name.StartsWith("BGM_KPOP")) return "BGM_M8";
            return null;
        }

        public static bool Has(string name) => Load(name) != null;

        /// 48차-5(사용자): 첫 시작(스플래시)·메인 화면 음악은 레코드 M5 「돌아온 제주」. 없으면 BGM_Title → 옛 BGM_Menu(_Cleared).
        public static string Menu(bool cleared) =>
            Has("BGM_M5") ? "BGM_M5"
            : Has("BGM_Title") ? "BGM_Title"
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

        /// 48차-15: 챕터 스템(BGM_CH*)을 지웠으므로 러닝 BGM 은 레코드 M1~M7 을 스테이지마다 돌려 쓴다(앨범 Track_* 도 없을 때).
        public static string ChapterFallback(int metaStage) => "BGM_M" + (1 + ((metaStage - 1) % 7 + 7) % 7);
        /// 49차(사용자): 스토리 러닝 BGM 은 M9·M10 두 곡만 — 홀수 스테이지 M9, 짝수 스테이지 M10 (레코드 M1~M7·앨범 Track_* 은 더 이상 안 씀).
        public static string Story(int metaStage) => (metaStage % 2 == 1) ? "BGM_M9" : "BGM_M10";

        public static string ChapterStem(int chapter, int stem) => $"BGM_CH{Mathf.Clamp(chapter, 1, 5)}_{(char)('a' + stem)}";
        public static string Memory(int chapter) => chapter >= 5 ? "BGM_Memory_Cold" : chapter >= 3 ? "BGM_Memory_Mid" : "BGM_Memory_Warm";
        public static string CineOpen(int chapter) => chapter <= 1 ? "BGM_Cine_Prologue" : $"BGM_Cine_CH{chapter}_Open";
        public static string CineClose(int chapter) => $"BGM_Cine_CH{chapter}_Close";
    }
}
