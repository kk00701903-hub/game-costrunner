using UnityEngine;

/// One-shot burst when a pickup is collected.
public static class PickupBurstVfx
{
    private static Material _material;

    public static void Play(Vector3 position, PickupKind kind)
    {
        GameObject go = new GameObject("PickupBurst");
        go.transform.position = position + Vector3.up * 0.35f;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = 48;
        main.startLifetime = 0.45f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = ColorFor(kind);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.6f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(12 + (int)kind, 12, 28)) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = BurstMaterial();

        ps.Play();
        Object.Destroy(go, 1.2f);
    }

    private static Color ColorFor(PickupKind kind)
    {
        switch (kind)
        {
            case PickupKind.Tag:
                return new Color(0.92f, 0.90f, 0.82f);
            case PickupKind.Letter:
                return new Color(0.98f, 0.92f, 0.62f);
            case PickupKind.BoosterCell:
                return new Color(0.34f, 0.88f, 0.52f);
            case PickupKind.Shield:
                return new Color(0.42f, 0.72f, 0.96f);
            case PickupKind.ReverseScan:
                return new Color(0.44f, 0.86f, 0.88f);
            case PickupKind.DeckTape:
                return new Color(0.92f, 0.38f, 0.32f);
            case PickupKind.DeckPiece:
                return new Color(0.62f, 0.46f, 0.30f);
            default:
                return new Color(0.92f, 0.76f, 0.28f);
        }
    }

    private static Material BurstMaterial()
    {
        if (_material != null)
            return _material;

        Shader shader = Shader.Find("Particles/Standard Unlit") ??
                        Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                        Shader.Find("Sprites/Default");

        _material = new Material(shader);
        Texture2D tex = Resources.Load<Texture2D>("FX/FX_Mote");
        if (tex != null)
            _material.mainTexture = tex;

        return _material;
    }
}
