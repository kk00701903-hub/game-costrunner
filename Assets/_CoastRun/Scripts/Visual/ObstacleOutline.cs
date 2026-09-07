using UnityEngine;

namespace CoastRun
{
    /// 14차: 메시 장애물에 흰 인버티드 헐 테두리. 배경(돌길·건물)과 톤이 같은 콘·크레이트·배리어가
    /// 두께 있는 흰 선으로 떨어져 나온다. 그림자·링·광원 쿼드는 건너뛴다.
    public static class ObstacleOutline
    {
        private static Material _ink;

        public static void Attach(Transform root, float scale = 1.06f)
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
            var ink = CoastMaterials.CreateUnlit(() => new Color(0.06f, 0.05f, 0.10f, 1f));   // 14차-11: 짙은 잉크 선
            if (ink.HasProperty("_Cull"))
                ink.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
            return ink;
        }
    }
}
