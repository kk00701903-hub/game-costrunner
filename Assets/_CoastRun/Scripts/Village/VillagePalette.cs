using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Village
{
    /// 143차: UI(연핑크·민트·베이지·라벤더·크림) 톤에서 뽑은 마을 월드 팔레트 + 동물의 숲풍 소프트 룩 전역 설정.
    /// UI 시안(HUD/버튼/알약)에서 k-means 로 추출한 색을 기준으로 3D 색을 같은 계열로 맞춘다.
    public static class VillagePalette
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        // ── UI 에서 추출(기준) ──
        public static readonly Color UiCream = Hex("#FBECDB"), UiSky = Hex("#DCEEF9"), UiPink = Hex("#FEC4DD"), UiMint = Hex("#87C796"),
                                     UiLavender = Hex("#A192E5"), UiPeach = Hex("#F7D5A6"), UiInk = Hex("#584C59");
        // ── 월드 ──
        public static readonly Color GrassLight = Hex("#A6D77A"), Grass = Hex("#8DC565"), GrassShade = Hex("#6FAE58");
        public static readonly Color Sand = Hex("#F6E7CB"), SandWet = Hex("#E6D3B0"), Path = Hex("#E6D2A8"), PathEdge = Hex("#CDB58A"), Soil = Hex("#B98E6A"), SoilLight = Hex("#D2AE88");
        public static readonly Color SeaDeep = Hex("#3F8FDC"), SeaShallow = Hex("#6AB2F2"), SeaFoam = Hex("#C9E1E7");
        public static readonly Color SkyTop = Hex("#5CA6F0"), SkyMid = Hex("#7DBCF8"), SkyHorizon = Hex("#A9D8FB"), Fog = Hex("#B7DCF7");
        public static readonly Color Rock = Hex("#A8A182"), RockShade = Hex("#7E7A5C"), Moss = Hex("#96B462");
        public static readonly Color Thatch = Hex("#E9C27A"), RoofPink = Hex("#F6B7C4"), RoofMint = Hex("#A9DCC8"), RoofSky = Hex("#BFD8F5"), RoofCream = Hex("#F7D5A6"), WallCream = Hex("#FFF6E8");
        public static readonly Color Log = Hex("#D9A66E"), LogDark = Hex("#B7834F");
        // 그림자 곱색(라벤더) · 외곽선(UI 잉크색)
        public static readonly Color ShadowTint = Hex("#CDBDDA"), Ink = Hex("#584C59");

        /// 소프트 룩 전역 켜기(마을 진입) / 끄기(러닝 등 다른 씬)
        public static void ApplySoftLook(bool on)
        {
            Shader.SetGlobalFloat("_CoastSoft", on ? 1f : 0f);
            Shader.SetGlobalColor("_CoastSoftShadowTint", ShadowTint);
            Shader.SetGlobalFloat("_CoastSoftFloor", 0.5f);
            Shader.SetGlobalFloat("_CoastSoftAmbient", 0.12f);
            Shader.SetGlobalFloat("_CoastSoftRim", 0.14f);
            Shader.SetGlobalColor("_CoastInkColor", Ink);
            Shader.SetGlobalFloat("_CoastInkWidth", 0.45f);
            Shader.SetGlobalFloat("_CoastSharpen", on ? 1f : 0f);   // 151차: 전체 화면 언샤프(CoastSharpenFeature) 마을에서만
        }

        /// 마을 전용 후처리 볼륨(전역 볼륨 위에 우선순위 5) — 따뜻하고 화사하게, 그림자 들어 올림
        public static Volume BuildPostVolume(Transform parent)
        {
            var go = new GameObject("VillageVolume"); go.transform.SetParent(parent, false);
            var vol = go.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 5f; vol.weight = 1f;
            var p = ScriptableObject.CreateInstance<VolumeProfile>(); p.name = "VP_Village"; vol.profile = p;
            // 톤매핑은 끈다 — Neutral 은 채도 높은 하늘·바다를 회색 파스텔로 눌러 버린다(시안은 쨍한 파랑)
            var tone = p.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.None);
            var ca = p.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.04f); ca.contrast.Override(12f); ca.saturation.Override(14f);   // 208차(사용자: 「목장이야기처럼 쨍하게」): 대비 +12 · 채도 +14
            ca.colorFilter.Override(new Color(1f, 0.99f, 0.965f));          // 아주 살짝 따뜻한 필터
            var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(3f); wb.tint.Override(1f);
            var bloom = p.Add<Bloom>(true); bloom.threshold.Override(1.1f); bloom.intensity.Override(0.35f); bloom.scatter.Override(0.7f);
            var vig = p.Add<Vignette>(true); vig.intensity.Override(0.06f); vig.smoothness.Override(0.7f); vig.color.Override(new Color(0.62f, 0.55f, 0.72f));
            var smh = p.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(1.02f, 1.0f, 1.05f, 0.02f));    // 그림자 살짝 라벤더로
            smh.midtones.Override(new Vector4(1.02f, 1.0f, 0.99f, 0f));
            smh.highlights.Override(new Vector4(1.03f, 1.01f, 0.97f, 0f));
            // 146차: 피사계 심도(가우시안 — 모바일 부담 적음): 주인공(카메라 11 m) 은 또렷, 등대·반도(45 m~)부터 살짝 뭉갬
            var dof = p.Add<DepthOfField>(true); dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(34f); dof.gaussianEnd.Override(95f); dof.gaussianMaxRadius.Override(0.6f); dof.active = false;   // 208차: 먼 곳 흐림 끔 — 쨍하게(모바일 부담도 줄어듦)   // 151차: 소품이 뭉개지지 않게 더 멀리서·약하게 dof.highQualitySampling.Override(false);
            var lgg = p.Add<LiftGammaGain>(true); lgg.lift.Override(new Vector4(1f, 1f, 1f, 0.01f)); lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            return vol;
        }
    }
}
