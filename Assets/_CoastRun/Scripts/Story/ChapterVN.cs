using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 어드벤처식 컷씬 플레이어 — 배경(BG) + 스탠딩(L/R) + 텍스트박스, 중요 컷은 CG.
    /// 대본은 ChapterScript(v5). 탭/스페이스로 진행, 길게 누르거나 SKIP 버튼으로 건너뛴다.
    /// 어느 씬 위에서든 오버레이로 뜬다(DontDestroyOnLoad). Time.timeScale은 건드리지 않고 unscaled 시간을 쓴다.
    public class ChapterVN : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }
        private static ChapterVN _active;

        public static void Play(string sceneId, Action onDone, string titleCard = null)
        {
            if (!ChapterScript.Has(sceneId))
            {
                onDone?.Invoke();
                return;
            }
            if (_active != null)
                UnityEngine.Object.Destroy(_active.gameObject);
            var go = new GameObject("ChapterVN");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _active = go.AddComponent<ChapterVN>();
            _active.Begin(sceneId, onDone, titleCard);
        }

        public static void PlayChapterOpening(int chapter, Action onDone)
        {
            string title = chapter >= 1 ? $"CHAPTER {chapter}\n「{ChapterScript.Title(chapter)}」" : null;
            Play(ChapterScript.OpenId(chapter), onDone, title);
        }

        public static bool HasClosing(int chapter) => ChapterScript.Has(ChapterScript.CloseId(chapter));
        public static void PlayChapterClosing(int chapter, Action onDone) => Play(ChapterScript.CloseId(chapter), onDone);

        // ── 상태 ──────────────────────────────────────────────────────────
        private VnLine[] _lines;
        private Action _onDone;
        private string _titleCard;
        private Canvas _canvas;
        private Image _black;
        private Image _fader;
        private Image _bg;
        private Image _cg;
        private Image _standL, _standR;
        private RectTransform _artArea;
        private Image _box;
        private Text _nameTag;
        private Image _namePlate;
        private Text _body;
        private Text _cursor;
        private Text _titleText;
        private CanvasGroup _titleCg;
        private bool _advance;
        private bool _skip;
        private bool _typing;
        private float _holdTimer;
        private string _curL, _curR;

        private const float TypeCps = 34f;

        private void Begin(string sceneId, Action onDone, string titleCard)
        {
            _lines = ChapterScript.Get(sceneId);
            _onDone = onDone;
            _titleCard = titleCard;
            IsPlaying = true;
            PlayerPrefs.SetInt("CoastRun_VN_" + sceneId, 1);
            BuildUi();
            StartCoroutine(Run());
        }

        private void BuildUi()
        {
            _canvas = CoastUiCanvas.Create("ChapterVNCanvas", 450);
            UnityEngine.Object.DontDestroyOnLoad(_canvas.gameObject);
            var root = CoastUiCanvas.Root(_canvas);

            // 전체 검정(레터박스 밖도 덮는다)
            _black = CoastHudLayout.MakeImage(root, "Black", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), Color.black);
            _black.raycastTarget = true;
            // 아래 UI로 클릭이 새지 않게 레이캐스트만 막는다. 진행 입력은 Pressed()가 Input으로 읽는다.

            // 그림 영역(상단 62%)
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(Mask));
            artGo.transform.SetParent(root, false);
            _artArea = artGo.GetComponent<RectTransform>();
            _artArea.anchorMin = new Vector2(0f, 0.36f);
            _artArea.anchorMax = new Vector2(1f, 1f);
            _artArea.offsetMin = new Vector2(-CoastUiCanvas.HudPad, 0f);
            _artArea.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
            var artImg = artGo.GetComponent<Image>();
            artImg.color = new Color(0.08f, 0.07f, 0.09f, 1f);
            artImg.raycastTarget = false;
            artGo.GetComponent<Mask>().showMaskGraphic = true;

            _bg = MakeCover(_artArea, "BG");
            _standL = MakeStanding(_artArea, "StandL", 0.25f);
            _standR = MakeStanding(_artArea, "StandR", 0.75f);
            _cg = MakeCover(_artArea, "CG");
            _cg.gameObject.SetActive(false);

            // 하단 그라데이션 띠(그림→텍스트박스 이음새)
            var shade = CoastHudLayout.MakeImage(root, "Shade", new Vector2(0f, 0.30f), new Vector2(1f, 0.42f),
                new Vector2(-CoastUiCanvas.HudPad, 0f), new Vector2(CoastUiCanvas.HudPad, 0f), new Color(0f, 0f, 0f, 0.35f));
            shade.raycastTarget = false;

            // 텍스트박스
            _box = CoastOrnate.Panel(root, "TextBox", CoastOrnate.Gold, new Vector2(0f, 0.02f), new Vector2(1f, 0.35f),
                new Vector2(10f, 0f), new Vector2(-10f, 0f), CoastOrnate.Ivory);
            _body = CoastOrnate.Label(_box.transform, "Body", "", 24, CoastOrnate.Ink, TextAnchor.UpperLeft);
            CoastOrnate.Stretch(_body.rectTransform, 30f, 26f, -30f, -48f);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _body.lineSpacing = 1.25f;

            _namePlate = CoastUiArt.Panel(_box.transform, "NamePlate", CoastOrnate.WoodDark, 12);
            var nrt = _namePlate.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.anchoredPosition = new Vector2(22f, 0f);
            nrt.sizeDelta = new Vector2(150f, 44f);
            _namePlate.raycastTarget = false;
            _nameTag = CoastOrnate.Label(_namePlate.transform, "Name", "", 22, CoastOrnate.GoldLight);
            CoastUiArt.OutlineText(_nameTag, new Color(0f, 0f, 0f, 0.4f), 1.2f);

            _cursor = CoastOrnate.Label(_box.transform, "Cursor", "▼", 20, CoastOrnate.Wood);
            var crt = _cursor.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(1f, 0f);
            crt.anchoredPosition = new Vector2(-22f, 14f);
            crt.sizeDelta = new Vector2(40f, 30f);

            // SKIP
            CoastOrnate.MenuButton(root, "Skip", "SKIP", new Vector2(1f, 1f), new Vector2(-60f, -34f), new Vector2(96f, 44f), () => _skip = true, CoastOrnate.WoodDark, 18);

            // 페이더(최상단, 클릭 통과)
            _fader = CoastHudLayout.MakeImage(root, "Fader", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), Color.black);
            _fader.raycastTarget = false;

            // 챕터 타이틀 카드
            var tgo = new GameObject("TitleCard", typeof(RectTransform), typeof(CanvasGroup));
            tgo.transform.SetParent(root, false);
            CoastOrnate.Stretch(tgo.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            _titleCg = tgo.GetComponent<CanvasGroup>();
            _titleCg.alpha = 0f;
            _titleCg.blocksRaycasts = false;
            var tdim = CoastHudLayout.MakeImage(tgo.transform, "Dim", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad - 400f, -CoastUiCanvas.HudPad - 400f), new Vector2(CoastUiCanvas.HudPad + 400f, CoastUiCanvas.HudPad + 400f), new Color(0f, 0f, 0f, 0.85f));
            tdim.raycastTarget = false;
            _titleText = CoastOrnate.Label(tgo.transform, "T", "", 40, CoastOrnate.Ivory);
            _titleText.lineSpacing = 1.3f;
            CoastUiArt.OutlineText(_titleText, new Color(0.83f, 0.69f, 0.22f, 0.9f), 1.5f);
            tgo.SetActive(false);
        }

        private static Image MakeCover(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 4f / 3f;
            return img;
        }

        private static Image MakeStanding(RectTransform parent, string name, float x)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 0.82f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta = new Vector2(0f, 0f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 0.5f;
            go.SetActive(false);
            return img;
        }

        private IEnumerator Run()
        {
            _fader.color = Color.black;
            if (!string.IsNullOrEmpty(_titleCard))
            {
                _titleText.text = _titleCard;
                _titleCg.gameObject.SetActive(true);
                yield return Fade(_titleCg, 0f, 1f, 0.5f);
                float hold = 0f;
                while (hold < 1.4f && !_skip && !Pressed())
                {
                    hold += Time.unscaledDeltaTime;
                    yield return null;
                }
                _advance = false;
                yield return Fade(_titleCg, 1f, 0f, 0.4f);
                _titleCg.gameObject.SetActive(false);
            }

            SetText("", "", false);
            for (int i = 0; i < _lines.Length && !_skip; i++)
            {
                var line = _lines[i];
                switch (line.Kind)
                {
                    case "BG":
                        yield return ShowBg(line);
                        break;
                    case "CG":
                        yield return ShowCg(line.A);
                        break;
                    case "SAY":
                        yield return Say(line.A, line.B);
                        break;
                    case "NARR":
                        yield return Say("", line.B);
                        break;
                    case "LETTER":
                        yield return Say("", line.B, letter: true);
                        break;
                }
            }

            yield return FadeImage(_fader, 1f, 0.3f, true);
            Finish();
        }

        private IEnumerator ShowBg(VnLine line)
        {
            // 짧은 암전 후 배경 교체
            bool first = _bg.sprite == null && !_cg.gameObject.activeSelf && _fader.color.a > 0.99f;
            if (!first) yield return FadeImage(_fader, 1f, 0.22f, true);
            _cg.gameObject.SetActive(false);
            var tex = ArtAssets.LoadTexture("BG_" + line.A);
            if (tex != null)
            {
                _bg.sprite = CoastUiArt.AsSprite(tex, 100f);
                _bg.color = Tint(line.D);
                _bg.gameObject.SetActive(true);
            }
            else
            {
                _bg.sprite = null;
                _bg.color = new Color(0.16f, 0.14f, 0.18f, 1f);
            }
            SetStanding(_standL, line.B, out _curL);
            SetStanding(_standR, line.C, out _curR);
            SetText("", "", false);
            yield return FadeImage(_fader, 0f, 0.3f, false);
        }

        private static Color Tint(string variant)
        {
            if (string.IsNullOrEmpty(variant)) return Color.white;
            if (variant.Contains("눈")) return new Color(0.86f, 0.90f, 1f);
            if (variant.Contains("밤")) return new Color(0.55f, 0.6f, 0.8f);
            return Color.white;
        }

        private IEnumerator ShowCg(string id)
        {
            var tex = ArtAssets.LoadTexture("Cut_" + id);
            yield return FadeImage(_fader, 1f, 0.25f, true);
            _standL.gameObject.SetActive(false);
            _standR.gameObject.SetActive(false);
            if (tex != null)
            {
                _cg.sprite = CoastUiArt.AsSprite(tex, 100f);
                _cg.color = Color.white;
                _cg.gameObject.SetActive(true);
            }
            else
            {
                // 그림이 아직 없으면 배경만 어둡게 남긴다(스탠딩 없이).
                _cg.gameObject.SetActive(false);
                _bg.color = new Color(0.55f, 0.5f, 0.5f, 1f);
            }
            SetText("", "", false);
            yield return FadeImage(_fader, 0f, 0.35f, false);
        }

        private void SetStanding(Image img, string who, out string cur)
        {
            cur = who;
            string res = ChapterScript.StandingResource(who);
            var tex = res != null ? ArtAssets.LoadTexture(res) : null;
            if (tex == null)
            {
                img.gameObject.SetActive(false);
                return;
            }
            img.sprite = CoastUiArt.AsSprite(tex, 100f);
            img.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;
            img.color = Color.white;
            img.gameObject.SetActive(true);
        }

        private void Highlight(string speaker)
        {
            bool anyone = !string.IsNullOrEmpty(speaker);
            HighlightOne(_standL, _curL, speaker, anyone);
            HighlightOne(_standR, _curR, speaker, anyone);
        }

        private static void HighlightOne(Image img, string who, string speaker, bool anyone)
        {
            if (img == null || !img.gameObject.activeSelf) return;
            bool me = anyone && ChapterScript.SpeakerName(who) == speaker;
            img.color = (!anyone || me) ? Color.white : new Color(0.62f, 0.6f, 0.66f, 1f);
            img.transform.localScale = me ? Vector3.one * 1.02f : Vector3.one;
        }

        private IEnumerator Say(string speaker, string text, bool letter = false)
        {
            Highlight(speaker);
            SetText(speaker, "", letter);
            _typing = true;
            _advance = false;
            float shown = 0f;
            while (shown < text.Length)
            {
                if (_skip) break;
                if (Pressed())
                {
                    _advance = false;
                    break;
                }
                shown += Time.unscaledDeltaTime * TypeCps;
                _body.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown)));
                yield return null;
            }
            _body.text = text;
            _typing = false;
            _advance = false;
            float blink = 0f;
            while (!_skip && !Pressed())
            {
                blink += Time.unscaledDeltaTime;
                _cursor.gameObject.SetActive(Mathf.Repeat(blink, 1f) < 0.6f);
                yield return null;
            }
            _cursor.gameObject.SetActive(false);
            _advance = false;
        }

        private void SetText(string speaker, string body, bool letter)
        {
            bool hasName = !string.IsNullOrEmpty(speaker);
            _namePlate.gameObject.SetActive(hasName);
            _nameTag.text = speaker;
            _body.text = body;
            _body.fontStyle = hasName ? FontStyle.Normal : FontStyle.Italic;
            _body.color = hasName ? CoastOrnate.Ink : new Color(0.36f, 0.30f, 0.28f);
            _body.alignment = letter ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
            _body.fontSize = letter ? 28 : (hasName ? 25 : 23);
            _cursor.gameObject.SetActive(false);
        }

        /// 탭/클릭/스페이스/엔터 한 번 = 진행. 1초 길게 누르면 스킵.
        private bool Pressed()
        {
            bool down = _advance || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                        (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.touchCount > 0;
            _holdTimer = held ? _holdTimer + Time.unscaledDeltaTime : 0f;
            if (_holdTimer > 1.2f) { _skip = true; _holdTimer = 0f; }
            if (Input.GetKeyDown(KeyCode.S)) _skip = true;
            return down;
        }

        private static IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            cg.alpha = to;
        }

        private static IEnumerator FadeImage(Image img, float toAlpha, float dur, bool keepRaycast)
        {
            float from = img.color.a;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var c = img.color; c.a = Mathf.Lerp(from, toAlpha, t / dur); img.color = c;
                yield return null;
            }
            var cc = img.color; cc.a = toAlpha; img.color = cc;
        }

        private void Finish()
        {
            IsPlaying = false;
            var cb = _onDone;
            _onDone = null;
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            if (_active == this) _active = null;
            UnityEngine.Object.Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
