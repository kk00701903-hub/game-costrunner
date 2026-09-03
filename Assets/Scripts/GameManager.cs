using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunState
{
    Prologue,
    Playing,
    GameOver,
    Ending
}

public enum EndingKind
{
    None,

    /// Reached the depot. The list gets what it came for.
    Retrieved,

    /// Reached the depot with all five letters unopened and unburnt.
    OffTheList,

    /// The 347th attempt.
    Hidden347
}

public class GameManager : MonoBehaviour
{
    private const string BestScoreKey = "r347_best_score";
    private const string RunCountKey = "r347_run_count";
    private const string DeckPieceKey = "r347_deck_pieces";

    public static GameManager Instance { get; private set; }

    /// The 347th attempt is not a failure state. It is the title.
    public const int HiddenRunCount = 347;

    [SerializeField] private Transform player;
    [Tooltip("Path distance to the western depot gate.")]
    [SerializeField] private float depotDistance = 5000f;

    [Tooltip("0 means a fresh seed every run. Any other value reproduces the course.")]
    [SerializeField] private int courseSeed;

    [Header("Scoring")]
    [SerializeField] private int coinScore = 10;
    [Tooltip("Points per second inside the collapse line's danger band.")]
    [SerializeField] private int riskScorePerSecond = 30;
    [SerializeField] private int grindScorePerSecond = 25;
    [SerializeField] private float riskBandMetres = 20f;

    public RunState State { get; private set; } = RunState.Prologue;
    public bool IsPlaying => State == RunState.Playing;
    public bool IsGameOver => State == RunState.GameOver;
    public bool IsEndless { get; private set; }
    public int Supplies { get; private set; }
    public int Hits { get; private set; }

    /// Bungeo's letters in hand. Spending one on a revive costs the true ending.
    public int Letters { get; private set; }
    public int LettersBurned { get; private set; }
    public float DepotDistance => depotDistance;
    public float RemainingDistance { get; private set; }
    public float TraveledDistance { get; private set; }
    public DeathCause Cause { get; private set; } = DeathCause.None;

    public Zone CurrentZone => Zones.At(TraveledDistance);
    public EndingKind Ending { get; private set; } = EndingKind.None;
    public Zone DeathZone { get; private set; } = Zone.Arcade;
    public float DeathDistance { get; private set; }

    public int Score => Mathf.FloorToInt(TraveledDistance) + Bonus;
    public int BestScore { get; private set; }

    /// Every tag ever picked up this run. The drones track on this number, so
    /// the score and the danger are literally the same quantity.
    public int Tags { get; private set; }

    /// Permanent currency, kept between runs.
    public int DeckPieces { get; private set; }

    public float Combo => Mathf.Clamp(1f + 0.1f * _comboTags, 1f, 5f);

    /// Same seed, same course. Needed for replays and for a shared daily run.
    public int Seed { get; private set; }

    public PlayerVitals PlayerVitals => _playerController != null ? _playerController.Vitals : null;

    /// Attempts logged by the control tower. The number is the play count, and
    /// the play count is the story.
    public int RunCount { get; private set; }

    /// Everything scored outside plain distance and pickups.
    public int Bonus { get; private set; }

    public event Action OnRunStarted;
    public event Action OnGameOver;
    public event Action OnEnding;
    public event Action<int> OnSuppliesChanged;
    public event Action OnHit;
    public event Action<int> OnRunCountChanged;
    public event Action<PickupKind> OnPickup;

    private readonly System.Collections.Generic.List<float> _hitTimes = new System.Collections.Generic.List<float>();
    private PlayerController _playerController;
    private float _comboTags;
    private float _scoreCarry;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RemainingDistance = depotDistance;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        RunCount = PlayerPrefs.GetInt(RunCountKey, 0);
        DeckPieces = PlayerPrefs.GetInt(DeckPieceKey, 0);
        Seed = courseSeed != 0 ? courseSeed : Environment.TickCount;
        FlagStore.ClearSession();
    }

    public void SetSeed(int seed)
    {
        Seed = seed;
    }

    /// Everything but raw distance runs through the combo multiplier.
    public void AddBonus(int points)
    {
        if (points == 0)
            return;

        Bonus = Mathf.Max(0, Bonus + Mathf.RoundToInt(points * Combo));
    }

    public void AddFlatBonus(int points)
    {
        Bonus = Mathf.Max(0, Bonus + points);
    }

    /// Called on every death and on every boss defeat, including the ones that
    /// restart at the arena mouth rather than ending the run.
    public void CountRun()
    {
        RunCount++;
        PlayerPrefs.SetInt(RunCountKey, RunCount);
        PlayerPrefs.Save();
        OnRunCountChanged?.Invoke(RunCount);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null)
            _playerController = player.GetComponent<PlayerController>();

        EnsureDepotGate();

        if (UIManager.Instance == null)
            StartCoroutine(FallbackPrologue());
    }

    private void EnsureDepotGate()
    {
        if (FindObjectOfType<DepotGate>() == null)
            new GameObject("DepotGate").AddComponent<DepotGate>();
    }

    private System.Collections.IEnumerator FallbackPrologue()
    {
        yield return new WaitForSeconds(3f);
        BeginRun();
    }

    private void Update()
    {
        if (_playerController == null)
        {
            if (player == null)
                return;

            _playerController = player.GetComponent<PlayerController>();
            if (_playerController == null)
                return;
        }

        TraveledDistance = Mathf.Max(0f, _playerController.PathDistance);
        RemainingDistance = IsEndless ? 0f : Mathf.Max(0f, depotDistance - TraveledDistance);

        if (State == RunState.Playing)
            AccrueRiskScore();

        if (State == RunState.Playing && !IsEndless && RemainingDistance <= 0f)
            ReachDepot();
    }

    /// The safe lane pays least. Riding inside the collapse line's band and
    /// holding a grind are the two ways to be paid for the risk.
    private void AccrueRiskScore()
    {
        float perSecond = 0f;

        CollapseLine collapse = CollapseLine.Instance;
        if (collapse != null && collapse.Gap <= riskBandMetres)
            perSecond += riskScorePerSecond;

        if (_playerController != null && _playerController.IsGrinding)
            perSecond += grindScorePerSecond;

        if (perSecond <= 0f)
            return;

        _scoreCarry += perSecond * Combo * Time.deltaTime;
        int whole = Mathf.FloorToInt(_scoreCarry);
        if (whole <= 0)
            return;

        _scoreCarry -= whole;
        AddFlatBonus(whole);
    }

    public void BeginRun()
    {
        if (State != RunState.Prologue)
            return;

        State = RunState.Playing;
        if (AdRewardService.Instance != null)
            AdRewardService.Instance.BeginRun();
        OnRunStarted?.Invoke();
    }

    public void Collect(PickupKind kind)
    {
        if (!IsPlaying)
            return;

        switch (kind)
        {
            case PickupKind.Coin:
                Supplies++;
                AddBonus(coinScore);
                if (Wallet.Instance != null)
                    Wallet.Instance.AddCoins(coinScore, true);
                break;
            case PickupKind.Tag:
                Supplies++;
                Tags++;
                _comboTags += 1f;
                if (Codex.Instance != null)
                    Codex.Instance.RegisterRandomTag(Seed, Tags);
                break;
            case PickupKind.DeckPiece:
                DeckPieces++;
                PlayerPrefs.SetInt(DeckPieceKey, DeckPieces);
                PlayerPrefs.Save();
                if (Wallet.Instance != null)
                    Wallet.Instance.AddDeckShards(1);
                break;
            case PickupKind.Letter:
                AddLetter();
                break;
            case PickupKind.DeckTape:
                Heal();
                break;
            default:
                if (ItemSlot.Instance != null)
                    ItemSlot.Instance.Store(kind);
                break;
        }

        OnSuppliesChanged?.Invoke(Supplies);
        OnPickup?.Invoke(kind);
    }

    private void Heal()
    {
        PlayerVitals vitals = _playerController != null ? _playerController.Vitals : null;
        if (vitals != null)
            vitals.Heal(1);
    }

    public void ReportHit()
    {
        Hits++;
        _hitTimes.Add(Time.time);
        _comboTags *= 0.5f;
        OnHit?.Invoke();
    }

    /// Hits inside the spawn director's rolling window.
    public int RecentHits(float seconds)
    {
        float cutoff = Time.time - seconds;
        int count = 0;
        for (int i = _hitTimes.Count - 1; i >= 0; i--)
        {
            if (_hitTimes[i] < cutoff)
                break;

            count++;
        }

        return count;
    }

    public void AddLetter()
    {
        Letters = Mathf.Min(5, Letters + 1);
    }

    public bool TryBurnLetter()
    {
        if (Letters <= 0)
            return false;

        Letters--;
        LettersBurned++;
        return true;
    }

    public void GameOver()
    {
        GameOver(DeathCause.Crash);
    }

    public void GameOver(DeathCause cause)
    {
        if (State != RunState.Playing)
            return;

        Cause = cause;
        DeathZone = CurrentZone;
        DeathDistance = TraveledDistance;
        _comboTags = 0f;
        SaveBestScore();
        CountRun();
        EconomyBootstrap.GrantFirstDeath();
        EconomyBootstrap.NoteRunFinished(TraveledDistance, 0);
        OnboardingMetrics.FirstDeath();

        // The attempt that ends the story is an attempt like any other, so it
        // arrives through the death path rather than a special case somewhere.
        if (RunCount >= HiddenRunCount)
        {
            Ending = EndingKind.Hidden347;
            if (Codex.Instance != null)
                Codex.Instance.UnlockPlayerTag();
            State = RunState.Ending;
            OnEnding?.Invoke();
            return;
        }

        State = RunState.GameOver;
        OnGameOver?.Invoke();
    }

    public void ReachDepot()
    {
        if (State != RunState.Playing)
            return;

        RemainingDistance = 0f;
        State = RunState.Ending;

        // Five letters is five people who wrote to Doha and were not answered
        // by the list. Burning one to survive spends that.
        Ending = Letters >= 5 ? EndingKind.OffTheList : EndingKind.Retrieved;
        if (Ending == EndingKind.OffTheList && Codex.Instance != null)
            Codex.Instance.UnlockPlayerTag();
        FlagStore.SetBool(FlagStore.ZoneCleared(Zones.Count), true);
        if (SaveSystem.Instance != null)
        {
            int z = Zones.Index(CurrentZone) - 1;
            if (z >= 0 && z < SaveSystem.Instance.Data.zoneCleared.Length)
                SaveSystem.Instance.Data.zoneCleared[z] = true;
            SaveSystem.Instance.MarkDirty();
        }

        EconomyBootstrap.GrantKingDaily(Zones.Index(CurrentZone));
        EconomyBootstrap.NoteRunFinished(TraveledDistance, 0);
        SaveBestScore();
        OnEnding?.Invoke();
    }

    /// Drops the goal and lets the run continue forever, scoring only.
    public void ContinueRun()
    {
        if (State != RunState.Ending)
            return;

        IsEndless = true;
        RemainingDistance = 0f;
        State = RunState.Playing;
        OnRunStarted?.Invoke();
    }

    /// The death screen is a filing entry, not a verdict. Nobody in the city is
    /// angry at Doha, which is the part that is supposed to sit badly.
    public string RecoveryLog()
    {
        return "「A-0347 회수. " + Zones.Label(DeathZone) +
               ", 거리 " + Mathf.FloorToInt(DeathDistance).ToString("N0") + " m. 기록 보관.」";
    }

    /// Full filing form for the first death (and later run-count unlocks).
    public string RecoveryLogFull()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
        sb.Append("회수 완료 — A-0347\n");
        sb.Append(Zones.Label(DeathZone));
        sb.Append(" / 주행 ");
        sb.Append(Mathf.FloorToInt(DeathDistance).ToString("N0"));
        sb.Append("m / 회차 ");
        sb.Append(RunCount);
        sb.Append("\n\n");
        sb.Append("구역 상태 : 지반 침하 진행 중 (서 → 동)\n");
        sb.Append("회수 근거 : 재난대응 임시령 4조 — 구역 내 자산 회수\n");
        sb.Append("비고      : ");
        sb.Append(RecoveryRemark());
        return sb.ToString();
    }

    public string RecoveryRemark()
    {
        if (RunCount >= 347)
            return "구조 시도 기록: 347건. 생존 확인: 0건.";
        if (RunCount >= 100)
            return "집하장 좌표 기입. 대상은 계속 반대 방위로 이동 중.";
        if (RunCount >= 50)
            return "관제 측이 대상 호칭을 「도하」로 기록. 출처 불명.";
        if (RunCount >= 30)
            return "동일 좌표 질의 반복 감지. 청소부 채널 간섭.";
        if (RunCount >= 15)
            return "회수반 최종 명령 원문 첨부 — 「모든 자산은 목록으로 돌아온다」.";
        if (RunCount >= 8)
            return "청소부 잔류 사유 미기재. 관제탑 전원 유지.";
        if (RunCount >= 3)
            return "열지층 3호 계통. 보고서 2회 반려.";
        return "대상이 자산 분류에 이의를 제기함. 무시함.";
    }

    public bool IsFirstDeathScreen => RunCount == 1;

    /// Kept short and factual. It says what happened, never what it meant.
    public string CauseCopy()
    {
        switch (Cause)
        {
            case DeathCause.Fell:
                return "도로가 끝난 자리로 내려갔다";
            case DeathCause.WrongTurn:
                return "반대쪽으로 꺾어 벽을 받았다";
            case DeathCause.Collapsed:
                return "붕괴선이 먼저 도착했다";
            case DeathCause.Retrieved:
                return "회수반이 자산을 확보했다";
            case DeathCause.TimedOut:
                return "붕괴선을 앞지르지 못했다";
            default:
                return "데크가 부러졌다";
        }
    }

    public string EndingCopy()
    {
        switch (Ending)
        {
            case EndingKind.OffTheList:
                return "붕어의 편지 다섯 통이 주머니에 그대로 있다.\n" +
                       "집하장 명단에 A-0347은 없다. 처음부터 없었던 것처럼.\n\n" +
                       "「목록 밖」";
            case EndingKind.Hidden347:
                return "회수 시도 347회. 회수 불가.\n" +
                       "관제 로그가 A-0347 항목을 지운다. 지운 자리에 아무것도 채우지 않는다.\n\n" +
                       "「347」";
            default:
                return "집하장 셔터가 열리고, 목록이 도하의 번호를 부른다.\n" +
                       "데크 뒷면에는 아직 「서이한 것. 손대지 마」가 남아 있다.\n\n" +
                       "「회수 완료」";
        }
    }

    private void SaveBestScore()
    {
        if (Score <= BestScore)
            return;

        BestScore = Score;
        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.Save();
    }

    public void Configure(Transform playerTransform, float goalDistance)
    {
        player = playerTransform;
        _playerController = playerTransform != null ? playerTransform.GetComponent<PlayerController>() : null;
        depotDistance = Mathf.Max(50f, goalDistance);
        RemainingDistance = depotDistance;
    }

    public void Restart()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path))
        {
            SceneManager.LoadScene(scene.path);
            return;
        }

        GameObject hook = new GameObject("SceneReboot");
        hook.AddComponent<EmptySceneReboot>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public class EmptySceneReboot : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i] != gameObject)
                Destroy(roots[i]);
        }

        yield return null;

        GameObject boot = new GameObject("GameBootstrap");
        boot.AddComponent<GameBootstrap>();
        Destroy(gameObject);
    }
}
