using UnityEngine;

namespace CoastRun
{
    /// A little delivery car driving *toward* the player down one lane (chapter 3+).
    /// It closes at player speed + its own, so it reads faster than any static row:
    /// headlights on, a horn when it is about a second and a half out, and a soft hit
    /// if the player is still in its lane when they meet.
    ///
    /// The spawner plans its lane and meeting point so the rows around that point
    /// never block the escape lanes — the car *is* the row there.
    public class OncomingCar : MonoBehaviour
    {
        private static readonly Color[] Paints =
        {
            new Color(0.93f, 0.36f, 0.34f),   // tomato
            new Color(0.38f, 0.62f, 0.93f),   // sky
            new Color(0.45f, 0.80f, 0.62f),   // mint
            new Color(0.97f, 0.80f, 0.35f),   // mustard
            new Color(0.92f, 0.92f, 0.94f),   // white
        };

        private PlayerController _player;
        private float _pathZ;
        private float _lateral;
        private float _speed;
        private Transform[] _wheels;
        private Transform _body;
        private bool _honked;
        private float _bobPhase;

        public int Lane { get; private set; }
        public float PathZ => _pathZ;
        public float Speed => _speed;

        public static OncomingCar Spawn(Transform parent, PlayerController player, float startZ, int lane,
            float laneWidth, float speed, System.Random rng)
        {
            var go = new GameObject("Obstacle_OncomingCar");
            go.transform.SetParent(parent, false);
            var car = go.AddComponent<OncomingCar>();
            car._player = player;
            car._pathZ = startZ;
            car.Lane = lane;
            car._lateral = lane * laneWidth;
            car._speed = speed;
            car._bobPhase = (float)rng.NextDouble() * 6.28f;

            // Faces the player: the model's +Z is its nose, and it drives toward -Z.
            go.transform.SetPositionAndRotation(RoadPlacement.OnRoad(startZ, car._lateral),
                DownhillPath.Rotation * Quaternion.Euler(0f, 180f, 0f));

            Color paint = Paints[rng.Next(Paints.Length)];
            car.BuildVisual(paint);

            // Moving trigger volumes need a kinematic body so enter/exit events fire
            // reliably against the player's rigidbody.
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var hard = new GameObject("HardHit");
            hard.transform.SetParent(go.transform, false);
            hard.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var hardCol = hard.AddComponent<BoxCollider>();
            hardCol.isTrigger = true;
            hardCol.size = new Vector3(1.4f, 1.3f, 2.7f);
            var hazard = hard.AddComponent<ObstacleHazard>();

            var near = new GameObject("NearMiss");
            near.transform.SetParent(go.transform, false);
            near.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var nearCol = near.AddComponent<BoxCollider>();
            nearCol.isTrigger = true;
            nearCol.size = new Vector3(3.1f, 2f, 3.9f);
            var zone = near.AddComponent<NearMissZone>();
            zone.Configure(25, lane);
            hazard.BindNearMiss(zone);

            BlobShadow.Attach(go.transform, 1.1f);
            return car;
        }

        private void BuildVisual(Color paint)
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            // A touch over lane scale so it reads from 80 m out, before the horn.
            _body.localScale = Vector3.one * 1.2f;

            var bodyMat = CoastMaterials.CreateLit(paint, 0.35f);
            var darkMat = CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.55f));
            var glassMat = CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.SkyBlue, Color.white, 0.45f), 0.6f);
            var lampMat = CoastMaterials.CreateUnlit(new Color(1f, 0.96f, 0.75f));

            // Lower body, cabin, hood — a stubby kei-van silhouette.
            Box(_body, "Chassis", new Vector3(0f, 0.42f, 0f), new Vector3(1.3f, 0.5f, 2.5f), bodyMat);
            Box(_body, "Cabin", new Vector3(0f, 0.92f, -0.25f), new Vector3(1.15f, 0.55f, 1.4f), bodyMat);
            Box(_body, "Hood", new Vector3(0f, 0.72f, 0.85f), new Vector3(1.2f, 0.16f, 0.75f), bodyMat);
            Box(_body, "Windshield", new Vector3(0f, 0.95f, 0.47f), new Vector3(1.0f, 0.42f, 0.06f), glassMat);
            Box(_body, "RearGlass", new Vector3(0f, 0.95f, -0.97f), new Vector3(1.0f, 0.36f, 0.05f), glassMat);
            Box(_body, "Bumper", new Vector3(0f, 0.28f, 1.27f), new Vector3(1.32f, 0.16f, 0.1f), darkMat);
            Box(_body, "Grille", new Vector3(0f, 0.5f, 1.26f), new Vector3(0.5f, 0.16f, 0.05f), darkMat);
            Box(_body, "Roof", new Vector3(0f, 1.2f, -0.25f), new Vector3(1.05f, 0.05f, 1.3f),
                CoastMaterials.CreateLit(Color.Lerp(paint, Color.white, 0.35f)));

            // Headlights: unlit, so they glow in the shadowed side of a curve.
            Box(_body, "LampL", new Vector3(-0.42f, 0.55f, 1.27f), new Vector3(0.24f, 0.16f, 0.05f), lampMat);
            Box(_body, "LampR", new Vector3(0.42f, 0.55f, 1.27f), new Vector3(0.24f, 0.16f, 0.05f), lampMat);

            _wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.62f : 0.62f;
                float z = (i < 2) ? 0.8f : -0.8f;
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(_body, false);
                wheel.transform.localPosition = new Vector3(x, 0.25f, z);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                Object.Destroy(wheel.GetComponent<Collider>());
                wheel.GetComponent<Renderer>().sharedMaterial = darkMat;
                _wheels[i] = wheel.transform;
            }
        }

        private static void Box(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _pathZ -= _speed * dt;
            transform.position = RoadPlacement.OnRoad(_pathZ, _lateral);

            if (_wheels != null)
            {
                float spin = _speed * dt / 0.25f * Mathf.Rad2Deg;
                for (int i = 0; i < _wheels.Length; i++)
                    if (_wheels[i] != null)
                        _wheels[i].Rotate(0f, spin, 0f, Space.Self);
            }

            if (_body != null)
            {
                float bob = Mathf.Sin(Time.time * 9f + _bobPhase) * 0.012f;
                _body.localPosition = new Vector3(0f, bob, 0f);
                _body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 7f + _bobPhase) * 0.6f, 0f, 0f);
            }

            if (_player == null)
                return;

            float playerZ = _player.PathDistance;
            float closing = Mathf.Max(1f, _speed + _player.Speed);
            float seconds = (_pathZ - playerZ) / closing;

            if (!_honked && seconds < 1.6f)
            {
                _honked = true;
                CoastAudioManager.Instance?.PlaySfx(CoastSfx.Horn);
            }

            if (_pathZ < playerZ - 8f)
                Destroy(gameObject);
        }
    }
}
