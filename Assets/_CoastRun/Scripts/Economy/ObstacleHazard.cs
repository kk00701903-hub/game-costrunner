using UnityEngine;

namespace CoastRun
{
    /// Hard hit body — SoftHit on player contact. Pair with NearMissZone sibling.
    [RequireComponent(typeof(Collider))]
    public class ObstacleHazard : MonoBehaviour
    {
        [SerializeField] private NearMissZone nearMiss;
        [SerializeField] private bool softHit = true;

        public NearMissZone NearMiss => nearMiss;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            nearMiss = GetComponentInChildren<NearMissZone>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null)
                return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;

            nearMiss?.NotifyHardHit();
            if (softHit)
                player.SoftHit(ClassifyHit(player), BounceSide(player));
        }

        /// Anything that reaches above her waist (≈ 0.9 m) is a solid body she cannot
        /// stumble over — she gets bounced sideways instead. Cones, hurdles and
        /// low rocks stay trips.
        public const float BounceHeight = 0.9f;

        public HitKind ClassifyHit(PlayerController player)
        {
            float top = float.MinValue;
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                if (c.GetComponent<NearMissZone>() != null) continue;
                top = Mathf.Max(top, c.bounds.max.y - c.bounds.min.y);
            }
            if (top == float.MinValue)
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                    top = Mathf.Max(top, r.bounds.size.y);
            return top >= BounceHeight ? HitKind.Bounce : HitKind.Trip;
        }

        /// Deflect toward the side of the obstacle she is already on; centred hits let
        /// the controller pick a lane that exists.
        public int BounceSide(PlayerController player)
        {
            Vector3 right = transform.right;
            float side = Vector3.Dot(player.transform.position - transform.position, right);
            if (Mathf.Abs(side) < 0.15f)
                return 0;
            return side > 0f ? 1 : -1;
        }

        /// Builds cone-style obstacle: solid body + wider near-miss shell.
        public static GameObject CreateTrafficCone(Transform parent, Vector3 localPos, int lane)
        {
            var root = new GameObject("Obstacle_Cone");
            root.transform.SetParent(parent, false);
            root.transform.position = localPos;
            root.transform.rotation = DownhillPath.Rotation;

            // Firefly-painted cone wins; otherwise the FBX prefab / procedural cone below.
            if (PaintedProp.Available("Cone"))
            {
                PaintedProp.Attach(root.transform, "Cone", 0.72f, replace: false);
                FinishCone(root, lane);
                return root;
            }

            // Visual — prefer MCP FBX prefab when size is sane, then shrink to knee-high.
            var visualPrefab = PrefabLibrary.TryInstantiate("Obstacle_Cone", root.transform, Vector3.zero);
            if (visualPrefab != null && !RoadPlacement.IsPrefabUsable(visualPrefab))
            {
                Object.Destroy(visualPrefab);
                visualPrefab = null;
            }

            if (visualPrefab != null)
            {
                RoadPlacement.FitHeight(visualPrefab, 0.62f);
            }
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                visual.transform.localPosition = new Vector3(0f, 0.32f, 0f);
                Object.Destroy(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateLit(() => CoastPalette.AccentOrange);

                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Stripe";
                stripe.transform.SetParent(root.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                stripe.transform.localScale = new Vector3(0.34f, 0.07f, 0.34f);
                Object.Destroy(stripe.GetComponent<Collider>());
                stripe.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.TownCream, Color.white, 0.5f));
            }

            FinishCone(root, lane);
            return root;
        }

        /// Hit body + near-miss shell + shadow, shared by the painted and modelled cone.
        private static void FinishCone(GameObject root, int lane)
        {
            // Hard hit (tight)
            var hard = new GameObject("HardHit");
            hard.transform.SetParent(root.transform, false);
            hard.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            var hardCol = hard.AddComponent<CapsuleCollider>();
            hardCol.isTrigger = true;
            hardCol.radius = 0.16f;
            hardCol.height = 0.65f;
            var hazard = hard.AddComponent<ObstacleHazard>();

            // Near-miss shell (wider)
            var near = new GameObject("NearMiss");
            near.transform.SetParent(root.transform, false);
            near.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            var nearCol = near.AddComponent<CapsuleCollider>();
            nearCol.isTrigger = true;
            nearCol.radius = 0.55f;
            nearCol.height = 0.95f;
            var zone = near.AddComponent<NearMissZone>();
            zone.Configure(10, lane);
            hazard.BindNearMiss(zone);

            BlobShadow.Attach(root.transform, 0.5f);
        }

        public void BindNearMiss(NearMissZone zone) => nearMiss = zone;
    }
}
