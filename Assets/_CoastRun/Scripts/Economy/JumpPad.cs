using UnityEngine;

namespace CoastRun
{
    /// 14차-3: 점프 패드. 밟으면(트리거) 입력 없이도 큰 점프 — 보통 점프의 1.5배 초속으로
    /// 체공 0.96초·높이 3 m, 4 m 간격으로 깔린 낮은 장애물 서너 줄을 한 번에 넘는다.
    /// 그림은 Resources/CoastRun/Obs_JumpPad.png(마젠타 키 빌보드); 없으면 청록 원판.
    public class JumpPad : MonoBehaviour
    {
        public const float LaunchMul = 1.7f;   // 14차-10: 하늘 코인 아치까지 닿는 높이(약 4 m)
        private bool _used;
        private Transform _vis;
        private float _pulse;

        public static JumpPad Spawn(Transform parent, Vector3 worldPos)
        {
            var go = new GameObject("JumpPad");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = DownhillPath.Rotation;
            var pad = go.AddComponent<JumpPad>();

            if (PaintedProp.Available("JumpPad"))
            {
                // 살짝 눕힌 빌보드: 위에서 내려다보는 카메라에서 바닥에 놓인 판처럼 읽힌다.
                pad._vis = PaintedProp.Attach(go.transform, "JumpPad", 1.15f, replace: false, groundLift: 0.02f);
                if (pad._vis != null)
                {
                    Object.Destroy(pad._vis.GetComponent<YawBillboard>());
                    // 쿼드 피벗은 가운데 — 눕힌 만큼 아래쪽이 땅에 파묻히지 않게 반높이·cos만큼 올린다.
                    pad._vis.localRotation = Quaternion.Euler(62f, 0f, 0f);
                    float half = pad._vis.localScale.y * 0.5f;
                    pad._vis.localPosition = new Vector3(0f, half * Mathf.Cos(62f * Mathf.Deg2Rad) + 0.04f, 0f);
                }
            }
            else
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Pad";
                disc.transform.SetParent(go.transform, false);
                disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                disc.transform.localScale = new Vector3(1.3f, 0.05f, 1.3f);
                Object.Destroy(disc.GetComponent<Collider>());
                disc.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(() => CoastPalette.SeaTeal, 0.3f);
                pad._vis = disc.transform;
            }

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.4f, 0f);
            col.size = new Vector3(1.6f, 0.8f, 1.4f);
            PickupGlow.Attach(go.transform, new Color(0.5f, 1f, 0.95f), 1.3f, 0.2f);
            return pad;
        }

        private Vector3 _visBase;

        private void Update()
        {
            if (_vis == null) return;
            if (_visBase == Vector3.zero) _visBase = _vis.localScale;
            _pulse += Time.deltaTime * 4f;
            float s = _used ? 1f : 1f + Mathf.Sin(_pulse) * 0.04f;
            _vis.localScale = new Vector3(_visBase.x * s, _visBase.y * (_used ? 0.7f : s), _visBase.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            _used = true;
            player.LaunchFromPad(LaunchMul);
            GetComponent<PickupGlow>()?.Hide();
            JuiceDirector.Instance?.OnJumpPad(transform.position);
        }
    }
}
