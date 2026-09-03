using UnityEngine;

/// Axis-aligned hurt box for obstacles. Colliders may still exist for authoring,
/// but runtime hits go through HazardRegistry + Aabb — no physics queries.
[DisallowMultipleComponent]
public class HazardVolume : MonoBehaviour
{
    [SerializeField] private Vector3 size = new Vector3(1.2f, 1.4f, 1.6f);
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private bool lowBarrier;

    public Vector3 Size => size;
    public Vector3 Center => transform.TransformPoint(centerOffset);
    public bool IsLowBarrier => lowBarrier;

    private void OnEnable()
    {
        HazardRegistry.Instance.Register(this);
    }

    private void OnDisable()
    {
        HazardRegistry.Instance.Unregister(this);
    }

    public void ConfigureFromRenderer()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;

        Bounds b = renderer.bounds;
        size = b.size;
        // Store offset in local space so rotated corner tiles stay correct.
        centerOffset = transform.InverseTransformPoint(b.center);

        if (name.IndexOf("Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            lowBarrier = true;
            size.y = Mathf.Min(size.y, 0.7f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = lowBarrier ? new Color(1f, 0.8f, 0.2f, 0.35f) : new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireCube(Center, size);
    }
#endif
}
