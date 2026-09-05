using UnityEngine;

namespace CoastRun
{
    public enum PetKind
    {
        Seagull = 0,   // 갈매기: wider magnet — jellies and coins drift in from further away
        Puppy = 1,     // 강아지: stamina regen — the bar drains slower
        Cat = 2        // 고양이: coin bonus — every coin is worth more
    }

    /// Cookie-Run pet: a small creature that flies/runs beside the skater and gives one
    /// passive. Procedural (no prefab) so it works today; a modelled pet can replace the
    /// visuals by dropping a `Pet_<Kind>` prefab into Resources/CoastRun.
    public class PetCompanion : MonoBehaviour
    {
        public const string PrefsKey = "CoastRun.Pet";
        public static readonly string[] Names = { "갈매기", "강아지", "고양이" };
        public static readonly string[] Blurbs =
        {
            "젤리·코인 자석 범위 +2.5 m",
            "체력이 1초에 1.2씩 회복",
            "코인 가치 ×1.5",
        };

        /// Extra magnet reach granted by the active pet (metres). Read by pickups.
        public static float MagnetBonus { get; private set; }
        /// Coin value multiplier granted by the active pet. Read by CoinPickup.
        public static float CoinBonus { get; private set; } = 1f;

        public static PetKind Selected
        {
            get => (PetKind)Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, 0), 0, 2);
            set { PlayerPrefs.SetInt(PrefsKey, (int)value); PlayerPrefs.Save(); }
        }

        private PlayerController _player;
        private HealthSystem _health;
        private PetKind _kind;
        private Transform _body;
        private Transform _wingL, _wingR, _tail;
        private Vector3 _offset;
        private Vector3 _vel;
        private float _phase;
        private float _regenPerSecond;

        public PetKind Kind => _kind;

        public static PetCompanion Create(PlayerController player, HealthSystem health)
        {
            var go = new GameObject("Pet");
            var pet = go.AddComponent<PetCompanion>();
            pet.Init(player, health, Selected);
            return pet;
        }

        private void Init(PlayerController player, HealthSystem health, PetKind kind)
        {
            _player = player;
            _health = health;
            _kind = kind;
            _phase = Random.value * 6.28f;

            MagnetBonus = kind == PetKind.Seagull ? 2.5f : 0f;
            CoinBonus = kind == PetKind.Cat ? 1.5f : 1f;
            _regenPerSecond = kind == PetKind.Puppy ? 1.2f : 0f;

            // Seagull flies high on the sea side; the ground pets run at the skater's heel.
            _offset = kind == PetKind.Seagull ? new Vector3(1.35f, 1.9f, -0.4f) : new Vector3(-1.25f, 0.35f, -0.9f);
            Build();
            if (_player != null)
                transform.position = _player.transform.position + _player.PathRotation * _offset;
        }

        private void OnDestroy()
        {
            MagnetBonus = 0f;
            CoinBonus = 1f;
        }

        private void Build()
        {
            var prefab = PrefabLibrary.TryInstantiate("Pet_" + _kind, transform, Vector3.zero);
            if (prefab != null)
            {
                _body = prefab.transform;
                RoadPlacement.FitHeight(prefab, 0.6f);
                return;
            }

            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            switch (_kind)
            {
                case PetKind.Seagull: BuildSeagull(); break;
                case PetKind.Puppy: BuildPuppy(); break;
                default: BuildCat(); break;
            }
        }

        private void BuildSeagull()
        {
            Color white = Color.Lerp(Color.white, CoastPalette.TownCream, 0.15f);
            Part(_body, "Torso", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.28f, 0.5f), white);
            Part(_body, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.26f), Vector3.one * 0.22f, white);
            Part(_body, "Beak", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0.4f), new Vector3(0.06f, 0.05f, 0.14f), CoastPalette.AccentOrange);
            Part(_body, "EyeL", PrimitiveType.Sphere, new Vector3(-0.07f, 0.2f, 0.33f), Vector3.one * 0.05f, Color.black);
            Part(_body, "EyeR", PrimitiveType.Sphere, new Vector3(0.07f, 0.2f, 0.33f), Vector3.one * 0.05f, Color.black);
            _wingL = Part(_body, "WingL", PrimitiveType.Cube, new Vector3(-0.32f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.26f), white).transform;
            _wingR = Part(_body, "WingR", PrimitiveType.Cube, new Vector3(0.32f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.26f), white).transform;
            Part(_body, "TipL", PrimitiveType.Cube, new Vector3(-0.55f, 0.04f, 0f), new Vector3(0.1f, 0.04f, 0.2f), CoastPalette.RoadGrey);
            Part(_body, "TipR", PrimitiveType.Cube, new Vector3(0.55f, 0.04f, 0f), new Vector3(0.1f, 0.04f, 0.2f), CoastPalette.RoadGrey);
        }

        private void BuildPuppy()
        {
            Color tan = new Color(0.86f, 0.68f, 0.42f);
            Color dark = new Color(0.45f, 0.32f, 0.2f);
            Part(_body, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.22f, 0f), new Vector3(0.3f, 0.22f, 0.3f), tan).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(_body, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0.24f), Vector3.one * 0.28f, tan);
            Part(_body, "Snout", PrimitiveType.Sphere, new Vector3(0f, 0.31f, 0.38f), new Vector3(0.14f, 0.11f, 0.14f), Color.Lerp(tan, Color.white, 0.4f));
            Part(_body, "Nose", PrimitiveType.Sphere, new Vector3(0f, 0.33f, 0.45f), Vector3.one * 0.05f, Color.black);
            Part(_body, "EarL", PrimitiveType.Cube, new Vector3(-0.15f, 0.42f, 0.2f), new Vector3(0.08f, 0.18f, 0.1f), dark);
            Part(_body, "EarR", PrimitiveType.Cube, new Vector3(0.15f, 0.42f, 0.2f), new Vector3(0.08f, 0.18f, 0.1f), dark);
            for (int i = 0; i < 4; i++)
                Part(_body, "Leg" + i, PrimitiveType.Cube,
                    new Vector3(i % 2 == 0 ? -0.1f : 0.1f, 0.08f, i < 2 ? 0.14f : -0.14f), new Vector3(0.07f, 0.16f, 0.07f), tan);
            _tail = Part(_body, "Tail", PrimitiveType.Cube, new Vector3(0f, 0.32f, -0.28f), new Vector3(0.05f, 0.05f, 0.18f), dark).transform;
        }

        private void BuildCat()
        {
            Color grey = new Color(0.55f, 0.55f, 0.6f);
            Part(_body, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.2f, 0f), new Vector3(0.26f, 0.2f, 0.26f), grey).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(_body, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.34f, 0.22f), Vector3.one * 0.26f, grey);
            Part(_body, "EarL", PrimitiveType.Cube, new Vector3(-0.09f, 0.48f, 0.2f), new Vector3(0.07f, 0.12f, 0.05f), grey).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Part(_body, "EarR", PrimitiveType.Cube, new Vector3(0.09f, 0.48f, 0.2f), new Vector3(0.07f, 0.12f, 0.05f), grey).transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Part(_body, "EyeL", PrimitiveType.Sphere, new Vector3(-0.06f, 0.36f, 0.33f), Vector3.one * 0.05f, new Color(0.3f, 0.9f, 0.5f));
            Part(_body, "EyeR", PrimitiveType.Sphere, new Vector3(0.06f, 0.36f, 0.33f), Vector3.one * 0.05f, new Color(0.3f, 0.9f, 0.5f));
            for (int i = 0; i < 4; i++)
                Part(_body, "Leg" + i, PrimitiveType.Cube,
                    new Vector3(i % 2 == 0 ? -0.09f : 0.09f, 0.07f, i < 2 ? 0.12f : -0.12f), new Vector3(0.06f, 0.14f, 0.06f), grey);
            _tail = Part(_body, "Tail", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.3f), new Vector3(0.04f, 0.04f, 0.3f), grey).transform;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(color);
            return go;
        }

        private void LateUpdate()
        {
            if (_player == null)
                return;

            float dt = Time.deltaTime;
            Quaternion frame = _player.PathRotation;
            // Follow the lane centre rather than the exact lateral, so the pet swings
            // across a beat after the skater instead of teleporting with her.
            Vector3 anchor = _player.transform.position - Vector3.up * _player.BodyHalfHeight;
            Vector3 target = anchor + frame * _offset;
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _vel, 0.18f);
            transform.rotation = frame;

            // Animation: seagull flaps, runners bob and wag.
            _phase += dt * (_kind == PetKind.Seagull ? 9f : 14f);
            if (_body != null)
            {
                float bob = _kind == PetKind.Seagull ? Mathf.Sin(_phase * 0.5f) * 0.12f : Mathf.Abs(Mathf.Sin(_phase)) * 0.08f;
                _body.localPosition = new Vector3(0f, bob, 0f);
                _body.localRotation = Quaternion.Euler(_kind == PetKind.Seagull ? 0f : Mathf.Sin(_phase) * 6f, 0f, 0f);
            }
            if (_wingL != null && _wingR != null)
            {
                float flap = Mathf.Sin(_phase) * 35f;
                _wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                _wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
            if (_tail != null)
                _tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(_phase * 1.5f) * 30f, 0f);

            if (_regenPerSecond > 0f && _health != null && _player.Speed > 0.5f)
                _health.Heal(_regenPerSecond * dt);
        }
    }
}
