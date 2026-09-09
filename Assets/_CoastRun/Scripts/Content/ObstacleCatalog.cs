using UnityEngine;

namespace CoastRun
{
    /// Extra obstacle types beyond traffic cones — season aware.
    public static class ObstacleCatalog
    {
        public static ObstacleId Pick(SeasonKind season, WeatherKind weather, System.Random rng, int stage = 1)
        {
            double r = rng.NextDouble();
            if (weather == WeatherKind.Rain && r < 0.35)
                return ObstacleId.PuddleSlow;
            if (weather == WeatherKind.Snow && r < 0.4)
                return ObstacleId.SnowDrift;
            // 계절 고유 장애물(날씨와 무관): 봄 웅덩이, 여름 관광객, 가을 낙엽, 겨울 눈더미.
            if (season == SeasonKind.Autumn && r < 0.3)
                return ObstacleId.LeafDrift;
            if (season == SeasonKind.Winter && r < 0.22)
                return ObstacleId.SnowDrift;
            if (season == SeasonKind.Spring && r < 0.12)
                return ObstacleId.PuddleSlow;
            if (season == SeasonKind.Summer && r < 0.10)
                return ObstacleId.TouristCluster;

            // 챕터가 오르면 새 장애물이 섞인다(각 15%). 두 번째 주사위로 기존 분포를 흩트리지 않는다.
            double r2 = rng.NextDouble();
            if (stage >= ChapterDifficulty.LanternFrom && r2 < 0.12) return ObstacleId.LanternString;
            if (stage >= ChapterDifficulty.ScooterFrom && r2 < 0.26) return ObstacleId.ScooterParked;
            if (stage >= ChapterDifficulty.StatueFrom && r2 < 0.40) return ObstacleId.StoneStatue;

            if (r < 0.16) return ObstacleId.TrafficCone;
            if (r < 0.28) return ObstacleId.Slime;         // 14차-2: 슬라임이 콘 절반을 대신한다
            if (r < 0.4) return ObstacleId.OverheadBar;
            if (r < 0.5) return ObstacleId.Clothesline;
            if (r < 0.6) return ObstacleId.Barrier;
            if (r < 0.7) return ObstacleId.CrateStack;
            if (r < 0.78) return ObstacleId.DeliveryBox;
            if (r < 0.86) return ObstacleId.BikeFallen;
            if (r < 0.93) return ObstacleId.WetFloorSign;
            return ObstacleId.TouristCluster;
        }

        public static GameObject Spawn(ObstacleId id, Transform parent, Vector3 worldPos, int lane)
        {
            var go = SpawnInner(id, parent, worldPos, lane);
            // 14차-10: 첫 등장 하이라이트('!' + 통통 + 빨간 테두리) — 피할 시간을 준다
            ObstacleWarning.Attach(go);
            return go;
        }

        private static GameObject SpawnInner(ObstacleId id, Transform parent, Vector3 worldPos, int lane)
        {
            switch (id)
            {
                case ObstacleId.TrafficCone:
                    return ObstacleHazard.CreateTrafficCone(parent, worldPos, lane);
                case ObstacleId.OverheadBar:
                    return DuckHazard.Create(parent, worldPos, lane, DuckStyle.OverheadBar);
                case ObstacleId.Clothesline:
                    return DuckHazard.Create(parent, worldPos, lane, DuckStyle.Clothesline);
                case ObstacleId.Barrier:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Barrier",
                        new Vector3(0.95f, 0.48f, 0.22f), () => CoastPalette.AccentOrange, 0.32f, 0.55f, 0.55f, "Barrier", 0.9f);
                case ObstacleId.Slime:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Slime",
                        new Vector3(0.95f, 0.85f, 0.95f), () => new Color(0.95f, 0.36f, 0.30f), 0.42f, 0.75f, 0.9f, "Slime", 1.15f);   // 14차-9: 장애물은 빨강 계열(색 규칙)   // 14차-8: 말랑이 1.2배(무릎 높이 이상, 코인보다 크게)
                case ObstacleId.CrateStack:
                case ObstacleId.DeliveryBox:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Crate",
                        new Vector3(0.55f, 0.65f, 0.55f), () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.35f), 0.3f, 0.6f, 0.7f, "Crate", 1.0f);
                case ObstacleId.WetFloorSign:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_WetFloorSign",
                        new Vector3(0.35f, 0.55f, 0.1f), () => CoastPalette.CoinYellow, 0.2f, 0.5f, 0.6f, "WetSign", 0.8f);
                case ObstacleId.ParkedBus:
                    // Chest-high and solid: a Bounce hit (see ObstacleHazard.BounceHeight).
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Bus",
                        new Vector3(1.4f, 2.4f, 3.2f), () => Color.Lerp(CoastPalette.SeaTeal, Color.white, 0.25f), 0.6f, 2.4f, 2.4f, "Bus", 2.6f);
                case ObstacleId.SnowDrift:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_SnowDrift",
                        new Vector3(1.1f, 0.35f, 0.75f), () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.5f), 0.42f, 0.42f, 0.45f, "SnowDrift", 0.6f);
                case ObstacleId.LeafDrift:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_LeafDrift",
                        new Vector3(1.0f, 0.22f, 0.7f), () => CoastPalette.AccentOrange, 0.4f, 0.35f, 0.35f, "LeafDrift", 0.45f);
                case ObstacleId.PuddleSlow:
                    return CreatePuddle(parent, worldPos, lane);
                case ObstacleId.BikeFallen:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_BikeFallen",
                        new Vector3(0.9f, 0.22f, 0.35f), () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.SeaTeal, 0.4f), 0.32f, 0.35f, 0.4f, "Bike", 0.7f);
                case ObstacleId.StoneStatue:
                    return CreateStatue(parent, worldPos, lane);
                case ObstacleId.ScooterParked:
                    // 낮고 길쭉: 점프로 넘는다(Trip 높이).
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Scooter",
                        new Vector3(0.6f, 0.75f, 1.5f), () => Color.Lerp(CoastPalette.SeaTeal, Color.white, 0.3f), 0.34f, 0.6f, 0.8f, "Scooter", 1.0f);
                case ObstacleId.LanternString:
                    return DuckHazard.Create(parent, worldPos, lane, DuckStyle.LanternString);
                case ObstacleId.TouristCluster:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Tourists",
                        new Vector3(0.75f, 0.95f, 0.5f), () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.4f), 0.32f, 0.75f, 1.0f, "Tourists", 1.6f);
                default:
                    return ObstacleHazard.CreateTrafficCone(parent, worldPos, lane);
            }
        }

        private static GameObject CreateSimple(Transform parent, Vector3 worldPos, int lane, string name,
            Vector3 visualScale, System.Func<Color> color, float hardRadius, float hardHeight, float prefabFitHeight,
            string paintedKey = null, float paintedHeight = 1f)
        {
            GameObject root = null;
            // 14차-11: Blender 3D 키트(Obs3_<key>.fbx)가 있으면 그림 대신 진짜 입체 모델 + 잉크 테두리.
            if (paintedKey != null && JejuKit.Load("Obs3_" + paintedKey) != null)
            {
                root = new GameObject(name);
                root.transform.SetParent(parent, false);
                root.transform.position = worldPos;
                root.transform.rotation = DownhillPath.Rotation;
                JejuKit.Spawn("Obs3_" + paintedKey, root.transform, Vector3.zero, 0f, paintedHeight / Mathf.Max(0.5f, KitHeight(paintedKey)));
                AttachTriggers(root, lane, hardRadius, hardHeight, hardRadius * 2.0f, hardHeight * 1.25f);
                BlobShadow.Attach(root.transform, Mathf.Max(0.45f, visualScale.x * 0.85f));
                HazardRing.Attach(root.transform, Mathf.Max(0.55f, hardRadius * 1.9f));
                ObstacleOutline.Attach(root.transform, 1.05f);
                return root;
            }
            // Firefly painting first (Resources/CoastRun/Obs_<key>.png); the block below
            // stays as the fallback when no painting exists yet.
            if (paintedKey != null && PaintedProp.Available(paintedKey))
            {
                root = new GameObject(name);
                root.transform.SetParent(parent, false);
                root.transform.position = worldPos;
                root.transform.rotation = DownhillPath.Rotation;
                PaintedProp.Attach(root.transform, paintedKey, paintedHeight, replace: false, outline: true, outlineColor: ObstacleOutline.Red, outlineMul: 3.0f);   // 33차: 붉은 굵은 테두리
                AttachTriggers(root, lane, hardRadius, hardHeight, hardRadius * 2.0f, hardHeight * 1.25f);
                BlobShadow.Attach(root.transform, Mathf.Max(0.45f, visualScale.x * 0.85f));
                HazardRing.Attach(root.transform, Mathf.Max(0.55f, hardRadius * 1.9f));
                return root;
            }
            var prefab = PrefabLibrary.TryInstantiate(name, parent, Vector3.zero);
            if (prefab != null)
            {
                if (RoadPlacement.IsPrefabUsable(prefab))
                {
                    root = prefab;
                    root.transform.position = worldPos;
                    root.transform.rotation = DownhillPath.Rotation;
                    RoadPlacement.FitHeight(root, prefabFitHeight);
                }
                else
                {
                    Object.Destroy(prefab);
                }
            }

            if (root == null)
            {
                root = new GameObject(name);
                root.transform.SetParent(parent, false);
                root.transform.position = worldPos;
                root.transform.rotation = DownhillPath.Rotation;
                var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vis.name = "Visual";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = new Vector3(0f, visualScale.y * 0.5f, 0f);
                vis.transform.localScale = visualScale;
                Object.Destroy(vis.GetComponent<Collider>());
                vis.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(color);
            }

            AttachTriggers(root, lane, hardRadius, hardHeight, hardRadius * 2.0f, hardHeight * 1.25f);
            BlobShadow.Attach(root.transform, Mathf.Max(0.45f, visualScale.x * 0.85f));
            HazardRing.Attach(root.transform, Mathf.Max(0.55f, hardRadius * 1.9f));
            ObstacleOutline.Attach(root.transform);
            return root;
        }

        /// 돌하르방 석상: 제주 키트 모델이 있으면 그 모델, 없으면 그림/회색 기둥. 키가 커서 점프 불가(Bounce).
        private static GameObject CreateStatue(Transform parent, Vector3 worldPos, int lane)
        {
            if (JejuKit.Load("Prop_Hareubang") != null)
            {
                var root = new GameObject("Obstacle_StoneStatue");
                root.transform.SetParent(parent, false);
                root.transform.position = worldPos;
                root.transform.rotation = DownhillPath.Rotation;
                JejuKit.Spawn("Prop_Hareubang", root.transform, Vector3.zero, 180f, 1.35f);
                foreach (var c in root.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                AttachTriggers(root, lane, 0.42f, 1.9f, 0.85f, 2.2f);
                BlobShadow.Attach(root.transform, 0.7f);
                HazardRing.Attach(root.transform, 0.8f);
                ObstacleOutline.Attach(root.transform);
                return root;
            }
            return CreateSimple(parent, worldPos, lane, "Obstacle_StoneStatue",
                new Vector3(0.7f, 1.9f, 0.7f), () => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.35f), 0.42f, 1.9f, 1.9f, "Hareubang", 1.9f);
        }

        private static GameObject CreatePuddle(Transform parent, Vector3 worldPos, int lane)
        {
            var root = new GameObject("Obstacle_Puddle");
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;
            root.transform.rotation = DownhillPath.Rotation;
            if (PaintedProp.Available("Puddle"))
            {
                PaintedDecal.Attach(root.transform, "Puddle", 1.6f);
                AttachTriggers(root, lane, 0.45f, 0.3f, 0.85f, 0.45f);
                return root;
            }
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "Visual";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            vis.transform.localScale = new Vector3(1.1f, 0.03f, 0.85f);
            Object.Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial =
                CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.SeaTeal, CoastPalette.RoadGrey, 0.45f));
            AttachTriggers(root, lane, 0.45f, 0.3f, 0.85f, 0.45f);
            BlobShadow.Attach(root.transform, 0.9f);
            HazardRing.Attach(root.transform, 0.85f);
            return root;
        }

        /// Obs3 모델의 원래 높이(m) — Blender 스크립트와 맞춘다. paintedHeight 로 스케일한다.
        private static float KitHeight(string key)
        {
            switch (key)
            {
                case "Slime": return 0.84f;
                case "Cone": return 0.78f;
                case "Barrier": return 0.84f;
                case "Crate": return 0.74f;
                default: return 1f;
            }
        }

        private static void AttachTriggers(GameObject root, int lane, float hardR, float hardH, float nearR, float nearH)
        {
            var hard = new GameObject("HardHit");
            hard.transform.SetParent(root.transform, false);
            hard.transform.localPosition = new Vector3(0f, hardH * 0.5f, 0f);
            var hardCol = hard.AddComponent<CapsuleCollider>();
            hardCol.isTrigger = true;
            // 14차-9: 장애물 판정도 그림의 70% — 가장자리 스침은 '니어미스'로 보상된다.
            hardCol.radius = hardR * 0.7f;
            hardCol.height = hardH * 0.9f;
            var hazard = hard.AddComponent<ObstacleHazard>();

            var near = new GameObject("NearMiss");
            near.transform.SetParent(root.transform, false);
            near.transform.localPosition = new Vector3(0f, nearH * 0.5f, 0f);
            var nearCol = near.AddComponent<CapsuleCollider>();
            nearCol.isTrigger = true;
            nearCol.radius = nearR;
            nearCol.height = nearH;
            var zone = near.AddComponent<NearMissZone>();
            zone.Configure(10, lane);
            hazard.BindNearMiss(zone);
        }
    }
}
