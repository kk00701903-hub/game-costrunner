using UnityEngine;

namespace CoastRun
{
    /// Loads canonical scene stills from StyleBible reference frames (style_frame_1~5).
    public static class CoastSceneArt
    {
        public const string SceneRoot = "CoastRun/Scene/";

        /// style_frame_1 — steep downhill start
        public const string Frame1 = "Scene_Frame_1";
        /// style_frame_2 — promenade cruise wide view
        public const string Frame2 = "Scene_Frame_2";
        /// style_frame_3 — busy promenade with NPCs
        public const string Frame3 = "Scene_Frame_3";
        /// style_frame_4 — town + sea panorama
        public const string Frame4 = "Scene_Frame_4";
        /// style_frame_5 — main menu / gameplay tap composition
        public const string Frame5 = "Scene_Frame_5";

        public static Texture2D LoadFrame(int index)
        {
            if (index < 1 || index > 5)
                return null;
            return Load($"Scene_Frame_{index}");
        }

        public static Texture2D Load(string sceneName)
        {
            return Resources.Load<Texture2D>(SceneRoot + sceneName);
        }

        public static Sprite AsSprite(string sceneName, float ppu = 100f)
        {
            var tex = Load(sceneName);
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        }

        public static Sprite FrameSprite(int index, float ppu = 100f) => AsSprite($"Scene_Frame_{index}", ppu);

        /// Default prologue beat → reference frame mapping.
        public static string PrologueSceneForBeat(int beatIndex)
        {
            switch (beatIndex)
            {
                case 0: return Frame4; // 약속 — distant tower / town view
                case 1: return Frame3; // 장애 — crowded coastal street
                case 2: return Frame1; // 결심 — downhill push-off
                case 3: return Frame5; // 전환 — chase composition
                default: return Frame2;
            }
        }
    }
}
