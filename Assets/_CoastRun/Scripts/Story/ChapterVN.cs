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

        /// 씬이 끝난 뒤 다른 씬(러너)으로 넘어가는 경우 — 넘어가는 동안 육성 화면이 비치지 않게 검정을 붙잡아 둔다.
        public static bool HoldBlackOnNext;

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
            HoldBlackOnNext = true;   // 오프닝 뒤에는 런 씬으로 넘어간다
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
        private RectTransform _fxLayer;   // 8차: 계절 파티클(꽃잎·반딧불·낙엽·눈)
        private float _kenT;               // 8차: 켄번즈(느린 줌)
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
        private bool _bgShown;   // 37차: 빈 컷(sprite 없음)도 두 번째부터는 암전 전환

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
            StartCoroutine(Ambient());
        }

        // ── 8차: 감정 연출 — 켄번즈 + 계절 파티클 ─────────────────────────
        enum FxKind { None, Petal, Firefly, Leaf, Snow }
        FxKind FxFor(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "PRO") return FxKind.None;
            if (id.StartsWith("END_B")) return FxKind.Snow;
            if (id.StartsWith("END_TRUE") || id.StartsWith("END_A")) return FxKind.Petal;
            var m = System.Text.RegularExpressions.Regex.Match(id, @"CH(\d+)");
            if (m.Success)
            {
                int ch = int.Parse(m.Groups[1].Value);
                if (ch <= 5) return FxKind.Petal;
                if (ch <= 10) return FxKind.Firefly;
                if (ch <= 15) return FxKind.Leaf;
                return FxKind.Snow;
            }
            if (id.StartsWith("SIDE_")) return FxKind.Firefly;
            return FxKind.None;
        }

        IEnumerator Ambient()
        {
            var kind = FxFor(_sceneId);
            var sprite = CoastUiArt.RoundedRect(6);
            var pool = new System.Collections.Generic.List<(RectTransform rt, Image img, float vx, float vy, float phase, float life)>();
            int max = kind == FxKind.Snow ? 46 : kind == FxKind.Firefly ? 22 : kind == FxKind.None ? 0 : 26;
            float w = 760f, h = 1340f;
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                // 켄번즈: 12초 주기로 1.00 ↔ 1.06 — 그림이 숨을 쉰다
                _kenT += dt / 12f;
                float k = 1f + 0.06f * (0.5f - 0.5f * Mathf.Cos(_kenT * Mathf.PI));
                if (_bg != null) _bg.rectTransform.localScale = new Vector3(k, k, 1f);
                if (_cg != null) _cg.rectTransform.localScale = new Vector3(k, k, 1f);

                if (kind != FxKind.None && _fxLayer != null)
                {
                    if (pool.Count < max && UnityEngine.Random.value < 0.35f)
                    {
                        var go = new GameObject("p", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(_fxLayer, false);
                        var rt = go.GetComponent<RectTransform>(); var img = go.GetComponent<Image>();
                        img.sprite = sprite; img.type = Image.Type.Sliced; img.raycastTarget = false;
                        float size, vx, vy; Color c;
                        switch (kind)
                        {
                            case FxKind.Petal: size = UnityEngine.Random.Range(7f, 13f); c = new Color(1f, 0.92f, 0.45f, 0.85f); vx = UnityEngine.Random.Range(-30f, 50f); vy = UnityEngine.Random.Range(-70f, -35f); break;
                            case FxKind.Firefly: size = UnityEngine.Random.Range(4f, 8f); c = new Color(1f, 0.85f, 0.45f, 0.9f); vx = UnityEngine.Random.Range(-20f, 20f); vy = UnityEngine.Random.Range(12f, 40f); break;
                            case FxKind.Leaf: size = UnityEngine.Random.Range(9f, 15f); c = new Color(0.95f, 0.55f, 0.25f, 0.9f); vx = UnityEngine.Random.Range(-60f, 30f); vy = UnityEngine.Random.Range(-90f, -50f); break;
                            default: size = UnityEngine.Random.Range(5f, 10f); c = new Color(1f, 1f, 1f, 0.9f); vx = UnityEngine.Random.Range(-15f, 15f); vy = UnityEngine.Random.Range(-60f, -30f); break;
                        }
                        rt.sizeDelta = new Vector2(size, kind == FxKind.Petal ? size * 0.6f : size);
                        img.color = c;
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-w * 0.5f, w * 0.5f), kind == FxKind.Firefly ? UnityEngine.Random.Range(-h * 0.5f, 0f) : h * 0.5f + 20f);
                        rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                        pool.Add((rt, img, vx, vy, UnityEngine.Random.Range(0f, 6.28f), 0f));
                    }
                    for (int i = pool.Count - 1; i >= 0; i--)
                    {
                        var p2 = pool[i];
                        if (p2.rt == null) { pool.RemoveAt(i); continue; }
                        float life = p2.life + dt;
                        var pos = p2.rt.anchoredPosition;
                        pos.x += (p2.vx + Mathf.Sin(life * 1.7f + p2.phase) * 22f) * dt;
                        pos.y += p2.vy * dt;
                        p2.rt.anchoredPosition = pos;
                        if (kind == FxKind.Leaf || kind == FxKind.Petal) p2.rt.localRotation = Quaternion.Euler(0f, 0f, life * 90f + p2.phase * 40f);
                        if (kind == FxKind.Firefly) { var c2 = p2.img.color; c2.a = 0.35f + 0.55f * (0.5f + 0.5f * Mathf.Sin(life * 3f + p2.phase)); p2.img.color = c2; }
                        bool dead = pos.y < -h * 0.5f - 30f || pos.y > h * 0.5f + 30f || Mathf.Abs(pos.x) > w * 0.5f + 40f || life > 14f;
                        if (dead) { UnityEngine.Object.Destroy(p2.rt.gameObject); pool.RemoveAt(i); }
                        else pool[i] = (p2.rt, p2.img, p2.vx, p2.vy, p2.phase, life);
                    }
                }
                yield return null;
            }
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
            var fxGo = new GameObject("Fx", typeof(RectTransform));
            fxGo.transform.SetParent(_artArea, false);
            _fxLayer = fxGo.GetComponent<RectTransform>();
            _fxLayer.anchorMin = Vector2.zero; _fxLayer.anchorMax = Vector2.one; _fxLayer.offsetMin = Vector2.zero; _fxLayer.offsetMax = Vector2.zero;

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
            CoastOrnate.Stretch(_body.rectTransform, 28f, 26f, -28f, -50f);
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            _body.lineSpacing = 1.3f;
            CoastUiArt.OutlineText(_body, new Color(0f, 0f, 0f, 0.55f), 1f);

            _namePlate = CoastUiArt.Panel(_box.transform, "NamePlate", CoastOrnate.WoodDark, 12);
            var nrt = _namePlate.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.anchoredPosition = new Vector2(22f, 0f);
            nrt.sizeDelta = new Vector2(176f, 50f);
            _namePlate.raycastTarget = false;
            _nameTag = CoastOrnate.Label(_namePlate.transform, "Name", "", 24, CoastOrnate.GoldLight);
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
            if (top) { rt.anchorMin = new Vector2(0f, 0.65f); rt.anchorMax = new Vector2(1f, 0.935f); }
            else { rt.anchorMin = new Vector2(0f, 0.03f); rt.anchorMax = new Vector2(1f, 0.315f); }
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
                // 6차: 조건 태그 — SAY는 화자 칸, NARR/LETTER는 본문 앞 [조건]
                string speaker = line.A;
                bool pass = true;
                if (line.Kind == "SAY") speaker = StoryCond.Strip(line.A, out pass);
                else if (line.Kind == "NARR" || line.Kind == "LETTER")
                {
                    StoryCond.Strip(line.B, out pass);
                    txt = StoryCond.Strip(txt, out _);
                }
                if (!pass) continue;
                switch (line.Kind)
                {
                    case "BG":
                        yield return ShowBg(line);
                        break;
                    case "CG":
                        yield return ShowCg(line);
                        break;
                    case "SAY":
                        yield return Say(speaker, txt);
                        break;
                    case "NARR":
                        yield return Say("", txt);
                        break;
                    case "LETTER":
                        yield return Say("", txt, letter: true);
                        break;
                    case "BGM":
                        // 37차: 음악 큐 — 곡키(M1~M7 / 정지) | 볼륨 | 피치. 대본 파일의 'BGM | M6 | 0.6' 줄.
                        VnMusic.Cue(line.A, line.C, line.D);
                        break;
                }
            }

            VnMusic.Stop(0.8f);
            yield return FadeImage(_fader, 1f, 0.3f, true);
            Finish();
        }

        private IEnumerator ShowBg(VnLine line)
        {
            // 짧은 암전 후 배경 교체
            bool first = !_bgShown && !_cg.gameObject.activeSelf && _fader.color.a > 0.99f;
            _bgShown = true;
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
                // 37차: 그림이 없는 컷(BG | Blank) — 변형 칸의 분위기(세피아/비/밤/흰)로 단색만 깐다.
                _bg.sprite = null;
                _bg.GetComponent<AspectRatioFitter>().aspectRatio = 9f / 16f;
                _bg.color = BlankColor(line.D);
                _bg.gameObject.SetActive(true);
            }
            PlaceBox(WantsTop(line.D));
            // 새 컷씬 그림은 인물이 그려진 풀 일러스트 → 스탠딩은 쓰지 않는다(그림이 없을 때만 대체로).
            if (UseStandings || tex == null) { SetStanding(_standL, line.B, out _curL); SetStanding(_standR, line.C, out _curR); }
            else { _standL.gameObject.SetActive(false); _standR.gameObject.SetActive(false); _curL = line.B; _curR = line.C; }
            SetText("", "", false);
            yield return FadeImage(_fader, 0f, 0.3f, false);
        }

        /// 37차: 빈 컷 배경색. 회상=세피아, 비=청회색, 밤=검푸름, 흰=흰 화면, 그 외(현재)=어두운 보라회색.
        private static Color BlankColor(string variant)
        {
            if (string.IsNullOrEmpty(variant)) return new Color(0.16f, 0.14f, 0.18f, 1f);
            if (variant.Contains("흰")) return new Color(0.93f, 0.92f, 0.89f, 1f);
            if (variant.Contains("세피아") && variant.Contains("비")) return new Color(0.22f, 0.20f, 0.17f, 1f);
            if (variant.Contains("세피아")) return new Color(0.31f, 0.25f, 0.17f, 1f);
            if (variant.Contains("비")) return new Color(0.12f, 0.15f, 0.20f, 1f);
            if (variant.Contains("밤")) return new Color(0.07f, 0.07f, 0.12f, 1f);
            return new Color(0.16f, 0.14f, 0.18f, 1f);
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
            _body.fontSize = letter ? 38 : (hasName ? 34 : 32);   // 11차: 갤럭시 S 기준 가독성 — 창도 같이 키움(PlaceBox)
            _cursor.gameObject.SetActive(false);
        }

        /// 탭/클릭/스페이스/엔터 한 번 = 진행. 1초 길게 누르면 스킵.
        private bool Pressed()
        {
            bool down = _advance || Input.GetMouseButtonDown(0) || CoastRemoteKeys.Down(KeyCode.Space) || CoastRemoteKeys.Down(KeyCode.Return) ||   // 26차: 원격(MCP) 키도 진행
                        (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.touchCount > 0;
            _holdTimer = held ? _holdTimer + Time.unscaledDeltaTime : 0f;
            if (_holdTimer > 1.2f) { _skip = true; _holdTimer = 0f; }
            if (CoastRemoteKeys.Down(KeyCode.S)) _skip = true;
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
            VnMusic.Stop(0.8f);
            RecordTable.OnSceneWatched(_sceneId);   // 37차: 롱컷을 보면 레코드 해금
            var cb = _onDone;
            _onDone = null;
            if (HoldBlackOnNext && _canvas != null)
            {
                // 8차: 캔버스를 바로 지우지 않고 검정만 남겨 씬이 바뀔 때까지 붙잡는다(육성 화면 재노출 방지).
                HoldBlackOnNext = false;
                var hold = _canvas.gameObject.AddComponent<VnBlackHold>();
                hold.fader = _fader;
                _canvas = null;
            }
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            if (_active == this) _active = null;
            UnityEngine.Object.Destroy(gameObject);
            cb?.Invoke();
        }
    }

    /// 37차: 컷씬 음악 — 대본의 BGM 줄을 재생한다. 씬 위에 남는 별도 오브젝트(크로스페이드용 2소스).
    public static class VnMusic
    {
        private static VnMusicPlayer _p;

        /// 곡키: M1~M7 (+ 접미사 s=느리게 r=라디오 w=수중), '정지'/'stop'/'∅'/'0'/'무음' = 페이드아웃.
        public static void Cue(string key, string volCell, string pitchCell)
        {
            key = (key ?? "").Trim();
            if (key.Length == 0 || key == "정지" || key == "무음" || key == "∅" || key == "0" || key.Equals("stop", StringComparison.OrdinalIgnoreCase))
            { Stop(0.7f); return; }
            float vol = 0.7f, pitch = 1f;
            string k = key.ToUpperInvariant();
            // 접미사 변주 — 실제 파일은 BGM_M4 하나. s: 0.72배속(대본 지시) r: 차 안 라디오(작게) w: 물속(작게·낮게)
            if (k.EndsWith("S")) { k = k.Substring(0, k.Length - 1); pitch = 0.72f; vol = 0.6f; }
            else if (k.EndsWith("R")) { k = k.Substring(0, k.Length - 1); vol = 0.4f; }
            else if (k.EndsWith("W")) { k = k.Substring(0, k.Length - 1); vol = 0.45f; pitch = 0.9f; }
            if (float.TryParse(volCell, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) vol = Mathf.Clamp01(v);
            if (float.TryParse(pitchCell, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pt)) pitch = Mathf.Clamp(pt, 0.5f, 1.5f);
            var clip = CoastBgmLibrary.Load("BGM_" + k);
            if (clip == null) { Debug.LogWarning("[VnMusic] 곡 없음: " + key); Stop(0.5f); return; }
            Ensure().Play(clip, vol, pitch, k);   // 전체 볼륨은 AudioListener(CoastPrefs.VolumeStep)가 맡는다
        }

        public static void Stop(float fade)
        {
            if (_p != null) _p.FadeOut(fade);
        }

        private static VnMusicPlayer Ensure()
        {
            if (_p != null) return _p;
            var go = new GameObject("VnMusic");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _p = go.AddComponent<VnMusicPlayer>();
            return _p;
        }
    }

    public class VnMusicPlayer : MonoBehaviour
    {
        private AudioSource _a, _b;   // _a = 현재, _b = 물러나는 곡
        private string _key;
        private float _target;
        private Coroutine _fade;

        public void Play(AudioClip clip, float vol, float pitch, string key)
        {
            if (_a == null) { _a = Make("A"); _b = Make("B"); }
            if (_key == key && _a.clip == clip && _a.isPlaying)
            {
                // 같은 곡 — 볼륨·피치만 바꾼다(끊기지 않게)
                _target = vol; _a.pitch = pitch;
                if (_fade != null) StopCoroutine(_fade);
                _fade = StartCoroutine(FadeTo(_a, vol, 0.6f, false));
                return;
            }
            // 크로스페이드: 현재 곡을 B로 넘겨 페이드아웃, A에 새 곡
            var t = _a; _a = _b; _b = t;
            if (_b.isPlaying) StartCoroutine(FadeTo(_b, 0f, 0.8f, true));
            _a.clip = clip; _a.pitch = pitch; _a.volume = 0f; _a.loop = true; _a.Play();
            _key = key; _target = vol;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeTo(_a, vol, 0.9f, false));
        }

        public void FadeOut(float dur)
        {
            _key = null;
            if (_a != null && _a.isPlaying) StartCoroutine(FadeTo(_a, 0f, dur, true));
            if (_b != null && _b.isPlaying) StartCoroutine(FadeTo(_b, 0f, dur, true));
        }

        private AudioSource Make(string n)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0f; s.loop = true;
            return s;
        }

        private System.Collections.IEnumerator FadeTo(AudioSource s, float to, float dur, bool stopAfter)
        {
            float from = s.volume, t = 0f;
            while (t < dur && s != null)
            {
                t += Time.unscaledDeltaTime;
                s.volume = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            if (s == null) yield break;
            s.volume = to;
            if (stopAfter && to <= 0.001f) s.Stop();
        }
    }

    /// 씬 전환이 끝날 때까지 검정을 유지했다가 걷는다.
    public class VnBlackHold : MonoBehaviour
    {
        public Image fader;
        IEnumerator Start()
        {
            foreach (var g in GetComponentsInChildren<Graphic>(true))
                if (g.name != "Black" && g.name != "Fader") g.enabled = false;
            if (fader != null) fader.color = Color.black;
            var from = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            float t = 0f;
            while (t < 4f && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == from) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.25f);
            var imgs = new System.Collections.Generic.List<Image>();
            foreach (var im in GetComponentsInChildren<Image>(true)) if (im.enabled) imgs.Add(im);
            float f = 0f;
            while (f < 0.45f)
            {
                f += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(f / 0.45f);
                foreach (var im in imgs) { var c = im.color; c.a = a; im.color = c; }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
