using UnityEngine;

namespace CoastRun
{
    /// 49차(사용자): 러닝 오른쪽(바다 쪽)이 색 박스 그대로 보였다(밋밋한 초록 경사·평평한 바다·L자 돌담 조각).
    /// 레퍼런스(제주 해안 산책로): 짙은 나무 울타리 → 억새·유채가 무성한 풀 둔덕 → 현무암 돌담 → 검은 바위 해안 + 흰 포말 → 옥빛 바다.
    /// 그림(Kling)·텍스처는 Resources/CoastRun/ 의 Obs_SilverGrass / Obs_Canola / Obs_BasaltPile / Tex_Grass_Meadow / Tex_Shore_Basalt.
    /// 파일이 없으면 색 박스로 폴백(게임은 항상 돈다).
    public static partial class PromenadeSegmentBuilder
    {
        private static Material _grassMat, _shoreMat, _woodMat, _woodDarkMat;

        /// 풀 둔덕·들판·언덕 바닥 — 카툰 풀 텍스처(계절 틴트).
        private static Material GrassMaterial()
        {
            if (_grassMat == null)
            {
                var tex = ArtAssets.LoadTexture("Tex_Grass_Meadow");
                if (tex == null) return null;
                _grassMat = ArtAssets.CreateTexturedLit(tex, Color.white, 0.02f);
            }
            var s = SeasonLook.Current;
            var tint = s == SeasonKind.Winter ? new Color(0.92f, 0.94f, 0.96f)
                     : s == SeasonKind.Autumn ? new Color(1.0f, 0.90f, 0.70f)
                     : s == SeasonKind.Spring ? new Color(0.98f, 1.0f, 0.90f) : Color.white;
            if (_grassMat.HasProperty("_BaseColor")) _grassMat.SetColor("_BaseColor", tint);
            return _grassMat;
        }

        /// 검은 현무암 바위 해안 + 포말 텍스처(바닥).
        private static Material ShoreMaterial()
        {
            if (_shoreMat != null) return _shoreMat;
            var tex = ArtAssets.LoadTexture("Tex_Shore_Basalt");
            if (tex == null) return null;
            _shoreMat = ArtAssets.CreateTexturedLit(tex, Color.white, 0.25f);
            return _shoreMat;
        }

        private static Material WoodMaterial(bool dark)
        {
            if (dark) return _woodDarkMat ??= CoastMaterials.CreateLit(new Color(0.24f, 0.13f, 0.08f), 0.05f);
            return _woodMat ??= CoastMaterials.CreateLit(new Color(0.34f, 0.20f, 0.12f), 0.05f);
        }

        /// 텍스처 바닥판. 텍스처가 없으면 단색.
        private static GameObject TexturedSlab(Transform root, string name, Vector3 pos, Vector3 size, Material mat, System.Func<Color> fallback, float metresPerTile)
        {
            var go = CreateBox(root, name, pos, size, fallback, mat);
            if (mat != null) SetTiling(go, size.x / metresPerTile, size.z / metresPerTile);
            return go;
        }

        /// 짙은 나무 울타리(레퍼런스) — 기둥 + 가로대 2줄. x 는 울타리 선, y0 는 바닥.
        private static void WoodFence(Transform root, float x, float y0 = 0f, float height = 1.05f)
        {
            var rail = WoodMaterial(false);
            var post = WoodMaterial(true);
            CreateBox(root, "FenceRailTop", new Vector3(x, y0 + height, Length * 0.5f), new Vector3(0.09f, 0.11f, Length), () => Color.black, rail);
            CreateBox(root, "FenceRailMid", new Vector3(x, y0 + height * 0.55f, Length * 0.5f), new Vector3(0.08f, 0.10f, Length), () => Color.black, rail);
            for (float z = 1.0f; z < Length; z += 2.5f)
                CreateBox(root, "FencePost", new Vector3(x, y0 + height * 0.5f + 0.05f, z), new Vector3(0.14f, height + 0.1f, 0.14f), () => Color.black, post);
        }

        /// 그림 빌보드 한 포기(억새/유채/현무암 더미). 그림이 없으면 false.
        private static bool PaintedClump(Transform root, string key, Vector3 pos, float height, float yaw = 0f)
        {
            if (!PaintedProp.Available(key)) return false;
            var pivot = UprightPivot(root, key, pos);
            PaintedProp.Attach(pivot, key, height, false, 0f, 0f, false);
            return true;
        }

        /// 풀 둔덕 드레싱: 억새 + 유채(봄·여름) + 현무암 더미를 x0~x1 사이에 흩뿌린다.
        /// 겨울엔 억새만(눈 덮인 색은 GrassMaterial 틴트), 가을은 억새 위주.
        private static void VergeDressing(Transform root, System.Random rng, float x0, float x1, int count, float zPad = 0.8f)
        {
            var s = SeasonLook.Current;
            // 63차(사용자): 계절 꽃 — 봄 유채 · 여름 해바라기 · 가을 코스모스(겨울만 억새).
            bool canola = s != SeasonKind.Winter;
            string flowerKey = SeasonFlowerKey(FieldKind.Canola);
            float flowerH = flowerKey == "Sunflower" ? 1.5f : flowerKey == "Cosmos" ? 1.05f : 0.85f;
            for (int i = 0; i < count; i++)
            {
                float z = zPad + (float)rng.NextDouble() * (Length - zPad * 2f);
                float x = x0 + (float)rng.NextDouble() * (x1 - x0);
                int roll = rng.Next(10);
                if (roll < 2)
                {
                    if (!PaintedClump(root, "BasaltPile", new Vector3(x, 0f, z), 0.7f + (float)rng.NextDouble() * 0.4f))
                        CreateSphere(root, "Basalt", new Vector3(x, 0.25f, z), 0.4f, () => Basalt);
                }
                else if (canola && roll < 6)
                {
                    if (!PaintedClump(root, flowerKey, new Vector3(x, 0f, z), flowerH + (float)rng.NextDouble() * 0.35f))
                        CreateBox(root, "Bloom", new Vector3(x, 0.4f, z), new Vector3(0.5f, 0.3f, 0.5f), () => new Color(1f, 0.86f, 0.18f));
                }
                else
                {
                    if (!PaintedClump(root, "SilverGrass", new Vector3(x, 0f, z), 1.25f + (float)rng.NextDouble() * 0.55f))
                        CreateCapsule(root, "Plume", new Vector3(x, 0.8f, z), 0.12f, 1.2f, () => new Color(0.90f, 0.85f, 0.70f));
                }
            }
        }

        /// 이어지는 낮은 현무암 돌담(L자 조각 대신 한 줄) — 텍스처 타일링 실제 크기.
        private static void StoneWallRun(Transform root, float x, float height = 0.55f, float y0 = 0f)
        {
            var stone = StoneWallMaterial();
            var wall = CreateBox(root, "StoneWallRun", new Vector3(x, y0 + height * 0.5f, Length * 0.5f), new Vector3(0.5f, height, Length),
                () => Color.Lerp(CoastPalette.RoadGrey, Color.black, 0.4f), stone);
            if (stone != null) SetTiling(wall, Length / 2.4f, height / 2.4f);
        }

        /// 바위 해안: 풀 둔덕 끝에서 바다(-0.35 m)로 내려가는 검은 현무암 경사 + 물가 포말 띠 + 바위 몇 개.
        private static void ShoreBand(Transform root, System.Random rng, float xStart, float width = 4.5f)
        {
            var shore = ShoreMaterial();
            // 경사판: 위쪽(xStart, y 0) → 바다쪽(xStart+width, y -0.6)
            float drop = 0.7f;
            var slab = CreateBox(root, "ShoreSlope", new Vector3(xStart + width * 0.5f, -drop * 0.5f - 0.05f, Length * 0.5f),
                new Vector3(Mathf.Sqrt(width * width + drop * drop), 0.12f, Length + 0.4f), () => Color.Lerp(Basalt, Color.black, 0.2f), shore);
            slab.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-drop, width) * Mathf.Rad2Deg);
            if (shore != null) SetTiling(slab, width / 3.0f, Length / 3.0f);
            // 물가 포말: 반투명 흰 띠(위아래로 숨쉬는 건 CoastSea 의 FoamLine 이 맡는다)
            var foam = CreateBox(root, "ShoreFoam", new Vector3(xStart + width + 0.35f, -0.30f, Length * 0.5f), new Vector3(0.9f, 0.02f, Length + 0.4f),
                () => Color.Lerp(CoastPalette.SeaFoam, Color.white, 0.5f));
            foam.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(() => Color.Lerp(CoastPalette.SeaFoam, Color.white, 0.5f));
            // 바위 몇 개(둥근 검은 돌)
            for (int i = 0; i < 7; i++)
            {
                float t = (float)rng.NextDouble();
                float x = xStart + 0.6f + t * (width - 0.4f);
                float y = -drop * t;
                float r = 0.35f + (float)rng.NextDouble() * 0.55f;
                var rock = CreateSphere(root, "ShoreRock", new Vector3(x, y + r * 0.25f, (float)rng.NextDouble() * Length), r,
                    () => Color.Lerp(Basalt, Color.black, (float)rng.NextDouble() * 0.35f));
                rock.transform.localScale = new Vector3(r * 2.2f, r * 1.1f, r * 1.8f);
                rock.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
        }
    }
}
