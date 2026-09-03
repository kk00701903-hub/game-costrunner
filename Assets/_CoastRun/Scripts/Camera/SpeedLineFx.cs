using UnityEngine;

namespace CoastRun
{
    /// Screen-edge speed lines — emission scales with NormalizedSpeed.
    public class SpeedLineFx : MonoBehaviour
    {
        [SerializeField] private float maxRate = 120f;
        [SerializeField] private float minSpeedToEmit = 0.08f;

        private ParticleSystem _ps;
        private ParticleSystem.EmissionModule _emission;
        private float _ratio;

        public void EnsureBuilt()
        {
            if (_ps != null)
                return;

            var go = new GameObject("SpeedLines");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _ps = go.AddComponent<ParticleSystem>();
            var main = _ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.18f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;
            main.gravityModifier = 0f;

            var emission = _ps.emission;
            emission.rateOverTime = 0f;
            _emission = emission;

            var shape = _ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(2.4f, 4.2f, 0.1f);
            shape.position = new Vector3(0f, 0f, 1.2f);

            var vol = _ps.velocityOverLifetime;
            vol.enabled = true;
            vol.z = new ParticleSystem.MinMaxCurve(18f);
            vol.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var color = _ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    // Strong at edges (birth), fade toward center/end of life.
                    new GradientAlphaKey(0.55f, 0f),
                    new GradientAlphaKey(0.15f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = grad;

            var size = _ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4.5f;
            renderer.velocityScale = 0.08f;
            renderer.material = CoastMaterials.CreateUnlit(
                () => new Color(1f, 1f, 1f, 0.4f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _ps.Play();
        }

        /// NearMiss / juice burst — extra streaks without changing steady speed emission.
        public void Burst(int count)
        {
            if (_ps == null)
                EnsureBuilt();

            count = Mathf.Clamp(count, 4, 80);
            _ps.Emit(count);
        }

        public void SetSpeedRatio(float normalizedSpeed)
        {
            _ratio = Mathf.Clamp01(normalizedSpeed);
            if (_ps == null)
                EnsureBuilt();

            float rate = _ratio < minSpeedToEmit ? 0f : maxRate * Mathf.Pow(_ratio, 1.35f);
            var emission = _ps.emission;
            emission.rateOverTime = rate;
        }
    }
}
