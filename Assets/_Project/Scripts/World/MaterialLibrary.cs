using UnityEngine;

/// Baked URP Lit materials for roads, walls, props, and characters.
/// Prefer these over runtime Shader.Find so tiles stay consistent on mobile.
[CreateAssetMenu(menuName = "347/Art/MaterialLibrary", fileName = "MaterialLibrary")]
public class MaterialLibrary : ScriptableObject
{
    public const string ResourcePath = "347/MaterialLibrary";

    [Header("Surfaces")]
    public Material asphalt;
    public Material concrete;
    public Material metal;
    public Material emissiveSign;
    public Material water;
    public Material characterSkin;

    private static MaterialLibrary _cached;

    public static MaterialLibrary Active
    {
        get
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<MaterialLibrary>(ResourcePath);
            return _cached;
        }
    }

    public static void Override(MaterialLibrary library)
    {
        _cached = library;
    }

    public Material Surface(string token)
    {
        if (string.IsNullOrEmpty(token))
            return asphalt;

        if (token.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            token.IndexOf("Flooded", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return water != null ? water : asphalt;

        if (token.IndexOf("Metal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            token.IndexOf("Guardrail", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return metal != null ? metal : asphalt;

        if (token.IndexOf("Sign", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            token.IndexOf("Shop", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return emissiveSign != null ? emissiveSign : concrete;

        if (token.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            token.IndexOf("Concrete", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return concrete != null ? concrete : asphalt;

        return asphalt;
    }

    public bool HasBakedSurfaces =>
        asphalt != null || concrete != null || metal != null;
}
