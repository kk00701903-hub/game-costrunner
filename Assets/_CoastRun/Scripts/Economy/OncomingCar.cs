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

        public enum Kind { Van, Bus, Orange, Scooter }   // 14차-2 Scooter: 킥보드 탄 아이(목표 이미지), 좁고 낮아 옆으로 피하거나 점프
        public Kind VehicleKind { get; private set; }

        public static OncomingCar Spawn(Transform parent, PlayerController player, float startZ, int lane,
            float laneWidth, float speed, System.Random rng, Kind kind = Kind.Van)
        {
            var go = new GameObject(kind == Kind.Bus ? "Obstacle_OncomingBus" : kind == Kind.Orange ? "Obstacle_RollingOrange" : kind == Kind.Scooter ? "Obstacle_KickScooter" : "Obstacle_OncomingCar");
            go.transform.SetParent(parent, false);
            var car = go.AddComponent<OncomingCar>();
            car._player = player;
            car._pathZ = startZ;
            car.Lane = lane;
            car._lateral = lane * laneWidth;
            car._speed = speed;
            car._bobPhase = (float)rng.NextDouble() * 6.28f;
            car.VehicleKind = kind;

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
            hardCol.size = kind == Kind.Bus ? new Vector3(1.7f, 2.4f, 5.5f) : kind == Kind.Orange ? new Vector3(0.9f, 0.6f, 0.9f)
                         : kind == Kind.Scooter ? new Vector3(0.8f, 1.5f, 1.4f) : new Vector3(1.4f, 1.3f, 2.7f);
            if (kind == Kind.Bus) hard.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            if (kind == Kind.Orange) hard.transform.localPosition = new Vector3(0f, 0.3f, 0f);   // 낮아서 점프로 넘는다
            var hazard = hard.AddComponent<ObstacleHazard>();
            // 22차-7: 피해 밸런스 — 버스 한 방, 차 두 방(일반 장애물 세 방), 킥보드·귤은 일반보다 가볍게
            hazard.DamageMul = kind == Kind.Bus ? 99f : kind == Kind.Van ? 1.6f : kind == Kind.Scooter ? 0.8f : 0.7f;

            var near = new GameObject("NearMiss");
            near.transform.SetParent(go.transform, false);
            near.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var nearCol = near.AddComponent<BoxCollider>();
            nearCol.isTrigger = true;
            nearCol.size = kind == Kind.Bus ? new Vector3(3.4f, 3f, 6.5f) : kind == Kind.Orange ? new Vector3(2.6f, 1.6f, 2.2f)
                         : kind == Kind.Scooter ? new Vector3(2.6f, 2.2f, 2.6f) : new Vector3(3.1f, 2f, 3.9f);
            var zone = near.AddComponent<NearMissZone>();
            zone.Configure(25, lane);
            hazard.BindNearMiss(zone);

            BlobShadow.Attach(go.transform, kind == Kind.Orange ? 0.6f : kind == Kind.Scooter ? 0.7f : 1.1f);
            HazardRing.Attach(go.transform, kind == Kind.Bus ? 1.3f : 0.8f);
            ObstacleOutline.Attach(go.transform);
            ObstacleWarning.Attach(go);   // 14차-15: 마주 오는 차·버스·킥보드도 레인 경고
            return car;
        }

        private void BuildVisual(Color paint)
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            // A touch over lane scale so it reads from 80 m out, before the horn.
            _body.localScale = Vector3.one * 1.2f;

            // Firefly-painted vehicle (front view, it drives at the camera) when available.
            if (VehicleKind == Kind.Orange)
            {
                // 구르는 귤: 그림이 있으면 그림, 없으면 주황 공. 몸통이 굴러가는 회전은 Update에서.
                _wheels = new Transform[0];
                if (PaintedProp.Available("Orange"))
                {
                    PaintedProp.Attach(_body, "Orange", 0.8f, replace: false);
                    return;
                }
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Ball"; ball.transform.SetParent(_body, false);
                ball.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                ball.transform.localScale = Vector3.one * 0.7f;
                Object.Destroy(ball.GetComponent<Collider>());
                ball.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(1f, 0.6f, 0.15f), 0.3f);
                return;
            }
            if (VehicleKind == Kind.Scooter)
            {
                BuildScooterRider();
                return;
            }
            string key = VehicleKind == Kind.Bus ? "BusFront" : "Van";
            // 22차-2: Kling 정면 그림(택시·버스)을 최우선 — 폴리곤 키트는 그림이 없을 때만.
            if (PaintedProp.Available(key))
            {
                PaintedProp.Attach(_body, key, VehicleKind == Kind.Bus ? 2.6f : 1.8f, replace: false);
                _wheels = new Transform[0];
                return;
            }
            // 14차-11: Blender 3D 버스/밴 — 정면(-Y in Blender = -Z Unity)이 마주 오는 방향(플레이어 쪽).
            string kit = VehicleKind == Kind.Bus ? "Obs3_Bus" : "Obs3_Van";
            if (JejuKit.Load(kit) != null)
            {
                var m = JejuKit.Spawn(kit, _body, Vector3.zero, 0f, 1f);
                if (m != null) { _wheels = new Transform[0]; ObstacleOutline.Attach(_body, 1.03f); return; }
            }
            if (VehicleKind == Kind.Bus)
                _body.localScale = Vector3.one * 1.9f;   // procedural fallback: a bigger box van

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

        /// 킥보드 탄 아이(목표 이미지의 왼쪽 인물): 민트 킥보드 + 노란 헬멧 + 초록 셔츠. 프리미티브.
        private void BuildScooterRider()
        {
            _body.localScale = Vector3.one * 1.0f;
            var mint = CoastMaterials.CreateLit(new Color(0.35f, 0.80f, 0.72f), 0.3f);
            var dark = CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.55f));
            var skin = CoastMaterials.CreateLit(new Color(0.98f, 0.84f, 0.70f), 0.1f);
            var shirt = CoastMaterials.CreateLit(new Color(0.42f, 0.70f, 0.40f), 0.1f);
            var pants = CoastMaterials.CreateLit(new Color(0.30f, 0.42f, 0.66f), 0.1f);
            var helmet = CoastMaterials.CreateLit(new Color(1.0f, 0.82f, 0.30f), 0.4f);
            var hair = CoastMaterials.CreateLit(new Color(0.30f, 0.20f, 0.14f), 0.1f);
            // 킥보드
            Box(_body, "Deck", new Vector3(0f, 0.16f, 0f), new Vector3(0.22f, 0.06f, 0.95f), mint);
            Box(_body, "Stem", new Vector3(0f, 0.65f, 0.42f), new Vector3(0.06f, 0.95f, 0.06f), mint);
            Box(_body, "Handle", new Vector3(0f, 1.1f, 0.42f), new Vector3(0.52f, 0.05f, 0.05f), dark);
            _wheels = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel"; wheel.transform.SetParent(_body, false);
                wheel.transform.localPosition = new Vector3(0f, 0.11f, i == 0 ? 0.48f : -0.45f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);
                Object.Destroy(wheel.GetComponent<Collider>());
                wheel.GetComponent<Renderer>().sharedMaterial = dark;
                _wheels[i] = wheel.transform;
            }
            // 아이: 다리 → 몸통 → 머리(헬멧). 한 발은 데크, 한 발은 뒤로 차는 자세.
            Box(_body, "LegL", new Vector3(-0.08f, 0.45f, -0.05f), new Vector3(0.12f, 0.5f, 0.14f), pants);
            var legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "LegR"; legR.transform.SetParent(_body, false);
            legR.transform.localPosition = new Vector3(0.1f, 0.42f, -0.32f);
            legR.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            legR.transform.localScale = new Vector3(0.12f, 0.5f, 0.14f);
            Object.Destroy(legR.GetComponent<Collider>());
            legR.GetComponent<Renderer>().sharedMaterial = pants;
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso"; torso.transform.SetParent(_body, false);
            torso.transform.localPosition = new Vector3(0f, 0.98f, 0.02f);
            torso.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.34f, 0.3f, 0.26f);
            Object.Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().sharedMaterial = shirt;
            Box(_body, "ArmL", new Vector3(-0.2f, 1.02f, 0.2f), new Vector3(0.09f, 0.09f, 0.42f), skin);
            Box(_body, "ArmR", new Vector3(0.2f, 1.02f, 0.2f), new Vector3(0.09f, 0.09f, 0.42f), skin);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; head.transform.SetParent(_body, false);
            head.transform.localPosition = new Vector3(0f, 1.42f, 0.04f);
            head.transform.localScale = Vector3.one * 0.3f;
            Object.Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = skin;
            var hr = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hr.name = "Hair"; hr.transform.SetParent(_body, false);
            hr.transform.localPosition = new Vector3(0f, 1.47f, 0f);
            hr.transform.localScale = new Vector3(0.32f, 0.26f, 0.32f);
            Object.Destroy(hr.GetComponent<Collider>());
            hr.GetComponent<Renderer>().sharedMaterial = hair;
            var hm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hm.name = "Helmet"; hm.transform.SetParent(_body, false);
            hm.transform.localPosition = new Vector3(0f, 1.53f, -0.01f);
            hm.transform.localScale = new Vector3(0.36f, 0.24f, 0.36f);
            Object.Destroy(hm.GetComponent<Collider>());
            hm.GetComponent<Renderer>().sharedMaterial = helmet;
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

            if (VehicleKind == Kind.Orange && _body != null)
            {
                // 굴러오는 느낌: 좌우로 살짝 흔들리며 위아래로 통통.
                _bobPhase += dt * 9f;
                _body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_bobPhase) * 9f);
                _body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase * 0.5f)) * 0.12f, 0f);
            }
            if (!_honked && seconds < 1.6f && VehicleKind != Kind.Orange)
            {
                _honked = true;
                CoastAudioManager.Instance?.PlaySfx(CoastSfx.Horn);
            }

            if (_pathZ < playerZ - 8f)
                Destroy(gameObject);
        }
    }
}
