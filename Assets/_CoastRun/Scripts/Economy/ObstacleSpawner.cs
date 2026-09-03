using UnityEngine;

namespace CoastRun
{
    /// Spawns dual-collider obstacles along the promenade ahead of the player.
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SeasonWeatherDirector seasonWeather;
        [SerializeField] private float spawnAhead = 80f;
        [SerializeField] private float spawnInterval = 8f;
        [SerializeField] private float laneWidth = 2.2f;

        private float _nextSpawnZ = 10f;
        private Transform _root;
        private readonly System.Random _rng = new System.Random(42);

        public void Bind(PlayerController playerController, SeasonWeatherDirector director = null)
        {
            player = playerController;
            seasonWeather = director;
            if (_root == null)
            {
                _root = new GameObject("Obstacles").transform;
                _root.SetParent(null, false);
                _root.position = Vector3.zero;
                _root.rotation = Quaternion.identity;
                _root.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (player == null || _root == null)
                return;

            float z = player.PathDistance;
            SeasonKind season = StageManager.Instance != null
                ? StageManager.ChapterAsSeason(StageManager.Instance.ChapterIndex)
                : SeasonKind.Summer;
            WeatherKind weather = seasonWeather != null
                ? seasonWeather.CurrentWeather
                : WeatherKind.Clear;

            float dens = 1f;
            if (StageManager.Instance != null && StageManager.Instance.Current != null)
                dens = Mathf.Lerp(1f, 0.75f, StageManager.Instance.StageProgress01);
            float interval = spawnInterval * dens;

            while (_nextSpawnZ < z + spawnAhead)
            {
                SpawnAt(_nextSpawnZ, season, weather);
                _nextSpawnZ += interval + Random.Range(-1.5f, 2.5f);
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(child.position) < z - 45f)
                    Destroy(child.gameObject);
            }
        }

        private void SpawnAt(float z, SeasonKind season, WeatherKind weather)
        {
            int lane = Random.Range(-1, 2);
            int count = Random.value < 0.22f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                int l = count == 1 ? lane : (i == 0 ? -1 : 1);
                if (count == 2 && Random.value < 0.5f)
                    l = i == 0 ? -1 : 0;

                float lateral = l * laneWidth;
                Vector3 pos = RoadPlacement.OnRoad(z, lateral);
                var id = ObstacleCatalog.Pick(season, weather, _rng);
                var go = ObstacleCatalog.Spawn(id, _root, pos, l);
                if (go != null)
                    RoadPlacement.Snap(go, z, lateral);
            }
        }
    }
}
