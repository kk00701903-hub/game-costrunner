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
            // 14차-3: 스킨드 메시(Mixamo 리그)는 셸을 트랜스폼으로 키울 수 없다 — 노멀 방향으로
            // 정점을 미는 잉크 셰이더를 추가 머티리얼 슬롯으로 붙인다(서브메시가 1개면 같은 메시를
            // 남은 머티리얼로 한 번 더 그린다).
            var skinned = GetComponent<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                var inkShader = Shader.Find("CoastRun/InkOutline");
                if (inkShader == null) return;
                if (skinned.sharedMesh == null) return;
                var inkMat = new Material(inkShader);
                inkMat.SetColor("_OutlineColor", Color.Lerp(CoastPalette.ShadowCool, Color.black, 0.6f));
                inkMat.SetFloat("_Width", 0.010f);   // 25차-1: 0.017→0.010, 셰이더에서 거리 비례
                var shell = new GameObject("Outline");
                shell.transform.SetParent(transform, false);
                var smr = shell.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = skinned.sharedMesh;
                smr.bones = skinned.bones;
                smr.rootBone = skinned.rootBone;
                smr.localBounds = skinned.localBounds;
                var inks = new Material[Mathf.Max(1, skinned.sharedMesh.subMeshCount)];
                for (int i = 0; i < inks.Length; i++) inks[i] = inkMat;
                smr.sharedMaterials = inks;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = false;
                smr.updateWhenOffscreen = skinned.updateWhenOffscreen;
                return;
            }

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
