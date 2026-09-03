using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// Single ending sequence for 04_Ending. No branches. No explanations.
    /// Ambiguity is intentional — never resolve what happened to the other person.
    public class EndingController : MonoBehaviour
    {
        private const float Act1Seconds = 50f;
        private const float GroundCloseupSeconds = 2f;
        private const float Act2Seconds = 50f;
        private const float Act3LetterSeconds = 80f;
        private const float PhoneHoldSeconds = 3f;
        private const float DescentSeconds = 60f;
        private const float FootstepTailSeconds = 3f;

        private Camera _cam;
        private Transform _camRig;
        private Transform _girl;
        private Transform _board;
        private Transform _tower;
        private Light _beacon;
        private Light[] _villageLights;
        private AudioSource _bgm;
        private AudioSource _ambience;
        private AudioSource _pulse;
        private AudioSource _piano;
        private AudioSource _vo;
        private AudioSource _footsteps;
        private AudioSource _radio;
        private Canvas _ui;
        private CanvasGroup _veil;
        private Image _letterPaper;
        private Text _letterText;
        private RectTransform _phonePanel;
        private Text _phoneNumber;
        private Text _phoneNameClipped;
        private Image _handOverlay;
        private Text _titleCard;
        private Text _creditsText;
        private Text _stingerSub;
        private GameObject _groundCloseup;
        private GameObject _canProp;
        private GameObject _truckProp;
        private GameObject _paperProp;
        private bool _descentActive;
        private float _descentT;
        private Vector3 _camFixedPos;
        private Quaternion _camFixedRot;
        private float _letterBgmVolume = 0.38f;
        private bool _letterChordFrozen;

        private void Start()
        {
            GameDirector.EnsureExists();
            // Kill any run HUD that might have leaked via DDOL.
            HideAllRunUi();
            BuildWorld();
            BuildUi();
            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            // Beacon blink — also drives the low pulse rhythm in act 1.
            if (_beacon != null)
            {
                float blink = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * 1.15f) > 0f) ? 1f : 0.06f;
                _beacon.intensity = 5.5f * blink;
                if (_pulse != null && _pulse.isPlaying)
                    _pulse.volume = 0.04f + blink * 0.07f;
            }

            if (_descentActive)
                TickDescent();
        }

        private IEnumerator PlaySequence()
        {
            // ── 4-1 Arrival ───────────────────────────────────────────────
            SetAct1Framing();
            PlayBgm("BGM_End_Arrival", 90f, 0.05f, 0.18f);
            PlayAmbienceGrass();
            PlayPulse();
            // Girl steps off board, stands. Camera FIXED.
            yield return StepOffBoard(1.6f);
            yield return Hold(Act1Seconds - 1.6f);

            // ── 4-2 Dig ───────────────────────────────────────────────────
            StopPulse();
            CrossfadeBgm(null, 0.8f); // almost silent under dig
            yield return GroundCloseup(GroundCloseupSeconds);
            yield return DigAndFindCan(Act2Seconds - GroundCloseupSeconds - 8f);
            yield return RevealCanContents();

            // ── 4-3 Letter ────────────────────────────────────────────────
            PlayBgm("BGM_End_Letter", 160f, 0.03f, _letterBgmVolume);
            yield return ReadLetter(Act3LetterSeconds);
            // Phone 3s — camera still, harmony frozen.
            _letterChordFrozen = true;
            yield return PhoneContactHold(PhoneHoldSeconds);
            _letterChordFrozen = false;

            // ── 4-4 Descent (playable) ────────────────────────────────────
            HideLetterUi();
            PlayBgm("BGM_End_Descent", 140f, 0.04f, 0.4f, loop: true);
            yield return PlayableDescent(DescentSeconds);
            yield return FadeOutWithFootstepTail();

            // Title card
            yield return ShowTitleCard(3.5f);

            // ── 4-5 Credits → silence → black stinger ─────────────────────
            yield return RollCredits(12f);
            StopAllBeds();
            yield return Hold(2f); // silent 2s
            yield return BlackStinger();

            // Cleared + title
            var dir = GameDirector.Instance;
            if (dir != null)
            {
                dir.CampaignCleared = true;
                dir.Progression?.MarkCampaignCleared();
                dir.Flow?.CompleteEndingReturnToTitle();
            }
            else
            {
                PlayerPrefs.SetInt(ProgressionManager.ClearedKey, 1);
                PlayerPrefs.Save();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4-1
        // ═══════════════════════════════════════════════════════════════════

        private void SetAct1Framing()
        {
            // Fixed camera — looking at tower base / girl. Does not move during 4-1.
            Vector3 look = _tower != null ? _tower.position + Vector3.up * 3f : Vector3.zero;
            _camFixedPos = look + new Vector3(-2.2f, 2.4f, -7.5f);
            _camFixedRot = Quaternion.LookRotation((look + Vector3.up * 0.6f - _camFixedPos).normalized, Vector3.up);
            _camRig.SetPositionAndRotation(_camFixedPos, _camFixedRot);
            if (_cam != null)
                _cam.fieldOfView = 42f;
        }

        private IEnumerator StepOffBoard(float duration)
        {
            if (_girl == null || _board == null)
            {
                yield return Hold(duration);
                yield break;
            }

            Vector3 girlStart = _girl.position;
            Vector3 girlEnd = girlStart + _girl.forward * 0.8f;
            Vector3 boardStart = _board.position;
            Vector3 boardEnd = boardStart + _girl.right * 0.55f;
            Quaternion boardRotEnd = _board.rotation * Quaternion.Euler(0f, 0f, 12f);

            float t = 0f;
            while (t < duration)
            {
                // Camera must not move.
                _camRig.SetPositionAndRotation(_camFixedPos, _camFixedRot);
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                _girl.position = Vector3.Lerp(girlStart, girlEnd, u);
                _board.position = Vector3.Lerp(boardStart, boardEnd, u);
                _board.rotation = Quaternion.Slerp(_board.rotation, boardRotEnd, u * 0.5f);
                yield return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4-2
        // ═══════════════════════════════════════════════════════════════════

        private IEnumerator GroundCloseup(float seconds)
        {
            // Ambiguous ground — roots + rain marks. Not packed, not freshly dug.
            if (_groundCloseup != null)
                _groundCloseup.SetActive(true);

            Vector3 anchor = _girl != null ? _girl.position : Vector3.zero;
            Vector3 fwd = _girl != null ? _girl.forward : Vector3.forward;
            Vector3 pos = anchor + Vector3.up * 0.35f + fwd * 0.4f;
            _camRig.SetPositionAndRotation(pos + new Vector3(0.1f, 0.55f, -0.35f),
                Quaternion.Euler(78f, 12f, 0f));
            if (_cam != null)
                _cam.fieldOfView = 35f;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                // Fixed.
                yield return null;
            }

            if (_groundCloseup != null)
                _groundCloseup.SetActive(false);
        }

        private IEnumerator DigAndFindCan(float digSeconds)
        {
            // Kneel framing — hands area, no face.
            Vector3 basePos = _girl != null ? _girl.position : Vector3.zero;
            _camRig.SetPositionAndRotation(basePos + new Vector3(0.6f, 0.7f, -1.1f),
                Quaternion.Euler(35f, -18f, 0f));
            if (_cam != null)
                _cam.fieldOfView = 40f;

            if (_girl != null)
                _girl.localScale = new Vector3(0.95f, 0.72f, 0.95f); // kneel silhouette

            float t = 0f;
            while (t < digSeconds)
            {
                t += Time.unscaledDeltaTime;
                // Subtle hand dig motion via can prop rising late.
                if (_canProp != null && t > digSeconds * 0.55f)
                {
                    float u = Mathf.Clamp01((t - digSeconds * 0.55f) / (digSeconds * 0.4f));
                    _canProp.SetActive(true);
                    _canProp.transform.localPosition = Vector3.Lerp(
                        new Vector3(0f, -0.15f, 0.35f), new Vector3(0f, 0.05f, 0.4f), u);
                }

                yield return null;
            }
        }

        private IEnumerator RevealCanContents()
        {
            // First piano note when the can appears fully.
            PlayPianoNote();
            if (_canProp != null)
                _canProp.SetActive(true);
            if (_truckProp != null)
                _truckProp.SetActive(true);
            if (_paperProp != null)
                _paperProp.SetActive(true);

            // Close on tin contents — truck + folded paper. No face.
            Vector3 p = _canProp != null ? _canProp.transform.position : Vector3.zero;
            _camRig.SetPositionAndRotation(p + new Vector3(0.15f, 0.35f, -0.45f),
                Quaternion.LookRotation((p - (_camRig.position)).normalized, Vector3.up));

            yield return Hold(6f);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4-3 Letter + phone
        // ═══════════════════════════════════════════════════════════════════

        private IEnumerator ReadLetter(float seconds)
        {
            // Hands + paper only.
            ShowLetterUi(true);
            if (_handOverlay != null)
                _handOverlay.gameObject.SetActive(true);

            string[] lines = EndingLetter.Lines;
            float per = seconds / Mathf.Max(1, lines.Length);
            var spoken = ProceduralAudio.CreateLoop(180f, 0.02f, 2f); // soft VO bed, not speech content

            if (_vo != null)
            {
                _vo.clip = spoken;
                _vo.volume = 0.12f;
                _vo.loop = true;
                _vo.Play();
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (_letterText != null)
                    _letterText.text = BuildLetterSoFar(lines, i);
                // Keep BGM harmony steady — no stem changes during letter.
                if (_bgm != null && !_letterChordFrozen)
                    _bgm.pitch = 1f;
                yield return Hold(per);
            }

            if (_vo != null)
                _vo.Stop();
        }

        private static string BuildLetterSoFar(string[] lines, int upToInclusive)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i <= upToInclusive && i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append("\n\n");
                sb.Append(lines[i]);
            }

            return sb.ToString();
        }

        private IEnumerator PhoneContactHold(float seconds)
        {
            // Same framing language — hand + phone. No face. No cut.
            if (_letterPaper != null)
                _letterPaper.gameObject.SetActive(false);
            if (_letterText != null)
                _letterText.gameObject.SetActive(false);
            if (_phonePanel != null)
                _phonePanel.gameObject.SetActive(true);

            // Number visible; name clipped off-screen (above panel).
            if (_phoneNumber != null)
                _phoneNumber.text = "010-4***-**18";
            // Name stays clipped above mask — do not recentre it into view.

            // ★ Harmony frozen — hold BGM volume/pitch exactly.
            float lockedVol = _bgm != null ? _bgm.volume : 0f;
            float lockedPitch = _bgm != null ? _bgm.pitch : 1f;
            Vector3 camPos = _camRig.position;
            Quaternion camRot = _camRig.rotation;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _camRig.SetPositionAndRotation(camPos, camRot);
                if (_bgm != null)
                {
                    _bgm.volume = lockedVol;
                    _bgm.pitch = lockedPitch;
                }

                yield return null;
            }

            // Pocket — phone UI fades, no dramatized reaction.
            if (_phonePanel != null)
                _phonePanel.gameObject.SetActive(false);
            if (_handOverlay != null)
                _handOverlay.gameObject.SetActive(false);
        }

        private void HideLetterUi()
        {
            ShowLetterUi(false);
            if (_phonePanel != null)
                _phonePanel.gameObject.SetActive(false);
            if (_handOverlay != null)
                _handOverlay.gameObject.SetActive(false);
        }

        private void ShowLetterUi(bool on)
        {
            if (_letterPaper != null)
                _letterPaper.gameObject.SetActive(on);
            if (_letterText != null)
                _letterText.gameObject.SetActive(on);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4-4 Descent
        // ═══════════════════════════════════════════════════════════════════

        private IEnumerator PlayableDescent(float seconds)
        {
            if (_girl != null)
                _girl.localScale = Vector3.one;

            _descentActive = true;
            _descentT = 0f;
            StartFootsteps();

            // Start near tower, path goes downhill toward village lights.
            Vector3 start = _tower != null
                ? _tower.position + new Vector3(1.5f, 0f, 4f)
                : Vector3.zero;
            if (_girl != null)
                _girl.position = start;

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _descentT = Mathf.Clamp01(t / seconds);

                // Village lights one by one.
                if (_villageLights != null)
                {
                    int lit = Mathf.FloorToInt(_descentT * _villageLights.Length);
                    for (int i = 0; i < _villageLights.Length; i++)
                    {
                        if (_villageLights[i] == null)
                            continue;
                        _villageLights[i].enabled = i <= lit;
                        _villageLights[i].intensity = i <= lit ? 2.2f : 0f;
                    }
                }

                // Camera slowly pulls back — girl + tower in one frame, distance grows.
                if (_girl != null && _tower != null)
                {
                    Vector3 mid = Vector3.Lerp(_girl.position, _tower.position + Vector3.up * 8f, 0.35f);
                    float pull = Mathf.Lerp(10f, 28f, _descentT * _descentT);
                    Vector3 camPos = mid + new Vector3(-4f, 5f + _descentT * 6f, -pull);
                    _camRig.position = Vector3.Lerp(_camRig.position, camPos, Time.unscaledDeltaTime * 1.2f);
                    _camRig.rotation = Quaternion.Slerp(_camRig.rotation,
                        Quaternion.LookRotation((mid - camPos).normalized, Vector3.up),
                        Time.unscaledDeltaTime * 1.2f);
                    if (_cam != null)
                        _cam.fieldOfView = Mathf.Lerp(48f, 55f, _descentT);
                }

                yield return null;
            }

            _descentActive = false;
        }

        private void TickDescent()
        {
            if (_girl == null)
                return;

            // Input opens movement — no fail, no goal, no UI.
            bool push = Input.GetMouseButton(0) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ||
                        Input.touchCount > 0 || Input.GetKey(KeyCode.Space);
            float auto = 1.6f; // gentle downhill drift even without input
            float speed = auto + (push ? 3.2f : 0f);
            Vector3 downhill = _tower != null
                ? (_girl.position - _tower.position).normalized
                : Vector3.forward;
            downhill.y = 0f;
            if (downhill.sqrMagnitude < 0.01f)
                downhill = Vector3.forward;
            downhill.Normalize();
            // Away from tower = down the hill.
            _girl.position += downhill * speed * Time.unscaledDeltaTime;
            _girl.rotation = Quaternion.Slerp(_girl.rotation,
                Quaternion.LookRotation(downhill, Vector3.up), Time.unscaledDeltaTime * 3f);

            if (_footsteps != null)
            {
                _footsteps.volume = Mathf.Lerp(0.08f, 0.22f, push ? 1f : 0.35f);
                _footsteps.pitch = Mathf.Lerp(0.9f, 1.15f, push ? 1f : 0.4f);
            }
        }

        private IEnumerator FadeOutWithFootstepTail()
        {
            // Screen fade out…
            yield return FadeVeil(0f, 1f, 2.2f, Color.black);
            // ★ Footsteps continue 3s after fade.
            float t = 0f;
            while (t < FootstepTailSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (_footsteps != null)
                    _footsteps.volume = Mathf.Lerp(0.18f, 0f, t / FootstepTailSeconds);
                yield return null;
            }

            if (_footsteps != null)
                _footsteps.Stop();
            if (_bgm != null)
                _bgm.Stop();
        }

        private IEnumerator ShowTitleCard(float seconds)
        {
            if (_titleCard != null)
            {
                _titleCard.gameObject.SetActive(true);
                _titleCard.text = "우리의 송전탑";
                _titleCard.color = new Color(0.92f, 0.93f, 0.95f, 0f);
            }

            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                if (_titleCard != null)
                {
                    var c = _titleCard.color;
                    c.a = Mathf.Clamp01(t / 1.2f);
                    _titleCard.color = c;
                }

                yield return null;
            }

            yield return Hold(seconds - 1.2f);
            if (_titleCard != null)
                _titleCard.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Credits + stinger
        // ═══════════════════════════════════════════════════════════════════

        private IEnumerator RollCredits(float seconds)
        {
            // Keep veil black. Credits text only — no imagery, no explanations.
            if (_veil != null)
                _veil.alpha = 1f;
            if (_creditsText != null)
            {
                _creditsText.gameObject.SetActive(true);
                _creditsText.text =
                    "Coast Run\n\n" +
                    "우리의 송전탑\n\n\n" +
                    "—\n\n\n";
            }

            float t = 0f;
            var rt = _creditsText != null ? _creditsText.rectTransform : null;
            Vector2 from = new Vector2(0f, -200f);
            Vector2 to = new Vector2(0f, 420f);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (rt != null)
                    rt.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }

            if (_creditsText != null)
                _creditsText.gameObject.SetActive(false);
        }

        private IEnumerator BlackStinger()
        {
            // Screen stays black. No video.
            if (_veil != null)
            {
                _veil.alpha = 1f;
                var img = _veil.GetComponent<Image>();
                if (img != null)
                    img.color = Color.black;
            }

            // Radio click-on
            PlayRadioOn();
            yield return Hold(0.8f);

            if (_stingerSub != null)
            {
                _stingerSub.gameObject.SetActive(true);
                _stingerSub.text =
                    "다음 사연은요… 작년에 부친 편지가 올해 도착했대요. 이런 일도 있네요.\n" +
                    "보내주신 분이 신청곡도 같이 적어 주셨는데, 이거 오래된 노랜데.\n" +
                    "아무튼 틀어 드릴게요.";
            }

            // BGM_Sting_Radio — no fade; plays until tap. Never identify the sender.
            PlayBgm("BGM_Sting_Radio", 220f, 0.05f, 0.45f, loop: true);

            bool tapped = false;
            while (!tapped)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(KeyCode.Space) ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    tapped = true;
                yield return null;
            }

            if (_stingerSub != null)
                _stingerSub.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Audio helpers
        // ═══════════════════════════════════════════════════════════════════

        private void PlayBgm(string key, float freq, float noise, float volume, bool loop = true)
        {
            EnsureAudio();
            var clip = Resources.Load<AudioClip>("CoastRun/Audio/" + key);
            if (clip == null)
                clip = ProceduralAudio.CreateLoop(freq, noise, 8f);
            _bgm.clip = clip;
            _bgm.loop = loop;
            _bgm.volume = volume;
            _bgm.pitch = 1f;
            _bgm.Play();
        }

        private void CrossfadeBgm(string key, float fade)
        {
            if (_bgm == null)
                return;
            StartCoroutine(FadeSource(_bgm, _bgm.volume, 0f, fade));
        }

        private void PlayAmbienceGrass()
        {
            EnsureAudio();
            _ambience.clip = ProceduralAudio.CreateLoop(55f, 0.18f, 6f);
            _ambience.volume = 0.16f;
            _ambience.loop = true;
            _ambience.Play();
        }

        private void PlayPulse()
        {
            EnsureAudio();
            _pulse.clip = ProceduralAudio.CreateLoop(48f, 0.01f, 2f);
            _pulse.volume = 0.05f;
            _pulse.loop = true;
            _pulse.Play();
        }

        private void StopPulse()
        {
            if (_pulse != null && _pulse.isPlaying)
                _pulse.Stop();
        }

        private void PlayPianoNote()
        {
            EnsureAudio();
            _piano.clip = ProceduralAudio.CreateBlip(523.25f, 1.8f); // C5
            _piano.volume = 0.35f;
            _piano.pitch = 0.85f;
            _piano.Play();
        }

        private void StartFootsteps()
        {
            EnsureAudio();
            _footsteps.clip = ProceduralAudio.CreateLoop(90f, 0.15f, 1.2f);
            _footsteps.loop = true;
            _footsteps.volume = 0.1f;
            _footsteps.Play();
        }

        private void PlayRadioOn()
        {
            EnsureAudio();
            _radio.clip = ProceduralAudio.CreateBlip(400f, 0.35f);
            _radio.volume = 0.4f;
            _radio.Play();
        }

        private void StopAllBeds()
        {
            if (_bgm != null) _bgm.Stop();
            if (_ambience != null) _ambience.Stop();
            if (_pulse != null) _pulse.Stop();
            if (_vo != null) _vo.Stop();
            if (_footsteps != null) _footsteps.Stop();
        }

        private void EnsureAudio()
        {
            if (_bgm == null) _bgm = MakeSrc("EndBgm");
            if (_ambience == null) _ambience = MakeSrc("EndAmb");
            if (_pulse == null) _pulse = MakeSrc("EndPulse");
            if (_piano == null) _piano = MakeSrc("EndPiano");
            if (_vo == null) _vo = MakeSrc("EndVo");
            if (_footsteps == null) _footsteps = MakeSrc("EndFeet");
            if (_radio == null) _radio = MakeSrc("EndRadio");
        }

        private AudioSource MakeSrc(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = 0f;
            return a;
        }

        private static IEnumerator FadeSource(AudioSource src, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                if (src != null)
                    src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            if (src != null)
                src.volume = to;
        }

        private IEnumerator FadeVeil(float from, float to, float duration, Color color)
        {
            if (_veil == null)
                yield break;
            var img = _veil.GetComponent<Image>();
            if (img != null)
                img.color = color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _veil.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            _veil.alpha = to;
        }

        private static IEnumerator Hold(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // World / UI build
        // ═══════════════════════════════════════════════════════════════════

        private void BuildWorld()
        {
            // Lighting — blue hour, empty site.
            RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.12f, 0.16f, 0.28f);
            RenderSettings.fogDensity = 0.012f;

            var sunGo = new GameObject("EndingSun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.55f, 0.62f, 0.95f);
            sun.intensity = 0.55f;
            sun.transform.rotation = Quaternion.Euler(18f, -30f, 0f);

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "EndingGround";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.transform.position = Vector3.zero;
            SetMat(ground, new Color(0.22f, 0.26f, 0.2f));

            // Ambiguous closeup patch (roots/rain — neither packed nor dug).
            _groundCloseup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _groundCloseup.name = "AmbiguousGround";
            _groundCloseup.transform.position = new Vector3(0.2f, 0.02f, 0.5f);
            _groundCloseup.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _groundCloseup.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            SetMat(_groundCloseup, new Color(0.28f, 0.24f, 0.18f));
            _groundCloseup.SetActive(false);

            // Silver grass (knee height) — simple stalks, no people.
            for (int i = 0; i < 48; i++)
            {
                var stalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stalk.name = "Grass";
                float x = Random.Range(-6f, 6f);
                float z = Random.Range(-4f, 10f);
                float h = Random.Range(0.45f, 0.85f);
                stalk.transform.position = new Vector3(x, h * 0.5f, z);
                stalk.transform.localScale = new Vector3(0.04f, h, 0.04f);
                stalk.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), Random.Range(-8f, 8f));
                SetMat(stalk, new Color(0.55f, 0.58f, 0.48f));
                Object.Destroy(stalk.GetComponent<Collider>());
            }

            _tower = DestinationGate.CreateVisual(null, 0f);
            _tower.transform.position = new Vector3(0f, 0f, -2f);
            _tower.transform.rotation = Quaternion.identity;

            var beaconGo = new GameObject("EndingBeacon");
            beaconGo.transform.SetParent(_tower.transform, false);
            beaconGo.transform.localPosition = new Vector3(0f, 22f, 0f);
            _beacon = beaconGo.AddComponent<Light>();
            _beacon.type = LightType.Point;
            _beacon.color = new Color(1f, 0.12f, 0.08f);
            _beacon.range = 40f;
            _beacon.intensity = 0f;

            // Girl — simple stand-in. Never spawn the other person.
            _girl = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            _girl.name = "Haneul";
            _girl.position = new Vector3(0.4f, 1f, 1.5f);
            _girl.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            SetMat(_girl.gameObject, new Color(0.15f, 0.18f, 0.28f));
            Object.Destroy(_girl.GetComponent<Collider>());

            _board = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            _board.name = "Board";
            _board.position = _girl.position + new Vector3(0f, -0.85f, 0.1f);
            _board.localScale = new Vector3(0.25f, 0.05f, 0.75f);
            SetMat(_board.gameObject, new Color(0.35f, 0.22f, 0.12f));
            Object.Destroy(_board.GetComponent<Collider>());

            // Can / truck / paper props (hidden until dig).
            _canProp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _canProp.name = "SnackCan";
            _canProp.transform.SetParent(_girl, false);
            _canProp.transform.localPosition = new Vector3(0f, -0.15f, 0.35f);
            _canProp.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            SetMat(_canProp, new Color(0.45f, 0.32f, 0.18f));
            _canProp.SetActive(false);

            _truckProp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _truckProp.name = "BoardTruck";
            _truckProp.transform.SetParent(_canProp.transform, false);
            _truckProp.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            _truckProp.transform.localScale = new Vector3(0.8f, 0.25f, 0.35f);
            SetMat(_truckProp, new Color(0.75f, 0.78f, 0.8f));
            _truckProp.SetActive(false);

            _paperProp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _paperProp.name = "FoldedPaper";
            _paperProp.transform.SetParent(_canProp.transform, false);
            _paperProp.transform.localPosition = new Vector3(0.15f, 0.5f, 0.1f);
            _paperProp.transform.localScale = new Vector3(0.5f, 0.05f, 0.7f);
            SetMat(_paperProp, new Color(0.9f, 0.88f, 0.8f));
            _paperProp.SetActive(false);

            // Village lights in the valley ahead — off until descent.
            _villageLights = new Light[8];
            for (int i = 0; i < _villageLights.Length; i++)
            {
                var go = new GameObject("VillageLight_" + i);
                go.transform.position = new Vector3(
                    Random.Range(-10f, 10f), 0.5f, 18f + i * 3.5f + Random.Range(-1f, 1f));
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.75f, 0.45f);
                l.range = 8f;
                l.intensity = 0f;
                l.enabled = false;
                _villageLights[i] = l;
            }

            // Camera
            var camGo = new GameObject("EndingCamera");
            _camRig = camGo.transform;
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.1f, 0.14f, 0.28f);
            _cam.fieldOfView = 42f;
            _cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            if (camGo.GetComponent<CoastPortraitViewport>() == null)
                camGo.AddComponent<CoastPortraitViewport>();

            // Disable any leftover cameras from prior scenes.
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != null && cams[i] != _cam)
                    cams[i].enabled = false;
            }
        }

        private void BuildUi()
        {
            _ui = CoastUiCanvas.Create("EndingUI", 400);
            var root = CoastUiCanvas.Root(_ui);

            var veilGo = new GameObject("Veil", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            veilGo.transform.SetParent(root, false);
            var vrt = veilGo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            veilGo.GetComponent<Image>().color = Color.black;
            _veil = veilGo.GetComponent<CanvasGroup>();
            _veil.alpha = 0f;
            _veil.blocksRaycasts = false;

            // Hand + letter paper (no face).
            _handOverlay = MakeImage(root, "Hand", new Vector2(0.05f, 0f), new Vector2(0.95f, 0.35f),
                new Color(0.12f, 0.1f, 0.09f, 0.9f));
            _handOverlay.gameObject.SetActive(false);

            _letterPaper = MakeImage(root, "Paper", new Vector2(0.12f, 0.28f), new Vector2(0.88f, 0.82f),
                new Color(0.93f, 0.9f, 0.82f, 0.98f));
            _letterText = CoastHudLayout.MakeText(_letterPaper.transform, "LetterBody", "", 17,
                TextAnchor.UpperLeft,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            _letterText.color = new Color(0.18f, 0.16f, 0.14f);
            _letterText.fontStyle = FontStyle.Normal;
            _letterText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _letterText.verticalOverflow = VerticalWrapMode.Overflow;
            _letterPaper.gameObject.SetActive(false);

            // Phone — number in frame, name clipped above.
            _phonePanel = new GameObject("Phone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _phonePanel.SetParent(root, false);
            _phonePanel.anchorMin = new Vector2(0.28f, 0.22f);
            _phonePanel.anchorMax = new Vector2(0.72f, 0.72f);
            _phonePanel.offsetMin = Vector2.zero;
            _phonePanel.offsetMax = Vector2.zero;
            _phonePanel.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.13f, 0.98f);
            // Clip children that go outside phone bezel.
            _phonePanel.gameObject.AddComponent<RectMask2D>();

            _phoneNameClipped = CoastHudLayout.MakeText(_phonePanel, "Name", "서도윤", 22,
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            _phoneNameClipped.color = new Color(0.9f, 0.9f, 0.92f);
            // Name sits above the phone bezel → RectMask2D clips it. Number stays visible.
            var nameRt = _phoneNameClipped.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 36f);
            nameRt.sizeDelta = new Vector2(200f, 36f);

            _phoneNumber = CoastHudLayout.MakeText(_phonePanel, "Number", "010-4***-**18", 20,
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.55f), Vector2.zero, Vector2.zero);
            _phoneNumber.color = new Color(0.75f, 0.8f, 0.85f);

            // Call button present but never pressed / never highlighted as action.
            var callBtn = MakeImage(_phonePanel, "CallBtn", new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.22f),
                new Color(0.2f, 0.55f, 0.35f, 0.55f));
            callBtn.raycastTarget = false;

            _phonePanel.gameObject.SetActive(false);

            _titleCard = CoastHudLayout.MakeText(root, "TitleCard", "", 40,
                TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), Vector2.zero, Vector2.zero);
            _titleCard.gameObject.SetActive(false);

            _creditsText = CoastHudLayout.MakeText(root, "Credits", "", 22,
                TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.8f), Vector2.zero, Vector2.zero);
            _creditsText.gameObject.SetActive(false);

            _stingerSub = CoastHudLayout.MakeText(root, "Stinger", "", 18,
                TextAnchor.LowerCenter,
                new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);
            _stingerSub.color = new Color(0.85f, 0.88f, 0.9f);
            _stingerSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            _stingerSub.gameObject.SetActive(false);
        }

        private static Image MakeImage(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetMat(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return;
            var m = new Material(shader);
            m.color = color;
            r.sharedMaterial = m;
        }

        private static void HideAllRunUi()
        {
            foreach (var name in new[]
                     {
                         "CoastRunHUD", "JourneyHUD", "PhoneHUD", "StageClearCanvas",
                         "CutsceneProcUI", "CutsceneSkipUI", "MemoryPopupCanvas"
                     })
            {
                var go = GameObject.Find(name);
                if (go != null)
                    go.SetActive(false);
            }
        }
    }

    /// Letter body — ambiguous by design. Do not add clarifying narration.
    public static class EndingLetter
    {
        public static readonly string[] Lines =
        {
            "하늘아.",
            "나 다음 달에 이사 가. 아빠 회사 때문이고 나는 반대했는데 소용없었어.\n아니, 그게 아니고. 이 얘기 하려고 부른 거 맞긴 한데 그거 말고도 있어.",
            "네 트럭 고쳐놨어. 부싱이 나갔던 거였고 킹핀은 멀쩡했어. 내가 열두 살 때 진단을 잘못했더라.\n5년 걸렸다. 미안.",
            "그리고 좋아해.\n이 순서로 말하려고 했는데 막상 만나면 순서 다 틀릴 것 같아서 그냥 적어 놓는다.",
            "늦게 와도 돼. 기다릴게."
        };
    }
}
