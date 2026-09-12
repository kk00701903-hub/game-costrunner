using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 55차(사용자): 스토리 모드의 러닝은 **대회**다 — 이야기와 무관하게, 주차마다 정해진 대회에 참가해
    ///   제한시간 안에 조건(코인 모으기·사진(포토카드) 모으기·보스 퇴치·완주)을 이뤄야 한다.
    ///   대회를 못 깨면 주차가 넘어가지 않는다(GameManager.ContestFail → 그 주를 다시 육성).
    ///   러닝 중엔 컷씬이 전혀 안 나온다(컷씬은 육성의 턴 사이에서만 — TamaRaisingUI.BoundaryRoutine).
    public static class StoryContest
    {
        public enum Goal { Coins, Photos, Boss, Finish }

        public class Def
        {
            public int chapter; public string ko, en; public Goal goal; public int target; public float seconds;
            public string Name => Loc.T(ko, en);
            public string ShortGoal => goal == Goal.Coins ? Loc.T("코인", "Coins") : goal == Goal.Photos ? Loc.T("사진", "Photos") : goal == Goal.Boss ? Loc.T("보스", "Boss") : Loc.T("완주", "Finish");
            public string GoalText
            {
                get
                {
                    switch (goal)
                    {
                        case Goal.Coins: return Loc.T($"코인 {target} 모으기", $"Collect {target} coins");
                        case Goal.Photos: return Loc.T($"사진(포토카드) {target}장 찍기", $"Take {target} photos");
                        case Goal.Boss: return Loc.T($"보스 {target}마리 물리치기", $"Defeat {target} bosses");
                        default: return Loc.T("송전탑까지 완주", "Reach the tower");
                    }
                }
            }
        }

        /// 러닝 챕터 8개(StoryProgress.RunChapters)에 하나씩.
        public static readonly Def[] All =
        {
            new Def { chapter = 1,  ko = "첫 해안도로 달리기 대회", en = "First Coast Road Race", goal = Goal.Coins,  target = 120, seconds = 180f },
            new Def { chapter = 4,  ko = "봄 사진 콘테스트",        en = "Spring Photo Contest",  goal = Goal.Photos, target = 2,   seconds = 180f },
            new Def { chapter = 7,  ko = "갈매기 퇴치전",           en = "Seagull Hunt",          goal = Goal.Boss,   target = 1,   seconds = 180f },
            new Def { chapter = 10, ko = "코인 마라톤",             en = "Coin Marathon",         goal = Goal.Coins,  target = 350, seconds = 180f },
            new Def { chapter = 13, ko = "가을 사진 콘테스트",      en = "Autumn Photo Contest",  goal = Goal.Photos, target = 3,   seconds = 180f },
            new Def { chapter = 15, ko = "골렘 격퇴전",             en = "Golem Rout",            goal = Goal.Boss,   target = 2,   seconds = 180f },
            new Def { chapter = 18, ko = "해안도로 그랑프리",       en = "Coast Grand Prix",      goal = Goal.Coins,  target = 600, seconds = 180f },
            new Def { chapter = 20, ko = "송전탑 완주",             en = "Tower Finish",          goal = Goal.Finish, target = 1,   seconds = 200f },
        };
        public static Def Get(int chapter) { foreach (var d in All) if (d.chapter == chapter) return d; return null; }

        public static bool Active { get; private set; }
        public static Def Current { get; private set; }
        public static int Photos { get; private set; }
        public static int Bosses { get; private set; }
        public static bool TimedOut { get; private set; }
        private static ContestHud _hud;

        /// 스토리 러닝 스테이지 시작(GameSession.HandleStageStart 맨 앞) — 진행 초기화 + HUD.
        public static void Begin(int chapter)
        {
            Current = Get(chapter);
            Active = Current != null;
            Photos = 0; Bosses = 0; TimedOut = false;
            if (_hud != null) UnityEngine.Object.Destroy(_hud.gameObject);
            _hud = null;
            if (!Active) return;
            var go = new GameObject("ContestHud");
            _hud = go.AddComponent<ContestHud>();
        }
        /// 보스 퇴치전이면 보스 배치(GameSession 이 지난 BossDirector 를 지운 뒤에 호출).
        public static void SpawnBoss(PlayerController player, ObstacleSpawner obstacles, StageDef stage)
        {
            if (!Active || Current.goal != Goal.Boss || player == null) return;
            // BossDirector 의 마리 수 = 1 + (챕터−6)/3 → 목표 마리 수가 되도록 가짜 챕터로.
            int fake = 6 + 3 * (Current.target - 1);
            BossDirector.Create(player, obstacles, fake, false, Current.chapter * 977 + (stage != null ? stage.stageIndex : 0));
        }
        public static void End() { Active = false; Current = null; if (_hud != null) UnityEngine.Object.Destroy(_hud.gameObject); _hud = null; ContestRivals.Clear(); }

        public static void NotePhoto() { if (Active) Photos++; }
        public static void NoteBoss() { if (Active) Bosses++; }
        public static void NoteTimeout() { if (Active) TimedOut = true; }

        public static int Progress()
        {
            if (!Active) return 0;
            var st = StageRunStats.Instance;
            switch (Current.goal)
            {
                case Goal.Coins: return st != null ? st.CoinValue : 0;
                case Goal.Photos: return Photos;
                case Goal.Boss: return Bosses;
                default: return 0;
            }
        }
        public static float Elapsed => StageRunStats.Instance != null ? StageRunStats.Instance.Seconds : 0f;
        public static float Remaining => Active ? Mathf.Max(0f, Current.seconds - Elapsed) : 0f;
        public static bool GoalMet => Active && (Current.goal == Goal.Finish || Progress() >= Current.target);
        /// 완주 시점 판정: 목표 달성 + 제한시간 안.
        public static bool Succeeded => Active && GoalMet && !TimedOut && Elapsed <= Current.seconds + 0.5f;

        public static string ProgressText()
        {
            if (!Active) return "";
            var d = Current;
            string p = d.goal == Goal.Finish ? Loc.T("완주하면 성공", "Finish to win") : $"{Mathf.Min(Progress(), d.target)} / {d.target}";
            return p;
        }

        // ── 러닝 HUD: 위쪽 가운데 알약 「대회 이름 · 진행 · 남은 시간」 ──
        private class ContestHud : MonoBehaviour
        {
            private Canvas _canvas; private Text _t; private Image _pill; private bool _failShown;
            private void Start()
            {
                _canvas = CoastUiCanvas.Create("ContestHudCanvas", 300);
                var root = CoastUiCanvas.Root(_canvas);
                _pill = CoastUiArt.CutePill(root, "Pill", new Color(0.10f, 0.13f, 0.30f, 0.90f), 18, 3);
                var rt = _pill.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -138f); rt.sizeDelta = new Vector2(460f, 50f); _pill.raycastTarget = false;
                _t = CoastHudLayout.MakeText(rt, "T", "", 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
                _t.color = Color.white; _t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_t, new Color(0f, 0f, 0f, 0.5f), 1.2f);
                _t.resizeTextForBestFit = true; _t.resizeTextMinSize = 11; _t.resizeTextMaxSize = CoastHudLayout.Scaled(16);
            }
            private void Update()
            {
                if (!Active || _t == null) return;
                var d = Current; float rem = Remaining;
                bool met = GoalMet;
                string rank = ContestRivals.RankText();   // 66차-1: 라이벌 순위
                _t.text = $"{d.Name}  ·  {d.ShortGoal} {ProgressText()}  ·  {Mathf.FloorToInt(rem / 60f)}:{Mathf.FloorToInt(rem % 60f):00}" + (rank.Length > 0 ? $"  ·  {rank}" : "");
                _pill.color = met ? new Color(0.15f, 0.45f, 0.25f, 0.92f) : rem < 20f ? new Color(0.55f, 0.15f, 0.15f, 0.92f) : new Color(0.10f, 0.13f, 0.30f, 0.90f);
                if (rem <= 0f && !met && !_failShown && d.goal != Goal.Finish)
                {
                    // 시간 초과 — 목표를 못 채웠으면 그 자리에서 대회 종료
                    _failShown = true; NoteTimeout();
                    ContestResultUI.ShowFail(true);
                }
            }
            private void OnDestroy() { if (_canvas != null) Destroy(_canvas.gameObject); }
        }
    }

    /// 대회 안내(육성 턴 시작, 러닝 직전) — 이름·조건·제한시간 → 「출발!」. 60차: EventCardKit(크림 카드·젤리 제목·아이콘 줄) 스타일.
    public static class ContestIntroUI
    {
        private static Canvas _canvas;
        public static void Show(StoryContest.Def d, Action onGo)
        {
            Close();
            if (d == null) { onGo?.Invoke(); return; }
            var crt = EventCardKit.Card("ContestIntroCanvas", 466, new Vector2(640f, 640f), out _canvas, 20f);
            var kicker = CoastHudLayout.MakeText(crt, "K", Loc.T("이번 주 대회", "This week's contest"), 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), new Vector2(0f, -24f));
            kicker.color = new Color(0.90f, 0.32f, 0.45f); kicker.fontStyle = FontStyle.Bold;
            EventCardKit.JellyTitle(crt, d.Name, new Color(0.45f, 0.35f, 0.95f), new Color(0.20f, 0.12f, 0.45f), 56f, 84f, 44);
            EventCardKit.Divider(crt, 148f);
            int m = Mathf.FloorToInt(d.seconds / 60f), sec = Mathf.FloorToInt(d.seconds % 60f);
            string icon = d.goal == StoryContest.Goal.Photos ? "Icon_Camera" : d.goal == StoryContest.Goal.Coins ? "Icon_Coin" : d.goal == StoryContest.Goal.Boss ? "Icon_Bang" : "Icon_Tower";
            EventCardKit.IconRow(crt, icon, new Color(1f, 0.85f, 0.45f), Loc.T("조건 · ", "Goal · ") + d.GoalText, 180f, 64f, 24);
            EventCardKit.IconRow(crt, "Icon_Speed", new Color(0.70f, 0.80f, 1f), Loc.T($"제한시간 · {m}:{sec:00}", $"Time limit · {m}:{sec:00}"), 254f, 64f, 24);
            var box = EventCardKit.InfoBox(crt, 336f, 150f);
            EventCardKit.IconRow(box, "Icon_Bulb", new Color(0.80f, 0.88f, 1f), Loc.T("이야기와 상관없는 마을 대회야.", "A village contest, unrelated to the story."), 14f, 52f, 19, null, null, 18f, 14f);
            EventCardKit.IconRow(box, "Icon_Bang", new Color(1f, 0.85f, 0.45f), Loc.T("조건을 못 채우면 이 주는 넘어가지 않아.", "Miss the goal and the week doesn't advance."), 82f, 52f, 19, null, null, 18f, 14f);
            EventCardKit.IconButton(crt, "Go", "Icon_Arrow", Loc.T("출발!", "GO!"), new Color(1f, 0.52f, 0.10f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(440f, 84f), () => { Close(); onGo?.Invoke(); }, 32);
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.5f);
        }
        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }

    /// 대회 결과(미달) 화면 — 다시 도전 / 스토리로(주차는 그대로). 66차(사용자 시안): 금테 크림 카드 · 젤리 「대회 미달…」 · 아이콘 줄 2 · 흰 안내 상자 3개(! / 전구 / 새로고침) · 주황 「지금 다시 도전」 / 파랑 「스토리화면으로」.
    public static class ContestResultUI
    {
        private static Canvas _canvas;
        public static bool IsOpen => _canvas != null;
        private static readonly Color Gold = new Color(0.96f, 0.78f, 0.38f);

        /// 흰 상자(얇은 금테) + 동그란 아이콘 + 글. 상자 위치 yTop, 높이 h.
        private static void Box(RectTransform card, float yTop, float h, string icon, Color iconBg, string text, int size)
        {
            var edge = CoastUiArt.Panel(card, "BoxEdge", new Color(0.93f, 0.80f, 0.50f), 26); edge.raycastTarget = false;
            var er = edge.rectTransform; er.anchorMin = new Vector2(0f, 1f); er.anchorMax = new Vector2(1f, 1f); er.pivot = new Vector2(0.5f, 1f);
            er.offsetMin = new Vector2(30f, -yTop - h); er.offsetMax = new Vector2(-30f, -yTop);
            var box = CoastUiArt.Panel(er, "Box", Color.white, 23); box.raycastTarget = false;
            var br = box.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = new Vector2(3f, 3f); br.offsetMax = new Vector2(-3f, -3f);
            float ih = 54f;
            EventCardKit.IconRow(br, icon, iconBg, text, (h - ih) * 0.5f - 3f, ih, size, null, null, 14f, 14f);
        }

        public static void ShowFail(bool timeout)
        {
            Close();
            var d = StoryContest.Current; if (d == null) return;
            Time.timeScale = 0f;
            var crt = EventCardKit.Card("ContestResultCanvas", 470, new Vector2(648f, 1040f), out _canvas, 0f);
            // 금테(카드 가장자리 금색 띠 + 안쪽 크림) — 카드 배경 바로 위, 스파클 아래
            var rim = CoastUiArt.Panel(crt, "Rim", Gold, 28); rim.raycastTarget = false;
            var rr = rim.rectTransform; rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = new Vector2(6f, 6f); rr.offsetMax = new Vector2(-6f, -6f);
            rim.transform.SetSiblingIndex(3);
            var inner = CoastUiArt.Panel(crt, "RimIn", EventCardKit.Cream, 25); inner.raycastTarget = false;
            var ir = inner.rectTransform; ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one; ir.offsetMin = new Vector2(10f, 10f); ir.offsetMax = new Vector2(-10f, -10f);
            inner.transform.SetSiblingIndex(4);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(-150f, -70f), 18, Gold);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(170f, -60f), 22, Gold);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 1f), new Vector2(230f, -150f), 12, Gold);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 0f), new Vector2(-260f, 150f), 14, Gold);
            EventCardKit.Sparkle(crt, new Vector2(0.5f, 0f), new Vector2(250f, 160f), 18, Gold);
            EventCardKit.JellyTitle(crt, Loc.T("대회 미달…", "Contest failed…"), new Color(0.98f, 0.22f, 0.22f), new Color(0.60f, 0.06f, 0.10f), 96f, 120f, 78);
            string icon = d.goal == StoryContest.Goal.Photos ? "Icon_Camera" : d.goal == StoryContest.Goal.Coins ? "Icon_Coin" : d.goal == StoryContest.Goal.Boss ? "Icon_Bang" : "Icon_Tower";
            EventCardKit.IconRow(crt, icon, new Color(1f, 0.80f, 0.35f), d.Name, 266f, 66f, 32, null, null, 44f, 44f);
            string prog = StoryContest.ProgressText();
            EventCardKit.IconRow(crt, "Icon_Card", new Color(0.55f, 0.72f, 1f), d.GoalText, 352f, 60f, 24, prog, new Color(1f, 0.55f, 0.20f), 44f, 44f);
            string why = timeout ? Loc.T("제한시간이 끝났어.", "Time's up.") : Loc.T("결승선은 넘었지만 조건을 못 채웠어.", "Crossed the line but missed the goal.");
            Box(crt, 452f, 92f, "Icon_Bang", new Color(1f, 0.82f, 0.30f), why, 22);
            Box(crt, 570f, 92f, "Icon_Bulb", new Color(0.62f, 0.80f, 1f), Loc.T("대회를 깨야 다음 주로 넘어갈 수 있어.", "You must win to move on to next week."), 22);
            Box(crt, 688f, 116f, "Icon_Refresh", new Color(0.45f, 0.85f, 0.75f), Loc.T("이 주를 다시 키우고 도전하거나,\n지금 바로 다시!", "Raise this week again, or\nretry right now!"), 22);
            EventCardKit.IconButton(crt, "Retry", "Icon_Arrow", Loc.T("지금 다시 도전", "Retry now"), new Color(1f, 0.55f, 0.12f), new Vector2(0f, 0f), new Vector2(30f, 40f), new Vector2(284f, 84f), () =>
            {
                Close(); Time.timeScale = 1f;
                StageManager.Instance?.RetryCurrent();
            }, 26);
            EventCardKit.IconButton(crt, "Back", "Icon_Book", Loc.T("스토리화면으로", "To story"), new Color(0.28f, 0.58f, 0.98f), new Vector2(1f, 0f), new Vector2(-30f, 40f), new Vector2(284f, 84f), () =>
            {
                Close(); Time.timeScale = 1f;
                if (GameManager.Active) GameManager.I.ContestFail();
            }, 26);
            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.6f);
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
