using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoastRun
{
    /// 48차-13: 미니게임 3D 무대. uGUI 마당(Field) 안에 RawImage 하나를 깔고, 멀리 떨어진 곳(y=-3000)에
    /// 전용 카메라 + Kling 배경판(카메라 앞 고정 쿼드) + Blender 저폴리 소품을 두고 RenderTexture 로 그린다.
    /// 게임 로직은 기존 2D(정규화 0..1) 그대로 두고, GroundPoint(n) 으로 뷰포트 좌표 → 바닥 평면(y=0) 월드 좌표를 얻어
    /// 3D 트랜스폼만 구동한다(카메라 뷰포트 = 마당 사각형이라 2D 좌표와 항상 일치).
    ///   var st = MiniStage3D.Create(Field, "UI_MG_Yard_Marbles");
    ///   var m = st.Spawn("MG_Marble"); m.transform.position = st.GroundPoint(new Vector2(0.5f, 0.2f)) + Vector3.up * r;
    public class MiniStage3D : MonoBehaviour
    {
        private const float StageY = -3000f;
        private static int _serial;

        public Transform Root;
        public Camera Cam;
        public RawImage View;
        private RenderTexture _rt;
        private RectTransform _field;
        private Renderer _backdrop;
        private float _pitch, _fov, _dist;
        private bool _fogWas;

        public static MiniStage3D Create(RectTransform field, string backdropArt, float pitchDeg = 56f, float fovDeg = 38f, float dist = 10f)
        {
            // 에디터 핫리로드 뒤 주인을 잃은 무대(마당 RectTransform 이 사라진 것)는 여기서 치운다
            foreach (var old in FindObjectsByType<MiniStage3D>(FindObjectsSortMode.None))
                if (old._field == null) Destroy(old.gameObject);
            var go = new GameObject("MiniStage3D_" + (++_serial));
            go.transform.position = new Vector3(_serial * 200f, StageY, 0f);   // 무대끼리도 겹치지 않게
            var st = go.AddComponent<MiniStage3D>();
            st.Root = go.transform; st._field = field; st._pitch = pitchDeg; st._fov = fovDeg; st._dist = dist;
            st.BuildCamera();
            st.BuildView();
            st.SetBackdrop(backdropArt);
            st.BuildLight();
            return st;
        }

        private void BuildCamera()
        {
            var cgo = new GameObject("Cam");
            cgo.transform.SetParent(Root, false);
            float p = _pitch * Mathf.Deg2Rad;
            cgo.transform.localPosition = new Vector3(0f, Mathf.Sin(p) * _dist, -Mathf.Cos(p) * _dist);
            cgo.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            Cam = cgo.AddComponent<Camera>();
            Cam.fieldOfView = _fov; Cam.nearClipPlane = 0.5f; Cam.farClipPlane = 80f;
            Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = new Color(0.95f, 0.88f, 0.72f, 1f);
            Cam.depth = -50f; Cam.allowHDR = false; Cam.allowMSAA = false; Cam.useOcclusionCulling = false;
            var add = Cam.GetUniversalAdditionalCameraData();
            if (add != null) { add.renderType = CameraRenderType.Base; add.renderPostProcessing = false; add.renderShadows = false; add.requiresColorOption = CameraOverrideOption.Off; add.requiresDepthOption = CameraOverrideOption.Off; }
            AudioListener al = cgo.GetComponent<AudioListener>(); if (al != null) Destroy(al);
        }

        private void BuildView()
        {
            var vgo = new GameObject("Stage3D", typeof(RectTransform), typeof(RawImage));
            vgo.transform.SetParent(_field, false);
            vgo.transform.SetAsFirstSibling();
            var rt = vgo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            View = vgo.GetComponent<RawImage>(); View.raycastTarget = false; View.color = Color.white;
            EnsureRt(true);
        }

        private void EnsureRt(bool force)
        {
            var size = _field.rect.size;
            float px = Mathf.Abs(_field.lossyScale.x);
            int w = Mathf.Clamp(Mathf.RoundToInt(size.x * px), 64, 1200), h = Mathf.Clamp(Mathf.RoundToInt(size.y * px), 64, 1600);
            if (!force && _rt != null && Mathf.Abs(_rt.width - w) < 3 && Mathf.Abs(_rt.height - h) < 3) return;
            if (_rt != null) { Cam.targetTexture = null; _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "MiniStageRT", antiAliasing = 1, useMipMap = false, filterMode = FilterMode.Bilinear };
            _rt.Create();
            Cam.targetTexture = _rt;
            View.texture = _rt;
            FitBackdrop();
        }

        /// 배경판: 카메라 앞 40m 에 고정된 쿼드(뷰 전체를 덮음). Kling 그림의 원근을 그대로 쓰고, 소품만 3D 로 얹는다.
        public void SetBackdrop(string artName)
        {
            var tex = string.IsNullOrEmpty(artName) ? null : ArtAssets.LoadTexture(artName);
            if (tex == null) return;
            if (_backdrop == null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Backdrop"; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(Cam.transform, false);
                q.transform.localPosition = new Vector3(0f, 0f, 40f); q.transform.localRotation = Quaternion.identity;
                _backdrop = q.GetComponent<Renderer>();
                _backdrop.shadowCastingMode = ShadowCastingMode.Off; _backdrop.receiveShadows = false;
            }
            var mat = CoastMaterials.CreateTexturedTransparentNoFog(tex, Color.white);
            CoastMaterials.SetFlat(mat);
            _backdrop.sharedMaterial = mat;
            FitBackdrop();
        }

        private void FitBackdrop()
        {
            if (_backdrop == null || _rt == null) return;
            float d = _backdrop.transform.localPosition.z;
            float hgt = 2f * d * Mathf.Tan(_fov * 0.5f * Mathf.Deg2Rad) * 1.02f;
            float asp = (float)_rt.width / _rt.height;
            _backdrop.transform.localScale = new Vector3(hgt * asp, hgt, 1f);
        }

        private void BuildLight()
        {
            var lgo = new GameObject("KeyLight");
            lgo.transform.SetParent(Root, false);
            lgo.transform.localRotation = Quaternion.Euler(52f, -28f, 0f);
            var l = lgo.AddComponent<Light>(); _key = l;
            l.type = LightType.Directional; l.color = new Color(1f, 0.97f, 0.90f); l.intensity = 1.0f; l.shadows = LightShadows.None;
            l.cullingMask = ~0;
        }

        /// 정규화 마당 좌표(0..1, y 위 = 멀리) → 바닥 평면(y = Root.y) 월드 좌표
        public Vector3 GroundPoint(Vector2 n)
        {
            var ray = Cam.ViewportPointToRay(new Vector3(n.x, n.y, 0f));
            float dy = ray.direction.y;
            if (dy > -1e-4f) dy = -1e-4f;
            float t = (Root.position.y - ray.origin.y) / dy;
            return ray.origin + ray.direction * t;
        }

        /// 그 지점에서 마당 가로 1.0(정규화)이 월드로 몇 m 인지 — 크기를 2D 픽셀 감각으로 맞출 때 쓴다
        public float WorldPerNorm(Vector2 n)
        {
            var a = GroundPoint(new Vector2(Mathf.Max(0f, n.x - 0.1f), n.y));
            var b = GroundPoint(new Vector2(Mathf.Min(1f, n.x + 0.1f), n.y));
            return (b - a).magnitude / (Mathf.Min(1f, n.x + 0.1f) - Mathf.Max(0f, n.x - 0.1f));
        }

        /// Resources/CoastRun/<name>.fbx (Blender 키트) 를 무대 아래에 놓는다. 콜라이더 제거·그림자 끔.
        public GameObject Spawn(string resName)
        {
            var prefab = Resources.Load<GameObject>(ArtAssets.ResourceRoot + resName);
            GameObject go;
            if (prefab == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = resName + "(fallback)";
            }
            else go = Instantiate(prefab);
            // FBX 임포트 회전(-90° X 등)을 지키기 위해 피벗 하나로 감싼다 — 호출자는 피벗을 돌리고 키운다
            var pivot = new GameObject(resName);
            pivot.transform.SetParent(Root, false);
            go.transform.SetParent(pivot.transform, false);
            go = pivot;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            return go;
        }

        /// 바닥에 눕힌 납작한 막대(분필 선·테두리)
        public GameObject Bar(Vector3 a, Vector3 b, float width, float height, Material mat)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.name = "Bar"; Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(Root, false);
            c.transform.position = (a + b) * 0.5f + Vector3.up * height * 0.5f;
            var d = b - a;
            c.transform.rotation = d.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(d.normalized, Vector3.up) : Quaternion.identity;
            c.transform.localScale = new Vector3(width, height, d.magnitude + width);
            var r = c.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return c;
        }

        /// 바닥에 눕힌 부드러운 그림자 원반
        public GameObject Blob(float radius, float alpha = 0.28f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Blob"; Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(Root, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            var mat = SoftDisc(new Color(0.15f, 0.09f, 0.05f, alpha));
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return q;
        }

        /// 부드러운 원판(그림자·글로우) — URP Lit 투명 + 소프트 디스크 텍스처, 스페큘러 없음. (Particles/Unlit 은 무대 RT 에서 알파 0.55 아래가 안 보였다)
        public static Material SoftDisc(Color color)
        {
            var m = Lit(color, 0f, 0f, true);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", BlobShadow.SoftDisc());
            if (m.HasProperty("_SpecularHighlights")) { m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); }
            return m;
        }

        /// 60차: 네온(발광) 재질 — 무궁화 레인 선. 바탕색 + 같은 색 발광.
        public static Material Neon(Color color, float glow = 1.6f, bool transparent = false)
        {
            var m = Lit(color, 0.2f, 0f, transparent);
            if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", color * glow); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            return m;
        }

        /// URP Lit 런타임 머티리얼(귀여운 반짝 구슬용). transparent 면 알파 블렌드.
        public static Material Lit(Color color, float smoothness = 0.6f, float metallic = 0f, bool transparent = false)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? CoastMaterials.LitShader;
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color); else m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            // 하늘 반사(환경광 반사)가 러닝 씬 스카이박스를 비춰 하얗게 날리므로 끈다 — 하이라이트는 키 라이트 하나로
            if (m.HasProperty("_EnvironmentReflections")) { m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); }
            if (transparent && m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0); m.renderQueue = 3000;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                OpaqueAlpha(m);
            }
            return m;
        }

        /// RenderTexture 의 알파를 항상 1로 — 반투명(유리·그림자)이 RT 알파를 깎으면 RawImage 가 그 자리를 뚫어
        /// 뒤의 크림색 프레임이 비쳐 전부 파스텔로 바랬다(48차-13 원인). 알파 채널만 One/One 로 더한다.
        public static Material OpaqueAlpha(Material m)
        {
            if (m == null) return null;
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            return m;
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCam;
            RenderPipelineManager.endCameraRendering += OnEndCam;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCam;
            RenderPipelineManager.endCameraRendering -= OnEndCam;
        }

        // 무대 카메라가 그리는 동안만: 안개 끔 · 씬의 다른 조명(러닝 씬 해·달) 끔 · 환경광을 고정 — 낮/밤/날씨와 무관하게 늘 같은 밝기
        private readonly List<Light> _offLights = new List<Light>(8);
        private Light _key;
        private AmbientMode _ambModeWas; private Color _ambWas; private float _ambIntWas;
        private void OnBeginCam(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != Cam) return;
            _fogWas = RenderSettings.fog; RenderSettings.fog = false;
            _ambModeWas = RenderSettings.ambientMode; _ambWas = RenderSettings.ambientLight; _ambIntWas = RenderSettings.ambientIntensity;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.62f, 0.60f, 0.58f); RenderSettings.ambientIntensity = 1f;
            _offLights.Clear();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != _key && l.enabled) { l.enabled = false; _offLights.Add(l); }
        }

        private void OnEndCam(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != Cam) return;
            RenderSettings.fog = _fogWas;
            RenderSettings.ambientMode = _ambModeWas; RenderSettings.ambientLight = _ambWas; RenderSettings.ambientIntensity = _ambIntWas;
            foreach (var l in _offLights) if (l != null) l.enabled = true;
            _offLights.Clear();
        }

        private void LateUpdate()
        {
            if (_field == null) { Destroy(gameObject); return; }
            EnsureRt(false);
        }

        private void OnDestroy()
        {
            if (Cam != null) Cam.targetTexture = null;
            if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
            if (View != null) Destroy(View.gameObject);
        }
    }
}
