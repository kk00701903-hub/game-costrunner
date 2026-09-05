using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// v2 펫 3종 — 상점에서 돈으로 사고 장착한다(PetShop). 값은 PetShop.Price 참조.
    public enum PetKind
    {
        None = 0,
        Sparrow = 1,     // 참새: 런닝 돈 획득 ×1.2
        BikerThug = 2,   // 오토바이탄 깡패: 같은 레인 앞 장애물을 대신 부숨 (쿨타임 12 s, 런당 3회)
        WildGoose = 3    // 기러기: 반경 7 m 돈·하트 자동 수집(자석)
    }

    /// 스케이터 옆을 따라다니는 펫 하나. 절차 생성(프리팹 없음); Resources/CoastRun/Obs_Pet_<Kind>.png
    /// (Firefly 스프라이트)가 있으면 빌보드로 대체된다.
    public class PetCompanion : MonoBehaviour
    {
        public const string PrefsKey = "CoastRun.Pet";   // 레거시 키 — v2는 SaveData.equippedPet
        public static readonly string[] Names = { "없음", "참새", "오토바이탄 깡패", "기러기" };
        public static readonly string[] Blurbs =
        {
            "펫 없음",
            "런닝 중 돈 획득량 ×1.2",
            "앞을 막는 장애물을 대신 부숴줌 (쿨타임 12초, 3회)",
            "반경 7 m의 돈과 하트를 자석처럼 끌어모음",
        };

        public const float SparrowCoinMul = 1.2f;
        public const float GooseMagnet = 7f;
        public const float ThugCooldown = 12f;
        public const int ThugCharges = 3;
        public const float ThugReach = 8f;

        /// Extra magnet reach granted by the active pet (metres). Read by pickups.
        public static float MagnetBonus { get; private set; }
        /// Coin value multiplier granted by the active pet. Read by CoinPickup.
        public static float CoinBonus { get; private set; } = 1f;
        public static PetCompanion Instance { get; private set; }

        /// 레거시 셀렉터(타이틀 설정). v2에서는 GameManager 세이브가 우선한다.
        public static PetKind Selected
        {
            get
            {
                if (GameManager.Active) return GameManager.I.Save.equippedPet;
                return (PetKind)Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, 0), 0, 3);
            }
            set { PlayerPrefs.SetInt(PrefsKey, (int)value); PlayerPrefs.Save(); }
        }

        private PlayerController _player;
        private HealthSystem _health;
        private PetKind _kind;
        private Transform _body;
        private Transform _wingL, _wingR, _wheelF, _wheelB;
        private Vector3 _offset;
        private Vector3 _vel;
        private float _phase;
        private float _thugReadyAt;
        private int _thugUsed;
        private float _dashT = -1f;
        private Vector3 _dashFrom;
        private Transform _dashTarget;

        public PetKind Kind => _kind;
        public int ThugChargesLeft => Mathf.Max(0, ThugCharges - _thugUsed);
        public float ThugCooldownLeft => Mathf.Max(0f, _thugReadyAt - Time.time);

        public static PetCompanion Create(PlayerController player, HealthSystem health)
        {
            var kind = GameManager.Active ? GameManager.I.Save.equippedPet : Selected;
            if (kind == PetKind.None)
                return null;
            var go = new GameObject("Pet");
            var pet = go.AddComponent<PetCompanion>();
            pet.Init(player, health, kind);
            return pet;
        }

        private void Init(PlayerController player, HealthSystem health, PetKind kind)
        {
            _player = player;
            _health = health;
            _kind = kind;
            _phase = Random.value * 6.28f;
            Instance = this;

            MagnetBonus = kind == PetKind.WildGoose ? GooseMagnet : 0f;
            CoinBonus = kind == PetKind.Sparrow ? SparrowCoinMul : 1f;

            // 새들은 바다 쪽 공중, 깡패는 스쿠터로 뒤쪽 레인 옆을 달린다.
            switch (kind)
            {
                case PetKind.Sparrow: _offset = new Vector3(1.1f, 1.7f, -0.3f); break;
                case PetKind.WildGoose: _offset = new Vector3(1.5f, 2.2f, -0.6f); break;
                default: _offset = new Vector3(-1.6f, 0.3f, -1.4f); break;
            }
            Build();
            if (_player != null)
                transform.position = _player.transform.position + _player.PathRotation * _offset;
        }

        /// 새 스테이지: 깡패 횟수 리셋.
        public void ResetForStage()
        {
            _thugUsed = 0;
            _thugReadyAt = 0f;
            _dashT = -1f;
        }

        private void OnDestroy()
        {
            MagnetBonus = 0f;
            CoinBonus = 1f;
            if (Instance == this) Instance = null;
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
            if (PaintedProp.Available("Pet_" + _kind))
            {
                float h = _kind == PetKind.BikerThug ? 1.2f : _kind == PetKind.WildGoose ? 0.8f : 0.45f;
                PaintedProp.Attach(_body, "Pet_" + _kind, h, replace: false);
                return;
            }
            switch (_kind)
            {
                case PetKind.Sparrow: BuildBird(0.55f, new Color(0.62f, 0.45f, 0.30f), new Color(0.95f, 0.88f, 0.75f)); break;
                case PetKind.WildGoose: BuildBird(1.0f, new Color(0.55f, 0.50f, 0.45f), new Color(0.92f, 0.92f, 0.90f)); break;
                default: BuildThug(); break;
            }
        }

        private void BuildBird(float scale, Color back, Color belly)
        {
            var root = new GameObject("Bird").transform;
            root.SetParent(_body, false);
            root.localScale = Vector3.one * scale;
            Part(root, "Torso", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.36f, 0.28f, 0.5f), back);
            Part(root, "Belly", PrimitiveType.Sphere, new Vector3(0f, -0.06f, 0.02f), new Vector3(0.3f, 0.2f, 0.4f), belly);
            Part(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.26f), Vector3.one * 0.22f, back);
            Part(root, "Beak", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0.4f), new Vector3(0.06f, 0.05f, 0.14f), CoastPalette.AccentOrange);
            Part(root, "EyeL", PrimitiveType.Sphere, new Vector3(-0.07f, 0.2f, 0.33f), Vector3.one * 0.05f, Color.black);
            Part(root, "EyeR", PrimitiveType.Sphere, new Vector3(0.07f, 0.2f, 0.33f), Vector3.one * 0.05f, Color.black);
            _wingL = Part(root, "WingL", PrimitiveType.Cube, new Vector3(-0.32f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.26f), back).transform;
            _wingR = Part(root, "WingR", PrimitiveType.Cube, new Vector3(0.32f, 0.04f, 0f), new Vector3(0.5f, 0.04f, 0.26f), back).transform;
        }

        private void BuildThug()
        {
            Color red = new Color(0.85f, 0.22f, 0.25f);
            Color dark = new Color(0.16f, 0.16f, 0.2f);
            Color skin = new Color(0.96f, 0.80f, 0.66f);
            Color jacket = new Color(0.12f, 0.12f, 0.16f);
            // 스쿠터
            Part(_body, "Deck", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.36f, 0.16f, 1.1f), red);
            Part(_body, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.25f), new Vector3(0.3f, 0.12f, 0.45f), dark);
            Part(_body, "Handle", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.45f), new Vector3(0.5f, 0.05f, 0.05f), dark);
            Part(_body, "Stem", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.45f), new Vector3(0.06f, 0.4f, 0.06f), dark);
            _wheelF = Part(_body, "WheelF", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0.5f), new Vector3(0.36f, 0.06f, 0.36f), dark).transform;
            _wheelF.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _wheelB = Part(_body, "WheelB", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, -0.5f), new Vector3(0.36f, 0.06f, 0.36f), dark).transform;
            _wheelB.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // 라이더
            Part(_body, "Legs", PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.05f), new Vector3(0.3f, 0.26f, 0.3f), dark);
            Part(_body, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.95f, -0.15f), new Vector3(0.36f, 0.3f, 0.3f), jacket);
            Part(_body, "ArmL", PrimitiveType.Capsule, new Vector3(-0.2f, 0.9f, 0.15f), new Vector3(0.1f, 0.22f, 0.1f), jacket).transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            Part(_body, "ArmR", PrimitiveType.Capsule, new Vector3(0.2f, 0.9f, 0.15f), new Vector3(0.1f, 0.22f, 0.1f), jacket).transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            Part(_body, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.3f, -0.12f), Vector3.one * 0.3f, skin);
            Part(_body, "Helmet", PrimitiveType.Sphere, new Vector3(0f, 1.36f, -0.14f), new Vector3(0.34f, 0.26f, 0.34f), red);
            Part(_body, "Shades", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.02f), new Vector3(0.28f, 0.07f, 0.06f), Color.black);
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

        private void Update()
        {
            if (_kind == PetKind.BikerThug)
                UpdateThug();
        }

        /// 같은 레인, 1.5~8 m 앞의 부술 수 있는 장애물을 찾아 돌진해 부순다.
        private void UpdateThug()
        {
            if (_player == null || _thugUsed >= ThugCharges || Time.time < _thugReadyAt || _dashT >= 0f)
                return;
            float pz = _player.PathDistance;
            ObstacleHazard best = null;
            float bestD = float.MaxValue;
            foreach (var hz in ObstacleHazard.Active)
            {
                if (hz == null || !hz.Breakable) continue;
                float d = DownhillPath.DistanceAlong(hz.transform.position) - pz;
                if (d < 1.5f || d > ThugReach) continue;
                float lateral = Vector3.Dot(hz.transform.position - _player.transform.position, _player.PathRotation * Vector3.right);
                if (Mathf.Abs(lateral) > 1.3f) continue;   // 다른 레인
                if (d < bestD) { bestD = d; best = hz; }
            }
            if (best == null) return;

            _thugUsed++;
            _thugReadyAt = Time.time + ThugCooldown;
            _dashT = 0f;
            _dashFrom = transform.position;
            _dashTarget = best.transform;
            RunHudChrome.Instance?.ShowToast($"깡패 출동! ({ThugChargesLeft}회 남음)");
        }

        private void LateUpdate()
        {
            if (_player == null)
                return;

            float dt = Time.deltaTime;
            Quaternion frame = _player.PathRotation;
            Vector3 anchor = _player.transform.position - Vector3.up * _player.BodyHalfHeight;
            Vector3 target = anchor + frame * _offset;

            if (_dashT >= 0f)
            {
                // 돌진 연출: 0.35 s에 장애물까지, 닿으면 부수고 복귀.
                _dashT += dt / 0.35f;
                if (_dashTarget != null)
                {
                    Vector3 end = _dashTarget.position;
                    transform.position = Vector3.Lerp(_dashFrom, end, Mathf.Clamp01(_dashT));
                    if (_dashT >= 1f)
                    {
                        var hz = _dashTarget.GetComponent<ObstacleHazard>();
                        if (hz != null) hz.Smash();
                        _dashTarget = null;
                    }
                }
                else if (_dashT >= 1f)
                {
                    _dashT = -1f;
                    _vel = Vector3.zero;
                }
                transform.rotation = frame;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, target, ref _vel, 0.18f);
            transform.rotation = frame;

            bool bird = _kind != PetKind.BikerThug;
            _phase += dt * (bird ? 9f : 14f);
            if (_body != null)
            {
                float bob = bird ? Mathf.Sin(_phase * 0.5f) * 0.12f : Mathf.Abs(Mathf.Sin(_phase)) * 0.03f;
                _body.localPosition = new Vector3(0f, bob, 0f);
                _body.localRotation = Quaternion.Euler(bird ? 0f : Mathf.Sin(_phase) * 2f, 0f, 0f);
            }
            if (_wingL != null && _wingR != null)
            {
                float flap = Mathf.Sin(_phase) * (_kind == PetKind.Sparrow ? 45f : 28f);
                _wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                _wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
            if (_wheelF != null)
            {
                float spin = _player.Speed * dt / 0.18f * Mathf.Rad2Deg;
                _wheelF.Rotate(0f, spin, 0f, Space.Self);
                _wheelB.Rotate(0f, spin, 0f, Space.Self);
            }
        }
    }
}
