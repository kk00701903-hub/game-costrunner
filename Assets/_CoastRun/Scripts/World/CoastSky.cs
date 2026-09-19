using UnityEngine;

namespace CoastRun
{
    /// Atmosphere settings driven later by DynamicEnvironmentManager (serialized, not hardcoded).
    [System.Serializable]
    public class CoastFogSettings
    {
        public bool enabled = true;
        public FogMode mode = FogMode.Linear;
        public float start = 45f;
        public float end = 170f;
    }

    /// Gradient sky + parallax clouds + fog synced to sky color.
    public class CoastSky : MonoBehaviour
    {
        [SerializeField] private CoastFogSettings fog = new CoastFogSettings();
        [SerializeField] private CloudLayerScroller cloudScroller;

        private Transform _follow;
        private Transform _dome;
        private Texture2D _generatedSky;
        private Color _skyColor = new Color(0.31f, 0.66f, 0.85f);
        private float _atmosphere = 1f;

        public CoastFogSettings Fog => fog;
        public Color CurrentSkyColor => _skyColor;
        public float AtmosphereThickness => _atmosphere;

        public void Build(Transform follow)
        {
            _follow = follow;
            _skyColor = CoastPalette.SkyTop;
            ApplyRenderSettings(_skyColor);
            BuildGradientDome();
            EnsureCloudScroller();
            cloudScroller.Build(follow);
        }

        public void SetSkyColor(Color sky, bool instant = false)
        {
            _skyColor = sky;
            if (instant)
                ApplyRenderSettings(sky);
            else
            {
                // Fog stays locked to sky — never drift apart.
                RenderSettings.fogColor = sky;
                Camera cam = Camera.main;
                if (cam != null)
                    cam.backgroundColor = sky;
            }
        }

        /// Sky tint + atmosphere thickness driven by DynamicEnvironmentManager.SetTime.
        public void SetAtmosphere(Color skyTint, float thickness)
        {
            _skyColor = skyTint;
            _atmosphere = Mathf.Clamp(thickness, 0.5f, 2f);
            RenderSettings.fogColor = skyTint;

            Camera cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = skyTint;

            if (_dome == null)
                return;

            var mr = _dome.GetComponent<Renderer>();
            if (mr == null || mr.sharedMaterial == null)
                return;

            var mat = mr.material;
            Color tint = Color.Lerp(Color.white, skyTint, 0.5f);
            float dim = Mathf.Lerp(1.05f, 0.72f, Mathf.InverseLerp(0.7f, 1.5f, _atmosphere));
            tint *= dim;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            if (mat.HasProperty("_SkyTint"))
                mat.SetColor("_SkyTint", skyTint);
            if (mat.HasProperty("_AtmosphereThickness"))
                mat.SetFloat("_AtmosphereThickness", _atmosphere);
            if (_fadeMat != null)
            {
                var fc = _fadeMat.HasProperty("_BaseColor") ? _fadeMat.GetColor("_BaseColor") : _fadeMat.color;
                var nc = tint; nc.a = fc.a;
                if (_fadeMat.HasProperty("_BaseColor")) _fadeMat.SetColor("_BaseColor", nc);
                if (_fadeMat.HasProperty("_Color")) _fadeMat.SetColor("_Color", nc);
            }
        }

        public void ApplyFogSettings(CoastFogSettings settings)
        {
            if (settings != null)
                fog = settings;
            ApplyRenderSettings(_skyColor);
        }

        private void ApplyRenderSettings(Color sky)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(sky, CoastPalette.SkyHorizon, 0.35f) * 0.85f;

            RenderSettings.fog = fog != null && fog.enabled;
            if (fog != null)
            {
                RenderSettings.fogMode = fog.mode;
                if (fog.mode == FogMode.Linear)
                {
                    RenderSettings.fogStartDistance = fog.start;
                    RenderSettings.fogEndDistance = fog.end;
                }
                else
                {
                    RenderSettings.fogDensity = 0.0011f;
                }
            }

            // Critical: fog == sky, or distant props float as ghosts.
            RenderSettings.fogColor = sky;

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = sky;
            }
        }

        // 134차: 장소(챕터 트랙 세트)×계절 전경 그림 — Resources/CoastRun/Sky_{Coast|Village|Oreum|Forest|Cave}_{SPRING|NOON|AUTUMN|WINTER}[_2|_3].png
        //   있으면 하늘 쿼드에 그대로 쓰고(원경 Far_* 는 생략 — 그림에 원경이 들어 있다), 변형이 2장 이상이면 달리는 중 VistaSwitchMeters 마다 크로스페이드.
        private readonly System.Collections.Generic.List<Texture2D> _vistas = new System.Collections.Generic.List<Texture2D>();
        private int _vistaIdx;
        private Transform _fadeQuad; private Material _fadeMat; private float _fadeT = -1f;
        private float _lastZ; private float _sinceSwitch;
        public const float VistaSwitchMeters = 420f, VistaFadeSeconds = 2.5f;
        public bool HasVista => _vistas.Count > 0;

        private void LoadVistas()
        {
            _vistas.Clear();
            string set = ChapterLocation.SetKey(ChapterLocation.Current.set);
            string suf = SeasonLook.Suffix(SeasonLook.Current);
            for (int k = 1; k <= 4; k++)
            {
                string tail = k > 1 ? "_" + k : "";
                var t = ArtAssets.LoadTexture("Sky_" + set + "_" + suf + tail) ?? ArtAssets.LoadTexture("Sky_" + set + tail);
                if (t == null) { if (k == 1) break; else continue; }
                _vistas.Add(t);
            }
            // 계절 그림이 하나도 없으면 NOON(여름) 그림으로 대체
            if (_vistas.Count == 0 && suf != "NOON")
                for (int k = 1; k <= 4; k++)
                {
                    var t = ArtAssets.LoadTexture("Sky_" + set + "_NOON" + (k > 1 ? "_" + k : ""));
                    if (t != null) _vistas.Add(t);
                }
            // 같은 세트 안에서 순서를 섞어 매 런 다르게 시작
            if (_vistas.Count > 1) _vistaIdx = Random.Range(0, _vistas.Count);
            Debug.Log($"[Vista] set={set} season={suf} count={_vistas.Count} start={_vistaIdx}");
        }

        private void BuildGradientDome()
        {
            if (_dome != null)
                CoastEditUtil.DestroyObject(_dome.gameObject);
            if (_fadeQuad != null)
                CoastEditUtil.DestroyObject(_fadeQuad.gameObject);
            _fadeQuad = null; _fadeMat = null; _fadeT = -1f;

            if (_generatedSky != null)
                Destroy(_generatedSky);

            LoadVistas();
            // Painted sky (Firefly, Resources/CoastRun/Sky_Backdrop_NOON) when present;
            // the procedural gradient stays as the fallback.
            Texture2D skyTex = HasVista ? _vistas[_vistaIdx] : SeasonLook.LoadSeasonal("Sky_Backdrop");
            bool painted = skyTex != null;
            if (!painted)
            {
                _generatedSky = SkyTextureGenerator.CreatePortraitSky(512, 896);
                skyTex = _generatedSky;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SkyGradient";
            go.transform.SetParent(transform, false);
            // High + far so it fills upper third of portrait framing.
            if (HasVista)
            {
                // 전경 그림: 폭 150, 비율 유지, 아래 끝은 수평선(y=0) 아래 −16 — 그림의 지평선은 아래 ~10 % 에 있어야 한다.
                float w = 150f, h = w * skyTex.height / (float)skyTex.width;
                go.transform.localPosition = new Vector3(0f, -16f + h * 0.5f, 160f);
                go.transform.localScale = new Vector3(w, h, 1f);
            }
            else
            {
                go.transform.localPosition = new Vector3(0f, painted ? 70f : 62f, 160f);
                go.transform.localScale = painted ? new Vector3(230f, 172f, 1f) : new Vector3(220f, 150f, 1f);
            }
            go.transform.localRotation = Quaternion.identity;
            CoastEditUtil.DestroyCollider(go);

            var mat = CoastMaterials.SetFlat(ArtAssets.CreateTexturedUnlit(skyTex, Color.white));
            if (painted)
                CoastMaterials.SetNoFog(mat);
            var mr = go.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _dome = go.transform;

            if (HasVista && _vistas.Count > 1)
            {
                var fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fg.name = "SkyFade";
                fg.transform.SetParent(transform, false);
                fg.transform.localPosition = go.transform.localPosition + new Vector3(0f, 0f, -1.5f);
                fg.transform.localScale = go.transform.localScale;
                fg.transform.localRotation = Quaternion.identity;
                CoastEditUtil.DestroyCollider(fg);
                _fadeMat = CoastMaterials.CreateTexturedTransparentNoFog(_vistas[(_vistaIdx + 1) % _vistas.Count], new Color(1f, 1f, 1f, 0f));
                CoastMaterials.SetFlat(_fadeMat); CoastMaterials.SetNoFog(_fadeMat, 0f);
                _fadeMat.renderQueue = 2999;
                var fr = fg.GetComponent<Renderer>();
                fr.sharedMaterial = _fadeMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fr.receiveShadows = false;
                fr.enabled = false;
                _fadeQuad = fg.transform;
                _sinceSwitch = 0f; _lastZ = _follow != null ? _follow.position.z : 0f;
            }

            if (!HasVista) BuildFarTown();
        }

        /// 달린 거리로 전경 변형 교체(크로스페이드). LateUpdate 에서 호출.
        private void TickVista()
        {
            if (_fadeQuad == null || _fadeMat == null || _follow == null || _vistas.Count < 2) return;
            float z = _follow.position.z; float dz = Mathf.Abs(z - _lastZ); _lastZ = z;
            if (dz < 50f) _sinceSwitch += dz;
            if (_fadeT < 0f)
            {
                if (_sinceSwitch < VistaSwitchMeters) return;
                _sinceSwitch = 0f; _fadeT = 0f;
                int next = (_vistaIdx + 1) % _vistas.Count;
                SetTex(_fadeMat, _vistas[next]);
                _fadeQuad.GetComponent<Renderer>().enabled = true;
            }
            _fadeT += Time.deltaTime;
            float a = Mathf.Clamp01(_fadeT / VistaFadeSeconds);
            var c = _fadeMat.HasProperty("_BaseColor") ? _fadeMat.GetColor("_BaseColor") : _fadeMat.color;
            c.a = Mathf.SmoothStep(0f, 1f, a);
            if (_fadeMat.HasProperty("_BaseColor")) _fadeMat.SetColor("_BaseColor", c);
            if (_fadeMat.HasProperty("_Color")) _fadeMat.SetColor("_Color", c);
            if (a >= 1f)
            {
                _vistaIdx = (_vistaIdx + 1) % _vistas.Count;
                var mr = _dome.GetComponent<Renderer>();
                if (mr != null && mr.sharedMaterial != null) SetTex(mr.sharedMaterial, _vistas[_vistaIdx]);
                _fadeQuad.GetComponent<Renderer>().enabled = false;
                c.a = 0f; if (_fadeMat.HasProperty("_BaseColor")) _fadeMat.SetColor("_BaseColor", c); if (_fadeMat.HasProperty("_Color")) _fadeMat.SetColor("_Color", c);
                _fadeT = -1f;
            }
        }
        private static void SetTex(Material m, Texture2D t)
        {
            if (m == null || t == null) return;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        }

        private Transform _farTown;

        /// Distant coastal town along the horizon (Firefly, alpha-keyed). Sits just in
        /// front of the sky quad, feet on the horizon line, with the painted haze doing
        /// the distance work instead of fog.
        private void BuildFarTown()
        {
            if (_farTown != null)
                CoastEditUtil.DestroyObject(_farTown.gameObject);
            var tex = SeasonLook.LoadSeasonal("Far_Town");
            if (tex == null)
                return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FarTown";
            go.transform.SetParent(transform, false);
            // Small on purpose: at 150 m a 130 m wide strip reads as a town on the far
            // shore, not a wall of apartments behind the promenade.
            float width = 170f;
            float height = width * tex.height / (float)tex.width;
            // The painting carries Hallasan above a strip of painted sea; its shoreline
            // sits at ~72 % of the height and must land on the real horizon (y ≈ 0.5),
            // so the painted sea overlaps the 3D sea and dissolves into it (alpha fade).
            const float shoreline = 0.72f;
            float centreY = 0.5f + height * (shoreline - 0.5f);
            go.transform.localPosition = new Vector3(-20f, centreY, 152f);
            go.transform.localScale = new Vector3(width, height, 1f);
            go.transform.localRotation = Quaternion.identity;
            CoastEditUtil.DestroyCollider(go);

            // Fog-free: the stock URP Unlit fogged this quad to a single pale band at
            // 150 m (fog end ≈ 154 m), which was the saturation mismatch on the horizon.
            var mat = CoastMaterials.CreateTexturedTransparentNoFog(tex, Color.white);
            CoastMaterials.SetFlat(mat);
            CoastMaterials.SetNoFog(mat, 0f);
            mat.renderQueue = 3000;
            var mr = go.GetComponent<Renderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _farTown = go.transform;
        }

        private void EnsureCloudScroller()
        {
            if (cloudScroller == null)
                cloudScroller = GetComponent<CloudLayerScroller>();
            if (cloudScroller == null)
                cloudScroller = gameObject.AddComponent<CloudLayerScroller>();
        }

        private void LateUpdate()
        {
            if (_follow == null)
                return;

            transform.SetPositionAndRotation(_follow.position, Quaternion.identity);
            TickVista();

            if (_dome != null)
            {
                Vector3 bp = _dome.localPosition;
                bp.x = _follow.position.x * 0.01f;
                _dome.localPosition = bp;
            }

            if (_farTown != null)
            {
                Vector3 tp = _farTown.localPosition;
                tp.x = -20f + _follow.position.x * 0.03f;
                _farTown.localPosition = tp;
            }

            // Keep fog locked to live sky tint (day-cycle blends Update background).
            Camera cam = Camera.main;
            if (cam != null && RenderSettings.fog)
                RenderSettings.fogColor = cam.backgroundColor;
        }

        private void OnDestroy()
        {
            if (_generatedSky != null)
                Destroy(_generatedSky);
        }

        public static Color GetCameraClearColor() => CoastPalette.SkyTop;
    }
}
