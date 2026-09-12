using System;
using System.Collections;
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

            // 챕터별 낮/밤 고정표(ChapterClock): 1·20=낮, 2~19=낮≈70%/밤≈30%
            def.lightingTStart = ChapterClock.StartT(def.stageIndex);
            def.lightingTEnd = Mathf.Max(def.lightingTEnd, Mathf.Min(1f, def.lightingTStart + 0.25f));
            // 52차(사용자): 스토리 러닝은 이벤트(52주에 8번) — 한 판 3분 이내로 거리를 씌운다(표의 1650~3300 m → ≤1500 m).
            if (!ArcadeRun.Active && GameManager.Active && def.targetDistance > StoryProgress.MaxRunMeters) def.targetDistance = StoryProgress.MaxRunMeters;
            _current = def;
            StageIndex = def.stageIndex;
            ChapterIndex = def.chapterIndex;
            _stageOriginDistance = player != null ? player.PathDistance : 0f;
            // 22차-5: 골인 연출 되돌리기 + 스테이지 끝에 리본
            ResetFinishPresentation();
            if (!ArcadeRun.Active && def.targetDistance > 1f) _ribbon = FinishRibbon.Spawn(_stageOriginDistance + def.targetDistance);
            _stageElapsed = 0f;
            _stageActive = true;

            // Snap lighting to this stage's start — never earlier than that start for this load.
            environment?.ResetLightingTo(def.lightingTStart);
            BeginSunsetClock();
            MonochromeWorld.Arm(ChapterIndex);   // 19차-1: 20장은 10초 뒤 세상이 흑백
            if (!ArcadeRun.Active || ArcadeRun.KpopMode) FeverMode.Ensure().ArmForStage();   // 꼬마 도움 버튼 → 피버 (48차: K-POP 도 — 후렴에서 제안)
            _kpopFinishZ = float.PositiveInfinity; _kpopOutro = false; _kpopChorusOffered = false; _kpopChorus2Offered = false; _kpopFinishing = false;
            GiantMode.Ensure().EndNow();

            if (player != null && !player.enabled)
                player.enabled = true;
            if (player != null && player.State == SkateState.Finish) player.ResetSoftState();   // 22차-5: 골인 상태로 다음 스테이지에 들어오지 않게

            // 24차-6: 지난 스테이지의 타일·픽업 머티리얼(참조 끊긴 것)을 여기서 비운다. 비동기라 프레임을 막지 않는다.
            Resources.UnloadUnusedAssets();
            OnStageStart?.Invoke(def);
            AnnounceStart();
        }

        /// 러닝 시작: 잠깐 멈춘 뒤 화면 중앙에 「챕터 N 시작!」이 크게 뜨고 달린다.
        private void AnnounceStart()
        {
            StopCoroutineSafe(_announceCo);
            _announceCo = StartCoroutine(AnnounceStartCo());
        }

        private Coroutine _announceCo;
        private void StopCoroutineSafe(Coroutine c) { if (c != null) StopCoroutine(c); }

        private IEnumerator AnnounceStartCo()
        {
            // 씬·HUD가 한 프레임 잡힌 뒤
            yield return null;
            // 41차: 베일이 걷힐 때까지 3.5초씩 기다리지 않는다(사용자: "챕터 소개는 시작하자마자").
            // 43차: 단, 베일에 그려지는 로딩 그림(UI_Loading_Mock)은 **낮 풍경**이라 그 위에 카드를 띄우면
            //       밤 챕터도 "낮이었다가 카드 뒤에 밤"으로 보였다(사용자 보고). 베일이 반쯤 걷힐 때까지만(최대 0.6초) 기다린다.
            {
                float waited = 0f;
                while (waited < 0.6f)
                {
                    var uiV = GameDirector.Instance != null ? GameDirector.Instance.UI : null;
                    float veil = uiV != null ? uiV.VeilAlpha : 0f;
                    if (veil < 0.45f) break;
                    if (player != null) player.HoldForStart(0.35f);
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // 63차(사용자): 시작할 때 아이템·장애물을 살짝 소개 — 카드 아래 안내 띠, 그동안 조금 더 기다린다(1.55 → 2.6 s)
            const float hold = 2.6f;
            if (player != null) player.HoldForStart(hold);
            PickupFloat.BindToScene(gameObject);   // 42차: 타이틀 언로드에 카드가 같이 지워지지 않게 런 씬으로
            // 42차: K-POP(아케이드) 런도 「출발」 대신 챕터 소개 카드(사용자: "몇 챕터인지 안 나온다").
            // 아케이드는 StageIndex 가 곧 선택한 챕터(ArcadeRun.StartKpop). 스토리는 세이브 챕터.
            {
                int ch = _current != null ? _current.stageIndex : ChapterIndex;
                if (!ArcadeRun.Active && GameManager.Active) ch = Mathf.Clamp(GameManager.I.Save.chapter, 1, 20);
                ch = Mathf.Clamp(ch, 1, 20);
                string place = ChapterLocation.Get(ch).Name;
                string story = ChapterScript.Title(ch);
                if (ArcadeRun.KpopMode)
                {
                    // 48차: 한 곡 달리기 — 챕터 카드 대신 「♪ 곡 제목 / 오늘의 미션」
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < 3 && i < ArcadeRun.Conditions.Length; i++) { if (i > 0) sb.Append(" · "); sb.Append(ArcadeRun.Conditions[i].Text); }
                    PickupFloat.ChapterStart(ch, "♪ " + ArcadeRun.KpopTrack.Title, sb.ToString(), hold + 0.25f);
                }
                else
                PickupFloat.ChapterStart(ch, place, story, hold + 0.25f);
                PickupFloat.ItemGuide(hold + 0.1f);
                var ui0 = GameDirector.Instance != null ? GameDirector.Instance.UI : null;
#if UNITY_EDITOR
                // 에디터 브릿지(unity_log)는 경고 이상만 모으므로 타이밍 확인용으로 경고 레벨 사용
                Debug.LogWarning($"[StageManager] 챕터 {ch} 소개 카드 표시 t={Time.realtimeSinceStartup:F2} veil={(ui0 != null ? ui0.VeilAlpha : 0f):F2}");
#endif
            }
            // 49차(사용자): K-POP 런은 곡이 1초 뒤에 시작하는데 그 앞에서 스팅어가 「이전 BGM 찌꺼기」처럼 들렸다 → 스토리만.
            if (!ArcadeRun.KpopMode) CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.55f);
            yield return new WaitForSecondsRealtime(hold);
            _announceCo = null;
        }

        /// 23차: 원격(MCP) 디버그 — 즉시 클리어.
        public void DebugClear() { if (_stageActive && !ArcadeRun.Active) ClearCurrent(); }

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
            ResetFinishPresentation();
            if (!ArcadeRun.Active && _current.targetDistance > 1f) _ribbon = FinishRibbon.Spawn(_stageOriginDistance + _current.targetDistance);

            environment?.ResetLightingTo(_current.lightingTStart);
            BeginSunsetClock();
            if (!ArcadeRun.Active || ArcadeRun.KpopMode) FeverMode.Ensure().ArmForStage();
            _kpopFinishZ = float.PositiveInfinity; _kpopOutro = false; _kpopChorusOffered = false; _kpopChorus2Offered = false; _kpopFinishing = false;
            GiantMode.Ensure().EndNow();
            OnStageStart?.Invoke(_current);
            AnnounceStart();
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
            if (CoastRemoteKeys.Down(KeyCode.Home) && !ArcadeRun.Active) _stageElapsed = Mathf.Max(_stageElapsed, SunsetSeconds - 5f);
            if (CoastRemoteKeys.Down(KeyCode.End) && !ArcadeRun.Active) { ClearCurrent(); return; }
            if (CoastRemoteKeys.Down(KeyCode.PageDown) && !ArcadeRun.Active) { DebugWarpToFinish(); return; }   // 22차-5: 골인 30 m 앞으로(리본 확인)
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
                t = Mathf.Max(_current.lightingTStart, Mathf.Lerp(_current.lightingTStart, SunsetLightT, sun));   // 38차: 밤 챕터는 되돌아가지 않는다
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
                if (ArcadeRun.KpopMode) UpdateKpop();
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

            StartCoroutine(FinishThenClear(cleared, chapterEnd));
        }

        // 22차-5: 골인 연출 — 리본을 끊고 몇 걸음 더 달리다 멈춰 뒤돌아 포즈, 카메라는 앞에서 잡는다. 그 위에 정산이 인게임으로 뜬다.
        private FinishRibbon _ribbon;
        private RunnerCameraRig _finishCam;
        private SkaterRig _finishRig;
        /// 23차-3: 결승선 앞뒤 이 구간엔 장애물 행을 놓지 않는다(리본이 가려지지 않게).
        public float FinishPathZ => ArcadeRun.KpopMode ? _kpopFinishZ : (_current != null && _stageActive ? _stageOriginDistance + _current.targetDistance : float.PositiveInfinity);

        // ── 48차: K-POP 한 곡 달리기 — 시간 골인 ──
        // 곡 창(ArcadeRun.KpopTrack.length)이 끝나는 순간이 골인. 마지막 8초(아웃트로)에 장애물 스폰을 멈추고
        // 「지금 속도 × 남은 초」 앞에 리본을 놓는다. 리본을 지나면 완주 → GameSession.EndKpopRun(완주 결과 카드).
        private float _kpopFinishZ = float.PositiveInfinity;
        private bool _kpopOutro, _kpopChorusOffered, _kpopChorus2Offered, _kpopFinishing;
        private void UpdateKpop()
        {
            float t = Mathf.Max(0f, _stageElapsed - ArcadeRun.KpopMusicDelay);   // 곡은 1초 뒤에 시작(CoastAudioManager)
            ArcadeRun.TickKpop(t);
            var track = ArcadeRun.KpopTrack;
            // 후렴 진입: 꼬마 피버 제안(한 번) + 배너
            if (!_kpopChorusOffered && t >= track.chorusStart)
            {
                _kpopChorusOffered = true;
                FeverMode.Ensure().ForceOffer();
                PickupFloat.Banner(Loc.T("후렴! 코인 ×2", "CHORUS! Coins ×2"), new Color(1f, 0.55f, 0.85f), 1.6f);
            }
            // 52차: 3분 창 — 두 번째 후렴에도 한 번 더 제안
            if (_kpopChorusOffered && !_kpopChorus2Offered && track.chorus2Start >= 0f && t >= track.chorus2Start)
            {
                _kpopChorus2Offered = true;
                FeverMode.Ensure().ForceOffer();
                PickupFloat.Banner(Loc.T("후렴! 코인 ×2", "CHORUS! Coins ×2"), new Color(1f, 0.55f, 0.85f), 1.6f);
            }
            // 아웃트로: 장애물 없음 + 리본
            if (!_kpopOutro && t >= track.length - ArcadeRun.KpopOutroSeconds)
            {
                _kpopOutro = true;
                foreach (var o in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None)) o.SetSuppressed(true);
                float speed = Mathf.Max(6f, player.Speed);
                _kpopFinishZ = player.PathDistance + speed * (ArcadeRun.KpopOutroSeconds - 1.2f);   // 곡이 끝나기 조금 전에 리본을 지나도록(피격 감속 여유)
                _ribbon = FinishRibbon.Spawn(_kpopFinishZ);
            }
            // 48차-4(사용자): 페이드아웃은 리본 통과가 기준 — 리본을 지나는 순간부터 곡이 잦아든다. 리본 도달 전에는 끝내지 않는다(보호용 +6초).
            if (!_kpopFinishing && (player.PathDistance >= _kpopFinishZ || t >= track.length + 6f))
            {
                _kpopFinishing = true;
                StartCoroutine(KpopFinishCo());
            }
        }

        private System.Collections.IEnumerator KpopFinishCo()
        {
            _stageActive = false;
            ArcadeRun.MarkKpopFinished();
            _ribbon?.Break();
            CoastAudioManager.Instance?.SetRunBgmFade(2.2f);   // 리본 통과 = 곡 페이드아웃 시작
            player?.FinishRun();
            float sweepZ = player != null ? player.PathDistance - 1f : 0f;
            foreach (var c in FindObjectsByType<CoinSpawner>(FindObjectsSortMode.None)) c.ClearAhead(sweepZ);
            foreach (var j in FindObjectsByType<JellySpawner>(FindObjectsSortMode.None)) j.ClearAhead(sweepZ);
            FeverMode.Ensure().DismissOffer();
            PickupFloat.Banner(Loc.T("한 곡 완주! ♪", "SONG COMPLETE! ♪"), new Color(1f, 0.85f, 0.3f), 1.4f);
            yield return new WaitForSeconds(1.8f);   // 페이드가 거의 끝난 뒤 결과 카드(카드가 AudioListener 를 멈춘다)
            _kpopFinishing = false;
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (session != null) session.EndKpopRun();
        }

        private System.Collections.IEnumerator FinishThenClear(StageDef cleared, bool chapterEnd)
        {
            _ribbon?.Break();
            player?.FinishRun();
            // 뒤돌기/골인 포즈 없음 — 감속만. 관중 환호(FinishRibbon)는 Break()에서 유지.
            _finishRig = player != null ? player.GetComponentInChildren<SkaterRig>() : null;
            _finishRig?.SetFinishPose(false);
            _finishRig?.SettleFacingForward();
            // 23차-3: 리본을 지나는 순간 앞에 남은 코인·말랑이·장애물을 싹 치운다 — 무대는 관중과 주인공만.
            float sweepZ = player != null ? player.PathDistance - 1f : 0f;
            foreach (var o in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None)) o.SetSuppressed(true);
            foreach (var c in FindObjectsByType<CoinSpawner>(FindObjectsSortMode.None)) c.ClearAhead(sweepZ);
            foreach (var j in FindObjectsByType<JellySpawner>(FindObjectsSortMode.None)) j.ClearAhead(sweepZ);
            // 도착 모션(고정 카메라·돌아서서 포즈) 제거
            const bool FinishMotion = false;
            if (FinishMotion)
            {
                _finishCam = Camera.main != null ? Camera.main.GetComponent<RunnerCameraRig>() : null;
                _finishRig = player != null ? player.GetComponentInChildren<SkaterRig>() : null;
                if (_finishCam != null) _finishCam.StartCoroutine(_finishCam.PlayFinishFrame(0.9f));
                PetCompanion.Instance?.SetHidden(true);
                yield return new WaitForSeconds(0.75f);
                _finishRig?.SetFinishPose(true);
                yield return new WaitForSeconds(0.8f);
            }
            else
                yield return new WaitForSeconds(0.9f);

            var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
            if (flow != null)
            {
                flow.NotifyStageCleared(cleared, chapterEnd);
                yield break;
            }
            yield return LocalClearWithMemory(cleared, chapterEnd);
        }

        private void ResetFinishPresentation()
        {
            _finishRig?.SetFinishPose(false);
            _finishCam?.EndFinishFrame();
            PetCompanion.Instance?.SetHidden(false);   // 24차-3
            foreach (var o in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None)) o.SetSuppressed(false);
            if (_ribbon != null) { Destroy(_ribbon.gameObject); _ribbon = null; }
        }

        private System.Collections.IEnumerator LocalClearWithMemory(StageDef cleared, bool chapterEnd)
        {
            if (cleared.stageIndex >= 20)
                clearUi?.ShowFinal(cleared, ContinueToNext, RetryCurrent);
            else
                clearUi?.Show(cleared, chapterEnd, ContinueToNext, RetryCurrent);

            // 정산이 끝난 다음에 회상 — 클리어 UI를 스틸컷이 덮어버리지 않게
            while (clearUi != null && clearUi.IsSettling)
                yield return null;

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
