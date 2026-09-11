using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 아케이드 런 — 스토리 밖에서 매일 켜는 이유.
    ///   Endless "노을 달리기": 클리어한 계절의 코스가 끝없이 이어지고 속도가 계속 오른다. 거리·코인·하트 = 점수.
    ///   Daily   "오늘의 런": 날짜 시드(서버 없음)로 하루 한 코스 + 조건 3개. 다 채우면 그날 도장, 연속 일수.
    /// 스토리 런 파이프라인(StageManager/GameSession)을 그대로 타되, 클리어 판정은 막고(StageManager) 사망 때 결과창(GameSession).
    public enum ArcadeKind { None = 0, Endless = 1, Daily = 2 }

    public struct DailyCondition
    {
        public MissionKind kind; public int value;
        public DailyCondition(MissionKind k, int v) { kind = k; value = v; }
        public string Text
        {
            get
            {
                switch (kind)
                {
                    case MissionKind.NearMiss: return Loc.T($"니어미스 {value}회", $"{value} near misses");
                    case MissionKind.Coins: return Loc.T($"코인 {value}개", $"{value} coins");
                    case MissionKind.Combo: return Loc.T($"콤보 {value}", $"Combo {value}");
                    case MissionKind.Hearts: return Loc.T($"하트 {value}개", $"{value} hearts");
                    case MissionKind.MaxHits:
                        return ArcadeRun.KpopMode ? Loc.T($"부딪히기 {value}번까지", $"≤{value} hits") : Loc.T($"첫 500m 피격 {value}회 이하", $"≤{value} hits in first 500 m");
                    case MissionKind.NoHit: return Loc.T("한 번도 안 부딪히기", "No hits");
                    case MissionKind.Finish: return Loc.T("곡 끝까지 달리기", "Finish the song");
                    case MissionKind.Fever: return Loc.T("후렴에서 꼬마 도움 받기", "Use Fever in the chorus");
                    default: return Loc.T($"{value}m 달리기", $"Run {value} m");
                }
            }
        }
        /// 48차: K-POP 은 완주 여부(finished)·피버 사용을 같이 본다. MaxHits/NoHit 는 K-POP 에선 런 전체 기준(완주 뒤 판정).
        public bool Check(StageRunStats s, float dist, int hitsFirst500) => Check(s, dist, hitsFirst500, false);
        public bool Check(StageRunStats s, float dist, int hitsFirst500, bool finished)
        {
            switch (kind)
            {
                case MissionKind.NearMiss: return s != null && s.NearMissCount >= value;
                case MissionKind.Coins: return s != null && s.Coins >= value;
                case MissionKind.Combo: return s != null && s.BestCombo >= value;
                case MissionKind.Hearts: return s != null && s.Hearts >= value;
                case MissionKind.MaxHits:
                    return ArcadeRun.KpopMode ? (finished && s != null && s.SoftHits <= value) : (dist >= 500f && hitsFirst500 <= value);
                case MissionKind.NoHit: return finished && s != null && s.SoftHits == 0;
                case MissionKind.Finish: return finished;
                case MissionKind.Fever: return ArcadeRun.FeverInChorus > 0;
                default: return dist >= value;
            }
        }
        /// 런 중에 이미 확정되는 조건인가(완주해야 아는 조건은 false).
        public bool LiveCheckable => kind != MissionKind.MaxHits && kind != MissionKind.NoHit && kind != MissionKind.Finish;
    }

    /// 48차: K-POP 한 곡 달리기 — 곡별 재생 창(초). start 부터 length 초를 세션으로 쓴다. 후렴 구간엔 꼬마 피버 제안 + 코인 ×2.
    /// M4(97s)는 통째, M2(233s)·M7(202s)는 90초 창. 후렴 시각은 사운드 확인 전 기본값(HANDOVER 48차 Open question 1).
    public struct KpopTrackMeta
    {
        public int num; public float start, length, chorusStart, chorusEnd;
        public KpopTrackMeta(int num, float start, float length, float chorusStart, float chorusEnd)
        { this.num = num; this.start = start; this.length = length; this.chorusStart = chorusStart; this.chorusEnd = chorusEnd; }
        public string Clip => "BGM_M" + num;
        public string Title => RecordTable.TitleOf(num);
        /// 48차-5(사용자): 러닝 HUD 우하단 표기 — 「제목 — 우히&히시」(M1. 같은 번호 없이).
        public const string Artist = "우히&히시";
        public string Credit => Title + " — " + Artist;
    }

    public static class ArcadeRun
    {
        /// 48차-11: 에디터 「도메인 리로드 없이 플레이」에서 정적 상태(KpopMode 등)가 다음 플레이로 새는 것 방지.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Kind = ArcadeKind.None; KpopMode = false; KpopFinished = false; KpopElapsed = 0f; FeverInChorus = 0;
            Conditions = new DailyCondition[0]; ConditionDone = new bool[3]; ObstacleSpawner.SeedOverride = null;
        }
        public static ArcadeKind Kind { get; private set; }
        public static bool Active => Kind != ArcadeKind.None;
        public static int Seed { get; private set; }
        public static SeasonKind Season { get; private set; }
        public static int StageIndex { get; private set; }
        public static DailyCondition[] Conditions { get; private set; } = new DailyCondition[0];
        public static bool[] ConditionDone { get; private set; } = new bool[3];
        public static bool ReturnToRaising { get; private set; }
        /// 26차: 타이틀 하단 'K-POP 러닝모드' — 스토리 없는 무한 러닝, 육성 스탯(체력·순발력…) 적용, BGM_KPOP_* 재생.
        public static bool KpopMode { get; private set; }

        // 이번 런 집계
        public static float Distance { get; private set; }
        public static int HitsFirst500 { get; private set; }
        public static int LastScore { get; private set; }
        public static bool LastStamped { get; private set; }

        // ── 48차: K-POP 한 곡 달리기 ──
        /// 곡 3개 중 매 런 랜덤(직전 곡 제외). 창(start/length)·후렴 시각은 초기값 — 사운드 확인 뒤 조정.
        public static readonly KpopTrackMeta[] KpopTracks =
        {
            new KpopTrackMeta(2, 0f, 90f, 55f, 80f),    // 솜사탕 둘이서 (232.9s)
            new KpopTrackMeta(4, 0f, 97f, 55f, 82f),    // Goodbye My First Love (97.1s, 통째)
            new KpopTrackMeta(7, 0f, 90f, 55f, 80f),    // 보조개 (201.6s)
        };
        public static KpopTrackMeta KpopTrack { get; private set; } = new KpopTrackMeta(4, 0f, 90f, 55f, 80f);
        /// 스테이지 경과(초). StageManager 가 매 프레임 넣는다.
        public static float KpopElapsed { get; private set; }
        public static float KpopProgress01 => KpopMode && KpopTrack.length > 0f ? Mathf.Clamp01(KpopElapsed / KpopTrack.length) : 0f;
        public static float KpopSecondsLeft => Mathf.Max(0f, KpopTrack.length - KpopElapsed);
        /// 후렴 구간(코인 ×2, 피버 제안).
        public static bool KpopChorus => KpopMode && KpopElapsed >= KpopTrack.chorusStart && KpopElapsed < KpopTrack.chorusEnd;
        /// 아웃트로(마지막 8초): 장애물 없음, 리본.
        public const float KpopOutroSeconds = 8f;
        /// 48차-7(사용자): 러닝 시작 뒤 1초 쉬었다가 곡이 나온다. 곡 시계(KpopElapsed)도 그만큼 늦게 출발.
        public const float KpopMusicDelay = 1f;
        public static bool KpopOutro => KpopMode && KpopElapsed >= KpopTrack.length - KpopOutroSeconds;
        /// 이번 런에서 곡 끝까지 달렸나(리본 통과).
        public static bool KpopFinished { get; private set; }
        /// 후렴 구간에서 꼬마 피버를 켠 횟수(미션 Fever).
        public static int FeverInChorus { get; private set; }
        public static void NoteFever() { if (KpopMode && KpopChorus) FeverInChorus++; }
        /// 오늘 누적 미션 비트(런을 넘어 남는다) — 결과 카드·타이틀 표시용.
        public static int KpopMissionMaskToday(MetaProfile p) => p != null && p.kpopMissionDate == Today ? p.kpopMissionDoneMask : 0;
        public static int KpopMissionCountToday(MetaProfile p) { int m = KpopMissionMaskToday(p), n = 0; for (int i = 0; i < 3; i++) if ((m & (1 << i)) != 0) n++; return n; }
        public static bool KpopStampedToday(MetaProfile p) => DailyDoneToday(p);
        /// 이번 런 또는 오늘 누적으로 이미 이룬 미션인가(HUD 칩·결과 카드 공용).
        public static bool IsKpopMissionDone(int i)
        {
            if (i < 0 || i >= 3) return false;
            if (ConditionDone[i]) return true;
            var p = GameManager.I != null ? GameManager.I.Profile : null;
            return (KpopMissionMaskToday(p) & (1 << i)) != 0;
        }
        /// 코인 감쇠: 오늘 4번째 런부터 ×0.5, 8번째부터 ×0.25(파밍 방지, 점수·미션은 감쇠 없음).
        public static float KpopCoinDecay(MetaProfile p)
        {
            if (p == null || p.kpopRunsDate != Today) return 1f;
            return p.kpopRunsToday >= 7 ? 0.25f : p.kpopRunsToday >= 3 ? 0.5f : 1f;
        }
        public const int KpopStampCoins = 50, KpopAllClearCoins = 100, KpopFinishBonus = 300, KpopFlawlessBonus = 200;
        /// 결과 카드가 읽는 이번 런 보상 요약.
        public static int LastStampCoins { get; private set; }
        public static int LastAllClearCoins { get; private set; }
        public static bool LastAllClear { get; private set; }
        public static bool LastNewBest { get; private set; }

        private const string PrefLastTrack = "CoastRun_KpopLastTrack";
        static KpopTrackMeta PickTrack()
        {
            int last = PlayerPrefs.GetInt(PrefLastTrack, 0);
            var pool = new List<KpopTrackMeta>();
            foreach (var t in KpopTracks) if (t.num != last || KpopTracks.Length == 1) pool.Add(t);
            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            PlayerPrefs.SetInt(PrefLastTrack, pick.num);
            return pick;
        }

        /// K-POP 일일 미션 3개: 슬롯 0 = 곡 끝까지(고정) + 풀에서 2개(날짜 시드, 같은 종류 없음). 값은 90초 기준.
        public static DailyCondition[] MakeKpopConditions(int seed)
        {
            var rng = new System.Random(seed * 131 + 11);
            var pool = new List<DailyCondition>
            {
                new DailyCondition(MissionKind.Coins, 50 + rng.Next(0, 4) * 20),       // 50~110
                new DailyCondition(MissionKind.NearMiss, 6 + rng.Next(0, 5) * 2),      // 6~14
                new DailyCondition(MissionKind.Combo, 3 + rng.Next(0, 4)),             // 3~6
                new DailyCondition(MissionKind.Hearts, 6 + rng.Next(0, 5) * 2),        // 6~14
                new DailyCondition(MissionKind.MaxHits, rng.Next(0, 3)),               // 0~2
                new DailyCondition(MissionKind.Fever, 1),
            };
            if (rng.Next(0, 7) == 0) pool.Add(new DailyCondition(MissionKind.NoHit, 0));   // 주 1회쯤
            var picked = new List<DailyCondition> { new DailyCondition(MissionKind.Finish, 0) };
            while (picked.Count < 3 && pool.Count > 0) { int i = rng.Next(pool.Count); picked.Add(pool[i]); pool.RemoveAt(i); }
            return picked.ToArray();
        }

        /// 챕터 난이도 가상 스테이지: 250m마다 1스테이지. 20을 넘어도 계속 오른다(최대 26).
        public static float VirtualStage => Mathf.Min(26f, 1f + Distance / 250f);

        public static int Today => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        public static bool DailyDoneToday(MetaProfile p) => p != null && p.lastDailyDate == Today;

        /// 계절 해금: 그 계절의 첫 챕터를 클리어했으면 열린다(봄은 항상).
        public static bool SeasonUnlocked(MetaProfile p, SeasonKind s)
        {
            if (s == SeasonKind.Spring) return true;
            int first = (int)s * 5;   // Summer=1 → 트랙 6(index 5)
            return p != null && p.trackGrade != null && first < p.trackGrade.Length && p.trackGrade[first] > 0;
        }

        public static SeasonKind DailySeason(int dateSeed, MetaProfile p)
        {
            var s = (SeasonKind)(dateSeed % 4);
            if (!SeasonUnlocked(p, s)) s = SeasonKind.Spring;
            return s;
        }

        public static DailyCondition[] MakeConditions(int seed)
        {
            var rng = new System.Random(seed * 31 + 7);
            var pool = new List<DailyCondition>
            {
                new DailyCondition(MissionKind.Fast, 800 + rng.Next(0, 5) * 200),      // 800~1600m
                new DailyCondition(MissionKind.NearMiss, 8 + rng.Next(0, 4) * 4),      // 8~20
                new DailyCondition(MissionKind.Coins, 60 + rng.Next(0, 4) * 20),       // 60~120
                new DailyCondition(MissionKind.Combo, 4 + rng.Next(0, 4)),             // 4~7
                new DailyCondition(MissionKind.Hearts, 8 + rng.Next(0, 3) * 4),        // 8~16
                new DailyCondition(MissionKind.MaxHits, rng.Next(0, 3)),               // 0~2
            };
            var picked = new List<DailyCondition>();
            picked.Add(pool[0]);                                   // 거리 조건은 항상
            pool.RemoveAt(0);
            while (picked.Count < 3) { int i = rng.Next(pool.Count); picked.Add(pool[i]); pool.RemoveAt(i); }
            return picked.ToArray();
        }

        static int StageFor(SeasonKind s, System.Random rng) => (int)s * 5 + 1 + rng.Next(5);

        /// 시작. profile은 모드(러닝/보드)·계절 해금에 쓴다. fromRaising이면 끝나고 육성으로.
        public static void Start(ArcadeKind kind, GameManager gm, SeasonKind? season = null, bool fromRaising = false)
        {
            var p = gm != null ? gm.Profile : null;
            Kind = kind;
            ReturnToRaising = fromRaising;
            if (kind == ArcadeKind.Daily)
            {
                Seed = Today;
                Season = DailySeason(Seed, p);
                Conditions = MakeConditions(Seed);
            }
            else
            {
                Seed = Environment.TickCount;
                Season = season ?? SeasonKind.Spring;
                if (!SeasonUnlocked(p, Season)) Season = SeasonKind.Spring;
                Conditions = new DailyCondition[0];
            }
            ConditionDone = new bool[3];
            var rng = new System.Random(Seed);
            StageIndex = StageFor(Season, rng);
            Distance = 0f; HitsFirst500 = 0; LastScore = 0; LastStamped = false;

            RunTuning.Reset();
            RunTuning.HasSeason = true;
            RunTuning.Season = Season;
            RunTuning.Mode = (p != null && p.skateboardUnlocked && PlayerPrefs.GetInt("CoastRun_ArcadeBoard", 0) == 1) ? RunMode.Skateboard : RunMode.Running;
            if (RunTuning.Mode == RunMode.Skateboard) { RunTuning.SpeedMul = 1.3f; RunTuning.CoinMul = 1.3f; }
            RunTuning.Pet = PetCompanion.Selected;
            ObstacleSpawner.SeedOverride = Seed;

            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            UnityEngine.Object.FindAnyObjectByType<TitleAudio>()?.StopMenu();
            flow?.StartStoryRun(StageIndex, false);
        }

        /// 26차: K-POP 러닝모드 시작. 육성 세이브가 있으면 그 스탯으로(RunTuning.Configure), 없으면 기본값.
        /// 계절은 해금된 것 중 가장 늦은 계절, 코스는 매번 새 시드.
        // 39차-4: K-POP 챕터 선택. 마지막 클리어 챕터(PlayerPrefs)와 사용자가 고른 챕터(0 = 자동 = 마지막 클리어 + 1).
        private const string PrefLastClear = "CoastRun_KpopLastClear", PrefPick = "CoastRun_KpopPick";
        public static int KpopLastClear => PlayerPrefs.GetInt(PrefLastClear, 0);
        public static int KpopPick => PlayerPrefs.GetInt(PrefPick, 0);
        public static void SetKpopPick(int chapter) { PlayerPrefs.SetInt(PrefPick, Mathf.Clamp(chapter, 0, Timeline.Chapters)); PlayerPrefs.Save(); }
        /// 실제로 달릴 챕터: 고른 게 있으면 그것, 없으면 마지막 클리어 다음(없으면 1, 20 넘으면 20).
        public static int KpopChapter(GameManager gm)
        {
            int pick = KpopPick;
            if (pick >= 1 && pick <= Timeline.Chapters) return pick;
            return Mathf.Clamp(KpopLastClear + 1, 1, Timeline.Chapters);
        }
        /// K-POP 모드에서 스테이지를 클리어할 때(SceneFlowController.EnterStageClear) — 다음 자동 선택이 한 칸 나아간다. 고른 챕터는 초기화(다음엔 자동).
        public static void NoteKpopClear(int stage)
        {
            if (!KpopMode) return;
            if (stage > KpopLastClear) PlayerPrefs.SetInt(PrefLastClear, Mathf.Clamp(stage, 1, Timeline.Chapters));
            if (KpopPick == stage) PlayerPrefs.SetInt(PrefPick, 0);
            PlayerPrefs.Save();
        }

        public static void StartKpop(GameManager gm) => StartKpop(gm, KpopChapter(gm));

        /// 39차-4: 챕터를 지정해 K-POP 러닝 시작. 계절은 챕터의 계절(5챕터 = 1계절), 코스는 매번 새 시드.
        public static void StartKpop(GameManager gm, int chapter)
        {
            var p = gm != null ? gm.Profile : null;
            var save = gm != null ? gm.PeekSave() : null;
            // 48차: 「한 곡 달리기」 — 날짜 시드(오늘의 코스), 미션 3(완주 고정 + 2), 곡은 M2/M4/M7 랜덤. Kind 는 Endless 로 두되
            //        정산은 KpopMode 분기(SettleKpop)로 간다(도장·스트릭은 옛 「오늘의 런」 필드 재사용).
            Kind = ArcadeKind.Endless;
            KpopMode = true;
            ReturnToRaising = false;
            Seed = Today;
            chapter = Mathf.Clamp(chapter, 1, Timeline.Chapters);
            Season = (SeasonKind)((chapter - 1) / 5);
            Conditions = MakeKpopConditions(Seed);
            ConditionDone = new bool[3];
            StageIndex = chapter;
            Distance = 0f; HitsFirst500 = 0; LastScore = 0; LastStamped = false;
            KpopTrack = PickTrack();
            KpopElapsed = 0f; KpopFinished = false; FeverInChorus = 0;
            LastStampCoins = 0; LastAllClearCoins = 0; LastAllClear = false; LastNewBest = false;

            RunTuning.Configure(save);   // 세이브 null이면 Reset()과 같다
            RunTuning.HasSeason = true;
            RunTuning.Season = Season;
            if (save != null) RunTuning.Mode = save.runMode;
            if (RunTuning.Mode == RunMode.Skateboard) { RunTuning.SpeedMul = 1.3f; RunTuning.CoinMul = 1.3f; }
            RunTuning.CoinMul *= KpopCoinDecay(p);
            RunTuning.Pet = save != null ? save.equippedPet : PetCompanion.Selected;
            ObstacleSpawner.SeedOverride = Seed * 7 + chapter;

            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            UnityEngine.Object.FindAnyObjectByType<TitleAudio>()?.StopMenu();
            flow?.StartStoryRun(StageIndex, false);
        }

        /// 스테이지 시작(재시도 포함)마다.
        public static void OnStageBegin()
        {
            Distance = 0f; HitsFirst500 = 0; ConditionDone = new bool[3];
            KpopElapsed = 0f; KpopFinished = false; FeverInChorus = 0;
            LastStampCoins = 0; LastAllClearCoins = 0; LastAllClear = false; LastNewBest = false;
            if (KpopMode)
            {
                // 오늘 이미 이룬 미션은 켜진 채로 시작(런을 넘어 누적) — 재도전 때 다시 안 시켜도 된다
                var p = GameManager.I != null ? GameManager.I.Profile : null;
                int m = KpopMissionMaskToday(p);
                for (int i = 0; i < 3; i++) if ((m & (1 << i)) != 0) ConditionDone[i] = true;
                // 코인 감쇠는 런마다 다시(재도전도 한 번으로 센다)
                if (p != null)
                {
                    if (p.kpopRunsDate != Today) { p.kpopRunsDate = Today; p.kpopRunsToday = 0; }
                    p.kpopRunsToday++;
                }
            }
        }

        public static void Tick(float localDistance, StageRunStats s)
        {
            Distance = Mathf.Max(0f, localDistance);
            if ((Kind == ArcadeKind.Daily || KpopMode) && s != null)
                for (int i = 0; i < Conditions.Length && i < 3; i++)
                    if (!ConditionDone[i] && Conditions[i].LiveCheckable && Conditions[i].Check(s, Distance, HitsFirst500, false))
                    {
                        ConditionDone[i] = true;
                        if (KpopMode) OnKpopMissionDone?.Invoke(i);
                    }
        }

        /// 48차: StageManager 가 매 프레임 넣는 곡 경과. 미션 칩 갱신 이벤트.
        public static void TickKpop(float elapsed) { KpopElapsed = elapsed; }
        public static event Action<int> OnKpopMissionDone;
        /// 리본 통과 — 완주 확정. 이후 Settle(finished=true).
        public static void MarkKpopFinished()
        {
            if (!KpopMode || KpopFinished) return;
            KpopFinished = true;
            for (int i = 0; i < Conditions.Length && i < 3; i++)
                if (!ConditionDone[i] && Conditions[i].Check(StageRunStats.Instance, Distance, HitsFirst500, true)) { ConditionDone[i] = true; OnKpopMissionDone?.Invoke(i); }
        }

        public static void OnHit() { if (Distance < 500f) HitsFirst500++; }

        public static int Score(StageRunStats s) => Mathf.RoundToInt(Distance) + (s != null ? s.Coins * 5 + s.NearMissValue + s.Hearts * 20 : 0)
            + (KpopMode && KpopFinished ? KpopFinishBonus + (s != null && s.SoftHits == 0 ? KpopFlawlessBonus : 0) : 0);

        /// 사망 시 정산. 결과 문자열은 UI가 그린다.
        public static void Settle(GameManager gm, StageRunStats s)
        {
            var p = gm != null ? gm.Profile : null;
            LastScore = Score(s);
            if (p == null) return;
            p.EnsureArrays();
            if (KpopMode) { SettleKpop(gm, p, s); return; }
            p.totalArcadeRuns++; p.totalRuns++;
            p.totalDistance += Mathf.RoundToInt(Distance);
            if (s != null) { p.totalCoins += s.Coins; p.totalNearMiss += s.NearMissCount; p.totalHearts += s.Hearts; }
            if (Kind == ArcadeKind.Endless)
            {
                if (LastScore > p.endlessBestScore) p.endlessBestScore = LastScore;
                if (Distance > p.endlessBestDist) p.endlessBestDist = Mathf.RoundToInt(Distance);
            }
            else
            {
                if (LastScore > p.dailyBestScore) p.dailyBestScore = LastScore;
                bool all = true;
                for (int i = 0; i < Conditions.Length && i < 3; i++)
                {
                    if (Conditions[i].Check(s, Distance, HitsFirst500)) ConditionDone[i] = true;
                    all &= ConditionDone[i];
                }
                if (all && p.lastDailyDate != Today)
                {
                    LastStamped = true;
                    var list = new List<int>(p.dailyStamps) { Today };
                    p.dailyStamps = list.ToArray();
                    int yesterday = int.Parse(DateTime.Now.AddDays(-1).ToString("yyyyMMdd"));
                    p.dailyStreak = p.lastDailyDate == yesterday ? p.dailyStreak + 1 : 1;
                    p.dailyStreakBest = Mathf.Max(p.dailyStreakBest, p.dailyStreak);
                    p.lastDailyDate = Today;
                }
            }
            // 28차: 방 장식 드롭(800 m 이상 달렸을 때 25%)
            if (Distance >= 800f) RoomDeco.TryDropFromRun(p, Seed * 7 + Mathf.RoundToInt(Distance) + LastScore, 0.25f);
            gm.WriteProfileNow();
            AchievementTable.CheckAndToast(gm);
        }

        /// 48차: K-POP 한 곡 달리기 정산 — 완주면 오늘 첫 도장(스트릭), 미션 비트 누적, 3개 다 되면 올클리어 보상, 기록 갱신.
        static void SettleKpop(GameManager gm, MetaProfile p, StageRunStats s)
        {
            p.totalArcadeRuns++; p.totalRuns++;
            p.totalDistance += Mathf.RoundToInt(Distance);
            if (s != null) { p.totalCoins += s.Coins; p.totalNearMiss += s.NearMissCount; p.totalHearts += s.Hearts; }

            // 기록
            if (LastScore > p.dailyBestScore) { p.dailyBestScore = LastScore; LastNewBest = true; }
            if (p.kpopTodayDate != Today) { p.kpopTodayDate = Today; p.kpopTodayBest = 0; }
            if (LastScore > p.kpopTodayBest) p.kpopTodayBest = LastScore;
            int ci = Mathf.Clamp(StageIndex, 1, 20) - 1;
            if (LastScore > p.kpopBestByChapter[ci]) p.kpopBestByChapter[ci] = LastScore;

            // 미션 누적(오늘)
            if (p.kpopMissionDate != Today) { p.kpopMissionDate = Today; p.kpopMissionDoneMask = 0; }
            for (int i = 0; i < Conditions.Length && i < 3; i++)
            {
                if (!ConditionDone[i] && Conditions[i].Check(s, Distance, HitsFirst500, KpopFinished)) ConditionDone[i] = true;
                if (ConditionDone[i]) p.kpopMissionDoneMask |= 1 << i;
            }
            var wallet = UnityEngine.Object.FindAnyObjectByType<CoinWallet>();

            // 도장: 오늘 첫 완주
            if (KpopFinished)
            {
                p.kpopSongsFinished++;
                if (p.lastDailyDate != Today)
                {
                    LastStamped = true;
                    var list = new List<int>(p.dailyStamps) { Today };
                    p.dailyStamps = list.ToArray();
                    int yesterday = int.Parse(DateTime.Now.AddDays(-1).ToString("yyyyMMdd"));
                    p.dailyStreak = p.lastDailyDate == yesterday ? p.dailyStreak + 1 : 1;
                    p.dailyStreakBest = Mathf.Max(p.dailyStreakBest, p.dailyStreak);
                    p.lastDailyDate = Today;
                    LastStampCoins = KpopStampCoins;
                    wallet?.Add(KpopStampCoins);
                }
            }
            // 올클리어(하루 한 번)
            bool all = Conditions.Length >= 3;
            for (int i = 0; i < Conditions.Length && i < 3; i++) all &= (p.kpopMissionDoneMask & (1 << i)) != 0;
            if (all)
            {
                LastAllClear = true;
                if (p.kpopAllClearDate != Today)
                {
                    p.kpopAllClearDate = Today;
                    LastAllClearCoins = KpopAllClearCoins;
                    wallet?.Add(KpopAllClearCoins);
                    RoomDeco.TryDropFromRun(p, Seed * 13 + LastScore, 0.25f);   // 장식 상자 25%
                }
            }
            wallet?.Persist();
            gm.WriteProfileNow();
            AchievementTable.CheckAndToast(gm);
        }

        /// 결과창 '나가기'.
        public static void Exit()
        {
            bool toRaising = ReturnToRaising && GameManager.Active;
            Kind = ArcadeKind.None;
            KpopMode = false;
            KpopFinished = false; KpopElapsed = 0f; FeverInChorus = 0;
            ObstacleSpawner.SeedOverride = null;
            RunTuning.Reset();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (toRaising) GameManager.I.EnterRaising();
            else if (flow != null) _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
        }

        public static string SeasonName(SeasonKind s)
        {
            switch (s)
            {
                case SeasonKind.Spring: return Loc.T("봄", "Spring");
                case SeasonKind.Summer: return Loc.T("여름", "Summer");
                case SeasonKind.Autumn: return Loc.T("가을", "Autumn");
                default: return Loc.T("겨울", "Winter");
            }
        }
    }
}
