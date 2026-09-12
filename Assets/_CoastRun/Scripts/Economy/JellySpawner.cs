using UnityEngine;

namespace CoastRun
{
    /// Lays jelly trails ahead of the player: straight runs, lane-hopping zigzags and
    /// jump arcs, with a potion every so often and a Bonus Time star now and then.
    /// During Bonus Time every lane fills with big jellies and nothing else spawns.
    ///
    /// Trails are laid independently of obstacle rows (a line may cross a blocked
    /// lane — dodge and let the magnet radius catch the strays), which keeps them
    /// long and readable instead of chopped up around every cone.
    public class JellySpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private float spawnAhead = 50f;
        [SerializeField] private float laneWidth = 2.2f;
        [SerializeField] private float jellyHeight = 0.35f;
        [Header("Pacing (metres)")]
        [SerializeField] private float trailGapMin = 22f;  // coins lead; jelly only in quiet stretches
        [SerializeField] private float trailGapMax = 36f;
        [SerializeField] private float potionEvery = 110f;   // 17차: 피해 ↑ 만큼 물약도 자주(180→110 m)
        [SerializeField] private float starEvery = 900f;

        private Transform _root;
        private float _nextTrailZ = 48f;
        private float _nextPotionZ = 60f;
        private float _nextStarZ = 200f;
        private float _nextCardZ = 420f;   // 38차: 포토카드 아이템
        private float _nextGiantZ = 160f;  // 거인 무적
        private float _bonusFillZ;
        private float _nextHeartZ;
        private float _heartSpacing = 80f;
        private int _heartsLeft;
        private System.Random _rng = new System.Random(7);

        /// 말랑이 하트를 스테이지 길이에 균등 분배한다. RunTuning.HeartsPerStage 개.
        public void ConfigureHearts(float stageLength)
        {
            _heartsLeft = RunTuning.HeartsPerStage;
            _heartSpacing = Mathf.Max(20f, stageLength / (_heartsLeft + 1));
        }
        private int _lastLane;

        public bool BonusMode { get; private set; }

        public void Bind(PlayerController playerController, UpgradeManager upgradeManager)
        {
            player = playerController;
            upgrades = upgradeManager;
            if (_root == null)
            {
                _root = new GameObject("Jellies").transform;
                _root.SetParent(null, false);
            }
        }

        public void ResetForStage(int stageIndex, float startZ)
        {
            _rng = new System.Random(500 + stageIndex * 4271);
            _nextTrailZ = startZ + 48f;   // Gold Run: first beat is coins/obstacles, not jelly carpet
            _nextPotionZ = startZ + 90f + (float)_rng.NextDouble() * 60f;
            _nextStarZ = startZ + 320f + (float)_rng.NextDouble() * 120f;
            _nextCardZ = startZ + (StoryContest.Active && StoryContest.Current.goal == StoryContest.Goal.Photos ? 200f : 380f) + (float)_rng.NextDouble() * 180f;
            _nextGiantZ = startZ + 140f + (float)_rng.NextDouble() * 80f;
            _nextHeartZ = Mathf.Max(startZ + 40f, startZ + _heartSpacing * 0.6f);
            _heartsLeft = RunTuning.HeartsPerStage;
            ClearAll();
        }

        /// 23차-3: 골인 뒤 앞쪽 말랑이·물약·별을 전부 치운다.
        public void ClearAhead(float z)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(c.position) >= z) Destroy(c.gameObject);
            }
        }

        public void ClearAll()
        {
            if (_root == null)
                return;
            for (int i = _root.childCount - 1; i >= 0; i--)
                Destroy(_root.GetChild(i).gameObject);
        }

        /// Bonus Time: wipe the normal trails ahead and carpet all three lanes.
        public void SetBonusMode(bool on)
        {
            if (BonusMode == on)
                return;
            BonusMode = on;
            if (_root == null || player == null)
                return;
            float z = player.PathDistance;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(c.position) > z + 6f)
                    Destroy(c.gameObject);
            }
            _bonusFillZ = z + 8f;
            if (!on)
                _nextTrailZ = z + 14f;
        }

        private void Update()
        {
            if (player == null || _root == null)
                return;

            float z = player.PathDistance;

            // 43차: 피버 중엔 말랑이도 3배 이상 — 세 레인 지그재그로 1.2 m 간격 카펫(평소 트레일은 그대로 유지).
            if (FeverMode.Active && !BonusMode)
            {
                if (_feverFillZ < z + 5f) _feverFillZ = z + 5f;
                while (_feverFillZ < z + spawnAhead)
                {
                    int step = (int)(_feverFillZ / 1.2f);
                    int lane = (step % 4 == 0 || step % 4 == 2) ? 0 : (step % 4 == 1 ? -1 : 1);
                    Vector3 pos = RoadPlacement.OnRoad(_feverFillZ, lane * laneWidth, jellyHeight + 0.15f);
                    JellyPickup.Spawn(PickupKind.Jelly, _root, pos, player.transform, upgrades, step % 5);
                    _feverFillZ += 1.2f;
                }
            }
            else _feverFillZ = 0f;

            if (BonusMode)
            {
                while (_bonusFillZ < z + spawnAhead)
                {
                    int color = (int)(_bonusFillZ / 1.6f) % 5;
                    for (int lane = -1; lane <= 1; lane++)
                        Place(PickupKind.BigJelly, _bonusFillZ, lane, jellyHeight, color);
                    _bonusFillZ += 1.6f;
                }
            }
            else
            {
                while (_nextTrailZ < z + spawnAhead)
                {
                    // Gold Run: during coin-guide beats, leave the road to coins (no jelly carpet).
                    if (RunRhythm.At(_nextTrailZ) == RunRhythm.Phase.CoinLine)
                    {
                        _nextTrailZ += 28f;
                        continue;
                    }
                    float len = SpawnTrail(_nextTrailZ);
                    _nextTrailZ += len + Mathf.Lerp(trailGapMin, trailGapMax, (float)_rng.NextDouble());
                }

                if (_nextPotionZ < z + spawnAhead)
                {
                    Place(PickupKind.Potion, _nextPotionZ, _rng.Next(3) - 1, 0.35f);
                    _nextPotionZ += potionEvery * (0.8f + (float)_rng.NextDouble() * 0.5f);
                }

                if (_nextCardZ < z + spawnAhead)
                {
                    Place(PickupKind.Photocard, _nextCardZ, _rng.Next(3) - 1, 0.5f);
                    _nextCardZ += StoryContest.Active && StoryContest.Current.goal == StoryContest.Goal.Photos
                        ? 230f + (float)_rng.NextDouble() * 90f      // 55차: 사진 콘테스트 땐 포토카드가 촘촘히
                        : 420f + (float)_rng.NextDouble() * 220f;
                }
                if (_nextStarZ < z + spawnAhead)
                {
                    Place(PickupKind.BonusStar, _nextStarZ, _rng.Next(3) - 1, 0.5f);
                    _nextStarZ += starEvery * (0.85f + (float)_rng.NextDouble() * 0.4f);
                }

                if (_nextGiantZ < z + spawnAhead)
                {
                    Place(PickupKind.Giant, _nextGiantZ, _rng.Next(3) - 1, 0.45f);
                    _nextGiantZ += 220f + (float)_rng.NextDouble() * 120f;   // 스테이지당 대략 1~2개
                }

                // 말랑이 하트: 트랙 전체에 고르게, 레인은 시드 난수. 점프 높이(1.2 m)에 놓이는
                // 것도 섞어 "받으려면 뛰어야 하는" 하트를 만든다.
                if (_heartsLeft > 0 && _nextHeartZ < z + spawnAhead)
                {
                    bool high = _rng.NextDouble() < 0.3;
                    Place(PickupKind.Heart, _nextHeartZ, _rng.Next(3) - 1, high ? 1.25f : 0.4f);
                    _heartsLeft--;
                    _nextHeartZ += _heartSpacing * (0.85f + (float)_rng.NextDouble() * 0.3f);
                }
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                // 14차-7: 지나친 젤리/하트는 1.2 m 뒤에서 바로 제거(카메라 앞 거대 젤리 방지).
                if (DownhillPath.DistanceAlong(child.position) < z - 1.2f)
                    Destroy(child.gameObject);
            }
        }

        /// Returns the trail's length in metres.
        private float SpawnTrail(float z)
        {
            // Prefer short single-lane trails — Gold Run style, not a pour.
            int roll = _rng.Next(10);   // 0-5 straight, 6-7 zig, 8 arc, 9 rare double
            int lane = PickLane();
            const float step = 1.8f;

            if (roll <= 5)
            {
                int count = 4 + _rng.Next(4);   // 4..7
                for (int i = 0; i < count; i++)
                    Place(PickupKind.Jelly, z + i * step, lane, jellyHeight, i % 5);
                return count * step;
            }

            if (roll <= 7)
            {
                int count = 6;
                int dir = lane <= 0 ? 1 : -1;
                int l = lane;
                for (int i = 0; i < count; i++)
                {
                    Place(PickupKind.Jelly, z + i * step * 1.4f, l, jellyHeight, i % 5);
                    if (i % 3 == 2)
                    {
                        l += dir;
                        if (l > 1 || l < -1) { dir = -dir; l += 2 * dir; }
                    }
                }
                _lastLane = l;
                return count * step * 1.4f;
            }

            if (roll == 8)
            {
                int count = 5;
                for (int i = 0; i < count; i++)
                {
                    float u = i / (float)(count - 1);
                    float h = jellyHeight + Mathf.Sin(u * Mathf.PI) * 1.6f;
                    Place(PickupKind.Jelly, z + i * step, lane, h, 2);
                }
                return count * step;
            }

            // Rare double lane — greed test.
            {
                int count = 4;
                int other = lane == 1 ? 0 : lane + 1;
                for (int i = 0; i < count; i++)
                {
                    Place(PickupKind.Jelly, z + i * step, lane, jellyHeight, i % 5);
                    Place(PickupKind.Jelly, z + i * step, other, jellyHeight, (i + 2) % 5);
                }
                return count * step;
            }
        }

        private int PickLane()
        {
            // Bias toward where the last trail ended so runs chain naturally.
            int lane = _rng.NextDouble() < 0.55 ? _lastLane : _rng.Next(3) - 1;
            _lastLane = lane;
            return lane;
        }

        public static JellySpawner Instance { get; private set; }
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// 38차: 장애물이 나중에 놓였을 때 그 근처 말랑이를 걷어 낸다(아이템은 FindClear 로 이미 떨어져 있다).
        public void RemoveNear(float z, int lane, float dz)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                float cz = DownhillPath.DistanceAlong(c.position);
                if (Mathf.Abs(cz - z) > dz) continue;
                int cl = Mathf.RoundToInt(c.position.x / laneWidth);
                if (lane != RoadOccupancy.AllLanes && cl != lane) continue;
                var jp = c.GetComponent<JellyPickup>();
                if (jp != null && jp.Kind != PickupKind.Jelly && jp.Kind != PickupKind.BigJelly) continue;
                Destroy(c.gameObject);
            }
        }

        private float _feverFillZ;
        private void Place(PickupKind kind, float z, int lane, float height, int color = -1)
        {
            // 38차: 말랑이는 장애물 3 m 안엔 안 놓고, 아이템(물약/별/하트)은 장애물 4.5 m·다른 픽업 3.5 m 떨어진 자리로 미룬다
            bool item = kind == PickupKind.Potion || kind == PickupKind.BonusStar || kind == PickupKind.Heart
                        || kind == PickupKind.Photocard || kind == PickupKind.Giant;
            if (item) z = RoadOccupancy.FindClear(z, lane, 4.5f, 3.5f);
            else if (RoadOccupancy.Near(RoadOccupancy.Kind.Obstacle, z, lane, 3f)) return;
            RoadOccupancy.Add(item ? RoadOccupancy.Kind.Item : RoadOccupancy.Kind.Pickup, z, lane);
            Vector3 pos = RoadPlacement.OnRoad(z, lane * laneWidth, height);
            JellyPickup.Spawn(kind, _root, pos, player != null ? player.transform : null, upgrades, color);
        }

        /// 활공 하늘 보상 — 지면 점유 검사 없이 줄 높이에 바로 놓는다.
        public void SpawnGlideReward(PickupKind kind, float z, int lane, float height)
        {
            if (_root == null) return;
            Vector3 pos = RoadPlacement.OnRoad(z, lane * laneWidth, height);
            JellyPickup.Spawn(kind, _root, pos, player != null ? player.transform : null, upgrades);
        }
    }
}
