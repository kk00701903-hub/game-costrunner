using UnityEngine;

/// Cheap blob shadow for mobile (one quad, one draw call).
[RequireComponent(typeof(PlayerController))]
public class CharacterShadow : MonoBehaviour
{
    [SerializeField] private float radius = 0.55f;
    [SerializeField] private float heightOffset = 0.02f;
    [SerializeField] private Color tint = new Color(0f, 0f, 0f, 0.38f);

    private Transform _quad;
    private Material _material;

    private void Awake()
    {
        Build();
    }

    private void LateUpdate()
    {
        if (_quad == null)
            return;

        Vector3 pos = transform.position;
        pos.y = heightOffset;
        _quad.position = pos;

        float scale = radius * 2f;
        PlayerController player = GetComponent<PlayerController>();
        if (player != null && !player.IsGrounded)
            scale *= 0.72f;

        _quad.localScale = new Vector3(scale, scale, scale);
        _quad.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }

    private void Build()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "BlobShadow";
        Destroy(go.GetComponent<Collider>());
        _quad = go.transform;
        _quad.SetParent(transform, false);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Transparent") ??
                        Shader.Find("Sprites/Default");

        _material = new Material(shader);
        if (_material.HasProperty("_BaseColor"))
            _material.SetColor("_BaseColor", tint);
        if (_material.HasProperty("_Color"))
            _material.color = tint;

        Renderer renderer = go.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = _material;
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
