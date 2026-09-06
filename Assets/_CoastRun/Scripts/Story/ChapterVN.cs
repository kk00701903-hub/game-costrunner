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
            string title = chapter >= 1 ? $"CHAPTER {chapter}\n「{ChapterScript.Title(chapter)}」\n<size=18>{ChapterLocation.Get(chapter).Name}</size>" : null;
            Play(ChapterScript.OpenId(chapter), onDone, title);
        }

        public static bool HasClosing(int chapter) => ChapterScript.Has(ChapterScript.CloseId(chapter));
        public static void PlayChapterClosing(int chapter, Action onDone) => Play(ChapterScript.CloseId(chapter), onDone);

        // ── 상태 ──────────────────────────────────────────────────────────
        /// 옛 방식(배경 + 스탠딩 합성). 새 그림은 인물까지 그려진 풀 일러스트라 기본 꺼짐.
        public static bool UseStandings = false;
        private VnLine[] _lines;
        private string _sceneId;
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
            _sceneId = sceneId;
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

            // 그림 영역 — 화면 전체(레터박스 포함). 그림은 720×1280 세로 풀 일러스트, cover 로 채운다.
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(Mask));
            artGo.transform.SetParent(root, false);
            _artArea = artGo.GetComponent<RectTransform>();
            _artArea.anchorMin = new Vector2(0f, 0f);
            _artArea.anchorMax = new Vector2(1f, 1f);
            _artArea.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
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

            // 텍스트박스 — 프린세스 메이커식: 그림 위에 반투명 창. 기본은 아래, 씬이 '위'를 요구하면 위로(인물을 가리지 않게).
            _box = CoastUiArt.Panel(root, "TextBox", new Color(0.05f, 0.04f, 0.07f, 0.66f), 22);
            _box.raycastTarget = false;
            PlaceBox(true);
            var edge = CoastUiArt.Panel(_box.transform, "Edge", new Color(0.83f, 0.69f, 0.22f, 0.55f), 22);
            edge.raycastTarget = false;
            CoastOrnate.Stretch(edge.rectTransform, 0f, 0f, 0f, 0f);
            var inner = CoastUiArt.Panel(edge.transform, "Inner", new Color(0.05f, 0.04f, 0.07f, 1f), 20);
            inner.raycastTarget = false;
            CoastOrnate.Stretch(inner.rectTransform, 2f, 2f, -2f, -2f);
            inner.color = new Color(0.05f, 0.04f, 0.07f, 0.72f);
            _box.color = new Color(0f, 0f, 0f, 0f);
            _body = CoastOrnate.Label(_box.transform, "Body", "", 24, CoastOrnate.Ivory, TextAnchor.UpperLeft);
            CoastOrnate.Stretch(_body.rectTransform, 28f, 22f, -28f, -46f);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _body.lineSpacing = 1.25f;
            CoastUiArt.OutlineText(_body, new Color(0f, 0f, 0f, 0.55f), 1f);

            _namePlate = CoastUiArt.Panel(_box.transform, "NamePlate", CoastOrnate.WoodDark, 12);
            var nrt = _namePlate.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.anchoredPosition = new Vector2(22f, 0f);
            nrt.sizeDelta = new Vector2(150f, 44f);
            _namePlate.raycastTarget = false;
            _nameTag = CoastOrnate.Label(_namePlate.transform, "Name", "", 22, CoastOrnate.GoldLight);
            CoastUiArt.OutlineText(_nameTag, new Color(0f, 0f, 0f, 0.4f), 1.2f);

            _cursor = CoastOrnate.Label(_box.transform, "Cursor", "▼", 20, CoastOrnate.GoldLight);
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
            _titleText.supportRichText = true;
            CoastUiArt.OutlineText(_titleText, new Color(0.83f, 0.69f, 0.22f, 0.9f), 1.5f);
            tgo.SetActive(false);
        }

        /// 텍스트 창 위치. top=true 면 화면 위(인물이 아래쪽에 크게 있는 그림), 아니면 아래.
        private void PlaceBox(bool top)
        {
            var rt = _box.rectTransform;
            // 위: SKIP 버튼(우상단 44px) 아래부터. 아래: 홈 제스처 영역 위.
            if (top) { rt.anchorMin = new Vector2(0f, 0.715f); rt.anchorMax = new Vector2(1f, 0.935f); }
            else { rt.anchorMin = new Vector2(0f, 0.03f); rt.anchorMax = new Vector2(1f, 0.25f); }
            rt.offsetMin = new Vector2(12f, 0f);
            rt.offsetMax = new Vector2(-12f, 0f);
            if (_namePlate != null)
            {
                // 이름표는 항상 창의 위 테두리에 걸친다
                var nrt = _namePlate.rectTransform;
                nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 1f);
                nrt.anchoredPosition = new Vector2(22f, 0f);
            }
        }

        /// 대본 힌트: 새 컷씬 그림은 인물이 아래쪽에 크게 있어서 기본이 '위'. BG 의 D칸 / CG 의 C칸에 '아래' 또는 'bottom' 이 있으면 아래로.
        private static bool WantsTop(string hint) => string.IsNullOrEmpty(hint) || !(hint.Contains("아래") || hint.ToLowerInvariant().Contains("bottom"));

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
            fit.aspectRatio = 9f / 16f;
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
            yield return null;   // 씬을 연 그 탭/키가 첫 프레임에서 '진행'으로 읽히지 않게
            _advance = false;
            // 6차: 씬 앞 짧은 영상(Resources/CoastRun/Video/VID_<씬>) — 페이더 위, 타이틀 카드 아래
            var clip = StoryVideo.ClipFor(_sceneId);
            if (clip != null)
            {
                var host = new GameObject("VideoHost", typeof(RectTransform));
                host.transform.SetParent(_fader.transform.parent, false);
                host.transform.SetSiblingIndex(_fader.transform.GetSiblingIndex() + 1);
                var hr = host.GetComponent<RectTransform>();
                hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one;
                hr.offsetMin = new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad);
                hr.offsetMax = new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad);
                yield return StoryVideo.Play(clip, hr, Pressed, () => _skip);
                UnityEngine.Object.Destroy(host);
                _advance = false;
                _skip = false;   // 영상만 건너뛴 것 — 본편은 이어서
            }
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
                string txt = Loc.IsKo ? line.B : Loc.Tr(ChapterScript.TextEn(_sceneId, i) ?? line.B);
                switch (line.Kind)
                {
                    case "BG":
                        yield return ShowBg(line);
                        break;
                    case "CG":
                        yield return ShowCg(line);
                        break;
                    case "SAY":
                        yield return Say(line.A, txt);
                        break;
                    case "NARR":
                        yield return Say("", txt);
                        break;
                    case "LETTER":
                        yield return Say("", txt, letter: true);
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
            // 변형(눈 등) 전용 그림이 있으면 그걸 쓰고 틴트는 생략
            bool snow = !string.IsNullOrEmpty(line.D) && line.D.Contains("눈");
            var tex = snow ? ArtAssets.LoadTexture("BG_" + line.A + "_SNOW") : null;
            bool dedicated = tex != null;
            if (tex == null) tex = ArtAssets.LoadTexture("BG_" + line.A);
            if (tex != null)
            {
                _bg.sprite = CoastUiArt.AsSprite(tex, 100f);
                _bg.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;
                _bg.color = dedicated ? Color.white : Tint(line.D);
                _bg.gameObject.SetActive(true);
            }
            else
            {
                _bg.sprite = null;
                _bg.color = new Color(0.16f, 0.14f, 0.18f, 1f);
            }
            PlaceBox(WantsTop(line.D));
            // 새 컷씬 그림은 인물이 그려진 풀 일러스트 → 스탠딩은 쓰지 않는다(그림이 없을 때만 대체로).
            if (UseStandings || tex == null) { SetStanding(_standL, line.B, out _curL); SetStanding(_standR, line.C, out _curR); }
            else { _standL.gameObject.SetActive(false); _standR.gameObject.SetActive(false); _curL = line.B; _curR = line.C; }
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

        private IEnumerator ShowCg(VnLine line)
        {
            string id = line.A;
            var tex = ArtAssets.LoadTexture("Cut_" + id);
            yield return FadeImage(_fader, 1f, 0.25f, true);
            PlaceBox(WantsTop(line.C));
            _standL.gameObject.SetActive(false);
            _standR.gameObject.SetActive(false);
            if (tex != null)
            {
                _cg.sprite = CoastUiArt.AsSprite(tex, 100f);
                _cg.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;
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
            _nameTag.text = Loc.IsKo ? speaker : Loc.Tr(ChapterScript.SpeakerEn(speaker));
            _body.text = body;
            _body.fontStyle = hasName ? FontStyle.Normal : FontStyle.Italic;
            _body.color = hasName ? CoastOrnate.Ivory : new Color(0.93f, 0.90f, 0.84f, 0.92f);
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
