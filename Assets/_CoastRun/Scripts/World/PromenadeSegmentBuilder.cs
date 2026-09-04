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

        private static void BuildRoad(Transform root, int index)
        {
            Material roadMat = CoastMaterials.CreateLit(() => CoastPalette.Road);
            Texture2D roadTex = ArtAssets.LoadTexture("Road_Promenade");
            if (roadTex != null)
            {
                if (roadMat.HasProperty("_BaseMap"))
                {
                    roadMat.SetTexture("_BaseMap", roadTex);
                    roadMat.SetTextureScale("_BaseMap", new Vector2(1.2f, 10f));
                }
                else
                {
                    roadMat.mainTexture = roadTex;
                    roadMat.mainTextureScale = new Vector2(1.2f, 10f);
                }
            }

            RoadUvScroller.Register(roadMat);

            CreateBox(root, "Road", new Vector3(0f, -0.04f, Length * 0.5f),
                new Vector3(RoadHalfWidth * 2f, 0.08f, Length), () => CoastPalette.Road, roadMat);

            for (float z = 1.2f; z < Length; z += 3.2f)
            {
                CreateBox(root, "CentreDash", new Vector3(0f, 0.01f, z),
                    new Vector3(0.14f, 0.02f, 1.6f), () => CoastPalette.RoadLine);
            }

            CreateBox(root, "CurbL", new Vector3(-RoadHalfWidth - 0.12f, 0.08f, Length * 0.5f),
                new Vector3(0.28f, 0.18f, Length), () => CoastPalette.Curb);
            CreateBox(root, "CurbR", new Vector3(RoadHalfWidth + 0.12f, 0.08f, Length * 0.5f),
                new Vector3(0.28f, 0.18f, Length), () => CoastPalette.Curb);

            CreateBox(root, "SidewalkL", new Vector3(-RoadHalfWidth - 1.35f, 0.04f, Length * 0.5f),
                new Vector3(2.2f, 0.08f, Length), () => CoastPalette.Sidewalk);

            CreateBox(root, "Deck", new Vector3(0f, -0.55f, Length * 0.5f),
                new Vector3(RoadHalfWidth * 2f + 3.6f, 0.9f, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.TownCream, 0.2f));
        }

        private static void BuildTownSide(Transform root, int index)
        {
            var rng = new System.Random(index * 3571 + 3);
            float shopX = -(RoadHalfWidth + 3.2f);

            CreateBox(root, "TownFence", new Vector3(-RoadHalfWidth - 0.55f, 0.45f, Length * 0.5f),
                new Vector3(0.18f, 0.9f, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.35f));

            for (int i = 0; i < 3; i++)
            {
                int house = i;
                float z = 5f + i * 9f + (float)rng.NextDouble() * 1.2f;
                float shopW = 4.8f + (float)rng.NextDouble() * 1.2f;
                float shopH = 5.4f + (float)rng.NextDouble() * 1.6f;
                float shopD = 5.8f;

                var pivot = UprightPivot(root, "House", new Vector3(shopX, 0f, z));
                CreateBox(pivot, "Walls", new Vector3(0f, shopH * 0.5f, 0f),
                    new Vector3(shopW, shopH, shopD),
                    () => house % 2 == 0 ? CoastPalette.TownCream : CoastPalette.BuildingCool);

                // Trim goes on the unscaled pivot, never on the Walls cube. The walls carry
                // localScale (shopW, shopH, shopD); anything parented under them inherits
                // that scale, so a 0.55 m roof became a 31 × 36 m slab floating 25 m up and
                // the balcony rail turned into orange bars stretched out over the road —
                // the dark plane that covered the top third of the screen.
                float wallMidY = shopH * 0.5f;
                CreateBox(pivot, "Roof", new Vector3(0f, shopH + 0.35f, 0f),
                    new Vector3(shopW + 0.45f, 0.55f, shopD + 0.45f), () => CoastPalette.Roof);

                CreateBox(pivot, "Balcony", new Vector3(shopW * 0.48f, wallMidY + 0.1f, 0f),
                    new Vector3(0.35f, 0.12f, shopD * 0.55f),
                    () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.25f));
                CreateBox(pivot, "Flowers", new Vector3(shopW * 0.52f, wallMidY + 0.28f, 0f),
                    new Vector3(0.2f, 0.25f, shopD * 0.5f), () => CoastPalette.AccentOrange);

                for (int row = 0; row < 2; row++)
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

            CreateBox(root, "WoodRailBase", new Vector3(railX, 0.4f, Length * 0.5f),
                new Vector3(0.35f, 0.8f, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.45f));
            CreateBox(root, "WoodRailTop", new Vector3(railX, 1.05f, Length * 0.5f),
                new Vector3(0.28f, 0.14f, Length),
                () => Color.Lerp(CoastPalette.TownCream, CoastPalette.AccentOrange, 0.35f));

            for (float z = 1.5f; z < Length; z += 2.2f)
            {
                CreateBox(root, "WoodPost", new Vector3(railX, 0.55f, z),
                    new Vector3(0.16f, 1.1f, 0.16f),
                    () => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.45f));
            }

            CreateBox(root, "Cliff", new Vector3(railX + 3.5f, -4f, Length * 0.5f),
                new Vector3(6f, 8f, Length), () => CoastPalette.RoadGrey);

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
