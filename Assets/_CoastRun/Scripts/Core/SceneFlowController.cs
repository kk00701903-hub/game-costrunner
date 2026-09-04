using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoastRun
{
    /// App flow state machine — 5 scenes only (Boot / Title / Run / Cutscene / Ending).
    public class SceneFlowController : MonoBehaviour
    {
        public const string BootScene = "00_Boot";
        public const string TitleScene = "01_Title";
        public const string RunScene = "02_Run";
        public const string CutsceneScene = "03_Cutscene";
        public const string EndingScene = "04_Ending";


        private GameDirector _director;
        private FlowState _state = FlowState.Boot;
        private AsyncOperation _runPreload;
        private bool _runSceneReady;
        private int _pendingChapter = 1;
        private int _pendingStage = 1;
        private CutsceneKind _cutsceneKind = CutsceneKind.Prologue;
        private int _cutsceneChapter = 1;
        private bool _busy;
        private bool _awaitingPrologueHandoff;

        public FlowState State => _state;
        public int PendingChapter => _pendingChapter;
        public int PendingStage => _pendingStage;
        public CutsceneKind ActiveCutsceneKind => _cutsceneKind;
        public int ActiveCutsceneChapter => _cutsceneChapter;
        /// Run is preloaded under prologue; gameplay starts only after P4 camera snap.
        public bool AwaitingPrologueHandoff => _awaitingPrologueHandoff;

        public event Action<FlowState, FlowState> OnStateChanged;

        public void Bind(GameDirector director)
        {
            _director = director;
        }

        public Task GoTo(FlowState next, TransitionType t) =>
            RunTask(GoToRoutine(next, t));

        private Task RunTask(IEnumerator routine)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(Wrap(routine, tcs));
            return tcs.Task;
        }

        private static IEnumerator Wrap(IEnumerator routine, TaskCompletionSource<bool> tcs)
        {
            yield return routine;
            tcs.TrySetResult(true);
        }

        private IEnumerator GoToRoutine(FlowState next, TransitionType transition)
        {
            if (_busy && next != FlowState.StageClear)
                yield break;

            _busy = true;
            var prev = _state;

            yield return PlayTransitionOut(transition, next);

            switch (next)
            {
                case FlowState.Title:
                    yield return LoadSingle(ResolveTitleScene());
                    BeginPreloadRun();
                    break;

                case FlowState.Cutscene:
                    yield return LoadCutsceneAdditive();
                    break;

                case FlowState.Run:
                    yield return ActivateOrLoadRun();
                    break;

                case FlowState.StageClear:
                    // Stays in Run scene — UI only.
                    Time.timeScale = 1f;
                    break;

                case FlowState.Ending:
                    UnloadCutsceneIfAny();
                    yield return LoadSingle(EndingScene);
                    break;

                case FlowState.Credits:
                case FlowState.Sting:
                    // Handled inside Ending scene controller; state only.
                    break;

                case FlowState.Boot:
                    break;
            }

            SetState(next);
            yield return PlayTransitionIn(transition, prev, next);
            _busy = false;
        }

        private void SetState(FlowState next)
        {
            var prev = _state;
            _state = next;
            OnStateChanged?.Invoke(prev, next);
        }

        // ── Public flow entry points ───────────────────────────────────────

        public void BootToTitle()
        {
            StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            SetState(FlowState.Boot);
            _director?.UI?.Snap(1f, Color.black);
            _director?.UI?.SetLoader(true);
            // Warmup tick
            yield return null;
            yield return null;
            _director?.UI?.SetLoader(false);
            yield return GoToRoutine(FlowState.Title, TransitionType.Fade);
        }

        public void OnTitleStartPressed()
        {
            bool skip = PlayerPrefs.GetInt(MainMenuController.SkipPrologueKey, 0) == 1;
            if (skip)
            {
                int cont = PlayerPrefs.GetInt("CoastRun_ContinueStage", 0);
                if (cont > 0)
                {
                    _pendingStage = cont;
                    PlayerPrefs.DeleteKey("CoastRun_ContinueStage");
                }
                else
                {
                    _pendingChapter = 1;
                    _pendingStage = 1;
                }

                // Menu → CH1 BGM crossfade (no prologue).
                UnityEngine.Object.FindAnyObjectByType<TitleAudio>()?.StopMenu();
                StartCoroutine(GoToRoutine(FlowState.Run, TransitionType.Fade));
            }
            else
            {
                _cutsceneKind = CutsceneKind.Prologue;
                _cutsceneChapter = 1;
                _pendingChapter = 1;
                _pendingStage = 1;
                StartCoroutine(PlayPrologueSequence());
            }
        }

        /// Title → Run suspended → Cutscene additive → Timeline. P4 handoff has no fade/cut/lerp.
        private IEnumerator PlayPrologueSequence()
        {
            if (_busy)
                yield break;
            _busy = true;
            var prev = _state;

            yield return PlayTransitionOut(TransitionType.Fade, FlowState.Cutscene);

            _awaitingPrologueHandoff = true;
            // a. 02_Run first — game camera inactive.
            yield return ActivateRunSuspendedForHandoff();
            // b. 03_Cutscene additive + play Prologue.
            yield return LoadAndPlayCutscene();

            SetState(FlowState.Cutscene);
            yield return PlayTransitionIn(TransitionType.Fade, prev, FlowState.Cutscene);
            _busy = false;
            // Completion → OnCutsceneControllerFinished → ExecutePrologueHandoff
        }

        /// Continue from save — always skips prologue.
        public void OnContinuePressed(int stageIndex)
        {
            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 1);
            _pendingStage = Mathf.Clamp(stageIndex, 1, 20);
            _pendingChapter = ((_pendingStage - 1) / 4) + 1;
            UnityEngine.Object.FindAnyObjectByType<TitleAudio>()?.StopMenu();
            StartCoroutine(GoToRoutine(FlowState.Run, TransitionType.Fade));
        }

        /// Legacy entry — prefer OnCutsceneControllerFinished for prologue.
        public void OnPrologueHandoffToRun()
        {
            var ctrl = UnityEngine.Object.FindAnyObjectByType<CutsceneController>();
            StartCoroutine(ExecutePrologueHandoff(ctrl));
        }

        public void NotifyStageCleared(StageDef stage, bool chapterComplete)
        {
            if (stage == null)
                return;

            _pendingChapter = stage.chapterIndex;
            _pendingStage = stage.stageIndex;
            _director?.Progression?.SaveCheckpoint(stage.chapterIndex, stage.stageIndex);

            // S20 → Ending, no fade (BGM drone continues).
            if (stage.stageIndex >= 20)
            {
                StartCoroutine(GoToRoutine(FlowState.Ending, TransitionType.None));
                return;
            }

            StartCoroutine(EnterStageClear(stage, chapterComplete));
        }

        private IEnumerator EnterStageClear(StageDef stage, bool chapterComplete)
        {
            // SlowMotion 0.3s then UI.
            yield return GoToRoutine(FlowState.StageClear, TransitionType.SlowMotion);
            var clear = UnityEngine.Object.FindAnyObjectByType<StageClearUI>();
            if (clear != null)
            {
                clear.Show(stage, chapterComplete,
                    () => OnStageClearContinue(stage, chapterComplete),
                    () => OnStageClearRetry());
            }

            // Memory overlay on top of clear screen — no scene load.
            var mem = MemoryDirector.Instance ?? UnityEngine.Object.FindAnyObjectByType<MemoryDirector>();
            if (mem != null)
                yield return mem.PlayQueuedIfAny();
            else if (clear == null)
                OnStageClearContinue(stage, chapterComplete);
        }

        private void OnStageClearRetry()
        {
            Time.timeScale = 1f;
            UnityEngine.Object.FindAnyObjectByType<StageClearUI>()?.Hide();
            SetState(FlowState.Run);
            StageManager.Instance?.RetryCurrent();
        }

        private void OnStageClearContinue(StageDef stage, bool chapterComplete)
        {
            Time.timeScale = 1f;
            FindFirstObjectByType<StageClearUI>()?.Hide();

            if (chapterComplete)
            {
                // CH5 has no closing — should not happen before S20 (S20 goes Ending).
                if (stage.chapterIndex >= 5)
                {
                    StartCoroutine(GoToRoutine(FlowState.Ending, TransitionType.None));
                    return;
                }

                StartCoroutine(ChapterCutsceneBridge(stage.chapterIndex));
                return;
            }

            _pendingChapter = stage.chapterIndex;
            _pendingStage = stage.stageIndex + 1;
            StartCoroutine(ResumeNextStage());
        }

        private IEnumerator ResumeNextStage()
        {
            yield return GoToRoutine(FlowState.Run, TransitionType.Fade);
            StageManager.Instance?.LoadStage(_pendingStage);
        }

        private IEnumerator ChapterCutsceneBridge(int completedChapter)
        {
            // Closing for completed chapter. CH5 never closes here (S20 → Ending).
            _cutsceneKind = CutsceneKind.ChapterClosing;
            _cutsceneChapter = completedChapter;
            var table = CoastConfigRegistry.CutsceneTable;
            table.EnsurePopulated();
            var def = table.Resolve(CutsceneKind.ChapterClosing, completedChapter);
            var closingTransition = (def != null && def.isTwistCut) ||
                                    completedChapter == 3 || completedChapter == 4
                ? TransitionType.WhiteFlash
                : TransitionType.Fade;
            yield return GoToRoutine(FlowState.Cutscene, closingTransition);
        }

        public void OnCutsceneFinished()
        {
            StartCoroutine(AfterCutscene());
        }

        public void OnCutsceneControllerFinished(CutsceneController ctrl)
        {
            if (_cutsceneKind == CutsceneKind.Prologue)
            {
                StartCoroutine(ExecutePrologueHandoff(ctrl));
                return;
            }

            OnCutsceneFinished();
        }

        /// P4 zero-load handoff: copy cine cam → game cam same frame; no fade/cut/lerp/loader.
        private IEnumerator ExecutePrologueHandoff(CutsceneController ctrl)
        {
            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 1);
            _pendingChapter = 1;
            _pendingStage = 1;

            Camera cine = ctrl != null ? ctrl.CineCamera : null;
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();

            // c–d. Capture + snap + swap entirely before any yield.
            if (session != null && cine != null)
                session.ApplyPrologueCameraSnap(cine);
            else if (cine != null)
                cine.enabled = false;

            // e. Unload cutscene → input → HUD fade-in 0.5s
            UnloadCutsceneIfAny();
            _awaitingPrologueHandoff = false;

            string runName = ResolveRunScene();
            var runScene = SceneManager.GetSceneByName(runName);
            if (runScene.IsValid() && runScene.isLoaded)
                SceneManager.SetActiveScene(runScene);

            SetState(FlowState.Run);
            _director?.UI?.Snap(0f);

            if (session != null)
                yield return session.ReleaseAfterPrologueHandoff();
            else
            {
                StageManager.Instance?.BeginCampaign(1);
            }

            UnityEngine.Object.FindAnyObjectByType<CoastAudioManager>()?.SetBedMuted(false);
        }

        private IEnumerator AfterCutscene()
        {
            UnloadCutsceneIfAny();

            if (_cutsceneKind == CutsceneKind.Prologue)
            {
                yield return ExecutePrologueHandoff(UnityEngine.Object.FindAnyObjectByType<CutsceneController>());
                yield break;
            }

            if (_cutsceneKind == CutsceneKind.ChapterClosing)
            {
                int nextChapter = _cutsceneChapter + 1;
                if (nextChapter > 5)
                {
                    yield return GoToRoutine(FlowState.Ending, TransitionType.None);
                    yield break;
                }

                // Opening for next chapter (CH1 has no opening).
                _cutsceneKind = CutsceneKind.ChapterOpening;
                _cutsceneChapter = nextChapter;
                yield return GoToRoutine(FlowState.Cutscene, TransitionType.Fade);
                yield break;
            }

            if (_cutsceneKind == CutsceneKind.ChapterOpening)
            {
                _pendingChapter = _cutsceneChapter;
                _pendingStage = FirstStageOfChapter(_cutsceneChapter);
                yield return GoToRoutine(FlowState.Run, TransitionType.Fade);
                StageManager.Instance?.LoadStage(_pendingStage);
            }
        }

        public void NotifyEndingFinished()
        {
            // Legacy — full sequence now lives in EndingController.
            StartCoroutine(EndingTail());
        }

        /// Called when EndingController finishes stinger (tap → title).
        public void CompleteEndingReturnToTitle()
        {
            CampaignFlagAndTitle();
        }

        private void CampaignFlagAndTitle()
        {
            if (_director != null)
            {
                _director.CampaignCleared = true;
                _director.Progression?.MarkCampaignCleared();
            }
            else
            {
                PlayerPrefs.SetInt(ProgressionManager.ClearedKey, 1);
                PlayerPrefs.Save();
            }

            StartCoroutine(GoToRoutine(FlowState.Title, TransitionType.None));
        }

        private IEnumerator EndingTail()
        {
            // Fallback if EndingController didn't own credits/stinger.
            yield return GoToRoutine(FlowState.Credits, TransitionType.None);
            yield return new WaitForSecondsRealtime(1f);
            yield return GoToRoutine(FlowState.Sting, TransitionType.None);
            float t = 0f;
            while (t < 2f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            CampaignFlagAndTitle();
        }

        // ── Scene helpers ──────────────────────────────────────────────────

        private IEnumerator PlayTransitionOut(TransitionType t, FlowState next)
        {
            var ui = _director != null ? _director.UI : null;
            if (ui == null)
                yield break;

            switch (t)
            {
                case TransitionType.None:
                    yield break;
                case TransitionType.Fade:
                {
                    float dur = 0.8f;
                    if (_state == FlowState.StageClear && next == FlowState.Run)
                        dur = 0.4f;
                    else if (next == FlowState.Ending)
                        dur = 0f;
                    if (dur <= 0f)
                        yield break;
                    yield return ui.Fade(0f, 1f, dur, Color.black);
                    break;
                }
                case TransitionType.WhiteFlash:
                    yield return ui.WhiteFlash(0.15f, 0.6f);
                    ui.Snap(1f, Color.black);
                    break;
                case TransitionType.SlowMotion:
                    float elapsed = 0f;
                    float start = Time.timeScale > 0.01f ? Time.timeScale : 1f;
                    while (elapsed < 0.3f)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        Time.timeScale = Mathf.Lerp(start, 0.35f, elapsed / 0.3f);
                        yield return null;
                    }

                    Time.timeScale = 0.35f;
                    break;
            }
        }

        private IEnumerator PlayTransitionIn(TransitionType t, FlowState prev, FlowState next)
        {
            var ui = _director != null ? _director.UI : null;
            if (ui == null)
                yield break;

            // Tune durations per design table.
            float fadeIn = 0.4f;
            if (prev == FlowState.Title && next == FlowState.Cutscene)
                fadeIn = 0.8f;
            else if (next == FlowState.Cutscene)
                fadeIn = 1.0f;
            else if (prev == FlowState.StageClear && next == FlowState.Run)
                fadeIn = 0.4f;
            else if (next == FlowState.Credits)
                fadeIn = 2.0f;
            else if (t == TransitionType.None)
            {
                ui.Snap(0f);
                yield break;
            }

            if (t == TransitionType.SlowMotion)
            {
                Time.timeScale = 1f;
                yield break;
            }

            if (t == TransitionType.WhiteFlash)
            {
                // Already faded during out; lift black.
                yield return ui.Fade(1f, 0f, 0.4f, Color.black);
                yield break;
            }

            if (t == TransitionType.Fade)
                yield return ui.Fade(1f, 0f, fadeIn, Color.black);
        }

        private void BeginPreloadRun()
        {
            if (_runPreload != null || _runSceneReady)
                return;

            string run = ResolveRunScene();
            if (!Application.CanStreamedLevelBeLoaded(run))
                return;

            _runPreload = SceneManager.LoadSceneAsync(run, LoadSceneMode.Additive);
            if (_runPreload != null)
                _runPreload.allowSceneActivation = false;
        }

        private IEnumerator ActivateOrLoadRun()
        {
            string run = ResolveRunScene();
            // Prologue handoff path must never show a loader.
            bool showLoader = !_awaitingPrologueHandoff;
            if (showLoader)
                _director?.UI?.SetLoader(true);

            if (_runPreload != null)
            {
                _runPreload.allowSceneActivation = true;
                while (!_runPreload.isDone)
                    yield return null;
                _runPreload = null;
                _runSceneReady = true;

                yield return UnloadIfLoaded(ResolveTitleScene());
            }
            else if (!_runSceneReady || !IsSceneLoaded(run))
            {
                if (_awaitingPrologueHandoff)
                {
                    // Additive keep-alive under cutscene (do not Single-load).
                    var op = SceneManager.LoadSceneAsync(run, LoadSceneMode.Additive);
                    while (op != null && !op.isDone)
                        yield return null;
                    yield return UnloadIfLoaded(ResolveTitleScene());
                    _runSceneReady = true;
                }
                else
                {
                    yield return LoadSingle(run);
                    _runSceneReady = true;
                }
            }

            if (showLoader)
                _director?.UI?.SetLoader(false);

            if (!_awaitingPrologueHandoff)
                UnloadCutsceneIfAny();
        }

        private IEnumerator ActivateRunSuspendedForHandoff()
        {
            yield return ActivateOrLoadRun();
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            session?.SuspendForPrologueHandoff();
            // Extra frame so Awake/Start settle under suspend flag.
            yield return null;
            session?.SuspendForPrologueHandoff();
        }

        private IEnumerator LoadCutsceneAdditive()
        {
            yield return LoadAndPlayCutscene();
        }

        private IEnumerator LoadAndPlayCutscene()
        {
            if (!Application.CanStreamedLevelBeLoaded(CutsceneScene))
            {
                var ctrl = CutsceneController.Ensure();
                ctrl.PlayKind(_cutsceneKind, _cutsceneChapter, OnCutsceneControllerFinished);
                yield break;
            }

            if (!IsSceneLoaded(CutsceneScene))
            {
                var op = SceneManager.LoadSceneAsync(CutsceneScene, LoadSceneMode.Additive);
                while (op != null && !op.isDone)
                    yield return null;
            }

            var cut = UnityEngine.Object.FindAnyObjectByType<CutsceneController>();
            if (cut == null)
            {
                var go = new GameObject("CutsceneController");
                cut = go.AddComponent<CutsceneController>();
                var cutScene = SceneManager.GetSceneByName(CutsceneScene);
                if (cutScene.IsValid())
                    SceneManager.MoveGameObjectToScene(go, cutScene);
            }

            cut.PlayKind(_cutsceneKind, _cutsceneChapter, OnCutsceneControllerFinished);
        }

        private void UnloadCutsceneIfAny()
        {
            var stub = UnityEngine.Object.FindAnyObjectByType<CutsceneController>();
            // Destroy DDOL/stub host when not in cutscene scene.
            if (stub != null && stub.gameObject.scene.name != CutsceneScene)
            {
                // Keep scene-hosted; stub without scene unload is fine to destroy after prologue.
                if (!IsSceneLoaded(CutsceneScene))
                    UnityEngine.Object.Destroy(stub.gameObject);
            }

            if (IsSceneLoaded(CutsceneScene))
                SceneManager.UnloadSceneAsync(CutsceneScene);
        }

        private IEnumerator LoadSingle(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("[SceneFlow] Scene missing from build: " + sceneName);
                yield break;
            }

            _runSceneReady = sceneName == ResolveRunScene();
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone)
                yield return null;
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            if (!IsSceneLoaded(sceneName))
                yield break;
            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                yield return null;
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }

            return false;
        }

        public static string ResolveRunScene() => RunScene;

        public static string ResolveTitleScene() => TitleScene;

        private static int FirstStageOfChapter(int chapter)
        {
            switch (Mathf.Clamp(chapter, 1, 5))
            {
                case 1: return 1;
                case 2: return 5;
                case 3: return 9;
                case 4: return 13;
                default: return 17;
            }
        }
    }
}
