using System;
using UnityEngine;

namespace CoastRun
{
    public enum FlowState
    {
        Boot,
        Title,
        Cutscene,
        Run,
        StageClear,
        Ending,
        Credits,
        Sting
    }

    public enum TransitionType
    {
        None,
        Fade,
        WhiteFlash,
        SlowMotion
    }

    public enum CutsceneKind
    {
        Prologue,
        ChapterOpening,
        ChapterClosing
    }

    /// Persistent root — Story / Stage / Environment / Progression / Flow / UI.
    [DefaultExecutionOrder(-1000)]
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        [SerializeField] private StoryManager story;
        [SerializeField] private StageManager stages;
        [SerializeField] private DynamicEnvironmentManager environment;
        [SerializeField] private ProgressionManager progression;
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private UIRoot uiRoot;
        [SerializeField] private MemoryFragmentLog memoryLog;
        [SerializeField] private MemoryDirector memoryDirector;

        public StoryManager Story => story;
        public StageManager Stages => stages;
        public DynamicEnvironmentManager Environment => environment;
        public ProgressionManager Progression => progression;
        public SceneFlowController Flow => flow;
        public UIRoot UI => uiRoot;
        public MemoryFragmentLog MemoryLog => memoryLog;
        public MemoryDirector Memory => memoryDirector;

        public bool CampaignCleared { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBeforeSceneLoad()
        {
            // Boot scene creates director; this is a safety net if Boot is skipped in editor.
        }

        public static GameDirector EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("GameDirector");
            DontDestroyOnLoad(go);
            var dir = go.AddComponent<GameDirector>();
            dir.BuildChildren();
            return dir;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (flow == null)
                BuildChildren();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void BuildChildren()
        {
            story = GetOrAdd<StoryManager>();
            stages = GetOrAdd<StageManager>();
            environment = GetOrAdd<DynamicEnvironmentManager>();
            progression = GetOrAdd<ProgressionManager>();
            flow = GetOrAdd<SceneFlowController>();
            uiRoot = GetOrAdd<UIRoot>();
            memoryLog = GetOrAdd<MemoryFragmentLog>();
            memoryDirector = GetOrAdd<MemoryDirector>();

            progression.Load();
            StoryDatabase.EnsureLoaded();
            memoryLog.Bind(progression);
            memoryDirector.Bind(memoryLog, stages);
            uiRoot.EnsureBuilt();
            flow.Bind(this);
        }

        private T GetOrAdd<T>() where T : Component
        {
            var c = GetComponent<T>() ?? gameObject.AddComponent<T>();
            return c;
        }
    }
}
