using System.Collections;
using UnityEngine;

namespace CoastRun
{
    public enum CoinTier { Silver, Gold, Bundle }

    /// Collectible coin on the promenade. Magnet upgrades pull nearby coins on a curved path.
    /// Gold/Silver: chunky 3D puck + Obs_Coin face art. Bundle: stacked pile worth 10× gold.
    public class CoinPickup : MonoBehaviour
    {
        public const int GoldValue = 2;
        public const int SilverValue = 1;
        public const int BundleValue = GoldValue * 10;   // 기존 금화 10배

        [SerializeField] private int value = GoldValue;
        [SerializeField] private CoinTier tier = CoinTier.Gold;

        private CoinWallet _wallet;
        private UpgradeManager _upgrades;
        private UI_FeedbackController _feedback;
        private Transform _player;
        private bool _collected;
        private float _spin;
        private float _bobPhase;
        private bool _magnetActive;
        private float _magnetT;
        private Vector3 _magnetStart;
        private float _magnetBend;
        private Transform _visualRoot;

        private static readonly System.Collections.Generic.Stack<CoinPickup> _poolGold = new();
        private static readonly System.Collections.Generic.Stack<CoinPickup> _poolSilver = new();
        private static readonly System.Collections.Generic.Stack<CoinPickup> _poolBundle = new();
        private static Material _faceGold, _faceSilver, _outlineMat, _bezelMat, _rimMatGold, _rimMatSilver, _glintMat, _shineMat;
        private static Texture2D _sparkleTex;
        private Transform _sparkleRoot;
        private Transform[] _glints;
        private Transform _shineBand;
        private float _sparklePhase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            _poolGold.Clear();
            _poolSilver.Clear();
            _poolBundle.Clear();
            _faceGold = _faceSilver = _outlineMat = _bezelMat = _rimMatGold = _rimMatSilver = null;
            _glintMat = _shineMat = null;
            _sparkleTex = null;
        }

        private static System.Collections.Generic.Stack<CoinPickup> PoolFor(CoinTier t) =>
            t == CoinTier.Bundle ? _poolBundle : t == CoinTier.Silver ? _poolSilver : _poolGold;

        public void Recycle()
        {
            if (_collected) { Destroy(gameObject); return; }
            _magnetActive = false;
            gameObject.SetActive(false);
            PoolFor(tier).Push(this);
        }

        private static CoinPickup PopPool(CoinTier t)
        {
            var pool = PoolFor(t);
            while (pool.Count > 0)
            {
                var c = pool.Pop();
                if (c != null && c.gameObject != null) return c;
            }
            return null;
        }

        public static CoinPickup Spawn(Transform parent, Vector3 worldPos, CoinWallet wallet,
            UpgradeManager upgrades, UI_FeedbackController feedback, Transform player, bool silver = false)
            => Spawn(parent, worldPos, wallet, upgrades, feedback, player,
                silver ? CoinTier.Silver : CoinTier.Gold);

        public static CoinPickup Spawn(Transform parent, Vector3 worldPos, CoinWallet wallet,
            UpgradeManager upgrades, UI_FeedbackController feedback, Transform player, CoinTier tier)
        {
            var reuse = PopPool(tier);
            if (reuse != null)
            {
                var rgo = reuse.gameObject;
                rgo.transform.SetParent(parent, false);
                rgo.transform.SetPositionAndRotation(worldPos, DownhillPath.Rotation);
                reuse._wallet = wallet; reuse._upgrades = upgrades; reuse._feedback = feedback; reuse._player = player;
                reuse._collected = false; reuse._magnetActive = false; reuse._magnetT = 0f;
                reuse._bobPhase = Random.value * Mathf.PI * 2f;
                reuse._spin = Random.Range(0f, 360f);
                var rc = rgo.GetComponent<Collider>(); if (rc != null) rc.enabled = true;
                if (reuse._visualRoot != null)
                {
                    reuse._visualRoot.gameObject.SetActive(true);
                    reuse._visualRoot.localPosition = Vector3.zero;
                    reuse._visualRoot.localRotation = Quaternion.Euler(0f, reuse._spin, 0f);
                }
                var g = rgo.GetComponent<PickupGlow>(); if (g != null) g.Show();
                rgo.GetComponent<BlobShadow>()?.Invalidate();
                reuse._sparklePhase = Random.value * 6.28f;
                if (reuse._sparkleRoot != null) reuse._sparkleRoot.gameObject.SetActive(true);
                rgo.SetActive(true);
                return reuse;
            }

            var go = new GameObject(tier == CoinTier.Bundle ? "Coin_Bundle" : tier == CoinTier.Silver ? "Coin_Silver" : "Coin_Gold");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = DownhillPath.Rotation;

            var coin = go.AddComponent<CoinPickup>();
            coin.tier = tier;
            coin.value = tier == CoinTier.Bundle ? BundleValue : tier == CoinTier.Silver ? SilverValue : GoldValue;
            coin._wallet = wallet;
            coin._upgrades = upgrades;
            coin._feedback = feedback;
            coin._player = player;

            var visRoot = new GameObject("VisualRoot").transform;
            visRoot.SetParent(go.transform, false);
            coin._visualRoot = visRoot;

            if (tier == CoinTier.Bundle)
                BuildBundle(visRoot);
            else
                BuildSingleCoin(visRoot, tier == CoinTier.Silver);

            float colR = tier == CoinTier.Bundle ? 0.72f : 0.5f;
            float colY = tier == CoinTier.Bundle ? 0.42f : 0.32f;
            var mcol = go.AddComponent<SphereCollider>();
            mcol.isTrigger = true;
            mcol.radius = colR;
            mcol.center = new Vector3(0f, colY, 0f);
            BlobShadow.Attach(go.transform, tier == CoinTier.Bundle ? 0.75f : 0.5f);
            AttachGoldSparkle(coin, go.transform, tier);
            coin._bobPhase = Random.value * Mathf.PI * 2f;
            coin._spin = Random.Range(0f, 360f);
            coin._sparklePhase = Random.value * 6.28f;
            visRoot.localRotation = Quaternion.Euler(18f, coin._spin, 0f);
            return coin;
        }

        /// 황금 후광 + 반짝이 별 + 면 광택 스윕.
        private static void AttachGoldSparkle(CoinPickup coin, Transform host, CoinTier tier)
        {
            bool silver = tier == CoinTier.Silver;
            Color glow = silver
                ? new Color(0.92f, 0.95f, 1f, 0.38f)
                : new Color(1f, 0.82f, 0.22f, tier == CoinTier.Bundle ? 0.62f : 0.52f);
            float glowSize = tier == CoinTier.Bundle ? 1.25f : silver ? 0.82f : 1.05f;
            PickupGlow.Attach(host, glow, glowSize, glow.a);

            var root = new GameObject("Sparkles").transform;
            root.SetParent(host, false);
            root.localPosition = new Vector3(0f, tier == CoinTier.Bundle ? 0.48f : 0.34f, 0f);
            coin._sparkleRoot = root;

            _glintMat ??= CoastMaterials.CreateTexturedTransparentCurved(SparkleTexture(), Color.white, additive: true);
            _glintMat.renderQueue = 3100;
            int n = tier == CoinTier.Bundle ? 5 : 3;
            coin._glints = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Glint" + i;
                q.transform.SetParent(root, false);
                Object.Destroy(q.GetComponent<Collider>());
                float ang = i * (360f / n);
                q.transform.localPosition = Quaternion.Euler(0f, ang, 0f) * new Vector3(0.28f, 0.05f, 0f);
                q.transform.localScale = Vector3.one * (silver ? 0.14f : 0.18f);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = _glintMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                q.AddComponent<YawBillboard>();
                coin._glints[i] = q.transform;
            }

            // 면 위를 스치는 골드 광택 띠
            _shineMat ??= CoastMaterials.CreateTexturedTransparentCurved(BlobShadow.SoftDisc(), new Color(1f, 0.95f, 0.55f, 0.55f), additive: true);
            _shineMat.renderQueue = 3050;
            var shine = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shine.name = "ShineBand";
            shine.transform.SetParent(root, false);
            Object.Destroy(shine.GetComponent<Collider>());
            shine.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shine.transform.localScale = new Vector3(0.55f, 0.22f, 1f);
            var sr = shine.GetComponent<Renderer>();
            sr.sharedMaterial = _shineMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            shine.AddComponent<YawBillboard>();
            coin._shineBand = shine.transform;
        }

        private static Texture2D SparkleTexture()
        {
            if (_sparkleTex != null) return _sparkleTex;
            const int n = 32;
            _sparkleTex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "CoinSparkle", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                // 십자 별 스파클
                float arm = Mathf.Max(0f, 1f - Mathf.Max(ax * 6f, ay * 1.2f))
                          + Mathf.Max(0f, 1f - Mathf.Max(ay * 6f, ax * 1.2f));
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v) * 2.8f);
                float a = Mathf.Clamp01(arm * 0.85f + core);
                _sparkleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _sparkleTex.Apply(false, true);
            return _sparkleTex;
        }

        private static void BuildSingleCoin(Transform visRoot, bool silver)
        {
            string meshKey = silver ? "Coin_Silver" : "Coin_Gold";
            var mesh = JejuKit.Load(meshKey) != null
                ? JejuKit.Spawn(meshKey, visRoot, new Vector3(0f, 0.32f, 0f), 0f, 0.65f)
                : null;
            if (mesh != null)
            {
                mesh.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                ApplyCoinBodyMaterial(mesh, silver, allRenderers: true);   // 39차-3: FBX 자식 전부 황금 재질로
                // 39차-3: FBX 코인엔 인버티드 헐(Outline) 생략 — Blender 내보내기 와인딩이 반대라 Cull Front 껍데기가
                // 바깥으로 그려져 코인 전체가 검게 덮였다(별 릴리프까지 검정). 잉크 림만으로 윤곽을 잡는다.
                AttachCoinSilhouette(mesh.transform, silver, hull: false);
                // 39차-3: FBX 몸통 두께 ±0.17, 잉크 림 ±0.10 → 면 그림은 그 바깥(±0.18)에. (전엔 0.13이라 림 캡(±0.145) 안에 묻혀 검게 보였다)
                AttachCoinFaces(visRoot, silver, radius: 0.44f, halfThick: 0.18f, y: 0.32f);
            }
            else
            {
                var body = BuildProceduralChunkyCoin(visRoot, silver);
                AttachCoinSilhouette(body != null ? body.transform : visRoot, silver);
                AttachCoinFaces(visRoot, silver, radius: 0.44f, halfThick: 0.19f, y: 0.32f);
            }
        }

        /// 금화 4장을 살짝 어긋나게 쌓아 꾸러미처럼 보이게.
        private static void BuildBundle(Transform visRoot)
        {
            Vector3[] offs =
            {
                new Vector3(-0.12f, 0.28f, 0.02f),
                new Vector3(0.10f, 0.34f, -0.06f),
                new Vector3(-0.02f, 0.42f, 0.10f),
                new Vector3(0.06f, 0.50f, -0.02f),
            };
            float[] yaws = { -18f, 22f, -8f, 35f };
            for (int i = 0; i < offs.Length; i++)
            {
                var slot = new GameObject("Stack" + i).transform;
                slot.SetParent(visRoot, false);
                slot.localPosition = offs[i];
                slot.localRotation = Quaternion.Euler(12f + i * 3f, yaws[i], i % 2 == 0 ? -6f : 8f);
                slot.localScale = Vector3.one * (0.92f - i * 0.03f);
                var body = BuildProceduralChunkyCoin(slot, silver: false, localY: 0f);
                AttachCoinSilhouette(body != null ? body.transform : slot, silver: false);
                AttachCoinFaces(slot, silver: false, radius: 0.44f, halfThick: 0.19f, y: 0f);
            }
        }

        private static Material FaceMaterial(bool silver)
        {
            if (silver)
            {
                if (_faceSilver != null) return _faceSilver;
                var tex = PaintedProp.Load("Coin_Silver");
                if (tex == null) return null;
                _faceSilver = CoastMaterials.CreateTexturedTransparent(tex, Color.white);
                return _faceSilver;
            }
            if (_faceGold != null) return _faceGold;
            var gtex = PaintedProp.Load("Coin_Gold");
            if (gtex == null) return null;
            // 따뜻한 황금 틴트 — 그림 위에도 반짝이는 골드 느낌
            _faceGold = CoastMaterials.CreateTexturedTransparent(gtex, new Color(1f, 0.96f, 0.72f, 1f));
            return _faceGold;
        }

        /// Obs_Coin_*.png 를 앞·뒤 면에 붙인다. 면은 살짝 작게 해서 메탈 림·어두운 윤곽이 보이게.
        private static void AttachCoinFaces(Transform parent, bool silver, float radius, float halfThick, float y)
        {
            var mat = FaceMaterial(silver);
            if (mat == null) return;
            float face = radius * 1.58f;
            for (int side = -1; side <= 1; side += 2)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = side > 0 ? "CoinFace_F" : "CoinFace_B";
                q.transform.SetParent(parent, false);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.localPosition = new Vector3(0f, y, side * halfThick);
                q.transform.localRotation = side > 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                q.transform.localScale = new Vector3(face, face, 1f);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        /// 메시 인버티드 헐 + 두꺼운 어두운 림 — 멀리서도 원형 윤곽이 읽히게.
        private static void AttachCoinSilhouette(Transform body, bool silver, bool hull = true)
        {
            if (body == null) return;
            var outline = OutlineMaterial();

            foreach (var mf in body.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!hull) break;
                if (mf == null || mf.sharedMesh == null) continue;
                string n = mf.gameObject.name;
                if (n.StartsWith("CoinFace") || n.StartsWith("CoinBezel") || n == "Outline" || n == "InkRim") continue;
                if (mf.transform.Find("Outline") != null) continue;

                var shell = new GameObject("Outline");
                shell.transform.SetParent(mf.transform, false);
                shell.transform.localScale = Vector3.one * 1.10f;
                shell.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = shell.AddComponent<MeshRenderer>();
                r.sharedMaterial = outline;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            if (body.Find("InkRim") != null) return;
            var ink = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ink.name = "InkRim";
            ink.transform.SetParent(body, false);
            Object.Destroy(ink.GetComponent<Collider>());
            float x = body.localEulerAngles.x;
            bool flat = Mathf.Abs(Mathf.DeltaAngle(x, 90f)) < 8f;
            if (flat)
            {
                ink.transform.localPosition = Vector3.zero;
                ink.transform.localRotation = Quaternion.identity;
                ink.transform.localScale = new Vector3(1.14f, 0.42f, 1.14f);
            }
            else
            {
                ink.transform.localPosition = Vector3.zero;
                ink.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ink.transform.localScale = new Vector3(1.06f, 0.15f, 1.06f);   // 39차-3: 1.12/0.22 → 1.06/0.15 (면 그림보다 얇게, 테두리는 가늘게)
            }
            var ir = ink.GetComponent<Renderer>();
            ir.sharedMaterial = BezelMaterial();
            ir.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ir.receiveShadows = false;
        }

        private static Material OutlineMaterial()
        {
            if (_outlineMat != null) return _outlineMat;
            _outlineMat = CoastMaterials.CreateUnlit(new Color(0.10f, 0.06f, 0.04f, 1f));
            if (_outlineMat.HasProperty("_Cull"))
                _outlineMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
            return _outlineMat;
        }

        private static Material BezelMaterial()
        {
            if (_bezelMat != null) return _bezelMat;
            _bezelMat = CoastMaterials.CreateUnlit(new Color(0.48f, 0.30f, 0.06f, 1f));   // 39차-3: 검정 → 진한 금갈색(옆에서 봐도 황금)
            return _bezelMat;
        }

        private static Material _bodyGold, _bodySilver;

        private static void ApplyCoinBodyMaterial(GameObject root, bool silver, bool allRenderers = false)
        {
            // 39차-3: 코인 몸통이 흑갈색으로 보이던 것 — Coin_Gold.fbx(38차)의 법선/서브메시가 툰 조명과 안 맞아
            // 옆·뒤로 돈 코인이 검게 덮였다. 몸통은 무조명 황금(툰 ×) + 모든 서브메시 슬롯에 같은 재질, 코인마다 새 재질 만들지 않고 공유.
            Color coinFace = silver
                ? Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.28f)
                : new Color(1.0f, 0.80f, 0.22f, 1f);   // 진한 황금
            Material mat;
            if (silver) mat = _bodySilver ??= CoastMaterials.CreateUnlit(coinFace);
            else mat = _bodyGold ??= CoastMaterials.CreateUnlit(coinFace);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                string n = r.gameObject.name;
                // FBX(JejuKit) 몸통은 자식 이름이 "CoinFace_*" 같이 겹칠 수 있어(별 릴리프 노드) 이름 필터를 건너뛴다 — 안 그러면 FBX 기본 재질(검정)이 남는다.
                if (!allRenderers && (n == "Outline" || n == "InkRim" || n.StartsWith("CoinFace") || n.StartsWith("CoinBezel"))) continue;
                var slots = r.sharedMaterials;
                if (slots == null || slots.Length <= 1) r.sharedMaterial = mat;
                else { for (int i = 0; i < slots.Length; i++) slots[i] = mat; r.sharedMaterials = slots; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private static GameObject BuildProceduralChunkyCoin(Transform visRoot, bool silver, float localY = 0.32f)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vis.name = silver ? "Coin_Silver" : "Coin_Gold";
            vis.transform.SetParent(visRoot, false);
            vis.transform.localPosition = new Vector3(0f, localY, 0f);
            vis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vis.transform.localScale = new Vector3(0.55f, 0.34f, 0.55f);
            Object.Destroy(vis.GetComponent<Collider>());
            ApplyCoinBodyMaterial(vis, silver);

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(vis.transform, false);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localRotation = Quaternion.identity;
            rim.transform.localScale = new Vector3(1.08f, 0.55f, 1.08f);
            Object.Destroy(rim.GetComponent<Collider>());
            if (silver)
            {
                _rimMatSilver ??= CoastMaterials.CreateToon(
                    Color.Lerp(CoastPalette.TownCream, Color.white, 0.55f), null, null, 0.55f);
                rim.GetComponent<Renderer>().sharedMaterial = _rimMatSilver;
            }
            else
            {
                _rimMatGold ??= CoastMaterials.CreateToon(
                    new Color(1.2f, 0.95f, 0.4f, 1f), null, null, 0.62f);
                rim.GetComponent<Renderer>().sharedMaterial = _rimMatGold;
            }
            return vis;
        }

        private void Update()
        {
            if (_collected)
                return;

            _spin += Time.deltaTime * (tier == CoinTier.Bundle ? 160f : 220f);
            if (_visualRoot != null)
            {
                _visualRoot.localRotation = Quaternion.Euler(18f, _spin, 0f);
                _visualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3.6f + _bobPhase) * 0.08f, 0f);
            }
            else
                transform.rotation = DownhillPath.Rotation * Quaternion.Euler(0f, _spin, 0f);

            TickSparkle();

            if (_player == null)
                return;

            if (PickupReach.InReach(_player, transform.position))
            {
                Collect();
                return;
            }

            float magnet = (_upgrades != null ? _upgrades.GetMagnetRadius() : 0f) + PetCompanion.MagnetBonus + FeverMode.MagnetBonus;
            bool feverPull = FeverMode.InPullRange(_player, transform.position);
            Vector3 toPlayer = _player.position - transform.position;
            bool inSphere = magnet > 0.05f && toPlayer.sqrMagnitude <= magnet * magnet;
            if (!feverPull && !inSphere)
            {
                _magnetActive = false;
                return;
            }

            if (!_magnetActive)
            {
                _magnetActive = true;
                _magnetT = 0f;
                _magnetStart = transform.position;
                _magnetBend = Random.Range(0.35f, 0.75f) * (Random.value > 0.5f ? 1f : -1f);
            }

            _magnetT += Time.deltaTime * (FeverMode.Active ? 6.5f : 2.4f);
            float u = Mathf.Clamp01(_magnetT);
            float e = u * u * (3f - 2f * u);
            Vector3 end = PickupReach.MagnetTarget(_player);
            Vector3 mid = Vector3.Lerp(_magnetStart, end, 0.45f);
            Vector3 lateral = Vector3.Cross(Vector3.up, (end - _magnetStart).normalized);
            if (lateral.sqrMagnitude < 0.001f)
                lateral = DownhillPath.Rotation * Vector3.right;
            Vector3 ctrl = mid + lateral.normalized * _magnetBend;
            Vector3 a = Vector3.Lerp(_magnetStart, ctrl, e);
            Vector3 b = Vector3.Lerp(ctrl, end, e);
            transform.position = Vector3.Lerp(a, b, e);
        }

        /// 피버 시작 시 즉시 흡입 궤도에 태운다.
        public void BeginFeverPull(Transform player)
        {
            if (_collected || player == null) return;
            if (!FeverMode.InPullRange(player, transform.position)) return;
            _player = player;
            if (!_magnetActive)
            {
                _magnetActive = true;
                _magnetT = 0.15f;
                _magnetStart = transform.position;
                _magnetBend = Random.Range(0.2f, 0.5f) * (Random.value > 0.5f ? 1f : -1f);
            }
            else
                _magnetT = Mathf.Max(_magnetT, 0.35f);
        }

        private void TickSparkle()
        {
            if (_sparkleRoot == null) return;
            float t = Time.time + _sparklePhase;
            _sparkleRoot.localRotation = Quaternion.Euler(0f, t * 55f, 0f);

            if (_glints != null)
            {
                for (int i = 0; i < _glints.Length; i++)
                {
                    var g = _glints[i];
                    if (g == null) continue;
                    // 서로 다른 박자로 깜빡 — 황금 별 반짝
                    float blink = Mathf.Pow(Mathf.Abs(Mathf.Sin(t * (7.5f + i * 1.7f) + i)), 8f);
                    float sc = (tier == CoinTier.Silver ? 0.12f : 0.16f) * (0.35f + 1.8f * blink);
                    g.localScale = Vector3.one * sc;
                    g.gameObject.SetActive(blink > 0.08f);
                }
            }
            if (_shineBand != null)
            {
                float sweep = 0.5f + 0.5f * Mathf.Sin(t * 5.2f);
                _shineBand.localPosition = new Vector3(Mathf.Lerp(-0.18f, 0.18f, sweep), 0.02f, 0.02f);
                _shineBand.localScale = new Vector3(0.35f + 0.25f * sweep, 0.18f + 0.1f * sweep, 1f);
                _shineBand.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-25f, 25f, sweep));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected)
                return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null)
                return;

            Collect();
        }

        private void Collect()
        {
            if (_collected)
                return;
            _collected = true;
            GetComponent<PickupGlow>()?.Hide();
            if (_sparkleRoot != null) _sparkleRoot.gameObject.SetActive(false);

            float mult = (_upgrades != null ? _upgrades.GetCoinMultiplier() : 1f) * PetCompanion.CoinBonus * RunTuning.CoinMul;
            int amount = Mathf.Max(1, Mathf.RoundToInt(value * mult));
            _wallet?.Add(amount);
            StageRunStats.Instance?.NotifyCoin(amount);
            // +N 플로트는 JuiceDirector.PlayCoinCollect → PickupFloat 한 곳만(이중 표시·잔상 방지)

            var juice = JuiceDirector.Instance;
            if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
            juice?.PlayCoinCollect(null, PickupReach.PopPos(_player, transform.position), amount);

            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            if (juice == null)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(DestroyShell());
        }

        private IEnumerator DestroyShell()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
