using UnityEngine;

namespace CoastRun
{
    /// Applies cel toon + ink outline to character meshes (primitive or FBX).
    public class CoastPlayerVisual : MonoBehaviour
    {
        private const float ScreenOccupancyScale = 1.12f;

        [SerializeField] private float windStrength = 8f;

        private Transform _hair;
        private Transform _backpack;
        private Transform _board;
        private Transform _body;
        private Transform _rootVisual;
        private Transform _visualRoot;
        private Transform _billboard;
        private float _phase;
        private PlayerController _player;
        private bool _menuPose;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Vector3 _bodyBasePos;
        private Vector3 _billboardBasePos;
        private Vector3 _billboardBaseScale = new Vector3(0.95f, 1.45f, 1f);

        // Lane lean — same sign as camera roll (opposite to lane motion). Character leads camera.
        private float _leanZ;
        private float _leanTarget;
        private float _leanVel;
        private float _boardLeanZ;
        private float _boardLeanVel;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        public void SetMenuPose(bool menu)
        {
            _menuPose = menu;
            if (_body != null)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, 0f);
                _body.localPosition = _bodyBasePos;
                _body.localScale = _bodyBaseScale;
            }

            if (_board != null)
                _board.localRotation = Quaternion.Euler(8f, 0f, 0f);
        }

        public void Build()
        {
            ClearVisualChildren();
            _phase = Random.value * Mathf.PI * 2f;

            var prefabRoot = PrefabLibrary.TryInstantiate("GirlSkater", transform, Vector3.zero);
            if (prefabRoot != null)
            {
                prefabRoot.name = "GirlSkater";
                prefabRoot.transform.localScale = Vector3.one * (0.92f * ScreenOccupancyScale);
                _rootVisual = prefabRoot.transform;
                _visualRoot = prefabRoot.transform;
                CoastMaterials.ApplyToonToHierarchy(prefabRoot.transform);
                ApplyCharacterOutlines(prefabRoot.transform);
                CacheBones(prefabRoot.transform);
                CacheBasePose();
                TryAttachPaintedBillboard();
                BlobShadow.Attach(transform, 0.85f * ScreenOccupancyScale);
                return;
            }

            _visualRoot = new GameObject("VisualRoot").transform;
            _visualRoot.SetParent(transform, false);
            _visualRoot.localScale = Vector3.one * ScreenOccupancyScale;
            BuildProcedural();
            _rootVisual = _visualRoot;
            ApplyCharacterOutlines(_visualRoot);
            CacheBasePose();
            TryAttachPaintedBillboard();
            BlobShadow.Attach(transform, 0.85f * ScreenOccupancyScale);
        }

        private void TryAttachPaintedBillboard()
        {
            var tex = ArtAssets.LoadTexture("GirlSkater_Back");
            if (tex == null)
                return;

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name != "PaintedGirl")
                    r.enabled = false;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PaintedGirl";
            quad.transform.SetParent(_visualRoot != null ? _visualRoot : transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.78f, 0.04f);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(0.95f, 1.45f, 1f);
            CoastEditUtil.DestroyCollider(quad);

            var shader = Shader.Find("CoastRun/ChromaUnlit") ?? CoastMaterials.UnlitShader;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            else
                mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_KeyColor"))
                mat.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
            quad.GetComponent<Renderer>().sharedMaterial = mat;
            quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _billboard = quad.transform;
            _billboardBasePos = _billboard.localPosition;
            _billboardBaseScale = _billboard.localScale;
        }

        private void ClearVisualChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                CoastEditUtil.DestroyObject(child.gameObject);
            }

            _hair = null;
            _backpack = null;
            _board = null;
            _body = null;
            _rootVisual = null;
        }

        private void CacheBasePose()
        {
            if (_body != null)
            {
                _bodyBaseScale = _body.localScale;
                _bodyBasePos = _body.localPosition;
            }
        }

        private void CacheBones(Transform root)
        {
            _body = FindDeep(root, "Body") ?? root;
            _hair = FindDeep(root, "Hair");
            _backpack = FindDeep(root, "Backpack");
            _board = FindDeep(root, "Deck") ?? FindDeep(root, "Skateboard");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name.Contains(name))
                return root;
            foreach (Transform c in root)
            {
                var f = FindDeep(c, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private void ApplyCharacterOutlines(Transform root)
        {
            if (root == null)
                return;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                string n = r.gameObject.name;
                if (n.Contains("Outline") || n.Contains("Blob") || n.Contains("Wheel") ||
                    n.Contains("Deck") || n == "PaintedGirl" || n.Contains("Skateboard"))
                    continue;
                if (_board != null && r.transform.IsChildOf(_board))
                    continue;
                if (r.GetComponent<CelOutlineHint>() == null)
                    r.gameObject.AddComponent<CelOutlineHint>();
            }
        }

        private void BuildProcedural()
        {
            _body = AddPart("Body", PrimitiveType.Capsule,
                new Vector3(0f, 0.95f, 0f), new Vector3(0.38f, 0.55f, 0.28f), () => CoastPalette.Shirt).transform;

            AddPart("Head", PrimitiveType.Sphere,
                new Vector3(0f, 1.55f, 0.05f), new Vector3(0.32f, 0.32f, 0.32f), () => CoastPalette.Skin);

            _hair = AddPart("Hair", PrimitiveType.Cube,
                new Vector3(0f, 1.68f, -0.06f), new Vector3(0.34f, 0.14f, 0.22f), () => CoastPalette.Hair).transform;

            AddPart("Shorts", PrimitiveType.Cube,
                new Vector3(0f, 0.62f, 0f), new Vector3(0.36f, 0.22f, 0.26f), () => CoastPalette.Shorts);

            AddPart("LegL", PrimitiveType.Capsule,
                new Vector3(-0.12f, 0.28f, 0.05f), new Vector3(0.14f, 0.22f, 0.14f), () => CoastPalette.Shorts);
            AddPart("LegR", PrimitiveType.Capsule,
                new Vector3(0.12f, 0.28f, 0.05f), new Vector3(0.14f, 0.22f, 0.14f), () => CoastPalette.Shorts);

            AddPart("ShoeL", PrimitiveType.Cube,
                new Vector3(-0.12f, 0.08f, 0.12f), new Vector3(0.16f, 0.08f, 0.28f), () => CoastPalette.Shoes);
            AddPart("ShoeR", PrimitiveType.Cube,
                new Vector3(0.12f, 0.08f, 0.12f), new Vector3(0.16f, 0.08f, 0.28f), () => CoastPalette.Shoes);

            _backpack = AddPart("Backpack", PrimitiveType.Cube,
                new Vector3(0f, 1.05f, -0.22f), new Vector3(0.42f, 0.48f, 0.28f), () => CoastPalette.Backpack).transform;

            _board = new GameObject("Skateboard").transform;
            _board.SetParent(_visualRoot != null ? _visualRoot : transform, false);
            _board.localPosition = new Vector3(0f, 0.04f, 0.08f);
            _board.localRotation = Quaternion.Euler(4f, 0f, 0f);

            CreatePartOn(_board, "Deck", PrimitiveType.Cube,
                Vector3.zero, new Vector3(0.55f, 0.05f, 1.35f), () => CoastPalette.BoardDeck);
            CreatePartOn(_board, "WheelFL", PrimitiveType.Cylinder,
                new Vector3(-0.18f, -0.05f, 0.48f), new Vector3(0.14f, 0.04f, 0.14f), () => CoastPalette.WheelOrange);
            CreatePartOn(_board, "WheelFR", PrimitiveType.Cylinder,
                new Vector3(0.18f, -0.05f, 0.48f), new Vector3(0.14f, 0.04f, 0.14f), () => CoastPalette.WheelOrange);
            CreatePartOn(_board, "WheelBL", PrimitiveType.Cylinder,
                new Vector3(-0.18f, -0.05f, -0.48f), new Vector3(0.14f, 0.04f, 0.14f), () => CoastPalette.WheelOrange);
            CreatePartOn(_board, "WheelBR", PrimitiveType.Cylinder,
                new Vector3(0.18f, -0.05f, -0.48f), new Vector3(0.14f, 0.04f, 0.14f), () => CoastPalette.WheelOrange);
        }

        /// Lane change lean. Same direction as camera roll (centrifugal). Starts immediately;
        /// camera anticipation is slightly slower so character leads by ~leadSeconds.
        public void PulseLaneLean(int direction, float leadSeconds)
        {
            // direction +1 = move right → camera rolls negative → character lean negative.
            _leanTarget = -direction * 12f;
            CancelInvoke(nameof(ClearLeanTarget));
            Invoke(nameof(ClearLeanTarget), 0.22f + Mathf.Max(0.01f, leadSeconds));
        }

        private void ClearLeanTarget()
        {
            _leanTarget = 0f;
        }

        private void Update()
        {
            if (_menuPose)
                return;

            ApplyGameplayPose();
            UpdateLaneLean();

            float speed = _player != null ? _player.NormalizedSpeed : 0.5f;
            float t = Time.time * (windStrength + speed * 4f) + _phase;
            float sway = Mathf.Sin(t) * (4f + speed * 3f);

            if (_hair != null)
                _hair.localRotation = Quaternion.Euler(sway * 0.6f, 0f, sway * 0.3f);
            if (_backpack != null)
                _backpack.localRotation = Quaternion.Euler(sway * 0.25f, 0f, sway * 0.15f);

            if (_body != null && _player != null && _player.State == SkateState.Run && !_player.IsTucking)
            {
                _body.localRotation = Quaternion.Euler(sway * 0.12f, 0f, _leanZ);
            }
            else if (_body != null && Mathf.Abs(_leanZ) > 0.01f && _player != null &&
                     _player.State != SkateState.SoftHit)
            {
                var e = _body.localRotation.eulerAngles;
                _body.localRotation = Quaternion.Euler(e.x, e.y, _leanZ);
            }

            if (_board != null)
            {
                float bob = Mathf.Sin(t * 1.4f) * (1.5f + speed * 2f);
                _board.localRotation = Quaternion.Euler(4f + bob, 0f, sway * 0.08f + _boardLeanZ);
            }
        }

        private void UpdateLaneLean()
        {
            float dt = Time.unscaledDeltaTime;
            _leanZ = Mathf.SmoothDamp(_leanZ, _leanTarget, ref _leanVel, 0.08f, 80f, dt);
            _boardLeanZ = Mathf.SmoothDamp(_boardLeanZ, _leanTarget * 0.65f, ref _boardLeanVel, 0.1f, 80f, dt);
        }

        private void LateUpdate()
        {
            ApplyCrouchBillboard();

            if (_billboard == null)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            _billboard.LookAt(cam.transform.position, Vector3.up);
            _billboard.Rotate(0f, 180f, 0f);
        }

        private void ApplyCrouchBillboard()
        {
            if (_billboard == null || _player == null || _menuPose)
                return;

            if (_player.IsCrouching)
            {
                _billboard.localScale = new Vector3(
                    _billboardBaseScale.x,
                    _billboardBaseScale.y * 0.62f,
                    _billboardBaseScale.z);
                _billboard.localPosition = _billboardBasePos + new Vector3(0f, -0.28f, 0.08f);
            }
            else
            {
                _billboard.localScale = _billboardBaseScale;
                _billboard.localPosition = _billboardBasePos;
            }
        }

        private void ApplyGameplayPose()
        {
            if (_player == null || _body == null)
                return;

            switch (_player.State)
            {
                case SkateState.Crouch:
                    _body.localScale = new Vector3(_bodyBaseScale.x, _bodyBaseScale.y * 0.72f, _bodyBaseScale.z);
                    _body.localPosition = _bodyBasePos + new Vector3(0f, -0.18f, 0f);
                    if (_board != null)
                        _board.localRotation = Quaternion.Euler(12f, 0f, 0f);
                    break;
                case SkateState.Air:
                    _body.localScale = _bodyBaseScale;
                    _body.localPosition = _bodyBasePos + new Vector3(0f, 0.08f, 0f);
                    _body.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                    if (_board != null)
                        _board.localRotation = Quaternion.Euler(-6f, 0f, 0f);
                    break;
                case SkateState.SoftHit:
                    _body.localScale = _bodyBaseScale;
                    _body.localPosition = _bodyBasePos;
                    _body.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Sin(Time.time * 22f) * 6f);
                    break;
                default:
                    _body.localScale = _bodyBaseScale;
                    _body.localPosition = _bodyBasePos;
                    if (_player.IsTucking)
                        _body.localRotation = Quaternion.Euler(18f, 0f, 0f);
                    else
                        _body.localRotation = Quaternion.identity;
                    break;
            }
        }

        private GameObject AddPart(string name, PrimitiveType type, Vector3 pos, Vector3 scale, System.Func<Color> color)
        {
            var go = MakePrimitive(name, type, pos, scale, color);
            go.transform.SetParent(_visualRoot != null ? _visualRoot : transform, false);
            return go;
        }

        private static GameObject MakePrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale,
            System.Func<Color> color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateToon(color(), color);
            CoastEditUtil.DestroyCollider(go);
            return go;
        }

        private static void CreatePartOn(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale,
            System.Func<Color> color)
        {
            var go = MakePrimitive(name, type, pos, scale, color);
            go.transform.SetParent(parent, false);
            if (type == PrimitiveType.Cylinder)
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
