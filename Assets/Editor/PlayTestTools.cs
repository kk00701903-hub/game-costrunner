using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// One-click test entry: open Run scene, portrait game view, play runner/arena.
public static class PlayTestTools
{
    public const string RunScenePath = "Assets/_Project/Scenes/Run.unity";

    [InitializeOnLoadMethod]
    private static void EnsureRunSceneOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RunScenePath))
                CreateRunScene();
        };
    }

    [MenuItem("Tools/Archive A-0347/Open Run Test Scene %#o")]
    public static void OpenRunScene()
    {
        CreateRunSceneIfMissing();
        if (EditorSceneManager.GetActiveScene().path != RunScenePath)
            EditorSceneManager.OpenScene(RunScenePath);

        Debug.Log(
            "347: Run scene open.\n" +
            "→ Press Play, or Ctrl+Shift+R (Play Runner).\n" +
            "→ Game 탭에서 확인 (Scene 탭 말고).\n" +
            "→ 회색만 보이면 Tools > 347 > Fix Render Pipeline (URP) 후 다시 Play.");
    }

    [MenuItem("Tools/Archive A-0347/Galaxy S26 Game View (1080x2340)")]
    public static void PortraitGameView()
    {
        GameViewPortrait.Set(MobileDisplay.Width, MobileDisplay.Height);
        Debug.Log("347: Game View → " + MobileDisplay.DeviceName + " " + MobileDisplay.Width + "×" + MobileDisplay.Height + " (19.5:9).");
    }

    [MenuItem("Tools/Archive A-0347/Portrait Game View (1080x1920)")]
    public static void LegacyPortraitGameView()
    {
        GameViewPortrait.Set(1080, 1920);
        Debug.Log("347: Game View set to legacy 1080×1920 (9:16).");
    }

    [MenuItem("Tools/Archive A-0347/Rebuild UI (Play Mode)")]
    public static void RebuildUiInPlayMode()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("347: Enter Play Mode first, then run Rebuild UI.");
            return;
        }

        GameBootstrap.RebuildRunUi();
    }

    [MenuItem("Tools/Archive A-0347/Verify UI Art Loaded")]
    public static void VerifyUiArt()
    {
        (string folder, string file)[] required =
        {
            ("Concept", "Concept_Opening"),
            ("Concept", "Concept_Depot"),
            ("UI", "UI_Panel_City"),
            ("UI", "UI_Btn_Primary"),
            ("UI", "UI_Frame_Item"),
            ("UI", "UI_Icon_Tag"),
            ("UI", "UI_Icon_Letter"),
            ("UI", "UI_Icon_Coin"),
            ("UI", "UI_Icon_Depot"),
            ("UI", "UI_Deck_Ok"),
            ("UI", "UI_Deck_Cracked"),
            ("UI", "UI_Deck_Broken")
        };

        int ok = 0;
        foreach (var pair in required)
        {
            string path = pair.folder + "/" + pair.file;
            Object asset = Resources.Load<Texture2D>(path) ?? Resources.Load<Object>(path);
            if (asset != null)
            {
                ok++;
                Debug.Log("OK  " + path);
            }
            else
                Debug.LogWarning("MISSING  " + path);
        }

        Debug.Log("347 UI verify: " + ok + " / " + required.Length + " (UiArt uses legacy fallbacks when missing).");
    }

    public static void CreateRunSceneIfMissing()
    {
        if (!File.Exists(RunScenePath))
            CreateRunScene();
    }

    private static void CreateRunScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RunScenePath) ?? "Assets/_Project/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, RunScenePath);

        var scenes = new[]
        {
            new EditorBuildSettingsScene(RunScenePath, true)
        };
        EditorBuildSettings.scenes = scenes;
        AssetDatabase.Refresh();
        Debug.Log("347: created " + RunScenePath);
    }
}

/// Sets Game View to a custom portrait resolution for HUD testing.
internal static class GameViewPortrait
{
    public static void Set(int width, int height)
    {
        var assembly = typeof(EditorWindow).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
            return;

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        gameView.Show();

        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var singletonType = assembly.GetType("UnityEditor.ScriptableSingleton`1");
        if (sizesType == null || singletonType == null)
            return;

        var singleton = singletonType.MakeGenericType(sizesType);
        var instanceProp = singleton.GetProperty("instance");
        object sizesInstance = instanceProp?.GetValue(null);
        if (sizesInstance == null)
            return;

        var getGroup = sizesType.GetMethod("GetGroup");
        if (getGroup == null)
            return;

        // GameViewSizeGroupType.Standalone = 0
        object group = getGroup.Invoke(sizesInstance, new object[] { 0 });
        if (group == null)
            return;

        var groupType = group.GetType();
        var getBuiltin = groupType.GetMethod("GetBuiltinCount");
        var getCustom = groupType.GetMethod("GetCustomCount");

        if (getBuiltin == null || getCustom == null)
            return;

        string label = width + "x" + height + " Portrait";
        if (!TryAddCustomSize(assembly, group, groupType, width, height, label))
            return;

        int total = (int)getBuiltin.Invoke(group, null) + (int)getCustom.Invoke(group, null) - 1;
        var setSize = gameViewType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (setSize == null)
            return;

        try
        {
            if (setSize.GetParameters().Length == 2)
                setSize.Invoke(gameView, new object[] { total, null });
            else
                setSize.Invoke(gameView, new object[] { total });
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("347: Game View size select skipped — " + ex.Message);
        }
    }

    private static bool TryAddCustomSize(System.Reflection.Assembly assembly, object group, System.Type groupType, int width, int height, string label)
    {
        var addCustomMethods = groupType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        System.Type sizeType = assembly.GetType("UnityEditor.GameViewSizeType");
        object enumFixed = sizeType != null ? System.Enum.ToObject(sizeType, 1) : null;

        for (int i = 0; i < addCustomMethods.Length; i++)
        {
            if (addCustomMethods[i].Name != "AddCustomSize")
                continue;

            var p = addCustomMethods[i].GetParameters();
            try
            {
                if (p.Length == 4 && sizeType != null && p[0].ParameterType == sizeType)
                    addCustomMethods[i].Invoke(group, new object[] { enumFixed, width, height, label });
                else if (p.Length == 4 && p[0].ParameterType == typeof(int))
                    addCustomMethods[i].Invoke(group, new object[] { 1, width, height, label });
                else
                    continue;

                return true;
            }
            catch (System.Exception)
            {
                // try next overload
            }
        }

        return false;
    }
}
