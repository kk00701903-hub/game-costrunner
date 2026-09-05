using UnityEngine;

namespace CoastRun
{
    /// Lightweight rain / snow / mist particle FX attached to camera follow.
    public class WeatherFx : MonoBehaviour
    {
        private ParticleSystem _rain;
        private ParticleSystem _snow;
        private ParticleSystem _mist;
        private Transform _follow;
        private WeatherKind _weather = WeatherKind.Clear;

        public void Bind(Transform follow)
        {
            _follow = follow;
            EnsureSystems();
            SetState(WeatherKind.Clear, SeasonKind.Summer);
        }

        private void EnsureSystems()
        {
            if (_rain == null)
                _rain = CreateSpray("RainFx", new Color(0.7f, 0.8f, 0.95f, 0.55f), 900, 14f, 0.04f, 18f);
            if (_snow == null)
                _snow = CreateSpray("SnowFx", new Color(0.95f, 0.97f, 1f, 0.9f), 350, 3.5f, 0.12f, 8f);
            if (_mist == null)
                _mist = CreateSpray("MistFx", new Color(0.85f, 0.88f, 0.9f, 0.25f), 80, 0.8f, 0.55f, 2f);
        }

        private ParticleSystem CreateSpray(string name, Color color, int rate, float speed, float size, float lifetime)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSize = size;
            main.startSpeed = speed;
            main.startLifetime = lifetime;
            main.maxParticles = rate * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 1f, 30f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CoastMaterials.CreateParticle(color);

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private void LateUpdate()
        {
            if (_follow == null)
                return;
            transform.position = _follow.position + Vector3.up * 8f + _follow.forward * 6f;
        }

        public void SetState(WeatherKind weather, SeasonKind season)
        {
            _weather = weather;
            EnsureSystems();
            SetActive(_rain, weather == WeatherKind.Rain);
            SetActive(_snow, weather == WeatherKind.Snow);
            SetActive(_mist, weather == WeatherKind.Mist || weather == WeatherKind.Cloudy);
        }

        private static void SetActive(ParticleSystem ps, bool on)
        {
            if (ps == null)
                return;
            if (on && !ps.isPlaying)
                ps.Play();
            if (!on && ps.isPlaying)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
