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
        private ParticleSystem _runDust;     // 12차: 달리는 내내 발밑 먼지
        private TrailRenderer _boardTrail;   // 12차: 보드 뒤 트레일
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
            EnsureRunDust();
            EnsureBoardTrail();

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
            if (_boardTrail != null) { _boardTrail.emitting = false; _boardTrail.Clear(); }
            if (_runDust != null) { var em = _runDust.emission; em.rateOverTime = 0f; }
        }

        private void LateUpdate()
        {
            UpdateSpeedFx();
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
        /// 14차-3: 점프 패드 — 짧은 흔들림 + 스피드라인 + 코인 버스트 하나.
        /// 14차-6: 발 디딤 먼지 한 번(RunDust 파티클 4~6개를 그 자리에서 터뜨린다).
        public void PuffStep(Vector3 footPos)
        {
            EnsureRunDust();
            if (_runDust == null) return;
            var ep = new ParticleSystem.EmitParams { position = footPos + Vector3.up * 0.03f };
            _runDust.Emit(ep, 9);   // 14차-8: 발 디딤 먼지 더 또렷하게
        }

        /// 17차: 빨래줄 잡기 — 반짝 + 스피드라인 + 진동 + 코인 버스트
        /// 19차-1: 세상이 흑백으로 바뀌는 순간 — 긴 링 플래시 + 낮은 카메라 흔들림. 소리는 BGM이 그대로, SFX만.
        public void OnWorldFade()
        {
            cameraRig?.Shake(0.08f, 0.5f);
            cameraRig?.FovKick(-3f, 1.2f);
            audio?.PlaySfx(CoastSfx.SoftHit);
        }

        public void OnLineGrab(Vector3 worldPos)
        {
            cameraRig?.Shake(0.15f, 0.1f);
            cameraRig?.FovKick(+6f, 0.35f);
            speedLines?.Burst(60);
            SpawnCoinBurst(worldPos, Color.white, 10);
            StartCoroutine(FlashRing(worldPos, new Color(1f, 0.95f, 0.7f, 0.9f), 2.2f));
            PunchSaturation(+25f, 0.3f);
            CoastPrefs.Vibrate();
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        public void OnJumpPad(Vector3 worldPos)
        {
            cameraRig?.Shake(0.18f, 0.12f);
            speedLines?.Burst(36);
            SpawnCoinBurst(worldPos);
            CoastPrefs.Vibrate();
        }

        /// 14차-14: 장애물 팡 — 파스텔 별·하트 흩뿌리기 + 흰 링 + 작은 흔들림. 가볍고 귀엽게(실패 연출이 아니라 장난감처럼).
        public void PlayObstaclePop(Vector3 worldPos)
        {
            // 17차: 타격감 — 순간 정지(0.07 s) + 큰 흔들림 + 진동. 파편은 주인공 좌표계에서 흩어져 뒤에 남지 않는다.
            if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStop(0.04f, 0.07f));
            cameraRig?.Shake(0.32f, 0.16f);
            cameraRig?.FovKick(-5f, 0.15f);
            CoastPrefs.Vibrate();
            EnsurePopBursts();
            SpawnPop(_popStar, worldPos, new Color(1f, 0.93f, 0.45f), 9);
            SpawnPop(_popHeart, worldPos + Vector3.up * 0.15f, new Color(1f, 0.55f, 0.68f), 7);
            SpawnPop(_popPuff, worldPos, new Color(1f, 1f, 1f, 0.95f), 8);
            SpawnCoinBurst(worldPos, Color.white, 6);
            StartCoroutine(FlashRing(worldPos, new Color(1f, 0.8f, 0.85f, 0.9f), 1.7f));
            speedLines?.Burst(6);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        // 14차-15: 팡 파편 — 큰 별·하트(회전하며 튀어 오름) + 흰 뭉게 퍼프(만화 '펑' 구름)
        private ParticleSystem _popStar, _popHeart, _popPuff;
        private void EnsurePopBursts()
        {
            if (_popStar != null) return;
            _popStar = MakePop("PopStar", StarTexture(), 0.30f, 0.62f, 2.5f, 6f, 0.9f, true);
            _popHeart = MakePop("PopHeart", HeartTexture(), 0.26f, 0.5f, 2f, 5f, 0.7f, true);
            _popPuff = MakePop("PopPuff", PuffTexture(), 0.55f, 1.0f, 0.8f, 2.2f, -0.05f, false);
        }

        private ParticleSystem MakePop(string name, Texture2D tex, float sizeMin, float sizeMax, float spdMin, float spdMax, float gravity, bool spin)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spdMin, spdMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = spin ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.gravityModifier = gravity;
            main.simulationSpace = player != null ? ParticleSystemSimulationSpace.Custom : ParticleSystemSimulationSpace.World;
            if (player != null) main.customSimulationSpace = player.transform;
            main.maxParticles = 32;
            main.useUnscaledTime = true;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.2f;
            if (spin)
            {
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, spin
                ? new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.12f, 1.15f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f))
                : new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1.3f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = CoastMaterials.CreateParticle(Color.white);
            if (r.material.HasProperty("_BaseMap")) r.material.SetTexture("_BaseMap", tex);
            if (r.material.HasProperty("_ZTest")) r.material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            r.material.renderQueue = 3500;
            return ps;
        }

        private void SpawnPop(ParticleSystem prefab, Vector3 pos, Color tint, int count)
        {
            var go = Object.Instantiate(prefab.gameObject, pos, Quaternion.identity);
            go.SetActive(true);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main; main.startColor = tint;
            ps.Emit(count);
            Destroy(go, 1.2f);
        }

        private static Texture2D _starTex, _heartTex, _puffTex;
        private static Texture2D StarTexture()
        {
            if (_starTex != null) return _starTex;
            const int N = 64; _starTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float px = (x + 0.5f) / N - 0.5f, py = (y + 0.5f) / N - 0.5f;
                float a = Mathf.Atan2(py, px), r = Mathf.Sqrt(px * px + py * py) * 2f;
                float star = 0.55f + 0.45f * Mathf.Cos(5f * a + Mathf.PI * 0.5f);   // 5각 별 반지름
                float edge = star * 0.98f;
                Color c = r < edge * 0.78f ? Color.white : r < edge ? new Color(0.12f, 0.08f, 0.12f, 1f) : new Color(0, 0, 0, 0);
                _starTex.SetPixel(x, y, c);
            }
            _starTex.Apply(); return _starTex;
        }
        private static Texture2D HeartTexture()
        {
            if (_heartTex != null) return _heartTex;
            const int N = 64; _heartTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float px = ((x + 0.5f) / N - 0.5f) * 2.6f, py = ((y + 0.5f) / N - 0.45f) * 2.6f;
                // 하트 음함수: (x²+y²−1)³ − x²y³ < 0
                float f = Mathf.Pow(px * px + py * py - 1f, 3f) - px * px * py * py * py;
                float f2 = Mathf.Pow(px * px * 1.3f + py * py * 1.3f - 1f, 3f) - px * px * 1.3f * py * py * py * 1.3f * 1.14f;
                Color c = f2 < 0f ? Color.white : f < 0f ? new Color(0.12f, 0.08f, 0.12f, 1f) : new Color(0, 0, 0, 0);
                _heartTex.SetPixel(x, y, c);
            }
            _heartTex.Apply(); return _heartTex;
        }
        private static Texture2D PuffTexture()
        {
            if (_puffTex != null) return _puffTex;
            const int N = 64; _puffTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float px = (x + 0.5f) / N - 0.5f, py = (y + 0.5f) / N - 0.5f;
                float r = Mathf.Sqrt(px * px + py * py) * 2f;
                float a = Mathf.Atan2(py, px);
                float lobes = 0.86f + 0.10f * Mathf.Cos(6f * a);   // 뭉게구름 가장자리
                Color c = r < lobes * 0.9f ? Color.white : r < lobes ? new Color(0.12f, 0.08f, 0.12f, 1f) : new Color(0, 0, 0, 0);
                _puffTex.SetPixel(x, y, c);
            }
            _puffTex.Apply(); return _puffTex;
        }

        public void PlaySmash(Vector3 worldPos)
        {
            cameraRig?.Shake(0.25f, 0.14f);
            SpawnCoinBurst(worldPos);
            SpawnCoinBurst(worldPos + Vector3.up * 0.4f);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }

        /// Called by CoinPickup when collect VFX starts (after wallet Add).
        public void PlayCoinCollect(Transform coinVisual, Vector3 worldPos, int amount)
            => PlayCoinCollect(coinVisual, worldPos, amount, CoastPalette.CoinYellow);

        /// 14차-5: '팍' 터지는 수집 연출 — 스케일 팝 + 반짝이 별 버스트 + 퍼지는 링 플래시.
        /// tint 는 아이템 색(코인 금색, 하트 분홍, 별 노랑, 포션 하늘색).
        public void PlayCoinCollect(Transform coinVisual, Vector3 worldPos, int amount, Color tint)
        {
            if (coinVisual != null)
                StartCoroutine(CoinScalePop(coinVisual));

            // amount 0 = jelly: a trail spawns ten of these a second, so a light touch —
            // small ring only. Everything else gets the full pop.
            // 14차-6: 먹는 순간 아이템은 이미 몸에 겹쳐 있어 터짐이 몸 뒤로 지나갔다 —
            // 카메라 쪽으로 0.9 m, 위로 0.45 m 당겨서 주인공 앞에서 터지게 한다.
            // 14차-7: 호출자가 PickupReach.PopPos 로 이미 몸 앞 위치를 준다(coinVisual == null).
            Vector3 popPos = worldPos;
            if (coinVisual != null)
            {
                var cam = Camera.main != null ? Camera.main.transform : null;
                popPos += Vector3.up * 0.45f;
                if (cam != null)
                {
                    Vector3 toCam = cam.position - worldPos; toCam.y = 0f;
                    popPos += toCam.normalized * 0.9f;
                }
            }
            if (amount > 0)
            {
                SpawnCoinBurst(popPos, tint, amount >= 2 ? 26 : 18);
                StartCoroutine(FlashRing(popPos, tint, amount >= 2 ? 2.1f : 1.6f));
            }
            else
                StartCoroutine(FlashRing(popPos, tint, 1.1f));
            // 22차-3: 타격감 — 연속 획득마다 피치가 올라가는 효과음(서브웨이 서퍼/골드런 방식), '+N' 플로팅, 코인은 HUD로 날아감,
            // 주인공은 살짝 찌그러졌다 펴진다.
            float now = Time.unscaledTime;
            _pickupStreak = now - _lastPickupTime < 0.55f ? Mathf.Min(_pickupStreak + 1, 12) : 0;
            _lastPickupTime = now;
            if (audio != null) audio.SfxPitchBoost = _pickupStreak * 0.045f;
            audio?.PlaySfx(CoastSfx.Coin);
            if (audio != null) audio.SfxPitchBoost = 0f;
            if (amount > 0)
            {
                PickupFloat.Text(popPos, "+" + amount, tint, amount >= 2 ? 1.25f : 1f);
                if (tint == CoastPalette.CoinYellow) PickupFloat.FlyCoin(popPos, amount >= 2 ? 3 : 1);
                // 23차-4: 펫 코인 보너스가 눈에 보이게 — 배율이 있으면 1.1초에 한 번 "120%"가 함께 떠오른다(펫 색).
                if (tint == CoastPalette.CoinYellow && PetCompanion.CoinBonus > 1.001f && now - _lastPetTagTime > 1.1f)
                {
                    _lastPetTagTime = now;
                    PickupFloat.Text(popPos + Vector3.up * 0.55f, Mathf.RoundToInt(PetCompanion.CoinBonus * 100f) + "%", new Color(0.55f, 0.95f, 1f), 0.8f);
                }
            }
            else
                PickupFloat.Text(popPos, "+1", tint, 0.85f);
            if (_bodySquash == null) _bodySquash = StartCoroutine(BodySquash());
        }

        private int _pickupStreak; private float _lastPickupTime = -10f; private float _lastPetTagTime = -10f; private Coroutine _bodySquash;

        /// 23차-3: 골인 콘페티 — 리본이 끊기는 순간 게이트 위에서 색종이가 쏟아진다(별·하트 파티클 재사용).
        public void OnFinishConfetti(Vector3 top, float halfWidth)
        {
            EnsurePopBursts();
            Color[] cols = { new Color(1f, 0.35f, 0.45f), new Color(1f, 0.85f, 0.3f), new Color(0.45f, 0.75f, 1f), new Color(0.6f, 0.9f, 0.5f), Color.white };
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(-halfWidth, halfWidth, (i + 0.5f) / 7f);
                var p = top + Vector3.right * x;
                SpawnPop(i % 2 == 0 ? _popStar : _popHeart, p, cols[i % cols.Length], 8);
            }
            cameraRig?.Shake(0.12f, 0.2f);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }
        private IEnumerator BodySquash()
        {
            var t = player != null ? player.transform.Find("SkaterRig") : null;
            if (t == null) t = player != null && player.transform.childCount > 0 ? player.transform.GetChild(0) : null;
            if (t == null) { _bodySquash = null; yield break; }
            Vector3 s0 = t.localScale; float time = 0f; const float dur = 0.16f;
            while (time < dur)
            {
                time += Time.deltaTime; float u = time / dur;
                float k = Mathf.Sin(u * Mathf.PI);
                t.localScale = new Vector3(s0.x * (1f + 0.08f * k), s0.y * (1f - 0.07f * k), s0.z * (1f + 0.08f * k));
                yield return null;
            }
            t.localScale = s0; _bodySquash = null;
        }

        private Material _ringMat;
        private static readonly int RingColorId = Shader.PropertyToID("_BaseColor");

        /// 얇은 가산 링이 0.28초 동안 커지며 사라진다(카메라를 보는 쿼드).
        private IEnumerator FlashRing(Vector3 pos, Color tint, float size)
        {
            if (_ringMat == null)
            {
                _ringMat = CoastMaterials.CreateTexturedTransparentCurved(RingTexture(), Color.white, additive: true);
                if (_ringMat.HasProperty("_ZTest")) _ringMat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                _ringMat.renderQueue = 3500;   // 주인공보다 나중에, 깊이 무시 — 항상 앞에서 보인다
            }
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "CollectRing";
            CoastEditUtil.DestroyCollider(q);
            var mr = q.GetComponent<Renderer>();
            mr.sharedMaterial = _ringMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            var cam = Camera.main != null ? Camera.main.transform : null;
            float t = 0f; const float dur = 0.28f;
            while (t < dur && q != null)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.25f, size, 1f - (1f - u) * (1f - u));
                q.transform.position = pos;
                if (cam != null) q.transform.rotation = Quaternion.LookRotation(q.transform.position - cam.position);
                q.transform.localScale = new Vector3(s, s, 1f);
                var c = tint; c.a = (1f - u) * 0.9f;
                mpb.SetColor(RingColorId, c);
                mr.SetPropertyBlock(mpb);
                yield return null;
            }
            if (q != null) Object.Destroy(q);
        }

        private static Texture2D _ringTex;
        private static Texture2D RingTexture()
        {
            if (_ringTex != null) return _ringTex;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;           // 0 centre, 1 edge
                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.82f) / 0.14f);
                    px[y * n + x] = new Color(1f, 1f, 1f, ring * ring);
                }
            tex.SetPixels(px); tex.Apply();
            _ringTex = tex;
            return tex;
        }

        private static Texture2D _sparkleTex;
        /// 4각 별 반짝이(파티클용).
        private static Texture2D SparkleTexture()
        {
            if (_sparkleTex != null) return _sparkleTex;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / n - 0.5f) * 2f, dy = Mathf.Abs((y + 0.5f) / n - 0.5f) * 2f;
                    float star = Mathf.Clamp01(1f - (dx + dy) * 1.15f) + Mathf.Clamp01(1f - (dx * dx + dy * dy) * 6f) * 0.8f;
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(star));
                }
            tex.SetPixels(px); tex.Apply();
            _sparkleTex = tex;
            return tex;
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

        private void SpawnCoinBurst(Vector3 worldPos) => SpawnCoinBurst(worldPos, CoastPalette.CoinYellow, 14);

        private void SpawnCoinBurst(Vector3 worldPos, Color tint, int count)
        {
            EnsureCoinBurst();
            var go = Object.Instantiate(_coinBurstPrefab.gameObject, worldPos, Quaternion.identity);
            go.SetActive(true);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(tint * 1.6f, Color.Lerp(tint, Color.white, 0.7f) * 1.4f);
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        // ── NearMiss ───────────────────────────────────────────────────────

        private void HandleNearMiss(int reward, int combo, Vector3 worldPos)
        {
            CoastPrefs.Vibrate();   // 14차 게임필: 아슬아슬 통과에 짧은 진동
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
            CoastPrefs.Vibrate();
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

            // 23차-2: '꽈당' — 공격당했다는 불쾌한 충격이 화면에 와야 피하고 싶어진다.
            // 순간 정지(0.10 s) → 큰 흔들림 → 화면이 기울며 붉게 번쩍 + "꽈당!" + 채도 뚝.
            if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStop(0.03f, 0.10f));
            cameraRig?.Shake(0.55f, 0.38f);
            PunchSaturation(-70f, 0.55f);
            player?.FreezeInput(0.3f);
            cameraRig?.FovKick(-9f, 0.28f);
            PickupFloat.Impact(Loc.T("꽈당!", "OUCH!"));
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
            _cheerPopup.fontSize = CoastHudLayout.Scaled(34);
            _cheerPopup.fontStyle = FontStyle.Bold;
            _cheerPopup.alignment = TextAnchor.MiddleCenter;
            _cheerPopup.color = Color.white;
            _cheerPopup.raycastTarget = false;
            go.SetActive(false);
        }

        // ── Particles ──────────────────────────────────────────────────────

        // ── 12차: 속도감 — 상시 먼지·트레일 ─────────────────────────────
        private void EnsureRunDust()
        {
            if (_runDust != null || player == null)
                return;
            var go = new GameObject("RunDust");
            go.transform.SetParent(transform, false);
            _runDust = go.AddComponent<ParticleSystem>();
            var main = _runDust.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.36f);
            main.startColor = new Color(0.9f, 0.86f, 0.78f, 0.55f);
            main.gravityModifier = -0.05f;   // 살짝 떠오르며 흩어진다
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var emission = _runDust.emission;
            emission.rateOverTime = 0f;
            var shape = _runDust.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.14f;
            shape.rotation = new Vector3(-80f, 180f, 0f);   // 뒤쪽 아래로
            var col = _runDust.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var sz = _runDust.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = CoastMaterials.CreateParticle(new Color(0.9f, 0.86f, 0.78f, 0.6f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _runDust.Play();
        }

        private void EnsureBoardTrail()
        {
            if (_boardTrail != null || player == null)
                return;
            var go = new GameObject("BoardTrail");
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.06f, -0.35f);
            _boardTrail = go.AddComponent<TrailRenderer>();
            _boardTrail.time = 0.28f;
            _boardTrail.minVertexDistance = 0.08f;
            _boardTrail.startWidth = 0.22f;
            _boardTrail.endWidth = 0.0f;
            _boardTrail.alignment = LineAlignment.View;
            _boardTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _boardTrail.receiveShadows = false;
            _boardTrail.material = CoastMaterials.CreateParticle(new Color(1f, 1f, 1f, 0.35f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f), new GradientColorKey(new Color(0.8f, 0.95f, 1f), 1f) },
                      new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            _boardTrail.colorGradient = g;
            _boardTrail.emitting = false;
        }

        private void UpdateSpeedFx()
        {
            if (player == null)
                return;
            float n = player.NormalizedSpeed;
            bool ground = player.IsGrounded && player.Speed > 2f;
            if (_runDust != null)
            {
                var em = _runDust.emission;
                em.rateOverTime = ground ? Mathf.Lerp(6f, 34f, n) : 0f;
                _runDust.transform.position = player.transform.position + Vector3.up * 0.04f;
                _runDust.transform.rotation = player.transform.rotation;
            }
            if (_boardTrail != null)
            {
                // 트레일은 빠를 때만, 또 점프 중엔 끊는다 — 땅에 붙어 미끄러지는 느낌이 목적.
                _boardTrail.emitting = ground && n > 0.35f;
                _boardTrail.time = Mathf.Lerp(0.18f, 0.42f, n);
            }
        }

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
            // 14차-5: 더 크고 빠르게, 별 모양으로 — '팍' 터지는 느낌.
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.34f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = CoastPalette.CoinYellow * 1.5f;
            main.gravityModifier = 1.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
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
            if (renderer.material.HasProperty("_BaseMap")) renderer.material.SetTexture("_BaseMap", SparkleTexture());
            if (renderer.material.HasProperty("_ZTest")) renderer.material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            renderer.material.renderQueue = 3500;
            var sol = _coinBurstPrefab.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f)));
            var col = _coinBurstPrefab.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
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
