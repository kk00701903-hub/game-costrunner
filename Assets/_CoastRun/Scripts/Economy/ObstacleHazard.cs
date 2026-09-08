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

        /// 살아 있는 하자드 레지스트리 — 펫(오토바이탄 깡패)이 앞 장애물을 찾을 때 쓴다.
        public static readonly System.Collections.Generic.List<ObstacleHazard> Active =
            new System.Collections.Generic.List<ObstacleHazard>();

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        /// 차량(마주 오는 차·버스)은 못 부순다. 그 밖의 정적 장애물은 전부 부술 수 있다.
        public bool Breakable => GetComponentInParent<OncomingCar>() == null;

        /// 장애물 루트째 제거 + 파편 연출. 니어미스 존도 함께 사라진다.
        public void Smash()
        {
            Transform root = transform;
            while (root.parent != null && !root.parent.name.StartsWith("Obstacle") && root.parent.name != "Obstacles")
                root = root.parent;
            if (root.parent != null && root.parent.name.StartsWith("Obstacle_"))
                root = root.parent;
            JuiceDirector.Instance?.PlaySmash(root.position + Vector3.up * 0.5f);
            Destroy(root.gameObject);
        }

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
            {
                player.SoftHit(ClassifyHit(player), BounceSide(player));
                // 14차-14: 부딪힌 장애물은 '팡' 하고 귀엽게 터진다 — 납작해졌다 별·하트로 흩어지고 사라진다.
                Pop();
            }
        }

        private bool _popped;
        public void Pop()
        {
            if (_popped) return;
            _popped = true;
            PopFrom(transform);
        }

        /// 18차: 어떤 장애물 부품에서든 루트(Obstacle_*)를 찾아 팡 터뜨린다 — 허들(DuckHazard)·기타 하자드 공용.
        public static void PopFrom(Transform any)
        {
            Transform root = any;
            while (root.parent != null && !root.parent.name.StartsWith("Obstacle") && root.parent.name != "Obstacles")
                root = root.parent;
            if (root.parent != null && root.parent.name.StartsWith("Obstacle_"))
                root = root.parent;
            if (root.GetComponent<ObstaclePopAnim>() != null) return;
            PopRoot(root);
        }

        private static void PopRoot(Transform root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var w = root.GetComponent<ObstacleWarning>(); if (w != null) w.enabled = false;
            // 17차: '닿는 순간' 터진다 — 파편은 주인공과 장애물 사이(접점)에서, 주인공을 따라오며 흩어진다.
            // 장애물 자체는 주인공에게 붙여 두고(월드 위치 유지) 0.2 s 안에 납작→펑 사라지므로 뒤에 남지 않는다.
            var pc = FindAnyObjectByType<PlayerController>();
            Vector3 at = pc != null ? Vector3.Lerp(pc.transform.position, root.position, 0.45f) + Vector3.up * 0.35f
                                    : root.position + Vector3.up * 0.45f;
            JuiceDirector.Instance?.PlayObstaclePop(at);
            if (pc != null) root.SetParent(pc.transform, true);
            var pop = root.gameObject.AddComponent<ObstaclePopAnim>();
            pop.Begin(root);
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

            // 14차-11: Blender 3D 콘이 있으면 최우선(입체 + 잉크 테두리)
            if (JejuKit.Load("Obs3_Cone") != null)
            {
                JejuKit.Spawn("Obs3_Cone", root.transform, Vector3.zero, 0f, 0.72f / 0.78f);
                FinishCone(root, lane);
                ObstacleOutline.Attach(root.transform, 1.05f);
                return root;
            }
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
            HazardRing.Attach(root.transform, 0.6f);
            ObstacleOutline.Attach(root.transform);
        }

        public void BindNearMiss(NearMissZone zone) => nearMiss = zone;
    }

    /// 14차-14: 장애물 팡 — 0.08 s 납작(1.35, 0.55) → 0.22 s 로 0 으로 줄며 사라진다.
    public class ObstaclePopAnim : MonoBehaviour
    {
        private Transform _root; private Vector3 _base; private float _t;
        public void Begin(Transform root) { _root = root; _base = root.localScale; }
        private void Update()
        {
            if (_root == null) { Destroy(this); return; }
            _t += Time.deltaTime;
            _t += Time.unscaledDeltaTime - Time.deltaTime;   // 히트스톱 중에도 연출은 흐른다
            if (_t < 0.06f)
            {
                float k = _t / 0.06f;
                _root.localScale = new Vector3(_base.x * Mathf.Lerp(1f, 1.45f, k), _base.y * Mathf.Lerp(1f, 0.45f, k), _base.z * Mathf.Lerp(1f, 1.45f, k));
            }
            else if (_t < 0.22f)
            {
                float k = (_t - 0.06f) / 0.16f;
                float e = 1f - k * k;
                _root.localScale = new Vector3(_base.x * 1.35f * e, _base.y * Mathf.Lerp(0.55f, 1.6f, k) * e, _base.z * 1.35f * e);
            }
            else Destroy(_root.gameObject);
        }
    }
}
