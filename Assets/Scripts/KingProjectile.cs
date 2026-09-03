using UnityEngine;

public enum ThrowKind
{
    /// Sidestep it.
    ContainerShard,

    /// Slide under it.
    RetrievalNet,

    /// Jump it, and eat a counter lockout if it lands.
    TagLauncher,

    /// Comes from above, but the shadow is drawn on the road first.
    RailDrop
}

/// A thrown object resolved on arrival rather than by collision, so a hit is
/// decided by the same lane arithmetic the player is reading off the road.
public class KingProjectile : MonoBehaviour
{
    private PlayerController _player;
    private Renderer _body;
    private Transform _shadow;
    private ThrowKind _kind;
    private int _lane;
    private float _laneOffset = 2.5f;
    private float _startDistance;
    private float _flight;
    private float _elapsed;
    private bool _live;

    public bool Live => _live;
    public int Lane => _lane;

    public static KingProjectile Create(Transform parent, PlayerController player)
    {
        GameObject go = new GameObject("Throw");
        go.transform.SetParent(parent, false);

        KingProjectile shot = go.AddComponent<KingProjectile>();
        shot._player = player;
        shot._laneOffset = player != null ? player.LaneOffset : 2.5f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        Collider col = body.GetComponent<Collider>();
        if (col != null)
            Destroy(col);
        shot._body = body.GetComponent<Renderer>();

        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadow.name = "Shadow";
        shadow.transform.SetParent(go.transform, false);
        Collider shadowCol = shadow.GetComponent<Collider>();
        if (shadowCol != null)
            Destroy(shadowCol);
        shot._shadow = shadow.transform;
        Renderer shadowRenderer = shadow.GetComponent<Renderer>();
        shadowRenderer.sharedMaterial = MakeMaterial(new Color(0f, 0f, 0f, 0.45f));

        go.SetActive(false);
        return shot;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Transparent") ??
                        Shader.Find("Sprites/Default");

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.renderQueue = 3040;
        return material;
    }

    public void Launch(int lane, ThrowKind kind, float startDistance, float flightSeconds)
    {
        _lane = lane;
        _kind = kind;
        _startDistance = startDistance;
        _flight = Mathf.Max(0.12f, flightSeconds);
        _elapsed = 0f;
        _live = true;

        Vector3 scale;
        Color color;
        switch (kind)
        {
            case ThrowKind.RetrievalNet:
                scale = new Vector3(2.2f, 0.12f, 0.9f);
                color = new Color(0.72f, 0.70f, 0.30f);
                break;
            case ThrowKind.TagLauncher:
                scale = new Vector3(1.5f, 0.22f, 0.6f);
                color = new Color(0.85f, 0.25f, 0.20f);
                break;
            case ThrowKind.RailDrop:
                scale = new Vector3(1.6f, 1.6f, 1.6f);
                color = new Color(0.38f, 0.40f, 0.42f);
                break;
            default:
                scale = new Vector3(1.1f, 1.1f, 1.1f);
                color = new Color(0.52f, 0.46f, 0.34f);
                break;
        }

        _body.transform.localScale = scale;
        _body.sharedMaterial = MakeMaterial(color);
        _shadow.localScale = new Vector3(scale.x * 1.2f, scale.z * 1.4f, 1f);
        _shadow.gameObject.SetActive(kind == ThrowKind.RailDrop);

        gameObject.SetActive(true);
        Place(0f);
    }

    private void Update()
    {
        if (!_live)
            return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _flight);
        Place(t);

        if (t < 1f)
            return;

        Resolve();
        Retire();
    }

    private void Place(float t)
    {
        if (_player == null)
            return;

        float target = _player.PathDistance + 0.4f;
        float distance = Mathf.Lerp(_startDistance, target, t);

        RoadSpawner spawner = RoadSpawner.Instance;
        Vector3 point;
        float yaw;
        if (spawner == null || !spawner.TryGetPoint(distance, out point, out yaw))
        {
            point = _player.transform.position;
            yaw = _player.Yaw;
        }

        Quaternion frame = Quaternion.Euler(0f, yaw, 0f);
        float height = Height(t);
        transform.position = point + frame * new Vector3(_lane * _laneOffset, 0f, 0f);
        transform.rotation = frame;
        _body.transform.localPosition = new Vector3(0f, height, 0f);

        if (_shadow.gameObject.activeSelf)
        {
            _shadow.localPosition = new Vector3(0f, 0.1f, 0f);
            _shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private float Height(float t)
    {
        switch (_kind)
        {
            case ThrowKind.RetrievalNet:
                return 0.85f;
            case ThrowKind.TagLauncher:
                return 0.18f;
            case ThrowKind.RailDrop:
                return Mathf.Lerp(16f, 0.9f, t * t);
            default:
                return 1.05f;
        }
    }

    /// One action clears each kind, and the kind is readable from its height.
    private void Resolve()
    {
        if (_player == null || _player.IsDead)
            return;

        if ((int)_player.CurrentLane != _lane)
            return;

        bool dodged;
        switch (_kind)
        {
            case ThrowKind.RetrievalNet:
                dodged = _player.IsSliding;
                break;
            case ThrowKind.TagLauncher:
                dodged = !_player.IsGrounded;
                break;
            default:
                dodged = false;
                break;
        }

        if (dodged)
            return;

        HitKind hit = _kind == ThrowKind.TagLauncher ? HitKind.TagLauncher : HitKind.KingThrow;
        _player.TakeHit(hit);
    }

    public void Retire()
    {
        _live = false;
        gameObject.SetActive(false);
    }
}
