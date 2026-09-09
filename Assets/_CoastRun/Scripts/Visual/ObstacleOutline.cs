using UnityEngine;

namespace CoastRun
{
    /// 14차: 메시 장애물에 흰 인버티드 헐 테두리. 배경(돌길·건물)과 톤이 같은 콘·크레이트·배리어가
    /// 두께 있는 흰 선으로 떨어져 나온다. 그림자·링·광원 쿼드는 건너뛴다.
    public static class ObstacleOutline
    {
        private static Material _ink;
        /// 33차: 장애물 테두리 색(붉은색) — 메시 헐과 그림 테두리가 같은 색을 쓴다.
        public static readonly Color Red = new Color(0.88f, 0.12f, 0.14f, 1f);

        public static void Attach(Transform root, float scale = 1.10f)   // 33차: 1.06→1.10(굵게)
        {
            if (root == null)
                return;
            _ink ??= Make();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;
                string n = mf.gameObject.name;
                if (n == "BlobShadow" || n == "HazardRing" || n == "PickupGlow" || n == "Outline" || n.StartsWith("Painted_") || n.StartsWith("Decal_"))
                    continue;
                if (mf.transform.Find("Outline") != null) continue;
                // 납작한 데칼(웅덩이 판)은 테두리를 치면 그림자처럼 보인다.
                var b = mf.sharedMesh.bounds.size;
                var ls = mf.transform.lossyScale;
                if (b.y * Mathf.Abs(ls.y) < 0.08f) continue;

                var shell = new GameObject("Outline");
                shell.transform.SetParent(mf.transform, false);
                shell.transform.localScale = Vector3.one * scale;
                var f = shell.AddComponent<MeshFilter>();
                f.sharedMesh = mf.sharedMesh;
                var r = shell.AddComponent<MeshRenderer>();
                r.sharedMaterial = _ink;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static Material Make()
        {
            var ink = CoastMaterials.CreateUnlit(() => Red);   // 33차: 붉은 굵은 선(14차-11 잉크 → 사용자 요청)
            if (ink.HasProperty("_Cull"))
                ink.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
            return ink;
        }
    }
}
