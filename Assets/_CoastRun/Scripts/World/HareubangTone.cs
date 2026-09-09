using UnityEngine;

namespace CoastRun
{
    /// 38차: 돌하르방 밤 톤 — 해가 지면(LightingT↑) 조명이 죽어 새카매지던 것을, 기본색을 최대 +55% 끌어올려 '살짝만 어둡게' 유지한다.
    public class HareubangTone : MonoBehaviour
    {
        private Renderer[] _r;
        private MaterialPropertyBlock _mpb;
        private static readonly Color Base = new Color(0.46f, 0.44f, 0.46f);
        private float _last = -1f;
        private DynamicEnvironmentManager _env;

        private void Start() { _r = GetComponentsInChildren<Renderer>(true); _mpb = new MaterialPropertyBlock(); }

        private void LateUpdate()
        {
            if (_env == null && Time.frameCount % 30 == 0) _env = FindFirstObjectByType<DynamicEnvironmentManager>();
            float t = _env != null ? _env.LightingT : 0f;
            float lift = 1f + 0.55f * Mathf.SmoothStep(0.5f, 1f, t);
            if (Mathf.Abs(lift - _last) < 0.005f) return;
            _last = lift;
            _mpb.SetColor("_BaseColor", new Color(Base.r * lift, Base.g * lift, Base.b * lift, 1f));
            for (int i = 0; i < _r.Length; i++) if (_r[i] != null) _r[i].SetPropertyBlock(_mpb);
        }
    }
}
