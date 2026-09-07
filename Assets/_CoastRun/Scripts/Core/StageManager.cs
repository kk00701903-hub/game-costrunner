using System;
using UnityEngine;

namespace CoastRun
{
    /// 20-stage × 5-chapter progression in a single run scene (tile streaming, no scene loads).
    /// lightingT never rewinds past the current stage start — retry resets to lightingTStart only.
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [SerializeField] private StageTable table;
        [SerializeField] private PlayerController player;
        [SerializeField] private DynamicEnvironmentManager environment;
        [SerializeField] private StageClearUI clearUi;
        [SerializeField] private UI_FeedbackController feedback;

        private StageDef _current;
        private float _stageOriginDistance;
        private float _stageElapsed;
        private bool _stageActive;
        private bool _awaitingContinue;

        public int ChapterIndex { get; private set; } = 1;
        public int StageIndex { get; private set; } = 1;
        public StageDef Current => _current;
        public bool IsStageActive => _stageActive;
        public float StageProgress01
        {
            get
            {
                if (_current == null || _current.targetDistance <= 0.01f || player == null)
                    return 0f;
                return Mathf.Clamp01(StageLocalDistance / _current.targetDistance);
            }
        }

        public float StageLocalDistance =>
            player != null ? Mathf.Max(0f, player.PathDistance - _stageOriginDistance) : 0f;

        /// Metres completed before the current stage origin (sum of prior stage lengths).
        public float JourneyDistanceCompletedBefore
        {
            get
            {
                if (table == null || _current == null)
                    return 0f;
                float sum = 0f;
                for (int i = 0; i < table.stages.Length; i++)
                {
                    var s = table.stages[i];
                    if (s == null || s.stageIndex >= _current.stageIndex)
                        break;
                    sum += Mathf.Max(0f, s.targetDistance);
                }

                return sum;
            }
        }

        public float TotalJourneyDistance
        {
            get
            {
                if (table == null)
                {
                    table = CoastConfigRegistry.StageTable;
                    table.EnsurePopulated();
                }

                float sum = 0f;
                for (int i = 0; i < table.stages.Length; i++)
                {
                    if (table.stages[i] != null)
                        sum += Mathf.Max(0f, table.stages[i].targetDistance);
                }

                return Mathf.Max(1f, sum);
            }
        }

        /// 0..1 across all 20 stages (not per-stage).
        public float JourneyProgress01 =>
            Mathf.Clamp01((JourneyDistanceCompletedBefore + StageLocalDistance) / TotalJourneyDistance);

        public float RemainingJourneyDistance =>
            Mathf.Max(0f, TotalJourneyDistance - (JourneyDistanceCompletedBefore + StageLocalDistance));

        public event Action<StageDef> OnStageStart;
        public event Action<StageDef> OnStageClear;
        public event Action<int> OnChapterComplete;

        public void Bind(StageTable stageTable, PlayerController playerController,
            DynamicEnvironmentManager env, StageClearUI ui, UI_FeedbackController feedbackUi)
        {
            table = stageTable != null ? stageTable : CoastConfigRegistry.StageTable;
            table.EnsurePopulated();
            player = playerController;
            environment = env;
            clearUi = ui;
            feedback = feedbackUi;
            Instance = this;
        }

        private void OnEnable() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginCampaign(int startStageIndex = 1)
        {
            if (table == null)
            {
                table = CoastConfigRegistry.StageTable;
                table.EnsurePopulated();
            }

            LoadStage(Mathf.Clamp(startStageIndex, 1, Mathf.Max(1, table.Count)));
        }

        public void LoadStage(int stageIndex)
        {
            if (table == null)
            {
                table = CoastConfigRegistry.StageTable;
                table.EnsurePopulated();
            }

#if UNITY_EDITOR
            // Dev-only: "Coast Run/PLAY from stage N" menus park a one-shot stage here,
            // honoured by whichever entry point loads the first stage of the session.
            int devStage = PlayerPrefs.GetInt(GameSession.DevStartStageKey, 0);
            if (devStage > 0)
            {
                PlayerPrefs.DeleteKey(GameSession.DevStartStageKey);
                stageIndex = devStage;
                Debug.Log("[StageManager] Dev start stage " + stageIndex);
            }
#endif

            var def = table.GetByIndex(stageIndex);
            if (def == null)
            {
                Debug.LogWarning("[StageManager] Missing stage " + stageIndex);
                return;
            }

            _awaitingContinue = false;
            clearUi?.Hide();

            // Two managers can coexist for a frame or two during scene handoffs (the
            // bootstrap's and the run scene's). Whoever actually runs a stage is the one
            // spawners and HUD must read, so claim the singleton here.
            Instance = this;

            _current = def;
            StageIndex = def.stageIndex;
            ChapterIndex = def.chapterIndex;
            _stageOriginDistance = player != null ? player.PathDistance : 0f;
            _stageElapsed = 0f;
            _stageActive = true;

            // Snap lighting to this stage's start — never earlier than that start for this load.
            environment?.ResetLightingTo(def.lightingTStart);
            BeginSunsetClock();

            if (player != null && !player.enabled)
                player.enabled = true;

            OnStageStart?.Invoke(def);
        }

        /// Editor aid: warp the player to 30 m before the finish so a clear can be tested.
        public void DebugWarpToFinish()
        {
            if (_current == null || player == null || !_stageActive) return;
            player.SetPathDistance(_stageOriginDistance + Mathf.Max(0f, _current.targetDistance - 30f));
        }

        /// Fail / manual retry — path rewinds to stage origin; lighting only to lightingTStart.
        public void RetryCurrent()
        {
            if (_current == null || player == null)
                return;

            _awaitingContinue = false;
            clearUi?.Hide();

            player.SetPathDistance(_stageOriginDistance);
            player.ResetSoftState();
            _stageElapsed = 0f;
            _stageActive = true;

            environment?.ResetLightingTo(_current.lightingTStart);
            BeginSunsetClock();
            OnStageStart?.Invoke(_current);
        }

        public void ContinueToNext()
        {
            if (_current == null)
                return;

            _awaitingContinue = false;
            clearUi?.Hide();

            int next = _current.stageIndex + 1;
            if (next > table.Count)
            {
                _stageActive = false;
                feedback?.ShowWatchMessage("COMPLETE", "송전탑에 도착했어.");
                var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
                session?.EndRun();
                return;
            }

            // Same scene: origin advances from current path (seamless tile stream).
            LoadStage(next);
        }

        // ── 8차 노을 규칙 ───────────────────────────────────────────
        /// 해가 지기까지 걸리는 시간(초). 코스 거리/평균 속도 × RunTuning.SunsetGrace(체력).
        public float SunsetSeconds { get; private set; } = 90f;
        /// 0 = 해가 높다, 1 = 해가 졌다.
        public float SunsetT { get; private set; }
        /// 해가 진 뒤에도 아직 도착 못 함(늦음). 정산·컷씬 분기에 쓴다.
        public bool SunsetLate { get; private set; }
        /// 마지막으로 끝난 스테이지가 늦었는지(정산용, 씬을 넘어도 유지).
        public static bool LastRunLate;
        public static float LastRunSunsetT;
        const float SunsetLightT = 0.90f;   // 이 t에서 해가 수평선에 닿는다

        void BeginSunsetClock()
        {
            float avgSpeed = 15.5f * Mathf.Max(0.8f, RunTuning.SpeedMul);
            float par = _current != null ? _current.targetDistance / avgSpeed : 60f;
            SunsetSeconds = Mathf.Max(20f, par * RunTuning.SunsetGrace);
            SunsetT = 0f; SunsetLate = false;
        }

        private void Update()
        {
            if (!_stageActive || _awaitingContinue || _current == null || player == null)
                return;

            _stageElapsed += Time.deltaTime;
#if UNITY_EDITOR
            // 에디터 검증용: Home = 노을 5초 전으로, End = 스테이지 즉시 클리어(정산 화면 확인). (F키는 에디터 단축키와 겹친다)
            if (Input.GetKeyDown(KeyCode.Home) && !ArcadeRun.Active) _stageElapsed = Mathf.Max(_stageElapsed, SunsetSeconds - 5f);
            if (Input.GetKeyDown(KeyCode.End) && !ArcadeRun.Active) { ClearCurrent(); return; }
#endif

            float u = StageProgress01;
            float t;
            if (ArcadeRun.Active)
            {
                t = Mathf.Lerp(_current.lightingTStart, _current.lightingTEnd, u);
            }
            else
            {
                // 노을 규칙: 조명은 거리가 아니라 '시간'으로 저문다. 늦으면 해가 진 뒤(블루아워)까지 간다.
                SunsetT = Mathf.Clamp01(_stageElapsed / SunsetSeconds);
                float sun = Mathf.SmoothStep(0f, 1f, SunsetT);
                t = Mathf.Lerp(_current.lightingTStart, SunsetLightT, sun);
                if (_stageElapsed > SunsetSeconds)
                {
                    if (!SunsetLate) { SunsetLate = true; RunHudChrome.Instance?.OnSunsetPassed(); }
                    t = Mathf.Lerp(SunsetLightT, 1f, Mathf.Clamp01((_stageElapsed - SunsetSeconds) / 18f));
                }
                RunHudChrome.Instance?.SetSunset(SunsetT, SunsetLate);
            }
            // Monotonic within the stage; retry uses ResetLightingTo instead.
            environment?.SetTime(t);

            if (_current.timeLimit > 0.01f && _stageElapsed >= _current.timeLimit && u < 1f)
            {
                feedback?.ShowWatchMessage("TIME UP", _current.stageName);
                RetryCurrent();
                return;
            }

            // 아케이드: 끝이 없다. 코스 끝에 닿아도 계속 달리고, 조명은 노을에 고정.
            if (ArcadeRun.Active)
            {
                ArcadeRun.Tick(StageLocalDistance, StageRunStats.Instance);
                return;
            }

            if (u >= 1f)
                ClearCurrent();
        }

        private void ClearCurrent()
        {
            if (!_stageActive || _current == null)
                return;

            _stageActive = false;
            _awaitingContinue = true;
            LastRunLate = SunsetLate;
            LastRunSunsetT = SunsetT;

            // Lock lighting where the sun is now (노을 규칙: 도착 시각이 곧 하늘색).
            if (!ArcadeRun.Active) environment?.SetTime(Mathf.Lerp(_current.lightingTStart, SunsetLightT, Mathf.SmoothStep(0f, 1f, SunsetT)));
            else environment?.SetTime(_current.lightingTEnd);

            var cleared = _current;
            bool chapterEnd = IsLastStageOfChapter(cleared);

            OnStageClear?.Invoke(cleared);
            if (chapterEnd)
                OnChapterComplete?.Invoke(cleared.chapterIndex);

            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (flow != null)
            {
                flow.NotifyStageCleared(cleared, chapterEnd);
                return;
            }

            StartCoroutine(LocalClearWithMemory(cleared, chapterEnd));
        }

        private System.Collections.IEnumerator LocalClearWithMemory(StageDef cleared, bool chapterEnd)
        {
            if (cleared.stageIndex >= 20)
                clearUi?.ShowFinal(cleared, ContinueToNext, RetryCurrent);
            else
                clearUi?.Show(cleared, chapterEnd, ContinueToNext, RetryCurrent);

            var mem = MemoryDirector.Instance ?? UnityEngine.Object.FindAnyObjectByType<MemoryDirector>();
            if (mem != null)
                yield return mem.PlayQueuedIfAny();
        }

        private bool IsLastStageOfChapter(StageDef def)
        {
            if (def == null || table == null)
                return false;
            var next = table.GetByIndex(def.stageIndex + 1);
            return next == null || next.chapterIndex != def.chapterIndex;
        }

        /// Chapter-themed prop bias without season cycling.
        public static SeasonKind ChapterAsSeason(int chapter)
        {
            // v2: 육성 타임라인이 계절을 정한다(52주 = 4계절). 레거시 직행 플레이만 막 테마.
            if (RunTuning.HasSeason)
                return RunTuning.Season;
            switch (Mathf.Clamp(chapter, 1, 5))
            {
                case 1: return SeasonKind.Summer;
                case 2: return SeasonKind.Spring;
                case 3: return SeasonKind.Autumn;
                case 4: return SeasonKind.Autumn;
                default: return SeasonKind.Winter;
            }
        }
    }
}
