using UnityEngine;

namespace CoastRun
{
    public enum PickupKind
    {
        Jelly,        // score + a sip of stamina; spawned in trails
        BigJelly,     // bonus-time jelly: 3× score
        Potion,       // big stamina refill
        BonusStar     // starts Bonus Time
    }

    /// Cookie-Run pickups. Jellies are the breadcrumbs that pull the player through
    /// the level; potions keep the stamina bar alive; the star kicks off Bonus Time.
    /// Magnet pull mirrors CoinPickup so both families feel the same.
    public class JellyPickup : MonoBehaviour
    {
        private static readonly Color[] JellyColors =
        {
            new Color(1.00f, 0.42f, 0.62f),   // strawberry
            new Color(0.40f, 0.78f, 1.00f),   // soda
            new Color(1.00f, 0.86f, 0.30f),   // lemon
            new Color(0.55f, 0.90f, 0.45f),   // lime
            new Color(0.80f, 0.55f, 1.00f),   // grape
        };

        private PickupKind _kind;
        private Transform _player;
        private UpgradeManager _upgrades;
        private Transform _visualRoot;
        private bool _collected;
        private float _spin;
        private float _bobPhase;
        private bool _magnetActive;
        private float _magnetT;
        private Vector3 _magnetStart;
        private float _magnetBend;
        private Vector3 _basePos;

        public PickupKind Kind => _kind;

        public static JellyPickup Spawn(PickupKind kind, Transform parent, Vector3 worldPos, Transform player,
            UpgradeManager upgrades, int colorIndex = -1)
        {
            var go = new GameObject(kind.ToString());
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.rotation = DownhillPath.Rotation;

            var p = go.AddComponent<JellyPickup>();
            p._kind = kind;
            p._player = player;
            p._upgrades = upgrades;
            p._bobPhase = Random.value * Mathf.PI * 2f;
            p._basePos = worldPos;

            var vis = new GameObject("VisualRoot").transform;
            vis.SetParent(go.transform, false);
            p._visualRoot = vis;

            float radius;
            switch (kind)
            {
                case PickupKind.Potion:
                    BuildPotion(vis);
                    radius = 0.6f;
                    break;
                case PickupKind.BonusStar:
                    BuildStar(vis);
                    radius = 0.8f;
                    break;
                case PickupKind.BigJelly:
                    BuildJelly(vis, colorIndex, 0.42f, true);
                    radius = 0.6f;
                    break;
                default:
                    BuildJelly(vis, colorIndex, 0.3f, false);
                    radius = 0.55f;
                    break;
            }

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            col.center = new Vector3(0f, 0.25f, 0f);

            BlobShadow.Attach(go.transform, kind == PickupKind.Jelly ? 0.4f : 0.6f);
            return p;
        }

        private static void BuildJelly(Transform root, int colorIndex, float size, bool rainbow)
        {
            Color c = colorIndex >= 0
                ? JellyColors[colorIndex % JellyColors.Length]
                : JellyColors[Random.Range(0, JellyColors.Length)];
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Jelly";
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            body.transform.localScale = new Vector3(size, size * 0.55f, size);
            Object.Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c, 0.6f);

            // Highlight dot so it reads as glossy candy at a glance.
            var gloss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gloss.name = "Gloss";
            gloss.transform.SetParent(body.transform, false);
            gloss.transform.localPosition = new Vector3(0.25f, 0.28f, -0.2f);
            gloss.transform.localScale = Vector3.one * 0.22f;
            Object.Destroy(gloss.GetComponent<Collider>());
            gloss.GetComponent<Renderer>().sharedMaterial =
                CoastMaterials.CreateUnlit(rainbow ? Color.white : Color.Lerp(c, Color.white, 0.75f));
        }

        private static void BuildPotion(Transform root)
        {
            var flask = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flask.name = "Flask";
            flask.transform.SetParent(root, false);
            flask.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            flask.transform.localScale = new Vector3(0.42f, 0.5f, 0.42f);
            Object.Destroy(flask.GetComponent<Collider>());
            flask.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(1f, 0.35f, 0.45f), 0.7f);

            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root, false);
            neck.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            neck.transform.localScale = new Vector3(0.16f, 0.1f, 0.16f);
            Object.Destroy(neck.GetComponent<Collider>());
            neck.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(Color.Lerp(Color.white, CoastPalette.TownCream, 0.5f));

            var cork = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cork.name = "Cork";
            cork.transform.SetParent(root, false);
            cork.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            cork.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
            Object.Destroy(cork.GetComponent<Collider>());
            cork.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.38f, 0.22f));

            // Plus sign so it is readable as "health" at speed.
            var h = GameObject.CreatePrimitive(PrimitiveType.Cube);
            h.transform.SetParent(root, false);
            h.transform.localPosition = new Vector3(0f, 0.28f, -0.2f);
            h.transform.localScale = new Vector3(0.2f, 0.06f, 0.04f);
            Object.Destroy(h.GetComponent<Collider>());
            h.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(Color.white);
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.transform.SetParent(root, false);
            v.transform.localPosition = new Vector3(0f, 0.28f, -0.2f);
            v.transform.localScale = new Vector3(0.06f, 0.2f, 0.04f);
            Object.Destroy(v.GetComponent<Collider>());
            v.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(Color.white);
        }

        private static void BuildStar(Transform root)
        {
            // Five flattened lozenges around a core → a chunky star that spins.
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(root, false);
            core.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            core.transform.localScale = Vector3.one * 0.38f;
            Object.Destroy(core.GetComponent<Collider>());
            var gold = CoastMaterials.CreateUnlit(new Color(1f, 0.85f, 0.2f));
            core.GetComponent<Renderer>().sharedMaterial = gold;
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f + 90f;
                var pt = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pt.transform.SetParent(root, false);
                pt.transform.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.3f,
                    0.45f + Mathf.Sin(a * Mathf.Deg2Rad) * 0.3f, 0f);
                pt.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                pt.transform.localScale = new Vector3(0.36f, 0.16f, 0.12f);
                Object.Destroy(pt.GetComponent<Collider>());
                pt.GetComponent<Renderer>().sharedMaterial = gold;
            }
        }

        private void Update()
        {
            if (_collected)
                return;

            float spinSpeed = _kind == PickupKind.BonusStar ? 240f : 120f;
            _spin += Time.deltaTime * spinSpeed;
            if (_visualRoot != null)
            {
                _visualRoot.localRotation = Quaternion.Euler(0f, _spin, 0f);
                float bob = Mathf.Sin(Time.time * 3f + _bobPhase) * 0.06f;
                _visualRoot.localPosition = new Vector3(0f, bob, 0f);
            }

            if (_player == null)
                return;

            float magnet = (_upgrades != null ? _upgrades.GetMagnetRadius() : 1.4f) + PetCompanion.MagnetBonus;
            if (_kind == PickupKind.BonusStar || _kind == PickupKind.Potion)
                magnet += 0.6f;   // the rare ones should never be a near miss
            if (BonusTimeDirector.IsActive)
                magnet += 1.5f;

            Vector3 toPlayer = _player.position - transform.position;
            if (toPlayer.sqrMagnitude > magnet * magnet)
            {
                _magnetActive = false;
                return;
            }

            if (!_magnetActive)
            {
                _magnetActive = true;
                _magnetT = 0f;
                _magnetStart = transform.position;
                _magnetBend = Random.Range(0.3f, 0.6f) * (Random.value > 0.5f ? 1f : -1f);
            }

            _magnetT += Time.deltaTime * 3f;
            float u = Mathf.Clamp01(_magnetT);
            float e = u * u * (3f - 2f * u);
            Vector3 end = _player.position + Vector3.up * 0.3f;
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
            _collected = true;
            var health = HealthSystem.Instance;
            var hud = RunHudChrome.Instance;
            var juice = JuiceDirector.Instance;
            Vector3 pos = transform.position + Vector3.up * 0.3f;

            switch (_kind)
            {
                case PickupKind.Jelly:
                    health?.HealJelly();
                    hud?.AddScore(10, pos, false);
                    StageRunStats.Instance?.NotifyJelly(1);
                    break;
                case PickupKind.BigJelly:
                    health?.HealJelly();
                    hud?.AddScore(30, pos, false);
                    StageRunStats.Instance?.NotifyJelly(1);
                    break;
                case PickupKind.Potion:
                    health?.HealPotion();
                    hud?.AddScore(50, pos, true);
                    hud?.Flash(new Color(1f, 0.5f, 0.6f, 0.35f));
                    break;
                case PickupKind.BonusStar:
                    hud?.AddScore(100, pos, true);
                    BonusTimeDirector.Instance?.Activate();
                    break;
            }

            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            // The pop coroutine (JuiceDirector) owns the detached visual and destroys it.
            if (_visualRoot != null)
                _visualRoot.SetParent(null, true);
            if (juice != null)
                juice.PlayCoinCollect(_visualRoot, pos, _kind == PickupKind.Jelly ? 0 : 1);
            else if (_visualRoot != null)
                Destroy(_visualRoot.gameObject);
            Destroy(gameObject);
        }
    }
}
