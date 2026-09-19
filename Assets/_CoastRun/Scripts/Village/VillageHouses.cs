using System.Collections.Generic;
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
        static Texture2D _plank; static Texture2D PlankTex => _plank != null ? _plank : (_plank = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_Plank"));

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
            var wallGo = Box(house, "Wall", new Vector3(0f, H * 0.5f + 0.05f, 0f), new Vector3(W, H, D), wall);
            // 151차: 널판 텍스처(손그림풍) — 큐브 면마다 0..1 이라 가로 2 타일·세로 1 타일
            if (PlankTex != null && style != Style.Mom) { var pm = ArtAssets.CreateTexturedLit(PlankTex, wall, 0.02f); pm.mainTextureScale = new Vector2(2f, 1f); wallGo.GetComponent<Renderer>().sharedMaterial = pm; }
            // 147차: 널판(사이딩) 홈 + 모서리 트림 — 평평한 벽에 입체감
            var groove = Color.Lerp(wall, Color.black, 0.18f);
            for (float y = 0.75f; y < H - 0.15f; y += 0.36f)
            {
                Box(house, "Plank", new Vector3(0f, y, D * 0.5f + 0.005f), new Vector3(W - 0.04f, 0.035f, 0.02f), groove);
                Box(house, "Plank", new Vector3(0f, y, -D * 0.5f - 0.005f), new Vector3(W - 0.04f, 0.035f, 0.02f), groove);
                Box(house, "Plank", new Vector3(W * 0.5f + 0.005f, y, 0f), new Vector3(0.02f, 0.035f, D - 0.04f), groove);
                Box(house, "Plank", new Vector3(-W * 0.5f - 0.005f, y, 0f), new Vector3(0.02f, 0.035f, D - 0.04f), groove);
            }
            foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f })
                Box(house, "CornerTrim", new Vector3(sx * W * 0.5f, H * 0.5f + 0.05f, sz * D * 0.5f), new Vector3(0.16f, H, 0.16f), frame);
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
                Gable(house, "Roof", new Vector3(0f, H + 0.08f, 0f), W + 0.9f, D + 0.9f, 1.9f, Color.Lerp(roof, Color.black, 0.12f));
                // 147차: 기와 줄 — 경사면을 따라 반원통(한 줄 = 기와 한 단)을 겹쳐 쌓는다(앞·뒤 각 8단)
                TileRows(house, W + 0.9f, D + 0.9f, 1.9f, H + 0.08f, roof);
                Box(house, "Ridge", new Vector3(0f, H + 1.98f, 0f), new Vector3(W + 1.1f, 0.18f, 0.34f), Color.Lerp(roof, Color.black, 0.28f));
                Box(house, "Fascia", new Vector3(0f, H + 0.06f, (D + 0.9f) * 0.5f), new Vector3(W + 1.0f, 0.16f, 0.10f), frame);
                Box(house, "FasciaB", new Vector3(0f, H + 0.06f, -(D + 0.9f) * 0.5f), new Vector3(W + 1.0f, 0.16f, 0.10f), frame);
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
            Box(house, "DoorPanel", new Vector3(0f, 1.35f, fz + 0.105f), new Vector3(0.7f, 0.55f, 0.02f), Color.Lerp(door, Color.black, 0.2f));
            Box(house, "DoorPanel", new Vector3(0f, 0.6f, fz + 0.105f), new Vector3(0.7f, 0.65f, 0.02f), Color.Lerp(door, Color.black, 0.2f));
            Box(house, "Mat", new Vector3(0f, 0.24f, fz + 0.55f), new Vector3(0.9f, 0.03f, 0.5f), new Color(0.75f, 0.55f, 0.40f));
            // 147차: 벽 밑 관목·화분 — 시안처럼 건물 발치를 초록으로 채운다
            void Bush(Vector3 bp, float s) { for (int k = 0; k < 3; k++) Ball(house, "Bush", bp + new Vector3((k - 1) * s * 0.42f, s * 0.32f + (k == 1 ? 0.08f : 0f), (k % 2) * 0.12f), Vector3.one * s * (k == 1 ? 0.9f : 0.72f), k == 1 ? new Color(0.36f, 0.64f, 0.30f) : new Color(0.44f, 0.72f, 0.34f)); }
            Bush(new Vector3(-W * 0.5f + 0.5f, 0f, fz + 0.45f), 0.9f); Bush(new Vector3(W * 0.5f - 0.5f, 0f, fz + 0.45f), 0.9f);
            Bush(new Vector3(W * 0.5f + 0.35f, 0f, -D * 0.25f), 0.8f); Bush(new Vector3(-W * 0.5f - 0.35f, 0f, D * 0.2f), 0.8f);
            for (int k = 0; k < 2; k++)
            {
                var pp = new Vector3((k == 0 ? -1f : 1f) * 1.1f, 0f, fz + 0.75f);
                Cyl(house, "FlowerPot", pp + new Vector3(0f, 0.16f, 0f), new Vector3(0.36f, 0.16f, 0.36f), new Color(0.80f, 0.48f, 0.32f));
                Ball(house, "FlowerPotLeaf", pp + new Vector3(0f, 0.40f, 0f), new Vector3(0.42f, 0.30f, 0.42f), new Color(0.42f, 0.70f, 0.33f));
                Ball(house, "FlowerPotBloom", pp + new Vector3(0f, 0.56f, 0f), Vector3.one * 0.18f, k == 0 ? new Color(1f, 0.55f, 0.70f) : new Color(1f, 0.92f, 0.45f));
            }
            // 창(앞 2, 옆 1씩)
            void Window(Vector3 p, float yawW)
            {
                var f = Box(house, "WinFrame", p, new Vector3(1.0f, 1.0f, 0.10f), frame, yawW);
                var g = Box(house, "Win", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.03f), new Vector3(0.82f, 0.82f, 0.08f), new Color(0.72f, 0.88f, 1f), yawW);
                Box(house, "WinBar", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.06f), new Vector3(0.06f, 0.82f, 0.04f), frame, yawW);
                Box(house, "WinBar2", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, 0f, 0.06f), new Vector3(0.82f, 0.06f, 0.04f), frame, yawW);
                Box(house, "Sill", p + Quaternion.Euler(0f, yawW, 0f) * new Vector3(0f, -0.56f, 0.08f), new Vector3(1.1f, 0.08f, 0.22f), trim, yawW);
                // 147차: 덧문 + 꽃이 가득한 창가 화단
                var q = Quaternion.Euler(0f, yawW, 0f);
                Box(house, "Shutter", p + q * new Vector3(-0.66f, 0f, 0.01f), new Vector3(0.26f, 1.0f, 0.06f), frame, yawW);
                Box(house, "Shutter", p + q * new Vector3(0.66f, 0f, 0.01f), new Vector3(0.26f, 1.0f, 0.06f), frame, yawW);
                Box(house, "Pot", p + q * new Vector3(0f, -0.66f, 0.22f), new Vector3(1.05f, 0.24f, 0.26f), new Color(0.72f, 0.45f, 0.30f), yawW);
                Ball(house, "PotLeaf", p + q * new Vector3(0f, -0.52f, 0.22f), new Vector3(1.0f, 0.24f, 0.30f), new Color(0.40f, 0.68f, 0.32f));
                Color[] wc = { new Color(1f, 0.55f, 0.70f), new Color(1f, 0.92f, 0.45f), Color.white, new Color(1f, 0.62f, 0.55f) };
                for (int k = 0; k < 5; k++) Ball(house, "PotFlower", p + q * new Vector3(-0.38f + k * 0.19f, -0.44f + (k % 2) * 0.05f, 0.22f + (k % 2) * 0.06f), Vector3.one * 0.15f, wc[k % wc.Length]);
            }
            Window(new Vector3(-W * 0.30f, 1.55f, fz + 0.02f), 0f); Window(new Vector3(W * 0.30f, 1.55f, fz + 0.02f), 0f);
            Window(new Vector3(W * 0.5f + 0.02f, 1.55f, 0f), 90f); Window(new Vector3(-W * 0.5f - 0.02f, 1.55f, 0f), -90f);
            // 157차: 간판은 문 위 벽에(우리집·알바나라·마을상점…) — 마당의 기둥 간판은 상점만 남김
            if (!string.IsNullOrEmpty(signKo)) WallSign(house, signKo, signEn, W, H, fz, style == Style.Shop ? roof : new Color(0.62f, 0.42f, 0.24f), new Color(0.36f, 0.22f, 0.12f));
            if (!string.IsNullOrEmpty(signKo) && style == Style.Shop) Sign(house, Snap(house, new Vector3(W * 0.5f + 1.0f, 0f, fz + 0.8f)), signKo, signEn, roof);
            // 157차: 우리집 굴뚝 연기
            if (style == Style.Hero) { var sm = new GameObject("ChimneySmoke").transform; sm.SetParent(house, false); sm.localPosition = new Vector3(W * 0.28f, H + 2.2f, -0.8f); sm.gameObject.AddComponent<ChimneySmoke>(); }
            // 울타리·징검돌·화단(마당) — 우리 집만 넓게, 나머지는 작게
            if (style == Style.Hero)
            {
                Fence(house, -9.6f, 9.6f, fz + 9.6f, true);   // 앞
                for (float z = fz + 0.8f; z < fz + 9.4f; z += 1.2f) { Fence1(house, Snap(house, new Vector3(-9.6f, 0f, z)), 90f); Fence1(house, Snap(house, new Vector3(9.6f, 0f, z)), 90f); }
                for (int i = 0; i < 7; i++) Cyl(house, "Stone", Snap(house, new Vector3((i % 2 == 0 ? -0.25f : 0.25f), 0.03f, fz + 1.4f + i * 1.15f)), new Vector3(0.75f, 0.03f, 0.55f), new Color(0.80f, 0.78f, 0.72f));
                Bed(house, Snap(house, new Vector3(-3.2f, 0f, fz + 1.2f))); Bed(house, Snap(house, new Vector3(3.2f, 0f, fz + 1.2f)));
                // 157차: 우편함(동물의 숲) — 문 앞 오른쪽
                var mb = Snap(house, new Vector3(2.0f, 0f, fz + 2.2f));
                Cyl(house, "MailPost", mb + new Vector3(0f, 0.55f, 0f), new Vector3(0.12f, 0.55f, 0.12f), new Color(0.48f, 0.33f, 0.20f));
                Box(house, "MailBox", mb + new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.36f, 0.62f), new Color(0.95f, 0.45f, 0.40f));
                Cyl(house, "MailTop", mb + new Vector3(0f, 1.43f, 0f), new Vector3(0.42f, 0.31f, 0.42f), new Color(0.95f, 0.45f, 0.40f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Box(house, "MailDoor", mb + new Vector3(0f, 1.25f, 0.32f), new Vector3(0.36f, 0.30f, 0.03f), new Color(0.75f, 0.30f, 0.28f));
                Box(house, "MailFlag", mb + new Vector3(0.24f, 1.42f, -0.1f), new Vector3(0.04f, 0.32f, 0.16f), new Color(0.98f, 0.82f, 0.25f));
            }
            else
            {
                Bed(house, new Vector3(-W * 0.5f + 0.4f, 0f, fz + 1.0f)); Bed(house, new Vector3(W * 0.5f - 0.4f, 0f, fz + 1.0f));
            }
            BuildingOutline.Attach(house, 0.024f);
            // 146차: 접지 그늘(건물 발치 타원) — 종이 인형처럼 떠 보이지 않게
            GroundBlob.Static(house, house.position, (W + 1.6f) * scale, (D + 1.6f) * scale, 0.24f);
            var bc = house.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, H * 0.5f, 0f); bc.size = new Vector3(W + 0.4f, H, D + 0.4f);
            return house;
        }

        /// 기와 줄: 경사면(용마루 x축, 앞·뒤 ±z)을 따라 반원통을 겹쳐 놓는다.
        /// 149차(사용자: 「지붕이 직선으로 되어 있다」): 시안처럼 처마→용마루로 흐르는 둥근 기와 골(세로 원기둥) + 가로 단(얇은 입술).
        /// 원기둥 축이 경사면을 따라 눕고, 처마 끝이 둥글게 물결친다. 밝기는 골마다 교차.
        static void TileRows(Transform house, float w, float d, float h, float y0, Color roof)
        {
            float hd = d * 0.5f, slope = Mathf.Sqrt(hd * hd + h * h);
            var light = Color.Lerp(roof, Color.white, 0.07f); var dark = Color.Lerp(roof, Color.black, 0.05f);   // 격자로 읽히지 않게 대비는 약하게(둥근 음영은 라이팅이 만든다)
            var lip = Color.Lerp(roof, Color.black, 0.10f);
            float pitch = 0.30f; int cols = Mathf.Max(6, Mathf.RoundToInt(w / pitch));
            int rows = Mathf.Max(4, Mathf.RoundToInt(slope / 0.40f));
            for (int side = -1; side <= 1; side += 2)
            {
                var dirUp = new Vector3(0f, h, -side * hd).normalized;           // 처마 → 용마루
                var normal = new Vector3(0f, hd, side * h).normalized;           // 경사면 법선
                var center = new Vector3(0f, y0 + h * 0.5f, side * hd * 0.5f) + normal * 0.09f;
                // 세로 골(barrel)
                for (int c = 0; c < cols; c++)
                {
                    float x = -w * 0.5f + (c + 0.5f) * (w / cols);
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(g.GetComponent<Collider>()); g.name = "Tile";
                    g.transform.SetParent(house, false);
                    g.transform.localPosition = center + new Vector3(x, 0f, 0f) - dirUp * 0.10f;
                    g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dirUp);
                    g.transform.localScale = new Vector3(pitch * 0.98f, slope * 0.5f + 0.12f, 0.22f);
                    g.GetComponent<Renderer>().sharedMaterial = M(c % 2 == 0 ? light : dark);
                }
                // 가로 단(row) — 얇은 입술이 경사면 위에 가로로 누움
                for (int r = 1; r < rows; r++)
                {
                    float t = r / (float)rows;
                    var pos = new Vector3(0f, y0 + t * h, side * (hd - t * hd)) + normal * 0.21f;
                    var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(b.GetComponent<Collider>()); b.name = "TileRow";
                    b.transform.SetParent(house, false); b.transform.localPosition = pos;
                    b.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
                    b.transform.localScale = new Vector3(w + 0.04f, 0.035f, 0.07f);
                    b.GetComponent<Renderer>().sharedMaterial = M(lip);
                }
            }
        }

        /// 149차(사용자: 「나무의 텍스처가 틀리다」): 시안식 야자수 — 마디가 진 줄기(고리 원기둥 쌓기, 살짝 휨) + 코코넛 + 잎 7장.
        public static Transform Palm(Transform root, Vector3 ground, float yaw, float height)
        {
            var t = new GameObject("Tree_Palm").transform; t.SetParent(root, false);
            t.position = ground; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var barkA = new Color(0.62f, 0.44f, 0.28f); var barkB = new Color(0.50f, 0.34f, 0.21f); var barkRing = new Color(0.40f, 0.27f, 0.16f);
            int segs = Mathf.Max(8, Mathf.RoundToInt(height / 0.42f)); float segH = height / segs;
            float lean = 0.10f;   // 살짝 휨(위로 갈수록 +x 로)
            Vector3 top = Vector3.zero;
            for (int i = 0; i < segs; i++)
            {
                float k = i / (float)(segs - 1);
                float r = Mathf.Lerp(0.24f, 0.15f, k);
                var pos = new Vector3(lean * height * k * k, (i + 0.5f) * segH, 0f);
                Cyl(t, "Seg", pos, new Vector3(r * 2f, segH * 0.5f, r * 2f), i % 2 == 0 ? barkA : barkB);
                // 마디 고리(살짝 굵고 어둡게)
                Cyl(t, "Ring", pos + new Vector3(0f, segH * 0.5f - 0.03f, 0f), new Vector3(r * 2.25f, 0.035f, r * 2.25f), barkRing);
                top = pos + new Vector3(0f, segH * 0.5f, 0f);
            }
            // 코코넛
            var coco = new Color(0.45f, 0.30f, 0.16f);
            for (int i = 0; i < 3; i++) Ball(t, "Coconut", top + Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0.18f, -0.12f, 0f), Vector3.one * 0.26f, coco);
            // 잎 7장: 위쪽으로 뻗다 끝이 처지는 두 마디 + 잎맥
            var leafA = new Color(0.36f, 0.66f, 0.30f); var leafB = new Color(0.46f, 0.76f, 0.36f); var vein = new Color(0.30f, 0.52f, 0.24f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * (360f / 7f) + 11f; var rot = Quaternion.Euler(0f, a, 0f);
                var col = i % 2 == 0 ? leafA : leafB;
                // 1마디: 위로 25°
                float L = Mathf.Clamp(height * 0.30f, 0.9f, 1.6f);   // 잎 한 마디 길이 — 줄기 높이에 비례(작은 야자수는 작은 잎)
                var l1 = Ball(t, "Leaf", top + rot * new Vector3(L * 0.5f, 0.16f, 0f), new Vector3(L, 0.08f, L * 0.30f), col);
                l1.transform.localRotation = rot * Quaternion.Euler(0f, 0f, 20f);
                // 2마디: 끝이 처짐 −34°
                var l2 = Ball(t, "Leaf", top + rot * new Vector3(L * 1.25f, 0.08f, 0f), new Vector3(L * 0.95f, 0.07f, L * 0.24f), col);
                l2.transform.localRotation = rot * Quaternion.Euler(0f, 0f, -34f);
                // 잎맥(가는 막대)
                var v = Box(t, "Vein", top + rot * new Vector3(L * 0.55f, 0.19f, 0f), new Vector3(L * 1.05f, 0.035f, 0.045f), vein);
                v.transform.localRotation = rot * Quaternion.Euler(0f, 0f, 20f);
            }
            foreach (var c in t.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, height * 0.5f, 0f); bc.size = new Vector3(0.55f, height, 0.55f);
            BuildingOutline.Attach(t, 0.02f);
            return t;
        }

        // ── 153차: 언덕 채우기 — 소나무·풍차·울타리 ──────────────────────
        static Mesh _cone;
        static Mesh ConeMesh()
        {
            if (_cone != null) return _cone;
            int n = 16; var v = new List<Vector3>(); var tr = new List<int>(); var nm = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)); var top = new Vector3(0f, 1f, 0f);
                var fn = Vector3.Cross(p1 - p0, top - p0).normalized;
                int b = v.Count; v.Add(p0); v.Add(top); v.Add(p1); nm.Add(fn); nm.Add(fn); nm.Add(fn); tr.Add(b); tr.Add(b + 1); tr.Add(b + 2);
                // 바닥
                int c = v.Count; v.Add(p0); v.Add(p1); v.Add(Vector3.zero); nm.Add(Vector3.down); nm.Add(Vector3.down); nm.Add(Vector3.down); tr.Add(c); tr.Add(c + 1); tr.Add(c + 2);
            }
            _cone = new Mesh { name = "Cone" }; _cone.SetVertices(v); _cone.SetNormals(nm); _cone.SetTriangles(tr, 0); _cone.RecalculateBounds();
            return _cone;
        }
        /// 소나무: 줄기 + 원뿔 3단(아래로 갈수록 넓고, 색은 위가 밝게)
        public static Transform Pine(Transform root, Vector3 ground, float yaw, float height)
        {
            var t = new GameObject("Tree_Pine").transform; t.SetParent(root, false); t.position = ground; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            Cyl(t, "Trunk", new Vector3(0f, height * 0.22f, 0f), new Vector3(0.28f, height * 0.22f, 0.28f), new Color(0.50f, 0.34f, 0.22f));
            var cols = new[] { new Color(0.30f, 0.56f, 0.34f), new Color(0.36f, 0.64f, 0.38f), new Color(0.44f, 0.72f, 0.42f) };
            for (int i = 0; i < 3; i++)
            {
                float y0 = height * (0.30f + i * 0.22f), r = height * (0.34f - i * 0.08f), hh = height * 0.34f;
                var g = new GameObject("Cone" + i, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(t, false);
                g.transform.localPosition = new Vector3(0f, y0, 0f); g.transform.localScale = new Vector3(r, hh, r);
                g.GetComponent<MeshFilter>().sharedMesh = ConeMesh(); g.GetComponent<MeshRenderer>().sharedMaterial = M(cols[i]);
            }
            var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, height * 0.5f, 0f); bc.size = new Vector3(0.6f, height, 0.6f);
            BuildingOutline.Attach(t, 0.02f);
            return t;
        }
        /// 풍차: 기둥 + 허브 + 날개 4(천천히 돎)
        public static Transform Windmill(Transform root, Vector3 ground, float yaw, float height)
        {
            var t = new GameObject("Windmill").transform; t.SetParent(root, false); t.position = ground; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var post = Color.Lerp(VillagePalette.Log, Color.white, 0.2f);
            Cyl(t, "Post", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.22f, height * 0.5f, 0.22f), post);
            for (int i = 0; i < 4; i++) Box(t, "Brace", new Vector3(Mathf.Cos(i * 1.5708f) * 0.35f, height * 0.18f, Mathf.Sin(i * 1.5708f) * 0.35f), new Vector3(0.08f, height * 0.36f, 0.08f), Color.Lerp(post, Color.black, 0.2f));
            var hub = new GameObject("Hub").transform; hub.SetParent(t, false); hub.localPosition = new Vector3(0f, height, 0.32f);
            Ball(hub, "Cap", Vector3.zero, Vector3.one * 0.32f, new Color(0.85f, 0.35f, 0.35f));
            for (int i = 0; i < 4; i++)
            {
                var b = Box(hub, "Blade", Quaternion.Euler(0f, 0f, i * 90f) * new Vector3(0f, 0.9f, 0f), new Vector3(0.36f, 1.7f, 0.04f), i % 2 == 0 ? new Color(0.98f, 0.96f, 0.90f) : new Color(0.95f, 0.78f, 0.45f));
                b.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            }
            hub.gameObject.AddComponent<Spin>().speed = 28f;
            var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, height * 0.5f, 0f); bc.size = new Vector3(0.9f, height, 0.9f);
            BuildingOutline.Attach(t, 0.02f);
            return t;
        }
        public class Spin : MonoBehaviour { public float speed = 30f; void Update() { transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self); } }
        /// 통나무 울타리 한 줄(두 점 사이) — 말뚝 + 가로대 2, 지면 높이 따라감
        public static void LogFence(Transform root, Vector3 a, Vector3 b, float step = 1.6f)
        {
            var t = new GameObject("LogFence").transform; t.SetParent(root, false);
            var log = VillagePalette.Log; var logD = VillagePalette.LogDark;
            var d = b - a; d.y = 0f; float len = d.magnitude; var dir = d / Mathf.Max(0.01f, len); int n = Mathf.Max(1, Mathf.RoundToInt(len / step));
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= n; i++)
            {
                var p = a + dir * (len * i / n); p.y = VillageWorld.Height(p.x, p.z);
                var post = Cyl(t, "Post", Vector3.zero, new Vector3(0.18f, 0.42f, 0.18f), logD); post.transform.position = p + new Vector3(0f, 0.42f, 0f);
                if (i > 0)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        var m = (prev + p) * 0.5f + new Vector3(0f, 0.30f + k * 0.32f, 0f); var seg = p - prev; float sl = seg.magnitude;
                        var rail = Cyl(t, "Rail", Vector3.zero, new Vector3(0.11f, sl * 0.5f, 0.11f), log); rail.transform.position = m; rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, seg.normalized);
                    }
                    // 154차: 울타리도 막힘
                    var cgo = new GameObject("FenceCol", typeof(BoxCollider)); cgo.transform.SetParent(t, false); cgo.transform.position = (prev + p) * 0.5f + new Vector3(0f, 0.5f, 0f);
                    cgo.transform.rotation = Quaternion.LookRotation(new Vector3(p.x - prev.x, 0f, p.z - prev.z).normalized, Vector3.up); cgo.GetComponent<BoxCollider>().size = new Vector3(0.3f, 1f, Vector3.Distance(prev, p));
                }
                prev = p;
            }
            BuildingOutline.Attach(t, 0.02f);
        }

        /// 148차: 집 밖 소품(징검돌·울타리·화단·간판)은 집 발치가 아니라 그 자리 지면에 놓는다 — 언덕 위 우리집 앞이 경사라 공중에 떠 보이던 문제.
        static Vector3 Snap(Transform house, Vector3 p)
        {
            var w = house.TransformPoint(new Vector3(p.x, 0f, p.z));
            float gy = house.InverseTransformPoint(new Vector3(w.x, VillageWorld.Height(w.x, w.z), w.z)).y;
            return new Vector3(p.x, p.y + gy, p.z);
        }
        static void Fence(Transform house, float x0, float x1, float z, bool gate)
        {
            for (float x = x0; x <= x1 + 0.01f; x += 1.2f)
            {
                if (gate && Mathf.Abs(x) < 1.3f) continue;
                Fence1(house, Snap(house, new Vector3(x, 0f, z)), 0f);
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


        /// 157차(사용자: 「집 위에 간판으로 우리집·알바나라·마을상점」): 문 위 벽에 거는 나무 간판 + 글자(앞에서 읽힘).
        static void WallSign(Transform house, string ko, string en, float W, float H, float fz, Color board, Color dark)
        {
            float sw = Mathf.Min(W * 0.36f, 2.2f), sy = H - 0.45f;
            Box(house, "WallSignEdge", new Vector3(0f, sy, fz + 0.10f), new Vector3(sw + 0.14f, 0.86f, 0.10f), dark);
            Box(house, "WallSign", new Vector3(0f, sy, fz + 0.13f), new Vector3(sw, 0.72f, 0.12f), board);
            Box(house, "WallSignInner", new Vector3(0f, sy, fz + 0.195f), new Vector3(sw - 0.16f, 0.56f, 0.02f), Color.Lerp(board, Color.white, 0.18f));
            for (int s = -1; s <= 1; s += 2)
            {
                Box(house, "SignBracket", new Vector3(s * (sw * 0.5f - 0.10f), sy + 0.58f, fz + 0.08f), new Vector3(0.07f, 0.45f, 0.07f), dark);
                Ball(house, "SignNail", new Vector3(s * (sw * 0.5f - 0.10f), sy + 0.30f, fz + 0.20f), Vector3.one * 0.09f, new Color(0.95f, 0.85f, 0.45f));
            }
            var go = new GameObject("WallSignText", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(house, false); go.transform.localPosition = new Vector3(0f, sy, fz + 0.215f); go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.sortingOrder = 6;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(sw * 100f, 60f); rt.localScale = Vector3.one * 0.01f;
            var t = CoastHudLayout.MakeText(rt, "T", Loc.T(ko, en), 40, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            t.color = new Color(1f, 0.98f, 0.92f); t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(40);
            CoastUiArt.OutlineText(t, new Color(0.30f, 0.16f, 0.08f, 0.95f), 2.5f);
        }

        /// 간판: 나무 기둥 + 널판 + 월드 캔버스 글자(한/영).
        public static void Sign(Transform parent, Vector3 p, string ko, string en, Color board)
        {
            Cyl(parent, "SignPost", p + new Vector3(0f, 0.9f, 0f), new Vector3(0.14f, 0.9f, 0.14f), new Color(0.48f, 0.33f, 0.20f));
            var b = Box(parent, "SignBoard", p + new Vector3(0f, 1.75f, 0f), new Vector3(2.1f, 0.62f, 0.12f), board);
            Box(parent, "SignEdge", p + new Vector3(0f, 1.75f, -0.02f), new Vector3(2.22f, 0.74f, 0.10f), Color.Lerp(board, Color.black, 0.35f));
            var go = new GameObject("SignText", typeof(RectTransform), typeof(Canvas));
            // 147차: UI 캔버스의 읽히는 면은 -Z 쪽 → +Z(앞) 에 두는 판은 180° 돌려야 앞에서 바로 읽힌다(거울 글자 수정)
            go.transform.SetParent(parent, false); go.transform.localPosition = p + new Vector3(0f, 1.75f, 0.075f); go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.sortingOrder = 5;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(200f, 60f); rt.localScale = Vector3.one * 0.01f;
            var t = CoastHudLayout.MakeText(rt, "T", Loc.T(ko, en), 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = CoastHudLayout.Scaled(30);
            CoastUiArt.OutlineText(t, new Color(0.25f, 0.15f, 0.10f, 0.9f), 2f);
            // 뒷면에서도 읽히게 뒤집은 복사본
            var back = Object.Instantiate(go, parent); back.transform.localPosition = p + new Vector3(0f, 1.75f, -0.075f); back.transform.localRotation = Quaternion.identity;
        }
    }

    /// 157차: 굴뚝 연기 — 둥근 퍼프가 천천히 올라가며 커지고 사라진다(동물의 숲 아침 굴뚝).
    public class ChimneySmoke : MonoBehaviour
    {
        class Puff { public Transform t; public Material m; public float age, life; public Vector3 vel; }
        readonly System.Collections.Generic.List<Puff> _puffs = new System.Collections.Generic.List<Puff>();
        float _next; public float Rate = 0.75f;
        void Update()
        {
            _next -= Time.deltaTime;
            if (_next <= 0f && _puffs.Count < 10)
            {
                _next = Rate + Random.value * 0.3f;
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(g.GetComponent<Collider>()); g.name = "Smoke";
                g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(Random.value * 0.2f - 0.1f, 0f, Random.value * 0.2f - 0.1f);
                float s = 0.35f + Random.value * 0.15f; g.transform.localScale = Vector3.one * s;
                var m = CoastMaterials.CreateTransparent(new Color(1f, 1f, 1f, 0.55f)); g.GetComponent<Renderer>().sharedMaterial = m;
                _puffs.Add(new Puff { t = g.transform, m = m, life = 2.8f + Random.value * 0.8f, vel = new Vector3(Random.value * 0.3f - 0.15f, 0.9f + Random.value * 0.3f, Random.value * 0.3f - 0.15f) });
            }
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var p = _puffs[i]; p.age += Time.deltaTime; float k = p.age / p.life;
                if (k >= 1f) { Object.Destroy(p.t.gameObject); _puffs.RemoveAt(i); continue; }
                p.t.localPosition += (p.vel + new Vector3(Mathf.Sin(Time.time * 1.3f + i) * 0.25f, 0f, 0f)) * Time.deltaTime;
                p.t.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.35f, k);
                var c = new Color(1f, 1f, 1f, 0.55f * (1f - k) * Mathf.Clamp01(k * 6f));
                if (p.m.HasProperty("_BaseColor")) p.m.SetColor("_BaseColor", c); if (p.m.HasProperty("_Color")) p.m.SetColor("_Color", c);
            }
        }
    }
}
