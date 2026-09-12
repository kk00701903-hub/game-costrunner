using UnityEngine;

namespace CoastRun
{
    /// Spawns gold/silver coin lines on the road ahead of the player.
    public class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private float spawnAhead = 60f;
        [SerializeField] private float spawnInterval = 14f;  // Gold Run: clear asphalt between coin guides
        [SerializeField] private float laneWidth = 2.2f;

        private float _nextSpawnZ = 22f;
        private Transform _root;
        // 24차-8(점검 3-2): 스테이지 리셋이 없어 재도전 뒤 이전 도달점까지 코인이 0이었고, UnityEngine.Random이라
        // 같은 시드=같은 코스 원칙이 코인에서만 깨졌다. 장애물·젤리와 같은 방식으로 시드 RNG를 쓴다.
        private System.Random _rng = new System.Random(777);

        // 활공 보상 트레일: 플레이어 앞을 계속 채워 하늘에서 먹이가 끊기지 않게.
        private bool _glideTrail;
        private float _glideNextZ;
        private float _glideHeight;
        private int _glideLane;
        private int _glideCount;

        public void ResetForStage(int stageIndex, float startZ)
        {
            _rng = new System.Random(700 + stageIndex * 6131);
            _nextSpawnZ = startZ + 22f;
            RoadOccupancy.Clear();
            _lineLane = 0;
            EndGlideTrail();
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                var cp = c.GetComponent<CoinPickup>();
                if (cp != null) cp.Recycle(); else Destroy(c.gameObject);
            }
        }

        private int Rnd(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        private float Rnd(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        /// 23차-3: 골인 뒤 앞쪽 코인을 전부 거둔다(풀로).
        public void ClearAhead(float z)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (!c.gameObject.activeSelf || DownhillPath.DistanceAlong(c.position) < z) continue;
                var cp = c.GetComponent<CoinPickup>();
                if (cp != null) cp.Recycle(); else Destroy(c.gameObject);
            }
        }

        public void Bind(PlayerController playerController, CoinWallet coinWallet,
            UpgradeManager upgradeManager, UI_FeedbackController ui)
        {
            player = playerController;
            wallet = coinWallet;
            upgrades = upgradeManager;
            feedback = ui;
            if (_root == null)
            {
                _root = new GameObject("Coins").transform;
                _root.SetParent(null, false);
                _root.position = Vector3.zero;
                _root.rotation = Quaternion.identity;
                _root.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (player == null || wallet == null || _root == null)
                return;

            if (_glideTrail)
            {
                if (player.IsGliding) FillGlideTrail();
                else EndGlideTrail();
            }

            float z = player.PathDistance;
            // 43차: 피버 중엔 코인이 3배 이상 — 평소 패턴은 그대로 두고, 그 위에 **3레인 코인 카펫**(1.5 m 간격)을 앞쪽에 깐다.
            // 평소 라인은 한 레인 7~9개/약 40 m 이므로 카펫(3레인 × 40 m/1.5 = 80개)만으로도 8배가 넘는다.
            if (FeverMode.Active)
            {
                if (_feverFillZ < z + 4f) _feverFillZ = z + 4f;
                var followF = player.transform;
                while (_feverFillZ < z + spawnAhead)
                {
                    for (int lane = -1; lane <= 1; lane++)
                        PlaceFever(_feverFillZ, lane, followF);
                    _feverFillZ += 1.5f;
                }
            }
            else _feverFillZ = 0f;
            if (BossDirector.SpawnHold) _nextSpawnZ = Mathf.Max(_nextSpawnZ, z + spawnAhead);   // 51차: 보스 중엔 코인 패턴 쉼(최소화)
            while (_nextSpawnZ < z + spawnAhead)
            {
                // Pattern length + breath — Gold Run empty asphalt between guides.
                float len = SpawnPattern(_nextSpawnZ);
                float breath = Rnd(12f, 20f);
                float paced = (spawnInterval + Rnd(-1f, 2f)) * RunRhythm.CoinIntervalMul(_nextSpawnZ);
                _nextSpawnZ += Mathf.Max(paced, len + breath);
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                // 14차-7: 지나친 코인은 1.2 m 뒤에서 바로 회수 — 카메라 앞에서 거대하게 떠다니던 원인.
                if (DownhillPath.DistanceAlong(child.position) < z - 1.2f)
                {
                    var c = child.GetComponent<CoinPickup>();
                    if (c != null) c.Recycle(); else Destroy(child.gameObject);
                }
            }
        }

        private int _lineLane;   // 코인 라인 구간: 줄이 레인을 옮겨 가며 이어진다(S자)

        /// Prefer a lane clear of nearby obstacles so coin lines guide the open path.
        private int PickOpenLane(float z, int preferred)
        {
            int[] order = preferred == 0
                ? new[] { 0, -1, 1 }
                : preferred < 0 ? new[] { -1, 0, 1 } : new[] { 1, 0, -1 };
            for (int i = 0; i < order.Length; i++)
            {
                if (!RoadOccupancy.Near(RoadOccupancy.Kind.Obstacle, z, order[i], 3f))
                    return order[i];
            }
            return preferred;
        }

        /// Returns the pattern's length along Z so the next spawn can leave a clear gap.
        private float SpawnPattern(float z)
        {
            int lane = PickOpenLane(z, Rnd(-1, 2));
            Transform follow = player != null ? player.transform : null;
            var phase = RunRhythm.At(z);

            // CoinLine: one long single-lane breadcrumb (Gold Run pass1/pass3).
            if (phase == RunRhythm.Phase.CoinLine)
            {
                _lineLane = PickOpenLane(z, _lineLane);
                // ~25% dual parallel lanes like Gold Run pass3; else single lane.
                bool dual = Rnd(0, 4) == 0;
                int count = 7 + Rnd(0, 3);   // 7..9
                const float spacing = 2.4f;
                int other = _lineLane == 0 ? (Rnd(0, 2) == 0 ? -1 : 1) : 0;
                for (int i = 0; i < count; i++)
                {
                    Place(z + i * spacing, _lineLane, i % 5 == 4, follow);
                    if (dual) Place(z + i * spacing, other, false, follow);
                }
                int step = Rnd(0, 2) == 0 ? -1 : 1;
                _lineLane = Mathf.Clamp(_lineLane + step, -1, 1);
                if (_lineLane == 0 && Rnd(0, 3) == 0) _lineLane = step;
                return (count - 1) * spacing;
            }

            // Easy: almost always a short straight single-lane guide (Gold Run pass1).
            // Crisis: jump-arc tease or diagonal onto open lane.
            int pattern;
            if (phase == RunRhythm.Phase.Crisis)
                pattern = Rnd(0, 3) == 0 ? 1 : 3;
            else
            {
                int roll = Rnd(0, 10);
                if (roll < 8) pattern = 0;       // straight
                else if (roll < 9) pattern = 4;  // mild diagonal into next open lane
                else pattern = 3;               // elevated tease
            }

            if (pattern == 0)
            {
                int count = 5 + Rnd(0, 3);   // 5..7 — readable single-lane trail
                const float spacing = 2.4f;
                for (int i = 0; i < count; i++)
                    Place(z + i * spacing, lane, false, follow);
                return (count - 1) * spacing;
            }
            if (pattern == 1)
            {
                // Zigzag teach-swipe — keep sparse
                for (int i = 0; i < 4; i++)
                    Place(z + i * 2.8f, PickOpenLane(z + i * 2.8f, (i % 3) - 1), i % 2 == 1, follow);
                return 3f * 2.8f;
            }
            if (pattern == 4)
            {
                // Mild diagonal: stay in lane then step once (Gold Run S-curve lite)
                int count = 6;
                const float spacing = 2.3f;
                int to = Mathf.Clamp(lane + (Rnd(0, 2) == 0 ? -1 : 1), -1, 1);
                for (int i = 0; i < count; i++)
                {
                    int l = i < count / 2 ? lane : to;
                    Place(z + i * spacing, l, false, follow);
                }
                return (count - 1) * spacing;
            }
            // Jump-arc tease
            for (int i = 0; i < 3; i++)
                Place(z + i * 2f, lane, true, follow);
            Place(z + 7f, lane, false, follow);
            return 7f;
        }

        public static CoinSpawner Instance { get; private set; }
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// 14차-10: 점프대 뒤 하늘 코인 아치 — 점프대를 밟으면 포물선을 따라 코인을 먹는다.
        /// v0(초기 상승 속도)·g(중력)·speed(진행 속도)로 실제 궤적을 계산해 그 위에 1.6 m 간격으로 놓는다.
        public void SpawnAirArc(float z, int lane, float v0, float g, float speed)
        {
            if (_root == null || speed <= 0.1f) return;
            Transform follow = player != null ? player.transform : null;
            float T = 2f * v0 / Mathf.Abs(g);
            int n = Mathf.Clamp(Mathf.FloorToInt(speed * T / 1.6f), 4, 14);
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n * T;
                float h = v0 * t + 0.5f * g * t * t;
                Vector3 pos = RoadPlacement.OnRoad(z + speed * t, lane * laneWidth, h + 0.35f);
                CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, i == n / 2 ? false : (i % 4 == 3));
            }
        }

        /// 17차: 빨래줄 활공 코인 — 한 번에 깔아 두는 고정 줄(길이만큼). 연속 트레일은 BeginGlideTrail 사용.
        public void SpawnGlideLine(float z, int lane, float height, float length)
        {
            if (_root == null) return;
            Transform follow = player != null ? player.transform : null;
            int n = Mathf.Clamp(Mathf.FloorToInt(length / 1.4f), 8, 48);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                int l = lane;
                if (t > 0.5f)
                {
                    int seg = Mathf.FloorToInt((t - 0.5f) / 0.25f);
                    l = lane == 0 ? (seg % 2 == 0 ? 1 : -1) : (seg % 2 == 0 ? 0 : lane);
                }
                float wave = Mathf.Sin(t * Mathf.PI * 3f) * 0.25f;
                Vector3 pos = RoadPlacement.OnRoad(z + i * 1.4f, l * laneWidth, height + 0.35f + wave);
                CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, i % 5 == 4);
            }
        }

        /// 활공 시작: 플레이어 앞 ~24 m를 계속 채워 동전·말랑이·하트가 끊기지 않게 나온다.
        public void BeginGlideTrail(int lane, float height)
        {
            if (player == null || _root == null) return;
            _glideTrail = true;
            _glideLane = lane;
            _glideHeight = height;
            _glideNextZ = player.PathDistance + 2.2f;
            _glideCount = 0;
            player.OnGlideEnd -= EndGlideTrail;
            player.OnGlideEnd += EndGlideTrail;
            FillGlideTrail();
        }

        private void EndGlideTrail()
        {
            _glideTrail = false;
            if (player != null) player.OnGlideEnd -= EndGlideTrail;
        }

        private void FillGlideTrail()
        {
            if (player == null || _root == null) return;
            const float lead = 24f;
            const float spacing = 1.0f;
            float target = player.PathDistance + lead;
            Transform follow = player.transform;
            while (_glideNextZ < target)
            {
                int n = _glideCount;
                int l = _glideLane;
                // 앞쪽 몇 개는 잡은 레인 고정, 그 뒤는 옆 레인 물결(조종 유도)
                if (n >= 6)
                {
                    int seg = (n - 6) / 3;
                    l = _glideLane == 0 ? (seg % 2 == 0 ? 1 : -1) : (seg % 2 == 0 ? 0 : _glideLane);
                }
                float wave = Mathf.Sin(n * 0.55f) * 0.28f;
                float h = _glideHeight + 0.35f + wave;
                // 7개마다 말랑이, 14개마다 하트 — 나머지는 동전(5번째마다 은화)
                if (n > 0 && n % 14 == 0)
                    JellySpawner.Instance?.SpawnGlideReward(PickupKind.Heart, _glideNextZ, l, h);
                else if (n > 0 && n % 7 == 0)
                    JellySpawner.Instance?.SpawnGlideReward(PickupKind.Jelly, _glideNextZ, l, h);
                else
                {
                    Vector3 pos = RoadPlacement.OnRoad(_glideNextZ, l * laneWidth, h);
                    CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, n % 5 == 4);
                }
                _glideNextZ += spacing;
                _glideCount++;
            }
        }

        /// 38차: 장애물이 나중에 놓였을 때 그 근처 코인을 걷어 낸다(하늘 코인은 제외).
        public void RemoveNear(float z, int lane, float dz)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                if (c.position.y - RoadPlacement.OnRoad(z, 0f, 0f).y > 1.2f) continue;   // 아치·활공 코인은 그대로
                float cz = DownhillPath.DistanceAlong(c.position);
                if (Mathf.Abs(cz - z) > dz) continue;
                int cl = Mathf.RoundToInt(c.position.x / laneWidth);
                if (lane != RoadOccupancy.AllLanes && cl != lane) continue;
                var cp = c.GetComponent<CoinPickup>();
                if (cp != null) cp.Recycle(); else Destroy(c.gameObject);
            }
        }

        private float _feverFillZ;
        /// 43차: 피버 카펫 — 점유표 검사 없이(장애물 옆에도) 금화를 놓는다. 피버는 흡입이 있어 어차피 다 먹힌다.
        private void PlaceFever(float z, int lane, Transform follow)
        {
            Vector3 pos = RoadPlacement.OnRoad(z, lane * laneWidth, 0.5f);
            CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, CoinTier.Gold);
        }

        private void Place(float z, int lane, bool silver, Transform follow)
        {
            // 38차: 장애물 앞뒤 3 m(같은 레인)엔 코인을 안 놓는다 — 붙어 있으면 피하는 맛이 없다
            if (RoadOccupancy.Near(RoadOccupancy.Kind.Obstacle, z, lane, 3f)) return;
            RoadOccupancy.Add(RoadOccupancy.Kind.Pickup, z, lane);
            float lateral = lane * laneWidth;
            // Waist-height float like the coastal mock (not glued to asphalt).
            Vector3 pos = RoadPlacement.OnRoad(z, lateral, 0.5f);   // coin centre ends up ~0.7 m: waist height, not floating
            // 은화 / 금화 / 꾸러미(금화×10). 꾸러미는 ~7% — 라인 중간중간 보물처럼.
            CoinTier tier = silver ? CoinTier.Silver
                : (Rnd(0, 100) < 7 ? CoinTier.Bundle : CoinTier.Gold);
            CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, tier);
        }
    }
}
