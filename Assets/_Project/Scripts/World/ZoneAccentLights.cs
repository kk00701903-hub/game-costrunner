using System.Collections.Generic;
using UnityEngine;

/// Pooled accent lights per zone — shopfront sodium, scanner red, water haze.
public class ZoneAccentLights : MonoBehaviour
{
    [SerializeField] private int poolSize = 6;
    [SerializeField] private float range = 14f;
    [SerializeField] private float intensity = 1.4f;

    private readonly List<Light> _pool = new List<Light>();
    private Zone _zone = Zone.Arcade;
    private Transform _follow;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _follow = player.transform;

        BuildPool();

        if (ZoneDirector.Instance != null)
        {
            ZoneDirector.Instance.OnZoneChanged += HandleZone;
            HandleZone(ZoneDirector.Instance.CurrentZone);
        }
    }

    private void LateUpdate()
    {
        if (_follow == null || _pool.Count == 0)
            return;

        Vector3 basePos = _follow.position + _follow.forward * 18f + Vector3.up * 4f;
        for (int i = 0; i < _pool.Count; i++)
        {
            Light light = _pool[i];
            if (light == null)
                continue;

            float lane = (i - (_pool.Count - 1) * 0.5f) * 3.2f;
            light.transform.position = basePos + _follow.right * lane + Vector3.up * (i % 2) * 1.2f;
        }
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("ZoneAccent" + i);
            go.transform.SetParent(transform, false);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            _pool.Add(light);
        }
    }

    private void HandleZone(Zone zone)
    {
        _zone = zone;
        Color color = PaletteColor(zone);
        float power = intensity * ZoneIntensity(zone);

        for (int i = 0; i < _pool.Count; i++)
        {
            Light light = _pool[i];
            if (light == null)
                continue;

            light.color = color;
            light.intensity = power;
        }
    }

    private static Color PaletteColor(Zone zone)
    {
        switch (zone)
        {
            case Zone.Overpass:
                return new Color(0.72f, 0.78f, 0.92f);
            case Zone.Flooded:
                return new Color(0.42f, 0.82f, 0.72f);
            case Zone.Tower:
                return new Color(0.92f, 0.90f, 0.82f);
            case Zone.Depot:
                return new Color(0.98f, 0.42f, 0.22f);
            default:
                return new Color(0.98f, 0.82f, 0.58f);
        }
    }

    private static float ZoneIntensity(Zone zone)
    {
        return zone == Zone.Depot ? 2.2f : zone == Zone.Arcade ? 1.5f : 1.1f;
    }

    private void OnDestroy()
    {
        if (ZoneDirector.Instance != null)
            ZoneDirector.Instance.OnZoneChanged -= HandleZone;
    }
}
