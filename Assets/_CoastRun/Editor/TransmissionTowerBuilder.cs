#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.Editor
{
    /// Builds the lattice transmission tower — the game's title object and final
    /// destination — as real geometry instead of the Cylinder + Cube placeholder.
    ///
    /// A steel pylon is pure parametric structure (splayed legs, horizontal belts,
    /// X-bracing, cross-arms), so generating it beats both the primitive stand-in and
    /// an AI-generated mesh: clean quad-free triangle topology, predictable triangle
    /// count, and every proportion tweakable from the constants below.
    ///
    /// Menu: Coast Run/Build Transmission Tower
    public static class TransmissionTowerBuilder
    {
        private const string ResourcesRoot = "Assets/Resources/CoastRun";
        private const string MeshAsset = ResourcesRoot + "/TransmissionTower_Mesh.asset";
        private const string MatAsset = ResourcesRoot + "/TransmissionTower_Steel.mat";
        private const string PrefabAsset = ResourcesRoot + "/TransmissionTower.prefab";

        // ── Proportions (metres). Matches the placeholder's footprint so nothing
        //    downstream needs re-scaling: ~26 m tall, ~10 m widest cross-arm.
        private const float Height = 26f;
        private const float BaseHalf = 3.2f;   // half-width of the splayed footing
        private const float TopHalf = 1.0f;    // half-width at the apex
        private const int Belts = 7;           // horizontal bracing rings
        private const float Member = 0.16f;    // structural member half-thickness

        // Cross-arms: (height, half-length, half-thickness)
        private static readonly (float y, float half, float thick)[] Arms =
        {
            (18.0f, 5.0f, 0.18f),
            (21.2f, 4.2f, 0.16f),
            (24.0f, 3.0f, 0.14f),
        };

        [MenuItem("Coast Run/Build Transmission Tower")]
        public static void Build()
        {
            Directory.CreateDirectory(ResourcesRoot);

            Mesh mesh = Generate();
            SaveOrReplace(mesh, MeshAsset);

            Material mat = LoadOrCreateSteel();
            GameObject root = Assemble(mesh, mat);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabAsset);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Transmission tower rebuilt — {mesh.vertexCount} verts, " +
                      $"{mesh.triangles.Length / 3} tris → {PrefabAsset}");
        }

        // ────────────────────────────────────────────────────────────────────
        // Geometry
        // ────────────────────────────────────────────────────────────────────

        private static Mesh Generate()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            // Square cross-section tapering with height. t = 0 at the footing, 1 at the apex.
            float HalfAt(float t) => Mathf.Lerp(BaseHalf, TopHalf, EaseTaper(t));

            Vector3 Corner(int i, float t)
            {
                float h = HalfAt(t);
                float x = (i == 0 || i == 3) ? -h : h;
                float z = (i < 2) ? -h : h;
                return new Vector3(x, t * Height, z);
            }

            // Legs — four continuous splayed uprights.
            for (int i = 0; i < 4; i++)
                for (int s = 0; s < Belts; s++)
                {
                    float t0 = s / (float)Belts;
                    float t1 = (s + 1) / (float)Belts;
                    AddBeam(verts, tris, uvs, Corner(i, t0), Corner(i, t1), Member);
                }

            // Horizontal belts + X-bracing on all four faces.
            for (int s = 1; s <= Belts; s++)
            {
                float t0 = (s - 1) / (float)Belts;
                float t1 = s / (float)Belts;

                for (int i = 0; i < 4; i++)
                {
                    int j = (i + 1) % 4;

                    // Belt along the top edge of this bay.
                    AddBeam(verts, tris, uvs, Corner(i, t1), Corner(j, t1), Member * 0.75f);

                    // Diagonals. Alternating the lead diagonal per bay reads as real
                    // bracing rather than a repeated stamp.
                    bool flip = (s + i) % 2 == 0;
                    Vector3 a0 = Corner(i, t0), a1 = Corner(j, t1);
                    Vector3 b0 = Corner(j, t0), b1 = Corner(i, t1);
                    AddBeam(verts, tris, uvs, flip ? a0 : b0, flip ? a1 : b1, Member * 0.55f);
                    AddBeam(verts, tris, uvs, flip ? b0 : a0, flip ? b1 : a1, Member * 0.5f);
                }
            }

            // Cross-arms with a supporting strut back to the mast.
            foreach (var (y, half, thick) in Arms)
            {
                float t = y / Height;
                float mast = HalfAt(t);

                AddBeam(verts, tris, uvs,
                    new Vector3(-half, y, 0f), new Vector3(half, y, 0f), thick);

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 tip = new Vector3(sign * half, y, 0f);
                    Vector3 anchor = new Vector3(sign * mast, y - 2.0f, 0f);
                    AddBeam(verts, tris, uvs, tip, anchor, thick * 0.6f);

                    // Insulator stubs hanging off the arm.
                    for (int k = 1; k <= 2; k++)
                    {
                        float x = sign * half * (k / 2.6f + 0.35f);
                        AddBeam(verts, tris, uvs,
                            new Vector3(x, y, 0f), new Vector3(x, y - 0.9f, 0f), thick * 0.45f);
                    }
                }
            }

            // Apex mast carrying the aviation warning light.
            AddBeam(verts, tris, uvs,
                new Vector3(0f, Height, 0f), new Vector3(0f, Height + 1.6f, 0f), Member * 0.6f);

            var mesh = new Mesh { name = "TransmissionTower" };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }

        /// Legs taper fast near the ground and straighten out — a straight lerp reads
        /// like a traffic cone rather than a pylon.
        private static float EaseTaper(float t) => 1f - Mathf.Pow(1f - t, 1.7f);

        /// One structural member as a box-section prism from a to b.
        /// 8 verts / 12 tris — flat-shaded via RecalculateNormals on split geometry.
        private static void AddBeam(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
                                    Vector3 a, Vector3 b, float r)
        {
            Vector3 axis = b - a;
            float len = axis.magnitude;
            if (len < 0.0001f)
                return;

            axis /= len;
            Vector3 up = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            Vector3 side = Vector3.Normalize(Vector3.Cross(axis, up)) * r;
            Vector3 vert = Vector3.Normalize(Vector3.Cross(axis, side)) * r;

            int b0 = verts.Count;
            for (int end = 0; end < 2; end++)
            {
                Vector3 c = end == 0 ? a : b;
                verts.Add(c - side - vert);
                verts.Add(c + side - vert);
                verts.Add(c + side + vert);
                verts.Add(c - side + vert);

                // UVs run along the member so a steel texture stretches sensibly.
                float v = end == 0 ? 0f : len;
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(r * 2f, v));
                uvs.Add(new Vector2(r * 4f, v));
                uvs.Add(new Vector2(r * 6f, v));
            }

            int[] quads =
            {
                0, 1, 5, 4,
                1, 2, 6, 5,
                2, 3, 7, 6,
                3, 0, 4, 7,
                3, 2, 1, 0,
                4, 5, 6, 7,
            };

            for (int q = 0; q < quads.Length; q += 4)
            {
                int i0 = b0 + quads[q], i1 = b0 + quads[q + 1];
                int i2 = b0 + quads[q + 2], i3 = b0 + quads[q + 3];
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i0); tris.Add(i2); tris.Add(i3);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Assets
        // ────────────────────────────────────────────────────────────────────

        private static GameObject Assemble(Mesh mesh, Material mat)
        {
            var root = new GameObject("TransmissionTower");

            var body = new GameObject("Lattice");
            body.transform.SetParent(root.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = mat;

            // Aviation warning light. DynamicEnvironmentManager finds the tower by name
            // and drives the blink from t > 0.85; this is the anchor it looks for.
            var beacon = new GameObject("Beacon");
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.localPosition = new Vector3(0f, Height + 1.7f, 0f);

            var light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.15f, 0.12f);
            light.intensity = 3.5f;
            light.range = 14f;
            light.shadows = LightShadows.None;

            return root;
        }

        private static Material LoadOrCreateSteel()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MatAsset);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "TransmissionTower_Steel" };
            mat.color = new Color(0.42f, 0.45f, 0.48f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.6f);

            AssetDatabase.CreateAsset(mat, MatAsset);
            return mat;
        }

        private static void SaveOrReplace(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return;
            }

            // Keep the GUID so anything already pointing at the mesh survives a rebuild.
            existing.Clear();
            existing.SetVertices(new List<Vector3>(mesh.vertices));
            existing.SetTriangles(mesh.triangles, 0);
            existing.SetUVs(0, new List<Vector2>(mesh.uv));
            existing.RecalculateNormals();
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
        }
    }
}
#endif
