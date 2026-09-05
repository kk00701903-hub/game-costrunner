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

        /// 타임라인 재도전 중이면 본 진행 세이브가 여기 보관된다.
        private SaveData _mainSave;
        public bool IsRetry => _mainSave != null;
        public static bool Active => I != null && I.Save != null;

        /// 엔딩 씬이 읽는 분기. Resolve 시점에 채워진다.
        public EndingKind PendingEnding { get; private set; } = EndingKind.None;
        public bool OpenTimelineOnRaising { get; set; }
        /// 마지막 챕터 정산 결과(StageClearUI 표시용).
        public ChapterGrade LastGrade { get; private set; } = ChapterGrade.None;
        public bool LastImproved { get; private set; }
        public int LastRunHearts { get; private set; }

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

            var result = ScheduleJudge.Resolve(def, Save.stats, Timeline.SeasonOf(Save.week), SaveSys.NextDouble());
            Save.stats = result.after;
            Save.chapterHearts += result.heartsGained;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return result;
        }

        /// 3페이즈가 끝났을 때. 반환: 강제 스토리 돌입이 필요한가.
        public bool AdvanceWeek()
        {
            if (Save == null) return false;
            ScheduleJudge.WeeklyDecay(Save.stats);
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
            WriteMain();
            bool prologue = Save.chapter == 1 && !Save.prologueSeen && !IsRetry;
            Save.prologueSeen = true;
            Flow?.StartStoryRun(Save.chapter, prologue);
        }

        /// StageManager 클리어 → SceneFlow가 호출. 챕터 정산까지 여기서 끝낸다.
        public void OnRunCleared(StageRunStats stats)
        {
            if (Save == null) return;
            LastRunHearts = stats != null ? stats.Hearts : 0;
            Save.chapterHearts += LastRunHearts;
            Save.stats.money += stats != null ? stats.CoinValue + stats.NearMissValue : 0;
            Save.stats.Clamp();
            LastGrade = ChapterGrading.Settle(Save, out bool improved);
            LastImproved = improved;
            SetPhase(GamePhase.ChapterResult);
            WriteMain();
            OnSaveChanged?.Invoke(Save);
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

            var p = Profile;
            p.endingsSeen++;
            if (kind == EndingKind.Happy) p.happyEndings++;
            p.skateboardUnlocked = true;          // 엔딩 종류와 무관하게 해금
            p.bestPlaythrough = Mathf.Max(p.bestPlaythrough, Save.playthrough);
            SaveSys.WriteProfile(p);
            WriteMain();

            SetPhase(GamePhase.Ending);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Ending, TransitionType.Fade);
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
