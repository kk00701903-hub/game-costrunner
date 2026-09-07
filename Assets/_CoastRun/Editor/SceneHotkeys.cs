using UnityEditor;
using UnityEditor.SceneManagement;

namespace CoastRun.EditorTools
{
    /// 14차-8: 씬 바로 열기 단축키(자동 검증용). Ctrl+Shift+Alt+1/2/5.
    public static class SceneHotkeys
    {
        private static void Open(string name)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/_CoastRun/Scenes/" + name + ".unity");
        }

        [MenuItem("Coast Run/Scenes/Open 01_Title #F1")] public static void OpenTitle() => Open("01_Title");
        [MenuItem("Coast Run/Scenes/Open 02_Run #F2")] public static void OpenRun() => Open("02_Run");
        [MenuItem("Coast Run/Scenes/Open 05_Raising #F5")] public static void OpenRaising() => Open("05_Raising");
        [MenuItem("Coast Run/Scenes/Open 00_Boot #F9")] public static void OpenBoot() => Open("00_Boot");
    }
}
