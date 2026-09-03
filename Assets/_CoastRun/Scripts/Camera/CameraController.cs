using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// Compatibility facade — chase cam lives in RunnerCameraRig.
    [RequireComponent(typeof(RunnerCameraRig))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private RunnerCameraRig rig;

        public bool HandoffActive => Rig.HandoffActive;

        private RunnerCameraRig Rig
        {
            get
            {
                if (rig == null)
                    rig = GetComponent<RunnerCameraRig>();
                if (rig == null)
                    rig = gameObject.AddComponent<RunnerCameraRig>();
                return rig;
            }
        }

        private void Awake()
        {
            rig = Rig;
        }

        public void SetTarget(PlayerController player) => Rig.SetTarget(player);

        /// Legacy SoftHit API (strength, seconds) → Rig.Shake(duration, magnitude).
        public void Shake(float strength, float seconds) => Rig.Shake(seconds, strength);

        public void FovKick(float delta, float duration) => Rig.FovKick(delta, duration);

        public IEnumerator PlayGameplayHandoff(float duration) => Rig.PlayGameplayHandoff(duration);

        public void SetFollowSuspended(bool suspended) => Rig.SetFollowSuspended(suspended);

        public void SnapPose(Vector3 worldPos, Quaternion worldRot, float fieldOfView) =>
            Rig.SnapPose(worldPos, worldRot, fieldOfView);
    }
}
