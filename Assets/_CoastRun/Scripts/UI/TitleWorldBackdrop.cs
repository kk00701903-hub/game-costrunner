using UnityEngine;

namespace CoastRun
{
    /// Live title backdrop — in-game promenade geometry, not a still image.
    /// Cleared state: blue hour, no girl, closer tower + beacon.
    public class TitleWorldBackdrop : MonoBehaviour
    {
        [SerializeField] private float panDuration = 30f;
        [SerializeField] private float panLateral = 3.2f;

        private Camera _cam;
        private Transform _camPivot;
        private DynamicEnvironmentManager _env;
        private GameObject _girl;
        private GameObject _tower;
        private Light _beacon;
        private CoastSky _sky;
        private bool _cleared;
        private float _panT;
        private Vector3 _camBase;

        public void Build(bool cleared)
        {
            _cleared = cleared;
            Application.targetFrameRate = 60;
            CoastPalette.Bind(CoastConfigRegistry.CoastPaletteConfig);

            EnsureLight();
            EnsureCamera();
            BuildWorldStrip();
            BuildGirl();
            BuildTower();
            EnsureEnvironment(cleared ? 0.92f : 0.05f);
            CoastPostStack.EnsureGlobalVolume();
        }

        private void EnsureLight()
        {
            var go = GameObject.Find("Directional Light");
            Light light;
            if (go == null)
            {
                go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
            }
            else
            {
                light = go.GetComponent<Light>() ?? go.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            CoastUrpShadows.Apply();
        }

        private void EnsureCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            // Re-enable world rendering (legacy menu set cullingMask=0).
            _cam.cullingMask = ~0;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            if (_cam.GetComponent<CoastPortraitViewport>() == null)
                _cam.gameObject.AddComponent<CoastPortraitViewport>();

            _camPivot = _cam.transform;
            // Framing: behind / slightly above the promenade, looking along the coast.
            _camBase = DownhillPath.Point(18f, 0f, 2.4f) + DownhillPath.Rotation * new Vector3(0.4f, 1.6f, -7.5f);
            _camPivot.position = _camBase;
            Vector3 look = DownhillPath.Point(28f, 0f, 1.4f);
            _camPivot.rotation = Quaternion.LookRotation((look - _camBase).normalized, Vector3.up);
            _cam.fieldOfView = 42f;
        }

        private void BuildWorldStrip()
        {
            if (GameObject.Find("TitleWorld") != null)
                return;

            var world = new GameObject("TitleWorld");
            var mapRoot = new GameObject("Segments").transform;
            mapRoot.SetParent(world.transform, false);

            // A short static strip — enough for the slow lateral pan.
            for (int i = -1; i <= 4; i++)
                PromenadeSegmentBuilder.Build(i, mapRoot);

            var skyGo = new GameObject("Sky");
            skyGo.transform.SetParent(world.transform, false);
            _sky = skyGo.AddComponent<CoastSky>();
            _sky.Build(_camPivot);

            var seaGo = new GameObject("Sea");
            seaGo.transform.SetParent(world.transform, false);
            var sea = seaGo.AddComponent<CoastSea>();
            sea.Build(_camPivot);

            var env = world.AddComponent<EnvironmentManager>();
            env.Configure(sea);
            env.SetFollow(_camPivot);
        }

        private void BuildGirl()
        {
            var existing = GameObject.Find("TitleGirl");
            if (existing != null)
                Destroy(existing);

            if (_cleared)
                return; // Cleared title: she is simply gone. No caption.

            _girl = new GameObject("TitleGirl");
            _girl.transform.SetPositionAndRotation(
                DownhillPath.Point(22f, 0f, 0.05f),
                DownhillPath.Rotation * Quaternion.Euler(0f, 180f, 0f));

            // Standing pose — no PlayerController drive.
            var visual = _girl.AddComponent<CoastPlayerVisual>();
            visual.Build();
            visual.SetMenuPose(true);
        }

        private void BuildTower()
        {
            var existing = GameObject.Find("TitleTower");
            if (existing != null)
                Destroy(existing);

            // First play: tiny on the horizon. Cleared: closer, readable, beacon blinks.
            float z = _cleared ? 95f : 220f;
            float lateral = _cleared ? -5.5f : -7.5f;
            float scale = _cleared ? 1.15f : 0.45f;

            _tower = DestinationGate.CreateVisual(null, z);
            if (_tower == null)
                return;
            _tower.name = "TitleTower";
            // CreateVisual already placed at z,-6; re-seat for title framing.
            _tower.transform.SetParent(null, true);
            _tower.transform.position = DownhillPath.Point(z, lateral, 0f);
            _tower.transform.rotation = DownhillPath.Rotation;
            _tower.transform.localScale = Vector3.one * scale;

            // Soft silhouette — unlit dark so it reads as a distant mark.
            foreach (var r in _tower.GetComponentsInChildren<Renderer>())
            {
                if (r == null)
                    continue;
                Color c = _cleared
                    ? new Color(0.25f, 0.28f, 0.35f)
                    : new Color(0.18f, 0.22f, 0.28f);
                r.sharedMaterial = CoastMaterials.CreateUnlit(c);
            }

            var beaconGo = new GameObject("TowerBeacon");
            beaconGo.transform.SetParent(_tower.transform, false);
            beaconGo.transform.localPosition = new Vector3(0f, 22f, 0f);
            _beacon = beaconGo.AddComponent<Light>();
            _beacon.type = LightType.Point;
            _beacon.range = _cleared ? 32f : 8f;
            _beacon.color = new Color(1f, 0.12f, 0.08f);
            _beacon.intensity = 0f;
            _beacon.enabled = _cleared;
            _beacon.shadows = LightShadows.None;
        }

        private void EnsureEnvironment(float lightingT)
        {
            _env = Object.FindAnyObjectByType<DynamicEnvironmentManager>();
            if (_env == null)
            {
                var go = new GameObject("TitleEnvironment");
                _env = go.AddComponent<DynamicEnvironmentManager>();
            }

            _env.ResetLightingTo(lightingT);
        }

        private void LateUpdate()
        {
            // Infinite slow lateral pan (30s L→R loop).
            _panT += Time.unscaledDeltaTime / Mathf.Max(1f, panDuration);
            if (_panT > 1f)
                _panT -= 1f;

            float u = _panT < 0.5f ? (_panT * 2f) : (2f - _panT * 2f); // ping-pong ease
            u = u * u * (3f - 2f * u);
            float lateral = Mathf.Lerp(-panLateral, panLateral, u);

            if (_camPivot != null)
            {
                Vector3 pos = _camBase + DownhillPath.Rotation * new Vector3(lateral, 0f, 0f);
                _camPivot.position = pos;
                Vector3 look = DownhillPath.Point(28f + lateral * 0.3f, 0f, 1.4f);
                _camPivot.rotation = Quaternion.LookRotation((look - pos).normalized, Vector3.up);
            }

            if (_cleared && _beacon != null)
            {
                float blink = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * 1.15f) > 0f) ? 1f : 0.08f;
                _beacon.intensity = 4.2f * blink;
            }
        }
    }
}
