using UnityEngine;

namespace CoastRun
{
    /// Loads Coast Run ScriptableObjects from Resources/CoastRun/Config.
    public static class CoastConfigRegistry
    {
        private const string Root = "CoastRun/Config/";

        public static RunConfig RunConfig =>
            Load<RunConfig>("RunConfig") ?? CreateFallback<RunConfig>("RunConfig (runtime)");

        public static UpgradeConfig UpgradeConfig =>
            Load<UpgradeConfig>("UpgradeConfig") ?? CreateFallback<UpgradeConfig>("UpgradeConfig (runtime)");

        public static StoryConfig StoryConfig =>
            Load<StoryConfig>("StoryConfig") ?? CreateFallback<StoryConfig>("StoryConfig (runtime)");

        public static CoastPaletteConfig CoastPaletteConfig =>
            Load<CoastPaletteConfig>("CoastPalette") ?? CreateFallback<CoastPaletteConfig>("CoastPalette (runtime)");

        public static StageTable StageTable
        {
            get
            {
                var loaded = Load<StageTable>("StageTable");
                if (loaded != null)
                {
                    loaded.EnsurePopulated();
                    return loaded;
                }

                return StageTable.CreateDefault();
            }
        }

        public static CutsceneTable CutsceneTable
        {
            get
            {
                var loaded = Load<CutsceneTable>("CutsceneTable");
                if (loaded != null)
                {
                    loaded.EnsurePopulated();
                    return loaded;
                }

                return CutsceneTable.CreateDefault();
            }
        }

        private static T Load<T>(string name) where T : ScriptableObject =>
            Resources.Load<T>(Root + name);

        private static T CreateFallback<T>(string label) where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            so.name = label;
            return so;
        }
    }
}
