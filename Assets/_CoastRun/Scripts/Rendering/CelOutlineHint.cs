using UnityEngine;

namespace CoastRun
{
    /// Ink outline for character meshes only — never add to environment props.
    public class CelOutlineHint : MonoBehaviour
    {
        [SerializeField] private float outlineScale = 1.07f;
        [SerializeField] private Color outlineColor = new Color(0.10f, 0.14f, 0.22f, 1f);

        private void Start()
        {
            var filter = GetComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (filter == null || renderer == null || filter.sharedMesh == null)
                return;
            if (transform.Find("Outline") != null)
                return;

            var outline = new GameObject("Outline");
            outline.transform.SetParent(transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one * outlineScale;

            var mf = outline.AddComponent<MeshFilter>();
            mf.sharedMesh = filter.sharedMesh;
            var mr = outline.AddComponent<MeshRenderer>();
            var ink = CoastMaterials.CreateUnlit(
                () => Color.Lerp(CoastPalette.ShadowCool, Color.black, 0.55f));
            // Inverted-hull outline: only the shell's back faces may show, otherwise the
            // enlarged copy simply paints over the part (that was the "black backpack").
            if (ink.HasProperty("_Cull"))
                ink.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
            mr.sharedMaterial = ink;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }
}
