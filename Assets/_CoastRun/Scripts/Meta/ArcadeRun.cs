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
                    case MissionKind.MaxHits: return Loc.T($"첫 500m 피격 {value}회 이하", $"≤{value} hits in first 500 m");
                    default: return Loc.T($"{value}m 달리기", $"Run {value} m");
                }
            }
        }
        public bool Check(StageRunStats s, float dist, int hitsFirst500)
        {
            switch (kind)
            {
                case MissionKind.NearMiss: return s != null && s.NearMissCount >= value;
                case MissionKind.Coins: return s != null && s.Coins >= value;
                case MissionKind.Combo: return s != null && s.BestCombo >= value;
                case MissionKind.Hearts: return s != null && s.Hearts >= value;
                case MissionKind.MaxHits: return dist >= 500f && hitsFirst500 <= value;
                default: return dist >= value;
            }
        }
    }

    public static class ArcadeRun
    {
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
        public static void StartKpop(GameManager gm)
        {
            var p = gm != null ? gm.Profile : null;
            var save = gm != null ? gm.PeekSave() : null;
            Kind = ArcadeKind.Endless;
            KpopMode = true;
            ReturnToRaising = false;
            Seed = Environment.TickCount;
            Season = SeasonKind.Spring;
            for (int s = 3; s >= 0; s--)
                if (SeasonUnlocked(p, (SeasonKind)s)) { Season = (SeasonKind)s; break; }
            Conditions = new DailyCondition[0];
            ConditionDone = new bool[3];
            var rng = new System.Random(Seed);
            StageIndex = StageFor(Season, rng);
            Distance = 0f; HitsFirst500 = 0; LastScore = 0; LastStamped = false;

            RunTuning.Configure(save);   // 세이브 null이면 Reset()과 같다
            RunTuning.HasSeason = true;
            RunTuning.Season = Season;
            if (save != null) RunTuning.Mode = save.runMode;
            if (RunTuning.Mode == RunMode.Skateboard) { RunTuning.SpeedMul = 1.3f; RunTuning.CoinMul = 1.3f; }
            RunTuning.Pet = save != null ? save.equippedPet : PetCompanion.Selected;
            ObstacleSpawner.SeedOverride = Seed;

            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            UnityEngine.Object.FindAnyObjectByType<TitleAudio>()?.StopMenu();
            flow?.StartStoryRun(StageIndex, false);
        }

        /// 스테이지 시작(재시도 포함)마다.
        public static void OnStageBegin() { Distance = 0f; HitsFirst500 = 0; ConditionDone = new bool[3]; }

        public static void Tick(float localDistance, StageRunStats s)
        {
            Distance = Mathf.Max(0f, localDistance);
            if (Kind == ArcadeKind.Daily && s != null)
                for (int i = 0; i < Conditions.Length && i < 3; i++)
                    if (!ConditionDone[i] && Conditions[i].kind != MissionKind.MaxHits && Conditions[i].Check(s, Distance, HitsFirst500)) ConditionDone[i] = true;
        }

        public static void OnHit() { if (Distance < 500f) HitsFirst500++; }

        public static int Score(StageRunStats s) => Mathf.RoundToInt(Distance) + (s != null ? s.Coins * 5 + s.NearMissValue + s.Hearts * 20 : 0);

        /// 사망 시 정산. 결과 문자열은 UI가 그린다.
        public static void Settle(GameManager gm, StageRunStats s)
        {
            var p = gm != null ? gm.Profile : null;
            LastScore = Score(s);
            if (p == null) return;
            p.EnsureArrays();
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
            gm.WriteProfileNow();
            AchievementTable.CheckAndToast(gm);
        }

        /// 결과창 '나가기'.
        public static void Exit()
        {
            bool toRaising = ReturnToRaising && GameManager.Active;
            Kind = ArcadeKind.None;
            KpopMode = false;
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
