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
        WildGoose = 3,   // 기러기: 반경 7 m 돈·하트 자동 수집(자석)
        BlackPig = 4     // 14차 흑돼지: 체력이 바닥나면 런당 1회 40%로 버텨 준다(부활)
    }

    /// 스케이터 옆을 따라다니는 펫 하나. 절차 생성(프리팹 없음); Resources/CoastRun/Obs_Pet_<Kind>.png
    /// (Firefly 스프라이트)가 있으면 빌보드로 대체된다.
    public class PetCompanion : MonoBehaviour
    {
        public const string PrefsKey = "CoastRun.Pet";   // 레거시 키 — v2는 SaveData.equippedPet
        public static readonly string[] Names = { "없음", "참새", "오토바이탄 팡찌", "기러기", "흑돼지" };   // 66차 시안: 깡패 → 팡찌
        public static readonly string[] Blurbs =
        {
            "펫 없음",
            "러닝 중 돈 획득량 ×1.2",
            "앞을 막는 장애물에 대신 부딪힘\n(쿨타임 12초, 3회)",
            "반경 7 m의 돈과 하트를 자석처럼 끌어모음",
            "체력이 바닥나면 한 번 버텨줌\n(턴당 1회, 40% 회복)",
        };
        /// 흑돼지 부활: 런당 1회. HealthSystem 이 바닥날 때 묻는다.
        public static bool TryRevive()
        {
            if (Instance == null || Instance._kind != PetKind.BlackPig || Instance._reviveUsed)
                return false;
            Instance._reviveUsed = true;
            RunHudChrome.Instance?.ShowToast("흑돼지가 버텨줬어!");
            CoastPrefs.Vibrate();
            return true;
        }
        private bool _reviveUsed;

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
                // 14차-8: 펫은 주인공 '옆·살짝 앞'(화면 안). 뒤(-z)에 두면 카메라에 가까워 거대하게 잘려 보였다.
                case PetKind.Sparrow: _offset = new Vector3(0.9f, 1.6f, 0.5f); break;
                case PetKind.WildGoose: _offset = new Vector3(1.15f, 1.9f, 0.4f); break;
                case PetKind.BlackPig: _offset = new Vector3(0.95f, 0f, 0.7f); break;   // 옆에서 종종걸음
                default: _offset = new Vector3(-1.2f, 0.3f, 0.4f); break;
            }
            Build();
            if (_player != null)
                transform.position = _player.transform.position + _player.PathRotation * _offset;
        }

        /// 새 스테이지: 깡패 횟수 리셋.
        /// 24차-3: 골인 연출 동안 펫을 숨긴다(고정 카메라 옆에 끼어들어 프레임을 가렸다).
        public void SetHidden(bool hidden)
        {
            if (_body != null) _body.gameObject.SetActive(!hidden);
        }

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

        /// 66차(사용자): 펫은 Kling 으로 다시 그린 **뒷모습 그림**(Resources/CoastRun/Obs_Pet_<Kind>.png, 마젠타 키잉) 빌보드만 쓴다 —
        ///   옛 Blender FBX(Pet_*.fbx)·절차 조형(BuildBird/Pig/Thug)은 삭제. 그림이 없으면 작은 공 하나(폴백).
        private void Build()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            if (PaintedProp.Available("Pet_" + _kind))
            {
                float h = _kind == PetKind.BikerThug ? 1.15f : _kind == PetKind.WildGoose ? 0.85f : _kind == PetKind.BlackPig ? 0.62f : 0.55f;
                PaintedProp.Attach(_body, "Pet_" + _kind, h, replace: false, outline: true);
                return;
            }
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "PetFallback"; Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(_body, false); ball.transform.localPosition = new Vector3(0f, 0.25f, 0f); ball.transform.localScale = Vector3.one * 0.4f;
            ball.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateToon(new Color(0.95f, 0.8f, 0.3f), null, null, 0.3f);
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

            // 14차-8: 진행 방향은 스무딩 없이 따라간다 — 11 m/s 에서 SmoothDamp 지연(≈2 m)으로 펫이
            // 주인공 뒤·카메라 앞까지 처져 거대하게 잘려 보였다. 좌우·상하만 부드럽게.
            Vector3 tangent = DownhillPath.Tangent;
            Vector3 delta = transform.position - target;
            delta -= tangent * Vector3.Dot(delta, tangent);
            delta = Vector3.SmoothDamp(delta, Vector3.zero, ref _vel, 0.18f);
            transform.position = target + delta;
            transform.rotation = frame;

            bool bird = _kind != PetKind.BikerThug && _kind != PetKind.BlackPig;
            _phase += dt * (bird ? 9f : 14f);
            if (_body != null)
            {
                float bob = bird ? Mathf.Sin(_phase * 0.5f) * 0.12f : Mathf.Abs(Mathf.Sin(_phase)) * 0.03f;
                _body.localPosition = new Vector3(0f, bob, 0f);
                _body.localRotation = Quaternion.Euler(bird ? 0f : Mathf.Sin(_phase) * 2f, 0f, 0f);
            }
        }
    }
}
