using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 업적 40 — MetaProfile 누적 통계로 판정. 해금은 achMask 비트(id-1). 정산·아케이드 종료·컬렉션 열 때 Check().
    public struct AchievementDef
    {
        public int id; public string ko, en, hintKo, hintEn; public Func<MetaProfile, bool> test;
        public AchievementDef(int id, string ko, string en, string hintKo, string hintEn, Func<MetaProfile, bool> test)
        { this.id = id; this.ko = ko; this.en = en; this.hintKo = hintKo; this.hintEn = hintEn; this.test = test; }
        public string Name => Loc.T(ko, en);
        public string Hint => Loc.T(hintKo, hintEn);
    }

    public static class AchievementTable
    {
        static AchievementDef A(int id, string ko, string en, string hk, string he, Func<MetaProfile, bool> t) => new AchievementDef(id, ko, en, hk, he, t);

        public static readonly AchievementDef[] All =
        {
            // 진행
            A(1,  "첫 노을",        "First Sunset",        "1챕터 클리어",                "Clear chapter 1",                 p => p.trackGrade[0] > 0),
            A(2,  "봄이 끝났다",    "Spring Is Over",      "5챕터 클리어",                "Clear chapter 5",                 p => p.trackGrade[4] > 0),
            A(3,  "여름의 끝",      "End of Summer",       "10챕터 클리어",               "Clear chapter 10",                p => p.trackGrade[9] > 0),
            A(4,  "낙엽",           "Fallen Leaves",       "15챕터 클리어",               "Clear chapter 15",                p => p.trackGrade[14] > 0),
            A(5,  "송전탑",         "The Tower",           "20챕터 클리어",               "Clear chapter 20",                p => p.trackGrade[19] > 0),
            A(6,  "노을에 닿다",    "Reached the Sunset",  "엔딩 A",                       "Ending A",                        p => p.happyEndings > 0),
            A(7,  "빈자리",         "Empty Place",         "엔딩 B",                       "Ending B",                        p => p.endingsSeen - p.happyEndings > 0),
            A(8,  "두 번째 봄",     "Second Spring",       "회차 2번 시작",               "Start a second playthrough",      p => p.playthroughsStarted >= 2),
            A(9,  "보더",           "Boarder",             "스케이트보드 해금",           "Unlock the skateboard",           p => p.skateboardUnlocked),
            // 등급
            A(10, "첫 S",           "First S",             "S급 1챕터",                   "Rank S once",                     p => p.SCount >= 1),
            A(11, "S 다섯",         "Five S",              "S급 5챕터",                   "Rank S in 5 chapters",            p => p.SCount >= 5),
            A(12, "S 열",           "Ten S",               "S급 10챕터",                  "Rank S in 10 chapters",           p => p.SCount >= 10),
            A(13, "전부 S",         "All S",               "S급 20챕터",                  "Rank S in all 20",                p => p.SCount >= 20),
            // 별
            A(14, "별 하나",        "One Star",            "미션 별 10개",                "10 mission stars",                p => p.StarsTotal >= 10),
            A(15, "별 무리",        "Star Cluster",        "미션 별 30개",                "30 mission stars",                p => p.StarsTotal >= 30),
            A(16, "별 하늘",        "Starry Sky",          "미션 별 60개",                "All 60 mission stars",            p => p.StarsTotal >= 60),
            // 런 누적
            A(17, "첫 달리기",      "First Run",           "런 1회",                       "Finish 1 run",                    p => p.totalRuns >= 1),
            A(18, "달리기 50",      "Fifty Runs",          "런 50회",                      "50 runs",                         p => p.totalRuns >= 50),
            A(19, "달리기 200",     "Two Hundred Runs",    "런 200회",                     "200 runs",                        p => p.totalRuns >= 200),
            A(20, "10km",           "10 km",               "누적 10km",                    "10 km total",                     p => p.totalDistance >= 10000),
            A(21, "100km",          "100 km",              "누적 100km",                   "100 km total",                    p => p.totalDistance >= 100000),
            A(22, "귤 천 개",       "A Thousand Coins",    "코인 1,000개",                 "1,000 coins",                     p => p.totalCoins >= 1000),
            A(23, "귤 만 개",       "Ten Thousand Coins",  "코인 10,000개",                "10,000 coins",                    p => p.totalCoins >= 10000),
            A(24, "스치듯",         "Close Call",          "니어미스 100회",               "100 near misses",                 p => p.totalNearMiss >= 100),
            A(25, "바람처럼",       "Like the Wind",       "니어미스 1,000회",             "1,000 near misses",               p => p.totalNearMiss >= 1000),
            A(26, "무사히",         "Unharmed",            "무피격 런 1회",                "One flawless run",                p => p.flawlessRuns >= 1),
            A(27, "무사히 열 번",   "Unharmed Ten",        "무피격 런 10회",               "Ten flawless runs",               p => p.flawlessRuns >= 10),
            A(28, "하트 500",       "500 Hearts",          "하트 누적 500",                "500 hearts total",                p => p.totalHearts >= 500),
            // 아케이드
            A(29, "노을 달리기",    "Sunset Run",          "무한 모드 1회",                "Play endless once",               p => p.totalArcadeRuns >= 1),
            A(30, "1km",            "1 km",                "무한 모드 1,000m",             "1,000 m in endless",              p => p.endlessBestDist >= 1000),
            A(31, "3km",            "3 km",                "무한 모드 3,000m",             "3,000 m in endless",              p => p.endlessBestDist >= 3000),
            A(32, "오늘의 도장",    "Today's Stamp",       "오늘의 런 1회 완료",           "Complete a daily run",            p => p.DailyCount >= 1),
            A(33, "일주일",         "One Week",            "오늘의 런 7일 연속",           "7-day daily streak",              p => p.dailyStreakBest >= 7),
            A(34, "한 달",          "One Month",           "오늘의 런 30회",               "30 daily runs",                   p => p.DailyCount >= 30),
            // 컬렉션
            A(35, "첫 카드",        "First Card",          "포토카드 1장",                 "1 photocard",                     p => p.CardCount >= 1),
            A(36, "바인더 반",      "Half the Binder",     "포토카드 15장",                "15 photocards",                   p => p.CardCount >= 15),
            A(37, "바인더 완성",    "Full Binder",         "포토카드 30장",                "All 30 photocards",               p => p.CardCount >= 30),
            A(38, "전곡",           "Every Track",         "OST 7곡 전부 해금",             "Unlock all 7 tracks",             p => p.TracksUnlocked >= 7),
            A(39, "91.9",           "91.9",                "라디오 편지 대성공 3회",       "3 great radio letters",           p => p.radioGreatCount >= 3),
            A(40, "팬",             "Fan",                 "카드 공유 1회",                "Share a card once",               p => p.shareCount >= 1),
        };

        public static bool Has(MetaProfile p, int id) => p != null && (p.achMask & (1L << (id - 1))) != 0;
        public static int Count(MetaProfile p) { int n = 0; foreach (var a in All) if (Has(p, a.id)) n++; return n; }

        /// 새로 열린 업적 목록. 프로필은 호출자가 저장.
        public static List<AchievementDef> Check(MetaProfile p)
        {
            var got = new List<AchievementDef>();
            if (p == null) return got;
            p.EnsureArrays();
            foreach (var a in All)
            {
                if (Has(p, a.id)) continue;
                bool ok;
                try { ok = a.test(p); } catch { ok = false; }
                if (!ok) continue;
                p.achMask |= 1L << (a.id - 1);
                p.achNewCount++;
                got.Add(a);
            }
            return got;
        }

        /// 판정 + 저장 + 토스트. 게임 어디서나 안전.
        public static void CheckAndToast(GameManager gm)
        {
            if (gm == null || gm.Profile == null) return;
            var got = Check(gm.Profile);
            if (got.Count == 0) return;
            gm.WriteProfileNow();
            foreach (var a in got) CoastToast.Show(Loc.T("업적 달성  ★ ", "Achievement  ★ ") + a.Name + "  —  " + a.Hint);
            CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.5f);
        }
    }
}
