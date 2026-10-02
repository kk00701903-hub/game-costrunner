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
            /// 118차(사용자): 대회 보상 — 상금(코인) · 하트 · 스트레스 감소. 이겼을 때 StoryContest.GrantReward 가 준다.
            public int rewardCoins; public int rewardHearts; public int rewardStress;
            public string Name => Loc.T(ko, en);
            /// 안내 팝업·토스트에 쓰는 한 줄. 「코인 250 · 하트 1 · 스트레스 −5」
            public string RewardText
            {
                get
                {
                    string ko2 = $"코인 {rewardCoins}";
                    string en2 = $"{rewardCoins} coins";
                    if (rewardHearts > 0) { ko2 += $" · 하트 {rewardHearts}"; en2 += $" · {rewardHearts} hearts"; }
                    if (rewardStress > 0) { ko2 += $" · 스트레스 −{rewardStress}"; en2 += $" · stress −{rewardStress}"; }
                    return Loc.T(ko2, en2);
                }
            }
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
            new Def { chapter = 1,  ko = "첫 해안도로 달리기 대회", en = "First Coast Road Race", goal = Goal.Coins,  target = 120, seconds = 180f , rewardCoins = 250, rewardHearts = 1, rewardStress = 5 },
            new Def { chapter = 4,  ko = "봄 사진 콘테스트",        en = "Spring Photo Contest",  goal = Goal.Photos, target = 2,   seconds = 180f , rewardCoins = 300, rewardHearts = 1, rewardStress = 5 },
            new Def { chapter = 7,  ko = "갈매기 퇴치전",           en = "Seagull Hunt",          goal = Goal.Boss,   target = 1,   seconds = 180f , rewardCoins = 350, rewardHearts = 1, rewardStress = 6 },
            new Def { chapter = 10, ko = "코인 마라톤",             en = "Coin Marathon",         goal = Goal.Coins,  target = 350, seconds = 180f , rewardCoins = 420, rewardHearts = 2, rewardStress = 6 },
            new Def { chapter = 13, ko = "가을 사진 콘테스트",      en = "Autumn Photo Contest",  goal = Goal.Photos, target = 3,   seconds = 180f , rewardCoins = 480, rewardHearts = 2, rewardStress = 7 },
            new Def { chapter = 15, ko = "골렘 격퇴전",             en = "Golem Rout",            goal = Goal.Boss,   target = 2,   seconds = 180f , rewardCoins = 550, rewardHearts = 2, rewardStress = 8 },
            new Def { chapter = 18, ko = "해안도로 그랑프리",       en = "Coast Grand Prix",      goal = Goal.Coins,  target = 600, seconds = 180f , rewardCoins = 650, rewardHearts = 3, rewardStress = 8 },
            new Def { chapter = 20, ko = "송전탑 완주",             en = "Tower Finish",          goal = Goal.Finish, target = 1,   seconds = 200f , rewardCoins = 800, rewardHearts = 3, rewardStress = 10 },
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
            Photos = 0; Bosses = 0; TimedOut = false; _rewardedChapter = -1; PaidPass = false;
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

        /// 118차: 대회에 이겼을 때 한 번만 보상을 준다(SceneFlowController.NotifyStageCleared 성공 경로).
        private static int _rewardedChapter = -1;
        public static void GrantReward(GameManager gm)
        {
            if (!Active || Current == null || gm?.Save == null) return;
            if (!Succeeded || PaidPass) return;   // 135차: 코인 통과는 보상 없음
            if (_rewardedChapter == Current.chapter) return;
            _rewardedChapter = Current.chapter;
            var sv = gm.Save;
            sv.stats.money += Current.rewardCoins;
            sv.chapterHearts += Current.rewardHearts;
            if (Current.rewardStress > 0) sv.stats.stress = Mathf.Max(0, sv.stats.stress - Current.rewardStress);
            sv.stats.Clamp();
            gm.Persist();
            CoastToast.Show(Loc.T($"대회 보상 — {Current.RewardText}", $"Contest reward — {Current.RewardText}"));
        }

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
        public static bool Succeeded => Active && (PaidPass || (GoalMet && !TimedOut && Elapsed <= Current.seconds + 0.5f));
        /// 135차(사용자): 대회 미달 카드에서 코인을 내고 통과 — 보상 없이 주차만 넘어간다. 비용 = 상금 ×4.
        public static bool PaidPass { get; set; }
        public static int PassCost => Current != null ? Mathf.Max(800, Current.rewardCoins * 4) : 800;

        public static string ProgressText()
        {
            if (!Active) return "";
            var d = Current;
            string p = d.goal == Goal.Finish ? Loc.T("완주하면 성공", "Finish to win") : $"{Mathf.Min(Progress(), d.target)} / {d.target}";
            return p;
        }

        // ── 러닝 HUD: 86차(사용자 시안) 큰 배너 — 노랑→하늘 그라데이션 알약(UI_Contest_Banner) + 깃발(UI_Contest_Flag) + 두 줄 파란 제목 + 오른쪽 「코인 120/120 · 2:26 · 3위」 ──
        private class ContestHud : MonoBehaviour
        {
            private Canvas _canvas; private Text _t, _s; private Image _pill, _tint; private bool _failShown;
            private void Start()
            {
                _canvas = CoastUiCanvas.Create("ContestHudCanvas", 300);
                var root = CoastUiCanvas.Root(_canvas);
                var tex = ArtAssets.LoadTexture("UI_Contest_Banner");
                _pill = CoastHudLayout.MakeImage(root, "Pill", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-300f, -297f), new Vector2(300f, -184f), Color.white);
                if (tex != null) { _pill.sprite = CoastUiArt.AsSprite(tex); _pill.preserveAspect = false; }
                else { _pill.sprite = CoastUiArt.RoundedRect(56); _pill.type = Image.Type.Sliced; _pill.color = new Color(1f, 0.93f, 0.50f); }
                _pill.raycastTarget = false;
                var rt = _pill.rectTransform;
                _tint = CoastUiArt.Panel(rt, "Tint", new Color(0f, 0f, 0f, 0f), 52); _tint.raycastTarget = false;
                _tint.rectTransform.anchorMin = Vector2.zero; _tint.rectTransform.anchorMax = Vector2.one; _tint.rectTransform.offsetMin = new Vector2(6f, 6f); _tint.rectTransform.offsetMax = new Vector2(-6f, -6f);
                var ftex = ArtAssets.LoadTexture("UI_Contest_Flag");
                var flag = CoastHudLayout.MakeImage(rt, "Flag", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -48f), new Vector2(92f, 48f), Color.white);
                if (ftex != null) { flag.sprite = CoastUiArt.AsSprite(ftex); flag.preserveAspect = true; } else flag.color = Color.clear;
                flag.raycastTarget = false;
                _t = CoastHudLayout.MakeText(rt, "T", "", 27, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(94f, 6f), new Vector2(-290f, -6f));
                _t.color = new Color(0.01f, 0.08f, 0.58f); _t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_t, new Color(1f, 1f, 1f, 0.95f), 2.2f);
                _t.horizontalOverflow = HorizontalWrapMode.Wrap; _t.resizeTextForBestFit = true; _t.resizeTextMinSize = 12; _t.resizeTextMaxSize = CoastHudLayout.Scaled(27);
                var coin = CoastUiArt.Icon("Coin");
                if (coin != null) { var ci = CoastHudLayout.MakeImage(rt, "CoinIc", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-284f, -14f), new Vector2(-256f, 14f), Color.white); ci.sprite = coin; ci.preserveAspect = true; ci.raycastTarget = false; }
                _s = CoastHudLayout.MakeText(rt, "S", "", 15, TextAnchor.MiddleLeft, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-252f, 10f), new Vector2(-14f, -10f));
                _s.color = new Color(0.05f, 0.18f, 0.60f); _s.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_s, new Color(1f, 1f, 1f, 0.9f), 1.4f);
                _s.horizontalOverflow = HorizontalWrapMode.Wrap; _s.resizeTextForBestFit = true; _s.resizeTextMinSize = 10; _s.resizeTextMaxSize = CoastHudLayout.Scaled(15);
                // 반짝이(시안: 왼쪽 위·오른쪽 위·오른쪽 아래)
                foreach (var (ax, ay, sz) in new[] { (0.02f, 1.02f, 22), (0.96f, 1.0f, 18), (0.93f, 0.02f, 14) })
                {
                    var sp = CoastHudLayout.MakeText(rt, "Spark", "✦", sz, TextAnchor.MiddleCenter, new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(-16f, -16f), new Vector2(16f, 16f));
                    sp.color = new Color(1f, 0.95f, 0.55f); sp.raycastTarget = false; CoastUiArt.OutlineText(sp, new Color(1f, 1f, 1f, 0.8f), 1f);
                }
            }
            private void Update()
            {
                if (!Active || _t == null) return;
                var d = Current; float rem = Remaining;
                bool met = GoalMet;
                string rank = ContestRivals.RankText();   // 66차-1: 라이벌 순위
                string name = d.Name;
                if (name.Length > 7 && !name.Contains("\n")) { int sp = name.IndexOf(' ', name.Length / 2 - 1); if (sp < 0) sp = name.LastIndexOf(' '); if (sp > 0) name = name.Substring(0, sp) + "\n" + name.Substring(sp + 1); }
                _t.text = name;
                string prog = d.goal == Goal.Finish ? Loc.T("완주", "Finish") : $"{Mathf.Min(Progress(), d.target)}/{d.target}";
                _s.text = $"{prog}  ·  {Mathf.FloorToInt(rem / 60f)}:{Mathf.FloorToInt(rem % 60f):00}" + (rank.Length > 0 ? $"  ·  {rank}" : "");
                _tint.color = met ? new Color(0.2f, 0.9f, 0.4f, 0.22f) : rem < 20f ? new Color(1f, 0.2f, 0.2f, 0.25f) : new Color(0f, 0f, 0f, 0f);
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
        public static bool IsOpen => _canvas != null;

        static readonly Color Sky = new Color(0.80f, 0.91f, 0.99f);
        static readonly Color SkyEdge = new Color(0.58f, 0.78f, 0.96f);
        static readonly Color TitleBlue = new Color(0.27f, 0.47f, 0.92f);
        static readonly Color BadgeGold = new Color(1f, 0.83f, 0.30f);
        static readonly Color BadgeInk = new Color(0.45f, 0.26f, 0.06f);

        /// 117차(사용자 시안): 하늘색 머리띠 + 노란 「이번 주말!」 배지 + 점선으로 나뉜 다섯 줄 + 양쪽 화살표 출발 버튼.
        public static void Show(StoryContest.Def d, Action onGo)
        {
            Close();
            if (d == null) { onGo?.Invoke(); return; }
            var crt = EventCardKit.Card("ContestIntroCanvas", 466, new Vector2(660f, 962f), out _canvas, 12f);

            var bandEdge = CoastUiArt.Panel(crt, "HeadBandEdge", new Color(SkyEdge.r, SkyEdge.g, SkyEdge.b, 0.55f), 26);
            bandEdge.raycastTarget = false;
            var ber = bandEdge.rectTransform;
            ber.anchorMin = new Vector2(0f, 1f); ber.anchorMax = new Vector2(1f, 1f); ber.pivot = new Vector2(0.5f, 1f);
            ber.offsetMin = new Vector2(14f, -190f); ber.offsetMax = new Vector2(-14f, -6f);

            var band = CoastUiArt.Panel(crt, "HeadBand", Sky, 26); band.raycastTarget = false;
            var br = band.rectTransform;
            br.anchorMin = new Vector2(0f, 1f); br.anchorMax = new Vector2(1f, 1f); br.pivot = new Vector2(0.5f, 1f);
            br.offsetMin = new Vector2(18f, -186f); br.offsetMax = new Vector2(-18f, -10f);

            var badge = CoastUiArt.Panel(crt, "Badge", BadgeGold, 22); badge.raycastTarget = false;
            var bgr = badge.rectTransform;
            bgr.anchorMin = bgr.anchorMax = new Vector2(0f, 1f); bgr.pivot = new Vector2(0f, 1f);
            bgr.anchoredPosition = new Vector2(44f, -26f); bgr.sizeDelta = new Vector2(226f, 60f);
            var bt = CoastHudLayout.MakeText(crt, "BadgeT", Loc.T("이번 주말!", "This weekend!"), 26, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -86f), new Vector2(270f, -26f));
            bt.color = BadgeInk; bt.fontStyle = FontStyle.Bold; bt.raycastTarget = false;
            CoastUiArt.OutlineText(bt, new Color(1f, 1f, 1f, 0.85f), 2f);

            EventCardKit.JellyTitle(crt, d.Name, TitleBlue, new Color(1f, 1f, 1f, 0.95f), 96f, 82f, 46);
            EventCardKit.Kid(crt, Loc.T("누나, 달려보자!", "Let's run!"), true, 196f);

            int m = Mathf.FloorToInt(d.seconds / 60f), sec = Mathf.FloorToInt(d.seconds % 60f);
            string icon = d.goal == StoryContest.Goal.Photos ? "Icon_Camera" : d.goal == StoryContest.Goal.Coins ? "Icon_Coin" : d.goal == StoryContest.Goal.Boss ? "Icon_Bang" : "Icon_Tower";

            const float rowH = 70f, gap = 22f;
            float y = 224f;
            // 228차: 조건을 채워도 결승선(1,500 m)까지 못 가면 실패 — 카드에 같이 적는다
            string fin = d.goal == StoryContest.Goal.Finish ? "" : (Loc.IsKo ? $" + {StoryProgress.MaxRunMeters:N0} m 완주" : " + " + Loc.Tr($"Finish {StoryProgress.MaxRunMeters:N0} m"));
            EventCardKit.IconRow(crt, icon, new Color(1f, 0.85f, 0.45f), Loc.T("조건 · ", "Goal · ") + d.GoalText + fin, y, rowH, 27, null, null, 46f, 40f);
            DotLine(crt, y + rowH + 9f); y += rowH + gap;
            EventCardKit.IconRow(crt, "Icon_Speed", new Color(0.62f, 0.90f, 0.72f), Loc.T($"제한시간 · {m}:{sec:00}", $"Time limit · {m}:{sec:00}"), y, rowH, 27, null, null, 46f, 40f);
            DotLine(crt, y + rowH + 9f); y += rowH + gap;
            EventCardKit.IconRow(crt, "Icon_Bulb", new Color(0.70f, 0.86f, 1f), Loc.T("이야기와 상관없는 마을 대회야.", "A village contest, unrelated to the story."), y, rowH, 23, null, null, 46f, 24f);
            DotLine(crt, y + rowH + 9f); y += rowH + gap;
            EventCardKit.IconRow(crt, "Icon_Bang", new Color(1f, 0.82f, 0.42f), Loc.T("조건을 못 채우면 이 주는 넘어가지 않아!", "Miss the goal and the week doesn't advance!"), y, rowH, 21, null, null, 46f, 16f);
            DotLine(crt, y + rowH + 9f); y += rowH + gap;

            EventCardKit.IconRow(crt, "Icon_Star", new Color(0.78f, 0.66f, 1f), Loc.T("보상", "Reward"), y, rowH, 28, null, null, 46f, 40f);
            var gm = GameManager.I; var sv = gm != null ? gm.Save : null;
            var rs = RaisingFun.RecommendedStat(d);
            string tip = d.RewardText + "\n"
                + (sv != null
                    ? Loc.T($"{RaisingFun.StatName(rs)}이 힘 — ", $"{RaisingFun.StatName(rs)} matters — ") + RaisingFun.ContestStatLine(sv)
                    : Loc.T("체력 → HP · 순발력 → 레인 이동 · 매력 → 니어미스", "Stamina → HP · Agility → lanes · Charm → near-miss"));
            // 127차(사용자 「출발 버튼하고 글자가 겹친다」): 보상 설명은 위 y 고정이 아니라 **출발 버튼 위(바닥 +152)까지**로 잡아
            // 카드가 화면에 맞춰 줄어들어도 버튼 위에서 끝나고, 넘치면 글자가 줄어든다(Truncate).
            var tipT = CoastHudLayout.MakeText(crt, "Tip", tip, 19, TextAnchor.UpperLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(126f, 152f), new Vector2(-44f, -(y + rowH + 8f)));
            tipT.color = new Color(0.34f, 0.28f, 0.24f); tipT.raycastTarget = false;
            tipT.horizontalOverflow = HorizontalWrapMode.Wrap; tipT.verticalOverflow = VerticalWrapMode.Truncate;
            tipT.resizeTextForBestFit = true; tipT.resizeTextMinSize = 12; tipT.resizeTextMaxSize = CoastHudLayout.Scaled(19);

            var go = EventCardKit.IconButton(crt, "Go", "Icon_Arrow", Loc.T("출발!", "GO!"), new Color(1f, 0.52f, 0.10f),
                new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(470f, 96f), () => { Close(); onGo?.Invoke(); }, 34);
            RightArrow(go);
            CoastAudioManager.PlayAnywhere(CoastSfx.ChapterClear, 0.5f);
        }

        /// 시안의 옅은 점선 구분선.
        static void DotLine(RectTransform card, float yTop)
        {
            var t = CoastHudLayout.MakeText(card, "Dots", "· · · · · · · · · · · · · · · · · · · · · ·", 16, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(46f, -yTop - 12f), new Vector2(-40f, -yTop + 12f));
            t.color = new Color(0.82f, 0.72f, 0.56f, 0.85f); t.raycastTarget = false;
        }

        /// 출발 버튼 오른쪽에도 화살표(시안은 좌우 두 개).
        static void RightArrow(Button b)
        {
            if (b == null) return;
            var sp = CoastUiArt.Art("Icon_Arrow");
            if (sp == null) return;
            var go = new GameObject("ArrowR", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(b.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f); rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-26f, 0f); rt.sizeDelta = new Vector2(44f, 44f);
            var im = go.GetComponent<Image>();
            im.sprite = sp; im.color = Color.white; im.raycastTarget = false; im.preserveAspect = true;
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
            CoastPrefs.VibrateEvent();   // 109차: 대회 결과 — 특정 이벤트 진동
            var crt = EventCardKit.Card("ContestResultCanvas", 470, new Vector2(648f, 1130f), out _canvas, 0f);   // 135차: 버튼 3개(코인 통과 추가) — 안내 상자와 안 겹치게
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
            EventCardKit.Kid(crt, timeout ? Loc.T("누나, 노을이 졌어…", "The sun set…") : Loc.T("괜찮아, 다음에 또!", "It's okay, next time!"), true);   // 109차: 꼬마 동행
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
            EventCardKit.IconButton(crt, "Retry", "Icon_Arrow", Loc.T("지금 다시 도전", "Retry now"), new Color(1f, 0.55f, 0.12f), new Vector2(0f, 0f), new Vector2(30f, 132f), new Vector2(284f, 78f), () =>
            {
                Close(); Time.timeScale = 1f;
                StageManager.Instance?.RetryCurrent();
            }, 24);
            EventCardKit.IconButton(crt, "Back", "Icon_Book", Loc.T("스토리화면으로", "To story"), new Color(0.28f, 0.58f, 0.98f), new Vector2(1f, 0f), new Vector2(-30f, 132f), new Vector2(284f, 78f), () =>
            {
                Close(); Time.timeScale = 1f;
                if (GameManager.Active) GameManager.I.ContestFail();
            }, 24);
            // 135차(사용자): 그냥 넘어가기는 없다 — 코인을 내면 통과(보상 없음, 주차만 진행)
            int cost = StoryContest.PassCost;
            bool canPay = GameManager.Active && GameManager.I.Save != null && GameManager.I.Save.stats.money >= cost;
            var pay = EventCardKit.IconButton(crt, "Pay", "Icon_Coin", Loc.T($"코인 {cost:N0} 내고 통과", $"Pay {cost:N0} coins to pass"), canPay ? new Color(0.95f, 0.72f, 0.20f) : new Color(0.55f, 0.55f, 0.60f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(588f, 78f), () =>
            {
                if (!canPay) { CoastToast.Show(Loc.T("코인이 모자라 — 다시 도전하거나 이 주를 다시 키우자", "Not enough coins — retry or raise this week again")); return; }
                var sv = GameManager.I.Save; sv.stats.money -= cost; GameManager.I.Persist();
                StoryContest.PaidPass = true;
                Close(); Time.timeScale = 1f;
                CoastToast.Show(Loc.T($"코인 {cost:N0} 을 내고 통과했어 (보상 없음)", $"Paid {cost:N0} coins to pass (no reward)"));
                if (timeout) StageManager.Instance?.DebugClear();
                else UnityEngine.Object.FindAnyObjectByType<SceneFlowController>()?.ContestPaidPass();
            }, 22);
            CoastAudioManager.PlayAnywhere(CoastSfx.NearMiss, 0.6f);
        }

        public static void Close() { if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject); _canvas = null; }
    }
}
