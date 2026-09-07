using UnityEngine;

namespace CoastRun
{
    /// 계절(= 육성 주차 → 챕터)에 따라 주변 환경이 바뀐다: 하늘·원경 그림, 건물·길 색조.
    /// 그림은 Resources/CoastRun/Sky_Backdrop_{SPRING|NOON|AUTUMN|WINTER}, Far_Town_{...} — 없으면 NOON.
    public static class SeasonLook
    {
        public static SeasonKind Current
        {
            get
            {
                var sm = StageManager.Instance;
                return StageManager.ChapterAsSeason(sm != null ? sm.ChapterIndex : 1);
            }
        }

        public static string Suffix(SeasonKind s)
        {
            switch (s)
            {
                case SeasonKind.Spring: return "SPRING";
                case SeasonKind.Autumn: return "AUTUMN";
                case SeasonKind.Winter: return "WINTER";
                default: return "NOON";
            }
        }

        /// 계절 그림이 있으면 그것, 없으면 NOON. 원경(Far_Town)은 챕터 로케이션 세트를 먼저 찾는다.
        public static Texture2D LoadSeasonal(string baseName)
        {
            var s = Current;
            if (baseName == "Far_Town")
            {
                string set = ChapterLocation.SetKey(ChapterLocation.Current.set);
                var t0 = ArtAssets.LoadTexture("Far_" + set + "_" + Suffix(s)) ?? ArtAssets.LoadTexture("Far_" + set);
                if (t0 != null) return t0;
            }
            if (s != SeasonKind.Summer)
            {
                var t = ArtAssets.LoadTexture(baseName + "_" + Suffix(s));
                if (t != null) return t;
            }
            return ArtAssets.LoadTexture(baseName + "_NOON");
        }

        /// 건물 색조 — 봄 파스텔, 여름 원색, 가을 따뜻·어두움, 겨울 차갑고 밝음.
        public static Color BuildingTint(SeasonKind s)
        {
            switch (s)
            {
                case SeasonKind.Spring: return new Color(1.0f, 0.96f, 0.98f);
                case SeasonKind.Autumn: return new Color(0.98f, 0.86f, 0.72f);
                case SeasonKind.Winter: return new Color(0.86f, 0.90f, 1.0f);
                default: return Color.white;
            }
        }

        public static Color RoadTint(SeasonKind s)
        {
            switch (s)
            {
                // 11차: 바닥이 제주 현무암 돌길(어두운 회색)로 바뀌어 더 누르지 않는다. 계절은 색조만.
                case SeasonKind.Spring: return new Color(1.0f, 0.99f, 0.97f);
                case SeasonKind.Autumn: return new Color(1.0f, 0.92f, 0.82f);
                case SeasonKind.Winter: return new Color(0.92f, 0.95f, 1.0f);
                // 14차-3: 목표 이미지의 햇볕 받은 따뜻한 돌길 — 파란 하늘 반사광에 눌린 회청색을 걷어낸다.
                default: return new Color(1.0f, 0.95f, 0.86f);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock _mpb;

        /// 렌더러들에 계절 색조를 MaterialPropertyBlock으로 얹는다(공유 머티리얼은 건드리지 않음).
        public static void Tint(GameObject go, float strength = 1f) => Tint(go, strength, Color.white);

        /// extra: 건물마다 살짝 다른 색조(6차 — 같은 건물이 줄지어 서도 밋밋하지 않게).
        public static void Tint(GameObject go, float strength, Color extra)
        {
            var s = Current;
            if (go == null) return;
            if (s == SeasonKind.Summer && extra == Color.white) return;
            Color tint = Color.Lerp(Color.white, BuildingTint(s), strength) * extra;
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(_mpb);
                var mat = r.sharedMaterial;
                Color baseC = Color.white;
                if (mat != null)
                {
                    if (mat.HasProperty(BaseColorId)) baseC = mat.GetColor(BaseColorId);
                    else if (mat.HasProperty(ColorId)) baseC = mat.GetColor(ColorId);
                }
                _mpb.SetColor(BaseColorId, baseC * tint);
                _mpb.SetColor(ColorId, baseC * tint);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
