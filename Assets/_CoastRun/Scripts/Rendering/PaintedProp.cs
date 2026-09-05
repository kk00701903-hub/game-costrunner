using UnityEngine;

namespace CoastRun
{
    /// Firefly-painted prop as a yaw-only billboard (RGB on pure magenta, keyed by the
    /// ChromaUnlit shader). Resources/CoastRun/Obs_<Key>.png; when the painting is
    /// missing the caller keeps its procedural visual, so the game never breaks.
    public static class PaintedProp
    {
        public static Texture2D Load(string key) =>
            Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Obs_" + key);

        public static bool Available(string key) => Load(key) != null;

        /// Adds the sprite under `root`, `height` metres tall with its feet at y = 0
        /// (+ `groundLift`), and hides every other renderer under `root` if `replace`.
        public static Transform Attach(Transform root, string key, float height, bool replace = true,
            float groundLift = 0f, float zOffset = 0f)
        {
            var tex = Load(key);
            if (tex == null)
                return null;

            if (replace)
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    if (r.name != "BlobShadow")
                        r.enabled = false;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Painted_" + key;
            quad.transform.SetParent(root, false);
            CoastEditUtil.DestroyCollider(quad);
            float w = height * tex.width / (float)tex.height;
            quad.transform.localScale = new Vector3(w, height, 1f);
            quad.transform.localPosition = new Vector3(0f, height * 0.5f + groundLift, zOffset);

            var shader = Shader.Find("CoastRun/ChromaUnlit") ?? CoastMaterials.UnlitShader;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); else mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_KeyColor")) mat.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
            var mr = quad.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            quad.AddComponent<YawBillboard>();
            return quad.transform;
        }
    }

    /// Flat ground decal (puddle, leaf drift): lies on the road, no billboarding.
    public static class PaintedDecal
    {
        public static Transform Attach(Transform root, string key, float length, float lift = 0.02f)
        {
            var tex = PaintedProp.Load(key);
            if (tex == null)
                return null;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Decal_" + key;
            quad.transform.SetParent(root, false);
            CoastEditUtil.DestroyCollider(quad);
            float w = length * tex.width / (float)tex.height;
            quad.transform.localScale = new Vector3(w, length, 1f);
            quad.transform.localPosition = new Vector3(0f, lift, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var shader = Shader.Find("CoastRun/ChromaUnlit") ?? CoastMaterials.UnlitShader;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); else mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_KeyColor")) mat.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
            var mr = quad.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return quad.transform;
        }
    }

    /// Keeps a quad upright and turned toward the main camera around Y only, so a
    /// painted prop stands on the road instead of tipping toward a high camera.
    public class YawBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCam = cam.transform.position - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }
}
