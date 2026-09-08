using UnityEngine;

namespace CoastRun
{
    /// 22차-5: 스테이지 끝의 골인 리본 — 기둥 둘 사이에 걸린 빨강·흰 줄무늬 테이프. 주인공이 지나면 가운데가 끊겨 양쪽으로 펄럭이며 사라진다.
    public class FinishRibbon : MonoBehaviour
    {
        private static FinishRibbon _current;
        private Transform _left, _right;
        private bool _broken;
        private float _t;

        public static FinishRibbon Spawn(float pathZ)
        {
            if (_current != null) Destroy(_current.gameObject);
            var go = new GameObject("FinishRibbon");
            go.transform.SetPositionAndRotation(RoadPlacement.OnRoad(pathZ, 0f), DownhillPath.Rotation);
            var fr = go.AddComponent<FinishRibbon>();
            float half = PromenadeSegmentBuilder.RoadHalfWidth + 0.6f;
            var post = CoastMaterials.CreateLit(new Color(0.95f, 0.95f, 0.98f), 0.2f);
            Box(go.transform, "Post", new Vector3(-half, 0.8f, 0f), new Vector3(0.16f, 1.6f, 0.16f), post);
            Box(go.transform, "Post", new Vector3(half, 0.8f, 0f), new Vector3(0.16f, 1.6f, 0.16f), post);
            var ball = CoastMaterials.CreateLit(new Color(1f, 0.3f, 0.35f), 0.4f);
            var b1 = GameObject.CreatePrimitive(PrimitiveType.Sphere); b1.name = "Cap"; b1.transform.SetParent(go.transform, false); b1.transform.localPosition = new Vector3(-half, 1.68f, 0f); b1.transform.localScale = Vector3.one * 0.3f; CoastEditUtil.DestroyCollider(b1); b1.GetComponent<Renderer>().sharedMaterial = ball;
            var b2 = GameObject.CreatePrimitive(PrimitiveType.Sphere); b2.name = "Cap"; b2.transform.SetParent(go.transform, false); b2.transform.localPosition = new Vector3(half, 1.68f, 0f); b2.transform.localScale = Vector3.one * 0.3f; CoastEditUtil.DestroyCollider(b2); b2.GetComponent<Renderer>().sharedMaterial = ball;
            // 테이프: 왼쪽 반·오른쪽 반(끊기면 각자 기둥 쪽으로 젖혀진다). 피벗은 기둥 쪽 끝.
            var tape = CoastMaterials.CreateUnlit(Color.white);
            tape.mainTexture = Stripes();
            if (tape.HasProperty("_BaseMap")) tape.SetTexture("_BaseMap", Stripes());
            fr._left = Half(go.transform, -half, half, tape);
            fr._right = Half(go.transform, half, half, tape);
            _current = fr;
            return fr;
        }

        private static Transform Half(Transform parent, float postX, float half, Material m)
        {
            var pivot = new GameObject("Half").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(postX, 1.15f, 0f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Tape"; q.transform.SetParent(pivot, false);
            CoastEditUtil.DestroyCollider(q);
            float dir = postX < 0 ? 1f : -1f;
            q.transform.localPosition = new Vector3(dir * half * 0.5f, 0f, 0f);
            q.transform.localScale = new Vector3(half, 0.34f, 1f);
            q.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 주인공(뒤에서 오는 쪽)을 향해
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 뒷면도 보이게 한 장 더
            var q2 = Instantiate(q, pivot); q2.transform.localRotation = Quaternion.identity;
            return pivot;
        }

        private static Texture2D _stripes;
        private static Texture2D Stripes()
        {
            if (_stripes != null) return _stripes;
            const int w = 256, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool red = ((x + y) / 16) % 2 == 0;
                    px[y * w + x] = red ? new Color(0.95f, 0.22f, 0.30f) : new Color(1f, 1f, 1f);
                }
            tex.SetPixels(px); tex.Apply();
            _stripes = tex; return tex;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name; b.transform.SetParent(parent, false);
            b.transform.localPosition = pos; b.transform.localScale = size;
            CoastEditUtil.DestroyCollider(b);
            b.GetComponent<Renderer>().sharedMaterial = m;
            return b;
        }

        /// 주인공이 지나감 — 테이프가 끊긴다.
        public void Break()
        {
            if (_broken) return;
            _broken = true; _t = 0f;
            JuiceDirector.Instance?.OnLineGrab(transform.position + Vector3.up * 1.2f);
        }

        private void Update()
        {
            if (!_broken) return;
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / 1.2f);
            float swing = Mathf.Sin(u * Mathf.PI * 1.5f) * (1f - u) * 40f;
            if (_left != null) _left.localRotation = Quaternion.Euler(0f, -70f * u, -35f * u + swing);
            if (_right != null) _right.localRotation = Quaternion.Euler(0f, 70f * u, 35f * u - swing);
        }
    }
}
