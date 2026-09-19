using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 137차(사용자): 집 에셋을 지붕·본채·마당·정원으로 나눠 절차적으로 다시 그림 + 집마다 간판(우리집/엄마 집/상점).
    /// 키트 모델(도로용) 대신 만화풍 색 블록: 본채(벽·굽도리) + 박공/초가/평지붕 + 문·창·굴뚝·계단 + 울타리·징검돌·화단.
    public static class VillageHouses
    {
        public enum Style { Hero, Mom, Shop, Pastel }

        static Material M(Color c, float smooth = 0.05f) => CoastMaterials.CreateLit(c, smooth);

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Color col, float yaw = 0f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(g.GetComponent<Collider>()); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size; g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            g.GetComponent<Renderer>().sharedMaterial = M(col); return g;
        }
        static GameObject Ball(Transform parent, string name, Vector3 pos, Vector3 size, Color col)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(g.GetComponent<Collider>()); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = M(col); return g;
        }
        static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 size, Color col)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(g.GetComponent<Collider>()); g.name = name;
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = M(col); return g;
        }

        /// 박공 지붕(삼각 기둥, 용마루는 x축) — 평면 셰이딩.
        static GameObject Gable(Transform parent, string name, Vector3 pos, float w, float d, float h, Color col)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            float hw = w * 0.5f, hd = d * 0.5f;
            Vector3 a0 = new Vector3(-hw, 0f, hd), b0 = new Vector3(-hw, 0f, -hd), t0 = new Vector3(-hw, h, 0f);
            Vector3 a1 = new Vector3(hw, 0f, hd), b1 = new Vector3(hw, 0f, -hd), t1 = new Vector3(hw, h, 0f);
            var v = new System.Collections.Generic.List<Vector3>(); var tris = new System.Collections.Generic.List<int>();
            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) { int i = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3); tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 }); }
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2) { int i = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); tris.AddRange(new[] { i, i + 2, i + 1 }); }
            Quad(a0, t0, t1, a1);      // 앞 경사면(+z)
            Quad(b1, t1, t0, b0);      // 뒤 경사면(-z)
            Tri(b0, t0, a0);           // 왼쪽 박공
            Tri(a1, t1, b1);           // 오른쪽 박공
            Quad(a0, a1, b1, b0);      // 바닥
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = m; go.GetComponent<MeshRenderer>().sharedMaterial = M(col);
            return go;
        }

        public static Transform Build(Transform root, Vector3 ground, float yaw, Style style, string signKo, string signEn, Color? wallOverride = null, Color? roofOverride = null, float scale = 1f)
        {
            var house = new GameObject("House_" + style).transform; house.SetParent(root, false);
            house.position = ground; house.rotation = Quaternion.Euler(0f, yaw, 0f); house.localScale = Vector3.one * scale;
            Color wall, trim, roof, door, frame;
            float W, H, D;
            switch (style)
            {
                case Style.Hero: wall = new Color(1f, 0.90f, 0.86f); trim = new Color(0.85f, 0.62f, 0.55f); roof = new Color(0.92f, 0.36f, 0.34f); door = new Color(0.55f, 0.36f, 0.24f); frame = Color.white; W = 5.4f; H = 2.9f; D = 4.4f; break;
                case Style.Pastel: wall = wallOverride ?? new Color(0.62f, 0.86f, 0.80f); trim = Color.Lerp(wallOverride ?? new Color(0.62f, 0.86f, 0.80f), Color.black, 0.25f); roof = roofOverride ?? new Color(0.40f, 0.55f, 0.75f); door = new Color(0.55f, 0.36f, 0.24f); frame = Color.white; W = 4.6f; H = 2.7f; D = 4.0f; break;
                case Style.Mom: wall = new Color(0.98f, 0.94f, 0.80f); trim = new Color(0.62f, 0.50f, 0.36f); roof = new Color(0.78f, 0.62f, 0.34f); door = new Color(0.48f, 0.32f, 0.20f); frame = new Color(0.98f, 0.98f, 0.94f); W = 5.8f; H = 2.6f; D = 4.6f; break;
                default: wall = new Color(0.80f, 0.94f, 0.90f); trim = new Color(0.40f, 0.62f, 0.58f); roof = new Color(0.98f, 0.62f, 0.32f); door = new Color(0.42f, 0.30f, 0.22f); frame = Color.white; W = 6.0f; H = 3.0f; D = 4.2f; break;
            }
            // 본채
            Box(house, "Wall", new Vector3(0f, H * 0.5f + 0.05f, 0f), new Vector3(W, H, D), wall);
            Box(house, "Plinth", new Vector3(0f, 0.25f, 0f), new Vector3(W + 0.18f, 0.5f, D + 0.18f), trim);
            Box(house, "Band", new Vector3(0f, H + 0.02f, 0f), new Vector3(W + 0.12f, 0.16f, D + 0.12f), trim);
            // 지붕
            if (style == Style.Mom)
            {
                // 140차(시안): 짚 지붕 — 두툼한 돔 + 처마 챙(밝은 볏짚색) + 결 줄무늬
                var straw = VillagePalette.Thatch; var strawD = Color.Lerp(VillagePalette.Thatch, new Color(0.55f, 0.40f, 0.20f), 0.35f);
                Ball(house, "Thatch", new Vector3(0f, H - 0.35f, 0f), new Vector3(W + 2.0f, 3.6f, D + 2.0f), straw);
                Ball(house, "ThatchTop", new Vector3(0f, H + 0.55f, 0f), new Vector3(W + 0.6f, 2.6f, D + 0.6f), Color.Lerp(straw, Color.white, 0.12f));
                Cyl(house, "Eave", new Vector3(0f, H + 0.08f, 0f), new Vector3(W + 2.1f, 0.14f, D + 2.1f), strawD);
                for (int r = 0; r < 3; r++) Cyl(house, "Rope" + r, new Vector3(0f, H + 0.5f + r * 0.5f, 0f), new Vector3((W + 1.6f) * (1f - r * 0.18f), 0.03f, (D + 1.6f) * (1f - r * 0.18f)), new Color(0.50f, 0.36f, 0.22f));
            }
            else if (style == Style.Shop)
            {
                Box(house, "Roof", new Vector3(0f, H + 0.32f, 0f), new Vector3(W + 0.8f, 0.36f, D + 0.8f), roof);
                Box(house, "Parapet", new Vector3(0f, H + 0.62f, 0f), new Vector3(W + 0.9f, 0.25f, D + 0.9f), Color.Lerp(roof, Color.white, 0.25f));
                // 차양(앞면 줄무늬)
                for (int i = 0; i < 6; i++)
                    Box(house, "Awning" + i, new Vector3(-W * 0.42f + i * (W * 0.84f / 5f), H * 0.72f, D * 0.5f + 0.45f), new Vector3(W * 0.84f / 5f - 0.02f, 0.10f, 0.95f), i % 2 == 0 ? roof : Color.white)
                        .transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                Box(house, "Counter", new Vector3(0f, 0.55f, D * 0.5f + 0.35f), new Vector3(W * 0.5f, 1.1f, 0.5f), trim);
                Ball(house, "Fruit1", new Vector3(-0.8f, 1.25f, D * 0.5f + 0.35f), Vector3.one * 0.32f, new Color(1f, 0.62f, 0.18f));
                Ball(house, "Fruit2", new Vector3(0.1f, 1.25f, D * 0.5f + 0.35f), Vector3.one * 0.32f, new Color(0.95f, 0.30f, 0.30f));
                Ball(house, "Fruit3", new Vector3(0.9f, 1.25f, D * 0.5f + 0.35f), Vector3.one * 0.32f, new Color(0.55f, 0.80f, 0.30f));
            }
            else
            {
                Gable(house, "Roof", new Vector3(0f, H + 0.08f, 0f), W + 0.9f, D + 0.9f, 1.9f, roof);
                Box(house, "Ridge", new Vector3(0f, H + 1.98f, 0f), new Vector3(W + 1.0f, 0.14f, 0.28f), Color.Lerp(roof, Color.black, 0.25f));
                if (style == Style.Hero)
                {
                    Box(house, "Chimney", new Vector3(W * 0.28f, H + 1.55f, -0.8f), new Vector3(0.55f, 1.1f, 0.55f), trim);
                    Box(house, "ChimneyTop", new Vector3(W * 0.28f, H + 2.12f, -0.8f), new Vector3(0.68f, 0.14f, 0.68f), Color.Lerp(trim, Color.black, 0.2f));
                }
                // 다락 창(박공 앞면)
                Box(house, "AtticFrame", new Vector3(0f, H + 0.85f, D * 0.5f + 0.47f), new Vector3(0.9f, 0.9f, 0.08f), frame);
                Box(house, "Attic", new Vector3(0f, H + 0.85f, D * 0.5f + 0.50f), new Vector3(0.74f, 0.74f, 0.08f), new Color(0.72f, 0.88f, 1f));
            }
            // 문(앞면 = +z)
            float fz = D * 0.5f;
            Box(house, "DoorFrame", new Vector3(0f, 1.05f, fz + 0.02f), new Vector3(1.25f, 2.05f, 0.10f), frame);
            Box(house, "Door", new Vector3(0f, 1.0f, fz + 0.06f), new Vector3(1.0f, 1.9f, 0.08f), door);
            Ball(house, "Knob", new Vector3(0.32f, 1.0f, fz + 0.12f), Vector3.one * 0.12f, new Color(1f, 0.85f, 0.35f));
            Box(house, "Step", new Vector3(0f, 0.12f, fz + 0.45f), new Vector3(1.7f, 0.22f, 0.8f), Color.Lerp(trim, Color.white, 0.3f));
            // 창(앞 2, 옆 1씩)
            void Window(Vector3 p, float yawW)
            {
                var f = Box(house, "WinFrame", p, new Vector3(1.0f, 1.0f, 0.10f), frame, yawW);
                var g = Box(house, "Win", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.03f), new Vector3(0.82f, 0.82f, 0.08f), new Color(0.72f, 0.88f, 1f), yawW);
                Box(house, "WinBar", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0.82f, 0.04f), frame, yawW);
                Box(house, "WinBar2", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.06f), new Vector3(0.82f, 0.06f, 0.04f), frame, yawW);
                Box(house, "Sill", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, -0.56f, 0.08f), new Vector3(1.1f, 0.08f, 0.22f), trim, yawW);
                // 창가 화분
                Box(house, "Pot", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, -0.62f, 0.2f), new Vector3(0.7f, 0.18f, 0.2f), new Color(0.72f, 0.45f, 0.30f), yawW);
                Ball(house, "PotFlower", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, -0.48f, 0.2f), new Vector3(0.6f, 0.22f, 0.24f), new Color(1f, 0.55f, 0.70f));
            }
            Window(new Vector3(-W * 0.30f, 1.55f, fz + 0.02f), 0f); Window(new Vector3(W * 0.30f, 1.55f, fz + 0.02f), 0f);
            Window(new Vector3(W * 0.5f + 0.02f, 1.55f, 0f), 90f); Window(new Vector3(-W * 0.5f - 0.02f, 1.55f, 0f), -90f);
            // 간판: 문 오른쪽 기둥 + 널판 + 글자
            if (!string.IsNullOrEmpty(signKo)) Sign(house, new Vector3(W * 0.5f + 1.0f, 0f, fz + 0.8f), signKo, signEn, style == Style.Shop ? roof : trim);
            // 울타리·징검돌·화단(마당) — 우리 집만 넓게, 나머지는 작게
            if (style == Style.Hero)
            {
                Fence(house, -9.6f, 9.6f, fz + 9.6f, true);   // 앞
                for (float z = fz + 0.8f; z < fz + 9.4f; z += 1.2f) { Fence1(house, new Vector3(-9.6f, 0f, z), 90f); Fence1(house, new Vector3(9.6f, 0f, z), 90f); }
                for (int i = 0; i < 7; i++) Cyl(house, "Stone", new Vector3((i % 2 == 0 ? -0.25f : 0.25f), 0.03f, fz + 1.4f + i * 1.15f), new Vector3(0.75f, 0.03f, 0.55f), new Color(0.80f, 0.78f, 0.72f));
                Bed(house, new Vector3(-3.2f, 0f, fz + 1.2f)); Bed(house, new Vector3(3.2f, 0f, fz + 1.2f));
            }
            else
            {
                Bed(house, new Vector3(-W * 0.5f + 0.4f, 0f, fz + 1.0f)); Bed(house, new Vector3(W * 0.5f - 0.4f, 0f, fz + 1.0f));
            }
            BuildingOutline.Attach(house, 0.03f);
            var bc = house.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, H * 0.5f, 0f); bc.size = new Vector3(W + 0.4f, H, D + 0.4f);
            return house;
        }

        static void Fence(Transform house, float x0, float x1, float z, bool gate)
        {
            for (float x = x0; x <= x1 + 0.01f; x += 1.2f)
            {
                if (gate && Mathf.Abs(x) < 1.3f) continue;
                Fence1(house, new Vector3(x, 0f, z), 0f);
            }
        }
        static void Fence1(Transform house, Vector3 p, float yaw)
        {
            var c = new Color(0.98f, 0.96f, 0.90f);
            Box(house, "Post", p + new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.14f), c);
            Ball(house, "PostTop", p + new Vector3(0f, 0.92f, 0f), Vector3.one * 0.2f, c);
            Box(house, "Rail", p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.6f, 0.62f, 0f), new Vector3(1.2f, 0.08f, 0.06f), c, yaw);
            Box(house, "Rail2", p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.6f, 0.30f, 0f), new Vector3(1.2f, 0.08f, 0.06f), c, yaw);
        }
        static void Bed(Transform house, Vector3 p)
        {
            Box(house, "Bed", p + new Vector3(0f, 0.08f, 0f), new Vector3(1.8f, 0.16f, 1.0f), new Color(0.45f, 0.32f, 0.22f));
            Color[] cols = { new Color(1f, 0.55f, 0.70f), new Color(1f, 0.92f, 0.45f), Color.white, new Color(0.70f, 0.55f, 0.95f) };
            for (int i = 0; i < 6; i++)
            {
                var f = Ball(house, "Fl", p + new Vector3(-0.7f + i * 0.28f, 0.34f, (i % 2 == 0 ? -0.22f : 0.22f)), Vector3.one * 0.22f, cols[i % cols.Length]);
                Cyl(house, "St", f.transform.localPosition - new Vector3(0f, 0.14f, 0f), new Vector3(0.04f, 0.12f, 0.04f), new Color(0.35f, 0.62f, 0.28f));
            }
        }

        /// 간판: 나무 기둥 + 널판 + 월드 캔버스 글자(한/영).
        public static void Sign(Transform parent, Vector3 p, string ko, string en, Color board)
        {
            Cyl(parent, "SignPost", p + new Vector3(0f, 0.9f, 0f), new Vector3(0.14f, 0.9f, 0.14f), new Color(0.48f, 0.33f, 0.20f));
            var b = Box(parent, "SignBoard", p + new Vector3(0f, 1.75f, 0f), new Vector3(2.1f, 0.62f, 0.12f), board);
            Box(parent, "SignEdge", p + new Vector3(0f, 1.75f, -0.02f), new Vector3(2.22f, 0.74f, 0.10f), Color.Lerp(board, Color.black, 0.35f));
            var go = new GameObject("SignText", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false); go.transform.localPosition = p + new Vector3(0f, 1.75f, 0.075f); go.transform.localRotation = Quaternion.identity;
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.sortingOrder = 5;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200f, 60f); rt.localScale = Vector3.one * 0.01f;
            var t = CoastHudLayout.MakeText(rt, "T", Loc.T(ko, en), 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            CoastUiArt.OutlineText(t, new Color(0.25f, 0.15f, 0.10f, 0.9f), 2f);
            // 뒷면에서도 읽히게 뒤집은 복사본
            var back = Object.Instantiate(go, parent); back.transform.localPosition = p + new Vector3(0f, 1.75f, -0.075f); back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
