using UnityEngine;

namespace CoastRun
{
    /// Lightweight rain / snow / mist particle FX attached to camera follow.
    public class WeatherFx : MonoBehaviour
    {
        private ParticleSystem _rain;
        private ParticleSystem _snow;
        private ParticleSystem _mist;
        private ParticleSystem _wind;    // 35차: 바람에 날리는 꽃잎·낙엽·눈보라
        private Transform _follow;
        private float _windTilt;         // 비·눈이 옆으로 기울어지는 각도(바람 세기)
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
            if (_wind == null)
            {
                _wind = CreateSpray("WindFx", new Color(1f, 0.8f, 0.85f, 0.9f), 90, 7f, 0.16f, 4f);
                var m = _wind.main; m.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f); m.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f); m.gravityModifier = 0.12f;
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                var rot = _wind.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                var noise = _wind.noise; noise.enabled = true; noise.strength = 1.4f; noise.frequency = 0.6f;
                // 옆에서 불어온다: 왼쪽(마을) → 오른쪽(바다)
                _wind.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                _wind.transform.localPosition = new Vector3(-13f, -3f, 0f);   // 왼쪽(마을 쪽) 위에서 시작해 도로를 가로질러 날아간다
                var sh = _wind.shape; sh.scale = new Vector3(30f, 8f, 2f);
            }
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
            bool windy = weather == WeatherKind.Wind;
            SetActive(_rain, weather == WeatherKind.Rain);
            SetActive(_snow, weather == WeatherKind.Snow || (windy && season == SeasonKind.Winter));
            SetActive(_mist, weather == WeatherKind.Mist || weather == WeatherKind.Cloudy);
            // 35차: 바람 — 계절별 날리는 것: 봄 벚꽃·유채 꽃잎 / 여름 초록 잎·물보라 / 가을 낙엽 / 겨울 눈보라
            Color leaf = season == SeasonKind.Spring ? new Color(1f, 0.78f, 0.86f, 0.95f)
                : season == SeasonKind.Autumn ? new Color(0.92f, 0.52f, 0.18f, 0.95f)
                : season == SeasonKind.Winter ? new Color(0.97f, 0.98f, 1f, 0.9f)
                : new Color(0.55f, 0.80f, 0.45f, 0.85f);
            var wm = _wind.main; wm.startColor = leaf;
            var wr = _wind.GetComponent<ParticleSystemRenderer>(); if (wr != null) wr.material = CoastMaterials.CreateParticle(leaf);
            SetActive(_wind, windy || (weather == WeatherKind.Rain && season == SeasonKind.Autumn));
            // 비·눈 기울기: 바람이면 옆으로, 아니면 수직
            _windTilt = windy ? 28f : (weather == WeatherKind.Rain ? 10f : 4f);
            _rain.transform.localRotation = Quaternion.Euler(0f, 0f, -_windTilt) * Quaternion.Euler(90f, 0f, 0f);
            _snow.transform.localRotation = Quaternion.Euler(0f, 0f, -_windTilt * 0.6f) * Quaternion.Euler(90f, 0f, 0f);
            if (weather == WeatherKind.Snow || (windy && season == SeasonKind.Winter))
            {
                var sm = _snow.main; sm.startSpeed = windy ? 6.5f : 3.5f;
                var se = _snow.emission; se.rateOverTime = windy ? 600 : 350;
            }
        }

        public WeatherKind Current => _weather;

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
