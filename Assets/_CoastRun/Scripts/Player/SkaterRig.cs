using UnityEngine;

namespace CoastRun
{
    /// Drives the Mixamo-animated skater (Resources/CoastRun/Rig/Skater + SkaterAnimator)
    /// from gameplay events: ride loop, periodic push kicks, jump on take-off, stumble on
    /// a hit, an upper-body grab on coin/jelly pickups. Attached by CoastPlayerVisual
    /// when the rig exists; the painted billboard remains the fallback.
    public class SkaterRig : MonoBehaviour
    {
        /// 121차(사용자 시안): 러닝 캐릭터를 치비 하늘(Tools/blender/haneul_chibi_rig.py → Rig/HaneulChibi.fbx, Humanoid)로. 클립은 그대로 리타겟.
        public const string ModelPath = ArtAssets.ResourceRoot + "Rig/HaneulChibi";
        public const string LegacyModelPath = ArtAssets.ResourceRoot + "Rig/Skater";
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
        private static readonly int HashDoubleJump = Animator.StringToHash("DoubleJump");   // 48차-9
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
        public static SkaterRig Spawn(Transform parent, float height, bool runner = false) => SpawnModel(ModelPath, parent, height, runner);

        /// 138차: 다른 치비 모델(Rig/KidChibi 등)도 같은 컨트롤러로 세운다.
        public static SkaterRig SpawnModel(string modelPath, Transform parent, float height, bool runner = false)
        {
            var prefab = Resources.Load<GameObject>(modelPath);
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
                    CoastMaterials.SetShadow(toon, new Color(0.86f, 0.80f, 0.80f), 0.22f);   // 25차-1: 팔레트 갱신에도 유지
                    toon.name = mats[i].name;   // 167차: 원래 재질 이름 유지 — FaceDecal 이 HN_Blush/KD_Blush 등 얼굴 부품을 이름으로 찾는다(텍스처 없는 재질은 이름이 유일한 단서)
                    mats[i] = toon;
                }
                r.sharedMaterials = mats;
            }

            // 121차: 치비 모델은 가방을 메고 나온다 — 절차 가방(AttachBackpack)은 옛 Mixamo 몸에서만.
            if (prefab.name != "HaneulChibi" && prefab.name != "KidChibi") AttachBackpack(go, anim, height, runner);
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
                _player.OnDoubleJumped += HandleDoubleJump;   // 47차
                _player.OnSoftHit += HandleHit;
                _player.OnLanded += HandleLanded;      // 27차
                _player.OnCrouched += HandleCrouched;  // 27차
                _player.OnLaneChanged += HandleLane;   // 27차
            }
            _rootScale = transform.localScale;
            var chest = _anim != null ? (_anim.GetBoneTransform(HumanBodyBones.UpperChest)
                        ?? _anim.GetBoneTransform(HumanBodyBones.Chest)
                        ?? _anim.GetBoneTransform(HumanBodyBones.Spine)) : null;
            _bag = chest != null ? chest.Find("Backpack") : null;
            if (_bag != null) _bagRest = _bag.localRotation;
            _head = _anim != null ? _anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (_head != null) _headRestScale = _head.localScale;
            if (_head != null) _headRestLocalRot = _head.localRotation;   // 121차: 치비 큰 머리 — 클립의 고개 회전을 절반만
            // 132차: 머리카락 뼈(치비 리그 HairBack/HairL/HairR — Humanoid 밖) — 있으면 LateUpdate 스프링으로 흩날린다
            _hairBack = FindDeep(_anim.transform, "HairBack"); _hairL = FindDeep(_anim.transform, "HairL"); _hairR = FindDeep(_anim.transform, "HairR");
            if (_hairBack != null) _hairBackRest = _hairBack.localRotation;
            if (_hairL != null) _hairLRest = _hairL.localRotation;
            if (_hairR != null) _hairRRest = _hairR.localRotation;
            if (_health != null) _health.OnDamaged += HandleDamaged;
            if (_wallet != null) _wallet.OnCoinsChanged += HandleCoins;
            _pushClock = 0.6f;
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnJumped -= HandleJump;
                _player.OnDoubleJumped -= HandleDoubleJump;
                _player.OnSoftHit -= HandleHit;
                _player.OnLanded -= HandleLanded;
                _player.OnCrouched -= HandleCrouched;
                _player.OnLaneChanged -= HandleLane;
            }
            if (_health != null) _health.OnDamaged -= HandleDamaged;
            if (_wallet != null) _wallet.OnCoinsChanged -= HandleCoins;
        }

        // ── 27차: 톰 히어로식 쫀득 모션 ────────────────────────────────────
        // 스프링 하나(_squash, 1 = 원래 크기)가 위치(_bounce)와 스케일을 함께 만든다. 두 개를 따로 흔들면 과해진다.
        //   도약: 세로로 늘어남(jumpStretch) → 공중에서 1로 복귀
        //   착지: 납작(landSquash) → 스프링이 1을 지나쳐 살짝 커졌다가(overshoot) 가라앉는다
        //   걸음: 착지 충격을 스케일에도 조금(stepSquash) — 통통 걷는 느낌
        //   레인: 반대쪽으로 순간 기울었다 복귀(_laneKick), 가방·머리는 한 박자 늦게(secondary 스프링)
        private float _squash = 1f, _squashVel;
        private float _laneKick, _laneKickVel;
        private Vector3 _rootScale = Vector3.one;
        private Transform _bag, _head;
        private Quaternion _headRestLocalRot = Quaternion.identity;
        // 132차: 머리카락 스프링(피치 = 앞뒤 흩날림, 롤 = 좌우)
        private Transform _hairBack, _hairL, _hairR;
        private Quaternion _hairBackRest = Quaternion.identity, _hairLRest = Quaternion.identity, _hairRRest = Quaternion.identity;
        private float _hbPitch, _hbPitchVel, _hbRoll, _hbRollVel, _hsRoll, _hsRollVel, _hsPitch, _hsPitchVel;
        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var f = FindDeep(root.GetChild(i), name); if (f != null) return f; }
            return null;
        }
        /// 132차: 머리카락 — 달리는 속도만큼 뒤로 날리고(피치), 걸음 스프링(_bounceVel)·레인 킥(_laneKick)에 반 박자 늦게 따라오며 바람에 파르르(사인 두 겹).
        private void TickHair(float dt, bool running)
        {
            if (_hairBack == null && _hairL == null && _hairR == null) return;
            float spd = _player != null ? _player.NormalizedSpeed : 0f;
            bool air = _player != null && _player.State == SkateState.Air;
            float t = Time.time;
            float flutter = Mathf.Sin(t * 13f) * 5f + Mathf.Sin(t * 7.3f + 1f) * 3.5f + Mathf.Sin(t * 2.3f) * 2f;   // 134차: 진폭 ↑
            float wind = running ? 14f + 30f * spd : air ? 34f : 0f;                       // 뒤로 젖혀지는 기본 각(134차 ↑)
            float bob = Mathf.Clamp(-_bounceVel * 30f, -18f, 24f);                          // 착지 순간 위로 들썩(134차 ↑)
            float pitchT = wind + bob + flutter * (0.4f + spd);
            float lv = _player != null ? _player.LateralVelocity : 0f;
            float rollT = Mathf.Clamp(-lv * 4.5f, -28f, 28f) + _laneKick * 0.8f + Mathf.Sin(t * 9f) * 2.5f * spd + Mathf.Sin(t * 3.1f) * 3f * spd;
            for (float rem = Mathf.Min(dt, 0.25f); rem > 0f;)
            {
                float h = Mathf.Min(rem, 1f / 90f); rem -= h;
                _hbPitchVel += ((pitchT - _hbPitch) * 150f - _hbPitchVel * 10f) * h; _hbPitch += _hbPitchVel * h;
                _hbRollVel += ((rollT - _hbRoll) * 150f - _hbRollVel * 10f) * h; _hbRoll += _hbRollVel * h;
                _hsRollVel += ((rollT * 0.9f + flutter * 0.8f - _hsRoll) * 170f - _hsRollVel * 11f) * h; _hsRoll += _hsRollVel * h;
                _hsPitchVel += ((wind * 0.45f + bob * 0.6f - _hsPitch) * 170f - _hsPitchVel * 11f) * h; _hsPitch += _hsPitchVel * h;
            }
            if (float.IsNaN(_hbPitch) || float.IsNaN(_hbRoll) || float.IsNaN(_hsRoll) || float.IsNaN(_hsPitch)) { _hbPitch = _hbRoll = _hsRoll = _hsPitch = 0f; _hbPitchVel = _hbRollVel = _hsRollVel = _hsPitchVel = 0f; }
            _hbPitch = Mathf.Clamp(_hbPitch, -22f, 55f); _hbRoll = Mathf.Clamp(_hbRoll, -36f, 36f);
            _hsRoll = Mathf.Clamp(_hsRoll, -34f, 34f); _hsPitch = Mathf.Clamp(_hsPitch, -14f, 36f);
            // 월드 축 기준으로 얹는다(뼈 로컬 축 방향에 무관): 피치 = 몸의 오른쪽 축, 롤 = 진행 축
            Vector3 right = transform.right, fwd = transform.forward;
            if (_hairBack != null) { _hairBack.localRotation = _hairBackRest; _hairBack.rotation = Quaternion.AngleAxis(_hbPitch, right) * Quaternion.AngleAxis(_hbRoll, fwd) * _hairBack.rotation; }
            if (_hairL != null) { _hairL.localRotation = _hairLRest; _hairL.rotation = Quaternion.AngleAxis(_hsPitch, right) * Quaternion.AngleAxis(_hsRoll - 4f * spd, fwd) * _hairL.rotation; }
            if (_hairR != null) { _hairR.localRotation = _hairRRest; _hairR.rotation = Quaternion.AngleAxis(_hsPitch, right) * Quaternion.AngleAxis(_hsRoll + 4f * spd, fwd) * _hairR.rotation; }
        }
        private Quaternion _bagRest = Quaternion.identity;
        private Vector3 _headRestScale = Vector3.one;
        private float _bagPitch, _bagPitchVel, _bagRoll, _bagRollVel;
        private RunConfig Cfg => _player != null ? _player.Config : null;

        private void HandleJump()
        {
            if (_anim != null)
            {
                // 골인 직후 speed=0 잔여가 있으면 점프 클립이 안 돈다
                if (_anim.speed < 0.01f) _anim.speed = 1f;
                _anim.SetBool(HashGrounded, false);
                // 48차-9: 2단 점프 직후의 OnJumped 는 Jump 클립을 다시 틀지 않는다(DoubleJump 클립이 이미 트리거됨)
                if (!(_djClip && _player != null && _player.DoubleJumpUsed)) _anim.SetTrigger(HashJump);
            }
            var c = Cfg; float stretch = c != null ? c.jumpStretch : 1.16f;
            _squash = Mathf.Max(_squash, stretch); _squashVel = 2.2f;   // 위로 쭉
        }
        // ── 47차: 2단 점프 「허공 디딤」 ─────────────────────────────────
        // 공중에서 두 번째 점프: 0.1초 웅크렸다(무릎 두 개 가슴으로) → 오른발로 허공을 딛듯 아래로 쭉 뻗고 왼무릎·양팔은 위로. 0.45초 뒤 원래 Air 포즈로.
        private float _djTimer;
        private const float DjDur = 0.45f;
        private bool _djClip;   // 48차-9: 컨트롤러에 DoubleJump 클립(Blender 앞돌기)이 있으면 절차적 포즈 대신 클립
        // 122차(사용자 「이단점프할때 한바퀴 돌게」): 2단 점프 순간부터 리그 전체를 앞으로 360° 공중제비(엉덩이 제자리, 빨래줄 스핀과 같은 방식).
        private float _djSpinT = -1f;                // <0: 안 도는 중
        private const float DjSpinSeconds = 0.7f;    // 132차: 더 높이 뛰는 만큼 천천히 한 바퀴(부드럽게)
        private void HandleDoubleJump()
        {
            _djClip = _anim != null && HasParameter(_anim, "DoubleJump");
            if (_djClip) { _anim.ResetTrigger(HashJump); _anim.SetTrigger(HashDoubleJump); _djTimer = 0f; }
            else _djTimer = DjDur;
            _djSpinT = 0f;
#if UNITY_EDITOR
            Debug.LogWarning("[DJ] double jump: clip=" + _djClip + " t=" + Time.time.ToString("F2"));
#endif
            _squash = Mathf.Min(_squash, 0.84f); _squashVel = 3.4f;   // 살짝 움츠렸다 위로 쭉
            _bagPitchVel += 200f;
            // 48차-11: 엉덩이(Hips 뼈) 뒤·아래에서 뿜는다 + 리그를 따라오는 제트 꼬리
            var hipsT = _anim != null ? _anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            Vector3 fwd = transform.parent != null ? transform.parent.forward : transform.forward;
            Vector3 butt = (hipsT != null ? hipsT.position : transform.position + Vector3.up * 0.9f) - fwd * 0.18f + Vector3.down * 0.12f;
            JuiceDirector.Instance?.OnDoubleJump(butt, transform);
        }
        private void DoubleJumpPoseLate()
        {
            if (_djTimer <= 0f || _anim == null) return;
            float u = 1f - _djTimer / DjDur;                 // 0 → 1
            float tuck = Mathf.Clamp01(1f - u / 0.28f);      // 앞 28%: 웅크림
            float push = Mathf.Clamp01((u - 0.22f) / 0.30f); // 22~52%: 밀어 차기, 그 뒤 유지하다가
            float fade = Mathf.Clamp01((1f - u) / 0.25f);    // 마지막 25%: 원래 포즈로
            float k = Mathf.Max(tuck, push) * fade;
            if (k <= 0.001f) return;
            Vector3 fwd = transform.parent != null ? transform.parent.forward : transform.forward;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, fwd).normalized;
            var lT = _anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);  var lS = _anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);  var lF = _anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rT = _anim.GetBoneTransform(HumanBodyBones.RightUpperLeg); var rS = _anim.GetBoneTransform(HumanBodyBones.RightLowerLeg); var rF = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
            var lUp = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);  var lLo = _anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);  var lH = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
            var rUp = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm); var rLo = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm); var rH = _anim.GetBoneTransform(HumanBodyBones.RightHand);
            // 무릎 위로(웅크림): 허벅지 앞·위, 정강이 아래·뒤
            Vector3 kneeUp = (fwd * 0.9f + up * 0.55f).normalized;
            Vector3 shinTuck = (-up * 0.8f - fwd * 0.5f).normalized;
            // 허공 디딤(오른발): 허벅지 아래·살짝 앞, 정강이 곧게 아래 — 발바닥으로 공기를 누른다
            Vector3 stomp = (-up * 0.95f + fwd * 0.18f).normalized;
            float wL = tuck * fade + push * fade;   // 왼다리는 계속 올린다
            float wR = k;
            if (lT != null && lS != null) Aim(lT, lS, kneeUp - right * 0.06f, wL * 0.9f);
            if (lS != null && lF != null) Aim(lS, lF, shinTuck - right * 0.03f, wL * 0.9f);
            Vector3 rThighDir = Vector3.Slerp(kneeUp, stomp, push);
            Vector3 rShinDir = Vector3.Slerp(shinTuck, stomp, push);
            if (rT != null && rS != null) Aim(rT, rS, rThighDir + right * 0.06f, wR * 0.9f);
            if (rS != null && rF != null) Aim(rS, rF, rShinDir + right * 0.03f, wR * 0.9f);
            // 팔: 웅크릴 땐 몸 앞으로 모았다가, 밀어 찰 때 두 팔 위로 번쩍
            Vector3 armIn = (fwd * 0.6f - up * 0.3f).normalized;
            Vector3 armUp = (up * 0.9f + fwd * 0.25f).normalized;
            Vector3 aDir = Vector3.Slerp(armIn, armUp, push);
            float wA = k * 0.85f;
            if (rUp != null && rLo != null) Aim(rUp, rLo, aDir + right * 0.35f, wA);
            if (rLo != null && rH != null) Aim(rLo, rH, aDir + right * 0.15f, wA);
            if (lUp != null && lLo != null) Aim(lUp, lLo, aDir - right * 0.35f, wA);
            if (lLo != null && lH != null) Aim(lLo, lH, aDir - right * 0.15f, wA);
        }

        private void HandleLanded()
        {
            var c = Cfg; float sq = c != null ? c.landSquash : 0.78f;
            _squash = Mathf.Min(_squash, sq); _squashVel = -3.5f;      // 납작 → 스프링이 튕겨 올린다
            _bagPitchVel += 260f;                                       // 가방이 앞으로 쏠린다
        }
        private void HandleCrouched()
        {
            var c = Cfg; float sq = c != null ? c.crouchSquash : 0.86f;
            _squash = Mathf.Min(_squash, sq); _squashVel = -1.5f;
            _bagPitchVel += 160f;
        }
        private void HandleLane(int dir)
        {
            _laneKickVel += -dir * 150f;   // 반대쪽으로 순간 기울었다 돌아온다(관성) · 130차: 220 → 150 부드럽게
            _bagRollVel += dir * 300f;
        }

        /// 스프링 적분. 매 Update 끝에서 호출. 반환: 이번 프레임 스케일 배율(y)
        private float TickJuice(float dt, bool running)
        {
            var c = Cfg;
            float k = c != null ? c.squashStiffness : 260f, d = c != null ? c.squashDamping : 14f;
            float stepAmt = c != null ? c.stepSquash : 1.2f;
            float sec = c != null ? c.secondaryAmount : 1f;
            // 걸음 착지 스프링(_bounce, m)을 스케일에도 반영: 내려앉을 때 납작
            float target = 1f + (running ? _bounce * stepAmt : 0f);
            for (float r2 = Mathf.Min(dt, 0.25f); r2 > 0f;)
            {
                float h = Mathf.Min(r2, 1f / 90f); r2 -= h;
                _squashVel += ((target - _squash) * k - _squashVel * d) * h;
                _squash += _squashVel * h;
            }
            _squash = Mathf.Clamp(_squash, 0.6f, 1.35f);
            // 66차-7(사용자 「러닝할 때 사람이 옆으로 돌아간다」): 스프링을 명시적 오일러로 한 번에 적분하면
            // 프레임이 튀거나(로딩·GC·에디터 렉) timescale이 높을 때 dt 가 0.1 s 를 넘어 발산 → 몸이 옆으로 누움/NaN.
            // → 1/90 s 이하로 잘게 나눠 적분하고 값도 안전 범위로 묶는다.
            float bagPitchTarget = Mathf.Clamp(-_bounceVel * 40f, -25f, 25f) * sec;
            float rem = Mathf.Min(dt, 0.25f);
            while (rem > 0f)
            {
                float h = Mathf.Min(rem, 1f / 90f); rem -= h;
                // 레인 킥(°): 임계 감쇠보다 살짝 덜 감쇠 → 한 번 튕김
                _laneKickVel += (-_laneKick * 240f - _laneKickVel * 19f) * h;   // 130차: 덜 튕기게(감쇠 ↑)
                _laneKick += _laneKickVel * h;
                // 가방 secondary: 세로 가속(_bounceVel)과 레인 킥을 따라 늦게 움직인다
                _bagPitchVel += ((bagPitchTarget - _bagPitch) * 180f - _bagPitchVel * 11f) * h;
                _bagPitch += _bagPitchVel * h;
                _bagRollVel += ((-_laneKick * 1.4f * sec - _bagRoll) * 200f - _bagRollVel * 10f) * h;
                _bagRoll += _bagRollVel * h;
            }
            if (float.IsNaN(_laneKick) || float.IsInfinity(_laneKick)) { _laneKick = 0f; _laneKickVel = 0f; }
            _laneKick = Mathf.Clamp(_laneKick, -28f, 28f); _laneKickVel = Mathf.Clamp(_laneKickVel, -900f, 900f);
            if (float.IsNaN(_bagPitch) || float.IsNaN(_bagRoll)) { _bagPitch = _bagRoll = 0f; _bagPitchVel = _bagRollVel = 0f; }
            if (float.IsNaN(_squash)) { _squash = 1f; _squashVel = 0f; }
            return _squash;
        }

        private void ApplyJuiceLate()
        {
            if (_glideBlend > 0.001f || _finishBlend > 0.001f)
            {
                float g = _player != null ? Mathf.Max(0.5f, _player.VisualScaleMul) : 1f;
                transform.localScale = _rootScale * g;
                return;
            }
            float s = _squash;
            float xz = 1f / Mathf.Sqrt(Mathf.Max(0.3f, s));   // 부피 보존
            float mul = _player != null ? Mathf.Max(0.5f, _player.VisualScaleMul) : 1f;
            transform.localScale = new Vector3(_rootScale.x * xz * mul, _rootScale.y * s * mul, _rootScale.z * xz * mul);
            if (_bag != null)
                _bag.localRotation = _bagRest * Quaternion.Euler(Mathf.Clamp(_bagPitch, -30f, 30f), 0f, Mathf.Clamp(_bagRoll, -25f, 25f));
            if (_head != null)
            {
                // 머리는 몸보다 덜 찌그러지고(치비 비율 유지) 반 박자 늦게: 스케일 역보정 + 가방 롤의 절반
                float hs = Mathf.Lerp(1f, 1f / s, 0.5f);
                _head.localScale = new Vector3(_headRestScale.x * hs * (1f / xz), _headRestScale.y * hs, _headRestScale.z * hs * (1f / xz));
                // 121차: Mixamo 클립의 고개 숙임은 사람 머리 기준 — 치비 큰 머리에선 과해서 45% 만 남긴다
                _head.localRotation = Quaternion.Slerp(_headRestLocalRot, _head.localRotation, 0.45f);
            }
            TickHair(Time.deltaTime, _player != null && _player.State == SkateState.Run && _player.Speed > 0.5f);   // 132차
        }
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

        private float _glideBlend;
        private float _glideT; private bool _wasGliding;
        private float _finishBlend; private bool _finishPose;
        /// 골인 시 뒤돌기 포즈 — 사용자 요청으로 비활성. SettleFacingForward 만 쓴다.
        public void SetFinishPose(bool on)
        {
            _finishPose = on;
            if (!on)
            {
                _finishBlend = 0f;
                if (_anim != null) _anim.speed = 1f;
            }
        }

        /// 골인: 활공/뒤돌기 없이 정면·직립으로 고정(애니만 멈춤).
        public void SettleFacingForward()
        {
            _finishPose = false;
            _finishBlend = 0f;
            _glideBlend = 0f;
            _glideT = 0f;
            _wasGliding = false;
            _yaw = 0f; _yawVel = 0f;
            _lean = 0f; _leanVel = 0f;
            _pitch = 0f; _pitchVel = 0f;
            _tilt = 0f; _tiltVel = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            if (_anim != null)
            {
                _anim.SetBool(HashGrounded, true);
                _anim.SetFloat(HashSpeed, 0f);
                _anim.speed = 0f;
            }
        }
        private const float SpinSeconds = 0.6f;
        private Vector3 _hipRest;

        private void CacheHipRest()
        {
            if (_anim == null || _anim.avatar == null || !_anim.avatar.isHuman) { _hipRest = new Vector3(0f, 0.9f, 0f); return; }
            var hips = _anim.GetBoneTransform(HumanBodyBones.Hips);
            _hipRest = hips != null ? transform.InverseTransformPoint(hips.position) : new Vector3(0f, 0.9f, 0f);
            if (_hipRest.sqrMagnitude < 0.01f) _hipRest = new Vector3(0f, 0.9f, 0f);
        }

        /// 뼈 방향 강제: 현재 뼈 방향(뼈→자식)을 원하는 월드 방향으로 돌린다 — 리그 축 규약과 무관.
        /// 132차: 발끝(toes 또는 발 뼈의 첫 자식)이 진행 방향에서 벗어난 각도만큼 발을 되돌린다(세로축 회전만).
        private void FixFootYaw(Transform foot, Transform toes, Vector3 runFwd, bool inJumpClip, bool isLeft)
        {
            if (foot == null) return;
            if (toes == null && foot.childCount > 0) toes = foot.GetChild(0);
            float gain = inJumpClip ? 0.35f : 0.9f;
            float fix;
            if (toes != null)
            {
                Vector3 d = toes.position - foot.position; d.y = 0f;
                if (d.sqrMagnitude < 1e-6f) return;
                float err = Vector3.SignedAngle(runFwd, d.normalized, Vector3.up);   // −: 왼쪽으로 벌어짐
                if (Mathf.Abs(err) > 60f) return;   // 발이 뒤로 접힌 순간(뒤꿈치 차기)엔 건드리지 않는다
                fix = -err * gain;
            }
            else fix = isLeft ? leftFootYawFix * (inJumpClip ? 0.35f : 1f) : 0f;
            foot.rotation = Quaternion.AngleAxis(fix, Vector3.up) * foot.rotation;
        }

        private static void Aim(Transform bone, Transform child, Vector3 worldDir, float weight)
        {
            if (bone == null || child == null) return;
            Vector3 cur = child.position - bone.position;
            if (cur.sqrMagnitude < 1e-6f) return;
            var target = Quaternion.FromToRotation(cur.normalized, worldDir.normalized) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, target, weight);
        }

        [SerializeField] private float glideToeForward = 0.55f;   // 31차: 활공 때 발끝 방향(1 = 완전 앞, 0 = 완전 아래)
        [SerializeField] private float leftFootYawFix = 22f;   // 22차-4: 달릴 때 왼발이 바깥(왼쪽)으로 벌어져 보임 → 앞을 보게 안쪽으로

        private void LateUpdate()
        {
            if (_anim == null || _anim.avatar == null || !_anim.avatar.isHuman) return;
            ApplyJuiceLate();   // 27차: 애니메이터가 뼈를 쓴 뒤에 스케일·가방·머리를 얹는다
            if (_finishBlend > 0.001f)
            {
                FinishPoseLate();
                return;
            }
            if (_glideBlend <= 0.001f)
            {
                // 25차-2: 점프 클립에서 왼다리가 바깥으로 벌어지는 문제 보정.
                // 40차: Air 전체에서 허벅지를 거울상으로 덮어쓰면 Jump 클립 자체가 죽어
                // '옛 점프'처럼 보인다 → Jump 스테이트일 땐 클립을 그대로 두고, 발끝 yaw만 고친다.
                bool inJumpClip = false;
                if (_anim != null)
                {
                    var st = _anim.GetCurrentAnimatorStateInfo(0);
                    inJumpClip = st.IsName("Jump") || st.IsTag("Jump") || st.IsName("DoubleJump");
                }
                if (_djTimer > 0f && _player != null && _player.State == SkateState.Air)
                {
                    // 47차: 2단 점프 허공 디딤 — 이 동안은 다리 거울 보정 대신 디딤 포즈
                    DoubleJumpPoseLate();
                }
                else if (_player != null && _player.State == SkateState.Air && !inJumpClip)
                {
                    var lT = _anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);  var lS = _anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);  var lF = _anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var rT = _anim.GetBoneTransform(HumanBodyBones.RightUpperLeg); var rS = _anim.GetBoneTransform(HumanBodyBones.RightLowerLeg); var rF = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
                    if (lT != null && lS != null && rT != null && rS != null)
                    {
                        Vector3 runFwd = transform.parent != null ? transform.parent.forward : transform.forward;
                        Vector3 side = Vector3.Cross(Vector3.up, runFwd).normalized;
                        Vector3 rDir = rS.position - rT.position;
                        Vector3 mirrored = rDir - 2f * Vector3.Dot(rDir, side) * side;   // 좌우 대칭
                        // 완전 대칭이면 딱딱해 보이니 0.7 만 섞고, 다리 사이는 엉덩이 폭만큼 유지
                        Aim(lT, lS, mirrored - side * 0.06f, 0.7f);
                        if (lF != null && rF != null)
                        {
                            Vector3 rDir2 = rF.position - rS.position;
                            Aim(lS, lF, rDir2 - 2f * Vector3.Dot(rDir2, side) * side, 0.7f);
                        }
                    }
                }
                // 22차-4: 왼발 방향 보정 — 발끝(toes)이 있으면 진행 방향과의 편차를 재서 되돌리고, 없으면 고정 각도.
                // 132차(사용자 「왼쪽 발이 왼쪽으로 돌아가 있다」): **양발** 모두, 발끝은 Humanoid Toes 가 안 매핑돼도
                //   발 뼈의 첫 자식(치비 리그 *ToeBase)으로 잰다 → 고정 각도(22°)에 기대지 않고 실제 편차만큼 앞을 보게.
                if (_player != null && (_player.Speed > 0.5f || _player.State == SkateState.Air))
                {
                    Vector3 runFwd = transform.parent != null ? transform.parent.forward : transform.forward;
                    FixFootYaw(_anim.GetBoneTransform(HumanBodyBones.LeftFoot), _anim.GetBoneTransform(HumanBodyBones.LeftToes), runFwd, inJumpClip, true);
                    FixFootYaw(_anim.GetBoneTransform(HumanBodyBones.RightFoot), _anim.GetBoneTransform(HumanBodyBones.RightToes), runFwd, inJumpClip, false);
                }
                return;
            }
            float k = _glideBlend;
            Vector3 fwd = transform.parent != null ? transform.parent.forward : transform.forward;   // 진행 방향(몸을 눕혀도 변하지 않는 축)
            Vector3 up = Vector3.up;
            // 팔: 어깨→팔꿈치→손 모두 진행 방향으로, 양팔은 어깨 폭만큼 살짝 벌어지게
            var lUp = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);  var lLo = _anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);  var lH = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
            var rUp = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm); var rLo = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm); var rH = _anim.GetBoneTransform(HumanBodyBones.RightHand);
            Vector3 right = Vector3.Cross(up, fwd).normalized;
            // 22차-6: 양손 슈퍼맨 — 두 팔 다 앞으로 곧게(주먹 나란히), 살짝 위로
            Aim(rUp, rLo, (fwd + right * 0.10f + up * 0.14f), k);
            Aim(rLo, rH, (fwd + right * 0.04f + up * 0.10f), k);
            Aim(lUp, lLo, (fwd - right * 0.10f + up * 0.14f), k);
            Aim(lLo, lH, (fwd - right * 0.04f + up * 0.10f), k);
            // 다리: 뒤로 곧게, 발끝은 살짝 위로
            var lThigh = _anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);  var lShin = _anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);  var lFoot = _anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rThigh = _anim.GetBoneTransform(HumanBodyBones.RightUpperLeg); var rShin = _anim.GetBoneTransform(HumanBodyBones.RightLowerLeg); var rFoot = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
            Vector3 back = -fwd - up * 0.12f;
            Aim(lThigh, lShin, back - right * 0.05f, k);
            Aim(lShin, lFoot, back - right * 0.03f + up * 0.05f, k);
            Aim(rThigh, rShin, back + right * 0.05f, k);
            Aim(rShin, rFoot, back + right * 0.03f + up * 0.05f, k);
            // 31차: 발끝 — 왼발이 바깥(왼쪽)으로 꺾여 보이던 것. 양발 다 발끝을 진행 방향(앞) 쪽으로, 좌우 성분 없이.
            var lToes = _anim.GetBoneTransform(HumanBodyBones.LeftToes); var rToes = _anim.GetBoneTransform(HumanBodyBones.RightToes);
            Vector3 toeDir = (fwd * glideToeForward - up * (1f - glideToeForward)).normalized;
            if (lToes != null) Aim(lFoot, lToes, toeDir, k);
            if (rToes != null) Aim(rFoot, rToes, toeDir, k);
            // 고개: 앞을 본다
            var head = _anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(fwd + up * 0.35f, up), k * 0.8f);
        }

        /// 골인 포즈(LateUpdate): 몸은 이미 180° 돌아 카메라를 본다. 오른팔 하늘로, 왼팔은 팔꿈치 굽혀 손을 허리에, 고개 갸웃.
        private void FinishPoseLate()
        {
            float k = _finishBlend * _finishBlend;
            Vector3 up = Vector3.up;
            Vector3 face = transform.forward;               // 돌아선 뒤의 정면(카메라 쪽)
            Vector3 right = Vector3.Cross(up, face).normalized;
            var rUp = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm); var rLo = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm); var rH = _anim.GetBoneTransform(HumanBodyBones.RightHand);
            var lUp = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);  var lLo = _anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);  var lH = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
            // 오른손: 위로 쭉(살짝 바깥·앞) — 23차-1: 손목까지 같은 방향으로 펴서 꺾인 손이 없게, 손바닥은 앞
            Aim(rUp, rLo, (up + right * 0.30f + face * 0.08f), k);
            Aim(rLo, rH, (up + right * 0.22f + face * 0.04f), k);
            var rMid = _anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (rMid != null) Aim(rH, rMid, (up + right * 0.30f - face * 0.05f), k);
            // 왼손: 위팔은 아래·바깥, 아래팔은 허리 쪽으로 꺾어 손이 옆구리에. 손목은 손등이 바깥을 보게 몸 쪽으로.
            Aim(lUp, lLo, (-up * 0.80f - right * 0.75f + face * 0.05f), k);   // 팔꿈치 바깥으로
            Aim(lLo, lH, (right * 1.0f + up * 0.22f + face * 0.35f), k);      // 아래팔은 허리로 꺾어 손을 옆구리에
            var lMid = _anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            if (lMid != null) Aim(lH, lMid, (right * 0.6f - up * 0.35f - face * 0.7f), k);   // 손가락은 등 쪽으로 감싸듯
            var head = _anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                var cam = Camera.main != null ? Camera.main.transform : null;
                Vector3 look = cam != null ? (cam.position - head.position).normalized : face;
                var q = Quaternion.LookRotation(look, up) * Quaternion.Euler(0f, 0f, 14f);   // 갸웃
                head.rotation = Quaternion.Slerp(head.rotation, q, k * 0.9f);
            }
        }

        private void Update()
        {
            if (_anim == null || _player == null)
                return;
            float dt = Time.deltaTime;
            _collectCooldown -= dt;
            if (_djTimer > 0f) { _djTimer -= dt; if (_player.State != SkateState.Air) _djTimer = 0f; }

            _tilt = Mathf.SmoothDamp(_tilt, 0f, ref _tiltVel, 0.28f, 400f, dt);
            // 7차: 레인 이동 중 몸을 진행 방향으로 살짝 기울이고(roll) 고개를 돌린다(yaw) — 미끄러지듯 옆으로.
            float lv = _player.LateralVelocity;
            float leanTarget = Mathf.Clamp(-lv * 2.0f, -12f, 12f);   // 130차: 살짝 줄임
            float yawTarget = Mathf.Clamp(lv * 2.6f, -16f, 16f);
            // 골인: 뒤돌기 연출 없음 — 정면 직립으로 멈추기만
            if (_player.State == SkateState.Finish)
            {
                if (_glideBlend > 0.001f || _finishBlend > 0.001f || _finishPose)
                    SettleFacingForward();
                else
                {
                    _anim.SetBool(HashGrounded, true);
                    _anim.SetFloat(HashSpeed, 0f);
                    _anim.speed = 0f;
                    transform.localRotation = Quaternion.identity;
                    transform.localPosition = Vector3.zero;
                }
                return;
            }
            _finishBlend = Mathf.MoveTowards(_finishBlend, _finishPose ? 1f : 0f, dt * 2.2f);
            if (_finishPose && _finishBlend > 0.001f)
            {
                // (레거시 경로 — FinishMotion 켰을 때만 SetFinishPose(true))
                yawTarget = 180f * (_finishBlend * _finishBlend * (3f - 2f * _finishBlend));
                _anim.SetBool(HashGrounded, true);
                _anim.SetFloat(HashSpeed, 0f);
                _anim.speed = 0f;
                _lean = Mathf.SmoothDamp(_lean, 0f, ref _leanVel, 0.12f, 800f, dt);
                _yaw = Mathf.SmoothDamp(_yaw, yawTarget, ref _yawVel, 0.10f, 800f, dt);
                transform.localRotation = Quaternion.Euler(_pitch, _yaw, _lean + _tilt);
                return;
            }
            _lean = Mathf.SmoothDamp(_lean, leanTarget, ref _leanVel, 0.16f, 600f, dt);   // 130차: 0.10 → 0.16 더 매끈하게
            _yaw = Mathf.SmoothDamp(_yaw, yawTarget, ref _yawVel, 0.16f, 600f, dt);
            bool grounded = _player.State != SkateState.Air;
            bool running = grounded && _player.State == SkateState.Run;
            _anim.SetBool(HashGrounded, grounded);
            _anim.SetFloat(HashSpeed, _player.NormalizedSpeed);
            // 14차-8: 발이 땅을 '차고' 나가는 느낌 — 클립(조깅 보폭 ≈ 3 m/s)을 실제 속도에 맞춰 더 빨리 돌리고,
            // 속도에 따라 몸을 앞으로 기울이며, 발 디딜 때마다 몸이 위아래로 튄다. 공중·피격 중엔 1.0.
            // 130차: 클립 재생 속도를 즉시 바꾸지 않고 따라가게(가속·감속 때 걸음이 툭 끊기지 않게)
            float animTarget = running ? Mathf.Clamp(_player.Speed / 7.5f, 1.0f, 1.9f) : 1f;
            _anim.speed = Mathf.Lerp(_anim.speed < 0.01f ? animTarget : _anim.speed, animTarget, 1f - Mathf.Exp(-dt * 6f));
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
                    JuiceDirector.Instance?.StepThump();   // 63차: 발이 닿는 「쿵」 — 카메라 미세 흔들림
                    _bounceVel = -0.55f;   // 착지 충격: 아래로 (63차: 더 세게) · 130차(사용자 「달리기 더 부드럽게」): 0.85 → 0.55
                }
            }
            // 스프링: 착지 → 살짝 내려앉음 → 튕겨 올라옴 (66차-7: 잘게 나눠 적분 — 큰 dt 에서 발산 방지)
            for (float rem = Mathf.Min(dt, 0.25f); rem > 0f;)
            {
                float h = Mathf.Min(rem, 1f / 90f); rem -= h;
                _bounceVel += (-_bounce * 200f - _bounceVel * 17f) * h;   // 130차: 스프링 느긋하게
                _bounce += _bounceVel * h;
            }
            if (float.IsNaN(_bounce)) { _bounce = 0f; _bounceVel = 0f; }
            _bounce = Mathf.Clamp(_bounce, -0.3f, 0.3f);
            float side = running ? Mathf.Sin(_stepClock / stepPeriod * Mathf.PI) * 0.45f * _stepSide : 0f;   // 어깨 좌우 흔들림(°) · 130차: 0.6 → 0.45
            // 19차-2: 빨래줄 활공 — 슈퍼맨 자세. 몸을 78° 앞으로 눕히고(엉덩이 기준으로 회전) 살짝 출렁인다.
            // 팔·다리는 LateUpdate에서 뼈를 직접 펴서 앞으로 뻗는다(믹사모 클립 없이 절차적으로).
            bool gliding = _player.IsGliding;
            if (gliding && !_wasGliding) _glideT = 0f;
            _wasGliding = gliding;
            if (gliding) _glideT += dt;
            _glideBlend = Mathf.MoveTowards(_glideBlend, gliding ? 1f : 0f, dt * (gliding ? 5f : 3f));
            if (_glideBlend > 0.001f)
            {
                float k = _glideBlend * _glideBlend * (3f - 2f * _glideBlend);
                float bob = Mathf.Sin(Time.time * 3.1f) * 3f * k;
                // 22차-6: 줄을 잡는 순간 앞으로 한 바퀴(360°) 뱅그르르 돌고 나서 슈퍼맨 자세로 편다.
                float spinU = Mathf.Clamp01(_glideT / SpinSeconds);
                float spin = gliding ? 360f * (spinU * spinU * (3f - 2f * spinU)) : 360f;
                float glidePitch = Mathf.Lerp(_pitch, 78f + bob, k) + spin;
                var rot = Quaternion.Euler(glidePitch, _yaw, _tilt + _lean * 1.6f);
                transform.localRotation = rot;
                // 엉덩이가 제자리에 남도록 회전으로 밀려난 만큼 되돌리고, 조금 띄운다
                if (_hipRest == Vector3.zero) CacheHipRest();
                Vector3 shift = _hipRest - rot * _hipRest;
                transform.localPosition = Vector3.Lerp(Vector3.zero, shift + new Vector3(0f, 0.25f, 0.15f), k);
            }
            else
            {
                // 122차: 2단 점프 공중제비 — 0.55초 동안 앞으로 한 바퀴(스무스스텝), 땅에 닿으면 남은 각도는 바로 마무리
                float djSpin = 0f;
                if (_djSpinT >= 0f)
                {
                    _djSpinT += dt;
                    float su = Mathf.Clamp01(_djSpinT / DjSpinSeconds);
                    if (grounded && su > 0.5f) su = 1f;
                    djSpin = 360f * (su * su * (3f - 2f * su));
                    if (su >= 1f) { _djSpinT = -1f; djSpin = 0f; }
                }
                var rot = Quaternion.Euler(_pitch + djSpin, _yaw, _tilt + _lean + side + _laneKick);   // 27차: 레인 킥
                transform.localRotation = rot;
                // 63차(사용자 「공중에 살짝 떠다니는 느낌」): 달릴 땐 몸을 3.5 cm 가라앉혀 발이 바닥을 확실히 딛게 + 착지 스프링 폭 ↑
                Vector3 basePos = new Vector3(0f, running ? Mathf.Clamp(_bounce, -0.08f, 0.04f) - 0.035f : 0f, 0f);
                if (djSpin > 0f)
                {
                    if (_hipRest == Vector3.zero) CacheHipRest();
                    basePos += _hipRest - rot * _hipRest;   // 엉덩이를 축으로 돌게 보정
                }
                transform.localPosition = basePos;
            }
            TickJuice(dt, running);   // 27차

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
