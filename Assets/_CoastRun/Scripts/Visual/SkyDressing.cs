using UnityEngine;

namespace CoastRun
{
    /// 14차-2: 하늘의 생기 — 해 플레어(가산 원판, 카메라 우상단 고정)와 멀리 나는 갈매기 V자 몇 마리.
    /// 목표 이미지의 '햇빛 번짐 + 새' 두 요소. 카메라 자식으로 붙어 세계가 휘어도 화면에 남는다.
    public class SkyDressing : MonoBehaviour
    {
        private Transform _cam;
        private Transform _flare;
        private readonly Transform[] _birds = new Transform[5];
        private readonly Transform[][] _wings = new Transform[5][];
        private readonly float[] _phase = new float[5];
        private readonly Vector3[] _base = new Vector3[5];
        private static Material _flareMat, _birdMat;

        public static SkyDressing Attach(Camera cam)
        {
            if (cam == null) return null;
            var existing = cam.GetComponent<SkyDressing>();
            if (existing != null) return existing;
            var d = cam.gameObject.AddComponent<SkyDressing>();
            d._cam = cam.transform;
            d.Build();
            return d;
        }

        private void Build()
        {
            // 가산 원판: 곡면 셰이더의 additive 경로(블렌드 프로퍼티를 직접 읽는다). 카메라 자식이므로 휨은 0.
            _flareMat ??= CoastMaterials.SetFlat(CoastMaterials.CreateTexturedTransparentCurved(BlobShadow.SoftDisc(), new Color(1f, 0.92f, 0.66f, 0.42f), additive: true));
            _flareMat.renderQueue = 3100;
            var f = GameObject.CreatePrimitive(PrimitiveType.Quad);
            f.name = "SunFlare";
            f.transform.SetParent(_cam, false);
            CoastEditUtil.DestroyCollider(f);
            f.transform.localPosition = new Vector3(4.2f, 6.2f, 30f);
            f.transform.localScale = Vector3.one * 14f;
            var fr = f.GetComponent<MeshRenderer>();
            fr.sharedMaterial = _flareMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fr.receiveShadows = false;
            _flare = f.transform;

            _birdMat ??= CoastMaterials.CreateUnlit(() => new Color(0.18f, 0.20f, 0.26f, 1f));
            for (int i = 0; i < _birds.Length; i++)
            {
                var b = new GameObject("Bird" + i);
                b.transform.SetParent(_cam, false);
                _base[i] = new Vector3(-9f + i * 3.6f + Random.Range(-1f, 1f), 7.5f + Random.Range(-1.5f, 2.5f), 55f + Random.Range(-8f, 12f));
                b.transform.localPosition = _base[i];
                _phase[i] = Random.value * 6.28f;
                _wings[i] = new Transform[2];
                for (int w = 0; w < 2; w++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = w == 0 ? "WingL" : "WingR";
                    q.transform.SetParent(b.transform, false);
                    CoastEditUtil.DestroyCollider(q);
                    q.transform.localPosition = new Vector3(w == 0 ? -0.35f : 0.35f, 0f, 0f);
                    q.transform.localScale = new Vector3(0.7f, 0.12f, 1f);
                    var r = q.GetComponent<MeshRenderer>();
                    r.sharedMaterial = _birdMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    _wings[i][w] = q.transform;
                }
                _birds[i] = b.transform;
            }
        }

        private void LateUpdate()
        {
            float t = Time.time;
            if (_flare != null)
            {
                // 살짝 숨 쉬는 플레어.
                _flare.localScale = Vector3.one * (14f + Mathf.Sin(t * 0.7f) * 0.7f);
            }
            for (int i = 0; i < _birds.Length; i++)
            {
                var b = _birds[i]; if (b == null) continue;
                float p = t * 0.55f + _phase[i];
                // 화면을 천천히 가로지르다 반대편에서 다시 나온다.
                float x = Mathf.Repeat(_base[i].x + t * 0.9f + 12f, 26f) - 12f;
                b.localPosition = new Vector3(x, _base[i].y + Mathf.Sin(p) * 0.5f, _base[i].z);
                float flap = Mathf.Sin(t * 7f + _phase[i]) * 32f;
                _wings[i][0].localRotation = Quaternion.Euler(0f, 0f, 20f + flap);
                _wings[i][1].localRotation = Quaternion.Euler(0f, 0f, -20f - flap);
            }
        }
    }
}
