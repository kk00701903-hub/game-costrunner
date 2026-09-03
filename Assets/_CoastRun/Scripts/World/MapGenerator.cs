using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Infinite coastal promenade stream: road centre, town left, sea right.
    public class MapGenerator : MonoBehaviour, IMapStream
    {
        [SerializeField] private float roadY;
        [SerializeField] private int segmentsAhead = 6;
        [SerializeField] private int segmentsBehind = 2;

        private readonly Dictionary<int, GameObject> _segments = new Dictionary<int, GameObject>();
        private int _lastCentreIndex = int.MinValue;

        public bool TryGetPose(float pathDistance, out Vector3 position, out float yaw)
        {
            position = DownhillPath.Point(pathDistance);
            yaw = 0f;
            return true;
        }

        public void SetPlayerDistance(float pathDistance)
        {
            int centre = Mathf.FloorToInt(pathDistance / PromenadeSegmentBuilder.Length);
            if (centre == _lastCentreIndex)
                return;

            _lastCentreIndex = centre;

            for (int i = centre - segmentsBehind; i <= centre + segmentsAhead; i++)
            {
                if (!_segments.ContainsKey(i))
                    _segments[i] = PromenadeSegmentBuilder.Build(i, transform);
            }

            var toRemove = new List<int>();
            foreach (var kv in _segments)
            {
                if (kv.Key < centre - segmentsBehind - 1 || kv.Key > centre + segmentsAhead + 1)
                    toRemove.Add(kv.Key);
            }

            foreach (int idx in toRemove)
            {
                if (_segments.TryGetValue(idx, out GameObject seg) && seg != null)
                    Destroy(seg);
                _segments.Remove(idx);
            }
        }

        private void OnDestroy()
        {
            foreach (var kv in _segments)
            {
                if (kv.Value != null)
                    Destroy(kv.Value);
            }

            _segments.Clear();
        }
    }
}
