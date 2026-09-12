using UnityEngine;

namespace CoastRun
{
    /// Lightweight rain / snow / mist particle FX attached to camera follow.
    public class WeatherFx : MonoBehaviour
    {
        private ParticleSystem _rain, _rainNear;   // 48차-10: 카메라 앞 근경 층(굵은 빗줄기)
        private ParticleSystem _snow, _snowNear;   // 48차-10: 큰 눈송이 근경 층
        private ParticleSystem _mist;
        private ParticleSystem _wind;    // 35차: 바람에 날리는 꽃잎·낙엽·눈보라
        private ParticleSystem _petals, _petalsNear;  // 63차: 봄엔 날씨와 상관없이 벚꽃잎이 흩날린다(원경·근경)
        private Transform _follow;
        private float _windTilt;         // 비·눈이 옆으로 기울어지는 각도(바람 세기)
        private WeatherKind _weather = WeatherKind.Clear;
        // 48차-12(사용자): 비·눈은 한 번에 계속 오지 않고 왔다 안 왔다 — 러닝 시간의 약 50%. ON 10~18초 / OFF 10~18초, 2초 페이드.
        private float _precipTimer, _precipPhase = 14f; private bool _precipOn = true; private float _precipMul = 1f;
        private int _rainBase = 800, _rainNearBase = 160, _snowBase = 350, _snowNearBase = 70;
        private System.Random _precipRng = new System.Random(unchecked((int)System.DateTime.Now.Ticks));

        public void Bind(Transform follow)
        {
            _follow = follow;
            EnsureSystems();
            SetState(WeatherKind.Clear, SeasonKind.Summer);
        }

        private void EnsureSystems()
        {
            // 48차-10(사용자: 비·눈이 잘 안 보인다): 빗방울은 속도 방향으로 늘어난 줄(Stretched Billboard), 눈은 크고 하얗게 + 흔들림.
            //        원경(넓게, 많이) + 근경(카메라 앞, 굵게) 두 층으로 깊이감. 수명은 바닥까지 떨어지는 시간만큼만.
            if (_rain == null)
            {
                _rain = CreateSpray("RainFx", new Color(0.82f, 0.90f, 1f, 0.80f), 800, 24f, 0.05f, 1.6f);   // 48차-11: 사용자 "너무 강하다" → 절반
                Stretch(_rain, 0.09f);
                var m = _rain.main; m.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.065f); m.startSpeed = new ParticleSystem.MinMaxCurve(20f, 28f);
                var sh = _rain.shape; sh.scale = new Vector3(24f, 1f, 36f);
            }
            if (_rainNear == null)
            {
                _rainNear = CreateSpray("RainNearFx", new Color(0.88f, 0.94f, 1f, 0.9f), 160, 26f, 0.09f, 0.9f);
                Stretch(_rainNear, 0.11f);
                var m = _rainNear.main; m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
                var sh = _rainNear.shape; sh.scale = new Vector3(9f, 1f, 8f);
                _rainNear.transform.localPosition = new Vector3(0f, -2.5f, -5f);   // 카메라 바로 앞·위
            }
            if (_snow == null)
            {
                _snow = CreateSpray("SnowFx", new Color(1f, 1f, 1f, 1f), 350, 2.4f, 0.22f, 7f);
                var m = _snow.main; m.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
                var noise = _snow.noise; noise.enabled = true; noise.strength = 0.9f; noise.frequency = 0.35f; noise.scrollSpeed = 0.3f;
                var sh = _snow.shape; sh.scale = new Vector3(24f, 1f, 34f);
            }
            if (_snowNear == null)
            {
                _snowNear = CreateSpray("SnowNearFx", new Color(1f, 1f, 1f, 1f), 70, 1.8f, 0.45f, 5f);
                var m = _snowNear.main; m.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.42f); m.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
                var noise = _snowNear.noise; noise.enabled = true; noise.strength = 1.2f; noise.frequency = 0.3f; noise.scrollSpeed = 0.35f;
                var sh = _snowNear.shape; sh.scale = new Vector3(9f, 1f, 8f);
                _snowNear.transform.localPosition = new Vector3(0f, -2.5f, -5f);
            }
            if (_mist == null)
                _mist = CreateSpray("MistFx", new Color(0.85f, 0.88f, 0.9f, 0.25f), 80, 0.8f, 0.55f, 2f);
            if (_petals == null)
            {
                // 63차(사용자): 봄 벚꽃 — 위에서 살랑살랑 떨어지는 분홍 꽃잎(회전·노이즈), 원경 + 근경 두 겹.
                //   첫 확인에서 희미해 눈에 안 띄었다 → 채도·개수 올리고 카메라 앞 근경 층을 추가(근경은 너무 크면 분홍 덩어리로 보여 0.16~0.28).
                var petalTex = ArtAssets.LoadTexture("Fx_Petal");
                _petals = MakePetals("PetalFx", 120, new ParticleSystem.MinMaxCurve(0.14f, 0.26f), new Vector3(22f, 1f, 30f), Vector3.zero, petalTex);
                _petalsNear = MakePetals("PetalNearFx", 40, new ParticleSystem.MinMaxCurve(0.11f, 0.19f), new Vector3(9f, 1f, 8f), new Vector3(0f, -2.5f, -5f), petalTex);
            }
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

        /// 빗줄기: 속도 방향으로 늘어난 빌보드.
        private static void Stretch(ParticleSystem ps, float velocityScale)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 0f; r.velocityScale = velocityScale;
        }

        /// 63차: 벚꽃잎 층 하나 — 진한 분홍~연분홍, 회전하며 노이즈로 흔들리며 천천히 떨어진다.
        private ParticleSystem MakePetals(string name, int rate, ParticleSystem.MinMaxCurve size, Vector3 box, Vector3 localPos, Texture2D tex)
        {
            var ps = CreateSpray(name, new Color(1f, 0.66f, 0.80f, 1f), rate, 1.2f, 0.3f, 7f);
            var m = ps.main; m.startSize = size; m.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.7f); m.gravityModifier = 0.05f;
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.78f, 1f), new Color(1f, 0.86f, 0.92f, 1f));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 1.6f; noise.frequency = 0.45f; noise.scrollSpeed = 0.4f;
            var sh = ps.shape; sh.scale = box;
            ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ps.transform.localPosition = localPos;
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            if (tex != null && pr.material.HasProperty("_BaseMap")) pr.material.SetTexture("_BaseMap", tex);
            return ps;
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
            UpdatePrecipCycle();
        }

        /// 비·눈 ON/OFF 사이클. 날씨가 비/눈이 아니면 손대지 않는다(SetActive 가 이미 껐다).
        private void UpdatePrecipCycle()
        {
            bool precip = _weather == WeatherKind.Rain || _weather == WeatherKind.Snow;
            if (!precip || Time.timeScale <= 0f) return;
            _precipTimer += Time.deltaTime;
            if (_precipTimer >= _precipPhase)
            {
                _precipTimer = 0f; _precipOn = !_precipOn;
                _precipPhase = 10f + (float)_precipRng.NextDouble() * 8f;
            }
            float target = _precipOn ? 1f : 0f;
            float next = Mathf.MoveTowards(_precipMul, target, Time.deltaTime / 2f);
            if (Mathf.Abs(next - _precipMul) < 0.0005f) return;
            _precipMul = next;
            ApplyPrecipMul();
        }

        private void ApplyPrecipMul()
        {
            var re = _rain.emission; re.rateOverTime = _rainBase * _precipMul;
            var rn = _rainNear.emission; rn.rateOverTime = _rainNearBase * _precipMul;
            var se = _snow.emission; se.rateOverTime = _snowBase * _precipMul;
            var sn = _snowNear.emission; sn.rateOverTime = _snowNearBase * _precipMul;
        }

        public void SetState(WeatherKind weather, SeasonKind season)
        {
            _weather = weather;
            EnsureSystems();
            bool windy = weather == WeatherKind.Wind;
            bool rain = weather == WeatherKind.Rain, snow = weather == WeatherKind.Snow || (windy && season == SeasonKind.Winter);
            // 비/눈으로 바뀌면 사이클을 '오는 중'으로 시작
            if (rain || weather == WeatherKind.Snow) { _precipOn = true; _precipTimer = 0f; _precipPhase = 12f + (float)_precipRng.NextDouble() * 6f; _precipMul = 1f; }
            if (rain) { var re = _rain.emission; re.rateOverTime = _rainBase * _precipMul; var rn = _rainNear.emission; rn.rateOverTime = _rainNearBase * _precipMul; }
            SetActive(_rain, rain); SetActive(_rainNear, rain);
            SetActive(_snow, snow); SetActive(_snowNear, snow);
            SetActive(_mist, weather == WeatherKind.Mist || weather == WeatherKind.Cloudy);
            bool petals = season == SeasonKind.Spring && weather != WeatherKind.Rain;   // 63차: 봄이면 늘 벚꽃잎
            SetActive(_petals, petals); SetActive(_petalsNear, petals);
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
            var rainRot = Quaternion.Euler(0f, 0f, -_windTilt) * Quaternion.Euler(90f, 0f, 0f);
            var snowRot = Quaternion.Euler(0f, 0f, -_windTilt * 0.6f) * Quaternion.Euler(90f, 0f, 0f);
            _rain.transform.localRotation = rainRot; _rainNear.transform.localRotation = rainRot;
            _snow.transform.localRotation = snowRot; _snowNear.transform.localRotation = snowRot;
            if (snow)
            {
                var sm = _snow.main; sm.startSpeed = windy ? new ParticleSystem.MinMaxCurve(5f, 7.5f) : new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
                _snowBase = windy ? 550 : 350; _snowNearBase = windy ? 110 : 70;
                var se = _snow.emission; se.rateOverTime = _snowBase * _precipMul;
                var nm = _snowNear.main; nm.startSpeed = windy ? new ParticleSystem.MinMaxCurve(4f, 6f) : new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
                var ne = _snowNear.emission; ne.rateOverTime = _snowNearBase * _precipMul;
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
