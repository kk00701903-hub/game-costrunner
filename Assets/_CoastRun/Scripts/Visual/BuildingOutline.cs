using UnityEngine;

namespace CoastRun
{
    /// 14차-11: 건물·큰 소품용 잉크 테두리 — 정점을 노멀 방향으로 밀어낸 셸(CoastRun/InkOutline, Cull Front).
    /// 스케일 헐(ObstacleOutline)은 10 m 건물에서 50 cm 두께가 되므로, 미터 단위 폭(2~3 cm)으로 민다.
    /// 캐릭터·소품과 같은 짙은 남색이라 화면 전체가 한 '잉크'로 묶인다.
    public static class BuildingOutline
    {
        private static Material _ink;

        public static void Attach(Transform root, float widthMeters = 0.028f)
        {
            if (root == null) return;
            _ink ??= Make(widthMeters);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;
                string n = mf.gameObject.name;
                if (n == "Outline" || n == "Ink" || n == "BlobShadow" || n == "ContactShadow" || n.StartsWith("Painted_") || n.StartsWith("Decal_")) continue;
                if (mf.transform.Find("Ink") != null) continue;
                var shell = new GameObject("Ink");
                shell.transform.SetParent(mf.transform, false);
                var f = shell.AddComponent<MeshFilter>();
                f.sharedMesh = mf.sharedMesh;
                var r = shell.AddComponent<MeshRenderer>();
                r.sharedMaterial = _ink;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static Material Make(float width)
        {
            var sh = Shader.Find("CoastRun/InkOutline");
            if (sh == null) return CoastMaterials.CreateUnlit(new Color(0.06f, 0.05f, 0.10f, 1f));
            var m = new Material(sh);
            m.SetColor("_OutlineColor", new Color(0.06f, 0.05f, 0.10f, 1f));
            m.SetFloat("_Width", width);
            return m;
        }
    }
}
