#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Batch / one-click: URP + materials + Kenney CC0 import + validate.
/// Also watches Temp/347-*.request so an already-open Editor can be driven from the shell.
public static class Project347BootstrapBatch
{
    private const string BootstrapRequest = "Temp/347-bootstrap.request";
    private const string PlayRequest = "Temp/347-play.request";
    private const string DoneMarker = "Temp/347-bootstrap.done";
    private const string FailMarker = "Temp/347-bootstrap.fail";

    [InitializeOnLoadMethod]
    private static void WatchRequests()
    {
        EditorApplication.delayCall += ProcessPendingRequests;
    }

    private static void ProcessPendingRequests()
    {
        try
        {
            if (File.Exists(BootstrapRequest))
            {
                File.Delete(BootstrapRequest);
                if (File.Exists(DoneMarker))
                    File.Delete(DoneMarker);
                if (File.Exists(FailMarker))
                    File.Delete(FailMarker);

                BootstrapAll();
                File.WriteAllText(DoneMarker, System.DateTime.Now.ToString("o"));
            }

            if (File.Exists(PlayRequest))
            {
                File.Delete(PlayRequest);
                EnterPlayMode.PlayRunner();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("347 request watcher failed: " + ex);
            try
            {
                File.WriteAllText(FailMarker, ex.ToString());
            }
            catch
            {
                // ignore
            }
        }
    }

    [MenuItem("Tools/Archive A-0347/Bootstrap All (Visual + Free Assets)")]
    public static void BootstrapAll()
    {
        Debug.Log("347 Bootstrap: Setup Visual Pipeline…");
        Project347Setup.SetupVisualPipeline();

        Debug.Log("347 Bootstrap: Import Free CC0 Assets…");
        FreeAssetImporter.ImportAll();

        Debug.Log("347 Bootstrap: Validate Art…");
        ArtValidateTools.ValidateArt(showDialog: false);

        PlayTestTools.CreateRunSceneIfMissing();
        if (EditorSceneManager.GetActiveScene().path != PlayTestTools.RunScenePath)
            EditorSceneManager.OpenScene(PlayTestTools.RunScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("347 Bootstrap: done.");
    }

    /// Unity -batchmode -quit -executeMethod Project347BootstrapBatch.RunBatch
    public static void RunBatch()
    {
        try
        {
            BootstrapAll();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("347 Bootstrap failed: " + ex);
            EditorApplication.Exit(1);
        }
    }
}
#endif
