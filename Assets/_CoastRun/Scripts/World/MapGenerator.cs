using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Infinite coastal promenade stream: road centre, town left, sea right.
    public class MapGenerator : MonoBehaviour, IMapStream
    {
        [SerializeField] private float roadY;
        [SerializeField] private int segmentsAhead = 10;  // ~300 m covers pulled-back chase cam
        [SerializeField] private int segmentsBehind = 2;

        private readonly Dictionary<int, GameObject> _segments = new Dictionary<int, GameObject>();
        private int _lastCentreIndex = int.MinValue;
        private float _lastPathDistance = float.NaN;

        private void Awake()
        {
            // Prefill before the first player Update so frame 0 isn't bare asphalt + sky.
            WarmStart(0f);
        }

        public bool TryGetPose(float pathDistance, out Vector3 position, out float yaw)
        {
            position = DownhillPath.Point(pathDistance);
            yaw = 0f;
            return true;
        }

        /// Build the starting tiles immediately so the first frame isn't empty road + flat sky.
        public void WarmStart(float pathDistance = 0f)
        {
            _lastCentreIndex = int.MinValue;
            _lastPathDistance = float.NaN;
            SetPlayerDistance(pathDistance);
        }

        public void SetPlayerDistance(float pathDistance)
        {
            int centre = Mathf.FloorToInt(pathDistance / PromenadeSegmentBuilder.Length);
            int from = centre - segmentsBehind;
            int to = centre + segmentsAhead;

            // Always fill holes — a prior failed Build used to lock centre and leave a void forever.
            bool need = centre != _lastCentreIndex;
            if (!need)
            {
                for (int i = from; i <= to; i++)
                {
                    if (!_segments.ContainsKey(i) || _segments[i] == null)
                    {
                        need = true;
                        break;
                    }
                }
            }
            if (!need) return;

            for (int i = from; i <= to; i++)
            {
                if (_segments.TryGetValue(i, out var existing) && existing != null)
                    continue;
                if (existing == null)
                    _segments.Remove(i);

                try
                {
                    _segments[i] = PromenadeSegmentBuilder.Build(i, transform);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MapGenerator] segment {i} failed: {e.Message}");
                    // Destroy any orphan partial child with this name (Build creates root first).
                    for (int c = transform.childCount - 1; c >= 0; c--)
                    {
                        var child = transform.GetChild(c);
                        if (child != null && child.name == "Segment_" + i)
                            Destroy(child.gameObject);
                    }
                }
            }

            _lastCentreIndex = centre;
            _lastPathDistance = pathDistance;

            var toRemove = new List<int>();
            foreach (var kv in _segments)
            {
                if (kv.Key < from - 1 || kv.Key > to + 1)
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
