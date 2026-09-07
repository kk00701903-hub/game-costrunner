using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// Collectible coin on the promenade. Magnet upgrades pull nearby coins on a curved path.
    public class CoinPickup : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private bool silver;

        private CoinWallet _wallet;
        private UpgradeManager _upgrades;
        private UI_FeedbackController _feedback;
        private Transform _player;
        private bool _collected;
        private float _spin;
        private float _bobPhase;   // 12차: 코인도 젤리처럼 떠서 흔들린다
        private bool _magnetActive;
        private float _magnetT;
        private Vector3 _magnetStart;
        private float _magnetBend;
        private Transform _visualRoot;
        private Transform _painted;        // 14차-3: 그림 코인 빌보드
        private Vector3 _paintedScale;

        // 14차 최적화: 지나친 코인은 파괴하지 않고 풀에 넣었다가 다시 쓴다(금/은 따로).
        // 런 한 번에 코인 수백 개 — 프리미티브 3개 + 셰이더 머티리얼을 매번 만들면 GC 스파이크가 났다.
        private static readonly System.Collections.Generic.Stack<CoinPickup> _poolGold = new System.Collections.Generic.Stack<CoinPickup>();
        private static readonly System.Collections.Generic.Stack<CoinPickup> _poolSilver = new System.Collections.Generic.Stack<CoinPickup>();

        /// 뒤로 지나간 코인: 비활성화하고 풀로. 먹은 코인은 연출 때문에 그대로 파괴된다.
        public void Recycle()
        {
            if (_collected) { Destroy(gameObject); return; }
            _magnetActive = false;
            gameObject.SetActive(false);
            (silver ? _poolSilver : _poolGold).Push(this);
        }

        private static CoinPickup PopPool(bool silver)
        {
            var pool = silver ? _poolSilver : _poolGold;
            while (pool.Count > 0)
            {
                var c = pool.Pop();
                if (c != null && c.gameObject != null) return c;   // 씬 전환으로 파괴된 항목은 건너뛴다
            }
            return null;
        }

        public static CoinPickup Spawn(Transform parent, Vector3 worldPos, CoinWallet wallet,
            UpgradeManager upgrades, UI_FeedbackController feedback, Transform player, bool silver = false)
        {
            var reuse = PopPool(silver);
            if (reuse != null)
            {
                var rgo = reuse.gameObject;
                rgo.transform.SetParent(parent, false);
                rgo.transform.SetPositionAndRotation(worldPos, DownhillPath.Rotation);
                reuse._wallet = wallet; reuse._upgrades = upgrades; reuse._feedback = feedback; reuse._player = player;
                reuse._collected = false; reuse._magnetActive = false; reuse._magnetT = 0f;
                reuse._bobPhase = Random.value * Mathf.PI * 2f;
                var rc = rgo.GetComponent<Collider>(); if (rc != null) rc.enabled = true;
                if (reuse._visualRoot != null) { reuse._visualRoot.localPosition = Vector3.zero; reuse._visualRoot.localRotation = Quaternion.identity; }
                var g = rgo.GetComponent<PickupGlow>(); if (g != null) g.Show();
                rgo.GetComponent<BlobShadow>()?.Invalidate();
                rgo.SetActive(true);
                return reuse;
            }

            var go = new GameObject(silver ? "Coin_Silver" : "Coin_Gold");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = DownhillPath.Rotation;

            var coin = go.AddComponent<CoinPickup>();
            coin.value = silver ? 1 : 2;
            coin.silver = silver;
            coin._wallet = wallet;
            coin._upgrades = upgrades;
            coin._feedback = feedback;
            coin._player = player;

            var visRoot = new GameObject("VisualRoot").transform;
            visRoot.SetParent(go.transform, false);
            coin._visualRoot = visRoot;

            // 14차-3: Kling 코인 그림(별 엠블럼)이 있으면 또렷한 빌보드로. 프리미티브 원통 + HDR 색은
            // 블룸에 먹혀 노란 얼룩으로 보였다. 회전은 빌보드 가로 스케일로 흉내 낸다.
            string paintedKey = silver ? "Coin_Silver" : "Coin_Gold";
            if (PaintedProp.Available(paintedKey))
            {
                coin._painted = PaintedProp.Attach(visRoot, paintedKey, 0.62f, replace: false, groundLift: -0.11f);
                if (coin._painted != null) coin._paintedScale = coin._painted.localScale;
                var pcol = go.AddComponent<SphereCollider>();
                pcol.isTrigger = true;
                pcol.radius = 0.5f;
                pcol.center = new Vector3(0f, 0.2f, 0f);
                BlobShadow.Attach(go.transform, 0.45f);
                PickupGlow.Attach(go.transform, silver ? new Color(0.8f, 0.92f, 1f) : new Color(1f, 0.8f, 0.25f), 0.7f, 0.14f);
                coin._bobPhase = Random.value * Mathf.PI * 2f;
                return coin;
            }

            var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vis.name = "Visual";
            vis.transform.SetParent(visRoot, false);
            vis.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            vis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vis.transform.localScale = new Vector3(0.5f, 0.07f, 0.5f);
            Object.Destroy(vis.GetComponent<Collider>());
            // 12차: 1.0을 넘는 색은 블룸(임계 1.1)이 집어 광원을 만든다 — 은은하게 빛나는 동전.
            System.Func<Color> face = silver
                ? () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.35f) * 1.35f
                : () => CoastPalette.CoinYellow * 1.5f;
            System.Func<Color> rimCol = silver
                ? () => Color.Lerp(CoastPalette.TownCream, Color.white, 0.4f)
                : () => Color.Lerp(CoastPalette.CoinYellow, Color.white, 0.35f);
            vis.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(face(), face);

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(visRoot, false);
            rim.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rim.transform.localScale = new Vector3(0.56f, 0.035f, 0.56f);
            Object.Destroy(rim.GetComponent<Collider>());
            rim.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(rimCol(), rimCol);

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 0.2f, 0f);
            // 12차: 두 번 붙이던 그림자를 하나로(뒤 호출이 앞 값을 덮어쓰고 있었다), 광원 추가.
            BlobShadow.Attach(go.transform, 0.5f);
            PickupGlow.Attach(go.transform, silver ? new Color(0.8f, 0.92f, 1f) : new Color(1f, 0.8f, 0.25f), 0.85f, 0.26f);
            coin._bobPhase = Random.value * Mathf.PI * 2f;
            return coin;
        }

        private void Update()
        {
            if (_collected)
                return;

            _spin += Time.deltaTime * 180f;
            if (_painted != null)
            {
                float w = Mathf.Max(0.18f, Mathf.Abs(Mathf.Cos(_spin * Mathf.Deg2Rad)));
                _painted.localScale = new Vector3(_paintedScale.x * w, _paintedScale.y, _paintedScale.z);
                _visualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3.2f + _bobPhase) * 0.07f, 0f);
            }
            else if (_visualRoot != null)
            {
                _visualRoot.localRotation = Quaternion.Euler(0f, _spin, 0f);
                _visualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3.2f + _bobPhase) * 0.07f, 0f);
            }
            else
                transform.rotation = DownhillPath.Rotation * Quaternion.Euler(0f, _spin, 0f);

            if (_player == null || _upgrades == null)
                return;

            float magnet = _upgrades.GetMagnetRadius() + PetCompanion.MagnetBonus;
            if (magnet <= 0.05f)
                return;

            Vector3 toPlayer = _player.position - transform.position;
            if (toPlayer.sqrMagnitude > magnet * magnet)
            {
                _magnetActive = false;
                return;
            }

            // Curved suck — never MoveTowards straight line.
            if (!_magnetActive)
            {
                _magnetActive = true;
                _magnetT = 0f;
                _magnetStart = transform.position;
                _magnetBend = Random.Range(0.35f, 0.75f) * (Random.value > 0.5f ? 1f : -1f);
            }

            _magnetT += Time.deltaTime * 2.4f;
            float u = Mathf.Clamp01(_magnetT);
            float e = u * u * (3f - 2f * u);
            Vector3 end = _player.position + Vector3.up * 0.8f;
            Vector3 mid = Vector3.Lerp(_magnetStart, end, 0.45f);
            Vector3 lateral = Vector3.Cross(Vector3.up, (end - _magnetStart).normalized);
            if (lateral.sqrMagnitude < 0.001f)
                lateral = DownhillPath.Rotation * Vector3.right;
            Vector3 ctrl = mid + lateral.normalized * _magnetBend;
            Vector3 a = Vector3.Lerp(_magnetStart, ctrl, e);
            Vector3 b = Vector3.Lerp(ctrl, end, e);
            transform.position = Vector3.Lerp(a, b, e);
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

            float mult = (_upgrades != null ? _upgrades.GetCoinMultiplier() : 1f) * PetCompanion.CoinBonus * RunTuning.CoinMul;
            int amount = Mathf.Max(1, Mathf.RoundToInt(value * mult));
            _wallet?.Add(amount);
            StageRunStats.Instance?.NotifyCoin(amount);
            _feedback?.ShowFloatingReward(transform.position + Vector3.up * 0.6f, amount, 1);

            var juice = JuiceDirector.Instance;
            Transform vis = _visualRoot != null ? _visualRoot : transform;
            juice?.PlayCoinCollect(vis, transform.position + Vector3.up * 0.2f, amount);

            // Disable collision; visual destroyed by juice pop (or fallback).
            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            if (juice == null)
            {
                Destroy(gameObject);
                return;
            }

            // Detach visual for independent pop; destroy empty shell after.
            if (_visualRoot != null)
                _visualRoot.SetParent(null, true);
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
