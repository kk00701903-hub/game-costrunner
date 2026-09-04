namespace CoastRun
{
    /// The five scenes of the flow, in one place.
    ///
    /// Two stale constants used to live on the bootstrap components —
    /// MainMenuBootstrap.ScenePath pointed at MainMenu.unity and
    /// CoastRunBootstrap.ScenePath at Run.unity. Both scenes were removed when the
    /// project moved to the numbered flow, and the editor menus that still read them
    /// would happily recreate the deleted scene and insert it at build index 0,
    /// displacing 00_Boot as the entry point.
    public static class CoastScenes
    {
        public const string Dir = "Assets/_CoastRun/Scenes";

        public const string Boot = "00_Boot";
        public const string Title = "01_Title";
        public const string Run = "02_Run";
        public const string Cutscene = "03_Cutscene";
        public const string Ending = "04_Ending";

        public static string Path(string sceneName) => $"{Dir}/{sceneName}.unity";

        public static readonly string[] BuildOrder = { Boot, Title, Run, Cutscene, Ending };
    }
}
