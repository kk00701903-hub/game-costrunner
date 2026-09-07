using UnityEngine;

namespace CoastRun
{
    /// 14차-11: 장애물이 처음 사정거리(약 26 m)에 들어올 때 한 번, 장애물 '모양 자체'가 색으로 반짝인다 —
    /// 본체 색이 흰빛→빨강 사이를 3번 튀고(0.9 s), 테두리는 빨갛게 달아올랐다 식는다. 말풍선 없음.
    public class ObstacleWarning : MonoBehaviour
    {
        private const float TriggerAhead = 26f;
        private const float Duration = 0.9f;
        private static PlayerController _player;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private bool _fired;
        private float _t = -1f;
        private Renderer[] _targets;
        private Color[] _baseColors;
        private bool[] _hasOutline;
        private MaterialPropertyBlock _mpb;

        public static void Attach(GameObject root)
        {
            if (root != null && root.GetComponent<ObstacleWarning>() == null)
                root.AddComponent<ObstacleWarning>();
        }

        private void Start()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r == null || r.sharedMaterial == null) continue;
                string n = r.gameObject.name;
                if (n == "BlobShadow" || n == "HazardRing" || n == "PickupGlow" || n == "Outline" || n == "Painted_Back" || n.StartsWith("Decal_")) continue;
                if (r is ParticleSystemRenderer) continue;
                list.Add(r);
            }
            _targets = list.ToArray();
            _baseColors = new Color[_targets.Length];
            _hasOutline = new bool[_targets.Length];
            _mpb = new MaterialPropertyBlock();
            for (int i = 0; i < _targets.Length; i++)
            {
                var m = _targets[i].sharedMaterial;
                _baseColors[i] = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : (m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white);
                _hasOutline[i] = m.HasProperty(OutlineColorId);
                // 기존 프로퍼티 블록(계절 틴트 등)이 있으면 그 색을 기준으로
                _targets[i].GetPropertyBlock(_mpb);
                var pb = _mpb.GetColor(BaseColorId);
                if (pb != default) _baseColors[i] = pb;
            }
        }

        private void Update()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player == null || _targets == null) return;
            if (!_fired)
            {
                float ahead = DownhillPath.DistanceAlong(transform.position) - _player.PathDistance;
                if (ahead > TriggerAhead || ahead < 2f) return;
                _fired = true; _t = 0f;
            }
            if (_t < 0f) return;
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Duration);
            // 3번 반짝: |sin(3π k)| 로 흰빛 ↔ 빨강, 끝으로 갈수록 잦아든다
            float pulse = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * (1f - k * 0.6f);
            Color flash = Color.Lerp(new Color(1f, 0.30f, 0.22f, 1f), new Color(1.6f, 1.4f, 1.3f, 1f), pulse);
            Color outline = Color.Lerp(new Color(0.06f, 0.05f, 0.10f, 1f), new Color(1f, 0.2f, 0.12f, 1f), 1f - k);
            for (int i = 0; i < _targets.Length; i++)
            {
                var r = _targets[i]; if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                Color c = _t >= Duration ? _baseColors[i] : Color.Lerp(_baseColors[i], _baseColors[i] * flash, 0.85f * (1f - k * 0.3f));
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                if (_hasOutline[i]) _mpb.SetColor(OutlineColorId, _t >= Duration ? new Color(0.06f, 0.05f, 0.10f, 1f) : outline);
                r.SetPropertyBlock(_mpb);
            }
            if (_t >= Duration) { _t = -1f; enabled = false; }
        }
    }
}
