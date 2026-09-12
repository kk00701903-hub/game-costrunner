using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// 51차(사용자): K-POP 러닝 난이도 — 챕터가 오를수록 **가속 구간**과 **보스**가 들어온다. 더보기 › 보스전은 보스만 연달아.
    ///   보스 3종(그림 Obs_Boss_*, 앞 14 m 에서 떠다니며 따라옴):
    ///     Seagull  갈매기 해적 — 앞에서 미사일(Obs_Missile)을 레인 따라 쏜다(주인공 레인 60%).
    ///     Golem    돌하르방 골렘 — 하늘에서 바위(SkyHazards.DropRock)를 끝없이 떨어뜨린다.
    ///     Dokkaebi 태풍 도깨비 — 예측 불가하게 레인을 옮겨 다니는 태풍(SkyHazards.SpawnTornado)을 보낸다.
    ///   보스가 있는 동안 장애물 스포너는 멈추고(SetSuppressed) 코인·말랑이 패턴도 쉰다(SpawnHold) — 「보스 등장 시 동전·장애물 최소화」.
    ///   난이도(챕터, 52차): 1~5 보스 없음 / 6챕터부터 1회, 3챕터마다 +1(6~8:1 · 9~11:2 · 12~14:3 · 15~17:4 · 18+:5), 4챕터부터 가속 구간(6초·×1.35), 보스 종류는 난이도 가중 랜덤.
    ///   52차(사용자): 보스는 더 멀리(28 m)·더 높이(5.5 m)·3배 크기. 퇴치 1마리 = 100코인(+400점). 보스 중엔 감속이 없다 — 오히려 ×1.2 로 빨라지고(PlayerController.NoHitSlow) 끝나면 서서히 원래대로.
    public class BossDirector : MonoBehaviour
    {
        public enum Kind { Seagull = 0, Golem = 1, Dokkaebi = 2 }

        public static BossDirector Instance { get; private set; }
        /// 보스가 화면에 있는 동안 true — 코인·말랑이·바위비가 이걸 보고 쉰다.
        public static bool Active { get; private set; }
        public static bool SpawnHold => Active;
        public static Kind Current { get; private set; }

        private PlayerController _player;
        private ObstacleSpawner _obstacles;
        private System.Random _rng;
        private Transform _boss, _bossVis, _bossBlob;
        private float _bossT, _bossLen, _attackT, _lat, _latTarget, _appear;
        private int _chapter;
        private bool _rush;
        private readonly System.Collections.Generic.List<float> _bossAt = new System.Collections.Generic.List<float>();   // 곡 시각(초)
        private int _bossIdx;
        private float _surgeNext, _surgeEnd;
        private bool _surging;
        private Coroutine _co;

        public static BossDirector Create(PlayerController player, ObstacleSpawner obstacles, int chapter, bool bossRush, int seed)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("BossDirector");
            var d = go.AddComponent<BossDirector>();
            d._player = player; d._obstacles = obstacles; d._chapter = Mathf.Max(1, chapter); d._rush = bossRush;
            d._rng = new System.Random(seed);
            d.Plan();
            return d;
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; Active = false; if (_player != null && Mathf.Approximately(_player.SpeedBoost, _applied)) _player.SpeedBoost = 1f; }

        /// 곡 창(길이 L)에 보스 시각을 배치. 보스전은 3초 뒤부터 20초마다 계속.
        private void Plan()
        {
            _bossAt.Clear();
            // 55차: 스토리 대회(보스 퇴치전)에서도 쓴다 — 곡이 없으면 대회 제한시간의 70 % 창에 배치.
            float L = ArcadeRun.KpopMode ? ArcadeRun.KpopTrack.length : StoryContest.Active ? StoryContest.Current.seconds * 0.7f : 150f;
            if (_rush)
            {
                for (float t = 3f; t < L - 12f; t += 26f) _bossAt.Add(t);
                _surgeNext = float.MaxValue;
                return;
            }
            int n = _chapter < 6 ? 0 : Mathf.Clamp(1 + (_chapter - 6) / 3, 1, 5);
            for (int i = 0; i < n; i++) _bossAt.Add(L * (0.22f + 0.66f * (i + 0.5f) / n) - 9f);
            _surgeNext = _chapter >= 4 ? 18f + (float)_rng.NextDouble() * 8f : float.MaxValue;
        }

        private float Elapsed => ArcadeRun.KpopMode ? ArcadeRun.KpopElapsed : Time.timeSinceLevelLoad;

        // 52차: 속도 부스트를 내가 관리 — 가속 구간 1.35 / 보스 중 1.2 / 평소 1.0, 올릴 땐 빠르게·내릴 땐 천천히(뚝 떨어지는 「느려짐」 없이).
        private float _boost = 1f, _applied = 1f, _fxNext;
        private void DriveBoost(float dt)
        {
            float want = _surging ? 1.35f : Active ? 1.4f : 1f;   // 63차(사용자): 보스 중 「느려지는」 느낌 → 더 확실히 빠르게(1.2 → 1.4)
            _boost = Mathf.MoveTowards(_boost, want, dt * (want > _boost ? 0.9f : 0.08f));
            float cur = _player.SpeedBoost;
            if (Mathf.Approximately(cur, _applied) || cur < _boost) _player.SpeedBoost = _boost;   // 다른 주체(보너스타임·빨래줄)가 더 올려 둔 값은 존중
            _applied = _player.SpeedBoost;
        }

        private void Update()
        {
            if (_player == null || !_player.enabled) return;
            float e = Elapsed;
            DriveBoost(Time.deltaTime);
            // 63차(사용자): 보스 중엔 코인·장애물이 줄어 속도감이 죽는다 → 속도선·FOV 를 계속 밀어 「더 빨라졌다」가 보이게
            if (Active && (_fxNext -= Time.deltaTime) <= 0f)
            {
                _fxNext = 0.3f;
                var j = JuiceDirector.Instance;
                if (j != null) j.BossSpeedPulse();
            }
            // 가속 구간
            if (!_surging && !Active && e >= _surgeNext)
            {
                _surging = true; _surgeEnd = e + 6f;
                PickupFloat.Banner(Loc.T("가속 구간!", "SPEED UP!"), new Color(0.4f, 0.9f, 1f), 1.2f);
                JuiceDirector.Instance?.OnJumpPad(_player.transform.position);
                _surgeNext = e + 22f + (float)_rng.NextDouble() * 10f;
            }
            if (_surging && e >= _surgeEnd) _surging = false;   // 부스트는 DriveBoost 가 천천히 내린다
            // 보스 등장
            if (!Active && _bossIdx < _bossAt.Count && e >= _bossAt[_bossIdx])
            {
                _bossIdx++;
                var kind = PickKind();
                _co = StartCoroutine(BossPhase(kind, _rush ? 20f : 16f + Mathf.Min(6f, _chapter * 0.4f)));
            }
            if (Active && _boss != null) FollowBoss();
        }

        private Kind PickKind()
        {
            // 난이도 가중: 낮은 챕터는 갈매기(피하기 쉬움), 높을수록 골렘·도깨비
            int r = _rng.Next(100);
            if (_chapter < 9) return r < 60 ? Kind.Seagull : r < 85 ? Kind.Golem : Kind.Dokkaebi;
            if (_chapter < 14) return r < 35 ? Kind.Seagull : r < 70 ? Kind.Golem : Kind.Dokkaebi;
            return r < 25 ? Kind.Seagull : r < 55 ? Kind.Golem : Kind.Dokkaebi;
        }

        private static string Name(Kind k) => k switch { Kind.Seagull => Loc.T("갈매기 해적", "Seagull Pirate"), Kind.Golem => Loc.T("돌하르방 골렘", "Hareubang Golem"), _ => Loc.T("태풍 도깨비", "Typhoon Dokkaebi") };
        /// 65차: 보스마다 뭘 하는지 한 줄 — 등장 띠에 표시
        private static string Effect(Kind k) => k switch
        {
            Kind.Seagull => Loc.T("앞에서 미사일을 쏜다(내 레인 60%) — 레인을 옮겨 피하자!\n보스 동안 코인·장애물 없음 · 버티면 +100 코인", "Fires missiles down the lanes (60% yours) — switch lanes!\nNo coins/obstacles meanwhile · survive for +100 coins"),
            Kind.Golem => Loc.T("하늘에서 바위가 끝없이 떨어진다 — 바닥 그림자를 보고 비키자!\n보스 동안 코인·장애물 없음 · 버티면 +100 코인", "Rocks keep falling — watch the ground shadows!\nNo coins/obstacles meanwhile · survive for +100 coins"),
            _ => Loc.T("태풍이 예측 못 하게 레인을 옮겨 다닌다 — 닿으면 피해!\n보스 동안 코인·장애물 없음 · 버티면 +100 코인", "A typhoon wanders across lanes — don't touch it!\nNo coins/obstacles meanwhile · survive for +100 coins"),
        };
        private static string Art(Kind k) => k switch { Kind.Seagull => "Boss_Seagull", Kind.Golem => "Boss_Golem", _ => "Boss_Dokkaebi" };

        private IEnumerator BossPhase(Kind kind, float seconds)
        {
            Active = true; Current = kind;
            _obstacles?.SetSuppressed(true);
            PickupFloat.Banner(Name(kind), new Color(1f, 0.35f, 0.35f), 1.6f);   // 배너는 8자 안팎만 들어간다 — 이름만
            PickupFloat.InfoStrip("Obs_" + Art(kind), Name(kind), Effect(kind), new Color(1f, 0.55f, 0.45f), 3.2f);   // 65차(사용자): 보스 효과 설명
            CoastAudioManager.PlayAnywhere(CoastSfx.Horn, 0.8f);
            JuiceDirector.Instance?.PlayHitImpact();
            // 보스 본체
            _boss = new GameObject("Boss_" + kind).transform;
            _boss.SetParent(SkyHazards.Root, false);
            _bossVis = SkyHazards.Visual(_boss, Art(kind), kind == Kind.Golem ? 7.8f : 6.9f, new Color(0.5f, 0.2f, 0.6f), outline: true);   // 52차: 3배
            // 떠 있는 보스의 바닥 그림자(직접 놓는 소프트 원판 — BlobShadow 는 호스트 높이를 따라가 버림)
            var sq = GameObject.CreatePrimitive(PrimitiveType.Quad); sq.name = "BossShadow"; Destroy(sq.GetComponent<Collider>());
            sq.transform.SetParent(SkyHazards.Root, false); sq.transform.rotation = Quaternion.Euler(90f, 0f, 0f); sq.transform.localScale = new Vector3(5.5f, 5.5f, 1f);
            var sr = sq.GetComponent<Renderer>(); sr.sharedMaterial = CoastMaterials.CreateTexturedTransparentCurved(BlobShadow.SoftDisc(), new Color(0.08f, 0.05f, 0.05f, 0.35f), additive: false); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bossBlob = sq.transform;
            _appear = 0f; _bossT = 0f; _bossLen = seconds; _attackT = 0.9f; _lat = 0f; _latTarget = 0f;
            while (_bossT < _bossLen && _player != null && _player.enabled)
            {
                float dt = Time.deltaTime; _bossT += dt; _appear = Mathf.Min(1f, _appear + dt * 1.2f);
                _attackT -= dt;
                if (_attackT <= 0f) { Attack(kind); _attackT = AttackGap(kind); }
                yield return null;
            }
            // 퇴장: 위로 날아가며 사라짐(Active 를 먼저 꺼서 FollowBoss 가 붙잡지 않게)
            Active = false;
            if (_bossBlob != null) Destroy(_bossBlob.gameObject);
            var leaving = _boss; _boss = null; _bossBlob = null;
            float t = 0f;
            while (t < 0.8f && leaving != null)
            {
                t += Time.deltaTime;
                if (_player != null) leaving.position = RoadPlacement.OnRoad(_player.PathDistance + Ahead + t * 30f, _lat, Hover + t * 12f);
                yield return null;
            }
            if (leaving != null) Destroy(leaving.gameObject);
            _obstacles?.SetSuppressed(false);
            PickupFloat.Banner(Loc.T("보스 퇴치!", "BOSS CLEARED!"), new Color(1f, 0.9f, 0.4f), 1.2f);
            CoastAudioManager.PlayAnywhere(CoastSfx.RankS, 0.7f);
            ArcadeRun.NoteBossCleared();
            StoryContest.NoteBoss();   // 55차: 대회(보스 퇴치전) 진행
            // 52차(사용자): 보스 1마리 = 100코인
            var wallet = Object.FindAnyObjectByType<CoinWallet>();
            if (wallet != null) wallet.Add(BossCoins);
            PickupFloat.Banner(Loc.T($"+{BossCoins} 코인", $"+{BossCoins} coins"), new Color(1f, 0.85f, 0.3f), 1.0f);
        }
        public const int BossCoins = 100;
        /// 52차: 보스 위치 — 주인공 앞 28 m, 높이 5.5 m (전엔 14 m·2 m 로 너무 가까웠다).
        public const float Ahead = 28f, Hover = 5.5f;

        private float AttackGap(Kind k)
        {
            float d = Mathf.Clamp01((_chapter - 6) / 12f);   // 챕터가 오를수록 촘촘
            switch (k)
            {
                case Kind.Seagull: return Mathf.Lerp(1.7f, 1.0f, d);
                case Kind.Golem: return Mathf.Lerp(1.5f, 0.9f, d);
                default: return Mathf.Lerp(3.2f, 2.0f, d);
            }
        }

        private void Attack(Kind k)
        {
            if (_player == null) return;
            float z = _player.PathDistance, v = Mathf.Max(6f, _player.Speed);
            int myLane = _player.Lane;
            switch (k)
            {
                case Kind.Seagull:
                {
                    int lane = _rng.Next(100) < 60 ? myLane : _rng.Next(3) - 1;
                    SkyHazards.FireMissile(z + 40f, lane, v + 13f);
                    Bob(0.3f);
                    break;
                }
                case Kind.Golem:
                {
                    int lane = _rng.Next(100) < 50 ? myLane : _rng.Next(3) - 1;
                    float lead = 1.05f;
                    float zz = z + v * (lead + 0.25f) + 5f;
                    SkyHazards.DropRock(zz, lane, lead, 0.62f);
                    if (_chapter >= 8 && _rng.Next(100) < 40) SkyHazards.DropRock(zz + 3f, (lane + 1 + _rng.Next(2)) % 3 - 1, lead + 0.2f, 0.55f);
                    Bob(0.5f);
                    break;
                }
                default:
                {
                    float amp = SkyHazards.LaneWidth * (0.6f + (float)_rng.NextDouble() * 0.5f);
                    float hz = 0.25f + (float)_rng.NextDouble() * 0.35f;
                    SkyHazards.SpawnTornado(z + 45f, v * 0.55f + 6f, amp, hz, 7f);
                    Bob(0.6f);
                    break;
                }
            }
        }

        private float _bob;
        private void Bob(float amount) => _bob = amount;

        /// 보스는 주인공 앞 Ahead m, 높이 Hover m 에서 떠다닌다. 가끔 레인을 옮긴다.
        private void FollowBoss()
        {
            if (_boss == null || _player == null) return;
            if (_rng.Next(1000) < 8) _latTarget = (_rng.Next(3) - 1) * SkyHazards.LaneWidth * 0.8f;
            _lat = Mathf.MoveTowards(_lat, _latTarget, Time.deltaTime * 2.2f);
            _bob = Mathf.MoveTowards(_bob, 0f, Time.deltaTime * 1.5f);
            float hover = Hover + Mathf.Sin(Time.time * 2.1f) * 0.45f + _bob * 2f;
            float ahead = Mathf.Lerp(60f, Ahead, 1f - (1f - _appear) * (1f - _appear));
            _boss.position = RoadPlacement.OnRoad(_player.PathDistance + ahead, _lat, hover);
            if (_bossBlob != null) _bossBlob.position = RoadPlacement.OnRoad(_player.PathDistance + ahead, _lat, 0.03f);
        }
    }
}
