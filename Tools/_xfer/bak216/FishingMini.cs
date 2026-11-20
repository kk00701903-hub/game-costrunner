using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 136차: 해변 낚시 미니게임 — 던지기 → 입질(!) 때 당기기 → 릴 감기(초록 칸을 잡고 물고기 마커를 따라가기) → 결과.
    /// 잡은 물고기는 재료 「생선」(LifeItems ing_fish)과 코인이 된다. 한 페이즈에 5번.
    public class FishingMini : MonoBehaviour
    {
        public static int RodTier;
        public static bool DevAuto;   // 198차 테스트용: 입질·릴 자동   // 198차: 낚싯대 등급 — 귀한 물고기 확률↑
        public const int CastsPerPhase = 5;
        struct Fish { public string ko, en; public int coins; public float weight, hard; public Color col; }
        static readonly Fish[] Table = {
            new Fish { ko = "멸치", en = "Anchovy", coins = 40, weight = 34f, hard = 0.55f, col = new Color(0.75f, 0.80f, 0.88f) },
            new Fish { ko = "고등어", en = "Mackerel", coins = 80, weight = 28f, hard = 0.75f, col = new Color(0.35f, 0.55f, 0.80f) },
            new Fish { ko = "옥돔", en = "Tilefish", coins = 160, weight = 18f, hard = 0.95f, col = new Color(0.95f, 0.55f, 0.45f) },
            new Fish { ko = "갈치", en = "Hairtail", coins = 220, weight = 12f, hard = 1.15f, col = new Color(0.85f, 0.88f, 0.95f) },
            new Fish { ko = "다금바리", en = "Grouper", coins = 500, weight = 5f, hard = 1.45f, col = new Color(0.45f, 0.40f, 0.30f) },
            new Fish { ko = "낡은 장화", en = "Old boot", coins = 5, weight = 8f, hard = 0.4f, col = new Color(0.40f, 0.32f, 0.25f) },
        };

        GameManager _gm; Action _onDone;
        Canvas _canvas; RectTransform _root, _water, _bobber, _bar, _zone, _fishMk, _prog; Text _msg, _castsT, _btnT; Image _btnImg;
        Button _btn; bool _holding; int _state;   // 0 idle 1 cast 2 bite 3 reel 4 result
        float _zoneY, _zoneV, _fishY, _fishT, _progress; Fish _cur; Coroutine _co; RectTransform _fishArt;   // 213차

        public static FishingMini Open(GameManager gm, Action onDone)
        {
            var go = new GameObject("FishingMini"); var f = go.AddComponent<FishingMini>();
            f._gm = gm; f._onDone = onDone; f.Build(); return f;
        }

        SaveData Save => _gm != null ? _gm.Save : null;
        int Stamp => Save != null ? Save.week * 4 + Save.phaseIndex : 0;
        int CastsLeft
        {
            get { if (Save == null) return 0; if (Save.villageFishStamp != Stamp) { Save.villageFishStamp = Stamp; Save.villageFishCasts = 0; } return CastsPerPhase - Save.villageFishCasts; }
        }

        void Build()
        {
            _canvas = CoastUiCanvas.Create("FishingCanvas", 140, transform);
            _root = CoastUiCanvas.Root(_canvas);
            // 바탕: 하늘→바다
            var sky = CoastHudLayout.MakeImage(_root, "Sky", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.72f, 0.88f, 0.98f));
            sky.raycastTarget = true;
            var sea = CoastUiArt.Panel(_root, "Sea", new Color(0.30f, 0.66f, 0.84f), 40); sea.raycastTarget = false;
            _water = sea.rectTransform; _water.anchorMin = new Vector2(0f, 0f); _water.anchorMax = new Vector2(1f, 0.62f); _water.offsetMin = new Vector2(-300f, -300f); _water.offsetMax = new Vector2(300f, 0f);
            // 213차(사용자: 「단점들 커버해줘」 — 212차 평가: 낚시 화면이 하늘색·파란 판 두 장뿐인 단색 2D):
            // Firefly 로 그린 부둣가 바다 그림을 깔고(하늘·바다 판은 투명하게), 잡은 물고기는 결과 때 그림으로 크게 보여 준다.
            var bgTex = Resources.Load<Texture2D>("CoastRun/Textures/Village/Tex_FishingBG");
            if (bgTex != null)
            {
                var bgGo = new GameObject("BG", typeof(RawImage)); var bgRt = bgGo.GetComponent<RectTransform>(); bgRt.SetParent(_root, false); bgRt.SetSiblingIndex(1);
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                var bgRi = bgGo.GetComponent<RawImage>(); bgRi.texture = bgTex; bgRi.raycastTarget = false;
                var fit = bgGo.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = bgTex.width / (float)bgTex.height;
                sea.color = new Color(0.30f, 0.66f, 0.84f, 0f);
            }
            else
            {
                // 그림이 없으면(생성 한도 등) 진짜 마을 바다가 비쳐 보이게 — 하늘·바다 판을 반투명으로(카메라는 VillageHub 가 바다 쪽으로 돌린다)
                sky.color = new Color(0.72f, 0.88f, 0.98f, 0.0f);
                sea.color = new Color(0.20f, 0.55f, 0.78f, 0.30f);
            }
            var fishArt = new GameObject("FishArt", typeof(RawImage)); _fishArt = fishArt.GetComponent<RectTransform>(); _fishArt.SetParent(_root, false);
            _fishArt.anchorMin = _fishArt.anchorMax = new Vector2(0.5f, 0.62f); _fishArt.sizeDelta = new Vector2(300f, 300f); _fishArt.anchoredPosition = new Vector2(0f, -170f);
            fishArt.GetComponent<RawImage>().raycastTarget = false; fishArt.SetActive(false);
            for (int i = 0; i < 7; i++)
            {
                var w = CoastUiArt.Panel(_water, "W" + i, new Color(1f, 1f, 1f, 0.16f), 12); w.raycastTarget = false;
                var wr = w.rectTransform; wr.anchorMin = new Vector2(0f, 1f); wr.anchorMax = new Vector2(0f, 1f); wr.pivot = new Vector2(0f, 1f);
                wr.anchoredPosition = new Vector2(320f + (i % 3) * 300f, -80f - i * 120f); wr.sizeDelta = new Vector2(180f + (i * 37) % 120, 14f);
                w.gameObject.AddComponent<WaveDrift>();
            }
            // 제목 알약
            var title = CoastUiArt.GlossyPill(_root, "Title", new Color(0.35f, 0.62f, 0.95f), 26, 8); title.raycastTarget = false;
            var trt = title.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 1f); trt.anchoredPosition = new Vector2(0f, -18f); trt.sizeDelta = new Vector2(360f, 66f);
            var tt = CoastHudLayout.MakeText(trt, "T", Loc.T("🎣 바닷가 낚시", "🎣 Beach fishing"), 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            tt.color = Color.white; tt.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(tt, new Color(0.1f, 0.25f, 0.5f, 0.7f), 1.8f);
            _castsT = CoastHudLayout.MakeText(_root, "Casts", "", 18, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -130f), new Vector2(0f, -92f));
            _castsT.color = new Color(0.15f, 0.30f, 0.50f); _castsT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_castsT, new Color(1f, 1f, 1f, 0.9f), 1.8f);
            // 닫기
            var close = CoastUiArt.CutePill(_root, "Close", new Color(1f, 1f, 1f, 0.92f), 20, 3);
            var crt = close.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(1f, 1f); crt.anchoredPosition = new Vector2(-12f, -20f); crt.sizeDelta = new Vector2(110f, 56f); close.raycastTarget = true;
            var ct = CoastHudLayout.MakeText(crt, "T", Loc.T("그만 ✕", "Done ✕"), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); ct.color = new Color(0.30f, 0.32f, 0.48f); ct.fontStyle = FontStyle.Bold;
            var cb = close.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None; cb.onClick.AddListener(() => { CoastPrefs.Vibrate(); Close(); });
            // 찌
            var bob = CoastUiArt.GlossyPill(_root, "Bobber", new Color(0.98f, 0.35f, 0.35f), 22, 6); bob.raycastTarget = false;
            _bobber = bob.rectTransform; _bobber.anchorMin = _bobber.anchorMax = new Vector2(0.5f, 0.62f); _bobber.sizeDelta = new Vector2(44f, 44f); _bobber.anchoredPosition = new Vector2(0f, 40f);
            var bobW = CoastUiArt.Panel(_bobber, "W", Color.white, 10); bobW.raycastTarget = false; bobW.rectTransform.anchorMin = new Vector2(0.2f, 0.5f); bobW.rectTransform.anchorMax = new Vector2(0.8f, 0.72f); bobW.rectTransform.offsetMin = bobW.rectTransform.offsetMax = Vector2.zero;
            _bobber.gameObject.SetActive(false);
            // 메시지
            _msg = CoastHudLayout.MakeText(_root, "Msg", "", 26, TextAnchor.MiddleCenter, new Vector2(0f, 0.62f), new Vector2(1f, 0.62f), new Vector2(30f, 20f), new Vector2(-30f, 110f));
            _msg.color = new Color(0.12f, 0.25f, 0.45f); _msg.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_msg, new Color(1f, 1f, 1f, 0.9f), 2.2f);   /* 213차: 그림 위에서도 읽히게 */ _msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            _msg.resizeTextForBestFit = true; _msg.resizeTextMinSize = 14; _msg.resizeTextMaxSize = CoastHudLayout.Scaled(26);
            // 릴 게이지(세로) — 오른쪽
            var barBg = CoastUiArt.CutePill(_root, "Bar", new Color(0.10f, 0.20f, 0.35f, 0.85f), 18, 3); barBg.raycastTarget = false;
            _bar = barBg.rectTransform; _bar.anchorMin = _bar.anchorMax = new Vector2(1f, 0.5f); _bar.pivot = new Vector2(1f, 0.5f); _bar.anchoredPosition = new Vector2(-30f, 60f); _bar.sizeDelta = new Vector2(90f, 560f);
            var zone = CoastUiArt.Panel(_bar, "Zone", new Color(0.45f, 0.90f, 0.55f, 0.85f), 12); zone.raycastTarget = false;
            _zone = zone.rectTransform; _zone.anchorMin = new Vector2(0.15f, 0f); _zone.anchorMax = new Vector2(0.85f, 0f); _zone.pivot = new Vector2(0.5f, 0f); _zone.sizeDelta = new Vector2(0f, 150f);
            var fm = CoastUiArt.GlossyPill(_bar, "Fish", new Color(0.98f, 0.75f, 0.30f), 16, 4); fm.raycastTarget = false;
            _fishMk = fm.rectTransform; _fishMk.anchorMin = _fishMk.anchorMax = new Vector2(0.5f, 0f); _fishMk.sizeDelta = new Vector2(44f, 34f);
            var fmT = CoastHudLayout.MakeText(_fishMk, "T", "🐟", 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var progBg = CoastUiArt.CutePill(_root, "Prog", new Color(0.10f, 0.20f, 0.35f, 0.85f), 10, 2); progBg.raycastTarget = false;
            var prt = progBg.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(1f, 0.5f); prt.pivot = new Vector2(1f, 0.5f); prt.anchoredPosition = new Vector2(-130f, 60f); prt.sizeDelta = new Vector2(26f, 560f);
            var pf = CoastUiArt.Panel(prt, "F", new Color(0.98f, 0.55f, 0.65f), 8); pf.raycastTarget = false;
            _prog = pf.rectTransform; _prog.anchorMin = new Vector2(0f, 0f); _prog.anchorMax = new Vector2(1f, 0f); _prog.pivot = new Vector2(0.5f, 0f); _prog.offsetMin = new Vector2(3f, 3f); _prog.offsetMax = new Vector2(-3f, 0f); _prog.sizeDelta = new Vector2(-6f, 0f);
            _bar.gameObject.SetActive(false); prt.gameObject.SetActive(false); _progHost = prt;
            // 큰 버튼
            var edge = CoastUiArt.CutePill(_root, "BtnE", new Color(0.85f, 0.35f, 0.45f), 30, 0); edge.raycastTarget = false;
            var ert = edge.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0f); ert.pivot = new Vector2(0.5f, 0f); ert.anchoredPosition = new Vector2(0f, 26f); ert.sizeDelta = new Vector2(520f, 110f);
            _btnImg = CoastUiArt.GlossyPill(_root, "Btn", new Color(1f, 0.55f, 0.65f), 28, 10); _btnImg.raycastTarget = true;
            var brt = _btnImg.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f); brt.pivot = new Vector2(0.5f, 0f); brt.anchoredPosition = new Vector2(0f, 32f); brt.sizeDelta = new Vector2(512f, 100f);
            _btnT = CoastHudLayout.MakeText(brt, "T", "", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), Vector2.zero);
            _btnT.color = Color.white; _btnT.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_btnT, new Color(0.5f, 0.15f, 0.25f, 0.8f), 2f);
            _btnT.resizeTextForBestFit = true; _btnT.resizeTextMinSize = 16; _btnT.resizeTextMaxSize = CoastHudLayout.Scaled(34);
            _btn = _btnImg.gameObject.AddComponent<Button>(); _btn.transition = Selectable.Transition.None; _btn.onClick.AddListener(OnBtn);
            var hold = _btnImg.gameObject.AddComponent<HoldRelay>(); hold.Down = () => _holding = true; hold.Up = () => _holding = false;
            SetIdle();
        }
        RectTransform _progHost;

        void SetIdle()
        {
            _state = 0; if (_fishArt != null) _fishArt.gameObject.SetActive(false); _bobber.gameObject.SetActive(false); _bar.gameObject.SetActive(false); _progHost.gameObject.SetActive(false);
            int left = CastsLeft;
            _castsT.text = Loc.T($"남은 던지기 {left}/{CastsPerPhase}", $"Casts left {left}/{CastsPerPhase}");
            if (left <= 0) { _msg.text = Loc.T("오늘은 여기까지. 다음 턴에 또 오자!", "That's it for today — come back next turn!"); _btnT.text = Loc.T("돌아가기", "Back"); }
            else { _msg.text = Loc.T("찌를 던지고 「!」가 뜨면 바로 당겨!", "Cast, then pull the moment you see 「!」"); _btnT.text = Loc.T("🎣 던지기", "🎣 Cast"); }
        }

        public void DevCast() { OnBtn(); }
        void OnBtn()
        {
            CoastPrefs.Vibrate();
            if (_state == 0) { if (CastsLeft <= 0) { Close(); return; } if (_co != null) StopCoroutine(_co); _co = StartCoroutine(CastCo()); }
            else if (_state == 1) { _msg.text = Loc.T("아직이야… 기다려", "Not yet… wait"); }
            else if (_state == 2) { _state = 3; }
            else if (_state == 4) { SetIdle(); }
        }

        IEnumerator CastCo()
        {
            _state = 1; Save.villageFishCasts++; _gm.Persist();
            _castsT.text = Loc.T($"남은 던지기 {CastsLeft}/{CastsPerPhase}", $"Casts left {CastsLeft}/{CastsPerPhase}");
            _bobber.gameObject.SetActive(true); _btnT.text = Loc.T("기다리는 중…", "Waiting…"); _msg.text = "";
            // 찌 날아가기
            float t = 0f; var from = new Vector2(0f, 420f); var to = new Vector2(UnityEngine.Random.Range(-160f, 160f), UnityEngine.Random.Range(-40f, 60f));
            while (t < 0.6f) { t += Time.deltaTime; float k = t / 0.6f; _bobber.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 180f); yield return null; }
            _bobber.anchoredPosition = to;
            float wait = UnityEngine.Random.Range(1.6f, 4.2f); t = 0f;
            while (t < wait) { t += Time.deltaTime; _bobber.anchoredPosition = to + new Vector2(0f, Mathf.Sin(t * 3f) * 4f); yield return null; }
            // 입질
            _state = 2; _msg.text = "<size=60>!</size>"; _btnT.text = Loc.T("당겨!!", "PULL!!"); CoastPrefs.VibrateEvent();
            float win = 0.9f; t = 0f;
            while (t < win && _state == 2) { t += Time.deltaTime; if (DevAuto && t > 0.2f) _state = 3; _bobber.anchoredPosition = to + new Vector2(0f, -22f + Mathf.Sin(t * 30f) * 6f); yield return null; }
            if (_state != 3) { Result(false, Loc.T("놓쳤다… 「!」가 뜨면 바로 당겨야 해.", "Missed… pull right when 「!」 shows.")); yield break; }
            // 릴 감기
            _cur = Pick(); _bar.gameObject.SetActive(true); _progHost.gameObject.SetActive(true);
            _zoneY = 200f; _zoneV = 0f; _fishY = 250f; _fishT = 0f; _progress = 0.3f;
            _msg.text = Loc.T("버튼을 눌러 초록 칸을 올리고, 물고기를 칸 안에 붙잡아!", "Hold to raise the green zone — keep the fish inside!");
            _btnT.text = Loc.T("▲ 누르면 올라감", "▲ Hold to raise");
            float barH = 560f, zoneH = Mathf.Lerp(170f, 110f, Mathf.InverseLerp(0.5f, 1.5f, _cur.hard)); _zone.sizeDelta = new Vector2(0f, zoneH);
            float limit = 12f; t = 0f;
            while (t < limit)
            {
                float dt = Time.deltaTime; t += dt; _fishT += dt; if (DevAuto) _holding = _fishY > _zoneY + zoneH * 0.5f;
                // 물고기: 느린 사인 + 가끔 튐
                float target = 60f + (Mathf.PerlinNoise(_fishT * 0.45f * _cur.hard, 3.7f) * (barH - 140f));
                if (UnityEngine.Random.value < 0.012f * _cur.hard) target = UnityEngine.Random.Range(40f, barH - 80f);
                _fishY = Mathf.Lerp(_fishY, target, dt * (1.8f + _cur.hard));
                // 초록 칸: 누르면 위로 가속, 놓으면 중력
                _zoneV += (_holding ? 900f : -700f) * dt; _zoneV = Mathf.Clamp(_zoneV, -650f, 650f);
                _zoneY += _zoneV * dt;
                if (_zoneY < 0f) { _zoneY = 0f; _zoneV = 0f; } if (_zoneY > barH - zoneH) { _zoneY = barH - zoneH; _zoneV = 0f; }
                _zone.anchoredPosition = new Vector2(0f, _zoneY); _fishMk.anchoredPosition = new Vector2(0f, _fishY);
                bool inside = _fishY >= _zoneY && _fishY <= _zoneY + zoneH;
                _progress += (inside ? 0.28f : -0.20f * _cur.hard) * dt;
                _zone.GetComponent<Image>().color = inside ? new Color(0.45f, 0.90f, 0.55f, 0.9f) : new Color(0.90f, 0.55f, 0.45f, 0.75f);
                _prog.sizeDelta = new Vector2(-6f, Mathf.Clamp01(_progress) * (barH - 6f));
                if (_progress >= 1f) { Result(true, null); yield break; }
                if (_progress <= 0f) { Result(false, Loc.T("도망갔다… 물고기를 초록 칸 안에 오래 붙잡아야 해.", "It got away… keep the fish inside the green zone.")); yield break; }
                yield return null;
            }
            Result(false, Loc.T("줄이 끊겼다…", "The line snapped…"));
        }

        Fish Pick()
        {
            float W(Fish f) => f.coins >= 160 ? f.weight * (1f + 0.35f * RodTier) : f.weight;   // 198차: 낚싯대 등급
            float sum = 0f; foreach (var f in Table) sum += W(f);
            float r = UnityEngine.Random.value * sum;
            foreach (var f in Table) { r -= W(f); if (r <= 0f) return f; }
            return Table[0];
        }

        void Result(bool ok, string failMsg)
        {
            _state = 4; _bar.gameObject.SetActive(false); _progHost.gameObject.SetActive(false); _bobber.gameObject.SetActive(false);
            if (ok)
            {
                bool boot = _cur.ko == "낡은 장화";
                // 198차(사용자: 「낚시할 때 물고기도 팔 수 있게」): 즉시 코인 대신 그 물고기가 가방에(상점에서 판다) + 요리용 생선 1
                int coins = boot ? 4 : 0; Save.stats.money += coins;
                int fidx = 0; for (int fi = 0; fi < Table.Length; fi++) if (Table[fi].ko == _cur.ko) { VillageDex.See(Save, "fish_" + fi); fidx = fi; break; }   // 195차: 도감
                int fish = boot ? 0 : 1;
                if (!boot) { LifeItems.Add(Save, "fish_" + fidx, 1); LifeItems.Add(Save, "ing_fish", 1); }
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 2);
                _gm.Persist();
                string nm = Loc.T(_cur.ko, _cur.en);
                var art = Resources.Load<Texture2D>("CoastRun/Items/Item_" + (boot ? "fish_boot" : "fish_" + fidx));   // 213차: 잡은 것 그림
                if (art != null && _fishArt != null) { _fishArt.GetComponent<RawImage>().texture = art; _fishArt.gameObject.SetActive(true); StartCoroutine(PopArt()); }
                _msg.text = boot ? Loc.T($"…{nm}?! 그래도 {coins}G", $"…{nm}?! Still {coins}G")
                    : Loc.T($"{nm} 낚았다! 가방에 {nm} +1 · 생선 +{fish} (상점에서 팔 수 있다)", $"Caught {nm}! {nm} +1 · Fish +{fish} (sell at the shop)");
                CoastAudioManager.PlayAnywhere(CoastSfx.Coin); CoastPrefs.VibrateEvent();
                VillageHub.RefreshStatus();
            }
            else _msg.text = failMsg;
            if (DevAuto) Debug.LogWarning($"[Fish] ok={ok} msg={_msg.text} bag fish_0..4=" + string.Join(",", new[]{0,1,2,3,4}.Select(i => LifeItems.Count(Save, "fish_" + i))) + " ing_fish=" + LifeItems.Count(Save, "ing_fish"));
            _btnT.text = CastsLeft > 0 ? Loc.T("다시 던지기", "Cast again") : Loc.T("돌아가기", "Back");
        }

        IEnumerator PopArt()
        {
            float t = 0f; while (t < 0.35f && _fishArt != null) { t += Time.deltaTime; float k = t / 0.35f; float sc = k < 0.7f ? Mathf.Lerp(0.2f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f); _fishArt.localScale = Vector3.one * sc; _fishArt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 9f) * 8f * (1f - k)); yield return null; }
            if (_fishArt != null) { _fishArt.localScale = Vector3.one; _fishArt.localRotation = Quaternion.identity; }
        }

        void Close()
        {
            if (_co != null) StopCoroutine(_co);
            var cb = _onDone; _onDone = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(gameObject);
            cb?.Invoke();
        }

        class WaveDrift : MonoBehaviour
        {
            Vector2 _p; float _ph; void Start() { _p = ((RectTransform)transform).anchoredPosition; _ph = UnityEngine.Random.value * 6f; }
            void Update() { ((RectTransform)transform).anchoredPosition = _p + new Vector2(Mathf.Sin(Time.time * 0.7f + _ph) * 40f, 0f); }
        }
        class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action Down, Up;
            public void OnPointerDown(PointerEventData e) { Down?.Invoke(); }
            public void OnPointerUp(PointerEventData e) { Up?.Invoke(); }
        }
    }
}
