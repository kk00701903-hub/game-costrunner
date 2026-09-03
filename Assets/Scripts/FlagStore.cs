using System.Collections.Generic;
using UnityEngine;

/// Story state. Permanent flags survive the process, session flags die with the
/// run, and everything the story engine reads goes through here.
public static class FlagStore
{
    public const string RunCount = "run_count";
    public const string Letters = "letters";
    public const string LettersBurned = "letters_burned";
    public const string KnowsBrother = "knows_brother";
    public const string TagsTotal = "tags_total";
    public const string CleanerAlive = "cleaner_alive";
    public const string SawKingFace = "saw_king_face";
    public const string DeckPieces = "deck_pieces";
    public const string BestScore = "best_score";

    private const string Prefix = "r347_flag_";
    private const string IndexKey = "r347_flag_index";

    private static readonly HashSet<string> Permanent = new HashSet<string>
    {
        RunCount,
        LettersBurned,
        KnowsBrother,
        TagsTotal,
        SawKingFace,
        DeckPieces,
        BestScore
    };

    private static readonly Dictionary<string, int> Session = new Dictionary<string, int>();

    public static string ZoneCleared(int zone)
    {
        return "zone_cleared_" + Mathf.Clamp(zone, 1, 5);
    }

    static FlagStore()
    {
        for (int zone = 1; zone <= 5; zone++)
            Permanent.Add(ZoneCleared(zone));
    }

    public static bool IsPermanent(string key)
    {
        return Permanent.Contains(key);
    }

    public static int GetInt(string key, int fallback = 0)
    {
        if (IsPermanent(key))
            return PlayerPrefs.GetInt(Prefix + key, fallback);

        int value;
        return Session.TryGetValue(key, out value) ? value : fallback;
    }

    public static void SetInt(string key, int value)
    {
        if (IsPermanent(key))
        {
            PlayerPrefs.SetInt(Prefix + key, value);
            RememberKey(key);
            PlayerPrefs.Save();
            return;
        }

        Session[key] = value;
    }

    public static int AddInt(string key, int delta)
    {
        int next = GetInt(key) + delta;
        SetInt(key, next);
        return next;
    }

    public static bool GetBool(string key, bool fallback = false)
    {
        return GetInt(key, fallback ? 1 : 0) != 0;
    }

    public static void SetBool(string key, bool value)
    {
        SetInt(key, value ? 1 : 0);
    }

    /// Called at the start of every attempt. Permanent flags are untouched:
    /// the world is supposed to remember.
    public static void ClearSession()
    {
        Session.Clear();
        SetBool(CleanerAlive, true);
    }

    public static void ClearAll()
    {
        Session.Clear();

        string index = PlayerPrefs.GetString(IndexKey, string.Empty);
        string[] keys = index.Split(';');
        for (int i = 0; i < keys.Length; i++)
        {
            if (!string.IsNullOrEmpty(keys[i]))
                PlayerPrefs.DeleteKey(Prefix + keys[i]);
        }

        PlayerPrefs.DeleteKey(IndexKey);
        PlayerPrefs.DeleteKey("r347_run_count");
        PlayerPrefs.DeleteKey("r347_best_score");
        PlayerPrefs.DeleteKey("r347_deck_pieces");
        PlayerPrefs.Save();
    }

    private static void RememberKey(string key)
    {
        string index = PlayerPrefs.GetString(IndexKey, string.Empty);
        if (index.Contains(key + ";"))
            return;

        PlayerPrefs.SetString(IndexKey, index + key + ";");
    }
}
