using UnityEngine;

namespace CoastRun
{
    /// Turquoise sea with animated waves, foam crests, and shore break.
    public class CoastSea : MonoBehaviour
    {
        [SerializeField] private float seaLevel = -0.35f;
        [SerializeField] private float scrollSpeed = 0.15f;
        [SerializeField] private float waveAmplitude = 0.12f;

        private Transform _follow;
        private Material _material;
        private MeshFilter _meshFilter;
        private Vector3[] _baseVertices;
        private float _phase;
        private Transform _foamLine;

        public Material SeaMaterial => _material;

        public void Build(Transform follow)
        {
            _follow = follow;

            var meshObj = new GameObject("SeaPlane");
            meshObj.transform.SetParent(transform, false);
            meshObj.transform.localPosition = new Vector3(0f, 0f, -20f);

            _meshFilter = meshObj.AddComponent<MeshFilter>();
            var renderer = meshObj.AddComponent<MeshRenderer>();
            _meshFilter.sharedMesh = CreateSeaMesh(90f, 160f, 36, 52);
            _baseVertices = _meshFilter.sharedMesh.vertices;

            Texture2D seaTex = ArtAssets.LoadTexture("Sea_Turquoise_Tile");
            _material = CoastMaterials.CreateToon(CoastPalette.Sea, () => CoastPalette.Sea, seaTex, 0.55f);
            if (seaTex != null)
            {
                _material.mainTextureScale = new Vector2(8f, 10f);
                if (_material.HasProperty("_BaseMap"))
                    _material.SetTextureScale("_BaseMap", new Vector2(8f, 10f));
            }

            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            BuildFoamLine(meshObj.transform);
        }

        private void BuildFoamLine(Transform parent)
        {
            var foam = new GameObject("FoamLine");
            foam.transform.SetParent(parent, false);
            foam.transform.localPosition = new Vector3(-38f, 0.08f, 0f);
            var mf = foam.AddComponent<MeshFilter>();
            var mr = foam.AddComponent<MeshRenderer>();
            mf.sharedMesh = CreateSeaMesh(4f, 120f, 2, 52);
            mr.sharedMaterial = CoastMaterials.CreateUnlit(() => CoastPalette.SeaFoam);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _foamLine = foam.transform;
        }

        private static Mesh CreateSeaMesh(float width, float length, int xSeg, int zSeg)
        {
            var mesh = new Mesh { name = "SeaMesh" };
            int vx = xSeg + 1;
            int vz = zSeg + 1;
            var verts = new Vector3[vx * vz];
            var uvs = new Vector2[vx * vz];
            var tris = new int[xSeg * zSeg * 6];

            float halfW = width * 0.5f;
            for (int z = 0; z < vz; z++)
            {
                for (int x = 0; x < vx; x++)
                {
                    int i = z * vx + x;
                    float fx = x / (float)xSeg;
                    float fz = z / (float)zSeg;
                    verts[i] = new Vector3(fx * width - halfW, 0f, fz * length);
                    uvs[i] = new Vector2(fx * 4f, fz * 6f);
                }
            }

            int t = 0;
            for (int z = 0; z < zSeg; z++)
            {
                for (int x = 0; x < xSeg; x++)
                {
                    int i = z * vx + x;
                    tris[t++] = i;
                    tris[t++] = i + vx;
                    tris[t++] = i + 1;
                    tris[t++] = i + 1;
                    tris[t++] = i + vx;
                    tris[t++] = i + vx + 1;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void Update()
        {
            _phase += Time.deltaTime * scrollSpeed;
            AnimateWaves();
            ScrollTexture();
        }

        private void ScrollTexture()
        {
            if (_material == null)
                return;

            Vector2 offset = new Vector2(Mathf.Sin(_phase * 0.4f) * 0.02f, _phase * 0.4f);
            if (_material.HasProperty("_BaseMap"))
                _material.SetTextureOffset("_BaseMap", offset);
            else
                _material.mainTextureOffset = offset;
        }

        private void AnimateWaves()
        {
            if (_meshFilter == null || _baseVertices == null)
                return;

            var mesh = _meshFilter.sharedMesh;
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = _baseVertices[i];
                float wave = Mathf.Sin(v.x * 0.22f + _phase * 5f) * waveAmplitude
                             + Mathf.Sin(v.z * 0.15f + _phase * 3.5f) * waveAmplitude * 0.65f
                             + Mathf.Sin((v.x + v.z) * 0.08f + _phase * 2f) * waveAmplitude * 0.35f;
                verts[i] = v + Vector3.up * wave;
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
        }

        private void LateUpdate()
        {
            if (_follow == null)
                return;

            Vector3 p = _follow.position;
            transform.position = new Vector3(p.x + 24f, p.y - 14f, p.z);
            transform.rotation = Quaternion.identity;
        }
    }
}
