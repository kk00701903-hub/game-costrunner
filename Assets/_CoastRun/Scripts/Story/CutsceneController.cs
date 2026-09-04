using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace CoastRun
{
    /// Plays Timeline cutscenes (or procedural fallback). Hold 1s to skip.
    /// Prologue completion uses zero-load camera handoff — no fade/lerp/loading.
    public class CutsceneController : MonoBehaviour
    {
        public const float HoldSkipSeconds = 1f;

        [SerializeField] private PlayableDirector director;
        [SerializeField] private Camera cineCamera;
        [SerializeField] private AudioSource bgmSource;

        private CutsceneDef _def;
        private Action<CutsceneController> _onComplete;
        private bool _playing;
        private bool _holdSkipping;
        private float _holdTimer;
        private Image _skipRing;
        private Canvas _skipCanvas;
        private float _playhead;
        private bool _useProcedural;
        private Coroutine _routine;
        private static CutsceneController _stub;

        public Camera CineCamera => cineCamera;
        public CutsceneDef CurrentDef => _def;
        public bool IsPlaying => _playing;

        public static CutsceneController Ensure()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<CutsceneController>();
            if (existing != null)
                return existing;
            if (_stub != null)
                return _stub;

            var go = new GameObject("CutsceneController");
            _stub = go.AddComponent<CutsceneController>();
            return _stub;
        }

        public void Play(CutsceneDef def, Action<CutsceneController> onComplete)
        {
            if (def == null)
            {
                onComplete?.Invoke(this);
                return;
            }

            _def = def;
            _onComplete = onComplete;
            _playing = true;
            _playhead = 0f;
            _holdTimer = 0f;
            EnsureGear();
            UnityEngine.Object.FindAnyObjectByType<CoastAudioManager>()?.SetBedMuted(true);

            if (_routine != null)
                StopCoroutine(_routine);

            if (def.timeline != null && director != null)
            {
                _useProcedural = false;
                director.playableAsset = def.timeline;
                director.time = 0;
                director.Play();
                StartBgm(def);
                _routine = StartCoroutine(WatchTimeline());
            }
            else
            {
                _useProcedural = true;
                StartBgm(def);
                _routine = StartCoroutine(PlayProcedural(def));
            }
        }

        public void PlayKind(CutsceneKind kind, int chapter, Action<CutsceneController> onComplete)
        {
            var table = CoastConfigRegistry.CutsceneTable;
            table.EnsurePopulated();
            var def = table.Resolve(kind, chapter) ?? FallbackDef(kind, chapter);
            Play(def, onComplete);
        }

        private static CutsceneDef FallbackDef(CutsceneKind kind, int chapter)
        {
            var table = CutsceneTable.CreateDefault();
            return table.Resolve(kind, chapter);
        }

        private IEnumerator WatchTimeline()
        {
            while (_playing && director != null && director.state == PlayState.Playing)
            {
                _playhead = (float)director.time;
                EnforceCh4Silence();
                HandleHoldSkip();
                yield return null;
            }

            // Director stopped or finished
            if (_playing)
                Finish();
        }

        private IEnumerator PlayProcedural(CutsceneDef def)
        {
            EnsureProceduralUi();
            if (def.id == "Prologue")
                yield return PlayPrologueBeats();
            else
                yield return PlayGenericBeats(def);

            if (_playing)
                Finish();
        }

        private IEnumerator PlayPrologueBeats()
        {
            // P1 0:40 — phone message; send-time field visible (tiny).
            yield return ShowBeat(
                "약속의 스마트폰",
                "「노을 질 때, 우리 어릴 적 비밀 기지였던\n그 송전탑 아래에서 만나자.\n꼭 할 말이 있어.」",
                8f, showSendTime: true, showStickers: false);

            // P2 0:50 — station board
            yield return ShowBeat(
                "예기치 못한 장애",
                "정류장 전광판\n『정비 중 · 운행 중단』",
                8f, showSendTime: false, showStickers: false);

            // P3 0:40 — board underside stickers MUST be visible
            yield return ShowBeat(
                "소녀의 결심",
                "보드를 뒤집는다.",
                8f, showSendTime: false, showStickers: true);

            // P4 0:20 — not a rendered clip; handoff frame
            yield return ShowBeat(
                "",
                "",
                2.5f, showSendTime: false, showStickers: false, clearUi: true);

            // P4 complete → handoff (Finish will detect Prologue)
        }

        private IEnumerator PlayGenericBeats(CutsceneDef def)
        {
            string title = def.id;
            string body = "";
            if (def.id == "CH3_Close")
            {
                title = "작년에도";
                body = "같은 길. 다른 계절.";
            }
            else if (def.id == "CH4_Close")
            {
                title = "16:40";
                body = "발신 시각이 보인다.";
            }

            float hold = Mathf.Clamp(def.duration * 0.15f, 4f, 12f);
            float t = 0f;
            ShowProceduralText(title, body, false, false);
            while (t < hold && _playing)
            {
                t += Time.unscaledDeltaTime;
                _playhead = t;
                // CH4 silence window — mute BGM, do NOT inject ambient.
                if (def.silenceStart >= 0f)
                {
                    float scaled = (t / hold) * def.duration;
                    EnforceSilenceAt(scaled);
                }

                HandleHoldSkip();
                yield return null;
            }
        }

        private IEnumerator ShowBeat(string title, string body, float seconds,
            bool showSendTime, bool showStickers, bool clearUi = false)
        {
            if (clearUi)
                HideProceduralUi();
            else
                ShowProceduralText(title, body, showSendTime, showStickers);

            float t = 0f;
            while (t < seconds && _playing)
            {
                t += Time.unscaledDeltaTime;
                _playhead += Time.unscaledDeltaTime;
                HandleHoldSkip();
                yield return null;
            }
        }

        private void EnforceCh4Silence()
        {
            if (_def == null || _def.silenceStart < 0f)
                return;
            EnforceSilenceAt(_playhead);
        }

        private void EnforceSilenceAt(float timeSeconds)
        {
            if (_def == null || _def.silenceStart < 0f || bgmSource == null)
                return;

            // Intentional void — never fade ambient in to "fix" the gap.
            if (timeSeconds >= _def.silenceStart && timeSeconds <= _def.silenceEnd)
            {
                if (bgmSource.isPlaying)
                    bgmSource.Pause();
                bgmSource.volume = 0f;
            }
            else if (!bgmSource.isPlaying && _playing && bgmSource.clip != null)
            {
                bgmSource.UnPause();
                bgmSource.volume = 0.45f;
            }
        }

        private void HandleHoldSkip()
        {
            bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) ||
                        Input.GetKey(KeyCode.Escape) ||
                        (Input.touchCount > 0);
            if (held)
            {
                _holdTimer += Time.unscaledDeltaTime;
                _holdSkipping = true;
                UpdateSkipRing(_holdTimer / HoldSkipSeconds);
                if (_holdTimer >= HoldSkipSeconds)
                    SkipNow();
            }
            else
            {
                _holdTimer = Mathf.MoveTowards(_holdTimer, 0f, Time.unscaledDeltaTime * 2f);
                _holdSkipping = _holdTimer > 0.01f;
                UpdateSkipRing(_holdTimer / HoldSkipSeconds);
            }
        }

        private void SkipNow()
        {
            if (!_playing)
                return;
            if (director != null && director.state == PlayState.Playing)
                director.Stop();
            Finish();
        }

        private void Finish()
        {
            if (!_playing)
                return;
            _playing = false;
            HideProceduralUi();
            StopBgm();
            // Unmute run bed after cutscene — except prologue handoff keeps mute until Release.
            if (_def == null || _def.id != "Prologue")
                UnityEngine.Object.FindAnyObjectByType<CoastAudioManager>()?.SetBedMuted(false);

            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke(this);
        }

        private void StartBgm(CutsceneDef def)
        {
            EnsureGear();
            if (bgmSource == null || string.IsNullOrEmpty(def.bgmKey))
                return;

            // Prefer Resources clip; else procedural placeholder. Never invent fill during silence.
            // Generated scores live in Resources/CoastRun/BGM (CoastBgmLibrary); the old
            // CoastRun/Audio path stays as a second lookup for hand-placed clips.
            var clip = CoastBgmLibrary.Load(def.bgmKey)
                       ?? Resources.Load<AudioClip>("CoastRun/Audio/" + def.bgmKey);
            bool real = clip != null;
            if (clip == null)
                clip = ProceduralAudio.CreateLoop(def.isTwistCut ? 98f : 160f, 0.04f, 6f);
            bgmSource.clip = clip;
            bgmSource.loop = !real;          // a composed cue plays once, to picture
            bgmSource.volume = real ? 0.7f : 0.45f;
            bgmSource.Play();
        }

        private void StopBgm()
        {
            if (bgmSource != null && bgmSource.isPlaying)
                bgmSource.Stop();
        }

        private void EnsureGear()
        {
            if (director == null)
                director = GetComponent<PlayableDirector>() ?? gameObject.AddComponent<PlayableDirector>();

            if (cineCamera == null)
            {
                var camGo = GameObject.Find("CineCamera");
                if (camGo == null)
                {
                    camGo = new GameObject("CineCamera");
                    cineCamera = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                }
                else
                    cineCamera = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            }

            cineCamera.enabled = true;
            if (cineCamera.GetComponent<CoastPortraitViewport>() == null)
                cineCamera.gameObject.AddComponent<CoastPortraitViewport>();

            // The run camera's listener is switched off for the handoff; the cutscene
            // must carry its own or the whole prologue (and its score) plays silent.
            var cineListener = cineCamera.GetComponent<AudioListener>();
            if (cineListener == null)
                cineListener = cineCamera.gameObject.AddComponent<AudioListener>();
            cineListener.enabled = true;

            // Framing for procedural / missing timeline — looking down the promenade.
            cineCamera.transform.position = DownhillPath.Point(12f, 0.2f, 2.2f) +
                                           DownhillPath.Rotation * new Vector3(0.3f, 1.5f, -6.5f);
            Vector3 look = DownhillPath.Point(24f, 0f, 1.2f);
            cineCamera.transform.rotation = Quaternion.LookRotation(
                (look - cineCamera.transform.position).normalized, Vector3.up);
            cineCamera.fieldOfView = 48f;

            if (bgmSource == null)
            {
                var a = new GameObject("CutsceneBgm");
                a.transform.SetParent(transform, false);
                bgmSource = a.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f;
            }

            EnsureSkipUi();
        }

        private void EnsureSkipUi()
        {
            if (_skipCanvas != null)
                return;
            _skipCanvas = CoastUiCanvas.Create("CutsceneSkipUI", 400);
            var root = CoastUiCanvas.Root(_skipCanvas);
            var go = new GameObject("SkipRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.92f, 0.08f);
            rt.sizeDelta = new Vector2(48f, 48f);
            _skipRing = go.GetComponent<Image>();
            _skipRing.type = Image.Type.Filled;
            _skipRing.fillMethod = Image.FillMethod.Radial360;
            _skipRing.fillOrigin = (int)Image.Origin360.Top;
            _skipRing.color = new Color(1f, 1f, 1f, 0.55f);
            _skipRing.fillAmount = 0f;
            go.SetActive(false);
        }

        private void UpdateSkipRing(float amount)
        {
            if (_skipRing == null)
                return;
            bool show = amount > 0.02f;
            _skipRing.gameObject.SetActive(show);
            _skipRing.fillAmount = Mathf.Clamp01(amount);
        }

        // ── Procedural UI (until Timeline assets land) ─────────────────────

        private Canvas _procCanvas;
        private Text _procTitle;
        private Text _procBody;
        private Text _sendTime;
        private GameObject _stickerRow;

        private void EnsureProceduralUi()
        {
            if (_procCanvas != null)
                return;
            _procCanvas = CoastUiCanvas.Create("CutsceneProcUI", 320);
            var root = CoastUiCanvas.Root(_procCanvas);

            var bg = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0.42f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.72f);

            _procTitle = MakeText(root, "Title", 28, new Vector2(0.5f, 0.28f), new Vector2(560f, 40f));
            _procBody = MakeText(root, "Body", 20, new Vector2(0.5f, 0.18f), new Vector2(560f, 100f));

            _sendTime = MakeText(root, "SendTime", 10, new Vector2(0.5f, 0.12f), new Vector2(400f, 24f));
            _sendTime.text = "발신  어제 19:04";
            var c = _sendTime.color;
            c.a = 0.35f;
            _sendTime.color = c;
            _sendTime.gameObject.SetActive(false);

            _stickerRow = new GameObject("Stickers", typeof(RectTransform));
            _stickerRow.transform.SetParent(root, false);
            var srt = _stickerRow.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.55f);
            srt.sizeDelta = new Vector2(420f, 80f);
            // Board underside stickers — visual thread to ending.
            AddSticker(_stickerRow.transform, "🐻🍦", -140f);      // 북극곰 아이스크림
            AddSticker(_stickerRow.transform, "BAND", 0f);         // 반쯤 뜯긴 밴드 로고
            AddSticker(_stickerRow.transform, "Y+D", 140f);        // 유성펜
            _stickerRow.SetActive(false);
        }

        private static void AddSticker(Transform parent, string label, float x)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(100f, 64f);
            go.GetComponent<Image>().color = new Color(0.95f, 0.9f, 0.75f, 0.95f);
            var t = MakeText(go.transform, "L", 16, new Vector2(0.5f, 0.5f), new Vector2(96f, 60f));
            t.text = label;
            t.color = new Color(0.2f, 0.15f, 0.1f);
        }

        private void ShowProceduralText(string title, string body, bool sendTime, bool stickers)
        {
            EnsureProceduralUi();
            _procCanvas.gameObject.SetActive(true);
            if (_procTitle != null)
                _procTitle.text = title;
            if (_procBody != null)
                _procBody.text = body;
            if (_sendTime != null)
                _sendTime.gameObject.SetActive(sendTime);
            if (_stickerRow != null)
                _stickerRow.SetActive(stickers);
        }

        private void HideProceduralUi()
        {
            if (_procCanvas != null)
                _procCanvas.gameObject.SetActive(false);
            if (_stickerRow != null)
                _stickerRow.SetActive(false);
            if (_sendTime != null)
                _sendTime.gameObject.SetActive(false);
        }

        private static Text MakeText(Transform parent, string name, int size, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = sizeDelta;
            var text = go.AddComponent<Text>();
            text.font = CoastHudLayout.Font();
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.93f, 0.88f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
    }
}
