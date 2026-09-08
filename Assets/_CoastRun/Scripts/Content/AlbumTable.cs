using UnityEngine;

namespace CoastRun
{
    /// 22차-8: 7트랙 디지털 앨범(실제 Suno 곡 M1~M7). (예전 20트랙 설계는 폐기)
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
        public string Clip => $"Track_{song}";
        public string Lyrics => $"Album/Lyrics_{song}";
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

        /// 22차-8: 실제 OST 7곡(Suno). chapter = 해금 챕터(그 챕터 클리어 등급으로 열린다). 오디오: Resources/CoastRun/BGM/Track_M{n}.ogg
        public static readonly TrackDef[] Tracks =
        {
            new TrackDef(1,  "M7", TrackKind.Title,    "보조개",                 "Dimples"),                 // 러닝 BGM — 신나는 여자 댄스
            new TrackDef(3,  "M2", TrackKind.Song,     "솜사탕 둘이서",           "Cotton Candy for Two"),    // 보디가드 형들·행복기
            new TrackDef(6,  "M4", TrackKind.Song,     "Good bye My first love",  "Good bye My first love"),  // 첫사랑 아이러니
            new TrackDef(9,  "M1", TrackKind.Song,     "이별예감",                "Premonition"),             // 하늘(현재)
            new TrackDef(12, "M6", TrackKind.Inst,     "남녀사랑이야기",          "A Love Story"),            // 기억·아빠 테마(연주곡)
            new TrackDef(16, "M5", TrackKind.Title,    "돌아온 제주",             "Back to Jeju"),            // 도윤·타이틀
            new TrackDef(20, "M3", TrackKind.Duet,     "별",                      "Stars"),                   // 둘의 테마·엔딩 듀엣(S급)
        };

        public const int TrackCount = 7;

        /// 트랙 번호(1..7)로.
        public static TrackDef ByIndex(int index) => Tracks[Mathf.Clamp(index, 1, TrackCount) - 1];

        /// 해금 챕터가 정확히 이 챕터인 트랙(없으면 null).
        public static TrackDef? ForChapter(int chapter)
        {
            foreach (var t in Tracks) if (t.chapter == chapter) return t;
            return null;
        }

        /// 러닝 BGM용: 그 챕터까지 나온 곡 중 가장 최근 곡(챕터 1은 보조개).
        public static TrackDef Get(int chapter)
        {
            var best = Tracks[0];
            foreach (var t in Tracks) if (t.chapter <= chapter) best = t;
            return best;
        }

        /// 등급별 버전: C/B = 1절(preview), A = 풀버전, S = 풀버전 + 재킷·가사 카드. 듀엣 트랙은 S에서만 열린다.
        public static bool UnlockedBy(TrackDef t, ChapterGrade g)
        {
            if (t.kind == TrackKind.Duet) return g == ChapterGrade.S;
            return g >= ChapterGrade.C;
        }
        public static bool FullVersion(ChapterGrade g) => g >= ChapterGrade.A;
    }
}
