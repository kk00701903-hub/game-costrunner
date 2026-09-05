using UnityEngine;

namespace CoastRun
{
    /// Soft ground disc — does most of the "planted on the road" read for primitives.
    [DisallowMultipleComponent]
    public class BlobShadow : MonoBehaviour
    {
        [SerializeField] private Transform follow;
        [SerializeField] private float baseScale = 0.95f;
        [SerializeField] private float maxLift = 2.4f;
        [SerializeField] private float groundedAlpha = 0.42f;
        [SerializeField] private float airborneAlpha = 0.08f;

        private Transform _quad;
        private Material _mat;
        private static Material _sharedMat;

        public static BlobShadow Attach(Transform host, float scale = 0.95f)
        {
            if (host == null)
                return null;

            var existing = host.GetComponent<BlobShadow>();
            if (existing != null)
            {
                existing.baseScale = scale;
                existing.follow = host;
                return existing;
            }

            var blob = host.gameObject.AddComponent<BlobShadow>();
            blob.follow = host;
            blob.baseScale = scale;
            // AddComponent already ran Awake → Build; only build if that didn't happen
            // (inactive host), otherwise two discs stack under the player.
            if (blob._quad == null)
                blob.Build();
            return blob;
        }

        private void Awake()
        {
            if (follow == null)
                follow = transform;
            if (_quad == null)
                Build();
        }

        private void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BlobShadow";
            go.transform.SetParent(transform, false);
            DestroyCollider(go);

            _quad = go.transform;
            _quad.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _quad.localPosition = new Vector3(0f, 0.02f, 0f);
            _quad.localScale = Vector3.one * baseScale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (_sharedMat == null)
            {
                // A plain-colour quad drew a hard square under the skater; a radial
                // alpha falloff makes it the soft disc it was meant to be.
                _sharedMat = CoastMaterials.CreateTexturedTransparent(DiscTexture(), CoastPalette.BlobShadow);
                _sharedMat.renderQueue = 2950;
            }

            _mat = new Material(_sharedMat);
            mr.sharedMaterial = _mat;
        }

        private static Texture2D _disc;

        /// 128² white disc with a smooth alpha falloff (opaque core ≈ 55 %, then eased to 0).
        private static Texture2D DiscTexture()
        {
            if (_disc != null)
                return _disc;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, false)
            {
                name = "BlobShadowDisc",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0.55f, 1f, r);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _disc = tex;
            return tex;
        }

        private void LateUpdate()
        {
            if (follow == null || _quad == null)
                return;

            float groundY;
            float lift;
            var player = follow.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                groundY = DownhillPath.Point(player.PathDistance, player.LateralOffset).y;
                lift = player.GroundClearance;
            }
            else
            {
                groundY = SampleGroundY(follow.position);
                lift = Mathf.Max(0f, follow.position.y - groundY);
            }

            float t = Mathf.Clamp01(lift / Mathf.Max(0.01f, maxLift));
            Vector3 world = new Vector3(follow.position.x, groundY + 0.025f, follow.position.z);
            _quad.SetPositionAndRotation(world, Quaternion.Euler(90f, follow.eulerAngles.y, 0f));

            float s = Mathf.Lerp(baseScale, baseScale * 0.42f, t);
            // Slightly longer along the direction of travel: a board's footprint.
            _quad.localScale = new Vector3(s * 0.85f, s * 1.35f, 1f);

            float a = Mathf.Lerp(groundedAlpha, airborneAlpha, t);
            if (_mat != null)
            {
                Color c = CoastPalette.BlobShadow;
                c.a = a;
                if (_mat.HasProperty("_BaseColor"))
                    _mat.SetColor("_BaseColor", c);
                else
                    _mat.color = c;
            }
        }

        private static float SampleGroundY(Vector3 from)
        {
            Vector3 origin = from + Vector3.up * 3f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MinValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider.isTrigger)
                    continue;
                if (hits[i].point.y > best)
                {
                    best = hits[i].point.y;
                    found = true;
                }
            }

            if (found)
                return best;

            float z = DownhillPath.DistanceAlong(from);
            return DownhillPath.Point(z).y;
        }

        private static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }
        }
    }
}
