using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnterPlayMode
{
    [InitializeOnLoadMethod]
    private static void BootFromCommandLine()
    {
        if (!HasArg("-347Play") && !HasArg("-347Arena") && !HasArg("-CoastPlay"))
            return;

        EditorApplication.delayCall += () =>
        {
            if (HasArg("-CoastPlay"))
            {
                CoastRunMenu.OpenRunScene();
                CoastRunMenu.PortraitGameView();
                EditorApplication.isPlaying = true;
                return;
            }

            PlayTestTools.OpenRunScene();
            PlayTestTools.PortraitGameView();
            if (HasArg("-347Play"))
                StartRunnerPlay();
            else if (HasArg("-347Arena"))
                StartArenaPlay();
        };
    }

    private static bool HasArg(string flag)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == flag)
                return true;
        }

        return false;
    }

    public static void Run()
    {
        Debug.Log("EnterPlayMode: starting Play.");
        PlayTestTools.OpenRunScene();
        GameBootstrap.PendingMode = BootMode.Runner;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Archive A-0347/Play Runner %#r")]
    public static void PlayRunner()
    {
        PlayTestTools.OpenRunScene();
        EditorApplication.delayCall += StartRunnerPlay;
    }

    private static void StartRunnerPlay()
    {
        try
        {
            PlayTestTools.PortraitGameView();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("347: Game View portrait skipped — " + ex.Message);
        }

        GameBootstrap.PendingMode = BootMode.Runner;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Archive A-0347/Play King Arena %#k")]
    public static void PlayKingArena()
    {
        PlayTestTools.OpenRunScene();
        EditorApplication.delayCall += StartArenaPlay;
    }

    private static void StartArenaPlay()
    {
        try
        {
            PlayTestTools.PortraitGameView();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("347: Game View portrait skipped — " + ex.Message);
        }

        GameBootstrap.PendingMode = BootMode.KingArena;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Archive A-0347/Toggle Numeric HP")]
    public static void ToggleNumericHp()
    {
        UIManager.NumericHp = !UIManager.NumericHp;
        Debug.Log("Numeric HP HUD: " + UIManager.NumericHp);
    }

    [MenuItem("Tools/Archive A-0347/Toggle Slow Telegraphs")]
    public static void ToggleSlowTelegraphs()
    {
        bool slow = KingFight.TelegraphScale > 1f;
        KingFight.TelegraphScale = slow ? 1f : 1.5f;
        Debug.Log("Telegraph scale: " + KingFight.TelegraphScale);
    }

    [MenuItem("Tools/Archive A-0347/Reset Save Data")]
    public static void ResetSave()
    {
        FlagStore.ClearAll();
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.ResetAll();
        else
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        Debug.Log("347: save data cleared.");
    }
}
