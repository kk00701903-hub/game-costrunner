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

        /// 14차-13: 파트별 상가 키트(Shop_A~F) 개수. 있으면 Bldg_ 대신 쓴다.
        private static int _shopCount = -1;
        public static int ShopCount
        {
            get
            {
                if (_shopCount < 0)
                {
                    _shopCount = 0;
                    for (char c = 'A'; c <= 'H'; c++) { if (Load("Shop_" + c) == null) break; _shopCount++; }
                }
                return _shopCount;
            }
        }

        public static GameObject SpawnBuilding(int variant, Transform parent, Vector3 localPos, float yawDegrees)
        {
            if (ShopCount > 0)
            {
                int sn = ShopCount;
                int sv = ((variant % sn) + sn) % sn;
                var sb = Spawn("Shop_" + (char)('A' + sv), parent, localPos, yawDegrees, 1f);
                WallColorRule(sb, parent);
                BuildingOutline.Attach(sb.transform, 0.035f);   // 14차-14: 만화 윤곽 3.5 cm
                return sb;
            }
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
            // 14차-11: 건물 잉크 테두리(3 cm) — 파사드·처마·옆벽이 한 덩어리 입체로 또렷하게 떨어진다.
            BuildingOutline.Attach(b.transform, 0.03f);
            return b;
        }

        // 14차-12: 팔레트 5+1 고정(파스텔 S 60~75% · V 95~100%). 베이스 70%(크림·스카이), 포인트 30%.
        // 건물 하나 = 벽 파스텔 1색 + 같은 계열의 진한 색 1개(차양·간판·지붕). 그 이상 쓰지 않는다.
        public static readonly Color Cream = Hex("#FFF5E1"), Sky = Hex("#A0D2EB"),
            Pink = Hex("#FF8E9E"), Mint = Hex("#A8E6CF"), Lemon = Hex("#FFEB7A"), Coral = Hex("#FF8A65");
        private static readonly Color[] Base = { Cream, Sky, Cream, Sky, Cream, Cream, Sky };   // 7 중 7 = 베이스(70%)
        private static readonly Color[] Point = { Pink, Mint, Lemon, Coral };
        /// 마지막으로 스폰한 건물의 벽색 / 포인트(진한 같은 계열) — 차양·간판·화분이 같은 계열을 쓴다.
        public static Color LastWall { get; private set; } = Cream;
        public static Color LastAccent { get; private set; } = Hex("#E0956B");
        public static int LastFamily { get; private set; }

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        /// 같은 계열의 '진한 색': 채도 +0.28, 밝기 −0.18 (크림은 코랄 계열, 스카이는 진한 하늘색).
        public static Color Accent(Color wall)
        {
            if (wall == Cream) return Hex("#F2A56C");
            Color.RGBToHSV(wall, out float h, out float sv, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(sv + 0.28f), Mathf.Clamp01(v - 0.18f));
        }

        private static MaterialPropertyBlock _wallMpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private static void WallColorRule(GameObject b, Transform parent)
        {
            if (b == null) return;
            float z = parent != null ? DownhillPath.DistanceAlong(parent.position) : 0f;
            int lot = Mathf.Abs(Mathf.FloorToInt(z / 10f));
            // 10집 중 7집 베이스, 3집 포인트 — 순서는 해시로 섞되 같은 색이 연달아 서지 않게
            int h = Mathf.Abs((lot * 7919 + 13) % 10);
            Color wall = h < 7 ? Base[h] : Point[(lot * 31 + h) % Point.Length];
            LastWall = wall; LastAccent = Accent(wall); LastFamily = lot;
            Color roof = LastAccent;
            _wallMpb ??= new MaterialPropertyBlock();
            foreach (var r in b.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                string n = m.name;
                Color cc;
                if (n.StartsWith("Facade") || n.StartsWith("Wall")) cc = wall;
                else if (n.StartsWith("Roof")) cc = roof;
                else if (n == "AwningA" || n.StartsWith("Awning_") || n == "Frame") cc = LastAccent;
                else if (n == "Door") cc = LastAccent * new Color(0.82f, 0.78f, 0.78f, 1f);
                else if (n == "Trim" || n == "AwningB") cc = Color.white;
                else continue;
                r.GetPropertyBlock(_wallMpb);
                _wallMpb.SetColor(_baseColorId, cc);
                _wallMpb.SetColor(_colorId, cc);
                r.SetPropertyBlock(_wallMpb);
            }
        }

        /// 14차-14: 특정 재질(이름 접두)만 색 바꾸기 — 집 지붕색, 카페 의자색 등.
        public static void Recolor(GameObject go, string matPrefix, Color c)
        {
            if (go == null) return;
            _wallMpb ??= new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial; if (m == null || !m.name.StartsWith(matPrefix)) continue;
                r.GetPropertyBlock(_wallMpb); _wallMpb.SetColor(_baseColorId, c); _wallMpb.SetColor(_colorId, c); r.SetPropertyBlock(_wallMpb);
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

        /// 14차-13: 파스텔용 라이팅 — 그늘 면이 탁한 남색으로 죽지 않게 그림자 틴트를 밝은 라벤더로, 문턱을 낮춘다.
        private static Material PastelLit()
        {
            var m = CoastMaterials.CreateLit(Color.white, 0.05f);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", new Color(0.80f, 0.80f, 0.90f, 1f));
            if (m.HasProperty("_ShadowThreshold")) m.SetFloat("_ShadowThreshold", 0.25f);
            return m;
        }

        private static Material RoofMat(string tex, System.Func<Color> fallback)
        {
            // 14차-12: 지붕도 단색(포인트 색을 MPB 로 곱한다) — 사진 기와 위에 색을 얹으면 탁해진다.
            return PastelLit();
        }

        private static Material Build(string name)
        {
            if (name.StartsWith("Facade_"))
            {
                // 14차-12: 사진 파사드 대신 '선화' 파사드(흰 바탕 + 창·문·간판 잉크) — 파스텔을 곱해도 탁해지지 않는다.
                var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_FacadeLine_" + name.Substring(7))
                          ?? Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_" + name);
                if (tex != null)
                    return ArtAssets.CreateTexturedLit(tex, Color.white, 0.05f);
                return CoastMaterials.CreateLit(() => CoastPalette.TownCream);
            }
            switch (name)
            {
                // 6차: 옆벽·지붕도 그려진 텍스처(Firefly). 없으면 예전 단색/스투코.
                case "Wall":
                case "WallCool":
                    // 14차-12: 옆벽·뒷벽은 단색 흰 바탕(파스텔 MPB) — 사진 벽 텍스처 OFF
                    return PastelLit();
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
                // 14차-13: 상가 파트 키트(Shop_*) — 색은 MPB 로 들어오므로 흰 바탕
                case "Trim": case "Frame": case "Door": case "AwningA": case "AwningB": case "Roof":
                    return PastelLit();
                case "Dark": return CoastMaterials.CreateToon(new Color(0.12f, 0.10f, 0.12f), null, null, 0.1f);
                // 14차-11: 장애물·차량 3D 키트(Obs3_*)
                case "SlimeBody": return CoastMaterials.CreateToon(new Color(0.95f, 0.36f, 0.30f), null, null, 0.45f);
                case "SlimeDark": return CoastMaterials.CreateToon(new Color(0.72f, 0.20f, 0.17f), null, null, 0.35f);
                case "Eye": return CoastMaterials.CreateToon(new Color(0.08f, 0.06f, 0.09f), null, null, 0.6f);
                case "EyeWhite": return CoastMaterials.CreateUnlit(Color.white);
                case "ConeOrange": return CoastMaterials.CreateToon(new Color(0.98f, 0.46f, 0.12f), null, null, 0.2f);
                case "ConeWhite": case "BarrierWhite": return CoastMaterials.CreateToon(new Color(0.97f, 0.97f, 0.95f), null, null, 0.2f);
                case "ConeBase": return CoastMaterials.CreateToon(new Color(0.14f, 0.14f, 0.16f), null, null, 0.1f);
                case "BarrierOrange": return CoastMaterials.CreateToon(new Color(0.98f, 0.52f, 0.16f), null, null, 0.2f);
                case "WoodDark": return CoastMaterials.CreateToon(new Color(0.42f, 0.28f, 0.16f), null, null, 0.05f);
                case "BusBody": return CoastMaterials.CreateToon(new Color(0.20f, 0.56f, 0.82f), null, null, 0.4f);
                case "BusRoof": return CoastMaterials.CreateToon(new Color(0.93f, 0.94f, 0.96f), null, null, 0.3f);
                case "VanBody": return CoastMaterials.CreateToon(new Color(0.96f, 0.93f, 0.86f), null, null, 0.4f);
                case "Tire": return CoastMaterials.CreateToon(new Color(0.10f, 0.10f, 0.11f), null, null, 0.1f);
                case "Light": return CoastMaterials.CreateUnlit(new Color(1f, 0.95f, 0.6f));
                case "Chrome": return CoastMaterials.CreateToon(new Color(0.75f, 0.78f, 0.82f), null, null, 0.7f);
                default: return CoastMaterials.CreateLit(() => CoastPalette.TownCream);
            }
        }
    }
}
