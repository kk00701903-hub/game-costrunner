using UnityEngine;

namespace CoastRun
{
    /// Firefly-painted prop as a yaw-only billboard (RGB on pure magenta, keyed by the
    /// ChromaUnlit shader). Resources/CoastRun/Obs_<Key>.png; when the painting is
    /// missing the caller keeps its procedural visual, so the game never breaks.
    public static class PaintedProp
    {
        /// 21차-3: 그림에 이미 굵은 이중 테두리(남색+흰)를 구운 키 — 셰이더 테두리는 얇게만(흰 링을 덮지 않게).
        /// Tools/Art/bold_outline.py 로 굽는다. 셰이더 텍셀 테두리는 멀리서 사라지지만 구운 테두리는 크기에 비례해 남는다.
        private static bool HasBakedOutline(string key) =>
            key.StartsWith("Jelly_") || key.StartsWith("Coin") || key == "Potion" || key == "Heart" || key == "Star";
        private static float OutlineTexels(string key, Texture2D tex) =>
            HasBakedOutline(key) ? 1.5f : Mathf.Clamp(tex.width / 120f, 4f, 10f);
        public static Texture2D Load(string key) =>
            Resources.Load<Texture2D>(ArtAssets.ResourceRoot + "Obs_" + key);

        public static bool Available(string key) => Load(key) != null;

        /// Adds the sprite under `root`, `height` metres tall with its feet at y = 0
        /// (+ `groundLift`), and hides every other renderer under `root` if `replace`.
        public static Transform Attach(Transform root, string key, float height, bool replace = true,
            float groundLift = 0f, float zOffset = 0f, bool outline = false, Color? outlineColor = null, float outlineMul = 1f)
        {
            // 33차: 장애물은 붉은 굵은 테두리(outlineColor/outlineMul), 나머지는 기존 남색.
            Color oc = outlineColor ?? new Color(0.06f, 0.05f, 0.10f, 1f);
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
            // 하트처럼 분홍이 본체인 스프라이트는 핑크 에지 제거를 끈다(키 거리만으로 자른다).
            // 14차-3: 하트도 Kling 빨간 하트로 바뀌어 핑크 에지 제거를 그대로 둔다(예전 분홍 하트 예외 삭제).
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_KeyColor")) mat.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
            // 14차: 장애물은 흰 테두리 — 그림 크기에 맞춰 두께를 잡는다(1024px 기준 5텍셀 ≈ 화면에서 2px).
            if (outline && mat.HasProperty("_OutlineOn"))
            {
                mat.SetFloat("_OutlineOn", 1f);
                // 14차-9: 흰 테두리는 밝은 배경에서 뿌옇게 번져 보였다 → 짙은 남색 굵은 선(레퍼런스의 볼드 아웃라인).
                // 14차-10: 더 진하고 굵게(거의 검정 남색, 1024px 기준 8텍셀)
                mat.SetColor("_OutlineColor", oc);
                mat.SetFloat("_OutlineWidth", OutlineTexels(key, tex) * outlineMul);
            }
            var mr = quad.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            quad.AddComponent<YawBillboard>();

            // 14차-10: '두께' — 같은 그림을 어둡게 한 장 뒤에 살짝 비껴 깔아 종이 인형이 아니라
            // 두툼한 조각처럼 읽히게 한다(빌보드와 함께 돌아가므로 늘 한쪽 가장자리로 어두운 옆면이 보인다).
            if (outline)
            {
                var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
                back.name = "Painted_Back";
                back.transform.SetParent(quad.transform, false);
                CoastEditUtil.DestroyCollider(back);
                back.transform.localPosition = new Vector3(0.028f, -0.012f, 0.06f);
                back.transform.localScale = new Vector3(1.035f, 1.0f, 1f);
                var bm = new Material(shader);
                if (bm.HasProperty("_BaseMap")) bm.SetTexture("_BaseMap", tex); else bm.mainTexture = tex;
                if (bm.HasProperty("_BaseColor")) bm.SetColor("_BaseColor", new Color(0.22f, 0.20f, 0.28f, 1f));
                if (bm.HasProperty("_KeyColor")) bm.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
                if (bm.HasProperty("_OutlineOn")) bm.SetFloat("_OutlineOn", 1f);
                if (bm.HasProperty("_OutlineColor")) bm.SetColor("_OutlineColor", oc);
                if (bm.HasProperty("_OutlineWidth")) bm.SetFloat("_OutlineWidth", OutlineTexels(key, tex) * outlineMul);
                if (bm.HasProperty("_Shade")) bm.SetFloat("_Shade", 0f);
                var br = back.GetComponent<Renderer>();
                br.sharedMaterial = bm;
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                br.receiveShadows = false;
            }
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
