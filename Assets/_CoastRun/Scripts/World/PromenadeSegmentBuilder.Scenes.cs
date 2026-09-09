using UnityEngine;

namespace CoastRun
{
    /// 35차: 챕터별 장면 배합(SceneMix)에 따른 좌/우 빌더 — 숲길·들판(유채/메밀/억새/귤밭/초원)·현무암 바위·해변·오름 언덕·풍력 발전기·무지개 방호벽·오른쪽 상가.
    /// 전부 프로시저럴(박스·구·킷 프리팹) — 그림이 없어도 실루엣과 색으로 읽히게. 좌표는 타일 로컬(x: − 왼쪽 / + 오른쪽, z: 0~30).
    public static partial class PromenadeSegmentBuilder
    {
        // ── 공용 재료 ─────────────────────────────────────────────────────
        private static Material _mat(Color c, float s = 0.05f) => CoastMaterials.CreateLit(c, s);
        private static readonly Color Moss = new Color(0.36f, 0.52f, 0.28f);
        private static readonly Color Basalt = new Color(0.20f, 0.20f, 0.22f);
        private static readonly Color Sand = new Color(0.93f, 0.86f, 0.68f);
        private static readonly Color GrassGreen = new Color(0.50f, 0.70f, 0.34f);

        private static Color SeasonGrass()
        {
            switch (SeasonLook.Current)
            {
                case SeasonKind.Spring: return new Color(0.56f, 0.76f, 0.38f);
                case SeasonKind.Autumn: return new Color(0.72f, 0.62f, 0.32f);
                case SeasonKind.Winter: return new Color(0.86f, 0.88f, 0.90f);   // 눈 덮인 들
                default: return GrassGreen;
            }
        }

        private static string TreePrefab(ForestKind kind, System.Random rng)
        {
            var s = SeasonLook.Current;
            switch (kind)
            {
                case ForestKind.Cedar: return "Prop_Pine";
                case ForestKind.Camellia: return rng.Next(3) == 0 ? "Prop_Pine" : "Prop_CherryTree";
                case ForestKind.Broadleaf: return s == SeasonKind.Autumn ? "Prop_Maple" : (s == SeasonKind.Spring ? "Prop_CherryTree" : (rng.Next(2) == 0 ? "Prop_Maple" : "Prop_OrangeTree"));
                default: return rng.Next(4) == 0 ? "Prop_Palm" : "Prop_Pine";
            }
        }

        /// 킷 나무가 없을 때: 원뿔(잎) + 기둥.
        private static void ProceduralTree(Transform parent, Vector3 pos, float h, Color leaf, System.Random rng)
        {
            var trunk = CreateBox(parent, "Trunk", pos + new Vector3(0f, h * 0.22f, 0f), new Vector3(0.18f, h * 0.44f, 0.18f), () => new Color(0.36f, 0.24f, 0.14f));
            var crown = CreateCapsule(parent, "Crown", pos + new Vector3(0f, h * 0.62f, 0f), h * 0.28f, h * 0.9f, () => leaf);
            crown.transform.localRotation = Quaternion.identity;
        }

        private static void KerbWall(Transform root, int side, System.Random rng, float density = 6.5f)
        {
            float x = side * (RoadHalfWidth + 1.15f);
            for (float z = 2f; z < Length - 1f; z += density)
            {
                if (JejuKit.Spawn("Prop_StoneWall", root, new Vector3(x, 0f, z + (float)rng.NextDouble() * 0.8f), 0f, 0.55f) == null)
                    CreateBox(root, "Kerb", new Vector3(x, 0.3f, z + 2.5f), new Vector3(0.4f, 0.6f, 5f), () => Basalt);
            }
        }

        // ── 숲 ────────────────────────────────────────────────────────────
        private static void BuildForestSide(Transform root, int index, int side, ForestKind kind)
        {
            var rng = new System.Random(index * 4111 + side * 17 + 5);
            float inner = RoadHalfWidth + 1.4f;
            // 바닥: 이끼·낙엽 톤
            Color floor = kind == ForestKind.Camellia ? new Color(0.30f, 0.42f, 0.24f) : (SeasonLook.Current == SeasonKind.Autumn ? new Color(0.48f, 0.36f, 0.20f) : new Color(0.28f, 0.40f, 0.22f));
            if (SeasonLook.Current == SeasonKind.Winter) floor = new Color(0.82f, 0.85f, 0.88f);
            CreateBox(root, "ForestFloor", new Vector3(side * (inner + 6f), -0.06f, Length * 0.5f), new Vector3(12f, 0.1f, Length), () => floor);
            KerbWall(root, side, rng, 9f);
            // 나무: 두 줄(앞줄 큰 나무 촘촘, 뒷줄 더 큰 나무) — 삼나무 숲길의 '터널' 느낌
            Color leaf = kind == ForestKind.Camellia ? new Color(0.16f, 0.38f, 0.20f) : new Color(0.20f, 0.42f, 0.24f);
            for (int i = 0; i < 8; i++)
            {
                float z = 1.5f + i * 3.8f + (float)rng.NextDouble() * 1.2f;
                float x = side * (inner + 0.9f + (float)rng.NextDouble() * 1.6f);
                float sc = 1.15f + (float)rng.NextDouble() * 0.5f;
                if (JejuKit.Spawn(TreePrefab(kind, rng), root, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, sc) == null)
                    ProceduralTree(root, new Vector3(x, 0f, z), 5f * sc, leaf, rng);
            }
            for (int i = 0; i < 5; i++)
            {
                float z = 3f + i * 6f + (float)rng.NextDouble() * 2f;
                float x = side * (inner + 3.8f + (float)rng.NextDouble() * 3f);
                float sc = 1.5f + (float)rng.NextDouble() * 0.6f;
                if (JejuKit.Spawn(TreePrefab(kind, rng), root, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, sc) == null)
                    ProceduralTree(root, new Vector3(x, 0f, z), 7f * sc, leaf, rng);
            }
            // 동백: 붉은 꽃 점(작은 구) / 곶자왈: 이끼 바위
            if (kind == ForestKind.Camellia)
                for (int i = 0; i < 14; i++)
                    CreateSphere(root, "Camellia", new Vector3(side * (inner + 1.2f + (float)rng.NextDouble() * 3f), 1.4f + (float)rng.NextDouble() * 2.2f, (float)rng.NextDouble() * Length), 0.14f, () => new Color(0.85f, 0.12f, 0.20f));
            for (int i = 0; i < 4; i++)
            {
                var r = CreateSphere(root, "MossRock", new Vector3(side * (inner + 0.5f + (float)rng.NextDouble() * 1.5f), 0.1f, (float)rng.NextDouble() * Length), 0.45f + (float)rng.NextDouble() * 0.4f, () => Color.Lerp(Basalt, Moss, 0.55f));
                r.transform.localScale = new Vector3(r.transform.localScale.x * 1.3f, r.transform.localScale.y * 0.6f, r.transform.localScale.z);
            }
        }

        // ── 들판 ──────────────────────────────────────────────────────────
        private static void BuildFieldSide(Transform root, int index, int side, FieldKind kind)
        {
            var rng = new System.Random(index * 5227 + side * 23 + 9);
            float inner = RoadHalfWidth + 1.3f;
            Color ground = SeasonGrass();
            Color flower = Color.clear;
            switch (kind)
            {
                case FieldKind.Canola: ground = SeasonLook.Current == SeasonKind.Winter ? ground : new Color(0.62f, 0.72f, 0.30f); flower = new Color(1f, 0.86f, 0.18f); break;
                case FieldKind.Buckwheat: ground = new Color(0.55f, 0.66f, 0.36f); flower = new Color(0.98f, 0.96f, 0.92f); break;
                case FieldKind.SilverGrass: ground = new Color(0.66f, 0.60f, 0.40f); flower = new Color(0.90f, 0.85f, 0.70f); break;
                case FieldKind.Orchard: ground = new Color(0.46f, 0.64f, 0.30f); break;
            }
            CreateBox(root, "Field", new Vector3(side * (inner + 7f), -0.06f, Length * 0.5f), new Vector3(14f, 0.1f, Length), () => ground);
            KerbWall(root, side, rng, 7f);
            bool snow = SeasonLook.Current == SeasonKind.Winter;
            if (kind == FieldKind.Orchard)
            {
                for (int i = 0; i < 6; i++)
                {
                    float z = 2f + i * 4.8f + (float)rng.NextDouble() * 1.5f;
                    float x = side * (inner + 1.2f + (float)rng.NextDouble() * 5f);
                    if (JejuKit.Spawn("Prop_OrangeTree", root, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.4f) == null)
                        ProceduralTree(root, new Vector3(x, 0f, z), 3f, new Color(0.25f, 0.5f, 0.25f), rng);
                }
                if (rng.Next(2) == 0) JejuKit.Spawn("Prop_OrangeStall", root, new Vector3(side * (inner + 0.6f), 0f, 12f + (float)rng.NextDouble() * 8f), side < 0 ? 180f : 0f);
            }
            else if (flower != Color.clear && !snow)
            {
                // 꽃/이삭: 줄 맞춘 작은 박스 (유채 노랑 · 메밀 흰색 · 억새 베이지) — 멀리서 색 띠로 읽힌다
                int rows = kind == FieldKind.SilverGrass ? 5 : 6;
                for (int r = 0; r < rows; r++)
                    for (float z = 0.8f; z < Length; z += kind == FieldKind.SilverGrass ? 0.9f : 1.1f)
                    {
                        float x = side * (inner + 0.7f + r * 1.25f + (float)rng.NextDouble() * 0.5f);
                        float h = kind == FieldKind.SilverGrass ? 1.1f + (float)rng.NextDouble() * 0.5f : 0.45f + (float)rng.NextDouble() * 0.25f;
                        Vector3 p = new Vector3(x, h * 0.5f, z + (float)rng.NextDouble() * 0.5f);
                        if (kind == FieldKind.SilverGrass)
                        {
                            CreateBox(root, "Reed", p, new Vector3(0.06f, h, 0.06f), () => new Color(0.62f, 0.52f, 0.32f));
                            CreateCapsule(root, "Plume", p + new Vector3(0f, h * 0.5f + 0.15f, 0f), 0.09f, 0.5f, () => flower);
                        }
                        else
                        {
                            CreateBox(root, "Stem", p, new Vector3(0.05f, h, 0.05f), () => new Color(0.35f, 0.55f, 0.25f));
                            CreateBox(root, "Bloom", p + new Vector3(0f, h * 0.5f + 0.08f, 0f), new Vector3(0.28f, 0.16f, 0.28f), () => flower);
                        }
                    }
            }
            else
            {
                // 초원: 풀 뭉치 + 돌담 + 가끔 방사탑/돌하르방
                for (int i = 0; i < 10; i++)
                    CreateCapsule(root, "Tuft", new Vector3(side * (inner + 0.6f + (float)rng.NextDouble() * 6f), 0.15f, (float)rng.NextDouble() * Length), 0.3f, 0.5f, () => Color.Lerp(ground, Color.black, 0.15f));
            }
            if (rng.Next(3) == 0) JejuKit.Spawn("Prop_Hareubang", root, new Vector3(side * (inner + 0.8f), 0f, 6f + (float)rng.NextDouble() * 18f), side < 0 ? 180f : 0f, 0.8f);
            if (rng.Next(4) == 0)
            {
                // 방사탑(현무암 돌탑)
                float bz = 4f + (float)rng.NextDouble() * 20f; float bx = side * (inner + 2.5f);
                for (int k = 0; k < 5; k++) CreateBox(root, "Bangsatap", new Vector3(bx, 0.3f + k * 0.55f, bz), new Vector3(1.8f - k * 0.25f, 0.55f, 1.8f - k * 0.25f), () => Basalt);
            }
        }

        // ── 현무암 바위밭 ──────────────────────────────────────────────────
        private static void BuildRockSide(Transform root, int index, int side)
        {
            var rng = new System.Random(index * 6151 + side * 31 + 13);
            float inner = RoadHalfWidth + 1.2f;
            CreateBox(root, "RockGround", new Vector3(side * (inner + 6f), -0.08f, Length * 0.5f), new Vector3(12f, 0.1f, Length), () => Color.Lerp(Basalt, Moss, 0.25f));
            for (int i = 0; i < 16; i++)
            {
                float x = side * (inner + 0.3f + (float)rng.NextDouble() * 7f);
                float z = (float)rng.NextDouble() * Length;
                float r = 0.35f + (float)rng.NextDouble() * 0.9f;
                var rock = CreateSphere(root, "Basalt", new Vector3(x, r * 0.35f, z), r, () => Color.Lerp(Basalt, Color.black, (float)rng.NextDouble() * 0.3f));
                rock.transform.localScale = new Vector3(r * 2f * (1f + (float)rng.NextDouble() * 0.8f), r * 1.2f, r * 2f * (1f + (float)rng.NextDouble() * 0.5f));
                rock.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
            for (int i = 0; i < 6; i++)
                CreateBox(root, "MossPatch", new Vector3(side * (inner + 1f + (float)rng.NextDouble() * 5f), 0.0f, (float)rng.NextDouble() * Length), new Vector3(1.5f + (float)rng.NextDouble() * 2f, 0.05f, 1.2f + (float)rng.NextDouble() * 2f), () => Moss);
            if (rng.Next(2) == 0) JejuKit.Spawn("Prop_Buoy", root, new Vector3(side * (inner + 1.5f), 0.2f, 8f + (float)rng.NextDouble() * 14f), 0f, 0.9f);
        }

        // ── 해변(오른쪽) ───────────────────────────────────────────────────
        private static void BuildBeachSide(Transform root, int index)
        {
            var rng = new System.Random(index * 7331 + 41);
            float railX = RoadHalfWidth + 0.9f;
            // 낮은 연석만, 난간 없이 모래로 이어진다
            CreateBox(root, "BeachKerb", new Vector3(railX, 0.12f, Length * 0.5f), new Vector3(0.4f, 0.24f, Length), () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.4f));
            CreateBox(root, "Sand", new Vector3(railX + 6f, -0.22f, Length * 0.5f), new Vector3(12f, 0.12f, Length), () => Sand);
            CreateBox(root, "SandWet", new Vector3(railX + 12.5f, -0.30f, Length * 0.5f), new Vector3(3f, 0.06f, Length), () => Color.Lerp(Sand, CoastPalette.SeaTeal, 0.35f));
            // 파라솔·부표·바위·야자
            for (int i = 0; i < 2; i++)
            {
                float z = 5f + i * 14f + (float)rng.NextDouble() * 6f;
                if (rng.Next(2) == 0) JejuKit.Spawn("Prop_CafeUmbrella", root, new Vector3(railX + 3f + (float)rng.NextDouble() * 3f, -0.15f, z), (float)rng.NextDouble() * 360f, 1.0f);
                else JejuKit.Spawn("Prop_Palm", root, new Vector3(railX + 2.2f + (float)rng.NextDouble() * 2f, -0.15f, z), (float)rng.NextDouble() * 360f, 1.0f + (float)rng.NextDouble() * 0.3f);
            }
            for (int i = 0; i < 5; i++)
            {
                float r = 0.3f + (float)rng.NextDouble() * 0.6f;
                var rock = CreateSphere(root, "BeachRock", new Vector3(railX + 7f + (float)rng.NextDouble() * 6f, -0.2f + r * 0.3f, (float)rng.NextDouble() * Length), r, () => Basalt);
                rock.transform.localScale = new Vector3(r * 2.4f, r * 1.1f, r * 2f);
            }
            if (rng.Next(2) == 0) JejuKit.Spawn("Prop_Buoy", root, new Vector3(railX + 9f, -0.1f, 10f + (float)rng.NextDouble() * 10f), 0f, 0.9f);
            // 비치 의자 두 개(색 의자 사진)
            Color[] chair = { new Color(0.95f, 0.35f, 0.35f), new Color(0.35f, 0.55f, 0.95f), new Color(0.98f, 0.8f, 0.25f), new Color(0.4f, 0.8f, 0.5f) };
            for (int i = 0; i < 2; i++)
            {
                var c = chair[rng.Next(chair.Length)]; float z = 8f + i * 9f + (float)rng.NextDouble() * 4f; float x = railX + 1.8f;
                CreateBox(root, "ChairSeat", new Vector3(x, 0.25f, z), new Vector3(0.5f, 0.06f, 0.5f), () => c);
                CreateBox(root, "ChairBack", new Vector3(x - 0.22f, 0.55f, z), new Vector3(0.06f, 0.6f, 0.5f), () => c);
            }
        }

        // ── 오름 언덕(오른쪽) ─────────────────────────────────────────────
        private static void BuildHillSide(Transform root, int index)
        {
            var rng = new System.Random(index * 8231 + 47);
            float railX = RoadHalfWidth + 0.9f;
            KerbWall(root, +1, rng, 7f);
            Color g = SeasonGrass();
            // 완만히 올라가는 초록 경사(박스를 기울여) + 뒤에 더 높은 둔덕
            var slope = CreateBox(root, "HillSlope", new Vector3(railX + 6.5f, 1.4f, Length * 0.5f), new Vector3(15f, 0.8f, Length + 2f), () => g);
            slope.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            var back = CreateBox(root, "HillBack", new Vector3(railX + 15f, 3.6f, Length * 0.5f), new Vector3(10f, 4f, Length + 2f), () => Color.Lerp(g, CoastPalette.SkyBlue, 0.25f));
            back.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            for (int i = 0; i < 5; i++)
            {
                float z = 2f + i * 6f + (float)rng.NextDouble() * 2f; float x = railX + 2f + (float)rng.NextDouble() * 6f;
                float y = (x - railX) * 0.25f;
                if (rng.Next(3) == 0) { if (JejuKit.Spawn("Prop_Pine", root, new Vector3(x, y, z), (float)rng.NextDouble() * 360f, 0.9f) == null) ProceduralTree(root, new Vector3(x, y, z), 4f, new Color(0.2f, 0.42f, 0.24f), rng); }
                else CreateCapsule(root, "Tuft", new Vector3(x, y + 0.15f, z), 0.35f, 0.5f, () => Color.Lerp(g, Color.black, 0.15f));
            }
            // 억새/유채 점(계절)
            if (SeasonLook.Current == SeasonKind.Spring)
                for (int i = 0; i < 20; i++) { float x = railX + 1.5f + (float)rng.NextDouble() * 7f; CreateBox(root, "Bloom", new Vector3(x, (x - railX) * 0.25f + 0.3f, (float)rng.NextDouble() * Length), new Vector3(0.3f, 0.15f, 0.3f), () => new Color(1f, 0.85f, 0.2f)); }
            if (rng.Next(3) == 0)
            {
                float bx = railX + 4f; float bz = 6f + (float)rng.NextDouble() * 18f; float by = (bx - railX) * 0.25f;
                for (int k = 0; k < 5; k++) CreateBox(root, "Bangsatap", new Vector3(bx, by + 0.3f + k * 0.55f, bz), new Vector3(1.8f - k * 0.25f, 0.55f, 1.8f - k * 0.25f), () => Basalt);
            }
        }

        // ── 풍력 발전기(바다 위) ───────────────────────────────────────────
        private static void AddTurbines(Transform root, int index)
        {
            var rng = new System.Random(index * 9377 + 53);
            float railX = RoadHalfWidth + 0.9f;
            int n = 2 + rng.Next(2);
            for (int i = 0; i < n; i++)
            {
                float x = railX + 12f + (float)rng.NextDouble() * 10f;
                float z = 3f + i * (Length / n) + (float)rng.NextDouble() * 4f;
                float h = 11f + (float)rng.NextDouble() * 4f;
                var pivot = new GameObject("Turbine").transform; pivot.SetParent(root, false); pivot.localPosition = new Vector3(x, -0.4f, z);
                CreateBox(pivot, "Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f), () => new Color(0.85f, 0.7f, 0.25f));
                CreateBox(pivot, "Tower", new Vector3(0f, h * 0.5f, 0f), new Vector3(0.45f, h, 0.45f), () => new Color(0.95f, 0.96f, 0.98f));
                CreateBox(pivot, "Nacelle", new Vector3(0f, h, -0.3f), new Vector3(0.7f, 0.7f, 1.6f), () => new Color(0.92f, 0.93f, 0.95f));
                var hub = new GameObject("Hub").transform; hub.SetParent(pivot, false); hub.localPosition = new Vector3(0f, h, -1.2f);
                for (int b = 0; b < 3; b++)
                {
                    var blade = CreateBox(hub, "Blade", Vector3.zero, new Vector3(0.28f, h * 0.42f, 0.10f), () => new Color(0.97f, 0.97f, 1f));
                    blade.transform.localRotation = Quaternion.Euler(0f, 0f, b * 120f);
                    blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, h * 0.21f, 0f);
                    var tip = CreateBox(blade.transform, "Tip", new Vector3(0f, 0.42f, 0f), new Vector3(1f, 0.16f, 1f), () => new Color(0.9f, 0.3f, 0.2f));
                }
                var spin = hub.gameObject.AddComponent<TurbineSpin>(); spin.speed = 22f + (float)rng.NextDouble() * 14f;
            }
        }

        private class TurbineSpin : MonoBehaviour
        {
            public float speed = 30f;
            private void Update() { transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self); }
        }

        // ── 무지개 방호벽(바다 쪽 벽 대체) ────────────────────────────────
        private static void AddRainbowBlocks(Transform root, int index)
        {
            float railX = RoadHalfWidth + 0.9f;
            Color[] rainbow = { new Color(0.95f, 0.25f, 0.30f), new Color(0.98f, 0.60f, 0.20f), new Color(0.98f, 0.88f, 0.25f), new Color(0.35f, 0.75f, 0.40f), new Color(0.30f, 0.55f, 0.95f), new Color(0.55f, 0.35f, 0.85f) };
            int k = index * 7;
            for (float z = 0f; z < Length; z += 2.5f)
            {
                var c = rainbow[(k++) % rainbow.Length];
                CreateBox(root, "RainbowBlock", new Vector3(railX, 0.5f, z + 1.25f), new Vector3(0.7f, 1.0f, 2.3f), () => c);
            }
            CreateBox(root, "RainbowCap", new Vector3(railX, 1.02f, Length * 0.5f), new Vector3(0.75f, 0.06f, Length), () => Color.Lerp(Color.white, CoastPalette.TownCream, 0.3f));
        }

        // ── 디버그 ────────────────────────────────────────────────────────
        public static string LastSceneLabel { get; private set; }
    }
}
