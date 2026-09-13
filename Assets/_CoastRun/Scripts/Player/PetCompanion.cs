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

            // 73차(사용자): 오토바이(팡찌)만 주인공 **오른쪽 바닥**을 같이 달리고, 나머지(참새·흑돼지·기러기)는 **오른쪽 위**에 둥둥 떠다닌다.
            //   크기는 주인공의 35 %(Build 의 높이). 14차-8: 뒤(-z)에 두면 카메라에 가까워 거대하게 잘려 보이니 살짝 앞(+z).
            switch (kind)
            {
                case PetKind.BikerThug: _offset = new Vector3(1.0f, 0f, 0.6f); break;   // 살짝 앞에서 나란히
                default: _offset = new Vector3(0.82f, 1.55f, 0.35f); break;
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
                // 73차(사용자): 주인공(≈1.6 m)의 35 % ≈ 0.56 m. 오토바이는 바퀴까지라 살짝 크게.
                float h = _kind == PetKind.BikerThug ? 0.64f : 0.56f;
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
            {
                UpdateThug();
                SmashNearby();
            }
        }

        /// 73차(사용자): 오토바이 펫은 주인공처럼 **몸으로 부딪힌 장애물이 팡 터진다** — 자기 자리(오른쪽 레인)의 부술 수 있는 장애물이 0.8 m 안에 오면 부순다(횟수 제한 없음, 피해 없음).
        private void SmashNearby()
        {
            if (_dashT >= 0f || _body == null) return;
            Vector3 p = transform.position;
            for (int i = ObstacleHazard.Active.Count - 1; i >= 0; i--)
            {
                var hz = ObstacleHazard.Active[i];
                if (hz == null || !hz.Breakable) continue;
                Vector3 d = hz.transform.position - p; d.y = 0f;
                if (d.sqrMagnitude > 0.8f * 0.8f) continue;
                hz.Smash();
                CoastPrefs.Vibrate();
            }
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

            // 73차: 떠다니는 펫(오토바이 제외) — 위아래 둥둥(0.12) + 좌우 살랑(0.05) + 날갯짓 스쿼시(4 %, 새) + 기울기 ±5°. 오토바이는 엔진 진동 + 살짝 앞뒤 흔들림.
            bool floating = _kind != PetKind.BikerThug;
            bool bird = _kind == PetKind.Sparrow || _kind == PetKind.WildGoose;
            _phase += dt * (floating ? 8f : 16f);
            if (_body != null)
            {
                if (floating)
                {
                    float bob = Mathf.Sin(_phase * 0.45f) * 0.12f + Mathf.Sin(_phase * 1.1f) * 0.02f;
                    float sway = Mathf.Sin(_phase * 0.3f) * 0.05f;
                    _body.localPosition = new Vector3(sway, bob, 0f);
                    float flap = bird ? 1f + Mathf.Sin(_phase * 1.6f) * 0.04f : 1f + Mathf.Sin(_phase * 0.9f) * 0.02f;
                    _body.localScale = new Vector3(1f / flap, flap, 1f);
                    _body.localRotation = Quaternion.Euler(Mathf.Sin(_phase * 0.45f) * 3f, 0f, Mathf.Sin(_phase * 0.3f) * -5f);
                }
                else
                {
                    _body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_phase)) * 0.015f, 0f);
                    _body.localRotation = Quaternion.Euler(Mathf.Sin(_phase * 0.5f) * 1.5f, 0f, 0f);
                }
            }
        }
    }
}
