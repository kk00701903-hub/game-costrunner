using UnityEngine;

namespace CoastRun
{
    /// 14차-14: 상가 아닌 필지들 — 제주 해안 마을의 '사이': 낮은 집, 돌담 공터, 작은 공원, 카페 테라스.
    /// 피벗 = 필지 도로 쪽 앞 중앙(+X 가 도로). 폭 ≈ 9.6 m(Z), 깊이 ≈ 6 m(−X).
    public static class JejuLots
    {
        private static readonly Color[] HouseRoofs =
        {
            new Color(0.83f, 0.42f, 0.30f),   // 주황 기와(테라코타)
            new Color(0.31f, 0.43f, 0.55f),   // 슬레이트 청회색
            new Color(0.24f, 0.55f, 0.55f),   // 청록 함석
            new Color(0.20f, 0.20f, 0.22f),   // 현무암 검정
        };
        private static Material _grass, _gravel, _path;

        private static void Ensure()
        {
            _grass ??= CoastMaterials.CreateLit(() => new Color(0.55f, 0.78f, 0.40f), 0.02f);
            _gravel ??= CoastMaterials.CreateLit(() => new Color(0.62f, 0.58f, 0.52f), 0.02f);
            _path ??= CoastMaterials.CreateLit(() => new Color(0.85f, 0.80f, 0.70f), 0.02f);
        }

        /// 제주 낮은 집: 현무암 기단 + 크림 벽 + 진짜 지붕색 4종. 마당 돌담·감귤나무.
        public static void House(Transform pivot, System.Random rng, bool twoStorey)
        {
            var go = JejuKit.Spawn(twoStorey ? "House_B" : "House_A", pivot, Vector3.zero, 180f, 1f);
            if (go == null) return;
            Color wall = rng.Next(4) == 0 ? JejuKit.Sky : JejuKit.Cream;
            JejuKit.Recolor(go, "Wall", wall);
            JejuKit.Recolor(go, "Frame", JejuKit.Accent(wall));
            JejuKit.Recolor(go, "Door", new Color(0.45f, 0.30f, 0.20f));
            JejuKit.Recolor(go, "Roof", HouseRoofs[rng.Next(HouseRoofs.Length)]);
            JejuKit.Recolor(go, "Trim", Color.white);
            BuildingOutline.Attach(go.transform, 0.03f);
            // 마당: 감귤나무 한두 그루, 돌담
            JejuKit.Spawn("Prop_OrangeTree", pivot, new Vector3(-1.2f, 0f, 3.9f), (float)rng.NextDouble() * 360f, 0.85f);
            if (rng.Next(2) == 0) JejuKit.Spawn("Prop_OrangeTree", pivot, new Vector3(-2.5f, 0f, -4.0f), (float)rng.NextDouble() * 360f, 0.8f);
            for (int side = -1; side <= 1; side += 2)
                JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(0.55f, 0f, side * 3.6f), 0f, 0.55f);
            if (rng.Next(2) == 0) StreetDressing.Hydrangea(pivot, new Vector3(0.8f, 0f, 2.6f), rng, 0.9f);
        }

        /// 공터: 돌담으로 둘린 자갈 마당 + 감귤나무 + 귤 상자 + (가끔) 돌하르방
        public static void EmptyLot(Transform pivot, System.Random rng)
        {
            Ensure();
            Box(pivot, "Gravel", new Vector3(-3.0f, 0.02f, 0f), new Vector3(6.2f, 0.04f, 9.4f), _gravel);
            // 돌담 3면(뒤·양옆) + 앞은 낮게 반만
            JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(-6.0f, 0f, 0f), 90f, 0.7f);
            JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(-3.0f, 0f, 4.7f), 0f, 0.6f);
            JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(-3.0f, 0f, -4.7f), 0f, 0.6f);
            JejuKit.Spawn("Prop_StoneWall", pivot, new Vector3(0.4f, 0f, 2.8f), 0f, 0.5f);
            int trees = 2 + rng.Next(3);
            for (int i = 0; i < trees; i++)
                JejuKit.Spawn("Prop_OrangeTree", pivot, new Vector3(-1.5f - (float)rng.NextDouble() * 3.5f, 0f, -3.5f + i * (7f / Mathf.Max(1, trees - 1))), (float)rng.NextDouble() * 360f, 0.8f + (float)rng.NextDouble() * 0.4f);
            if (rng.Next(2) == 0) JejuKit.Spawn("Prop_OrangeStall", pivot, new Vector3(-0.6f, 0f, -2.6f), 180f);
            if (rng.Next(3) == 0) JejuKit.Spawn("Prop_Hareubang", pivot, new Vector3(0.2f, 0f, -3.6f), 180f, 0.8f);
        }

        /// 작은 공원: 잔디 + 흙길 + 정자 + 벤치 + 야자 + 파라솔 테이블
        public static void Park(Transform pivot, System.Random rng)
        {
            Ensure();
            Box(pivot, "Grass", new Vector3(-3.0f, 0.02f, 0f), new Vector3(6.4f, 0.04f, 9.6f), _grass);
            Box(pivot, "Path", new Vector3(-1.4f, 0.035f, 0f), new Vector3(1.2f, 0.02f, 9.6f), _path);
            var pav = JejuKit.Spawn("Prop_Pavilion", pivot, new Vector3(-4.0f, 0f, 1.5f), 0f, 0.9f);
            if (pav != null) { JejuKit.Recolor(pav, "Roof", HouseRoofs[1]); BuildingOutline.Attach(pav.transform, 0.02f); }
            JejuKit.Spawn("Prop_Bench", pivot, new Vector3(-0.9f, 0f, -3.2f), 180f);
            JejuKit.Spawn("Prop_Palm", pivot, new Vector3(-4.5f, 0f, -3.4f), (float)rng.NextDouble() * 360f, 1.0f);
            if (rng.Next(2) == 0) JejuKit.Spawn("Prop_Palm", pivot, new Vector3(-1.5f, 0f, 4.2f), (float)rng.NextDouble() * 360f, 0.85f);
            CafeTerrace(pivot, rng, new Vector3(-1.0f, 0f, 1.2f));
            StreetDressing.Hydrangea(pivot, new Vector3(0.6f, 0f, -4.2f), rng, 0.9f);
            StreetDressing.Hydrangea(pivot, new Vector3(0.6f, 0f, 4.3f), rng, 0.9f);
        }

        /// 카페 테라스: 파라솔(그림) + 탁자·의자 세트(3D). 상가 앞 인도 또는 공원 안.
        public static void CafeTerrace(Transform pivot, System.Random rng, Vector3? at = null)
        {
            Vector3 p = at ?? new Vector3(1.0f, 0f, 2.6f);
            var set = JejuKit.Spawn("Prop_CafeSet", pivot, p, (float)rng.NextDouble() * 360f, 1f);
            if (set != null) { JejuKit.Recolor(set, "Frame", JejuKit.LastAccent); BuildingOutline.Attach(set.transform, 0.012f); }
            if (PaintedProp.Available("Parasol"))
            {
                var pp = new GameObject("Parasol").transform;
                pp.SetParent(pivot, false); pp.localPosition = p + new Vector3(0f, 0f, 0.2f);
                PaintedProp.Attach(pp, "Parasol", 2.4f, replace: false);
                BlobShadow.Attach(pp, 1.0f);
            }
        }

        private static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = size;
            CoastEditUtil.DestroyCollider(go);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
