using UnityEngine;

namespace CoastRun.Village
{
    /// 146차: 발밑 원형 가짜 그림자(Blob Shadow). 부모 회전·바운스와 무관하게 지면(VillageWorld.Height)에 붙고,
    /// 몸이 뜨면(콩 뛰기) 작아지며 옅어진다. 재질은 SoftDisc(부드러운 원판, 알파) 하나 — 드로우콜 1, 라이팅 계산 없음.
    /// 데칼 프로젝터(URP DBuffer)보다 가볍고, 경사면에서도 지면 높이만 따라가면 충분한 치비 스케일에 맞다.
    public class GroundBlob : MonoBehaviour
    {
        public Transform Follow;          // 발 위치 기준(루트)
        public Transform Lift;            // 이 트랜스폼의 localPosition.y 만큼 떠 있다고 본다(CharacterMotion 피벗)
        public float Radius = 0.45f, Alpha = 0.34f, Squash = 0.78f;
        Transform _q; Material _m; string _prop; static Material _shared;

        public static GroundBlob Attach(Transform root, float radius, float alpha = 0.34f, Transform lift = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(go.GetComponent<Collider>()); go.name = "GroundBlob";
            go.transform.SetParent(root, false);
            var b = go.AddComponent<GroundBlob>(); b.Follow = root; b.Lift = lift; b.Radius = radius; b.Alpha = alpha; b._q = go.transform;
            var mr = go.GetComponent<MeshRenderer>();
            if (_shared == null) _shared = MiniStage3D.SoftDisc(new Color(0.22f, 0.16f, 0.30f, 1f));   // 라벤더 계열 그늘(팔레트 ShadowTint 의 어두운 쪽)
            b._m = new Material(_shared); if (b._m.HasProperty("_CurveWeight")) b._m.SetFloat("_CurveWeight", 1f); mr.sharedMaterial = b._m;
            b._prop = b._m.HasProperty("_BaseColor") ? "_BaseColor" : (b._m.HasProperty("_Color") ? "_Color" : null);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            b.LateUpdate();
            return b;
        }

        void LateUpdate()
        {
            if (Follow == null || _q == null) return;
            var p = Follow.position;
            float g = VillageWorld.Height(p.x, p.z);
            float lift = Lift != null ? Mathf.Max(0f, Lift.localPosition.y - 0.02f) : 0f;
            float k = Mathf.Clamp01(1f - lift * 1.6f);                       // 30 cm 뜨면 절반
            _q.position = new Vector3(p.x, g + 0.025f, p.z);
            _q.rotation = Quaternion.Euler(90f, 0f, 0f);
            float s = Radius * 2f * Mathf.Lerp(0.6f, 1f, k);
            _q.localScale = new Vector3(s, s * Squash, 1f);
            if (_prop != null) _m.SetColor(_prop, new Color(0.22f, 0.16f, 0.30f, Alpha * Mathf.Lerp(0.45f, 1f, k)));
        }

        /// 건물·나무처럼 안 움직이는 것: 발자국 형태의 정적 접지 그늘(타원)
        public static void Static(Transform parent, Vector3 worldPos, float w, float d, float alpha = 0.26f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(go.GetComponent<Collider>()); go.name = "ContactShadow";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldPos.x, VillageWorld.Height(worldPos.x, worldPos.z) + 0.02f, worldPos.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ls = parent != null ? parent.lossyScale : Vector3.one;   // 부모 스케일 보정(월드 크기 w×d)
            go.transform.localScale = new Vector3(w / Mathf.Max(0.01f, ls.x), d / Mathf.Max(0.01f, ls.z), 1f);
            var mr = go.GetComponent<MeshRenderer>(); var sm = MiniStage3D.SoftDisc(new Color(0.22f, 0.16f, 0.30f, alpha)); if (sm.HasProperty("_CurveWeight")) sm.SetFloat("_CurveWeight", 1f); mr.sharedMaterial = sm;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }
    }
}
