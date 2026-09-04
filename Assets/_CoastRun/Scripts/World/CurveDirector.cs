using UnityEngine;

namespace CoastRun
{
    /// Drives the curved-world bend that makes the promenade sweep left and right.
    ///
    /// The run stays a straight line internally — lanes, hit tests and row planning
    /// never change (see DownhillPath). The bend is purely visual: every Coast Run
    /// shader offsets vertices ahead of the player by curvature × distance², and this
    /// component feeds that curvature from a smooth profile over path distance. Because
    /// the profile is a function of distance rather than time, a retry replays the same
    /// bends, and the road ahead reads as a real course instead of random sway.
    ///
    /// Profile: a slow S-curve layered with a longer swell so the straights vary in
    /// length; the first stage eases in from zero so a new player sees a straight road
    /// for the first hundred metres. Vertical curvature adds gentle rises and dips.
    public class CurveDirector : MonoBehaviour
    {
        public static CurveDirector Instance { get; private set; }

        [SerializeField] private PlayerController player;

        [Header("Lateral bends")]
        [Tooltip("Peak lateral curvature (m per m²). 0.0022 bends 100 m ahead by 22 m.")]
        [SerializeField] private float maxCurvature = 0.0022f;
        [SerializeField] private float bendLength = 260f;
        [SerializeField] private float swellLength = 730f;
        [Tooltip("Distance over which bends ease in at the very start of a run.")]
        [SerializeField] private float easeInDistance = 120f;

        [Header("Vertical rolls")]
        [SerializeField] private float maxVertical = 0.0006f;
        [SerializeField] private float rollLength = 190f;

        [Header("Shader")]
        [Tooltip("Bend stops growing beyond this distance so far geometry never flies off.")]
        [SerializeField] private float clampDistance = 220f;
        [Tooltip("How quickly the shader follows the profile; hides row-to-row jitter.")]
        [SerializeField] private float smoothing = 0.35f;

        private static readonly int CurveId = Shader.PropertyToID("_CoastCurve");

        private float _lateral;
        private float _vertical;
        private float _lateralVel;
        private float _verticalVel;
        private bool _enabled = true;

        /// Current lateral curvature (m per m²), + bends right. Camera leans into it.
        public float Curvature => _lateral;
        public float VerticalCurvature => _vertical;

        public void Bind(PlayerController playerController)
        {
            player = playerController;
        }

        /// Cutscenes and the ending want a straight world; the bend eases out, not snaps.
        public void SetEnabled(bool on) => _enabled = on;

        private void Awake()
        {
            Instance = this;
            Push(0f, 0f, 0f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Shader.SetGlobalVector(CurveId, Vector4.zero);
        }

        private void LateUpdate()
        {
            if (player == null)
                return;

            float s = player.PathDistance;
            // A stopped player is in a cutscene, a clear screen or a game-over; ease the
            // world straight so cinematic cameras placed on straight coordinates line up.
            bool active = _enabled && player.Speed > 0.5f;
            float wantLat = active ? LateralProfile(s) : 0f;
            float wantVert = active ? VerticalProfile(s) : 0f;

            float dt = Time.deltaTime;
            _lateral = Mathf.SmoothDamp(_lateral, wantLat, ref _lateralVel, smoothing, Mathf.Infinity, dt);
            _vertical = Mathf.SmoothDamp(_vertical, wantVert, ref _verticalVel, smoothing, Mathf.Infinity, dt);

            Push(_lateral, _vertical, s);
        }

        private void Push(float lateral, float vertical, float originZ)
        {
            Shader.SetGlobalVector(CurveId, new Vector4(lateral, vertical, originZ, clampDistance));
        }

        public float LateralProfile(float s)
        {
            float bend = Mathf.Sin(s * (Mathf.PI * 2f) / bendLength);
            float swell = 0.55f + 0.45f * Mathf.Sin(s * (Mathf.PI * 2f) / swellLength + 1.3f);
            float easeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(s / Mathf.Max(1f, easeInDistance)));
            return maxCurvature * bend * swell * easeIn;
        }

        public float VerticalProfile(float s)
        {
            float easeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(s / Mathf.Max(1f, easeInDistance)));
            return maxVertical * Mathf.Sin(s * (Mathf.PI * 2f) / rollLength + 0.7f) * easeIn;
        }
    }
}
