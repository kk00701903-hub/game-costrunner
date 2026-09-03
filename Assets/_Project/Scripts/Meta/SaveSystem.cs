using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// Quiet JSON save. Failures retry silently — never surface a dialog.
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string FileName = "save.json";
    private const float RetrySeconds = 2f;

    private SaveData _data;
    private bool _dirty;
    private float _retryAt = -1f;
    private string _path;

    public SaveData Data
    {
        get
        {
            if (_data == null)
                Load();
            return _data;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _path = Path.Combine(Application.persistentDataPath, FileName);
        Load();
        ServiceLocator.Register(this);
        ResetDailyIfNeeded();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    private void Update()
    {
        if (_dirty)
            SaveNow();

        if (_retryAt > 0f && Time.unscaledTime >= _retryAt)
        {
            _retryAt = -1f;
            SaveNow();
        }
    }

    public void MarkDirty()
    {
        _dirty = true;
    }

    public void SaveNow()
    {
        if (_data == null)
            return;

        try
        {
            string json = JsonUtility.ToJson(_data, false);
            File.WriteAllText(_path, json);
            _dirty = false;
            _retryAt = -1f;
        }
        catch (Exception)
        {
            _dirty = true;
            _retryAt = Time.unscaledTime + RetrySeconds;
        }
    }

    public void Load()
    {
        _data = new SaveData();
        if (!File.Exists(_path))
            return;

        try
        {
            string json = File.ReadAllText(_path);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded != null)
                _data = loaded;
        }
        catch (Exception)
        {
            // Keep a fresh SaveData. Never block the player on a corrupt file.
        }

        Normalize(_data);
    }

    public void ResetAll()
    {
        _data = new SaveData();
        Normalize(_data);
        SaveNow();
    }

    /// 04:00 local soft reset for daily counters.
    public void ResetDailyIfNeeded()
    {
        string today = EconomyClock.TodayKey();
        if (_data.lastResetDate == today)
            return;

        _data.lastResetDate = today;
        _data.dailyRunsToday = 0;
        _data.adUsesToday_Double = 0;
        _data.adUsesToday_Revive = 0;
        _data.adUsesToday_Gacha = 0;
        _data.adUsesToday_Reroll = 0;
        MarkDirty();
    }

    private static void Normalize(SaveData data)
    {
        if (data.partLevels == null || data.partLevels.Length != 5)
            data.partLevels = new int[5];
        if (data.zoneCleared == null || data.zoneCleared.Length != 5)
            data.zoneCleared = new bool[5];
        if (data.ownedParts == null)
            data.ownedParts = new List<string>();
        if (data.collectedTagIds == null)
            data.collectedTagIds = new List<int>();
        if (data.lostItemIds == null)
            data.lostItemIds = new List<int>();
        if (data.unlockedSkins == null)
            data.unlockedSkins = new List<string>();
        if (data.flags == null)
            data.flags = new List<string>();
        if (data.dailyMissionIds == null)
            data.dailyMissionIds = new List<string>();
        if (data.dailyMissionDone == null)
            data.dailyMissionDone = new List<bool>();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}

/// Day key rolls over at 04:00 local time.
public static class EconomyClock
{
    public static DateTime NowLocal()
    {
        return DateTime.Now;
    }

    public static string TodayKey()
    {
        DateTime now = NowLocal();
        if (now.Hour < 4)
            now = now.AddDays(-1);
        return now.ToString("yyyy-MM-dd");
    }

    public static string WeekKey()
    {
        DateTime now = NowLocal();
        if (now.Hour < 4)
            now = now.AddDays(-1);

        // Monday 04:00 week start.
        int delta = ((int)now.DayOfWeek + 6) % 7;
        DateTime monday = now.Date.AddDays(-delta);
        return monday.ToString("yyyy-MM-dd");
    }
}
