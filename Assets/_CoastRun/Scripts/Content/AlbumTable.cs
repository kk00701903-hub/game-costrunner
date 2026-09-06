using UnityEngine;

namespace CoastRun
{
    /// 20트랙 디지털 앨범 『너와 나의 주파수』 — AI 제작 혼성 듀오 '제주(JEJU)'.
    /// 계절마다 미니앨범 5트랙: 타이틀 / 수록곡 / 타이틀 Inst / 수록곡 어쿠스틱 / 타이틀 듀엣(계절 마지막·만남 챕터).
    /// 실제로 만드는 곡은 계절당 2곡(S1~S8). 오디오: Resources/CoastRun/BGM/Track_CHnn.ogg (없으면 챕터 스템으로 대체).
    public enum TrackKind { Title, Song, Inst, Acoustic, Duet }

    public struct TrackDef
    {
        public int chapter;          // 1..20 = 트랙 번호
        public string song;          // S1..S8 (원곡)
        public TrackKind kind;
        public string ko, en;
        public TrackDef(int ch, string song, TrackKind kind, string ko, string en)
        { chapter = ch; this.song = song; this.kind = kind; this.ko = ko; this.en = en; }
        public string Title => Loc.T(ko, en);
        public string Clip => $"Track_CH{chapter:00}";
        public string Lyrics => $"Album/Lyrics_CH{chapter:00}";
        public SeasonKind Season => (SeasonKind)((chapter - 1) / 5);
        public string KindLabel
        {
            get
            {
                switch (kind)
                {
                    case TrackKind.Title: return Loc.T("타이틀", "Title");
                    case TrackKind.Song: return Loc.T("수록곡", "Track");
                    case TrackKind.Inst: return "Inst.";
                    case TrackKind.Acoustic: return Loc.T("어쿠스틱", "Acoustic");
                    default: return Loc.T("듀엣", "Duet");
                }
            }
        }
    }

    public static class AlbumTable
    {
        public const string ArtistKo = "제주";
        public const string ArtistEn = "JEJU";
        public const string AlbumKo = "너와 나의 주파수";
        public const string AlbumEn = "Our Frequency";
        public static string Artist => Loc.T(ArtistKo, ArtistEn);
        public static string ArtistTag => Loc.T("AI 제작 혼성 듀오", "AI-produced mixed duo");

        public static readonly TrackDef[] Tracks =
        {
            // 봄 — S1 「다시 온 사람」 / S2 「주파수」
            new TrackDef(1,  "S1", TrackKind.Title,    "다시 온 사람",            "The One Who Came Back"),
            new TrackDef(2,  "S2", TrackKind.Song,     "기지",                    "Base Camp"),
            new TrackDef(3,  "S1", TrackKind.Inst,     "다시 온 사람 (Inst.)",    "The One Who Came Back (Inst.)"),
            new TrackDef(4,  "S2", TrackKind.Acoustic, "기지 (Acoustic)",         "Base Camp (Acoustic)"),
            new TrackDef(5,  "S1", TrackKind.Duet,     "다시 온 사람 (Duet)",     "The One Who Came Back (Duet)"),
            // 여름 — S3 「축제」 / S4 「태풍」
            new TrackDef(6,  "S3", TrackKind.Title,    "축제",                    "Festival Night"),
            new TrackDef(7,  "S4", TrackKind.Song,     "바다",                    "The Sea"),
            new TrackDef(8,  "S3", TrackKind.Inst,     "축제 (Inst.)",            "Festival Night (Inst.)"),
            new TrackDef(9,  "S4", TrackKind.Acoustic, "바다 (Acoustic)",         "The Sea (Acoustic)"),
            new TrackDef(10, "S3", TrackKind.Duet,     "축제 (Duet)",             "Festival Night (Duet)"),
            // 가을 — S5 「귤」 / S6 「못 한 말」
            new TrackDef(11, "S5", TrackKind.Title,    "귤",                      "Tangerine"),
            new TrackDef(12, "S6", TrackKind.Song,     "오름",                    "Oreum"),
            new TrackDef(13, "S5", TrackKind.Inst,     "귤 (Inst.)",              "Tangerine (Inst.)"),
            new TrackDef(14, "S6", TrackKind.Acoustic, "오름 (Acoustic)",         "Oreum (Acoustic)"),
            new TrackDef(15, "S5", TrackKind.Duet,     "귤 (Duet)",               "Tangerine (Duet)"),
            // 겨울 — S7 「첫눈」 / S8 「구십일 점 구」
            new TrackDef(16, "S7", TrackKind.Title,    "첫눈",                    "First Snow"),
            new TrackDef(17, "S8", TrackKind.Song,     "깡통",                    "The Tin Can"),
            new TrackDef(18, "S7", TrackKind.Inst,     "첫눈 (Inst.)",            "First Snow (Inst.)"),
            new TrackDef(19, "S8", TrackKind.Acoustic, "구십일 점 구 (Acoustic)", "91.9 (Acoustic)"),
            new TrackDef(20, "S7", TrackKind.Duet,     "첫눈 (Duet) — 송전탑",    "First Snow (Duet) — The Tower"),
        };

        public static TrackDef Get(int chapter) => Tracks[Mathf.Clamp(chapter, 1, 20) - 1];

        /// 등급별 버전: C/B = 1절(preview), A = 풀버전, S = 풀버전 + 재킷·가사 카드. 듀엣 트랙은 S에서만 열린다.
        public static bool UnlockedBy(TrackDef t, ChapterGrade g)
        {
            if (t.kind == TrackKind.Duet) return g == ChapterGrade.S;
            return g >= ChapterGrade.C;
        }
        public static bool FullVersion(ChapterGrade g) => g >= ChapterGrade.A;
    }
}
