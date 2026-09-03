using UnityEngine;

namespace CoastRun
{
    /// Factory for reusable / seasonal props. Prefers Resources prefab, else procedural mesh.
    public static class PropCatalog
    {
        public static GameObject Spawn(PropId id, Transform parent, Vector3 localPos, SeasonKind season, System.Random rng)
        {
            string prefab = PrefabName(id, season);
            var go = TryUsablePrefab(prefab, parent, localPos);
            if (go != null)
                return go;

            go = TryUsablePrefab(BlenderPrefabName(id), parent, localPos);
            if (go != null)
                return go;

            go = TryUsablePrefab(BasePrefabName(id), parent, localPos);
            if (go != null)
                return go;

            return BuildProcedural(id, parent, localPos, season, rng);
        }

        private static GameObject TryUsablePrefab(string name, Transform parent, Vector3 localPos)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            var go = PrefabLibrary.TryInstantiate(name, parent, localPos);
            if (go == null)
                return null;
            if (!RoadPlacement.IsPrefabUsable(go))
            {
                Object.Destroy(go);
                return null;
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = DownhillPath.UprightLocal;
            return go;
        }

        public static string PrefabName(PropId id, SeasonKind season) =>
            "Prop_" + id + "_" + season;

        public static string BasePrefabName(PropId id) => "Prop_" + id;

        /// Maps enum → Resources name used by Blender seasonal pack.
        public static string BlenderPrefabName(PropId id)
        {
            switch (id)
            {
                case PropId.Bench: return "Prop_Bench";
                case PropId.StreetLamp: return "Prop_StreetLamp";
                case PropId.CherryTree: return "Prop_CherryTree";
                case PropId.Maple: return "Prop_Maple";
                case PropId.Pine: return "Prop_Pine";
                case PropId.Palm: return "Prop_Palm";
                case PropId.Snowman: return "Prop_Snowman";
                case PropId.Buoy: return "Prop_Buoy";
                case PropId.CafeUmbrella: return "Prop_CafeUmbrella";
                case PropId.IceCreamCart: return "Prop_IceCreamCart";
                case PropId.Pumpkin: return "Prop_Pumpkin";
                case PropId.LeafPile: return "Prop_LeafPile";
                case PropId.SnowBank: return "Prop_SnowBank";
                case PropId.VendingMachine: return "Prop_VendingMachine";
                case PropId.Planter:
                case PropId.FlowerBox: return "Prop_Planter";
                case PropId.FestivalLantern: return "Prop_FestivalLantern";
                default: return BasePrefabName(id);
            }
        }

        public static PropId[] PoolFor(SeasonKind season, bool townSide)
        {
            switch (season)
            {
                case SeasonKind.Spring:
                    return townSide
                        ? new[]
                        {
                            PropId.CherryTree, PropId.FlowerBox, PropId.CafeUmbrella, PropId.Bench,
                            PropId.WindChime, PropId.ShopAwningSpring, PropId.Planter, PropId.Signboard,
                            PropId.VendingMachine, PropId.StreetLamp, PropId.TouristNpc, PropId.CoupleNpc,
                            PropId.BusStop, PropId.Mailbox, PropId.NewspaperStand
                        }
                        : new[]
                        {
                            PropId.CherryTree, PropId.Buoy, PropId.Lifebuoy, PropId.Sandbag,
                            PropId.FlowerBox, PropId.BirdFlockMarker, PropId.Bench
                        };
                case SeasonKind.Autumn:
                    return townSide
                        ? new[]
                        {
                            PropId.Maple, PropId.LeafPile, PropId.Pumpkin, PropId.Bench,
                            PropId.ShopAwningAutumn, PropId.CafeUmbrella, PropId.Planter, PropId.FestivalLantern,
                            PropId.StreetLamp, PropId.Signboard, PropId.Scooter, PropId.KidNpc,
                            PropId.PowerBox, PropId.TrashCan, PropId.BikeRack
                        }
                        : new[]
                        {
                            PropId.Maple, PropId.LeafPile, PropId.Buoy, PropId.FishingRodRack,
                            PropId.Sandbag, PropId.Barrier, PropId.Bench
                        };
                case SeasonKind.Winter:
                    return townSide
                        ? new[]
                        {
                            PropId.Pine, PropId.SnowBank, PropId.Snowman, PropId.ShopAwningWinter,
                            PropId.StreetLamp, PropId.Bench, PropId.VendingMachine, PropId.BusStop,
                            PropId.TaxiStand, PropId.FestivalLantern, PropId.Mailbox, PropId.FireHydrant,
                            PropId.DogWalkerNpc, PropId.Planter, PropId.TrafficLight
                        }
                        : new[]
                        {
                            PropId.Pine, PropId.SnowBank, PropId.Snowman, PropId.Sandbag,
                            PropId.Buoy, PropId.Barrier, PropId.Lifebuoy
                        };
                default: // Summer
                    return townSide
                        ? new[]
                        {
                            PropId.Palm, PropId.CafeUmbrella, PropId.IceCreamCart, PropId.SurfboardRack,
                            PropId.ShopAwningSummer, PropId.Bench, PropId.Planter, PropId.Signboard,
                            PropId.VendingMachine, PropId.StreetLamp, PropId.TouristNpc, PropId.KidNpc,
                            PropId.Scooter, PropId.BikeRack, PropId.Fountain, PropId.StatueSmall,
                            PropId.CrosswalkSign, PropId.Manhole, PropId.TrashCan
                        }
                        : new[]
                        {
                            PropId.Palm, PropId.SurfboardRack, PropId.Buoy, PropId.Lifebuoy,
                            PropId.Sandbag, PropId.IceCreamCart, PropId.Bench, PropId.BirdFlockMarker
                        };
            }
        }

        private static GameObject BuildProcedural(PropId id, Transform parent, Vector3 localPos, SeasonKind season, System.Random rng)
        {
            var root = new GameObject(id.ToString());
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            Color accent = SeasonPalettes.Get(season).foliage;
            float s = 0.85f + (float)rng.NextDouble() * 0.35f;

            switch (id)
            {
                case PropId.Bench:
                    Box(root.transform, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.08f, 0.45f), new Color(0.45f, 0.32f, 0.22f));
                    Box(root.transform, "LegL", new Vector3(-0.55f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.4f), new Color(0.3f, 0.3f, 0.32f));
                    Box(root.transform, "LegR", new Vector3(0.55f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.4f), new Color(0.3f, 0.3f, 0.32f));
                    break;
                case PropId.StreetLamp:
                    Box(root.transform, "Pole", new Vector3(0f, 2.2f, 0f), new Vector3(0.12f, 4.4f, 0.12f), new Color(0.35f, 0.35f, 0.38f));
                    Box(root.transform, "Head", new Vector3(0.35f, 4.2f, 0f), new Vector3(0.7f, 0.15f, 0.35f), new Color(0.9f, 0.85f, 0.55f));
                    break;
                case PropId.CafeUmbrella:
                    Box(root.transform, "Pole", new Vector3(0f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.08f), Color.white);
                    Box(root.transform, "Canopy", new Vector3(0f, 2.15f, 0f), new Vector3(1.8f * s, 0.08f, 1.8f * s),
                        season == SeasonKind.Autumn ? new Color(0.85f, 0.45f, 0.2f) : new Color(0.9f, 0.25f, 0.3f));
                    break;
                case PropId.TrashCan:
                    Cyl(root.transform, "Can", new Vector3(0f, 0.45f, 0f), 0.28f, 0.9f, new Color(0.4f, 0.45f, 0.4f));
                    break;
                case PropId.VendingMachine:
                    Box(root.transform, "Body", new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 1.9f, 0.7f), new Color(0.85f, 0.15f, 0.2f));
                    Box(root.transform, "Glass", new Vector3(0f, 1.05f, 0.36f), new Vector3(0.7f, 1.2f, 0.05f), new Color(0.4f, 0.7f, 0.9f));
                    break;
                case PropId.Planter:
                case PropId.FlowerBox:
                    Box(root.transform, "Box", new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.5f, 0.55f), new Color(0.55f, 0.4f, 0.28f));
                    Box(root.transform, "Leaves", new Vector3(0f, 0.55f, 0f), new Vector3(0.95f, 0.25f, 0.4f), accent);
                    break;
                case PropId.CherryTree:
                case PropId.Maple:
                case PropId.Palm:
                case PropId.Pine:
                    BuildTree(root.transform, id, accent, s);
                    break;
                case PropId.Snowman:
                    Sphere(root.transform, "Base", new Vector3(0f, 0.35f, 0f), 0.7f, Color.white);
                    Sphere(root.transform, "Mid", new Vector3(0f, 0.95f, 0f), 0.5f, Color.white);
                    Sphere(root.transform, "Head", new Vector3(0f, 1.4f, 0f), 0.35f, Color.white);
                    break;
                case PropId.Pumpkin:
                    Sphere(root.transform, "Body", new Vector3(0f, 0.28f, 0f), 0.55f, new Color(0.95f, 0.45f, 0.1f));
                    break;
                case PropId.LeafPile:
                    Box(root.transform, "Pile", new Vector3(0f, 0.12f, 0f), new Vector3(1.2f, 0.25f, 0.9f), accent);
                    break;
                case PropId.SnowBank:
                    Box(root.transform, "Bank", new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 0.8f), new Color(0.92f, 0.95f, 1f));
                    break;
                case PropId.PuddleDecal:
                    Box(root.transform, "Puddle", new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.02f, 0.9f), new Color(0.35f, 0.45f, 0.55f, 0.7f));
                    break;
                case PropId.Barrier:
                    Box(root.transform, "Bar", new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 0.12f, 0.12f), new Color(0.95f, 0.55f, 0.1f));
                    Box(root.transform, "PostL", new Vector3(-0.7f, 0.35f, 0f), new Vector3(0.1f, 0.7f, 0.1f), new Color(0.3f, 0.3f, 0.3f));
                    Box(root.transform, "PostR", new Vector3(0.7f, 0.35f, 0f), new Vector3(0.1f, 0.7f, 0.1f), new Color(0.3f, 0.3f, 0.3f));
                    break;
                case PropId.Cone:
                    Cyl(root.transform, "Cone", new Vector3(0f, 0.4f, 0f), 0.25f, 0.8f, new Color(0.95f, 0.4f, 0.1f));
                    break;
                case PropId.IceCreamCart:
                    Box(root.transform, "Cart", new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 1.1f, 0.8f), new Color(0.95f, 0.95f, 0.98f));
                    Box(root.transform, "Umbrella", new Vector3(0f, 1.5f, 0f), new Vector3(1.4f, 0.08f, 1.4f), new Color(0.2f, 0.65f, 0.9f));
                    break;
                case PropId.SurfboardRack:
                    Box(root.transform, "Rack", new Vector3(0f, 0.9f, 0f), new Vector3(0.15f, 1.8f, 0.8f), new Color(0.5f, 0.4f, 0.3f));
                    Box(root.transform, "Board", new Vector3(0.15f, 1f, 0f), new Vector3(0.08f, 1.6f, 0.35f), new Color(0.2f, 0.7f, 0.85f));
                    break;
                case PropId.Buoy:
                    Sphere(root.transform, "Buoy", new Vector3(0f, 0.4f, 0f), 0.55f, new Color(0.95f, 0.25f, 0.2f));
                    break;
                case PropId.Lifebuoy:
                    Cyl(root.transform, "Ring", new Vector3(0f, 0.9f, 0.2f), 0.45f, 0.12f, new Color(0.95f, 0.2f, 0.2f));
                    break;
                case PropId.Signboard:
                    Box(root.transform, "Post", new Vector3(0f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.1f), new Color(0.4f, 0.4f, 0.4f));
                    Box(root.transform, "Board", new Vector3(0f, 1.8f, 0.05f), new Vector3(1.1f, 0.7f, 0.08f), new Color(0.15f, 0.35f, 0.75f));
                    break;
                case PropId.FestivalLantern:
                    Box(root.transform, "Pole", new Vector3(0f, 1.5f, 0f), new Vector3(0.08f, 3f, 0.08f), new Color(0.35f, 0.25f, 0.2f));
                    Sphere(root.transform, "Lantern", new Vector3(0.4f, 2.4f, 0f), 0.35f, new Color(0.95f, 0.35f, 0.2f));
                    break;
                case PropId.BusStop:
                    Box(root.transform, "Shelter", new Vector3(0f, 1.4f, 0f), new Vector3(2.2f, 0.1f, 1.2f), new Color(0.7f, 0.75f, 0.8f));
                    Box(root.transform, "Post", new Vector3(-1f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.12f), new Color(0.4f, 0.4f, 0.45f));
                    break;
                case PropId.TouristNpc:
                case PropId.KidNpc:
                case PropId.CoupleNpc:
                case PropId.DogWalkerNpc:
                    BuildNpc(root.transform, id, rng);
                    break;
                case PropId.ShopAwningSpring:
                case PropId.ShopAwningSummer:
                case PropId.ShopAwningAutumn:
                case PropId.ShopAwningWinter:
                    Color awning = id == PropId.ShopAwningSpring ? new Color(0.95f, 0.7f, 0.8f)
                        : id == PropId.ShopAwningAutumn ? new Color(0.85f, 0.4f, 0.15f)
                        : id == PropId.ShopAwningWinter ? new Color(0.7f, 0.8f, 0.95f)
                        : new Color(0.2f, 0.55f, 0.85f);
                    Box(root.transform, "Awning", new Vector3(0f, 2.4f, 0.6f), new Vector3(3.2f, 0.1f, 1.4f), awning);
                    break;
                case PropId.BikeRack:
                    Box(root.transform, "Rack", new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.8f, 0.15f), new Color(0.35f, 0.35f, 0.4f));
                    break;
                case PropId.Scooter:
                    Box(root.transform, "Deck", new Vector3(0f, 0.15f, 0f), new Vector3(0.35f, 0.08f, 1.1f), new Color(0.2f, 0.2f, 0.22f));
                    Box(root.transform, "Stem", new Vector3(0f, 0.55f, 0.4f), new Vector3(0.08f, 0.8f, 0.08f), new Color(0.5f, 0.5f, 0.55f));
                    break;
                case PropId.Fountain:
                    Cyl(root.transform, "Base", new Vector3(0f, 0.3f, 0f), 1.2f, 0.6f, new Color(0.75f, 0.75f, 0.78f));
                    Cyl(root.transform, "Jet", new Vector3(0f, 0.9f, 0f), 0.15f, 0.8f, new Color(0.55f, 0.75f, 0.9f));
                    break;
                case PropId.StatueSmall:
                    Box(root.transform, "Plinth", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.65f, 0.65f, 0.68f));
                    Box(root.transform, "Figure", new Vector3(0f, 1.1f, 0f), new Vector3(0.35f, 0.9f, 0.35f), new Color(0.7f, 0.7f, 0.72f));
                    break;
                case PropId.TrafficLight:
                    Box(root.transform, "Pole", new Vector3(0f, 2f, 0f), new Vector3(0.12f, 4f, 0.12f), new Color(0.3f, 0.3f, 0.32f));
                    Box(root.transform, "Head", new Vector3(0.35f, 3.6f, 0f), new Vector3(0.35f, 0.9f, 0.35f), new Color(0.15f, 0.15f, 0.15f));
                    break;
                case PropId.Crate:
                case PropId.Barrel:
                    Box(root.transform, "Box", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.55f, 0.4f, 0.25f));
                    break;
                default:
                    Box(root.transform, "Prop", new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), accent);
                    break;
            }

            return root;
        }

        private static void BuildTree(Transform parent, PropId id, Color foliage, float scale)
        {
            Box(parent, "Trunk", new Vector3(0f, 0.9f * scale, 0f), new Vector3(0.22f, 1.8f * scale, 0.22f), new Color(0.4f, 0.28f, 0.18f));
            if (id == PropId.Palm)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 72f;
                    Box(parent, "Frond" + i, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.6f, 2.1f * scale, Mathf.Sin(a * Mathf.Deg2Rad) * 0.6f),
                        new Vector3(1.2f, 0.08f, 0.35f), foliage);
                }
            }
            else if (id == PropId.Pine)
            {
                for (int i = 0; i < 3; i++)
                    Box(parent, "Layer" + i, new Vector3(0f, (1.4f + i * 0.55f) * scale, 0f),
                        new Vector3((1.6f - i * 0.35f) * scale, 0.45f, (1.6f - i * 0.35f) * scale), foliage);
            }
            else
            {
                Color canopy = id == PropId.CherryTree ? new Color(0.95f, 0.7f, 0.8f) : foliage;
                Sphere(parent, "Canopy", new Vector3(0f, 2.2f * scale, 0f), 1.6f * scale, canopy);
            }
        }

        private static void BuildNpc(Transform parent, PropId id, System.Random rng)
        {
            Color shirt = Color.HSVToRGB((float)rng.NextDouble(), 0.45f, 0.85f);
            float h = id == PropId.KidNpc ? 0.75f : 1f;
            Capsule(parent, "Body", new Vector3(0f, 0.95f * h, 0f), 0.28f, 1.1f * h, shirt);
            Sphere(parent, "Head", new Vector3(0f, 1.65f * h, 0f), 0.22f * h, CoastPalette.Skin);
        }

        private static void Box(Transform p, string n, Vector3 pos, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c);
            CoastEditUtil.DestroyCollider(go);
        }

        private static void Sphere(Transform p, string n, Vector3 pos, float diameter, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c);
            CoastEditUtil.DestroyCollider(go);
        }

        private static void Cyl(Transform p, string n, Vector3 pos, float radius, float height, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c);
            CoastEditUtil.DestroyCollider(go);
        }

        private static void Capsule(Transform p, string n, Vector3 pos, float radius, float height, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = n;
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c);
            CoastEditUtil.DestroyCollider(go);
        }
    }
}
