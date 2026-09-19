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
        /// 136차: 다음 05_Raising 진입 때 스케줄 화면 대신 바닷가 마을(VillageHub)을 연다(타이틀에서 들어올 때·「마을」 버튼).
        /// 111차: 마을러닝(대회)·놀이 직후 육성으로 돌아올 때 돌발 이벤트를 한 번 건너뛴다.
        public bool SuppressRandomEventOnce { get; set; }
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
        public DecoDef LastDecoDrop { get; private set; }   // 28차: 직전 런에서 얻은 방 장식(없으면 null)

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

        /// 26차: K-POP 러닝모드 — 스토리에 들어가지 않고 육성 스탯만 읽는다(세이브가 없으면 null).
        public SaveData PeekSave() => Save ?? SaveSys.Load();

        /// 타이틀 → 캐릭터 선택 → NewGame(mode). 해금 전이면 Skateboard는 Running으로 강등.
        public void NewGame(RunMode mode)
        {
            if (mode == RunMode.Skateboard && !Profile.skateboardUnlocked)
                mode = RunMode.Running;
            _mainSave = null;
            Save = SaveSys.CreateNew();
            Save.runMode = mode;
            ChapterGrading.InitRecords(Save);
            // 105차(재미요소 P2-3, 우마무스메 인자 계승): 엔딩을 한 번이라도 봤으면 2회차 — 지난 회차 최종 스탯의 10% + 숙련 절반을 물려받는다.
            if (Profile != null && Profile.hasLastFinal && Profile.lastFinalStats != null)
            {
                Save.playthrough = Mathf.Max(2, Profile.endingsSeen + 1);
                var lf = Profile.lastFinalStats;
                Save.stats.stamina += Mathf.RoundToInt(lf.stamina * 0.10f);
                Save.stats.agility += Mathf.RoundToInt(lf.agility * 0.10f);
                Save.stats.charm += Mathf.RoundToInt(lf.charm * 0.10f);
                Save.stats.sense += Mathf.RoundToInt(lf.sense * 0.10f);
                Save.stats.Clamp();
                if (Profile.lastMasteryIds != null && Profile.lastMasteryCounts != null)
                {
                    var ids = new System.Collections.Generic.List<string>(); var cs = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < Profile.lastMasteryIds.Length && i < Profile.lastMasteryCounts.Length; i++)
                        if (Profile.lastMasteryCounts[i] / 2 > 0) { ids.Add(Profile.lastMasteryIds[i]); cs.Add(Profile.lastMasteryCounts[i] / 2); }
                    Save.masteryIds = ids.ToArray(); Save.masteryCounts = cs.ToArray();
                }
                Save.inheritedShown = false;
            }
            if (DevUnlockAll) PetShop.UnlockAllPets(Save);
            AdoptWallet(true);   // 109차: 코인 = 돈
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
            AdoptWallet(false);   // 109차: 코인 = 돈
            EnterRaising();
        }

        /// 136차: 스케줄 화면 → 바닷가 마을로.
        public void ToVillage()
        {
            Persist();
            EnterRaising();
        }

        // ── 육성 ─────────────────────────────────────────────────────────

        public void EnterRaising()
        {
            SyncWallet();   // 109차: 러닝(대회·K-POP)에서 모은 코인을 스토리 돈으로
            ArcadeRun.ClearSession();   // K-POP/아케이드 플래그가 남아 스토리 대회가 오염되지 않게
            if (Save != null)
            {
                bool changed = false;
                if (DevUnlockAll) changed |= PetShop.UnlockAllPets(Save);
                changed |= PetShop.EnsureEquipped(Save);
                if (changed) Persist();
            }
            ScheduleTable.Playthrough = Save != null ? Save.playthrough : 1;
            SetPhase(GamePhase.Raising);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Raising, TransitionType.Fade);
        }

        /// 육성 화면 돌발 이벤트 — 스탯은 적용하지 않음. UI에서 선택 후 CommitRandomEvent.
        /// 111차(사용자): 놀이(격주 미니게임·축제)나 마을러닝(대회)이 우선 — 같은 타이밍에 돌발은 안 띄운다.
        public RandomEventDef PeekRandomEvent()
        {
            if (Save == null) return null;
            if (SuppressRandomEventOnce) { SuppressRandomEventOnce = false; return null; }
            if (BlocksRandomEvent(Save)) return null;
            if (SaveSys.NextDouble() >= RandomEventTable.Chance) return null;
            // 74차: 스트레스가 쌓였을 때만 뜨는 사건(새벽 세 시·아무 버스나)을 위해 현재 스트레스를 넘긴다.
            return RandomEventTable.Pick(Timeline.SeasonOf(Save.week), SaveSys.NextDouble(), Save.stats.stress);
        }

        /// 놀이·마을러닝이 이번 주에 있으면 돌발 이벤트를 막는다.
        public static bool BlocksRandomEvent(SaveData s)
        {
            if (s == null) return true;
            // 이야기·대회 직전(다음 턴에 경계 루틴)
            if (s.boundaryPending) return true;
            // 격주 놀이(주말 미니게임) 주
            if (StoryProgress.WeeklyMinigame(s.week, out _)) return true;
            // 계절 축제 주(놀이로 대체)
            if (Festival.AtWeek(s.week) != null) return true;
            // 마을러닝(대회)이 있는 챕터의 마지막 주 — 대회가 우선
            if (StoryProgress.IsRunChapter(s.chapter) && s.week >= Timeline.WeekEnd(s.chapter)) return true;
            return false;
        }

        public RandomEventResult CommitRandomEvent(RandomEventDef ev, int choice)
        {
            var res = RandomEventTable.ApplyChoice(ev, Save.stats, choice);
            Save.chapterHearts += res.dHearts;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return res;
        }

        /// 구 API: 피크 후 Eval 분기로 즉시 적용(레거시 RaisingUI).
        public RandomEventResult? RollRandomEvent()
        {
            var ev = PeekRandomEvent();
            if (ev == null) return null;
            return CommitRandomEvent(ev, ev.Eval(Save.stats) ? 0 : 1);
        }

        /// 이번 주 페이즈 i 실행. Story면 null을 돌려주고 호출자가 StartStoryRun()으로 넘긴다.
        /// 105차: 자동 모드로 행동 중인가(TamaRaisingUI 가 ResolvePhase 앞에서 켠다) — 효율 85%.
        public bool AutoActing;
        /// 105차: 직전 ResolvePhase 에서 숙련 ★이 올랐으면 새 레벨, 아니면 0.
        public int LastMasteryUp;
        /// 105차(P1-2): 직전 행동으로 하트 문턱을 넘었으면 {npc, level}, 아니면 null.
        public int[] LastDailyScene;
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

            ScheduleJudge.Rhythm = Save.rhythm; ScheduleJudge.SnackOn = Save.snackOn; ScheduleJudge.Condition = Save.condition;
            // 105차: 숙련 ★ × 자동 모드 × 좋은 한 달
            ScheduleJudge.GainMul = RaisingFun.MasteryMul(RaisingFun.MasteryLevel(Save, def.id)) * (AutoActing ? RaisingFun.AutoEfficiency : 1f) * (Save.goodMonthBonus ? 1.2f : 1f);
            var result = ScheduleJudge.Resolve(def, Save.stats, Timeline.SeasonOf(Save.week), SaveSys.NextDouble());
            ScheduleJudge.GainMul = 1f;
            LastMasteryUp = RaisingFun.NoteUse(Save, def.id);
            Save.stats = result.after;
            Save.chapterHearts += result.heartsGained;
            if (def.id == "dev_radio" && result.outcome == Outcome.GreatSuccess) Collection.OnRadioGreat();
            Collection.CheckStatCards(Save.stats);
            var side = Affinity.OnSchedule(Save, def.id, result.outcome);
            // 81차 이후: 옛 미니컷씬(SIDE_* ChapterVN)은 자동으로 안 튼다 — 호감 문턱 보상만 즉시.
            LastDailyScene = null;
            if (side != null)
            {
                int lvl = side.EndsWith("_3") ? 3 : side.EndsWith("_2") ? 2 : 1;
                LastDailyScene = new int[] { Affinity.NpcOf(def.id), lvl };   // 105차(P1-2): TamaRaisingUI 가 일상 장면 카드를 띄운다
                Affinity.Reward(Save, lvl);
                CoastToast.Show(Loc.T($"호감도 {Affinity.ShortName(Affinity.NpcOf(def.id))} · {lvl}단계 보상",
                    $"Affinity {Affinity.ShortName(Affinity.NpcOf(def.id))} · Lv{lvl} reward"));
            }
            PendingSideScene = null;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return result;
        }

        /// 주말에 번아웃 단계에서 나온 문장(육성 화면이 한 번 보여 주고 지운다).
        public string PendingWeekNote;
        /// 레거시: 옛 SIDE 미니컷씬 큐. 더 이상 채우지 않음(호감 보상은 ResolvePhase에서 즉시).
        public string PendingSideScene;

        public bool AdvanceWeek()
        {
            if (Save == null) return false;
            ScheduleJudge.Rhythm = Save.rhythm; ScheduleJudge.SnackOn = Save.snackOn; ScheduleJudge.Condition = Save.condition;
            ScheduleJudge.WeeklyDecay(Save.stats);
            PendingWeekNote = ScheduleJudge.BurnoutStage(Save);
            Save.week = Mathf.Min(Timeline.Weeks + 30, Save.week + 1);   // 26차: 게이트 연장으로 52주를 넘길 수 있다(계절은 겨울에 고정)
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];

            var rec = Save.CurrentChapter;
            bool forced = rec != null && !rec.cleared && Save.week > rec.weekEnd;
            if (forced)
                Save.week = rec.weekEnd;   // 마지막 주에 머문 채로 돌입
            Save.boundaryPending = forced;   // 55차: 다음 턴 시작에 컷씬·대회가 기다린다(앱을 껐다 켜도 이어짐)
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            return forced;
        }

        /// 26차: 체력 게이트 불통과 — 이 챕터 마감을 한 주 늘리고 육성으로 돌아간다.
        public void GateFail()
        {
            if (Save == null) return;
            var rec = Save.CurrentChapter;
            if (rec != null)
            {
                rec.weekEnd = Mathf.Max(rec.weekEnd, Save.week) + 1;
                rec.gateFails++;
                Save.week = rec.weekEnd;          // 연장된 주로 넘어간다(같은 주차를 반복하지 않게)
                Save.phaseIndex = 0;
                Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            }
            Save.boundaryPending = false;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
        }

        public bool GatePassed => StoryGate.Passes(Save);

        public void SetQueued(int slot, string id)
        {
            if (Save == null || slot < 0 || slot >= Timeline.PhasesPerWeek) return;
            Save.queuedSchedule[slot] = id;
        }

        // ── 런닝 ─────────────────────────────────────────────────────────

        public void StartStoryRun()
        {
            if (Save == null) return;
            ArcadeRun.ClearSession();   // 스토리 대회는 아케이드/K-POP 플래그 없이
            TitleAudio.StopMenuGlobal();   // 육성 BGM(M13) 끄고 러닝으로
            SetPhase(GamePhase.Run);
            RunTuning.Configure(Save);
            RunTuning.CoinMul *= LevelSystem.CoinMul(Save);   // 53차: 레벨 코인 보너스
            // v5 컷씬: 회차 첫 돌입이면 프롤로그(VN) → 챕터 오프닝(VN) → 런. 재도전은 컷씬 생략.
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
            // 26차: 챕터 오프닝은 육성 화면의 챕터 경계 이벤트에서 이미 재생됐다.
            // 55차(사용자): 러닝 앞뒤로 컷씬 없음 — 프롤로그도 육성의 턴 사이(TamaRaisingUI.BoundaryRoutine)에서 튼다.
            launch();
        }

        /// 55차(사용자): 대회 미달 — 정산 없이 육성으로. **주차는 그대로**(그 주의 행동 3번을 다시 하고 다시 도전).
        public void ContestFail()
        {
            if (Save == null) return;
            Save.contestFails++;
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            Save.boundaryPending = false;
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            SuppressRandomEventOnce = true;   // 111차: 마을러닝 직후 돌발과 겹치지 않게
            EnterRaising();
        }

        /// 55차: 쓰러진 뒤 「처음부터」 — 같은 이동 모드로 새 회차(프로필·컬렉션은 유지).
        public void RestartAfterDeath()
        {
            var mode = Save != null ? Save.runMode : RunMode.Running;
            NewGame(mode);
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
        /// 131차: 직전 러닝에서 받은 별조각 / 행운의 부적으로 코인이 두 배였는가(정산 화면 표기용)
        public int LastShards { get; private set; }
        public bool LastLuckyDoubled { get; private set; }

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
            // 131차(재미요소): 행운의 부적(별빛 캡슐 SR) — 이번 완주 코인 ×2 · 완주 별조각
            LastLuckyDoubled = false;
            if (Save.luckyRunPending && stats != null) { Save.stats.money += stats.CoinValue; Save.luckyRunPending = false; LastLuckyDoubled = true; }
            LastShards = StarGacha.OnStoryRunEnd(Save, stats, true);
            LevelSystem.Add(LevelSystem.ExpStoryRun);   // 53차: 스토리 러닝 완주 경험치
            Save.stats.Clamp();
            LastGrade = ChapterGrading.Settle(Save, out bool improved);
            LastImproved = improved;
            Collection.OnChapterSettled(Save.chapter, LastGrade, IsRetry);
            // 6차 1단계: 미션 별·기록·누적 통계·업적
            var p = Profile; p.EnsureArrays();
            LastStarsGained = MissionTable.Settle(p, Save.chapter, stats);
            // 28차: 방 장식 드롭(스토리 런 클리어 45%)
            LastDecoDrop = RoomDeco.TryDropFromRun(p, Save.seed * 131 + Save.week * 17 + Save.chapter, 0.45f);
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
                SuppressRandomEventOnce = true;
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
            Save.boundaryPending = false;
            // 26차: 게이트로 이전 챕터가 늘어났으면 시간을 되돌리지 않고 이어서 간다(다음 챕터도 정해진 주 수만큼).
            Save.week = Mathf.Max(Timeline.WeekStart(Save.chapter), Save.week + 1);
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            var rec = Save.CurrentChapter;
            if (rec != null)
            {
                rec.snapshotAtStart = Save.stats.Clone();
                rec.weekStart = Save.week;
                rec.weekEnd = Save.week + Timeline.WeeksIn(Save.chapter) - 1;
            }
            WriteMain();
            OnSaveChanged?.Invoke(Save);
            SuppressRandomEventOnce = true;   // 111차: 마을러닝(대회) 클리어 뒤 돌발과 겹치지 않게
            EnterRaising();
        }

        /// 52차: 러닝 없는 챕터 — 컷씬(리더)을 읽고 나면 그대로 다음 챕터로. 육성 하트로 등급을 매기고, 육성 화면은 그대로(씬 전환 없음).
        public void CompleteChapterNoRun()
        {
            if (Save == null || IsRetry) return;
            if (Save.chapter >= Timeline.Chapters) { ResolveEnding(); return; }
            var grade = ChapterGrading.SettleNoRun(Save);
            Collection.OnChapterSettled(Save.chapter, grade, false);
            Save.chapter++;
            Save.chapterHearts = 0;
            Save.boundaryPending = false;
            Save.week = Mathf.Max(Timeline.WeekStart(Save.chapter), Save.week + 1);
            Save.phaseIndex = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            var rec = Save.CurrentChapter;
            if (rec != null)
            {
                rec.snapshotAtStart = Save.stats.Clone();
                rec.weekStart = Save.week;
                rec.weekEnd = Save.week + Timeline.WeeksIn(Save.chapter) - 1;
            }
            WriteMain();
            OnSaveChanged?.Invoke(Save);
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

        private bool _epilogueShown, _creditsShown;
        public void ResolveEnding()
        {
            if (Save == null) return;
            // 85차(대본 v4): 엔딩은 **단서 여섯 개(clueMask)** 로 갈린다 — END_A 「엇갈린 정류장」 / END_B 「우유 두 병」 / END_TRUE 「맞닿은 주파수 89.2」.
            //   TRUE 만 Happy(타이틀로), A·B 는 Tragic(타임라인으로 돌아가 다음 회차). 옛 조건(전부 S / 양쪽 엔딩을 본 뒤)은 쓰지 않는다.
            string endId = ClueSystem.EndingId(Save.clueMask);
            var kind = endId == "END_TRUE" ? EndingKind.Happy : EndingKind.Tragic;
            Save.reachedEnding = kind;
            Save.pendingEndingId = endId;
            PendingEnding = kind;
            Collection.OnEnding(kind);

            var p = Profile;
            var st = Save.stats;
            // 엔딩 갤러리 비트: bit0 = A(엇갈린 정류장) · bit3 = B(우유 두 병) · bit6 = 진엔딩. (옛 변형 비트 1·2·4·5 는 더 쓰지 않음)
            Save.endingVariant = 0;
            Save.trueEndingPending = endId == "END_TRUE";
            p.endingMask |= endId == "END_A" ? 1 : endId == "END_B" ? 1 << 3 : 1 << 6;
            if (Save.trueEndingPending) p.trueEndingSeen = true;
            p.lastFinalStats = st.Clone(); p.hasLastFinal = true;
            // 105차(재미요소 P2-3): 다음 회차 계승용 — 숙련·본 단서
            p.lastMasteryIds = Save.masteryIds != null ? (string[])Save.masteryIds.Clone() : new string[0];
            p.lastMasteryCounts = Save.masteryCounts != null ? (int[])Save.masteryCounts.Clone() : new int[0];
            p.clueSeenMask |= Save.clueMask;
            p.endingsSeen++;
            if (kind == EndingKind.Happy) p.happyEndings++;
            p.skateboardUnlocked = true;          // 엔딩 종류와 무관하게 해금
            p.bestPlaythrough = Mathf.Max(p.bestPlaythrough, Save.playthrough);
            SaveSys.WriteProfile(p);
            WriteMain();

            SetPhase(GamePhase.Ending);
            var flow = Flow;
            // 85차: 엔딩 시네마틱(CinematicTable END_*) → 기존 엔딩 시퀀스(편지·크레딧). 시네마 정의가 없으면 옛 VN 대본으로.
            System.Action toEnding = () => { if (flow != null) _ = flow.GoTo(FlowState.Ending, TransitionType.Fade); };
            if (CinematicTable.Get(endId) != null) CinematicPlayer.Play(endId, toEnding);
            else if (ChapterScript.Has(endId)) ChapterVN.Play(endId, toEnding);
            else toEnding();
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
            // 109차(사용자): 엔딩 시네마 끝 → **엔딩 크레딧**(지금까지 모은 컬렉션 사진 한 장씩) → 에필로그 카드 → 원래 흐름
            if (Save != null && !_creditsShown)
            {
                _creditsShown = true;
                EndingCreditsUI.Show(Profile, OnEndingFinished);
                return;
            }
            // 105차(재미요소 P2-2): 엔딩 시네마 뒤 「스무 살, 하늘」 에필로그 카드 한 장 → 그 다음 원래 흐름
            if (Save != null && !_epilogueShown)
            {
                _epilogueShown = true;
                EpilogueUI.Show(Save, OnEndingFinished);
                return;
            }
            _epilogueShown = false; _creditsShown = false;
            if (Save != null && PendingEnding == EndingKind.Tragic)
            {
                OpenTimelineOnRaising = true;
                EnterRaising();
                return;
            }
            SetPhase(GamePhase.Title);
            ArcadeRun.ClearSession();
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

        /// 37차: 비밀코드 테스트 — 아직 안 온 챕터로 바로 점프. 본 진행의 주차·챕터를 그 챕터 시작으로 옮긴다(스탯은 지금 것 그대로).
        /// 37차 비밀코드(1111) 또는 52차 기부 선물 ②(모든 게임 열림) — 챕터·미션·시네마·펫 상점 전부 열림.
        public bool DevUnlockAll => Profile != null && (Profile.devUnlockAll || (Profile.donateGiftMask & (int)Donation.Gift.UnlockAll) != 0);
        public void DevJumpTo(int chapter)
        {
            if (Save == null || !DevUnlockAll || chapter < 1 || chapter > Timeline.Chapters) return;
            if (IsRetry) { Save = _mainSave; _mainSave = null; }
            if (Save.chapters == null || Save.chapters.Length != Timeline.Chapters) ChapterGrading.InitRecords(Save);
            Save.chapter = chapter;
            Save.week = Timeline.WeekStart(chapter);
            Save.phaseIndex = 0;
            Save.chapterHearts = 0;
            Save.queuedSchedule = new string[Timeline.PhasesPerWeek];
            Save.prologueSeen = true;
            var rec = Save.CurrentChapter;
            if (rec != null)
            {
                rec.snapshotAtStart = Save.stats.Clone();
                rec.weekStart = Save.week;
                rec.weekEnd = Save.week + Timeline.WeeksIn(chapter) - 1;
            }
            WriteMain();
            OnSaveChanged?.Invoke(Save);
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
            ArcadeRun.ClearSession();
            WriteMain();
            _mainSave = null;
            SetPhase(GamePhase.Title);
            var flow = Flow;
            if (flow != null) _ = flow.GoTo(FlowState.Title, TransitionType.Fade);
        }

        public void Persist()
        {
            SyncWallet();
            WriteMain();
            OnSaveChanged?.Invoke(Save);
        }

        // ── 109차(사용자): 코인·돈 일원화 — 스토리 모드의 돈(stats.money)은 K-POP 러닝 코인 지갑(CoinWallet) 하나다. ──
        //   스토리에서 번/쓴 만큼을 지갑에 흘리고, 러닝에서 모은 코인은 다음 동기화 때 스토리 돈으로 들어온다. 재도전 샌드박스는 건드리지 않는다.
        private int _walletSynced = int.MinValue;
        public const int StartCoins = 300;
        public void SyncWallet()
        {
            if (Save == null || IsRetry) return;
            int wallet = CoinWallet.TotalStatic;
            if (_walletSynced == int.MinValue) { Save.stats.money = wallet; _walletSynced = wallet; return; }
            int delta = Save.stats.money - _walletSynced;
            if (delta != 0) CoinWallet.AddStatic(delta);
            Save.stats.money = CoinWallet.TotalStatic; _walletSynced = Save.stats.money;
        }
        /// 새 회차/이어하기 직후 — 지갑을 그대로 스토리 돈으로. 첫 회차 시작 자금은 지갑이 StartCoins 보다 적을 때만 채워 준다.
        private void AdoptWallet(bool newGame)
        {
            _walletSynced = int.MinValue;
            if (newGame && CoinWallet.TotalStatic < StartCoins) CoinWallet.AddStatic(StartCoins - CoinWallet.TotalStatic);
            SyncWallet();
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
