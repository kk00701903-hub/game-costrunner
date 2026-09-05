using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Blender-built Jeju street kit (Tools/blender/jeju_kit.py → Resources/CoastRun/Models).
    /// Meshes carry only material *names*; this maps them to the game's toon materials
    /// and the Firefly paintings: Facade_X → Tex_Facade_X, Stone → Tex_Stonewall_Jeju,
    /// Roof_* / Awning_* / Wood … → palette colours. Everything is cached per name so a
    /// street of 30 houses shares a handful of materials.
    public static class JejuKit
    {
        public const string ModelRoot = ArtAssets.ResourceRoot + "Models/";

        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        private static int _buildingCount = -1;

        /// Number of Bldg_A.. models present (0 → the procedural boxes stay).
        public static int BuildingCount
        {
            get
            {
                if (_buildingCount < 0)
                {
                    _buildingCount = 0;
                    for (char c = 'A'; c <= 'L'; c++)
                    {
                        if (Load("Bldg_" + c) == null) break;
                        _buildingCount++;
                    }
                }
                return _buildingCount;
            }
        }

        public static GameObject Load(string name)
        {
            if (Prefabs.TryGetValue(name, out var p))
                return p;
            p = Resources.Load<GameObject>(ModelRoot + name);
            Prefabs[name] = p;
            return p;
        }

        /// Instantiates a kit piece with game materials applied. Returns null when the
        /// model is missing so callers can fall back to procedural geometry.
        public static GameObject Spawn(string name, Transform parent, Vector3 localPos, float yawDegrees = 0f, float scale = 1f)
        {
            var prefab = Load(name);
            if (prefab == null)
                return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                var mapped = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                    mapped[i] = MaterialFor(shared[i] != null ? shared[i].name : "Wall");
                r.sharedMaterials = mapped;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            return go;
        }

        public static GameObject SpawnBuilding(int variant, Transform parent, Vector3 localPos, float yawDegrees)
        {
            int n = BuildingCount;
            if (n == 0) return null;
            variant = ((variant % n) + n) % n;
            return Spawn("Bldg_" + (char)('A' + variant), parent, localPos, yawDegrees);
        }

        private static Material MaterialFor(string rawName)
        {
            // Unity appends " (Instance)" / import suffixes; keep the leading token.
            string name = rawName;
            int cut = name.IndexOf(' ');
            if (cut > 0) name = name.Substring(0, cut);
            cut = name.IndexOf('.');
            if (cut > 0) name = name.Substring(0, cut);

            if (Mats.TryGetValue(name, out var m) && m != null)
                return m;
            m = Build(name);
            Mats[name] = m;
            return m;
        }

        private static Material Build(string name)
        {
            if (name.StartsWith("Facade_"))
            {
                var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_" + name);
                if (tex != null)
                    return ArtAssets.CreateTexturedLit(tex, Color.white, 0.05f);
                return CoastMaterials.CreateLit(() => CoastPalette.TownCream);
            }
            switch (name)
            {
                case "Wall":
                {
                    var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Wall_Stucco");
                    return tex != null ? ArtAssets.CreateTexturedLit(tex, Color.white, 0.03f)
                                       : CoastMaterials.CreateLit(() => CoastPalette.TownCream);
                }
                case "WallCool": return CoastMaterials.CreateLit(() => CoastPalette.BuildingCool);
                case "Roof_Terracotta": return CoastMaterials.CreateLit(() => CoastPalette.Roof);
                case "Roof_Slate": return CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.SkyBlue, CoastPalette.RoadGrey, 0.55f));
                case "Roof_Basalt": return CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.55f));
                case "Stone":
                {
                    var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Stonewall_Jeju");
                    return tex != null ? ArtAssets.CreateTexturedLit(tex, Color.white, 0.02f)
                                       : CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.5f));
                }
                case "Wood": return CoastMaterials.CreateLit(new Color(0.55f, 0.38f, 0.22f));
                case "Awning_Red": return CoastMaterials.CreateLit(new Color(0.85f, 0.30f, 0.28f));
                case "Awning_Blue": return CoastMaterials.CreateLit(new Color(0.30f, 0.50f, 0.80f));
                case "Awning_Orange": return CoastMaterials.CreateLit(() => CoastPalette.AccentOrange);
                case "Glass": return CoastMaterials.CreateLit(() => CoastPalette.Window, 0.6f);
                case "Trunk": return CoastMaterials.CreateLit(new Color(0.40f, 0.28f, 0.18f));
                case "Leaf": return CoastMaterials.CreateLit(new Color(0.25f, 0.55f, 0.28f));
                case "Orange": return CoastMaterials.CreateLit(new Color(0.98f, 0.60f, 0.15f), 0.3f);
                case "Sign": return CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.TownCream, Color.white, 0.6f));
                case "Metal": return CoastMaterials.CreateLit(() => CoastPalette.Pole, 0.3f);
                case "Concrete": return CoastMaterials.CreateLit(() => CoastPalette.Sidewalk);
                default: return CoastMaterials.CreateLit(() => CoastPalette.TownCream);
            }
        }
    }
}
