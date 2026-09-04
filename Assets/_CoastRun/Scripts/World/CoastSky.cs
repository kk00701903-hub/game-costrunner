using UnityEngine;

namespace CoastRun
{
    /// Atmosphere settings driven later by DynamicEnvironmentManager (serialized, not hardcoded).
    [System.Serializable]
    public class CoastFogSettings
    {
        public bool enabled = true;
        public FogMode mode = FogMode.Linear;
        public float start = 45f;
        public float end = 170f;
    }

    /// Gradient sky + parallax clouds + fog synced to sky color.
    public class CoastSky : MonoBehaviour
    {
        [SerializeField] private CoastFogSettings fog = new CoastFogSettings();
        [SerializeField] private CloudLayerScroller cloudScroller;

        private Transform _follow;
        private Transform _dome;
        private Texture2D _generatedSky;
        private Color _skyColor = new Color(0.31f, 0.66f, 0.85f);
        private float _atmosphere = 1f;

        public CoastFogSettings Fog => fog;
        public Color CurrentSkyColor => _skyColor;
        public float AtmosphereThickness => _atmosphere;

        public void Build(Transform follow)
        {
            _follow = follow;
            _skyColor = CoastPalette.SkyTop;
            ApplyRenderSettings(_skyColor);
            BuildGradientDome();
            EnsureCloudScroller();
            cloudScroller.Build(follow);
        }

        public void SetSkyColor(Color sky, bool instant = false)
        {
            _skyColor = sky;
            if (instant)
                ApplyRenderSettings(sky);
            else
            {
                // Fog stays locked to sky — never drift apart.
                RenderSettings.fogColor = sky;
                Camera cam = Camera.main;
                if (cam != null)
                    cam.backgroundColor = sky;
            }
        }

        /// Sky tint + atmosphere thickness driven by DynamicEnvironmentManager.SetTime.
        public void SetAtmosphere(Color skyTint, float thickness)
        {
            _skyColor = skyTint;
            _atmosphere = Mathf.Clamp(thickness, 0.5f, 2f);
            RenderSettings.fogColor = skyTint;

            Camera cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = skyTint;

            if (_dome == null)
                return;

            var mr = _dome.GetComponent<Renderer>();
            if (mr == null || mr.sharedMaterial == null)
                return;

            var mat = mr.material;
            Color tint = Color.Lerp(Color.white, skyTint, 0.5f);
            float dim = Mathf.Lerp(1.05f, 0.72f, Mathf.InverseLerp(0.7f, 1.5f, _atmosphere));
            tint *= dim;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            if (mat.HasProperty("_SkyTint"))
                mat.SetColor("_SkyTint", skyTint);
            if (mat.HasProperty("_AtmosphereThickness"))
                mat.SetFloat("_AtmosphereThickness", _atmosphere);
        }

        public void ApplyFogSettings(CoastFogSettings settings)
        {
            if (settings != null)
                fog = settings;
            ApplyRenderSettings(_skyColor);
        }

        private void ApplyRenderSettings(Color sky)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(sky, CoastPalette.SkyHorizon, 0.35f) * 0.85f;

            RenderSettings.fog = fog != null && fog.enabled;
            if (fog != null)
            {
                RenderSettings.fogMode = fog.mode;
                if (fog.mode == FogMode.Linear)
                {
                    RenderSettings.fogStartDistance = fog.start;
                    RenderSettings.fogEndDistance = fog.end;
                }
                else
                {
                    RenderSettings.fogDensity = 0.0011f;
                }
            }

            // Critical: fog == sky, or distant props float as ghosts.
            RenderSettings.fogColor = sky;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = sky;
            }
        }

        private void BuildGradientDome()
        {
            if (_dome != null)
                CoastEditUtil.DestroyObject(_dome.gameObject);

            if (_generatedSky != null)
                Destroy(_generatedSky);
            _generatedSky = SkyTextureGenerator.CreatePortraitSky(512, 896);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SkyGradient";
            go.transform.SetParent(transform, false);
            // High + far so it fills upper third of portrait framing.
            go.transform.localPosition = new Vector3(0f, 62f, 160f);
            go.transform.localScale = new Vector3(220f, 150f, 1f);
            go.transform.localRotation = Quaternion.identity;
            CoastEditUtil.DestroyCollider(go);

            var mat = CoastMaterials.SetFlat(ArtAssets.CreateTexturedUnlit(_generatedSky, Color.white));
            var mr = go.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _dome = go.transform;
        }

        private void EnsureCloudScroller()
        {
            if (cloudScroller == null)
                cloudScroller = GetComponent<CloudLayerScroller>();
            if (cloudScroller == null)
                cloudScroller = gameObject.AddComponent<CloudLayerScroller>();
        }

        private void LateUpdate()
        {
            if (_follow == null)
                return;

            transform.SetPositionAndRotation(_follow.position, Quaternion.identity);

            if (_dome != null)
            {
                Vector3 bp = _dome.localPosition;
                bp.x = _follow.position.x * 0.01f;
                _dome.localPosition = bp;
            }

            // Keep fog locked to live sky tint (day-cycle blends Update background).
            Camera cam = Camera.main;
            if (cam != null && RenderSettings.fog)
                RenderSettings.fogColor = cam.backgroundColor;
        }

        private void OnDestroy()
        {
            if (_generatedSky != null)
                Destroy(_generatedSky);
        }

        public static Color GetCameraClearColor() => CoastPalette.SkyTop;
    }
}
