using UnityEngine;

namespace CoastRun
{
    /// Chapter art theme + weather FX. Season is NOT a clock here — time of day belongs
    /// to DynamicEnvironmentManager.SetTime, driven by StageManager's lightingT.
    ///
    /// What this still owns, and what reads it:
    ///   - SetChapterTheme(chapter) → GameSession calls it per stage; picks the prop and
    ///     foliage theme via StageManager.ChapterAsSeason
    ///     (CH1 summer, CH2 spring, CH3/4 autumn, CH5 winter)
    ///   - CurrentWeather → CoastAudioManager (rain/snow ambience), ObstacleSpawner (hazard mix)
    public class SeasonWeatherDirector : MonoBehaviour
    {
        [SerializeField] private WeatherFx weatherFx;
        [SerializeField] private WeatherKind weather = WeatherKind.Clear;

        private SeasonKind _season = SeasonKind.Summer;
        private WeatherKind _appliedWeather;
        private SeasonKind _appliedSeason;

        public SeasonKind CurrentSeason => _season;
        public WeatherKind CurrentWeather => weather;
        public SeasonPalettes.Snapshot Snapshot => SeasonPalettes.Get(_season);

        public void Bind(PlayerController playerController, DynamicEnvironmentManager env, WeatherFx fx)
        {
            weatherFx = fx;
            // Lock to summer-clear look; chapter props still bias via SetChapterTheme.
            _season = SeasonKind.Summer;
            weather = WeatherKind.Clear;
            Apply();
        }

        public void SetChapterTheme(int chapter)
        {
            _season = StageManager.ChapterAsSeason(chapter);
            RollWeather(_season, chapter * 977 + System.Environment.TickCount);
        }

        // ── 35차: 계절별 날씨 ──────────────────────────────────────────────
        // 봄: 맑음 50 / 바람(꽃잎) 22 / 흐림 15 / 비 13   여름: 맑음 45 / 소나기 28 / 흐림 15 / 안개 12
        // 가을: 맑음 40 / 바람(낙엽) 32 / 흐림 15 / 비 13   겨울: 눈 42 / 맑음 28 / 바람(눈보라) 18 / 흐림 12
        private float _changeTimer, _nextChange = 60f;
        private System.Random _wrng = new System.Random();

        public static WeatherKind Roll(SeasonKind s, System.Random rng)
        {
            int r = rng.Next(100);
            switch (s)
            {
                case SeasonKind.Spring: return r < 50 ? WeatherKind.Clear : r < 72 ? WeatherKind.Wind : r < 87 ? WeatherKind.Cloudy : WeatherKind.Rain;
                case SeasonKind.Autumn: return r < 40 ? WeatherKind.Clear : r < 72 ? WeatherKind.Wind : r < 87 ? WeatherKind.Cloudy : WeatherKind.Rain;
                case SeasonKind.Winter: return r < 42 ? WeatherKind.Snow : r < 70 ? WeatherKind.Clear : r < 88 ? WeatherKind.Wind : WeatherKind.Cloudy;
                default: return r < 45 ? WeatherKind.Clear : r < 73 ? WeatherKind.Rain : r < 88 ? WeatherKind.Cloudy : WeatherKind.Mist;
            }
        }

        /// 런 시작: 계절에 맞는 날씨를 뽑고, 45~90초마다 다시 굴린다(같은 날씨 연속은 피함).
        public void RollWeather(SeasonKind season, int seed)
        {
            _season = season;
            _wrng = new System.Random(seed);
            weather = Roll(season, _wrng);
            _changeTimer = 0f; _nextChange = 45f + (float)_wrng.NextDouble() * 45f;
            Apply();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            _changeTimer += Time.deltaTime;
            if (_changeTimer < _nextChange) return;
            _changeTimer = 0f; _nextChange = 45f + (float)_wrng.NextDouble() * 45f;
            var next = Roll(_season, _wrng);
            if (next == weather) next = Roll(_season, _wrng);
            weather = next;
            Apply();
        }

        public void ForceSeason(SeasonKind season, WeatherKind w, bool instant)
        {
            _season = season;
            weather = w;
            Apply();
        }

        private void LateUpdate()
        {
            // Nothing cycles with distance any more, so this only exists to pick up an
            // inspector tweak during a play session. Push on change, not every frame.
            if (weather == _appliedWeather && _season == _appliedSeason)
                return;
            Apply();
        }

        private void Apply()
        {
            _appliedWeather = weather;
            _appliedSeason = _season;
            weatherFx?.SetState(weather, _season);
        }
    }
}
