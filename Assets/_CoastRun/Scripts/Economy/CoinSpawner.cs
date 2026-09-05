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
                _nextSpawnZ += spawnInterval + Random.Range(-1.5f, 2.5f);
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(child.position) < z - 40f)
                    Destroy(child.gameObject);
            }
        }

        private void SpawnPattern(float z)
        {
            int lane = Random.Range(-1, 2);
            int pattern = Random.Range(0, 4);
            Transform follow = player != null ? player.transform : null;

            if (pattern == 0)
            {
                int count = 4 + Random.Range(0, 3);
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

        private void Place(float z, int lane, bool silver, Transform follow)
        {
            float lateral = lane * laneWidth;
            // Waist-height float like the coastal mock (not glued to asphalt).
            Vector3 pos = RoadPlacement.OnRoad(z, lateral, 0.5f);   // coin centre ends up ~0.7 m: waist height, not floating
            CoinPickup.Spawn(_root, pos, wallet, upgrades, feedback, follow, silver);
        }
    }
}
