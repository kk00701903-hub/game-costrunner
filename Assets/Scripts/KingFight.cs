using System;
using System.Collections.Generic;
using UnityEngine;

public enum KingStage
{
    Idle,
    Aim,
    Throw,
    Counter,
    Recover,
    Stagger,
    Beaten
}

/// The king rides backwards ahead of the player and never changes the runner's
/// rules. One line runs the whole fight: the lane an attack just came from is
/// the lane the counter appears in, so dodging and hitting share one space.
///
/// This class is the KingCycleFSM from the development brief
/// (Aim → Throw → CounterWindow → Recover).
public class KingFight : MonoBehaviour
{
    public static KingFight Instance { get; private set; }

    [Header("Cycle (fallback if no KingPhaseData)")]
    [SerializeField] private float aimSeconds = 0.6f;
    [SerializeField] private float throwSeconds = 0.5f;
    [SerializeField] private float counterSeconds = 1.4f;
    [SerializeField] private float minRecoverSeconds = 0.25f;
    [SerializeField] private float staggerSeconds = 0.8f;

    [Header("Arena")]
    [SerializeField] private int hpPerPhase = 3;
    [SerializeField] private float arenaSpeed = 14f;
    [SerializeField] private float baseDistance = 74f;
    [SerializeField] private float distanceSwing = 14f;
    [SerializeField] private float staggerCloseIn = 30f;
    [SerializeField] private bool reverseRules;
    [Tooltip("Teach the counter rule before free play. Off for rematches.")]
    [SerializeField] private bool runOnboarding = true;

    private bool _prerunner;
    private bool _dismissed;

    private const float TelegraphFloor = 0.45f;
    private const int MaxCounters = 9;

    private readonly List<int> _thrownLanes = new List<int>();
    private readonly List<int> _counterLanes = new List<int>();
    private readonly List<LaneMarker> _markers = new List<LaneMarker>();
    private readonly List<KingProjectile> _shots = new List<KingProjectile>();

    private PlayerController _player;
    private Transform _king;
    private Transform _arm;
    private Renderer[] _kingRenderers;
    private Color[] _kingColors;
    private KingStage _stage = KingStage.Idle;
    private float _timer;
    private float _stageDuration;
    private float _distance;
    private float _cyclePhase;
    private int _hp;
    private int _maxHp;
    private int _phase;
    private int _trapLane = -2;
    private int _droppedPhase = -1;
    private int _counters;
    private int _consumedWindows;
    private bool _tookHit;
    private float _throwSpeedMul = 1f;
    private ThrowKind _volleyKind = ThrowKind.ContainerShard;
    private DeterministicRandom _rng;
    private int _onboardingCycle;
    private bool _forceLaneLock;
    private int _lockedLane;
    private float _whiteFlash;

    public bool Active => _stage != KingStage.Idle && _stage != KingStage.Beaten && !_dismissed;
    public bool IsPrerunner => _prerunner;
    public int Hp => _hp;
    public int MaxHp => _maxHp;
    public int Phase => _phase + 1;
    public KingStage Stage => _stage;
    public int OnboardingCycle => _onboardingCycle;

    public event Action<int> OnPhaseChanged;
    public event Action OnDefeated;

    public static float TelegraphScale
    {
        get { return PlayerPrefs.GetInt("r347_slow_telegraph", 0) != 0 ? 1.5f : 1f; }
        set
        {
            PlayerPrefs.SetInt("r347_slow_telegraph", value > 1f ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private KingPhaseData PhaseData
    {
        get
        {
            GameConfig cfg = GameConfig.Active;
            if (cfg == null || cfg.kingPhases == null || cfg.kingPhases.Length == 0)
                return null;
            int i = Mathf.Clamp(_phase, 0, cfg.kingPhases.Length - 1);
            return cfg.kingPhases[i];
        }
    }

    private void Awake()
    {
        Instance = this;
        _maxHp = hpPerPhase * 3;
        GameConfig cfg = GameConfig.Active;
        if (cfg != null)
        {
            arenaSpeed = cfg.bossFixedSpeed;
            if (cfg.kingPhases != null && cfg.kingPhases.Length > 0)
            {
                int total = 0;
                for (int i = 0; i < cfg.kingPhases.Length; i++)
                    if (cfg.kingPhases[i] != null)
                        total += cfg.kingPhases[i].hp;
                if (total > 0)
                    _maxHp = total;
            }
        }
    }

    private void Start()
    {
        if (_player == null)
            _player = FindObjectOfType<PlayerController>();
    }

    public void Begin()
    {
        BeginInternal(prerunner: false);
    }

    /// Zone-2 scout. Weak, slow, and you cannot die here — it flies off instead.
    public void BeginPrerunner()
    {
        BeginInternal(prerunner: true);
    }

    private void BeginInternal(bool prerunner)
    {
        if (_player == null)
            _player = FindObjectOfType<PlayerController>();
        if (_player == null)
            return;

        int seed = GameManager.Instance != null ? GameManager.Instance.Seed : 347;
        _rng = new DeterministicRandom(seed ^ 0x4B1F);

        _prerunner = prerunner;
        _dismissed = false;
        _hp = prerunner ? 3 : _maxHp;
        _phase = 0;
        _counters = 0;
        _droppedPhase = -1;
        _tookHit = false;
        _throwSpeedMul = 1f;
        bool onboard = runOnboarding || prerunner;
        // Returning players who skipped to zone 2: keep only cycle-1 wall narrow.
        int attempts = GameManager.Instance != null ? GameManager.Instance.RunCount + 1 : 1;
        if (prerunner && attempts >= 2)
            _onboardingCycle = 1; // wall narrow only, then free
        else
            _onboardingCycle = onboard ? 1 : 5;
        _forceLaneLock = false;
        _distance = baseDistance;

        if (prerunner)
        {
            aimSeconds = 0.85f;
            throwSeconds = 0.65f;
            counterSeconds = 2.0f;
            arenaSpeed = GameConfig.Active != null ? GameConfig.Active.bossFixedSpeed : 14f;
            // Period effectively ~5s via RecoverSeconds + stages.
        }

        _player.LockSpeed(arenaSpeed);

        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner != null)
        {
            spawner.ForceStraight = true;
            spawner.SuppressHazards = true;
            spawner.SetSpawnAhead(baseDistance + distanceSwing + 70f);
        }

        if (CounterCueTone.Instance == null)
            new GameObject("CounterCueTone").AddComponent<CounterCueTone>();

        if (ItemSlot.Instance != null)
            ItemSlot.Instance.RecordEntryLoadout();

        BuildKing();
        EnterStage(KingStage.Aim);
        AnnouncePhase();
    }

    private void AnnouncePhase()
    {
        OnPhaseChanged?.Invoke(Phase);

        if (AdRewardService.Instance != null)
            AdRewardService.Instance.SetFinalPhaseBlock(_phase >= 2);

        if (StoryEngine.Instance != null)
            StoryEngine.Instance.ReportBossPhase(Phase);
    }

    /// Losing the fight restarts the fight, not the run: the runner part is
    /// already earned. The attempt counter still goes up.
    public bool TryAbsorbDefeat()
    {
        if (!Active)
            return false;

        if (_prerunner)
        {
            DismissPrerunner();
            return true;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.CountRun();

        _hp = _maxHp;
        _phase = 0;
        _counters = 0;
        _droppedPhase = -1;
        _tookHit = false;
        _throwSpeedMul = 1f;
        _onboardingCycle = runOnboarding ? 1 : 5;
        _forceLaneLock = false;
        _distance = baseDistance;

        ClearMarkers();
        ClearShots();

        if (_player != null && _player.Vitals != null)
            _player.Vitals.Restore();

        if (ItemSlot.Instance != null)
            ItemSlot.Instance.RestoreEntryLoadout();

        if (CollapseLine.Instance != null)
            CollapseLine.Instance.ResetGap();

        EnterStage(KingStage.Aim);
        AnnouncePhase();
        return true;
    }

    private void DismissPrerunner()
    {
        _dismissed = true;
        _stage = KingStage.Beaten;
        ClearMarkers();
        ClearShots();
        _forceLaneLock = false;

        if (_player != null)
        {
            _player.UnlockSpeed();
            if (_player.Vitals != null)
                _player.Vitals.Restore();
        }

        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner != null)
        {
            spawner.ForceStraight = false;
            spawner.SuppressHazards = false;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner("정찰기 이탈", 1.6f);

        OnDefeated?.Invoke();
    }

    private void Update()
    {
        if (!Active || _player == null || _player.IsDead)
            return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        if (_whiteFlash > 0f)
        {
            _whiteFlash -= Time.unscaledDeltaTime;
            ApplyKingFlash(_whiteFlash > 0f);
        }

        if (_forceLaneLock && _player != null)
        {
            // Onboarding cycle 1: the arena is one lane wide.
            while ((int)_player.CurrentLane < _lockedLane)
                SimulateLane(+1);
            while ((int)_player.CurrentLane > _lockedLane)
                SimulateLane(-1);
        }

        _timer -= Time.deltaTime;
        MoveKing();

        if (_stage == KingStage.Counter)
        {
            float life = _stageDuration > 0f ? Mathf.Clamp01(_timer / _stageDuration) : 0f;
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i].Visible)
                    _markers[i].SetLife(life);
            }

            CheckCounter();
        }

        if (_timer > 0f)
            return;

        Advance();
    }

    private void SimulateLane(int dir)
    {
        int next = Mathf.Clamp((int)_player.CurrentLane + dir, -1, 1);
        _player.ForceLane((Lane)next);
    }

    private void Advance()
    {
        switch (_stage)
        {
            case KingStage.Aim:
                LaunchVolley();
                EnterStage(KingStage.Throw);
                break;
            case KingStage.Throw:
                OpenCounterWindows();
                EnterStage(KingStage.Counter);
                break;
            case KingStage.Counter:
                CloseCounterWindows();
                EnterStage(KingStage.Recover);
                break;
            case KingStage.Stagger:
                EnterStage(KingStage.Aim);
                break;
            default:
                EnterStage(KingStage.Aim);
                break;
        }
    }

    private void EnterStage(KingStage stage)
    {
        _stage = stage;
        KingPhaseData data = PhaseData;

        switch (stage)
        {
            case KingStage.Aim:
                _stageDuration = Telegraph(data != null ? data.aimTime : aimSeconds);
                _timer = _stageDuration;
                PickVolley();
                ShowAimMarkers();
                break;
            case KingStage.Throw:
                _stageDuration = (data != null ? data.throwTime : throwSeconds) / Mathf.Max(1f, _throwSpeedMul);
                _timer = _stageDuration;
                break;
            case KingStage.Counter:
                float window = data != null ? data.counterWindow : counterSeconds;
                if (UpgradeSystem.Instance != null)
                    window += UpgradeSystem.Instance.Mods().counterWindowBonus;
                _stageDuration = window;
                _timer = _stageDuration;
                break;
            case KingStage.Recover:
                _stageDuration = RecoverSeconds();
                _timer = _stageDuration;
                DropPhaseItem();
                FinishOnboardingCycle();
                break;
            case KingStage.Stagger:
                _stageDuration = data != null ? data.staggerSeconds : staggerSeconds;
                _timer = _stageDuration;
                float close = data != null ? data.staggerCloseIn : staggerCloseIn;
                _distance = Mathf.Max(24f, _distance - close);
                break;
        }
    }

    private void FinishOnboardingCycle()
    {
        if (_onboardingCycle <= 0 || _onboardingCycle > 4)
            return;

        if (_onboardingCycle == 4 && _counters == 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowSubtitle(Speaker.Sweeper, "던진 자리로 들어가. 거기가 비어 있어. …아니 비어 있는 게 아니라, 아무튼 거기로 가.", 3.5f, false);
            OnboardingMetrics.KingHint();
        }

        _onboardingCycle++;
        _forceLaneLock = false;
    }

    private static float Telegraph(float seconds)
    {
        float floor = GameConfig.Active != null ? GameConfig.Active.minTelegraphSec : TelegraphFloor;
        Debug.Assert(seconds * TelegraphScale + ItemSlot.TelegraphBonus >= floor - 0.001f,
            "Telegraph below absolute floor " + floor);
        return Mathf.Max(floor, seconds * TelegraphScale) + ItemSlot.TelegraphBonus;
    }

    private float Period()
    {
        if (_prerunner)
            return 5.0f;

        switch (_phase)
        {
            case 0:
                return 4.0f;
            case 1:
                return 3.4f;
            default:
                return 2.8f;
        }
    }

    private float RecoverSeconds()
    {
        float used = Telegraph(aimSeconds) + throwSeconds + counterSeconds;
        return Mathf.Max(minRecoverSeconds, Period() - used);
    }

    /// Exactly one tape per phase, and only if the deck actually needs it. The
    /// fight is a test of reading, not of luck with drops.
    private void DropPhaseItem()
    {
        if (_droppedPhase == _phase)
            return;

        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner == null || _player == null || _player.Vitals == null)
            return;

        PickupKind kind = _player.Vitals.Hp < _player.Vitals.MaxHp
            ? PickupKind.DeckTape
            : PickupKind.BoosterCell;

        int lane = FreeLane();
        if (lane == -2)
            lane = 0;

        if (spawner.DropItem(kind, lane, 34f))
            _droppedPhase = _phase;
    }

    private int WindowCount()
    {
        return _phase >= 2 ? 2 : 1;
    }

    /// P1 one lane, P2 two lanes with shards, P3 every lane with one kind so a
    /// single jump or slide still clears it.
    private void PickVolley()
    {
        _thrownLanes.Clear();
        _trapLane = -2;
        _forceLaneLock = false;

        int[] lanes = { -1, 0, 1 };
        Shuffle(lanes);

        // Onboarding cycle 1: one lane only, then lock the player onto it.
        if (_onboardingCycle == 1)
        {
            _volleyKind = ThrowKind.ContainerShard;
            int lane = (int)_player.CurrentLane;
            _thrownLanes.Add(lane);
            _lockedLane = lane;
            _forceLaneLock = true;
            return;
        }

        if (_phase == 0 || _onboardingCycle == 2 || _onboardingCycle == 3)
        {
            _volleyKind = ThrowKind.ContainerShard;
            _thrownLanes.Add(lanes[0]);
            return;
        }

        if (_phase == 1)
        {
            _volleyKind = ThrowKind.ContainerShard;
            _thrownLanes.Add(lanes[0]);
            _thrownLanes.Add(lanes[1]);
            return;
        }

        _volleyKind = Chance(0.5f) ? ThrowKind.RetrievalNet : ThrowKind.TagLauncher;
        _thrownLanes.Add(-1);
        _thrownLanes.Add(0);
        _thrownLanes.Add(1);
    }

    private void ShowAimMarkers()
    {
        ClearMarkers();

        for (int i = 0; i < _thrownLanes.Count; i++)
            Marker().Show(_thrownLanes[i], LaneMarkStyle.Aim, LeadMetres(), 8f);

        // P2 adds a crack that never becomes an attack. It is a distinct shape
        // so it can be learned rather than guessed.
        if (_phase == 1)
        {
            int free = FreeLane();
            if (free != -2)
                Marker().Show(free, LaneMarkStyle.Crack, LeadMetres() + 3f, 6f);
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayTurn();
    }

    private int FreeLane()
    {
        for (int lane = -1; lane <= 1; lane++)
        {
            if (!_thrownLanes.Contains(lane))
                return lane;
        }

        return -2;
    }

    private float LeadMetres()
    {
        return Mathf.Max(10f, _player.CurrentSpeed * 1.1f);
    }

    private void LaunchVolley()
    {
        float flight = throwSeconds / Mathf.Max(1f, _throwSpeedMul);
        KingPhaseData data = PhaseData;
        if (data != null)
            flight = data.throwTime / Mathf.Max(1f, _throwSpeedMul);

        float start = _player.PathDistance + _distance;
        for (int i = 0; i < _thrownLanes.Count; i++)
            Shot().Launch(_thrownLanes[i], _volleyKind, start, flight);

        if (_phase >= 2 && _onboardingCycle > 4 && Chance(0.4f))
        {
            int drop = _thrownLanes[Range(0, _thrownLanes.Count)];
            Shot().Launch(drop, ThrowKind.RailDrop, start, flight * 1.4f);
        }
    }

    /// The lane it came from is the lane to come back to.
    private void OpenCounterWindows()
    {
        ClearMarkers();
        _counterLanes.Clear();
        _consumedWindows = 0;

        int wanted = Mathf.Min(WindowCount(), _thrownLanes.Count);
        if (_onboardingCycle > 0 && _onboardingCycle <= 3)
            wanted = 1;

        int[] pool = _thrownLanes.ToArray();
        Shuffle(pool);

        if (reverseRules)
            SortByDanger(pool);

        for (int i = 0; i < wanted; i++)
            _counterLanes.Add(pool[i]);

        if (_counterLanes.Count > 1 && _onboardingCycle > 4)
            _trapLane = _counterLanes[_counterLanes.Count - 1];

        for (int i = 0; i < _counterLanes.Count; i++)
        {
            LaneMarkStyle style = _counterLanes[i] == _trapLane ? LaneMarkStyle.Trap : LaneMarkStyle.Counter;
            Marker().Show(_counterLanes[i], style, LeadMetres() * 0.55f, 9f);
        }

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCounterCue();
        else if (CounterCueTone.Instance != null)
            CounterCueTone.Instance.PlayOpen();
    }

    /// Reverse mode puts the pad on the side that costs the most to reach.
    private void SortByDanger(int[] lanes)
    {
        int here = (int)_player.CurrentLane;
        Array.Sort(lanes, (a, b) => Mathf.Abs(b - here).CompareTo(Mathf.Abs(a - here)));
    }

    private void CheckCounter()
    {
        if (_consumedWindows >= _counterLanes.Count)
            return;

        int lane = (int)_player.CurrentLane;
        if (!_counterLanes.Contains(lane))
            return;

        if (lane == _trapLane)
        {
            _counterLanes.Remove(lane);
            HideMarkerForLane(lane);
            _player.TakeHit(HitKind.KingThrow);
            return;
        }

        if (!_player.CanCounter)
            return;

        Counter(lane);
    }

    private void Counter(int lane)
    {
        _consumedWindows++;
        _counters++;
        OnboardingMetrics.KingCounter(_onboardingCycle);
        _counterLanes.Remove(lane);
        HideMarkerForLane(lane);

        _player.EnterCounter(0.5f);

        // Onboarding cycle 2: gold is harmless — teach the step, not the hit.
        bool dealDamage = _onboardingCycle != 2;
        if (dealDamage)
            _hp = Mathf.Max(0, _hp - 1);

        float hitStop = GameConfig.Active != null ? GameConfig.Active.counterHitStopSec : 0.15f;
        _player.HitStop(hitStop);
        _whiteFlash = 0.05f;
        ApplyKingFlash(true);

        if (GameManager.Instance != null && dealDamage)
            GameManager.Instance.AddBonus(500);

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayCounterHit();

        if (CollapseLine.Instance != null && dealDamage)
        {
            if (reverseRules)
                CollapseLine.Instance.Push(-8f);
            else
                CollapseLine.Instance.Reward();
        }

        if (_hp <= 0)
        {
            Defeat();
            return;
        }

        if (dealDamage && _hp % hpPerPhase == 0)
            NextPhase();
    }

    private void CloseCounterWindows()
    {
        if (_consumedWindows == 0)
        {
            KingPhaseData data = PhaseData;
            float mul = data != null ? data.missPenaltySpeedMul : 1.30f;
            _throwSpeedMul = mul;

            if (CounterCueTone.Instance != null)
                CounterCueTone.Instance.PlayMiss();
        }
        else
        {
            _throwSpeedMul = 1f;
        }

        if (reverseRules && CollapseLine.Instance != null)
            CollapseLine.Instance.Push(5f);

        if (reverseRules && _counters < MaxCounters && CollapseLine.Instance != null &&
            CollapseLine.Instance.Gap <= 0.5f)
            _player.Kill(DeathCause.TimedOut);

        ClearMarkers();
        _counterLanes.Clear();
        _trapLane = -2;
        _forceLaneLock = false;
    }

    private void NextPhase()
    {
        _phase = Mathf.Min(2, _phase + 1);
        _droppedPhase = -1;
        ClearShots();
        EnterStage(KingStage.Stagger);
        AnnouncePhase();
    }

    private void Defeat()
    {
        if (_prerunner)
        {
            DismissPrerunner();
            EconomyBootstrap.GrantFirstKing();
            return;
        }

        _stage = KingStage.Beaten;
        ClearMarkers();
        ClearShots();

        if (!_tookHit && GameManager.Instance != null)
            GameManager.Instance.AddBonus(GameManager.Instance.Score);

        if (_player != null)
            _player.UnlockSpeed();

        RoadSpawner spawner = RoadSpawner.Instance;
        if (spawner != null)
        {
            spawner.ForceStraight = false;
            spawner.SuppressHazards = false;
        }

        EconomyBootstrap.GrantFirstKing();
        EconomyBootstrap.GrantKingDaily(Zones.Index(Zones.At(_player != null ? _player.PathDistance : 0f)));

        if (SaveSystem.Instance != null && !_tookHit)
        {
            SaveSystem.Instance.Data.kingFlawlessKills++;
            SaveSystem.Instance.MarkDirty();
        }

        OnDefeated?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.ReachDepot();
    }

    private void MoveKing()
    {
        if (_king == null || _player == null)
            return;

        if (_stage != KingStage.Stagger)
        {
            _cyclePhase += Time.deltaTime / Mathf.Max(0.5f, Period());
            float target = baseDistance + Mathf.Sin(_cyclePhase * Mathf.PI * 2f) * distanceSwing;
            _distance = Mathf.MoveTowards(_distance, target, 18f * Time.deltaTime);
        }

        RoadSpawner spawner = RoadSpawner.Instance;
        float distance = _player.PathDistance + _distance;

        Vector3 point;
        float yaw;
        if (spawner == null || !spawner.TryGetPoint(distance, out point, out yaw))
        {
            point = _player.transform.position + Quaternion.Euler(0f, _player.Yaw, 0f) * new Vector3(0f, 0f, _distance);
            yaw = _player.Yaw;
        }

        Quaternion frame = Quaternion.Euler(0f, yaw, 0f);
        _king.position = point + Vector3.up * 1.1f;

        // Riding backwards: the upper body stays turned toward the player.
        _king.rotation = frame * Quaternion.Euler(0f, 180f, 0f);

        if (_arm != null)
        {
            bool raised = _stage == KingStage.Aim || _stage == KingStage.Throw;
            int side = _thrownLanes.Count > 0 ? _thrownLanes[0] : 0;
            _arm.localPosition = new Vector3(-side * 0.9f, raised ? 0.95f : 0.2f, 0.1f);
        }
    }

    private void BuildKing()
    {
        if (_king != null)
            return;

        GameObject prefab = Resources.Load<GameObject>("Retrieval/King_Collector");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Character/King/KingModel");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Hazards/Wreck_Van");

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = "King";
            _king = instance.transform;
            _arm = _king.Find("Arm");
            ArtLibrary.EnsureVisible(instance);

            Collider[] cols = instance.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
                Destroy(cols[i]);

            _kingRenderers = _king.GetComponentsInChildren<Renderer>();
            _kingColors = new Color[_kingRenderers.Length];
            for (int i = 0; i < _kingRenderers.Length; i++)
            {
                Material mat = _kingRenderers[i].material;
                _kingColors[i] = mat.HasProperty("_Color") ? mat.color
                    : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                    : Color.white;
            }

            return;
        }

        BuildKingPrimitives();
    }

    private void BuildKingPrimitives()
    {
        GameObject go = new GameObject("King");
        go.transform.SetParent(transform, false);
        _king = go.transform;

        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(_king, false);
        torso.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
        Collider torsoCol = torso.GetComponent<Collider>();
        if (torsoCol != null)
            Destroy(torsoCol);
        Paint(torso, new Color(0.14f, 0.14f, 0.16f));

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(_king, false);
        board.transform.localPosition = new Vector3(0f, -1.05f, 0f);
        board.transform.localScale = new Vector3(0.9f, 0.1f, 2.4f);
        Collider boardCol = board.GetComponent<Collider>();
        if (boardCol != null)
            Destroy(boardCol);
        Paint(board, new Color(0.26f, 0.22f, 0.20f));

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(_king, false);
        arm.transform.localScale = new Vector3(0.9f, 0.22f, 0.22f);
        Collider armCol = arm.GetComponent<Collider>();
        if (armCol != null)
            Destroy(armCol);
        Paint(arm, new Color(0.42f, 0.40f, 0.38f));
        _arm = arm.transform;

        _kingRenderers = _king.GetComponentsInChildren<Renderer>();
        _kingColors = new Color[_kingRenderers.Length];
        for (int i = 0; i < _kingRenderers.Length; i++)
        {
            Material mat = _kingRenderers[i].material;
            _kingColors[i] = mat.HasProperty("_Color") ? mat.color
                : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                : Color.white;
        }
    }

    private void ApplyKingFlash(bool on)
    {
        if (_kingRenderers == null)
            return;

        for (int i = 0; i < _kingRenderers.Length; i++)
        {
            if (_kingRenderers[i] == null)
                continue;

            Material mat = _kingRenderers[i].material;
            Color c = on ? Color.white : _kingColors[i];
            if (mat.HasProperty("_Color"))
                mat.color = c;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
        }
    }

    private static void Paint(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (material.HasProperty("_Color"))
            material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        renderer.sharedMaterial = material;
    }

    private LaneMarker Marker()
    {
        for (int i = 0; i < _markers.Count; i++)
        {
            if (!_markers[i].Visible)
                return _markers[i];
        }

        LaneMarker marker = LaneMarker.Create(transform, _player);
        _markers.Add(marker);
        return marker;
    }

    private void HideMarkerForLane(int lane)
    {
        for (int i = 0; i < _markers.Count; i++)
        {
            if (_markers[i].Visible && _markers[i].Lane == lane)
                _markers[i].Hide();
        }
    }

    private void ClearMarkers()
    {
        for (int i = 0; i < _markers.Count; i++)
            _markers[i].Hide();
    }

    private KingProjectile Shot()
    {
        for (int i = 0; i < _shots.Count; i++)
        {
            if (!_shots[i].Live)
                return _shots[i];
        }

        KingProjectile shot = KingProjectile.Create(transform, _player);
        _shots.Add(shot);
        return shot;
    }

    private void ClearShots()
    {
        for (int i = 0; i < _shots.Count; i++)
            _shots[i].Retire();
    }

    private void Shuffle(int[] values)
    {
        if (_rng != null)
        {
            _rng.Shuffle(values);
            return;
        }

        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = i;
            int tmp = values[i];
            values[i] = values[j];
            values[j] = tmp;
        }
    }

    private bool Chance(float p)
    {
        return _rng != null ? _rng.Chance(p) : p >= 1f;
    }

    private int Range(int min, int max)
    {
        return _rng != null ? _rng.Range(min, max) : min;
    }

    public void NoteHit()
    {
        _tookHit = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
