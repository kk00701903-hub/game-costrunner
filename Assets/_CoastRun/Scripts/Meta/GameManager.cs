using System;
using UnityEngine;

namespace CoastRun
{
    public enum GamePhase { Title, Raising, Executing, Run, Cutscene, ChapterResult, Timeline, Ending }

    /// v2 코어 루프의 중심. 회차 상태(SaveData)를 들고 육성 ↔ 런닝 ↔ 컷씬 ↔ 정산을 잇는다.
    /// GameDirector(DDOL) 자식 컴포넌트로 살며, SceneFlowController가 씬 전환을, 이 클래스가
    /// "지금 무엇을 해야 하는가"를 결정한다. Save == null 이면 레거시 20스테이지 연속 모드.
    [DefaultExecutionOrder(-900)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public SaveData Save { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public SaveManager SaveSys { get; private set; }
        public MetaProfile Profile => SaveSys.Profile;
        public void WriteProfileNow() => SaveSys.WriteProfile(Profile);

        /// 타임라인 재도전 중이면 본 진행 세이브가 여기 보관된다.
        private SaveData _mainSave;
        public bool IsRetry => _mainSave != null;
        public static bool Active => I != null && I.Save != null;

        /// 엔딩 씬이 읽는 분기. Resolve 시점에 채워진다.
        public EndingKind PendingEnding { get; private set; } = EndingKind.None;
        public bool OpenTimelineOnRaising { get; set; }
        /// 10차: 챕터 선택에서 '다시 달리기' — 재도전 육성 화면이 뜨자마자 런으로 넘어간다.
        public bool RetryRunPending { get; set; }
        public bool FlowBusy => Flow != null && Flow.IsBusy;
        public bool LastRunLate { get; private set; }
        public bool LastRunEarly { get; private set; }
        /// 마지막 챕터 정산 결과(StageClearUI 표시용).
        public ChapterGrade LastGrade { get; private set; } = ChapterGrade.None;
        public bool LastImproved { get; private set; }
        public int LastRunHearts { get; private set; }
        /// 6차: 이번 정산에서 새로 딴 미션 별 수 / 챕터 별 합계(0~3) / 기록 갱신 여부
        public int LastStarsGained { get; private set; }
        public int LastStars { get; private set; }
        public bool LastRecord { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<SaveData> OnSaveChanged;

        public static GameManager Ensure()
        {
            if (I != null) return I;
            var dir = GameDirector.EnsureExists();
            var gm = dir.GetComponent<GameManager>() ?? dir.gameObject.AddComponent<GameManager>();
            return gm;
        }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            SaveSys = GetComponent<SaveManager>() ?? gameObject.AddComponent<SaveManager>();
        }

        private void OnDestroy()
        {
            if (I == this) I = null;
        }

        private SceneFlowController Flow => GameDirector.Instance != null ? GameDirector.Instance.Flow : null;

        // ── 진입 ─────────────────────────────────────────────────────────

        public bool HasSave => SaveSys.HasSave;

        /// 타이틀 → 캐릭터 선택 → NewGame(mode). 해금 전이면 Skateboard는 Running으로 강등.
        public void NewGame(RunMode mode)
        {
            if (mode == RunMode.Skateboard && !Profile.skateboardUnlocked)
                mode = RunMode.Running;
            _mainSave = null;
            Save = SaveSys.CreateNew();
            Save.runMode = mode;
            ChapterGrading.InitRecords(Save);
            Profile.playthroughsStarted++;
            SaveSys.WriteProfile(Profile);
            WriteMain();
            PlayerPrefs.SetInt(MainMenuController.SkipPrologueKey, 0);
            EnterRaising();
        }

        public void Continue()
        {
            _mainSave = null;
            Save = SaveSys.Load();
            if (Save == null) { NewGame(RunMode.Running); return; }
            EnterRaising();
        }

        // ── 육성 ─────────────────────────────────────────────────────────

        public void EnterRaising()
        {
            ScheduleTable.Playthrough = Save != null ? Save.playthrough : 1;
            SetPhase(GamePhase.Raising);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Raising, TransitionType.Fade);
        }

        /// 육성 화면 복귀 시 30% 확률 돌발 이벤트. null이면 없음. 결과는 이미 스탯에 적용됨.
        public RandomEventResult? RollRandomEvent()
        {
            if (Save == null) return null;
            if (SaveSys.NextDouble() >= RandomEventTable.Chance) return null;
            var ev = RandomEventTable.Pick(Timeline.SeasonOf(Save.week), SaveSys.NextDouble());
            var res = RandomEventTable.Apply(ev, Save.stats);
            Save.chapterHearts += res.dHearts;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return res;
        }

        /// 이번 주 페이즈 i 실행. Story면 null을 돌려주고 호출자가 StartStoryRun()으로 넘긴다.
        public PhaseResult? ResolvePhase(int i)
        {
            if (Save == null || i < 0 || i >= Timeline.PhasesPerWeek) return null;
            var def = ScheduleTable.Get(Save.queuedSchedule[i]);
            Save.phaseIndex = i + 1;
            if (def == null || def.category == ScheduleCategory.Story)
            {
                WriteMain();
                return null;
            }

            ScheduleJudge.Rhythm = Save.rhythm; ScheduleJudge.SnackOn = Save.snackOn;
            var result = ScheduleJudge.Resolve(def, Save.stats, Timeline.SeasonOf(Save.week), SaveSys.NextDouble());
            Save.stats = result.after;
            Save.chapterHearts += result.heartsGained;
            if (def.id == "dev_radio" && result.outcome == Outcome.GreatSuccess) Collection.OnRadioGreat();
            Collection.CheckStatCards(Save.stats);
            var side = Affinity.OnSchedule(Save, def.id, result.outcome);
            if (side != null && ChapterScript.Has(side)) PendingSideScene = side;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return result;
        }

        /// 3페이즈가 끝났을 때. 반환: 강제 스토리 돌입이 필요한가.
        /// 주말에 번아웃 단계에서 나온 문장(육성 화면이 한 번 보여 주고 지운다).
        public string PendingWeekNote;
        /// 6차: 이번 주말에 재생할 NPC 사이드 씬(SIDE_*). RaisingUI가 재생 후 비운다.
        public string PendingSideScene;

        public bool AdvanceWeek()
        {
            if (Save == null) return false;
            ScheduleJudge.Rhythm = Save.rhythm; ScheduleJudge.SnackOn = Save.snackOn;
            ScheduleJudge.WeeklyDecay(Save.stats);
            PendingWeekNote = ScheduleJudge.BurnoutStage(Save);
            Save.week = Mathf.Min(Timeline.Weeks + 1, Save.week + 1);
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];

            var rec = Save.CurrentChapter;
            bool forced = rec != null && !rec.cleared && Save.week > rec.weekEnd;
            if (forced)
                Save.week = rec.weekEnd;   // 마지막 주에 머문 채로 돌입
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return forced;
        }

        public void SetQueued(int slot, string id)
        {
            if (Save == null || slot < 0 || slot >= Timeline.PhasesPerWeek) return;
            Save.queuedSchedule[slot] = id;
        }

        // ── 런닝 ─────────────────────────────────────────────────────────

        public void StartStoryRun()
        {
            if (Save == null) return;
            SetPhase(GamePhase.Run);
            RunTuning.Configure(Save);
            // v5 컷씬: 회차 첫 돌입이면 프롤로그(VN) → 챕터 오프닝(VN) → 런. 재도전은 컷씬 생략.
            bool prologue = Save.chapter == 1 && !Save.prologueSeen && !IsRetry;
            Save.prologueSeen = true;   // 9차: 저장 전에 찍어야 런 실패 후 돌아와도 프롤로그가 다시 안 나온다
            WriteMain();
            int chapter = Save.chapter;
            if (IsRetry)
            {
                Flow?.StartStoryRun(chapter, false);
                StartCoroutine(RunLaunchWatchdog(chapter));
                return;
            }
            System.Action launch = () =>
            {
                Debug.Log($"[GameManager] launch run CH{chapter} (flow={(Flow != null)}, busy={FlowBusy})");
                Flow?.StartStoryRun(chapter, false);
                StartCoroutine(RunLaunchWatchdog(chapter));
            };
            System.Action opening = () => ChapterVN.PlayChapterOpening(chapter, launch);
            if (prologue) ChapterVN.Play("PRO", opening);
            else opening();
        }

        /// 11차: 실기기에서 오프닝 뒤 런으로 못 넘어가는 보고 → 자가 복구. 8초 안에 02_Run 이 활성 씬이 안 되면
        /// 플로우를 풀고 한 번 더, 그래도 안 되면 씬을 직접 연다. (에디터에선 정상 경로가 3초 안에 끝난다)
        private System.Collections.IEnumerator RunLaunchWatchdog(int chapter)
        {
            string run = SceneFlowController.ResolveRunScene();
            float t = 0f;
            while (t < 8f)
            {
                t += Time.unscaledDeltaTime;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == run || Phase != GamePhase.Run) yield break;
                yield return null;
            }
            Debug.LogWarning("[GameManager] run scene not active after 8s — retrying via flow");
            Flow?.ForceIdle();
            Flow?.StartStoryRun(chapter, false);
            t = 0f;
            while (t < 8f)
            {
                t += Time.unscaledDeltaTime;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == run || Phase != GamePhase.Run) yield break;
                yield return null;
            }
            Debug.LogWarning("[GameManager] still stuck — loading run scene directly");
            Time.timeScale = 1f; AudioListener.pause = false;
            Flow?.ForceIdle();
            UnityEngine.SceneManagement.SceneManager.LoadScene(run, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        /// 챕터 선택 '다시 보기': 오프닝 컷씬만 재생(진행 영향 없음).
        public void ReplayOpening(int chapter, System.Action onDone)
        {
            if (chapter == 1)
                ChapterVN.Play("PRO", () => ChapterVN.PlayChapterOpening(1, onDone));
            else
                ChapterVN.PlayChapterOpening(chapter, onDone);
        }

        /// StageManager 클리어 → SceneFlow가 호출. 챕터 정산까지 여기서 끝낸다.
        public void OnRunCleared(StageRunStats stats)
        {
            if (Save == null) return;
            LastRunHearts = stats != null ? stats.Hearts : 0;
            // 8차 노을 규칙: 해가 진 뒤 도착 = 하트 40% 감소, 여유 있게 도착(노을 30% 이상 남음) = +10%
            LastRunLate = StageManager.LastRunLate;
            LastRunEarly = !LastRunLate && StageManager.LastRunSunsetT < 0.7f;
            if (LastRunLate) LastRunHearts = Mathf.RoundToInt(LastRunHearts * 0.6f);
            else if (LastRunEarly) LastRunHearts = Mathf.RoundToInt(LastRunHearts * 1.1f);
            Save.lateRuns += LastRunLate ? 1 : 0;
            Save.lastRunLate = LastRunLate;
            Save.chapterHearts += LastRunHearts;
            Save.stats.money += stats != null ? stats.CoinValue + stats.NearMissValue : 0;
            Save.stats.Clamp();
            LastGrade = ChapterGrading.Settle(Save, out bool improved);
            LastImproved = improved;
            Collection.OnChapterSettled(Save.chapter, LastGrade, IsRetry);
            // 6차 1단계: 미션 별·기록·누적 통계·업적
            var p = Profile; p.EnsureArrays();
            LastStarsGained = MissionTable.Settle(p, Save.chapter, stats);
            LastStars = MissionTable.Stars(p, Save.chapter);
            LastRecord = false;
            int ci = Save.chapter - 1;
            if (stats != null && ci >= 0 && ci < 20)
            {
                if (stats.Coins > p.bestCoins[ci]) { p.bestCoins[ci] = stats.Coins; LastRecord = true; }
                if (stats.BestCombo > p.bestCombo[ci]) { p.bestCombo[ci] = stats.BestCombo; LastRecord = true; }
                if (stats.NearMissCount > p.bestNearMiss[ci]) { p.bestNearMiss[ci] = stats.NearMissCount; LastRecord = true; }
                p.totalCoins += stats.Coins; p.totalNearMiss += stats.NearMissCount; p.totalHearts += stats.Hearts;
                if (stats.Flawless) p.flawlessRuns++;
                var sm = StageManager.Instance;
                if (sm != null && sm.Current != null) p.totalDistance += Mathf.RoundToInt(sm.Current.targetDistance);
            }
            p.totalRuns++;
            WriteProfileNow();
            AchievementTable.CheckAndToast(this);
            SetPhase(GamePhase.ChapterResult);
            WriteMain();
            OnSaveChanged?.Invoke(Save);
        }

        /// v3 잠수: 노을을 못 가서 이번 챕터를 C급(하트 0)으로 닫고 다음 챕터로.
        public void ForfeitChapter()
        {
            if (Save == null) return;
            var rec = Save.CurrentChapter;
            if (rec != null && !rec.cleared) { rec.cleared = true; rec.heartsEarned = 0; rec.grade = ChapterGrade.C; }
            Save.forfeitPending = false;
            Collection.OnChapterSettled(Save.chapter, ChapterGrade.C, false);
            WriteMain();
            AfterChapterContinue();
        }

        /// 정산 화면 '계속' (+ 막 컷씬) 이후. 다음 챕터 첫 주로 가거나, 20챕터면 엔딩.
        public void AfterChapterContinue()
        {
            if (Save == null) return;

            if (IsRetry)
            {
                // 샌드박스 종료: 기록은 이미 Settle이 본 배열에 덮어썼다. 본 진행으로 복귀.
                Save = _mainSave;
                _mainSave = null;
                WriteMain();
                OpenTimelineOnRaising = true;
                EnterRaising();
                return;
            }

            if (Save.chapter >= Timeline.Chapters)
            {
                ResolveEnding();
                return;
            }

            Save.chapter++;
            Save.chapterHearts = 0;
            Save.week = Timeline.WeekStart(Save.chapter);
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            var rec = Save.CurrentChapter;
            if (rec != null) rec.snapshotAtStart = Save.stats.Clone();
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            EnterRaising();
        }

        /// 런 실패 후 '육성으로'. 페이즈는 이미 소비됐고 챕터 하트는 보존.
        public void ReturnToRaisingAfterFail()
        {
            if (Save == null) return;
            if (Save.phaseIndex >= Timeline.PhasesPerWeek)
                AdvanceWeek();
            EnterRaising();
        }

        // ── 엔딩 / 타임라인 ────────────────────────────────────────────────

        public void ResolveEnding()
        {
            if (Save == null) return;
            var kind = ChapterGrading.AllS(Save) ? EndingKind.Happy : EndingKind.Tragic;
            Save.reachedEnding = kind;
            PendingEnding = kind;
            Collection.OnEnding(kind);

            var p = Profile;
            // 6차: 엔딩 변형(스탯) + 진엔딩(양쪽 엔딩을 이미 본 회차의 만남)
            var st = Save.stats;
            bool sawA = (p.endingMask & 0b111) != 0, sawB = (p.endingMask & 0b111000) != 0;
            if (kind == EndingKind.Happy) Save.endingVariant = st.sense >= 60 ? 1 : st.trust >= 50 ? 2 : 0;
            else Save.endingVariant = st.trust >= 50 ? 1 : st.stamina < 30 ? 2 : 0;
            Save.trueEndingPending = kind == EndingKind.Happy && sawA && sawB && !p.trueEndingSeen;
            p.endingMask |= 1 << ((kind == EndingKind.Happy ? 0 : 3) + Save.endingVariant);
            if (Save.trueEndingPending) { p.endingMask |= 1 << 6; p.trueEndingSeen = true; }
            p.lastFinalStats = st.Clone(); p.hasLastFinal = true;
            p.endingsSeen++;
            if (kind == EndingKind.Happy) p.happyEndings++;
            p.skateboardUnlocked = true;          // 엔딩 종류와 무관하게 해금
            p.bestPlaythrough = Mathf.Max(p.bestPlaythrough, Save.playthrough);
            SaveSys.WriteProfile(p);
            WriteMain();

            SetPhase(GamePhase.Ending);
            var flow = Flow;
            // v5: 엔딩 VN(만난다/못 만난다) 컷씬을 먼저 보여 주고 기존 엔딩 시퀀스(편지·크레딧)로.
            string vn = kind == EndingKind.Happy ? "END_A" : "END_B";
            string epi = EndingEpilogueId(kind, Save.endingVariant);
            bool trueEnd = Save.trueEndingPending;
            System.Action toEnding = () => { if (flow != null) _ = flow.GoTo(FlowState.Ending, TransitionType.Fade); };
            System.Action afterEpi = trueEnd && ChapterScript.Has("END_TRUE") ? () => ChapterVN.Play("END_TRUE", toEnding) : toEnding;
            System.Action afterMain = epi != null && ChapterScript.Has(epi) ? () => ChapterVN.Play(epi, afterEpi) : afterEpi;
            ChapterVN.Play(vn, afterMain);
        }

        public static string EndingEpilogueId(EndingKind kind, int variant)
        {
            if (variant <= 0) return null;
            if (kind == EndingKind.Happy) return variant == 1 ? "END_A_SENSE" : "END_A_TRUST";
            return variant == 1 ? "END_B_TRUST" : "END_B_WEAK";
        }

        /// 엔딩 끝. 비극이면 타임라인으로, 해피면 타이틀로.
        public void OnEndingFinished()
        {
            if (Save != null && PendingEnding == EndingKind.Tragic)
            {
                OpenTimelineOnRaising = true;
                EnterRaising();
                return;
            }
            SetPhase(GamePhase.Title);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
        }

        public bool CanRetry(int chapter)
        {
            if (Save == null || chapter < 1 || chapter > Timeline.Chapters) return false;
            var rec = Save.chapters[chapter - 1];
            return rec != null && rec.cleared && rec.grade != ChapterGrade.S && rec.snapshotAtStart != null;
        }

        /// 타임라인에서 챕터 재도전: 그 챕터 시작 스냅샷으로 샌드박스 진입.
        public void BeginRetry(int chapter)
        {
            if (!CanRetry(chapter)) return;
            var rec = Save.chapters[chapter - 1];
            var sandbox = new SaveData
            {
                week = rec.weekStart, chapter = chapter, phaseIndex = 0,
                stats = rec.snapshotAtStart.Clone(),
                chapters = Save.chapters,            // 같은 배열 → Settle이 바로 덮어씀
                equippedPet = Save.equippedPet, ownedPetMask = Save.ownedPetMask,
                chapterHearts = 0, playthrough = Save.playthrough, runMode = Save.runMode,
                prologueSeen = true, seed = Save.seed ^ (chapter * 7919), reachedEnding = Save.reachedEnding,
            };
            _mainSave = Save;
            Save = sandbox;
            OpenTimelineOnRaising = false;
            EnterRaising();
        }

        public void CancelRetry()
        {
            if (!IsRetry) return;
            Save = _mainSave;
            _mainSave = null;
            OpenTimelineOnRaising = true;
            EnterRaising();
        }

        public void ToTitle()
        {
            WriteMain();
            _mainSave = null;
            SetPhase(GamePhase.Title);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
        }

        public void Persist()
        {
            WriteMain();
            OnSaveChanged?.Invoke(Save);
        }

        /// 재도전 샌드박스는 파일에 쓰지 않는다 — 본 진행(_mainSave)만 저장. 챕터 배열은 공유되므로
        /// 샌드박스에서 갱신된 등급도 함께 저장된다.
        private void WriteMain()
        {
            var target = _mainSave ?? Save;
            if (target != null) SaveSys.Write(target);
        }

        private void SetPhase(GamePhase p)
        {
            Phase = p;
            OnPhaseChanged?.Invoke(p);
        }
    }
}
