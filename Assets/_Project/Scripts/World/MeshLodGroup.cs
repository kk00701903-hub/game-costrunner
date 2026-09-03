using UnityEngine;

/// Simple distance LOD for imported Store meshes (two steps).
public class MeshLodGroup : MonoBehaviour
{
    [SerializeField] private float lod1Distance = 45f;
    [SerializeField] private float lod2Distance = 90f;

    private Renderer[] _renderers;
    private Transform _camera;
    private int _lastStep = -1;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        Camera cam = Camera.main;
        _camera = cam != null ? cam.transform : null;
    }

    private void LateUpdate()
    {
        if (_renderers == null || _renderers.Length == 0 || _camera == null)
            return;

        float distance = Vector3.Distance(_camera.position, transform.position);
        int step = distance < lod1Distance ? 0 : distance < lod2Distance ? 1 : 2;
        if (step == _lastStep)
            return;

        _lastStep = step;
        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null)
                continue;

            r.enabled = step < 2;
            if (step == 1 && r is MeshRenderer)
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
