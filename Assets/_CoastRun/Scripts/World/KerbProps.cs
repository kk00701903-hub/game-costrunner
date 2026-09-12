using UnityEngine;

namespace CoastRun
{
    /// 59차(사용자 「도로의 네모 박스가 계속 반복된다 — 변주를 주고 Blender 로 다시 디자인」):
    ///   인도 가장자리(가로등 사이) 15 m 마다 놓이던 주황 상자 화분을 제주 소품 6종(Kerb_*.fbx, Tools/blender/kerb_kit.py)으로 교체.
    ///   같은 종류가 연달아 나오지 않게 하고, 회전·크기·꽃 색을 조금씩 흔들어 반복감을 없앤다. 모델이 없으면 옛 상자 화분.
    public static class KerbProps
    {
        private static readonly string[] Kinds = { "Kerb_PlanterBasalt", "Kerb_PlanterWood", "Kerb_CrateStack", "Kerb_Onggi", "Kerb_Buoys", "Kerb_StoneBed" };
        private static int _last = -1, _last2 = -1;
        private static int _seq;
        private static bool _checked, _available;

        public static bool Available
        {
            get
            {
                if (!_checked) { _checked = true; _available = JejuKit.Load(Kinds[0]) != null; }
                return _available;
            }
        }

        /// 소품 하나를 놓는다(또는 일부러 비운다). false 면 키트가 없어 호출처가 옛 상자를 그린다.
        public static bool Spawn(Transform parent, Vector3 localPos, System.Random rng)
        {
            if (!Available) return false;
            // 직전 둘과 다른 종류 — 30 m 안에 같은 소품이 두 번 보이지 않게
            int k = rng.Next(Kinds.Length);
            for (int guard = 0; guard < 6 && (k == _last || k == _last2); guard++) k = rng.Next(Kinds.Length);
            _last2 = _last; _last = k;
            // 6개 중 3번에 1번은 아예 비워 두어 리듬을 깬다(가로등만 남는다)
            if ((_seq++ % 5) == 4 && rng.Next(2) == 0) return true;

            string name = Kinds[k];
            float yaw = (float)rng.NextDouble() * 360f;
            if (name == "Kerb_PlanterWood") yaw = (rng.Next(2) == 0 ? 0f : 180f) + ((float)rng.NextDouble() - 0.5f) * 14f;   // 긴 쪽이 도로와 나란히
            float scale = 0.88f + (float)rng.NextDouble() * 0.28f;
            var go = JejuKit.Spawn(name, parent, localPos, yaw, scale);
            if (go == null) return false;
            go.name = name;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            TintBlooms(go, name, rng);
            BlobShadow.Attach(go.transform, 0.55f * scale);
            return true;
        }

        /// 꽃 색 변주: 같은 모델이라도 수국 파랑/분홍/하양, 유채 노랑/주황이 섞인다.
        private static void TintBlooms(GameObject go, string kind, System.Random rng)
        {
            Color? bloom = null;
            switch (kind)
            {
                case "Kerb_PlanterBasalt":
                    bloom = rng.Next(3) switch { 0 => new Color(0.45f, 0.60f, 0.95f), 1 => new Color(0.85f, 0.55f, 0.85f), _ => new Color(0.98f, 0.97f, 0.92f) };
                    break;
                case "Kerb_StoneBed":
                    bloom = rng.Next(2) == 0 ? new Color(0.98f, 0.55f, 0.72f) : new Color(0.92f, 0.25f, 0.30f);
                    break;
                case "Kerb_PlanterWood":
                    bloom = rng.Next(3) == 0 ? new Color(0.98f, 0.60f, 0.20f) : new Color(1f, 0.85f, 0.20f);
                    break;
            }
            if (bloom == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.name.StartsWith("Bloom")) continue;
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor("_BaseColor", bloom.Value);
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }
    }
}
