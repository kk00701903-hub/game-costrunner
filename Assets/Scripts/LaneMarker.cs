using UnityEngine;

public enum LaneMarkStyle
{
    /// Red band: something is about to arrive in this lane.
    Aim,

    /// Gold pad: step in and the counter fires itself.
    Counter,

    /// Gold pad with a red rim. Looks like a counter, costs a crack.
    Trap,

    /// Ground crack. Reads as a threat, never becomes one.
    Crack
}

/// A painted strip on the road. Every band carries a shape as well as a colour
/// so the read survives colour blindness.
public class LaneMarker : MonoBehaviour
{
    private static Texture2D _hatch;
    private static Texture2D _dots;
    private static Texture2D _crack;

    private Renderer _renderer;
    private Material _material;
    private PlayerController _player;
    private float _laneOffset = 2.5f;
    private float _lead;
    private float _length;
    private float _flash;

    private Color _baseTint;
    private float _life = 1f;

    public int Lane { get; private set; }
    public LaneMarkStyle Style { get; private set; }
    public bool Visible { get; private set; }

    public static LaneMarker Create(Transform parent, PlayerController player)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "LaneMark";
        go.transform.SetParent(parent, false);

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        LaneMarker marker = go.AddComponent<LaneMarker>();
        marker._player = player;
        marker._renderer = go.GetComponent<Renderer>();
        marker._material = new Material(FindShader());
        marker._renderer.sharedMaterial = marker._material;
        marker._laneOffset = player != null ? player.LaneOffset : 2.5f;
        go.SetActive(false);
        return marker;
    }

    private static Shader FindShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit") ??
               Shader.Find("Unlit/Transparent") ??
               Shader.Find("Sprites/Default");
    }

    public void Show(int lane, LaneMarkStyle style, float lead, float length)
    {
        Lane = lane;
        Style = style;
        _lead = lead;
        _length = length;
        Visible = true;
        _flash = 0f;

        Apply(style);
        gameObject.SetActive(true);
        Follow();
    }

    public void Hide()
    {
        Visible = false;
        gameObject.SetActive(false);
    }

    private void Apply(LaneMarkStyle style)
    {
        Color tint;
        Texture2D pattern;

        switch (style)
        {
            case LaneMarkStyle.Counter:
                tint = new Color(1f, 0.82f, 0.28f, 0.78f);
                pattern = Dots();
                break;
            case LaneMarkStyle.Trap:
                tint = new Color(1f, 0.66f, 0.30f, 0.72f);
                pattern = Dots();
                break;
            case LaneMarkStyle.Crack:
                tint = new Color(0.24f, 0.23f, 0.22f, 0.85f);
                pattern = Crack();
                break;
            default:
                tint = new Color(0.92f, 0.16f, 0.16f, 0.62f);
                pattern = Hatch();
                break;
        }

        SetTexture(pattern);
        _baseTint = tint;
        _life = 1f;
        SetColor(tint);
    }

    /// Counter remaining time is shown as gold desaturation, never as a bar.
    public void SetLife(float normalized)
    {
        _life = Mathf.Clamp01(normalized);
        if (!Visible)
            return;

        if (Style != LaneMarkStyle.Counter && Style != LaneMarkStyle.Trap)
            return;

        Color c = _baseTint;
        float sat = Mathf.Lerp(0.15f, 1f, _life);
        float grey = (c.r + c.g + c.b) / 3f;
        c.r = Mathf.Lerp(grey, c.r, sat);
        c.g = Mathf.Lerp(grey, c.g, sat);
        c.b = Mathf.Lerp(grey, c.b, sat);
        c.a = Mathf.Lerp(0.25f, _baseTint.a, 0.35f + 0.65f * _life);
        SetColor(c);
    }

    private void SetTexture(Texture2D tex)
    {
        if (_material == null)
            return;

        if (_material.HasProperty("_BaseMap"))
            _material.SetTexture("_BaseMap", tex);
        if (_material.HasProperty("_MainTex"))
            _material.SetTexture("_MainTex", tex);
    }

    private void SetColor(Color color)
    {
        if (_material == null)
            return;

        if (_material.HasProperty("_BaseColor"))
            _material.SetColor("_BaseColor", color);
        if (_material.HasProperty("_Color"))
            _material.SetColor("_Color", color);

        if (_material.HasProperty("_EmissionColor") &&
            (Style == LaneMarkStyle.Counter || Style == LaneMarkStyle.Aim))
        {
            _material.EnableKeyword("_EMISSION");
            _material.SetColor("_EmissionColor", color * 0.9f);
        }

        _material.renderQueue = 3050;
    }

    private void LateUpdate()
    {
        if (!Visible)
            return;

        Follow();

        // Only the trap pulses, and it pulses against its own rim, so the tell
        // is a shape change rather than a colour change.
        if (Style == LaneMarkStyle.Trap)
        {
            _flash += Time.deltaTime * 7f;
            float a = Mathf.Lerp(0.42f, 0.86f, (Mathf.Sin(_flash) + 1f) * 0.5f);
            SetColor(new Color(1f, 0.52f, 0.24f, a));
        }
    }

    private void Follow()
    {
        if (_player == null)
            return;

        RoadSpawner spawner = RoadSpawner.Instance;
        float distance = _player.PathDistance + _lead;

        Vector3 point;
        float yaw;
        if (spawner == null || !spawner.TryGetPoint(distance, out point, out yaw))
        {
            point = _player.transform.position + Quaternion.Euler(0f, _player.Yaw, 0f) * new Vector3(0f, 0f, _lead);
            yaw = _player.Yaw;
        }

        Quaternion frame = Quaternion.Euler(0f, yaw, 0f);
        transform.position = point + frame * new Vector3(Lane * _laneOffset, 0.09f, 0f);
        transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        transform.localScale = new Vector3(_laneOffset * 0.92f, _length, 1f);
    }

    private static Texture2D Hatch()
    {
        if (_hatch != null)
            return _hatch;

        _hatch = Build(32, (x, y) => ((x + y) % 10) < 5 ? 1f : 0.25f);
        return _hatch;
    }

    private static Texture2D Dots()
    {
        if (_dots != null)
            return _dots;

        _dots = Build(32, (x, y) =>
        {
            float dx = (x % 8) - 3.5f;
            float dy = (y % 8) - 3.5f;
            return Mathf.Sqrt(dx * dx + dy * dy) < 2.4f ? 1f : 0.2f;
        });
        return _dots;
    }

    private static Texture2D Crack()
    {
        if (_crack != null)
            return _crack;

        _crack = Build(32, (x, y) =>
        {
            float spine = Mathf.Abs(x - 16f - Mathf.Sin(y * 0.5f) * 3f);
            return spine < 1.6f ? 1f : 0.08f;
        });
        return _crack;
    }

    private static Texture2D Build(int size, System.Func<int, int, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y)));
        }

        tex.Apply(false, false);
        return tex;
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
