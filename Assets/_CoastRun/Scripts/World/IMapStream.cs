using UnityEngine;

namespace CoastRun
{
    /// Path sampler used by the player and camera. MapGenerator implements this.
    public interface IMapStream
    {
        bool TryGetPose(float pathDistance, out Vector3 position, out float yaw);
        void SetPlayerDistance(float pathDistance);
    }
}
