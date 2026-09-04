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
        private bool _magnetActive;
        private float _magnetT;
        private Vector3 _magnetStart;
        private float _magnetBend;
        private Transform _visualRoot;

        public static CoinPickup Spawn(Transform parent, Vector3 worldPos, CoinWallet wallet,
            UpgradeManager upgrades, UI_FeedbackController feedback, Transform player, bool silver = false)
        {
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

            var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vis.name = "Visual";
            vis.transform.SetParent(visRoot, false);
            vis.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            vis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vis.transform.localScale = new Vector3(0.5f, 0.07f, 0.5f);
            Object.Destroy(vis.GetComponent<Collider>());
            System.Func<Color> face = silver
                ? () => Color.Lerp(CoastPalette.TownCream, CoastPalette.SkyBlue, 0.35f)
                : () => CoastPalette.CoinYellow;
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

            BlobShadow.Attach(go.transform, 0.55f);
            return coin;
        }

        private void Update()
        {
            if (_collected)
                return;

            _spin += Time.deltaTime * 180f;
            if (_visualRoot != null)
                _visualRoot.localRotation = Quaternion.Euler(0f, _spin, 0f);
            else
                transform.rotation = DownhillPath.Rotation * Quaternion.Euler(0f, _spin, 0f);

            if (_player == null || _upgrades == null)
                return;

            float magnet = _upgrades.GetMagnetRadius();
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

            float mult = _upgrades != null ? _upgrades.GetCoinMultiplier() : 1f;
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
