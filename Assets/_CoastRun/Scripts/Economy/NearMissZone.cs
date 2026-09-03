using UnityEngine;

namespace CoastRun
{
    /// Wide trigger around an obstacle. Awards only if player leaves without hard-hit.
    [RequireComponent(typeof(Collider))]
    public class NearMissZone : MonoBehaviour
    {
        [SerializeField] private int baseReward = 10;
        [SerializeField] private int laneHint;
        [SerializeField] private float rearmSeconds = 2.5f;

        private bool _playerInside;
        private bool _hardHit;
        private bool _armed = true;
        private float _rearmAt;

        public int BaseReward => baseReward;
        public int LaneHint => laneHint;

        public void Configure(int reward, int lane)
        {
            baseReward = reward;
            laneHint = lane;
        }

        private void Update()
        {
            if (_armed || Time.time < _rearmAt)
                return;
            _armed = true;
            _hardHit = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_armed || !IsPlayer(other))
                return;

            _playerInside = true;
            _hardHit = false;
            NearMissSystem.Instance?.BeginPass(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other) || !_playerInside)
                return;

            _playerInside = false;

            if (_hardHit)
            {
                NearMissSystem.Instance?.CancelPass(this);
                return;
            }

            NearMissSystem.Instance?.CompletePass(this, baseReward);
            _armed = false;
            _rearmAt = Time.time + rearmSeconds;
        }

        public void NotifyHardHit()
        {
            _hardHit = true;
            NearMissSystem.Instance?.CancelPass(this);
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null;
        }
    }
}
