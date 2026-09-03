using UnityEngine;

namespace CoastRun
{
    /// Extra obstacle types beyond traffic cones — season aware.
    public static class ObstacleCatalog
    {
        public static ObstacleId Pick(SeasonKind season, WeatherKind weather, System.Random rng)
        {
            double r = rng.NextDouble();
            if (weather == WeatherKind.Rain && r < 0.35)
                return ObstacleId.PuddleSlow;
            if (weather == WeatherKind.Snow && r < 0.4)
                return ObstacleId.SnowDrift;
            if (season == SeasonKind.Autumn && r < 0.3)
                return ObstacleId.LeafDrift;

            if (r < 0.28) return ObstacleId.TrafficCone;
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
                        new Vector3(0.95f, 0.48f, 0.22f), () => CoastPalette.AccentOrange, 0.32f, 0.55f, 0.55f);
                case ObstacleId.CrateStack:
                case ObstacleId.DeliveryBox:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Crate",
                        new Vector3(0.55f, 0.65f, 0.55f), () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.35f), 0.3f, 0.6f, 0.7f);
                case ObstacleId.WetFloorSign:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_WetFloorSign",
                        new Vector3(0.35f, 0.55f, 0.1f), () => CoastPalette.CoinYellow, 0.2f, 0.5f, 0.6f);
                case ObstacleId.SnowDrift:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_SnowDrift",
                        new Vector3(1.1f, 0.35f, 0.75f), () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.5f), 0.42f, 0.42f, 0.45f);
                case ObstacleId.LeafDrift:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_LeafDrift",
                        new Vector3(1.0f, 0.22f, 0.7f), () => CoastPalette.AccentOrange, 0.4f, 0.35f, 0.35f);
                case ObstacleId.PuddleSlow:
                    return CreatePuddle(parent, worldPos, lane);
                case ObstacleId.BikeFallen:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_BikeFallen",
                        new Vector3(0.9f, 0.22f, 0.35f), () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.SeaTeal, 0.4f), 0.32f, 0.35f, 0.4f);
                case ObstacleId.TouristCluster:
                    return CreateSimple(parent, worldPos, lane, "Obstacle_Tourists",
                        new Vector3(0.75f, 0.95f, 0.5f), () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.4f), 0.32f, 0.75f, 1.0f);
                default:
                    return ObstacleHazard.CreateTrafficCone(parent, worldPos, lane);
            }
        }

        private static GameObject CreateSimple(Transform parent, Vector3 worldPos, int lane, string name,
            Vector3 visualScale, System.Func<Color> color, float hardRadius, float hardHeight, float prefabFitHeight)
        {
            GameObject root = null;
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
            return root;
        }

        private static GameObject CreatePuddle(Transform parent, Vector3 worldPos, int lane)
        {
            var root = new GameObject("Obstacle_Puddle");
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;
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
            return root;
        }

        private static void AttachTriggers(GameObject root, int lane, float hardR, float hardH, float nearR, float nearH)
        {
            var hard = new GameObject("HardHit");
            hard.transform.SetParent(root.transform, false);
            hard.transform.localPosition = new Vector3(0f, hardH * 0.5f, 0f);
            var hardCol = hard.AddComponent<CapsuleCollider>();
            hardCol.isTrigger = true;
            hardCol.radius = hardR;
            hardCol.height = hardH;
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
