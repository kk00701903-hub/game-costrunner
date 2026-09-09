using System;
using UnityEngine;

namespace CoastRun
{
    public enum ChapterGrade { None = 0, C = 1, B = 2, A = 3, S = 4 }
    public enum EndingKind { None = 0, Happy = 1, Tragic = 2 }
    /// 이동 모드 — 회차 시작 시 캐릭터 선택으로 고정된다.
    public enum RunMode { Running = 0, Skateboard = 1 }

    /// 육성 스탯 4종 + 재화. 값은 모두 정수, Clamp()로 범위를 지킨다.
    [Serializable]
    public class PlayerStats
    {
        public const int StatMax = 200;

        public int stamina = 30;   // 체력
        public int agility = 20;   // 순발력
        public int charm = 20;     // 매력
        public int stress = 0;     // 스트레스
        public int money = 300;    // 돈
        public int hearts = 0;     // 말랑이 하트 누적(회차 전체)
        // v3 (프메 오마주): 감성·평판(공개), 말썽(숨김)
        public int sense = 15;     // 감성 — 라디오·사진·정령계 이벤트
        public int trust = 10;     // 평판 — 마을 신뢰, 알바 해금, 할인 (0~100)
        public int trouble = 0;    // 말썽 — 밤 알바·수상한 물건 (0~100, 숨김)

        public PlayerStats Clone() => (PlayerStats)MemberwiseClone();

        public int Get(StatKind kind)
        {
            switch (kind)
            {
                case StatKind.Stamina: return stamina;
                case StatKind.Agility: return agility;
                case StatKind.Charm: return charm;
                case StatKind.Stress: return stress;
                case StatKind.Sense: return sense;
                case StatKind.Trust: return trust;
                default: return 0;
            }
        }

        public void Clamp()
        {
            stamina = Mathf.Clamp(stamina, 0, StatMax);
            agility = Mathf.Clamp(agility, 0, StatMax);
            charm = Mathf.Clamp(charm, 0, StatMax);
            stress = Mathf.Clamp(stress, 0, StatMax);
            sense = Mathf.Clamp(sense, 0, StatMax);
            trust = Mathf.Clamp(trust, 0, 100);
            trouble = Mathf.Clamp(trouble, 0, 100);
            money = Mathf.Max(0, money);
            hearts = Mathf.Max(0, hearts);
        }

        /// 스트레스가 체력을 넘으면 번아웃: 실패율 급증, 대성공 거의 없음.
        public bool Burnout => stress > stamina;
    }

    public enum StatKind { None = 0, Stamina = 1, Agility = 2, Charm = 3, Stress = 4, Sense = 5, Trust = 6 }

    /// v3 생활 리듬(프메의 식단): 체력 성장·스트레스 배율.
    public enum LifeRhythm { Normal = 0, Hard = 1, Easy = 2 }

    /// 챕터 1개의 영구 기록. 타임라인 재도전은 이 객체만 덮어쓴다.
    [Serializable]
    public class ChapterRecord
    {
        public int chapter;             // 1..20
        public int weekStart;
        public int weekEnd;
        public int heartsEarned;        // 이 챕터에서 얻은 말랑이 하트
        public int gateFails;           // 26차: 체력 게이트 불통과 횟수(마감이 그만큼 늘어남)
        public int heartsTarget;        // 만점. earned/target >= 0.9 → S
        public ChapterGrade grade;
        public bool cleared;            // 런닝 클리어 + 컷씬까지 본 챕터
        public PlayerStats snapshotAtStart;

        public float Ratio => heartsTarget > 0 ? (float)heartsEarned / heartsTarget : 0f;
    }

    /// 회차 진행 상태 전체. save_0.json 한 파일에 JsonUtility로 직렬화.
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public int week = 1;                 // 1..52
        public int chapter = 1;              // 1..20
        public int phaseIndex = 0;           // 이번 주에 소화한 페이즈 수 0..3
        public PlayerStats stats = new PlayerStats();
        public ChapterRecord[] chapters = new ChapterRecord[Timeline.Chapters];
        public int chapterHearts;            // 진행 중 챕터에서 지금까지 모은 하트
        public int lateRuns;                 // 8차: 해가 진 뒤 도착한 횟수(누적) — 대본 [늦음>=N]
        public bool lastRunLate;             // 8차: 직전 런이 늦었는지 — 대본 [늦음==1] 은 이걸 본다
        // v3
        public LifeRhythm rhythm = LifeRhythm.Normal;
        public int burnoutWeeks;             // 연속 번아웃 주 수 (1 지침 / 2 앓아눕기 / 3 잠수)
        public int sickWeeks;                // 앓아눕기로 강제 휴식한 횟수(통계)
        public bool snackOn;                 // 간식비(주 15G, 스트레스 ×0.8)
        public bool forfeitPending;          // 잠수: 이번 챕터 노을을 놓쳐 자동 C급 처리 대기
        public PetKind equippedPet = PetKind.None;
        public int ownedPetMask;
        public string[] queuedSchedule = new string[3];
        public EndingKind reachedEnding = EndingKind.None;
        public int playthrough = 1;
        public RunMode runMode = RunMode.Running;
        public bool prologueSeen;
        public int seed;
        public int rollCount;
        // ── 6차 2단계 ──
        public int[] affinity = new int[4];  // 루아·만수·할머니·DJ 호감도
        public int affinityShown;            // 사이드 씬 본 비트 (npc*3+level-1)
        public int endingVariant;            // 엔딩 변형(0 기본 / 1 / 2)
        public bool trueEndingPending;       // 진엔딩 조건 충족(양쪽 엔딩을 본 뒤의 만남)
        public PotState[] pots = new PotState[HomeData.PotCount];   // 30차: 베란다 화분
        public int treadmillStamp = -1;      // 30차: 러닝머신 마지막 사용 (week*4+phase)
        public int miniGameWeek, miniGamePlays;   // 30차: 미니게임 보상 횟수(주 3회)
        public int flowersSold;              // 30차: 판 꽃 수(통계)

        public ChapterRecord CurrentChapter =>
            chapters != null && chapter >= 1 && chapter <= chapters.Length ? chapters[chapter - 1] : null;

        public bool HasQueuedSchedule
        {
            get
            {
                if (queuedSchedule == null) return false;
                for (int i = 0; i < queuedSchedule.Length; i++)
                    if (!string.IsNullOrEmpty(queuedSchedule[i])) return true;
                return false;
            }
        }
    }

    /// 세이브 슬롯과 무관한 계정 프로필: 회차 간 해금. profile.json
    [Serializable]
    public class MetaProfile
    {
        public int endingsSeen;
        public int happyEndings;
        public bool skateboardUnlocked;
        public int bestPlaythrough;
        // ── 컬렉션 (회차를 넘어 남는다) ──
        public int[] trackGrade = new int[20];   // 트랙별 최고 등급(0 잠김, 1 C … 4 S)
        public int cardMask;                     // 포토카드 1..30 획득 비트 (bit id-1)
        public int cardSignedMask;               // 챕터 카드 사인(S) 버전 비트
        public int cardNewMask;                  // 아직 안 열어 본(개봉 연출 대기) 비트
        public bool albumOwned;                  // 디지털 앨범 언락(유료). 봄(1~5챕터)은 무료
        public int radioGreatCount;              // 시크릿 카드 25 조건
        public bool aiNoticeSeen;                // 첫 실행 AI 제작 고지
        public bool ratePrompted;                // 리뷰 요청 1회
        public int shareCount;

        // ── 6차 1단계: 매일 켜는 이유 ──
        public int[] starMask = new int[20];     // 챕터별 별 3개 비트 (bit0 클리어, bit1 미션1, bit2 미션2)
        public int[] bestCoins = new int[20];    // 챕터별 최고 코인 수
        public int[] bestCombo = new int[20];    // 챕터별 최고 니어미스 콤보
        public int[] bestNearMiss = new int[20];
        public int endlessBestScore;             // 무한 모드 최고 점수
        public int endlessBestDist;              // 무한 모드 최고 거리(m)
        public int dailyBestScore;
        public int[] dailyStamps = new int[0];   // 오늘의 런 완료 날짜(yyyymmdd) 목록
        public int dailyStreak;                  // 연속 일수
        public int dailyStreakBest;
        public int lastDailyDate;                // 마지막 도장 날짜
        // 누적 통계 (업적용)
        public int totalRuns, totalArcadeRuns, flawlessRuns;
        public long totalCoins, totalNearMiss, totalHearts, totalDistance;
        public long achMask;                     // 업적 1..64 비트
        public int achNewCount;                  // 아직 안 본 업적 수
        public int playthroughsStarted;
        public int endingMask;                   // 엔딩 갤러리: bit0 A기본 1 A감성 2 A평판 3 B기본 4 B루아 5 B쓰러짐 6 진엔딩
        public bool trueEndingSeen;
        public bool hasLastFinal;                // NG+ 계승용 마지막 회차 최종 스탯
        public PlayerStats lastFinalStats;
        public int decoOwnedMask;                // 28차: 방 장식 보유 비트(RoomDeco.All 순서)
        public int decoNewMask;                  // 28차: 아직 안 본 새 장식 비트
        public string[] roomSlots = new string[RoomDeco.SlotCount];   // 28차: 슬롯별 배치된 장식 id(30차부터는 homeItems로 이관)
        public HomeItem[] homeItems = new HomeItem[0];                  // 30차: 방 안 자유 배치(id, x, y)
        public bool homeCompleteRewarded;                                 // 31차: 방 완성 보상(300G) 지급 여부
        public int EndingsSeenCount { get { int n = 0; for (int i = 0; i < 7; i++) if ((endingMask & (1 << i)) != 0) n++; return n; } }

        public int StarsTotal { get { int n = 0; if (starMask != null) foreach (var m in starMask) n += (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1); return n; } }
        public int CardCount { get { int n = 0; for (int i = 0; i < 30; i++) if ((cardMask & (1 << i)) != 0) n++; return n; } }
        public int TracksUnlocked { get { int n = 0; if (trackGrade != null) foreach (var g in trackGrade) if (g > 0) n++; return n; } }
        public int SCount { get { int n = 0; if (trackGrade != null) foreach (var g in trackGrade) if (g >= 4) n++; return n; } }
        public int DailyCount => dailyStamps != null ? dailyStamps.Length : 0;
        public void EnsureArrays()
        {
            if (trackGrade == null || trackGrade.Length < 20) trackGrade = Grow(trackGrade, 20);
            if (starMask == null || starMask.Length < 20) starMask = Grow(starMask, 20);
            if (bestCoins == null || bestCoins.Length < 20) bestCoins = Grow(bestCoins, 20);
            if (bestCombo == null || bestCombo.Length < 20) bestCombo = Grow(bestCombo, 20);
            if (bestNearMiss == null || bestNearMiss.Length < 20) bestNearMiss = Grow(bestNearMiss, 20);
            if (dailyStamps == null) dailyStamps = new int[0];
            RoomDeco.Ensure(this);
        }
        static int[] Grow(int[] a, int n) { var r = new int[n]; if (a != null) Array.Copy(a, r, Math.Min(a.Length, n)); return r; }
    }
}
