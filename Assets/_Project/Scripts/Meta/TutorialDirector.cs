using System;
using UnityEngine;

/// First-run teaching without saying "tutorial". Units follow
/// Forced → Choice → Pressure. Skip rules scale with run_count.
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    public enum Unit
    {
        None = 0,
        Steer = 1,
        Jump = 2,
        Slide = 3,
        Combo = 4,
        FirstHit = 5,
        ItemSlot = 6,
        Done = 7
    }

    [SerializeField] private float unitSteerEnd = 16f;
    [SerializeField] private float unitJumpEnd = 28f;
    [SerializeField] private float unitSlideEnd = 40f;
    [SerializeField] private float unitComboEnd = 53f;
    [SerializeField] private float unitHitAt = 60f;
    [SerializeField] private float unitItemAt = 75f;
    [SerializeField] private float tutorialEnd = 90f;
    [SerializeField] private float softFailSpeedFactor = 0.4f;
    [SerializeField] private float collapseHoldMetres = 400f;

    private Unit _unit = Unit.None;
    private float _elapsed;
    private bool _active;
    private bool _firstHitDone;
    private bool _tapeDropped;
    private bool _boosterDropped;
    private bool _itemNudged;
    private bool _comboVoicePlayed;
    private int _runCount;

    public Unit CurrentUnit => _unit;
    public bool IsTeaching => _active && _unit > Unit.None && _unit < Unit.Done;
    public bool SoftFailEnabled => IsTeaching && _unit <= Unit.Combo && Beat() != BeatKind.Pressure;
    public bool HoldCollapseLine => _active && _runCount <= 1 &&
        (GameManager.Instance == null || GameManager.Instance.TraveledDistance < collapseHoldMetres);
    public bool SuppressFirstHitScript => _runCount > 1;

    public event Action<Unit> OnUnitChanged;
    public event Action<string> OnMetric;

    public enum BeatKind { Forced, Choice, Pressure }

    private void Awake()
    {
        Instance = this;
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted += HandleRunStarted;
    }

    private void HandleRunStarted()
    {
        _runCount = GameManager.Instance != null ? GameManager.Instance.RunCount : 0;
        // RunCount is deaths so far; first ever play is 0 before any death.
        int attempts = _runCount + 1;

        _elapsed = 0f;
        _firstHitDone = false;
        _tapeDropped = false;
        _boosterDropped = false;
        _itemNudged = false;
        _comboVoicePlayed = false;

        if (attempts >= 5)
        {
            // Zone 1 is a normal course.
            _active = false;
            SetUnit(Unit.Done);
            return;
        }

        _active = true;
        if (attempts >= 2)
        {
            // Skip forced beats; start at choice/pressure structures.
            SetUnit(Unit.Steer);
            TutorialHints.SuppressAll = true;
        }
        else
        {
            TutorialHints.SuppressAll = false;
            SetUnit(Unit.Steer);
            TutorialHints.Show(TutorialHints.Id.Steer);
        }

        OnMetric?.Invoke("tutorial_start");
    }

    private void Update()
    {
        if (!_active || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        _elapsed += Time.deltaTime;
        AdvanceByTime();
        RunUnitLogic();
    }

    private void AdvanceByTime()
    {
        if (_unit == Unit.Done)
            return;

        Unit next = _unit;
        if (_elapsed >= tutorialEnd)
            next = Unit.Done;
        else if (_elapsed >= unitItemAt)
            next = Unit.ItemSlot;
        else if (_elapsed >= unitHitAt)
            next = Unit.FirstHit;
        else if (_elapsed >= unitComboEnd)
            next = Unit.Combo;
        else if (_elapsed >= unitSlideEnd)
            next = Unit.Slide;
        else if (_elapsed >= unitJumpEnd)
            next = Unit.Jump;
        else if (_elapsed >= 0f)
            next = Unit.Steer;

        if (next != _unit)
            SetUnit(next);
    }

    private void RunUnitLogic()
    {
        switch (_unit)
        {
            case Unit.Jump:
                if (!TutorialHints.SuppressAll)
                    TutorialHints.Show(TutorialHints.Id.Jump);
                break;
            case Unit.Slide:
                if (!TutorialHints.SuppressAll)
                    TutorialHints.Show(TutorialHints.Id.Slide);
                break;
            case Unit.Combo:
                if (!_comboVoicePlayed && StoryEngine.Instance != null)
                {
                    _comboVoicePlayed = true;
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowSubtitle(
                            Speaker.Retrieval,
                            "자산 A-0347. 현재 속도는 회수 절차에 비효율적입니다. 감속을 권장드립니다.",
                            4f,
                            false);
                    }
                }
                break;
            case Unit.FirstHit:
                TryScriptedHit();
                break;
            case Unit.ItemSlot:
                TryDropBooster();
                TryNudgeItem();
                break;
            case Unit.Done:
                if (_active)
                {
                    _active = false;
                    TutorialHints.Hide();
                    OnMetric?.Invoke("tutorial_complete");
                }
                break;
        }
    }

    private void TryScriptedHit()
    {
        if (_firstHitDone || SuppressFirstHitScript)
            return;

        PlayerController player = FindPlayer();
        if (player == null || player.IsDead)
            return;

        _firstHitDone = true;
        OnMetric?.Invoke("tutorial_first_hit");

        // Scripted: unavoidable. Teaches deck cracks, slowdown, collapse, i-frames.
        player.TakeHit(HitKind.Obstacle, scripted: true);

        if (!_tapeDropped && RoadSpawner.Instance != null)
        {
            _tapeDropped = true;
            RoadSpawner.Instance.DropItem(PickupKind.DeckTape, 0, 28f);
        }
    }

    private void TryDropBooster()
    {
        if (_boosterDropped || RoadSpawner.Instance == null)
            return;

        _boosterDropped = true;
        RoadSpawner.Instance.DropItem(PickupKind.BoosterCell, 0, 18f);
        if (!TutorialHints.SuppressAll)
            TutorialHints.Show(TutorialHints.Id.Item);
    }

    private void TryNudgeItem()
    {
        if (_itemNudged || ItemSlot.Instance == null)
            return;
        if (!ItemSlot.Instance.HasItem)
            return;
        if (ItemSlot.Instance.HasActive)
            return;

        // 3 seconds of no activation → bounce the slot once.
        if (_elapsed < unitItemAt + 3f)
            return;

        _itemNudged = true;
        ItemSlot.Instance.PulseHint();
    }

    /// Soft-fail: lose speed, keep HP. Only during forced/choice beats of units 1–4.
    public bool TrySoftFail(PlayerController player)
    {
        if (!SoftFailEnabled || player == null)
            return false;

        player.ApplySoftFail(softFailSpeedFactor);
        OnMetric?.Invoke("tutorial_soft_fail");
        return true;
    }

    public BeatKind Beat()
    {
        // Rough thirds inside each unit window.
        float local;
        switch (_unit)
        {
            case Unit.Steer:
                local = Mathf.InverseLerp(0f, unitSteerEnd, _elapsed);
                break;
            case Unit.Jump:
                local = Mathf.InverseLerp(unitSteerEnd, unitJumpEnd, _elapsed);
                break;
            case Unit.Slide:
                local = Mathf.InverseLerp(unitJumpEnd, unitSlideEnd, _elapsed);
                break;
            case Unit.Combo:
                local = Mathf.InverseLerp(unitSlideEnd, unitComboEnd, _elapsed);
                break;
            default:
                return BeatKind.Pressure;
        }

        if (local < 0.34f)
            return BeatKind.Forced;
        if (local < 0.67f)
            return BeatKind.Choice;
        return BeatKind.Pressure;
    }

    private void SetUnit(Unit unit)
    {
        if (_unit == unit)
            return;
        _unit = unit;
        OnUnitChanged?.Invoke(unit);
        OnMetric?.Invoke("tutorial_unit_" + (int)unit);
        OnboardingMetrics.Note("tutorial_unit_" + (int)unit);
        if (unit == Unit.Done)
            OnboardingMetrics.TutorialComplete();
    }

    private static PlayerController FindPlayer()
    {
        return GameManager.Instance != null && GameManager.Instance.PlayerVitals != null
            ? GameManager.Instance.PlayerVitals.GetComponent<PlayerController>()
            : FindObjectOfType<PlayerController>();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunStarted -= HandleRunStarted;
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}
