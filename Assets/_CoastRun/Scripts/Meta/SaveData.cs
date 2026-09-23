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
        /// 74차(사용자: 스트레스가 너무 적게 쌓인다): 스트레스는 **0~100 한 척도**로 통일했다.
        ///   전엔 상태창은 /100, 육성 화면 세로 게이지는 /200(StatMax)으로 그려 실제의 절반으로 보였다.
        public const int StressMax = 100;
        /// 경고(피곤) · 지침 구간의 문턱.
        public const int StressTired = 30, StressWorn = 55;

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
            stress = Mathf.Clamp(stress, 0, StressMax);
            sense = Mathf.Clamp(sense, 0, StatMax);
            trust = Mathf.Clamp(trust, 0, 100);
            trouble = Mathf.Clamp(trouble, 0, 100);
            money = Mathf.Max(0, money);
            hearts = Mathf.Max(0, hearts);
        }

        /// 번아웃 문턱 — 체력이 곧 「버티는 힘」(프메식). 체력 30이면 73, 체력 200이면 90에서 무너진다.
        ///   전엔 `stress > stamina` 라 체력이 100을 넘는 중반부터는 번아웃이 사실상 불가능했다.
        public int StressLimit => Mathf.Clamp(70 + stamina / 10, 70, 90);

        /// 스트레스가 문턱을 넘으면 번아웃: 실패율 급증, 대성공 거의 없음.
        public bool Burnout => stress >= StressLimit;

        /// 스트레스 구간 — 표정·게이지 색·경고 문구·자동 행동이 모두 이걸 본다(다마고치식 즉각 피드백).
        public StressStage Stage =>
            stress >= StressLimit + 15 ? StressStage.Crisis :
            stress >= StressLimit ? StressStage.Burnout :
            stress >= StressWorn ? StressStage.Worn :
            stress >= StressTired ? StressStage.Tired : StressStage.Calm;
    }

    /// 스트레스 5단계. 평온(0~29) · 피곤(30~54) · 지침(55~문턱) · 번아웃(문턱~+15) · 위기(그 위).
    public enum StressStage { Calm = 0, Tired = 1, Worn = 2, Burnout = 3, Crisis = 4 }

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
        /// 109차(마이룸 펫 명령): 이번 주에 펫 명령을 쓴 주차(주 1회). -1 = 아직.
        public int petCmdWeek = -1;
        public int ownedPetMask;
        public int missionDoneMask;             // 44차: 이번 회차에서 깬 챕터 미션(ChapterMission.Kind 비트) — 롱컷 직전 게이트
        public int weekMiniDone;                 // 52차: 주말 미니게임을 깬 마지막 주차(격주 미니게임을 이겨야 그 주가 넘어간다)
        public int level = 1;                    // 53차: 육성 레벨(LevelSystem) — 젤리·행동·러닝이 경험치
        public int exp;                          // 53차: 현재 레벨에서 쌓은 경험치
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
        public int clueMask;                 // 85차(대본 v4): 단서 6비트 — ClueSystem.Clue(이름·편지·하트·머리띠·돌·라디오). 엔딩 분기.
        public string pendingEndingId;       // 85차: ResolveEnding 이 고른 시네마 id(END_A/END_B/END_TRUE) — 엔딩 화면·갤러리 표기용
        public PotState[] pots = new PotState[HomeData.PotCount];   // 30차: 베란다 화분
        public int treadmillStamp = -1;      // 30차: 러닝머신 마지막 사용 (week*4+phase)
        public int miniGameWeek, miniGamePlays;   // 30차: 미니게임 보상 횟수(주 3회)
        public int flowersSold;              // 30차: 판 꽃 수(통계)
        // ── 55차(사용자): 생존 생태계(다마고치) — 식료품·옷·허기·수면·컨디션·죽음 ──
        public int rice = 2;                 // 레거시 미러(LifeItems.SyncLegacy) — 재료+주식 합
        public int sideDish = 2;             // 레거시 미러 — 채소·고기·반찬 합
        public LifeStack[] bag = new LifeStack[0]; // 생활 인벤(재료·요리·약·케어)
        public bool invMigrated;             // rice/sideDish → bag 이관 완료
        public bool ateThisWeek;             // 이번 주 「밥」으로 요리를 먹었는가
        public int hunger = 80;              // 배부름 0~100 (0 = 굶주림)
        public int clothesWeeks = 12;        // 옷 남은 주(3개월 = 12주, 0 이면 낡아서 못 입음)
        public int condition = 80;           // 컨디션 0~100 — 0이 이어지면 죽는다
        public int sleepDebt;                // 잠(밥·휴식 행동)을 안 한 연속 주
        public int starveWeeks;              // 식사 없이 지낸 연속 주
        public int dangerWeeks;              // 컨디션 0 인 연속 주(2주 = 사망)
        public int stressCrisisWeeks;        // 74차: 스트레스 위기(한계+15↑) 연속 주(2주 = 사망)
        public int deaths;                   // 쓰러진 횟수(통계)
        public bool restedThisWeek;          // 이번 주 밥/휴식 행동을 했는가
        public bool boundaryPending;         // 챕터 마지막 주가 끝나 다음 턴에 컷씬·대회가 기다리는 중
        public int contestFails;             // 대회 미달 횟수(통계)
        // 105차(재미요소): 카드 숙련(행동 id → 횟수, JsonUtility 라 배열 두 개) · 지난주에 고른 카드(쉼/놀기/알바) · 잘 산 주 연속(생활 보상)
        public string[] masteryIds = new string[0];
        public int[] masteryCounts = new int[0];
        public string[] lastCardIds = new string[3];
        public int goodWeeks;                // 잘 먹고 잘 잔 주 연속(4주 = 「좋은 한 달」)
        public bool goodMonthBonus;          // 이번 주 성장 +20% (지난주 「좋은 한 달」)
        public bool autoGrocery;             // 정기 장보기(결산 때 쌀·반찬 자동 구입)
        public int[] festivalPlace = new int[4];   // 105차: 계절 축제 등수(0 없음 / 1~3 / 4 참가)
        public int cluePendingMask;          // 105차: 컷씬은 봤지만 육성 조건이 모자라 아직 못 얻은 단서(ClueSystem.Condition) — 조건을 채우면 카드가 다시 뜬다
        public bool inheritedShown;          // 105차: 2회차 계승 안내를 보여 줬는가
        // 131차(재미요소): 별빛 캡슐(이벤트 가챠 — 결제 없음)
        public int starShards;               // 별조각(★) — 스토리 러닝 성과·럭키 캡슐로 모은다
        public int starShardsTotal;          // 누적(통계)
        public int capsuleTickets;           // 러닝 중 럭키 캡슐 = 뽑기권
        public int gachaPulls;               // 누적 뽑기 횟수
        public int gachaPity;                // SR 이상 없이 지나간 횟수(천장 10)
        public bool luckyRunPending;         // 행운의 부적: 다음 완주 러닝 코인 ×2
        public bool autoMode;
        // 136차: 바닷가 마을 허브
        public float villageX, villageZ;                 // 마지막 위치
        public int villageSleepStamp = -1;               // 엄마 집 낮잠 (week*4+phase)
        public int villageFishStamp = -1; public int villageFishCasts;   // 낚시 던진 횟수(페이즈당 5)
        public int villageTreeWeek = -1; public int villageTreeMask;      // 이번 주 흔든 나무
        // 150차: 채집 — 텃밭 9칸(스타듀식), 잡초, 난로 연료(장작), 도끼질한 나무, 주운 것(주 단위 리스폰), 잡은 벌레 수
        public PotState[] farm; public int farmWeedMask; public int farmWetMask; public int farmWetStamp = -1;
        public int villageFuel; public int villageChopWeek = -1; public int villageChopMask;
        public int villagePickWeek = -1; public long villagePickMask; public int bugsCaught;
        public int farmChickens, farmEggs; public int[] farmRabbitAge = new int[0];
        public int reqWeek = -1; public int[] reqNpc = new int[0], reqKind = new int[0], reqProg = new int[0], reqState = new int[0];   // 171차: 주민 요청(주 2~3개)   // 171차: 농장 가축(닭 수·쌓인 달걀·토끼 나이=잠 횟수)
        // 154차: 나무 베기(3번 패면 쓰러짐)·돌 캐기(3번) — 사라진 주(-1 없음), 8주 뒤 다시 생김
        public int[] villageTreeHits; public int[] villageTreeGone; public int[] villageRockHits; public int[] villageRockGone;
        public float villageHour;   // 155차: 마을 시계(0~24, 0 이면 08:00 부터)
        public int villageActMask;  // 156차: 오늘 한 활동(1 밥 · 2 놀기 · 4 알바 · 8 일일 미션) — 잠자면 0
        public int villageMissionKind = -1, villageMissionProg;   // 160차: 오늘의 미션(VillageMission.Kind)과 진행도
        public string[] yardItems = new string[0];       // 마당 가구 "id|x|z|rot"                // 135차: 육성 자동 진행 — 끌 때까지 유지(대회·이야기 다녀와도)

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
        public string[] lastMasteryIds = new string[0];   // 105차: 지난 회차 숙련(다음 회차에 절반 계승)
        public int[] lastMasteryCounts = new int[0];
        public int clueSeenMask;                 // 105차: 한 번이라도 얻어 본 단서(2회차 카드에 「본 적 있음」)
        public int recordMask;                   // 37차: 레코드(M1~M7) 해금 비트 — bit(n-1). 보너스 3곡은 S급 18/20 또는 비밀코드
        public int recordNewMask;                // 37차: 아직 안 들어 본 새 레코드 비트
        public bool devUnlockAll;                // 37차: 설정 비밀코드(1111) — 전체 챕터·레코드 열림(테스트용)
        public bool starterCardsGiven;           // 38차: 실사 포토카드(21~26) 기본 지급 완료
        public int gachaFreeDate;                // 131차: 별빛 캡슐 무료 1회를 쓴 날(yyyymmdd)
        public int decoOwnedMask;                // 28차: 방 장식 보유 비트(RoomDeco.All 순서)
        public int decoNewMask;                  // 28차: 아직 안 본 새 장식 비트
        public int missionClearMask;             // 44차: 한 번이라도 깬 챕터 미션 비트 — 더보기 › 미니게임 다시하기 해금(회차를 넘어 남는다)
        public string[] roomSlots = new string[RoomDeco.SlotCount];   // 28차: 슬롯별 배치된 장식 id(30차부터는 homeItems로 이관)
        public HomeItem[] homeItems = new HomeItem[0];                  // 30차: 방 안 자유 배치(id, x, y)
        public bool homeCompleteRewarded;                                 // 31차: 방 완성 보상(300G) 지급 여부
        // ── 48차: K-POP 한 곡 달리기(데일리). 도장·스트릭은 dailyStamps/dailyStreak/lastDailyDate 를 그대로 쓴다 ──
        public int kpopMissionDoneMask;          // 오늘 미션 3비트(kpopMissionDate != 오늘이면 0)
        public int kpopMissionDate;
        public int kpopAllClearDate;             // 올클리어 보상을 준 날짜
        public int kpopRunsToday, kpopRunsDate;  // 코인 감쇠용(4번째 런부터 ×0.5, 8번째부터 ×0.25)
        public int kpopTodayBest, kpopTodayDate;
        public int[] kpopBestByChapter = new int[20];
        public int kpopSongsFinished;            // 완주 누적
        // ── 52차: 기부(Donation) ──
        public int donateCups;                   // 기부 잔 수(커피 한 잔 = 1)
        public int donateGiftMask;               // Donation.Gift 비트: 1 히든 트랙 / 2 모든 게임 열림
        public int EndingsSeenCount { get { int n = 0; for (int i = 0; i < 7; i++) if ((endingMask & (1 << i)) != 0) n++; return n; } }

        public int StarsTotal { get { int n = 0; if (starMask != null) foreach (var m in starMask) n += (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1); return n; } }
        public int CardCount { get { int n = 0; for (int i = 0; i < 30; i++) if ((cardMask & (1 << i)) != 0) n++; return n; } }
        public int TracksUnlocked { get { int n = 0; if (trackGrade != null) foreach (var g in trackGrade) if (g > 0) n++; return n; } }
        public int SCount { get { int n = 0; if (trackGrade != null) foreach (var g in trackGrade) if (g >= 4) n++; return n; } }
        public int DailyCount => dailyStamps != null ? dailyStamps.Length : 0;
        public void EnsureArrays()
        {
            // 38차: 실사 포토카드 21~26 은 처음부터 열려 있다(나머지는 러닝 포토카드 아이템으로)
            if (!starterCardsGiven) { starterCardsGiven = true; for (int id = 21; id <= 26; id++) cardMask |= 1 << (id - 1); }
            if (trackGrade == null || trackGrade.Length < 20) trackGrade = Grow(trackGrade, 20);
            if (starMask == null || starMask.Length < 20) starMask = Grow(starMask, 20);
            if (bestCoins == null || bestCoins.Length < 20) bestCoins = Grow(bestCoins, 20);
            if (bestCombo == null || bestCombo.Length < 20) bestCombo = Grow(bestCombo, 20);
            if (bestNearMiss == null || bestNearMiss.Length < 20) bestNearMiss = Grow(bestNearMiss, 20);
            if (dailyStamps == null) dailyStamps = new int[0];
            if (kpopBestByChapter == null || kpopBestByChapter.Length < 20) kpopBestByChapter = Grow(kpopBestByChapter, 20);
            RoomDeco.Ensure(this);
        }
        static int[] Grow(int[] a, int n) { var r = new int[n]; if (a != null) Array.Copy(a, r, Math.Min(a.Length, n)); return r; }
    }
}
