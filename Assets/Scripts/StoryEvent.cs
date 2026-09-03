using System;

/// Who is talking. The colour and the manner both come from here, so a line
/// never has to name its own speaker.
public enum Speaker
{
    None,
    /// The cleaner on the open channel. Calm, asks questions.
    Sweeper,
    /// Bungeo. Too loud, always mid-sentence.
    Bungeo,
    /// Retrieval. Polite, and never answers.
    Retrieval,
    /// Ihan. Only ever heard, never met.
    Ihan,
    /// Doha herself, talking to keep from stopping.
    Doha
}

/// Higher wins. A lower channel is dropped rather than queued, except the log.
public enum StoryChannel
{
    UiLog = 10,
    AmbientVoice = 40,
    Radio = 60,
    Cutscene = 100
}

public enum StoryTrigger
{
    None,
    OnDistance,
    OnZoneEnter,
    OnItemPickup,
    OnDamage,
    OnBossPhase,
    OnDeath,
    OnRunCountChanged
}

[Serializable]
public class StoryLine
{
    public string speaker;
    public string text;
    public float seconds = 3f;

    /// Everything but the board drops out for this line.
    public bool silence;
}

[Serializable]
public class StoryEvent
{
    public string id;
    public string trigger;
    public string channel = "Radio";
    public string speaker = "Sweeper";

    public float distance;
    public int zone;
    public string pickup;
    public int bossPhase;

    public int minRunCount;
    public int maxRunCount;
    public string[] requireFlags;
    public string[] forbidFlags;
    public string[] setFlags;

    /// Once per attempt by default. Set permanent for once per save file.
    public bool once = true;
    public bool permanent;
    public float cooldown;

    public StoryLine[] lines;

    private StoryTrigger _trigger = StoryTrigger.None;
    private StoryChannel _channel = StoryChannel.Radio;
    private Speaker _speaker = Speaker.None;
    private bool _parsed;

    public StoryTrigger Trigger
    {
        get
        {
            Parse();
            return _trigger;
        }
    }

    public StoryChannel Channel
    {
        get
        {
            Parse();
            return _channel;
        }
    }

    public Speaker DefaultSpeaker
    {
        get
        {
            Parse();
            return _speaker;
        }
    }

    // JsonUtility cannot read enums from strings, so the text form is the
    // authored form and the enum is derived once on first use.
    private void Parse()
    {
        if (_parsed)
            return;

        _parsed = true;
        _trigger = ParseEnum(trigger, StoryTrigger.None);
        _channel = ParseEnum(channel, StoryChannel.Radio);
        _speaker = ParseEnum(speaker, Speaker.None);
    }

    private static T ParseEnum<T>(string value, T fallback) where T : struct
    {
        if (string.IsNullOrEmpty(value))
            return fallback;

        T parsed;
        return Enum.TryParse(value, true, out parsed) ? parsed : fallback;
    }

    public static Speaker SpeakerOf(string value, Speaker fallback)
    {
        return ParseEnum(value, fallback);
    }
}

[Serializable]
public class StoryEventTable
{
    public StoryEvent[] events;
}
