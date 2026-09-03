using System;
using System.Collections.Generic;
using UnityEngine;

/// Persistent save blob. JSON at Application.persistentDataPath/save.json.
[Serializable]
public class SaveData
{
    public int version = 1;
    public int runCount;
    public long coins;
    public long alloy;
    public long deckShards;
    public int[] partLevels = new int[5];
    public List<string> ownedParts = new List<string>();
    public int gachaPityCounter;
    public int freeTenPullTickets;
    public bool[] zoneCleared = new bool[5];
    public int letters;
    public int lettersBurned;
    public List<int> collectedTagIds = new List<int>();
    public List<int> lostItemIds = new List<int>();
    public List<string> unlockedSkins = new List<string>();
    public List<string> flags = new List<string>();
    public string lastDailySeedDate = "";
    public int dailyRunsToday;
    public int adUsesToday_Double;
    public int adUsesToday_Revive;
    public int adUsesToday_Gacha;
    public int adUsesToday_Reroll;
    public int attendanceDay;
    public string lastAttendanceDate = "";
    public bool firstKingDefeated;
    public bool tutorialGrantDone;
    public bool firstDeathGrantDone;
    public int bestScore;
    public long totalDistanceMetres;
    public int kingFlawlessKills;
    public string lastResetDate = "";
    public List<string> dailyMissionIds = new List<string>();
    public List<bool> dailyMissionDone = new List<bool>();
    public string weeklyChallengeId = "";
    public bool weeklyChallengeDone;
}
