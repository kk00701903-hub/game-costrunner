using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoastRun
{
    /// 44차: 챕터 미션 미니게임 5종 + 미션 오버레이(설명 카드 → 게임 → 승리/패배·다시하기).
    ///   구슬치기(3발에 3개↑) · 윷놀이(한 바퀴 먼저) · 투호(5발에 3발↑) · 딱지치기(3번 안에 넘기기) · 무궁화 꽃이 피었습니다(술래 터치)
    /// 30차 HomeMiniGames.MiniBase(머리·마당·발판 공용 틀)를 그대로 상속. Finish(1)=승리, Finish(0)=패배.
    /// 진입: ChapterMissionUI.Play(kind, replay, onDone(won)) — 미션(replay=false)은 이길 때까지 「다시하기」만, 다시하기(replay=true)는 「그만」도 있음.
    public static class ChapterMissionUI
    {
        private static Canvas _canvas;
        private static RectTransform _root;
        private static Action<bool> _onDone;
        private static ChapterMission.Kind _kind;
        private static bool _replay;

        private static readonly Color Navy = new Color(0.10f, 0.13f, 0.30f);

        public static bool IsOpen => _canvas != null;

        public static void Play(ChapterMission.Kind kind, bool replay, Action<bool> onDone)
        {
            Close();
            _kind = kind; _replay = replay; _onDone = onDone;
#if UNITY_EDITOR
            Debug.LogWarning($"[ChapterMissionUI] Play {kind} replay={replay}");
#endif
            _canvas = CoastUiCanvas.Create("MissionCanvas", 470);
            _root = CoastUiCanvas.Root(_canvas);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(_root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.04f, 0.05f, 0.12f, 0.94f));
            dim.raycastTarget = true;
            ShowIntro();
        }

        /// 49차(사용자): 진입 화면 리디자인 — 게임 마당 그림을 배경으로 깔고, 3D 아이콘 + 큰 제목 + 「3단계 방법」 카드 + 목표 배지 + 큰 시작 버튼.
        private static readonly string[] IntroYards = { "UI_MG_Yard_Marbles", "UI_MG_Yard_Yut", "UI_MG_Yard_Tuho", "UI_MG_Yard_Ddakji", "UI_MG_Yard_Mugunghwa" };
        private static readonly string[] IntroIcons = { "UI_MG_Marbles", "UI_MG_Yut", "UI_MG_Tuho", "UI_MG_Ddakji", "UI_MG_Mugunghwa" };
        private static (string ko, string en)[] IntroSteps(ChapterMission.Kind k)
        {
            switch (k)
            {
                case ChapterMission.Kind.Yut: return new[] { ("[던지기!]로 윷 4개를 던져요", "Tap [Throw!] to toss 4 sticks"), ("도·개·걸·윷·모 만큼 말이 가요", "Move by Do·Gae·Geol·Yut·Mo"), ("같은 칸에 서면 잡고 한 번 더!", "Land on Dodam to catch & throw again") };
                case ChapterMission.Kind.Tuho: return new[] { ("바늘이 항아리를 향할 때 [방향 확정]", "Set aim when the needle points at the jar"), ("힘 게이지 흰 띠 안에서 [발사!]", "Throw inside the white band"), ("화살이 포물선을 그리며 쏙!", "The arrow arcs into the jar") };
                case ChapterMission.Kind.Ddakji: return new[] { ("게이지가 오르내려요", "The timing bar swings"), ("노란 띠 안에서 [내리치기!]", "Slam inside the yellow band"), ("상대 딱지가 뒤집히면 내 것!", "Flip Dodam's ddakji to win") };
                case ChapterMission.Kind.Mugunghwa: return new[] { ("[달리기]를 꾹 누르면 앞으로", "Hold [Run] to move forward"), ("술래가 돌아보면 손을 떼요", "Let go when the tagger looks"), ("끝까지 가서 [술래 터치!]", "Reach the end and [Tag!]") };
                default: return new[] { ("바늘이 멈추길 원하는 방향에서 [방향 확정]", "Set aim where the needle points"), ("힘 게이지에서 [발사!]", "Pick power and [Shoot!]"), ("삼각형 밖으로 구슬을 튕겨 내요", "Knock marbles out of the triangle") };
            }
        }
        private static (string ko, string en) IntroGoal(ChapterMission.Kind k)
        {
            switch (k)
            {
                case ChapterMission.Kind.Yut: return ("도담이보다 먼저 한 바퀴", "Get around before Dodam");
                case ChapterMission.Kind.Tuho: return ("5발 중 3발 넣기", "3 of 5 in the jar");
                case ChapterMission.Kind.Ddakji: return ("3번 안에 한 번 넘기기", "Flip once in 3 tries");
                case ChapterMission.Kind.Mugunghwa: return ("3번 걸리기 전에 술래 터치", "Tag before 3 catches");
                default: return ("3턴 안에 삼각형 안 구슬 2개 이하로", "≤2 marbles left inside after 3 turns");
            }
        }

        private static void ShowIntro()
        {
            var d = ChapterMission.Get(_kind);
            int ki = Mathf.Clamp((int)_kind, 0, 4);
            var pad = CoastUiCanvas.HudPad;
            var card = new GameObject("Intro", typeof(RectTransform)).GetComponent<RectTransform>();
            card.SetParent(_root, false); card.anchorMin = Vector2.zero; card.anchorMax = Vector2.one; card.offsetMin = card.offsetMax = Vector2.zero;
            // 배경: 마당 그림(꽉 채움) + 남색 딤 + 위아래 어둡게
            var yard = CoastUiArt.Art(IntroYards[ki]);
            if (yard != null)
            {
                var bg = new GameObject("Yard", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter)).GetComponent<Image>();
                bg.transform.SetParent(card, false); bg.sprite = yard; bg.raycastTarget = true; bg.color = new Color(0.75f, 0.72f, 0.85f);
                var brt = bg.rectTransform; brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f); brt.sizeDelta = new Vector2(_root.rect.width + pad * 2f, _root.rect.height + pad * 2f);
                var arf = bg.GetComponent<AspectRatioFitter>(); arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; arf.aspectRatio = yard.rect.width / yard.rect.height;
            }
            var dim = CoastHudLayout.MakeImage(card, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.06f, 0.06f, 0.18f, 0.52f)); dim.raycastTarget = true;
            var top = CoastHudLayout.MakeImage(card, "TopShade", new Vector2(0f, 0.72f), new Vector2(1f, 1f), new Vector2(-pad, 0f), new Vector2(pad, pad), new Color(0.04f, 0.04f, 0.14f, 0.55f));
            var bot = CoastHudLayout.MakeImage(card, "BotShade", new Vector2(0f, 0f), new Vector2(1f, 0.30f), new Vector2(-pad, -pad), new Vector2(pad, 0f), new Color(0.04f, 0.04f, 0.14f, 0.65f));
            // 태그(리본)
            var tag = CoastUiArt.GlossyPill(card, "Tag", new Color(0.93f, 0.22f, 0.52f), 18, 6);
            var trt = tag.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.885f); trt.pivot = new Vector2(0.5f, 0.5f); trt.sizeDelta = new Vector2(220f, 44f);
            var tagT = CoastHudLayout.MakeText(trt, "T", _replay ? Loc.T("미니게임 다시하기", "REPLAY") : Loc.T("★ 미션 ★", "★ MISSION ★"), 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), new Vector2(0f, 1f));
            tagT.color = Color.white; CoastUiArt.OutlineText(tagT, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            // 아이콘(3D 그림) + 바닥 그림자 + 둥실
            var shadow = CoastUiArt.Panel(card, "IconShadow", new Color(0f, 0f, 0.05f, 0.45f), 60);
            var srt = shadow.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.64f); srt.pivot = new Vector2(0.5f, 0.5f); srt.sizeDelta = new Vector2(190f, 44f);
            var ico = CoastUiArt.Art(IntroIcons[ki]);
            if (ico != null)
            {
                var im = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                im.transform.SetParent(card, false); im.sprite = ico; im.preserveAspect = true; im.raycastTarget = false;
                var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.755f); irt.pivot = new Vector2(0.5f, 0.5f); irt.sizeDelta = new Vector2(250f, 250f);
                im.gameObject.AddComponent<IntroBob>().shadow = shadow.rectTransform;
            }
            // 제목
            var title = CoastHudLayout.MakeText(card, "Title", Loc.T(d.nameKo, d.nameEn), 46, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.545f), new Vector2(1f, 0.62f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            title.color = new Color(1f, 0.96f, 0.86f); CoastUiArt.OutlineText(title, new Color(0.15f, 0.08f, 0.25f, 0.95f), 3f);
            // 방법 카드: 3단계 + 목표
            var how = CoastUiArt.GlossyPill(card, "How", new Color(1f, 0.95f, 0.80f), 28, 10);
            var hrt = how.rectTransform; hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.40f); hrt.pivot = new Vector2(0.5f, 0.5f); hrt.sizeDelta = new Vector2(620f, 262f); how.raycastTarget = true;
            var howT = CoastHudLayout.MakeText(hrt, "T", Loc.T("이렇게 해요", "HOW TO PLAY"), 15, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(0f, -12f));
            howT.color = new Color(0.60f, 0.45f, 0.35f);
            var steps = IntroSteps(_kind);
            for (int i = 0; i < steps.Length; i++)
            {
                float y = 0.735f - i * 0.185f;
                var num = CoastUiArt.Panel(hrt, "N" + i, new Color(0.93f, 0.22f, 0.52f), 16);
                var nrt = num.rectTransform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.075f, y); nrt.pivot = new Vector2(0.5f, 0.5f); nrt.sizeDelta = new Vector2(34f, 34f);
                var nt = CoastHudLayout.MakeText(nrt, "T", (i + 1).ToString(), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero); nt.color = Color.white;
                var st = CoastHudLayout.MakeText(hrt, "S" + i, Loc.T(steps[i].ko, steps[i].en), 19, TextAnchor.MiddleLeft, new Vector2(0.14f, y - 0.08f), new Vector2(0.98f, y + 0.08f), Vector2.zero, Vector2.zero);
                st.color = Navy; st.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            var goal = IntroGoal(_kind);
            var gp = CoastUiArt.Panel(hrt, "Goal", new Color(0.93f, 0.22f, 0.52f, 0.12f), 14);
            var grt = gp.rectTransform; grt.anchorMin = new Vector2(0.05f, 0.06f); grt.anchorMax = new Vector2(0.95f, 0.21f); grt.offsetMin = grt.offsetMax = Vector2.zero;
            var gt = CoastHudLayout.MakeText(grt, "T", Loc.T("목표 · ", "GOAL · ") + Loc.T(goal.ko, goal.en), 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 2f), Vector2.zero);
            gt.color = new Color(0.80f, 0.12f, 0.40f); gt.fontStyle = FontStyle.Bold;
            // 시작 버튼(맥동)
            var start = Pill(card, "Start", Loc.T("시작!", "START!"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0.175f), Vector2.zero, new Vector2(340f, 84f),
                () => { UnityEngine.Object.Destroy(card.gameObject); StartGame(); });
            var stT = start.GetComponentInChildren<Text>(); if (stT != null) stT.fontSize = 30;
            start.gameObject.AddComponent<IntroPulse>();
            if (!_replay)
            {
                var note = CoastHudLayout.MakeText(card, "Note", Loc.T($"이기면 {ChapterMission.Reward(GameManager.I)}G 보상 · 져도 다음으로 넘어가요", $"Win for {ChapterMission.Reward(GameManager.I)}G · lose and you still move on"), 13, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0.10f), new Vector2(1f, 0.13f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
                note.color = new Color(1f, 1f, 1f, 0.75f);
            }
            else
                Pill(card, "Back", Loc.T("↩ 돌아가기", "↩ Back"), new Color(0.35f, 0.35f, 0.45f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(150f, 46f), () => Done(false));
        }

        /// 진입 화면 아이콘 둥실 + 그림자 호흡
        private class IntroBob : MonoBehaviour
        {
            public RectTransform shadow; private RectTransform _rt; private Vector2 _base;
            private void Awake() { _rt = (RectTransform)transform; _base = _rt.anchoredPosition; }
            private void Update()
            {
                float s = Mathf.Sin(Time.unscaledTime * 2.2f);
                _rt.anchoredPosition = _base + new Vector2(0f, s * 9f);
                _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.1f) * 3f);
                if (shadow != null) shadow.localScale = Vector3.one * (1f - s * 0.08f);
            }
        }
        private class IntroPulse : MonoBehaviour
        {
            private void Update() { transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.03f); }
        }

        private static void StartGame()
        {
            var host = new GameObject("Host", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(_root, false);
            host.anchorMin = Vector2.zero; host.anchorMax = Vector2.one; host.offsetMin = Vector2.zero; host.offsetMax = Vector2.zero;
            MissionMiniGames.Start(_kind, host, _replay, won =>
            {
                if (host != null) UnityEngine.Object.Destroy(host.gameObject);
                if (won) ShowWin(); else ShowLose();
            });
        }

        private static void ShowWin()
        {
            var d = ChapterMission.Get(_kind);
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.8f);
            ChapterMission.MarkCleared(GameManager.I, _kind);
            int reward = 0;
            if (!_replay)
            {
                LevelSystem.Add(LevelSystem.ExpMinigame);   // 53차
                reward = ChapterMission.Reward(GameManager.I);   // 55차-2(사용자): 미니게임 성공 = 돈 보상
                if (GameManager.Active && reward > 0) { GameManager.I.Save.stats.money += reward; GameManager.I.Persist(); }
            }
            var card = CoastUiArt.GlossyPill(_root, "Win", new Color(1f, 0.85f, 0.30f), 30, 12);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 60f); crt.sizeDelta = new Vector2(560f, 300f); card.raycastTarget = true;
            var t = CoastHudLayout.MakeText(crt, "T", _replay ? Loc.T("이겼다!", "You win!") : Loc.T("미션 클리어!", "MISSION CLEAR!"), 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -30f));
            t.color = Navy; CoastUiArt.OutlineText(t, new Color(1f, 1f, 1f, 0.7f), 2f);
            var s = CoastHudLayout.MakeText(crt, "S", d.nameKo + (_replay ? "" : Loc.T($" — 보상 +{reward}G · 더보기 › 미니게임에서 다시 할 수 있어요", $" — +{reward}G · replay it from More › Mini-games")), 15, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.3f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            s.color = new Color(0.35f, 0.30f, 0.40f); s.horizontalOverflow = HorizontalWrapMode.Wrap;
            Pill(crt, "Go", _replay ? Loc.T("돌아가기", "Back") : Loc.T("다음으로", "Continue"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(300f, 70f), () => Done(true));
        }

        private static void ShowLose()
        {
            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.8f);
            var card = CoastUiArt.GlossyPill(_root, "Lose", new Color(0.80f, 0.82f, 0.90f), 30, 12);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 60f); crt.sizeDelta = new Vector2(560f, 300f); card.raycastTarget = true;
            var t = CoastHudLayout.MakeText(crt, "T", Loc.T("아쉽다…", "So close…"), 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -30f));
            t.color = Navy;
            var s = CoastHudLayout.MakeText(crt, "S", _replay ? Loc.T("그 자리에서 바로 다시!", "Try again right here!") : Loc.T("보상은 없지만 그냥 넘어갈 수 있어. 다시 해서 돈을 노려도 좋고!", "No reward, but you can move on — or retry for the money!"), 16, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.3f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            s.color = new Color(0.35f, 0.30f, 0.40f);
            Pill(crt, "Retry", Loc.T("다시하기", "Retry"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.30f, 0f), new Vector2(0f, 22f), new Vector2(230f, 70f),
                () => { UnityEngine.Object.Destroy(card.gameObject); StartGame(); });
            // 55차-2(사용자): 미니게임은 져도 다음으로 넘어간다(보상만 없음). 「넘어가기」= 시도한 것으로 표시하고 종료.
            Pill(crt, "Quit", _replay ? Loc.T("그만", "Quit") : Loc.T("넘어가기", "Move on"), new Color(0.55f, 0.55f, 0.62f), new Vector2(0.76f, 0f), new Vector2(0f, 22f), new Vector2(170f, 70f),
                () => { if (!_replay) ChapterMission.MarkAttempted(GameManager.I, _kind); Done(false); });
        }

        private static void Done(bool won)
        {
            var cb = _onDone; Close(); cb?.Invoke(won);
        }

        public static void Close()
        {
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null; _root = null; _onDone = null;
        }

        private static Button Pill(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var pill = CoastUiArt.GlossyPill(parent, name, color, 24, 8);
            var rt = pill.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size; pill.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f); onClick?.Invoke(); });
            var t = CoastHudLayout.MakeText(rt, "T", label, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 4f), new Vector2(0f, 2f));
            t.color = Color.white; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return b;
        }

        // ── 더보기 › 미니게임 (클리어한 것만 다시하기) ──────────────────────

        private static Canvas _menu;
        public static void OpenMenu(GameManager gm, Action onClose)
        {
            CloseMenu();
            _menu = CoastUiCanvas.Create("MissionMenu", 320);
            var root = CoastUiCanvas.Root(_menu);
            var pad = CoastUiCanvas.HudPad;
            var dim = CoastHudLayout.MakeImage(root, "Dim", Vector2.zero, Vector2.one, new Vector2(-pad, -pad), new Vector2(pad, pad), new Color(0.03f, 0.05f, 0.14f, 0.72f));
            dim.raycastTarget = true;
            var title = CoastHudLayout.MakeText(root, "Title", Loc.T("미니게임", "MINI-GAMES"), 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -30f));
            title.color = new Color(1f, 0.85f, 0.30f); CoastUiArt.OutlineText(title, new Color(0.30f, 0.12f, 0.02f, 0.9f), 2.5f);
            var sub = CoastHudLayout.MakeText(root, "Sub", Loc.T("미션에서 이긴 놀이만 다시 할 수 있어요", "Replay the games you have beaten in missions"), 13, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -148f), new Vector2(0f, -120f));
            sub.color = Color.white; CoastUiArt.OutlineText(sub, new Color(0f, 0f, 0f, 0.6f), 1.2f);
            var xSpr = CoastUiArt.Art("UI_CloseX");
            var xgo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            xgo.transform.SetParent(root, false);
            var xi = xgo.GetComponent<Image>(); xi.raycastTarget = true;
            if (xSpr != null) { xi.sprite = xSpr; xi.preserveAspect = true; } else xi.color = new Color(0.25f, 0.45f, 0.85f);
            var xrt = xgo.GetComponent<RectTransform>(); xrt.anchorMin = xrt.anchorMax = new Vector2(1f, 1f); xrt.pivot = new Vector2(0.5f, 0.5f);
            xrt.anchoredPosition = new Vector2(-46f, -66f); xrt.sizeDelta = new Vector2(84f, 84f);
            xgo.GetComponent<Button>().onClick.AddListener(() => { CloseMenu(); onClose?.Invoke(); });

            var prof = gm != null ? gm.Profile : null;
            Color[] fills = { new Color(1f, 0.90f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.65f, 0.35f), new Color(0.72f, 0.62f, 0.95f), new Color(0.55f, 0.90f, 0.60f) };
            for (int i = 0; i < ChapterMission.All.Length; i++)
            {
                var d = ChapterMission.All[i];
                bool open = ChapterMission.IsCleared(prof, d.kind) || (gm != null && gm.DevUnlockAll);
                var card = CoastUiArt.GlossyPill(root, "M" + i, open ? fills[i] : new Color(0.66f, 0.66f, 0.70f), 22, 9);
                var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 1f); crt.pivot = new Vector2(0.5f, 1f);
                crt.anchoredPosition = new Vector2(0f, -190f - i * 128f); crt.sizeDelta = new Vector2(560f, 112f); card.raycastTarget = true;
                // 46차(사용자 시안): 왼쪽에 Kling 아이콘(구슬 그릇·윷·투호 항아리·딱지·무궁화)
                string[] icons = { "UI_MG_Marbles", "UI_MG_Yut", "UI_MG_Tuho", "UI_MG_Ddakji", "UI_MG_Mugunghwa" };
                var ico = CoastUiArt.Art(icons[Mathf.Clamp((int)d.kind, 0, icons.Length - 1)]);
                if (ico != null)
                {
                    var im = new GameObject("I", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    im.transform.SetParent(crt, false); im.sprite = ico; im.preserveAspect = true; im.raycastTarget = false;
                    var irt = im.rectTransform; irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f); irt.pivot = new Vector2(0f, 0.5f);
                    irt.anchoredPosition = new Vector2(16f, 0f); irt.sizeDelta = new Vector2(88f, 88f);
                    if (!open) im.color = new Color(0.7f, 0.7f, 0.72f, 0.8f);
                }
                var n = CoastHudLayout.MakeText(crt, "N", d.nameKo, 28, TextAnchor.MiddleLeft, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(120f, 0f), new Vector2(-20f, -8f));
                n.color = open ? Navy : new Color(0.33f, 0.33f, 0.38f);
                var s = CoastHudLayout.MakeText(crt, "S", open ? Loc.T($"챕터 {d.chapter} 미션 · 다시하기", $"Chapter {d.chapter} mission · replay") : Loc.T($"챕터 {d.chapter}를 클리어하고 미션에서 이기면 열려요", $"Beat the chapter {d.chapter} mission to unlock"), 14, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(120f, 10f), new Vector2(-20f, 0f));
                s.color = open ? new Color(0.30f, 0.28f, 0.40f) : new Color(0.40f, 0.40f, 0.46f);
                var kind = d.kind;
                var b = card.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() =>
                {
                    if (!open) { CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss); CoastToast.Show(Loc.T("아직 잠겨 있어요 — 미션에서 먼저 이겨요", "Locked — beat it in the story mission first")); return; }
                    if (_menu != null) _menu.gameObject.SetActive(false);
                    Play(kind, true, _ => { if (_menu != null) _menu.gameObject.SetActive(true); });
                });
            }
        }

        public static void CloseMenu()
        {
            if (_menu != null) UnityEngine.Object.Destroy(_menu.gameObject);
            _menu = null;
        }
    }

    /// 미션용 미니게임 5종. HomeMiniGames.MiniBase 상속 — Finish(1)=승리 / Finish(0)=패배.
    public static partial class MissionMiniGames
    {
        private static readonly Color Navy = new Color(0.23f, 0.16f, 0.29f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.90f);
        private static readonly Color Coral = new Color(1f, 0.44f, 0.57f);
        private static readonly Color Sky = new Color(0.39f, 0.71f, 0.96f);
        private static readonly Color Sun = new Color(1f, 0.78f, 0.30f);

        public static void Start(ChapterMission.Kind kind, RectTransform host, bool replay, Action<bool> onDone)
        {
            var go = new GameObject("Mission_" + kind, typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 6f); rt.offsetMax = new Vector2(-6f, -6f);
            HomeMiniGames.MiniBase g = kind switch
            {
                ChapterMission.Kind.Marbles => go.AddComponent<MarblesMission>(),
                ChapterMission.Kind.Yut => go.AddComponent<YutMission3D>(),        // 49차: 3D 무대(MissionMiniGames.Games3D.cs)
                ChapterMission.Kind.Tuho => go.AddComponent<TuhoMission3D>(),
                ChapterMission.Kind.Ddakji => go.AddComponent<DdakjiMission3D>(),
                _ => go.AddComponent<MugunghwaMission3D>(),
            };
            g.Init(rt, true, r => onDone?.Invoke(r > 0));
            // 미션은 「그만」으로 도망 못 간다(다시하기만). 다시하기 모드는 그만 = 패배로 처리돼 메뉴로.
            if (!replay)
            {
                var q = rt.Find("Head/Quit");
                if (q != null) q.gameObject.SetActive(false);
            }
        }

        // ── 구슬치기(45차 → 48차-13 3D → 48차-14 시안 반영): 네온 삼각형 · 보석 구슬 · 점선 조준 · 하단 3분할 조작 패널 ─
        // 판정·시뮬레이션은 45차 2D(정규화 0..1) 그대로. 보이는 것만 MiniStage3D(Kling 마당 배경판 + Blender 유리 구슬)로.
        private class MarblesMission : HomeMiniGames.MiniBase
        {
            protected override string Title => Loc.T("미션 · 구슬치기", "Mission · Marbles");
            private class Marble
            {
                public Transform tr, blob, spark; public Vector2 pos, vel; public bool player, outOf;
                public Material glass, swirl, core; public Color color;
            }
            private readonly List<Marble> _ms = new List<Marble>();
            private Marble _me;
            private int _shots = 3, _knocked;
            private const int Need = 6;   // 51차(사용자): 8개 중 2개 이하 남아야 승리 = 6개 이상 밖으로
            private enum Step { Aim, Power, Rolling, Done }
            private Step _step = Step.Aim;
            private float _t, _angle, _power;
            private MiniStage3D _stage;
            private readonly List<Transform> _aimDots = new List<Transform>();
            private Transform _aimHead;
            private RectTransform _needle, _powerMark, _powerFill;
            private Text _btnLabel, _angleTxt, _powerTxt, _dirHint, _powHint;
            private Image _fireFill;
            private float _radius, _aimLen;
            private static readonly Vector2 MarbleDiamNorm = new Vector2(0.048f, 0f);   // 45차 34px/706px
            private static readonly Color Neon = new Color(0.35f, 0.95f, 1f);
            private static readonly Color PanelNavy = new Color(0.16f, 0.22f, 0.42f);
            private static readonly Color PanelEdge = new Color(0.10f, 0.14f, 0.30f);
            // 삼각형(정규화): 꼭짓점 위, 밑변 아래
            private static readonly Vector2 A = new Vector2(0.5f, 0.86f), B = new Vector2(0.18f, 0.42f), C = new Vector2(0.82f, 0.42f);

            protected override void Build(Transform foot)
            {
                // 시안: 아래 조작 패널이 크다 — 마당 0.29~0.92, 발판 0~0.28
                var frame = Field.parent as RectTransform;
                if (frame != null) { frame.anchorMin = new Vector2(0f, 0.29f); frame.anchorMax = new Vector2(1f, 0.92f); }
                var footRt = foot as RectTransform;
                if (footRt != null) { footRt.anchorMin = new Vector2(0f, 0f); footRt.anchorMax = new Vector2(1f, 0.28f); }
                var footImg = foot.GetComponent<Image>(); if (footImg != null) footImg.color = new Color(0.12f, 0.16f, 0.34f);
                Rect(Status.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, -2f));
                Status.fontSize = 14; Status.fontStyle = FontStyle.Bold; Status.alignment = TextAnchor.MiddleCenter; Status.color = new Color(1f, 0.96f, 0.75f);
                CoastUiArt.OutlineText(Status, new Color(0f, 0f, 0f, 0.6f), 1.5f);

                _stage = MiniStage3D.Create(Field, "UI_MG_Yard_Marbles", 56f, 38f, 10f);
                float wpn = _stage.WorldPerNorm(new Vector2(0.5f, 0.5f));
                _radius = MarbleDiamNorm.x * wpn * 0.5f * 1.9f;
                _aimLen = 0.30f * _stage.WorldPerNorm(new Vector2(0.5f, 0.16f));
                // 네온 삼각형 — 발광 막대 + 그 아래 넓은 글로우
                var neonMat = MiniStage3D.Lit(new Color(0.05f, 0.50f, 0.70f, 1f), 0.2f);
                if (neonMat.HasProperty("_EmissionColor")) { neonMat.EnableKeyword("_EMISSION"); neonMat.SetColor("_EmissionColor", new Color(0.10f, 0.75f, 0.95f)); }
                var glowMat = MiniStage3D.SoftDisc(new Color(Neon.r, Neon.g, Neon.b, 0.6f));
                Edge(A, B, neonMat, glowMat); Edge(B, C, neonMat, glowMat); Edge(C, A, neonMat, glowMat);
                // 보석 구슬 8개(채도 높은 8색) + 흰(파랑 유리) 구슬
                Color[] cols = { new Color(0.95f, 0.25f, 0.30f), new Color(0.20f, 0.55f, 1f), new Color(0.25f, 0.85f, 0.40f), new Color(1f, 0.80f, 0.15f), new Color(0.65f, 0.35f, 0.95f), new Color(1f, 0.50f, 0.15f), new Color(0.20f, 0.85f, 0.90f), new Color(0.95f, 0.40f, 0.75f) };
                Vector2[] spots = { new Vector2(0.5f, 0.72f), new Vector2(0.44f, 0.62f), new Vector2(0.56f, 0.62f), new Vector2(0.38f, 0.52f), new Vector2(0.50f, 0.52f), new Vector2(0.62f, 0.52f), new Vector2(0.44f, 0.46f), new Vector2(0.56f, 0.46f) };
                for (int i = 0; i < 8; i++) _ms.Add(Make(spots[i], cols[i], false));
                _me = Make(new Vector2(0.5f, 0.16f), new Color(0.30f, 0.55f, 1f), true);
                // 점선 조준(노란 구슬 점 7개 + 작은 화살촉)
                var dotMat = MiniStage3D.Lit(new Color(1f, 0.85f, 0.25f), 0.5f);
                if (dotMat.HasProperty("_EmissionColor")) { dotMat.EnableKeyword("_EMISSION"); dotMat.SetColor("_EmissionColor", new Color(0.6f, 0.45f, 0.05f)); }
                for (int i = 0; i < 7; i++)
                {
                    var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>());
                    d.transform.SetParent(_stage.Root, false); d.transform.localScale = Vector3.one * _radius * (0.42f - i * 0.03f);
                    var r = d.GetComponent<Renderer>(); r.sharedMaterial = dotMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    _aimDots.Add(d.transform);
                }
                var ar = _stage.Spawn("MG_Arrow"); _aimHead = ar.transform;
                foreach (var r in ar.GetComponentsInChildren<Renderer>()) r.sharedMaterial = dotMat;

                BuildPanel(foot);
                Status.text = Loc.T($"남은 발 3 · 삼각형 안에 2개 이하로 남기기(8개 중 {Need}개 밖으로) — [방향 확정]", $"Shots 3 · leave ≤2 inside ({Need} of 8 out) — [Set aim]");
                Place(_me); foreach (var m in _ms) Place(m);
                PlaceAim();
            }

            // ── 하단 조작 패널(시안): [방향 선택 반원 게이지] [힘 선택 세로 게이지] [발사!] ──
            private void BuildPanel(Transform foot)
            {
                // 왼쪽 카드: 방향 선택
                var dir = CoastUiArt.CutePill(foot, "DirCard", PanelNavy, 18, 3);
                Rect(dir.rectTransform, new Vector2(0.02f, 0.05f), new Vector2(0.34f, 0.82f), Vector2.zero, Vector2.zero); dir.raycastTarget = false;
                var dt = Txt(dir.transform, "T", Loc.T("방향 선택", "Direction"), 17, new Color(1f, 0.93f, 0.55f), TextAnchor.UpperCenter);
                Rect(dt.rectTransform, new Vector2(0f, 0.76f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -6f)); dt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(dt, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                // 반원 게이지: 점 호(파랑) + 눈금 0/90/180 + 빨간 바늘
                var gaugeC = new GameObject("Gauge", typeof(RectTransform)).GetComponent<RectTransform>();
                gaugeC.SetParent(dir.transform, false); gaugeC.anchorMin = gaugeC.anchorMax = new Vector2(0.5f, 0.30f); gaugeC.sizeDelta = Vector2.zero;
                for (int i = 0; i <= 24; i++)
                {
                    float a = Mathf.PI * (1f - i / 24f);
                    var p = CoastUiArt.Panel(gaugeC, "Arc" + i, i % 6 == 0 ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.45f, 0.75f, 1f, 0.9f), 4); p.raycastTarget = false;
                    p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    p.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * 62f, Mathf.Sin(a) * 62f);
                    p.rectTransform.sizeDelta = i % 6 == 0 ? new Vector2(9f, 9f) : new Vector2(6f, 6f);
                }
                string[] ticks = { "0", "90", "180" }; Vector2[] tp = { new Vector2(-62f, -14f), new Vector2(0f, 76f), new Vector2(62f, -14f) };
                for (int i = 0; i < 3; i++)
                {
                    var tt = Txt(gaugeC, "Tick" + i, ticks[i], 11, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
                    tt.rectTransform.anchorMin = tt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); tt.rectTransform.anchoredPosition = tp[i]; tt.rectTransform.sizeDelta = new Vector2(40f, 16f);
                }
                var hub = CoastUiArt.Panel(gaugeC, "Hub", new Color(0.95f, 0.3f, 0.3f), 7); hub.raycastTarget = false;
                hub.rectTransform.anchorMin = hub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); hub.rectTransform.sizeDelta = new Vector2(14f, 14f);
                var needle = CoastUiArt.Panel(gaugeC, "Needle", new Color(1f, 0.25f, 0.25f), 3); needle.raycastTarget = false;
                _needle = needle.rectTransform; _needle.anchorMin = _needle.anchorMax = new Vector2(0.5f, 0.5f); _needle.pivot = new Vector2(0.5f, 0f);
                _needle.sizeDelta = new Vector2(6f, 58f);
                _angleTxt = Txt(dir.transform, "Ang", "90°", 14, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter);
                Rect(_angleTxt.rectTransform, new Vector2(0f, 0.36f), new Vector2(1f, 0.50f), Vector2.zero, Vector2.zero); _angleTxt.fontStyle = FontStyle.Bold;
                _dirHint = Txt(dir.transform, "Hint", Loc.T("좌우로 움직이는 중…\n타이밍에 멈추기", "Sweeping left/right…\nstop it on time"), 11, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
                Rect(_dirHint.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.20f), new Vector2(6f, 0f), new Vector2(-6f, 0f));

                // 가운데 카드: 힘 선택
                var pow = CoastUiArt.CutePill(foot, "PowCard", PanelNavy, 18, 3);
                Rect(pow.rectTransform, new Vector2(0.36f, 0.05f), new Vector2(0.60f, 0.82f), Vector2.zero, Vector2.zero); pow.raycastTarget = false;
                var pt = Txt(pow.transform, "T", Loc.T("힘 선택", "Power"), 17, new Color(1f, 0.93f, 0.55f), TextAnchor.UpperCenter);
                Rect(pt.rectTransform, new Vector2(0f, 0.76f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -6f)); pt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(pt, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                var bar = CoastUiArt.CutePill(pow.transform, "Bar", new Color(0.06f, 0.08f, 0.18f), 12, 3);
                Rect(bar.rectTransform, new Vector2(0.40f, 0.30f), new Vector2(0.62f, 0.74f), Vector2.zero, Vector2.zero); bar.raycastTarget = false;
                // 무지개 세로 게이지(초록→노랑→빨강, 8단)
                var fillC = new GameObject("FillC", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                fillC.SetParent(bar.transform, false); Rect(fillC, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 4f), new Vector2(-4f, 0f));
                _powerFill = fillC;
                var segHost = new GameObject("Seg", typeof(RectTransform)).GetComponent<RectTransform>();
                segHost.SetParent(fillC, false); segHost.anchorMin = new Vector2(0f, 0f); segHost.anchorMax = new Vector2(1f, 0f); segHost.pivot = new Vector2(0.5f, 0f);
                segHost.anchoredPosition = Vector2.zero; segHost.sizeDelta = new Vector2(0f, 200f);   // 실제 높이는 LateUpdate 에서 바 높이에 맞춤
                _segHost = segHost;
                for (int i = 0; i < 8; i++)
                {
                    var seg = CoastHudLayout.MakeImage(segHost, "S" + i, new Vector2(0f, i / 8f), new Vector2(1f, (i + 1) / 8f), Vector2.zero, Vector2.zero,
                        Color.Lerp(Color.Lerp(new Color(0.2f, 0.9f, 0.4f), new Color(1f, 0.9f, 0.2f), Mathf.Clamp01(i / 4f)), new Color(1f, 0.25f, 0.2f), Mathf.Clamp01((i - 4) / 3.5f)));
                    seg.raycastTarget = false;
                }
                string[] pct = { "0%", "50%", "100%" }; float[] py = { 0.30f, 0.52f, 0.74f };
                for (int i = 0; i < 3; i++)
                {
                    var l = Txt(pow.transform, "P" + i, pct[i], 11, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleRight);
                    Rect(l.rectTransform, new Vector2(0.02f, py[i] - 0.04f), new Vector2(0.37f, py[i] + 0.04f), Vector2.zero, Vector2.zero);
                }
                _powerTxt = Txt(pow.transform, "Pct", "0%", 24, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
                Rect(_powerTxt.rectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.28f), Vector2.zero, Vector2.zero); _powerTxt.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(_powerTxt, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                _powHint = Txt(pow.transform, "Hint", Loc.T("힘을 멈춰서 결정!", "Stop to set power!"), 11, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
                Rect(_powHint.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);

                // 오른쪽: 발사! 큰 노란 버튼
                var fire = CoastUiArt.GlossyPill(foot, "Fire", new Color(1f, 0.80f, 0.20f), 22, 10);
                Rect(fire.rectTransform, new Vector2(0.62f, 0.05f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero); fire.raycastTarget = true;
                _fireFill = fire.transform.Find("Fill")?.GetComponent<Image>();
                var fb = fire.gameObject.AddComponent<Button>(); fb.transition = Selectable.Transition.None;
                fb.onClick.AddListener(() => { CoastPrefs.Vibrate(); OnButton(); });
                _btnLabel = Txt(fire.transform, "T", Loc.T("방향 확정", "Set aim"), 30, new Color(0.45f, 0.18f, 0.02f), TextAnchor.MiddleCenter);
                Rect(_btnLabel.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero); _btnLabel.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(_btnLabel, new Color(1f, 1f, 1f, 0.35f), 1.2f);
                var arrow = Txt(fire.transform, "Arrow", "→", 30, new Color(1f, 0.55f, 0.1f), TextAnchor.MiddleCenter);
                Rect(arrow.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.32f), Vector2.zero, Vector2.zero); arrow.fontStyle = FontStyle.Bold;
                CoastUiArt.OutlineText(arrow, new Color(0.4f, 0.15f, 0f, 0.6f), 1.5f);
            }
            private RectTransform _segHost;

            private void Edge(Vector2 p, Vector2 q, Material mat, Material glow)
            {
                var a = _stage.GroundPoint(p); var b = _stage.GroundPoint(q);
                _stage.Bar(a, b, _radius * 0.26f, _radius * 0.10f, mat);
                // 글로우: 납작한 쿼드(소프트 원판을 길게 늘림)
                var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = "Glow"; Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(_stage.Root, false);
                var d = b - a;
                g.transform.position = (a + b) * 0.5f + Vector3.up * 0.004f;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                g.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);   // 위를 보는 납작 쿼드, 길이축 = 변 방향
                g.transform.localScale = new Vector3(_radius * 1.8f, d.magnitude + _radius, 1f);
                var r = g.GetComponent<Renderer>(); r.sharedMaterial = glow; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            }

            private Marble Make(Vector2 anchor, Color c, bool player)
            {
                var go = _stage.Spawn("MG_Marble");
                go.name = player ? "Me" : "Marble";
                go.transform.localScale = Vector3.one * (_radius * 2f);   // FBX 반지름 0.5
                var m = new Marble { tr = go.transform, pos = anchor, player = player, color = c };
                // 보석 느낌: 색 유리 진하게 + 안쪽 어두운 소용돌이 + 밝은 코어 + 반짝이
                var glassC = player ? new Color(0.75f, 0.88f, 1f, 0.55f) : new Color(c.r, c.g, c.b, 0.62f);
                m.glass = MiniStage3D.Lit(glassC, 0.92f, 0.1f, true);
                m.swirl = MiniStage3D.Lit(Color.Lerp(c, Color.black, 0.45f), 0.5f);
                m.core = MiniStage3D.Lit(Color.Lerp(c, Color.white, 0.45f), 0.7f);
                if (m.core.HasProperty("_EmissionColor")) { m.core.EnableKeyword("_EMISSION"); m.core.SetColor("_EmissionColor", c * 0.35f); }
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    string n = r.gameObject.name;
                    r.sharedMaterial = n.StartsWith("Glass") ? m.glass : n.StartsWith("Swirl") ? m.swirl : n.StartsWith("Core") ? m.core : m.swirl;
                }
                go.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f));
                m.blob = _stage.Blob(_radius * 1.3f, 0.55f).transform;
                // 반짝이: 카메라를 향한 작은 흰 원판
                var sp = GameObject.CreatePrimitive(PrimitiveType.Quad); sp.name = "Spark"; Destroy(sp.GetComponent<Collider>());
                sp.transform.SetParent(_stage.Root, false); sp.transform.localScale = Vector3.one * _radius * 0.55f;
                var sr = sp.GetComponent<Renderer>(); sr.sharedMaterial = MiniStage3D.SoftDisc(new Color(1f, 1f, 1f, 0.95f));
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
                m.spark = sp.transform;
                return m;
            }

            private void Place(Marble m)
            {
                var g = _stage.GroundPoint(m.pos);
                float k = m.outOf ? 0.75f : 1f;
                m.tr.position = g + Vector3.up * _radius * k;
                m.blob.position = g + Vector3.up * 0.005f;
                var cam = _stage.Cam.transform;
                m.spark.rotation = cam.rotation;
                m.spark.position = m.tr.position + (cam.up * 0.55f - cam.right * 0.45f) * _radius * k - cam.forward * _radius * 0.9f * k;
                m.spark.gameObject.SetActive(!m.outOf);
            }

            private void PlaceAim()
            {
                bool on = _step == Step.Aim || _step == Step.Power;
                foreach (var d in _aimDots) d.gameObject.SetActive(on);
                if (_aimHead != null) _aimHead.gameObject.SetActive(on);
                if (!on) return;
                var g = _stage.GroundPoint(_me.pos);
                var dir = Quaternion.Euler(0f, _angle, 0f) * Vector3.forward;
                float len = _aimLen * (_step == Step.Power ? 0.6f + _power * 0.6f : 1f);
                for (int i = 0; i < _aimDots.Count; i++)
                {
                    float t = (i + 1f) / (_aimDots.Count + 1f);
                    _aimDots[i].position = g + dir * (_radius * 1.3f + len * t) + Vector3.up * _radius * 0.25f;
                }
                _aimHead.position = g + dir * (_radius * 1.3f + len) + Vector3.up * 0.01f;
                _aimHead.rotation = Quaternion.Euler(0f, _angle + 180f, 0f);
                _aimHead.localScale = new Vector3(_radius * 1.1f, _radius * 1.1f, _radius * 1.6f);
                if (_needle != null) _needle.localRotation = Quaternion.Euler(0f, 0f, -_angle);
                if (_angleTxt != null) _angleTxt.text = $"{Mathf.RoundToInt(90f + _angle)}°";
            }

            private static bool InsideTri(Vector2 p)
            {
                float s1 = (B.x - A.x) * (p.y - A.y) - (B.y - A.y) * (p.x - A.x);
                float s2 = (C.x - B.x) * (p.y - B.y) - (C.y - B.y) * (p.x - B.x);
                float s3 = (A.x - C.x) * (p.y - C.y) - (A.y - C.y) * (p.x - C.x);
                bool neg = s1 < 0 || s2 < 0 || s3 < 0, pos = s1 > 0 || s2 > 0 || s3 > 0;
                return !(neg && pos);
            }

            private void OnButton()
            {
                if (_step == Step.Aim)
                {
                    _step = Step.Power; _t = 0f; _btnLabel.text = Loc.T("발사!", "Shoot!");
                    _dirHint.text = Loc.T("방향 확정!", "Aim set!"); _powHint.text = Loc.T("힘을 멈춰서 결정!", "Stop to set power!");
                    Status.text = Loc.T("힘 게이지가 오르내려요 — 원하는 세기에서 [발사!]", "Power swings — tap [Shoot!] at the strength you want");
                    CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.35f);
                }
                else if (_step == Step.Power)
                {
                    _step = Step.Rolling;
                    var dir = new Vector2(Mathf.Sin(_angle * Mathf.Deg2Rad), Mathf.Cos(_angle * Mathf.Deg2Rad));
                    _me.vel = dir * (0.6f + _power * 2.6f);
                    _shots--;
                    PlaceAim();
                    _btnLabel.text = Loc.T("굴러가는 중", "Rolling…"); if (_fireFill != null) _fireFill.color = new Color(0.85f, 0.75f, 0.45f);
                    _powHint.text = Loc.T($"{Mathf.RoundToInt(_power * 100f)}% 로 발사!", $"Shot at {Mathf.RoundToInt(_power * 100f)}%!");
                    CoastAudioManager.PlayAnywhere(CoastSfx.Jump, 0.5f);
                    Status.text = Loc.T($"남은 발 {_shots} · 남은 구슬 {8 - _knocked}개(2개 이하면 승리)", $"Shots {_shots} · {8 - _knocked} left (≤2 wins)");
                }
            }

            private bool Moving() { foreach (var m in _ms) if (m.vel.sqrMagnitude > 1e-5f) return true; return _me.vel.sqrMagnitude > 1e-5f; }

            private void LateUpdate()
            {
                // 힘 게이지 색 띠 높이를 바 높이에 맞춘다(레이아웃 뒤)
                if (_segHost != null && _powerFill != null && _powerFill.parent is RectTransform pr)
                    _segHost.sizeDelta = new Vector2(0f, Mathf.Max(10f, pr.rect.height - 8f));
            }

            private void Update()
            {
                if (_step == Step.Done || _stage == null || _me == null) return;   // 에디터 핫리로드로 필드가 비면 조용히
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                if (_step == Step.Aim)
                {
                    _t += dt; _angle = Mathf.Sin(_t * 1.6f) * 55f;   // 좌우 55°
                    PlaceAim();
                    return;
                }
                if (_step == Step.Power)
                {
                    _t += dt; _power = 0.5f + 0.5f * Mathf.Sin(_t * 2.4f * Mathf.PI - Mathf.PI * 0.5f);
                    _powerFill.anchorMax = new Vector2(1f, _power);
                    _powerTxt.text = $"{Mathf.RoundToInt(_power * 100f)}%";
                    PlaceAim();
                    return;
                }
                // Rolling
                var all = new List<Marble>(_ms) { _me };
                var r = Field.rect; float rad = 17f / r.width;
                foreach (var m in all)
                {
                    if (m.outOf) continue;
                    m.pos += m.vel * dt;
                    m.vel *= Mathf.Pow(0.15f, dt);
                    if (m.vel.magnitude < 0.01f) m.vel = Vector2.zero;
                    if (m.pos.x < 0.03f + rad) { m.pos.x = 0.03f + rad; m.vel.x = -m.vel.x * 0.6f; }
                    if (m.pos.x > 0.97f - rad) { m.pos.x = 0.97f - rad; m.vel.x = -m.vel.x * 0.6f; }
                    if (m.pos.y < 0.04f + rad) { m.pos.y = 0.04f + rad; m.vel.y = -m.vel.y * 0.6f; }
                    if (m.pos.y > 0.96f - rad) { m.pos.y = 0.96f - rad; m.vel.y = -m.vel.y * 0.6f; }
                    if (!m.player && !InsideTri(m.pos) && m.vel.magnitude < 0.05f)
                    {
                        m.outOf = true; m.vel = Vector2.zero; _knocked++;
                        m.tr.localScale = Vector3.one * (_radius * 2f * 0.75f);
                        var gc = m.glass.GetColor("_BaseColor"); gc.a = 0.35f; m.glass.SetColor("_BaseColor", gc);
                        m.swirl.SetColor("_BaseColor", Color.Lerp(m.color, new Color(0.5f, 0.5f, 0.5f), 0.6f));
                        m.blob.localScale *= 0.75f;
                        CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.6f);
                        Status.text = Loc.T($"남은 발 {_shots} · 남은 구슬 {8 - _knocked}개(2개 이하면 승리)", $"Shots {_shots} · {8 - _knocked} left (≤2 wins)");
                    }
                }
                for (int i = 0; i < all.Count; i++)
                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var a = all[i]; var b = all[j]; if (a.outOf || b.outOf) continue;
                        var d = b.pos - a.pos; d.y *= r.height / r.width;
                        float dist = d.magnitude; float min = rad * 2f;
                        if (dist < min && dist > 1e-4f)
                        {
                            var n = d / dist; n.y *= r.width / r.height;
                            var rel = a.vel - b.vel; float p = Vector2.Dot(rel, n.normalized);
                            if (p > 0f)
                            {
                                a.vel -= n.normalized * p; b.vel += n.normalized * p;
                                if (p > 0.25f) CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit, Mathf.Clamp01(p * 0.4f));
                            }
                            float push = (min - dist) * 0.5f;
                            a.pos -= n.normalized * push; b.pos += n.normalized * push;
                        }
                    }
                foreach (var m in all)
                {
                    if (m.outOf) continue;
                    var before = m.tr.position;
                    Place(m);
                    var mv = m.tr.position - before; mv.y = 0f;
                    float len = mv.magnitude;
                    if (len > 1e-5f) m.tr.Rotate(Vector3.Cross(Vector3.up, mv / len), len / _radius * Mathf.Rad2Deg, Space.World);
                }
                if (Moving()) return;
                if (_knocked >= Need) { _step = Step.Done; Status.text = Loc.T($"성공! {_knocked}개를 밖으로", $"Done! {_knocked} out"); StartCoroutine(EndAfter(0.9f, 1)); }
                else if (_shots <= 0) { _step = Step.Done; Status.text = Loc.T($"{8 - _knocked}개 남았다… 2개 이하여야 해", $"{8 - _knocked} left… need ≤2"); StartCoroutine(EndAfter(1.0f, 0)); }
                else
                {
                    // 흰 구슬이 삼각형 안에 멈추면 밑으로 되돌린다(다음 발 조준)
                    _me.pos = new Vector2(Mathf.Clamp(_me.pos.x, 0.2f, 0.8f), Mathf.Min(_me.pos.y, 0.30f));
                    Place(_me);
                    _step = Step.Aim; _t = 0f; _btnLabel.text = Loc.T("방향 확정", "Set aim"); if (_fireFill != null) _fireFill.color = new Color(1f, 0.80f, 0.20f);
                    _powerFill.anchorMax = new Vector2(1f, 0f); _powerTxt.text = "0%";
                    _dirHint.text = Loc.T("좌우로 움직이는 중…\n타이밍에 멈추기", "Sweeping left/right…\nstop it on time");
                    PlaceAim();
                    Status.text = Loc.T($"다음 발 — 남은 발 {_shots} · 남은 구슬 {8 - _knocked}개", $"Next — Shots {_shots} · {8 - _knocked} left");
                }
            }

            private void OnDestroy() { if (_stage != null) Destroy(_stage.gameObject); }

            private IEnumerator EndAfter(float s, int r) { yield return new WaitForSecondsRealtime(s); Finish(r); }
        }
        // 49차: 윷놀이·투호·딱지치기·무궁화는 MissionMiniGames.Games3D.cs 의 3D 무대 버전으로 이동(2D 버전 삭제).
    }
}
