using UnityEngine;

namespace CoastRun
{
    /// Follows the runner: sea plane, sky tint, linear fog synced to sky.
    public class EnvironmentManager : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private CoastSea sea;
        [SerializeField] private CoastFogSettings fog = new CoastFogSettings();

        public CoastFogSettings Fog => fog;

        public void Configure(CoastSea coastSea) => sea = coastSea;

        public void SetFollow(Transform target) => followTarget = target;

        /// Called when distance crosses palette thresholds — fog color always == sky.
        public void ApplyPalette(Color sky, Color fogColor, float fogDensityUnused)
        {
            RenderSettings.fog = fog.enabled;
            RenderSettings.fogMode = fog.mode;
            if (fog.mode == FogMode.Linear)
            {
                RenderSettings.fogStartDistance = fog.start;
                RenderSettings.fogEndDistance = fog.end;
            }
            else
                RenderSettings.fogDensity = Mathf.Max(0.0001f, fogDensityUnused);

            // Prefer sky for fog so far props dissolve into the horizon.
            RenderSettings.fogColor = sky;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = sky;
            }

            var coastSky = Object.FindAnyObjectByType<CoastSky>();
            coastSky?.SetSkyColor(sky, true);
        }
    }
}
