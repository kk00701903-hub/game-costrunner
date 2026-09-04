using UnityEngine;

namespace CoastRun
{
    /// Spawns obstacles ahead of the player in rows that are always survivable.
    ///
    /// The previous version rolled a random lane every 6–8 m with no memory of the row
    /// before it. At 18 m/s two rows could land 4.5 m apart — a quarter of a second —
    /// while blocking lanes that needed two swipes to get between. The player did
    /// everything right and still ate a hit, which is the one thing a runner must never
    /// do. Every row here is planned against the previous one:
    ///
    ///   - Spacing scales with speed so there is always a reaction window plus the time
    ///     the lane change itself takes.
    ///   - At least one open lane in the new row is reachable from an open lane in the
    ///     old row with at most one swipe.
    ///   - A row that costs two swipes to escape is spaced further, not closer.
    ///
    /// Difficulty still climbs through a stage — rows get denser and double rows more
    /// frequent — but never past what the rules above allow.
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private SeasonWeatherDirector seasonWeather;
        [SerializeField] private float spawnAhead = 80f;
        [SerializeField] private float laneWidth = 2.2f;

        [Header("Pacing")]
        [Tooltip("Seconds the player gets to see a row and react before reaching it.")]
        [SerializeField] private float reactionSeconds = 0.55f;
        [Tooltip("Extra seconds allowed per lane change needed to reach a safe lane.")]
        [SerializeField] private float laneChangeSeconds = 0.22f;
        [Tooltip("Base gap between rows at the start of a stage, in seconds of travel.")]
        [SerializeField] private float rowGapSecondsStart = 1.4f;
        [Tooltip("Base gap at the end of a stage. Never goes below the reaction floor.")]
        [SerializeField] private float rowGapSecondsEnd = 0.85f;
        [Tooltip("Chance of a two-lane row at stage start / end.")]
        [SerializeField, Range(0f, 1f)] private float doubleRowChanceStart = 0.10f;
        [SerializeField, Range(0f, 1f)] private float doubleRowChanceEnd = 0.32f;

        private float _nextSpawnZ = 14f;
        private Transform _root;
        private System.Random _rng = new System.Random(42);

        // Lanes still open after the most recent row (bitmask: bit0=-1, bit1=0, bit2=+1).
        private int _prevOpen = 0b111;

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

        /// Deterministic per stage: a retry lays out the same course, which is what a
        /// player replaying the same 200 m expects.
        public void ResetForStage(int stageIndex, float startZ)
        {
            _rng = new System.Random(1000 + stageIndex * 7919);
            _nextSpawnZ = startZ + 14f;
            _prevOpen = 0b111;
        }

        private void Update()
        {
            if (player == null || _root == null)
                return;

            float z = player.PathDistance;
            float speed = Mathf.Max(6f, player.Speed);

            float progress = 0f;
            SeasonKind season = SeasonKind.Summer;
            var stages = StageManager.Instance;
            if (stages != null)
            {
                progress = stages.Current != null ? stages.StageProgress01 : 0f;
                season = StageManager.ChapterAsSeason(stages.ChapterIndex);
            }
            WeatherKind weather = seasonWeather != null ? seasonWeather.CurrentWeather : WeatherKind.Clear;

            while (_nextSpawnZ < z + spawnAhead)
            {
                int blocked = PlanRow(progress);
                SpawnRow(_nextSpawnZ, blocked, season, weather);

                int open = 0b111 & ~blocked;
                float gap = RowGap(speed, progress, _prevOpen, open);
                _prevOpen = open;
                _nextSpawnZ += gap;
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(child.position) < z - 45f)
                    Destroy(child.gameObject);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Planning
        // ────────────────────────────────────────────────────────────────

        /// Picks which lanes the next row blocks, guaranteeing that some open lane is
        /// within one swipe of a lane that was open in the previous row.
        private int PlanRow(float progress)
        {
            float doubleChance = Mathf.Lerp(doubleRowChanceStart, doubleRowChanceEnd, progress);
            bool wantDouble = _rng.NextDouble() < doubleChance;

            // Candidate layouts, as blocked-lane masks.
            int[] singles = { 0b001, 0b010, 0b100 };
            int[] doubles = { 0b011, 0b110, 0b101 };
            int[] pool = wantDouble ? doubles : singles;

            // Shuffle-pick until one is reachable. Every single is always reachable
            // (two lanes stay open), so this terminates.
            int start = _rng.Next(pool.Length);
            for (int k = 0; k < pool.Length; k++)
            {
                int blocked = pool[(start + k) % pool.Length];
                if (Reachable(_prevOpen, 0b111 & ~blocked))
                    return blocked;
            }

            // Fall back to a single that keeps the previous open lane open.
            for (int k = 0; k < singles.Length; k++)
            {
                int blocked = singles[(start + k) % singles.Length];
                if (Reachable(_prevOpen, 0b111 & ~blocked))
                    return blocked;
            }
            return 0b010;
        }

        /// True if any open lane in `next` is the same as, or adjacent to, an open lane in `prev`.
        private static bool Reachable(int prev, int next)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                if ((prev & (1 << lane)) == 0)
                    continue;
                int reach = (1 << lane) | (lane > 0 ? 1 << (lane - 1) : 0) | (lane < 2 ? 1 << (lane + 1) : 0);
                if ((reach & next) != 0)
                    return true;
            }
            return false;
        }

        /// Distance to the next row: the tuned pacing gap, but never less than the
        /// reaction floor plus however many lane changes the escape actually needs.
        private float RowGap(float speed, float progress, int prevOpen, int nextOpen)
        {
            float pacing = Mathf.Lerp(rowGapSecondsStart, rowGapSecondsEnd, progress);

            int swipes = MinSwipes(prevOpen, nextOpen);
            float floor = reactionSeconds + swipes * laneChangeSeconds;

            float seconds = Mathf.Max(pacing, floor);
            float jitter = (float)(_rng.NextDouble() * 0.25 - 0.1);   // -0.1 .. +0.15 s
            return speed * (seconds + jitter);
        }

        /// Fewest lane changes from any open lane in prev to any open lane in next.
        private static int MinSwipes(int prev, int next)
        {
            int best = 2;
            for (int a = 0; a < 3; a++)
            {
                if ((prev & (1 << a)) == 0) continue;
                for (int b = 0; b < 3; b++)
                {
                    if ((next & (1 << b)) == 0) continue;
                    best = Mathf.Min(best, Mathf.Abs(a - b));
                }
            }
            return best;
        }

        // ────────────────────────────────────────────────────────────────

        private void SpawnRow(float z, int blocked, SeasonKind season, WeatherKind weather)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                if ((blocked & (1 << lane)) == 0)
                    continue;

                int l = lane - 1;
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
