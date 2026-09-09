using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Builds one 30 m coastal promenade tile matching the reference layout.
    public static class PromenadeSegmentBuilder
    {
        public const float Length = 30f;
        public const float RoadHalfWidth = 4f;
        public const float TownHalfWidth = 8f;
        public const float SeaHalfWidth = 14f;

        public static GameObject Build(int segmentIndex, Transform parent)
        {
            float baseZ = segmentIndex * Length;
            var root = new GameObject("Segment_" + segmentIndex);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(
                DownhillPath.Point(baseZ), DownhillPath.Rotation);

            BuildRoad(root.transform, segmentIndex);
            BuildTownSide(root.transform, segmentIndex);
            BuildSeaSide(root.transform, segmentIndex);
            BuildPolesAndWires(root.transform, segmentIndex);

            float pathZ = segmentIndex * Length;
            var season = StageManager.Instance != null
                ? StageManager.ChapterAsSeason(StageManager.Instance.ChapterIndex)
                : SeasonKind.Summer;
            SegmentDecorator.Decorate(root.transform, segmentIndex, season);

            return root;
        }

        private static Material _roadMat;

        /// One shared flagstone material for every segment — the old code made a new
        /// material (and re-registered a UV scroller) per tile, which leaked forever.
        private static Material RoadMaterial()
        {
            if (_roadMat != null)
            {
                if (_roadMat.HasProperty("_BaseColor")) _roadMat.SetColor("_BaseColor", SeasonLook.RoadTint(SeasonLook.Current));
                return _roadMat;
            }
            _roadMat = CoastMaterials.CreateLit(() => CoastPalette.Road);
            if (_roadMat.HasProperty("_BaseColor")) _roadMat.SetColor("_BaseColor", SeasonLook.RoadTint(SeasonLook.Current));
            // Painted flagstone (Firefly) when present; the procedural stones otherwise.
            Texture2D tex = ArtAssets.LoadTexture("Tex_Pavement_Cream") ?? RoadTextureGenerator.Flagstone();
            // Whole repeats per 30 m tile so the stone pattern is seamless across segments.
            // Not registered with RoadUvScroller: the world already moves past the camera,
            // and a scrolling texture on moving geometry makes the stones slide.
            if (_roadMat.HasProperty("_BaseMap"))
            {
                _roadMat.SetTexture("_BaseMap", tex);
                _roadMat.SetTextureScale("_BaseMap", new Vector2(3f, 12f));   // 8차: 돌 무늬 촘촘히
            }
            else
            {
                _roadMat.mainTexture = tex;
                _roadMat.mainTextureScale = new Vector2(2f, 8f);
            }
            return _roadMat;
        }

        private static void BuildRoad(Transform root, int index)
        {
            // Cream flagstone promenade with inlaid lane guides — the "cosy seaside
            // pavement" from the reference boards, not grey asphalt with a yellow dash.
            CreateBox(root, "Road", new Vector3(0f, -0.04f, Length * 0.5f),
                new Vector3(RoadHalfWidth * 2f, 0.08f, Length), () => CoastPalette.Road, RoadMaterial());

            // Lane guides sit on the lane *boundaries* (lanes are 2.2 m apart), so on a
            // curve the player still reads three lanes at a glance.
            // 14차: 노란 점선 — 3레인이 한눈에 보이고, 점선이 흘러가며 속도를 알려 준다(서브웨이 서퍼의 레일).
            // 14차-12: 점선 채도 올리고(레몬), 다섯 칸에 한 번 스카이 — 바닥에 노랑/하늘 물감이 번진 느낌
            _laneDashMat ??= CoastMaterials.CreateUnlit(() => new Color(1f, 0.90f, 0.30f, 1f));
            _laneDashMat2 ??= CoastMaterials.CreateUnlit(() => new Color(0.55f, 0.82f, 0.96f, 1f));
            int di = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = 0.6f; z < Length; z += 2.4f)
                {
                    // 14차-7: 바닥과 같은 높이라 z-파이팅으로 아른거렸다 — 1 cm 띄우고 조금 굵게.
                    var dash = CreateBox(root, "LaneDash", new Vector3(side * 1.1f, 0.016f, z + 0.6f),
                        new Vector3(0.16f, 0.012f, 1.2f), null, (di++ % 5 == 4) ? _laneDashMat2 : _laneDashMat);
                    dash.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // Terracotta curbs and a warm sidewalk on the town side.
            CreateBox(root, "CurbL", new Vector3(-RoadHalfWidth - 0.12f, 0.08f, Length * 0.5f),
                new Vector3(0.28f, 0.18f, Length), () => CoastPalette.Curb);
            CreateBox(root, "CurbR", new Vector3(RoadHalfWidth + 0.12f, 0.08f, Length * 0.5f),
                new Vector3(0.28f, 0.18f, Length), () => CoastPalette.Curb);

            CreateBox(root, "SidewalkL", new Vector3(-RoadHalfWidth - 1.35f, 0.04f, Length * 0.5f),
                new Vector3(2.2f, 0.08f, Length), () => CoastPalette.Sidewalk);

            CreateBox(root, "Deck", new Vector3(0f, -0.55f, Length * 0.5f),
                new Vector3(RoadHalfWidth * 2f + 3.6f, 0.9f, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.TownCream, 0.2f));

            BuildRoadDetails(root, index);
        }

        /// Small regular touches that make a road feel lived-in: a zebra crossing every
        /// few tiles, a manhole now and then, lamps at a fixed rhythm, planters between.
        private static void BuildRoadDetails(Transform root, int index)
        {
            var rng = new System.Random(index * 7331 + 11);

            if (index % 5 == 2)
            {
                var cross = CreateBox(root, "Crosswalk", new Vector3(0f, 0.012f, Length * 0.5f),
                    new Vector3(RoadHalfWidth * 2f - 0.3f, 0.01f, 2.6f), () => Color.white);
                var mat = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.9f));
                mat.SetTexture("_BaseMap", RoadTextureGenerator.Crosswalk());
                mat.SetTextureScale("_BaseMap", new Vector2(4f, 1f));
                cross.GetComponent<Renderer>().sharedMaterial = mat;
            }

            if (rng.NextDouble() < 0.4)
            {
                int lane = rng.Next(3) - 1;
                float z = 4f + (float)rng.NextDouble() * (Length - 8f);
                var lid = CreateCylinder(root, "Manhole", new Vector3(lane * 2.2f, 0.014f, z), 0.45f, 0.012f,
                    () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.Road, 0.35f));
                lid.transform.localRotation = Quaternion.identity;
            }

            // Street lamps every 15 m at the sidewalk edge, warm heads; planters between.
            float lampX = -(RoadHalfWidth + 0.55f);
            for (int i = 0; i < 2; i++)
            {
                float z = 7.5f + i * 15f;
                var lamp = UprightPivot(root, "Lamp", new Vector3(lampX, 0f, z));
                CreateBox(lamp, "Post", new Vector3(0f, 1.9f, 0f), new Vector3(0.12f, 3.8f, 0.12f), () => CoastPalette.Pole);
                CreateBox(lamp, "Arm", new Vector3(0.3f, 3.7f, 0f), new Vector3(0.6f, 0.07f, 0.07f), () => CoastPalette.Pole);
                CreateBox(lamp, "Head", new Vector3(0.55f, 3.55f, 0f), new Vector3(0.34f, 0.26f, 0.34f),
                    () => Color.Lerp(CoastPalette.CoinYellow, Color.white, 0.35f));

                var planter = UprightPivot(root, "Planter", new Vector3(lampX - 0.1f, 0f, z + 7.5f));
                CreateBox(planter, "Box", new Vector3(0f, 0.28f, 0f), new Vector3(0.8f, 0.55f, 0.5f),
                    () => Color.Lerp(CoastPalette.AccentOrange, CoastPalette.TownCream, 0.35f));
                CreateBox(planter, "Bloom", new Vector3(0f, 0.68f, 0f), new Vector3(0.7f, 0.3f, 0.42f),
                    () => rng.NextDouble() < 0.5 ? CoastPalette.AccentOrange : Color.Lerp(CoastPalette.SeaTeal, Color.white, 0.3f));
            }

            // Short bollards on the sea side between the wooden posts.
            float bollardX = RoadHalfWidth + 0.35f;
            for (float z = 3.5f; z < Length; z += 6f)
            {
                CreateCylinder(root, "Bollard", new Vector3(bollardX, 0f, z), 0.09f, 0.55f,
                    () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.4f));
            }
        }

        private static GameObject CreateCylinder(Transform parent, string name, Vector3 localPos, float radius,
            float height, System.Func<Color> color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos + new Vector3(0f, height * 0.5f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(color);
            CoastEditUtil.DestroyCollider(go);
            return go;
        }

        private static void BuildTownSide(Transform root, int index)
        {
            var rng = new System.Random(index * 3571 + 3);
            float shopX = -(RoadHalfWidth + 3.2f);

            // Blender kit (Resources/CoastRun/Models): real Jeju shops, 돌담, 감귤 trees.
            if (JejuKit.BuildingCount > 0 || JejuKit.ShopCount > 0)
            {
                BuildTownSideKit(root, index, rng);
                return;
            }

            // Jeju 돌담: a low basalt stone wall along the town side when the painted
            // texture exists; the plain rail otherwise.
            var stone = StoneWallMaterial();
            CreateBox(root, "TownFence", new Vector3(-RoadHalfWidth - 0.55f, stone != null ? 0.4f : 0.45f, Length * 0.5f),
                new Vector3(stone != null ? 0.45f : 0.18f, stone != null ? 0.8f : 0.9f, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.35f), stone);

            int facadeCount = FacadeCount();
            for (int i = 0; i < 3; i++)
            {
                int house = i;
                float z = 5f + i * 9f + (float)rng.NextDouble() * 1.2f;
                // Streets are a mix: low one-storey shops, two-storey guesthouses and
                // the odd small apartment block, never three of the same in a row.
                int variant = facadeCount > 0 ? rng.Next(facadeCount) : house % 2;
                int storeys = FacadeStoreys(variant, rng);
                float shopW = 4.6f + (float)rng.NextDouble() * 1.8f;
                float shopH = storeys * 3.1f + (float)rng.NextDouble() * 0.6f;
                float shopD = 5.8f;

                var pivot = UprightPivot(root, "House", new Vector3(shopX, 0f, z));
                Material facade = FacadeMaterial(variant);
                // 12차: 정사각 파사드 그림을 육면체 여섯 면에 그대로 씌우니 1층 상가는 세로로 2:1 눌리고
                // 옆면엔 정면 그림이 늘어져 붙어 있었다(픽셀 밀도·원근 이질감). 벽은 담백한 벽 텍스처로
                // 실제 크기 타일링하고, 도로 쪽 면에만 비율을 지킨 파사드 쿼드를 얹는다.
                var walls = CreateBox(pivot, "Walls", new Vector3(0f, shopH * 0.5f, 0f),
                    new Vector3(shopW, shopH, shopD),
                    () => house % 2 == 0 ? CoastPalette.TownCream : CoastPalette.BuildingCool,
                    facade != null ? PlainWallMaterial(variant) : null);
                if (facade != null)
                {
                    SetTiling(walls, shopD / 2.2f, shopH / 2.2f);
                    CreateFacadeQuad(pivot, facade, _facadeTex[Mathf.Clamp(variant, 0, _facadeTex.Length - 1)],
                        shopW, shopH, shopD);
                }

                // Trim goes on the unscaled pivot, never on the Walls cube. The walls carry
                // localScale (shopW, shopH, shopD); anything parented under them inherits
                // that scale, so a 0.55 m roof became a 31 × 36 m slab floating 25 m up and
                // the balcony rail turned into orange bars stretched out over the road —
                // the dark plane that covered the top third of the screen.
                float wallMidY = shopH * 0.5f;
                int roofKind = rng.Next(3);
                CreateBox(pivot, "Roof", new Vector3(0f, shopH + 0.35f, 0f),
                    new Vector3(shopW + 0.45f, 0.55f, shopD + 0.45f),
                    () => roofKind == 0 ? CoastPalette.Roof
                        : roofKind == 1 ? Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.45f)   // Jeju basalt-dark
                        : Color.Lerp(CoastPalette.SkyBlue, CoastPalette.RoadGrey, 0.5f));          // slate blue

                CreateBox(pivot, "Balcony", new Vector3(shopW * 0.48f, wallMidY + 0.1f, 0f),
                    new Vector3(0.35f, 0.12f, shopD * 0.55f),
                    () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.25f));
                CreateBox(pivot, "Flowers", new Vector3(shopW * 0.52f, wallMidY + 0.28f, 0f),
                    new Vector3(0.2f, 0.25f, shopD * 0.5f), () => CoastPalette.AccentOrange);

                // The painted facade already carries its windows.
                for (int row = 0; row < (facade != null ? 0 : 2); row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        float wx = shopW * 0.48f;
                        float wy = wallMidY - shopH * 0.15f + row * 1.4f;
                        float wz = -shopD * 0.22f + col * shopD * 0.44f;
                        CreateBox(pivot, "Window", new Vector3(wx, wy, wz),
                            new Vector3(0.08f, 0.95f, 1.05f), () => CoastPalette.Window);
                    }
                }
            }
        }

        /// Kit street: three lots per 30 m tile. Each lot gets a building drawn from the
        /// kit (never the same as its neighbour), a 돌담 run along the kerb with a gap at
        /// the shop door, and a tree / bench / 감귤 stall between lots.
        private static int _shopStreak;
        private static void BuildTownSideKit(Transform root, int index, System.Random rng)
        {
            float frontX = -(RoadHalfWidth + 0.9f);
            int n = JejuKit.ShopCount > 0 ? JejuKit.ShopCount : JejuKit.BuildingCount;
            int prev = (index * 7) % n;

            for (int lot = 0; lot < 3; lot++)
            {
                float z = 5.0f + lot * 10f + (float)rng.NextDouble() * 0.5f;
                int variant = rng.Next(n);
                if (variant == prev) variant = (variant + 1) % n;
                prev = variant;
                if (JejuKit.FShopAvailable) variant = rng.Next(1000000);   // 16차: 그림 상가는 높이·지붕·그림 조합이 많다 — 넓은 시드

                var pivot = UprightPivot(root, "Lot", new Vector3(frontX, 0f, z));
                // 14차-14: 실제 제주 해안 마을처럼 — 상가만 줄지어 서지 않는다. 필지 종류를 섞는다:
                //   상가 50% / 제주 낮은 집 22% / 공터(돌담+감귤나무+귤 상자) 14% / 작은 공원(정자·벤치·야자·파라솔 테라스) 14%
                int lotRoll = rng.Next(100);
                bool hasHouse = JejuKit.Load("House_A") != null;
                // 16차: 지루함 방지 — 상가가 2채 연속이면 다음은 반드시 집/공터/공원. 상가 45 / 집 22 / 공터 15 / 공원 18
                if (_shopStreak >= 2 && lotRoll < 50) lotRoll = 50 + rng.Next(50);
                if (lotRoll < 45 || !hasHouse) _shopStreak++; else _shopStreak = 0;
                if (lotRoll < 45 || !hasHouse)
                {
                    // The FBX handedness swap mirrors Blender's X: the kit's front (+X in
                    // Blender) imports facing -X, so a half turn puts the shopfront on the
                    // road side with the body extending away from it.
                    JejuKit.SpawnBuilding(variant, pivot, Vector3.zero, 180f);
                    if (JejuKit.ShopCount == 0)
                    {
                        StreetDressing.ShopFront(pivot, rng, 2.8f + (float)rng.NextDouble() * 0.9f);
                        if (rng.Next(3) != 0)
                            StreetDressing.Balcony(pivot, rng, 3.6f + (float)rng.NextDouble() * 0.6f, 5.4f);
                    }
                    StreetDressing.ContactShadow(pivot, 9.5f, 1.4f);
                    // 상가 앞 테라스: 파라솔 + 카페 세트(3집 중 1집)
                    if (rng.Next(3) == 0) JejuLots.CafeTerrace(pivot, rng);
                }
                else if (lotRoll < 67)
                {
                    JejuLots.House(pivot, rng, lotRoll % 2 == 0);
                    StreetDressing.ContactShadow(pivot, 8.5f, 1.2f);
                }
                else if (lotRoll < 82)
                {
                    JejuLots.EmptyLot(pivot, rng);
                }
                else
                {
                    JejuLots.Park(pivot, rng);
                }

                // 돌담 either side of the entrance.
                for (int side = -1; side <= 1; side += 2)
                {
                    float wz = side * 3.6f;
                    JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(0.55f, 0f, wz), 0f, 0.55f);
                    // 14차: 돌담 앞 수국 — 목표 이미지의 파란 수국 덤불. 문 앞은 비운다.
                    if (rng.Next(3) != 0)
                        StreetDressing.Hydrangea(pivot, new Vector3(0.7f, 0f, wz + side * 0.9f), rng, 0.85f + (float)rng.NextDouble() * 0.35f);
                }

                // Between lots: something to look at.
                // Jeju signatures (돌하르방, 야자수) get the biggest share so the street
                // reads as the island at a glance; the rest is orchard / seating.
                int filler = rng.Next(10);
                var gap = UprightPivot(root, "Gap", new Vector3(frontX - 0.3f, 0f, z + 4.9f));
                switch (filler)
                {
                    case 7:
                    case 8:
                    case 9:
                    {
                        // 14차-3: Kling으로 그린 거리 소품(마젠타 키 빌보드) — 귤 좌판·파라솔·서프보드
                        // 걸이·아이스크림 카트·우체통·수국 화분. 그림이 없으면 귤나무로.
                        string[] painted = { "Stall_Tangerine", "Parasol", "SurfRack", "IceCreamCart", "Postbox", "HydrangeaPot",
                                             "Haenyeo", "Hareubang2", "PlantPot", "RopeCoil", "Cone2" };
                        float[] heights = { 2.4f, 2.3f, 2.2f, 2.2f, 1.5f, 1.1f, 2.3f, 1.3f, 0.9f, 0.55f, 1.2f };
                        int pk = rng.Next(painted.Length);
                        var pivot2 = new GameObject("PaintedProp").transform;
                        pivot2.SetParent(gap, false);
                        pivot2.localPosition = new Vector3(1.1f, 0f, 0f);
                        if (PaintedProp.Attach(pivot2, painted[pk], heights[pk], replace: false, zOffset: 0f) == null)
                            JejuKit.Spawn("Prop_OrangeTree", gap, Vector3.zero, 0f, 1f);
                        else
                            BlobShadow.Attach(pivot2, heights[pk] * 0.55f);
                        break;
                    }
                    case 0: JejuKit.Spawn("Prop_OrangeTree", gap, Vector3.zero, (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.3f); break;
                    case 1: JejuKit.Spawn("Prop_Bench", gap, new Vector3(0.6f, 0f, 0f), 180f); break;
                    case 2: JejuKit.Spawn("Prop_OrangeStall", gap, new Vector3(0.9f, 0f, 0f), 180f); break;
                    case 3:
                        // A pair of hareubang flanking the gap, facing the road (front = −Y
                        // in Blender → yaw 180° like the facades).
                        JejuKit.Spawn("Prop_Hareubang", gap, new Vector3(0.9f, 0f, -1.4f), 180f, 0.85f);
                        JejuKit.Spawn("Prop_Hareubang", gap, new Vector3(0.9f, 0f, 1.4f), 180f, 0.85f);
                        break;
                    case 4:
                    case 5:
                        JejuKit.Spawn("Prop_Palm", gap, new Vector3(-0.8f, 0f, 0f), (float)rng.NextDouble() * 360f, 0.85f + (float)rng.NextDouble() * 0.35f);
                        if (rng.Next(2) == 0)
                            JejuKit.Spawn("Prop_Hareubang", gap, new Vector3(1.0f, 0f, 0.6f), 180f, 0.8f);
                        break;
                    default: JejuKit.Spawn("Prop_OrangeTree", gap, new Vector3(-1.5f, 0f, 0f), 0f, 1.1f); break;
                }
            }
        }

        // Facade sheet: Tex_Facade_A, _B, _C … in Resources/CoastRun; any gap ends the list.
        private static Texture2D[] _facadeTex;
        private static Material[] _facadeMats;

        private static int FacadeCount()
        {
            if (_facadeTex == null)
            {
                var list = new System.Collections.Generic.List<Texture2D>();
                for (char c = 'A'; c <= 'L'; c++)
                {
                    var t = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Facade_" + c);
                    if (t == null) break;
                    list.Add(t);
                }
                _facadeTex = list.ToArray();
                _facadeMats = new Material[_facadeTex.Length];
            }
            return _facadeTex.Length;
        }

        /// Firefly-painted facade on the house walls when present. A cube maps the whole
        /// texture onto each face, so the road-facing side reads as a full storefront
        /// elevation; null falls back to the flat toon colour.
        private static Material FacadeMaterial(int variant)
        {
            int n = FacadeCount();
            if (n == 0)
                return null;
            variant = Mathf.Clamp(variant, 0, n - 1);
            if (_facadeMats[variant] == null)
                _facadeMats[variant] = ArtAssets.CreateTexturedLit(_facadeTex[variant], Color.white, 0.05f);
            return _facadeMats[variant];
        }

        private static Material _laneDashMat, _laneDashMat2;
        private static Material[] _plainWallMats;
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        /// 옆면·뒷면용 벽 텍스처(Tex_Wall_Side / Stucco / Cool 순환). 없으면 톤 색.
        private static Material PlainWallMaterial(int variant)
        {
            _plainWallMats ??= new Material[3];
            int k = Mathf.Abs(variant) % 3;
            if (_plainWallMats[k] == null)
            {
                string[] names = { "Tex_Wall_Side", "Tex_Wall_Stucco", "Tex_Wall_Cool" };
                var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + names[k])
                          ?? Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Wall_Side");
                _plainWallMats[k] = tex != null
                    ? ArtAssets.CreateTexturedLit(tex, Color.white, 0.03f)
                    : CoastMaterials.CreateLit(() => CoastPalette.TownCream, 0.03f);
            }
            return _plainWallMats[k];
        }

        /// 렌더러 하나의 텍스처 타일링만 바꾼다(공유 머티리얼은 그대로, MPB).
        private static void SetTiling(GameObject go, float tx, float ty, float ox = 0f, float oy = 0f)
        {
            var r = go != null ? go.GetComponent<Renderer>() : null;
            if (r == null) return;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetVector(BaseMapStId, new Vector4(tx, ty, ox, oy));
            r.SetPropertyBlock(mpb);
        }

        /// 도로를 보는 +x 면에 파사드 쿼드. 그림의 텍셀 밀도를 가로·세로 같게 유지하고,
        /// 남는 쪽은 잘라 낸다(1층 상가는 그림 아랫부분만 — 1층이 땅에 붙어야 하니 바닥 정렬).
        private static void CreateFacadeQuad(Transform pivot, Material facade, Texture2D tex,
            float shopW, float shopH, float shopD)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Facade";
            quad.transform.SetParent(pivot, false);
            CoastEditUtil.DestroyCollider(quad);
            quad.transform.localPosition = new Vector3(shopW * 0.5f + 0.012f, shopH * 0.5f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // -z 를 보던 쿼드를 +x 로
            quad.transform.localScale = new Vector3(shopD, shopH, 1f);
            var mr = quad.GetComponent<Renderer>();
            mr.sharedMaterial = facade;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float aspect = tex != null && tex.height > 0 ? tex.width / (float)tex.height : 1f;   // 그림 가로/세로
            float faceAspect = shopD / shopH;
            float uw, uh, ox = 0f, oy = 0f;
            if (faceAspect >= aspect) { uw = 1f; uh = aspect / faceAspect; }          // 옆으로 긴 면: 세로를 자른다
            else { uh = 1f; uw = faceAspect / aspect; ox = (1f - uw) * 0.5f; }         // 위로 긴 면: 가로 가운데
            SetTiling(quad, uw, uh, ox, oy);
        }

        /// Storeys per facade: the painting's own floor count where it is obvious.
        /// A/B are the old 3–4 storey blocks; C–I are the 14차 reference-style shophouses
        /// (three floors, awning over the shop), J–L the low two-floor seaside shops.
        private static int FacadeStoreys(int variant, System.Random rng)
        {
            if (FacadeCount() == 0)
                return 2;
            switch (variant)
            {
                case 0: return 2 + rng.Next(2);   // cream block: 2–3
                case 1: return 2;                 // mint block
                case 9:
                case 10:
                case 11: return 2;                // low shophouses (shop_0 sheet)
                default: return 2 + rng.Next(2);  // shophouse rows: 2–3
            }
        }

        private static Material _stoneMat;

        private static Material StoneWallMaterial()
        {
            if (_stoneMat != null)
                return _stoneMat;
            var tex = Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Tex_Stonewall_Jeju");
            if (tex == null)
                return null;
            _stoneMat = ArtAssets.CreateTexturedLit(tex, Color.white, 0.02f);
            if (_stoneMat.HasProperty("_BaseMap"))
                _stoneMat.SetTextureScale("_BaseMap", new Vector2(12f, 1f));
            return _stoneMat;
        }

        private static void CreateNpc(Transform root, Vector3 pos, System.Random rng)
        {
            var npc = new GameObject("NPC");
            npc.transform.SetParent(root, false);
            npc.transform.localPosition = pos;
            npc.transform.localRotation = DownhillPath.UprightLocal;
            npc.AddComponent<CoastNpcWalker>();

            CreateCapsule(npc.transform, "Body", new Vector3(0f, 0.95f, 0f), 0.28f, 1.1f,
                () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SeaTeal, 0.35f));
            CreateSphere(npc.transform, "Head", new Vector3(0f, 1.65f, 0f), 0.22f, () => CoastPalette.Skin);
            CreateCapsule(npc.transform, "ArmL", new Vector3(-0.32f, 1.05f, 0f), 0.08f, 0.45f,
                () => Color.Lerp(CoastPalette.TownCream, CoastPalette.AccentOrange, 0.2f));
            CreateCapsule(npc.transform, "ArmR", new Vector3(0.32f, 1.05f, 0f), 0.08f, 0.45f,
                () => Color.Lerp(CoastPalette.TownCream, CoastPalette.AccentOrange, 0.2f));
        }

        private static void BuildSeaSide(Transform root, int index)
        {
            float railX = RoadHalfWidth + 0.9f;
            var drng = new System.Random(index * 911 + 17);

            // 14차: 만국기 — 타일마다 한 줄, 왼쪽 상가 처마에서 바다 쪽 기둥까지. 축제 거리의 리듬.
            float bz = 9f + (float)drng.NextDouble() * 12f;
            // 24차-1: 줄 높이 4.3→5.2 m. 카메라(1.85 m)가 줄 밑을 지날 때 깃발이 근접 평면에 걸려 파란 판처럼 크게 떴다.
            StreetDressing.Bunting(root, new Vector3(-RoadHalfWidth - 1.0f, 5.2f, bz), new Vector3(railX + 0.2f, 4.9f, bz + 1.5f), drng);
            CreateBox(root, "BuntingPost", new Vector3(railX + 0.2f, 2.45f, bz + 1.5f), new Vector3(0.12f, 4.9f, 0.12f),
                () => Color.Lerp(CoastPalette.TownCream, CoastPalette.RoadGrey, 0.5f));
            // 14차: 난간 너머 야자수 — 목표 이미지의 오른쪽 실루엣. 절벽 상판(y=0) 위, 타일당 2그루.
            for (int i = 0; i < 2; i++)
            {
                float pz = 4f + i * 15f + (float)drng.NextDouble() * 5f;
                if (i == 0 || drng.Next(2) == 0)
                    JejuKit.Spawn("Prop_Palm", root, new Vector3(railX + 2.6f, -0.2f, pz), (float)drng.NextDouble() * 360f, 1.05f + (float)drng.NextDouble() * 0.35f);
            }
            // 난간 앞 화단 두 개.
            StreetDressing.Planter(root, new Vector3(railX - 0.75f, 0f, 4.5f + (float)drng.NextDouble() * 3f), drng);
            StreetDressing.Planter(root, new Vector3(railX - 0.75f, 0f, 19f + (float)drng.NextDouble() * 4f), drng);

            // 14차-6: 목표 이미지의 바다 쪽 — 현무암 돌 방파제(낮은 벽) + 그 위 가는 하늘색 철난간.
            // 예전 주황 나무 난간은 사진과 달랐다. 돌담 텍스처(Tex_Stonewall_Jeju)를 실제 크기로 타일링.
            var stone = StoneWallMaterial();
            var wall = CreateBox(root, "SeaWall", new Vector3(railX, 0.45f, Length * 0.5f),
                new Vector3(0.55f, 0.9f, Length), () => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.35f), stone);
            if (stone != null) SetTiling(wall, Length / 2.4f, 0.9f / 2.4f);
            CreateBox(root, "SeaWallCap", new Vector3(railX, 0.93f, Length * 0.5f),
                new Vector3(0.7f, 0.08f, Length), () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.4f));
            var railCol = new System.Func<Color>(() => Color.Lerp(CoastPalette.SkyBlue, Color.white, 0.55f));
            CreateBox(root, "RailTop", new Vector3(railX, 1.75f, Length * 0.5f), new Vector3(0.07f, 0.07f, Length), railCol);
            CreateBox(root, "RailMid", new Vector3(railX, 1.35f, Length * 0.5f), new Vector3(0.05f, 0.05f, Length), railCol);
            for (float z = 1.2f; z < Length; z += 2.4f)
                CreateBox(root, "RailPost", new Vector3(railX, 1.36f, z), new Vector3(0.07f, 0.78f, 0.07f), railCol);

            var cliff = CreateBox(root, "Cliff", new Vector3(railX + 3.5f, -4f, Length * 0.5f),
                new Vector3(6f, 8f, Length), () => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.3f), stone);
            if (stone != null) SetTiling(cliff, Length / 2.4f, 8f / 2.4f);

            for (int i = 0; i < 2; i++)
            {
                float z = 8f + i * 12f;
                CreateBox(root, "Islet", new Vector3(22f + i * 4f, -2.2f, z),
                    new Vector3(3.5f, 2.2f, 4f),
                    () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.SeaTeal, 0.2f));
            }

            if (index % 2 == 0)
            {
                var boat = UprightPivot(root, "Sailboat", new Vector3(26f, -1.6f, Length * 0.55f));
                CreateBox(boat, "Hull", new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 3.2f),
                    () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.5f));
                CreateBox(boat, "Sail", new Vector3(0f, 2.2f, 0.2f), new Vector3(0.08f, 3.2f, 1.8f),
                    () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.15f));
            }
        }

        private static void BuildPolesAndWires(Transform root, int index)
        {
            float poleX = -(RoadHalfWidth + 6.2f);
            if (index % 2 != 0)
                return;

            var polePositions = new List<Vector3>();
            for (int i = 0; i < 2; i++)
            {
                float z = 8f + i * 14f;
                polePositions.Add(CreatePole(root, new Vector3(poleX, 0f, z)));
            }

            if (polePositions.Count > 1)
            {
                CreateWire(root, polePositions[0], polePositions[1]);
                CreateWire(root, polePositions[0] + Vector3.down * 0.35f,
                    polePositions[1] + Vector3.down * 0.35f);
            }
        }

        private static Vector3 CreatePole(Transform root, Vector3 localPos)
        {
            var pivot = UprightPivot(root, "Pole", localPos);
            CreateBox(pivot, "Shaft", new Vector3(0f, 3f, 0f),
                new Vector3(0.22f, 6f, 0.22f), () => CoastPalette.Pole);
            CreateBox(pivot, "CrossArm", new Vector3(0f, 5.4f, 0f),
                new Vector3(1.6f, 0.08f, 0.08f), () => CoastPalette.Pole);
            Vector3 worldTop = root.TransformPoint(localPos) + Vector3.up * 6.2f;
            return root.InverseTransformPoint(worldTop);
        }

        private static void CreateWire(Transform root, Vector3 a, Vector3 b)
        {
            var go = new GameObject("Wire");
            go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 8;
            lr.useWorldSpace = false;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.material = CoastMaterials.CreateUnlit(() => CoastPalette.Wire);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * 0.25f;
                lr.SetPosition(i, p);
            }
        }

        private static Transform UprightPivot(Transform parent, string name, Vector3 localPosOnRoad)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosOnRoad;
            go.transform.localRotation = DownhillPath.UprightLocal;
            return go.transform;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale,
            System.Func<Color> color)
        {
            return CreateBox(parent, name, localPos, scale, color, null);
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale,
            System.Func<Color> color, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial =
                material != null ? material : CoastMaterials.CreateLit(color);
            CoastEditUtil.DestroyCollider(go);
            return go;
        }

        private static GameObject CreateCapsule(Transform parent, string name, Vector3 localPos, float radius,
            float height, System.Func<Color> color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(color);
            CoastEditUtil.DestroyCollider(go);
            return go;
        }

        private static GameObject CreateSphere(Transform parent, string name, Vector3 localPos, float radius,
            System.Func<Color> color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * radius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(color);
            CoastEditUtil.DestroyCollider(go);
            return go;
        }
    }

    /// Simple back-and-forth walk for town NPCs with arm sway.
    public class CoastNpcWalker : MonoBehaviour
    {
        private Vector3 _origin;
        private float _phase;
        private Transform _armL;
        private Transform _armR;

        private void Start()
        {
            _origin = transform.localPosition;
            _phase = Random.value * Mathf.PI * 2f;
            _armL = transform.Find("ArmL");
            _armR = transform.Find("ArmR");
        }

        private void Update()
        {
            float walk = Mathf.Sin(Time.time * 1.4f + _phase) * 0.9f;
            transform.localPosition = _origin + new Vector3(0f, 0f, walk);

            float sway = Mathf.Sin(Time.time * 2.8f + _phase) * 12f;
            if (_armL != null)
                _armL.localRotation = Quaternion.Euler(sway, 0f, 0f);
            if (_armR != null)
                _armR.localRotation = Quaternion.Euler(-sway, 0f, 0f);
        }
    }
}
