using System.Collections.Generic;

namespace CoastRun
{
    /// 컷씬 한 줄. Kind: BG(배경 + L/R 스탠딩) · CG(풀 일러스트) · SAY(대사) · NARR(지문) · LETTER(편지 한 줄).
    /// A = BG id / CG id / 화자, B = L 스탠딩("이름:표정") / CG 설명 / 텍스트, C = R 스탠딩, D = BG 변형(눈 등).
    public readonly struct VnLine
    {
        public readonly string Kind, A, B, C, D;

        public VnLine(string kind, string a, string b, string c, string d)
        {
            Kind = kind; A = a; B = b; C = c; D = d;
        }
    }

    /// v5 대본 테이블 접근자. 데이터는 ChapterScript.Data.cs(자동 생성).
    public static partial class ChapterScript
    {
        public static string OpenId(int chapter) => $"CH{chapter:00}_Open";
        public static string CloseId(int chapter) => $"CH{chapter:00}_Close";

        public static bool Has(string id) => !string.IsNullOrEmpty(id) && Scenes.TryGetValue(id, out var l) && l != null && l.Length > 0;

        public static VnLine[] Get(string id) => Has(id) ? Scenes[id] : System.Array.Empty<VnLine>();

        public static string Title(int chapter) => Titles.TryGetValue(chapter, out var t) ? t : "";

        /// 스탠딩 리소스 이름: 하늘은 육성 캐릭터(Raise_Girl_*), 나머지는 Stand_<Name>_<Mood>.
        public static string StandingResource(string who)
        {
            if (string.IsNullOrEmpty(who)) return null;
            int i = who.IndexOf(':');
            string name = i < 0 ? who : who.Substring(0, i);
            string mood = i < 0 ? "Normal" : who.Substring(i + 1);
            switch (name)
            {
                case "하늘":
                    if (mood != "Happy" && mood != "Tired") mood = "Normal";
                    return "Raise_Girl_" + mood;
                case "도윤":
                    if (mood != "Smile" && mood != "Tired" && mood != "Winter") mood = "Normal";
                    return "Stand_Doyun_" + mood;
                case "루아":
                    if (mood != "Sharp") mood = "Normal";
                    return "Stand_Rua_" + mood;
                case "만수":
                    return "Stand_Mansu_Normal";
                default:
                    return null;
            }
        }

        public static string SpeakerName(string who)
        {
            if (string.IsNullOrEmpty(who)) return "";
            int i = who.IndexOf(':');
            return i < 0 ? who : who.Substring(0, i);
        }
    }
}
