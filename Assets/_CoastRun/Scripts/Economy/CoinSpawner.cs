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
        [SerializeField] private float spawnAhead = 80f;
        [SerializeField] private float spawnInterval = 13f;   // jellies are the main breadcrumb now; coins stay the currency
        [SerializeField] private float laneWidth = 2.2f;

        private float _nextSpawnZ = 8f;
        private Transform _root;
        // 24차-8(점검 3-2): 스테이지 리셋이 없어 재도전 뒤 이전 도달점까지 코인이 0이었고, UnityEngine.Random이라
        // 같은 시드=같은 코스 원칙이 코인에서만 깨졌다. 장애물·젤리와 같은 방식으로 시드 RNG를 쓴다.
        private System.Random _rng = new System.Random(777);

        public void ResetForStage(int stageIndex, float startZ)
        {
            _rng = new System.Random(700 + stageIndex * 6131);
            _nextSpawnZ = startZ + 8f;
            RoadOccupancy.Clear();
            _lineLane = 0;
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

            float z = player.PathDistance;
            while (_nextSpawnZ < z + spawnAhead)
            {
                SpawnPattern(_nextSpawnZ);
                _nextSpawnZ += (spawnInterval + Rnd(-1.5f, 2.5f)) * RunRhythm.CoinIntervalMul(_nextSpawnZ);
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

        private void SpawnPattern(float z)
        {
            int lane = Rnd(-1, 2);
            int pattern = Rnd(0, 4);
            Transform follow = player != null ? player.transform : null;

            // 14차 리듬: 코인 라인 구간엔 한 레인에 길게, 다음 줄은 옆 레인으로 — '따라가면 되는' 길.
            var phase = RunRhythm.At(z);
            if (phase == RunRhythm.Phase.CoinLine)
            {
                int count = 9 + Rnd(0, 4);
                for (int i = 0; i < count; i++)
                    Place(z + i * 2.0f, _lineLane, i % 5 == 4, follow);
                int step = Rnd(0, 2) == 0 ? -1 : 1;
                _lineLane = Mathf.Clamp(_lineLane + step, -1, 1);
                if (_lineLane == 0 && Rnd(0, 3) == 0) _lineLane = step;   // 가운데에만 머물지 않게
                return;
            }
            if (phase == RunRhythm.Phase.Crisis)
                pattern = Rnd(0, 2) == 0 ? 3 : 1;   // 위기 구간: 점프 아치·지그재그 위주

            if (pattern == 0)
            {
                int count = 4 + Rnd(0, 3);
                for (int i = 0; i < count; i++)
                    Place(z + i * 2.2f, lane, false, follow);
            }
            else if (pattern == 1)
            {
                for (int i = 0; i < 5; i++)
                    Place(z + i * 2.4f, (i % 3) - 1, i % 2 == 1, follow);
            }
            else if (pattern == 2)
            {
                for (int l = -1; l <= 1; l++)
                    Place(z, l, false, follow);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    Place(z + i * 2f, lane, true, follow);
                Place(z + 7f, lane, false, follow);
            }
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

        /// 17차: 빨래줄 활공 코인 — 줄 높이에 1.8 m 간격, 앞 1/3은 잡은 레인, 그 뒤는 옆 레인으로 물결(조종 유도). 5개마다 은화.
        public void SpawnGlideLine(float z, int lane, float height, float length)
        {
            if (_root == null) return;
            Transform follow = player != null ? player.transform : null;
            // 33차: 하늘 코인 — 촘촘하게(1.4 m), 앞 절반은 잡은 레인 그대로(그냥 날면 먹힌다), 뒤 절반은 옆 레인으로 물결.
            // 높이는 줄보다 0.35 m 위(가슴 앞)로, 위아래로 살짝 파도치게 해서 '하늘에 떠 있는 코인 줄'로 읽힌다.
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

        private void Place(float z, int lane, bool silver, Transform follow)
        {
            // 38차: 장애물 앞뒤 3 m(같은 레인)엔 코인을 안 놓는다 — 붙어 있으면 피하는 맛이 없다
            if (RoadOccupancy.Near(RoadOccupancy.Kind.Obstacle, z, lane, 3f)) return;
            RoadOccupancy.Add(RoadOccupancy.Kind.Pickup, z, lane);
            float lateral = lane * laneWidth;
            // Waist-height float like the coastal mock (not glued to asphalt).
            Vector3 pos = RoadPlacement.OnRoad(z, lateral, 0.5f);   // coin centre ends up ~0.7 m: waist height, not floating
            CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, silver);
        }
    }
}
