using System.Collections.Generic;
using UnityEngine;

/// Dialogue never runs on a timeline. It subscribes to what the player did and
/// says one thing about it, so a run that goes differently sounds different.
public class StoryEngine : MonoBehaviour
{
    public static StoryEngine Instance { get; private set; }

    private const string ResourcePath = "Story/events";

    private readonly List<StoryEvent> _events = new List<StoryEvent>();
    private readonly Dictionary<string, float> _lastFired = new Dictionary<string, float>();
    private readonly Queue<StoryLine> _logQueue = new Queue<StoryLine>();
    private readonly List<StoryLine> _pending = new List<StoryLine>();
    private readonly List<StoryEvent> _candidates = new List<StoryEvent>();

    private StoryChannel _activeChannel;
    private float _activeTimer;
    private float _silenceUntil;
    private float _lastDistance;
    private int _lastZone = -1;
    private bool _hooked;

    private void Awake()
    {
        Instance = this;
        LoadEvents();
    }

    private void Start()
    {
        Hook();
        Raise(StoryTrigger.OnRunCountChanged, GameManager.Instance != null ? GameManager.Instance.RunCount : 0);
    }

    private void LoadEvents()
    {
        _events.Clear();
        _events.AddRange(StoryScript.BuiltIn());

        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
            return;

        StoryEventTable table = JsonUtility.FromJson<StoryEventTable>(asset.text);
        if (table == null || table.events == null)
            return;

        for (int i = 0; i < table.events.Length; i++)
        {
            if (table.events[i] != null && !string.IsNullOrEmpty(table.events[i].id))
                _events.Add(table.events[i]);
        }
    }

    private void Hook()
    {
        if (_hooked || GameManager.Instance == null)
            return;

        GameManager.Instance.OnHit += HandleHit;
        GameManager.Instance.OnPickup += HandlePickup;
        GameManager.Instance.OnGameOver += HandleDeath;
        GameManager.Instance.OnRunCountChanged += HandleRunCount;
        _hooked = true;
    }

    private void OnDestroy()
    {
        if (_hooked && GameManager.Instance != null)
        {
            GameManager.Instance.OnHit -= HandleHit;
            GameManager.Instance.OnPickup -= HandlePickup;
            GameManager.Instance.OnGameOver -= HandleDeath;
            GameManager.Instance.OnRunCountChanged -= HandleRunCount;
        }

        if (Instance == this)
            Instance = null;
    }

    private void HandleHit()
    {
        Raise(StoryTrigger.OnDamage, 0);
    }

    private void HandlePickup(PickupKind kind)
    {
        Raise(StoryTrigger.OnItemPickup, 0, kind.ToString());
    }

    private void HandleDeath()
    {
        Raise(StoryTrigger.OnDeath, 0);
    }

    private void HandleRunCount(int count)
    {
        Raise(StoryTrigger.OnRunCountChanged, count);
    }

    public void ReportBossPhase(int phase)
    {
        Raise(StoryTrigger.OnBossPhase, phase);
    }

    public void ReportZone(int zone)
    {
        if (zone == _lastZone)
            return;

        _lastZone = zone;
        Raise(StoryTrigger.OnZoneEnter, zone);
    }

    private void Update()
    {
        Hook();
        Advance();

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying)
            return;

        float distance = gm.TraveledDistance;
        if (distance > _lastDistance)
        {
            CheckDistance(_lastDistance, distance);
            _lastDistance = distance;
        }
    }

    private void CheckDistance(float from, float to)
    {
        for (int i = 0; i < _events.Count; i++)
        {
            StoryEvent evt = _events[i];
            if (evt.Trigger != StoryTrigger.OnDistance)
                continue;

            if (evt.distance <= from || evt.distance > to)
                continue;

            TryPlay(evt);
        }
    }

    /// The public entry point. Everything that happens in the game calls this
    /// and the table decides whether it is worth saying anything.
    public void Raise(StoryTrigger trigger, int value, string tag = null)
    {
        _candidates.Clear();

        for (int i = 0; i < _events.Count; i++)
        {
            StoryEvent evt = _events[i];
            if (evt.Trigger != trigger)
                continue;

            if (trigger == StoryTrigger.OnZoneEnter && evt.zone != value)
                continue;
            if (trigger == StoryTrigger.OnBossPhase && evt.bossPhase != value)
                continue;
            if (trigger == StoryTrigger.OnItemPickup &&
                !string.IsNullOrEmpty(evt.pickup) && evt.pickup != tag)
                continue;
            if (trigger == StoryTrigger.OnRunCountChanged && !RunCountInRange(evt, value))
                continue;

            _candidates.Add(evt);
        }

        // Several lines can be valid for the same moment. Picking at random is
        // what keeps a pool of variants from always saying its first entry.
        while (_candidates.Count > 0)
        {
            int pick = Random.Range(0, _candidates.Count);
            StoryEvent evt = _candidates[pick];
            _candidates.RemoveAt(pick);

            if (TryPlay(evt))
                return;
        }
    }

    private static bool RunCountInRange(StoryEvent evt, int count)
    {
        if (evt.minRunCount > 0 && count < evt.minRunCount)
            return false;

        return evt.maxRunCount <= 0 || count <= evt.maxRunCount;
    }

    private bool TryPlay(StoryEvent evt)
    {
        if (!Allowed(evt))
            return false;

        string firedKey = FiredKey(evt);
        if (evt.once && FlagStore.GetBool(firedKey))
            return false;

        float last;
        if (evt.cooldown > 0f && _lastFired.TryGetValue(evt.id, out last) &&
            Time.time - last < evt.cooldown)
            return false;

        if (evt.lines == null || evt.lines.Length == 0)
            return false;

        if (evt.once)
            FlagStore.SetBool(firedKey, true);
        _lastFired[evt.id] = Time.time;

        if (evt.setFlags != null)
        {
            for (int i = 0; i < evt.setFlags.Length; i++)
                SetFlag(evt.setFlags[i]);
        }

        Speak(evt);
        return true;
    }

    private static string FiredKey(StoryEvent evt)
    {
        return (evt.permanent ? "story_seen_" : "story_ran_") + evt.id;
    }

    /// "knows_brother" sets true, "cleaner_alive=0" sets a value.
    private static void SetFlag(string spec)
    {
        if (string.IsNullOrEmpty(spec))
            return;

        int split = spec.IndexOf('=');
        if (split < 0)
        {
            FlagStore.SetBool(spec.Trim(), true);
            return;
        }

        string key = spec.Substring(0, split).Trim();
        int value;
        int.TryParse(spec.Substring(split + 1).Trim(), out value);
        FlagStore.SetInt(key, value);
    }

    private bool Allowed(StoryEvent evt)
    {
        GameManager gm = GameManager.Instance;
        int runCount = gm != null ? gm.RunCount : 0;
        if (!RunCountInRange(evt, runCount))
            return false;

        if (evt.requireFlags != null)
        {
            for (int i = 0; i < evt.requireFlags.Length; i++)
            {
                if (!FlagStore.GetBool(evt.requireFlags[i]))
                    return false;
            }
        }

        if (evt.forbidFlags == null)
            return true;

        for (int i = 0; i < evt.forbidFlags.Length; i++)
        {
            if (FlagStore.GetBool(evt.forbidFlags[i]))
                return false;
        }

        return true;
    }

    private void Speak(StoryEvent evt)
    {
        StoryChannel channel = evt.Channel;

        // A cutscene interrupts. A log waits its turn. Anything else that
        // arrives late is simply not said, because two voices is worse than one
        // missing line.
        if (_activeTimer > 0f && channel < _activeChannel)
        {
            if (channel != StoryChannel.UiLog)
                return;

            for (int i = 0; i < evt.lines.Length; i++)
                Enqueue(evt, evt.lines[i]);

            return;
        }

        _pending.Clear();
        for (int i = 1; i < evt.lines.Length; i++)
            _pending.Add(Resolve(evt, evt.lines[i]));

        _activeChannel = channel;
        Present(Resolve(evt, evt.lines[0]));
    }

    private void Enqueue(StoryEvent evt, StoryLine line)
    {
        _logQueue.Enqueue(Resolve(evt, line));
    }

    private static StoryLine Resolve(StoryEvent evt, StoryLine line)
    {
        return new StoryLine
        {
            speaker = string.IsNullOrEmpty(line.speaker) ? evt.speaker : line.speaker,
            text = line.text,
            seconds = line.seconds <= 0f ? 3f : line.seconds,
            silence = line.silence
        };
    }

    private void Present(StoryLine line)
    {
        _activeTimer = line.seconds;

        if (line.silence)
        {
            _silenceUntil = Time.time + line.seconds;
            if (GameAudio.Instance != null)
                GameAudio.Instance.DuckAll(line.seconds);
        }

        Speaker who = StoryEvent.SpeakerOf(line.speaker, Speaker.None);

        // Retrieval broadcasts from the drones and always comes through. The
        // open channel is what the collapse eats, so the closer it gets the
        // more Doha is alone with it.
        if (_activeChannel == StoryChannel.Radio && !RadioAlive(who))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowSubtitle(who, "…치직", Mathf.Min(1.2f, line.seconds), true);

            return;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle(who, line.text, line.seconds, false);
    }

    private static bool RadioAlive(Speaker who)
    {
        if (who == Speaker.Retrieval)
            return true;

        if (FlagStore.GetInt("radio_battery", 1) <= 0)
            return false;

        CollapseLine collapse = CollapseLine.Instance;
        return collapse == null || collapse.Warning < 0.6f;
    }

    private void Advance()
    {
        if (_activeTimer > 0f)
        {
            _activeTimer -= Time.deltaTime;
            if (_activeTimer > 0f)
                return;
        }

        if (_pending.Count > 0)
        {
            StoryLine next = _pending[0];
            _pending.RemoveAt(0);
            Present(next);
            return;
        }

        if (_logQueue.Count == 0)
            return;

        _activeChannel = StoryChannel.UiLog;
        Present(_logQueue.Dequeue());
    }

    public bool Silenced => Time.time < _silenceUntil;
}
