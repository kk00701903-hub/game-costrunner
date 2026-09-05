using UnityEngine;

namespace CoastRun
{
    /// Parallax cloud billboard layers — far layers slower + hazier.
    public class CloudLayerScroller : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public Transform root;
            public float scrollSpeed = 1.2f;
            public float opacity = 0.85f;
            public float height = 42f;
            public float depth = 90f;
        }

        [SerializeField] private Transform follow;
        [SerializeField] private Layer[] layers;

        private Vector3[] _origins;

        public void Build(Transform followTarget)
        {
            follow = followTarget;
            ClearLayers();

            layers = new[]
            {
                MakeLayer("CloudFar", height: 55f, depth: 150f, speed: 0.35f, opacity: 0.45f, scale: 38f, count: 5),
                MakeLayer("CloudMid", height: 42f, depth: 95f, speed: 0.9f, opacity: 0.7f, scale: 26f, count: 6),
                MakeLayer("CloudNear", height: 32f, depth: 55f, speed: 1.8f, opacity: 0.92f, scale: 16f, count: 5)
            };

            _origins = new Vector3[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                _origins[i] = layers[i].root != null ? layers[i].root.localPosition : Vector3.zero;
        }

        private void ClearLayers()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Cloud"))
                    CoastEditUtil.DestroyObject(child.gameObject);
            }
        }

        private Layer MakeLayer(string name, float height, float depth, float speed, float opacity,
            float scale, int count)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, height, depth);

            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                float x = ((i / (float)Mathf.Max(1, count - 1)) - 0.5f) * 160f;
                x += ((float)rng.NextDouble() - 0.5f) * 18f;
                float y = ((float)rng.NextDouble() - 0.5f) * 10f;
                float z = ((float)rng.NextDouble() - 0.5f) * 40f;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Billboard";
                quad.transform.SetParent(root, false);
                quad.transform.localPosition = new Vector3(x, y, z);
                float s = scale * (0.7f + (float)rng.NextDouble() * 0.6f);
                CoastEditUtil.DestroyCollider(quad);

                // Painted cumulus (Firefly, alpha) when the textures exist; the flat
                // tinted slabs stay as the fallback.
                Texture2D cloudTex = PaintedCloud(rng);
                Material mat;
                if (cloudTex != null)
                {
                    float aspect = cloudTex.width / (float)cloudTex.height;
                    quad.transform.localScale = new Vector3(s * 1.6f, s * 1.6f / aspect, 1f);
                    if (rng.NextDouble() < 0.5)
                        quad.transform.localScale = Vector3.Scale(quad.transform.localScale, new Vector3(-1f, 1f, 1f));
                    Color tint = Color.white;
                    tint.a = opacity;
                    mat = CoastMaterials.CreateTexturedTransparent(cloudTex, tint);
                    CoastMaterials.SetNoFog(mat, 0f);
                }
                else
                {
                    quad.transform.localScale = new Vector3(s * 1.8f, s * 0.55f, 1f);
                    Color c = Color.Lerp(CoastPalette.CloudLight, CoastPalette.CloudShadow,
                        (float)rng.NextDouble() * 0.35f);
                    c.a = opacity;
                    mat = CoastMaterials.CreateTransparent(c, () =>
                    {
                        Color live = Color.Lerp(CoastPalette.CloudLight, CoastPalette.CloudShadow, 0.2f);
                        live.a = opacity;
                        return live;
                    });
                }
                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = CoastMaterials.SetFlat(mat);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            return new Layer
            {
                root = root,
                scrollSpeed = speed,
                opacity = opacity,
                height = height,
                depth = depth
            };
        }

        private static readonly string[] CloudNames = { "Cloud_Cumulus_A", "Cloud_Cumulus_B", "Cloud_Cumulus_C" };
        private static Texture2D[] _cloudTex;

        private static Texture2D PaintedCloud(System.Random rng)
        {
            if (_cloudTex == null)
            {
                var list = new System.Collections.Generic.List<Texture2D>();
                foreach (var n in CloudNames)
                {
                    var t = ArtAssets.LoadTexture(n);
                    if (t != null) list.Add(t);
                }
                _cloudTex = list.ToArray();
            }
            return _cloudTex.Length == 0 ? null : _cloudTex[rng.Next(_cloudTex.Length)];
        }

        private void LateUpdate()
        {
            if (layers == null || layers.Length == 0)
                return;

            Vector3 followPos = follow != null ? follow.position : Vector3.zero;
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer.root == null)
                    continue;

                float scroll = Time.time * layer.scrollSpeed * 2.5f;
                Vector3 origin = _origins != null && i < _origins.Length ? _origins[i] : Vector3.zero;
                // Parallax vs player + constant drift
                float parallax = followPos.x * (0.02f + i * 0.01f);
                layer.root.localPosition = new Vector3(
                    origin.x + Mathf.Repeat(scroll + parallax, 180f) - 90f,
                    layer.height,
                    layer.depth);

                // Face camera roughly (billboard group)
                if (Camera.main != null)
                {
                    Vector3 toCam = Camera.main.transform.position - layer.root.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f)
                        layer.root.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                }
            }
        }
    }
}
