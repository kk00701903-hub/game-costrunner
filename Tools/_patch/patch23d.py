# 23차 patch D: 육성 페이지 메인 톤 + 집사 꼬마 + 움직이는 주인공(6·7·8), 피버(9) 연결
import io, sys, re
ROOT = 'Assets/_CoastRun/Scripts/'
def rw(path, pairs):
    p = ROOT + path
    s = io.open(p, encoding='utf-8').read()
    for old, new in pairs:
        if old not in s:
            print('MISSING in', path, ':', old[:90]); sys.exit(1)
        s = s.replace(old, new, 1)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
    print('ok', path)

# ── RaisingUI ────────────────────────────────────────────────────
rw('Raising/RaisingUI.cs', [
# 팔레트: 우드/골드 → 메인페이지(노을·유채꽃·크림) 톤
('''        private static readonly Color Ivory = Hex("#FFF8E7");
        private static readonly Color Wood = Hex("#8D6E63");
        private static readonly Color WoodDark = Hex("#4E342E");
        private static readonly Color Gold = Hex("#D4AF37");
        private static readonly Color GoldLight = Hex("#F1D37A");''',
'''        // 23차-6: 메인페이지(노을 유채밭 키아트) 톤 — 크림 종이 + 코랄/살구 테두리 + 저녁 보라 HUD. (우드·골드 이름은 호출부 호환용으로 유지)
        private static readonly Color Ivory = Hex("#FFF8E7");
        private static readonly Color Wood = Hex("#F6D8B0");        // 살구 크림(알약·프레임 바탕)
        private static readonly Color WoodDark = Hex("#4A2F55");    // 저녁 보라(HUD 바)
        private static readonly Color Gold = Hex("#FF8A65");        // 노을 코랄(프레임 테두리)
        private static readonly Color GoldLight = Hex("#FFD180");'''),
('''        private static readonly Color Navy = Hex("#3E2723");
        private static readonly Color Ink = Hex("#4E342E");''',
'''        private static readonly Color Navy = Hex("#3B2A4A");
        private static readonly Color Ink = Hex("#4A3550");'''),
# 배경: 타이틀 키아트(블러) + 반투명 크림 종이
('''            var bg = CoastHudLayout.MakeImage(_root, "Background", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), Wood);
            bg.transform.SetAsFirstSibling();
            var paper = CoastHudLayout.MakeImage(_root, "Paper", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad + 10f, -CoastUiCanvas.HudPad + 10f), new Vector2(CoastUiCanvas.HudPad - 10f, CoastUiCanvas.HudPad - 10f), Ivory);''',
'''            var bg = CoastHudLayout.MakeImage(_root, "Background", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad, -CoastUiCanvas.HudPad), new Vector2(CoastUiCanvas.HudPad, CoastUiCanvas.HudPad), Wood);
            bg.transform.SetAsFirstSibling();
            // 23차-6: 메인페이지 키아트를 흐리게 깔아 같은 세계로 읽히게(노을 하늘이 위, 유채꽃이 아래)
            var backdropTex = ArtAssets.LoadTexture("UI_Raising_Backdrop") ?? ArtAssets.LoadTexture("UI_Title_Gate");
            if (backdropTex != null)
            {
                bg.sprite = CoastUiArt.AsSprite(backdropTex); bg.color = Color.white; bg.preserveAspect = false;
            }
            var paper = CoastHudLayout.MakeImage(_root, "Paper", Vector2.zero, Vector2.one,
                new Vector2(-CoastUiCanvas.HudPad + 10f, -CoastUiCanvas.HudPad + 10f), new Vector2(CoastUiCanvas.HudPad - 10f, CoastUiCanvas.HudPad - 10f), new Color(Ivory.r, Ivory.g, Ivory.b, 0.42f));'''),
# 방: 집사 + 주인공 애니 훅
('''            _charFace = Label(_charRoot, "Face", "", 64, Navy);
            Place(_charFace.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 100f), new Vector2(0.5f, 0.5f));''',
'''            _charFace = Label(_charRoot, "Face", "", 64, Navy);
            Place(_charFace.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 100f), new Vector2(0.5f, 0.5f));
            _charRoot.pivot = new Vector2(0.5f, 0f);   // 23차-8: 발끝 기준으로 숨쉬기·흔들림
            // 23차-7: 집사 꼬마(왼쪽 아래) — 상황별 조언, 탭하면 다음 조언
            _butler = new RaisingButler(host, () => { });'''),
# Update: 틱
('''        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.GetKeyDown(KeyCode.Space))
                _tapped = true;''',
'''        private RaisingButler _butler; private float _charHop; private float _charBlink = 3f; private Image _eyelid;
        /// 23차-8: 움직이는 주인공 — 숨쉬기(세로 1.6%), 좌우 흔들림(±1.3°), 살짝 떠오름, 4~6초마다 눈 깜빡임(눈 위치 띠), 탭하면 콩 뛰기.
        private void TickPortrait(float dt)
        {
            if (_charRoot == null || !_charRoot.gameObject.activeInHierarchy) return;
            float t = Time.unscaledTime;
            float breathe = 1f + Mathf.Sin(t * 1.9f) * 0.016f;
            float sway = Mathf.Sin(t * 0.7f) * 1.3f;
            _charHop = Mathf.MoveTowards(_charHop, 0f, dt * 2.6f);
            float hop = Mathf.Sin(Mathf.Clamp01(_charHop) * Mathf.PI) * 26f;
            bool burnout = Save != null && Save.stats.Burnout;
            _charRoot.localScale = new Vector3((1f / breathe) * (_charMoodScale), breathe * _charMoodScale, 1f);
            _charRoot.localRotation = Quaternion.Euler(0f, 0f, sway + (burnout ? -4f : 0f));
            _charRoot.anchoredPosition = new Vector2(Mathf.Sin(t * 0.7f) * 3f, 18f + hop + Mathf.Sin(t * 1.9f) * 2f);
            // 깜빡임: 눈 높이에 살구색 얇은 띠가 0.12초 스쳐 지나간다(그림 위 살구 톤이라 눈을 감은 듯 읽힌다)
            _charBlink -= dt;
            if (_charBlink <= 0f) { _charBlink = UnityEngine.Random.Range(3.5f, 6.5f); StartCoroutine(Blink()); }
        }
        private float _charMoodScale = 1f;
        private System.Collections.IEnumerator Blink()
        {
            if (_charImage == null || !_charImage.enabled) yield break;
            if (_eyelid == null)
            {
                _eyelid = CoastHudLayout.MakeImage(_charRoot, "Eyelid", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-68f, -132f), new Vector2(68f, -108f), new Color(0.98f, 0.86f, 0.76f, 0.9f));
                _eyelid.sprite = CoastUiArt.RoundedRect(8); _eyelid.type = Image.Type.Sliced; _eyelid.raycastTarget = false;
            }
            _eyelid.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.11f);
            if (_eyelid != null) _eyelid.gameObject.SetActive(false);
        }

        private void Update()
        {
            float udt = Time.unscaledDeltaTime;
            _butler?.Tick(udt);
            TickPortrait(udt);
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.GetKeyDown(KeyCode.Space))
                _tapped = true;'''),
# RefreshCharacter: 스케일을 애니 변수로, 회전은 틱이 맡는다
('''            _charRoot.localRotation = Quaternion.Euler(0f, 0f, st.Burnout ? -4f : 0f);
            _charRoot.localScale = mood == Mood.Great ? Vector3.one * 1.04f : Vector3.one;''',
'''            _charMoodScale = mood == Mood.Great ? 1.04f : 1f;   // 23차-8: 회전·스케일은 TickPortrait가 매 프레임 적용'''),
# 탭 → 콩 뛰기
('''        private void OnCharacterTapped()
        {
            if (_busy || Save == null) return;''',
'''        private void OnCharacterTapped()
        {
            if (_busy || Save == null) return;
            _charHop = 1f;'''),
# Refresh → 집사 조언 갱신
('''            ApplySeasonRoom(season);
            RefreshSlots();
            RefreshStats();
            RefreshCharacter();''',
'''            ApplySeasonRoom(season);
            RefreshSlots();
            RefreshStats();
            RefreshCharacter();
            _butler?.Refresh(s, s.week <= 1 && s.phaseIndex == 0 && !s.HasQueuedSchedule);'''),
])

# ── 피버: 코인·말랑이 자석 + JuiceDirector 연출 + PickupFloat 배너 + StageManager에서 생성 ──
rw('Economy/CoinPickup.cs', [
('''            float magnet = _upgrades.GetMagnetRadius() + PetCompanion.MagnetBonus;
            if (magnet <= 0.05f)
                return;''',
'''            float magnet = _upgrades.GetMagnetRadius() + PetCompanion.MagnetBonus + FeverMode.MagnetBonus;   // 23차-9: 피버 3초 동안 전부 빨아들인다
            if (magnet <= 0.05f)
                return;'''),
('''            _magnetT += Time.deltaTime * 2.4f;''',
'''            _magnetT += Time.deltaTime * (FeverMode.Active ? 4.5f : 2.4f);'''),
])
rw('Economy/JellyPickup.cs', [
('''            if (BonusTimeDirector.IsActive)
                magnet += 1.5f;''',
'''            if (BonusTimeDirector.IsActive)
                magnet += 1.5f;
            magnet += FeverMode.MagnetBonus;   // 23차-9'''),
('''            _magnetT += Time.deltaTime * 3f;''',
'''            _magnetT += Time.deltaTime * (FeverMode.Active ? 5f : 3f);'''),
])
rw('Visual/JuiceDirector.cs', [
('''        /// 23차-3: 골인 콘페티''',
'''        /// 23차-9: 피버 시작/끝 — 채도 킥 + 속도선 + FOV, 끝나면 잔잔히.
        private Coroutine _feverLines;
        public void OnFeverStart()
        {
            cameraRig?.Shake(0.15f, 0.15f);
            cameraRig?.FovKick(+7f, 0.4f);
            PunchSaturation(+35f, 0.5f);
            audio?.PlaySfx(CoastSfx.NearMiss);
            if (_feverLines != null) StopCoroutine(_feverLines);
            _feverLines = StartCoroutine(FeverLines());
            SpawnCoinBurst((player != null ? player.transform.position : Vector3.zero) + Vector3.up * 1f, new Color(1f, 0.85f, 0.25f), 24);
        }
        public void OnFeverEnd()
        {
            if (_feverLines != null) { StopCoroutine(_feverLines); _feverLines = null; }
            audio?.PlaySfx(CoastSfx.Coin);
        }
        private IEnumerator FeverLines()
        {
            while (FeverMode.Active) { speedLines?.Burst(14); yield return new WaitForSeconds(0.12f); }
        }

        /// 23차-3: 골인 콘페티'''),
])
rw('Visual/PickupFloat.cs', [
('''        // 23차-2: 꽈당 — 붉은 비네트 플래시 + 화면 기울기 + 큰 글자''',
'''        // 23차-9: 화면 위쪽 배너(FEVER!) — 지속 시간 동안 흔들리며 떠 있다가 사라진다
        private Text _bannerText;
        public static void Banner(string text, Color color, float seconds)
        {
            var f = Ensure();
            f.StartCoroutine(f.BannerSeq(text, color, seconds));
        }
        private IEnumerator BannerSeq(string text, Color color, float seconds)
        {
            if (_bannerText == null)
            {
                _bannerText = CoastHudLayout.MakeText(_root, "Banner", "", 96, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400f, -330f), new Vector2(400f, -200f));
                _bannerText.fontStyle = FontStyle.Bold; _bannerText.raycastTarget = false;
                CoastUiArt.OutlineText(_bannerText, new Color(0.35f, 0.12f, 0.02f, 1f), 4f);
            }
            _bannerText.gameObject.SetActive(true);
            _bannerText.text = text;
            var rt = _bannerText.rectTransform;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float u = t / seconds;
                float pop = t < 0.15f ? Mathf.Lerp(1.8f, 1f, t / 0.15f) : 1f + Mathf.Sin(t * 9f) * 0.05f;
                rt.localScale = Vector3.one * pop;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 4f);
                var c = Color.Lerp(color, Color.white, (Mathf.Sin(t * 14f) + 1f) * 0.25f);
                c.a = u > 0.85f ? 1f - (u - 0.85f) / 0.15f : 1f;
                _bannerText.color = c;
                yield return null;
            }
            _bannerText.gameObject.SetActive(false);
        }

        // 23차-2: 꽈당 — 붉은 비네트 플래시 + 화면 기울기 + 큰 글자'''),
])
rw('Core/StageManager.cs', [
('''            MonochromeWorld.Arm(ChapterIndex);   // 19차-1: 20장은 10초 뒤 세상이 흑백''',
'''            MonochromeWorld.Arm(ChapterIndex);   // 19차-1: 20장은 10초 뒤 세상이 흑백
            if (!ArcadeRun.Active) FeverMode.Ensure();   // 23차-9: 꼬마 도움 버튼 → 3초 피버'''),
])
print('ALL OK')
