using UnityEngine;

namespace CoastRun
{
    /// 17차: 빨래줄 — 길 양쪽 기둥 사이에 걸린 줄에 빨래가 펄럭인다. 점프대로 떠서 줄에 닿으면
    /// 잡고 멀리 활공(PlayerController.GrabLine): 지면 3.4 m 위를 1.55배속으로 3.4초, 그 길 위에 코인 줄.
    /// 그림(Resources/CoastRun/Obs_Laundry.png)이 있으면 빨래는 빌보드, 없으면 색 천 조각.
    public class ClothesLine : MonoBehaviour
    {
        public const float LineHeight = 3.4f;
        public const float GlideSeconds = 3.4f;
        public const float GlideSpeedMul = 1.55f;
        private static Material _rope, _post;
        private bool _used;
        private Transform[] _laundry;
        private float _wave;

        public static ClothesLine Spawn(Transform parent, float pathZ)
        {
            _rope ??= CoastMaterials.CreateLit(new Color(0.93f, 0.90f, 0.82f), 0.1f);
            _post ??= CoastMaterials.CreateLit(new Color(0.42f, 0.30f, 0.20f), 0.1f);
            var go = new GameObject("ClothesLine");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(RoadPlacement.OnRoad(pathZ, 0f), DownhillPath.Rotation);
            var cl = go.AddComponent<ClothesLine>();
            float half = PromenadeSegmentBuilder.RoadHalfWidth + 0.9f;
            Box(go.transform, "Post", new Vector3(-half, LineHeight * 0.5f + 0.35f, 0f), new Vector3(0.14f, LineHeight + 0.7f, 0.14f), _post);
            Box(go.transform, "Post", new Vector3(half, LineHeight * 0.5f + 0.35f, 0f), new Vector3(0.14f, LineHeight + 0.7f, 0.14f), _post);
            float y0 = LineHeight + 0.45f, ym = LineHeight + 0.15f;
            Vector3 a = new Vector3(-half, y0, 0f), b = new Vector3(0f, ym, 0f), c = new Vector3(half, y0, 0f);
            Rope(go.transform, a, b); Rope(go.transform, b, c);
            int n = 5;
            cl._laundry = new Transform[n];
            Color[] cols = { new Color(1f, 0.85f, 0.3f), new Color(0.55f, 0.8f, 1f), new Color(1f, 0.6f, 0.7f), new Color(0.75f, 0.95f, 0.75f), new Color(1f, 1f, 1f) };
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                float x = Mathf.Lerp(-half + 0.6f, half - 0.6f, t);
                float y = Mathf.Lerp(y0, ym, 1f - Mathf.Abs(t * 2f - 1f));
                Transform piece;
                if (PaintedProp.Available("Laundry"))
                {
                    var pv = new GameObject("Laundry").transform;
                    pv.SetParent(go.transform, false);
                    pv.localPosition = new Vector3(x, y - 1.35f, 0f);
                    PaintedProp.Attach(pv, "Laundry", 1.35f, replace: false, outline: true);
                    piece = pv;
                }
                else
                {
                    float w = 0.7f + (i % 2) * 0.25f, h = 0.9f + (i % 3) * 0.2f;
                    piece = Box(go.transform, "Cloth", new Vector3(x, y - h * 0.5f, 0f), new Vector3(w, h, 0.04f), CoastMaterials.CreateLit(cols[i % cols.Length], 0.05f)).transform;
                    Box(go.transform, "Peg", new Vector3(x - w * 0.35f, y + 0.02f, 0f), new Vector3(0.06f, 0.14f, 0.06f), _post);
                    Box(go.transform, "Peg", new Vector3(x + w * 0.35f, y + 0.02f, 0f), new Vector3(0.06f, 0.14f, 0.06f), _post);
                }
                cl._laundry[i] = piece;
            }
            // 잡는 판정: 줄 높이 ±0.9 m, 앞뒤 2.6 m(포물선의 오르막·내리막 둘 다 걸린다)
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, LineHeight, 0f);
            col.size = new Vector3(half * 2f, 1.8f, 2.6f);
            PickupGlow.Attach(go.transform, new Color(1f, 0.95f, 0.6f), 0.9f, 0.15f);
            return cl;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name; b.transform.SetParent(parent, false);
            b.transform.localPosition = pos; b.transform.localScale = size;
            CoastEditUtil.DestroyCollider(b);
            b.GetComponent<Renderer>().sharedMaterial = m;
            b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return b;
        }

        private static void Rope(Transform parent, Vector3 a, Vector3 b)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Rope"; r.transform.SetParent(parent, false);
            r.transform.localPosition = (a + b) * 0.5f;
            r.transform.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
            r.transform.localScale = new Vector3((b - a).magnitude, 0.045f, 0.045f);
            CoastEditUtil.DestroyCollider(r);
            r.GetComponent<Renderer>().sharedMaterial = _rope;
        }

        private void Update()
        {
            if (_laundry == null) return;
            _wave += Time.deltaTime * 3.2f;
            for (int i = 0; i < _laundry.Length; i++)
            {
                var t = _laundry[i]; if (t == null || t.parent != transform) continue;
                t.localRotation = Quaternion.Euler(Mathf.Sin(_wave + i * 1.3f) * 9f, 0f, Mathf.Sin(_wave * 0.7f + i) * 3f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsGrounded || player.IsGliding) return;
            _used = true;
            player.GrabLine(GlideSeconds, LineHeight, GlideSpeedMul);
            GetComponent<PickupGlow>()?.Hide();
            float z = DownhillPath.DistanceAlong(transform.position);
            float speed = Mathf.Max(player.Speed, 8f) * GlideSpeedMul;
            CoinSpawner.Instance?.SpawnGlideLine(z + 3f, player.Lane, LineHeight + 0.15f, speed * GlideSeconds - 4f);
            GlideRope.Show(player, z, speed * GlideSeconds + 6f, LineHeight + 0.55f);
            JuiceDirector.Instance?.OnLineGrab(transform.position + Vector3.up * LineHeight);
            // 빨래 한 장이 주인공 손에 딸려 간다
            if (_laundry != null && _laundry.Length > 0 && _laundry[_laundry.Length / 2] != null)
            {
                var piece = _laundry[_laundry.Length / 2];
                piece.SetParent(player.transform, false);
                piece.localPosition = new Vector3(0f, 2.05f, 0.05f);   // 머리 위(카메라 가리지 않게), 반 크기
                piece.localRotation = Quaternion.Euler(20f, 0f, 0f);
                piece.localScale *= 0.5f;
                GlideRope.ReleaseWith(piece.gameObject);
            }
        }
    }

    /// 활공 중 머리 위에서 앞으로 뻗은 줄(집라인처럼 읽힌다). 끝나면 사라진다.
    public class GlideRope : MonoBehaviour
    {
        private static GlideRope _current;
        private LineRenderer _lr; private PlayerController _player; private float _endZ, _height; private GameObject _held;

        public static void Show(PlayerController player, float fromZ, float length, float height)
        {
            if (_current != null) Destroy(_current.gameObject);
            var go = new GameObject("GlideRope");
            var gr = go.AddComponent<GlideRope>();
            gr._player = player; gr._endZ = fromZ + length; gr._height = height;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.widthMultiplier = 0.05f;
            lr.material = CoastMaterials.CreateLit(new Color(0.93f, 0.90f, 0.82f), 0.1f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            int n = Mathf.Max(2, Mathf.CeilToInt(length / 4f) + 1);
            lr.positionCount = n;
            gr._lr = lr;
            _current = gr;
            gr.Update();
            player.OnGlideEnd += gr.End;
        }

        public static void ReleaseWith(GameObject held) { if (_current != null) _current._held = held; }

        private static float Lateral(PlayerController p)
        {
            Vector3 local = Quaternion.Inverse(DownhillPath.Rotation) * (p.transform.position - DownhillPath.Point(DownhillPath.DistanceAlong(p.transform.position)));
            return local.x;
        }

        private void Update()
        {
            if (_player == null || _lr == null) return;
            float lat = Lateral(_player);
            float z0 = DownhillPath.DistanceAlong(_player.transform.position);
            int n = _lr.positionCount;
            for (int i = 0; i < n; i++)
                _lr.SetPosition(i, RoadPlacement.OnRoad(Mathf.Lerp(z0 - 1f, _endZ, (float)i / (n - 1)), lat, _height));
        }

        private void End()
        {
            if (_player != null) _player.OnGlideEnd -= End;
            if (_held != null) Destroy(_held);
            Destroy(gameObject);
        }
    }
}
