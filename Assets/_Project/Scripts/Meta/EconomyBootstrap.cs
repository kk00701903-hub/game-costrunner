using UnityEngine;

/// Wires meta systems once per session and applies the 30-minute front-load grants.
public class EconomyBootstrap : MonoBehaviour
{
    public static EconomyBootstrap Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path.IndexOf("_CoastRun", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        if (Instance != null)
            return;

        GameObject go = new GameObject("Economy");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveSystem>();
        go.AddComponent<Wallet>();
        go.AddComponent<UpgradeSystem>();
        go.AddComponent<Vendor>();
        go.AddComponent<Codex>();
        go.AddComponent<MissionSystem>();
        go.AddComponent<AdRewardService>();
        go.AddComponent<PrestigeVisuals>();
        go.AddComponent<EconomyBootstrap>();
        go.AddComponent<TutorialDirector>();
        go.AddComponent<PrerunnerTrigger>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DestroyIfCoastRun()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path.IndexOf("_CoastRun", System.StringComparison.OrdinalIgnoreCase) < 0)
            return;

        var economy = GameObject.Find("Economy");
        if (economy != null)
            Object.Destroy(economy);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.ResetDailyIfNeeded();
        if (MissionSystem.Instance != null)
            MissionSystem.Instance.RefreshIfNeeded();
    }

    public static void GrantTutorialEnd()
    {
        SaveData save = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
        if (save == null || save.tutorialGrantDone || Wallet.Instance == null)
            return;

        save.tutorialGrantDone = true;
        Wallet.Instance.AddCoins(400, false);
        SaveSystem.Instance.MarkDirty();
        GameLog.Verbose("Economy: tutorial grant 400 coins.");
    }

    public static void GrantFirstKing()
    {
        SaveData save = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
        if (save == null || save.firstKingDefeated || Wallet.Instance == null)
            return;

        save.firstKingDefeated = true;
        Wallet.Instance.AddCoins(1000, false);
        Wallet.Instance.AddAlloy(10);
        Wallet.Instance.AddDeckShards(5);
        save.freeTenPullTickets++;
        SaveSystem.Instance.MarkDirty();
        GameLog.Verbose("Economy: first king grant.");
    }

    public static void GrantKingDaily(int zoneIndex)
    {
        if (Wallet.Instance == null || SaveSystem.Instance == null)
            return;

        string key = "king_daily_" + EconomyClock.TodayKey() + "_" + zoneIndex;
        SaveData save = SaveSystem.Instance.Data;
        if (save.flags.Contains(key))
            return;

        save.flags.Add(key);
        Wallet.Instance.AddCoins(200, false);
        Wallet.Instance.AddAlloy(3);
        Wallet.Instance.AddDeckShards(1);
        SaveSystem.Instance.MarkDirty();
    }

    public static void GrantFirstDeath()
    {
        SaveData save = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
        if (save == null || save.firstDeathGrantDone || Wallet.Instance == null)
            return;

        save.firstDeathGrantDone = true;
        Wallet.Instance.AddCoins(200, false);
        SaveSystem.Instance.MarkDirty();
    }

    public static void NoteRunFinished(float distance, int runCoins)
    {
        if (SaveSystem.Instance == null)
            return;

        SaveData save = SaveSystem.Instance.Data;
        save.dailyRunsToday++;
        save.totalDistanceMetres += Mathf.Max(0, Mathf.FloorToInt(distance));
        SaveSystem.Instance.MarkDirty();

        if (Wallet.Instance != null && Wallet.Instance.IsSoftCapped && UIManager.Instance != null)
        {
            UIManager.Instance.ShowSubtitle(
                Speaker.Sweeper,
                "저쪽이 오늘 너 패턴 다 외웠어. 내일 다시 와.",
                3.5f,
                false);
        }
    }
}
