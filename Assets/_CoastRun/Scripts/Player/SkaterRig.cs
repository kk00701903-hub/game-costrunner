using UnityEngine;

namespace CoastRun
{
    /// Drives the Mixamo-animated skater (Resources/CoastRun/Rig/Skater + SkaterAnimator)
    /// from gameplay events: ride loop, periodic push kicks, jump on take-off, stumble on
    /// a hit, an upper-body grab on coin/jelly pickups. Attached by CoastPlayerVisual
    /// when the rig exists; the painted billboard remains the fallback.
    public class SkaterRig : MonoBehaviour
    {
        public const string ModelPath = ArtAssets.ResourceRoot + "Rig/Skater";
        public const string ControllerPath = ArtAssets.ResourceRoot + "Rig/SkaterAnimator";

        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashHit = Animator.StringToHash("Hit");
        private static readonly int HashCollect = Animator.StringToHash("Collect");
        private static readonly int HashPush = Animator.StringToHash("Push");
        private static readonly int HashGrounded = Animator.StringToHash("Grounded");
        private static readonly int HashSpeed = Animator.StringToHash("Speed");

        private Animator _anim;
        private PlayerController _player;
        private HealthSystem _health;
        private CoinWallet _wallet;
        private float _pushClock;
        private float _collectCooldown;
        private bool _hasPush;

        public static bool Available =>
            Resources.Load<GameObject>(ModelPath) != null &&
            Resources.Load<RuntimeAnimatorController>(ControllerPath) != null;

        /// Instantiates the rig under `parent`, scaled so the character stands `height` m.
        public static SkaterRig Spawn(Transform parent, float height)
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            var ctrl = Resources.Load<RuntimeAnimatorController>(ControllerPath);
            if (prefab == null || ctrl == null)
                return null;

            var go = Object.Instantiate(prefab, parent, false);
            go.name = "SkaterRig";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // Measure the T-pose and normalise to the requested height.
            var rs = go.GetComponentsInChildren<Renderer>(true);
            float h = 0f;
            foreach (var r in rs)
                h = Mathf.Max(h, r.bounds.max.y - go.transform.position.y);
            if (h > 0.01f)
                go.transform.localScale = Vector3.one * (height / h);

            var anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.updateMode = AnimatorUpdateMode.Normal;

            foreach (var r in rs)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr)
                    smr.updateWhenOffscreen = true;
            }
            // Toon shading with the Mixamo textures kept (every sub-material).
            foreach (var r in rs)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : Color.white;
                    Texture t = mats[i].HasProperty("_BaseMap") ? mats[i].GetTexture("_BaseMap") : mats[i].mainTexture;
                    mats[i] = CoastMaterials.CreateToon(c, t as Texture2D);
                }
                r.sharedMaterials = mats;
            }

            var rig = go.AddComponent<SkaterRig>();
            rig._anim = anim;
            rig._hasPush = HasParameter(anim, "Push");
            return rig;
        }

        private static bool HasParameter(Animator anim, string name)
        {
            foreach (var p in anim.parameters)
                if (p.name == name) return true;
            return false;
        }

        private void Start()
        {
            _player = GetComponentInParent<PlayerController>();
            _health = FindAnyObjectByType<HealthSystem>();
            _wallet = FindAnyObjectByType<CoinWallet>();
            if (_player != null)
            {
                _player.OnJumped += HandleJump;
                _player.OnSoftHit += HandleHit;
            }
            if (_health != null) _health.OnDamaged += HandleDamaged;
            if (_wallet != null) _wallet.OnCoinsChanged += HandleCoins;
            _pushClock = 0.6f;
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnJumped -= HandleJump;
                _player.OnSoftHit -= HandleHit;
            }
            if (_health != null) _health.OnDamaged -= HandleDamaged;
            if (_wallet != null) _wallet.OnCoinsChanged -= HandleCoins;
        }

        private void HandleJump() { if (_anim != null) _anim.SetTrigger(HashJump); }
        private void HandleHit() { if (_anim != null) _anim.SetTrigger(HashHit); }
        private void HandleDamaged(float amount) { HandleHit(); }

        private void HandleCoins(int total, int delta)
        {
            if (delta <= 0 || _anim == null || _collectCooldown > 0f)
                return;
            _anim.SetTrigger(HashCollect);
            _collectCooldown = 0.45f;
        }

        private void Update()
        {
            if (_anim == null || _player == null)
                return;
            float dt = Time.deltaTime;
            _collectCooldown -= dt;

            bool grounded = _player.State != SkateState.Air;
            _anim.SetBool(HashGrounded, grounded);
            _anim.SetFloat(HashSpeed, _player.NormalizedSpeed);

            // Kick every 1.2–1.8 s while cruising on the ground (slower when fast).
            if (_hasPush && grounded && _player.State == SkateState.Run && !_player.IsCrouching)
            {
                _pushClock -= dt;
                if (_pushClock <= 0f)
                {
                    _anim.SetTrigger(HashPush);
                    _pushClock = 1.2f + _player.NormalizedSpeed * 0.6f;
                }
            }
        }
    }
}
