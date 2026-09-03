using System.Collections;
using UnityEngine;

/// Full 3D rider view. Loads Resources/Character/Doha/DohaModel when present;
/// otherwise builds a stylized low-poly placeholder and animates from FSM state.
[RequireComponent(typeof(PlayerController))]
public class PlayerCharacterView : MonoBehaviour
{
    private const string PrefabPath = "Character/Doha/DohaModel";
    private const string ModelFallbackPath = "Character/Doha/characterMedium";

    [SerializeField] private Vector3 modelOffset = new Vector3(0f, -0.85f, 0f);
    [SerializeField] private float modelScale = 1f;
    [SerializeField] private float leanAngle = 14f;

    private PlayerController _player;
    private PlayerVitals _vitals;
    private Transform _visualRoot;
    private Animator _animator;
    private Transform _torso;
    private Transform _board;
    private Renderer[] _deckRenderers;
    private bool _procedural;
    private float _bob;

    private struct RunBone
    {
        public Transform Bone;
        public Quaternion BaseLocal;
        public float Sign;
    }

    private RunBone[] _runBones;
    private bool _kenneyRun;
    private float _runPhase;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _vitals = GetComponent<PlayerVitals>();
        HidePlaceholderMesh();

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = Resources.Load<GameObject>(ModelFallbackPath);
        if (prefab != null)
            BuildFromPrefab(prefab);
        else
            BuildProcedural();

        if (GetComponent<CharacterShadow>() == null)
            gameObject.AddComponent<CharacterShadow>();
    }

    private void Start()
    {
        if (_animator != null)
            StartCoroutine(BootAnimator());
    }

    private void LateUpdate()
    {
        if (_player == null || _visualRoot == null)
            return;

        SyncAnimator();

        if (_kenneyRun)
            AnimateKenneyRun();

        // Always add runner lean/bob so Kenney T-pose still reads as motion.
        AnimateProcedural();
        ApplyDeckCondition();
    }

    private void BuildFromPrefab(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, transform);
        instance.name = "DohaModel";
        _visualRoot = instance.transform;
        _visualRoot.localPosition = modelOffset;
        // Kenney humanoid faces +Z (run direction); camera sits behind at -Z.
        _visualRoot.localRotation = Quaternion.identity;
        _visualRoot.localScale = Vector3.one * modelScale;

        _animator = instance.GetComponentInChildren<Animator>();
        _deckRenderers = instance.GetComponentsInChildren<Renderer>();

        Texture2D skin = Resources.Load<Texture2D>("Character/Doha/humanFemaleA");
        ArtLibrary.EnsureCharacterVisible(instance, skin);

        CacheKenneyRunBones(instance.transform);

        if (_animator != null)
        {
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            Avatar avatar = _animator.avatar;
            if (avatar == null || !avatar.isValid)
            {
                Animator source = prefab.GetComponentInChildren<Animator>();
                if (source != null && source.avatar != null && source.avatar.isValid)
                    _animator.avatar = source.avatar;
            }
        }

        if (_animator == null || _animator.runtimeAnimatorController == null)
            _procedural = true;
    }

    private IEnumerator BootAnimator()
    {
        yield return null;

        if (_animator == null)
            yield break;

        _animator.Rebind();
        _animator.Update(0f);

        int runHash = Animator.StringToHash("Run");
        if (_animator.HasState(0, runHash))
            _animator.Play(runHash, 0, 0f);
        else
            _animator.Play("Run", 0, 0f);

        yield return new WaitForSeconds(0.2f);

        if (ShouldUseKenneyRun())
            EnableKenneyRun();
    }

    private bool ShouldUseKenneyRun()
    {
        if (_runBones == null || _runBones.Length == 0)
            return false;

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return true;

        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime <= 0.001f && _player != null && _player.NormalizedSpeed > 0.05f)
            return true;

        Transform leftArm = _runBones[0].Bone;
        if (leftArm == null)
            return false;

        // T-pose arms sit near 0° pitch; a running clip lifts them within a few frames.
        float pitch = leftArm.localEulerAngles.x;
        if (pitch > 180f)
            pitch -= 360f;

        return Mathf.Abs(pitch) < 8f;
    }

    private void EnableKenneyRun()
    {
        if (_runBones == null || _runBones.Length == 0)
            return;

        _kenneyRun = true;
        _procedural = true;
    }

    private void CacheKenneyRunBones(Transform root)
    {
        var defs = new[]
        {
            ("LeftArm", 1f),
            ("RightArm", -1f),
            ("LeftUpLeg", -1f),
            ("RightUpLeg", 1f)
        };

        var bones = new RunBone[defs.Length];
        int count = 0;

        for (int i = 0; i < defs.Length; i++)
        {
            Transform bone = FindBone(root, defs[i].Item1);
            if (bone == null)
                continue;

            bones[count++] = new RunBone
            {
                Bone = bone,
                BaseLocal = bone.localRotation,
                Sign = defs[i].Item2
            };
        }

        if (count == 0)
        {
            _runBones = null;
            return;
        }

        if (count == bones.Length)
        {
            _runBones = bones;
            return;
        }

        _runBones = new RunBone[count];
        for (int i = 0; i < count; i++)
            _runBones[i] = bones[i];
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == boneName)
                return all[i];
        }

        return null;
    }

    private void AnimateKenneyRun()
    {
        if (_runBones == null || _player == null)
            return;

        float speed = Mathf.Clamp01(_player.NormalizedSpeed);
        _runPhase += Time.deltaTime * Mathf.Lerp(7f, 13f, speed);
        float armSwing = 38f * speed;
        float legSwing = 44f * speed;
        float wave = Mathf.Sin(_runPhase);

        for (int i = 0; i < _runBones.Length; i++)
        {
            Transform bone = _runBones[i].Bone;
            if (bone == null)
                continue;

            float swing = i < 2 ? armSwing : legSwing;
            float angle = swing * wave * _runBones[i].Sign;
            bone.localRotation = _runBones[i].BaseLocal * Quaternion.Euler(angle, 0f, 0f);
        }
    }

    private void BuildProcedural()
    {
        _procedural = true;

        GameObject root = new GameObject("DohaPlaceholder");
        root.transform.SetParent(transform, false);
        _visualRoot = root.transform;
        _visualRoot.localPosition = modelOffset;
        _visualRoot.localScale = Vector3.one * modelScale;

        Material skin = MaterialLibrary.Active != null && MaterialLibrary.Active.characterSkin != null
            ? MaterialLibrary.Active.characterSkin
            : ArtLibrary.Surface(null, null, new Color(0.92f, 0.78f, 0.66f), Vector2.one, 0.2f);

        Material cloth = ArtLibrary.Surface(null, null, new Color(0.18f, 0.22f, 0.30f), Vector2.one, 0.08f);
        Material deckMat = ArtLibrary.Surface(null, null, new Color(0.34f, 0.28f, 0.22f), Vector2.one, 0.15f);

        _torso = MakePart(root.transform, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.55f, 0f),
            new Vector3(0.42f, 0.55f, 0.30f), cloth).transform;
        MakePart(root.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.05f, 0.02f),
            new Vector3(0.34f, 0.34f, 0.34f), skin);
        MakePart(root.transform, PrimitiveType.Capsule, "LegL", new Vector3(-0.14f, 0.18f, 0f),
            new Vector3(0.14f, 0.36f, 0.14f), cloth);
        MakePart(root.transform, PrimitiveType.Capsule, "LegR", new Vector3(0.14f, 0.18f, 0f),
            new Vector3(0.14f, 0.36f, 0.14f), cloth);

        GameObject boardGo = MakePart(root.transform, PrimitiveType.Cube, "Board", new Vector3(0f, 0.08f, 0.05f),
            new Vector3(0.22f, 0.05f, 0.82f), deckMat);
        _board = boardGo.transform;
        _deckRenderers = boardGo.GetComponentsInChildren<Renderer>();
    }

    private static GameObject MakePart(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && mat != null)
            renderer.sharedMaterial = mat;

        return go;
    }

    private void SyncAnimator()
    {
        if (_animator == null || _kenneyRun)
            return;

        _animator.SetFloat(CharacterAnimParams.Speed, _player.NormalizedSpeed);
        _animator.SetBool(CharacterAnimParams.Grounded, _player.IsGrounded);
        _animator.SetBool(CharacterAnimParams.Slide, _player.IsSliding);
        _animator.SetBool(CharacterAnimParams.Jump, _player.IsJumping);
        _animator.SetFloat(CharacterAnimParams.Lean, _player.TurnLeanDirection);
        _animator.SetBool(CharacterAnimParams.Hurt, _player.State == PlayerState.Hurt);
        _animator.SetBool(CharacterAnimParams.Counter, _player.State == PlayerState.Counter);
        _animator.SetBool(CharacterAnimParams.Grind, _player.State == PlayerState.Grind);
        _animator.SetBool(CharacterAnimParams.WallRun, _player.State == PlayerState.WallRun);
        _animator.SetBool(CharacterAnimParams.Dead, _player.State == PlayerState.Dead);
    }

    private void AnimateProcedural()
    {
        float speed = _player.NormalizedSpeed;
        _bob += Time.deltaTime * Mathf.Lerp(6f, 14f, speed);

        Transform leanTarget = _torso != null ? _torso : _visualRoot;
        if (leanTarget != null)
        {
            float lean = _player.TurnLeanDirection * leanAngle;
            float hurt = _player.State == PlayerState.Hurt ? 8f : 0f;
            float slide = _player.IsSliding ? -22f : 0f;
            float air = !_player.IsGrounded ? (_player.IsJumping ? 12f : -6f) : 0f;
            float runBob = _player.IsGrounded && !_player.IsSliding ? Mathf.Sin(_bob) * 3f : 0f;

            if (_torso != null)
                _torso.localRotation = Quaternion.Euler(slide + air + runBob, 0f, -lean - hurt);
            else
                _visualRoot.localRotation = Quaternion.Euler(slide * 0.35f + air * 0.25f, 0f, -lean * 0.6f);
        }

        if (_board != null)
        {
            float pitch = _player.IsSliding ? 18f : (_player.IsGrounded ? Mathf.Sin(_bob * 0.5f) * 4f : -10f);
            _board.localRotation = Quaternion.Euler(pitch, 0f, _player.TurnLeanDirection * 6f);
        }

        if (_visualRoot != null)
            _visualRoot.localPosition = modelOffset + Vector3.up * (_player.IsSliding ? -0.25f : 0f);
    }

    private void ApplyDeckCondition()
    {
        if (_deckRenderers == null || _vitals == null)
            return;

        Color deckTint = _vitals.Hp >= 3
            ? new Color(0.34f, 0.28f, 0.22f)
            : _vitals.Hp >= 2
                ? new Color(0.42f, 0.30f, 0.22f)
                : new Color(0.48f, 0.22f, 0.18f);

        for (int i = 0; i < _deckRenderers.Length; i++)
        {
            Renderer r = _deckRenderers[i];
            if (r == null || r.gameObject.name.IndexOf("Board", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            Material mat = r.sharedMaterial;
            if (mat == null)
                continue;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", deckTint);
            if (mat.HasProperty("_Color"))
                mat.color = deckTint;
        }
    }

    private void HidePlaceholderMesh()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }
}
