using System.Collections.Generic;
using UnityEngine;

/// Shared loader for the CC0 surface textures under Resources/Textures.
/// Materials are cached so the pooled tiles all share one instance and the
/// season tint keeps working through MaterialPropertyBlock.
public static class ArtLibrary
{
    private const string TextureFolder = "Textures/";

    private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    private static Shader _surfaceShader;

    public static Texture2D Texture(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (_textures.TryGetValue(fileName, out Texture2D cached))
            return cached;

        Texture2D tex = Resources.Load<Texture2D>(TextureFolder + fileName);
        _textures[fileName] = tex;
        return tex;
    }

    public static Shader SurfaceShader()
    {
        if (_surfaceShader != null)
            return _surfaceShader;

        // URP project — Standard renders magenta. Prefer Lit/Simple Lit.
        _surfaceShader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Universal Render Pipeline/Simple Lit") ??
                         Shader.Find("Standard") ??
                         Shader.Find("Legacy Shaders/Diffuse");
        return _surfaceShader;
    }

    /// A rough opaque surface. Falls back to a plain tinted material when the
    /// texture is missing, so the game still runs on a bare checkout.
    public static Material Surface(string colorTexture, string normalTexture, Color tint, Vector2 tiling, float smoothness = 0.12f)
    {
        Material baked = TryBakedSurface(colorTexture);
        if (baked != null)
            return baked;

        string key = colorTexture + "|" + normalTexture + "|" + tiling + "|" + tint + "|" + smoothness;
        if (_materials.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Shader shader = SurfaceShader();
        if (shader == null)
            return null;

        var material = new Material(shader) { name = colorTexture ?? "Surface" };
        Apply(material, "_Color", tint);
        Apply(material, "_BaseColor", tint);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        Texture2D albedo = Texture(colorTexture);
        if (albedo != null)
        {
            SetMap(material, "_MainTex", albedo, tiling);
            SetMap(material, "_BaseMap", albedo, tiling);
        }

        Texture2D normal = Texture(normalTexture);
        if (normal != null)
        {
            SetMap(material, "_BumpMap", normal, tiling);
            material.EnableKeyword("_NORMALMAP");
        }

        _materials[key] = material;
        return material;
    }

    private static void SetMap(Material material, string property, Texture2D tex, Vector2 tiling)
    {
        if (!material.HasProperty(property))
            return;

        material.SetTexture(property, tex);
        material.SetTextureScale(property, tiling);
    }

    private static void Apply(Material material, string property, Color color)
    {
        if (material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static Material TryBakedSurface(string colorTexture)
    {
        MaterialLibrary library = MaterialLibrary.Active;
        if (library == null || !library.HasBakedSurfaces)
            return null;

        if (string.IsNullOrEmpty(colorTexture))
            return library.asphalt;

        if (colorTexture.IndexOf("Asphalt", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            colorTexture.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return library.asphalt;

        if (colorTexture.IndexOf("Concrete", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            colorTexture.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return library.concrete;

        return library.Surface(colorTexture);
    }

    /// Swap Built-in / broken shaders to URP Lit, keeping albedo when possible.
    public static void EnsureVisible(GameObject root)
    {
        if (root == null)
            return;

        Shader shader = SurfaceShader();
        if (shader == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer || r is SpriteRenderer)
                continue;

            Material[] shared = r.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                r.sharedMaterial = MakeUrpFallback(null, new Color(0.32f, 0.32f, 0.34f), true);
                continue;
            }

            Material[] next = null;
            for (int m = 0; m < shared.Length; m++)
            {
                Material mat = shared[m];
                if (!NeedsUrpSwap(mat))
                    continue;

                if (next == null)
                {
                    next = new Material[shared.Length];
                    for (int c = 0; c < shared.Length; c++)
                        next[c] = shared[c];
                }

                next[m] = MakeUrpFrom(mat, true);
            }

            if (next != null)
                r.sharedMaterials = next;
        }
    }

    /// Character/props: swap broken shaders only; optional skin texture on skin slots.
    public static void EnsureCharacterVisible(GameObject root, Texture2D skinOverride = null)
    {
        if (root == null)
            return;

        Shader shader = SurfaceShader();
        if (shader == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer || r is SpriteRenderer)
                continue;

            Material[] shared = r.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                var fallback = new[] { MakeUrpFallback(skinOverride, new Color(0.92f, 0.78f, 0.66f), false) };
                r.sharedMaterials = fallback;
                continue;
            }

            Material[] next = null;
            for (int m = 0; m < shared.Length; m++)
            {
                Material mat = shared[m];
                Material use = mat;
                if (NeedsUrpSwap(mat))
                {
                    if (next == null)
                    {
                        next = new Material[shared.Length];
                        for (int c = 0; c < shared.Length; c++)
                            next[c] = shared[c];
                    }

                    use = MakeUrpFrom(mat, false);
                    next[m] = use;
                }

                if (skinOverride != null && ShouldApplySkinTexture(r, mat, m))
                {
                    if (next == null)
                    {
                        next = new Material[shared.Length];
                        for (int c = 0; c < shared.Length; c++)
                            next[c] = shared[c];
                    }

                    if (next[m] == shared[m])
                        next[m] = new Material(use) { name = use.name + "_Skin" };

                    SetMap(next[m], "_BaseMap", skinOverride, Vector2.one);
                    SetMap(next[m], "_MainTex", skinOverride, Vector2.one);
                }
            }

            if (next != null)
                r.sharedMaterials = next;
        }
    }

    private static bool ShouldApplySkinTexture(Renderer renderer, Material mat, int slot)
    {
        string meshName = renderer != null ? renderer.gameObject.name : string.Empty;
        string matName = mat != null ? mat.name : string.Empty;

        if (matName.IndexOf("skin", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            matName.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            matName.IndexOf("body", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (meshName.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            meshName.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            meshName.IndexOf("skin", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (matName.IndexOf("shirt", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            matName.IndexOf("pants", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            matName.IndexOf("cloth", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            matName.IndexOf("short", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        // Kenney humanoid: slot 0 is usually exposed skin/head.
        return slot == 0;
    }

    private static bool NeedsUrpSwap(Material mat)
    {
        if (mat == null || mat.shader == null)
            return true;

        string n = mat.shader.name;
        if (n.IndexOf("Hidden/InternalErrorShader", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (!mat.shader.isSupported)
            return true;

        // Already on a working URP shader — keep it.
        if (n.IndexOf("Universal Render Pipeline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        // Built-in Standard / Autodesk / HDRP pink under URP.
        return true;
    }

    private static Material MakeUrpFrom(Material source, bool roadFallback)
    {
        Texture2D albedo = null;
        Color tint = roadFallback ? new Color(0.32f, 0.32f, 0.34f) : Color.white;

        if (source != null)
        {
            if (source.HasProperty("_BaseColor"))
                tint = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color"))
                tint = source.color;

            if (source.HasProperty("_BaseMap"))
                albedo = source.GetTexture("_BaseMap") as Texture2D;
            if (albedo == null && source.HasProperty("_MainTex"))
                albedo = source.GetTexture("_MainTex") as Texture2D;
        }

        if (albedo == null && roadFallback)
            albedo = Texture("Road_Asphalt");

        return MakeUrpFallback(albedo, tint, roadFallback);
    }

    private static Material MakeUrpFallback(Texture2D albedo, Color tint, bool roadTiling)
    {
        Shader shader = SurfaceShader();
        var material = new Material(shader) { name = "UrpFallback" };
        Apply(material, "_Color", tint);
        Apply(material, "_BaseColor", tint);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.18f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.18f);
        if (material.HasProperty("_EnvironmentReflections"))
            material.SetFloat("_EnvironmentReflections", 1f);

        if (albedo != null)
        {
            Vector2 tiling = roadTiling ? new Vector2(4f, 4f) : Vector2.one;
            SetMap(material, "_MainTex", albedo, tiling);
            SetMap(material, "_BaseMap", albedo, tiling);
        }

        return material;
    }
}
