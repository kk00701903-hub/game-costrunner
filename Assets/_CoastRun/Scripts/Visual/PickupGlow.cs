using UnityEngine;

namespace CoastRun
{
    /// 12차 시인성 규칙 ① — 먹는 것은 빛나고 떠서 흔들린다.
    /// 아이템 뒤에 가산 혼합 광원 디스크(빌보드)를 하나 붙이고, 부드럽게 숨 쉬듯 커졌다 작아진다.
    /// 장애물은 절대 이걸 쓰지 않는다(장애물은 HazardRing).
    [DisallowMultipleComponent]
    public class PickupGlow : MonoBehaviour
    {
        private Transform _quad;
        private Material _mat;
        private Color _color;
        private float _size;
        private float _phase;

        public static PickupGlow Attach(Transform host, Color color, float size = 1.1f, float alpha = 0.55f)
        {
            if (host == null)
                return null;
            var g = host.GetComponent<PickupGlow>() ?? host.gameObject.AddComponent<PickupGlow>();
            g._color = new Color(color.r, color.g, color.b, alpha);
            g._size = size;
            g._phase = Random.value * 6.28f;
            if (g._quad == null)
                g.Build();
            else
                g._mat.SetColor("_BaseColor", g._color);
            return g;
        }

        private void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "PickupGlow";
            go.transform.SetParent(transform, false);
            CoastEditUtil.DestroyCollider(go);
            _quad = go.transform;
            _quad.localPosition = new Vector3(0f, 0.42f, 0f);
            _quad.localScale = Vector3.one * _size;
            _mat = CoastMaterials.CreateTexturedTransparentCurved(BlobShadow.SoftDisc(), _color, additive: true);
            _mat.renderQueue = 2990;   // 그림자 뒤, 아이템 본체 앞이 아니라 뒤에서 은은하게
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<YawBillboard>();
        }

        private void LateUpdate()
        {
            if (_quad == null)
                return;
            float k = 0.9f + 0.12f * Mathf.Sin(Time.time * 4.2f + _phase);
            _quad.localScale = Vector3.one * (_size * k);
        }

        /// 먹었을 때 본체와 같이 사라지게.
        public void Hide()
        {
            if (_quad != null)
                _quad.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (_quad != null)
                _quad.gameObject.SetActive(true);
        }
    }
}
