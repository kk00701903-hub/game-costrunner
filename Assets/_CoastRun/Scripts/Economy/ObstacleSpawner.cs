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

        [Header("Oncoming cars (chapter 3+)")]
        [Tooltip("First chapter (1-based) in which cars drive toward the player.")]
        [SerializeField] private int carFromChapter = 3;
        [Tooltip("Rows between cars, at stage start / end.")]
        [SerializeField] private int carEveryRowsStart = 9;
        [SerializeField] private int carEveryRowsEnd = 5;
        [Tooltip("The car's own speed along the road (it closes at this + player speed).")]
        [SerializeField] private float carSpeed = 9f;
        [Tooltip("Seconds of travel around the meeting point kept free of other rows.")]
        [SerializeField] private float carClearSeconds = 1.2f;

        private OncomingCar _car;
        private int _carLaneMask;
        private float _carMeetZ;
        private int _rowsUntilCar = 6;

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
            _rowsUntilCar = carEveryRowsStart;
            if (_car != null)
                Destroy(_car.gameObject);
            _car = null;
        }

        private bool _suppressed;

        /// Bonus Time: no new rows, and everything already ahead of the player is
        /// removed so the carpet of jellies is genuinely free to run through.
        public void SetSuppressed(bool on)
        {
            _suppressed = on;
            if (!on || _root == null || player == null)
                return;
            float z = player.PathDistance;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(c.position) > z - 2f)
                    Destroy(c.gameObject);
            }
            _prevOpen = 0b111;
        }

        private void Update()
        {
            if (player == null || _root == null)
                return;

            float z = player.PathDistance;
            float speed = Mathf.Max(6f, player.Speed);

            if (_suppressed)
            {
                // Keep the cursor just ahead so rows resume right after Bonus Time.
                _nextSpawnZ = Mathf.Max(_nextSpawnZ, z + 25f);
                return;
            }

            float progress = 0f;
            int chapter = 1;
            SeasonKind season = SeasonKind.Summer;
            var stages = StageManager.Instance;
            if (stages != null)
            {
                progress = stages.Current != null ? stages.StageProgress01 : 0f;
                chapter = stages.ChapterIndex;
                season = StageManager.ChapterAsSeason(chapter);
            }
            WeatherKind weather = seasonWeather != null ? seasonWeather.CurrentWeather : WeatherKind.Clear;

            if (_car == null)
                _carLaneMask = 0;

            while (_nextSpawnZ < z + spawnAhead)
            {
                bool carsAllowed = chapter >= carFromChapter && progress > 0.06f && progress < 0.93f;
                if (carsAllowed && _car == null && _rowsUntilCar <= 0)
                {
                    PlanCar(z, speed, progress);
                    continue;
                }

                int blocked = PlanRow(progress);
                _rowsUntilCar--;

                if (_carLaneMask != 0)
                {
                    // A car is on its way down one lane. Rows it still has to drive
                    // through leave that lane empty, and the stretch where it meets the
                    // player has no other row at all — the car is the row there.
                    blocked &= ~_carLaneMask;
                    if (Mathf.Abs(_nextSpawnZ - _carMeetZ) < speed * carClearSeconds)
                        blocked = 0;
                }

                if (blocked != 0)
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

        /// Plans an oncoming car whose meeting point with the player is the next row
        /// slot. It starts far enough up the road that, at the player's current speed,
        /// both arrive at that slot together; the slot itself blocks only the car's lane,
        /// so an escape is always one swipe away like any single row.
        /// Editor aid (Coast Run/Debug): every oncoming vehicle becomes a bus.
        public const string DebugForceBusKey = "CoastRun.Debug.ForceBus";
        public static bool DebugForceBus
        {
            get => PlayerPrefs.GetInt(DebugForceBusKey, 0) != 0;
            set { PlayerPrefs.SetInt(DebugForceBusKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        private void PlanCar(float playerZ, float speed, float progress)
        {
            // Pick a lane the previous row left open — the player is likely there, which
            // is exactly what makes the car a dodge instead of a freebie.
            int lane = _rng.Next(3);
            for (int k = 0; k < 3; k++)
            {
                int candidate = (lane + k) % 3;
                if ((_prevOpen & (1 << candidate)) != 0)
                {
                    lane = candidate;
                    break;
                }
            }

            float meetZ = _nextSpawnZ;
            float secondsToMeet = Mathf.Max(0.5f, (meetZ - playerZ) / Mathf.Max(4f, speed));
            float startZ = meetZ + carSpeed * secondsToMeet;

            // From chapter 4 a third of the traffic is a city bus: slower, but a wall.
            int chapterNow = StageManager.Instance != null ? StageManager.Instance.ChapterIndex : 1;
            var kind = chapterNow >= 4 && _rng.NextDouble() < 0.35 ? OncomingCar.Kind.Bus : OncomingCar.Kind.Van;
            if (DebugForceBus) kind = OncomingCar.Kind.Bus;
            float vSpeed = kind == OncomingCar.Kind.Bus ? carSpeed * 0.8f : carSpeed;
            startZ = meetZ + vSpeed * secondsToMeet;
            _car = OncomingCar.Spawn(_root, player, startZ, lane - 1, laneWidth, vSpeed, _rng, kind);
            _carLaneMask = 1 << lane;
            _carMeetZ = meetZ;

            int open = 0b111 & ~_carLaneMask;
            // Extra breathing room after the car: the swerve happens at closing speed.
            float gap = RowGap(speed, progress, _prevOpen, open) + speed * 0.5f;
            _prevOpen = open;
            _nextSpawnZ += gap;
            _rowsUntilCar = Mathf.RoundToInt(Mathf.Lerp(carEveryRowsStart, carEveryRowsEnd, progress))
                            + _rng.Next(3) - 1;
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
