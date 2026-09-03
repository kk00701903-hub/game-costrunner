using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// Scrolls registered road materials with player speed (fake motion on asphalt).
    public static class RoadUvScroller
    {
        private static readonly List<Material> Roads = new List<Material>(32);
        private static float _scroll;
        private static float _speed;

        public static void Register(Material mat)
        {
            if (mat == null || Roads.Contains(mat))
                return;
            Roads.Add(mat);
        }

        public static void SetScrollSpeed(float metresPerSecond)
        {
            _speed = Mathf.Max(0f, metresPerSecond);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootTicker()
        {
            var go = new GameObject("RoadUvScroller");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<RoadUvScrollerBehaviour>();
        }

        private class RoadUvScrollerBehaviour : MonoBehaviour
        {
            private void Update()
            {
                if (_speed < 0.01f || Roads.Count == 0)
                    return;

                _scroll += _speed * 0.085f * Time.deltaTime;
                if (_scroll > 1000f)
                    _scroll -= 1000f;

                var offset = new Vector2(0f, -_scroll);
                for (int i = Roads.Count - 1; i >= 0; i--)
                {
                    var mat = Roads[i];
                    if (mat == null)
                    {
                        Roads.RemoveAt(i);
                        continue;
                    }

                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTextureOffset("_BaseMap", offset);
                    else
                        mat.mainTextureOffset = offset;
                }
            }
        }
    }
}
