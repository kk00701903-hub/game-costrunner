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
            go.transform.localScale = Vector3.one * scale * UnitFix(name, go);
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

        private static readonly Dictionary<string, float> UnitFixes = new Dictionary<string, float>();

        /// Blender's FBX arrives in centimetres on some importer settings; a 6 m house
        /// then lands as a 600 m wall. Measure once per model and bring anything that
        /// is off by a hundred back to metres (kit pieces are all under 12 m tall).
        private static float UnitFix(string name, GameObject instance)
        {
            if (UnitFixes.TryGetValue(name, out float f))
                return f;
            var rs = instance.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(instance.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in rs)
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            float h = b.size.y;
            f = h > 25f ? 0.01f : 1f;
            if (h < 0.001f) f = 1f;
            UnitFixes[name] = f;
            Vector3 lo = instance.transform.InverseTransformPoint(b.min);
            Vector3 hi = instance.transform.InverseTransformPoint(b.max);
            Debug.Log($"[JejuKit] {name} size {b.size} local min {lo} max {hi} → unit fix {f}");
#if UNITY_EDITOR
            System.IO.File.AppendAllText(System.IO.Path.Combine(Application.dataPath, "..", "Tools", "kit_log.txt"),
                $"{name} size {b.size} local min {lo} max {hi} fix {f}\n");
#endif
            return f;
        }

        public static GameObject SpawnBuilding(int variant, Transform parent, Vector3 localPos, float yawDegrees)
        {
            int n = BuildingCount;
            if (n == 0) return null;
            variant = ((variant % n) + n) % n;
            // 14차-3: 목표 이미지의 2~3층 상가 벽. 키트의 낮은 집(C~H, 4~4.8 m)은 1.3배로 키워
            // 옆집과 거의 붙은 연속 파사드가 되게 한다. A/B(7~10 m 블록)는 그대로.
            float scale = variant < 2 ? 1.05f : 1.5f;   // 14차-6: 2~3층 높이(7 m 안팎)로
            var b = Spawn("Bldg_" + (char)('A' + variant), parent, localPos, yawDegrees, scale);
            // 계절 색조 + 건물별 미세 변주(위치 해시): 밝기 ±6%, 따뜻/차가운 쪽으로 ±4%
            float h = Mathf.Abs(Mathf.Sin(localPos.z * 12.9898f + variant * 78.233f + (parent != null ? parent.position.z * 0.37f : 0f)) * 43758.5453f) % 1f;
            float h2 = Mathf.Abs(Mathf.Sin(h * 91.7f + 3.1f) * 24634.63f) % 1f;
            float br = 0.96f + h * 0.08f;
            var extra = new Color(br * (1f + (h2 - 0.5f) * 0.06f), br, br * (1f - (h2 - 0.5f) * 0.06f), 1f);
            SeasonLook.Tint(b, 1f, extra);
            // 14차-8: 목표 이미지의 '벽 3색 규칙' — 민트 / 베이지 / 블루가 순서대로 돌아 거리에 리듬이 생긴다.
            // 파사드 그림은 그대로 두고 옆벽·뒷벽(Wall 재질)만 물들인다.
            WallColorRule(b, parent);
            return b;
        }

        private static readonly Color[] WallRule =
        {
            new Color(0.74f, 0.92f, 0.86f),   // 민트
            new Color(0.97f, 0.91f, 0.78f),   // 베이지
            new Color(0.72f, 0.83f, 0.97f),   // 블루
        };
        private static MaterialPropertyBlock _wallMpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private static void WallColorRule(GameObject b, Transform parent)
        {
            if (b == null) return;
            float z = parent != null ? DownhillPath.DistanceAlong(parent.position) : 0f;
            int ix = Mathf.Abs(Mathf.FloorToInt(z / 10f)) % WallRule.Length;
            Color c = WallRule[ix];
            _wallMpb ??= new MaterialPropertyBlock();
            foreach (var r in b.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                string n = m.name;
                // 키트 건물은 재질 하나(Facade_X)에 옆벽까지 들어 있다 → 파사드 그림엔 색을 옅게(55%), 나머지엔 진하게.
                bool facade = n.StartsWith("Facade");
                if (!(facade || n.StartsWith("Wall") || n.StartsWith("Concrete"))) continue;
                Color cc = facade ? Color.Lerp(Color.white, c, 0.55f) : c;
                r.GetPropertyBlock(_wallMpb);
                Color prev = _wallMpb.GetColor(_baseColorId);
                if (prev == default) prev = Color.white;
                _wallMpb.SetColor(_baseColorId, prev * cc);
                _wallMpb.SetColor(_colorId, prev * cc);
                r.SetPropertyBlock(_wallMpb);
            }
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
            if (m != null) m.name = name;   // 14차-8: WallColorRule 이 이름으로 벽 재질을 고른다
            Mats[name] = m;
            return m;
        }

        private static Material RoofMat(string tex, System.Func<Color> fallback)
        {
            var t = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + tex);
            if (t == null) return CoastMaterials.CreateLit(fallback);
            var m = ArtAssets.CreateTexturedLit(t, Color.white, 0.05f);
            // 지붕 UV는 미터 단위 → 타일 1장 = 1.5m
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", new Vector2(0.66f, 0.66f));
            else m.mainTextureScale = new Vector2(0.66f, 0.66f);
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
                // 6차: 옆벽·지붕도 그려진 텍스처(Firefly). 없으면 예전 단색/스투코.
                case "Wall":
                {
                    var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Wall_Side")
                              ?? Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Wall_Stucco");
                    return tex != null ? ArtAssets.CreateTexturedLit(tex, Color.white, 0.03f)
                                       : CoastMaterials.CreateLit(() => CoastPalette.TownCream);
                }
                case "WallCool":
                {
                    var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Wall_Cool");
                    return tex != null ? ArtAssets.CreateTexturedLit(tex, Color.white, 0.03f)
                                       : CoastMaterials.CreateLit(() => CoastPalette.BuildingCool);
                }
                case "Roof_Terracotta": return RoofMat("Tex_Roof_Terracotta", () => CoastPalette.Roof);
                case "Roof_Slate": return RoofMat("Tex_Roof_Slate", () => Color.Lerp(CoastPalette.SkyBlue, CoastPalette.RoadGrey, 0.55f));
                case "Roof_Basalt": return RoofMat("Tex_Roof_Basalt", () => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.55f));
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
                // Player skateboard (Prop_Skateboard): mint deck, darker grip top, orange wheels.
                case "Deck": return CoastMaterials.CreateToon(new Color(0.45f, 0.82f, 0.74f));
                case "Grip": return CoastMaterials.CreateToon(new Color(0.30f, 0.62f, 0.56f));
                case "Wheel": return CoastMaterials.CreateToon(CoastPalette.WheelOrange, () => CoastPalette.WheelOrange, null, 0.3f);
                case "Concrete": return CoastMaterials.CreateLit(() => CoastPalette.Sidewalk);
                default: return CoastMaterials.CreateLit(() => CoastPalette.TownCream);
            }
        }
    }
}
