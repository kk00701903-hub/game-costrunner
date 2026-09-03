using UnityEngine;

namespace CoastRun
{
    /// Removed from run HUD — season cycle UI conflicted with one-day lightingT design.
    /// Kept as empty stub so existing scene refs do not break.
    public class SeasonWeatherHud : MonoBehaviour
    {
        public void Bind(SeasonWeatherDirector seasonWeather)
        {
            // Destroy any leftover label from previous builds.
            var canvas = GameObject.Find("SeasonWeatherCanvas");
            if (canvas != null)
                Destroy(canvas);

            var hud = GameObject.Find("CoastRunHUD");
            if (hud != null)
            {
                var t = hud.transform.Find("TopBar/SeasonWeather");
                if (t != null)
                    Destroy(t.gameObject);
            }

            enabled = false;
        }
    }
}
