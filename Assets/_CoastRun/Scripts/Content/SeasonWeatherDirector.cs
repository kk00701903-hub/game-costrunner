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
