using UnityEngine;

namespace CoastRun
{
    /// Optional weather FX only — NO season cycling.
    /// Day progression is owned by DynamicEnvironmentManager.SetTime / StageManager.
    public class SeasonWeatherDirector : MonoBehaviour
    {
        [SerializeField] private WeatherFx weatherFx;
        [SerializeField] private WeatherKind weather = WeatherKind.Clear;

        private SeasonKind _season = SeasonKind.Summer;

        public SeasonKind CurrentSeason => _season;
        public WeatherKind CurrentWeather => weather;
        public SeasonPalettes.Snapshot Snapshot => SeasonPalettes.Get(_season);

        public void Bind(PlayerController playerController, DynamicEnvironmentManager env, WeatherFx fx)
        {
            weatherFx = fx;
            // Lock to summer-clear look; chapter props may still bias via StageManager.ChapterAsSeason.
            _season = SeasonKind.Summer;
            weather = WeatherKind.Clear;
            weatherFx?.SetState(weather, _season);
        }

        public void SetChapterTheme(int chapter)
        {
            _season = StageManager.ChapterAsSeason(chapter);
            weatherFx?.SetState(weather, _season);
        }

        private void LateUpdate()
        {
            // Intentionally empty — seasons do not cycle with path distance.
            weatherFx?.SetState(weather, _season);
        }

        public void ForceSeason(SeasonKind season, WeatherKind w, bool instant)
        {
            _season = season;
            weather = w;
            weatherFx?.SetState(weather, _season);
        }
    }
}
