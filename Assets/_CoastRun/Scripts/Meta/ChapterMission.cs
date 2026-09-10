using UnityEngine;

namespace CoastRun
{
    /// 44차: 롱컷씬 직전 챕터의 「미션 미니게임」.
    /// 롱컷 = CH04·CH07·CH10·CH13·CH15 오프닝(RecordTable/HANDOVER 36차) → 그 직전 챕터 3·6·9·12·14 를 클리어하면
    /// 정산 화면 「다음」 뒤에 미션이 뜨고, **이겨야** 다음 챕터(롱컷)로 넘어간다. 지면 그 자리에서 다시하기.
    /// 클리어한 미션은 타이틀 더보기 › 미니게임에서 다시 할 수 있다(MetaProfile.missionClearMask — 회차를 넘어 남는다).
    public static class ChapterMission
    {
        public enum Kind { Marbles = 0, Yut = 1, Tuho = 2, Ddakji = 3, Mugunghwa = 4 }

        public struct Def
        {
            public Kind kind; public int chapter; public string nameKo, nameEn, ruleKo, ruleEn;
        }

        public static readonly Def[] All =
        {
            new Def { kind = Kind.Marbles,   chapter = 3,  nameKo = "구슬치기", nameEn = "Marbles",
                      ruleKo = "[방향 선택] → [발사!] 힘 선택으로 흰 구슬을 쏴요.\n3발 안에 삼각형 안 구슬 8개 중 5개 이상 밖으로 내보내면 승리", ruleEn = "Pick aim, then power, then shoot. Knock 5 of 8 marbles out of the triangle in 3 shots." },
            new Def { kind = Kind.Yut,       chapter = 6,  nameKo = "윷놀이", nameEn = "Yut Nori",
                      ruleKo = "윷을 던져 한 바퀴!\n도담이보다 먼저 들어오면 승리", ruleEn = "Throw the yut sticks. Get around the board before Dodam." },
            new Def { kind = Kind.Tuho,      chapter = 9,  nameKo = "투호", nameEn = "Tuho",
                      ruleKo = "[방향 선택] → 힘 게이지 흰 띠에서 [발사!]\n5발 중 3발을 항아리에 넣으면 승리", ruleEn = "Pick aim, then power inside the white band, then throw. 3 of 5 in wins." },
            new Def { kind = Kind.Ddakji,    chapter = 12, nameKo = "딱지치기", nameEn = "Ddakji",
                      ruleKo = "힘 게이지가 오르내려요. 노란 구간에서 [내리치기]!\n3번 안에 한 번 넘기면 승리", ruleEn = "The power bar swings. Slam in the yellow zone. Flip it once in 3 tries." },
            new Def { kind = Kind.Mugunghwa, chapter = 14, nameKo = "무궁화 꽃이 피었습니다", nameEn = "Red Light, Green Light",
                      ruleKo = "[달리기]를 누르고 있으면 앞으로!\n술래가 돌아보면 손을 떼. 걸리면 처음부터. 술래를 터치하면 승리", ruleEn = "Hold [Run] to move. Let go when the tagger turns. Reach and touch the tagger to win." },
        };

        public static bool TryGetForChapter(int chapter, out Def def)
        {
            foreach (var d in All) if (d.chapter == chapter) { def = d; return true; }
            def = default; return false;
        }

        public static Def Get(Kind k) { foreach (var d in All) if (d.kind == k) return d; return All[0]; }

        public static bool IsCleared(MetaProfile p, Kind k) => p != null && (p.missionClearMask & (1 << (int)k)) != 0;

        public static void MarkCleared(GameManager gm, Kind k)
        {
            if (gm == null) return;
            if (gm.Profile != null) { gm.Profile.missionClearMask |= 1 << (int)k; gm.WriteProfileNow(); }
            if (gm.Save != null) { gm.Save.missionDoneMask |= 1 << (int)k; gm.Persist(); }
        }

        /// 이 챕터를 막 클리어했고 **이번 회차**에서 아직 미션을 안 깼으면 true(SaveData.missionDoneMask — 회차마다 다시 한다).
        /// 프로필 비트(missionClearMask)는 더보기 › 미니게임 다시하기 해금용.
        public static bool Pending(GameManager gm, int chapter)
        {
            if (gm == null || gm.Save == null || !TryGetForChapter(chapter, out var d)) return false;
            return (gm.Save.missionDoneMask & (1 << (int)d.kind)) == 0;
        }
    }
}
