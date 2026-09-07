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
        [SerializeField] private float laneChangeSeconds = 0.22f;   // 14차-9: 레인 이동 0.20s ease-out 에 맞춤
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
        [SerializeField] private int scooterFromChapter = 1;
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
        /// 아케이드(오늘의 런): 날짜 시드로 코스 고정.
        public static int? SeedOverride;

        public void ResetForStage(int stageIndex, float startZ)
        {
            _rng = new System.Random(SeedOverride ?? (1000 + stageIndex * 7919));
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
                // 14차-3: 킥보드 아이는 1챕터부터 마주 온다(목표 이미지). 밴·버스는 carFromChapter부터.
                bool carsAllowed = (chapter >= carFromChapter || chapter >= scooterFromChapter) && progress > 0.06f && progress < 0.93f;
                if (carsAllowed && _car == null && _rowsUntilCar <= 0)
                {
                    PlanCar(z, speed, progress);
                    continue;
                }

                // 14차-3: 점프 패드 구간 — 패드 하나 + 4 m 간격 낮은 장애물 세 줄. 패드를 밟으면 한 번에 넘고,
                // 안 밟아도 한 레인은 늘 비어 있다. 차가 오는 중엔 넣지 않는다.
                bool carFar = _carLaneMask == 0 || Mathf.Abs(_nextSpawnZ + 8f - _carMeetZ) > speed * carClearSeconds + 10f;
                if (carFar && progress > 0.04f && _rowsUntilPad <= 0 && _rng.NextDouble() < 0.7)
                {
                    SpawnJumpPadSection(_nextSpawnZ, speed);
                    _rowsUntilPad = 5 + _rng.Next(5);
                    _prevOpen = 0b111;
                    _nextSpawnZ += 14f + RowGap(speed, progress, 0b111, 0b111);
                    continue;
                }

                int blocked = PlanRow(progress);
                _rowsUntilCar--;
                _rowsUntilPad--;

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
            float doubleChance = Mathf.Lerp(doubleRowChanceStart, doubleRowChanceEnd, progress) + ChapterDifficulty.DoubleLaneBonus;
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
            // 11챕터부터 일부는 '구르는 귤' — 작고 느리지만 점프로만 넘는다.
            if (_rng.NextDouble() < ChapterDifficulty.RollingOrangeChance(ChapterDifficulty.Stage) && !DebugForceBus)
                kind = OncomingCar.Kind.Orange;
            // 14차-2: 킥보드 탄 아이 — 봄부터 나온다. 마주 오는 차량 셋 중 하나꼴, 속도는 밴의 절반.
            if (kind == OncomingCar.Kind.Van && !DebugForceBus && _rng.NextDouble() < 0.35)
                kind = OncomingCar.Kind.Scooter;
            if (chapterNow < carFromChapter && !DebugForceBus)
                kind = OncomingCar.Kind.Scooter;   // 초반 챕터엔 킥보드만
            float vSpeed = (kind == OncomingCar.Kind.Bus ? carSpeed * 0.8f : kind == OncomingCar.Kind.Orange ? carSpeed * 0.55f
                          : kind == OncomingCar.Kind.Scooter ? carSpeed * 0.5f : carSpeed)
                           * ChapterDifficulty.CarSpeedMul;
            startZ = meetZ + vSpeed * secondsToMeet;
            _car = OncomingCar.Spawn(_root, player, startZ, lane - 1, laneWidth, vSpeed, _rng, kind);
            _carLaneMask = 1 << lane;
            _carMeetZ = meetZ;

            int open = 0b111 & ~_carLaneMask;
            // Extra breathing room after the car: the swerve happens at closing speed.
            float gap = RowGap(speed, progress, _prevOpen, open) + speed * 0.5f;
            _prevOpen = open;
            _nextSpawnZ += gap;
            _rowsUntilCar = Mathf.RoundToInt(Mathf.Lerp(carEveryRowsStart, carEveryRowsEnd, progress) * ChapterDifficulty.CarEveryMul)
                            + _rng.Next(3) - 1;
            // 17챕터부터 가끔 차 두 대가 연달아 온다.
            if (ChapterDifficulty.Stage >= ChapterDifficulty.DoubleCarFrom && _rng.NextDouble() < 0.3)
                _rowsUntilCar = Mathf.Min(_rowsUntilCar, 2);
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
            float pacing = Mathf.Lerp(rowGapSecondsStart, rowGapSecondsEnd, progress) * ChapterDifficulty.GapMul
                           * RunRhythm.ObstacleGapMul(_nextSpawnZ);   // 14차: 30초 마디(쉬움/코인/위기)

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

        private int _rowsUntilPad = 2;

        /// 에디터 디버그: 플레이어 20 m 앞, 현재 레인에 점프 패드 구간을 깐다.
        public void DebugSpawnPadAhead()
        {
            if (player == null || _root == null) return;
            float z = player.PathDistance + 20f;
            int lane = player.Lane;
            JumpPad.Spawn(_root, RoadPlacement.OnRoad(z, lane * laneWidth));
            ObstacleId[] low = { ObstacleId.TrafficCone, ObstacleId.Slime, ObstacleId.WetFloorSign };
            for (int row = 0; row < 3; row++)
            {
                float rz = z + 6f + row * 4f;
                var go = ObstacleCatalog.Spawn(low[row], _root, RoadPlacement.OnRoad(rz, lane * laneWidth), lane);
                if (go != null) RoadPlacement.Snap(go, rz, lane * laneWidth);
            }
            _nextSpawnZ = Mathf.Max(_nextSpawnZ, z + 24f);
        }

        private void SpawnJumpPadSection(float z, float speed)
        {
            // 차가 달려오는 레인은 비워 둔다(패드도 장애물도).
            int padLane = _rng.Next(3) - 1;
            for (int k = 0; k < 3 && (_carLaneMask & (1 << (padLane + 1))) != 0; k++)
                padLane = ((padLane + 2) % 3) - 1;
            JumpPad.Spawn(_root, RoadPlacement.OnRoad(z, padLane * laneWidth));
            // 14차-10: 점프대 뒤 하늘 코인 아치(밟으면 포물선을 따라 먹는다)
            if (player != null && player.Config != null)
                CoinSpawner.Instance?.SpawnAirArc(z + 0.6f, padLane, player.Config.jumpForce * JumpPad.LaunchMul, player.Config.gravity, Mathf.Max(speed, player.Speed));
            // 낮은(점프로 넘는) 장애물만. 패드 레인 + 옆 레인 하나를 막고 나머지 하나는 비운다.
            ObstacleId[] low = { ObstacleId.TrafficCone, ObstacleId.Slime, ObstacleId.WetFloorSign, ObstacleId.BikeFallen };
            int other = padLane == 0 ? (_rng.Next(2) == 0 ? -1 : 1) : 0;
            if ((_carLaneMask & (1 << (other + 1))) != 0) other = padLane;
            for (int row = 0; row < 3; row++)
            {
                float rz = z + 6f + row * 4f;
                foreach (int l in (other == padLane ? new[] { padLane } : new[] { padLane, other }))
                {
                    var id = low[_rng.Next(low.Length)];
                    var go = ObstacleCatalog.Spawn(id, _root, RoadPlacement.OnRoad(rz, l * laneWidth), l);
                    if (go != null)
                        RoadPlacement.Snap(go, rz, l * laneWidth);
                }
            }
        }

        private void SpawnRow(float z, int blocked, SeasonKind season, WeatherKind weather)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                if ((blocked & (1 << lane)) == 0)
                    continue;

                int l = lane - 1;
                float lateral = l * laneWidth;
                Vector3 pos = RoadPlacement.OnRoad(z, lateral);
                var id = ObstacleCatalog.Pick(season, weather, _rng, ChapterDifficulty.Stage);
                var go = ObstacleCatalog.Spawn(id, _root, pos, l);
                if (go != null)
                    RoadPlacement.Snap(go, z, lateral);
            }
        }
    }
}
