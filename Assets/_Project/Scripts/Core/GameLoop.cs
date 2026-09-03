using UnityEngine;
using UnityEngine.SceneManagement;

/// Boot → Meta → Run. Empty scenes still work because GameBootstrap auto-runs
/// inside Run; Boot just forwards so the three-scene layout is real.
public class GameLoop : MonoBehaviour
{
    public const string BootScene = "Boot";
    public const string MetaScene = "Meta";
    public const string RunScene = "Run";

    public static GameLoop Instance { get; private set; }

    [SerializeField] private string firstScene = MetaScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ServiceLocator.Register(this);
        GameLog.Verbose("GameLoop ready. Config=" + GameConfig.Active.name);
    }

    private void Start()
    {
        string active = SceneManager.GetActiveScene().name;
        if (active == BootScene || string.IsNullOrEmpty(active) || active == "Untitled")
            GoMeta();
    }

    public void GoMeta()
    {
        LoadIfPresent(MetaScene, fallbackRun: false);
    }

    public void GoRun(BootMode mode = BootMode.Runner)
    {
        GameBootstrap.PendingMode = mode;
        LoadIfPresent(RunScene, fallbackRun: true);
    }

    private static void LoadIfPresent(string scene, bool fallbackRun)
    {
        if (Application.CanStreamedLevelBeLoaded(scene))
        {
            SceneManager.LoadScene(scene);
            return;
        }

        // Scenes are optional until Tools/347/Create Boot Meta Run Scenes runs.
        // Empty-scene Play still boots through GameBootstrap.
        GameLog.Warn("Scene '" + scene + "' missing — staying in current scene.");
        if (fallbackRun && Object.FindObjectOfType<GameBootstrap>() == null)
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        if (Instance == this)
            Instance = null;
    }
}
