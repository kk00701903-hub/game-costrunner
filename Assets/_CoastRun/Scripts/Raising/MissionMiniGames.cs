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

        private static void ShowIntro()
        {
            var d = ChapterMission.Get(_kind);
            var card = CoastUiArt.GlossyPill(_root, "Intro", new Color(1f, 0.93f, 0.72f), 30, 12);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 60f); crt.sizeDelta = new Vector2(600f, 470f); card.raycastTarget = true;
            var tag = CoastHudLayout.MakeText(crt, "Tag", _replay ? Loc.T("미니게임 다시하기", "Replay") : Loc.T("미션!", "MISSION!"), 20, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(0f, -24f));
            tag.color = new Color(0.93f, 0.22f, 0.52f);
            var title = CoastHudLayout.MakeText(crt, "Title", d.nameKo, 40, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -70f));
            title.color = Navy; CoastUiArt.OutlineText(title, new Color(1f, 1f, 1f, 0.6f), 1.5f);
            var rule = CoastHudLayout.MakeText(crt, "Rule", Loc.T(d.ruleKo, d.ruleEn), 18, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(30f, 120f), new Vector2(-30f, -160f));
            rule.color = new Color(0.25f, 0.22f, 0.30f); rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (!_replay)
            {
                var note = CoastHudLayout.MakeText(crt, "Note", Loc.T("이겨야 다음 이야기로 넘어가요 · 져도 바로 다시 할 수 있어요", "Win to continue the story · you can retry right away"), 13, TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 92f), new Vector2(-20f, 118f));
                note.color = new Color(0.45f, 0.40f, 0.50f);
            }
            Pill(crt, "Start", Loc.T("시작!", "Start!"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(300f, 74f),
                () => { UnityEngine.Object.Destroy(card.gameObject); StartGame(); });
            if (_replay)
                Pill(crt, "Back", Loc.T("돌아가기", "Back"), new Color(0.55f, 0.55f, 0.62f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(120f, 44f), () => Done(false));
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
            var card = CoastUiArt.GlossyPill(_root, "Win", new Color(1f, 0.85f, 0.30f), 30, 12);
            var crt = card.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0f, 60f); crt.sizeDelta = new Vector2(560f, 300f); card.raycastTarget = true;
            var t = CoastHudLayout.MakeText(crt, "T", _replay ? Loc.T("이겼다!", "You win!") : Loc.T("미션 클리어!", "MISSION CLEAR!"), 44, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -30f));
            t.color = Navy; CoastUiArt.OutlineText(t, new Color(1f, 1f, 1f, 0.7f), 2f);
            var s = CoastHudLayout.MakeText(crt, "S", d.nameKo + (_replay ? "" : Loc.T(" — 더보기 › 미니게임에서 다시 할 수 있어요", " — replay it from More › Mini-games")), 15, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.3f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            s.color = new Color(0.35f, 0.30f, 0.40f); s.horizontalOverflow = HorizontalWrapMode.Wrap;
            Pill(crt, "Go", _replay ? Loc.T("돌아가기", "Back") : Loc.T("다음 이야기로", "Continue"), new Color(0.93f, 0.22f, 0.52f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(300f, 70f), () => Done(true));
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
            var s = CoastHudLayout.MakeText(crt, "S", Loc.T("그 자리에서 바로 다시!", "Try again right here!"), 16, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.3f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            s.color = new Color(0.35f, 0.30f, 0.40f);
            Pill(crt, "Retry", Loc.T("다시하기", "Retry"), new Color(0.93f, 0.22f, 0.52f), new Vector2(_replay ? 0.30f : 0.5f, 0f), new Vector2(0f, 22f), new Vector2(_replay ? 230f : 260f, 70f),
                () => { UnityEngine.Object.Destroy(card.gameObject); StartGame(); });
            if (_replay)
                Pill(crt, "Quit", Loc.T("그만", "Quit"), new Color(0.55f, 0.55f, 0.62f), new Vector2(0.76f, 0f), new Vector2(0f, 22f), new Vector2(170f, 70f), () => Done(false));
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
    public static class MissionMiniGames
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
                ChapterMission.Kind.Yut => go.AddComponent<YutMission>(),
                ChapterMission.Kind.Tuho => go.AddComponent<TuhoMission>(),
                ChapterMission.Kind.Ddakji => go.AddComponent<DdakjiMission>(),
                _ => go.AddComponent<MugunghwaMission>(),
            };
            g.Init(rt, true, r => onDone?.Invoke(r > 0));
            // 미션은 「그만」으로 도망 못 간다(다시하기만). 다시하기 모드는 그만 = 패배로 처리돼 메뉴로.
            if (!replay)
            {
                var q = rt.Find("Head/Quit");
                if (q != null) q.gameObject.SetActive(false);
            }
        }

        // ── 구슬치기(45차): [방향] → [힘] → 발사. 삼각형 안 구슬 8개, 3발에 5개 이상 밖으로 ─
        private class MarblesMission : HomeMiniGames.MiniBase
        {
            protected override string Title => Loc.T("미션 · 구슬치기", "Mission · Marbles");
            private class Marble { public RectTransform rt; public Vector2 pos, vel; public bool player, outOf; }
            private readonly List<Marble> _ms = new List<Marble>();
            private Marble _me;
            private int _shots = 3, _knocked;
            private const int Need = 5;
            private enum Step { Aim, Power, Rolling, Done }
            private Step _step = Step.Aim;
            private float _t, _angle, _power;
            private RectTransform _aimLine, _powerFill;
            private Text _btnLabel;
            // 삼각형(정규화): 꼭짓점 위, 밑변 아래
            private static readonly Vector2 A = new Vector2(0.5f, 0.86f), B = new Vector2(0.18f, 0.42f), C = new Vector2(0.82f, 0.42f);

            protected override void Build(Transform foot)
            {
                CoastHudLayout.MakeImage(Field, "Ground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.87f, 0.78f, 0.60f));
                // 삼각형 — 세 변을 얇은 막대로
                Edge(A, B); Edge(B, C); Edge(C, A);
                Color[] cols = { new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.75f, 0.95f), new Color(0.55f, 0.85f, 0.55f), new Color(0.95f, 0.85f, 0.40f), new Color(0.80f, 0.55f, 0.90f), new Color(1f, 0.65f, 0.35f), new Color(0.40f, 0.85f, 0.85f), new Color(0.95f, 0.60f, 0.80f) };
                Vector2[] spots = { new Vector2(0.5f, 0.72f), new Vector2(0.44f, 0.62f), new Vector2(0.56f, 0.62f), new Vector2(0.38f, 0.52f), new Vector2(0.50f, 0.52f), new Vector2(0.62f, 0.52f), new Vector2(0.44f, 0.46f), new Vector2(0.56f, 0.46f) };
                for (int i = 0; i < 8; i++) _ms.Add(Make(spots[i], cols[i], false));
                _me = Make(new Vector2(0.5f, 0.16f), Color.white, true);
                // 조준선(흰 구슬에서 뻗는 선) · 힘 게이지(오른쪽 세로)
                var line = CoastUiArt.Panel(Field, "AimLine", new Color(1f, 1f, 1f, 0.75f), 3);
                _aimLine = line.rectTransform; _aimLine.anchorMin = _aimLine.anchorMax = _me.pos; _aimLine.pivot = new Vector2(0.5f, 0f);
                _aimLine.sizeDelta = new Vector2(5f, 150f); line.raycastTarget = false;
                var bar = CoastUiArt.Panel(Field, "PowerBar", new Color(0.35f, 0.30f, 0.30f), 8);
                Rect(bar.rectTransform, new Vector2(0.92f, 0.10f), new Vector2(0.97f, 0.40f), Vector2.zero, Vector2.zero);
                var fill = CoastUiArt.Panel(bar.transform, "Fill", Sun, 6);
                _powerFill = fill.rectTransform; Rect(_powerFill, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 2f), new Vector2(-2f, 0f));
                var pb = Btn(foot, "Go", Loc.T("방향 선택", "Set aim"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(180f, 54f), OnButton);
                _btnLabel = pb.GetComponentInChildren<Text>();
                Status.text = Loc.T($"조준선이 돌아가요 — 원하는 방향에서 [방향 선택]. 남은 발 3 · 목표 {Need}개", $"The aim line sweeps — tap [Set aim]. Shots 3 · goal {Need}");
            }

            private void Edge(Vector2 p, Vector2 q)
            {
                var r = Field.rect; Vector2 pp = new Vector2(p.x * r.width, p.y * r.height), qq = new Vector2(q.x * r.width, q.y * r.height);
                var e = CoastUiArt.Panel(Field, "Edge", new Color(0.55f, 0.35f, 0.25f), 2);
                var rt = e.rectTransform; rt.anchorMin = rt.anchorMax = (p + q) * 0.5f; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2((qq - pp).magnitude, 5f); rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(qq.y - pp.y, qq.x - pp.x) * Mathf.Rad2Deg);
                e.raycastTarget = false;
            }

            private Marble Make(Vector2 anchor, Color c, bool player)
            {
                var im = Dot(Field, player ? "Me" : "Marble", anchor, new Vector2(34f, 34f), c);
                var gloss = CoastUiArt.Panel(im.transform, "Gloss", new Color(1f, 1f, 1f, 0.55f), 6);
                gloss.rectTransform.anchorMin = gloss.rectTransform.anchorMax = new Vector2(0.35f, 0.7f); gloss.rectTransform.sizeDelta = new Vector2(12f, 8f);
                return new Marble { rt = im.rectTransform, pos = anchor, player = player };
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
                if (_step == Step.Aim) { _step = Step.Power; _t = 0f; _btnLabel.text = Loc.T("발사!", "Shoot!"); Status.text = Loc.T("힘 게이지가 오르내려요 — 원하는 세기에서 [발사!]", "Power swings — tap [Shoot!] at the strength you want"); }
                else if (_step == Step.Power)
                {
                    _step = Step.Rolling;
                    var dir = new Vector2(Mathf.Sin(_angle * Mathf.Deg2Rad), Mathf.Cos(_angle * Mathf.Deg2Rad));
                    _me.vel = dir * (0.6f + _power * 2.6f);
                    _shots--;
                    _aimLine.gameObject.SetActive(false);
                    Status.text = Loc.T($"남은 발 {_shots} · 밖으로 {_knocked}/{Need}", $"Shots {_shots} · out {_knocked}/{Need}");
                }
            }

            private bool Moving() { foreach (var m in _ms) if (m.vel.sqrMagnitude > 1e-5f) return true; return _me.vel.sqrMagnitude > 1e-5f; }

            private void Update()
            {
                if (_step == Step.Done || _aimLine == null || _me == null) return;   // 에디터 핫리로드로 필드가 비면 조용히
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
                if (_step == Step.Aim)
                {
                    _t += dt; _angle = Mathf.Sin(_t * 1.6f) * 55f;   // 좌우 55°
                    _aimLine.anchorMin = _aimLine.anchorMax = _me.pos;
                    _aimLine.localRotation = Quaternion.Euler(0f, 0f, -_angle);
                    return;
                }
                if (_step == Step.Power)
                {
                    _t += dt; _power = 0.5f + 0.5f * Mathf.Sin(_t * 2.4f * Mathf.PI - Mathf.PI * 0.5f);
                    _powerFill.anchorMax = new Vector2(1f, _power);
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
                    if (m.pos.x > 0.90f - rad) { m.pos.x = 0.90f - rad; m.vel.x = -m.vel.x * 0.6f; }
                    if (m.pos.y < 0.04f + rad) { m.pos.y = 0.04f + rad; m.vel.y = -m.vel.y * 0.6f; }
                    if (m.pos.y > 0.96f - rad) { m.pos.y = 0.96f - rad; m.vel.y = -m.vel.y * 0.6f; }
                    if (!m.player && !InsideTri(m.pos) && m.vel.magnitude < 0.05f)
                    {
                        m.outOf = true; m.vel = Vector2.zero; _knocked++;
                        m.rt.localScale = Vector3.one * 0.7f; var im = m.rt.GetComponent<Image>(); im.color = new Color(im.color.r, im.color.g, im.color.b, 0.45f);
                        CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.6f);
                        Status.text = Loc.T($"남은 발 {_shots} · 밖으로 {_knocked}/{Need}", $"Shots {_shots} · out {_knocked}/{Need}");
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
                            if (p > 0f) { a.vel -= n.normalized * p; b.vel += n.normalized * p; }
                            float push = (min - dist) * 0.5f;
                            a.pos -= n.normalized * push; b.pos += n.normalized * push;
                        }
                    }
                foreach (var m in all) { m.rt.anchorMin = m.rt.anchorMax = m.pos; }
                if (Moving()) return;
                if (_knocked >= Need) { _step = Step.Done; Status.text = Loc.T($"성공! {_knocked}개를 밖으로", $"Done! {_knocked} out"); StartCoroutine(EndAfter(0.9f, 1)); }
                else if (_shots <= 0) { _step = Step.Done; Status.text = Loc.T($"{_knocked}개… {Need}개가 필요해", $"{_knocked}… need {Need}"); StartCoroutine(EndAfter(1.0f, 0)); }
                else
                {
                    // 흰 구슬이 삼각형 안에 멈추면 밑으로 되돌린다(다음 발 조준)
                    _me.pos = new Vector2(Mathf.Clamp(_me.pos.x, 0.2f, 0.8f), Mathf.Min(_me.pos.y, 0.30f));
                    _me.rt.anchorMin = _me.rt.anchorMax = _me.pos;
                    _step = Step.Aim; _t = 0f; _aimLine.gameObject.SetActive(true); _btnLabel.text = Loc.T("방향 선택", "Set aim");
                    _powerFill.anchorMax = new Vector2(1f, 0f);
                    Status.text = Loc.T($"다음 발 — [방향 선택]. 남은 발 {_shots} · 밖으로 {_knocked}/{Need}", $"Next — [Set aim]. Shots {_shots} · out {_knocked}/{Need}");
                }
            }

            private IEnumerator EndAfter(float s, int r) { yield return new WaitForSecondsRealtime(s); Finish(r); }
        }

        // ── 윷놀이: 한 바퀴 먼저 ─────────────────────────────────────────
        private class YutMission : HomeMiniGames.MiniBase
        {
            protected override string Title => Loc.T("미션 · 윷놀이", "Mission · Yut Nori");
            private const int Cells = 20;
            private int _me, _ai;
            private bool _myTurn = true, _busy;
            private Image _meTok, _aiTok;
            private readonly Image[] _sticks = new Image[4];
            private Text _result;
            private readonly List<Vector2> _cellPos = new List<Vector2>();

            protected override void Build(Transform foot)
            {
                for (int i = 0; i <= Cells; i++) _cellPos.Add(CellAnchor(i));
                for (int i = 0; i < Cells; i++)
                {
                    bool corner = i % 5 == 0;
                    var c = Dot(Field, "Cell" + i, _cellPos[i], corner ? new Vector2(34f, 34f) : new Vector2(22f, 22f), corner ? new Color(0.55f, 0.35f, 0.25f) : new Color(0.75f, 0.60f, 0.45f));
                    if (i == 0) Txt(c.transform, "S", Loc.T("출발", "Start"), 10, Cream, TextAnchor.MiddleCenter);
                }
                _meTok = Dot(Field, "Me", _cellPos[0], new Vector2(30f, 30f), Coral);
                Txt(_meTok.transform, "T", Loc.T("나", "Me"), 10, Color.white, TextAnchor.MiddleCenter);
                _aiTok = Dot(Field, "Ai", _cellPos[0], new Vector2(30f, 30f), Sky);
                _aiTok.rectTransform.anchoredPosition = new Vector2(10f, -8f);
                Txt(_aiTok.transform, "T", Loc.T("도담", "Dodam"), 9, Color.white, TextAnchor.MiddleCenter);
                for (int i = 0; i < 4; i++)
                {
                    var st = CoastUiArt.Panel(Field, "Stick" + i, new Color(0.85f, 0.70f, 0.50f), 8);
                    st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0.5f, 0.55f); st.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    st.rectTransform.anchoredPosition = new Vector2(-60f + i * 40f, 0f); st.rectTransform.sizeDelta = new Vector2(22f, 90f);
                    _sticks[i] = st;
                }
                _result = Txt(Field, "Res", Loc.T("[던지기]를 눌러 시작", "Press [Throw] to start"), 20, Navy, TextAnchor.MiddleCenter);
                Rect(_result.rectTransform, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.40f), Vector2.zero, Vector2.zero);
                Btn(foot, "Throw", Loc.T("던지기", "Throw"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(160f, 54f), () => { if (_myTurn && !_busy) StartCoroutine(Turn(true)); });
                Status.text = Loc.T("내 차례. 도담이보다 먼저 한 바퀴!", "Your turn. Get around before Dodam!");
            }

            private static Vector2 CellAnchor(int i)
            {
                float l = 0.10f, r = 0.90f, b = 0.10f, t = 0.90f;
                i = Mathf.Clamp(i, 0, Cells);
                if (i <= 5) return new Vector2(Mathf.Lerp(l, r, i / 5f), b);
                if (i <= 10) return new Vector2(r, Mathf.Lerp(b, t, (i - 5) / 5f));
                if (i <= 15) return new Vector2(Mathf.Lerp(r, l, (i - 10) / 5f), t);
                return new Vector2(l, Mathf.Lerp(t, b, (i - 15) / 5f));
            }

            private IEnumerator Turn(bool me)
            {
                _busy = true;
                float t = 0f;
                bool[] flat = new bool[4];
                while (t < 0.6f)
                {
                    t += Time.unscaledDeltaTime;
                    for (int i = 0; i < 4; i++)
                    {
                        _sticks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f + i) * 25f);
                        _sticks[i].color = UnityEngine.Random.value > 0.5f ? new Color(0.85f, 0.70f, 0.50f) : new Color(0.45f, 0.30f, 0.20f);
                    }
                    yield return null;
                }
                int flats = 0;
                for (int i = 0; i < 4; i++) { flat[i] = UnityEngine.Random.value < 0.55f; if (flat[i]) flats++; _sticks[i].rectTransform.localRotation = Quaternion.identity; _sticks[i].color = flat[i] ? new Color(0.92f, 0.82f, 0.62f) : new Color(0.45f, 0.30f, 0.20f); }
                int move; string name; bool again = false;
                switch (flats) { case 1: move = 1; name = "도"; break; case 2: move = 2; name = "개"; break; case 3: move = 3; name = "걸"; break; case 4: move = 4; name = "윷"; again = true; break; default: move = 5; name = "모"; again = true; break; }
                _result.text = (me ? Loc.T("나: ", "Me: ") : Loc.T("도담: ", "Dodam: ")) + name + $" (+{move})" + (again ? Loc.T("  한 번 더!", "  again!") : "");
                yield return new WaitForSecondsRealtime(0.5f);
                for (int k = 0; k < move; k++)
                {
                    if (me) _me = Mathf.Min(Cells, _me + 1); else _ai = Mathf.Min(Cells, _ai + 1);
                    var tok = me ? _meTok : _aiTok; int pos = me ? _me : _ai;
                    tok.rectTransform.anchorMin = tok.rectTransform.anchorMax = _cellPos[pos];
                    tok.rectTransform.anchoredPosition = me ? Vector2.zero : new Vector2(10f, -8f);
                    tok.rectTransform.localScale = Vector3.one * 1.25f;
                    yield return new WaitForSecondsRealtime(0.12f);
                    tok.rectTransform.localScale = Vector3.one;
                }
                if (me && _me == _ai && _ai > 0 && _ai < Cells) { _ai = 0; _aiTok.rectTransform.anchorMin = _aiTok.rectTransform.anchorMax = _cellPos[0]; _result.text += Loc.T("  도담이를 잡았다!", "  Caught Dodam!"); again = true; }
                if (!me && _ai == _me && _me > 0 && _me < Cells) { _me = 0; _meTok.rectTransform.anchorMin = _meTok.rectTransform.anchorMax = _cellPos[0]; _result.text += Loc.T("  잡혔다…", "  Caught…"); again = true; }
                yield return new WaitForSecondsRealtime(0.4f);
                if (_me >= Cells) { Status.text = Loc.T("먼저 들어왔다!", "Home first!"); _result.text = Loc.T("승리!", "Victory!"); yield return new WaitForSecondsRealtime(1.0f); Finish(1); yield break; }
                if (_ai >= Cells) { Status.text = Loc.T("도담이가 먼저…", "Dodam got home first…"); _result.text = Loc.T("패배…", "Lost…"); yield return new WaitForSecondsRealtime(1.0f); Finish(0); yield break; }
                _busy = false;
                if (again) { if (!me) StartCoroutine(Turn(false)); else Status.text = Loc.T("한 번 더 던져!", "Throw again!"); yield break; }
                _myTurn = !me;
                if (_myTurn) Status.text = Loc.T("내 차례.", "Your turn.");
                else { Status.text = Loc.T("도담이 차례…", "Dodam's turn…"); yield return new WaitForSecondsRealtime(0.5f); StartCoroutine(Turn(false)); }
            }
        }

        // ── 투호(45차): [방향] → [힘] → 발사. 방향 ±6°·힘 ±9% 안이면 항아리에 쏙. 5발 중 3발 ─
        private class TuhoMission : HomeMiniGames.MiniBase
        {
            protected override string Title => Loc.T("미션 · 투호", "Mission · Tuho");
            private RectTransform _arrow, _jar, _powerFill;
            private Text _btnLabel;
            private enum Step { Aim, Power, Flying, Done }
            private Step _step = Step.Aim;
            private float _t, _angle, _power;
            private int _left = 5, _in;
            private const int Need = 3;
            private static readonly Vector2 Start = new Vector2(0.5f, 0.14f);
            private static readonly Vector2 Jar = new Vector2(0.5f, 0.76f);
            private const float AimTol = 6f, PowerTol = 0.09f, NeedPower = 0.62f;

            protected override void Build(Transform foot)
            {
                CoastHudLayout.MakeImage(Field, "Ground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.93f, 0.90f, 0.80f));
                var mat = CoastUiArt.Panel(Field, "Mat", new Color(0.72f, 0.55f, 0.40f), 14);
                Rect(mat.rectTransform, new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.74f), Vector2.zero, Vector2.zero);
                var jar = CoastUiArt.GlossyPill(Field, "Jar", new Color(0.45f, 0.30f, 0.22f), 26, 8);
                _jar = jar.rectTransform; _jar.anchorMin = _jar.anchorMax = Jar; _jar.pivot = new Vector2(0.5f, 0.5f);
                _jar.anchoredPosition = Vector2.zero; _jar.sizeDelta = new Vector2(120f, 150f);
                var mouth = CoastUiArt.Panel(_jar, "Mouth", new Color(0.15f, 0.10f, 0.08f), 30);
                mouth.rectTransform.anchorMin = mouth.rectTransform.anchorMax = new Vector2(0.5f, 1f); mouth.rectTransform.anchoredPosition = new Vector2(0f, -10f); mouth.rectTransform.sizeDelta = new Vector2(96f, 26f);
                var arrow = CoastUiArt.Panel(Field, "Arrow", new Color(0.85f, 0.25f, 0.20f), 4);
                _arrow = arrow.rectTransform; _arrow.anchorMin = _arrow.anchorMax = Start; _arrow.pivot = new Vector2(0.5f, 0.15f);
                _arrow.sizeDelta = new Vector2(8f, 120f);
                var fl = CoastUiArt.Panel(_arrow, "Feather", new Color(1f, 0.85f, 0.30f), 4);
                fl.rectTransform.anchorMin = fl.rectTransform.anchorMax = new Vector2(0.5f, 0f); fl.rectTransform.anchoredPosition = new Vector2(0f, 12f); fl.rectTransform.sizeDelta = new Vector2(26f, 24f);
                // 힘 게이지(오른쪽 세로) + 목표 표시선
                var bar = CoastUiArt.Panel(Field, "PowerBar", new Color(0.35f, 0.30f, 0.30f), 8);
                Rect(bar.rectTransform, new Vector2(0.92f, 0.10f), new Vector2(0.97f, 0.60f), Vector2.zero, Vector2.zero);
                var fill = CoastUiArt.Panel(bar.transform, "Fill", Sun, 6);
                _powerFill = fill.rectTransform; Rect(_powerFill, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 2f), new Vector2(-2f, 0f));
                var mark = CoastUiArt.Panel(bar.transform, "Mark", new Color(1f, 1f, 1f, 0.8f), 2);
                Rect(mark.rectTransform, new Vector2(-0.4f, NeedPower - PowerTol), new Vector2(1.4f, NeedPower + PowerTol), Vector2.zero, Vector2.zero);
                mark.color = new Color(1f, 1f, 1f, 0.35f);
                var pb = Btn(foot, "Go", Loc.T("방향 선택", "Set aim"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(180f, 54f), OnButton);
                _btnLabel = pb.GetComponentInChildren<Text>();
                Status.text = Loc.T($"화살이 좌우로 돌아요 — 항아리를 향할 때 [방향 선택]. 남은 화살 {_left} · 넣은 것 {_in}/{Need}", $"The arrow sweeps — tap [Set aim] when it points at the jar. Arrows {_left} · in {_in}/{Need}");
            }

            private void OnButton()
            {
                if (_step == Step.Aim) { _step = Step.Power; _t = 0f; _btnLabel.text = Loc.T("발사!", "Throw!"); Status.text = Loc.T("힘 게이지 — 흰 띠 안에서 [발사!]", "Power — tap [Throw!] inside the white band"); }
                else if (_step == Step.Power) StartCoroutine(Throw());
            }

            private void Update()
            {
                if (_arrow == null) return;
                if (_step == Step.Aim)
                {
                    _t += Time.unscaledDeltaTime; _angle = Mathf.Sin(_t * 1.5f) * 35f;
                    _arrow.localRotation = Quaternion.Euler(0f, 0f, -_angle);
                }
                else if (_step == Step.Power)
                {
                    _t += Time.unscaledDeltaTime; _power = 0.5f + 0.5f * Mathf.Sin(_t * 2.2f * Mathf.PI - Mathf.PI * 0.5f);
                    _powerFill.anchorMax = new Vector2(1f, _power);
                }
            }

            private IEnumerator Throw()
            {
                _step = Step.Flying; _left--;
                bool hit = Mathf.Abs(_angle) <= AimTol && Mathf.Abs(_power - NeedPower) <= PowerTol;
                var dir = new Vector2(Mathf.Sin(_angle * Mathf.Deg2Rad), Mathf.Cos(_angle * Mathf.Deg2Rad));
                float dist = hit ? (Jar - Start).magnitude : (Jar - Start).magnitude * (_power / NeedPower);
                Vector2 end = hit ? Jar : Start + dir * dist;
                end.x = Mathf.Clamp(end.x, 0.05f, 0.88f); end.y = Mathf.Clamp(end.y, 0.05f, 0.95f);
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.5f);
                    var pos = Vector2.Lerp(Start, end, u); pos.y += Mathf.Sin(u * Mathf.PI) * 0.10f;
                    _arrow.anchorMin = _arrow.anchorMax = pos;
                    _arrow.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, u);
                    if (!hit && u > 0.7f) _arrow.localRotation = Quaternion.Euler(0f, 0f, -_angle + (_angle >= 0 ? 1f : -1f) * (u - 0.7f) / 0.3f * 80f);
                    yield return null;
                }
                if (hit) { _in++; CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.7f); var stuck = UnityEngine.Object.Instantiate(_arrow.gameObject, Field); stuck.name = "Stuck"; stuck.transform.SetSiblingIndex(_jar.GetSiblingIndex()); }
                else CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.5f);
                string why = hit ? "" : Mathf.Abs(_angle) > AimTol ? Loc.T("(방향이 빗나감)", "(off aim)") : _power < NeedPower ? Loc.T("(힘이 약함)", "(too weak)") : Loc.T("(힘이 셈)", "(too strong)");
                Status.text = hit ? Loc.T($"쏙! 넣은 것 {_in}/{Need} · 남은 화살 {_left}", $"In! {_in}/{Need} · arrows {_left}")
                                  : Loc.T($"빗나감 {why} · 넣은 것 {_in}/{Need} · 남은 화살 {_left}", $"Miss {why} · {_in}/{Need} · arrows {_left}");
                yield return new WaitForSecondsRealtime(0.4f);
                _arrow.localScale = Vector3.one; _arrow.localRotation = Quaternion.identity;
                _arrow.anchorMin = _arrow.anchorMax = Start;
                _powerFill.anchorMax = new Vector2(1f, 0f);
                if (_in >= Need) { _step = Step.Done; Status.text = Loc.T("3발 성공! 투호 명인", "3 in! Tuho master"); yield return new WaitForSecondsRealtime(0.8f); Finish(1); yield break; }
                if (_left <= 0) { _step = Step.Done; Status.text = Loc.T($"{_in}발… {Need}발이 필요해", $"{_in}… need {Need}"); yield return new WaitForSecondsRealtime(0.9f); Finish(0); yield break; }
                _step = Step.Aim; _t = 0f; _btnLabel.text = Loc.T("방향 선택", "Set aim");
            }
        }

        // ── 딱지치기: 힘 게이지 노란 구간에서 내리치기. 3번 안에 1번 넘기기 ─
        private class DdakjiMission : HomeMiniGames.MiniBase
        {
            protected override string Title => Loc.T("미션 · 딱지치기", "Mission · Ddakji");
            private RectTransform _needle, _mine, _theirs;
            private float _t; private const float Speed = 2.1f;
            private int _left = 3; private bool _busy, _ended;
            private const float ZoneL = 0.66f, ZoneR = 0.86f;

            protected override void Build(Transform foot)
            {
                CoastHudLayout.MakeImage(Field, "Ground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.80f, 0.78f, 0.74f));
                // 바닥의 상대 딱지(파랑) — 넘기면 뒤집혀 빨강
                var th = CoastUiArt.GlossyPill(Field, "Theirs", new Color(0.30f, 0.50f, 0.90f), 10, 4);
                _theirs = th.rectTransform; _theirs.anchorMin = _theirs.anchorMax = new Vector2(0.5f, 0.42f); _theirs.pivot = new Vector2(0.5f, 0.5f); _theirs.sizeDelta = new Vector2(130f, 130f);
                Txt(_theirs, "T", Loc.T("도담", "Dodam"), 16, Color.white, TextAnchor.MiddleCenter);
                var mine = CoastUiArt.GlossyPill(Field, "Mine", new Color(0.95f, 0.35f, 0.35f), 10, 4);
                _mine = mine.rectTransform; _mine.anchorMin = _mine.anchorMax = new Vector2(0.5f, 0.86f); _mine.pivot = new Vector2(0.5f, 0.5f); _mine.sizeDelta = new Vector2(120f, 120f);
                Txt(_mine, "T", Loc.T("나", "Me"), 16, Color.white, TextAnchor.MiddleCenter);
                // 힘 게이지(왼쪽 세로)
                var bar = CoastUiArt.Panel(Field, "Bar", new Color(0.35f, 0.33f, 0.36f), 10);
                Rect(bar.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.15f, 0.92f), Vector2.zero, Vector2.zero);
                var zone = CoastUiArt.Panel(bar.transform, "Zone", Sun, 8);
                Rect(zone.rectTransform, new Vector2(0f, ZoneL), new Vector2(1f, ZoneR), new Vector2(3f, 0f), new Vector2(-3f, 0f));
                var nd = CoastUiArt.Panel(bar.transform, "Needle", Color.white, 3);
                _needle = nd.rectTransform; _needle.anchorMin = new Vector2(-0.3f, 0f); _needle.anchorMax = new Vector2(1.3f, 0f); _needle.pivot = new Vector2(0.5f, 0.5f);
                _needle.offsetMin = new Vector2(0f, -3f); _needle.offsetMax = new Vector2(0f, 3f);
                Btn(foot, "Slam", Loc.T("내리치기!", "Slam!"), Coral, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(170f, 54f), () => { if (!_busy && !_ended) StartCoroutine(Slam()); });
                Status.text = Loc.T($"노란 구간에서 내리쳐! 남은 기회 {_left}", $"Slam in the yellow zone! Tries {_left}");
            }

            private float Needle01 => 0.5f + 0.5f * Mathf.Sin(_t * Speed * Mathf.PI);

            private void Update()
            {
                if (_busy || _ended) return;
                _t += Time.unscaledDeltaTime;
                float v = Needle01;
                _needle.anchorMin = new Vector2(-0.3f, v); _needle.anchorMax = new Vector2(1.3f, v);
            }

            private IEnumerator Slam()
            {
                _busy = true; _left--;
                float v = Needle01;
                bool flip = v >= ZoneL && v <= ZoneR;
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.25f);
                    _mine.anchorMin = _mine.anchorMax = new Vector2(0.5f, Mathf.Lerp(0.86f, 0.46f, u * u));
                    yield return null;
                }
                CoastAudioManager.PlayAnywhere(flip ? CoastSfx.ChapterClear : CoastSfx.NearMiss, 0.7f);
                if (flip)
                {
                    t = 0f;
                    while (t < 0.5f)
                    {
                        t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.5f);
                        _theirs.localRotation = Quaternion.Euler(u * 180f, 0f, 0f);
                        _theirs.anchoredPosition = new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 60f);
                        yield return null;
                    }
                    _theirs.GetComponent<Image>().color = new Color(0.95f, 0.35f, 0.35f);
                    _ended = true;
                    Status.text = Loc.T("넘어갔다! 내 딱지!", "Flipped! It's mine!");
                    yield return new WaitForSecondsRealtime(0.8f);
                    Finish(1); yield break;
                }
                // 실패: 상대 딱지가 들썩만
                t = 0f;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime; float u = Mathf.Clamp01(t / 0.3f);
                    _theirs.anchoredPosition = new Vector2(0f, Mathf.Sin(u * Mathf.PI) * 14f);
                    _theirs.localRotation = Quaternion.Euler(Mathf.Sin(u * Mathf.PI) * 25f, 0f, 0f);
                    yield return null;
                }
                _theirs.anchoredPosition = Vector2.zero; _theirs.localRotation = Quaternion.identity;
                _mine.anchorMin = _mine.anchorMax = new Vector2(0.5f, 0.86f);
                Status.text = v < ZoneL ? Loc.T($"약해! 남은 기회 {_left}", $"Too weak! Tries {_left}") : Loc.T($"너무 세서 튕겼어! 남은 기회 {_left}", $"Too hard, it bounced! Tries {_left}");
                if (_left <= 0) { _ended = true; yield return new WaitForSecondsRealtime(0.6f); Finish(0); yield break; }
                _busy = false;
            }
        }

        // ── 무궁화 꽃이 피었습니다: [달리기] 누르는 동안 전진, 술래가 돌아보면 멈춰야 ─
        private class MugunghwaMission : HomeMiniGames.MiniBase, IPointerDownHandler, IPointerUpHandler
        {
            protected override string Title => Loc.T("미션 · 무궁화 꽃이 피었습니다", "Mission · Red Light, Green Light");
            private RectTransform _me, _tagger, _bar;
            private Image _taggerFace, _barFill;
            private Text _chant;
            private float _progress;      // 0 → 1(술래 앞)
            private bool _holding, _turned, _ended, _touchable;
            private float _phaseT, _phaseLen;
            private int _caught;

            protected override void Build(Transform foot)
            {
                var ground = CoastHudLayout.MakeImage(Field, "Ground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.78f, 0.88f, 0.70f));
                ground.raycastTarget = true;
                var lane = CoastUiArt.Panel(Field, "Lane", new Color(0.90f, 0.86f, 0.70f), 14);
                Rect(lane.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.58f, 0.86f), Vector2.zero, Vector2.zero);
                var tg = CoastUiArt.GlossyPill(Field, "Tagger", new Color(0.35f, 0.45f, 0.65f), 30, 8);
                _tagger = tg.rectTransform; _tagger.anchorMin = _tagger.anchorMax = new Vector2(0.5f, 0.88f); _tagger.pivot = new Vector2(0.5f, 0.5f); _tagger.sizeDelta = new Vector2(96f, 96f);
                _taggerFace = tg;
                Txt(_tagger, "T", Loc.T("술래", "It"), 16, Color.white, TextAnchor.MiddleCenter);
                var tb = _tagger.gameObject.AddComponent<Button>(); tb.transition = Selectable.Transition.None; tg.raycastTarget = true;
                tb.onClick.AddListener(() => { if (_touchable && !_ended) StartCoroutine(Win()); });
                var me = CoastUiArt.GlossyPill(Field, "Me", Coral, 24, 6);
                _me = me.rectTransform; _me.anchorMin = _me.anchorMax = new Vector2(0.5f, 0.12f); _me.pivot = new Vector2(0.5f, 0.5f); _me.sizeDelta = new Vector2(64f, 64f);
                Txt(_me, "T", Loc.T("나", "Me"), 14, Color.white, TextAnchor.MiddleCenter);
                _chant = Txt(Field, "Chant", "", 22, Navy, TextAnchor.MiddleCenter);
                Rect(_chant.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), Vector2.zero, Vector2.zero);
                CoastUiArt.OutlineText(_chant, new Color(1f, 1f, 1f, 0.7f), 1.5f);
                // 달리기 버튼(누르고 있는 동안)
                var run = CoastUiArt.CutePill(foot, "Run", Coral, 16, 3);
                run.rectTransform.anchorMin = run.rectTransform.anchorMax = new Vector2(1f, 0.5f); run.rectTransform.pivot = new Vector2(1f, 0.5f);
                run.rectTransform.anchoredPosition = new Vector2(-12f, 0f); run.rectTransform.sizeDelta = new Vector2(180f, 58f); run.raycastTarget = true;
                var rl = Txt(run.transform, "T", Loc.T("달리기 (꾹)", "Run (hold)"), 17, Color.white, TextAnchor.MiddleCenter);
                CoastUiArt.OutlineText(rl, new Color(0f, 0f, 0f, 0.35f), 1.5f);
                var hold = run.gameObject.AddComponent<HoldRelay>(); hold.target = this;
                NextPhase(false);
                Status.text = Loc.T("술래가 돌아보면 손을 떼! 술래 앞에 가서 술래를 터치", "Let go when It turns! Reach It and tap");
            }

            public void OnPointerDown(PointerEventData e) { _holding = true; }
            public void OnPointerUp(PointerEventData e) { _holding = false; }

            private void NextPhase(bool turned)
            {
                _turned = turned; _phaseT = 0f;
                _phaseLen = turned ? UnityEngine.Random.Range(0.9f, 1.6f) : UnityEngine.Random.Range(1.4f, 3.2f);
                _taggerFace.color = turned ? new Color(0.95f, 0.30f, 0.30f) : new Color(0.35f, 0.45f, 0.65f);
                _tagger.localRotation = Quaternion.Euler(0f, 0f, turned ? 0f : 180f);
                _chant.text = turned ? Loc.T("돌아봤다!", "Looking!") : Loc.T("무궁화 꽃이 피었습니다…", "Red light, green light…");
                _chant.color = turned ? new Color(0.85f, 0.15f, 0.15f) : Navy;
            }

            private void Update()
            {
                if (_ended) return;
                float dt = Time.unscaledDeltaTime;
                _phaseT += dt;
                if (_phaseT >= _phaseLen) NextPhase(!_turned);
                if (_holding)
                {
                    if (_turned && _phaseT > 0.18f)   // 0.18초 반응 여유
                    {
                        _caught++;
                        _progress = 0f;
                        _holding = false;
                        CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.8f);
                        Status.text = Loc.T($"걸렸다! 처음부터 (걸린 횟수 {_caught})", $"Caught! Back to start ({_caught})");
                        if (_caught >= 3) { _ended = true; StartCoroutine(EndAfter(0.8f, 0)); return; }
                    }
                    else _progress = Mathf.Min(1f, _progress + dt * 0.22f);
                }
                _me.anchorMin = _me.anchorMax = new Vector2(0.5f, Mathf.Lerp(0.12f, 0.78f, _progress));
                _touchable = _progress >= 0.999f;
                if (_touchable && !_turned) _chant.text = Loc.T("지금! 술래를 터치!", "Now! Tap It!");
            }

            private IEnumerator Win()
            {
                _ended = true;
                CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.8f);
                Status.text = Loc.T("술래 터치! 이겼다!", "Tagged It! You win!");
                _chant.text = Loc.T("만세!", "Hooray!");
                yield return new WaitForSecondsRealtime(0.9f);
                Finish(1);
            }

            private IEnumerator EndAfter(float s, int r) { yield return new WaitForSecondsRealtime(s); Finish(r); }

            private class HoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
            {
                public MugunghwaMission target;
                public void OnPointerDown(PointerEventData e) => target?.OnPointerDown(e);
                public void OnPointerUp(PointerEventData e) => target?.OnPointerUp(e);
            }
        }
    }
}
