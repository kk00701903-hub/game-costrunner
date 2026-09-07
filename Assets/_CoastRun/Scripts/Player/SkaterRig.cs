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
        public const string RunnerControllerPath = ArtAssets.ResourceRoot + "Rig/RunnerAnimator";

        /// v2 러닝 모드용 컨트롤러(Anim_Run.fbx → RunnerAnimator)가 있는가.
        public static bool RunnerAvailable => Resources.Load<RuntimeAnimatorController>(RunnerControllerPath) != null;

        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashHit = Animator.StringToHash("Hit");
        private static readonly int HashCollect = Animator.StringToHash("Collect");
        private static readonly int HashPush = Animator.StringToHash("Push");
        private static readonly int HashGrounded = Animator.StringToHash("Grounded");
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashHitMirror = Animator.StringToHash("HitMirror");
        // Sideways knock: the whole rig tips away from the impact and eases back.
        private float _tilt, _tiltVel;
        private float _lean, _leanVel, _yaw, _yawVel;

        private Animator _anim;
        private PlayerController _player;
        private HealthSystem _health;
        private CoinWallet _wallet;
        private float _pushClock;
        private float _stepClock;
        private float _pitch, _pitchVel, _bounce, _bounceVel;   // 14차-8: 달리기 기울기·튐
        private int _stepSide = 1;
        private float _collectCooldown;
        private bool _hasPush;

        public static bool Available =>
            Resources.Load<GameObject>(ModelPath) != null &&
            Resources.Load<RuntimeAnimatorController>(ControllerPath) != null;

        /// Instantiates the rig under `parent`, scaled so the character stands `height` m.
        public static SkaterRig Spawn(Transform parent, float height, bool runner = false)
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            var ctrl = runner ? Resources.Load<RuntimeAnimatorController>(RunnerControllerPath) : null;
            if (ctrl == null)
                ctrl = Resources.Load<RuntimeAnimatorController>(ControllerPath);
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
                    // 6차: 계절 옷 — 디퓨즈 텍스처의 계절 변형(Ch46_1001_Diffuse_<SEASON>)이 있으면 교체
                    if (t != null && RunTuning.HasSeason && t.name.StartsWith("Ch46_1001_Diffuse"))
                    {
                        var seasonal = Resources.Load<Texture2D>("CoastRun/Rig/Textures/Ch46_1001_Diffuse_" + SeasonLook.Suffix(RunTuning.Season));
                        if (seasonal != null) t = seasonal;
                    }
                    var toon = CoastMaterials.CreateToon(c, t as Texture2D);
                    // The camera only ever sees her shadow side (sun ahead), so the
                    // default cool shade turned her muddy. A pale warm shade with a low
                    // threshold keeps hair and shirt at key-art brightness.
                    if (toon.HasProperty("_ShadowColor")) toon.SetColor("_ShadowColor", new Color(0.86f, 0.80f, 0.80f));
                    if (toon.HasProperty("_ShadowThreshold")) toon.SetFloat("_ShadowThreshold", 0.22f);
                    mats[i] = toon;
                }
                r.sharedMaterials = mats;
            }

            AttachBackpack(go, anim, height, runner);
            // (14차-2 벙거지는 사용자 요청으로 뺌 — AttachBucketHat 는 남겨 둠, 필요하면 한 줄로 복구)

            var rig = go.AddComponent<SkaterRig>();
            rig._anim = anim;
            rig._hasPush = HasParameter(anim, "Push");
            return rig;
        }

        /// 14차-2: 목표 이미지의 초록 벙거지(해녀 모자 느낌) — 머리 뼈에 얹는다. 크라운 + 챙 + 분홍 띠.
        private static void AttachBucketHat(GameObject go, Animator anim, float height)
        {
            if (anim == null || anim.avatar == null || !anim.avatar.isHuman)
                return;
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head == null || head.Find("BucketHat") != null)
                return;
            float k = height / 1.62f;
            var hat = new GameObject("BucketHat");
            hat.transform.SetParent(head, false);
            Vector3 up = go.transform.up;
            hat.transform.position = head.position + up * (0.16f * k) + go.transform.forward * (0.01f * k);
            hat.transform.rotation = go.transform.rotation * Quaternion.Euler(-6f, 0f, 0f);

            var green = new Color(0.52f, 0.68f, 0.38f);
            var greenDark = new Color(0.40f, 0.55f, 0.30f);
            var pink = new Color(0.98f, 0.62f, 0.72f);
            Material crown = CoastMaterials.CreateToon(green, null, 0.05f);
            Material brim = CoastMaterials.CreateToon(greenDark, null, 0.05f);
            Material band = CoastMaterials.CreateUnlit(pink);
            void Part(string name, PrimitiveType type, Vector3 pos, Vector3 size, Material m)
            {
                var b = GameObject.CreatePrimitive(type);
                b.name = name;
                b.transform.SetParent(hat.transform, false);
                b.transform.localPosition = pos * k;
                b.transform.localScale = size * k;
                CoastEditUtil.DestroyCollider(b);
                var r = b.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                b.AddComponent<CelOutlineHint>();
            }
            Part("Crown", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.23f, 0.055f, 0.23f), crown);
            Part("Top", PrimitiveType.Sphere, new Vector3(0f, 0.07f, 0f), new Vector3(0.23f, 0.10f, 0.23f), crown);
            Part("Band", PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0f), new Vector3(0.235f, 0.012f, 0.235f), band);
            Part("Brim", PrimitiveType.Cylinder, new Vector3(0f, -0.045f, 0.01f), new Vector3(0.34f, 0.008f, 0.34f), brim);
        }

        /// The blue school backpack is part of her silhouette in every painting; the
        /// Mixamo body has none, so it rides on the chest bone (follows every clip).
        private static void AttachBackpack(GameObject go, Animator anim, float height, bool runner = false)
        {
            if (anim == null || anim.avatar == null || !anim.avatar.isHuman)
                return;
            var chest = anim.GetBoneTransform(HumanBodyBones.UpperChest)
                        ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                        ?? anim.GetBoneTransform(HumanBodyBones.Spine);
            if (chest == null)
                return;

            float k = height / 1.62f;
            var pack = new GameObject("Backpack");
            pack.transform.SetParent(chest, false);
            // Bone axes differ per rig, so place in world space using the body's own
            // facing (character root forward) and re-parent keeping that pose.
            Vector3 back = -go.transform.forward;
            Vector3 up = go.transform.up;
            pack.transform.position = chest.position + back * (0.17f * k) + up * (0.03f * k);
            pack.transform.rotation = go.transform.rotation;

            // Cute pastel kit: coral body, cream pocket, cocoa straps, a sunny badge.
            var coral = new Color(1.00f, 0.56f, 0.62f);
            var cream = new Color(1.00f, 0.95f, 0.84f);
            var cocoa = new Color(0.45f, 0.30f, 0.24f);
            var sunny = new Color(1.00f, 0.85f, 0.30f);
            // The sun sits ahead of the runner, so her whole back is in toon shadow and
            // a lit bag went navy. Flat-unlit pastels keep the bag readable from behind
            // (the same trick the painted sprite used); only the straps stay lit.
            Material mat = CoastMaterials.CreateUnlit(coral);
            Material pocket = CoastMaterials.CreateUnlit(cream);
            Material strap = CoastMaterials.CreateToon(cocoa);
            Material badge = CoastMaterials.CreateUnlit(sunny);
            void Part(string name, PrimitiveType type, Vector3 pos, Vector3 size, Material m)
            {
                var b = GameObject.CreatePrimitive(type);
                b.name = name;
                b.transform.SetParent(pack.transform, false);
                b.transform.localPosition = pos * k;
                b.transform.localScale = size * k;
                CoastEditUtil.DestroyCollider(b);
                var r = b.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                b.AddComponent<CelOutlineHint>();
            }
            // Rounded body: a squashed capsule reads as a soft, plump little bag.
            Part("Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(0.27f, 0.17f, 0.15f), mat);
            Part("Lid", PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0f), new Vector3(0.25f, 0.16f, 0.15f), mat);
            Part("Pocket", PrimitiveType.Capsule, new Vector3(0f, -0.06f, -0.075f), new Vector3(0.18f, 0.07f, 0.06f), pocket);
            Part("Badge", PrimitiveType.Sphere, new Vector3(0.07f, 0.06f, -0.085f), new Vector3(0.05f, 0.05f, 0.03f), badge);
            Part("StrapL", PrimitiveType.Cube, new Vector3(-0.09f, 0.02f, 0.12f), new Vector3(0.045f, 0.32f, 0.11f), strap);
            Part("StrapR", PrimitiveType.Cube, new Vector3(0.09f, 0.02f, 0.12f), new Vector3(0.045f, 0.32f, 0.11f), strap);

            // Chibi touch: a slightly bigger head like the painted key art. Humanoid
            // clips never write bone scale, so this sticks through every animation.
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                head.localScale = Vector3.one * (runner ? 1.32f : 1.22f);
            // 손·발도 살짝 크게 — 치비 비율(러닝 모드는 보드가 없어 발이 더 보인다).
            foreach (var hb in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                var t = anim.GetBoneTransform(hb);
                if (t != null) t.localScale = Vector3.one * 1.12f;
            }
            foreach (var fb in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var t = anim.GetBoneTransform(fb);
                if (t != null) t.localScale = Vector3.one * (runner ? 1.18f : 1.08f);
            }
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
        private void HandleHit()
        {
            if (_anim == null) return;
            bool bounce = _player != null && _player.LastHitKind == HitKind.Bounce && _player.LastBounceDir != 0;
            int dir = bounce ? _player.LastBounceDir : 0;
            if (HasParameter(_anim, "HitMirror"))
                _anim.SetBool(HashHitMirror, dir > 0);
            _anim.SetTrigger(HashHit);
            // Tip away from the thing she hit (dir is the side she deflects to).
            _tilt = bounce ? -dir * 22f : 0f;
        }
        private void HandleDamaged(float amount) { /* HealthSystem fires after OnSoftHit; the stumble is already playing */ }

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

            _tilt = Mathf.SmoothDamp(_tilt, 0f, ref _tiltVel, 0.28f, 400f, dt);
            // 7차: 레인 이동 중 몸을 진행 방향으로 살짝 기울이고(roll) 고개를 돌린다(yaw) — 미끄러지듯 옆으로.
            float lv = _player.LateralVelocity;
            float leanTarget = Mathf.Clamp(-lv * 2.2f, -14f, 14f);
            float yawTarget = Mathf.Clamp(lv * 3.0f, -18f, 18f);
            _lean = Mathf.SmoothDamp(_lean, leanTarget, ref _leanVel, 0.10f, 800f, dt);
            _yaw = Mathf.SmoothDamp(_yaw, yawTarget, ref _yawVel, 0.10f, 800f, dt);
            bool grounded = _player.State != SkateState.Air;
            bool running = grounded && _player.State == SkateState.Run;
            _anim.SetBool(HashGrounded, grounded);
            _anim.SetFloat(HashSpeed, _player.NormalizedSpeed);
            // 14차-8: 발이 땅을 '차고' 나가는 느낌 — 클립(조깅 보폭 ≈ 3 m/s)을 실제 속도에 맞춰 더 빨리 돌리고,
            // 속도에 따라 몸을 앞으로 기울이며, 발 디딜 때마다 몸이 위아래로 튄다. 공중·피격 중엔 1.0.
            if (running)
                _anim.speed = Mathf.Clamp(_player.Speed / 7.5f, 1.0f, 1.9f);
            else
                _anim.speed = 1f;
            float pitch = running ? 5f + 7f * _player.NormalizedSpeed : 0f;   // 앞으로 기울기
            _pitch = Mathf.SmoothDamp(_pitch, pitch, ref _pitchVel, 0.25f, 200f, dt);
            // 발 디딤: 주기(클립 0.73 s)의 절반마다 — 먼지 + 튐(스텝 직후 살짝 내려앉았다 올라온다)
            float stepPeriod = 0.365f;
            if (running)
            {
                _stepClock += Time.deltaTime * _anim.speed;
                if (_stepClock >= stepPeriod)
                {
                    _stepClock -= stepPeriod;
                    _stepSide = -_stepSide;
                    JuiceDirector.Instance?.PuffStep(transform.position + transform.right * (0.14f * _stepSide));
                    _bounceVel = -0.55f;   // 착지 충격: 아래로
                }
            }
            // 스프링: 착지 → 살짝 내려앉음 → 튕겨 올라옴
            _bounceVel += (-_bounce * 260f - _bounceVel * 18f) * dt;
            _bounce += _bounceVel * dt;
            float side = running ? Mathf.Sin(_stepClock / stepPeriod * Mathf.PI) * 0.6f * _stepSide : 0f;   // 어깨 좌우 흔들림(°)
            transform.localRotation = Quaternion.Euler(_pitch, _yaw, _tilt + _lean + side);
            transform.localPosition = new Vector3(0f, running ? Mathf.Clamp(_bounce, -0.05f, 0.04f) : 0f, 0f);

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
