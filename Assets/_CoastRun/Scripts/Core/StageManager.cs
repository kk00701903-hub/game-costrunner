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

            if (player != null && !player.enabled)
                player.enabled = true;

            OnStageStart?.Invoke(def);
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

        private void Update()
        {
            if (!_stageActive || _awaitingContinue || _current == null || player == null)
                return;

            _stageElapsed += Time.deltaTime;

            float u = StageProgress01;
            float t = Mathf.Lerp(_current.lightingTStart, _current.lightingTEnd, u);
            // Monotonic within the stage; retry uses ResetLightingTo instead.
            environment?.SetTime(t);

            if (_current.timeLimit > 0.01f && _stageElapsed >= _current.timeLimit && u < 1f)
            {
                feedback?.ShowWatchMessage("TIME UP", _current.stageName);
                RetryCurrent();
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

            // Lock lighting at stage end (do not dip).
            environment?.SetTime(_current.lightingTEnd);

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
