using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CoastRun
{
    /// Central gameplay juice: coin pop, near-miss hit-stop, SoftHit feedback, land/lane feel.
    /// All timed recoveries use unscaledDeltaTime so pause / timeScale never brick effects.
    public class JuiceDirector : MonoBehaviour
    {
        public static JuiceDirector Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private NearMissSystem nearMiss;
        [SerializeField] private CoinWallet wallet;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private UI_FinalDestinationController destinationUi;
        [SerializeField] private CoastAudioManager audio;
        [SerializeField] private RunnerCameraRig cameraRig;
        [SerializeField] private SpeedLineFx speedLines;

        private Volume _juiceVolume;
        private ColorAdjustments _juiceColor;
        private Coroutine _satRoutine;
        private Coroutine _hitStopRoutine;
        private Coroutine _coinHudRoutine;
        private int _displayedCoins;
        private ParticleSystem _landDust;
        private ParticleSystem _coinBurstPrefab;
        private Text _cheerPopup;
        private CanvasGroup _cheerCg;
        private Coroutine _cheerRoutine;
        private float _baseTimeScale = 1f;

        public void Bind(
            PlayerController p,
            NearMissSystem nm,
            CoinWallet w,
            UI_FeedbackController ui,
            UI_FinalDestinationController dest,
            CoastAudioManager audioMgr,
            RunnerCameraRig rig)
        {
            Instance = this;
            Unbind();

            player = p;
            nearMiss = nm;
            wallet = w;
            feedback = ui;
            destinationUi = dest;
            audio = audioMgr;
            cameraRig = rig;

            if (cameraRig != null)
            {
                speedLines = cameraRig.GetComponent<SpeedLineFx>() ??
                             cameraRig.gameObject.AddComponent<SpeedLineFx>();
                speedLines.EnsureBuilt();
            }

            EnsureJuiceVolume();
            EnsureCheerPopup();
            EnsureLandDust();

            if (feedback != null)
                feedback.SetCoinDriveExternal(true);

            if (wallet != null)
            {
                _displayedCoins = wallet.TotalCoins;
                feedback?.SetDisplayedCoins(_displayedCoins);
                wallet.OnCoinsChanged += HandleCoinsChanged;
            }

            if (nearMiss != null)
                nearMiss.OnNearMissRewarded += HandleNearMiss;

            if (player != null)
            {
                player.OnSoftHit += HandleSoftHit;
                player.OnLanded += HandleLanded;
                player.OnJumped += HandleJumped;
                player.OnLaneChanged += HandleLaneChanged;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Unbind();
        }

        private void Unbind()
        {
            if (wallet != null)
                wallet.OnCoinsChanged -= HandleCoinsChanged;
            if (nearMiss != null)
                nearMiss.OnNearMissRewarded -= HandleNearMiss;
            if (player != null)
            {
                player.OnSoftHit -= HandleSoftHit;
                player.OnLanded -= HandleLanded;
                player.OnJumped -= HandleJumped;
                player.OnLaneChanged -= HandleLaneChanged;
            }
        }

        // ── Coin HUD count-up ──────────────────────────────────────────────

        private void HandleCoinsChanged(int total, int delta)
        {
            if (delta <= 0)
            {
                _displayedCoins = total;
                feedback?.SetDisplayedCoins(_displayedCoins);
                return;
            }

            // Anticipation: brief hold before digits roll.
            if (_coinHudRoutine != null)
                StopCoroutine(_coinHudRoutine);
            _coinHudRoutine = StartCoroutine(CountUpCoins(total, 0.3f, 0.08f));
        }

        private IEnumerator CountUpCoins(int target, float duration, float anticipation)
        {
            if (anticipation > 0f)
            {
                float wait = 0f;
                while (wait < anticipation)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            int from = _displayedCoins;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                u = 1f - (1f - u) * (1f - u); // EaseOutQuad
                _displayedCoins = Mathf.RoundToInt(Mathf.Lerp(from, target, u));
                feedback?.SetDisplayedCoins(_displayedCoins);
                yield return null;
            }

            _displayedCoins = target;
            feedback?.SetDisplayedCoins(_displayedCoins);
            _coinHudRoutine = null;
        }

        /// Bonus Time kick-off: shake + burst + max speed lines for a beat.
        public void PlayBonusStart()
        {
            cameraRig?.Shake(0.35f, 0.18f);
            for (int i = 0; i < 3; i++)
                SpawnCoinBurst((player != null ? player.transform.position : Vector3.zero)
                               + Vector3.up * (0.5f + i * 0.4f));
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        /// 펫(오토바이탄 깡패)이 장애물을 부술 때: 흔들림 + 파편 버스트 + 타격음.
        public void PlaySmash(Vector3 worldPos)
        {
            cameraRig?.Shake(0.25f, 0.14f);
            SpawnCoinBurst(worldPos);
            SpawnCoinBurst(worldPos + Vector3.up * 0.4f);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        /// Called by CoinPickup when collect VFX starts (after wallet Add).
        public void PlayCoinCollect(Transform coinVisual, Vector3 worldPos, int amount)
        {
            if (coinVisual != null)
                StartCoroutine(CoinScalePop(coinVisual));

            // amount 0 = jelly: a trail spawns ten of these a second, so no burst — the
            // scale pop and the SFX carry it. (Bursts also leaked a full-screen tint.)
            if (amount > 0)
                SpawnCoinBurst(worldPos);
            audio?.PlaySfx(CoastSfx.Coin);
        }

        private static IEnumerator CoinScalePop(Transform visual)
        {
            // Anticipation squash then pop 1 → 1.3 → 0 over 0.2s EaseOutBack.
            Vector3 baseScale = visual.localScale;
            float anticip = 0.06f;
            float t = 0f;
            while (t < anticip)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / anticip);
                visual.localScale = baseScale * Mathf.Lerp(1f, 0.92f, u);
                yield return null;
            }

            const float duration = 0.2f;
            t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float s;
                if (u < 0.45f)
                {
                    float p = u / 0.45f;
                    s = Mathf.Lerp(0.92f, 1.3f, EaseOutBack(p));
                }
                else
                {
                    float p = (u - 0.45f) / 0.55f;
                    s = Mathf.Lerp(1.3f, 0f, EaseInCubic(p));
                }

                if (visual == null)
                    yield break;          // owner already destroyed it (jelly shell)
                visual.localScale = baseScale * s;
                yield return null;
            }

            if (visual != null)
                Object.Destroy(visual.gameObject);
        }

        private void SpawnCoinBurst(Vector3 worldPos)
        {
            EnsureCoinBurst();
            var go = Object.Instantiate(_coinBurstPrefab.gameObject, worldPos, Quaternion.identity);
            go.SetActive(true);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        // ── NearMiss ───────────────────────────────────────────────────────

        private void HandleNearMiss(int reward, int combo, Vector3 worldPos)
        {
            StartCoroutine(NearMissSequence(combo));
        }

        private IEnumerator NearMissSequence(int combo)
        {
            // Anticipation micro-hold before hit-stop.
            float wait = 0f;
            while (wait < 0.07f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_hitStopRoutine != null)
                StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStop(0.85f, 0.15f));

            PunchSaturation(+30f, 0.25f);
            speedLines?.Burst(48);
            cameraRig?.FovKick(+4f, 0.2f);
            ShowCheerPopup(combo);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        private IEnumerator HitStop(float scale, float duration)
        {
            _baseTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
            if (_baseTimeScale < 0.01f)
                _baseTimeScale = 1f;

            Time.timeScale = scale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Must restore with unscaled clock — scaled wait would never finish at 0.85 forever if stuck.
            Time.timeScale = 1f;
            _hitStopRoutine = null;
        }

        // ── SoftHit ────────────────────────────────────────────────────────

        private void HandleSoftHit()
        {
            StartCoroutine(SoftHitSequence());
        }

        private IEnumerator SoftHitSequence()
        {
            float wait = 0f;
            while (wait < 0.05f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            cameraRig?.Shake(0.25f, 0.3f);
            PunchSaturation(-40f, 0.4f);
            player?.FreezeInput(0.3f);
            cameraRig?.FovKick(-6f, 0.2f);
            // ★ BGM never stops — SFX only.
            audio?.PlaySfx(CoastSfx.SoftHit);
        }

        // ── Jump / land ────────────────────────────────────────────────────

        private void HandleJumped()
        {
            // Light anticipation FOV for jump takeoff.
            cameraRig?.FovKick(+1.5f, 0.12f);
        }

        private void HandleLanded()
        {
            PlayLandDust();
            cameraRig?.LandDip(0.08f);
            audio?.PlaySfx(CoastSfx.Land);
        }

        private void PlayLandDust()
        {
            if (player == null)
                return;

            EnsureLandDust();
            _landDust.transform.position = player.transform.position + Vector3.up * 0.05f;
            _landDust.Play();
        }

        // ── Lane lean (character leads camera by 0.05s) ────────────────────

        private void HandleLaneChanged(int direction)
        {
            var visual = player != null ? player.GetComponent<CoastPlayerVisual>() : null;
            visual?.PulseLaneLean(direction, 0.05f);
            // Camera roll already anticipates via RunnerCameraRig; character leads.
        }

        // ── Saturation punch via Volume weight ─────────────────────────────

        private void PunchSaturation(float delta, float recoverSeconds)
        {
            EnsureJuiceVolume();
            if (_juiceColor == null)
                return;

            _juiceColor.saturation.Override(delta);
            if (_satRoutine != null)
                StopCoroutine(_satRoutine);
            _satRoutine = StartCoroutine(AnimateVolumeWeight(1f, recoverSeconds));
        }

        private IEnumerator AnimateVolumeWeight(float peak, float recover)
        {
            // Anticipation: snap weight up quickly, then ease back with unscaled time.
            float rise = 0.06f;
            float t = 0f;
            while (t < rise)
            {
                t += Time.unscaledDeltaTime;
                if (_juiceVolume != null)
                    _juiceVolume.weight = Mathf.Lerp(0f, peak, Mathf.Clamp01(t / rise));
                yield return null;
            }

            if (_juiceVolume != null)
                _juiceVolume.weight = peak;

            t = 0f;
            while (t < recover)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / recover);
                u = u * u * (3f - 2f * u);
                if (_juiceVolume != null)
                    _juiceVolume.weight = Mathf.Lerp(peak, 0f, u);
                yield return null;
            }

            if (_juiceVolume != null)
                _juiceVolume.weight = 0f;
            _satRoutine = null;
        }

        private void EnsureJuiceVolume()
        {
            if (_juiceVolume != null)
                return;

            var go = new GameObject("CoastVolume_Juice");
            go.transform.SetParent(transform, false);
            _juiceVolume = go.AddComponent<Volume>();
            _juiceVolume.isGlobal = true;
            _juiceVolume.priority = 20f;
            _juiceVolume.weight = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_Juice";
            _juiceColor = profile.Add<ColorAdjustments>(true);
            _juiceColor.active = true;
            _juiceColor.saturation.Override(0f);
            _juiceVolume.profile = profile;
        }

        // ── Cheer popup ────────────────────────────────────────────────────

        private void ShowCheerPopup(int combo)
        {
            // Story cheer lives on UI_FinalDestinationController (chapter lines / CH5 silent).
            // No extra "나이스" juice toast — it fights the voice design.
        }

        private IEnumerator CheerPulse()
        {
            if (_cheerPopup == null)
                yield break;

            _cheerPopup.gameObject.SetActive(true);
            if (_cheerCg != null)
                _cheerCg.alpha = 0f;

            var rt = _cheerPopup.rectTransform;
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            const float inDur = 0.12f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float u = EaseOutBack(Mathf.Clamp01(t / inDur));
                if (_cheerCg != null)
                    _cheerCg.alpha = Mathf.Clamp01(t / inDur);
                rt.localScale = Vector3.LerpUnclamped(baseScale * 0.7f, baseScale * 1.08f, u);
                yield return null;
            }

            float hold = 0f;
            while (hold < 0.9f)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            t = 0f;
            const float outDur = 0.2f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / outDur);
                if (_cheerCg != null)
                    _cheerCg.alpha = 1f - u;
                rt.localScale = Vector3.Lerp(baseScale * 1.08f, baseScale * 0.9f, u);
                yield return null;
            }

            _cheerPopup.gameObject.SetActive(false);
            _cheerRoutine = null;
        }

        private void EnsureCheerPopup()
        {
            if (_cheerPopup != null)
                return;

            var canvas = CoastUiCanvas.Create("JuiceHUD", 110);
            var go = new GameObject("CheerJuice", typeof(RectTransform));
            go.transform.SetParent(CoastUiCanvas.Root(canvas), false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.62f);
            rt.anchorMax = new Vector2(0.5f, 0.62f);
            rt.sizeDelta = new Vector2(420f, 64f);
            rt.anchoredPosition = Vector2.zero;

            _cheerCg = go.AddComponent<CanvasGroup>();
            _cheerCg.blocksRaycasts = false;
            _cheerPopup = go.AddComponent<Text>();
            _cheerPopup.font = CoastHudLayout.Font();
            _cheerPopup.fontSize = 34;
            _cheerPopup.fontStyle = FontStyle.Bold;
            _cheerPopup.alignment = TextAnchor.MiddleCenter;
            _cheerPopup.color = Color.white;
            _cheerPopup.raycastTarget = false;
            go.SetActive(false);
        }

        // ── Particles ──────────────────────────────────────────────────────

        private void EnsureLandDust()
        {
            if (_landDust != null)
                return;

            var go = new GameObject("LandDust");
            go.transform.SetParent(transform, false);
            _landDust = go.AddComponent<ParticleSystem>();
            var main = _landDust.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new Color(0.85f, 0.8f, 0.7f, 0.65f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            main.useUnscaledTime = true;

            var emission = _landDust.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = _landDust.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.35f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CoastMaterials.CreateParticle(new Color(0.9f, 0.85f, 0.75f, 0.7f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _landDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EnsureCoinBurst()
        {
            if (_coinBurstPrefab != null)
                return;

            var go = new GameObject("CoinBurstPrefab");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            _coinBurstPrefab = go.AddComponent<ParticleSystem>();
            var main = _coinBurstPrefab.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = CoastPalette.CoinYellow;
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;
            main.useUnscaledTime = true;

            var emission = _coinBurstPrefab.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = _coinBurstPrefab.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            // Particles get the stock URP particle shader, not the curved-world unlit:
            // ParticleSystemRenderer hands the curved shader vertices it does not expect
            // and the burst smeared as screen-sized yellow blobs (even into the letterbox).
            renderer.material = CoastMaterials.CreateParticle(CoastPalette.CoinYellow);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInCubic(float t) => t * t * t;
    }
}
