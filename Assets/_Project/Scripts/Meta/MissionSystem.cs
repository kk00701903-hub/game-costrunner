using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MissionDef
{
    public string id;
    public string description;
    public int coinReward;
    public int alloyReward;
    public int shardReward;
    public MissionKind kind;
    public int target;
}

public enum MissionKind
{
    Distance,
    Tags,
    Counters,
    NoHitDistance,
    DebrisClear,
    GrindSeconds
}

/// Daily 3 / weekly 1 / attendance 7-day loop. No streak punishment.
public class MissionSystem : MonoBehaviour
{
    public static MissionSystem Instance { get; private set; }

    public event Action OnChanged;

    private readonly List<MissionDef> _pool = new List<MissionDef>();
    private SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ServiceLocator.Register(this);
        BuildPool();
        RefreshIfNeeded();
    }

    private void BuildPool()
    {
        _pool.Clear();
        _pool.Add(M("dist_1500", "1,500m 이상 주행", 150, 0, 0, MissionKind.Distance, 1500));
        _pool.Add(M("tags_40", "회수 태그 40개 획득", 150, 2, 0, MissionKind.Tags, 40));
        _pool.Add(M("counter_6", "왕 반격 6회 성공", 150, 0, 1, MissionKind.Counters, 6));
        _pool.Add(M("nohit_800", "무피격으로 800m 주행", 200, 0, 0, MissionKind.NoHitDistance, 800));
        _pool.Add(M("debris_1", "잔해 수거 구간 1회 완주", 0, 5, 0, MissionKind.DebrisClear, 1));
        _pool.Add(M("grind_30", "그라인드 누적 30초", 150, 0, 0, MissionKind.GrindSeconds, 30));
    }

    private static MissionDef M(string id, string desc, int coin, int alloy, int shard, MissionKind kind, int target)
    {
        return new MissionDef
        {
            id = id,
            description = desc,
            coinReward = coin,
            alloyReward = alloy,
            shardReward = shard,
            kind = kind,
            target = target
        };
    }

    public void RefreshIfNeeded()
    {
        if (Save == null)
            return;

        string today = EconomyClock.TodayKey();
        if (Save.lastDailySeedDate != today || Save.dailyMissionIds.Count != 3)
            RollDaily(today);

        string week = EconomyClock.WeekKey();
        if (string.IsNullOrEmpty(Save.weeklyChallengeId) || !Save.weeklyChallengeId.StartsWith(week))
            RollWeekly(week);
    }

    private void RollDaily(string today)
    {
        Save.lastDailySeedDate = today;
        Save.dailyMissionIds.Clear();
        Save.dailyMissionDone.Clear();

        var rng = new DeterministicRandom(Hash(today) ^ 0xD41A);
        var indices = new List<int>();
        for (int i = 0; i < _pool.Count; i++)
            indices.Add(i);
        rng.Shuffle(indices);

        for (int i = 0; i < 3 && i < indices.Count; i++)
        {
            Save.dailyMissionIds.Add(_pool[indices[i]].id);
            Save.dailyMissionDone.Add(false);
        }

        Dirty();
        OnChanged?.Invoke();
    }

    private void RollWeekly(string week)
    {
        string[] ids = { "glass_deck", "blackout", "bare", "reverse_week" };
        var rng = new DeterministicRandom(Hash(week) ^ 0x7EE7);
        Save.weeklyChallengeId = week + ":" + ids[rng.Range(0, ids.Length)];
        Save.weeklyChallengeDone = false;
        Dirty();
    }

    public IReadOnlyList<string> DailyIds => Save != null ? Save.dailyMissionIds : Array.Empty<string>();

    public MissionDef Find(string id)
    {
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i].id == id)
                return _pool[i];
        return null;
    }

    public bool TryRerollOne(int index)
    {
        if (Save == null || index < 0 || index >= Save.dailyMissionIds.Count)
            return false;
        if (AdRewardService.Instance == null || !AdRewardService.Instance.TryRerollMission())
            return false;

        var rng = new DeterministicRandom(Hash(EconomyClock.TodayKey()) ^ (index * 13 + Save.adUsesToday_Reroll));
        string current = Save.dailyMissionIds[index];
        for (int attempt = 0; attempt < 12; attempt++)
        {
            MissionDef pick = _pool[rng.Range(0, _pool.Count)];
            if (pick.id == current || Save.dailyMissionIds.Contains(pick.id))
                continue;
            Save.dailyMissionIds[index] = pick.id;
            Save.dailyMissionDone[index] = false;
            Dirty();
            OnChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryClaim(int index)
    {
        if (Save == null || index < 0 || index >= Save.dailyMissionIds.Count)
            return false;
        if (index >= Save.dailyMissionDone.Count || Save.dailyMissionDone[index])
            return false;

        // Progression hooks will call MarkProgress; for now Meta UI can force-claim in editor.
        Save.dailyMissionDone[index] = true;
        MissionDef def = Find(Save.dailyMissionIds[index]);
        if (def != null && Wallet.Instance != null)
        {
            Wallet.Instance.AddCoins(def.coinReward, false);
            Wallet.Instance.AddAlloy(def.alloyReward);
            Wallet.Instance.AddDeckShards(def.shardReward);
        }

        if (AllDailyDone())
            GrantDailyBonus();

        Dirty();
        OnChanged?.Invoke();
        return true;
    }

    public void MarkProgress(MissionKind kind, int amount)
    {
        // Progress tracking for live runs lands with Meta UI; keep the hook ready.
        _ = kind;
        _ = amount;
    }

    private bool AllDailyDone()
    {
        if (Save == null || Save.dailyMissionDone.Count == 0)
            return false;
        for (int i = 0; i < Save.dailyMissionDone.Count; i++)
            if (!Save.dailyMissionDone[i])
                return false;
        return true;
    }

    private void GrantDailyBonus()
    {
        if (Wallet.Instance != null)
            Wallet.Instance.AddCoins(150, false);
    }

    public bool TryClaimWeekly()
    {
        if (Save == null || Save.weeklyChallengeDone)
            return false;

        Save.weeklyChallengeDone = true;
        if (Wallet.Instance != null)
        {
            Wallet.Instance.AddCoins(1200, false);
            Wallet.Instance.AddAlloy(20);
            Wallet.Instance.AddDeckShards(6);
        }

        Dirty();
        OnChanged?.Invoke();
        return true;
    }

    public bool TryCheckIn()
    {
        if (Save == null)
            return false;

        string today = EconomyClock.TodayKey();
        if (Save.lastAttendanceDate == today)
            return false;

        Save.lastAttendanceDate = today;
        Save.attendanceDay = Save.attendanceDay % 7 + 1;

        if (Wallet.Instance != null)
        {
            if (Save.attendanceDay < 7)
            {
                Wallet.Instance.AddCoins(50 + Save.attendanceDay * 20, false);
                if (Save.attendanceDay % 2 == 0)
                    Wallet.Instance.AddAlloy(1);
            }
            else
            {
                Wallet.Instance.AddCoins(200, false);
                Wallet.Instance.AddAlloy(4);
                Wallet.Instance.AddDeckShards(3);
            }
        }

        Dirty();
        OnChanged?.Invoke();
        return true;
    }

    private void Dirty()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.MarkDirty();
    }

    private static int Hash(string s)
    {
        unchecked
        {
            int h = 23;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h;
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}
