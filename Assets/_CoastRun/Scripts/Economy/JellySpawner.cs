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
        [SerializeField] private float spawnAhead = 90f;
        [SerializeField] private float laneWidth = 2.2f;
        [SerializeField] private float jellyHeight = 0.35f;
        [Header("Pacing (metres)")]
        [SerializeField] private float trailGapMin = 6f;
        [SerializeField] private float trailGapMax = 14f;
        [SerializeField] private float potionEvery = 180f;
        [SerializeField] private float starEvery = 900f;

        private Transform _root;
        private float _nextTrailZ = 12f;
        private float _nextPotionZ = 60f;
        private float _nextStarZ = 200f;
        private float _bonusFillZ;
        private System.Random _rng = new System.Random(7);
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
            _nextTrailZ = startZ + 12f;
            _nextPotionZ = startZ + 90f + (float)_rng.NextDouble() * 60f;
            _nextStarZ = startZ + 320f + (float)_rng.NextDouble() * 120f;
            ClearAll();
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
                    float len = SpawnTrail(_nextTrailZ);
                    _nextTrailZ += len + Mathf.Lerp(trailGapMin, trailGapMax, (float)_rng.NextDouble());
                }

                if (_nextPotionZ < z + spawnAhead)
                {
                    Place(PickupKind.Potion, _nextPotionZ, _rng.Next(3) - 1, 0.35f);
                    _nextPotionZ += potionEvery * (0.8f + (float)_rng.NextDouble() * 0.5f);
                }

                if (_nextStarZ < z + spawnAhead)
                {
                    Place(PickupKind.BonusStar, _nextStarZ, _rng.Next(3) - 1, 0.5f);
                    _nextStarZ += starEvery * (0.85f + (float)_rng.NextDouble() * 0.4f);
                }
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(child.position) < z - 30f)
                    Destroy(child.gameObject);
            }
        }

        /// Returns the trail's length in metres.
        private float SpawnTrail(float z)
        {
            int pattern = _rng.Next(4);
            int lane = PickLane();
            const float step = 1.5f;

            if (pattern == 0)
            {
                // Straight run.
                int count = 7 + _rng.Next(6);
                for (int i = 0; i < count; i++)
                    Place(PickupKind.Jelly, z + i * step, lane, jellyHeight, i % 5);
                return count * step;
            }

            if (pattern == 1)
            {
                // Zigzag across lanes — teaches the swipe rhythm.
                int count = 9;
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

            if (pattern == 2)
            {
                // Jump arc: rises to 1.6 m — the reward for hopping.
                int count = 8;
                for (int i = 0; i < count; i++)
                {
                    float u = i / (float)(count - 1);
                    float h = jellyHeight + Mathf.Sin(u * Mathf.PI) * 1.6f;
                    Place(PickupKind.Jelly, z + i * step, lane, h, 2);
                }
                return count * step;
            }

            // Double lane run (two parallel lines) — greed test.
            {
                int count = 6;
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

        private void Place(PickupKind kind, float z, int lane, float height, int color = -1)
        {
            Vector3 pos = RoadPlacement.OnRoad(z, lane * laneWidth, height);
            JellyPickup.Spawn(kind, _root, pos, player != null ? player.transform : null, upgrades, color);
        }
    }
}
